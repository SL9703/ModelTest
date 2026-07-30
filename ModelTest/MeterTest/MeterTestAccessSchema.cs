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
/// 表：MeterTestRuntimeMeasurement
/// 用途：保存分别执行各 TestItem 时产生的最新结构化测量值，防止下一次点击执行后内存数据丢失。
/// 唯一键：SchemeName + StationNo + TestItemName + TestSubItemName + MeasurementName + SequenceNo。
/// RunId记录最后一次产生该测量值的运行编号；NumericValue/AverageValue保存原值和平均值。
///
/// 表：MeterTestStationConfig
/// 用途：保存 48 个工位的通信配置，后续可替代 XML/界面临时配置。
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
/// 表：MeterTestAssetBarcodeSetting
/// 用途：保存资产扫码后生成电表地址的当前规则，规则1使用起止位置，规则2使用两个可配置组合片段。
/// 字段：
/// - Id：固定为1的单行主键。
/// - BarcodeStartIndex / BarcodeEndIndex：规则1使用的0基起止位置，结束位置包含在截取范围内。
/// - RuleType：Rule1Range 或 Rule2Composite。
/// - Rule2FirstStart / Rule2FirstLength：规则2第一个片段的0基起始位置和截取长度。
/// - Rule2SecondStart / Rule2SecondLength：规则2第二个片段的0基起始位置和截取长度。
/// - UpdatedAt：配置更新时间。
///
/// 表：MeterTestAssetOption
/// 用途：保存资产信息各下拉字段的候选值和默认值，界面不再维护硬编码候选数组。
/// 字段：Category为字段类别；Scope用于区分直接式/互感式等作用域；Value为候选值；
/// SortOrder为显示顺序；IsDefault标记默认项；Enabled控制是否在界面中提供。
///
/// 表：MeterTestPowerFactorAngle
/// 用途：保存有功基本误差测试中“功率方向+功率因数”对应的 AnyUIOutput FA 角度。
/// 字段：Direction为ForwardActive/ReverseActive；PowerFactor为1.0/0.5L等；
/// LoadType为纯阻性/感性/容性；CurrentAngle为IAJ/IBJ/ICJ使用的[-180,180]有符号夹角；
/// Description为现场配置说明；Enabled控制该组合是否可用。
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
/// - CurrentSpecification：电流规格，例如 0.25-0.5(60)A，分别对应 Imin/Itr/Imax。
/// - ActiveClass：有功等级，默认 A。
/// - ActiveConstant：有功常数，默认 1000。
/// - ReactiveClass：无功等级，默认 2.0。
/// - ReactiveConstant：无功常数，默认 1000。
/// - Barcode：资产条形码。
/// - MeterAddress：电表地址，默认空。
/// - BaudRate：电表通信波特率，默认 9600-8-E-1。
/// - UpdatedAt：配置更新时间。
///
/// 表：MeterTestResultTask
/// 用途：保存一次自动或手动保存的测试任务，是测试结果查询页面最上层对象。
/// 字段：RunId为运行唯一值；Status为运行状态；SaveMode区分自动保存和手动保存；ResultSummary为汇总文本。
///
/// 表：MeterTestResultStation
/// 用途：冻结任务保存时各工位的电表档案及工位总结论，避免后续修改资产影响历史结果。
/// 字段：TaskId关联任务；StationNo为工位；CurrentSpecification保存当时使用的Imin/Itr/Imax规格；OverallResult为工位总结论。
///
/// 表：MeterTestResultDetail
/// 用途：保存任务下每个工位、测试项、测试小项的结论和结构化测量值。
/// 字段：MeasurementName区分起动误差、潜动脉冲、基本误差和日计时误差；SequenceNo表示轮次；
/// NumericValue保存可计算原值；AverageValue保存平均值；LimitText保存判定标准；Message保存过程说明。
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
        CREATE TABLE MeterTestRuntimeMeasurement
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RunId TEXT NOT NULL,
            SchemeName TEXT NOT NULL,
            StationNo INTEGER NOT NULL,
            TestItemName TEXT NOT NULL,
            TestSubItemName TEXT NOT NULL,
            MeasurementName TEXT NOT NULL,
            SequenceNo INTEGER NOT NULL,
            NumericValue REAL NOT NULL,
            ValueText TEXT NOT NULL,
            Unit TEXT NOT NULL,
            AverageValue REAL,
            LimitText TEXT NOT NULL,
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
        CREATE TABLE MeterTestAssetBarcodeSetting
        (
            Id INTEGER PRIMARY KEY,
            BarcodeStartIndex INTEGER NOT NULL,
            BarcodeEndIndex INTEGER NOT NULL,
            RuleType TEXT NOT NULL DEFAULT 'Rule1Range',
            Rule2FirstStart INTEGER NOT NULL DEFAULT 6,
            Rule2FirstLength INTEGER NOT NULL DEFAULT 2,
            Rule2SecondStart INTEGER NOT NULL DEFAULT 10,
            Rule2SecondLength INTEGER NOT NULL DEFAULT 10,
            UpdatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE MeterTestAssetOption
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Category TEXT NOT NULL,
            Scope TEXT NOT NULL DEFAULT '',
            Value TEXT NOT NULL,
            SortOrder INTEGER NOT NULL,
            IsDefault INTEGER NOT NULL DEFAULT 0,
            Enabled INTEGER NOT NULL DEFAULT 1,
            UNIQUE (Category, Scope, Value)
        )
        """,
        """
        CREATE TABLE MeterTestPowerFactorAngle
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Direction TEXT NOT NULL,
            PowerFactor TEXT NOT NULL,
            LoadType TEXT NOT NULL,
            CurrentAngle REAL NOT NULL,
            Description TEXT NOT NULL DEFAULT '',
            Enabled INTEGER NOT NULL DEFAULT 1,
            UpdatedAt TEXT NOT NULL,
            UNIQUE (Direction, PowerFactor)
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
            CurrentSpecification TEXT NOT NULL DEFAULT '0.25-0.5(60)A',
            ActiveClass TEXT NOT NULL,
            ActiveConstant TEXT NOT NULL,
            ReactiveClass TEXT NOT NULL,
            ReactiveConstant TEXT NOT NULL,
            Barcode TEXT NOT NULL DEFAULT '',
            MeterAddress TEXT NOT NULL,
            BaudRate TEXT NOT NULL DEFAULT '9600-8-E-1',
            UpdatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE MeterTestResultTask
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RunId TEXT NOT NULL,
            SchemeName TEXT NOT NULL,
            StartedAt TEXT NOT NULL,
            EndedAt TEXT NOT NULL,
            Status TEXT NOT NULL,
            SaveMode TEXT NOT NULL,
            StationCount INTEGER NOT NULL,
            ResultSummary TEXT NOT NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        )
        """,
        """
        CREATE TABLE MeterTestResultStation
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TaskId INTEGER NOT NULL,
            StationNo INTEGER NOT NULL,
            Barcode TEXT NOT NULL,
            MeterAddress TEXT NOT NULL,
            MeterType TEXT NOT NULL,
            AccessMode TEXT NOT NULL,
            Voltage TEXT NOT NULL,
            BasicCurrent TEXT NOT NULL,
            CurrentSpecification TEXT NOT NULL,
            ActiveClass TEXT NOT NULL,
            ActiveConstant TEXT NOT NULL,
            ReactiveClass TEXT NOT NULL,
            ReactiveConstant TEXT NOT NULL,
            OverallResult TEXT NOT NULL,
            CompletedAt TEXT NOT NULL,
            FOREIGN KEY (TaskId) REFERENCES MeterTestResultTask(Id) ON DELETE CASCADE
        )
        """,
        """
        CREATE TABLE MeterTestResultDetail
        (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TaskId INTEGER NOT NULL,
            StationNo INTEGER NOT NULL,
            TestItemName TEXT NOT NULL,
            TestSubItemName TEXT NOT NULL,
            Result TEXT NOT NULL,
            ResultTimeText TEXT NOT NULL,
            Message TEXT NOT NULL,
            MeasurementName TEXT NOT NULL,
            SequenceNo INTEGER NOT NULL,
            ValueText TEXT NOT NULL,
            NumericValue REAL,
            Unit TEXT NOT NULL,
            AverageValue REAL,
            LimitText TEXT NOT NULL,
            FOREIGN KEY (TaskId) REFERENCES MeterTestResultTask(Id) ON DELETE CASCADE
        )
        """
    };

    public static readonly string[] CreateIndexSql =
    {
        "CREATE INDEX idx_MeterTestRun_RunId ON MeterTestRun (RunId)",
        "CREATE UNIQUE INDEX ux_MeterTestStationResult_Key ON MeterTestStationResult (SchemeName, TestItemName, TestSubItemName, StationNo)",
        "CREATE UNIQUE INDEX ux_MeterTestRuntimeMeasurement_Key ON MeterTestRuntimeMeasurement (SchemeName, StationNo, TestItemName, TestSubItemName, MeasurementName, SequenceNo)",
        "CREATE UNIQUE INDEX ux_MeterTestStationConfig_StationNo ON MeterTestStationConfig (StationNo)",
        "CREATE UNIQUE INDEX ux_MeterTestControlPcbConfig_Name ON MeterTestControlPcbConfig ([Name])",
        "CREATE UNIQUE INDEX ux_MeterTestAssetOption_Key ON MeterTestAssetOption (Category, Scope, Value)",
        "CREATE UNIQUE INDEX ux_MeterTestPowerFactorAngle_Key ON MeterTestPowerFactorAngle (Direction, PowerFactor)",
        "CREATE UNIQUE INDEX ux_MeterTestMeterArchive_StationNo ON MeterTestMeterArchive (StationNo)",
        "CREATE UNIQUE INDEX ux_MeterTestResultTask_RunId ON MeterTestResultTask (RunId)",
        "CREATE UNIQUE INDEX ux_MeterTestResultStation_TaskStation ON MeterTestResultStation (TaskId, StationNo)",
        "CREATE INDEX idx_MeterTestResultDetail_TaskStation ON MeterTestResultDetail (TaskId, StationNo, TestItemName, TestSubItemName)",
        "CREATE UNIQUE INDEX ux_MeterTestResultDetail_Key ON MeterTestResultDetail (TaskId, StationNo, TestItemName, TestSubItemName, MeasurementName, SequenceNo)"
    };
}
