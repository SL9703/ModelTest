using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL.Socket_Client.TCPClientManner
{/// <summary>
 /// TCP客户端连接信息
 /// </summary>
    public class TcpClientConnection
    {
        public string ConnectionId { get; set; }
        public string Name { get; set; }
        public string ServerIp { get; set; }
        public int ServerPort { get; set; }
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public bool IsConnected { get; set; }
        public DateTime ConnectTime { get; set; }
        public DateTime LastActivity { get; set; }
        public long TotalBytesSent { get; set; }
        public long TotalBytesReceived { get; set; }
        public int ReconnectCount { get; set; }
        public string Status { get; set; }
        public Dictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();
    }
}
