using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using ModelTest.Socket_DLL.Socket_Client;
using ModelTest.Socket_DLL.Socket_Client.TCPClientManner;
using ModelTest.Socket_DLL.Socket_UDP;
using ModelTest.Tools;
using static System.Windows.Forms.AxHost;

namespace ModelTest.CustomControl
{
    public partial class UDPMessageUserControl : UserControl
    {
        // 定义一个委托，用于调用主窗体方法
        public delegate void UpdateMainFormDelegate(string message);
        // 事件，让主窗体订阅
        public event UpdateMainFormDelegate? OnUpdateRequested_UDPMessage;
        private static readonly string configpath = Path.Combine(Application.StartupPath, "XCKJcomfig.ini");
        private const string UdpMessageConfigSection = "UDPMessage";
        private const string StationCountConfigKey = "BWNumbers";
        private const string UdpServerListenSection = "UDPServerListen";
        private const string UdpListenIpKey = "udpip";
        private const string DefaultUdpListenIp = "127.0.0.1";
        private OptimizedUdpServer? _udpServer;
        private readonly SynchronizationContext _uiContext;
        private readonly TcpConnectionPool connectionPool;
        private System.Timers.Timer? _logTimer;
        private bool _autoScrollLog = true;
        private static readonly (string Section, string KeyPrefix)[] DefaultTcpConfigKeys =
        [
            ("232", "232_No_"),
            ("485-1", "485_1No_"),
            ("485-2", "485_2No_"),
            ("PCB", "xckjpcbCtrl_")
        ];


        public UDPMessageUserControl()
        {
            InitializeComponent();
            LoadStationCountConfig();
            EnsureDefaultConfig();
            NUDBWNumbers.ValueChanged += NUDBWNumbers_ValueChanged;
            this.BackColor = Color.FromArgb(88, 149, 127);
            //InitializeUdpServer(10001);
            btnUDPServerListen.Enabled = true;
            btnUDPServerListenColse.Enabled = false;
            _uiContext = SynchronizationContext.Current ?? new SynchronizationContext();
            connectionPool = TcpConnectionPool.Instance;
            EnsureEventsSubscribed(connectionPool.Manager);
            ProtocolHelper.RegisterMessageSender(SendDataAsync);
        }
        #region 事件处理

        private void EnsureDefaultConfig()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configpath) ?? Application.StartupPath);

            WriteStationCountConfig();
            EnsureUdpListenConfig();

            int stationCount = GetStationCount();
            foreach ((string section, string keyPrefix) in DefaultTcpConfigKeys)
            {
                for (int i = 1; i <= stationCount; i++)
                {
                    string key = $"{keyPrefix}{i}";
                    string currentValue = Confighelper.ReadIni(section, key, "", 255, configpath);
                    if (string.IsNullOrEmpty(currentValue))
                    {
                        Confighelper.WriteIni(section, key, "", configpath);
                    }
                }
            }
        }

        private void LoadStationCountConfig()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configpath) ?? Application.StartupPath);

            string configuredValue = Confighelper
                .ReadIni(UdpMessageConfigSection, StationCountConfigKey, "", 255, configpath)
                .Trim();

            if (int.TryParse(configuredValue, out int stationCount))
            {
                decimal minValue = NUDBWNumbers.Minimum;
                decimal maxValue = NUDBWNumbers.Maximum;
                NUDBWNumbers.Value = Math.Min(maxValue, Math.Max(minValue, stationCount));
                return;
            }

            WriteStationCountConfig();
        }

        private void WriteStationCountConfig()
        {
            Confighelper.WriteIni(
                UdpMessageConfigSection,
                StationCountConfigKey,
                GetStationCount().ToString(),
                configpath);
        }

        private int GetStationCount()
        {
            return Math.Max(1, (int)NUDBWNumbers.Value);
        }

        private void NUDBWNumbers_ValueChanged(object? sender, EventArgs e)
        {
            EnsureDefaultConfig();
        }

        private string GetUdpListenIp()
        {
            string ip = Confighelper
                .ReadIni(UdpServerListenSection, UdpListenIpKey, "", 255, configpath)
                .Trim();

            if (!string.IsNullOrWhiteSpace(ip))
            {
                return ip;
            }

            EnsureUdpListenConfig();
            return DefaultUdpListenIp;
        }

        private static void EnsureUdpListenConfig()
        {
            string ip = Confighelper
                .ReadIni(UdpServerListenSection, UdpListenIpKey, "", 255, configpath)
                .Trim();

            if (string.IsNullOrWhiteSpace(ip))
            {
                Confighelper.WriteIni(UdpServerListenSection, UdpListenIpKey, DefaultUdpListenIp, configpath);
            }
        }


        /// <summary>
        /// 开启监听udp端口服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnUDPServerListen_Click(object sender, EventArgs e)
        {
            string _ip = GetUdpListenIp();
            int _udpPort = int.Parse(tbx_UDPServerport.Text.Trim());//udp 端口
            InitializeUdpServer(_ip, _udpPort);
            connectionPool.Manager.EnableAutoReconnect = true;
            connectionPool.Manager.EnableHeartbeat = true;

            // 更新配置
            if (_udpServer == null)
            {
                OnUpdateRequested_UDPMessage?.Invoke("UDP服务初始化失败");
                return;
            }

            _udpServer.ClientTimeout = int.Parse(txtTimeout.Text.Trim()) * 1000; // 客户端超时时间（秒）
            _udpServer.EnableAutoCleanup = chkAutoCleanup.Checked; // 是否启用自动清理断开客户端
            _udpServer.EnableBroadcast = chkBroadcast.Checked;//是否启用广播
            bool started = await _udpServer.StartAsync();

            UpdateUI(() =>
            {
                if (started)
                {
                    btnUDPServerListen.Enabled = false;
                    btnUDPServerListenColse.Enabled = true;
                    OnUpdateRequested_UDPMessage?.Invoke($"UDP服务已启动，IP: {_ip}端口: {_udpPort}");
                }
            });
            if (started)
            {
                LogMessage.Info("UDP启动成功，跳过启动时自动打开源串口，改为按需打开");
            }

            //链接串口服务器方法 485-1 485-2建立方法
            tbxTCPClientManner.Clear();
            foreach ((string section, string keyPrefix) in DefaultTcpConfigKeys)
            {
                InitTCPClientServer(section, keyPrefix);
            }

        }
        /// <summary>
        /// 链接串口服务器的方法
        /// </summary>


        private async void InitTCPClientServer(string configheader, string configtitle)
        {
            int BWNumber = GetStationCount();
            var connections = new List<(string ip, int port, string name)>();
            for (int i = 1; i <= BWNumber; i++)
            {

                string Sgcciport = Confighelper.ReadIni($"{configheader}", $"{configtitle}{i}", "", 255, configpath);
                if (string.IsNullOrEmpty(Sgcciport))
                    continue;
                OnUpdateRequested_UDPMessage?.Invoke($"读取配置: {Sgcciport}");
                // await Task.Delay(200);延迟等待200ms

                tbxTCPClientManner.AppendText(Sgcciport + "\r\n");
                // 解析地址
                var endpointParts = Sgcciport.Trim().Split(':');
                if (endpointParts.Length == 2 && int.TryParse(endpointParts[1], out int port))
                {
                    string ip = endpointParts[0];
                    string name = $"{configtitle}{i}";
                    connections.Add((ip, port, name));
                }
            }
            if (connections.Any())
            {
                OnUpdateRequested_UDPMessage?.Invoke($"准备创建 {connections.Count} 个连接（自动去重）...");
                var connectionIds = await connectionPool.AddConnectionsBatchAsync(connections);

                OnUpdateRequested_UDPMessage?.Invoke($"成功创建 {connectionIds.Count} 个连接");
                OnUpdateRequested_UDPMessage?.Invoke($"当前总连接数: {connectionPool.Manager.ConnectionCount}");
                // 显示所有连接状态
                foreach (var conn in connectionPool.Manager.GetAllConnectionInfos())
                {
                    string status = conn.IsConnected ? "✓" : "✗";
                    OnUpdateRequested_UDPMessage?.Invoke($"  {status} {conn.Name}: {conn.ServerIp}:{conn.ServerPort} - {conn.Status}");
                }
            }
            else
            {
                OnUpdateRequested_UDPMessage?.Invoke("没有有效的连接配置");
            }
        }
        private bool _eventsSubscribed = false;

        private void EnsureEventsSubscribed(BatchTcpClientManager manager)
        {
            if (!_eventsSubscribed)
            {
                manager.MessageReceived += OnMessageReceived;
                manager.ConnectionStatusChanged += OnConnectionStatusChanged;
                manager.ErrorOccurred += OnErrorOccurred;
                _eventsSubscribed = true;
            }
        }
        /// <summary>
        /// 485通信
        /// </summary>
        /// <param name="meterPos"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        public async Task<(bool Success, string CallResult)> BtnSend485_2DataAsync(int meterPos, string msg)
        {
            return await SendDataAsync(null, meterPos, 5, msg);
        }

        public async Task<(bool Success, string CallResult)> SendDataAsync(string? udpClientId, int meterPos, int communicationMethod, string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
            {
                return (false, "消息不能为空");
            }

            if (meterPos <= 0)
            {
                return (false, "表位号必须大于0");
            }

            string? connectionName = GetConnectionName(communicationMethod, meterPos);
            if (string.IsNullOrEmpty(connectionName))
            {
                return (false, $"不支持的通信方式: {communicationMethod}");
            }

            var connection = connectionPool.GetConnectionByName(connectionName);
            if (connection == null)
            {
                return (false, $"未找到配置连接: {connectionName}");
            }

            bool success = await connectionPool.SendHexToServerByNameAsync(connectionName, msg);

            if (success)
            {
                string resultText = $"发送到 {connectionName} ({connection.Endpoint}): {msg}";
                OnUpdateRequested_UDPMessage?.Invoke(resultText);
                return (true, resultText);
            }

            string failText = $"发送失败: {connectionName} ({connection.Endpoint})";
            OnUpdateRequested_UDPMessage?.Invoke(failText);
            return (false, failText);
        }

        private static string? GetConnectionName(int communicationMethod, int meterPos)
        {
            return communicationMethod switch
            {
                0 => $"232_No_{meterPos}",
                1 => $"485_1No_{meterPos}",
                5 => $"485_2No_{meterPos}",
                _ => null
            };
        }
        private void OnErrorOccurred(object? sender, (string ConnectionId, string Error) error)
        {
            _uiContext.Post(_ =>
            {
                OnUpdateRequested_UDPMessage?.Invoke($"[错误] [{error.ConnectionId}] {error.Error}");
            }, null);
        }
        private void OnConnectionStatusChanged(object? sender, TcpConnectionEventArgs e)
        {
            var server = connectionPool.GetAllConnectionInfos()
            .FirstOrDefault(s => s.ConnectionId == e.ConnectionId);
            _uiContext.Post(_ =>
            {
                string serverName = server?.Name ?? e.ConnectionId;
                string status = e.IsConnected ? "已连接" : "已断开";


                //string statusText = e.IsConnected ? "连接成功" : $"断开: {e.Status}";
                Color color = e.IsConnected ? Color.LightGreen : Color.Orange;
                OnUpdateRequested_UDPMessage?.Invoke($"[状态] {serverName}: {status} - {e.Status}");
                // OnUpdateRequested_UDPMessage?.Invoke($"[{e.Timestamp:HH:mm:ss}] [{e.ConnectionId}] {statusText}");
            }, null);
            //UpdateComboBox();
        }

        private async void OnMessageReceived(object? sender, TcpClientMessageEventArgs e)
        {
            // 获取连接信息
            var server = connectionPool.GetAllConnectionInfos()
                .FirstOrDefault(s => s.ConnectionId == e.ConnectionId);
            string serverName = server?.Name ?? e.ConnectionId;
            string hexMessage = NormalizeServerHexPayload(e.RawData);
            _uiContext.Post(_ =>
            {
                OnUpdateRequested_UDPMessage?.Invoke($"[收到] {serverName}: {hexMessage}");
            }, null);

            if ((serverName.StartsWith("485_1No_") || serverName.StartsWith("485_2No_")) && _udpServer != null)
            {
                string meterPos = serverName.Split('_').LastOrDefault() ?? string.Empty;
                string cmd = serverName.StartsWith("485_1No_") ? "1002" : "1001";
                string forwardMessage = cmd == "1002" ? Trim645Frame(hexMessage) : hexMessage;
                string ret = cmd == "1002"
                    ? $"cmd=1002,data={meterPos};{forwardMessage}"
                    : ProtocolHelper.BuildReturnData("1001", 0, new List<string> { $"{meterPos};{forwardMessage}" });
                int forwardedCount = await _udpServer.BroadcastAsync(ret);
                LogMessage.Info($"{cmd}通道数据已广播到上层客户端，来源={serverName}，客户端数={forwardedCount}");
            }
        }

        private static string Trim645Frame(string hexMessage)
        {
            if (string.IsNullOrWhiteSpace(hexMessage))
            {
                return string.Empty;
            }

            string compactHex = hexMessage
                .Replace(" ", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .ToUpperInvariant();

            int startIndex = compactHex.IndexOf("68", StringComparison.Ordinal);
            int endIndex = compactHex.LastIndexOf("16", StringComparison.Ordinal);
            if (startIndex < 0 || endIndex <= startIndex || endIndex % 2 != 0)
            {
                return compactHex;
            }

            return compactHex.Substring(startIndex, endIndex - startIndex + 2);
        }

        private static string NormalizeServerHexPayload(byte[]? rawData)
        {
            byte[] bytes = rawData ?? Array.Empty<byte>();
            if (bytes.Length == 0)
            {
                return string.Empty;
            }

            string asciiText = Encoding.ASCII.GetString(bytes).Trim();
            string compactAscii = asciiText
                .Replace(" ", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty);

            if (!string.IsNullOrEmpty(compactAscii) &&
                compactAscii.Length % 2 == 0 &&
                compactAscii.All(Uri.IsHexDigit))
            {
                return compactAscii.ToUpperInvariant();
            }

            return ModelTool.ByteArrayToHex(bytes);
        }

        /// <summary>
        /// 关闭监听udp端口服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUDPServerListenColse_Click(object sender, EventArgs e)
        {
            _udpServer?.Stop();
            connectionPool.StopAndClearAll();
            UpdateUI(() =>
            {
                btnUDPServerListen.Enabled = true;
                btnUDPServerListenColse.Enabled = false;
                tbxTCPClientManner.Clear();
                OnUpdateRequested_UDPMessage?.Invoke("UDP服务已停止，串口服务器连接已断开");

            });
        }

        private void InitializeUdpServer(string _ip, int _prot)
        {
            _udpServer = new OptimizedUdpServer(_ip, _prot);

            // 订阅事件
            _udpServer.MessageReceived += OnMessageReceived;
            _udpServer.MessageSent += OnMessageSent;
            _udpServer.ClientConnected += OnClientConnected;
            _udpServer.ClientDisconnected += OnClientDisconnected;
            _udpServer.ErrorOccurred += OnErrorOccurred;
            _udpServer.StatusChanged += OnStatusChanged;
        }
        private void OnMessageReceived(object? sender, UdpMessageEventArgs e)
        {
            //_logQueue.Enqueue($"[{e.Timestamp:HH:mm:ss.fff}] 来自 {e.ClientId}: {e.Message}");
            LogMessage.Debug($"[{e.Timestamp:HH:mm:ss.fff}] 来自 {e.ClientId}: {e.Message}");
            AppendLog($"[{e.Timestamp:HH:mm:ss.fff}] 来自 {e.ClientId}: {e.Message}", Color.White);
            UpdateStatsDisplay();

        }

        private void OnMessageSent(object? sender, UdpMessageEventArgs e)
        {
            LogMessage.Debug($"[{e.Timestamp:HH:mm:ss.fff}] 回复 {e.ClientId}: {e.Message}");
            AppendLog($"[{e.Timestamp:HH:mm:ss.fff}] 回复 {e.ClientId}: {e.Message}", Color.LightGreen);
            UpdateStatsDisplay();
        }

        private void OnClientConnected(object? sender, UdpClientEventArgs e)
        {
            UpdateUI(() =>
            {
                if (!lstClients.Items.Contains(e.ClientId))
                {
                    lstClients.Items.Add(e.ClientId);
                }
                OnUpdateRequested_UDPMessage?.Invoke($"客户端连接: {e.ClientId} ({e.ClientEndpoint})");
                UpdateStatsDisplay();
            });
        }

        private void OnClientDisconnected(object? sender, UdpClientEventArgs e)
        {
            UpdateUI(() =>
            {
                lstClients.Items.Remove(e.ClientId);
                OnUpdateRequested_UDPMessage?.Invoke($"客户端断开: {e.ClientId} - {e.Message}");
                UpdateStatsDisplay();
            });
        }

        private void OnErrorOccurred(object? sender, string errorMessage)
        {
            //_logQueue.Enqueue($"[错误] {errorMessage}");
            LogMessage.Error(new Exception(errorMessage));
            AppendLog($"[错误] {errorMessage}", Color.Red);
        }

        private void OnStatusChanged(object? sender, UdpServerStatus status)
        {
            UpdateUI(() =>
            {
                lblStatus.Text = $"UDP服务端 - {status} - 客户端数: {_udpServer?.ConnectedClients ?? 0}";
            });
        }
        #endregion

        #region UI辅助方法
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
        private void UpdateStatsDisplay()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateStatsDisplay));
                return;
            }

            if (_udpServer == null)
            {
                return;
            }

            labelServernum.Text = _udpServer.GetStatistics();

            // 更新窗口标题
            lblStatus.Text = $"UDP服务端 - {_udpServer.Status} - 客户端数: {_udpServer.ConnectedClients}";
        }
        private void AppendLog(string message, Color? color = null)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(message, color)));
                return;
            }

            if (color.HasValue)
            {
                tbxUDPMessageLog.SelectionStart = tbxUDPMessageLog.TextLength;
                tbxUDPMessageLog.SelectionLength = 0;
                tbxUDPMessageLog.SelectionColor = color.Value;
                tbxUDPMessageLog.AppendText(message + Environment.NewLine);
                tbxUDPMessageLog.SelectionColor = tbxUDPMessageLog.ForeColor;
            }
            else
            {
                tbxUDPMessageLog.AppendText(message + Environment.NewLine);
            }

            if (_autoScrollLog)
            {
                tbxUDPMessageLog.ScrollToCaret();
            }
        }
        #endregion


    }
}
