using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 工位属性配置类
    /// </summary>
    internal class WorkStationConfiguration
    {
        public List<WorkStation> WorkStations { get; set; } = new List<WorkStation>();
        public bool EnableParallelExecution { get; set; } = true;
        public int MaxParallelWorkStations { get; set; } = 5;
        public bool StopOnFirstFailure { get; set; } = false;
        public bool RetryOnFailure { get; set; } = false;
        public int MaxRetryCount { get; set; } = 3;
    }
}
