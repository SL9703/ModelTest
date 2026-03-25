using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ModelTest.Socket_DLL.Socket_Client;
using ModelTest.Tools;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Button;

namespace ModelTest.CustomControl
{
    public partial class TerminalV1YXUserControl : UserControl
    {
        // 定义一个委托，用于调用主窗体方法
        public delegate void UpdateMainFormDelegate(string message);
        // 事件，让主窗体订阅
        public event UpdateMainFormDelegate OnUpdateRequestedTYXLog;
        public TerminalV1YXUserControl()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(88, 149, 127);
        }
        private EnhancedTcpClient _yxclient;
        private async void btn_YXConnect_Click(object sender, EventArgs e)
        {
            string yxip = tbxyxIp.Text;
            int yxport = int.Parse(tbxyxPort.Text);
            if (_yxclient == null)
            {
                _yxclient = new EnhancedTcpClient();
                // 订阅事件
                _yxclient.MessageReceived += OnYXMCUMessageReceived;//监听服务器传来的消息事件
                //_yxclient.MessageSent += OnMessageSent;//传输文件事件
                _yxclient.ConnectionStatusChanged += OnYXMCUConnectionStatusChanged;//连接状态改变事件
                _yxclient.ErrorOccurred += OnErrorOccurred;
                _yxclient.BytesTransferred += OnBytesTransferred;
                bool connected = await _yxclient.ConnectAsync(yxip, yxport); //链接
                if (connected)
                {
                    OnUpdateRequestedTYXLog.Invoke(yxip + ":" + yxport + "连接成功");
                    btn_YXConnect.Text = "断开";
                    btn_YXConnect.BackColor = Color.White;
                }
                else
                {
                    OnUpdateRequestedTYXLog.Invoke(yxip + ":" + yxport + "连接失败");
                    btn_YXConnect.Text = "链接";
                    btn_YXConnect.BackColor = Color.White;
                }
            }
        }
        /// <summary>
        /// 统计数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnBytesTransferred(object? sender, long e)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 报错事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="errorMessage"></param>

        private void OnErrorOccurred(object sender, string errorMessage)
        {
            UpdateUI(() =>
            {
                OnUpdateRequestedTYXLog.Invoke($"[错误] {errorMessage}");
            });
        }
        /// <summary>
        /// 链接状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnYXMCUConnectionStatusChanged(object sender, TcpClientStatusEventArgs e)
        {
            UpdateUI(() =>
            {
                string statusText = e.IsConnected ? "✅ 已连接" : "❌ 已断开";
                string color = e.IsConnected ? "Green" : "Red";
                OnUpdateRequestedTYXLog.Invoke($"[{e.Timestamp:HH:mm:ss}] {statusText}: {e.Status}");
                // 更新窗体标题
                if (e.IsConnected)
                {
                    groupBox1.Text = $"数据汇总通信单元    TCP客户端 - 已连接到 {_yxclient.ServerEndpoint}";
                }
                else if (_yxclient.Status == "Disconnected")
                {
                    groupBox1.Text = "数据汇总通信单元    TCP客户端 - 未连接";
                }
            });
        }
        /// <summary>
        /// 消息接收事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnYXMCUMessageReceived(object sender, TcpClientMessageEventArgs e)
        {
            UpdateUI(() =>
            {
                //显示原始数据
                string hexData = BitConverter.ToString(e.RawData).Replace("-", " ");
                OnUpdateRequestedTYXLog.Invoke($"接收消息成功[PC<--MCU] : {hexData}");
                LogMessage.Debug($"接受消息成功[PC<-- MCU]的数据: {hexData}");
            });
        }
        private void UpdateUI(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mCU"></param>
        /// <returns></returns>
        private async Task SendMCUToPC(string MCUData)
        {
            if (tbx_YXAddr.Text != null && MCUData != null)
            {
                if (_yxclient != null)
                {
                    bool send = await _yxclient.SendBytesAsync(ModelTool.HexStringToByteArray(MCUData));
                    if (send)
                    {
                        OnUpdateRequestedTYXLog.Invoke($"发送消息成功[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(MCUData)).Replace("-", " ")}");
                    }
                    else
                    {
                        OnUpdateRequestedTYXLog.Invoke($"发送消息成功[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(MCUData)).Replace("-", " ")}");
                    }
                }
            }
        }
        string MCUStartByte = "55";
        string TerminalDataLength = string.Empty;
        string MCUCtrl = "00";//控制协议
        string MCUTransparent = "00";//透传协议
        string CommandCode = string.Empty;
        string MCUAddr = string.Empty;
        string MCUData_1 = string.Empty;
        string MCUData_2 = string.Empty;
        string MCUStopByte = "AA";
        /// <summary>
        /// 启动遥信
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btn_YXstart_Click(object sender, EventArgs e)
        {
            MCUAddr = tbx_YXAddr.Text;
            MCUData_1 = UpdateDisplay(cbx_YX0, cbx_YX1, cbx_YX2, cbx_YX3, cbx_YX4, cbx_YX5, cbx_YX6, cbx_YX7);
            MCUData_2 = "FF";
            TerminalDataLength = HexConverter.ConvertHex(ModelTool.ToHex(((2 + 3 + 2 + 1))));
            var StartTerminalV1_YX = TerminalModel.TerminalByte(MCUStartByte, TerminalDataLength + "00", MCUAddr, MCUCtrl, "03", MCUData_1 + MCUData_2, MCUStopByte);
            //OnUpdateRequestedTYXLog.Invoke(StartTerminalV1_YX);
            await SendMCUToPC(StartTerminalV1_YX);
        }
        // 核心转换：根据复选框状态计算二进制并更新十六进制
        private string UpdateDisplay(params System.Windows.Forms.CheckBox[] checkBox)
        {
            // 1. 构建二进制字符串 (高位在左 D7 ... D0)
            char[] bits = new char[8];
            for (int i = 0; i < 8; i++)
            {
                // i=0 -> D7, i=1 -> D6 ... i=7 -> D0
                // 因为通常显示从左到右是 MSB 到 LSB，所以索引映射
                int bitIndex = 7 - i;  // D7对应checkBoxes[7], D0对应checkBoxes[0]
                bits[i] = checkBox[bitIndex].Checked ? '1' : '0';
            }
            string binaryStr = new string(bits);  // 例如 "10100101"
            label4.Text = binaryStr;

            // 2. 计算数值 (根据D0~D7权重，D0为最低位)
            int value = 0;
            for (int i = 0; i < 8; i++)
            {
                if (checkBox[i].Checked)  // i=0对应D0(bit0)，权重 1<<i
                {
                    value |= (1 << i);
                }
            }
            // 3. 转换为十六进制，始终显示两位大写 (00~FF)
            string hexStr = value.ToString("X2");
            label5.Text = hexStr;
            return hexStr;
        }
        /// <summary>
        /// 关闭遥信
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btn_YXstop_Click(object sender, EventArgs e)
        {
            MCUAddr = tbx_YXAddr.Text;
            TerminalDataLength = HexConverter.ConvertHex(ModelTool.ToHex(((2 + 3 + 2 + 1))));
            var StopTerminalV1_YX = TerminalModel.TerminalByte(MCUStartByte, TerminalDataLength + "00", MCUAddr, MCUCtrl, "03", "00" + "00", MCUStopByte);
            //OnUpdateRequestedTYXLog.Invoke(StopTerminalV1_YX);
            await SendMCUToPC(StopTerminalV1_YX);
        }
        /// <summary>
        /// 启动脉冲 0x05 0x01&0x02 0x01
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnStartMC_Click(object sender, EventArgs e)
        {
            //55 1100 01 00 05 01 01 0000C842 10000000 32 64 AA PWM1输出100HZ占空比50%，输出16个脉冲
            //55 1100 01 00 05 02 01 0000C842 10000000 32 65 AA PWM2输出100HZ占空比50%，输出16个脉冲
            MCUAddr = tbx_YXAddr.Text;
            bool mcc = double.TryParse(tbxMCCounts.Text, out double MCcounts);
            bool mctime = double.TryParse(tbxMCTime.Text, out double MCTime);
            if (mcc && mctime)
                tbxMCHZ.Text = (MCcounts / (MCTime * 60)).ToString();
            label9.Text = "频率16进制：" + IEEE754Converter.FloatToHex(float.Parse(tbxMCHZ.Text));
            label10.Text = "脉冲个数16进制：" + ModelTool.Ensure4Bytes(ModelTool.ToHex(long.Parse(tbxMCCounts.Text)), 4);
            TerminalDataLength = HexConverter.ConvertHex(ModelTool.ToHex((2 + 3 + 11 + 1)));
            if (btnStartMC.Text == "启动脉冲")
            {
                var StartMAC_One = TerminalModel.TerminalByte(
              MCUStartByte, TerminalDataLength + "00", MCUAddr, MCUCtrl,
              "05", "01" + "01"
              + IEEE754Converter.FloatToHex(float.Parse(tbxMCHZ.Text))
              + ModelTool.Ensure4Bytes(ModelTool.ToHex(long.Parse(tbxMCCounts.Text)), 4),
              MCUStopByte
              );
                var StartMAC_Two = TerminalModel.TerminalByte(
                    MCUStartByte, TerminalDataLength + "00", MCUAddr, MCUCtrl,
                    "05", "02" + "01"
                    + IEEE754Converter.FloatToHex(float.Parse(tbxMCHZ.Text))
                    + ModelTool.Ensure4Bytes(ModelTool.ToHex(long.Parse(tbxMCCounts.Text)), 4), MCUStopByte
                    );
                await SendMCUToPC(StartMAC_One);
                Thread.Sleep(500);
                await SendMCUToPC(StartMAC_Two);
                btnStartMC.Text = "停止脉冲";
            }
            else if (btnStartMC.Text == "停止脉冲")
            {
                var StopMAC_One = TerminalModel.TerminalByte(
                MCUStartByte, TerminalDataLength + "00", MCUAddr, MCUCtrl,
                "05", "01" + "00"
                + IEEE754Converter.FloatToHex(float.Parse(tbxMCHZ.Text))
                + ModelTool.Ensure4Bytes(ModelTool.ToHex(long.Parse(tbxMCCounts.Text)), 4),
                MCUStopByte
                );
                var StopMAC_Two = TerminalModel.TerminalByte(
                    MCUStartByte, TerminalDataLength + "00", MCUAddr, MCUCtrl,
                    "05", "02" + "00"
                    + IEEE754Converter.FloatToHex(float.Parse(tbxMCHZ.Text))
                    + ModelTool.Ensure4Bytes(ModelTool.ToHex(long.Parse(tbxMCCounts.Text)), 4), MCUStopByte
                    );
                await SendMCUToPC(StopMAC_One);
                Thread.Sleep(500);
                await SendMCUToPC(StopMAC_Two);
                btnStartMC.Text = "启动脉冲";
            }
        }
    }
}
