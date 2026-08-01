namespace ModelTest.MeterTest;

/// <summary>
/// 资产条形码的电表地址提取器。
/// 规则1使用用户配置的0-based起止位（包含结束位）；
/// 规则2拼接两个由用户配置的“起始位置+长度”片段。
/// </summary>
public static class MeterTestBarcodeExtractor
{
    public const string Rule1Range = "Rule1Range";
    public const string Rule2Composite = "Rule2Composite";

    /// <summary>根据当前规则将条形码转换成电表地址。</summary>
    public static bool TryExtract(
        string? barcode,
        string? ruleType,
        int rangeStartIndex,
        int rangeEndIndex,
        out string meterAddress)
    {
        return TryExtract(
            barcode,
            ruleType,
            rangeStartIndex,
            rangeEndIndex,
            6,
            2,
            10,
            10,
            out meterAddress);
    }

    /// <summary>根据当前规则将条形码转换成电表地址，规则2的两个片段均可配置。</summary>
    public static bool TryExtract(
        string? barcode,
        string? ruleType,
        int rangeStartIndex,
        int rangeEndIndex,
        int firstSegmentStart,
        int firstSegmentLength,
        int secondSegmentStart,
        int secondSegmentLength,
        out string meterAddress)
    {
        meterAddress = string.Empty;
        string normalized = barcode?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
            return false;

        if (string.Equals(ruleType, Rule2Composite, StringComparison.OrdinalIgnoreCase))
        {
            if (firstSegmentStart < 0 || firstSegmentLength <= 0 ||
                secondSegmentStart < 0 || secondSegmentLength <= 0 ||
                firstSegmentStart + firstSegmentLength > normalized.Length ||
                secondSegmentStart + secondSegmentLength > normalized.Length)
            {
                return false;
            }

            meterAddress = normalized.Substring(firstSegmentStart, firstSegmentLength)
                + normalized.Substring(secondSegmentStart, secondSegmentLength);
            return meterAddress.Length > 0;
        }

        if (rangeStartIndex < 0 || rangeEndIndex < rangeStartIndex || normalized.Length <= rangeEndIndex)
            return false;

        int length = rangeEndIndex - rangeStartIndex + 1;
        if (rangeStartIndex + length > normalized.Length)
            return false;

        meterAddress = normalized.Substring(rangeStartIndex, length);
        return meterAddress.Length > 0;
    }
}
