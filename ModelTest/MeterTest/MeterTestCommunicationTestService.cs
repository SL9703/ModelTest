using System.Net;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest“通信测试”业务流程服务。
///
/// 本服务负责三类非 UI 工作：
/// 1. 按串口服务器 IP 去重执行 64444/F3/F1 波特率同步，并缓存本轮结果供四个方案节点复用；
/// 2. 执行工位 485 TCP 一发一收、698 地址读取和备用波特率循环；
/// 3. 完成普通 HEX 匹配、698 地址解析、实际地址比对和逐工位结论生成。
///
/// 窗体只传入日志与状态回调，不再直接建立 TCP、构造 698 报文或解析协议。
/// </summary>
internal sealed class MeterTestCommunicationTestService
{
    private readonly MeterTestSerialPortServerService serialPortServerService;
    private readonly MeterTestCommunicationAddressService communicationAddressService;
    private readonly MeterTestStationTcpSessionService stationTcpSessionService;
    private readonly MeterTestAccessDatabaseService accessDatabaseService;
    private bool serialFlowExecuted;
    private bool serialFlowSucceeded;
    private IReadOnlyDictionary<int, bool> serialStationResults = new Dictionary<int, bool>();
    private IReadOnlyDictionary<int, MeterTestSerialPortServerStationTrace> serialStationTraces =
        new Dictionary<int, MeterTestSerialPortServerStationTrace>();

    /// <summary>
    /// 创建通信测试服务。
    /// </summary>
    /// <param name="serialPortServerService">电表 V2 串口服务器 F3/F1 参数同步服务。</param>
    /// <param name="communicationAddressService">698 地址读取及通用底层协议备用波特率服务。</param>
    /// <param name="stationTcpSessionService">普通工位 TCP 长连接服务。</param>
    /// <param name="accessDatabaseService">资产数据库服务，用于读取可尝试的波特率候选项。</param>
    public MeterTestCommunicationTestService(
        MeterTestSerialPortServerService serialPortServerService,
        MeterTestCommunicationAddressService communicationAddressService,
        MeterTestStationTcpSessionService stationTcpSessionService,
        MeterTestAccessDatabaseService accessDatabaseService)
    {
        this.serialPortServerService = serialPortServerService;
        this.communicationAddressService = communicationAddressService;
        this.stationTcpSessionService = stationTcpSessionService;
        this.accessDatabaseService = accessDatabaseService;
    }

    /// <summary>
    /// 开始新的方案执行批次，清空上一次串口服务器同步缓存。
    /// 64444 与工位 TCP 连接的实际生命周期仍由各自连接服务统一管理。
    /// </summary>
    public void BeginRun()
    {
        serialFlowExecuted = false;
        serialFlowSucceeded = false;
        serialStationResults = new Dictionary<int, bool>();
        serialStationTraces = new Dictionary<int, MeterTestSerialPortServerStationTrace>();
        LogMessage.Debug("[通信测试服务] 新测试批次开始，已清空串口服务器步骤缓存。");
    }

    /// <summary>
    /// 执行方案树中的一个串口服务器波特率节点。
    /// 第一次调用完成完整 F3/F1 流程；后续 Connect/ReadParameters/Compare/Apply 节点只回放对应日志，
    /// 保证一个 IP 的 64444 管理操作不会因树节点拆分而重复发送。
    /// </summary>
    /// <param name="context">当前串口服务器方案小项。</param>
    /// <param name="selectedStations">本轮选中的工位通信配置。</param>
    /// <param name="writeStationLog">写入工位文件日志和右侧过程日志的回调。</param>
    /// <param name="cancellationToken">停止测试时使用的取消令牌。</param>
    public async Task<MeterTestCommunicationBatchStepResult> ExecuteSerialPortServerStepAsync(
        SelectedSubItemContext context,
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        Action<int, string[]> writeStationLog,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        if (!serialFlowExecuted)
        {
            serialFlowExecuted = true;
            MeterTestSerialPortServerFlowResult flow = await EnsureSerialServerBaudRatesAsync(
                selectedStations,
                cancellationToken).ConfigureAwait(false);
            serialFlowSucceeded = flow.Succeeded;
            serialStationResults = flow.StationResults;
            serialStationTraces = flow.StationTraces;
        }

        Dictionary<int, MeterTestCommunicationStationResult> stationResults = new();
        foreach (StationCommunicationConfig station in selectedStations)
        {
            bool passed = serialStationResults.TryGetValue(station.StationNo, out bool stationPassed) && stationPassed;
            string message = passed
                ? "串口服务器波特率步骤已完成。"
                : "串口服务器波特率流程存在失败；按照通信测试容错规则继续执行后续地址读取。";
            string[] logLines = BuildSerialPortServerStepLog(context, station);
            writeStationLog(station.StationNo, logLines);
            stationResults[station.StationNo] = new MeterTestCommunicationStationResult(
                station.StationNo,
                passed,
                string.Empty,
                message,
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        string summary = serialFlowSucceeded
            ? "串口服务器波特率检查流程完成，当前方案节点未重复发送报文。"
            : "串口服务器波特率检查存在失败步骤，已记录完整接口日志并继续后续地址读取。";
        LogMessage.Debug(
            $"[通信测试服务][串口服务器][{context.SubItem.Name}] 完成："
            + $"结论={(serialFlowSucceeded ? "合格" : "不合格")}，{summary}");
        return new MeterTestCommunicationBatchStepResult(
            serialFlowSucceeded,
            summary,
            stationResults,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    /// <summary>
    /// 执行单个工位的 StationTcp 测试。
    /// 地址读取先使用资产波特率；未得到合法地址时依次切换数据库候选波特率，任意一次成功即停止。
    /// 普通测试则按 XML 的 ExpectedResponse/MatchMode 判定。
    /// </summary>
    /// <param name="station">目标工位通信配置。</param>
    /// <param name="context">当前方案小项。</param>
    /// <param name="writeStationLog">写入工位文件日志和右侧过程日志的回调。</param>
    /// <param name="cancellationToken">停止测试时使用的取消令牌。</param>
    public async Task<MeterTestCommunicationStationResult> ExecuteStationStepAsync(
        StationCommunicationConfig station,
        SelectedSubItemContext context,
        Action<int, string[]> writeStationLog,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        string response = string.Empty;
        bool passed = false;
        string message;

        LogMessage.Debug(
            $"[通信测试服务][工位{station.StationNo}] 开始：小项={context.SubItem.Name}，"
            + $"端点={station.Ip}:{station.Port}，资产波特率={station.BaudRate}，"
            + $"资产地址={NormalizeMeterAddress(station.MeterAddress)}。");
        try
        {
            if (UsesSgcc698AddressParser(context.SubItem))
            {
                (response, passed, message) = await ExecuteAddressReadAsync(
                    station,
                    context,
                    writeStationLog,
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                string requestHex = BuildStationRequestHex(station, context);
                response = await stationTcpSessionService.SendRequestAsync(
                    station,
                    requestHex,
                    $"测试内容={context.SubItem.Name}",
                    context.SubItem.TimeoutMs,
                    line => WriteInterfaceTrace(station, writeStationLog, line),
                    cancellationToken).ConfigureAwait(false);
                passed = IsResponseMatched(context.SubItem, response);
                message = passed
                    ? "应答匹配，测试通过。"
                    : $"应答不匹配，模式={context.SubItem.MatchMode}，"
                      + $"期望={NormalizeHex(context.SubItem.ExpectedResponse)}，实际={NormalizeHex(response)}。";
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            passed = false;
            message = UsesSgcc698AddressParser(context.SubItem)
                ? $"电表无响应，等待超时={Math.Max(100, context.SubItem.TimeoutMs)}ms。"
                : $"等待响应超时={Math.Max(100, context.SubItem.TimeoutMs)}ms。";
            WriteInterfaceTrace(station, writeStationLog, message);
            LogMessage.Error($"[通信测试服务][工位{station.StationNo}] {message}", null);
        }
        catch (OperationCanceledException)
        {
            passed = false;
            message = "测试被取消，当前工位未收到完整应答。";
            WriteInterfaceTrace(station, writeStationLog, message);
        }
        catch (Exception ex)
        {
            passed = false;
            message = $"执行异常：{ex.Message}";
            WriteInterfaceTrace(station, writeStationLog, message);
            LogMessage.Error(
                $"[通信测试服务][工位{station.StationNo}] 接口执行异常："
                + $"端点={station.Ip}:{station.Port}，小项={context.SubItem.Name}。",
                ex);
        }

        long elapsed = Math.Max(0, Environment.TickCount64 - startTicks);
        LogMessage.Debug(
            $"[通信测试服务][工位{station.StationNo}] 完成：小项={context.SubItem.Name}，"
            + $"耗时={elapsed}ms，结论={(passed ? "合格" : "不合格")}，说明={message}，"
            + $"最终响应={NormalizeHex(response)}。");
        return new MeterTestCommunicationStationResult(
            station.StationNo,
            passed,
            response,
            message,
            elapsed);
    }

    /// <summary>
    /// 执行地址读取的“资产波特率优先 + 备用波特率循环”业务规则。
    /// </summary>
    private async Task<(string Response, bool Passed, string Message)> ExecuteAddressReadAsync(
        StationCommunicationConfig station,
        SelectedSubItemContext context,
        Action<int, string[]> writeStationLog,
        CancellationToken cancellationToken)
    {
        string requestHex = BuildStationRequestHex(station, context);
        WriteInterfaceTrace(station, writeStationLog, MeterTestLogText.Separator);
        string response = string.Empty;
        try
        {
            response = await communicationAddressService.ReadAssetBaudRateAddressAsync(
                BuildAddressRequirement(station, context.SubItem, tryAssetBaudRateFirst: true),
                requestHex,
                line => WriteInterfaceTrace(station, writeStationLog, line),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            WriteInterfaceTrace(
                station,
                writeStationLog,
                $"资产波特率首次地址读取异常：{ex.Message}；继续尝试备用波特率。");
            LogMessage.Error(
                $"[通信测试服务][工位{station.StationNo}] 资产波特率首次地址读取异常。",
                ex);
        }

        string actualAddress = NormalizeMeterAddress(station.MeterAddress);
        Sgcc698BroadcastAddressParseResult? initialParse = string.IsNullOrWhiteSpace(response)
            ? null
            : ParseAddressResponse(context.SubItem, response);
        if (initialParse?.IsValid == true)
        {
            string returnedAddress = NormalizeMeterAddress(initialParse.MeterAddress);
            bool passed = actualAddress.Equals(returnedAddress, StringComparison.OrdinalIgnoreCase);
            string message = passed ? "电表响应正常。" : "电表响应地址与资产地址不一致。";
            WriteInterfaceTrace(
                station,
                writeStationLog,
                $"698解析：OAD={initialParse.Oad}，APDU={initialParse.Apdu}，说明={initialParse.Message}。",
                $"实际地址：{actualAddress}；返回地址：{returnedAddress}；"
                + $"有效波特率：{station.BaudRate}；结论：{(passed ? "合格" : "不合格")}。",
                MeterTestLogText.Separator);
            return (response, passed, message);
        }

        WriteInterfaceTrace(
            station,
            writeStationLog,
            string.IsNullOrWhiteSpace(response)
                ? $"资产波特率 {station.BaudRate} 未读取到地址，开始追加尝试其他波特率。"
                : $"资产波特率 {station.BaudRate} 响应未解析出地址：{initialParse?.Message}；"
                  + "开始追加尝试其他波特率。");

        IReadOnlyList<string> baudRates = accessDatabaseService
            .LoadAssetOptions("BaudRate")
            .Select(option => option.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToList();
        MeterTestCommunicationAddressResult result = await communicationAddressService.ExecuteAsync(
            BuildAddressRequirement(station, context.SubItem, tryAssetBaudRateFirst: false),
            baudRates,
            line => WriteInterfaceTrace(station, writeStationLog, line),
            cancellationToken).ConfigureAwait(false);
        WriteInterfaceTrace(
            station,
            writeStationLog,
            $"通信测试结论：{(result.Passed ? "合格" : "不合格")}，"
            + (result.AddressParsed
                ? $"有效波特率={result.SuccessfulBaudRate}，返回地址={result.ReturnedAddress}。"
                : "资产波特率及全部备用波特率均未读取到地址。"),
            MeterTestLogText.Separator);
        return (result.ResponseHex, result.Passed, result.Message);
    }

    /// <summary>
    /// 按 IP 去重执行串口服务器 F3 读取、参数核对和 F1 修改，并建立逐工位可回放跟踪。
    /// </summary>
    private async Task<MeterTestSerialPortServerFlowResult> EnsureSerialServerBaudRatesAsync(
        IReadOnlyList<StationCommunicationConfig> selectedStations,
        CancellationToken cancellationToken)
    {
        List<IGrouping<string, StationCommunicationConfig>> groups = selectedStations
            .GroupBy(station => NormalizeIp(station.Ip), StringComparer.OrdinalIgnoreCase)
            .ToList();
        LogMessage.Debug(
            $"[通信测试服务][64444] 选中工位={selectedStations.Count}，按IP去重后管理连接={groups.Count}。"
            + $" IP={string.Join(',', groups.Select(group => group.Key))}。");

        MeterTestSerialPortServerResult[] results = await Task.WhenAll(groups.Select(group =>
            serialPortServerService.EnsureBaudRatesAsync(
                group.Key,
                group.Select(station => new MeterTestSerialPortBaudRequirement(
                    station.StationNo,
                    station.Port,
                    station.BaudRate)).ToList(),
                cancellationToken))).ConfigureAwait(false);

        bool allSucceeded = true;
        Dictionary<int, bool> stationResults = new();
        Dictionary<int, MeterTestSerialPortServerStationTrace> traces = new();
        for (int index = 0; index < groups.Count; index++)
        {
            IGrouping<string, StationCommunicationConfig> group = groups[index];
            MeterTestSerialPortServerResult result = results[index];
            string shared =
                $"同一IP工位 {string.Join(',', group.Select(station => station.StationNo))} "
                + $"共用一次 {group.Key}:{MeterTestSerialPortServerService.ManagementPort} 管理流程。";
            List<string> details = new() { shared };
            details.AddRange(result.Details);
            foreach (StationCommunicationConfig station in group)
            {
                stationResults[station.StationNo] = result.Success;
                traces[station.StationNo] = new MeterTestSerialPortServerStationTrace(
                    group.Key,
                    result.Success,
                    result.Message,
                    details);
            }

            string fullDetails = string.Join(Environment.NewLine, details);
            LogMessage.Debug(
                $"[通信测试服务][64444][{group.Key}] 结论={(result.Success ? "合格" : "不合格")}，"
                + $"说明={result.Message}{Environment.NewLine}{fullDetails}");
            allSucceeded &= result.Success;
        }

        return new MeterTestSerialPortServerFlowResult(allSucceeded, stationResults, traces);
    }

    /// <summary>
    /// 根据当前方案节点筛选串口服务器跟踪明细，并生成单工位日志块。
    /// </summary>
    private string[] BuildSerialPortServerStepLog(
        SelectedSubItemContext context,
        StationCommunicationConfig station)
    {
        if (!serialStationTraces.TryGetValue(station.StationNo, out MeterTestSerialPortServerStationTrace? trace))
        {
            return new[]
            {
                MeterTestLogText.Separator,
                $"测试小项：{context.SubItem.Name}",
                "串口服务器流程没有返回当前工位跟踪信息。",
                "步骤结论：不合格",
                MeterTestLogText.Separator
            };
        }

        List<string> details = trace.Details
            .Where(detail => IsDetailForStation(detail, station))
            .Where(detail => IsDetailForStep(detail, context.SubItem.SerialPortServerStep))
            .ToList();
        if (details.Count == 0)
            details.Add(GetStepFallback(context.SubItem.SerialPortServerStep, trace));

        return new[]
        {
            MeterTestLogText.Separator,
            $"测试小项：{context.SubItem.Name}",
            $"串口服务器：{trace.IpAddress}:{MeterTestSerialPortServerService.ManagementPort}",
            $"工位配置：工位{station.StationNo}，端口={station.Port}，波特率={station.BaudRate}"
        }
        .Concat(details)
        .Concat(new[]
        {
            $"步骤结论：{(trace.Success ? "合格" : "不合格")}",
            MeterTestLogText.Separator
        })
        .ToArray();
    }

    /// <summary>生成 698 地址读取服务需要的完整工位参数。</summary>
    private static MeterTestCommunicationAddressRequirement BuildAddressRequirement(
        StationCommunicationConfig station,
        MeterTestSubItem subItem,
        bool tryAssetBaudRateFirst)
    {
        return new MeterTestCommunicationAddressRequirement(
            station.StationNo,
            station.Ip,
            station.Port,
            station.MeterAddress,
            station.BaudRate,
            subItem.TimeoutMs,
            subItem.ExpectedOad,
            subItem.ExpectedApdu,
            subItem.ExpectedDataType,
            subItem.ExpectedDataLength,
            tryAssetBaudRateFirst);
    }

    /// <summary>生成当前工位请求；698 地址读取按资产地址动态重算 HCS/FCS。</summary>
    private static string BuildStationRequestHex(
        StationCommunicationConfig station,
        SelectedSubItemContext context)
    {
        if (!UsesSgcc698AddressParser(context.SubItem))
            return context.SubItem.RequestHex;

        if (string.IsNullOrWhiteSpace(station.MeterAddress))
            throw new InvalidOperationException($"工位{station.StationNo}未配置电表地址，无法生成定址698读地址报文。");

        string request = SGCCTools.BuildMeterAddressReadRequest(station.MeterAddress);
        LogMessage.Debug(
            $"[通信测试服务][工位{station.StationNo}][698组帧] 地址={NormalizeMeterAddress(station.MeterAddress)}，"
            + $"OAD={context.SubItem.ExpectedOad}，完整下行={NormalizeHex(request)}。");
        return NormalizeHex(request);
    }

    /// <summary>按 XML 中的 APDU/OAD/类型/长度约束解析 698 地址响应。</summary>
    private static Sgcc698BroadcastAddressParseResult ParseAddressResponse(
        MeterTestSubItem subItem,
        string responseHex)
    {
        return SGCCTools.ParseBroadcastAddressResponse(
            NormalizeHex(responseHex),
            subItem.ExpectedOad,
            subItem.ExpectedApdu,
            subItem.ExpectedDataType,
            subItem.ExpectedDataLength);
    }

    /// <summary>判断方案小项是否使用国网 698 地址读取解析器。</summary>
    private static bool UsesSgcc698AddressParser(MeterTestSubItem subItem)
    {
        return subItem.ResponseParser.Equals(
            ResponseParserType.Sgcc698BroadcastAddress.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>按 Exact、StartsWith 或 Contains 规则匹配普通 HEX 响应。</summary>
    private static bool IsResponseMatched(MeterTestSubItem subItem, string? response)
    {
        string actual = NormalizeHex(response ?? string.Empty).Replace(" ", string.Empty);
        string expected = NormalizeHex(subItem.ExpectedResponse).Replace(" ", string.Empty);
        if (string.IsNullOrEmpty(expected))
            return !string.IsNullOrEmpty(actual);

        ResponseMatchMode mode = Enum.TryParse(subItem.MatchMode, true, out ResponseMatchMode parsed)
            ? parsed
            : ResponseMatchMode.Contains;
        return mode switch
        {
            ResponseMatchMode.Exact => actual.Equals(expected, StringComparison.OrdinalIgnoreCase),
            ResponseMatchMode.StartsWith => actual.StartsWith(expected, StringComparison.OrdinalIgnoreCase),
            _ => actual.Contains(expected, StringComparison.OrdinalIgnoreCase)
        };
    }

    /// <summary>过滤同一 IP 组内属于其他工位的端口明细。</summary>
    private static bool IsDetailForStation(string detail, StationCommunicationConfig station)
    {
        int? detailStationNo = TryExtractStationNo(detail);
        if (detailStationNo.HasValue && detailStationNo.Value != station.StationNo)
            return false;

        return !detail.StartsWith("读取端口 ", StringComparison.OrdinalIgnoreCase) ||
               detail.Contains($"读取端口 {station.Port}（", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>按方案中的 Connect/ReadParameters/Compare/Apply 分类接口跟踪。</summary>
    private static bool IsDetailForStep(string detail, string step)
    {
        return (step ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "CONNECT" => detail.Contains("连接", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("准备", StringComparison.OrdinalIgnoreCase),
            "READPARAMETERS" => detail.Contains("读取", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("F3", StringComparison.OrdinalIgnoreCase),
            "COMPARE" => detail.Contains("待检查", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("一致", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("不一致", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("匹配", StringComparison.OrdinalIgnoreCase),
            "APPLY" => detail.Contains("解锁", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("修改", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("设置", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("F1", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    /// <summary>当前步骤没有独立报文时生成明确的回放说明。</summary>
    private static string GetStepFallback(string step, MeterTestSerialPortServerStationTrace trace)
    {
        return (step ?? string.Empty).Trim().ToUpperInvariant() switch
        {
            "CONNECT" => trace.Message,
            "READPARAMETERS" => $"未取得独立读取明细；完整流程结论：{trace.Message}",
            "COMPARE" => $"未取得独立校验明细；完整流程结论：{trace.Message}",
            "APPLY" when trace.Success => "所有目标端口参数一致，本步骤无需修改。",
            "APPLY" => $"未完成参数修改；完整流程结论：{trace.Message}",
            _ => trace.Message
        };
    }

    /// <summary>从串口服务器明细中的“工位N”文本提取单个工位号。</summary>
    private static int? TryExtractStationNo(string value)
    {
        System.Text.RegularExpressions.Match match =
            System.Text.RegularExpressions.Regex.Match(value ?? string.Empty, @"工位(?<No>\d+)");
        return match.Success && int.TryParse(match.Groups["No"].Value, out int stationNo)
            ? stationNo
            : null;
    }

    /// <summary>规范化 IP 文本，防止等价地址因空格或大小写被拆成多个管理连接。</summary>
    private static string NormalizeIp(string ipAddress)
    {
        string normalized = (ipAddress ?? string.Empty).Trim();
        return IPAddress.TryParse(normalized, out IPAddress? parsed)
            ? parsed.ToString()
            : normalized.ToUpperInvariant();
    }

    /// <summary>将地址统一为 6 字节连续大写 HEX；无效地址返回空字符串。</summary>
    private static string NormalizeMeterAddress(string? meterAddress)
    {
        string normalized = new((meterAddress ?? string.Empty)
            .Where(Uri.IsHexDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
        return normalized.Length == 12 ? normalized : string.Empty;
    }

    /// <summary>将任意带分隔符 HEX 文本规范化为大写、单空格分隔格式。</summary>
    private static string NormalizeHex(string? value)
    {
        string compact = new((value ?? string.Empty)
            .Where(Uri.IsHexDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
        if (compact.Length == 0 || compact.Length % 2 != 0)
            return (value ?? string.Empty).Trim();

        return string.Join(" ", Enumerable.Range(0, compact.Length / 2)
            .Select(index => compact.Substring(index * 2, 2)));
    }

    /// <summary>同时写入业务日志回调和全局 Debug 日志，确保接口明细在两处均可追溯。</summary>
    private static void WriteInterfaceTrace(
        StationCommunicationConfig station,
        Action<int, string[]> writeStationLog,
        params string[] lines)
    {
        writeStationLog(station.StationNo, lines);
        foreach (string line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
            LogMessage.Debug($"[通信测试服务][工位{station.StationNo}] {line}");
    }
}

/// <summary>通信服务执行单个工位小项后的完整业务结果。</summary>
internal sealed record MeterTestCommunicationStationResult(
    int StationNo,
    bool Passed,
    string ResponseHex,
    string Message,
    long ElapsedMilliseconds);

/// <summary>通信服务执行一个多工位步骤后的整体结论和逐工位结果。</summary>
internal sealed record MeterTestCommunicationBatchStepResult(
    bool Passed,
    string Message,
    IReadOnlyDictionary<int, MeterTestCommunicationStationResult> StationResults,
    long ElapsedMilliseconds);

/// <summary>一轮串口服务器波特率同步的整体结果与逐工位跟踪。</summary>
internal sealed record MeterTestSerialPortServerFlowResult(
    bool Succeeded,
    IReadOnlyDictionary<int, bool> StationResults,
    IReadOnlyDictionary<int, MeterTestSerialPortServerStationTrace> StationTraces);

/// <summary>串口服务器完整流程在单个工位上的可回放接口日志。</summary>
internal sealed record MeterTestSerialPortServerStationTrace(
    string IpAddress,
    bool Success,
    string Message,
    IReadOnlyList<string> Details);
