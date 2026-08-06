using System.Globalization;
using System.Text.RegularExpressions;

namespace ModelTest.MeterTest;

/// <summary>
/// JJG596-2026 有功基本误差测试点计算器。
/// 负责把 XML 测试条件和资产档案转换成升源参数、0x38脉冲参数、等待时间及规程误差限。
/// </summary>
public static class MeterTestBasicErrorCalculator
{
    private const decimal EnergyConversionFactor = 3_600_000m;

    /// <summary>
    /// 计算一个基本误差小项涉及的全部工位参数。
    /// 同一源同时输出时，所有选中工位的相制、相电压和测试电流必须一致。
    /// </summary>
    public static bool TryCreateExecutionPlan(
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestStationCommunication> selectedStations,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        IReadOnlyList<MeterTestPowerFactorAngleData> powerFactorAngles,
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

        if (!TryParseDirection(
                subItem.BasicErrorDirection,
                out MeterTestErrorEnergyType energyType,
                out bool reverseActive,
                out string directionText))
        {
            errorMessage = $"基本误差方向不支持：{subItem.BasicErrorDirection}。";
            return false;
        }

        if (!TryParsePowerFactor(
                subItem.BasicErrorPowerFactor,
                energyType,
                reverseActive,
                powerFactorAngles,
                out decimal powerFactor,
                out decimal currentAngle,
                out string powerFactorText))
        {
            errorMessage =
                $"基本误差功率因数或FA角度配置不支持："
                + $"{subItem.BasicErrorDirection}/{subItem.BasicErrorPowerFactor}。"
                + "请检查数据库表 MeterTestPowerFactorAngle。";
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

        if (subItem.BasicErrorPulseCount is < 0 or > MeterTestBasicErrorDefaults.MaxPulseCount)
        {
            errorMessage = $"0x38脉冲数只能为0（自动）或1-{MeterTestBasicErrorDefaults.MaxPulseCount}：{subItem.BasicErrorPulseCount}。";
            return false;
        }

        int minimumWaitSeconds = Math.Max(1, subItem.BasicErrorMinimumWaitSeconds);
        // 所有基本误差点使用同一个固定余量，避免现场XML中不同小项出现不一致等待时间。
        int waitPaddingSeconds = MeterTestBasicErrorDefaults.WaitPaddingSeconds;
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
                    energyType,
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
            energyType,
            stationPlans);
        return true;
    }

    /// <summary>根据单工位资产和测试点标记生成电压、电流、相角、功率、脉冲数及等待时间计划。</summary>
    private static bool TryCreateStationPlan(
        MeterTestSubItem subItem,
        MeterArchiveData archive,
        MeterTestErrorEnergyType energyType,
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
        string reactiveClass = NormalizeReactiveClass(archive.ReactiveClass);
        if (!TryParsePositiveNumber(archive.Current, out decimal basicCurrent))
        {
            errorMessage = $"基本电流无法解析：{archive.Current}。";
            return false;
        }

        if (!TryParseCurrentSpecification(
                archive.CurrentSpecification,
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
                basicCurrent,
                out decimal testCurrent,
                out string currentPointText))
        {
            errorMessage = $"基本误差电流点不支持：{subItem.BasicErrorCurrentPoint}。";
            return false;
        }

        string meterConstantText = energyType == MeterTestErrorEnergyType.Reactive
            ? archive.ReactiveConstant
            : archive.ActiveConstant;
        string meterConstantName = energyType == MeterTestErrorEnergyType.Reactive ? "无功常数" : "有功常数";
        if (!TryParsePositiveNumber(meterConstantText, out decimal meterConstant))
        {
            errorMessage = $"{meterConstantName}无效：{meterConstantText}。";
            return false;
        }

        if (meterConstant != decimal.Truncate(meterConstant) || meterConstant > uint.MaxValue)
        {
            errorMessage = $"{meterConstantName}必须是1-{uint.MaxValue}之间的整数：{meterConstantText}。";
            return false;
        }

        decimal sourcePhaseVoltage = phaseVoltage * subItem.BasicErrorVoltageMultiplier;
        // 电流点由独立电流规格解析。百分比仅用于日志和数据追溯，实际升源使用绝对电流值。
        decimal currentPercentage = testCurrent / basicCurrent * 100m;
        decimal power = energyType == MeterTestErrorEnergyType.Reactive
            ? CalculateReactivePower(
                phaseMode,
                phase,
                sourcePhaseVoltage,
                lineVoltage * subItem.BasicErrorVoltageMultiplier,
                testCurrent,
                powerFactor)
            : CalculateActivePower(
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

        if (!MeterTestStartingTestCalculator.TryCalculateStartingCurrent(
                archive,
                out decimal startingCurrent,
                out _,
                out string? startingCurrentError))
        {
            errorMessage = $"基本误差限所需Ist计算失败：{startingCurrentError}";
            return false;
        }

        MeterTestErrorLimitResult errorLimitResult = MeterTestErrorResultComparer.CalculateLimit(
            new MeterTestErrorLimitRequest(
                energyType,
                energyType == MeterTestErrorEnergyType.Reactive ? reactiveClass : activeClass,
                archive.AccessMode,
                powerFactorText,
                testCurrent,
                startingCurrent,
                currentSpecification!.Imin,
                currentSpecification.Itr,
                currentSpecification.Imax,
                basicCurrent,
                phase));
        if (!errorLimitResult.IsValid || !errorLimitResult.IsApplicable)
        {
            errorMessage = $"测试点 {subItem.Name} 无法计算规程误差限：{errorLimitResult.Message}";
            return false;
        }

        int pulseCount = subItem.BasicErrorPulseCount;
        if (pulseCount == 0)
        {
            pulseCount = Math.Max(
                1,
                (int)Math.Ceiling(minimumWaitSeconds * meterConstant * power / EnergyConversionFactor));
        }

        if (pulseCount > MeterTestBasicErrorDefaults.MaxPulseCount)
        {
            errorMessage = $"保证不少于{minimumWaitSeconds}s需要{pulseCount}个脉冲，超过0x38协议上限{MeterTestBasicErrorDefaults.MaxPulseCount}个。";
            return false;
        }

        // singleTestSeconds 已经包含0x38配置的脉冲数，总等待时间只再乘试验次数，
        // 不能在外层再乘一次脉冲数，否则会重复计算。
        decimal singleTestSeconds = EnergyConversionFactor * pulseCount / (meterConstant * power);
        decimal totalTestSeconds = singleTestSeconds * subItem.BasicErrorTestCount;
        int roundedTheorySeconds = (int)Math.Ceiling(singleTestSeconds);
        int singleRoundWaitSeconds = Math.Max(
            minimumWaitSeconds,
            roundedTheorySeconds);
        long totalWaitSeconds = (long)singleRoundWaitSeconds * subItem.BasicErrorTestCount + waitPaddingSeconds;
        if (totalWaitSeconds > int.MaxValue)
        {
            errorMessage = $"基本误差总等待时间超出系统上限：{totalWaitSeconds}s。";
            return false;
        }

        int waitSeconds = (int)totalWaitSeconds;
        string calculationNote =
            $"{directionText}/{phase}/{powerFactorText}/1U/{currentPointText}，"
            + $"电流规格={currentSpecification!.Description}，测试电流={testCurrent:0.#########}A，"
            + $"AnyUIOutput电压={sourcePhaseVoltage:0.######}V，"
            + $"AnyUIOutput电流={testCurrent:0.#########}A（相对基本电流{currentPercentage:0.#########}%），"
            + $"{(energyType == MeterTestErrorEnergyType.Reactive ? "无功功率" : "有功功率")}={power:0.######}W，"
            + $"{meterConstantName}={meterConstant:0.######}，"
            + $"脉冲数={pulseCount}，次数={subItem.BasicErrorTestCount}，"
            + $"单次理论时间={singleTestSeconds:0.###}s，向上取整并保证不少于{minimumWaitSeconds}s后={singleRoundWaitSeconds}s，"
            + $"总理论时间={totalTestSeconds:0.###}s，"
            + $"总等待={singleRoundWaitSeconds}s×次数{subItem.BasicErrorTestCount}+{waitPaddingSeconds}s余量={waitSeconds}s，"
            + $"最大允许误差=±{errorLimitResult.MaximumPermittedLimit:0.######}%，"
            + $"60%判定限=±{errorLimitResult.ComparisonLimit:0.######}%";

        stationPlan = new MeterTestBasicErrorStationPlan(
            archive.StationNo,
            phaseMode,
            sourcePhaseVoltage,
            testCurrent,
            basicCurrent,
            currentPercentage,
            energyType,
            power,
            meterConstant,
            (byte)pulseCount,
            (byte)subItem.BasicErrorTestCount,
            errorLimitResult,
            singleTestSeconds,
            singleRoundWaitSeconds,
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
        return MeterTestCurrentSpecificationParser.TryParse(
            currentText,
            accessMode,
            activeClass,
            out specification,
            out errorMessage);
    }

    /// <summary>将 Imin、Itr、10Itr、0.5Imax、Imax 或 1.2Imax 转换为实际测试电流。</summary>
    private static bool TryResolveTestCurrent(
        string currentPoint,
        MeterTestBasicErrorCurrentSpecification specification,
        decimal ratedCurrent,
        out decimal current,
        out string normalizedPoint)
    {
        normalizedPoint = currentPoint?.Trim() ?? string.Empty;
        current = normalizedPoint.ToUpperInvariant() switch
        {
            "0.05IN" => ratedCurrent * 0.05m,
            "0.1IN" => ratedCurrent * 0.1m,
            "0.2IN" => ratedCurrent * 0.2m,
            "0.5IN" => ratedCurrent * 0.5m,
            "1IN" or "IN" => ratedCurrent,
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

    /// <summary>按单相或三相测量单元、相电压、电流和功率因数计算当前点有功功率。</summary>
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

    /// <summary>按单相或三相测量单元、相电压、电流和无功功率因数计算当前点无功功率。</summary>
    private static decimal CalculateReactivePower(
        MeterTestSourcePhaseMode phaseMode,
        string phase,
        decimal phaseVoltage,
        decimal lineVoltage,
        decimal current,
        decimal reactiveFactor)
    {
        if (phaseMode == MeterTestSourcePhaseMode.SinglePhase)
            return phaseVoltage * current * reactiveFactor;

        if (phase == "H")
            return (decimal)Math.Sqrt(3d) * lineVoltage * current * reactiveFactor;

        return phaseVoltage * current * reactiveFactor;
    }

    /// <summary>解析 1.0、0.5L、0.8C 等功率因数文本，并返回数值及负载性质。</summary>
    private static bool TryParsePowerFactor(
        string value,
        MeterTestErrorEnergyType energyType,
        bool reverseActive,
        IReadOnlyList<MeterTestPowerFactorAngleData> powerFactorAngles,
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
            "0.25L" => 0.25m,
            "0.5C" => 0.5m,
            "0.25C" => 0.25m,
            _ => 0m
        };
        if (powerFactor <= 0)
        {
            currentAngle = 0;
            return false;
        }

        normalized = normalized is "1" ? "1.0" : normalized;
        if (energyType == MeterTestErrorEnergyType.Reactive)
        {
            // 无功点的“功率因数”按 sinφ 使用：1.0=>90°，0.5L=>+30°，0.5C=>-30°。
            double angle = Math.Asin((double)powerFactor) * 180d / Math.PI;
            if (normalized.EndsWith("C", StringComparison.OrdinalIgnoreCase))
            {
                angle = -angle;
            }

            currentAngle = decimal.Round((decimal)angle, 6, MidpointRounding.AwayFromZero);
            return true;
        }

        string direction = reverseActive ? "ReverseActive" : "ForwardActive";
        string normalizedPowerFactor = normalized;
        MeterTestPowerFactorAngleData? angleConfiguration = powerFactorAngles.FirstOrDefault(item =>
            string.Equals(item.Direction, direction, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.PowerFactor, normalizedPowerFactor, StringComparison.OrdinalIgnoreCase));
        if (angleConfiguration is null || angleConfiguration.CurrentAngle is < -180m or > 180m)
        {
            currentAngle = 0m;
            return false;
        }

        // IAJ/IBJ/ICJ直接使用数据库维护的有符号电压电流夹角，避免再次转换成0~360°。
        currentAngle = decimal.Round(angleConfiguration.CurrentAngle, 6, MidpointRounding.AwayFromZero);
        return true;
    }

    /// <summary>解析正有、反有或正无方向标记，并返回能量类型、反向标记及可读说明。</summary>
    private static bool TryParseDirection(
        string value,
        out MeterTestErrorEnergyType energyType,
        out bool reverseActive,
        out string directionText)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Equals("ForwardReactive", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("正无", StringComparison.Ordinal))
        {
            energyType = MeterTestErrorEnergyType.Reactive;
            reverseActive = false;
            directionText = "正向无功";
            return true;
        }

        if (normalized.Equals("ReverseReactive", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("反无", StringComparison.Ordinal))
        {
            energyType = MeterTestErrorEnergyType.Reactive;
            reverseActive = true;
            directionText = "反向无功";
            return true;
        }

        if (normalized.Equals("ForwardActive", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("正有", StringComparison.Ordinal) ||
            normalized.Contains("正", StringComparison.Ordinal))
        {
            energyType = MeterTestErrorEnergyType.Active;
            reverseActive = false;
            directionText = "正向有功";
            return true;
        }

        if (normalized.Equals("ReverseActive", StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("反有", StringComparison.Ordinal) ||
            normalized.Contains("反", StringComparison.Ordinal))
        {
            energyType = MeterTestErrorEnergyType.Active;
            reverseActive = true;
            directionText = "反向有功";
            return true;
        }

        energyType = MeterTestErrorEnergyType.Active;
        reverseActive = false;
        directionText = string.Empty;
        return false;
    }

    /// <summary>根据资产电表类型解析单相或三相源输出模式。</summary>
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

    /// <summary>解析额定电压文本；三相线电压按相制换算为源输出所需相电压。</summary>
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

    /// <summary>规范化有功准确度等级，去除“级”和多余空格并转为大写。</summary>
    private static string NormalizeActiveClass(string value)
    {
        Match match = Regex.Match(value?.ToUpperInvariant() ?? string.Empty, @"[A-E]");
        return match.Success ? match.Value : string.Empty;
    }

    /// <summary>规范化无功准确度等级，支持3、2、1、1S和0.5S。</summary>
    private static string NormalizeReactiveClass(string value)
    {
        string normalized = (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace("级", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase);
        if (normalized.Contains("0.5S", StringComparison.OrdinalIgnoreCase))
            return "0.5S";
        if (normalized.Contains("1S", StringComparison.OrdinalIgnoreCase))
            return "1S";

        Match match = Regex.Match(normalized, @"(?:^|[^0-9.])(3(?:\.0)?|2(?:\.0)?|1(?:\.0)?)(?:$|[^0-9.])");
        if (!match.Success)
        {
            match = Regex.Match(normalized, @"^(3(?:\.0)?|2(?:\.0)?|1(?:\.0)?)$");
        }

        return match.Success ? match.Groups[1].Value.TrimEnd('0').TrimEnd('.') : string.Empty;
    }

    /// <summary>从带单位文本中提取首个正十进制数。</summary>
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
    MeterTestErrorEnergyType EnergyType,
    IReadOnlyList<MeterTestBasicErrorStationPlan> Stations);

/// <summary>单个工位的基本误差计算结果和协议参数。</summary>
public sealed record MeterTestBasicErrorStationPlan(
    int StationNo,
    MeterTestSourcePhaseMode PhaseMode,
    decimal SourcePhaseVoltage,
    decimal TestCurrent,
    decimal BasicCurrent,
    decimal CurrentPercentage,
    MeterTestErrorEnergyType EnergyType,
    decimal Power,
    decimal MeterConstant,
    byte PulseCount,
    byte TestCount,
    MeterTestErrorLimitResult ErrorLimitResult,
    decimal SingleTestSeconds,
    int SingleRoundWaitSeconds,
    int WaitSeconds,
    string CalculationNote)
{
    /// <summary>规程表给出的最大允许误差绝对值。</summary>
    public decimal MaximumPermittedErrorLimit => ErrorLimitResult.MaximumPermittedLimit ?? 0m;

    /// <summary>实际判定使用的60%误差限绝对值。</summary>
    public decimal ErrorLimit => ErrorLimitResult.ComparisonLimit ?? 0m;
}
