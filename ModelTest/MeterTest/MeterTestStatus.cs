using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 测试枚举类
    /// </summary>
    internal class MeterTestStatus
    {
        // 测试状态枚举
        public enum TestStatus
        {
            NotStarted,
            Running,
            Passed,
            Failed,
            Skipped
        }


    }
}
