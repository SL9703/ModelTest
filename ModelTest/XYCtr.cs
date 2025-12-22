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
    public class XYCtr
    {
        public static int iResult; //降源接口返回值
        /// <summary>
        /// 降源接口 0-----电压、电流同时停止输出1-----电压停止输出 2-----电流停止输出
        /// </summary>
        /// <param name="ShutPowerSource"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int ShutPowerSource([In] int ShutPowerSource);
        public static int CallShutPowerSource(int shutPowerSource)
        {

            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = ShutPowerSource(shutPowerSource);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("降源正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用降源接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// 打开源串口
        /// </summary>
        /// <param name="Port"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int OpenComm(int Port);

        public static int CallOpenComm(int Port)
        {

            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = OpenComm(Port);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("打开源串口正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用打开源串口接口异常" + iResult);
                    }

                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
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
        /// <summary>
        /// 读取标准表数据接口
        /// </summary>
        /// <param name="StandModel"></param>
        /// <param name="StandValue"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int ReadStandValue([In, Out] string StandModel, [Out] byte[] StandValue);
        public static int CallReadStandValue(string standModel, byte[] sStandValue)
        {
            object lockObject = new object();
            lock (lockObject)
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        iResult = ReadStandValue(standModel, sStandValue);
                        if (iResult == 1)
                        {
                            LogMessage.Debug("标准表数据返回成功");
                            LogMessage.Debug("新跃源标准表数据：" + System.Text.Encoding.Default.GetString(sStandValue));
                        }
                        else
                        {
                            LogMessage.Debug("标准表数据返回失败，错误代码：" + iResult);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage.Error(ex);
                    }
                });
                thread.IsBackground = true;
                thread.Start();
                return iResult;
            }
        }

        /// <summary>
        /// 控源输出接口
        /// </summary>
        /// <param name="StrUICommand">控源参数</param>
        /// <param name="iPulse">脉冲数</param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int AnyUIOutput(string StrUICommand, int iPulse);
        public static int CallAnyUIOutput(string StrUICommand, int iPulse)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = AnyUIOutput(StrUICommand, iPulse);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("控源正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用控源接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
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
        public static int CallReadTestData(int readtype, int iposition, byte[] sresultdata)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = ReadTestData(readtype, iposition, sresultdata);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("读取装置信息成功" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用装置信息接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }

        /// <summary>
        /// 读取常数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int ReadStandConst([Out] byte[] constanst);

        public static int CallReadStandConst(byte[] constanst)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = ReadStandConst(constanst);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("读取常数成功" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用读取常数接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// 初始化电表参数
        /// </summary>
        /// <param name="Cmd"></param>
        /// <param name="AdjTags"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int SendCommand(string Cmd, bool AdjTags);
        public static int CallSendCommand(string cmd, bool AdjTags)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = SendCommand(cmd, AdjTags);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("SendCommand接口正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用SendCommand接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// 设置蓝牙模式和通道
        /// </summary>
        /// <param name="IchanngelNo"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int Set_BlueTooth_Channel(int IchanngelNo);
        public static int CallSet_BlueTooth_Channel(int IchanngelNo)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = Set_BlueTooth_Channel(IchanngelNo);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("设置Set_BlueTooth_Channel接口正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Set_BlueTooth_Channel接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }

        /// <summary>
        /// 测试时钟误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int Clock_Start(int iPulse);
        public static int Call_Clock_Start(int iPulse)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = Clock_Start(iPulse);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("调用设置Clock_Start接口正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Clock_Start接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// 读取误差
        /// </summary>
        /// <param name="iMeterNo"></param>
        /// <param name="MeterError"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int Read_Test([In, Out] int iMeterNo, [Out] byte[] MeterError);
        public static int Call_Read_TestError(int iMeterNo, byte[] MeterError)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = Read_Test(iMeterNo, MeterError);
                    if (iResult == 1)
                    {
                        // int.TryParse(System.Text.Encoding.Default.GetString(MeterError), out double resut);

                        LogMessage.Debug($"读取{iMeterNo}表位误差数据成功：" + MeterError);
                    }
                    else
                    {
                        LogMessage.Debug($"读取{iMeterNo}误差数据失败，错误代码：" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }

        [DllImport("xyctr.dll")]
        private static extern int Error_Start(string MeterConstant, int iMeterCount, int iPulse);
        /// <summary>
        /// 误差试验
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static int Call_Error_Start(string meterConstant, int iMeterCount, int iPulse)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = Error_Start(meterConstant, iMeterCount, iPulse);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("调用设置Error_Start(误差试验)接口正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Error_Start(误差试验)接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// 停止测试
        /// </summary>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int Stop_Test();

        public static int Call_Stop_Test()
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = Stop_Test();
                    if (iResult == 1)
                    {
                        LogMessage.Debug("调用设置Stop_Test(停止误差)接口正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Stop_Test(停止误差)接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error (ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }

        [DllImport("xyctr.dll")]
        private static extern int Error_Start();
        public static int Call_Error_Start()
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = Error_Start();
                    if (iResult == 1)
                    {
                        LogMessage.Debug("调用设置Error_Start(清除误差)接口正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Error_Start(清除误差)接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// 读取脉冲数
        /// </summary>
        /// <param name="iMeterNo"></param>
        /// <param name="MeterError"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int Read_Pulse([In, Out] int iMeterNo, [Out] byte[] MeterError);
        public static int Call_Read_Pulse(int iMeterNo, byte[] MeterError)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = Read_Pulse(iMeterNo, MeterError);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("调用设置Read_Pulse(读取脉冲数)接口正常" + iResult);
                        LogMessage.Debug($"读取表位{iMeterNo}脉冲数数为：{MeterError}");
                    }
                    else
                    {
                        LogMessage.Debug("调用设置Read_Pulse(读取脉冲数)接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// 读取版本号
        /// </summary>
        /// <param name="StrVer"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int FunctionReadVersion([Out] byte[] StrVer);
        public static int CallFunctionReadVersion(byte[] StrVer)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    iResult = FunctionReadVersion(StrVer);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("调用设置FunctionReadVersion(读取版本号)接口正常" + iResult);
                        LogMessage.Debug("版本号为：" + System.Text.Encoding.Default.GetString(StrVer));
                    }
                    else
                    {
                        LogMessage.Debug("调用设置FunctionReadVersion(读取版本号)接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// 设置电压电流量程
        /// </summary>
        /// <param name="iUI"></param>
        /// <param name="iValue"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int SetUIRange(int iUI, int iValue);
        public static int CallSetUIRange(int iui,int ivalue)
        {
            Thread thread = new Thread(() =>
            {

                try
                {
                    iResult = SetUIRange(iui, ivalue);
                    if (iResult > 0)
                    {
                        LogMessage.Debug("SetUIRange设置电压和电流量程接口正常" + iResult);
                    }
                    else if (true)
                    {
                        LogMessage.Debug("SetUIRange设置电压和电流量程接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
        }
        /// <summary>
        /// rangeoutoutui
        /// </summary>
        /// <param name="StrUICommand"></param>
        /// <returns></returns>
        [DllImport("xyctr.dll")]
        private static extern int RangeOutputUI(string StrUICommand);
        public static int CallRangeOutputUI(string StrUICommand)
        {
            Thread thread = new Thread(() =>
            {

                try
                {
                    iResult = RangeOutputUI(StrUICommand);
                    if (iResult == 1)
                    {
                        LogMessage.Debug("RangeOutputUI设置电压和电流量程接口正常" + iResult);
                    }
                    else
                    {
                        LogMessage.Debug("RangeOutputUI设置电压和电流量程接口异常" + iResult);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error(ex);
                }
            });
            thread.IsBackground = true;
            thread.Start();
            return iResult;
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
    }
}
