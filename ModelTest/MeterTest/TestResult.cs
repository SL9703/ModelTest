using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// // 测试结果（按工位）
    /// </summary>
    internal class TestResult
    {
        public int WorkStationId { get; set; }
        public string WorkStationName { get; set; }
        public TestPlan TestPlan { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TestStatus OverallStatus { get; set; }
        public List<TestCaseResult> CaseResults { get; set; } = new List<TestCaseResult>();
        public TimeSpan TotalTime => EndTime - StartTime;
        public int PassedCases => CaseResults.Count(r => r.Status == TestStatus.Passed);
        public int TotalCases => CaseResults.Count;
        public string ErrorMessage { get; set; }
    }
}
