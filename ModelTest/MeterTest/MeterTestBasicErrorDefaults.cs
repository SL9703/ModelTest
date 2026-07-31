namespace ModelTest.MeterTest;

/// <summary>基本误差流程的统一固定参数。</summary>
public static class MeterTestBasicErrorDefaults
{
    /// <summary>0x38基本误差协议中脉冲数为1字节，允许范围是1-255；0仅表示配置自动计算。</summary>
    public const int MaxPulseCount = byte.MaxValue;

    /// <summary>全部试验次数的理论时间结束后统一增加的结果计算余量，单位秒。</summary>
    public const int WaitPaddingSeconds = 20;
}
