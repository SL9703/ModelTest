using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml;
using Newtonsoft.Json;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 工位配置管理类
    /// </summary>
    internal class WorkStationConfigManager
    {
        private string _configFilePath;

        public WorkStationConfigManager(string configFilePath = "WorkStationConfig.xml")
        {
            _configFilePath = configFilePath;
        }

        public WorkStationConfiguration LoadConfiguration()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var serializer = new XmlSerializer(typeof(WorkStationConfiguration));
                    using (var reader = new StreamReader(_configFilePath))
                    {
                        return (WorkStationConfiguration)serializer.Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置文件失败: {ex.Message}");
            }

            // 返回默认配置
            return CreateDefaultConfiguration();
        }

        public void SaveConfiguration(WorkStationConfiguration config)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(WorkStationConfiguration));
                using (var writer = new StreamWriter(_configFilePath))
                {
                    serializer.Serialize(writer, config);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存配置文件失败: {ex.Message}");
            }
        }

        public void SaveAsJson(WorkStationConfiguration config, string filePath)
        {
            try
            {
                string json = JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存JSON配置失败: {ex.Message}");
            }
        }

        private WorkStationConfiguration CreateDefaultConfiguration()
        {
            var config = new WorkStationConfiguration
            {
                EnableParallelExecution = true,
                MaxParallelWorkStations = 5,
                StopOnFirstFailure = false,
                RetryOnFailure = true,
                MaxRetryCount = 3
            };

            // 添加默认的20个工位
            for (int i = 1; i <= 48; i++)
            {
                config.WorkStations.Add(new WorkStation
                {
                    Id = i,
                    Name = $"表位 {i:D2}",
                    Description = $"测试表位 {i}",
                    IPAddress = $"192.168.127.{130 + i}",
                    Port = 8888,
                    IsSelected = i <= 5, // 默认选择前5个工位
                    Status = WorkStationStatus.Idle
                });
            }

            return config;
        }

        // 导入工位配置
        public List<WorkStation> ImportWorkStationsFromCsv(string csvFilePath)
        {
            var workStations = new List<WorkStation>();

            try
            {
                var lines = File.ReadAllLines(csvFilePath);
                for (int i = 1; i < lines.Length; i++) // 跳过标题行
                {
                    var fields = lines[i].Split(',');
                    if (fields.Length >= 4)
                    {
                        var ws = new WorkStation
                        {
                            Id = int.Parse(fields[0]),
                            Name = fields[1],
                            IPAddress = fields[2],
                            Port = int.Parse(fields[3]),
                            IsSelected = true,
                            Status = WorkStationStatus.Idle
                        };

                        if (fields.Length > 4)
                            ws.Description = fields[4];

                        workStations.Add(ws);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导入CSV失败: {ex.Message}");
            }
            return workStations;
        }
    }
}
