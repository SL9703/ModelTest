using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ModelTest.CustomControl
{
    public partial class ShowStandValueUserControl : UserControl
    {
        // 定义一个委托，用于调用主窗体方法
        public delegate void UpdateMainFormDelegate(string message);
        // 事件，让主窗体订阅
        public event UpdateMainFormDelegate OnUpdateRequested;

        XYCtr xyCtr = new XYCtr();
        int OpenComm_data = 0;
        private byte[] sStandValue = new byte[1024];//标准表数据缓存
        private string XYModel = "model1";//新跃源类型
        private string MeterConnection; //电能表参数
        private string MeterV; //电能表额定电压
        private string LC; //相位
        public ShowStandValueUserControl()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(88, 149, 127);
            //初始化控件
            XYInitalizeComponent();
        }

        private void XYInitalizeComponent()
        {
            //电压
            cbxSelecteA_V.SelectedIndex = 2;
            cbxSelecteB_V.SelectedIndex = 2;
            cbxSelecteC_V.SelectedIndex = 2;
            //电流
            cbxSelecetA_A.SelectedIndex = 0;
            cbxSelecetB_A.SelectedIndex = 0;
            cbxSelecetC_A.SelectedIndex = 0;
            //相位
            cbxIAJ.SelectedIndex = 0;
            cbxIBJ.SelectedIndex = 0;
            cbxICJ.SelectedIndex = 0;
            //uab uac
            cbxUab.SelectedIndex = 0;
            cbxUac.SelectedIndex = 1;
            //接线方式 额定电压 额定电流 电表常数
            cbx_Connection.SelectedIndex = 1;
            cbx_ratedvoltage.SelectedIndex = 1;
            cbx_ratedcurrent.SelectedIndex = 1;
            cbx_meterconstant.SelectedIndex = 3;
            //合分元
            cbx_HABC.SelectedIndex = 0;
            cbx_LC.SelectedIndex = 3;
            //相位
            cbx_BlueTooth.SelectedIndex = 0;
            cbx_ToosNum.SelectedIndex = 0;

            ModelTool.BindMutexCheckBoxes(cbx_RJS, cbx_Error);

        }
        /// <summary>
        /// 降源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnXY_x0E_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            if (cbxShutdownUI0.Checked && OpenComm_data == 1)
            {
                int ShutDownUI = 0;
                OnUpdateRequested?.Invoke("输出给源电压电流参数：" + ShutDownUI);
                var result = xyCtr.CallShutPowerSource(ShutDownUI);
                if (result.Result == 1)
                {
                    OnUpdateRequested?.Invoke("降源接口正常 返回值：" + result.Result.ToString());
                }
                else
                {
                    OnUpdateRequested?.Invoke("降源失败，错误代码：" + result.Result.ToString());
                }
            }
            else
            {
                OnUpdateRequested?.Invoke("降源异常，请检查串口是否链接");
            }
        }
        /// <summary>
        /// 升源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCtrlUI_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            if (OpenComm_data == 1)
            {
                //源参数;电压电流 电压电流夹脚， uab uac夹脚
                //byte[] U_I_F_Uab_Uac = new byte[1024];
                string ui = cbxSelecteA_V.Text + "_" +
                cbxSelecteB_V.Text + "_" +
                cbxSelecteC_V.Text + "_" +
                cbxSelecetA_A.Text + "_" +
                cbxSelecetB_A.Text + "_" +
                cbxSelecetC_A.Text + "_" +
                cbxIAJ.Text + "_" +
                cbxIBJ.Text + "_" +
                cbxICJ.Text + "_" +
                cbxUab.Text + "_" +
                cbxUac.Text;
                //合并数组
                OnUpdateRequested?.Invoke("输出给源电压电流参数：" + ui);
                //是否进行误差仪计算
                int iPulse = int.Parse(tbxiPulse.Text);
                var result = xyCtr.CallAnyUIOutput(ui, iPulse);
                if (result.Result == 1)
                {
                    OnUpdateRequested?.Invoke("控源接口正常 返回值：" + result.Result.ToString());
                }
                else
                {
                    OnUpdateRequested?.Invoke("控源失败，错误代码：" + result.Result.ToString());
                }
            }

        }
        /// <summary>
        /// 打开串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SourcePort_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            try
            {
                if (OpenComm_data == 1)
                {
                    //串口是打开的状态
                    XYCtr.CallCloseComm();
                    btn_SourcePort.Text = "OPEN";
                    btn_SourcePort.BackColor = Color.YellowGreen;
                    OnUpdateRequested?.Invoke("源串口已关闭");
                    OpenComm_data = 0;
                }
                else
                {
                    //初始化源串口
                    var result = xyCtr.CallOpenComm(int.Parse(tbx_sourcePort.Text));
                    if (result.Result == 1)
                    {
                        OpenComm_data = 1;
                        OnUpdateRequested?.Invoke("源串口打开成功");
                        //串口已经关闭状态，需要设置好属性后打开
                        OnUpdateRequested?.Invoke("源串口已打开");
                        btn_SourcePort.Text = "CLOSE";
                        btn_SourcePort.BackColor = Color.IndianRed;
                    }
                    else
                    {
                        OnUpdateRequested?.Invoke("源串口打开失败，错误代码：" + OpenComm_data);
                        XYCtr.CallCloseComm();
                        btn_SourcePort.Text = "OPEN";
                        btn_SourcePort.BackColor = Color.YellowGreen;
                    }
                }
            }
            catch (Exception ex_prot)
            {
                OnUpdateRequested?.Invoke(ex_prot.Message);
                XYCtr.CallCloseComm();
                btn_SourcePort.Text = "OPEN";
                btn_SourcePort.BackColor = Color.YellowGreen;
            }
        }
        /// <summary>
        /// 读取标准表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ReadStandMeter_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            if (OpenComm_data == 1)
            {

                var result = xyCtr.CallReadStandValue(XYModel, sStandValue);
                int ReadStandMeterResult = result.Result;
                if (ReadStandMeterResult == 1)
                {
                    OnUpdateRequested?.Invoke("读标准表接口正常 返回值：" + ReadStandMeterResult.ToString());
                    var StandResult = ModelTool.SplitString(System.Text.Encoding.Default.GetString(sStandValue));
                    ShowTextReadStandValue(StandResult);//winform显示标准表数据
                    ModelTool.LogSplitResult(StandResult);
                }
                else
                {
                    OnUpdateRequested?.Invoke("读标准表失败，错误代码：" + ReadStandMeterResult.ToString());
                }
            }
        }
        private void ShowTextReadStandValue(IList<string> processedParts)
        {

            try
            {
                //电压
                tb_UA.Text = processedParts[0];
                tb_UB.Text = processedParts[1];
                tb_UC.Text = processedParts[2];
                //电流       
                tb_IA.Text = processedParts[3];
                tb_IB.Text = processedParts[4];
                tb_IC.Text = processedParts[5];
                //相位角     
                tb_XA.Text = processedParts[6];
                tb_XB.Text = processedParts[7];
                tb_XC.Text = processedParts[8];
                //有功       
                tb_PA.Text = processedParts[9];
                tb_PB.Text = processedParts[10];
                tb_PC.Text = processedParts[11];
                //无功       
                tb_QA.Text = processedParts[12];
                tb_QB.Text = processedParts[13];
                tb_QC.Text = processedParts[14];
                //频率       
                tb_HZ.Text = processedParts[15];
                //报警
                tb_Alarm.Text = processedParts[16];
                //uba
                tb_Uba.Text = processedParts[17];
                //uca
                tb_Uca.Text = processedParts[18];
                //相线
                tb_xx.Text = processedParts[19];
                //电压量程
                tb_V_LC.Text = processedParts[20];
                //电流量程
                tb_A_LC.Text = processedParts[21];

                //tb_SA.Text = processedParts[0];
                //tb_SB.Text = processedParts[0];
                //tb_SC.Text = processedParts[0];

                //tb_PFA.Text = processedParts[0];
                //tb_PFB.Text = processedParts[0];
                //tb_PFC.Text = processedParts[0];

                //tb_EP.Text = processedParts[0];
                //tb_EQ.Text = processedParts[0];
                //tb_ES.Text = processedParts[0];
            }
            catch (Exception ex)
            {
                OnUpdateRequested?.Invoke("显示标准表信息异常:" + ex.ToString());
            }
        }
        /// <summary>
        /// 读取装置信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnCmdReadMeterData_Click(object sender, EventArgs e)
        {
            byte[] sResultData;
            sResultData = new byte[255];
            var result = xyCtr.CallReadTestData(0, 0, sResultData);
            if (result.Result == 1)
            {
                OnUpdateRequested?.Invoke("读取装置信息成功：" + System.Text.Encoding.Default.GetString(sResultData));
                lblcmd.Text = System.Text.Encoding.Default.GetString(sResultData);
            }
            else
            {
                OnUpdateRequested?.Invoke("读取装置信息失败，错误代码：" + result.Result);
            }
        }
        /// <summary>
        /// 读取常数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ReadContans_Click(object sender, EventArgs e)
        {

            LogMessage.Info(sender.ToString());
            byte[] constas = new byte[1024];
            var result = xyCtr.CallReadStandConst(constas);
            if (result.Result == 1)
            {
                OnUpdateRequested?.Invoke("读取常数接口正常" + result.Result);
                tb_contans.Text = System.Text.Encoding.Default.GetString(constas);
            }
            else
            {
                OnUpdateRequested?.Invoke("调用读取常数接口异常" + result.Result);
            }
        }
        /// <summary>
        /// 初始化电表参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Init_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MeterConnection = XYCtr.Init_meterConnection(cbx_Connection.Text);
            MeterV = XYCtr.Init_meterV(cbx_ratedvoltage.Text);
            var MeterInit = $"Ini_{MeterConnection}_{MeterV}_{cbx_ratedcurrent.Text}_{cbx_meterconstant.Text}_E";
            OnUpdateRequested?.Invoke("初始化电表参数" + MeterInit);
            var result = xyCtr.CallSendCommand(MeterInit, true);
            if (result.Result == 1)
            {
                OnUpdateRequested?.Invoke("初始化电表接口正常" + result.Result);
            }
            else
            {
                OnUpdateRequested?.Invoke("初始化电表接口异常" + result.Result);
            }

        }

        /// <summary>
        /// adj 升源接口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_XY_ADJ_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            LC = XYCtr.ADJLC_CHANGE(cbx_LC.Text);
            var AdjCMD = $"Adj_{tbx_V_5.Text}_{tbx_A_5.Text}_{cbx_HABC.Text}_{LC}_{tbxiPulse.Text}_E";
            OnUpdateRequested?.Invoke("ADJ升源接口指令" + AdjCMD);
            var result = xyCtr.CallSendCommand(AdjCMD, true);
            if (result.Result == 1)
            {
                OnUpdateRequested?.Invoke("ADJ升源接口正常" + result.Result);
            }
            else
            {
                OnUpdateRequested?.Invoke("ADJ升源接口异常" + result.Result);
            }
        }
        int BluetoothMode = 0; //接线模式 0-常规接线 1-蓝牙 2-双光电头
        int BluetoothChannel = 3; //通道号 3-有功 4-无功 
        bool BoolTooth = true;//模式选择
        /// <summary>
        /// 设置蓝牙模式和通道 
        /// 先设置模式
        ///常规接线模式
        ///蓝牙模块接线模式
        ///双光电头接线模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_settooth_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            if (BoolTooth)
            {
                BluetoothMode = XYCtr.BlueTooth_Channel(cbx_BlueTooth.Text);
                OnUpdateRequested?.Invoke("设置模式：" + cbx_BlueTooth.Text);
                var result = xyCtr.CallSet_BlueTooth_Channel(BluetoothMode);
                if (result.Result == 1)
                {
                    OnUpdateRequested?.Invoke("设置成功");
                }
                else
                {
                    OnUpdateRequested?.Invoke("设置失败，错误代码：" + result.Result);
                }
                btn_settooth.Text = "设置通道"; //切换到设置通道
                BoolTooth = false; //切换到设置通道 
            }
            else
            {
                //设置通道
                BluetoothChannel = XYCtr.BlueTooth_Channel(cbx_ToosNum.Text);
                OnUpdateRequested?.Invoke("设置通道：" + cbx_ToosNum.Text);
                var result = xyCtr.CallSet_BlueTooth_Channel(BluetoothChannel);
                if (result.Result == 1)
                {
                    OnUpdateRequested?.Invoke("设置成功");
                }
                else
                {
                    OnUpdateRequested?.Invoke("设置失败，错误代码：" + result.Result);
                }
                btn_settooth.Text = "设置模式"; //切换到设置模式
                BoolTooth = true; //切换到设置模式
            }
        }
        /// <summary>
        /// 开始误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btn_ErrorStart_Click(object sender, EventArgs e)
        {
            //时钟误差
            if (cbx_RJS.Checked)
            {
                LogMessage.Info(sender.ToString());
                btn_ErrorStart.Enabled = false;//开始实验之后，置灰，不可用状态
                int iPulse = int.Parse(tbx_iPulse.Text);//时钟误差数
                OnUpdateRequested?.Invoke("时钟误差数：" + iPulse);

                var result = xyCtr.Call_Clock_Start(iPulse);
                if (result.Result == 1)
                {
                    OnUpdateRequested?.Invoke($"开始测试时钟误差,延迟等待{iPulse + iPulse}秒，等待结束自动读取误差数据。");
                    await Task.Delay(iPulse * 1000 + iPulse * 1000);//延迟等待
                }
                //读取误差
                ReadTestError();
                btn_ErrorStart.Enabled = true;
            }
            //基础误差
            else if (cbx_Error.Checked)
            {
                LogMessage.Info(sender.ToString());
                //获取传入的参数
                string MeterConstant = tbx_MeterConstant.Text;//电表常数
                int.TryParse(tbx_MeterNo.Text.Split('-')[1], out int ListNo);//表位数
                int Pulse = int.Parse(tbx_iPulse.Text);//圈数
                StringBuilder CMeterConstant = new StringBuilder();
                for (int i = 1; i <= ListNo; i++)
                {
                    if (i < ListNo)
                    {
                        CMeterConstant.Append(tbx_MeterConstant.Text + ",");
                    }
                    else
                    {
                        CMeterConstant.Append(tbx_MeterConstant.Text);
                    }
                }
                OnUpdateRequested?.Invoke($"误差启动=>  Error_Start({CMeterConstant.ToString()},{ListNo},{Pulse})");
                var result = xyCtr.Call_Error_Start(CMeterConstant.ToString(), ListNo, Pulse);
                if (result.Result == 1)
                {
                    OnUpdateRequested?.Invoke("启动误差接口正常" + result.Result);
                    //添加延迟等待
                    await Task.Delay(int.Parse(tbx_iPulse.Text) * 1000);
                    //读取实验误差
                    ReadTestError_1(ListNo);
                }
                else
                {
                    OnUpdateRequested?.Invoke("调用启动误差接口异常" + result.Result);
                }
            }
            else
            {
                MessageBox.Show("异常，未选中任何实验……", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 停止误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ErrorStop_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            var result = xyCtr.Call_Stop_Test();
            if (result.Result == 1)
            {
                OnUpdateRequested?.Invoke("停止误差接口正常" + result.Result);
            }
            else
            {
                OnUpdateRequested?.Invoke("调用停止误差接口异常" + result.Result);
            }
        }
        /// <summary>
        /// 清除误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ErrorClean_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            var result = xyCtr.Call_Error_Clear();
            if (result.Result == 1)
            {
                OnUpdateRequested?.Invoke("清除误差接口正常" + result.Result);
            }
            else
            {
                OnUpdateRequested?.Invoke(("调用清除误差接口异常" + result.Result));
            }
        }
        /// <summary>
        /// 读取时钟误差
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ReadTestError()
        {
            byte[] MeterError = new byte[1024];
            string[] meterNo = tbx_MeterNo.Text.Split('-');//分割字符串
            if (meterNo.Length == 2 && int.TryParse(meterNo[0], out int firtNo) && int.TryParse(meterNo[1], out int ListNo))
            {
                for (int i = firtNo; i < ListNo + 1; i++)
                {
                    var result = xyCtr.CallReadTestData(1, i, MeterError);
                    if (result.Result == 1)
                    {
                        OnUpdateRequested?.Invoke($"正在读取{i}表位误差数据...");
                        OnUpdateRequested?.Invoke($"误差数据" + MeterError + "+\r\n");
                    }
                }
            }
            else if (meterNo.Length != 2)
            {
                OnUpdateRequested?.Invoke("电表编号格式错误，请输入正确的格式，如：1-1,1-12");
                return;
            }
        }
        /// <summary>
        /// 读取基础误差
        /// </summary>
        /// <param name="meter"></param>
        private void ReadTestError_1(int meter)
        {
            byte[] MeterError = new byte[1024];
            for (int i = 1; i <= meter; i++)
            {
                OnUpdateRequested?.Invoke($"正在读取{i}表位误差数据...");
                var result = xyCtr.Call_Read_TestError(i, MeterError);
                if (result.Result == 1)
                {
                    OnUpdateRequested?.Invoke($"误差数据" + MeterError + "+\r\n");
                }
            }
        }
        /// <summary>
        /// 读取dll版本日期
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ReadTime_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            byte[] StrVer = new byte[1024];
            var result = xyCtr.CallFunctionReadVersion(StrVer);
            if (result.Result == 1)
            {
                OnUpdateRequested?.Invoke("读取新跃dll版本日期接口正常" + result.Result);
            }
            else
            {
                OnUpdateRequested?.Invoke("读取新跃dll版本日期接口异常" + result.Result);
            }
        }

        private void btn_SetUIRange_Click(object sender, EventArgs e)
        {
            //得到数据，分割数据，
            string ServerData = textBoxSetUIRange.Text;
            string[] ServerDataNew = ModelTool.StringDataSplit(ServerData);
            int Iui = int.Parse(ServerDataNew[0]);
            int Ivalue = int.Parse(ServerDataNew[1]);
            var result = xyCtr.CallSetUIRange(Iui, Ivalue);
            if (result.Result > 0)
            {
                OnUpdateRequested?.Invoke("设置setui接口正常" + result.Result);
            }
            else
            {
                OnUpdateRequested?.Invoke("设置setui接口异常" + result.Result);
            }
        }

        private void btn_RangeOutputUI_Click(object sender, EventArgs e)
        {
            string RangeOutputUIData = textBoxRangeOutputUI.Text;
            string[] ServerDataNew = ModelTool.StringDataSplit(RangeOutputUIData);
            string ua = ServerDataNew[0];
            string ub = ServerDataNew[1];
            string uc = ServerDataNew[2];
            string ia = ServerDataNew[3];
            string ib = ServerDataNew[4];
            string ic = ServerDataNew[5];
            string StrUICommand = $"{ua}_{ub}_{uc}_{ia}_{ib}_{ic}";
            var result = xyCtr.CallRangeOutputUI(StrUICommand);
            if (result.Result == 1)
            {
                OnUpdateRequested?.Invoke("设置RangeOutputUI接口正常" + result.Result);
            }
            else
            {
                OnUpdateRequested?.Invoke("设置RangeOutputUI接口异常" + result.Result);
            }
        }
    }
}
