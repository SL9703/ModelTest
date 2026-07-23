using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ModelTest.MeterTest;

/// <summary>
/// JJG596 起动试验参数计算器。
/// 统一负责启动电流 Ist、启动功率 Pst 和起动时间 Tst 上下限计算，供升源和等待步骤复用。
/// </summary>
public static class MeterTestStartingTestCalculator
{
    private const decimal Ki = 1m;
    private const decimal Ku = 1m;

    /// <summary>
    /// 仅计算启动电流。该方法只依赖接入方式、基本电流和有功等级，适合升源步骤调用。
    /// </summary>
    public static bool TryCalculateStartingCurrent(
        MeterArchiveData archive,
        out decimal startingCurrent,
        out string calculationNote,
        out string? errorMessage)
    {
        startingCurrent = 0;
        calculationNote = string.Empty;
        errorMessage = null;

        if (!TryParsePositiveNumber(archive.Current, out decimal basicCurrent))
        {
            errorMessage = $"基本电流无效：{archive.Current}。";
            return false;
        }

        string accessMode = archive.AccessMode?.Trim() ?? string.Empty;
        bool isTransformer = accessMode.Contains("互感", StringComparison.OrdinalIgnoreCase);
        bool isDirect = accessMode.Contains("直接", StringComparison.OrdinalIgnoreCase);
        if (!isTransformer && !isDirect)
        {
            errorMessage = $"接入方式无法识别：{archive.AccessMode}。";
            return false;
        }

        string activeClass = NormalizeActiveClass(archive.ActiveClass);
        decimal currentFactor = isTransformer
            ? activeClass switch
            {
                "A" => 0.05m,
                "B" => 0.04m,
                "C" or "D" or "E" => 0.02m,
                _ => 0m
            }
            : activeClass switch
            {
                "A" => 0.05m,
                "B" or "C" or "D" => 0.04m,
                _ => 0m
            };
        if (currentFactor <= 0)
        {
            errorMessage = $"有功等级无法计算启动电流：{archive.ActiveClass}。";
            return false;
        }

        decimal currentDivisor = isTransformer ? 20m : 10m;
        startingCurrent = basicCurrent / currentDivisor * currentFactor;
        if (startingCurrent <= 0)
        {
            errorMessage = $"计算出的启动电流无效：{startingCurrent}A。";
            return false;
        }

        calculationNote = $"接入方式={accessMode}，有功等级={activeClass}，基础电流={basicCurrent:0.######}A，"
            + $"Ist={basicCurrent:0.######}/{currentDivisor:0}×{currentFactor:0.##}={startingCurrent:0.#########}A";
        return true;
    }

    /// <summary>
    /// 按 Tst=(1±Est)×3.6×10^6/(C×Pst×Ki×Ku) 计算单个工位的起动时间。
    /// Est 在资产信息中以百分数表示，参与公式前会除以100。
    /// </summary>
    public static bool TryCalculateStartingTime(
        MeterArchiveData archive,
        out MeterTestStartingTimeResult? result,
        out string? errorMessage)
    {
        result = null;
        errorMessage = null;
        if (!TryCalculateStartingCurrent(archive, out decimal startingCurrent, out string currentNote, out errorMessage))
            return false;

        string activeClass = NormalizeActiveClass(archive.ActiveClass);
        decimal estPercent = activeClass switch
        {
            "A" => 2.5m,
            "B" => 1.5m,
            "C" => 1.0m,
            "D" => 0.4m,
            _ => 0m
        };
        if (estPercent <= 0)
        {
            errorMessage = $"有功等级 {archive.ActiveClass} 未配置起动最大允许误差 Est。";
            return false;
        }

        if (!TryParsePositiveNumber(archive.ActiveConstant, out decimal meterConstant))
        {
            errorMessage = $"电能表有功常数无效：{archive.ActiveConstant}。";
            return false;
        }

        if (!TryParseVoltage(archive.Voltage, out decimal voltage))
        {
            errorMessage = $"额定电压无法解析：{archive.Voltage}。";
            return false;
        }

        if (!TryResolveMeasurementUnit(archive.MeterType, out decimal unitFactor, out string unitNote, out errorMessage))
            return false;

        decimal startingPower = voltage * startingCurrent * unitFactor;
        if (startingPower <= 0)
        {
            errorMessage = $"计算出的启动功率无效：{startingPower}W。";
            return false;
        }

        decimal estRatio = estPercent / 100m;
        decimal baseTime = 3_600_000m / (meterConstant * startingPower * Ki * Ku);
        decimal lowerSeconds = (1m - estRatio) * baseTime;
        decimal upperSeconds = (1m + estRatio) * baseTime;
        if (upperSeconds <= 0 || upperSeconds > int.MaxValue)
        {
            errorMessage = $"计算出的起动时间超出支持范围：{upperSeconds}s。";
            return false;
        }

        result = new MeterTestStartingTimeResult(
            archive.StationNo,
            activeClass,
            estPercent,
            estRatio,
            meterConstant,
            voltage,
            startingCurrent,
            unitFactor,
            startingPower,
            lowerSeconds,
            upperSeconds,
            (int)Math.Ceiling(upperSeconds),
            $"{currentNote}；测量单元={unitNote}；Ki=1，Ku=1");
        return true;
    }

    /// <summary>按资产电表类型解析测量单元系数d。</summary>
    private static bool TryResolveMeasurementUnit(
        string? meterType,
        out decimal unitFactor,
        out string unitNote,
        out string? errorMessage)
    {
        string normalized = meterType?.Trim() ?? string.Empty;
        errorMessage = null;
        if (normalized.Contains("单相", StringComparison.OrdinalIgnoreCase))
        {
            unitFactor = 1m;
            unitNote = "单相，d=1";
            return true;
        }

        if (normalized.Contains("三相三线", StringComparison.OrdinalIgnoreCase))
        {
            unitFactor = 2m;
            unitNote = "三相三线，d=2";
            return true;
        }

        if (normalized.Contains("三相四线", StringComparison.OrdinalIgnoreCase))
        {
            unitFactor = 3m;
            unitNote = "三相四线，d=3";
            return true;
        }

        if (normalized.Equals("三相", StringComparison.OrdinalIgnoreCase))
        {
            // 兼容旧数据库的笼统“三相”值；新数据应明确选择三相三线或三相四线。
            unitFactor = 3m;
            unitNote = "旧资产类型三相，兼容按三相四线d=3";
            return true;
        }

        unitFactor = 0;
        unitNote = string.Empty;
        errorMessage = $"电表类型无法确定测量单元系数d：{meterType}。";
        return false;
    }

    /// <summary>从资产电压中提取相电压；例如3×220/380V取220。</summary>
    private static bool TryParseVoltage(string? value, out decimal voltage)
    {
        voltage = 0;
        string normalized = value?.Trim() ?? string.Empty;
        Match phaseVoltageMatch = Regex.Match(normalized, @"[x×]\s*(\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
        string numericText = phaseVoltageMatch.Success
            ? phaseVoltageMatch.Groups[1].Value
            : Regex.Match(normalized, @"\d+(?:\.\d+)?").Value;
        return decimal.TryParse(numericText, NumberStyles.Float, CultureInfo.InvariantCulture, out voltage) && voltage > 0;
    }

    /// <summary>从带单位文本中提取第一个正数。</summary>
    private static bool TryParsePositiveNumber(string? value, out decimal number)
    {
        number = 0;
        Match match = Regex.Match(value ?? string.Empty, @"\d+(?:\.\d+)?");
        return match.Success &&
               decimal.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out number) &&
               number > 0;
    }

    private static string NormalizeActiveClass(string? value)
    {
        return (value ?? string.Empty)
            .Replace("级", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim()
            .ToUpperInvariant();
    }
}

/// <summary>单个工位的起动时间完整计算结果。</summary>
public sealed record MeterTestStartingTimeResult(
    int StationNo,
    string ActiveClass,
    decimal EstPercent,
    decimal EstRatio,
    decimal MeterConstant,
    decimal Voltage,
    decimal StartingCurrent,
    decimal UnitFactor,
    decimal StartingPower,
    decimal LowerSeconds,
    decimal UpperSeconds,
    int WaitSeconds,
    string CalculationNote);
