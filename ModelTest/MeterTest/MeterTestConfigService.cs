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
    /// 默认包含源控制配置、控制 PCB 到工位的映射，以及通信测试、日计时、起动试验、潜动试验和常数试验测试项。
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
                            Description = "升源后执行串口服务器波特率检查，最后读取表位地址",
                            TestSubItems =
                            {
                                new MeterTestSubItem
                                {
                                    Name = "串口服务器连接",
                                    Enabled = true,
                                    Description = "连接各 IP 的串口服务器管理端 64444；失败后继续后续步骤。",
                                    Protocol = "MeterControlPcbV2",
                                    ExecutionMode = MeterTestExecutionMode.SerialPortServerBaudRateSync.ToString(),
                                    SerialPortServerStep = "Connect",
                                    SourceControlConfig = "三相默认源",
                                    TimeoutMs = 5000
                                },
                                new MeterTestSubItem
                                {
                                    Name = "读取串口参数",
                                    Enabled = true,
                                    Description = "读取串口服务器全部 COM 参数，并映射到 951 起始的端口。",
                                    Protocol = "MeterControlPcbV2",
                                    ExecutionMode = MeterTestExecutionMode.SerialPortServerBaudRateSync.ToString(),
                                    SerialPortServerStep = "ReadParameters",
                                    TimeoutMs = 5000
                                },
                                new MeterTestSubItem
                                {
                                    Name = "校验工位波特率",
                                    Enabled = true,
                                    Description = "将读取结果与资产信息中的 IP、Port、波特率配置逐工位比对。",
                                    Protocol = "MeterControlPcbV2",
                                    ExecutionMode = MeterTestExecutionMode.SerialPortServerBaudRateSync.ToString(),
                                    SerialPortServerStep = "Compare",
                                    TimeoutMs = 5000
                                },
                                new MeterTestSubItem
                                {
                                    Name = "修改不一致波特率",
                                    Enabled = true,
                                    Description = "仅修改不一致的端口，使用立即生效参数，不发送重启报文。",
                                    Protocol = "MeterControlPcbV2",
                                    ExecutionMode = MeterTestExecutionMode.SerialPortServerBaudRateSync.ToString(),
                                    SerialPortServerStep = "Apply",
                                    TimeoutMs = 5000
                                },
                                new MeterTestSubItem
                                {
                                    Name = "地址读取",
                                    Enabled = true,
                                    Description = "按工位电表地址定址读取电表地址，运行时自动重新计算 HCS/FCS。",
                                    Protocol = "DLT698.45",
                                    ExecutionMode = MeterTestExecutionMode.StationTcp.ToString(),
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
                            Description = "通过控制PCB执行三轮多表位日计时并计算平均误差",
                            TestSubItems = CreateDailyTimingSubItems()
                        },
                        new MeterTestItem
                        {
                            Name = "起动试验",
                            Description = "升源后执行起动误差试验，当前先预置流程节点，具体报文待接入。",
                            TestSubItems = CreateStartingTestSubItems()
                        },
                        new MeterTestItem
                        {
                            Name = "潜动试验",
                            Description = "升潜动电压后执行潜动脉冲试验，当前先预置流程节点，具体报文待接入。",
                            TestSubItems = CreateCreepingTestSubItems()
                        },
                        new MeterTestItem
                        {
                            Name = "常数试验",
                            Description = "常数试验流程暂定，等待协议和判定规则确认。"
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// 创建起动试验的预置流程。
    /// 当前节点只用于方案展示，待接入源控制、0x21/0x38 误差试验和结果判定协议后再启用。
    /// </summary>
    private static List<MeterTestSubItem> CreateStartingTestSubItems()
    {
        return new List<MeterTestSubItem>
        {
            CreatePlannedTestSubItem("升源（启动电流）", "按资产信息和起动电流参数升源。"),
            CreatePlannedTestSubItem("开启起动试验（启动误差）", "发送起动误差试验启动报文。"),
            CreatePlannedTestSubItem("等待起动时间", "按启动时间计算结果等待测试完成。"),
            CreatePlannedTestSubItem("读取误差结果", "读取并解析起动试验误差结果。"),
            CreatePlannedTestSubItem("判断误差结果", "根据起动试验最大允许误差判定结果。")
        };
    }

    /// <summary>
    /// 创建潜动试验的预置流程。
    /// 当前节点只用于方案展示，待接入潜动电压、脉冲读取和判定协议后再启用。
    /// </summary>
    private static List<MeterTestSubItem> CreateCreepingTestSubItems()
    {
        return new List<MeterTestSubItem>
        {
            CreatePlannedTestSubItem("升源（潜动电压）", "按规程升至潜动试验电压。"),
            CreatePlannedTestSubItem("开启潜动试验", "发送潜动试验启动报文。"),
            CreatePlannedTestSubItem("等待潜动时间", "按潜动时间计算结果等待测试完成。"),
            CreatePlannedTestSubItem("读取脉冲数量", "读取潜动期间被测表输出的脉冲数量。"),
            CreatePlannedTestSubItem("判断脉冲结果", "根据潜动允许脉冲数量判定结果。")
        };
    }

    /// <summary>
    /// 创建一个暂未接入执行器的方案节点。
    /// Enabled=false 可保证当前执行方案时不会发送空报文或误判失败。
    /// </summary>
    private static MeterTestSubItem CreatePlannedTestSubItem(string name, string description)
    {
        return new MeterTestSubItem
        {
            Name = name,
            Enabled = false,
            Protocol = "Pending",
            ExecutionMode = MeterTestExecutionMode.Planned.ToString(),
            Description = description,
            TimeoutMs = 5000
        };
    }

    /// <summary>
    /// 创建默认的三轮日计时方案树节点。
    /// 每一轮固定展示：开始试验 -> 延迟等待倒计时 -> 读取结果。
    /// </summary>
    private static List<MeterTestSubItem> CreateDailyTimingSubItems()
    {
        List<MeterTestSubItem> subItems = new();
        for (int round = 1; round <= 3; round++)
        {
            string sourceControlConfig = round == 1 ? "三相默认源" : string.Empty;
            subItems.Add(CreateDailyTimingSubItem(
                $"开始日计时实验-第{round}次",
                "Start",
                round,
                sourceControlConfig,
                "开始本轮日计时试验。"));
            subItems.Add(CreateDailyTimingSubItem(
                $"延迟等待110秒倒计时-第{round}次",
                "Wait",
                round,
                string.Empty,
                "等待日计时试验完成，并显示倒计时。"));
            subItems.Add(CreateDailyTimingSubItem(
                $"读取日计时结果-第{round}次",
                "Read",
                round,
                string.Empty,
                "读取本轮日计时结果，解析 float 误差值。"));
        }

        return subItems;
    }

    /// <summary>
    /// 创建一个日计时流程小项。
    /// </summary>
    private static MeterTestSubItem CreateDailyTimingSubItem(
        string name,
        string step,
        int round,
        string sourceControlConfig,
        string description)
    {
        return new MeterTestSubItem
        {
            Name = name,
            Enabled = true,
            Description = description,
            Protocol = "MeterControlPcb",
            ExecutionMode = MeterTestExecutionMode.ControlPcbDailyTiming.ToString(),
            ControlPcbGroup = string.Empty,
            SourceControlConfig = sourceControlConfig,
            DailyTimingStep = step,
            DailyTimingRound = round,
            ResponseParser = ResponseParserType.MeterControlDailyTiming.ToString(),
            DailyTimingTime = 10,
            DailyTimingCount = 10,
            PacketIntervalMs = 100,
            TimeoutMs = 5000
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
