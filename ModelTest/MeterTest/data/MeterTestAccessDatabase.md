# MeterTest 本地数据库结构

数据库类型：

SQLite 本地文件数据库

数据库文件默认路径：

`MeterTest/data/MeterTest.db`

程序启动时会自动创建目录、数据库文件和表结构，不依赖 Access、ACE、Jet 或其他 OLE DB 环境。

## MeterTestRun

用途：记录一次完整测试运行的生命周期。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | 自增主键 |
| RunId | TEXT(64) | 一次测试运行的唯一编号 |
| SchemeName | TEXT(255) | 方案名称 |
| StartedAt | DATETIME | 测试开始时间 |
| EndedAt | DATETIME | 测试结束时间 |
| Status | TEXT(50) | 运行状态，例如 Running / Completed / Failed / Cancelled |
| Remark | TEXT | 运行备注或异常信息 |

## MeterTestStationResult

用途：保存每个测试小项、每个工位的最新测试结论，用于界面切换和程序重启后恢复。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | 自增主键 |
| RunId | TEXT(64) | 来源测试运行编号 |
| SchemeName | TEXT(255) | 方案名称 |
| TestItemName | TEXT(255) | 测试项名称，例如 通信测试 / 日计时 |
| TestSubItemName | TEXT(255) | 测试小项名称，例如 地址读取 / 日计时 |
| StationNo | LONG | 工位号 |
| TestContent | TEXT(255) | 界面“测试内容”列展示文本 |
| MeterAddress | TEXT(64) | 解析出的表位地址；日计时等项目可为空 |
| Result | TEXT(50) | 测试结论，例如 测试中 / 合格 / 不合格 / 待测试 |
| ResultTime | DATETIME | 结果更新时间 |
| ResultTimeText | TEXT(32) | 界面展示用时间文本，格式 HH:mm:ss |
| ToolTip | TEXT | 鼠标悬停提示内容 |
| Message | TEXT | 结论说明或异常信息 |
| ResultColorArgb | INTEGER | 界面结果颜色 ARGB 整数 |
| UpdatedAt | TEXT | 数据库更新时间 |

唯一索引：`SchemeName + TestItemName + TestSubItemName + StationNo`

## MeterTestStationConfig

用途：保存 20 个工位的通信配置，后续可替代 XML/界面临时配置。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | 自增主键 |
| StationNo | INTEGER | 工位号 |
| Ip | TEXT | 工位串口服务器或通信服务 IP |
| Port | INTEGER | 工位串口服务器或通信服务端口 |
| Enabled | INTEGER | 是否启用该工位，1 表示启用，0 表示禁用 |
| UpdatedAt | TEXT | 配置更新时间 |

唯一索引：`StationNo`

## MeterTestControlPcbConfig

用途：保存控制 PCB 与工位/表位的映射关系，后续可替代 XML 的 `ControlPcbGroups`。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | 自增主键 |
| Name | TEXT | 控制 PCB 名称 |
| Ip | TEXT | 控制 PCB IP |
| Port | INTEGER | 控制 PCB 端口 |
| ProtocolVersion | TEXT | 控制 PCB 协议版本，V1 或 V2 |
| StationStart | INTEGER | 该 PCB 控制的起始工位 |
| StationEnd | INTEGER | 该 PCB 控制的结束工位 |
| MeterAddressStart | INTEGER | StationStart 对应的 PCB 表位地址 |
| Enabled | INTEGER | 是否启用该 PCB，1 表示启用，0 表示禁用 |
| UpdatedAt | TEXT | 配置更新时间 |

唯一索引：`Name`

## MeterTestMeterArchive

用途：保存每个工位的电表档案信息，并回填到 MeterTest 测试过程区域。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | 自增主键 |
| StationNo | INTEGER | 工位号 |
| MeterType | TEXT | 电表类型，单相或三相，默认单相 |
| AccessMode | TEXT | 接入方式，直接式或互感式，默认直接式 |
| Voltage | TEXT | 电表电压，默认 220V |
| Current | TEXT | 基本电流，默认 5A |
| ActiveClass | TEXT | 有功等级，默认 A |
| ActiveConstant | TEXT | 有功常数，默认 1000 |
| ReactiveClass | TEXT | 无功等级，默认 2.0 |
| ReactiveConstant | TEXT | 无功常数，默认 1000 |
| MeterAddress | TEXT | 电表地址，默认空 |
| UpdatedAt | TEXT | 配置更新时间 |

唯一索引：`StationNo`
