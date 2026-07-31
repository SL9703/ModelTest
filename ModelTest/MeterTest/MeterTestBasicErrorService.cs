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
    private const int MaxStationCount = 48;
    private readonly MeterTestSourceControlService sourceControlService;
    private readonly MeterTestControlPcbConnectionManager connectionManager;
    private readonly MeterTestCountdownService countdownService;
    private readonly MeterTestAccessDatabaseService databaseService;
    private readonly ConcurrentDictionary<string, Lazy<Task<MeterTestBasicErrorExecutionResult>>> executionBatches = new();

    /// <summary>创建基本误差服务并注入源控制、控制 PCB 长连接、数据库和倒计时服务。</summary>
    public MeterTestBasicErrorService(
        MeterTestSourceControlService sourceControlService,
        MeterTestControlPcbConnectionManager connectionManager,
        MeterTestCountdownService countdownService,
        MeterTestAccessDatabaseService databaseService)
    {
        this.sourceControlService = sourceControlService;
        this.connectionManager = connectionManager;
        this.countdownService = countdownService;
        this.databaseService = databaseService;
    }

    /// <summary>
    /// 开始一轮新的界面测试，清除上一轮的基本误差批次任务。
    /// 每次点击“执行测试”只调用一次，不能在工位循环中调用。
    /// </summary>
    public void BeginRun()
    {
        executionBatches.Clear();
    }

    /// <summary>
    /// 执行方案中的一个正向或反向有功基本误差点，并生成可直接回填界面和结果库的数据。
    /// 资产加载、工位通信模型转换、五步试验流程以及测量值组装都在服务内完成；
    /// 调用方只负责把返回结果刷新到 WinForms 控件。
    /// </summary>
    internal async Task<MeterTestBasicErrorWorkflowResult> ExecutePointAsync(
        string runId,
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
            databaseService.LoadOrCreateMeterArchives(MaxStationCount);
        List<MeterTestStationCommunication> sourceStations = selectedStations
            .Select(station => new MeterTestStationCommunication
            {
                StationNo = station.StationNo,
                Ip = station.Ip,
                Port = station.Port
            })
            .ToList();

        MeterTestBasicErrorExecutionResult executionResult = await ExecuteAsync(
            runId,
            planConfig,
            context.SubItem,
            sourceStations,
            meterArchives,
            stationLogger,
            cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MeterTestMeasurementData> measurements = BuildMeasurements(
            context,
            selectedStations,
            executionResult);

        return new MeterTestBasicErrorWorkflowResult(
            executionResult,
            measurements,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 执行一个正向或反向有功基本误差测试点。
    /// 同一运行批次、同一测试点只创建一个完整任务；重复入口共享该任务，防止多工位重复初始化和升源。
    /// </summary>
    public Task<MeterTestBasicErrorExecutionResult> ExecuteAsync(
        string runId,
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        string stationSet = string.Join(",", selectedStations
            .Select(station => station.StationNo)
            .OrderBy(stationNo => stationNo));
        string batchKey = $"{runId}|{subItem.Name}|{stationSet}";
        Lazy<Task<MeterTestBasicErrorExecutionResult>> candidate = new(
            () => ExecuteCoreAsync(
                runId,
                planConfig,
                subItem,
                selectedStations,
                meterArchives,
                stationLogger,
                cancellationToken),
            LazyThreadSafetyMode.ExecutionAndPublication);
        Lazy<Task<MeterTestBasicErrorExecutionResult>> batch = executionBatches.GetOrAdd(batchKey, candidate);

        if (ReferenceEquals(batch, candidate))
        {
            LogMessage.Debug(
                $"[基本误差][批次源控制] 创建唯一批次：测试点={subItem.Name}，工位数={selectedStations.Count}，"
                + "执行前源初始化已完成，本测试点只执行一次升源接口。");
        }
        else
        {
            LogMessage.Debug(
                $"[基本误差][批次源控制] 复用已有批次：测试点={subItem.Name}，工位数={selectedStations.Count}，"
                + "不重复初始化、不重复升源。");
        }

        return batch.Value;
    }

    /// <summary>基本误差测试点的实际五步流程，由批次入口保证只执行一次。</summary>
    private async Task<MeterTestBasicErrorExecutionResult> ExecuteCoreAsync(
        string runId,
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
                databaseService.LoadPowerFactorAngles(),
                out MeterTestBasicErrorExecutionPlan? executionPlan,
                out string? calculationError))
        {
            return CreateFailureResult(selectedStations, calculationError ?? "基本误差测试点参数计算失败。", stationLogger);
        }

        MeterTestBasicErrorExecutionPlan point = executionPlan!;
        string batchSummary =
            $"公共源批次包含{selectedStations.Count}个工位；本测试点复用执行方案阶段的源连接参数，只调用一次AnyUIOutput升源接口。";
        foreach (MeterTestBasicErrorStationPlan stationPlan in point.Stations)
        {
            Trace(stationPlan.StationNo, "-----------------------------------------------------------------", stationLogger);
            Trace(stationPlan.StationNo, $"[流程开始] 基本误差测试点：{point.TestPointName}", stationLogger);
            Trace(stationPlan.StationNo, $"[批次源控制] {batchSummary}", stationLogger);
            Trace(
                stationPlan.StationNo,
                $"[参数版本] 0x38脉冲数协议上限={MeterTestBasicErrorDefaults.MaxPulseCount}，"
                + $"结果等待余量={MeterTestBasicErrorDefaults.WaitPaddingSeconds}s。",
                stationLogger);
            Trace(
                stationPlan.StationNo,
                $"[步骤1/5 升源] FA角度配置：{point.Direction}/{point.PowerFactorText} "
                + $"=> {point.CurrentAngle:0.######}°（数据库MeterTestPowerFactorAngle）。",
                stationLogger);
            Trace(stationPlan.StationNo, $"[步骤1/5 升源] 参数计算：{stationPlan.CalculationNote}", stationLogger);
        }

        string sourceBatchKey = $"{runId}|BasicErrorPoint|{subItem.Name}";
        MeterTestSourceControlService.MeterTestSourceControlResult sourceResult = await sourceControlService.ExecuteBatchOnceAsync(
            sourceBatchKey,
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
        List<MeterTestControlPcbGroup> groups = ResolveControlPcbGroups(planConfig, subItem, selectedStations);
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
            SetStationFailureUnlessSucceeded(stationResults, station.StationNo, message, stationLogger);
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
                    $"[步骤3/5 等待] 开始倒计时：统一等待{waitSeconds}s，"
                    + $"当前工位={stationPlan.SingleRoundWaitSeconds}s×次数{stationPlan.TestCount}"
                    + $"+{MeterTestBasicErrorDefaults.WaitPaddingSeconds}s余量={stationPlan.WaitSeconds}s。",
                    stationLogger);
            }

            await countdownService
                .DelayAsync(waitSeconds, subItem.Name, cancellationToken)
                .ConfigureAwait(false);
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
                "[流程结束]",
                stationLogger);
            Trace(station.StationNo, $"测试点：{point.TestPointName}", stationLogger);
            Trace(station.StationNo, $"最终结论：{stationConclusion}", stationLogger);
            Trace(station.StationNo, "-----------------------------------------------------------------", stationLogger);
        }

        return new MeterTestBasicErrorExecutionResult(success, summary, stationResults, sourceResult.StandValues);
    }

    /// <summary>隔离单个控制 PCB 分组启动异常，确保其它分组和工位继续执行。</summary>
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

    /// <summary>隔离单个控制 PCB 分组读取或判定异常，并把失败结果落到该组目标工位。</summary>
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

    /// <summary>向一个控制 PCB 分组依次发送 A2、A0 和 0x38+00，并筛选已正确应答的工位。</summary>
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

    /// <summary>按轮次等待并发送 0x38+AA，累计误差结果后计算平均值和允许区间结论。</summary>
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
            .Where(target => !HasSucceeded(stationResults, target.StationNo))
            .ToList();
        if (targets.Count == 0)
            return;

        if (!connectionManager.TryGetConnectedConnection(group, out MeterTestControlPcbConnection connection, out string connectionError))
        {
            SetGroupFailure(targets, stationResults, connectionError, stationLogger);
            return;
        }

        TimeSpan timeout = TimeSpan.FromMilliseconds(Math.Max(100, subItem.TimeoutMs));
        TimeSpan interval = TimeSpan.FromMilliseconds(Math.Max(0, subItem.PacketIntervalMs));
        Dictionary<int, int> supplementalWaitCounts = new();
        Dictionary<int, int> supplementalWaitLimits = new();
        List<BasicErrorTarget> pendingTargets = targets;

        while (pendingTargets.Count > 0)
        {
            List<BasicErrorTarget> currentTargets = pendingTargets;
            Dictionary<byte, byte[]> responses = await SendAndCollectAsync(
                connection,
                currentTargets,
                target => ElectricEnergyMeterControlV2.BuildBasicError38ResultPacket(
                    target.MeterAddress,
                    planByStation[target.StationNo].PulseCount,
                    planByStation[target.StationNo].TestCount),
                target => supplementalWaitCounts.TryGetValue(target.StationNo, out int supplementalRound)
                    ? $"[步骤4/5 读取误差] {FormatReadOrdinal(supplementalRound + 1)}读取误差结果，"
                        + $"0x38+AA，脉冲数={planByStation[target.StationNo].PulseCount}，次数={planByStation[target.StationNo].TestCount}"
                    : $"[步骤4/5 读取误差] 第一次读取误差结果，"
                        + $"0x38+AA，脉冲数={planByStation[target.StationNo].PulseCount}，次数={planByStation[target.StationNo].TestCount}",
                frame => ResolveResultResponse(frame, currentTargets, planByStation),
                timeout,
                interval,
                stationLogger,
                cancellationToken).ConfigureAwait(false);

            List<BasicErrorTarget> nextPendingTargets = new();
            foreach (BasicErrorTarget target in currentTargets)
            {
                MeterTestBasicErrorStationPlan stationPlan = planByStation[target.StationNo];
                if (!responses.TryGetValue(target.MeterAddress, out byte[]? response))
                {
                    string message = "[步骤4/5 读取误差] 未收到0x38+AA基本误差结果应答。";
                    SetStationFailureUnlessSucceeded(stationResults, target.StationNo, message, stationLogger);
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
                    SetStationFailureUnlessSucceeded(stationResults, target.StationNo, message, stationLogger);
                    continue;
                }

                int pendingValueCount = errors.Count(value => Math.Abs(value - 2.0f) < 0.000001f);
                bool resultIncomplete = errors.Count < stationPlan.TestCount || pendingValueCount > 0;
                if (resultIncomplete)
                {
                    if (!supplementalWaitLimits.TryGetValue(target.StationNo, out int supplementalWaitLimit))
                    {
                        // 首次读取之外，最多再等待“试验次数”个完整单轮时间。
                        // 例如次数=2、第二次读取仍只有1/2时，还允许再等待一轮并执行第三次读取。
                        supplementalWaitLimit = Math.Max(1, (int)stationPlan.TestCount);
                        supplementalWaitLimits[target.StationNo] = supplementalWaitLimit;
                    }

                    int completedSupplementalWaits = supplementalWaitCounts.TryGetValue(
                        target.StationNo,
                        out int waitCount)
                            ? waitCount
                            : 0;
                    if (completedSupplementalWaits >= supplementalWaitLimit)
                    {
                        string timeoutMessage =
                            $"[步骤4/5 读取误差] 已补等允许的{completedSupplementalWaits}轮后结果仍未完成：{parseMessage}"
                            + (pendingValueCount > 0 ? $"，其中{pendingValueCount}个结果为2.0（尚未计算完成）。" : string.Empty);
                        SetStationFailureUnlessSucceeded(stationResults, target.StationNo, timeoutMessage, stationLogger);
                        continue;
                    }

                    int nextSupplementalRound = completedSupplementalWaits + 1;
                    supplementalWaitCounts[target.StationNo] = nextSupplementalRound;
                    nextPendingTargets.Add(target);
                    int currentReadSequence = completedSupplementalWaits + 1;
                    int nextReadSequence = nextSupplementalRound + 1;
                    string pendingMessage =
                        $"[步骤4/5 读取误差] {FormatReadOrdinal(currentReadSequence)}读取后结果尚未完成：{parseMessage}"
                        + (pendingValueCount > 0 ? $"，其中{pendingValueCount}个结果为2.0（尚未计算完成）。" : string.Empty)
                        + $" 将等待{FormatReadOrdinal(nextReadSequence)}误差结果。";
                    Trace(target.StationNo, pendingMessage, stationLogger);
                    continue;
                }

                if (errors.Any(value => Math.Abs(value - 1.0f) < 0.000001f))
                {
                    const string message = "[步骤5/5 判定] 误差结果为1.0，表示待测表未输出一个完整脉冲，结论：不合格";
                    SetStationFailureUnlessSucceeded(stationResults, target.StationNo, message, stationLogger);
                    continue;
                }

                decimal averageError = errors.Select(value => (decimal)value).Average();
                MeterTestErrorComparisonResult comparison = MeterTestErrorResultComparer.Compare(
                    stationPlan.ErrorLimitResult,
                    averageError);
                bool passed = comparison.Passed;
                string resultText = string.Join("、", errors.Select(value => value.ToString("0.######", CultureInfo.InvariantCulture)));
                string conclusion = passed ? "合格" : "不合格";
                string withinRangeText = passed ? "是" : "否";
                string resultMessage =
                    $"[步骤4/5 读取误差] 已获取{errors.Count}次误差结果：{resultText}。"
                    + Environment.NewLine
                    + $"[步骤5/5 判定] 误差平均值：{averageError:0.######}%，"
                    + $"最大允许误差区间：[-{stationPlan.MaximumPermittedErrorLimit:0.######}%, +{stationPlan.MaximumPermittedErrorLimit:0.######}%]，"
                    + $"60%判定区间：[-{stationPlan.ErrorLimit:0.######}%, +{stationPlan.ErrorLimit:0.######}%]，"
                    + $"是否在误差范围区间内：{withinRangeText}，结论：{conclusion}。"
                    + Environment.NewLine
                    + $"[判定说明] {comparison.Message}";
                stationResults[target.StationNo] = new MeterTestBasicErrorStationResult(
                    target.StationNo,
                    passed,
                    errors.Select(value => (decimal)value).ToList(),
                    averageError,
                    stationPlan.MaximumPermittedErrorLimit,
                    stationPlan.ErrorLimit,
                    resultMessage);
                Trace(target.StationNo, resultMessage, stationLogger);
            }

            if (nextPendingTargets.Count == 0)
                break;

            int supplementalWaitSeconds = nextPendingTargets.Max(target =>
                planByStation[target.StationNo].SingleRoundWaitSeconds);
            foreach (BasicErrorTarget target in nextPendingTargets)
            {
                MeterTestBasicErrorStationPlan stationPlan = planByStation[target.StationNo];
                int supplementalRound = supplementalWaitCounts[target.StationNo];
                Trace(
                    target.StationNo,
                    $"[步骤3/5 等待] 开始等待{FormatReadOrdinal(supplementalRound + 1)}误差结果："
                    + $"统一等待{supplementalWaitSeconds}s，"
                    + $"当前工位单轮等待={stationPlan.SingleRoundWaitSeconds}s。",
                    stationLogger);
            }

            await countdownService
                .DelayAsync(supplementalWaitSeconds, $"{subItem.Name}（补读）", cancellationToken)
                .ConfigureAwait(false);
            foreach (BasicErrorTarget target in nextPendingTargets)
            {
                Trace(
                    target.StationNo,
                    $"[步骤3/5 等待] {FormatReadOrdinal(supplementalWaitCounts[target.StationNo] + 1)}"
                    + "误差结果等待结束，开始读取0x38+AA累计结果。",
                    stationLogger);
            }

            pendingTargets = nextPendingTargets;
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
            Trace(target.StationNo, $"{FormatTimestamp()} - 接收报文：{ToHex(frame)}", stationLogger);
        }

        return responses;
    }

    /// <summary>校验 A2/A0 设置常数应答，并返回匹配的表位地址。</summary>
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

    /// <summary>校验 0x38+00 启动应答的脉冲数、次数和脉冲类型。</summary>
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

    /// <summary>校验 0x38+AA 结果应答并返回当前目标表位地址。</summary>
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

    /// <summary>仅保留本步骤收到正确应答的工位，失败工位不阻断同组其它工位。</summary>
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

            SetStationFailureUnlessSucceeded(stationResults, target.StationNo, failureMessage, stationLogger);
        }

        return responded;
    }

    /// <summary>
    /// 从方案配置中筛选当前基本误差点实际会操作的控制 PCB 分组。
    /// 运行时配置可能由方案配置和工位配置合并而来，这里按端点和工位映射去重，避免同一 PCB 组重复发送 A2/A0/0x38。
    /// </summary>
    private static List<MeterTestControlPcbGroup> ResolveControlPcbGroups(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations)
    {
        string configuredGroup = subItem.ControlPcbGroup?.Trim() ?? string.Empty;
        List<MeterTestControlPcbGroup> matchedGroups = planConfig.ControlPcbGroups
            .Where(group => group.Enabled)
            .Where(group => string.IsNullOrWhiteSpace(configuredGroup) ||
                            group.Name.Equals(configuredGroup, StringComparison.OrdinalIgnoreCase))
            .Where(group => !string.IsNullOrWhiteSpace(group.Ip) && group.Port is >= 1 and <= 65535)
            .Where(group => GetTargets(group, selectedStations).Count > 0)
            .ToList();

        List<MeterTestControlPcbGroup> deduplicatedGroups = new();
        foreach (IGrouping<string, MeterTestControlPcbGroup> groupSet in matchedGroups.GroupBy(BuildControlPcbGroupRuntimeKey))
        {
            MeterTestControlPcbGroup first = groupSet.First();
            deduplicatedGroups.Add(first);
            int duplicateCount = groupSet.Count() - 1;
            if (duplicateCount <= 0)
                continue;

            string duplicateNames = string.Join("、", groupSet.Skip(1).Select(group => group.Name));
            LogMessage.Debug(
                $"[基本误差][控制PCB组去重] 已忽略{duplicateCount}个重复分组：保留={first.Name}，"
                + $"忽略={duplicateNames}，Key={groupSet.Key}。");
        }

        return deduplicatedGroups;
    }

    /// <summary>生成控制 PCB 分组运行时唯一键，用于避免同一端点和同一工位映射被重复调度。</summary>
    private static string BuildControlPcbGroupRuntimeKey(MeterTestControlPcbGroup group)
    {
        return string.Join(
            "|",
            group.Ip.Trim(),
            group.Port.ToString(CultureInfo.InvariantCulture),
            group.ProtocolVersion.Trim(),
            group.StationStart.ToString(CultureInfo.InvariantCulture),
            group.StationEnd.ToString(CultureInfo.InvariantCulture),
            group.MeterAddressStart.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>把当前选中工位映射成指定控制 PCB 分组的表位地址和基本误差计划。</summary>
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

    /// <summary>
    /// 调用 XYCtr 标准表接口读取有功脉冲常数，并记录调用参数、返回值、原始文本和解析结论。
    /// </summary>
    private static async Task<(bool Success, ulong Constant, string Message)> ReadStandardActiveConstantAsync(
        CancellationToken cancellationToken)
    {
        if (!XYCtr.IsSourcePortOpen)
        {
            LogMessage.Error("[基本误差接口][XYCtr.CallReadStandConst] 源串口未打开，取消接口调用。", null);
            return (false, 0, "源串口尚未打开，无法读取标准表脉冲常数。");
        }

        using XYCtr xyCtr = new();
        byte[] buffer = new byte[1024];
        LogMessage.Debug(
            $"[基本误差接口][XYCtr.CallReadStandConst] 开始调用："
            + $"缓冲区={buffer.Length}字节，超时={MeterTestSourceControlDefaults.OperationTimeout.TotalMilliseconds:0}ms。"
        );
        bool success;
        int result;
        try
        {
            (success, result) = await xyCtr
                .CallReadStandConstAsync(buffer, MeterTestSourceControlDefaults.OperationTimeout)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogMessage.Error("[基本误差接口][XYCtr.CallReadStandConst] 调用异常。", ex);
            return (false, 0, $"读取标准表脉冲常数异常：{ex.Message}");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!success)
        {
            LogMessage.Error(
                $"[基本误差接口][XYCtr.CallReadStandConst] 调用失败：返回值={result}。",
                null);
            return (false, 0, $"读取标准表脉冲常数失败，XYCtr返回值={result}。");
        }

        string rawValue = Encoding.Default.GetString(buffer).TrimEnd('\0', '\r', '\n', ' ');
        LogMessage.Debug(
            $"[基本误差接口][XYCtr.CallReadStandConst] 调用返回：返回值={result}，原始文本={rawValue}。"
        );
        Match match = Regex.Match(rawValue, @"\d+");
        if (!match.Success ||
            !ulong.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong standardConstant) ||
            standardConstant == 0)
        {
            LogMessage.Error(
                $"[基本误差接口][XYCtr.CallReadStandConst] 返回解析失败：原始文本={rawValue}。",
                null);
            return (false, 0, $"标准表脉冲常数解析失败，原始返回={rawValue}。");
        }

        LogMessage.Debug(
            $"[基本误差接口][XYCtr.CallReadStandConst] 解析成功：标准表有功脉冲常数={standardConstant}。"
        );
        return (true, standardConstant, $"读取标准表脉冲常数成功：{standardConstant}。");
    }

    /// <summary>将资产电能表常数转换为协议使用的无符号整数，并拒绝越界或小数值。</summary>
    private static uint ToMeterConstant(decimal value)
    {
        if (value <= 0 || value > uint.MaxValue || value != decimal.Truncate(value))
            throw new InvalidOperationException($"电能表有功常数必须是1-{uint.MaxValue}之间的整数：{value}。");

        return decimal.ToUInt32(value);
    }

    /// <summary>
    /// 将每次基本误差及平均误差转换为统一结果明细。
    /// 正向、反向有功共享相同结构，方向和点位名称由当前方案小项保留。
    /// </summary>
    private static IReadOnlyList<MeterTestMeasurementData> BuildMeasurements(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        MeterTestBasicErrorExecutionResult executionResult)
    {
        List<MeterTestMeasurementData> measurements = new();
        foreach (StationCommunicationConfig station in selectedStations)
        {
            if (!executionResult.StationResults.TryGetValue(
                    station.StationNo,
                    out MeterTestBasicErrorStationResult? stationResult))
            {
                continue;
            }

            string limitText = BuildLimitText(stationResult);
            for (int index = 0; index < stationResult.ErrorValues.Count; index++)
            {
                decimal errorValue = stationResult.ErrorValues[index];
                measurements.Add(new MeterTestMeasurementData(
                    station.StationNo,
                    context.TestItemName,
                    context.SubItem.Name,
                    "基本误差",
                    index + 1,
                    (double)errorValue,
                    errorValue.ToString("0.######", CultureInfo.InvariantCulture),
                    "%",
                    stationResult.AverageError.HasValue ? (double)stationResult.AverageError.Value : null,
                    limitText));
            }

            if (!stationResult.AverageError.HasValue)
                continue;

            decimal averageError = stationResult.AverageError.Value;
            measurements.Add(new MeterTestMeasurementData(
                station.StationNo,
                context.TestItemName,
                context.SubItem.Name,
                "基本误差平均值",
                0,
                (double)averageError,
                averageError.ToString("0.######", CultureInfo.InvariantCulture),
                "%",
                (double)averageError,
                limitText));
        }

        return measurements;
    }

    /// <summary>生成结果库和导出文件共用的最大允许误差说明。</summary>
    private static string BuildLimitText(MeterTestBasicErrorStationResult stationResult)
    {
        return $"最大允许±{stationResult.MaximumPermittedErrorLimit.ToString("0.######", CultureInfo.InvariantCulture)}%；"
            + $"60%判定区间[-{stationResult.ErrorLimit.ToString("0.######", CultureInfo.InvariantCulture)}%,"
            + $"+{stationResult.ErrorLimit.ToString("0.######", CultureInfo.InvariantCulture)}%]";
    }

    /// <summary>把 64 位标准表常数转换为协议要求的小端字节序。</summary>
    private static byte[] ToLittleEndianBytes(ulong value)
    {
        byte[] bytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>把 32 位电能表常数转换为协议要求的小端字节序。</summary>
    private static byte[] ToLittleEndianBytes(uint value)
    {
        byte[] bytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>为当前分组尚未完成的所有工位写入同一失败原因。</summary>
    private static void SetGroupFailure(
        IEnumerable<BasicErrorTarget> targets,
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        string message,
        Action<int, string>? stationLogger)
    {
        foreach (BasicErrorTarget target in targets)
        {
            SetStationFailureUnlessSucceeded(stationResults, target.StationNo, message, stationLogger);
        }
    }

    /// <summary>判断指定工位是否已经得到合格判定；后续重复分支不应再覆盖为失败。</summary>
    private static bool HasSucceeded(
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        int stationNo)
    {
        return stationResults.TryGetValue(stationNo, out MeterTestBasicErrorStationResult? existing) &&
            existing.Success;
    }

    /// <summary>
    /// 写入失败结果前先检查是否已经合格。
    /// 这层保护用于防止重复调度、晚到超时或重复读取把已经完成的合格结果覆盖成不合格。
    /// </summary>
    private static void SetStationFailureUnlessSucceeded(
        IDictionary<int, MeterTestBasicErrorStationResult> stationResults,
        int stationNo,
        string message,
        Action<int, string>? stationLogger)
    {
        if (HasSucceeded(stationResults, stationNo))
        {
            Trace(
                stationNo,
                $"[重复结果保护] 当前工位已有合格判定，忽略后续失败覆盖：{message}",
                stationLogger);
            return;
        }

        stationResults[stationNo] = MeterTestBasicErrorStationResult.Fail(stationNo, message);
        Trace(stationNo, message, stationLogger);
    }

    /// <summary>创建包含测试点、工位、耗时和失败原因的基本误差执行结果。</summary>
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
            Trace(station.StationNo, "[流程结束]", stationLogger);
            Trace(station.StationNo, "最终结论：不合格", stationLogger);
            Trace(station.StationNo, $"失败原因：{message}", stationLogger);
            Trace(station.StationNo, "-----------------------------------------------------------------", stationLogger);
        }

        return new MeterTestBasicErrorExecutionResult(false, message, results, standValues);
    }

    /// <summary>将基本误差流程明细同步写入工位文件日志和全局 Debug 日志。</summary>
    private static void Trace(int stationNo, string message, Action<int, string>? stationLogger)
    {
        string timestampedMessage = AddTimestampToFlowMessage(message);
        // 全局Debug日志由LogMessage统一加时间；工位原始日志需要在正文中显式携带时间。
        LogMessage.Debug($"[基本误差][工位{stationNo}] {message}");
        stationLogger?.Invoke(stationNo, timestampedMessage);
    }

    /// <summary>
    /// 将源控制的打开串口、初始化、AnyUIOutput下发及20秒标准表验证进度同步到每个选中工位。
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
            Trace(station.StationNo, $"[步骤1/5 升源] {message}", stationLogger);
        }
    }

    /// <summary>
    /// 给基本误差流程的每一行添加毫秒时间。报文收发已经携带完整时间时保持原样，
    /// 分隔线也不添加时间，避免日志出现双重时间或破坏版式。
    /// </summary>
    private static string AddTimestampToFlowMessage(string message)
    {
        string timestamp = FormatTimestamp();
        string normalized = message.Replace("\r\n", "\n", StringComparison.Ordinal);
        return string.Join(
            Environment.NewLine,
            normalized.Split('\n').Select(line =>
                string.IsNullOrWhiteSpace(line) || line == "-----------------------------------------------------------------" || HasTimestamp(line)
                    ? line
                    : $"{timestamp} - {line}"));
    }

    /// <summary>判断日志行是否已经以完整日期时间开头。</summary>
    private static bool HasTimestamp(string line)
    {
        return line.Length >= 26 &&
            line[0] == '[' &&
            char.IsDigit(line[1]) &&
            line.IndexOf("] - ", StringComparison.Ordinal) > 0;
    }

    /// <summary>将控制 PCB 原始帧格式化为空格分隔的十六进制接口日志。</summary>
    private static string ToHex(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }

    /// <summary>生成基本误差收发日志使用的毫秒时间戳。</summary>
    private static string FormatTimestamp()
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";
    }

    /// <summary>将读取序号格式化为日志中更易读的第一次、第二次等文字。</summary>
    private static string FormatReadOrdinal(int sequence)
    {
        return sequence switch
        {
            1 => "第一次",
            2 => "第二次",
            3 => "第三次",
            _ => $"第{sequence}次"
        };
    }

    /// <summary>基本误差流程中工位号与控制 PCB 表位地址的映射。</summary>
    private sealed record BasicErrorTarget(int StationNo, byte MeterAddress);
}

/// <summary>单个基本误差小项的整体执行结果。</summary>
public sealed record MeterTestBasicErrorExecutionResult(
    bool Success,
    string Message,
    IReadOnlyDictionary<int, MeterTestBasicErrorStationResult> StationResults,
    IReadOnlyDictionary<string, string>? StandValues);

/// <summary>
/// 一个正向或反向有功基本误差点的完整服务输出。
/// ExecutionResult 用于界面结论，Measurements 用于结果保存和 Excel 导出。
/// </summary>
internal sealed record MeterTestBasicErrorWorkflowResult(
    MeterTestBasicErrorExecutionResult ExecutionResult,
    IReadOnlyList<MeterTestMeasurementData> Measurements,
    long ElapsedMilliseconds);

/// <summary>单个工位的基本误差最终判定。</summary>
public sealed record MeterTestBasicErrorStationResult(
    int StationNo,
    bool Success,
    IReadOnlyList<decimal> ErrorValues,
    decimal? AverageError,
    decimal MaximumPermittedErrorLimit,
    decimal ErrorLimit,
    string Message)
{
    /// <summary>创建没有有效误差数据的单工位失败结果。</summary>
    public static MeterTestBasicErrorStationResult Fail(int stationNo, string message)
    {
        return new MeterTestBasicErrorStationResult(
            stationNo,
            false,
            Array.Empty<decimal>(),
            null,
            0,
            0,
            message);
    }
}
