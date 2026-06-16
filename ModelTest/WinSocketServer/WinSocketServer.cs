using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public class WinSocketServer : IDisposable
    {

        #region 字段和常量

        private readonly object _lockObject = new();
        private readonly IWinSocketServiceCatalog _serviceCatalog;
        private bool _disposed;

        // 缓冲区默认大小常量
        private static class BufferSizes
        {
            public const int DefaultRand = 128;
            public const int DefaultSID = 32;
            public const int DefaultAttachData = 64;
            public const int DefaultData = 256;
            public const int DefaultMAC = 32;
            public const int RandHost = 32;
            public const int SessionInit = 64;
            public const int Sign = 64;
        }

        // 返回码常量
        private const int SuccessCode = 0;
        private const int ErrorCode = -1;

        #endregion

        public WinSocketServer()
            : this(new DefaultWinSocketServiceCatalog())
        {
        }

        public WinSocketServer(IWinSocketServiceCatalog serviceCatalog)
        {
            _serviceCatalog = serviceCatalog ?? throw new ArgumentNullException(nameof(serviceCatalog));
        }

        #region 结果封装类

        /// <summary>
        /// DLL调用结果
        /// </summary>
        public readonly struct DllResult
        {
            public bool Success { get; }
            public int Code { get; }

            public DllResult(bool success, int code)
            {
                Success = success;
                Code = code;
            }

            public static DllResult Ok() => new(true, SuccessCode);
            public static DllResult Fail(int code = ErrorCode) => new(false, code);

            public void Deconstruct(out bool success, out int code)
            {
                success = Success;
                code = Code;
            }
        }

        #endregion
        #region 基础DLL导入

        [DllImport("WinSocketServer.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        private static extern int ConnectDevice(string ip, string port, string ctime);

        [DllImport("WinSocketServer.dll")]
        private static extern int CloseDevice();

        [DllImport("WinSocketServer.dll")]
        private static extern int CreateRand([In, Out] int inRand, [Out] byte[] outRand);

        [DllImport("WinSocketServer.dll")]
        private static extern int ClseUsbkey();

        #endregion

        #region 业务DLL导入

        [DllImport("WinSocketServer.dll")]
        private static extern int RESAM_Formal_GetKeyData_AppLayer(
            [In, Out] int iOperateMode, string cTESAMID, string cSessionKey, int cTaskType, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_SetESAMData(
            [In, Out] int inKeyState, int inOperateMode, string cESAMNO, string cSessionKey,
            string cMeterNo, string cESAMRand, string cData,
            [Out] byte[] outSID, [Out] byte[] outAddData, [Out] byte[] outData, [Out] byte[] outMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GetESAMData(
            [In, Out] int iKeyState, int iOperateMode, string cMeterNo, string cOAD,
            [Out] byte[] cOutRandHost, [Out] byte[] cOutSID, [Out] byte[] cOutAttachData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_VerifyESAMData(
            [In, Out] int inKeyState, int iOperateMode, string cESAMNO, string cMeterNo, string cRandHost,
            string cOAD, string cTaskData, string cMAC, [Out] byte[] cOutData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GetPurseData(
            [In, Out] int iOperateMode, string cESAMNO, string cSessionKey, int cTaskType, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GetMeterSetData(
            [In, Out] int iOperateMode, string cESAMNO, string cSessionKey, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GetGrpBrdCstData(
            [In, Out] int iKeyState, int iOperateMode, string cESAMNO, string cGrpKID, string cDIV, string aGSEQ,
            string cBrdCstData, [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GetTrmKeyData(
            [In, Out] int iKeyState, string cESAMNO, string cSessionKey, string cMeterNo, string cKeyType,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutTrmKeyData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_InitTrmKeyData(
            [In, Out] int iKeyState, string cESAMNO, string cSessionKey, string cMeterNo, string cKeyType,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutTrmKeyData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GetESAMFileData(
            [In, Out] int inKeyState, int iOperateMode, string meterNo, string fileName, string fileLenth,
            [Out] byte[] cOutRandHost, [Out] byte[] cOutSID, [Out] byte[] cOutAttachData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_DataClear1(
            [In, Out] int flag, string putRand, string putDiv, string putData, [Out] byte[] outData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_DataClear2(
            [In, Out] int flag, string putRand, string putDiv, string putData, [Out] byte[] outData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_IdentityAuthentication(
            [In, Out] int flag, string putDiv, [Out] byte[] outRand, [Out] byte[] outEndata);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_UserControl(
            [In, Out] int flag, string putRand, string putDiv, string putESAMNo, string putData, [Out] byte[] outEndata);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_ParameterUpdate(
            [In, Out] int flag, string putRand, string putDiv, string putApdu, string putData, [Out] byte[] outEndata);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_ParameterElseUpdate(
            [In, Out] int flag, string putRand, string putDiv, string putApdu, string putData, [Out] byte[] outEndata);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_InfraredAuth(
            [In, Out] int flag, string putDiv, string putESAMNo, string putRand1, string putRand1Endata,
            [Out] byte[] putRand2, [Out] byte[] outRand2Endata);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_MacCheck(
            [In, Out] int flag, string putRand, string putDiv, string putApdu, string putData, string putMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_KeyUpdateV2(
            [In, Out] int putKeySum, string putKeystate, string putKeyid, string putRand, string putDiv,
            string putESAMNo, string putChipInfor, [Out] byte[] outData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetTrmKeyData(
            [In, Out] int iKeyVersion, string strEsamNo, string strSessionKey, string cTerminalAddress, string strKeyType,
            [Out] byte[] strOutSID, [Out] byte[] strOutAttachData, [Out] byte[] strOutTrmKeyData, [Out] byte[] strOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_InitSession(
            [In, Out] int iKeyVersion, string cTESAMID, string cASCTR, string cFLG, string cMasterCert,
            [Out] byte[] cOutRandHost, [Out] byte[] cOutSessionInit, [Out] byte[] cOutSign);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_InitSession_RH(
            int iKeyVersion, string cTESAMID, string cASCTR,
            IntPtr cMasterCert, IntPtr cOutRandHost, IntPtr cOutSessionInit, IntPtr cOutSign);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetSessionData(
            [In, Out] int iOperateMode, string cEasmid, string cSessionKey, int cTasktype, string cTaskData,
            [Out] byte[] outSID, [Out] byte[] outAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetTerminalSetData(
            [In, Out] int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,
            [Out] byte[] outSID, byte[] outAttachData, byte[] cOutData, byte[] cOutMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetGrpBrdCstData(
            [In, Out] int iKeyState, int iOperateMode, string cTESAMID, string cGrpKID, string cDIV, string aGSEQ,
            string cBrdCstData, [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_VerifyTerminalData(
            [In, Out] int ikeyState, int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,
            string cMac, byte[] cOutData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_VerifyReadData(
            [In, Out] int iKeyState, int iOperateMode, string cTESAMID, string cRandHost, string cReadData,
            string cMac, [Out] byte[] cOutData, [Out] byte[] cOutRSPCTR);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_VerifyReportData(
            [In, Out] int iKeyState, int iOperateMode, string cTESAMID, string cRandT, string cReportData,
            string cMac, [Out] byte[] cOutData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetResponseData(
            [In, Out] int iKeyState, int iOperateMode, string cTESAMID, string cRandT, string cReportData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Send_Formal_DataForGetKey(
            [In, Out] string inDeviceType, string cTasktype, string cKeyState, string cTESAMID, string inMeterNo, string cSessionKey,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, byte[] cOutTaskData, byte[] cOutTaskMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GenReadData(
            [In, Out] string iKeyVersion, string strEsamNo, string strMeterNo, string iOperateMode, string randHost, string cReadData,
            [Out] byte[] strOutData, [Out] byte[] strOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GetSessionData(
            [In, Out] int iOperateMode, string cESAMNO, string cSessionKey, int cTaskType, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_VerifyMeterData(
            [In, Out] int iKeyState, int iOperateMode, string cESAMNO, string cSessionKey, string cTaskData,
            string cMac, [Out] byte[] cOutData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_VerifySession(
            [In, Out] int iKeyState, string cDiv, string cRandHost, string cSessionData, string cSign, [Out] byte[] cOutSessionKey);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_VerifyReadData(
            [In, Out] int iKeyState, int iOperateMode, string cMeterNo, string cRandHost, string cReadData,
            string cMac, [Out] byte[] cOutData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_VerifyReportData(
            [In, Out] int iKeyState, int iOperateMode, string cMeterNo, string cRandT, string cReportData,
            string cMac, [Out] byte[] cOutData, [Out] byte[] cOutRSPCTR);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GetResponseData(
            [In, Out] int iKeyState, int iOperateMode, string cMeterNo, string randHost, string cReportData,
            [Out] byte[] outSID, [Out] byte[] outAttachData, [Out] byte[] cOutData, [Out] byte[] ucOutMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetSessionDataForMeter(
            [In, Out] int cOperateMode, string cTESAMID, string cSessionKey, int iTaskType, string cApdu, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutTaskData, [Out] byte[] cOutTaskMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_InitTrmKeyData(
            [In, Out] int iKeyState, string cTESAMID, string cSessionKey, string cTerminalAddress, string cKeyType,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutTrmKeyData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetCACertificateData(
            [In, Out] int iKeyState, string cTESAMID, string cSessionKey, string cCerType,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutCertificateData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_VerifySession_RH(
            [In, Out] int iKeyState, string cTESAMNO, string cRandHost, string cSessionData, string cSign,
            string cTerminalCert, [Out] byte[] cOutSessionKey);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetSessionData_RH(
            string cTESAMNO, int iOperateMode, string keyID, string cSessionKey, int cTaskType, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutData, [Out] byte[] cOutSign);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_VerifyData_RH(
            [In, Out] int iKeyState, int iOperateMode, string cTESAMNO, string cSessionKey, string cTerminalCert,
            string cTaskData, string cSign, [Out] byte[] cOutData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetTrmKeyData_RH(
            [In, Out] int iKeyState, string cTESAMNO, string cIV, string inKid, string inCert,
            [Out] byte[] ucOutTrmKeyData, [Out] byte[] cOutSign);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetCACertificateData_RH(
            [In, Out] int iKeyState, string cTESAMNO, string cSessionKey, int iCertType, string cCert,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutCertificateData, [Out] byte[] cOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int RH_InternalSign(int sConnect, string cTESAMID, int iKeyIndex, string cCert, string inData, [Out] byte[] outSign);

        [DllImport("WinSocketServer.dll")]
        private static extern int RH_VerifySig(int sConnect, string cTESAMID, string cCert, string inData, string inSign);

        [DllImport("WinSocketServer.dll")]
        private static extern int RH_EncData(
            int sConnect, string cTESAMID, string cKeyVer, string cKeyIndex, string cDivData, string cIvData,
            string cInData, [Out] byte[] cOutEncData, [Out] byte[] cOutMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int RH_DisEncData(
            int sConnect, string cTESAMID, string cKeyVer, string cKeyIndex, string cDivData, string cIvData,
            string cInEncData, string cInMac, [Out] byte[] cOutData);

        [DllImport("WinSocketServer.dll")]
        private static extern int RH_Terminal_Formal_GetTrmKeyData(
            [In, Out] int iKeyState, string cTESAMID, string cIV, string inKid, string inCert,
            [Out] byte[] ucOutTrmKeyData, [Out] byte[] cOutSign);
        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_InitSession([In, Out] int iKeyState, string cDiv, string cASCTR, string cFLG,
          [Out] byte[] strOutRandHost, [Out] byte[] strOutSessionInit, [Out] byte[] strOutMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_GetR1([Out] byte[] outR1);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_SessionInitRec(
            string putState, string putTESAMNo, string putVersionNum, string putSessionID, string putR1,
            [Out] byte[] outMasterCertificate, [Out] byte[] outEncR1, [Out] byte[] outMac, [Out] byte[] outSign1);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_SessionKeyConsult(
            string putState, string putTESAMNo, string putVersionNum, string putSessionID,
            string putCRLCertificateNo, string putMasterCertificateNo, string putTerminalCertificate,
            string putEncR2, string putSign2, string putR1,
            [Out] byte[] outEncM1, [Out] byte[] outSign3, [Out] byte[] outMac2, [Out] byte[] outSign4);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_SessionConsultVerify(string putR3, string putMac3);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_SessionRecoveryVerify(
            string putState, string putTESAMNo, string putVersionNum, string putSessionID,
            string putEncR2, string putR3, string putMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_MACVerify(
            string putState, string putTESAMNo, string putFnType, string putData, [Out] byte[] outMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_ExternalAuth(
            string putState, string putTESAMNo, string putR4, string putEncR4, string putR5, [Out] byte[] outEncR5);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_CertificateStateChange(
            string putState, string putTESAMNo, string putCertificateState, string putR6,
            [Out] byte[] outEncR6, [Out] byte[] outMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_SetOfflineCounter(
            string putState, string putTESAMNo, string putCounter, [Out] byte[] outEncCounter);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_ChangeDataAuthorize([Out] byte[] outData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_GetCipherMeterKey(
            string putMeterState, string putMeterNo, int putTaskType, [Out] byte[] outMeterEncKey);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_EncTaskData(
            [In, Out] int putInDataType, string putTaskData, [Out] byte[] outTaskData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_GroupBroadcast(
            string putState, string putTESAMNo, string putFnType, [In, Out] int putOutDataType,
            string putGroupAdrass, string putMtime, string putBroadcastData, [Out] byte[] outMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_SystemBroadcast(
            string putState, string putTESAMNo, string putFnType, [In, Out] int putOutDataType,
            string putGroupAdrass, string putMtime, string putBroadcastData, [Out] byte[] outMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_SymmetricKeyUpdate(
            string putState, string putTESAMNo, [Out] byte[] outKeyNum, [Out] byte[] outEncKeyData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_CACertificateUpdate(
            string putCertificateState, string putCertificateType, [Out] byte[] outEncCertificateData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Terminal_Formal_SymmetricKeyUpdateCT(
            string putState, string putTESAMNo, [Out] byte[] outKeyNum, [Out] byte[] outEncKeyData);

        [DllImport("WinSocketServer.dll")]
        private static extern int CT_Terminal_Formal_GetCTESAMKey(
            [In, Out] int inFlag, string inKeyid, string inCTTESAMNo, string inCTESAMNo, string inRand,
            [Out] byte[] outKey, [Out] byte[] outMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int CT_Terminal_Formal_CalCTESAMMac(
            [In, Out] int inFlag, string inCTESAMNo, string inRand, string inData, [Out] byte[] outMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int CT_Terminal_Formal_CalCTTESAMMac(
            string putState, string inCTTESAMNo, string inRand, string inData, [Out] byte[] outMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int CT_Terminal_Formal_CalVerifyCTESAMMac(
            [In, Out] int inFlag, string inCTESAMNo, string inRand, string inData, string inMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int CT_Terminal_Formal_CalVerifyCTTESAMMac(
            string putState, string inCTTESAMNo, string inRand, string inData, string inMac);

        [DllImport("WinSocketServer.dll")]
        private static extern int RDID_Formal_RFIDChangeKey(
            string putKeyState, string putDiv, [Out] byte[] outKey);

        [DllImport("WinSocketServer.dll")]
        private static extern int RDID_Formal_RFIDEncrptData(
            string putKeyState, string putDiv, string putData, string putOPInfor,
            [Out] byte[] outEncData, [Out] byte[] outMAC2);

        [DllImport("WinSocketServer.dll")]
        private static extern int RDID_Formal_RFIDDisEncrptData(
            string putKeyState, string putDiv, string putEncData, string putOPInfor, string putMAC,
            [Out] byte[] outData);

        [DllImport("WinSocketServer.dll")]
        private static extern int RDID_Formal_RFIDCheckData(
            string inKeyState, [In, Out] int inDataNo, string putDiv, string putData, string putMac);
        #endregion

        #region 核心执行方法

        /// <summary>
        /// 执行DLL调用（无输出参数）
        /// </summary>
        private DllResult ExecuteDllCall(string methodName, Func<int> dllCall)
        {
            EnsureNotDisposed();

            lock (_lockObject)
            {
                try
                {
                    int result = dllCall();
                    bool isValid = ValidateResult(methodName, result);
                    return new DllResult(isValid, result);
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error($"[{methodName}] 内存访问冲突", ex);
                    return DllResult.Fail();
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error($"[{methodName}] DLL格式错误", ex);
                    return DllResult.Fail();
                }
                catch (SEHException ex)
                {
                    LogMessage.Error($"[{methodName}] 结构化异常", ex);
                    return DllResult.Fail();
                }
                catch (Exception ex)
                {
                    LogMessage.Error($"[{methodName}] DLL调用异常", ex);
                    return DllResult.Fail();
                }
            }
        }

        /// <summary>
        /// 执行DLL调用（带字节数组输出参数）
        /// </summary>
        private DllResult ExecuteDllCallWithByteOutputs(
            string methodName,
            Func<int> dllCall,
            params (string Name, byte[] Data)[] outputs)
        {
            EnsureNotDisposed();

            lock (_lockObject)
            {
                try
                {
                    int result = dllCall();
                    bool isValid = ValidateResult(methodName, result, outputs);
                    return new DllResult(isValid, result);
                }
                catch (AccessViolationException ex)
                {
                    LogMessage.Error($"[{methodName}] 内存访问冲突", ex);
                    return DllResult.Fail();
                }
                catch (BadImageFormatException ex)
                {
                    LogMessage.Error($"[{methodName}] DLL格式错误", ex);
                    return DllResult.Fail();
                }
                catch (SEHException ex)
                {
                    LogMessage.Error($"[{methodName}] 结构化异常", ex);
                    return DllResult.Fail();
                }
                catch (Exception ex)
                {
                    LogMessage.Error($"[{methodName}] DLL调用异常", ex);
                    return DllResult.Fail();
                }
            }
        }

        #endregion

        #region 公共方法

        public DllResult ConnectDeviceEx(string ip, string port, string ctime)
        {
            return ExecuteDllCall(nameof(ConnectDevice), () => ConnectDevice(ip, port, ctime));
        }

        public DllResult CloseDeviceEx()
        {
            return ExecuteDllCall(nameof(CloseDevice), CloseDevice);
        }

        public DllResult CreateRandEx(int inRand, byte[] outRand)
        {
            if (outRand == null) throw new ArgumentNullException(nameof(outRand));

            return ExecuteDllCallWithByteOutputs(
                nameof(CreateRand),
                () => CreateRand(inRand, outRand),
                (nameof(outRand), outRand));
        }

        public int ClseUsbkeyEx()
        {
            try
            {
                return ClseUsbkey();
            }
            catch (Exception ex)
            {
                LogMessage.Error(ex);
                return ErrorCode;
            }
        }

        public DllResult CallReSAM_Formal_GetKeyData_AppLayer(
            int iOperateMode, string cTESAMID, string cSessionKey, int cTaskType, string cTaskData,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(RESAM_Formal_GetKeyData_AppLayer),
                () => RESAM_Formal_GetKeyData_AppLayer(iOperateMode, cTESAMID, cSessionKey, cTaskType, cTaskData,
                                                       cOutSID, cOutAttachData, cOutData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallObj_Meter_Formal_SetESAMData(
            int inKeyState, int inOperateMode, string cESAMNO, string cSessionKey,
            string cMeterNo, string cESAMRand, string cData,
            byte[] outSID, byte[] outAddData, byte[] outData, byte[] outMAC)
        {
            ValidateByteArrayParams((outSID, nameof(outSID)), (outAddData, nameof(outAddData)),
                                    (outData, nameof(outData)), (outMAC, nameof(outMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_SetESAMData),
                () => Obj_Meter_Formal_SetESAMData(inKeyState, inOperateMode, cESAMNO, cSessionKey,
                                                   cMeterNo, cESAMRand, cData, outSID, outAddData, outData, outMAC),
                (nameof(outSID), outSID), (nameof(outAddData), outAddData),
                (nameof(outData), outData), (nameof(outMAC), outMAC));
        }

        public DllResult CallObj_Meter_Formal_GetESAMData(
            int iKeyState, int iOperateMode, string cMeterNo, string cOAD,
            byte[] cOutRandHost, byte[] cOutSID, byte[] cOutAttachData)
        {
            ValidateByteArrayParams((cOutRandHost, nameof(cOutRandHost)),
                                    (cOutSID, nameof(cOutSID)),
                                    (cOutAttachData, nameof(cOutAttachData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GetESAMData),
                () => Obj_Meter_Formal_GetESAMData(iKeyState, iOperateMode, cMeterNo, cOAD, cOutRandHost, cOutSID, cOutAttachData),
                (nameof(cOutRandHost), cOutRandHost),
                (nameof(cOutSID), cOutSID),
                (nameof(cOutAttachData), cOutAttachData));
        }

        public DllResult CallObj_Meter_Formal_VerifyESAMData(
            int inKeyState, int iOperateMode, string cESAMNO, string cMeterNo, string cRandHost,
            string cOAD, string cTaskData, string cMAC, byte[] cOutData)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_VerifyESAMData),
                () => Obj_Meter_Formal_VerifyESAMData(inKeyState, iOperateMode, cESAMNO, cMeterNo, cRandHost, cOAD, cTaskData, cMAC, cOutData),
                (nameof(cOutData), cOutData));
        }

        public DllResult CallObj_Meter_Formal_GetPurseData(
            int iOperateMode, string cESAMNO, string cSessionKey, int cTaskType, string cTaskData,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GetPurseData),
                () => Obj_Meter_Formal_GetPurseData(iOperateMode, cESAMNO, cSessionKey, cTaskType, cTaskData,
                                                    cOutSID, cOutAttachData, cOutData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallObj_Meter_Formal_GetMeterSetData(
            int iOperateMode, string cESAMNO, string cSessionKey, string cTaskData,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GetMeterSetData),
                () => Obj_Meter_Formal_GetMeterSetData(iOperateMode, cESAMNO, cSessionKey, cTaskData,
                                                       cOutSID, cOutAttachData, cOutData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallObj_Meter_Formal_GetGrpBrdCstData(
            int iKeyState, int iOperateMode, string cESAMNO, string cGrpKID, string cDIV, string aGSEQ,
            string cBrdCstData, byte[] cOutSID, byte[] cOutAttachData, byte[] cOutData, byte[] cOutMac)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMac, nameof(cOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GetGrpBrdCstData),
                () => Obj_Meter_Formal_GetGrpBrdCstData(iKeyState, iOperateMode, cESAMNO, cGrpKID, cDIV, aGSEQ, cBrdCstData,
                                                        cOutSID, cOutAttachData, cOutData, cOutMac),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMac), cOutMac));
        }

        public DllResult CallObj_Meter_Formal_GetTrmKeyData(
            int iKeyState, string cESAMNO, string cSessionKey, string cMeterNo, string cKeyType,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutTrmKeyData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutTrmKeyData, nameof(cOutTrmKeyData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GetTrmKeyData),
                () => Obj_Meter_Formal_GetTrmKeyData(iKeyState, cESAMNO, cSessionKey, cMeterNo, cKeyType,
                                                     cOutSID, cOutAttachData, cOutTrmKeyData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutTrmKeyData), cOutTrmKeyData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallObj_Meter_Formal_InitTrmKeyData(
            int iKeyState, string cESAMNO, string cSessionKey, string cMeterNo, string cKeyType,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutTrmKeyData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutTrmKeyData, nameof(cOutTrmKeyData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_InitTrmKeyData),
                () => Obj_Meter_Formal_InitTrmKeyData(iKeyState, cESAMNO, cSessionKey, cMeterNo, cKeyType,
                                                      cOutSID, cOutAttachData, cOutTrmKeyData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutTrmKeyData), cOutTrmKeyData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallObj_Meter_Formal_GetESAMFileData(
            int inKeyState, int iOperateMode, string meterNo, string fileName, string fileLenth,
            byte[] cOutRandHost, byte[] cOutSID, byte[] cOutAttachData)
        {
            ValidateByteArrayParams((cOutRandHost, nameof(cOutRandHost)),
                                    (cOutSID, nameof(cOutSID)),
                                    (cOutAttachData, nameof(cOutAttachData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GetESAMFileData),
                () => Obj_Meter_Formal_GetESAMFileData(inKeyState, iOperateMode, meterNo, fileName, fileLenth,
                                                       cOutRandHost, cOutSID, cOutAttachData),
                (nameof(cOutRandHost), cOutRandHost),
                (nameof(cOutSID), cOutSID),
                (nameof(cOutAttachData), cOutAttachData));
        }

        public DllResult CallMeter_Formal_DataClear1(int flag, string putRand, string putDiv, string putData, byte[] outData)
        {
            ValidateByteArrayParams((outData, nameof(outData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Meter_Formal_DataClear1),
                () => Meter_Formal_DataClear1(flag, putRand, putDiv, putData, outData),
                (nameof(outData), outData));
        }

        public DllResult CallMeter_Formal_DataClear2(int flag, string putRand, string putDiv, string putData, byte[] outData)
        {
            ValidateByteArrayParams((outData, nameof(outData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Meter_Formal_DataClear2),
                () => Meter_Formal_DataClear2(flag, putRand, putDiv, putData, outData),
                (nameof(outData), outData));
        }

        public DllResult CallMeter_Formal_IdentityAuthentication(int flag, string putDiv, byte[] outRand, byte[] outEndata)
        {
            ValidateByteArrayParams((outRand, nameof(outRand)), (outEndata, nameof(outEndata)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Meter_Formal_IdentityAuthentication),
                () => Meter_Formal_IdentityAuthentication(flag, putDiv, outRand, outEndata),
                (nameof(outRand), outRand), (nameof(outEndata), outEndata));
        }

        public DllResult CallMeter_Formal_UserControl(
            int flag, string putRand, string putDiv, string putESAMNo, string putData, byte[] outEndata)
        {
            ValidateByteArrayParams((outEndata, nameof(outEndata)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Meter_Formal_UserControl),
                () => Meter_Formal_UserControl(flag, putRand, putDiv, putESAMNo, putData, outEndata),
                (nameof(outEndata), outEndata));
        }

        public DllResult CallMeter_Formal_ParameterUpdate(
            int flag, string putRand, string putDiv, string putApdu, string putData, byte[] outEndata)
        {
            ValidateByteArrayParams((outEndata, nameof(outEndata)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Meter_Formal_ParameterUpdate),
                () => Meter_Formal_ParameterUpdate(flag, putRand, putDiv, putApdu, putData, outEndata),
                (nameof(outEndata), outEndata));
        }

        public DllResult CallMeter_Formal_ParameterElseUpdate(
            int flag, string putRand, string putDiv, string putApdu, string putData, byte[] outEndata)
        {
            ValidateByteArrayParams((outEndata, nameof(outEndata)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Meter_Formal_ParameterElseUpdate),
                () => Meter_Formal_ParameterElseUpdate(flag, putRand, putDiv, putApdu, putData, outEndata),
                (nameof(outEndata), outEndata));
        }

        public DllResult CallMeter_Formal_InfraredAuth(
            int flag, string putDiv, string putESAMNo, string putRand1, string putRand1Endata,
            byte[] putRand2, byte[] outRand2Endata)
        {
            ValidateByteArrayParams((putRand2, nameof(putRand2)), (outRand2Endata, nameof(outRand2Endata)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Meter_Formal_InfraredAuth),
                () => Meter_Formal_InfraredAuth(flag, putDiv, putESAMNo, putRand1, putRand1Endata, putRand2, outRand2Endata),
                (nameof(putRand2), putRand2), (nameof(outRand2Endata), outRand2Endata));
        }

        public DllResult CallMeter_Formal_MacCheck(
            int flag, string putRand, string putDiv, string putApdu, string putData, string putMac)
        {
            return ExecuteDllCall(
                nameof(Meter_Formal_MacCheck),
                () => Meter_Formal_MacCheck(flag, putRand, putDiv, putApdu, putData, putMac));
        }

        public DllResult CallMeter_Formal_KeyUpdateV2(
            int putKeySum, string putKeystate, string putKeyid, string putRand, string putDiv,
            string putESAMNo, string putChipInfor, byte[] outData)
        {
            ValidateByteArrayParams((outData, nameof(outData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Meter_Formal_KeyUpdateV2),
                () => Meter_Formal_KeyUpdateV2(putKeySum, putKeystate, putKeyid, putRand, putDiv, putESAMNo, putChipInfor, outData),
                (nameof(outData), outData));
        }

        public DllResult CallObj_Terminal_Formal_GetTrmKeyData(
            int iKeyVersion, string strEsamNo, string strSessionKey, string cTerminalAddress, string strKeyType,
            byte[] strOutSID, byte[] strOutAttachData, byte[] strOutTrmKeyData, byte[] strOutMAC)
        {
            ValidateByteArrayParams((strOutSID, nameof(strOutSID)), (strOutAttachData, nameof(strOutAttachData)),
                                    (strOutTrmKeyData, nameof(strOutTrmKeyData)), (strOutMAC, nameof(strOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetTrmKeyData),
                () => Obj_Terminal_Formal_GetTrmKeyData(iKeyVersion, strEsamNo, strSessionKey, cTerminalAddress, strKeyType,
                                                        strOutSID, strOutAttachData, strOutTrmKeyData, strOutMAC),
                (nameof(strOutSID), strOutSID), (nameof(strOutAttachData), strOutAttachData),
                (nameof(strOutTrmKeyData), strOutTrmKeyData), (nameof(strOutMAC), strOutMAC));
        }

        public DllResult CallObj_Terminal_Formal_InitSession(
            int iKeyVersion, string cTESAMID, string cASCTR, string cFLG, string cMasterCert,
            byte[] cOutRandHost, byte[] cOutSessionInit, byte[] cOutSign)
        {
            ValidateByteArrayParams((cOutRandHost, nameof(cOutRandHost)),
                                    (cOutSessionInit, nameof(cOutSessionInit)),
                                    (cOutSign, nameof(cOutSign)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_InitSession),
                () => Obj_Terminal_Formal_InitSession(iKeyVersion, cTESAMID, cASCTR, cFLG, cMasterCert,
                                                      cOutRandHost, cOutSessionInit, cOutSign),
                (nameof(cOutRandHost), cOutRandHost), (nameof(cOutSessionInit), cOutSessionInit), (nameof(cOutSign), cOutSign));
        }

        //public async Task<DllResult> CallInitSessionAsync(
        //    int iKeyVersion, string cTESAMID, string cASCTR, string cMasterCert,
        //    byte[] cOutRandHost, byte[] cOutSessionInit, byte[] cOutSign)
        //{
        //    LogMessage.Info("异步调用 InitSession");
        //    return await Task.Run(() => CallObj_Terminal_Formal_InitSession_RH(
        //        iKeyVersion, cTESAMID, cASCTR, cMasterCert,
        //        cOutRandHost, cOutSessionInit, cOutSign));
        //}

        public DllResult CallObj_Terminal_Formal_InitSession_RH(
            int iKeyVersion, string cTESAMID, string cASCTR, string cMasterCert,
            byte[] cOutRandHost, byte[] cOutSessionInit, byte[] cOutSign)
        {
            EnsureNotDisposed();

            ValidateByteArrayParams(
                (cOutRandHost, nameof(cOutRandHost), BufferSizes.RandHost),
                (cOutSessionInit, nameof(cOutSessionInit), BufferSizes.SessionInit),
                (cOutSign, nameof(cOutSign), BufferSizes.Sign));

            lock (_lockObject)
            {
                IntPtr masterCertPtr = IntPtr.Zero;
                IntPtr randPtr = IntPtr.Zero;
                IntPtr sessPtr = IntPtr.Zero;
                IntPtr signPtr = IntPtr.Zero;

                try
                {
                    masterCertPtr = Marshal.StringToHGlobalAnsi(cMasterCert ?? string.Empty);
                    randPtr = Marshal.AllocHGlobal(cOutRandHost.Length);
                    sessPtr = Marshal.AllocHGlobal(cOutSessionInit.Length);
                    signPtr = Marshal.AllocHGlobal(cOutSign.Length);

                    ZeroMemory(randPtr, cOutRandHost.Length);
                    ZeroMemory(sessPtr, cOutSessionInit.Length);
                    ZeroMemory(signPtr, cOutSign.Length);

                    int result = Obj_Terminal_Formal_InitSession_RH(
                        iKeyVersion, cTESAMID, cASCTR,
                        masterCertPtr, randPtr, sessPtr, signPtr);

                    Marshal.Copy(randPtr, cOutRandHost, 0, cOutRandHost.Length);
                    Marshal.Copy(sessPtr, cOutSessionInit, 0, cOutSessionInit.Length);
                    Marshal.Copy(signPtr, cOutSign, 0, cOutSign.Length);

                    bool isValid = ValidateResult(nameof(Obj_Terminal_Formal_InitSession_RH), result,
                        (nameof(cOutRandHost), cOutRandHost),
                        (nameof(cOutSessionInit), cOutSessionInit),
                        (nameof(cOutSign), cOutSign));

                    return new DllResult(isValid, result);
                }
                catch (Exception ex) when (ex is AccessViolationException or BadImageFormatException or SEHException)
                {
                    LogMessage.Error($"[{nameof(Obj_Terminal_Formal_InitSession_RH)}] {ex.GetType().Name}", ex);
                    return DllResult.Fail();
                }
                finally
                {
                    if (masterCertPtr != IntPtr.Zero) Marshal.FreeHGlobal(masterCertPtr);
                    if (randPtr != IntPtr.Zero) Marshal.FreeHGlobal(randPtr);
                    if (sessPtr != IntPtr.Zero) Marshal.FreeHGlobal(sessPtr);
                    if (signPtr != IntPtr.Zero) Marshal.FreeHGlobal(signPtr);
                }
            }
        }

        public DllResult CallObj_Terminal_Formal_GetSessionData(
            int iOperateMode, string cEasmid, string cSessionKey, int cTasktype, string cTaskData,
            byte[] outSID, byte[] outAttachData, byte[] cOutData, byte[] cOutMac)
        {
            ValidateByteArrayParams((outSID, nameof(outSID)), (outAttachData, nameof(outAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMac, nameof(cOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetSessionData),
                () => Obj_Terminal_Formal_GetSessionData(iOperateMode, cEasmid, cSessionKey, cTasktype, cTaskData,
                                                         outSID, outAttachData, cOutData, cOutMac),
                (nameof(outSID), outSID), (nameof(outAttachData), outAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMac), cOutMac));
        }

        public DllResult CallObj_Terminal_Formal_GetTerminalSetData(
            int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,
            byte[] outSID, byte[] outAttachData, byte[] cOutData, byte[] cOutMac)
        {
            ValidateByteArrayParams((outSID, nameof(outSID)), (outAttachData, nameof(outAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMac, nameof(cOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetTerminalSetData),
                () => Obj_Terminal_Formal_GetTerminalSetData(iOperateMode, cEasmid, cSessionKey, cTaskData,
                                                             outSID, outAttachData, cOutData, cOutMac),
                (nameof(outSID), outSID), (nameof(outAttachData), outAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMac), cOutMac));
        }

        public DllResult CallObj_Terminal_Formal_GetGrpBrdCstData(
            int iKeyState, int iOperateMode, string cTESAMID, string cGrpKID, string cDIV, string aGSEQ,
            string cBrdCstData, byte[] cOutSID, byte[] cOutAttachData, byte[] cOutData, byte[] cOutMac)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMac, nameof(cOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetGrpBrdCstData),
                () => Obj_Terminal_Formal_GetGrpBrdCstData(iKeyState, iOperateMode, cTESAMID, cGrpKID, cDIV, aGSEQ, cBrdCstData,
                                                           cOutSID, cOutAttachData, cOutData, cOutMac),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMac), cOutMac));
        }

        public DllResult CallObj_Terminal_Formal_VerifyTerminalData(
            int ikeyState, int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,
            string cMac, byte[] cOutData)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_VerifyTerminalData),
                () => Obj_Terminal_Formal_VerifyTerminalData(ikeyState, iOperateMode, cEasmid, cSessionKey, cTaskData, cMac, cOutData),
                (nameof(cOutData), cOutData));
        }

        public DllResult CallObj_Terminal_Formal_VerifyReadData(
            int iKeyState, int iOperateMode, string cTESAMID, string cRandHost, string cReadData,
            string cMac, byte[] cOutData, byte[] cOutRSPCTR)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)), (cOutRSPCTR, nameof(cOutRSPCTR)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_VerifyReadData),
                () => Obj_Terminal_Formal_VerifyReadData(iKeyState, iOperateMode, cTESAMID, cRandHost, cReadData, cMac, cOutData, cOutRSPCTR),
                (nameof(cOutData), cOutData), (nameof(cOutRSPCTR), cOutRSPCTR));
        }

        public DllResult CallObj_Terminal_Formal_VerifyReportData(
            int iKeyState, int iOperateMode, string cTESAMID, string cRandT, string cReportData,
            string cMac, byte[] cOutData)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_VerifyReportData),
                () => Obj_Terminal_Formal_VerifyReportData(iKeyState, iOperateMode, cTESAMID, cRandT, cReportData, cMac, cOutData),
                (nameof(cOutData), cOutData));
        }

        public DllResult CallObj_Terminal_Formal_GetResponseData(
            int iKeyState, int iOperateMode, string cTESAMID, string cRandT, string cReportData,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutData, byte[] cOutMac)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMac, nameof(cOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetResponseData),
                () => Obj_Terminal_Formal_GetResponseData(iKeyState, iOperateMode, cTESAMID, cRandT, cReportData,
                                                          cOutSID, cOutAttachData, cOutData, cOutMac),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMac), cOutMac));
        }

        public DllResult CallObj_Send_Formal_DataForGetKey(
            string deviceType, string cTasktype, string cKeyState, string cEasmid, string inMeterNo, string cSessionKey,
            byte[] outSID, byte[] outAttachData, byte[] cOutData, byte[] cOutMac)
        {
            ValidateByteArrayParams((outSID, nameof(outSID)), (outAttachData, nameof(outAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMac, nameof(cOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Send_Formal_DataForGetKey),
                () => Obj_Send_Formal_DataForGetKey(deviceType, cTasktype, cKeyState, cEasmid, inMeterNo, cSessionKey,
                                                    outSID, outAttachData, cOutData, cOutMac),
                (nameof(outSID), outSID), (nameof(outAttachData), outAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMac), cOutMac));
        }

        public DllResult CallObj_Meter_Formal_GenReadData(
            string iKeyVersion, string strEsamNo, string strMeterNo, string iOperateMode, string randHost, string cReadData,
            byte[] cOutData, byte[] cOutMac)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)), (cOutMac, nameof(cOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GenReadData),
                () => Obj_Meter_Formal_GenReadData(iKeyVersion, strEsamNo, strMeterNo, iOperateMode, randHost, cReadData,
                                                   cOutData, cOutMac),
                (nameof(cOutData), cOutData), (nameof(cOutMac), cOutMac));
        }

        public DllResult CallObj_Meter_Formal_GetSessionData(
            int iOperateMode, string cESAMNO, string cSessionKey, int cTaskType, string cTaskData,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GetSessionData),
                () => Obj_Meter_Formal_GetSessionData(iOperateMode, cESAMNO, cSessionKey, cTaskType, cTaskData,
                                                      cOutSID, cOutAttachData, cOutData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallObj_Meter_Formal_VerifyMeterData(
            int iKeyState, int iOperateMode, string cESAMNO, string cSessionKey, string cTaskData,
            string cMac, byte[] cOutData)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_VerifyMeterData),
                () => Obj_Meter_Formal_VerifyMeterData(iKeyState, iOperateMode, cESAMNO, cSessionKey, cTaskData, cMac, cOutData),
                (nameof(cOutData), cOutData));
        }

        public DllResult CallObj_Terminal_Formal_GetSessionDataForMeter(
            int cOperateMode, string cTESAMID, string cSessionKey, int iTaskType, string cApdu, string cTaskData,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutTaskData, byte[] cOutTaskMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutTaskData, nameof(cOutTaskData)), (cOutTaskMAC, nameof(cOutTaskMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetSessionDataForMeter),
                () => Obj_Terminal_Formal_GetSessionDataForMeter(cOperateMode, cTESAMID, cSessionKey, iTaskType, cApdu, cTaskData,
                                                                 cOutSID, cOutAttachData, cOutTaskData, cOutTaskMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutTaskData), cOutTaskData), (nameof(cOutTaskMAC), cOutTaskMAC));
        }

        public DllResult CallObj_Terminal_Formal_InitTrmKeyData(
            int iKeyState, string cTESAMID, string cSessionKey, string cTerminalAddress, string cKeyType,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutTrmKeyData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutTrmKeyData, nameof(cOutTrmKeyData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_InitTrmKeyData),
                () => Obj_Terminal_Formal_InitTrmKeyData(iKeyState, cTESAMID, cSessionKey, cTerminalAddress, cKeyType,
                                                         cOutSID, cOutAttachData, cOutTrmKeyData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutTrmKeyData), cOutTrmKeyData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallObj_Terminal_Formal_GetCACertificateData(
            int iKeyState, string cTESAMID, string cSessionKey, string cCerType,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutCertificateData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutCertificateData, nameof(cOutCertificateData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetCACertificateData),
                () => Obj_Terminal_Formal_GetCACertificateData(iKeyState, cTESAMID, cSessionKey, cCerType,
                                                               cOutSID, cOutAttachData, cOutCertificateData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutCertificateData), cOutCertificateData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallObj_Terminal_Formal_VerifySession_RH(
            int iKeyState, string cTESAMNO, string cRandHost, string cSessionData, string cSign, string cTerminalCert,
            byte[] cOutSessionKey)
        {
            ValidateByteArrayParams((cOutSessionKey, nameof(cOutSessionKey)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_VerifySession_RH),
                () => Obj_Terminal_Formal_VerifySession_RH(iKeyState, cTESAMNO, cRandHost, cSessionData, cSign, cTerminalCert, cOutSessionKey),
                (nameof(cOutSessionKey), cOutSessionKey));
        }

        public DllResult CallObj_Terminal_Formal_GetSessionData_RH(
            string cTESAMNO, int iOperateMode, string keyID, string cSessionKey, int cTaskType, string cTaskData,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutData, byte[] cOutSign)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutData, nameof(cOutData)), (cOutSign, nameof(cOutSign)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetSessionData_RH),
                () => Obj_Terminal_Formal_GetSessionData_RH(cTESAMNO, iOperateMode, keyID, cSessionKey, cTaskType, cTaskData,
                                                             cOutSID, cOutAttachData, cOutData, cOutSign),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutData), cOutData), (nameof(cOutSign), cOutSign));
        }

        public DllResult CallObj_Terminal_Formal_VerifyData_RH(
            int iKeyState, int iOperateMode, string cTESAMNO, string cSessionKey, string cTerminalCert,
            string cTaskData, string cSign, byte[] cOutData)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_VerifyData_RH),
                () => Obj_Terminal_Formal_VerifyData_RH(iKeyState, iOperateMode, cTESAMNO, cSessionKey, cTerminalCert, cTaskData, cSign, cOutData),
                (nameof(cOutData), cOutData));
        }

        public DllResult CallObj_Terminal_Formal_GetTrmKeyData_RH(
            int iKeyState, string cTESAMNO, string cIV, string inKid, string inCert,
            byte[] ucOutTrmKeyData, byte[] cOutSign)
        {
            ValidateByteArrayParams((ucOutTrmKeyData, nameof(ucOutTrmKeyData)), (cOutSign, nameof(cOutSign)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetTrmKeyData_RH),
                () => Obj_Terminal_Formal_GetTrmKeyData_RH(iKeyState, cTESAMNO, cIV, inKid, inCert, ucOutTrmKeyData, cOutSign),
                (nameof(ucOutTrmKeyData), ucOutTrmKeyData), (nameof(cOutSign), cOutSign));
        }

        public DllResult CallObj_Terminal_Formal_GetCACertificateData_RH(
            int iKeyState, string cTESAMNO, string cSessionKey, int iCertType, string cCert,
            byte[] cOutSID, byte[] cOutAttachData, byte[] cOutCertificateData, byte[] cOutMAC)
        {
            ValidateByteArrayParams((cOutSID, nameof(cOutSID)), (cOutAttachData, nameof(cOutAttachData)),
                                    (cOutCertificateData, nameof(cOutCertificateData)), (cOutMAC, nameof(cOutMAC)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Terminal_Formal_GetCACertificateData_RH),
                () => Obj_Terminal_Formal_GetCACertificateData_RH(iKeyState, cTESAMNO, cSessionKey, iCertType, cCert,
                                                                   cOutSID, cOutAttachData, cOutCertificateData, cOutMAC),
                (nameof(cOutSID), cOutSID), (nameof(cOutAttachData), cOutAttachData),
                (nameof(cOutCertificateData), cOutCertificateData), (nameof(cOutMAC), cOutMAC));
        }

        public DllResult CallRH_InternalSign(int sConnect, string cTESAMID, int iKeyIndex, string cCert, string inData, byte[] outSign)
        {
            ValidateByteArrayParams((outSign, nameof(outSign)));

            return ExecuteDllCallWithByteOutputs(
                nameof(RH_InternalSign),
                () => RH_InternalSign(sConnect, cTESAMID, iKeyIndex, cCert, inData, outSign),
                (nameof(outSign), outSign));
        }

        public DllResult CallRH_VerifySig(int sConnect, string cTESAMID, string cCert, string inData, string inSign)
        {
            return ExecuteDllCall(nameof(RH_VerifySig),
                () => RH_VerifySig(sConnect, cTESAMID, cCert, inData, inSign));
        }

        public DllResult CallRH_EncData(
            int sConnect, string cTESAMID, string cKeyVer, string cKeyIndex, string cDivData, string cIvData,
            string cInData, byte[] cOutEncData, byte[] cOutMac)
        {
            ValidateByteArrayParams((cOutEncData, nameof(cOutEncData)), (cOutMac, nameof(cOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(RH_EncData),
                () => RH_EncData(sConnect, cTESAMID, cKeyVer, cKeyIndex, cDivData, cIvData, cInData, cOutEncData, cOutMac),
                (nameof(cOutEncData), cOutEncData), (nameof(cOutMac), cOutMac));
        }

        public DllResult CallRH_DisEncData(
            int sConnect, string cTESAMID, string cKeyVer, string cKeyIndex, string cDivData, string cIvData,
            string cInEncData, string cInMac, byte[] cOutData)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(RH_DisEncData),
                () => RH_DisEncData(sConnect, cTESAMID, cKeyVer, cKeyIndex, cDivData, cIvData, cInEncData, cInMac, cOutData),
                (nameof(cOutData), cOutData));
        }

        public DllResult CallRH_Terminal_Formal_GetTrmKeyData(
            int iKeyState, string cTESAMID, string cIV, string inKid, string inCert,
            byte[] ucOutTrmKeyData, byte[] cOutSign)
        {
            ValidateByteArrayParams((ucOutTrmKeyData, nameof(ucOutTrmKeyData)), (cOutSign, nameof(cOutSign)));

            return ExecuteDllCallWithByteOutputs(
                nameof(RH_Terminal_Formal_GetTrmKeyData),
                () => RH_Terminal_Formal_GetTrmKeyData(iKeyState, cTESAMID, cIV, inKid, inCert, ucOutTrmKeyData, cOutSign),
                (nameof(ucOutTrmKeyData), ucOutTrmKeyData), (nameof(cOutSign), cOutSign));
        }

        public DllResult CallTerminal_Formal_GetR1(byte[] outR1)
        {
            ValidateByteArrayParams((outR1, nameof(outR1)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_GetR1),
                () => Terminal_Formal_GetR1(outR1),
                (nameof(outR1), outR1));
        }

        public DllResult CallTerminal_Formal_SessionInitRec(
            string putState, string putTESAMNo, string putVersionNum, string putSessionID, string putR1,
            byte[] outMasterCertificate, byte[] outEncR1, byte[] outMac, byte[] outSign1)
        {
            ValidateByteArrayParams((outMasterCertificate, nameof(outMasterCertificate)),
                                    (outEncR1, nameof(outEncR1)),
                                    (outMac, nameof(outMac)),
                                    (outSign1, nameof(outSign1)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_SessionInitRec),
                () => Terminal_Formal_SessionInitRec(putState, putTESAMNo, putVersionNum, putSessionID, putR1,
                                                     outMasterCertificate, outEncR1, outMac, outSign1),
                (nameof(outMasterCertificate), outMasterCertificate),
                (nameof(outEncR1), outEncR1),
                (nameof(outMac), outMac),
                (nameof(outSign1), outSign1));
        }

        public DllResult CallTerminal_Formal_SessionKeyConsult(
            string putState, string putTESAMNo, string putVersionNum, string putSessionID,
            string putCRLCertificateNo, string putMasterCertificateNo, string putTerminalCertificate,
            string putEncR2, string putSign2, string putR1,
            byte[] outEncM1, byte[] outSign3, byte[] outMac2, byte[] outSign4)
        {
            ValidateByteArrayParams((outEncM1, nameof(outEncM1)),
                                    (outSign3, nameof(outSign3)),
                                    (outMac2, nameof(outMac2)),
                                    (outSign4, nameof(outSign4)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_SessionKeyConsult),
                () => Terminal_Formal_SessionKeyConsult(putState, putTESAMNo, putVersionNum, putSessionID,
                                                        putCRLCertificateNo, putMasterCertificateNo, putTerminalCertificate,
                                                        putEncR2, putSign2, putR1,
                                                        outEncM1, outSign3, outMac2, outSign4),
                (nameof(outEncM1), outEncM1),
                (nameof(outSign3), outSign3),
                (nameof(outMac2), outMac2),
                (nameof(outSign4), outSign4));
        }

        public DllResult CallTerminal_Formal_SessionConsultVerify(string putR3, string putMac3)
        {
            return ExecuteDllCall(nameof(Terminal_Formal_SessionConsultVerify),
                () => Terminal_Formal_SessionConsultVerify(putR3, putMac3));
        }

        public DllResult CallTerminal_Formal_SessionRecoveryVerify(
            string putState, string putTESAMNo, string putVersionNum, string putSessionID,
            string putEncR2, string putR3, string putMac)
        {
            return ExecuteDllCall(nameof(Terminal_Formal_SessionRecoveryVerify),
                () => Terminal_Formal_SessionRecoveryVerify(putState, putTESAMNo, putVersionNum, putSessionID, putEncR2, putR3, putMac));
        }

        public DllResult CallTerminal_Formal_MACVerify(
            string putState, string putTESAMNo, string putFnType, string putData, byte[] outMac)
        {
            ValidateByteArrayParams((outMac, nameof(outMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_MACVerify),
                () => Terminal_Formal_MACVerify(putState, putTESAMNo, putFnType, putData, outMac),
                (nameof(outMac), outMac));
        }

        public DllResult CallTerminal_Formal_ExternalAuth(
            string putState, string putTESAMNo, string putR4, string putEncR4, string putR5, byte[] outEncR5)
        {
            ValidateByteArrayParams((outEncR5, nameof(outEncR5)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_ExternalAuth),
                () => Terminal_Formal_ExternalAuth(putState, putTESAMNo, putR4, putEncR4, putR5, outEncR5),
                (nameof(outEncR5), outEncR5));
        }

        public DllResult CallTerminal_Formal_CertificateStateChange(
            string putState, string putTESAMNo, string putCertificateState, string putR6,
            byte[] outEncR6, byte[] outMac)
        {
            ValidateByteArrayParams((outEncR6, nameof(outEncR6)), (outMac, nameof(outMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_CertificateStateChange),
                () => Terminal_Formal_CertificateStateChange(putState, putTESAMNo, putCertificateState, putR6, outEncR6, outMac),
                (nameof(outEncR6), outEncR6), (nameof(outMac), outMac));
        }

        public DllResult CallTerminal_Formal_SetOfflineCounter(
            string putState, string putTESAMNo, string putCounter, byte[] outEncCounter)
        {
            ValidateByteArrayParams((outEncCounter, nameof(outEncCounter)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_SetOfflineCounter),
                () => Terminal_Formal_SetOfflineCounter(putState, putTESAMNo, putCounter, outEncCounter),
                (nameof(outEncCounter), outEncCounter));
        }

        public DllResult CallTerminal_Formal_ChangeDataAuthorize(byte[] outData)
        {
            ValidateByteArrayParams((outData, nameof(outData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_ChangeDataAuthorize),
                () => Terminal_Formal_ChangeDataAuthorize(outData),
                (nameof(outData), outData));
        }

        public DllResult CallTerminal_Formal_GetCipherMeterKey(
            string putMeterState, string putMeterNo, int putTaskType, byte[] outMeterEncKey)
        {
            ValidateByteArrayParams((outMeterEncKey, nameof(outMeterEncKey)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_GetCipherMeterKey),
                () => Terminal_Formal_GetCipherMeterKey(putMeterState, putMeterNo, putTaskType, outMeterEncKey),
                (nameof(outMeterEncKey), outMeterEncKey));
        }

        public DllResult CallTerminal_Formal_EncTaskData(int putInDataType, string putTaskData, byte[] outTaskData)
        {
            ValidateByteArrayParams((outTaskData, nameof(outTaskData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_EncTaskData),
                () => Terminal_Formal_EncTaskData(putInDataType, putTaskData, outTaskData),
                (nameof(outTaskData), outTaskData));
        }

        public DllResult CallTerminal_Formal_GroupBroadcast(
            string putState, string putTESAMNo, string putFnType, int putOutDataType,
            string putGroupAdrass, string putMtime, string putBroadcastData, byte[] outMac)
        {
            ValidateByteArrayParams((outMac, nameof(outMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_GroupBroadcast),
                () => Terminal_Formal_GroupBroadcast(putState, putTESAMNo, putFnType, putOutDataType, putGroupAdrass, putMtime, putBroadcastData, outMac),
                (nameof(outMac), outMac));
        }

        public DllResult CallTerminal_Formal_SystemBroadcast(
            string putState, string putTESAMNo, string putFnType, int putOutDataType,
            string putGroupAdrass, string putMtime, string putBroadcastData, byte[] outMac)
        {
            ValidateByteArrayParams((outMac, nameof(outMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_SystemBroadcast),
                () => Terminal_Formal_SystemBroadcast(putState, putTESAMNo, putFnType, putOutDataType, putGroupAdrass, putMtime, putBroadcastData, outMac),
                (nameof(outMac), outMac));
        }

        public DllResult CallTerminal_Formal_SymmetricKeyUpdate(
            string putState, string putTESAMNo, byte[] outKeyNum, byte[] outEncKeyData)
        {
            ValidateByteArrayParams((outKeyNum, nameof(outKeyNum)), (outEncKeyData, nameof(outEncKeyData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_SymmetricKeyUpdate),
                () => Terminal_Formal_SymmetricKeyUpdate(putState, putTESAMNo, outKeyNum, outEncKeyData),
                (nameof(outKeyNum), outKeyNum), (nameof(outEncKeyData), outEncKeyData));
        }

        public DllResult CallTerminal_Formal_CACertificateUpdate(
            string putCertificateState, string putCertificateType, byte[] outEncCertificateData)
        {
            ValidateByteArrayParams((outEncCertificateData, nameof(outEncCertificateData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_CACertificateUpdate),
                () => Terminal_Formal_CACertificateUpdate(putCertificateState, putCertificateType, outEncCertificateData),
                (nameof(outEncCertificateData), outEncCertificateData));
        }

        public DllResult CallTerminal_Formal_SymmetricKeyUpdateCT(
            string putState, string putTESAMNo, byte[] outKeyNum, byte[] outEncKeyData)
        {
            ValidateByteArrayParams((outKeyNum, nameof(outKeyNum)), (outEncKeyData, nameof(outEncKeyData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Terminal_Formal_SymmetricKeyUpdateCT),
                () => Terminal_Formal_SymmetricKeyUpdateCT(putState, putTESAMNo, outKeyNum, outEncKeyData),
                (nameof(outKeyNum), outKeyNum), (nameof(outEncKeyData), outEncKeyData));
        }

        public DllResult CallCT_Terminal_Formal_GetCTESAMKey(
            int inFlag, string inKeyid, string inCTTESAMNo, string inCTESAMNo, string inRand,
            byte[] outKey, byte[] outMac)
        {
            ValidateByteArrayParams((outKey, nameof(outKey)), (outMac, nameof(outMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(CT_Terminal_Formal_GetCTESAMKey),
                () => CT_Terminal_Formal_GetCTESAMKey(inFlag, inKeyid, inCTTESAMNo, inCTESAMNo, inRand, outKey, outMac),
                (nameof(outKey), outKey), (nameof(outMac), outMac));
        }

        public DllResult CallCT_Terminal_Formal_CalCTESAMMac(
            int inFlag, string inCTESAMNo, string inRand, string inData, byte[] outMac)
        {
            ValidateByteArrayParams((outMac, nameof(outMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(CT_Terminal_Formal_CalCTESAMMac),
                () => CT_Terminal_Formal_CalCTESAMMac(inFlag, inCTESAMNo, inRand, inData, outMac),
                (nameof(outMac), outMac));
        }

        public DllResult CallCT_Terminal_Formal_CalCTTESAMMac(
            string putState, string inCTTESAMNo, string inRand, string inData, byte[] outMac)
        {
            ValidateByteArrayParams((outMac, nameof(outMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(CT_Terminal_Formal_CalCTTESAMMac),
                () => CT_Terminal_Formal_CalCTTESAMMac(putState, inCTTESAMNo, inRand, inData, outMac),
                (nameof(outMac), outMac));
        }

        public DllResult CallCT_Terminal_Formal_CalVerifyCTESAMMac(
            int inFlag, string inCTESAMNo, string inRand, string inData, string inMac)
        {
            return ExecuteDllCall(nameof(CT_Terminal_Formal_CalVerifyCTESAMMac),
                () => CT_Terminal_Formal_CalVerifyCTESAMMac(inFlag, inCTESAMNo, inRand, inData, inMac));
        }

        public DllResult CallCT_Terminal_Formal_CalVerifyCTTESAMMac(
            string putState, string inCTTESAMNo, string inRand, string inData, string inMac)
        {
            return ExecuteDllCall(nameof(CT_Terminal_Formal_CalVerifyCTTESAMMac),
                () => CT_Terminal_Formal_CalVerifyCTTESAMMac(putState, inCTTESAMNo, inRand, inData, inMac));
        }

        public DllResult CallRDID_Formal_RFIDChangeKey(string putKeyState, string putDiv, byte[] outKey)
        {
            ValidateByteArrayParams((outKey, nameof(outKey)));

            return ExecuteDllCallWithByteOutputs(
                nameof(RDID_Formal_RFIDChangeKey),
                () => RDID_Formal_RFIDChangeKey(putKeyState, putDiv, outKey),
                (nameof(outKey), outKey));
        }

        public DllResult CallRDID_Formal_RFIDEncrptData(
            string putKeyState, string putDiv, string putData, string putOPInfor,
            byte[] outEncData, byte[] outMAC2)
        {
            ValidateByteArrayParams((outEncData, nameof(outEncData)), (outMAC2, nameof(outMAC2)));

            return ExecuteDllCallWithByteOutputs(
                nameof(RDID_Formal_RFIDEncrptData),
                () => RDID_Formal_RFIDEncrptData(putKeyState, putDiv, putData, putOPInfor, outEncData, outMAC2),
                (nameof(outEncData), outEncData), (nameof(outMAC2), outMAC2));
        }

        public DllResult CallRDID_Formal_RFIDDisEncrptData(
            string putKeyState, string putDiv, string putEncData, string putOPInfor, string putMAC, byte[] outData)
        {
            ValidateByteArrayParams((outData, nameof(outData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(RDID_Formal_RFIDDisEncrptData),
                () => RDID_Formal_RFIDDisEncrptData(putKeyState, putDiv, putEncData, putOPInfor, putMAC, outData),
                (nameof(outData), outData));
        }

        public DllResult CallRDID_Formal_RFIDCheckData(
            string inKeyState, int inDataNo, string putDiv, string putData, string putMac)
        {
            return ExecuteDllCall(nameof(RDID_Formal_RFIDCheckData),
                () => RDID_Formal_RFIDCheckData(inKeyState, inDataNo, putDiv, putData, putMac));
        }
        public DllResult CallObj_Meter_Formal_InitSession(
          int iKeyState, string cDiv, string _strASCTR, string _strFLG,
          byte[] _strOutRandHost, byte[] _strOutSessionInit, byte[] _strOutMac)
        {
            ValidateByteArrayParams((_strOutRandHost, nameof(_strOutRandHost)), (_strOutSessionInit, nameof(_strOutSessionInit)),
                                    (_strOutMac, nameof(_strOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_InitSession),
                () => Obj_Meter_Formal_InitSession(iKeyState, cDiv, _strASCTR, _strFLG,
                                                                 _strOutRandHost, _strOutSessionInit, _strOutMac),
                (nameof(_strOutRandHost), _strOutRandHost), (nameof(_strOutSessionInit), _strOutSessionInit),
                (nameof(_strOutMac), _strOutMac));
        }

        public DllResult CallObj_Meter_Formal_VerifySession(
            int iKeyState, string cDiv, string cRandHost, string cSessionData, string cSign, byte[] cOutSessionKey)
        {
            ValidateByteArrayParams((cOutSessionKey, nameof(cOutSessionKey)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_VerifySession),
                () => Obj_Meter_Formal_VerifySession(iKeyState, cDiv, cRandHost, cSessionData, cSign, cOutSessionKey),
                (nameof(cOutSessionKey), cOutSessionKey));
        }

        public DllResult CallObj_Meter_Formal_VerifyReadData(
            int iKeyState, int iOperateMode, string cMeterNo, string cRandHost, string cReadData, string cMac, byte[] cOutData)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_VerifyReadData),
                () => Obj_Meter_Formal_VerifyReadData(iKeyState, iOperateMode, cMeterNo, cRandHost, cReadData, cMac, cOutData),
                (nameof(cOutData), cOutData));
        }

        public DllResult CallObj_Meter_Formal_VerifyReportData(
            int iKeyState, int iOperateMode, string cMeterNo, string cRandT, string cReportData,
            string cMac, byte[] cOutData, byte[] cOutRSPCTR)
        {
            ValidateByteArrayParams((cOutData, nameof(cOutData)), (cOutRSPCTR, nameof(cOutRSPCTR)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_VerifyReportData),
                () => Obj_Meter_Formal_VerifyReportData(iKeyState, iOperateMode, cMeterNo, cRandT, cReportData, cMac, cOutData, cOutRSPCTR),
                (nameof(cOutData), cOutData), (nameof(cOutRSPCTR), cOutRSPCTR));
        }

        public DllResult CallObj_Meter_Formal_GetResponseData(
            int iKeyState, int iOperateMode, string cMeterNo, string randHost, string cReportData,
            byte[] outSID, byte[] outAttachData, byte[] cOutData, byte[] ucOutMac)
        {
            ValidateByteArrayParams((outSID, nameof(outSID)), (outAttachData, nameof(outAttachData)),
                                    (cOutData, nameof(cOutData)), (ucOutMac, nameof(ucOutMac)));

            return ExecuteDllCallWithByteOutputs(
                nameof(Obj_Meter_Formal_GetResponseData),
                () => Obj_Meter_Formal_GetResponseData(iKeyState, iOperateMode, cMeterNo, randHost, cReportData,
                                                       outSID, outAttachData, cOutData, ucOutMac),
                (nameof(outSID), outSID), (nameof(outAttachData), outAttachData),
                (nameof(cOutData), cOutData), (nameof(ucOutMac), ucOutMac));
        }

        #endregion

        #region 验证方法

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(WinSocketServer));
        }

        private static void ValidateByteArrayParams(params (byte[] Data, string Name, int MinLength)[] arrays)
        {
            foreach (var (data, name, minLength) in arrays)
            {
                if (data == null)
                    throw new ArgumentNullException(name);
                if (data.Length == 0)
                    throw new ArgumentException($"{name} 长度不能为0", name);
                if (minLength > 0 && data.Length < minLength)
                    throw new ArgumentException($"{name} 长度至少为 {minLength} 字节", name);
            }
        }

        private static void ValidateByteArrayParams(params (byte[] Data, string Name)[] arrays)
        {
            foreach (var (data, name) in arrays)
            {
                if (data == null)
                    throw new ArgumentNullException(name);
                if (data.Length == 0)
                    throw new ArgumentException($"{name} 长度不能为0", name);
            }
        }

        private static bool ValidateResult(string methodName, int resultCode)
        {
            if (resultCode == SuccessCode)
            {
                LogMessage.Debug($"[{methodName}] 调用成功");
                return true;
            }

            LogMessage.Debug($"[{methodName}] 调用失败，错误代码: {resultCode}");
            return false;
        }

        private static bool ValidateResult(string methodName, int resultCode, params (string Name, byte[] Data)[] outputs)
        {
            if (resultCode == SuccessCode)
            {
                LogMessage.Debug($"[{methodName}] 调用成功");
                foreach (var (name, data) in outputs)
                {
                    LogMessage.Debug($"{name}: {System.Text.Encoding.Default.GetString(data)?.ToString().TrimEnd(' ')}");
                }
                return true;
            }

            LogMessage.Debug($"[{methodName}] 调用失败，错误代码: {resultCode}");
            return false;
        }

        private static string BytesToHexString(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return string.Empty;

            var sb = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.AppendFormat("{0:X2}", b);
            return sb.ToString();
        }

        private static void ZeroMemory(IntPtr ptr, int length)
        {
            for (int i = 0; i < length; i++)
                Marshal.WriteByte(ptr, i, 0);
        }

        #endregion


        /// <summary>
        /// 加密机所有的接口方法名称。
        /// </summary>
        /// <returns></returns>
        public List<string> WinSocketSericeImp()
        {
            return _serviceCatalog.GetServiceNames().ToList();
        }
        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
