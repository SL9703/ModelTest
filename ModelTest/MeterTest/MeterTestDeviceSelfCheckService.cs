using ModelTest.CustomControl;
using ModelTest.Protocol;

namespace ModelTest.MeterTest;

/// <summary>
/// 检测单元设备自检流程服务。
///
/// 本服务负责短路检测(0x86)、断路检测(0x84)和温度检测(0xCA)的完整业务流程。
/// 短路检测在任何报文下发前先调用源控制服务确认无压，再通过 UI 回调要求操作员确认；
/// 控制 PCB 的连接、完整收发报文和超时日志由共享命令服务统一处理。
/// </summary>
internal sealed class MeterTestDeviceSelfCheckService
{
    private readonly MeterTestSourceControlService sourceControlService;
    private readonly MeterTestControlPcbCommandService controlPcbCommandService;

    /// <summary>注入源安全检查与控制 PCB 命令服务。</summary>
    public MeterTestDeviceSelfCheckService(
        MeterTestSourceControlService sourceControlService,
        MeterTestControlPcbCommandService controlPcbCommandService)
    {
        this.sourceControlService = sourceControlService;
        this.controlPcbCommandService = controlPcbCommandService;
    }

    /// <summary>
    /// 执行一个设备自检方案小项。
    /// confirmShortCircuitSafety 只负责显示确认界面；是否需要确认及取消后的业务结论由本服务决定。
    /// </summary>
    public async Task<MeterTestFlowStepResult> ExecuteAsync(
        MeterTestPlanConfig planConfig,
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Func<bool> confirmShortCircuitSafety,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext> updateRunningState,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        foreach (StationCommunicationConfig station in selectedStations)
        {
            updateRunningState(station.StationNo, context);
            writeStationLog(station.StationNo, new[]
            {
                MeterTestLogText.Separator,
                $"[设备自检] 开始执行：步骤={context.SubItem.DeviceSelfCheckStep}，小项={context.SubItem.Name}。"
            });
        }

        string normalizedStep = context.SubItem.DeviceSelfCheckStep.Trim().ToUpperInvariant();
        if (normalizedStep == "SHORTCIRCUIT")
        {
            MeterTestFlowStepResult safetyResult = await EnsureShortCircuitSafetyAsync(
                context,
                selectedStations,
                confirmShortCircuitSafety,
                writeStationLog,
                applyResult,
                cancellationToken).ConfigureAwait(false);
            if (!safetyResult.Success)
                return safetyResult;
        }

        List<MeterTestControlPcbGroup> groups =
            MeterTestControlPcbCommandService.GetEnabledGroups(planConfig, context.SubItem);
        if (groups.Count == 0)
        {
            const string noGroup = "未找到可用控制PCB分组，请检查ControlPcbGroups。";
            ApplyFailure(context, selectedStations, noGroup, writeStationLog, applyResult);
            return MeterTestFlowStepResult.Fail(noGroup, startTicks);
        }

        HashSet<int> mappedStations = groups
            .SelectMany(group => MeterTestControlPcbCommandService.GetTargets(group, selectedStations))
            .Select(target => target.StationNo)
            .ToHashSet();
        foreach (StationCommunicationConfig station in selectedStations.Where(
                     station => !mappedStations.Contains(station.StationNo)))
        {
            string unmapped = $"工位{station.StationNo}未映射到启用的ControlPcbGroup。";
            writeStationLog(station.StationNo, new[] { unmapped, MeterTestLogText.Separator });
            applyResult(station.StationNo, context, false, unmapped);
        }

        bool[] groupResults = await Task.WhenAll(groups.Select(group => ExecuteGroupAsync(
            context,
            group,
            selectedStations,
            normalizedStep,
            writeStationLog,
            applyResult,
            recordMeasurement,
            cancellationToken))).ConfigureAwait(false);
        bool passed = mappedStations.Count == selectedStations.Count &&
            groupResults.Length > 0 &&
            groupResults.All(value => value);
        string summary = passed
            ? $"{context.SubItem.Name}完成，全部选中工位合格。"
            : $"{context.SubItem.Name}完成，存在未执行、无应答或检测异常工位。";
        LogMessage.Debug(
            $"[设备自检][流程结束] 小项={context.SubItem.Name}，工位数={selectedStations.Count}，"
            + $"控制PCB组成功={groupResults.Count(value => value)}/{groupResults.Length}，结论={passed}。");
        return new MeterTestFlowStepResult(
            passed,
            summary,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 短路检测安全前置：检查最近标准表电压，必要时调用ShutPowerSource(0)并复核，随后要求人工确认。
    /// </summary>
    private async Task<MeterTestFlowStepResult> EnsureShortCircuitSafetyAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Func<bool> confirmShortCircuitSafety,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        LogMessage.Debug(
            $"[设备自检][短路安全检查] 开始，最大安全电压={context.SubItem.SelfCheckMaximumSafeVoltage:0.###}V，"
            + $"工位={string.Join(",", selectedStations.Select(station => station.StationNo))}。");
        MeterTestSourceControlService.MeterTestSourceSafetyResult safety =
            await sourceControlService.EnsureDeEnergizedAsync(
                context.SubItem.SelfCheckMaximumSafeVoltage,
                cancellationToken,
                message =>
                {
                    LogMessage.Debug($"[设备自检][短路安全检查][源接口] {message}");
                    foreach (StationCommunicationConfig station in selectedStations)
                        writeStationLog(station.StationNo, new[] { $"[短路安全检查] {message}" });
                }).ConfigureAwait(false);
        if (!safety.Success)
        {
            ApplyFailure(context, selectedStations, safety.Message, writeStationLog, applyResult);
            return MeterTestFlowStepResult.Fail(safety.Message, startTicks);
        }

        bool confirmed = confirmShortCircuitSafety();
        LogMessage.Debug($"[设备自检][短路安全检查] 人工确认结果={(confirmed ? "确认执行" : "取消")}。");
        if (!confirmed)
        {
            const string cancelled = "用户取消短路检测，未向控制PCB发送0x86报文。";
            ApplyFailure(context, selectedStations, cancelled, writeStationLog, applyResult);
            return MeterTestFlowStepResult.Fail(cancelled, startTicks);
        }

        foreach (StationCommunicationConfig station in selectedStations)
        {
            writeStationLog(
                station.StationNo,
                new[] { "用户已确认线路无电压，允许发送0x86短路检测报文。" });
        }

        return new MeterTestFlowStepResult(
            true,
            safety.Message,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>按配置步骤路由一个控制 PCB 分组的短路、断路或温度检测。</summary>
    private Task<bool> ExecuteGroupAsync(
        SelectedSubItemContext context,
        MeterTestControlPcbGroup group,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        string normalizedStep,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        List<ControlPcbStationTarget> targets =
            MeterTestControlPcbCommandService.GetTargets(group, selectedStations);
        if (targets.Count == 0)
            return Task.FromResult(true);

        if (!MeterTestControlPcbCommandService.IsV2(group.ProtocolVersion))
        {
            string protocolError =
                $"{group.Name}配置为{group.ProtocolVersion}，设备自检仅支持V2电表控制协议类型0x02。";
            ApplyTargetFailure(context, targets, protocolError, writeStationLog, applyResult);
            return Task.FromResult(false);
        }

        return normalizedStep switch
        {
            "SHORTCIRCUIT" => ExecuteDetectionGroupAsync(
                context, group, targets, true, writeStationLog, applyResult, cancellationToken),
            "OPENCIRCUIT" => ExecuteDetectionGroupAsync(
                context, group, targets, false, writeStationLog, applyResult, cancellationToken),
            "TEMPERATUREHUMIDITY" => ExecuteTemperatureGroupAsync(
                context, group, targets, writeStationLog, applyResult, recordMeasurement, cancellationToken),
            _ => Task.FromResult(FailUnknownStep(
                context, targets, writeStationLog, applyResult))
        };
    }

    /// <summary>
    /// 执行0x86短路或0x84断路检测：发送启动、筛选应答工位、等待配置延迟、发送结果读取并判定结果码。
    /// </summary>
    private async Task<bool> ExecuteDetectionGroupAsync(
        SelectedSubItemContext context,
        MeterTestControlPcbGroup group,
        IReadOnlyList<ControlPcbStationTarget> targets,
        bool isShortCircuit,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        CancellationToken cancellationToken)
    {
        byte command = isShortCircuit ? (byte)0x86 : (byte)0x84;
        string testName = isShortCircuit ? "检测单元短路检测" : "检测单元断路检测";
        TimeSpan timeout = TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs));
        TimeSpan packetInterval = TimeSpan.FromMilliseconds(Math.Max(0, context.SubItem.PacketIntervalMs));
        LogMessage.Debug(
            $"[设备自检][{testName}] 分组={group.Name}，端点={group.Ip}:{group.Port}，"
            + $"目标={string.Join(",", targets.Select(target => $"工位{target.StationNo}/表位{target.MeterAddress:X2}"))}。");

        MeterTestControlPcbBatchResult startBatch = await controlPcbCommandService.SendAndCollectAsync(
            group,
            targets,
            target => isShortCircuit
                ? ElectricEnergyMeterControlV2.BuildShortCircuitDetectionStartPacket(target.MeterAddress)
                : ElectricEnergyMeterControlV2.BuildOpenCircuitDetectionStartPacket(target.MeterAddress),
            target => $"{testName}启动，命令=0x{command:X2}，操作=0x{ElectricEnergyMeterControlV2.OperationExecute:X2}",
            frame => ResolveResponseAddress(
                frame,
                command,
                ElectricEnergyMeterControlV2.OperationExecute,
                targets.Select(target => target.MeterAddress)),
            timeout,
            packetInterval,
            writeStationLog,
            cancellationToken).ConfigureAwait(false);

        List<ControlPcbStationTarget> activeTargets = new();
        bool allPassed = true;
        foreach (ControlPcbStationTarget target in targets)
        {
            bool valid = startBatch.Responses.TryGetValue(target.MeterAddress, out byte[]? response) &&
                ParseDetectionResponse(
                    response!,
                    target.MeterAddress,
                    ElectricEnergyMeterControlV2.OperationExecute,
                    isShortCircuit,
                    out _,
                    out _);
            if (valid)
            {
                activeTargets.Add(target);
                writeStationLog(target.StationNo, new[] { $"{testName}启动应答正常，仅该工位继续读取结果。" });
                continue;
            }

            allPassed = false;
            string failure = $"{testName}启动未收到正确应答，该工位停止当前自检步骤。";
            writeStationLog(target.StationNo, new[] { failure, MeterTestLogText.Separator });
            applyResult(target.StationNo, context, false, failure);
        }

        if (activeTargets.Count == 0)
            return false;

        int delayMs = Math.Max(0, context.SubItem.SelfCheckDelayMs);
        if (delayMs > 0)
        {
            LogMessage.Debug($"[设备自检][{testName}] 启动应答完成，等待{delayMs}ms后读取结果。");
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
        }

        MeterTestControlPcbBatchResult resultBatch = await controlPcbCommandService.SendAndCollectAsync(
            group,
            activeTargets,
            target => isShortCircuit
                ? ElectricEnergyMeterControlV2.BuildShortCircuitDetectionResultPacket(target.MeterAddress)
                : ElectricEnergyMeterControlV2.BuildOpenCircuitDetectionResultPacket(target.MeterAddress),
            target => $"{testName}结果获取，命令=0x{command:X2}，操作=0x{MeterControlPcbProtocol.ReadOperation:X2}",
            frame => ResolveResponseAddress(
                frame,
                command,
                MeterControlPcbProtocol.ReadOperation,
                activeTargets.Select(target => target.MeterAddress)),
            timeout,
            packetInterval,
            writeStationLog,
            cancellationToken).ConfigureAwait(false);

        foreach (ControlPcbStationTarget target in activeTargets)
        {
            byte resultCode = 0xFF;
            string description = "检测结果应答解析失败。";
            bool hasResponse = resultBatch.Responses.TryGetValue(target.MeterAddress, out byte[]? response);
            bool parsed = hasResponse && ParseDetectionResponse(
                response!,
                target.MeterAddress,
                MeterControlPcbProtocol.ReadOperation,
                isShortCircuit,
                out resultCode,
                out description);
            if (!parsed)
            {
                resultCode = 0xFF;
                description = hasResponse ? "检测结果应答解析失败。" : "检测结果获取无应答。";
            }

            bool passed = parsed && (isShortCircuit ? resultCode == 0x00 : resultCode == 0x01);
            allPassed &= passed;
            string resultText = parsed
                ? $"结果码=0x{resultCode:X2}，{description}；结论：{(passed ? "合格" : "不合格")}。"
                : $"{description}；结论：不合格。";
            writeStationLog(target.StationNo, new[] { resultText, MeterTestLogText.Separator });
            applyResult(target.StationNo, context, passed, resultText);
        }

        return allPassed;
    }

    /// <summary>发送0xCA+传感器序号+AA并解析4字节有符号小端温度原始值。</summary>
    private async Task<bool> ExecuteTemperatureGroupAsync(
        SelectedSubItemContext context,
        MeterTestControlPcbGroup group,
        IReadOnlyList<ControlPcbStationTarget> targets,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult,
        Action<MeterTestMeasurementData> recordMeasurement,
        CancellationToken cancellationToken)
    {
        if (context.SubItem.TemperatureSensorIndex is < 1 or > byte.MaxValue)
        {
            string invalid = $"温度传感器序号{context.SubItem.TemperatureSensorIndex}无效，允许1-255。";
            ApplyTargetFailure(context, targets, invalid, writeStationLog, applyResult);
            return false;
        }

        byte sensorIndex = (byte)context.SubItem.TemperatureSensorIndex;
        MeterTestControlPcbBatchResult batch = await controlPcbCommandService.SendAndCollectAsync(
            group,
            targets,
            target => ElectricEnergyMeterControlV2.BuildTemperatureReadPacket(target.MeterAddress, sensorIndex),
            target => $"温度获取，命令=0xCA，传感器={sensorIndex}，操作=0xAA",
            frame => ResolveResponseAddress(
                frame,
                MeterControlPcbProtocol.TemperatureCommand,
                MeterControlPcbProtocol.ReadOperation,
                targets.Select(target => target.MeterAddress)),
            TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
            TimeSpan.FromMilliseconds(Math.Max(0, context.SubItem.PacketIntervalMs)),
            writeStationLog,
            cancellationToken).ConfigureAwait(false);

        bool allPassed = true;
        foreach (ControlPcbStationTarget target in targets)
        {
            int rawTemperature = 0;
            string description = "温度获取应答解析失败。";
            bool hasResponse = batch.Responses.TryGetValue(target.MeterAddress, out byte[]? response);
            bool parsed = hasResponse && ElectricEnergyMeterControlV2.TryParseTemperatureReadResponse(
                response!,
                target.MeterAddress,
                sensorIndex,
                out rawTemperature,
                out description);
            if (!parsed)
            {
                rawTemperature = 0;
                description = hasResponse ? "温度获取应答解析失败。" : "温度获取无应答。";
            }

            allPassed &= parsed;
            string message = parsed
                ? $"温度传感器{sensorIndex}通信正常，温度原始值={rawTemperature}；协议未定义缩放比例；结论：合格。"
                : $"{description}；结论：不合格。";
            if (parsed)
            {
                recordMeasurement(new MeterTestMeasurementData(
                    target.StationNo,
                    context.TestItemName,
                    context.SubItem.Name,
                    $"温度传感器{sensorIndex}原始值",
                    1,
                    rawTemperature,
                    rawTemperature.ToString(),
                    "raw",
                    null,
                    "0xCA返回"));
            }

            writeStationLog(target.StationNo, new[] { message, MeterTestLogText.Separator });
            applyResult(target.StationNo, context, parsed, message);
        }

        return allPassed;
    }

    /// <summary>调用ElectricEnergyMeterControlV2中的协议解析器校验0x84/0x86应答。</summary>
    private static bool ParseDetectionResponse(
        byte[] response,
        byte meterAddress,
        byte operation,
        bool isShortCircuit,
        out byte resultCode,
        out string description)
    {
        return isShortCircuit
            ? ElectricEnergyMeterControlV2.TryParseShortCircuitDetectionResponse(
                response, meterAddress, operation, out resultCode, out description)
            : ElectricEnergyMeterControlV2.TryParseOpenCircuitDetectionResponse(
                response, meterAddress, operation, out resultCode, out description);
    }

    /// <summary>只接收V2指定命令、操作码和当前目标表位的设备自检应答。</summary>
    private static byte? ResolveResponseAddress(
        byte[] frame,
        byte command,
        byte operation,
        IEnumerable<byte> expectedAddresses)
    {
        return MeterTestControlPcbCommandService.TryGetDataItems(
                   frame,
                   MeterControlPcbProtocolVersion.V2.ToString(),
                   command,
                   out byte meterAddress,
                   out byte[] dataItems) &&
               expectedAddresses.Contains(meterAddress) &&
               dataItems.Length > 0 &&
               dataItems[0] == operation
            ? meterAddress
            : null;
    }

    /// <summary>处理无法识别的deviceSelfCheckStep配置，并同步所有目标工位失败。</summary>
    private static bool FailUnknownStep(
        SelectedSubItemContext context,
        IReadOnlyList<ControlPcbStationTarget> targets,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult)
    {
        string message = $"不支持的deviceSelfCheckStep：{context.SubItem.DeviceSelfCheckStep}。";
        ApplyTargetFailure(context, targets, message, writeStationLog, applyResult);
        return false;
    }

    /// <summary>将公共失败同步到每个选中工位，且不阻断其它方案小项。</summary>
    private static void ApplyFailure(
        SelectedSubItemContext context,
        IEnumerable<StationCommunicationConfig> stations,
        string message,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult)
    {
        foreach (StationCommunicationConfig station in stations)
        {
            writeStationLog(station.StationNo, new[] { message, MeterTestLogText.Separator });
            applyResult(station.StationNo, context, false, message);
        }
    }

    /// <summary>将控制PCB分组级失败同步到该组每个目标工位。</summary>
    private static void ApplyTargetFailure(
        SelectedSubItemContext context,
        IEnumerable<ControlPcbStationTarget> targets,
        string message,
        Action<int, string[]> writeStationLog,
        Action<int, SelectedSubItemContext, bool, string> applyResult)
    {
        foreach (ControlPcbStationTarget target in targets)
        {
            writeStationLog(target.StationNo, new[] { message, MeterTestLogText.Separator });
            applyResult(target.StationNo, context, false, message);
        }
    }
}
