using System.Collections.Generic;
using System.Xml.Serialization;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 测试方案配置根对象。
/// XML 层级为：源控制配置、控制 PCB 配置、蓝牙 TCP 通道配置、测试方案。
/// </summary>
[XmlRoot("MeterTestPlanConfig")]
public class MeterTestPlanConfig
{
    /// <summary>
    /// 升源前的台体类型切换配置，根据资产信息选择单相、三相直接式或三相互感式。
    /// </summary>
    [XmlElement("BenchTypeSwitchConfig")]
    public MeterTestBenchTypeSwitchConfig BenchTypeSwitchConfig { get; set; } = new();

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
    /// 工位与蓝牙转换器专用 TCP 端点的映射。
    /// 该配置独立于资产信息中的 485 IP/Port，蓝牙流程禁止回退使用 485 端点。
    /// </summary>
    [XmlArray("BluetoothTcpChannels")]
    [XmlArrayItem("BluetoothTcpChannel")]
    public List<MeterTestBluetoothTcpChannel> BluetoothTcpChannels { get; set; } = new();

    /// <summary>
    /// 测试方案集合。界面左侧 TreeView 按此集合生成方案树。
    /// </summary>
    [XmlElement("Scheme")]
    public List<MeterTestScheme> Schemes { get; set; } = new();
}

/// <summary>
/// 升源前通过装置通信板切换台体接线类型的固定通信配置。
/// </summary>
public class MeterTestBenchTypeSwitchConfig
{
    /// <summary>是否启用升源前台体类型切换。</summary>
    [XmlAttribute("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>装置通信板固定 IP。</summary>
    [XmlAttribute("ip")]
    public string Ip { get; set; } = "192.168.127.101";

    /// <summary>装置通信板固定 TCP 端口。</summary>
    [XmlAttribute("port")]
    public int Port { get; set; } = 4001;

    /// <summary>连接和应答超时时间，单位毫秒。</summary>
    [XmlAttribute("timeoutMs")]
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>切换成功后进入控源流程前的等待时间，单位毫秒。</summary>
    [XmlAttribute("delayAfterSuccessMs")]
    public int DelayAfterSuccessMs { get; set; } = 1000;
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
/// 单个工位的蓝牙转换器专用 TCP 通道。
/// 每个工位单独配置，便于多工位并发时分别建立连接。
/// </summary>
public class MeterTestBluetoothTcpChannel
{
    /// <summary>工位号，有效范围为1-48。</summary>
    [XmlAttribute("station")]
    public int Station { get; set; }

    /// <summary>是否启用该工位的蓝牙通道。</summary>
    [XmlAttribute("enabled")]
    public bool Enabled { get; set; }

    /// <summary>蓝牙转换器专用 TCP IP，不是资产信息中的 485 IP。</summary>
    [XmlAttribute("ip")]
    public string Ip { get; set; } = string.Empty;

    /// <summary>蓝牙转换器专用 TCP 端口，不是资产信息中的 485 Port。</summary>
    [XmlAttribute("port")]
    public int Port { get; set; }
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
/// StationTcp 模式走工位 IP/Port，一发一收；ControlPcbDailyTiming 模式走控制 PCB；
/// SerialPortServerBaudRateSync 模式执行通信测试中的串口服务器波特率流程。
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

    /// <summary>
    /// 串口服务器波特率流程的步骤名称：Connect、ReadParameters、Compare、Apply。
    /// 仅在 SerialPortServerBaudRateSync 模式下生效。
    /// </summary>
    [XmlAttribute("serialPortServerStep")]
    public string SerialPortServerStep { get; set; } = string.Empty;

    /// <summary>
    /// 蓝牙专用TCP流程步骤：Reset、ConnectMeter、Preprocess、ReadAddress。
    /// 每个选中工位都根据BluetoothTcpChannels使用独立TcpClient，
    /// 不复用资产信息中的485端点、StationTcp或控制PCB连接。
    /// </summary>
    [XmlAttribute("bluetoothStep")]
    public string BluetoothStep { get; set; } = string.Empty;

    /// <summary>日计时测试时间，单位秒。</summary>
    [XmlAttribute("dailyTimingTime")]
    public int DailyTimingTime { get; set; } = 10;

    /// <summary>日计时测试次数。</summary>
    [XmlAttribute("dailyTimingCount")]
    public int DailyTimingCount { get; set; } = 10;

    /// <summary>同一连接内连续发送报文的间隔，单位毫秒。</summary>
    [XmlAttribute("packetIntervalMs")]
    public int PacketIntervalMs { get; set; } = 100;

    /// <summary>
    /// 日计时流程步骤：Start、Wait、Read。
    /// 仅在 ControlPcbDailyTiming 模式下生效。
    /// </summary>
    [XmlAttribute("dailyTimingStep")]
    public string DailyTimingStep { get; set; } = string.Empty;

    /// <summary>日计时流程轮次，当前默认执行 1-3 轮。</summary>
    [XmlAttribute("dailyTimingRound")]
    public int DailyTimingRound { get; set; }

    /// <summary>0x35潜动走字试验的目标脉冲数，协议字段为1字节。</summary>
    [XmlAttribute("creepingPulseCount")]
    public int CreepingPulseCount { get; set; } = 1;

    /// <summary>0x35潜动走字试验的手动配置时间，单位秒；启动节点和等待节点分别读取各自配置值。</summary>
    [XmlAttribute("creepingTimeSeconds")]
    public int CreepingTimeSeconds { get; set; } = 60;

    /// <summary>0x38基本误差试验使用的被测表脉冲数；0表示按不少于10秒自动计算，1-99表示固定值。</summary>
    [XmlAttribute("basicErrorPulseCount")]
    public int BasicErrorPulseCount { get; set; } = 2;

    /// <summary>0x38基本误差试验次数，允许1-10，起动试验默认1。</summary>
    [XmlAttribute("basicErrorTestCount")]
    public int BasicErrorTestCount { get; set; } = 1;

    /// <summary>0x38脉冲类型：0表示有功，1表示无功。</summary>
    [XmlAttribute("basicErrorPulseType")]
    public int BasicErrorPulseType { get; set; }

    /// <summary>误差结果绝对值阈值；大于0时使用配置值，基本误差点配置0时按JJG596表1自动计算。</summary>
    [XmlAttribute("basicErrorLimit")]
    public decimal BasicErrorLimit { get; set; } = 1.5m;

    /// <summary>基本误差电能方向：ForwardActive（正向有功）或 ReverseActive（反向有功）。</summary>
    [XmlAttribute("basicErrorDirection")]
    public string BasicErrorDirection { get; set; } = string.Empty;

    /// <summary>基本误差输出相别，H 表示合源，A/B/C 表示分相。</summary>
    [XmlAttribute("basicErrorPhase")]
    public string BasicErrorPhase { get; set; } = "H";

    /// <summary>基本误差功率因数，例如 1.0、0.5L、0.8C。</summary>
    [XmlAttribute("basicErrorPowerFactor")]
    public string BasicErrorPowerFactor { get; set; } = "1.0";

    /// <summary>基本误差电压倍数，1 表示 1U（额定电压）。</summary>
    [XmlAttribute("basicErrorVoltageMultiplier")]
    public decimal BasicErrorVoltageMultiplier { get; set; } = 1m;

    /// <summary>基本误差电流点：Imin、Itr、10Itr、0.5Imax、Imax、1.2Imax。</summary>
    [XmlAttribute("basicErrorCurrentPoint")]
    public string BasicErrorCurrentPoint { get; set; } = string.Empty;

    /// <summary>基本误差单次测量的最短时间，单位秒。</summary>
    [XmlAttribute("basicErrorMinimumWaitSeconds")]
    public int BasicErrorMinimumWaitSeconds { get; set; } = 10;

    /// <summary>基本误差理论测量时间结束后的结果计算余量，单位秒。</summary>
    [XmlAttribute("basicErrorWaitPaddingSeconds")]
    public int BasicErrorWaitPaddingSeconds { get; set; } = 10;

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
    MeterControlDailyTiming,
    MeterControlCreepingTest
}

/// <summary>
/// 测试执行模式。
/// </summary>
public enum MeterTestExecutionMode
{
    StationTcp,
    ControlPcbDailyTiming,
    /// <summary>
    /// 读取标准表脉冲常数，并通过控制PCB依次下发A2、A0和0x38开始试验命令。
    /// </summary>
    ControlPcbStartingError,
    SerialPortServerBaudRateSync,
    /// <summary>
    /// 根据资产信息计算 Ist，并在升源前把启动电流用于电表初始化参数。
    /// </summary>
    StartingSource,
    /// <summary>
    /// 根据资产额定电压计算1.1倍潜动电压，单相输出Ua、三相输出Ua/Ub/Uc，电流保持为0。
    /// </summary>
    CreepingSource,
    /// <summary>通过V2控制PCB按工位发送0x35潜动走字试验启动报文，并等待逐表位应答。</summary>
    ControlPcbCreepingStart,
    /// <summary>按XML中手动配置的秒数等待潜动试验，不执行公式计算。</summary>
    CreepingWait,
    /// <summary>通过V2控制PCB按工位发送0x35+AA，并读取累计脉冲数和累计时间。</summary>
    ControlPcbCreepingRead,
    /// <summary>根据已读取的累计脉冲数判定潜动试验，脉冲数小于等于1为合格。</summary>
    CreepingPulseJudge,
    /// <summary>
    /// 根据资产档案计算各工位Tst上限，并按最大值统一等待。
    /// </summary>
    StartingTimeWait,
    /// <summary>通过控制PCB发送0x38+AA读取起动误差float结果。</summary>
    ControlPcbStartingErrorRead,
    /// <summary>按配置阈值判断已读取的起动误差结果。</summary>
    StartingErrorJudge,
    /// <summary>执行单个基本误差测试点内部的升源、启动、等待、读取和判定完整流程。</summary>
    BasicErrorPoint,
    /// <summary>按工位建立独立TCP连接，执行国网智芯蓝牙转换器检测步骤。</summary>
    BluetoothStationTcp,
    /// <summary>
    /// 仅用于在方案树中预置尚未接入报文的测试流程，启用前需要补充对应执行器。
    /// </summary>
    Planned
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

    /// <summary>升源后等待标准表进入允许误差范围的最长时间，单位为秒。</summary>
    [XmlAttribute("verificationTimeoutSeconds")]
    public int VerificationTimeoutSeconds { get; set; } = 20;

    /// <summary>升源验证期间读取标准表的时间间隔，单位为秒。</summary>
    [XmlAttribute("verificationIntervalSeconds")]
    public int VerificationIntervalSeconds { get; set; } = 3;

    /// <summary>电压和电流允许误差，单位为百分数；0.03 表示正负 0.03%。</summary>
    [XmlAttribute("verificationTolerancePercent")]
    public decimal VerificationTolerancePercent { get; set; } = 0.03m;

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
