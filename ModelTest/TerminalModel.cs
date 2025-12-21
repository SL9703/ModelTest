using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public class TerminalModel
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
        public static string TerminalMeassage { get; set; }

        public static string TerminalByte(string startByte, string dataLength, string address, string portocol, string command, string dataIteam, string stopByte)
        {
            StartByte = startByte;
            DataLength = dataLength;
            Address = AddressToHexChange.MeassageAddr(address);
            Portocol = portocol;
            Command = command;
            DataItem = dataIteam;
            StopByte = stopByte;
            CheekSum = MessagesCheckSum.CalculateChecksum(DataLength + Address + Portocol + Command + DataItem);
            TerminalMeassage = StartByte + DataLength + Address + Portocol + Command + DataItem + CheekSum + StopByte;
            LogMessage.Debug("终端单元-准备发送消息：" + TerminalMeassage);
            return TerminalMeassage;
        }
        /// <summary>
        /// STA1-STA2 03 STA1 01 STA2 02
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetTerminalSTA1STA2Byte(string sta)
        {
            if (!string.IsNullOrEmpty(sta))
            {
                switch (sta)
                {
                    case "STA1-STA2":
                        return "03";
                    case "STA1":
                        return "01";
                    case "STA2":
                        return "02";
                }
            }
            return "00";
        }
        /// <summary>
        /// RST_1 SET_1 EVENT_1 RST_2 SET_2 EVENT_2
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetTerminalSTAPINByte(string stapin)
        {
            if (!string.IsNullOrEmpty(stapin))
            {
                switch (stapin)
                {
                    case "RST_1":
                        return "01";
                    case "SET_1":
                        return "02";
                    case "EVENT_1":
                        return "04";
                    case "RST_2":
                        return "01";
                    case "SET_2":
                        return "02";
                    case "EVENT_2":
                        return "04";
                }
            }
            return "00";
        }
        /// <summary>
        /// 00 断开，01台区智能融合终端，02 13集中器，03 13专变 ，04 22集中器，05 22专变，
        /// 06 22能源控制器，07南网-负荷管理终端，08 南网-配变监测计量终端，09南网-13集中器
        /// </summary>
        /// <param name="terminalclass">终端类型</param>
        /// <returns></returns>
        public static string GetTerminalClass(int terminalclass)
        {
            //cbxTerminalV1.SelectedIndex
            switch (terminalclass)
            {
                case 0:
                    return "00";
                case 1:
                    return  "01";
                case 2:
                    return  "02";
                case 3:
                    return "03";
                case 4:
                    return "04";
                case 5:
                    return "05";
                case 6:
                    return "06";
                case 7:
                    return "07";
                case 8:
                    return "08";
                case 9:
                    return "09";
            }
            return "00";
        }
    }
}
