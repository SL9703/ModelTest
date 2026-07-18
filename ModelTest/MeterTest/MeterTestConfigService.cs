using System;
using System.IO;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 测试方案 XML 配置读写服务。
/// 负责在程序目录下加载 MeterTestPlanConfig.xml；当配置不存在时自动生成默认方案。
/// </summary>
public sealed class MeterTestConfigService
{
    /// <summary>
    /// XmlSerializer 复用成本较高，服务实例内缓存一个序列化器即可。
    /// </summary>
    private readonly XmlSerializer serializer = new(typeof(MeterTestPlanConfig));

    /// <summary>
    /// 加载测试方案配置；如果文件不存在，则创建默认配置并保存到指定路径。
    /// </summary>
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

    /// <summary>
    /// 将测试方案配置保存为 XML 文件，供现场按配置方式维护测试流程。
    /// </summary>
    public void Save(string configPath, MeterTestPlanConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory);

        using FileStream stream = File.Create(configPath);
        serializer.Serialize(stream, config);
    }

    /// <summary>
    /// 创建默认测试配置。
    /// 默认包含源控制配置、控制 PCB 到工位的映射，以及通信测试/日计时两个测试项。
    /// </summary>
    private static MeterTestPlanConfig CreateDefault()
    {
        return new MeterTestPlanConfig
        {
            SourceControlConfigs =
            {
                CreateSourceControlConfig("单相默认源", MeterTestSourcePhaseMode.SinglePhase, "220", "5"),
                CreateSourceControlConfig("三相默认源", MeterTestSourcePhaseMode.ThreePhase, "220", "5")
            },
            ControlPcbGroups =
            {
                CreateControlPcbGroup(1, 4001, 1, 3),
                CreateControlPcbGroup(2, 4002, 4, 6),
                CreateControlPcbGroup(3, 4003, 7, 9),
                CreateControlPcbGroup(4, 4004, 10, 12),
                CreateControlPcbGroup(5, 4005, 13, 15),
                CreateControlPcbGroup(6, 4006, 16, 18),
                CreateControlPcbGroup(7, 4007, 19, 20)
            },
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
                                    Name = "地址读取",
                                    Enabled = true,
                                    Description = "读取电表地址",
                                    Protocol = "DLT698.45",
                                    ExecutionMode = MeterTestExecutionMode.StationTcp.ToString(),
                                    SourceControlConfig = "三相默认源",
                                    RequestHex = "68 17 00 43 05 AA AA AA AA AA AA 10 2B 3A 05 01 71 40 01 02 00 00 C7 C2 16",
                                    ResponseParser = ResponseParserType.Sgcc698BroadcastAddress.ToString(),
                                    ExpectedApdu = "85 01",
                                    ExpectedOad = "40 01 02 00",
                                    ExpectedDataType = "09",
                                    ExpectedDataLength = 6,
                                    ResultField = "MeterAddress",
                                    ExpectedResponse = string.Empty,
                                    MatchMode = ResponseMatchMode.Contains.ToString(),
                                    TimeoutMs = 5000,
                                    MockResponse = "68 21 00 C3 05 96 81 32 02 00 90 A0 F5 F6 85 01 00 40 01 02 00 01 09 06 90 00 02 32 81 96 00 00 BA 13 16"
                                }
                            }
                        },
                        new MeterTestItem
                        {
                            Name = "日计时",
                            Description = "通过控制PCB执行多表位日计时",
                            TestSubItems =
                            {
                                new MeterTestSubItem
                                {
                                    Name = "日计时",
                                    Enabled = true,
                                    Description = "控制PCB日计时：开始、等待、结果获取；表位范围由 ControlPcbGroups 配置决定",
                                    Protocol = "MeterControlPcb",
                                    ExecutionMode = MeterTestExecutionMode.ControlPcbDailyTiming.ToString(),
                                    ControlPcbGroup = string.Empty,
                                    SourceControlConfig = "三相默认源",
                                    ResponseParser = ResponseParserType.MeterControlDailyTiming.ToString(),
                                    DailyTimingTime = 10,
                                    DailyTimingCount = 10,
                                    PacketIntervalMs = 100,
                                    TimeoutMs = 5000
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// 创建一组控制 PCB 映射配置。
    /// 默认规则为一个 PCB 控制一个连续工位范围，表位地址从起始工位递增。
    /// </summary>
    private static MeterTestControlPcbGroup CreateControlPcbGroup(int index, int port, int stationStart, int stationEnd)
    {
        return new MeterTestControlPcbGroup
        {
            Name = $"控制PCB-{index}",
            Enabled = true,
            Ip = "192.168.127.101",
            Port = port,
            ProtocolVersion = MeterControlPcbProtocolVersion.V2.ToString(),
            StationStart = stationStart,
            StationEnd = stationEnd,
            MeterAddressStart = stationStart
        };
    }

    /// <summary>
    /// 创建升源默认配置。
    /// 单相默认 A 相输出；三相默认 H 相输出，即 ABC 三相同电压同电流。
    /// </summary>
    private static MeterTestSourceControlConfig CreateSourceControlConfig(
        string name,
        MeterTestSourcePhaseMode phaseMode,
        string voltage,
        string current)
    {
        return new MeterTestSourceControlConfig
        {
            Name = name,
            Enabled = true,
            PhaseMode = phaseMode.ToString(),
            InterfaceType = MeterTestSourceInterfaceType.AnyUIOutput.ToString(),
            SourcePort = 1,
            OpenCommBeforeOutput = true,
            Voltage = voltage,
            Current = current,
            CurrentAngleA = "0",
            CurrentAngleB = "0",
            CurrentAngleC = "0",
            Uab = "120",
            Uac = "240",
            Phase = phaseMode == MeterTestSourcePhaseMode.SinglePhase ? "A" : "H",
            PowerFactor = "1.0",
            Pulse = 2,
            Description = phaseMode == MeterTestSourcePhaseMode.SinglePhase
                ? "单相默认升源配置，默认 A 相输出"
                : "三相默认升源配置，ABC 三相同电压同电流输出"
        };
    }
}
