using System.Globalization;

namespace ModelTest.Protocol;

/// <summary>
/// 串口服务器通用底层协议。
/// 本类只生成“解锁 + 设置串口参数”两类原始 FF 指令，不读取当前通道参数，也不负责网络发送。
/// </summary>
public static class GenericSerialPortServerProtocol
{
    public const byte CommandPrefix = 0xFF;
    public const byte UnlockCommand = 0x02;
    public const byte SetPortCommand = 0x0B;
    public const byte ReadParametersCommand = 0x0C;
    public const byte SaveAndRestartCommand = 0x0D;
    public const byte UnlockPayloadLength = 0x06;
    public const byte SetPortPayloadLength = 0x06;
    public const byte ApplyImmediately = 0x01;
    public const int MaximumChannelIndex = 0x0F;
    public const int FirstLegacyTcpPort = 951;
    public const int FirstStandardTcpPort = 4001;
    public const int ChannelCount = 16;

    /// <summary>构造默认 admin 解锁指令：FF 02 06 61 64 6D 69 6E 00。</summary>
    public static byte[] BuildUnlockCommand(string password = "admin")
    {
        string normalizedPassword = password?.Trim() ?? string.Empty;
        if (normalizedPassword.Length == 0)
            throw new ArgumentException("串口服务器解锁密码不能为空。", nameof(password));
        if (normalizedPassword.Any(character => character > 0x7F))
            throw new ArgumentException("串口服务器解锁密码只支持ASCII字符。", nameof(password));

        byte[] passwordBytes = System.Text.Encoding.ASCII.GetBytes(normalizedPassword);
        if (passwordBytes.Length + 1 > byte.MaxValue)
            throw new ArgumentException("串口服务器解锁密码过长。", nameof(password));

        byte[] command = new byte[3 + passwordBytes.Length + 1];
        command[0] = CommandPrefix;
        command[1] = UnlockCommand;
        command[2] = (byte)(passwordBytes.Length + 1);
        passwordBytes.CopyTo(command, 3);
        command[^1] = 0x00;
        return command;
    }

    /// <summary>
    /// 构造读取串口参数指令：FF 0C 06 72 65 61 64 0B 00。
    /// 其中 read 为ASCII文本，0B 表示读取串口参数。
    /// </summary>
    public static byte[] BuildReadSerialParametersCommand()
    {
        return
        [
            CommandPrefix,
            ReadParametersCommand,
            0x06,
            0x72,
            0x65,
            0x61,
            0x64,
            SetPortCommand,
            0x00
        ];
    }

    /// <summary>
    /// 构造断电保存指令：FF 0D 06 73 61 76 65 72 00。
    /// 该指令由用户明确点击“保存”时发送，设置按钮只负责立即生效。
    /// </summary>
    public static byte[] BuildSaveAndRestartCommand()
    {
        return
        [
            CommandPrefix,
            SaveAndRestartCommand,
            0x06,
            0x73,
            0x61,
            0x76,
            0x65,
            0x72,
            0x00
        ];
    }

    /// <summary>
    /// 按通道号构造串口参数设置指令：
    /// FF 0B 06 + 通道 + 立即生效 + 波特率 + 数据位 + 停止位 + 校验位。
    /// </summary>
    /// <param name="channelIndex">COM通道索引，00-0F分别对应串口1-16。</param>
    public static byte[] BuildSetPortCommand(
        int channelIndex,
        int baudRate,
        int dataBits = 8,
        int stopBits = 1,
        SerialPortServerParity parity = SerialPortServerParity.None,
        bool applyImmediately = true)
    {
        ValidateChannelIndex(channelIndex);
        byte baudRateCode = SerialPortServerProtocolV2.GetBaudRateCode(baudRate);
        byte dataBitsCode = dataBits switch
        {
            7 => 0x00,
            8 => 0x01,
            _ => throw new ArgumentOutOfRangeException(nameof(dataBits), "数据位只支持7位或8位。")
        };
        byte stopBitsCode = stopBits switch
        {
            1 => 0x00,
            2 => 0x01,
            _ => throw new ArgumentOutOfRangeException(nameof(stopBits), "停止位只支持1位或2位。")
        };

        return
        [
            CommandPrefix,
            SetPortCommand,
            SetPortPayloadLength,
            (byte)channelIndex,
            applyImmediately ? ApplyImmediately : (byte)0x00,
            baudRateCode,
            dataBitsCode,
            stopBitsCode,
            (byte)parity
        ];
    }

    /// <summary>
    /// 根据TCP端口自动映射通道并构造设置指令。
    /// 支持951-966和4001-4016两组端口，两个区间的首端口都映射到通道00。
    /// </summary>
    public static byte[] BuildSetPortCommandForTcpPort(
        int tcpPort,
        int baudRate,
        int dataBits = 8,
        int stopBits = 1,
        SerialPortServerParity parity = SerialPortServerParity.None,
        bool applyImmediately = true)
    {
        int channelIndex = GetChannelIndex(tcpPort);
        return BuildSetPortCommand(
            channelIndex,
            baudRate,
            dataBits,
            stopBits,
            parity,
            applyImmediately);
    }

    /// <summary>按“9600-8-E-1”格式解析参数并构造设置指令。</summary>
    public static byte[] BuildSetPortCommandForTcpPort(int tcpPort, string serialProfile)
    {
        if (!TryParseSerialProfile(
                serialProfile,
                out int baudRate,
                out int dataBits,
                out SerialPortServerParity parity,
                out int stopBits,
                out string error))
        {
            throw new ArgumentException(error, nameof(serialProfile));
        }

        return BuildSetPortCommandForTcpPort(
            tcpPort,
            baudRate,
            dataBits,
            stopBits,
            parity,
            applyImmediately: true);
    }

    /// <summary>一次设置所需的两条顺序指令：先解锁，再立即生效设置。</summary>
    public static GenericSerialPortServerCommandSet BuildCommandSet(
        int tcpPort,
        string serialProfile,
        string password = "admin")
    {
        int channelIndex = GetChannelIndex(tcpPort);
        return new GenericSerialPortServerCommandSet(
            tcpPort,
            channelIndex,
            BuildUnlockCommand(password),
            BuildSetPortCommandForTcpPort(tcpPort, serialProfile));
    }

    /// <summary>把951/4001起始的TCP端口映射为00-0F通道索引。</summary>
    public static int GetChannelIndex(int tcpPort)
    {
        if (tcpPort >= FirstLegacyTcpPort && tcpPort < FirstLegacyTcpPort + ChannelCount)
            return tcpPort - FirstLegacyTcpPort;
        if (tcpPort >= FirstStandardTcpPort && tcpPort < FirstStandardTcpPort + ChannelCount)
            return tcpPort - FirstStandardTcpPort;

        throw new ArgumentOutOfRangeException(
            nameof(tcpPort),
            tcpPort,
            $"TCP端口必须位于{FirstLegacyTcpPort}-{FirstLegacyTcpPort + ChannelCount - 1}"
            + $"或{FirstStandardTcpPort}-{FirstStandardTcpPort + ChannelCount - 1}，才能映射到通道00-0F。");
    }

    /// <summary>解析“波特率-数据位-校验位-停止位”，校验位支持N/E/O。</summary>
    public static bool TryParseSerialProfile(
        string serialProfile,
        out int baudRate,
        out int dataBits,
        out SerialPortServerParity parity,
        out int stopBits,
        out string error)
    {
        baudRate = 0;
        dataBits = 0;
        stopBits = 0;
        parity = SerialPortServerParity.None;
        error = string.Empty;

        string[] parts = (serialProfile ?? string.Empty).Trim().Split(
            '-',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out baudRate) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out dataBits) ||
            !int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out stopBits))
        {
            error = $"串口参数格式无效：{serialProfile}，应为9600-8-E-1。";
            return false;
        }

        try
        {
            _ = SerialPortServerProtocolV2.GetBaudRateCode(baudRate);
        }
        catch (ArgumentOutOfRangeException)
        {
            error = $"串口服务器不支持波特率{baudRate}。";
            return false;
        }

        if (dataBits is not (7 or 8))
        {
            error = $"数据位只支持7或8，当前={dataBits}。";
            return false;
        }
        if (stopBits is not (1 or 2))
        {
            error = $"停止位只支持1或2，当前={stopBits}。";
            return false;
        }

        parity = parts[2].ToUpperInvariant() switch
        {
            "N" or "NONE" => SerialPortServerParity.None,
            "E" or "EVEN" => SerialPortServerParity.Even,
            "O" or "ODD" => SerialPortServerParity.Odd,
            _ => (SerialPortServerParity)byte.MaxValue
        };
        if ((byte)parity == byte.MaxValue)
        {
            error = $"校验位只支持N、E或O，当前={parts[2]}。";
            parity = SerialPortServerParity.None;
            return false;
        }

        return true;
    }

    /// <summary>输出空格分隔的十六进制文本，供MeterTest日志直接使用。</summary>
    public static string ToHexString(ReadOnlySpan<byte> command)
    {
        return string.Join(' ', command.ToArray().Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
    }

    /// <summary>
    /// 解析 FF0C 读取串口参数应答。
    /// 数据区按每4字节一组解析：波特率、数据位、停止位、校验位，共最多16路COM。
    /// </summary>
    public static bool TryParseReadSerialParametersResponse(
        ReadOnlySpan<byte> response,
        out List<GenericSerialPortChannelInfo> channels,
        out string error)
    {
        channels = new List<GenericSerialPortChannelInfo>();
        error = string.Empty;

        if (response.Length < 3)
        {
            error = "应答长度不足。";
            return false;
        }

        int headerIndex = IndexOfHeader(response, ReadParametersCommand);
        if (headerIndex < 0 || headerIndex + 3 > response.Length)
        {
            error = "未找到FF0C串口参数应答头。";
            return false;
        }

        int payloadLength = response[headerIndex + 2];
        int payloadStart = headerIndex + 3;
        if (payloadStart + payloadLength > response.Length)
        {
            error = $"FF0C应答长度不完整，声明={payloadLength}，实际剩余={response.Length - payloadStart}。";
            return false;
        }

        ReadOnlySpan<byte> payload = response.Slice(payloadStart, payloadLength);
        if (payload.Length % 4 != 0)
        {
            error = $"串口参数数据长度必须是4的倍数，当前={payload.Length}。";
            return false;
        }

        for (int index = 0; index < payload.Length / 4; index++)
        {
            byte baudRateCode = payload[index * 4];
            byte dataBitsCode = payload[index * 4 + 1];
            byte stopBitsCode = payload[index * 4 + 2];
            byte parityCode = payload[index * 4 + 3];
            channels.Add(new GenericSerialPortChannelInfo(
                index,
                GetTcpPortFromChannelIndex(index, FirstLegacyTcpPort),
                TryDecodeBaudRate(baudRateCode, out int baudRate) ? baudRate : 0,
                dataBitsCode == 0x00 ? 7 : 8,
                stopBitsCode == 0x01 ? 2 : 1,
                parityCode switch
                {
                    0x01 => SerialPortServerParity.Even,
                    0x02 => SerialPortServerParity.Odd,
                    _ => SerialPortServerParity.None
                },
                baudRateCode,
                dataBitsCode,
                stopBitsCode,
                parityCode));
        }

        return true;
    }

    /// <summary>把通道索引映射回TCP端口，默认951/4001两组均支持。</summary>
    public static int GetTcpPortFromChannelIndex(int channelIndex, int firstTcpPort = FirstLegacyTcpPort)
    {
        ValidateChannelIndex(channelIndex);
        return firstTcpPort + channelIndex;
    }

    /// <summary>把校验位枚举转换成N/E/O文本。</summary>
    public static string FormatParity(SerialPortServerParity parity)
    {
        return parity switch
        {
            SerialPortServerParity.Even => "E",
            SerialPortServerParity.Odd => "O",
            _ => "N"
        };
    }

    private static void ValidateChannelIndex(int channelIndex)
    {
        if (channelIndex is < 0 or > MaximumChannelIndex)
        {
            throw new ArgumentOutOfRangeException(
                nameof(channelIndex),
                channelIndex,
                "COM通道索引必须在00-0F之间，对应串口1-16。");
        }
    }

    private static int IndexOfHeader(ReadOnlySpan<byte> response, byte command)
    {
        for (int index = 0; index <= response.Length - 2; index++)
        {
            if (response[index] == CommandPrefix && response[index + 1] == command)
                return index;
        }

        return -1;
    }

    private static bool TryDecodeBaudRate(byte code, out int baudRate)
    {
        baudRate = code switch
        {
            0x00 => 300,
            0x01 => 600,
            0x02 => 1200,
            0x03 => 2400,
            0x04 => 4800,
            0x05 => 9600,
            0x06 => 19200,
            0x07 => 38400,
            0x08 => 57600,
            0x09 => 115200,
            _ => 0
        };
        return baudRate > 0;
    }
}

/// <summary>通用串口服务器一次端口设置所需的解锁和设置指令。</summary>
public sealed record GenericSerialPortServerCommandSet(
    int TcpPort,
    int ChannelIndex,
    byte[] UnlockCommand,
    byte[] SetPortCommand);

/// <summary>通用串口服务器读取到的一路COM参数。</summary>
public sealed record GenericSerialPortChannelInfo(
    int ChannelIndex,
    int TcpPort,
    int BaudRate,
    int DataBits,
    int StopBits,
    SerialPortServerParity Parity,
    byte BaudRateCode,
    byte DataBitsCode,
    byte StopBitsCode,
    byte ParityCode);
