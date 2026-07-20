namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 本地 SQLite 数据库表结构定义。
///
/// 说明：
/// 1. 这个文件保留了旧的类名，方便历史代码搜索和对照。
/// 2. 真实运行时的数据库实现已经切换为 SQLite 本地文件，不再依赖 Access/ACE/Jet。
/// 3. 这里的建表语句仅用于结构参考，与当前 SQLite 服务保持一致。
///
/// 表：MeterTestRun
/// 用途：记录一次完整测试运行的生命周期。
/// 字段：
/// - Id：自增主键。
/// - RunId：一次测试运行的唯一编号，程序启动后每次点击开始测试生成。
/// - SchemeName：方案名称。
/// - StartedAt：测试开始时间。
/// - EndedAt：测试结束时间。
/// - Status：运行状态，例如 Running / Completed / Failed / Cancelled。
/// - Remark：运行备注或异常信息。
/// 
/// 表：MeterTestStationResult
/// 用途：保存每个测试小项、每个工位的最新测试结论，用于界面切换和程序重启后恢复。
/// 字段：
/// - Id：自增主键。
/// - RunId：来源测试运行编号。
/// - SchemeName：方案名称。
/// - TestItemName：测试项名称，例如 通信测试 / 日计时。
/// - TestSubItemName：测试小项名称，例如 地址读取 / 日计时。
/// - StationNo：工位号。
/// - TestContent：界面“测试内容”列展示文本。
/// - MeterAddress：解析出的表位地址；日计时等无地址解析的项目可以为空。
/// - Result：测试结论，例如 测试中 / 合格 / 不合格 / 待测试。
/// - ResultTime：结果更新时间，使用文本形式保存。
/// - ResultTimeText：界面展示用时间文本，格式 HH:mm:ss。
/// - ToolTip：鼠标悬停提示内容。
/// - Message：结论说明或异常信息。
/// - ResultColorArgb：界面结果颜色 ARGB 整数，用于恢复显示颜色。
/// - UpdatedAt：数据库更新时间。
/// 
/// 表：MeterTestStationConfig
/// 用途：保存 20 个工位的通信配置，后续可替代 XML/界面临时配置。
/// 字段：
/// - Id：自增主键。
/// - StationNo：工位号。
/// - Ip：工位串口服务器或通信服务 IP。
/// - Port：工位串口服务器或通信服务端口。
/// - Enabled：是否启用该工位。
/// - UpdatedAt：配置更新时间。
/// 
/// 表：MeterTestControlPcbConfig
/// 用途：保存控制 PCB 与工位/表位的映射关系，后续可替代 XML 的 ControlPcbGroups。
/// 字段：
/// - Id：自增主键。
/// - Name：控制 PCB 名称。
/// - Ip：控制 PCB IP。
/// - Port：控制 PCB 端口。
/// - ProtocolVersion：控制 PCB 协议版本，V1 或 V2。
/// - StationStart：该 PCB 控制的起始工位。
/// - StationEnd：该 PCB 控制的结束工位。
/// - MeterAddressStart：StationStart 对应的 PCB 表位地址。
/// - Enabled：是否启用该 PCB。
/// - UpdatedAt：配置更新时间。
///
/// 表：MeterTestMeterArchive
/// 用途：保存每个工位的电表档案信息，并回填到测试过程区域。
/// 字段：
/// - Id：自增主键。
/// - StationNo：工位号。
/// - MeterType：电表类型，单相或三相。
/// - AccessMode：接入方式，直接式或互感式。
/// - Voltage：电表电压，默认 220V。
/// - Current：基本电流，默认 5A。
/// - ActiveClass：有功等级，默认 A。
/// - ActiveConstant：有功常数，默认 1000。
/// - ReactiveClass：无功等级，默认 2.0。
/// - ReactiveConstant：无功常数，默认 1000。
/// - MeterAddress：电表地址，默认空。
/// - BaudRate：电表通信波特率，默认 9600-8-E-1。
/// - UpdatedAt：配置更新时间。
/// </summary>
public static class MeterTestAccessSchema
{
    public static readonly string[] CreateTableSql =
    {
        """
        CREATE TABLE MeterTestRun
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RunId TEXT NOT NULL,
            SchemeName TEXT NOT NULL,
            StartedAt TEXT NOT NULL,
            EndedAt TEXT,
            Status TEXT NOT NULL,
            Remark TEXT
        )
        """,
        """
        CREATE TABLE MeterTestStationResult
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RunId TEXT NOT NULL,
            SchemeName TEXT NOT NULL,
            TestItemName TEXT NOT NULL,
            TestSubItemName TEXT NOT NULL,
            StationNo INTEGER NOT NULL,
            TestContent TEXT NOT NULL,
            MeterAddress TEXT NOT NULL,
            Result TEXT NOT NULL,
            ResultTime TEXT NOT NULL,
            ResultTimeText TEXT NOT NULL,
            ToolTip TEXT NOT NULL,
            Message TEXT NOT NULL,
            ResultColorArgb INTEGER NOT NULL,
            UpdatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE MeterTestStationConfig
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            StationNo INTEGER NOT NULL,
            Ip TEXT NOT NULL,
            Port INTEGER NOT NULL,
            Enabled INTEGER NOT NULL,
            UpdatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE MeterTestControlPcbConfig
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            [Name] TEXT NOT NULL,
            Ip TEXT NOT NULL,
            Port INTEGER NOT NULL,
            ProtocolVersion TEXT NOT NULL,
            StationStart INTEGER NOT NULL,
            StationEnd INTEGER NOT NULL,
            MeterAddressStart INTEGER NOT NULL,
            Enabled INTEGER NOT NULL,
            UpdatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE MeterTestMeterArchive
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            StationNo INTEGER NOT NULL,
            MeterType TEXT NOT NULL,
            AccessMode TEXT NOT NULL,
            Voltage TEXT NOT NULL,
            Current TEXT NOT NULL,
            ActiveClass TEXT NOT NULL,
            ActiveConstant TEXT NOT NULL,
            ReactiveClass TEXT NOT NULL,
            ReactiveConstant TEXT NOT NULL,
            MeterAddress TEXT NOT NULL,
            BaudRate TEXT NOT NULL DEFAULT '9600-8-E-1',
            UpdatedAt TEXT NOT NULL
        )
        """
    };

    public static readonly string[] CreateIndexSql =
    {
        "CREATE INDEX idx_MeterTestRun_RunId ON MeterTestRun (RunId)",
        "CREATE UNIQUE INDEX ux_MeterTestStationResult_Key ON MeterTestStationResult (SchemeName, TestItemName, TestSubItemName, StationNo)",
        "CREATE UNIQUE INDEX ux_MeterTestStationConfig_StationNo ON MeterTestStationConfig (StationNo)",
        "CREATE UNIQUE INDEX ux_MeterTestControlPcbConfig_Name ON MeterTestControlPcbConfig ([Name])",
        "CREATE UNIQUE INDEX ux_MeterTestMeterArchive_StationNo ON MeterTestMeterArchive (StationNo)"
    };
}
