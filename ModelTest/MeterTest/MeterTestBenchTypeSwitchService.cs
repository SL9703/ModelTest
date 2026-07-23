using System.Net;
using ModelTest.Protocol;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 升源前的台体类型切换服务。
///
/// 服务从选中工位资产档案中识别单相/三相及直接式/互感式，向方案配置的固定装置通信板
/// 发送 0x82 命令。只有收到并校验通过的应答后，调用方才能继续打开源串口和升源。
/// </summary>
public sealed class MeterTestBenchTypeSwitchService
{
    private readonly DetectionBoardProtocolV2 protocol = new();

    /// <summary>根据资产信息切换台体类型并等待应答。</summary>
    public async Task<MeterTestBenchTypeSwitchResult> ExecuteAsync(
        MeterTestBenchTypeSwitchConfig config,
        IReadOnlyList<int> selectedStationNumbers,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        MeterTestControlPcbConnectionManager connectionManager,
        CancellationToken cancellationToken)
    {
        if (!config.Enabled)
        {
            const string skippedMessage = "台体类型切换配置已禁用，跳过0x82命令。";
            LogMessage.Info($"[台体切换] {skippedMessage}");
            return MeterTestBenchTypeSwitchResult.Succeeded(skippedMessage, null);
        }

        if (!TryValidateConfig(config, out string configError))
        {
            LogMessage.Info($"[台体切换] 配置错误：{configError}");
            return MeterTestBenchTypeSwitchResult.Fail(configError);
        }

        if (!TryResolveConnectionType(
                selectedStationNumbers,
                meterArchives,
                out DeviceBoardConnectionMode connectionType,
                out string typeDescription,
                out string typeError))
        {
            LogMessage.Info($"[台体切换] 资产信息判定失败：{typeError}");
            return MeterTestBenchTypeSwitchResult.Fail(typeError);
        }

        string endpoint = $"{config.Ip.Trim()}:{config.Port}";
        byte[] request = protocol.BuildDeviceBoardConnectionModeFrame(
            address: 0x00,
            DeviceBoardControlSource.PcControl,
            connectionType);
        LogMessage.Debug(
            $"[台体切换] 资产信息判定完成：工位={string.Join(',', selectedStationNumbers.OrderBy(value => value))}，"
            + $"模式={typeDescription}(0x{(byte)connectionType:X2})，Endpoint={endpoint}");

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(config.TimeoutMs);

        try
        {
            if (!connectionManager.TryGetConnectedConnection(
                    config.Ip,
                    config.Port,
                    MeterControlPcbProtocolVersion.V2.ToString(),
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                return MeterTestBenchTypeSwitchResult.Fail(connectionError, connectionType);
            }

            TaskCompletionSource<byte[]> responseSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            using IDisposable responseSubscription = connection.Subscribe(frame =>
            {
                if (protocol.TryValidateDeviceBoardConnectionModeResponse(frame, connectionType, out _))
                {
                    responseSource.TrySetResult(frame);
                }
            });

            LogMessage.Debug($"[台体切换] 复用装置通信板长连接：{endpoint}");
            await connection.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);
            LogMessage.Debug(
                $"[台体切换] 发送0x82台体类型切换[PC-->通信板]：{DetectionBoardProtocolV2.ToHexString(request)}");

            byte[] response = await responseSource.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            LogMessage.Debug(
                $"[台体切换] 收到匹配的0x82应答[通信板-->PC]：{DetectionBoardProtocolV2.ToHexString(response)}");

            string successMessage = $"台体已切换为{typeDescription}，0x82应答正常。";
            LogMessage.Debug($"[台体切换] {successMessage}");
            if (config.DelayAfterSuccessMs > 0)
            {
                LogMessage.Debug($"[台体切换] 成功后等待 {config.DelayAfterSuccessMs}ms，再进入控源流程。");
                await Task.Delay(config.DelayAfterSuccessMs, cancellationToken).ConfigureAwait(false);
            }

            return MeterTestBenchTypeSwitchResult.Succeeded(successMessage, connectionType);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            string message = $"连接装置通信板或等待0x82应答超时，Endpoint={endpoint}，超时={config.TimeoutMs}ms。";
            LogMessage.Info($"[台体切换] {message}");
            return MeterTestBenchTypeSwitchResult.Fail(message, connectionType);
        }
        catch (OperationCanceledException)
        {
            LogMessage.Debug("[台体切换] 操作已取消。");
            throw;
        }
        catch (Exception ex)
        {
            string message = $"台体类型切换失败：{ex.Message}";
            LogMessage.Error($"[台体切换] {message}", ex);
            return MeterTestBenchTypeSwitchResult.Fail(message, connectionType);
        }
    }

    /// <summary>检查固定通信端点及超时参数。</summary>
    private static bool TryValidateConfig(MeterTestBenchTypeSwitchConfig config, out string error)
    {
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(config.Ip) || !IPAddress.TryParse(config.Ip.Trim(), out _))
        {
            error = $"台体类型切换IP无效：{config.Ip}";
            return false;
        }

        if (config.Port is < 1 or > 65535)
        {
            error = $"台体类型切换端口必须在1-65535之间，当前={config.Port}。";
            return false;
        }

        if (config.TimeoutMs <= 0)
        {
            error = $"台体类型切换超时时间必须大于0，当前={config.TimeoutMs}ms。";
            return false;
        }

        if (config.DelayAfterSuccessMs < 0)
        {
            error = $"台体类型切换成功后等待时间不能小于0，当前={config.DelayAfterSuccessMs}ms。";
            return false;
        }

        return true;
    }

    /// <summary>
    /// 从所有选中工位资产档案中识别唯一接线类型。
    /// 同一套台体不能同时切换成多个模式，因此混选不同类型时直接返回错误。
    /// </summary>
    private static bool TryResolveConnectionType(
        IReadOnlyList<int> selectedStationNumbers,
        IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
        out DeviceBoardConnectionMode connectionType,
        out string description,
        out string error)
    {
        connectionType = default;
        description = string.Empty;
        error = string.Empty;

        if (selectedStationNumbers.Count == 0)
        {
            error = "未选择工位，不能切换台体类型。";
            return false;
        }

        Dictionary<DeviceBoardConnectionMode, List<int>> stationsByType = new();
        foreach (int stationNo in selectedStationNumbers.Distinct().OrderBy(value => value))
        {
            if (!meterArchives.TryGetValue(stationNo, out MeterArchiveData? archive))
            {
                error = $"工位{stationNo}缺少资产信息，不能判定台体类型。";
                return false;
            }

            if (!TryResolveArchiveConnectionType(archive, out DeviceBoardConnectionMode stationType, out string stationError))
            {
                error = $"工位{stationNo}{stationError}";
                return false;
            }

            if (!stationsByType.TryGetValue(stationType, out List<int>? stations))
            {
                stations = new List<int>();
                stationsByType[stationType] = stations;
            }

            stations.Add(stationNo);
        }

        if (stationsByType.Count != 1)
        {
            string detail = string.Join(
                "；",
                stationsByType.Select(pair => $"{DescribeConnectionType(pair.Key)}=工位{string.Join(',', pair.Value)}"));
            error = $"选中工位存在多种台体类型，不能使用同一固定通信板切换：{detail}。";
            return false;
        }

        connectionType = stationsByType.Keys.Single();
        description = DescribeConnectionType(connectionType);
        return true;
    }

    /// <summary>将单个资产档案的电表类型和接入方式转换为 0x82 模式。</summary>
    private static bool TryResolveArchiveConnectionType(
        MeterArchiveData archive,
        out DeviceBoardConnectionMode connectionType,
        out string error)
    {
        connectionType = default;
        error = string.Empty;
        string meterType = (archive.MeterType ?? string.Empty).Trim();
        string accessMode = (archive.AccessMode ?? string.Empty).Trim();
        bool isSinglePhase = meterType.Contains("单相", StringComparison.OrdinalIgnoreCase);
        bool isThreePhase = meterType.Contains("三相", StringComparison.OrdinalIgnoreCase);
        bool isTransformer = accessMode.Contains("互感", StringComparison.OrdinalIgnoreCase);
        bool isDirect = accessMode.Contains("直接", StringComparison.OrdinalIgnoreCase);

        if (isSinglePhase)
        {
            if (isTransformer)
            {
                error = $"配置为单相互感式，0x82协议只支持单相直接式；电表类型={meterType}，接入方式={accessMode}。";
                return false;
            }

            if (!isDirect)
            {
                error = $"接入方式无法识别；电表类型={meterType}，接入方式={accessMode}。";
                return false;
            }

            connectionType = DeviceBoardConnectionMode.SinglePhase;
            return true;
        }

        if (!isThreePhase)
        {
            error = $"电表类型无法识别；电表类型={meterType}，接入方式={accessMode}。";
            return false;
        }

        if (isTransformer)
        {
            connectionType = DeviceBoardConnectionMode.ThreePhaseTransformer;
            return true;
        }

        if (isDirect)
        {
            connectionType = DeviceBoardConnectionMode.ThreePhaseDirect;
            return true;
        }

        error = $"接入方式无法识别；电表类型={meterType}，接入方式={accessMode}。";
        return false;
    }

    private static string DescribeConnectionType(DeviceBoardConnectionMode connectionType)
    {
        return connectionType switch
        {
            DeviceBoardConnectionMode.ThreePhaseDirect => "三相直接式",
            DeviceBoardConnectionMode.ThreePhaseTransformer => "三相互感式",
            DeviceBoardConnectionMode.SinglePhase => "单相直接式",
            _ => $"未知类型0x{(byte)connectionType:X2}"
        };
    }
}

/// <summary>台体类型切换执行结果。</summary>
public sealed record MeterTestBenchTypeSwitchResult(
    bool Success,
    string Message,
    DeviceBoardConnectionMode? ConnectionType)
{
    public static MeterTestBenchTypeSwitchResult Succeeded(
        string message,
        DeviceBoardConnectionMode? connectionType)
        => new(true, message, connectionType);

    public static MeterTestBenchTypeSwitchResult Fail(
        string message,
        DeviceBoardConnectionMode? connectionType = null)
        => new(false, message, connectionType);
}
