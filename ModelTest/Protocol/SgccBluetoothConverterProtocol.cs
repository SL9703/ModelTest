using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ModelTest.Protocol;

/// <summary>
/// 国网智芯蓝牙/脉冲转换器协议。
/// 帧格式：7E 7E 7E 5A + 长度 + 功能码 + 数据 + CS + 7E A5。
/// 长度从起始符计算到数据内容末尾，因此为 6 + 数据字节数。
/// </summary>
public static class SgccBluetoothConverterProtocol
{
    public const byte Start1 = 0x7E;
    public const byte Start2 = 0x7E;
    public const byte Start3 = 0x7E;
    public const byte Start4 = 0x5A;
    public const byte End1 = 0x7E;
    public const byte End2 = 0xA5;
    public const byte ResponseMask = 0x80;
    public const string DefaultPin = "123456";

    /// <summary>构造复位转换器报文（0x00）。</summary>
    public static byte[] BuildResetFrame() => BuildFrame(BluetoothConverterFunction.Reset);

    /// <summary>
    /// 构造连接待测电能表报文（0x01）。
    /// 电表地址按6字节BCD解析并低字节在前发送。
    /// </summary>
    /// <param name="meterAddress">正常显示顺序的12位地址，例如112233445566。</param>
    /// <param name="pin">空值表示自动模式；6位数字表示扩展PIN模式。</param>
    /// <param name="useLegacyPinLength">
    /// true时复制协议文档中扩展PIN示例的0x0C长度值；false时按正式长度定义使用0x12。
    /// </param>
    public static byte[] BuildConnectMeterFrame(
        string meterAddress,
        string? pin = null,
        bool useLegacyPinLength = false)
    {
        byte[] address = ParseBcdAddressLowByteFirst(meterAddress);
        if (string.IsNullOrWhiteSpace(pin))
            return BuildFrame(BluetoothConverterFunction.ConnectMeter, address);

        byte[] pinBytes = ParsePinLowByteFirst(pin);
        byte[] payload = address.Concat(pinBytes).ToArray();
        return BuildFrame(
            BluetoothConverterFunction.ConnectMeter,
            payload,
            useLegacyPinLength ? (byte)0x0C : null);
    }

    /// <summary>构造待测电表进入/退出检定模式报文（0x02）。</summary>
    public static byte[] BuildMeterVerificationModeFrame(
        byte pulseType,
        byte transmitPowerLevel,
        byte frequencyBand,
        byte channelGenerationMode,
        IReadOnlyList<ushort>? channelFrequencies = null,
        byte automaticChannelCount = 1)
    {
        ValidateRange(transmitPowerLevel, 0, 4, nameof(transmitPowerLevel));
        ValidateRange(frequencyBand, 0, 2, nameof(frequencyBand));
        ValidateRange(channelGenerationMode, 0, 1, nameof(channelGenerationMode));

        IReadOnlyList<ushort> frequencies = channelFrequencies ?? Array.Empty<ushort>();
        int channelCount = channelGenerationMode == 1
            ? automaticChannelCount
            : frequencies.Count;
        if (channelCount is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(channelFrequencies), "通道数量必须在1-5之间。");

        List<byte> payload = new()
        {
            0x01,
            pulseType,
            transmitPowerLevel,
            frequencyBand,
            channelGenerationMode,
            (byte)channelCount
        };
        if (channelGenerationMode == 0)
        {
            foreach (ushort frequency in frequencies)
            {
                payload.Add((byte)(frequency & 0xFF));
                payload.Add((byte)(frequency >> 8));
            }
        }

        return BuildFrame(BluetoothConverterFunction.SwitchMeterVerificationMode, payload.ToArray());
    }

    /// <summary>构造转换器进入/退出检定模式报文（0x03）。</summary>
    public static byte[] BuildConverterVerificationModeFrame(
        byte pulseType,
        byte transmitPowerLevel,
        byte communicationMode)
    {
        ValidateRange(transmitPowerLevel, 0, 4, nameof(transmitPowerLevel));
        ValidateRange(communicationMode, 0, 1, nameof(communicationMode));
        return BuildFrame(
            BluetoothConverterFunction.SwitchConverterVerificationMode,
            0x01,
            pulseType,
            transmitPowerLevel,
            communicationMode);
    }

    /// <summary>构造设置RS485波特率报文（0x04）。</summary>
    public static byte[] BuildSetRs485BaudRateFrame(BluetoothConverterBaudRate baudRate) =>
        BuildFrame(BluetoothConverterFunction.SetRs485BaudRate, (byte)baudRate);

    public static byte[] BuildReadManagementFirmwareVersionFrame() =>
        BuildFrame(BluetoothConverterFunction.ReadManagementFirmwareVersion);

    public static byte[] BuildReadBluetoothFirmwareVersionFrame() =>
        BuildFrame(BluetoothConverterFunction.ReadBluetoothFirmwareVersion);

    public static byte[] BuildPreprocessFrame() =>
        BuildFrame(BluetoothConverterFunction.Preprocess);

    public static byte[] BuildQueryPreprocessStatusFrame() =>
        BuildFrame(BluetoothConverterFunction.QueryPreprocessStatus);

    /// <summary>构造任意功能码报文。</summary>
    public static byte[] BuildFrame(BluetoothConverterFunction function, params byte[] data) =>
        BuildFrame(function, data, null);

    private static byte[] BuildFrame(
        BluetoothConverterFunction function,
        IReadOnlyList<byte> data,
        byte? declaredLengthOverride)
    {
        int formalLength = 6 + data.Count;
        if (formalLength > byte.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(data), "蓝牙协议单帧数据过长。");

        int frameLength = formalLength + 3;
        byte[] frame = new byte[frameLength];
        frame[0] = Start1;
        frame[1] = Start2;
        frame[2] = Start3;
        frame[3] = Start4;
        frame[4] = declaredLengthOverride ?? (byte)formalLength;
        frame[5] = (byte)function;
        for (int index = 0; index < data.Count; index++)
        {
            frame[6 + index] = data[index];
        }

        // 实际帧结构依据数据字节数确定；兼容长度值只影响长度字段和CS。
        int checksumIndex = 6 + data.Count;
        frame[checksumIndex] = CalculateChecksum(frame.AsSpan(0, checksumIndex));
        frame[checksumIndex + 1] = End1;
        frame[checksumIndex + 2] = End2;
        return frame;
    }

    /// <summary>校验并解析一帧转换器报文。</summary>
    public static bool TryParseFrame(
        ReadOnlySpan<byte> rawFrame,
        out BluetoothConverterFrame? frame,
        out string error)
    {
        frame = null;
        error = string.Empty;
        if (rawFrame.Length < 9)
        {
            error = "蓝牙转换器报文长度不足。";
            return false;
        }

        if (rawFrame[0] != Start1 || rawFrame[1] != Start2 || rawFrame[2] != Start3 || rawFrame[3] != Start4)
        {
            error = "蓝牙转换器起始符错误。";
            return false;
        }

        int declaredLength = rawFrame[4];
        int expectedTotalLength = declaredLength + 3;
        if (declaredLength < 6 || rawFrame.Length != expectedTotalLength)
        {
            error = $"蓝牙转换器长度错误：声明={declaredLength}，实际总长度={rawFrame.Length}。";
            return false;
        }

        if (rawFrame[^2] != End1 || rawFrame[^1] != End2)
        {
            error = "蓝牙转换器结束符错误。";
            return false;
        }

        int checksumIndex = declaredLength;
        byte expectedChecksum = CalculateChecksum(rawFrame[..checksumIndex]);
        if (rawFrame[checksumIndex] != expectedChecksum)
        {
            error = $"蓝牙转换器CS错误：应为{expectedChecksum:X2}，实际{rawFrame[checksumIndex]:X2}。";
            return false;
        }

        byte functionCode = rawFrame[5];
        byte[] data = rawFrame.Slice(6, declaredLength - 6).ToArray();
        frame = new BluetoothConverterFrame(
            functionCode,
            (functionCode & ResponseMask) != 0,
            (byte)(functionCode & ~ResponseMask),
            data,
            rawFrame.ToArray());
        return true;
    }

    /// <summary>校验通用命令应答，结果字00表示成功。</summary>
    public static bool TryParseCommandResult(
        ReadOnlySpan<byte> rawFrame,
        BluetoothConverterFunction requestFunction,
        out bool success,
        out byte resultCode,
        out string message)
    {
        success = false;
        resultCode = 0xFF;
        if (!TryParseFrame(rawFrame, out BluetoothConverterFrame? frame, out message))
            return false;

        byte expectedFunction = (byte)((byte)requestFunction | ResponseMask);
        if (frame!.FunctionCode != expectedFunction)
        {
            message = $"蓝牙应答功能码错误：期望{expectedFunction:X2}，实际{frame.FunctionCode:X2}。";
            return false;
        }

        if (frame.Data.Length < 1)
        {
            message = "蓝牙命令应答缺少结果字。";
            return false;
        }

        resultCode = frame.Data[0];
        success = resultCode == 0x00;
        message = success ? "命令应答成功。" : $"命令应答失败，错误码={resultCode:X2}。";
        return true;
    }

    /// <summary>解析0x08预处理状态应答。</summary>
    public static bool TryParsePreprocessStatus(
        ReadOnlySpan<byte> rawFrame,
        out BluetoothPreprocessStatus status,
        out string message)
    {
        status = BluetoothPreprocessStatus.Failed;
        if (!TryParseFrame(rawFrame, out BluetoothConverterFrame? frame, out message))
            return false;

        byte expectedFunction = (byte)((byte)BluetoothConverterFunction.QueryPreprocessStatus | ResponseMask);
        if (frame!.FunctionCode != expectedFunction || frame.Data.Length < 1)
        {
            message = "蓝牙预处理状态应答功能码或数据长度错误。";
            return false;
        }

        status = frame.Data[0] switch
        {
            0x00 => BluetoothPreprocessStatus.Succeeded,
            0x01 => BluetoothPreprocessStatus.Failed,
            0x02 => BluetoothPreprocessStatus.Processing,
            _ => BluetoothPreprocessStatus.Unknown
        };
        message = status switch
        {
            BluetoothPreprocessStatus.Succeeded => "蓝牙检定预处理成功。",
            BluetoothPreprocessStatus.Processing => "蓝牙检定预处理正在进行。",
            BluetoothPreprocessStatus.Failed => "蓝牙检定预处理失败。",
            _ => $"未知蓝牙检定预处理状态：{frame.Data[0]:X2}。"
        };
        return status != BluetoothPreprocessStatus.Unknown;
    }

    /// <summary>从TCP累积缓冲区中提取一帧完整蓝牙转换器报文。</summary>
    public static bool TryTakeFrame(List<byte> buffer, out byte[]? frame)
    {
        frame = null;
        int startIndex = FindStart(buffer);
        if (startIndex < 0)
        {
            if (buffer.Count > 3)
                buffer.RemoveRange(0, buffer.Count - 3);
            return false;
        }

        if (startIndex > 0)
            buffer.RemoveRange(0, startIndex);
        if (buffer.Count < 5)
            return false;

        int totalLength = buffer[4] + 3;
        if (buffer.Count < totalLength)
            return false;

        frame = buffer.Take(totalLength).ToArray();
        buffer.RemoveRange(0, totalLength);
        return true;
    }

    public static string ToHexString(IEnumerable<byte> data) =>
        string.Join(" ", data.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));

    private static int FindStart(IReadOnlyList<byte> buffer)
    {
        for (int index = 0; index <= buffer.Count - 4; index++)
        {
            if (buffer[index] == Start1 && buffer[index + 1] == Start2 &&
                buffer[index + 2] == Start3 && buffer[index + 3] == Start4)
            {
                return index;
            }
        }

        return -1;
    }

    private static byte[] ParseBcdAddressLowByteFirst(string meterAddress)
    {
        string normalized = Regex.Replace(meterAddress ?? string.Empty, @"[\s-]", string.Empty);
        if (!Regex.IsMatch(normalized, @"^\d{12}$"))
            throw new ArgumentException("电表通信地址必须是12位十进制BCD数字。", nameof(meterAddress));

        byte[] bytes = Enumerable.Range(0, 6)
            .Select(index => byte.Parse(normalized.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .Reverse()
            .ToArray();
        return bytes;
    }

    private static byte[] ParsePinLowByteFirst(string pin)
    {
        string normalized = pin?.Trim() ?? string.Empty;
        if (!Regex.IsMatch(normalized, @"^\d{6}$"))
            throw new ArgumentException("PIN码必须是6位数字。", nameof(pin));

        return Encoding.ASCII.GetBytes(normalized).Reverse().ToArray();
    }

    private static byte CalculateChecksum(ReadOnlySpan<byte> data)
    {
        int sum = 0;
        foreach (byte value in data)
            sum += value;
        return (byte)(sum & 0xFF);
    }

    private static void ValidateRange(byte value, byte minimum, byte maximum, string parameterName)
    {
        if (value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(parameterName, value, $"参数必须在{minimum}-{maximum}之间。");
    }
}

public enum BluetoothConverterFunction : byte
{
    Reset = 0x00,
    ConnectMeter = 0x01,
    SwitchMeterVerificationMode = 0x02,
    SwitchConverterVerificationMode = 0x03,
    SetRs485BaudRate = 0x04,
    ReadManagementFirmwareVersion = 0x05,
    ReadBluetoothFirmwareVersion = 0x06,
    Preprocess = 0x07,
    QueryPreprocessStatus = 0x08
}

public enum BluetoothConverterBaudRate : byte
{
    Baud2400 = 0x00,
    Baud4800 = 0x01,
    Baud9600 = 0x02,
    Baud19200 = 0x03,
    Baud38400 = 0x04,
    Baud57600 = 0x05
}

public enum BluetoothPreprocessStatus : byte
{
    Succeeded = 0x00,
    Failed = 0x01,
    Processing = 0x02,
    Unknown = 0xFF
}

/// <summary>解析后的蓝牙转换器报文。</summary>
public sealed record BluetoothConverterFrame(
    byte FunctionCode,
    bool IsResponse,
    byte RequestFunctionCode,
    byte[] Data,
    byte[] RawFrame);
