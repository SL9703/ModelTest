using System.Globalization;
using ModelTest.Protocol;
using ModelTest.Socket_DLL.Socket_Client;

namespace ModelTest.CustomControl
{
    /// <summary>
    /// 新装置电源端口控制界面。
    /// 控件分区按照协议文档建议划分，后续按钮事件可直接调用 SHPowerProtocolClient。
    /// </summary>
    public partial class SHUserControl : UserControl
    {
        public delegate void UpdateMainFormDelegate(string message);
        public event UpdateMainFormDelegate? OnUpdateRequestedSHLog;

        private readonly EnhancedTcpClient _powerTcpClient;
        private readonly SHPowerProtocolClient _powerProtocolClient;
        private readonly ToolTip _wireTypeToolTip = new();

        public SHUserControl()
        {
            _powerTcpClient = new EnhancedTcpClient("SH_POWER");
            _powerProtocolClient = new SHPowerProtocolClient(_powerTcpClient);

            InitializeComponent();
            BackColor = Color.FromArgb(88, 149, 127);
            InitializeDefaultValues();
            BindProtocolButtonEvents();
        }

        private void InitializeDefaultValues()
        {
            txtPowerIp.Text = "127.0.0.1";
            nudPowerPort.Value = 4001;

            BindEnum(cmbVoltagePhase, typeof(ZkPhase));
            BindEnum(cmbVoltagePhaseAngle, typeof(ZkPhase));
            BindVoltagePhaseControl(cmbVoltagePhaseControl);
            BindEnum(cmbCurrentPhaseAngle, typeof(ZkPhase));
            BindCurrentPhaseControl(cmbCurrentPhaseControl);
            BindEnum(cmbWireType, typeof(ZkWireType));
            ConfigureWireTypeComboBox();
            BindEnum(cmbPulseSource, typeof(ZkPulseSource));
            BindEnum(cmbCurrentAccessMode, typeof(ZkCurrentAccessMode));
            BindEnum(cmbStatusMode, typeof(ZkStatusMode));
            BindEnum(cmbTestCommand, typeof(ZkTestCommand));
            BindEnum(cmbHarmonicTarget, typeof(ZkHarmonicTarget));
            BindEnum(cmbMiscCommand, typeof(ZkMiscCommand));
            BindEnum(cmbCheckLamp, typeof(ZkLampState));
            BindEnum(cmbWithstandLamp, typeof(ZkLampState));
            BindEnum(cmbPulseLevelMode, typeof(ZkPulseLevelMode));

            cmbVoltagePhaseAngle.SelectedItem = ZkPhase.A;
            cmbVoltagePhaseControl.SelectedItem = ZkPhase.B;
            cmbCurrentPhaseAngle.SelectedItem = ZkPhase.A;
            cmbCurrentPhaseControl.SelectedItem = "A";
            cmbOutputPulseChannel.SelectedIndex = 0;
            cmbRunPulseChannel.SelectedIndex = 0;
            cmbPulseMergeLineCount.SelectedIndex = 0;
        }

        private void BindProtocolButtonEvents()
        {
            btnConnect.Click -= LogButtonClick;
            btnConnect.Click += btnConnect_Click;

            btnDisconnect.Click -= LogButtonClick;
            btnDisconnect.Click += btnDisconnect_Click;

            btnAllOn.Click -= LogButtonClick;
            btnAllOn.Click += btnAllOn_Click;

            btnAllOff.Click -= LogButtonClick;
            btnAllOff.Click += btnAllOff_Click;

            btnVoltageOn.Click -= LogButtonClick;
            btnVoltageOn.Click += btnVoltageOn_Click;

            btnVoltageOff.Click -= LogButtonClick;
            btnVoltageOff.Click += btnVoltageOff_Click;

            btnCurrentOn.Click -= LogButtonClick;
            btnCurrentOn.Click += btnCurrentOn_Click;

            btnCurrentOff.Click -= LogButtonClick;
            btnCurrentOff.Click += btnCurrentOff_Click;

            btnPhaseVoltageOn.Click -= LogButtonClick;
            btnPhaseVoltageOn.Click += btnPhaseVoltageOn_Click;

            btnPhaseVoltageOff.Click -= LogButtonClick;
            btnPhaseVoltageOff.Click += btnPhaseVoltageOff_Click;

            btnPhaseCurrentOn.Click -= LogButtonClick;
            btnPhaseCurrentOn.Click += btnPhaseCurrentOn_Click;

            btnPhaseCurrentOff.Click -= LogButtonClick;
            btnPhaseCurrentOff.Click += btnPhaseCurrentOff_Click;

            btnSetThreeVoltage.Click -= LogButtonClick;
            btnSetThreeVoltage.Click += btnSetThreeVoltage_Click;

            btnSetPhaseVoltage.Click -= LogButtonClick;
            btnSetPhaseVoltage.Click += btnSetPhaseVoltage_Click;

            btnSetVoltagePercent.Click -= LogButtonClick;
            btnSetVoltagePercent.Click += btnSetVoltagePercent_Click;

            btnSetThreeCurrent.Click -= LogButtonClick;
            btnSetThreeCurrent.Click += btnSetThreeCurrent_Click;

            btnSetPhaseCurrent.Click -= LogButtonClick;
            btnSetPhaseCurrent.Click += btnSetPhaseCurrent_Click;

            btnSetCurrentPercent.Click -= LogButtonClick;
            btnSetCurrentPercent.Click += btnSetCurrentPercent_Click;

            btnSetVoltagePhase.Click -= LogButtonClick;
            btnSetVoltagePhase.Click += btnSetVoltagePhase_Click;

            btnSetCurrentPhase.Click -= LogButtonClick;
            btnSetCurrentPhase.Click += btnSetCurrentPhase_Click;

            btnSetFrequency.Click -= LogButtonClick;
            btnSetFrequency.Click += btnSetFrequency_Click;

            btnSetWireType.Click -= LogButtonClick;
            btnSetWireType.Click += btnSetWireType_Click;

            btnSetReferenceVoltage.Click -= LogButtonClick;
            btnSetReferenceVoltage.Click += btnSetReferenceVoltage_Click;

            btnSetCurrentRange.Click -= LogButtonClick;
            btnSetCurrentRange.Click += btnSetCurrentRange_Click;
        }

        private static void BindEnum(ComboBox comboBox, Type enumType)
        {
            comboBox.DataSource = Enum.GetValues(enumType);
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private static void BindVoltagePhaseControl(ComboBox comboBox)
        {
            comboBox.DataSource = new[] { ZkPhase.B, ZkPhase.C };
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private static void BindCurrentPhaseControl(ComboBox comboBox)
        {
            comboBox.DataSource = new[] { "A", "B", "C", "H" };
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void ConfigureWireTypeComboBox()
        {
            cmbWireType.DropDownWidth = Math.Max(520, GetComboBoxDropDownWidth(cmbWireType));
            cmbWireType.SelectedIndexChanged -= cmbWireType_SelectedIndexChanged;
            cmbWireType.SelectedIndexChanged += cmbWireType_SelectedIndexChanged;
            UpdateWireTypeToolTip();
        }

        private static int GetComboBoxDropDownWidth(ComboBox comboBox)
        {
            int maxWidth = comboBox.Width;
            foreach (object item in comboBox.Items)
            {
                int itemWidth = TextRenderer.MeasureText(item.ToString(), comboBox.Font).Width;
                maxWidth = Math.Max(maxWidth, itemWidth);
            }

            return maxWidth + SystemInformation.VerticalScrollBarWidth + 24;
        }

        private void cmbWireType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateWireTypeToolTip();
        }

        private void UpdateWireTypeToolTip()
        {
            string text = cmbWireType.SelectedItem?.ToString() ?? string.Empty;
            _wireTypeToolTip.SetToolTip(cmbWireType, text);
        }

        private void LogButtonClick(object? sender, EventArgs e)
        {
            string buttonText = sender is Button button ? button.Text : "按钮";
            OnUpdateRequestedSHLog?.Invoke($"SHUserControl 已点击：{buttonText}。请在该事件中接入 SHPowerProtocolClient。");
        }

        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            string ipAddress = txtPowerIp.Text.Trim();
            int port = (int)nudPowerPort.Value;

            btnConnect.Enabled = false;
            OnUpdateRequestedSHLog?.Invoke($"SH源正在连接：{ipAddress}:{port}");

            try
            {
                bool success = await _powerProtocolClient.ConnectAsync(ipAddress, port);
                OnUpdateRequestedSHLog?.Invoke(success
                    ? $"SH源连接成功：{ipAddress}:{port}"
                    : $"SH源连接失败：{ipAddress}:{port}");
            }
            catch (Exception ex)
            {
                OnUpdateRequestedSHLog?.Invoke($"SH源连接异常：{ex.Message}");
            }
            finally
            {
                btnConnect.Enabled = true;
            }
        }

        private void btnDisconnect_Click(object? sender, EventArgs e)
        {
            try
            {
                _powerProtocolClient.Disconnect();
                OnUpdateRequestedSHLog?.Invoke("SH源连接已断开");
            }
            catch (Exception ex)
            {
                OnUpdateRequestedSHLog?.Invoke($"SH源断开异常：{ex.Message}");
            }
        }

        private async void btnAllOn_Click(object? sender, EventArgs e)
        {
            await ExecuteOutputCommandAsync(btnAllOn, "全部输出", "UION", () => _powerProtocolClient.SetAllOutputAsync(true));
        }

        private async void btnAllOff_Click(object? sender, EventArgs e)
        {
            await ExecuteOutputCommandAsync(btnAllOff, "全部关闭", "UIOF", () => _powerProtocolClient.SetAllOutputAsync(false));
        }

        private async void btnVoltageOn_Click(object? sender, EventArgs e)
        {
            await ExecuteOutputCommandAsync(btnVoltageOn, "电压输出", "UON", () => _powerProtocolClient.SetVoltageOutputAsync(true));
        }

        private async void btnVoltageOff_Click(object? sender, EventArgs e)
        {
            await ExecuteOutputCommandAsync(btnVoltageOff, "电压关闭", "UOF", () => _powerProtocolClient.SetVoltageOutputAsync(false));
        }

        private async void btnCurrentOn_Click(object? sender, EventArgs e)
        {
            await ExecuteOutputCommandAsync(btnCurrentOn, "电流输出", "ION", () => _powerProtocolClient.SetCurrentOutputAsync(true));
        }

        private async void btnCurrentOff_Click(object? sender, EventArgs e)
        {
            await ExecuteOutputCommandAsync(btnCurrentOff, "电流关闭", "IOF", () => _powerProtocolClient.SetCurrentOutputAsync(false));
        }

        private async void btnPhaseVoltageOn_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedOutputPhase();
            await ExecuteOutputCommandAsync(btnPhaseVoltageOn, $"{phase}相电压输出", GetPhaseVoltageCommand(phase, true), () => _powerProtocolClient.SetPhaseVoltageOutputAsync(phase, true));
        }

        private async void btnPhaseVoltageOff_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedOutputPhase();
            await ExecuteOutputCommandAsync(btnPhaseVoltageOff, $"{phase}相电压关闭", GetPhaseVoltageCommand(phase, false), () => _powerProtocolClient.SetPhaseVoltageOutputAsync(phase, false));
        }

        private async void btnPhaseCurrentOn_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedOutputPhase();
            await ExecuteOutputCommandAsync(btnPhaseCurrentOn, $"{phase}相电流输出", GetPhaseCurrentCommand(phase, true), () => _powerProtocolClient.SetPhaseCurrentOutputAsync(phase, true));
        }

        private async void btnPhaseCurrentOff_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedOutputPhase();
            await ExecuteOutputCommandAsync(btnPhaseCurrentOff, $"{phase}相电流关闭", GetPhaseCurrentCommand(phase, false), () => _powerProtocolClient.SetPhaseCurrentOutputAsync(phase, false));
        }

        private async void btnSetThreeVoltage_Click(object? sender, EventArgs e)
        {
            decimal voltageValue = nudThreeVoltage.Value;
            string command = $"Pum:{voltageValue.ToString("0.000", CultureInfo.InvariantCulture)}";
            await ExecuteOutputCommandAsync(
                btnSetThreeVoltage,
                "设置三相电压幅度",
                command,
                () => _powerProtocolClient.SetThreePhaseVoltageAmplitudeAsync(voltageValue));
        }

        private async void btnSetPhaseVoltage_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedVoltageAmplitudePhase();
            decimal voltageValue = nudPhaseVoltage.Value;
            string command = $"{GetPhaseVoltageAmplitudeCommandPrefix(phase)}:{voltageValue.ToString("0.000", CultureInfo.InvariantCulture)}";
            await ExecuteOutputCommandAsync(
                btnSetPhaseVoltage,
                $"{phase}相电压幅度",
                command,
                () => _powerProtocolClient.SetPhaseVoltageAmplitudeAsync(phase, voltageValue));
        }

        private async void btnSetVoltagePercent_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedVoltageAmplitudePhase();
            decimal percent = nudVoltagePercent.Value;
            string command = $"{GetPhaseVoltagePercentCommandPrefix(phase)}:{FormatOneDecimalPercentValue(percent):0000}";
            await ExecuteOutputCommandAsync(
                btnSetVoltagePercent,
                $"{phase}相电压百分比",
                command,
                () => _powerProtocolClient.SetPhaseVoltagePercentAsync(phase, percent));
        }

        private async void btnSetThreeCurrent_Click(object? sender, EventArgs e)
        {
            decimal currentValue = nudThreeCurrent.Value;
            decimal maxCurrentValue = nudMaxCurrent.Value;
            string command = $"Pim:{currentValue.ToString("0.000", CultureInfo.InvariantCulture)}";
            await ExecuteOutputCommandAsync(
                btnSetThreeCurrent,
                "设置三相电流幅度",
                command,
                () => _powerProtocolClient.SetThreePhaseCurrentAmplitudeAsync(currentValue, maxCurrentValue));
        }

        private async void btnSetPhaseCurrent_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedCurrentAmplitudePhase();
            decimal currentValue = nudPhaseCurrent.Value;
            decimal maxCurrentValue = nudMaxCurrent.Value;
            string command = $"{GetPhaseCurrentAmplitudeCommandPrefix(phase)}:{currentValue.ToString("0.000", CultureInfo.InvariantCulture)}";
            await ExecuteOutputCommandAsync(
                btnSetPhaseCurrent,
                $"{phase}相电流幅度",
                command,
                () => _powerProtocolClient.SetPhaseCurrentAmplitudeAsync(phase, currentValue, maxCurrentValue));
        }

        private async void btnSetCurrentPercent_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedCurrentAmplitudePhase();
            decimal percent = nudCurrentPercent.Value;
            string command = $"{GetPhaseCurrentPercentCommandPrefix(phase)}:{FormatOneDecimalPercentValue(percent):0000}";
            await ExecuteOutputCommandAsync(
                btnSetCurrentPercent,
                $"{phase}相电流百分比",
                command,
                () => _powerProtocolClient.SetPhaseCurrentPercentAsync(phase, percent));
        }

        private async void btnSetVoltagePhase_Click(object? sender, EventArgs e)
        {
            ZkPhase phase = GetSelectedVoltagePhaseControl();
            decimal angle = nudVoltagePhaseAngle.Value;
            string command = $"{GetVoltagePhaseCommandPrefix(phase)}:{angle.ToString("0.000", CultureInfo.InvariantCulture)}";
            await ExecuteOutputCommandAsync(
                btnSetVoltagePhase,
                $"{phase}相电压相位",
                command,
                () => _powerProtocolClient.SetVoltagePhaseAsync(phase, angle));
        }

        private async void btnSetCurrentPhase_Click(object? sender, EventArgs e)
        {
            decimal angle = nudPhaseAngle.Value;

            if (IsSelectedCombinedCurrentPhase())
            {
                string combinedCommand = $"Pph:{angle.ToString("0.000", CultureInfo.InvariantCulture)}";
                await ExecuteOutputCommandAsync(
                    btnSetCurrentPhase,
                    "电流合相相位",
                    combinedCommand,
                    () => _powerProtocolClient.SetCombinedCurrentPhaseAsync(angle));
                return;
            }

            ZkPhase phase = GetSelectedCurrentPhaseControl();
            string command = $"{GetCurrentPhaseCommandPrefix(phase)}:{angle.ToString("0.000", CultureInfo.InvariantCulture)}";
            await ExecuteOutputCommandAsync(
                btnSetCurrentPhase,
                $"{phase}相电流相位",
                command,
                () => _powerProtocolClient.SetCurrentPhaseAsync(phase, angle));
        }

        private async void btnSetFrequency_Click(object? sender, EventArgs e)
        {
            decimal frequency = nudFrequency.Value;
            string command = $"Pfr:{frequency.ToString("0.000", CultureInfo.InvariantCulture)}";
            await ExecuteOutputCommandAsync(
                btnSetFrequency,
                "频率",
                command,
                () => _powerProtocolClient.SetFrequencyAsync(frequency));
        }

        private async void btnSetWireType_Click(object? sender, EventArgs e)
        {
            ZkWireType wireType = cmbWireType.SelectedItem is ZkWireType selectedWireType
                ? selectedWireType
                : ZkWireType.单相有功;
            string command = $"TYPE:{(char)wireType}";
            await ExecuteOutputCommandAsync(
                btnSetWireType,
                "接线方式",
                command,
                () => _powerProtocolClient.SetWireTypeAsync(wireType));
        }

        private async void btnSetReferenceVoltage_Click(object? sender, EventArgs e)
        {
            string referenceVoltageText = cmbReferenceVoltage.SelectedItem?.ToString() ?? "220";
            decimal referenceVoltage = decimal.Parse(referenceVoltageText, CultureInfo.InvariantCulture);
            string command = $"Ue:{referenceVoltageText}";
            await ExecuteOutputCommandAsync(
                btnSetReferenceVoltage,
                "参比电压",
                command,
                () => _powerProtocolClient.SetRferencevottageAsync(referenceVoltage));
        }

        private async void btnSetCurrentRange_Click(object? sender, EventArgs e)
        {
            decimal basicCurrent = nudBasicCurrent.Value;
            decimal ratedCurrent = nudRatedCurrent.Value;
            string command = $"Ie:{FormatPlainDecimalValue(basicCurrent)}({FormatPlainDecimalValue(ratedCurrent)})";
            await ExecuteOutputCommandAsync(
                btnSetCurrentRange,
                "电流量程",
                command,
                () => _powerProtocolClient.SetCurrentRangeAsync(basicCurrent, ratedCurrent));
        }

        private ZkPhase GetSelectedOutputPhase()
        {
            return cmbVoltagePhase.SelectedItem is ZkPhase phase ? phase : ZkPhase.A;
        }

        private ZkPhase GetSelectedVoltageAmplitudePhase()
        {
            return cmbVoltagePhaseAngle.SelectedItem is ZkPhase phase ? phase : ZkPhase.A;
        }

        private ZkPhase GetSelectedCurrentAmplitudePhase()
        {
            return cmbCurrentPhaseAngle.SelectedItem is ZkPhase phase ? phase : ZkPhase.A;
        }

        private ZkPhase GetSelectedVoltagePhaseControl()
        {
            return cmbVoltagePhaseControl.SelectedItem is ZkPhase phase ? phase : ZkPhase.B;
        }

        private ZkPhase GetSelectedCurrentPhaseControl()
        {
            return cmbCurrentPhaseControl.SelectedItem?.ToString() switch
            {
                "B" => ZkPhase.B,
                "C" => ZkPhase.C,
                _ => ZkPhase.A
            };
        }

        private bool IsSelectedCombinedCurrentPhase()
        {
            return string.Equals(cmbCurrentPhaseControl.SelectedItem?.ToString(), "H", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetPhaseVoltageAmplitudeCommandPrefix(ZkPhase phase)
        {
            return phase switch
            {
                ZkPhase.A => "PAum",
                ZkPhase.B => "PBum",
                ZkPhase.C => "PCum",
                _ => string.Empty
            };
        }

        private static string GetPhaseCurrentAmplitudeCommandPrefix(ZkPhase phase)
        {
            return phase switch
            {
                ZkPhase.A => "PAim",
                ZkPhase.B => "PBim",
                ZkPhase.C => "PCim",
                _ => string.Empty
            };
        }

        private static string GetPhaseVoltagePercentCommandPrefix(ZkPhase phase)
        {
            return phase switch
            {
                ZkPhase.A => "STUA",
                ZkPhase.B => "STUB",
                ZkPhase.C => "STUC",
                _ => string.Empty
            };
        }

        private static string GetPhaseCurrentPercentCommandPrefix(ZkPhase phase)
        {
            return phase switch
            {
                ZkPhase.A => "STIA",
                ZkPhase.B => "STIB",
                ZkPhase.C => "STIC",
                _ => string.Empty
            };
        }

        private static string GetVoltagePhaseCommandPrefix(ZkPhase phase)
        {
            return phase switch
            {
                ZkPhase.B => "PHUB",
                ZkPhase.C => "PHUC",
                _ => string.Empty
            };
        }

        private static string GetCurrentPhaseCommandPrefix(ZkPhase phase)
        {
            return phase switch
            {
                ZkPhase.A => "PHIA",
                ZkPhase.B => "PHIB",
                ZkPhase.C => "PHIC",
                _ => string.Empty
            };
        }

        private static int FormatOneDecimalPercentValue(decimal percent)
        {
            return (int)Math.Round(percent * 10m, MidpointRounding.AwayFromZero);
        }

        private static string FormatPlainDecimalValue(decimal value)
        {
            return value.ToString("0.################", CultureInfo.InvariantCulture);
        }

        private static string GetPhaseVoltageCommand(ZkPhase phase, bool isOn)
        {
            return (phase, isOn) switch
            {
                (ZkPhase.A, true) => "UAON",
                (ZkPhase.A, false) => "UAOF",
                (ZkPhase.B, true) => "UBON",
                (ZkPhase.B, false) => "UBOF",
                (ZkPhase.C, true) => "UCON",
                (ZkPhase.C, false) => "UCOF",
                _ => string.Empty
            };
        }

        private static string GetPhaseCurrentCommand(ZkPhase phase, bool isOn)
        {
            return (phase, isOn) switch
            {
                (ZkPhase.A, true) => "IAON",
                (ZkPhase.A, false) => "IAOF",
                (ZkPhase.B, true) => "IBON",
                (ZkPhase.B, false) => "IBOF",
                (ZkPhase.C, true) => "ICON",
                (ZkPhase.C, false) => "ICOF",
                _ => string.Empty
            };
        }

        private async Task ExecuteOutputCommandAsync(Button button, string actionName, string command, Func<Task<bool>> sendCommandAsync)
        {
            if (!_powerProtocolClient.IsConnected)
            {
                OnUpdateRequestedSHLog?.Invoke($"SH源未连接，无法发送{actionName}命令：{command}");
                return;
            }

            button.Enabled = false;

            try
            {
                bool success = await sendCommandAsync();
                OnUpdateRequestedSHLog?.Invoke(success
                    ? $"SH源{actionName}命令已发送：{command}(0Dh)"
                    : $"SH源{actionName}命令发送失败：{command}(0Dh)");
            }
            catch (Exception ex)
            {
                OnUpdateRequestedSHLog?.Invoke($"SH源{actionName}命令异常：{ex.Message}");
            }
            finally
            {
                button.Enabled = true;
            }
        }

    }
}
