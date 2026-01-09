using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL
{
    /// <summary>
    /// 客户端信息类
    /// </summary>
    public class ClientInfo
    {
        public string Id { get; set; }
        public string Endpoint { get; set; }
        public TcpClient Client { get; set; }
        public DateTime ConnectedTime { get; set; }
        public DateTime LastActivity { get; set; }
        public string CustomTag { get; set; }
    }
}
