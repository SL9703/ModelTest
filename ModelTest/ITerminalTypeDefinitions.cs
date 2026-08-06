using System.ComponentModel;

namespace ModelTest;

public interface ITerminalTypeDefinitions
{
    public enum TerminalClass : byte
    {
        [Description("专变III")]
        Terminal1 = 0x01,
        [Description("集中器")]
        Terminal2 = 0x02,
        [Description("(模组化)专变")]
        Terminal3 = 0x03,
        [Description("智能融合终端")]
        Terminal4 = 0x04,
        [Description("单相物联网表")]
        Terminal5 = 0x05,
        [Description("三相物联网表")]
        Terminal6 = 0x06,
        [Description("单相智能电表")]
        Terminal7 = 0x07,
        [Description("三相智能电表")]
        Terminal8 = 0x08
    }

    public enum TerminalV1Class : byte
    {
        [Description("断开-无终端类型")]
        Terminal0 = 0x00,
        [Description("台区智能融合终端")]
        Terminal1 = 0x01,
        [Description("13版集中器I型")]
        Terminal2 = 0x02,
        [Description("13版专变III型")]
        Terminal3 = 0x03,
        [Description("22版集中器I型")]
        Terminal4 = 0x04,
        [Description("22版专变III型")]
        Terminal5 = 0x05,
        [Description("22版能源控制器")]
        Terminal6 = 0x06,
        [Description("南网-负荷管理终端")]
        Terminal7 = 0x07,
        [Description("南网-配变监测计量终端")]
        Terminal8 = 0x08,
        [Description("南网-13集中器")]
        Terminal9 = 0x09,
        [Description("智能融合终端-IFT")]
        Terminal10 = 0x0A
    }
}
