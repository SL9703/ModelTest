using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    /// <summary>
    /// 新跃源接口调用
    /// </summary>
    public class XYCtr : IDisposable
    {
        private readonly object lockObject = new object();//lock对象
        private static int AnyUIOutput_Result;//控源输出接口返回值
        private static int OpenComm_Result;//打开源串口接口返回值
        private static int ShutPowerSource_Result;//降源接口返回值
        private static int ReadStandMeter_data;//读取标准表接口返回值
        private static int ReadTestData_Result;//读取装置信息接口返回值
        private static int SetBlueToothChannel_Result;//设置蓝牙模式和通道接口返回值
        private static int SendCommand_Result;//初始化电表参数接口返回值
        private static int ReadStandConst_Result;//读取常数接口返回值
        private static int Clock_Start_Result;//测试时钟误差接口返回值
        // public static int iResult; //降源接口返回值
        /// <summary>
        /// 降源接口 0-----电压、电流同时停止输出1-----电压停止输出 2-----电流停止输出
        /// </summary>
        /// <param name="ShutPowerSource"></param>
        /// <returns></returns>

        [DllImport("xyctr.dll")]
        private static extern int ShutPowerSource([In] int ShutPowerSource);
        public (bool Success, int Result) CallShutPowerSource(int shutPowerSource)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    ShutPowerSource_Result = ShutPowerSource(shutPowerSource);
                    if (ShutPowerSource_Result == 1)
                    {
                        LogMessage.Debug("降源正常" + ShutPowerSource_Result);
                        return (true, ShutPowerSource_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用降源接口异常" + ShutPowerSource_Result);
                        return (false, ShutPowerSource_Result);
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
        /// 打开源串口
        /// </summary>
        /// <param name="Port"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int OpenComm(int Port);
        public (bool Success, int Result) CallOpenComm(int Port)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    OpenComm_Result = OpenComm(Port);
                    if (OpenComm_Result == 1)
                    {
                        LogMessage.Debug("打开源串口正常" + OpenComm_Result);
                        return (true, OpenComm_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用打开源串口接口异常" + OpenComm_Result);
                        return (false, OpenComm_Result);
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
        /// 关闭源串口
        /// </summary>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int CloseComm();
        public static void CallCloseComm()
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    CloseComm();
                    LogMessage.Debug("调用关闭源串口接口正常");
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        #region 读取标准表接口
        /// <summary>
        /// 读取标准表数据接口
        /// </summary>
        /// <param name="StandModel"></param>
        /// <param name="StandValue"></param>
        /// <returns></returns>


        [DllImport("xyctr.dll")]
        private static extern int ReadStandValue([In, Out] string StandModel, [Out] byte[] StandValue);
        public (bool Success, int Result) CallReadStandValue(string standModel, byte[] sStandValue)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    ReadStandMeter_data = ReadStandValue(standModel, sStandValue);
                    if (IsValidResult(ReadStandMeter_data, sStandValue))
                    {
                        LogMessage.Debug($"DLL返回有效内容:{ReadStandMeter_data.ToString()}");
                        return (true, ReadStandMeter_data);

                    }
                    else
                    {
                        LogMessage.Debug($"DLL返回无效内容:{ReadStandMeter_data.ToString()}");
                        return (false, ReadStandMeter_data);
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

        private bool IsValidResult(int readStandMeter_data, byte[] sStandValue)
        {
            if (readStandMeter_data == 1)
            {
                LogMessage.Debug("标准表数据返回成功");
                LogMessage.Debug("新跃源标准表数据：" + System.Text.Encoding.Default.GetString(sStandValue));
                return true;
            }
            else
            {
                LogMessage.Debug("标准表数据返回失败，错误代码：" + readStandMeter_data);
                return false;
            }
        }
        #endregion

        /// <summary>
        /// 控源输出接口
        /// </summary>
        /// <param name="StrUICommand">控源参数</param>
        /// <param name="iPulse">脉冲数</param>
        /// <returns></returns>

        [DllImport("xyctr.dll")]
        private static extern int AnyUIOutput(string StrUICommand, int iPulse);
        public (bool Success, int Result) CallAnyUIOutput(string StrUICommand, int iPulse)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    AnyUIOutput_Result = AnyUIOutput(StrUICommand, iPulse);
                    if (AnyUIOutput_Result == 1)
                    {
                        LogMessage.Debug("控源输出接口正常" + AnyUIOutput_Result);
                        return (true, AnyUIOutput_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用控源输出接口异常" + AnyUIOutput_Result);
                        return (false, AnyUIOutput_Result);
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
        /// 读取装置信息
        /// </summary>
        /// <param name="ReadType"></param>
        /// <param name="iPosition"></param>
        /// <param name="sResultData"></param>
        /// <returns></returns>

        [DllImport("xyctr.dll")]
        private static extern int ReadTestData([In, Out] int ReadType, int iPosition, [Out] byte[] sResultData);
        public (bool Success, int Result) CallReadTestData(int readtype, int iposition, byte[] sresultdata)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    ReadTestData_Result = ReadTestData(readtype, iposition, sresultdata);
                    if (ReadTestData_Result == 1)
                    {
                        LogMessage.Debug("读取装置信息成功" + ReadTestData_Result);
                        return (true, ReadTestData_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用装置信息接口异常" + ReadTestData_Result);
                        return (false, ReadTestData_Result);
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
        /// 读取常数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        [DllImport("xyctr.dll")]
        private static extern int ReadStandConst([Out] byte[] constanst);
        public (bool Success, int Result) CallReadStandConst(byte[] constanst)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    ReadStandConst_Result = ReadStandConst(constanst);
                    if (ReadStandConst_Result == 1)
                    {
                        LogMessage.Debug("读取常数成功" + ReadStandConst_Result);
                        return (true, ReadStandConst_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用读取常数接口异常" + ReadStandConst_Result);
                        return (false, ReadStandConst_Result);
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
        /// 初始化电表参数
        /// </summary>
        /// <param name="Cmd"></param>
        /// <param name="AdjTags"></param>
        /// <returns></returns>

        [DllImport("xyctr.dll")]
        private static extern int SendCommand(string Cmd, bool AdjTags);
        public (bool Success, int Result) CallSendCommand(string cmd, bool AdjTags)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    SendCommand_Result = SendCommand(cmd, AdjTags);
                    if (SendCommand_Result == 1)
                    {
                        LogMessage.Debug("SendCommand接口正常" + SendCommand_Result);
                        return (true, SendCommand_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用SendCommand接口异常" + SendCommand_Result);
                        return (false, SendCommand_Result);
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
        /// 设置蓝牙模式和通道
        /// </summary>
        /// <param name="IchanngelNo"></param>
        /// <returns></returns>

        [DllImport("xyctr.dll")]
        private static extern int Set_BlueTooth_Channel(int IchanngelNo);
        public (bool Success, int Result) CallSet_BlueTooth_Channel(int IchanngelNo)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    SetBlueToothChannel_Result = Set_BlueTooth_Channel(IchanngelNo);
                    if (SetBlueToothChannel_Result == 1)
                    {
                        LogMessage.Debug("设置Set_BlueTooth_Channel接口正常" + SetBlueToothChannel_Result);
                        return (true, SetBlueToothChannel_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Set_BlueTooth_Channel接口异常" + SetBlueToothChannel_Result);
                        return (false, SetBlueToothChannel_Result);
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
        /// 测试时钟误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        [DllImport("xyctr.dll")]
        private static extern int Clock_Start(int iPulse);
        public (bool Success, int Result) Call_Clock_Start(int iPulse)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    Clock_Start_Result = Clock_Start(iPulse);
                    if (Clock_Start_Result == 1)
                    {
                        LogMessage.Debug("调用设置Clock_Start接口正常" + Clock_Start_Result);
                        return (true, Clock_Start_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Clock_Start接口异常" + Clock_Start_Result);
                        return (false, Clock_Start_Result);
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
        /// 读取误差
        /// </summary>
        /// <param name="iMeterNo"></param>
        /// <param name="MeterError"></param>
        /// <returns></returns>
        private static int Read_Test_Result;
        [DllImport("xyctr.dll")]
        private static extern int Read_Test([In, Out] int iMeterNo, [Out] byte[] MeterError);
        public (bool Success, int Result) Call_Read_TestError(int iMeterNo, byte[] MeterError)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    Read_Test_Result = Read_Test(iMeterNo, MeterError);
                    if (Read_Test_Result == 1)
                    {
                        // int.TryParse(System.Text.Encoding.Default.GetString(MeterError), out double resut);

                        LogMessage.Debug($"读取{iMeterNo}表位误差数据成功：" + MeterError);
                        return (true, Read_Test_Result);
                    }
                    else
                    {
                        LogMessage.Debug($"读取{iMeterNo}误差数据失败，错误代码：" + Read_Test_Result);
                        return (false, Read_Test_Result);
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
        /// 开始误差试验
        /// </summary>
        /// <param name="MeterConstant"></param>
        /// <param name="iMeterCount"></param>
        /// <param name="iPulse"></param>
        /// <returns></returns>
        private static int Error_Start_Result;
        [DllImport("xyctr.dll")]
        private static extern int Error_Start(string MeterConstant, int iMeterCount, int iPulse);
        public (bool Success, int Result) Call_Error_Start(string meterConstant, int iMeterCount, int iPulse)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    Error_Start_Result = Error_Start(meterConstant, iMeterCount, iPulse);
                    if (Error_Start_Result == 1)
                    {
                        LogMessage.Debug("调用设置Error_Start(误差试验)接口正常" + Error_Start_Result);
                        return (true, Error_Start_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Error_Start(误差试验)接口异常" + Error_Start_Result);
                        return (false, Error_Start_Result);
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
        /// 停止测试
        /// </summary>
        /// <returns></returns>
        private static int Stop_Test_Result;
        [DllImport("xyctr.dll")]
        private static extern int Stop_Test();

        public (bool Success, int Result) Call_Stop_Test()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    Stop_Test_Result = Stop_Test();
                    if (Stop_Test_Result == 1)
                    {
                        LogMessage.Debug("调用设置Stop_Test(停止误差)接口正常" + Stop_Test_Result);
                        return (true, Stop_Test_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Stop_Test(停止误差)接口异常" + Stop_Test_Result);
                        return (false, Stop_Test_Result);
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
        /// 清除误差
        /// </summary>
        /// <returns></returns>
        private static int Error_Clear_Result;
        [DllImport("xyctr.dll")]
        private static extern int Error_Clear();
        public (bool Success, int Result) Call_Error_Clear()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    Error_Clear_Result = Error_Clear();
                    if (Error_Clear_Result == 1)
                    {
                        LogMessage.Debug("调用设置Error_Clear(清除误差)接口正常" + Error_Clear_Result);
                        return (true, Error_Clear_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Error_Clear(清除误差)接口异常" + Error_Clear_Result);
                        return (false, Error_Clear_Result);
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
        /// 读取脉冲数
        /// </summary>
        /// <param name="iMeterNo"></param>
        /// <param name="MeterError"></param>
        /// <returns></returns>
        private static int Read_Pulse_Result;
        [DllImport("xyctr.dll")]
        private static extern int Read_Pulse([In, Out] int iMeterNo, [Out] byte[] MeterError);
        public (bool Success, int Result) Call_Read_Pulse(int iMeterNo, byte[] MeterError)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    Read_Pulse_Result = Read_Pulse(iMeterNo, MeterError);
                    if (Read_Pulse_Result == 1)
                    {
                        LogMessage.Debug("调用设置Read_Pulse(读取脉冲数)接口正常" + Read_Pulse_Result);
                        LogMessage.Debug($"读取表位{iMeterNo}脉冲数数为：{MeterError}");
                        return (true, Read_Pulse_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Read_Pulse(读取脉冲数)接口异常" + Read_Pulse_Result);
                        return (false, Read_Pulse_Result);
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
        /// 读取版本号
        /// </summary>
        /// <param name="StrVer"></param>
        /// <returns></returns>
        private static int Read_Version_Result;
        [DllImport("xyctr.dll")]
        private static extern int FunctionReadVersion([Out] byte[] StrVer);
        public (bool Success, int Result) CallFunctionReadVersion(byte[] StrVer)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    Read_Version_Result = FunctionReadVersion(StrVer);
                    if (Read_Version_Result == 1)
                    {
                        LogMessage.Debug("调用设置FunctionReadVersion(读取版本号)接口正常" + Read_Version_Result);
                        LogMessage.Debug("版本号为：" + System.Text.Encoding.Default.GetString(StrVer));
                        return (true, Read_Version_Result);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置FunctionReadVersion(读取版本号)接口异常" + Read_Version_Result);
                        return (false, Read_Version_Result);
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
        /// 设置电压电流量程
        /// </summary>
        /// <param name="iUI"></param>
        /// <param name="iValue"></param>
        /// <returns></returns>
        private static int SetUIRange_Result;
        [DllImport("xyctr.dll")]
        private static extern int SetUIRange(int iUI, int iValue);
        public (bool Success, int Result) CallSetUIRange(int iui, int ivalue)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    SetUIRange_Result = SetUIRange(iui, ivalue);
                    if (SetUIRange_Result > 0)
                    {
                        LogMessage.Debug("SetUIRange设置电压和电流量程接口正常" + SetUIRange_Result);
                        return (true, SetUIRange_Result);
                    }
                    else if (true)
                    {
                        LogMessage.Debug("SetUIRange设置电压和电流量程接口异常" + SetUIRange_Result);
                        return (false, SetUIRange_Result);
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
        /// rangeoutoutui
        /// </summary>
        /// <param name="StrUICommand"></param>
        /// <returns></returns>
        private static int RangeOutputUI_Result;
        [DllImport("xyctr.dll")]
        private static extern int RangeOutputUI(string StrUICommand);
        public (bool Success, int Result) CallRangeOutputUI(string StrUICommand)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(XYCtr));
            }
            lock (lockObject)
            {
                try
                {
                    RangeOutputUI_Result = RangeOutputUI(StrUICommand);
                    if (RangeOutputUI_Result == 1)
                    {
                        LogMessage.Debug("RangeOutputUI设置电压和电流量程接口正常" + RangeOutputUI_Result);
                        return (true, RangeOutputUI_Result);
                    }
                    else
                    {
                        LogMessage.Debug("RangeOutputUI设置电压和电流量程接口异常" + RangeOutputUI_Result);
                        return (false, RangeOutputUI_Result);
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
        /// 电表参数转换
        /// </summary>
        /// <param name="MeterPara"></param>
        /// <returns></returns>
        public static string Init_meterConnection(string meterpara)
        {
            /*电表常数
            //单相有功
            //三相四线有功
            //三相三线有功
            //90°无功
            //60°无功
            //四线正弦无功
            //三线正弦无功
            //三相四线视在
            //三相三线视在
            //二相三线有功(AC相)
            //单相无功
            //单相三线(AC相)
            //单相三线(BC相)
            //单相三线(AB相)
            //二相三线有功(BC相)
            //二相三线有功(AB相)
            //二相三线无功（AB相）
            //二相三线无功（AC相）
            //二相三线无功（BC相）
            **/
            switch (meterpara)
            {
                //cbx_Connection.Text
                case "单相有功":
                    return "0";
                case "三相四线有功":
                    return "1";
                case "三相三线有功":
                    return "2";
                case "90°无功":
                    return "3";
                case "60°无功":
                    return "4";
                case "四线正弦无功":
                    return "5";
                case "三线正弦无功":
                    return "6";
                case "三相四线视在":
                    return "7";
                case "三相三线视在":
                    return "8";
                case "二相三线有功(AC相)":
                    return "9";
                case "单相无功":
                    return "10";
                case "单相三线(AC相)":
                    return "11";
                case "单相三线(BC相)":
                    return "12";
                case "单相三线(AB相)":
                    return "13";
                case "二相三线有功(BC相)":
                    return "14";
                case "二相三线有功(AB相)":
                    return "15";
                case "二相三线无功（AB相）":
                    return "16";
                case "二相三线无功（AC相）":
                    return "17";
                case "二相三线无功（BC相）":
                    return "18";
                default:
                    return "-1";
            }
        }
        /// <summary>
        /// 额定电压转换代码
        /// </summary>
        public static string Init_meterV(string MeterV)
        {
            /* 额定电压
             * 57.7
             * 100
             * 220
             * 380
             * 110
             * 120**/
            switch (MeterV)
            {
                //cbx_ratedvoltage.Text
                case "57.7":
                    return "0";
                case "100":
                    return "1";
                case "220":
                    return "2";
                case "380":
                    return "3";
                case "110":
                    return "4";
                case "120":
                    return "5";
                default:
                    return "-1";
            }
        }
        /// <summary>
        /// 功率因数代码转化
        /// </summary>
        public static string ADJLC_CHANGE(string LC)
        {
            // 0.25L
            //0.5L
            //0.8L
            //1.0
            //0.8C
            //0.5C
            //0.25C
            //0C
            //0.25L - 反向
            //0.5L - 反向
            //0.8L - 反向
            //1.0 - 反向
            //0.8C - 反向
            //0.5C - 反向
            //0.25C - 反向
            //0L - 反向
            //cbx_LC.Text
            switch (LC)
            {
                case "0.25L":
                    return "0";
                case "0.5L":
                    return "1";
                case "0.8L":
                    return "2";
                case "1.0":
                    return "3";
                case "0.8C":
                    return "4";
                case "0.5C":
                    return "5";
                case "0.25C":
                    return "6";
                case "0C":
                    return "7";
                case "0.25L-反向":
                    return "8";
                case "0.5L-反向":
                    return "9";
                case "0.8L-反向":
                    return "A";
                case "1.0-反向":
                    return "B";
                case "0.8C-反向":
                    return "C";
                case "0.5C-反向":
                    return "D";
                case "0.25C-反向":
                    return "E";
                case "0L-反向":
                    return "F";
                default:
                    return "-1";
            }
        }


        public static int BlueTooth_Channel(string BTC)
        {
            if (BTC.Equals("常规接线模式"))
            {
                return 0;
            }
            else if (BTC.Equals("蓝牙模块接线模式"))
            {
                return 1;
            }
            else if (BTC.Equals("双光电头接线模式"))
            {
                return 2;
            }
            if (BTC.Equals("有功通道"))
            {
                return 3;
            }
            if (BTC.Equals("无功通道"))
            {
                return 4;
            }
            return -1;
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
