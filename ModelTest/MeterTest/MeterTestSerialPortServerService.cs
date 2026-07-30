using System.Net.Sockets;
using ModelTest.Protocol;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 测试前串口服务器波特率同步服务。
///
/// 串口服务器管理端固定连接 IP:64444：
/// 1. 使用电表 V2 F3 命令读取设备上传的真实端口、波特率和校验位；
/// 2. 与资产信息中的工位端口和波特率比较；
/// 3. 只对不一致端口发送 F1 属性更改命令，并使用断电保存标志。
/// </summary>
public sealed class MeterTestSerialPortServerService
{
    /// <summary>串口服务器管理端口。</summary>
    public const int ManagementPort = SerialPortServerProtocolV2.ManagementPortNumber;

    /// <summary>单次管理命令等待应答的超时时间。</summary>
    public const int ResponseTimeoutMilliseconds = 5000;

    private readonly SerialPortServerProtocolV2 protocol = new();

    /// <summary>
    /// 检查指定 IP 下所有目标工位的波特率，并自动修正不一致通道。
    /// </summary>
    public async Task<MeterTestSerialPortServerResult> EnsureBaudRatesAsync(
        string ipAddress,
        IReadOnlyList<MeterTestSerialPortBaudRequirement> requirements,
        CancellationToken cancellationToken)
    {
        string endpoint = $"{ipAddress?.Trim()}:{ManagementPort}";
        string currentOperation = "参数校验";
        LogMessage.Debug(
            $"[串口服务器接口] 开始波特率同步：端点={endpoint}，目标工位数={requirements.Count}，"
            + $"目标={string.Join("；", requirements.Select(item => $"工位{item.StationNo}/Port={item.Port}/{item.BaudRate}"))}。"
        );
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            LogMessage.Error("[串口服务器接口] 波特率同步终止：串口服务器 IP 不能为空。", null);
            return MeterTestSerialPortServerResult.Fail("串口服务器 IP 不能为空。");
        }

        if (requirements.Count == 0)
        {
            LogMessage.Debug($"[串口服务器接口] {endpoint} 没有需要检查的目标通道，本次调用直接完成。");
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
            LogMessage.Error($"[串口服务器接口] {endpoint} 资产波特率参数校验失败。", ex);
            return MeterTestSerialPortServerResult.Fail(ex.Message, details);
        }

        using TcpClient client = new();
        try
        {
            currentOperation = "连接管理端";
            LogMessage.Debug(
                $"[串口服务器接口] 准备连接：端点={endpoint}，"
                + $"超时={ResponseTimeoutMilliseconds}ms。"
            );
            using CancellationTokenSource connectCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(ResponseTimeoutMilliseconds);
            await client.ConnectAsync(ipAddress.Trim(), ManagementPort, connectCts.Token).ConfigureAwait(false);
            details.Add($"串口服务器管理端连接成功：{ipAddress.Trim()}:{ManagementPort}");
            LogMessage.Debug($"[串口服务器接口] 连接成功：端点={endpoint}。");

            await using NetworkStream stream = client.GetStream();
            byte[] readRequest = protocol.BuildMeterPortInfoReadFrame();
            currentOperation = "F3读取端口信息";
            string readRequestHex = SerialPortServerProtocolV2.ToHexString(readRequest);
            details.Add($"发送F3端口信息读取：{readRequestHex}");
            LogMessage.Debug(
                $"[串口服务器接口][PC-->Server] 端点={endpoint}，命令=F3读取端口信息，"
                + $"超时={ResponseTimeoutMilliseconds}ms，报文={readRequestHex}"
            );
            byte[] readResponse = await SendAndReceiveAsync(stream, readRequest, cancellationToken).ConfigureAwait(false);
            string readResponseHex = SerialPortServerProtocolV2.ToHexString(readResponse);
            details.Add($"收到F3端口信息应答：{readResponseHex}");
            LogMessage.Debug(
                $"[串口服务器接口][Server-->PC] 端点={endpoint}，命令=F3读取端口信息，"
                + $"报文={readResponseHex}"
            );

            if (!protocol.TryParseMeterPortInformation(
                    readResponse,
                    out IReadOnlyList<MeterSerialPortServerPortSetting>? settings,
                    out string parseError))
            {
                LogMessage.Error(
                    $"[串口服务器接口] F3应答解析失败：端点={endpoint}，"
                    + $"响应={readResponseHex}，原因={parseError}",
                    null);
                return MeterTestSerialPortServerResult.Fail($"读取串口参数失败：{parseError}", details);
            }

            Dictionary<int, MeterSerialPortServerPortSetting> settingsByPort = settings
                .ToDictionary(setting => (int)setting.Port);
            foreach (MeterSerialPortServerPortSetting setting in settings)
            {
                details.Add(
                    $"读取端口 {setting.Port}（序号{setting.Sequence}）：{FormatSetting(setting)}");
            }

            List<MeterTestSerialPortBaudUpdate> mismatches = updates
                .Where(update => !IsSettingMatched(settingsByPort, update))
                .ToList();

            foreach (MeterTestSerialPortBaudUpdate update in updates.Where(update => IsSettingMatched(settingsByPort, update)))
            {
                MeterSerialPortServerPortSetting current = settingsByPort[update.Port];
                details.Add(
                    $"工位{update.StationNo} 端口 {update.Port} 波特率一致，无需修改：{FormatSetting(current)}");
            }

            if (mismatches.Count == 0)
            {
                LogMessage.Debug(
                    $"[串口服务器接口] 波特率同步完成：端点={endpoint}，"
                    + $"目标端口={updates.Count}，不一致端口=0，结论=合格。"
                );
                return MeterTestSerialPortServerResult.Succeeded("串口服务器波特率检查完成，所有目标端口均已匹配。", details);
            }

            details.Add($"检测到 {mismatches.Count} 个端口参数不一致，开始修改。");
            foreach (MeterTestSerialPortBaudUpdate update in mismatches)
            {
                byte[] setRequest = protocol.BuildMeterPortPropertyChangeFrame(
                    (ushort)update.Port,
                    (uint)update.BaudRate,
                    update.Parity,
                    saveOnPowerLoss: true);
                currentOperation = $"F1设置端口{update.Port}";
                string setRequestHex = SerialPortServerProtocolV2.ToHexString(setRequest);
                details.Add(
                    $"工位{update.StationNo} 端口 {update.Port} 修改为 {update.BaudRateProfile}，发送F1：{setRequestHex}");
                LogMessage.Debug(
                    $"[串口服务器接口][PC-->Server] 端点={endpoint}，命令=F1端口属性更改，"
                    + $"工位={update.StationNo}，目标端口={update.Port}，目标波特率={update.BaudRateProfile}，"
                    + $"断电保存=true，超时={ResponseTimeoutMilliseconds}ms，报文={setRequestHex}"
                );
                byte[] setResponse = await SendAndReceiveAsync(stream, setRequest, cancellationToken).ConfigureAwait(false);
                string setResponseHex = SerialPortServerProtocolV2.ToHexString(setResponse);
                details.Add($"收到F1设置应答：{setResponseHex}");
                LogMessage.Debug(
                    $"[串口服务器接口][Server-->PC] 端点={endpoint}，命令=F1端口属性更改，"
                    + $"工位={update.StationNo}，目标端口={update.Port}，报文={setResponseHex}"
                );
                if (!protocol.TryValidateMeterPortPropertyResponse(
                        setResponse,
                        (ushort)update.Port,
                        (uint)update.BaudRate,
                        update.Parity,
                        expectedSaveOnPowerLoss: true,
                        out string ackError))
                {
                    LogMessage.Error(
                        $"[串口服务器接口] F1应答校验失败：端点={endpoint}，"
                        + $"工位={update.StationNo}，端口={update.Port}，响应={setResponseHex}，原因={ackError}",
                        null);
                    throw new InvalidOperationException($"设置端口 {update.Port} 属性应答校验失败：{ackError}");
                }
            }

            LogMessage.Debug(
                $"[串口服务器接口] 波特率同步完成：端点={endpoint}，"
                + $"目标端口={updates.Count}，已修改={mismatches.Count}，结论=合格。"
            );
            return MeterTestSerialPortServerResult.Succeeded("串口服务器端口属性修改完成，F1已按断电保存方式设置。", details);
        }
        catch (OperationCanceledException ex)
        {
            bool cancelledByUser = cancellationToken.IsCancellationRequested;
            string message = cancelledByUser
                ? $"串口服务器波特率检查已取消，阶段={currentOperation}。"
                : $"串口服务器等待应答超时，阶段={currentOperation}，超时={ResponseTimeoutMilliseconds}ms。";
            details.Add(message);
            LogMessage.Error($"[串口服务器接口] {endpoint} {message}", ex);
            return MeterTestSerialPortServerResult.Fail(message, details);
        }
        catch (Exception ex)
        {
            details.Add($"接口异常阶段：{currentOperation}；{ex.Message}");
            LogMessage.Error(
                $"[串口服务器接口] 波特率同步异常：端点={endpoint}，阶段={currentOperation}。",
                ex);
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
            if (requirement.Port is < 1 or > 65535 || requirement.Port == ManagementPort)
            {
                throw new InvalidOperationException(
                    $"工位{requirement.StationNo} 端口 {requirement.Port} 无效，端口必须是1-65535且不能是管理端口64444。");
            }

            if (!TryParseBaudRateProfile(requirement.BaudRate, out BaudRateProfile? profile, out string error) ||
                profile is null)
            {
                throw new InvalidOperationException($"工位{requirement.StationNo} 波特率配置错误：{error}");
            }

            MeterTestSerialPortBaudUpdate update = new(
                requirement.StationNo,
                requirement.Port,
                profile.BaudRate,
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
        IReadOnlyDictionary<int, MeterSerialPortServerPortSetting> settingsByPort,
        MeterTestSerialPortBaudUpdate update)
    {
        if (!settingsByPort.TryGetValue(update.Port, out MeterSerialPortServerPortSetting? current))
        {
            return false;
        }

        return current.BaudRate == (uint)update.BaudRate &&
               current.ParityCode == SerialPortServerProtocolV2.GetMeterPortParityCode(update.Parity);
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
            if (TryTakeFrame(receiveBuffer, out byte[] frame))
            {
                return frame;
            }
        }
    }

    /// <summary>
    /// 从接收缓冲区提取一帧 55 44 V2 报文。
    /// </summary>
    private static bool TryTakeFrame(List<byte> buffer, out byte[] frame)
    {
        frame = Array.Empty<byte>();
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
    /// 将 F3 当前端口参数格式化为资产信息中使用的可读格式。
    /// F3 不上传数据位和停止位，按协议固定值展示为8位数据、1位停止位。
    /// </summary>
    private static string FormatSetting(MeterSerialPortServerPortSetting setting)
    {
        string baud = setting.BaudRate == 0 ? "未接硬件(0)" : setting.BaudRate.ToString();
        string parity = setting.ParityCode switch
        {
            0x00 => "E",
            0x01 => "O",
            0x02 => "N",
            _ => $"未知({setting.ParityCode:X2})"
        };
        return $"{baud}-{MeterSerialPortServerPortSetting.DataBits}-{parity}-{MeterSerialPortServerPortSetting.StopBits}";
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

        if (baudRate <= 0)
        {
            error = "波特率必须大于0。";
            return false;
        }

        if (dataBits != MeterSerialPortServerPortSetting.DataBits ||
            stopBits != MeterSerialPortServerPortSetting.StopBits)
        {
            error = "电表V2串口服务器协议固定为8位数据、1位停止位，格式必须为 波特率-8-校验位-1。";
            return false;
        }

        profile = new BaudRateProfile(baudRate, dataBits, stopBits, parity, $"{baudRate}-{dataBits}-{parts[2].ToUpperInvariant()}-{stopBits}");
        return true;
    }

    /// <summary>资产波特率文本解析后的数值参数，供 F3 比对和 F1 组帧共同使用。</summary>
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
    /// <summary>创建一个成功结果，并保留本次管理接口的全部过程明细。</summary>
    public static MeterTestSerialPortServerResult Succeeded(
        string message,
        IReadOnlyList<string>? details = null)
    {
        return new MeterTestSerialPortServerResult(true, message, details ?? Array.Empty<string>());
    }

    /// <summary>创建一个失败结果，并保留失败前已经产生的连接、发送和接收明细。</summary>
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
    int BaudRate,
    SerialPortServerParity Parity,
    string BaudRateProfile);
