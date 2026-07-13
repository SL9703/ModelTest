using System.Net;
using System.Threading.Tasks;

namespace ModelTest;

/// <summary>
/// WinSocket 登录服务。
/// 统一负责 IP/Port 校验、连接、断开以及 UI 需要的登录结果封装。
/// </summary>
public sealed class WinSocketLoginService
{
    private readonly WinSocketServiceInvoker _invoker;
    private readonly WinSocketServer _server;

    public WinSocketLoginService(WinSocketServiceInvoker invoker, WinSocketServer server)
    {
        _invoker = invoker;
        _server = server;
    }

    /// <summary>
    /// 登录按钮统一入口。
    /// 当前已连接时执行断开，未连接时执行连接。
    /// </summary>
    public async Task<WinSocketLoginOperationResult> ExecuteAsync(bool isConnected, string rawIp, string rawPort)
    {
        string serverIp = (rawIp ?? string.Empty).Trim();
        string serverPort = (rawPort ?? string.Empty).Trim();

        if (isConnected)
        {
            return await DisconnectAsync(serverIp, serverPort);
        }

        return await ConnectAsync(serverIp, serverPort);
    }

    /// <summary>
    /// 连接前先做参数合法性校验，连接成功后同步获取一次随机数验证链路。
    /// </summary>
    private async Task<WinSocketLoginOperationResult> ConnectAsync(string serverIp, string serverPort)
    {
        if (string.IsNullOrWhiteSpace(serverIp) || string.IsNullOrWhiteSpace(serverPort))
        {
            return WinSocketLoginOperationResult.Fail(
                serverIp,
                serverPort,
                "加密服务器连接状态：连接失败",
                "请输入有效的服务器 IP 和端口。");
        }

        if (!IPAddress.TryParse(serverIp, out _))
        {
            return WinSocketLoginOperationResult.Fail(
                serverIp,
                serverPort,
                "加密服务器连接状态：连接失败",
                "IP 格式无效，请输入正确的 IPv4 或 IPv6 地址。");
        }

        if (!int.TryParse(serverPort, out int port) || port is < 1 or > 65535)
        {
            return WinSocketLoginOperationResult.Fail(
                serverIp,
                serverPort,
                "加密服务器连接状态：连接失败",
                "端口范围无效，请输入 1-65535 之间的整数。");
        }

        var result = await Task.Run(() => _invoker.ConnectServerAndGetRandom(serverIp, port.ToString()));
        if (!result.Connected)
        {
            return WinSocketLoginOperationResult.Fail(
                serverIp,
                serverPort,
                "加密服务器连接状态：连接失败",
                $"连接失败，错误码：{result.ConnectCode}");
        }

        return new WinSocketLoginOperationResult(
            serverIp,
            serverPort,
            true,
            true,
            false,
            "加密服务器连接状态：已连接",
            result.RandCode == 0
                ? $"获取随机数成功！随机数结果 = {result.RandText}"
                : $"获取随机数失败！错误码：{result.RandCode}",
            "断开服务器");
    }

    /// <summary>
    /// 调用底层释放接口断开当前加密机会话。
    /// </summary>
    private async Task<WinSocketLoginOperationResult> DisconnectAsync(string serverIp, string serverPort)
    {
        int result = await Task.Run(() => _server.ClseUsbkeyEx());
        if (result != 0)
        {
            return new WinSocketLoginOperationResult(
                serverIp,
                serverPort,
                true,
                false,
                false,
                "加密服务器连接状态：已连接",
                $"断开失败！错误码：{result}",
                "断开服务器");
        }

        return new WinSocketLoginOperationResult(
            serverIp,
            serverPort,
            false,
            false,
            true,
            "加密服务器连接状态：已断开",
            "加密服务器已断开。",
            "登录加密机");
    }
}

/// <summary>
/// 登录操作结果。
/// 这个 record 既保存连接状态，也保存 UI 层需要直接回写的文本状态。
/// </summary>
public sealed record WinSocketLoginOperationResult(
    string ServerIp,
    string ServerPort,
    bool IsConnected,
    bool ShouldStartHeartbeat,
    bool ShouldStopHeartbeat,
    string StatusText,
    string Message,
    string ButtonText)
{
    /// <summary>
    /// 连接失败时的统一工厂方法。
    /// </summary>
    public static WinSocketLoginOperationResult Fail(
        string serverIp,
        string serverPort,
        string statusText,
        string message)
    {
        return new WinSocketLoginOperationResult(
            serverIp,
            serverPort,
            false,
            false,
            false,
            statusText,
            message,
            "登录加密机");
    }
}
