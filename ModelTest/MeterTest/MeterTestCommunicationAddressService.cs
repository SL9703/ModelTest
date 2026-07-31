using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Sockets;
using ModelTest.Protocol;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// 执行通信测试中的“多波特率地址读取”流程。
/// 原地址读取流程失败后，通过串口服务器管理端口依次切换数据库中的其他波特率，
/// 并在每次切换后重新读取电表地址。
/// </summary>
public sealed class MeterTestCommunicationAddressService
{
    public const int SerialServerManagementPort = 64444;
    private const int BaudRateApplyDelayMs = 150;
    private const int OptionalManagementResponseWaitMs = 300;
    private const int MaximumResponseBytes = 16 * 1024;
    private const int MaximumSendAttempts = 4;
    private static readonly TimeSpan SendRetryDelay = TimeSpan.FromMilliseconds(100);

    // 同一工位端点只允许存在一条地址读取流程，防止重复测试同时占用同一个TCP通道。
    private readonly ConcurrentDictionary<string, SemaphoreSlim> stationEndpointLocks =
        new(StringComparer.OrdinalIgnoreCase);

    // 同一个IP在一轮测试中只建立一条64444管理连接；失败状态也会缓存，避免每个工位重复连接。
    private readonly ConcurrentDictionary<string, ManagementConnectionSession> managementSessions =
        new(StringComparer.OrdinalIgnoreCase);

    // 同一轮测试中，工位485端口首次地址读取成功建连后继续保留，备用波特率尝试复用这条连接。
    private readonly ConcurrentDictionary<string, StationConnectionSession> stationSessions =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>开始一轮测试，清理上轮遗留的64444管理连接和失败缓存。</summary>
    public void BeginRun() => EndRun();

    /// <summary>结束一轮测试并关闭所有共享管理连接。</summary>
    public void EndRun()
    {
        foreach (StationConnectionSession session in stationSessions.Values)
        {
            session.Dispose();
        }

        stationSessions.Clear();

        foreach (ManagementConnectionSession session in managementSessions.Values)
        {
            session.Dispose();
        }

        managementSessions.Clear();
    }

    /// <summary>
    /// 通过本轮测试共享的64444管理连接设置指定串口服务器端口。
    /// 蓝牙接口检测和通信测试都调用此入口，确保相同IP不会重复建立管理连接。
    /// </summary>
    public Task<MeterTestSerialPortSettingResult> ApplySerialPortBaudRateAsync(
        string ipAddress,
        int tcpPort,
        string baudRate,
        int timeoutMs,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return Task.FromResult(new MeterTestSerialPortSettingResult(
                false,
                true,
                "串口服务器IP不能为空。"));
        }

        if (tcpPort is < 1 or > 65535)
        {
            return Task.FromResult(new MeterTestSerialPortSettingResult(
                false,
                true,
                $"串口服务器端口无效：{tcpPort}。"));
        }

        return ApplyBaudRateCoreAsync(
            ipAddress.Trim(),
            tcpPort,
            baudRate,
            timeoutMs,
            trace,
            cancellationToken);
    }

    /// <summary>
    /// 执行资产波特率下的第一次地址读取。
    /// 该入口会保留成功建立的工位连接，供后续备用波特率循环继续复用。
    /// </summary>
    public async Task<string> ReadAssetBaudRateAddressAsync(
        MeterTestCommunicationAddressRequirement requirement,
        string requestHex,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ValidateRequirement(requirement);

        string endpointKey = BuildStationEndpointKey(requirement.IpAddress, requirement.Port);
        SemaphoreSlim endpointLock = stationEndpointLocks.GetOrAdd(endpointKey, _ => new SemaphoreSlim(1, 1));
        await endpointLock.WaitAsync(cancellationToken);

        try
        {
            MeterTestAddressReadAttempt attempt = await ReadAddressAsync(
                requirement,
                NormalizeSerialProfile(requirement.DefaultBaudRate),
                requestHex,
                trace,
                cancellationToken);
            return attempt.ResponseHex;
        }
        finally
        {
            endpointLock.Release();
        }
    }

    /// <summary>
    /// 执行单个工位的完整波特率回退地址读取。
    /// 只要任意波特率解析出合法地址便立即停止；地址是否与资产一致由最终结果单独给出。
    /// </summary>
    public async Task<MeterTestCommunicationAddressResult> ExecuteAsync(
        MeterTestCommunicationAddressRequirement requirement,
        IReadOnlyList<string> configuredBaudRates,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ValidateRequirement(requirement);

        IReadOnlyList<string> baudRateAttempts = BuildBaudRateAttemptOrder(
            requirement.DefaultBaudRate,
            configuredBaudRates);
        string endpointKey = $"{requirement.IpAddress.Trim()}:{requirement.Port}";
        SemaphoreSlim endpointLock = stationEndpointLocks.GetOrAdd(endpointKey, _ => new SemaphoreSlim(1, 1));
        await endpointLock.WaitAsync(cancellationToken);

        try
        {
            string requestHex = SGCCTools.BuildMeterAddressReadRequest(requirement.MeterAddress);
            string actualAddress = NormalizeMeterAddress(requirement.MeterAddress);
            trace?.Invoke(
                $"[追加波特率组帧] 工位={requirement.StationNo}，资产地址={actualAddress}，"
                + $"下行报文={GenericSerialPortServerProtocol.ToHexString(Convert.FromHexString(requestHex.Replace(" ", string.Empty)))}。");
            string lastResponseHex = string.Empty;
            string stopReason = string.Empty;
            List<string> attemptedBaudRates = new();

            int firstAttemptIndex = requirement.TryAssetBaudRateFirst ? 0 : 1;
            IReadOnlyList<string> actualAttempts = baudRateAttempts.Skip(firstAttemptIndex).ToList();
            for (int index = firstAttemptIndex; index < baudRateAttempts.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string baudRate = baudRateAttempts[index];
                attemptedBaudRates.Add(baudRate);
                bool usesAssetBaudRate = index == 0;
                int displayAttempt = index - firstAttemptIndex + 1;

                trace?.Invoke(
                    $"[追加波特率尝试 {displayAttempt}/{actualAttempts.Count}] "
                    + (usesAssetBaudRate
                        ? $"按原流程已校验并同步的资产波特率 {baudRate} 读取地址。"
                        : $"资产波特率未读到地址，追加尝试候选波特率 {baudRate}。"));

                if (!usesAssetBaudRate)
                {
                    MeterTestSerialPortSettingResult settingResult = await ApplyBaudRateAsync(
                        requirement,
                        baudRate,
                        trace,
                        cancellationToken);
                    if (!settingResult.Succeeded)
                    {
                        trace?.Invoke($"串口参数设置失败：{settingResult.Message}");
                        if (settingResult.StopFurtherAttempts)
                        {
                            stopReason = settingResult.Message;
                            trace?.Invoke("当前IP的64444管理连接不可用，停止本工位后续备用波特率尝试。");
                            break;
                        }

                        continue;
                    }

                    trace?.Invoke($"串口参数 {baudRate} 设置完成，复用工位连接 {requirement.IpAddress}:{requirement.Port} 读取地址。");
                }

                MeterTestAddressReadAttempt readAttempt = await ReadAddressAsync(
                    requirement,
                    baudRate,
                    requestHex,
                    trace,
                    cancellationToken);
                lastResponseHex = readAttempt.ResponseHex;
                if (!readAttempt.AddressParsed)
                {
                    trace?.Invoke(
                        string.IsNullOrWhiteSpace(readAttempt.ResponseHex)
                            ? $"波特率 {baudRate} 未收到可解析的地址响应。"
                            : $"波特率 {baudRate} 响应未解析出地址：{readAttempt.ParseMessage}");
                    continue;
                }

                string returnedAddress = NormalizeMeterAddress(readAttempt.ReturnedAddress);
                bool addressMatched = actualAddress.Equals(returnedAddress, StringComparison.OrdinalIgnoreCase);
                string conclusion = addressMatched ? "合格" : "不合格";
                string message = addressMatched
                    ? $"波特率 {baudRate} 读取地址成功，返回地址与资产地址一致。"
                    : $"波特率 {baudRate} 读取地址成功，但返回地址与资产地址不一致。";

                trace?.Invoke(
                    $"实际地址：{actualAddress}；返回地址：{returnedAddress}；"
                    + $"有效波特率：{baudRate}；结论：{conclusion}");
                return new MeterTestCommunicationAddressResult(
                    AddressParsed: true,
                    Passed: addressMatched,
                    ResponseHex: readAttempt.ResponseHex,
                    ReturnedAddress: returnedAddress,
                    SuccessfulBaudRate: baudRate,
                    Message: message);
            }

            string attemptedText = string.Join("、", attemptedBaudRates);
            string failedMessage = !string.IsNullOrWhiteSpace(stopReason)
                ? $"备用波特率流程已停止：{stopReason}；已进入的波特率={attemptedText}。"
                : actualAttempts.Count == 0
                ? "数据库没有可追加尝试的其他波特率，仍未读取到电表地址。"
                : $"已尝试全部备用波特率（{attemptedText}），仍未读取到电表地址。";
            trace?.Invoke($"实际地址：{actualAddress}；返回地址：空；结论：不合格");
            trace?.Invoke(failedMessage);
            return new MeterTestCommunicationAddressResult(
                AddressParsed: false,
                Passed: false,
                ResponseHex: lastResponseHex,
                ReturnedAddress: string.Empty,
                SuccessfulBaudRate: string.Empty,
                Message: failedMessage);
        }
        finally
        {
            endpointLock.Release();
        }
    }

    /// <summary>
    /// 生成尝试顺序：资产信息中的当前值固定排第一，其余数据库候选按配置顺序去重追加。
    /// 这样资产值为9600时，后续正好依次尝试1200、2400、4800、115200。
    /// </summary>
    public static IReadOnlyList<string> BuildBaudRateAttemptOrder(
        string defaultBaudRate,
        IEnumerable<string> configuredBaudRates)
    {
        string normalizedDefault = NormalizeSerialProfile(defaultBaudRate);
        if (string.IsNullOrWhiteSpace(normalizedDefault))
        {
            throw new ArgumentException("资产信息中的波特率不能为空。", nameof(defaultBaudRate));
        }

        List<string> result = [normalizedDefault];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase) { normalizedDefault };
        foreach (string candidate in configuredBaudRates ?? Array.Empty<string>())
        {
            string normalizedCandidate = NormalizeSerialProfile(candidate);
            if (!string.IsNullOrWhiteSpace(normalizedCandidate) && seen.Add(normalizedCandidate))
            {
                result.Add(normalizedCandidate);
            }
        }

        return result;
    }

    /// <summary>通过串口服务器管理端口依次发送解锁和立即生效设置指令。</summary>
    private async Task<MeterTestSerialPortSettingResult> ApplyBaudRateAsync(
        MeterTestCommunicationAddressRequirement requirement,
        string baudRate,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        return await ApplySerialPortBaudRateAsync(
            requirement.IpAddress,
            requirement.Port,
            baudRate,
            requirement.TimeoutMs,
            trace,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>在共享的64444会话上串行执行解锁和串口参数设置。</summary>
    private async Task<MeterTestSerialPortSettingResult> ApplyBaudRateCoreAsync(
        string ipAddress,
        int tcpPort,
        string baudRate,
        int timeoutMs,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        GenericSerialPortServerCommandSet commandSet;
        try
        {
            commandSet = GenericSerialPortServerProtocol.BuildCommandSet(tcpPort, baudRate);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            return new MeterTestSerialPortSettingResult(false, true, ex.Message);
        }

        string managementKey = ipAddress;
        ManagementConnectionSession session = managementSessions.GetOrAdd(
            managementKey,
            _ => new ManagementConnectionSession());
        await session.Gate.WaitAsync(cancellationToken);

        try
        {
            if (session.IsUnavailable)
            {
                return new MeterTestSerialPortSettingResult(
                    false,
                    true,
                    $"管理端 {ipAddress}:{SerialServerManagementPort} 本轮已连接失败：{session.FailureMessage}");
            }

            if (!session.IsConnected)
            {
                TcpClient client = new();
                try
                {
                    await ConnectWithTimeoutAsync(
                        client,
                        ipAddress,
                        SerialServerManagementPort,
                        timeoutMs,
                        cancellationToken);
                    session.Attach(client);
                    trace?.Invoke($"串口服务器管理端首次连接成功：{ipAddress}:{SerialServerManagementPort}。后续工位复用该连接。");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    client.Dispose();
                    throw;
                }
                catch (Exception ex)
                {
                    client.Dispose();
                    string failure = $"连接管理端 {ipAddress}:{SerialServerManagementPort} 失败：{ex.Message}";
                    session.MarkUnavailable(failure);
                    return new MeterTestSerialPortSettingResult(false, true, failure);
                }
            }
            else
            {
                trace?.Invoke($"复用串口服务器管理连接：{ipAddress}:{SerialServerManagementPort}。");
            }

            NetworkStream stream = session.Stream!;
            trace?.Invoke(
                $"准备设置串口服务器端口={tcpPort}，通道={commandSet.ChannelIndex:X2}，波特率={baudRate}。");

            string unlockResponse = await SendManagementCommandAsync(
                stream,
                commandSet.UnlockCommand,
                "解锁",
                timeoutMs,
                trace,
                cancellationToken);
            string settingResponse = await SendManagementCommandAsync(
                stream,
                commandSet.SetPortCommand,
                $"设置 {baudRate}",
                timeoutMs,
                trace,
                cancellationToken);

            await Task.Delay(BaudRateApplyDelayMs, cancellationToken);
            // 通用底层协议没有规定解锁/设置必须应答；报文成功写入后，以后续698地址读取作为最终生效判据。
            bool acknowledged = !string.IsNullOrWhiteSpace(unlockResponse) &&
                                !string.IsNullOrWhiteSpace(settingResponse);
            return new MeterTestSerialPortSettingResult(
                true,
                false,
                acknowledged ? "解锁和设置指令均收到应答。" : "解锁和设置指令已发送，管理端未返回可选应答。");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            const string failure = "串口服务器管理命令等待超时。";
            session.MarkUnavailable(failure);
            return new MeterTestSerialPortSettingResult(false, true, failure);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            session.MarkUnavailable(ex.Message);
            return new MeterTestSerialPortSettingResult(false, true, ex.Message);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    /// <summary>发送一条管理指令并读取一次应答；无应答时返回空字符串。</summary>
    private static async Task<string> SendManagementCommandAsync(
        NetworkStream stream,
        byte[] command,
        string action,
        int timeoutMs,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        string commandHex = GenericSerialPortServerProtocol.ToHexString(command);
        trace?.Invoke($"{FormatTimestamp()} - 发送串口服务器{action}指令：{commandHex}");
        await WriteWithRetryAsync(
                stream,
                command,
                $"串口服务器{action}指令",
                trace,
                cancellationToken)
            .ConfigureAwait(false);

        byte[] buffer = new byte[1024];
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Math.Max(100, Math.Min(timeoutMs, OptionalManagementResponseWaitMs)));
        try
        {
            int length = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), timeoutCts.Token);
            string responseHex = length > 0
                ? GenericSerialPortServerProtocol.ToHexString(buffer.AsSpan(0, length))
                : string.Empty;
            trace?.Invoke($"{FormatTimestamp()} - 接收串口服务器{action}应答：{responseHex}");
            return responseHex;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            trace?.Invoke($"{FormatTimestamp()} - 接收串口服务器{action}应答：");
            return string.Empty;
        }
    }

    /// <summary>连接工位485端口，发送698地址读取报文并累计响应直到解析成功或超时。</summary>
    private async Task<MeterTestAddressReadAttempt> ReadAddressAsync(
        MeterTestCommunicationAddressRequirement requirement,
        string baudRate,
        string requestHex,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        string endpointKey = BuildStationEndpointKey(requirement.IpAddress, requirement.Port);
        StationConnectionSession session = GetOrCreateStationSession(endpointKey);
        await session.Gate.WaitAsync(cancellationToken);

        try
        {
            if (!session.IsConnected)
            {
                TcpClient client = new();
                try
                {
                    trace?.Invoke($"准备连接：{requirement.IpAddress}:{requirement.Port}，波特率={baudRate}，测试内容=地址读取");
                    await ConnectWithTimeoutAsync(
                        client,
                        requirement.IpAddress,
                        requirement.Port,
                        requirement.TimeoutMs,
                        cancellationToken);
                    session.Attach(client);
                    trace?.Invoke($"连接成功：{requirement.IpAddress}:{requirement.Port}，后续备用波特率尝试复用此连接");
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    client.Dispose();
                    return MeterTestAddressReadAttempt.Fail(string.Empty, "连接工位端口超时。");
                }
                catch (OperationCanceledException)
                {
                    client.Dispose();
                    throw;
                }
                catch (Exception ex)
                {
                    client.Dispose();
                    trace?.Invoke($"连接失败：{requirement.IpAddress}:{requirement.Port}，原因={ex.Message}");
                    return MeterTestAddressReadAttempt.Fail(string.Empty, $"连接失败：{ex.Message}");
                }
            }
            else
            {
                trace?.Invoke($"复用工位连接：{requirement.IpAddress}:{requirement.Port}，波特率={baudRate}，测试内容=地址读取");
            }

            NetworkStream stream = session.Stream!;
            byte[] requestBytes = Convert.FromHexString(requestHex.Replace(" ", string.Empty));
            string discardedHex = await DrainBufferedDataAsync(stream, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(discardedHex))
            {
                trace?.Invoke($"{FormatTimestamp()} - 丢弃旧缓存报文：{discardedHex}");
            }

            trace?.Invoke($"{FormatTimestamp()} - 发送报文：{requestHex}");
            await WriteWithRetryAsync(
                    stream,
                    requestBytes,
                    $"地址读取[端点={requirement.IpAddress}:{requirement.Port}, 波特率={baudRate}]",
                    trace,
                    cancellationToken)
                .ConfigureAwait(false);

            using MemoryStream responseBuffer = new();
            try
            {
                MeterTestAddressReadAttempt? parsedAttempt = await ReadAddressResponseAsync(
                    stream,
                    requirement,
                    responseBuffer,
                    trace,
                    cancellationToken);
                if (parsedAttempt is not null)
                {
                    return parsedAttempt;
                }
            }
            catch (IOException ex)
            {
                session.DisposeConnection();
                trace?.Invoke($"工位连接读写异常，已关闭本轮缓存连接：{requirement.IpAddress}:{requirement.Port}，原因={ex.Message}");
                return MeterTestAddressReadAttempt.Fail(string.Empty, ex.Message);
            }
            catch (SocketException ex)
            {
                session.DisposeConnection();
                trace?.Invoke($"工位连接Socket异常，已关闭本轮缓存连接：{requirement.IpAddress}:{requirement.Port}，原因={ex.Message}");
                return MeterTestAddressReadAttempt.Fail(string.Empty, ex.Message);
            }

            string responseHex = GenericSerialPortServerProtocol.ToHexString(responseBuffer.ToArray());
            trace?.Invoke($"{FormatTimestamp()} - 接收报文：{responseHex}");
            if (string.IsNullOrWhiteSpace(responseHex))
            {
                return MeterTestAddressReadAttempt.Fail(string.Empty, "电表无响应。");
            }

            Sgcc698BroadcastAddressParseResult parseResult = ParseAddress(requirement, responseHex);
            return parseResult.IsValid
                ? MeterTestAddressReadAttempt.Success(responseHex, parseResult.MeterAddress, parseResult.Message)
                : MeterTestAddressReadAttempt.Fail(responseHex, parseResult.Message);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    /// <summary>
    /// 等待工位485响应，但不取消底层NetworkStream读取，避免无响应超时把可复用TCP连接打断。
    /// </summary>
    private static async Task<MeterTestAddressReadAttempt?> ReadAddressResponseAsync(
        NetworkStream stream,
        MeterTestCommunicationAddressRequirement requirement,
        MemoryStream responseBuffer,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        byte[] readBuffer = new byte[4096];
        long deadline = Environment.TickCount64 + Math.Max(100, requirement.TimeoutMs);
        while (Environment.TickCount64 < deadline && responseBuffer.Length < MaximumResponseBytes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!stream.DataAvailable)
            {
                await Task.Delay(20, cancellationToken);
                continue;
            }

            int length = await stream.ReadAsync(readBuffer.AsMemory(0, readBuffer.Length), cancellationToken);
            if (length <= 0)
            {
                break;
            }

            responseBuffer.Write(readBuffer, 0, length);
            string currentResponse = GenericSerialPortServerProtocol.ToHexString(responseBuffer.ToArray());
            Sgcc698BroadcastAddressParseResult currentParse = ParseAddress(requirement, currentResponse);
            if (currentParse.IsValid)
            {
                trace?.Invoke($"{FormatTimestamp()} - 接收报文：{currentResponse}");
                return MeterTestAddressReadAttempt.Success(
                    currentResponse,
                    currentParse.MeterAddress,
                    currentParse.Message);
            }
        }

        return null;
    }

    /// <summary>
    /// 发送新地址读取请求前清掉连接里的旧响应，避免备用波特率或后续测试拿到上一帧数据。
    /// </summary>
    private static async Task<string> DrainBufferedDataAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        using MemoryStream discarded = new();
        byte[] buffer = new byte[4096];
        while (stream.DataAvailable && discarded.Length < MaximumResponseBytes)
        {
            int length = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                break;

            discarded.Write(buffer, 0, length);
        }

        return discarded.Length == 0
            ? string.Empty
            : GenericSerialPortServerProtocol.ToHexString(discarded.ToArray());
    }

    /// <summary>构造工位 485 TCP 会话的唯一 IP:Port 键。</summary>
    private static string BuildStationEndpointKey(string ipAddress, int port) => $"{ipAddress.Trim()}:{port}";

    /// <summary>取得或创建本轮测试内可复用的工位 485 TCP 会话。</summary>
    private StationConnectionSession GetOrCreateStationSession(string endpointKey)
    {
        return stationSessions.GetOrAdd(endpointKey, _ => new StationConnectionSession());
    }

    /// <summary>使用方案中的 APDU、OAD、类型和长度约束解析 698 地址读取响应。</summary>
    private static Sgcc698BroadcastAddressParseResult ParseAddress(
        MeterTestCommunicationAddressRequirement requirement,
        string responseHex)
    {
        return SGCCTools.ParseBroadcastAddressResponse(
            responseHex,
            requirement.ExpectedOad,
            requirement.ExpectedApdu,
            requirement.ExpectedDataType,
            requirement.ExpectedDataLength);
    }

    /// <summary>在用户取消令牌和配置超时的共同约束下建立 TCP 连接。</summary>
    private static async Task ConnectWithTimeoutAsync(
        TcpClient client,
        string ipAddress,
        int port,
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Math.Max(100, timeoutMs));
        await client.ConnectAsync(ipAddress, port, timeoutCts.Token);
    }

    /// <summary>校验地址读取所需的 IP、端口和 6 字节电表地址。</summary>
    private static void ValidateRequirement(MeterTestCommunicationAddressRequirement requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement.IpAddress))
            throw new ArgumentException("工位IP不能为空。", nameof(requirement));
        if (requirement.Port is < 1 or > 65535)
            throw new ArgumentOutOfRangeException(nameof(requirement), "工位端口必须在1-65535之间。");
        if (string.IsNullOrWhiteSpace(NormalizeMeterAddress(requirement.MeterAddress)))
            throw new ArgumentException("工位电表地址必须是6字节地址。", nameof(requirement));
    }

    /// <summary>规范化资产波特率文本，供候选项去重和比对。</summary>
    private static string NormalizeSerialProfile(string? profile)
    {
        return (profile ?? string.Empty).Trim().ToUpperInvariant();
    }

    /// <summary>提取并规范化 12 位十六进制电表地址；非法地址返回空文本。</summary>
    private static string NormalizeMeterAddress(string? meterAddress)
    {
        string normalized = new(
            (meterAddress ?? string.Empty)
                .Where(Uri.IsHexDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
        return normalized.Length == 12 ? normalized : string.Empty;
    }

    /// <summary>生成通信测试接口日志使用的毫秒时间戳。</summary>
    private static string FormatTimestamp() =>
        $"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff", CultureInfo.InvariantCulture)}]";

    /// <summary>
    /// 通信测试TCP写入重试。适用于64444管理命令和工位地址读取命令；
    /// 只重试写入失败场景，写入成功后响应读取仍按原流程执行。
    /// </summary>
    private static async Task WriteWithRetryAsync(
        NetworkStream stream,
        byte[] payload,
        string description,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        for (int attempt = 1; attempt <= MaximumSendAttempts; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                string successMessage = $"{FormatTimestamp()} - {description}发送完成：尝试={attempt}/{MaximumSendAttempts}";
                if (attempt > 1)
                {
                    trace?.Invoke(successMessage);
                }

                LogMessage.Debug($"[通信测试TCP接口] {description}发送完成：尝试={attempt}/{MaximumSendAttempts}。");
                return;
            }
            catch (Exception ex) when (attempt < MaximumSendAttempts && ex is IOException or ObjectDisposedException or SocketException or InvalidOperationException)
            {
                lastException = ex;
                trace?.Invoke($"{FormatTimestamp()} - {description}发送失败，准备重试：尝试={attempt}/{MaximumSendAttempts}，原因={ex.Message}");
                LogMessage.Error($"[通信测试TCP接口] {description}发送失败，准备重试：尝试={attempt}/{MaximumSendAttempts}。", ex);
                await Task.Delay(SendRetryDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        throw new IOException($"{description}发送失败且重试耗尽。", lastException);
    }

    /// <summary>单一波特率下的一次 698 地址读取及解析结果。</summary>
    private sealed record MeterTestAddressReadAttempt(
        bool AddressParsed,
        string ResponseHex,
        string ReturnedAddress,
        string ParseMessage)
    {
        /// <summary>创建成功解析出电表地址的读取结果。</summary>
        public static MeterTestAddressReadAttempt Success(string responseHex, string returnedAddress, string message) =>
            new(true, responseHex, returnedAddress, message);

        /// <summary>创建无响应或响应解析失败的读取结果。</summary>
        public static MeterTestAddressReadAttempt Fail(string responseHex, string message) =>
            new(false, responseHex, string.Empty, message);
    }

    /// <summary>通用串口服务器波特率设置的执行结果及是否应停止后续候选尝试。</summary>
    public sealed record MeterTestSerialPortSettingResult(
        bool Succeeded,
        bool StopFurtherAttempts,
        string Message);

    /// <summary>一轮测试内按IP复用的串口服务器64444管理连接。</summary>
    private sealed class ManagementConnectionSession : IDisposable
    {
        /// <summary>保证同一 IP 的 64444 解锁和设置命令严格串行。</summary>
        public SemaphoreSlim Gate { get; } = new(1, 1);
        /// <summary>当前管理端 TCP 客户端。</summary>
        public TcpClient? Client { get; private set; }
        /// <summary>当前管理端网络流。</summary>
        public NetworkStream? Stream { get; private set; }
        /// <summary>标记当前 IP 管理端在本轮是否已确定不可用。</summary>
        public bool IsUnavailable { get; private set; }
        /// <summary>管理端首次失败原因。</summary>
        public string FailureMessage { get; private set; } = string.Empty;
        /// <summary>判断管理端连接是否可复用。</summary>
        public bool IsConnected => !IsUnavailable && Client?.Connected == true && Stream is not null;

        /// <summary>接管新建管理端客户端并清除旧失败状态。</summary>
        public void Attach(TcpClient client)
        {
            DisposeConnection();
            Client = client;
            Stream = client.GetStream();
            IsUnavailable = false;
            FailureMessage = string.Empty;
        }

        /// <summary>记录失败并关闭连接，避免相同 IP 在本轮重复连接 64444。</summary>
        public void MarkUnavailable(string message)
        {
            FailureMessage = message;
            IsUnavailable = true;
            DisposeConnection();
        }

        /// <summary>释放管理端连接和串行锁。</summary>
        public void Dispose()
        {
            DisposeConnection();
            Gate.Dispose();
        }

        /// <summary>只释放当前网络连接并保留会话锁。</summary>
        private void DisposeConnection()
        {
            Stream?.Dispose();
            Client?.Dispose();
            Stream = null;
            Client = null;
        }
    }

    /// <summary>一轮测试内按工位IP:Port复用的485地址读取连接。</summary>
    private sealed class StationConnectionSession : IDisposable
    {
        /// <summary>保证同一工位端点的地址读取请求严格串行。</summary>
        public SemaphoreSlim Gate { get; } = new(1, 1);
        /// <summary>当前工位 485 TCP 客户端。</summary>
        public TcpClient? Client { get; private set; }
        /// <summary>当前工位网络流。</summary>
        public NetworkStream? Stream { get; private set; }
        /// <summary>判断工位连接是否可供下一波特率或后续试验复用。</summary>
        public bool IsConnected => Client?.Connected == true && Stream is not null;

        /// <summary>接管新建工位客户端及其网络流。</summary>
        public void Attach(TcpClient client)
        {
            DisposeConnection();
            Client = client;
            Stream = client.GetStream();
        }

        /// <summary>释放工位连接和串行锁。</summary>
        public void Dispose()
        {
            DisposeConnection();
            Gate.Dispose();
        }

        /// <summary>仅关闭当前工位连接；会话对象仍可在下一次请求重建连接。</summary>
        public void DisposeConnection()
        {
            Stream?.Dispose();
            Client?.Dispose();
            Stream = null;
            Client = null;
        }
    }
}

/// <summary>单工位地址读取所需的资产、通信端点和698解析参数。</summary>
public sealed record MeterTestCommunicationAddressRequirement(
    int StationNo,
    string IpAddress,
    int Port,
    string MeterAddress,
    string DefaultBaudRate,
    int TimeoutMs,
    string ExpectedOad,
    string ExpectedApdu,
    string ExpectedDataType,
    int ExpectedDataLength,
    bool TryAssetBaudRateFirst = true);

/// <summary>多波特率地址读取的最终结果。</summary>
public sealed record MeterTestCommunicationAddressResult(
    bool AddressParsed,
    bool Passed,
    string ResponseHex,
    string ReturnedAddress,
    string SuccessfulBaudRate,
    string Message);
