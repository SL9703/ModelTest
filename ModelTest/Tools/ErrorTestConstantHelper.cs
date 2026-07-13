using System.Globalization;

namespace ModelTest.Tools;

internal static class ErrorTestConstantHelper
{
    private static readonly double[] VoltageRanges = [60D, 120D, 240D, 480D];
    private static readonly double[] CurrentRanges = [100D, 50D, 25D, 10D, 5D, 2.5D, 1D, 0.5D, 0.25D, 0.1D, 0.05D, 0.025D];
    private static readonly ulong[,] StandardConstantTable =
    {
        { 10000000UL, 20000000UL, 40000000UL, 100000000UL, 200000000UL, 400000000UL, 1000000000UL, 2000000000UL, 4000000000UL, 10000000000UL, 20000000000UL, 40000000000UL },
        { 5000000UL, 10000000UL, 20000000UL, 50000000UL, 100000000UL, 200000000UL, 500000000UL, 1000000000UL, 2000000000UL, 5000000000UL, 10000000000UL, 20000000000UL },
        { 2500000UL, 5000000UL, 10000000UL, 25000000UL, 50000000UL, 100000000UL, 250000000UL, 500000000UL, 1000000000UL, 2500000000UL, 5000000000UL, 10000000000UL },
        { 1250000UL, 2500000UL, 5000000UL, 12500000UL, 25000000UL, 50000000UL, 125000000UL, 250000000UL, 500000000UL, 1250000000UL, 2500000000UL, 5000000000UL }
    };

    public const uint DefaultMeterConstant = 10000;

    public static bool TryCalculateConstants(
        string voltageText,
        string currentText,
        out ulong standardConstant,
        out uint meterConstant)
    {
        standardConstant = 0;
        meterConstant = DefaultMeterConstant;

        if (!TryParseInputNumber(voltageText, out double voltage) ||
            !TryParseInputNumber(currentText, out double current))
        {
            return false;
        }

        standardConstant = CalculateStandardConstant(voltage, current);
        return true;
    }

    public static ulong CalculateStandardConstant(double voltage, double current)
    {
        int voltageIndex = FindAscendingRangeIndex(VoltageRanges, voltage);
        int currentIndex = FindDescendingRangeIndex(CurrentRanges, current);
        return StandardConstantTable[voltageIndex, currentIndex];
    }

    public static bool TryParseInputNumber(string text, out double value)
    {
        string normalized = text
            .Trim()
            .Replace("V", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("A", string.Empty, StringComparison.OrdinalIgnoreCase);

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
               double.TryParse(normalized, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
    }

    private static int FindAscendingRangeIndex(double[] ranges, double value)
    {
        for (int i = 0; i < ranges.Length; i++)
        {
            if (value <= ranges[i])
            {
                return i;
            }
        }

        return ranges.Length - 1;
    }

    private static int FindDescendingRangeIndex(double[] ranges, double value)
    {
        for (int i = ranges.Length - 1; i >= 0; i--)
        {
            if (value <= ranges[i])
            {
                return i;
            }
        }

        return 0;
    }
}
