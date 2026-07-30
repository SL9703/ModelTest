namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 源控制公共参数。
/// 所有可能阻塞的 XYCtr 控制指令统一使用同一个等待时间，避免各流程出现不同超时行为。
/// </summary>
internal static class MeterTestSourceControlDefaults
{
    /// <summary>打开、关闭、初始化、升降源及读取源常数接口的统一等待时间。</summary>
    public static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(40);
}
