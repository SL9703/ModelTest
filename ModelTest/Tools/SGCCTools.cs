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

        /// <summary>
        /// 构造指定电表地址的 698 读地址请求。
        ///
        /// 地址读取报文不能继续使用广播地址。调用方传入 6 字节电表地址后，
        /// 由统一的 698 帧构造逻辑重新计算 HCS 和 FCS，避免直接替换地址后校验失效。
        /// </summary>
        /// <param name="meterAddress">正常显示顺序的 6 字节电表地址，例如 999000032515。</param>
        /// <returns>已重新计算 HCS/FCS 的完整 698 请求报文。</returns>
        public static string BuildMeterAddressReadRequest(string meterAddress)
        {
            string normalizedAddress = NormalizeHex(meterAddress, nameof(meterAddress));
            if (normalizedAddress.Length != 12)
            {
                throw new ArgumentException("电表地址必须是 6 字节（12 个十六进制字符）。", nameof(meterAddress));
            }

            // 698 服务器地址域按低字节在前发送，因此组帧前按字节逆序。
            byte[] addressBytes = HexToBytes(normalizedAddress);
            Array.Reverse(addressBytes);
            string wireAddress = ToHex(addressBytes);

            return BytesToSGCCMessage(
                "68",
                "43",
                "05",
                wireAddress,
                "10",
                "05 01 71 40 01 02 00 00",
                "16");
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

        public static Sgcc698BroadcastAddressParseResult ParseBroadcastAddressResponse(string message)
        {
            return ParseBroadcastAddressResponse(message, "40010200", "8501", "09", 6);
        }

        public static Sgcc698BroadcastAddressParseResult ParseBroadcastAddressResponse(
            string message,
            string expectedOad,
            string expectedApdu,
            string expectedDataType,
            int expectedDataLength)
        {
            try
            {
                string normalizedExpectedOad = string.IsNullOrWhiteSpace(expectedOad)
                    ? "40010200"
                    : NormalizeHex(expectedOad, nameof(expectedOad));
                string normalizedExpectedApdu = string.IsNullOrWhiteSpace(expectedApdu)
                    ? "8501"
                    : NormalizeHex(expectedApdu, nameof(expectedApdu));
                string normalizedExpectedDataType = string.IsNullOrWhiteSpace(expectedDataType)
                    ? "09"
                    : NormalizeHex(expectedDataType, nameof(expectedDataType));
                int normalizedExpectedDataLength = expectedDataLength <= 0 ? 6 : expectedDataLength;

                List<string> details = new();
                byte[] frame = HexToBytes(message);

                // 部分电表响应会在正式 698 帧前发送若干个 FE 前导符。
                // FE 不是 698 帧内容，必须在长度、HCS、APDU 和 FCS 校验前剥离。
                int preambleLength = 0;
                while (preambleLength < frame.Length && frame[preambleLength] == 0xFE)
                {
                    preambleLength++;
                }

                if (preambleLength > 0 &&
                    preambleLength < frame.Length &&
                    frame[preambleLength] == 0x68)
                {
                    details.Add($"已去除 698 前导符：FE × {preambleLength}。");
                    frame = frame.Skip(preambleLength).ToArray();
                }

                if (frame.Length < 18)
                {
                    return Sgcc698BroadcastAddressParseResult.Fail("698响应报文长度不足，无法解析广播读地址响应。");
                }

                if (frame[0] != 0x68 || frame[^1] != 0x16)
                {
                    return Sgcc698BroadcastAddressParseResult.Fail("起始符或结束符错误，期望起始符68、结束符16。");
                }

                details.Add("起止符校验成功：68...16。");

                int declaredLength = frame[1] | (frame[2] << 8);
                int actualLength = frame.Length - 2;
                if (declaredLength != actualLength)
                {
                    return Sgcc698BroadcastAddressParseResult.Fail(
                        $"长度校验失败，声明长度={declaredLength}，实际中间数据长度={actualLength}。",
                        details);
                }

                details.Add($"长度校验成功：{ToHex(frame[1], frame[2])}，长度={declaredLength}。");

                byte control = frame[3];
                byte serverAddressSign = frame[4];
                int serverAddressLength = (serverAddressSign & 0x0F) + 1;
                int serverAddressStartIndex = 5;
                int clientAddressIndex = serverAddressStartIndex + serverAddressLength;
                int hcsIndex = clientAddressIndex + 1;
                int apduIndex = hcsIndex + 2;
                int fcsIndex = frame.Length - 3;

                if (hcsIndex + 1 >= frame.Length || apduIndex >= fcsIndex)
                {
                    return Sgcc698BroadcastAddressParseResult.Fail("698帧结构长度不足，服务器地址、客户机地址、HCS或APDU位置异常。", details);
                }

                string serverAddress = ToHex(frame, serverAddressStartIndex, serverAddressLength);
                string hcsActual = ToHex(frame[hcsIndex], frame[hcsIndex + 1]);
                string hcsSource = ToHex(frame, 1, hcsIndex - 1);
                string hcsExpected = CalculateFcs(hcsSource);
                if (!hcsActual.Equals(hcsExpected, StringComparison.OrdinalIgnoreCase))
                {
                    return Sgcc698BroadcastAddressParseResult.Fail(
                        $"帧头校验失败，期望={hcsExpected}，实际={hcsActual}。",
                        details);
                }

                details.Add($"帧头校验成功：HCS={hcsActual}。");

                string fcsActual = ToHex(frame[fcsIndex], frame[fcsIndex + 1]);
                string fcsSource = ToHex(frame, 1, fcsIndex - 1);
                string fcsExpected = CalculateFcs(fcsSource);
                if (!fcsActual.Equals(fcsExpected, StringComparison.OrdinalIgnoreCase))
                {
                    return Sgcc698BroadcastAddressParseResult.Fail(
                        $"帧尾校验失败，期望={fcsExpected}，实际={fcsActual}。",
                        details);
                }

                details.Add($"帧尾校验成功：FCS={fcsActual}。");

                byte[] apdu = frame.Skip(apduIndex).Take(fcsIndex - apduIndex).ToArray();
                string apduHex = ToHex(apdu, 0, apdu.Length);
                details.Add($"APDU={apduHex}。");

                if (apdu.Length < 12)
                {
                    return Sgcc698BroadcastAddressParseResult.Fail("APDU长度不足，无法解析读取响应OAD和数据。", details);
                }

                string actualApdu = ToHex(apdu[0], apdu[1]);
                if (!actualApdu.Equals(normalizedExpectedApdu, StringComparison.OrdinalIgnoreCase))
                {
                    return Sgcc698BroadcastAddressParseResult.Fail(
                        $"读取响应标识错误，期望={normalizedExpectedApdu}，实际={actualApdu}。",
                        details);
                }

                details.Add($"读取响应标识校验成功：{normalizedExpectedApdu}。");

                byte piid = apdu[2];
                string oad = ToHex(apdu, 3, 4);
                if (!oad.Equals(normalizedExpectedOad, StringComparison.OrdinalIgnoreCase))
                {
                    return Sgcc698BroadcastAddressParseResult.Fail($"OAD校验失败，期望={normalizedExpectedOad}，实际={oad}。", details);
                }

                details.Add($"OAD校验成功：{normalizedExpectedOad}。");

                byte dataCount = apdu[7];
                if (dataCount < 1)
                {
                    return Sgcc698BroadcastAddressParseResult.Fail($"响应数据条数错误，期望至少1条，实际={dataCount}。", details);
                }

                byte dataType = apdu[8];
                byte dataLength = apdu[9];
                int dataStartIndex = 10;
                int dataEndIndex = dataStartIndex + dataLength;
                string actualDataType = dataType.ToString("X2");
                if (!actualDataType.Equals(normalizedExpectedDataType, StringComparison.OrdinalIgnoreCase))
                {
                    return Sgcc698BroadcastAddressParseResult.Fail($"地址数据类型错误，期望={normalizedExpectedDataType}，实际={actualDataType}。", details);
                }

                if (dataLength != normalizedExpectedDataLength || dataEndIndex > apdu.Length)
                {
                    return Sgcc698BroadcastAddressParseResult.Fail($"地址数据长度错误，期望={normalizedExpectedDataLength:X2}，实际={dataLength:X2}。", details);
                }

                string meterAddress = ToHex(apdu, dataStartIndex, dataLength);
                details.Add($"地址数据解析成功：类型={normalizedExpectedDataType}，长度={normalizedExpectedDataLength:X2}，地址={meterAddress}。");

                if (apdu.Length > dataEndIndex)
                {
                    byte followReport = apdu[dataEndIndex];
                    details.Add($"跟随上报信息={followReport:X2}。");
                }

                if (apdu.Length > dataEndIndex + 1)
                {
                    byte timeFlag = apdu[dataEndIndex + 1];
                    details.Add($"时间标识={timeFlag:X2}。");
                }

                return Sgcc698BroadcastAddressParseResult.Success(
                    meterAddress,
                    $"698广播读地址响应解析成功。控制域={control:X2}，服务器标识={serverAddressSign:X2}，服务器地址={serverAddress}，客户机地址={frame[clientAddressIndex]:X2}，PIID={piid:X2}。",
                    string.Join(Environment.NewLine, details),
                    oad,
                    apduHex);
            }
            catch (Exception ex)
            {
                return Sgcc698BroadcastAddressParseResult.Fail($"698响应报文解析异常：{ex.Message}");
            }
        }

        private static byte[] HexToBytes(string value)
        {
            string normalized = NormalizeHex(value, nameof(value));
            byte[] bytes = new byte[normalized.Length / 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                bytes[index] = Convert.ToByte(normalized.Substring(index * 2, 2), 16);
            }

            return bytes;
        }

        private static string ToHex(params byte[] bytes)
        {
            return ToHex(bytes, 0, bytes.Length);
        }

        private static string ToHex(byte[] bytes, int startIndex, int count)
        {
            return BitConverter.ToString(bytes, startIndex, count).Replace("-", string.Empty);
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

    public sealed class Sgcc698BroadcastAddressParseResult
    {
        public bool IsValid { get; private init; }
        public string MeterAddress { get; private init; } = string.Empty;
        public string Message { get; private init; } = string.Empty;
        public string Detail { get; private init; } = string.Empty;
        public string Oad { get; private init; } = string.Empty;
        public string Apdu { get; private init; } = string.Empty;

        public static Sgcc698BroadcastAddressParseResult Success(
            string meterAddress,
            string message,
            string detail,
            string oad,
            string apdu)
        {
            return new Sgcc698BroadcastAddressParseResult
            {
                IsValid = true,
                MeterAddress = meterAddress,
                Message = message,
                Detail = detail,
                Oad = oad,
                Apdu = apdu
            };
        }

        public static Sgcc698BroadcastAddressParseResult Fail(string message, List<string>? details = null)
        {
            return new Sgcc698BroadcastAddressParseResult
            {
                IsValid = false,
                Message = message,
                Detail = details is null || details.Count == 0
                    ? message
                    : string.Join(Environment.NewLine, details.Concat(new[] { message }))
            };
        }
    }
}
