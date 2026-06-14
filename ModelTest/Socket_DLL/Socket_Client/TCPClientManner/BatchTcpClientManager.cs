using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ModelTest.Socket_DLL.Socket_UDP;
using System.Timers;

namespace ModelTest.Socket_DLL.Socket_Client.TCPClientManner
{
    /// <summary>
    /// 批量TCP客户端管理器
    /// </summary>
    public class BatchTcpClientManager : IDisposable
    {
        #region 事件定义
        public event EventHandler<TcpClientMessageEventArgs> MessageReceived;
        public event EventHandler<TcpConnectionEventArgs> ConnectionStatusChanged;
        public event EventHandler<(string ConnectionId, string Error)> ErrorOccurred;
        #endregion

        #region 字段
        private readonly ConcurrentDictionary<string, TcpClientConnection> _connections;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellationTokens;
        private readonly object _lockObject = new object();
        private System.Timers.Timer _healthCheckTimer;
        private System.Timers.Timer _reconnectTimer;
        private Encoding _encoding = Encoding.UTF8;
        private int _nextConnectionId;
        #endregion

        #region 属性
        public int ConnectionCount => _connections.Count;
        public int ConnectedCount => _connections.Values.Count(c => c.IsConnected);
        public List<string> AllConnectionIds => _connections.Keys.ToList();
        public bool EnableAutoReconnect { get; set; } = true;
        public int ReconnectInterval { get; set; } = 5000;
        public int HeartbeatInterval { get; set; } = 30000;
        public int ReceiveTimeout { get; set; } = 30000;
        public int SendTimeout { get; set; } = 30000;
        public bool EnableHeartbeat { get; set; } = true;
        public string HeartbeatMessage { get; set; } = "PING";
        #endregion

        #region 构造函数
        public BatchTcpClientManager()
        {
            _connections = new ConcurrentDictionary<string, TcpClientConnection>();
            _cancellationTokens = new ConcurrentDictionary<string, CancellationTokenSource>();
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            _healthCheckTimer = new System.Timers.Timer(10000); // 10秒检查一次
            _healthCheckTimer.Elapsed += OnHealthCheckTimerElapsed;
            _healthCheckTimer.AutoReset = true;
            _healthCheckTimer.Start();

            _reconnectTimer = new System.Timers.Timer();
            _reconnectTimer.Elapsed += OnReconnectTimerElapsed;
            _reconnectTimer.AutoReset = false;
        }
        #endregion

        #region 批量连接创建
        /// <summary>
        /// 批量创建连接
        /// </summary>
        public async Task<List<string>> CreateConnectionsAsync(List<(string ip, int port, string name)> connections)
        {
            var results = new List<string>();
            var tasks = new List<Task<string>>();

            foreach (var conn in connections)
            {
                tasks.Add(CreateAndConnectAsync(conn.ip, conn.port, conn.name));
            }

            var taskResults = await Task.WhenAll(tasks);
            results.AddRange(taskResults.Where(r => r != null));

            return results;
        }

        /// <summary>
        /// 创建单个连接
        /// </summary>
        public async Task<string> CreateAndConnectAsync(string serverIp, int serverPort, string name = null)
        {
            string connectionId = GenerateConnectionId();
            var connection = new TcpClientConnection
            {
                ConnectionId = connectionId,
                Name = name ?? $"Connection_{connectionId}",
                ServerIp = serverIp,
                ServerPort = serverPort,
                Status = "创建中"
            };

            _connections[connectionId] = connection;
            _cancellationTokens[connectionId] = new CancellationTokenSource();

            bool success = await ConnectAsync(connectionId);
            return success ? connectionId : null;
        }

        /// <summary>
        /// 批量从配置文件创建连接
        /// </summary>
        public async Task<List<string>> CreateFromConfigAsync(string configFile)
        {
            var connections = new List<(string ip, int port, string name)>();

            // 从配置文件读取（示例：JSON格式）
            if (File.Exists(configFile))
            {
                string json = File.ReadAllText(configFile);
                // 解析JSON配置
                // 这里简化处理，实际使用时需要正确的JSON解析
                dynamic config = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                foreach (var item in config.connections)
                {
                    connections.Add((item.ip.ToString(), (int)item.port, item.name.ToString()));
                }
            }

            return await CreateConnectionsAsync(connections);
        }

        private string GenerateConnectionId()
        {
            return $"TCP_{Interlocked.Increment(ref _nextConnectionId):D4}_{DateTime.Now:HHmmss}";
        }

        private async Task<bool> ConnectAsync(string connectionId)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
                return false;

            if (connection.IsConnected)
                return true;

            try
            {
                connection.Status = "连接中";
                OnConnectionStatusChanged(connectionId, false, "正在连接...");

                var client = new TcpClient();

                // 配置
                client.ReceiveTimeout = ReceiveTimeout;
                client.SendTimeout = SendTimeout;
                client.NoDelay = true;

                // 连接
                var connectTask = client.ConnectAsync(connection.ServerIp, connection.ServerPort);
                var timeoutTask = Task.Delay(10000);

                if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
                {
                    throw new TimeoutException("连接超时");
                }

                await connectTask;

                // 更新连接信息
                connection.Client = client;
                connection.Stream = client.GetStream();
                connection.IsConnected = true;
                connection.ConnectTime = DateTime.Now;
                connection.LastActivity = DateTime.Now;
                connection.Status = "已连接";
                connection.ReconnectCount = 0;

                // 启动接收任务
                var cts = _cancellationTokens[connectionId];
                _ = Task.Run(() => ReceiveMessagesAsync(connectionId, cts.Token));

                OnConnectionStatusChanged(connectionId, true, "连接成功");
                return true;
            }
            catch (Exception ex)
            {
                connection.Status = $"连接失败: {ex.Message}";
                connection.IsConnected = false;
                OnErrorOccurred(connectionId, $"连接失败: {ex.Message}");
                OnConnectionStatusChanged(connectionId, false, $"连接失败: {ex.Message}", ex);

                if (EnableAutoReconnect)
                {
                    ScheduleReconnect(connectionId);
                }

                return false;
            }
        }
        #endregion

        #region 消息接收
        private async Task ReceiveMessagesAsync(string connectionId, CancellationToken cancellationToken)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
                return;

            var buffer = new byte[8192];

            try
            {
                while (!cancellationToken.IsCancellationRequested && connection.IsConnected)
                {
                    try
                    {
                        int bytesRead = await connection.Stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead == 0)
                        {
                            // 服务器关闭连接
                            HandleDisconnect(connectionId, "服务器关闭连接");
                            break;
                        }

                        connection.LastActivity = DateTime.Now;
                        connection.TotalBytesReceived += bytesRead;

                        // 处理消息
                        byte[] receivedData = new byte[bytesRead];
                        Array.Copy(buffer, 0, receivedData, 0, bytesRead);
                        string message = _encoding.GetString(receivedData);

                        // 触发消息事件
                        OnMessageReceived(new TcpClientMessageEventArgs
                        {
                            ConnectionId = connectionId,
                            Message = message,
                            RawData = receivedData,
                            Timestamp = DateTime.Now
                        });

                        // 处理心跳响应
                        if (EnableHeartbeat && message == "PONG")
                        {
                            // 心跳响应，更新活动时间
                            connection.LastActivity = DateTime.Now;
                        }
                    }
                    catch (IOException ioEx) when (ioEx.InnerException is SocketException)
                    {
                        HandleDisconnect(connectionId, "连接异常断开");
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        OnErrorOccurred(connectionId, $"接收消息错误: {ex.Message}");
                    }
                }
            }
            finally
            {
                if (connection.IsConnected)
                {
                    HandleDisconnect(connectionId, "接收循环结束");
                }
            }
        }

        private void HandleDisconnect(string connectionId, string reason)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
                return;

            connection.IsConnected = false;
            connection.Status = $"断开: {reason}";
            OnConnectionStatusChanged(connectionId, false, reason);

            // 清理资源
            try
            {
                connection.Stream?.Close();
                connection.Client?.Close();
            }
            catch { }

            // 自动重连
            if (EnableAutoReconnect)
            {
                ScheduleReconnect(connectionId);
            }
        }
        #endregion

        #region 消息发送
        /// <summary>
        /// 发送消息到指定连接
        /// </summary>
        public async Task<bool> SendAsync(string connectionId, string message)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
            {
                OnErrorOccurred(connectionId, "连接不存在");
                return false;
            }

            if (!connection.IsConnected || connection.Stream == null)
            {
                OnErrorOccurred(connectionId, "连接未建立");
                return false;
            }

            try
            {
                byte[] data = _encoding.GetBytes(message + "\n");
                await connection.Stream.WriteAsync(data, 0, data.Length);
                await connection.Stream.FlushAsync();

                connection.LastActivity = DateTime.Now;
                connection.TotalBytesSent += data.Length;

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(connectionId, $"发送失败: {ex.Message}");
                HandleDisconnect(connectionId, $"发送失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendBytesAsync(string connectionId, byte[] data)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
            {
                OnErrorOccurred(connectionId, "连接不存在");
                return false;
            }

            if (!connection.IsConnected || connection.Stream == null)
            {
                OnErrorOccurred(connectionId, "连接未建立");
                return false;
            }

            try
            {
                await connection.Stream.WriteAsync(data, 0, data.Length);
                await connection.Stream.FlushAsync();

                connection.LastActivity = DateTime.Now;
                connection.TotalBytesSent += data.Length;

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(connectionId, $"发送失败: {ex.Message}");
                HandleDisconnect(connectionId, $"发送失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 发送消息到所有连接
        /// </summary>
        public async Task<Dictionary<string, bool>> BroadcastAsync(string message)
        {
            var results = new Dictionary<string, bool>();
            var tasks = new List<Task<KeyValuePair<string, bool>>>();

            foreach (var connectionId in _connections.Keys)
            {
                tasks.Add(SendAndReturnResultAsync(connectionId, message));
            }

            var taskResults = await Task.WhenAll(tasks);
            foreach (var result in taskResults)
            {
                results[result.Key] = result.Value;
            }

            return results;
        }

        private async Task<KeyValuePair<string, bool>> SendAndReturnResultAsync(string connectionId, string message)
        {
            bool success = await SendAsync(connectionId, message);
            return new KeyValuePair<string, bool>(connectionId, success);
        }

        /// <summary>
        /// 发送消息到指定条件的一组连接
        /// </summary>
        public async Task<Dictionary<string, bool>> SendToGroupAsync(Func<TcpClientConnection, bool> predicate, string message)
        {
            var targetConnections = _connections.Values.Where(predicate).Select(c => c.ConnectionId).ToList();
            var results = new Dictionary<string, bool>();

            foreach (var connectionId in targetConnections)
            {
                results[connectionId] = await SendAsync(connectionId, message);
            }

            return results;
        }
        #endregion

        #region 连接管理
        /// <summary>
        /// 断开指定连接
        /// </summary>
        public void Disconnect(string connectionId)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
            {
                HandleDisconnect(connectionId, "主动断开");
            }
        }

        /// <summary>
        /// 断开所有连接
        /// </summary>
        public void DisconnectAll()
        {
            foreach (var connectionId in _connections.Keys.ToList())
            {
                Disconnect(connectionId);
            }
        }

        /// <summary>
        /// 重新连接指定连接
        /// </summary>
        public async Task<bool> ReconnectAsync(string connectionId)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
                return false;

            // 先断开
            HandleDisconnect(connectionId, "准备重连");

            // 清理旧资源
            try
            {
                connection.Client?.Close();
                connection.Stream?.Close();
            }
            catch { }

            // 创建新的CancellationToken
            if (_cancellationTokens.TryGetValue(connectionId, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }
            _cancellationTokens[connectionId] = new CancellationTokenSource();

            // 重新连接
            connection.ReconnectCount++;
            return await ConnectAsync(connectionId);
        }

        /// <summary>
        /// 获取连接信息
        /// </summary>
        public TcpClientConnection GetConnectionInfo(string connectionId)
        {
            _connections.TryGetValue(connectionId, out var connection);
            return connection;
        }

        /// <summary>
        /// 获取所有连接信息
        /// </summary>
        public List<TcpClientConnection> GetAllConnectionInfos()
        {
            return _connections.Values.ToList();
        }

        /// <summary>
        /// 移除连接
        /// </summary>
        public bool RemoveConnection(string connectionId)
        {
            Disconnect(connectionId);

            if (_cancellationTokens.TryRemove(connectionId, out var cts))
            {
                cts.Dispose();
            }

            return _connections.TryRemove(connectionId, out _);
        }

        /// <summary>
        /// 清空所有连接
        /// </summary>
        public void ClearAllConnections()
        {
            DisconnectAll();
            _connections.Clear();
            _cancellationTokens.Clear();
        }
        #endregion

        #region 自动重连和健康检查
        private void ScheduleReconnect(string connectionId)
        {
            if (!EnableAutoReconnect)
                return;

            if (!_connections.TryGetValue(connectionId, out var connection))
                return;

            if (connection.ReconnectCount >= 10)
            {
                OnErrorOccurred(connectionId, "达到最大重连次数，停止重连");
                return;
            }

            // 使用定时器延迟重连
            var timer = new System.Timers.Timer(ReconnectInterval * (connection.ReconnectCount + 1));
            timer.Elapsed += async (sender, e) =>
            {
                timer.Stop();
                timer.Dispose();

                if (!_connections.TryGetValue(connectionId, out var conn) || conn.IsConnected)
                    return;

                OnErrorOccurred(connectionId, $"正在重连... (第{conn.ReconnectCount + 1}次)");
                await ReconnectAsync(connectionId);
            };
            timer.Start();
        }

        private async void OnHealthCheckTimerElapsed(object sender, ElapsedEventArgs e)
        {
            var now = DateTime.Now;
            var timeout = TimeSpan.FromMilliseconds(HeartbeatInterval * 2);

            foreach (var connection in _connections.Values)
            {
                if (connection.IsConnected && EnableHeartbeat)
                {
                    // 检查连接是否活跃
                    if (now - connection.LastActivity > timeout)
                    {
                        OnErrorOccurred(connection.ConnectionId, "心跳超时，正在重连");
                        await ReconnectAsync(connection.ConnectionId);
                    }
                    else
                    {
                        // 发送心跳
                        await SendAsync(connection.ConnectionId, HeartbeatMessage);
                    }
                }
            }
        }

        private async void OnReconnectTimerElapsed(object sender, ElapsedEventArgs e)
        {
            // 批量重连逻辑
            var toReconnect = _connections.Values.Where(c => !c.IsConnected).ToList();
            foreach (var connection in toReconnect)
            {
                await ReconnectAsync(connection.ConnectionId);
                await Task.Delay(100); // 避免同时重连过多
            }
        }
        #endregion

        #region 批量操作
        /// <summary>
        /// 批量执行操作
        /// </summary>
        public async Task<Dictionary<string, T>> BatchOperationAsync<T>(Func<string, Task<T>> operation)
        {
            var results = new Dictionary<string, T>();
            var tasks = new List<Task<KeyValuePair<string, T>>>();

            foreach (var connectionId in _connections.Keys)
            {
                tasks.Add(ExecuteOperationAsync(connectionId, operation));
            }

            var taskResults = await Task.WhenAll(tasks);
            foreach (var result in taskResults)
            {
                results[result.Key] = result.Value;
            }

            return results;
        }

        private async Task<KeyValuePair<string, T>> ExecuteOperationAsync<T>(string connectionId, Func<string, Task<T>> operation)
        {
            try
            {
                T result = await operation(connectionId);
                return new KeyValuePair<string, T>(connectionId, result);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(connectionId, $"批量操作失败: {ex.Message}");
                return new KeyValuePair<string, T>(connectionId, default(T));
            }
        }

        /// <summary>
        /// 获取连接统计信息
        /// </summary>
        public string GetStatistics()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== TCP连接统计 ===");
            sb.AppendLine($"总连接数: {ConnectionCount}");
            sb.AppendLine($"已连接数: {ConnectedCount}");
            sb.AppendLine($"断开连接数: {ConnectionCount - ConnectedCount}");
            sb.AppendLine($"自动重连: {(EnableAutoReconnect ? "启用" : "禁用")}");
            sb.AppendLine($"心跳检测: {(EnableHeartbeat ? "启用" : "禁用")}");
            sb.AppendLine();
            sb.AppendLine("详细连接信息:");
            sb.AppendLine("ID | 名称 | 服务器 | 状态 | 发送/接收 | 最后活动");

            foreach (var conn in _connections.Values)
            {
                sb.AppendLine($"{conn.ConnectionId} | {conn.Name} | {conn.ServerIp}:{conn.ServerPort} | " +
                             $"{conn.Status} | {conn.TotalBytesSent}/{conn.TotalBytesReceived} | " +
                             $"{conn.LastActivity:HH:mm:ss}");
            }

            return sb.ToString();
        }
        #endregion

        #region 事件触发
        protected virtual void OnMessageReceived(TcpClientMessageEventArgs e)
        {
            MessageReceived?.Invoke(this, e);
        }

        protected virtual void OnConnectionStatusChanged(string connectionId, bool isConnected, string status, Exception error = null)
        {
            ConnectionStatusChanged?.Invoke(this, new TcpConnectionEventArgs
            {
                ConnectionId = connectionId,
                IsConnected = isConnected,
                Status = status,
                Timestamp = DateTime.Now,
                Error = error
            });
        }

        protected virtual void OnErrorOccurred(string connectionId, string error)
        {
            ErrorOccurred?.Invoke(this, (connectionId, error));
        }
        #endregion

        #region IDisposable
        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                DisconnectAll();
                _healthCheckTimer?.Stop();
                _healthCheckTimer?.Dispose();
                _reconnectTimer?.Stop();
                _reconnectTimer?.Dispose();

                foreach (var cts in _cancellationTokens.Values)
                {
                    cts?.Dispose();
                }
                _disposed = true;
            }
        }
        #endregion
    }
}
