using System.Collections.Generic;
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
    /// 工位列表。程序启动时会自动补齐缺失工位，并按工位号排序。
    /// </summary>
    [XmlElement("Station")]
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
