using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ModelTest.CustomControl;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 有功基本误差完整流程服务。
/// 每个方案小项统一执行：升源 -> A2/A0/0x38启动 -> 等待 -> 0x38读取 -> 误差判定。
/// </summary>
public sealed class MeterTestBasicErrorService
{
    private readonly MeterTestSourceControlService sourceControlService;
    private readonly MeterTestControlPcbConnectionManager connectionManager;

    public MeterTestBasicErrorService(
        MeterTestSourceControlService sourceControlService,
        MeterTestControlPcbConnectionManager connectionManager)
    {
        this.sourceControlService = sourceControlService;
        this.connectionManager = connectionManager;
    }

    /// <summary>执行一个正向或反向有功基本误差测试点。</summary>
    public async Task<MeterTestBasicErrorExecutionResult> ExecuteAsync(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        if (!MeterTestBasicErrorCalculator.TryCreateExecutionPlan(
                subItem,
                selectedStations,
                meterArchives,
                out MeterTestBasicErrorExecutionPlan? executionPlan,
                out string? calculationError))
        {
            return CreateFailureResult(selectedStations, calculationError ?? "基本误差测试点参数计算失败。", stationLogger);
        }

        MeterTestBasicErrorExecutionPlan point = executionPlan!;
        foreach (MeterTestBasicErrorStationPlan stationPlan in point.Stations)
        {
            Trace(stationPlan.StationNo, "-----------------------------------------------------------------", stationLogger);
            Trace(stationPlan.StationNo, $"[流程开始] 基本误差测试点：{point.TestPointName}", stationLogger);
            Trace(stationPlan.StationNo, $"[步骤1/5 升源] 参数计算：{stationPlan.CalculationNote}", stationLogger);
        }

        MeterTestSourceControlService.MeterTestSourceControlResult sourceResult = await sourceControlService.ExecuteAsync(
            planConfig,
            subItem,
            selectedStations,
            meterArchives,
            cancellationToken,
            message => ForwardSourceProgress(selectedStations, message, stationLogger)).ConfigureAwait(false);
        if (!sourceResult.Success)
        {
            return CreateFailureResult(selectedStations, $"基本误差升源失败：{sourceResult.Message}", stationLogger, sourceResult.StandValues);
        }

        foreach (MeterTestStationCommunication station in selectedStations)
        {
            Trace(station.StationNo, $"[步骤1/5 升源] 完成：{sourceResult.Message}", stationLogger);
            Trace(station.StationNo, "[步骤2/5 启动误差] 开始读取标准表脉冲常数。", stationLogger);
        }

        (bool constantSuccess, ulong standardConstant, string constantMessage) =
            await ReadStandardActiveConstantAsync(cancellationToken).ConfigureAwait(false);
        if (!constantSuccess)
        {
            return CreateFailureResult(selectedStations, constantMessage, stationLogger, sourceResult.StandValues);
        }

        foreach (MeterTestStationCommunication station in selectedStations)
        {
            Trace(station.StationNo, $"[步骤2/5 启动误差] {constantMessage}", stationLogger);
        }

        ConcurrentDictionary<int, MeterTestBasicErrorStationResult> stationResults = new();
        List<MeterTestControlPcbGroup> groups = ResolveControlPcbGroups(planConfig, subItem);
        if (groups.Count == 0)
        {
            return CreateFailureResult(selectedStations, "未找到可用控制PCB分组，请检查 ControlPcbGroups。", stationLogger, sourceResult.StandValues);
        }

        Dictionary<int, MeterTestBasicErrorStationPlan> planByStation = point.Stations
            .ToDictionary(item => item.StationNo);
        HashSet<int> mappedStations = groups
            .SelectMany(group => GetTargets(group, selectedStations))
            .Select(target => target.StationNo)
            .ToHashSet();
        foreach (MeterTestStationCommunication station in selectedStations.Where(
                     station => !mappedStations.Contains(station.StationNo)))
        {
            string message = "当前工位未映射到可用控制PCB分组。";
            stationResults[station.StationNo] = MeterTestBasicErrorStationResult.Fail(station.StationNo, message);
            Trace(station.StationNo, message, stationLogger);
        }

        ConcurrentDictionary<int, bool> startedStations = new();
        List<Task> startTasks = groups
            .Select(group => StartGroupSafelyAsync(
                group,
                selectedStations,
                planByStation,
                standardConstant,
                subItem,
                startedStations,
                stationResults,
                stationLogger,
                cancellationToken))
            .ToList();
        await Task.WhenAll(startTasks).ConfigureAwait(false);

        List<MeterTestBasicErrorStationPlan> activePlans = point.Stations
            .Where(item => startedStations.ContainsKey(item.StationNo))
            .ToList();
        if (activePlans.Count > 0)
        {
            int waitSeconds = activePlans.Max(item => item.WaitSeconds);
            foreach (MeterTestBasicErrorStationPlan stationPlan in activePlans)
            {
                Trace(
                    stationPlan.StationNo,
                    $"[步骤3/5 等待] 开始倒计时：统一等待{waitSeconds}s，当前工位理论等待={stationPlan.WaitSeconds}s。",
                    stationLogger);
            }

            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken).ConfigureAwait(false);
            foreach (MeterTestBasicErrorStationPlan stationPlan in activePlans)
            {
                Trace(stationPlan.StationNo, "[步骤3/5 等待] 倒计时结束。", stationLogger);
            }

            List<Task> readTasks = groups
                .Select(group => ReadAndJudgeGroupSafelyAsync(
                    group,
                    selectedStations,
                    planByStation,
                    startedStations,
                    subItem,
                    stationResults,
                    stationLogger,
                    cancellationToken))
                .ToList();
            await Task.WhenAll(readTasks).ConfigureAwait(false);
        }

        foreach (MeterTestStationCommunication station in selectedStations)
        {
            if (stationResults.ContainsKey(station.StationNo))
                continue;

            string message = startedStations.ContainsKey(station.StationNo)
                ? "基本误差结果未完成读取或判定。"
                : "A2/A0/0x38启动流程未完成。";
            stationResults[station.StationNo] = MeterTestBasicErrorStationResult.Fail(station.StationNo, message);
            Trace(station.StationNo, message, stationLogger);
        }

        bool success = selectedStations.All(station =>
            stationResults.TryGetValue(station.StationNo, out MeterTestBasicErrorStationResult? result) && result.Success);
        string summary = success
            ? $"基本误差测试点 {point.TestPointName} 全部工位合格。"
            : $"基本误差测试点 {point.TestPointName} 存在不合格或未完成工位。";
        foreach (MeterTestStationCommunication station in selectedStations)
        {
            string stationConclusion = stationResults.TryGetValue(
                station.StationNo,
                out MeterTestBasicErrorStationResult? stationResult)
                    ? stationResult.Success ? "合格" : "不合格"
                    : "未完成";
            Trace(
                station.StationNo,
                $"[流程结束] 测试点={point.TestPointName}，结论={stationConclusion}。",
                stationLogger);
            Trace(station.StationNo, "-----------------------------------------------------------------", stationLogger);
        }

        return new MeterTestBasicErrorExecutionResult(success, summary, stationResults, sourceResult.StandValues);
    }

    private async Task StartGroupSafelyAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterTestBasicErrorStationPlan> planByStation,
        ulong standardConstant,
        MeterTestSubItem subItem,
        ConcurrentDictionary<int, bool> startedStations,
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            await StartGroupAsync(
                group,
                selectedStations,
                planByStation,
                standardConstant,
                subItem,
                startedStations,
                stationResults,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            string message = $"控制PCB组 {group.Name} 基本误差启动异常：{ex.Message}";
            SetGroupFailure(GetTargets(group, selectedStations), stationResults, message, stationLogger);
            LogMessage.Error($"[基本误差] {message}", ex);
        }
    }

    private async Task ReadAndJudgeGroupSafelyAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterTestBasicErrorStationPlan> planByStation,
        IReadOnlyDictionary<int, bool> startedStations,
        MeterTestSubItem subItem,
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            await ReadAndJudgeGroupAsync(
                group,
                selectedStations,
                planByStation,
                startedStations,
                subItem,
                stationResults,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            string message = $"控制PCB组 {group.Name} 基本误差读取异常：{ex.Message}";
            SetGroupFailure(
                GetTargets(group, selectedStations).Where(target => startedStations.ContainsKey(target.StationNo)),
                stationResults,
                message,
                stationLogger);
            LogMessage.Error($"[基本误差] {message}", ex);
        }
    }

    private async Task StartGroupAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterTestBasicErrorStationPlan> planByStation,
        ulong standardConstant,
        MeterTestSubItem subItem,
        ConcurrentDictionary<int, bool> startedStations,
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        List<BasicErrorTarget> targets = GetTargets(group, selectedStations);
        if (targets.Count == 0)
            return;

        if (!group.ProtocolVersion.Equals(MeterControlPcbProtocolVersion.V2.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            SetGroupFailure(targets, stationResults, $"控制PCB组 {group.Name} 不是V2协议。", stationLogger);
            return;
        }

        if (!connectionManager.TryGetConnectedConnection(group, out MeterTestControlPcbConnection connection, out string connectionError))
        {
            SetGroupFailure(targets, stationResults, connectionError, stationLogger);
            return;
        }

        List<BasicErrorTarget> activeTargets = targets
            .Where(target => planByStation.ContainsKey(target.StationNo))
            .ToList();
        TimeSpan timeout = TimeSpan.FromMilliseconds(Math.Max(100, subItem.TimeoutMs));
        TimeSpan interval = TimeSpan.FromMilliseconds(Math.Max(0, subItem.PacketIntervalMs));
        byte[] standardPayload = ToLittleEndianBytes(standardConstant);
        Dictionary<byte, byte[]> a2Responses = await SendAndCollectAsync(
            connection,
            activeTargets,
            target => ElectricEnergyMeterControlV2.BuildBasicErrorStandardConstantPacket(target.MeterAddress, standardConstant),
            target => $"[步骤2/5 启动误差] A2设置标准表有功常数={standardConstant}",
            frame => ResolveSettingResponse(frame, activeTargets, 0xA2, _ => standardPayload),
            timeout,
            interval,
            stationLogger,
            cancellationToken).ConfigureAwait(false);
        activeTargets = KeepRespondedTargets(
            activeTargets,
            a2Responses,
            stationResults,
            "[步骤2/5 启动误差] A2设置标准表常数未收到正确应答",
            stationLogger);

        if (activeTargets.Count > 0)
        {
            Dictionary<byte, byte[]> a0Responses = await SendAndCollectAsync(
                connection,
                activeTargets,
                target => ElectricEnergyMeterControlV2.BuildBasicErrorMeterConstantPacket(
                    target.MeterAddress,
                    ToMeterConstant(planByStation[target.StationNo].MeterConstant)),
                target => $"[步骤2/5 启动误差] A0设置电能表有功常数={planByStation[target.StationNo].MeterConstant:0}",
                frame => ResolveSettingResponse(
                    frame,
                    activeTargets,
                    0xA0,
                    target => ToLittleEndianBytes(ToMeterConstant(planByStation[target.StationNo].MeterConstant))),
                timeout,
                interval,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
            activeTargets = KeepRespondedTargets(
                activeTargets,
                a0Responses,
                stationResults,
                "[步骤2/5 启动误差] A0设置电能表常数未收到正确应答",
                stationLogger);
        }

        if (activeTargets.Count > 0)
        {
            Dictionary<byte, byte[]> startResponses = await SendAndCollectAsync(
                connection,
                activeTargets,
                target => ElectricEnergyMeterControlV2.BuildBasicError38StartPacket(
                    target.MeterAddress,
                    planByStation[target.StationNo].PulseCount,
                    planByStation[target.StationNo].TestCount),
                target => $"[步骤2/5 启动误差] 0x38+00启动，脉冲数={planByStation[target.StationNo].PulseCount}，次数={planByStation[target.StationNo].TestCount}",
                frame => ResolveStartResponse(frame, activeTargets, planByStation),
                timeout,
                interval,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
            activeTargets = KeepRespondedTargets(
                activeTargets,
                startResponses,
                stationResults,
                "[步骤2/5 启动误差] 0x38+00未收到正确应答",
                stationLogger);
        }

        foreach (BasicErrorTarget target in activeTargets)
        {
            startedStations[target.StationNo] = true;
            Trace(target.StationNo, "[步骤2/5 启动误差] A2、A0和0x38+00应答全部正常。", stationLogger);
        }
    }

    private async Task ReadAndJudgeGroupAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterTestBasicErrorStationPlan> planByStation,
        IReadOnlyDictionary<int, bool> startedStations,
        MeterTestSubItem subItem,
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        List<BasicErrorTarget> targets = GetTargets(group, selectedStations)
            .Where(target => startedStations.ContainsKey(target.StationNo))
            .ToList();
        if (targets.Count == 0)
            return;

        if (!connectionManager.TryGetConnectedConnection(group, out MeterTestControlPcbConnection connection, out string connectionError))
        {
            SetGroupFailure(targets, stationResults, connectionError, stationLogger);
            return;
        }

        Dictionary<byte, byte[]> responses = await SendAndCollectAsync(
            connection,
            targets,
            target => ElectricEnergyMeterControlV2.BuildBasicError38ResultPacket(
                target.MeterAddress,
                planByStation[target.StationNo].PulseCount,
                planByStation[target.StationNo].TestCount),
            target => $"[步骤4/5 读取误差] 0x38+AA读取，脉冲数={planByStation[target.StationNo].PulseCount}，次数={planByStation[target.StationNo].TestCount}",
            frame => ResolveResultResponse(frame, targets, planByStation),
            TimeSpan.FromMilliseconds(Math.Max(100, subItem.TimeoutMs)),
            TimeSpan.FromMilliseconds(Math.Max(0, subItem.PacketIntervalMs)),
            stationLogger,
            cancellationToken).ConfigureAwait(false);

        foreach (BasicErrorTarget target in targets)
        {
            MeterTestBasicErrorStationPlan stationPlan = planByStation[target.StationNo];
            if (!responses.TryGetValue(target.MeterAddress, out byte[]? response))
            {
                string message = "[步骤4/5 读取误差] 未收到0x38+AA基本误差结果应答。";
                stationResults[target.StationNo] = MeterTestBasicErrorStationResult.Fail(target.StationNo, message);
                Trace(target.StationNo, message, stationLogger);
                continue;
            }

            if (!ElectricEnergyMeterControlV2.TryParseBasicError38ResultResponse(
                    response,
                    target.MeterAddress,
                    stationPlan.PulseCount,
                    stationPlan.TestCount,
                    out IReadOnlyList<float> errors,
                    out string parseMessage))
            {
                string message = $"[步骤4/5 读取误差] 结果解析失败：{parseMessage}";
                stationResults[target.StationNo] = MeterTestBasicErrorStationResult.Fail(target.StationNo, message);
                Trace(target.StationNo, message, stationLogger);
                continue;
            }

            if (errors.Count != stationPlan.TestCount)
            {
                string message = $"[步骤4/5 读取误差] 结果尚未完成：{parseMessage}";
                stationResults[target.StationNo] = MeterTestBasicErrorStationResult.Fail(target.StationNo, message);
                Trace(target.StationNo, message, stationLogger);
                continue;
            }

            if (errors.Any(value => Math.Abs(value - 1.0f) < 0.000001f))
            {
                const string message = "[步骤5/5 判定] 误差结果为1.0，表示待测表未输出一个完整脉冲，结论：不合格";
                stationResults[target.StationNo] = MeterTestBasicErrorStationResult.Fail(target.StationNo, message);
                Trace(target.StationNo, message, stationLogger);
                continue;
            }

            if (errors.Any(value => Math.Abs(value - 2.0f) < 0.000001f))
            {
                const string message = "[步骤5/5 判定] 误差结果为2.0，表示当前试验尚未计算完成，结论：不合格";
                stationResults[target.StationNo] = MeterTestBasicErrorStationResult.Fail(target.StationNo, message);
                Trace(target.StationNo, message, stationLogger);
                continue;
            }

            decimal averageError = errors.Select(value => (decimal)value).Average();
            bool passed = Math.Abs(averageError) <= stationPlan.ErrorLimit;
            string resultText = string.Join("、", errors.Select(value => value.ToString("0.######", CultureInfo.InvariantCulture)));
            string conclusion = passed ? "合格" : "不合格";
            string resultMessage =
                $"[步骤4/5 读取误差] 解析结果：{resultText}。"
                + $"[步骤5/5 判定] 平均误差：{averageError:0.######}%，"
                + $"标准值：±{stationPlan.ErrorLimit:0.######}%，结论：{conclusion}";
            stationResults[target.StationNo] = new MeterTestBasicErrorStationResult(
                target.StationNo,
                passed,
                averageError,
                stationPlan.ErrorLimit,
                resultMessage);
            Trace(target.StationNo, resultMessage, stationLogger);
        }
    }

    private static async Task<Dictionary<byte, byte[]>> SendAndCollectAsync(
        MeterTestControlPcbConnection connection,
        List<BasicErrorTarget> targets,
        Func<BasicErrorTarget, byte[]> packetFactory,
        Func<BasicErrorTarget, string> descriptionFactory,
        Func<byte[], byte?> responseResolver,
        TimeSpan timeout,
        TimeSpan interval,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        Dictionary<byte, TaskCompletionSource<byte[]>> pending = targets.ToDictionary(
            target => target.MeterAddress,
            _ => new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously));
        using IDisposable subscription = connection.Subscribe(frame =>
        {
            byte? meterAddress = responseResolver(frame);
            if (meterAddress.HasValue && pending.TryGetValue(meterAddress.Value, out TaskCompletionSource<byte[]>? source))
            {
                source.TrySetResult(frame);
            }
        });

        byte[][] packets = targets.Select(packetFactory).ToArray();
        await connection.SendSequenceAsync(
            packets,
            interval,
            (index, packet) =>
            {
                BasicErrorTarget target = targets[index];
                Trace(
                    target.StationNo,
                    $"{FormatTimestamp()} - 发送报文：{ToHex(packet)}，{descriptionFactory(target)}",
                    stationLogger);
            },
            cancellationToken).ConfigureAwait(false);

        Task allResponses = Task.WhenAll(pending.Values.Select(item => item.Task));
        Task completed = await Task.WhenAny(allResponses, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);
        if (completed != allResponses)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        Dictionary<byte, byte[]> responses = new();
        foreach ((byte address, TaskCompletionSource<byte[]> source) in pending)
        {
            if (!source.Task.IsCompletedSuccessfully)
                continue;

            byte[] frame = source.Task.Result;
            responses[address] = frame;
            BasicErrorTarget target = targets.First(item => item.MeterAddress == address);
            Trace(target.StationNo, $"{FormatTimestamp()} - 接受报文：{ToHex(frame)}", stationLogger);
        }

        return responses;
    }

    private static byte? ResolveSettingResponse(
        byte[] frame,
        IReadOnlyList<BasicErrorTarget> targets,
        byte command,
        Func<BasicErrorTarget, byte[]> payloadFactory)
    {
        if (frame.Length <= 5)
            return null;

        byte address = frame[5];
        BasicErrorTarget? target = targets.FirstOrDefault(item => item.MeterAddress == address);
        return target is not null && ElectricEnergyMeterControlV2.IsExpectedBasicErrorSettingResponse(
            frame,
            address,
            command,
            payloadFactory(target))
            ? address
            : null;
    }

    private static byte? ResolveStartResponse(
        byte[] frame,
        IReadOnlyList<BasicErrorTarget> targets,
        IReadOnlyDictionary<int, MeterTestBasicErrorStationPlan> planByStation)
    {
        if (frame.Length <= 5)
            return null;

        byte address = frame[5];
        BasicErrorTarget? target = targets.FirstOrDefault(item => item.MeterAddress == address);
        if (target is null)
            return null;

        MeterTestBasicErrorStationPlan stationPlan = planByStation[target.StationNo];
        return ElectricEnergyMeterControlV2.IsExpectedBasicError38StartResponse(
            frame,
            address,
            stationPlan.PulseCount,
            stationPlan.TestCount)
            ? address
            : null;
    }

    private static byte? ResolveResultResponse(
        byte[] frame,
        IReadOnlyList<BasicErrorTarget> targets,
        IReadOnlyDictionary<int, MeterTestBasicErrorStationPlan> planByStation)
    {
        if (frame.Length <= 5)
            return null;

        byte address = frame[5];
        BasicErrorTarget? target = targets.FirstOrDefault(item => item.MeterAddress == address);
        if (target is null)
            return null;

        MeterTestBasicErrorStationPlan stationPlan = planByStation[target.StationNo];
        return ElectricEnergyMeterControlV2.TryParseBasicError38ResultResponse(
            frame,
            address,
            stationPlan.PulseCount,
            stationPlan.TestCount,
            out _,
            out _)
            ? address
            : null;
    }

    private static List<BasicErrorTarget> KeepRespondedTargets(
        IEnumerable<BasicErrorTarget> targets,
        IReadOnlyDictionary<byte, byte[]> responses,
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        string failureMessage,
        Action<int, string>? stationLogger)
    {
        List<BasicErrorTarget> responded = new();
        foreach (BasicErrorTarget target in targets)
        {
            if (responses.ContainsKey(target.MeterAddress))
            {
                responded.Add(target);
                continue;
            }

            stationResults[target.StationNo] = MeterTestBasicErrorStationResult.Fail(target.StationNo, failureMessage);
            Trace(target.StationNo, failureMessage, stationLogger);
        }

        return responded;
    }

    private static List<MeterTestControlPcbGroup> ResolveControlPcbGroups(
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

    private static List<BasicErrorTarget> GetTargets(
        MeterTestControlPcbGroup group,
        IReadOnlyList<MeterTestStationCommunication> selectedStations)
    {
        List<BasicErrorTarget> targets = new();
        foreach (MeterTestStationCommunication station in selectedStations)
        {
            if (station.StationNo < group.StationStart || station.StationNo > group.StationEnd)
                continue;

            int meterAddress = group.MeterAddressStart + station.StationNo - group.StationStart;
            if (meterAddress is < 1 or > 254)
                continue;

            targets.Add(new BasicErrorTarget(station.StationNo, (byte)meterAddress));
        }

        return targets;
    }

    private static async Task<(bool Success, ulong Constant, string Message)> ReadStandardActiveConstantAsync(
        CancellationToken cancellationToken)
    {
        if (!XYCtr.IsSourcePortOpen)
            return (false, 0, "源串口尚未打开，无法读取标准表脉冲常数。");

        using XYCtr xyCtr = new();
        byte[] buffer = new byte[1024];
        (bool success, int result) = await xyCtr
            .CallReadStandConstAsync(buffer, TimeSpan.FromSeconds(5))
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!success)
            return (false, 0, $"读取标准表脉冲常数失败，XYCtr返回值={result}。");

        string rawValue = Encoding.Default.GetString(buffer).TrimEnd('\0', '\r', '\n', ' ');
        Match match = Regex.Match(rawValue, @"\d+");
        if (!match.Success ||
            !ulong.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong standardConstant) ||
            standardConstant == 0)
        {
            return (false, 0, $"标准表脉冲常数解析失败，原始返回={rawValue}。");
        }

        return (true, standardConstant, $"读取标准表脉冲常数成功：{standardConstant}。");
    }

    private static uint ToMeterConstant(decimal value)
    {
        if (value <= 0 || value > uint.MaxValue || value != decimal.Truncate(value))
            throw new InvalidOperationException($"电能表有功常数必须是1-{uint.MaxValue}之间的整数：{value}。");

        return decimal.ToUInt32(value);
    }

    private static byte[] ToLittleEndianBytes(ulong value)
    {
        byte[] bytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        return bytes;
    }

    private static byte[] ToLittleEndianBytes(uint value)
    {
        byte[] bytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    private static void SetGroupFailure(
        IEnumerable<BasicErrorTarget> targets,
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        string message,
        Action<int, string>? stationLogger)
    {
        foreach (BasicErrorTarget target in targets)
        {
            stationResults[target.StationNo] = MeterTestBasicErrorStationResult.Fail(target.StationNo, message);
            Trace(target.StationNo, message, stationLogger);
        }
    }

    private static MeterTestBasicErrorExecutionResult CreateFailureResult(
        IReadOnlyList<MeterTestStationCommunication> stations,
        string message,
        Action<int, string>? stationLogger,
        IReadOnlyDictionary<string, string>? standValues = null)
    {
        Dictionary<int, MeterTestBasicErrorStationResult> results = new();
        foreach (MeterTestStationCommunication station in stations)
        {
            results[station.StationNo] = MeterTestBasicErrorStationResult.Fail(station.StationNo, message);
            Trace(station.StationNo, $"[流程终止] {message}", stationLogger);
            Trace(station.StationNo, "-----------------------------------------------------------------", stationLogger);
        }

        return new MeterTestBasicErrorExecutionResult(false, message, results, standValues);
    }

    private static void Trace(int stationNo, string message, Action<int, string>? stationLogger)
    {
        LogMessage.Debug($"[基本误差][工位{stationNo}] {message}");
        stationLogger?.Invoke(stationNo, message);
    }

    /// <summary>
    /// 将源控制的打开串口、初始化、Adj下发及20秒标准表验证进度同步到每个选中工位。
    /// 源控制服务本身已写入全局Debug日志，这里只转发工位过程日志，避免全局日志重复。
    /// </summary>
    private static void ForwardSourceProgress(
        IEnumerable<MeterTestStationCommunication> stations,
        string message,
        Action<int, string>? stationLogger)
    {
        if (stationLogger is null)
            return;

        foreach (MeterTestStationCommunication station in stations)
        {
            stationLogger(station.StationNo, $"[步骤1/5 升源] {message}");
        }
    }

    private static string ToHex(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }

    private static string FormatTimestamp()
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";
    }

    private sealed record BasicErrorTarget(int StationNo, byte MeterAddress);
}

/// <summary>单个基本误差小项的整体执行结果。</summary>
public sealed record MeterTestBasicErrorExecutionResult(
    bool Success,
    string Message,
    IReadOnlyDictionary<int, MeterTestBasicErrorStationResult> StationResults,
    IReadOnlyDictionary<string, string>? StandValues);

/// <summary>单个工位的基本误差最终判定。</summary>
public sealed record MeterTestBasicErrorStationResult(
    int StationNo,
    bool Success,
    decimal? AverageError,
    decimal ErrorLimit,
    string Message)
{
    public static MeterTestBasicErrorStationResult Fail(int stationNo, string message)
    {
        return new MeterTestBasicErrorStationResult(stationNo, false, null, 0, message);
    }
}
