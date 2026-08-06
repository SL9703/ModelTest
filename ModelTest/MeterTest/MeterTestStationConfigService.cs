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
        planConfig.IndicatorLightGroups = stationConfig.IndicatorLightGroups;
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
    /// 确保存在工位485通信通道节点。
    /// 只在StationTcpChannels完全缺失时创建模板；如果用户已经维护了相关节点，程序启动不补齐、不排序、不改IP和端口。
    /// </summary>
    private static void EnsureStations(MeterTestStationConfig config, int stationCount, string defaultIp, int defaultStartPort)
    {
        EnsureStationTcpChannels(config, stationCount, defaultIp, defaultStartPort);
    }

    /// <summary>
    /// 兼容旧版根级Station，并在缺少新版节点时创建485-1/485-2两个通道模板。
    /// 地址读取默认继续使用485-2，485-1只作为独立配置保存，不参与旧流程默认选择。
    /// </summary>
    private static void EnsureStationTcpChannels(
        MeterTestStationConfig config,
        int stationCount,
        string defaultIp,
        int defaultStartPort)
    {
        config.StationTcpChannels ??= new List<MeterTestStationTcpChannel>();
        if (config.StationTcpChannels.Count > 0)
            return;

        List<MeterTestStationCommunication> templateStations =
            config.LegacyStations is { Count: > 0 }
                ? config.LegacyStations.Select(CloneStationCommunication).ToList()
                : CreateDefaultStations(stationCount, defaultIp, defaultStartPort);
        EnsureChannelStations(templateStations, stationCount, defaultIp, defaultStartPort);

        config.StationTcpChannels.Add(new MeterTestStationTcpChannel
        {
            Name = "485-1通信通道",
            Channel = "485-1",
            Enabled = true,
            IsDefault = false,
            Stations = templateStations.Select(CloneStationCommunication).ToList()
        });
        config.StationTcpChannels.Add(new MeterTestStationTcpChannel
        {
            Name = "485-2通信通道",
            Channel = "485-2",
            Enabled = true,
            IsDefault = true,
            Stations = templateStations.Select(CloneStationCommunication).ToList()
        });

        config.LegacyStations.Clear();
    }

    /// <summary>创建首次使用时的默认工位通信模板。</summary>
    private static List<MeterTestStationCommunication> CreateDefaultStations(
        int stationCount,
        string defaultIp,
        int defaultStartPort)
    {
        List<MeterTestStationCommunication> stations = new();
        EnsureChannelStations(stations, stationCount, defaultIp, defaultStartPort);
        return stations;
    }

    /// <summary>补齐一个通道内1-48工位，并按工位号排序。</summary>
    private static void EnsureChannelStations(
        List<MeterTestStationCommunication> stations,
        int stationCount,
        string defaultIp,
        int defaultStartPort)
    {
        for (int stationNo = 1; stationNo <= stationCount; stationNo++)
        {
            if (stations.Any(station => station.StationNo == stationNo))
                continue;

            stations.Add(new MeterTestStationCommunication
            {
                StationNo = stationNo,
                Ip = defaultIp,
                Port = defaultStartPort + stationNo - 1
            });
        }

        List<MeterTestStationCommunication> sortedStations = stations
            .Where(station => station.StationNo >= 1 && station.StationNo <= stationCount)
            .OrderBy(station => station.StationNo)
            .ToList();
        stations.Clear();
        stations.AddRange(sortedStations);
    }

    /// <summary>复制工位通信配置，迁移旧节点或生成485-1模板时避免共享同一对象。</summary>
    private static MeterTestStationCommunication CloneStationCommunication(MeterTestStationCommunication source) => new()
    {
        StationNo = source.StationNo,
        Ip = source.Ip,
        Port = source.Port
    };

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

        config.IndicatorLightGroups ??= new List<MeterTestIndicatorLightGroup>();
        if (config.IndicatorLightGroups.Count == 0 && fallbackPlanConfig?.IndicatorLightGroups?.Count > 0)
        {
            config.IndicatorLightGroups = fallbackPlanConfig.IndicatorLightGroups
                .Select(CloneIndicatorLightGroup)
                .ToList();
        }

        EnsureIndicatorLightGroups(config.IndicatorLightGroups, config.ControlPcbGroups);
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

    /// <summary>
    /// 补齐控制 PCB 分组。
    /// 现场可能存在“三相 PCB 每个 IP 只控制两个工位”的非默认映射；
    /// 因此只在配置完全为空时生成默认三工位模板，已有配置一律保留，不自动扩展范围。
    /// </summary>
    private static void EnsureControlPcbGroups(List<MeterTestControlPcbGroup> groups)
    {
        if (groups.Count > 0)
        {
            groups.Sort((left, right) => left.StationStart.CompareTo(right.StationStart));
            return;
        }

        for (int stationStart = 1; stationStart <= MaximumStationCount; stationStart += StationsPerControlPcb)
        {
            int stationEnd = Math.Min(stationStart + StationsPerControlPcb - 1, MaximumStationCount);
            int groupIndex = (stationStart - 1) / StationsPerControlPcb + 1;
            groups.Add(CreateControlPcbGroup(groupIndex, 4000 + groupIndex, stationStart, stationEnd));
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

    /// <summary>
    /// 补齐工位指示灯控制板配置。
    /// 若用户已经配置 IndicatorLightGroups，则只排序不改 IP、端口、工位范围和灯地址；
    /// 只有完全缺失时才按当前 ControlPcbGroups 生成一份同端点模板，便于首次使用。
    /// </summary>
    private static void EnsureIndicatorLightGroups(
        List<MeterTestIndicatorLightGroup> groups,
        IReadOnlyList<MeterTestControlPcbGroup> controlPcbGroups)
    {
        if (groups.Count > 0)
        {
            groups.Sort((left, right) => left.StationStart.CompareTo(right.StationStart));
            return;
        }

        foreach (MeterTestControlPcbGroup controlGroup in controlPcbGroups.Where(group => group.Enabled))
        {
            groups.Add(new MeterTestIndicatorLightGroup
            {
                Name = $"{controlGroup.Name}-灯光",
                Enabled = true,
                Ip = controlGroup.Ip,
                Port = controlGroup.Port,
                ProtocolVersion = controlGroup.ProtocolVersion,
                StationStart = controlGroup.StationStart,
                StationEnd = controlGroup.StationEnd,
                LightAddressStart = controlGroup.MeterAddressStart
            });
        }

        groups.Sort((left, right) => left.StationStart.CompareTo(right.StationStart));
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

    /// <summary>复制工位指示灯配置，保留用户维护的端点和灯光地址映射。</summary>
    private static MeterTestIndicatorLightGroup CloneIndicatorLightGroup(MeterTestIndicatorLightGroup source) => new()
    {
        Name = source.Name,
        Enabled = source.Enabled,
        Ip = source.Ip,
        Port = source.Port,
        ProtocolVersion = source.ProtocolVersion,
        StationStart = source.StationStart,
        StationEnd = source.StationEnd,
        LightAddressStart = source.LightAddressStart
    };
}
