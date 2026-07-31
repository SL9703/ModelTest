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

用途：保存 48 个工位的通信配置，后续可替代 XML/界面临时配置。

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
| CurrentSpecification | TEXT | 电流规格；界面候选项从 MeterTestAssetOption 获取 |
| ActiveClass | TEXT | 有功等级，默认 A |
| ActiveConstant | TEXT | 有功常数，默认 1000 |
| ReactiveClass | TEXT | 无功等级，默认 2.0 |
| ReactiveConstant | TEXT | 无功常数，默认 1000 |
| Barcode | TEXT | 资产条形码 |
| MeterAddress | TEXT | 电表地址，默认空 |
| BaudRate | TEXT | 电表通信波特率，默认 9600-8-E-1 |
| UpdatedAt | TEXT | 配置更新时间 |

唯一索引：`StationNo`

## MeterTestAssetBarcodeSetting

用途：保存条形码生成电表地址的规则。规则1使用单个起止区间；规则2拼接两个用户可配置片段。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY | 固定为 1 的单行配置 |
| BarcodeStartIndex | INTEGER | 规则1的0基起始位置 |
| BarcodeEndIndex | INTEGER | 规则1的0基结束位置，包含结束位置 |
| RuleType | TEXT | Rule1Range 或 Rule2Composite |
| Rule2FirstStart | INTEGER | 规则2第一段0基起始位置 |
| Rule2FirstLength | INTEGER | 规则2第一段长度 |
| Rule2SecondStart | INTEGER | 规则2第二段0基起始位置 |
| Rule2SecondLength | INTEGER | 规则2第二段长度 |
| UpdatedAt | TEXT | 配置更新时间 |

## MeterTestAssetOption

用途：保存资产信息下拉字段的候选值、显示顺序和默认项。程序启动后从该表加载候选值，Designer不保存业务候选数组。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | 自增主键 |
| Category | TEXT | 字段类别，例如 MeterType、CurrentSpecification |
| Scope | TEXT | 作用域，例如 Direct 或 Transformer |
| Value | TEXT | 下拉候选值 |
| SortOrder | INTEGER | 显示顺序 |
| IsDefault | INTEGER | 是否为默认值 |
| Enabled | INTEGER | 是否启用 |

唯一约束：`Category + Scope + Value`

直接式电流规格初始数据包含：`0.25-0.5(60)A`、`0.25-0.5(100)A`、`0.2-0.5(60)A`、`0.2-0.5(100)A`、`0.5-1(60)A`、`0.5-1(100)A`。

## MeterTestPowerFactorAngle

用途：保存有功基本误差测试中“功率方向 + 功率因数”对应的 `AnyUIOutput` FA角度。基本误差执行时使用 `Direction + PowerFactor` 从该表查询，不再从代码硬编码角度。

| 字段 | 类型 | 说明 |
| --- | --- | --- |
| Id | INTEGER PRIMARY KEY AUTOINCREMENT | 自增主键 |
| Direction | TEXT | `ForwardActive` 正向有功或 `ReverseActive` 反向有功 |
| PowerFactor | TEXT | 功率因数，例如 `1.0`、`0.5L`、`0.8C` |
| LoadType | TEXT | 纯阻性、感性或容性 |
| CurrentAngle | REAL | `IAJ/IBJ/ICJ` 使用的有符号电压电流夹角，范围 `-180°~180°` |
| Description | TEXT | 现场配置说明 |
| Enabled | INTEGER | 是否启用，1为启用，0为停用 |
| UpdatedAt | TEXT | 数据库更新时间 |

唯一约束：`Direction + PowerFactor`。程序只会补充缺失的默认组合，不会覆盖现场已修改的角度。

例如测试点 `正有-H-0.5L-1U-10Itr` 使用查询键 `ForwardActive + 0.5L`，默认读取到 `CurrentAngle=60`。

## MeterTestResultTask / MeterTestResultStation / MeterTestResultDetail

用途：保存用户确认后的历史测试任务、任务工位资产快照和测试明细。清理方案运行态时只删除 `MeterTestStationResult`，不会删除这三张历史表的数据。
