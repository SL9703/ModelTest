using ModelTest.Protocol;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 控制 PCB 通用命令服务。
///
/// 本类只负责所有控制 PCB 流程都会重复使用的基础动作：
/// 1. 根据 ControlPcbGroup 将选中工位转换成 PCB 表位地址；
/// 2. 复用程序启动阶段已经建立的控制 PCB 长连接；
/// 3. 组内按配置间隔发送完整二进制报文；
/// 4. 根据调用方提供的应答解析器，按表位地址收集响应；
/// 5. 输出完整连接、发送、接收和超时日志。
///
/// 日计时、设备自检、常数、起动、潜动和基本误差服务均可复用此类，
/// 避免每个流程各自维护 TaskCompletionSource 和应答订阅。
/// </summary>
internal sealed class MeterTestControlPcbCommandService
{
    private readonly MeterTestControlPcbConnectionManager connectionManager;

    /// <summary>创建控制 PCB 命令服务，连接生命周期仍由共享连接管理器负责。</summary>
    public MeterTestControlPcbCommandService(MeterTestControlPcbConnectionManager connectionManager)
    {
        this.connectionManager = connectionManager;
    }

    /// <summary>
    /// 从方案配置中筛选当前小项可使用的控制 PCB 分组。
    /// controlPcbGroup 为空时使用全部启用分组；填写名称时只使用指定分组。
    /// </summary>
    public static List<MeterTestControlPcbGroup> GetEnabledGroups(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem)
    {
        string configuredGroup = subItem.ControlPcbGroup?.Trim() ?? string.Empty;
        return planConfig.ControlPcbGroups
            .Where(group => group.Enabled)
            .Where(group => string.IsNullOrWhiteSpace(configuredGroup) ||
                            group.Name.Equals(configuredGroup, StringComparison.OrdinalIgnoreCase))
            .Where(group => !string.IsNullOrWhiteSpace(group.Ip) && group.Port is >= 1 and <= 65535)
            .ToList();
    }

    /// <summary>
    /// 将当前选中工位映射为指定 PCB 分组中的表位地址。
    /// 工位不在 stationStart/stationEnd 范围内时不会进入该分组。
    /// </summary>
    public static List<ControlPcbStationTarget> GetTargets(
        MeterTestControlPcbGroup group,
        IReadOnlyList<StationCommunicationConfig> selectedStations)
    {
        if (group.StationStart < 1 ||
            group.StationEnd < group.StationStart ||
            group.MeterAddressStart < 1)
        {
            return new List<ControlPcbStationTarget>();
        }

        List<ControlPcbStationTarget> targets = new();
        foreach (StationCommunicationConfig station in selectedStations)
        {
            if (station.StationNo < group.StationStart || station.StationNo > group.StationEnd)
                continue;

            int meterAddress = group.MeterAddressStart + station.StationNo - group.StationStart;
            if (meterAddress is < 1 or > 254)
                continue;

            targets.Add(new ControlPcbStationTarget(station.StationNo, (byte)meterAddress));
        }

        return targets;
    }

    /// <summary>
    /// 向一个控制 PCB 分组发送一批工位报文，并按表位地址收集应答。
    ///
    /// packetFactory 生成每个目标的完整下行帧；responseAddressResolver 只有在帧格式、
    /// 命令和数据项全部符合当前步骤时才返回表位地址。未匹配帧会留给其它订阅者，
    /// 不会错误完成当前工位的等待任务。
    /// </summary>
    public async Task<MeterTestControlPcbBatchResult> SendAndCollectAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<ControlPcbStationTarget> targets,
        Func<ControlPcbStationTarget, byte[]> packetFactory,
        Func<ControlPcbStationTarget, string> packetDescriptionFactory,
        Func<byte[], byte?> responseAddressResolver,
        TimeSpan timeout,
        TimeSpan packetInterval,
        Action<int, string[]>? stationLogger,
        CancellationToken cancellationToken)
    {
        if (targets.Count == 0)
        {
            return MeterTestControlPcbBatchResult.Empty(group.Name);
        }

        if (!connectionManager.TryGetConnectedConnection(
                group,
                out MeterTestControlPcbConnection connection,
                out string connectionError))
        {
            foreach (ControlPcbStationTarget target in targets)
            {
                WriteLog(
                    stationLogger,
                    target.StationNo,
                    $"[控制PCB接口] 连接不可用：分组={group.Name}，端点={group.Ip}:{group.Port}，"
                    + $"协议={group.ProtocolVersion}，表位={target.MeterAddress:X2}，原因={connectionError}");
            }

            return MeterTestControlPcbBatchResult.ConnectionFailure(connectionError);
        }

        foreach (ControlPcbStationTarget target in targets)
        {
            WriteLog(
                stationLogger,
                target.StationNo,
                $"[控制PCB接口] 复用长连接：分组={group.Name}，端点={connection.DisplayName}，"
                + $"协议={group.ProtocolVersion}，工位={target.StationNo}，表位={target.MeterAddress:X2}。");
        }

        Dictionary<byte, TaskCompletionSource<byte[]>> pending = targets.ToDictionary(
            target => target.MeterAddress,
            _ => new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously));
        using IDisposable subscription = connection.Subscribe(frame =>
        {
            byte? meterAddress;
            try
            {
                meterAddress = responseAddressResolver(frame);
            }
            catch (Exception ex)
            {
                LogMessage.Error(
                    $"[控制PCB接口] 应答解析器异常：分组={group.Name}，端点={connection.DisplayName}，"
                    + $"原始报文={ToHex(frame)}。",
                    ex);
                return;
            }

            if (meterAddress.HasValue &&
                pending.TryGetValue(meterAddress.Value, out TaskCompletionSource<byte[]>? completionSource))
            {
                completionSource.TrySetResult(frame);
                return;
            }

            LogMessage.Debug(
                $"[控制PCB接口][MCU-->PC] 收到非当前步骤应答，已忽略：分组={group.Name}，"
                + $"端点={connection.DisplayName}，报文={ToHex(frame)}");
        });

        byte[][] packets = targets.Select(packetFactory).ToArray();
        try
        {
            await connection.SendSequenceAsync(
                packets,
                packetInterval,
                (index, packet) =>
                {
                    ControlPcbStationTarget target = targets[index];
                    WriteLog(
                        stationLogger,
                        target.StationNo,
                        $"{FormatTimestamp()} - [控制PCB接口][PC-->MCU] 分组={group.Name}，"
                        + $"端点={connection.DisplayName}，工位={target.StationNo}，表位={target.MeterAddress:X2}，"
                        + $"说明={packetDescriptionFactory(target)}，报文={ToHex(packet)}");
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            foreach (ControlPcbStationTarget target in targets)
            {
                WriteLog(
                    stationLogger,
                    target.StationNo,
                    $"[控制PCB接口] 发送失败：分组={group.Name}，端点={connection.DisplayName}，"
                    + $"工位={target.StationNo}，表位={target.MeterAddress:X2}，"
                    + $"说明={packetDescriptionFactory(target)}，异常={ex.Message}。");
            }

            LogMessage.Error(
                $"[控制PCB接口] 批量发送异常：分组={group.Name}，端点={connection.DisplayName}，"
                + $"报文数量={packets.Length}。",
                ex);
            throw;
        }

        Task allResponses = Task.WhenAll(pending.Values.Select(source => source.Task));
        Task completed = await Task.WhenAny(
            allResponses,
            Task.Delay(timeout <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(100) : timeout, cancellationToken))
            .ConfigureAwait(false);
        if (completed != allResponses)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        Dictionary<byte, byte[]> responses = new();
        foreach (ControlPcbStationTarget target in targets)
        {
            TaskCompletionSource<byte[]> completionSource = pending[target.MeterAddress];
            if (completionSource.Task.IsCompletedSuccessfully)
            {
                byte[] response = completionSource.Task.Result;
                responses[target.MeterAddress] = response;
                WriteLog(
                    stationLogger,
                    target.StationNo,
                    $"{FormatTimestamp()} - [控制PCB接口][MCU-->PC] 分组={group.Name}，"
                    + $"端点={connection.DisplayName}，工位={target.StationNo}，表位={target.MeterAddress:X2}，"
                    + $"报文={ToHex(response)}");
                continue;
            }

            WriteLog(
                stationLogger,
                target.StationNo,
                $"[控制PCB接口] 等待应答超时：分组={group.Name}，端点={connection.DisplayName}，"
                + $"工位={target.StationNo}，表位={target.MeterAddress:X2}，超时={Math.Max(100, timeout.TotalMilliseconds):0}ms。");
        }

        string message = responses.Count == targets.Count
            ? $"控制PCB批量命令完成，应答={responses.Count}/{targets.Count}。"
            : $"控制PCB批量命令完成，应答={responses.Count}/{targets.Count}，存在超时工位。";
        return new MeterTestControlPcbBatchResult(true, message, responses);
    }

    /// <summary>按控制 PCB 协议版本构造电表控制下行帧。</summary>
    public static byte[] BuildMeterPacket(
        string protocolVersion,
        byte meterAddress,
        byte command,
        params byte[] dataItems)
    {
        return IsV2(protocolVersion)
            ? MeterControlPcbProtocol.BuildV2ControlFrame(meterAddress, command, dataItems)
            : MeterControlPcbProtocol.BuildV1ControlFrame(meterAddress, command, dataItems);
    }

    /// <summary>
    /// 校验控制 PCB 上行帧，并提取表位地址与命令数据项。
    /// V1/V2 均校验起止符、长度、方向、协议类型、命令码和累加和。
    /// </summary>
    public static bool TryGetDataItems(
        byte[] rawData,
        string protocolVersion,
        byte command,
        out byte meterAddress,
        out byte[] dataItems)
    {
        return IsV2(protocolVersion)
            ? TryGetV2DataItems(rawData, command, out meterAddress, out dataItems)
            : TryGetV1DataItems(rawData, command, out meterAddress, out dataItems);
    }

    /// <summary>校验命令和期望数据项，并返回匹配帧的表位地址。</summary>
    public static byte? ResolveExpectedResponse(
        byte[] frame,
        string protocolVersion,
        byte command,
        IReadOnlyDictionary<byte, byte[]> expectedPayloads)
    {
        if (!TryGetDataItems(frame, protocolVersion, command, out byte meterAddress, out byte[] dataItems) ||
            !expectedPayloads.TryGetValue(meterAddress, out byte[]? expectedPayload))
        {
            return null;
        }

        return dataItems.SequenceEqual(expectedPayload) ? meterAddress : null;
    }

    /// <summary>判断配置是否为 V2；除明确填写 V1 外均按 V2 处理。</summary>
    public static bool IsV2(string protocolVersion)
    {
        return !protocolVersion.Equals(
            MeterControlPcbProtocolVersion.V1.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>将完整二进制帧格式化为现场日志使用的空格分隔十六进制文本。</summary>
    public static string ToHex(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }

    /// <summary>
    /// 校验 V1 上行帧的起止符、长度、方向、协议类型、命令和校验和，并提取数据项。
    /// </summary>
    private static bool TryGetV1DataItems(
        byte[] rawData,
        byte command,
        out byte meterAddress,
        out byte[] dataItems)
    {
        meterAddress = 0;
        dataItems = Array.Empty<byte>();
        if (rawData.Length < 10 ||
            rawData[0] != MeterControlPcbProtocol.V1StartByte ||
            rawData[^1] != MeterControlPcbProtocol.V1EndByte)
        {
            return false;
        }

        int frameLength = rawData[1] | rawData[2] << 8;
        if (rawData.Length != frameLength + 2 || frameLength < 8)
            return false;

        int dataItemLength = frameLength - 7;
        if (dataItemLength < 0 ||
            MeterControlPcbProtocol.CalculateChecksum(rawData, 1, frameLength - 1) != rawData[frameLength] ||
            rawData[3] != MeterControlPcbProtocol.UplinkDirection ||
            rawData[5] != MeterControlPcbProtocol.V1ControlProtocolType ||
            rawData[6] != command)
        {
            return false;
        }

        meterAddress = rawData[4];
        dataItems = rawData.Skip(7).Take(dataItemLength).ToArray();
        return true;
    }

    /// <summary>
    /// 校验 V2 上行帧的起止符、长度、方向、协议类型、命令和校验和，并提取数据项。
    /// </summary>
    private static bool TryGetV2DataItems(
        byte[] rawData,
        byte command,
        out byte meterAddress,
        out byte[] dataItems)
    {
        meterAddress = 0;
        dataItems = Array.Empty<byte>();
        if (rawData.Length < 11 ||
            rawData[0] != MeterControlPcbProtocol.V2StartByte1 ||
            rawData[1] != MeterControlPcbProtocol.V2StartByte2 ||
            rawData[^2] != MeterControlPcbProtocol.V2EndByte1 ||
            rawData[^1] != MeterControlPcbProtocol.V2EndByte2)
        {
            return false;
        }

        int dataLength = rawData[2] | rawData[3] << 8;
        if (rawData.Length != dataLength + 4 || dataLength < 7)
            return false;

        int dataItemLength = dataLength - 7;
        if (dataItemLength < 0 ||
            MeterControlPcbProtocol.CalculateChecksum(rawData, 2, dataLength - 1) != rawData[^3] ||
            rawData[4] != MeterControlPcbProtocol.UplinkDirection ||
            rawData[6] != MeterControlPcbProtocol.V2MeterControlProtocolType ||
            rawData[7] != command)
        {
            return false;
        }

        meterAddress = rawData[5];
        dataItems = rawData.Skip(8).Take(dataItemLength).ToArray();
        return true;
    }

    /// <summary>将控制 PCB 接口明细同步写入指定工位日志和全局 Debug 日志。</summary>
    private static void WriteLog(Action<int, string[]>? logger, int stationNo, params string[] lines)
    {
        logger?.Invoke(stationNo, lines);
        foreach (string line in lines)
        {
            LogMessage.Debug($"[控制PCB接口][工位{stationNo}] {line}");
        }
    }

    /// <summary>生成控制 PCB 收发日志使用的毫秒时间戳。</summary>
    private static string FormatTimestamp()
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";
    }
}

/// <summary>一次控制 PCB 分组批量命令的连接状态、说明和逐表位响应。</summary>
internal sealed record MeterTestControlPcbBatchResult(
    bool ConnectionAvailable,
    string Message,
    IReadOnlyDictionary<byte, byte[]> Responses)
{
    /// <summary>创建“当前分组没有目标工位”的成功空结果。</summary>
    public static MeterTestControlPcbBatchResult Empty(string groupName) =>
        new(true, $"控制PCB分组 {groupName} 没有当前选中工位。", new Dictionary<byte, byte[]>());

    /// <summary>创建控制 PCB 长连接不可用的失败结果。</summary>
    public static MeterTestControlPcbBatchResult ConnectionFailure(string message) =>
        new(false, message, new Dictionary<byte, byte[]>());
}
