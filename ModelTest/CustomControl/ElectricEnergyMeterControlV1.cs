using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ModelTest.Socket_DLL.Socket_Client;

namespace ModelTest.CustomControl
{
    public partial class ElectricEnergyMeterControlV1 : UserControl
    {
        private const byte MeterFrameStart = 0x55;
        private const byte MeterFrameStop = 0xAA;
        private static readonly Color SuccessMessageColor = Color.FromArgb(245, 245, 245);
        private const byte MeterDirectionPcToMcu = 0x00;
        private const byte MeterDirectionMcuToPc = 0x01;
        private const byte MeterControlProtocol = 0x00;
        private const byte MeterTestCommand = 0x00;
        private const byte MeterAcVoltageCommand = 0x01;
        private const byte MeterAcCurrentCommand = 0x02;
        private const byte MeterDailyTimingCommand = 0x36;
        private const byte MeterMeterPresenceDetectionCommand = 0x84;
        private const byte MeterVoltageShortCircuitDetectionCommand = 0x86;
        private const byte MeterMotorCrimpingCommand = 0xC9;
        private const byte MeterResetCommand = 0xFF;
        private const byte MeterFeedbackCommand = 0xFB;
        private const byte MeterEmptyDataItem = 0x00;
        private const byte DailyTimingStartDataItem = 0x00;
        private const byte DailyTimingResultDataItem = 0xAA;
        private const byte DailyTimingStopDataItem = 0xFF;
        private const byte MeterPresenceDetectionStartDataItem = 0x01;
        private const byte MeterPresenceDetectionResultDataItem = 0xAA;
        private const byte VoltageShortCircuitDetectionStartDataItem = 0x01;
        private const byte VoltageShortCircuitDetectionResultDataItem = 0xAA;
        private const byte MotorCrimpingPressDataItem = 0x00;
        private const byte MotorCrimpingReleaseDataItem = 0x01;
        private const byte MotorCrimpingPowerOffDataItem = 0xFF;

        public delegate void UpdateMainFormDelegate(string message, Color? color = null);

        public event UpdateMainFormDelegate? OnUpdateRequested_MeterV1;

        private readonly PhaseControlConfig _acVoltageControl;
        private readonly PhaseControlConfig _acCurrentControl;
        private readonly object _meterResponseWaitersLock = new();
        private readonly List<MeterResponseWaiter> _meterResponseWaiters = new();
        private EnhancedTcpClient? _meterClient;
        private CancellationTokenSource? _dailyTimingWorkflowCts;
        private string _voltageShortCircuitSummary = "未检测";
        private string _meterPresenceSummary = "未检测";

        private enum ConnectionUiState
        {
            Disconnected,
            Connecting,
            Connected
        }

        private sealed class PhaseControlConfig
        {
            public PhaseControlConfig(
                Button actionButton,
                CheckBox phaseA,
                CheckBox phaseB,
                CheckBox phaseC,
                byte commandCode,
                string enableText,
                string disableText,
                string categoryLabel)
            {
                ActionButton = actionButton;
                PhaseA = phaseA;
                PhaseB = phaseB;
                PhaseC = phaseC;
                CommandCode = commandCode;
                EnableText = enableText;
                DisableText = disableText;
                CategoryLabel = categoryLabel;
            }

            public Button ActionButton { get; }

            public CheckBox PhaseA { get; }

            public CheckBox PhaseB { get; }

            public CheckBox PhaseC { get; }

            public byte CommandCode { get; }

            public string EnableText { get; }

            public string DisableText { get; }

            public string CategoryLabel { get; }
        }

        private sealed class MeterResponseWaiter
        {
            public MeterResponseWaiter(Func<byte[], bool> predicate, TaskCompletionSource<byte[]> completionSource)
            {
                Predicate = predicate;
                CompletionSource = completionSource;
            }

            public Func<byte[], bool> Predicate { get; }

            public TaskCompletionSource<byte[]> CompletionSource { get; }
        }

        public ElectricEnergyMeterControlV1()
        {
            InitializeComponent();
            BackColor = Color.FromArgb(88, 149, 127);

            _acVoltageControl = new PhaseControlConfig(
                btnAcVoltagePower,
                cbxPhaseA,
                cbxPhaseB,
                cbxPhaseC,
                MeterAcVoltageCommand,
                "上电",
                "断电",
                "交流电压控制");

            _acCurrentControl = new PhaseControlConfig(
                btnAcCurrentPower,
                cbxCurrentPhaseA,
                cbxCurrentPhaseB,
                cbxCurrentPhaseC,
                MeterAcCurrentCommand,
                "通电流",
                "断电流",
                "交流电流控制");

            ConfigureNumericTextBox(tbxDailyTimingTime, 2);
            ConfigureNumericTextBox(tbxDailyTimingCount, 2);
            UpdateDailyTimingCountdownLabel(null);
            UpdateStationDetectionResultLabel();
            SetConnectionUiState(ConnectionUiState.Disconnected);
        }

        private async void btn_MeterTCPConnect_Click(object sender, EventArgs e)
        {
            if (!TryGetMeterEndpoint(out string meterIp, out int meterPort, out string meterPortText))
            {
                return;
            }

            if (_meterClient?.IsConnected == true)
            {
                DisconnectMeterClient(meterIp, meterPortText);
                return;
            }

            EnsureMeterClient();

            bool connected = await _meterClient!.ConnectAsync(meterIp, meterPort);
            if (connected)
            {
                PublishMeterMessage($"{meterIp}:{meterPortText}连接成功");
                SetConnectionUiState(ConnectionUiState.Connected, _meterClient.ServerEndpoint);
            }
            else
            {
                PublishMeterMessage($"{meterIp}:{meterPortText}连接失败");
                _meterClient = null;
                SetConnectionUiState(ConnectionUiState.Disconnected);
            }
        }

        private bool TryGetMeterEndpoint(out string meterIp, out int meterPort, out string meterPortText)
        {
            meterIp = tbx_MeterIP.Text.Trim();
            meterPortText = tbx_MeterPort.Text.Trim();
            meterPort = 0;

            if (string.IsNullOrEmpty(meterIp) || string.IsNullOrEmpty(meterPortText))
            {
                MessageBox.Show("请输入IP地址和端口号！");
                return false;
            }

            if (!int.TryParse(meterPortText, out meterPort))
            {
                MessageBox.Show("端口号格式不正确！");
                return false;
            }

            return true;
        }

        private void EnsureMeterClient()
        {
            if (_meterClient != null)
            {
                return;
            }

            _meterClient = new EnhancedTcpClient();
            _meterClient.EnableAutoReconnect = false;
            _meterClient.EnableHeartbeat = false;
            _meterClient.EnableInactivityProbe = false;
            _meterClient.MessageReceived += OnMeterMCUMessageReceived;
            _meterClient.ConnectionStatusChanged += OnMeterMCUConnectionStatusChanged;
            _meterClient.ErrorOccurred += OnErrorOccurred;
            _meterClient.BytesTransferred += OnBytesTransferred;
        }

        private void DisconnectMeterClient(string meterIp, string meterPortText)
        {
            _meterClient?.Disconnect();
            _meterClient = null;
            PublishMeterMessage($"{meterIp}:{meterPortText}已断开");
            SetConnectionUiState(ConnectionUiState.Disconnected);
        }

        private void SetConnectionUiState(ConnectionUiState state, string? endpoint = null)
        {
            btn_MeterTCPConnect.BackColor = Color.White;

            switch (state)
            {
                case ConnectionUiState.Connected:
                    btn_MeterTCPConnect.Text = "断开";
                    label3.Text = $"状态：TCP客户端 - 已连接到 {endpoint}";
                    break;
                case ConnectionUiState.Connecting:
                    btn_MeterTCPConnect.Text = "连接中";
                    label3.Text = $"状态：TCP客户端 - 正在连接 {endpoint}";
                    break;
                default:
                    btn_MeterTCPConnect.Text = "连接";
                    label3.Text = "状态：TCP客户端 - 未连接";
                    break;
            }
        }

        private ConnectionUiState GetConnectionUiState(TcpClientStatusEventArgs e)
        {
            if (e.IsConnected)
            {
                return ConnectionUiState.Connected;
            }

            return _meterClient?.Status == "Connecting"
                ? ConnectionUiState.Connecting
                : ConnectionUiState.Disconnected;
        }

        private void PublishMeterMessage(string message, Color? color = null)
        {
            OnUpdateRequested_MeterV1?.Invoke(message, color);
        }

        private static string ToHexString(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", " ");
        }

        private void OnErrorOccurred(object? sender, string errorMessage)
        {
            UpdateUI(() => PublishMeterMessage($"[错误] {errorMessage}"));
        }

        private void OnBytesTransferred(object? sender, long e)
        {
        }

        private void OnMeterMCUConnectionStatusChanged(object? sender, TcpClientStatusEventArgs e)
        {
            UpdateUI(() =>
            {
                ConnectionUiState uiState = GetConnectionUiState(e);
                string statusText = uiState switch
                {
                    ConnectionUiState.Connected => "✅ 已连接",
                    ConnectionUiState.Connecting => "🔄 连接中",
                    _ => "❌ 已断开"
                };

                PublishMeterMessage($"[{e.Timestamp:HH:mm:ss}] {statusText}: {e.Status}");
                SetConnectionUiState(uiState, _meterClient?.ServerEndpoint);
            });
        }

        private void OnMeterMCUMessageReceived(object? sender, TcpClientMessageEventArgs e)
        {
            UpdateStationDetectionSummary(e.RawData);
            NotifyMeterResponseWaiters(e.RawData);
            UpdateUI(() =>
            {
                string hexData = ToHexString(e.RawData);
                PublishMeterMessage($"接收消息成功[PC<--MCU] : {hexData}", SuccessMessageColor);

                string messageDescription = DescribeMeterResponse(e.RawData);
                if (!string.IsNullOrEmpty(messageDescription))
                {
                    PublishMeterMessage(
                        messageDescription,
                        IsErrorResponseDescription(messageDescription) ? Color.Red : SuccessMessageColor);
                }

                LogMessage.Debug($"接受消息成功[PC<-- MCU]的数据: {hexData}");
            });
        }

        private void UpdateStationDetectionSummary(byte[] rawData)
        {
            if (TryGetMeterPacketDataItems(rawData, out byte command, out byte[] dataItems))
            {
                if (command == MeterVoltageShortCircuitDetectionCommand &&
                    dataItems.Length == 2 &&
                    dataItems[0] == VoltageShortCircuitDetectionResultDataItem)
                {
                    _voltageShortCircuitSummary = dataItems[1] switch
                    {
                        0x00 => "电压正常",
                        0x01 => "A相电压短路",
                        0x02 => "B相电压短路",
                        0x04 => "C相电压短路",
                        0x03 => "A、B与N短路",
                        0x05 => "A、C与N短路",
                        0x06 => "B、C与N短路",
                        0x07 => "三相电压都短路",
                        _ => $"未知结果 {dataItems[1]:X2}"
                    };
                }

                if (command == MeterMeterPresenceDetectionCommand &&
                    dataItems.Length == 2 &&
                    dataItems[0] == MeterPresenceDetectionResultDataItem)
                {
                    _meterPresenceSummary = dataItems[1] switch
                    {
                        0x00 => "无电表，电流线路可能开路",
                        0x01 => "有电表，电流线路正常",
                        0x02 => "短接磁保持继电器短路异常",
                        _ => $"未知结果 {dataItems[1]:X2}"
                    };
                }
            }

            UpdateStationDetectionResultLabel();
        }

        private void NotifyMeterResponseWaiters(byte[] rawData)
        {
            List<MeterResponseWaiter> matchedWaiters = new();

            lock (_meterResponseWaitersLock)
            {
                foreach (MeterResponseWaiter waiter in _meterResponseWaiters.ToList())
                {
                    if (!waiter.Predicate(rawData))
                    {
                        continue;
                    }

                    matchedWaiters.Add(waiter);
                    _meterResponseWaiters.Remove(waiter);
                }
            }

            foreach (MeterResponseWaiter waiter in matchedWaiters)
            {
                waiter.CompletionSource.TrySetResult(rawData);
            }
        }

        private static void ConfigureNumericTextBox(TextBox textBox, int maxLength)
        {
            textBox.MaxLength = maxLength;
            textBox.KeyPress += NumericTextBox_KeyPress;
            textBox.TextChanged += NumericTextBox_TextChanged;
        }

        private static void NumericTextBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private static void NumericTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            string digitsOnly = new(textBox.Text.Where(char.IsDigit).ToArray());
            if (textBox.Text == digitsOnly)
            {
                return;
            }

            textBox.Text = digitsOnly;
            textBox.SelectionStart = textBox.Text.Length;
        }

        private void UpdateUI(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        private async void btnTestMeterCommunication_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(MeterTestCommand, "测试表位通信报文", MeterEmptyDataItem);
        }

        private async void btnResetCommand_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(MeterResetCommand, "复位命令", MeterEmptyDataItem);
        }

        private async void btnAcVoltagePower_Click(object sender, EventArgs e)
        {
            await HandlePhaseControlAsync(_acVoltageControl);
        }

        private async void btnAcCurrentPower_Click(object sender, EventArgs e)
        {
            await HandlePhaseControlAsync(_acCurrentControl);
        }

        private async void btnStartDailyTiming_Click(object sender, EventArgs e)
        {
            await RunDailyTimingWorkflowAsync();
        }

        private async void btnGetDailyTimingResult_Click(object sender, EventArgs e)
        {
            if (!TryGetDailyTimingParameters(out byte testTime, out byte testCount))
            {
                return;
            }

            await SendCommandAsync(
                MeterDailyTimingCommand,
                $"日计时结果获取[时间={testTime}s, 次数={testCount}]",
                DailyTimingResultDataItem,
                testTime,
                testCount);
        }

        private async void btnStopDailyTiming_Click(object sender, EventArgs e)
        {
            _dailyTimingWorkflowCts?.Cancel();

            if (!TryGetDailyTimingParameters(out byte testTime, out byte testCount))
            {
                return;
            }

            await SendCommandAsync(
                MeterDailyTimingCommand,
                $"停止日计时[时间={testTime}s, 次数={testCount}]",
                DailyTimingStopDataItem,
                testTime,
                testCount);
        }

        private async void btnStartVoltageShortCircuitDetection_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterVoltageShortCircuitDetectionCommand,
                "表位电压短路检测[开始检测]",
                VoltageShortCircuitDetectionStartDataItem);
        }

        private async void btnGetVoltageShortCircuitDetectionResult_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterVoltageShortCircuitDetectionCommand,
                "表位电压短路检测[结果获取]",
                VoltageShortCircuitDetectionResultDataItem);
        }

        private async void btnStartMeterPresenceDetection_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMeterPresenceDetectionCommand,
                "表位有无电表检测[开始检测]",
                MeterPresenceDetectionStartDataItem);
        }

        private async void btnGetMeterPresenceDetectionResult_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMeterPresenceDetectionCommand,
                "表位有无电表检测[结果获取]",
                MeterPresenceDetectionResultDataItem);
        }

        private async void btnMotorCrimpPress_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMotorCrimpingCommand,
                "电机压接[压接]",
                MotorCrimpingPressDataItem);
        }

        private async void btnMotorCrimpRelease_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMotorCrimpingCommand,
                "电机压接[弹开]",
                MotorCrimpingReleaseDataItem);
        }

        private async void btnMotorCrimpPowerOff_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMotorCrimpingCommand,
                "电机压接[电机断电]",
                MotorCrimpingPowerOffDataItem);
        }

        private async Task RunDailyTimingWorkflowAsync()
        {
            if (_dailyTimingWorkflowCts != null)
            {
                PublishMeterMessage("[错误] 日计时试验正在进行中，请勿重复开始");
                return;
            }

            if (!TryGetReadyMeterAddress(out byte meterAddress) ||
                !TryGetDailyTimingParameters(out byte testTime, out byte testCount))
            {
                return;
            }

            _dailyTimingWorkflowCts = new CancellationTokenSource();
            CancellationToken cancellationToken = _dailyTimingWorkflowCts.Token;
            SetDailyTimingUiBusy(true);

            try
            {
                byte[] startPacket = BuildMeterPacket(
                    MeterDirectionPcToMcu,
                    meterAddress,
                    MeterDailyTimingCommand,
                    DailyTimingStartDataItem,
                    testTime,
                    testCount);

                byte[]? startResponse = await SendPacketAndWaitForResponseAsync(
                    startPacket,
                    $"日计时试验[开始, 时间={testTime}s, 次数={testCount}]",
                    rawData => IsExpectedDailyTimingResponse(rawData, meterAddress, DailyTimingStartDataItem, testTime, testCount),
                    TimeSpan.FromSeconds(5),
                    cancellationToken);

                if (startResponse == null)
                {
                    PublishMeterMessage("[错误] 开始日计时后未收到正确应答");
                    return;
                }

                int waitSeconds = (testTime * testCount) + testCount;
                PublishMeterMessage($"日计时开始应答正常，等待 {testTime} * {testCount} + {testCount} = {waitSeconds} 秒后自动获取结果");

                await RunDailyTimingCountdownAsync(waitSeconds, cancellationToken);

                byte[] resultPacket = BuildMeterPacket(
                    MeterDirectionPcToMcu,
                    meterAddress,
                    MeterDailyTimingCommand,
                    DailyTimingResultDataItem,
                    testTime,
                    testCount);

                byte[]? resultResponse = await SendPacketAndWaitForResponseAsync(
                    resultPacket,
                    $"日计时结果获取[时间={testTime}s, 次数={testCount}]",
                    rawData => IsExpectedDailyTimingResponse(rawData, meterAddress, DailyTimingResultDataItem, testTime, testCount),
                    TimeSpan.FromSeconds(10),
                    cancellationToken);

                if (resultResponse == null)
                {
                    PublishMeterMessage("[错误] 自动获取日计时结果后未收到应答");
                }
            }
            catch (OperationCanceledException)
            {
                PublishMeterMessage("日计时自动流程已取消");
            }
            finally
            {
                _dailyTimingWorkflowCts.Dispose();
                _dailyTimingWorkflowCts = null;
                SetDailyTimingUiBusy(false);
                UpdateDailyTimingCountdownLabel(null);
            }
        }

        private async Task SendCommandAsync(byte commandCode, string packetName, params byte[] dataItems)
        {
            if (!TryGetReadyMeterAddress(out byte meterAddress))
            {
                return;
            }

            byte[] packet = BuildMeterPacket(MeterDirectionPcToMcu, meterAddress, commandCode, dataItems);
            await SendMeterPacketAsync(packet, packetName);
        }

        private async Task<byte[]?> SendPacketAndWaitForResponseAsync(
            byte[] packet,
            string packetName,
            Func<byte[], bool> responsePredicate,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using CancellationTokenSource responseWaitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task<byte[]?> responseTask = WaitForMeterResponseAsync(responsePredicate, timeout, responseWaitCts.Token);

            bool sendSuccess = await SendMeterPacketAsync(packet, packetName);
            if (!sendSuccess)
            {
                responseWaitCts.Cancel();
                return null;
            }

            return await responseTask;
        }

        private async Task HandlePhaseControlAsync(PhaseControlConfig config)
        {
            if (!TryGetReadyMeterAddress(out byte meterAddress))
            {
                return;
            }

            bool isEnableAction = config.ActionButton.Text == config.EnableText;
            if (!TryGetPhaseControlDataItem(config, isEnableAction, out byte dataItem, out string phaseDescription))
            {
                return;
            }

            string actionText = isEnableAction ? config.EnableText : config.DisableText;
            byte[] packet = BuildMeterPacket(MeterDirectionPcToMcu, meterAddress, config.CommandCode, dataItem);
            await SendMeterPacketAsync(packet, $"{config.CategoryLabel}[{phaseDescription}{actionText}]");

            config.ActionButton.Text = isEnableAction ? config.DisableText : config.EnableText;
        }

        private async Task<bool> SendMeterPacketAsync(byte[] packet, string packetName)
        {
            if (_meterClient?.IsConnected != true)
            {
                PublishMeterMessage($"[错误] 电表TCP客户端未连接，无法发送{packetName}");
                return false;
            }

            bool send = await _meterClient.SendBytesAsync(packet);
            string hexPacket = ToHexString(packet);

            if (send)
            {
                PublishMeterMessage($"发送{packetName}[PC-->MCU] : {hexPacket}", Color.Green);
            }
            else
            {
                PublishMeterMessage($"发送{packetName}失败[PC-->MCU] : {hexPacket}");
            }

            return send;
        }

        private bool TryGetReadyMeterAddress(out byte meterAddress)
        {
            meterAddress = 0x00;
            if (_meterClient?.IsConnected != true)
            {
                PublishMeterMessage("[错误] 电表TCP客户端未连接");
                return false;
            }

            if (!TryParseMeterAddress(tbxMeterV1Addr.Text, out meterAddress))
            {
                PublishMeterMessage("[错误] 表位地址格式不正确，请输入 1-255 或 AA");
                return false;
            }

            return true;
        }

        private bool TryGetDailyTimingParameters(out byte testTime, out byte testCount)
        {
            testTime = 0x00;
            testCount = 0x00;

            if (!byte.TryParse(tbxDailyTimingTime.Text.Trim(), out testTime) || testTime < 1 || testTime > 99)
            {
                PublishMeterMessage("[错误] 日计时时间只能填写 1-99 秒");
                return false;
            }

            if (!byte.TryParse(tbxDailyTimingCount.Text.Trim(), out testCount) || testCount < 1 || testCount > 10)
            {
                PublishMeterMessage("[错误] 日计时次数只能填写 1-10 次");
                return false;
            }

            return true;
        }

        private void SetDailyTimingUiBusy(bool isBusy)
        {
            btnStartDailyTiming.Enabled = !isBusy;
            btnGetDailyTimingResult.Enabled = !isBusy;
            tbxDailyTimingTime.Enabled = !isBusy;
            tbxDailyTimingCount.Enabled = !isBusy;
        }

        private void UpdateStationDetectionResultLabel()
        {
            void UpdateLabel()
            {
                labelStationDetectionSummary.Text =
                    $"电压短路检测：{_voltageShortCircuitSummary}\r\n有无电表/电流线路：{_meterPresenceSummary}";
            }

            UpdateUI(UpdateLabel);
        }

        private async Task RunDailyTimingCountdownAsync(int totalSeconds, CancellationToken cancellationToken)
        {
            for (int remainingSeconds = totalSeconds; remainingSeconds > 0; remainingSeconds--)
            {
                UpdateDailyTimingCountdownLabel(remainingSeconds);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }

            UpdateDailyTimingCountdownLabel(0);
        }

        private void UpdateDailyTimingCountdownLabel(int? remainingSeconds)
        {
            void UpdateLabel()
            {
                labelDailyTimingCountdown.Text = remainingSeconds.HasValue
                    ? $"倒计时：{remainingSeconds.Value}s"
                    : "倒计时：未开始";
            }

            UpdateUI(UpdateLabel);
        }

        private async Task<byte[]?> WaitForMeterResponseAsync(
            Func<byte[], bool> predicate,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            TaskCompletionSource<byte[]> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
            MeterResponseWaiter waiter = new(predicate, completionSource);

            lock (_meterResponseWaitersLock)
            {
                _meterResponseWaiters.Add(waiter);
            }

            try
            {
                Task completedTask = await Task.WhenAny(
                    completionSource.Task,
                    Task.Delay(timeout, cancellationToken));

                if (completedTask == completionSource.Task)
                {
                    return await completionSource.Task;
                }

                cancellationToken.ThrowIfCancellationRequested();
                return null;
            }
            finally
            {
                lock (_meterResponseWaitersLock)
                {
                    _meterResponseWaiters.Remove(waiter);
                }
            }
        }

        private bool TryGetPhaseControlDataItem(PhaseControlConfig config, bool isEnableAction, out byte dataItem, out string phaseDescription)
        {
            return TryGetPhaseControlDataItem(
                config.PhaseA,
                config.PhaseB,
                config.PhaseC,
                isEnableAction,
                out dataItem,
                out phaseDescription);
        }

        private bool TryGetPhaseControlDataItem(CheckBox phaseA, CheckBox phaseB, CheckBox phaseC, bool isEnableAction, out byte dataItem, out string phaseDescription)
        {
            dataItem = 0x00;
            phaseDescription = string.Empty;

            int selectedCount = new[] { phaseA.Checked, phaseB.Checked, phaseC.Checked }.Count(x => x);

            if (selectedCount == 0)
            {
                PublishMeterMessage("[错误] 请至少选择 A/B/C 中的一相，或三相全选");
                return false;
            }

            if (selectedCount == 2)
            {
                PublishMeterMessage("[错误] 协议不支持两相组合，请选择单相或三相");
                return false;
            }

            if (selectedCount == 3)
            {
                dataItem = isEnableAction ? (byte)0x04 : (byte)0x08;
                phaseDescription = "ABC三相";
                return true;
            }

            if (phaseA.Checked)
            {
                dataItem = isEnableAction ? (byte)0x01 : (byte)0x05;
                phaseDescription = "A相";
                return true;
            }

            if (phaseB.Checked)
            {
                dataItem = isEnableAction ? (byte)0x02 : (byte)0x06;
                phaseDescription = "B相";
                return true;
            }

            dataItem = isEnableAction ? (byte)0x03 : (byte)0x07;
            phaseDescription = "C相";
            return true;
        }

        private static byte[] BuildMeterPacket(byte direction, byte meterAddress, byte command, params byte[] dataItems)
        {
            byte[] payload = (dataItems == null || dataItems.Length == 0) ? new[] { MeterEmptyDataItem } : dataItems;
            int frameLength = 7 + payload.Length;
            byte[] packet = new byte[frameLength + 2];

            packet[0] = MeterFrameStart;
            packet[1] = (byte)(frameLength & 0xFF);
            packet[2] = (byte)((frameLength >> 8) & 0xFF);
            packet[3] = direction;
            packet[4] = meterAddress;
            packet[5] = MeterControlProtocol;
            packet[6] = command;
            Array.Copy(payload, 0, packet, 7, payload.Length);

            packet[frameLength] = CalculateChecksum(packet, 1, frameLength - 1);
            packet[frameLength + 1] = MeterFrameStop;
            return packet;
        }

        private static byte CalculateChecksum(byte[] data, int startIndex, int count)
        {
            int sum = 0;
            for (int i = startIndex; i < startIndex + count; i++)
            {
                sum += data[i];
            }

            return (byte)sum;
        }

        private static bool TryParseMeterAddress(string addressText, out byte meterAddress)
        {
            meterAddress = 0x00;
            if (string.IsNullOrWhiteSpace(addressText))
            {
                return false;
            }

            string normalized = addressText.Trim();
            if (normalized.Equals("AA", StringComparison.OrdinalIgnoreCase))
            {
                meterAddress = 0xAA;
                return true;
            }

            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[2..];
            }

            if (byte.TryParse(normalized, out byte decimalAddress) && decimalAddress > 0)
            {
                meterAddress = decimalAddress;
                return true;
            }

            if (byte.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hexAddress) && hexAddress > 0)
            {
                meterAddress = hexAddress;
                return true;
            }

            return false;
        }

        private static bool IsExpectedDailyTimingResponse(
            byte[] rawData,
            byte meterAddress,
            byte operation,
            byte testTime,
            byte testCount)
        {
            if (!TryGetMeterPacketDataItems(rawData, meterAddress, MeterDailyTimingCommand, out byte[] dataItems))
            {
                return false;
            }

            if (dataItems.Length < 3 ||
                dataItems[0] != operation ||
                dataItems[1] != testTime ||
                dataItems[2] != testCount)
            {
                return false;
            }

            return operation != DailyTimingResultDataItem || dataItems.Length >= 3;
        }

        private static bool TryGetMeterPacketDataItems(byte[] rawData, byte meterAddress, byte command, out byte[] dataItems)
        {
            dataItems = Array.Empty<byte>();
            if (rawData == null || rawData.Length < 10)
            {
                return false;
            }

            if (rawData[0] != MeterFrameStart || rawData[^1] != MeterFrameStop)
            {
                return false;
            }

            int frameLength = rawData[1] | (rawData[2] << 8);
            if (rawData.Length < frameLength + 2 || frameLength < 8)
            {
                return false;
            }

            int dataItemLength = frameLength - 7;
            if (dataItemLength < 0 || rawData.Length < dataItemLength + 9)
            {
                return false;
            }

            if (CalculateChecksum(rawData, 1, frameLength - 1) != rawData[frameLength])
            {
                return false;
            }

            if (rawData[3] != MeterDirectionMcuToPc ||
                rawData[4] != meterAddress ||
                rawData[5] != MeterControlProtocol ||
                rawData[6] != command)
            {
                return false;
            }

            dataItems = rawData.Skip(7).Take(dataItemLength).ToArray();
            return true;
        }

        private static bool TryGetMeterPacketDataItems(byte[] rawData, out byte command, out byte[] dataItems)
        {
            command = 0x00;
            dataItems = Array.Empty<byte>();
            if (rawData == null || rawData.Length < 10)
            {
                return false;
            }

            if (rawData[0] != MeterFrameStart || rawData[^1] != MeterFrameStop)
            {
                return false;
            }

            int frameLength = rawData[1] | (rawData[2] << 8);
            if (rawData.Length < frameLength + 2 || frameLength < 8)
            {
                return false;
            }

            int dataItemLength = frameLength - 7;
            if (dataItemLength < 0 || rawData.Length < dataItemLength + 9)
            {
                return false;
            }

            if (CalculateChecksum(rawData, 1, frameLength - 1) != rawData[frameLength] ||
                rawData[3] != MeterDirectionMcuToPc ||
                rawData[5] != MeterControlProtocol)
            {
                return false;
            }

            command = rawData[6];
            dataItems = rawData.Skip(7).Take(dataItemLength).ToArray();
            return true;
        }

        private static string DescribeMeterResponse(byte[] rawData)
        {
            if (rawData == null || rawData.Length < 10)
            {
                return string.Empty;
            }

            if (rawData[0] != MeterFrameStart || rawData[^1] != MeterFrameStop)
            {
                return string.Empty;
            }

            int frameLength = rawData[1] | (rawData[2] << 8);
            if (rawData.Length < frameLength + 2 || frameLength < 8)
            {
                return "收到电表协议报文，但长度字段异常";
            }

            int dataItemLength = frameLength - 7;
            if (dataItemLength < 0 || rawData.Length < dataItemLength + 9)
            {
                return "收到电表协议报文，但数据项长度异常";
            }

            byte expectedChecksum = CalculateChecksum(rawData, 1, frameLength - 1);
            byte actualChecksum = rawData[frameLength];
            if (expectedChecksum != actualChecksum)
            {
                return $"收到电表协议报文，但校验失败，期望 {expectedChecksum:X2}，实际 {actualChecksum:X2}";
            }

            byte direction = rawData[3];
            byte meterAddress = rawData[4];
            byte protocol = rawData[5];
            byte command = rawData[6];
            byte[] dataItems = rawData.Skip(7).Take(dataItemLength).ToArray();

            if (command == MeterFeedbackCommand)
            {
                return DescribeFeedbackPacket(meterAddress, dataItems);
            }

            if (TryDescribeSuccessResponse(direction, protocol, command, meterAddress, dataItems, out string responseDescription))
            {
                return responseDescription;
            }

            return $"收到电表协议报文，方向={direction:X2} 地址={meterAddress:X2} 协议={protocol:X2} 命令={command:X2}";
        }

        private static bool TryDescribeSuccessResponse(byte direction, byte protocol, byte command, byte meterAddress, byte[] dataItems, out string responseDescription)
        {
            responseDescription = string.Empty;
            if (direction != MeterDirectionMcuToPc || protocol != MeterControlProtocol)
            {
                return false;
            }

            if (command == MeterTestCommand &&
                dataItems.Length == 1 &&
                dataItems[0] == MeterEmptyDataItem)
            {
                responseDescription = $"表位通信测试应答正常，表位地址={meterAddress:X2}";
                return true;
            }

            if (command == MeterResetCommand &&
                dataItems.Length == 1 &&
                dataItems[0] == MeterEmptyDataItem)
            {
                responseDescription = $"复位命令应答正常，表位地址={meterAddress:X2}";
                return true;
            }

            if (command == MeterDailyTimingCommand &&
                TryDescribeDailyTimingResponse(meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (command == MeterMeterPresenceDetectionCommand &&
                TryDescribeMeterPresenceDetectionResponse(meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (command == MeterVoltageShortCircuitDetectionCommand &&
                TryDescribeVoltageShortCircuitDetectionResponse(meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (command == MeterMotorCrimpingCommand &&
                TryDescribeMotorCrimpingResponse(meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (!TryGetPhaseControlLabel(command, out string controlLabel) ||
                !TryGetPhaseActionDescription(command, dataItems, out string actionDescription))
            {
                return false;
            }

            responseDescription = $"{controlLabel}应答，表位地址={meterAddress:X2}，{actionDescription}";
            return true;
        }

        private static bool TryGetPhaseControlLabel(byte command, out string controlLabel)
        {
            controlLabel = command switch
            {
                MeterAcVoltageCommand => "交流电压控制",
                MeterAcCurrentCommand => "交流电流控制",
                _ => string.Empty
            };

            return !string.IsNullOrEmpty(controlLabel);
        }

        private static bool TryGetPhaseActionDescription(byte command, byte[] dataItems, out string actionDescription)
        {
            actionDescription = string.Empty;
            if (dataItems.Length != 1)
            {
                return false;
            }

            byte dataItem = dataItems[0];
            string phaseLabel = dataItem switch
            {
                0x01 or 0x05 => "A相",
                0x02 or 0x06 => "B相",
                0x03 or 0x07 => "C相",
                0x04 or 0x08 => "三相",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(phaseLabel))
            {
                return false;
            }

            bool isEnableAction = dataItem <= 0x04;
            string actionLabel = command switch
            {
                MeterAcVoltageCommand => isEnableAction ? "上电" : "断电",
                MeterAcCurrentCommand => isEnableAction ? "通电流" : "断电流",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(actionLabel))
            {
                return false;
            }

            actionDescription = $"{phaseLabel}{actionLabel}";
            return true;
        }

        private static bool TryDescribeDailyTimingResponse(byte meterAddress, byte[] dataItems, out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length < 3)
            {
                return false;
            }

            byte operation = dataItems[0];
            byte testTime = dataItems[1];
            byte testCount = dataItems[2];

            if (operation == DailyTimingStartDataItem && dataItems.Length == 3)
            {
                responseDescription = $"日计时试验开始应答，表位地址={meterAddress:X2}，时间={testTime}s，次数={testCount}";
                return true;
            }

            if (operation == DailyTimingStopDataItem && dataItems.Length == 3)
            {
                responseDescription = $"日计时试验停止应答，表位地址={meterAddress:X2}，时间={testTime}s，次数={testCount}";
                return true;
            }

            if (operation != DailyTimingResultDataItem)
            {
                return false;
            }

            if (dataItems.Length == 3)
            {
                responseDescription = $"日计时结果获取应答，表位地址={meterAddress:X2}，时间={testTime}s，次数={testCount}，试验未完成或暂无结果";
                return true;
            }

            int resultDataLength = dataItems.Length - 3;
            if (resultDataLength % 4 != 0)
            {
                responseDescription = $"日计时结果获取应答，表位地址={meterAddress:X2}，结果数据长度异常";
                return true;
            }

            int resultCount = resultDataLength / 4;
            List<string> results = new(resultCount);
            for (int i = 0; i < resultCount; i++)
            {
                float result = ReadSingleLittleEndian(dataItems, 3 + (i * 4));
                results.Add($"第{i + 1}次={result.ToString("F6", CultureInfo.InvariantCulture)} s/d");
            }

            string resultSummary = string.Join("；", results);
            responseDescription = $"日计时结果获取应答，表位地址={meterAddress:X2}，时间={testTime}s，次数={testCount}，{resultSummary}";
            return true;
        }

        private static float ReadSingleLittleEndian(byte[] dataItems, int startIndex)
        {
            byte[] floatBytes = new byte[4];
            Array.Copy(dataItems, startIndex, floatBytes, 0, 4);

            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(floatBytes);
            }

            return BitConverter.ToSingle(floatBytes, 0);
        }

        private static bool TryDescribeVoltageShortCircuitDetectionResponse(byte meterAddress, byte[] dataItems, out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length == 1 &&
                dataItems[0] == VoltageShortCircuitDetectionStartDataItem)
            {
                responseDescription = $"表位电压短路检测开始应答正常，表位地址={meterAddress:X2}";
                return true;
            }

            if (dataItems.Length != 2 ||
                dataItems[0] != VoltageShortCircuitDetectionResultDataItem)
            {
                return false;
            }

            string resultDescription = dataItems[1] switch
            {
                0x00 => "电压正常",
                0x01 => "A相电压短路",
                0x02 => "B相电压短路",
                0x04 => "C相电压短路",
                0x03 => "A、B与N短路",
                0x05 => "A、C与N短路",
                0x06 => "B、C与N短路",
                0x07 => "三相电压都短路",
                _ => $"未知检测结果 {dataItems[1]:X2}"
            };

            responseDescription = $"表位电压短路检测结果，表位地址={meterAddress:X2}，{resultDescription}";
            return true;
        }

        private static bool TryDescribeMeterPresenceDetectionResponse(byte meterAddress, byte[] dataItems, out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length == 1 &&
                dataItems[0] == MeterPresenceDetectionStartDataItem)
            {
                responseDescription = $"表位有无电表检测开始应答正常，表位地址={meterAddress:X2}";
                return true;
            }

            if (dataItems.Length != 2 ||
                dataItems[0] != MeterPresenceDetectionResultDataItem)
            {
                return false;
            }

            string resultDescription = dataItems[1] switch
            {
                0x00 => "无电表",
                0x01 => "有电表",
                0x02 => "短接磁保持继电器短路异常",
                _ => $"未知检测结果 {dataItems[1]:X2}"
            };

            responseDescription = $"表位有无电表检测结果，表位地址={meterAddress:X2}，{resultDescription}";
            return true;
        }

        private static bool TryDescribeMotorCrimpingResponse(byte meterAddress, byte[] dataItems, out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length != 1)
            {
                return false;
            }

            string actionDescription = dataItems[0] switch
            {
                MotorCrimpingPressDataItem => "压接",
                MotorCrimpingReleaseDataItem => "弹开",
                MotorCrimpingPowerOffDataItem => "电机断电",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(actionDescription))
            {
                return false;
            }

            responseDescription = $"电机压接应答，表位地址={meterAddress:X2}，{actionDescription}";
            return true;
        }

        private static string DescribeFeedbackPacket(byte meterAddress, byte[] dataItems)
        {
            if (dataItems == null || dataItems.Length == 0)
            {
                return $"收到反馈包，表位地址={meterAddress:X2}，但缺少错误码";
            }

            string errorDescription = dataItems[0] switch
            {
                0x01 => "校验和不对",
                0x02 => "没有此命令码",
                0x03 => "命令码下的数据项不对",
                _ => $"未知错误码 {dataItems[0]:X2}"
            };

            return $"收到反馈包，表位地址={meterAddress:X2}，错误={errorDescription}";
        }

        private static bool IsErrorResponseDescription(string messageDescription)
        {
            return messageDescription.Contains("错误", StringComparison.Ordinal) ||
                   messageDescription.Contains("异常", StringComparison.Ordinal) ||
                   messageDescription.Contains("校验失败", StringComparison.Ordinal) ||
                   messageDescription.Contains("缺少错误码", StringComparison.Ordinal);
        }
    }
}
