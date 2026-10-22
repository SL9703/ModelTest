using System.Diagnostics;
using System.Globalization;
using System.Text;
using ModelTest.Tools;

namespace ModelTest.MeterTest;

/// <summary>
/// 单相电表密钥恢复和密钥更新流程。服务内部按工位串行执行完整流程，避免方案树逐项调度破坏一收一发顺序。
/// </summary>
internal sealed class MeterTestKeyRecoveryService : IDisposable
{
    internal const string TestItemName = "密钥恢复";
    internal const string KeyUpdateTestItemName = "密钥更新";
    internal static readonly string[] StepNames =
    {
        "读取安全模式参数",
        "读取ESAM信息",
        "建立应用连接",
        "密钥恢复",
        "读取ESAM信息",
        "建立应用连接"
    };

    internal static readonly string[] KeyUpdateStepNames =
    {
        "读取安全模式参数",
        "读取ESAM信息",
        "建立应用连接",
        "密钥恢复",
        "读取ESAM信息",
        "建立应用连接",
        "密钥更新",
        "读取ESAM信息（更新后）",
        "建立应用连接（更新后）",
        "跳闸",
        "合闸",
        "广播冻结",
        "读取当前电表时间和日期"
    };

    private const string SecurityModeApdu = "050103F101020000";
    private const string EsamReadApdu = "05020203F1000200F1000400F100070000";
    private const string WinSocketServerSection = "WinSocketServer";
    private const string WinSocketIpKey = "ip";
    private const string WinSocketPortKey = "port";
    private const string KeyRecoveryTimeoutKey = "keyRecoveryTimeoutMs";
    private const int IniValueBufferSize = 255;
    private const int MeterRequestIntervalMs = 200;
    private const int SegmentedPayloadBytes = 450;
    private const int BroadcastWaitAfterSendMs = 3000;
    private const string MeterNumberReadApduPrefix = "0501";
    private const string CurrentDateTimeReadApduPrefix = "0501";
    private const string BroadcastDiv = "00000000000000AA";
    private const string BroadcastPiid = "38";
    private const string BroadcastAgseq = "00000034";
    private const string BroadcastTaskSuffix = "5000030012000000";

    private readonly MeterTestStationTcpSessionService stationTcpSessionService;
    private readonly MeterTestAccessDatabaseService databaseService;
    private readonly WinSocketServer winSocketServer = new();
    private readonly Dictionary<(MeterTestWorkflowKind Workflow, int StationNo), KeyRecoveryRunState> runs = new();
    private readonly object syncRoot = new();
    private bool encryptionConnected;

    public MeterTestKeyRecoveryService(
        MeterTestStationTcpSessionService stationTcpSessionService,
        MeterTestAccessDatabaseService databaseService)
    {
        this.stationTcpSessionService = stationTcpSessionService;
        this.databaseService = databaseService;
    }

    public void BeginRun()
    {
        lock (syncRoot)
        {
            runs.Clear();
        }
    }

    public async Task<MeterTestFlowStepResult> ExecuteStepAsync(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        return await ExecuteWorkflowStepAsync(
            MeterTestWorkflowKind.KeyRecovery,
            context,
            station,
            trace,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<MeterTestFlowStepResult> ExecuteKeyUpdateStepAsync(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        return await ExecuteWorkflowStepAsync(
            MeterTestWorkflowKind.KeyUpdate,
            context,
            station,
            trace,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<MeterTestFlowStepResult> ExecuteWorkflowStepAsync(
        MeterTestWorkflowKind workflow,
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        long startTicks = Environment.TickCount64;
        KeyRecoveryRunState run = GetOrCreateRun(workflow, station);
        if (run.Completed)
        {
            return new MeterTestFlowStepResult(
                run.Success,
                run.Message,
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        try
        {
            if (workflow == MeterTestWorkflowKind.KeyUpdate)
            {
                await ExecuteKeyUpdateFlowAsync(context, station, run, trace, cancellationToken).ConfigureAwait(false);
                run.Message = "密钥更新十三步流程完成，更新后业务验证通过。";
            }
            else
            {
                await ExecuteFullFlowAsync(context, station, run, trace, cancellationToken).ConfigureAwait(false);
                run.Message = "密钥恢复六步流程完成，恢复后对称密钥版本为全0。";
            }

            run.Completed = true;
            run.Success = true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            run.Completed = true;
            run.Success = false;
            run.Message = workflow == MeterTestWorkflowKind.KeyUpdate
                ? $"密钥更新失败：{ex.Message}"
                : $"密钥恢复失败：{ex.Message}";
            trace($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {run.Message}");
        }

        return new MeterTestFlowStepResult(
            run.Success,
            run.Message,
            Math.Max(0, Environment.TickCount64 - startTicks));
    }

    private async Task ExecuteFullFlowAsync(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        KeyRecoveryRunState run,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        await ExecuteRecoveryPhaseAsync(
            context,
            station,
            run,
            TestItemName,
            trace,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<RecoveryPhaseData> ExecuteRecoveryPhaseAsync(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        KeyRecoveryRunState run,
        string testItemName,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        trace(
            $"{Separator}\r\n"
            + $"测试项目：{testItemName}  测试开始  表位号：{station.MeterAddress}\r\n"
            + $"工位：{station.StationNo}\r\n"
            + $"{Separator}");

        MeterEsamData before = await ReadEsamStateAsync(
            station,
            "读取安全模式参数",
            trace,
            cancellationToken).ConfigureAwait(false);
        run.Esams.Add(before);
        databaseService.SaveMeterEsamData(before);

        SessionData beforeRecoverySession = await EstablishSessionAsync(
            station,
            before,
            "建立应用连接",
            trace,
            cancellationToken).ConfigureAwait(false);

        await ExecuteKeyTransferAsync(
            context,
            station,
            before,
            beforeRecoverySession,
            keyState: 0,
            operationName: "密钥恢复",
            piid: "06",
            trace,
            cancellationToken).ConfigureAwait(false);

        MeterEsamData after = await ReadEsamStateAsync(
            station,
            "读取ESAM信息",
            trace,
            cancellationToken,
            includeSecurityMode: false,
            secureModeOverride: before.SecureModeEnabled).ConfigureAwait(false);
        run.Esams.Add(after);
        databaseService.SaveMeterEsamData(after);
        if (!IsAllZero(after.SymmetricKeyVersion))
            throw new InvalidOperationException($"恢复后对称密钥版本不是全0：{after.SymmetricKeyVersion}。");

        SessionData afterRecoverySession = await EstablishSessionAsync(
            station,
            after,
            "建立应用连接",
            trace,
            cancellationToken).ConfigureAwait(false);
        trace($"密钥恢复流程结束：恢复后密钥版本={after.SymmetricKeyVersion}。");
        return new RecoveryPhaseData(before, after, afterRecoverySession);
    }

    private async Task ExecuteKeyUpdateFlowAsync(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        KeyRecoveryRunState run,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        RecoveryPhaseData recovery = await ExecuteRecoveryPhaseAsync(
            context,
            station,
            run,
            KeyUpdateTestItemName,
            trace,
            cancellationToken).ConfigureAwait(false);

        string keyUpdatePiid = SGCCTools.GeneratePiid();
        await ExecuteKeyTransferAsync(
            context,
            station,
            recovery.After,
            recovery.AfterRecoverySession,
            keyState: 1,
            operationName: "密钥更新",
            piid: keyUpdatePiid,
            trace,
            cancellationToken).ConfigureAwait(false);

        MeterEsamData updated = await ReadEsamStateAsync(
            station,
            "读取ESAM信息（更新后）",
            trace,
            cancellationToken,
            generatePiid: true,
            includeSecurityMode: false,
            secureModeOverride: recovery.After.SecureModeEnabled).ConfigureAwait(false);
        run.Esams.Add(updated);
        databaseService.SaveMeterEsamData(updated);
        if (string.IsNullOrWhiteSpace(updated.SymmetricKeyVersion) ||
            !updated.SymmetricKeyVersion.StartsWith("7FFFFF", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"更新后对称密钥版本校验失败，实际值={updated.SymmetricKeyVersion}，期望以7FFFFF开头。");
        }

        SessionData updatedSession = await EstablishSessionAsync(
            station,
            updated,
            "建立应用连接（更新后）",
            trace,
            cancellationToken,
            ConnectApduProfile.Updated).ConfigureAwait(false);

        await ReadMeterNumberBeforeControlAsync(
            station,
            context.SubItem.TimeoutMs,
            trace,
            cancellationToken).ConfigureAwait(false);

        await ExecuteMeterControlAsync(
            context,
            station,
            updated,
            updatedSession,
            taskType: 8,
            operationName: "跳闸",
            taskPrefix: "800081000101020451F20502011100120000030001",
            expectedTaskPrefix: "8701{PIID}8000810000000001",
            trace,
            cancellationToken).ConfigureAwait(false);

        await ExecuteMeterControlAsync(
            context,
            station,
            updated,
            updatedSession,
            taskType: 6,
            operationName: "合闸",
            taskPrefix: "800082000101020251F2050201160101",
            expectedTaskPrefix: "8701{PIID}8000820000000001",
            trace,
            cancellationToken).ConfigureAwait(false);

        await ExecuteBroadcastFreezeAsync(
            context,
            station,
            updated,
            trace,
            cancellationToken).ConfigureAwait(false);

        trace(
            $"{Separator}\r\n延时等待 3 秒\r\n{Separator}");
        await Task.Delay(BroadcastWaitAfterSendMs, cancellationToken).ConfigureAwait(false);
        trace("延时等待结束。");

        await ReadCurrentMeterDateTimeAsync(
            station,
            context.SubItem.TimeoutMs,
            trace,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteKeyTransferAsync(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        MeterEsamData esam,
        SessionData session,
        int keyState,
        string operationName,
        string piid,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        await EnsureEncryptionConnectedAsync(trace, cancellationToken).ConfigureAwait(false);
        string meterNo = BuildMeterNo(station.MeterAddress);
        byte[] sid = new byte[128];
        byte[] attachData = new byte[256];
        byte[] terminalKeyData = new byte[4096];
        byte[] mac = new byte[128];
        WinSocketServer.DllResult keyDataResult;
        try
        {
            keyDataResult = winSocketServer.CallObj_Meter_Formal_GetTrmKeyData(
                keyState,
                esam.EsamNumber,
                session.SessionKey,
                meterNo,
                "00",
                sid,
                attachData,
                terminalKeyData,
                mac);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{operationName}接口调用异常：{ex.Message}", ex);
        }

        trace(FormatEncryptionCall(
            $"调用 Obj_Meter_Formal_GetTrmKeyData 接口（{operationName}）",
            ("iKeyState", keyState.ToString(CultureInfo.InvariantCulture)),
            ("cESAMID", esam.EsamNumber),
            ("cSessionKey", session.SessionKey),
            ("InMeterNum", meterNo),
            ("cKeyType", "00"),
            ("cOutSID", DecodeOutputForLog(sid)),
            ("cOutAttachData", DecodeOutputForLog(attachData)),
            ("cOutTrmKeyData", DecodeOutputForLog(terminalKeyData)),
            ("cOutMAC", DecodeOutputForLog(mac)),
            ("iRes", keyDataResult.Code.ToString(CultureInfo.InvariantCulture))));
        if (!keyDataResult.Success)
            throw new InvalidOperationException($"{operationName}数据生成失败，错误码={keyDataResult.Code}。");

        string keyData = DecodeOutput(terminalKeyData, $"{operationName}数据");
        string keySid = DecodeOutput(sid, $"{operationName}SID");
        string keyAttachData = DecodeOutput(attachData, $"{operationName}附加数据");
        string keyMac = DecodeOutput(mac, $"{operationName}MAC");
        string taskData = BuildKeyTransferTaskData(piid, keyData, keySid, keyAttachData, keyMac);

        byte[] outSid = new byte[128];
        byte[] outAttachData = new byte[256];
        byte[] outData = new byte[4096];
        byte[] outMac = new byte[128];
        await EnsureEncryptionConnectedAsync(trace, cancellationToken).ConfigureAwait(false);
        WinSocketServer.DllResult setDataResult = winSocketServer.CallObj_Meter_Formal_GetMeterSetData(
            3,
            esam.EsamNumber,
            session.SessionKey,
            taskData,
            outSid,
            outAttachData,
            outData,
            outMac);
        trace(FormatEncryptionCall(
            $"调用 Obj_Meter_Formal_GetMeterSetData 接口（{operationName}）",
            ("iOperateMode", "3"),
            ("cESAMNO", esam.EsamNumber),
            ("cSessionKey", session.SessionKey),
            ("cTaskData", taskData),
            ("cOutSID", DecodeOutputForLog(outSid)),
            ("cOutAttachData", DecodeOutputForLog(outAttachData)),
            ("cOutData", DecodeOutputForLog(outData)),
            ("cOutMAC", DecodeOutputForLog(outMac)),
            ("iRes", setDataResult.Code.ToString(CultureInfo.InvariantCulture))));
        if (!setDataResult.Success)
            throw new InvalidOperationException($"{operationName}安全传输封装失败，错误码={setDataResult.Code}。");

        string linkFrameData = BuildLinkFrameData(outSid, outAttachData, outData, outMac);
        trace($"链路分帧数据：{linkFrameData}");
        string response = await SendSegmentedRecoveryAsync(
            station,
            linkFrameData,
            context.SubItem.TimeoutMs,
            trace,
            cancellationToken,
            operationName).ConfigureAwait(false);

        string expectedResponse = keyState == 1
            ? $"8701{NormalizeHex(piid)}F100070000000000"
            : string.Empty;
        await VerifyMeterBusinessResponseAsync(
            response,
            esam,
            session,
            verifyKeyState: 0,
            expectedResponse,
            operationName,
            trace,
            cancellationToken).ConfigureAwait(false);
    }

    private static string BuildKeyTransferTaskData(
        string piid,
        string keyData,
        string sid,
        string attachData,
        string mac)
    {
        return $"0701{NormalizeHex(piid)}F1000700020209{EncodeLength(keyData)}{keyData}"
            + $"5E{sid}0B{attachData}04{mac}00";
    }

    private async Task<MeterEsamData> ReadEsamStateAsync(
        StationCommunicationConfig station,
        string stepName,
        Action<string> trace,
        CancellationToken cancellationToken,
        bool generatePiid = false,
        bool includeSecurityMode = true,
        bool? secureModeOverride = null)
    {
        bool secureMode;
        if (includeSecurityMode)
        {
            string securityModeApdu = generatePiid
                ? $"0501{SGCCTools.GeneratePiid()}F101020000"
                : SecurityModeApdu;
            string securityResponse = await SendOne485RequestAsync(
                station,
                Build698Frame(station.MeterAddress, securityModeApdu),
                stepName,
                GetConfiguredTimeoutMs(),
                trace,
                cancellationToken).ConfigureAwait(false);
            secureMode = ParseSecurityMode(securityResponse);

            // 部分485转换器需要在上一帧应答后留出固定处理间隔，避免连续请求丢帧。
            await Task.Delay(MeterRequestIntervalMs, cancellationToken).ConfigureAwait(false);
        }
        else if (secureModeOverride.HasValue)
        {
            secureMode = secureModeOverride.Value;
        }
        else
        {
            throw new InvalidOperationException($"{stepName}未读取安全模式且没有可复用的安全模式状态。");
        }

        string esamReadApdu = generatePiid
            ? $"0502{SGCCTools.GeneratePiid()}03F1000200F1000400F100070000"
            : EsamReadApdu;
        string esamResponse = await SendOne485RequestAsync(
            station,
            Build698Frame(station.MeterAddress, esamReadApdu),
            includeSecurityMode ? "读取ESAM信息" : stepName,
            GetConfiguredTimeoutMs(),
            trace,
            cancellationToken).ConfigureAwait(false);
        EsamPayload payload = ParseEsamPayload(esamResponse);
        return new MeterEsamData(
            station.StationNo,
            NormalizeHex(station.MeterAddress),
            secureMode,
            payload.EsamNumber,
            payload.SymmetricKeyVersion,
            payload.ApplicationCounter,
            payload.ActiveReportCounter,
            payload.BroadcastCounter,
            DateTime.Now);
    }

    private async Task<SessionData> EstablishSessionAsync(
        StationCommunicationConfig station,
        MeterEsamData esam,
        string stepName,
        Action<string> trace,
        CancellationToken cancellationToken,
        ConnectApduProfile connectProfile = ConnectApduProfile.Recovery)
    {
        await EnsureEncryptionConnectedAsync(trace, cancellationToken).ConfigureAwait(false);
        int keyState = IsAllZero(esam.SymmetricKeyVersion) ? 0 : 1;
        // InitSession 和 VerifySession 必须使用同一个分散因子；iKeyState=1 时不能回退为 ESAM 序列号。
        string sessionDiv = BuildSessionDiv(keyState, station.MeterAddress, esam.EsamNumber);
        string asctr = ((ulong)esam.ApplicationCounter + 1).ToString("X8", CultureInfo.InvariantCulture);
        byte[] randHost = new byte[128];
        byte[] sessionInit = new byte[256];
        byte[] sign = new byte[128];
        WinSocketServer.DllResult initResult = winSocketServer.CallObj_Meter_Formal_InitSession(
            keyState,
            sessionDiv,
            asctr,
            "01",
            randHost,
            sessionInit,
            sign);
        trace(FormatEncryptionCall(
            "调用 Obj_Meter_Formal_InitSession 接口",
            ("iKeyState", keyState.ToString(CultureInfo.InvariantCulture)),
            ("cDiv", sessionDiv),
            ("cASCTR", asctr),
            ("cFLG", "01"),
            ("cOutRandHost", DecodeOutputForLog(randHost)),
            ("cOutSessionInit", DecodeOutputForLog(sessionInit)),
            ("cOutMac", DecodeOutputForLog(sign)),
            ("iRes", initResult.Code.ToString(CultureInfo.InvariantCulture))));
        if (!initResult.Success)
            throw new InvalidOperationException($"{stepName}会话初始化失败，错误码={initResult.Code}。");

        string randHostHex = DecodeOutput(randHost, "主站随机数");
        string sessionInitHex = DecodeOutput(sessionInit, "会话初始化数据");
        string signHex = DecodeOutput(sign, "客户机签名");
        string piid = SGCCTools.GeneratePiid();
        string connectApdu = BuildConnectApdu(piid, sessionInitHex, signHex, connectProfile);
        trace($"建立应用连接请求PIID：{piid}");
        string response = await SendOne485RequestAsync(
            station,
            Build698Frame(station.MeterAddress, connectApdu),
            stepName,
            GetConfiguredTimeoutMs(),
            trace,
            cancellationToken).ConfigureAwait(false);
        (string sessionData, string responseSign) = ParseSessionResponse(response);
        byte[] sessionKey = new byte[1024];
        await EnsureEncryptionConnectedAsync(trace, cancellationToken).ConfigureAwait(false);
        string verifyDiv = BuildSessionDiv(keyState, station.MeterAddress, esam.EsamNumber);
        if (!string.Equals(sessionDiv, verifyDiv, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"VerifySession分散因子不一致：InitSession={sessionDiv}，VerifySession={verifyDiv}。");

        WinSocketServer.DllResult verifyResult = winSocketServer.CallObj_Meter_Formal_VerifySession(
            keyState,
            verifyDiv,
            randHostHex,
            sessionData,
            responseSign,
            sessionKey);
        trace(FormatEncryptionCall(
            "调用 Obj_Meter_Formal_VerifySession 接口",
            ("iKeyState", keyState.ToString(CultureInfo.InvariantCulture)),
            ("cDiv", verifyDiv),
            ("cRandHost", randHostHex),
            ("cSessionData", sessionData),
            ("cSign", responseSign),
            ("cOutSessionKey", DecodeOutputForLog(sessionKey)),
            ("iRes", verifyResult.Code.ToString(CultureInfo.InvariantCulture))));
        if (!verifyResult.Success)
            throw new InvalidOperationException($"{stepName}会话验证失败，错误码={verifyResult.Code}。");

        string sessionKeyHex = DecodeOutput(sessionKey, "会话密钥");
        trace($"{stepName}完成：iKeyState={keyState}，ESAM={esam.EsamNumber}，ASCTR={asctr}。");
        return new SessionData(randHostHex, sessionKeyHex);
    }

    private async Task EnsureEncryptionConnectedAsync(Action<string> trace, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (encryptionConnected)
            return;

        WinSocketServerConfig config = ReadWinSocketServerConfig();
        string timeoutText = config.TimeoutMs.ToString(CultureInfo.InvariantCulture);

        WinSocketServer.DllResult connectResult = await Task.Run(
            () => winSocketServer.ConnectDeviceEx(config.Ip, config.Port, timeoutText),
            cancellationToken).ConfigureAwait(false);
        trace(FormatEncryptionCall(
            "ConnectDevice",
            ("ip", config.Ip),
            ("port", config.Port),
            ("ctime", timeoutText),
            ("iRes", connectResult.Code.ToString(CultureInfo.InvariantCulture))));

        if (!connectResult.Success)
            throw new InvalidOperationException($"加密机连接失败，错误码={connectResult.Code}。");

        encryptionConnected = true;
        trace($"加密机连接确认成功：{config.Ip}:{config.Port}。");
    }

    private async Task<string> SendOne485RequestAsync(
        StationCommunicationConfig station,
        string request,
        string description,
        int timeoutMs,
        Action<string> trace,
        CancellationToken cancellationToken,
        string? plainApdu = null)
    {
        trace(
            $"{Separator}\r\n"
            + $"{description}\r\n"
            + $"{Separator}\r\n"
            + $"发送时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\r\n"
            + $"明文APDU：{FormatHex(plainApdu is null ? GetApdu(request) : HexToBytes(plainApdu))}\r\n"
            + $"发送内容：{FormatHex(request)}");

        string response = await stationTcpSessionService.SendRequestAsync(
                station,
                request,
                description,
                Math.Max(GetConfiguredTimeoutMs(), timeoutMs),
                trace,
                cancellationToken,
                logTraffic: false)
            .ConfigureAwait(false);

        trace(
            $"接收时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\r\n"
            + $"接收内容：{FormatHex(response)}");
        return response;
    }

    private async Task SendBroadcast485RequestAsync(
        StationCommunicationConfig station,
        string request,
        string plainApdu,
        string description,
        int timeoutMs,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        trace(
            $"{Separator}\r\n"
            + $"{description}\r\n"
            + $"{Separator}\r\n"
            + $"发送时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\r\n"
            + $"明文APDU：{FormatHex(HexToBytes(plainApdu))}\r\n"
            + $"发送内容：{FormatHex(request)}");

        await stationTcpSessionService.SendWithoutResponseAsync(
                station,
                request,
                description,
                Math.Max(GetConfiguredTimeoutMs(), timeoutMs),
                trace,
                cancellationToken,
                logTraffic: false)
            .ConfigureAwait(false);

        trace("接收内容：广播报文按协议不等待电表响应，发送成功即判定合格。");
    }

    private async Task ReadMeterNumberBeforeControlAsync(
        StationCommunicationConfig station,
        int timeoutMs,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        string piid = SGCCTools.GeneratePiid();
        string apdu = $"{MeterNumberReadApduPrefix}{piid}4002020000";
        string response = await SendOne485RequestAsync(
            station,
            Build698Frame(station.MeterAddress, apdu),
            "读表号",
            timeoutMs,
            trace,
            cancellationToken).ConfigureAwait(false);

        byte[] meterNumber = FindTypedValue(GetApdu(response), "40020200", 0x09, 6);
        string actualAddress = ToHex(meterNumber);
        string expectedAddress = NormalizeHex(station.MeterAddress);
        if (!string.Equals(actualAddress, expectedAddress, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"读表号校验失败，资产地址={expectedAddress}，电表返回={actualAddress}。");
        }

        trace($"表号：[ {actualAddress} ]");
    }

    private async Task ExecuteMeterControlAsync(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        MeterEsamData esam,
        SessionData session,
        int taskType,
        string operationName,
        string taskPrefix,
        string expectedTaskPrefix,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        string piid = SGCCTools.GeneratePiid();
        string dateTimeFields = BuildMeterDateTimeFields(DateTime.Now);
        string taskData = $"0701{piid}{NormalizeHex(taskPrefix)}{dateTimeFields}";
        byte[] sid = new byte[128];
        byte[] attachData = new byte[256];
        byte[] outData = new byte[4096];
        byte[] mac = new byte[128];

        await EnsureEncryptionConnectedAsync(trace, cancellationToken).ConfigureAwait(false);
        WinSocketServer.DllResult result;
        try
        {
            result = winSocketServer.CallObj_Meter_Formal_GetSessionData(
                3,
                esam.EsamNumber,
                session.SessionKey,
                taskType,
                taskData,
                sid,
                attachData,
                outData,
                mac);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"{operationName}接口调用异常：{ex.Message}", ex);
        }

        trace(FormatEncryptionCall(
            $"Obj_Meter_GetSessionData（{operationName}）",
            ("cOperateMode", "3"),
            ("cESAMID", esam.EsamNumber),
            ("cSessionKey", session.SessionKey),
            ("cTaskType", taskType.ToString(CultureInfo.InvariantCulture)),
            ("cTaskData", taskData),
            ("cOutSID", DecodeOutputForLog(sid)),
            ("cOutAttachData", DecodeOutputForLog(attachData)),
            ("cOutData", DecodeOutputForLog(outData)),
            ("cOutMAC", DecodeOutputForLog(mac)),
            ("iRes", result.Code.ToString(CultureInfo.InvariantCulture))));
        if (!result.Success)
            throw new InvalidOperationException($"{operationName}数据生成失败，错误码={result.Code}。");

        string outDataHex = DecodeOutput(outData, $"{operationName}加密数据");
        string sidHex = DecodeOutput(sid, $"{operationName}SID");
        string attachHex = DecodeOutput(attachData, $"{operationName}附加数据");
        string macHex = DecodeOutput(mac, $"{operationName}MAC");
        string linkApdu = BuildShortBusinessLinkFrameData(outDataHex, sidHex, attachHex, macHex);
        string response = await SendOne485RequestAsync(
            station,
            Build698Frame(station.MeterAddress, linkApdu),
            operationName,
            context.SubItem.TimeoutMs,
            trace,
            cancellationToken,
            plainApdu: taskData).ConfigureAwait(false);

        string expectedResponse = expectedTaskPrefix
            .Replace("{PIID}", piid, StringComparison.Ordinal)
            + dateTimeFields;
        await VerifyMeterBusinessResponseAsync(
            response,
            esam,
            session,
            verifyKeyState: 1,
            expectedResponse,
            operationName,
            trace,
            cancellationToken).ConfigureAwait(false);
        trace($"{operationName} 成功！");
    }

    private async Task ExecuteBroadcastFreezeAsync(
        SelectedSubItemContext context,
        StationCommunicationConfig station,
        MeterEsamData esam,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        string piid = BroadcastPiid;
        string broadcastTaskData = $"0701{piid}{BroadcastTaskSuffix}";
        string agseq = BroadcastAgseq;
        byte[] sid = new byte[128];
        byte[] attachData = new byte[256];
        byte[] outData = new byte[4096];
        byte[] mac = new byte[128];

        await EnsureEncryptionConnectedAsync(trace, cancellationToken).ConfigureAwait(false);
        WinSocketServer.DllResult result;
        try
        {
            result = winSocketServer.CallObj_Meter_Formal_GetGrpBrdCstData(
                1,
                1,
                esam.EsamNumber,
                BroadcastDiv,
                agseq,
                broadcastTaskData,
                sid,
                attachData,
                outData,
                mac);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"广播冻结接口调用异常：{ex.Message}", ex);
        }

        trace(FormatEncryptionCall(
            "Obj_Meter_GetGrpBrdCstData",
            ("iKeyState", "1"),
            ("cOperateMode", "1"),
            ("cESAMID", esam.EsamNumber),
            ("cDIV", BroadcastDiv),
            ("AGSEQ", agseq),
            ("cBrdCstData", broadcastTaskData),
            ("cOutSID", DecodeOutputForLog(sid)),
            ("cOutAttachData", DecodeOutputForLog(attachData)),
            ("cOutData", DecodeOutputForLog(outData)),
            ("cOutMAC", DecodeOutputForLog(mac)),
            ("iRes", result.Code.ToString(CultureInfo.InvariantCulture))));
        if (!result.Success)
            throw new InvalidOperationException($"广播冻结数据生成失败，错误码={result.Code}。");

        // 广播封装按协议使用 cBrdCstData，但仍需校验加密机返回的 cOutData，避免无效HEX被带入日志或后续流程。
        _ = DecodeOutput(outData, "广播冻结加密数据");
        string sidHex = DecodeOutput(sid, "广播冻结SID");
        string attachHex = DecodeOutput(attachData, "广播冻结附加数据");
        string macHex = DecodeOutput(mac, "广播冻结MAC");
        string linkApdu = BuildBroadcastLinkFrameData(broadcastTaskData, sidHex, attachHex, macHex);
        string request = BuildBroadcast698Frame(linkApdu);
        await SendBroadcast485RequestAsync(
            station,
            request,
            broadcastTaskData,
            "广播冻结",
            context.SubItem.TimeoutMs,
            trace,
            cancellationToken).ConfigureAwait(false);
        trace("广播冻结发送成功：无响应按协议合格。");
    }

    private async Task ReadCurrentMeterDateTimeAsync(
        StationCommunicationConfig station,
        int timeoutMs,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        string piid = SGCCTools.GeneratePiid();
        string apdu = $"{CurrentDateTimeReadApduPrefix}{piid}4000020000";
        string response = await SendOne485RequestAsync(
            station,
            Build698Frame(station.MeterAddress, apdu),
            "读取当前日期和时间",
            timeoutMs,
            trace,
            cancellationToken).ConfigureAwait(false);
        DateTime meterDateTime = ParseMeterDateTime(response);
        trace($"当前日期和时间：[{meterDateTime:yyyy-MM-dd HH:mm:ss}]");
    }

    private static string BuildMeterDateTimeFields(DateTime value)
    {
        return $"{value.Year:X4}{value.Month:X2}{value.Day:X2}{value.Hour:X2}"
            + $"{value.Minute:X2}{value.Second:X2}010005";
    }

    private static DateTime ParseMeterDateTime(string response)
    {
        byte[] apdu = GetApdu(response);
        byte[] oad = HexToBytes("40000200");
        for (int i = 0; i + oad.Length + 11 <= apdu.Length; i++)
        {
            if (!apdu.AsSpan(i, oad.Length).SequenceEqual(oad))
                continue;

            int cursor = i + oad.Length;
            if (apdu[cursor] != 0x01 || apdu[cursor + 1] != 0x1C)
                continue;

            cursor += 2;
            int year = (apdu[cursor] << 8) | apdu[cursor + 1];
            int month = apdu[cursor + 2];
            int day = apdu[cursor + 3];
            int hour = apdu[cursor + 4];
            int minute = apdu[cursor + 5];
            int second = apdu[cursor + 6];
            try
            {
                return new DateTime(year, month, day, hour, minute, second);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidOperationException(
                    $"电表返回日期时间字段非法：{year:D4}-{month:D2}-{day:D2} {hour:D2}:{minute:D2}:{second:D2}。",
                    ex);
            }
        }

        throw new InvalidOperationException("读取当前日期和时间响应中未找到有效的OAD 40000200时间字段。");
    }

    private int GetConfiguredTimeoutMs() => ReadWinSocketServerConfig().TimeoutMs;

    private static WinSocketServerConfig ReadWinSocketServerConfig()
    {
        string configPath = GetConfigPath();
        string ip = Confighelper.ReadIni(
            WinSocketServerSection,
            WinSocketIpKey,
            string.Empty,
            IniValueBufferSize,
            configPath).Trim();
        string port = Confighelper.ReadIni(
            WinSocketServerSection,
            WinSocketPortKey,
            string.Empty,
            IniValueBufferSize,
            configPath).Trim();
        string timeoutText = Confighelper.ReadIni(
            WinSocketServerSection,
            KeyRecoveryTimeoutKey,
            string.Empty,
            IniValueBufferSize,
            configPath).Trim();

        if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(port))
            throw new InvalidOperationException("XCKJcomfig.ini 未配置 [WinSocketServer] 的 ip 或 port，已停止密钥恢复流程。");
        if (!int.TryParse(timeoutText, out int timeoutMs) || timeoutMs <= 0)
            throw new InvalidOperationException("XCKJcomfig.ini 的 [WinSocketServer] keyRecoveryTimeoutMs 必须是大于0的整数。");

        return new WinSocketServerConfig(ip, port, timeoutMs);
    }

    private KeyRecoveryRunState GetOrCreateRun(
        MeterTestWorkflowKind workflow,
        StationCommunicationConfig station)
    {
        lock (syncRoot)
        {
            (MeterTestWorkflowKind Workflow, int StationNo) key = (workflow, station.StationNo);
            if (!runs.TryGetValue(key, out KeyRecoveryRunState? state))
            {
                state = new KeyRecoveryRunState();
                runs[key] = state;
            }

            return state;
        }
    }

    private static string Build698Frame(string meterAddress, string apdu, string control = "43")
    {
        string address = NormalizeHex(meterAddress);
        if (address.Length != 12)
            throw new InvalidOperationException("密钥恢复要求电表地址为6字节HEX。");
        byte[] addressBytes = HexToBytes(address);
        Array.Reverse(addressBytes);
        return SGCCTools.BytesToSGCCMessage("68", control, "05", ToHex(addressBytes), "A0", apdu, "16");
    }

    private static string BuildBroadcast698Frame(string apdu)
    {
        return SGCCTools.BytesToSGCCMessage("68", "43", "C0", "AA", "A0", apdu, "16");
    }

    private static string BuildConnectApdu(
        string piid,
        string sessionInit,
        string sign,
        ConnectApduProfile profile)
    {
        string normalizedPiid = NormalizeHex(piid);
        if (normalizedPiid.Length != 2)
            throw new InvalidOperationException("建立应用连接PIID必须是1字节HEX。");

        string normalizedSessionInit = NormalizeHex(sessionInit);
        string normalizedSign = NormalizeHex(sign);
        const int ZeroPaddingAfterEsamCommand = 13;
        string fixedPrefix = profile == ConnectApduProfile.Updated
            ? "0016FFFFFFFFC0000000FFFEC4"
            : "0014FFFFFFFFC0000000FFFEC4";
        string fixedSuffix = profile == ConnectApduProfile.Updated
            ? "020002000107D000001C2002"
            : "020002000107D00001518002";

        return NormalizeHex(
            $"02{normalizedPiid}{fixedPrefix}"
            + new string('0', ZeroPaddingAfterEsamCommand * 2)
            + $"{fixedSuffix}20{normalizedSessionInit}04{normalizedSign}00");
    }

    private static string GetConfigPath() => Path.Combine(AppContext.BaseDirectory, "XCKJcomfig.ini");

    private static bool ParseSecurityMode(string response)
    {
        byte[] apdu = GetApdu(response);
        for (int i = 0; i + 7 < apdu.Length; i++)
        {
            if (apdu[i] == 0xF1 && apdu[i + 1] == 0x01 && apdu[i + 2] == 0x02 && apdu[i + 3] == 0x00 &&
                apdu[i + 4] == 0x01 && apdu[i + 5] == 0x16)
            {
                return apdu[i + 6] != 0;
            }
        }

        throw new InvalidOperationException("读取安全模式参数响应中未找到 OAD F1010200。");
    }

    private static EsamPayload ParseEsamPayload(string response)
    {
        byte[] apdu = GetApdu(response);
        byte[] serial = FindTypedValue(apdu, "F1000200", 0x09, 8);
        byte[] version = FindTypedValue(apdu, "F1000400", 0x09, 16);
        byte[] counter = FindTypedValue(apdu, "F1000700", 0x02, 12);
        if (serial.Length != 8 || version.Length != 16 || counter.Length < 12)
            throw new InvalidOperationException("读取 ESAM 信息响应字段不完整。");

        return new EsamPayload(
            ToHex(serial),
            ToHex(version),
            ReadUInt32(counter, 0),
            ReadUInt32(counter, 4),
            ReadUInt32(counter, 8));
    }

    private static byte[] FindTypedValue(byte[] apdu, string oad, byte type, int length)
    {
        byte[] oadBytes = HexToBytes(oad);
        for (int i = 0; i + oadBytes.Length + 3 < apdu.Length; i++)
        {
            if (!apdu.AsSpan(i, oadBytes.Length).SequenceEqual(oadBytes))
                continue;
            int cursor = i + oadBytes.Length;
            if (cursor >= apdu.Length || apdu[cursor] != 0x01)
                continue;
            cursor++;
            if (cursor >= apdu.Length || apdu[cursor++] != type)
                continue;
            int actualLength = type == 0x02 && length == 12 && cursor < apdu.Length ? 3 : 1;
            if (type == 0x09)
                actualLength = apdu[cursor++];
            if (type == 0x02)
            {
                if (cursor >= apdu.Length || apdu[cursor++] != 0x03)
                    continue;
                List<byte> values = new();
                for (int valueIndex = 0; valueIndex < 3; valueIndex++)
                {
                    if (cursor + 5 > apdu.Length || apdu[cursor++] != 0x06)
                        return Array.Empty<byte>();
                    values.AddRange(apdu.AsSpan(cursor, 4).ToArray());
                    cursor += 4;
                }
                return values.ToArray();
            }

            if (cursor + actualLength <= apdu.Length)
                return apdu.AsSpan(cursor, actualLength).ToArray();
        }

        return Array.Empty<byte>();
    }

    private static (string SessionData, string Sign) ParseSessionResponse(string response)
    {
        byte[] apdu = GetApdu(response);
        const int sessionDataLength = 48;
        const int signLength = 4;
        const int trailerLength = 2;
        if (apdu.Length < sessionDataLength + 1 + signLength + trailerLength)
            throw new InvalidOperationException("建立应用连接响应长度不足，无法提取会话数据。");

        int dataEnd = apdu.Length - trailerLength;
        if (apdu[dataEnd] != 0x00 || apdu[dataEnd + 1] != 0x00)
            throw new InvalidOperationException("建立应用连接响应尾部缺少00 00结束标识。");

        int signLengthIndex = dataEnd - signLength - 1;
        if (signLengthIndex < sessionDataLength || apdu[signLengthIndex] != signLength)
        {
            string tail = ToHex(apdu, Math.Max(0, apdu.Length - Math.Min(apdu.Length, 16)), Math.Min(apdu.Length, 16));
            throw new InvalidOperationException(
                $"建立应用连接响应格式不符合会话协商结构：APDU长度={apdu.Length}字节，"
                + $"期望签名长度标识位置={signLengthIndex}，实际尾部={tail}。");
        }

        int sessionDataIndex = signLengthIndex - sessionDataLength;
        string sessionData = ToHex(apdu, sessionDataIndex, sessionDataLength);
        string sign = ToHex(apdu, signLengthIndex + 1, signLength);
        return (sessionData, sign);
    }

    private static byte[] GetApdu(string response)
    {
        IReadOnlyList<string> frames = SGCCTools.ExtractSgcc698Frames(response);
        string frame = frames.LastOrDefault() ?? throw new InvalidOperationException("未收到完整 698 响应帧。");
        byte[] bytes = HexToBytes(frame);
        int serverAddressLength = (bytes[4] & 0x0F) + 1;
        int apduIndex = 5 + serverAddressLength + 1 + 2;
        int fcsIndex = bytes.Length - 3;
        if (apduIndex >= fcsIndex)
            throw new InvalidOperationException("698 响应帧 APDU 位置异常。");
        return bytes.AsSpan(apduIndex, fcsIndex - apduIndex).ToArray();
    }

    private static string BuildLinkFrameData(byte[] sid, byte[] attachData, byte[] data, byte[] mac)
    {
        string sidHex = DecodeOutput(sid, "安全传输SID");
        string attachHex = DecodeOutput(attachData, "安全传输附加数据");
        string dataHex = DecodeOutput(data, "安全传输密文");
        string macHex = DecodeOutput(mac, "安全传输MAC");
        if (string.IsNullOrWhiteSpace(dataHex))
            throw new InvalidOperationException("加密机未返回密钥传输密文数据。");

        // 密钥传输的链路数据格式与普通安全传输APDU不同，必须按参考协议拼接。
        return $"1001{EncodeLength(dataHex)}{dataHex}00{sidHex}02{attachHex}04{macHex}";
    }

    private static string BuildShortBusinessLinkFrameData(
        string data,
        string sid,
        string attachData,
        string mac)
    {
        int length = NormalizeHex(data).Length / 2;
        if (length == 0 || length > byte.MaxValue)
            throw new InvalidOperationException($"短业务密文长度不符合协议要求：{length}字节。");

        return $"1001{length:X2}{NormalizeHex(data)}00{NormalizeHex(sid)}02{NormalizeHex(attachData)}04{NormalizeHex(mac)}";
    }

    private static string BuildBroadcastLinkFrameData(
        string plainTaskData,
        string sid,
        string attachData,
        string mac)
    {
        int length = NormalizeHex(plainTaskData).Length / 2;
        if (length == 0 || length > byte.MaxValue)
            throw new InvalidOperationException($"广播冻结明文长度不符合协议要求：{length}字节。");

        return $"1000{length:X2}{NormalizeHex(plainTaskData)}00{NormalizeHex(sid)}02{NormalizeHex(attachData)}04{NormalizeHex(mac)}";
    }

    private static string EncodeLength(string hexData)
    {
        int length = NormalizeHex(hexData).Length / 2;
        if (length <= byte.MaxValue)
            return $"02{length:X2}";
        if (length <= ushort.MaxValue)
            return $"82{length:X4}";

        throw new InvalidOperationException($"HEX 数据长度超出协议限制：{length} 字节。");
    }

    private async Task<string> SendSegmentedRecoveryAsync(
        StationCommunicationConfig station,
        string linkFrameData,
        int timeoutMs,
        Action<string> trace,
        CancellationToken cancellationToken,
        string operationName)
    {
        byte[] payload = HexToBytes(linkFrameData);
        if (payload.Length == 0)
            throw new InvalidOperationException($"{operationName}链路分帧数据为空。");

        int frameCount = (payload.Length + SegmentedPayloadBytes - 1) / SegmentedPayloadBytes;
        string lastResponse = string.Empty;
        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int offset = frameIndex * SegmentedPayloadBytes;
            int count = Math.Min(SegmentedPayloadBytes, payload.Length - offset);
            string frameType = frameIndex == frameCount - 1 ? "40" : frameIndex == 0 ? "00" : "C0";
            string frameControl = $"{frameIndex:X2}{frameType}";
            string apdu = frameControl + ToHex(payload, offset, count);
            string request = Build698Frame(station.MeterAddress, apdu, "63");

            lastResponse = await SendOne485RequestAsync(
                station,
                request,
                $"{operationName}分帧 {frameIndex + 1}/{frameCount}",
                timeoutMs,
                trace,
                cancellationToken).ConfigureAwait(false);
            ValidateSegmentResponse(lastResponse, frameIndex, frameCount, operationName);

            if (frameIndex < frameCount - 1)
                await Task.Delay(MeterRequestIntervalMs, cancellationToken).ConfigureAwait(false);
        }

        return lastResponse;
    }

    private static void ValidateSegmentResponse(
        string response,
        int frameIndex,
        int frameCount,
        string operationName)
    {
        byte[] apdu = GetApdu(response);
        if (frameIndex < frameCount - 1)
        {
            if (apdu.Length < 2 || apdu[0] != frameIndex || apdu[1] != 0x80)
            {
                string actual = apdu.Length >= 2 ? $"{apdu[0]:X2} {apdu[1]:X2}" : ToHex(apdu);
                throw new InvalidOperationException(
                    $"{operationName}第{frameIndex + 1}帧确认异常，期望={frameIndex:X2} 80，实际={actual}。");
            }

            return;
        }

        if (apdu.Length == 0 || apdu[0] != 0x90)
        {
            string actual = apdu.Length == 0 ? string.Empty : $"{apdu[0]:X2}";
            throw new InvalidOperationException($"{operationName}最后一帧未返回业务响应90，实际={actual}。");
        }
    }

    private async Task VerifyMeterBusinessResponseAsync(
        string response,
        MeterEsamData esam,
        SessionData session,
        int verifyKeyState,
        string expectedResponse,
        string operationName,
        Action<string> trace,
        CancellationToken cancellationToken)
    {
        byte[] apdu = GetApdu(response);
        if (apdu.Length < 3 || apdu[0] != 0x90)
            throw new InvalidOperationException($"{operationName}最终响应不是90业务响应。");

        int taskLength = apdu[2];
        int taskIndex = 3;
        int afterTaskIndex = taskIndex + taskLength;
        int macLengthIndex = afterTaskIndex + 2;
        if (taskLength == 0 || afterTaskIndex + 1 >= apdu.Length || macLengthIndex >= apdu.Length ||
            apdu[afterTaskIndex] != 0x01 ||
            apdu[afterTaskIndex + 1] != 0x00 || apdu[macLengthIndex] != 0x04 ||
            macLengthIndex + 1 + apdu[macLengthIndex] > apdu.Length)
        {
            throw new InvalidOperationException($"{operationName}最终响应的解密字段格式异常。");
        }

        string taskData = ToHex(apdu, taskIndex, taskLength);
        string mac = ToHex(apdu, macLengthIndex + 1, apdu[macLengthIndex]);
        await EnsureEncryptionConnectedAsync(trace, cancellationToken).ConfigureAwait(false);
        byte[] output = new byte[4096];
        WinSocketServer.DllResult result = winSocketServer.CallObj_Meter_Formal_VerifyMeterData(
            verifyKeyState,
            3,
            esam.EsamNumber,
            session.SessionKey,
            taskData,
            mac,
            output);
        trace(FormatEncryptionCall(
            "Obj_Meter_Formal_VerifyMeterData",
            ("iKeyState", verifyKeyState.ToString(CultureInfo.InvariantCulture)),
            ("cOperateMode", "3"),
            ("cESAMID", esam.EsamNumber),
            ("cSessionKey", session.SessionKey),
            ("cTaskData", taskData),
            ("cMac", mac),
            ("cOutData", DecodeOutputForLog(output)),
            ("iRes", result.Code.ToString(CultureInfo.InvariantCulture))));
        if (!result.Success)
            throw new InvalidOperationException($"{operationName}最终响应解密失败，错误码={result.Code}。");

        string outputHex = DecodeOutput(output, $"{operationName}明文响应");
        if (!string.IsNullOrWhiteSpace(expectedResponse) &&
            !string.Equals(outputHex, NormalizeHex(expectedResponse), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"{operationName}明文响应校验失败，实际={outputHex}，期望={NormalizeHex(expectedResponse)}。");
        }

        trace($"{operationName}电表响应解密成功：{outputHex}。");
    }

    private static string BuildMeterNo(string meterAddress) => "0000" + NormalizeHex(meterAddress);

    private static string BuildSessionDiv(int keyState, string meterAddress, string esamNumber)
    {
        return keyState switch
        {
            0 => NormalizeHex(esamNumber),
            1 => BuildMeterNo(meterAddress),
            _ => throw new InvalidOperationException($"不支持的会话密钥状态：{keyState}。")
        };
    }

    private static bool IsAllZero(string value) => !string.IsNullOrWhiteSpace(value) && value.All(c => c == '0');

    private static uint ReadUInt32(byte[] data, int offset) =>
        (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);

    private static string DecodeOutput(byte[] bytes, string fieldName)
    {
        string text = Encoding.ASCII.GetString(bytes).Trim('\0', ' ', '\r', '\n');
        try
        {
            return NormalizeHex(text);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"加密机返回的{fieldName}不是合法的HEX，长度={text.Length}，内容摘要={CreateOutputPreview(text)}。",
                ex);
        }
    }

    private static byte[] HexToBytes(string value)
    {
        string normalized = NormalizeHex(value);
        byte[] bytes = new byte[normalized.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = byte.Parse(normalized.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return bytes;
    }

    private static string NormalizeHex(string? value)
    {
        string normalized = new string((value ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .ToArray())
            .ToUpperInvariant();
        if (normalized.Any(character => !Uri.IsHexDigit(character)))
            throw new InvalidOperationException($"HEX 数据包含非法字符：{CreateOutputPreview(value)}。");
        if ((normalized.Length & 1) != 0)
            throw new InvalidOperationException($"HEX 数据长度必须为偶数：{value}。");
        return normalized;
    }

    private static string FormatHex(string value)
    {
        byte[] bytes = HexToBytes(value);
        return FormatHex(bytes);
    }

    private static string FormatHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", " ");
    }

    private static string DecodeOutputForLog(byte[] bytes)
    {
        return Encoding.ASCII.GetString(bytes).Trim('\0', ' ', '\r', '\n');
    }

    private static string FormatEncryptionCall(
        string methodName,
        params (string Name, string Value)[] parameters)
    {
        return string.Join(
            Environment.NewLine,
            new[] { methodName }
                .Concat(parameters.Select(parameter => $"{parameter.Name} = {parameter.Value}"))
                .Concat(new[] { string.Empty }));
    }

    private static string CreateOutputPreview(string? value)
    {
        string preview = (value ?? string.Empty).Replace('\0', ' ');
        return preview.Length <= 64 ? preview : preview[..64] + "...";
    }

    private static string ToHex(byte[] data) => Convert.ToHexString(data);

    private static string ToHex(byte[] data, int offset, int length) => Convert.ToHexString(data, offset, length);

    private const string Separator = "-----------------------------------------------------------------";

    public void Dispose()
    {
        if (encryptionConnected)
        {
            try { winSocketServer.ClseUsbkeyEx(); } catch { }
            encryptionConnected = false;
        }
        winSocketServer.Dispose();
    }

    private sealed class KeyRecoveryRunState
    {
        public bool Completed { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<MeterEsamData> Esams { get; } = new();
    }

    private sealed record RecoveryPhaseData(
        MeterEsamData Before,
        MeterEsamData After,
        SessionData AfterRecoverySession);

    private enum ConnectApduProfile
    {
        Recovery,
        Updated
    }

    private sealed record WinSocketServerConfig(string Ip, string Port, int TimeoutMs);

    private sealed record EsamPayload(
        string EsamNumber,
        string SymmetricKeyVersion,
        uint ApplicationCounter,
        uint ActiveReportCounter,
        uint BroadcastCounter);

    private sealed record SessionData(string RandHost, string SessionKey);
}
