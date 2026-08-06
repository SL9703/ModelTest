using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ModelTest.Protocol;
using ModelTest.Socket_DLL.Socket_Client;
using ModelTest.Tools;

namespace ModelTest.CustomControl
{
    public partial class ElectricEnergyMeterControlV2 : UserControl
    {
        private const byte MeterFrameStart1 = MeterControlPcbProtocol.V2StartByte1;
        private const byte MeterFrameStart2 = MeterControlPcbProtocol.V2StartByte2;
        private const byte MeterFrameStop1 = MeterControlPcbProtocol.V2EndByte1;
        private const byte MeterFrameStop2 = MeterControlPcbProtocol.V2EndByte2;
        private static readonly Color SuccessMessageColor = Color.FromArgb(245, 245, 245);
        private const byte MeterDirectionPcToMcu = MeterControlPcbProtocol.DownlinkDirection;
        private const byte MeterDirectionMcuToPc = MeterControlPcbProtocol.UplinkDirection;
        private const byte MeterControlProtocol = MeterControlPcbProtocol.V2MeterControlProtocolType;
        private const byte MeterTransparentProtocol = MeterControlPcbProtocol.V2MeterTransparentProtocolType;
        private const byte MeterTestCommand = MeterControlPcbProtocol.TestCommunicationCommand;
        private const byte MeterAcVoltageCommand = MeterControlPcbProtocol.AcVoltageCommand;
        private const byte MeterAcCurrentCommand = MeterControlPcbProtocol.AcCurrentCommand;
        private const byte MeterBasicErrorCommand = MeterControlPcbProtocol.BasicError21Command;
        private const byte MeterCreepingTestCommand = MeterControlPcbProtocol.CreepingTestCommand;
        private const byte MeterWalkingTestCommand = MeterControlPcbProtocol.WalkingTestCommand;
        private const byte MeterBasicErrorCommand38 = MeterControlPcbProtocol.BasicError38Command;
        private const byte MeterDailyTimingCommand = MeterControlPcbProtocol.DailyTimingCommand;
        private const byte MeterMeterPresenceDetectionCommand = MeterControlPcbProtocol.MeterPresenceDetectionCommand;
        private const byte MeterVoltageShortCircuitDetectionCommand = MeterControlPcbProtocol.VoltageShortCircuitDetectionCommand;
        private const byte MeterTemperatureCommand = MeterControlPcbProtocol.TemperatureCommand;
        private const byte MeterMotorCrimpingCommand = MeterControlPcbProtocol.MotorCrimpingCommand;
        private const byte MeterResetCommand = MeterControlPcbProtocol.ResetCommand;
        private const byte MeterFeedbackCommand = MeterControlPcbProtocol.FeedbackCommand;
        /// <summary>通用操作值00：用于启动、压接或空数据项，具体含义由命令码决定。</summary>
        public const byte OperationStart = MeterControlPcbProtocol.StartOperation;
        /// <summary>通用操作值01：用于执行、检测启动、校准或释放，具体含义由命令码决定。</summary>
        public const byte OperationExecute = MeterControlPcbProtocol.ExecuteOperation;
        /// <summary>通用操作值AA：用于读取当前命令的试验或检测结果。</summary>
        public const byte OperationRead = MeterControlPcbProtocol.ReadOperation;
        /// <summary>通用操作值FF：用于停止、断电或删除配置，具体含义由命令码决定。</summary>
        public const byte OperationStop = MeterControlPcbProtocol.StopOperation;
        private static readonly TimeSpan MultiMeterPacketInterval = MeterControlPcbProtocol.DefaultPacketInterval;

        public delegate void UpdateMainFormDelegate(string message, Color? color = null);

        public event UpdateMainFormDelegate? OnUpdateRequested_MeterV2;

        private readonly PhaseControlConfig _acVoltageControl;
        private readonly PhaseControlConfig _acCurrentControl;
        private readonly DetectionBoardProtocolV2 _deviceBoardProtocol = new();
        private readonly object _meterResponseWaitersLock = new();
        private readonly List<MeterResponseWaiter> _meterResponseWaiters = new();
        private EnhancedTcpClient? _meterClient;
        private CancellationTokenSource? _dailyTimingWorkflowCts;
        private CancellationTokenSource? _basicErrorWorkflowCts;
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
                CheckBox phaseA,
                CheckBox phaseB,
                CheckBox phaseC,
                byte commandCode,
                string categoryLabel)
            {
                PhaseA = phaseA;
                PhaseB = phaseB;
                PhaseC = phaseC;
                CommandCode = commandCode;
                CategoryLabel = categoryLabel;
            }

            public CheckBox PhaseA { get; }

            public CheckBox PhaseB { get; }

            public CheckBox PhaseC { get; }

            public byte CommandCode { get; }

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

        public ElectricEnergyMeterControlV2()
        {
            InitializeComponent();
            BackColor = Color.FromArgb(88, 149, 127);

            _acVoltageControl = new PhaseControlConfig(
                cbxPhaseA,
                cbxPhaseB,
                cbxPhaseC,
                MeterAcVoltageCommand,
                "交流电压控制");

            _acCurrentControl = new PhaseControlConfig(
                cbxCurrentPhaseA,
                cbxCurrentPhaseB,
                cbxCurrentPhaseC,
                MeterAcCurrentCommand,
                "交流电流控制");

            ConfigureNumericTextBox(tbxDailyTimingTime, 2);
            ConfigureNumericTextBox(tbxDailyTimingCount, 2);
            ConfigureNumericTextBox(tbxBasicErrorPulseCount, 2);
            ConfigureNumericTextBox(tbxBasicErrorTestCount, 2);
            cbxBasicErrorType.SelectedIndex = 0;
            cbxDeviceBoardMeterCategory.SelectedIndex = 0;
            cbxDeviceBoardRunLamp.SelectedIndex = 0;
            cbxDeviceBoardConnectionSource.SelectedIndex = 0;
            cbxDeviceBoardConnectionMode.SelectedIndex = 0;
            cbxDeviceBoardNeutralSource.SelectedIndex = 0;
            cbxDeviceBoardNeutralMode.SelectedIndex = 0;
            UpdateDeviceBoardModeControls();
            RefreshBasicErrorConstants();
            UpdateBasicErrorProtocolUi();
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
            OnUpdateRequested_MeterV2?.Invoke(message, color);
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
                    dataItems[0] == OperationRead)
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
                    dataItems[0] == OperationRead)
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

        private void tbxBasicErrorVoltage_TextChanged(object sender, EventArgs e)
        {
            RefreshBasicErrorConstants();
        }

        private void tbxBasicErrorCurrent_TextChanged(object sender, EventArgs e)
        {
            RefreshBasicErrorConstants();
        }

        private void cbxBasicErrorProtocol21_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxBasicErrorProtocol21.Checked)
            {
                cbxBasicErrorProtocol38.Checked = false;
            }
            else if (!cbxBasicErrorProtocol38.Checked)
            {
                cbxBasicErrorProtocol38.Checked = true;
            }

            UpdateBasicErrorProtocolUi();
        }

        private void cbxBasicErrorProtocol38_CheckedChanged(object sender, EventArgs e)
        {
            if (cbxBasicErrorProtocol38.Checked)
            {
                cbxBasicErrorProtocol21.Checked = false;
            }
            else if (!cbxBasicErrorProtocol21.Checked)
            {
                cbxBasicErrorProtocol21.Checked = true;
            }

            UpdateBasicErrorProtocolUi();
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
            await SendCommandAsync(MeterTestCommand, "测试表位通信报文", OperationStart);
        }

        private async void btnResetCommand_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(MeterResetCommand, "复位命令", OperationStart);
        }

        private async void btnAcVoltagePower_Click(object sender, EventArgs e)
        {
            await HandlePhaseControlAsync(_acVoltageControl, isEnableAction: true);
        }

        private async void btnAcVoltagePowerOff_Click(object sender, EventArgs e)
        {
            await HandlePhaseControlAsync(_acVoltageControl, isEnableAction: false);
        }

        private async void btnAcCurrentPower_Click(object sender, EventArgs e)
        {
            await HandlePhaseControlAsync(_acCurrentControl, isEnableAction: true);
        }

        private async void btnAcCurrentPowerOff_Click(object sender, EventArgs e)
        {
            await HandlePhaseControlAsync(_acCurrentControl, isEnableAction: false);
        }

        /// <summary>下发0x80，设置装置通信板当前测试电表类别。</summary>
        private async void btnSetDeviceBoardMeterCategory_Click(object sender, EventArgs e)
        {
            if (!TryGetDeviceBoardAddress(out byte address))
            {
                return;
            }

            DeviceBoardMeterCategory category = (DeviceBoardMeterCategory)(cbxDeviceBoardMeterCategory.SelectedIndex + 1);
            byte[] packet = _deviceBoardProtocol.BuildDeviceBoardMeterCategoryFrame(address, category);
            await SendMeterPacketAsync(packet, $"装置通信板0x80当前电表类别[{cbxDeviceBoardMeterCategory.Text}, 地址={address:X2}]");
        }

        /// <summary>下发0x81，设置测试中、合格、不合格、关闭或复位灯状态。</summary>
        private async void btnSetDeviceBoardRunLamp_Click(object sender, EventArgs e)
        {
            if (!TryGetDeviceBoardAddress(out byte address))
            {
                return;
            }

            DeviceBoardRunLampState state = (DeviceBoardRunLampState)(cbxDeviceBoardRunLamp.SelectedIndex + 1);
            byte[] packet = _deviceBoardProtocol.BuildDeviceBoardRunLampFrame(address, state);
            await SendMeterPacketAsync(packet, $"装置通信板0x81运行指示灯[{cbxDeviceBoardRunLamp.Text}, 地址={address:X2}]");
        }

        /// <summary>下发0x82，切换接线模式、恢复旋钮检测或读取旋钮状态。</summary>
        private async void btnSetDeviceBoardConnectionMode_Click(object sender, EventArgs e)
        {
            if (!TryGetDeviceBoardAddress(out byte address))
            {
                return;
            }

            DeviceBoardControlSource source = cbxDeviceBoardConnectionSource.SelectedIndex switch
            {
                0 => DeviceBoardControlSource.PcControl,
                1 => DeviceBoardControlSource.RestoreKnobDetection,
                2 => DeviceBoardControlSource.ReadKnobStatus,
                _ => DeviceBoardControlSource.PcControl
            };
            DeviceBoardConnectionMode mode = source == DeviceBoardControlSource.PcControl
                ? (DeviceBoardConnectionMode)(cbxDeviceBoardConnectionMode.SelectedIndex + 1)
                : DeviceBoardConnectionMode.None;
            byte[] packet = _deviceBoardProtocol.BuildDeviceBoardConnectionModeFrame(address, source, mode);
            string modeText = source == DeviceBoardControlSource.PcControl ? cbxDeviceBoardConnectionMode.Text : "模式字00";
            await SendMeterPacketAsync(
                packet,
                $"装置通信板0x82接线模式[{cbxDeviceBoardConnectionSource.Text}, {modeText}, 地址={address:X2}]");
        }

        /// <summary>下发0x83，切换相电流/零线电流或恢复旋钮检测。</summary>
        private async void btnSetDeviceBoardNeutralMode_Click(object sender, EventArgs e)
        {
            if (!TryGetDeviceBoardAddress(out byte address))
            {
                return;
            }

            DeviceBoardControlSource source = cbxDeviceBoardNeutralSource.SelectedIndex == 0
                ? DeviceBoardControlSource.PcControl
                : DeviceBoardControlSource.RestoreKnobDetection;
            DeviceBoardNeutralCurrentMode mode = source == DeviceBoardControlSource.PcControl
                ? (DeviceBoardNeutralCurrentMode)(cbxDeviceBoardNeutralMode.SelectedIndex + 1)
                : DeviceBoardNeutralCurrentMode.None;
            byte[] packet = _deviceBoardProtocol.BuildDeviceBoardNeutralCurrentModeFrame(address, source, mode);
            string modeText = source == DeviceBoardControlSource.PcControl ? cbxDeviceBoardNeutralMode.Text : "模式字00";
            await SendMeterPacketAsync(
                packet,
                $"装置通信板0x83零线电流模式[{cbxDeviceBoardNeutralSource.Text}, {modeText}, 地址={address:X2}]");
        }

        private void cbxDeviceBoardConnectionSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDeviceBoardModeControls();
        }

        private void cbxDeviceBoardNeutralSource_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDeviceBoardModeControls();
        }

        /// <summary>恢复检测或读取状态时，禁用不参与下行报文的模式选择框。</summary>
        private void UpdateDeviceBoardModeControls()
        {
            cbxDeviceBoardConnectionMode.Enabled = cbxDeviceBoardConnectionSource.SelectedIndex == 0;
            cbxDeviceBoardNeutralMode.Enabled = cbxDeviceBoardNeutralSource.SelectedIndex == 0;
        }

        private async void btnStartDailyTiming_Click(object sender, EventArgs e)
        {
            await RunDailyTimingWorkflowAsync();
        }

        /// <summary>按表位逐一发送0x25+01潜动试验启动报文，并严格校验启动应答。</summary>
        private async void btnStartCreepingTest_Click(object sender, EventArgs e)
        {
            if (!TryGetDailyTimingMeterAddresses(out byte[] meterAddresses))
            {
                return;
            }

            SetCreepingTestUiBusy(true);
            try
            {
                Dictionary<byte, byte[]> responses = await SendPacketsAndCollectResponsesAsync(
                    meterAddresses,
                    BuildCreepingTestStartPacket,
                    meterAddress => $"0x25潜动试验启动[表位={meterAddress:X2}]",
                    meterAddress => rawData => TryParseCreepingTestStartResponse(rawData, meterAddress),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);

                byte[] failedAddresses = meterAddresses.Except(responses.Keys).ToArray();
                foreach (byte meterAddress in failedAddresses)
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 开启0x25潜动试验未收到正确应答");
                }

                labelCreepingResult.Text = responses.Count > 0
                    ? $"启动成功：{FormatMeterAddressList(responses.Keys)}"
                    : "启动失败：所有表位均未收到正确应答";
            }
            finally
            {
                SetCreepingTestUiBusy(false);
            }
        }

        /// <summary>发送0x25+AA结果获取报文，并展示各表位当前累计脉冲数。</summary>
        private async void btnGetCreepingTestResult_Click(object sender, EventArgs e)
        {
            if (!TryGetDailyTimingMeterAddresses(out byte[] meterAddresses))
            {
                return;
            }

            SetCreepingTestUiBusy(true);
            try
            {
                Dictionary<byte, byte[]> responses = await SendPacketsAndCollectResponsesAsync(
                    meterAddresses,
                    BuildCreepingTestResultPacket,
                    meterAddress => $"0x25潜动试验结果获取[表位={meterAddress:X2}]",
                    meterAddress => rawData => TryParseCreepingTestResultResponse(rawData, meterAddress, out _),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);

                List<string> resultTexts = new();
                foreach (byte meterAddress in meterAddresses)
                {
                    if (!responses.TryGetValue(meterAddress, out byte[]? response) ||
                        !TryParseCreepingTestResultResponse(response, meterAddress, out uint pulseCount))
                    {
                        PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 获取0x25潜动结果未收到正确应答");
                        continue;
                    }

                    string resultText = $"表位{meterAddress:X2}：实际脉冲数={pulseCount}";
                    resultTexts.Add(resultText);
                    PublishMeterMessage($"潜动走字试验结果，{resultText}");
                }

                labelCreepingResult.Text = resultTexts.Count > 0
                    ? string.Join("；", resultTexts)
                    : "结果获取失败：未收到正确应答";
            }
            finally
            {
                SetCreepingTestUiBusy(false);
            }
        }

        /// <summary>按表位逐一发送0x37+00走字试验启动报文，并严格校验启动应答。</summary>
        private async void btnStartWalkingTest_Click(object sender, EventArgs e)
        {
            await RunWalkingTestSimpleCommandAsync(
                BuildWalkingTestStartPacket,
                meterAddress => $"0x37走字试验开始[表位={meterAddress:X2}]",
                (rawData, meterAddress) => TryParseWalkingTestStartResponse(rawData, meterAddress),
                "开始成功",
                "开始失败");
        }

        /// <summary>按表位逐一发送0x37+FF走字试验停止报文，并严格校验停止应答。</summary>
        private async void btnStopWalkingTest_Click(object sender, EventArgs e)
        {
            await RunWalkingTestSimpleCommandAsync(
                BuildWalkingTestStopPacket,
                meterAddress => $"0x37走字试验停止[表位={meterAddress:X2}]",
                (rawData, meterAddress) => TryParseWalkingTestStopResponse(rawData, meterAddress),
                "停止成功",
                "停止失败");
        }

        /// <summary>发送0x37+AA结果获取报文，并展示被测表脉冲数与标准表电能量。</summary>
        private async void btnGetWalkingTestResult_Click(object sender, EventArgs e)
        {
            if (!TryGetDailyTimingMeterAddresses(out byte[] meterAddresses))
            {
                return;
            }

            SetWalkingTestUiBusy(true);
            try
            {
                Dictionary<byte, byte[]> responses = await SendPacketsAndCollectResponsesAsync(
                    meterAddresses,
                    BuildWalkingTestResultPacket,
                    meterAddress => $"0x37走字试验结果获取[表位={meterAddress:X2}]",
                    meterAddress => rawData => TryParseWalkingTestResultResponse(rawData, meterAddress, out _, out _),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);

                List<string> pulseTexts = new();
                List<string> energyTexts = new();
                foreach (byte meterAddress in meterAddresses)
                {
                    if (!responses.TryGetValue(meterAddress, out byte[]? response) ||
                        !TryParseWalkingTestResultResponse(response, meterAddress, out uint pulseCount, out float standardEnergyKwh))
                    {
                        PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 获取0x37走字结果未收到正确应答");
                        continue;
                    }

                    string energyText = standardEnergyKwh.ToString("0.000000", CultureInfo.InvariantCulture);
                    pulseTexts.Add($"表位{meterAddress:X2}：{pulseCount}");
                    energyTexts.Add($"表位{meterAddress:X2}：{energyText} kWh");
                    PublishMeterMessage($"走字试验结果，表位{meterAddress:X2}，待测表脉冲数={pulseCount}，标准表电能量={energyText} kWh");
                }

                labelWalkingPulseResult.Text = pulseTexts.Count > 0
                    ? $"待测表脉冲数：{string.Join("；", pulseTexts)}"
                    : "待测表脉冲数：未获取";
                labelWalkingEnergyResult.Text = energyTexts.Count > 0
                    ? $"标准表电能量：{string.Join("；", energyTexts)}"
                    : "标准表电能量：未获取";
            }
            finally
            {
                SetWalkingTestUiBusy(false);
            }
        }

        private async void btnStartBasicErrorTest_Click(object sender, EventArgs e)
        {
            await RunBasicErrorWorkflowAsync();
        }

        private async void btnGetBasicErrorTestResult_Click(object sender, EventArgs e)
        {
            if (_basicErrorWorkflowCts != null)
            {
                _basicErrorWorkflowCts.Cancel();
                PublishMeterMessage("已取消基本误差自动等待流程，立即执行手动结果获取");
            }

            await RunBasicErrorCommandAsync(OperationRead);
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
                OperationRead,
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
                OperationStop,
                testTime,
                testCount);
        }

        private async void btnStartVoltageShortCircuitDetection_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterVoltageShortCircuitDetectionCommand,
                "表位电压短路检测[开始检测]",
                OperationExecute);
        }

        private async void btnGetVoltageShortCircuitDetectionResult_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterVoltageShortCircuitDetectionCommand,
                "表位电压短路检测[结果获取]",
                OperationRead);
        }

        private async void btnStartMeterPresenceDetection_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMeterPresenceDetectionCommand,
                "表位有无电表检测[开始检测]",
                OperationExecute);
        }

        private async void btnGetMeterPresenceDetectionResult_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMeterPresenceDetectionCommand,
                "表位有无电表检测[结果获取]",
                OperationRead);
        }

        private async void btnMotorCrimpPress_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMotorCrimpingCommand,
                "电机压接[压接]",
                OperationStart);
        }

        private async void btnMotorCrimpRelease_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMotorCrimpingCommand,
                "电机压接[弹开]",
                OperationExecute);
        }

        private async void btnMotorCrimpPowerOff_Click(object sender, EventArgs e)
        {
            await SendCommandAsync(
                MeterMotorCrimpingCommand,
                "电机压接[电机断电]",
                OperationStop);
        }

        private async Task RunDailyTimingWorkflowAsync()
        {
            if (_dailyTimingWorkflowCts != null)
            {
                PublishMeterMessage("[错误] 日计时试验正在进行中，请勿重复开始");
                return;
            }

            if (!TryGetDailyTimingMeterAddresses(out byte[] meterAddresses) ||
                !TryGetDailyTimingParameters(out byte testTime, out byte testCount))
            {
                return;
            }

            _dailyTimingWorkflowCts = new CancellationTokenSource();
            CancellationToken cancellationToken = _dailyTimingWorkflowCts.Token;
            SetDailyTimingUiBusy(true);

            try
            {
                Dictionary<byte, byte[]> startResponses = await SendPacketsAndCollectResponsesAsync(
                    meterAddresses,
                    meterAddress => BuildMeterPacket(
                        MeterDirectionPcToMcu,
                        meterAddress,
                        MeterDailyTimingCommand,
                        OperationStart,
                        testTime,
                        testCount),
                    meterAddress => $"日计时试验[表位={meterAddress:X2}, 开始, 时间={testTime}s, 次数={testCount}]",
                    meterAddress => rawData => IsExpectedDailyTimingResponse(
                        rawData,
                        meterAddress,
                        OperationStart,
                        testTime,
                        testCount),
                    TimeSpan.FromSeconds(5),
                    cancellationToken);

                byte[] activeMeterAddresses = meterAddresses
                    .Where(startResponses.ContainsKey)
                    .ToArray();

                foreach (byte meterAddress in meterAddresses.Except(activeMeterAddresses))
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 开始日计时后未收到正确应答，已跳过后续流程");
                }

                if (activeMeterAddresses.Length == 0)
                {
                    PublishMeterMessage("[错误] 所有表位开始日计时都未收到正确应答，流程结束");
                    return;
                }

                int waitSeconds = (testTime * testCount) + testCount;
                PublishMeterMessage($"日计时开始应答正常，表位={FormatMeterAddressList(activeMeterAddresses)}，等待 {testTime} * {testCount} + {testCount} = {waitSeconds} 秒后自动获取结果");

                await RunDailyTimingCountdownAsync(waitSeconds, cancellationToken);

                Dictionary<byte, byte[]> resultResponses = await SendPacketsAndCollectResponsesAsync(
                    activeMeterAddresses,
                    meterAddress => BuildMeterPacket(
                        MeterDirectionPcToMcu,
                        meterAddress,
                        MeterDailyTimingCommand,
                        OperationRead,
                        testTime,
                        testCount),
                    meterAddress => $"日计时结果获取[表位={meterAddress:X2}, 时间={testTime}s, 次数={testCount}]",
                    meterAddress => rawData => IsExpectedDailyTimingResponse(
                        rawData,
                        meterAddress,
                        OperationRead,
                        testTime,
                        testCount),
                    TimeSpan.FromSeconds(10),
                    cancellationToken);

                foreach (byte meterAddress in activeMeterAddresses.Except(resultResponses.Keys))
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 自动获取日计时结果后未收到应答");
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

        private async Task<bool> RunBasicErrorCommandAsync(byte actionDataItem)
        {
            if (_meterClient?.IsConnected != true)
            {
                PublishMeterMessage("[错误] 电表TCP客户端未连接");
                return false;
            }

            if (!TryGetBasicErrorMeterAddresses(out byte[] meterAddresses) ||
                !TryGetBasicErrorParameters(out byte[] errorTypes, out ulong standardConstant, out uint meterConstant))
            {
                return false;
            }

            if (IsBasicErrorProtocol38Selected())
            {
                return await RunBasicError38CommandAsync(meterAddresses, errorTypes, standardConstant, meterConstant, actionDataItem);
            }

            if (actionDataItem == OperationExecute)
            {
                foreach (byte errorType in errorTypes)
                {
                    if (!await SendBasicErrorConstantAsync(meterAddresses, errorType, standardConstant, meterConstant))
                    {
                        PublishMeterMessage($"[错误] 误差测试常数下发失败，试验类型={DescribeBasicErrorType(errorType)}");
                        return false;
                    }
                }
            }

            bool hasAnyResponse = false;
            foreach (byte errorType in errorTypes)
            {
                Dictionary<byte, byte[]> responses = await SendPacketsAndCollectResponsesAsync(
                    meterAddresses,
                    meterAddress => BuildMeterPacket(
                        MeterDirectionPcToMcu,
                        meterAddress,
                        MeterBasicErrorCommand,
                        errorType,
                        actionDataItem),
                    meterAddress => $"误差测试[表位={meterAddress:X2}, 类型={DescribeBasicErrorType(errorType)}, 动作={DescribeBasicErrorAction(actionDataItem)}]",
                    meterAddress => rawData => IsExpectedBasicErrorResponse(rawData, meterAddress, errorType, actionDataItem),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);

                foreach (byte meterAddress in meterAddresses.Except(responses.Keys))
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} {DescribeBasicErrorType(errorType)}{DescribeBasicErrorAction(actionDataItem)}未收到应答");
                }

                if (responses.Count > 0)
                {
                    hasAnyResponse = true;
                }
            }

            return hasAnyResponse;
        }

        private async Task RunWalkingTestSimpleCommandAsync(
            Func<byte, byte[]> packetFactory,
            Func<byte, string> packetNameFactory,
            Func<byte[], byte, bool> responseParser,
            string successText,
            string failureText)
        {
            if (!TryGetDailyTimingMeterAddresses(out byte[] meterAddresses))
            {
                return;
            }

            SetWalkingTestUiBusy(true);
            try
            {
                Dictionary<byte, byte[]> responses = await SendPacketsAndCollectResponsesAsync(
                    meterAddresses,
                    packetFactory,
                    packetNameFactory,
                    meterAddress => rawData => responseParser(rawData, meterAddress),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);

                foreach (byte meterAddress in meterAddresses.Except(responses.Keys))
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 走字试验{failureText}：未收到正确应答");
                }

                labelWalkingPulseResult.Text = responses.Count > 0
                    ? $"待测表脉冲数：{successText}，表位={FormatMeterAddressList(responses.Keys)}"
                    : $"待测表脉冲数：{failureText}";
                labelWalkingEnergyResult.Text = responses.Count > 0
                    ? "标准表电能量：等待结果获取"
                    : "标准表电能量：未获取";
            }
            finally
            {
                SetWalkingTestUiBusy(false);
            }
        }

        private async Task RunBasicErrorWorkflowAsync()
        {
            if (_basicErrorWorkflowCts != null)
            {
                PublishMeterMessage("[错误] 基本误差自动流程正在执行中，请勿重复启动");
                return;
            }

            if (_meterClient?.IsConnected != true)
            {
                PublishMeterMessage("[错误] 电表TCP客户端未连接，不能启动基本误差自动流程");
                return;
            }

            if (!TryGetBasicErrorWaitSeconds(out int waitSeconds, out string waitDescription))
            {
                return;
            }

            _basicErrorWorkflowCts = new CancellationTokenSource();
            CancellationToken cancellationToken = _basicErrorWorkflowCts.Token;
            SetBasicErrorUiBusy(true);

            try
            {
                bool startSuccess = await RunBasicErrorCommandAsync(OperationExecute);
                if (!startSuccess)
                {
                    PublishMeterMessage("[错误] 基本误差启动阶段未成功，不进入自动等待和结果获取");
                    return;
                }

                PublishMeterMessage($"基本误差启动报文发送完成，等待 {waitSeconds} 秒后自动获取结果。{waitDescription}");
                await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);
                await RunBasicErrorCommandAsync(OperationRead);
            }
            catch (OperationCanceledException)
            {
                PublishMeterMessage("基本误差自动流程已取消");
            }
            finally
            {
                _basicErrorWorkflowCts.Dispose();
                _basicErrorWorkflowCts = null;
                SetBasicErrorUiBusy(false);
            }
        }

        private async Task<bool> RunBasicError38CommandAsync(
            byte[] meterAddresses,
            byte[] errorTypes,
            ulong standardConstant,
            uint meterConstant,
            byte actionDataItem)
        {
            if (!TryGetBasicError38Parameters(out byte pulseCount, out byte testCount))
            {
                return false;
            }

            if (actionDataItem == OperationExecute)
            {
                foreach (byte errorType in errorTypes)
                {
                    if (!await SendBasicErrorConstantAsync(meterAddresses, errorType, standardConstant, meterConstant))
                    {
                        PublishMeterMessage($"[错误] 误差测试常数下发失败，试验类型={DescribeBasicErrorType(errorType)}");
                        return false;
                    }
                }
            }

            bool hasAnyResponse = false;
            foreach (byte errorType in errorTypes)
            {
                byte pulseType = GetBasicError38PulseType(errorType);
                byte operation = GetBasicError38Operation(actionDataItem);

                Dictionary<byte, byte[]> responses = await SendPacketsAndCollectResponsesAsync(
                    meterAddresses,
                    meterAddress => BuildMeterPacket(
                        MeterDirectionPcToMcu,
                        meterAddress,
                        MeterBasicErrorCommand38,
                        BuildBasicError38Payload(operation, pulseCount, testCount, pulseType)),
                    meterAddress => BuildBasicError38PacketName(meterAddress, errorType, operation, pulseCount, testCount),
                    meterAddress => rawData => IsExpectedBasicError38Response(rawData, meterAddress, operation, pulseCount, testCount, pulseType),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);

                foreach (byte meterAddress in meterAddresses.Except(responses.Keys))
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} {DescribeBasicErrorType(errorType)}{DescribeBasicErrorAction(actionDataItem)}未收到0x38应答");
                }

                if (responses.Count > 0)
                {
                    hasAnyResponse = true;
                }
            }

            return hasAnyResponse;
        }

        private async Task<Dictionary<byte, byte[]>> SendPacketsAndCollectResponsesAsync(
            IEnumerable<byte> meterAddresses,
            Func<byte, byte[]> packetFactory,
            Func<byte, string> packetNameFactory,
            Func<byte, Func<byte[], bool>> responsePredicateFactory,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            byte[] meterAddressArray = meterAddresses.ToArray();
            Dictionary<byte, (CancellationTokenSource ResponseCts, Task<byte?> Placeholder, Task<byte[]?> ResponseTask)> pending =
                new();

            foreach (byte meterAddress in meterAddressArray)
            {
                CancellationTokenSource responseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                Task<byte[]?> responseTask = WaitForMeterResponseAsync(
                    responsePredicateFactory(meterAddress),
                    timeout,
                    responseCts.Token);
                pending[meterAddress] = (responseCts, Task.FromResult<byte?>(null), responseTask);
            }

            try
            {
                for (int index = 0; index < meterAddressArray.Length; index++)
                {
                    byte meterAddress = meterAddressArray[index];
                    bool sendSuccess = await SendMeterPacketAsync(
                        packetFactory(meterAddress),
                        packetNameFactory(meterAddress));

                    if (!sendSuccess && pending.TryGetValue(meterAddress, out var pendingItem))
                    {
                        pendingItem.ResponseCts.Cancel();
                    }

                    if (index < meterAddressArray.Length - 1)
                    {
                        await Task.Delay(MultiMeterPacketInterval, cancellationToken);
                    }
                }

                Dictionary<byte, byte[]> responses = new();
                foreach ((byte meterAddress, var pendingItem) in pending)
                {
                    try
                    {
                        byte[]? response = await pendingItem.ResponseTask;
                        if (response != null)
                        {
                            responses[meterAddress] = response;
                        }
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                    }
                }

                return responses;
            }
            finally
            {
                foreach (var pendingItem in pending.Values)
                {
                    pendingItem.ResponseCts.Dispose();
                }
            }
        }

        private async Task<bool> SendBasicErrorConstantAsync(byte[] meterAddresses, byte errorType, ulong standardConstant, uint meterConstant)
        {
            byte standardCommand = errorType switch
            {
                0x02 => 0xA3,
                _ => 0xA2
            };

            byte meterCommand = errorType switch
            {
                0x02 => 0xA1,
                _ => 0xA0
            };

            foreach (byte meterAddress in meterAddresses)
            {
                byte[] standardPacket = BuildMeterPacket(
                    MeterDirectionPcToMcu,
                    meterAddress,
                    standardCommand,
                    BitConverter.GetBytes(standardConstant));
                byte[]? standardResponse = await SendPacketAndWaitForResponseAsync(
                    standardPacket,
                    $"设置标准表常数[表位={meterAddress:X2}, {DescribeBasicErrorType(errorType)}={standardConstant}]",
                    rawData => IsExpectedWriteCommandResponseOrFeedback(rawData, meterAddress, standardCommand),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);
                if (standardResponse == null)
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 设置标准表常数未收到应答");
                    return false;
                }

                if (IsFeedbackPacketForMeter(standardResponse, meterAddress))
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 设置标准表常数收到反馈错误");
                    return false;
                }

                await Task.Delay(MultiMeterPacketInterval);
            }

            foreach (byte meterAddress in meterAddresses)
            {
                byte[] meterPacket = BuildMeterPacket(
                    MeterDirectionPcToMcu,
                    meterAddress,
                    meterCommand,
                    BitConverter.GetBytes(meterConstant));
                byte[]? meterResponse = await SendPacketAndWaitForResponseAsync(
                    meterPacket,
                    $"设置电能表常数[表位={meterAddress:X2}, {DescribeBasicErrorType(errorType)}={meterConstant}]",
                    rawData => IsExpectedWriteCommandResponseOrFeedback(rawData, meterAddress, meterCommand),
                    TimeSpan.FromSeconds(5),
                    CancellationToken.None);
                if (meterResponse == null)
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 设置电能表常数未收到应答");
                    return false;
                }

                if (IsFeedbackPacketForMeter(meterResponse, meterAddress))
                {
                    PublishMeterMessage($"[错误] 表位 {meterAddress:X2} 设置电能表常数收到反馈错误");
                    return false;
                }

                await Task.Delay(MultiMeterPacketInterval);
            }

            return true;
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

        private async Task HandlePhaseControlAsync(PhaseControlConfig config, bool isEnableAction)
        {
            if (!TryGetReadyMeterAddress(out byte meterAddress))
            {
                return;
            }

            if (!TryGetPhaseControlDataItem(config, isEnableAction, out byte dataItem, out string phaseDescription))
            {
                return;
            }

            string actionText = GetPhaseActionText(config.CommandCode, isEnableAction);
            byte[] packet = BuildMeterPacket(MeterDirectionPcToMcu, meterAddress, config.CommandCode, dataItem);
            await SendMeterPacketAsync(packet, $"{config.CategoryLabel}[{phaseDescription}{actionText}]");
        }

        private static string GetPhaseActionText(byte commandCode, bool isEnableAction)
        {
            return commandCode switch
            {
                MeterAcVoltageCommand => isEnableAction ? "上电" : "下电",
                MeterAcCurrentCommand => isEnableAction ? "通电流" : "断电流",
                _ => string.Empty
            };
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

            if (!TryParseMeterAddress(tbxMeterV2Addr.Text, out meterAddress))
            {
                PublishMeterMessage("[错误] 表位地址格式不正确，请输入 1-254 或 FF");
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

        private bool TryGetDailyTimingMeterAddresses(out byte[] meterAddresses)
        {
            meterAddresses = Array.Empty<byte>();

            if (_meterClient?.IsConnected != true)
            {
                PublishMeterMessage("[错误] 电表TCP客户端未连接");
                return false;
            }

            string addressText = tbxMeterV2Addr.Text.Trim();
            if (TryParseDailyTimingMeterAddressRange(addressText, out meterAddresses))
            {
                return true;
            }

            if (TryParseMeterAddress(addressText, out byte meterAddress))
            {
                meterAddresses = new[] { meterAddress };
                return true;
            }

            PublishMeterMessage("[错误] 日计时表位地址格式不正确，请输入单个地址、范围(如 1-48 / 01-03) 或列表(如 1,2,3)");
            return false;
        }

        private bool TryGetBasicErrorMeterAddresses(out byte[] meterAddresses)
        {
            meterAddresses = Array.Empty<byte>();

            if (_meterClient?.IsConnected != true)
            {
                PublishMeterMessage("[错误] 电表TCP客户端未连接");
                return false;
            }

            string addressText = tbxMeterV2Addr.Text.Trim();
            if (TryParseDailyTimingMeterAddressRange(addressText, out meterAddresses))
            {
                return true;
            }

            if (TryParseMeterAddress(addressText, out byte meterAddress))
            {
                meterAddresses = new[] { meterAddress };
                return true;
            }

            PublishMeterMessage("[错误] 误差测试表位地址格式不正确，请输入单个地址、范围(如 1-48 / 01-03) 或列表(如 1,2,3)");
            return false;
        }

        private bool TryGetBasicErrorParameters(out byte[] errorTypes, out ulong standardConstant, out uint meterConstant)
        {
            errorTypes = Array.Empty<byte>();
            standardConstant = 0;
            meterConstant = 0;

            RefreshBasicErrorConstants();

            if (!ulong.TryParse(tbxBasicErrorStandardConstant.Text.Trim(), out standardConstant) || standardConstant == 0)
            {
                PublishMeterMessage("[错误] 标准表常数不合法");
                return false;
            }

            if (!uint.TryParse(tbxBasicErrorMeterConstant.Text.Trim(), out meterConstant) || meterConstant == 0)
            {
                PublishMeterMessage("[错误] 电能表常数不合法");
                return false;
            }

            byte selectedType = GetBasicErrorTypeCode();
            errorTypes = selectedType == 0x03 ? new byte[] { 0x01, 0x02 } : new[] { selectedType };
            return true;
        }

        private bool TryGetBasicError38Parameters(out byte pulseCount, out byte testCount)
        {
            pulseCount = 0;
            testCount = 0;

            if (!byte.TryParse(tbxBasicErrorPulseCount.Text.Trim(), out pulseCount) || pulseCount < 1 || pulseCount > 255)
            {
                PublishMeterMessage("[错误] 0x38脉冲数只能填写 1-255");
                return false;
            }

            if (!byte.TryParse(tbxBasicErrorTestCount.Text.Trim(), out testCount) || testCount < 1 || testCount > 10)
            {
                PublishMeterMessage("[错误] 0x38试验次数只能填写 1-10");
                return false;
            }

            return true;
        }

        private void RefreshBasicErrorConstants()
        {
            if (!ErrorTestConstantHelper.TryCalculateConstants(
                    tbxBasicErrorVoltage.Text,
                    tbxBasicErrorCurrent.Text,
                    out ulong standardConstant,
                    out uint meterConstant))
            {
                return;
            }

            tbxBasicErrorStandardConstant.Text = standardConstant.ToString(CultureInfo.InvariantCulture);
            if (string.IsNullOrWhiteSpace(tbxBasicErrorMeterConstant.Text))
            {
                tbxBasicErrorMeterConstant.Text = meterConstant.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void UpdateBasicErrorProtocolUi()
        {
            bool use38 = IsBasicErrorProtocol38Selected();
            tbxBasicErrorPulseCount.Enabled = use38;
            tbxBasicErrorTestCount.Enabled = use38;
            labelBasicErrorPulseCount.Enabled = use38;
            labelBasicErrorTestCount.Enabled = use38;
        }

        private void SetBasicErrorUiBusy(bool isBusy)
        {
            btnStartBasicErrorTest.Enabled = !isBusy;
            cbxBasicErrorProtocol21.Enabled = !isBusy;
            cbxBasicErrorProtocol38.Enabled = !isBusy;
            cbxBasicErrorType.Enabled = !isBusy;
            tbxBasicErrorVoltage.Enabled = !isBusy;
            tbxBasicErrorCurrent.Enabled = !isBusy;
            tbxBasicErrorStandardConstant.Enabled = !isBusy;
            tbxBasicErrorMeterConstant.Enabled = !isBusy;
            tbxBasicErrorPulseCount.Enabled = !isBusy && IsBasicErrorProtocol38Selected();
            tbxBasicErrorTestCount.Enabled = !isBusy && IsBasicErrorProtocol38Selected();
        }

        private bool TryGetBasicErrorWaitSeconds(out int waitSeconds, out string waitDescription)
        {
            waitSeconds = 0;
            waitDescription = string.Empty;

            if (!TryGetBasicErrorParameters(out byte[] errorTypes, out _, out uint meterConstant))
            {
                return false;
            }

            if (!ErrorTestConstantHelper.TryParseInputNumber(tbxBasicErrorVoltage.Text.Trim(), out double voltage) ||
                !ErrorTestConstantHelper.TryParseInputNumber(tbxBasicErrorCurrent.Text.Trim(), out double current))
            {
                PublishMeterMessage("[错误] 误差测试电压或电流格式不正确，无法计算自动等待时间");
                return false;
            }

            decimal voltageValue = Convert.ToDecimal(voltage);
            decimal currentValue = Convert.ToDecimal(current);
            decimal power = voltageValue * currentValue;
            if (power <= 0)
            {
                PublishMeterMessage("[错误] 误差测试功率计算结果无效，无法计算自动等待时间");
                return false;
            }

            decimal pulseCount = 1m;
            decimal testCount = 1m;
            if (IsBasicErrorProtocol38Selected())
            {
                if (!TryGetBasicError38Parameters(out byte pulseCountValue, out byte testCountValue))
                {
                    return false;
                }

                pulseCount = pulseCountValue;
                testCount = testCountValue;
            }

            decimal singleTestSeconds = (3600000m * pulseCount) / (meterConstant * power);
            decimal totalWaitSeconds = (singleTestSeconds * testCount) + 10m;
            if (errorTypes.Length > 1)
            {
                totalWaitSeconds *= errorTypes.Length;
            }

            waitSeconds = Math.Max(1, (int)decimal.Ceiling(totalWaitSeconds));
            waitDescription =
                $"等待时间按 T+10s 计算：T={(singleTestSeconds * testCount).ToString("0.###", CultureInfo.InvariantCulture)}s" +
                (errorTypes.Length > 1 ? $"，试验类型数={errorTypes.Length}" : string.Empty) +
                $"，总等待={waitSeconds}s。";

            if (!IsBasicErrorProtocol38Selected())
            {
                waitDescription += " 0x21 协议当前按 N=1 估算等待时间。";
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
            byte[] payload = (dataItems == null || dataItems.Length == 0) ? new[] { OperationStart } : dataItems;
            int dataLength = 2 + 1 + 1 + 1 + 1 + payload.Length + 1;
            byte[] packet = new byte[2 + dataLength + 2];

            packet[0] = MeterFrameStart1;
            packet[1] = MeterFrameStart2;
            packet[2] = (byte)(dataLength & 0xFF);
            packet[3] = (byte)((dataLength >> 8) & 0xFF);
            packet[4] = direction;
            packet[5] = meterAddress;
            packet[6] = MeterControlProtocol;
            packet[7] = command;
            Array.Copy(payload, 0, packet, 8, payload.Length);

            packet[8 + payload.Length] = CalculateChecksum(packet, 2, dataLength - 1);
            packet[9 + payload.Length] = MeterFrameStop1;
            packet[10 + payload.Length] = MeterFrameStop2;
            return packet;
        }

        /// <summary>构造V2电表0x25+01潜动试验启动报文。</summary>
        public static byte[] BuildCreepingTestStartPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterCreepingTestCommand,
                OperationExecute);
        }

        /// <summary>构造V2电表0x25+AA潜动试验结果获取报文。</summary>
        public static byte[] BuildCreepingTestResultPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterCreepingTestCommand,
                OperationRead);
        }

        /// <summary>校验V2电表0x25启动应答；数据项必须严格为单字节01。</summary>
        public static bool TryParseCreepingTestStartResponse(
            byte[] rawData,
            byte expectedMeterAddress)
        {
            return TryGetMeterPacketDataItems(
                    rawData,
                    expectedMeterAddress,
                    MeterCreepingTestCommand,
                    out byte[] dataItems) &&
                dataItems.Length == 1 &&
                dataItems[0] == OperationExecute;
        }

        /// <summary>校验并解析V2电表0x25结果应答：AA后4字节为小端uint实际脉冲数。</summary>
        public static bool TryParseCreepingTestResultResponse(
            byte[] rawData,
            byte expectedMeterAddress,
            out uint pulseCount)
        {
            pulseCount = 0;
            if (!TryGetMeterPacketDataItems(
                    rawData,
                    expectedMeterAddress,
                    MeterCreepingTestCommand,
                    out byte[] dataItems) ||
                dataItems.Length != 5 ||
                dataItems[0] != OperationRead)
            {
                return false;
            }

            pulseCount = BinaryPrimitives.ReadUInt32LittleEndian(dataItems.AsSpan(1, sizeof(uint)));
            return true;
        }

        /// <summary>构造V2电表0x37+00走字试验启动报文。</summary>
        public static byte[] BuildWalkingTestStartPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterWalkingTestCommand,
                OperationStart);
        }

        /// <summary>构造V2电表0x37+FF走字试验停止报文。</summary>
        public static byte[] BuildWalkingTestStopPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterWalkingTestCommand,
                OperationStop);
        }

        /// <summary>构造V2电表0x37+AA走字试验结果获取报文。</summary>
        public static byte[] BuildWalkingTestResultPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterWalkingTestCommand,
                OperationRead);
        }

        /// <summary>校验V2电表0x37启动应答；数据项必须严格为单字节00。</summary>
        public static bool TryParseWalkingTestStartResponse(
            byte[] rawData,
            byte expectedMeterAddress)
        {
            return TryGetMeterPacketDataItems(
                    rawData,
                    expectedMeterAddress,
                    MeterWalkingTestCommand,
                    out byte[] dataItems) &&
                dataItems.Length == 1 &&
                dataItems[0] == OperationStart;
        }

        /// <summary>校验V2电表0x37停止应答；数据项必须严格为单字节FF。</summary>
        public static bool TryParseWalkingTestStopResponse(
            byte[] rawData,
            byte expectedMeterAddress)
        {
            return TryGetMeterPacketDataItems(
                    rawData,
                    expectedMeterAddress,
                    MeterWalkingTestCommand,
                    out byte[] dataItems) &&
                dataItems.Length == 1 &&
                dataItems[0] == OperationStop;
        }

        /// <summary>
        /// 校验V2电表0x37结果应答是否为完整的读取报文。
        /// 该方法只检查帧格式、表位地址、命令字和数据项结构，不判断标准表电能量是否有效。
        /// </summary>
        public static bool TryGetWalkingTestResultResponse(
            byte[] rawData,
            byte expectedMeterAddress,
            out byte[] dataItems)
        {
            return TryGetMeterPacketDataItems(
                    rawData,
                    expectedMeterAddress,
                    MeterWalkingTestCommand,
                    out dataItems) &&
                dataItems.Length == 9 &&
                dataItems[0] == OperationRead;
        }

        /// <summary>校验并解析V2电表0x37结果应答：AA后4字节uint脉冲数，再后4字节float标准表电能量(kWh)。</summary>
        public static bool TryParseWalkingTestResultResponse(
            byte[] rawData,
            byte expectedMeterAddress,
            out uint pulseCount,
            out float standardEnergyKwh)
        {
            return TryParseWalkingTestResultResponse(
                rawData,
                expectedMeterAddress,
                out pulseCount,
                out standardEnergyKwh,
                out _);
        }

        /// <summary>
        /// 校验并解析V2电表0x37结果应答，并把标准表电能量是否有效作为参考状态单独返回。
        /// 常数试验结论只依赖待测表脉冲数，标准表电能量为NaN/Infinity时不影响脉冲数读取。
        /// </summary>
        public static bool TryParseWalkingTestResultResponse(
            byte[] rawData,
            byte expectedMeterAddress,
            out uint pulseCount,
            out float standardEnergyKwh,
            out bool standardEnergyValid,
            out string diagnosticMessage)
        {
            pulseCount = 0;
            standardEnergyKwh = 0;
            standardEnergyValid = false;
            diagnosticMessage = string.Empty;
            if (!TryGetWalkingTestResultResponse(rawData, expectedMeterAddress, out byte[] dataItems))
            {
                diagnosticMessage = "报文不是0x37+AA读取结果应答，或长度/校验/表位不匹配。";
                return false;
            }

            pulseCount = BinaryPrimitives.ReadUInt32LittleEndian(dataItems.AsSpan(1, sizeof(uint)));
            standardEnergyKwh = BinaryPrimitives.ReadSingleLittleEndian(dataItems.AsSpan(5, sizeof(float)));
            standardEnergyValid = !float.IsNaN(standardEnergyKwh) && !float.IsInfinity(standardEnergyKwh);
            if (!standardEnergyValid)
            {
                string rawFloatBytes = BitConverter.ToString(dataItems, 5, sizeof(float)).Replace('-', ' ');
                diagnosticMessage = $"标准表电能量参考值无效：raw={rawFloatBytes}，解析值={standardEnergyKwh}。";
            }

            return true;
        }

        /// <summary>
        /// 校验并解析V2电表0x37结果应答，并在标准表电能量无效时返回明确的诊断信息。
        /// 该兼容入口保持旧语义：脉冲数和标准表电能量都有效才返回true。
        /// </summary>
        public static bool TryParseWalkingTestResultResponse(
            byte[] rawData,
            byte expectedMeterAddress,
            out uint pulseCount,
            out float standardEnergyKwh,
            out string errorMessage)
        {
            bool parsed = TryParseWalkingTestResultResponse(
                rawData,
                expectedMeterAddress,
                out pulseCount,
                out standardEnergyKwh,
                out bool standardEnergyValid,
                out errorMessage);
            return parsed && standardEnergyValid;
        }

        /// <summary>构造V2电表0x86检测单元短路检测启动报文。</summary>
        public static byte[] BuildShortCircuitDetectionStartPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterVoltageShortCircuitDetectionCommand,
                OperationExecute);
        }

        /// <summary>构造V2电表0x86检测单元短路检测结果读取报文。</summary>
        public static byte[] BuildShortCircuitDetectionResultPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterVoltageShortCircuitDetectionCommand,
                OperationRead);
        }

        /// <summary>
        /// 校验并解析0x86短路检测应答。启动应答只回显01，结果应答为AA+结果码。
        /// 结果码00表示电压线路正常，其余结果码表示对应相线存在短路。
        /// </summary>
        public static bool TryParseShortCircuitDetectionResponse(
            byte[] rawData,
            byte meterAddress,
            byte expectedOperation,
            out byte resultCode,
            out string description)
        {
            resultCode = 0x00;
            description = string.Empty;
            if (!TryGetMeterPacketDataItems(
                    rawData,
                    meterAddress,
                    MeterVoltageShortCircuitDetectionCommand,
                    out byte[] dataItems))
            {
                description = "0x86应答帧格式、地址、协议类型、命令码或校验和错误。";
                return false;
            }

            if (expectedOperation == OperationExecute)
            {
                if (dataItems.Length != 1 || dataItems[0] != OperationExecute)
                {
                    description = "0x86启动应答未正确回显01。";
                    return false;
                }

                description = "检测单元短路检测启动应答正常。";
                return true;
            }

            if (expectedOperation != OperationRead ||
                dataItems.Length != 2 ||
                dataItems[0] != OperationRead)
            {
                description = "0x86结果应答格式错误，期望AA+结果码。";
                return false;
            }

            resultCode = dataItems[1];
            description = GetShortCircuitDetectionResultDescription(resultCode);
            return true;
        }

        /// <summary>构造V2电表0x84检测单元断路检测启动报文。</summary>
        public static byte[] BuildOpenCircuitDetectionStartPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterMeterPresenceDetectionCommand,
                OperationExecute);
        }

        /// <summary>构造V2电表0x84检测单元断路检测结果读取报文。</summary>
        public static byte[] BuildOpenCircuitDetectionResultPacket(byte meterAddress)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterMeterPresenceDetectionCommand,
                OperationRead);
        }

        /// <summary>
        /// 校验并解析0x84断路检测应答。结果码01表示有电表且电流线路正常。
        /// </summary>
        public static bool TryParseOpenCircuitDetectionResponse(
            byte[] rawData,
            byte meterAddress,
            byte expectedOperation,
            out byte resultCode,
            out string description)
        {
            resultCode = 0x00;
            description = string.Empty;
            if (!TryGetMeterPacketDataItems(
                    rawData,
                    meterAddress,
                    MeterMeterPresenceDetectionCommand,
                    out byte[] dataItems))
            {
                description = "0x84应答帧格式、地址、协议类型、命令码或校验和错误。";
                return false;
            }

            if (expectedOperation == OperationExecute)
            {
                if (dataItems.Length != 1 || dataItems[0] != OperationExecute)
                {
                    description = "0x84启动应答未正确回显01。";
                    return false;
                }

                description = "检测单元断路检测启动应答正常。";
                return true;
            }

            if (expectedOperation != OperationRead ||
                dataItems.Length != 2 ||
                dataItems[0] != OperationRead)
            {
                description = "0x84结果应答格式错误，期望AA+结果码。";
                return false;
            }

            resultCode = dataItems[1];
            description = GetOpenCircuitDetectionResultDescription(resultCode);
            return true;
        }

        /// <summary>构造V2电表0xCA温度读取报文：CA+传感器序号+AA。</summary>
        public static byte[] BuildTemperatureReadPacket(byte meterAddress, byte sensorIndex)
        {
            ValidateTemperatureSensorIndex(sensorIndex);
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterTemperatureCommand,
                sensorIndex,
                OperationRead);
        }

        /// <summary>构造V2电表0xCA温度校准报文，温度值按4字节有符号小端编码。</summary>
        public static byte[] BuildTemperatureCalibrationPacket(byte meterAddress, byte sensorIndex, int temperatureValue)
        {
            ValidateTemperatureSensorIndex(sensorIndex);
            byte[] valueBytes = new byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(valueBytes, temperatureValue);
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterTemperatureCommand,
                new[] { sensorIndex, OperationExecute }.Concat(valueBytes).ToArray());
        }

        /// <summary>构造V2电表0xCA删除温度校准值报文：CA+传感器序号+FF+00。</summary>
        public static byte[] BuildTemperatureCalibrationDeletePacket(byte meterAddress, byte sensorIndex)
        {
            ValidateTemperatureSensorIndex(sensorIndex);
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterTemperatureCommand,
                sensorIndex,
                OperationStop,
                0x00);
        }

        /// <summary>
        /// 校验并解析0xCA温度读取应答。协议未定义缩放比例，因此返回4字节有符号原始值。
        /// </summary>
        public static bool TryParseTemperatureReadResponse(
            byte[] rawData,
            byte meterAddress,
            byte expectedSensorIndex,
            out int temperatureRawValue,
            out string description)
        {
            temperatureRawValue = 0;
            description = string.Empty;
            if (!TryGetMeterPacketDataItems(rawData, meterAddress, MeterTemperatureCommand, out byte[] dataItems))
            {
                description = "0xCA应答帧格式、地址、协议类型、命令码或校验和错误。";
                return false;
            }

            if (dataItems.Length != 6 ||
                dataItems[0] != expectedSensorIndex ||
                dataItems[1] != OperationRead)
            {
                description = $"0xCA读取应答格式错误，期望传感器{expectedSensorIndex}+AA+4字节温度值。";
                return false;
            }

            temperatureRawValue = BinaryPrimitives.ReadInt32LittleEndian(dataItems.AsSpan(2, sizeof(int)));
            description = $"温度传感器{expectedSensorIndex}读取正常，温度原始值={temperatureRawValue}。";
            return true;
        }

        /// <summary>返回0x86结果码对应的短路位置说明。</summary>
        public static string GetShortCircuitDetectionResultDescription(byte resultCode)
        {
            return resultCode switch
            {
                0x00 => "电压线路正常",
                0x01 => "A相电压短路",
                0x02 => "B相电压短路",
                0x03 => "A、B与N短路",
                0x04 => "C相电压短路",
                0x05 => "A、C与N短路",
                0x06 => "B、C与N短路",
                0x07 => "三相电压都短路",
                _ => $"未知短路检测结果0x{resultCode:X2}"
            };
        }

        /// <summary>返回0x84结果码对应的断路检测说明。</summary>
        public static string GetOpenCircuitDetectionResultDescription(byte resultCode)
        {
            return resultCode switch
            {
                0x00 => "无电表，电流线路可能断路",
                0x01 => "有电表，电流线路正常",
                0x02 => "短接磁保持继电器短路异常",
                _ => $"未知断路检测结果0x{resultCode:X2}"
            };
        }

        private static void ValidateTemperatureSensorIndex(byte sensorIndex)
        {
            if (sensorIndex == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sensorIndex), "温度传感器序号必须从1开始。");
            }
        }

        /// <summary>构造V2电表标准表常数设置报文，常数按8字节小端编码；有功使用A2，无功使用A3。</summary>
        public static byte[] BuildBasicErrorStandardConstantPacket(byte meterAddress, ulong standardConstant, bool reactive = false)
        {
            byte[] payload = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(payload, standardConstant);
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                reactive ? (byte)0xA3 : (byte)0xA2,
                payload);
        }

        /// <summary>构造V2电表被测表常数设置报文，常数按4字节小端编码；有功使用A0，无功使用A1。</summary>
        public static byte[] BuildBasicErrorMeterConstantPacket(byte meterAddress, uint meterConstant, bool reactive = false)
        {
            byte[] payload = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(payload, meterConstant);
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                reactive ? (byte)0xA1 : (byte)0xA0,
                payload);
        }

        /// <summary>构造V2电表0x38基本误差启动报文：00+脉冲数+次数+脉冲类型。</summary>
        public static byte[] BuildBasicError38StartPacket(byte meterAddress, byte pulseCount, byte testCount, byte pulseType = 0x00)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterBasicErrorCommand38,
                OperationStart,
                pulseCount,
                testCount,
                pulseType);
        }

        /// <summary>构造V2电表0x38基本误差结果读取报文：AA+脉冲数+次数。</summary>
        public static byte[] BuildBasicError38ResultPacket(byte meterAddress, byte pulseCount, byte testCount)
        {
            return BuildMeterPacket(
                MeterDirectionPcToMcu,
                meterAddress,
                MeterBasicErrorCommand38,
                OperationRead,
                pulseCount,
                testCount);
        }

        /// <summary>校验A2/A0设置命令应答是否完整回显了期望数据项。</summary>
        public static bool IsExpectedBasicErrorSettingResponse(
            byte[] rawData,
            byte meterAddress,
            byte command,
            byte[] expectedDataItems)
        {
            return TryGetMeterPacketDataItems(rawData, meterAddress, command, out byte[] dataItems) &&
                dataItems.SequenceEqual(expectedDataItems);
        }

        /// <summary>校验0x38启动应答是否完整回显操作码、脉冲数、次数和脉冲类型。</summary>
        public static bool IsExpectedBasicError38StartResponse(
            byte[] rawData,
            byte meterAddress,
            byte pulseCount,
            byte testCount,
            byte pulseType = 0x00)
        {
            return TryGetMeterPacketDataItems(rawData, meterAddress, MeterBasicErrorCommand38, out byte[] dataItems) &&
                dataItems.SequenceEqual(new[] { OperationStart, pulseCount, testCount, pulseType });
        }

        /// <summary>
        /// 解析0x38+AA结果应答。每个结果为4字节小端float；试验未全部完成时允许少于配置次数。
        /// </summary>
        public static bool TryParseBasicError38ResultResponse(
            byte[] rawData,
            byte meterAddress,
            byte pulseCount,
            byte testCount,
            out IReadOnlyList<float> results,
            out string message)
        {
            results = Array.Empty<float>();
            message = string.Empty;
            if (!TryGetMeterPacketDataItems(rawData, meterAddress, MeterBasicErrorCommand38, out byte[] dataItems))
            {
                message = "报文帧格式、方向、协议类型、命令码或校验和错误。";
                return false;
            }

            if (dataItems.Length < 3 ||
                dataItems[0] != OperationRead ||
                dataItems[1] != pulseCount ||
                dataItems[2] != testCount)
            {
                message = $"结果头不匹配，期望AA {pulseCount:X2} {testCount:X2}。";
                return false;
            }

            int resultDataLength = dataItems.Length - 3;
            if (resultDataLength < 4 || resultDataLength % 4 != 0)
            {
                message = $"误差数据长度{resultDataLength}不是有效float长度。";
                return false;
            }

            int resultCount = resultDataLength / 4;
            if (resultCount > testCount)
            {
                message = $"返回结果数量{resultCount}超过配置试验次数{testCount}。";
                return false;
            }

            List<float> parsedResults = new(resultCount);
            for (int index = 3; index < dataItems.Length; index += 4)
            {
                int bits = BinaryPrimitives.ReadInt32LittleEndian(dataItems.AsSpan(index, 4));
                float value = BitConverter.Int32BitsToSingle(bits);
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    message = $"第{parsedResults.Count + 1}个误差结果不是有效float。";
                    return false;
                }

                parsedResults.Add(value);
            }

            results = parsedResults;
            message = $"已解析{parsedResults.Count}/{testCount}个误差结果。";
            return true;
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
            if (normalized.Equals("FF", StringComparison.OrdinalIgnoreCase))
            {
                meterAddress = 0xFF;
                return true;
            }

            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[2..];
            }

            if (byte.TryParse(normalized, out byte decimalAddress) &&
                (decimalAddress is >= 1 and <= 254 || decimalAddress == 255))
            {
                meterAddress = decimalAddress;
                return true;
            }

            if (byte.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte hexAddress) &&
                (hexAddress is >= 0x01 and <= 0xFE || hexAddress == 0xFF))
            {
                meterAddress = hexAddress;
                return true;
            }

            return false;
        }

        /// <summary>解析装置通信板地址，只允许板地址00或广播地址FF。</summary>
        private bool TryGetDeviceBoardAddress(out byte address)
        {
            address = 0x00;
            string value = tbxDeviceBoardAddress.Text.Trim();
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                value = value[2..];
            }

            if (value is "0" or "00")
            {
                return true;
            }

            if (value.Equals("FF", StringComparison.OrdinalIgnoreCase) || value == "255")
            {
                address = 0xFF;
                return true;
            }

            PublishMeterMessage("[错误] 装置通信板地址只能填写00（通信板）或FF（广播全部设备）");
            return false;
        }

        private static bool TryParseDailyTimingMeterAddressRange(string addressText, out byte[] meterAddresses)
        {
            meterAddresses = Array.Empty<byte>();
            if (string.IsNullOrWhiteSpace(addressText))
            {
                return false;
            }

            string normalized = addressText.Trim().Replace(" ", string.Empty);
            if (normalized.Contains(','))
            {
                string[] parts = normalized.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 0)
                {
                    return false;
                }

                List<byte> parsedAddresses = new();
                foreach (string part in parts)
                {
                    if (!TryParseDailyTimingDecimalAddress(part, out byte address))
                    {
                        return false;
                    }

                    parsedAddresses.Add(address);
                }

                meterAddresses = parsedAddresses.Distinct().OrderBy(address => address).ToArray();
                return meterAddresses.Length > 0;
            }

            string[] rangeParts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (rangeParts.Length == 2)
            {
                if (!TryParseDailyTimingDecimalAddress(rangeParts[0], out byte startAddress) ||
                    !TryParseDailyTimingDecimalAddress(rangeParts[1], out byte endAddress) ||
                    startAddress > endAddress)
                {
                    return false;
                }

                meterAddresses = Enumerable.Range(startAddress, endAddress - startAddress + 1)
                    .Select(value => (byte)value)
                    .ToArray();
                return meterAddresses.Length > 0;
            }

            return false;
        }

        private static bool TryParseDailyTimingDecimalAddress(string addressText, out byte address)
        {
            address = 0x00;
            if (!int.TryParse(addressText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                return false;
            }

            if (parsed < 1 || parsed > 48)
            {
                return false;
            }

            address = (byte)parsed;
            return true;
        }

        private static string FormatMeterAddressList(IEnumerable<byte> meterAddresses)
        {
            return string.Join(",", meterAddresses.Select(address => address.ToString("X2")));
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

            return operation != OperationRead || dataItems.Length >= 3;
        }

        private static bool IsExpectedBasicErrorResponse(
            byte[] rawData,
            byte meterAddress,
            byte errorType,
            byte actionDataItem)
        {
            if (!TryGetMeterPacketDataItems(rawData, meterAddress, MeterBasicErrorCommand, out byte[] dataItems))
            {
                return false;
            }

            if (dataItems.Length < 2 ||
                dataItems[0] != errorType ||
                dataItems[1] != actionDataItem)
            {
                return false;
            }

            return actionDataItem != OperationRead || dataItems.Length >= 6;
        }

        private static bool IsExpectedBasicError38Response(
            byte[] rawData,
            byte meterAddress,
            byte operation,
            byte pulseCount,
            byte testCount,
            byte pulseType)
        {
            if (!TryGetMeterPacketDataItems(rawData, meterAddress, MeterBasicErrorCommand38, out byte[] dataItems))
            {
                return false;
            }

            if (dataItems.Length < 3 ||
                dataItems[0] != operation ||
                dataItems[1] != pulseCount ||
                dataItems[2] != testCount)
            {
                return false;
            }

            if (operation == OperationStart)
            {
                return dataItems.Length >= 4 && dataItems[3] == pulseType;
            }

            if (operation == OperationRead)
            {
                return true;
            }

            return operation == OperationStop;
        }

        private static bool IsExpectedWriteCommandResponseOrFeedback(byte[] rawData, byte meterAddress, byte command)
        {
            return TryGetMeterPacketDataItems(rawData, meterAddress, command, out _) ||
                   IsFeedbackPacketForMeter(rawData, meterAddress);
        }

        private static bool IsFeedbackPacketForMeter(byte[] rawData, byte meterAddress)
        {
            return TryGetMeterPacketDataItems(rawData, out byte command, out _) &&
                   command == MeterFeedbackCommand &&
                   rawData[5] == meterAddress;
        }

        private static bool TryGetMeterPacketDataItems(byte[] rawData, byte meterAddress, byte command, out byte[] dataItems)
        {
            dataItems = Array.Empty<byte>();
            if (rawData == null || rawData.Length < 11)
            {
                return false;
            }

            if (rawData[0] != MeterFrameStart1 ||
                rawData[1] != MeterFrameStart2 ||
                rawData[^2] != MeterFrameStop1 ||
                rawData[^1] != MeterFrameStop2)
            {
                return false;
            }

            int dataLength = rawData[2] | (rawData[3] << 8);
            if (rawData.Length != dataLength + 4 || dataLength < 7)
            {
                return false;
            }

            int dataItemLength = dataLength - 7;
            if (dataItemLength < 0 || rawData.Length < dataItemLength + 11)
            {
                return false;
            }

            if (CalculateChecksum(rawData, 2, dataLength - 1) != rawData[^3])
            {
                return false;
            }

            if (rawData[4] != MeterDirectionMcuToPc ||
                rawData[5] != meterAddress ||
                rawData[6] != MeterControlProtocol ||
                rawData[7] != command)
            {
                return false;
            }

            dataItems = rawData.Skip(8).Take(dataItemLength).ToArray();
            return true;
        }

        private static bool TryGetMeterPacketDataItems(byte[] rawData, out byte command, out byte[] dataItems)
        {
            command = 0x00;
            dataItems = Array.Empty<byte>();
            if (rawData == null || rawData.Length < 11)
            {
                return false;
            }

            if (rawData[0] != MeterFrameStart1 ||
                rawData[1] != MeterFrameStart2 ||
                rawData[^2] != MeterFrameStop1 ||
                rawData[^1] != MeterFrameStop2)
            {
                return false;
            }

            int dataLength = rawData[2] | (rawData[3] << 8);
            if (rawData.Length != dataLength + 4 || dataLength < 7)
            {
                return false;
            }

            int dataItemLength = dataLength - 7;
            if (dataItemLength < 0 || rawData.Length < dataItemLength + 11)
            {
                return false;
            }

            if (CalculateChecksum(rawData, 2, dataLength - 1) != rawData[^3] ||
                rawData[4] != MeterDirectionMcuToPc ||
                rawData[6] != MeterControlProtocol)
            {
                return false;
            }

            command = rawData[7];
            dataItems = rawData.Skip(8).Take(dataItemLength).ToArray();
            return true;
        }

        private static string DescribeMeterResponse(byte[] rawData)
        {
            if (rawData == null || rawData.Length < 11)
            {
                return string.Empty;
            }

            if (rawData[0] != MeterFrameStart1 ||
                rawData[1] != MeterFrameStart2 ||
                rawData[^2] != MeterFrameStop1 ||
                rawData[^1] != MeterFrameStop2)
            {
                return string.Empty;
            }

            int dataLength = rawData[2] | (rawData[3] << 8);
            if (rawData.Length != dataLength + 4 || dataLength < 7)
            {
                return "收到电表协议报文，但长度字段异常";
            }

            int dataItemLength = dataLength - 7;
            if (dataItemLength < 0 || rawData.Length < dataItemLength + 11)
            {
                return "收到电表协议报文，但数据项长度异常";
            }

            byte expectedChecksum = CalculateChecksum(rawData, 2, dataLength - 1);
            byte actualChecksum = rawData[^3];
            if (expectedChecksum != actualChecksum)
            {
                return $"收到电表协议报文，但校验失败，期望 {expectedChecksum:X2}，实际 {actualChecksum:X2}";
            }

            byte direction = rawData[4];
            byte meterAddress = rawData[5];
            byte protocol = rawData[6];
            byte command = rawData[7];
            byte[] dataItems = rawData.Skip(8).Take(dataItemLength).ToArray();

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
                dataItems[0] == OperationStart)
            {
                responseDescription = $"表位通信测试应答正常，表位地址={meterAddress:X2}";
                return true;
            }

            if (command == MeterResetCommand &&
                dataItems.Length == 1 &&
                dataItems[0] == OperationStart)
            {
                responseDescription = $"复位命令应答正常，表位地址={meterAddress:X2}";
                return true;
            }

            if (TryDescribeDeviceBoardResponse(command, meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (command == MeterDailyTimingCommand &&
                TryDescribeDailyTimingResponse(meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (command == MeterCreepingTestCommand &&
                TryDescribeCreepingTestResponse(meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (command == MeterWalkingTestCommand &&
                TryDescribeWalkingTestResponse(meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (command == MeterBasicErrorCommand &&
                TryDescribeBasicErrorResponse(meterAddress, dataItems, out responseDescription))
            {
                return true;
            }

            if (command == MeterBasicErrorCommand38 &&
                TryDescribeBasicError38Response(meterAddress, dataItems, out responseDescription))
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

            if (command == MeterTemperatureCommand &&
                TryDescribeTemperatureResponse(meterAddress, dataItems, out responseDescription))
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

        /// <summary>把0x80-0x83装置通信板应答转换为可读日志。</summary>
        private static bool TryDescribeDeviceBoardResponse(
            byte command,
            byte address,
            byte[] dataItems,
            out string description)
        {
            description = string.Empty;
            switch (command)
            {
                case 0x80 when dataItems.Length == 1:
                    string category = dataItems[0] switch
                    {
                        0x01 => "单相",
                        0x02 => "三相四线",
                        0x03 => "三相三线",
                        _ => $"未知类别0x{dataItems[0]:X2}"
                    };
                    description = $"装置通信板0x80电表类别应答，地址={address:X2}，类别={category}";
                    return true;
                case 0x81 when dataItems.Length == 1:
                    string lamp = dataItems[0] switch
                    {
                        0x01 => "测试中",
                        0x02 => "测试合格",
                        0x03 => "测试不合格/出错",
                        0x04 => "运行灯关闭",
                        0x05 => "运行灯复位",
                        _ => $"未知状态0x{dataItems[0]:X2}"
                    };
                    description = $"装置通信板0x81运行灯应答，地址={address:X2}，状态={lamp}";
                    return true;
                case 0x82 when dataItems.Length == 2:
                    description = $"装置通信板0x82接线模式上报/应答，地址={address:X2}，来源={DescribeDeviceBoardSource(dataItems[0])}，模式={DescribeDeviceBoardConnectionMode(dataItems[1])}";
                    return true;
                case 0x83 when dataItems.Length == 2:
                    description = $"装置通信板0x83零线电流上报/应答，地址={address:X2}，来源={DescribeDeviceBoardSource(dataItems[0])}，模式={DescribeDeviceBoardNeutralMode(dataItems[1])}";
                    return true;
                default:
                    return false;
            }
        }

        private static string DescribeDeviceBoardSource(byte value)
        {
            return value switch
            {
                0x01 => "PC控制",
                0x02 => "装置通信板发出",
                0xFF => "恢复旋钮检测",
                0xAA => "读取旋钮状态",
                _ => $"未知0x{value:X2}"
            };
        }

        private static string DescribeDeviceBoardConnectionMode(byte value)
        {
            return value switch
            {
                0x01 => "三相直接式",
                0x02 => "三相互感式",
                0x03 => "单相",
                _ => $"未知0x{value:X2}"
            };
        }

        private static string DescribeDeviceBoardNeutralMode(byte value)
        {
            return value switch
            {
                0x01 => "相电流",
                0x02 => "相电流切换到零线",
                _ => $"未知0x{value:X2}"
            };
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

            if (operation == OperationStart && dataItems.Length == 3)
            {
                responseDescription = $"日计时试验开始应答，表位地址={meterAddress:X2}，时间={testTime}s，次数={testCount}";
                return true;
            }

            if (operation == OperationStop && dataItems.Length == 3)
            {
                responseDescription = $"日计时试验停止应答，表位地址={meterAddress:X2}，时间={testTime}s，次数={testCount}";
                return true;
            }

            if (operation != OperationRead)
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

        /// <summary>把0x25启动/结果应答转换成可读日志。</summary>
        private static bool TryDescribeCreepingTestResponse(
            byte meterAddress,
            byte[] dataItems,
            out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length == 1 && dataItems[0] == OperationExecute)
            {
                responseDescription = $"潜动试验启动应答，表位地址={meterAddress:X2}";
                return true;
            }

            if (dataItems.Length == 5 && dataItems[0] == OperationRead)
            {
                uint pulseCount = BinaryPrimitives.ReadUInt32LittleEndian(dataItems.AsSpan(1, sizeof(uint)));
                responseDescription = $"潜动试验结果获取应答，表位地址={meterAddress:X2}，实际脉冲数={pulseCount}";
                return true;
            }

            return false;
        }

        /// <summary>把0x37启动/停止/结果应答转换成可读日志。</summary>
        private static bool TryDescribeWalkingTestResponse(
            byte meterAddress,
            byte[] dataItems,
            out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length == 1 && dataItems[0] == OperationStart)
            {
                responseDescription = $"走字试验启动应答，表位地址={meterAddress:X2}";
                return true;
            }

            if (dataItems.Length == 1 && dataItems[0] == OperationStop)
            {
                responseDescription = $"走字试验停止应答，表位地址={meterAddress:X2}";
                return true;
            }

            if (dataItems.Length == 9 && dataItems[0] == OperationRead)
            {
                uint pulseCount = BinaryPrimitives.ReadUInt32LittleEndian(dataItems.AsSpan(1, sizeof(uint)));
                float energyKwh = BinaryPrimitives.ReadSingleLittleEndian(dataItems.AsSpan(5, sizeof(float)));
                responseDescription = $"走字试验结果获取应答，表位地址={meterAddress:X2}，待测表脉冲数={pulseCount}，标准表电能量={energyKwh.ToString("0.000000", CultureInfo.InvariantCulture)} kWh";
                return true;
            }

            return false;
        }

        /// <summary>潜动走字命令执行期间锁定参数和按钮，避免重复发送。</summary>
        private void SetCreepingTestUiBusy(bool isBusy)
        {
            btnStartCreepingTest.Enabled = !isBusy;
            btnGetCreepingTestResult.Enabled = !isBusy;
        }

        /// <summary>走字试验执行期间锁定参数和按钮，避免重复发送。</summary>
        private void SetWalkingTestUiBusy(bool isBusy)
        {
            btnStartWalkingTest.Enabled = !isBusy;
            btnStopWalkingTest.Enabled = !isBusy;
            btnGetWalkingTestResult.Enabled = !isBusy;
        }

        private static bool TryDescribeBasicErrorResponse(byte meterAddress, byte[] dataItems, out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length < 2)
            {
                return false;
            }

            byte errorType = dataItems[0];
            byte action = dataItems[1];

            if (action == OperationExecute && dataItems.Length == 2)
            {
                responseDescription = $"误差测试启动应答，表位地址={meterAddress:X2}，类型={DescribeBasicErrorType(errorType)}";
                return true;
            }

            if (action != OperationRead)
            {
                return false;
            }

            if (dataItems.Length < 6)
            {
                responseDescription = $"误差测试结果获取应答，表位地址={meterAddress:X2}，类型={DescribeBasicErrorType(errorType)}，结果数据长度异常";
                return true;
            }

            float result = ReadSingleLittleEndian(dataItems, 2);
            responseDescription = $"误差测试结果，表位地址={meterAddress:X2}，类型={DescribeBasicErrorType(errorType)}，误差={result.ToString("F6", CultureInfo.InvariantCulture)}";
            return true;
        }

        private static bool TryDescribeBasicError38Response(byte meterAddress, byte[] dataItems, out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length < 3)
            {
                return false;
            }

            byte operation = dataItems[0];
            byte pulseCount = dataItems[1];
            byte testCount = dataItems[2];

            if (operation == OperationStart)
            {
                if (dataItems.Length < 4)
                {
                    return false;
                }

                string pulseType = DescribeBasicError38PulseType(dataItems[3]);
                responseDescription = $"0x38基本误差启动应答，表位地址={meterAddress:X2}，脉冲数={pulseCount}，次数={testCount}，类型={pulseType}";
                return true;
            }

            if (operation == OperationStop)
            {
                responseDescription = $"0x38基本误差停止应答，表位地址={meterAddress:X2}，脉冲数={pulseCount}，次数={testCount}";
                return true;
            }

            if (operation != OperationRead)
            {
                return false;
            }

            if (dataItems.Length == 3)
            {
                responseDescription = $"0x38基本误差结果获取应答，表位地址={meterAddress:X2}，脉冲数={pulseCount}，次数={testCount}，暂无结果";
                return true;
            }

            int resultDataLength = dataItems.Length - 3;
            if (resultDataLength % 4 != 0)
            {
                responseDescription = $"0x38基本误差结果获取应答，表位地址={meterAddress:X2}，结果数据长度异常";
                return true;
            }

            int resultCount = resultDataLength / 4;
            List<string> results = new(resultCount);
            for (int i = 0; i < resultCount; i++)
            {
                float result = ReadSingleLittleEndian(dataItems, 3 + (i * 4));
                results.Add($"第{i + 1}次={result.ToString("F6", CultureInfo.InvariantCulture)}");
            }

            responseDescription = $"0x38基本误差结果，表位地址={meterAddress:X2}，脉冲数={pulseCount}，次数={testCount}，{string.Join("；", results)}";
            return true;
        }

        private byte GetBasicErrorTypeCode()
        {
            return cbxBasicErrorType.SelectedIndex switch
            {
                1 => 0x02,
                2 => 0x03,
                3 => 0x04,
                _ => 0x01
            };
        }

        private bool IsBasicErrorProtocol38Selected()
        {
            return cbxBasicErrorProtocol38.Checked;
        }

        private static byte GetBasicError38Operation(byte actionDataItem)
        {
            return actionDataItem switch
            {
                OperationExecute => OperationStart,
                OperationRead => OperationRead,
                _ => OperationStop
            };
        }

        private static byte[] BuildBasicError38Payload(byte operation, byte pulseCount, byte testCount, byte pulseType)
        {
            return operation == OperationRead
                ? new[] { operation, pulseCount, testCount }
                : new[] { operation, pulseCount, testCount, pulseType };
        }

        private static string BuildBasicError38PacketName(byte meterAddress, byte errorType, byte operation, byte pulseCount, byte testCount)
        {
            return operation switch
            {
                OperationStart => $"0x38基本误差[表位={meterAddress:X2}, 类型={DescribeBasicErrorType(errorType)}, 脉冲数={pulseCount}, 次数={testCount}, 开始]",
                OperationRead => $"0x38基本误差[表位={meterAddress:X2}, 类型={DescribeBasicErrorType(errorType)}, 脉冲数={pulseCount}, 次数={testCount}, 结果获取]",
                _ => $"0x38基本误差[表位={meterAddress:X2}, 类型={DescribeBasicErrorType(errorType)}, 脉冲数={pulseCount}, 次数={testCount}, 停止]"
            };
        }

        private static byte GetBasicError38PulseType(byte errorType)
        {
            return errorType == 0x02 ? (byte)0x01 : (byte)0x00;
        }

        private static string DescribeBasicError38PulseType(byte pulseType)
        {
            return pulseType switch
            {
                0x00 => "有功",
                0x01 => "无功",
                _ => $"未知脉冲类型 {pulseType:X2}"
            };
        }

        private static string DescribeBasicErrorType(byte errorType)
        {
            return errorType switch
            {
                0x01 => "有功",
                0x02 => "无功",
                0x03 => "有功+无功",
                0x04 => "谐波",
                _ => $"未知类型 {errorType:X2}"
            };
        }

        private static string DescribeBasicErrorAction(byte actionDataItem)
        {
            return actionDataItem switch
            {
                OperationExecute => "实验启动",
                OperationRead => "实验结果获取",
                _ => $"动作{actionDataItem:X2}"
            };
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
                dataItems[0] == OperationExecute)
            {
                responseDescription = $"表位电压短路检测开始应答正常，表位地址={meterAddress:X2}";
                return true;
            }

            if (dataItems.Length != 2 ||
                dataItems[0] != OperationRead)
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
                dataItems[0] == OperationExecute)
            {
                responseDescription = $"表位有无电表检测开始应答正常，表位地址={meterAddress:X2}";
                return true;
            }

            if (dataItems.Length != 2 ||
                dataItems[0] != OperationRead)
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

        /// <summary>把0xCA温度读取、校准和删除校准应答转换为可读日志。</summary>
        private static bool TryDescribeTemperatureResponse(
            byte meterAddress,
            byte[] dataItems,
            out string responseDescription)
        {
            responseDescription = string.Empty;
            if (dataItems.Length == 6 &&
                (dataItems[1] == OperationRead || dataItems[1] == OperationExecute))
            {
                int rawValue = BinaryPrimitives.ReadInt32LittleEndian(dataItems.AsSpan(2, sizeof(int)));
                string operation = dataItems[1] == OperationRead ? "读取" : "校准";
                responseDescription =
                    $"温度{operation}应答，表位地址={meterAddress:X2}，传感器={dataItems[0]}，温度原始值={rawValue}";
                return true;
            }

            if (dataItems.Length == 3 &&
                dataItems[1] == OperationStop &&
                dataItems[2] == 0x00)
            {
                responseDescription =
                    $"删除温度校准值应答，表位地址={meterAddress:X2}，传感器={dataItems[0]}";
                return true;
            }

            return false;
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
                OperationStart => "压接",
                OperationExecute => "弹开",
                OperationStop => "电机断电",
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
