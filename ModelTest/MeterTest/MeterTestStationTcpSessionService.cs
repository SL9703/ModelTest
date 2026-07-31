using System.Collections.Concurrent;
using System.Net.Sockets;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 工位 485 TCP 长连接会话服务。
///
/// 本类统一管理按 IP:Port 去重的 TcpClient、同连接串行收发、请求前旧缓存清理、
/// 普通响应读取，以及 698 电量响应按完整帧、PIID、OAD 过滤的流程。
/// 测试服务只提交请求和解析条件，不再直接操作 TcpClient 或 NetworkStream。
/// </summary>
internal sealed class MeterTestStationTcpSessionService : IDisposable
{
    private const int MaximumSendAttempts = 4;
    private static readonly TimeSpan SendRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly ConcurrentDictionary<string, StationTcpConnectionHolder> connections =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>开始新一轮测试；先释放上一轮遗留连接，保证不同运行批次数据隔离。</summary>
    public void BeginRun()
    {
        EndRun();
        LogMessage.Debug("[工位TCP接口] 新测试批次开始，已清理上一轮工位TCP连接和缓存。");
    }

    /// <summary>结束测试并释放全部工位 TCP 连接。</summary>
    public void EndRun()
    {
        foreach (StationTcpConnectionHolder holder in connections.Values)
        {
            holder.Dispose();
        }

        connections.Clear();
        LogMessage.Debug("[工位TCP接口] 测试批次结束，全部工位TCP长连接已释放。");
    }

    /// <summary>
    /// 复用工位连接发送普通 HEX 请求并返回收到的响应字节。
    /// 接口日志包含连接、旧缓存、完整发送报文和完整接收报文。
    /// </summary>
    public async Task<string> SendRequestAsync(
        StationCommunicationConfig station,
        string requestHex,
        string requestDescription,
        int timeoutMs,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        byte[] requestBytes = ParseHexBytes(requestHex);
        if (requestBytes.Length == 0)
            throw new InvalidOperationException("请求报文为空或不是合法HEX。");

        StationTcpConnectionHolder holder = GetHolder(station);
        await holder.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            NetworkStream stream = await EnsureConnectedAsync(
                holder,
                station,
                timeoutMs,
                trace,
                cancellationToken).ConfigureAwait(false);
            using CancellationTokenSource timeoutCts = CreateTimeoutToken(timeoutMs, cancellationToken);
            string discarded = await DrainBufferedDataAsync(stream, timeoutCts.Token).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(discarded))
            {
                Trace(trace, $"{FormatTimestamp()} - [工位TCP接口] 丢弃旧缓存报文：{discarded}");
            }

            Trace(
                trace,
                $"{FormatTimestamp()} - [工位TCP接口][PC-->Meter] 端点={station.Ip}:{station.Port}，"
                + $"工位={station.StationNo}，说明={requestDescription}，报文={NormalizeHex(requestHex)}");
            await WriteWithRetryAsync(
                    stream,
                    requestBytes,
                    $"[工位TCP接口][工位{station.StationNo}] {requestDescription}",
                    trace,
                    timeoutCts.Token)
                .ConfigureAwait(false);
            string responseHex = await ReadResponseHexAsync(stream, timeoutCts.Token).ConfigureAwait(false);
            Trace(
                trace,
                $"{FormatTimestamp()} - [工位TCP接口][Meter-->PC] 端点={station.Ip}:{station.Port}，"
                + $"工位={station.StationNo}，报文={responseHex}");
            return responseHex;
        }
        catch (OperationCanceledException ex)
        {
            string reason = cancellationToken.IsCancellationRequested
                ? "测试被用户取消"
                : $"接口等待超时({Math.Max(100, timeoutMs)}ms)";
            Trace(
                trace,
                $"{FormatTimestamp()} - [工位TCP接口] 请求失败：端点={station.Ip}:{station.Port}，"
                + $"工位={station.StationNo}，说明={requestDescription}，原因={reason}，"
                + $"下行报文={NormalizeHex(requestHex)}");
            LogMessage.Error(
                $"[工位TCP接口][工位{station.StationNo}] {reason}：端点={station.Ip}:{station.Port}，"
                + $"说明={requestDescription}。",
                ex);
            ResetHolder(holder);
            throw;
        }
        catch (Exception ex)
        {
            Trace(
                trace,
                $"{FormatTimestamp()} - [工位TCP接口] 请求异常：端点={station.Ip}:{station.Port}，"
                + $"工位={station.StationNo}，说明={requestDescription}，异常={ex.Message}，"
                + $"下行报文={NormalizeHex(requestHex)}");
            LogMessage.Error(
                $"[工位TCP接口][工位{station.StationNo}] 接口异常：端点={station.Ip}:{station.Port}，"
                + $"说明={requestDescription}。",
                ex);
            ResetHolder(holder);
            throw;
        }
        finally
        {
            holder.Gate.Release();
        }
    }

    /// <summary>
    /// 发送 698 正向有功总电能读取请求，并持续读取完整 698 帧，直到匹配本次 PIID/OAD。
    /// 串口服务器延迟转发的旧响应会记录后丢弃，不会误当成本步骤结果。
    /// </summary>
    public async Task<EnergyReadResponse> SendPositiveActiveEnergyReadAsync(
        StationCommunicationConfig station,
        string requestHex,
        string expectedPiid,
        string requestDescription,
        int timeoutMs,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        byte[] requestBytes = ParseHexBytes(requestHex);
        if (requestBytes.Length == 0)
        {
            return new EnergyReadResponse(
                string.Empty,
                Sgcc698EnergyReadParseResult.Fail("正向有功总电能请求为空或不是合法HEX。"));
        }

        StationTcpConnectionHolder holder = GetHolder(station);
        await holder.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            NetworkStream stream = await EnsureConnectedAsync(
                holder,
                station,
                timeoutMs,
                trace,
                cancellationToken).ConfigureAwait(false);
            using CancellationTokenSource timeoutCts = CreateTimeoutToken(timeoutMs, cancellationToken);
            string discarded = await DrainBufferedDataAsync(stream, timeoutCts.Token).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(discarded))
            {
                Trace(trace, $"{FormatTimestamp()} - [工位TCP接口] 丢弃旧缓存报文：{discarded}");
            }

            Trace(
                trace,
                $"{FormatTimestamp()} - [工位TCP接口][PC-->Meter] 端点={station.Ip}:{station.Port}，"
                + $"工位={station.StationNo}，PIID={expectedPiid}，说明={requestDescription}，"
                + $"报文={NormalizeHex(requestHex)}");
            await WriteWithRetryAsync(
                    stream,
                    requestBytes,
                    $"[工位TCP接口][工位{station.StationNo}] {requestDescription}",
                    trace,
                    timeoutCts.Token)
                .ConfigureAwait(false);

            while (true)
            {
                string responseHex = await ReadResponseHexAsync(stream, timeoutCts.Token).ConfigureAwait(false);
                Trace(
                    trace,
                    $"{FormatTimestamp()} - [工位TCP接口][Meter-->PC] 端点={station.Ip}:{station.Port}，"
                    + $"工位={station.StationNo}，原始报文={responseHex}");
                IReadOnlyList<string> frames = SGCCTools.ExtractSgcc698Frames(responseHex);
                foreach (string frameHex in frames)
                {
                    Sgcc698EnergyReadParseResult parseResult = SGCCTools.ParsePositiveActiveEnergyResponse(
                        frameHex,
                        station.MeterAddress,
                        expectedPiid);
                    Trace(
                        trace,
                        $"[工位TCP接口][698解析] 工位={station.StationNo}，PIID={expectedPiid}，"
                        + $"帧={frameHex}，结果={(parseResult.IsValid ? "匹配" : "不匹配")}，说明={parseResult.Message}");
                    if (parseResult.IsValid)
                        return new EnergyReadResponse(frameHex, parseResult);

                    if (IsNonCurrentEnergyReadFrame(parseResult))
                    {
                        Trace(trace, $"[工位TCP接口] 丢弃非当前电量读取响应：{parseResult.Message}");
                        continue;
                    }

                    return new EnergyReadResponse(frameHex, parseResult);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            string message = $"等待PIID={expectedPiid}的正向有功电能响应超时({Math.Max(100, timeoutMs)}ms)。";
            Trace(
                trace,
                $"{FormatTimestamp()} - [工位TCP接口] {message} 端点={station.Ip}:{station.Port}，"
                + $"工位={station.StationNo}，说明={requestDescription}，下行报文={NormalizeHex(requestHex)}");
            LogMessage.Error(
                $"[工位TCP接口][工位{station.StationNo}] {message} 端点={station.Ip}:{station.Port}。",
                null);
            return new EnergyReadResponse(string.Empty, Sgcc698EnergyReadParseResult.Fail(message));
        }
        catch (OperationCanceledException ex)
        {
            string message = $"正向有功电能读取被取消，PIID={expectedPiid}。";
            Trace(
                trace,
                $"{FormatTimestamp()} - [工位TCP接口] {message} 端点={station.Ip}:{station.Port}，"
                + $"工位={station.StationNo}，下行报文={NormalizeHex(requestHex)}");
            LogMessage.Error(
                $"[工位TCP接口][工位{station.StationNo}] {message} 端点={station.Ip}:{station.Port}。",
                ex);
            ResetHolder(holder);
            throw;
        }
        catch (Exception ex)
        {
            Trace(
                trace,
                $"{FormatTimestamp()} - [工位TCP接口] 正向有功电能读取异常："
                + $"端点={station.Ip}:{station.Port}，工位={station.StationNo}，PIID={expectedPiid}，"
                + $"异常={ex.Message}，下行报文={NormalizeHex(requestHex)}");
            LogMessage.Error(
                $"[工位TCP接口][工位{station.StationNo}] 正向有功电能读取异常："
                + $"端点={station.Ip}:{station.Port}，PIID={expectedPiid}。",
                ex);
            ResetHolder(holder);
            throw;
        }
        finally
        {
            holder.Gate.Release();
        }
    }

    /// <summary>释放服务持有的全部工位 TCP 长连接和并发锁。</summary>
    public void Dispose()
    {
        EndRun();
    }

    /// <summary>按规范化 IP:Port 获取或创建唯一会话持有者。</summary>
    private StationTcpConnectionHolder GetHolder(StationCommunicationConfig station)
    {
        string key = $"{station.Ip.Trim()}:{station.Port}";
        return connections.GetOrAdd(key, _ => new StationTcpConnectionHolder());
    }

    /// <summary>
    /// 返回当前可用网络流；连接不存在时在指定超时内建立一次，并记录复用或建连过程。
    /// </summary>
    private static async Task<NetworkStream> EnsureConnectedAsync(
        StationTcpConnectionHolder holder,
        StationCommunicationConfig station,
        int timeoutMs,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        if (holder.Client?.Connected == true)
        {
            Trace(trace, $"[工位TCP接口] 复用长连接：端点={station.Ip}:{station.Port}，工位={station.StationNo}。");
            return holder.Client.GetStream();
        }

        ResetHolder(holder);
        holder.Client = new TcpClient();
        using CancellationTokenSource timeoutCts = CreateTimeoutToken(timeoutMs, cancellationToken);
        Trace(trace, $"[工位TCP接口] 准备连接：端点={station.Ip}:{station.Port}，工位={station.StationNo}。");
        await holder.Client.ConnectAsync(station.Ip, station.Port, timeoutCts.Token).ConfigureAwait(false);
        Trace(trace, $"[工位TCP接口] 连接成功：端点={station.Ip}:{station.Port}，工位={station.StationNo}。");
        return holder.Client.GetStream();
    }

    /// <summary>
    /// 从网络流读取本次普通请求的响应字节，并转换为空格分隔 HEX 文本。
    /// 对 698 帧读取至结束符，其它协议读取当前到达的数据块。
    /// </summary>
    private static async Task<string> ReadResponseHexAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        using MemoryStream memory = new();
        byte[] buffer = new byte[4096];
        while (true)
        {
            int length = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                break;

            memory.Write(buffer, 0, length);
            byte[] bytes = memory.ToArray();
            int frameStart = Array.FindIndex(bytes, value => value == 0x68);
            if (frameStart < 0 || bytes[^1] == 0x16)
                break;
        }

        return BitConverter.ToString(memory.ToArray()).Replace("-", " ");
    }

    /// <summary>
    /// 对普通工位TCP写入增加“原始发送+3次重试”。只在写入失败时重试；一旦写入成功，
    /// 后续应答读取仍只执行一次，避免重复消费电表返回。
    /// </summary>
    private static async Task WriteWithRetryAsync(
        NetworkStream stream,
        byte[] requestBytes,
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
                await stream.WriteAsync(requestBytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                string successMessage = $"{FormatTimestamp()} - {description} 发送完成：尝试={attempt}/{MaximumSendAttempts}。";
                if (attempt > 1)
                {
                    Trace(trace, successMessage);
                }
                else
                {
                    LogMessage.Debug(successMessage);
                }

                return;
            }
            catch (Exception ex) when (attempt < MaximumSendAttempts && ex is IOException or ObjectDisposedException or SocketException or InvalidOperationException)
            {
                lastException = ex;
                Trace(
                    trace,
                    $"{FormatTimestamp()} - {description} 发送失败，准备重试：尝试={attempt}/{MaximumSendAttempts}，原因={ex.Message}。");
                await Task.Delay(SendRetryDelay, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                lastException = ex;
                break;
            }
        }

        throw new IOException($"{description} 发送失败且重试耗尽。", lastException);
    }

    /// <summary>
    /// 请求发送前清空连接中已经到达的旧数据，避免上一步延迟响应污染当前小项。
    /// 返回被丢弃的完整 HEX，供现场日志追溯。
    /// </summary>
    private static async Task<string> DrainBufferedDataAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        using MemoryStream discarded = new();
        byte[] buffer = new byte[4096];
        while (stream.DataAvailable && discarded.Length < 64 * 1024)
        {
            int length = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                break;

            discarded.Write(buffer, 0, length);
        }

        return discarded.Length == 0
            ? string.Empty
            : BitConverter.ToString(discarded.ToArray()).Replace("-", " ");
    }

    /// <summary>判断解析失败是否仅因为帧属于其它 PIID/OAD，可继续等待当前请求响应。</summary>
    private static bool IsNonCurrentEnergyReadFrame(Sgcc698EnergyReadParseResult parseResult)
    {
        return parseResult.Message.Contains("PIID校验失败", StringComparison.OrdinalIgnoreCase) ||
               parseResult.Message.Contains("OAD校验失败", StringComparison.OrdinalIgnoreCase) ||
               parseResult.Message.Contains("读取响应标识错误", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>将允许空格和分隔符的 HEX 文本转换为原始字节；非法长度返回空数组。</summary>
    private static byte[] ParseHexBytes(string value)
    {
        string normalized = new(value.Where(Uri.IsHexDigit).ToArray());
        if (normalized.Length == 0 || normalized.Length % 2 != 0)
            return Array.Empty<byte>();

        byte[] bytes = new byte[normalized.Length / 2];
        for (int index = 0; index < bytes.Length; index++)
        {
            bytes[index] = Convert.ToByte(normalized.Substring(index * 2, 2), 16);
        }

        return bytes;
    }

    /// <summary>将任意合法 HEX 输入规范化为空格分隔大写文本，便于日志核对。</summary>
    private static string NormalizeHex(string value)
    {
        byte[] bytes = ParseHexBytes(value);
        return BitConverter.ToString(bytes).Replace("-", " ");
    }

    /// <summary>创建同时响应用户取消和接口超时的组合取消令牌。</summary>
    private static CancellationTokenSource CreateTimeoutToken(
        int timeoutMs,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource source = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        source.CancelAfter(Math.Max(100, timeoutMs));
        return source;
    }

    /// <summary>关闭失效连接并清空持有者，使下一次请求可以重新建立连接。</summary>
    private static void ResetHolder(StationTcpConnectionHolder holder)
    {
        holder.Client?.Dispose();
        holder.Client = null;
    }

    /// <summary>将接口明细同时写入工位过程日志和全局 Debug 日志。</summary>
    private static void Trace(Action<string>? trace, string message)
    {
        trace?.Invoke(message);
        LogMessage.Debug(message);
    }

    /// <summary>生成精确到毫秒的工位通信时间戳。</summary>
    private static string FormatTimestamp()
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";
    }

    /// <summary>单个 IP:Port 会话及串行收发锁。</summary>
    private sealed class StationTcpConnectionHolder : IDisposable
    {
        public TcpClient? Client { get; set; }

        public SemaphoreSlim Gate { get; } = new(1, 1);

        /// <summary>关闭当前 TCP 连接并释放该端点的串行收发锁。</summary>
        public void Dispose()
        {
            Client?.Dispose();
            Gate.Dispose();
        }
    }
}

/// <summary>工位 TCP 建连或重连失败；流程服务捕获后按当前工位失败处理。</summary>
internal sealed class MeterTestStationConnectionException : Exception
{
    /// <summary>使用明确的工位建连失败信息和底层异常创建连接异常。</summary>
    public MeterTestStationConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
