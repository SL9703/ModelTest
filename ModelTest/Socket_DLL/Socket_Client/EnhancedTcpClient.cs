using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ModelTest.Socket_DLL.Socket_Client
{
    // 事件委托
    public delegate void TcpClientMessageHandler(object sender, TcpClientMessageEventArgs e);
    public delegate void TcpClientStatusHandler(object sender, TcpClientStatusEventArgs e);
    public delegate void TcpClientErrorHandler(object sender, string errorMessage);
    public class EnhancedTcpClient
    {
        #region 常量定义
        private const int DEFAULT_RECEIVE_BUFFER_SIZE = 8192;
        private const int DEFAULT_SEND_BUFFER_SIZE = 8192;
        private const int DEFAULT_HEARTBEAT_INTERVAL = 30000; // 30秒
        private const int DEFAULT_RECONNECT_INTERVAL = 5000;  // 5秒
        private const int DEFAULT_RECEIVE_TIMEOUT = 30000;    // 30秒
        private const int DEFAULT_SEND_TIMEOUT = 30000;       // 30秒
        public const bool DEFAULT_IsHexOrAscii = false; // 确定展示hex数据还是ascii数据
        #endregion
        #region 事件
        public event TcpClientMessageHandler MessageReceived;
        public event TcpClientMessageHandler MessageSent;
        public event TcpClientStatusHandler ConnectionStatusChanged;
        public event TcpClientErrorHandler ErrorOccurred;
        public event EventHandler<long> BytesTransferred;
        #endregion

        #region 字段
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private CancellationTokenSource _receiveCts;
        private CancellationTokenSource _sendCts;
        private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);
        private readonly object _connectionLock = new object();

        // 定时器
        private System.Timers.Timer _heartbeatTimer;
        private System.Timers.Timer _reconnectTimer;
        private System.Timers.Timer _inactivityTimer;

        // 配置参数
        private string _serverIp;
        private int _serverPort;
        private Encoding _encoding = Encoding.UTF8;
        private bool _autoReconnect = true;
        private bool _enableHeartbeat = true;
        private int _maxReconnectAttempts = 10;

        // 状态跟踪
        private volatile bool _isConnecting;
        private DateTime _lastReceiveTime;
        private DateTime _lastSendTime;
        private int _reconnectAttempts;
        private long _totalBytesSent;
        private long _totalBytesReceived;
        #endregion

        #region 属性
        /// <summary>
        /// 客户端ID
        /// </summary>
        public string ClientId { get; private set; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// 服务器端点地址
        /// </summary>
        public string ServerEndpoint => $"{_serverIp}:{_serverPort}";

        /// <summary>
        /// 连接时间
        /// </summary>
        public DateTime ConnectionTime { get; private set; }

        /// <summary>
        /// 运行时间
        /// </summary>
        public TimeSpan Uptime => IsConnected ? DateTime.Now - ConnectionTime : TimeSpan.Zero;

        /// <summary>
        /// 最后活动时间
        /// </summary>
        public DateTime LastActivityTime => _lastReceiveTime > _lastSendTime ? _lastReceiveTime : _lastSendTime;

        /// <summary>
        /// 总发送字节数
        /// </summary>
        public long TotalBytesSent => _totalBytesSent;

        /// <summary>
        /// 总接收字节数
        /// </summary>
        public long TotalBytesReceived => _totalBytesReceived;

        /// <summary>
        /// 连接状态描述
        /// </summary>
        public string Status { get; private set; } = "Disconnected";

        /// <summary>
        /// 重连尝试次数
        /// </summary>
        public int ReconnectAttempts => _reconnectAttempts;

        // 配置属性
        public int ReceiveBufferSize { get; set; } = DEFAULT_RECEIVE_BUFFER_SIZE;
        public int SendBufferSize { get; set; } = DEFAULT_SEND_BUFFER_SIZE;
        public int ReceiveTimeout { get; set; } = DEFAULT_RECEIVE_TIMEOUT;
        public int SendTimeout { get; set; } = DEFAULT_SEND_TIMEOUT;
        public int HeartbeatInterval { get; set; } = DEFAULT_HEARTBEAT_INTERVAL;
        public int ReconnectInterval { get; set; } = DEFAULT_RECONNECT_INTERVAL;
        public int InactivityTimeout { get; set; } = 60000; // 60秒无活动超时
        public bool NoDelay { get; set; } = true;
        public bool EnableAutoReconnect { get; set; } = true;
        public bool EnableHeartbeat { get; set; } = true;
        public int MaxReconnectAttempts { get; set; } = 10;
        #endregion
        #region 构造函数
        /// <summary>
        /// 初始化TCP客户端
        /// </summary>
        /// <param name="clientId">客户端ID，如果为空则自动生成</param>
        public EnhancedTcpClient(string clientId = null)
        {
            ClientId = clientId ?? GenerateClientId();
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            _heartbeatTimer = new System.Timers.Timer();
            _heartbeatTimer.Elapsed += OnHeartbeatTimerElapsed;
            _heartbeatTimer.AutoReset = true;

            _reconnectTimer = new System.Timers.Timer();
            _reconnectTimer.Elapsed += OnReconnectTimerElapsed;
            _reconnectTimer.AutoReset = false;

            _inactivityTimer = new System.Timers.Timer();
            _inactivityTimer.Elapsed += OnInactivityTimerElapsed;
            _inactivityTimer.AutoReset = false;
        }

        private string GenerateClientId()
        {
            return $"{Environment.MachineName}_{Environment.UserName}_{Guid.NewGuid():N}".Substring(0, 32);
        }
        #endregion

        #region 连接管理
        /// <summary>
        /// 连接到服务器
        /// </summary>
        public async Task<bool> ConnectAsync(string serverIp, int serverPort)
        {
            if (_isConnecting)
                return false;

            lock (_connectionLock)
            {
                if (_isConnecting)
                    return false;
                _isConnecting = true;
            }

            try
            {
                _serverIp = serverIp;
                _serverPort = serverPort;
                Status = "Connecting";

                OnConnectionStatusChanged(new TcpClientStatusEventArgs
                {
                    IsConnected = false,
                    Status = $"正在连接服务器 {ServerEndpoint}...",
                    Timestamp = DateTime.Now
                });

                // 清理现有连接
                DisconnectInternal(false);

                // 创建TCP客户端并配置
                _tcpClient = new TcpClient();
                ConfigureTcpClient();

                // 异步连接
                var connectTask = _tcpClient.ConnectAsync(_serverIp, _serverPort);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));

                if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                {
                    throw new TimeoutException("连接服务器超时");
                }

                await connectTask;

                // 配置网络流
                _networkStream = _tcpClient.GetStream();
                _networkStream.ReadTimeout = ReceiveTimeout;
                _networkStream.WriteTimeout = SendTimeout;

                // 更新状态
                IsConnected = true;
                ConnectionTime = DateTime.Now;
                _lastReceiveTime = DateTime.Now;
                _lastSendTime = DateTime.Now;
                Status = "Connected";
                _reconnectAttempts = 0;

                // 启动接收任务
                _receiveCts = new CancellationTokenSource();
                _sendCts = new CancellationTokenSource();

                _ = Task.Run(() => ReceiveMessagesAsync(_receiveCts.Token), _receiveCts.Token);

                // 启动心跳检测
                if (EnableHeartbeat)
                {
                    StartHeartbeat();
                }

                // 启动活动检测
                StartInactivityMonitoring();

                OnConnectionStatusChanged(new TcpClientStatusEventArgs
                {
                    IsConnected = true,
                    Status = $"已成功连接到服务器 {ServerEndpoint}",
                    Timestamp = DateTime.Now
                });

                return true;
            }
            catch (Exception ex)
            {
                Status = "Connection Failed";
                OnErrorOccurred($"连接服务器失败: {ex.Message}");

                // 启动重连
                if (EnableAutoReconnect)
                {
                    StartReconnectTimer();
                }

                return false;
            }
            finally
            {
                lock (_connectionLock)
                {
                    _isConnecting = false;
                }
            }
        }

        private void ConfigureTcpClient()
        {
            _tcpClient.ReceiveBufferSize = ReceiveBufferSize;
            _tcpClient.SendBufferSize = SendBufferSize;
            _tcpClient.ReceiveTimeout = ReceiveTimeout;
            _tcpClient.SendTimeout = SendTimeout;
            _tcpClient.NoDelay = NoDelay;

            // 设置KeepAlive
            SetSocketKeepAlive(_tcpClient.Client);
        }

        private void SetSocketKeepAlive(Socket socket)
        {
            try
            {
                // 启用KeepAlive
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

                // 设置KeepAlive参数 (Windows)
                byte[] keepAliveValues = new byte[12];
                BitConverter.GetBytes(1u).CopyTo(keepAliveValues, 0); // 启用
                BitConverter.GetBytes(5000u).CopyTo(keepAliveValues, 4); // 5秒后开始探测
                BitConverter.GetBytes(1000u).CopyTo(keepAliveValues, 8); // 1秒探测间隔

                socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues, null);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"设置KeepAlive失败: {ex.Message}");
            }
        }
        #endregion
        #region 消息接收
        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[ReceiveBufferSize];
            var messageBuffer = new MemoryStream();

            try
            {
                while (!cancellationToken.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        int bytesRead = await _networkStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead == 0)
                        {
                            // 服务器正常关闭连接
                            OnConnectionStatusChanged(new TcpClientStatusEventArgs
                            {
                                IsConnected = false,
                                Status = "服务器关闭了连接",
                                Timestamp = DateTime.Now
                            });
                            break;
                        }
                        //更新统计数据
                        _lastReceiveTime = DateTime.Now;
                        _totalBytesReceived += bytesRead;

                        // 处理接收到的数据
                        await ProcessReceivedData(buffer, bytesRead);
                    }
                    catch (OperationCanceledException)
                    {
                        // 正常取消
                        break;
                    }
                    catch (IOException ioEx) when (ioEx.InnerException is SocketException socketEx)
                    {
                        HandleSocketException(socketEx);
                        break;
                    }
                    catch (SocketException socketEx)
                    {
                        HandleSocketException(socketEx);
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnErrorOccurred($"接收数据时发生错误: {ex.Message}");
                        break;
                    }
                }
            }
            finally
            {
                // 断开连接
                if (IsConnected)
                {
                    DisconnectInternal(true);

                    if (EnableAutoReconnect && !cancellationToken.IsCancellationRequested)
                    {
                        StartReconnectTimer();
                    }
                }
            }
        }

        private async Task ProcessReceivedData(byte[] buffer, int bytesRead)
        {
            // 将数据复制到新的数组
            byte[] receivedData = new byte[bytesRead];
            Array.Copy(buffer, 0, receivedData, 0, bytesRead);
            // 触发消息接收事件
            OnMessageReceived(new TcpClientMessageEventArgs
            {
                Message = _encoding.GetString(receivedData,0, receivedData.Length),
                RawData = receivedData,
                Timestamp = DateTime.Now,
                Direction = TcpClientMessageEventArgs.MessageDirection.Received
            });
            await ProcessSpecialMessage(receivedData);
        }

        private async Task ProcessSpecialMessage(byte[] data)
        {
            string message = _encoding.GetString(data).Trim();
            // 处理心跳响应
            if (message.Equals("PONG", StringComparison.OrdinalIgnoreCase) ||
                message.Equals("HEARTBEAT_ACK", StringComparison.OrdinalIgnoreCase))
            {
                // 心跳响应，更新活动时间
                _lastReceiveTime = DateTime.Now;
                return;
            }

            // 处理服务器命令
            if (message.StartsWith("CMD:"))
            {
                string command = message.Substring(4);
                await HandleServerCommand(command);
            }
        }

        private async Task HandleServerCommand(string command)
        {
            switch (command.ToUpper())
            {
                case "GET_STATUS":
                    string status = $"ID:{ClientId},Connected:{IsConnected},Uptime:{Uptime}";
                    await SendAsync($"STATUS:{status}");
                    break;

                case "PING":
                    await SendAsync("PONG");
                    break;

                case "DISCONNECT":
                    Disconnect();
                    break;
            }
        }

        private void HandleSocketException(SocketException socketEx)
        {
            string errorMsg = GetSocketErrorDescription(socketEx.SocketErrorCode);

            OnConnectionStatusChanged(new TcpClientStatusEventArgs
            {
                IsConnected = false,
                Status = errorMsg,
                Timestamp = DateTime.Now,
                Error = socketEx
            });

            OnErrorOccurred($"网络错误: {errorMsg}");
        }

        private string GetSocketErrorDescription(SocketError errorCode)
        {
            return errorCode switch
            {
                SocketError.ConnectionReset => "连接被重置",
                SocketError.ConnectionAborted => "连接被中止",
                SocketError.TimedOut => "连接超时",
                SocketError.HostUnreachable => "主机不可达",
                SocketError.NetworkUnreachable => "网络不可达",
                SocketError.Shutdown => "连接已关闭",
                _ => $"Socket错误: {errorCode}"
            };
        }
        #endregion

        #region 消息发送
        /// <summary>
        /// 发送消息到服务器
        /// </summary>
        public async Task<bool> SendAsync(string message)
        {
            if (!IsConnected || _networkStream == null)
            {
                OnErrorOccurred("发送失败: 未连接到服务器");
                return false;
            }

            await _sendSemaphore.WaitAsync();

            try
            {
                byte[] data = _encoding.GetBytes(message + "\n");

                await _networkStream.WriteAsync(data, 0, data.Length);
                await _networkStream.FlushAsync();

                _lastSendTime = DateTime.Now;
                _totalBytesSent += data.Length;

                // 触发消息发送事件
                OnMessageSent(new TcpClientMessageEventArgs
                {
                    Message = message,
                    RawData = data,
                    Timestamp = DateTime.Now,
                    Direction = TcpClientMessageEventArgs.MessageDirection.Sent
                });

                OnBytesTransferred(data.Length);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"发送消息失败: {ex.Message}");

                // 发送失败，断开连接
                if (IsConnected)
                {
                    DisconnectInternal(true);
                }

                return false;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// 发送原始字节数据
        /// </summary>
        public async Task<bool> SendBytesAsync(byte[] data)
        {
            if (!IsConnected || _networkStream == null)
                return false;

            await _sendSemaphore.WaitAsync();

            try
            {
                await _networkStream.WriteAsync(data, 0, data.Length);
                await _networkStream.FlushAsync();

                _lastSendTime = DateTime.Now;
                _totalBytesSent += data.Length;

                OnBytesTransferred(data.Length);

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"发送字节数据失败: {ex.Message}");
                return false;
            }
            finally
            {
                _sendSemaphore.Release();
            }
        }

        /// <summary>
        /// 发送文件（分块发送）
        /// </summary>
        public async Task<bool> SendFileAsync(string filePath, int chunkSize = 4096)
        {
            if (!File.Exists(filePath))
            {
                OnErrorOccurred($"文件不存在: {filePath}");
                return false;
            }

            try
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    var buffer = new byte[chunkSize];
                    int bytesRead;
                    long totalSent = 0;

                    // 发送文件开始标记
                    await SendAsync($"FILE_START:{Path.GetFileName(filePath)}:{fileStream.Length}");

                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        if (!await SendBytesAsync(buffer.AsMemory(0, bytesRead).ToArray()))
                            return false;

                        totalSent += bytesRead;

                        // 报告进度
                        OnMessageSent(new TcpClientMessageEventArgs
                        {
                            Message = $"文件传输进度: {totalSent}/{fileStream.Length}",
                            Timestamp = DateTime.Now,
                            Direction = TcpClientMessageEventArgs.MessageDirection.Sent
                        });

                        await Task.Delay(10); // 防止发送过快
                    }

                    // 发送文件结束标记
                    await SendAsync($"FILE_END:{Path.GetFileName(filePath)}");

                    return true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"发送文件失败: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 定时器处理
        private void StartHeartbeat()
        {
            _heartbeatTimer.Interval = HeartbeatInterval;
            _heartbeatTimer.Enabled = true;
            _heartbeatTimer.Start();
        }

        private void StopHeartbeat()
        {
            _heartbeatTimer.Stop();
            _heartbeatTimer.Enabled = false;
        }

        private async void OnHeartbeatTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsConnected)
                return;

            // 检查连接是否超时
            if ((DateTime.Now - LastActivityTime).TotalMilliseconds > HeartbeatInterval * 2)
            {
                OnErrorOccurred("心跳超时，连接可能已断开");
                DisconnectInternal(true);
                return;
            }

            // 发送心跳
            try
            {
                await SendAsync("PING");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"发送心跳失败: {ex.Message}");
            }
        }

        private void StartReconnectTimer()
        {
            if (_reconnectAttempts >= MaxReconnectAttempts)
            {
                OnConnectionStatusChanged(new TcpClientStatusEventArgs
                {
                    IsConnected = false,
                    Status = "已达到最大重连次数，停止重连",
                    Timestamp = DateTime.Now,
                    ReconnectAttempts = _reconnectAttempts
                });
                return;
            }

            _reconnectAttempts++;
            int delay = CalculateReconnectDelay();

            _reconnectTimer.Interval = delay;
            _reconnectTimer.Enabled = true;
            _reconnectTimer.Start();

            OnConnectionStatusChanged(new TcpClientStatusEventArgs
            {
                IsConnected = false,
                Status = $"等待 {delay / 1000} 秒后重连... (尝试 {_reconnectAttempts}/{MaxReconnectAttempts})",
                Timestamp = DateTime.Now,
                ReconnectAttempts = _reconnectAttempts
            });
        }

        private void StopReconnectTimer()
        {
            _reconnectTimer.Stop();
            _reconnectTimer.Enabled = false;
        }

        private int CalculateReconnectDelay()
        {
            // 指数退避算法
            int baseDelay = ReconnectInterval;
            int maxDelay = 60000; // 最大60秒

            int delay = baseDelay * (int)Math.Pow(2, _reconnectAttempts - 1);
            return Math.Min(delay, maxDelay);
        }

        private async void OnReconnectTimerElapsed(object sender, ElapsedEventArgs e)
        {
            StopReconnectTimer();

            if (_isConnecting || IsConnected)
                return;

            await ConnectAsync(_serverIp, _serverPort);
        }

        private void StartInactivityMonitoring()
        {
            _inactivityTimer.Interval = InactivityTimeout;
            _inactivityTimer.Enabled = true;
            _inactivityTimer.Start();
        }

        private void StopInactivityMonitoring()
        {
            _inactivityTimer.Stop();
            _inactivityTimer.Enabled = false;
        }

        private void OnInactivityTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!IsConnected)
                return;

            // 检查无活动时间
            if ((DateTime.Now - LastActivityTime).TotalMilliseconds > InactivityTimeout)
            {
                OnErrorOccurred("连接无活动，发送测试消息");
                _ = SendAsync("ACTIVITY_TEST");
            }

            // 重置定时器
            _inactivityTimer.Start();
        }
        #endregion

        #region 断开连接
        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            DisconnectInternal(false);

            OnConnectionStatusChanged(new TcpClientStatusEventArgs
            {
                IsConnected = false,
                Status = "客户端主动断开连接",
                Timestamp = DateTime.Now
            });
        }

        private void DisconnectInternal(bool autoReconnect)
        {
            IsConnected = false;
            Status = "Disconnected";

            // 停止定时器
            StopHeartbeat();
            StopInactivityMonitoring();

            if (!autoReconnect)
            {
                StopReconnectTimer();
            }

            // 取消任务
            _receiveCts?.Cancel();
            _sendCts?.Cancel();

            // 关闭网络流
            try
            {
                _networkStream?.Close();
                _networkStream?.Dispose();
            }
            catch { }

            // 关闭TCP客户端
            try
            {
                _tcpClient?.Close();
                _tcpClient?.Dispose();
            }
            catch { }

            _networkStream = null;
            _tcpClient = null;
        }
        #endregion
        /// <summary>
        /// 获取连接统计信息
        /// </summary>
        public string GetStatistics()
        {
            return $"客户端ID: {ClientId}\n" +
                   $"服务器: {ServerEndpoint}\n" +
                   $"连接状态: {Status}\n" +
                   $"运行时间: {Uptime}\n" +
                   $"发送字节: {TotalBytesSent}\n" +
                   $"接收字节: {TotalBytesReceived}\n" +
                   $"最后活动: {LastActivityTime:yyyy-MM-dd HH:mm:ss}\n" +
                   $"重连尝试: {ReconnectAttempts}";
        }
        #region 事件触发方法
        protected virtual void OnMessageReceived(TcpClientMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        protected virtual void OnMessageSent(TcpClientMessageEventArgs e)
        {
            MessageSent?.Invoke(this, e);
        }

        protected virtual void OnConnectionStatusChanged(TcpClientStatusEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(this, errorMessage);
        }

        protected virtual void OnBytesTransferred(long bytes)
        {
            BytesTransferred?.Invoke(this, bytes);
        }
        #endregion

        #region IDisposable实现
        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    DisconnectInternal(false);

                    _receiveCts?.Dispose();
                    _sendCts?.Dispose();
                    _sendSemaphore?.Dispose();

                    _heartbeatTimer?.Dispose();
                    _reconnectTimer?.Dispose();
                    _inactivityTimer?.Dispose();
                }

                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~EnhancedTcpClient()
        {
            Dispose(false);
        }
        #endregion
    }
}
