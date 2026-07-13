using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModelTest;

/// <summary>
/// WinSocket 交互上下文。
/// 统一组织登录、心跳、接口调用、接口目录和默认参数回填，供 UI 直接使用。
/// </summary>
public sealed class WinSocketUiContext
{
    private readonly WinSocketServiceInvoker _invoker;
    private readonly WinSocketLoginService _loginService;
    private readonly IWinSocketServiceCatalog _serviceCatalog;
    private readonly IWinSocketServiceDefaultParameterProvider _defaultParameterProvider;
    private readonly List<string> _serviceNames = new();
    private bool _catalogLoaded;

    /// <summary>
    /// 构造时注入底层 WinSocketServer 和日志出口。
    /// </summary>
    public WinSocketUiContext(WinSocketServer server, Action<string> log)
    {
        ArgumentNullException.ThrowIfNull(server);
        ArgumentNullException.ThrowIfNull(log);

        _invoker = new WinSocketServiceInvoker(server, log);
        _loginService = new WinSocketLoginService(_invoker, server);
        _serviceCatalog = new DefaultWinSocketServiceCatalog();
        _defaultParameterProvider = new WinSocketServiceDefaultParameterProvider();
    }

    public bool IsConnected { get; private set; }

    /// <summary>
    /// 登录按钮的统一入口。
    /// 内部会根据当前连接状态决定执行连接还是断开。
    /// </summary>
    public async Task<WinSocketLoginOperationResult> ExecuteLoginAsync(string rawIp, string rawPort)
    {
        var result = await _loginService.ExecuteAsync(IsConnected, rawIp, rawPort);
        IsConnected = result.IsConnected;
        return result;
    }

    /// <summary>
    /// 发送心跳身份认证数据。
    /// </summary>
    public WinSocketServiceInvoker.IdentityHeartbeatResult SendHeartbeat(int flag, string putDiv)
    {
        return _invoker.SendIdentityHeartbeat(flag, putDiv);
    }

    /// <summary>
    /// 执行一次接口调用。
    /// </summary>
    public WinSocketServiceInvoker.ExecutionResult ExecuteService(string? serviceName, string rawParameterText)
    {
        return _invoker.Execute(serviceName, rawParameterText);
    }

    /// <summary>
    /// 获取接口参数说明文字，供右下说明区展示。
    /// </summary>
    public string GetParameterDescription(string? serviceName)
    {
        return _invoker.GetParameterDescription(serviceName);
    }

    /// <summary>
    /// 获取默认参数。
    /// selectionKey 优先承接菜单语义，serviceName 用作兜底。
    /// </summary>
    public string GetDefaultParameters(string? selectionKey, string? serviceName, bool usePrivateKey)
    {
        return _defaultParameterProvider.GetDefaultParameters(selectionKey, serviceName, usePrivateKey);
    }

    /// <summary>
    /// 关键字过滤接口名列表，供下拉框懒加载。
    /// </summary>
    public string[] GetFilteredServiceNames(string? filter, int pageSize)
    {
        EnsureCatalogLoaded();

        return string.IsNullOrWhiteSpace(filter)
            ? _serviceNames.Take(pageSize).ToArray()
            : _serviceNames
                .Where(name => name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .Take(pageSize)
                .ToArray();
    }

    /// <summary>
    /// 返回完整接口名列表。
    /// </summary>
    public string[] GetAllServiceNames()
    {
        EnsureCatalogLoaded();
        return _serviceNames.ToArray();
    }

    /// <summary>
    /// 强制清空目录缓存。
    /// 一般在 UI 重新初始化搜索列表时调用。
    /// </summary>
    public void ResetCatalog()
    {
        _catalogLoaded = false;
        _serviceNames.Clear();
    }

    /// <summary>
    /// 惰性加载接口目录，避免每次搜索都重新构建完整列表。
    /// </summary>
    private void EnsureCatalogLoaded()
    {
        if (_catalogLoaded)
        {
            return;
        }

        _serviceNames.Clear();
        _serviceNames.AddRange(_serviceCatalog.GetServiceNames());
        _catalogLoaded = true;
    }
}
