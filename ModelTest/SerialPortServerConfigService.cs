using System.Net.Sockets;
using ModelTest.Protocol;

namespace ModelTest;

/// <summary>
/// 串口服务器配置业务服务。
/// 该类负责 TCP 管理端连接、通用底层协议指令发送、端口参数解析；窗体只负责按钮和UI刷新。
/// </summary>
public sealed class SerialPortServerConfigService : IDisposable
{
    private const int ConnectTimeoutMilliseconds = 5000;
    private const int OptionalResponseTimeoutMilliseconds = 1200;
    private TcpClient? tcpClient;
    private NetworkStream? networkStream;

    /// <summary>向界面输出通信过程日志。</summary>
    public event Action<string>? LogRequested;

    /// <summary>当前是否持有可用的 TCP 管理端连接。</summary>
    public bool IsConnected => tcpClient?.Connected == true && networkStream is not null;

    /// <summary>连接串口服务器管理端。</summary>
    public async Task<bool> ConnectAsync(string ip, int port)
    {
        if (IsConnected)
            return true;

        try
        {
            TcpClient client = new();
            using CancellationTokenSource timeout = new(TimeSpan.FromMilliseconds(ConnectTimeoutMilliseconds));
            Log($"准备连接串口服务器管理端：{ip}:{port}");
            await client.ConnectAsync(ip, port, timeout.Token);
            tcpClient = client;
            networkStream = client.GetStream();
            Log($"连接成功：{ip}:{port}");
            return true;
        }
        catch (Exception ex)
        {
            DisposeConnection();
            Log($"连接失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>主动断开当前管理端连接。</summary>
    public void Disconnect()
    {
        DisposeConnection();
        Log("已断开串口服务器管理端。");
    }

    /// <summary>发送 FF0C 查看端口信息并解析返回的16路串口参数。</summary>
    public async Task<IReadOnlyList<GenericSerialPortChannelInfo>> ViewPortsAsync()
    {
        if (!IsConnected)
        {
            Log("串口服务器管理端未连接。");
            return Array.Empty<GenericSerialPortChannelInfo>();
        }

        byte[] request = GenericSerialPortServerProtocol.BuildReadSerialParametersCommand();
        byte[] response = await SendCommandAsync("查看端口信息", request);
        if (response.Length == 0)
        {
            Log("查看端口信息未收到应答。");
            return Array.Empty<GenericSerialPortChannelInfo>();
        }

        if (!GenericSerialPortServerProtocol.TryParseReadSerialParametersResponse(
                response,
                out List<GenericSerialPortChannelInfo> channels,
                out string error))
        {
            Log($"端口信息解析失败：{error}");
            return Array.Empty<GenericSerialPortChannelInfo>();
        }

        Log($"端口信息查看完成：解析到 {channels.Count} 路COM。");
        return channels;
    }

    /// <summary>发送解锁和设置端口指令，设置指令默认立即生效。</summary>
    public async Task<bool> SetPortAsync(int tcpPort, string serialProfile)
    {
        if (!IsConnected)
        {
            Log("串口服务器管理端未连接。");
            return false;
        }

        GenericSerialPortServerCommandSet commandSet;
        try
        {
            commandSet = GenericSerialPortServerProtocol.BuildCommandSet(tcpPort, serialProfile);
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException)
        {
            Log($"参数错误：{ex.Message}");
            return false;
        }

        Log($"准备设置目标TCP端口={tcpPort}，映射通道=0x{commandSet.ChannelIndex:X2}，参数={serialProfile}。");
        await SendCommandAsync("解锁", commandSet.UnlockCommand);
        await SendCommandAsync("设置端口", commandSet.SetPortCommand);
        return true;
    }

    /// <summary>发送断电保存指令。部分串口服务器收到后会保存并重启。</summary>
    public async Task<bool> SaveAsync()
    {
        if (!IsConnected)
        {
            Log("串口服务器管理端未连接。");
            return false;
        }

        byte[] request = GenericSerialPortServerProtocol.BuildSaveAndRestartCommand();
        await SendCommandAsync("断电保存", request);
        Log("保存指令已发送，部分串口服务器会保存后重启，请留意连接状态。");
        return true;
    }

    /// <summary>发送一条底层管理指令，并读取一次可选应答。</summary>
    private async Task<byte[]> SendCommandAsync(string action, byte[] request)
    {
        if (networkStream is null)
            return [];

        string requestHex = GenericSerialPortServerProtocol.ToHexString(request);
        Log($"发送{action}：{requestHex}");
        await networkStream.WriteAsync(request);
        await networkStream.FlushAsync();

        byte[] buffer = new byte[2048];
        using CancellationTokenSource timeout = new(TimeSpan.FromMilliseconds(OptionalResponseTimeoutMilliseconds));
        try
        {
            int length = await networkStream.ReadAsync(buffer.AsMemory(0, buffer.Length), timeout.Token);
            byte[] response = buffer.AsSpan(0, length).ToArray();
            Log($"接收{action}：{GenericSerialPortServerProtocol.ToHexString(response)}");
            return response;
        }
        catch (OperationCanceledException)
        {
            Log($"接收{action}：");
            return [];
        }
    }

    private void Log(string message)
    {
        LogRequested?.Invoke(message);
    }

    private void DisposeConnection()
    {
        networkStream?.Dispose();
        tcpClient?.Dispose();
        networkStream = null;
        tcpClient = null;
    }

    public void Dispose()
    {
        DisposeConnection();
    }
}
