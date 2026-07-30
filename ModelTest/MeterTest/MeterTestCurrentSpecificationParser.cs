using System.Globalization;
using System.Text.RegularExpressions;

namespace ModelTest.MeterTest;

/// <summary>
/// 解析资产信息中的电流规格，并统一提供基本误差测试使用的 Imin、Itr 和 Imax。
/// </summary>
public static class MeterTestCurrentSpecificationParser
{
    private static readonly Regex CurrentRangeRegex = new(
        @"(?<imin>\d+(?:\.\d+)?)\s*-\s*(?<itr>\d+(?:\.\d+)?)\s*\(\s*(?<imax>\d+(?:\.\d+)?)\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex BaseCurrentWithMaximumRegex = new(
        @"(?<basic>\d+(?:\.\d+)?)\s*\(\s*(?<imax>\d+(?:\.\d+)?)\s*\)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 优先解析 Imin-Itr(Imax)A 完整规格；旧数据只有基本电流时按接入方式兼容推导。
    /// </summary>
    public static bool TryParse(
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
            : itr * (NormalizeActiveClass(activeClass) == "A" ? 0.5m : 0.4m);
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

    /// <summary>从带单位或其它字符的文本中提取首个大于零的十进制数。</summary>
    private static bool TryParsePositiveNumber(string value, out decimal result)
    {
        result = 0m;
        Match match = Regex.Match(value ?? string.Empty, @"\d+(?:\.\d+)?", RegexOptions.CultureInvariant);
        return match.Success &&
               decimal.TryParse(match.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out result) &&
               result > 0;
    }

    /// <summary>规范化有功等级文本，去除“级”和空格并统一为大写。</summary>
    private static string NormalizeActiveClass(string value)
    {
        string normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        return normalized.EndsWith("级", StringComparison.Ordinal) ? normalized[..^1] : normalized;
    }
}

/// <summary>资产信息解析得到的 Imin、Itr、Imax 和推导基本电流。</summary>
public sealed record MeterTestBasicErrorCurrentSpecification(
    decimal Imin,
    decimal Itr,
    decimal Imax,
    decimal BasicCurrent,
    string Description);
