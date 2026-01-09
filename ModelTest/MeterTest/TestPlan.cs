using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 测试方案
    /// </summary>
    internal class TestPlan
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
        public TestStatus Status { get; set; } = TestStatus.NotStarted;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TotalExecutionTime => EndTime - StartTime;
    }
}
