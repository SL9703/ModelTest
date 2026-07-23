using System.Collections.Concurrent;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 工位勾选状态对应的控制 PCB 上下电服务。
///
/// 协议与 ElectricEnergyMeterControlV2 保持一致：
/// 1. 电压控制命令为 0x01，电流控制命令为 0x02；
/// 2. 单相 A 相启用/停用数据项为 0x01/0x05；
/// 3. 三相 ABC 启用/停用数据项为 0x04/0x08；
/// 4. 协议类型固定为 0x02（电表控制协议）。
/// </summary>
public sealed class MeterTestStationPowerService
{
    private const byte FrameStart1 = 0x55;
    private const byte FrameStart2 = 0x44;
    private const byte FrameStop1 = 0xAA;
    private const byte FrameStop2 = 0xBB;
    private const byte DirectionPcToMcu = 0x00;
    private const byte MeterControlProtocol = 0x02;
    private const byte AcVoltageCommand = 0x01;
    private const byte AcCurrentCommand = 0x02;
    private const byte SinglePhaseEnableDataItem = 0x01;
    private const byte SinglePhaseDisableDataItem = 0x05;
    private const byte ThreePhaseEnableDataItem = 0x04;
    private const byte ThreePhaseDisableDataItem = 0x08;
    private static readonly TimeSpan PacketInterval = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// 同一个控制 PCB 连接内的报文必须保持顺序，避免多个工位快速勾选时发生报文穿插。
    /// 不同 IP/Port 的控制 PCB 可以并行操作。
    /// </summary>
    private readonly ConcurrentDictionary<string, SemaphoreSlim> endpointLocks =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 根据工位号、电表类型和勾选状态执行上电或下电。
    /// </summary>
    public async Task<MeterTestStationPowerResult> SetStationPowerAsync(
        MeterTestPlanConfig planConfig,
        MeterTestControlPcbConnectionManager connectionManager,
        int stationNo,
        bool isThreePhase,
        bool powerOn,
        CancellationToken cancellationToken)
    {
        MeterTestControlPcbGroup? group = planConfig.ControlPcbGroups
            .Where(item => item.Enabled)
            .FirstOrDefault(item => stationNo >= item.StationStart && stationNo <= item.StationEnd);
        if (group is null)
        {
            string message = $"工位{stationNo}未匹配到启用的 ControlPcbGroup。";
            LogMessage.Debug($"[工位电源] {message}");
            return MeterTestStationPowerResult.Fail(message);
        }

        if (!group.ProtocolVersion.Equals(MeterControlPcbProtocolVersion.V2.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            string message = $"工位{stationNo}匹配的{group.Name}不是V2协议，当前工位勾选电源控制只支持V2。";
            LogMessage.Debug($"[工位电源] {message}");
            return MeterTestStationPowerResult.Fail(message);
        }

        // 本功能按界面工位号直接作为控制 PCB 表位地址，例如工位10发送地址0x0A。
        // ControlPcbGroup 只负责确定该工位对应的控制 PCB IP 和 Port。
        int meterAddressValue = stationNo;
        if (meterAddressValue is < 1 or > 254)
        {
            string message = $"工位{stationNo}计算出的表位地址{meterAddressValue}超出1-254。";
            LogMessage.Debug($"[工位电源] {message}");
            return MeterTestStationPowerResult.Fail(message);
        }

        if (string.IsNullOrWhiteSpace(group.Ip) || group.Port is < 1 or > 65535)
        {
            string message = $"工位{stationNo}匹配的{group.Name} IP或Port配置无效。";
            LogMessage.Debug($"[工位电源] {message}");
            return MeterTestStationPowerResult.Fail(message);
        }

        byte meterAddress = (byte)meterAddressValue;
        byte dataItem = isThreePhase
            ? powerOn ? ThreePhaseEnableDataItem : ThreePhaseDisableDataItem
            : powerOn ? SinglePhaseEnableDataItem : SinglePhaseDisableDataItem;
        byte[] voltagePacket = BuildV2MeterPacket(meterAddress, AcVoltageCommand, dataItem);
        byte[] currentPacket = BuildV2MeterPacket(meterAddress, AcCurrentCommand, dataItem);
        (string Name, byte[] Packet)[] operations = powerOn
            ? new[] { ("上电压", voltagePacket), ("通电流", currentPacket) }
            : new[] { ("断电流", currentPacket), ("下电压", voltagePacket) };

        string endpoint = $"{group.Ip.Trim()}:{group.Port}";
        SemaphoreSlim endpointLock = endpointLocks.GetOrAdd(endpoint, _ => new SemaphoreSlim(1, 1));
        await endpointLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string phaseText = isThreePhase ? "ABC三相" : "A相";
            LogMessage.Debug(
                $"[工位电源] 准备{(powerOn ? "上电" : "下电")}：工位={stationNo}，表位地址=0x{meterAddress:X2}，"
                + $"电表类型={(isThreePhase ? "三相" : "单相")}，相位={phaseText}，控制PCB={group.Name}，Endpoint={endpoint}");

            if (!connectionManager.TryGetConnectedConnection(
                    group,
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                return MeterTestStationPowerResult.Fail(connectionError, meterAddress);
            }

            LogMessage.Debug($"[工位电源] 复用控制PCB长连接：工位={stationNo}，Endpoint={endpoint}");
            await connection.SendSequenceAsync(
                operations.Select(operation => operation.Packet).ToArray(),
                PacketInterval,
                (index, packet) => LogMessage.Debug(
                    $"[工位电源] 工位{stationNo} {operations[index].Name}报文[PC-->MCU]：{ToHexString(packet)}"),
                cancellationToken);

            string successMessage =
                $"工位{stationNo}{(powerOn ? "上电压并通电流" : "断电流并下电压")}完成，表位地址=0x{meterAddress:X2}。";
            LogMessage.Debug($"[工位电源] {successMessage}");
            return MeterTestStationPowerResult.Ok(successMessage, meterAddress);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            string message = $"工位{stationNo}连接控制PCB超时：{endpoint}。";
            LogMessage.Debug($"[工位电源] {message}");
            return MeterTestStationPowerResult.Fail(message, meterAddress);
        }
        catch (OperationCanceledException)
        {
            LogMessage.Debug($"[工位电源] 工位{stationNo}电源操作已取消。");
            throw;
        }
        catch (Exception ex)
        {
            string message = $"工位{stationNo}电源操作失败：{ex.Message}";
            LogMessage.Debug($"[工位电源] {message}");
            return MeterTestStationPowerResult.Fail(message, meterAddress);
        }
        finally
        {
            endpointLock.Release();
        }
    }

    /// <summary>
    /// 按 ElectricEnergyMeterControlV2 格式构造电表控制协议报文。
    /// </summary>
    private static byte[] BuildV2MeterPacket(byte meterAddress, byte command, byte dataItem)
    {
        const int dataLength = 8;
        byte[] packet = new byte[12];
        packet[0] = FrameStart1;
        packet[1] = FrameStart2;
        packet[2] = dataLength;
        packet[3] = 0x00;
        packet[4] = DirectionPcToMcu;
        packet[5] = meterAddress;
        packet[6] = MeterControlProtocol;
        packet[7] = command;
        packet[8] = dataItem;
        packet[9] = CalculateChecksum(packet, 2, dataLength - 1);
        packet[10] = FrameStop1;
        packet[11] = FrameStop2;
        return packet;
    }

    /// <summary>计算从长度字段开始到数据项结束的累加和低字节。</summary>
    private static byte CalculateChecksum(byte[] data, int startIndex, int count)
    {
        int checksum = 0;
        for (int index = startIndex; index < startIndex + count; index++)
        {
            checksum += data[index];
        }

        return (byte)checksum;
    }

    private static string ToHexString(byte[] data)
    {
        return BitConverter.ToString(data).Replace("-", " ");
    }
}

/// <summary>单个工位上电或下电操作结果。</summary>
public sealed record MeterTestStationPowerResult(bool Success, string Message, byte MeterAddress)
{
    public static MeterTestStationPowerResult Ok(string message, byte meterAddress)
        => new(true, message, meterAddress);

    public static MeterTestStationPowerResult Fail(string message, byte meterAddress = 0x00)
        => new(false, message, meterAddress);
}
