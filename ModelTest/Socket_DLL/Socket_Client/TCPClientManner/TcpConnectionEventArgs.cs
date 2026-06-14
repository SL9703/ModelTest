using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL.Socket_Client.TCPClientManner
{
    /// <summary>
    /// 连接状态事件参数
    /// </summary>
    public class TcpConnectionEventArgs : EventArgs
    {
        public string ConnectionId { get; set; }
        public bool IsConnected { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public Exception Error { get; set; }
    }
}
