namespace ModelTest.MeterTest;

/// <summary>
/// 手动降源调用前置保护。把是否允许进入原生 DLL 的判断独立出来，便于无硬件模拟验证。
/// </summary>
public static class MeterTestSourceShutdownGuard
{
    public const string PortUnavailableMessage =
        "源串口未打开或打开失败，未调用 XYCtr.ShutPowerSource(0)。";

    /// <summary>
    /// 从同一厂家协议的启用配置中解析唯一的源串口和降源模式。
    /// 单相、三相可以各有一套升源参数，但它们控制同一台源时必须使用相同串口和降源模式。
    /// </summary>
    public static bool TryResolveSettings(
        IEnumerable<MeterTestSourceControlConfig> configs,
        out int sourcePort,
        out int shutMode,
        out string errorMessage)
    {
        sourcePort = 0;
        shutMode = 0;
        errorMessage = string.Empty;

        List<MeterTestSourceControlConfig> enabledConfigs = configs
            .Where(config => config.Enabled)
            .ToList();
        if (enabledConfigs.Count == 0)
        {
            errorMessage = "没有可用于降源的启用源配置。";
            return false;
        }

        List<int> sourcePorts = enabledConfigs
            .Select(config => config.SourcePort)
            .Distinct()
            .ToList();
        if (sourcePorts.Count != 1 || sourcePorts[0] <= 0)
        {
            errorMessage = sourcePorts.Count == 1
                ? $"源串口号无效：{sourcePorts[0]}。"
                : $"启用的源配置包含多个串口：{string.Join("、", sourcePorts.Select(port => $"COM{port}"))}，无法确定降源端口。";
            return false;
        }

        List<int> shutModes = enabledConfigs
            .Select(config => config.ShutMode)
            .Distinct()
            .ToList();
        if (shutModes.Count != 1 || shutModes[0] is < 0 or > 2)
        {
            errorMessage = shutModes.Count == 1
                ? $"降源模式无效：{shutModes[0]}，有效范围为0-2。"
                : $"启用的源配置包含多个降源模式：{string.Join("、", shutModes)}，无法确定降源参数。";
            return false;
        }

        sourcePort = sourcePorts[0];
        shutMode = shutModes[0];
        return true;
    }

    /// <summary>只有源串口已成功打开时，才允许调用原生降源接口。</summary>
    public static bool CanInvoke(bool isSourcePortOpen)
    {
        return isSourcePortOpen;
    }
}
