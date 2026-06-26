using System.Globalization;

namespace ModelTest.Protocol;

/// <summary>
/// 检测装置电测板卡通信协议 V2.0。
/// 该类只负责协议帧拼装和解析，UI 控件只调用此类。
/// </summary>
public sealed class DetectionBoardProtocolV2
{
    public const byte StartByte1 = 0x55;
    public const byte StartByte2 = 0x44;
    public const byte EndByte1 = 0xAA;
    public const byte EndByte2 = 0xBB;
    public const byte DownlinkDirection = 0x00;
    public const byte UplinkDirection = 0x01;
    public const byte BroadcastAddress = 0xFF;

    private const int FixedFrameLength = 10;

    /// <summary>
    /// 构造控制协议帧。
    /// </summary>
    public byte[] BuildControlFrame(byte address, byte deviceType, byte commandCode, ReadOnlySpan<byte> data)
    {
        byte protocolType = BuildProtocolType(false, deviceType);
        return BuildFrame(DownlinkDirection, address, protocolType, commandCode, data);
    }

    /// <summary>
    /// 构造透传协议帧。commandCode 表示软件透传通道。
    /// </summary>
    public byte[] BuildTransparentFrame(byte address, byte deviceType, byte transparentChannel, ReadOnlySpan<byte> transparentData)
    {
        byte protocolType = BuildProtocolType(true, deviceType);
        return BuildFrame(DownlinkDirection, address, protocolType, transparentChannel, transparentData);
    }

    /// <summary>
    /// 构造终端单片机复位命令帧。
    /// 命令码固定为 0x00，设备类型固定为终端(2)，数据项为空，地址默认 0x01。
    /// </summary>
    public byte[] BuildTerminalResetFrame(byte address = 0x01)
    {
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x00, ReadOnlySpan<byte>.Empty);
    }

    /// <summary>
    /// 构造终端读取版本命令帧。
    /// 命令码固定为 0xDE，设备类型固定为终端(2)，数据项为空。
    /// </summary>
    public byte[] BuildTerminalVersionFrame(byte address)
    {
        return BuildControlFrame(address, deviceType: 2, commandCode: 0xDE, ReadOnlySpan<byte>.Empty);
    }

    /// <summary>
    /// 构造真实模组直流上电/断电命令帧。
    /// 命令码 0x01，数据项 2 字节：模块号(1~5)、1 上电 / 0 断电。
    /// </summary>
    public byte[] BuildRealModuleDcPowerFrame(byte address, byte moduleNumber, bool powerOn)
    {
        ValidateModuleNumber(moduleNumber);
        byte[] data = { moduleNumber, powerOn ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x01, data);
    }

    /// <summary>
    /// 构造真实模组交流 220V 上电/断电命令帧。
    /// 命令码 0x02，数据项 2 字节：模块号(1~5)、1 上电 / 0 断电。
    /// </summary>
    public byte[] BuildRealModuleAcPowerFrame(byte address, byte moduleNumber, bool powerOn)
    {
        ValidateModuleNumber(moduleNumber);
        byte[] data = { moduleNumber, powerOn ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x02, data);
    }

    /// <summary>
    /// 构造载波模组直流上电/断电命令帧。
    /// 命令码 0x03，数据项 2 字节：模块号(1~5)、1 上电 / 0 断电。
    /// </summary>
    public byte[] BuildCarrierModuleDcPowerFrame(byte address, byte moduleNumber, bool powerOn)
    {
        ValidateModuleNumber(moduleNumber);
        byte[] data = { moduleNumber, powerOn ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x03, data);
    }

    /// <summary>
    /// 构造载波模组交流 220V 上电/断电命令帧。
    /// 命令码 0x04，数据项 2 字节：模块号(1~5)、1 上电 / 0 断电。
    /// </summary>
    public byte[] BuildCarrierModuleAcPowerFrame(byte address, byte moduleNumber, bool powerOn)
    {
        ValidateModuleNumber(moduleNumber);
        byte[] data = { moduleNumber, powerOn ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x04, data);
    }

    /// <summary>
    /// 构造载波模组引脚电平设置命令帧。
    /// 命令码 0x05，数据项 3 字节：模块号(1~5)、引脚类型(1 RST/2 SET/3 EVENT)、电平(1 高/0 低)。
    /// </summary>
    public byte[] BuildCarrierModulePinLevelSetFrame(byte address, byte moduleNumber, byte pinType, bool highLevel)
    {
        ValidateModuleNumber(moduleNumber);
        ValidateCarrierPinType(pinType);
        byte[] data = { moduleNumber, pinType, highLevel ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x05, data);
    }

    /// <summary>
    /// 构造载波模组引脚电平读取命令帧。
    /// 命令码 0x06，数据项 2 字节：模块号(1~5)、固定读取 STA 引脚(1)。
    /// </summary>
    public byte[] BuildCarrierModulePinLevelReadFrame(byte address, byte moduleNumber)
    {
        ValidateModuleNumber(moduleNumber);
        byte[] data = { moduleNumber, 0x01 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x06, data);
    }

    /// <summary>
    /// 构造读取模组直流电压/电流/功耗命令帧。
    /// 命令码 0x07，数据项 4 字节：模组类型、模块号、读取项、读取方式/频率。
    /// </summary>
    public byte[] BuildCarrierModuleDcMeasureFrame(byte address, byte moduleType, byte moduleNumber, byte readItem, byte readModeOrRate)
    {
        ValidateCarrierModuleType(moduleType);
        ValidateModuleNumber(moduleNumber);
        ValidateCarrierMeasureReadItem(readItem);
        byte[] data = { moduleType, moduleNumber, readItem, readModeOrRate };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x07, data);
    }

    /// <summary>
    /// 解析 0x07 的数据项。
    /// </summary>
    public bool TryParseCarrierModuleDcMeasureData(ReadOnlySpan<byte> data, out CarrierModuleDcMeasureData? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;

        if (data.Length < 4)
        {
            error = "读取模组数据长度不足。";
            return false;
        }

        byte moduleType = data[0];
        byte moduleNumber = data[1];
        byte readItem = data[2];
        byte readModeOrRate = data[3];

        if (!IsValidCarrierModuleType(moduleType))
        {
            error = "模组类型错误。";
            return false;
        }

        if (!IsValidModuleNumber(moduleNumber))
        {
            error = "模块号错误。";
            return false;
        }

        if (!IsValidCarrierMeasureReadItem(readItem))
        {
            error = "读取项错误。";
            return false;
        }

        parsed = new CarrierModuleDcMeasureData(moduleType, moduleNumber, readItem, readModeOrRate, data.Length > 4 ? data[4..].ToArray() : []);
        return true;
    }

    /// <summary>
    /// 构造真实模组/虚拟模组切换命令帧。
    /// 命令码 0x10，数据项 2 字节：模组类型(1 虚拟/2 真实)、固定切换动作(1)。
    /// </summary>
    public byte[] BuildModuleModeSwitchFrame(byte address, byte moduleMode)
    {
        ValidateModuleMode(moduleMode);
        byte[] data = { moduleMode, 0x01 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x10, data);
    }

    /// <summary>
    /// 构造设置虚拟模组类型命令帧。
    /// 命令码 0x11，数据项 2 字节：模式(1 互换性/2 APP)、类型码。
    /// </summary>
    public byte[] BuildVirtualModuleTypeFrame(byte address, byte mode, byte typeCode)
    {
        ValidateVirtualModuleMode(mode);
        ValidateVirtualModuleTypeCode(mode, typeCode);
        byte[] data = { mode, typeCode };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x11, data);
    }

    /// <summary>
    /// 构造设置虚拟模组 USB 连接状态命令帧。
    /// 命令码 0x12，数据项 1 字节：0x01 恢复连接，0x00 断开连接，0x02 USB 重启。
    /// </summary>
    public byte[] BuildVirtualModuleUsbStateFrame(byte address, byte usbState)
    {
        ValidateVirtualModuleUsbState(usbState);
        byte[] data = { usbState };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x12, data);
    }

    /// <summary>
    /// 构造设置虚拟模组带载命令帧。
    /// 命令码 0x13，数据项 2 字节：带载类型、带载状态。
    /// </summary>
    public byte[] BuildVirtualModuleLoadFrame(byte address, byte loadType, bool enabled)
    {
        ValidateVirtualModuleLoadType(loadType);
        byte[] data = { loadType, enabled ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x13, data);
    }

    /// <summary>
    /// 构造设置虚拟模组纹波连接命令帧。
    /// 命令码 0x14，数据项 1 字节：0x00 断开，0x01 连接。
    /// </summary>
    public byte[] BuildVirtualModuleRippleConnectionFrame(byte address, byte rippleState)
    {
        ValidateVirtualModuleRippleState(rippleState);
        byte[] data = { rippleState };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x14, data);
    }

    /// <summary>
    /// 构造读取虚拟模组接口电压命令帧。
    /// 命令码 0x15，数据项 1 字节：0x01 单次读取，0x02 连续读取，0x00 停止连续读取。
    /// </summary>
    public byte[] BuildVirtualModuleInterfaceVoltageFrame(byte address, byte readMode)
    {
        ValidateVirtualModuleInterfaceVoltageReadMode(readMode);
        byte[] data = { readMode };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x15, data);
    }

    /// <summary>
    /// 构造设置虚拟模块状态管脚命令帧。
    /// 命令码 0x16，数据项 1 字节：0x00 无模块，0x01 有模块。
    /// </summary>
    public byte[] BuildVirtualModuleStatusPinFrame(byte address, byte moduleState)
    {
        ValidateVirtualModuleStatusPinState(moduleState);
        byte[] data = { moduleState };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x16, data);
    }

    /// <summary>
    /// 构造读取虚拟模块引脚电平和发生时间命令帧。
    /// 命令码 0x17，数据项 2 字节：引脚类型、读取序列号。
    /// </summary>
    public byte[] BuildVirtualModulePinLevelAndTimeFrame(byte address, byte pinType, byte sequenceNumber)
    {
        ValidateVirtualModulePinType(pinType);
        ValidateVirtualModulePinSequence(pinType, sequenceNumber);
        byte[] data = { pinType, sequenceNumber };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x17, data);
    }

    /// <summary>
    /// 构造清空虚拟模块引脚电平和发生时间缓存命令帧。
    /// 命令码 0x18，数据项 1 字节：bit0 清空 ON/OFF，bit1 清空 RST。
    /// </summary>
    public byte[] BuildVirtualModulePinCacheClearFrame(byte address, byte clearMask)
    {
        ValidateVirtualModulePinCacheClearMask(clearMask);
        byte[] data = { clearMask };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x18, data);
    }

    /// <summary>
    /// 构造设置虚拟模组主动上报运行模式命令帧。
    /// 命令码 0x19，数据项 1 字节：0x01 表示通过串口2主动上报运行模式。
    /// </summary>
    public byte[] BuildVirtualModuleActiveReportModeFrame(byte address, byte mode)
    {
        ValidateVirtualModuleActiveReportMode(mode);
        byte[] data = { mode };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x19, data);
    }

    /// <summary>
    /// 构造交流电压控制命令帧。
    /// 命令码 0x20，数据项 1 字节：bit0~bit2 分别表示 UA/UB/UC 上电状态。
    /// </summary>
    public byte[] BuildAcVoltageControlFrame(byte address, byte phaseMask)
    {
        ValidateThreePhaseMask(phaseMask, nameof(phaseMask));
        byte[] data = { phaseMask };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x20, data);
    }

    /// <summary>
    /// 构造交流电流控制命令帧。
    /// 命令码 0x21，数据项 1 字节：bit0~bit2 分别表示 IA/IB/IC 接入表位状态。
    /// </summary>
    public byte[] BuildAcCurrentControlFrame(byte address, byte phaseMask)
    {
        ValidateThreePhaseMask(phaseMask, nameof(phaseMask));
        byte[] data = { phaseMask };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x21, data);
    }

    /// <summary>
    /// 构造零线电流切换命令帧。
    /// 命令码 0x22，数据项 1 字节：0x01 C相电流切到 IN 侧，0x00 C相电流切到 IC 侧。
    /// </summary>
    public byte[] BuildNeutralCurrentSwitchFrame(byte address, byte switchState)
    {
        ValidateNeutralCurrentSwitchState(switchState);
        byte[] data = { switchState };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x22, data);
    }

    /// <summary>
    /// 构造读取表位电压回路功率命令帧。
    /// 命令码 0x24，数据项 1 字节：0x01 A相、0x02 B相、0x03 C相、0x04 合相、0xAA 校准/保存、0xBB 复位。
    /// </summary>
    public byte[] BuildMeterVoltageLoopPowerFrame(byte address, byte operation)
    {
        ValidateMeterVoltageLoopPowerOperation(operation);
        byte[] data = { operation };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x24, data);
    }

    /// <summary>
    /// 构造电机压接命令帧。
    /// 命令码 0x25，数据项 2 字节：电机类型(1 表位电机/2 永磁铁电机)、动作(1 压接/0 断开)。
    /// </summary>
    public byte[] BuildMotorPressFrame(byte address, byte motorType, bool pressed)
    {
        ValidateMotorType(motorType);
        byte[] data = { motorType, pressed ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x25, data);
    }

    /// <summary>
    /// 构造遥信状态量控制命令帧。命令码 0x26，数据项 2 字节。
    /// </summary>
    public byte[] BuildRemoteSignalStateFrame(byte address, ushort stateMask)
    {
        byte[] data = { (byte)(stateMask & 0xFF), (byte)((stateMask >> 8) & 0xFF) };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x26, data);
    }

    /// <summary>
    /// 构造脉冲参数设置状态命令帧。命令码 0x27，数据项 11 字节。
    /// </summary>
    public byte[] BuildPulseParameterFrame(byte address, byte channelMask, bool start, uint frequencyHz, uint pulseCount, byte dutyCycle)
    {
        ValidateLowNibbleMask(channelMask, nameof(channelMask), "脉冲通道只能使用 bit0~bit3。");
        ValidateDutyCycle(dutyCycle);
        byte[] data = new byte[11];
        data[0] = channelMask;
        data[1] = start ? (byte)0x01 : (byte)0x00;
        WriteUInt32LittleEndian(data, 2, frequencyHz);
        WriteUInt32LittleEndian(data, 6, pulseCount);
        data[10] = dutyCycle;
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x27, data);
    }

    /// <summary>
    /// 构造切换终端 RS485/RS232/CAN 命令帧。命令码 0x28。
    /// </summary>
    public byte[] BuildTerminalPortSwitchFrame(byte address, byte switchType, byte target)
    {
        ValidateTerminalPortSwitchType(switchType);
        if (target is not (0x00 or 0x01))
            throw new ArgumentOutOfRangeException(nameof(target), "切换目标必须是 0x00 或 0x01。");

        byte[] data = { switchType, target };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x28, data);
    }

    /// <summary>
    /// 构造切换 USB 状态命令帧。命令码 0x29。
    /// </summary>
    public byte[] BuildUsbSwitchFrame(byte address, bool enabled)
    {
        byte[] data = { enabled ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x29, data);
    }

    /// <summary>
    /// 构造遥信干扰脉冲测试命令帧。命令码 0x2A。
    /// </summary>
    public byte[] BuildRemoteSignalInterferencePulseFrame(byte address, byte channelMask, ushort durationMs)
    {
        byte[] data = { channelMask, (byte)(durationMs & 0xFF), (byte)((durationMs >> 8) & 0xFF) };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x2A, data);
    }

    /// <summary>
    /// 构造遥信防抖测试命令帧。命令码 0x2B。
    /// </summary>
    public byte[] BuildRemoteSignalDebounceFrame(byte address, byte channelMask, ushort durationMs, byte intervalMs)
    {
        byte[] data = { channelMask, (byte)(durationMs & 0xFF), (byte)((durationMs >> 8) & 0xFF), intervalMs };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x2B, data);
    }

    /// <summary>
    /// 构造遥信雪崩测试命令帧。命令码 0x2C。
    /// </summary>
    public byte[] BuildRemoteSignalAvalancheFrame(byte address, byte channelMask, byte changeCount, byte intervalSeconds)
    {
        ValidateAvalancheChangeCount(changeCount);
        if (intervalSeconds == 0)
            throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "雪崩测试间隔时间必须是 1~255 秒。");

        byte[] data = { channelMask, changeCount, intervalSeconds };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x2C, data);
    }

    /// <summary>
    /// 构造修改 CAN 波特率命令帧。命令码 0x2D。
    /// </summary>
    public byte[] BuildCanBaudRateFrame(byte address, byte canMask, ushort baudRateKbps)
    {
        ValidateLowNibbleMask(canMask, nameof(canMask), "CAN 通道只能使用 bit0~bit1。");
        ValidateCanBaudRate(baudRateKbps);
        byte[] data = { canMask, (byte)(baudRateKbps & 0xFF), (byte)((baudRateKbps >> 8) & 0xFF) };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x2D, data);
    }

    /// <summary>
    /// 构造温湿度数据读取命令帧。命令码 0x2E。
    /// </summary>
    public byte[] BuildTemperatureHumidityReadFrame(byte address)
    {
        byte[] data = { 0x01 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x2E, data);
    }

    public byte[] BuildIndicatorLightControlFrame(byte address, byte color, byte ledType, byte ledMask, byte mode, ushort blinkTimeMs)
    {
        byte[] data = { color, ledType, ledMask, mode, (byte)(blinkTimeMs & 0xFF), (byte)((blinkTimeMs >> 8) & 0xFF) };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x2F, data);
    }

    public byte[] BuildTerminalTypeSetFrame(byte address, byte terminalType)
    {
        byte[] data = { terminalType };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x30, data);
    }

    public byte[] BuildAuxiliaryPowerLoadFrame(byte address, byte channelMask, bool enabled)
    {
        byte[] data = { channelMask, enabled ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x31, data);
    }

    public byte[] BuildAuxiliaryPowerVoltageFrame(byte address, byte channel, byte operation, ushort calibrationMv)
    {
        byte[] data = { channel, operation, (byte)(calibrationMv & 0xFF), (byte)((calibrationMv >> 8) & 0xFF) };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x32, data);
    }

    public byte[] BuildSmaInterfaceControlFrame(byte address, byte state)
    {
        byte[] data = { state };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x33, data);
    }

    public byte[] BuildSourceMeterAmplifierPowerFrame(byte address, byte target, bool powerOn)
    {
        byte[] data = { target, powerOn ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x34, data);
    }

    public byte[] BuildSourceSwitchFrame(byte address, bool electricSource)
    {
        byte[] data = { electricSource ? (byte)0x01 : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x35, data);
    }

    public byte[] BuildSinglePhaseMeterSourceAccessFrame(byte address, byte accessState)
    {
        byte[] data = { accessState };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x36, data);
    }

    public byte[] BuildCurrentTimeFrame(byte address, bool setTime, DateTime time)
    {
        byte[] data = new byte[7];
        data[0] = setTime ? (byte)0x01 : (byte)0x00;
        if (setTime)
        {
            data[1] = (byte)(time.Year - 2000);
            data[2] = (byte)time.Month;
            data[3] = (byte)time.Day;
            data[4] = (byte)time.Hour;
            data[5] = (byte)time.Minute;
            data[6] = (byte)time.Second;
        }

        return BuildControlFrame(address, deviceType: 2, commandCode: 0x37, data);
    }

    public byte[] BuildSamplingVoltageFrame(byte address, byte voltageType, byte operation, ushort calibrationMv)
    {
        byte[] data = operation == 0x80
            ? [voltageType, operation, (byte)(calibrationMv & 0xFF), (byte)((calibrationMv >> 8) & 0xFF)]
            : [voltageType, operation];
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x38, data);
    }

    public byte[] BuildPanelRemovalInfoFrame(byte address, bool clear)
    {
        byte[] data = { clear ? (byte)0x0F : (byte)0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x39, data);
    }

    public byte[] BuildGroundFaultTestFrame(byte address, byte phase)
    {
        byte[] data = { phase };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x3A, data);
    }

    public byte[] BuildPtResetFrame(byte address)
    {
        byte[] data = { 0x01 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x3B, data);
    }

    public byte[] BuildLoopSelfCheckFrame(byte address, byte operation)
    {
        byte[] data = { operation };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x3C, data);
    }

    /// <summary>
    /// 构造误差实验命令帧。命令码 0x3D，数据项 5 字节。
    /// </summary>
    public byte[] BuildErrorExperimentFrame(byte address, byte experimentType, byte experimentMode, byte action, ushort count)
    {
        byte[] data =
        {
            experimentType,
            experimentMode,
            action,
            (byte)(count & 0xFF),
            (byte)((count >> 8) & 0xFF)
        };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x3D, data);
    }

    /// <summary>
    /// 构造常数设置命令帧。命令码 0x3E，数据项 6 字节。
    /// </summary>
    public byte[] BuildConstantSetFrame(byte address, byte constantType, ulong value)
    {
        if (constantType is < 0x01 or > 0x05)
            throw new ArgumentOutOfRangeException(nameof(constantType), "常数类型必须是 0x01~0x05。");

        byte[] data =
        {
            constantType,
            (byte)(value & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 24) & 0xFF),
            (byte)((value >> 32) & 0xFF)
        };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x3E, data);
    }

    public byte[] BuildImpedanceBoxControlFrame(byte address, byte voltagePhaseMask, byte itemCode, byte functionCode)
    {
        byte[] data = { voltagePhaseMask, itemCode, functionCode };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x50, data);
    }

    /// <summary>
    /// 构造遥控状态量读取命令帧。命令码 0x46，数据项固定 0x00。
    /// </summary>
    public byte[] BuildRemoteControlStatusReadFrame(byte address)
    {
        byte[] data = { 0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x46, data);
    }

    /// <summary>
    /// 构造遥控状态脉冲时间读取命令帧。命令码 0x47，数据项固定 0x00。
    /// </summary>
    public byte[] BuildRemoteControlPulseTimeReadFrame(byte address)
    {
        byte[] data = { 0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x47, data);
    }

    /// <summary>
    /// 构造电能表控制与反馈信号控制命令帧。命令码 0x48。
    /// </summary>
    public byte[] BuildMeterControlFeedbackSignalFrame(byte address, byte signalType, byte operation, byte value)
    {
        if (signalType is not (0x01 or 0x02))
            throw new ArgumentOutOfRangeException(nameof(signalType), "信号类型必须是 0x01 控制信号或 0x02 反馈信号。");

        if (operation is not (0x00 or 0x01))
            throw new ArgumentOutOfRangeException(nameof(operation), "操作类型必须是 0x00 读取状态或 0x01 设置状态。");

        if (signalType == 0x01 && operation == 0x00)
            throw new ArgumentOutOfRangeException(nameof(operation), "控制信号不支持读取状态。");

        if (value is not (0x00 or 0x01))
            throw new ArgumentOutOfRangeException(nameof(value), "设置值必须是 0x00 关或 0x01 开。");

        byte[] data = { signalType, operation, value };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x48, data);
    }

    /// <summary>
    /// 构造告警状态量读取命令帧。命令码 0x49，数据项固定 0x00。
    /// </summary>
    public byte[] BuildAlarmStatusReadFrame(byte address)
    {
        byte[] data = { 0x00 };
        return BuildControlFrame(address, deviceType: 2, commandCode: 0x49, data);
    }

    public bool TryParseTemperatureHumidityData(ReadOnlySpan<byte> data, out TemperatureHumidityData? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;

        if (data.Length != 8)
        {
            error = $"温湿度数据长度错误。期望8字节，实际{data.Length}字节。";
            return false;
        }

        parsed = new TemperatureHumidityData(ReadSingleLittleEndian(data[0..4]), ReadSingleLittleEndian(data[4..8]));
        return true;
    }

    /// <summary>
    /// 解析 0x24 的 13 字节返回数据。
    /// </summary>
    public bool TryParseMeterVoltageLoopPowerData(ReadOnlySpan<byte> data, out MeterVoltageLoopPowerData? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;

        if (data.Length != 13)
        {
            error = $"表位电压回路功率数据长度错误。期望13字节，实际{data.Length}字节。";
            return false;
        }

        byte phase = data[0];
        if (!IsValidMeterVoltageLoopPowerReadPhase(phase))
        {
            error = "表位电压回路功率读取相位错误。";
            return false;
        }

        parsed = new MeterVoltageLoopPowerData(
            phase,
            ReadSingleLittleEndian(data[1..5]),
            ReadSingleLittleEndian(data[5..9]),
            ReadSingleLittleEndian(data[9..13]));
        return true;
    }

    /// <summary>
    /// 构造完整协议帧。
    /// 数据长度为“数据长度字段”到“校验和”的字节数，不包含起始字符和结束字符。
    /// </summary>
    public byte[] BuildFrame(byte direction, byte address, byte protocolType, byte commandCode, ReadOnlySpan<byte> data)
    {
        ValidateDirection(direction);
        ValidateAddress(address);

        int payloadLength = 2 + 1 + 1 + 1 + 1 + data.Length + 1;
        if (payloadLength > ushort.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(data), "协议帧数据长度不能超过 65535。");

        byte[] frame = new byte[2 + payloadLength + 2];
        frame[0] = StartByte1;
        frame[1] = StartByte2;
        frame[2] = (byte)(payloadLength & 0xFF);
        frame[3] = (byte)((payloadLength >> 8) & 0xFF);
        frame[4] = direction;
        frame[5] = address;
        frame[6] = protocolType;
        frame[7] = commandCode;
        data.CopyTo(frame.AsSpan(8));
        frame[8 + data.Length] = CalculateChecksum(frame.AsSpan(2, 6 + data.Length));
        frame[9 + data.Length] = EndByte1;
        frame[10 + data.Length] = EndByte2;
        return frame;
    }

    /// <summary>
    /// 解析完整协议帧，并校验起始符、结束符、长度和校验和。
    /// </summary>
    public DetectionBoardProtocolV2Frame ParseFrame(ReadOnlySpan<byte> frame)
    {
        if (!TryParseFrame(frame, out DetectionBoardProtocolV2Frame? parsed, out string error))
            throw new FormatException(error);

        return parsed;
    }

    public bool TryParseFrame(ReadOnlySpan<byte> frame, out DetectionBoardProtocolV2Frame? parsed, out string error)
    {
        parsed = null;
        error = string.Empty;

        if (frame.Length < FixedFrameLength)
        {
            error = "报文长度不足。";
            return false;
        }

        if (frame[0] != StartByte1 || frame[1] != StartByte2)
        {
            error = "起始字符错误。";
            return false;
        }

        if (frame[^2] != EndByte1 || frame[^1] != EndByte2)
        {
            error = "结束字符错误。";
            return false;
        }

        ushort dataLength = (ushort)(frame[2] | (frame[3] << 8));
        if (frame.Length != dataLength + 4)
        {
            error = $"数据长度错误。字段长度={dataLength}，实际帧长={frame.Length}。";
            return false;
        }

        byte expectedChecksum = CalculateChecksum(frame.Slice(2, dataLength - 1));
        byte actualChecksum = frame[frame.Length - 3];
        if (expectedChecksum != actualChecksum)
        {
            error = $"校验和错误。期望={expectedChecksum:X2}，实际={actualChecksum:X2}。";
            return false;
        }

        byte direction = frame[4];
        byte address = frame[5];
        byte protocolType = frame[6];
        byte commandCode = frame[7];
        byte[] data = frame.Slice(8, dataLength - 7).ToArray();

        parsed = new DetectionBoardProtocolV2Frame(
            dataLength,
            direction,
            address,
            protocolType,
            commandCode,
            data,
            actualChecksum,
            IsTransparentProtocol(protocolType),
            GetDeviceType(protocolType));

        return true;
    }

    /// <summary>
    /// 将表位十进制数字转换为协议使用的 16 进制字节。
    /// </summary>
    public byte ConvertStationDecimalToByte(string stationText)
    {
        if (!int.TryParse(stationText.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            throw new ArgumentException("表位必须是十进制数字。", nameof(stationText));

        if (value < 0 || value > 255)
            throw new ArgumentOutOfRangeException(nameof(stationText), "表位范围必须是 0~255。");

        return (byte)value;
    }

    /// <summary>
    /// 将表位十进制数字转换为两位大写 16 进制文本，便于 UI 显示。
    /// </summary>
    public string ConvertStationDecimalToHexText(string stationText)
    {
        return ConvertStationDecimalToByte(stationText).ToString("X2", CultureInfo.InvariantCulture);
    }

    public static byte BuildProtocolType(bool transparentProtocol, byte deviceType)
    {
        if (deviceType > 0x7F)
            throw new ArgumentOutOfRangeException(nameof(deviceType), "设备类型范围必须是 0~127。");

        return (byte)((deviceType << 1) | (transparentProtocol ? 0x01 : 0x00));
    }

    public static bool IsTransparentProtocol(byte protocolType) => (protocolType & 0x01) == 0x01;

    public static byte GetDeviceType(byte protocolType) => (byte)(protocolType >> 1);

    public static string ToHexString(ReadOnlySpan<byte> data)
    {
        return BitConverter.ToString(data.ToArray()).Replace("-", " ");
    }

    private static byte CalculateChecksum(ReadOnlySpan<byte> data)
    {
        int sum = 0;
        foreach (byte item in data)
        {
            sum += item;
        }

        return (byte)(sum & 0xFF);
    }

    private static void ValidateDirection(byte direction)
    {
        if (direction != DownlinkDirection && direction != UplinkDirection)
            throw new ArgumentOutOfRangeException(nameof(direction), "数据方向只能是 0x00 或 0x01。");
    }

    private static void ValidateAddress(byte address)
    {
        if (address == 0)
            throw new ArgumentOutOfRangeException(nameof(address), "地址有效范围为 1~254，0xFF 为广播地址。");
    }

    private static void ValidateModuleNumber(byte moduleNumber)
    {
        if (!IsValidModuleNumber(moduleNumber))
            throw new ArgumentOutOfRangeException(nameof(moduleNumber), "模块号范围必须是 1~5。");
    }

    private static void ValidateCarrierPinType(byte pinType)
    {
        if (pinType is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(pinType), "引脚类型必须是 1~3，对应 RST/SET/EVENT。");
    }

    private static void ValidateCarrierModuleType(byte moduleType)
    {
        if (!IsValidCarrierModuleType(moduleType))
            throw new ArgumentOutOfRangeException(nameof(moduleType), "模组类型必须是 0x01 或 0x02。");
    }

    private static void ValidateCarrierMeasureReadItem(byte readItem)
    {
        if (!IsValidCarrierMeasureReadItem(readItem))
            throw new ArgumentOutOfRangeException(nameof(readItem), "读取项必须是 0x01~0x04。");
    }

    private static void ValidateModuleMode(byte moduleMode)
    {
        if (moduleMode is not (0x01 or 0x02))
            throw new ArgumentOutOfRangeException(nameof(moduleMode), "模组切换类型必须是 0x01 或 0x02。");
    }

    private static void ValidateVirtualModuleMode(byte mode)
    {
        if (mode is not (0x01 or 0x02))
            throw new ArgumentOutOfRangeException(nameof(mode), "模式必须是 0x01 或 0x02。");
    }

    private static void ValidateVirtualModuleTypeCode(byte mode, byte typeCode)
    {
        if (mode == 0x01 && typeCode is not (0x01 or 0x02 or 0x03 or 0x04 or 0x05 or 0x06))
            throw new ArgumentOutOfRangeException(nameof(typeCode), "互换性类型必须是 0x01~0x06。");

        if (mode == 0x02 && typeCode is not (0x01 or 0x02 or 0x03 or 0x04 or 0x05 or 0x06 or 0x07 or 0x08 or 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x0E))
            throw new ArgumentOutOfRangeException(nameof(typeCode), "APP 类型必须是 0x01~0x0E。");
    }

    private static void ValidateVirtualModuleUsbState(byte usbState)
    {
        if (usbState is not (0x00 or 0x01 or 0x02))
            throw new ArgumentOutOfRangeException(nameof(usbState), "USB连接状态必须是 0x00、0x01 或 0x02。");
    }

    private static void ValidateVirtualModuleLoadType(byte loadType)
    {
        if (loadType > 0x05)
            throw new ArgumentOutOfRangeException(nameof(loadType), "带载类型必须是 0x00~0x05。");
    }

    private static void ValidateVirtualModuleRippleState(byte rippleState)
    {
        if (rippleState is not (0x00 or 0x01))
            throw new ArgumentOutOfRangeException(nameof(rippleState), "纹波连接状态必须是 0x00 或 0x01。");
    }

    private static void ValidateVirtualModuleInterfaceVoltageReadMode(byte readMode)
    {
        if (readMode is not (0x00 or 0x01 or 0x02))
            throw new ArgumentOutOfRangeException(nameof(readMode), "接口电压读取模式必须是 0x00、0x01 或 0x02。");
    }

    private static void ValidateVirtualModuleStatusPinState(byte moduleState)
    {
        if (moduleState is not (0x00 or 0x01))
            throw new ArgumentOutOfRangeException(nameof(moduleState), "状态管脚只能是 0x00 或 0x01。");
    }

    private static void ValidateVirtualModulePinType(byte pinType)
    {
        if (pinType is not (0x01 or 0x02))
            throw new ArgumentOutOfRangeException(nameof(pinType), "虚拟模块引脚类型必须是 0x01 或 0x02。");
    }

    private static void ValidateVirtualModulePinSequence(byte pinType, byte sequenceNumber)
    {
        if (pinType == 0x01 && sequenceNumber is < 1 or > 3)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "ON/OFF 引脚序列号只能是 1~3。");

        if (pinType == 0x02 && sequenceNumber is < 1 or > 2)
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "RST 引脚序列号只能是 1~2。");
    }

    private static void ValidateVirtualModulePinCacheClearMask(byte clearMask)
    {
        if (clearMask > 0x03)
            throw new ArgumentOutOfRangeException(nameof(clearMask), "清空缓存掩码必须在 0x00~0x03。");
    }

    private static void ValidateVirtualModuleActiveReportMode(byte mode)
    {
        if (mode != 0x01)
            throw new ArgumentOutOfRangeException(nameof(mode), "主动上报运行模式目前只能是 0x01。");
    }

    private static void ValidateThreePhaseMask(byte phaseMask, string paramName)
    {
        if ((phaseMask & 0xF8) != 0)
            throw new ArgumentOutOfRangeException(paramName, "三相控制掩码只能使用 bit0~bit2。");
    }

    private static void ValidateNeutralCurrentSwitchState(byte switchState)
    {
        if (switchState is not (0x00 or 0x01))
            throw new ArgumentOutOfRangeException(nameof(switchState), "零线电流切换状态必须是 0x00 或 0x01。");
    }

    private static void ValidateMeterVoltageLoopPowerOperation(byte operation)
    {
        if (!IsValidMeterVoltageLoopPowerOperation(operation))
            throw new ArgumentOutOfRangeException(nameof(operation), "表位电压回路功率操作必须是 0x01、0x02、0x03、0x04、0xAA 或 0xBB。");
    }

    private static void ValidateMotorType(byte motorType)
    {
        if (motorType is not (0x01 or 0x02))
            throw new ArgumentOutOfRangeException(nameof(motorType), "电机类型必须是 0x01(表位电机) 或 0x02(永磁铁电机)。");
    }

    private static void ValidateLowNibbleMask(byte mask, string paramName, string message)
    {
        if ((mask & 0xF0) != 0 || mask == 0)
            throw new ArgumentOutOfRangeException(paramName, message);
    }

    private static void ValidateDutyCycle(byte dutyCycle)
    {
        if (dutyCycle is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(dutyCycle), "脉冲占空比必须是 1~100。");
    }

    private static void ValidateTerminalPortSwitchType(byte switchType)
    {
        if (switchType is not (0x02 or 0x03 or 0x04))
            throw new ArgumentOutOfRangeException(nameof(switchType), "切换类型必须是 0x02、0x03 或 0x04。");
    }

    private static void ValidateAvalancheChangeCount(byte changeCount)
    {
        if (changeCount is < 2 or > 254 || changeCount % 2 != 0)
            throw new ArgumentOutOfRangeException(nameof(changeCount), "雪崩测试变位次数必须是 2~254 的偶数。");
    }

    private static void ValidateCanBaudRate(ushort baudRateKbps)
    {
        if (baudRateKbps is not (10 or 25 or 50 or 100 or 125 or 250 or 500 or 1000))
            throw new ArgumentOutOfRangeException(nameof(baudRateKbps), "CAN 波特率必须是 10、25、50、100、125、250、500 或 1000K。");
    }

    private static void WriteUInt32LittleEndian(byte[] data, int startIndex, uint value)
    {
        data[startIndex] = (byte)(value & 0xFF);
        data[startIndex + 1] = (byte)((value >> 8) & 0xFF);
        data[startIndex + 2] = (byte)((value >> 16) & 0xFF);
        data[startIndex + 3] = (byte)((value >> 24) & 0xFF);
    }

    private static float ReadSingleLittleEndian(ReadOnlySpan<byte> data)
    {
        Span<byte> buffer = stackalloc byte[4];
        data.CopyTo(buffer);
        if (!BitConverter.IsLittleEndian)
        {
            buffer.Reverse();
        }

        return BitConverter.ToSingle(buffer);
    }

    private static bool IsValidModuleNumber(byte moduleNumber) => moduleNumber is >= 1 and <= 5;

    private static bool IsValidCarrierModuleType(byte moduleType) => moduleType is 0x01 or 0x02;

    private static bool IsValidCarrierMeasureReadItem(byte readItem) => readItem is >= 0x01 and <= 0x04;

    private static bool IsValidMeterVoltageLoopPowerOperation(byte operation) => IsValidMeterVoltageLoopPowerReadPhase(operation) || operation is 0xAA or 0xBB;

    private static bool IsValidMeterVoltageLoopPowerReadPhase(byte phase) => phase is >= 0x01 and <= 0x04;
}

public sealed record DetectionBoardProtocolV2Frame(
    ushort DataLength,
    byte Direction,
    byte Address,
    byte ProtocolType,
    byte CommandCode,
    byte[] Data,
    byte Checksum,
    bool IsTransparentProtocol,
    byte DeviceType);

public sealed record CarrierModuleDcMeasureData(
    byte ModuleType,
    byte ModuleNumber,
    byte ReadItem,
    byte ReadModeOrRate,
    byte[] Payload);

public sealed record MeterVoltageLoopPowerData(
    byte Phase,
    float ActivePower,
    float ReactivePower,
    float ApparentPower);

public sealed record TemperatureHumidityData(
    float Temperature,
    float Humidity);
