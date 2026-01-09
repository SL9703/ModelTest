using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL
{
    public class Socket_DLL
    {
        #region 事件定义
        public event MessageReceivedHandler MessageReceived;
        public event ClientStatusChangedHandler ClientConnected;
        public event ClientStatusChangedHandler ClientDisconnected;
        public event EventHandler<string> ServerError;
        public event EventHandler<string> ServerStatusChanged;
        #endregion
        #region 字段和属性
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private readonly ConcurrentDictionary<string, ClientInfo> _clients;
        private readonly object _lockObject = new object();
        private readonly string _host;
        private readonly int _port;
        private readonly int _bufferSize;
        private readonly Encoding _encoding;
        private bool _isRunning = false;
        public bool IsRunning => _isRunning;
        public int ConnectedClientsCount => _clients.Count;
        public IReadOnlyDictionary<string, ClientInfo> ConnectedClients =>
            _clients.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        #endregion

        #region 构造函数
        public Socket_DLL(string host, int port, int bufferSize = 4096)
        {
            _host = host;
            _port = port;
            _bufferSize = bufferSize;
            _encoding = Encoding.UTF8;
            _clients = new ConcurrentDictionary<string, ClientInfo>();
        }
        #endregion

        #region 启动/停止服务器
        public async Task StartAsync()
        {
            if (_isRunning)
                throw new InvalidOperationException("服务器已经在运行");
            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Parse(_host), _port);

                // 设置Socket选项
                _listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _listener.Server.ReceiveBufferSize = _bufferSize * 2;
                _listener.Server.SendBufferSize = _bufferSize * 2;

                _listener.Start();
                _isRunning = true;

                OnServerStatusChanged($"TCP服务器已启动，监听IP：{_host}，监听端口: {_port}");
                // 开始监听客户端连接
                _ = Task.Run(() => AcceptClientsAsync(_cts.Token), _cts.Token);
            }
            catch (Exception ex)
            {
                _isRunning = false;
                OnServerError($"启动服务器失败: {ex.Message}");
                throw;
            }
        }

        public void Stop()
        {
            if (!_isRunning) 
                return;

            try
            {
                _cts?.Cancel();

                // 断开所有客户端连接
                var disconnectTasks = new List<Task>();
                foreach (var clientInfo in _clients.Values)
                {
                    disconnectTasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            clientInfo.Client.Close();
                        }
                        catch { }
                    }));
                }

                Task.WhenAll(disconnectTasks).Wait(3000);
                _clients.Clear();

                _listener?.Stop();
                _isRunning = false;

                OnServerStatusChanged("TCP服务器已停止");
            }
            catch (Exception ex)
            {
                OnServerError($"停止服务器时发生错误: {ex.Message}");
            }
        }
        #endregion
        #region 客户端连接处理
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested && _isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync()
                        .WaitAsync(TimeSpan.FromMilliseconds(1000), cancellationToken);

                    if (client != null)
                    {
                        _ = Task.Run(() => HandleClientConnectionAsync(client, cancellationToken), cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    // 正常取消
                    break;
                }
                catch (TimeoutException)
                {
                    // 接受连接超时，继续循环
                    continue;
                }
                catch (Exception ex)
                {
                    OnServerError($"接受客户端连接时发生错误: {ex.Message}");
                }
            }
        }

        private async Task HandleClientConnectionAsync(TcpClient client, CancellationToken cancellationToken)
        {
            string clientId = null;
            string clientEndpoint = null;

            try
            {
                // 配置客户端
                ConfigureClient(client);

                clientEndpoint = client.Client.RemoteEndPoint?.ToString() ?? "未知地址";
                clientId = GenerateClientId(clientEndpoint);

                var clientInfo = new ClientInfo
                {
                    Id = clientId,
                    Endpoint = clientEndpoint,
                    Client = client,
                    LastActivity = DateTime.Now,
                    ConnectedTime = DateTime.Now
                };

                if (!_clients.TryAdd(clientId, clientInfo))
                {
                    client.Close();
                    return;
                }

                // 触发客户端连接事件
                OnClientConnected(new ClientStatusChangedEventArgs
                {
                    ClientId = clientId,
                    ClientEndpoint = clientEndpoint,
                    IsConnected = true,
                    ChangeTime = DateTime.Now
                });

                // 开始接收消息
                await ReceiveMessagesAsync(clientInfo, cancellationToken);
            }
            catch (Exception ex)
            {
                OnServerError($"处理客户端连接时发生错误 ({clientEndpoint}): {ex.Message}");
            }
            finally
            {
                if (clientId != null)
                {
                    _clients.TryRemove(clientId, out _);
                    OnClientDisconnected(new ClientStatusChangedEventArgs
                    {
                        ClientId = clientId,
                        ClientEndpoint = clientEndpoint ?? "未知地址",
                        IsConnected = false,
                        ChangeTime = DateTime.Now
                    });
                }

                try { client?.Close(); } catch { }
            }
        }

        private void ConfigureClient(TcpClient client)
        {
            client.NoDelay = true; // 禁用Nagle算法，减少延迟
            client.ReceiveTimeout = 30000; // 30秒接收超时
            client.SendTimeout = 30000; // 30秒发送超时
            client.ReceiveBufferSize = _bufferSize;
            client.SendBufferSize = _bufferSize;
        }
        #endregion
        #region 消息接收处理
        private async Task ReceiveMessagesAsync(ClientInfo clientInfo, CancellationToken cancellationToken)
        {
            var client = clientInfo.Client;
            var stream = client.GetStream();
            var buffer = new byte[_bufferSize];
            var messageBuilder = new StringBuilder();
            var partialBuffer = new List<byte>();

            try
            {
                while (!cancellationToken.IsCancellationRequested && client.Connected)
                {
                    // 检查连接状态
                    if (IsSocketConnected(client.Client))
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

                        if (bytesRead == 0)
                        {
                            // 客户端正常关闭连接
                            await Task.Delay(100, cancellationToken);
                            break;
                        }

                        clientInfo.LastActivity = DateTime.Now;

                        // 处理接收到的数据
                        await ProcessReceivedData(clientInfo, buffer, bytesRead, cancellationToken);
                    }
                    else
                    {
                        // Socket连接已断开
                        break;
                    }
                }
            }
            catch (IOException ex) when (ex.InnerException is SocketException socketEx)
            {
                // Socket相关异常
                OnServerError($"客户端 {clientInfo.Endpoint} 连接异常: {socketEx.SocketErrorCode}");
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                OnServerError($"接收来自 {clientInfo.Endpoint} 的消息时发生错误: {ex.Message}");
            }
        }

        private async Task ProcessReceivedData(ClientInfo clientInfo, byte[] buffer, int bytesRead, CancellationToken cancellationToken)
        {
            // 这里可以添加消息分隔符处理，例如换行符分隔的消息
            var message = _encoding.GetString(buffer, 0, bytesRead);

            // 触发消息接收事件
            OnMessageReceived(new MessageReceivedEventArgs
            {
                ClientId = clientInfo.Id,
                ClientEndpoint = clientInfo.Endpoint,
                Message = message,
                ReceivedTime = DateTime.Now
            });

            // 自动回复确认（可选）
            await SendAsync(clientInfo.Id, "ACK: Message received",false, cancellationToken);
        }
        #endregion

        #region 消息发送方法
        /// <summary>
        /// 发送消息到指定客户端
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="message"></param>
        /// <param name="ASCIIOrHEX">true hex  false ascii</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<bool> SendAsync(string clientId, string message, bool ASCIIOrHEX, CancellationToken cancellationToken = default)
        {
            if (!_clients.TryGetValue(clientId, out var clientInfo))
            {
                OnServerError($"找不到客户端: {clientId}");
                return false;
            }

            if (!clientInfo.Client.Connected)
            {
                OnServerError($"客户端 {clientId} 已断开连接");
                _clients.TryRemove(clientId, out _);
                return false;
            }

            try
            {
               
                var stream = clientInfo.Client.GetStream();
                if (!ASCIIOrHEX)
                {
                    // 发送ascii数据
                    byte[] hexBytes = ModelTool.HexStringToByteArray(message);//转换
                    hexBytes = ModelTool.bytesTrimEnd(hexBytes);//去除结尾的0x00
                    await stream.WriteAsync(hexBytes, 0, hexBytes.Length, cancellationToken);//发送
                }
                else
                {
                    // 发送hex数据
                    var messageBytes = Encoding.ASCII.GetBytes(message);
                    await stream.WriteAsync(messageBytes, 0, messageBytes.Length, cancellationToken);
                }
                await stream.FlushAsync(cancellationToken);
                clientInfo.LastActivity = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                OnServerError($"发送消息到客户端 {clientId} 失败: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendToEndpointAsync(string endpoint, string message, CancellationToken cancellationToken = default)
        {
            var clientInfo = _clients.Values.FirstOrDefault(c => c.Endpoint == endpoint);
            if (clientInfo == null)
                return false;

            return await SendAsync(clientInfo.Id, message, false,cancellationToken);
        }

        public async Task<bool> BroadcastAsync(string message,bool ASCIIOrHEX,CancellationToken cancellationToken = default)
        {
            var success = true;
            var tasks = new List<Task<bool>>();

            foreach (var clientId in _clients.Keys.ToList())
            {
                tasks.Add(SendAsync(clientId, message, ASCIIOrHEX, cancellationToken));
            }

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
        #endregion

        #region 客户端管理方法
        public bool DisconnectClient(string clientId)
        {
            if (_clients.TryRemove(clientId, out var clientInfo))
            {
                try
                {
                    clientInfo.Client.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    OnServerError($"断开客户端 {clientId} 连接时发生错误: {ex.Message}");
                    return false;
                }
            }
            return false;
        }

        public void DisconnectAllClients()
        {
            foreach (var clientId in _clients.Keys.ToList())
            {
                DisconnectClient(clientId);
            }
        }

        public ClientInfo GetClientInfo(string clientId)
        {
            _clients.TryGetValue(clientId, out var clientInfo);
            return clientInfo;
        }

        public IEnumerable<ClientInfo> GetAllClientInfos()
        {
            return _clients.Values.ToList();
        }
        #endregion

        #region 工具方法
        private string GenerateClientId(string endpoint)
        {
            return $"{endpoint}_{DateTime.Now.Ticks}_{Guid.NewGuid().GetHashCode():X}";
        }

        private bool IsSocketConnected(Socket socket)
        {
            try
            {
                return !(socket.Poll(1000, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch
            {
                return false;
            }
        }

        public void CleanupInactiveClients(TimeSpan timeout)
        {
            var cutoff = DateTime.Now - timeout;
            var inactiveClients = _clients.Values
                .Where(c => c.LastActivity < cutoff)
                .ToList();

            foreach (var client in inactiveClients)
            {
                DisconnectClient(client.Id);
            }
        }
        #endregion
        #region 资源清理
        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
            _listener?.Server?.Dispose();
        }
        #endregion
        #region 事件触发方法
        protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
        {
            LogMessage.SocketLog($"收到消息 from {e.ClientEndpoint}: {e.Message}");
            MessageReceived?.Invoke(this, e);
        }

        protected virtual void OnClientConnected(ClientStatusChangedEventArgs e)
        {
            LogMessage.SocketLog(_host + $" 客户端已连接: {e.ClientEndpoint}");
            ClientConnected?.Invoke(this, e);
        }

        protected virtual void OnClientDisconnected(ClientStatusChangedEventArgs e)
        {
            LogMessage.SocketLog(_host + $" 客户端已断开连接: {e.ClientEndpoint}");
            ClientDisconnected?.Invoke(this, e);
        }

        protected virtual void OnServerError(string errorMessage)
        {
            LogMessage.SocketLog(errorMessage);
            ServerError?.Invoke(this, errorMessage);
        }

        protected virtual void OnServerStatusChanged(string statusMessage)
        {
            LogMessage.SocketLog(_host + " " + statusMessage);
            ServerStatusChanged?.Invoke(this, statusMessage);
        }
        #endregion

    }
}