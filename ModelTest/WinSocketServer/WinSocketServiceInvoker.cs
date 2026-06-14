using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelTest
{
    public sealed class WinSocketServiceInvoker
    {
        private readonly WinSocketServer _server;
        private readonly Action<string> _log;

        public sealed class ServerLoginResult
        {
            public bool Connected { get; init; }
            public int ConnectCode { get; init; }
            public int RandCode { get; init; }
            public string RandText { get; init; } = string.Empty;
        }

        private static readonly Dictionary<string, string> ServiceDescriptions = new()
        {
            ["RESAM_Formal_GetKeyData_AppLayer"] = "int iOperateMode,char * cTESAMID, char * cSessionKey,int cTaskType, char * cTaskData, char * cOutSID,char * cOutAttachData, char * cOutData ,char * cOutMAC",
            ["CloseDevice"] = "连接密码机，用于断开服务器或密码机连接；无参数",
            ["ClseUsbkey"] = "释放服务器登录权限，兼容 09 版电能表使用的函数；无参数",
            ["Meter_Formal_DataClear1"] = "用于远程费控电能表清零；int Flag整型,0:公钥;1,私钥；10，双协议公钥；11，双协议私钥；,char *PutRand随机数 2,电表身份认证成功返回, 4 字节,char *PutDiv分散因子,8 字节,“0000”+表号,char *PutData入参,清零数据,char *Outdata 20字节密文；",
            ["Meter_Formal_DataClear2"] = "用于事件或需量清零函数；int Flag13 版，整型,0:公钥;1,私钥；16 版，1,私钥；10，面向对象；,char *PutRand随机数 2,电表身份认证成功返回, 4 字节,char *PutDiv分散因子,8 字节,“0000”+表号,char *PutData入参,清零数据,char *Outdata 20字节密文；",
            ["Obj_Meter_Formal_SetESAMData"] = "int InKeyState,int InOperateMode,char * cESAMNO, char * cSessionKey, char * cMeterNo, char * cESAMRand, char * cData, char * OutSID,char * OutAddData, char * OutData,char * OutMAC",
            ["Obj_Terminal_Formal_GetTrmKeyData"] = "char* iKeyVersion 密钥更新的目标状态 “00000000000000000000000000000000” 表示恢复到公钥，其他相同长度非全零数据表示更新到私钥\r\nchar* strEsamNo ESAM 序列号\r\nchar* strSessionKey 会话密钥\r\nchar* cTerminalAddress 终端地址(8 Bytes)\r\nchar* strKeyType 密钥类型，00 应用密钥，01 链路密钥",
            ["Obj_Terminal_Formal_InitSession"] = "输入参数:iKeyState，cTESAMID，cASCTR，cFLG，cMasterCert",
            ["Obj_Terminal_Formal_InitSession_RH"] = "输入参数:int iKeyState，string cTESAMID，string cASCT，string cMasterCert",
            ["Obj_Terminal_Formal_GetSessionData"] = "输入参数:int iOperateMode, char cTeasmid, char cSessionKey, int cTaskType, char cTaskData",
            ["Obj_Terminal_Formal_GetTerminalSetData"] = "输入参数:int iOperateMode, char cTeasmid, char cSessionKey, char cTaskData",
            ["Obj_Terminal_Formal_VerifyTerminalData"] = "输入参数:int ikeyState, int iOperateMode, char cTeasmid, char cSessionKey, char cTaskData, char cMac",
            ["Obj_Terminal_Formal_VerifyReadData"] = "输入参数:int iKeyState, int iOperateMode, char cTESAMID, char cRandHost, char cReadData, char cMac, char cOutData, char cOutRSPCTR",
            ["Obj_Send_Formal_DataForGetKey"] = "输入参数:string InDeviceType, string cTastType, string cKeyState, string cTeasmid, string InMeterNo, string cSessionKey",
            ["Obj_Meter_Formal_InitSession"] = "输入参数:int iKeyState(密钥版本号（ESAM 中 16 字节）), string cDiv(分散因子（8Byte），iKeyState=0，cDiv 为芯片序列号;iKeyState=1，cDiv 为表号；), string strASCTR(会话协商计数器), string strFLG(标识 默认“01”)",
            ["Obj_Meter_Formal_GenReadData"] = "输入参数:string _iKeyVersion,string _strEsamNo,string _strMeterNo,string _iOperateMode,string _randHost,string _cReadData",
            ["Obj_Terminal_Formal_GetSessionDataForMeter"] = "输入参数:int cOperateMode,string cTESAMID,string cSessionKey,int iTaskType,string cApdu,string cTaskData",
        };

        private static readonly Dictionary<int, string> ErrorMessages = new()
        {
            [45] = "密码机密钥错",
            [48] = "无设备或设备无效",
            [56] = "创建 socket 句柄失败",
            [57] = "连接服务器失败",
            [64] = "客户端发送数据失败",
            [65] = "客户端接收数据失败",
            [100] = "打开设备失败",
            [160] = "连接密码机失败",
            [161] = "操作权限不够",
            [162] = "USBKey 不是操作员",
            [163] = "服务器发送数据失败",
            [164] = "服务端接收报文失败",
            [165] = "密码机加密数据失败",
            [166] = "密码机导出密钥失败",
            [167] = "密码机计算 MAC 失败",
            [168] = "服务器已断开连接",
            [169] = "数据无效",
            [170] = "密码机收发报文错误",
            [171] = "密码机故障",
            [172] = "数据库出错",
            [1100] = "系统认证错误",
            [1107] = "USBKey 权限不正确",
            [1206] = "签名数据错误",
        };

        public WinSocketServiceInvoker(WinSocketServer server, Action<string> log)
        {
            _server = server ?? throw new ArgumentNullException(nameof(server));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public string GetParameterDescription(string? serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                return string.Empty;
            }

            return ServiceDescriptions.TryGetValue(serviceName, out var description)
                ? $"选择加密机函数 {serviceName}，调用接口参数：\r\n{description}"
                : $"选择加密机函数 {serviceName}，当前未配置参数说明。";
        }

        public void Execute(string? serviceName, string rawParameterText)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                _log("请右上角选择加密算法！");
                return;
            }

            var args = ParseArguments(rawParameterText);
            var buffers = new OutputBuffers();

            try
            {
                _log($"调用接口：{serviceName}----------开始加密计算");
                var result = Invoke(serviceName, args, buffers);
                PrintResult(serviceName, result);
            }
            catch (Exception ex)
            {
                _log($"调用接口：{serviceName}----------异常：{ex.Message}");
            }
        }

        public ServerLoginResult ConnectServerAndGetRandom(string ip, string port)
        {
            _log("开始连接加密服务器！！！");

            var connectResult = _server.ConnectDeviceEx(ip, port, "8000");
            if (!connectResult.Success)
            {
                _log($"连接加密服务器失败，错误码：{connectResult.Code}，错误说明：{GetErrorMessage(connectResult.Code)}");
                return new ServerLoginResult
                {
                    Connected = false,
                    ConnectCode = connectResult.Code,
                    RandCode = 0
                };
            }

            _log("连接加密服务器成功！");
            var outRand = new byte[128];
            var randResult = _server.CreateRandEx(16, outRand);
            return new ServerLoginResult
            {
                Connected = true,
                ConnectCode = connectResult.Code,
                RandCode = randResult.Code,
                RandText = randResult.Success ? System.Text.Encoding.Default.GetString(outRand).TrimEnd('\0', ' ') : string.Empty
            };
        }

        private WinSocketServer.DllResult Invoke(string serviceName, string[] args, OutputBuffers buffers)
        {
            switch (serviceName)
            {
                case "RESAM_Formal_GetKeyData_AppLayer":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iOperateMode", args[0]), ("cTESAMID", args[1]), ("cSessionKey", args[2]), ("cTaskType", args[3]), ("cTaskData", args[4]));
                    return _server.CallReSAM_Formal_GetKeyData_AppLayer(ParseInt(args[0], "iOperateMode"), args[1], args[2], ParseInt(args[3], "cTaskType"), args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "CloseDevice":
                    return _server.CloseDeviceEx();
                case "ClseUsbkey":
                    var closeUsbKeyResult = _server.ClseUsbkeyEx();
                    return new WinSocketServer.DllResult(closeUsbKeyResult == 0, closeUsbKeyResult);
                case "Meter_Formal_DataClear1":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("Flag", args[0]), ("PutRand", args[1]), ("PutDiv", args[2]), ("PutData", args[3]));
                    return _server.CallMeter_Formal_DataClear1(ParseInt(args[0], "Flag"), args[1], args[2], args[3], buffers.OutData);
                case "Meter_Formal_DataClear2":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("Flag", args[0]), ("PutRand", args[1]), ("PutDiv", args[2]), ("PutData", args[3]));
                    return _server.CallMeter_Formal_DataClear2(ParseInt(args[0], "Flag"), args[1], args[2], args[3], buffers.OutData);
                case "Obj_Meter_Formal_SetESAMData":
                    RequireArgs(serviceName, args, 7);
                    LogArgs(("InKeyState", args[0]), ("InOperateMode", args[1]), ("cESAMNO", args[2]), ("cSessionKey", args[3]), ("cMeterNo", args[4]), ("cESAMRand", args[5]), ("cData", args[6]));
                    return _server.CallObj_Meter_Formal_SetESAMData(ParseInt(args[0], "InKeyState"), ParseInt(args[1], "InOperateMode"), args[2], args[3], args[4], args[5], args[6],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_GetTrmKeyData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyVersion", args[0]), ("strEsamNo", args[1]), ("strSessionKey", args[2]), ("cTerminalAddress", args[3]), ("strKeyType", args[4]));
                    return _server.CallObj_Terminal_Formal_GetTrmKeyData(ParseInt(args[0], "iKeyVersion"), args[1], args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_InitSession":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iKeyVersion", args[0]), ("cTESAMID", args[1]), ("cASCTR", args[2]), ("cFLG", args[3]), ("cMasterCert", args[4]));
                    return _server.CallObj_Terminal_Formal_InitSession(ParseInt(args[0], "iKeyVersion"), args[1], args[2], args[3], args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData);
                case "Obj_Terminal_Formal_InitSession_RH":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iKeyState", args[0]), ("cTESAMNO", args[1]), ("cASCTR", args[2]), ("cMasterCert", args[3]));
                    return _server.CallObj_Terminal_Formal_InitSession_RH(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData);
                case "Obj_Terminal_Formal_GetSessionData":
                    RequireArgs(serviceName, args, 5);
                    LogArgs(("iOperateMode", args[0]), ("cEasmid", args[1]), ("cSessionKey", args[2]), ("cTasktype", args[3]), ("cTaskData", args[4]));
                    return _server.CallObj_Terminal_Formal_GetSessionData(ParseInt(args[0], "iOperateMode"), args[1], args[2], ParseInt(args[3], "cTasktype"), args[4],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_GetTerminalSetData":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iOperateMode", args[0]), ("cEasmid", args[1]), ("cSessionKey", args[2]), ("cTaskData", args[3]));
                    return _server.CallObj_Terminal_Formal_GetTerminalSetData(ParseInt(args[0], "iOperateMode"), args[1], args[2], args[3],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_VerifyTerminalData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("ikeyState", args[0]), ("iOperateMode", args[1]), ("cEasmid", args[2]), ("cSessionKey", args[3]), ("cTaskData", args[4]), ("cMac", args[5]));
                    return _server.CallObj_Terminal_Formal_VerifyTerminalData(ParseInt(args[0], "ikeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5], buffers.OutData);
                case "Obj_Terminal_Formal_VerifyReadData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyState", args[0]), ("iOperateMode", args[1]), ("cTESAMID", args[2]), ("cRandHost", args[3]), ("cReadData", args[4]), ("cMac", args[5]));
                    return _server.CallObj_Terminal_Formal_VerifyReadData(ParseInt(args[0], "iKeyState"), ParseInt(args[1], "iOperateMode"), args[2], args[3], args[4], args[5],
                        buffers.OutData, buffers.OutMAC);
                case "Obj_Send_Formal_DataForGetKey":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("InDeviceType", args[0]), ("cTastType", args[1]), ("cKeyState", args[2]), ("cTESAMID", args[3]), ("InMeterNo", args[4]), ("cSessionKey", args[5]));
                    return _server.CallObj_Send_Formal_DataForGetKey(args[0], args[1], args[2], args[3], args[4], args[5],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_GenReadData":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("iKeyVersion", args[0]), ("strEsamNo", args[1]), ("strMeterNo", args[2]), ("iOperateMode", args[3]), ("randHost", args[4]), ("cReadData", args[5]));
                    return _server.CallObj_Meter_Formal_GenReadData(args[0], args[1], args[2], args[3], args[4], args[5], buffers.OutData, buffers.OutMAC);
                case "Obj_Terminal_Formal_GetSessionDataForMeter":
                    RequireArgs(serviceName, args, 6);
                    LogArgs(("cOperateMode", args[0]), ("cTESAMID", args[1]), ("cSessionKey", args[2]), ("iTaskType", args[3]), ("cApdu", args[4]), ("cTaskData", args[5]));
                    return _server.CallObj_Terminal_Formal_GetSessionDataForMeter(ParseInt(args[0], "cOperateMode"), args[1], args[2], ParseInt(args[3], "iTaskType"), args[4], args[5],
                        buffers.OutSID, buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                case "Obj_Meter_Formal_InitSession":
                    RequireArgs(serviceName, args, 4);
                    LogArgs(("iKeyState", args[0]), ("cDiv", args[1]), ("cAsctr", args[2]), ("cFlag", args[3]));
                    return _server.CallObj_Meter_Formal_InitSession(ParseInt(args[0], "iKeyState"), args[1], args[2], args[3],
                        buffers.OutAttachData, buffers.OutData, buffers.OutMAC);
                default:
                    _log($"接口 {serviceName} 暂未实现调用分发，只在接口列表中展示。");
                    return WinSocketServer.DllResult.Fail();
            }
        }

        private void PrintResult(string serviceName, WinSocketServer.DllResult result)
        {
            if (result.Code == 0)
            {
                _log($"调用接口：{serviceName}----------成功,返回值：{result.Code}");
                return;
            }

            _log($"调用接口：{serviceName}----------失败,返回值：{result.Code}，错误说明：{GetErrorMessage(result.Code)}");
        }

        private static string GetErrorMessage(int code)
        {
            if (ErrorMessages.TryGetValue(code, out var message))
            {
                return message;
            }

            return code switch
            {
                >= 700 and <= 712 => "客户端导出密钥失败",
                >= 800 and <= 810 => "计算 MAC 失败",
                >= 900 and <= 910 => "加密数据失败",
                >= 1000 and <= 1010 => "数据长度错",
                >= 1108 and <= 1111 => "操作 USBKey 失败",
                _ => "未知错误码"
            };
        }

        private void LogArgs(params (string Name, string Value)[] args)
        {
            _log(string.Join("\r\n", args.Select(arg => $"{arg.Name} = {arg.Value}")));
        }

        private static string[] ParseArguments(string rawParameterText)
        {
            return (rawParameterText ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();
        }

        private static void RequireArgs(string serviceName, string[] args, int requiredCount)
        {
            if (args.Length < requiredCount)
            {
                throw new ArgumentException($"{serviceName} 至少需要 {requiredCount} 个参数，当前只有 {args.Length} 个。");
            }
        }

        private static int ParseInt(string value, string name)
        {
            if (!int.TryParse(value, out var result))
            {
                throw new ArgumentException($"{name} 必须是整数，当前值：{value}");
            }

            return result;
        }

        private sealed class OutputBuffers
        {
            public byte[] OutSID { get; } = new byte[256];
            public byte[] OutAttachData { get; } = new byte[256];
            public byte[] OutData { get; } = new byte[256];
            public byte[] OutMAC { get; } = new byte[256];
        }
    }
}
