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


    }
}
