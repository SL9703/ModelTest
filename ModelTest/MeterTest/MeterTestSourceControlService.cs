using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ModelTest;

namespace ModelTest.MeterTest;

/// <summary>
/// 独立的源控制执行服务。
///
/// 这个服务负责：
/// 1. 根据测试小项找到源控制配置。
/// 2. 根据资产信息里的电表类型判断单相或三相。
/// 3. 在执行升源前先打开源串口。
/// 4. 调用 XYCtr 完成具体的升源/降源接口。
///
/// 窗体只需要调用 <see cref="ExecuteAsync"/>，不再直接编写参数拼装和 DLL 调用逻辑。
/// </summary>
public sealed class MeterTestSourceControlService : IDisposable
{
    /// <summary>打开串口、初始化、升源和标准表读取之间的统一指令间隔。</summary>
    private static readonly TimeSpan SourceStepInterval = TimeSpan.FromSeconds(1);

    private readonly object monitorSync = new();
    private CancellationTokenSource? monitorCancellationTokenSource;
    private Task? monitorTask;
    private bool disposed;

    /// <summary>
    /// 标准表每次读取成功后触发。MeterTest 使用该事件实时刷新台体信息采集区域。
    /// </summary>
    public event Action<IReadOnlyDictionary<string, string>>? StandardValuesUpdated;

    /// <summary>
    /// 按当前测试小项执行一次源控制。
    /// </summary>
    /// <param name="planConfig">当前测试方案配置。</param>
    /// <param name="subItem">当前测试小项。</param>
    /// <param name="selectedStations">当前勾选的工位。</param>
    /// <param name="meterArchives">工位对应的资产档案，用于判断单相或三相。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>源控制执行结果。</returns>
    public async Task<MeterTestSourceControlResult> ExecuteAsync(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        CancellationToken cancellationToken,
        Action<string>? progressLogger = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await StopStandardMeterMonitorAsync().ConfigureAwait(false);

        try
        {
            LogMessage.Debug($"[源控制] 开始执行：小项={subItem.Name}，绑定配置={subItem.SourceControlConfig}，选中工位={FormatStations(selectedStations)}");
            ReportProgress(progressLogger, $"开始升源：小项={subItem.Name}，选中工位={FormatStations(selectedStations)}。");

            using XYCtr xyCtr = new();
            SourceControlExecutionState state = await Task.Run(
                () =>
                {
                    try
                    {
                        return ExecuteCore(
                            planConfig,
                            subItem,
                            selectedStations,
                            meterArchives,
                            xyCtr,
                            cancellationToken,
                            progressLogger);
                    }
                    catch (OperationCanceledException)
                    {
                        // 步骤间等待期间取消时保持任务取消语义，不转换成源控制失败。
                        throw;
                    }
                    catch (Exception ex)
                    {
                        LogMessage.Error("[源控制] 执行异常", ex);
                        return SourceControlExecutionState.Fail($"源控制执行异常：{ex.Message}");
                    }
                },
                cancellationToken).ConfigureAwait(false);

            if (!state.Result.Success || !state.ShouldVerify)
            {
                return state.Result;
            }

            return await VerifySourceRaisedAsync(
                xyCtr,
                state,
                cancellationToken,
                progressLogger).ConfigureAwait(false);
        }
        finally
        {
            // 验证阶段本身已经按3秒周期读取；验证结束后继续后台采集，保持台体信息区域实时更新。
            if (XYCtr.IsSourcePortOpen && !disposed)
            {
                StartStandardMeterMonitor();
            }
        }
    }

    /// <summary>
    /// 执行源控制的同步核心流程。
    /// </summary>
    private static SourceControlExecutionState ExecuteCore(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        XYCtr xyCtr,
        CancellationToken cancellationToken,
        Action<string>? progressLogger)
    {
        MeterTestSourceControlConfig? sourceConfig = ResolveSourceControlConfig(planConfig, subItem);
        if (sourceConfig is null)
        {
            LogMessage.Info($"[源控制] 小项 {subItem.Name} 未绑定源控制配置，跳过升源。");
            return SourceControlExecutionState.Ok($"测试小项 {subItem.Name} 未绑定源控制配置，跳过升源。");
        }

        if (!sourceConfig.Enabled)
        {
            LogMessage.Info($"[源控制] 配置 {sourceConfig.Name} 已禁用，跳过升源。");
            return SourceControlExecutionState.Ok($"源控制配置 {sourceConfig.Name} 已禁用，跳过升源。");
        }

        if (selectedStations.Count == 0)
        {
            LogMessage.Info($"[源控制] 配置 {sourceConfig.Name} 触发时未选择工位，跳过升源。");
            return SourceControlExecutionState.Ok("当前未选择工位，跳过源控制。");
        }

        string? sourceCurrentOverride = null;
        string? sourceCurrentAngleOverride = null;
        string? sourceVoltageOverride = null;
        string? meterInitCurrentOverride = null;
        MeterTestBasicErrorExecutionPlan? basicErrorPlan = null;
        string startingCurrentNote = string.Empty;
        if (IsStartingSourceExecution(subItem))
        {
            if (!TryResolveStartingCurrent(
                    selectedStations,
                    meterArchives,
                    out sourceCurrentOverride,
                    out startingCurrentNote,
                    out string? startingCurrentError))
            {
                LogMessage.Error($"[源控制] 启动电流 Ist 计算失败：{startingCurrentError}", null);
                return SourceControlExecutionState.Fail(startingCurrentError ?? "启动电流计算失败。");
            }

            LogMessage.Debug($"[源控制] 启动电流计算完成：{startingCurrentNote}");
            // 起动试验沿用原有规则：Ini 命令以 Ist 作为初始化电流。
            meterInitCurrentOverride = sourceCurrentOverride;
        }
        else if (IsBasicErrorPointExecution(subItem))
        {
            if (!MeterTestBasicErrorCalculator.TryCreateExecutionPlan(
                    subItem,
                    selectedStations,
                    meterArchives,
                    out basicErrorPlan,
                    out string? basicErrorError))
            {
                LogMessage.Error($"[源控制] 基本误差测试点参数计算失败：{basicErrorError}", null);
                return SourceControlExecutionState.Fail(basicErrorError ?? "基本误差测试点参数计算失败。");
            }

            sourceCurrentOverride = NormalizeNumericText(
                basicErrorPlan!.SourceCurrent.ToString(CultureInfo.InvariantCulture));
            sourceCurrentAngleOverride = NormalizeNumericText(
                basicErrorPlan.CurrentAngle.ToString(CultureInfo.InvariantCulture));
            sourceVoltageOverride = NormalizeNumericText(
                basicErrorPlan.SourcePhaseVoltage.ToString(CultureInfo.InvariantCulture));
            // 基本误差 Adj 使用“测试电流/基本电流”百分比，
            // 所以 Ini 必须使用资产信息中的基本电流 Ib/In 建立百分比基准。
            meterInitCurrentOverride = NormalizeNumericText(
                basicErrorPlan.BasicCurrent.ToString(CultureInfo.InvariantCulture));
            startingCurrentNote =
                $"基本误差点={basicErrorPlan.TestPointName}，方向={basicErrorPlan.Direction}，"
                + $"相别={basicErrorPlan.Phase}，功率因数={basicErrorPlan.PowerFactorText}，"
                + $"初始化基本电流={meterInitCurrentOverride}A，"
                + $"Adj电压={basicErrorPlan.VoltagePercentage:0.######}%，"
                + $"Adj电流={basicErrorPlan.SourceCurrent:0.#########}/{basicErrorPlan.BasicCurrent:0.#########}×100"
                + $"={basicErrorPlan.CurrentPercentage:0.#########}%";
            LogMessage.Debug($"[源控制] 基本误差升源参数计算完成：{startingCurrentNote}");
        }

        if (!TryResolvePhaseMode(sourceConfig, selectedStations, meterArchives, out MeterTestSourcePhaseMode phaseMode, out string phaseNote, out string? errorMessage))
        {
            LogMessage.Error($"[源控制] 配置 {sourceConfig.Name} 电表类型判定失败：{errorMessage}", null);
            return SourceControlExecutionState.Fail(errorMessage ?? "源控制参数解析失败。");
        }

        LogMessage.Debug($"[源控制] 配置 {sourceConfig.Name} 电表类型判定完成：{phaseNote}");

        if (!TryResolveSourceVoltage(sourceConfig, selectedStations, meterArchives, out string nominalVoltage, out string voltageNote, out string? voltageError))
        {
            LogMessage.Error($"[源控制] 配置 {sourceConfig.Name} 电压判定失败：{voltageError}", null);
            return SourceControlExecutionState.Fail(voltageError ?? "源控制电压参数解析失败。");
        }

        string sourceVoltage = nominalVoltage;
        if (!string.IsNullOrWhiteSpace(sourceVoltageOverride))
        {
            sourceVoltage = sourceVoltageOverride;
            voltageNote += $"；基本误差输出电压={sourceVoltage}V";
        }
        else if (IsCreepingSourceExecution(subItem))
        {
            if (!TryCalculateCreepingVoltage(nominalVoltage, out sourceVoltage, out string? creepingVoltageError))
            {
                LogMessage.Error($"[源控制] 潜动电压计算失败：{creepingVoltageError}", null);
                return SourceControlExecutionState.Fail(creepingVoltageError ?? "潜动电压计算失败。");
            }

            voltageNote += $"；潜动电压=额定电压{nominalVoltage}V×1.1={sourceVoltage}V";
        }

        LogMessage.Debug($"[源控制] 配置 {sourceConfig.Name} 电压判定完成：{voltageNote}");

        if (sourceConfig.SourcePort <= 0)
        {
            LogMessage.Error($"[源控制] 配置 {sourceConfig.Name} 未配置有效串口号。", null);
            return SourceControlExecutionState.Fail($"源控制配置 {sourceConfig.Name} 未配置有效串口号。");
        }

        if (!XYCtr.IsSourcePortOpen)
        {
            LogMessage.Debug($"[源控制] 准备打开源串口：Port={sourceConfig.SourcePort}，配置={sourceConfig.Name}");

            // 老版本 xyctr.dll 在传入不存在的 COM 口时可能直接退出进程，
            // 不能只依赖托管层 try/catch，因此先在调用 DLL 前检查端口是否真实存在。
            if (!TryFindSourcePort(sourceConfig.SourcePort, out string sourcePortName, out string? portError))
            {
                LogMessage.Error($"[源控制] {portError}", null);
                return SourceControlExecutionState.Fail(portError ?? "源串口不存在。");
            }

            LogMessage.Debug($"[源控制] 已确认源串口存在：{sourcePortName}，通过专用 STA 队列调用 OpenComm");
            (bool openSuccess, int openResult) = xyCtr
                .CallOpenCommAsync(sourceConfig.SourcePort, TimeSpan.FromSeconds(10))
                .GetAwaiter()
                .GetResult();
            if (!openSuccess)
            {
                LogMessage.Error($"[源控制] 打开源串口失败：配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}，返回值={openResult}", null);
                return SourceControlExecutionState.Fail($"打开源串口失败，配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}，返回值={openResult}");
            }

            LogMessage.Info($"[源控制] 打开源串口成功：配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}，返回值={openResult}");
            ReportProgress(progressLogger, $"打开源串口成功：COM{sourceConfig.SourcePort}，返回值={openResult}。");
        }
        else
        {
            LogMessage.Debug($"[源控制] 源串口已打开，跳过重复打开：配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}");
            ReportProgress(progressLogger, $"源串口 COM{sourceConfig.SourcePort} 已打开，跳过重复打开。");
        }

        DelayBetweenSourceSteps("打开串口", "初始化电表参数", cancellationToken);

        if (!TryBuildMeterInitCommand(
                selectedStations,
                meterArchives,
                phaseMode,
                nominalVoltage,
                meterInitCurrentOverride,
                out string initCommand,
                out string initNote,
                out string? initError))
        {
            LogMessage.Error($"[源控制] 初始化电表参数失败：配置={sourceConfig.Name}，{initError}", null);
            return SourceControlExecutionState.Fail(initError ?? "初始化电表参数失败。");
        }

        LogMessage.Debug($"[源控制] 准备初始化电表参数：配置={sourceConfig.Name}，{initNote}，command={initCommand}");
        ReportProgress(progressLogger, $"初始化电表参数：{initNote}，command={initCommand}。");
        (bool initSuccess, int initResult) = xyCtr
            .CallSendCommandAsync(initCommand, true, TimeSpan.FromSeconds(10))
            .GetAwaiter()
            .GetResult();
        if (!initSuccess)
        {
            LogMessage.Error($"[源控制] 初始化电表参数接口失败：配置={sourceConfig.Name}，command={initCommand}，返回值={initResult}", null);
            return SourceControlExecutionState.Fail($"初始化电表参数失败：配置={sourceConfig.Name}，参数={initCommand}，返回值={initResult}");
        }

        LogMessage.Info($"[源控制] 初始化电表参数成功：配置={sourceConfig.Name}，参数={initCommand}，返回值={initResult}");
        ReportProgress(progressLogger, $"初始化成功：command={initCommand}，返回值={initResult}。");
        DelayBetweenSourceSteps("初始化电表参数", "升源", cancellationToken);

        MeterTestSourceControlResult result = ExecuteSourceControl(
            xyCtr,
            sourceConfig,
            phaseMode,
            sourceVoltage,
            sourceCurrentOverride,
            sourceCurrentAngleOverride,
            basicErrorPlan);
        LogMessage.Debug(result.Success
            ? $"[源控制] 升源指令执行完成：{result.Message}"
            : $"[源控制] 升源指令执行失败：{result.Message}");
        ReportProgress(
            progressLogger,
            result.Success ? $"Adj升源指令下发成功：{result.Message}" : $"Adj升源指令下发失败：{result.Message}");
        string finalMessage = $"{result.Message}；{phaseNote}；{voltageNote}";
        if (!string.IsNullOrWhiteSpace(startingCurrentNote))
        {
            finalMessage += $"；{startingCurrentNote}";
        }

        return result.Success
            ? SourceControlExecutionState.Executed(
                new MeterTestSourceControlResult(true, finalMessage),
                sourceConfig,
                phaseMode,
                sourceVoltage,
                sourceCurrentOverride)
            : SourceControlExecutionState.Fail(finalMessage);
    }

    /// <summary>
    /// 在同步 DLL 调用流程中插入可取消的步骤间隔。
    /// 该方法运行在后台任务中，不阻塞 WinForms UI 线程。
    /// </summary>
    private static void DelayBetweenSourceSteps(
        string completedStep,
        string nextStep,
        CancellationToken cancellationToken)
    {
        LogMessage.Debug($"[源控制] {completedStep}完成，等待 {SourceStepInterval.TotalSeconds:0} 秒后执行{nextStep}。");
        Task.Delay(SourceStepInterval, cancellationToken).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 在异步验证流程中插入可取消的步骤间隔。
    /// </summary>
    private static async Task DelayBetweenSourceStepsAsync(
        string completedStep,
        string nextStep,
        CancellationToken cancellationToken)
    {
        LogMessage.Debug($"[源控制] {completedStep}完成，等待 {SourceStepInterval.TotalSeconds:0} 秒后执行{nextStep}。");
        await Task.Delay(SourceStepInterval, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 检查源控制配置中的端口号是否对应当前计算机实际存在的 COM 口。
    /// </summary>
    private static bool TryFindSourcePort(
        int sourcePort,
        out string sourcePortName,
        out string? errorMessage)
    {
        sourcePortName = $"COM{sourcePort}";
        errorMessage = null;

        if (sourcePort <= 0)
        {
            errorMessage = $"源串口号无效：{sourcePort}。";
            return false;
        }

        try
        {
            string[] availablePorts = SerialPort.GetPortNames();
            string expectedPortName = sourcePortName;
            if (availablePorts.Any(port => port.Equals(expectedPortName, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            string availableText = availablePorts.Length == 0
                ? "未检测到任何串口"
                : string.Join(", ", availablePorts.OrderBy(port => port, StringComparer.OrdinalIgnoreCase));
            errorMessage = $"源串口 {sourcePortName} 不存在，当前可用串口：{availableText}。";
            return false;
        }
        catch (Exception ex)
        {
            errorMessage = $"检查源串口 {sourcePortName} 失败：{ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// 通过测试小项的 <c>sourceControlConfig</c> 名称查找源控制配置。
    /// </summary>
    private static MeterTestSourceControlConfig? ResolveSourceControlConfig(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem)
    {
        string sourceConfigName = subItem.SourceControlConfig.Trim();
        if (string.IsNullOrWhiteSpace(sourceConfigName))
            return null;

        return planConfig.SourceControlConfigs.FirstOrDefault(
            item => string.Equals(item.Name, sourceConfigName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 根据资产信息中的电表类型推导单相或三相。
    /// 如果多个工位的电表类型不一致，直接返回失败，避免升源参数错配。
    /// </summary>
    private static bool TryResolvePhaseMode(
        MeterTestSourceControlConfig config,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        out MeterTestSourcePhaseMode phaseMode,
        out string phaseNote,
        out string? errorMessage)
    {
        phaseMode = MeterTestSourcePhaseMode.ThreePhase;
        phaseNote = string.Empty;
        errorMessage = null;

        List<string> meterTypes = new();
        foreach (MeterTestStationCommunication station in selectedStations)
        {
            if (!meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
                continue;

            string meterType = Normalize(archive.MeterType);
            if (!string.IsNullOrWhiteSpace(meterType))
            {
                meterTypes.Add(meterType);
            }
        }

        List<string> distinctMeterTypes = meterTypes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctMeterTypes.Count > 1)
        {
            errorMessage = $"选中工位的电表类型不一致：{string.Join("、", distinctMeterTypes)}，请先统一资产信息后再升源。";
            LogMessage.Error($"[源控制] {errorMessage}", null);
            return false;
        }

        if (distinctMeterTypes.Count == 1)
        {
            string meterType = distinctMeterTypes[0];
            if (meterType.Contains("单相", StringComparison.OrdinalIgnoreCase))
            {
                phaseMode = MeterTestSourcePhaseMode.SinglePhase;
                phaseNote = $"已根据资产信息电表类型={meterType} 判定为单相升源。";
                LogMessage.Debug($"[源控制] 工位电表类型={meterType}，判定为单相。");
                return true;
            }

            if (meterType.Contains("三相", StringComparison.OrdinalIgnoreCase))
            {
                phaseMode = MeterTestSourcePhaseMode.ThreePhase;
                phaseNote = $"已根据资产信息电表类型={meterType} 判定为三相升源。";
                LogMessage.Debug($"[源控制] 工位电表类型={meterType}，判定为三相。");
                return true;
            }
        }

        if (Enum.TryParse(config.PhaseMode, true, out MeterTestSourcePhaseMode fallbackPhaseMode))
        {
            phaseMode = fallbackPhaseMode;
            phaseNote = $"未能从资产信息识别电表类型，回退使用源控制配置 phaseMode={fallbackPhaseMode}。";
            LogMessage.Info($"[源控制] 未识别到明确电表类型，回退到配置 phaseMode={fallbackPhaseMode}。");
            return true;
        }

        phaseNote = "未能从资产信息识别电表类型，也无法解析源控制配置 phaseMode，默认按三相处理。";
        phaseMode = MeterTestSourcePhaseMode.ThreePhase;
        LogMessage.Info("[源控制] 未识别到电表类型且配置 phaseMode 无法解析，默认按三相处理。");
        return true;
    }

    /// <summary>
    /// 调用具体的源控制接口。
    /// AnyUIOutput、Adj、RangeOutputUI、ShutPowerSource 都在这里统一路由。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteSourceControl(
        XYCtr xyCtr,
        MeterTestSourceControlConfig config,
        MeterTestSourcePhaseMode phaseMode,
        string sourceVoltage,
        string? sourceCurrentOverride,
        string? sourceCurrentAngleOverride,
        MeterTestBasicErrorExecutionPlan? basicErrorPlan)
    {
        // 基本误差点的升源参数来自测试点名称和资产信息，统一走 Adj 百分比入口。
        if (basicErrorPlan is not null)
        {
            return ExecuteBasicErrorAdjOutput(xyCtr, config, basicErrorPlan);
        }

        if (!Enum.TryParse(config.InterfaceType, true, out MeterTestSourceInterfaceType interfaceType))
        {
            return MeterTestSourceControlResult.Fail($"源控制配置 {config.Name} 的 interfaceType={config.InterfaceType} 不支持。");
        }

        return interfaceType switch
        {
            MeterTestSourceInterfaceType.AnyUIOutput => ExecuteAnyUiOutput(
                xyCtr,
                config,
                phaseMode,
                sourceVoltage,
                sourceCurrentOverride,
                sourceCurrentAngleOverride),
            MeterTestSourceInterfaceType.Adj => ExecuteAdjOutput(xyCtr, config, sourceCurrentOverride),
            MeterTestSourceInterfaceType.RangeOutputUI => ExecuteRangeOutputUi(xyCtr, config, phaseMode, sourceCurrentOverride),
            MeterTestSourceInterfaceType.ShutPowerSource => ExecuteShutPowerSource(xyCtr, config),
            _ => MeterTestSourceControlResult.Fail($"源控制接口 {interfaceType} 暂未实现。")
        };
    }

    /// <summary>
    /// 使用 Adj 接口输出基本误差测试点。
    /// Adj 命令格式：Adj_电压百分比_电流百分比_相别_功率因数代码_脉冲_E。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteBasicErrorAdjOutput(
        XYCtr xyCtr,
        MeterTestSourceControlConfig config,
        MeterTestBasicErrorExecutionPlan plan)
    {
        string powerFactorText = plan.Direction == "反向有功"
            ? $"{plan.PowerFactorText}-反向"
            : plan.PowerFactorText;
        string powerFactorCode = XYCtr.ADJLC_CHANGE(powerFactorText);
        if (powerFactorCode == "-1")
        {
            string message = $"Adj 功率因数不支持：{powerFactorText}。";
            LogMessage.Error($"[源控制] {message}", null);
            return MeterTestSourceControlResult.Fail(message);
        }

        string voltagePercentage = NormalizeNumericText(
            plan.VoltagePercentage.ToString(CultureInfo.InvariantCulture));
        string currentPercentage = NormalizeNumericText(
            plan.CurrentPercentage.ToString(CultureInfo.InvariantCulture));
        string command = $"Adj_{voltagePercentage}_{currentPercentage}_{plan.Phase}_{powerFactorCode}_{config.Pulse}_E";
        LogMessage.Debug(
            $"[源控制] 基本误差 Adj 下发：测试点={plan.TestPointName}，方向={plan.Direction}，"
            + $"相别={plan.Phase}，功率因数={powerFactorText}(代码{powerFactorCode})，"
            + $"电压={voltagePercentage}%，"
            + $"电流={plan.SourceCurrent:0.#########}/{plan.BasicCurrent:0.#########}×100={currentPercentage}%，"
            + $"command={command}");

        (bool success, int result) = xyCtr
            .CallSendCommandAsync(command, true, TimeSpan.FromSeconds(10))
            .GetAwaiter()
            .GetResult();
        return success
            ? MeterTestSourceControlResult.Ok(
                $"升源成功：测试点={plan.TestPointName}，接口=Adj，参数={command}，返回值={result}")
            : MeterTestSourceControlResult.Fail(
                $"升源失败：测试点={plan.TestPointName}，接口=Adj，参数={command}，返回值={result}");
    }

    /// <summary>
    /// 调用 AnyUIOutput 接口进行升源。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteAnyUiOutput(
        XYCtr xyCtr,
        MeterTestSourceControlConfig config,
        MeterTestSourcePhaseMode phaseMode,
        string sourceVoltage,
        string? sourceCurrentOverride,
        string? sourceCurrentAngleOverride)
    {
        string ua = NormalizeSourceVoltage(sourceVoltage);
        string outputCurrent = Normalize(sourceCurrentOverride, "0");
        string currentAngle = Normalize(sourceCurrentAngleOverride, "0");
        string command = phaseMode == MeterTestSourcePhaseMode.SinglePhase
            ? string.Join("_", ua, "0", "0", outputCurrent, "0", "0", currentAngle, "0", "0", Normalize(config.Uab, "120"), Normalize(config.Uac, "240"))
            : string.Join("_", ua, ua, ua, outputCurrent, outputCurrent, outputCurrent, currentAngle, currentAngle, currentAngle, Normalize(config.Uab, "120"), Normalize(config.Uac, "240"));

        LogMessage.Debug($"[源控制] AnyUIOutput 下发：配置={config.Name}，phaseMode={phaseMode}，sourceVoltage={ua}，current={outputCurrent}，currentAngle={currentAngle}，command={command}，pulse={config.Pulse}");
        (bool success, int result) = xyCtr
            .CallAnyUIOutputAsync(command, config.Pulse, TimeSpan.FromSeconds(10))
            .GetAwaiter()
            .GetResult();
        return success
            ? MeterTestSourceControlResult.Ok($"升源成功：配置={config.Name}，接口=AnyUIOutput，参数={command}，Pulse={config.Pulse}，返回值={result}")
            : MeterTestSourceControlResult.Fail($"升源失败：配置={config.Name}，接口=AnyUIOutput，参数={command}，Pulse={config.Pulse}，返回值={result}");
    }

    /// <summary>
    /// 调用 Adj 接口进行升源。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteAdjOutput(
        XYCtr xyCtr,
        MeterTestSourceControlConfig config,
        string? sourceCurrentOverride)
    {
        string powerFactorCode = XYCtr.ADJLC_CHANGE(config.PowerFactor);
        if (powerFactorCode == "-1")
        {
            LogMessage.Error($"[源控制] ADJ 功率因数不支持：{config.PowerFactor}", null);
            return MeterTestSourceControlResult.Fail($"ADJ 升源失败：功率因数 {config.PowerFactor} 不支持。");
        }

        string phase = string.IsNullOrWhiteSpace(config.Phase) ? "H" : config.Phase.Trim();
        string current = sourceCurrentOverride ?? config.Current;
        string command = $"Adj_{config.Voltage}_{current}_{phase}_{powerFactorCode}_{config.Pulse}_E";
        LogMessage.Debug($"[源控制] Adj 下发：配置={config.Name}，command={command}");
        (bool success, int result) = xyCtr
            .CallSendCommandAsync(command, true, TimeSpan.FromSeconds(10))
            .GetAwaiter()
            .GetResult();
        return success
            ? MeterTestSourceControlResult.Ok($"升源成功：配置={config.Name}，接口=Adj，参数={command}，返回值={result}")
            : MeterTestSourceControlResult.Fail($"升源失败：配置={config.Name}，接口=Adj，参数={command}，返回值={result}");
    }

    /// <summary>
    /// 调用 RangeOutputUI 接口进行升源。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteRangeOutputUi(
        XYCtr xyCtr,
        MeterTestSourceControlConfig config,
        MeterTestSourcePhaseMode phaseMode,
        string? sourceCurrentOverride)
    {
        SourcePhaseValues values = BuildSourcePhaseValues(config, phaseMode, sourceCurrentOverride);
        string command = string.Join("_", values.Ua, values.Ub, values.Uc, values.Ia, values.Ib, values.Ic);
        LogMessage.Debug($"[源控制] RangeOutputUI 下发：配置={config.Name}，phaseMode={phaseMode}，command={command}");
        (bool success, int result) = xyCtr.CallRangeOutputUI(command);
        return success
            ? MeterTestSourceControlResult.Ok($"升源成功：配置={config.Name}，接口=RangeOutputUI，参数={command}，返回值={result}")
            : MeterTestSourceControlResult.Fail($"升源失败：配置={config.Name}，接口=RangeOutputUI，参数={command}，返回值={result}");
    }

    /// <summary>
    /// 调用 ShutPowerSource 接口进行降源。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteShutPowerSource(XYCtr xyCtr, MeterTestSourceControlConfig config)
    {
        LogMessage.Debug($"[源控制] ShutPowerSource 下发：配置={config.Name}，shutMode={config.ShutMode}");
        (bool success, int result) = xyCtr.CallShutPowerSource(config.ShutMode);
        return success
            ? MeterTestSourceControlResult.Ok($"降源成功：配置={config.Name}，接口=ShutPowerSource，ShutMode={config.ShutMode}，返回值={result}")
            : MeterTestSourceControlResult.Fail($"降源失败：配置={config.Name}，接口=ShutPowerSource，ShutMode={config.ShutMode}，返回值={result}");
    }

    /// <summary>
    /// 升源指令成功后，每隔配置的采样周期读取一次标准表。
    /// 在验证超时前，相关相位的电压和电流全部进入目标值正负允许误差范围才判定升源成功。
    /// </summary>
    private async Task<MeterTestSourceControlResult> VerifySourceRaisedAsync(
        XYCtr xyCtr,
        SourceControlExecutionState state,
        CancellationToken cancellationToken,
        Action<string>? progressLogger)
    {
        TimeSpan verificationTimeout = TimeSpan.FromSeconds(Math.Max(1, state.VerificationTimeoutSeconds));
        TimeSpan samplingInterval = TimeSpan.FromSeconds(
            Math.Clamp(state.VerificationIntervalSeconds, 1, Math.Max(1, state.VerificationTimeoutSeconds)));
        decimal tolerancePercent = state.VerificationTolerancePercent > 0
            ? state.VerificationTolerancePercent
            : 0.03m;

        LogMessage.Debug(
            $"[源控制] 升源指令已下发，开始标准表达标验证：配置={state.SourceConfigName}，"
            + $"phaseMode={state.PhaseMode}，目标电压={state.SourceVoltage}V，"
            + $"目标电流={(string.IsNullOrWhiteSpace(state.SourceCurrent) ? "不校验" : state.SourceCurrent + "A")}，"
            + $"采样周期={samplingInterval.TotalSeconds:0}s，最长等待={verificationTimeout.TotalSeconds:0}s，"
            + $"允许误差=正负{tolerancePercent:0.######}%。");
        ReportProgress(
            progressLogger,
            $"开始标准表达标验证：目标电压={state.SourceVoltage}V，"
            + $"目标电流={(string.IsNullOrWhiteSpace(state.SourceCurrent) ? "不校验" : state.SourceCurrent + "A")}，"
            + $"最长等待={verificationTimeout.TotalSeconds:0}s，采样周期={samplingInterval.TotalSeconds:0}s，"
            + $"允许误差=±{tolerancePercent:0.######}%。");

        Stopwatch stopwatch = Stopwatch.StartNew();
        IReadOnlyDictionary<string, string>? lastStandValues = null;
        string lastDetail = "尚未读取到有效标准表数据";
        int sampleIndex = 0;

        while (stopwatch.Elapsed < verificationTimeout)
        {
            TimeSpan remaining = verificationTimeout - stopwatch.Elapsed;
            TimeSpan delay = remaining < samplingInterval ? remaining : samplingInterval;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            sampleIndex++;
            remaining = verificationTimeout - stopwatch.Elapsed;
            TimeSpan readTimeout = remaining > TimeSpan.Zero && remaining < TimeSpan.FromSeconds(5)
                ? remaining
                : TimeSpan.FromSeconds(5);
            if (readTimeout <= TimeSpan.Zero)
            {
                break;
            }

            StandardMeterReadResult readResult = await ReadStandardMeterAsync(
                xyCtr,
                readTimeout,
                cancellationToken).ConfigureAwait(false);
            if (!readResult.Success || readResult.Values is null || readResult.StandValues is null)
            {
                lastDetail = readResult.Message;
                LogMessage.Debug(
                    $"[源控制] 第{sampleIndex}次标准表采样失败，已等待{stopwatch.Elapsed.TotalSeconds:0.0}s：{readResult.Message}");
                ReportProgress(
                    progressLogger,
                    $"第{sampleIndex}次标准表采样失败，已等待{stopwatch.Elapsed.TotalSeconds:0.0}s：{readResult.Message}。");
                continue;
            }

            lastStandValues = readResult.StandValues;
            PublishStandardValues(lastStandValues);

            if (!TryEvaluateSourceTolerance(
                    readResult.Values,
                    state,
                    tolerancePercent,
                    out bool withinTolerance,
                    out lastDetail))
            {
                LogMessage.Debug(
                    $"[源控制] 第{sampleIndex}次标准表采样无法判定，已等待{stopwatch.Elapsed.TotalSeconds:0.0}s：{lastDetail}");
                ReportProgress(
                    progressLogger,
                    $"第{sampleIndex}次标准表采样无法判定，已等待{stopwatch.Elapsed.TotalSeconds:0.0}s：{lastDetail}。");
                continue;
            }

            LogMessage.Debug(
                $"[源控制] 第{sampleIndex}次标准表采样，已等待{stopwatch.Elapsed.TotalSeconds:0.0}s：{lastDetail}，"
                + $"结果={(withinTolerance ? "达标" : "未达标")}。");
            ReportProgress(
                progressLogger,
                $"第{sampleIndex}次标准表采样，已等待{stopwatch.Elapsed.TotalSeconds:0.0}s："
                + $"{lastDetail}，结果={(withinTolerance ? "达标" : "未达标")}。");
            if (withinTolerance)
            {
                string successMessage = $"{state.Result.Message}；升源后在{stopwatch.Elapsed.TotalSeconds:0.0}s内达到正负{tolerancePercent:0.######}%：{lastDetail}。";
                ReportProgress(
                    progressLogger,
                    $"升源验证成功：{stopwatch.Elapsed.TotalSeconds:0.0}s内进入±{tolerancePercent:0.######}%范围。");
                return new MeterTestSourceControlResult(true, successMessage)
                {
                    StandValues = lastStandValues
                };
            }
        }

        string failureMessage = $"{state.Result.Message}；升源后{verificationTimeout.TotalSeconds:0}s内未达到正负{tolerancePercent:0.######}%：{lastDetail}。";
        LogMessage.Error($"[源控制] {failureMessage}", null);
        ReportProgress(
            progressLogger,
            $"升源验证失败：{verificationTimeout.TotalSeconds:0}s内未进入±{tolerancePercent:0.######}%范围，{lastDetail}。");
        return new MeterTestSourceControlResult(false, failureMessage)
        {
            StandValues = lastStandValues
        };
    }

    /// <summary>
    /// 向调用方转发源控制进度。回调异常不得中断硬件控制流程。
    /// </summary>
    private static void ReportProgress(Action<string>? progressLogger, string message)
    {
        if (progressLogger is null)
            return;

        try
        {
            progressLogger(message);
        }
        catch (Exception ex)
        {
            LogMessage.Error("[源控制] 进度日志回调异常。", ex);
        }
    }

    /// <summary>
    /// 读取并解析一次标准表数据。读取成功时必须得到完整的15项数据。
    /// </summary>
    private static async Task<StandardMeterReadResult> ReadStandardMeterAsync(
        XYCtr xyCtr,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] standValueBuffer = new byte[1024];

        (bool success, int result) = await xyCtr
            .CallReadStandValueAsync("model1", standValueBuffer, timeout)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!success)
        {
            return StandardMeterReadResult.Fail($"CallReadStandValue失败，返回值={result}");
        }

        string rawStandValue = Encoding.Default.GetString(standValueBuffer).TrimEnd('\0', '\r', '\n', ' ');
        List<string> standParts = ModelTool.SplitString(rawStandValue)
            .Select(item => item ?? string.Empty)
            .ToList();
        if (standParts.Count < 15)
        {
            return StandardMeterReadResult.Fail($"标准表数据项不足，期望15项，实际{standParts.Count}项，原始数据={rawStandValue}");
        }

        IReadOnlyDictionary<string, string> standValues = BuildStandValueMap(standParts);
        return StandardMeterReadResult.Ok(standParts, standValues, rawStandValue);
    }

    /// <summary>
    /// 把标准表 15 组数据映射成台体信息采集区域使用的指标名。
    /// </summary>
    private static IReadOnlyDictionary<string, string> BuildStandValueMap(IReadOnlyList<string> standParts)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Ua"] = standParts[0],
            ["Ub"] = standParts[1],
            ["Uc"] = standParts[2],
            ["Ia"] = standParts[3],
            ["Ib"] = standParts[4],
            ["Ic"] = standParts[5],
            ["Φa"] = standParts[6],
            ["Φb"] = standParts[7],
            ["Φc"] = standParts[8],
            ["Pa"] = standParts[9],
            ["Pb"] = standParts[10],
            ["Pc"] = standParts[11],
            ["Qa"] = standParts[12],
            ["Qb"] = standParts[13],
            ["Qc"] = standParts[14]
        };

        return values;
    }

    /// <summary>
    /// 在一次升源验证结束后继续每3秒读取标准表。
    /// 新一轮控源开始前会先停止该任务，避免监控读取与初始化、升源指令交叉执行。
    /// </summary>
    private void StartStandardMeterMonitor()
    {
        lock (monitorSync)
        {
            if (disposed || monitorTask is { IsCompleted: false })
                return;

            monitorCancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = monitorCancellationTokenSource.Token;
            monitorTask = Task.Run(() => MonitorStandardMeterAsync(token), CancellationToken.None);
        }

        LogMessage.Debug("[源控制] 标准表3秒周期采集任务已启动。");
    }

    /// <summary>停止后台标准表采集，并等待当前一次原生读取结束。</summary>
    private async Task StopStandardMeterMonitorAsync()
    {
        CancellationTokenSource? cancellationTokenSource;
        Task? runningTask;
        lock (monitorSync)
        {
            cancellationTokenSource = monitorCancellationTokenSource;
            runningTask = monitorTask;
            monitorCancellationTokenSource = null;
            monitorTask = null;
        }

        if (cancellationTokenSource is null)
            return;

        cancellationTokenSource.Cancel();
        if (runningTask is not null)
        {
            try
            {
                await runningTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // 正常停止周期采集时会取消等待，不需要作为错误记录。
            }
        }

        cancellationTokenSource.Dispose();
        LogMessage.Debug("[源控制] 标准表3秒周期采集任务已停止。");
    }

    /// <summary>标准表后台周期采集循环。</summary>
    private async Task MonitorStandardMeterAsync(CancellationToken cancellationToken)
    {
        try
        {
            using XYCtr xyCtr = new();
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
                StandardMeterReadResult result = await ReadStandardMeterAsync(
                    xyCtr,
                    TimeSpan.FromSeconds(5),
                    cancellationToken).ConfigureAwait(false);
                if (!result.Success || result.StandValues is null)
                {
                    LogMessage.Debug($"[源控制] 标准表周期采集失败：{result.Message}");
                    continue;
                }

                PublishStandardValues(result.StandValues);
                LogMessage.Debug(
                    $"[源控制] 标准表周期采集："
                    + $"Ua={result.StandValues["Ua"]}，Ub={result.StandValues["Ub"]}，Uc={result.StandValues["Uc"]}，"
                    + $"Ia={result.StandValues["Ia"]}，Ib={result.StandValues["Ib"]}，Ic={result.StandValues["Ic"]}。");
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 窗体关闭或下一轮控源开始时取消周期采集，属于正常生命周期。
        }
        catch (Exception ex)
        {
            LogMessage.Error("[源控制] 标准表周期采集任务异常", ex);
        }
    }

    /// <summary>安全发布标准表数据，避免界面订阅异常中断控源流程。</summary>
    private void PublishStandardValues(IReadOnlyDictionary<string, string> standValues)
    {
        try
        {
            StandardValuesUpdated?.Invoke(standValues);
        }
        catch (Exception ex)
        {
            LogMessage.Error("[源控制] 发布标准表数据失败", ex);
        }
    }

    /// <summary>
    /// 对参与输出的每一相电压和电流分别判断误差范围。
    /// 普通通信测试只输出电压，因此不校验电流；起动试验传入 Ist 时同时校验电压和电流。
    /// </summary>
    private static bool TryEvaluateSourceTolerance(
        IReadOnlyList<string> standParts,
        SourceControlExecutionState state,
        decimal tolerancePercent,
        out bool withinTolerance,
        out string detail)
    {
        withinTolerance = false;
        detail = string.Empty;
        if (!TryParseNumber(state.SourceVoltage, out decimal targetVoltage) || targetVoltage <= 0)
        {
            detail = $"目标电压解析失败：{state.SourceVoltage}";
            return false;
        }

        int phaseCount = state.PhaseMode == MeterTestSourcePhaseMode.SinglePhase ? 1 : 3;
        string[] voltageNames = { "Ua", "Ub", "Uc" };
        if (!TryEvaluateMeasurements(
                standParts,
                0,
                voltageNames,
                phaseCount,
                targetVoltage,
                tolerancePercent,
                out bool voltageWithinTolerance,
                out string voltageDetail))
        {
            detail = voltageDetail;
            return false;
        }

        bool currentWithinTolerance = true;
        string currentDetail = "电流不校验";
        if (!string.IsNullOrWhiteSpace(state.SourceCurrent))
        {
            if (!TryParseNumber(state.SourceCurrent, out decimal targetCurrent) || targetCurrent <= 0)
            {
                detail = $"目标电流解析失败：{state.SourceCurrent}";
                return false;
            }

            string[] currentNames = { "Ia", "Ib", "Ic" };
            if (!TryEvaluateMeasurements(
                    standParts,
                    3,
                    currentNames,
                    phaseCount,
                    targetCurrent,
                    tolerancePercent,
                    out currentWithinTolerance,
                    out currentDetail))
            {
                detail = currentDetail;
                return false;
            }
        }

        withinTolerance = voltageWithinTolerance && currentWithinTolerance;
        detail = $"{voltageDetail}；{currentDetail}";
        return true;
    }

    /// <summary>判断一组同目标值的相量是否全部进入允许范围。</summary>
    private static bool TryEvaluateMeasurements(
        IReadOnlyList<string> standParts,
        int startIndex,
        IReadOnlyList<string> names,
        int count,
        decimal target,
        decimal tolerancePercent,
        out bool withinTolerance,
        out string detail)
    {
        decimal tolerance = target * tolerancePercent / 100m;
        decimal lower = target - tolerance;
        decimal upper = target + tolerance;
        List<string> actualValues = new();
        withinTolerance = true;

        for (int index = 0; index < count; index++)
        {
            string rawValue = standParts[startIndex + index];
            if (!TryParseNumber(rawValue, out decimal actual))
            {
                detail = $"标准表{names[index]}解析失败：{rawValue}";
                withinTolerance = false;
                return false;
            }

            bool phaseWithinTolerance = actual >= lower && actual <= upper;
            withinTolerance &= phaseWithinTolerance;
            actualValues.Add($"{names[index]}={actual:0.#########}");
        }

        detail = $"目标={target:0.#########}，范围=[{lower:0.#########},{upper:0.#########}]，实测{string.Join("、", actualValues)}";
        return true;
    }

    /// <summary>
    /// 生成升源需要的相量参数。
    /// 单相和三相的默认值不同，这里统一封装。
    /// </summary>
    private static SourcePhaseValues BuildSourcePhaseValues(
        MeterTestSourceControlConfig config,
        MeterTestSourcePhaseMode phaseMode,
        string? sourceCurrentOverride)
    {
        string voltage = Normalize(config.Voltage, "220");
        string current = Normalize(sourceCurrentOverride, Normalize(config.Current, "5"));

        if (phaseMode == MeterTestSourcePhaseMode.SinglePhase)
        {
            return new SourcePhaseValues(
                Normalize(config.VoltageA, voltage),
                "0",
                "0",
                Normalize(sourceCurrentOverride, Normalize(config.CurrentA, current)),
                "0",
                "0");
        }

        return new SourcePhaseValues(
            Normalize(config.VoltageA, voltage),
            Normalize(config.VoltageB, voltage),
            Normalize(config.VoltageC, voltage),
            Normalize(sourceCurrentOverride, Normalize(config.CurrentA, current)),
            Normalize(sourceCurrentOverride, Normalize(config.CurrentB, current)),
            Normalize(sourceCurrentOverride, Normalize(config.CurrentC, current)));
    }

    /// <summary>
    /// 从资产信息里的电压字段提取升源使用的数值。
    /// 例如：
    /// - 220V => 220
    /// - 3×220/380V => 220
    /// - 3×57.7/100V => 57.7
    /// </summary>
    private static string NormalizeSourceVoltage(string voltageText)
    {
        string normalized = Normalize(voltageText, "220");

        Match matchedVoltage = Regex.Match(normalized, @"[x×]\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        if (matchedVoltage.Success)
        {
            return NormalizeNumericText(matchedVoltage.Groups[1].Value);
        }

        Match fallbackVoltage = Regex.Match(normalized, @"\d+(?:\.\d+)?");
        if (fallbackVoltage.Success)
        {
            return NormalizeNumericText(fallbackVoltage.Value);
        }

        return "220";
    }

    /// <summary>
    /// 把数值字符串整理成更干净的展示格式。
    /// </summary>
    private static string NormalizeNumericText(string value)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal numericValue))
        {
            return numericValue % 1 == 0
                ? numericValue.ToString("0", CultureInfo.InvariantCulture)
                : numericValue.ToString("0.######", CultureInfo.InvariantCulture);
        }

        return value.Trim();
    }

    /// <summary>判断当前源控制小项是否为起动试验的启动电流升源。</summary>
    private static bool IsStartingSourceExecution(MeterTestSubItem subItem)
    {
        return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
            && executionMode == MeterTestExecutionMode.StartingSource;
    }

    /// <summary>判断当前小项是否为潜动试验的1.1倍额定电压升源。</summary>
    private static bool IsCreepingSourceExecution(MeterTestSubItem subItem)
    {
        return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
            && executionMode == MeterTestExecutionMode.CreepingSource;
    }

    /// <summary>判断当前小项是否为有功基本误差完整测试点。</summary>
    private static bool IsBasicErrorPointExecution(MeterTestSubItem subItem)
    {
        return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
            && executionMode == MeterTestExecutionMode.BasicErrorPoint;
    }

    /// <summary>
    /// 按潜动试验规则计算输出电压：Ucreep=1.1×额定电压。
    /// </summary>
    private static bool TryCalculateCreepingVoltage(
        string nominalVoltage,
        out string creepingVoltage,
        out string? errorMessage)
    {
        creepingVoltage = string.Empty;
        errorMessage = null;
        string normalizedNominalVoltage = NormalizeSourceVoltage(nominalVoltage);
        if (!TryParseNumber(normalizedNominalVoltage, out decimal voltage) || voltage <= 0)
        {
            errorMessage = $"额定电压无法解析：{nominalVoltage}";
            return false;
        }

        creepingVoltage = NormalizeNumericText(
            (voltage * 1.1m).ToString(CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>
    /// 按 JJG596 起动试验规则计算 Ist。
    /// 直接式以基本电流/10 为基准，互感式以基本电流/20 为基准；多个工位最终 Ist 必须一致，
    /// 因为同一次源控制只能向同一套源设备下发一个公共电流参数。
    /// </summary>
    private static bool TryResolveStartingCurrent(
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        out string startingCurrent,
        out string note,
        out string? errorMessage)
    {
        startingCurrent = string.Empty;
        note = string.Empty;
        errorMessage = null;
        List<(int StationNo, decimal Current)> calculatedValues = new();

        foreach (MeterTestStationCommunication station in selectedStations)
        {
            if (!meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
            {
                errorMessage = $"工位{station.StationNo}缺少资产档案，无法计算启动电流。";
                return false;
            }

            if (!MeterTestStartingTestCalculator.TryCalculateStartingCurrent(
                    archive,
                    out decimal ist,
                    out string calculationNote,
                    out string? calculationError))
            {
                errorMessage = $"工位{station.StationNo}{calculationError}";
                return false;
            }

            calculatedValues.Add((station.StationNo, ist));
            LogMessage.Debug($"[源控制] 工位{station.StationNo}启动电流：{calculationNote}。");
        }

        List<decimal> distinctValues = calculatedValues
            .Select(item => item.Current)
            .Distinct()
            .ToList();
        if (distinctValues.Count != 1)
        {
            errorMessage = "选中工位计算出的启动电流不一致："
                + string.Join("、", calculatedValues.Select(item => $"工位{item.StationNo}={item.Current:0.######}A"))
                + "，请先统一资产信息后再升源。";
            return false;
        }

        startingCurrent = NormalizeNumericText(distinctValues[0].ToString(CultureInfo.InvariantCulture));
        note = $"Ist={startingCurrent}A，计算依据：{string.Join("；", calculatedValues.Select(item => $"工位{item.StationNo}={item.Current:0.######}A"))}";
        return true;
    }

    /// <summary>
    /// 从带单位或普通数值字符串中解析十进制数。
    /// </summary>
    private static bool TryParseNumber(string? text, out decimal value)
    {
        value = 0;
        string normalized = Normalize(text);
        if (decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;

        Match numberMatch = Regex.Match(normalized, @"[+-]?\d+(?:\.\d+)?");
        return numberMatch.Success &&
               decimal.TryParse(numberMatch.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>
    /// 从资产信息中解析升源电压。
    /// 选中多个工位时要求电压一致，避免升源参数错配。
    /// </summary>
    private static bool TryResolveSourceVoltage(
        MeterTestSourceControlConfig config,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        out string sourceVoltage,
        out string voltageNote,
        out string? errorMessage)
    {
        sourceVoltage = string.Empty;
        voltageNote = string.Empty;
        errorMessage = null;

        List<string> voltages = new();
        foreach (MeterTestStationCommunication station in selectedStations)
        {
            if (!meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
                continue;

            string voltage = NormalizeSourceVoltage(archive.Voltage);
            if (!string.IsNullOrWhiteSpace(voltage))
            {
                voltages.Add(voltage);
            }
        }

        List<string> distinctVoltages = voltages
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctVoltages.Count > 1)
        {
            errorMessage = $"选中工位的电压不一致：{string.Join("、", distinctVoltages)}，请先统一资产信息后再升源。";
            LogMessage.Error($"[源控制] {errorMessage}", null);
            return false;
        }

        if (distinctVoltages.Count == 1)
        {
            sourceVoltage = distinctVoltages[0];
            voltageNote = $"已根据资产信息电压={sourceVoltage} 作为升源电压。";
            LogMessage.Debug($"[源控制] 资产电压识别成功：{sourceVoltage}");
            return true;
        }

        errorMessage = "未能从资产信息识别电压，请先确认资产信息已完整保存到数据库。";
        LogMessage.Info("[源控制] 未识别到资产电压，已停止升源。");
        return false;
    }

    /// <summary>
    /// 根据资产信息构造源初始化命令。
    /// 命令格式：Ini_接线方式_电压代码_电流_有功常数_E。
    /// </summary>
    private static bool TryBuildMeterInitCommand(
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        MeterTestSourcePhaseMode phaseMode,
        string sourceVoltage,
        string? sourceCurrentOverride,
        out string command,
        out string note,
        out string? errorMessage)
    {
        command = string.Empty;
        note = string.Empty;
        errorMessage = null;

        string meterConnection = phaseMode == MeterTestSourcePhaseMode.SinglePhase ? "0" : "1";
        string meterVoltage = XYCtr.Init_meterV(NormalizeSourceVoltage(sourceVoltage));
        if (meterVoltage == "-1")
        {
            errorMessage = $"资产电压不支持初始化转换：{sourceVoltage}";
            return false;
        }

        string current;
        if (!string.IsNullOrWhiteSpace(sourceCurrentOverride))
        {
            current = NormalizeCurrentForInit(sourceCurrentOverride);
        }
        else if (!TryResolveSameArchiveValue(
                     selectedStations,
                     meterArchives,
                     archive => NormalizeCurrentForInit(archive.Current),
                     "基本电流",
                     out current,
                     out errorMessage))
        {
            return false;
        }

        if (!TryResolveSameArchiveValue(
                selectedStations,
                meterArchives,
                archive => NormalizeNumericText(Normalize(archive.ActiveConstant)),
                "有功常数",
                out string activeConstant,
                out errorMessage))
        {
            return false;
        }

        command = $"Ini_{meterConnection}_{meterVoltage}_{current}_{activeConstant}_E";
        note = $"接线方式={meterConnection}，电压代码={meterVoltage}，电流={current}，有功常数={activeConstant}";
        return true;
    }

    /// <summary>
    /// 从选中工位资产信息中读取同一个参数；多个工位参数不一致时停止升源。
    /// </summary>
    private static bool TryResolveSameArchiveValue(
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        Func<MeterArchiveData, string> selector,
        string fieldName,
        out string value,
        out string? errorMessage)
    {
        value = string.Empty;
        errorMessage = null;

        List<string> values = new();
        foreach (MeterTestStationCommunication station in selectedStations)
        {
            if (!meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
                continue;

            string fieldValue = Normalize(selector(archive));
            if (!string.IsNullOrWhiteSpace(fieldValue))
            {
                values.Add(fieldValue);
            }
        }

        List<string> distinctValues = values
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctValues.Count == 0)
        {
            errorMessage = $"未能从资产信息识别{fieldName}，请先确认资产信息已完整保存到数据库。";
            return false;
        }

        if (distinctValues.Count > 1)
        {
            errorMessage = $"选中工位的{fieldName}不一致：{string.Join("、", distinctValues)}，请先统一资产信息后再初始化电表参数。";
            return false;
        }

        value = distinctValues[0];
        return true;
    }

    /// <summary>
    /// 初始化命令里的电流参数只需要数值，例如资产信息 5A 转成 5。
    /// </summary>
    private static string NormalizeCurrentForInit(string currentText)
    {
        return TryParseNumber(currentText, out decimal current)
            ? NormalizeNumericText(current.ToString(CultureInfo.InvariantCulture))
            : Normalize(currentText).Replace("A", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 规范化字符串，空值时回退默认值。
    /// </summary>
    private static string Normalize(string? value, string defaultValue = "")
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
    }

    /// <summary>
    /// 源控制执行结果。
    /// </summary>
    public sealed record MeterTestSourceControlResult(bool Success, string Message)
    {
        /// <summary>
        /// 标准表读回并解析后的指标值。用于 MeterTest 台体信息采集区域回填。
        /// </summary>
        public IReadOnlyDictionary<string, string>? StandValues { get; init; }

        /// <summary>
        /// 创建成功结果。
        /// </summary>
        public static MeterTestSourceControlResult Ok(string message)
        {
            return new MeterTestSourceControlResult(true, message);
        }

        /// <summary>
        /// 创建失败结果。
        /// </summary>
        public static MeterTestSourceControlResult Fail(string message)
        {
            return new MeterTestSourceControlResult(false, message);
        }
    }

    /// <summary>
    /// 升源需要的三相电压/电流值。
    /// </summary>
    private sealed record SourcePhaseValues(string Ua, string Ub, string Uc, string Ia, string Ib, string Ic);

    /// <summary>一次标准表读取和解析结果。</summary>
    private sealed record StandardMeterReadResult(
        bool Success,
        string Message,
        IReadOnlyList<string>? Values,
        IReadOnlyDictionary<string, string>? StandValues)
    {
        public static StandardMeterReadResult Ok(
            IReadOnlyList<string> values,
            IReadOnlyDictionary<string, string> standValues,
            string rawValue)
        {
            return new StandardMeterReadResult(true, rawValue, values, standValues);
        }

        public static StandardMeterReadResult Fail(string message)
        {
            return new StandardMeterReadResult(false, message, null, null);
        }
    }

    /// <summary>
    /// 源控制指令执行后的上下文。只有真正下发过源控制指令时才需要继续读取标准表校验。
    /// </summary>
    private sealed record SourceControlExecutionState(
        MeterTestSourceControlResult Result,
        string SourceConfigName,
        MeterTestSourcePhaseMode PhaseMode,
        string SourceVoltage,
        string SourceCurrent,
        int VerificationTimeoutSeconds,
        int VerificationIntervalSeconds,
        decimal VerificationTolerancePercent,
        bool ShouldVerify)
    {
        public static SourceControlExecutionState Ok(string message)
        {
            return new SourceControlExecutionState(
                MeterTestSourceControlResult.Ok(message),
                string.Empty,
                MeterTestSourcePhaseMode.ThreePhase,
                string.Empty,
                string.Empty,
                20,
                3,
                0.03m,
                false);
        }

        public static SourceControlExecutionState Fail(string message)
        {
            return new SourceControlExecutionState(
                MeterTestSourceControlResult.Fail(message),
                string.Empty,
                MeterTestSourcePhaseMode.ThreePhase,
                string.Empty,
                string.Empty,
                20,
                3,
                0.03m,
                false);
        }

        public static SourceControlExecutionState Executed(
            MeterTestSourceControlResult result,
            MeterTestSourceControlConfig sourceConfig,
            MeterTestSourcePhaseMode phaseMode,
            string sourceVoltage,
            string? sourceCurrent)
        {
            return new SourceControlExecutionState(
                result,
                sourceConfig.Name,
                phaseMode,
                sourceVoltage,
                sourceCurrent ?? string.Empty,
                sourceConfig.VerificationTimeoutSeconds,
                sourceConfig.VerificationIntervalSeconds,
                sourceConfig.VerificationTolerancePercent,
                true);
        }
    }

    /// <summary>停止标准表后台采集任务并释放事件订阅。</summary>
    public void Dispose()
    {
        CancellationTokenSource? cancellationTokenSource;
        Task? runningTask;
        lock (monitorSync)
        {
            if (disposed)
                return;

            disposed = true;
            cancellationTokenSource = monitorCancellationTokenSource;
            runningTask = monitorTask;
            monitorCancellationTokenSource = null;
            monitorTask = null;
            StandardValuesUpdated = null;
        }

        cancellationTokenSource?.Cancel();
        if (cancellationTokenSource is not null)
        {
            if (runningTask is null)
            {
                cancellationTokenSource.Dispose();
            }
            else
            {
                _ = runningTask.ContinueWith(
                    _ => cancellationTokenSource.Dispose(),
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }

    /// <summary>
    /// 格式化工位列表，便于日志打印。
    /// </summary>
    private static string FormatStations(IReadOnlyList<MeterTestStationCommunication> stations)
    {
        if (stations.Count == 0)
            return "空";

        return string.Join(
            "；",
            stations.Select(station => $"{station.StationNo}@{station.Ip}:{station.Port}"));
    }
}
