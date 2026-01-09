using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 测试步骤结果类
    /// </summary>
    internal class TestStepResult
    {
        public string StepId { get; set; }
        public string StepName { get; set; }
        public TestStatus Status { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public string CommandSent { get; set; }
        public string ResponseReceived { get; set; }
        public string ErrorMessage { get; set; }
    }
}
