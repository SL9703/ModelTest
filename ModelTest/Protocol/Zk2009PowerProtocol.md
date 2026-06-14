# ZK2009 新装置电源端口协议分析和 WinForm 接入说明

## 1. 协议结论

这份协议的电源命令口是 TCP 通讯，目标地址为 `192.168.0.AAA:1000`，其中 `AAA` 由现场设备 IP 决定。

1000 端口的命令是 ASCII 文本命令，帧尾固定为十六进制 `0D`，也就是 C# 字符 `\r`。不要使用 `\n`，也不要在命令正文里手动拼接回车，`SHPowerProtocolClient` 会自动追加 `0x0D`。

1001 端口是长数据端口，命令以 `END` 结尾，用于 `METER:`、`LCTDT:`、`WAVDT:`、`XZZDT:` 等长数据传输。本次新增的接口类只封装 1000 电源命令端口；1001 端口后续建议单独写类，避免两种结束符混用。

## 2. 新增接口类

文件：`ModelTest/Protocol/SHPowerProtocolClient.cs`

命名空间：`ModelTest.Protocol`

核心职责：

- 连接设备 `ip:1000`
- 校验 WinForm 控件传入的参数范围
- 把方法参数格式化成协议命令
- 按 ASCII 编码发送，并只追加 `0x0D`
- 提供 `SendRawCommandAsync` 兜底发送协议未封装的低频命令

## 3. WinForm 初始化示例

```csharp
using ModelTest.Protocol;
using ModelTest.Socket_DLL.Socket_Client;

private EnhancedTcpClient _powerTcpClient = new("ZK2009_POWER");
private SHPowerProtocolClient _powerClient;

private async void btnConnectPower_Click(object sender, EventArgs e)
{
    _powerClient = new SHPowerProtocolClient(_powerTcpClient);
    bool ok = await _powerClient.ConnectAsync(txtPowerIp.Text, 1000);
    AddLog(ok ? "电源端口连接成功" : "电源端口连接失败");
}
```

注意：项目现有 `EnhancedTcpClient.SendAsync(string)` 会追加 `\n`，不适合这份协议。调用本接口类时会自动使用 `SendBytesAsync(byte[])` 发送 `ASCII + 0D`。

## 4. 控件参数清单

### 连接控件

| 控件建议 | 参数名 | 类型 | 范围/格式 | 示例 |
|---|---|---:|---|---|
| TextBox | `ipAddress` | string | 设备 IP | `192.168.0.123` |
| NumericUpDown | `port` | int | 1~65535，默认 1000 | `1000` |

调用：

```csharp
await _powerClient.ConnectAsync(txtPowerIp.Text, (int)nudPowerPort.Value);
```

### 输出开关

| 功能 | 方法 | 控件参数 |
|---|---|---|
| 全部输出/关闭 | `SetAllOutputAsync(bool isOn)` | `isOn`：按钮或 CheckBox |
| 电压总输出/关闭 | `SetVoltageOutputAsync(bool isOn)` | `isOn` |
| 电流总输出/关闭 | `SetCurrentOutputAsync(bool isOn)` | `isOn` |
| A/B/C 相电压输出/关闭 | `SetPhaseVoltageOutputAsync(ZkPhase phase, bool isOn)` | `phase`：ComboBox；`isOn` |
| A/B/C 相电流输出/关闭 | `SetPhaseCurrentOutputAsync(ZkPhase phase, bool isOn)` | `phase`：ComboBox；`isOn` |

### 幅值设置

| 功能 | 方法 | 控件参数 |
|---|---|---|
| 三相电压幅度 | `SetThreePhaseVoltageAmplitudeAsync(decimal voltageValue)` | `voltageValue`：0.001~115.000，3 位小数 |
| 单相电压幅度 | `SetPhaseVoltageAmplitudeAsync(ZkPhase phase, decimal voltageValue)` | `phase`；`voltageValue`：0.001~115.000 |
| 单相电压百分比 | `SetPhaseVoltagePercentAsync(ZkPhase phase, decimal percent)` | `phase`；`percent`：0.1% 细度 |
| 三相电流幅度 | `SetThreePhaseCurrentAmplitudeAsync(decimal currentValue, decimal maxCurrentValue)` | `currentValue`；`maxCurrentValue=过载倍数*100` |
| 单相电流幅度 | `SetPhaseCurrentAmplitudeAsync(ZkPhase phase, decimal currentValue, decimal maxCurrentValue)` | `phase`；`currentValue`；`maxCurrentValue` |
| 单相电流百分比 | `SetPhaseCurrentPercentAsync(ZkPhase phase, decimal percent)` | `phase`；`percent`：0.1% 细度 |

控件建议：`NumericUpDown.DecimalPlaces = 3`，电压幅度设置 `Minimum=0.001`、`Maximum=115.000`。

### 相位和频率

| 功能 | 方法 | 控件参数 |
|---|---|---|
| 合相电流相位 | `SetCombinedCurrentPhaseAsync(decimal angleDegree)` | `angleDegree`：0.01~359.99 |
| B/C 相电压相位 | `SetVoltagePhaseAsync(ZkPhase phase, decimal angleDegree)` | `phase` 只允许 B/C；`angleDegree` |
| A/B/C 相电流相位 | `SetCurrentPhaseAsync(ZkPhase phase, decimal angleDegree)` | `phase`；`angleDegree` |
| 频率 | `SetFrequencyAsync(decimal frequencyHz)` | `frequencyHz`：45.00~65.00 |

### 参数设置

| 功能 | 方法 | 控件参数 |
|---|---|---|
| 校验圈数 | `SetCheckCircleCountAsync(int circleCount)` | 1~99 |
| 电表常数 | `SetMeterConstantAsync(int meterPosition, decimal constant, bool isSinglePhaseBench)` | 表位 0~200；常数最多 9 位；是否单相台 |
| 参比电压 | `SetReferenceVoltageAsync(int referenceVoltage)` | 1~9999 |
| 基本/额定电流 | `SetCurrentRangeAsync(decimal basicCurrent, decimal ratedCurrent)` | 最多 6 位，可带小数 |
| 接线方式 | `SetWireTypeAsync(ZkWireType wireType)` | ComboBox 绑定 `ZkWireType` |
| 精度等级 | `SetAccuracyClassAsync(decimal accuracyClass)` | 如 0.2、0.5、1.0 |
| 走字电能 | `SetRunningEnergyAsync(int meterPosition, decimal energyKWh, bool useTpAlias=false)` | 表位 0~200；电能最多 5 位 |
| 挂表状态 | `SetMeterMountedAsync(int meterPosition, bool isMounted)` | 表位；是否挂表 |
| 脉冲选择 | `SetPulseSourceAsync(ZkPulseSource pulseSource)` | 光电头/电子脉冲 |
| 电流接入方式 | `SetCurrentAccessModeAsync(ZkCurrentAccessMode mode)` | 直接/互感器/止逆器 |
| 脉冲输出组合 | `SetPulseMergeLineCountAsync(int lineCount)` | 2、3、4 路 |

表位规则：`meterPosition=0` 表示广播，发送时会格式化为 `000`；`1~200` 表示指定表位。

### 状态和试验

| 功能 | 方法 | 控件参数 |
|---|---|---|
| 实际值/百分比/正负序/正负功率 | `SetStatusModeAsync(ZkStatusMode mode)` | 按钮组或 ComboBox |
| 功能试验 | `ExecuteTestCommandAsync(ZkTestCommand testCommand)` | 按钮或 ComboBox |

`ZkTestCommand` 已封装常用命令：前沿对斑、结束对斑、启动试验、潜动试验、进入走字、开始走字、失压、计电能、盘转、常数测试、脉冲波形、接地故障、退出其他试验。

### 谐波

| 功能 | 方法 | 控件参数 |
|---|---|---|
| 设置谐波点 | `SetHarmonicPointAsync(ZkHarmonicTarget target, decimal phaseDegree, decimal amplitudePercent, int order)` | 目标；相位 0~359.9；幅度 0.1~600.0%；次数 2~21 |
| 计算谐波输出相别 | `ApplyHarmonicPhaseMaskAsync(bool ua, bool ub, bool uc, bool ia, bool ib, bool ic)` | 六个 CheckBox |
| 退出谐波 | `ExitHarmonicAsync()` | 按钮 |
| 清除谐波 | `ClearHarmonicAsync()` | 按钮，先退出后清除 |

### 查询和杂项

| 功能 | 方法 | 控件参数 |
|---|---|---|
| 版本/CHECK/心跳/重连/复位源箱等 | `ExecuteMiscCommandAsync(ZkMiscCommand miscCommand)` | ComboBox 或按钮 |
| 查询表位通信状态 | `QueryMeterCommunicationStatusAsync(int meterPosition)` | 表位 0~200 |
| 表位指示灯 | `SetMeterLampAsync(int meterPosition, ZkLampState checkLamp, ZkLampState withstandVoltageLamp)` | 表位；检验灯；耐压灯 |
| 输出脉冲参数 | `SetOutputPulseParameterAsync(int meterPosition, int channel, int periodMs, int widthMs, ZkPulseLevelMode levelMode)` | 表位；通道 0~2；周期；宽度；电平 |
| 输出脉冲运行 | `SetOutputPulseRunAsync(int meterPosition, int channel, bool isRunning, int pulseCount)` | 表位；通道 1~2；是否运行；脉冲数 |

## 5. SHUserControl 当前已实现事件

文件：`ModelTest/CustomControl/SHUserControl.cs`

`SHUserControl` 初始化时会调用 `BindProtocolButtonEvents()`，把已经接入协议的按钮从占位日志事件 `LogButtonClick` 切换为真实发送事件。下面是当前已经实现的按钮和协议映射。

### 连接区

| 按钮 | 事件方法 | 协议/方法 | 参数来源 | 说明 |
|---|---|---|---|---|
| 连接 | `btnConnect_Click` | `ConnectAsync(ipAddress, port)` | `txtPowerIp.Text`、`nudPowerPort.Value` | 连接 SH 源命令端口，默认端口 1000 |
| 断开 | `btnDisconnect_Click` | `Disconnect()` | 无 | 断开当前 TCP 连接 |

### 输出控制区

| 按钮 | 事件方法 | 发送命令 | 协议方法 | 参数来源 |
|---|---|---|---|---|
| 全部输出 | `btnAllOn_Click` | `UION(0Dh)` | `SetAllOutputAsync(true)` | 无 |
| 全部关闭 | `btnAllOff_Click` | `UIOF(0Dh)` | `SetAllOutputAsync(false)` | 无 |
| 电压输出 | `btnVoltageOn_Click` | `UON(0Dh)` | `SetVoltageOutputAsync(true)` | 无 |
| 电压关闭 | `btnVoltageOff_Click` | `UOF(0Dh)` | `SetVoltageOutputAsync(false)` | 无 |
| 电流输出 | `btnCurrentOn_Click` | `ION(0Dh)` | `SetCurrentOutputAsync(true)` | 无 |
| 电流关闭 | `btnCurrentOff_Click` | `IOF(0Dh)` | `SetCurrentOutputAsync(false)` | 无 |
| 分相电压开 | `btnPhaseVoltageOn_Click` | A=`UAON(0Dh)`，B=`UBON(0Dh)`，C=`UCON(0Dh)` | `SetPhaseVoltageOutputAsync(phase, true)` | `cmbVoltagePhase.SelectedItem` |
| 分相电压关 | `btnPhaseVoltageOff_Click` | A=`UAOF(0Dh)`，B=`UBOF(0Dh)`，C=`UCOF(0Dh)` | `SetPhaseVoltageOutputAsync(phase, false)` | `cmbVoltagePhase.SelectedItem` |
| 分相电流开 | `btnPhaseCurrentOn_Click` | A=`IAON(0Dh)`，B=`IBON(0Dh)`，C=`ICON(0Dh)` | `SetPhaseCurrentOutputAsync(phase, true)` | `cmbVoltagePhase.SelectedItem` |
| 分相电流关 | `btnPhaseCurrentOff_Click` | A=`IAOF(0Dh)`，B=`IBOF(0Dh)`，C=`ICOF(0Dh)` | `SetPhaseCurrentOutputAsync(phase, false)` | `cmbVoltagePhase.SelectedItem` |

输出控制区的分相电压和分相电流共用 `cmbVoltagePhase` 作为相别选择框，取值为 `ZkPhase.A/B/C`。点击前会检查 `_powerProtocolClient.IsConnected`，未连接时只写日志，不发送命令。

### 幅值相位区

| 按钮 | 事件方法 | 发送命令 | 协议方法 | 参数来源 | 范围 |
|---|---|---|---|---|---|
| 设置三相电压 | `btnSetThreeVoltage_Click` | `Pum:XXX.YYY(0Dh)` | `SetThreePhaseVoltageAmplitudeAsync(voltageValue)` | `nudThreeVoltage.Value` | `0.001~115.000` |

三相电压幅度会按三位小数格式化。例如 `nudThreeVoltage.Value = 100` 时，实际发送命令为 `Pum:100.000(0Dh)`。结束符仍由 `SHPowerProtocolClient` 自动追加，按钮事件中不要手动拼接 `\r`。

### 尚未接入的按钮

未在 `BindProtocolButtonEvents()` 中显式解除 `LogButtonClick` 的按钮仍为占位日志事件。点击这些按钮只会输出“请在该事件中接入 SHPowerProtocolClient”的提示，不会发送协议命令。

## 6. ComboBox 绑定建议

```csharp
cmbPhase.DataSource = Enum.GetValues(typeof(ZkPhase));
cmbWireType.DataSource = Enum.GetValues(typeof(ZkWireType));
cmbPulseSource.DataSource = Enum.GetValues(typeof(ZkPulseSource));
cmbCurrentAccessMode.DataSource = Enum.GetValues(typeof(ZkCurrentAccessMode));
cmbTestCommand.DataSource = Enum.GetValues(typeof(ZkTestCommand));
```

读取：

```csharp
var phase = (ZkPhase)cmbPhase.SelectedItem;
await _powerClient.SetPhaseVoltageOutputAsync(phase, true);
```

## 7. 未封装命令的发送方式

协议中低频命令很多，例如 `SETIP`、`SETGW`、`MBAUD`、`CLKFRQ`、`YXCTL`。如果界面暂时只需要直接发送，可用：

```csharp
await _powerClient.SendRawCommandAsync("CHECK");
await _powerClient.SendRawCommandAsync("SETWIP:192.168.0.123");
await _powerClient.SendRawCommandAsync("MBAUD:000,1200,8,1,E");
```

不要写成 `"CHECK\r"`，接口类会自动追加结束符。

## 8. 建议的界面分组

- 连接区：IP、端口、连接、断开、心跳
- 输出控制区：全部、电压、电流、分相输出
- 幅值相位区：电压、电流、相位、频率
- 参数区：表位、常数、接线方式、精度、电流量程、走字电能
- 试验区：启动、潜动、走字、失压、盘转、常数、退出
- 谐波区：谐波点、相别勾选、应用、退出、清除
- 查询区：版本、综合查询、表位通信状态、源 IP/MAC 查询

这样控件传参能和接口方法一一对应，后续维护时不用在按钮事件里拼协议字符串。
