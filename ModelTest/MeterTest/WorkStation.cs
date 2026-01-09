using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// // 工位信息
    /// </summary>
    internal class WorkStation
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public WorkStationStatus Status { get; set; } = WorkStationStatus.Idle;
        public string IPAddress { get; set; }
        public int Port { get; set; } = 502;
        public int Timeout { get; set; } = 5000;
        public bool IsSelected { get; set; } = true;
        public TestPlan CurrentTestPlan { get; set; }
        public TestResult TestResult { get; set; }
        public DateTime LastCommunication { get; set; }
        public bool IsConnected { get; set; }
        public string ConnectionMessage { get; set; }
        // 下位机通信接口
        public IDeviceCommunication DeviceInterface { get; set; }
    }
    // 工位状态枚举
    public enum WorkStationStatus
    {
        Idle,           // 空闲
        Busy,           // 忙碌
        Testing,        // 测试中
        Completed,      // 完成
        Error,          // 错误
        Disabled        // 禁用
    }

}
