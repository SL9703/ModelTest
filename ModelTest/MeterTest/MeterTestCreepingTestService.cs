using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ModelTest.CustomControl;

namespace ModelTest.MeterTest;

/// <summary>
/// 潜动试验流程状态与结果判定服务。
/// 本服务负责0x25启动、0x25读取、有效工位记录、脉冲结果保存和最终≤1个脉冲判定。
/// </summary>
internal sealed class MeterTestCreepingTestService
{
    private const int MaxStationCount = 48;
    private readonly MeterTestControlPcbConnectionManager connectionManager;
    private readonly MeterTestAccessDatabaseService accessDatabaseService;
    private readonly MeterTestCountdownService countdownService;
    private readonly ConcurrentDictionary<int, byte> activeStations = new();
    private readonly ConcurrentDictionary<int, CreepingPulseMeasurement> pulseResults = new();

    /// <summary>
    /// 创建潜动试验服务。
    /// </summary>
    /// <param name="connectionManager">控制 PCB 长连接管理器。</param>
    /// <param name="accessDatabaseService">资产数据库服务，用于读取等级、常数、电压和电流规格。</param>
    /// <param name="countdownService">统一倒计时服务，用于向界面发布剩余时间。</param>
    public MeterTestCreepingTestService(
        MeterTestControlPcbConnectionManager connectionManager,
        MeterTestAccessDatabaseService accessDatabaseService,
        MeterTestCountdownService countdownService)
    {
        this.connectionManager = connectionManager;
        this.accessDatabaseService = accessDatabaseService;
        this.countdownService = countdownService;
    }

    /// <summary>开始新一轮测试前清空潜动状态。</summary>
    public void BeginRun()
    {
        activeStations.Clear();
        pulseResults.Clear();
    }

    /// <summary>清理单个工位的启动状态。</summary>
    public void ClearActiveStation(int stationNo)
    {
        activeStations.TryRemove(stationNo, out _);
    }

    /// <summary>记录收到0x25+01启动应答的工位，只有这些工位进入等待和结果读取。</summary>
    public void MarkActiveStation(int stationNo, byte meterAddress)
    {
        activeStations[stationNo] = meterAddress;
    }

    /// <summary>判断工位是否已成功启动潜动试验。</summary>
    public bool IsActiveStation(int stationNo)
    {
        return activeStations.ContainsKey(stationNo);
    }

    /// <summary>筛选已成功启动潜动试验的工位。</summary>
    public List<StationCommunicationConfig> GetActiveStations(IEnumerable<StationCommunicationConfig> stations)
    {
        return stations
            .Where(station => IsActiveStation(station.StationNo))
            .ToList();
    }

    /// <summary>清理单个工位旧的潜动脉冲结果。</summary>
    public void ClearPulseResult(int stationNo)
    {
        pulseResults.TryRemove(stationNo, out _);
    }

    /// <summary>保存0x25+AA读取到的潜动脉冲数。</summary>
    public void SavePulseResult(int stationNo, uint pulseCount)
    {
        pulseResults[stationNo] = new CreepingPulseMeasurement(pulseCount);
    }

    /// <summary>统计选中工位中已读取到潜动脉冲数的数量。</summary>
    public int CountPulseResults(IEnumerable<StationCommunicationConfig> stations)
    {
        return stations.Count(station => pulseResults.ContainsKey(station.StationNo));
    }

    /// <summary>
    /// 执行潜动试验启动节点。
    /// 各控制PCB组并发，组内按配置间隔逐表位下发0x25+01；单个工位失败不会阻塞其它工位。
    /// </summary>
    public async Task<MeterTestCreepingStepResult> StartAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        IReadOnlyList<MeterTestControlPcbGroup> groups,
        int packetIntervalMs,
        Action<int, string[]> writeStationLog,
        Action<IEnumerable<ControlPcbStationTarget>, bool, string> applyGroupResult,
        Action<int> updateStationRunningState,
        Action<int, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        foreach (StationCommunicationConfig station in selectedStations)
        {
            ClearActiveStation(station.StationNo);
            writeStationLog(
                station.StationNo,
                new[] { "[步骤2/5 开启潜动试验] 开始下发0x25+01启动报文。" });
        }

        if (groups.Count == 0)
        {
            const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
            foreach (StationCommunicationConfig station in selectedStations)
            {
                writeStationLog(
                    station.StationNo,
                    new[] { $"[步骤2/5 开启潜动试验] 结论：不合格，{message}" });
                applyResult(station.StationNo, false, message);
            }

            return MeterTestCreepingStepResult.Fail(message, startTicks);
        }

        bool[] groupResults = await Task.WhenAll(groups.Select(group => StartGroupAsync(
            group,
            selectedStations,
            context,
            packetIntervalMs,
            writeStationLog,
            applyGroupResult,
            updateStationRunningState,
            applyResult,
            cancellationToken))).ConfigureAwait(false);
        bool passed = groupResults.All(result => result);
        string summary = passed
            ? "全部选中工位已收到0x25+01启动应答。"
            : "潜动试验启动完成，但存在未连接或未收到正确应答的工位；成功工位继续后续流程。";
        return new MeterTestCreepingStepResult(passed, summary, Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 执行潜动脉冲读取节点。
    /// 仅对已经收到0x25+01启动应答的工位发送0x25+AA，并保存4字节小端uint脉冲数。
    /// </summary>
    public async Task<MeterTestCreepingStepResult> ReadAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        IReadOnlyList<MeterTestControlPcbGroup> groups,
        int packetIntervalMs,
        Action<int, string[]> writeStationLog,
        Action<IEnumerable<ControlPcbStationTarget>, bool, string> applyGroupResult,
        Action<int> updateStationRunningState,
        Action<int, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        foreach (StationCommunicationConfig station in selectedStations)
        {
            ClearPulseResult(station.StationNo);
            writeStationLog(
                station.StationNo,
                new[] { "[步骤4/5 读取脉冲数量] 开始下发0x25+AA结果读取报文。" });
        }

        if (groups.Count == 0)
        {
            const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
            foreach (StationCommunicationConfig station in selectedStations)
            {
                writeStationLog(
                    station.StationNo,
                    new[] { $"[步骤4/5 读取脉冲数量] 结论：不合格，{message}" });
                applyResult(station.StationNo, false, message);
            }

            return MeterTestCreepingStepResult.Fail(message, startTicks);
        }

        HashSet<int> mappedStationNumbers = groups
            .SelectMany(group => GetControlPcbStationTargets(group, selectedStations))
            .Select(target => target.StationNo)
            .ToHashSet();
        foreach (StationCommunicationConfig station in selectedStations.Where(
                     station => !mappedStationNumbers.Contains(station.StationNo)))
        {
            const string message = "当前工位未映射到可用控制PCB分组，未发送0x25+AA读取报文。";
            writeStationLog(
                station.StationNo,
                new[] { $"[步骤4/5 读取脉冲数量] 结论：不合格，{message}" });
            applyResult(station.StationNo, false, message);
        }

        bool[] groupResults = await Task.WhenAll(groups.Select(group => ReadGroupAsync(
            group,
            selectedStations,
            context,
            packetIntervalMs,
            writeStationLog,
            applyGroupResult,
            updateStationRunningState,
            applyResult,
            cancellationToken))).ConfigureAwait(false);
        bool passed = mappedStationNumbers.Count == selectedStations.Count &&
            groupResults.Length > 0 &&
            groupResults.All(result => result);
        int resultCount = CountPulseResults(selectedStations);
        string summary = $"潜动脉冲读取完成，成功读取={resultCount}/{selectedStations.Count}个工位；失败工位不影响其他工位读取。";
        return new MeterTestCreepingStepResult(passed, summary, Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 执行潜动试验步骤 3：按资产参数计算每个有效工位的潜动时间，并按最大值统一倒计时。
    /// 仅收到 0x25+01 正确应答的工位参与等待；参数无效工位记录失败但不阻断其他工位。
    /// </summary>
    /// <param name="context">当前潜动等待小项上下文。</param>
    /// <param name="selectedStations">本轮选择的全部工位。</param>
    /// <param name="writeStationLog">写入工位文件日志和右侧过程日志的回调。</param>
    /// <param name="updateStationRunningState">将工位状态更新为“测试中”的回调。</param>
    /// <param name="applyResult">写入逐工位步骤结论的回调。</param>
    /// <param name="cancellationToken">停止测试时使用的取消令牌。</param>
    public async Task<MeterTestCreepingStepResult> WaitAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int> updateStationRunningState,
        Action<int, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        List<StationCommunicationConfig> active = GetActiveStations(selectedStations);
        LogMessage.Debug(
            $"[潜动试验][步骤3/5] 准备计算等待时间：小项={context.SubItem.Name}，"
            + $"选中工位={string.Join(',', selectedStations.Select(station => station.StationNo))}，"
            + $"启动成功工位={string.Join(',', active.Select(station => station.StationNo))}。");

        if (active.Count == 0)
        {
            const string message = "没有工位收到0x25+01潜动启动应答，跳过潜动等待。";
            foreach (StationCommunicationConfig station in selectedStations)
            {
                writeStationLog(
                    station.StationNo,
                    new[] { $"[步骤3/5 等待潜动时间] 结论：不合格，{message}" });
                applyResult(station.StationNo, false, message);
            }

            LogMessage.Error($"[潜动试验][步骤3/5] {message}", null);
            return MeterTestCreepingStepResult.Fail(message, startTicks);
        }

        IReadOnlyDictionary<int, MeterArchiveData> archives =
            accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
        Dictionary<int, MeterTestCreepingTimePlan> plans = new();
        foreach (StationCommunicationConfig station in active)
        {
            cancellationToken.ThrowIfCancellationRequested();
            updateStationRunningState(station.StationNo);
            if (!archives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
            {
                string error = $"工位{station.StationNo}缺少资产信息，无法计算潜动时间。";
                writeStationLog(
                    station.StationNo,
                    new[] { $"[步骤3/5 等待潜动时间] 计算失败：{error}" });
                applyResult(station.StationNo, false, error);
                LogMessage.Error($"[潜动试验][步骤3/5][工位{station.StationNo}] {error}", null);
                continue;
            }

            if (!MeterTestCreepingTimeCalculator.TryCalculate(
                    archive,
                    out MeterTestCreepingTimePlan? plan,
                    out string? calculationError) ||
                plan is null)
            {
                string error = calculationError ?? "潜动时间计算失败。";
                writeStationLog(
                    station.StationNo,
                    new[] { $"[步骤3/5 等待潜动时间] 计算失败：{error}" });
                applyResult(station.StationNo, false, error);
                LogMessage.Error($"[潜动试验][步骤3/5][工位{station.StationNo}] {error}", null);
                continue;
            }

            plans[station.StationNo] = plan;
            string calculationMessage = $"[步骤3/5 等待潜动时间] 自动计算：{plan.CalculationNote}。";
            writeStationLog(station.StationNo, new[] { calculationMessage });
            LogMessage.Debug($"[潜动试验][步骤3/5][工位{station.StationNo}] {calculationMessage}");
        }

        if (plans.Count == 0)
        {
            const string message = "所有已启动工位的潜动时间均计算失败，未执行倒计时。";
            LogMessage.Error($"[潜动试验][步骤3/5] {message}", null);
            return MeterTestCreepingStepResult.Fail(message, startTicks);
        }

        int waitSeconds = plans.Values.Max(plan => plan.WaitSeconds);
        List<int> waitingStationNumbers = plans.Keys.OrderBy(stationNo => stationNo).ToList();
        string startMessage =
            $"[步骤3/5 等待潜动时间] 开始自动倒计时：统一等待{waitSeconds}s，"
            + $"参与工位={string.Join(',', waitingStationNumbers)}。";
        LogMessage.Debug($"[潜动试验][步骤3/5] {startMessage}");
        foreach (int stationNo in waitingStationNumbers)
            writeStationLog(stationNo, new[] { startMessage });

        await countdownService.DelayAsync(
            waitSeconds,
            context.SubItem.Name,
            cancellationToken).ConfigureAwait(false);

        string endMessage = $"[步骤3/5 等待潜动时间] 自动倒计时结束：{waitSeconds}s。";
        LogMessage.Debug($"[潜动试验][步骤3/5] {endMessage}");
        foreach (StationCommunicationConfig station in selectedStations)
        {
            plans.TryGetValue(station.StationNo, out MeterTestCreepingTimePlan? stationPlan);
            bool passed = IsActiveStation(station.StationNo) && stationPlan is not null;
            string message = passed
                ? $"已完成潜动等待，工位计算值={stationPlan!.WaitSeconds}s，统一等待={waitSeconds}s。"
                : IsActiveStation(station.StationNo)
                    ? "潜动时间计算失败，未进入等待。"
                    : "潜动启动未成功，未进入等待。";
            writeStationLog(
                station.StationNo,
                new[]
                {
                    passed ? endMessage : $"[步骤3/5 等待潜动时间] 未参与统一倒计时。",
                    $"[步骤3/5 等待潜动时间] 结论：{(passed ? "合格" : "不合格")}，{message}"
                });
            applyResult(station.StationNo, passed, message);
        }

        bool allPassed = plans.Count == selectedStations.Count;
        string summary =
            $"潜动等待结束，参与工位={string.Join(',', waitingStationNumbers)}，统一等待={waitSeconds}s；"
            + $"结论={(allPassed ? "合格" : "不合格")}。";
        LogMessage.Debug($"[潜动试验][步骤3/5] {summary}");
        return new MeterTestCreepingStepResult(
            allPassed,
            summary,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 按累计脉冲数≤1个判定潜动结果。
    /// 0个或1个均为合格；未读取到结果按不合格处理。
    /// </summary>
    public bool JudgeResults(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<MeterTestMeasurementData> recordMeasurement,
        Action<int, bool, string> applyResult)
    {
        bool allPassed = true;
        foreach (StationCommunicationConfig station in selectedStations)
        {
            bool hasResult = pulseResults.TryGetValue(station.StationNo, out CreepingPulseMeasurement? measurement);
            bool passed = hasResult && measurement!.PulseCount <= 1;
            allPassed &= passed;

            string pulseText = hasResult
                ? measurement!.PulseCount.ToString(CultureInfo.InvariantCulture)
                : "未读取";
            string message =
                $"[步骤5/5 判断脉冲结果] 当前脉冲个数：{pulseText}，标准脉冲个数≦1个，结论：{(passed ? "合格" : "不合格")}";

            if (hasResult)
            {
                recordMeasurement(new MeterTestMeasurementData(
                    station.StationNo,
                    context.TestItemName,
                    context.SubItem.Name,
                    "潜动脉冲数",
                    1,
                    measurement!.PulseCount,
                    pulseText,
                    "个",
                    null,
                    "≤1个"));
            }

            writeStationLog(station.StationNo, new[]
            {
                message,
                "[流程结束]",
                $"测试项目：{context.TestItemName}",
                $"最终结论：{(passed ? "合格" : "不合格")}"
            });
            applyResult(station.StationNo, passed, message);
        }

        return allPassed;
    }

    /// <summary>执行一个控制PCB分组的0x25潜动启动命令。</summary>
    private async Task<bool> StartGroupAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        SelectedSubItemContext context,
        int packetIntervalMs,
        Action<int, string[]> writeStationLog,
        Action<IEnumerable<ControlPcbStationTarget>, bool, string> applyGroupResult,
        Action<int> updateStationRunningState,
        Action<int, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
        if (targets.Count == 0)
            return true;

        if (!IsControlPcbV2(group.ProtocolVersion))
        {
            string message = $"控制PCB组 {group.Name} 使用 {group.ProtocolVersion}，0x25潜动试验只支持V2协议。";
            WriteGroupLog(group, targets, writeStationLog, message, MeterTestLogText.Separator);
            applyGroupResult(targets, false, message);
            return false;
        }

        foreach (ControlPcbStationTarget target in targets)
        {
            updateStationRunningState(target.StationNo);
        }

        if (!connectionManager.TryGetConnectedConnection(
                group,
                out MeterTestControlPcbConnection connection,
                out string connectionError))
        {
            WriteGroupLog(group, targets, writeStationLog, connectionError, MeterTestLogText.Separator);
            applyGroupResult(targets, false, connectionError);
            return false;
        }

        WriteGroupLog(group, targets, writeStationLog, $" 复用控制PCB长连接：{connection.DisplayName}", MeterTestLogText.Separator);
        Dictionary<byte, byte[]> responses = await SendControlPcbPacketsAndCollectResponsesAsync(
            context.TestItemName,
            connection,
            group,
            targets,
            target => ElectricEnergyMeterControlV2.BuildCreepingTestStartPacket(target.MeterAddress),
            target => $"[步骤2/5 开启潜动试验] 0x25+01[工位={target.StationNo}, 表位={target.MeterAddress:X2}]",
            ResolveCreepingTestStartResponse,
            TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
            TimeSpan.FromMilliseconds(Math.Max(0, packetIntervalMs)),
            writeStationLog,
            cancellationToken).ConfigureAwait(false);

        bool groupPassed = true;
        foreach (ControlPcbStationTarget target in targets)
        {
            bool stationPassed = responses.ContainsKey(target.MeterAddress);
            groupPassed &= stationPassed;
            if (stationPassed)
            {
                MarkActiveStation(target.StationNo, target.MeterAddress);
            }
            else
            {
                ClearActiveStation(target.StationNo);
            }

            string message = stationPassed
                ? "0x25+01启动应答正常。"
                : "0x25启动未收到正确应答，当前工位不进入后续潜动等待。";
            WriteStationLog(
                group,
                target,
                writeStationLog,
                $"[步骤2/5 开启潜动试验] 结论：{(stationPassed ? "合格" : "不合格")}，{message}",
                MeterTestLogText.Separator);
            applyResult(target.StationNo, stationPassed, message);
        }

        return groupPassed;
    }

    /// <summary>执行一个控制PCB分组的0x25潜动读取命令。</summary>
    private async Task<bool> ReadGroupAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        SelectedSubItemContext context,
        int packetIntervalMs,
        Action<int, string[]> writeStationLog,
        Action<IEnumerable<ControlPcbStationTarget>, bool, string> applyGroupResult,
        Action<int> updateStationRunningState,
        Action<int, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
        if (targets.Count == 0)
            return true;

        if (!IsControlPcbV2(group.ProtocolVersion))
        {
            string message = $"控制PCB组 {group.Name} 使用 {group.ProtocolVersion}，0x25潜动读取只支持V2协议。";
            WriteGroupLog(group, targets, writeStationLog, message, MeterTestLogText.Separator);
            applyGroupResult(targets, false, message);
            return false;
        }

        List<ControlPcbStationTarget> activeTargets = targets
            .Where(target => IsActiveStation(target.StationNo))
            .ToList();
        foreach (ControlPcbStationTarget target in targets.Where(target => !IsActiveStation(target.StationNo)))
        {
            const string message = "潜动启动未成功，未发送0x25+AA结果读取报文。";
            WriteStationLog(
                group,
                target,
                writeStationLog,
                $"[步骤4/5 读取脉冲数量] 结论：不合格，{message}",
                MeterTestLogText.Separator);
            applyResult(target.StationNo, false, message);
        }

        if (activeTargets.Count == 0)
            return false;

        foreach (ControlPcbStationTarget target in activeTargets)
        {
            updateStationRunningState(target.StationNo);
        }

        if (!connectionManager.TryGetConnectedConnection(
                group,
                out MeterTestControlPcbConnection connection,
                out string connectionError))
        {
            WriteGroupLog(group, activeTargets, writeStationLog, connectionError, MeterTestLogText.Separator);
            applyGroupResult(activeTargets, false, connectionError);
            return false;
        }

        WriteGroupLog(group, activeTargets, writeStationLog, $" 复用控制PCB长连接：{connection.DisplayName}", MeterTestLogText.Separator);
        Dictionary<byte, byte[]> responses = await SendControlPcbPacketsAndCollectResponsesAsync(
            context.TestItemName,
            connection,
            group,
            activeTargets,
            target => ElectricEnergyMeterControlV2.BuildCreepingTestResultPacket(target.MeterAddress),
            target => $"[步骤4/5 读取脉冲数量] 0x25+AA[工位={target.StationNo}, 表位={target.MeterAddress:X2}]",
            ResolveCreepingTestResultResponse,
            TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
            TimeSpan.FromMilliseconds(Math.Max(0, packetIntervalMs)),
            writeStationLog,
            cancellationToken).ConfigureAwait(false);

        bool groupPassed = activeTargets.Count == targets.Count;
        foreach (ControlPcbStationTarget target in activeTargets)
        {
            bool hasResponse = responses.TryGetValue(target.MeterAddress, out byte[]? response);
            uint pulseCount = 0;
            bool parsed = hasResponse && ElectricEnergyMeterControlV2.TryParseCreepingTestResultResponse(
                response!,
                target.MeterAddress,
                out pulseCount);
            if (parsed)
            {
                SavePulseResult(target.StationNo, pulseCount);
                string message = $"[步骤4/5 读取脉冲数量] 结论：合格，当前脉冲个数：{pulseCount}。";
                WriteStationLog(group, target, writeStationLog, message, MeterTestLogText.Separator);
                applyResult(target.StationNo, true, message);
            }
            else
            {
                groupPassed = false;
                string message = hasResponse
                    ? "收到0x25结果应答，但AA后的4字节小端脉冲数解析失败。"
                    : "未收到0x25+AA潜动结果应答。";
                WriteStationLog(
                    group,
                    target,
                    writeStationLog,
                    $"[步骤4/5 读取脉冲数量] 结论：不合格，{message}",
                    MeterTestLogText.Separator);
                applyResult(target.StationNo, false, message);
            }
        }

        return groupPassed;
    }

    /// <summary>向控制PCB发送一批表位报文，并按表位地址收集响应。</summary>
    private static async Task<Dictionary<byte, byte[]>> SendControlPcbPacketsAndCollectResponsesAsync(
        string testItemName,
        MeterTestControlPcbConnection connection,
        MeterTestControlPcbGroup group,
        List<ControlPcbStationTarget> targets,
        Func<ControlPcbStationTarget, byte[]> packetFactory,
        Func<ControlPcbStationTarget, string> packetNameFactory,
        Func<byte[], byte?> responseAddressResolver,
        TimeSpan timeout,
        TimeSpan packetInterval,
        Action<int, string[]> writeStationLog,
        CancellationToken cancellationToken)
    {
        Dictionary<byte, TaskCompletionSource<byte[]>> pending = targets.ToDictionary(
            target => target.MeterAddress,
            _ => new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously));

        using IDisposable subscription = connection.Subscribe(frame =>
        {
            byte? meterAddress = responseAddressResolver(frame);
            if (meterAddress.HasValue &&
                pending.TryGetValue(meterAddress.Value, out TaskCompletionSource<byte[]>? completionSource))
            {
                completionSource.TrySetResult(frame);
            }
        });

        byte[][] packets = targets.Select(packetFactory).ToArray();
        await connection.SendSequenceAsync(
            packets,
            packetInterval,
            (index, packet) =>
            {
                ControlPcbStationTarget target = targets[index];
                string packetHex = BitConverter.ToString(packet).Replace("-", " ");
                WriteStationLog(
                    group,
                    target,
                    writeStationLog,
                    $"{FormatStationLogTimestamp()} - 发送报文：{packetHex}，{packetNameFactory(target)}");
            },
            cancellationToken).ConfigureAwait(false);

        Task allResponsesTask = Task.WhenAll(pending.Values.Select(source => source.Task));
        Task completedTask = await Task.WhenAny(allResponsesTask, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);
        if (completedTask != allResponsesTask)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        Dictionary<byte, byte[]> responses = new();
        foreach ((byte meterAddress, TaskCompletionSource<byte[]> completionSource) in pending)
        {
            if (!completionSource.Task.IsCompletedSuccessfully)
                continue;

            responses[meterAddress] = completionSource.Task.Result;
            ControlPcbStationTarget? target = targets.FirstOrDefault(item => item.MeterAddress == meterAddress);
            if (target is not null)
            {
                string responseHex = BitConverter.ToString(completionSource.Task.Result).Replace("-", " ");
                WriteStationLog(group, target, writeStationLog, $"{FormatStationLogTimestamp()} - 接收报文：{responseHex}");
            }
        }

        return responses;
    }

    /// <summary>根据控制PCB分组和当前选中工位，计算实际下发的表位地址。</summary>
    private static List<ControlPcbStationTarget> GetControlPcbStationTargets(
        MeterTestControlPcbGroup group,
        IReadOnlyList<StationCommunicationConfig> selectedStations)
    {
        if (group.StationStart < 1 || group.StationEnd < group.StationStart || group.MeterAddressStart < 1)
            return new List<ControlPcbStationTarget>();

        List<ControlPcbStationTarget> targets = new();
        foreach (StationCommunicationConfig station in selectedStations)
        {
            if (station.StationNo < group.StationStart || station.StationNo > group.StationEnd)
                continue;

            int meterAddress = group.MeterAddressStart + (station.StationNo - group.StationStart);
            if (meterAddress < 1 || meterAddress > 48)
                continue;

            targets.Add(new ControlPcbStationTarget(station.StationNo, (byte)meterAddress));
        }

        return targets;
    }

    /// <summary>校验0x25+01潜动启动应答，并返回应答所属表位地址。</summary>
    private static byte? ResolveCreepingTestStartResponse(byte[] frame)
    {
        if (frame == null || frame.Length < 11)
            return null;

        byte meterAddress = frame[5];
        return ElectricEnergyMeterControlV2.TryParseCreepingTestStartResponse(frame, meterAddress)
            ? meterAddress
            : null;
    }

    /// <summary>校验0x25+AA潜动结果应答，并返回应答所属表位地址。</summary>
    private static byte? ResolveCreepingTestResultResponse(byte[] frame)
    {
        if (frame == null || frame.Length < 11)
            return null;

        byte meterAddress = frame[5];
        return ElectricEnergyMeterControlV2.TryParseCreepingTestResultResponse(frame, meterAddress, out _)
            ? meterAddress
            : null;
    }

    /// <summary>判断控制PCB分组是否为V2协议。</summary>
    private static bool IsControlPcbV2(string protocolVersion)
    {
        return !protocolVersion.Equals(MeterControlPcbProtocolVersion.V1.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>写入同一控制PCB分组下所有目标工位日志。</summary>
    private static void WriteGroupLog(
        MeterTestControlPcbGroup group,
        IEnumerable<ControlPcbStationTarget> targets,
        Action<int, string[]> writeStationLog,
        params string[] lines)
    {
        foreach (ControlPcbStationTarget target in targets)
        {
            WriteStationLog(group, target, writeStationLog, lines);
        }
    }

    /// <summary>写入单个控制PCB目标工位日志。</summary>
    private static void WriteStationLog(
        MeterTestControlPcbGroup group,
        ControlPcbStationTarget target,
        Action<int, string[]> writeStationLog,
        params string[] lines)
    {
        writeStationLog(target.StationNo, lines);
    }

    /// <summary>统一的工位日志时间戳格式。</summary>
    private static string FormatStationLogTimestamp()
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";
    }
}

/// <summary>潜动单个测试步骤的执行结果，用于主窗体刷新过程日志。</summary>
internal sealed record MeterTestCreepingStepResult(
    bool Passed,
    string Message,
    long ElapsedMilliseconds)
{
    /// <summary>创建潜动步骤失败结果，并按步骤起始时间计算已消耗毫秒数。</summary>
    public static MeterTestCreepingStepResult Fail(string message, long startTicks)
    {
        return new MeterTestCreepingStepResult(false, message, Math.Max(0, Environment.TickCount64 - startTicks));
    }
}

/// <summary>MeterTest 日志文本常量，避免服务层依赖窗体私有常量。</summary>
internal static class MeterTestLogText
{
    public const string Separator = "-----------------------------------------------------------------";
}
