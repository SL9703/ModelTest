using System.Net;
using System.Text;

namespace ModelTest.Protocol;

/// <summary>
/// 串口服务器通信协议 V2.0。
///
/// 串口服务器使用检测板卡 V2 帧格式：
/// 55 44 + 长度(小端) + 方向 + 地址 + 协议类型 + 命令码 + 数据项 + 校验和 + AA BB。
/// 本类只负责报文构造、校验和计算和应答解析，不负责 TCP/串口连接。
/// </summary>
public sealed class SerialPortServerProtocolV2
{
    /// <summary>协议帧起始字符。</summary>
    public const byte StartByte1 = 0x55;

    /// <summary>协议帧起始字符。</summary>
    public const byte StartByte2 = 0x44;

    /// <summary>协议帧结束字符。</summary>
    public const byte EndByte1 = 0xAA;

    /// <summary>协议帧结束字符。</summary>
    public const byte EndByte2 = 0xBB;

    /// <summary>下行方向：PC/主机发送到串口服务器。</summary>
    public const byte DownlinkDirection = 0x00;

    /// <summary>上行方向：串口服务器返回到 PC/主机。</summary>
    public const byte UplinkDirection = 0x01;

    /// <summary>串口服务器命令码。</summary>
    public const byte SerialServerCommand = 0xDF;

    /// <summary>电表控制协议类型：bit0=0，设备类型为电表。</summary>
    public const byte MeterControlProtocolType = 0x02;

    /// <summary>终端控制协议类型：bit0=0，设备类型为终端。</summary>
    public const byte TerminalControlProtocolType = 0x04;

    /// <summary>示例串口服务器报文使用的协议类型，兼容终端配置示例。</summary>
    public const byte SerialServerControlProtocolType = TerminalControlProtocolType;

    /// <summary>
    /// 读取 COM 参数时，通道 0 对应的 TCP 端口。
    /// 后续通道按 951、952……顺序映射。
    /// </summary>
    public const int FirstMappedTcpPort = 951;

    /// <summary>串口服务器协议默认 COM 通道数量。</summary>
    public const int DefaultComPortCount = 16;

    private const byte UnlockDataItem = 0x02;
    private const byte SetParameterDataItem = 0x0B;
    private const byte ReadParameterDataItem = 0x0C;
    private const byte SaveRestartDataItem = 0x0D;
    private const byte SetPortDataItem = 0x0E;
    private const byte SetIpDataItem = 0x0F;
    private const byte SerialParameterPayloadLength = 0x06;
    private const byte ImmediateApply = 0x01;
    private const byte SevenDataBits = 0x00;
    private const byte EightDataBits = 0x01;
    private const byte OneStopBit = 0x00;
    private const byte TwoStopBits = 0x01;

    private static readonly Encoding AsciiEncoding = Encoding.ASCII;

    /// <summary>
    /// 构造串口服务器控制协议帧。
    /// </summary>
    /// <param name="address">串口服务器板卡地址，通常为 01。</param>
    /// <param name="commandCode">命令码，串口服务器固定为 DF。</param>
    /// <param name="data">命令数据项。</param>
    /// <param name="protocolType">协议类型，示例使用 04。</param>
    public byte[] BuildControlFrame(
        byte address,
        byte commandCode,
        ReadOnlySpan<byte> data,
        byte protocolType = SerialServerControlProtocolType)
    {
        ValidateAddress(address);

        // 长度字段本身 2 字节，后面包含方向、地址、协议类型、命令码、数据项和校验和。
        int dataLength = 7 + data.Length;
        if (dataLength > ushort.MaxValue)
        {
            throw new ArgumentException("串口服务器数据项过长。", nameof(data));
        }

        byte[] frame = new byte[dataLength + 4];
        frame[0] = StartByte1;
        frame[1] = StartByte2;
        frame[2] = (byte)(dataLength & 0xFF);
        frame[3] = (byte)((dataLength >> 8) & 0xFF);
        frame[4] = DownlinkDirection;
        frame[5] = address;
        frame[6] = protocolType;
        frame[7] = commandCode;
        data.CopyTo(frame.AsSpan(8));

        int checksumIndex = 8 + data.Length;
        frame[checksumIndex] = CalculateChecksum(frame.AsSpan(2, checksumIndex - 2));
        frame[checksumIndex + 1] = EndByte1;
        frame[checksumIndex + 2] = EndByte2;
        return frame;
    }

    /// <summary>
    /// 构造底层解锁报文。
    /// 示例：admin 对应 55 44 10 00 00 01 04 DF FF 02 06 61 64 6D 69 6E 00 04 AA BB。
    /// </summary>
    public byte[] BuildUnlockFrame(
        byte address = 0x01,
        string password = "admin",
        byte protocolType = SerialServerControlProtocolType)
    {
        byte[] passwordBytes = AsciiEncoding.GetBytes(password ?? string.Empty);
        if (passwordBytes.Length > byte.MaxValue - 1)
        {
            throw new ArgumentException("解锁密码过长。", nameof(password));
        }

        byte[] data = new byte[2 + 1 + passwordBytes.Length + 1];
        data[0] = 0xFF;
        data[1] = UnlockDataItem;
        data[2] = (byte)(passwordBytes.Length + 1);
        passwordBytes.CopyTo(data, 3);
        data[^1] = 0x00;
        return BuildControlFrame(address, SerialServerCommand, data, protocolType);
    }

    /// <summary>
    /// 构造设置串口波特率报文。
    /// 数据项格式：FF 0B 06 COM口 立即生效 波特率 数据位 停止位 校验位。
    /// </summary>
    /// <param name="address">串口服务器板卡地址。</param>
    /// <param name="comPort">COM 通道，从 0 开始。</param>
    /// <param name="baudRate">波特率，支持协议规定的 300 到 115200。</param>
    /// <param name="dataBits">数据位，支持 7 或 8。</param>
    /// <param name="stopBits">停止位，支持 1 或 2。</param>
    /// <param name="parity">校验位：None/Even/Odd。</param>
    /// <param name="applyImmediately">是否立即生效；false 表示仅写入参数，需保存重启后生效。</param>
    public byte[] BuildSetBaudRateFrame(
        byte address,
        byte comPort,
        int baudRate,
        int dataBits = 8,
        int stopBits = 1,
        SerialPortServerParity parity = SerialPortServerParity.None,
        bool applyImmediately = true,
        byte protocolType = MeterControlProtocolType)
    {
        byte baudCode = GetBaudRateCode(baudRate);
        byte dataBitsCode = dataBits switch
        {
            7 => SevenDataBits,
            8 => EightDataBits,
            _ => throw new ArgumentOutOfRangeException(nameof(dataBits), "数据位只支持 7 或 8。")
        };
        byte stopBitsCode = stopBits switch
        {
            1 => OneStopBit,
            2 => TwoStopBits,
            _ => throw new ArgumentOutOfRangeException(nameof(stopBits), "停止位只支持 1 或 2。")
        };

        byte[] data =
        {
            0xFF,
            SetParameterDataItem,
            SerialParameterPayloadLength,
            comPort,
            applyImmediately ? ImmediateApply : (byte)0x00,
            baudCode,
            dataBitsCode,
            stopBitsCode,
            (byte)parity
        };
        return BuildControlFrame(address, SerialServerCommand, data, protocolType);
    }

    /// <summary>
    /// 构造保存并重启报文。
    /// 数据项为 FF 0D 06 saver 00。
    /// </summary>
    public byte[] BuildSaveRestartFrame(
        byte address = 0x01,
        byte protocolType = MeterControlProtocolType)
    {
        byte[] data = { 0xFF, SaveRestartDataItem, 0x06, (byte)'s', (byte)'a', (byte)'v', (byte)'e', (byte)'r', 0x00 };
        return BuildControlFrame(address, SerialServerCommand, data, protocolType);
    }

    /// <summary>
    /// 构造读取串口参数报文。
    /// parameterType 使用 0B 读取串口参数，0E 读取端口参数，0F 读取调试参数。
    /// </summary>
    public byte[] BuildReadParametersFrame(
        byte address = 0x01,
        byte parameterType = SetParameterDataItem,
        byte protocolType = MeterControlProtocolType)
    {
        if (parameterType is not (SetParameterDataItem or SetPortDataItem or SetIpDataItem))
        {
            throw new ArgumentOutOfRangeException(nameof(parameterType), "读取参数类型只支持 0B、0E 或 0F。");
        }

        byte[] data =
        {
            0xFF,
            ReadParameterDataItem,
            0x06,
            (byte)'r',
            (byte)'e',
            (byte)'a',
            (byte)'d',
            parameterType,
            0x00
        };
        return BuildControlFrame(address, SerialServerCommand, data, protocolType);
    }

    /// <summary>
    /// 构造设置串口服务器端口报文。
    /// 端口使用大端序，例如 4001 为 0F A1。
    /// </summary>
    public byte[] BuildSetPortFrame(
        byte address,
        byte comPort,
        int port,
        byte protocolType = MeterControlProtocolType)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), "端口范围必须是 1-65535。");
        }

        byte[] data =
        {
            0xFF,
            SetPortDataItem,
            0x06,
            comPort,
            (byte)((port >> 8) & 0xFF),
            (byte)(port & 0xFF),
            0x00,
            0x00,
            0x00
        };
        return BuildControlFrame(address, SerialServerCommand, data, protocolType);
    }

    /// <summary>
    /// 构造设置串口服务器 IP 报文。
    /// </summary>
    public byte[] BuildSetIpFrame(
        byte address,
        string ipAddress,
        byte protocolType = MeterControlProtocolType)
    {
        if (!IPAddress.TryParse(ipAddress?.Trim(), out IPAddress? ip) ||
            ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
        {
            throw new ArgumentException("IP 地址必须是合法的 IPv4 地址。", nameof(ipAddress));
        }

        byte[] ipBytes = ip.GetAddressBytes();
        byte[] data =
        {
            0xFF,
            SetIpDataItem,
            0x06,
            ipBytes[0],
            ipBytes[1],
            ipBytes[2],
            ipBytes[3],
            0x00,
            0x00
        };
        return BuildControlFrame(address, SerialServerCommand, data, protocolType);
    }

    /// <summary>
    /// 解析串口服务器 V2 应答帧，并校验起止符、长度和累加校验和。
    /// </summary>
    public bool TryParseFrame(
        ReadOnlySpan<byte> frame,
        out SerialPortServerFrame? parsed,
        out string error)
    {
        parsed = null;
        error = string.Empty;

        if (frame.Length < 11)
        {
            error = "串口服务器报文长度不足。";
            return false;
        }

        if (frame[0] != StartByte1 || frame[1] != StartByte2 ||
            frame[^2] != EndByte1 || frame[^1] != EndByte2)
        {
            error = "串口服务器报文起始符或结束符错误。";
            return false;
        }

        int dataLength = frame[2] | (frame[3] << 8);
        if (dataLength != frame.Length - 4)
        {
            error = $"串口服务器报文长度错误，声明={dataLength}，实际={frame.Length - 4}。";
            return false;
        }

        int checksumIndex = frame.Length - 3;
        byte expectedChecksum = CalculateChecksum(frame.Slice(2, checksumIndex - 2));
        if (frame[checksumIndex] != expectedChecksum)
        {
            error = $"串口服务器校验和错误，期望={expectedChecksum:X2}，实际={frame[checksumIndex]:X2}。";
            return false;
        }

        parsed = new SerialPortServerFrame(
            frame[4],
            frame[5],
            frame[6],
            frame[7],
            frame.Slice(8, checksumIndex - 8).ToArray(),
            frame[checksumIndex]);
        return true;
    }

    /// <summary>
    /// 解析 FF 0C 读取串口参数的应答。
    /// 每组参数为 4 字节：波特率码、数据位、停止位、校验位。
    /// 由于协议数据没有返回 TCP 端口号，这里按 951 + 通道号生成展示端口。
    /// </summary>
    public bool TryParseSerialPortParameters(
        ReadOnlySpan<byte> frame,
        out IReadOnlyList<SerialPortServerPortSetting> settings,
        out string error,
        byte expectedProtocolType = MeterControlProtocolType)
    {
        settings = Array.Empty<SerialPortServerPortSetting>();
        error = string.Empty;

        if (!TryParseFrame(frame, out SerialPortServerFrame? parsed, out error))
        {
            return false;
        }

        if (parsed is null ||
            parsed.ProtocolType != expectedProtocolType ||
            parsed.CommandCode != SerialServerCommand ||
            parsed.Data.Length < 3 ||
            parsed.Data[0] != 0xFF ||
            parsed.Data[1] != ReadParameterDataItem)
        {
            error = $"不是期望的串口服务器 FF 0C 参数读取应答，协议类型应为 {expectedProtocolType:X2}。";
            return false;
        }

        int parameterLength = parsed.Data[2];
        int availableBytes = parsed.Data.Length - 3;
        int settingCount = Math.Min(DefaultComPortCount, availableBytes / 4);
        if (settingCount == 0)
        {
            error = "串口服务器参数读取应答中没有有效 COM 参数。";
            return false;
        }

        List<SerialPortServerPortSetting> parsedSettings = new(settingCount);
        for (int channel = 0; channel < settingCount; channel++)
        {
            int offset = 3 + channel * 4;
            byte baudCode = parsed.Data[offset];
            int? baudRate = TryGetBaudRate(baudCode, out int resolvedBaudRate)
                ? resolvedBaudRate
                : null;

            parsedSettings.Add(new SerialPortServerPortSetting(
                channel,
                FirstMappedTcpPort + channel,
                baudCode,
                baudRate,
                parsed.Data[offset + 1],
                parsed.Data[offset + 2],
                parsed.Data[offset + 3]));
        }

        settings = parsedSettings;
        if (parameterLength < settingCount * 4)
        {
            error = $"串口参数读取成功，但返回长度标识为 {parameterLength}，实际解析 {settingCount} 个通道。";
        }

        return true;
    }

    /// <summary>
    /// 把协议规定的波特率转换为配置码。
    /// </summary>
    public static byte GetBaudRateCode(int baudRate)
    {
        return baudRate switch
        {
            300 => 0x00,
            600 => 0x01,
            1200 => 0x02,
            2400 => 0x03,
            4800 => 0x04,
            9600 => 0x05,
            19200 => 0x06,
            38400 => 0x07,
            57600 => 0x08,
            115200 => 0x09,
            _ => throw new ArgumentOutOfRangeException(nameof(baudRate), "不支持的串口服务器波特率。")
        };
    }

    /// <summary>
    /// 根据协议码读取波特率。
    /// </summary>
    public static bool TryGetBaudRate(byte baudCode, out int baudRate)
    {
        (bool success, int value) = baudCode switch
        {
            0x00 => (true, 300),
            0x01 => (true, 600),
            0x02 => (true, 1200),
            0x03 => (true, 2400),
            0x04 => (true, 4800),
            0x05 => (true, 9600),
            0x06 => (true, 19200),
            0x07 => (true, 38400),
            0x08 => (true, 57600),
            0x09 => (true, 115200),
            _ => (false, 0)
        };

        baudRate = value;
        return success;
    }

    /// <summary>
    /// 计算串口服务器协议校验和：从长度字段开始，累加到数据项末尾，取低字节。
    /// </summary>
    public static byte CalculateChecksum(ReadOnlySpan<byte> data)
    {
        int sum = 0;
        foreach (byte value in data)
        {
            sum = (sum + value) & 0xFF;
        }

        return (byte)sum;
    }

    /// <summary>
    /// 输出空格分隔的十六进制报文，便于日志记录。
    /// </summary>
    public static string ToHexString(ReadOnlySpan<byte> data)
    {
        return Convert.ToHexString(data).Chunk(2).Select(pair => new string(pair.ToArray())).Aggregate(string.Empty, (current, value) => current.Length == 0 ? value : $"{current} {value}");
    }

    private static void ValidateAddress(byte address)
    {
        if (address == 0x00)
        {
            throw new ArgumentOutOfRangeException(nameof(address), "串口服务器地址必须是 01-FF。");
        }
    }
}

/// <summary>
/// 串口服务器校验位编码。
/// </summary>
public enum SerialPortServerParity : byte
{
    /// <summary>无校验。</summary>
    None = 0x00,

    /// <summary>偶校验。</summary>
    Even = 0x01,

    /// <summary>奇校验。</summary>
    Odd = 0x02
}

/// <summary>
/// 已通过基础协议校验的串口服务器帧。
/// </summary>
public sealed record SerialPortServerFrame(
    byte Direction,
    byte Address,
    byte ProtocolType,
    byte CommandCode,
    byte[] Data,
    byte Checksum);

/// <summary>
/// 串口服务器一个 COM 通道的串口参数及其端口映射。
/// </summary>
public sealed record SerialPortServerPortSetting(
    int Channel,
    int TcpPort,
    byte BaudRateCode,
    int? BaudRate,
    byte DataBitsCode,
    byte StopBitsCode,
    byte ParityCode);
