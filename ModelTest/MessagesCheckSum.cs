using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    /// <summary>
    /// 数据检验和计算方法
    /// </summary>
    public class MessagesCheckSum
    {
        /// <summary>
        /// 计算16进制字符串的累加和
        /// </summary>
        /// <param name="hexString">16进制字符串（长度需为偶数）</param>
        /// <param name="isComplement">是否计算补码校验和（默认为false）</param>
        /// <returns>两位16进制校验和字符串</returns>
        public static string CalculateChecksum(string hexString, bool isComplement = false)
        {
            // 验证输入有效性
            if (string.IsNullOrEmpty(hexString))
                throw new ArgumentException("输入字符串不能为空");

            if (hexString.Length % 2 != 0)
                throw new ArgumentException("输入字符串长度必须为偶数");

            // 移除可能存在的空格
            hexString = hexString.Replace(" ", "");

            // 转换为字节数组
            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                string hexByte = hexString.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(hexByte, 16);
            }

            // 计算累加和
            int sum = bytes.Sum(b => (int)b);

            // 处理校验和
            byte checksum;
            if (isComplement)
            {
                // 计算补码校验和：取反加一
                checksum = (byte)(~(byte)sum + 1);
            }
            else
            {
                // 标准累加和：取低8位
                checksum = (byte)sum;
            }

            // 返回两位16进制字符串
            return checksum.ToString("X2");
        }
    }
}
