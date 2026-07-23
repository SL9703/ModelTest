using System.Globalization;
using System.Net.Sockets;
using ModelTest.Protocol;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 国网智芯蓝牙接口检测服务。
/// 每个工位使用方案配置中的蓝牙专用 IP/Port 新建 TcpClient，
/// 不复用资产信息中的485端点、StationTcp、控制PCB或其它连接池。
/// </summary>
public sealed class MeterTestBluetoothInterfaceService
{
    private static readonly TimeSpan PreprocessPollInterval = TimeSpan.FromSeconds(2);

    /// <summary>并发执行当前蓝牙小项，一个工位失败不中断其它工位。</summary>
    public async Task<IReadOnlyDictionary<int, MeterTestBluetoothStationResult>> ExecuteStepAsync(
        MeterTestSubItem subItem,
        IReadOnlyList<MeterTestBluetoothStation> stations,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        Task<MeterTestBluetoothStationResult>[] tasks = stations
            .Select(station => ExecuteStationStepSafelyAsync(subItem, station, stationLogger, cancellationToken))
            .ToArray();
        MeterTestBluetoothStationResult[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.ToDictionary(result => result.StationNo);
    }

    private static async Task<MeterTestBluetoothStationResult> ExecuteStationStepSafelyAsync(
        MeterTestSubItem subItem,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecuteStationStepAsync(
                subItem,
                station,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            string message = $"蓝牙步骤{subItem.Name}等待超时，超时={subItem.TimeoutMs}ms。";
            Trace(station.StationNo, message, stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, message);
        }
        catch (Exception ex)
        {
            string message = $"蓝牙步骤{subItem.Name}执行异常：{ex.Message}";
            Trace(station.StationNo, message, stationLogger);
            LogMessage.Error($"[蓝牙接口][工位{station.StationNo}] {message}", ex);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, message);
        }
    }

    private static async Task<MeterTestBluetoothStationResult> ExecuteStationStepAsync(
        MeterTestSubItem subItem,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(station.ConfigurationError))
        {
            Trace(station.StationNo, station.ConfigurationError, stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, station.ConfigurationError);
        }

        if (string.IsNullOrWhiteSpace(station.Ip) || station.Port is < 1 or > 65535)
            return MeterTestBluetoothStationResult.Fail(station.StationNo, $"蓝牙专用TCP端点无效：{station.Ip}:{station.Port}。");

        if (!Enum.TryParse(subItem.BluetoothStep, true, out MeterTestBluetoothStep step))
            return MeterTestBluetoothStationResult.Fail(station.StationNo, $"蓝牙流程步骤不支持：{subItem.BluetoothStep}。");

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Math.Max(100, subItem.TimeoutMs));
        Trace(
            station.StationNo,
            $"准备建立工位专用蓝牙TCP连接：{station.Ip}:{station.Port}，步骤={subItem.Name}。",
            stationLogger);

        using TcpClient client = new();
        await client.ConnectAsync(station.Ip.Trim(), station.Port, timeoutCts.Token).ConfigureAwait(false);
        Trace(station.StationNo, $"蓝牙TCP连接成功：{station.Ip}:{station.Port}。", stationLogger);
        await using NetworkStream stream = client.GetStream();

        MeterTestBluetoothStationResult result = step switch
        {
            MeterTestBluetoothStep.Reset => await ExecuteSimpleCommandAsync(
                stream,
                station,
                subItem.Name,
                BluetoothConverterFunction.Reset,
                SgccBluetoothConverterProtocol.BuildResetFrame(),
                stationLogger,
                timeoutCts.Token).ConfigureAwait(false),
            MeterTestBluetoothStep.ConnectMeter => await ExecuteConnectMeterAsync(
                stream,
                station,
                stationLogger,
                timeoutCts.Token).ConfigureAwait(false),
            MeterTestBluetoothStep.Preprocess => await ExecutePreprocessAsync(
                stream,
                station,
                stationLogger,
                timeoutCts.Token).ConfigureAwait(false),
            MeterTestBluetoothStep.ReadAddress => await ExecuteReadAddressAsync(
                stream,
                station,
                subItem,
                stationLogger,
                timeoutCts.Token).ConfigureAwait(false),
            _ => MeterTestBluetoothStationResult.Fail(station.StationNo, $"蓝牙流程步骤未实现：{step}。")
        };

        Trace(station.StationNo, $"关闭本次工位专用蓝牙TCP连接：{station.Ip}:{station.Port}。", stationLogger);
        return result;
    }

    private static async Task<MeterTestBluetoothStationResult> ExecuteConnectMeterAsync(
        NetworkStream stream,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMeterAddress(station.MeterAddress, out string meterAddress))
        {
            string message = $"工位{station.StationNo}电表地址必须是12位BCD数字：{station.MeterAddress}。";
            Trace(station.StationNo, message, stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, message);
        }

        byte[] request = SgccBluetoothConverterProtocol.BuildConnectMeterFrame(meterAddress);
        return await ExecuteSimpleCommandAsync(
            stream,
            station,
            $"自动连接电表，地址={meterAddress}",
            BluetoothConverterFunction.ConnectMeter,
            request,
            stationLogger,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<MeterTestBluetoothStationResult> ExecutePreprocessAsync(
        NetworkStream stream,
        MeterTestBluetoothStation station,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        MeterTestBluetoothStationResult startResult = await ExecuteSimpleCommandAsync(
            stream,
            station,
            "启动蓝牙检定预处理",
            BluetoothConverterFunction.Preprocess,
            SgccBluetoothConverterProtocol.BuildPreprocessFrame(),
            stationLogger,
            cancellationToken).ConfigureAwait(false);
        if (!startResult.Success)
            return startResult;

        int queryIndex = 0;
        while (true)
        {
            await Task.Delay(PreprocessPollInterval, cancellationToken).ConfigureAwait(false);
            queryIndex++;
            byte[] request = SgccBluetoothConverterProtocol.BuildQueryPreprocessStatusFrame();
            byte[] response = await SendAndReceiveBluetoothFrameAsync(
                stream,
                station.StationNo,
                $"查询蓝牙检定预处理状态-第{queryIndex}次",
                request,
                stationLogger,
                cancellationToken).ConfigureAwait(false);
            if (!SgccBluetoothConverterProtocol.TryParsePreprocessStatus(response, out BluetoothPreprocessStatus status, out string statusMessage))
            {
                Trace(station.StationNo, $"预处理状态应答解析失败：{statusMessage}", stationLogger);
                return MeterTestBluetoothStationResult.Fail(station.StationNo, statusMessage);
            }

            Trace(station.StationNo, $"第{queryIndex}次预处理状态：{statusMessage}", stationLogger);
            if (status == BluetoothPreprocessStatus.Succeeded)
                return MeterTestBluetoothStationResult.Pass(station.StationNo, statusMessage);
            if (status == BluetoothPreprocessStatus.Failed)
                return MeterTestBluetoothStationResult.Fail(station.StationNo, statusMessage);
        }
    }

    private static async Task<MeterTestBluetoothStationResult> ExecuteReadAddressAsync(
        NetworkStream stream,
        MeterTestBluetoothStation station,
        MeterTestSubItem subItem,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeMeterAddress(station.MeterAddress, out string expectedAddress))
        {
            string validationMessage = $"工位{station.StationNo}电表地址必须是12位BCD数字：{station.MeterAddress}。";
            Trace(station.StationNo, validationMessage, stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, validationMessage);
        }

        string requestHex = SGCCTools.BuildMeterAddressReadRequest(expectedAddress);
        byte[] request = ParseHex(requestHex);
        Trace(
            station.StationNo,
            $"{FormatTimestamp()} - 发送698报文：{SgccBluetoothConverterProtocol.ToHexString(request)}，OAD=40010200。",
            stationLogger);
        await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        byte[] response = await Read698FrameAsync(stream, cancellationToken).ConfigureAwait(false);
        string responseHex = SgccBluetoothConverterProtocol.ToHexString(response);
        Trace(station.StationNo, $"{FormatTimestamp()} - 接受698报文：{responseHex}", stationLogger);

        Sgcc698BroadcastAddressParseResult parseResult = SGCCTools.ParseBroadcastAddressResponse(
            responseHex,
            subItem.ExpectedOad,
            subItem.ExpectedApdu,
            subItem.ExpectedDataType,
            subItem.ExpectedDataLength);
        string actualAddress = NormalizeAddress(parseResult.MeterAddress);
        bool passed = parseResult.IsValid && expectedAddress.Equals(actualAddress, StringComparison.OrdinalIgnoreCase);
        string message = parseResult.IsValid
            ? $"实际地址={expectedAddress}，返回地址={actualAddress}，结论={(passed ? "合格" : "不合格")}。"
            : $"OAD=40010200应答解析失败：{parseResult.Message}";
        Trace(station.StationNo, message, stationLogger);
        return passed
            ? MeterTestBluetoothStationResult.Pass(station.StationNo, message, actualAddress)
            : MeterTestBluetoothStationResult.Fail(station.StationNo, message, actualAddress);
    }

    private static async Task<MeterTestBluetoothStationResult> ExecuteSimpleCommandAsync(
        NetworkStream stream,
        MeterTestBluetoothStation station,
        string description,
        BluetoothConverterFunction function,
        byte[] request,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        byte[] response = await SendAndReceiveBluetoothFrameAsync(
            stream,
            station.StationNo,
            description,
            request,
            stationLogger,
            cancellationToken).ConfigureAwait(false);
        if (!SgccBluetoothConverterProtocol.TryParseCommandResult(
                response,
                function,
                out bool success,
                out byte resultCode,
                out string message))
        {
            Trace(station.StationNo, $"{description}应答解析失败：{message}", stationLogger);
            return MeterTestBluetoothStationResult.Fail(station.StationNo, message);
        }

        string resultMessage = $"{description}：{message}结果码={resultCode:X2}。";
        Trace(station.StationNo, resultMessage, stationLogger);
        return success
            ? MeterTestBluetoothStationResult.Pass(station.StationNo, resultMessage)
            : MeterTestBluetoothStationResult.Fail(station.StationNo, resultMessage);
    }

    private static async Task<byte[]> SendAndReceiveBluetoothFrameAsync(
        NetworkStream stream,
        int stationNo,
        string description,
        byte[] request,
        Action<int, string>? stationLogger,
        CancellationToken cancellationToken)
    {
        Trace(
            stationNo,
            $"{FormatTimestamp()} - 发送报文：{SgccBluetoothConverterProtocol.ToHexString(request)}，{description}。",
            stationLogger);
        await stream.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

        List<byte> receiveBuffer = new();
        byte[] buffer = new byte[1024];
        while (true)
        {
            int length = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                throw new IOException("蓝牙TCP连接已关闭。");

            receiveBuffer.AddRange(buffer.AsSpan(0, length).ToArray());
            if (!SgccBluetoothConverterProtocol.TryTakeFrame(receiveBuffer, out byte[]? frame))
                continue;

            Trace(
                stationNo,
                $"{FormatTimestamp()} - 接受报文：{SgccBluetoothConverterProtocol.ToHexString(frame!)}。",
                stationLogger);
            return frame!;
        }
    }

    private static async Task<byte[]> Read698FrameAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        List<byte> receiveBuffer = new();
        byte[] buffer = new byte[4096];
        while (true)
        {
            int length = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (length <= 0)
                throw new IOException("蓝牙TCP连接在698应答返回前已关闭。");

            receiveBuffer.AddRange(buffer.AsSpan(0, length).ToArray());
            int startIndex = receiveBuffer.FindIndex(value => value == 0x68);
            if (startIndex < 0)
                continue;
            if (receiveBuffer.Count < startIndex + 3)
                continue;

            int declaredLength = receiveBuffer[startIndex + 1] | (receiveBuffer[startIndex + 2] << 8);
            int totalLength = declaredLength + 2;
            if (declaredLength < 1 || receiveBuffer.Count < startIndex + totalLength)
                continue;

            // 保留正式698帧前的FE前导符，统一交给SGCCTools剔除和记录。
            int preambleStart = startIndex;
            while (preambleStart > 0 && receiveBuffer[preambleStart - 1] == 0xFE)
                preambleStart--;
            int frameLength = startIndex + totalLength - preambleStart;
            return receiveBuffer.Skip(preambleStart).Take(frameLength).ToArray();
        }
    }

    private static byte[] ParseHex(string value)
    {
        string normalized = new(value.Where(Uri.IsHexDigit).ToArray());
        if (normalized.Length == 0 || normalized.Length % 2 != 0)
            throw new FormatException("698请求报文不是合法HEX。");

        return Enumerable.Range(0, normalized.Length / 2)
            .Select(index => byte.Parse(normalized.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            .ToArray();
    }

    private static bool TryNormalizeMeterAddress(string value, out string address)
    {
        address = NormalizeAddress(value);
        return address.Length == 12 && address.All(char.IsDigit);
    }

    private static string NormalizeAddress(string value) =>
        new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();

    private static void Trace(int stationNo, string message, Action<int, string>? stationLogger)
    {
        LogMessage.Debug($"[蓝牙接口][工位{stationNo}] {message}");
        stationLogger?.Invoke(stationNo, message);
    }

    private static string FormatTimestamp() => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";
}

public enum MeterTestBluetoothStep
{
    Reset,
    ConnectMeter,
    Preprocess,
    ReadAddress
}

/// <summary>蓝牙检测使用的工位专用TCP端点、电表地址及配置校验结果。</summary>
public sealed record MeterTestBluetoothStation(
    int StationNo,
    string Ip,
    int Port,
    string MeterAddress,
    string ConfigurationError = "");

/// <summary>单个工位蓝牙小项执行结果。</summary>
public sealed record MeterTestBluetoothStationResult(
    int StationNo,
    bool Success,
    string Message,
    string MeterAddress)
{
    public static MeterTestBluetoothStationResult Pass(int stationNo, string message, string meterAddress = "") =>
        new(stationNo, true, message, meterAddress);

    public static MeterTestBluetoothStationResult Fail(int stationNo, string message, string meterAddress = "") =>
        new(stationNo, false, message, meterAddress);
}
