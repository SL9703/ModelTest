using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public class WinSocketServer
    {
        public int res = -1;
        private readonly object lockObject = new object();//lock对象
        private static void HandleException(Exception ex)
        {
            if (ex != null)
            {
                // 记录日志
                LogMessage.Error(ex);
            }
        }
        /// <summary>
        /// 登录加密机服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="Ctime"></param>
        /// <returns></returns>
        private static int LoginResult;
        [DllImport("WinSocketServer.dll")]
        private static extern int ConnectDevice(string ip, string port, string Ctime);

        public (bool Success, int Result) ConnectDeviceEx(string ip, string port, string Ctime)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    LoginResult = ConnectDevice(ip, port, Ctime); ;
                    if (IsValidResult("ConnectDevice", LoginResult))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{LoginResult.ToString()}");
                        return (true, LoginResult);

                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{LoginResult.ToString()}");
                        return (false, LoginResult);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        /// <summary>
        /// 产生随机数
        /// </summary>
        /// <param name="InRand"></param>
        /// <param name="OutRand"></param>
        /// <returns></returns>
        private static int CreateRandResult;
        [DllImport("WinSocketServer.dll")]
        private static extern int CreateRand([In, Out] int InRand, [Out] byte[] OutRand);
        public (bool Success, int Result) CreateRandEx(int inRand, [Out] byte[] outRand)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    CreateRandResult = CreateRand(inRand, outRand);
                    if (IsValidResult("ConnectDevice", CreateRandResult, "OutRand", outRand))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{CreateRandResult.ToString()}");
                        return (true, CreateRandResult);

                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{CreateRandResult.ToString()}");
                        return (false, CreateRandResult);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        private static int _GetKeyData_AppLayer;
        [DllImport("WinSocketServer.dll")]
        private static extern int RESAM_Formal_GetKeyData_AppLayer([In, Out]
        int iOperateMode, string cTESAMID, string cSessionKey, int cTaskType, string cTaskData,
        [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC);

        public (bool Success, int Result) ReSAM_Formal_GetKeyData_AppLayer(
            int iOperateMode, string cTESAMID, string cSessionKey, int cTaskType, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    _GetKeyData_AppLayer = RESAM_Formal_GetKeyData_AppLayer(iOperateMode, cTESAMID, cSessionKey, cTaskType, cTaskData,
             cOutSID, cOutAttachData, cOutData, cOutMAC);
                    if (IsValidResult("ReSAM_Formal_GetKeyData_AppLayer", GetSessionData, cOutSID, cOutAttachData, cOutData, cOutMAC))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{_GetKeyData_AppLayer.ToString()}");
                        return (true, _GetKeyData_AppLayer);

                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{_GetKeyData_AppLayer.ToString()}");
                        return (false, _GetKeyData_AppLayer);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        private static int _SetESAMData;
        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_SetESAMData([In, Out]
            int InKeyState,
            int InOperateMode,
            string cESAMNO,
            string cSessionKey,
            string cMeterNo,
            string cESAMRand,
            string cData,
           [Out] byte[] OutSID,
           [Out] byte[] OutAddData,
           [Out] byte[] OutData,
           [Out] byte[] OutMAC);
        public (bool Success, int Result) CallObj_Meter_Formal_SetESAMData(int InKeyState, int InOperateMode, string cESAMNO, string cSessionKey,
            string cMeterNo, string cESAMRand, string cData, [Out] byte[] OutSID, [Out] byte[] OutAddData,
            [Out] byte[] OutData, [Out] byte[] OutMAC)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    _SetESAMData = Obj_Meter_Formal_SetESAMData(InKeyState, InOperateMode, cESAMNO, cSessionKey,
             cMeterNo, cESAMRand, cData, OutSID, OutAddData,
             OutData, OutMAC);
                    if (IsValidResult("Obj_Meter_Formal_SetESAMData", _SetESAMData, OutSID, OutAddData, OutData, OutMAC))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{_SetESAMData.ToString()}");
                        return (true, _SetESAMData);

                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{_SetESAMData.ToString()}");
                        return (false, _SetESAMData);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        /// <summary>
        /// 断开密码机
        /// </summary>
        /// <returns></returns>
        private static int _CloseDevice;
        [DllImport("WinSocketServer.dll")]
        private static extern int CloseDevice();
        public (bool Success, int Result) CloseDeviceEx()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    _CloseDevice = CloseDevice();
                    if (IsValidResult("CloseDevice", _CloseDevice))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{_CloseDevice.ToString()}");
                        return (true, _CloseDevice);
                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{_CloseDevice.ToString()}");
                        return (false, _CloseDevice);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        /// <summary>
        /// 释放服务器登录权限，兼容 09 版电能表使用的函数。
        /// </summary>
        /// <returns></returns>
        [DllImport("WinSocketServer.dll")]
        private static extern int ClseUsbkey();
        public int ClseUsbkeyEx()
        {
            try
            {
                res = ClseUsbkeyEx();
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex); return res;
            }
        }
        /// <summary>
        /// 用于远程费控电能表清零
        /// </summary>
        /// <returns></returns>
        private static int _DataClear1;
        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_DataClear1([In, Out] int Flag, string PutRand, string PutDiv, string PutData, [Out] byte[] Outdata);
        public (bool Success, int Result) CallMeter_Formal_DataClear1(int flag, string putRand, string putDiv, string putData, 
            [Out] byte[] outdata)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    _DataClear1 = Meter_Formal_DataClear1(flag, putRand, putDiv, putData, outdata);
                    if (IsValidResult("Meter_Formal_DataClear1", _DataClear1, "OutRand", outdata))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{_DataClear1.ToString()}");
                        return (true, _DataClear1);
                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{_DataClear1.ToString()}");
                        return (false, _DataClear1);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        /// <summary>
        /// 用于电能表事件或需量清零    
        /// </summary>
        /// <param name="Flag"></param>
        /// <param name="PutRand"></param>
        /// <param name="PutDiv"></param>
        /// <param name="PutData"></param>
        /// <param name="Outdata"></param>
        /// <returns></returns>
        private static int _DataClear2;
        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_DataClear2([In, Out] int Flag, string PutRand, string PutDiv, string PutData, [Out] byte[] Outdata);
        public (bool Success, int Result) CallMeter_Formal_DataClear2(int flag, string putRand, string putDiv, string putData, [Out] byte[] outdata)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    _DataClear2 = Meter_Formal_DataClear2(flag, putRand, putDiv, putData, outdata);
                    if (IsValidResult("Meter_Formal_DataClear2", _DataClear2, "outdata", outdata))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{_DataClear2.ToString()}");
                        return (true, _DataClear2);
                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{_DataClear2.ToString()}");
                        return (false, _DataClear2);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }

        private static int GetTrmKeyData;
        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetTrmKeyData([In, Out] int iKeyVersion, string strEsamNo, string strSessionKey, string cTerminalAddress, string strKeyType, [Out] byte[] strOutSID, [Out] byte[] strOutAttachData, [Out] byte[] strOutTrmKeyData, [Out] byte[] strOutMAC);
        public (bool Success, int Result) CallObj_Terminal_Formal_GetTrmKeyData(int iKeyVersion, string strEsamNo, string strSessionKey, string cTerminalAddress, string strKeyType,
            [Out] byte[] strOutSID, [Out] byte[] strOutAttachData, [Out] byte[] strOutTrmKeyData, [Out] byte[] strOutMAC)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    GetTrmKeyData = Obj_Terminal_Formal_GetTrmKeyData(iKeyVersion, strEsamNo, strSessionKey, strKeyType, cTerminalAddress,
                        strOutSID, strOutAttachData, strOutTrmKeyData, strOutMAC);
                    if (IsValidResult("Obj_Terminal_Formal_GetTrmKeyData", GetTrmKeyData, strOutSID, strOutAttachData, strOutTrmKeyData,strOutMAC))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{GetTrmKeyData.ToString()}");
                        return (true, GetTrmKeyData);
                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{GetTrmKeyData.ToString()}");
                        return (false, GetTrmKeyData);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        private static int _InitSession;
        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_InitSession([In, Out] int iKeyVersion, string cTESAMID, string cASCTR, string cFLG, string cMasterCert, [Out] byte[] cOutRandHost, [Out] byte[] cOutSessionInit, [Out] byte[] cOutSign);
        public (bool Success, int Result) CallObj_Terminal_Formal_InitSession(int iKeyVersion, string cTESAMID, string cASCTR, string cFLG, string cMasterCert, 
            [Out] byte[] cOutRandHost, [Out] byte[] cOutSessionInit, [Out] byte[] cOutSign)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    _InitSession = Obj_Terminal_Formal_InitSession(iKeyVersion, cTESAMID, cASCTR, cFLG, cMasterCert,
                        cOutRandHost, cOutSessionInit, cOutSign);
                    if (IsValidResult("Obj_Terminal_Formal_GetTrmKeyData", _InitSession, cOutRandHost, cOutSessionInit, cOutSign))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{_InitSession.ToString()}");
                        return (true, _InitSession);
                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{_InitSession.ToString()}");
                        return (false, _InitSession);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        private static int GetSessionData;
        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetSessionData(
             [In, Out] int iOperateMode, string cEasmid, string cSessionKey, int cTasktype, string cTaskData,
                [Out] byte[] OutSID, [Out] byte[] OutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMac);
        public (bool Success, int Result) CallObj_Terminal_Formal_GetSessionData(
            int iOperateMode, string cEasmid, string cSessionKey, int cTasktype, string cTaskData,
               byte[] OutSID, byte[] OutAttachData, byte[] cOutData, byte[] cOutMac)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    GetSessionData =
                        Obj_Terminal_Formal_GetSessionData
                        (iOperateMode, cEasmid, cSessionKey, cTasktype, cTaskData,
                        OutSID, OutAttachData, cOutData, cOutMac);
                    if (IsValidResult("Obj_Terminal_Formal_GetTerminalSetData", GetSessionData, OutSID, OutAttachData, cOutData, cOutMac))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{GetSessionData.ToString()}");
                        return (true, GetSessionData);

                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{GetSessionData.ToString()}");
                        return (false, GetSessionData);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }


        private static int _GetTerminalSetData;
        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetTerminalSetData(
           [In, Out] int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,
              [Out] byte[] OutSID, byte[] OutAttachData, byte[] cOutData, byte[] cOutMac);
        public (bool Success, int Result) CallObj_Terminal_Formal_GetTerminalSetData(
         [In, Out] int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,
             byte[] OutSID, byte[] OutAttachData, byte[] cOutData, byte[] cOutMac)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    _GetTerminalSetData =
                        Obj_Terminal_Formal_GetTerminalSetData
                        (iOperateMode, cEasmid, cSessionKey, cTaskData,
                        OutSID, OutAttachData, cOutData, cOutMac);
                    if (IsValidResult("Obj_Terminal_Formal_GetTerminalSetData", _GetTerminalSetData, OutSID, OutAttachData, cOutData, cOutMac))
                    {
                        LogMessage.Debug($"Obj_Terminal_Formal_GetTerminalSetData返回有效内容:{_GetTerminalSetData.ToString()}");
                        return (true, _GetTerminalSetData);

                    }
                    else
                    {
                        LogMessage.Debug($"Obj_Terminal_Formal_GetTerminalSetData返回无效内容:{_GetTerminalSetData.ToString()}");
                        return (false, _GetTerminalSetData);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        private static int _VerifyTerminalData;

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_VerifyTerminalData(
           [In, Out] int ikeyState, int iOperateMode, string cEasmid, string cSessionKey, string cTaskData, string cMac,
           byte[] cOutData);
        public (bool Success, int Result) CallObj_Terminal_Formal_VerifyTerminalData(
         [In, Out] int ikeyState, int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,
            string cMac, byte[] cOutData)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(WinSocketServer));
            }
            lock (lockObject)
            {
                try
                {
                    _VerifyTerminalData =
                        Obj_Terminal_Formal_VerifyTerminalData
                        (ikeyState, iOperateMode, cEasmid, cSessionKey, cTaskData,
                        cMac, cOutData);
                    if (IsValidResult("Obj_Terminal_Formal_VerifyTerminalData", _VerifyTerminalData, "cOutData", cOutData))
                    {
                        LogMessage.Debug($"Obj_Terminal_Formal_VerifyTerminalData返回有效内容:{_VerifyTerminalData.ToString()}");
                        return (true, _VerifyTerminalData);
                    }
                    else
                    {
                        LogMessage.Debug($"Obj_Terminal_Formal_VerifyTerminalData返回无效内容:{_VerifyTerminalData.ToString()}");
                        return (false, _VerifyTerminalData);
                    }
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error("内存访问冲突", ex);
                    return (false, -1);
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error("DLL格式错误", ex);
                    return (false, -1);
                }
                catch (Exception ex)
                {
                    LogMessage.Error("DLL调用异常", ex);
                    return (false, -1);
                }
            }
        }
        private bool IsValidResult(string dllMethod, int resultData)
        {
            if (resultData == 0)
            {
                LogMessage.Debug($"调用{dllMethod}成功");
                return true;
            }
            else
            {
                LogMessage.Debug($"调用{dllMethod}失败，错误代码：" + resultData);
                return false;
            }
        }
        private bool IsValidResult(string dllMethod, int resultData,string cMethod, byte[] cOutData)
        {
            if (resultData == 0)
            {
                LogMessage.Debug($"调用{dllMethod}成功");
                LogMessage.Debug($"{cMethod}：" + System.Text.Encoding.Default.GetString(cOutData).TrimEnd());
                return true;
            }
            else
            {
                LogMessage.Debug($"调用{dllMethod}失败，错误代码：" + resultData);
                return false;
            }
        }
        private bool IsValidResult(string dllMethod, int resultData, byte[] cOutData, byte[] cOutData2, byte[] cOutData3)
        {
            if (resultData == 0)
            {
                LogMessage.Debug($"调用{dllMethod}成功");
                LogMessage.Debug("返回加密数据：" + System.Text.Encoding.Default.GetString(cOutData).TrimEnd());
                LogMessage.Debug("返回加密数据：" + System.Text.Encoding.Default.GetString(cOutData2).TrimEnd());
                LogMessage.Debug("返回加密数据：" + System.Text.Encoding.Default.GetString(cOutData3).TrimEnd());
                return true;
            }
            else
            {
                LogMessage.Debug($"调用{dllMethod}失败，错误代码：" + resultData);
                return false;
            }
        }
        private bool IsValidResult(string dllMethod, int resultData, byte[] outSID, byte[] outAttachData, byte[] cOutData, byte[] cOutMac)
        {
            if (resultData == 0)
            {
                LogMessage.Debug($"调用{dllMethod}成功");
                LogMessage.Debug("outSID：" + System.Text.Encoding.Default.GetString(outSID).TrimEnd());
                LogMessage.Debug("outAttachData：" + System.Text.Encoding.Default.GetString(outAttachData).TrimEnd());
                LogMessage.Debug("cOutData：" + System.Text.Encoding.Default.GetString(cOutData).TrimEnd());
                LogMessage.Debug("cOutMac：" + System.Text.Encoding.Default.GetString(cOutMac).TrimEnd());
                return true;
            }
            else
            {
                LogMessage.Debug($"调用{dllMethod}失败，错误代码：" + resultData);
                return false;
            }
        }
        /// <summary>
        /// 加密机所有的接口方法名称
        /// </summary>
        /// <returns></returns>
        public List<string> WinSocketSericeImp()
        {
            return new List<string>()
            {
                "RESAM_Formal_GetKeyData_AppLayer",
                "BT_WESAM_Formal_GetTrmKeyData",
                "CT_Terminal_Formal_CalCTESAMMac",
                "CT_Terminal_Formal_CalCTTESAMMac",
                "CT_Terminal_Formal_CalVerifyCTESAMMac",
                "CT_Terminal_Formal_CalVerifyCTTESAMMac",
                "ClearKeyInfo",
                "CloseDevice",
                "ClseUsbkey",
                "ConnectDevice",
                "CreateRand",
                "Create_Rand",
                "DisEncrptUserInfor",
                "IdentityAuthentication",
                "InCreasePurse",
                "KeyUpdate",
                "LgServer",
                "LgoutServer",
                "Log_WriteLogFile_FlieName",
                "Maccheck",
                "Meter_Formal_DataClear1",
                "Meter_Formal_DataClear2",
                "Meter_Formal_EncMacWrite",
                "Meter_Formal_GetAuthKey",
                "Meter_Formal_IdentityAuthentication",
                "Meter_Formal_IdentityAuthentication_",
"Meter_Formal_InCreasePurse",
"Meter_Formal_InfraredAuth",
"Meter_Formal_InfraredAuth_TermnalToMeter",
"Meter_Formal_InfraredRand",
"Meter_Formal_InintPurse",
"Meter_Formal_KeyUpdateV2",
"Meter_Formal_KeyUpdate_20201118",
"Meter_Formal_MacCheck",
"Meter_Formal_MacWrite",
"Meter_Formal_ParameterElseUpdate",
"Meter_Formal_ParameterUpdate",
"Meter_Formal_ParameterUpdate1",
"Meter_Formal_ParameterUpdate2",
"Meter_Formal_UserControl",
"NMeter_Formal_DataClear1",
"NMeter_Formal_DataClear2",
"NMeter_Formal_GetTrmKeyData",
"NMeter_Formal_ParameterElseUpdate",
"NMeter_Formal_ParameterUpdate",
"NMeter_PowerOff",
"NMeter_PowerOn",
"NMeter_VerifyPowerOff",
"Obj_CT_ESAM_Formal_GetSessionData",
"Obj_CT_ESAM_Formal_GetTrmKeyData",
"Obj_CT_ESAM_Formal_InitSession",
"Obj_CT_ESAM_Formal_VerifyCTData",
"Obj_CT_ESAM_Formal_VerifySession",
"Obj_CT_TESAM_Formal_GetTrmKeyData",
"Obj_ESAM_GN_Formal_GetMacKey",
"Obj_ESAM_GN_Formal_GetPubKey",
"Obj_ESAM_GN_Formal_GetSm4Key",
"Obj_Formal_GetRandHost",
"Obj_InterFace_Formal_BusinessData",
"Obj_InterFace_Formal_GetTrmKeyData",
"Obj_InterFace_Formal_InitSession",
"Obj_InterFace_Formal_ParameterElseUpdate",
"Obj_InterFace_Formal_VerifyBusinessData",
"Obj_InterFace_Formal_VerifyMeterData",
"Obj_InterFace_Formal_VerifySession",
"Obj_InterFace_GetSessionData",
"Obj_JL_Formal_InitSession",
"Obj_JL_Formal_VerifySession",
"Obj_Meter_Formal_EncForCompare",
"Obj_Meter_Formal_GenReadData",
"Obj_Meter_Formal_GetESAMData",
"Obj_Meter_Formal_GetESAMFileData",
"Obj_Meter_Formal_GetGrpBrdCstData",
"Obj_Meter_Formal_GetGrpBrdCstDataNew",
"Obj_Meter_Formal_GetMeterSetData",
"Obj_Meter_Formal_GetPurseData",
"Obj_Meter_Formal_GetResponseData",
"Obj_Meter_Formal_GetSessionData",
"Obj_Meter_Formal_GetTrmKeyData",
"Obj_Meter_Formal_GetTrmKeyData_ForCheck",
"Obj_Meter_Formal_InitSession",
"Obj_Meter_Formal_InitTrmKeyData",
"Obj_Meter_Formal_SetESAMData",
"Obj_Meter_Formal_SetESAMDataNew",
"Obj_Meter_Formal_VerifyESAMData",
"Obj_Meter_Formal_VerifyMeterData",
"Obj_Meter_Formal_VerifyReadData",
"Obj_Meter_Formal_VerifyReportData",
"Obj_Meter_Formal_VerifySession",
"Obj_Meter_Formal_VerifySessionForECard",
"Obj_Meter_JL_VerifyReadData",
"Obj_Meter_JL_VerifyReportData",
"Obj_Meter_Test_GetTrmKeyData",
"Obj_Meter_Test_VerifyESAMData",
"Obj_NMeter_Formal_GetESAMData",
"Obj_NMeter_Formal_SetESAMData",
"Obj_Normal_Formal_InitSession",
"Obj_Normal_Formal_VerifySession",
"Obj_Send_Formal_Data",
"Obj_Send_Formal_DataForGetKey",
"Obj_Terminal_Formal_ChangeDataAuthorize",
"Obj_Terminal_Formal_ExternalAuth",
"Obj_Terminal_Formal_GetCACertificateData",
"Obj_Terminal_Formal_GetGrpBrdCstData",
"Obj_Terminal_Formal_GetMeterSessionKey",
"Obj_Terminal_Formal_GetResponseData",
"Obj_Terminal_Formal_GetSessionData",
"Obj_Terminal_Formal_GetSessionDataForMeter",
"Obj_Terminal_Formal_GetTerminalSetData",
"Obj_Terminal_Formal_GetTerminlMeterKey",
"Obj_Terminal_Formal_GetTrmKeyData",
"Obj_Terminal_Formal_InitSession",
"Obj_Terminal_Formal_InitTrmKeyData",
"Obj_Terminal_Formal_VerifyReadData",
"Obj_Terminal_Formal_VerifyReportData",
"Obj_Terminal_Formal_VerifySession",
"Obj_Terminal_Formal_VerifyTerminalData",
"OpenDevice",
"OpenUsbkey",
"ParameterElseUpdate",
"ParameterUpdate",
"ParameterUpdate1",
"ParameterUpdate2",
"Pcsc_CloseDevice",
"Pcsc_GetDeviceList",
"Pcsc_OpenDevice",
"Pcsc_OpenDeviceSgchip",
"RDID_Formal_RFIDChangeKey",
"RDID_Formal_RFIDCheckData",
"RDID_Formal_RFIDDataMAC",
"RDID_Formal_RFIDDisEncrptData",
"RDID_Formal_RFIDEncrptData",
"RDID_Formal_RFIDGetPin",
"RDID_Formal_SealRFIDChangeKey",
"Seal_ChangekeyF",
"Seal_ChangekeySgchip",
"Seal_ReadData",
"Seal_ReadDataSgchip",
"Seal_WriteCodeDataF",
"Seal_WriteCodeDataSgchip",
"Seal_WriteDataSgchip",
"Set_MeterType",
"Terminal_Formal_CACertificateUpdate",
"Terminal_Formal_CertificateStateChange",
"Terminal_Formal_ChangeDataAuthorize",
"Terminal_Formal_EncTaskData",
"Terminal_Formal_ExternalAuth",
"Terminal_Formal_GetCipherMeterKey",
"Terminal_Formal_GetR1",
"Terminal_Formal_GroupBroadcast",
"Terminal_Formal_InternalAuth",
"Terminal_Formal_MACVerify",
"Terminal_Formal_SessionConsultVerify",
"Terminal_Formal_SessionConsultVerify_",
"Terminal_Formal_SessionInitRec",
"Terminal_Formal_SessionKeyConsult",
"Terminal_Formal_SessionKeyConsult_",
"Terminal_Formal_SessionRecoveryVerify",
"Terminal_Formal_SessionRecoveryVerify_",
"Terminal_Formal_SetOfflineCounter",
"Terminal_Formal_SymmetricKeyUpdate",
"Terminal_Formal_SymmetricKeyUpdateCT",
"Terminal_Formal_SymmetricKeyUpdateNT",
"Terminal_Formal_SystemBroadcast",
"Terminal_Formal_WriteTEsam",
"UserControl",
"WESAM_Formal_EncrypteData",
"WESAM_Formal_GetSessionData",
"WESAM_Formal_GetTrmKeyDataForMeteringBox",
"WESAM_Formal_InitSession",
"WESAM_Formal_SetESAMData",
"WESAM_Formal_VerifyData",
"WESAM_Formal_VerifyReadData",
"WESAM_Formal_VerifyReportData",
"WESAM_Formal_VerifySession",
"Write_SealRDID",
"Write_SealRFIDForCheckData",
"Write_SealRFIDForSceneData",
"YESAM_Formal_ChangeSealKey",
"YESAM_Formal_GetSealKey",
"YESAM_Formal_GetSessionData",
"YESAM_Formal_GetTrmKeyData",
"YESAM_Formal_GetWESAMEncrptKey",
"YESAM_Formal_GetWESAMSessionKeyForMeteringBox",
"YESAM_Formal_InitSessionOffline",
"YESAM_Formal_VerifyData",
"YESAM_Formal_VerifySessionOffline",
"testapi",
            };
        }

        private bool _disposed = false;
        public void Dispose()
        {
            if (!_disposed)
            {
                // 清理资源
                _disposed = true;
            }
        }
    }
}
