using System;
using System.Globalization;

namespace ModelTest
{
    /// <summary>
    /// 地址转换工具。
    /// 将界面输入的站点号、客户机地址或协议地址统一转换为两位十六进制文本。
    /// </summary>
    public static class AddressToHexChange
    {
        /// <summary>
        /// 协议地址转换：将十进制或十六进制文本转换为两位十六进制字符串。
        /// </summary>
        /// <remarks>
        /// 兼容历史行为：
        /// 1. "AA" 保持为 "AA"。
        /// 2. "255" 转换为 "FF"。
        /// 3. 其它十进制值按 0-255 转成两位大写十六进制。
        /// 4. 如果本身就是两位十六进制文本，也会原样规范为大写。
        /// </remarks>
        public static string MeassageAddr(string addr)
        {
            if (string.IsNullOrWhiteSpace(addr))
            {
                return string.Empty;
            }

            string value = addr.Trim().ToUpperInvariant();
            if (value == "AA")
                return value;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int decimalValue))
            {
                if (decimalValue is < 0 or > 255)
                    return string.Empty;

                return decimalValue.ToString("X2", CultureInfo.InvariantCulture);
            }

            if (byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hexValue))
            {
                return hexValue.ToString("X2", CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }
    }
}
