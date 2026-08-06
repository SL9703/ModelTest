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
    /// 执行方案中的 LED 效果灯测试。
    /// 跑马灯和闪烁测试都只通过0x2F灯光协议发送，不改变测试业务结论之外的其它流程状态。
    /// </summary>
    public async Task<MeterTestFlowStepResult> ExecuteLedEffectTestAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        MeterTestSubItem subItem,
        IReadOnlyList<StationCommunicationConfig> stations,
        Action<int, IEnumerable<string>> writeStationLog,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        if (stations.Count == 0)
        {
            return MeterTestFlowStepResult.Fail("LED效果灯测试未选择工位。", startTicks);
        }

        string step = (subItem.LedEffectStep ?? string.Empty).Trim();
        bool success = step.Equals("Blink", StringComparison.OrdinalIgnoreCase)
            ? await ExecuteBlinkEffectAsync(planConfig, connectionManager, subItem, stations, writeStationLog, cancellationToken)
                .ConfigureAwait(false)
            : await ExecuteMarqueeEffectAsync(planConfig, connectionManager, subItem, stations, writeStationLog, cancellationToken)
                .ConfigureAwait(false);

        // 效果测试结束后恢复测试正常状态：LED1保持上电绿灯，LED2/LED3/LED4熄灭。
        foreach (StationCommunicationConfig station in stations.OrderBy(item => item.StationNo))
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

        string message = success
            ? $"LED效果灯测试完成：{subItem.Name}。"
            : $"LED效果灯测试存在发送失败或灯光配置缺失：{subItem.Name}。";
        return new MeterTestFlowStepResult(success, message, Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 跑马灯测试：按红、绿、黄三种颜色依次点亮LED1-LED4，每个状态按配置间隔保持，默认2秒。
    /// 默认总时长60秒；若一轮24个状态不足60秒，会从序列开头继续循环。
    /// </summary>
    private async Task<bool> ExecuteMarqueeEffectAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        MeterTestSubItem subItem,
        IReadOnlyList<StationCommunicationConfig> stations,
        Action<int, IEnumerable<string>> writeStationLog,
        CancellationToken cancellationToken)
    {
        int durationSeconds = Math.Max(MinimumDelaySeconds, subItem.LedMarqueeDurationSeconds);
        int intervalSeconds = Math.Max(MinimumDelaySeconds, subItem.LedMarqueeIntervalSeconds);
        int totalSteps = Math.Max(1, (int)Math.Ceiling(durationSeconds / (double)intervalSeconds));
        LightEffectStep[] sequence = BuildMarqueeSequence();
        bool allSuccess = true;

        WriteAllStationLogs(
            stations,
            writeStationLog,
            $"[LED跑马灯测试] 开始：总时长={durationSeconds}s，每{intervalSeconds}s发送一次，共{totalSteps}步。");

        for (int stepIndex = 0; stepIndex < totalSteps; stepIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LightEffectStep step = sequence[stepIndex % sequence.Length];
            allSuccess &= await SendEffectStepToStationsAsync(
                    planConfig,
                    connectionManager,
                    stations,
                    step,
                    $"[LED跑马灯测试] 第{stepIndex + 1}/{totalSteps}步：{step.Description}",
                    writeStationLog,
                    cancellationToken)
                .ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cancellationToken).ConfigureAwait(false);
        }

        WriteAllStationLogs(stations, writeStationLog, "[LED跑马灯测试] 结束，准备恢复测试正常状态。");
        return allSuccess;
    }

    /// <summary>
    /// 闪烁测试：LED1-LED4逐个执行红、绿、黄的闪烁、长亮、熄灭流程。
    /// 闪烁、长亮、熄灭分别由ledBlinkHoldSeconds、ledSteadyHoldSeconds、ledOffHoldSeconds配置，默认均为15秒。
    /// </summary>
    private async Task<bool> ExecuteBlinkEffectAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        MeterTestSubItem subItem,
        IReadOnlyList<StationCommunicationConfig> stations,
        Action<int, IEnumerable<string>> writeStationLog,
        CancellationToken cancellationToken)
    {
        int fallbackHoldSeconds = Math.Max(MinimumDelaySeconds, subItem.LedEffectHoldSeconds);
        int blinkHoldSeconds = subItem.LedBlinkHoldSeconds > 0 ? subItem.LedBlinkHoldSeconds : fallbackHoldSeconds;
        int steadyHoldSeconds = subItem.LedSteadyHoldSeconds > 0 ? subItem.LedSteadyHoldSeconds : fallbackHoldSeconds;
        int offHoldSeconds = subItem.LedOffHoldSeconds > 0 ? subItem.LedOffHoldSeconds : fallbackHoldSeconds;
        ushort blinkTimeMs = (ushort)Math.Clamp(subItem.LedBlinkTimeMs, 1, ushort.MaxValue);
        LightEffectStep[] sequence = BuildBlinkSequence(
            blinkTimeMs,
            Math.Max(MinimumDelaySeconds, blinkHoldSeconds),
            Math.Max(MinimumDelaySeconds, steadyHoldSeconds),
            Math.Max(MinimumDelaySeconds, offHoldSeconds));
        bool allSuccess = true;

        WriteAllStationLogs(
            stations,
            writeStationLog,
            $"[LED闪烁测试] 开始：闪烁保持={blinkHoldSeconds}s，长亮保持={steadyHoldSeconds}s，"
            + $"熄灭保持={offHoldSeconds}s，闪烁周期={blinkTimeMs}ms，共{sequence.Length}步。");

        for (int stepIndex = 0; stepIndex < sequence.Length; stepIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LightEffectStep step = sequence[stepIndex];
            allSuccess &= await SendEffectStepToStationsAsync(
                    planConfig,
                    connectionManager,
                    stations,
                    step,
                    $"[LED闪烁测试] 第{stepIndex + 1}/{sequence.Length}步：{step.Description}，保持{step.HoldSeconds}s",
                    writeStationLog,
                    cancellationToken)
                .ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromSeconds(step.HoldSeconds), cancellationToken).ConfigureAwait(false);
        }

        WriteAllStationLogs(stations, writeStationLog, "[LED闪烁测试] 结束，准备恢复测试正常状态。");
        return allSuccess;
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

    /// <summary>按“红灯跑完LED1-LED4，再绿灯，再黄灯”的顺序生成跑马灯状态序列。</summary>
    private static LightEffectStep[] BuildMarqueeSequence()
    {
        byte[] colors = { Red, Green, Yellow };
        byte[] leds = { Led1Power, Led2SelfCheck, Led3Testing, Led4Result };
        return colors
            .SelectMany(color => leds.SelectMany(led => new[]
            {
                new LightEffectStep(color, led, SteadyOn, 0, 0, $"{GetLedName(led)} {GetColorName(color)}灯长亮"),
                new LightEffectStep(color, led, Off, 0, 0, $"{GetLedName(led)} 熄灭")
            }))
            .ToArray();
    }

    /// <summary>生成闪烁测试状态序列；LED3按用户要求使用红、黄、绿顺序，其余为红、绿、黄。</summary>
    private static LightEffectStep[] BuildBlinkSequence(
        ushort blinkTimeMs,
        int blinkHoldSeconds,
        int steadyHoldSeconds,
        int offHoldSeconds)
    {
        byte[] leds = { Led1Power, Led2SelfCheck, Led3Testing, Led4Result };
        return leds
            .SelectMany(led =>
            {
                byte[] colors = led == Led3Testing
                    ? new[] { Red, Yellow, Green }
                    : new[] { Red, Green, Yellow };
                return colors.SelectMany(color => new[]
                {
                    new LightEffectStep(color, led, Blink, blinkTimeMs, blinkHoldSeconds, $"{GetLedName(led)} {GetColorName(color)}灯闪烁"),
                    new LightEffectStep(color, led, SteadyOn, 0, steadyHoldSeconds, $"{GetLedName(led)} {GetColorName(color)}灯长亮"),
                    new LightEffectStep(color, led, Off, 0, offHoldSeconds, $"{GetLedName(led)} {GetColorName(color)}灯熄灭")
                });
            })
            .ToArray();
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

    private static string GetLedName(byte ledMask) => ledMask switch
    {
        Led1Power => "LED1",
        Led2SelfCheck => "LED2",
        Led3Testing => "LED3",
        Led4Result => "LED4",
        _ => $"LED掩码0x{ledMask:X2}"
    };

    private static string GetColorName(byte color) => color switch
    {
        Red => "红",
        Green => "绿",
        Yellow => "黄",
        _ => $"颜色0x{color:X2}"
    };

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
