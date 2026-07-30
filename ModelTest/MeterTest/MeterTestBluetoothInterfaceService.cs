using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Sockets;
using ModelTest.Protocol;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 国网智芯蓝牙接口检测服务。
/// 每个工位使用方案配置中的蓝牙专用 IP/Port；波特率设置通过同IP的64444管理端并与通信测试共用，
/// 同一轮测试的蓝牙工位步骤复用同一条TCP连接，
/// 不复用资产信息中的485端点、StationTcp、控制PCB或其它连接池。
/// </summary>
public sealed class MeterTestBluetoothInterfaceService
{
    private static readonly TimeSpan PreprocessPollInterval = TimeSpan.FromSeconds(2);
    private readonly ConcurrentDictionary<string, BluetoothConnectionSession> connectionSessions =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly MeterTestCommunicationAddressService communicationAddressService;

    /// <summary>
    /// 创建蓝牙接口检测服务。
    /// </summary>
    /// <param name="communicationAddressService">
    /// 共享的串口服务器管理服务；蓝牙通道波特率设置与通信测试共用同一 IP 的 64444 会话。
    /// </param>
    public MeterTestBluetoothInterfaceService(
        MeterTestCommunicationAddressService communicationAddressService)
    {
        this.communicationAddressService = communicationAddressService;
    }

    /// <summary>开始一轮测试，清除上轮蓝牙连接和失败缓存，并记录会话生命周期日志。</summary>
    public void BeginRun()
    {
        EndRun();
        LogMessage.Debug("[蓝牙接口] 新测试批次开始，已清理上轮蓝牙专用TCP会话。");
    }

    /// <summary>结束一轮测试，关闭所有工位蓝牙TCP连接。</summary>
    public void EndRun()
    {
        int sessionCount = connectionSessions.Count;
        foreach (BluetoothConnectionSession session in connectionSessions.Values)
        {
            session.Dispose();
        }

        connectionSessions.Clear();
        LogMessage.Debug($"[蓝牙接口] 测试批次结束，已释放蓝牙专用TCP会话={sessionCount}条。");
    }

    /// <summary>
    /// 从方案配置解析每个工位唯一的蓝牙专用 TCP 通道，然后并发执行当前蓝牙测试步骤。
    /// 缺失、重复、禁用或非法端点会作为当前工位失败结果返回，绝不回退使用资产 485 端点。
    /// </summary>
    internal Task<IReadOnlyDictionary<int, MeterTestBluetoothStationResult>> ExecuteConfiguredStepAsync(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<StationCommunicationConfig> stations,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(planConfig);
        List<MeterTestBluetoothStation> bluetoothStations = stations
            .Select(station => CreateBluetoothStation(planConfig, station))
            .ToList();
        LogMessage.Debug(
            $"[蓝牙接口] 开始步骤：名称={subItem.Name}，步骤={subItem.BluetoothStep}，"
            + $"工位={string.Join(',', bluetoothStations.Select(station => station.StationNo))}，"
            + $"端点={string.Join(';', bluetoothStations.Select(station => $"工位{station.StationNo}={station.Ip}:{station.Port}"))}。");
        return ExecuteStepAsync(subItem, bluetoothStations, stationLogger, cancellationToken);
    }

    /// <summary>并发执行当前蓝牙小项，一个工位失败不中断其它工位。</summary>
    public async Task<IReadOnlyDictionary<int, MeterTestBluetoothStationResult>> ExecuteStepAsync(
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestBluetoothStation> stations,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        Task<MeterTestBluetoothStationResult>[] tasks = stations
            .Select(station => ExecuteStationStepSafelyAsync(subItem, station, stationLogger, cancellationToken))
            .ToArray();
        MeterTestBluetoothStationResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        LogMessage.Debug(
            $"[蓝牙接口] 步骤完成：名称={subItem.Name}，成功={results.Count(result => result.Success)}/{results.Length}，"
            + $"失败工位={string.Join(',', results.Where(result => !result.Success).Select(result => result.StationNo))}。");
        return results.ToDictionary(result => result.StationNo);
    }

    /// <summary>
    /// 将工位资产地址与方案中的 BluetoothTcpChannel 唯一映射合并为蓝牙执行对象。
    /// </summary>
    private static MeterTestBluetoothStation CreateBluetoothStation(
        MeterTestPlanConfig planConfig,
        StationCommunicationConfig station)
    {
        List<MeterTestBluetoothTcpChannel> matches = planConfig.BluetoothTcpChannels
            .Where(channel => channel.Station == station.StationNo)
            .ToList();
        if (matches.Count == 0)
        {
            return new MeterTestBluetoothStation(
                station.StationNo,
                string.Empty,
                0,
                station.MeterAddress,
                $"工位{station.StationNo}未配置蓝牙专用TCP通道，请维护BluetoothTcpChannels。");
        }

        if (matches.Count > 1)
        {
            return new MeterTestBluetoothStation(
                station.StationNo,
                string.Empty,
                0,
                station.MeterAddress,
                $"工位{station.StationNo}存在{matches.Count}条蓝牙TCP配置，请保留唯一映射。");
        }

        MeterTestBluetoothTcpChannel channel = matches[0];
        string ip = channel.Ip.Trim();
        string configurationError = !channel.Enabled
            ? $"工位{station.StationNo}的蓝牙专用TCP通道未启用。"
            : string.IsNullOrWhiteSpace(ip) || channel.Port is < 1 or > 65535
                ? $"工位{station.StationNo}的蓝牙专用TCP端点无效：{ip}:{channel.Port}。"
                : string.Empty;
        return new MeterTestBluetoothStation(
            station.StationNo,
            ip,
            channel.Port,
            station.MeterAddress,
            configurationError);
    }

    /// <summary>包装单工位执行异常，确保一个工位失败不会中断其它工位。</summary>
    private async Task<MeterTestBluetoothStationResult> ExecuteStationStepSafelyAsync(
        MeterTestSubItem subItem,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteStationStepAsync(
                subItem,
                station,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            string message = $"蓝牙步骤{subItem.Name}等待超时，超时={subItem.TimeoutMs}ms。";
            Trace(station.StationNo, message, stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, message);
        }
        catch (Exception ex)
        {
            string message = $"蓝牙步骤{subItem.Name}执行异常：{ex.Message}";
            Trace(station.StationNo, message, stationLogger);
            LogMessage.Error($"[蓝牙接口][工位{station.StationNo}] {message}", ex);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, message);
        }
    }

    /// <summary>执行单个工位的蓝牙步骤，并在本轮内复用其专用 IP:Port TCP 会话。</summary>
    private async Task<MeterTestBluetoothStationResult> ExecuteStationStepAsync(
        MeterTestSubItem subItem,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(station.ConfigurationError))
        {
            Trace(station.StationNo, station.ConfigurationError, stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, station.ConfigurationError);
        }

        if (string.IsNullOrWhiteSpace(station.Ip) || station.Port is < 1 or > 65535)
            return MeterTestBluetoothStationResult.Fail(station.StationNo, $"蓝牙专用TCP端点无效：{station.Ip}:{station.Port}。");

        if (!Enum.TryParse(subItem.BluetoothStep, true, out MeterTestBluetoothStep step))
            return MeterTestBluetoothStationResult.Fail(station.StationNo, $"蓝牙流程步骤不支持：{subItem.BluetoothStep}。");

        // 波特率设置步骤使用同一IP的64444管理端，不建立蓝牙工位端口连接。
        if (step == MeterTestBluetoothStep.SetBaudRate)
        {
            return await ExecuteSetBaudRateAsync(
                subItem,
                station,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
        }

        string endpointKey = $"{station.Ip.Trim()}:{station.Port}";
        BluetoothConnectionSession session = connectionSessions.GetOrAdd(
            endpointKey,
            _ => new BluetoothConnectionSession());
        await session.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (session.IsUnavailable)
            {
                string unavailableMessage =
                    $"蓝牙TCP端点 {endpointKey} 本轮已连接失败，不再重复连接：{session.FailureMessage}";
                Trace(station.StationNo, unavailableMessage, stationLogger);
                return MeterTestBluetoothStationResult.Fail(station.StationNo, unavailableMessage);
            }

            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Math.Max(100, subItem.TimeoutMs));
            if (!session.IsConnected)
            {
                Trace(
                    station.StationNo,
                    $"准备首次建立工位专用蓝牙TCP连接：{endpointKey}，步骤={subItem.Name}。",
                    stationLogger);
                TcpClient client = new();
                try
                {
                    await client.ConnectAsync(station.Ip.Trim(), station.Port, timeoutCts.Token).ConfigureAwait(false);
                    session.Attach(client);
                    Trace(
                        station.StationNo,
                        $"蓝牙TCP首次连接成功：{endpointKey}，后续蓝牙步骤复用该连接。",
                        stationLogger);
                }
                catch
                {
                    client.Dispose();
                    throw;
                }
            }
            else
            {
                Trace(station.StationNo, $"复用工位蓝牙TCP连接：{endpointKey}，步骤={subItem.Name}。", stationLogger);
            }

            NetworkStream stream = session.Stream!;
            return step switch
            {
                MeterTestBluetoothStep.Reset => await ExecuteSimpleCommandAsync(
                    stream,
                    station,
                    subItem.Name,
                    BluetoothConverterFunction.Reset,
                    SgccBluetoothConverterProtocol.BuildResetFrame(),
                    stationLogger,
                    timeoutCts.Token).ConfigureAwait(false),
                MeterTestBluetoothStep.ConnectMeter => await ExecuteConnectMeterAsync(
                    stream,
                    station,
                    stationLogger,
                    timeoutCts.Token).ConfigureAwait(false),
                MeterTestBluetoothStep.Preprocess => await ExecutePreprocessAsync(
                    stream,
                    station,
                    stationLogger,
                    timeoutCts.Token).ConfigureAwait(false),
                MeterTestBluetoothStep.ReadAddress => await ExecuteReadAddressAsync(
                    stream,
                    station,
                    subItem,
                    stationLogger,
                    timeoutCts.Token).ConfigureAwait(false),
                _ => MeterTestBluetoothStationResult.Fail(station.StationNo, $"蓝牙流程步骤未实现：{step}。")
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            session.MarkUnavailable("测试已取消，蓝牙连接已关闭。");
            throw;
        }
        catch (Exception ex)
        {
            session.MarkUnavailable(ex.Message);
            throw;
        }
        finally
        {
            session.Gate.Release();
        }
    }

    /// <summary>通过共享的64444管理连接，把当前BluetoothTcpChannel端口设置为9600-8-E-1。</summary>
    private async Task<MeterTestBluetoothStationResult> ExecuteSetBaudRateAsync(
        MeterTestSubItem subItem,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        const string targetProfile = "9600-8-E-1";
        Trace(
            station.StationNo,
            $"开始检查蓝牙通道波特率：{station.Ip}:{station.Port}，目标={targetProfile}，管理端={station.Ip}:64444。",
            stationLogger);

        MeterTestCommunicationAddressService.MeterTestSerialPortSettingResult result =
            await communicationAddressService.ApplySerialPortBaudRateAsync(
                station.Ip,
                station.Port,
                targetProfile,
                subItem.TimeoutMs,
                message => Trace(station.StationNo, message, stationLogger),
                cancellationToken).ConfigureAwait(false);

        string message = result.Succeeded
            ? $"蓝牙通道端口{station.Port}已设置为{targetProfile}：{result.Message}"
            : $"蓝牙通道端口{station.Port}波特率设置失败：{result.Message}";
        Trace(station.StationNo, message, stationLogger);
        return result.Succeeded
            ? MeterTestBluetoothStationResult.Pass(station.StationNo, message)
            : MeterTestBluetoothStationResult.Fail(station.StationNo, message);
    }

    /// <summary>按 6 字节低字节在前的 BCD 地址发送 0x01 连接电表命令。</summary>
    private static async Task<MeterTestBluetoothStationResult> ExecuteConnectMeterAsync(
        NetworkStream stream,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMeterAddress(station.MeterAddress, out string meterAddress))
        {
            string message = $"工位{station.StationNo}电表地址必须是12位BCD数字：{station.MeterAddress}。";
            Trace(station.StationNo, message, stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, message);
        }

        byte[] request = SgccBluetoothConverterProtocol.BuildConnectMeterFrame(meterAddress);
        return await ExecuteSimpleCommandAsync(
            stream,
            station,
            $"自动连接电表，地址={meterAddress}",
            BluetoothConverterFunction.ConnectMeter,
            request,
            stationLogger,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>发送 0x07 预处理并轮询 0x08，直到返回成功、失败或外层超时。</summary>
    private static async Task<MeterTestBluetoothStationResult> ExecutePreprocessAsync(
        NetworkStream stream,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        MeterTestBluetoothStationResult startResult = await ExecuteSimpleCommandAsync(
            stream,
            station,
            "启动蓝牙检定预处理",
            BluetoothConverterFunction.Preprocess,
            SgccBluetoothConverterProtocol.BuildPreprocessFrame(),
            stationLogger,
            cancellationToken).ConfigureAwait(false);
        if (!startResult.Success)
            return startResult;

        int queryIndex = 0;
        while (true)
        {
            await Task.Delay(PreprocessPollInterval, cancellationToken).ConfigureAwait(false);
            queryIndex++;
            byte[] request = SgccBluetoothConverterProtocol.BuildQueryPreprocessStatusFrame();
            byte[] response = await SendAndReceiveBluetoothFrameAsync(
                stream,
                station.StationNo,
                $"查询蓝牙检定预处理状态-第{queryIndex}次",
                request,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
            if (!SgccBluetoothConverterProtocol.TryParsePreprocessStatus(response, out BluetoothPreprocessStatus status, out string statusMessage))
            {
                Trace(station.StationNo, $"预处理状态应答解析失败：{statusMessage}", stationLogger);
                return MeterTestBluetoothStationResult.Fail(station.StationNo, statusMessage);
            }

            Trace(station.StationNo, $"第{queryIndex}次预处理状态：{statusMessage}", stationLogger);
            if (status == BluetoothPreprocessStatus.Succeeded)
                return MeterTestBluetoothStationResult.Pass(station.StationNo, statusMessage);
            if (status == BluetoothPreprocessStatus.Failed)
                return MeterTestBluetoothStationResult.Fail(station.StationNo, statusMessage);
        }
    }

    /// <summary>通过蓝牙通道发送 698 OAD=40010200 地址读取报文并比对资产地址。</summary>
    private static async Task<MeterTestBluetoothStationResult> ExecuteReadAddressAsync(
        NetworkStream stream,
        MeterTestBluetoothStation station,
        MeterTestSubItem subItem,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMeterAddress(station.MeterAddress, out string expectedAddress))
        {
            string validationMessage = $"工位{station.StationNo}电表地址必须是12位BCD数字：{station.MeterAddress}。";
            Trace(station.StationNo, validationMessage, stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, validationMessage);
        }

        string requestHex = SGCCTools.BuildMeterAddressReadRequest(expectedAddress);
        byte[] request = ParseHex(requestHex);
        Trace(
            station.StationNo,
            $"{FormatTimestamp()} - 发送698报文：{SgccBluetoothConverterProtocol.ToHexString(request)}，OAD=40010200。",
            stationLogger);
        await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        byte[] response = await Read698FrameAsync(stream, cancellationToken).ConfigureAwait(false);
        string responseHex = SgccBluetoothConverterProtocol.ToHexString(response);
        Trace(station.StationNo, $"{FormatTimestamp()} - 接收698报文：{responseHex}", stationLogger);

        Sgcc698BroadcastAddressParseResult parseResult = SGCCTools.ParseBroadcastAddressResponse(
            responseHex,
            subItem.ExpectedOad,
            subItem.ExpectedApdu,
            subItem.ExpectedDataType,
            subItem.ExpectedDataLength);
        string actualAddress = NormalizeAddress(parseResult.MeterAddress);
        bool passed = parseResult.IsValid && expectedAddress.Equals(actualAddress, StringComparison.OrdinalIgnoreCase);
        string message = parseResult.IsValid
            ? $"实际地址={expectedAddress}，返回地址={actualAddress}，结论={(passed ? "合格" : "不合格")}。"
            : $"OAD=40010200应答解析失败：{parseResult.Message}";
        Trace(station.StationNo, message, stationLogger);
        return passed
            ? MeterTestBluetoothStationResult.Pass(station.StationNo, message, actualAddress)
            : MeterTestBluetoothStationResult.Fail(station.StationNo, message, actualAddress);
    }

    /// <summary>发送一个转换器命令并按功能码最高位置 1 的应答规则解析结果码。</summary>
    private static async Task<MeterTestBluetoothStationResult> ExecuteSimpleCommandAsync(
        NetworkStream stream,
        MeterTestBluetoothStation station,
        string description,
        BluetoothConverterFunction function,
        byte[] request,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        byte[] response = await SendAndReceiveBluetoothFrameAsync(
            stream,
            station.StationNo,
            description,
            request,
            stationLogger,
            cancellationToken).ConfigureAwait(false);
        if (!SgccBluetoothConverterProtocol.TryParseCommandResult(
                response,
                function,
                out bool success,
                out byte resultCode,
                out string message))
        {
            Trace(station.StationNo, $"{description}应答解析失败：{message}", stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, message);
        }

        string resultMessage = $"{description}：{message}结果码={resultCode:X2}。";
        Trace(station.StationNo, resultMessage, stationLogger);
        return success
            ? MeterTestBluetoothStationResult.Pass(station.StationNo, resultMessage)
            : MeterTestBluetoothStationResult.Fail(station.StationNo, resultMessage);
    }

    /// <summary>发送完整蓝牙转换器帧并持续收取数据，直到提取一条校验有效的协议帧。</summary>
    private static async Task<byte[]> SendAndReceiveBluetoothFrameAsync(
        NetworkStream stream,
        int stationNo,
        string description,
        byte[] request,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        Trace(
            stationNo,
            $"{FormatTimestamp()} - 发送报文：{SgccBluetoothConverterProtocol.ToHexString(request)}，{description}。",
            stationLogger);
        await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        List<byte> receiveBuffer = new();
        byte[] buffer = new byte[1024];
        while (true)
        {
            int length = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                throw new IOException("蓝牙TCP连接已关闭。");

            receiveBuffer.AddRange(buffer.AsSpan(0, length).ToArray());
            if (!SgccBluetoothConverterProtocol.TryTakeFrame(receiveBuffer, out byte[]? frame))
                continue;

            Trace(
                stationNo,
                $"{FormatTimestamp()} - 接收报文：{SgccBluetoothConverterProtocol.ToHexString(frame!)}。",
                stationLogger);
            return frame!;
        }
    }

    /// <summary>从蓝牙 TCP 字节流中按 698 长度域提取一帧，并保留其前导 FE。</summary>
    private static async Task<byte[]> Read698FrameAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        List<byte> receiveBuffer = new();
        byte[] buffer = new byte[4096];
        while (true)
        {
            int length = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                throw new IOException("蓝牙TCP连接在698应答返回前已关闭。");

            receiveBuffer.AddRange(buffer.AsSpan(0, length).ToArray());
            int startIndex = receiveBuffer.FindIndex(value => value == 0x68);
            if (startIndex < 0)
                continue;
            if (receiveBuffer.Count < startIndex + 3)
                continue;

            int declaredLength = receiveBuffer[startIndex + 1] | (receiveBuffer[startIndex + 2] << 8);
            int totalLength = declaredLength + 2;
            if (declaredLength < 1 || receiveBuffer.Count < startIndex + totalLength)
                continue;

            // 保留正式698帧前的FE前导符，统一交给SGCCTools剔除和记录。
            int preambleStart = startIndex;
            while (preambleStart > 0 && receiveBuffer[preambleStart - 1] == 0xFE)
                preambleStart--;
            int frameLength = startIndex + totalLength - preambleStart;
            return receiveBuffer.Skip(preambleStart).Take(frameLength).ToArray();
        }
    }

    /// <summary>将 698 HEX 文本转换为发送字节；非法或奇数长度文本直接抛出格式异常。</summary>
    private static byte[] ParseHex(string value)
    {
        string normalized = new(value.Where(Uri.IsHexDigit).ToArray());
        if (normalized.Length == 0 || normalized.Length % 2 != 0)
            throw new FormatException("698请求报文不是合法HEX。");

        return Enumerable.Range(0, normalized.Length / 2)
            .Select(index => byte.Parse(normalized.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ToArray();
    }

    /// <summary>将电表地址规范为 12 位 BCD 数字并返回是否合法。</summary>
    private static bool TryNormalizeMeterAddress(string value, out string address)
    {
        address = NormalizeAddress(value);
        return address.Length == 12 && address.All(char.IsDigit);
    }

    /// <summary>去除地址分隔符并转换为大写连续文本。</summary>
    private static string NormalizeAddress(string value) =>
        new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    /// <summary>同时写入全局日志和工位测试日志，保证接口过程可双向追溯。</summary>
    private static void Trace(int stationNo, string message, Action<int, string>? stationLogger)
    {
        LogMessage.Debug($"[蓝牙接口][工位{stationNo}] {message}");
        stationLogger?.Invoke(stationNo, message);
    }

    /// <summary>生成蓝牙接口收发日志使用的毫秒时间戳。</summary>
    private static string FormatTimestamp() => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";

    /// <summary>一轮测试内按蓝牙专用IP:Port复用的TCP会话。</summary>
    private sealed class BluetoothConnectionSession : IDisposable
    {
        /// <summary>保证同一蓝牙端点的请求严格串行，避免多步骤报文交叉。</summary>
        public SemaphoreSlim Gate { get; } = new(1, 1);

        /// <summary>当前端点已经建立的 TCP 客户端。</summary>
        public TcpClient? Client { get; private set; }

        /// <summary>当前 TCP 客户端对应的网络流。</summary>
        public NetworkStream? Stream { get; private set; }

        /// <summary>指示该端点是否已在本轮被判定为不可用。</summary>
        public bool IsUnavailable { get; private set; }

        /// <summary>端点不可用时保存首次失败原因，阻止后续步骤重复攻击式重连。</summary>
        public string FailureMessage { get; private set; } = string.Empty;

        /// <summary>判断会话是否可直接供当前蓝牙步骤复用。</summary>
        public bool IsConnected => !IsUnavailable && Client?.Connected == true && Stream is not null;

        /// <summary>接管新建 TCP 客户端，并初始化可复用网络流和会话状态。</summary>
        public void Attach(TcpClient client)
        {
            DisposeConnection();
            Client = client;
            Stream = client.GetStream();
            IsUnavailable = false;
            FailureMessage = string.Empty;
        }

        /// <summary>记录不可用原因并立即关闭连接，后续步骤直接返回同一失败信息。</summary>
        public void MarkUnavailable(string message)
        {
            FailureMessage = message;
            IsUnavailable = true;
            DisposeConnection();
        }

        /// <summary>释放当前 TCP 连接及端点串行锁。</summary>
        public void Dispose()
        {
            DisposeConnection();
            Gate.Dispose();
        }

        /// <summary>只释放网络连接，保留并发锁供当前会话生命周期继续使用。</summary>
        private void DisposeConnection()
        {
            Stream?.Dispose();
            Client?.Dispose();
            Stream = null;
            Client = null;
        }
    }
}

/// <summary>方案 XML 可配置的蓝牙接口检测步骤。</summary>
public enum MeterTestBluetoothStep
{
    /// <summary>通过同 IP 的 64444 管理端将蓝牙通道设置为 9600-8-E-1。</summary>
    SetBaudRate,

    /// <summary>发送 0x00 复位蓝牙转换器。</summary>
    Reset,

    /// <summary>发送 0x01，按资产电表地址连接待测电表。</summary>
    ConnectMeter,

    /// <summary>发送 0x07 并轮询 0x08 完成蓝牙检定预处理。</summary>
    Preprocess,

    /// <summary>通过蓝牙透传 698 地址读取报文并与资产地址比对。</summary>
    ReadAddress
}

/// <summary>蓝牙检测使用的工位专用TCP端点、电表地址及配置校验结果。</summary>
public sealed record MeterTestBluetoothStation(
    int StationNo,
    string Ip,
    int Port,
    string MeterAddress,
    string ConfigurationError = "");

/// <summary>单个工位蓝牙小项执行结果。</summary>
public sealed record MeterTestBluetoothStationResult(
    int StationNo,
    bool Success,
    string Message,
    string MeterAddress)
{
    /// <summary>创建单工位蓝牙步骤成功结果。</summary>
    public static MeterTestBluetoothStationResult Pass(int stationNo, string message, string meterAddress = "") =>
        new(stationNo, true, message, meterAddress);

    /// <summary>创建单工位蓝牙步骤失败结果，并保留可选的响应地址。</summary>
    public static MeterTestBluetoothStationResult Fail(int stationNo, string message, string meterAddress = "") =>
        new(stationNo, false, message, meterAddress);
}
