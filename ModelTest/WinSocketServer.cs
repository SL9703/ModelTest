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
        public int res =-1;
        private readonly object lockObject = new object();//lock对象
        private static void HandleException(Exception ex)
        {
            if (ex != null)
            {
                // 记录日志
                LogMessage.Error(ex);
            }
        }

        public byte[] RandNum;
        /// <summary>
        /// 登录加密机服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="Ctime"></param>
        /// <returns></returns>
        [DllImport("WinSocketServer.dll")]
        private static extern int ConnectDevice(string ip, string port, string Ctime);

        public int ConnectDeviceEx(string ip, string port, string Ctime)
        {
            try
            {
                int res= ConnectDevice(ip, port, Ctime);
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return res;
            }
        }
        /// <summary>
        /// 产生随机数
        /// </summary>
        /// <param name="InRand"></param>
        /// <param name="OutRand"></param>
        /// <returns></returns>
        [DllImport("WinSocketServer.dll")]
        private static extern int CreateRand([In,Out] int InRand, [Out] byte[] OutRand);
        //byte[] OutRand = new byte[128];
        public int CreateRandEx(int InRand, [Out] byte[] OutRand)
        {
            
            try
            {
                RandNum = new byte[128];
                res =  CreateRand(InRand, OutRand);
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex); return res;
            }
        }

        [DllImport("WinSocketServer.dll")]
        private static extern int RESAM_Formal_GetKeyData_AppLayer([In, Out] 
        int iOperateMode,string cTESAMID,string cSessionKey, int cTaskType, string cTaskData,
        [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC);

        public int ReSAM_Formal_GetKeyData_AppLayer(
            int iOperateMode, string cTESAMID, string cSessionKey, int cTaskType, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC)
        {
            try
            {
                res= RESAM_Formal_GetKeyData_AppLayer(iOperateMode, cTESAMID, cSessionKey, cTaskType, cTaskData,
                 cOutSID, cOutAttachData, cOutData, cOutMAC);
                LogMessage.Debug(System.Text.Encoding.Default.GetString(cOutSID));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(cOutAttachData));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(cOutData));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(cOutMAC));
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex); return res;
            }
        }
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
        public int Call_Obj_Meter_Formal_SetESAMData(int InKeyState, int InOperateMode, string cESAMNO, string cSessionKey,
            string cMeterNo, string cESAMRand, string cData, [Out] byte[] OutSID, [Out] byte[] OutAddData,
            [Out] byte[] OutData, [Out] byte[] OutMAC)
        {
            try
            {
                res =  Obj_Meter_Formal_SetESAMData(InKeyState, InOperateMode, cESAMNO, cSessionKey,
             cMeterNo, cESAMRand, cData, OutSID, OutAddData,
             OutData, OutMAC);
                LogMessage.Debug(System.Text.Encoding.Default.GetString(OutSID));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(OutAddData));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(OutData));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(OutMAC));
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex); return res;
            }
        }
        /// <summary>
        /// 断开密码机
        /// </summary>
        /// <returns></returns>
        [DllImport("WinSocketServer.dll")]
        private static extern int CloseDevice();
        public int CloseDeviceEx()
        {
            try
            {
                res = CloseDevice();
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex); return res;
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
                res=  ClseUsbkeyEx();
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
        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_DataClear1([In,Out] int Flag,string PutRand,string PutDiv,string PutData,[Out] byte[] Outdata);
        public int Meter_Formal_DataClear1Ex(int flag, string putRand,string putDiv,string putData, [Out] byte[] outdata)
        {
            try
            {
                res= Meter_Formal_DataClear1(flag,putRand,putDiv,putData,outdata);
                LogMessage.Debug(System.Text.Encoding.Default.GetString(outdata));
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex); return res;
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
        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_DataClear2([In, Out] int Flag, string PutRand, string PutDiv, string PutData, [Out] byte[] Outdata);
        public int Meter_Formal_DataClear2Ex(int flag, string putRand, string putDiv, string putData, [Out] byte[] outdata)
        {
            try
            {
                res= Meter_Formal_DataClear2(flag, putRand, putDiv, putData, outdata);
                LogMessage.Debug(System.Text.Encoding.Default.GetString(outdata));
                return res;
            }
            catch (Exception ex)
            {
                 HandleException(ex); return  res;
            }
        }

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetTrmKeyData([In, Out] int iKeyVersion, string strEsamNo, string strSessionKey,string cTerminalAddress, string strKeyType, [Out] byte[] strOutSID, [Out] byte[] strOutAttachData, [Out] byte[] strOutTrmKeyData,[Out] byte[] strOutMAC);
        public int Obj_Terminal_Formal_GetTrmKeyDataEx(int iKeyVersion, string strEsamNo, string strSessionKey,string cTerminalAddress, string strKeyType, [Out] byte[] strOutSID, [Out] byte[] strOutAttachData, [Out] byte[] strOutTrmKeyData, [Out] byte[] strOutMAC)
        {
            try
            {
                res = Obj_Terminal_Formal_GetTrmKeyData(iKeyVersion, strEsamNo, strSessionKey, strKeyType, cTerminalAddress, strOutSID, strOutAttachData, strOutTrmKeyData, strOutMAC);
                Thread.Sleep(2000);
                LogMessage.Debug(System.Text.Encoding.Default.GetString(strOutSID));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(strOutAttachData));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(strOutTrmKeyData));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(strOutMAC));
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex); 
                return res;
            }
        }

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_InitSession([In, Out] int iKeyVersion, string cTESAMID,string cASCTR, string cFLG, string cMasterCert,  [Out] byte[] cOutRandHost, [Out] byte[] cOutSessionInit, [Out] byte[] cOutSign);
        public int Obj_Terminal_Formal_InitSessionEx(int iKeyVersion, string cTESAMID, string cASCTR, string cFLG, string cMasterCert, [Out] byte[] cOutRandHost, [Out] byte[] cOutSessionInit, [Out] byte[] cOutSign)
        {
            try
            {
                res = Obj_Terminal_Formal_InitSession(iKeyVersion, cTESAMID, cASCTR, cFLG, cMasterCert, cOutRandHost, cOutSessionInit, cOutSign);
                LogMessage.Debug(System.Text.Encoding.Default.GetString(cOutRandHost));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(cOutSessionInit));
                LogMessage.Debug(System.Text.Encoding.Default.GetString(cOutSign));
                return res;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return res;
            }
        }
        private static int GetSessionData;
       [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetSessionData(
            [In, Out]int iOperateMode,string cEasmid ,string cSessionKey, int cTasktype,string cTaskData,
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
                    if (IsValidResult("Obj_Terminal_Formal_GetTerminalSetData",_GetTerminalSetData, OutSID, OutAttachData, cOutData, cOutMac))
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
           [In, Out] int ikeyState, int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,string cMac,
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
                        (ikeyState,iOperateMode, cEasmid, cSessionKey, cTaskData,
                        cMac, cOutData);
                    if (IsValidResult("Obj_Terminal_Formal_VerifyTerminalData", _VerifyTerminalData, cOutData))
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

        private bool IsValidResult(string dllMethod, int verifyTerminalData, byte[] cOutData)
        {
            if (verifyTerminalData == 0)
            {
                LogMessage.Debug($"调用{dllMethod}成功");
                LogMessage.Debug("返回加密数据：" + System.Text.Encoding.Default.GetString(cOutData));
                return true;
            }
            else
            {
                LogMessage.Debug("调用Obj_Terminal_Formal_GetSessionData失败，错误代码：" + verifyTerminalData);
                return false;
            }
        }
        private bool IsValidResult(string dllMethod, int getSessionData, byte[] outSID, byte[] outAttachData, byte[] cOutData, byte[] cOutMac)
        {
            if (getSessionData == 0)
            {
                LogMessage.Debug($"调用{dllMethod}成功");
                LogMessage.Debug("返回加密数据：" + System.Text.Encoding.Default.GetString(outSID));
                LogMessage.Debug("返回加密数据：" + System.Text.Encoding.Default.GetString(outAttachData));
                LogMessage.Debug("返回加密数据：" + System.Text.Encoding.Default.GetString(cOutData));
                LogMessage.Debug("返回加密数据：" + System.Text.Encoding.Default.GetString(cOutMac));
                return true;
            }
            else
            {
                LogMessage.Debug("调用Obj_Terminal_Formal_GetSessionData失败，错误代码：" + getSessionData);
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
