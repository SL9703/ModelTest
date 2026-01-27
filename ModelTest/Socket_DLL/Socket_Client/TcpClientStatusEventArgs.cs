using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL.Socket_Client
{
    /// <summary>
    /// 状态相关参数
    /// </summary>
    public class TcpClientStatusEventArgs : EventArgs
    {
        public bool IsConnected { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public Exception Error { get; set; }
        public int ReconnectAttempts { get; set; }
    }
}
