# 国网智芯蓝牙转换器协议封装

## 帧格式

```text
7E 7E 7E 5A | LEN | FUNC | DATA... | CS | 7E A5
```

- `LEN`：从起始符到DATA末尾的字节数，即 `6 + DATA.Length`。
- `FUNC`：请求功能码；应答功能码的最高位置1。
- `CS`：从第一个 `7E` 到DATA末尾的累加和低8位。
- 多字节数据低字节在前。

## 协议代码

`SgccBluetoothConverterProtocol` 封装了：

- 复位转换器 `0x00`
- 自动/PIN扩展模式连接电表 `0x01`
- 待测表进入/退出检定模式 `0x02`
- 转换器进入/退出检定模式 `0x03`
- RS485波特率 `0x04`
- 管理单元/蓝牙模块版本 `0x05/0x06`
- 检定预处理及状态查询 `0x07/0x08`
- 帧起止符、长度、CS、应答功能码和结果码校验
- TCP粘包/拆包缓冲提取

## 地址与PIN

电表地址使用12位BCD数字。例如 `000215302589` 在报文中转换为：

```text
89 25 30 15 02 00
```

PIN扩展模式使用6位ASCII数字并逆序发送。`123456` 转换为：

```text
36 35 34 33 32 31
```

正式长度定义下，PIN扩展模式的 `LEN=0x12`。原协议示例使用 `LEN=0x0C`，
代码保留 `useLegacyPinLength=true` 选项兼容该示例行为。MeterTest当前默认使用不携带PIN的自动连接模式。

## MeterTest调用流程

```text
复位蓝牙(00)
  -> 连接电表(01，资产地址低字节在前)
  -> 检定预处理(07)
     -> 每2秒查询状态(08，最长40秒)
  -> 在蓝牙TCP通道发送698 OAD=40010200地址读取
     -> 解析地址
     -> 与资产电表地址比对
```

最后一步不发送 `F1010200` 安全模式读取。它复用通信测试中的
`SGCCTools.BuildMeterAddressReadRequest()` 和 `ParseBroadcastAddressResponse()`，仅将实际发送通道切换为当前工位新建的蓝牙专用TCP连接。

蓝牙端点只从 `MeterTestPlanConfig.xml` 的 `BluetoothTcpChannels` 获取：

```xml
<BluetoothTcpChannels>
  <BluetoothTcpChannel station="1" enabled="true" ip="192.168.127.131" port="5001" />
</BluetoothTcpChannels>
```

该映射与资产信息中的485 `IP/Port` 完全独立。未配置或未启用蓝牙通道时，对应工位直接
返回配置失败，不回退使用485通道。

## 已核对示例

```text
复位：      7E7E7E5A0600DA7EA5
连表-工位1：7E7E7E5A0C01892530150200D67EA5
连表-工位2：7E7E7E5A0C01082830150200587EA5
预处理：    7E7E7E5A0607E17EA5
```
