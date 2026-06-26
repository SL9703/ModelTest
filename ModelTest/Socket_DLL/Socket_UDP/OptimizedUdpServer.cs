using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ModelTest.Socket_DLL.Socket_UDP
{
    public enum UdpServerStatus
    {
        Stopped,
        Starting,
        Running,
        Stopping,
        Error
    }
    public class OptimizedUdpServer : IDisposable
    {
        private const bool EnableUdpTraceLog = false;
        private const string UdpTracePrefix = "[UDP-TRACE]";
        #region 事件定义
        public event EventHandler<UdpMessageEventArgs> MessageReceived;
        public event EventHandler<UdpMessageEventArgs> MessageSent;
        public event EventHandler<UdpClientEventArgs> ClientConnected;
        public event EventHandler<UdpClientEventArgs> ClientDisconnected;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<UdpServerStatus> StatusChanged;
        #endregion
        #region 字段
        private UdpClient _udpServer;
        private CancellationTokenSource _cts;
        private readonly ConcurrentDictionary<string, UdpClientInfo> _clients;
        private readonly object _lockObject = new object();

        // 定时器
        private System.Timers.Timer _cleanupTimer;
        private System.Timers.Timer _broadcastTimer;
        // 配置
        private int _port;
        private string _ip;
        private Encoding _encoding = Encoding.UTF8;
        private int _receiveBufferSize = 65536; // 64KB
        private int _sendBufferSize = 65536;
        private int _clientTimeout = 30000; // 30秒无消息则视为断开
        private int _cleanupInterval = 10000; // 10秒清理一次
        private bool _enableAutoCleanup = true;
        // 状态
        private UdpServerStatus _status = UdpServerStatus.Stopped;
        private DateTime _startTime;
        private long _totalBytesReceived;
        private long _totalBytesSent;
        private long _totalMessagesReceived;
        private long _totalMessagesSent;
        #endregion

        #region 属性
        public UdpServerStatus Status => _status;
        public bool IsRunning => _status == UdpServerStatus.Running;
        public int Port => _port;
        public string IP => _ip;
        public int ConnectedClients => _clients.Count;
        public TimeSpan Uptime => IsRunning ? DateTime.Now - _startTime : TimeSpan.Zero;
        public long TotalBytesReceived => _totalBytesReceived;
        public long TotalBytesSent => _totalBytesSent;
        public long TotalMessagesReceived => _totalMessagesReceived;
        public long TotalMessagesSent => _totalMessagesSent;
        public IReadOnlyDictionary<string, UdpClientInfo> ConnectedClientsInfo => _clients;

        // 可配置属性
        public int ReceiveTimeout { get; set; } = 5000;
        public int SendTimeout { get; set; } = 5000;
        public int ClientTimeout { get => _clientTimeout; set => _clientTimeout = value; }
        public int CleanupInterval { get => _cleanupInterval; set => _cleanupInterval = value; }
        public bool EnableAutoCleanup { get => _enableAutoCleanup; set => _enableAutoCleanup = value; }
        public int MaxClientCount { get; set; } = 1000;
        public bool EnableBroadcast { get; set; } = false;
        public int BroadcastInterval { get; set; } = 60000; // 60秒广播一次
        public string BroadcastMessage { get; set; } = "SERVER_HEARTBEAT";
        #endregion

        #region 构造函数
        public OptimizedUdpServer(string ip, int port)
        {
            _ip = ip;
            _port = port;
            _clients = new ConcurrentDictionary<string, UdpClientInfo>();
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            _cleanupTimer = new System.Timers.Timer();
            _cleanupTimer.Elapsed += OnCleanupTimerElapsed;
            _cleanupTimer.AutoReset = true;

            _broadcastTimer = new System.Timers.Timer();
            _broadcastTimer.Elapsed += OnBroadcastTimerElapsed;
            _broadcastTimer.AutoReset = true;
        }
        #endregion
        #region 启动和停止
        /// <summary>
        /// 启动UDP服务端
        /// </summary>
        public async Task<bool> StartAsync()
        {
            if (_status != UdpServerStatus.Stopped)
            {
                OnErrorOccurred($"服务已在运行中，当前状态: {_status}");
                return false;
            }

            try
            {
                _status = UdpServerStatus.Starting;
                OnStatusChanged(_status);

                // 创建UDP客户端
                _udpServer = new UdpClient(new IPEndPoint(IPAddress.Parse(_ip), _port));

                // 配置UDP客户端
                _udpServer.Client.ReceiveBufferSize = _receiveBufferSize;
                _udpServer.Client.SendBufferSize = _sendBufferSize;
                _udpServer.Client.ReceiveTimeout = ReceiveTimeout;
                _udpServer.Client.SendTimeout = SendTimeout;

                // 启用广播（如果需要）
                if (EnableBroadcast)
                {
                    _udpServer.EnableBroadcast = true;
                }

                _cts = new CancellationTokenSource();
                _startTime = DateTime.Now;
                _status = UdpServerStatus.Running;

                // 启动接收任务
                _ = Task.Run(() => ReceiveMessagesAsync(_cts.Token), _cts.Token);

                // 启动定时器
                if (_enableAutoCleanup)
                {
                    _cleanupTimer.Interval = _cleanupInterval;
                    _cleanupTimer.Start();
                }

                if (EnableBroadcast)
                {
                    _broadcastTimer.Interval = BroadcastInterval;
                    _broadcastTimer.Start();
                }

                OnStatusChanged(_status);
                LogMessage.Info($"UDP服务已启动，监听端口: {_port}");

                return true;
            }
            catch (Exception ex)
            {
                _status = UdpServerStatus.Error;
                OnErrorOccurred($"启动UDP服务失败: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 停止UDP服务端
        /// </summary>
        public void Stop()
        {
            if (_status != UdpServerStatus.Running)
                return;

            _status = UdpServerStatus.Stopping;
            OnStatusChanged(_status);

            try
            {
                // 停止定时器
                _cleanupTimer.Stop();
                _broadcastTimer.Stop();

                // 取消接收任务
                _cts?.Cancel();

                // 关闭UDP客户端
                _udpServer?.Close();
                _udpServer?.Dispose();

                // 清理客户端列表
                _clients.Clear();

                _status = UdpServerStatus.Stopped;
                OnStatusChanged(_status);
                LogMessage.Info("UDP服务已停止");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"停止UDP服务时发生错误: {ex.Message}");
                _status = UdpServerStatus.Error;
            }
        }
        #endregion

        #region 消息接收
        private async Task ReceiveMessagesAsync(CancellationToken cancellationToken)
        {
            try
            {
                TraceUdp($"接收循环已启动 状态={_status} IP={_ip} 端口={_port}");
                while (!cancellationToken.IsCancellationRequested && _status == UdpServerStatus.Running)
                {
                    try
                    {
                        if (_udpServer == null)
                        {
                            break;
                        }

                        var result = await _udpServer.ReceiveAsync();
                        var clientEndpoint = result.RemoteEndPoint as IPEndPoint;
                        var receivedData = result.Buffer;

                        if (clientEndpoint == null)
                        {
                            TraceUdp($"接收端点为空 字节数={receivedData.Length}");
                            continue;
                        }

                        TraceUdp($"Socket收到数据 端点={clientEndpoint} 字节数={receivedData.Length} 预览={BuildPreview(receivedData)}");
                        _totalBytesReceived += receivedData.Length;
                        _totalMessagesReceived++;

                        // 处理接收到的数据
                        await ProcessReceivedData(clientEndpoint, receivedData);
                    }
                    catch (OperationCanceledException)
                    {
                        TraceUdp("接收循环已取消");
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        if (cancellationToken.IsCancellationRequested || _status != UdpServerStatus.Running)
                        {
                            TraceUdp("停止过程中接收对象已释放");
                            break;
                        }

                        OnErrorOccurred("UDP接收对象已释放");
                    }
                    catch (SocketException socketEx)
                    {
                        if (cancellationToken.IsCancellationRequested ||
                            _status != UdpServerStatus.Running ||
                            socketEx.SocketErrorCode == SocketError.Interrupted ||
                            socketEx.SocketErrorCode == SocketError.OperationAborted)
                        {
                            TraceUdp($"接收循环Socket已关闭 错误码={socketEx.SocketErrorCode}");
                            break;
                        }

                        OnErrorOccurred($"Socket错误: {socketEx.Message}");
                    }
                    catch (Exception ex)
                    {
                        OnErrorOccurred($"接收数据时发生错误: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"接收任务异常: {ex.Message}");
            }
            finally
            {
                TraceUdp($"接收循环已退出 状态={_status}");
            }
        }

        private async Task ProcessReceivedData(IPEndPoint clientEndpoint, byte[] data)
        {
            string clientId = GetClientId(clientEndpoint);
            string message = _encoding.GetString(data);

            // 更新或添加客户端信息
            bool isNewClient = false;
            var clientInfo = _clients.GetOrAdd(clientId, id =>
            {
                isNewClient = true;
                return new UdpClientInfo
                {
                    ClientId = id,
                    Endpoint = clientEndpoint,
                    FirstSeen = DateTime.Now,
                    LastActivity = DateTime.Now,
                    TotalMessages = 0,
                    TotalBytes = 0
                };
            });

            // 更新客户端信息
            clientInfo.LastActivity = DateTime.Now;
            clientInfo.TotalMessages++;
            clientInfo.TotalBytes += data.Length;
            clientInfo.LastMessage = message;
            TraceUdp($"客户端已登记 客户端={clientId} 新客户端={isNewClient} 总消息数={clientInfo.TotalMessages} 本次字节数={data.Length}");

            // 触发客户端连接事件（如果是新客户端）
            if (isNewClient)
            {
                TraceUdp($"触发客户端连接事件 客户端={clientId}");
                OnClientConnected(new UdpClientEventArgs
                {
                    ClientEndpoint = clientEndpoint,
                    ClientId = clientId,
                    IsNewClient = true,
                    Timestamp = DateTime.Now,
                    Message = "新客户端连接"
                });
            }

            // 触发消息接收事件
            TraceUdp($"准备触发消息接收事件 客户端={clientId} 消息长度={message.Length}");
            OnMessageReceived(new UdpMessageEventArgs
            {
                ClientEndpoint = clientEndpoint,
                ClientId = clientId,
                Message = message,
                RawData = data,
                Timestamp = DateTime.Now
            });
            TraceUdp($"消息接收事件已触发 客户端={clientId}");

            try
            {
                // 先把原始报文上抛给业务层，再做协议内置处理，避免内置逻辑异常吞掉首包
                TraceUdp($"开始执行特殊消息处理 客户端={clientId}");
                await ProcessSpecialMessage(clientId, clientEndpoint, message);
                TraceUdp($"特殊消息处理完成 客户端={clientId}");
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"处理特殊消息时发生错误: {ex.Message}");
            }
        }

        private async Task ProcessSpecialMessage(string clientId, IPEndPoint clientEndpoint, string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TraceUdp($"特殊消息处理跳过空消息 客户端={clientId}");
                return;
            }

            if (message.StartsWith("CMD", StringComparison.OrdinalIgnoreCase))
            {
                TraceUdp($"识别到CMD命令 客户端={clientId} 消息={message}");
                string ret = await ProtocolHelper.ProcessCommandAsync(clientId, message);
                if (!string.IsNullOrWhiteSpace(ret))
                {
                    bool sent = await SendToEndpointAsync(clientEndpoint, ret);
                    if (sent)
                    {
                        TraceUdp($"CMD命令响应已发送 客户端={clientId} 端点={clientEndpoint} 响应={ret}");
                        if (message.StartsWith("cmd=0401", StringComparison.OrdinalIgnoreCase))
                        {
                            ProtocolHelper.Execute0401PostReplyBusiness(message);
                        }
                    }
                    else
                    {
                        TraceUdp($"CMD命令响应发送失败 客户端={clientId} 端点={clientEndpoint} 响应={ret}");
                    }
                }
            }
            else
            {
                TraceUdp($"非CMD消息，跳过特殊处理 客户端={clientId} 消息长度={message.Length}");
            }
        }

        private async Task HandleClientRegistration(string originalId, string customId)
        {
            if (_clients.TryGetValue(originalId, out var clientInfo))
            {
                // 移除原ID
                _clients.TryRemove(originalId, out _);

                // 使用新ID重新添加
                clientInfo.ClientId = customId;
                clientInfo.CustomId = customId;
                _clients[customId] = clientInfo;

                OnClientConnected(new UdpClientEventArgs
                {
                    ClientEndpoint = clientInfo.Endpoint,
                    ClientId = customId,
                    IsNewClient = false,
                    Timestamp = DateTime.Now,
                    Message = $"客户端注册新ID: {customId}"
                });
            }
        }

        private string GetClientId(IPEndPoint endpoint)
        {
            return $"{endpoint.Address}:{endpoint.Port}";
        }
        #endregion

        #region 消息发送
        /// <summary>
        /// 发送消息到指定客户端
        /// </summary>
        public async Task<bool> SendToClientAsync(string clientId, string message)
        {
            if (!_clients.TryGetValue(clientId, out var clientInfo))
            {
                OnErrorOccurred($"客户端不存在: {clientId}");
                return false;
            }

            return await SendToEndpointAsync(clientInfo.Endpoint, message);
        }

        /// <summary>
        /// 发送消息到指定端点
        /// </summary>
        public async Task<bool> SendToEndpointAsync(IPEndPoint endpoint, string message)
        {
            if (_status != UdpServerStatus.Running)
            {
                OnErrorOccurred("服务未运行");
                return false;
            }

            try
            {
                byte[] data = _encoding.GetBytes(message);
                int bytesSent = await _udpServer.SendAsync(data, data.Length, endpoint);
                TraceUdp($"发送消息到 {endpoint}: {message}");
                LogUdpReply(endpoint, message, bytesSent > 0);
                _totalBytesSent += bytesSent;
                _totalMessagesSent++;

                if (bytesSent > 0 && ShouldLogUdpReply(message))
                {
                    OnMessageSent(new UdpMessageEventArgs
                    {
                        ClientEndpoint = endpoint,
                        ClientId = GetClientId(endpoint),
                        Message = message,
                        RawData = data,
                        Timestamp = DateTime.Now
                    });
                }

                // 更新客户端信息（如果存在）
                string clientId = GetClientId(endpoint);
                if (_clients.TryGetValue(clientId, out var clientInfo))
                {
                    clientInfo.LastActivity = DateTime.Now;
                }

                return bytesSent > 0;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"发送消息到 {endpoint} 失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送消息到所有客户端
        /// </summary>
        public async Task<int> BroadcastAsync(string message, Func<UdpClientInfo, bool> filter = null)
        {
            if (_status != UdpServerStatus.Running)
                return 0;

            var tasks = new List<Task<bool>>();

            foreach (var client in _clients.Values)
            {
                if (filter == null || filter(client))
                {
                    tasks.Add(SendToEndpointAsync(client.Endpoint, message));
                }
            }

            if (tasks.Count == 0)
                return 0;

            var results = await Task.WhenAll(tasks);
            return results.Count(r => r);
        }

        /// <summary>
        /// 发送原始字节数据
        /// </summary>
        public async Task<bool> SendBytesToClientAsync(string clientId, byte[] data)
        {
            if (!_clients.TryGetValue(clientId, out var clientInfo))
                return false;

            try
            {
                int bytesSent = await _udpServer.SendAsync(data, data.Length, clientInfo.Endpoint);
                _totalBytesSent += bytesSent;
                return bytesSent > 0;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"发送字节数据失败: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region 客户端管理
        /// <summary>
        /// 清理超时客户端
        /// </summary>
        private void OnCleanupTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_enableAutoCleanup || _status != UdpServerStatus.Running)
                return;

            var timeout = DateTime.Now.AddMilliseconds(-_clientTimeout);
            var timeoutClients = _clients
                .Where(kvp => kvp.Value.LastActivity < timeout)
                .ToList();

            foreach (var client in timeoutClients)
            {
                if (_clients.TryRemove(client.Key, out var removedClient))
                {
                    OnClientDisconnected(new UdpClientEventArgs
                    {
                        ClientEndpoint = removedClient.Endpoint,
                        ClientId = removedClient.ClientId,
                        IsNewClient = false,
                        Timestamp = DateTime.Now,
                        Message = $"客户端超时断开 (最后活动: {removedClient.LastActivity:HH:mm:ss})"
                    });
                }
            }

            if (timeoutClients.Any())
            {
                LogMessage.Info($"清理了 {timeoutClients.Count} 个超时客户端");
            }
        }

        /// <summary>
        /// 广播心跳
        /// </summary>
        private async void OnBroadcastTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!EnableBroadcast || _status != UdpServerStatus.Running)
                return;

            await BroadcastAsync(BroadcastMessage);
        }

        /// <summary>
        /// 获取客户端信息
        /// </summary>
        public UdpClientInfo GetClientInfo(string clientId)
        {
            _clients.TryGetValue(clientId, out var clientInfo);
            return clientInfo;
        }

        /// <summary>
        /// 移除指定客户端
        /// </summary>
        public bool RemoveClient(string clientId)
        {
            if (_clients.TryRemove(clientId, out var clientInfo))
            {
                OnClientDisconnected(new UdpClientEventArgs
                {
                    ClientEndpoint = clientInfo.Endpoint,
                    ClientId = clientId,
                    IsNewClient = false,
                    Timestamp = DateTime.Now,
                    Message = "客户端被手动移除"
                });
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取所有客户端ID
        /// </summary>
        public List<string> GetAllClientIds()
        {
            return _clients.Keys.ToList();
        }

        /// <summary>
        /// 获取所有客户端信息
        /// </summary>
        public List<UdpClientInfo> GetAllClientInfos()
        {
            return _clients.Values.ToList();
        }
        #endregion

        #region 统计和诊断
        /// <summary>
        /// 获取服务统计信息
        /// </summary>
        public string GetStatistics()
        {
            return $"UDP服务统计:\n" +
                   $"状态: {_status}" +
                   $"端口: {_port}" +
                   $"运行时间: {Uptime}" +
                   $"连接客户端: {ConnectedClients}\n" +
                   $"接收消息: {_totalMessagesReceived:N0}" +
                   $"发送消息: {_totalMessagesSent:N0}" +
                   $"接收字节: {_totalBytesReceived:N0}" +
                   $"发送字节: {_totalBytesSent:N0}\n" +
                   $"客户端超时: {_clientTimeout}ms" +
                   $"最大客户端数: {MaxClientCount}";
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            _totalBytesReceived = 0;
            _totalBytesSent = 0;
            _totalMessagesReceived = 0;
            _totalMessagesSent = 0;
        }
        #endregion

        #region 事件触发方法
        protected virtual void OnMessageReceived(UdpMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        protected virtual void OnMessageSent(UdpMessageEventArgs e)
        {
            MessageSent?.Invoke(this, e);
        }

        protected virtual void OnClientConnected(UdpClientEventArgs e)
        {
            ClientConnected?.Invoke(this, e);
        }

        protected virtual void OnClientDisconnected(UdpClientEventArgs e)
        {
            ClientDisconnected?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(this, errorMessage);
            LogMessage.Info("UDP消息异常: " + errorMessage);
        }

        private static void TraceUdp(string message)
        {
            if (!EnableUdpTraceLog)
            {
                return;
            }

            LogMessage.Debug($"{UdpTracePrefix} {message}");
        }

        private static void LogUdpReply(IPEndPoint endpoint, string message, bool success)
        {
            if (!ShouldLogUdpReply(message))
            {
                return;
            }

            string logMessage = $"UDP回复 {endpoint}: {message}";
            if (success)
            {
                LogMessage.Debug(logMessage);
                return;
            }

            LogMessage.Info(logMessage);
        }

        private static bool ShouldLogUdpReply(string message)
        {
            return !string.IsNullOrWhiteSpace(message) &&
                   message.StartsWith("cmd=", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildPreview(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return "<empty>";
            }

            const int maxBytes = 24;
            int take = Math.Min(data.Length, maxBytes);
            string hex = BitConverter.ToString(data, 0, take);
            return data.Length > maxBytes ? $"{hex}..." : hex;
        }

        protected virtual void OnStatusChanged(UdpServerStatus status)
        {
            StatusChanged?.Invoke(this, status);
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
                    Stop();

                    _cts?.Dispose();
                    _cleanupTimer?.Dispose();
                    _broadcastTimer?.Dispose();
                    _udpServer?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
