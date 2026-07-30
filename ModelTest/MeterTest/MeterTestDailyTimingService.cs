using System.Collections.Concurrent;
using System.Globalization;
using ModelTest.Protocol;

namespace ModelTest.MeterTest;

/// <summary>
/// 日计时三轮自动试验服务。
///
/// 本服务完整负责以下业务流程：
/// 1. 根据方案和选中工位解析控制 PCB 分组及表位地址；
/// 2. 每轮向全部有效表位发送 0x36+00 开始报文；
/// 3. 仅保留收到正确开始应答的工位，并执行一次全局倒计时；
/// 4. 向有效工位发送 0x36+AA 结果读取报文，解析连续的小端 float；
/// 5. 计算每轮平均误差及三轮平均误差，并按绝对值小于 0.5% 判定。
///
/// 窗体只提供日志、状态和测量值回调，不参与组包、收发、等待或结果计算。
/// </summary>
internal sealed class MeterTestDailyTimingService
{
    private const int RoundCount = 3;
    private const double MaximumAbsoluteAverageError = 0.5d;

    private readonly MeterTestControlPcbCommandService controlPcbCommandService;
    private readonly MeterTestCountdownService countdownService;
    private readonly object executionSyncRoot = new();
    private readonly ConcurrentDictionary<DailyTimingStepKey, DailyTimingStepState> stepStates = new();
    private Task<bool>? currentFlowTask;

    /// <summary>
    /// 创建日计时服务。控制 PCB 连接由共享命令服务复用，倒计时状态由共享倒计时服务发布给 UI。
    /// </summary>
    public MeterTestDailyTimingService(
        MeterTestControlPcbCommandService controlPcbCommandService,
        MeterTestCountdownService countdownService)
    {
        this.controlPcbCommandService = controlPcbCommandService;
        this.countdownService = countdownService;
    }

    /// <summary>
    /// 开始新的测试运行，清除上一轮三轮结果和正在执行的任务引用。
    /// 必须在用户每次点击“执行测试”时调用一次。
    /// </summary>
    public void BeginRun()
    {
        lock (executionSyncRoot)
        {
            currentFlowTask = null;
            stepStates.Clear();
        }

        LogMessage.Debug("[日计时] 新测试批次开始，已清理三轮步骤状态和上一轮误差结果。");
    }

    /// <summary>
    /// 执行或复用完整三轮日计时流程，并返回当前方案节点的汇总结论。
    /// 首次调用会执行三轮流程；后续八个节点只读取已经实时保存的阶段状态，不重复发送报文。
    /// </summary>
    public async Task<MeterTestFlowStepResult> ExecuteStepAsync(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        Task<bool> flowTask;
        lock (executionSyncRoot)
        {
            currentFlowTask ??= ExecuteFlowAsync(
                planConfig,
                context,
                selectedStations,
                writeStationLog,
                updateRunningState,
                applyResult,
                recordMeasurement,
                cancellationToken);
            flowTask = currentFlowTask;
        }

        bool flowPassed = await flowTask.ConfigureAwait(false);
        EnsureCurrentStepState(context, selectedStations, flowPassed, writeStationLog, applyResult);
        bool currentStepPassed = selectedStations.Count > 0 && selectedStations.All(station =>
            stepStates.TryGetValue(CreateStepKey(station.StationNo, context.SubItem), out DailyTimingStepState? state) &&
            state.Passed);
        string message = currentStepPassed
            ? "该日计时阶段已完成，完整三轮流程没有重复发送报文。"
            : "该日计时阶段存在失败或缺失结果；其它工位和后续方案继续执行。";
        LogMessage.Debug(
            $"[日计时][节点汇总] 小项={context.SubItem.Name}，流程总结果={flowPassed}，"
            + $"当前节点结果={currentStepPassed}，工位数={selectedStations.Count}。");
        return new MeterTestFlowStepResult(
            currentStepPassed,
            message,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 执行三轮日计时主流程。不同控制 PCB 分组并行开始和读取，但每轮只执行一次统一倒计时。
    /// </summary>
    private async Task<bool> ExecuteFlowAsync(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        if (!TryGetConfiguration(
                context.SubItem,
                out byte testTime,
                out byte testCount,
                out int packetIntervalMs,
                out string configurationError))
        {
            ApplyFailureToAllSteps(
                planConfig,
                context,
                selectedStations.Select(station => station.StationNo),
                configurationError,
                writeStationLog,
                applyResult);
            return false;
        }

        List<MeterTestControlPcbGroup> groups =
            MeterTestControlPcbCommandService.GetEnabledGroups(planConfig, context.SubItem);
        if (groups.Count == 0)
        {
            const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
            ApplyFailureToAllSteps(
                planConfig,
                context,
                selectedStations.Select(station => station.StationNo),
                message,
                writeStationLog,
                applyResult);
            return false;
        }

        Dictionary<MeterTestControlPcbGroup, List<ControlPcbStationTarget>> targetsByGroup = groups
            .ToDictionary(
                group => group,
                group => MeterTestControlPcbCommandService.GetTargets(group, selectedStations));
        HashSet<int> mappedStations = targetsByGroup.Values
            .SelectMany(targets => targets)
            .Select(target => target.StationNo)
            .ToHashSet();
        foreach (StationCommunicationConfig station in selectedStations.Where(
                     station => !mappedStations.Contains(station.StationNo)))
        {
            ApplyFailureToAllSteps(
                planConfig,
                context,
                new[] { station.StationNo },
                "工位未映射到启用的控制PCB分组，未发送日计时报文。",
                writeStationLog,
                applyResult);
        }

        Dictionary<int, List<float>> stationRoundAverages = mappedStations
            .ToDictionary(stationNo => stationNo, _ => new List<float>());
        int waitSeconds = (int)Math.Ceiling(testTime * testCount * 1.1m);
        LogMessage.Debug(
            $"[日计时] 参数确认：时间={testTime}s，次数={testCount}，轮数={RoundCount}，"
            + $"统一等待={waitSeconds}s，报文间隔={packetIntervalMs}ms，控制PCB组数={groups.Count}。");

        for (int round = 1; round <= RoundCount; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SelectedSubItemContext startContext = GetStepContext(planConfig, context, "Start", round);
            SelectedSubItemContext waitContext = GetStepContext(planConfig, context, "Wait", round);
            SelectedSubItemContext readContext = GetStepContext(planConfig, context, "Read", round);
            Dictionary<MeterTestControlPcbGroup, List<ControlPcbStationTarget>> activeByGroup = new();

            Task<(MeterTestControlPcbGroup Group, List<ControlPcbStationTarget> Targets, MeterTestControlPcbBatchResult Batch)>[] startTasks =
                targetsByGroup
                    .Where(entry => entry.Value.Count > 0)
                    .Select(entry => StartRoundForGroupAsync(
                        entry.Key,
                        entry.Value,
                        round,
                        testTime,
                        testCount,
                        packetIntervalMs,
                        context,
                        writeStationLog,
                        cancellationToken))
                    .ToArray();
            foreach (ControlPcbStationTarget target in targetsByGroup.Values.SelectMany(targets => targets))
                updateRunningState(target.StationNo, startContext);

            (MeterTestControlPcbGroup Group, List<ControlPcbStationTarget> Targets, MeterTestControlPcbBatchResult Batch)[] startBatches =
                await Task.WhenAll(startTasks).ConfigureAwait(false);
            foreach ((MeterTestControlPcbGroup group, List<ControlPcbStationTarget> targets, MeterTestControlPcbBatchResult batch) in startBatches)
            {
                List<ControlPcbStationTarget> activeTargets = targets
                    .Where(target => batch.Responses.ContainsKey(target.MeterAddress))
                    .ToList();
                activeByGroup[group] = activeTargets;
                HashSet<int> activeStationNumbers = activeTargets.Select(target => target.StationNo).ToHashSet();
                foreach (ControlPcbStationTarget target in targets)
                {
                    bool started = activeStationNumbers.Contains(target.StationNo);
                    string message = started
                        ? $"第{round}轮开始日计时应答正常。"
                        : $"第{round}轮表位{target.MeterAddress:X2}未收到正确开始应答。";
                    SetStepResult(target.StationNo, startContext, started, message, applyResult);
                    if (started)
                        continue;

                    writeStationLog(target.StationNo, new[] { message });
                    SetStepResult(
                        target.StationNo,
                        waitContext,
                        false,
                        $"第{round}轮开始失败，未进入倒计时。",
                        applyResult);
                    SetStepResult(
                        target.StationNo,
                        readContext,
                        false,
                        $"第{round}轮开始失败，未读取日计时结果。",
                        applyResult);
                }
            }

            List<ControlPcbStationTarget> allActiveTargets = activeByGroup.Values
                .SelectMany(targets => targets)
                .ToList();
            if (allActiveTargets.Count == 0)
            {
                string noActiveMessage = $"第{round}轮没有工位收到开始应答，跳过本轮等待和结果读取。";
                LogMessage.Debug($"[日计时] {noActiveMessage}");
                continue;
            }

            foreach (ControlPcbStationTarget target in allActiveTargets)
            {
                updateRunningState(target.StationNo, waitContext);
                writeStationLog(target.StationNo, new[] { $"第{round}轮开始倒计时：{waitSeconds}s。" });
            }

            LogMessage.Debug(
                $"[日计时][第{round}轮] 开始统一倒计时：{waitSeconds}s，"
                + $"有效工位={string.Join(",", allActiveTargets.Select(target => target.StationNo).OrderBy(value => value))}。");
            await countdownService.DelayAsync(waitSeconds, waitContext.SubItem.Name, cancellationToken).ConfigureAwait(false);
            foreach (ControlPcbStationTarget target in allActiveTargets)
            {
                string waitMessage = $"第{round}轮倒计时结束：{waitSeconds}s。";
                writeStationLog(target.StationNo, new[] { waitMessage });
                SetStepResult(target.StationNo, waitContext, true, waitMessage, applyResult);
                updateRunningState(target.StationNo, readContext);
            }

            Task<(MeterTestControlPcbGroup Group, List<ControlPcbStationTarget> Targets, MeterTestControlPcbBatchResult Batch)>[] readTasks =
                activeByGroup
                    .Where(entry => entry.Value.Count > 0)
                    .Select(entry => ReadRoundForGroupAsync(
                        entry.Key,
                        entry.Value,
                        round,
                        testTime,
                        testCount,
                        packetIntervalMs,
                        context,
                        writeStationLog,
                        cancellationToken))
                    .ToArray();
            (MeterTestControlPcbGroup Group, List<ControlPcbStationTarget> Targets, MeterTestControlPcbBatchResult Batch)[] readBatches =
                await Task.WhenAll(readTasks).ConfigureAwait(false);
            foreach ((MeterTestControlPcbGroup group, List<ControlPcbStationTarget> targets, MeterTestControlPcbBatchResult batch) in readBatches)
            {
                foreach (ControlPcbStationTarget target in targets)
                {
                    IReadOnlyList<float> values = Array.Empty<float>();
                    string parseMessage = string.Empty;
                    bool hasResponse = batch.Responses.TryGetValue(target.MeterAddress, out byte[]? response);
                    bool parsed = hasResponse && TryParseResults(
                        response!,
                        group.ProtocolVersion,
                        testTime,
                        testCount,
                        out values,
                        out parseMessage);
                    if (!parsed)
                    {
                        string failure = hasResponse
                            ? $"第{round}轮日计时结果解析失败：{parseMessage}"
                            : $"第{round}轮未收到日计时结果应答。";
                        writeStationLog(target.StationNo, new[] { failure });
                        SetStepResult(target.StationNo, readContext, false, failure, applyResult);
                        continue;
                    }

                    float roundAverage = values.Average();
                    stationRoundAverages[target.StationNo].Add(roundAverage);
                    SelectedSubItemContext finalContext = GetStepContext(planConfig, context, "Read", RoundCount);
                    recordMeasurement(new MeterTestMeasurementData(
                        target.StationNo,
                        context.TestItemName,
                        finalContext.SubItem.Name,
                        "日计时误差",
                        round,
                        roundAverage,
                        roundAverage.ToString("0.######", CultureInfo.InvariantCulture),
                        "%",
                        null,
                        "绝对值<0.5%"));
                    string valuesText = string.Join(", ", values.Select(
                        value => value.ToString("0.######", CultureInfo.InvariantCulture)));
                    string successMessage =
                        $"第{round}轮结果正常：误差值={valuesText}，本轮平均误差={roundAverage:0.######}%。";
                    writeStationLog(target.StationNo, new[] { successMessage });
                    SetStepResult(target.StationNo, readContext, true, successMessage, applyResult);
                }
            }
        }

        SelectedSubItemContext finalResultContext = GetStepContext(planConfig, context, "Read", RoundCount);
        bool allPassed = mappedStations.Count == selectedStations.Count;
        foreach (StationCommunicationConfig station in selectedStations)
        {
            stationRoundAverages.TryGetValue(station.StationNo, out List<float>? roundAverages);
            bool hasThreeResults = roundAverages?.Count == RoundCount;
            double finalAverage = hasThreeResults ? roundAverages!.Average() : double.NaN;
            bool passed = hasThreeResults && Math.Abs(finalAverage) < MaximumAbsoluteAverageError;
            allPassed &= passed;
            string resultText = hasThreeResults
                ? $"三轮平均误差={finalAverage:0.######}%，允许条件=绝对值<0.5%，结论={(passed ? "合格" : "不合格")}。"
                : $"仅获取到{roundAverages?.Count ?? 0}/{RoundCount}轮有效结果，结论：不合格。";
            if (hasThreeResults)
            {
                recordMeasurement(new MeterTestMeasurementData(
                    station.StationNo,
                    context.TestItemName,
                    finalResultContext.SubItem.Name,
                    "日计时三轮平均误差",
                    0,
                    finalAverage,
                    finalAverage.ToString("0.######", CultureInfo.InvariantCulture),
                    "%",
                    finalAverage,
                    "绝对值<0.5%"));
            }

            writeStationLog(station.StationNo, new[]
            {
                resultText,
                "[流程结束]",
                $"测试项目：{context.TestItemName}",
                $"最终结论：{(passed ? "合格" : "不合格")}",
                MeterTestLogText.Separator
            });
            SetStepResult(station.StationNo, finalResultContext, passed, resultText, applyResult);
        }

        LogMessage.Debug(
            $"[日计时][流程结束] 工位数={selectedStations.Count}，"
            + $"结论={(allPassed ? "全部合格" : "存在不合格或结果缺失")}。");
        return allPassed;
    }

    /// <summary>
    /// 向一个控制 PCB 分组发送当前轮的日计时开始报文。
    /// 接口日志由共享命令服务输出完整端点、工位、表位、下行 HEX、上行 HEX 和超时结论。
    /// </summary>
    private async Task<(MeterTestControlPcbGroup Group, List<ControlPcbStationTarget> Targets, MeterTestControlPcbBatchResult Batch)>
        StartRoundForGroupAsync(
            MeterTestControlPcbGroup group,
            List<ControlPcbStationTarget> targets,
            int round,
            byte testTime,
            byte testCount,
            int packetIntervalMs,
            SelectedSubItemContext context,
            Action<int, string[]> writeStationLog,
            CancellationToken cancellationToken)
    {
        Dictionary<byte, byte[]> expected = targets.ToDictionary(
            target => target.MeterAddress,
            _ => new[] { MeterControlPcbProtocol.StartOperation, testTime, testCount });
        MeterTestControlPcbBatchResult batch = await controlPcbCommandService.SendAndCollectAsync(
            group,
            targets,
            target => BuildDailyTimingPacket(
                group.ProtocolVersion,
                target.MeterAddress,
                MeterControlPcbProtocol.StartOperation,
                testTime,
                testCount),
            target => $"第{round}轮日计时开始，时间={testTime}s，次数={testCount}",
            frame => ResolveDailyTimingResponse(
                frame,
                group.ProtocolVersion,
                MeterControlPcbProtocol.StartOperation,
                testTime,
                testCount,
                expected.Keys),
            TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
            TimeSpan.FromMilliseconds(packetIntervalMs),
            writeStationLog,
            cancellationToken).ConfigureAwait(false);
        return (group, targets, batch);
    }

    /// <summary>
    /// 向一个控制 PCB 分组发送当前轮的日计时结果读取报文。
    /// 只向本轮收到开始应答的工位发送，故单工位故障不会终止其它工位。
    /// </summary>
    private async Task<(MeterTestControlPcbGroup Group, List<ControlPcbStationTarget> Targets, MeterTestControlPcbBatchResult Batch)>
        ReadRoundForGroupAsync(
            MeterTestControlPcbGroup group,
            List<ControlPcbStationTarget> targets,
            int round,
            byte testTime,
            byte testCount,
            int packetIntervalMs,
            SelectedSubItemContext context,
            Action<int, string[]> writeStationLog,
            CancellationToken cancellationToken)
    {
        MeterTestControlPcbBatchResult batch = await controlPcbCommandService.SendAndCollectAsync(
            group,
            targets,
            target => BuildDailyTimingPacket(
                group.ProtocolVersion,
                target.MeterAddress,
                MeterControlPcbProtocol.ReadOperation,
                testTime,
                testCount),
            target => $"第{round}轮读取日计时结果，时间={testTime}s，次数={testCount}",
            frame => ResolveDailyTimingResponse(
                frame,
                group.ProtocolVersion,
                MeterControlPcbProtocol.ReadOperation,
                testTime,
                testCount,
                targets.Select(target => target.MeterAddress)),
            TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
            TimeSpan.FromMilliseconds(packetIntervalMs),
            writeStationLog,
            cancellationToken).ConfigureAwait(false);
        return (group, targets, batch);
    }

    /// <summary>从方案读取日计时时间、次数和报文间隔，并执行协议范围校验。</summary>
    private static bool TryGetConfiguration(
        MeterTestSubItem subItem,
        out byte testTime,
        out byte testCount,
        out int packetIntervalMs,
        out string error)
    {
        testTime = 0;
        testCount = 0;
        packetIntervalMs = Math.Max(0, subItem.PacketIntervalMs);
        error = string.Empty;
        if (subItem.DailyTimingTime is < 1 or > 99)
        {
            error = $"日计时时间必须为1-99秒，当前值={subItem.DailyTimingTime}。";
            return false;
        }

        if (subItem.DailyTimingCount is < 1 or > 10)
        {
            error = $"日计时次数必须为1-10，当前值={subItem.DailyTimingCount}。";
            return false;
        }

        testTime = (byte)subItem.DailyTimingTime;
        testCount = (byte)subItem.DailyTimingCount;
        return true;
    }

    /// <summary>按控制 PCB 协议版本构造0x36日计时下行帧。</summary>
    private static byte[] BuildDailyTimingPacket(
        string protocolVersion,
        byte meterAddress,
        byte operation,
        byte testTime,
        byte testCount)
    {
        return MeterTestControlPcbCommandService.BuildMeterPacket(
            protocolVersion,
            meterAddress,
            MeterControlPcbProtocol.DailyTimingCommand,
            operation,
            testTime,
            testCount);
    }

    /// <summary>
    /// 校验日计时上行帧的协议、命令、表位、操作、时间和次数，并返回匹配表位地址。
    /// 读取应答允许在前三个数据项后附加任意个完整 float 结果。
    /// </summary>
    private static byte? ResolveDailyTimingResponse(
        byte[] frame,
        string protocolVersion,
        byte operation,
        byte testTime,
        byte testCount,
        IEnumerable<byte> expectedAddresses)
    {
        if (!MeterTestControlPcbCommandService.TryGetDataItems(
                frame,
                protocolVersion,
                MeterControlPcbProtocol.DailyTimingCommand,
                out byte meterAddress,
                out byte[] dataItems) ||
            !expectedAddresses.Contains(meterAddress) ||
            dataItems.Length < 3 ||
            dataItems[0] != operation ||
            dataItems[1] != testTime ||
            dataItems[2] != testCount)
        {
            return null;
        }

        return meterAddress;
    }

    /// <summary>解析0x36+AA应答中位于AA、时间、次数之后的连续4字节小端float误差结果。</summary>
    private static bool TryParseResults(
        byte[] frame,
        string protocolVersion,
        byte testTime,
        byte testCount,
        out IReadOnlyList<float> values,
        out string message)
    {
        values = Array.Empty<float>();
        message = string.Empty;
        if (!MeterTestControlPcbCommandService.TryGetDataItems(
                frame,
                protocolVersion,
                MeterControlPcbProtocol.DailyTimingCommand,
                out _,
                out byte[] dataItems))
        {
            message = "报文帧格式、长度、协议类型、命令码或校验和错误。";
            return false;
        }

        if (dataItems.Length < 7 ||
            dataItems[0] != MeterControlPcbProtocol.ReadOperation ||
            dataItems[1] != testTime ||
            dataItems[2] != testCount)
        {
            message = "结果头不匹配，期望AA、测试时间和测试次数。";
            return false;
        }

        int resultDataLength = dataItems.Length - 3;
        if (resultDataLength < 4 || resultDataLength % 4 != 0)
        {
            message = $"结果数据长度{resultDataLength}不是4字节float的整数倍。";
            return false;
        }

        List<float> parsedValues = new(resultDataLength / 4);
        for (int index = 3; index < dataItems.Length; index += 4)
        {
            float value = BitConverter.ToSingle(dataItems, index);
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                message = $"第{parsedValues.Count + 1}个误差结果不是有效float。";
                return false;
            }

            parsedValues.Add(value);
        }

        values = parsedValues;
        message = $"成功解析{parsedValues.Count}个误差结果。";
        return true;
    }

    /// <summary>按轮次和阶段找到真实方案节点，使服务回填状态时与左侧树节点完全对应。</summary>
    private static SelectedSubItemContext GetStepContext(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        string step,
        int round)
    {
        MeterTestSubItem? subItem = planConfig.Schemes
            .FirstOrDefault(scheme => scheme.Name.Equals(context.SchemeName, StringComparison.OrdinalIgnoreCase))?
            .TestItems
            .FirstOrDefault(item => item.Name.Equals(context.TestItemName, StringComparison.OrdinalIgnoreCase))?
            .TestSubItems
            .FirstOrDefault(item =>
                MeterTestWorkflowRouter.Is(item, MeterTestWorkflowKind.ControlPcbDailyTiming) &&
                item.DailyTimingStep.Equals(step, StringComparison.OrdinalIgnoreCase) &&
                item.DailyTimingRound == round);
        return subItem is null
            ? context
            : new SelectedSubItemContext(context.SchemeName, context.TestItemName, subItem);
    }

    /// <summary>保存单个工位单个日计时节点的状态，并通过回调立即刷新UI与数据库。</summary>
    private void SetStepResult(
        int stationNo,
        SelectedSubItemContext context,
        bool passed,
        string message,
        Action<int, SelectedSubItemContext, bool, string> applyResult)
    {
        stepStates[CreateStepKey(stationNo, context.SubItem)] = new DailyTimingStepState(passed, message);
        applyResult(stationNo, context, passed, message);
    }

    /// <summary>给没有在异常路径中产生状态的当前节点补写失败，避免方案树长期停留在黄色或灰色。</summary>
    private void EnsureCurrentStepState(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> stations,
        bool flowPassed,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult)
    {
        foreach (StationCommunicationConfig station in stations)
        {
            DailyTimingStepKey key = CreateStepKey(station.StationNo, context.SubItem);
            if (stepStates.ContainsKey(key))
                continue;

            string fallback = flowPassed
                ? "完整日计时流程结束，但当前节点没有生成工位状态。"
                : "日计时流程在当前节点完成前失败。";
            writeStationLog(station.StationNo, new[] { fallback });
            SetStepResult(station.StationNo, context, false, fallback, applyResult);
        }
    }

    /// <summary>公共配置、连接或映射失败时，为三轮九个节点逐工位回填失败状态。</summary>
    private void ApplyFailureToAllSteps(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IEnumerable<int> stationNumbers,
        string message,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult)
    {
        foreach (int stationNo in stationNumbers.Distinct())
        {
            writeStationLog(stationNo, new[] { $"日计时流程失败：{message}", MeterTestLogText.Separator });
            for (int round = 1; round <= RoundCount; round++)
            {
                foreach (string step in new[] { "Start", "Wait", "Read" })
                {
                    SelectedSubItemContext stepContext = GetStepContext(planConfig, context, step, round);
                    SetStepResult(stationNo, stepContext, false, message, applyResult);
                }
            }
        }

        LogMessage.Error($"[日计时] 流程公共失败：{message}", null);
    }

    /// <summary>构造日计时步骤状态缓存键。</summary>
    private static DailyTimingStepKey CreateStepKey(int stationNo, MeterTestSubItem subItem)
    {
        return new DailyTimingStepKey(
            stationNo,
            subItem.DailyTimingRound,
            subItem.DailyTimingStep.Trim().ToUpperInvariant());
    }

    /// <summary>单工位、单轮、单阶段的缓存键。</summary>
    private sealed record DailyTimingStepKey(int StationNo, int Round, string Step);

    /// <summary>一个日计时阶段的判定及说明。</summary>
    private sealed record DailyTimingStepState(bool Passed, string Message);
}
