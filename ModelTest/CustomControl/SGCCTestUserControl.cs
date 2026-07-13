using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using ModelTest.Tools;

namespace ModelTest.CustomControl
{
    public partial class SGCCTestUserControl : UserControl
    {
        private sealed class Jjg596MeasurementUnitConfig
        {
            public required string[] Voltages { get; init; }

            public required string[] Currents { get; init; }

            public required string[] ActiveClasses { get; init; }

            public required string[] ReactiveClasses { get; init; }

            public required string[] MeterConstants { get; init; }

            public required string[] AccessModes { get; init; }
        }

        private const string Jjg596PdfFileName = "4 JJG596-2026 安装式交流电能表检定规程分发.pdf";
        private const string SGCC645BroadcastMessage = "FEFEFEFE68AAAAAAAAAAAA681300DF16";
        private const string CSG698BroadcastMessage = "6810001000684AFFFFFFFFFFFF010A710000210100E0C216";
        private const string KZHLStatusMessage = "6817004345AAAAAAAAAAAA10da5f05013DFF140200006c6816";
        private const string KZHLIdMessage = "6817004345AAAAAAAAAAAA10DA5F050127F10002000027D316";
        private static readonly Regex CurrentRangeRegex = new(
            @"^\s*(?<imin>\d+(?:\.\d+)?)\s*-\s*(?<itr>\d+(?:\.\d+)?)\s*\((?<imax>\d+(?:\.\d+)?)\)\s*A\s*$",
            RegexOptions.Compiled);
        private static readonly Dictionary<string, Jjg596MeasurementUnitConfig> Jjg596Configs = new()
        {
            ["单相"] = new Jjg596MeasurementUnitConfig
            {
                Voltages = new[] { "220V" },
                Currents = new[] { "0.25-0.5(60)A", "0.25-0.5(100)A", "0.5-1(100)A" },
                ActiveClasses = new[] { "A" },
                ReactiveClasses = new[] { "2.0" },
                MeterConstants = new[] { "1000", "2000" },
                AccessModes = new[] { "直接接入" }
            },
            ["三相三线"] = new Jjg596MeasurementUnitConfig
            {
                Voltages = new[] { "3×100V" },
                Currents = new[] { "0.015-0.075(6)A", "0.2-0.5(60)A", "0.2-0.5(100)A", "0.003-0.015(1.2)A" },
                ActiveClasses = new[] { "B", "C", "D" },
                ReactiveClasses = new[] { "3.0", "2.0", "1S", "0.5S" },
                MeterConstants = new[] { "500", "1000", "10000", "20000", "40000", "100000" },
                AccessModes = new[] { "直接接入", "经互感器接入" }
            },
            ["三相四线"] = new Jjg596MeasurementUnitConfig
            {
                Voltages = new[] { "3×220/380V", "3×57.7/100V" },
                Currents = new[] { "0.2-0.5(60)A", "0.2-0.5(100)A", "0.015-0.075(6)A", "0.003-0.015(1.2)A" },
                ActiveClasses = new[] { "B", "C", "D" },
                ReactiveClasses = new[] { "3.0", "2.0", "1S", "0.5S" },
                MeterConstants = new[] { "500", "1000", "10000", "20000", "40000", "100000" },
                AccessModes = new[] { "直接接入", "经互感器接入" }
            }
        };

        public event Func<string, Task>? SendMessageRequested;
        public event Action<string>? LogRequested;

        public SGCCTestUserControl()
        {
            InitializeComponent();
            BackColor = Color.FromArgb(88, 149, 127);
            cbxSgccOadCategory.DataSource = SGCCOadConfig.OadCategories.ToList();
            cbxSgccOadCategory.SelectedItem = SGCCOadConfig.EnergyCategory;
            BindSgccOadItems();
            ModelTool.BindMutexCheckBoxes(cbxSGCC_Meter, cbxSGCC_Terminal);
            InitializeJjg596Controls();
        }

        private async void SGCC645FF_Click(object sender, EventArgs e)
        {
            await SendAsync(SGCC645BroadcastMessage);
        }

        private async void CSG698FF_Click(object sender, EventArgs e)
        {
            await SendAsync(CSG698BroadcastMessage);
        }

        private async void buttonKZHLStatus_Click(object sender, EventArgs e)
        {
            await SendAsync(KZHLStatusMessage);
        }

        private async void buttonKZHLID_Click(object sender, EventArgs e)
        {
            await SendAsync(KZHLIdMessage);
        }

        private async void btnReadMSG_Click(object sender, EventArgs e)
        {
            const string _68H = "68";
            const string _16H = "16";
            const string Ctrl = "43";
            const string SASgin = "05";
            const int RequiredAddressLength = 12;

            string serverAddress = tbxMeterTerminalAddr.Text.Trim();
            if (serverAddress.Length != RequiredAddressLength)
            {
                WriteLog("698报文服务器地址长度不正确，应为12位");
                return;
            }

            string caAddress = cbxSGCC_Meter.Checked ? "A0" : "10";
            string reverseServerAddress = ModelTool.ReverseHexString(serverAddress);
            if (!SGCCTools.TryGetOadApdu(cbxSgccOAD.Text, out string apdu))
            {
                WriteLog("请选择要读取的国网698 OAD项目");
                return;
            }

            if (cbxSgccOAD.Text == "广播读取终端或电表地址")
            {
                reverseServerAddress = "AAAAAAAAAAAA";
            }

            string sgccMessage = SGCCTools.BytesToSGCCMessage(_68H, Ctrl, SASgin, reverseServerAddress, caAddress, apdu, _16H);
            WriteLog("国网698 APDU类型：" + SGCCTools.GetApduServiceTypeDescription(apdu));
            WriteLog("国网698 GET-Request类型：" + SGCCTools.GetApduChoiceDescription(apdu));
            WriteLog("国网698 请求PID：" + SGCCTools.GetApduPiidDescription(apdu));
            WriteLog("国网698 功能OAD：" + SGCCTools.GetApduOadDescription(apdu));
            await SendAsync(sgccMessage);
        }

        private void btnOpenJjg596Pdf_Click(object sender, EventArgs e)
        {
            string pdfPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Protocol", Jjg596PdfFileName);
            if (!File.Exists(pdfPath))
            {
                WriteLog($"JJG596-2026 规程PDF不存在：{pdfPath}");
                MessageBox.Show("JJG596-2026 规程PDF不存在，请确认文件路径。", "文件未找到");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
                WriteLog("已打开 JJG596-2026 规程PDF");
            }
            catch (Exception ex)
            {
                WriteLog($"打开 JJG596-2026 规程PDF失败：{ex.Message}");
                MessageBox.Show($"打开规程PDF失败：{ex.Message}", "打开失败");
            }
        }

        private void cbxJjg596MeasurementUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplyJjg596MeasurementUnitConfig(cbxJjg596MeasurementUnit.Text.Trim());
        }

        private void cbxJjg596Current_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJjg596CurrentDerivedFields();
        }

        private void cbxJjg596Current_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596CurrentDerivedFields();
        }

        private void cbxJjg596Voltage_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJjg596CreepTimeResult();
            UpdateJjg596StartTimeResult();
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596Voltage_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596CreepTimeResult();
            UpdateJjg596StartTimeResult();
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596ActiveClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJjg596StartCurrent();
            UpdateJjg596CreepTimeResult();
            UpdateJjg596StartTimeResult();
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596ActiveClass_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596StartCurrent();
            UpdateJjg596CreepTimeResult();
            UpdateJjg596StartTimeResult();
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596MeterConstant_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJjg596CreepTimeResult();
            UpdateJjg596StartTimeResult();
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596MeterConstant_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596CreepTimeResult();
            UpdateJjg596StartTimeResult();
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596AccessMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJjg596CurrentDerivedFields();
        }

        private void cbxJjg596AccessMode_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596CurrentDerivedFields();
        }

        private void tbxJjg596StartCurrent_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596StartTimeResult();
        }

        private void cbxJjg596ErrorPowerType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596ErrorPowerFactor_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596ErrorPowerFactor_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596ErrorPhase_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxJjg596ErrorPhase_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596ErrorTimeResult();
        }

        private void tbxJjg596ErrorPulseCount_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596ErrorTimeResult();
        }

        private void tbxJjg596ErrorCurrent_TextChanged(object sender, EventArgs e)
        {
            UpdateJjg596ErrorTimeResult();
        }

        private void cbxSgccOadCategory_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindSgccOadItems();
        }

        private void InitializeJjg596Controls()
        {
            ConfigureEditableComboBox(cbxJjg596MeasurementUnit);
            ConfigureEditableComboBox(cbxJjg596Voltage);
            ConfigureEditableComboBox(cbxJjg596Current);
            ConfigureEditableComboBox(cbxJjg596ActiveClass);
            ConfigureEditableComboBox(cbxJjg596ReactiveClass);
            ConfigureEditableComboBox(cbxJjg596MeterConstant);
            ConfigureEditableComboBox(cbxJjg596AccessMode);
            ConfigureEditableComboBox(cbxJjg596ErrorPowerType);
            ConfigureEditableComboBox(cbxJjg596ErrorPowerFactor);
            ConfigureEditableComboBox(cbxJjg596ErrorPhase);

            SetComboBoxOptions(cbxJjg596MeasurementUnit, Jjg596Configs.Keys);
            SetComboBoxOptions(cbxJjg596ErrorPowerType, new[] { "有功", "无功" });
            SetComboBoxOptions(cbxJjg596ErrorPowerFactor, new[] { "1.0", "0.8", "0.5", "0.25" });
            SetComboBoxOptions(cbxJjg596ErrorPhase, new[] { "H", "A", "B", "C" });
            tbxJjg596ErrorPulseCount.Text = "1";
            ApplyJjg596MeasurementUnitConfig("单相");
        }

        private static void ConfigureEditableComboBox(ComboBox comboBox)
        {
            comboBox.DropDownStyle = ComboBoxStyle.DropDown;
            comboBox.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBox.AutoCompleteSource = AutoCompleteSource.ListItems;
        }

        private void ApplyJjg596MeasurementUnitConfig(string measurementUnit)
        {
            if (!Jjg596Configs.TryGetValue(measurementUnit, out Jjg596MeasurementUnitConfig? config))
            {
                return;
            }

            SetComboBoxOptions(cbxJjg596Voltage, config.Voltages);
            SetComboBoxOptions(cbxJjg596Current, config.Currents);
            SetComboBoxOptions(cbxJjg596ActiveClass, config.ActiveClasses);
            SetComboBoxOptions(cbxJjg596ReactiveClass, config.ReactiveClasses);
            SetComboBoxOptions(cbxJjg596MeterConstant, config.MeterConstants);
            SetComboBoxOptions(cbxJjg596AccessMode, config.AccessModes);
            cbxJjg596MeasurementUnit.Text = measurementUnit;

            if (!string.Equals(measurementUnit, "单相", StringComparison.Ordinal))
            {
                SetComboBoxSelectedText(cbxJjg596ReactiveClass, "2.0");
            }

            UpdateJjg596CurrentDerivedFields();
            UpdateJjg596CreepTimeResult();
            UpdateJjg596StartTimeResult();
            UpdateJjg596ErrorTimeResult();
        }

        private static void SetComboBoxOptions(ComboBox comboBox, IEnumerable<string> values)
        {
            string[] items = values.ToArray();
            comboBox.BeginUpdate();
            comboBox.Items.Clear();
            comboBox.Items.AddRange(items);
            comboBox.EndUpdate();
            comboBox.Text = items.FirstOrDefault() ?? string.Empty;
            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = 0;
            }
        }

        private static void SetComboBoxSelectedText(ComboBox comboBox, string text)
        {
            int index = comboBox.FindStringExact(text);
            if (index >= 0)
            {
                comboBox.SelectedIndex = index;
            }
            else
            {
                comboBox.Text = text;
            }
        }

        private void UpdateJjg596CurrentDerivedFields()
        {
            string currentText = cbxJjg596Current.Text.Trim();
            if (!TryParseCurrentRange(currentText, out decimal imin, out decimal itr, out decimal imax))
            {
                tbxJjg596Imin.Clear();
                tbxJjg596Itr.Clear();
                tbxJjg596Imax.Clear();
                tbxJjg596ReferenceCurrent.Clear();
                tbxJjg596StartCurrent.Clear();
                return;
            }

            tbxJjg596Imin.Text = FormatCurrentValue(imin);
            tbxJjg596Itr.Text = FormatCurrentValue(itr);
            tbxJjg596Imax.Text = FormatCurrentValue(imax);

            bool isTransformerAccess = cbxJjg596AccessMode.Text.Contains("互感器", StringComparison.Ordinal);
            decimal referenceValue = itr * (isTransformerAccess ? 20m : 10m);
            string referenceName = isTransformerAccess ? "In" : "Ib";
            labelJjg596ReferenceCurrent.Text = referenceName;
            tbxJjg596ReferenceCurrent.Text = $"{referenceName} = {FormatCurrentValue(referenceValue)}";
            if (string.IsNullOrWhiteSpace(tbxJjg596ErrorCurrent.Text))
            {
                tbxJjg596ErrorCurrent.Text = FormatCurrentValue(referenceValue);
            }

            UpdateJjg596StartCurrent();

            UpdateJjg596CreepTimeResult();
            UpdateJjg596StartTimeResult();
            UpdateJjg596ErrorTimeResult();
        }

        private static bool TryParseCurrentRange(string input, out decimal imin, out decimal itr, out decimal imax)
        {
            imin = 0;
            itr = 0;
            imax = 0;

            Match match = CurrentRangeRegex.Match(input);
            if (!match.Success)
            {
                return false;
            }

            return decimal.TryParse(match.Groups["imin"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out imin)
                && decimal.TryParse(match.Groups["itr"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out itr)
                && decimal.TryParse(match.Groups["imax"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out imax);
        }

        private static string FormatCurrentValue(decimal value)
        {
            return $"{value.ToString("0.###", CultureInfo.InvariantCulture)}A";
        }

        private void UpdateJjg596CreepTimeResult()
        {
            if (!TryGetCreepTimeParameters(
                    out decimal bValue,
                    out decimal meterConstant,
                    out decimal dValue,
                    out decimal voltageValue,
                    out decimal iminValue))
            {
                tbxJjg596CreepHours.Clear();
                tbxJjg596CreepMinutes.Clear();
                tbxJjg596CreepSeconds.Clear();
                return;
            }

            decimal denominator = 1.1m * bValue * meterConstant * dValue * voltageValue * iminValue;
            if (denominator <= 0)
            {
                tbxJjg596CreepHours.Clear();
                tbxJjg596CreepMinutes.Clear();
                tbxJjg596CreepSeconds.Clear();
                return;
            }

            decimal hours = 100000m / denominator;
            decimal minutes = hours * 60m;
            decimal seconds = minutes * 60m;

            tbxJjg596CreepHours.Text = $"{hours.ToString("0.####", CultureInfo.InvariantCulture)} H";
            tbxJjg596CreepMinutes.Text = $"{minutes.ToString("0.##", CultureInfo.InvariantCulture)} min";
            tbxJjg596CreepSeconds.Text = $"{RoundSeconds(seconds).ToString(CultureInfo.InvariantCulture)} s";
        }

        private void UpdateJjg596StartTimeResult()
        {
            if (!TryGetStartTimeParameters(
                    out decimal estRatio,
                    out decimal meterConstant,
                    out decimal unitFactor,
                    out decimal voltageValue,
                    out decimal startCurrent))
            {
                tbxJjg596StartPst.Clear();
                tbxJjg596StartTimeLower.Clear();
                tbxJjg596StartTimeUpper.Clear();
                return;
            }

            decimal pst = voltageValue * startCurrent * unitFactor;
            if (pst <= 0)
            {
                tbxJjg596StartPst.Clear();
                tbxJjg596StartTimeLower.Clear();
                tbxJjg596StartTimeUpper.Clear();
                return;
            }

            const decimal ki = 1m;
            const decimal ku = 1m;
            decimal baseTime = 3600000m / (meterConstant * pst * ki * ku);
            decimal lower = (1m - estRatio) * baseTime;
            decimal upper = (1m + estRatio) * baseTime;

            tbxJjg596StartPst.Text = $"{pst.ToString("0.###", CultureInfo.InvariantCulture)} W";
            tbxJjg596StartTimeLower.Text = $"{lower.ToString("0.####", CultureInfo.InvariantCulture)} s";
            tbxJjg596StartTimeUpper.Text = $"{upper.ToString("0.####", CultureInfo.InvariantCulture)} s";
        }

        private void UpdateJjg596StartCurrent()
        {
            if (!TryGetStartCurrentValue(out decimal startCurrent))
            {
                tbxJjg596StartCurrent.Clear();
                return;
            }

            tbxJjg596StartCurrent.Text = FormatCurrentValue(startCurrent);
        }

        private void UpdateJjg596ErrorTimeResult()
        {
            if (!TryGetErrorTimeParameters(
                    out decimal meterConstant,
                    out decimal currentValue,
                    out decimal lineVoltage,
                    out decimal powerFactor,
                    out decimal pulseCount,
                    out decimal phaseFactor,
                    out bool isReactive))
            {
                tbxJjg596ErrorPower.Clear();
                tbxJjg596ErrorTime.Clear();
                tbxJjg596ErrorCorrectedPulseCount.Clear();
                labelJjg596ErrorHint.Text = string.Empty;
                return;
            }

            decimal trigFactor = isReactive
                ? GetReactiveFactor(powerFactor)
                : powerFactor;
            if (trigFactor <= 0)
            {
                tbxJjg596ErrorPower.Clear();
                tbxJjg596ErrorTime.Clear();
                tbxJjg596ErrorCorrectedPulseCount.Clear();
                labelJjg596ErrorHint.Text = string.Empty;
                return;
            }

            const decimal ki = 1m;
            const decimal ku = 1m;
            decimal power = phaseFactor * lineVoltage * currentValue * trigFactor;
            if (power <= 0)
            {
                tbxJjg596ErrorPower.Clear();
                tbxJjg596ErrorTime.Clear();
                tbxJjg596ErrorCorrectedPulseCount.Clear();
                labelJjg596ErrorHint.Text = string.Empty;
                return;
            }

            decimal timeSeconds = (3600000m * pulseCount) / (meterConstant * ki * ku * power);
            decimal correctedPulseCount = (10m * meterConstant * ki * ku * power) / 3600000m;
            decimal correctedPulseCeiling = decimal.Ceiling(correctedPulseCount);

            tbxJjg596ErrorPower.Text = $"{power.ToString("0.###", CultureInfo.InvariantCulture)} W";
            tbxJjg596ErrorTime.Text = $"{timeSeconds.ToString("0.####", CultureInfo.InvariantCulture)} s";
            tbxJjg596ErrorCorrectedPulseCount.Text = $"{correctedPulseCeiling.ToString(CultureInfo.InvariantCulture)}";
            labelJjg596ErrorHint.Text = timeSeconds < 10m
                ? $"提醒：当前 T = {timeSeconds.ToString("0.####", CultureInfo.InvariantCulture)}s，小于 10s。请至少测 {correctedPulseCeiling.ToString(CultureInfo.InvariantCulture)} 个脉冲，保证单次测试不少于 10s。"
                : $"当前脉冲数满足 10s 要求。按 10s 反推的最少脉冲数为 {correctedPulseCeiling.ToString(CultureInfo.InvariantCulture)}。";
        }

        private static int RoundSeconds(decimal seconds)
        {
            decimal integerPart = decimal.Truncate(seconds);
            decimal fraction = seconds - integerPart;
            return fraction > 0.5m
                ? (int)integerPart + 1
                : (int)integerPart;
        }

        private bool TryGetCreepTimeParameters(
            out decimal bValue,
            out decimal meterConstant,
            out decimal dValue,
            out decimal voltageValue,
            out decimal iminValue)
        {
            bValue = 0;
            meterConstant = 0;
            dValue = 0;
            voltageValue = 0;
            iminValue = 0;

            if (!TryGetActiveClassBValue(cbxJjg596ActiveClass.Text.Trim(), out bValue))
            {
                return false;
            }

            if (!decimal.TryParse(cbxJjg596MeterConstant.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out meterConstant) ||
                meterConstant <= 0)
            {
                return false;
            }

            if (!TryGetMeasurementUnitFactor(cbxJjg596MeasurementUnit.Text.Trim(), out dValue))
            {
                return false;
            }

            if (!TryGetVoltageValue(cbxJjg596Voltage.Text.Trim(), out voltageValue))
            {
                return false;
            }

            if (!TryGetAmpereValue(tbxJjg596Imin.Text.Trim(), out iminValue))
            {
                return false;
            }

            return true;
        }

        private bool TryGetStartTimeParameters(
            out decimal estRatio,
            out decimal meterConstant,
            out decimal unitFactor,
            out decimal voltageValue,
            out decimal startCurrent)
        {
            estRatio = 0;
            meterConstant = 0;
            unitFactor = 0;
            voltageValue = 0;
            startCurrent = 0;

            if (!TryGetActiveClassBValue(cbxJjg596ActiveClass.Text.Trim(), out decimal estPercent))
            {
                return false;
            }

            estRatio = estPercent / 100m;

            if (!decimal.TryParse(cbxJjg596MeterConstant.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out meterConstant) ||
                meterConstant <= 0)
            {
                return false;
            }

            if (!TryGetMeasurementUnitFactor(cbxJjg596MeasurementUnit.Text.Trim(), out unitFactor))
            {
                return false;
            }

            if (!TryGetVoltageValue(cbxJjg596Voltage.Text.Trim(), out voltageValue))
            {
                return false;
            }

            if (!TryGetStartCurrentValue(out startCurrent))
            {
                return false;
            }

            return true;
        }

        private bool TryGetErrorTimeParameters(
            out decimal meterConstant,
            out decimal currentValue,
            out decimal lineVoltage,
            out decimal powerFactor,
            out decimal pulseCount,
            out decimal phaseFactor,
            out bool isReactive)
        {
            meterConstant = 0;
            currentValue = 0;
            lineVoltage = 0;
            powerFactor = 0;
            pulseCount = 0;
            phaseFactor = 0;
            isReactive = false;

            if (!decimal.TryParse(cbxJjg596MeterConstant.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out meterConstant) ||
                meterConstant <= 0)
            {
                return false;
            }

            if (!TryGetAmpereValue(tbxJjg596ErrorCurrent.Text.Trim(), out currentValue))
            {
                return false;
            }

            if (!TryGetLineVoltageValue(cbxJjg596Voltage.Text.Trim(), out lineVoltage))
            {
                return false;
            }

            if (!decimal.TryParse(cbxJjg596ErrorPowerFactor.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out powerFactor) ||
                powerFactor <= 0 ||
                powerFactor > 1)
            {
                return false;
            }

            if (!decimal.TryParse(tbxJjg596ErrorPulseCount.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out pulseCount) ||
                pulseCount <= 0)
            {
                return false;
            }

            string measurementUnit = cbxJjg596MeasurementUnit.Text.Trim();
            if (string.Equals(measurementUnit, "单相", StringComparison.Ordinal))
            {
                phaseFactor = 1m;
            }
            else
            {
                string errorPhase = cbxJjg596ErrorPhase.Text.Trim();
                phaseFactor = string.Equals(errorPhase, "H", StringComparison.OrdinalIgnoreCase)
                    ? (decimal)Math.Sqrt(3d)
                    : 1m;
            }

            isReactive = string.Equals(cbxJjg596ErrorPowerType.Text.Trim(), "无功", StringComparison.Ordinal);
            return true;
        }

        private bool TryGetStartCurrentValue(out decimal startCurrent)
        {
            startCurrent = 0;

            if (!TryGetAmpereValue(tbxJjg596Itr.Text.Trim(), out decimal itrValue))
            {
                return false;
            }

            string activeClass = cbxJjg596ActiveClass.Text.Trim();
            bool isTransformerAccess = cbxJjg596AccessMode.Text.Contains("互感器", StringComparison.Ordinal);

            decimal factor = (activeClass, isTransformerAccess) switch
            {
                ("A", false) => 0.05m,
                ("B", false) => 0.04m,
                ("C", false) => 0.04m,
                ("D", false) => 0.04m,
                ("A", true) => 0.05m,
                ("B", true) => 0.04m,
                ("C", true) => 0.02m,
                ("D", true) => 0.04m,
                _ => 0m
            };

            if (factor <= 0)
            {
                return false;
            }

            startCurrent = itrValue * factor;
            return startCurrent > 0;
        }

        private static bool TryGetActiveClassBValue(string activeClass, out decimal bValue)
        {
            bValue = activeClass switch
            {
                "A" => 2.5m,
                "B" => 1.5m,
                "C" => 1.0m,
                "D" => 0.4m,
                _ => 0m
            };

            return bValue > 0;
        }

        private static bool TryGetMeasurementUnitFactor(string measurementUnit, out decimal dValue)
        {
            dValue = measurementUnit switch
            {
                "单相" => 1m,
                "三相三线" => 2m,
                "三相四线" => 3m,
                _ => 0m
            };

            return dValue > 0;
        }

        private static bool TryGetVoltageValue(string voltageText, out decimal voltageValue)
        {
            voltageValue = 0;
            if (string.IsNullOrWhiteSpace(voltageText))
            {
                return false;
            }

            string normalized = voltageText.Trim().Replace("×", "x", StringComparison.Ordinal);
            if (normalized.StartsWith("3x", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[2..];
            }

            int slashIndex = normalized.IndexOf('/');
            if (slashIndex >= 0)
            {
                normalized = normalized[..slashIndex];
            }

            normalized = normalized.Replace("V", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out voltageValue) && voltageValue > 0;
        }

        private static bool TryGetLineVoltageValue(string voltageText, out decimal voltageValue)
        {
            voltageValue = 0;
            if (string.IsNullOrWhiteSpace(voltageText))
            {
                return false;
            }

            string normalized = voltageText.Trim().Replace("×", "x", StringComparison.Ordinal);
            if (normalized.StartsWith("3x", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized[2..];
            }

            int slashIndex = normalized.IndexOf('/');
            if (slashIndex >= 0)
            {
                normalized = normalized[(slashIndex + 1)..];
            }

            normalized = normalized.Replace("V", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
            return decimal.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out voltageValue) && voltageValue > 0;
        }

        private static bool TryGetAmpereValue(string text, out decimal value)
        {
            value = 0;
            Match match = Regex.Match(text, @"\d+(?:\.\d+)?");
            return match.Success
                && decimal.TryParse(match.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                && value > 0;
        }

        private static decimal GetReactiveFactor(decimal powerFactor)
        {
            if (powerFactor < 0 || powerFactor > 1)
            {
                return 0;
            }

            decimal square = 1m - (powerFactor * powerFactor);
            if (square < 0)
            {
                square = 0;
            }

            return (decimal)Math.Sqrt((double)square);
        }

        private void BindSgccOadItems()
        {
            string category = cbxSgccOadCategory.Text;
            cbxSgccOAD.DataSource = SGCCOadConfig.GetServiceNamesByCategory(category).ToList();
            cbxSgccOAD.SelectedIndex = -1;
        }

        private async Task SendAsync(string message)
        {
            if (SendMessageRequested == null)
            {
                WriteLog("国网测试发送事件未绑定");
                return;
            }

            await SendMessageRequested.Invoke(message);
        }

        private void WriteLog(string message)
        {
            LogRequested?.Invoke(message);
        }
    }
}
