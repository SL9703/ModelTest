using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public class MeterModel
    {
        /// <summary>
        /// 起始符
        /// </summary>
        public static string StartByte { get; set; }
        /// <summary>
        /// 数据长度
        /// </summary>
        public static string DataLength { get; set; }
        /// <summary>
        /// 数据方向
        /// </summary>
        public static string DataDirection { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        public static string Address { get; set; }
        /// <summary>
        /// 协议类型
        /// </summary>
        public static string Portocol { get; set; }
        /// <summary>
        /// 命令码
        /// </summary>
        public static string Command { get; set; }
        /// <summary>
        /// 数据项
        /// </summary>
        public static string? DataItem { get; set; }
        /// <summary>
        /// 校验和
        /// </summary>
        public static string CheekSum { get; set; }
        /// <summary>
        /// 停止位
        /// </summary>
        public static string StopByte { get; set; }
        /// <summary>
        /// 完整数据报文
        /// </summary>
        public static string MeterV1Meassage { get; set; }

        
        public static string MeterByte(string startByte, string dataLength, string address, string portocol, string command, string dataIteam, string stopByte)
        {
            StartByte = startByte;
            DataLength = dataLength;
            Address = AddressToHexChange.MeassageAddr(address);
            Portocol = portocol;
            Command = command;
            DataItem = dataIteam;
            StopByte = stopByte;
            CheekSum = MessagesCheckSum.CalculateChecksum(DataLength + Address + Portocol + Command + DataItem);
            MeterV1Meassage = StartByte + DataLength + Address + Portocol + Command + DataItem + CheekSum + StopByte;
            LogMessage.Debug("电表单元-准备发送消息：" + MeterV1Meassage);
            return MeterV1Meassage;
        }
    }
}
