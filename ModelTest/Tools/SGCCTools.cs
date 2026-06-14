using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndercoverRiver;

namespace ModelTest.Tools
{
    public static class SGCCTools
    {
        private const int FixedFrameBytesWithoutServerAddressAndApdu = 9;

        private static readonly IReadOnlyDictionary<string, string> ApduServiceTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["02"] = "建立应用连接请求 CONNECT-Request",
            ["03"] = "断开应用连接请求 RELEASE-Request",
            ["05"] = "读取请求 GET-Request",
            ["06"] = "设置请求 SET-Request",
            ["07"] = "操作请求 ACTION-Request",
            ["08"] = "上报应答 REPORT-Response",
            ["09"] = "代理请求 PROXY-Request"
        };

        private static readonly IReadOnlyDictionary<string, string> GetRequestChoices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["01"] = "读取一个对象属性请求 GetRequestNormal",
            ["02"] = "读取若干个对象属性请求 GetRequestNormalList",
            ["03"] = "读取一个记录型对象属性请求 GetRequestRecord",
            ["04"] = "读取若干个记录型对象属性请求 GetRequestRecordList",
            ["05"] = "读取分帧响应的下一个数据块请求 GetRequestNext"
        };

        /// <summary>
        /// 起始符
        /// </summary>
        public static string SGCC_68 { get; set; } = string.Empty;
        /// <summary>
        /// 长度
        /// </summary>
        public static string? SGCC_Lenght { get; set; }
        /// <summary>
        /// 控制域
        /// </summary>
        public static string SGCC_Ctrl { get; set; } = string.Empty;
        /// <summary>
        /// 服务器标识
        /// </summary>
        public static string SGCC_SASIGN { get; set; } = string.Empty;
        /// <summary>
        /// 服务器地址
        /// </summary>
        public static string SGCC_SA { get; set; } = string.Empty;
        /// <summary>
        /// 客户机地址
        /// </summary>

        public static string SGCC_CA { get; set; } = string.Empty;
        /// <summary>
        /// 帧头校验
        /// </summary>
        public static string SGCC_HCS { get; set; } = string.Empty;
        /// <summary>
        /// apdu
        /// </summary>
        public static string SGCC_APDU { get; set; } = string.Empty;
        /// <summary>
        /// 帧尾校验
        /// </summary>
        public static string SGCC_FCS { get; set; } = string.Empty;
        /// <summary>
        /// 结束符
        /// </summary>
        public static string SGCC_16 { get; set; } = string.Empty;

        /// <summary>
        /// 完整数据报文
        /// </summary>
        public static string SGCC_698 { get; set; } = string.Empty;

        public static string LastRequestPiid { get; private set; } = string.Empty;

        public static string BytesToSGCCMessage(
            string _68,
            string _C,
            string _sasign,
            string _sa,
            string _ca,
            string _apdu,
            string _16)
        {
            string start = NormalizeHex(_68, nameof(_68));
            string control = NormalizeHex(_C, nameof(_C));
            string serverAddressSign = NormalizeHex(_sasign, nameof(_sasign));
            string serverAddress = NormalizeHex(_sa, nameof(_sa));
            string clientAddress = NormalizeHex(_ca, nameof(_ca));
            string apdu = NormalizeHex(_apdu, nameof(_apdu));
            string end = NormalizeHex(_16, nameof(_16));

            // 长度不包括起始符和结束符。
            string length = HexConverter.ConvertToLittleEndianHex(FixedFrameBytesWithoutServerAddressAndApdu + serverAddress.Length / 2 + apdu.Length / 2);
            string headerForHcs = length + control + serverAddressSign + serverAddress + clientAddress;
            string hcs = CalculateFcs(headerForHcs);
            string frameForFcs = headerForHcs + hcs + apdu;
            string fcs = CalculateFcs(frameForFcs);
            string message = start + frameForFcs + fcs + end;

            UpdateLastFrameState(start, length, control, serverAddressSign, serverAddress, clientAddress, hcs, apdu, fcs, end, message);
            LogMessage.Debug("国网单元-准备发送消息：" + SGCC_698);
            return SGCC_698;
        }

        public static List<string> SGCCSericeImp()
        {
            return SGCCOadConfig.OadDefinitions.Keys.ToList();
        }

        public static bool TryGetOadApdu(string serviceName, out string apdu)
        {
            return TryGetOadApdu(serviceName, GeneratePiid(), out apdu);
        }

        public static bool TryGetOadApdu(string serviceName, string piid, out string apdu)
        {
            if (SGCCOadConfig.OadDefinitions.TryGetValue(serviceName, out SgccOadDefinition? definition))
            {
                string normalizedPiid = NormalizePiid(piid);
                LastRequestPiid = normalizedPiid;
                apdu = definition.BuildApdu(normalizedPiid);
                return true;
            }

            apdu = string.Empty;
            return false;
        }

        public static string GeneratePiid()
        {
            // 协议约定为 01-99，这里按两位十进制字符串生成，最终仍作为一个十六进制字节发送。
            return Random.Shared.Next(1, 100).ToString("D2");
        }

        public static string GetApduServiceTypeDescription(string apdu)
        {
            string normalizedApdu = NormalizeHex(apdu, nameof(apdu));
            string serviceType = normalizedApdu[..2];
            return ApduServiceTypes.TryGetValue(serviceType, out string? description)
                ? $"[{serviceType}] {description}"
                : $"[{serviceType}] 未知APDU类型";
        }

        public static string GetApduChoiceDescription(string apdu)
        {
            string normalizedApdu = NormalizeHex(apdu, nameof(apdu));
            if (normalizedApdu.Length < 4)
            {
                return "APDU数据长度不足，无法解析CHOICE";
            }

            string serviceType = normalizedApdu[..2];
            string choice = normalizedApdu.Substring(2, 2);
            if (serviceType == "05")
            {
                return GetRequestChoices.TryGetValue(choice, out string? description)
                    ? $"[{choice}] {description}"
                    : $"[{choice}] 未知GET-Request CHOICE";
            }

            return $"[{choice}] 当前APDU类型暂未定义CHOICE说明";
        }

        public static string GetApduPiidDescription(string apdu)
        {
            string normalizedApdu = NormalizeHex(apdu, nameof(apdu));
            if (normalizedApdu.Length < 6)
            {
                return "APDU数据长度不足，无法解析PIID";
            }

            return $"[{normalizedApdu.Substring(4, 2)}] PIID/优先级，用于请求与响应匹配";
        }

        public static string GetApduOadDescription(string apdu)
        {
            string normalizedApdu = NormalizeHex(apdu, nameof(apdu));
            if (normalizedApdu.Length < 10)
            {
                return "APDU数据长度不足，无法解析OAD";
            }

            string oad = normalizedApdu.Substring(6, 4);
            return SGCCOadConfig.OadCatalog.TryGetValue(oad, out string? description)
                ? $"[{oad}] {description}"
                : $"[{oad}] 未登记OAD描述";
        }

        public static void RegisterOadDescription(string oad, string description)
        {
            string normalizedOad = NormalizeHex(oad, nameof(oad));
            if (normalizedOad.Length != 4)
            {
                throw new ArgumentException("OAD码必须是2个字节", nameof(oad));
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("OAD描述不能为空", nameof(description));
            }

            SGCCOadConfig.RegisterOadDescription(normalizedOad, description);
        }

        public static bool TryExtractApduPiid(string message, out string piid)
        {
            piid = string.Empty;
            string normalizedMessage = NormalizeHex(message, nameof(message));
            string normalizedStart = SGCC_68;
            string normalizedEnd = SGCC_16;
            string normalizedServerAddress = SGCC_SA;

            int apduStartIndex = normalizedStart.Length
                + 4
                + SGCC_Ctrl.Length
                + SGCC_SASIGN.Length
                + normalizedServerAddress.Length
                + SGCC_CA.Length
                + 4
                + 4;

            if (normalizedMessage.Length < apduStartIndex + 6 + 4 + normalizedEnd.Length)
            {
                return false;
            }

            piid = normalizedMessage.Substring(apduStartIndex + 4, 2);
            return true;
        }

        public static bool IsLastRequestResponse(string message)
        {
            return !string.IsNullOrEmpty(LastRequestPiid)
                && TryExtractApduPiid(message, out string responsePiid)
                && string.Equals(responsePiid, LastRequestPiid, StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeHex(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("698报文参数不能为空", parameterName);
            }

            string hex = new string(value.Where(Uri.IsHexDigit).Select(char.ToUpperInvariant).ToArray());
            if (hex.Length == 0)
            {
                throw new ArgumentException("698报文参数必须包含十六进制字符", parameterName);
            }

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("698报文参数十六进制字符数量必须为偶数", parameterName);
            }

            return hex;
        }

        private static string NormalizePiid(string piid)
        {
            string normalizedPiid = NormalizeHex(piid, nameof(piid));
            if (normalizedPiid.Length != 2)
            {
                throw new ArgumentException("PIID必须是1个字节", nameof(piid));
            }

            int value = Convert.ToInt32(normalizedPiid, 16);
            if (value < 1 || value > 0x99)
            {
                throw new ArgumentOutOfRangeException(nameof(piid), "PIID范围应为01-99");
            }

            return normalizedPiid;
        }

        private static string CalculateFcs(string hex)
        {
            return ICRC_16.bytesToHexFun2(ICRC_16.CalcFCS16(ModelTool.HexStringToByteArray(hex))).Trim();
        }

        private static void UpdateLastFrameState(
            string start,
            string length,
            string control,
            string serverAddressSign,
            string serverAddress,
            string clientAddress,
            string hcs,
            string apdu,
            string fcs,
            string end,
            string message)
        {
            SGCC_68 = start;
            SGCC_Lenght = length;
            SGCC_Ctrl = control;
            SGCC_SASIGN = serverAddressSign;
            SGCC_SA = serverAddress;
            SGCC_CA = clientAddress;
            SGCC_HCS = hcs;
            SGCC_APDU = apdu;
            SGCC_FCS = fcs;
            SGCC_16 = end;
            SGCC_698 = message;
        }

    }
}
