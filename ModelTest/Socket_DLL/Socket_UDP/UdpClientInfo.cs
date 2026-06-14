using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.Socket_DLL.Socket_UDP
{
    public class UdpClientInfo
    {
        public string ClientId { get; set; }
        public string CustomId { get; set; }
        public IPEndPoint Endpoint { get; set; }
        public DateTime FirstSeen { get; set; }
        public DateTime LastActivity { get; set; }
        public long TotalMessages { get; set; }
        public long TotalBytes { get; set; }
        public string LastMessage { get; set; }
        public Dictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();

        public TimeSpan InactiveTime => DateTime.Now - LastActivity;
        public string DisplayName => CustomId ?? ClientId;
    }
}
