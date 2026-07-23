using System.Collections.Concurrent;
using System.Net;
using ModelTest.Socket_DLL.Socket_Client;
using ModelTest.Socket_DLL.Socket_Client.TCPClientManner;

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
            if (TryCreateDefinition(group.Ip, group.Port, group.ProtocolVersion, $"控制PCB组 {group.Name}", out ControlPcbEndpointDefinition? definition))
            {
                definitions.Add(definition);
            }
            else
            {
                statusLogger?.Invoke($"控制PCB连接跳过：{group.Name} 的 IP/Port 配置无效。");
            }
        }

        MeterTestBenchTypeSwitchConfig benchConfig = planConfig.BenchTypeSwitchConfig;
        if (benchConfig.Enabled &&
            TryCreateDefinition(
                benchConfig.Ip,
                benchConfig.Port,
                MeterControlPcbProtocolVersion.V2.ToString(),
                "升源前台体类型切换",
                out ControlPcbEndpointDefinition? benchDefinition))
        {
            definitions.Add(benchDefinition);
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

    private void BatchManager_MessageReceived(object? sender, TcpClientMessageEventArgs e)
    {
        MeterTestControlPcbConnection? connection = connections.Values.FirstOrDefault(item => item.ConnectionId == e.ConnectionId);
        connection?.HandleRawData(e.RawData);
    }

    private void BatchManager_ConnectionStatusChanged(object? sender, TcpConnectionEventArgs e)
    {
        MeterTestControlPcbConnection? connection = connections.Values.FirstOrDefault(item => item.ConnectionId == e.ConnectionId);
        connection?.HandleBatchConnectionStatus(e.IsConnected, e.Status);
    }

    private static bool TryCreateDefinition(
        string ip,
        int port,
        string protocolVersion,
        string source,
        out ControlPcbEndpointDefinition? definition)
    {
        definition = null;
        if (!TryCreateEndpoint(ip, port, out ControlPcbEndpoint endpoint, out _))
        {
            return false;
        }

        definition = new ControlPcbEndpointDefinition(endpoint, protocolVersion, source);
        return true;
    }

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

    private void ThrowIfDisposed()
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            throw new ObjectDisposedException(nameof(MeterTestControlPcbConnectionManager));
        }
    }

    internal readonly record struct ControlPcbEndpoint(string Ip, int Port)
    {
        public string DisplayName => $"{Ip}:{Port}";
    }

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

    public string ProtocolVersion { get; }

    public string DisplayName => endpoint.DisplayName;

    public string? ConnectionId => connectionId;

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
            statusLogger?.Invoke($"控制PCB开始连接：{DisplayName}");
            string? createdConnectionId = await batchManager.CreateAndConnectAsync(endpoint.Ip, endpoint.Port, DisplayName);
            connectionId = createdConnectionId;
            if (createdConnectionId is null)
            {
                statusLogger?.Invoke($"控制PCB连接失败：{DisplayName}");
            }
            else
            {
                statusLogger?.Invoke($"控制PCB连接成功：{DisplayName}，后续测试复用此连接");
            }
        }
        catch (Exception ex)
        {
            statusLogger?.Invoke($"控制PCB连接失败：{DisplayName}，{ex.Message}");
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
                bool sent = await batchManager.SendBytesAsync(connectionId, packet);
                if (!sent)
                {
                    throw new IOException($"BatchTcpClientManager 发送失败：{DisplayName}");
                }

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
            while (TryTakeFrame(receiveBuffer, ProtocolVersion, out byte[]? frame))
            {
                foreach (Action<byte[]> handler in subscribers.Values.ToArray())
                {
                    try
                    {
                        handler(frame);
                    }
                    catch (Exception ex)
                    {
                        statusLogger?.Invoke($"控制PCB应答处理异常：{DisplayName}，{ex.Message}");
                    }
                }
            }
        }
    }

    internal void HandleBatchConnectionStatus(bool isConnected, string status)
    {
        if (!isConnected)
        {
            statusLogger?.Invoke($"控制PCB连接状态变化：{DisplayName}，{status}");
        }
    }

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

    private static bool TryTakeFrame(List<byte> buffer, string protocolVersion, out byte[]? frame)
    {
        frame = null;
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

    private sealed class Subscription : IDisposable
    {
        private Action? disposeAction;

        public Subscription(Action disposeAction)
        {
            this.disposeAction = disposeAction;
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref disposeAction, null)?.Invoke();
        }
    }
}
