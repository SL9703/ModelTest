# MeterTest 执行测试流程说明

本文档说明 MeterTest 点击“执行测试”后的完整逻辑，以及测试方案、工位、控制 PCB、源控制、日志和数据库之间的关系。

## 1. 总体流程

点击“执行测试”按钮后，入口是 `MeterTest.StartSelectedTestAsync()`。

执行顺序如下：

1. 判断当前是否已有测试任务正在运行，防止重复启动。
2. 检查左侧 TreeView 是否已经选中方案、测试项或测试小项。
3. 从当前选中节点解析出要执行的测试小项集合。
4. 读取当前勾选的工位，默认最多 20 个工位。
5. 生成本次运行编号 `currentRunId`。
6. 创建 `CancellationTokenSource`，用于“停止测试”按钮取消任务。
7. 禁用“执行测试”按钮，启用“停止测试”按钮。
8. 按选中的测试小项逐项执行。
9. 每个小项执行结束后，将结果写入界面、缓存、本地数据库和测试日志。
10. 全部结束、异常或取消后，恢复按钮状态。

## 2. 测试小项解析

测试方案来自 `MeterTest/config/MeterTestPlanConfig.xml`。

TreeView 三层结构为：

1. 方案：`Scheme`
2. 测试项：`TestItem`
3. 测试小项：`TestSubItem`

`GetSelectedTestContexts()` 会根据当前选中的 TreeView 节点生成 `SelectedSubItemContext`：

- 如果选中方案，则执行该方案下所有启用的小项。
- 如果选中测试项，则执行该测试项下所有启用的小项。
- 如果选中测试小项，则只执行当前小项。
- `enabled="false"` 的小项不会执行。

## 3. 执行模式

每个 `TestSubItem` 通过 `executionMode` 决定实际流程。

当前主要支持三类：

- `StationTcp`：每个工位都有独立 IP 和端口，测试报文直接发送到对应工位。
- `ControlPcbDailyTiming`：通过控制 PCB 批量控制多个表位执行日计时。
- `SerialPortServerBaudRateSync`：执行通信测试中的串口服务器波特率检查小项。

执行入口是 `ExecuteTestContextAsync()`：

1. 如果小项配置了 `sourceControlConfig`，先执行源控制。
2. 再根据执行模式进入对应测试流程。
3. 通信测试中的波特率流程按 XML 树顺序执行，源控制绑定在第一个波特率小项上，因此不会在升源之前检查波特率。

## 4. 源控制流程

如果 `TestSubItem.sourceControlConfig` 配置了源控制名称，执行测试前会调用 `TryExecuteSourceControlAsync()`。

源控制配置在 XML 的 `SourceControlConfigs` 节点内。

支持的源控制接口类型包括：

- `AnyUIOutput`
- `Adj`
- `RangeOutputUI`
- `ShutPowerSource`

执行逻辑：

1. 根据名称查找启用的源控制配置。
2. 单相/三相优先读取资产信息里的电表类型，无法识别时再回退到源控制配置。
3. 根据单相/三相、相别、电压、电流、角度、功率因数等参数组装源控制参数。
4. 调用 `XYCtr` 对应接口升源或降源。
5. 如果源控制失败，当前测试小项直接按不合格处理，不再继续下发测试报文。

## 5. StationTcp 流程

`StationTcp` 用于工位独立通信，例如通信测试里的“地址读取”。

执行入口是 `ExecuteStationSubItemAsync()`。

核心流程：

1. 获取当前勾选的工位列表。
2. 每个工位创建一个独立任务。
3. 使用 `Task.WhenAll()` 并发执行所有选中工位。
4. 每个工位调用 `SendStationRequestAsync()`。
5. 工位连接成功后生成并发送请求报文。698 地址读取会把请求模板中的 6 字节地址替换为当前工位“电表地址”列的地址，并重新计算 HCS/FCS；其他测试直接发送配置中的 requestHex。
6. 在 `timeoutMs` 内等待响应。
7. 按 `responseParser` 判断响应是否合格。
8. 将结论写入界面、数据库和日志。

### 5.1 通信测试中的串口服务器波特率同步

通信测试方案树中包含四个独立的波特率检查小项：

1. 串口服务器连接
2. 读取串口参数
3. 校验工位波特率
4. 修改不一致波特率

执行通信测试时，先执行绑定在“串口服务器连接”小项上的升源，再按树顺序执行波特率流程，最后执行“地址读取”。波特率检查不再作为升源之前的隐藏准备步骤。

波特率流程按 IP 分组连接串口服务器管理端 IP:64444。

1. 发送 FF 0C 参数读取命令，协议类型固定为电表控制协议 02。
2. 将 COM0、COM1……映射为 TCP 端口 951、952……。
3. 把读取到的端口参数与资产信息中的端口和波特率逐项比较。
4. 参数一致时只记录“无需修改”，不发送设置命令。
5. 参数不一致时发送电表协议类型 02 的解锁和 FF 0B 设置命令。
6. 设置命令使用立即生效参数 01，不发送 FF 0D 保存重启，也不重新连接。
7. 任意同步步骤失败只记录该步骤失败，仍继续执行后续步骤和地址读取。
8. 通信测试的升源步骤失败也不会阻断流程，最终仍会尝试地址读取；其他测试项仍保持升源失败即停止。

### 5.2 地址读取解析

通信测试中的地址读取使用 `Sgcc698BroadcastAddress` 解析器。

该解析器会校验：

1. 698 报文起始符 `68` 和结束符 `16`。
2. 长度域是否和实际报文长度一致。
3. 帧头校验 HCS。
4. APDU 是否为读取响应，例如 `85 01`。
5. OAD 是否为配置值，例如 `40 01 02 00`。
6. 数据类型是否为 `09`。
7. 数据长度是否为配置值，例如 `6`。
8. 帧尾校验 FCS。

解析成功后，会把数据区内的 6 字节电表地址写入“表位地址”列。

## 6. ControlPcbDailyTiming 流程

`ControlPcbDailyTiming` 用于日计时，参考 `ElectricEnergyMeterControlV1/V2` 的控制 PCB 协议。

执行入口是 `ExecuteControlPcbDailyTimingStepAsync()`，方案树中的日计时小项按三轮展示：

```text
开始日计时实验-第1次 -> 延迟等待110秒倒计时-第1次 -> 读取日计时结果-第1次
开始日计时实验-第2次 -> 延迟等待110秒倒计时-第2次 -> 读取日计时结果-第2次
开始日计时实验-第3次 -> 延迟等待110秒倒计时-第3次 -> 读取日计时结果-第3次
```

点击“日计时”测试项时，完整流程只执行一次，后续树节点用于展示已经完成的流程阶段，不会重复发送整套报文。

核心流程：

1. 根据配置找到启用的控制 PCB 组。
2. 根据当前勾选的工位匹配对应 PCB。
3. 每个 PCB 组创建一个独立任务并发执行。
4. 每个 PCB 组只负责自己配置范围内的工位。
5. 建立 TCP 连接到控制 PCB 的 IP 和端口。
6. 按工位对应的表位地址分别发送“开始日计时”报文。
7. 每条报文发送间隔使用 `packetIntervalMs`，默认 100ms。
8. 收集每个表位的开始应答，失败工位不阻断其他工位和后续轮次。
9. 对开始成功的表位执行逐秒倒计时等待。
10. 等待结束后，对成功表位分别发送“日计时结果获取”报文。
11. 解析 `AA + 时间 + 次数` 后的连续 4 字节小端 `float` 误差值。
12. 每轮先计算该工位本轮所有结果值的平均误差，再累计三轮结果。
13. 三轮均获得有效结果且最终平均误差绝对值小于 `0.5` 时判定合格，否则判定不合格。

等待时间计算：

```text
等待秒数 = dailyTimingTime * dailyTimingCount + dailyTimingCount
```

例如时间 10 秒、次数 10 次，则等待：

```text
10 * 10 + 10 = 110 秒
```

## 7. 控制 PCB 映射

控制 PCB 配置在 `MeterTest/config/MeterTestPlanConfig.xml` 的 `ControlPcbGroups` 节点内。

典型配置含义：

- `ip`：控制 PCB IP。
- `port`：控制 PCB 端口。
- `protocolVersion`：协议版本，支持 V1/V2。
- `stationStart`：起始工位。
- `stationEnd`：结束工位。
- `meterAddressStart`：起始工位对应的 PCB 表位地址。

例如：

```text
192.168.127.101:4001 控制工位 1-3
192.168.127.101:4002 控制工位 4-6
```

如果工位 4 对应表位地址 1，则工位 5 对应表位地址 2，工位 6 对应表位地址 3。

## 8. 界面结果刷新

测试过程区域有两种视图：

- 测试方案视图：显示选择、工位、测试内容、表位地址、结果、时间。
- 资产信息视图：显示工位、IP、Port、电表类型、接入方式、电压、基本电流、有功等级、有功常数、无功等级、无功常数。

点击“测试方案”：

1. 隐藏资产信息列。
2. 显示测试执行相关列。
3. 根据当前选中的测试小项恢复已保存的工位结果。

点击“资产信息”：

1. 显示资产信息列。
2. 允许修改工位 IP、端口和电表档案参数。
3. 显示“保存”和“批量修改”按钮。

执行测试时界面保持在测试方案视图，不自动切换到资产信息视图。

## 9. 数据保存

MeterTest 使用 SQLite 本地文件数据库，不依赖 Access、ACE、Jet。

数据库路径：

```text
MeterTest/data/MeterTest.db
```

主要保存内容：

- 工位测试结果。
- 工位 IP 和端口配置。
- 控制 PCB 配置。
- 电表资产档案。

结果保存后，用户切换测试项或重启程序时，可以恢复对应测试内容的结论和表位地址。

## 10. 测试日志

工位通信日志保存到：

```text
XCKJ_logs/TextLog/yy/MM/dd
```

文件命名规则：

```text
测试项名称 + 工位号.log
```

示例：

```text
通信测试工位1.log
日计时工位1.log
```

正常通信日志包括：

- 准备连接。
- 连接成功。
- 发送报文。
- 接收报文。
- 测试结论。

连接失败或无响应时，也会按相同分隔格式写入日志，方便现场排查。

## 11. 新增测试项方法

新增测试项优先修改 `MeterTest/config/MeterTestPlanConfig.xml`。

### 11.1 新增普通工位 TCP 测试

配置要点：

- `executionMode="StationTcp"`
- `requestHex` 填写下发报文。
- `timeoutMs` 填写等待时间。
- `responseParser` 选择 `HexMatch` 或 `Sgcc698BroadcastAddress`。
- `matchMode` 可选 `Exact`、`Contains`、`StartsWith`。

### 11.2 新增控制 PCB 日计时测试

配置要点：

- `executionMode="ControlPcbDailyTiming"`
- `dailyTimingTime` 填写单次日计时时间。
- `dailyTimingCount` 填写次数。
- `packetIntervalMs` 填写报文发送间隔。
- `controlPcbGroup` 可指定固定 PCB 组；为空时按工位自动匹配启用的 PCB 组。

### 11.3 新增需要升源的测试

配置要点：

- 在 `SourceControlConfigs` 中定义源控制参数。
- 在 `TestSubItem.sourceControlConfig` 中引用源控制名称。
- 测试执行前会先升源，升源失败则不会继续执行该测试小项。

## 12. 关键代码位置

- `MeterTest.cs`：界面事件、执行测试主流程、工位并发、控制 PCB 日计时流程。
- `MeterTest.Designer.cs`：WinForms 控件布局。
- `MeterTestPlanConfig.cs`：测试方案 XML 对象模型。
- `MeterTestConfigService.cs`：测试方案配置加载和默认配置生成。
- `MeterTestStationConfig.cs`：工位通信配置对象模型。
- `MeterTestStationConfigService.cs`：工位通信配置加载保存。
- `MeterTestAccessDatabaseService.cs`：SQLite 数据库初始化、查询和保存。
- `MeterTestAccessSchema.cs`：数据库表结构说明。
- `MeterTestExecutor.cs`：早期通用测试执行器，当前主界面流程主要在 `MeterTest.cs` 中执行。
