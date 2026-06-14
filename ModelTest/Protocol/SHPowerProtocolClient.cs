using System.Globalization;
using System.Text;
using ModelTest.Socket_DLL.Socket_Client;

namespace ModelTest.Protocol;

/// <summary>
/// ZK2009 新装置电源端口协议客户端。
/// 协议特点：TCP 连接设备电源命令端口，命令内容为 ASCII 字符串，帧尾固定追加 0x0D（回车符）。
/// 默认目标端口为 1000；长数据端口 1001 使用 END 结尾，本类只封装 1000 端口命令。
/// </summary>
public sealed class SHPowerProtocolClient
{
    /// <summary>
    /// 电源命令端口，协议文档描述为 192.168.0.AAA:1000。
    /// </summary>
    public const int DefaultCommandPort = 1000;

    /// <summary>
    /// 命令帧结束符。协议要求以十六进制 0D 结尾，不是换行 0A。
    /// </summary>
    public const byte CommandTerminator = 0x0D;

    private static readonly Encoding CommandEncoding = Encoding.ASCII;
    private readonly EnhancedTcpClient _tcpClient;

    /// <summary>
    /// 初始化协议客户端。
    /// </summary>
    /// <param name="tcpClient">项目现有 TCP 客户端实例；必须已经或即将连接到设备 1000 端口。</param>
    public SHPowerProtocolClient(EnhancedTcpClient tcpClient)
    {
        _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
    }

    /// <summary>
    /// 当前 TCP 是否已连接。
    /// </summary>
    public bool IsConnected => _tcpClient.IsConnected;

    /// <summary>
    /// 连接设备电源命令端口。
    /// WinForm 控件参数：设备 IP，例如 192.168.0.123；端口默认 1000。
    /// </summary>
    /// <param name="ipAddress">设备 IP 地址，协议中的 192.168.0.AAA。</param>
    /// <param name="port">电源命令端口，默认 1000。</param>
    public Task<bool> ConnectAsync(string ipAddress, int port = DefaultCommandPort)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("设备 IP 不能为空。", nameof(ipAddress));

        ValidateRange(port, 1, 65535, nameof(port));
        return _tcpClient.ConnectAsync(ipAddress.Trim(), port);
    }

    /// <summary>
    /// 断开设备连接。
    /// </summary>
    public void Disconnect() => _tcpClient.Disconnect();

    /// <summary>
    /// 发送原始协议命令。方法会自动追加 0x0D，传入值不要包含 \r、\n 或 END。
    /// WinForm 控件参数：命令文本，例如 UION、Pum:100.000、CHECK。
    /// </summary>
    /// <param name="commandWithoutTerminator">不带结束符的 ASCII 命令正文。</param>
    public Task<bool> SendRawCommandAsync(string commandWithoutTerminator)
    {
        byte[] frame = BuildFrame(commandWithoutTerminator);
        return _tcpClient.SendBytesAsync(frame);
    }

    /// <summary>
    /// 构造 1000 端口命令帧字节，便于日志显示或单元测试。
    /// </summary>
    /// <param name="commandWithoutTerminator">不带 0x0D 的命令正文。</param>
    public static byte[] BuildFrame(string commandWithoutTerminator)
    {
        if (string.IsNullOrWhiteSpace(commandWithoutTerminator))
            throw new ArgumentException("命令不能为空。", nameof(commandWithoutTerminator));

        string command = commandWithoutTerminator.Trim();
        if (command.Contains('\r') || command.Contains('\n'))
            throw new ArgumentException("命令正文不能包含回车或换行，结束符由接口类自动追加。", nameof(commandWithoutTerminator));

        byte[] commandBytes = CommandEncoding.GetBytes(command);
        byte[] frame = new byte[commandBytes.Length + 1];
        Buffer.BlockCopy(commandBytes, 0, frame, 0, commandBytes.Length);
        frame[^1] = CommandTerminator;
        return frame;
    }

    /// <summary>
    /// 设置全部输出。对应 UION / UIOF。
    /// WinForm 控件参数：isOn=true 全部输出；false 全部关闭。
    /// </summary>
    public Task<bool> SetAllOutputAsync(bool isOn) => SendRawCommandAsync(isOn ? "UION" : "UIOF");

    /// <summary>
    /// 设置电压总输出。对应 UON / UOF。
    /// WinForm 控件参数：isOn=true 电压输出；false 电压关闭。
    /// </summary>
    public Task<bool> SetVoltageOutputAsync(bool isOn) => SendRawCommandAsync(isOn ? "UON" : "UOF");

    /// <summary>
    /// 设置电流总输出。对应 ION / IOF。
    /// WinForm 控件参数：isOn=true 电流输出；false 电流关闭。
    /// </summary>
    public Task<bool> SetCurrentOutputAsync(bool isOn) => SendRawCommandAsync(isOn ? "ION" : "IOF");

    /// <summary>
    /// 设置单相电压输出。对应 UAON/UAOF、UBON/UBOF、UCON/UCOF。
    /// WinForm 控件参数：phase=A/B/C；isOn=true 输出，false 关闭。
    /// </summary>
    public Task<bool> SetPhaseVoltageOutputAsync(ZkPhase phase, bool isOn)
    {
        string command = (phase, isOn) switch
        {
            (ZkPhase.A, true) => "UAON",
            (ZkPhase.A, false) => "UAOF",
            (ZkPhase.B, true) => "UBON",
            (ZkPhase.B, false) => "UBOF",
            (ZkPhase.C, true) => "UCON",
            (ZkPhase.C, false) => "UCOF",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "相别只允许 A、B、C。")
        };

        return SendRawCommandAsync(command);
    }

    /// <summary>
    /// 设置单相电流输出。对应 IAON/IAOF、IBON/IBOF、ICON/ICOF。
    /// WinForm 控件参数：phase=A/B/C；isOn=true 输出，false 关闭。
    /// </summary>
    public Task<bool> SetPhaseCurrentOutputAsync(ZkPhase phase, bool isOn)
    {
        string command = (phase, isOn) switch
        {
            (ZkPhase.A, true) => "IAON",
            (ZkPhase.A, false) => "IAOF",
            (ZkPhase.B, true) => "IBON",
            (ZkPhase.B, false) => "IBOF",
            (ZkPhase.C, true) => "ICON",
            (ZkPhase.C, false) => "ICOF",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "相别只允许 A、B、C。")
        };

        return SendRawCommandAsync(command);
    }

    /// <summary>
    /// 设置三相电压幅度。对应 Pum:XXX.YYY，范围 0.001~115.000。
    /// WinForm 控件参数：voltageValue，建议 NumericUpDown 设置 DecimalPlaces=3、Minimum=0.001、Maximum=115.000。
    /// </summary>
    public Task<bool> SetThreePhaseVoltageAmplitudeAsync(decimal voltageValue)
    {
        ValidateDecimalRange(voltageValue, 0.001m, 115.000m, nameof(voltageValue));
        return SendRawCommandAsync($"Pum:{FormatDecimal(voltageValue, 3)}");
    }

    /// <summary>
    /// 设置单相电压幅度。对应 PAum/PBum/PCum:XXX.YYY，范围 0.001~115.000。
    /// WinForm 控件参数：phase=A/B/C；voltageValue，三位小数。
    /// </summary>
    public Task<bool> SetPhaseVoltageAmplitudeAsync(ZkPhase phase, decimal voltageValue)
    {
        ValidateDecimalRange(voltageValue, 0.001m, 115.000m, nameof(voltageValue));
        string prefix = phase switch
        {
            ZkPhase.A => "PAum",
            ZkPhase.B => "PBum",
            ZkPhase.C => "PCum",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "相别只允许 A、B、C。")
        };

        return SendRawCommandAsync($"{prefix}:{FormatDecimal(voltageValue, 3)}");
    }

    /// <summary>
    /// 按百分比设置单相电压幅度。对应 STUA/STUB/STUC:XXXX，细度 0.1%。
    /// WinForm 控件参数：phase=A/B/C；percent，如 100.0 表示 100.0%。
    /// </summary>
    public Task<bool> SetPhaseVoltagePercentAsync(ZkPhase phase, decimal percent)
    {
        int scaled = ToOneDecimalPercentValue(percent, nameof(percent));
        string prefix = phase switch
        {
            ZkPhase.A => "STUA",
            ZkPhase.B => "STUB",
            ZkPhase.C => "STUC",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "相别只允许 A、B、C。")
        };

        return SendRawCommandAsync($"{prefix}:{scaled:0000}");
    }

    /// <summary>
    /// 设置三相电流幅度。对应 Pim:XXX.YYY，范围 0.001~过载倍数*100。
    /// WinForm 控件参数：currentValue；maxCurrentValue 由装置过载倍数计算后传入。
    /// </summary>
    public Task<bool> SetThreePhaseCurrentAmplitudeAsync(decimal currentValue, decimal maxCurrentValue)
    {
        ValidateDecimalRange(currentValue, 0.001m, maxCurrentValue, nameof(currentValue));
        return SendRawCommandAsync($"Pim:{FormatDecimal(currentValue, 3)}");
    }

    /// <summary>
    /// 设置单相电流幅度。对应 PAim/PBim/PCim:XXX.YYY，范围 0.001~过载倍数*100。
    /// WinForm 控件参数：phase=A/B/C；currentValue；maxCurrentValue 由装置过载倍数计算后传入。
    /// </summary>
    public Task<bool> SetPhaseCurrentAmplitudeAsync(ZkPhase phase, decimal currentValue, decimal maxCurrentValue)
    {
        ValidateDecimalRange(currentValue, 0.001m, maxCurrentValue, nameof(currentValue));
        string prefix = phase switch
        {
            ZkPhase.A => "PAim",
            ZkPhase.B => "PBim",
            ZkPhase.C => "PCim",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "相别只允许 A、B、C。")
        };

        return SendRawCommandAsync($"{prefix}:{FormatDecimal(currentValue, 3)}");
    }

    /// <summary>
    /// 按百分比设置单相电流幅度。对应 STIA/STIB/STIC:XXXX，细度 0.1%。
    /// WinForm 控件参数：phase=A/B/C；percent，如 100.0 表示 100.0%。
    /// </summary>
    public Task<bool> SetPhaseCurrentPercentAsync(ZkPhase phase, decimal percent)
    {
        int scaled = ToOneDecimalPercentValue(percent, nameof(percent));
        string prefix = phase switch
        {
            ZkPhase.A => "STIA",
            ZkPhase.B => "STIB",
            ZkPhase.C => "STIC",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "相别只允许 A、B、C。")
        };

        return SendRawCommandAsync($"{prefix}:{scaled:0000}");
    }

    /// <summary>
    /// 设置合相电流相位。对应 Pph:XXX.YYY，协议范围 0.01~359.99。
    /// WinForm 控件参数：angleDegree，建议 NumericUpDown 设置 DecimalPlaces=3。
    /// </summary>
    public Task<bool> SetCombinedCurrentPhaseAsync(decimal angleDegree)
    {
        ValidateDecimalRange(angleDegree, 0.01m, 359.99m, nameof(angleDegree));
        return SendRawCommandAsync($"Pph:{FormatDecimal(angleDegree, 3)}");
    }

    /// <summary>
    /// 设置 B/C 相电压相位。对应 PHUB/PHUC:XXX.YYY，协议范围 0.01~359.99。
    /// WinForm 控件参数：phase 只允许 B/C；angleDegree。
    /// </summary>
    public Task<bool> SetVoltagePhaseAsync(ZkPhase phase, decimal angleDegree)
    {
        ValidateDecimalRange(angleDegree, 0.01m, 359.99m, nameof(angleDegree));
        string prefix = phase switch
        {
            ZkPhase.B => "PHUB",
            ZkPhase.C => "PHUC",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "电压相位分相控制只支持 B 相和 C 相。")
        };

        return SendRawCommandAsync($"{prefix}:{FormatDecimal(angleDegree, 3)}");
    }

    /// <summary>
    /// 设置 A/B/C 相电流相位。对应 PHIA/PHIB/PHIC:XXX.YYY，协议范围 0.01~359.99。
    /// WinForm 控件参数：phase=A/B/C；angleDegree。
    /// </summary>
    public Task<bool> SetCurrentPhaseAsync(ZkPhase phase, decimal angleDegree)
    {
        ValidateDecimalRange(angleDegree, 0.01m, 359.99m, nameof(angleDegree));
        string prefix = phase switch
        {
            ZkPhase.A => "PHIA",
            ZkPhase.B => "PHIB",
            ZkPhase.C => "PHIC",
            _ => throw new ArgumentOutOfRangeException(nameof(phase), "相别只允许 A、B、C。")
        };

        return SendRawCommandAsync($"{prefix}:{FormatDecimal(angleDegree, 3)}");
    }

    /// <summary>
    /// 设置输出频率。对应 Pfr:XXX.YYY，协议范围 45.00~65.00。
    /// WinForm 控件参数：frequencyHz，建议 NumericUpDown 设置 DecimalPlaces=3、Minimum=45、Maximum=65。
    /// </summary>
    public Task<bool> SetFrequencyAsync(decimal frequencyHz)
    {
        ValidateDecimalRange(frequencyHz, 45.00m, 65.00m, nameof(frequencyHz));
        return SendRawCommandAsync($"Pfr:{FormatDecimal(frequencyHz, 3)}");
    }

    /// <summary>
    /// 设置校验圈数。对应 N:YY，范围 1~99。
    /// WinForm 控件参数：circleCount。
    /// </summary>
    public Task<bool> SetCheckCircleCountAsync(int circleCount)
    {
        ValidateRange(circleCount, 1, 99, nameof(circleCount));
        return SendRawCommandAsync($"N:{circleCount:00}");
    }

    /// <summary>
    /// 设置电表常数。对应 C:XXX,AAAA（三相台）或 CS:XXX,AAAA（单相台）。
    /// WinForm 控件参数：meterPosition 0/000 表示广播，1~200 表示具体表位；constant 最多 9 位，可带小数；isSinglePhaseBench 是否单相台。
    /// </summary>
    public Task<bool> SetMeterConstantAsync(int meterPosition, decimal constant, bool isSinglePhaseBench)
    {
        ValidateMeterPosition(meterPosition);
        ValidatePositiveNumberText(constant, 9, nameof(constant));
        string prefix = isSinglePhaseBench ? "CS" : "C";
        return SendRawCommandAsync($"{prefix}:{FormatMeterPosition(meterPosition)},{FormatPlainDecimal(constant)}");
    }

    /// <summary>
    /// 设置参比电压。对应 Ue:YYYY，最多 4 位。
    /// WinForm 控件参数：referenceVoltage，例如 220。
    /// </summary>
    public Task<bool> SetReferenceVoltageAsync(int referenceVoltage)
    {
        return SetReferenceVoltageAsync((decimal)referenceVoltage);
    }

    /// <summary>
    /// 设置参比电压。对应 Ue:YYYY，最多 4 位，可选 57.7、100、220、380。
    /// </summary>
    public Task<bool> SetReferenceVoltageAsync(decimal referenceVoltage)
    {
        ValidatePositiveNumberText(referenceVoltage, 4, nameof(referenceVoltage));
        return SendRawCommandAsync($"Ue:{FormatPlainDecimal(referenceVoltage)}");
    }

    /// <summary>
    /// 设置参比电压。兼容现有调用命名，协议同 Ue:YYYY。
    /// </summary>
    public Task<bool> SetRferencevottageAsync(int referenceVoltage)
    {
        return SetReferenceVoltageAsync(referenceVoltage);
    }

    /// <summary>
    /// 设置参比电压。兼容现有调用命名，协议同 Ue:YYYY。
    /// </summary>
    public Task<bool> SetRferencevottageAsync(decimal referenceVoltage)
    {
        return SetReferenceVoltageAsync(referenceVoltage);
    }

    /// <summary>
    /// 设置基本电流和额定电流。对应 Ie:AAAA(BBBB)。
    /// WinForm 控件参数：basicCurrent、ratedCurrent，最多 6 位，可带小数。
    /// </summary>
    public Task<bool> SetCurrentRangeAsync(decimal basicCurrent, decimal ratedCurrent)
    {
        ValidatePositiveNumberText(basicCurrent, 6, nameof(basicCurrent));
        ValidatePositiveNumberText(ratedCurrent, 6, nameof(ratedCurrent));
        return SendRawCommandAsync($"Ie:{FormatPlainDecimal(basicCurrent)}({FormatPlainDecimal(ratedCurrent)})");
    }

    /// <summary>
    /// 设置接线方式。对应 TYPE:A。
    /// WinForm 控件参数：wireType 使用枚举绑定下拉框。
    /// </summary>
    public Task<bool> SetWireTypeAsync(ZkWireType wireType) => SendRawCommandAsync($"TYPE:{(char)wireType}");

    /// <summary>
    /// 设置精度等级。对应 ACC:A.B。
    /// WinForm 控件参数：accuracyClass，例如 0.2、0.5、1.0。
    /// </summary>
    public Task<bool> SetAccuracyClassAsync(decimal accuracyClass)
    {
        ValidateDecimalRange(accuracyClass, 0.01m, 99.9m, nameof(accuracyClass));
        return SendRawCommandAsync($"ACC:{FormatPlainDecimal(accuracyClass)}");
    }

    /// <summary>
    /// 设置走字电能。对应 TESTC:XXX,AAAA 或 TP:XXX,AAAA，AAAA 最多 5 位，可带小数。
    /// WinForm 控件参数：meterPosition；energyKWh；useTpAlias=true 时发送 TP 命令。
    /// </summary>
    public Task<bool> SetRunningEnergyAsync(int meterPosition, decimal energyKWh, bool useTpAlias = false)
    {
        ValidateMeterPosition(meterPosition);
        ValidatePositiveNumberText(energyKWh, 5, nameof(energyKWh));
        string prefix = useTpAlias ? "TP" : "TESTC";
        return SendRawCommandAsync($"{prefix}:{FormatMeterPosition(meterPosition)},{FormatPlainDecimal(energyKWh)}");
    }

    /// <summary>
    /// 设置挂表状态。对应 SGB:XXX,A，A=1 挂表，A=2 不挂表。
    /// WinForm 控件参数：meterPosition；isMounted=true 挂表。
    /// </summary>
    public Task<bool> SetMeterMountedAsync(int meterPosition, bool isMounted)
    {
        ValidateMeterPosition(meterPosition);
        return SendRawCommandAsync($"SGB:{FormatMeterPosition(meterPosition)},{(isMounted ? 1 : 2)}");
    }

    /// <summary>
    /// 设置脉冲选择。对应 FRCH:A，A=1 光电头脉冲，A=2 电子脉冲。
    /// WinForm 控件参数：pulseSource 使用枚举绑定单选/下拉。
    /// </summary>
    public Task<bool> SetPulseSourceAsync(ZkPulseSource pulseSource) => SendRawCommandAsync($"FRCH:{(int)pulseSource}");

    /// <summary>
    /// 设置电流接入方式。对应 HGQ:A，A=1 直接接入，A=2 经互感器，A=3 经止逆器。
    /// WinForm 控件参数：mode 使用枚举绑定下拉框。
    /// </summary>
    public Task<bool> SetCurrentAccessModeAsync(ZkCurrentAccessMode mode) => SendRawCommandAsync($"HGQ:{(int)mode}");

    /// <summary>
    /// 设置被检表脉冲输出组合方式。对应 MS:A，A=1 合成两路，A=2 合成三路，A=3 合成四路。
    /// WinForm 控件参数：lineCount 2/3/4。
    /// </summary>
    public Task<bool> SetPulseMergeLineCountAsync(int lineCount)
    {
        int code = lineCount switch
        {
            2 => 1,
            3 => 2,
            4 => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(lineCount), "脉冲输出组合只允许 2、3、4 路。")
        };

        return SendRawCommandAsync($"MS:{code}");
    }

    /// <summary>
    /// 设置状态显示或功率方向。AM 实际值、PER 百分比、PS 正序、NS 负序、*PP 正功率、*NP 负功率。
    /// WinForm 控件参数：mode 使用枚举绑定按钮组。
    /// </summary>
    public Task<bool> SetStatusModeAsync(ZkStatusMode mode)
    {
        string command = mode switch
        {
            ZkStatusMode.ActualValue => "AM",
            ZkStatusMode.Percent => "PER",
            ZkStatusMode.PositiveSequence => "PS",
            ZkStatusMode.NegativeSequence => "NS",
            ZkStatusMode.PositivePower => "*PP",
            ZkStatusMode.NegativePower => "*NP",
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        return SendRawCommandAsync(command);
    }

    /// <summary>
    /// 进入或退出常用功能试验状态。
    /// WinForm 控件参数：testCommand 使用枚举绑定按钮。
    /// </summary>
    public Task<bool> ExecuteTestCommandAsync(ZkTestCommand testCommand)
    {
        string command = testCommand switch
        {
            ZkTestCommand.AlignFrontEdge => "SHBON1",
            ZkTestCommand.StopAlignAndReturnErrorCheck => "SHBOF",
            ZkTestCommand.StartTest => "STION",
            ZkTestCommand.StartCreepTest => "STUON",
            ZkTestCommand.EnterRunningEnergy => "TCON",
            ZkTestCommand.StartRunningEnergy => "TCSTR",
            ZkTestCommand.EnterVoltageLoss => "SYON",
            ZkTestCommand.StartVoltageLoss => "SYSTR",
            ZkTestCommand.ExitVoltageLoss => "QTSY",
            ZkTestCommand.EnterEnergyCounting => "ZZSTR",
            ZkTestCommand.EnterDiskError => "PZSTR",
            ZkTestCommand.EnterConstantTest => "CSSTR",
            ZkTestCommand.EnterPulseWaveform => "MCBX",
            ZkTestCommand.EnterGroundFault => "GDGZ",
            ZkTestCommand.ExitOtherTest => "TCOF",
            _ => throw new ArgumentOutOfRangeException(nameof(testCommand))
        };

        return SendRawCommandAsync(command);
    }

    /// <summary>
    /// 设置谐波点。对应 SETXP/SETUXP/SETIXP/SETUAXP 等命令。
    /// WinForm 控件参数：target 选择合相/分相电压/电流；phaseDegree 0~359.9 度；amplitudePercent 0.1~600.0%；order 2~21 次。
    /// </summary>
    public Task<bool> SetHarmonicPointAsync(ZkHarmonicTarget target, decimal phaseDegree, decimal amplitudePercent, int order)
    {
        ValidateDecimalRange(phaseDegree, 0m, 359.9m, nameof(phaseDegree));
        ValidateDecimalRange(amplitudePercent, 0.1m, 600.0m, nameof(amplitudePercent));
        ValidateRange(order, 2, 21, nameof(order));

        string prefix = target switch
        {
            ZkHarmonicTarget.AllVoltageAndCurrent => "SETXP",
            ZkHarmonicTarget.AllVoltage => "SETUXP",
            ZkHarmonicTarget.AllCurrent => "SETIXP",
            ZkHarmonicTarget.PhaseAVoltage => "SETUAXP",
            ZkHarmonicTarget.PhaseBVoltage => "SETUBXP",
            ZkHarmonicTarget.PhaseCVoltage => "SETUCXP",
            ZkHarmonicTarget.PhaseACurrent => "SETIAXP",
            ZkHarmonicTarget.PhaseBCurrent => "SETIBXP",
            ZkHarmonicTarget.PhaseCCurrent => "SETICXP",
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };

        int phaseCode = (int)Math.Round(phaseDegree * 10m, MidpointRounding.AwayFromZero);
        int amplitudeCode = (int)Math.Round(amplitudePercent * 10m, MidpointRounding.AwayFromZero);
        return SendRawCommandAsync($"{prefix}:{phaseCode:0000}.{amplitudeCode:0000}.{order:00}");
    }

    /// <summary>
    /// 计算谐波输出相别。对应 MADEXP:AB。
    /// WinForm 控件参数：布尔值 ua/ub/uc/ia/ib/ic，勾选哪个相别就置 true。
    /// </summary>
    public Task<bool> ApplyHarmonicPhaseMaskAsync(bool ua, bool ub, bool uc, bool ia, bool ib, bool ic)
    {
        int mask = 0;
        if (ua) mask |= 1 << 0;
        if (ub) mask |= 1 << 1;
        if (uc) mask |= 1 << 2;
        if (ia) mask |= 1 << 3;
        if (ib) mask |= 1 << 4;
        if (ic) mask |= 1 << 5;

        return SendRawCommandAsync($"MADEXP:{mask:00}");
    }

    /// <summary>
    /// 退出谐波输出。对应 QTXP。
    /// </summary>
    public Task<bool> ExitHarmonicAsync() => SendRawCommandAsync("QTXP");

    /// <summary>
    /// 清除谐波。对应 CLRXP；协议要求先退出谐波，再清除谐波。
    /// </summary>
    public Task<bool> ClearHarmonicAsync() => SendRawCommandAsync("CLRXP");

    /// <summary>
    /// 查询或控制杂项命令，例如 VER、CHECK、HEART、RELINK、RSTPWR、CHKWIP。
    /// WinForm 控件参数：miscCommand 使用枚举绑定按钮。
    /// </summary>
    public Task<bool> ExecuteMiscCommandAsync(ZkMiscCommand miscCommand)
    {
        string command = miscCommand switch
        {
            ZkMiscCommand.ReadVersion => "VER",
            ZkMiscCommand.CheckStatus => "CHECK",
            ZkMiscCommand.Heart => "HEART",
            ZkMiscCommand.Relink => "RELINK",
            ZkMiscCommand.ResetPowerBox => "RSTPWR",
            ZkMiscCommand.ClearCurrentEnergy => "CLRPW",
            ZkMiscCommand.ReadSourceIp => "CHKWIP",
            ZkMiscCommand.ReadSourceGateway => "CHKWGW",
            ZkMiscCommand.ReadSourceMac => "CHKWMAC",
            ZkMiscCommand.ReadDeviceConfig => "RDWCFG",
            _ => throw new ArgumentOutOfRangeException(nameof(miscCommand))
        };

        return SendRawCommandAsync(command);
    }

    /// <summary>
    /// 设置表位通信状态查询。对应 CKMSTS:XXX。
    /// WinForm 控件参数：meterPosition=0 查询全部表位；1~200 查询指定表位。
    /// </summary>
    public Task<bool> QueryMeterCommunicationStatusAsync(int meterPosition)
    {
        ValidateMeterPosition(meterPosition);
        return SendRawCommandAsync($"CKMSTS:{meterPosition}");
    }

    /// <summary>
    /// 设置表位指示灯。对应 LIGHT:XXX,AB。
    /// WinForm 控件参数：meterPosition；checkLamp 检验状态灯；withstandVoltageLamp 耐压状态灯。
    /// </summary>
    public Task<bool> SetMeterLampAsync(int meterPosition, ZkLampState checkLamp, ZkLampState withstandVoltageLamp)
    {
        ValidateMeterPosition(meterPosition);
        return SendRawCommandAsync($"LIGHT:{FormatMeterPosition(meterPosition)},{(int)checkLamp}{(int)withstandVoltageLamp}");
    }

    /// <summary>
    /// 设置输出脉冲参数。对应 PULSET:XXX,A,BBBB,CCCC,D。
    /// WinForm 控件参数：meterPosition；channel=0 全部/1 脉冲1/2 脉冲2；periodMs 周期；widthMs 宽度；levelMode 低/高电平。
    /// </summary>
    public Task<bool> SetOutputPulseParameterAsync(int meterPosition, int channel, int periodMs, int widthMs, ZkPulseLevelMode levelMode)
    {
        ValidateMeterPosition(meterPosition);
        ValidateRange(channel, 0, 2, nameof(channel));
        ValidateRange(periodMs, 1, 9999, nameof(periodMs));
        ValidateRange(widthMs, 1, 9999, nameof(widthMs));
        return SendRawCommandAsync($"PULSET:{FormatMeterPosition(meterPosition)},{channel},{periodMs:0000},{widthMs:0000},{(int)levelMode}");
    }

    /// <summary>
    /// 设置输出脉冲运行。对应 PULRUN:XXX,AB,CCC。
    /// WinForm 控件参数：meterPosition；channel=1/2；isRunning 是否开始；pulseCount 脉冲个数。
    /// </summary>
    public Task<bool> SetOutputPulseRunAsync(int meterPosition, int channel, bool isRunning, int pulseCount)
    {
        ValidateMeterPosition(meterPosition);
        ValidateRange(channel, 1, 2, nameof(channel));
        ValidateRange(pulseCount, 0, 999, nameof(pulseCount));
        return SendRawCommandAsync($"PULRUN:{FormatMeterPosition(meterPosition)},{channel}{(isRunning ? 1 : 0)},{pulseCount:000}");
    }

    private static string FormatMeterPosition(int meterPosition)
    {
        ValidateMeterPosition(meterPosition);
        return meterPosition == 0 ? "000" : meterPosition.ToString("000", CultureInfo.InvariantCulture);
    }

    private static string FormatDecimal(decimal value, int decimalPlaces)
    {
        return value.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
    }

    private static string FormatPlainDecimal(decimal value)
    {
        return value.ToString("0.################", CultureInfo.InvariantCulture);
    }

    private static int ToOneDecimalPercentValue(decimal percent, string paramName)
    {
        ValidateDecimalRange(percent, 0m, 999.9m, paramName);
        return (int)Math.Round(percent * 10m, MidpointRounding.AwayFromZero);
    }

    private static void ValidateMeterPosition(int meterPosition)
    {
        ValidateRange(meterPosition, 0, 200, nameof(meterPosition));
    }

    private static void ValidateRange(int value, int min, int max, string paramName)
    {
        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, value, $"参数范围必须为 {min}~{max}。");
    }

    private static void ValidateDecimalRange(decimal value, decimal min, decimal max, string paramName)
    {
        if (max < min)
            throw new ArgumentOutOfRangeException(paramName, max, "最大值不能小于最小值。");

        if (value < min || value > max)
            throw new ArgumentOutOfRangeException(paramName, value, $"参数范围必须为 {min}~{max}。");
    }

    private static void ValidatePositiveNumberText(decimal value, int maxLength, string paramName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, "参数必须大于 0。");

        string text = FormatPlainDecimal(value);
        int digitCount = text.Count(char.IsDigit);
        if (digitCount > maxLength)
            throw new ArgumentOutOfRangeException(paramName, value, $"参数最多允许 {maxLength} 位数字。");
    }
}

/// <summary>
/// 三相相别。
/// </summary>
public enum ZkPhase
{
    A,
    B,
    C
}

/// <summary>
/// 接线方式，对应 TYPE:A 中的 A。
/// </summary>
public enum ZkWireType
{
    单相有功 = '0',
    三相四线有功 = '1',
    三相三线有功 = '2',
    三相四线跨线无功90度三元件无功 = '3',
    三相三线移相60度无功60度两元件无功 = '4',
    三相三线跨线无功90度两元件无功 = '5',
    三相四线真无功 = '6',
    三相三线真无功 = '7',
    三相三线有功UAUBUC按三相四线输出UB对地不为0 = '8',
    三相三线无功输出按三相三线输出UB接U0 = '9',
    三相三线人工中心点无功 = ':',
    单相方式无功 = ';'
}

/// <summary>
/// 脉冲来源，对应 FRCH:A。
/// </summary>
public enum ZkPulseSource
{
    PhotoelectricHead = 1,
    ElectronicPulse = 2
}

/// <summary>
/// 电流接入方式，对应 HGQ:A。
/// </summary>
public enum ZkCurrentAccessMode
{
    Direct = 1,
    Transformer = 2,
    ReverseStopper = 3
}

/// <summary>
/// 电源显示和功率方向控制。
/// </summary>
public enum ZkStatusMode
{
    ActualValue,
    Percent,
    PositiveSequence,
    NegativeSequence,
    PositivePower,
    NegativePower
}

/// <summary>
/// 常用功能试验命令。
/// </summary>
public enum ZkTestCommand
{
    AlignFrontEdge,
    StopAlignAndReturnErrorCheck,
    StartTest,
    StartCreepTest,
    EnterRunningEnergy,
    StartRunningEnergy,
    EnterVoltageLoss,
    StartVoltageLoss,
    ExitVoltageLoss,
    EnterEnergyCounting,
    EnterDiskError,
    EnterConstantTest,
    EnterPulseWaveform,
    EnterGroundFault,
    ExitOtherTest
}

/// <summary>
/// 谐波设置目标。
/// </summary>
public enum ZkHarmonicTarget
{
    AllVoltageAndCurrent,
    AllVoltage,
    AllCurrent,
    PhaseAVoltage,
    PhaseBVoltage,
    PhaseCVoltage,
    PhaseACurrent,
    PhaseBCurrent,
    PhaseCCurrent
}

/// <summary>
/// 常用杂项命令。
/// </summary>
public enum ZkMiscCommand
{
    ReadVersion,
    CheckStatus,
    Heart,
    Relink,
    ResetPowerBox,
    ClearCurrentEnergy,
    ReadSourceIp,
    ReadSourceGateway,
    ReadSourceMac,
    ReadDeviceConfig
}

/// <summary>
/// 表位指示灯状态，对应 LIGHT 命令中的灯状态字符。
/// </summary>
public enum ZkLampState
{
    Off = 0,
    Yellow = 1,
    Green = 2,
    Red = 3
}

/// <summary>
/// 输出脉冲电平模式。
/// </summary>
public enum ZkPulseLevelMode
{
    LowLevelPulse = 0,
    HighLevelPulse = 1
}
