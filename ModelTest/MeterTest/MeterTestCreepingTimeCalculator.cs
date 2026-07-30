using System.Globalization;
using System.Text.RegularExpressions;

namespace ModelTest.MeterTest;

/// <summary>
/// 按 JJG596-2026 计算潜动试验等待时间。
/// 公式与 SGCCTestUserControl 保持一致：
/// Delta t = (100 * 10^3) / (1.1 * b * C * d * U * Imin)，结果单位为小时。
/// </summary>
public static class MeterTestCreepingTimeCalculator
{
    private const decimal FormulaNumerator = 100000m;
    private const decimal SecondsPerHour = 3600m;

    /// <summary>从单个工位资产档案计算潜动等待时间。</summary>
    public static bool TryCalculate(
        MeterArchiveData archive,
        out MeterTestCreepingTimePlan? plan,
        out string? errorMessage)
    {
        plan = null;
        errorMessage = null;

        if (!TryResolveErrorLimit(archive.ActiveClass, out decimal errorLimit))
        {
            errorMessage = $"有功等级无法计算潜动误差极限b：{archive.ActiveClass}。";
            return false;
        }

        if (!TryParsePositiveNumber(archive.ActiveConstant, out decimal meterConstant))
        {
            errorMessage = $"有功常数无法解析：{archive.ActiveConstant}。";
            return false;
        }

        if (!TryResolveMeasurementUnit(archive.MeterType, out decimal measurementUnit))
        {
            errorMessage = $"电表类型无法计算测量单元系数d：{archive.MeterType}。";
            return false;
        }

        if (!TryParsePhaseVoltage(archive.Voltage, out decimal phaseVoltage))
        {
            errorMessage = $"额定电压无法解析：{archive.Voltage}。";
            return false;
        }

        if (!MeterTestCurrentSpecificationParser.TryParse(
                archive.CurrentSpecification,
                archive.AccessMode,
                archive.ActiveClass,
                out MeterTestBasicErrorCurrentSpecification? currentSpecification,
                out string? currentError))
        {
            errorMessage = currentError ?? $"电流规格无法解析：{archive.CurrentSpecification}。";
            return false;
        }

        decimal denominator =
            1.1m * errorLimit * meterConstant * measurementUnit * phaseVoltage * currentSpecification!.Imin;
        if (denominator <= 0)
        {
            errorMessage = "潜动时间公式分母必须大于0。";
            return false;
        }

        decimal hours = FormulaNumerator / denominator;
        decimal minutes = hours * 60m;
        decimal seconds = hours * SecondsPerHour;
        int waitSeconds = Math.Max(1, RoundSeconds(seconds));
        string calculationNote =
            $"Delta t=(100x10^3)/(1.1xbxCxdxUxImin)，"
            + $"b={errorLimit:0.######}%，C={meterConstant:0.######}imp/kWh，"
            + $"d={measurementUnit:0.######}，U={phaseVoltage:0.######}V，"
            + $"Imin={currentSpecification.Imin:0.######}A，"
            + $"结果={hours:0.######}h={minutes:0.###}min={seconds:0.###}s，取整等待={waitSeconds}s";

        plan = new MeterTestCreepingTimePlan(
            archive.StationNo,
            errorLimit,
            meterConstant,
            measurementUnit,
            phaseVoltage,
            currentSpecification.Imin,
            hours,
            minutes,
            seconds,
            waitSeconds,
            calculationNote);
        return true;
    }

    /// <summary>按 SGCCTestUserControl 的规则取整：小数部分大于0.5时进1，否则舍去。</summary>
    private static int RoundSeconds(decimal seconds)
    {
        decimal integerPart = decimal.Truncate(seconds);
        if (integerPart >= int.MaxValue)
        {
            return int.MaxValue;
        }

        decimal fraction = seconds - integerPart;
        return fraction > 0.5m ? (int)integerPart + 1 : (int)integerPart;
    }

    /// <summary>解析A级至D级在Imin、功率因数1时的最大允许误差极限b。</summary>
    private static bool TryResolveErrorLimit(string activeClass, out decimal errorLimit)
    {
        string normalized = (activeClass ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.EndsWith("级", StringComparison.Ordinal))
        {
            normalized = normalized[..^1];
        }

        errorLimit = normalized switch
        {
            "A" => 2.5m,
            "B" => 1.5m,
            "C" => 1.0m,
            "D" => 0.4m,
            _ => 0m
        };
        return errorLimit > 0;
    }

    /// <summary>解析测量单元系数：单相1、三相三线2、三相四线3。</summary>
    private static bool TryResolveMeasurementUnit(string meterType, out decimal measurementUnit)
    {
        string normalized = meterType?.Trim() ?? string.Empty;
        measurementUnit = normalized.Contains("三相三线", StringComparison.OrdinalIgnoreCase)
            ? 2m
            : normalized.Contains("三相四线", StringComparison.OrdinalIgnoreCase)
                ? 3m
                : normalized.Contains("单相", StringComparison.OrdinalIgnoreCase)
                    ? 1m
                    : 0m;
        return measurementUnit > 0;
    }

    /// <summary>解析相电压；例如3x220/380V按规程取220V。</summary>
    private static bool TryParsePhaseVoltage(string voltageText, out decimal voltage)
    {
        voltage = 0m;
        string normalized = (voltageText ?? string.Empty)
            .Trim()
            .Replace("×", "x", StringComparison.Ordinal);
        if (normalized.StartsWith("3x", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[2..];
        }

        int slashIndex = normalized.IndexOf('/');
        if (slashIndex >= 0)
        {
            normalized = normalized[..slashIndex];
        }

        normalized = normalized.Replace("V", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out voltage) && voltage > 0;
    }

    /// <summary>从带单位文本中提取第一个正数。</summary>
    private static bool TryParsePositiveNumber(string text, out decimal value)
    {
        value = 0m;
        Match match = Regex.Match(text ?? string.Empty, @"\d+(?:\.\d+)?", RegexOptions.CultureInvariant);
        return match.Success &&
            decimal.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            value > 0;
    }
}

/// <summary>单个工位的潜动时间计算结果。</summary>
public sealed record MeterTestCreepingTimePlan(
    int StationNo,
    decimal ErrorLimit,
    decimal MeterConstant,
    decimal MeasurementUnit,
    decimal PhaseVoltage,
    decimal MinimumCurrent,
    decimal Hours,
    decimal Minutes,
    decimal Seconds,
    int WaitSeconds,
    string CalculationNote);
