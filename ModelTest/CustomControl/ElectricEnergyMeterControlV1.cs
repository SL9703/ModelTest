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

namespace ModelTest.CustomControl
{
    public partial class ElectricEnergyMeterControlV1 : UserControl
    {
        // 定义一个委托，用于调用主窗体方法
        public delegate void UpdateMainFormDelegate(string message,Color? color = null);
        // 事件，让主窗体订阅
        public event UpdateMainFormDelegate OnUpdateRequested_MeterV1;
        public ElectricEnergyMeterControlV1()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(88, 149, 127);
        }
        private EnhancedTcpClient _meterClient;
        private async void btn_MeterTCPConnect_Click(object sender, EventArgs e)
        {
            string _meterIP = tbx_MeterIP.Text.Trim(); //ip
            string _meterPort = tbx_MeterPort.Text.Trim(); //port
            if (string.IsNullOrEmpty(_meterIP) || string.IsNullOrEmpty(_meterPort))
            {
                MessageBox.Show("请输入IP地址和端口号！");
                return;
            }
            if (_meterClient == null)
            {
                _meterClient = new EnhancedTcpClient();//建立链接
                // 订阅事件
                _meterClient.MessageReceived += OnMeterMCUMessageReceived;//监听服务器传来的消息事件
                //_yxclient.MessageSent += OnMessageSent;//传输文件事件
                _meterClient.ConnectionStatusChanged += OnMeterMCUConnectionStatusChanged;//连接状态改变事件
                _meterClient.ErrorOccurred += OnErrorOccurred;
                _meterClient.BytesTransferred += OnBytesTransferred;
                bool connected = await _meterClient.ConnectAsync(_meterIP, int.Parse(_meterPort)); //链接
                if (connected)
                {
                    OnUpdateRequested_MeterV1.Invoke(_meterIP + ":" + _meterPort + "连接成功");
                    btn_MeterTCPConnect.Text = "断开";
                    btn_MeterTCPConnect.BackColor = Color.White;
                }
                else
                {
                    OnUpdateRequested_MeterV1.Invoke(_meterIP + ":" + _meterPort + "连接失败");
                    btn_MeterTCPConnect.Text = "链接";
                    btn_MeterTCPConnect.BackColor = Color.White;
                }
            }
            else
            {
                OnUpdateRequested_MeterV1.Invoke(_meterIP + ":" + _meterPort + "连接失败");
                btn_MeterTCPConnect.Text = "链接";
                btn_MeterTCPConnect.BackColor = Color.White;
            }
        }
        /// <summary>
        /// 处理错误事件，显示错误信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="errorMessage"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnErrorOccurred(object sender, string errorMessage)
        {
            UpdateUI(() =>
            {
                OnUpdateRequested_MeterV1.Invoke($"[错误] {errorMessage}");
            });
        }
        /// <summary>
        /// 统计数据传输的字节数，更新UI显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnBytesTransferred(object? sender, long e)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// 链接状态改变事件处理，更新UI显示当前连接状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnMeterMCUConnectionStatusChanged(object sender, TcpClientStatusEventArgs e)
        {
            UpdateUI(() =>
            {
                string statusText = e.IsConnected ? "✅ 已连接" : "❌ 已断开";
                string color = e.IsConnected ? "Green" : "Red";
                OnUpdateRequested_MeterV1.Invoke($"[{e.Timestamp:HH:mm:ss}] {statusText}: {e.Status}");
                // 更新窗体标题
                if (e.IsConnected)
                {
                    label3.Text = $"状态：TCP客户端 - 已连接到 {_meterClient.ServerEndpoint}";
                }
                else if (_meterClient.Status == "Disconnected")
                {
                    label3.Text = "状态：TCP客户端 - 未连接";
                }
            });
        }
        /// <summary>
        /// 处理服务器传来的消息事件，解析消息内容并更新UI显示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>

        private void OnMeterMCUMessageReceived(object sender, TcpClientMessageEventArgs e)
        {
            UpdateUI(() =>
            {
                //显示原始数据
                string hexData = BitConverter.ToString(e.RawData).Replace("-", " ");
                OnUpdateRequested_MeterV1.Invoke($"接收消息成功[PC<--MCU] : {hexData}",Color.Red);
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
        private async Task SendMCUToPC(string MCUData)
        {
            if (tbxMeterV1Addr.Text != null && MCUData != null)
            {
                if (_meterClient != null)
                {
                    bool send = await _meterClient.SendBytesAsync(ModelTool.HexStringToByteArray(MCUData));
                    if (send)
                    {
                        OnUpdateRequested_MeterV1.Invoke($"发送消息成功[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(MCUData)).Replace("-", " ")}",Color.Red);
                    }
                    else
                    {
                        OnUpdateRequested_MeterV1.Invoke($"发送消息失败[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(MCUData)).Replace("-", " ")}",Color.White);
                    }
                }
            }
        }
    }
}
