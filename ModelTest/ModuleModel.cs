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
        public static string StartByte { get; set; } = string.Empty;
        /// <summary>
        /// 数据长度
        /// </summary>
        public static string DataLength { get; set; } = string.Empty;
        /// <summary>
        /// 地址
        /// </summary>
        public static string Address { get; set; } = string.Empty;
        /// <summary>
        /// 协议类型
        /// </summary>
        public static string Portocol { get; set; } = string.Empty;
        /// <summary>
        /// 命令码
        /// </summary>
        public static string Command { get; set; } = string.Empty;
        /// <summary>
        /// 数据项
        /// </summary>
        public static string? DataItem { get; set; }
        /// <summary>
        /// 校验和
        /// </summary>
        public static string CheekSum { get; set; } = string.Empty;
        /// <summary>
        /// 停止位
        /// </summary>
        public static string StopByte { get; set; } = string.Empty;
        /// <summary>
        /// 完整数据报文
        /// </summary>
        public static string ModuleMeassage { get; set; } = string.Empty;
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
        /// <param name="terminalMeterAddr"></param>
        /// <returns></returns>
        public static string TerminalMeterAddr(byte terminalMeterAddr)
        {
            if (terminalMeterAddr is >= 0x01 and <= 0x08)
            {
                return terminalMeterAddr.ToString("X2");
            }

            return "00";
        }

        /// <summary>
        /// 模组编号转换为位掩码：
        /// 1->01, 2->02, 3->04, 4->08, 5->10
        /// </summary>
        public static string GetModuleNumberMask(string? moduleNumberText)
        {
            if (!byte.TryParse(moduleNumberText?.Trim(), out byte moduleNumber) ||
                moduleNumber is < 1 or > 5)
            {
                return "00";
            }

            byte mask = (byte)(1 << (moduleNumber - 1));
            return mask.ToString("X2");
        }

        /// <summary>
        /// 交流相位选择转换为位掩码：
        /// A->01, B->02, C->04, N->08。
        /// 按现有协议约束，仅支持 A/B/C 任意组合且 N 必须单独使用。
        /// </summary>
        public static string GetAcPhaseMask(bool phaseA, bool phaseB, bool phaseC, bool neutral)
        {
            if (neutral)
            {
                return !phaseA && !phaseB && !phaseC ? "08" : "00";
            }

            byte mask = 0x00;
            if (phaseA)
            {
                mask |= 0x01;
            }

            if (phaseB)
            {
                mask |= 0x02;
            }

            if (phaseC)
            {
                mask |= 0x04;
            }

            return mask.ToString("X2");
        }

        /// <summary>
        /// 直流命令选择：
        /// 常规命令 -> 01，扩展命令 -> 31，未选择或冲突 -> 00。
        /// </summary>
        public static string GetDcCommandCode(bool normalCommandChecked, bool extendedCommandChecked)
        {
            if (normalCommandChecked == extendedCommandChecked)
            {
                return "00";
            }

            return normalCommandChecked ? "01" : "31";
        }

    }
}
