using System.Globalization;
using System.Text.RegularExpressions;

namespace ModelTest.MeterTest;

/// <summary>
/// JJG596-2026 有功基本误差测试点计算器。
/// 负责把 XML 测试条件和资产档案转换成升源参数、0x38脉冲参数、等待时间及误差限。
/// </summary>
public static class MeterTestBasicErrorCalculator
{
    private const decimal EnergyConversionFactor = 3_600_000m;
    private static readonly Regex CurrentRangeRegex = new(
        @"(?<imin>\d+(?:\.\d+)?)\s*-\s*(?<itr>\d+(?:\.\d+)?)\s*\(\s*(?<imax>\d+(?:\.\d+)?)\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex BaseCurrentWithMaximumRegex = new(
        @"(?<basic>\d+(?:\.\d+)?)\s*\(\s*(?<imax>\d+(?:\.\d+)?)\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 计算一个基本误差小项涉及的全部工位参数。
    /// 同一源同时输出时，所有选中工位的相制、相电压和测试电流必须一致。
    /// </summary>
    public static bool TryCreateExecutionPlan(
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        out MeterTestBasicErrorExecutionPlan? plan,
        out string? errorMessage)
    {
        plan = null;
        errorMessage = null;
        if (selectedStations.Count == 0)
        {
            errorMessage = "未选择基本误差测试工位。";
            return false;
        }

        if (!TryParseDirection(subItem.BasicErrorDirection, out bool reverseActive, out string directionText))
        {
            errorMessage = $"基本误差方向不支持：{subItem.BasicErrorDirection}。";
            return false;
        }

        if (!TryParsePowerFactor(
                subItem.BasicErrorPowerFactor,
                reverseActive,
                out decimal powerFactor,
                out decimal currentAngle,
                out string powerFactorText))
        {
            errorMessage = $"基本误差功率因数不支持：{subItem.BasicErrorPowerFactor}。";
            return false;
        }

        string phase = string.IsNullOrWhiteSpace(subItem.BasicErrorPhase)
            ? "H"
            : subItem.BasicErrorPhase.Trim().ToUpperInvariant();
        if (phase is not ("H" or "A" or "B" or "C"))
        {
            errorMessage = $"基本误差相别不支持：{subItem.BasicErrorPhase}。";
            return false;
        }

        if (subItem.BasicErrorVoltageMultiplier <= 0)
        {
            errorMessage = $"基本误差电压倍数必须大于0：{subItem.BasicErrorVoltageMultiplier}。";
            return false;
        }

        if (subItem.BasicErrorTestCount is < 1 or > 10)
        {
            errorMessage = $"0x38试验次数必须在1-10之间：{subItem.BasicErrorTestCount}。";
            return false;
        }

        if (subItem.BasicErrorPulseCount is < 0 or > 99)
        {
            errorMessage = $"0x38脉冲数只能为0（自动）或1-99：{subItem.BasicErrorPulseCount}。";
            return false;
        }

        int minimumWaitSeconds = Math.Max(1, subItem.BasicErrorMinimumWaitSeconds);
        int waitPaddingSeconds = Math.Max(0, subItem.BasicErrorWaitPaddingSeconds);
        List<MeterTestBasicErrorStationPlan> stationPlans = new();
        foreach (MeterTestStationCommunication station in selectedStations.OrderBy(item => item.StationNo))
        {
            if (!meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
            {
                errorMessage = $"工位{station.StationNo}没有资产档案。";
                return false;
            }

            if (!TryCreateStationPlan(
                    subItem,
                    archive,
                    phase,
                    directionText,
                    powerFactorText,
                    powerFactor,
                    minimumWaitSeconds,
                    waitPaddingSeconds,
                    out MeterTestBasicErrorStationPlan? stationPlan,
                    out errorMessage))
            {
                errorMessage = $"工位{station.StationNo}基本误差参数计算失败：{errorMessage}";
                return false;
            }

            stationPlans.Add(stationPlan!);
        }

        List<MeterTestSourcePhaseMode> phaseModes = stationPlans
            .Select(item => item.PhaseMode)
            .Distinct()
            .ToList();
        List<decimal> sourceVoltages = stationPlans
            .Select(item => item.SourcePhaseVoltage)
            .Distinct()
            .ToList();
        List<decimal> sourceCurrents = stationPlans
            .Select(item => item.TestCurrent)
            .Distinct()
            .ToList();
        List<decimal> basicCurrents = stationPlans
            .Select(item => item.BasicCurrent)
            .Distinct()
            .ToList();
        List<decimal> currentPercentages = stationPlans
            .Select(item => item.CurrentPercentage)
            .Distinct()
            .ToList();
        if (phaseModes.Count != 1 ||
            sourceVoltages.Count != 1 ||
            sourceCurrents.Count != 1 ||
            basicCurrents.Count != 1 ||
            currentPercentages.Count != 1)
        {
            errorMessage = "选中工位计算出的基本误差升源参数不一致："
                + string.Join("；", stationPlans.Select(item =>
                    $"工位{item.StationNo}={item.PhaseMode}/{item.SourcePhaseVoltage:0.######}V/"
                    + $"基本电流{item.BasicCurrent:0.#########}A/"
                    + $"测试电流{item.TestCurrent:0.#########}A/"
                    + $"{item.CurrentPercentage:0.#########}%"))
                + "。同一公共源无法同时输出不同参数，请统一资产信息或分批测试。";
            return false;
        }

        plan = new MeterTestBasicErrorExecutionPlan(
            subItem.Name,
            directionText,
            phase,
            powerFactorText,
            powerFactor,
            currentAngle,
            subItem.BasicErrorCurrentPoint,
            phaseModes[0],
            sourceVoltages[0],
            sourceCurrents[0],
            basicCurrents[0],
            subItem.BasicErrorVoltageMultiplier * 100m,
            currentPercentages[0],
            (byte)subItem.BasicErrorTestCount,
            stationPlans);
        return true;
    }

    private static bool TryCreateStationPlan(
        MeterTestSubItem subItem,
        MeterArchiveData archive,
        string phase,
        string directionText,
        string powerFactorText,
        decimal powerFactor,
        int minimumWaitSeconds,
        int waitPaddingSeconds,
        out MeterTestBasicErrorStationPlan? stationPlan,
        out string? errorMessage)
    {
        stationPlan = null;
        errorMessage = null;
        if (!TryResolvePhaseMode(archive.MeterType, out MeterTestSourcePhaseMode phaseMode))
        {
            errorMessage = $"电表类型无法识别：{archive.MeterType}。";
            return false;
        }

        if (!TryParseVoltage(archive.Voltage, phaseMode, out decimal phaseVoltage, out decimal lineVoltage))
        {
            errorMessage = $"额定电压无法解析：{archive.Voltage}。";
            return false;
        }

        string activeClass = NormalizeActiveClass(archive.ActiveClass);
        if (!TryParseCurrentSpecification(
                archive.Current,
                archive.AccessMode,
                activeClass,
                out MeterTestBasicErrorCurrentSpecification? currentSpecification,
                out errorMessage))
        {
            return false;
        }

        if (!TryResolveTestCurrent(
                subItem.BasicErrorCurrentPoint,
                currentSpecification!,
                out decimal testCurrent,
                out string currentPointText))
        {
            errorMessage = $"基本误差电流点不支持：{subItem.BasicErrorCurrentPoint}。";
            return false;
        }

        if (!TryParsePositiveNumber(archive.ActiveConstant, out decimal meterConstant))
        {
            errorMessage = $"有功常数无效：{archive.ActiveConstant}。";
            return false;
        }

        if (meterConstant != decimal.Truncate(meterConstant) || meterConstant > uint.MaxValue)
        {
            errorMessage = $"有功常数必须是1-{uint.MaxValue}之间的整数：{archive.ActiveConstant}。";
            return false;
        }

        decimal sourcePhaseVoltage = phaseVoltage * subItem.BasicErrorVoltageMultiplier;
        decimal currentPercentage = testCurrent / currentSpecification!.BasicCurrent * 100m;
        decimal power = CalculateActivePower(
            phaseMode,
            phase,
            sourcePhaseVoltage,
            lineVoltage * subItem.BasicErrorVoltageMultiplier,
            testCurrent,
            powerFactor);
        if (power <= 0)
        {
            errorMessage = "基本误差功率计算结果无效。";
            return false;
        }

        if (!TryResolveErrorLimit(
                activeClass,
                subItem.BasicErrorCurrentPoint,
                powerFactorText,
                subItem.BasicErrorLimit,
                out decimal errorLimit))
        {
            errorMessage = $"有功等级 {archive.ActiveClass} 没有可用的基本误差限。";
            return false;
        }

        int pulseCount = subItem.BasicErrorPulseCount;
        if (pulseCount == 0)
        {
            pulseCount = Math.Max(
                1,
                (int)Math.Ceiling(minimumWaitSeconds * meterConstant * power / EnergyConversionFactor));
        }

        if (pulseCount > 99)
        {
            errorMessage = $"保证不少于{minimumWaitSeconds}s需要{pulseCount}个脉冲，超过0x38协议上限99个。";
            return false;
        }

        decimal singleTestSeconds = EnergyConversionFactor * pulseCount / (meterConstant * power);
        decimal totalTestSeconds = singleTestSeconds * subItem.BasicErrorTestCount;
        int waitSeconds = Math.Max(
            minimumWaitSeconds,
            (int)Math.Ceiling(totalTestSeconds + waitPaddingSeconds));
        string calculationNote =
            $"{directionText}/{phase}/{powerFactorText}/1U/{currentPointText}，"
            + $"电流规格={currentSpecification!.Description}，测试电流={testCurrent:0.#########}A，"
            + $"Adj电压={subItem.BasicErrorVoltageMultiplier * 100m:0.######}%，"
            + $"Adj电流={testCurrent:0.#########}/{currentSpecification.BasicCurrent:0.#########}×100={currentPercentage:0.#########}%，"
            + $"功率={power:0.######}W，表常数={meterConstant:0.######}，"
            + $"脉冲数={pulseCount}，次数={subItem.BasicErrorTestCount}，"
            + $"理论时间={totalTestSeconds:0.###}s，结果余量={waitPaddingSeconds}s，等待={waitSeconds}s，"
            + $"误差限=±{errorLimit:0.######}%";

        stationPlan = new MeterTestBasicErrorStationPlan(
            archive.StationNo,
            phaseMode,
            sourcePhaseVoltage,
            testCurrent,
            currentSpecification.BasicCurrent,
            currentPercentage,
            power,
            meterConstant,
            (byte)pulseCount,
            (byte)subItem.BasicErrorTestCount,
            errorLimit,
            singleTestSeconds,
            waitSeconds,
            calculationNote);
        return true;
    }

    /// <summary>
    /// 解析资产电流规格。优先使用 Imin-Itr(Imax)A；只有基本电流时按接入方式推导。
    /// 直接式：Itr=Ib/10、Imax=12Ib、Imin=0.5Itr(A级)或0.4Itr(B/C/D级)；
    /// 互感式：Itr=In/20、Imax=4In、Imin=0.2Itr。
    /// </summary>
    public static bool TryParseCurrentSpecification(
        string currentText,
        string accessMode,
        string activeClass,
        out MeterTestBasicErrorCurrentSpecification? specification,
        out string? errorMessage)
    {
        specification = null;
        errorMessage = null;
        string normalized = currentText?.Trim() ?? string.Empty;
        bool isTransformer = accessMode?.Contains("互感", StringComparison.OrdinalIgnoreCase) == true;
        bool isDirect = accessMode?.Contains("直接", StringComparison.OrdinalIgnoreCase) == true;
        if (!isTransformer && !isDirect)
        {
            errorMessage = $"接入方式无法识别：{accessMode}。";
            return false;
        }

        Match rangeMatch = CurrentRangeRegex.Match(normalized);
        if (rangeMatch.Success &&
            TryParsePositiveNumber(rangeMatch.Groups["imin"].Value, out decimal rangeImin) &&
            TryParsePositiveNumber(rangeMatch.Groups["itr"].Value, out decimal rangeItr) &&
            TryParsePositiveNumber(rangeMatch.Groups["imax"].Value, out decimal rangeImax) &&
            rangeImin <= rangeItr &&
            rangeItr <= rangeImax)
        {
            decimal rangeBasicCurrent = rangeItr * (isTransformer ? 20m : 10m);
            specification = new MeterTestBasicErrorCurrentSpecification(
                rangeImin,
                rangeItr,
                rangeImax,
                rangeBasicCurrent,
                $"资产完整规格 {rangeImin:0.######}-{rangeItr:0.######}({rangeImax:0.######})A，"
                + $"基本电流{(isTransformer ? "In" : "Ib")}={rangeBasicCurrent:0.######}A");
            return true;
        }

        decimal basicCurrent;
        decimal? configuredMaximum = null;
        Match baseWithMaximumMatch = BaseCurrentWithMaximumRegex.Match(normalized);
        if (baseWithMaximumMatch.Success &&
            TryParsePositiveNumber(baseWithMaximumMatch.Groups["basic"].Value, out basicCurrent) &&
            TryParsePositiveNumber(baseWithMaximumMatch.Groups["imax"].Value, out decimal maximumCurrent))
        {
            configuredMaximum = maximumCurrent;
        }
        else if (!TryParsePositiveNumber(normalized, out basicCurrent))
        {
            errorMessage = $"额定/基本电流无法解析：{currentText}。";
            return false;
        }

        decimal itr = basicCurrent / (isTransformer ? 20m : 10m);
        decimal imin = isTransformer
            ? itr * 0.2m
            : itr * (activeClass == "A" ? 0.5m : 0.4m);
        decimal imax = configuredMaximum ?? basicCurrent * (isTransformer ? 4m : 12m);
        if (imin <= 0 || itr <= 0 || imax < itr)
        {
            errorMessage = $"由资产电流推导出的 Imin/Itr/Imax 无效：{currentText}。";
            return false;
        }

        specification = new MeterTestBasicErrorCurrentSpecification(
            imin,
            itr,
            imax,
            basicCurrent,
            $"由{(isTransformer ? "In" : "Ib")}={basicCurrent:0.######}A推导"
            + $" Imin={imin:0.######}A、Itr={itr:0.######}A、Imax={imax:0.######}A");
        return true;
    }

    private static bool TryResolveTestCurrent(
        string currentPoint,
        MeterTestBasicErrorCurrentSpecification specification,
        out decimal current,
        out string normalizedPoint)
    {
        normalizedPoint = currentPoint?.Trim() ?? string.Empty;
        current = normalizedPoint.ToUpperInvariant() switch
        {
            "IMIN" => specification.Imin,
            "ITR" => specification.Itr,
            "10ITR" => specification.Itr * 10m,
            "0.5IMAX" => specification.Imax * 0.5m,
            "IMAX" => specification.Imax,
            "1.2IMAX" => specification.Imax * 1.2m,
            _ => 0m
        };
        return current > 0;
    }

    private static bool TryResolveErrorLimit(
        string activeClass,
        string currentPoint,
        string powerFactor,
        decimal configuredLimit,
        out decimal limit)
    {
        if (configuredLimit > 0)
        {
            limit = configuredLimit;
            return true;
        }

        bool lowCurrentRange = currentPoint.Equals("Imin", StringComparison.OrdinalIgnoreCase);
        bool unityPowerFactor = powerFactor.Equals("1.0", StringComparison.OrdinalIgnoreCase);
        limit = (lowCurrentRange, unityPowerFactor, activeClass) switch
        {
            (false, true, "A") => 2.0m,
            (false, true, "B") => 1.0m,
            (false, true, "C") => 0.5m,
            (false, true, "D") => 0.2m,
            (false, false, "A") => 2.0m,
            (false, false, "B") => 1.0m,
            (false, false, "C") => 0.6m,
            (false, false, "D") => 0.3m,
            (true, true, "A") => 2.5m,
            (true, true, "B") => 1.5m,
            (true, true, "C") => 1.0m,
            (true, true, "D") => 0.4m,
            (true, false, "A") => 2.5m,
            (true, false, "B") => 1.5m,
            (true, false, "C") => 1.0m,
            (true, false, "D") => 0.5m,
            _ => 0m
        };
        return limit > 0;
    }

    private static decimal CalculateActivePower(
        MeterTestSourcePhaseMode phaseMode,
        string phase,
        decimal phaseVoltage,
        decimal lineVoltage,
        decimal current,
        decimal powerFactor)
    {
        if (phaseMode == MeterTestSourcePhaseMode.SinglePhase)
            return phaseVoltage * current * powerFactor;

        if (phase == "H")
            return (decimal)Math.Sqrt(3d) * lineVoltage * current * powerFactor;

        return phaseVoltage * current * powerFactor;
    }

    private static bool TryParsePowerFactor(
        string value,
        bool reverseActive,
        out decimal powerFactor,
        out decimal currentAngle,
        out string normalized)
    {
        normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        powerFactor = normalized switch
        {
            "1" or "1.0" => 1m,
            "0.5L" => 0.5m,
            "0.8C" => 0.8m,
            _ => 0m
        };
        if (powerFactor <= 0)
        {
            currentAngle = 0;
            return false;
        }

        normalized = normalized is "1" ? "1.0" : normalized;
        currentAngle = (reverseActive, normalized) switch
        {
            (false, "1.0") => 0m,
            (false, "0.5L") => 60m,
            (false, "0.8C") => 323.130102m,
            (true, "1.0") => 180m,
            (true, "0.5L") => 120m,
            (true, "0.8C") => 216.869898m,
            _ => 0m
        };
        return true;
    }

    private static bool TryParseDirection(string value, out bool reverseActive, out string directionText)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Equals("ForwardActive", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("正", StringComparison.Ordinal))
        {
            reverseActive = false;
            directionText = "正向有功";
            return true;
        }

        if (normalized.Equals("ReverseActive", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("反", StringComparison.Ordinal))
        {
            reverseActive = true;
            directionText = "反向有功";
            return true;
        }

        reverseActive = false;
        directionText = string.Empty;
        return false;
    }

    private static bool TryResolvePhaseMode(string meterType, out MeterTestSourcePhaseMode phaseMode)
    {
        if (meterType?.Contains("单相", StringComparison.OrdinalIgnoreCase) == true)
        {
            phaseMode = MeterTestSourcePhaseMode.SinglePhase;
            return true;
        }

        if (meterType?.Contains("三相", StringComparison.OrdinalIgnoreCase) == true)
        {
            phaseMode = MeterTestSourcePhaseMode.ThreePhase;
            return true;
        }

        phaseMode = MeterTestSourcePhaseMode.ThreePhase;
        return false;
    }

    private static bool TryParseVoltage(
        string value,
        MeterTestSourcePhaseMode phaseMode,
        out decimal phaseVoltage,
        out decimal lineVoltage)
    {
        phaseVoltage = 0;
        lineVoltage = 0;
        string normalized = (value ?? string.Empty).Trim().Replace("×", "x", StringComparison.OrdinalIgnoreCase);
        bool hasThreePhasePrefix = normalized.StartsWith("3x", StringComparison.OrdinalIgnoreCase);
        if (hasThreePhasePrefix)
        {
            normalized = normalized[2..];
        }

        MatchCollection matches = Regex.Matches(normalized, @"\d+(?:\.\d+)?");
        if (matches.Count == 0 ||
            !decimal.TryParse(matches[0].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out phaseVoltage) ||
            phaseVoltage <= 0)
        {
            return false;
        }

        if (matches.Count > 1 &&
            decimal.TryParse(matches[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal parsedLineVoltage))
        {
            lineVoltage = parsedLineVoltage;
        }
        else if (phaseMode == MeterTestSourcePhaseMode.ThreePhase && !hasThreePhasePrefix)
        {
            lineVoltage = phaseVoltage * (decimal)Math.Sqrt(3d);
        }
        else
        {
            lineVoltage = phaseVoltage;
        }

        return lineVoltage > 0;
    }

    private static string NormalizeActiveClass(string value)
    {
        Match match = Regex.Match(value?.ToUpperInvariant() ?? string.Empty, @"[A-D]");
        return match.Success ? match.Value : string.Empty;
    }

    private static bool TryParsePositiveNumber(string value, out decimal number)
    {
        number = 0;
        Match match = Regex.Match(value ?? string.Empty, @"\d+(?:\.\d+)?");
        return match.Success &&
            decimal.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
            number > 0;
    }
}

/// <summary>一次基本误差测试小项的统一执行参数。</summary>
public sealed record MeterTestBasicErrorExecutionPlan(
    string TestPointName,
    string Direction,
    string Phase,
    string PowerFactorText,
    decimal PowerFactor,
    decimal CurrentAngle,
    string CurrentPoint,
    MeterTestSourcePhaseMode PhaseMode,
    decimal SourcePhaseVoltage,
    decimal SourceCurrent,
    decimal BasicCurrent,
    decimal VoltagePercentage,
    decimal CurrentPercentage,
    byte TestCount,
    IReadOnlyList<MeterTestBasicErrorStationPlan> Stations);

/// <summary>单个工位的基本误差计算结果和协议参数。</summary>
public sealed record MeterTestBasicErrorStationPlan(
    int StationNo,
    MeterTestSourcePhaseMode PhaseMode,
    decimal SourcePhaseVoltage,
    decimal TestCurrent,
    decimal BasicCurrent,
    decimal CurrentPercentage,
    decimal ActivePower,
    decimal MeterConstant,
    byte PulseCount,
    byte TestCount,
    decimal ErrorLimit,
    decimal SingleTestSeconds,
    int WaitSeconds,
    string CalculationNote);

/// <summary>资产信息解析得到的 Imin、Itr 和 Imax。</summary>
public sealed record MeterTestBasicErrorCurrentSpecification(
    decimal Imin,
    decimal Itr,
    decimal Imax,
    decimal BasicCurrent,
    string Description);
