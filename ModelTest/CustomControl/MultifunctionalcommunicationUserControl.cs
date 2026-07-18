using System.Net;
using System.Net.Sockets;
using System.Text;
using ModelTest.Socket_DLL;
using ModelTest.Socket_DLL.Socket_Client;

namespace ModelTest.CustomControl
{
    /// <summary>
    /// 多功能 TCP 通信控件，负责 TCP Server/TCP Client 的连接、收发和接收日志展示。
    /// 发送结果通过事件回传给主界面日志区，避免通信页和主界面日志逻辑耦合。
    /// </summary>
    public sealed partial class MultifunctionalcommunicationUserControl : UserControl
    {
        public delegate void UpdateMainFormDelegate(string message, Color? color = null);

        public event UpdateMainFormDelegate? OnUpdateRequestedMultifunctionalLog;

        private readonly Dictionary<string, string> _clientIdsByEndpoint = new();

        private Socket_DLL.Socket_DLL? _server;
        private EnhancedTcpClient? _tcpClient;
        private bool _isServerRunning;
        private bool _isClientConnected;
        private bool _isDisposing;
        private readonly Color _segmentCheckedBackColor = Color.FromArgb(242, 196, 55);
        private readonly Color _segmentCheckedHoverBackColor = Color.FromArgb(250, 206, 78);
        private readonly Color _segmentUncheckedBackColor = Color.FromArgb(104, 156, 137);
        private readonly Color _segmentUncheckedHoverBackColor = Color.FromArgb(118, 170, 150);
        private readonly Color _segmentCheckedForeColor = Color.FromArgb(37, 47, 41);
        private readonly Color _segmentUncheckedForeColor = Color.FromArgb(233, 240, 236);

        public MultifunctionalcommunicationUserControl()
        {
            InitializeComponent();
            BindEvents();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _isDisposing = true;
                DisconnectClient();
                StopServer();
            }

            base.Dispose(disposing);
        }
        private void BindEvents()
        {
            cbxSocketClass.SelectedIndexChanged += (_, _) =>
            {
                if (!_isClientConnected)
                {
                    ReleaseTcpClient();
                }

                ResetConnectButtonText();
            };
            cbxRevcASCII.CheckedChanged += (_, _) => BindExclusive(cbxRevcASCII, cbxRevcHEX);
            cbxRevcHEX.CheckedChanged += (_, _) => BindExclusive(cbxRevcHEX, cbxRevcASCII);
            cbxSendASCII.CheckedChanged += (_, _) => BindExclusive(cbxSendASCII, cbxSendHEX);
            cbxSendHEX.CheckedChanged += (_, _) => BindExclusive(cbxSendHEX, cbxSendASCII);

            btnConnect.Click += btnConnect_Click;
            btnSendData.Click += btnSendData_Click;
            btnClearReceive.Click += (_, _) => rtbxRevcData.Clear();
            btnClearSend.Click += (_, _) => rtbxSendData.Clear();
            UpdateFormatModeVisualState();
        }

        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            btnConnect.Enabled = false;
            try
            {
                if (_isClientConnected)
                {
                    DisconnectClient();
                    return;
                }

                if (_isServerRunning)
                {
                    StopServer();
                    return;
                }

                if (!TryGetEndpoint(out var ip, out var port))
                {
                    return;
                }

                if (IsClientMode())
                {
                    await ConnectTcpClientAsync(ip, port);
                }
                else
                {
                    await StartTcpServerAsync(ip, port);
                }
            }
            catch (Exception ex)
            {
                PublishLog($"TCP通信异常：{ex.Message}", Color.Red);
                LogMessage.Error(ex);
            }
            finally
            {
                btnConnect.Enabled = true;
            }
        }

        private async Task ConnectTcpClientAsync(string ip, int port)
        {
            ReleaseTcpClient();

            _tcpClient = new EnhancedTcpClient
            {
                // 当前工具页需要用户手动控制连接生命周期。
                // 自动重连会在切换到 TCPServer 后留下旧客户端回调，导致状态和端口占用判断混乱。
                EnableAutoReconnect = false,
            };
            _tcpClient.MessageReceived += TcpClient_MessageReceived;
            _tcpClient.MessageSent += TcpClient_MessageSent;
            _tcpClient.ConnectionStatusChanged += TcpClient_ConnectionStatusChanged;
            _tcpClient.ErrorOccurred += TcpClient_ErrorOccurred;

            var connected = await _tcpClient.ConnectAsync(ip, port);
            _isClientConnected = connected;

            if (connected)
            {
                btnConnect.Text = "断开TCP客户端";
                SetStatus($"TCP客户端已连接：{ip}:{port}", Color.Green);
                PublishLog($"TCP客户端连接成功：{ip}:{port}", Color.Green);
                return;
            }

            ReleaseTcpClient();
            SetStatus("TCP客户端未连接", Color.Red);
            PublishLog($"TCP客户端连接失败：{ip}:{port}", Color.Red);
            ResetConnectButtonText();
        }

        private async Task StartTcpServerAsync(string ip, int port)
        {
            ReleaseTcpClient();

            _server = new Socket_DLL.Socket_DLL(ip, port);
            _server.MessageReceived += TcpServer_MessageReceived;
            _server.ClientConnected += TcpServer_ClientConnected;
            _server.ClientDisconnected += TcpServer_ClientDisconnected;
            _server.ServerError += TcpServer_ServerError;
            _server.ServerStatusChanged += TcpServer_ServerStatusChanged;

            await _server.StartAsync();
            _isServerRunning = true;
            btnConnect.Text = "关闭TCP服务器";
            SetStatus($"TCP服务器已启动：{ip}:{port}", Color.Green);
            PublishLog($"启动TCP侦听服务器成功，监听IP：{ip}，端口：{port}", Color.Green);
        }

        private async void btnSendData_Click(object? sender, EventArgs e)
        {
            try
            {
                var message = rtbxSendData.Text.Trim();
                if (string.IsNullOrWhiteSpace(message))
                {
                    PublishLog("发送失败：发送内容不能为空", Color.Red);
                    return;
                }

                if (_isClientConnected)
                {
                    await SendClientMessageAsync(message);
                    return;
                }

                if (_isServerRunning)
                {
                    if (cbxIsBroadcastMessage.Checked)
                    {
                        await BroadcastServerMessageAsync(message);
                    }
                    else
                    {
                        await SendServerMessageAsync(cbxClientConnc.Text, message);
                    }
                    return;
                }

                PublishLog("发送失败：TCP客户端未连接或TCP服务器未启动", Color.Red);
            }
            catch (Exception ex)
            {
                PublishLog($"发送异常：{ex.Message}", Color.Red);
                LogMessage.Error(ex);
            }
        }

        private async Task SendClientMessageAsync(string message)
        {
            if (_tcpClient == null || !_tcpClient.IsConnected)
            {
                PublishLog("发送失败：TCP客户端未连接", Color.Red);
                return;
            }

            var outgoing = NormalizeOutgoingMessage(message, cbxSendASCII.Checked);
            var sent = cbxSendASCII.Checked
                ? await _tcpClient.SendAsync(outgoing.Message)
                : await _tcpClient.SendBytesAsync(ModelTool.HexStringToByteArray(outgoing.Message));

            PublishSendResult(sent, $"发送消息至服务器：{FormatSendMessage(outgoing.DisplayMessage)}");
        }

        private async Task SendServerMessageAsync(string selectedClientEndpoint, string message)
        {
            if (_server == null || !_server.IsRunning)
            {
                PublishLog("发送失败：TCP服务器未启动", Color.Red);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedClientEndpoint))
            {
                PublishLog("发送失败：请选择服务端客户端", Color.Red);
                return;
            }

            if (!_clientIdsByEndpoint.TryGetValue(selectedClientEndpoint, out var clientId))
            {
                PublishLog($"发送失败：未找到客户端 {selectedClientEndpoint}", Color.Red);
                UpdateClientList();
                return;
            }

            var asciiOrHex = cbxSendASCII.Checked;
            var outgoing = NormalizeOutgoingMessage(message, asciiOrHex);
            var success = await _server.SendAsync(clientId, outgoing.Message, asciiOrHex);
            PublishSendResult(success, $"发送消息至客户端 {selectedClientEndpoint}：{FormatSendMessage(outgoing.DisplayMessage)}");
        }

        private async Task BroadcastServerMessageAsync(string message)
        {
            if (_server == null || !_server.IsRunning)
            {
                PublishLog("广播失败：TCP服务器未启动", Color.Red);
                return;
            }

            var asciiOrHex = cbxSendASCII.Checked;
            var outgoing = NormalizeOutgoingMessage(message, asciiOrHex);
            var success = await _server.BroadcastAsync(outgoing.Message, asciiOrHex);
            PublishSendResult(success, $"广播消息：{FormatSendMessage(outgoing.DisplayMessage)}");
        }

        private void TcpClient_MessageReceived(object sender, TcpClientMessageEventArgs e)
        {
            UpdateUi(() =>
            {
                var display = cbxRevcASCII.Checked
                    ? Encoding.ASCII.GetString(e.RawData)
                    : BitConverter.ToString(e.RawData).Replace("-", " ");

                AppendReceiveLog($"[{e.Timestamp:HH:mm:ss}] 服务器 -> 客户端：{display}");
                LogMessage.SocketLog($"接受消息<-- 服务器 的数据: {display}");
            });
        }

        private void TcpClient_MessageSent(object sender, TcpClientMessageEventArgs e)
        {
            if (e.Message.Contains("文件传输进度") || e.Message.Contains("FILE_"))
            {
                PublishLog($"TCP客户端发送：{e.Message}", Color.DarkGreen);
            }
        }

        private void TcpClient_ConnectionStatusChanged(object sender, TcpClientStatusEventArgs e)
        {
            UpdateUi(() =>
            {
                _isClientConnected = e.IsConnected;
                SetStatus(e.IsConnected ? $"TCP客户端已连接：{e.Status}" : $"TCP客户端已断开：{e.Status}",
                    e.IsConnected ? Color.Green : Color.Red);
                if (!e.IsConnected)
                {
                    ResetConnectButtonText();
                }
                PublishLog($"TCP客户端状态：{e.Status}", e.IsConnected ? Color.Green : Color.Red);
            });
        }

        private void TcpClient_ErrorOccurred(object sender, string errorMessage)
        {
            PublishLog($"TCP客户端错误：{errorMessage}", Color.Red);
        }

        private void TcpServer_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            UpdateUi(() =>
            {
                UpdateClientList();
                var display = cbxRevcASCII.Checked
                    ? e.Message
                    : BitConverter.ToString(e.RawData).Replace("-", " ");

                AppendReceiveLog($"[{e.ReceivedTime:HH:mm:ss}] 客户端 {e.ClientEndpoint} -> 服务器：{display}");
            });
        }

        private void TcpServer_ClientConnected(object sender, ClientStatusChangedEventArgs e)
        {
            UpdateUi(() =>
            {
                UpdateClientList();
                PublishLog($"[{e.ChangeTime:HH:mm:ss}] TCP客户端接入：{e.ClientEndpoint}", Color.Green);
            });
        }

        private void TcpServer_ClientDisconnected(object sender, ClientStatusChangedEventArgs e)
        {
            UpdateUi(() =>
            {
                UpdateClientList();
                PublishLog($"[{e.ChangeTime:HH:mm:ss}] TCP客户端断开：{e.ClientEndpoint}", Color.Red);
            });
        }

        private void TcpServer_ServerError(object? sender, string error)
        {
            PublishLog($"TCP服务器错误：{error}", Color.Red);
        }

        private void TcpServer_ServerStatusChanged(object? sender, string statusMessage)
        {
            PublishLog($"TCP服务器状态：{statusMessage}", Color.DarkGreen);
        }

        private void UpdateClientList()
        {
            string selectedEndpoint = cbxClientConnc.Text;
            cbxClientConnc.Items.Clear();
            _clientIdsByEndpoint.Clear();
            if (_server == null)
            {
                return;
            }

            foreach (var clientInfo in _server.GetAllClientInfos())
            {
                _clientIdsByEndpoint[clientInfo.Endpoint] = clientInfo.Id;
                cbxClientConnc.Items.Add(clientInfo.Endpoint);
            }

            if (cbxClientConnc.Items.Contains(selectedEndpoint))
            {
                cbxClientConnc.SelectedItem = selectedEndpoint;
            }
            else if (cbxClientConnc.Items.Count > 0 && cbxClientConnc.SelectedIndex < 0)
            {
                cbxClientConnc.SelectedIndex = 0;
            }
        }

        private bool TryGetEndpoint(out string ip, out int port)
        {
            ip = cbxIp.Text.Trim();
            port = 0;

            if (string.IsNullOrWhiteSpace(ip) || !IPAddress.TryParse(ip, out _))
            {
                PublishLog("IP格式不正确", Color.Red);
                return false;
            }

            if (!int.TryParse(cbxPort.Text.Trim(), out port) || port < 1 || port > 65535)
            {
                PublishLog("端口号输入不正确，请输入1-65535之间的数字", Color.Red);
                return false;
            }

            return true;
        }

        private void DisconnectClient()
        {
            if (_tcpClient == null)
            {
                _isClientConnected = false;
                ResetConnectButtonText();
                return;
            }

            ReleaseTcpClient();

            SetStatus("TCP客户端已断开", Color.Red);
            PublishLog("TCP客户端已断开", Color.Red);
            ResetConnectButtonText();
        }

        /// <summary>
        /// 释放 TCP Client 并解除所有事件绑定。
        /// 用于连接失败、模式切换、启动服务端和控件释放，避免旧客户端重连回调影响当前模式。
        /// </summary>
        private void ReleaseTcpClient()
        {
            if (_tcpClient == null)
            {
                _isClientConnected = false;
                return;
            }

            _tcpClient.MessageReceived -= TcpClient_MessageReceived;
            _tcpClient.MessageSent -= TcpClient_MessageSent;
            _tcpClient.ConnectionStatusChanged -= TcpClient_ConnectionStatusChanged;
            _tcpClient.ErrorOccurred -= TcpClient_ErrorOccurred;
            _tcpClient.EnableAutoReconnect = false;
            _tcpClient.Disconnect();
            _tcpClient.Dispose();
            _tcpClient = null;
            _isClientConnected = false;
        }

        private void StopServer()
        {
            if (_server == null)
            {
                _isServerRunning = false;
                ResetConnectButtonText();
                return;
            }

            _server.MessageReceived -= TcpServer_MessageReceived;
            _server.ClientConnected -= TcpServer_ClientConnected;
            _server.ClientDisconnected -= TcpServer_ClientDisconnected;
            _server.ServerError -= TcpServer_ServerError;
            _server.ServerStatusChanged -= TcpServer_ServerStatusChanged;
            _server.Stop();
            _server.Dispose();
            _server = null;
            _clientIdsByEndpoint.Clear();
            cbxClientConnc.Items.Clear();
            _isServerRunning = false;

            SetStatus("TCP服务器已关闭", Color.Red);
            PublishLog("TCP侦听服务器已关闭", Color.Red);
            ResetConnectButtonText();
        }

        private void ResetConnectButtonText()
        {
            if (_isClientConnected || _isServerRunning)
            {
                return;
            }

            btnConnect.Text = IsClientMode() ? "连接TCP服务器" : "启动TCP服务器";
        }

        private bool IsClientMode()
        {
            return cbxSocketClass.Text == "TCPClient";
        }

        private void BindExclusive(CheckBox current, CheckBox other)
        {
            if (current.Checked)
            {
                other.Checked = false;
                UpdateFormatModeVisualState();
                return;
            }

            if (!other.Checked)
            {
                current.Checked = true;
            }

            UpdateFormatModeVisualState();
        }

        /// <summary>
        /// 刷新 ASCII/HEX 分段按钮的视觉状态，和加密机控件的 segmented control 保持同一套配色。
        /// </summary>
        private void UpdateFormatModeVisualState()
        {
            ApplySegmentedCheckBoxStyle(cbxRevcASCII, cbxRevcASCII.Checked, isHovering: false);
            ApplySegmentedCheckBoxStyle(cbxRevcHEX, cbxRevcHEX.Checked, isHovering: false);
            ApplySegmentedCheckBoxStyle(cbxSendASCII, cbxSendASCII.Checked, isHovering: false);
            ApplySegmentedCheckBoxStyle(cbxSendHEX, cbxSendHEX.Checked, isHovering: false);
        }

        /// <summary>
        /// 分段按钮统一配色：选中黄色实心，未选中浅绿，hover 时轻微提亮。
        /// </summary>
        private void ApplySegmentedCheckBoxStyle(CheckBox checkBox, bool isChecked, bool isHovering)
        {
            checkBox.BackColor = isChecked
                ? (isHovering ? _segmentCheckedHoverBackColor : _segmentCheckedBackColor)
                : (isHovering ? _segmentUncheckedHoverBackColor : _segmentUncheckedBackColor);
            checkBox.ForeColor = isChecked ? _segmentCheckedForeColor : _segmentUncheckedForeColor;
        }

        private void SegmentCheckBox_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                ApplySegmentedCheckBoxStyle(checkBox, checkBox.Checked, isHovering: true);
            }
        }

        private void SegmentCheckBox_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                ApplySegmentedCheckBoxStyle(checkBox, checkBox.Checked, isHovering: false);
            }
        }

        private void SetStatus(string text, Color color)
        {
            if (!CanTouchUi() || lblStatusValue.IsDisposed)
            {
                return;
            }

            lblStatusValue.Text = text;
            lblStatusValue.ForeColor = color;
        }

        private void AppendReceiveLog(string message)
        {
            if (!CanTouchUi() || rtbxRevcData.IsDisposed)
            {
                return;
            }

            rtbxRevcData.AppendText(message + Environment.NewLine);
            rtbxRevcData.ScrollToCaret();
        }

        private void PublishSendResult(bool success, string message)
        {
            var log = success ? $"{message} 成功" : $"{message} 失败";
            PublishLog(log, success ? Color.Green : Color.Red);
        }

        private void PublishLog(string message, Color? color = null)
        {
            if (_isDisposing || IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                if (!IsHandleCreated)
                {
                    return;
                }

                try
                {
                    BeginInvoke(new Action<string, Color?>(PublishLog), message, color);
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            OnUpdateRequestedMultifunctionalLog?.Invoke(message, color);
        }

        private string FormatSendMessage(string message)
        {
            return cbxSendASCII.Checked ? $"ASCII[{message}]" : $"HEX[{message}]";
        }

        /// <summary>
        /// 服务端内部客户端 ID 会附加时间戳和随机段，日志展示只保留 IP:Port。
        /// </summary>
        private static string FormatClientEndpointForLog(string clientId)
        {
            int separatorIndex = clientId.IndexOf('_');
            return separatorIndex > 0 ? clientId[..separatorIndex] : clientId;
        }

        /// <summary>
        /// 按当前发送模式规范化用户输入。
        /// HEX 模式下如果输入不是合法 HEX，则把普通字符按 ASCII 字节转为 HEX；
        /// ASCII 模式下如果输入是合法 HEX，则把 HEX 字节转为 ASCII 字符。
        /// </summary>
        private static (string Message, string DisplayMessage) NormalizeOutgoingMessage(string message, bool sendAscii)
        {
            string trimmed = message.Trim();
            if (sendAscii)
            {
                if (TryParseHex(trimmed, out byte[] bytes))
                {
                    string ascii = Encoding.ASCII.GetString(bytes);
                    return (ascii, ascii);
                }

                return (trimmed, trimmed);
            }

            if (TryParseHex(trimmed, out byte[] hexBytes))
            {
                string normalizedHex = ToSpacedHex(hexBytes);
                return (normalizedHex, normalizedHex);
            }

            string convertedHex = ToSpacedHex(Encoding.ASCII.GetBytes(trimmed));
            return (convertedHex, convertedHex);
        }

        private static bool TryParseHex(string input, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            string compactHex = NormalizeHexText(input);
            if (compactHex.Length == 0 || compactHex.Length % 2 != 0)
            {
                return false;
            }

            if (compactHex.Any(c => !Uri.IsHexDigit(c)))
            {
                return false;
            }

            try
            {
                bytes = ModelTool.HexStringToByteArray(compactHex);
                return true;
            }
            catch
            {
                bytes = Array.Empty<byte>();
                return false;
            }
        }

        private static string NormalizeHexText(string input)
        {
            return input
                .Replace("0x", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace(" ", string.Empty)
                .Replace("\t", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("-", string.Empty);
        }

        private static string ToSpacedHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", " ");
        }

        private void UpdateUi(Action action)
        {
            if (_isDisposing || IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                if (!IsHandleCreated)
                {
                    return;
                }

                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (CanTouchUi())
                        {
                            action();
                        }
                    }));
                }
                catch (ObjectDisposedException)
                {
                }

                return;
            }

            if (CanTouchUi())
            {
                action();
            }
        }

        private bool CanTouchUi()
        {
            return !_isDisposing && !IsDisposed && IsHandleCreated;
        }

        private static object[] GetLocalIPv4Addresses()
        {
            try
            {
                return Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList
                    .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString())
                    .Cast<object>()
                    .ToArray();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }
    }
}
