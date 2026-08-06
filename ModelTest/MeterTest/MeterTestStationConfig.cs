using System.Collections.Generic;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

/// <summary>
/// 工位通信配置 XML 根节点。
/// 保存 1-N 工位对应的 IP/端口，以及现场设备端点配置。
/// 测试方案 XML 只描述测试流程；源、控制PCB、台体切换和蓝牙通道等现场连接参数统一放在这里。
/// </summary>
[XmlRoot("MeterTestStationConfig")]
public sealed class MeterTestStationConfig
{
    /// <summary>
    /// 升源前台体类型切换装置通信板配置。
    /// </summary>
    [XmlElement("BenchTypeSwitchConfig")]
    public MeterTestBenchTypeSwitchConfig BenchTypeSwitchConfig { get; set; } = new();

    /// <summary>
    /// 源控制配置集合，用于执行升源、降源和标准表达标校验。
    /// </summary>
    [XmlArray("SourceControlConfigs")]
    [XmlArrayItem("SourceControlConfig")]
    public List<MeterTestSourceControlConfig> SourceControlConfigs { get; set; } = new();

    /// <summary>
    /// 控制PCB与工位/表位映射集合，用于日计时、误差、潜动、常数试验和工位上下电。
    /// </summary>
    [XmlArray("ControlPcbGroups")]
    [XmlArrayItem("ControlPcbGroup")]
    public List<MeterTestControlPcbGroup> ControlPcbGroups { get; set; } = new();

    /// <summary>
    /// 蓝牙转换器专用TCP通道集合，独立于资产信息中的485通信IP/端口。
    /// </summary>
    [XmlArray("BluetoothTcpChannels")]
    [XmlArrayItem("BluetoothTcpChannel")]
    public List<MeterTestBluetoothTcpChannel> BluetoothTcpChannels { get; set; } = new();

    /// <summary>
    /// 工位指示灯控制板映射集合。
    /// 只有配置文件缺少该节点时才生成默认模板，用户已有 IP/端口和地址映射不会被程序启动覆盖。
    /// </summary>
    [XmlArray("IndicatorLightGroups")]
    [XmlArrayItem("IndicatorLightGroup")]
    public List<MeterTestIndicatorLightGroup> IndicatorLightGroups { get; set; } = new();

    /// <summary>
    /// 工位485通信通道集合。
    /// 485-2为地址读取默认上行通道，485-1作为备用/扩展通道保留独立IP和端口配置。
    /// </summary>
    [XmlArray("StationTcpChannels")]
    [XmlArrayItem("StationTcpChannel")]
    public List<MeterTestStationTcpChannel> StationTcpChannels { get; set; } = new();

    /// <summary>
    /// 旧版根级工位列表兼容入口。
    /// 旧XML仍可读取，加载后会迁移到默认485-2通道；新版保存时不再输出根级Station。
    /// </summary>
    [XmlElement("Station")]
    public List<MeterTestStationCommunication> LegacyStations { get; set; } = new();

    /// <summary>
    /// 默认工位通信列表。
    /// 现有测试流程继续通过该属性使用485-2，不需要在业务代码里关心XML层级变化。
    /// </summary>
    [XmlIgnore]
    public List<MeterTestStationCommunication> Stations
    {
        get => GetDefaultStationTcpChannel().Stations;
        set => GetDefaultStationTcpChannel().Stations = value ?? new List<MeterTestStationCommunication>();
    }

    /// <summary>获取默认485-2通道；缺失时自动创建，保证旧调用方始终有可用Station列表。</summary>
    public MeterTestStationTcpChannel GetDefaultStationTcpChannel()
    {
        MeterTestStationTcpChannel? defaultChannel = StationTcpChannels.FirstOrDefault(channel => channel.IsDefault)
            ?? StationTcpChannels.FirstOrDefault(channel => channel.Channel.Equals("485-2", StringComparison.OrdinalIgnoreCase))
            ?? StationTcpChannels.FirstOrDefault();
        if (defaultChannel is not null)
            return defaultChannel;

        defaultChannel = new MeterTestStationTcpChannel
        {
            Name = "485-2通信通道",
            Channel = "485-2",
            Enabled = true,
            IsDefault = true
        };
        StationTcpChannels.Add(defaultChannel);
        return defaultChannel;
    }
}

/// <summary>
/// 工位485通信通道配置。
/// 一个通道下包含1-48工位的IP/Port映射，便于同时维护485-1和485-2。
/// </summary>
public sealed class MeterTestStationTcpChannel
{
    /// <summary>通道名称，显示和日志使用。</summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>通道编号，例如485-1、485-2。</summary>
    [XmlAttribute("channel")]
    public string Channel { get; set; } = string.Empty;

    /// <summary>是否启用该通道。</summary>
    [XmlAttribute("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>是否为默认地址读取通道。</summary>
    [XmlAttribute("isDefault")]
    public bool IsDefault { get; set; }

    /// <summary>该通道下每个工位的IP和端口。</summary>
    [XmlArray("Stations")]
    [XmlArrayItem("Station")]
    public List<MeterTestStationCommunication> Stations { get; set; } = new();
}

/// <summary>
/// 单个工位的通信参数。
/// </summary>
public sealed class MeterTestStationCommunication
{
    /// <summary>
    /// 工位号，当前范围为 1-48。
    /// </summary>
    [XmlAttribute("stationNo")]
    public int StationNo { get; set; }

    /// <summary>
    /// 该工位对应的串口服务器或 TCP 服务 IP。
    /// </summary>
    [XmlAttribute("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// 该工位对应的串口服务器或 TCP 服务端口。
    /// </summary>
    [XmlAttribute("port")]
    public int Port { get; set; }
}
