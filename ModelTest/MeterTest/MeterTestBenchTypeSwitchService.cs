using System.Net;
using ModelTest.Protocol;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest 升源前的台体类型切换服务。
///
/// 服务从选中工位资产档案中识别单相/三相及直接式/互感式，再按端点能力筛选通信板：
/// 单相只发送到supportsSinglePhase=true的端点，三相直接式和互感式发送到全部启用端点。
/// 目标端点都收到并校验通过应答后，调用方才能继续打开源串口和升源。
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

        IReadOnlyList<MeterTestBenchTypeSwitchEndpoint> configuredEndpoints = config.GetEnabledEndpoints();
        IReadOnlyList<MeterTestBenchTypeSwitchEndpoint> modeEndpoints =
            config.GetEnabledEndpointsForMode(connectionType);
        if (!TryValidateConfig(config, modeEndpoints, out string configError))
        {
            LogMessage.Info($"[台体切换] 配置错误：{configError}");
            return MeterTestBenchTypeSwitchResult.Fail(configError, connectionType);
        }

        List<MeterTestBenchTypeSwitchEndpoint> skippedEndpoints = configuredEndpoints
            .Where(endpoint => !modeEndpoints.Contains(endpoint))
            .ToList();
        string selectedEndpointText = string.Join(
            ',',
            modeEndpoints.Select(endpoint => $"{endpoint.DisplayName}({endpoint.Ip.Trim()}:{endpoint.Port})"));
        string skippedEndpointText = skippedEndpoints.Count == 0
            ? "无"
            : string.Join(',', skippedEndpoints.Select(endpoint => $"{endpoint.DisplayName}({endpoint.Ip.Trim()}:{endpoint.Port})"));
        LogMessage.Debug(
            $"[台体切换] 0x82端点筛选：资产模式={typeDescription}(0x{(byte)connectionType:X2})，"
            + $"实际发送端点={selectedEndpointText}，跳过端点={skippedEndpointText}。"
            + "程序启动阶段建立的TCP长连接不代表本次发送了0x82。");
        if (skippedEndpoints.Count > 0)
        {
            LogMessage.Debug(
                $"[台体切换] 模式={typeDescription}，跳过不支持单相的端点："
                + string.Join(',', skippedEndpoints.Select(endpoint => endpoint.DisplayName)));
        }

        // 同一IP:Port即同一块通信板。配置重复时只发送一次，避免重复切换和重复等待应答。
        List<MeterTestBenchTypeSwitchEndpoint> endpoints = modeEndpoints
            .GroupBy(endpoint => $"{endpoint.Ip.Trim()}:{endpoint.Port}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        byte[] request = protocol.BuildDeviceBoardConnectionModeFrame(
            address: 0x00,
            DeviceBoardControlSource.PcControl,
            connectionType);
        LogMessage.Debug(
            $"[台体切换] 资产信息判定完成：工位={string.Join(',', selectedStationNumbers.OrderBy(value => value))}，"
            + $"模式={typeDescription}(0x{(byte)connectionType:X2})，"
            + $"端点={string.Join(',', endpoints.Select(endpoint => $"{endpoint.DisplayName}({endpoint.Ip.Trim()}:{endpoint.Port})"))}");

        try
        {
            BenchTypeEndpointResult[] endpointResults = await Task.WhenAll(
                    endpoints.Select(endpoint => ExecuteEndpointAsync(
                        endpoint,
                        request,
                        connectionType,
                        config.TimeoutMs,
                        connectionManager,
                        cancellationToken)))
                .ConfigureAwait(false);

            BenchTypeEndpointResult[] failedResults = endpointResults.Where(result => !result.Success).ToArray();
            if (failedResults.Length > 0)
            {
                string failedDetail = string.Join("；", failedResults.Select(result => result.Message));
                string failureMessage =
                    $"台体类型切换未全部成功：成功{endpointResults.Length - failedResults.Length}/{endpointResults.Length}，"
                    + $"失败详情：{failedDetail}";
                LogMessage.Info($"[台体切换] {failureMessage}");
                return MeterTestBenchTypeSwitchResult.Fail(failureMessage, connectionType);
            }

            string successMessage =
                $"{endpointResults.Length}个装置通信板均已切换为{typeDescription}，0x82应答正常；"
                + $"实际发送端点={selectedEndpointText}，跳过端点={skippedEndpointText}。";
            LogMessage.Debug($"[台体切换] {successMessage}");
            if (config.DelayAfterSuccessMs > 0)
            {
                LogMessage.Debug(
                    $"[台体切换] 所有端点成功后统一等待 {config.DelayAfterSuccessMs}ms，再进入控源流程。");
                await Task.Delay(config.DelayAfterSuccessMs, cancellationToken).ConfigureAwait(false);
            }

            return MeterTestBenchTypeSwitchResult.Succeeded(successMessage, connectionType);
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

    /// <summary>向一个端点发送0x82并等待该连接上的匹配应答。</summary>
    private async Task<BenchTypeEndpointResult> ExecuteEndpointAsync(
        MeterTestBenchTypeSwitchEndpoint endpointConfig,
        byte[] request,
        DeviceBoardConnectionMode connectionType,
        int timeoutMs,
        MeterTestControlPcbConnectionManager connectionManager,
        CancellationToken cancellationToken)
    {
        string endpoint = $"{endpointConfig.Ip.Trim()}:{endpointConfig.Port}";
        string displayName = $"{endpointConfig.DisplayName}({endpoint})";
        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            if (!connectionManager.TryGetConnectedConnection(
                    endpointConfig.Ip,
                    endpointConfig.Port,
                    MeterControlPcbProtocolVersion.V2.ToString(),
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                return BenchTypeEndpointResult.Fail($"{displayName}：{connectionError}");
            }

            TaskCompletionSource<byte[]> responseSource =
                new(TaskCreationOptions.RunContinuationsAsynchronously);
            using IDisposable responseSubscription = connection.Subscribe(frame =>
            {
                if (protocol.TryValidateDeviceBoardConnectionModeResponse(frame, connectionType, out string parseMessage))
                {
                    responseSource.TrySetResult(frame);
                    return;
                }

                LogMessage.Debug(
                    $"[台体切换][{displayName}] 收到非当前0x82应答，已忽略："
                    + $"报文={DetectionBoardProtocolV2.ToHexString(frame)}，说明={parseMessage}。"
                );
            });

            LogMessage.Debug($"[台体切换][{displayName}] 复用装置通信板长连接。");
            LogMessage.Debug(
                $"[台体切换][{displayName}][PC-->通信板] 准备发送0x82："
                + $"模式={DescribeConnectionType(connectionType)}，超时={timeoutMs}ms，"
                + $"报文={DetectionBoardProtocolV2.ToHexString(request)}"
            );
            await connection.SendAsync(request, timeoutCts.Token).ConfigureAwait(false);

            byte[] response = await responseSource.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
            LogMessage.Debug(
                $"[台体切换][{displayName}] 收到匹配的0x82应答[通信板-->PC]："
                + DetectionBoardProtocolV2.ToHexString(response));
            return BenchTypeEndpointResult.Succeeded($"{displayName}切换成功");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            string message = $"{displayName}等待0x82应答超时({timeoutMs}ms)";
            LogMessage.Info($"[台体切换] {message}。");
            return BenchTypeEndpointResult.Fail(message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            string message = $"{displayName}切换异常：{ex.Message}";
            LogMessage.Error($"[台体切换] {message}", ex);
            return BenchTypeEndpointResult.Fail(message);
        }
    }

    /// <summary>检查全部通信端点及超时参数。</summary>
    private static bool TryValidateConfig(
        MeterTestBenchTypeSwitchConfig config,
        IReadOnlyList<MeterTestBenchTypeSwitchEndpoint> endpoints,
        out string error)
    {
        error = string.Empty;
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

        if (endpoints.Count == 0)
        {
            error = "台体类型切换已启用，但没有配置支持当前台体模式的启用Endpoint。";
            return false;
        }

        foreach (MeterTestBenchTypeSwitchEndpoint endpoint in endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Ip) || !IPAddress.TryParse(endpoint.Ip.Trim(), out _))
            {
                error = $"台体类型切换端点{endpoint.DisplayName}的IP无效：{endpoint.Ip}";
                return false;
            }

            if (endpoint.Port is < 1 or > 65535)
            {
                error = $"台体类型切换端点{endpoint.DisplayName}的端口必须在1-65535之间，当前={endpoint.Port}。";
                return false;
            }
        }

        return true;
    }

    /// <summary>单个装置通信板的切换结果，用于汇总多端点执行状态。</summary>
    private sealed record BenchTypeEndpointResult(bool Success, string Message)
    {
        /// <summary>创建单个装置通信板切换成功结果。</summary>
        public static BenchTypeEndpointResult Succeeded(string message) => new(true, message);

        /// <summary>创建单个装置通信板切换失败结果。</summary>
        public static BenchTypeEndpointResult Fail(string message) => new(false, message);
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

    /// <summary>将 0x82 台体模式枚举转换为日志使用的中文说明。</summary>
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
    /// <summary>创建台体类型切换整体成功结果。</summary>
    public static MeterTestBenchTypeSwitchResult Succeeded(
        string message,
        DeviceBoardConnectionMode? connectionType)
        => new(true, message, connectionType);

    /// <summary>创建台体类型切换整体失败结果，并保留已解析的目标模式。</summary>
    public static MeterTestBenchTypeSwitchResult Fail(
        string message,
        DeviceBoardConnectionMode? connectionType = null)
        => new(false, message, connectionType);
}
