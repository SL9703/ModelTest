namespace ModelTest.Protocol;

/// <summary>
/// 电表控制 PCB 的共享协议定义。
/// MeterTest、工位上下电服务及电表控制界面应复用本类，避免各自维护帧常量和命令码。
/// </summary>
public static class MeterControlPcbProtocol
{
    /// <summary>V1 单字节起始符。</summary>
    public const byte V1StartByte = 0x55;

    /// <summary>V1 单字节结束符。</summary>
    public const byte V1EndByte = 0xAA;

    /// <summary>V2 起始符第1字节。</summary>
    public const byte V2StartByte1 = DetectionBoardProtocolV2.StartByte1;

    /// <summary>V2 起始符第2字节。</summary>
    public const byte V2StartByte2 = DetectionBoardProtocolV2.StartByte2;

    /// <summary>V2 结束符第1字节。</summary>
    public const byte V2EndByte1 = DetectionBoardProtocolV2.EndByte1;

    /// <summary>V2 结束符第2字节。</summary>
    public const byte V2EndByte2 = DetectionBoardProtocolV2.EndByte2;

    /// <summary>PC向MCU发送的下行方向。</summary>
    public const byte DownlinkDirection = DetectionBoardProtocolV2.DownlinkDirection;

    /// <summary>MCU向PC返回的上行方向。</summary>
    public const byte UplinkDirection = DetectionBoardProtocolV2.UplinkDirection;

    /// <summary>V1电表控制协议类型。</summary>
    public const byte V1ControlProtocolType = 0x00;

    /// <summary>V2设备类型中的电表类型值。</summary>
    public const byte MeterDeviceType = 0x01;

    /// <summary>V2电表控制协议类型：设备类型1左移一位，bit0为控制协议0。</summary>
    public const byte V2MeterControlProtocolType = MeterDeviceType << 1;

    /// <summary>V2电表透传协议类型：设备类型1左移一位，bit0为透传协议1。</summary>
    public const byte V2MeterTransparentProtocolType = (MeterDeviceType << 1) | 0x01;

    public const byte TestCommunicationCommand = 0x00;
    public const byte AcVoltageCommand = 0x01;
    public const byte AcCurrentCommand = 0x02;
    public const byte BasicError21Command = 0x21;
    public const byte CreepingTestCommand = 0x25;
    public const byte DailyTimingCommand = 0x36;
    public const byte WalkingTestCommand = 0x37;
    public const byte BasicError38Command = 0x38;
    public const byte MeterPresenceDetectionCommand = 0x84;
    public const byte VoltageShortCircuitDetectionCommand = 0x86;
    public const byte ActiveConstantCommand = 0xA0;
    public const byte ReactiveConstantCommand = 0xA1;
    public const byte StandardActiveConstantCommand = 0xA2;
    public const byte StandardReactiveConstantCommand = 0xA3;
    public const byte MotorCrimpingCommand = 0xC9;
    public const byte TemperatureCommand = 0xCA;
    public const byte FeedbackCommand = 0xFB;
    public const byte ResetCommand = 0xFF;

    /// <summary>通用操作值00：开始、压接或默认空数据项。</summary>
    public const byte StartOperation = 0x00;

    /// <summary>通用操作值01：执行、检测启动、校准或释放。</summary>
    public const byte ExecuteOperation = 0x01;

    /// <summary>通用操作值AA：读取试验或检测结果。</summary>
    public const byte ReadOperation = 0xAA;

    /// <summary>通用操作值FF：停止、断电或删除配置。</summary>
    public const byte StopOperation = 0xFF;

    /// <summary>0x38基本误差的有功脉冲类型。</summary>
    public const byte ActivePulseType = 0x00;

    /// <summary>0x38基本误差的无功脉冲类型。</summary>
    public const byte ReactivePulseType = 0x01;

    public const byte SinglePhaseEnableDataItem = 0x01;
    public const byte SinglePhaseDisableDataItem = 0x05;
    public const byte ThreePhaseEnableDataItem = 0x04;
    public const byte ThreePhaseDisableDataItem = 0x08;

    /// <summary>同一控制PCB连续发送报文的默认间隔。</summary>
    public static readonly TimeSpan DefaultPacketInterval = TimeSpan.FromMilliseconds(100);

    private static readonly DetectionBoardProtocolV2 V2Protocol = new();

    /// <summary>构造V2电表控制报文；空数据项按协议约定补一个0x00。</summary>
    public static byte[] BuildV2ControlFrame(byte address, byte command, params byte[] dataItems)
    {
        byte[] payload = dataItems is { Length: > 0 } ? dataItems : new[] { StartOperation };
        return V2Protocol.BuildControlFrame(address, MeterDeviceType, command, payload);
    }

    /// <summary>构造V1电表控制报文；空数据项按协议约定补一个0x00。</summary>
    public static byte[] BuildV1ControlFrame(byte address, byte command, params byte[] dataItems)
    {
        byte[] payload = dataItems is { Length: > 0 } ? dataItems : new[] { StartOperation };
        int frameLength = 7 + payload.Length;
        byte[] packet = new byte[frameLength + 2];
        packet[0] = V1StartByte;
        packet[1] = (byte)(frameLength & 0xFF);
        packet[2] = (byte)((frameLength >> 8) & 0xFF);
        packet[3] = DownlinkDirection;
        packet[4] = address;
        packet[5] = V1ControlProtocolType;
        packet[6] = command;
        Array.Copy(payload, 0, packet, 7, payload.Length);
        packet[frameLength] = CalculateChecksum(packet, 1, frameLength - 1);
        packet[frameLength + 1] = V1EndByte;
        return packet;
    }

    /// <summary>计算指定区间累加和的低字节。</summary>
    public static byte CalculateChecksum(byte[] data, int startIndex, int count)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (startIndex < 0 || count < 0 || startIndex + count > data.Length)
            throw new ArgumentOutOfRangeException(nameof(count), "校验区间超出报文范围。");

        int sum = 0;
        for (int index = startIndex; index < startIndex + count; index++)
        {
            sum += data[index];
        }

        return (byte)sum;
    }
}
