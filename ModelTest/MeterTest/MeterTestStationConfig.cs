using System.Collections.Generic;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

/// <summary>
/// 工位通信配置 XML 根节点。
/// 保存 1-N 工位对应的 IP 和端口，用于 StationTcp 测试模式。
/// </summary>
[XmlRoot("MeterTestStationConfig")]
public sealed class MeterTestStationConfig
{
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
