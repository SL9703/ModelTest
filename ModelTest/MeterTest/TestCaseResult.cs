using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 测试用例结果类
    /// </summary>
    internal class TestCaseResult
    {
        public string TestCaseId { get; set; }
        public string TestCaseName { get; set; }
        public TestStatus Status { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public List<TestStepResult> StepResults { get; set; } = new List<TestStepResult>();
        public string ErrorMessage { get; set; }
    }
}
