using System.Globalization;

namespace ModelTest.MeterTest;

/// <summary>电能误差类型。</summary>
public enum MeterTestErrorEnergyType
{
    Active,
    Reactive
}

/// <summary>
/// 误差限计算输入。电流统一使用安培，误差和功率因数使用十进制文本。
/// 有功低电流段需要 StartingCurrent；无功计算使用 RatedCurrent 作为 In。
/// </summary>
public sealed record MeterTestErrorLimitRequest(
    MeterTestErrorEnergyType EnergyType,
    string AccuracyClass,
    string AccessMode,
    string PowerFactor,
    decimal Current,
    decimal StartingCurrent,
    decimal MinimumCurrent,
    decimal TransitionCurrent,
    decimal MaximumCurrent,
    decimal RatedCurrent,
    string Phase = "H");

/// <summary>规程误差限计算结果。</summary>
public sealed record MeterTestErrorLimitResult(
    bool IsValid,
    bool IsApplicable,
    string EnergyTypeText,
    string AccuracyClass,
    string PowerFactor,
    string CurrentRange,
    decimal? MaximumPermittedLimit,
    decimal? ComparisonLimit,
    decimal? LowerLimit,
    decimal? UpperLimit,
    string Message);

/// <summary>实测误差与规程限值的比较结果。</summary>
public sealed record MeterTestErrorComparisonResult(
    bool IsValid,
    bool IsApplicable,
    bool Passed,
    decimal MeasuredError,
    MeterTestErrorLimitResult Limit,
    string Message);

/// <summary>
/// JJG596-2026 误差结果比较算法。
/// 先计算规程最大允许误差，再统一乘以 60% 得到本系统实际使用的上下限。
/// </summary>
public static class MeterTestErrorResultComparer
{
    /// <summary>最大允许误差转为实际判定限的系数。</summary>
    public const decimal ComparisonRatio = 0.60m;

    /// <summary>根据能量类型计算误差限。</summary>
    public static MeterTestErrorLimitResult CalculateLimit(MeterTestErrorLimitRequest request)
    {
        return request.EnergyType == MeterTestErrorEnergyType.Active
            ? CalculateActiveLimit(request)
            : CalculateReactiveLimit(request);
    }

    /// <summary>计算误差限并与实测误差比较；上下边界均按合格处理。</summary>
    public static MeterTestErrorComparisonResult Compare(
        MeterTestErrorLimitRequest request,
        decimal measuredError)
    {
        return Compare(CalculateLimit(request), measuredError);
    }

    /// <summary>使用已经计算好的误差限比较实测误差，避免测试结束时重复解析参数。</summary>
    public static MeterTestErrorComparisonResult Compare(
        MeterTestErrorLimitResult limit,
        decimal measuredError)
    {
        if (!limit.IsValid || !limit.IsApplicable ||
            !limit.LowerLimit.HasValue || !limit.UpperLimit.HasValue)
        {
            string unavailableMessage = $"实测误差={Format(measuredError)}%，无法判定：{limit.Message}";
            return new MeterTestErrorComparisonResult(
                limit.IsValid,
                limit.IsApplicable,
                false,
                measuredError,
                limit,
                unavailableMessage);
        }

        decimal lower = limit.LowerLimit.Value;
        decimal upper = limit.UpperLimit.Value;
        bool passed = measuredError >= lower && measuredError <= upper;
        string reason = passed
            ? "实测误差位于60%判定区间内"
            : measuredError > upper
                ? $"实测误差超过上限{Format(upper)}%"
                : $"实测误差低于下限{Format(lower)}%";
        string message =
            $"实测误差={Format(measuredError)}%；最大允许误差区间=[{Format(-limit.MaximumPermittedLimit!.Value)}%, +{Format(limit.MaximumPermittedLimit.Value)}%]；"
            + $"60%判定区间=[{Format(lower)}%, +{Format(upper)}%]；{reason}，结论={(passed ? "合格" : "不合格")}。";
        return new MeterTestErrorComparisonResult(true, true, passed, measuredError, limit, message);
    }

    /// <summary>计算有功电能最大允许误差。</summary>
    private static MeterTestErrorLimitResult CalculateActiveLimit(MeterTestErrorLimitRequest request)
    {
        string accuracyClass = NormalizeActiveClass(request.AccuracyClass);
        string powerFactor = NormalizePowerFactor(request.PowerFactor);
        if (accuracyClass.Length == 0)
            return Invalid("有功等级必须为A、B、C、D或E。", "有功", accuracyClass, powerFactor);
        if (request.Current <= 0 || request.MinimumCurrent <= 0 || request.TransitionCurrent <= 0 ||
            request.MaximumCurrent <= 0 || request.MinimumCurrent > request.TransitionCurrent ||
            request.TransitionCurrent > request.MaximumCurrent)
        {
            return Invalid("有功电流参数无效，必须满足0<Imin≤Itr≤Imax。", "有功", accuracyClass, powerFactor);
        }

        decimal current = request.Current;
        decimal maximumTestCurrent = request.MaximumCurrent * 1.2m;
        if (current > maximumTestCurrent)
        {
            return NotApplicable(
                $"当前电流{Format(current)}A超过1.2Imax({Format(maximumTestCurrent)}A)，规程表未定义误差限。",
                "有功",
                accuracyClass,
                powerFactor,
                "I>1.2Imax");
        }

        // I=Itr 明确进入高电流段。既有方案包含1.2Imax过载点，该点沿用Itr-Imax段误差限。
        if (current >= request.TransitionCurrent)
        {
            string range = current > request.MaximumCurrent
                ? "Imax<I≤1.2Imax（过载点沿用Itr≤I≤Imax限值）"
                : "Itr≤I≤Imax";
            decimal? limit = ResolveActiveHighCurrentLimit(accuracyClass, powerFactor);
            return limit.HasValue
                ? Applicable("有功", accuracyClass, powerFactor, range, limit.Value)
                : NotApplicable("该等级、功率因数和高电流段组合按规程不测试。", "有功", accuracyClass, powerFactor, range);
        }

        // PF=1且I=Imin时按低电流公式；其他已定义功率因数的Imin点按中电流段处理。
        bool mediumCurrent = current > request.MinimumCurrent ||
            (current == request.MinimumCurrent && powerFactor != "1.0");
        if (mediumCurrent && current < request.TransitionCurrent)
        {
            string range = current == request.MinimumCurrent
                ? "I=Imin（非1.0功率因数按中电流段）"
                : "Imin<I<Itr";
            decimal? limit = ResolveActiveMediumCurrentLimit(accuracyClass, powerFactor);
            return limit.HasValue
                ? Applicable("有功", accuracyClass, powerFactor, range, limit.Value)
                : NotApplicable("该等级、功率因数和中电流段组合按规程不测试。", "有功", accuracyClass, powerFactor, range);
        }

        if (powerFactor != "1.0")
        {
            return NotApplicable(
                "Ist≤I≤Imin低电流段仅规定功率因数1.0的有功误差限。",
                "有功",
                accuracyClass,
                powerFactor,
                "Ist≤I≤Imin");
        }

        if (request.StartingCurrent <= 0 || request.StartingCurrent > request.MinimumCurrent)
        {
            return Invalid(
                "计算有功低电流段误差限时，Ist必须大于0且不大于Imin。",
                "有功",
                accuracyClass,
                powerFactor);
        }
        if (current < request.StartingCurrent)
        {
            return NotApplicable(
                $"当前电流{Format(current)}A小于Ist({Format(request.StartingCurrent)}A)，规程表未定义误差限。",
                "有功",
                accuracyClass,
                powerFactor,
                "I<Ist");
        }

        decimal baseLimit = accuracyClass switch
        {
            "A" => 2.5m,
            "B" => 1.5m,
            "C" => 1.0m,
            "D" => 0.4m,
            "E" => 0.2m,
            _ => 0m
        };
        decimal lowCurrentLimit = baseLimit * request.MinimumCurrent / current;
        return Applicable("有功", accuracyClass, powerFactor, "Ist≤I≤Imin", lowCurrentLimit);
    }

    /// <summary>计算无功电能最大允许误差。</summary>
    private static MeterTestErrorLimitResult CalculateReactiveLimit(MeterTestErrorLimitRequest request)
    {
        string accuracyClass = NormalizeReactiveClass(request.AccuracyClass);
        string powerFactor = NormalizePowerFactor(request.PowerFactor);
        bool isTransformer = request.AccessMode?.Contains("互感", StringComparison.OrdinalIgnoreCase) == true;
        bool isDirect = request.AccessMode?.Contains("直接", StringComparison.OrdinalIgnoreCase) == true;
        if (accuracyClass.Length == 0)
            return Invalid("无功等级必须为3、2、1S、1或0.5S。", "无功", accuracyClass, powerFactor);
        if (!isDirect && !isTransformer)
            return Invalid($"接入方式无法识别：{request.AccessMode}。", "无功", accuracyClass, powerFactor);
        if (request.Current <= 0 || request.RatedCurrent <= 0 || request.MaximumCurrent <= 0)
            return Invalid("无功电流参数无效，I、In和Imax必须大于0。", "无功", accuracyClass, powerFactor);
        if (request.Current > request.MaximumCurrent)
        {
            return NotApplicable(
                $"当前电流{Format(request.Current)}A超过Imax({Format(request.MaximumCurrent)}A)。",
                "无功",
                accuracyClass,
                powerFactor,
                "I>Imax");
        }

        return isTransformer
            ? CalculateTransformerReactiveLimit(request, accuracyClass, powerFactor)
            : CalculateDirectReactiveLimit(request, accuracyClass, powerFactor);
    }

    /// <summary>按直接接入无功等级、电流区间和功率因数查找最大允许误差。</summary>
    private static MeterTestErrorLimitResult CalculateDirectReactiveLimit(
        MeterTestErrorLimitRequest request,
        string accuracyClass,
        string powerFactor)
    {
        decimal ratio = request.Current / request.RatedCurrent;
        decimal? limit = null;
        string range;
        bool isBalanced = string.Equals(request.Phase, "H", StringComparison.OrdinalIgnoreCase);
        if (!isBalanced)
        {
            return CalculateDirectReactiveUnbalancedLimit(request, accuracyClass, powerFactor, ratio);
        }

        if (powerFactor == "1.0")
        {
            if (ratio >= 0.1m)
            {
                range = "0.1In≤I≤Imax";
                limit = ResolveReactiveClassLimit(accuracyClass, 3.0m, 2.0m, 1.0m, 0.5m);
            }
            else if (ratio >= 0.05m)
            {
                range = "0.05In≤I<0.1In";
                limit = ResolveReactiveClassLimit(accuracyClass, null, null, 1.5m, 1.0m);
            }
            else
            {
                range = "I<0.05In";
            }
        }
        else if (powerFactor is "0.5L" or "0.5C")
        {
            if (ratio >= 0.2m)
            {
                range = "0.2In≤I≤Imax";
                limit = ResolveReactiveClassLimit(accuracyClass, 4.0m, 2.5m, 2.0m, 1.0m);
            }
            else if (ratio >= 0.1m)
            {
                range = "0.1In≤I<0.2In";
                limit = ResolveReactiveClassLimit(accuracyClass, 4.0m, 2.5m, 1.5m, 1.0m);
            }
            else
            {
                range = "I<0.1In";
            }
        }
        else if (powerFactor is "0.25L" or "0.25C")
        {
            range = ratio >= 0.2m ? "0.2In≤I≤Imax" : "I<0.2In";
            if (ratio >= 0.2m)
                limit = ResolveReactiveClassLimit(accuracyClass, 4.0m, 2.5m, 2.0m, 1.0m);
        }
        else
        {
            return NotApplicable("直接接入无功误差表未定义该功率因数。", "无功", accuracyClass, powerFactor, "未定义");
        }

        return limit.HasValue
            ? Applicable("无功", accuracyClass, powerFactor, range, limit.Value)
            : NotApplicable("该等级、电流段和功率因数组合按规程不测试。", "无功", accuracyClass, powerFactor, range);
    }

    /// <summary>按直接接入 A/B/C 单相不平衡负载无功等级、电流区间和功率因数查找最大允许误差。</summary>
    private static MeterTestErrorLimitResult CalculateDirectReactiveUnbalancedLimit(
        MeterTestErrorLimitRequest request,
        string accuracyClass,
        string powerFactor,
        decimal ratio)
    {
        decimal? limit = null;
        string range;
        if (powerFactor == "1.0")
        {
            range = ratio >= 0.1m ? "0.1In≤I≤Imax（不平衡负载）" : "I<0.1In（不平衡负载）";
            if (ratio >= 0.1m)
                limit = ResolveReactiveClassLimit(accuracyClass, 4.0m, 3.0m, 1.5m, 0.7m);
        }
        else if (powerFactor is "0.5L" or "0.5C")
        {
            range = ratio >= 0.2m ? "0.2In≤I≤Imax（不平衡负载）" : "I<0.2In（不平衡负载）";
            if (ratio >= 0.2m)
                limit = ResolveReactiveClassLimit(accuracyClass, 4.0m, 3.0m, 2.0m, 1.0m);
        }
        else if (powerFactor is "0.25L" or "0.25C")
        {
            range = ratio >= 0.2m ? "0.2In≤I≤Imax（不平衡负载）" : "I<0.2In（不平衡负载）";
            if (ratio >= 0.2m)
                limit = ResolveReactiveClassLimit(accuracyClass, null, null, 3.0m, 1.5m);
        }
        else
        {
            return NotApplicable("直接接入不平衡无功误差表未定义该功率因数。", "无功", accuracyClass, powerFactor, "未定义");
        }

        return limit.HasValue
            ? Applicable("无功", accuracyClass, powerFactor, range, limit.Value)
            : NotApplicable("该等级、电流段和功率因数组合按规程不测试。", "无功", accuracyClass, powerFactor, range);
    }

    /// <summary>按经互感器接入无功等级、电流区间和功率因数查找最大允许误差。</summary>
    private static MeterTestErrorLimitResult CalculateTransformerReactiveLimit(
        MeterTestErrorLimitRequest request,
        string accuracyClass,
        string powerFactor)
    {
        decimal ratio = request.Current / request.RatedCurrent;
        decimal? limit = null;
        string range;
        if (powerFactor == "1.0")
        {
            if (ratio >= 0.05m)
            {
                range = "0.05In≤I≤Imax";
                limit = ResolveReactiveClassLimit(accuracyClass, 3.0m, 2.0m, 1.0m, 0.5m);
            }
            else if (accuracyClass is "3" or "2")
            {
                range = "0.02In≤I<0.05In";
                if (ratio >= 0.02m)
                    limit = ResolveReactiveClassLimit(accuracyClass, 4.0m, 2.5m, null, null);
            }
            else
            {
                range = "0.01In≤I<0.05In";
                if (ratio >= 0.01m)
                    limit = ResolveReactiveClassLimit(accuracyClass, null, null, 1.5m, 1.0m);
            }
        }
        else if (powerFactor == "0.5L")
        {
            if (ratio >= 0.1m)
            {
                range = "0.1In≤I≤Imax";
                limit = ResolveReactiveClassLimit(accuracyClass, 3.0m, 2.0m, 1.0m, 0.5m);
            }
            else if (ratio >= 0.05m)
            {
                range = "0.05In≤I<0.1In";
                limit = ResolveReactiveClassLimit(accuracyClass, 4.0m, 2.5m, 1.5m, 1.0m);
            }
            else
            {
                range = "I<0.05In";
            }
        }
        else if (powerFactor == "0.25L")
        {
            range = ratio >= 0.1m ? "0.1In≤I≤Imax" : "I<0.1In";
            if (ratio >= 0.1m)
                limit = ResolveReactiveClassLimit(accuracyClass, 4.0m, 2.5m, 2.0m, 1.0m);
        }
        else
        {
            return NotApplicable("经互感器接入无功误差表未定义该功率因数。", "无功", accuracyClass, powerFactor, "未定义");
        }

        return limit.HasValue
            ? Applicable("无功", accuracyClass, powerFactor, range, limit.Value)
            : NotApplicable("该等级、电流段和功率因数组合按规程不测试。", "无功", accuracyClass, powerFactor, range);
    }

    /// <summary>取得 Itr 至 Imax 区间对应的有功最大允许误差；不适用点返回 null。</summary>
    private static decimal? ResolveActiveHighCurrentLimit(string accuracyClass, string powerFactor)
    {
        return powerFactor switch
        {
            "1.0" => accuracyClass switch
            {
                "A" => 2.0m, "B" => 1.0m, "C" => 0.5m, "D" => 0.2m, "E" => 0.1m, _ => null
            },
            "0.5L" or "0.8C" => accuracyClass switch
            {
                "A" => 2.0m, "B" => 1.0m, "C" => 0.6m, "D" => 0.3m, "E" => 0.15m, _ => null
            },
            "0.25L" => accuracyClass switch
            {
                "B" => 3.5m, "C" => 1.0m, "D" => 0.5m, "E" => 0.25m, _ => null
            },
            "0.5C" => accuracyClass switch
            {
                "B" => 2.5m, "C" => 1.0m, "D" => 0.5m, "E" => 0.25m, _ => null
            },
            "0.25C" => accuracyClass == "E" ? 0.25m : null,
            _ => null
        };
    }

    /// <summary>取得 Imin 至 Itr 区间对应的有功最大允许误差；不适用点返回 null。</summary>
    private static decimal? ResolveActiveMediumCurrentLimit(string accuracyClass, string powerFactor)
    {
        return powerFactor switch
        {
            "1.0" => accuracyClass switch
            {
                "A" => 2.5m, "B" => 1.5m, "C" => 1.0m, "D" => 0.4m, "E" => 0.2m, _ => null
            },
            "0.5L" or "0.8C" => accuracyClass switch
            {
                "A" => 2.5m, "B" => 1.5m, "C" => 1.0m, "D" => 0.5m, "E" => 0.25m, _ => null
            },
            _ => null
        };
    }

    /// <summary>按无功准确度等级从 3、2、1S/1、0.5S 四组限值中选择适用值。</summary>
    private static decimal? ResolveReactiveClassLimit(
        string accuracyClass,
        decimal? class3,
        decimal? class2,
        decimal? class1,
        decimal? class05S)
    {
        return accuracyClass switch
        {
            "3" => class3,
            "2" => class2,
            "1" or "1S" => class1,
            "0.5S" => class05S,
            _ => null
        };
    }

    /// <summary>创建可测试点的原始限值、60%判定限值及上下界结果。</summary>
    private static MeterTestErrorLimitResult Applicable(
        string energyType,
        string accuracyClass,
        string powerFactor,
        string currentRange,
        decimal maximumPermittedLimit)
    {
        decimal comparisonLimit = maximumPermittedLimit * ComparisonRatio;
        string message =
            $"{energyType}等级={accuracyClass}，电流段={currentRange}，功率因数={powerFactor}；"
            + $"最大允许误差=±{Format(maximumPermittedLimit)}%，60%判定限=±{Format(comparisonLimit)}%。";
        return new MeterTestErrorLimitResult(
            true,
            true,
            energyType,
            accuracyClass,
            powerFactor,
            currentRange,
            maximumPermittedLimit,
            comparisonLimit,
            -comparisonLimit,
            comparisonLimit,
            message);
    }

    /// <summary>创建输入参数无效、无法计算误差区间的结果。</summary>
    private static MeterTestErrorLimitResult Invalid(
        string message,
        string energyType,
        string accuracyClass,
        string powerFactor)
    {
        return new MeterTestErrorLimitResult(
            false, false, energyType, accuracyClass, powerFactor, string.Empty,
            null, null, null, null, message);
    }

    /// <summary>创建规程明确规定当前等级或负载点不测试的结果。</summary>
    private static MeterTestErrorLimitResult NotApplicable(
        string message,
        string energyType,
        string accuracyClass,
        string powerFactor,
        string currentRange)
    {
        return new MeterTestErrorLimitResult(
            true, false, energyType, accuracyClass, powerFactor, currentRange,
            null, null, null, null, message);
    }

    /// <summary>规范化 A-E 有功准确度等级文本。</summary>
    private static string NormalizeActiveClass(string value)
    {
        string normalized = (value ?? string.Empty).Trim().ToUpperInvariant().Replace("级", string.Empty);
        return normalized is "A" or "B" or "C" or "D" or "E" ? normalized : string.Empty;
    }

    /// <summary>规范化 3、2、1S/1、0.5S 无功准确度等级文本。</summary>
    private static string NormalizeReactiveClass(string value)
    {
        string normalized = (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace("级", string.Empty)
            .Replace(" ", string.Empty);
        return normalized switch
        {
            "3" or "3.0" => "3",
            "2" or "2.0" => "2",
            "1" or "1.0" => "1",
            "1S" or "1.0S" => "1S",
            "0.5S" => "0.5S",
            _ => string.Empty
        };
    }

    /// <summary>规范化功率因数文本并保留 L/C 负载方向标记。</summary>
    private static string NormalizePowerFactor(string value)
    {
        string normalized = (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace("功率因数", string.Empty)
            .Replace("COSΦ", string.Empty)
            .Replace("COS", string.Empty)
            .Replace("=", string.Empty)
            .Replace(" ", string.Empty);
        return normalized switch
        {
            "1" or "1.0" => "1.0",
            "0.5L" => "0.5L",
            "0.8C" => "0.8C",
            "0.25L" => "0.25L",
            "0.5C" => "0.5C",
            "0.25C" => "0.25C",
            _ => normalized
        };
    }

    /// <summary>以稳定的小数格式输出误差限值，避免日志出现多余尾随零。</summary>
    private static string Format(decimal value)
    {
        return value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
