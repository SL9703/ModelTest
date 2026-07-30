using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ModelTest.CustomControl;
using ModelTest.Protocol;

namespace ModelTest.MeterTest;

/// <summary>
/// 起动试验五步流程编排服务。
/// 窗体只提供具体执行回调和UI刷新回调；五步顺序、临时小项克隆和失败短路逻辑统一收敛在这里。
/// </summary>
internal sealed class MeterTestStartingErrorService
{
    private const int MaxStationCount = 48;
    private readonly MeterTestControlPcbConnectionManager connectionManager;
    private readonly MeterTestAccessDatabaseService accessDatabaseService;
    private readonly MeterTestCountdownService countdownService;
    private readonly ConcurrentDictionary<int, float> errorResults = new();

    /// <summary>
    /// 创建起动试验服务。
    /// </summary>
    /// <param name="connectionManager">控制 PCB 长连接管理器，负责复用已建立的板卡连接。</param>
    /// <param name="accessDatabaseService">资产数据库服务，用于读取等级、电压、电流规格和电能表常数。</param>
    /// <param name="countdownService">统一倒计时服务，用于向测试过程区域发布剩余时间。</param>
    public MeterTestStartingErrorService(
        MeterTestControlPcbConnectionManager connectionManager,
        MeterTestAccessDatabaseService accessDatabaseService,
        MeterTestCountdownService countdownService)
    {
        this.connectionManager = connectionManager;
        this.accessDatabaseService = accessDatabaseService;
        this.countdownService = countdownService;
    }

    /// <summary>
    /// 执行一个起动误差测试点。
    /// 方案树只展示“正有/反有-H-1.0-1U-Ist”，内部仍按升源、启动、等待、读取、判定五步执行。
    /// </summary>
    public async Task ExecutePointAsync(
        SelectedSubItemContext context,
        List<StationCommunicationConfig> selectedStations,
        Func<SelectedSubItemContext, CancellationToken, Task<bool>> executeSourceAsync,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, string[]> writeStationLog,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<string, string, bool, string, long> addProcessLog,
        Func<MeterTestSubItem, IReadOnlyList<MeterTestControlPcbGroup>> getControlPcbGroups,
        Action refreshStationDisplay,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        foreach (StationCommunicationConfig station in selectedStations)
        {
            errorResults.TryRemove(station.StationNo, out _);
            updateRunningState(station.StationNo, context);
            writeStationLog(
                station.StationNo,
                new[]
                {
                    MeterTestLogText.Separator,
                    $"[流程开始] 起动误差测试点：{context.SubItem.Name}"
                });
        }

        SelectedSubItemContext sourceContext = context with
        {
            SubItem = CloneStartingPointSubItem(
                context.SubItem,
                MeterTestExecutionMode.StartingSource,
                "按起动误差点位计算 Ist 并升源。",
                20000)
        };
        bool sourceSucceeded = await executeSourceAsync(sourceContext, cancellationToken).ConfigureAwait(false);
        if (!sourceSucceeded)
        {
            addProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                false,
                "起动误差点升源失败，未继续执行0x38启动、等待、读取和判定。",
                Math.Max(0, Environment.TickCount64 - startTicks));
            return;
        }

        SelectedSubItemContext startContext = context with
        {
            SubItem = CloneStartingPointSubItem(
                context.SubItem,
                MeterTestExecutionMode.ControlPcbStartingError,
                "A2/A0/0x38启动起动误差试验。",
                5000)
        };
        await StartControlPcbAsync(
            startContext,
            selectedStations,
            getControlPcbGroups(startContext.SubItem),
            writeStationLog,
            writeControlPcbLog,
            updateRunningState,
            applyResult: (stationNo, passed, message) => applyResult(stationNo, startContext, passed, message),
            addProcessLog,
            refreshStationDisplay,
            cancellationToken).ConfigureAwait(false);

        SelectedSubItemContext waitContext = context with
        {
            SubItem = CloneStartingPointSubItem(
                context.SubItem,
                MeterTestExecutionMode.StartingTimeWait,
                "按起动时间公式统一等待。",
                0)
        };
        await ExecuteWaitAsync(
            waitContext,
            selectedStations,
            writeStationLog,
            updateRunningState,
            applyResult: (stationNo, passed, message) => applyResult(stationNo, waitContext, passed, message),
            addProcessLog,
            refreshStationDisplay,
            cancellationToken).ConfigureAwait(false);

        SelectedSubItemContext readContext = context with
        {
            SubItem = CloneStartingPointSubItem(
                context.SubItem,
                MeterTestExecutionMode.ControlPcbStartingErrorRead,
                "0x38+AA读取起动误差结果。",
                5000)
        };
        await ReadControlPcbAsync(
            readContext,
            selectedStations,
            getControlPcbGroups(readContext.SubItem),
            writeStationLog,
            writeControlPcbLog,
            updateRunningState,
            applyResult: (stationNo, passed, message) => applyResult(stationNo, readContext, passed, message),
            addProcessLog,
            refreshStationDisplay,
            cancellationToken).ConfigureAwait(false);

        SelectedSubItemContext judgeContext = context with
        {
            SubItem = CloneStartingPointSubItem(
                context.SubItem,
                MeterTestExecutionMode.StartingErrorJudge,
                "按JJG596起动误差限值判定。",
                0)
        };
        JudgeResults(
            judgeContext,
            selectedStations,
            writeStationLog,
            applyResult: (stationNo, passed, message) => applyResult(stationNo, judgeContext, passed, message),
            addProcessLog,
            refreshStationDisplay,
            recordMeasurement);
    }

    /// <summary>
    /// 执行起动试验步骤2：读取标准表脉冲常数，然后依次下发A2、A0和0x38+00。
    /// </summary>
    public async Task StartControlPcbAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        IReadOnlyList<MeterTestControlPcbGroup> groups,
        Action<int, string[]> writeStationLog,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, bool, string> applyResult,
        Action<string, string, bool, string, long> addProcessLog,
        Action refreshStationDisplay,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        foreach (StationCommunicationConfig station in selectedStations)
        {
            writeStationLog(station.StationNo, new[] { "[步骤2/5 开启起动试验] 开始读取标准表脉冲常数。" });
        }

        if (!TryGetStartingErrorConfig(
                context.SubItem,
                out byte pulseCount,
                out byte testCount,
                out byte pulseType,
                out int packetIntervalMs,
                out string? configError))
        {
            string message = configError ?? "起动误差试验配置错误。";
            ApplyFailureToStations(context, selectedStations, writeStationLog, applyResult, "[步骤2/5 开启起动试验]", message);
            addProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
            return;
        }

        (bool constantRead, ulong standardConstant, string constantMessage) =
            await ReadStandardActiveConstantAsync(cancellationToken).ConfigureAwait(false);
        LogMessage.Debug($"[起动试验] {constantMessage}");
        foreach (StationCommunicationConfig station in selectedStations)
        {
            writeStationLog(station.StationNo, new[] { $"[步骤2/5 开启起动试验] {constantMessage}" });
        }

        if (!constantRead)
        {
            foreach (StationCommunicationConfig station in selectedStations)
            {
                applyResult(station.StationNo, false, constantMessage);
            }

            refreshStationDisplay();
            addProcessLog(
                context.SchemeName,
                context.SubItem.Name,
                false,
                constantMessage,
                Math.Max(0, Environment.TickCount64 - startTicks));
            return;
        }

        IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
            accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
        if (groups.Count == 0)
        {
            const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
            ApplyFailureToStations(context, selectedStations, writeStationLog, applyResult, "[步骤2/5 开启起动试验]", message);
            addProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
            return;
        }

        bool[] groupResults = await Task.WhenAll(groups.Select(group => StartControlPcbGroupAsync(
            group,
            selectedStations,
            meterArchives,
            context,
            standardConstant,
            pulseCount,
            testCount,
            pulseType,
            packetIntervalMs,
            writeControlPcbLog,
            updateRunningState,
            applyResult,
            cancellationToken))).ConfigureAwait(false);
        bool passed = groupResults.Length > 0 && groupResults.All(result => result);
        refreshStationDisplay();
        addProcessLog(
            $"{context.SchemeName}/{context.TestItemName}",
            context.SubItem.Name,
            passed,
            passed
                ? $"A2、A0和0x38启动命令全部完成，标准表常数={standardConstant}。"
                : $"起动误差启动流程存在失败工位，标准表常数={standardConstant}，请查看工位日志。",
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>执行起动试验步骤4：发送0x38+AA并解析float误差结果。</summary>
    public async Task ReadControlPcbAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        IReadOnlyList<MeterTestControlPcbGroup> groups,
        Action<int, string[]> writeStationLog,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, bool, string> applyResult,
        Action<string, string, bool, string, long> addProcessLog,
        Action refreshStationDisplay,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        foreach (StationCommunicationConfig station in selectedStations)
        {
            errorResults.TryRemove(station.StationNo, out _);
            writeStationLog(station.StationNo, new[] { "[步骤4/5 读取误差结果] 开始下发0x38+AA读取报文。" });
        }

        if (!TryGetStartingErrorReadConfig(
                context.SubItem,
                out byte pulseCount,
                out byte testCount,
                out int packetIntervalMs,
                out string? configError))
        {
            string message = configError ?? "读取起动误差配置错误。";
            ApplyFailureToStations(context, selectedStations, writeStationLog, applyResult, "[步骤4/5 读取误差结果]", message);
            addProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
            return;
        }

        if (groups.Count == 0)
        {
            const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
            ApplyFailureToStations(context, selectedStations, writeStationLog, applyResult, "[步骤4/5 读取误差结果]", message);
            addProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
            return;
        }

        bool[] groupResults = await Task.WhenAll(groups.Select(group => ReadControlPcbGroupAsync(
            group,
            selectedStations,
            context,
            pulseCount,
            testCount,
            packetIntervalMs,
            writeControlPcbLog,
            updateRunningState,
            applyResult,
            cancellationToken))).ConfigureAwait(false);
        bool passed = groupResults.Length > 0 && groupResults.All(result => result);
        refreshStationDisplay();
        addProcessLog(
            $"{context.SchemeName}/{context.TestItemName}",
            context.SubItem.Name,
            passed,
            passed ? "所有选中工位均已读取并解析起动误差结果。" : "存在未读取到有效起动误差结果的工位。",
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 执行起动试验步骤 3：按每个工位的资产参数计算 Tst 上限，并按最大等待时间统一倒计时。
    /// </summary>
    /// <param name="context">当前起动试验小项上下文。</param>
    /// <param name="selectedStations">本轮参与测试的工位。</param>
    /// <param name="writeStationLog">写入工位文件日志和右侧过程日志的回调。</param>
    /// <param name="updateRunningState">将工位状态更新为“测试中”的回调。</param>
    /// <param name="applyResult">写入单工位步骤结论的回调。</param>
    /// <param name="addProcessLog">写入方案级过程结论的回调。</param>
    /// <param name="refreshStationDisplay">刷新当前方案表格的回调。</param>
    /// <param name="cancellationToken">停止测试时使用的取消令牌。</param>
    public async Task ExecuteWaitAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, bool, string> applyResult,
        Action<string, string, bool, string, long> addProcessLog,
        Action refreshStationDisplay,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        int pulseCount = Math.Max(1, context.SubItem.BasicErrorPulseCount);
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
            accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
        List<MeterTestStartingTimeResult> calculations = new();
        List<int> invalidStations = new();

        LogMessage.Debug(
            $"[起动试验][步骤3/5] 开始计算等待时间：小项={context.SubItem.Name}，"
            + $"工位={string.Join(',', selectedStations.Select(station => station.StationNo))}，"
            + $"倍率={context.SubItem.StartingTimeMultiplier}，脉冲数={pulseCount}。");

        foreach (StationCommunicationConfig station in selectedStations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            updateRunningState(station.StationNo, context);

            if (!meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
            {
                const string missingArchive = "缺少资产信息，无法计算起动等待时间。";
                invalidStations.Add(station.StationNo);
                writeStationLog(
                    station.StationNo,
                    new[] { $"[步骤3/5 等待起动时间] 结论：不合格，{missingArchive}" });
                applyResult(station.StationNo, false, missingArchive);
                LogMessage.Error($"[起动试验][步骤3/5][工位{station.StationNo}] {missingArchive}", null);
                continue;
            }

            if (!MeterTestStartingTestCalculator.TryCalculateStartingTime(
                    archive,
                    context.SubItem.StartingTimeMultiplier,
                    pulseCount,
                    out MeterTestStartingTimeResult? calculation,
                    out string? calculationError) ||
                calculation is null)
            {
                string error = $"起动时间计算失败：{calculationError ?? "未知参数错误"}";
                invalidStations.Add(station.StationNo);
                writeStationLog(
                    station.StationNo,
                    new[] { $"[步骤3/5 等待起动时间] 结论：不合格，{error}" });
                applyResult(station.StationNo, false, error);
                LogMessage.Error($"[起动试验][步骤3/5][工位{station.StationNo}] {error}", null);
                continue;
            }

            calculations.Add(calculation);
            string calculationMessage = FormatStartingTimeCalculation(calculation);
            writeStationLog(
                station.StationNo,
                new[] { $"[步骤3/5 等待起动时间] {calculationMessage}" });
            LogMessage.Debug(
                $"[起动试验][步骤3/5][工位{station.StationNo}] {calculationMessage}");
        }

        if (calculations.Count == 0)
        {
            const string noValidCalculation = "所有选中工位的起动时间参数均无效，未执行倒计时。";
            refreshStationDisplay();
            addProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                false,
                noValidCalculation,
                Math.Max(0, Environment.TickCount64 - startTicks));
            LogMessage.Error($"[起动试验][步骤3/5] {noValidCalculation}", null);
            return;
        }

        int waitSeconds = calculations.Max(calculation => calculation.WaitSeconds);
        string limitingStations = string.Join(",", calculations
            .Where(calculation => calculation.WaitSeconds == waitSeconds)
            .Select(calculation => calculation.StationNo));
        string startMessage =
            $"[步骤3/5 等待起动时间] 开始倒计时：{waitSeconds}s，"
            + $"按最大 ceil(Tst上限)×{context.SubItem.StartingTimeMultiplier}×脉冲数{pulseCount}，"
            + $"限制工位={limitingStations}。";

        LogMessage.Debug($"[起动试验][步骤3/5] {startMessage}");
        foreach (MeterTestStartingTimeResult calculation in calculations)
            writeStationLog(calculation.StationNo, new[] { startMessage });

        await countdownService.DelayAsync(
            waitSeconds,
            context.SubItem.Name,
            cancellationToken).ConfigureAwait(false);

        string completedMessage = $"[步骤3/5 等待起动时间] 倒计时结束：{waitSeconds}s。";
        LogMessage.Debug($"[起动试验][步骤3/5] {completedMessage}");
        foreach (MeterTestStartingTimeResult calculation in calculations)
        {
            string resultMessage =
                $"已统一等待{waitSeconds}s；本工位Tst上限={calculation.UpperSeconds:0.####}s，"
                + $"计算等待={calculation.WaitSeconds}s。";
            writeStationLog(
                calculation.StationNo,
                new[]
                {
                    completedMessage,
                    $"[步骤3/5 等待起动时间] 结论：合格，{resultMessage}"
                });
            applyResult(calculation.StationNo, true, resultMessage);
        }

        bool passed = invalidStations.Count == 0 && calculations.Count == selectedStations.Count;
        string processMessage = passed
            ? $"起动时间计算完成并统一等待{waitSeconds}s，限制工位={limitingStations}。"
            : $"有效工位已等待{waitSeconds}s；参数无效工位={string.Join(',', invalidStations)}。";
        refreshStationDisplay();
        addProcessLog(
            $"{context.SchemeName}/{context.TestItemName}",
            context.SubItem.Name,
            passed,
            processMessage,
            Math.Max(0, Environment.TickCount64 - startTicks));
        LogMessage.Debug(
            $"[起动试验][步骤3/5] 完成：结论={(passed ? "合格" : "不合格")}，{processMessage}");
    }

    /// <summary>
    /// 格式化单个工位的起动时间计算参数，确保日志能够追溯等级、常数、电压、Ist 和公式倍率。
    /// </summary>
    private static string FormatStartingTimeCalculation(MeterTestStartingTimeResult result)
    {
        return $"起动时间参数：等级={result.ActiveClass}，Est={result.EstPercent:0.###}%={result.EstRatio:0.#####}，"
            + $"C={result.MeterConstant:0.###}imp/kWh，U={result.Voltage:0.###}V，"
            + $"Ist={result.StartingCurrent:0.#########}A，d={result.UnitFactor:0}，"
            + $"Pst=U×Ist×d={result.StartingPower:0.######}W，Ki=1，Ku=1，"
            + $"Tst下限={result.LowerSeconds:0.####}s，Tst上限={result.UpperSeconds:0.####}s，"
            + $"等待=ceil(Tst上限)×{result.WaitMultiplier}×脉冲数{result.PulseCount}={result.WaitSeconds}s；"
            + $"{result.CalculationNote}。";
    }

    /// <summary>执行起动试验步骤5：按JJG596规程限值的60%判断起动误差结果。</summary>
    public void JudgeResults(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, bool, string> applyResult,
        Action<string, string, bool, string, long> addProcessLog,
        Action refreshStationDisplay,
        Action<MeterTestMeasurementData> recordMeasurement)
    {
        long startTicks = Environment.TickCount64;
        IReadOnlyDictionary<int, MeterArchiveData> archives =
            accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
        bool allPassed = true;

        foreach (StationCommunicationConfig station in selectedStations)
        {
            bool hasResult = errorResults.TryGetValue(station.StationNo, out float errorValue);
            MeterTestErrorComparisonResult? comparison = null;
            string parameterError = string.Empty;
            if (!archives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
            {
                parameterError = $"工位{station.StationNo}没有资产档案。";
            }
            else if (!MeterTestBasicErrorCalculator.TryParseCurrentSpecification(
                         archive.CurrentSpecification,
                         archive.AccessMode,
                         archive.ActiveClass,
                         out MeterTestBasicErrorCurrentSpecification? specification,
                         out string? specificationError))
            {
                parameterError = $"电流规格解析失败：{specificationError}";
            }
            else if (!MeterTestStartingTestCalculator.TryCalculateStartingCurrent(
                         archive,
                         out decimal startingCurrent,
                         out _,
                         out string? startingCurrentError))
            {
                parameterError = $"Ist计算失败：{startingCurrentError}";
            }
            else if (hasResult)
            {
                MeterTestErrorLimitRequest request = new(
                    MeterTestErrorEnergyType.Active,
                    archive.ActiveClass,
                    archive.AccessMode,
                    "1.0",
                    startingCurrent,
                    startingCurrent,
                    specification!.Imin,
                    specification.Itr,
                    specification.Imax,
                    specification.BasicCurrent);
                comparison = MeterTestErrorResultComparer.Compare(request, (decimal)errorValue);
                if (!comparison.IsValid || !comparison.IsApplicable)
                    parameterError = comparison.Message;
            }

            bool protocolSentinel = hasResult &&
                (Math.Abs(errorValue - 1.0f) < 0.000001f || Math.Abs(errorValue - 2.0f) < 0.000001f);
            bool passed = hasResult && !protocolSentinel && comparison?.Passed == true;
            allPassed &= passed;
            string errorText = hasResult
                ? errorValue.ToString("0.######", CultureInfo.InvariantCulture)
                : "未读取";
            string message;
            if (!hasResult)
            {
                message = "[步骤5/5 判断误差结果] 未读取到起动误差，结论：不合格。";
            }
            else if (protocolSentinel)
            {
                string sentinelReason = Math.Abs(errorValue - 1.0f) < 0.000001f
                    ? "协议返回1.0，表示待测表未输出一个完整脉冲"
                    : "协议返回2.0，表示试验结果尚未计算完成";
                message = $"[步骤5/5 判断误差结果] 误差值：{errorText}%，{sentinelReason}，结论：不合格。";
            }
            else if (comparison is null || !string.IsNullOrWhiteSpace(parameterError))
            {
                message = $"[步骤5/5 判断误差结果] 误差值：{errorText}%，无法计算规程限值：{parameterError}，结论：不合格。";
            }
            else
            {
                message =
                    $"[步骤5/5 判断误差结果] 误差值：{errorText}%，"
                    + $"最大允许误差区间：[-{comparison.Limit.MaximumPermittedLimit:0.######}%, +{comparison.Limit.MaximumPermittedLimit:0.######}%]，"
                    + $"60%判定区间：[-{comparison.Limit.ComparisonLimit:0.######}%, +{comparison.Limit.ComparisonLimit:0.######}%]，"
                    + $"结论：{(passed ? "合格" : "不合格")}。"
                    + Environment.NewLine
                    + $"[判定说明] {comparison.Message}";
            }

            if (hasResult)
            {
                string limitText = comparison?.Limit.IsApplicable == true
                    ? $"最大允许±{comparison.Limit.MaximumPermittedLimit:0.######}%；"
                        + $"60%判定区间[-{comparison.Limit.ComparisonLimit:0.######}%,"
                        + $"+{comparison.Limit.ComparisonLimit:0.######}%]"
                    : string.IsNullOrWhiteSpace(parameterError) ? "规程限值不可用" : parameterError;
                recordMeasurement(new MeterTestMeasurementData(
                    station.StationNo,
                    context.TestItemName,
                    context.SubItem.Name,
                    "起动误差",
                    1,
                    errorValue,
                    errorText,
                    "%",
                    errorValue,
                    limitText));
            }

            writeStationLog(
                station.StationNo,
                new[]
                {
                    message,
                    "[流程结束]",
                    $"测试项目：{context.TestItemName}",
                    $"最终结论：{(passed ? "合格" : "不合格")}",
                    MeterTestLogText.Separator
                });
            applyResult(station.StationNo, passed, message);
        }

        refreshStationDisplay();
        addProcessLog(
            $"{context.SchemeName}/{context.TestItemName}",
            context.SubItem.Name,
            allPassed,
            allPassed
                ? "所有工位起动误差均位于各自最大允许误差60%的判定区间内。"
                : "存在起动误差超出60%判定区间、协议特殊值、参数无效或未读取结果的工位。",
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 执行单个控制PCB组的A2、A0和0x38启动流程。
    /// A2或A0未正确应答的工位会从后续步骤移除，其他工位继续执行。
    /// </summary>
    private async Task<bool> StartControlPcbGroupAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        SelectedSubItemContext context,
        ulong standardConstant,
        byte pulseCount,
        byte testCount,
        byte pulseType,
        int packetIntervalMs,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
        if (targets.Count == 0)
            return true;

        Dictionary<int, string> failureReasons = new();
        if (!IsControlPcbV2(group.ProtocolVersion))
        {
            string message = $"控制PCB组 {group.Name} 使用 {group.ProtocolVersion}，起动误差A2/A0/0x38流程只支持V2协议。";
            WriteControlPcbGroupLog(group, targets, writeControlPcbLog, message, MeterTestLogText.Separator);
            foreach (ControlPcbStationTarget target in targets)
                applyResult(target.StationNo, false, message);
            return false;
        }

        Dictionary<byte, uint> meterConstants = new();
        foreach (ControlPcbStationTarget target in targets)
        {
            if (!meterArchives.TryGetValue(target.StationNo, out MeterArchiveData? archive) ||
                !TryParseUnsignedConstant(archive.ActiveConstant, out ulong activeConstant) ||
                activeConstant is 0 or > uint.MaxValue)
            {
                failureReasons[target.StationNo] = $"资产信息有功常数无效：{archive?.ActiveConstant ?? "空"}";
                continue;
            }

            meterConstants[target.MeterAddress] = (uint)activeConstant;
            updateRunningState(target.StationNo, context);
        }

        List<ControlPcbStationTarget> activeTargets = targets
            .Where(target => meterConstants.ContainsKey(target.MeterAddress))
            .ToList();
        if (activeTargets.Count == 0)
        {
            ApplyStartingErrorGroupResults(group, targets, context, failureReasons, Array.Empty<byte>(), writeControlPcbLog, applyResult);
            return false;
        }

        if (!connectionManager.TryGetConnectedConnection(
                group,
                out MeterTestControlPcbConnection connection,
                out string connectionError))
        {
            foreach (ControlPcbStationTarget target in activeTargets)
                failureReasons[target.StationNo] = connectionError;

            WriteControlPcbGroupLog(group, targets, writeControlPcbLog, connectionError, MeterTestLogText.Separator);
            ApplyStartingErrorGroupResults(group, targets, context, failureReasons, Array.Empty<byte>(), writeControlPcbLog, applyResult);
            return false;
        }

        WriteControlPcbGroupLog(group, targets, writeControlPcbLog, $" 复用控制PCB长连接：{connection.DisplayName}", MeterTestLogText.Separator);
        TimeSpan responseTimeout = TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs));
        TimeSpan packetInterval = TimeSpan.FromMilliseconds(Math.Max(0, packetIntervalMs));

        byte[] standardPayload = ToLittleEndianBytes(standardConstant);
        Dictionary<byte, byte[]> a2ExpectedPayloads = activeTargets.ToDictionary(
            target => target.MeterAddress,
            _ => standardPayload);
        Dictionary<byte, byte[]> a2Responses = await SendControlPcbPacketsAndCollectResponsesAsync(
            connection,
            group,
            activeTargets,
            target => BuildV2MeterPacket(target.MeterAddress, MeterControlPcbProtocol.StandardActiveConstantCommand, standardPayload),
            target => $"[步骤2/5 开启起动试验] A2设置标准表有功常数[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 常数={standardConstant}]",
            frame => ResolveExpectedControlPcbResponse(frame, group.ProtocolVersion, MeterControlPcbProtocol.StandardActiveConstantCommand, a2ExpectedPayloads),
            responseTimeout,
            packetInterval,
            writeControlPcbLog,
            cancellationToken).ConfigureAwait(false);
        activeTargets = KeepRespondedStartingErrorTargets(activeTargets, a2Responses, failureReasons, "A2设置标准表常数未收到正确应答");

        if (activeTargets.Count > 0)
        {
            Dictionary<byte, byte[]> a0ExpectedPayloads = activeTargets.ToDictionary(
                target => target.MeterAddress,
                target => ToLittleEndianBytes(meterConstants[target.MeterAddress]));
            Dictionary<byte, byte[]> a0Responses = await SendControlPcbPacketsAndCollectResponsesAsync(
                connection,
                group,
                activeTargets,
                target => BuildV2MeterPacket(target.MeterAddress, MeterControlPcbProtocol.ActiveConstantCommand, a0ExpectedPayloads[target.MeterAddress]),
                target => $"[步骤2/5 开启起动试验] A0设置电能表有功常数[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 常数={meterConstants[target.MeterAddress]}]",
                frame => ResolveExpectedControlPcbResponse(frame, group.ProtocolVersion, MeterControlPcbProtocol.ActiveConstantCommand, a0ExpectedPayloads),
                responseTimeout,
                packetInterval,
                writeControlPcbLog,
                cancellationToken).ConfigureAwait(false);
            activeTargets = KeepRespondedStartingErrorTargets(activeTargets, a0Responses, failureReasons, "A0设置电能表常数未收到正确应答");
        }

        if (activeTargets.Count > 0)
        {
            byte[] startPayload = { MeterControlPcbProtocol.StartOperation, pulseCount, testCount, pulseType };
            Dictionary<byte, byte[]> startExpectedPayloads = activeTargets.ToDictionary(
                target => target.MeterAddress,
                _ => startPayload);
            Dictionary<byte, byte[]> startResponses = await SendControlPcbPacketsAndCollectResponsesAsync(
                connection,
                group,
                activeTargets,
                target => BuildV2MeterPacket(target.MeterAddress, MeterControlPcbProtocol.BasicError38Command, startPayload),
                target => $"[步骤2/5 开启起动试验] 0x38+00[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 脉冲数={pulseCount}, 次数={testCount}, 类型={(pulseType == MeterControlPcbProtocol.ActivePulseType ? "有功" : "无功")}]",
                frame => ResolveExpectedControlPcbResponse(frame, group.ProtocolVersion, MeterControlPcbProtocol.BasicError38Command, startExpectedPayloads),
                responseTimeout,
                packetInterval,
                writeControlPcbLog,
                cancellationToken).ConfigureAwait(false);
            activeTargets = KeepRespondedStartingErrorTargets(activeTargets, startResponses, failureReasons, "0x38开启起动试验未收到正确应答");
        }

        HashSet<byte> successfulAddresses = activeTargets.Select(target => target.MeterAddress).ToHashSet();
        ApplyStartingErrorGroupResults(group, targets, context, failureReasons, successfulAddresses, writeControlPcbLog, applyResult);
        bool groupPassed = targets.All(target => successfulAddresses.Contains(target.MeterAddress));
        WriteControlPcbGroupLog(
            group,
            targets,
            writeControlPcbLog,
            groupPassed ? "A2、A0和0x38启动命令全部应答正常" : "起动误差启动流程存在失败工位",
            MeterTestLogText.Separator);
        return groupPassed;
    }

    /// <summary>执行单个控制PCB组的0x38起动误差读取。</summary>
    private async Task<bool> ReadControlPcbGroupAsync(
        MeterTestControlPcbGroup group,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        SelectedSubItemContext context,
        byte pulseCount,
        byte testCount,
        int packetIntervalMs,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
        if (targets.Count == 0)
            return true;

        if (!IsControlPcbV2(group.ProtocolVersion))
        {
            string message = $"控制PCB组 {group.Name} 使用 {group.ProtocolVersion}，0x38误差读取只支持V2协议。";
            WriteControlPcbGroupLog(group, targets, writeControlPcbLog, message, MeterTestLogText.Separator);
            foreach (ControlPcbStationTarget target in targets)
                applyResult(target.StationNo, false, message);
            return false;
        }

        foreach (ControlPcbStationTarget target in targets)
            updateRunningState(target.StationNo, context);

        if (!connectionManager.TryGetConnectedConnection(
                group,
                out MeterTestControlPcbConnection connection,
                out string connectionError))
        {
            WriteControlPcbGroupLog(group, targets, writeControlPcbLog, connectionError, MeterTestLogText.Separator);
            foreach (ControlPcbStationTarget target in targets)
                applyResult(target.StationNo, false, connectionError);
            return false;
        }

        WriteControlPcbGroupLog(group, targets, writeControlPcbLog, $" 复用控制PCB长连接：{connection.DisplayName}", MeterTestLogText.Separator);
        byte[] resultPayload = { MeterControlPcbProtocol.ReadOperation, pulseCount, testCount };
        Dictionary<byte, byte[]> responses = await SendControlPcbPacketsAndCollectResponsesAsync(
            connection,
            group,
            targets,
            target => BuildV2MeterPacket(target.MeterAddress, MeterControlPcbProtocol.BasicError38Command, resultPayload),
            target => $"[步骤4/5 读取误差结果] 0x38+AA[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 脉冲数={pulseCount}, 次数={testCount}]",
            frame => ResolveStartingErrorResultResponse(frame, group.ProtocolVersion, pulseCount, testCount),
            TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
            TimeSpan.FromMilliseconds(Math.Max(0, packetIntervalMs)),
            writeControlPcbLog,
            cancellationToken).ConfigureAwait(false);

        bool groupPassed = true;
        foreach (ControlPcbStationTarget target in targets)
        {
            bool hasResponse = responses.TryGetValue(target.MeterAddress, out byte[]? response);
            float errorValue = 0;
            string parseMessage = "未收到0x38误差结果应答。";
            bool parsed = hasResponse && TryParseStartingErrorResult(
                response!,
                group.ProtocolVersion,
                pulseCount,
                testCount,
                out _,
                out errorValue,
                out parseMessage);
            if (parsed)
            {
                errorResults[target.StationNo] = errorValue;
                string message = $"[步骤4/5 读取误差结果] 结论：合格，误差值：{errorValue.ToString("0.######", CultureInfo.InvariantCulture)}；{parseMessage}";
                WriteControlPcbStationLog(group, target, writeControlPcbLog, message, MeterTestLogText.Separator);
                applyResult(target.StationNo, true, message);
            }
            else
            {
                groupPassed = false;
                string message = hasResponse
                    ? $"误差结果解析失败：{parseMessage}"
                    : "未收到0x38误差结果应答。";
                WriteControlPcbStationLog(
                    group,
                    target,
                    writeControlPcbLog,
                    $"[步骤4/5 读取误差结果] 结论：不合格，{message}",
                    MeterTestLogText.Separator);
                applyResult(target.StationNo, false, message);
            }
        }

        return groupPassed;
    }

    /// <summary>向控制PCB发送一批表位报文，并按表位地址收集响应。</summary>
    private static async Task<Dictionary<byte, byte[]>> SendControlPcbPacketsAndCollectResponsesAsync(
        MeterTestControlPcbConnection connection,
        MeterTestControlPcbGroup group,
        List<ControlPcbStationTarget> targets,
        Func<ControlPcbStationTarget, byte[]> packetFactory,
        Func<ControlPcbStationTarget, string> packetNameFactory,
        Func<byte[], byte?> responseAddressResolver,
        TimeSpan timeout,
        TimeSpan packetInterval,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
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
                WriteControlPcbStationLog(
                    group,
                    target,
                    writeControlPcbLog,
                    $"{FormatStationLogTimestamp()} - 发送报文：{packetHex}，{packetNameFactory(target)}");
            },
            cancellationToken).ConfigureAwait(false);

        Task allResponsesTask = Task.WhenAll(pending.Values.Select(source => source.Task));
        Task completedTask = await Task.WhenAny(allResponsesTask, Task.Delay(timeout, cancellationToken)).ConfigureAwait(false);
        if (completedTask != allResponsesTask)
            cancellationToken.ThrowIfCancellationRequested();

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
                WriteControlPcbStationLog(group, target, writeControlPcbLog, $"{FormatStationLogTimestamp()} - 接收报文：{responseHex}");
            }
        }

        return responses;
    }

    /// <summary>保留正确应答的工位，并为未应答工位记录失败原因。</summary>
    private static List<ControlPcbStationTarget> KeepRespondedStartingErrorTargets(
        IEnumerable<ControlPcbStationTarget> targets,
        IReadOnlyDictionary<byte, byte[]> responses,
        IDictionary<int, string> failureReasons,
        string failureReason)
    {
        List<ControlPcbStationTarget> respondedTargets = new();
        foreach (ControlPcbStationTarget target in targets)
        {
            if (responses.ContainsKey(target.MeterAddress))
                respondedTargets.Add(target);
            else
                failureReasons[target.StationNo] = failureReason;
        }

        return respondedTargets;
    }

    /// <summary>把起动误差启动流程的逐工位结论写入界面、缓存和数据库。</summary>
    private static void ApplyStartingErrorGroupResults(
        MeterTestControlPcbGroup group,
        IEnumerable<ControlPcbStationTarget> targets,
        SelectedSubItemContext context,
        IReadOnlyDictionary<int, string> failureReasons,
        IEnumerable<byte> successfulAddresses,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
        Action<int, bool, string> applyResult)
    {
        HashSet<byte> successSet = successfulAddresses.ToHashSet();
        foreach (ControlPcbStationTarget target in targets)
        {
            bool passed = successSet.Contains(target.MeterAddress);
            string message = passed
                ? "A2标准表常数、A0电能表常数和0x38起动误差启动命令应答正常。"
                : failureReasons.TryGetValue(target.StationNo, out string? reason)
                    ? reason
                    : "起动误差启动流程未完成。";
            WriteControlPcbStationLog(
                group,
                target,
                writeControlPcbLog,
                $"[步骤2/5 开启起动试验] 结论：{(passed ? "合格" : "不合格")}，{message}");
            applyResult(target.StationNo, passed, message);
        }
    }

    /// <summary>给配置错误或不可执行步骤批量写入失败结论。</summary>
    private static void ApplyFailureToStations(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        Action<int, bool, string> applyResult,
        string stepName,
        string message)
    {
        foreach (StationCommunicationConfig station in selectedStations)
        {
            writeStationLog(station.StationNo, new[] { $"{stepName} 结论：不合格，{message}" });
            applyResult(station.StationNo, false, message);
        }
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
            if (meterAddress is < 1 or > 48)
                continue;

            targets.Add(new ControlPcbStationTarget(station.StationNo, (byte)meterAddress));
        }

        return targets;
    }

    /// <summary>校验控制PCB应答命令和数据项，并返回应答所属表位地址。</summary>
    private static byte? ResolveExpectedControlPcbResponse(
        byte[] frame,
        string protocolVersion,
        byte command,
        IReadOnlyDictionary<byte, byte[]> expectedPayloads)
    {
        if (!TryGetControlPcbPacketDataItems(frame, protocolVersion, command, out byte meterAddress, out byte[] dataItems) ||
            !expectedPayloads.TryGetValue(meterAddress, out byte[]? expectedPayload) ||
            !dataItems.SequenceEqual(expectedPayload))
        {
            return null;
        }

        return meterAddress;
    }

    /// <summary>校验0x38结果应答并返回表位地址。</summary>
    private static byte? ResolveStartingErrorResultResponse(
        byte[] frame,
        string protocolVersion,
        byte pulseCount,
        byte testCount)
    {
        return TryParseStartingErrorResult(
            frame,
            protocolVersion,
            pulseCount,
            testCount,
            out byte meterAddress,
            out _,
            out _)
            ? meterAddress
            : null;
    }

    /// <summary>解析0x38+AA应答中的小端float结果，多个结果时返回平均值。</summary>
    private static bool TryParseStartingErrorResult(
        byte[] frame,
        string protocolVersion,
        byte pulseCount,
        byte testCount,
        out byte meterAddress,
        out float errorValue,
        out string message)
    {
        meterAddress = 0;
        errorValue = 0;
        message = string.Empty;
        if (!TryGetControlPcbPacketDataItems(
                frame,
                protocolVersion,
                MeterControlPcbProtocol.BasicError38Command,
                out meterAddress,
                out byte[] dataItems))
        {
            message = "报文帧格式、方向、协议类型、命令码或校验和错误。";
            return false;
        }

        if (dataItems.Length < 3 ||
            dataItems[0] != MeterControlPcbProtocol.ReadOperation ||
            dataItems[1] != pulseCount ||
            dataItems[2] != testCount)
        {
            message = $"结果头不匹配，期望AA {pulseCount:X2} {testCount:X2}。";
            return false;
        }

        int resultDataLength = dataItems.Length - 3;
        if (resultDataLength < 4 || resultDataLength % 4 != 0)
        {
            message = $"误差数据长度{resultDataLength}不是有效float长度。";
            return false;
        }

        int resultCount = resultDataLength / 4;
        if (resultCount > testCount)
        {
            message = $"返回结果数量{resultCount}超过配置试验次数{testCount}。";
            return false;
        }

        List<float> results = new(resultCount);
        for (int index = 3; index < dataItems.Length; index += 4)
        {
            int bits = BinaryPrimitives.ReadInt32LittleEndian(dataItems.AsSpan(index, 4));
            float value = BitConverter.Int32BitsToSingle(bits);
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                message = $"第{results.Count + 1}个误差结果不是有效float。";
                return false;
            }

            results.Add(value);
        }

        errorValue = results.Average();
        message = resultCount == 1
            ? "成功解析1个误差结果。"
            : $"成功解析{resultCount}个误差结果，使用平均值。";
        return true;
    }

    /// <summary>读取并校验起动误差试验的XML参数。</summary>
    private static bool TryGetStartingErrorConfig(
        MeterTestSubItem subItem,
        out byte pulseCount,
        out byte testCount,
        out byte pulseType,
        out int packetIntervalMs,
        out string? errorMessage)
    {
        pulseCount = 0;
        testCount = 0;
        pulseType = 0;
        packetIntervalMs = Math.Max(0, subItem.PacketIntervalMs);
        errorMessage = null;

        if (subItem.BasicErrorPulseCount is < 1 or > 99)
        {
            errorMessage = "0x38脉冲数必须在1-99之间。";
            return false;
        }

        if (subItem.BasicErrorTestCount is < 1 or > 10)
        {
            errorMessage = "0x38试验次数必须在1-10之间。";
            return false;
        }

        if (subItem.BasicErrorPulseType is < 0 or > 1)
        {
            errorMessage = "0x38脉冲类型只支持0（有功）或1（无功）。";
            return false;
        }

        pulseCount = (byte)subItem.BasicErrorPulseCount;
        testCount = (byte)subItem.BasicErrorTestCount;
        pulseType = (byte)subItem.BasicErrorPulseType;
        return true;
    }

    /// <summary>读取并校验0x38误差结果查询参数。</summary>
    private static bool TryGetStartingErrorReadConfig(
        MeterTestSubItem subItem,
        out byte pulseCount,
        out byte testCount,
        out int packetIntervalMs,
        out string? errorMessage)
    {
        pulseCount = 0;
        testCount = 0;
        packetIntervalMs = Math.Max(0, subItem.PacketIntervalMs);
        errorMessage = null;
        if (subItem.BasicErrorPulseCount is < 1 or > 99)
        {
            errorMessage = "0x38结果查询脉冲数必须在1-99之间。";
            return false;
        }

        if (subItem.BasicErrorTestCount is < 1 or > 10)
        {
            errorMessage = "0x38结果查询试验次数必须在1-10之间。";
            return false;
        }

        pulseCount = (byte)subItem.BasicErrorPulseCount;
        testCount = (byte)subItem.BasicErrorTestCount;
        return true;
    }

    /// <summary>通过XYCtr读取标准表有功脉冲常数并解析为无符号整数。</summary>
    private static async Task<(bool Success, ulong Constant, string Message)> ReadStandardActiveConstantAsync(
        CancellationToken cancellationToken)
    {
        if (!XYCtr.IsSourcePortOpen)
        {
            LogMessage.Error("[起动试验接口][XYCtr.CallReadStandConst] 源串口未打开，取消接口调用。", null);
            return (false, 0, "源串口尚未打开，无法读取标准表脉冲常数；请先执行升源（启动电流）。");
        }

        using XYCtr xyCtr = new();
        byte[] constantBuffer = new byte[1024];
        cancellationToken.ThrowIfCancellationRequested();
        LogMessage.Debug(
            $"[起动试验接口][XYCtr.CallReadStandConst] 调用开始："
            + $"缓冲区={constantBuffer.Length}字节，"
            + $"超时={MeterTestSourceControlDefaults.OperationTimeout.TotalMilliseconds:0}ms。"
        );
        bool success;
        int result;
        try
        {
            (success, result) = await xyCtr
                .CallReadStandConstAsync(constantBuffer, MeterTestSourceControlDefaults.OperationTimeout)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogMessage.Error("[起动试验接口][XYCtr.CallReadStandConst] 调用异常。", ex);
            return (false, 0, $"读取标准表脉冲常数异常：{ex.Message}");
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!success)
        {
            LogMessage.Error(
                $"[起动试验接口][XYCtr.CallReadStandConst] 调用失败：返回值={result}。",
                null);
            return (false, 0, $"读取标准表脉冲常数失败，XYCtr返回值={result}。");
        }

        string rawValue = Encoding.Default.GetString(constantBuffer).TrimEnd('\0', '\r', '\n', ' ');
        LogMessage.Debug(
            $"[起动试验接口][XYCtr.CallReadStandConst] 调用返回：返回值={result}，原始文本={rawValue}。"
        );
        if (!TryParseUnsignedConstant(rawValue, out ulong standardConstant) || standardConstant == 0)
        {
            LogMessage.Error(
                $"[起动试验接口][XYCtr.CallReadStandConst] 返回解析失败：原始文本={rawValue}。",
                null);
            return (false, 0, $"标准表脉冲常数解析失败，原始返回={rawValue}。");
        }

        LogMessage.Debug(
            $"[起动试验接口][XYCtr.CallReadStandConst] 解析成功：标准表有功脉冲常数={standardConstant}。"
        );
        return (true, standardConstant, $"读取标准表脉冲常数成功：{standardConstant}，原始返回={rawValue}。");
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

    /// <summary>把ulong按控制PCB协议要求编码成8字节小端数据。</summary>
    private static byte[] ToLittleEndianBytes(ulong value)
    {
        byte[] bytes = new byte[sizeof(ulong)];
        BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>把uint按控制PCB协议要求编码成4字节小端数据。</summary>
    private static byte[] ToLittleEndianBytes(uint value)
    {
        byte[] bytes = new byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>构造V2控制PCB电表控制报文。</summary>
    private static byte[] BuildV2MeterPacket(byte meterAddress, byte command, params byte[] dataItems)
    {
        return MeterControlPcbProtocol.BuildV2ControlFrame(meterAddress, command, dataItems);
    }

    /// <summary>从控制PCB报文中拆出命令相关的数据项。</summary>
    private static bool TryGetControlPcbPacketDataItems(
        byte[] rawData,
        string protocolVersion,
        byte command,
        out byte meterAddress,
        out byte[] dataItems)
    {
        meterAddress = 0x00;
        dataItems = Array.Empty<byte>();
        return IsControlPcbV2(protocolVersion) &&
               TryGetV2MeterPacketDataItems(rawData, command, out meterAddress, out dataItems);
    }

    /// <summary>从V2电表报文中提取表位地址和数据项。</summary>
    private static bool TryGetV2MeterPacketDataItems(byte[] rawData, byte command, out byte meterAddress, out byte[] dataItems)
    {
        meterAddress = 0x00;
        dataItems = Array.Empty<byte>();
        if (rawData.Length < 11 ||
            rawData[0] != MeterControlPcbProtocol.V2StartByte1 ||
            rawData[1] != MeterControlPcbProtocol.V2StartByte2 ||
            rawData[^2] != MeterControlPcbProtocol.V2EndByte1 ||
            rawData[^1] != MeterControlPcbProtocol.V2EndByte2)
        {
            return false;
        }

        int dataLength = rawData[2] | (rawData[3] << 8);
        if (rawData.Length != dataLength + 4 || dataLength < 7)
            return false;

        int dataItemLength = dataLength - 7;
        if (dataItemLength < 0 ||
            MeterControlPcbProtocol.CalculateChecksum(rawData, 2, dataLength - 1) != rawData[^3])
        {
            return false;
        }

        if (rawData[4] != MeterControlPcbProtocol.UplinkDirection ||
            rawData[6] != MeterControlPcbProtocol.V2MeterControlProtocolType ||
            rawData[7] != command)
        {
            return false;
        }

        meterAddress = rawData[5];
        dataItems = rawData.Skip(8).Take(dataItemLength).ToArray();
        return true;
    }

    /// <summary>判断控制PCB分组是否为V2协议。</summary>
    private static bool IsControlPcbV2(string protocolVersion)
    {
        return !protocolVersion.Equals(MeterControlPcbProtocolVersion.V1.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>写入同一控制PCB分组下所有目标工位日志。</summary>
    private static void WriteControlPcbGroupLog(
        MeterTestControlPcbGroup group,
        IEnumerable<ControlPcbStationTarget> targets,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
        params string[] lines)
    {
        foreach (ControlPcbStationTarget target in targets)
            WriteControlPcbStationLog(group, target, writeControlPcbLog, lines);
    }

    /// <summary>写入单个控制PCB目标工位日志。</summary>
    private static void WriteControlPcbStationLog(
        MeterTestControlPcbGroup group,
        ControlPcbStationTarget target,
        Action<MeterTestControlPcbGroup, ControlPcbStationTarget, string[]> writeControlPcbLog,
        params string[] lines)
    {
        writeControlPcbLog(group, target, lines);
    }

    /// <summary>统一的工位日志时间戳格式。</summary>
    private static string FormatStationLogTimestamp()
    {
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";
    }

    /// <summary>复制起动误差点配置，并临时切换为五步流程中的某个执行模式。</summary>
    private static MeterTestSubItem CloneStartingPointSubItem(
        MeterTestSubItem source,
        MeterTestExecutionMode executionMode,
        string description,
        int timeoutMs)
    {
        return new MeterTestSubItem
        {
            Name = source.Name,
            Enabled = source.Enabled,
            Protocol = source.Protocol,
            ExecutionMode = executionMode.ToString(),
            SourceControlConfig = source.SourceControlConfig,
            ControlPcbGroup = source.ControlPcbGroup,
            StartingTimeMultiplier = source.StartingTimeMultiplier,
            BasicErrorPulseCount = source.BasicErrorPulseCount,
            BasicErrorTestCount = source.BasicErrorTestCount,
            BasicErrorPulseType = source.BasicErrorPulseType,
            BasicErrorLimit = source.BasicErrorLimit,
            BasicErrorLimits = source.BasicErrorLimits,
            BasicErrorDirection = source.BasicErrorDirection,
            BasicErrorPhase = source.BasicErrorPhase,
            BasicErrorPowerFactor = source.BasicErrorPowerFactor,
            BasicErrorVoltageMultiplier = source.BasicErrorVoltageMultiplier,
            BasicErrorCurrentPoint = source.BasicErrorCurrentPoint,
            BasicErrorMinimumWaitSeconds = source.BasicErrorMinimumWaitSeconds,
            BasicErrorWaitPaddingSeconds = source.BasicErrorWaitPaddingSeconds,
            PacketIntervalMs = source.PacketIntervalMs,
            Description = description,
            TimeoutMs = timeoutMs
        };
    }
}
