using System.Collections.Concurrent;
using System.Net;
using ModelTest.Socket_DLL.Socket_Client;
using ModelTest.Socket_DLL.Socket_Client.TCPClientManner;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 控制 PCB 连接适配层。
///
/// TCP 建连、原始字节接收和连接释放复用 BatchTcpClientManager；本类只负责：
/// 1. 按规范化 IP:Port 去重；
/// 2. 将 Batch 收到的字节块拆成 V1/V2 完整协议帧；
/// 3. 为测试步骤提供按帧订阅和整组发送锁。
/// </summary>
public sealed class MeterTestControlPcbConnectionManager : IAsyncDisposable
{
    private readonly BatchTcpClientManager batchManager = new()
    {
        EnableAutoReconnect = false,
        EnableHeartbeat = false
    };

    private readonly ConcurrentDictionary<ControlPcbEndpoint, MeterTestControlPcbConnection> connections = new();
    private int disposed;

    /// <summary>创建连接管理器并订阅 BatchTcpClientManager 的原始消息和连接状态事件。</summary>
    public MeterTestControlPcbConnectionManager()
    {
        batchManager.MessageReceived += BatchManager_MessageReceived;
        batchManager.ConnectionStatusChanged += BatchManager_ConnectionStatusChanged;
    }

    /// <summary>按方案配置去重端点，并在程序启动阶段各连接一次。</summary>
    public async Task InitializeAsync(
        MeterTestPlanConfig planConfig,
        TimeSpan connectTimeout,
        Action<string>? statusLogger,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        batchManager.ConnectTimeout = Math.Max(100, (int)connectTimeout.TotalMilliseconds);

        List<ControlPcbEndpointDefinition> definitions = new();
        foreach (MeterTestControlPcbGroup group in planConfig.ControlPcbGroups.Where(group => group.Enabled))
        {
            if (TryCreateDefinition(group.Ip, group.Port, group.ProtocolVersion, $"控制PCB组 {group.Name}", out ControlPcbEndpointDefinition definition))
            {
                definitions.Add(definition);
            }
            else
            {
                statusLogger?.Invoke($"控制PCB连接跳过：{group.Name} 的 IP/Port 配置无效。");
            }
        }

        MeterTestBenchTypeSwitchConfig benchConfig = planConfig.BenchTypeSwitchConfig;
        if (benchConfig.Enabled)
        {
            foreach (MeterTestBenchTypeSwitchEndpoint endpoint in benchConfig.GetEnabledEndpoints())
            {
                if (TryCreateDefinition(
                        endpoint.Ip,
                        endpoint.Port,
                        MeterControlPcbProtocolVersion.V2.ToString(),
                        $"升源前台体类型切换 {endpoint.DisplayName}",
                        out ControlPcbEndpointDefinition benchDefinition))
                {
                    definitions.Add(benchDefinition);
                }
                else
                {
                    statusLogger?.Invoke(
                        $"台体类型切换端点跳过：{endpoint.DisplayName} 的 IP/Port 配置无效。"
                        + $" 当前={endpoint.Ip}:{endpoint.Port}。");
                }
            }
        }

        foreach (MeterTestIndicatorLightGroup group in planConfig.IndicatorLightGroups.Where(group => group.Enabled))
        {
            if (TryCreateDefinition(
                    group.Ip,
                    group.Port,
                    group.ProtocolVersion,
                    $"工位指示灯 {group.Name}",
                    out ControlPcbEndpointDefinition lightDefinition))
            {
                definitions.Add(lightDefinition);
            }
            else
            {
                statusLogger?.Invoke($"指示灯控制端点跳过：{group.Name} 的 IP/Port 配置无效。");
            }
        }

        Dictionary<ControlPcbEndpoint, ControlPcbEndpointDefinition> uniqueDefinitions = new();
        foreach (ControlPcbEndpointDefinition definition in definitions)
        {
            if (!uniqueDefinitions.TryGetValue(definition.Endpoint, out ControlPcbEndpointDefinition? existing))
            {
                uniqueDefinitions[definition.Endpoint] = definition;
                continue;
            }

            if (!existing.ProtocolVersion.Equals(definition.ProtocolVersion, StringComparison.OrdinalIgnoreCase))
            {
                statusLogger?.Invoke(
                    $"控制PCB端点 {definition.Endpoint.DisplayName} 协议冲突："
                    + $"{existing.ProtocolVersion}/{definition.ProtocolVersion}，保留首次配置。");
            }
        }

        List<(MeterTestControlPcbConnection Connection, ControlPcbEndpointDefinition Definition)> pending = new();
        foreach (ControlPcbEndpointDefinition definition in uniqueDefinitions.Values)
        {
            MeterTestControlPcbConnection connection = connections.GetOrAdd(
                definition.Endpoint,
                _ => new MeterTestControlPcbConnection(batchManager, definition.Endpoint, definition.ProtocolVersion, statusLogger));

            if (!connection.ProtocolVersion.Equals(definition.ProtocolVersion, StringComparison.OrdinalIgnoreCase))
            {
                statusLogger?.Invoke(
                    $"控制PCB端点 {definition.Endpoint.DisplayName} 已使用协议 {connection.ProtocolVersion}，"
                    + $"忽略冲突配置 {definition.ProtocolVersion}。");
                continue;
            }

            pending.Add((connection, definition));
        }

        // 连接对象内部有一次性建连保护；重复初始化只等待原始建连结果，不会创建第二个 TcpClient。
        await Task.WhenAll(pending.Select(item => item.Connection.ConnectOnceAsync(connectTimeout, cancellationToken)));
    }

    /// <summary>取得启动阶段已经建立的连接；此方法绝不会触发新的网络连接。</summary>
    public bool TryGetConnectedConnection(
        MeterTestControlPcbGroup group,
        out MeterTestControlPcbConnection connection,
        out string error)
    {
        return TryGetConnectedConnection(group.Ip, group.Port, group.ProtocolVersion, out connection, out error);
    }

    /// <summary>按 IP、端口和协议版本取得已建立连接。</summary>
    public bool TryGetConnectedConnection(
        string ip,
        int port,
        string protocolVersion,
        out MeterTestControlPcbConnection connection,
        out string error)
    {
        connection = null!;
        error = string.Empty;
        if (!TryCreateEndpoint(ip, port, out ControlPcbEndpoint endpoint, out error))
        {
            return false;
        }

        if (!connections.TryGetValue(endpoint, out MeterTestControlPcbConnection? existing))
        {
            error = $"控制PCB {endpoint.DisplayName} 未在程序启动阶段建立连接。";
            return false;
        }

        if (!existing.ProtocolVersion.Equals(protocolVersion, StringComparison.OrdinalIgnoreCase))
        {
            error = $"控制PCB {endpoint.DisplayName} 协议不一致，连接={existing.ProtocolVersion}，配置={protocolVersion}。";
            return false;
        }

        if (!existing.IsConnected)
        {
            error = $"控制PCB {endpoint.DisplayName} 未连接或已断开，测试过程中不临时重连。";
            return false;
        }

        connection = existing;
        return true;
    }

    /// <summary>释放所有 MeterTest 控制 PCB 连接。</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
        {
            return;
        }

        batchManager.MessageReceived -= BatchManager_MessageReceived;
        batchManager.ConnectionStatusChanged -= BatchManager_ConnectionStatusChanged;
        foreach (MeterTestControlPcbConnection connection in connections.Values)
        {
            await connection.DisposeAsync();
        }

        batchManager.Dispose();
        connections.Clear();
    }

    /// <summary>将 BatchTcpClientManager 收到的原始字节块路由到对应控制 PCB 连接。</summary>
    private void BatchManager_MessageReceived(object? sender, TcpClientMessageEventArgs e)
    {
        MeterTestControlPcbConnection? connection = connections.Values.FirstOrDefault(item => item.ConnectionId == e.ConnectionId);
        connection?.HandleRawData(e.RawData);
    }

    /// <summary>将底层连接状态变化路由到对应控制 PCB 连接并记录断线信息。</summary>
    private void BatchManager_ConnectionStatusChanged(object? sender, TcpConnectionEventArgs e)
    {
        MeterTestControlPcbConnection? connection = connections.Values.FirstOrDefault(item => item.ConnectionId == e.ConnectionId);
        connection?.HandleBatchConnectionStatus(e.IsConnected, e.Status);
    }

    /// <summary>校验端点并创建带协议版本和配置来源的连接定义。</summary>
    private static bool TryCreateDefinition(
        string ip,
        int port,
        string protocolVersion,
        string source,
        out ControlPcbEndpointDefinition definition)
    {
        definition = null!;
        if (!TryCreateEndpoint(ip, port, out ControlPcbEndpoint endpoint, out _))
        {
            return false;
        }

        definition = new ControlPcbEndpointDefinition(endpoint, protocolVersion, source);
        return true;
    }

    /// <summary>校验 IP 和端口并生成规范化控制 PCB 端点键。</summary>
    private static bool TryCreateEndpoint(
        string ip,
        int port,
        out ControlPcbEndpoint endpoint,
        out string error)
    {
        endpoint = default;
        error = string.Empty;
        if (!IPAddress.TryParse(ip?.Trim(), out IPAddress? address))
        {
            error = $"IP 地址无效：{ip}";
            return false;
        }

        if (port is < 1 or > 65535)
        {
            error = $"端口必须在1-65535之间：{port}";
            return false;
        }

        endpoint = new ControlPcbEndpoint(address.ToString(), port);
        return true;
    }

    /// <summary>管理器已释放时阻止继续创建或获取连接。</summary>
    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(MeterTestControlPcbConnectionManager));
        }
    }

    /// <summary>作为连接字典键使用的规范化控制 PCB IP 和端口。</summary>
    internal readonly record struct ControlPcbEndpoint(string Ip, int Port)
    {
        public string DisplayName => $"{Ip}:{Port}";
    }

    /// <summary>控制 PCB 唯一端点、协议版本及其配置来源。</summary>
    private sealed record ControlPcbEndpointDefinition(
        ControlPcbEndpoint Endpoint,
        string ProtocolVersion,
        string Source);
}

/// <summary>
/// MeterTest 单个控制 PCB 的协议适配连接。
/// BatchTcpClientManager 负责原始 TCP；本类负责完整帧拆分、应答订阅和发送序列互斥。
/// </summary>
public sealed class MeterTestControlPcbConnection : IAsyncDisposable
{
    private const int MaximumSendAttempts = 4;
    private static readonly TimeSpan SendRetryDelay = TimeSpan.FromMilliseconds(100);

    private readonly BatchTcpClientManager batchManager;
    private readonly MeterTestControlPcbConnectionManager.ControlPcbEndpoint endpoint;
    private readonly Action<string>? statusLogger;
    private readonly SemaphoreSlim sendGate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, Action<byte[]>> subscribers = new();
    private readonly List<byte> receiveBuffer = new();
    private readonly object receiveBufferLock = new();
    private readonly TaskCompletionSource<bool> connectCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private string? connectionId;
    private int connectAttempted;
    private int disposed;

    /// <summary>创建一个绑定到 BatchTcpClientManager 唯一端点的协议适配连接。</summary>
    internal MeterTestControlPcbConnection(
        BatchTcpClientManager batchManager,
        MeterTestControlPcbConnectionManager.ControlPcbEndpoint endpoint,
        string protocolVersion,
        Action<string>? statusLogger)
    {
        this.batchManager = batchManager;
        this.endpoint = endpoint;
        ProtocolVersion = protocolVersion;
        this.statusLogger = statusLogger;
    }

    /// <summary>该端点拆包和组包使用的控制 PCB 协议版本。</summary>
    public string ProtocolVersion { get; }

    /// <summary>用于日志展示的 IP:Port。</summary>
    public string DisplayName => endpoint.DisplayName;

    /// <summary>BatchTcpClientManager 返回的内部连接标识。</summary>
    public string? ConnectionId => connectionId;

    /// <summary>判断底层连接是否存在且处于已连接状态。</summary>
    public bool IsConnected => connectionId is not null &&
        batchManager.GetConnectionInfo(connectionId)?.IsConnected == true;

    /// <summary>使用 BatchTcpClientManager 建立一次连接，后续重复调用只等待首次结果。</summary>
    internal async Task ConnectOnceAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref connectAttempted, 1) != 0)
        {
            await connectCompletion.Task.WaitAsync(cancellationToken);
            return;
        }

        try
        {
            ReportStatus(
                $"[控制PCB连接接口] 开始连接：端点={DisplayName}，协议={ProtocolVersion}，"
                + $"超时={Math.Max(100, timeout.TotalMilliseconds):0}ms。"
            );
            string? createdConnectionId = await batchManager.CreateAndConnectAsync(endpoint.Ip, endpoint.Port, DisplayName);
            connectionId = createdConnectionId;
            if (createdConnectionId is null)
            {
                ReportStatus(
                    $"[控制PCB连接接口] 连接失败：端点={DisplayName}，协议={ProtocolVersion}，"
                    + "BatchTcpClientManager未返回连接标识。",
                    isError: true);
            }
            else
            {
                ReportStatus(
                    $"[控制PCB连接接口] 连接成功：端点={DisplayName}，协议={ProtocolVersion}，"
                    + $"ConnectionId={createdConnectionId}；后续测试复用此连接。"
                );
            }
        }
        catch (Exception ex)
        {
            ReportStatus(
                $"[控制PCB连接接口] 连接异常：端点={DisplayName}，协议={ProtocolVersion}，"
                + $"异常={ex.Message}。",
                isError: true,
                exception: ex);
        }
        finally
        {
            connectCompletion.TrySetResult(IsConnected);
        }
    }

    /// <summary>注册一个测试步骤的应答订阅。</summary>
    public IDisposable Subscribe(Action<byte[]> frameHandler)
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(MeterTestControlPcbConnection));
        }

        Guid id = Guid.NewGuid();
        subscribers[id] = frameHandler;
        return new Subscription(() => subscribers.TryRemove(id, out _));
    }

    /// <summary>发送单条二进制报文，不触发连接操作。</summary>
    public Task SendAsync(byte[] packet, CancellationToken cancellationToken)
    {
        return SendSequenceAsync(new[] { packet }, TimeSpan.Zero, null, cancellationToken);
    }

    /// <summary>在同一发送锁内发送一组报文，避免多个测试步骤交叉写入同一 TCP 流。</summary>
    public async Task SendSequenceAsync(
        IReadOnlyList<byte[]> packets,
        TimeSpan packetInterval,
        Action<int, byte[]>? beforeSend,
        CancellationToken cancellationToken)
    {
        if (connectionId is null || !IsConnected)
        {
            throw new InvalidOperationException($"控制PCB {DisplayName} 未连接，测试过程中不临时重连。");
        }

        await sendGate.WaitAsync(cancellationToken);
        try
        {
            for (int index = 0; index < packets.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                byte[] packet = packets[index];
                beforeSend?.Invoke(index, packet);
                bool sent = false;
                for (int attempt = 1; attempt <= MaximumSendAttempts; attempt++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    sent = await batchManager.SendBytesAsync(connectionId, packet);
                    if (sent)
                    {
                        if (attempt > 1)
                        {
                            LogMessage.Debug(
                                $"[控制PCB连接接口] 发送重试成功：端点={DisplayName}，协议={ProtocolVersion}，"
                                + $"序号={index + 1}/{packets.Count}，尝试={attempt}/{MaximumSendAttempts}。");
                        }

                        break;
                    }

                    LogMessage.Error(
                        $"[控制PCB连接接口] 发送失败，准备重试：端点={DisplayName}，协议={ProtocolVersion}，"
                        + $"序号={index + 1}/{packets.Count}，尝试={attempt}/{MaximumSendAttempts}，"
                        + $"报文={BitConverter.ToString(packet).Replace("-", " ")}。",
                        null);
                    if (attempt < MaximumSendAttempts)
                    {
                        await Task.Delay(SendRetryDelay, cancellationToken);
                    }
                }

                if (!sent)
                {
                    LogMessage.Error(
                        $"[控制PCB连接接口] 发送失败且重试耗尽：端点={DisplayName}，协议={ProtocolVersion}，"
                        + $"序号={index + 1}/{packets.Count}，尝试={MaximumSendAttempts}/{MaximumSendAttempts}，"
                        + $"报文={BitConverter.ToString(packet).Replace("-", " ")}。",
                        null);
                    throw new IOException($"BatchTcpClientManager 发送失败：{DisplayName}");
                }

                LogMessage.Debug(
                    $"[控制PCB连接接口] 发送完成：端点={DisplayName}，协议={ProtocolVersion}，"
                    + $"序号={index + 1}/{packets.Count}，字节数={packet.Length}，"
                    + $"报文={BitConverter.ToString(packet).Replace("-", " ")}。");

                if (index < packets.Count - 1 && packetInterval > TimeSpan.Zero)
                {
                    await Task.Delay(packetInterval, cancellationToken);
                }
            }
        }
        finally
        {
            sendGate.Release();
        }
    }

    /// <summary>处理 BatchTcpClientManager 收到的原始字节块，并按 V1/V2 格式分发完整帧。</summary>
    internal void HandleRawData(byte[] rawData)
    {
        if (rawData is null || rawData.Length == 0 || Volatile.Read(ref disposed) != 0)
        {
            return;
        }

        lock (receiveBufferLock)
        {
            receiveBuffer.AddRange(rawData);
            while (TryTakeFrame(receiveBuffer, ProtocolVersion, out byte[] frame))
            {
                foreach (Action<byte[]> handler in subscribers.Values.ToArray())
                {
                    try
                    {
                        handler(frame);
                    }
                    catch (Exception ex)
                    {
                        ReportStatus(
                            $"[控制PCB连接接口] 应答订阅处理异常：端点={DisplayName}，"
                            + $"协议={ProtocolVersion}，报文={BitConverter.ToString(frame).Replace("-", " ")}，"
                            + $"异常={ex.Message}。",
                            isError: true,
                            exception: ex);
                    }
                }
            }
        }
    }

    /// <summary>接收底层连接状态；断开时写入可追踪的端点状态日志。</summary>
    internal void HandleBatchConnectionStatus(bool isConnected, string status)
    {
        if (!isConnected)
        {
            ReportStatus(
                $"[控制PCB连接接口] 连接状态变化：端点={DisplayName}，协议={ProtocolVersion}，"
                + $"IsConnected={isConnected}，状态={status}。",
                isError: true);
        }
    }

    /// <summary>将连接层状态同步写入界面回调和全局日志，并保留可选异常堆栈。</summary>
    private void ReportStatus(
        string message,
        bool isError = false,
        Exception? exception = null)
    {
        statusLogger?.Invoke(message);
        if (isError)
        {
            LogMessage.Error(message, exception);
        }
        else
        {
            LogMessage.Debug(message);
        }
    }

    /// <summary>停止接收、清空缓存并释放发送锁；底层管理器统一处理实际 TCP 生命周期。</summary>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            subscribers.Clear();
            lock (receiveBufferLock)
            {
                receiveBuffer.Clear();
            }

            sendGate.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>按 V1 或 V2 长度及结束符从累计缓冲区提取一帧完整控制 PCB 报文。</summary>
    private static bool TryTakeFrame(List<byte> buffer, string protocolVersion, out byte[] frame)
    {
        frame = Array.Empty<byte>();
        bool isV2 = !protocolVersion.Equals(MeterControlPcbProtocolVersion.V1.ToString(), StringComparison.OrdinalIgnoreCase);
        int startIndex = FindStartIndex(buffer, 0x55, isV2 ? (byte?)0x44 : null);
        if (startIndex < 0)
        {
            buffer.Clear();
            return false;
        }

        if (startIndex > 0)
        {
            buffer.RemoveRange(0, startIndex);
        }

        if (isV2)
        {
            if (buffer.Count < 4)
                return false;

            int dataLength = buffer[2] | (buffer[3] << 8);
            int totalLength = dataLength + 4;
            if (dataLength < 7 || totalLength > 65539)
            {
                buffer.RemoveAt(0);
                return false;
            }

            if (buffer.Count < totalLength)
                return false;

            if (buffer[totalLength - 2] != 0xAA || buffer[totalLength - 1] != 0xBB)
            {
                buffer.RemoveAt(0);
                return false;
            }

            frame = buffer.Take(totalLength).ToArray();
            buffer.RemoveRange(0, totalLength);
            return true;
        }

        if (buffer.Count < 3)
            return false;

        int frameLength = buffer[1] | (buffer[2] << 8);
        int totalV1Length = frameLength + 2;
        if (frameLength < 8 || totalV1Length > 65537)
        {
            buffer.RemoveAt(0);
            return false;
        }

        if (buffer.Count < totalV1Length)
            return false;

        if (buffer[totalV1Length - 1] != 0xAA)
        {
            buffer.RemoveAt(0);
            return false;
        }

        frame = buffer.Take(totalV1Length).ToArray();
        buffer.RemoveRange(0, totalV1Length);
        return true;
    }

    /// <summary>在累计缓冲区定位 V1 单字节或 V2 双字节起始符。</summary>
    private static int FindStartIndex(List<byte> buffer, byte first, byte? second)
    {
        for (int index = 0; index < buffer.Count; index++)
        {
            if (buffer[index] != first)
                continue;

            if (!second.HasValue || (index + 1 < buffer.Count && buffer[index + 1] == second.Value))
                return index;
        }

        return -1;
    }

    /// <summary>应答订阅的可释放句柄，释放时从连接订阅集合移除回调。</summary>
    private sealed class Subscription : IDisposable
    {
        private Action? disposeAction;

        /// <summary>创建由指定移除动作控制的订阅句柄。</summary>
        public Subscription(Action disposeAction)
        {
            this.disposeAction = disposeAction;
        }

        /// <summary>以线程安全且幂等的方式执行一次取消订阅。</summary>
        public void Dispose()
        {
            Interlocked.Exchange(ref disposeAction, null)?.Invoke();
        }
    }
}
