using ModelTest.Tools;

namespace ModelTest.Protocol;

/// <summary>
/// 终端控制 PCB 的 V1 协议组帧工具。
/// </summary>
public static class TerminalControlPcbProtocol
{
    public const string StartByte = "55";
    public const string StopByte = "AA";
    public const string ControlProtocol = "00";
    public const string TerminalTypeCommand = "2D";
    public const string VoltageCommand = "21";
    public const string CurrentCommand = "22";
    public const string MotorCrimpingCommand = "29";
    public const string CcoAcCommand = "02";
    public const string MeterAcCommand = "21";
    public const string StaDcCommand = "3A";
    public const string StaAcCommand = "85";
    public const string Sta1SetPinCommand = "3B";
    public const string Sta2SetPinCommand = "86";
    public const string Sta1ReadPinCommand = "3C";
    public const string Sta2ReadPinCommand = "87";
    public const string LedCommand = "30";
    public const string SwitchBoardAcCommand = "41";
    public const string IndicatorLightCommand = "2A";
    public const string TaitiIndicatorlLightCommand = "2C";
    public const string ChangePowerCommand = "60";
    /// <summary>
    /// 构造终端控制 PCB V1 报文：55 + 长度(小端) + 地址 + 协议类型 + 命令 + 数据项 + 校验和 + AA。
    /// </summary>
    public static string BuildFrame(string address, string command, string? dataItem = null)
    {
        string normalizedAddress = AddressToHexChange.MeassageAddr(address?.Trim() ?? string.Empty);
        if (!IsByteHex(normalizedAddress))
            throw new ArgumentException($"终端地址无效：{address}", nameof(address));

        if (!IsByteHex(command))
            throw new ArgumentException($"命令码无效：{command}", nameof(command));

        string normalizedData = dataItem?.Trim() ?? string.Empty;
        if (normalizedData.Length % 2 != 0 || normalizedData.Any(value => !Uri.IsHexDigit(value)))
            throw new ArgumentException($"数据项不是有效HEX：{dataItem}", nameof(dataItem));

        int length = 7 + normalizedData.Length / 2 - 1;
        string lengthHex = (length & 0xFF).ToString("X2") + ((length >> 8) & 0xFF).ToString("X2");
        string checksum = MessagesCheckSum.CalculateChecksum(
            lengthHex + normalizedAddress + ControlProtocol + command.ToUpperInvariant() + normalizedData.ToUpperInvariant());

        return StartByte
            + lengthHex
            + normalizedAddress
            + ControlProtocol
            + command.ToUpperInvariant()
            + normalizedData.ToUpperInvariant()
            + checksum
            + StopByte;
    }

    private static bool IsByteHex(string? value) =>
        value is { Length: 2 } && value.All(Uri.IsHexDigit);
}
