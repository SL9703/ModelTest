using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using ModelTest.CustomControl;
using ModelTest.Protocol;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// 常数试验流程状态与结果判定服务。
/// MeterTest 窗体只负责收发报文和刷新 UI，常数试验的数据暂存、误差公式和结论统一放在这里。
/// </summary>
internal sealed class MeterTestConstantTestService
{
    private const int MaxStationCount = 48;
    private const decimal ConstantErrorBaseLimit = 2.0m;
    private const decimal ConstantErrorLimitFactor = 0.1m;

    private readonly MeterTestControlPcbCommandService controlPcbCommandService;
    private readonly MeterTestStationTcpSessionService stationTcpSessionService;
    private readonly MeterTestCommunicationAddressService communicationAddressService;
    private readonly MeterTestCountdownService countdownService;
    private readonly MeterTestAccessDatabaseService databaseService;
    private readonly ConcurrentDictionary<int, decimal> startEnergies = new();
    private readonly ConcurrentDictionary<int, decimal> endEnergies = new();
    private readonly ConcurrentDictionary<int, ConstantWalkingMeasurement> walkingResults = new();

    /// <summary>
    /// 创建常数试验服务。连接、倒计时和数据库均由共享服务注入，确保九个步骤使用同一批次状态。
    /// </summary>
    public MeterTestConstantTestService(
        MeterTestControlPcbCommandService controlPcbCommandService,
        MeterTestStationTcpSessionService stationTcpSessionService,
        MeterTestCommunicationAddressService communicationAddressService,
        MeterTestCountdownService countdownService,
        MeterTestAccessDatabaseService databaseService)
    {
        this.controlPcbCommandService = controlPcbCommandService;
        this.stationTcpSessionService = stationTcpSessionService;
        this.communicationAddressService = communicationAddressService;
        this.countdownService = countdownService;
        this.databaseService = databaseService;
    }

    /// <summary>
    /// 开始读取常数试验起始电量前清理本轮工位缓存。
    /// </summary>
    public void ClearRunStations(IEnumerable<StationCommunicationConfig> stations)
    {
        foreach (StationCommunicationConfig station in stations)
        {
            startEnergies.TryRemove(station.StationNo, out _);
            endEnergies.TryRemove(station.StationNo, out _);
            walkingResults.TryRemove(station.StationNo, out _);
        }
    }

    /// <summary>保存起始电量。</summary>
    public void SaveStartEnergy(int stationNo, decimal energyKwh)
    {
        startEnergies[stationNo] = energyKwh;
    }

    /// <summary>保存结束电量。</summary>
    public void SaveEndEnergy(int stationNo, decimal energyKwh)
    {
        endEnergies[stationNo] = energyKwh;
    }

    /// <summary>清理单个工位旧的走字结果，避免复测时沿用上一轮数据。</summary>
    public void ClearWalkingResult(int stationNo)
    {
        walkingResults.TryRemove(stationNo, out _);
    }

    /// <summary>保存0x37+AA读取到的走字结果。</summary>
    public void SaveWalkingResult(int stationNo, uint pulseCount, decimal standardEnergyKwh)
    {
        walkingResults[stationNo] = new ConstantWalkingMeasurement(pulseCount, standardEnergyKwh);
    }

    /// <summary>
    /// 执行步骤1或步骤6：通过工位 485 TCP 通道读取正向有功总电能。
    /// 开始电量步骤会清理通信测试连接和本轮常数缓存；单个工位失败不会阻断其它工位。
    /// </summary>
    public async Task<MeterTestFlowStepResult> ExecuteEnergyReadAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        bool isStartRead,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        if (isStartRead)
        {
            communicationAddressService.EndRun();
            stationTcpSessionService.BeginRun();
            ClearRunStations(selectedStations);
            LogMessage.Debug("[常数试验][步骤1/9] 已清理地址读取会话、工位TCP旧缓存和上一轮常数试验结果。");
        }

        Task<bool>[] tasks = selectedStations
            .Select(station => ExecuteEnergyReadStationAsync(
                station,
                context,
                isStartRead,
                writeStationLog,
                updateRunningState,
                applyResult,
                recordMeasurement,
                cancellationToken))
            .ToArray();
        bool[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        bool passed = results.Length > 0 && results.All(value => value);
        string message = isStartRead
            ? $"常数试验开始电量读取完成，成功={results.Count(value => value)}/{results.Length}。"
            : $"常数试验结束电量读取完成，成功={results.Count(value => value)}/{results.Length}。";
        LogMessage.Debug($"[常数试验] {message}");
        return new MeterTestFlowStepResult(passed, message, Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 执行步骤2、步骤7或步骤8的 0x37 操作。
    /// operation=00 开始，FF 停止，AA 读取脉冲数和标准表电能量。
    /// </summary>
    private async Task<MeterTestFlowStepResult> ExecuteWalkingOperationCoreAsync(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        byte operation,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        string stepTitle = operation switch
        {
            MeterControlPcbProtocol.StartOperation => "[步骤2/9 开始走字试验]",
            MeterControlPcbProtocol.StopOperation => "[步骤7/9 停止走字试验]",
            MeterControlPcbProtocol.ReadOperation => "[步骤8/9 读取走字试验结果]",
            _ => "[常数试验0x37操作]"
        };
        if (operation == MeterControlPcbProtocol.ReadOperation)
        {
            foreach (StationCommunicationConfig station in selectedStations)
                ClearWalkingResult(station.StationNo);
        }

        List<MeterTestControlPcbGroup> groups =
            MeterTestControlPcbCommandService.GetEnabledGroups(planConfig, context.SubItem);
        if (groups.Count == 0)
        {
            const string noGroupMessage = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
            ApplyFailureToStations(
                context,
                selectedStations,
                stepTitle,
                noGroupMessage,
                writeStationLog,
                applyResult);
            return MeterTestFlowStepResult.Fail(noGroupMessage, startTicks);
        }

        HashSet<int> mappedStations = groups
            .SelectMany(group => MeterTestControlPcbCommandService.GetTargets(group, selectedStations))
            .Select(target => target.StationNo)
            .ToHashSet();
        foreach (StationCommunicationConfig station in selectedStations.Where(
                     station => !mappedStations.Contains(station.StationNo)))
        {
            string message = $"{stepTitle} 工位未映射到启用的控制PCB分组，未发送0x37报文。";
            writeStationLog(station.StationNo, new[] { message, MeterTestLogText.Separator });
            applyResult(station.StationNo, context, false, message);
        }

        bool[] groupResults = await Task.WhenAll(groups.Select(group => ExecuteWalkingGroupAsync(
            group,
            context,
            selectedStations,
            operation,
            stepTitle,
            writeStationLog,
            updateRunningState,
            applyResult,
            recordMeasurement,
            cancellationToken))).ConfigureAwait(false);
        bool passed = mappedStations.Count == selectedStations.Count &&
            groupResults.Length > 0 &&
            groupResults.All(value => value);
        string operationText = operation switch
        {
            MeterControlPcbProtocol.StartOperation => "0x37+00开始",
            MeterControlPcbProtocol.StopOperation => "0x37+FF停止",
            MeterControlPcbProtocol.ReadOperation => "0x37+AA读取",
            _ => $"0x37+{operation:X2}"
        };
        string summary = $"常数试验{operationText}步骤完成，控制PCB分组成功={groupResults.Count(value => value)}/{groupResults.Length}。";
        LogMessage.Debug($"[常数试验] {summary}");
        return new MeterTestFlowStepResult(passed, summary, Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 按业务语义执行走字操作，并在服务内部映射到 0x37 的 00/FF/AA 数据项。
    /// 该入口是调度层唯一需要使用的走字接口。
    /// </summary>
    public Task<MeterTestFlowStepResult> ExecuteWalkingOperationAsync(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        MeterTestWalkingOperation operation,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        byte protocolOperation = operation switch
        {
            MeterTestWalkingOperation.Start => MeterControlPcbProtocol.StartOperation,
            MeterTestWalkingOperation.Stop => MeterControlPcbProtocol.StopOperation,
            MeterTestWalkingOperation.ReadResult => MeterControlPcbProtocol.ReadOperation,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, "不支持的走字操作。")
        };
        return ExecuteWalkingOperationCoreAsync(
            planConfig,
            context,
            selectedStations,
            protocolOperation,
            writeStationLog,
            updateRunningState,
            applyResult,
            recordMeasurement,
            cancellationToken);
    }

    /// <summary>
    /// 执行常数试验“开始走字”语义入口。
    /// 服务内部固定转换为 0x37+00，调用方无需了解协议数据项。
    /// </summary>
    public Task<MeterTestFlowStepResult> StartWalkingAsync(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        return ExecuteWalkingOperationCoreAsync(
            planConfig,
            context,
            selectedStations,
            MeterControlPcbProtocol.StartOperation,
            writeStationLog,
            updateRunningState,
            applyResult,
            recordMeasurement,
            cancellationToken);
    }

    /// <summary>
    /// 执行常数试验“停止走字”语义入口。
    /// 服务内部固定转换为 0x37+FF，调用方无需了解协议数据项。
    /// </summary>
    public Task<MeterTestFlowStepResult> StopWalkingAsync(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        return ExecuteWalkingOperationCoreAsync(
            planConfig,
            context,
            selectedStations,
            MeterControlPcbProtocol.StopOperation,
            writeStationLog,
            updateRunningState,
            applyResult,
            recordMeasurement,
            cancellationToken);
    }

    /// <summary>
    /// 执行常数试验“读取走字结果”语义入口。
    /// 服务内部固定转换为 0x37+AA，并解析待测表脉冲数和标准表电能量。
    /// </summary>
    public Task<MeterTestFlowStepResult> ReadWalkingResultAsync(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        return ExecuteWalkingOperationCoreAsync(
            planConfig,
            context,
            selectedStations,
            MeterControlPcbProtocol.ReadOperation,
            writeStationLog,
            updateRunningState,
            applyResult,
            recordMeasurement,
            cancellationToken);
    }

    /// <summary>
    /// 执行步骤4固定等待。倒计时由共享服务更新 UI，本方法只记录开始/结束并回填逐工位结果。
    /// </summary>
    public async Task<MeterTestFlowStepResult> ExecuteWaitAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        int waitSeconds = Math.Max(0, context.SubItem.TimeoutMs / 1000);
        string startMessage = $"[步骤4/9 等待] 开始常数试验倒计时：{waitSeconds}s。";
        foreach (StationCommunicationConfig station in selectedStations)
            writeStationLog(station.StationNo, new[] { startMessage });
        LogMessage.Debug($"[常数试验] {startMessage}");

        await countdownService.DelayAsync(waitSeconds, context.SubItem.Name, cancellationToken).ConfigureAwait(false);
        string completedMessage = $"[步骤4/9 等待] 倒计时结束：{waitSeconds}s。";
        foreach (StationCommunicationConfig station in selectedStations)
        {
            writeStationLog(station.StationNo, new[] { completedMessage });
            applyResult(station.StationNo, context, true, completedMessage);
        }

        LogMessage.Debug($"[常数试验] {completedMessage}");
        return new MeterTestFlowStepResult(true, completedMessage, Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 对所有选中工位执行常数试验最终结果判定。
    /// 判定公式：ek = (N / k - E) / E × 100%，其中 E = 结束电量 - 开始电量。
    /// 判定区间由资产信息中的有功等级决定：A/B/C/D/E 对应固定误差范围。
    /// </summary>
    public MeterTestFlowStepResult JudgeResults(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<MeterTestMeasurementData> recordMeasurement,
        Action<int, SelectedSubItemContext, bool, string> applyResult)
    {
        long startTicks = Environment.TickCount64;
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
            databaseService.LoadOrCreateMeterArchives(MaxStationCount);

        foreach (StationCommunicationConfig station in selectedStations)
        {
            ConstantTestJudgeResult judgeResult = BuildJudgeResult(
                context,
                station,
                meterArchives,
                out decimal lowerLimit,
                out decimal upperLimit);

            writeStationLog(station.StationNo, new[]
            {
                judgeResult.Message,
                "[流程结束]",
                $"测试项目：{context.TestItemName}",
                $"最终结论：{(judgeResult.Passed ? "合格" : "不合格")}"
            });

            recordMeasurement(new MeterTestMeasurementData(
                station.StationNo,
                context.TestItemName,
                context.SubItem.Name,
                "常数试验实际误差",
                1,
                (double)judgeResult.ActualError,
                judgeResult.ActualError.ToString("0.######", CultureInfo.InvariantCulture),
                "%",
                judgeResult.WalkingPulseCount,
                $"[{lowerLimit:0.######},{upperLimit:0.######}]"));

            applyResult(station.StationNo, context, judgeResult.Passed, judgeResult.Message);
        }

        bool allPassed = selectedStations.All(station =>
        {
            ConstantTestJudgeResult result = BuildJudgeResult(
                context,
                station,
                meterArchives,
                out _,
                out _);
            return result.Passed;
        });
        string summary = allPassed
            ? "常数试验所有工位实际误差均在允许区间内。"
            : "常数试验存在数据缺失或实际误差超限工位。";
        LogMessage.Debug($"[常数试验] {summary}");
        return new MeterTestFlowStepResult(allPassed, summary, Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>读取单个工位的正向有功总电能，并保存开始或结束电量。</summary>
    private async Task<bool> ExecuteEnergyReadStationAsync(
        StationCommunicationConfig station,
        SelectedSubItemContext context,
        bool isStartRead,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        updateRunningState(station.StationNo, context);
        string stepTitle = isStartRead ? "[步骤1/9 读取电表开始电量]" : "[步骤6/9 读取电表结束电量]";
        try
        {
            if (string.IsNullOrWhiteSpace(station.MeterAddress))
            {
                string missingAddress = $"{stepTitle} 工位{station.StationNo}未配置电表地址。";
                writeStationLog(station.StationNo, new[] { missingAddress });
                applyResult(station.StationNo, context, false, missingAddress);
                return false;
            }

            string requestHex = SGCCTools.BuildPositiveActiveEnergyReadRequest(station.MeterAddress, out string piid);
            EnergyReadResponse response = await stationTcpSessionService.SendPositiveActiveEnergyReadAsync(
                station,
                requestHex,
                piid,
                $"{stepTitle} 读取正向有功总电能，表地址={station.MeterAddress}",
                context.SubItem.TimeoutMs,
                line => writeStationLog(station.StationNo, new[] { line }),
                cancellationToken).ConfigureAwait(false);
            Sgcc698EnergyReadParseResult parseResult = response.ParseResult;
            if (!parseResult.IsValid)
            {
                string invalidMessage = $"{stepTitle} 电量响应异常：{parseResult.Message}";
                writeStationLog(station.StationNo, new[] { invalidMessage });
                applyResult(station.StationNo, context, false, invalidMessage);
                return false;
            }

            if (isStartRead)
                SaveStartEnergy(station.StationNo, parseResult.EnergyKwh);
            else
                SaveEndEnergy(station.StationNo, parseResult.EnergyKwh);

            string resultMessage = $"{stepTitle} 电量读取成功：{parseResult.EnergyKwh:0.00}kWh，PIID={piid}。";
            writeStationLog(
                station.StationNo,
                new[] { resultMessage, $"匹配电量响应APDU：{parseResult.Apdu}" });
            recordMeasurement(new MeterTestMeasurementData(
                station.StationNo,
                context.TestItemName,
                context.SubItem.Name,
                isStartRead ? "开始电量" : "结束电量",
                1,
                (double)parseResult.EnergyKwh,
                parseResult.EnergyKwh.ToString("0.00", CultureInfo.InvariantCulture),
                "kWh",
                null,
                "698 OAD=00100200"));
            applyResult(station.StationNo, context, true, resultMessage);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            string exceptionMessage = $"{stepTitle} 执行异常：{ex.Message}，当前工位失败但其它工位继续。";
            writeStationLog(station.StationNo, new[] { exceptionMessage, MeterTestLogText.Separator });
            applyResult(station.StationNo, context, false, exceptionMessage);
            LogMessage.Error($"[常数试验][工位{station.StationNo}] {exceptionMessage}", ex);
            return false;
        }
    }

    /// <summary>执行单个控制 PCB 分组的0x37开始、停止或结果读取。</summary>
    private async Task<bool> ExecuteWalkingGroupAsync(
        MeterTestControlPcbGroup group,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        byte operation,
        string stepTitle,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        List<ControlPcbStationTarget> targets =
            MeterTestControlPcbCommandService.GetTargets(group, selectedStations);
        if (targets.Count == 0)
            return true;

        if (!MeterTestControlPcbCommandService.IsV2(group.ProtocolVersion))
        {
            string protocolMessage = $"{stepTitle} 控制PCB组 {group.Name} 使用{group.ProtocolVersion}，0x37只支持V2。";
            foreach (ControlPcbStationTarget target in targets)
            {
                writeStationLog(target.StationNo, new[] { protocolMessage, MeterTestLogText.Separator });
                applyResult(target.StationNo, context, false, protocolMessage);
            }

            return false;
        }

        foreach (ControlPcbStationTarget target in targets)
            updateRunningState(target.StationNo, context);

        Dictionary<byte, byte[]> expectedPayloads = targets.ToDictionary(
            target => target.MeterAddress,
            _ => new[] { operation });
        Func<byte[], byte?> resolver = operation == MeterControlPcbProtocol.ReadOperation
            ? frame => ResolveWalkingResultResponse(frame, targets)
            : frame => MeterTestControlPcbCommandService.ResolveExpectedResponse(
                frame,
                group.ProtocolVersion,
                MeterControlPcbProtocol.WalkingTestCommand,
                expectedPayloads);
        MeterTestControlPcbBatchResult batch = await controlPcbCommandService.SendAndCollectAsync(
            group,
            targets,
            target => MeterTestControlPcbCommandService.BuildMeterPacket(
                group.ProtocolVersion,
                target.MeterAddress,
                MeterControlPcbProtocol.WalkingTestCommand,
                operation),
            target => $"{stepTitle} 0x37+{operation:X2}，工位={target.StationNo}，表位={target.MeterAddress:X2}",
            resolver,
            TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
            TimeSpan.FromMilliseconds(Math.Max(0, context.SubItem.PacketIntervalMs)),
            writeStationLog,
            cancellationToken).ConfigureAwait(false);

        bool allPassed = batch.ConnectionAvailable;
        foreach (ControlPcbStationTarget target in targets)
        {
            bool passed = batch.Responses.TryGetValue(target.MeterAddress, out byte[]? response);
            string message;
            if (passed && operation == MeterControlPcbProtocol.ReadOperation)
            {
                passed = ElectricEnergyMeterControlV2.TryParseWalkingTestResultResponse(
                    response!,
                    target.MeterAddress,
                    out uint pulseCount,
                    out float standardEnergyKwh,
                    out bool standardEnergyValid,
                    out string parseError);
                if (passed)
                {
                    SaveWalkingResult(target.StationNo, pulseCount, standardEnergyValid ? (decimal)standardEnergyKwh : 0m);
                    message = standardEnergyValid
                        ? $"{stepTitle} 读取成功：待测表脉冲数={pulseCount}，标准表电能量={standardEnergyKwh:0.######}kWh。"
                        : $"{stepTitle} 读取成功：待测表脉冲数={pulseCount}，标准表电能量参考值无效，已忽略参考值并继续按脉冲数计算。";
                    recordMeasurement(new MeterTestMeasurementData(
                        target.StationNo,
                        context.TestItemName,
                        context.SubItem.Name,
                        "走字标准表电能量",
                        1,
                        standardEnergyValid ? (double)standardEnergyKwh : 0d,
                        standardEnergyKwh.ToString("0.######", CultureInfo.InvariantCulture),
                        "kWh",
                        null,
                        "0x37返回"));
                    if (!standardEnergyValid)
                    {
                        LogMessage.Debug($"[常数试验][工位{target.StationNo}] {parseError}");
                    }
                }
                else
                {
                    message = $"{stepTitle} 收到应答，但结果无效：{parseError} 原始报文={MeterTestControlPcbCommandService.ToHex(response!)}。";
                }
            }
            else
            {
                message = passed
                    ? $"{stepTitle} 0x37+{operation:X2}应答正常。"
                    : $"{stepTitle} 未收到0x37+{operation:X2}正确应答。";
            }

            allPassed &= passed;
            writeStationLog(target.StationNo, new[] { message, MeterTestLogText.Separator });
            applyResult(target.StationNo, context, passed, message);
        }

        return allPassed;
    }

    /// <summary>从0x37+AA应答中解析表位地址，供通用批量等待器匹配工位。</summary>
    private static byte? ResolveWalkingResultResponse(
        byte[] frame,
        IReadOnlyList<ControlPcbStationTarget> targets)
    {
        foreach (ControlPcbStationTarget target in targets)
        {
            if (ElectricEnergyMeterControlV2.TryGetWalkingTestResultResponse(
                    frame,
                    target.MeterAddress,
                    out _))
            {
                return target.MeterAddress;
            }
        }

        return null;
    }

    /// <summary>将公共配置错误同步为每个选中工位的失败结果。</summary>
    private static void ApplyFailureToStations(
        SelectedSubItemContext context,
        IEnumerable<StationCommunicationConfig> stations,
        string stepTitle,
        string reason,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult)
    {
        foreach (StationCommunicationConfig station in stations)
        {
            string message = $"{stepTitle} 结论：不合格，{reason}";
            writeStationLog(station.StationNo, new[] { message, MeterTestLogText.Separator });
            applyResult(station.StationNo, context, false, message);
        }
    }

    /// <summary>按单工位构造常数试验判定结果。</summary>
    private ConstantTestJudgeResult BuildJudgeResult(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        out decimal lowerLimit,
        out decimal upperLimit)
    {
        (lowerLimit, upperLimit) = GetConstantErrorLimit(meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive)
            ? archive?.ActiveClass
            : null);
        bool hasStart = startEnergies.TryGetValue(station.StationNo, out decimal startEnergy);
        bool hasEnd = endEnergies.TryGetValue(station.StationNo, out decimal endEnergy);
        bool hasWalking = walkingResults.TryGetValue(station.StationNo, out ConstantWalkingMeasurement? walking);
        bool hasArchive = meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? resolvedArchive);
        ulong activeConstant = 0;
        bool hasActiveConstant = hasArchive &&
            resolvedArchive is not null &&
            TryParseUnsignedConstant(resolvedArchive.ActiveConstant, out activeConstant) &&
            activeConstant > 0;
        decimal energyValue = hasStart && hasEnd ? endEnergy - startEnergy : 0m;
        decimal pulseEnergy = hasWalking && hasActiveConstant && walking is not null
            ? walking.PulseCount / (decimal)activeConstant
            : 0m;
        decimal actualError = hasStart && hasEnd && hasWalking && hasActiveConstant && energyValue != 0m
            ? (pulseEnergy - energyValue) / energyValue * 100m
            : 0m;
        bool hasValidDelta = energyValue != 0m;
        bool passed = hasStart &&
            hasEnd &&
            hasWalking &&
            hasActiveConstant &&
            hasValidDelta &&
            actualError >= lowerLimit &&
            actualError <= upperLimit;

        string message = hasStart && hasEnd && hasWalking && hasActiveConstant && walking is not null
            ? hasValidDelta
                ? $"[步骤9/9 对比试验结果] 开始电量={startEnergy:0.00}kWh，结束电量={endEnergy:0.00}kWh，差值电量E=结束电量-开始电量={energyValue:0.######}kWh，待测表脉冲数N={walking.PulseCount}，脉冲常数k={activeConstant}imp/kWh，N/k={pulseEnergy:0.######}kWh，实际误差ek=(N/k-E)/E×100%={actualError:0.######}%，允许区间=[{lowerLimit:0.######}, {upperLimit:0.######}]，结论：{(passed ? "合格" : "不合格")}。"
                : $"[步骤9/9 对比试验结果] 开始电量={startEnergy:0.00}kWh，结束电量={endEnergy:0.00}kWh，差值电量E=0，无法按 ek = (N/k-E)/E×100% 计算，结论：不合格。"
            : $"[步骤9/9 对比试验结果] 缺少数据：开始电量={(hasStart ? "已读取" : "未读取")}，结束电量={(hasEnd ? "已读取" : "未读取")}，走字结果={(hasWalking ? "已读取" : "未读取")}，有功常数={(hasActiveConstant ? "已读取" : "未读取或无效")}，结论：不合格。";

        return new ConstantTestJudgeResult(
            passed,
            message,
            actualError,
            hasWalking && walking is not null ? walking.PulseCount : null);
    }

    /// <summary>
    /// 根据有功等级返回常数试验允许误差区间。
    /// 规则固定为：A[-0.2,0.2]、B[-0.1,0.1]、C[-0.05,0.05]、D[-0.02,0.02]、E[-0.01,0.01]。
    /// </summary>
    private static (decimal LowerLimit, decimal UpperLimit) GetConstantErrorLimit(string? activeClass)
    {
        string normalized = (activeClass ?? string.Empty).Trim().ToUpperInvariant();
        return normalized switch
        {
            "A" => (-0.2m, 0.2m),
            "B" => (-0.1m, 0.1m),
            "C" => (-0.05m, 0.05m),
            "D" => (-0.02m, 0.02m),
            "E" => (-0.01m, 0.01m),
            _ => (-0.2m, 0.2m)
        };
    }

    /// <summary>从纯数字或带说明文本中提取第一个无符号整数常数。</summary>
    private static bool TryParseUnsignedConstant(string? value, out ulong constant)
    {
        constant = 0;
        string normalized = value?.Trim() ?? string.Empty;
        if (ulong.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out constant))
            return true;

        Match match = Regex.Match(normalized, @"\d+");
        return match.Success &&
               ulong.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out constant);
    }

    /// <summary>单工位常数试验判定结果。</summary>
    private sealed record ConstantTestJudgeResult(
        bool Passed,
        string Message,
        decimal ActualError,
        double? WalkingPulseCount);
}

/// <summary>
/// 常数试验走字操作的业务语义。
/// 协议层会把 Start、Stop、ReadResult 分别映射为 0x37 的 00、FF、AA 数据项。
/// </summary>
internal enum MeterTestWalkingOperation
{
    /// <summary>开始累计待测表和标准表脉冲。</summary>
    Start,

    /// <summary>停止当前走字累计。</summary>
    Stop,

    /// <summary>读取待测表脉冲数和标准表电能量。</summary>
    ReadResult
}
