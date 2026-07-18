using System;
using System.Collections.Generic;
using System.Globalization;
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
public sealed class MeterTestSourceControlService
{
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
        CancellationToken cancellationToken)
    {
        LogMessage.Debug($"[源控制] 开始执行：小项={subItem.Name}，绑定配置={subItem.SourceControlConfig}，选中工位={FormatStations(selectedStations)}");

        using XYCtr xyCtr = new();
        SourceControlExecutionState state = await Task.Run(
            () =>
            {
                try
                {
                    return ExecuteCore(planConfig, subItem, selectedStations, meterArchives, xyCtr);
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

        MeterTestSourceControlResult verifyResult = await VerifySourceRaisedAsync(xyCtr, state, cancellationToken).ConfigureAwait(false);

        return verifyResult;
    }

    /// <summary>
    /// 执行源控制的同步核心流程。
    /// </summary>
    private static SourceControlExecutionState ExecuteCore(
        MeterTestPlanConfig planConfig,
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        XYCtr xyCtr)
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

        if (!TryResolvePhaseMode(sourceConfig, selectedStations, meterArchives, out MeterTestSourcePhaseMode phaseMode, out string phaseNote, out string? errorMessage))
        {
            LogMessage.Error($"[源控制] 配置 {sourceConfig.Name} 电表类型判定失败：{errorMessage}", null);
            return SourceControlExecutionState.Fail(errorMessage ?? "源控制参数解析失败。");
        }

        LogMessage.Debug($"[源控制] 配置 {sourceConfig.Name} 电表类型判定完成：{phaseNote}");

        if (!TryResolveSourceVoltage(sourceConfig, selectedStations, meterArchives, out string sourceVoltage, out string voltageNote, out string? voltageError))
        {
            LogMessage.Error($"[源控制] 配置 {sourceConfig.Name} 电压判定失败：{voltageError}", null);
            return SourceControlExecutionState.Fail(voltageError ?? "源控制电压参数解析失败。");
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
            (bool openSuccess, int openResult) = xyCtr.CallOpenComm(sourceConfig.SourcePort);
            if (!openSuccess)
            {
                LogMessage.Error($"[源控制] 打开源串口失败：配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}，返回值={openResult}", null);
                return SourceControlExecutionState.Fail($"打开源串口失败，配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}，返回值={openResult}");
            }

            LogMessage.Info($"[源控制] 打开源串口成功：配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}，返回值={openResult}");
        }
        else
        {
            LogMessage.Debug($"[源控制] 源串口已打开，跳过重复打开：配置={sourceConfig.Name}，Port={sourceConfig.SourcePort}");
        }

        MeterTestSourceControlResult result = ExecuteSourceControl(xyCtr, sourceConfig, phaseMode, sourceVoltage);
        LogMessage.Debug(result.Success
            ? $"[源控制] 升源指令执行完成：{result.Message}"
            : $"[源控制] 升源指令执行失败：{result.Message}");
        string finalMessage = string.IsNullOrWhiteSpace(phaseNote)
            ? result.Message
            : $"{result.Message}；{phaseNote}";

        return result.Success
            ? SourceControlExecutionState.Executed(new MeterTestSourceControlResult(true, finalMessage), sourceConfig.Name, phaseMode, sourceVoltage)
            : SourceControlExecutionState.Fail(finalMessage);
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
        string sourceVoltage)
    {
        if (!Enum.TryParse(config.InterfaceType, true, out MeterTestSourceInterfaceType interfaceType))
        {
            return MeterTestSourceControlResult.Fail($"源控制配置 {config.Name} 的 interfaceType={config.InterfaceType} 不支持。");
        }

        return interfaceType switch
        {
            MeterTestSourceInterfaceType.AnyUIOutput => ExecuteAnyUiOutput(xyCtr, config, phaseMode, sourceVoltage),
            MeterTestSourceInterfaceType.Adj => ExecuteAdjOutput(xyCtr, config),
            MeterTestSourceInterfaceType.RangeOutputUI => ExecuteRangeOutputUi(xyCtr, config, phaseMode),
            MeterTestSourceInterfaceType.ShutPowerSource => ExecuteShutPowerSource(xyCtr, config),
            _ => MeterTestSourceControlResult.Fail($"源控制接口 {interfaceType} 暂未实现。")
        };
    }

    /// <summary>
    /// 调用 AnyUIOutput 接口进行升源。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteAnyUiOutput(
        XYCtr xyCtr,
        MeterTestSourceControlConfig config,
        MeterTestSourcePhaseMode phaseMode,
        string sourceVoltage)
    {
        string ua = NormalizeSourceVoltage(sourceVoltage);
        string command = phaseMode == MeterTestSourcePhaseMode.SinglePhase
            ? string.Join("_", ua, "0", "0", "0", "0", "0", "0", "0", "0", Normalize(config.Uab, "120"), Normalize(config.Uac, "240"))
            : string.Join("_", ua, ua, ua, "0", "0", "0", "0", "0", "0", Normalize(config.Uab, "120"), Normalize(config.Uac, "240"));

        LogMessage.Debug($"[源控制] AnyUIOutput 下发：配置={config.Name}，phaseMode={phaseMode}，sourceVoltage={ua}，command={command}，pulse={config.Pulse}");
        (bool success, int result) = xyCtr.CallAnyUIOutput(command, config.Pulse);
        return success
            ? MeterTestSourceControlResult.Ok($"升源成功：配置={config.Name}，接口=AnyUIOutput，参数={command}，Pulse={config.Pulse}，返回值={result}")
            : MeterTestSourceControlResult.Fail($"升源失败：配置={config.Name}，接口=AnyUIOutput，参数={command}，Pulse={config.Pulse}，返回值={result}");
    }

    /// <summary>
    /// 调用 Adj 接口进行升源。
    /// </summary>
    private static MeterTestSourceControlResult ExecuteAdjOutput(XYCtr xyCtr, MeterTestSourceControlConfig config)
    {
        string powerFactorCode = XYCtr.ADJLC_CHANGE(config.PowerFactor);
        if (powerFactorCode == "-1")
        {
            LogMessage.Error($"[源控制] ADJ 功率因数不支持：{config.PowerFactor}", null);
            return MeterTestSourceControlResult.Fail($"ADJ 升源失败：功率因数 {config.PowerFactor} 不支持。");
        }

        string phase = string.IsNullOrWhiteSpace(config.Phase) ? "H" : config.Phase.Trim();
        string command = $"Adj_{config.Voltage}_{config.Current}_{phase}_{powerFactorCode}_{config.Pulse}_E";
        LogMessage.Debug($"[源控制] Adj 下发：配置={config.Name}，command={command}");
        (bool success, int result) = xyCtr.CallSendCommand(command, true);
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
        MeterTestSourcePhaseMode phaseMode)
    {
        SourcePhaseValues values = BuildSourcePhaseValues(config, phaseMode);
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
    /// 升源指令成功后等待源稳定，再读取标准表数据判断源是否真正升起。
    /// 标准表返回 15 组数据：0-2 电压，3-5 电流，6-8 相角，9-11 有功，12-14 无功。
    /// </summary>
    private static async Task<MeterTestSourceControlResult> VerifySourceRaisedAsync(
        XYCtr xyCtr,
        SourceControlExecutionState state,
        CancellationToken cancellationToken)
    {
        LogMessage.Debug($"[源控制] 升源指令已下发，等待 10s 后读取标准表：配置={state.SourceConfigName}，phaseMode={state.PhaseMode}，资产电压={state.SourceVoltage}");
        await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);

        byte[] standValueBuffer = new byte[1024];
        Array.Clear(standValueBuffer, 0, standValueBuffer.Length);

        LogMessage.Debug("[源控制] 开始读取标准表参数：CallReadStandValue(model1)");
        (bool readSuccess, int readResult) = await xyCtr
            .CallReadStandValueAsync("model1", standValueBuffer, TimeSpan.FromSeconds(5))
            .ConfigureAwait(false);

        if (!readSuccess)
        {
            string message = $"{state.Result.Message}；读取标准表失败，返回值={readResult}";
            LogMessage.Error($"[源控制] {message}", null);
            return MeterTestSourceControlResult.Fail(message);
        }

        string rawStandValue = Encoding.Default.GetString(standValueBuffer).TrimEnd('\0', '\r', '\n', ' ');
        List<string> standParts = ModelTool.SplitString(rawStandValue)
            .Select(item => item ?? string.Empty)
            .ToList();

        LogMessage.Debug($"[源控制] 标准表原始返回：{rawStandValue}");
        LogMessage.Debug($"[源控制] 标准表分割结果：共 {standParts.Count} 项，{string.Join(" | ", standParts.Take(15))}");

        if (standParts.Count < 15)
        {
            string message = $"{state.Result.Message}；标准表数据项不足，期望15项，实际{standParts.Count}项。";
            LogMessage.Error($"[源控制] {message}", null);
            return MeterTestSourceControlResult.Fail(message);
        }

        IReadOnlyDictionary<string, string> standValues = BuildStandValueMap(standParts);
        if (!TryParseNumber(state.SourceVoltage, out decimal assetVoltage) || assetVoltage <= 0)
        {
            string message = $"{state.Result.Message}；资产电压解析失败：{state.SourceVoltage}";
            LogMessage.Error($"[源控制] {message}", null);
            return new MeterTestSourceControlResult(false, message)
            {
                StandValues = standValues
            };
        }

        if (!TryGetStandardVoltageForJudgement(standParts, state.PhaseMode, out decimal standardVoltage, out string standardVoltageText, out string? parseError))
        {
            string message = $"{state.Result.Message}；{parseError}";
            LogMessage.Error($"[源控制] {message}", null);
            return new MeterTestSourceControlResult(false, message)
            {
                StandValues = standValues
            };
        }

        decimal ratio = standardVoltage / assetVoltage;
        bool sourceRaised = ratio < 1m;
        string judgementText = sourceRaised ? "源升成功，继续执行测试。" : "源升失败，停止后续测试。";
        string finalMessage = $"{state.Result.Message}；读取标准表成功：电压={standardVoltageText}，资产电压={assetVoltage:0.######}，比值={ratio:0.######}，{judgementText}";

        LogMessage.Debug($"[源控制] 标准表电压判断：phaseMode={state.PhaseMode}，标准表电压={standardVoltageText}，资产电压={assetVoltage:0.######}，比值={ratio:0.######}，结果={judgementText}");
        return new MeterTestSourceControlResult(sourceRaised, finalMessage)
        {
            StandValues = standValues
        };
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
    /// 根据单相/三相取参与升源判断的标准表电压。
    /// 单相使用 Ua；三相使用 Ua/Ub/Uc 平均值，同时日志保留三相原值。
    /// </summary>
    private static bool TryGetStandardVoltageForJudgement(
        IReadOnlyList<string> standParts,
        MeterTestSourcePhaseMode phaseMode,
        out decimal standardVoltage,
        out string standardVoltageText,
        out string? errorMessage)
    {
        standardVoltage = 0;
        standardVoltageText = string.Empty;
        errorMessage = null;

        if (phaseMode == MeterTestSourcePhaseMode.SinglePhase)
        {
            if (!TryParseNumber(standParts[0], out standardVoltage))
            {
                errorMessage = $"标准表 Ua 电压解析失败：{standParts[0]}";
                return false;
            }

            standardVoltageText = $"Ua={standardVoltage:0.######}";
            return true;
        }

        if (!TryParseNumber(standParts[0], out decimal ua) ||
            !TryParseNumber(standParts[1], out decimal ub) ||
            !TryParseNumber(standParts[2], out decimal uc))
        {
            errorMessage = $"标准表三相电压解析失败：Ua={standParts[0]}，Ub={standParts[1]}，Uc={standParts[2]}";
            return false;
        }

        standardVoltage = (ua + ub + uc) / 3m;
        standardVoltageText = $"Ua={ua:0.######}, Ub={ub:0.######}, Uc={uc:0.######}, Avg={standardVoltage:0.######}";
        return true;
    }

    /// <summary>
    /// 生成升源需要的相量参数。
    /// 单相和三相的默认值不同，这里统一封装。
    /// </summary>
    private static SourcePhaseValues BuildSourcePhaseValues(
        MeterTestSourceControlConfig config,
        MeterTestSourcePhaseMode phaseMode)
    {
        string voltage = Normalize(config.Voltage, "220");
        string current = Normalize(config.Current, "5");

        if (phaseMode == MeterTestSourcePhaseMode.SinglePhase)
        {
            return new SourcePhaseValues(
                Normalize(config.VoltageA, voltage),
                "0",
                "0",
                Normalize(config.CurrentA, current),
                "0",
                "0");
        }

        return new SourcePhaseValues(
            Normalize(config.VoltageA, voltage),
            Normalize(config.VoltageB, voltage),
            Normalize(config.VoltageC, voltage),
            Normalize(config.CurrentA, current),
            Normalize(config.CurrentB, current),
            Normalize(config.CurrentC, current));
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

    /// <summary>
    /// 源控制指令执行后的上下文。只有真正下发过源控制指令时才需要继续读取标准表校验。
    /// </summary>
    private sealed record SourceControlExecutionState(
        MeterTestSourceControlResult Result,
        string SourceConfigName,
        MeterTestSourcePhaseMode PhaseMode,
        string SourceVoltage,
        bool ShouldVerify)
    {
        public static SourceControlExecutionState Ok(string message)
        {
            return new SourceControlExecutionState(MeterTestSourceControlResult.Ok(message), string.Empty, MeterTestSourcePhaseMode.ThreePhase, string.Empty, false);
        }

        public static SourceControlExecutionState Fail(string message)
        {
            return new SourceControlExecutionState(MeterTestSourceControlResult.Fail(message), string.Empty, MeterTestSourcePhaseMode.ThreePhase, string.Empty, false);
        }

        public static SourceControlExecutionState Executed(
            MeterTestSourceControlResult result,
            string sourceConfigName,
            MeterTestSourcePhaseMode phaseMode,
            string sourceVoltage)
        {
            return new SourceControlExecutionState(result, sourceConfigName, phaseMode, sourceVoltage, true);
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
