using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL.Socket_Client.TCPClientManner
{
    /// <summary>
    /// 连接信息类
    /// </summary>
    public class ConnectionInfo
    {
        public string ConnectionId { get; set; }
        public string Name { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
        public string AddressKey { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastStatusChange { get; set; }
        public string StatusMessage { get; set; }

        public string Endpoint => $"{Ip}:{Port}";

        public override string ToString()
        {
            return $"{Name} ({Endpoint}) - {(IsConnected ? "在线" : "离线")}";
        }
    }
}
