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
    public class ClientStatusChangedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public string ClientEndpoint { get; set; }
        public bool IsConnected { get; set; }
        public DateTime ChangeTime { get; set; }
    }
    public delegate void ClientStatusChangedHandler(object sender, ClientStatusChangedEventArgs e);
}
