using System;
using System.IO;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

public sealed class MeterTestConfigService
{
    private readonly XmlSerializer serializer = new(typeof(MeterTestPlanConfig));

    public MeterTestPlanConfig LoadOrCreate(string configPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory);

        if (!File.Exists(configPath))
        {
            MeterTestPlanConfig defaultConfig = CreateDefault();
            Save(configPath, defaultConfig);
            return defaultConfig;
        }

        using FileStream stream = File.OpenRead(configPath);
        return serializer.Deserialize(stream) as MeterTestPlanConfig ?? CreateDefault();
    }

    public void Save(string configPath, MeterTestPlanConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory);

        using FileStream stream = File.Create(configPath);
        serializer.Serialize(stream, config);
    }

    private static MeterTestPlanConfig CreateDefault()
    {
        return new MeterTestPlanConfig
        {
            Schemes =
            {
                new MeterTestScheme
                {
                    Name = "默认方案",
                    Description = "MeterTest 默认测试方案",
                    TestItems =
                    {
                        new MeterTestItem
                        {
                            Name = "通信测试",
                            Description = "一发一收通信验证",
                            TestSubItems =
                            {
                                new MeterTestSubItem
                                {
                                    Name = "表位通信",
                                    Description = "测试表位通信是否正常",
                                    RequestHex = "68 01 00 16",
                                    ExpectedResponse = "68 01 00 16",
                                    MatchMode = ResponseMatchMode.Contains.ToString(),
                                    TimeoutMs = 3000,
                                    MockResponse = "68 01 00 16"
                                },
                                new MeterTestSubItem
                                {
                                    Name = "地址读取",
                                    Description = "读取电表地址",
                                    RequestHex = "68 AA BB 16",
                                    ExpectedResponse = "68 AA BB 16",
                                    MatchMode = ResponseMatchMode.Contains.ToString(),
                                    TimeoutMs = 5000,
                                    MockResponse = "68 AA BB 16"
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
