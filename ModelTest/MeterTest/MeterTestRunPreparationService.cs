namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 方案执行前置准备服务。
///
/// 每轮方案开始前只执行一次：
/// 1. 从资产数据库读取本轮工位档案；
/// 2. 按电表类型和接入方式切换装置通信板；
/// 3. 按资产基本电流、额定电压和有功常数初始化源。
///
/// 任一步失败都会返回明确结果并阻止方案进入具体 TestSubItem，避免各测试服务重复执行 0x82 或 Ini。
/// </summary>
internal sealed class MeterTestRunPreparationService
{
    private const int MaxStationCount = 48;
    private readonly MeterTestAccessDatabaseService accessDatabaseService;
    private readonly MeterTestBenchTypeSwitchService benchTypeSwitchService;
    private readonly MeterTestSourceControlService sourceControlService;
    private readonly MeterTestControlPcbConnectionManager controlPcbConnectionManager;

    /// <summary>
    /// 创建方案前置准备服务。
    /// </summary>
    /// <param name="accessDatabaseService">资产数据库服务。</param>
    /// <param name="benchTypeSwitchService">装置通信板 0x82 台体类型切换服务。</param>
    /// <param name="sourceControlService">源串口打开与 Ini 初始化服务。</param>
    /// <param name="controlPcbConnectionManager">装置通信板和控制 PCB 长连接管理器。</param>
    public MeterTestRunPreparationService(
        MeterTestAccessDatabaseService accessDatabaseService,
        MeterTestBenchTypeSwitchService benchTypeSwitchService,
        MeterTestSourceControlService sourceControlService,
        MeterTestControlPcbConnectionManager controlPcbConnectionManager)
    {
        this.accessDatabaseService = accessDatabaseService;
        this.benchTypeSwitchService = benchTypeSwitchService;
        this.sourceControlService = sourceControlService;
        this.controlPcbConnectionManager = controlPcbConnectionManager;
    }

    /// <summary>
    /// 执行本轮测试唯一一次台体切换和源初始化。
    /// </summary>
    /// <param name="planConfig">方案和硬件端点配置。</param>
    /// <param name="selectedStations">本轮选择的工位。</param>
    /// <param name="trace">完整前置流程日志回调。</param>
    /// <param name="cancellationToken">停止测试时使用的取消令牌。</param>
    public async Task<MeterTestRunPreparationResult> ExecuteAsync(
        MeterTestPlanConfig planConfig,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<string>? trace,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        if (selectedStations.Count == 0)
            return MeterTestRunPreparationResult.Fail("没有选中工位，不能执行前置准备。", startTicks);

        IReadOnlyDictionary<int, MeterArchiveData> archives =
            accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
        List<int> stationNumbers = selectedStations
            .Select(station => station.StationNo)
            .Distinct()
            .OrderBy(stationNo => stationNo)
            .ToList();
        Trace(
            trace,
            $"[执行前准备] 开始：工位={string.Join(',', stationNumbers)}，"
            + $"资产数量={archives.Count}，步骤=台体0x82切换 -> 源Ini初始化。");

        long benchStartTicks = Environment.TickCount64;
        MeterTestBenchTypeSwitchResult benchResult = await benchTypeSwitchService.ExecuteAsync(
            planConfig.BenchTypeSwitchConfig,
            stationNumbers,
            archives,
            controlPcbConnectionManager,
            cancellationToken).ConfigureAwait(false);
        long benchElapsed = Math.Max(0, Environment.TickCount64 - benchStartTicks);
        Trace(
            trace,
            $"[执行前准备][台体切换接口] 完成：耗时={benchElapsed}ms，"
            + $"结论={(benchResult.Success ? "合格" : "不合格")}，说明={benchResult.Message}");
        if (!benchResult.Success)
        {
            return MeterTestRunPreparationResult.Fail(
                $"执行前台体类型切换失败：{benchResult.Message}",
                startTicks,
                benchResult,
                null);
        }

        List<MeterTestStationCommunication> sourceStations = selectedStations
            .Select(station => new MeterTestStationCommunication
            {
                StationNo = station.StationNo,
                Ip = station.Ip,
                Port = station.Port
            })
            .ToList();
        long sourceStartTicks = Environment.TickCount64;
        MeterTestSourceControlService.MeterTestSourceControlResult sourceResult =
            await sourceControlService.InitializeRunAsync(
                planConfig,
                sourceStations,
                archives,
                cancellationToken,
                message => Trace(trace, $"[执行前准备][源初始化接口] {message}"))
                .ConfigureAwait(false);
        long sourceElapsed = Math.Max(0, Environment.TickCount64 - sourceStartTicks);
        Trace(
            trace,
            $"[执行前准备][源初始化接口] 完成：耗时={sourceElapsed}ms，"
            + $"结论={(sourceResult.Success ? "合格" : "不合格")}，说明={sourceResult.Message}");
        if (!sourceResult.Success)
        {
            return MeterTestRunPreparationResult.Fail(
                $"执行前源初始化失败：{sourceResult.Message}",
                startTicks,
                benchResult,
                sourceResult);
        }

        string message =
            $"执行前准备完成：台体切换={benchResult.Message}；源初始化={sourceResult.Message}。";
        Trace(trace, $"[执行前准备] 完成：结论=合格，{message}");
        return new MeterTestRunPreparationResult(
            true,
            message,
            benchResult,
            sourceResult,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>同时写入调用方日志和全局 Debug 日志。</summary>
    private static void Trace(Action<string>? trace, string message)
    {
        LogMessage.Debug(message);
        trace?.Invoke(message);
    }
}

/// <summary>方案前置准备的整体结果，以及台体切换和源初始化的原始子结果。</summary>
internal sealed record MeterTestRunPreparationResult(
    bool Success,
    string Message,
    MeterTestBenchTypeSwitchResult? BenchSwitchResult,
    MeterTestSourceControlService.MeterTestSourceControlResult? SourceInitializationResult,
    long ElapsedMilliseconds)
{
    /// <summary>使用方法起始 Tick 构造失败结果，并保留已完成的子步骤信息。</summary>
    public static MeterTestRunPreparationResult Fail(
        string message,
        long startTicks,
        MeterTestBenchTypeSwitchResult? benchSwitchResult = null,
        MeterTestSourceControlService.MeterTestSourceControlResult? sourceInitializationResult = null)
    {
        return new MeterTestRunPreparationResult(
            false,
            message,
            benchSwitchResult,
            sourceInitializationResult,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }
}
