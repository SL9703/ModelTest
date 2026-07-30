using System;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// 单个工位的通信参数。
/// 从资产信息和工位通信配置合并得到，测试流程服务只依赖这个轻量模型。
/// </summary>
internal sealed record StationCommunicationConfig(
    int StationNo,
    string Ip,
    int Port,
    string MeterAddress,
    string BaudRate);

/// <summary>
/// 当前被执行的小项上下文。
/// 把 Scheme、TestItem、TestSubItem 三层信息打包，便于流程服务脱离 WinForms 控件执行。
/// </summary>
internal sealed record SelectedSubItemContext(
    string SchemeName,
    string TestItemName,
    MeterTestSubItem SubItem);

/// <summary>
/// 控制 PCB 流程中的目标工位与控制板表位地址。
/// 多个测试服务共用这个轻量模型，避免在主窗体里保留重复的私有 record。
/// </summary>
internal sealed record ControlPcbStationTarget(
    int StationNo,
    byte MeterAddress);

/// <summary>
/// 0x37+AA 返回的常数试验走字结果。
/// PulseCount 是待测表脉冲数，StandardEnergyKwh 是控制PCB上传的标准表电能量。
/// </summary>
internal sealed record ConstantWalkingMeasurement(
    uint PulseCount,
    decimal StandardEnergyKwh);

/// <summary>
/// 常数试验电量读取时，匹配到的完整 698 帧及解析结果。
/// </summary>
internal sealed record EnergyReadResponse(
    string ResponseHex,
    Sgcc698EnergyReadParseResult ParseResult);

/// <summary>
/// 0x25+AA 返回的单个工位实际脉冲数，协议类型为4字节无符号整数。
/// </summary>
internal sealed record CreepingPulseMeasurement(
    uint PulseCount);

/// <summary>
/// 测试流程服务执行一个方案小项后的统一结果。
/// Success 用于更新红绿灯，Message 用于过程表和提示，ElapsedMilliseconds 用于结果时间统计。
/// </summary>
internal sealed record MeterTestFlowStepResult(
    bool Success,
    string Message,
    long ElapsedMilliseconds)
{
    /// <summary>使用方法开始时的 Tick 值快速构造失败结果。</summary>
    public static MeterTestFlowStepResult Fail(string message, long startTicks) =>
        new(false, message, Math.Max(0, Environment.TickCount64 - startTicks));
}
