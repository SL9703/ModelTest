using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL.Socket_UDP
{
    public class UdpClientEventArgs : EventArgs
    {
        public IPEndPoint ClientEndpoint { get; set; }
        public string ClientId { get; set; }
        public bool IsNewClient { get; set; }
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }

    }
}
