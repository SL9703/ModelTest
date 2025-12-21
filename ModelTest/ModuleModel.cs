using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public class ModuleModel
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
        public static string ModuleMeassage { get; set; }
        public static string ModuleByte(string startByte, string dataLength, string address, string portocol, string command, string dataIteam, string stopByte)
        {
            StartByte = startByte;
            DataLength = dataLength;
            Address = AddressToHexChange.MeassageAddr(address);
            Portocol = portocol;
            Command = command;
            DataItem = dataIteam;
            StopByte = stopByte;
            CheekSum = MessagesCheckSum.CalculateChecksum(DataLength + Address + Portocol + Command + DataItem);
            ModuleMeassage = StartByte + DataLength + Address + Portocol + Command + DataItem + CheekSum + StopByte;
            LogMessage.Debug("模组单元-准备发送消息：" + ModuleMeassage);
            return ModuleMeassage;
        }
        /// <summary>
        /// terminal class 
        /// 0x01:专变III
        /// 0x02：集中器
        /// 0x03：ECU
        /// 0x04：SCU
        /// 0X05：单相物联网表
        /// 0x06：三相物联网表
        /// 0x07：单相智能电表
        /// 0x08：三相智能电表
        /// </summary>
        /// <param name="TerminalMeterAddr"></param>
        /// <returns></returns>
        public static string TerminalMeterAddr(int terminalMeterAddr)
        {
            switch (terminalMeterAddr)
            {
                case 0:
                    return "01";
                case 1:
                    return "02";
                case 2:
                    return "03";
                case 3:
                    return "04";
                case 4:
                    return "05";
                case 5:
                    return "06";
                case 6:
                    return "07";
                case 7:
                    return "08";
            }
            return "00";
        }

    }
}
