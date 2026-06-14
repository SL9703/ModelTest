using System.ComponentModel;
using System.Drawing.Drawing2D;
using ModelTest.Tools;

namespace ModelTest.CustomControl
{
    public partial class UltrSimpleDisplay : UserControl
    {
        private const string MCUStartByte = "55";
        private const string MCUStopByte = "AA";
        private const string MCUCtrl = "00";
        private static readonly double[] VoltageRanges = [60D, 120D, 240D, 480D];
        private static readonly double[] CurrentRanges = [100D, 50D, 25D, 10D, 5D, 2.5D, 1D, 0.5D, 0.25D, 0.1D, 0.05D, 0.025D];
        private static readonly double[,] StandardConstantTable =
        {
            { 1E7, 2E7, 4E7, 1E8, 2E8, 4E8, 1E9, 2E9, 4E9, 1E10, 2E10, 4E10 },
            { 5E6, 1E7, 2E7, 5E7, 1E8, 2E8, 5E8, 1E9, 2E9, 5E9, 1E10, 2E10 },
            { 2.5E6, 5E6, 1E7, 2.5E7, 5E7, 1E8, 2.5E8, 5E8, 1E9, 2.5E9, 5E9, 1E10 },
            { 1.25E6, 2.5E6, 5E6, 1.25E7, 2.5E7, 5E7, 1.25E8, 2.5E8, 5E8, 1.25E9, 2.5E9, 5E9 }
        };
        private const int ResponseTimeoutMilliseconds = 5000;

        private double _displayValue;
        private readonly object _responseLock = new();
        private PendingResponse? _pendingResponse;

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
            tbxDNBC.Text = "10000";
            tbxRJSC.Text = "10";
            RefreshStandardConstant();
        }

        public event Func<string, Task>? SendCommandRequested;

        public event Action<string>? LogRequested;

        public Func<string>? TerminalAddressProvider { get; set; }

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
                    if (!await SendCommandAndWaitAsync("设置标准表有功脉冲常数", "32", BuildConstantData("01", standardConstant)))
                    {
                        return;
                    }

                    if (!await SendCommandAndWaitAsync("设置待测表有功脉冲常数", "32", BuildConstantData("03", meterConstant)))
                    {
                        return;
                    }
                }
                else if (experimentType == "02")
                {
                    if (!await SendCommandAndWaitAsync("设置标准表无功脉冲常数", "32", BuildConstantData("02", standardConstant)))
                    {
                        return;
                    }

                    if (!await SendCommandAndWaitAsync("设置待测表无功脉冲常数", "32", BuildConstantData("04", meterConstant)))
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

                    if (!await SendCommandAndWaitAsync("设置时钟频率", "32", BuildConstantData("05", standardConstant)))
                    {
                        return;
                    }
                }

                string circleData = BuildExperimentData(experimentType, experimentMode, "03", circleCount);
                if (!await SendCommandAndWaitAsync("设置实验圈数", "2F", circleData, GetExperimentAckPrefix(experimentType, experimentMode, "03")))
                {
                    return;
                }

                string startData = BuildExperimentData(experimentType, experimentMode, "01", circleCount);
                await SendCommandAndWaitAsync("启动误差实验", "2F", startData, GetExperimentAckPrefix(experimentType, experimentMode, "01"));
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
                if (!TryReadExperimentSettings(
                        out string experimentType,
                        out string experimentMode,
                        out _,
                        out _,
                        out ushort circleCount))
                {
                    return;
                }

                string stopData = BuildExperimentData(experimentType, experimentMode, "02", circleCount);
                await SendCommandAndWaitAsync("停止误差实验", "2F", stopData, GetExperimentAckPrefix(experimentType, experimentMode, "02"));
            }
            finally
            {
                btnStopErrorTerminal.Enabled = true;
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
            if (!TryParseInputNumber(tbxVoltage.Text, out double voltage) ||
                !TryParseInputNumber(tbxCurrent.Text, out double current))
            {
                return;
            }

            int voltageIndex = FindAscendingRangeIndex(VoltageRanges, voltage);
            int currentIndex = FindDescendingRangeIndex(CurrentRanges, current);
            tbxBZBC.Text = FormatConstant(StandardConstantTable[voltageIndex, currentIndex]);
        }

        private static bool TryParseInputNumber(string text, out double value)
        {
            string normalized = text
                .Trim()
                .Replace("V", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("A", string.Empty, StringComparison.OrdinalIgnoreCase);
            return double.TryParse(normalized, out value);
        }

        private static int FindAscendingRangeIndex(double[] ranges, double value)
        {
            for (int i = 0; i < ranges.Length; i++)
            {
                if (value <= ranges[i])
                {
                    return i;
                }
            }

            return ranges.Length - 1;
        }

        private static int FindDescendingRangeIndex(double[] ranges, double value)
        {
            for (int i = ranges.Length - 1; i >= 0; i--)
            {
                if (value <= ranges[i])
                {
                    return i;
                }
            }

            return 0;
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

        private bool TryParseUInt32(string text, string name, out uint value)
        {
            value = 0;
            if (!TryParseInputNumber(text, out double parsed) || parsed < 0 || parsed > uint.MaxValue)
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
            if (!TryParseInputNumber(text, out double parsed) || parsed < 0 || parsed > ushort.MaxValue)
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

        private static string BuildConstantData(string constantType, uint value)
        {
            return constantType + HexConverter.ConvertHex(value.ToString("X"), 4);
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
            if (!string.Equals(command, "2F", StringComparison.OrdinalIgnoreCase) ||
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

        private sealed record PendingResponse(string Command, string DataPrefix, TaskCompletionSource<string> TaskSource);
    }
}
