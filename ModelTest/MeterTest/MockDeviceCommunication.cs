using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// // 模拟下位机通信（用于测试）
    /// </summary>
    internal class MockDeviceCommunication : IDeviceCommunication
    {
        private Random _random = new Random();
        private bool _isConnected = false;

        public bool IsConnected => _isConnected;
        public event EventHandler<string> DataReceived;

        public bool Connect(string ipAddress, int port, int timeout)
        {
            // 模拟连接延迟
            Thread.Sleep(100);
            _isConnected = true;
            return true;
        }

        public void Disconnect()
        {
            _isConnected = false;
        }

        public bool SendCommand(string command)
        {
            if (!_isConnected) return false;

            // 模拟发送延迟
            Thread.Sleep(50);

            // 模拟随机失败（10%概率）
            if (_random.Next(0, 100) < 10)
                return false;

            return true;
        }

        public string SendAndReceive(string command, int timeout)
        {
            if (!_isConnected) return null;

            // 模拟通信延迟
            Thread.Sleep(_random.Next(100, 500));

            // 模拟随机失败（5%概率）
            if (_random.Next(0, 100) < 5)
                throw new Exception("模拟通信错误");

            // 生成模拟响应
            switch (command.ToUpper())
            {
                case "AT":
                    return "OK";
                case "VERSION?":
                    return "FW v1.2.3";
                case "TEST START":
                    return "TEST STARTED";
                case "TEST STOP":
                    return "TEST STOPPED";
                case "READ DATA":
                    return $"DATA: {_random.Next(1000, 9999)}";
                default:
                    return "ACK";
            }
        }
    }
}
