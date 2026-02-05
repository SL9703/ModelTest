using System;
using System.Text;

public class HexConverter
{
    /// <summary>
    /// 16进制转换，不足补0，低字节在前
    /// </summary>
    /// <param name="hexString">输入的16进制字符串</param>
    /// <param name="requiredBytes">要求的字节数（0表示自动计算）</param>
    /// <returns>转换后的16进制字符串</returns>
    public static string ConvertHex(string hexString, int requiredBytes = 0)
    {
        // 1. 清理输入
        string cleanHex = CleanHexString(hexString);

        // 2. 计算实际字节数
        int actualByteCount = GetByteCount(cleanHex);

        // 3. 确定目标字节数
        int targetByteCount = requiredBytes > 0 ? requiredBytes : actualByteCount;

        // 4. 补0（不足目标字节数时高位补0）
        string paddedHex = PadHexString(cleanHex, targetByteCount);

        // 5. 字节序转换（低字节在前）
        string result = ConvertEndianness(paddedHex);

        return result;
    }

    /// <summary>
    /// 清理16进制字符串
    /// </summary>
    private static string CleanHexString(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return "";

        StringBuilder result = new StringBuilder();

        foreach (char c in hex)
        {
            // 转换为大写
            char upper = char.ToUpper(c);

            // 只保留16进制字符
            if ((upper >= '0' && upper <= '9') ||
                (upper >= 'A' && upper <= 'F'))
            {
                result.Append(upper);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 计算字节数
    /// </summary>
    private static int GetByteCount(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return 0;

        // 字节数 = 字符数 / 2（向上取整）
        return (hex.Length + 1) / 2;
    }

    /// <summary>
    /// 补0到指定字节数
    /// </summary>
    private static string PadHexString(string hex, int targetByteCount)
    {
        if (targetByteCount <= 0)
            return hex;

        // 计算当前字节数
        int currentByteCount = GetByteCount(hex);

        if (currentByteCount >= targetByteCount)
        {
            // 如果已经够长，直接返回
            return hex;
        }

        // 需要补充的字符数
        int requiredChars = targetByteCount * 2;
        int charsToAdd = requiredChars - hex.Length;

        // 高位补0
        return new string('0', charsToAdd) + hex;
    }

    /// <summary>
    /// 转换字节序（低字节在前，高字节在后）
    /// </summary>
    private static string ConvertEndianness(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return "";

        // 确保长度为偶数
        if (hex.Length % 2 != 0)
        {
            hex = "0" + hex;
        }

        // 分割为字节
        int byteCount = hex.Length / 2;
        string[] bytes = new string[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            bytes[i] = hex.Substring(i * 2, 2);
        }

        // 反转字节顺序（低字节在前）
        Array.Reverse(bytes);

        // 重新组合
        return string.Concat(bytes);
    }

    /// <summary>
    /// 批量转换
    /// </summary>
    public static string[] ConvertBatch(string[] hexStrings, int requiredBytes = 0)
    {
        if (hexStrings == null)
            return Array.Empty<string>();

        string[] results = new string[hexStrings.Length];

        for (int i = 0; i < hexStrings.Length; i++)
        {
            results[i] = ConvertHex(hexStrings[i], requiredBytes);
        }

        return results;
    }

    /// <summary>
    /// 转换为字节数组（低字节在前）
    /// </summary>
    public static byte[] HexToBytes(string hexString, int requiredBytes = 0)
    {
        string convertedHex = ConvertHex(hexString, requiredBytes);

        // 确保长度为偶数
        if (convertedHex.Length % 2 != 0)
        {
            convertedHex = "0" + convertedHex;
        }

        int byteCount = convertedHex.Length / 2;
        byte[] bytes = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            string byteString = convertedHex.Substring(i * 2, 2);
            bytes[i] = Convert.ToByte(byteString, 16);
        }

        return bytes;
    }

    /// <summary>
    /// 生成C语言数组格式
    /// </summary>
    public static string GenerateCArray(string hexString, int requiredBytes = 0, string arrayName = "data")
    {
        byte[] bytes = HexToBytes(hexString, requiredBytes);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"// 原始16进制: {hexString}");
        sb.AppendLine($"// 转换后16进制: {BitConverter.ToString(bytes).Replace("-", "")}");
        sb.AppendLine($"// 字节数: {bytes.Length}");
        sb.AppendLine($"const uint8_t {arrayName}[{bytes.Length}] = {{");

        for (int i = 0; i < bytes.Length; i++)
        {
            sb.Append($"  0x{bytes[i]:X2}");

            if (i < bytes.Length - 1)
            {
                sb.Append(",");
            }

            if ((i + 1) % 8 == 0 || i == bytes.Length - 1)
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("};");

        return sb.ToString();
    }
}