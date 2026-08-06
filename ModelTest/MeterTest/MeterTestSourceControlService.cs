using System;
using System.Collections.Concurrent;
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
/// 窗体只需要调用 <see cref="ExecuteBatchOnceAsync"/>，不再直接编写参数拼装和 DLL 调用逻辑。
/// </summary>
public sealed class MeterTestSourceControlService : IDisposable
{
    /// <summary>打开串口、初始化、升源和标准表读取之间的统一指令间隔。</summary>
    private static readonly TimeSpan SourceStepInterval = TimeSpan.FromSeconds(1);

    private readonly object monitorSync = new();
    private readonly SemaphoreSlim runInitializationGate = new(1, 1);
    private readonly MeterTestAccessDatabaseService databaseService;
    private readonly ConcurrentDictionary<string, Lazy<Task<MeterTestSourceControlResult>>> sourceExecutionBatches = new();
    private CancellationTokenSource? monitorCancellationTokenSource;
    private Task? monitorTask;
    private IReadOnlyDictionary<string, string>? latestStandardValues;
    private int sourceOutputActive;
    private int runSourceInitialized;
    private int runInitializedSourcePort;
    private MeterTestSourcePhaseMode runInitializedPhaseMode;
    private string runInitializedSourceConfigName = string.Empty;
    private string runInitializedCommand = string.Empty;
    private bool disposed;

    /// <summary>
    /// 最近一次成功源控制指令是否为升源输出。
    /// 短路自检用该状态阻止在程序已升源后下发0x86危险命令。
    /// </summary>
    public bool IsSourceOutputActive => Volatile.Read(ref sourceOutputActive) != 0;

    /// <summary>返回最近一次标准表成功采样值；尚未采样时返回null。</summary>
    public IReadOnlyDictionary<string, string>? LatestStandardValues => Volatile.Read(ref latestStandardValues);

    /// <summary>
    /// 标准表每次读取成功后触发。MeterTest 使用该事件实时刷新台体信息采集区域。
    /// </summary>
    public event Action<IReadOnlyDictionary<string, string>>? StandardValuesUpdated;

    /// <summary>
    /// 创建源控制服务。数据库服务用于读取基本误差FA角度，
    /// 保证 AnyUIOutput 下发参数与现场可维护配置一致。
    /// </summary>
    public MeterTestSourceControlService(MeterTestAccessDatabaseService? databaseService = null)
    {
        this.databaseService = databaseService ?? new MeterTestAccessDatabaseService();
    }

    /// <summary>
    /// 开始新一轮界面测试，只清除上一轮源控制批次。
    /// 已成功执行的 Ini 命令在当前 MeterTest 控件生命周期内保留；只有初始化参数变化时才重新发送。
    /// </summary>
    public void BeginRun()
    {
        sourceExecutionBatches.Clear();
    }

    /// <summary>
    /// 执行一轮测试的唯一源初始化步骤。
    ///
    /// 该方法由“执行测试”按钮调用，负责计算当前资产对应的 Ini 命令。
    /// 当前程序运行期间参数未变化时复用上次成功结果；参数变化时才重新发送 Ini。
    /// </summary>
    public async Task<MeterTestSourceControlResult> InitializeRunAsync(
        MeterTestPlanConfig planConfig,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        CancellationToken cancellationToken,
        Action<string>? progressLogger = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        await runInitializationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 每次执行前都重新计算目标 Ini 命令，内部根据完整命令和源串口判断是否需要重发。
            return await InitializeRunCoreAsync(
                planConfig,
                selectedStations,
                meterArchives,
                cancellationToken,
                progressLogger).ConfigureAwait(false);
        }
        finally
        {
            runInitializationGate.Release();
        }
    }

    /// <summary>
    /// 实际执行打开串口和发送 Ini 的内部入口，只允许由受保护的
    /// <see cref="InitializeRunAsync"/> 调用。
    /// </summary>
    private async Task<MeterTestSourceControlResult> InitializeRunCoreAsync(
        MeterTestPlanConfig planConfig,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        CancellationToken cancellationToken,
        Action<string>? progressLogger = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (selectedStations.Count == 0)
        {
            return MeterTestSourceControlResult.Fail("执行测试前没有选中的工位，无法初始化源。");
        }

        List<MeterTestSourceControlConfig> enabledConfigs = planConfig.SourceControlConfigs
            .Where(config => config.Enabled)
            .ToList();
        List<string> protocols = enabledConfigs
            .Select(config => config.Protocol?.Trim() ?? string.Empty)
            .Where(protocol => !string.IsNullOrWhiteSpace(protocol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (protocols.Count != 1 || !Enum.TryParse(protocols.SingleOrDefault(), true, out MeterTestSourceProtocol sourceProtocol))
        {
            return MeterTestSourceControlResult.Fail(
                protocols.Count == 0
                    ? "SourceControlConfigs 中没有启用的源控制协议。"
                    : $"执行测试前无法确定唯一源控制协议：{string.Join("、", protocols)}。");
        }

        if (sourceProtocol != MeterTestSourceProtocol.XYCtr)
        {
            return MeterTestSourceControlResult.Fail($"源厂家协议 {sourceProtocol} 尚未接入执行前初始化流程。");
        }

        MeterTestSourceControlConfig phaseReferenceConfig = enabledConfigs[0];
        if (!TryResolvePhaseMode(
                phaseReferenceConfig,
                selectedStations,
                meterArchives,
                out MeterTestSourcePhaseMode phaseMode,
                out string phaseNote,
                out string? phaseError))
        {
            return MeterTestSourceControlResult.Fail(phaseError ?? "执行测试前无法识别电表相制。");
        }

        List<MeterTestSourceControlConfig> phaseConfigs = enabledConfigs
            .Where(config => string.Equals(config.Protocol?.Trim(), sourceProtocol.ToString(), StringComparison.OrdinalIgnoreCase))
            .Where(config => Enum.TryParse(config.PhaseMode, true, out MeterTestSourcePhaseMode configuredPhase)
                && configuredPhase == phaseMode)
            .ToList();
        if (phaseConfigs.Count == 0)
        {
            return MeterTestSourceControlResult.Fail(
                $"没有找到与资产信息相符的源控制配置：相制={phaseMode}。");
        }

        // 同一相制只允许一个配置作为本轮 Ini 的来源，避免初始化参数不确定。
        if (phaseConfigs.Count > 1)
        {
            return MeterTestSourceControlResult.Fail(
                $"相制={phaseMode} 存在多个启用源控制配置：{string.Join("、", phaseConfigs.Select(config => config.Name))}。");
        }

        MeterTestSourceControlConfig sourceConfig = phaseConfigs[0];
        if (!TryResolveSourceVoltage(
                sourceConfig,
                selectedStations,
                meterArchives,
                out string sourceVoltage,
                out string voltageNote,
                out string? voltageError))
        {
            return MeterTestSourceControlResult.Fail(voltageError ?? "执行测试前无法识别初始化电压。");
        }

        if (!TryBuildMeterInitCommand(
                selectedStations,
                meterArchives,
                phaseMode,
                sourceVoltage,
                sourceCurrentOverride: null,
                MeterTestErrorEnergyType.Active,
                out string initCommand,
                out string initNote,
                out string? initError))
        {
            return MeterTestSourceControlResult.Fail(initError ?? "执行前初始化电表参数失败。");
        }

        bool canReuseInitialization =
            Volatile.Read(ref runSourceInitialized) != 0 &&
            XYCtr.IsSourcePortOpen &&
            runInitializedSourcePort == sourceConfig.SourcePort &&
            runInitializedPhaseMode == phaseMode &&
            string.Equals(runInitializedCommand, initCommand, StringComparison.OrdinalIgnoreCase);
        if (canReuseInitialization)
        {
            // 配置名称可能调整，但物理串口、相制和完整 Ini 参数未变化时无需重复初始化。
            runInitializedSourceConfigName = sourceConfig.Name;
            string reusedMessage =
                $"源初始化参数未变化，复用已成功结果：配置={sourceConfig.Name}，"
                + $"COM{sourceConfig.SourcePort}/{phaseMode}，Ini={initCommand}；跳过重复初始化。";
            LogMessage.Debug($"[源控制][执行前初始化] {reusedMessage}");
            ReportProgress(progressLogger, reusedMessage);
            return MeterTestSourceControlResult.Ok(reusedMessage);
        }

        if (Volatile.Read(ref runSourceInitialized) != 0)
        {
            string changedMessage =
                $"源初始化参数发生变化，准备重新初始化：原配置={runInitializedSourceConfigName}，"
                + $"原COM={runInitializedSourcePort}，原Ini={runInitializedCommand}；"
                + $"新配置={sourceConfig.Name}，新COM={sourceConfig.SourcePort}，新Ini={initCommand}。";
            LogMessage.Debug($"[源控制][执行前初始化] {changedMessage}");
            ReportProgress(progressLogger, changedMessage);
        }

        // 从此处开始会真正调用原生初始化接口；失败时不得继续复用旧参数的成功状态。
        Volatile.Write(ref runSourceInitialized, 0);

        await StopStandardMeterMonitorAsync().ConfigureAwait(false);
        try
        {
            using XYCtr xyCtr = new();
            if (sourceConfig.SourcePort <= 0)
            {
                return MeterTestSourceControlResult.Fail(
                    $"源控制配置 {sourceConfig.Name} 未配置有效串口号：{sourceConfig.SourcePort}。");
            }

            if (!XYCtr.IsSourcePortOpen)
            {
                ReportProgress(progressLogger, $"[执行前初始化] 准备打开源串口：COM{sourceConfig.SourcePort}。");
                LogMessage.Debug(
                    $"[源控制][执行前初始化] 准备打开源串口：配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}。");

                // 旧版 xyctr.dll 传入不存在的 COM 口可能直接退出进程，调用 DLL 前先检查端口。
                if (!TryFindSourcePort(sourceConfig.SourcePort, out string sourcePortName, out string? portError))
                {
                    string failureMessage = portError ?? $"源串口 COM{sourceConfig.SourcePort} 不存在。";
                    LogMessage.Error($"[源控制][执行前初始化] {failureMessage}", null);
                    return MeterTestSourceControlResult.Fail(failureMessage);
                }

                (bool Success, int Result) openResult = await xyCtr
                    .CallOpenCommAsync(sourceConfig.SourcePort, MeterTestSourceControlDefaults.OperationTimeout)
                    .ConfigureAwait(false);
                LogMessage.Debug(
                    $"[源控制接口][XYCtr.OpenComm] 调用返回：COM={sourceConfig.SourcePort}，"
                    + $"成功={openResult.Success}，返回值={openResult.Result}。"
                );
                if (!openResult.Success)
                {
                    string failureMessage =
                        $"执行前打开源串口失败：串口={sourcePortName}，返回值={openResult.Result}。";
                    LogMessage.Error($"[源控制][执行前初始化] {failureMessage}", null);
                    return MeterTestSourceControlResult.Fail(failureMessage);
                }

                ReportProgress(progressLogger, $"[执行前初始化] 打开源串口成功：{sourcePortName}。");
                LogMessage.Info(
                    $"[源控制][执行前初始化] 打开源串口成功：{sourcePortName}，返回值={openResult.Result}。");
            }
            else
            {
                ReportProgress(progressLogger, $"[执行前初始化] 源串口 COM{sourceConfig.SourcePort} 已打开，跳过重复打开。");
                LogMessage.Debug(
                    $"[源控制][执行前初始化] 源串口已打开，跳过重复打开：COM{sourceConfig.SourcePort}。");
            }

            await DelayBetweenSourceStepsAsync("打开串口", "初始化电表参数", cancellationToken).ConfigureAwait(false);

            ReportProgress(progressLogger, $"[执行前初始化] 发送一次 Ini：{initCommand}。");
            LogMessage.Debug(
                $"[源控制][执行前初始化] 准备初始化电表参数：配置={sourceConfig.Name}，{initNote}，command={initCommand}。");
            (bool Success, int Result) initResult = await xyCtr
                .CallSendCommandAsync(initCommand, true, MeterTestSourceControlDefaults.OperationTimeout)
                .ConfigureAwait(false);
            LogMessage.Debug(
                $"[源控制接口][XYCtr.SendCommand] Ini调用返回：配置={sourceConfig.Name}，"
                + $"参数={initCommand}，同步=true，成功={initResult.Success}，返回值={initResult.Result}。"
            );
            if (!initResult.Success)
            {
                string failureMessage =
                    $"执行前初始化电表参数失败：配置={sourceConfig.Name}，参数={initCommand}，返回值={initResult.Result}。";
                LogMessage.Error($"[源控制][执行前初始化] {failureMessage}", null);
                return MeterTestSourceControlResult.Fail(failureMessage);
            }

            runInitializedSourcePort = sourceConfig.SourcePort;
            runInitializedPhaseMode = phaseMode;
            runInitializedSourceConfigName = sourceConfig.Name;
            runInitializedCommand = initCommand;
            Volatile.Write(ref runSourceInitialized, 1);

            string successMessage =
                $"执行前源初始化成功：配置={sourceConfig.Name}，相制={phaseMode}，{phaseNote}，{voltageNote}，"
                + $"Ini={initCommand}，返回值={initResult.Result}；当前程序运行期间参数未变化时不再重复初始化。";
            ReportProgress(progressLogger, successMessage);
            LogMessage.Info($"[源控制][执行前初始化] {successMessage}");
            await DelayBetweenSourceStepsAsync("初始化电表参数", "执行测试", cancellationToken).ConfigureAwait(false);
            return MeterTestSourceControlResult.Ok(successMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Volatile.Write(ref runSourceInitialized, 0);
            string failureMessage = $"执行前源初始化异常：{ex.Message}";
            LogMessage.Error("[源控制][执行前初始化] " + failureMessage, ex);
            return MeterTestSourceControlResult.Fail(failureMessage);
        }
    }

    /// <summary>
    /// 根据 SourceControlConfigs 中启用配置的 protocol 路由手动降源驱动。
    /// 同一台源可以有单相、三相等多套参数配置，但它们的厂家协议必须一致。
    /// </summary>
    public async Task<MeterTestSourceControlResult> ShutDownFromConfigurationAsync(
        MeterTestPlanConfig planConfig,
        CancellationToken cancellationToken,
        Action<string>? progressLogger = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        List<MeterTestSourceControlConfig> enabledSourceConfigs = planConfig.SourceControlConfigs
            .Where(config => config.Enabled)
            .ToList();
        List<string> protocols = enabledSourceConfigs
            .Select(config => config.Protocol?.Trim() ?? string.Empty)
            .Where(protocol => !string.IsNullOrWhiteSpace(protocol))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (protocols.Count == 0)
        {
            return MeterTestSourceControlResult.Fail(
                "SourceControlConfigs 中没有启用且配置 protocol 的源控制项。");
        }

        if (protocols.Count > 1)
        {
            return MeterTestSourceControlResult.Fail(
                $"启用的源配置包含多个厂家协议：{string.Join("、", protocols)}，无法确定手动降源驱动。");
        }

        string protocol = protocols[0];
        if (!Enum.TryParse(protocol, true, out MeterTestSourceProtocol sourceProtocol))
        {
            return MeterTestSourceControlResult.Fail($"源厂家协议 protocol={protocol} 尚未接入降源驱动。");
        }

        List<MeterTestSourceControlConfig> protocolConfigs = enabledSourceConfigs
            .Where(config => string.Equals(config.Protocol?.Trim(), protocol, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (!MeterTestSourceShutdownGuard.TryResolveSettings(
                protocolConfigs,
                out int sourcePort,
                out int shutMode,
                out string settingsError))
        {
            return MeterTestSourceControlResult.Fail(settingsError);
        }

        ReportProgress(progressLogger, $"手动降源配置：协议={sourceProtocol}，串口=COM{sourcePort}，模式={shutMode}。");
        LogMessage.Debug(
            $"[源控制][手动降源] 根据 SourceControlConfigs 选择 "
            + $"protocol={sourceProtocol}，sourcePort={sourcePort}，shutMode={shutMode}。");
        await StopStandardMeterMonitorAsync().ConfigureAwait(false);
        try
        {
            return sourceProtocol switch
            {
                MeterTestSourceProtocol.XYCtr => await ShutDownXyCtrAsync(
                    sourcePort,
                    shutMode,
                    cancellationToken,
                    progressLogger).ConfigureAwait(false),
                _ => MeterTestSourceControlResult.Fail($"源厂家协议 {sourceProtocol} 尚未接入降源驱动。")
            };
        }
        finally
        {
            // 降源后继续采集标准表，便于界面观察电压电流回零过程。
            if (XYCtr.IsSourcePortOpen && !disposed)
            {
                StartStandardMeterMonitor();
            }
        }
    }

    /// <summary>
    /// 调用新跃 XYCtr 驱动降源。串口未打开时先按配置安全打开，再下发降源命令。
    /// </summary>
    private async Task<MeterTestSourceControlResult> ShutDownXyCtrAsync(
        int sourcePort,
        int shutMode,
        CancellationToken cancellationToken,
        Action<string>? progressLogger)
    {
        try
        {
            using XYCtr xyCtr = new();
            if (!XYCtr.IsSourcePortOpen)
            {
                ReportProgress(progressLogger, $"源串口未打开，准备打开 COM{sourcePort}。");
                LogMessage.Debug($"[源控制][手动降源] 准备打开源串口：COM{sourcePort}。");

                // 旧版 xyctr.dll 收到不存在的 COM 口时可能退出进程，调用前必须先检查系统端口。
                if (!TryFindSourcePort(sourcePort, out string sourcePortName, out string? portError))
                {
                    string failureMessage = portError ?? $"源串口 COM{sourcePort} 不存在。";
                    ReportProgress(progressLogger, failureMessage);
                    LogMessage.Error($"[源控制][手动降源] {failureMessage}", null);
                    return MeterTestSourceControlResult.Fail(failureMessage);
                }

                cancellationToken.ThrowIfCancellationRequested();
                (bool Success, int Result) openResult = await xyCtr
                    .CallOpenCommAsync(sourcePort, MeterTestSourceControlDefaults.OperationTimeout)
                    .ConfigureAwait(false);

                // OpenComm立即失败时，可能是DLL内部残留了上一次串口句柄。
                // 先在同一STA队列关闭残留句柄，再按SourceControlConfigs中的端口重开一次。
                if (!openResult.Success &&
                    openResult.Result is not XYCtr.TimeoutResult and not XYCtr.BusyResult)
                {
                    ReportProgress(
                        progressLogger,
                        $"首次打开 {sourcePortName} 失败，返回值={openResult.Result}；准备关闭残留句柄后重试一次。");
                    LogMessage.Debug(
                        $"[源控制][手动降源] 首次OpenComm失败：配置端口={sourcePortName}，"
                        + $"返回值={openResult.Result}；开始清理DLL串口句柄。");
                    (bool Success, int Result) closeResult = await xyCtr
                        .CallCloseCommAsync(MeterTestSourceControlDefaults.OperationTimeout)
                        .ConfigureAwait(false);
                    LogMessage.Debug(
                        $"[源控制][手动降源] CloseComm恢复调用完成：成功={closeResult.Success}，"
                        + $"返回值={closeResult.Result}。");
                    await Task.Delay(SourceStepInterval, cancellationToken).ConfigureAwait(false);
                    openResult = await xyCtr
                        .CallOpenCommAsync(sourcePort, MeterTestSourceControlDefaults.OperationTimeout)
                        .ConfigureAwait(false);
                }

                if (!openResult.Success)
                {
                    string failureMessage =
                        $"打开源串口失败：配置来源=SourceControlConfigs，sourcePort={sourcePort}，"
                        + $"串口={sourcePortName}，XYCtr.OpenComm最终返回值={openResult.Result}。";
                    ReportProgress(progressLogger, failureMessage);
                    LogMessage.Error($"[源控制][手动降源] {failureMessage}", null);
                    return MeterTestSourceControlResult.Fail(failureMessage);
                }

                ReportProgress(progressLogger, $"打开源串口成功：{sourcePortName}，返回值={openResult.Result}。");
                LogMessage.Info(
                    $"[源控制][手动降源] 打开源串口成功：{sourcePortName}，返回值={openResult.Result}。");
                await DelayBetweenSourceStepsAsync("打开串口", "降源", cancellationToken).ConfigureAwait(false);
            }
            else
            {
                ReportProgress(progressLogger, $"源串口 COM{sourcePort} 已打开，跳过重复打开。");
                LogMessage.Debug($"[源控制][手动降源] 源串口已打开，跳过重复打开：COM{sourcePort}。");
            }

            if (!MeterTestSourceShutdownGuard.CanInvoke(XYCtr.IsSourcePortOpen))
            {
                const string portMessage = MeterTestSourceShutdownGuard.PortUnavailableMessage;
                ReportProgress(progressLogger, portMessage);
                LogMessage.Error($"[源控制][手动降源] {portMessage}", null);
                return MeterTestSourceControlResult.Fail(portMessage);
            }

            ReportProgress(progressLogger, $"准备调用 XYCtr.ShutPowerSource({shutMode})。");
            cancellationToken.ThrowIfCancellationRequested();
            (bool Success, int Result) result = await xyCtr
                .CallShutPowerSourceAsync(shutMode, MeterTestSourceControlDefaults.OperationTimeout)
                .ConfigureAwait(false);

            // 降源是幂等安全指令；原生接口立即返回失败时等待1秒再补发一次。
            // 超时或队列忙表示原调用仍可能在执行，不能追加重复命令。
            if (!result.Success && result.Result is not XYCtr.TimeoutResult and not XYCtr.BusyResult)
            {
                LogMessage.Debug(
                    $"[源控制][手动降源] 首次调用失败，返回值={result.Result}，等待1秒后重试一次。");
                ReportProgress(progressLogger, $"首次降源失败，返回值={result.Result}，准备重试一次。");
                await Task.Delay(SourceStepInterval, cancellationToken).ConfigureAwait(false);
                result = await xyCtr
                    .CallShutPowerSourceAsync(shutMode, MeterTestSourceControlDefaults.OperationTimeout)
                    .ConfigureAwait(false);
            }

            if (!result.Success)
            {
                string failureMessage =
                    $"XYCtr.ShutPowerSource({shutMode})降源失败，串口=COM{sourcePort}，返回值={result.Result}。";
                LogMessage.Error($"[源控制][手动降源] {failureMessage}", null);
                return MeterTestSourceControlResult.Fail(failureMessage);
            }

            Volatile.Write(ref sourceOutputActive, 0);
            string successMessage =
                $"XYCtr.ShutPowerSource({shutMode})降源成功，串口=COM{sourcePort}，返回值={result.Result}。";
            ReportProgress(progressLogger, successMessage);
            LogMessage.Debug($"[源控制][手动降源] {successMessage}");
            return MeterTestSourceControlResult.Ok(successMessage);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            string failureMessage = $"调用 XYCtr.ShutPowerSource({shutMode}) 异常：{ex.Message}";
            LogMessage.Error($"[源控制][手动降源] {failureMessage}", ex);
            return MeterTestSourceControlResult.Fail(failureMessage);
        }
    }

    /// <summary>
    /// 源控制统一批次入口。同一个批次键无论被多少工位或调用方请求，都只执行一次初始化和升源。
    /// </summary>
    /// <param name="batchKey">当前运行内唯一的测试小项批次键。</param>
    /// <param name="planConfig">当前测试方案配置。</param>
    /// <param name="subItem">当前测试小项。</param>
    /// <param name="selectedStations">当前勾选的工位。</param>
    /// <param name="meterArchives">工位对应的资产档案，用于判断单相或三相。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>源控制执行结果。</returns>
    public Task<MeterTestSourceControlResult> ExecuteBatchOnceAsync(
        string batchKey,
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        CancellationToken cancellationToken,
        Action<string>? progressLogger = null)
    {
        if (string.IsNullOrWhiteSpace(batchKey))
        {
            throw new ArgumentException("源控制批次键不能为空。", nameof(batchKey));
        }

        Lazy<Task<MeterTestSourceControlResult>> candidate = new(
            () => ExecuteCoreAsync(
                planConfig,
                subItem,
                selectedStations,
                meterArchives,
                cancellationToken,
                progressLogger),
            LazyThreadSafetyMode.ExecutionAndPublication);
        Lazy<Task<MeterTestSourceControlResult>> batch = sourceExecutionBatches.GetOrAdd(batchKey, candidate);
        if (ReferenceEquals(batch, candidate))
        {
            LogMessage.Debug(
                $"[源控制批次] 创建唯一任务：小项={subItem.Name}，工位数={selectedStations.Count}，"
                + "执行前初始化已完成，当前小项只执行一次升源接口。");
        }
        else
        {
            LogMessage.Debug(
                $"[源控制批次] 复用已有任务：小项={subItem.Name}，不重复初始化、不重复升源。");
        }

        return batch.Value;
    }

    /// <summary>执行一次真实的源控制流程，只允许由批次入口创建。</summary>
    private async Task<MeterTestSourceControlResult> ExecuteCoreAsync(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        CancellationToken cancellationToken,
        Action<string>? progressLogger)
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

            // 只要已经尝试下发升源，线路就可能带电，即使DLL返回失败也先保守标记为“已升源”。
            // 后续标准表复核负责给出测试结论，0x86短路检测则始终按最安全状态拦截。
            if (state.SourceOutputActiveState.HasValue)
            {
                Volatile.Write(ref sourceOutputActive, state.SourceOutputActiveState.Value ? 1 : 0);
            }

            if (!state.ShouldVerify)
            {
                return state.Result;
            }

            if (!state.Result.Success)
            {
                string fallbackMessage =
                    $"升源接口返回失败，但指令已经尝试下发；开始{Math.Max(1, state.VerificationTimeoutSeconds)}秒标准表电压电流复核，"
                    + "以标准表实测值作为最终结论。";
                LogMessage.Debug($"[源控制] {fallbackMessage}");
                ReportProgress(progressLogger, fallbackMessage);
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
    /// 在短路检测前确认台体无压。检测到程序已升源或最近标准表电压超限时，
    /// 自动调用ShutPowerSource(0)同时降电压和电流，并在20秒内复核Ua/Ub/Uc。
    /// </summary>
    public async Task<MeterTestSourceSafetyResult> EnsureDeEnergizedAsync(
        decimal maximumSafeVoltage,
        CancellationToken cancellationToken,
        Action<string>? progressLogger = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        decimal safeVoltage = maximumSafeVoltage > 0 ? maximumSafeVoltage : 5m;
        IReadOnlyDictionary<string, string>? currentValues = LatestStandardValues;
        bool voltageUnsafe = TryDescribeUnsafeVoltage(currentValues, safeVoltage, out string voltageDetail);
        if (!IsSourceOutputActive && !voltageUnsafe)
        {
            string safeMessage = currentValues is null
                ? "程序未记录升源状态，且当前没有可用标准表采样；继续执行人工无压确认。"
                : $"最近标准表采样处于安全范围：{voltageDetail}";
            ReportProgress(progressLogger, safeMessage);
            return MeterTestSourceSafetyResult.Ok(safeMessage, currentValues);
        }

        await StopStandardMeterMonitorAsync().ConfigureAwait(false);
        try
        {
            string reason = IsSourceOutputActive
                ? $"程序记录源输出处于开启状态；{voltageDetail}"
                : $"标准表检测到电压超限；{voltageDetail}";
            ReportProgress(progressLogger, $"{reason}，准备调用ShutPowerSource(0)同时降电压和电流。");
            LogMessage.Debug($"[设备自检] {reason}，准备调用ShutPowerSource(0)。");

            using XYCtr xyCtr = new();
            LogMessage.Debug(
                $"[源控制接口][XYCtr.ShutPowerSource] 设备自检降源调用开始："
                + $"ShutMode=0，超时由设备接口控制。"
            );
            (bool Success, int Result) shutResult = await Task.Run(
                () => xyCtr.CallShutPowerSource(0),
                cancellationToken).ConfigureAwait(false);
            LogMessage.Debug(
                $"[源控制接口][XYCtr.ShutPowerSource] 设备自检降源调用返回："
                + $"ShutMode=0，成功={shutResult.Success}，返回值={shutResult.Result}。"
            );
            if (!shutResult.Success)
            {
                string message = $"ShutPowerSource(0)降源失败，返回值={shutResult.Result}，禁止执行短路检测。";
                LogMessage.Error($"[设备自检] {message}", null);
                return MeterTestSourceSafetyResult.Fail(message, currentValues);
            }

            Volatile.Write(ref sourceOutputActive, 0);
            ReportProgress(progressLogger, $"ShutPowerSource(0)下发成功，返回值={shutResult.Result}，开始复核三相电压。");

            if (!XYCtr.IsSourcePortOpen)
            {
                string message = "降源命令已成功，但源串口未打开，无法读取标准表复核电压，禁止自动执行短路检测。";
                LogMessage.Error($"[设备自检] {message}", null);
                return MeterTestSourceSafetyResult.Fail(message, currentValues);
            }

            DateTime deadline = DateTime.UtcNow.AddSeconds(20);
            string lastDetail = voltageDetail;
            while (DateTime.UtcNow < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();
                StandardMeterReadResult readResult = await ReadStandardMeterAsync(
                    xyCtr,
                    TimeSpan.FromSeconds(5),
                    cancellationToken).ConfigureAwait(false);
                if (readResult.Success && readResult.StandValues is not null)
                {
                    currentValues = readResult.StandValues;
                    PublishStandardValues(currentValues);
                    bool stillUnsafe = TryDescribeUnsafeVoltage(currentValues, safeVoltage, out lastDetail);
                    ReportProgress(progressLogger, $"降源电压复核：{lastDetail}");
                    if (!stillUnsafe)
                    {
                        string message = $"降源完成，Ua/Ub/Uc均不高于{safeVoltage:0.###}V。";
                        LogMessage.Debug($"[设备自检] {message}");
                        return MeterTestSourceSafetyResult.Ok(message, currentValues);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
            }

            string timeoutMessage = $"ShutPowerSource(0)已下发，但20秒内电压未降至{safeVoltage:0.###}V以内：{lastDetail}，禁止执行短路检测。";
            LogMessage.Error($"[设备自检] {timeoutMessage}", null);
            return MeterTestSourceSafetyResult.Fail(timeoutMessage, currentValues);
        }
        finally
        {
            if (XYCtr.IsSourcePortOpen && !disposed)
            {
                StartStandardMeterMonitor();
            }
        }
    }

    /// <summary>检查最近标准表Ua/Ub/Uc是否超过短路检测允许的安全电压。</summary>
    private static bool TryDescribeUnsafeVoltage(
        IReadOnlyDictionary<string, string>? standValues,
        decimal maximumSafeVoltage,
        out string detail)
    {
        if (standValues is null)
        {
            detail = "无标准表采样";
            return false;
        }

        string[] phaseNames = { "Ua", "Ub", "Uc" };
        List<string> values = new();
        bool unsafeVoltage = false;
        foreach (string phaseName in phaseNames)
        {
            string rawValue = standValues.TryGetValue(phaseName, out string? value) ? value : string.Empty;
            if (!TryParseNumber(rawValue, out decimal parsedValue))
            {
                values.Add($"{phaseName}=无法解析({rawValue})");
                unsafeVoltage = true;
                continue;
            }

            values.Add($"{phaseName}={parsedValue:0.####}V");
            unsafeVoltage |= Math.Abs(parsedValue) > maximumSafeVoltage;
        }

        detail = string.Join("，", values);
        return unsafeVoltage;
    }

    /// <summary>
    /// 执行源控制的同步核心流程。
    /// </summary>
    private SourceControlExecutionState ExecuteCore(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        XYCtr xyCtr,
        CancellationToken cancellationToken,
        Action<string>? progressLogger)
    {
        MeterTestSourceControlConfig? sourceConfig = ResolveSourceControlConfig(planConfig, subItem);

        // 基本误差方案历史配置统一写的是“单相默认源”，但实际测试可能是三相资产。
        // 基本误差必须复用本轮执行前真正完成Ini的源配置，避免在升源前因配置不一致直接退出。
        if (IsBasicErrorPointExecution(subItem) && Volatile.Read(ref runSourceInitialized) != 0)
        {
            MeterTestSourceControlConfig? initializedConfig = planConfig.SourceControlConfigs.FirstOrDefault(
                config => config.Enabled &&
                    string.Equals(config.Name, runInitializedSourceConfigName, StringComparison.OrdinalIgnoreCase));
            if (initializedConfig is not null)
            {
                sourceConfig = initializedConfig;
                LogMessage.Debug(
                    $"[源控制][基本误差] 测试点={subItem.Name} 复用本轮初始化源配置："
                    + $"{initializedConfig.Name}, COM{initializedConfig.SourcePort}, phaseMode={runInitializedPhaseMode}。");
                ReportProgress(
                    progressLogger,
                    $"基本误差复用本轮初始化源配置：{initializedConfig.Name}, COM{initializedConfig.SourcePort}。");
            }
        }

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
            startingCurrentNote += $"；复用执行方案阶段的源连接参数，升源Ist={sourceCurrentOverride}A";
            if (TryResolveStartingPowerFactorAngle(subItem, databaseService.LoadPowerFactorAngles(), out decimal currentAngle, out string angleNote))
            {
                sourceCurrentAngleOverride = NormalizeNumericText(currentAngle.ToString(CultureInfo.InvariantCulture));
                startingCurrentNote += $"；{angleNote}";
                LogMessage.Debug($"[源控制] 起动误差点相角计算完成：{angleNote}");
            }
        }
        else if (IsBasicErrorPointExecution(subItem))
        {
            if (!MeterTestBasicErrorCalculator.TryCreateExecutionPlan(
                    subItem,
                    selectedStations,
                    meterArchives,
                    databaseService.LoadPowerFactorAngles(),
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
            startingCurrentNote =
                $"基本误差点={basicErrorPlan.TestPointName}，方向={basicErrorPlan.Direction}，"
                + $"相别={basicErrorPlan.Phase}，功率因数={basicErrorPlan.PowerFactorText}，"
                + $"FA角度={basicErrorPlan.CurrentAngle:0.######}°（数据库），"
                + "复用执行方案阶段的源连接参数，"
                + $"AnyUIOutput电压={basicErrorPlan.SourcePhaseVoltage:0.######}V，"
                + $"AnyUIOutput电流={basicErrorPlan.SourceCurrent:0.#########}A，"
                + $"相对基本电流={basicErrorPlan.SourceCurrent:0.#########}/{basicErrorPlan.BasicCurrent:0.#########}×100"
                + $"={basicErrorPlan.CurrentPercentage:0.#########}%";
            LogMessage.Debug($"[源控制] 基本误差升源参数计算完成：{startingCurrentNote}");
        }
        else if (IsConstantImaxSourceExecution(subItem))
        {
            if (!TryResolveConstantImaxCurrent(
                    selectedStations,
                    meterArchives,
                    out sourceCurrentOverride,
                    out startingCurrentNote,
                    out string? imaxError))
            {
                LogMessage.Error($"[源控制] 常数试验Imax电流计算失败：{imaxError}", null);
                return SourceControlExecutionState.Fail(imaxError ?? "常数试验Imax电流计算失败。");
            }

            LogMessage.Debug($"[源控制] 常数试验Imax升源参数计算完成：{startingCurrentNote}");
        }
        else if (IsConstantVoltageSourceExecution(subItem))
        {
            sourceCurrentOverride = "0";
            startingCurrentNote = "常数试验结束阶段仅保持基础电压，电流降为0A；标准表复核只校验电压。";
            LogMessage.Debug($"[源控制] {startingCurrentNote}");
        }

        if (!TryResolvePhaseMode(sourceConfig, selectedStations, meterArchives, out MeterTestSourcePhaseMode phaseMode, out string phaseNote, out string? errorMessage))
        {
            LogMessage.Error($"[源控制] 配置 {sourceConfig.Name} 电表类型判定失败：{errorMessage}", null);
            return SourceControlExecutionState.Fail(errorMessage ?? "源控制参数解析失败。");
        }

        if (Volatile.Read(ref runSourceInitialized) == 0)
        {
            return SourceControlExecutionState.Fail(
                $"小项 {subItem.Name} 执行前未完成源初始化，已阻止升源。请重新点击执行测试。");
        }

        if (sourceConfig.SourcePort != runInitializedSourcePort || phaseMode != runInitializedPhaseMode)
        {
            return SourceControlExecutionState.Fail(
                $"小项 {subItem.Name} 的源配置与执行前初始化不一致："
                + $"初始化配置={runInitializedSourceConfigName}, COM{runInitializedSourcePort}/{runInitializedPhaseMode}；"
                + $"当前配置={sourceConfig.Name}, COM{sourceConfig.SourcePort}/{phaseMode}。");
        }

        if (!XYCtr.IsSourcePortOpen)
        {
            return SourceControlExecutionState.Fail(
                $"小项 {subItem.Name} 执行时源串口已断开；方案步骤禁止重新初始化，请重新点击执行测试。");
        }

        LogMessage.Debug(
            $"[源控制] 小项={subItem.Name} 使用执行前初始化结果：配置={runInitializedSourceConfigName}，"
            + $"COM{runInitializedSourcePort}/{runInitializedPhaseMode}；当前步骤仅执行升源，不发送初始化命令。");

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

        if (basicErrorPlan is not null &&
            !EnsureBasicErrorEnergyInitialization(
                xyCtr,
                sourceConfig,
                selectedStations,
                meterArchives,
                phaseMode,
                sourceVoltage,
                basicErrorPlan.EnergyType,
                progressLogger,
                out SourceControlExecutionState? initializationFailure))
        {
            return initializationFailure ?? SourceControlExecutionState.Fail("基本误差有功/无功源初始化失败。");
        }

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
            result.Success
                ? $"升源接口下发成功：{result.Message}"
                : $"升源接口返回失败，仍将继续20秒标准表复核：{result.Message}");
        string finalMessage = $"{result.Message}；{phaseNote}；{voltageNote}";
        if (!string.IsNullOrWhiteSpace(startingCurrentNote))
        {
            finalMessage += $"；{startingCurrentNote}";
        }

        // DLL 返回失败不代表物理源一定没有动作；只要已尝试下发，就继续执行20秒标准表复核。
        return SourceControlExecutionState.Executed(
            new MeterTestSourceControlResult(result.Success, finalMessage),
            sourceConfig,
            phaseMode,
            sourceVoltage,
            sourceCurrentOverride,
            basicErrorPlan?.Phase ?? sourceConfig.Phase);
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
        // 基本误差点使用资产计算出的绝对电压、电流和相角，避免 Adj 百分比在大电流点超出范围。
        if (basicErrorPlan is not null)
        {
            return ExecuteBasicErrorAnyUiOutput(xyCtr, config, basicErrorPlan);
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
    /// 使用 AnyUIOutput 接口输出基本误差测试点。
    /// 命令直接携带绝对电压、电流和电流相角，不受 Adj 电流百分比上限影响。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteBasicErrorAnyUiOutput(
        XYCtr xyCtr,
        MeterTestSourceControlConfig config,
        MeterTestBasicErrorExecutionPlan plan)
    {
        string voltage = NormalizeNumericText(plan.SourcePhaseVoltage.ToString(CultureInfo.InvariantCulture));
        string current = NormalizeNumericText(plan.SourceCurrent.ToString(CultureInfo.InvariantCulture));
        string currentAngle = NormalizeNumericText(plan.CurrentAngle.ToString(CultureInfo.InvariantCulture));
        string zero = "0";

        string ua;
        string ub;
        string uc;
        string ia;
        string ib;
        string ic;
        string angleA;
        string angleB;
        string angleC;
        if (plan.PhaseMode == MeterTestSourcePhaseMode.SinglePhase)
        {
            ua = voltage;
            ub = zero;
            uc = zero;
            ia = current;
            ib = zero;
            ic = zero;
            angleA = currentAngle;
            angleB = zero;
            angleC = zero;
        }
        else
        {
            ua = voltage;
            ub = voltage;
            uc = voltage;
            bool outputA = plan.Phase is "H" or "A";
            bool outputB = plan.Phase is "H" or "B";
            bool outputC = plan.Phase is "H" or "C";
            ia = outputA ? current : zero;
            ib = outputB ? current : zero;
            ic = outputC ? current : zero;
            angleA = outputA ? currentAngle : zero;
            angleB = outputB ? currentAngle : zero;
            angleC = outputC ? currentAngle : zero;
        }

        string command = string.Join(
            "_",
            ua,
            ub,
            uc,
            ia,
            ib,
            ic,
            angleA,
            angleB,
            angleC,
            Normalize(config.Uab, "120"),
            Normalize(config.Uac, "240"));
        LogMessage.Debug(
            $"[源控制] 基本误差 AnyUIOutput 下发：测试点={plan.TestPointName}，方向={plan.Direction}，"
            + $"相别={plan.Phase}，功率因数={plan.PowerFactorText}，电流相角={currentAngle}°，"
            + $"电压={voltage}V，电流={current}A，"
            + $"command={command}");

        (bool success, int result) = xyCtr
            .CallAnyUIOutputAsync(command, config.Pulse, MeterTestSourceControlDefaults.OperationTimeout)
            .GetAwaiter()
            .GetResult();
        LogMessage.Debug(
            $"[源控制接口][XYCtr.AnyUIOutput] 调用返回：测试点={plan.TestPointName}，"
            + $"参数={command}，Pulse={config.Pulse}，成功={success}，返回值={result}。"
        );
        return success
            ? MeterTestSourceControlResult.Ok(
                $"升源成功：测试点={plan.TestPointName}，接口=AnyUIOutput，参数={command}，Pulse={config.Pulse}，返回值={result}")
            : MeterTestSourceControlResult.Fail(
                $"升源失败：测试点={plan.TestPointName}，接口=AnyUIOutput，参数={command}，Pulse={config.Pulse}，返回值={result}");
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
            .CallAnyUIOutputAsync(command, config.Pulse, MeterTestSourceControlDefaults.OperationTimeout)
            .GetAwaiter()
            .GetResult();
        LogMessage.Debug(
            $"[源控制接口][XYCtr.AnyUIOutput] 调用返回：配置={config.Name}，"
            + $"参数={command}，Pulse={config.Pulse}，成功={success}，返回值={result}。"
        );
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
            .CallSendCommandAsync(command, true, MeterTestSourceControlDefaults.OperationTimeout)
            .GetAwaiter()
            .GetResult();
        LogMessage.Debug(
            $"[源控制接口][XYCtr.SendCommand] Adj调用返回：配置={config.Name}，"
            + $"参数={command}，同步=true，成功={success}，返回值={result}。"
        );
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
        (bool success, int result) = xyCtr
            .CallRangeOutputUIAsync(command, MeterTestSourceControlDefaults.OperationTimeout)
            .GetAwaiter()
            .GetResult();
        LogMessage.Debug(
            $"[源控制接口][XYCtr.RangeOutputUI] 调用返回：配置={config.Name}，"
            + $"参数={command}，成功={success}，返回值={result}。"
        );
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
        (bool success, int result) = xyCtr
            .CallShutPowerSourceAsync(config.ShutMode, MeterTestSourceControlDefaults.OperationTimeout)
            .GetAwaiter()
            .GetResult();
        LogMessage.Debug(
            $"[源控制接口][XYCtr.ShutPowerSource] 调用返回：配置={config.Name}，"
            + $"ShutMode={config.ShutMode}，成功={success}，返回值={result}。"
        );
        return success
            ? MeterTestSourceControlResult.Ok($"降源成功：配置={config.Name}，接口=ShutPowerSource，ShutMode={config.ShutMode}，返回值={result}")
            : MeterTestSourceControlResult.Fail($"降源失败：配置={config.Name}，接口=ShutPowerSource，ShutMode={config.ShutMode}，返回值={result}");
    }

    /// <summary>
    /// 升源指令下发后，每隔配置的采样周期读取一次标准表。
    /// 即使DLL返回失败也必须执行本流程，以标准表实际电压、电流作为最终升源结论。
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
                string commandStatus = state.Result.Success
                    ? state.Result.Message
                    : $"升源接口返回失败，但标准表复核确认源实际已升起；原始返回：{state.Result.Message}";
                string successMessage = $"{commandStatus}；升源后在{stopwatch.Elapsed.TotalSeconds:0.0}s内达到正负{tolerancePercent:0.######}%：{lastDetail}。";
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

        LogMessage.Debug(
            $"[源控制接口][XYCtr.CallReadStandValue] 调用开始：Model=model1，"
            + $"缓冲区={standValueBuffer.Length}字节，超时={timeout.TotalMilliseconds:0}ms。"
        );
        (bool success, int result) = await xyCtr
            .CallReadStandValueAsync("model1", standValueBuffer, timeout)
            .ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!success)
        {
            LogMessage.Error(
                $"[源控制接口][XYCtr.CallReadStandValue] 调用失败：Model=model1，返回值={result}。",
                null);
            return StandardMeterReadResult.Fail($"CallReadStandValue失败，返回值={result}");
        }

        string rawStandValue = Encoding.Default.GetString(standValueBuffer).TrimEnd('\0', '\r', '\n', ' ');
        LogMessage.Debug(
            $"[源控制接口][XYCtr.CallReadStandValue] 调用返回：Model=model1，"
            + $"返回值={result}，原始数据={rawStandValue}。"
        );
        List<string> standParts = ModelTool.SplitString(rawStandValue)
            .Select(item => item ?? string.Empty)
            .ToList();
        if (standParts.Count < 15)
        {
            LogMessage.Error(
                $"[源控制接口][XYCtr.CallReadStandValue] 数据解析失败："
                + $"期望15项，实际={standParts.Count}，原始数据={rawStandValue}。",
                null);
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
            ["Ua"] = FormatStandValue(standParts[0]),
            ["Ub"] = FormatStandValue(standParts[1]),
            ["Uc"] = FormatStandValue(standParts[2]),
            ["Ia"] = FormatStandValue(standParts[3]),
            ["Ib"] = FormatStandValue(standParts[4]),
            ["Ic"] = FormatStandValue(standParts[5]),
            ["Φa"] = FormatStandValue(standParts[6]),
            ["Φb"] = FormatStandValue(standParts[7]),
            ["Φc"] = FormatStandValue(standParts[8]),
            ["Pa"] = FormatStandValue(standParts[9]),
            ["Pb"] = FormatStandValue(standParts[10]),
            ["Pc"] = FormatStandValue(standParts[11]),
            ["Qa"] = FormatStandValue(standParts[12]),
            ["Qb"] = FormatStandValue(standParts[13]),
            ["Qc"] = FormatStandValue(standParts[14])
        };

        AddDerivedStandValues(values);

        return values;
    }

    /// <summary>
    /// 把标准表原始数值统一整理为 6 位小数。
    /// </summary>
    private static string FormatStandValue(string value)
    {
        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal numericValue))
        {
            return FormatStandValue(numericValue);
        }

        return value.Trim();
    }

    /// <summary>
    /// 把标准表计算值统一整理为 6 位小数。
    /// </summary>
    private static string FormatStandValue(decimal value)
    {
        return value.ToString("0.000000", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 根据标准表的 P/Q 原始数据计算 Sa/Sb/Sc、PFa/PFb/PFc、ΣP/ΣQ/ΣS。
    /// </summary>
    private static void AddDerivedStandValues(IDictionary<string, string> values)
    {
        decimal pa = ParseStandDecimal(values, "Pa");
        decimal pb = ParseStandDecimal(values, "Pb");
        decimal pc = ParseStandDecimal(values, "Pc");
        decimal qa = ParseStandDecimal(values, "Qa");
        decimal qb = ParseStandDecimal(values, "Qb");
        decimal qc = ParseStandDecimal(values, "Qc");

        decimal sa = CalculateApparentPower(pa, qa);
        decimal sb = CalculateApparentPower(pb, qb);
        decimal sc = CalculateApparentPower(pc, qc);

        values["Sa"] = FormatStandValue(sa);
        values["Sb"] = FormatStandValue(sb);
        values["Sc"] = FormatStandValue(sc);
        values["Pfa"] = FormatStandValue(CalculatePowerFactor(pa, sa));
        values["Pfb"] = FormatStandValue(CalculatePowerFactor(pb, sb));
        values["Pfc"] = FormatStandValue(CalculatePowerFactor(pc, sc));

        decimal sumP = pa + pb + pc;
        decimal sumQ = qa + qb + qc;
        decimal sumS = CalculateApparentPower(sumP, sumQ);

        values["ΣP"] = FormatStandValue(sumP);
        values["ΣQ"] = FormatStandValue(sumQ);
        values["ΣS"] = FormatStandValue(sumS);
    }

    /// <summary>
    /// 从标准表字典里安全解析数值，缺失时按 0 处理。
    /// </summary>
    private static decimal ParseStandDecimal(IDictionary<string, string> values, string key)
    {
        if (values.TryGetValue(key, out string? text)
            && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal numericValue))
        {
            return numericValue;
        }

        return 0m;
    }

    /// <summary>
    /// 计算视在功率 S = sqrt(P^2 + Q^2)。
    /// </summary>
    private static decimal CalculateApparentPower(decimal activePower, decimal reactivePower)
    {
        double p = (double)activePower;
        double q = (double)reactivePower;
        return (decimal)Math.Sqrt(p * p + q * q);
    }

    /// <summary>
    /// 计算功率因数 PF = P / S。
    /// </summary>
    private static decimal CalculatePowerFactor(decimal activePower, decimal apparentPower)
    {
        if (apparentPower == 0m)
        {
            return 0m;
        }

        return activePower / apparentPower;
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
        // 保存一份独立快照，避免事件订阅方或后续采样修改当前安全判断所依据的数据。
        IReadOnlyDictionary<string, string> snapshot = new Dictionary<string, string>(standValues);
        Volatile.Write(ref latestStandardValues, snapshot);
        try
        {
            StandardValuesUpdated?.Invoke(snapshot);
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

        int[] voltagePhaseIndices = state.PhaseMode == MeterTestSourcePhaseMode.SinglePhase
            ? new[] { 0 }
            : new[] { 0, 1, 2 };
        string[] voltageNames = { "Ua", "Ub", "Uc" };
        if (!TryEvaluateMeasurements(
                standParts,
                0,
                voltageNames,
                voltagePhaseIndices,
                targetVoltage,
                tolerancePercent,
                decimalPlaces: 2,
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
                currentDetail = $"目标电流={state.SourceCurrent}A，按电流降为0处理，不参与正负百分比校验。";
                targetCurrent = 0;
            }
            else
            {
                string[] currentNames = { "Ia", "Ib", "Ic" };
                int[] currentPhaseIndices = ResolveCurrentPhaseIndices(state.PhaseMode, state.SourcePhase);
                if (!TryEvaluateMeasurements(
                        standParts,
                        3,
                        currentNames,
                        currentPhaseIndices,
                        targetCurrent,
                        tolerancePercent,
                        decimalPlaces: 4,
                        out currentWithinTolerance,
                        out currentDetail))
                {
                    detail = currentDetail;
                    return false;
                }
            }
        }

        withinTolerance = voltageWithinTolerance && currentWithinTolerance;
        detail = $"{voltageDetail}；{currentDetail}";
        return true;
    }

    /// <summary>返回当前源输出实际参与电流校验的相位索引。</summary>
    private static int[] ResolveCurrentPhaseIndices(
        MeterTestSourcePhaseMode phaseMode,
        string sourcePhase)
    {
        if (phaseMode == MeterTestSourcePhaseMode.SinglePhase)
            return new[] { 0 };

        return sourcePhase.Trim().ToUpperInvariant() switch
        {
            "A" => new[] { 0 },
            "B" => new[] { 1 },
            "C" => new[] { 2 },
            _ => new[] { 0, 1, 2 }
        };
    }

    /// <summary>判断指定相位的一组同目标值相量是否全部进入允许范围，并按业务精度完成范围展示和实际比对。</summary>
    private static bool TryEvaluateMeasurements(
        IReadOnlyList<string> standParts,
        int startIndex,
        IReadOnlyList<string> names,
        IReadOnlyList<int> phaseIndices,
        decimal target,
        decimal tolerancePercent,
        int decimalPlaces,
        out bool withinTolerance,
        out string detail)
    {
        decimal tolerance = target * tolerancePercent / 100m;
        decimal roundedTarget = TruncateMeasurement(target, decimalPlaces);
        decimal lower = TruncateMeasurement(target - tolerance, decimalPlaces);
        decimal upper = TruncateMeasurement(target + tolerance, decimalPlaces);
        List<string> actualValues = new();
        withinTolerance = true;

        foreach (int phaseIndex in phaseIndices)
        {
            string rawValue = standParts[startIndex + phaseIndex];
            if (!TryParseNumber(rawValue, out decimal actual))
            {
                detail = $"标准表{names[phaseIndex]}解析失败：{rawValue}";
                withinTolerance = false;
                return false;
            }

            decimal truncatedActual = TruncateMeasurement(actual, decimalPlaces);
            bool phaseWithinTolerance = truncatedActual >= lower && truncatedActual <= upper;
            withinTolerance &= phaseWithinTolerance;
            actualValues.Add($"{names[phaseIndex]}={FormatMeasurement(truncatedActual, decimalPlaces)}");
        }

        detail = $"目标={FormatMeasurement(roundedTarget, decimalPlaces)}，范围=[{FormatMeasurement(lower, decimalPlaces)},{FormatMeasurement(upper, decimalPlaces)}]，实测{string.Join("、", actualValues)}";
        return true;
    }

    /// <summary>按源验证要求截取标准表数值，电压用2位、电流用4位，不做四舍五入。</summary>
    private static decimal TruncateMeasurement(decimal value, int decimalPlaces)
    {
        decimal factor = (decimal)Math.Pow(10, decimalPlaces);
        return Math.Truncate(value * factor) / factor;
    }

    /// <summary>按固定小数位输出源验证日志，保证日志展示值和判定值一致。</summary>
    private static string FormatMeasurement(decimal value, int decimalPlaces)
    {
        return value.ToString($"F{decimalPlaces}", CultureInfo.InvariantCulture);
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

    /// <summary>
    /// 起动试验收缩为单点后，使用点位里的方向和功率因数维护AnyUIOutput电流相角。
    /// 未配置方向或功率因数时保持旧流程默认0度，兼容原五步起动配置。
    /// </summary>
    private static bool TryResolveStartingPowerFactorAngle(
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestPowerFactorAngleData> powerFactorAngles,
        out decimal currentAngle,
        out string note)
    {
        currentAngle = 0m;
        note = string.Empty;
        string direction = (subItem.BasicErrorDirection ?? string.Empty).Trim();
        string powerFactor = (subItem.BasicErrorPowerFactor ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(direction) || string.IsNullOrWhiteSpace(powerFactor))
            return false;

        string normalizedDirection;
        string directionText;
        if (direction.Equals("ForwardActive", StringComparison.OrdinalIgnoreCase) ||
            direction.Contains("正", StringComparison.Ordinal))
        {
            normalizedDirection = "ForwardActive";
            directionText = "正向有功";
        }
        else if (direction.Equals("ReverseActive", StringComparison.OrdinalIgnoreCase) ||
                 direction.Contains("反", StringComparison.Ordinal))
        {
            normalizedDirection = "ReverseActive";
            directionText = "反向有功";
        }
        else
        {
            return false;
        }

        string normalizedPowerFactor = powerFactor.ToUpperInvariant() == "1"
            ? "1.0"
            : powerFactor.ToUpperInvariant();
        MeterTestPowerFactorAngleData? angleConfiguration = powerFactorAngles.FirstOrDefault(item =>
            string.Equals(item.Direction, normalizedDirection, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.PowerFactor, normalizedPowerFactor, StringComparison.OrdinalIgnoreCase));
        if (angleConfiguration is null || angleConfiguration.CurrentAngle is < -180m or > 180m)
            return false;

        currentAngle = decimal.Round(angleConfiguration.CurrentAngle, 6, MidpointRounding.AwayFromZero);
        note = $"起动点位={directionText}/{normalizedPowerFactor}，FA角度={currentAngle:0.######}°（数据库）";
        return true;
    }

    /// <summary>判断当前小项是否为潜动试验的1.1倍额定电压升源。</summary>
    private static bool IsCreepingSourceExecution(MeterTestSubItem subItem)
    {
        return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
            && executionMode == MeterTestExecutionMode.CreepingSource;
    }

    /// <summary>判断当前小项是否为常数试验的额定电压加Imax电流升源。</summary>
    private static bool IsConstantImaxSourceExecution(MeterTestSubItem subItem)
    {
        return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
            && executionMode == MeterTestExecutionMode.ConstantImaxSource;
    }

    /// <summary>判断当前小项是否为常数试验结束阶段的额定电压保压。</summary>
    private static bool IsConstantVoltageSourceExecution(MeterTestSubItem subItem)
    {
        return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
            && executionMode == MeterTestExecutionMode.ConstantVoltageSource;
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
    /// 常数试验使用资产电流规格中的 Imax 作为升源电流；同一次源控制要求所有工位 Imax 一致。
    /// </summary>
    private static bool TryResolveConstantImaxCurrent(
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        out string imaxCurrent,
        out string note,
        out string? errorMessage)
    {
        imaxCurrent = string.Empty;
        note = string.Empty;
        errorMessage = null;
        List<(int StationNo, decimal Imax, string Specification)> calculatedValues = new();
        foreach (MeterTestStationCommunication station in selectedStations)
        {
            if (!meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
            {
                errorMessage = $"工位{station.StationNo}缺少资产档案，无法计算常数试验Imax。";
                return false;
            }

            if (!MeterTestCurrentSpecificationParser.TryParse(
                    archive.CurrentSpecification,
                    archive.AccessMode,
                    archive.ActiveClass,
                    out MeterTestBasicErrorCurrentSpecification? specification,
                    out string? specificationError))
            {
                errorMessage = $"工位{station.StationNo}{specificationError}";
                return false;
            }

            calculatedValues.Add((station.StationNo, specification!.Imax, archive.CurrentSpecification));
        }

        List<decimal> distinctValues = calculatedValues.Select(item => item.Imax).Distinct().ToList();
        if (distinctValues.Count != 1)
        {
            errorMessage = "选中工位的Imax不一致："
                + string.Join("、", calculatedValues.Select(item => $"工位{item.StationNo}={item.Imax:0.######}A({item.Specification})"))
                + "，同一次常数试验只能下发一个公共Imax电流。";
            return false;
        }

        imaxCurrent = NormalizeNumericText(distinctValues[0].ToString(CultureInfo.InvariantCulture));
        note = $"常数试验Imax={imaxCurrent}A，计算依据："
            + string.Join("；", calculatedValues.Select(item => $"工位{item.StationNo}规格={item.Specification}"));
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
    /// 基本误差在有功/无功测试点之间切换时，需要确保源处于对应的Ini接线方式。
    /// 无功点使用接线方式5；有功点使用原有单相0、三相1。完整Ini命令未变化时直接复用。
    /// </summary>
    private bool EnsureBasicErrorEnergyInitialization(
        XYCtr xyCtr,
        MeterTestSourceControlConfig sourceConfig,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        MeterTestSourcePhaseMode phaseMode,
        string sourceVoltage,
        MeterTestErrorEnergyType energyType,
        Action<string>? progressLogger,
        out SourceControlExecutionState? failure)
    {
        failure = null;
        if (!TryBuildMeterInitCommand(
                selectedStations,
                meterArchives,
                phaseMode,
                sourceVoltage,
                sourceCurrentOverride: null,
                energyType,
                out string desiredInitCommand,
                out string desiredInitNote,
                out string? initError))
        {
            failure = SourceControlExecutionState.Fail(initError ?? "基本误差有功/无功初始化参数生成失败。");
            return false;
        }

        if (Volatile.Read(ref runSourceInitialized) != 0 &&
            string.Equals(runInitializedCommand, desiredInitCommand, StringComparison.OrdinalIgnoreCase))
        {
            LogMessage.Debug($"[源控制][基本误差] 当前源Ini已匹配{energyType}：{desiredInitCommand}，跳过重复初始化。");
            return true;
        }

        string energyText = energyType == MeterTestErrorEnergyType.Reactive ? "无功" : "有功";
        string message = $"基本误差切换到{energyText}点，准备重新发送Ini：{desiredInitCommand}（{desiredInitNote}）。";
        LogMessage.Debug($"[源控制][基本误差] {message}");
        ReportProgress(progressLogger, message);

        (bool success, int result) = xyCtr
            .CallSendCommandAsync(desiredInitCommand, true, MeterTestSourceControlDefaults.OperationTimeout)
            .GetAwaiter()
            .GetResult();
        if (!success)
        {
            string failureMessage = $"基本误差{energyText}Ini初始化失败：参数={desiredInitCommand}，返回值={result}。";
            failure = SourceControlExecutionState.Fail(failureMessage);
            LogMessage.Error($"[源控制][基本误差] {failureMessage}", null);
            return false;
        }

        runInitializedSourcePort = sourceConfig.SourcePort;
        runInitializedPhaseMode = phaseMode;
        runInitializedSourceConfigName = sourceConfig.Name;
        runInitializedCommand = desiredInitCommand;
        Volatile.Write(ref runSourceInitialized, 1);
        LogMessage.Info($"[源控制][基本误差] {energyText}Ini初始化成功：参数={desiredInitCommand}，返回值={result}。");
        return true;
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
        MeterTestErrorEnergyType energyType,
        out string command,
        out string note,
        out string? errorMessage)
    {
        command = string.Empty;
        note = string.Empty;
        errorMessage = null;

        string meterConnection = energyType == MeterTestErrorEnergyType.Reactive
            ? "5"
            : phaseMode == MeterTestSourcePhaseMode.SinglePhase ? "0" : "1";
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
                archive => NormalizeNumericText(Normalize(
                    energyType == MeterTestErrorEnergyType.Reactive ? archive.ReactiveConstant : archive.ActiveConstant)),
                energyType == MeterTestErrorEnergyType.Reactive ? "无功常数" : "有功常数",
                out string activeConstant,
                out errorMessage))
        {
            return false;
        }

        command = $"Ini_{meterConnection}_{meterVoltage}_{current}_{activeConstant}_E";
        note = $"接线方式={meterConnection}，电压代码={meterVoltage}，电流={current}，{(energyType == MeterTestErrorEnergyType.Reactive ? "无功常数" : "有功常数")}={activeConstant}";
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

    /// <summary>短路检测前自动降源及标准表安全复核结果。</summary>
    public sealed record MeterTestSourceSafetyResult(
        bool Success,
        string Message,
        IReadOnlyDictionary<string, string>? StandValues)
    {
        /// <summary>创建短路检测前降源和电压复核成功结果。</summary>
        public static MeterTestSourceSafetyResult Ok(
            string message,
            IReadOnlyDictionary<string, string>? standValues) =>
            new(true, message, standValues);

        /// <summary>创建短路检测前降源或电压复核失败结果。</summary>
        public static MeterTestSourceSafetyResult Fail(
            string message,
            IReadOnlyDictionary<string, string>? standValues) =>
            new(false, message, standValues);
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
        /// <summary>创建标准表读取成功结果并保存十五项原始值及解析值。</summary>
        public static StandardMeterReadResult Ok(
            IReadOnlyList<string> values,
            IReadOnlyDictionary<string, string> standValues,
            string rawValue)
        {
            return new StandardMeterReadResult(true, rawValue, values, standValues);
        }

        /// <summary>创建标准表接口调用或返回解析失败结果。</summary>
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
        string SourcePhase,
        string SourceVoltage,
        string SourceCurrent,
        int VerificationTimeoutSeconds,
        int VerificationIntervalSeconds,
        decimal VerificationTolerancePercent,
        bool ShouldVerify,
        bool? SourceOutputActiveState)
    {
        /// <summary>创建无需额外动作且状态有效的源控制执行状态。</summary>
        public static SourceControlExecutionState Ok(string message)
        {
            return new SourceControlExecutionState(
                MeterTestSourceControlResult.Ok(message),
                string.Empty,
                MeterTestSourcePhaseMode.ThreePhase,
                "H",
                string.Empty,
                string.Empty,
                20,
                3,
                0.03m,
                false,
                null);
        }

        /// <summary>创建源控制执行失败状态。</summary>
        public static SourceControlExecutionState Fail(string message)
        {
            return new SourceControlExecutionState(
                MeterTestSourceControlResult.Fail(message),
                string.Empty,
                MeterTestSourcePhaseMode.ThreePhase,
                "H",
                string.Empty,
                string.Empty,
                20,
                3,
                0.03m,
                false,
                null);
        }

        /// <summary>创建已实际调用源接口的执行状态，并保留接口结果和输出参数。</summary>
        public static SourceControlExecutionState Executed(
            MeterTestSourceControlResult result,
            MeterTestSourceControlConfig sourceConfig,
            MeterTestSourcePhaseMode phaseMode,
            string sourceVoltage,
            string? sourceCurrent,
            string? sourcePhase)
        {
            bool isPowerOffCommand = sourceConfig.InterfaceType.Equals(
                MeterTestSourceInterfaceType.ShutPowerSource.ToString(),
                StringComparison.OrdinalIgnoreCase);
            return new SourceControlExecutionState(
                result,
                sourceConfig.Name,
                phaseMode,
                string.IsNullOrWhiteSpace(sourcePhase) ? "H" : sourcePhase.Trim().ToUpperInvariant(),
                sourceVoltage,
                sourceCurrent ?? string.Empty,
                sourceConfig.VerificationTimeoutSeconds,
                sourceConfig.VerificationIntervalSeconds,
                sourceConfig.VerificationTolerancePercent,
                !isPowerOffCommand,
                !isPowerOffCommand);
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
