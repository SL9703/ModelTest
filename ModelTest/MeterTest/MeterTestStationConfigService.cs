using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

/// <summary>
/// 工位通信配置 XML 读写服务。
/// 用于维护 StationTcp 测试模式下 48 个工位的 IP/Port 映射。
/// </summary>
public sealed class MeterTestStationConfigService
{
    private const int MaximumStationCount = 48;
    private const int StationsPerControlPcb = 3;

    /// <summary>
    /// 工位配置序列化器。
    /// </summary>
    private readonly XmlSerializer serializer = new(typeof(MeterTestStationConfig));

    /// <summary>
    /// 加载工位通信配置；如果不存在或工位不完整，会用默认 IP/端口补齐并保存。
    /// </summary>
    public MeterTestStationConfig LoadOrCreate(
        string configPath,
        int stationCount,
        string defaultIp,
        int defaultStartPort,
        MeterTestPlanConfig? fallbackPlanConfig = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory);

        MeterTestStationConfig config;
        if (File.Exists(configPath))
        {
            using FileStream stream = File.OpenRead(configPath);
            config = serializer.Deserialize(stream) as MeterTestStationConfig ?? new MeterTestStationConfig();
        }
        else
        {
            config = new MeterTestStationConfig();
        }

        EnsureStations(config, stationCount, defaultIp, defaultStartPort);
        EnsureRuntimeDeviceConfigs(config, fallbackPlanConfig);
        Save(configPath, config);
        return config;
    }

    /// <summary>
    /// 使用工位配置中的现场设备端点覆盖测试方案运行时配置。
    /// 这样测试流程仍从Plan读取，但实际连接参数统一由MeterTestStationConfig维护。
    /// </summary>
    public void ApplyRuntimeDeviceConfigs(MeterTestPlanConfig planConfig, MeterTestStationConfig stationConfig)
    {
        EnsureRuntimeDeviceConfigs(stationConfig, planConfig);
        planConfig.BenchTypeSwitchConfig = stationConfig.BenchTypeSwitchConfig;
        planConfig.SourceControlConfigs = stationConfig.SourceControlConfigs;
        planConfig.ControlPcbGroups = stationConfig.ControlPcbGroups;
        planConfig.BluetoothTcpChannels = stationConfig.BluetoothTcpChannels;
    }

    /// <summary>
    /// 保存工位通信配置。
    /// </summary>
    public void Save(string configPath, MeterTestStationConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(configPath) ?? AppContext.BaseDirectory);

        using FileStream stream = File.Create(configPath);
        serializer.Serialize(stream, config);
    }

    /// <summary>
    /// 确保配置中包含指定数量的工位。
    /// 缺失工位按 defaultStartPort + stationNo - 1 自动分配端口。
    /// </summary>
    private static void EnsureStations(MeterTestStationConfig config, int stationCount, string defaultIp, int defaultStartPort)
    {
        for (int stationNo = 1; stationNo <= stationCount; stationNo++)
        {
            if (config.Stations.Any(station => station.StationNo == stationNo))
                continue;

            config.Stations.Add(new MeterTestStationCommunication
            {
                StationNo = stationNo,
                Ip = defaultIp,
                Port = defaultStartPort + stationNo - 1
            });
        }

        config.Stations = config.Stations
            .Where(station => station.StationNo >= 1 && station.StationNo <= stationCount)
            .OrderBy(station => station.StationNo)
            .ToList();
    }

    /// <summary>
    /// 补齐台体切换、源、控制PCB和蓝牙通道配置。
    /// 如果旧版PlanConfig中已有现场配置，会优先迁移旧值；否则写入项目默认值。
    /// </summary>
    private static void EnsureRuntimeDeviceConfigs(MeterTestStationConfig config, MeterTestPlanConfig? fallbackPlanConfig)
    {
        config.BenchTypeSwitchConfig ??= new MeterTestBenchTypeSwitchConfig();
        if (!HasBenchTypeSwitchEndpoints(config.BenchTypeSwitchConfig) && fallbackPlanConfig is not null)
        {
            config.BenchTypeSwitchConfig = CloneBenchTypeSwitchConfig(fallbackPlanConfig.BenchTypeSwitchConfig);
        }

        EnsureBenchTypeSwitchEndpoints(config.BenchTypeSwitchConfig);

        config.SourceControlConfigs ??= new List<MeterTestSourceControlConfig>();
        if (config.SourceControlConfigs.Count == 0 && fallbackPlanConfig?.SourceControlConfigs?.Count > 0)
        {
            config.SourceControlConfigs = fallbackPlanConfig.SourceControlConfigs
                .Select(CloneSourceControlConfig)
                .ToList();
        }

        if (config.SourceControlConfigs.Count == 0)
        {
            config.SourceControlConfigs.Add(CreateSourceControlConfig("单相默认源", MeterTestSourcePhaseMode.SinglePhase, "220", "5"));
            config.SourceControlConfigs.Add(CreateSourceControlConfig("三相默认源", MeterTestSourcePhaseMode.ThreePhase, "220", "5"));
        }

        EnsureSourceControlProtocols(config.SourceControlConfigs);

        config.ControlPcbGroups ??= new List<MeterTestControlPcbGroup>();
        if (config.ControlPcbGroups.Count == 0 && fallbackPlanConfig?.ControlPcbGroups?.Count > 0)
        {
            config.ControlPcbGroups = fallbackPlanConfig.ControlPcbGroups
                .Select(CloneControlPcbGroup)
                .ToList();
        }

        EnsureControlPcbGroups(config.ControlPcbGroups);

        config.BluetoothTcpChannels ??= new List<MeterTestBluetoothTcpChannel>();
        if (config.BluetoothTcpChannels.Count == 0 && fallbackPlanConfig?.BluetoothTcpChannels?.Count > 0)
        {
            config.BluetoothTcpChannels = fallbackPlanConfig.BluetoothTcpChannels
                .Select(CloneBluetoothTcpChannel)
                .ToList();
        }

        EnsureBluetoothTcpChannels(config.BluetoothTcpChannels);
    }

    /// <summary>判断台体切换配置是否已经有新版端点或旧版单IP端点。</summary>
    private static bool HasBenchTypeSwitchEndpoints(MeterTestBenchTypeSwitchConfig config)
    {
        return config.Endpoints?.Count > 0 ||
               (!string.IsNullOrWhiteSpace(config.Ip) && config.Port is >= 1 and <= 65535);
    }

    /// <summary>补齐台体切换端点，并兼容旧版单IP/Port配置。</summary>
    private static void EnsureBenchTypeSwitchEndpoints(MeterTestBenchTypeSwitchConfig benchConfig)
    {
        benchConfig.Endpoints ??= new List<MeterTestBenchTypeSwitchEndpoint>();
        if (benchConfig.Endpoints.Count == 0)
        {
            string legacyIp = benchConfig.Ip?.Trim() ?? string.Empty;
            int legacyPort = benchConfig.Port;
            if (!string.IsNullOrWhiteSpace(legacyIp) && legacyPort is >= 1 and <= 65535)
            {
                benchConfig.Endpoints.Add(CreateBenchTypeSwitchEndpoint("台体切换-1", legacyIp, legacyPort, true));
                if (legacyIp.Equals("192.168.127.121", StringComparison.OrdinalIgnoreCase) && legacyPort == 8080)
                {
                    benchConfig.Endpoints.Add(CreateBenchTypeSwitchEndpoint("台体切换-2", "192.168.127.122", 8080, false));
                    benchConfig.Endpoints.Add(CreateBenchTypeSwitchEndpoint("台体切换-3", "192.168.127.123", 8080, false));
                }
            }
            else
            {
                benchConfig.Endpoints.AddRange(CreateDefaultBenchTypeSwitchEndpoints());
            }
        }

        foreach (MeterTestBenchTypeSwitchEndpoint endpoint in benchConfig.Endpoints)
        {
            endpoint.SupportsSinglePhase = endpoint.Name.Trim() switch
            {
                "台体切换-1" => true,
                "台体切换-2" => false,
                "台体切换-3" => false,
                _ => endpoint.SupportsSinglePhase
            };
        }

        benchConfig.Ip = string.Empty;
        benchConfig.Port = 0;
    }

    /// <summary>为源配置补齐协议名称，旧配置默认按XYCtr处理。</summary>
    private static void EnsureSourceControlProtocols(IEnumerable<MeterTestSourceControlConfig> sourceConfigs)
    {
        foreach (MeterTestSourceControlConfig sourceConfig in sourceConfigs)
        {
            if (string.IsNullOrWhiteSpace(sourceConfig.Protocol))
            {
                sourceConfig.Protocol = MeterTestSourceProtocol.XYCtr.ToString();
            }
        }
    }

    /// <summary>补齐1-48工位的控制PCB三工位分组。</summary>
    private static void EnsureControlPcbGroups(List<MeterTestControlPcbGroup> groups)
    {
        for (int stationStart = 1; stationStart <= MaximumStationCount; stationStart += StationsPerControlPcb)
        {
            int stationEnd = Math.Min(stationStart + StationsPerControlPcb - 1, MaximumStationCount);
            int groupIndex = (stationStart - 1) / StationsPerControlPcb + 1;
            MeterTestControlPcbGroup? existingGroup = groups.FirstOrDefault(group =>
                group.StationStart == stationStart ||
                string.Equals(group.Name, $"控制PCB-{groupIndex}", StringComparison.OrdinalIgnoreCase));
            if (existingGroup is null)
            {
                groups.Add(CreateControlPcbGroup(groupIndex, 4000 + groupIndex, stationStart, stationEnd));
                continue;
            }

            if (existingGroup.StationEnd < stationEnd)
            {
                existingGroup.StationEnd = stationEnd;
            }
        }

        groups.Sort((left, right) => left.StationStart.CompareTo(right.StationStart));
    }

    /// <summary>补齐1-48工位的蓝牙TCP通道占位配置。</summary>
    private static void EnsureBluetoothTcpChannels(List<MeterTestBluetoothTcpChannel> channels)
    {
        for (int station = 1; station <= MaximumStationCount; station++)
        {
            if (channels.Any(channel => channel.Station == station))
                continue;

            channels.Add(new MeterTestBluetoothTcpChannel
            {
                Station = station,
                Enabled = false,
                Ip = string.Empty,
                Port = 0
            });
        }

        channels.Sort((left, right) => left.Station.CompareTo(right.Station));
    }

    /// <summary>创建默认的三个台体切换端点。</summary>
    private static List<MeterTestBenchTypeSwitchEndpoint> CreateDefaultBenchTypeSwitchEndpoints() => new()
    {
        CreateBenchTypeSwitchEndpoint("台体切换-1", "192.168.127.121", 8080, true),
        CreateBenchTypeSwitchEndpoint("台体切换-2", "192.168.127.122", 8080, false),
        CreateBenchTypeSwitchEndpoint("台体切换-3", "192.168.127.123", 8080, false)
    };

    /// <summary>创建单个台体切换端点。</summary>
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

    /// <summary>创建默认源配置。</summary>
    private static MeterTestSourceControlConfig CreateSourceControlConfig(
        string name,
        MeterTestSourcePhaseMode phaseMode,
        string voltage,
        string current) => new()
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

    /// <summary>创建默认控制PCB分组。</summary>
    private static MeterTestControlPcbGroup CreateControlPcbGroup(
        int index,
        int port,
        int stationStart,
        int stationEnd) => new()
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

    /// <summary>深复制台体切换配置及其端点集合，避免运行时修改污染 XML 缓存。</summary>
    private static MeterTestBenchTypeSwitchConfig CloneBenchTypeSwitchConfig(MeterTestBenchTypeSwitchConfig source) => new()
    {
        Enabled = source.Enabled,
        Ip = source.Ip,
        Port = source.Port,
        TimeoutMs = source.TimeoutMs,
        DelayAfterSuccessMs = source.DelayAfterSuccessMs,
        Endpoints = source.Endpoints.Select(endpoint => new MeterTestBenchTypeSwitchEndpoint
        {
            Name = endpoint.Name,
            Enabled = endpoint.Enabled,
            Ip = endpoint.Ip,
            Port = endpoint.Port,
            SupportsSinglePhase = endpoint.SupportsSinglePhase
        }).ToList()
    };

    /// <summary>复制源控制配置的全部厂家接口和默认输出参数。</summary>
    private static MeterTestSourceControlConfig CloneSourceControlConfig(MeterTestSourceControlConfig source) => new()
    {
        Name = source.Name,
        Enabled = source.Enabled,
        Protocol = source.Protocol,
        PhaseMode = source.PhaseMode,
        InterfaceType = source.InterfaceType,
        SourcePort = source.SourcePort,
        OpenCommBeforeOutput = source.OpenCommBeforeOutput,
        VerificationTimeoutSeconds = source.VerificationTimeoutSeconds,
        VerificationIntervalSeconds = source.VerificationIntervalSeconds,
        VerificationTolerancePercent = source.VerificationTolerancePercent,
        Voltage = source.Voltage,
        Current = source.Current,
        VoltageA = source.VoltageA,
        VoltageB = source.VoltageB,
        VoltageC = source.VoltageC,
        CurrentA = source.CurrentA,
        CurrentB = source.CurrentB,
        CurrentC = source.CurrentC,
        CurrentAngleA = source.CurrentAngleA,
        CurrentAngleB = source.CurrentAngleB,
        CurrentAngleC = source.CurrentAngleC,
        Uab = source.Uab,
        Uac = source.Uac,
        Phase = source.Phase,
        PowerFactor = source.PowerFactor,
        Pulse = source.Pulse,
        ShutMode = source.ShutMode,
        Description = source.Description
    };

    /// <summary>复制控制 PCB 分组、工位范围和表位地址映射。</summary>
    private static MeterTestControlPcbGroup CloneControlPcbGroup(MeterTestControlPcbGroup source) => new()
    {
        Name = source.Name,
        Enabled = source.Enabled,
        Ip = source.Ip,
        Port = source.Port,
        ProtocolVersion = source.ProtocolVersion,
        StationStart = source.StationStart,
        StationEnd = source.StationEnd,
        MeterAddressStart = source.MeterAddressStart
    };

    /// <summary>复制单工位蓝牙专用 TCP 通道配置。</summary>
    private static MeterTestBluetoothTcpChannel CloneBluetoothTcpChannel(MeterTestBluetoothTcpChannel source) => new()
    {
        Station = source.Station,
        Enabled = source.Enabled,
        Ip = source.Ip,
        Port = source.Port
    };
}
