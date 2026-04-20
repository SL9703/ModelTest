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
        private static extern int Meter_Formal_DataClear1(
            [In, Out] int flag, string putRand, string putDiv, string putData, [Out] byte[] outData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Meter_Formal_DataClear2(
            [In, Out] int flag, string putRand, string putDiv, string putData, [Out] byte[] outData);

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
        private static extern int Obj_Terminal_Formal_VerifyTerminalData(
            [In, Out] int ikeyState, int iOperateMode, string cEasmid, string cSessionKey, string cTaskData,
            string cMac, byte[] cOutData);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Send_Formal_DataForGetKey(
            [In, Out] string inDeviceType, string cTasktype, string cKeyState, string cTESAMID, string inMeterNo, string cSessionKey,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, byte[] cOutTaskData, byte[] cOutTaskMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Meter_Formal_GenReadData(
            [In, Out] string iKeyVersion, string strEsamNo, string strMeterNo, string iOperateMode, string randHost, string cReadData,
            [Out] byte[] strOutData, [Out] byte[] strOutMAC);

        [DllImport("WinSocketServer.dll")]
        private static extern int Obj_Terminal_Formal_GetSessionDataForMeter(
            [In, Out] int cOperateMode, string cTESAMID, string cSessionKey, int iTaskType, string cApdu, string cTaskData,
            [Out] byte[] cOutSID, [Out] byte[] cOutAttachData, [Out] byte[] cOutTaskData, [Out] byte[] cOutTaskMAC);

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
                    LogMessage.Debug($"{name}: {data?.ToString().TrimEnd().Trim('0')}");
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
"Obj_Terminal_Formal_InitSession_RH",
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
