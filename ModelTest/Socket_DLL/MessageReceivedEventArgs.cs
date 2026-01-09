using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL
{
    /// <summary>
    /// TCP通信相关的事件和委托定义
    /// </summary>
    public class MessageReceivedEventArgs: EventArgs
    {
        public string ClientId { get; set; }
        public string ClientEndpoint { get; set; }
        public string Message { get; set; }
        public DateTime ReceivedTime { get; set; }
    }
    public delegate void MessageReceivedHandler(object sender, MessageReceivedEventArgs e);

}
