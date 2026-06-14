using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModelTest.Socket_DLL.Socket_UDP;

namespace ModelTest.Socket_DLL.Socket_Client.TCPClientManner
{
    /// <summary>
    /// 连接池管理
    /// </summary>
    public class TcpConnectionPool
    {
        private static TcpConnectionPool _instance;
        private static readonly object _lock = new object();
        private BatchTcpClientManager _clientManager;

        private ConcurrentDictionary<string, string> _addressToConnectionId; // 地址 -> 连接ID
        private ConcurrentDictionary<string, ConnectionInfo> _connectionInfoMap; // 连接ID -> 详细信息

        //private HashSet<string> _connectedAddresses; // 存储已连接的地址
        /// <summary>
        /// 连接池构造函数
        /// </summary>
        private TcpConnectionPool()
        {
            _clientManager = new BatchTcpClientManager();
            _clientManager.EnableAutoReconnect = true;
            _clientManager.EnableHeartbeat = true;
            _clientManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            _addressToConnectionId = new ConcurrentDictionary<string, string>();
            _connectionInfoMap = new ConcurrentDictionary<string, ConnectionInfo>();
            //_connectedAddresses = new HashSet<string>();
        }
        public static TcpConnectionPool Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TcpConnectionPool();
                        }
                    }
                }
                return _instance;
            }
        }
        public BatchTcpClientManager Manager => _clientManager;
        /// <summary>
        /// 添加连接（如果已存在则跳过）
        /// </summary>
        public async Task<string> AddConnectionIfNotExists(string ip, int port, string name)
        {
            string addressKey = $"{ip}:{port}";

            // 检查是否已存在
            if (_addressToConnectionId.ContainsKey(addressKey))
            {
                LogMessage.SocketLog($"连接 {addressKey} 已存在，跳过创建");
                return _addressToConnectionId[addressKey];
            }

            // 创建新连接
            var results = await _clientManager.CreateConnectionsAsync(new List<(string, int, string)> { (ip, port, name) });

            if (results != null && results.Any())
            {
                string connectionId = results.First();
                _addressToConnectionId[addressKey] = connectionId;
                _connectionInfoMap[connectionId] = new ConnectionInfo
                {
                    ConnectionId = connectionId,
                    Name = name,
                    Ip = ip,
                    Port = port,
                    AddressKey = addressKey
                };
                return connectionId;
            }

            return null;
        }
        /// <summary>
        /// 批量添加连接（自动去重）
        /// </summary>
        public async Task<List<string>> AddConnectionsBatchAsync(List<(string ip, int port, string name)> connections)
        {
            var newConnections = new List<(string ip, int port, string name)>();

            foreach (var conn in connections)
            {
                string addressKey = $"{conn.ip}:{conn.port}";
                if (!_addressToConnectionId.ContainsKey(addressKey))
                {
                    newConnections.Add(conn);
                }

            }

            if (newConnections.Any())
            {
                var results = await _clientManager.CreateConnectionsAsync(newConnections);
                for (int i = 0; i < results.Count && i < newConnections.Count; i++)
                {
                    string addressKey = $"{newConnections[i].ip}:{newConnections[i].port}";
                    _addressToConnectionId[addressKey] = results[i];
                    _connectionInfoMap[results[i]] = new ConnectionInfo
                    {
                        ConnectionId = results[i],
                        Name = newConnections[i].name,
                        Ip = newConnections[i].ip,
                        Port = newConnections[i].port,
                        AddressKey = addressKey
                    };
                }

                return results;
            }

            return new List<string>();
        }
        #region 发送消息到指定服务器的方法

        /// <summary>
        /// 通过IP和端口发送消息
        /// </summary>
        public async Task<bool> SendToServerAsync(string ip, int port, string message)
        {
            string addressKey = $"{ip}:{port}";

            if (_addressToConnectionId.TryGetValue(addressKey, out string connectionId))
            {
                return await _clientManager.SendAsync(connectionId, message);
            }
            else
            {
                LogMessage.SocketLog($"未找到服务器连接: {addressKey}");
                return false;
            }
        }

        /// <summary>
        /// 通过连接名称发送消息
        /// </summary>
        public async Task<bool> SendToServerByNameAsync(string name, string message)
        {
            var connection = _connectionInfoMap.Values.FirstOrDefault(c => c.Name == name);
            if (connection != null)
            {
                return await _clientManager.SendAsync(connection.ConnectionId, message);
            }
            else
            {
                LogMessage.SocketLog($"未找到名称为 '{name}' 的连接");
                return false;
            }
        }

        public async Task<bool> SendHexToServerByNameAsync(string name, string hexMessage)
        {
            var connection = _connectionInfoMap.Values.FirstOrDefault(c => c.Name == name);
            if (connection != null)
            {
                byte[] data = ModelTool.HexStringToByteArray(hexMessage);
                return await _clientManager.SendBytesAsync(connection.ConnectionId, data);
            }

            LogMessage.SocketLog($"未找到名称为 '{name}' 的连接");
            return false;
        }

        /// <summary>
        /// 通过连接ID发送消息
        /// </summary>
        public async Task<bool> SendToServerByIdAsync(string connectionId, string message)
        {
            return await _clientManager.SendAsync(connectionId, message);
        }

        /// <summary>
        /// 发送消息到所有服务器
        /// </summary>
        public async Task<Dictionary<string, bool>> SendToAllAsync(string message)
        {
            return await _clientManager.BroadcastAsync(message);
        }

        /// <summary>
        /// 发送消息到指定的一组服务器
        /// </summary>
        public async Task<Dictionary<string, bool>> SendToServersAsync(List<string> connectionIds, string message)
        {
            var results = new Dictionary<string, bool>();

            foreach (var connectionId in connectionIds)
            {
                bool success = await _clientManager.SendAsync(connectionId, message);
                results[connectionId] = success;
            }

            return results;
        }

        /// <summary>
        /// 根据条件发送消息
        /// </summary>
        public async Task<Dictionary<string, bool>> SendToServersWhereAsync(Func<ConnectionInfo, bool> predicate, string message)
        {
            var targetConnections = _connectionInfoMap.Values.Where(predicate).ToList();
            var results = new Dictionary<string, bool>();

            foreach (var conn in targetConnections)
            {
                bool success = await _clientManager.SendAsync(conn.ConnectionId, message);
                results[conn.ConnectionId] = success;
            }

            return results;
        }

        /// <summary>
        /// 发送自定义协议消息到指定服务器
        /// </summary>
        public async Task<bool> SendCustomMessageAsync(string ip, int port, string command, string data)
        {
            string message = $"cmd={command},data={data}";
            return await SendToServerAsync(ip, port, message);
        }

        /// <summary>
        /// 发送命令到指定服务器（带确认）
        /// </summary>
        public async Task<(bool success, string response)> SendWithResponseAsync(string ip, int port, string message, int timeoutMs = 5000)
        {
            string connectionId = GetConnectionId(ip, port);
            if (string.IsNullOrEmpty(connectionId))
            {
                return (false, "连接不存在");
            }

            var tcs = new TaskCompletionSource<string>();
            string responseMessage = null;

            // 临时订阅消息事件
            EventHandler<TcpClientMessageEventArgs> handler = null;
            handler = (s, e) =>
            {
                if (e.ConnectionId == connectionId)
                {
                    responseMessage = e.Message;
                    tcs.TrySetResult(e.Message);
                    _clientManager.MessageReceived -= handler;
                }
            };

            _clientManager.MessageReceived += handler;

            // 发送消息
            bool sent = await _clientManager.SendAsync(connectionId, message);
            if (!sent)
            {
                _clientManager.MessageReceived -= handler;
                return (false, "发送失败");
            }

            // 等待响应
            var timeoutTask = Task.Delay(timeoutMs);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _clientManager.MessageReceived -= handler;
                return (false, "响应超时");
            }

            return (true, responseMessage);
        }
        #endregion

        #region 查询方法

        /// <summary>
        /// 获取服务器的连接ID
        /// </summary>
        public string GetConnectionId(string ip, int port)
        {
            string addressKey = $"{ip}:{port}";
            _addressToConnectionId.TryGetValue(addressKey, out string connectionId);
            return connectionId;
        }

        /// <summary>
        /// 获取所有连接信息
        /// </summary>
        public List<ConnectionInfo> GetAllConnectionInfos()
        {
            return _connectionInfoMap.Values.ToList();
        }

        /// <summary>
        /// 获取所有已连接的服务器信息
        /// </summary>
        public List<ConnectionInfo> GetConnectedServers()
        {
            var connectedIds = _clientManager.GetAllConnectionInfos()
                .Where(c => c.IsConnected)
                .Select(c => c.ConnectionId)
                .ToHashSet();

            return _connectionInfoMap.Values
                .Where(c => connectedIds.Contains(c.ConnectionId))
                .ToList();
        }

        /// <summary>
        /// 检查服务器是否已连接
        /// </summary>
        public bool IsServerConnected(string ip, int port)
        {
            string connectionId = GetConnectionId(ip, port);
            if (string.IsNullOrEmpty(connectionId))
                return false;

            var conn = _clientManager.GetConnectionInfo(connectionId);
            return conn?.IsConnected ?? false;
        }

        /// <summary>
        /// 根据名称获取连接信息
        /// </summary>
        public ConnectionInfo GetConnectionByName(string name)
        {
            return _connectionInfoMap.Values.FirstOrDefault(c => c.Name == name);
        }
        #endregion

        #region 连接管理
        private void OnConnectionStatusChanged(object sender, TcpConnectionEventArgs e)
        {
            if (_connectionInfoMap.TryGetValue(e.ConnectionId, out var info))
            {
                info.IsConnected = e.IsConnected;
                info.LastStatusChange = DateTime.Now;
                info.StatusMessage = e.Status;
            }
        }

        public void RemoveConnection(string ip, int port)
        {
            string addressKey = $"{ip}:{port}";

            if (_addressToConnectionId.TryRemove(addressKey, out string connectionId))
            {
                _connectionInfoMap.TryRemove(connectionId, out _);
                _clientManager.RemoveConnection(connectionId);
            }
        }

        public void DisconnectAll()
        {
            _clientManager.DisconnectAll();
        }
        public void StopAndClearAll()
        {
            _clientManager.EnableAutoReconnect = false;
            _clientManager.EnableHeartbeat = false;
            _clientManager.ClearAllConnections();
            _addressToConnectionId.Clear();
            _connectionInfoMap.Clear();
        }
        #endregion

    }
}
