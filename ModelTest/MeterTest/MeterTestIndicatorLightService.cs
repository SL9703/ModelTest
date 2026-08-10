using System.Collections.Concurrent;
using ModelTest.Protocol;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 工位指示灯控制服务。
/// 负责把测试状态转换成 0x2F 指示灯控制帧，并复用程序启动阶段已经建立的控制板 TCP 长连接发送。
/// </summary>
internal sealed class MeterTestIndicatorLightService
{
    private const byte Red = 0x01;
    private const byte Green = 0x02;
    private const byte Yellow = 0x03;
    private const byte StationLed = 0x01;
    private const byte SteadyOn = 0x01;
    private const byte Blink = 0x02;
    private const byte Off = 0x00;
    private const byte Led1Power = 0x01;
    private const byte Led2SelfCheck = 0x02;
    private const byte Led3Testing = 0x04;
    private const byte Led4Result = 0x08;
    private const int MinimumDelaySeconds = 1;

    /// <summary>同一灯光板按端点串行发送，避免多工位同时刷新时 TCP 报文交叉。</summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> endpointLocks =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly DetectionBoardProtocolV2 protocol = new();

    /// <summary>
    /// 工位上电联动 LED1：上电成功绿灯，上电失败红灯，取消选择或下电时熄灭。
    /// </summary>
    public Task SetPowerIndicatorAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        int stationNo,
        bool selected,
        bool powerSuccess,
        CancellationToken cancellationToken)
    {
        byte mode = selected ? SteadyOn : Off;
        byte color = selected && !powerSuccess ? Red : Green;
        return SendStationLightAsync(
            planConfig,
            connectionManager,
            stationNo,
            color,
            Led1Power,
            mode,
            0,
            "[工位指示灯][LED1上电指示]",
            cancellationToken);
    }

    /// <summary>设备自检结果联动 LED2：合格绿灯，不合格红灯。</summary>
    public Task SetSelfCheckIndicatorAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        int stationNo,
        bool passed,
        CancellationToken cancellationToken)
    {
        return SendStationLightAsync(
            planConfig,
            connectionManager,
            stationNo,
            passed ? Green : Red,
            Led2SelfCheck,
            SteadyOn,
            0,
            "[工位指示灯][LED2自检结果]",
            cancellationToken);
    }

    /// <summary>测试执行状态联动 LED3：测试中黄灯，测试结束熄灭。</summary>
    public Task SetTestingIndicatorAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        int stationNo,
        bool running,
        CancellationToken cancellationToken)
    {
        return SendStationLightAsync(
            planConfig,
            connectionManager,
            stationNo,
            Yellow,
            Led3Testing,
            running ? SteadyOn : Off,
            0,
            "[工位指示灯][LED3测试状态]",
            cancellationToken);
    }

    /// <summary>整表方案结论联动 LED4：全合格绿灯，任一不合格红灯。</summary>
    public Task SetFinalResultIndicatorAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        int stationNo,
        bool passed,
        CancellationToken cancellationToken)
    {
        return SendStationLightAsync(
            planConfig,
            connectionManager,
            stationNo,
            passed ? Green : Red,
            Led4Result,
            SteadyOn,
            0,
            "[工位指示灯][LED4最终结果]",
            cancellationToken);
    }

    /// <summary>保存结果或开始新任务前熄灭指定工位 LED1-LED4。</summary>
    public Task TurnOffAllStationIndicatorsAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        int stationNo,
        CancellationToken cancellationToken)
    {
        return SendStationLightAsync(
            planConfig,
            connectionManager,
            stationNo,
            Green,
            Led1Power | Led2SelfCheck | Led3Testing | Led4Result,
            Off,
            0,
            "[工位指示灯][全部熄灭]",
            cancellationToken);
    }

    /// <summary>
    /// 按灯光控制面板执行完整LED效果测试。
    /// 流程为：开始确认 -> 面板1 -> 面板2 -> 面板3；同一面板内的工位并发发送，
    /// 面板之间严格串行，避免多个面板同时发送导致目视顺序混乱。
    /// </summary>
    public async Task<MeterTestFlowStepResult> ExecuteLedEffectSuiteAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        MeterTestSubItem marqueeSubItem,
        MeterTestSubItem blinkSubItem,
        IReadOnlyList<StationCommunicationConfig> stations,
        Action<int, IEnumerable<string>> writeStationLog,
        Func<string, bool> confirmStart,
        Func<string, IReadOnlyList<int>, IReadOnlyList<int>?> confirmPanelResult,
        Action<IReadOnlyList<int>, IReadOnlyList<int>, bool, string> panelResult,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        if (stations.Count == 0)
            return MeterTestFlowStepResult.Fail("LED效果灯测试未选择工位。", startTicks);

        if (!confirmStart("即将开始LED效果灯测试，请确认现场人员已就位，可以观察灯光变化。"))
            return MeterTestFlowStepResult.Fail("用户取消LED效果灯测试。", startTicks);

        MeterTestIndicatorLightGroup[] groups = planConfig.IndicatorLightGroups
            .Where(group => group.Enabled)
            .OrderBy(group => group.StationStart)
            .ToArray();
        bool allSuccess = true;
        bool completedAnyGroup = false;

        foreach (MeterTestIndicatorLightGroup group in groups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<StationCommunicationConfig> groupStations = stations
                .Where(station => station.StationNo >= group.StationStart && station.StationNo <= group.StationEnd)
                .OrderBy(station => station.StationNo)
                .ToList();
            if (groupStations.Count == 0)
                continue;

            completedAnyGroup = true;
            int marqueeIntervalSeconds = Math.Max(MinimumDelaySeconds, marqueeSubItem.LedMarqueeIntervalSeconds);
            ushort blinkTimeMs = (ushort)Math.Clamp(blinkSubItem.LedBlinkTimeMs, 1, ushort.MaxValue);
            int blinkHoldSeconds = blinkSubItem.LedBlinkHoldSeconds > 0
                ? blinkSubItem.LedBlinkHoldSeconds
                : Math.Max(MinimumDelaySeconds, blinkSubItem.LedEffectHoldSeconds);
            int steadyHoldSeconds = blinkSubItem.LedSteadyHoldSeconds > 0
                ? blinkSubItem.LedSteadyHoldSeconds
                : Math.Max(MinimumDelaySeconds, blinkSubItem.LedEffectHoldSeconds);
            int offHoldSeconds = blinkSubItem.LedOffHoldSeconds > 0
                ? blinkSubItem.LedOffHoldSeconds
                : Math.Max(MinimumDelaySeconds, blinkSubItem.LedEffectHoldSeconds);

            string stationSummary = string.Join(",", groupStations.Select(station => station.StationNo));
            WriteAllStationLogs(
                groupStations,
                writeStationLog,
                $"[LED效果灯测试] 开始面板：{group.Name}，工位={stationSummary}。");

            bool groupSuccess = true;
            LightEffectStep[] sequence =
            [
                new(Red, Led1Power, SteadyOn, 0, marqueeIntervalSeconds, "LED1红灯长亮"),
                new(Red, Led2SelfCheck, SteadyOn, 0, marqueeIntervalSeconds, "LED2红灯长亮"),
                new(Red, Led3Testing, SteadyOn, 0, marqueeIntervalSeconds, "LED3红灯长亮"),
                new(Red, Led4Result, SteadyOn, 0, marqueeIntervalSeconds, "LED4红灯长亮"),
                new(0, 0, Off, 0, 5, "红灯顺序确认等待"),
                new(Green, Led1Power | Led2SelfCheck | Led3Testing | Led4Result, SteadyOn, 0, 5, "LED1-LED4绿灯长亮"),
                new(Yellow, Led1Power | Led2SelfCheck | Led3Testing | Led4Result, SteadyOn, 0, 5, "LED1-LED4黄灯长亮"),
                new(Red, Led1Power | Led2SelfCheck | Led3Testing | Led4Result, Blink, blinkTimeMs, 5, "LED1-LED4红灯闪烁"),
                new(Green, Led1Power | Led2SelfCheck | Led3Testing | Led4Result, Blink, blinkTimeMs, 5, "LED1-LED4绿灯闪烁"),
                new(Yellow, Led1Power | Led2SelfCheck | Led3Testing | Led4Result, Blink, blinkTimeMs, 5, "LED1-LED4黄灯闪烁"),
                new(Green, Led1Power | Led2SelfCheck | Led3Testing | Led4Result, Off, 0, 0, "LED1-LED4熄灭")
            ];

            foreach (LightEffectStep step in sequence)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (step.LedMask != 0)
                {
                    groupSuccess &= await SendEffectStepToStationsAsync(
                            planConfig,
                            connectionManager,
                            groupStations,
                            step,
                            $"[LED效果灯测试][{group.Name}] {step.Description}",
                            writeStationLog,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    WriteAllStationLogs(groupStations, writeStationLog, $"[LED效果灯测试][{group.Name}] {step.Description}");
                }

                if (step.HoldSeconds > 0)
                    await Task.Delay(TimeSpan.FromSeconds(step.HoldSeconds), cancellationToken).ConfigureAwait(false);
            }

            string panelMessage = groupSuccess
                ? $"{group.Name} 工位{stationSummary}灯光报文发送完成，请目视确认流水灯和闪烁效果。"
                : $"{group.Name} 工位{stationSummary}存在灯光报文发送失败，请检查日志和现场设备。";
            int[] groupStationNumbers = groupStations.Select(station => station.StationNo).ToArray();
            IReadOnlyList<int>? passedStations = confirmPanelResult(panelMessage, groupStationNumbers);
            if (passedStations is null)
            {
                WriteAllStationLogs(groupStations, writeStationLog, $"[LED效果灯测试] 用户取消{group.Name}确认，结束本次灯光测试。");
                return new MeterTestFlowStepResult(
                    false,
                    $"用户取消{group.Name}工位合格确认。",
                    Math.Max(0, Environment.TickCount64 - startTicks));
            }

            int[] normalizedPassedStations = passedStations
                .Intersect(groupStationNumbers)
                .Distinct()
                .OrderBy(stationNo => stationNo)
                .ToArray();
            bool finalGroupResult = groupSuccess && normalizedPassedStations.Length == groupStationNumbers.Length;
            panelResult(
                groupStationNumbers,
                normalizedPassedStations,
                groupSuccess,
                groupSuccess
                    ? $"用户确认合格工位：{string.Join(",", normalizedPassedStations)}。"
                    : "灯光报文发送存在失败，未通过发送验证的工位判为不合格。");
            allSuccess &= finalGroupResult;

            WriteAllStationLogs(
                groupStations,
                writeStationLog,
                $"[LED效果灯测试] 面板{group.Name}完成，结论={(finalGroupResult ? "合格" : "不合格")}。");
        }

        if (!completedAnyGroup)
            return MeterTestFlowStepResult.Fail("选中工位未匹配到启用的IndicatorLightGroup。", startTicks);

        foreach (StationCommunicationConfig station in stations)
        {
            await TurnOffAllStationIndicatorsAsync(
                    planConfig,
                    connectionManager,
                    station.StationNo,
                    cancellationToken)
                .ConfigureAwait(false);
            await SetPowerIndicatorAsync(
                    planConfig,
                    connectionManager,
                    station.StationNo,
                    selected: true,
                    powerSuccess: true,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return new MeterTestFlowStepResult(
            allSuccess,
            allSuccess ? "所有灯光控制面板LED效果测试合格。" : "LED效果灯测试存在不合格面板。",
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 将一个效果步骤并发发送到本次选中的全部工位；同一端点内部仍由SendStationLightAsync串行化。
    /// </summary>
    private async Task<bool> SendEffectStepToStationsAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        IReadOnlyList<StationCommunicationConfig> stations,
        LightEffectStep step,
        string stationLog,
        Action<int, IEnumerable<string>> writeStationLog,
        CancellationToken cancellationToken)
    {
        Task<bool>[] tasks = stations
            .OrderBy(item => item.StationNo)
            .Select(async station =>
            {
                writeStationLog(station.StationNo, new[] { stationLog });
                return await SendStationLightAsync(
                        planConfig,
                        connectionManager,
                        station.StationNo,
                        step.Color,
                        step.LedMask,
                        step.Mode,
                        step.BlinkTimeMs,
                        "[LED效果灯测试]",
                        cancellationToken)
                    .ConfigureAwait(false);
            })
            .ToArray();

        bool[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.All(result => result);
    }

    /// <summary>把同一条过程说明写入所有目标工位的测试日志。</summary>
    private static void WriteAllStationLogs(
        IEnumerable<StationCommunicationConfig> stations,
        Action<int, IEnumerable<string>> writeStationLog,
        string log)
    {
        foreach (StationCommunicationConfig station in stations.OrderBy(item => item.StationNo))
        {
            writeStationLog(station.StationNo, new[] { log });
        }
    }

    /// <summary>
    /// 根据工位查找灯光分组，计算灯光地址并发送 0x2F 控制帧。
    /// 失败只写日志，不中断测试主流程。
    /// </summary>
    private async Task<bool> SendStationLightAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        int stationNo,
        byte color,
        byte ledMask,
        byte mode,
        ushort blinkTimeMs,
        string logPrefix,
        CancellationToken cancellationToken)
    {
        MeterTestIndicatorLightGroup? group = planConfig.IndicatorLightGroups
            .Where(item => item.Enabled)
            .FirstOrDefault(item => stationNo >= item.StationStart && stationNo <= item.StationEnd);
        if (group is null)
        {
            LogMessage.Debug($"{logPrefix} 工位{stationNo}未匹配到启用的 IndicatorLightGroup，跳过灯光控制。");
            return false;
        }

        int lightAddressValue = group.LightAddressStart + stationNo - group.StationStart;
        if (lightAddressValue is < 1 or > 254)
        {
            LogMessage.Debug($"{logPrefix} 工位{stationNo}计算出的灯光地址{lightAddressValue}超出1-254，跳过灯光控制。");
            return false;
        }

        try
        {
            if (!connectionManager.TryGetConnectedConnection(
                    group.Ip,
                    group.Port,
                    group.ProtocolVersion,
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                LogMessage.Debug($"{logPrefix} 工位{stationNo}灯光控制跳过：{connectionError}");
                return false;
            }

            byte lightAddress = (byte)lightAddressValue;
            byte[] packet = protocol.BuildIndicatorLightControlFrame(
                lightAddress,
                color,
                StationLed,
                ledMask,
                mode,
                blinkTimeMs);
            string endpoint = $"{group.Ip.Trim()}:{group.Port}";
            SemaphoreSlim endpointLock = endpointLocks.GetOrAdd(endpoint, _ => new SemaphoreSlim(1, 1));
            await endpointLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                LogMessage.Debug(
                    $"{logPrefix} 准备发送：工位={stationNo}，灯光地址=0x{lightAddress:X2}，"
                    + $"颜色=0x{color:X2}，LED掩码=0x{ledMask:X2}，模式=0x{mode:X2}，Endpoint={endpoint}，"
                    + $"报文={ToHexString(packet)}");
                await connection.SendAsync(packet, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                endpointLock.Release();
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            LogMessage.Error($"{logPrefix} 工位{stationNo}灯光控制异常。", ex);
            return false;
        }
    }

    /// <summary>格式化指示灯控制帧，便于现场按日志核对。</summary>
    private static string ToHexString(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }

    /// <summary>单个LED效果状态，统一携带颜色、LED位、模式、闪烁周期和日志说明。</summary>
    private sealed record LightEffectStep(
        byte Color,
        byte LedMask,
        byte Mode,
        ushort BlinkTimeMs,
        int HoldSeconds,
        string Description);
}
