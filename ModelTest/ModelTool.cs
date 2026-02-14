using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public static class ModelTool
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? value.ToString();
        }

        public static IList<string> SplitString(string strData)
        {

            if (string.IsNullOrEmpty(strData))
                return Array.Empty<string>();
            return strData.Split(';', StringSplitOptions.RemoveEmptyEntries)
                 .Select(p => p == "None" ? null : p)
                 .ToList();
        }
        // 单独的调试输出方法,打印日志
        public static void LogSplitResult(IList<string> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                LogMessage.Debug("分割结果为空");
                return;
            }

            LogMessage.Debug($"分割结果（共 {parts.Count} 项）:");
            for (int i = 0; i < parts.Count; i++)
            {
                LogMessage.Debug($"[{i}] {parts[i] ?? "null"}");
            }
        }
        public static string[] StringDataSplit(string ServerData)
        {
            // 分割字符串并移除空条目
            string[] parts = ServerData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            // 处理特殊值：将 "None" 转换为 null，数值保持原样
            var ServerDataNew = parts.Select(p => p == "None" ? "None" : p).ToArray();
            return ServerDataNew;
        }
        /// <summary>
        /// Hex字符串转换为字节数组
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static byte[] HexStringToByteArray(string hex)
        {
            // 移除所有空白字符
            hex = hex.Replace(" ", "").Replace("\t", "").Replace("\n", "");
            hex = hex.Replace("0X", string.Empty);
            hex = hex.Replace("0x", string.Empty);
            hex = hex.Replace(" ", string.Empty);
            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex字符串长度必须是偶数");
            }

            byte[] data = new byte[hex.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                string byteValue = hex.Substring(i * 2, 2);
                data[i] = Convert.ToByte(byteValue, 16);
            }
            return data;
        }
        /// <summary>
        /// 去空
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] bytesTrimEnd(byte[] bytes)
        {
            List<byte> list = bytes.ToList();
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                if (bytes[i] == 0x00)
                {
                    list.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
            return list.ToArray();
        }
        /// <summary>
        /// 字符串数组转16进制字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="upperCase"></param>
        /// <returns></returns>
        public static string ByteArrayToHex(byte[] bytes, bool upperCase = true)
        {
            char[] hexChars = upperCase
                ? "0123456789ABCDEF".ToCharArray()
                : "0123456789abcdef".ToCharArray();

            char[] result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int value = bytes[i];
                result[2 * i] = hexChars[value >> 4];    // 高4位
                result[2 * i + 1] = hexChars[value & 0xF]; // 低4位
            }
            return new string(result);
        }
        // 十六进制字符表
        private static readonly char[] hexDigitsUpper = "0123456789ABCDEF".ToCharArray();
        private static readonly char[] hexDigitsLower = "0123456789abcdef".ToCharArray();
        /// <summary>
        /// 十进制转十六进制（快速算法）
        /// </summary>
        public static string ToHex(this double value, bool uppercase = true, int minLength = 1)
        {
            return ToHex((long)value, uppercase, minLength);
        }
        public static string ToHex(this int value, bool uppercase = true, int minLength = 1)
        {
            return ToHex((long)value, uppercase, minLength);
        }

        public static string ToHex(this long value, bool uppercase = true, int minLength = 1)
        {
            if (value == 0) return new string('0', Math.Max(1, minLength));

            bool isNegative = value < 0;
            ulong number = isNegative ? (ulong)(-value) : (ulong)value;

            char[] digits = uppercase ? hexDigitsUpper : hexDigitsLower;
            char[] buffer = new char[16]; // long最大16位十六进制
            int index = 15;

            while (number > 0)
            {
                buffer[index--] = digits[number & 0xF];
                number >>= 4;
            }

            int start = index + 1;
            int length = 16 - start;

            if (length < minLength)
            {
                char[] result = new char[minLength];
                int padding = minLength - length;
                Array.Fill(result, '0', 0, padding);
                Array.Copy(buffer, start, result, padding, length);

                if (isNegative)
                {
                    return "-" + new string(result);
                }
                return new string(result);
            }

            string hex = new string(buffer, start, length);

            if (isNegative)
            {
                return "-" + hex;
            }
            return hex;
        }
        /// <summary>
        /// 检查字符串是否为有效的十六进制
        /// </summary>
        public static bool IsValidHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                return false;

            int startIndex = 0;
            if (hex[0] == '-')
                startIndex = 1;
            else if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                startIndex = 2;

            if (startIndex >= hex.Length)
                return false;

            for (int i = startIndex; i < hex.Length; i++)
            {
                char c = hex[i];
                if (!((c >= '0' && c <= '9') ||
                      (c >= 'A' && c <= 'F') ||
                      (c >= 'a' && c <= 'f')))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 16进制算字节长度：更精确的计算（考虑奇偶性）
        /// </summary>
        public static int CalculateByteLengthExact(string hexString)
        {
            if (string.IsNullOrEmpty(hexString))
                return 0;

            hexString = NormalizeHexString(hexString);

            if (hexString.Length == 0)
                return 0;

            // 检查是否为有效的十六进制字符串
            if (!IsValidHex(hexString))
                throw new ArgumentException("无效的十六进制字符串");

            // 偶数长度：直接除以2
            // 奇数长度：补0后除以2
            if (hexString.Length % 2 == 0)
            {
                return hexString.Length / 2;
            }
            else
            {
                // 奇数长度可能需要补0，但字节长度需要包含第一个半字节
                return (hexString.Length + 1) / 2;
            }
        }
        private static string NormalizeHexString(string hex)
        {
            return hex?.Trim()
                      .Replace("0x", "", StringComparison.OrdinalIgnoreCase)
                      .Replace(" ", "")
                      .Replace("\t", "")
                      .Replace("\n", "")
                      .Replace("\r", "")
                      .Replace("-", "")
                      .Replace(":", "")
                      .ToUpper() ?? "";
        }
        /// <summary>
        /// 绑定一组互斥的复选框（两个或三个）
        /// </summary>
        /// <param name="checkBoxes">要设为互斥的复选框数组（按顺序传入即可）</param>
        public static void BindMutexCheckBoxes(params CheckBox[] checkBoxes)
        {
            if (checkBoxes == null || checkBoxes.Length < 2 || checkBoxes.Length > 3)
                throw new ArgumentException("必须传入2个或3个复选框");

            // 为每个复选框绑定相同的事件处理
            foreach (var cb in checkBoxes)
            {
                cb.CheckedChanged += (sender, e) =>
                {
                    var currentCb = sender as CheckBox;
                    if (currentCb == null || !currentCb.Checked) return; // 只有选中时才需要处理

                    // 将其他复选框取消选中
                    foreach (var other in checkBoxes)
                    {
                        if (other != currentCb && other.Checked)
                        {
                            other.Checked = false;
                        }
                    }
                };
            }
        }
    }
}
