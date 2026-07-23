using System;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 测试方案 XML 配置读写服务。
/// 负责在程序目录下加载 MeterTestPlanConfig.xml；当配置不存在时自动生成默认方案。
/// </summary>
public sealed class MeterTestConfigService
{
    private const int MaximumStationCount = 48;
    private const int StationsPerControlPcb = 3;

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

        MeterTestPlanConfig config;
        using (FileStream stream = File.OpenRead(configPath))
        {
            config = serializer.Deserialize(stream) as MeterTestPlanConfig ?? CreateDefault();
        }

        // 兼容旧版20工位配置：保留现场已有通信参数，只补齐缺失的21-48工位映射。
        bool configChanged = EnsureControlPcbGroups(config);
        configChanged |= EnsureBluetoothTcpChannels(config);
        configChanged |= EnsureCreepingSourceSubItem(config);
        configChanged |= EnsureCreepingProtocolSubItems(config);
        configChanged |= EnsureBasicErrorTestItems(config);
        configChanged |= EnsureBluetoothInterfaceTestItem(config);
        if (configChanged)
        {
            Save(configPath, config);
        }

        return config;
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
            BenchTypeSwitchConfig = new MeterTestBenchTypeSwitchConfig
            {
                Enabled = true,
                Ip = "192.168.127.101",
                Port = 4001,
                TimeoutMs = 5000,
                DelayAfterSuccessMs = 1000
            },
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
                CreateControlPcbGroup(7, 4007, 19, 21),
                CreateControlPcbGroup(8, 4008, 22, 24),
                CreateControlPcbGroup(9, 4009, 25, 27),
                CreateControlPcbGroup(10, 4010, 28, 30),
                CreateControlPcbGroup(11, 4011, 31, 33),
                CreateControlPcbGroup(12, 4012, 34, 36),
                CreateControlPcbGroup(13, 4013, 37, 39),
                CreateControlPcbGroup(14, 4014, 40, 42),
                CreateControlPcbGroup(15, 4015, 43, 45),
                CreateControlPcbGroup(16, 4016, 46, 48)
            },
            // 蓝牙通道不得复用资产信息的485端点。默认只生成禁用占位项，由现场填写后启用。
            BluetoothTcpChannels = CreateDefaultBluetoothTcpChannels(),
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
                        CreateBluetoothInterfaceTestItem(),
                        new MeterTestItem
                        {
                            Name = "日计时",
                            Description = "通过控制PCB执行三轮多表位日计时并计算平均误差",
                            TestSubItems = CreateDailyTimingSubItems()
                        },
                        new MeterTestItem
                        {
                            Name = "起动试验",
                            Description = "按启动电流升源后，通过控制PCB设置常数并启动0x38起动误差试验。",
                            TestSubItems = CreateStartingTestSubItems()
                        },
                        new MeterTestItem
                        {
                            Name = "潜动试验",
                            Description = "按资产额定电压的1.1倍升潜动电压，后续脉冲试验流程继续按配置接入。",
                            TestSubItems = CreateCreepingTestSubItems()
                        },
                        CreateBasicErrorTestItem("基本误差-正向有功", "正有", "ForwardActive"),
                        CreateBasicErrorTestItem("基本误差-反向有功", "反有", "ReverseActive"),
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
    /// 为配置补齐1-48工位蓝牙专用TCP通道占位项。
    /// 已有工位配置原样保留；新增项默认禁用且端点为空，防止误用资产信息中的485参数。
    /// </summary>
    private static bool EnsureBluetoothTcpChannels(MeterTestPlanConfig config)
    {
        config.BluetoothTcpChannels ??= new List<MeterTestBluetoothTcpChannel>();
        bool changed = false;
        for (int station = 1; station <= MaximumStationCount; station++)
        {
            if (config.BluetoothTcpChannels.Any(channel => channel.Station == station))
                continue;

            config.BluetoothTcpChannels.Add(CreateBluetoothTcpChannel(station));
            changed = true;
        }

        return changed;
    }

    /// <summary>创建1-48工位的蓝牙专用TCP空配置。</summary>
    private static List<MeterTestBluetoothTcpChannel> CreateDefaultBluetoothTcpChannels() =>
        Enumerable.Range(1, MaximumStationCount)
            .Select(CreateBluetoothTcpChannel)
            .ToList();

    /// <summary>创建单个禁用的蓝牙通道占位项。</summary>
    private static MeterTestBluetoothTcpChannel CreateBluetoothTcpChannel(int station) => new()
    {
        Station = station,
        Enabled = false,
        Ip = string.Empty,
        Port = 0
    };

    /// <summary>
    /// 确保控制 PCB 映射覆盖1-48工位。
    /// 已存在的组保留现场 IP、Port 和协议版本，只扩展不完整范围或添加缺失分组。
    /// </summary>
    private static bool EnsureControlPcbGroups(MeterTestPlanConfig config)
    {
        bool changed = false;
        for (int stationStart = 1; stationStart <= MaximumStationCount; stationStart += StationsPerControlPcb)
        {
            int stationEnd = Math.Min(stationStart + StationsPerControlPcb - 1, MaximumStationCount);
            int groupIndex = (stationStart - 1) / StationsPerControlPcb + 1;

            MeterTestControlPcbGroup? existingGroup = config.ControlPcbGroups.FirstOrDefault(group =>
                group.StationStart == stationStart ||
                string.Equals(group.Name, $"控制PCB-{groupIndex}", StringComparison.OrdinalIgnoreCase));
            if (existingGroup is null)
            {
                config.ControlPcbGroups.Add(
                    CreateControlPcbGroup(groupIndex, 4000 + groupIndex, stationStart, stationEnd));
                changed = true;
                continue;
            }

            if (existingGroup.StationEnd < stationEnd)
            {
                existingGroup.StationEnd = stationEnd;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// 将旧配置中的“升源（潜动电压）”预置节点迁移为可执行的1.1倍额定电压控源节点。
    /// </summary>
    private static bool EnsureCreepingSourceSubItem(MeterTestPlanConfig config)
    {
        MeterTestSubItem? subItem = config.Schemes
            .SelectMany(scheme => scheme.TestItems)
            .Where(item => item.Name.Equals("潜动试验", StringComparison.OrdinalIgnoreCase))
            .SelectMany(item => item.TestSubItems)
            .FirstOrDefault(item => item.Name.Equals("升源（潜动电压）", StringComparison.OrdinalIgnoreCase));
        if (subItem is null)
            return false;

        string executionMode = MeterTestExecutionMode.CreepingSource.ToString();
        const string description = "从资产信息读取额定电压，按1.1倍计算潜动电压；单相输出Ua，三相输出Ua/Ub/Uc，电流为0。";
        bool changed = !subItem.Enabled ||
            !subItem.Protocol.Equals("XYCtr", StringComparison.OrdinalIgnoreCase) ||
            !subItem.ExecutionMode.Equals(executionMode, StringComparison.OrdinalIgnoreCase) ||
            !subItem.SourceControlConfig.Equals("单相默认源", StringComparison.OrdinalIgnoreCase) ||
            subItem.TimeoutMs != 20000 ||
            !subItem.Description.Equals(description, StringComparison.Ordinal);
        if (!changed)
            return false;

        subItem.Enabled = true;
        subItem.Protocol = "XYCtr";
        subItem.ExecutionMode = executionMode;
        subItem.SourceControlConfig = "单相默认源";
        subItem.Description = description;
        subItem.TimeoutMs = 20000;
        return true;
    }

    /// <summary>
    /// 把旧配置中的潜动启动和等待预置节点迁移为可执行节点。
    /// 已由现场手动配置的脉冲数和等待秒数会保留，只修正无效值和执行模式。
    /// </summary>
    private static bool EnsureCreepingProtocolSubItems(MeterTestPlanConfig config)
    {
        MeterTestItem? creepingItem = config.Schemes
            .SelectMany(scheme => scheme.TestItems)
            .FirstOrDefault(item => item.Name.Equals("潜动试验", StringComparison.OrdinalIgnoreCase));
        if (creepingItem is null)
            return false;

        bool changed = false;
        MeterTestSubItem? startItem = creepingItem.TestSubItems
            .FirstOrDefault(item => item.Name.Equals("开启潜动试验", StringComparison.OrdinalIgnoreCase));
        if (startItem is not null)
        {
            string executionMode = MeterTestExecutionMode.ControlPcbCreepingStart.ToString();
            string responseParser = ResponseParserType.MeterControlCreepingTest.ToString();
            if (!startItem.Enabled ||
                !startItem.Protocol.Equals("MeterControlPcbV2", StringComparison.OrdinalIgnoreCase) ||
                !startItem.ExecutionMode.Equals(executionMode, StringComparison.OrdinalIgnoreCase) ||
                !startItem.ResponseParser.Equals(responseParser, StringComparison.OrdinalIgnoreCase))
            {
                startItem.Enabled = true;
                startItem.Protocol = "MeterControlPcbV2";
                startItem.ExecutionMode = executionMode;
                startItem.ResponseParser = responseParser;
                startItem.Description = "按工位发送0x35+00+脉冲数+小端4字节时间，收到完整回显的工位继续后续流程。";
                changed = true;
            }

            if (startItem.CreepingPulseCount is < 1 or > byte.MaxValue)
            {
                startItem.CreepingPulseCount = 1;
                changed = true;
            }

            if (startItem.CreepingTimeSeconds < 1)
            {
                startItem.CreepingTimeSeconds = 60;
                changed = true;
            }

            if (startItem.PacketIntervalMs < 0)
            {
                startItem.PacketIntervalMs = 100;
                changed = true;
            }

            if (startItem.TimeoutMs < 100)
            {
                startItem.TimeoutMs = 5000;
                changed = true;
            }
        }

        MeterTestSubItem? waitItem = creepingItem.TestSubItems
            .FirstOrDefault(item => item.Name.Equals("等待潜动时间", StringComparison.OrdinalIgnoreCase));
        if (waitItem is not null)
        {
            string executionMode = MeterTestExecutionMode.CreepingWait.ToString();
            if (!waitItem.Enabled ||
                !waitItem.Protocol.Equals("ConfiguredDelay", StringComparison.OrdinalIgnoreCase) ||
                !waitItem.ExecutionMode.Equals(executionMode, StringComparison.OrdinalIgnoreCase))
            {
                waitItem.Enabled = true;
                waitItem.Protocol = "ConfiguredDelay";
                waitItem.ExecutionMode = executionMode;
                waitItem.Description = "按creepingTimeSeconds手动配置值等待，只记录倒计时开始和结束。";
                waitItem.TimeoutMs = 0;
                changed = true;
            }

            if (waitItem.CreepingTimeSeconds < 1)
            {
                waitItem.CreepingTimeSeconds = startItem?.CreepingTimeSeconds > 0
                    ? startItem.CreepingTimeSeconds
                    : 60;
                changed = true;
            }
        }

        MeterTestSubItem? readItem = creepingItem.TestSubItems
            .FirstOrDefault(item => item.Name.Equals("读取脉冲数量", StringComparison.OrdinalIgnoreCase));
        if (readItem is not null)
        {
            string executionMode = MeterTestExecutionMode.ControlPcbCreepingRead.ToString();
            string responseParser = ResponseParserType.MeterControlCreepingTest.ToString();
            if (!readItem.Enabled ||
                !readItem.Protocol.Equals("MeterControlPcbV2", StringComparison.OrdinalIgnoreCase) ||
                !readItem.ExecutionMode.Equals(executionMode, StringComparison.OrdinalIgnoreCase) ||
                !readItem.ResponseParser.Equals(responseParser, StringComparison.OrdinalIgnoreCase))
            {
                readItem.Enabled = true;
                readItem.Protocol = "MeterControlPcbV2";
                readItem.ExecutionMode = executionMode;
                readItem.ResponseParser = responseParser;
                readItem.Description = "按工位发送0x35+AA，解析当前累计脉冲数和累计时间。";
                changed = true;
            }

            if (readItem.PacketIntervalMs < 0)
            {
                readItem.PacketIntervalMs = 100;
                changed = true;
            }

            if (readItem.TimeoutMs < 100)
            {
                readItem.TimeoutMs = 5000;
                changed = true;
            }
        }

        MeterTestSubItem? judgeItem = creepingItem.TestSubItems
            .FirstOrDefault(item => item.Name.Equals("判断脉冲结果", StringComparison.OrdinalIgnoreCase));
        if (judgeItem is not null)
        {
            string executionMode = MeterTestExecutionMode.CreepingPulseJudge.ToString();
            if (!judgeItem.Enabled ||
                !judgeItem.Protocol.Equals("JJG596-2026", StringComparison.OrdinalIgnoreCase) ||
                !judgeItem.ExecutionMode.Equals(executionMode, StringComparison.OrdinalIgnoreCase) ||
                judgeItem.TimeoutMs != 0)
            {
                judgeItem.Enabled = true;
                judgeItem.Protocol = "JJG596-2026";
                judgeItem.ExecutionMode = executionMode;
                judgeItem.Description = "累计脉冲数小于等于1判定合格，0个或1个均合格。";
                judgeItem.TimeoutMs = 0;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>
    /// 为已有方案补齐正向、反向有功各18个基本误差测试点。
    /// 已存在同名测试点保持现场自定义参数不变，只补充缺失节点。
    /// </summary>
    private static bool EnsureBasicErrorTestItems(MeterTestPlanConfig config)
    {
        bool changed = false;
        foreach (MeterTestScheme scheme in config.Schemes)
        {
            changed |= EnsureBasicErrorTestItem(
                scheme,
                CreateBasicErrorTestItem("基本误差-正向有功", "正有", "ForwardActive"));
            changed |= EnsureBasicErrorTestItem(
                scheme,
                CreateBasicErrorTestItem("基本误差-反向有功", "反有", "ReverseActive"));
        }

        return changed;
    }

    private static bool EnsureBasicErrorTestItem(MeterTestScheme scheme, MeterTestItem expectedItem)
    {
        MeterTestItem? existingItem = scheme.TestItems.FirstOrDefault(item =>
            item.Name.Equals(expectedItem.Name, StringComparison.OrdinalIgnoreCase));
        if (existingItem is null)
        {
            scheme.TestItems.Add(expectedItem);
            return true;
        }

        bool changed = false;
        foreach (MeterTestSubItem expectedSubItem in expectedItem.TestSubItems)
        {
            if (existingItem.TestSubItems.Any(item =>
                    item.Name.Equals(expectedSubItem.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            existingItem.TestSubItems.Add(expectedSubItem);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 为已有现场配置补齐蓝牙接口检测及四个子项。
    /// 已存在的Planned占位节点会自动升级为BluetoothStationTcp执行节点。
    /// </summary>
    private static bool EnsureBluetoothInterfaceTestItem(MeterTestPlanConfig config)
    {
        bool changed = false;
        MeterTestItem expectedItem = CreateBluetoothInterfaceTestItem();
        foreach (MeterTestScheme scheme in config.Schemes)
        {
            MeterTestItem? existingItem = scheme.TestItems.FirstOrDefault(item =>
                item.Name.Equals(expectedItem.Name, StringComparison.OrdinalIgnoreCase));
            if (existingItem is null)
            {
                scheme.TestItems.Add(CreateBluetoothInterfaceTestItem());
                changed = true;
                continue;
            }

            foreach (MeterTestSubItem expectedSubItem in expectedItem.TestSubItems)
            {
                MeterTestSubItem? existingSubItem = existingItem.TestSubItems.FirstOrDefault(item =>
                    item.Name.Equals(expectedSubItem.Name, StringComparison.OrdinalIgnoreCase));
                if (existingSubItem is null)
                {
                    existingItem.TestSubItems.Add(expectedSubItem);
                    changed = true;
                    continue;
                }

                if (existingSubItem.ExecutionMode.Equals(
                        MeterTestExecutionMode.Planned.ToString(),
                        StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(existingSubItem.BluetoothStep))
                {
                    CopyBluetoothExecutionDefaults(existingSubItem, expectedSubItem);
                    changed = true;
                }
            }
        }

        return changed;
    }

    /// <summary>
    /// 创建起动试验流程。
    /// 第一项负责启动电流升源，第二项通过控制PCB启动0x38误差试验，第三项计算Tst并等待。
    /// </summary>
    private static List<MeterTestSubItem> CreateStartingTestSubItems()
    {
        return new List<MeterTestSubItem>
        {
            new MeterTestSubItem
            {
                Name = "升源（启动电流）",
                Enabled = true,
                Protocol = "XYCtr",
                ExecutionMode = MeterTestExecutionMode.StartingSource.ToString(),
                SourceControlConfig = "单相默认源",
                Description = "按资产信息的接入方式、有功等级和基本电流计算 Ist，并使用 Ist 初始化后升源。",
                TimeoutMs = 20000
            },
            new MeterTestSubItem
            {
                Name = "开启起动试验（启动误差）",
                Enabled = true,
                Protocol = "MeterControlPcbV2",
                ExecutionMode = MeterTestExecutionMode.ControlPcbStartingError.ToString(),
                ControlPcbGroup = string.Empty,
                BasicErrorPulseCount = 2,
                BasicErrorTestCount = 1,
                BasicErrorPulseType = 0,
                PacketIntervalMs = 100,
                Description = "读取标准表脉冲常数，按工位依次下发A2标准表常数、A0电能表有功常数和0x38开始试验。",
                TimeoutMs = 5000
            },
            new MeterTestSubItem
            {
                Name = "等待起动时间",
                Enabled = true,
                Protocol = "JJG596-2026",
                ExecutionMode = MeterTestExecutionMode.StartingTimeWait.ToString(),
                Description = "按资产信息计算各工位Tst上限，并按最大Tst向上取整后统一等待。",
                TimeoutMs = 0
            },
            new MeterTestSubItem
            {
                Name = "读取误差结果",
                Enabled = true,
                Protocol = "MeterControlPcbV2",
                ExecutionMode = MeterTestExecutionMode.ControlPcbStartingErrorRead.ToString(),
                ControlPcbGroup = string.Empty,
                BasicErrorPulseCount = 1,
                BasicErrorTestCount = 1,
                PacketIntervalMs = 100,
                Description = "发送0x38+AA+01+01，并解析上行小端float误差结果。",
                TimeoutMs = 5000
            },
            new MeterTestSubItem
            {
                Name = "判断误差结果",
                Enabled = true,
                Protocol = "JJG596-2026",
                ExecutionMode = MeterTestExecutionMode.StartingErrorJudge.ToString(),
                BasicErrorLimit = 1.5m,
                Description = "按误差绝对值严格小于1.5判断合格。",
                TimeoutMs = 0
            }
        };
    }

    /// <summary>
    /// 创建潜动试验流程。
    /// 第一项接入1.1倍额定电压升源；第二项启动0x35试验；第三项按XML手动时间等待。
    /// </summary>
    private static List<MeterTestSubItem> CreateCreepingTestSubItems()
    {
        return new List<MeterTestSubItem>
        {
            new MeterTestSubItem
            {
                Name = "升源（潜动电压）",
                Enabled = true,
                Protocol = "XYCtr",
                ExecutionMode = MeterTestExecutionMode.CreepingSource.ToString(),
                SourceControlConfig = "单相默认源",
                Description = "从资产信息读取额定电压，按1.1倍计算潜动电压；单相输出Ua，三相输出Ua/Ub/Uc，电流为0。",
                TimeoutMs = 20000
            },
            new MeterTestSubItem
            {
                Name = "开启潜动试验",
                Enabled = true,
                Protocol = "MeterControlPcbV2",
                ExecutionMode = MeterTestExecutionMode.ControlPcbCreepingStart.ToString(),
                ControlPcbGroup = string.Empty,
                ResponseParser = ResponseParserType.MeterControlCreepingTest.ToString(),
                CreepingPulseCount = 1,
                CreepingTimeSeconds = 1190,
                PacketIntervalMs = 100,
                Description = "按工位发送0x35+00+脉冲数+小端4字节时间，收到完整回显的工位继续后续流程。",
                TimeoutMs = 5000
            },
            new MeterTestSubItem
            {
                Name = "等待潜动时间",
                Enabled = true,
                Protocol = "ConfiguredDelay",
                ExecutionMode = MeterTestExecutionMode.CreepingWait.ToString(),
                CreepingTimeSeconds = 1190,
                Description = "按creepingTimeSeconds手动配置值等待，只记录倒计时开始和结束。",
                TimeoutMs = 0
            },
            new MeterTestSubItem
            {
                Name = "读取脉冲数量",
                Enabled = true,
                Protocol = "MeterControlPcbV2",
                ExecutionMode = MeterTestExecutionMode.ControlPcbCreepingRead.ToString(),
                ControlPcbGroup = string.Empty,
                ResponseParser = ResponseParserType.MeterControlCreepingTest.ToString(),
                PacketIntervalMs = 100,
                Description = "按工位发送0x35+AA，解析当前累计脉冲数和累计时间。",
                TimeoutMs = 5000
            },
            new MeterTestSubItem
            {
                Name = "判断脉冲结果",
                Enabled = true,
                Protocol = "JJG596-2026",
                ExecutionMode = MeterTestExecutionMode.CreepingPulseJudge.ToString(),
                Description = "累计脉冲数小于等于1判定合格，0个或1个均合格。",
                TimeoutMs = 0
            }
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
    /// 创建蓝牙接口检测方案节点。
    /// 蓝牙通道必须按工位从BluetoothTcpChannels解析专用端点并建立独立TCP会话，
    /// 不复用资产485端点、普通StationTcp或控制PCB连接。
    /// </summary>
    private static MeterTestItem CreateBluetoothInterfaceTestItem()
    {
        return new MeterTestItem
        {
            Name = "蓝牙接口检测",
            Description = "按BluetoothTcpChannels中的工位映射建立独立蓝牙TCP连接，依次执行复位、连接、检定预处理和通信地址读取。",
            TestSubItems =
            {
                CreateBluetoothSubItem("复位蓝牙", "Reset", "通过当前工位的专用蓝牙TCP连接发送0x00复位指令。"),
                CreateBluetoothSubItem("连接电表", "ConnectMeter", "使用资产信息中的电表地址发送0x01自动连接指令。"),
                CreateBluetoothSubItem("检定预处理", "Preprocess", "发送0x07后轮询0x08，最长等待40秒获取预处理结果。", 40000),
                CreateBluetoothAddressReadSubItem()
            }
        };
    }

    /// <summary>创建一个按工位独立TCP执行的蓝牙协议子项。</summary>
    private static MeterTestSubItem CreateBluetoothSubItem(
        string name,
        string step,
        string description,
        int timeoutMs = 5000)
    {
        return new MeterTestSubItem
        {
            Name = name,
            Enabled = true,
            Protocol = "SgccBluetoothConverter",
            ExecutionMode = MeterTestExecutionMode.BluetoothStationTcp.ToString(),
            BluetoothStep = step,
            Description = description,
            TimeoutMs = timeoutMs
        };
    }

    /// <summary>创建OAD=40010200通信地址读取占位节点，保留后续698解析参数。</summary>
    private static MeterTestSubItem CreateBluetoothAddressReadSubItem()
    {
        MeterTestSubItem subItem = CreateBluetoothSubItem(
            "读取通信地址40010200",
            "ReadAddress",
            "在当前工位蓝牙TCP会话中读取OAD=40010200并解析通信地址。");
        subItem.ResponseParser = ResponseParserType.Sgcc698BroadcastAddress.ToString();
        subItem.ExpectedApdu = "85 01";
        subItem.ExpectedOad = "40 01 02 00";
        subItem.ExpectedDataType = "09";
        subItem.ExpectedDataLength = 6;
        subItem.ResultField = "MeterAddress";
        return subItem;
    }

    /// <summary>将程序内置蓝牙流程参数复制到旧版Planned占位节点。</summary>
    private static void CopyBluetoothExecutionDefaults(
        MeterTestSubItem target,
        MeterTestSubItem source)
    {
        target.Enabled = source.Enabled;
        target.Protocol = source.Protocol;
        target.ExecutionMode = source.ExecutionMode;
        target.BluetoothStep = source.BluetoothStep;
        target.Description = source.Description;
        target.ResponseParser = source.ResponseParser;
        target.ExpectedApdu = source.ExpectedApdu;
        target.ExpectedOad = source.ExpectedOad;
        target.ExpectedDataType = source.ExpectedDataType;
        target.ExpectedDataLength = source.ExpectedDataLength;
        target.ResultField = source.ResultField;
        target.TimeoutMs = source.TimeoutMs;
    }

    /// <summary>创建一组18个有功基本误差测试点，每个点内部由统一服务执行完整五步流程。</summary>
    private static MeterTestItem CreateBasicErrorTestItem(string itemName, string namePrefix, string direction)
    {
        string[] powerFactors = { "1.0", "0.5L", "0.8C" };
        string[] currentPoints = { "Imin", "Itr", "10Itr", "0.5Imax", "Imax", "1.2Imax" };
        MeterTestItem item = new()
        {
            Name = itemName,
            Description = $"{(direction == "ForwardActive" ? "正向" : "反向")}有功基本误差测试；每个测试点内部统一执行升源、0x38启动、等待、读取和判定。"
        };

        foreach (string powerFactor in powerFactors)
        {
            foreach (string currentPoint in currentPoints)
            {
                item.TestSubItems.Add(new MeterTestSubItem
                {
                    Name = $"{namePrefix}-H-{powerFactor}-1U-{currentPoint}",
                    Enabled = true,
                    Protocol = "MeterControlPcbV2",
                    ExecutionMode = MeterTestExecutionMode.BasicErrorPoint.ToString(),
                    ControlPcbGroup = string.Empty,
                    SourceControlConfig = "单相默认源",
                    BasicErrorDirection = direction,
                    BasicErrorPhase = "H",
                    BasicErrorPowerFactor = powerFactor,
                    BasicErrorVoltageMultiplier = 1m,
                    BasicErrorCurrentPoint = currentPoint,
                    BasicErrorPulseCount = 0,
                    BasicErrorTestCount = 2,
                    BasicErrorPulseType = 0,
                    BasicErrorLimit = 0,
                    BasicErrorMinimumWaitSeconds = 10,
                    BasicErrorWaitPaddingSeconds = 10,
                    PacketIntervalMs = 100,
                    TimeoutMs = 5000,
                    Description = "按资产信息计算测试电流、等待时间和JJG596误差限，执行完整有功基本误差流程。"
                });
            }
        }

        return item;
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
                $"延迟等待",
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
            DailyTimingTime = 60,
            DailyTimingCount = 1,
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
            VerificationTimeoutSeconds = 20,
            VerificationIntervalSeconds = 3,
            VerificationTolerancePercent = 0.03m,
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
