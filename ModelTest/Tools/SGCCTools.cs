using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UndercoverRiver;

namespace ModelTest.Tools
{
    public class SGCCTools
    {
        /// <summary>
        /// 起始符
        /// </summary>
        public static string SGCC_68 { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        public static string? SGCC_Lenght { get; set; }
        /// <summary>
        /// 控制域
        /// </summary>
        public static string SGCC_Ctrl { get; set; }
        /// <summary>
        /// 服务器标识
        /// </summary>
        public static string SGCC_SASIGN { get; set; }
        /// <summary>
        /// 服务器地址
        /// </summary>
        public static string SGCC_SA { get; set; }
        /// <summary>
        /// 客户机地址
        /// </summary>

        public static string SGCC_CA { get; set; }
        /// <summary>
        /// 帧头校验
        /// </summary>
        public static string SGCC_HCS { get; set; }
        /// <summary>
        /// apdu
        /// </summary>
        public static string SGCC_APDU { get; set; }
        /// <summary>
        /// 帧尾校验
        /// </summary>
        public static string SGCC_FCS { get; set; }
        /// <summary>
        /// 结束符
        /// </summary>
        public static string SGCC_16 { get; set; }

        /// <summary>
        /// 完整数据报文
        /// </summary>
        public static string SGCC_698 { get; set; }

        public static string BytesToSGCCMessage(
            string _68,
            string _C,
            string _sasign,
            string _sa,
            string _ca,
            string _apdu,
            string _16)
        {
            //计算长度
            SGCC_Lenght = HexConverter.ConvertToLittleEndianHex(((9 + (_sa.Length / 2) + (_apdu.Length / 2))));//不包括帧头和帧尾
            SGCC_68 = _68;
            SGCC_Ctrl = _C;
            SGCC_SASIGN = _sasign;
            SGCC_SA = _sa;
            SGCC_CA = _ca;
            //帧头校验 HCS为2字节，是对帧头部分不包含起始字符和HCS本身的所有字节的校验
            var hcs = SGCC_Lenght + SGCC_Ctrl + SGCC_SASIGN + SGCC_SA + SGCC_CA;
           // LogMessage.Debug("698 HCS需要校验数据：" + hcs);
            SGCC_HCS = ICRC_16.bytesToHexFun2(ICRC_16.CalcFCS16(ModelTool.HexStringToByteArray(hcs))).Trim();
            //LogMessage.Debug("698 HCS校验数据：" + SGCC_HCS);
            SGCC_APDU = _apdu;
            //帧校验  FCS为2字节，是对整帧不包含起始字符、结束字符和FCS本身的所有字节的校验
            var fcs = SGCC_Lenght + SGCC_Ctrl + SGCC_SASIGN + SGCC_SA + SGCC_CA + SGCC_HCS + SGCC_APDU;
            //LogMessage.Debug("698 FCS需要校验数据：" + fcs);
            SGCC_FCS = ICRC_16.bytesToHexFun2(ICRC_16.CalcFCS16(ModelTool.HexStringToByteArray(fcs)));
           // LogMessage.Debug("698 FCS校验数据：" + SGCC_FCS);
            SGCC_16 = _16;
            SGCC_698 = SGCC_68 + SGCC_Lenght + SGCC_Ctrl + SGCC_SASIGN + SGCC_SA + SGCC_CA + SGCC_HCS + SGCC_APDU + SGCC_FCS + SGCC_16;
            LogMessage.Debug("国网单元-准备发送消息：" + SGCC_698);
            return SGCC_698;
        }

        public static List<string> SGCCSericeImp()
        {
            return new List<string>()
            {
                "读取终端或电表485属性",
                "读取广播终端或电表地址",
            };
        }
    }
}
