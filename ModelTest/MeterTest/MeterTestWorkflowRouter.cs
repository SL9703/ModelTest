using System;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 方案小项的执行流程分类。
/// 该枚举是 UI 主窗体和具体流程服务之间的边界，避免窗体直接解析 XML executionMode。
/// </summary>
internal enum MeterTestWorkflowKind
{
    StationTcp,
    SourceControl,
    Planned,
    SerialPortServerBaudRate,
    ControlPcbDailyTiming,
    ControlPcbCreepingStart,
    CreepingWait,
    ControlPcbCreepingRead,
    CreepingPulseJudge,
    BasicErrorPoint,
    StartingErrorPoint,
    BluetoothStationTcp,
    DeviceSelfCheck,
    ControlPcbStartingError,
    StartingTimeWait,
    ControlPcbStartingErrorRead,
    StartingErrorJudge,
    ConstantEnergyRead,
    ControlPcbWalkingStart,
    ConstantWait,
    ConstantVoltageSource,
    ConstantImaxSource,
    ControlPcbWalkingStop,
    ControlPcbWalkingRead,
    ConstantResultJudge,
    LedEffectTest
}

/// <summary>
/// 根据 XML 配置中的 executionMode/responseParser 判断测试小项应该进入哪个流程。
/// 后续新增测试项时优先扩展此类，不再把判断逻辑继续堆到 MeterTest 窗体里。
/// </summary>
internal static class MeterTestWorkflowRouter
{
    /// <summary>
    /// 将测试小项解析为统一流程类型；无法识别时默认走普通工位 TCP 一发一收。
    /// </summary>
    public static MeterTestWorkflowKind Resolve(MeterTestSubItem subItem)
    {
        MeterTestExecutionMode? executionMode = ParseExecutionMode(subItem);
        return executionMode switch
        {
            MeterTestExecutionMode.ControlPcbDeviceSelfCheck => MeterTestWorkflowKind.DeviceSelfCheck,
            MeterTestExecutionMode.BasicErrorPoint => MeterTestWorkflowKind.BasicErrorPoint,
            MeterTestExecutionMode.StartingErrorPoint => MeterTestWorkflowKind.StartingErrorPoint,
            MeterTestExecutionMode.BluetoothStationTcp => MeterTestWorkflowKind.BluetoothStationTcp,
            MeterTestExecutionMode.StartingSource => MeterTestWorkflowKind.SourceControl,
            MeterTestExecutionMode.CreepingSource => MeterTestWorkflowKind.SourceControl,
            MeterTestExecutionMode.ConstantImaxSource => MeterTestWorkflowKind.ConstantImaxSource,
            MeterTestExecutionMode.ConstantVoltageSource => MeterTestWorkflowKind.ConstantVoltageSource,
            MeterTestExecutionMode.Planned => MeterTestWorkflowKind.Planned,
            MeterTestExecutionMode.SerialPortServerBaudRateSync => MeterTestWorkflowKind.SerialPortServerBaudRate,
            MeterTestExecutionMode.ControlPcbDailyTiming => MeterTestWorkflowKind.ControlPcbDailyTiming,
            MeterTestExecutionMode.ControlPcbCreepingStart => MeterTestWorkflowKind.ControlPcbCreepingStart,
            MeterTestExecutionMode.CreepingWait => MeterTestWorkflowKind.CreepingWait,
            MeterTestExecutionMode.ControlPcbCreepingRead => MeterTestWorkflowKind.ControlPcbCreepingRead,
            MeterTestExecutionMode.CreepingPulseJudge => MeterTestWorkflowKind.CreepingPulseJudge,
            MeterTestExecutionMode.ControlPcbStartingError => MeterTestWorkflowKind.ControlPcbStartingError,
            MeterTestExecutionMode.StartingTimeWait => MeterTestWorkflowKind.StartingTimeWait,
            MeterTestExecutionMode.ControlPcbStartingErrorRead => MeterTestWorkflowKind.ControlPcbStartingErrorRead,
            MeterTestExecutionMode.StartingErrorJudge => MeterTestWorkflowKind.StartingErrorJudge,
            MeterTestExecutionMode.ConstantEnergyReadStart => MeterTestWorkflowKind.ConstantEnergyRead,
            MeterTestExecutionMode.ConstantEnergyReadEnd => MeterTestWorkflowKind.ConstantEnergyRead,
            MeterTestExecutionMode.ControlPcbWalkingStart => MeterTestWorkflowKind.ControlPcbWalkingStart,
            MeterTestExecutionMode.ConstantWait => MeterTestWorkflowKind.ConstantWait,
            MeterTestExecutionMode.ControlPcbWalkingStop => MeterTestWorkflowKind.ControlPcbWalkingStop,
            MeterTestExecutionMode.ControlPcbWalkingRead => MeterTestWorkflowKind.ControlPcbWalkingRead,
            MeterTestExecutionMode.ConstantResultJudge => MeterTestWorkflowKind.ConstantResultJudge,
            MeterTestExecutionMode.LedEffectTest => MeterTestWorkflowKind.LedEffectTest,
            _ => MeterTestWorkflowKind.StationTcp
        };
    }

    /// <summary>
    /// 判断当前小项是否需要先调用源控制服务。
    /// 显式配置 sourceControlConfig 的小项也归入源控制流程，兼容旧配置。
    /// </summary>
    public static bool RequiresSourceControl(MeterTestSubItem subItem)
    {
        if (!string.IsNullOrWhiteSpace(subItem.SourceControlConfig))
            return true;

        MeterTestWorkflowKind workflowKind = Resolve(subItem);
        return workflowKind is MeterTestWorkflowKind.SourceControl
            or MeterTestWorkflowKind.ConstantImaxSource
            or MeterTestWorkflowKind.ConstantVoltageSource;
    }

    /// <summary>
    /// 起动、潜动、常数试验中源控制步骤展示到日志里的固定步骤标题。
    /// </summary>
    public static string? GetFiveStepSourceTitle(MeterTestSubItem subItem)
    {
        MeterTestExecutionMode? executionMode = ParseExecutionMode(subItem);
        return executionMode switch
        {
            MeterTestExecutionMode.StartingSource => "升源（启动电流）",
            MeterTestExecutionMode.CreepingSource => "升源（潜动电压）",
            MeterTestExecutionMode.ConstantImaxSource => "升源（基础电压、Imax电流）",
            MeterTestExecutionMode.ConstantVoltageSource => "升源（基础电压）",
            _ => null
        };
    }

    /// <summary>判断测试小项是否需要走 698 广播地址解析器。</summary>
    public static bool UsesSgcc698BroadcastAddressParser(MeterTestSubItem subItem)
    {
        return Enum.TryParse(subItem.ResponseParser, true, out ResponseParserType parserType)
            && parserType == ResponseParserType.Sgcc698BroadcastAddress;
    }

    /// <summary>判断当前小项是否是起动试验的启动电流升源步骤。</summary>
    public static bool IsStartingSource(MeterTestSubItem subItem)
    {
        return ParseExecutionMode(subItem) == MeterTestExecutionMode.StartingSource;
    }

    /// <summary>判断当前小项是否是潜动试验的1.1倍额定电压升源步骤。</summary>
    public static bool IsCreepingSource(MeterTestSubItem subItem)
    {
        return ParseExecutionMode(subItem) == MeterTestExecutionMode.CreepingSource;
    }

    /// <summary>判断指定小项是否属于某个流程类型。</summary>
    public static bool Is(MeterTestSubItem subItem, MeterTestWorkflowKind workflowKind)
    {
        return Resolve(subItem) == workflowKind;
    }

    /// <summary>解析 executionMode，非法或空配置返回 null。</summary>
    private static MeterTestExecutionMode? ParseExecutionMode(MeterTestSubItem subItem)
    {
        return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
            ? executionMode
            : null;
    }
}
