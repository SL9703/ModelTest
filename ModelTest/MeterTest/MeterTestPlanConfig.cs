using System.Collections.Generic;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 测试方案配置根对象。
/// XML 层级为：源控制配置、控制 PCB 配置、测试方案。
/// </summary>
[XmlRoot("MeterTestPlanConfig")]
public class MeterTestPlanConfig
{
    /// <summary>
    /// 源控制配置集合，用于执行测试前升源或降源。
    /// </summary>
    [XmlArray("SourceControlConfigs")]
    [XmlArrayItem("SourceControlConfig")]
    public List<MeterTestSourceControlConfig> SourceControlConfigs { get; set; } = new();

    /// <summary>
    /// 控制 PCB 配置集合，用于日计时等控制 PCB 测试模式。
    /// </summary>
    [XmlArray("ControlPcbGroups")]
    [XmlArrayItem("ControlPcbGroup")]
    public List<MeterTestControlPcbGroup> ControlPcbGroups { get; set; } = new();

    /// <summary>
    /// 测试方案集合。界面左侧 TreeView 按此集合生成方案树。
    /// </summary>
    [XmlElement("Scheme")]
    public List<MeterTestScheme> Schemes { get; set; } = new();
}

/// <summary>
/// 控制 PCB 与工位/表位的映射。
/// 例如一个 PCB 控制 1-3 工位，另一个 PCB 控制 4-6 工位。
/// </summary>
public class MeterTestControlPcbGroup
{
    /// <summary>控制 PCB 名称，作为配置和日志中的唯一标识。</summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>是否启用该控制 PCB。</summary>
    [XmlAttribute("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>控制 PCB 的 IP 地址。</summary>
    [XmlAttribute("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>控制 PCB 的 TCP 端口。</summary>
    [XmlAttribute("port")]
    public int Port { get; set; }

    /// <summary>控制 PCB 协议版本，当前支持 V1 / V2。</summary>
    [XmlAttribute("protocolVersion")]
    public string ProtocolVersion { get; set; } = MeterControlPcbProtocolVersion.V2.ToString();

    /// <summary>该 PCB 管理的起始工位号。</summary>
    [XmlAttribute("stationStart")]
    public int StationStart { get; set; } = 1;

    /// <summary>该 PCB 管理的结束工位号。</summary>
    [XmlAttribute("stationEnd")]
    public int StationEnd { get; set; } = 20;

    /// <summary>起始工位对应的 PCB 表位地址，后续工位按顺序递增。</summary>
    [XmlAttribute("meterAddressStart")]
    public int MeterAddressStart { get; set; } = 1;
}

/// <summary>
/// 测试方案节点，对应 TreeView 第一层。
/// </summary>
public class MeterTestScheme
{
    /// <summary>方案名称。</summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>方案描述。</summary>
    [XmlAttribute("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>测试项集合，对应 TreeView 第二层。</summary>
    [XmlElement("TestItem")]
    public List<MeterTestItem> TestItems { get; set; } = new();
}

/// <summary>
/// 测试项节点，例如“通信测试”“日计时”。
/// </summary>
public class MeterTestItem
{
    /// <summary>测试项名称。</summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>测试项描述。</summary>
    [XmlAttribute("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>测试小项集合，对应 TreeView 第三层。</summary>
    [XmlElement("TestSubItem")]
    public List<MeterTestSubItem> TestSubItems { get; set; } = new();
}

/// <summary>
/// 测试小项节点，是实际执行的最小测试单元。
/// StationTcp 模式走工位 IP/Port，一发一收；ControlPcbDailyTiming 模式走控制 PCB。
/// </summary>
public class MeterTestSubItem
{
    /// <summary>测试小项名称。</summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>是否启用该测试小项。</summary>
    [XmlAttribute("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>协议名称，仅用于标识和日志展示。</summary>
    [XmlAttribute("protocol")]
    public string Protocol { get; set; } = string.Empty;

    /// <summary>执行模式，决定点击开始测试后进入哪套流程。</summary>
    [XmlAttribute("executionMode")]
    public string ExecutionMode { get; set; } = MeterTestExecutionMode.StationTcp.ToString();

    /// <summary>指定控制 PCB 组名；为空时按选中工位自动匹配启用的 PCB 组。</summary>
    [XmlAttribute("controlPcbGroup")]
    public string ControlPcbGroup { get; set; } = string.Empty;

    /// <summary>执行测试前使用的源控制配置名称。</summary>
    [XmlAttribute("sourceControlConfig")]
    public string SourceControlConfig { get; set; } = string.Empty;

    /// <summary>日计时测试时间，单位秒。</summary>
    [XmlAttribute("dailyTimingTime")]
    public int DailyTimingTime { get; set; } = 10;

    /// <summary>日计时测试次数。</summary>
    [XmlAttribute("dailyTimingCount")]
    public int DailyTimingCount { get; set; } = 10;

    /// <summary>同一连接内连续发送报文的间隔，单位毫秒。</summary>
    [XmlAttribute("packetIntervalMs")]
    public int PacketIntervalMs { get; set; } = 100;

    /// <summary>StationTcp 模式下发送的请求 HEX 报文。</summary>
    [XmlAttribute("requestHex")]
    public string RequestHex { get; set; } = string.Empty;

    /// <summary>响应解析器类型，决定如何判定应答是否合格。</summary>
    [XmlAttribute("responseParser")]
    public string ResponseParser { get; set; } = ResponseParserType.HexMatch.ToString();

    /// <summary>普通 HEX 匹配的期望响应。</summary>
    [XmlAttribute("expectedResponse")]
    public string ExpectedResponse { get; set; } = string.Empty;

    /// <summary>普通 HEX 匹配模式：Exact / Contains / StartsWith。</summary>
    [XmlAttribute("responseMatchMode")]
    public string MatchMode { get; set; } = ResponseMatchMode.Contains.ToString();

    /// <summary>698 解析时期望的 APDU 标识。</summary>
    [XmlAttribute("expectedApdu")]
    public string ExpectedApdu { get; set; } = string.Empty;

    /// <summary>698 解析时期望的 OAD。</summary>
    [XmlAttribute("expectedOad")]
    public string ExpectedOad { get; set; } = string.Empty;

    /// <summary>698 解析时期望的数据类型。</summary>
    [XmlAttribute("expectedDataType")]
    public string ExpectedDataType { get; set; } = string.Empty;

    /// <summary>698 解析时期望的数据长度。</summary>
    [XmlAttribute("expectedDataLength")]
    public int ExpectedDataLength { get; set; } = 0;

    /// <summary>解析结果写入字段，例如 MeterAddress。</summary>
    [XmlAttribute("resultField")]
    public string ResultField { get; set; } = string.Empty;

    /// <summary>等待响应超时时间，单位毫秒。</summary>
    [XmlAttribute("timeoutMs")]
    public int TimeoutMs { get; set; } = 3000;

    /// <summary>测试小项说明。</summary>
    [XmlAttribute("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>调试用模拟响应，现场真实测试不依赖该值。</summary>
    [XmlAttribute("mockResponse")]
    public string MockResponse { get; set; } = string.Empty;
}

/// <summary>
/// 普通响应匹配模式。
/// </summary>
public enum ResponseMatchMode
{
    Exact,
    Contains,
    StartsWith
}

/// <summary>
/// 响应解析器类型。
/// </summary>
public enum ResponseParserType
{
    HexMatch,
    Sgcc698BroadcastAddress,
    MeterControlDailyTiming
}

/// <summary>
/// 测试执行模式。
/// </summary>
public enum MeterTestExecutionMode
{
    StationTcp,
    ControlPcbDailyTiming
}

/// <summary>
/// 控制 PCB 协议版本。
/// </summary>
public enum MeterControlPcbProtocolVersion
{
    V1,
    V2
}

/// <summary>
/// 源控制配置。
/// 执行测试前可按这里的配置调用源接口进行升源或降源。
/// </summary>
public class MeterTestSourceControlConfig
{
    /// <summary>源控制配置名称。</summary>
    [XmlAttribute("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>是否启用该源控制配置。</summary>
    [XmlAttribute("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>测量单元类型：SinglePhase / ThreePhase。</summary>
    [XmlAttribute("phaseMode")]
    public string PhaseMode { get; set; } = MeterTestSourcePhaseMode.ThreePhase.ToString();

    /// <summary>源控制接口类型。</summary>
    [XmlAttribute("interfaceType")]
    public string InterfaceType { get; set; } = MeterTestSourceInterfaceType.AnyUIOutput.ToString();

    /// <summary>源串口号。</summary>
    [XmlAttribute("sourcePort")]
    public int SourcePort { get; set; } = 1;

    /// <summary>输出前是否先打开源通信口。</summary>
    [XmlAttribute("openCommBeforeOutput")]
    public bool OpenCommBeforeOutput { get; set; } = true;

    /// <summary>默认电压。</summary>
    [XmlAttribute("voltage")]
    public string Voltage { get; set; } = "220";

    /// <summary>默认电流。</summary>
    [XmlAttribute("current")]
    public string Current { get; set; } = "5";

    /// <summary>A 相电压。为空时使用默认电压。</summary>
    [XmlAttribute("voltageA")]
    public string VoltageA { get; set; } = string.Empty;

    /// <summary>B 相电压。为空时使用默认电压。</summary>
    [XmlAttribute("voltageB")]
    public string VoltageB { get; set; } = string.Empty;

    /// <summary>C 相电压。为空时使用默认电压。</summary>
    [XmlAttribute("voltageC")]
    public string VoltageC { get; set; } = string.Empty;

    /// <summary>A 相电流。为空时使用默认电流。</summary>
    [XmlAttribute("currentA")]
    public string CurrentA { get; set; } = string.Empty;

    /// <summary>B 相电流。为空时使用默认电流。</summary>
    [XmlAttribute("currentB")]
    public string CurrentB { get; set; } = string.Empty;

    /// <summary>C 相电流。为空时使用默认电流。</summary>
    [XmlAttribute("currentC")]
    public string CurrentC { get; set; } = string.Empty;

    /// <summary>A 相电流角度。</summary>
    [XmlAttribute("currentAngleA")]
    public string CurrentAngleA { get; set; } = "0";

    /// <summary>B 相电流角度。</summary>
    [XmlAttribute("currentAngleB")]
    public string CurrentAngleB { get; set; } = "0";

    /// <summary>C 相电流角度。</summary>
    [XmlAttribute("currentAngleC")]
    public string CurrentAngleC { get; set; } = "0";

    /// <summary>Uab 相角参数。</summary>
    [XmlAttribute("uab")]
    public string Uab { get; set; } = "120";

    /// <summary>Uac 相角参数。</summary>
    [XmlAttribute("uac")]
    public string Uac { get; set; } = "240";

    /// <summary>源输出相别，例如 A / B / C / H。</summary>
    [XmlAttribute("phase")]
    public string Phase { get; set; } = "H";

    /// <summary>功率因数，Adj 接口会将该值转换为源识别的功率因数字段。</summary>
    [XmlAttribute("powerFactor")]
    public string PowerFactor { get; set; } = "1.0";

    /// <summary>源输出脉冲参数。</summary>
    [XmlAttribute("pulse")]
    public int Pulse { get; set; } = 2;

    /// <summary>降源模式，ShutPowerSource 接口使用。</summary>
    [XmlAttribute("shutMode")]
    public int ShutMode { get; set; } = 0;

    /// <summary>源控制配置说明。</summary>
    [XmlAttribute("description")]
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 源控制相制类型。
/// </summary>
public enum MeterTestSourcePhaseMode
{
    SinglePhase,
    ThreePhase
}

/// <summary>
/// 源控制接口类型。
/// </summary>
public enum MeterTestSourceInterfaceType
{
    AnyUIOutput,
    Adj,
    RangeOutputUI,
    ShutPowerSource
}
