using System.Net;

namespace ModelTest.Socket_DLL.Socket_UDP
{
    public class UdpMessageEventArgs : EventArgs
    {
        public IPEndPoint ClientEndpoint { get; set; }
        public string ClientId { get; set; }
        public string Message { get; set; }
        public byte[] RawData { get; set; }
        public DateTime Timestamp { get; set; }
        public int DataLength => RawData?.Length ?? 0;
    }
}
