using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL.Socket_Client
{
    // 事件参数类
    public class TcpClientMessageEventArgs : EventArgs
    {
        public string Message { get; set; }
        public byte[] RawData { get; set; }
        public DateTime Timestamp { get; set; }
        public MessageDirection Direction { get; set; }
        public int DataLength => RawData?.Length ?? 0;

        public enum MessageDirection
        {
            Received,
            Sent
        }
    }

}
