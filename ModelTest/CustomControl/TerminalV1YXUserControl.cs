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

namespace ModelTest.CustomControl
{
    public partial class TerminalV1YXUserControl : UserControl
    {
        // 定义一个委托，用于调用主窗体方法
        public delegate void UpdateMainFormDelegate(string message);
        // 事件，让主窗体订阅
        public event UpdateMainFormDelegate? OnUpdateRequestedTYXLog;
        private EnhancedTcpClient? _yxclient;
        private readonly SemaphoreSlim _connectionGate = new(1, 1);
        private readonly SemaphoreSlim _pulseCommandGate = new(1, 1);
        private bool _isDisposed;
        private bool _pulseOutputRunning;

        public TerminalV1YXUserControl()
        {
            InitializeComponent();
            this.BackColor = Color.FromArgb(88, 149, 127);
            cbxChange232And485.SelectionLength = 0;
            Disposed += TerminalV1YXUserControl_Disposed;
        }

        private async void btn_YXConnect_Click(object sender, EventArgs e)
        {
            if (_isDisposed || !await _connectionGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                if (_yxclient?.IsConnected == true)
                {
                    DisconnectYXClient("用户主动断开连接");
                    return;
                }

                string yxip = tbxyxIp.Text.Trim();
                if (!int.TryParse(tbxyxPort.Text.Trim(), out int yxport) || yxport is < 1 or > 65535)
                {
                    PublishLog("连接失败：端口必须是1-65535。");
                    return;
                }

                if (string.IsNullOrWhiteSpace(yxip))
                {
                    PublishLog("连接失败：IP地址不能为空。");
                    return;
                }

                SetConnectionButtonState(false, "连接中...");
                DisconnectYXClient("准备建立新的连接");

                EnhancedTcpClient client = CreateYXClient();
                _yxclient = client;

                bool connected = await client.ConnectAsync(yxip, yxport);
                if (connected && client.IsConnected)
                {
                    PublishLog($"{yxip}:{yxport}连接成功");
                    SetConnectionButtonState(true, "断开");
                }
                else
                {
                    PublishLog($"{yxip}:{yxport}连接失败");
                    DisconnectYXClient("连接失败，清理客户端");
                    SetConnectionButtonState(false, "连接");
                }
            }
            catch (Exception ex)
            {
                PublishLog($"连接操作异常：{ex.Message}");
                LogMessage.Error("[遥信TCP] 连接操作异常", ex);
                DisconnectYXClient("连接异常，清理客户端");
                SetConnectionButtonState(false, "连接");
            }
            finally
            {
                _connectionGate.Release();
            }
        }

        /// <summary>创建遥信TCP客户端并统一配置事件，关闭自动重连避免手动断开后又被后台重连。</summary>
        private EnhancedTcpClient CreateYXClient()
        {
            EnhancedTcpClient client = new()
            {
                EnableAutoReconnect = false
            };
            client.MessageReceived += OnYXMCUMessageReceived;
            client.ConnectionStatusChanged += OnYXMCUConnectionStatusChanged;
            client.ErrorOccurred += OnErrorOccurred;
            client.BytesTransferred += OnBytesTransferred;
            return client;
        }

        /// <summary>解除事件并释放当前遥信客户端，保证失败连接和主动断开都不残留资源。</summary>
        private void DisconnectYXClient(string reason)
        {
            EnhancedTcpClient? client = _yxclient;
            _yxclient = null;
            if (client is null)
            {
                SetConnectionButtonState(false, "连接");
                return;
            }

            try
            {
                client.MessageReceived -= OnYXMCUMessageReceived;
                client.ConnectionStatusChanged -= OnYXMCUConnectionStatusChanged;
                client.ErrorOccurred -= OnErrorOccurred;
                client.BytesTransferred -= OnBytesTransferred;
                if (client.IsConnected || client.Status == "Connecting")
                {
                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                LogMessage.Error($"[遥信TCP] {reason}时断开客户端异常", ex);
            }
            finally
            {
                client.Dispose();
                SetConnectionButtonState(false, "连接");
            }
        }

        /// <summary>控件销毁时停止接收回调并释放TCP客户端。</summary>
        private void TerminalV1YXUserControl_Disposed(object? sender, EventArgs e)
        {
            _isDisposed = true;
            DisconnectYXClient("控件销毁");
        }

        /// <summary>安全写入主窗体日志，避免主窗体未订阅或控件销毁时抛异常。</summary>
        private void PublishLog(string message)
        {
            if (_isDisposed)
                return;

            try
            {
                OnUpdateRequestedTYXLog?.Invoke(message);
            }
            catch (Exception ex)
            {
                LogMessage.Error("[遥信TCP] 写入界面日志异常", ex);
            }
        }

        /// <summary>统一更新连接按钮，所有UI操作都回到控件线程。</summary>
        private void SetConnectionButtonState(bool connected, string text)
        {
            UpdateUI(() =>
            {
                if (_isDisposed || btn_YXConnect.IsDisposed)
                    return;

                btn_YXConnect.Text = text;
                btn_YXConnect.Enabled = true;
                btn_YXConnect.BackColor = connected
                    ? Color.FromArgb(255, 235, 205)
                    : Color.White;
            });
        }

        /// <summary>
        /// 统计数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnBytesTransferred(object? sender, long e)
        {
            // 该控件暂不展示流量统计，但不能让事件回调抛异常终止TCP接收线程。
        }
        /// <summary>
        /// 报错事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="errorMessage"></param>

        private void OnErrorOccurred(object? sender, string errorMessage)
        {
            UpdateUI(() =>
            {
                PublishLog($"[错误] {errorMessage}");
            });
        }
        /// <summary>
        /// 链接状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnYXMCUConnectionStatusChanged(object? sender, TcpClientStatusEventArgs e)
        {
            UpdateUI(() =>
            {
                string statusText = e.IsConnected ? "✅ 已连接" : "❌ 已断开";
                PublishLog($"[{e.Timestamp:HH:mm:ss}] {statusText}: {e.Status}");
                // 更新窗体标题
                if (e.IsConnected)
                {
                    string endpoint = sender is EnhancedTcpClient client
                        ? client.ServerEndpoint
                        : _yxclient?.ServerEndpoint ?? "未知端点";
                    groupBox1.Text = $"数据汇总通信单元    TCP客户端 - 已连接到 {endpoint}";
                }
                else
                {
                    groupBox1.Text = "数据汇总通信单元    TCP客户端 - 未连接";
                    SetConnectionButtonState(false, "连接");
                }
            });
        }
        /// <summary>
        /// 消息接收事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnYXMCUMessageReceived(object? sender, TcpClientMessageEventArgs e)
        {
            UpdateUI(() =>
            {
                //显示原始数据
                string hexData = BitConverter.ToString(e.RawData).Replace("-", " ");
                PublishLog($"接收消息成功[PC<--MCU] : {hexData}");
                string protocolDescription = ParseAdditionalProtocolResponse(e.RawData);
                if (!string.IsNullOrWhiteSpace(protocolDescription))
                    PublishLog(protocolDescription);
                LogMessage.Debug($"接收消息成功[PC<--MCU]的数据: {hexData}");
            });
        }

        private void UpdateUI(Action action)
        {
            if (_isDisposed || IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch (ObjectDisposedException)
                {
                    // 控件已经释放，忽略后台TCP事件。
                }
                catch (InvalidOperationException)
                {
                    // 控件正在销毁，忽略后台TCP事件。
                }
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
            if (string.IsNullOrWhiteSpace(MCUData))
                return;

            EnhancedTcpClient? client = _yxclient;
            if (client is null || !client.IsConnected)
            {
                PublishLog("发送失败：遥信TCP客户端未连接。");
                return;
            }

            try
            {
                byte[] data = ModelTool.HexStringToByteArray(MCUData);
                bool send = await client.SendBytesAsync(data);
                string hex = BitConverter.ToString(data).Replace("-", " ");
                PublishLog(send
                    ? $"发送消息成功[PC-->MCU] : {hex}"
                    : $"发送消息失败[PC-->MCU] : {hex}");
            }
            catch (Exception ex)
            {
                PublishLog($"发送消息异常：{ex.Message}");
                LogMessage.Error("[遥信TCP] 发送消息异常", ex);
            }
        }
        string MCUStartByte = "55";
        string TerminalDataLength = string.Empty;
        string MCUCtrl = "00";//控制协议
        string MCUTransparent = "01";//透传协议
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
            if (!await _pulseCommandGate.WaitAsync(0))
            {
                PublishLog("脉冲命令正在发送，请勿重复点击。");
                return;
            }

            try
            {
                if (!TryReadPulseParameters(out long pulseCount, out double durationMinutes, out float frequency))
                    return;

                MCUAddr = tbx_YXAddr.Text.Trim();
                string frequencyHex = IEEE754Converter.FloatToHex(frequency);
                string countHex = ModelTool.Ensure4Bytes(ModelTool.ToHex(pulseCount), 4);
                bool start = !_pulseOutputRunning;
                string actionHex = start ? "01" : "00";

                label9.Text = "频率16进制：" + frequencyHex;
                label10.Text = "脉冲个数16进制：" + countHex;

                PublishLog(start
                    ? $"开始发送双通道脉冲：频率={frequency:F6}Hz，脉冲数={pulseCount}。"
                    : "开始停止双通道脉冲输出。");

                string channelOneFrame = BuildPulseFrame("01", actionHex, frequencyHex, countHex, includeDutyCycle: start);
                string channelTwoFrame = BuildPulseFrame("02", actionHex, frequencyHex, countHex, includeDutyCycle: start);

                await SendMCUToPC(channelOneFrame);
                await Task.Delay(500);
                await SendMCUToPC(channelTwoFrame);

                _pulseOutputRunning = start;
                btnStartMC.Text = start ? "停止脉冲" : "启动脉冲";
                PublishLog(start ? "双通道脉冲启动命令发送完成。" : "双通道脉冲停止命令发送完成。");
            }
            catch (Exception ex)
            {
                PublishLog($"脉冲命令处理失败：{ex.Message}");
                LogMessage.Error("[遥信] 脉冲命令处理失败", ex);
            }
            finally
            {
                _pulseCommandGate.Release();
            }
        }

        /// <summary>
        /// 读取并校验脉冲测试参数，避免空值、零时间和非法数值进入协议转换。
        /// </summary>
        private bool TryReadPulseParameters(out long pulseCount, out double durationMinutes, out float frequency)
        {
            pulseCount = 0;
            durationMinutes = 0;
            frequency = 0;

            if (!long.TryParse(tbxMCCounts.Text.Trim(), out pulseCount) || pulseCount <= 0)
            {
                PublishLog("脉冲参数错误：脉冲个数必须是大于0的整数。");
                return false;
            }

            if (!double.TryParse(tbxMCTime.Text.Trim(), out durationMinutes) || durationMinutes <= 0)
            {
                PublishLog("脉冲参数错误：时间必须是大于0的分钟数。");
                return false;
            }

            frequency = (float)(pulseCount / (durationMinutes * 60d));
            if (!float.IsFinite(frequency) || frequency <= 0)
            {
                PublishLog("脉冲参数错误：计算出的频率无效。");
                return false;
            }

            tbxMCHZ.Text = frequency.ToString("G9");
            return true;
        }

        /// <summary>
        /// 生成遥信脉冲命令。启动命令包含占空比字节，停止命令按协议只保留基础参数。
        /// </summary>
        private string BuildPulseFrame(
            string channel,
            string action,
            string frequencyHex,
            string countHex,
            bool includeDutyCycle)
        {
            string data = channel + action + frequencyHex + countHex;
            if (includeDutyCycle)
                data += "32";

            TerminalDataLength = HexConverter.ConvertHex(ModelTool.ToHex(2 + 3 + 11 + 1));
            return TerminalModel.TerminalByte(
                MCUStartByte,
                TerminalDataLength + "00",
                tbx_YXAddr.Text.Trim(),
                MCUCtrl,
                "05",
                data,
                MCUStopByte);
        }

        /// <summary>
        /// 切换232 485-3 485-4
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnChange232485_Click(object sender, EventArgs e)
        {
            string selection = cbxChange232And485.Text.Trim();
            string port;
            string mode;

            if (selection.Contains("485-3", StringComparison.OrdinalIgnoreCase))
            {
                port = "03";
                mode = selection.Contains("切换到232", StringComparison.OrdinalIgnoreCase) ? "01" : "00";
            }
            else if (selection.Contains("485-4", StringComparison.OrdinalIgnoreCase))
            {
                port = "04";
                mode = selection.Contains("切换到232", StringComparison.OrdinalIgnoreCase) ? "01" : "00";
            }
            else
            {
                PublishLog("协议切换失败：请选择485-3或485-4的切换方向。");
                return;
            }

            await SendTerminalProtocolCommandAsync("BD", port + mode, $"终端RS485/RS232切换：{selection}");
        }
        /// <summary>
        /// 切换485和can通道
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnChangeCAN485_Click(object sender, EventArgs e)
        {
            if (cbxYXCan.Checked == cbxYX485.Checked)
            {
                PublishLog("协议切换失败：请仅选择485或CAN其中一种接口。");
                return;
            }

            string mode = cbxYXCan.Checked ? "01" : "00";
            string description = cbxYXCan.Checked ? "CAN" : "RS485";
            await SendTerminalProtocolCommandAsync("BE", mode, $"终端RS485/CAN切换：切换到{description}");
        }

        /// <summary>读取合闸1~4当前状态及变化状态，命令码0x09。</summary>
        private async void btnReadClosingState_Click(object sender, EventArgs e)
        {
            await SendTerminalProtocolCommandAsync("09", string.Empty, "读取合闸状态");
        }

        /// <summary>执行SOE站内分辨率测试，命令码0xC1无数据项。</summary>
        private async void btnSoeResolutionTest_Click(object sender, EventArgs e)
        {
            await SendTerminalProtocolCommandAsync("C1", string.Empty, "SOE站内分辨率测试");
        }

        /// <summary>设置遥信防抖脉宽并触发防抖测试，命令码0xC2，脉宽为低字节在前。</summary>
        private async void btnSetDebounceWidth_Click(object sender, EventArgs e)
        {
            if (!ushort.TryParse(tbxDebounceWidth.Text.Trim(), out ushort width))
            {
                PublishLog("防抖测试失败：脉宽必须是0~65535的整数。");
                return;
            }

            string data = $"{width & 0xFF:X2}{width >> 8:X2}";
            await SendTerminalProtocolCommandAsync("C2", data, $"设置遥信防抖脉宽：{width}ms");
        }

        /// <summary>启动遥信雪崩测试，时间和次数均按协议限制在60~255。</summary>
        private async void btnAvalancheTest_Click(object sender, EventArgs e)
        {
            if (!byte.TryParse(tbxAvalancheSeconds.Text.Trim(), out byte seconds) ||
                !byte.TryParse(tbxAvalancheCount.Text.Trim(), out byte count) ||
                seconds is < 60 or > 255 ||
                count is < 60 or > 255)
            {
                PublishLog("雪崩测试失败：时间和次数都必须在60~255之间。");
                return;
            }

            await SendTerminalProtocolCommandAsync(
                "C3",
                $"{seconds:X2}{count:X2}01",
                $"启动遥信雪崩测试：{seconds}秒，{count}次");
        }

        /// <summary>修改CAN波特率，协议数据为以1K为单位的两字节高字节在前。</summary>
        private async void btnChangeCanBaudRate_Click(object sender, EventArgs e)
        {
            var baudRates = new Dictionary<string, ushort>(StringComparer.OrdinalIgnoreCase)
            {
                ["50K"] = 50,
                ["100K"] = 100,
                ["125K"] = 125,
                ["250K"] = 250,
                ["500K"] = 500,
                ["1000K"] = 1000
            };

            string selected = cbxCanBaudRate.Text.Trim();
            if (!baudRates.TryGetValue(selected, out ushort baudRate))
            {
                PublishLog("CAN波特率修改失败：请选择50K、100K、125K、250K、500K或1000K。");
                return;
            }

            await SendTerminalProtocolCommandAsync(
                "BF",
                $"{baudRate >> 8:X2}{baudRate & 0xFF:X2}",
                $"修改CAN波特率：{selected}");
        }

        /// <summary>读取温湿度数据，命令码0x0C。</summary>
        private async void btnReadTemperatureHumidity_Click(object sender, EventArgs e)
        {
            await SendTerminalProtocolCommandAsync("0C", string.Empty, "读取温湿度");
        }

        /// <summary>统一发送新增遥信协议命令，集中记录命令说明、报文和发送异常。</summary>
        private async Task SendTerminalProtocolCommandAsync(string command, string data, string description)
        {
            try
            {
                string frame = BuildTerminalCommandFrame(command, data);
                PublishLog($"{description}，发送报文：{frame}");
                await SendMCUToPC(frame);
            }
            catch (Exception ex)
            {
                PublishLog($"{description}失败：{ex.Message}");
                LogMessage.Error($"[遥信协议] {description}失败", ex);
            }
        }

        /// <summary>
        /// 解析新增协议的关键响应字段。
        /// 其余命令仍保留原始HEX日志，避免在协议字段未完整定义时误判数据。
        /// </summary>
        private string ParseAdditionalProtocolResponse(byte[] rawData)
        {
            if (rawData is null || rawData.Length < 8 || rawData[0] != 0x55)
                return string.Empty;

            int commandIndex = 5;
            byte command = rawData[commandIndex];
            int dataStart = 6;
            int dataLength = rawData.Length - dataStart - 2;
            if (dataLength < 0)
                return string.Empty;

            return command switch
            {
                0x09 when dataLength >= 2 =>
                    ParseClosingState(rawData, dataStart, dataLength),
                0x0C when dataLength >= 4 =>
                    ParseTemperatureHumidity(rawData, dataStart, dataLength),
                _ => string.Empty
            };
        }

        /// <summary>解析0x09返回的合闸当前状态和变化状态。</summary>
        private static string ParseClosingState(byte[] data, int start, int length)
        {
            byte current = data[start];
            byte changed = data[start + 1];
            string currentText = DescribeSwitchBits(current);
            string changedText = DescribeSwitchBits(changed);

            if (length >= 4)
            {
                byte mode = data[start + 2];
                byte modeState = data[start + 3];
                return $"合闸状态：当前={currentText}，变化={changedText}，控制模式={(mode == 0 ? "电平" : "脉冲")}，合闸1~8状态={DescribeSwitchBits(modeState)}。";
            }

            return $"合闸状态：当前={currentText}，变化={changedText}。";
        }

        /// <summary>解析0x0C返回的温湿度浮点数据；不足8字节时只提示温度原始值。</summary>
        private static string ParseTemperatureHumidity(byte[] data, int start, int length)
        {
            float temperature = BitConverter.ToSingle(data, start);
            if (length >= 8)
            {
                float humidity = BitConverter.ToSingle(data, start + 4);
                return $"温湿度：温度={temperature:F2}，湿度={humidity:F2}。";
            }

            return $"温湿度：温度={temperature:F2}，湿度数据长度不足，原始数据={BitConverter.ToString(data, start, length).Replace("-", " ")}。";
        }

        /// <summary>将低4位开关状态转换为可读文本。</summary>
        private static string DescribeSwitchBits(byte value)
        {
            return $"1={((value & 0x01) != 0 ? "闭合" : "断开")}，" +
                   $"2={((value & 0x02) != 0 ? "闭合" : "断开")}，" +
                   $"3={((value & 0x04) != 0 ? "闭合" : "断开")}，" +
                   $"4={((value & 0x08) != 0 ? "闭合" : "断开")}";
        }

        /// <summary>
        /// 生成终端V1控制协议帧：
        /// 55 + 长度 + 方向00 + 地址 + 协议类型00 + 命令码 + 数据项 + 校验和 + AA。
        /// 长度和校验和均按协议示例计算，地址沿用当前遥信地址。
        /// </summary>
        private string BuildTerminalCommandFrame(string command, string data)
        {
            string address = AddressToHexChange.MeassageAddr(tbx_YXAddr.Text.Trim());
            if (!byte.TryParse(command, System.Globalization.NumberStyles.HexNumber, null, out byte commandByte))
                throw new ArgumentException($"命令码无效：{command}");

            if (!string.IsNullOrWhiteSpace(data) && (!IsEvenHex(data) || data.Length % 2 != 0))
                throw new ArgumentException($"数据项不是有效偶数位HEX：{data}");

            byte[] dataBytes = string.IsNullOrWhiteSpace(data)
                ? Array.Empty<byte>()
                : ModelTool.HexStringToByteArray(data);

            if (!byte.TryParse(address, System.Globalization.NumberStyles.HexNumber, null, out byte addressByte))
                throw new ArgumentException($"终端地址无效：{tbx_YXAddr.Text}");

            int length = 1 + 4 + dataBytes.Length + 1;
            if (length > byte.MaxValue)
                throw new ArgumentException("终端协议数据长度超出单字节范围。");

            var frame = new List<byte>
            {
                0x55,
                (byte)length,
                0x00,
                addressByte,
                0x00,
                commandByte
            };
            frame.AddRange(dataBytes);

            byte checksum = 0;
            for (int i = 1; i < frame.Count; i++)
                checksum = unchecked((byte)(checksum + frame[i]));

            frame.Add(checksum);
            frame.Add(0xAA);
            return string.Join(" ", frame.Select(value => value.ToString("X2")));
        }

        /// <summary>
        /// 校验协议数据项是否只包含偶数位十六进制字符。
        /// </summary>
        private static bool IsEvenHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length % 2 != 0)
                return false;

            return value.All(Uri.IsHexDigit);
        }
    }
}
