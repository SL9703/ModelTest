using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 测试用例类
    /// </summary>
    internal class TestCase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TestStep> Steps { get; set; } = new List<TestStep>();
        public TestStatus Status { get; set; } = TestStatus.NotStarted;
        public TimeSpan ExecutionTime { get; set; }
        public bool IsEnabled { get; set; } = true;

    }
}
