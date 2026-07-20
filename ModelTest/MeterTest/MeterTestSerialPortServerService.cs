using System.Net.Sockets;
using ModelTest.Protocol;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 测试前串口服务器波特率同步服务。
///
/// 串口服务器管理端固定连接 IP:64444：
/// 1. 读取全部 COM 参数；
/// 2. 将 COM0、COM1……映射为 TCP 端口 951、952……；
/// 3. 与资产信息中的工位端口和波特率比较；
/// 4. 只对不一致的通道执行解锁、设置和保存重启。
/// </summary>
public sealed class MeterTestSerialPortServerService
{
    /// <summary>串口服务器管理端口。</summary>
    public const int ManagementPort = 64444;

    /// <summary>串口服务器板卡地址，协议示例使用 01。</summary>
    public const byte ServerAddress = 0x01;

    /// <summary>单次管理命令等待应答的超时时间。</summary>
    public const int ResponseTimeoutMilliseconds = 5000;

    private const byte ReadSerialParametersType = 0x0B;
    private const string UnlockPassword = "admin";
    private readonly SerialPortServerProtocolV2 protocol = new();

    /// <summary>
    /// 检查指定 IP 下所有目标工位的波特率，并自动修正不一致通道。
    /// </summary>
    public async Task<MeterTestSerialPortServerResult> EnsureBaudRatesAsync(
        string ipAddress,
        IReadOnlyList<MeterTestSerialPortBaudRequirement> requirements,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return MeterTestSerialPortServerResult.Fail("串口服务器 IP 不能为空。");
        }

        if (requirements.Count == 0)
        {
            return MeterTestSerialPortServerResult.Succeeded("没有需要检查的串口服务器通道。");
        }

        List<string> details = new()
        {
            $"准备连接串口服务器管理端：{ipAddress.Trim()}:{ManagementPort}"
        };

        List<MeterTestSerialPortBaudUpdate> updates;
        try
        {
            updates = BuildUpdates(requirements, details);
        }
        catch (Exception ex)
        {
            return MeterTestSerialPortServerResult.Fail(ex.Message, details);
        }

        using TcpClient client = new();
        try
        {
            await client.ConnectAsync(ipAddress.Trim(), ManagementPort, cancellationToken).ConfigureAwait(false);
            details.Add($"串口服务器管理端连接成功：{ipAddress.Trim()}:{ManagementPort}");

            await using NetworkStream stream = client.GetStream();
            byte[] readRequest = protocol.BuildReadParametersFrame(
                ServerAddress,
                ReadSerialParametersType,
                SerialPortServerProtocolV2.MeterControlProtocolType);
            byte[] readResponse = await SendAndReceiveAsync(stream, readRequest, cancellationToken).ConfigureAwait(false);
            details.Add($"发送读取参数：{SerialPortServerProtocolV2.ToHexString(readRequest)}");
            details.Add($"收到读取应答：{SerialPortServerProtocolV2.ToHexString(readResponse)}");

            if (!protocol.TryParseSerialPortParameters(readResponse, out IReadOnlyList<SerialPortServerPortSetting>? settings, out string parseError))
            {
                return MeterTestSerialPortServerResult.Fail($"读取串口参数失败：{parseError}", details);
            }

            if (!string.IsNullOrWhiteSpace(parseError))
            {
                details.Add($"读取参数提示：{parseError}");
            }

            Dictionary<int, SerialPortServerPortSetting> settingsByPort = settings
                .ToDictionary(setting => setting.TcpPort);
            foreach (SerialPortServerPortSetting setting in settings)
            {
                details.Add(
                    $"读取端口 {setting.TcpPort}（COM{setting.Channel}）：{FormatSetting(setting)}");
            }

            List<MeterTestSerialPortBaudUpdate> mismatches = updates
                .Where(update => !IsSettingMatched(settingsByPort, update))
                .ToList();

            foreach (MeterTestSerialPortBaudUpdate update in updates.Where(update => IsSettingMatched(settingsByPort, update)))
            {
                SerialPortServerPortSetting current = settingsByPort[update.Port];
                details.Add(
                    $"工位{update.StationNo} 端口 {update.Port} 波特率一致，无需修改：{FormatSetting(current)}");
            }

            if (mismatches.Count == 0)
            {
                return MeterTestSerialPortServerResult.Succeeded("串口服务器波特率检查完成，所有目标端口均已匹配。", details);
            }

            details.Add($"检测到 {mismatches.Count} 个端口参数不一致，开始修改。");
            byte[] unlockRequest = protocol.BuildUnlockFrame(
                ServerAddress,
                UnlockPassword,
                SerialPortServerProtocolV2.MeterControlProtocolType);
            byte[] unlockResponse = await SendAndReceiveAsync(stream, unlockRequest, cancellationToken).ConfigureAwait(false);
            details.Add($"发送解锁：{SerialPortServerProtocolV2.ToHexString(unlockRequest)}");
            details.Add($"收到解锁应答：{SerialPortServerProtocolV2.ToHexString(unlockResponse)}");
            EnsureAck(unlockResponse, 0x02, "解锁");

            foreach (MeterTestSerialPortBaudUpdate update in mismatches)
            {
                byte[] setRequest = protocol.BuildSetBaudRateFrame(
                    ServerAddress,
                    (byte)update.Channel,
                    update.BaudRate,
                    update.DataBits,
                    update.StopBits,
                    update.Parity,
                    applyImmediately: true,
                    protocolType: SerialPortServerProtocolV2.MeterControlProtocolType);
                byte[] setResponse = await SendAndReceiveAsync(stream, setRequest, cancellationToken).ConfigureAwait(false);
                details.Add(
                    $"工位{update.StationNo} 端口 {update.Port} 修改为 {update.BaudRateProfile}，发送：{SerialPortServerProtocolV2.ToHexString(setRequest)}");
                details.Add($"收到设置应答：{SerialPortServerProtocolV2.ToHexString(setResponse)}");
                EnsureAck(setResponse, 0x0B, $"设置端口 {update.Port} 波特率");
            }

            // 设置报文使用 0x01“立即生效”，因此这里不再发送 FF 0D 保存重启，
            // 避免串口服务器重启导致当前管理连接断开，也不需要额外重连。
            return MeterTestSerialPortServerResult.Succeeded("串口服务器波特率修改完成，参数已立即生效。", details);
        }
        catch (OperationCanceledException)
        {
            return MeterTestSerialPortServerResult.Fail("串口服务器波特率检查已取消或等待应答超时。", details);
        }
        catch (Exception ex)
        {
            return MeterTestSerialPortServerResult.Fail($"串口服务器波特率检查失败：{ex.Message}", details);
        }
    }

    /// <summary>
    /// 将资产配置转换成实际需要比对的串口参数，并检查重复端口冲突。
    /// </summary>
    private static List<MeterTestSerialPortBaudUpdate> BuildUpdates(
        IReadOnlyList<MeterTestSerialPortBaudRequirement> requirements,
        ICollection<string> details)
    {
        List<MeterTestSerialPortBaudUpdate> updates = new();
        foreach (MeterTestSerialPortBaudRequirement requirement in requirements)
        {
            int channel = requirement.Port - SerialPortServerProtocolV2.FirstMappedTcpPort;
            if (channel < 0 || channel >= SerialPortServerProtocolV2.DefaultComPortCount)
            {
                throw new InvalidOperationException(
                    $"工位{requirement.StationNo} 端口 {requirement.Port} 不在串口服务器端口映射范围 951-{SerialPortServerProtocolV2.FirstMappedTcpPort + SerialPortServerProtocolV2.DefaultComPortCount - 1} 内。");
            }

            if (!TryParseBaudRateProfile(requirement.BaudRate, out BaudRateProfile? profile, out string error))
            {
                throw new InvalidOperationException($"工位{requirement.StationNo} 波特率配置错误：{error}");
            }

            MeterTestSerialPortBaudUpdate update = new(
                requirement.StationNo,
                requirement.Port,
                channel,
                profile.BaudRate,
                profile.DataBits,
                profile.StopBits,
                profile.Parity,
                profile.DisplayText);

            MeterTestSerialPortBaudUpdate? duplicate = updates.FirstOrDefault(item => item.Port == update.Port);
            if (duplicate is not null &&
                !duplicate.BaudRateProfile.Equals(update.BaudRateProfile, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"端口 {update.Port} 被多个工位配置为不同波特率：{duplicate.BaudRateProfile} / {update.BaudRateProfile}。");
            }

            if (duplicate is null)
            {
                updates.Add(update);
            }
        }

        details.Add($"待检查端口：{string.Join(", ", updates.Select(update => $"{update.Port}={update.BaudRateProfile}"))}");
        return updates;
    }

    /// <summary>
    /// 判断串口服务器当前参数是否符合资产信息中的目标波特率。
    /// </summary>
    private static bool IsSettingMatched(
        IReadOnlyDictionary<int, SerialPortServerPortSetting> settingsByPort,
        MeterTestSerialPortBaudUpdate update)
    {
        if (!settingsByPort.TryGetValue(update.Port, out SerialPortServerPortSetting? current))
        {
            return false;
        }

        return current.BaudRate == update.BaudRate &&
               current.DataBitsCode == (update.DataBits == 7 ? 0x00 : 0x01) &&
               current.StopBitsCode == (update.StopBits == 1 ? 0x00 : 0x01) &&
               current.ParityCode == (byte)update.Parity;
    }

    /// <summary>
    /// 发送一帧管理命令，并按 V2 长度字段读取完整应答帧。
    /// </summary>
    private static async Task<byte[]> SendAndReceiveAsync(
        NetworkStream stream,
        byte[] request,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ResponseTimeoutMilliseconds);

        await stream.WriteAsync(request, timeoutCts.Token).ConfigureAwait(false);
        await stream.FlushAsync(timeoutCts.Token).ConfigureAwait(false);

        List<byte> receiveBuffer = new();
        byte[] buffer = new byte[4096];
        while (true)
        {
            int length = await stream.ReadAsync(buffer, timeoutCts.Token).ConfigureAwait(false);
            if (length <= 0)
            {
                throw new IOException("串口服务器管理端连接已关闭。");
            }

            receiveBuffer.AddRange(buffer.AsSpan(0, length).ToArray());
            if (TryTakeFrame(receiveBuffer, out byte[]? frame))
            {
                return frame;
            }
        }
    }

    /// <summary>
    /// 从接收缓冲区提取一帧 55 44 V2 报文。
    /// </summary>
    private static bool TryTakeFrame(List<byte> buffer, out byte[]? frame)
    {
        frame = null;
        int startIndex = -1;
        for (int index = 0; index < buffer.Count - 1; index++)
        {
            if (buffer[index] == SerialPortServerProtocolV2.StartByte1 &&
                buffer[index + 1] == SerialPortServerProtocolV2.StartByte2)
            {
                startIndex = index;
                break;
            }
        }

        if (startIndex < 0)
        {
            if (buffer.Count > 1)
            {
                buffer.RemoveRange(0, buffer.Count - 1);
            }

            return false;
        }

        if (startIndex > 0)
        {
            buffer.RemoveRange(0, startIndex);
        }

        if (buffer.Count < 4)
        {
            return false;
        }

        int dataLength = buffer[2] | (buffer[3] << 8);
        int frameLength = dataLength + 4;
        if (dataLength < 7 || frameLength > 65539)
        {
            buffer.RemoveAt(0);
            return false;
        }

        if (buffer.Count < frameLength)
        {
            return false;
        }

        frame = buffer.Take(frameLength).ToArray();
        buffer.RemoveRange(0, frameLength);
        return true;
    }

    /// <summary>
    /// 校验设置类命令的上行应答是否返回了原命令数据项。
    /// </summary>
    private static void EnsureAck(
        byte[] response,
        byte expectedDataItem,
        string operation,
        byte expectedProtocolType = SerialPortServerProtocolV2.MeterControlProtocolType)
    {
        SerialPortServerProtocolV2 protocol = new();
        if (!protocol.TryParseFrame(response, out SerialPortServerFrame? parsed, out string error) ||
            parsed is null)
        {
            throw new InvalidOperationException($"{operation}应答解析失败：{error}");
        }

        if (parsed.Direction != SerialPortServerProtocolV2.UplinkDirection ||
            parsed.ProtocolType != expectedProtocolType ||
            parsed.CommandCode != SerialPortServerProtocolV2.SerialServerCommand ||
            parsed.Data.Length < 2 ||
            parsed.Data[0] != 0xFF ||
            parsed.Data[1] != expectedDataItem)
        {
            throw new InvalidOperationException($"{operation}应答内容不匹配。");
        }
    }

    /// <summary>
    /// 将当前通道参数格式化为可读日志。
    /// </summary>
    private static string FormatSetting(SerialPortServerPortSetting setting)
    {
        string baud = setting.BaudRate?.ToString() ?? $"未知(码={setting.BaudRateCode:X2})";
        string dataBits = setting.DataBitsCode == 0x00 ? "7" : setting.DataBitsCode == 0x01 ? "8" : $"未知({setting.DataBitsCode:X2})";
        string stopBits = setting.StopBitsCode == 0x00 ? "1" : setting.StopBitsCode == 0x01 ? "2" : $"未知({setting.StopBitsCode:X2})";
        string parity = setting.ParityCode switch
        {
            0x00 => "N",
            0x01 => "E",
            0x02 => "O",
            _ => $"未知({setting.ParityCode:X2})"
        };
        return $"{baud}-{dataBits}-{parity}-{stopBits}";
    }

    /// <summary>
    /// 解析资产信息中的波特率格式，例如 9600-8-E-1。
    /// </summary>
    private static bool TryParseBaudRateProfile(
        string value,
        out BaudRateProfile? profile,
        out string error)
    {
        profile = null;
        error = string.Empty;

        string[] parts = (value ?? string.Empty).Trim().Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 ||
            !int.TryParse(parts[0], out int baudRate) ||
            !int.TryParse(parts[1], out int dataBits) ||
            !int.TryParse(parts[3], out int stopBits))
        {
            error = "格式应为 波特率-数据位-校验位-停止位，例如 9600-8-E-1。";
            return false;
        }

        SerialPortServerParity parity = parts[2].ToUpperInvariant() switch
        {
            "N" => SerialPortServerParity.None,
            "E" => SerialPortServerParity.Even,
            "O" => SerialPortServerParity.Odd,
            _ => (SerialPortServerParity)0xFF
        };

        if (parity == (SerialPortServerParity)0xFF)
        {
            error = "校验位只支持 N、E、O。";
            return false;
        }

        try
        {
            SerialPortServerProtocolV2.GetBaudRateCode(baudRate);
        }
        catch (ArgumentOutOfRangeException)
        {
            error = $"不支持波特率 {baudRate}。";
            return false;
        }

        if (dataBits is not (7 or 8) || stopBits is not (1 or 2))
        {
            error = "数据位只支持 7/8，停止位只支持 1/2。";
            return false;
        }

        profile = new BaudRateProfile(baudRate, dataBits, stopBits, parity, $"{baudRate}-{dataBits}-{parts[2].ToUpperInvariant()}-{stopBits}");
        return true;
    }

    private sealed record BaudRateProfile(
        int BaudRate,
        int DataBits,
        int StopBits,
        SerialPortServerParity Parity,
        string DisplayText);
}

/// <summary>
/// MeterTest 资产信息中的串口服务器目标参数。
/// </summary>
public sealed record MeterTestSerialPortBaudRequirement(
    int StationNo,
    int Port,
    string BaudRate);

/// <summary>
/// 一个 IP 的串口服务器波特率同步结果。
/// </summary>
public sealed record MeterTestSerialPortServerResult(
    bool Success,
    string Message,
    IReadOnlyList<string> Details)
{
    public static MeterTestSerialPortServerResult Succeeded(
        string message,
        IReadOnlyList<string>? details = null)
    {
        return new MeterTestSerialPortServerResult(true, message, details ?? Array.Empty<string>());
    }

    public static MeterTestSerialPortServerResult Fail(
        string message,
        IReadOnlyList<string>? details = null)
    {
        return new MeterTestSerialPortServerResult(false, message, details ?? Array.Empty<string>());
    }
}

/// <summary>
/// 一个实际需要修改的串口服务器通道。
/// </summary>
internal sealed record MeterTestSerialPortBaudUpdate(
    int StationNo,
    int Port,
    int Channel,
    int BaudRate,
    int DataBits,
    int StopBits,
    SerialPortServerParity Parity,
    string BaudRateProfile);
