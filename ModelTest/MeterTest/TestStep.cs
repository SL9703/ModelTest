using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 测试步骤类
    /// </summary>
    internal class TestStep
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public Action ExecuteAction { get; set; }
        public Func<bool> ValidationAction { get; set; }
        public TestStatus Status { get; set; } = TestStatus.NotStarted;
        public string ErrorMessage { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
}
