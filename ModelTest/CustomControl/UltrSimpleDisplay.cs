using System.ComponentModel;
using System.Globalization;
using System.Drawing.Drawing2D;
using ModelTest.Protocol;
using ModelTest.Tools;

namespace ModelTest.CustomControl
{
    public partial class UltrSimpleDisplay : UserControl
    {
        private const string MCUStartByte = "55";
        private const string MCUStopByte = "AA";
        private const string MCUCtrl = "00";
        private const int ResponseTimeoutMilliseconds = 5000;

        private double _displayValue;
        private readonly object _responseLock = new();
        private PendingResponse? _pendingResponse;
        private ErrorInstrumentProtocolVersion _protocolVersion = ErrorInstrumentProtocolVersion.V1;

        public UltrSimpleDisplay()
        {
            InitializeComponent();
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            UpdateStyles();
            cbxErrorTest.SelectedIndex = 0;
            cbxErrorTextClass.SelectedIndex = 0;
            tbxVoltage.Text = "220";
            tbxCurrent.Text = "5";
            tbxDNBC.Text = ErrorTestConstantHelper.DefaultMeterConstant.ToString(CultureInfo.InvariantCulture);
            tbxRJSC.Text = "10";
            RefreshStandardConstant();
        }

        public event Func<string, Task>? SendCommandRequested;

        public event Action<string>? LogRequested;

        public Func<string>? TerminalAddressProvider { get; set; }

        [Category("Behavior")]
        [Description("误差仪协议版本。V1 使用 55...AA，V2 使用 55 44...AA BB。")]
        public ErrorInstrumentProtocolVersion ProtocolVersion
        {
            get => _protocolVersion;
            set => _protocolVersion = value;
        }

        public void HandleReceivedMessage(string messageHex)
        {
            string normalized = NormalizeHex(messageHex);
            if (!TryParseMcuFrame(normalized, out string command, out string dataItem))
            {
                return;
            }

            TryDisplayErrorResult(command, dataItem);

            PendingResponse? pending;
            lock (_responseLock)
            {
                pending = _pendingResponse;
            }

            if (pending == null ||
                !string.Equals(command, pending.Command, StringComparison.OrdinalIgnoreCase) ||
                !dataItem.StartsWith(pending.DataPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            pending.TaskSource.TrySetResult(normalized);
        }

        [Category("Data")]
        [Description("数码管显示的数值。")]
        public double DisplayValue
        {
            get => _displayValue;
            set
            {
                if (Math.Abs(_displayValue - value) < double.Epsilon)
                {
                    return;
                }

                _displayValue = value;
                Invalidate();
            }
        }

        private void simpleDisplay(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(displayBackColor);

            int reservedBottom = errorPanel.Visible ? errorPanel.Height : 0;
            Rectangle content = new(
                Padding.Left,
                Padding.Top,
                Math.Max(1, ClientSize.Width - Padding.Horizontal),
                Math.Max(1, ClientSize.Height - Padding.Vertical - reservedBottom));
            if (content.Width <= 0 || content.Height <= 0)
            {
                return;
            }

            string displayText = FormatForDisplay(_displayValue);
            DrawHeader(g, displayText);

            Rectangle digitArea = new(
                content.Left,
                content.Top + 30,
                content.Width,
                Math.Max(1, content.Height - 36));

            int digitWidth = CalculateDigitWidth(displayText, digitArea.Width);
            int bottomLabelSpace = 26;
            int digitHeight = Math.Min(defaultDigitHeight, Math.Max(46, digitArea.Height - bottomLabelSpace));
            int totalWidth = displayText.Length * digitWidth + Math.Max(0, displayText.Length - 1) * digitSpacing;
            int startX = digitArea.Left + Math.Max(0, (digitArea.Width - totalWidth) / 2);
            int startY = digitArea.Top + Math.Max(0, (digitArea.Height - digitHeight - bottomLabelSpace) / 2);

            for (int i = 0; i < displayText.Length; i++)
            {
                int x = startX + i * (digitWidth + digitSpacing);
                DrawDigit(g, displayText[i], x, startY, digitWidth, digitHeight);
            }
        }

        private void DrawDigit(Graphics g, char c, int x, int y, int width, int height)
        {
            bool[] segments = GetSegmentsForChar(c);

            int segmentThickness = Math.Max(4, width / 8);
            int horizontalLength = width - 2 * segmentThickness;
            int verticalLength = (height - 3 * segmentThickness) / 2;

            Rectangle[] segmentRects = new Rectangle[7];
            segmentRects[0] = new Rectangle(x + segmentThickness, y, horizontalLength, segmentThickness);
            segmentRects[1] = new Rectangle(x + width - segmentThickness, y + segmentThickness, segmentThickness, verticalLength);
            segmentRects[2] = new Rectangle(x + width - segmentThickness, y + segmentThickness + verticalLength + segmentThickness, segmentThickness, verticalLength);
            segmentRects[3] = new Rectangle(x + segmentThickness, y + height - segmentThickness, horizontalLength, segmentThickness);
            segmentRects[4] = new Rectangle(x, y + segmentThickness + verticalLength + segmentThickness, segmentThickness, verticalLength);
            segmentRects[5] = new Rectangle(x, y + segmentThickness, segmentThickness, verticalLength);
            segmentRects[6] = new Rectangle(x + segmentThickness, y + segmentThickness + verticalLength, horizontalLength, segmentThickness);

            for (int i = 0; i < 7; i++)
            {
                DrawSegment(g, segmentRects[i], segments[i]);
            }

            // 绘制小数点
            if (c == '.')
            {
                Rectangle dpRect = new Rectangle(
                    x + width,
                    y + height - segmentThickness * 2,
                    segmentThickness,
                    segmentThickness);
                DrawSegment(g, dpRect, true);
            }

            using Pen framePen = new(frameColor);
            g.DrawRectangle(framePen, x, y, width, height);

            using Font charFont = new("Arial", 9);
            using SolidBrush textBrush = new(mutedTextColor);
            g.DrawString(c.ToString(), charFont, textBrush, x + width / 2 - 5, y + height + 3);
        }

        public string FormatForDisplay(double value)
        {
            string str = value.ToString("F7");
            if (str.Length > digitCount)
            {
                str = value.ToString("E7");
            }
            return str;
        }

        private void DrawSegment(Graphics g, Rectangle rect, bool isOn)
        {
            Color fillColor = isOn ? onColor : offColor;
            Color glowColor = isOn ? Color.FromArgb(100, onColor) : Color.Transparent;

            if (isOn)
            {
                using SolidBrush glowBrush = new(glowColor);
                Rectangle glowRect = new(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
                g.FillRectangle(glowBrush, glowRect);
            }

            using SolidBrush brush = new(fillColor);
            g.FillRectangle(brush, rect);

            using Pen borderPen = new(frameColor);
            g.DrawRectangle(borderPen, rect);

            if (isOn)
            {
                using Pen highlightPen = new(Color.FromArgb(150, Color.White), 1);
                g.DrawLine(highlightPen, rect.Left, rect.Top, rect.Right, rect.Top);
                g.DrawLine(highlightPen, rect.Left, rect.Top, rect.Left, rect.Bottom);
            }
        }

        private bool[] GetSegmentsForChar(char c)
        {
            return c switch
            {
                '0' => [true, true, true, true, true, true, false],
                '1' => [false, true, true, false, false, false, false],
                '2' => [true, true, false, true, true, false, true],
                '3' => [true, true, true, true, false, false, true],
                '4' => [false, true, true, false, false, true, true],
                '5' => [true, false, true, true, false, true, true],
                '6' => [true, false, true, true, true, true, true],
                '7' => [true, true, true, false, false, false, false],
                '8' => [true, true, true, true, true, true, true],
                '9' => [true, true, true, true, false, true, true],
                '-' => [false, false, false, false, false, false, true],
                '.' => [false, false, false, false, false, false, false],
                'E' or 'e' => [true, false, false, true, true, true, true],
                _ => [false, false, false, false, false, false, false]
            };
        }

        private int CalculateDigitWidth(string displayText, int availableWidth)
        {
            int maxWidth = (availableWidth - Math.Max(0, displayText.Length - 1) * digitSpacing) / Math.Max(1, displayText.Length);
            return Math.Max(18, Math.Min(defaultDigitWidth, maxWidth));
        }

        private void DrawHeader(Graphics g, string displayText)
        {
            using Font infoFont = new("Microsoft YaHei UI", 9F);
            using SolidBrush valueBrush = new(ForeColor);
            using SolidBrush mutedBrush = new(mutedTextColor);
            g.DrawString($"显示值: {_displayValue}", infoFont, valueBrush, Padding.Left, Padding.Top);
            g.DrawString($"显示内容: {displayText}", infoFont, valueBrush, Padding.Left + 210, Padding.Top);
            g.DrawString("模拟显示", infoFont, mutedBrush, Padding.Left + 430, Padding.Top);
        }

        private void UltrSimpleDisplay_Resize(object? sender, EventArgs e)
        {
            Invalidate();
        }

        private async void btnStartErrorTerminal_Click(object? sender, EventArgs e)
        {
            LogMessage.Info(sender?.ToString() ?? string.Empty);
            RefreshStandardConstant();

            btnStartErrorTerminal.Enabled = false;

            try
            {
                if (!TryReadExperimentSettings(
                        out string experimentType,
                        out string experimentMode,
                        out uint standardConstant,
                        out uint meterConstant,
                        out ushort circleCount))
                {
                    return;
                }

                if (experimentType == "01")
                {
                    if (!await SendCommandAndWaitAsync("设置标准表有功脉冲常数", GetConstantCommand(), BuildConstantData("01", standardConstant)))
                    {
                        return;
                    }

                    if (!await SendCommandAndWaitAsync("设置待测表有功脉冲常数", GetConstantCommand(), BuildConstantData("03", meterConstant)))
                    {
                        return;
                    }
                }
                else if (experimentType == "02")
                {
                    if (!await SendCommandAndWaitAsync("设置标准表无功脉冲常数", GetConstantCommand(), BuildConstantData("02", standardConstant)))
                    {
                        return;
                    }

                    if (!await SendCommandAndWaitAsync("设置待测表无功脉冲常数", GetConstantCommand(), BuildConstantData("04", meterConstant)))
                    {
                        return;
                    }
                }
                else if (experimentType == "03")
                {
                    if (experimentMode == "03")
                    {
                        LogRequested?.Invoke("日计时实验不支持光脉冲方式，请切换实验方式。");
                        return;
                    }

                    if (!await SendCommandAndWaitAsync("设置时钟频率", GetConstantCommand(), BuildConstantData("05", standardConstant)))
                    {
                        return;
                    }
                }

                string circleData = BuildExperimentData(experimentType, experimentMode, "03", circleCount);
                if (!await SendCommandAndWaitAsync("设置实验圈数", GetExperimentCommand(), circleData, GetExperimentAckPrefix(experimentType, experimentMode, "03")))
                {
                    return;
                }

                string startData = BuildExperimentData(experimentType, experimentMode, "01", circleCount);
                await SendCommandAndWaitAsync("启动误差实验", GetExperimentCommand(), startData, GetExperimentAckPrefix(experimentType, experimentMode, "01"));
            }
            finally
            {
                btnStartErrorTerminal.Enabled = true;
            }
        }

        private async void btnStopErrorTerminal_Click(object? sender, EventArgs e)
        {
            LogMessage.Info(sender?.ToString() ?? string.Empty);

            btnStopErrorTerminal.Enabled = false;

            try
            {
                if (!TryReadExperimentControlSettings(
                        out string experimentType,
                        out string experimentMode,
                        out ushort circleCount))
                {
                    return;
                }

                string stopData = BuildExperimentData(experimentType, experimentMode, "02", circleCount);
                await SendCommandAndWaitAsync("停止误差实验", GetExperimentCommand(), stopData, GetExperimentAckPrefix(experimentType, experimentMode, "02"));
            }
            finally
            {
                btnStopErrorTerminal.Enabled = true;
            }
        }

        private async void btnGetErrorResultTerminal_Click(object? sender, EventArgs e)
        {
            LogMessage.Info(sender?.ToString() ?? string.Empty);

            btnGetErrorResultTerminal.Enabled = false;

            try
            {
                if (!TryReadExperimentControlSettings(
                        out string experimentType,
                        out string experimentMode,
                        out ushort circleCount))
                {
                    return;
                }

                string resultData = BuildExperimentData(experimentType, experimentMode, "AA", circleCount);
                await SendCommandAndWaitAsync("获取误差实验结果", GetExperimentCommand(), resultData, GetExperimentAckPrefix(experimentType, experimentMode, "AA"));
            }
            finally
            {
                btnGetErrorResultTerminal.Enabled = true;
            }
        }

        private async Task<bool> SendCommandAndWaitAsync(string stepName, string command, string dataItem, string? expectedDataPrefix = null)
        {
            expectedDataPrefix ??= dataItem;
            string message = BuildMcuMessage(command, dataItem);
            LogRequested?.Invoke($"{stepName}：{message}");
            LogMessage.Debug($"误差仪-{stepName}，等待应答命令={command}，数据={expectedDataPrefix}");

            if (SendCommandRequested == null)
            {
                LogRequested?.Invoke("误差仪发送事件未绑定");
                return false;
            }

            Task<string> responseTask = WaitForResponseAsync(command, expectedDataPrefix);
            await SendCommandRequested.Invoke(message);

            string? response = await Task.WhenAny(responseTask, Task.Delay(ResponseTimeoutMilliseconds)) == responseTask
                ? await responseTask
                : null;

            ClearPendingResponse(command, expectedDataPrefix);

            if (string.IsNullOrEmpty(response))
            {
                LogRequested?.Invoke($"{stepName}失败：{ResponseTimeoutMilliseconds / 1000}秒内未收到应答。");
                return false;
            }

            LogRequested?.Invoke($"{stepName}应答成功：{response}");
            return true;
        }

        private void VoltageOrCurrent_TextChanged(object? sender, EventArgs e)
        {
            RefreshStandardConstant();
        }

        private void RefreshStandardConstant()
        {
            if (!ErrorTestConstantHelper.TryCalculateConstants(
                    tbxVoltage.Text,
                    tbxCurrent.Text,
                    out ulong standardConstant,
                    out _))
            {
                return;
            }

            tbxBZBC.Text = FormatConstant(standardConstant);
        }

        private static string FormatConstant(double value)
        {
            return value.ToString("0.################");
        }

        private bool TryReadExperimentSettings(
            out string experimentType,
            out string experimentMode,
            out uint standardConstant,
            out uint meterConstant,
            out ushort circleCount)
        {
            experimentType = GetExperimentType();
            experimentMode = GetExperimentMode();
            standardConstant = 0;
            meterConstant = 0;
            circleCount = 0;

            if (!TryParseUInt32(tbxBZBC.Text, "标准表常数", out standardConstant) ||
                !TryParseUInt32(tbxDNBC.Text, "电能表常数", out meterConstant) ||
                !TryParseUInt16(tbxRJSC.Text, "圈数", out circleCount))
            {
                return false;
            }

            return true;
        }

        private bool TryReadExperimentControlSettings(
            out string experimentType,
            out string experimentMode,
            out ushort circleCount)
        {
            experimentType = GetExperimentType();
            experimentMode = GetExperimentMode();
            circleCount = 0;

            if (experimentType == "03" && experimentMode == "03")
            {
                LogRequested?.Invoke("日计时实验不支持光脉冲方式，请切换实验方式。");
                return false;
            }

            return TryParseUInt16(tbxRJSC.Text, "圈数", out circleCount);
        }

        private bool TryParseUInt32(string text, string name, out uint value)
        {
            value = 0;
            if (!ErrorTestConstantHelper.TryParseInputNumber(text, out double parsed) || parsed < 0 || parsed > uint.MaxValue)
            {
                LogRequested?.Invoke($"{name}不合法，必须是0到{uint.MaxValue}之间的整数。");
                return false;
            }

            value = Convert.ToUInt32(Math.Round(parsed));
            return true;
        }

        private bool TryParseUInt16(string text, string name, out ushort value)
        {
            value = 0;
            if (!ErrorTestConstantHelper.TryParseInputNumber(text, out double parsed) || parsed < 0 || parsed > ushort.MaxValue)
            {
                LogRequested?.Invoke($"{name}不合法，必须是0到{ushort.MaxValue}之间的整数。");
                return false;
            }

            value = Convert.ToUInt16(Math.Round(parsed));
            return true;
        }

        private string GetExperimentType()
        {
            return cbxErrorTextClass.SelectedIndex switch
            {
                1 => "02",
                2 => "03",
                _ => "01"
            };
        }

        private string GetExperimentMode()
        {
            return cbxErrorTest.SelectedIndex switch
            {
                1 => "02",
                2 => "03",
                _ => "01"
            };
        }

        private string BuildConstantData(string constantType, uint value)
        {
            // 常数设置数据项：字节1=常数类型，字节2~6=设置值小端5字节。
            // 例如标准表常数50000000：32 01 80 F0 FA 02 00。
            return constantType + HexConverter.ConvertHex(value.ToString("X"), 5);
        }

        private static string BuildExperimentData(string experimentType, string experimentMode, string action, ushort circleCount)
        {
            return experimentType + experimentMode + action + HexConverter.ConvertHex(circleCount.ToString("X"), 2);
        }

        private static string GetExperimentAckPrefix(string experimentType, string experimentMode, string action)
        {
            return experimentType + experimentMode + action;
        }

        private string BuildMcuMessage(string command, string dataItem)
        {
            string terminalAddress = TerminalAddressProvider?.Invoke() ?? string.Empty;
            if (ProtocolVersion == ErrorInstrumentProtocolVersion.V2)
            {
                byte address = string.IsNullOrWhiteSpace(terminalAddress)
                    ? (byte)0x01
                    : new DetectionBoardProtocolV2().ConvertStationDecimalToByte(terminalAddress);
                byte commandCode = Convert.ToByte(command, 16);
                byte[] data = ModelTool.HexStringToByteArray(dataItem);
                byte[] frame = new DetectionBoardProtocolV2().BuildControlFrame(address, deviceType: 2, commandCode, data);
                return DetectionBoardProtocolV2.ToHexString(frame).Replace(" ", string.Empty);
            }

            string terminalDataLength = HexConverter.ConvertHex(ModelTool.ToHex(2 + 3 + dataItem.Length / 2 + 1), 2);

            return TerminalModel.TerminalByte(
                MCUStartByte,
                terminalDataLength,
                terminalAddress,
                MCUCtrl,
                command,
                dataItem,
                MCUStopByte);
        }

        private Task<string> WaitForResponseAsync(string command, string dataPrefix)
        {
            TaskCompletionSource<string> source = new(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (_responseLock)
            {
                _pendingResponse = new PendingResponse(command, dataPrefix, source);
            }

            return source.Task;
        }

        private void ClearPendingResponse(string command, string dataPrefix)
        {
            lock (_responseLock)
            {
                if (_pendingResponse != null &&
                    string.Equals(_pendingResponse.Command, command, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(_pendingResponse.DataPrefix, dataPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    _pendingResponse = null;
                }
            }
        }

        private static bool TryParseMcuFrame(string messageHex, out string command, out string dataItem)
        {
            command = string.Empty;
            dataItem = string.Empty;

            if (TryParseV2Frame(messageHex, out command, out dataItem))
            {
                return true;
            }

            if (messageHex.Length < 16 ||
                !messageHex.StartsWith("55", StringComparison.OrdinalIgnoreCase) ||
                !messageHex.EndsWith("AA", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            command = messageHex.Substring(10, 2);
            dataItem = messageHex.Length > 16 ? messageHex.Substring(12, messageHex.Length - 16) : string.Empty;
            return true;
        }

        private void TryDisplayErrorResult(string command, string dataItem)
        {
            if (!string.Equals(command, GetExperimentCommand(), StringComparison.OrdinalIgnoreCase) ||
                dataItem.Length < 14 ||
                !string.Equals(dataItem.Substring(4, 2), "AA", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            string resultHex = dataItem.Substring(6, 8);
            float result = ParseLittleEndianFloat(resultHex);
            DisplayValue = result;
            LogRequested?.Invoke($"误差实验结果：{result:F7}");
            LogMessage.Debug($"误差仪-解析实验结果：数据={resultHex}，结果={result:F7}");
        }

        private static float ParseLittleEndianFloat(string littleEndianHex)
        {
            byte[] bytes = ModelTool.HexStringToByteArray(littleEndianHex);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToSingle(bytes, 0);
        }

        private static string NormalizeHex(string message)
        {
            return new string(message.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        }

        private string GetExperimentCommand()
        {
            return ProtocolVersion == ErrorInstrumentProtocolVersion.V2 ? "3D" : "2F";
        }

        private string GetConstantCommand()
        {
            return ProtocolVersion == ErrorInstrumentProtocolVersion.V2 ? "3E" : "32";
        }

        private static bool TryParseV2Frame(string messageHex, out string command, out string dataItem)
        {
            command = string.Empty;
            dataItem = string.Empty;

            if (messageHex.Length < 20 ||
                !messageHex.StartsWith("5544", StringComparison.OrdinalIgnoreCase) ||
                !messageHex.EndsWith("AABB", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            try
            {
                byte[] frameBytes = ModelTool.HexStringToByteArray(messageHex);
                DetectionBoardProtocolV2 protocol = new();
                if (!protocol.TryParseFrame(frameBytes, out DetectionBoardProtocolV2Frame? frame, out _))
                {
                    return false;
                }

                command = frame!.CommandCode.ToString("X2");
                dataItem = BitConverter.ToString(frame.Data).Replace("-", string.Empty);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed record PendingResponse(string Command, string DataPrefix, TaskCompletionSource<string> TaskSource);
    }

    public enum ErrorInstrumentProtocolVersion
    {
        V1,
        V2
    }
}
