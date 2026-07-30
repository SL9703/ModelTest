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
        bool configChanged = EnsureBenchTypeSwitchEndpoints(config);
        configChanged |= EnsureSourceControlProtocols(config);
        configChanged |= EnsureControlPcbGroups(config);
        configChanged |= EnsureBluetoothTcpChannels(config);
        configChanged |= EnsureCreepingSourceSubItem(config);
        configChanged |= EnsureCreepingProtocolSubItems(config);
        configChanged |= EnsureBasicErrorTestItems(config);
        configChanged |= EnsureCommunicationAddressFallbackFlow(config);
        configChanged |= EnsureDeviceSelfCheckTestItem(config);
        configChanged |= EnsureBluetoothInterfaceTestItem(config);
        configChanged |= EnsureConstantTestItem(config);
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
                TimeoutMs = 5000,
                DelayAfterSuccessMs = 1000,
                Endpoints = CreateDefaultBenchTypeSwitchEndpoints()
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
                        CreateDeviceSelfCheckTestItem(),
                        CreateCommunicationTestItem(),
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
                        CreateConstantTestItem()
                    }
                }
            }
        };
    }

    /// <summary>
    /// 把旧版 BenchTypeSwitchConfig 的单 IP/Port 迁移为端点集合。
    /// 当前标准台体的旧端点为121时，同时补入122、123；已经使用新版端点集合的现场配置保持原样。
    /// </summary>
    private static bool EnsureBenchTypeSwitchEndpoints(MeterTestPlanConfig config)
    {
        config.BenchTypeSwitchConfig ??= new MeterTestBenchTypeSwitchConfig();
        MeterTestBenchTypeSwitchConfig benchConfig = config.BenchTypeSwitchConfig;
        benchConfig.Endpoints ??= new List<MeterTestBenchTypeSwitchEndpoint>();
        if (benchConfig.Endpoints.Count > 0)
        {
            return EnsureStandardBenchTypeSwitchCapabilities(benchConfig.Endpoints);
        }

        string legacyIp = benchConfig.Ip?.Trim() ?? string.Empty;
        int legacyPort = benchConfig.Port;
        if (string.IsNullOrWhiteSpace(legacyIp) || legacyPort is < 1 or > 65535)
        {
            benchConfig.Endpoints = CreateDefaultBenchTypeSwitchEndpoints();
        }
        else
        {
            benchConfig.Endpoints.Add(CreateBenchTypeSwitchEndpoint(
                "台体切换-1",
                legacyIp,
                legacyPort,
                supportsSinglePhase: true));

            // 仅对本项目原有121:8080配置执行一次版本迁移，避免给现场自定义端点擅自追加地址。
            if (legacyIp.Equals("192.168.127.121", StringComparison.OrdinalIgnoreCase) && legacyPort == 8080)
            {
                benchConfig.Endpoints.Add(CreateBenchTypeSwitchEndpoint(
                    "台体切换-2",
                    "192.168.127.122",
                    8080,
                    supportsSinglePhase: false));
                benchConfig.Endpoints.Add(CreateBenchTypeSwitchEndpoint(
                    "台体切换-3",
                    "192.168.127.123",
                    8080,
                    supportsSinglePhase: false));
            }
        }

        benchConfig.Ip = string.Empty;
        benchConfig.Port = 0;
        return true;
    }

    /// <summary>创建默认的三个台体类型切换装置通信板端点。</summary>
    private static List<MeterTestBenchTypeSwitchEndpoint> CreateDefaultBenchTypeSwitchEndpoints() => new()
    {
        CreateBenchTypeSwitchEndpoint("台体切换-1", "192.168.127.121", 8080, supportsSinglePhase: true),
        CreateBenchTypeSwitchEndpoint("台体切换-2", "192.168.127.122", 8080, supportsSinglePhase: false),
        CreateBenchTypeSwitchEndpoint("台体切换-3", "192.168.127.123", 8080, supportsSinglePhase: false)
    };

    /// <summary>创建单个启用的台体类型切换端点。</summary>
    private static MeterTestBenchTypeSwitchEndpoint CreateBenchTypeSwitchEndpoint(
        string name,
        string ip,
        int port,
        bool supportsSinglePhase) => new()
    {
        Name = name,
        Enabled = true,
        Ip = ip,
        Port = port,
        SupportsSinglePhase = supportsSinglePhase
    };

    /// <summary>
    /// 为旧版三端点配置补齐单相能力标记。
    /// 标准硬件中只有台体切换-1支持单相，切换-2和切换-3只参与三相直接式/互感式。
    /// </summary>
    private static bool EnsureStandardBenchTypeSwitchCapabilities(
        IEnumerable<MeterTestBenchTypeSwitchEndpoint> endpoints)
    {
        bool changed = false;
        foreach (MeterTestBenchTypeSwitchEndpoint endpoint in endpoints)
        {
            bool? expected = endpoint.Name.Trim() switch
            {
                "台体切换-1" => true,
                "台体切换-2" => false,
                "台体切换-3" => false,
                _ => null
            };
            if (expected.HasValue && endpoint.SupportsSinglePhase != expected.Value)
            {
                endpoint.SupportsSinglePhase = expected.Value;
                changed = true;
            }
        }

        return changed;
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
    /// 兼容旧版源配置：缺少 protocol 时按当前已接入的 XYCtr 驱动补齐并回写XML。
    /// </summary>
    private static bool EnsureSourceControlProtocols(MeterTestPlanConfig config)
    {
        bool changed = false;
        foreach (MeterTestSourceControlConfig sourceConfig in config.SourceControlConfigs)
        {
            if (!string.IsNullOrWhiteSpace(sourceConfig.Protocol))
                continue;

            sourceConfig.Protocol = MeterTestSourceProtocol.XYCtr.ToString();
            changed = true;
        }

        return changed;
    }

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
    /// 启动/读取统一使用0x25协议；等待时间按资产信息和JJG596公式自动计算。
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
                !startItem.ResponseParser.Equals(responseParser, StringComparison.OrdinalIgnoreCase) ||
                !startItem.Description.Contains("0x25+01", StringComparison.OrdinalIgnoreCase))
            {
                startItem.Enabled = true;
                startItem.Protocol = "MeterControlPcbV2";
                startItem.ExecutionMode = executionMode;
                startItem.ResponseParser = responseParser;
                startItem.Description = "按工位发送0x25+01启动潜动试验，收到数据项01应答的工位继续后续流程。";
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
            const string protocol = "JJG596-2026";
            const string description = "根据资产信息中的有功等级、常数、电表类型、额定电压和Imin自动计算潜动等待时间。";
            if (!waitItem.Enabled ||
                !waitItem.Protocol.Equals(protocol, StringComparison.OrdinalIgnoreCase) ||
                !waitItem.ExecutionMode.Equals(executionMode, StringComparison.OrdinalIgnoreCase) ||
                !waitItem.Description.Equals(description, StringComparison.Ordinal))
            {
                waitItem.Enabled = true;
                waitItem.Protocol = protocol;
                waitItem.ExecutionMode = executionMode;
                waitItem.Description = description;
                waitItem.TimeoutMs = 0;
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
                !readItem.ResponseParser.Equals(responseParser, StringComparison.OrdinalIgnoreCase) ||
                !readItem.Description.Contains("0x25+AA", StringComparison.OrdinalIgnoreCase))
            {
                readItem.Enabled = true;
                readItem.Protocol = "MeterControlPcbV2";
                readItem.ExecutionMode = executionMode;
                readItem.ResponseParser = responseParser;
                readItem.Description = "按工位发送0x25+AA，解析AA后的4字节小端uint实际脉冲数。";
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

    /// <summary>确保方案包含指定基本误差测试项，并补齐缺失的小项而不覆盖用户已有配置。</summary>
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
            MeterTestSubItem? existingSubItem = existingItem.TestSubItems.FirstOrDefault(item =>
                item.Name.Equals(expectedSubItem.Name, StringComparison.OrdinalIgnoreCase));
            if (existingSubItem is not null)
            {
                // 现场曾手工补过“正有-A/B/C...”节点时，节点名称可能已经是分相，
                // 但 basicErrorPhase 仍保留旧的 H。加载配置时按名称自动修正，避免界面与实际升源相别不一致。
                string? expectedPhase = ExtractBasicErrorPhaseFromName(existingSubItem.Name);
                if (!string.IsNullOrWhiteSpace(expectedPhase) &&
                    !existingSubItem.BasicErrorPhase.Equals(expectedPhase, StringComparison.OrdinalIgnoreCase))
                {
                    existingSubItem.BasicErrorPhase = expectedPhase;
                    changed = true;
                }

                // 同样兼容手工粘贴“反有-A/B/C...”模板时 direction 仍写成 ForwardActive 的情况。
                string? expectedDirection = ExtractBasicErrorDirectionFromName(existingSubItem.Name);
                if (!string.IsNullOrWhiteSpace(expectedDirection) &&
                    !existingSubItem.BasicErrorDirection.Equals(expectedDirection, StringComparison.OrdinalIgnoreCase))
                {
                    existingSubItem.BasicErrorDirection = expectedDirection;
                    changed = true;
                }

                // 保留旧版等级误差限用于兼容既有XML；当前执行统一调用JJG596误差比较算法。
                if (existingSubItem.BasicErrorLimit <= 0 &&
                    string.IsNullOrWhiteSpace(existingSubItem.BasicErrorLimits))
                {
                    existingSubItem.BasicErrorLimits = expectedSubItem.BasicErrorLimits;
                    changed = true;
                }

                // 基本误差按“单次理论时间×次数+20秒”等待，旧现场配置加载时自动校正。
                if (existingSubItem.BasicErrorWaitPaddingSeconds != MeterTestBasicErrorDefaults.WaitPaddingSeconds)
                {
                    existingSubItem.BasicErrorWaitPaddingSeconds = MeterTestBasicErrorDefaults.WaitPaddingSeconds;
                    changed = true;
                }

                continue;
            }

            existingItem.TestSubItems.Add(expectedSubItem);
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// 为已有现场配置补齐蓝牙接口检测及五个子项。
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

            if (!existingItem.Description.Equals(expectedItem.Description, StringComparison.Ordinal))
            {
                existingItem.Description = expectedItem.Description;
                changed = true;
            }

            for (int expectedIndex = 0; expectedIndex < expectedItem.TestSubItems.Count; expectedIndex++)
            {
                MeterTestSubItem expectedSubItem = expectedItem.TestSubItems[expectedIndex];
                MeterTestSubItem? existingSubItem = existingItem.TestSubItems.FirstOrDefault(item =>
                    item.Name.Equals(expectedSubItem.Name, StringComparison.OrdinalIgnoreCase));
                if (existingSubItem is null)
                {
                    existingItem.TestSubItems.Insert(
                        Math.Min(expectedIndex, existingItem.TestSubItems.Count),
                        expectedSubItem);
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
    /// 为已有现场方案补齐常数试验九步流程。
    /// 旧配置中的空“常数试验”节点会被升级为可执行流程，现场已有同名步骤只补齐执行入口。
    /// </summary>
    private static bool EnsureConstantTestItem(MeterTestPlanConfig config)
    {
        bool changed = false;
        MeterTestItem expectedItem = CreateConstantTestItem();
        foreach (MeterTestScheme scheme in config.Schemes)
        {
            MeterTestItem? existingItem = scheme.TestItems.FirstOrDefault(item =>
                item.Name.Equals(expectedItem.Name, StringComparison.OrdinalIgnoreCase));
            if (existingItem is null)
            {
                scheme.TestItems.Add(CreateConstantTestItem());
                changed = true;
                continue;
            }

            if (!existingItem.Description.Equals(expectedItem.Description, StringComparison.Ordinal))
            {
                existingItem.Description = expectedItem.Description;
                changed = true;
            }

            for (int index = 0; index < expectedItem.TestSubItems.Count; index++)
            {
                MeterTestSubItem expectedSubItem = expectedItem.TestSubItems[index];
                MeterTestSubItem? existingSubItem = existingItem.TestSubItems.FirstOrDefault(item =>
                    item.Name.Equals(expectedSubItem.Name, StringComparison.OrdinalIgnoreCase));
                if (existingSubItem is null)
                {
                    existingItem.TestSubItems.Insert(Math.Min(index, existingItem.TestSubItems.Count), expectedSubItem);
                    changed = true;
                    continue;
                }

                changed |= CopyConstantExecutionDefaults(existingSubItem, expectedSubItem);
            }
        }

        return changed;
    }

    /// <summary>
    /// 保留并补齐原有“连接/读取/校验/修改/地址读取”通信流程。
    /// 备用波特率循环只作为最后地址读取失败后的追加动作，不改变前四个V2串口服务器步骤。
    /// </summary>
    private static bool EnsureCommunicationAddressFallbackFlow(MeterTestPlanConfig config)
    {
        bool changed = false;
        MeterTestItem expectedItem = CreateCommunicationTestItem();

        foreach (MeterTestScheme scheme in config.Schemes)
        {
            MeterTestItem? communicationItem = scheme.TestItems.FirstOrDefault(item =>
                item.Name.Equals("通信测试", StringComparison.OrdinalIgnoreCase));
            if (communicationItem is null)
            {
                communicationItem = CreateCommunicationTestItem();
                int selfCheckIndex = scheme.TestItems.FindIndex(item =>
                    item.Name.Equals("设备自检测试", StringComparison.OrdinalIgnoreCase));
                scheme.TestItems.Insert(selfCheckIndex >= 0 ? selfCheckIndex + 1 : 0, communicationItem);
                changed = true;
                continue;
            }

            for (int expectedIndex = 0; expectedIndex < expectedItem.TestSubItems.Count; expectedIndex++)
            {
                MeterTestSubItem expectedSubItem = expectedItem.TestSubItems[expectedIndex];
                MeterTestSubItem? existingSubItem = communicationItem.TestSubItems.FirstOrDefault(subItem =>
                    subItem.Name.Equals(expectedSubItem.Name, StringComparison.OrdinalIgnoreCase));
                if (existingSubItem is null)
                {
                    communicationItem.TestSubItems.Insert(
                        Math.Min(expectedIndex, communicationItem.TestSubItems.Count),
                        expectedSubItem);
                    changed = true;
                    continue;
                }

                if (expectedSubItem.Name.Equals("地址读取", StringComparison.OrdinalIgnoreCase))
                {
                    changed |= ApplyCommunicationAddressDefaults(existingSubItem);
                }
            }

            string expectedDescription = expectedItem.Description;
            if (!communicationItem.Description.Equals(expectedDescription, StringComparison.Ordinal))
            {
                communicationItem.Description = expectedDescription;
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>创建原串口服务器四步同步流程，并在地址读取失败后追加备用波特率尝试。</summary>
    private static MeterTestItem CreateCommunicationTestItem()
    {
        return new MeterTestItem
        {
            Name = "通信测试",
            Description = "先执行原串口服务器波特率同步流程；地址读取失败后追加数据库候选波特率循环。",
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
                CreateCommunicationAddressSubItem()
            }
        };
    }

    /// <summary>创建698定址读取节点；串口服务器的波特率回退由执行服务在节点内部完成。</summary>
    private static MeterTestSubItem CreateCommunicationAddressSubItem()
    {
        return new MeterTestSubItem
        {
            Name = "地址读取",
            Enabled = true,
            Description = "先按原流程同步后的资产波特率读取；无地址响应时追加切换数据库候选波特率并重试。",
            Protocol = "DLT698.45",
            ExecutionMode = MeterTestExecutionMode.StationTcp.ToString(),
            SourceControlConfig = "单相默认源",
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
        };
    }

    /// <summary>补齐旧地址读取节点的协议字段，同时保留现场已配置的超时、升源名称和模拟响应。</summary>
    private static bool ApplyCommunicationAddressDefaults(MeterTestSubItem target)
    {
        MeterTestSubItem expected = CreateCommunicationAddressSubItem();
        bool changed = false;

        changed |= SetIfDifferent(target.Enabled, expected.Enabled, value => target.Enabled = value);
        changed |= SetIfDifferent(target.Protocol, expected.Protocol, value => target.Protocol = value);
        changed |= SetIfDifferent(target.ExecutionMode, expected.ExecutionMode, value => target.ExecutionMode = value);
        changed |= SetIfDifferent(target.Description, expected.Description, value => target.Description = value);
        changed |= SetIfDifferent(target.RequestHex, expected.RequestHex, value => target.RequestHex = value);
        changed |= SetIfDifferent(target.ResponseParser, expected.ResponseParser, value => target.ResponseParser = value);
        changed |= SetIfDifferent(target.ExpectedApdu, expected.ExpectedApdu, value => target.ExpectedApdu = value);
        changed |= SetIfDifferent(target.ExpectedOad, expected.ExpectedOad, value => target.ExpectedOad = value);
        changed |= SetIfDifferent(target.ExpectedDataType, expected.ExpectedDataType, value => target.ExpectedDataType = value);
        changed |= SetIfDifferent(target.ExpectedDataLength, expected.ExpectedDataLength, value => target.ExpectedDataLength = value);
        changed |= SetIfDifferent(target.ResultField, expected.ResultField, value => target.ResultField = value);
        changed |= SetIfDifferent(target.MatchMode, expected.MatchMode, value => target.MatchMode = value);

        if (string.IsNullOrWhiteSpace(target.SourceControlConfig))
        {
            target.SourceControlConfig = expected.SourceControlConfig;
            changed = true;
        }
        if (target.TimeoutMs <= 0)
        {
            target.TimeoutMs = expected.TimeoutMs;
            changed = true;
        }
        if (!string.IsNullOrWhiteSpace(target.SerialPortServerStep))
        {
            target.SerialPortServerStep = string.Empty;
            changed = true;
        }

        return changed;
    }

    /// <summary>仅在值确实变化时写入，避免每次启动都重写XML。</summary>
    private static bool SetIfDifferent<T>(T current, T expected, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, expected))
        {
            return false;
        }

        setter(expected);
        return true;
    }

    /// <summary>
    /// 为已有现场方案补齐设备自检测试，并确保它排列在通信测试之前。
    /// 已有同名子项会保留现场超时和延时配置，只补齐协议执行入口及步骤标识。
    /// </summary>
    private static bool EnsureDeviceSelfCheckTestItem(MeterTestPlanConfig config)
    {
        bool changed = false;
        MeterTestItem expectedItem = CreateDeviceSelfCheckTestItem();
        foreach (MeterTestScheme scheme in config.Schemes)
        {
            MeterTestItem? existingItem = scheme.TestItems.FirstOrDefault(item =>
                item.Name.Equals(expectedItem.Name, StringComparison.OrdinalIgnoreCase));
            if (existingItem is null)
            {
                int communicationIndex = scheme.TestItems.FindIndex(item =>
                    item.Name.Equals("通信测试", StringComparison.OrdinalIgnoreCase));
                scheme.TestItems.Insert(
                    communicationIndex >= 0 ? communicationIndex : 0,
                    CreateDeviceSelfCheckTestItem());
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

                if (!existingSubItem.ExecutionMode.Equals(
                        MeterTestExecutionMode.ControlPcbDeviceSelfCheck.ToString(),
                        StringComparison.OrdinalIgnoreCase) ||
                    string.IsNullOrWhiteSpace(existingSubItem.DeviceSelfCheckStep))
                {
                    existingSubItem.Enabled = true;
                    existingSubItem.Protocol = expectedSubItem.Protocol;
                    existingSubItem.ExecutionMode = expectedSubItem.ExecutionMode;
                    existingSubItem.DeviceSelfCheckStep = expectedSubItem.DeviceSelfCheckStep;
                    existingSubItem.ControlPcbGroup = expectedSubItem.ControlPcbGroup;
                    existingSubItem.PacketIntervalMs = expectedSubItem.PacketIntervalMs;
                    existingSubItem.Description = expectedSubItem.Description;
                    if (existingSubItem.TimeoutMs <= 0)
                    {
                        existingSubItem.TimeoutMs = expectedSubItem.TimeoutMs;
                    }
                    changed = true;
                }
            }

            int currentIndex = scheme.TestItems.IndexOf(existingItem);
            int targetIndex = scheme.TestItems.FindIndex(item =>
                item.Name.Equals("通信测试", StringComparison.OrdinalIgnoreCase));
            if (targetIndex >= 0 && currentIndex > targetIndex)
            {
                scheme.TestItems.RemoveAt(currentIndex);
                targetIndex = scheme.TestItems.FindIndex(item =>
                    item.Name.Equals("通信测试", StringComparison.OrdinalIgnoreCase));
                scheme.TestItems.Insert(targetIndex, existingItem);
                changed = true;
            }
        }

        return changed;
    }

    /// <summary>创建升源前执行的检测单元设备自检方案。</summary>
    private static MeterTestItem CreateDeviceSelfCheckTestItem()
    {
        return new MeterTestItem
        {
            Name = "设备自检测试",
            Description = "在通信测试和升源前，通过V2控制PCB执行短路、断路和温度传感器检查。",
            TestSubItems =
            {
                new MeterTestSubItem
                {
                    Name = "检测单元短路检测",
                    Enabled = true,
                    Protocol = "MeterControlPcbV2",
                    ExecutionMode = MeterTestExecutionMode.ControlPcbDeviceSelfCheck.ToString(),
                    DeviceSelfCheckStep = "ShortCircuit",
                    SelfCheckDelayMs = 1000,
                    SelfCheckMaximumSafeVoltage = 5m,
                    PacketIntervalMs = 100,
                    Description = "确认无压后按工位发送0x86启动和结果读取；检测到电压时先ShutPowerSource(0)降源并复核。",
                    TimeoutMs = 5000
                },
                new MeterTestSubItem
                {
                    Name = "检测单元断路检测",
                    Enabled = true,
                    Protocol = "MeterControlPcbV2",
                    ExecutionMode = MeterTestExecutionMode.ControlPcbDeviceSelfCheck.ToString(),
                    DeviceSelfCheckStep = "OpenCircuit",
                    SelfCheckDelayMs = 1000,
                    PacketIntervalMs = 100,
                    Description = "按工位发送0x84启动和结果读取，结果码01表示电流线路正常。",
                    TimeoutMs = 5000
                },
                new MeterTestSubItem
                {
                    Name = "检测单元温湿度检测",
                    Enabled = true,
                    Protocol = "MeterControlPcbV2",
                    ExecutionMode = MeterTestExecutionMode.ControlPcbDeviceSelfCheck.ToString(),
                    DeviceSelfCheckStep = "TemperatureHumidity",
                    TemperatureSensorIndex = 1,
                    PacketIntervalMs = 100,
                    Description = "按工位发送0xCA+传感器序号+AA并解析4字节有符号小端温度原始值。",
                    TimeoutMs = 5000
                }
            }
        };
    }

    /// <summary>
    /// 创建起动试验测试点。
    /// 方案树只展示正向/反向两个点，内部仍复用原升源、启动、等待、读取和判定五步流程。
    /// </summary>
    private static List<MeterTestSubItem> CreateStartingTestSubItems()
    {
        return new List<MeterTestSubItem>
        {
            CreateStartingErrorPoint("正有-H-1.0-1U-Ist", "ForwardActive", "正向有功起动误差点。"),
            CreateStartingErrorPoint("反有-H-1.0-1U-Ist", "ReverseActive", "反向有功起动误差点。")
        };
    }

    /// <summary>创建一个起动误差测试点配置，执行器会在内部展开为原五步流程。</summary>
    private static MeterTestSubItem CreateStartingErrorPoint(string name, string direction, string description)
    {
        return new MeterTestSubItem
        {
            Name = name,
            Enabled = true,
            Protocol = "JJG596-2026+MeterControlPcbV2+XYCtr",
            ExecutionMode = MeterTestExecutionMode.StartingErrorPoint.ToString(),
            SourceControlConfig = "单相默认源",
            ControlPcbGroup = string.Empty,
            BasicErrorDirection = direction,
            BasicErrorPhase = "H",
            BasicErrorPowerFactor = "1.0",
            BasicErrorVoltageMultiplier = 1m,
            BasicErrorCurrentPoint = "Ist",
            BasicErrorPulseCount = 1,
            BasicErrorTestCount = 1,
            BasicErrorPulseType = 0,
            StartingTimeMultiplier = 2,
            PacketIntervalMs = 100,
            Description = description + "内部执行升源、启动、等待、读取和判定完整流程。",
            TimeoutMs = 5000
        };
    }

    /// <summary>
    /// 创建潜动试验流程。
    /// 第一项接入1.1倍额定电压升源；第二项启动0x25试验；第三项按资产自动计算时间等待。
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
                PacketIntervalMs = 100,
                Description = "按工位发送0x25+01启动潜动试验，收到数据项01应答的工位继续后续流程。",
                TimeoutMs = 5000
            },
            new MeterTestSubItem
            {
                Name = "等待潜动时间",
                Enabled = true,
                Protocol = "JJG596-2026",
                ExecutionMode = MeterTestExecutionMode.CreepingWait.ToString(),
                Description = "根据资产信息中的有功等级、常数、电表类型、额定电压和Imin自动计算潜动等待时间。",
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
                Description = "按工位发送0x25+AA，解析AA后的4字节小端uint实际脉冲数。",
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
    /// 首先通过同IP的64444管理端设置BluetoothTcpChannel为9600-8-E-1，再建立蓝牙工位端口会话，
    /// 不复用资产485端点、普通StationTcp或控制PCB连接。
    /// </summary>
    private static MeterTestItem CreateBluetoothInterfaceTestItem()
    {
        return new MeterTestItem
        {
            Name = "蓝牙接口检测",
            Description = "先通过同IP的64444管理端设置蓝牙通道9600-8-E-1，再复用蓝牙TCP连接执行复位、连接、检定预处理和通信地址读取。",
            TestSubItems =
            {
                CreateBluetoothBaudRateSubItem(),
                CreateBluetoothSubItem("复位蓝牙", "Reset", "通过当前工位的专用蓝牙TCP连接发送0x00复位指令。"),
                CreateBluetoothSubItem("连接电表", "ConnectMeter", "使用资产信息中的电表地址发送0x01自动连接指令。"),
                CreateBluetoothSubItem("检定预处理", "Preprocess", "发送0x07后轮询0x08，最长等待40秒获取预处理结果。", 40000),
                CreateBluetoothAddressReadSubItem()
            }
        };
    }

    /// <summary>创建蓝牙接口检测的首个波特率设置节点，使用同IP的64444管理端。</summary>
    private static MeterTestSubItem CreateBluetoothBaudRateSubItem()
    {
        MeterTestSubItem subItem = CreateBluetoothSubItem(
            "修改串口波特率 9600-8-E-1",
            "SetBaudRate",
            "连接当前蓝牙通道所属IP的64444管理端，通过通用串口服务器协议将当前BluetoothTcpChannel端口设置为9600-8-E-1。");
        subItem.Protocol = "GenericSerialPortServer";
        return subItem;
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

    /// <summary>创建常数试验九步流程。</summary>
    private static MeterTestItem CreateConstantTestItem()
    {
        return new MeterTestItem
        {
            Name = "常数试验",
            Description = "读取电表起止电量，执行0x37走字试验，并按电量差换算理论脉冲后比对待测表脉冲数。",
            TestSubItems =
            {
                new MeterTestSubItem
                {
                    Name = "读取电表开始电量",
                    Enabled = true,
                    Protocol = "DLT698.45",
                    ExecutionMode = MeterTestExecutionMode.ConstantEnergyReadStart.ToString(),
                    ExpectedApdu = "85 01",
                    ExpectedOad = "00 10 02 00",
                    Description = "通过MeterTestStationConfig配置的工位TCP通道读取正向有功总电能作为起始电量。",
                    TimeoutMs = 5000
                },
                new MeterTestSubItem
                {
                    Name = "开始走字试验",
                    Enabled = true,
                    Protocol = "MeterControlPcbV2",
                    ExecutionMode = MeterTestExecutionMode.ControlPcbWalkingStart.ToString(),
                    ControlPcbGroup = string.Empty,
                    PacketIntervalMs = 100,
                    Description = "按工位发送0x37+00开始走字试验。",
                    TimeoutMs = 5000
                },
                new MeterTestSubItem
                {
                    Name = "升源（基础电压、Imax电流）",
                    Enabled = true,
                    Protocol = "XYCtr",
                    ExecutionMode = MeterTestExecutionMode.ConstantImaxSource.ToString(),
                    SourceControlConfig = "单相默认源",
                    Description = "按资产信息额定电压和电流规格Imax升源。",
                    TimeoutMs = 20000
                },
                new MeterTestSubItem
                {
                    Name = "等待60秒",
                    Enabled = true,
                    Protocol = "System",
                    ExecutionMode = MeterTestExecutionMode.ConstantWait.ToString(),
                    Description = "走字试验固定等待60秒。",
                    TimeoutMs = 60000
                },
                new MeterTestSubItem
                {
                    Name = "升源（基础电压）",
                    Enabled = true,
                    Protocol = "XYCtr",
                    ExecutionMode = MeterTestExecutionMode.ConstantVoltageSource.ToString(),
                    SourceControlConfig = "单相默认源",
                    Description = "保持资产额定电压输出，电流降为0A。",
                    TimeoutMs = 20000
                },
                new MeterTestSubItem
                {
                    Name = "读取电表结束电量",
                    Enabled = true,
                    Protocol = "DLT698.45",
                    ExecutionMode = MeterTestExecutionMode.ConstantEnergyReadEnd.ToString(),
                    ExpectedApdu = "85 01",
                    ExpectedOad = "00 10 02 00",
                    Description = "通过MeterTestStationConfig配置的工位TCP通道读取正向有功总电能作为结束电量。",
                    TimeoutMs = 5000
                },
                new MeterTestSubItem
                {
                    Name = "停止走字试验",
                    Enabled = true,
                    Protocol = "MeterControlPcbV2",
                    ExecutionMode = MeterTestExecutionMode.ControlPcbWalkingStop.ToString(),
                    ControlPcbGroup = string.Empty,
                    PacketIntervalMs = 100,
                    Description = "按工位发送0x37+FF停止走字试验。",
                    TimeoutMs = 5000
                },
                new MeterTestSubItem
                {
                    Name = "读取走字试验结果",
                    Enabled = true,
                    Protocol = "MeterControlPcbV2",
                    ExecutionMode = MeterTestExecutionMode.ControlPcbWalkingRead.ToString(),
                    ControlPcbGroup = string.Empty,
                    PacketIntervalMs = 100,
                    Description = "按工位发送0x37+AA，解析被测表脉冲数和标准表电能量。",
                    TimeoutMs = 5000
                },
                new MeterTestSubItem
                {
                    Name = "对比试验结果",
                    Enabled = true,
                    Protocol = "JJG596-2026",
                    ExecutionMode = MeterTestExecutionMode.ConstantResultJudge.ToString(),
                    ConstantEnergyToleranceKwh = 0.01m,
                    Description = "用(结束电量-开始电量)×资产有功常数得到理论脉冲，和0x37返回待测表脉冲数比较，差值≤1判定合格。",
                    TimeoutMs = 0
                }
            }
        };
    }

    /// <summary>把常数试验内置执行参数写入旧节点，返回是否实际发生变化。</summary>
    private static bool CopyConstantExecutionDefaults(MeterTestSubItem target, MeterTestSubItem source)
    {
        bool changed = false;
        changed |= SetIfDifferent(target.Enabled, source.Enabled, value => target.Enabled = value);
        changed |= SetIfDifferent(target.Protocol, source.Protocol, value => target.Protocol = value);
        changed |= SetIfDifferent(target.ExecutionMode, source.ExecutionMode, value => target.ExecutionMode = value);
        changed |= SetIfDifferent(target.ControlPcbGroup, source.ControlPcbGroup, value => target.ControlPcbGroup = value);
        changed |= SetIfDifferent(target.SourceControlConfig, source.SourceControlConfig, value => target.SourceControlConfig = value);
        changed |= SetIfDifferent(target.ExpectedApdu, source.ExpectedApdu, value => target.ExpectedApdu = value);
        changed |= SetIfDifferent(target.ExpectedOad, source.ExpectedOad, value => target.ExpectedOad = value);
        changed |= SetIfDifferent(target.PacketIntervalMs, source.PacketIntervalMs, value => target.PacketIntervalMs = value);
        changed |= SetIfDifferent(target.ConstantEnergyToleranceKwh, source.ConstantEnergyToleranceKwh, value => target.ConstantEnergyToleranceKwh = value);
        changed |= SetIfDifferent(target.Description, source.Description, value => target.Description = value);
        if (target.TimeoutMs <= 0 && source.TimeoutMs > 0)
        {
            target.TimeoutMs = source.TimeoutMs;
            changed = true;
        }

        return changed;
    }

    /// <summary>创建有功基本误差测试点，每个点内部由统一服务执行完整五步流程。</summary>
    private static MeterTestItem CreateBasicErrorTestItem(string itemName, string namePrefix, string direction)
    {
        string[] powerFactors = { "1.0", "0.5L", "0.8C" };
        string[] currentPoints = { "Imin", "Itr", "10Itr", "0.5Imax", "Imax", "1.2Imax" };
        string[] phases = { "H", "A", "B", "C" };
        MeterTestItem item = new()
        {
            Name = itemName,
            Description = $"{(direction == "ForwardActive" ? "正向" : "反向")}有功基本误差测试；每个测试点内部统一执行升源、0x38启动、等待、读取和判定。"
        };

        foreach (string phase in phases)
        {
            foreach (string powerFactor in powerFactors)
            {
                foreach (string currentPoint in currentPoints)
                {
                    item.TestSubItems.Add(new MeterTestSubItem
                    {
                        Name = $"{namePrefix}-{phase}-{powerFactor}-1U-{currentPoint}",
                        Enabled = true,
                        Protocol = "MeterControlPcbV2",
                        ExecutionMode = MeterTestExecutionMode.BasicErrorPoint.ToString(),
                        ControlPcbGroup = string.Empty,
                        SourceControlConfig = "单相默认源",
                        BasicErrorDirection = direction,
                        BasicErrorPhase = phase,
                        BasicErrorPowerFactor = powerFactor,
                        BasicErrorVoltageMultiplier = 1m,
                        BasicErrorCurrentPoint = currentPoint,
                        BasicErrorPulseCount = 0,
                        BasicErrorTestCount = 2,
                        BasicErrorPulseType = 0,
                        BasicErrorLimit = 0,
                        BasicErrorLimits = CreateDefaultBasicErrorLimits(currentPoint, powerFactor),
                        BasicErrorMinimumWaitSeconds = 10,
                        BasicErrorWaitPaddingSeconds = MeterTestBasicErrorDefaults.WaitPaddingSeconds,
                        PacketIntervalMs = 100,
                        TimeoutMs = 5000,
                        Description = "按资产信息计算测试电流、等待时间和JJG596误差限，执行完整有功基本误差流程。"
                    });
                }
            }
        }

        return item;
    }

    /// <summary>从“正有-A-1.0-1U-Imin”这类名称中提取分相标识。</summary>
    private static string? ExtractBasicErrorPhaseFromName(string name)
    {
        string[] parts = (name ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 && parts[1] is "H" or "A" or "B" or "C"
            ? parts[1]
            : null;
    }

    /// <summary>从“正有-A...”或“反有-A...”这类名称中提取有功方向。</summary>
    private static string? ExtractBasicErrorDirectionFromName(string name)
    {
        string[] parts = (name ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        return parts[0] switch
        {
            "正有" => "ForwardActive",
            "反有" => "ReverseActive",
            _ => null
        };
    }

    /// <summary>
    /// 生成旧版XML兼容用的默认等级误差限。
    /// 当前测试执行期不读取这些固定值，统一调用 MeterTestErrorResultComparer。
    /// </summary>
    private static string CreateDefaultBasicErrorLimits(string currentPoint, string powerFactor)
    {
        bool isMinimumCurrent = currentPoint.Equals("Imin", StringComparison.OrdinalIgnoreCase);
        bool isUnityPowerFactor = powerFactor.Equals("1.0", StringComparison.OrdinalIgnoreCase);
        if (isMinimumCurrent)
        {
            return isUnityPowerFactor
                ? "A=2.5;B=1.5;C=1.0;D=0.4"
                : "A=2.5;B=1.5;C=1.0;D=0.5";
        }

        return isUnityPowerFactor
            ? "A=2.0;B=1.0;C=0.5;D=0.2"
            : "A=2.0;B=1.0;C=0.6;D=0.3";
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
            Protocol = MeterTestSourceProtocol.XYCtr.ToString(),
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
