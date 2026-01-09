using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.MeterTest
{
    internal interface IDeviceCommunication
    {
        bool Connect(string ipAddress, int port, int timeout);
        void Disconnect();
        bool SendCommand(string command);
        string SendAndReceive(string command, int timeout);
        bool IsConnected { get; }
        event EventHandler<string> DataReceived;
    }
}
