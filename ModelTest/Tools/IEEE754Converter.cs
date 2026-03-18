using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Tools
{
    public class IEEE754Converter
    {
        /// <summary>
        /// 将单精度浮点数转换为十六进制字符串
        /// </summary>
        public static string FloatToHex(float value)
        {
            // 方法1：使用BitConverter
            byte[] bytes = BitConverter.GetBytes(value);

            // 确保使用小端序
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            // 转换为十六进制字符串
            string hex = BitConverter.ToString(bytes).Replace("-", "");
            return hex;
        }

        /// <summary>
        /// 将双精度浮点数转换为十六进制字符串
        /// </summary>
        public static string DoubleToHex(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            string hex = BitConverter.ToString(bytes).Replace("-", "");
            return hex;
        }
        /// <summary>
        /// 将long类型转换为十六进制字符串（低字节在前）
        /// </summary>
        public static string LongToHex(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            string hex = BitConverter.ToString(bytes).Replace("-", "");
            return hex;
        }

        /// <summary>
        /// 将十六进制字符串转换为单精度浮点数
        /// </summary>
        public static float HexToFloat(string hex)
        {
            // 去除可能的空格和0x前缀
            hex = hex.Replace(" ", "").Replace("0x", "").Replace("0X", "");

            // 确保十六进制字符串长度为8（32位）
            if (hex.Length != 8)
            {
                throw new ArgumentException("单精度浮点数十六进制表示应为8位");
            }

            // 将十六进制字符串转换为字节数组
            byte[] bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            // 如果需要，处理字节序
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// 将十六进制字符串转换为双精度浮点数
        /// </summary>
        public static double HexToDouble(string hex)
        {
            hex = hex.Replace(" ", "").Replace("0x", "").Replace("0X", "");

            if (hex.Length != 16)
            {
                throw new ArgumentException("双精度浮点数十六进制表示应为16位");
            }

            byte[] bytes = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// 使用联合体方法进行转换（更高效）
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct FloatConverter
        {
            [FieldOffset(0)]
            public float FloatValue;

            [FieldOffset(0)]
            public uint UIntValue;

            [FieldOffset(0)]
            public byte[] Bytes;
        }

        /// <summary>
        /// 使用位操作将浮点数转换为十六进制
        /// </summary>
        public static string FloatToHexBitwise(float value)
        {
            FloatConverter converter = new FloatConverter();
            converter.FloatValue = value;
            return converter.UIntValue.ToString("X8");
        }

    }
}
