using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ModelTest
{
    public partial class ProtocolParserForm : Form
    {
        private const string TerminalProtocolName = "终端检测装置电测协议V1";
        private const string MeterHardwareProtocolName = "电表检测装置硬件通信协议V1";
        private const string TerminalProtocolPdfRelativePath = "Protocol/TerminalElectricTestProtocolV1.pdf";
        private const string TerminalProtocolTextRelativePath = "Protocol/TerminalElectricTestProtocolV1.txt";
        private const string MeterHardwareProtocolPdfRelativePath = "Protocol/MeterHardwareProtocolV1.pdf";
        private const string MeterHardwareProtocolTextRelativePath = "Protocol/MeterHardwareProtocolV1.txt";
        private const string TerminalDefaultFrameText = "5507000100210730AA";
        private const string MeterHardwareDefaultFrameText = "55 08 00 00 01 00 00 00 09 AA";
        private static readonly Lazy<Dictionary<byte, List<ProtocolCommandInfo>>> TerminalProtocolCommandSections = new Lazy<Dictionary<byte, List<ProtocolCommandInfo>>>(() => LoadProtocolCommandSections(TerminalProtocolTextRelativePath));
        private static readonly Lazy<Dictionary<byte, List<ProtocolCommandInfo>>> MeterHardwareProtocolCommandSections = new Lazy<Dictionary<byte, List<ProtocolCommandInfo>>>(() => LoadProtocolCommandSections(MeterHardwareProtocolTextRelativePath));

        private readonly BindingSource _fieldBindingSource = new BindingSource();
        private readonly BindingSource _frameBindingSource = new BindingSource();
        private readonly List<ParseHistoryEntry> _parseHistory = new List<ParseHistoryEntry>();
        private bool _suppressTextChanged;
        private bool _suppressHistory;
        private bool _suppressProtocolChange;
        private int _historyIndex = -1;

        public ProtocolParserForm()
        {
            InitializeComponent();
            dgvFields.AutoGenerateColumns = false;
            dgvFrames.AutoGenerateColumns = false;
            dgvFields.DataSource = _fieldBindingSource;
            dgvFrames.DataSource = _frameBindingSource;
            CenterInputButtons();
            _suppressProtocolChange = true;
            cmbProtocol.SelectedIndex = 0;
            _suppressProtocolChange = false;
            UpdateProtocolOpenButtonState();
            LoadSelectedProtocolExample();
        }

        private void panelInputButtons_SizeChanged(object sender, EventArgs e)
        {
            CenterInputButtons();
        }

        private void CenterInputButtons()
        {
            int spacing = 10;
            int totalWidth = btnPreviousFrame.Width + btnPasteNew.Width + btnNextFrame.Width + spacing * 2;
            int rightControlsLeft = lblProtocol.Left > 0 ? lblProtocol.Left : panelInputButtons.Width;
            int left = Math.Max(0, (Math.Min(panelInputButtons.Width, rightControlsLeft) - totalWidth) / 2);
            int top = Math.Max(0, (panelInputButtons.Height - btnPasteNew.Height) / 2);

            btnPreviousFrame.Location = new Point(left, top);
            btnPasteNew.Location = new Point(btnPreviousFrame.Right + spacing, top);
            btnNextFrame.Location = new Point(btnPasteNew.Right + spacing, top);
        }

        private void txtProtocol_TextChanged(object sender, EventArgs e)
        {
            if (_suppressTextChanged)
            {
                return;
            }

            ParseInput();
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtProtocol.Clear();
        }

        private void btnPreviousFrame_Click(object sender, EventArgs e)
        {
            MoveHistory(-1);
        }

        private void btnPasteNew_Click(object sender, EventArgs e)
        {
            txtProtocol.Clear();

            if (!Clipboard.ContainsText())
            {
                lblStatus.Text = "剪贴板没有可粘贴的文本。";
                return;
            }

            txtProtocol.Text = Clipboard.GetText();
            lblStatus.Text = "已清空并粘贴新的报文内容。";
        }

        private void btnNextFrame_Click(object sender, EventArgs e)
        {
            MoveHistory(1);
        }

        private void cmbProtocol_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateProtocolOpenButtonState();
            if (_suppressProtocolChange)
            {
                return;
            }

            LoadSelectedProtocolExample();
        }

        private void btnOpenProtocol_Click(object sender, EventArgs e)
        {
            string relativePdfPath = GetSelectedProtocolPdfRelativePath();
            if (string.IsNullOrWhiteSpace(relativePdfPath))
            {
                lblStatus.Text = "请选择协议后再打开。";
                return;
            }

            string protocolPath = Path.Combine(Application.StartupPath, relativePdfPath);
            if (!File.Exists(protocolPath))
            {
                protocolPath = Path.Combine(AppContext.BaseDirectory, relativePdfPath);
            }

            if (!File.Exists(protocolPath))
            {
                MessageBox.Show($"未找到协议PDF文件：{protocolPath}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                lblStatus.Text = "未找到协议PDF文件。";
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = protocolPath,
                    UseShellExecute = true
                });

                lblStatus.Text = "已打开协议PDF。";
                LogMessage.Debug($"报文解析工具 | 打开协议PDF：{protocolPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开协议PDF失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "打开协议PDF失败。";
                LogMessage.Error("打开协议PDF失败", ex);
            }
        }

        private void btnCopyResult_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtResult.Text))
            {
                return;
            }

            Clipboard.SetText(txtResult.Text);
            lblStatus.Text = "解析结果已复制。";
        }

        private void UpdateProtocolOpenButtonState()
        {
            btnOpenProtocol.Enabled = !string.IsNullOrWhiteSpace(GetSelectedProtocolPdfRelativePath());
        }

        private string GetSelectedProtocolPdfRelativePath()
        {
            return cmbProtocol.Text switch
            {
                TerminalProtocolName => TerminalProtocolPdfRelativePath,
                MeterHardwareProtocolName => MeterHardwareProtocolPdfRelativePath,
                _ => string.Empty
            };
        }

        private void dgvFrames_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvFrames.CurrentRow?.DataBoundItem is ParsedFrame frame)
            {
                ShowFrame(frame);
            }
        }

        private void MoveSelectedFrame(int offset)
        {
            if (dgvFrames.Rows.Count == 0)
            {
                lblStatus.Text = "当前没有可切换的解析报文。";
                return;
            }

            int currentIndex = dgvFrames.CurrentRow?.Index ?? 0;
            int targetIndex = currentIndex + offset;

            if (targetIndex < 0)
            {
                targetIndex = 0;
                lblStatus.Text = "已经是第一条解析报文。";
            }
            else if (targetIndex >= dgvFrames.Rows.Count)
            {
                targetIndex = dgvFrames.Rows.Count - 1;
                lblStatus.Text = "已经是最后一条解析报文。";
            }

            dgvFrames.ClearSelection();
            dgvFrames.Rows[targetIndex].Selected = true;
            dgvFrames.CurrentCell = dgvFrames.Rows[targetIndex].Cells[0];
        }

        private void MoveHistory(int offset)
        {
            if (_parseHistory.Count == 0)
            {
                lblStatus.Text = "当前没有解析历史。";
                return;
            }

            int targetIndex = _historyIndex + offset;
            if (targetIndex < 0)
            {
                lblStatus.Text = "已经是第一条解析历史。";
                return;
            }

            if (targetIndex >= _parseHistory.Count)
            {
                lblStatus.Text = "已经是最后一条解析历史。";
                return;
            }

            ShowHistoryEntry(targetIndex);
        }

        private void LoadSelectedProtocolExample()
        {
            string exampleText = GetSelectedProtocolDefinition().HasDataDirection
                ? MeterHardwareDefaultFrameText
                : TerminalDefaultFrameText;

            _suppressTextChanged = true;
            txtProtocol.Text = exampleText;
            _suppressTextChanged = false;
            ParseInput();
        }

        private void ParseInput()
        {
            string input = txtProtocol.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                _frameBindingSource.DataSource = new BindingList<FrameSummaryRow>();
                _fieldBindingSource.DataSource = new BindingList<ProtocolFieldRow>();
                txtResult.Clear();
                lblStatus.Text = "粘贴报文后自动解析。";
                return;
            }

            try
            {
                List<byte> bytes = ExtractHexBytes(input);
                ProtocolDefinition protocolDefinition = GetSelectedProtocolDefinition();
                List<ParsedFrame> frames = ParseFrames(bytes, protocolDefinition);
                List<FrameSummaryRow> frameRows = BuildFrameSummaryRows(frames, protocolDefinition);

                _frameBindingSource.DataSource = new BindingList<FrameSummaryRow>(frameRows);

                if (frames.Count > 0)
                {
                    dgvFrames.ClearSelection();
                    if (dgvFrames.Rows.Count > 0)
                    {
                        dgvFrames.Rows[0].Selected = true;
                    }

                    ShowFrame(frames[0]);
                }
                else
                {
                    _fieldBindingSource.DataSource = new BindingList<ProtocolFieldRow>();
                    txtResult.Text = "未找到完整的 55 ... AA 报文。";
                }

                lblStatus.Text = $"已解析 {frames.Count} 帧，原始有效十六进制字节 {bytes.Count} 个。";
                if (!_suppressHistory && frames.Count > 0)
                {
                    AddHistoryEntry(input, protocolDefinition, frames, frameRows);
                }

                LogMessage.Debug($"报文解析工具 | 解析 {frames.Count} 帧");
            }
            catch (Exception ex)
            {
                _frameBindingSource.DataSource = new BindingList<FrameSummaryRow>();
                _fieldBindingSource.DataSource = new BindingList<ProtocolFieldRow>();
                txtResult.Text = ex.Message;
                lblStatus.Text = "解析失败。";
                LogMessage.Error("报文解析工具解析失败", ex);
            }
        }

        private void ShowFrame(ParsedFrame frame)
        {
            _fieldBindingSource.DataSource = new BindingList<ProtocolFieldRow>(frame.Fields);
            txtResult.Text = frame.DetailText;
        }

        private void AddHistoryEntry(string input, ProtocolDefinition protocolDefinition, List<ParsedFrame> frames, List<FrameSummaryRow> frameRows)
        {
            string normalizedInput = NormalizeHexText(input);
            if (_historyIndex >= 0
                && _historyIndex < _parseHistory.Count
                && string.Equals(_parseHistory[_historyIndex].NormalizedInputText, normalizedInput, StringComparison.Ordinal)
                && string.Equals(_parseHistory[_historyIndex].ProtocolName, protocolDefinition.Name, StringComparison.Ordinal))
            {
                return;
            }

            if (_historyIndex < _parseHistory.Count - 1)
            {
                _parseHistory.RemoveRange(_historyIndex + 1, _parseHistory.Count - _historyIndex - 1);
            }

            _parseHistory.Add(new ParseHistoryEntry(
                protocolDefinition.Name,
                input,
                normalizedInput,
                frames,
                frameRows));
            _historyIndex = _parseHistory.Count - 1;
            lblStatus.Text = $"已解析 {frames.Count} 帧，历史 {_historyIndex + 1}/{_parseHistory.Count}。";
        }

        private void ShowHistoryEntry(int targetIndex)
        {
            ParseHistoryEntry entry = _parseHistory[targetIndex];
            _historyIndex = targetIndex;

            if (!string.Equals(cmbProtocol.Text, entry.ProtocolName, StringComparison.Ordinal))
            {
                _suppressProtocolChange = true;
                cmbProtocol.Text = entry.ProtocolName;
                _suppressProtocolChange = false;
                UpdateProtocolOpenButtonState();
            }

            _suppressTextChanged = true;
            txtProtocol.Text = entry.InputText;
            _suppressTextChanged = false;

            _suppressHistory = true;
            _frameBindingSource.DataSource = new BindingList<FrameSummaryRow>(entry.FrameRows);
            if (entry.Frames.Count > 0)
            {
                dgvFrames.ClearSelection();
                if (dgvFrames.Rows.Count > 0)
                {
                    dgvFrames.Rows[0].Selected = true;
                    dgvFrames.CurrentCell = dgvFrames.Rows[0].Cells[0];
                }

                ShowFrame(entry.Frames[0]);
            }
            else
            {
                _fieldBindingSource.DataSource = new BindingList<ProtocolFieldRow>();
                txtResult.Clear();
            }

            _suppressHistory = false;
            lblStatus.Text = $"已显示解析历史 {targetIndex + 1}/{_parseHistory.Count}。";
        }

        private static List<FrameSummaryRow> BuildFrameSummaryRows(List<ParsedFrame> frames, ProtocolDefinition protocolDefinition)
        {
            return frames.Select(frame => new FrameSummaryRow
            {
                Index = frame.Index,
                ProtocolType = DescribeProtocolType(frame.ProtocolType, protocolDefinition),
                Command = frame.CommandText,
                LengthStatus = frame.LengthStatus,
                ChecksumStatus = frame.ChecksumStatus,
                Status = frame.IsValid ? "正常" : "异常"
            }).ToList();
        }

        private static string NormalizeHexText(string input)
        {
            StringBuilder hex = new StringBuilder();
            foreach (char c in input)
            {
                if (Uri.IsHexDigit(c))
                {
                    hex.Append(char.ToUpperInvariant(c));
                }
            }

            return hex.ToString();
        }

        private static List<byte> ExtractHexBytes(string input)
        {
            StringBuilder hex = new StringBuilder();
            foreach (char c in input)
            {
                if (Uri.IsHexDigit(c))
                {
                    hex.Append(char.ToUpperInvariant(c));
                }
            }

            if (hex.Length == 0)
            {
                return new List<byte>();
            }

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("十六进制字符数量不是偶数，请检查是否漏写半个字节。");
            }

            List<byte> bytes = new List<byte>();
            for (int i = 0; i < hex.Length; i += 2)
            {
                bytes.Add(byte.Parse(hex.ToString(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
            }

            return bytes;
        }

        private ProtocolDefinition GetSelectedProtocolDefinition()
        {
            return string.Equals(cmbProtocol.Text, MeterHardwareProtocolName, StringComparison.Ordinal)
                ? ProtocolDefinition.MeterHardware
                : ProtocolDefinition.TerminalElectricTest;
        }

        private static List<ParsedFrame> ParseFrames(List<byte> bytes, ProtocolDefinition protocolDefinition)
        {
            List<ParsedFrame> frames = new List<ParsedFrame>();
            int index = 0;
            int minimumFrameLength = protocolDefinition.HasDataDirection ? 9 : 8;
            int minimumDeclaredLength = protocolDefinition.HasDataDirection ? 7 : 6;

            while (index < bytes.Count)
            {
                while (index < bytes.Count && bytes[index] != 0x55)
                {
                    index++;
                }

                if (index >= bytes.Count)
                {
                    break;
                }

                if (bytes.Count - index < minimumFrameLength)
                {
                    frames.Add(ParsedFrame.CreateInvalid(frames.Count + 1, bytes.Skip(index).ToArray(), $"报文长度不足，{protocolDefinition.Name}至少需要 {minimumFrameLength} 字节：{protocolDefinition.FrameFormatSummary}。"));
                    break;
                }

                int declaredLength = bytes[index + 1] + (bytes[index + 2] << 8);
                int totalLength = declaredLength + 2;
                if (declaredLength < minimumDeclaredLength)
                {
                    frames.Add(ParsedFrame.CreateInvalid(frames.Count + 1, bytes.Skip(index).Take(Math.Min(bytes.Count - index, minimumFrameLength)).ToArray(), $"数据长度字段异常：{declaredLength}，{protocolDefinition.Name}最小应为 {minimumDeclaredLength}。"));
                    index++;
                    continue;
                }

                if (index + totalLength > bytes.Count)
                {
                    frames.Add(ParsedFrame.CreateInvalid(frames.Count + 1, bytes.Skip(index).ToArray(), $"报文不完整：长度字段要求总长度 {totalLength} 字节，当前剩余 {bytes.Count - index} 字节。"));
                    break;
                }

                byte[] frameBytes = bytes.Skip(index).Take(totalLength).ToArray();
                frames.Add(ParseSingleFrame(frames.Count + 1, frameBytes, protocolDefinition));
                index += totalLength;
            }

            return frames;
        }

        private static ParsedFrame ParseSingleFrame(int frameIndex, byte[] frameBytes, ProtocolDefinition protocolDefinition)
        {
            int declaredLength = frameBytes[1] + (frameBytes[2] << 8);
            byte start = frameBytes[0];
            int cursor = 3;
            byte? dataDirection = null;
            if (protocolDefinition.HasDataDirection)
            {
                dataDirection = frameBytes[cursor++];
            }

            byte address = frameBytes[cursor++];
            byte protocolType = frameBytes[cursor++];
            byte command = frameBytes[cursor++];
            int dataLength = declaredLength - cursor;
            byte[] dataItems = dataLength > 0 ? frameBytes.Skip(cursor).Take(dataLength).ToArray() : Array.Empty<byte>();
            byte checksum = frameBytes[declaredLength];
            byte stop = frameBytes[declaredLength + 1];
            byte expectedChecksum = CalculateChecksum(frameBytes, 1, declaredLength - 1);
            bool lengthValid = declaredLength == frameBytes.Length - 2;
            bool checksumValid = checksum == expectedChecksum;
            bool startValid = start == 0x55;
            bool stopValid = stop == 0xAA;
            bool protocolValid = protocolType is 0x00 or 0x01;
            bool dataDirectionValid = !protocolDefinition.HasDataDirection || dataDirection is 0x00 or 0x01;
            string protocolDescription = DescribeProtocolType(protocolType, protocolDefinition);
            CommandAnalysis commandAnalysis = AnalyzeCommand(protocolType, command, dataItems, protocolDefinition);

            List<ProtocolFieldRow> fields = BuildTreeRows(
                frameIndex,
                frameBytes,
                declaredLength,
                protocolDefinition,
                dataDirection,
                address,
                protocolType,
                command,
                dataItems,
                checksum,
                expectedChecksum,
                stop,
                startValid,
                lengthValid,
                dataDirectionValid,
                protocolValid,
                checksumValid,
                stopValid,
                protocolDescription,
                commandAnalysis);

            bool isValid = startValid && stopValid && lengthValid && dataDirectionValid && checksumValid && protocolValid;
            string detailText = BuildDetailText(frameIndex, frameBytes, declaredLength, dataDirection, dataItems, expectedChecksum, isValid, fields, commandAnalysis, protocolDefinition);

            return new ParsedFrame
            {
                Index = frameIndex,
                ProtocolType = protocolType,
                CommandText = $"{ToHex(command)} {commandAnalysis.Title}",
                LengthStatus = lengthValid ? "正确" : "错误",
                ChecksumStatus = checksumValid ? "正确" : $"错误，应为 {ToHex(expectedChecksum)}",
                IsValid = isValid,
                Fields = fields,
                DetailText = detailText
            };
        }

        private static byte CalculateChecksum(byte[] frameBytes, int startIndex, int count)
        {
            int sum = 0;
            for (int i = startIndex; i < startIndex + count; i++)
            {
                sum += frameBytes[i];
            }

            return (byte)sum;
        }

        private static List<ProtocolFieldRow> BuildTreeRows(
            int frameIndex,
            byte[] frameBytes,
            int declaredLength,
            ProtocolDefinition protocolDefinition,
            byte? dataDirection,
            byte address,
            byte protocolType,
            byte command,
            byte[] dataItems,
            byte checksum,
            byte expectedChecksum,
            byte stop,
            bool startValid,
            bool lengthValid,
            bool dataDirectionValid,
            bool protocolValid,
            bool checksumValid,
            bool stopValid,
            string protocolDescription,
            CommandAnalysis commandAnalysis)
        {
            List<ProtocolFieldRow> rows = new List<ProtocolFieldRow>();
            rows.Add(new ProtocolFieldRow($"第{frameIndex}帧", ToHexString(frameBytes), $"{protocolDefinition.Name}；总长度 {frameBytes.Length} 字节，状态：{(startValid && lengthValid && dataDirectionValid && protocolValid && checksumValid && stopValid ? "正常" : "存在异常")}", "根"));
            rows.Add(new ProtocolFieldRow("├─ 起始字符55H(1BIN)", ToHex(frameBytes[0]), "固定起始符 0x55。", startValid ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("├─ 数据长度(2BIN)", $"{ToHex(frameBytes[1])} {ToHex(frameBytes[2])}", $"长度={declaredLength}，总长度=长度+2={declaredLength + 2}。", lengthValid ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 长度低字节", ToHex(frameBytes[1]), "小端低字节。", "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 长度高字节", ToHex(frameBytes[2]), "小端高字节。", "正常"));

            if (protocolDefinition.HasDataDirection)
            {
                rows.Add(new ProtocolFieldRow("├─ 数据方向(1BIN)", dataDirection.HasValue ? ToHex(dataDirection.Value) : "缺少", DescribeDataDirection(dataDirection), dataDirectionValid ? "正常" : "未知"));
            }

            rows.Add(new ProtocolFieldRow("├─ 地址/通道(1BIN)", ToHex(address), DescribeAddress(address, protocolDefinition), "正常"));

            rows.Add(new ProtocolFieldRow("├─ 协议类型(1BIN)", ToHex(protocolType), protocolDescription, protocolValid ? "正常" : "未知"));

            rows.Add(new ProtocolFieldRow("├─ 命令码(1BIN)", ToHex(command), commandAnalysis.Summary, "正常"));

            rows.Add(new ProtocolFieldRow("├─ 数据项", dataItems.Length == 0 ? "无" : ToHexString(dataItems), commandAnalysis.DataItemDescription, "正常"));
            AddDataItemTreeRows(rows, command, dataItems, protocolDefinition);

            rows.Add(new ProtocolFieldRow("├─ 校验和(1BIN)", ToHex(checksum), $"期望 {ToHex(expectedChecksum)}；校验范围：数据长度到校验和前一字节。", checksumValid ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("└─ 结束字符AAH(1BIN)", ToHex(stop), "固定结束符 0xAA。", stopValid ? "正常" : "异常"));

            return rows;
        }

        private static void AddDataItemTreeRows(List<ProtocolFieldRow> rows, byte command, byte[] dataItems, ProtocolDefinition protocolDefinition)
        {
            if (dataItems.Length == 0)
            {
                rows.Add(new ProtocolFieldRow("│  └─ 无数据项", "", "该报文数据项长度为0。", "正常"));
                return;
            }

            if (protocolDefinition.HasDataDirection)
            {
                AddMeterHardwareDataItemTreeRows(rows, command, dataItems);
                return;
            }

            if (command == 0x05)
            {
                AddPulseConfigTreeRows(rows, dataItems);
                return;
            }

            for (int i = 0; i < dataItems.Length; i++)
            {
                string prefix = i == dataItems.Length - 1 ? "│  └─" : "│  ├─";
                rows.Add(new ProtocolFieldRow($"{prefix} 字节{i + 1}", ToHex(dataItems[i]), GetDataByteDescription(command, i, dataItems), "正常"));
                IReadOnlyDictionary<int, string>? bitDescriptions = GetDataItemBitDescriptions(command, i, dataItems[i]);
                if (bitDescriptions != null)
                {
                    AddByteBitRows(rows, "│  │  ", dataItems[i], bitDescriptions);
                }
            }
        }

        private static void AddPulseConfigTreeRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            if (dataItems.Length >= 1)
            {
                rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 脉冲通道选择", ToHex(dataItems[0]), "D0-D3按位表示脉冲1-脉冲4。", "正常"));
                AddByteBitRows(rows, "│  │  ", dataItems[0], CreatePulseChannelBitDescriptions(dataItems[0]));
            }
            else
            {
                rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 脉冲通道选择", "缺少", "D0-D3按位表示脉冲1-脉冲4。", "异常"));
            }

            rows.Add(new ProtocolFieldRow(
                "│  ├─ 字节2: 脉冲启动标志",
                dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少",
                dataItems.Length >= 2 ? DescribePulseStartFlag(dataItems[1]) : "0x00停止脉冲发送，0x01启动脉冲发送。",
                dataItems.Length >= 2 ? "正常" : "异常"));

            rows.Add(new ProtocolFieldRow(
                "│  ├─ 字节3~6: 输出脉冲频率",
                dataItems.Length >= 6 ? ToHexString(dataItems.Skip(2).Take(4).ToArray()) : "缺少",
                dataItems.Length >= 6 ? $"float低字节在前，频率={ReadFloatLittleEndian(dataItems, 2):0.###} Hz。" : "float类型数据，低字节在前。",
                dataItems.Length >= 6 ? "正常" : "异常"));

            rows.Add(new ProtocolFieldRow(
                "│  ├─ 字节7~10: 输出脉冲个数",
                dataItems.Length >= 10 ? ToHexString(dataItems.Skip(6).Take(4).ToArray()) : "缺少",
                dataItems.Length >= 10 ? $"long低字节在前，输出脉冲个数={ReadUInt32LittleEndian(dataItems, 6)}。" : "long类型数据，低字节在前。",
                dataItems.Length >= 10 ? "正常" : "异常"));

            rows.Add(new ProtocolFieldRow(
                "│  └─ 字节11: 脉冲占空比",
                dataItems.Length >= 11 ? ToHex(dataItems[10]) : "缺少",
                dataItems.Length >= 11 ? $"无符号char，范围0-100，占空比={dataItems[10]}%。" : "无符号char类型，范围0-100。",
                dataItems.Length >= 11 ? "正常" : "异常"));
        }

        private static void AddMeterHardwareDataItemTreeRows(List<ProtocolFieldRow> rows, byte command, byte[] dataItems)
        {
            switch (command)
            {
                case 0xFB:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 反馈码", ToHex(dataItems[0]), DescribeMeterFeedbackCode(dataItems[0]), "正常"));
                    break;
                case 0x00:
                    AddRawMeterDataRows(rows, dataItems, "测试包数据项。");
                    break;
                case 0xFF:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 复位保留字节", ToHex(dataItems[0]), dataItems[0] == 0x00 ? "命令项+0x00，单片机回到开始上电状态。" : "复位命令按协议应为0x00。", dataItems[0] == 0x00 ? "正常" : "异常"));
                    break;
                case 0x01:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 交流电压控制", ToHex(dataItems[0]), DescribeMeterVoltageControl(dataItems[0]), IsMeterControlCodeValid(dataItems[0]) ? "正常" : "未知"));
                    break;
                case 0x02:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 交流电流控制", ToHex(dataItems[0]), DescribeMeterCurrentControl(dataItems[0]), IsMeterControlCodeValid(dataItems[0]) ? "正常" : "未知"));
                    break;
                case 0x20:
                    rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 试验类型", ToHex(dataItems[0]), DescribeMeterCircleTestType(dataItems[0]), "正常"));
                    rows.Add(new ProtocolFieldRow(
                        "│  └─ 字节2~3: 试验圈数",
                        dataItems.Length >= 3 ? ToHexString(dataItems.Skip(1).Take(2).ToArray()) : "缺少",
                        dataItems.Length >= 3 ? $"低字节在前，圈数={ReadUInt16LittleEndian(dataItems, 1)}，最大65535。" : "缺少2字节圈数数据。",
                        dataItems.Length >= 3 ? "正常" : "异常"));
                    break;
                case 0x21:
                    rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 误差类型", ToHex(dataItems[0]), DescribeMeterBasicErrorType(dataItems[0]), "正常"));
                    rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 试验动作", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? DescribeMeterBasicErrorAction(dataItems[1]) : "缺少动作数据。", dataItems.Length >= 2 ? "正常" : "异常"));
                    if (dataItems.Length >= 6 && dataItems[1] == 0xAA)
                    {
                        rows.Add(new ProtocolFieldRow(
                            "│  └─ 字节3~6: 误差结果",
                            ToHexString(dataItems.Skip(2).Take(4).ToArray()),
                            $"float低字节在前，误差结果={ReadFloatLittleEndian(dataItems, 2):0.######}。",
                            "正常"));
                    }
                    else
                    {
                        rows.Add(new ProtocolFieldRow("│  └─ 误差结果", dataItems.Length > 2 ? ToHexString(dataItems.Skip(2)) : "无", "启动命令上行无误差结果；结果获取上行应携带4字节float。", "说明"));
                    }
                    break;
                case 0x22:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1~2: 日计时试验圈数", ToHexString(dataItems.Take(2)), dataItems.Length >= 2 ? $"低字节在前，圈数={ReadUInt16LittleEndian(dataItems, 0)}，最大65535。" : "缺少2字节圈数。", dataItems.Length >= 2 ? "正常" : "异常"));
                    break;
                case 0x23:
                case 0x24:
                case 0x26:
                case 0x27:
                case 0x28:
                    AddMeterActionFloatRows(rows, dataItems, DescribeMeterExperimentAction, "结果值");
                    break;
                case 0x25:
                    AddMeterActionUIntRows(rows, dataItems, "实际收到的脉冲数");
                    break;
                case 0x29:
                case 0x30:
                case 0x31:
                case 0x32:
                case 0x38:
                case 0x40:
                case 0x41:
                case 0x42:
                case 0x43:
                case 0x44:
                case 0x45:
                case 0x46:
                case 0x47:
                    AddMeterBasicErrorV2Rows(rows, dataItems);
                    break;
                case 0x33:
                    AddMeterTimeSwitchRows(rows, dataItems);
                    break;
                case 0x34:
                    AddMeterDemandPeriodRows(rows, dataItems);
                    break;
                case 0x35:
                    AddMeterStartCreepRows(rows, dataItems);
                    break;
                case 0x36:
                    AddMeterDailyTimingV2Rows(rows, dataItems);
                    break;
                case 0x37:
                    AddMeterRegisterRunningRows(rows, dataItems);
                    break;
                case 0x80:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 电表类别", ToHex(dataItems[0]), DescribeMeterCategory(dataItems[0]), "正常"));
                    break;
                case 0x81:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 运行指示灯", ToHex(dataItems[0]), DescribeMeterRunLamp(dataItems[0]), "正常"));
                    break;
                case 0x82:
                    AddTwoByteModeRows(rows, dataItems, "直接式/互感式", DescribeMeterBoardSource, DescribeDirectTransformerMode);
                    break;
                case 0x83:
                    AddTwoByteModeRows(rows, dataItems, "零线电流切换", DescribeMeterBoardSource, DescribeNeutralCurrentMode);
                    break;
                case 0x84:
                    AddMeterPresenceRows(rows, dataItems);
                    break;
                case 0x85:
                    AddMeterPositionBitmapRows(rows, dataItems);
                    break;
                case 0x86:
                    AddMeterVoltageShortRows(rows, dataItems);
                    break;
                case 0x87:
                    AddMeterTripRows(rows, dataItems);
                    break;
                case 0x88:
                    AddMeterVoltagePowerRows(rows, dataItems);
                    break;
                case 0x89:
                    AddMeterPhaseFloatRows(rows, dataItems, "毫伏值", "mV");
                    break;
                case 0x8C:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 脉冲源", ToHex(dataItems[0]), DescribePulseSource(dataItems[0]), "正常"));
                    break;
                case 0x8D:
                    AddMeterRawSampleRows(rows, dataItems);
                    break;
                case 0x8E:
                    AddMeterCreepWithTimeRows(rows, dataItems);
                    break;
                case 0x8F:
                    AddMeterAlarmRows(rows, dataItems);
                    break;
                case 0x90:
                    AddMeterWiringCheckRows(rows, dataItems);
                    break;
                case 0xA0:
                case 0xA1:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1~4: 待测表脉冲常数", ToHexString(dataItems.Take(4)), dataItems.Length >= 4 ? $"uint32低字节在前，值={ReadUInt32LittleEndian(dataItems, 0)}。" : "缺少4字节常数。", dataItems.Length >= 4 ? "正常" : "异常"));
                    break;
                case 0xA2:
                case 0xA3:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1~8: 标准表脉冲常数", ToHexString(dataItems.Take(8)), dataItems.Length >= 8 ? $"uint64低字节在前，值={ReadUInt64LittleEndian(dataItems, 0)}。" : "缺少8字节常数。", dataItems.Length >= 8 ? "正常" : "异常"));
                    break;
                case 0xA4:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 时基源脉冲常数", ToHex(dataItems[0]), dataItems[0] == 0x01 ? "50K。" : dataItems[0] == 0x02 ? "500K。" : $"未知值{ToHex(dataItems[0])}。", "正常"));
                    break;
                case 0xA5:
                case 0xA6:
                    AddRawMeterDataRows(rows, dataItems, GetMeterHardwareCommandTitle(command));
                    break;
                case 0xC0:
                    AddTwoByteModeRows(rows, dataItems, "模组直流电压通断", DescribeModuleKind, DescribeOnOff2);
                    break;
                case 0xC1:
                    AddMeterLoadRows(rows, dataItems);
                    break;
                case 0xC2:
                    AddMeterActionFloatRows(rows, dataItems, DescribeStartGetStopAction, "复位信号电平持续时间(s)");
                    break;
                case 0xC3:
                    AddMeterModulePowerRows(rows, dataItems);
                    break;
                case 0xC9:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1: 表位电机", ToHex(dataItems[0]), DescribeMotorPress(dataItems[0]), "正常"));
                    break;
                case 0xCA:
                    AddFanTemperatureRows(rows, dataItems);
                    break;
                case 0xD0:
                    AddMeterChipCalibrationRows(rows, dataItems);
                    break;
                case 0xD5:
                case 0xD6:
                case 0xD7:
                case 0xD8:
                    AddMeterRangeRows(rows, command, dataItems);
                    break;
                case 0xF0:
                    rows.Add(new ProtocolFieldRow("│  └─ 字节1~4: IP地址", ToHexString(dataItems.Take(4)), dataItems.Length >= 4 ? $"{dataItems[0]}.{dataItems[1]}.{dataItems[2]}.{dataItems[3]}" : "缺少4字节IP。", dataItems.Length >= 4 ? "正常" : "异常"));
                    break;
                case 0xF1:
                    AddPortPropertyRows(rows, dataItems);
                    break;
                case 0xF2:
                    AddVersionRows(rows, dataItems);
                    break;
                case 0xF3:
                    AddSerialServerPortRows(rows, dataItems);
                    break;
                default:
                    AddRawMeterDataRows(rows, dataItems, "未登记电表硬件命令数据项，按原始字节展示。");
                    break;
            }
        }

        private static void AddRawMeterDataRows(List<ProtocolFieldRow> rows, byte[] dataItems, string description)
        {
            for (int i = 0; i < dataItems.Length; i++)
            {
                string prefix = i == dataItems.Length - 1 ? "│  └─" : "│  ├─";
                rows.Add(new ProtocolFieldRow($"{prefix} 字节{i + 1}", ToHex(dataItems[i]), description, "正常"));
            }
        }

        private static void AddMeterActionFloatRows(List<ProtocolFieldRow> rows, byte[] dataItems, Func<byte, string> actionDescriptor, string resultLabel)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), actionDescriptor(dataItems[0]), "正常"));
            if (dataItems.Length >= 5)
            {
                rows.Add(new ProtocolFieldRow("│  └─ 字节2~5: " + resultLabel, ToHexString(dataItems.Skip(1).Take(4)), $"float低字节在前，值={ReadFloatLittleEndian(dataItems, 1):0.######}。", "正常"));
            }
            else
            {
                rows.Add(new ProtocolFieldRow("│  └─ 结果", dataItems.Length > 1 ? ToHexString(dataItems.Skip(1)) : "无", "结果获取上行通常携带4字节float。", "说明"));
            }
        }

        private static void AddMeterActionUIntRows(List<ProtocolFieldRow> rows, byte[] dataItems, string resultLabel)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeMeterExperimentAction(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2~5: " + resultLabel, dataItems.Length >= 5 ? ToHexString(dataItems.Skip(1).Take(4)) : "缺少", dataItems.Length >= 5 ? $"uint32低字节在前，值={ReadUInt32LittleEndian(dataItems, 1)}。" : "结果获取上行应携带4字节uint。", dataItems.Length >= 5 ? "正常" : "说明"));
        }

        private static void AddMeterBasicErrorV2Rows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeStartGetStopAction(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 脉冲数", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? $"待测表试验圈数={dataItems[1]}，范围1~99。" : "缺少脉冲数。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节3: 试验次数", dataItems.Length >= 3 ? ToHex(dataItems[2]) : "缺少", dataItems.Length >= 3 ? $"试验次数={dataItems[2]}，范围1~10。" : "缺少试验次数。", dataItems.Length >= 3 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节4: 脉冲类型", dataItems.Length >= 4 ? ToHex(dataItems[3]) : "无", dataItems.Length >= 4 ? DescribeActiveReactiveType(dataItems[3]) : "结果获取时可不带脉冲类型。", "说明"));
            AddFloatResultRows(rows, dataItems, 3, "误差结果");
        }

        private static void AddFloatResultRows(List<ProtocolFieldRow> rows, byte[] dataItems, int startIndex, string label)
        {
            if (dataItems.Length <= startIndex)
            {
                rows.Add(new ProtocolFieldRow("│  └─ " + label, "无", "当前报文未携带结果。", "说明"));
                return;
            }

            int resultIndex = 1;
            for (int i = startIndex; i + 3 < dataItems.Length; i += 4)
            {
                string prefix = i + 4 >= dataItems.Length ? "│  └─" : "│  ├─";
                float value = ReadFloatLittleEndian(dataItems, i);
                rows.Add(new ProtocolFieldRow($"{prefix} {label}{resultIndex}", ToHexString(dataItems.Skip(i).Take(4)), $"float低字节在前，值={value:0.######}。{DescribeSpecialMeterResult(value)}", "正常"));
                resultIndex++;
            }
        }

        private static void AddMeterTimeSwitchRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), dataItems[0] == 0x00 ? "开始进行时段投切试验。" : dataItems[0] == 0xAA ? "结果获取。" : $"未知动作{ToHex(dataItems[0])}。", "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 脉冲数P", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? $"脉冲数={dataItems[1]}。" : "结果包应包含脉冲数。", dataItems.Length >= 2 ? "正常" : "说明"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节3~6: 时间T", dataItems.Length >= 6 ? ToHexString(dataItems.Skip(2).Take(4)) : "缺少", dataItems.Length >= 6 ? $"uint32低字节在前，时间={ReadUInt32LittleEndian(dataItems, 2)}s。" : "结果包应包含4字节时间。", dataItems.Length >= 6 ? "正常" : "说明"));
        }

        private static void AddMeterDemandPeriodRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeStartGetStopAction(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 需量周期", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? $"{dataItems[1]} min。" : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节3: 滑差时间", dataItems.Length >= 3 ? ToHex(dataItems[2]) : "缺少", dataItems.Length >= 3 ? $"{dataItems[2]} min。" : "缺少。", dataItems.Length >= 3 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节4: 测试滑差数", dataItems.Length >= 4 ? ToHex(dataItems[3]) : "缺少", dataItems.Length >= 4 ? $"{dataItems[3]}个滑差计算一次误差。" : "缺少。", dataItems.Length >= 4 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节5: 测试次数", dataItems.Length >= 5 ? ToHex(dataItems[4]) : "缺少", dataItems.Length >= 5 ? $"N={dataItems[4]}。" : "缺少。", dataItems.Length >= 5 ? "正常" : "异常"));
            AddFloatResultRows(rows, dataItems, 5, "需量误差结果");
        }

        private static void AddMeterStartCreepRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), dataItems[0] == 0x00 ? "开始试验。" : dataItems[0] == 0xAA ? "结果获取。" : $"未知动作{ToHex(dataItems[0])}。", "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 脉冲数", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? $"脉冲数={dataItems[1]}。" : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节3~6: 时间", dataItems.Length >= 6 ? ToHexString(dataItems.Skip(2).Take(4)) : "缺少", dataItems.Length >= 6 ? $"uint32低字节在前，时间={ReadUInt32LittleEndian(dataItems, 2)}s。" : "缺少4字节时间。", dataItems.Length >= 6 ? "正常" : "异常"));
        }

        private static void AddMeterDailyTimingV2Rows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeStartGetStopAction(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 时间", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? $"{dataItems[1]}s，范围1~99。" : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节3: 次数", dataItems.Length >= 3 ? ToHex(dataItems[2]) : "缺少", dataItems.Length >= 3 ? $"{dataItems[2]}次，范围1~10。" : "缺少。", dataItems.Length >= 3 ? "正常" : "异常"));
            AddFloatResultRows(rows, dataItems, 3, "日计时误差");
        }

        private static void AddMeterRegisterRunningRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeStartGetStopAction(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2~5: 被测表脉冲数", dataItems.Length >= 5 ? ToHexString(dataItems.Skip(1).Take(4)) : "缺少", dataItems.Length >= 5 ? $"uint32低字节在前，脉冲数={ReadUInt32LittleEndian(dataItems, 1)}。" : "结果获取上行应包含4字节脉冲数。", dataItems.Length >= 5 ? "正常" : "说明"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节6~9: 标准表电能量", dataItems.Length >= 9 ? ToHexString(dataItems.Skip(5).Take(4)) : "缺少", dataItems.Length >= 9 ? $"float低字节在前，电能量={ReadFloatLittleEndian(dataItems, 5):0.######} kWh。" : "结果获取上行应包含4字节电能量。", dataItems.Length >= 9 ? "正常" : "说明"));
        }

        private static void AddTwoByteModeRows(List<ProtocolFieldRow> rows, byte[] dataItems, string title, Func<byte, string> first, Func<byte, string> second)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: " + title + "来源/功能", ToHex(dataItems[0]), first(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2: " + title + "状态", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? second(dataItems[1]) : "缺少状态字节。", dataItems.Length >= 2 ? "正常" : "异常"));
        }

        private static void AddMeterPresenceRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), dataItems[0] == 0x01 ? "PC下发表位有无电表检测。" : dataItems[0] == 0xAA ? "PC下发结果获取。" : $"未知动作{ToHex(dataItems[0])}。", "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2: 检表结果", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "无", dataItems.Length >= 2 ? DescribeMeterPresence(dataItems[1]) : "开始检测回复通常不带结果。", "说明"));
        }

        private static void AddMeterPositionBitmapRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            for (int i = 0; i < Math.Min(dataItems.Length, 6); i++)
            {
                int start = i * 8 + 1;
                int end = start + 7;
                string prefix = i == Math.Min(dataItems.Length, 6) - 1 ? "│  └─" : "│  ├─";
                rows.Add(new ProtocolFieldRow($"{prefix} 字节{i + 1}: 表位{start}~{end}", ToHex(dataItems[i]), DescribeMeterPositionByte(dataItems[i], start), "正常"));
            }
        }

        private static void AddMeterVoltageShortRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), dataItems[0] == 0x01 ? "开始检测。" : dataItems[0] == 0xAA ? "结果获取。" : $"未知动作{ToHex(dataItems[0])}。", "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2: 短路结果", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "无", dataItems.Length >= 2 ? DescribeVoltageShortResult(dataItems[1]) : "开始检测回复通常不带结果。", "说明"));
        }

        private static void AddMeterTripRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 开关类型", ToHex(dataItems[0]), DescribeTripSwitchType(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 目标状态", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? DescribeTripState(dataItems[1]) : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节3: 检测结果", dataItems.Length >= 3 ? ToHex(dataItems[2]) : "无", dataItems.Length >= 3 ? DescribeSuccessFail(dataItems[2]) : "下行通常不带结果。", "说明"));
        }

        private static void AddMeterVoltagePowerRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeMeterExperimentAction(dataItems[0]), "正常"));
            string[] phases = { "A相", "B相", "C相" };
            for (int phase = 0; phase < 3; phase++)
            {
                int offset = 1 + phase * 12;
                if (dataItems.Length >= offset + 12)
                {
                    rows.Add(new ProtocolFieldRow($"│  ├─ {phases[phase]}视在功率", ToHexString(dataItems.Skip(offset).Take(4)), $"{ReadFloatLittleEndian(dataItems, offset):0.######}。", "正常"));
                    rows.Add(new ProtocolFieldRow($"│  ├─ {phases[phase]}有功功率", ToHexString(dataItems.Skip(offset + 4).Take(4)), $"{ReadFloatLittleEndian(dataItems, offset + 4):0.######}。", "正常"));
                    rows.Add(new ProtocolFieldRow($"│  ├─ {phases[phase]}功率因素", ToHexString(dataItems.Skip(offset + 8).Take(4)), $"{ReadFloatLittleEndian(dataItems, offset + 8):0.######}。", "正常"));
                }
            }
        }

        private static void AddMeterPhaseFloatRows(List<ProtocolFieldRow> rows, byte[] dataItems, string label, string unit)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeMeterExperimentAction(dataItems[0]), "正常"));
            string[] phases = { "A相", "B相", "C相" };
            for (int i = 0; i < 3; i++)
            {
                int offset = 1 + i * 4;
                if (dataItems.Length >= offset + 4)
                {
                    rows.Add(new ProtocolFieldRow($"│  ├─ {phases[i]}{label}", ToHexString(dataItems.Skip(offset).Take(4)), $"{ReadFloatLittleEndian(dataItems, offset):0.######} {unit}。", "正常"));
                }
            }
        }

        private static void AddMeterRawSampleRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1~4: SPS采样率", dataItems.Length >= 4 ? ToHexString(dataItems.Take(4)) : "缺少", dataItems.Length >= 4 ? $"{ReadUInt32LittleEndian(dataItems, 0)} 次/秒。" : "缺少4字节SPS。", dataItems.Length >= 4 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节5~8: ST总采样时间", dataItems.Length >= 8 ? ToHexString(dataItems.Skip(4).Take(4)) : "缺少", dataItems.Length >= 8 ? $"{ReadUInt32LittleEndian(dataItems, 4)} s。" : "缺少4字节ST。", dataItems.Length >= 8 ? "正常" : "异常"));
        }

        private static void AddMeterCreepWithTimeRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeMeterExperimentAction(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2~5: 试验持续时间", dataItems.Length >= 5 ? ToHexString(dataItems.Skip(1).Take(4)) : "缺少", dataItems.Length >= 5 ? $"{ReadFloatLittleEndian(dataItems, 1):0.######} h。" : "启动下行应包含4字节float小时。", "说明"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2~5: 实际收到脉冲数", dataItems.Length >= 5 ? ToHexString(dataItems.Skip(1).Take(4)) : "缺少", dataItems.Length >= 5 ? $"{ReadUInt32LittleEndian(dataItems, 1)}。" : "结果获取上行应包含4字节uint。", "说明"));
        }

        private static void AddMeterAlarmRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), dataItems[0] == 0x00 ? "开始检测。" : dataItems[0] == 0xAA ? "结果获取。" : $"未知动作{ToHex(dataItems[0])}。", "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2: 报警结果", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "无", dataItems.Length >= 2 ? (dataItems[1] == 0x00 ? "检测到报警信号。" : dataItems[1] == 0x01 ? "没有检测到报警信号。" : $"未知结果{ToHex(dataItems[1])}。") : "开始检测回复通常不带结果。", "说明"));
        }

        private static void AddMeterWiringCheckRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeStartGetStopAction(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 测试类型", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? DescribeWiringCheckType(dataItems[1]) : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节3~6: 基本误差", dataItems.Length >= 6 ? ToHexString(dataItems.Skip(2).Take(4)) : "无", dataItems.Length >= 6 ? $"{ReadFloatLittleEndian(dataItems, 2):0.######}。" : "按测试类型决定是否携带。", "说明"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节7~10: 日计时误差", dataItems.Length >= 10 ? ToHexString(dataItems.Skip(6).Take(4)) : "无", dataItems.Length >= 10 ? $"{ReadFloatLittleEndian(dataItems, 6):0.######} s/d。" : "按测试类型决定是否携带。", "说明"));
        }

        private static void AddMeterLoadRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 测试类型", ToHex(dataItems[0]), dataItems[0] == 0x01 ? "秒平均电流。" : dataItems[0] == 0x02 ? "峰值电流。" : dataItems[0] == 0xFF ? "测试完成。" : $"未知类型{ToHex(dataItems[0])}。", "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 动作", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? DescribeMeterExperimentAction(dataItems[1]) : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节3~6: 电压值", dataItems.Length >= 6 ? ToHexString(dataItems.Skip(2).Take(4)) : "无", dataItems.Length >= 6 ? $"{ReadFloatLittleEndian(dataItems, 2):0.######} V。" : "结果获取上行携带float电压值。", "说明"));
        }

        private static void AddMeterModulePowerRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 动作", ToHex(dataItems[0]), DescribeStartGetStopAction(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2~5: 功率", dataItems.Length >= 5 ? ToHexString(dataItems.Skip(1).Take(4)) : "无", dataItems.Length >= 5 ? $"{ReadFloatLittleEndian(dataItems, 1):0.######} W。" : "结果获取上行携带。", "说明"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节6~9: 电压", dataItems.Length >= 9 ? ToHexString(dataItems.Skip(5).Take(4)) : "无", dataItems.Length >= 9 ? $"{ReadFloatLittleEndian(dataItems, 5):0.######} V。" : "结果获取上行携带。", "说明"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节10~13: 电流", dataItems.Length >= 13 ? ToHexString(dataItems.Skip(9).Take(4)) : "无", dataItems.Length >= 13 ? $"{ReadFloatLittleEndian(dataItems, 9):0.######} A。" : "结果获取上行携带。", "说明"));
        }

        private static void AddFanTemperatureRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 传感器序号", ToHex(dataItems[0]), $"序号={dataItems[0]}，从1开始。", "正常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节2: 功能", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? (dataItems[1] == 0xAA ? "获取温度值。" : dataItems[1] == 0x01 ? "校准。" : dataItems[1] == 0xFF ? "删除校准值。" : $"未知功能{ToHex(dataItems[1])}。") : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节3~6: 温度值", dataItems.Length >= 6 ? ToHexString(dataItems.Skip(2).Take(4)) : "无", dataItems.Length >= 6 ? $"int32低字节在前，值={ReadInt32LittleEndian(dataItems, 2)}。" : "获取/校准时携带。", "说明"));
        }

        private static void AddMeterChipCalibrationRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 校准对象", ToHex(dataItems[0]), DescribeCalibrationTarget(dataItems[0]), "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2~5: 校准值", dataItems.Length >= 5 ? ToHexString(dataItems.Skip(1).Take(4)) : "缺少", dataItems.Length >= 5 ? $"float低字节在前，值={ReadFloatLittleEndian(dataItems, 1):0.######}。" : "缺少4字节float校准值。", dataItems.Length >= 5 ? "正常" : "异常"));
        }

        private static void AddMeterRangeRows(List<ProtocolFieldRow> rows, byte command, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 功能", ToHex(dataItems[0]), dataItems[0] == 0x01 ? "输出挡位设置/负载切换。" : dataItems[0] == 0xAA ? "挡位获取。" : $"未知功能{ToHex(dataItems[0])}。", "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2: 状态", dataItems.Length >= 2 ? ToHex(dataItems[1]) : "缺少", dataItems.Length >= 2 ? DescribeRangeState(command, dataItems[1]) : "缺少挡位状态。", dataItems.Length >= 2 ? "正常" : "异常"));
        }

        private static void AddPortPropertyRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1~2: 端口号", dataItems.Length >= 2 ? ToHexString(dataItems.Take(2)) : "缺少", dataItems.Length >= 2 ? $"{ReadUInt16LittleEndian(dataItems, 0)}。" : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节3~6: 波特率", dataItems.Length >= 6 ? ToHexString(dataItems.Skip(2).Take(4)) : "缺少", dataItems.Length >= 6 ? $"{ReadUInt32LittleEndian(dataItems, 2)}。" : "缺少。", dataItems.Length >= 6 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  ├─ 字节7: 奇偶校验", dataItems.Length >= 7 ? ToHex(dataItems[6]) : "缺少", dataItems.Length >= 7 ? DescribeParity(dataItems[6]) : "缺少。", dataItems.Length >= 7 ? "正常" : "异常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节8: 保存标志", dataItems.Length >= 8 ? ToHex(dataItems[7]) : "缺少", dataItems.Length >= 8 ? (dataItems[7] == 0x00 ? "断电不保存。" : dataItems[7] == 0x01 ? "断电保存。" : $"未知{ToHex(dataItems[7])}。") : "缺少。", dataItems.Length >= 8 ? "正常" : "异常"));
        }

        private static void AddVersionRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            int length = dataItems[0];
            string version = dataItems.Length > 1 ? Encoding.ASCII.GetString(dataItems.Skip(1).Take(Math.Min(length, dataItems.Length - 1)).ToArray()) : string.Empty;
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1: 字符串长度", ToHex(dataItems[0]), $"长度={length}。", "正常"));
            rows.Add(new ProtocolFieldRow("│  └─ 字节2~N: 版本字符串", dataItems.Length > 1 ? ToHexString(dataItems.Skip(1)) : "无", string.IsNullOrEmpty(version) ? "下行为0x00或未携带版本字符串。" : version, "正常"));
        }

        private static void AddSerialServerPortRows(List<ProtocolFieldRow> rows, byte[] dataItems)
        {
            rows.Add(new ProtocolFieldRow("│  ├─ 字节1~2: 端口信息长度", dataItems.Length >= 2 ? ToHexString(dataItems.Take(2)) : "缺少", dataItems.Length >= 2 ? $"{ReadUInt16LittleEndian(dataItems, 0)}字节。" : "缺少。", dataItems.Length >= 2 ? "正常" : "异常"));
            int index = 2;
            int item = 1;
            while (index + 7 < dataItems.Length)
            {
                byte no = dataItems[index];
                ushort port = ReadUInt16LittleEndian(dataItems, index + 1);
                uint baud = ReadUInt32LittleEndian(dataItems, index + 3);
                byte parity = dataItems[index + 7];
                rows.Add(new ProtocolFieldRow($"│  ├─ 端口信息{item}", ToHexString(dataItems.Skip(index).Take(8)), $"序号={no}，端口={port}，波特率={baud}，校验={DescribeParity(parity)}", "正常"));
                index += 8;
                item++;
            }
        }

        private static void AddByteBitRows(List<ProtocolFieldRow> rows, string prefix, byte value, IReadOnlyDictionary<int, string> bitDescriptions)
        {
            for (int bit = 7; bit >= 0; bit--)
            {
                int bitValue = (value >> bit) & 0x01;
                string description = bitDescriptions.TryGetValue(bit, out string? text) ? text : "未定义/保留";
                rows.Add(new ProtocolFieldRow($"{prefix}{(bit == 0 ? "└" : "├")}─ bit{bit}", bitValue.ToString(CultureInfo.InvariantCulture), description, "bit"));
            }
        }

        private static IReadOnlyDictionary<int, string>? GetDataItemBitDescriptions(byte command, int byteIndex, byte value)
        {
            return command switch
            {
                0x03 when byteIndex == 0 => Enumerable.Range(0, 8).ToDictionary(bit => bit, bit => $"遥信{bit + 1}：{(((value >> bit) & 1) == 1 ? "闭合" : "断开")}"),
                0x03 when byteIndex == 1 => Enumerable.Range(0, 8).ToDictionary(bit => bit, bit => bit < 4 ? $"遥信{bit + 9}：{(((value >> bit) & 1) == 1 ? "闭合" : "断开")}" : $"门节点{bit - 3}：{(((value >> bit) & 1) == 1 ? "闭合" : "断开")}"),
                0x04 or 0x09 or 0x15 or 0xC4 => Enumerable.Range(0, 8).ToDictionary(bit => bit, bit => $"状态位D{bit}：{(((value >> bit) & 1) == 1 ? "闭合/有变化/脉冲" : "断开/无变化/电平")}"),
                0x05 when byteIndex == 0 => Enumerable.Range(0, 8).ToDictionary(bit => bit, bit => bit < 4 ? $"脉冲{bit + 1}：{(((value >> bit) & 1) == 1 ? "选中" : "未选中")}" : "保留"),
                0x14 when byteIndex == 0 => Enumerable.Range(0, 8).ToDictionary(bit => bit, bit => bit < 5 ? $"遥信/脉冲端口{bit + 1}：{(((value >> bit) & 1) == 1 ? "输出干扰脉冲" : "不输出")}" : "保留"),
                0x21 => new Dictionary<int, string> { [0] = "UA上电状态", [1] = "UB上电状态", [2] = "UC上电状态", [3] = "保留", [4] = "保留", [5] = "保留", [6] = "保留", [7] = "保留" },
                0x22 => new Dictionary<int, string> { [0] = "IA/三相电流接入状态", [1] = "IB接入状态", [2] = "IC接入状态", [3] = "保留", [4] = "保留", [5] = "保留", [6] = "保留", [7] = "保留" },
                0x2A or 0x30 => new Dictionary<int, string> { [0] = "红色", [1] = "绿色", [2] = "黄色", [3] = "LED1", [4] = "LED2", [5] = "LED3", [6] = "LED4", [7] = "保留" },
                0x33 => new Dictionary<int, string> { [0] = "A相带电状态", [1] = "B相带电状态", [2] = "C相带电状态", [3] = "保留", [4] = "保留", [5] = "保留", [6] = "保留", [7] = "保留" },
                0x3A or 0x85 => new Dictionary<int, string> { [0] = "STA1选择", [1] = "STA2选择", [2] = "保留", [3] = "保留", [4] = "保留", [5] = "保留", [6] = "保留", [7] = "保留" },
                0x3B or 0x86 or 0x3C or 0x87 => new Dictionary<int, string> { [0] = "RST", [1] = "SET", [2] = "EVENT", [3] = "保留", [4] = "保留", [5] = "保留", [6] = "保留", [7] = "保留" },
                _ => null
            };
        }

        private static IReadOnlyDictionary<int, string> CreatePulseChannelBitDescriptions(byte value)
        {
            return Enumerable.Range(0, 8).ToDictionary(
                bit => bit,
                bit => bit < 4 ? $"D{bit}: 脉冲{bit + 1}，{(((value >> bit) & 1) == 1 ? "选中" : "未选中")}" : $"D{bit}: 保留");
        }

        private static string DescribePulseStartFlag(byte value)
        {
            return value switch
            {
                0x00 => "停止脉冲发送。",
                0x01 => "启动脉冲发送。",
                _ => $"未知启动标志 {ToHex(value)}，协议定义0x00停止、0x01启动。"
            };
        }

        private static string GetDataByteDescription(byte command, int byteIndex, byte[] dataItems)
        {
            return command switch
            {
                0x03 when byteIndex == 0 => "D0-D7分别表示1-8路遥信状态，1闭合，0断开。",
                0x03 when byteIndex == 1 => "D0-D7分别表示9-12路遥信状态及门节点状态。",
                0x05 when byteIndex == 0 => "脉冲通道选择，D0-D3表示脉冲1-4。",
                0x05 when byteIndex == 1 => "脉冲启动标志：00停止，01启动。",
                0x05 when byteIndex >= 2 && byteIndex <= 5 => "输出脉冲频率float数据，低字节在前。",
                0x05 when byteIndex >= 6 && byteIndex <= 9 => "输出脉冲个数long数据，低字节在前。",
                0x05 when byteIndex == 10 => "脉冲占空比，0-100。",
                0x14 when byteIndex == 0 => "D0-D4分别表示遥信/脉冲端口。",
                0x14 when byteIndex is 1 or 2 => "干扰脉冲脉宽时间，2字节无符号整型低字节在前，单位ms。",
                0x21 => "D0-D2分别表示UA/UB/UC上电状态。",
                0x22 => "电流接入状态位。",
                0x2A or 0x30 => "BIT0-BIT2表示颜色，BIT3-BIT6表示LED灯位。",
                0x33 => "bit0-A相，bit1-B相，bit2-C相带电状态。",
                0x3A or 0x85 => "STA1/STA2选择位。",
                0x3B or 0x86 or 0x3C or 0x87 => "RST/SET/EVENT引脚状态位。",
                _ => $"数据项第{byteIndex + 1}字节。"
            };
        }

        private static string BuildDetailText(
            int frameIndex,
            byte[] frameBytes,
            int declaredLength,
            byte? dataDirection,
            byte[] dataItems,
            byte expectedChecksum,
            bool isValid,
            List<ProtocolFieldRow> fields,
            CommandAnalysis commandAnalysis,
            ProtocolDefinition protocolDefinition)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"第 {frameIndex} 帧解析结果：{(isValid ? "正常" : "存在异常")}");
            builder.AppendLine($"协议选择：{protocolDefinition.Name}");
            builder.AppendLine($"完整报文：{ToHexString(frameBytes)}");
            builder.AppendLine($"长度说明：长度字段值 {declaredLength}，实际总帧长 {frameBytes.Length} 字节。");
            builder.AppendLine($"校验说明：实际 {ToHex(frameBytes[declaredLength])}，计算 {ToHex(expectedChecksum)}。");
            if (protocolDefinition.HasDataDirection)
            {
                builder.AppendLine($"数据方向：{DescribeDataDirection(dataDirection)}");
            }
            builder.AppendLine($"协议差异：{protocolDefinition.DifferenceSummary}");
            builder.AppendLine($"命令说明：{commandAnalysis.Summary}");
            builder.AppendLine($"数据项：{commandAnalysis.DataItemDescription}");
            builder.AppendLine();
            builder.AppendLine("数据项详细解析：");
            builder.AppendLine(commandAnalysis.DataItemDetail);
            builder.AppendLine();
            builder.AppendLine("协议文档匹配：");
            builder.AppendLine(commandAnalysis.ProtocolDocumentText);
            builder.AppendLine();
            builder.AppendLine("字段明细：");

            foreach (ProtocolFieldRow field in fields)
            {
                builder.AppendLine($"{field.Name}：{field.Value}，{field.Description} [{field.Status}]");
            }

            return builder.ToString();
        }

        private static string DescribeProtocolType(byte protocolType, ProtocolDefinition protocolDefinition)
        {
            string prefix = protocolDefinition.HasDataDirection ? "电表硬件" : "终端电测";
            return protocolType switch
            {
                0x00 => $"{prefix}控制协议，用于电源、遥信、遥控、状态读取、电压电流检测等控制类操作。",
                0x01 => $"{prefix}透传协议，用于透传 645、698、1376.2、1376.3 等业务报文。",
                _ => $"未知协议类型 0x{protocolType:X2}。"
            };
        }

        private static string DescribeDataDirection(byte? dataDirection)
        {
            return dataDirection switch
            {
                0x00 => "0x00：PC下发到MCU。",
                0x01 => "0x01：MCU上传到PC。",
                null => "该协议无数据方向字段。",
                _ => $"未知数据方向 {ToHex(dataDirection.Value)}，协议仅定义0x00/0x01，其他预留。"
            };
        }

        private static string DescribeAddress(byte address, ProtocolDefinition protocolDefinition)
        {
            if (protocolDefinition.HasDataDirection)
            {
                return address == 0xAA
                    ? "0xAA：所有工位广播地址，所有工位收到命令进行响应。"
                    : $"待测工位地址/通道，十进制={address}，从1开始，对应拨码开关二进制位号。";
            }

            return address == 0xFF ? "广播地址 255。" : $"地址/通道十进制={address}。";
        }

        private static CommandAnalysis AnalyzeCommand(byte protocolType, byte command, byte[] dataItems, ProtocolDefinition protocolDefinition)
        {
            if (protocolType == 0x01)
            {
                return new CommandAnalysis(
                    "透传协议",
                    "透传协议中命令码无效，后续数据项通常承载被透传业务报文。",
                    dataItems.Length == 0 ? "无透传数据。" : $"透传数据长度 {dataItems.Length} 字节：{ToHexString(dataItems)}。",
                    dataItems.Length == 0 ? "无透传数据。" : ToHexString(dataItems),
                    protocolDefinition.HasDataDirection
                        ? "电表检测装置硬件透传协议：用于透传模块和电表/虚拟表之间的通信报文或抄表报文，例如645、698、1376.2、1376.3等。"
                        : "终端检测装置电测透传协议：主要用于透传模块和终端、模块与虚拟表之间的通信报文或者抄表报文等，例如645、698、376.2、376.3等报文。");
            }

            if (protocolDefinition.HasDataDirection)
            {
                MeterCommandAnalysis meterCommandAnalysis = AnalyzeMeterHardwareCommand(command, dataItems);
                string meterProtocolText = GetProtocolCommandDocumentText(protocolDefinition, command);

                return new CommandAnalysis(
                    meterCommandAnalysis.Title,
                    meterCommandAnalysis.Summary,
                    meterCommandAnalysis.DataSummary,
                    meterCommandAnalysis.DataDetail,
                    meterProtocolText);
            }

            List<ProtocolCommandInfo> matchedSections = GetProtocolCommandSections(protocolDefinition, command);

            string title = matchedSections.Count > 0
                ? string.Join(" / ", matchedSections.Select(section => section.Title).Distinct())
                : $"未登记命令码 0x{command:X2}";
            string protocolText = matchedSections.Count > 0
                ? string.Join($"{Environment.NewLine}{Environment.NewLine}", matchedSections.Select(section => section.Text))
                : $"协议文档中未找到命令码 0x{command:X2} 的章节，请确认协议版本。";
            string dataDetail = DescribeDataItemDetail(command, dataItems);
            string dataSummary = dataItems.Length == 0
                ? "无数据项。"
                : $"数据项长度 {dataItems.Length} 字节，原始值：{ToHexString(dataItems)}。";

            return new CommandAnalysis(
                title,
                $"{title}。{(matchedSections.Count > 0 ? "已匹配协议文档章节。" : "协议文档未匹配到章节。")}",
                dataSummary,
                dataDetail,
                protocolText);
        }

        private static string DescribeDataItemDetail(byte command, byte[] dataItems)
        {
            if (dataItems.Length == 0)
            {
                return "无数据项。";
            }

            return command switch
            {
                0x03 => DescribeBitStates(dataItems, "遥信", 1, 12, "闭合", "断开"),
                0x04 or 0x09 or 0x15 => DescribeControlState(dataItems),
                0x05 => DescribePulseConfig(dataItems),
                0x06 or 0x07 or 0x2B => DescribeSingleSwitchState(dataItems, "状态", "接入/跳闸/靠近", "未接入/未跳闸/离开"),
                0x08 => DescribeMeterInfo(dataItems, false),
                0x0A => DescribeMeterInfo(dataItems, true),
                0x0B => DescribeModulePowerTest(dataItems),
                0x0C => DescribeTemperatureHumidity(dataItems),
                0x10 or 0xC1 => "该命令按协议无数据项；当前报文带有数据项，请检查是否为应答扩展数据：" + ToHexString(dataItems),
                0x11 => DescribeSingleSwitchState(dataItems, "永磁体", "吸合/应用", "释放"),
                0x14 => DescribeInterferencePulse(dataItems),
                0x16 => DescribeControlChangeTime(dataItems),
                0x19 => DescribeSingleSwitchState(dataItems, "台区变压器", "切入", "切开"),
                0x21 => DescribePhaseBitmap(dataItems[0], "电压", "UA", "UB", "UC"),
                0x22 => DescribePhaseBitmap(dataItems[0], "电流", "IA", "IB", "IC"),
                0x24 => DescribeSingleSwitchState(dataItems, "IN继电器", "断开", "旁路"),
                0x26 => DescribeSingleSwitchState(dataItems, "IN/IC回路", "C相电流切到IN侧", "C相电流切到IC侧"),
                0x29 => DescribeSingleSwitchState(dataItems, "压接电机", "压接", "退出压接"),
                0x2A => DescribeLampBitmap(dataItems[0], "表位运行指示灯"),
                0x2C => DescribeTaitiLamp(dataItems),
                0x2D => DescribeTerminalType(dataItems[0]),
                0x2E => DescribeSingleSwitchState(dataItems, "红外模块", "上电", "下电"),
                0x2F => DescribeErrorExperiment(dataItems),
                0x30 => DescribeLampBitmap(dataItems[0], "表位LED指示灯"),
                0x32 => DescribeConstantSetting(dataItems),
                0x33 => DescribePhaseBitmap(dataItems[0], "表位带电状态", "A相", "B相", "C相"),
                0x3A or 0x85 => DescribeStaSelection(dataItems[0]),
                0x3B or 0x86 => DescribeStaPin(dataItems[0]),
                0x3C or 0x87 => dataItems.Length == 0 ? "读取STA引脚电平，无发送数据项。" : DescribeStaPin(dataItems[0]),
                0x41 => DescribeSwitchBoardPower(dataItems),
                0x42 => DescribeSourceSwitch(dataItems),
                0x43 => DescribeSingleSwitchState(dataItems, "电表柜电工源接入单相表", "接入", "断开"),
                0x44 => DescribeDateTimePayload(dataItems),
                0x45 => DescribeVoltageRead(dataItems),
                0x46 => DescribePanelRemovalInfo(dataItems),
                0x47 => DescribeGroundFault(dataItems),
                0x48 => DescribeSingleSwitchState(dataItems, "CT复位", "复位", "未复位"),
                0x49 => DescribeSelfCheck(dataItems),
                0x53 => DescribeSingleSwitchState(dataItems, "电子负载", "连接", "断开"),
                0x59 => DescribeSingleSwitchState(dataItems, "纹波", "连接", "断开"),
                0x60 => DescribeSingleSwitchState(dataItems, "电源切换", "切换电源", "不切换电源"),
                0x61 or 0x71 => DescribeVirtualModuleType(dataItems[0], command),
                0x62 => "设置虚拟模组重启命令；数据项：" + ToHexString(dataItems),
                0x64 => DescribeSingleSwitchState(dataItems, "电源短路状态", "电源短路", "电源开路"),
                0x65 => DescribeSingleSwitchState(dataItems, "恒定负载", "连接", "断开"),
                0x66 => DescribeVoltageSamples(dataItems),
                0x67 => DescribeInterfaceVoltageTest(dataItems),
                0x68 => DescribeSingleSwitchState(dataItems, "终端数据透传", "关闭终端透传数据", "启动透传终端数据"),
                0x69 => DescribeSingleSwitchState(dataItems, "虚拟模块状态管脚", "有GPRS模块且响应AT指令", "无GPRS模块"),
                0x6A => DescribeSingleSwitchState(dataItems, "虚拟模组USB连接", "断开连接", "恢复连接"),
                0x6C or 0x6D => DescribePinSequenceTime(dataItems),
                0x6F => DescribeSmaOrClearCache(dataItems),
                0x73 => DescribeSingleSwitchState(dataItems, "切换模组", "切到实体/其他模组", "切到虚拟模组"),
                0x99 => "设置印制板地址；数据项：" + ToHexString(dataItems),
                0x9A => DescribeSingleSwitchState(dataItems, "瞬态带载", "连接", "断开"),
                0x9B => DescribeSteadyLoad(dataItems),
                0xBD => DescribeRs485Rs232(dataItems),
                0xBE => DescribeSingleSwitchState(dataItems, "终端RS485/CAN", "切到CAN", "切到RS485"),
                0xBF => DescribeCanBaudrate(dataItems),
                0xC0 => DescribeSingleSwitchState(dataItems, "USB状态", "打开USB", "关闭USB"),
                0xC2 => $"遥信脉宽设置：{ReadUInt16LittleEndian(dataItems, 0)} ms。",
                0xC3 => DescribeAvalancheTest(dataItems),
                0xC4 => DescribeControlState(dataItems),
                0xC5 => DescribePowerRead(dataItems),
                0xCF => "主动上报运行模式；数据项：" + ToHexString(dataItems),
                _ => $"数据项原始值：{ToHexString(dataItems)}。"
            };
        }

        private static string GetProtocolCommandDocumentText(ProtocolDefinition protocolDefinition, byte command)
        {
            List<ProtocolCommandInfo> matchedSections = GetProtocolCommandSections(protocolDefinition, command);
            return matchedSections.Count > 0
                ? string.Join($"{Environment.NewLine}{Environment.NewLine}", matchedSections.Select(section => section.Text))
                : $"协议文档中未找到命令码 0x{command:X2} 的说明，请确认协议版本或txt文档内容。";
        }

        private static List<ProtocolCommandInfo> GetProtocolCommandSections(ProtocolDefinition protocolDefinition, byte command)
        {
            Dictionary<byte, List<ProtocolCommandInfo>> commandSections = protocolDefinition.HasDataDirection
                ? MeterHardwareProtocolCommandSections.Value
                : TerminalProtocolCommandSections.Value;

            return commandSections.TryGetValue(command, out List<ProtocolCommandInfo>? sections)
                ? sections
                : new List<ProtocolCommandInfo>();
        }

        private static Dictionary<byte, List<ProtocolCommandInfo>> LoadProtocolCommandSections(string relativeTextPath)
        {
            string protocolPath = Path.Combine(Application.StartupPath, relativeTextPath);
            if (!File.Exists(protocolPath))
            {
                protocolPath = Path.Combine(AppContext.BaseDirectory, relativeTextPath);
            }

            if (!File.Exists(protocolPath))
            {
                protocolPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", relativeTextPath);
            }

            Dictionary<byte, List<ProtocolCommandInfo>> result = new Dictionary<byte, List<ProtocolCommandInfo>>();
            if (!File.Exists(protocolPath))
            {
                return result;
            }

            string[] lines = File.ReadAllLines(protocolPath, Encoding.UTF8);
            List<(byte Command, int Start, string Title)> commandStarts = new List<(byte Command, int Start, string Title)>();
            for (int i = 0; i < lines.Length; i++)
            {
                if (!TryGetCommandFromDefinitionLine(lines[i], out byte command))
                {
                    continue;
                }

                int start = FindProtocolSectionStart(lines, i);
                string title = BuildProtocolSectionTitle(lines, start, i, command);
                commandStarts.Add((command, start, title));
            }

            commandStarts = commandStarts
                .GroupBy(item => new { item.Command, item.Start })
                .Select(group => group.First())
                .OrderBy(item => item.Start)
                .ToList();

            for (int i = 0; i < commandStarts.Count; i++)
            {
                (byte command, int start, string title) = commandStarts[i];
                int end = i + 1 < commandStarts.Count ? commandStarts[i + 1].Start : lines.Length;
                string text = string.Join(Environment.NewLine, lines.Skip(start).Take(end - start)).Trim();
                if (string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                if (!result.TryGetValue(command, out List<ProtocolCommandInfo>? commandSections))
                {
                    commandSections = new List<ProtocolCommandInfo>();
                    result[command] = commandSections;
                }

                commandSections.Add(new ProtocolCommandInfo(title, text));
            }

            return result;
        }

        private static bool TryGetCommandFromDefinitionLine(string line, out byte command)
        {
            command = 0;
            Match commandLineMatch = Regex.Match(line, @"(?:命令码|命令项)\s*[：:（(]\s*0x([0-9A-Fa-f]{2})", RegexOptions.IgnoreCase);
            if (commandLineMatch.Success)
            {
                command = byte.Parse(commandLineMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return true;
            }

            Match titleMatch = Regex.Match(line, @"^\s*(?:[0-9A-Fa-f]{1,2}|\d+(?:\.\d+)*)[\.、]?\s*.+?[（(]0x([0-9A-Fa-f]{2})[）)]", RegexOptions.IgnoreCase);
            if (titleMatch.Success)
            {
                command = byte.Parse(titleMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                return true;
            }

            return false;
        }

        private static int FindProtocolSectionStart(string[] lines, int commandLineIndex)
        {
            if (Regex.IsMatch(lines[commandLineIndex], @"^\s*(?:[0-9A-Fa-f]{1,2}|\d+(?:\.\d+)*)[\.、]"))
            {
                return commandLineIndex;
            }

            for (int i = commandLineIndex - 1; i >= Math.Max(0, commandLineIndex - 6); i--)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (Regex.IsMatch(line, @"^(?:[0-9A-Fa-f]{1,2}|\d+(?:\.\d+)*)[\.、]"))
                {
                    return i;
                }
            }

            return commandLineIndex;
        }

        private static string BuildProtocolSectionTitle(string[] lines, int sectionStart, int commandLineIndex, byte command)
        {
            string titleLine = lines[sectionStart].Trim();
            if (sectionStart != commandLineIndex && !string.IsNullOrWhiteSpace(titleLine))
            {
                return $"{titleLine}（0x{command:X2}）";
            }

            Match sameLineTitle = Regex.Match(titleLine, @"^\s*(?<title>.+?)(?:命令码|命令项)", RegexOptions.IgnoreCase);
            if (sameLineTitle.Success && !string.IsNullOrWhiteSpace(sameLineTitle.Groups["title"].Value))
            {
                return $"{sameLineTitle.Groups["title"].Value.Trim()}（0x{command:X2}）";
            }

            return $"命令码 0x{command:X2}";
        }

        private static MeterCommandAnalysis AnalyzeMeterHardwareCommand(byte command, byte[] dataItems)
        {
            string dataSummary = dataItems.Length == 0
                ? "无数据项。注意：电表硬件协议要求无数据时保留一个0x00，当前报文未携带数据项。"
                : $"数据项长度 {dataItems.Length} 字节，原始值：{ToHexString(dataItems)}。";
            string title = GetMeterHardwareCommandTitle(command);

            return command switch
            {
                0xFB => new MeterCommandAnalysis(
                    "反馈包",
                    "反馈包命令码0xFB。发送命令正确时返回原命令；发送命令错误时返回反馈包+数据项。",
                    dataSummary,
                    dataItems.Length >= 1 ? $"反馈码：{DescribeMeterFeedbackCode(dataItems[0])}" : "反馈包应携带1字节反馈码。"),
                0x00 => new MeterCommandAnalysis(
                    "测试包",
                    "测试包命令码0x00，当前由PC发起，用于PC与单片机链路测试。",
                    dataSummary,
                    "示例：55080000xx000008+xxAA表示PC向单片机发送；55080001xx000009+xxAA表示单片机向PC回复。"),
                0xFF => new MeterCommandAnalysis(
                    "复位命令",
                    "复位命令码0xFF，单片机回到开始上电状态。",
                    dataSummary,
                    dataItems.Length >= 1 ? $"复位数据项：{(dataItems[0] == 0x00 ? "0x00，符合命令项+0x00。" : $"非预期值{ToHex(dataItems[0])}，协议要求0x00。")}" : "复位命令应携带0x00。"),
                0x01 => new MeterCommandAnalysis(
                    "交流电压控制",
                    "交流电压控制命令码0x01，下行/上行为命令项+数据项。",
                    dataSummary,
                    dataItems.Length >= 1 ? DescribeMeterVoltageControl(dataItems[0]) : "缺少交流电压控制数据项。"),
                0x02 => new MeterCommandAnalysis(
                    "交流电流控制",
                    "交流电流控制命令码0x02，下行/上行为命令项+数据项。",
                    dataSummary,
                    dataItems.Length >= 1 ? DescribeMeterCurrentControl(dataItems[0]) : "缺少交流电流控制数据项。"),
                0x20 => new MeterCommandAnalysis(
                    "电能误差计量试验圈数",
                    "电能误差计量试验圈数命令码0x20，需配合数据项1试验类型和数据项2圈数使用。",
                    dataSummary,
                    DescribeMeterCircleCount(dataItems)),
                0x21 => new MeterCommandAnalysis(
                    "电能表基本误差",
                    "电能表基本误差命令码0x21，数据项1为误差类型，数据项2为试验启动或结果获取。",
                    dataSummary,
                    DescribeMeterBasicError(dataItems)),
                0x22 => new MeterCommandAnalysis(title, $"{title}命令码0x22，2字节圈数低字节在前。", dataSummary, dataItems.Length >= 2 ? $"圈数={ReadUInt16LittleEndian(dataItems, 0)}。" : "缺少2字节圈数。"),
                0x23 or 0x24 or 0x26 or 0x27 or 0x28 => new MeterCommandAnalysis(title, $"{title}命令码0x{command:X2}，动作码+可选float结果。", dataSummary, DescribeActionFloatData(dataItems, "结果值")),
                0x25 => new MeterCommandAnalysis(title, "潜动试验命令码0x25，动作码+可选uint32脉冲数。", dataSummary, DescribeActionUIntData(dataItems, "实际收到的脉冲数")),
                0x29 or 0x30 or 0x31 or 0x32 or 0x38 or 0x40 or 0x41 or 0x42 or 0x43 or 0x44 or 0x45 or 0x46 or 0x47 => new MeterCommandAnalysis(title, $"{title}按0x38基本误差试验数据项规则解析。", dataSummary, DescribeMeterBasicErrorV2(dataItems)),
                0x33 => new MeterCommandAnalysis(title, "时段投切命令码0x33，动作码+脉冲数+时间。", dataSummary, dataItems.Length >= 6 ? $"动作={DescribeStartGetStopAction(dataItems[0])}，脉冲数={dataItems[1]}，时间={ReadUInt32LittleEndian(dataItems, 2)}s。" : $"数据项：{ToHexString(dataItems)}。"),
                0x34 => new MeterCommandAnalysis(title, "需量周期命令码0x34，动作码+周期参数+N个float结果。", dataSummary, DescribeMeterDemandPeriod(dataItems)),
                0x35 => new MeterCommandAnalysis(title, "起潜动试验命令码0x35，动作码+脉冲数+时间。", dataSummary, dataItems.Length >= 6 ? $"动作={DescribeStartGetStopAction(dataItems[0])}，脉冲数={dataItems[1]}，时间={ReadUInt32LittleEndian(dataItems, 2)}s。" : $"数据项：{ToHexString(dataItems)}。"),
                0x36 => new MeterCommandAnalysis(title, "日计时试验命令码0x36，动作码+时间+次数+N个float结果。", dataSummary, DescribeMeterDailyTimingV2(dataItems)),
                0x37 => new MeterCommandAnalysis(title, "走字试验命令码0x37，动作码+脉冲数+标准表电能量。", dataSummary, DescribeMeterRegisterRunning(dataItems)),
                0x80 or 0x81 or 0x82 or 0x83 or 0x84 or 0x85 or 0x86 or 0x87 or 0x88 or 0x89 or 0x8A or 0x8B or 0x8C or 0x8D or 0x8E or 0x8F or 0x90
                    => new MeterCommandAnalysis(title, $"{title}命令码0x{command:X2}。", dataSummary, DescribeMeterHardwareDataSummary(command, dataItems)),
                0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5 or 0xA6 or 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC9 or 0xCA or 0xD0 or 0xD5 or 0xD6 or 0xD7 or 0xD8 or 0xF0 or 0xF1 or 0xF2 or 0xF3
                    => new MeterCommandAnalysis(title, $"{title}命令码0x{command:X2}。", dataSummary, DescribeMeterHardwareDataSummary(command, dataItems)),
                _ => new MeterCommandAnalysis(
                    title,
                    $"电表检测装置硬件通信协议V1中当前未登记命令码0x{command:X2}。",
                    dataSummary,
                    dataItems.Length == 0 ? "无数据项。" : $"数据项原始值：{ToHexString(dataItems)}。")
            };
        }

        private static string DescribeMeterFeedbackCode(byte value)
        {
            return value switch
            {
                0x01 => "0x01：校验和不对。",
                0x02 => "0x02：没有此命令码。",
                0x03 => "0x03：命令码下的数据项不对。",
                _ => $"未知反馈码 {ToHex(value)}。"
            };
        }

        private static string GetMeterHardwareCommandTitle(byte command)
        {
            return command switch
            {
                0xFB => "反馈包",
                0x00 => "测试包",
                0xFF => "复位命令",
                0x01 => "交流电压控制",
                0x02 => "交流电流控制",
                0x20 => "电能误差计量试验圈数",
                0x21 => "电能表基本误差",
                0x22 => "日计时试验圈数",
                0x23 => "日计时试验",
                0x24 => "起动试验",
                0x25 => "潜动试验",
                0x26 => "电能表常数试验",
                0x27 => "电子指示显示器电能示值组合误差试验",
                0x28 => "需量示值误差试验",
                0x29 => "误差一致性试验",
                0x30 => "变差要求试验",
                0x31 => "负载电流升降变差试验",
                0x32 => "重复性试验",
                0x33 => "时段投切",
                0x34 => "需量周期",
                0x35 => "起潜动试验",
                0x36 => "日计时试验",
                0x37 => "走字试验",
                0x38 => "基本误差试验",
                0x40 => "第5次谐波试验",
                0x41 => "方顶波波形试验",
                0x42 => "尖顶波波形试验",
                0x43 => "间谐波脉冲串触发波形试验",
                0x44 => "奇次谐波90度相位触发波形试验",
                0x45 => "半波整流波形试验",
                0x46 => "谐波有功电能准确度试验",
                0x47 => "谐波测量试验",
                0x80 => "当前测试电表类别",
                0x81 => "运行指示灯",
                0x82 => "三相直接式和互感式选择",
                0x83 => "零线电流切换",
                0x84 => "表位有无电表检测",
                0x85 => "选择表位",
                0x86 => "表位电压短路检测",
                0x87 => "跳闸测试",
                0x88 => "电表电压线路功耗",
                0x89 => "电表电流线路毫伏值",
                0x8A => "辅助电源线路",
                0x8B => "电流回路阻抗",
                0x8C => "脉冲源选择",
                0x8D => "电压电流原始数据采集",
                0x8E => "潜动试验",
                0x8F => "电表报警信号检测",
                0x90 => "接线检测/电压电流停止数据采集",
                0xA0 => "待测电表有功脉冲常数",
                0xA1 => "待测电表无功脉冲常数",
                0xA2 => "标准表有功脉冲常数",
                0xA3 => "标准表无功脉冲常数",
                0xA4 => "时基源脉冲常数",
                0xA5 => "当前试验停止",
                0xA6 => "误差仪回到初始界面",
                0xC0 => "模组直流电压通断",
                0xC1 => "电表带载",
                0xC2 => "电表复位信号电平持续时间",
                0xC3 => "电表实际模块直流功耗",
                0xC9 => "表位电机压接",
                0xCA => "风扇温度获取",
                0xD0 => "计量芯片校准",
                0xD5 => "功放电压挡位选择",
                0xD6 => "功放电流挡位选择",
                0xD7 => "功放电流互感器挡位选择",
                0xD8 => "电流负载切换",
                0xF0 => "IP地址更改",
                0xF1 => "端口属性更改",
                0xF2 => "嵌软版本号获取",
                0xF3 => "串口服务器端口信息获取",
                _ => $"未登记电表硬件命令 0x{command:X2}"
            };
        }

        private static bool IsMeterControlCodeValid(byte value)
        {
            return value is >= 0x01 and <= 0x08;
        }

        private static string DescribeMeterVoltageControl(byte value)
        {
            return value switch
            {
                0x01 => "A相上电，单相。",
                0x02 => "B相上电。",
                0x03 => "C相上电。",
                0x04 => "三相上电。",
                0x05 => "A相断电，单相。",
                0x06 => "B相断电。",
                0x07 => "C相断电。",
                0x08 => "三相断电。",
                _ => $"未知交流电压控制数据项 {ToHex(value)}。"
            };
        }

        private static string DescribeMeterCurrentControl(byte value)
        {
            return value switch
            {
                0x01 => "A相通电流，单相。",
                0x02 => "B相通电流。",
                0x03 => "C相通电流。",
                0x04 => "三相通电流。",
                0x05 => "A相断电流，单相旁路。",
                0x06 => "B相断电流。",
                0x07 => "C相断电流。",
                0x08 => "三相断电流。",
                _ => $"未知交流电流控制数据项 {ToHex(value)}。"
            };
        }

        private static string DescribeMeterCircleTestType(byte value)
        {
            return value switch
            {
                0x01 => "0x01：有功。",
                0x02 => "0x02：无功。",
                0x03 => "0x03：谐波。",
                _ => $"未知试验类型 {ToHex(value)}。"
            };
        }

        private static string DescribeMeterBasicErrorType(byte value)
        {
            return value switch
            {
                0x01 => "0x01：有功。",
                0x02 => "0x02：无功，电表上正向反向是同一个脉冲口。",
                0x03 => "0x03：有功+无功。",
                0x04 => "0x04：谐波。",
                _ => $"未知误差类型 {ToHex(value)}。"
            };
        }

        private static string DescribeMeterCircleCount(byte[] dataItems)
        {
            if (dataItems.Length < 3)
            {
                return $"圈数命令需要3字节数据项：试验类型1字节+圈数2字节。当前：{ToHexString(dataItems)}。";
            }

            return $"试验类型：{DescribeMeterCircleTestType(dataItems[0])} 圈数：{ReadUInt16LittleEndian(dataItems, 1)}。";
        }

        private static string DescribeMeterBasicErrorAction(byte value)
        {
            return value switch
            {
                0x01 => "0x01：试验启动。",
                0xAA => "0xAA：试验结果获取。",
                _ => $"未知试验动作 {ToHex(value)}。"
            };
        }

        private static string DescribeMeterBasicError(byte[] dataItems)
        {
            if (dataItems.Length < 2)
            {
                return $"基本误差命令至少需要2字节数据项：误差类型+试验动作。当前：{ToHexString(dataItems)}。";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"误差类型：{DescribeMeterBasicErrorType(dataItems[0])}");
            builder.AppendLine($"试验动作：{DescribeMeterBasicErrorAction(dataItems[1])}");
            if (dataItems[1] == 0xAA && dataItems.Length >= 6)
            {
                float error = ReadFloatLittleEndian(dataItems, 2);
                builder.AppendLine($"误差结果：{error:0.######}。");
                builder.AppendLine("特殊值说明：1.0表示被测表校表脉冲未输出；2.0表示结果未计算出或未到试验时间。");
            }

            return builder.ToString().Trim();
        }

        private static string DescribeMeterHardwareDataSummary(byte command, byte[] dataItems)
        {
            if (dataItems.Length == 0)
            {
                return "无数据项。";
            }

            return command switch
            {
                0x80 => DescribeMeterCategory(dataItems[0]),
                0x81 => DescribeMeterRunLamp(dataItems[0]),
                0x8C => DescribePulseSource(dataItems[0]),
                0xA0 or 0xA1 when dataItems.Length >= 4 => $"uint32常数={ReadUInt32LittleEndian(dataItems, 0)}。",
                0xA2 or 0xA3 when dataItems.Length >= 8 => $"uint64常数={ReadUInt64LittleEndian(dataItems, 0)}。",
                0xF0 when dataItems.Length >= 4 => $"IP={dataItems[0]}.{dataItems[1]}.{dataItems[2]}.{dataItems[3]}。",
                _ => $"数据项原始值：{ToHexString(dataItems)}。"
            };
        }

        private static string DescribeActionFloatData(byte[] dataItems, string label)
        {
            if (dataItems.Length == 0)
            {
                return "缺少动作数据项。";
            }

            string result = dataItems.Length >= 5 ? $"，{label}={ReadFloatLittleEndian(dataItems, 1):0.######}" : string.Empty;
            return $"动作：{DescribeMeterExperimentAction(dataItems[0])}{result}。";
        }

        private static string DescribeActionUIntData(byte[] dataItems, string label)
        {
            if (dataItems.Length == 0)
            {
                return "缺少动作数据项。";
            }

            string result = dataItems.Length >= 5 ? $"，{label}={ReadUInt32LittleEndian(dataItems, 1)}" : string.Empty;
            return $"动作：{DescribeMeterExperimentAction(dataItems[0])}{result}。";
        }

        private static string DescribeMeterBasicErrorV2(byte[] dataItems)
        {
            if (dataItems.Length == 0)
            {
                return "缺少动作数据项。";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"动作：{DescribeStartGetStopAction(dataItems[0])}");
            if (dataItems.Length >= 2) builder.AppendLine($"脉冲数：{dataItems[1]}。");
            if (dataItems.Length >= 3) builder.AppendLine($"试验次数：{dataItems[2]}。");
            if (dataItems.Length >= 4) builder.AppendLine($"脉冲类型：{DescribeActiveReactiveType(dataItems[3])}");
            int resultIndex = 1;
            for (int i = 3; i + 3 < dataItems.Length; i += 4)
            {
                float value = ReadFloatLittleEndian(dataItems, i);
                builder.AppendLine($"结果{resultIndex}：{value:0.######}。{DescribeSpecialMeterResult(value)}");
                resultIndex++;
            }

            return builder.ToString().Trim();
        }

        private static string DescribeMeterDemandPeriod(byte[] dataItems)
        {
            if (dataItems.Length < 5)
            {
                return $"需量周期至少需要5字节参数。当前：{ToHexString(dataItems)}。";
            }

            return $"动作={DescribeStartGetStopAction(dataItems[0])}，需量周期={dataItems[1]}min，滑差时间={dataItems[2]}min，滑差数={dataItems[3]}，次数={dataItems[4]}。";
        }

        private static string DescribeMeterDailyTimingV2(byte[] dataItems)
        {
            if (dataItems.Length < 3)
            {
                return $"日计时试验至少需要动作+时间+次数。当前：{ToHexString(dataItems)}。";
            }

            return $"动作={DescribeStartGetStopAction(dataItems[0])}，时间={dataItems[1]}s，次数={dataItems[2]}。";
        }

        private static string DescribeMeterRegisterRunning(byte[] dataItems)
        {
            if (dataItems.Length < 9)
            {
                return $"走字试验结果包应包含动作+uint32脉冲数+float电能量。当前：{ToHexString(dataItems)}。";
            }

            return $"动作={DescribeStartGetStopAction(dataItems[0])}，被测表脉冲数={ReadUInt32LittleEndian(dataItems, 1)}，标准表电能量={ReadFloatLittleEndian(dataItems, 5):0.######}kWh。";
        }

        private static string DescribeMeterExperimentAction(byte value)
        {
            return value switch
            {
                0x00 => "0x00：开始试验。",
                0x01 => "0x01：试验启动/开始测试。",
                0xAA => "0xAA：结果获取。",
                0xFF => "0xFF：试验停止/测试完成。",
                _ => $"未知动作 {ToHex(value)}。"
            };
        }

        private static string DescribeStartGetStopAction(byte value)
        {
            return value switch
            {
                0x00 => "0x00：开始试验。",
                0x01 => "0x01：开始/设置。",
                0xAA => "0xAA：结果获取。",
                0xFF => "0xFF：停止/完成。",
                _ => $"未知动作 {ToHex(value)}。"
            };
        }

        private static string DescribeSpecialMeterResult(float value)
        {
            if (Math.Abs(value - 1.0f) < 0.0001f)
            {
                return "特殊值：1.0表示未检测到完整脉冲。";
            }

            if (Math.Abs(value - 2.0f) < 0.0001f)
            {
                return "特殊值：2.0表示有脉冲但时间未到/结果未完成。";
            }

            if (Math.Abs(value - 0.99f) < 0.0001f)
            {
                return "特殊值：0.99表示当前脉冲未捕获到，返回上一次结果。";
            }

            return string.Empty;
        }

        private static string DescribeActiveReactiveType(byte value)
        {
            return value switch
            {
                0x00 => "0x00：有功。",
                0x01 => "0x01：无功。",
                _ => $"未知脉冲类型 {ToHex(value)}。"
            };
        }

        private static string DescribeMeterCategory(byte value) => value switch { 0x01 => "单相。", 0x02 => "三相四线。", 0x03 => "三相三线。", _ => $"未知类别{ToHex(value)}。" };
        private static string DescribeMeterRunLamp(byte value) => value switch { 0x01 => "测试中。", 0x02 => "测试合格。", 0x03 => "测试不合格或者出错。", 0x04 => "运行灯关闭。", 0x05 => "运行灯复位。", _ => $"未知灯状态{ToHex(value)}。" };
        private static string DescribeMeterBoardSource(byte value) => value switch { 0x01 => "PC下发。", 0x02 => "装置通信板发出。", 0xFF => "PC下发恢复检测。", 0xAA => "获取当前状态。", _ => $"未知来源{ToHex(value)}。" };
        private static string DescribeDirectTransformerMode(byte value) => value switch { 0x01 => "直接式。", 0x02 => "互感式。", _ => $"未知模式{ToHex(value)}。" };
        private static string DescribeNeutralCurrentMode(byte value) => value switch { 0x01 => "相电流。", 0x02 => "相电流切换到零线。", _ => $"未知模式{ToHex(value)}。" };
        private static string DescribeMeterPresence(byte value) => value switch { 0x00 => "无电表。", 0x01 => "有电表。", 0x02 => "短接磁保持继电器短路异常。", _ => $"未知结果{ToHex(value)}。" };
        private static string DescribeVoltageShortResult(byte value) => value switch { 0x00 => "电压正常。", 0x01 => "A相电压短路。", 0x02 => "B相电压短路。", 0x04 => "C相电压短路。", 0x03 => "A、B与N短路。", 0x05 => "A、C与N短路。", 0x06 => "B、C与N短路。", 0x07 => "三相电压都短路。", _ => $"未知短路结果{ToHex(value)}。" };
        private static string DescribeTripSwitchType(byte value) => value switch { 0x01 => "开关内置。", 0x02 => "开关外置-控制和反馈。", 0x03 => "开关外置-继电器。", _ => $"未知开关类型{ToHex(value)}。" };
        private static string DescribeTripState(byte value) => value switch { 0x01 => "合闸状态。", 0x02 => "拉闸状态。", _ => $"未知状态{ToHex(value)}。" };
        private static string DescribeSuccessFail(byte value) => value switch { 0x01 => "成功。", 0x02 => "失败。", _ => $"未知结果{ToHex(value)}。" };
        private static string DescribePulseSource(byte value) => value switch { 0x00 => "电脉冲，默认。", 0x01 => "光电脉冲。", 0x02 => "蓝牙脉冲。", _ => $"未知脉冲源{ToHex(value)}。" };
        private static string DescribeModuleKind(byte value) => value switch { 0x01 => "虚拟模组。", 0x02 => "实际模组。", _ => $"未知模组{ToHex(value)}。" };
        private static string DescribeOnOff2(byte value) => value switch { 0x01 => "接通。", 0x02 => "断开。", _ => $"未知状态{ToHex(value)}。" };
        private static string DescribeMotorPress(byte value) => value switch { 0x00 => "压接。", 0x01 => "弹开。", 0xFF => "电机断电。", _ => $"未知电机动作{ToHex(value)}。" };
        private static string DescribeParity(byte value) => value switch { 0x00 => "偶校验。", 0x01 => "奇校验。", 0x02 => "无校验。", _ => $"未知校验{ToHex(value)}。" };
        private static string DescribeWiringCheckType(byte value) => value switch { 0x00 => "基本误差+日计时误差。", 0x01 => "基本误差。", 0x02 => "日计时误差。", _ => $"未知测试类型{ToHex(value)}。" };

        private static string DescribeMeterPositionByte(byte value, int startPosition)
        {
            List<string> selected = new List<string>();
            for (int bit = 0; bit < 8; bit++)
            {
                if ((value & (1 << bit)) != 0)
                {
                    selected.Add((startPosition + bit).ToString(CultureInfo.InvariantCulture));
                }
            }

            return selected.Count == 0 ? "未选择表位。" : $"选择表位：{string.Join("、", selected)}。";
        }

        private static string DescribeCalibrationTarget(byte value)
        {
            return value switch
            {
                0x00 => "三相电压。",
                0x01 => "A相电压。",
                0x02 => "B相电压。",
                0x03 => "C相电压。",
                0x04 => "A相电流。",
                0x05 => "B相电流。",
                0x06 => "C相电流。",
                0x07 => "三相电流。",
                0x08 => "A相视在功率。",
                0x09 => "B相视在功率。",
                0x0A => "C相视在功率。",
                0x0B => "三相视在功率。",
                0x0C => "A相毫伏值。",
                0x0D => "B相毫伏值。",
                0x0E => "C相毫伏值。",
                0x0F => "三相毫伏值。",
                0x10 => "A相功率因素。",
                0x11 => "B相功率因素。",
                0x12 => "C相功率因素。",
                0x13 => "三相功率因素。",
                _ => $"未知校准对象{ToHex(value)}。"
            };
        }

        private static string DescribeRangeState(byte command, byte value)
        {
            return command switch
            {
                0xD5 => value switch { 0xFF => "升压器电压输出关断。", 0x01 => "60V档。", 0x02 => "120V档。", 0x03 => "220V档。", 0x04 => "380V档。", _ => $"未知电压挡位{ToHex(value)}。" },
                0xD6 => value switch { 0xFF => "升流器输出关断。", 0x01 => "1mA档。", 0x02 => "10mA档。", 0x03 => "100mA档。", 0x04 => "1A档。", 0x05 => "10A档。", 0x06 => "100A档。", _ => $"未知电流挡位{ToHex(value)}。" },
                0xD7 => value switch { 0x01 => "1mA档。", 0x02 => "10mA档。", 0x03 => "100mA档。", 0x04 => "1A档。", 0x05 => "10A档。", 0x06 => "100A档。", _ => $"未知互感器挡位{ToHex(value)}。" },
                0xD8 => value switch { 0x01 => "重负载。", 0x02 => "轻负载。", _ => $"未知负载挡位{ToHex(value)}。" },
                _ => $"未知状态{ToHex(value)}。"
            };
        }

        private static string GetMeterHardwareProtocolSummary()
        {
            string protocolPath = Path.Combine(Application.StartupPath, MeterHardwareProtocolTextRelativePath);
            if (!File.Exists(protocolPath))
            {
                protocolPath = Path.Combine(AppContext.BaseDirectory, MeterHardwareProtocolTextRelativePath);
            }

            if (!File.Exists(protocolPath))
            {
                protocolPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Protocol", "MeterHardwareProtocolV1.txt");
            }

            if (File.Exists(protocolPath))
            {
                return File.ReadAllText(protocolPath, Encoding.UTF8);
            }

            return """
                电表检测装置硬件通信协议V1（与MCU部分）：
                1. 该部分主要描述MCU上传以及PC接收/下发的交互协议。
                2. 协议包括控制协议和透传协议。
                3. 控制协议用于控制台体模块电源系统、遥信状态设置、遥控状态读取、电压电流等参数检测。
                4. 透传协议用于透传模块和电表/虚拟表之间的通信报文或抄表报文，例如645、698、1376.2、1376.3。
                5. 控制协议采用485通信，波特率115200b/S，无校验，1位停止位；透传协议端口参数随被测模块变化。
                6. 帧结构：55 + 长度Lo + 长度Hi + 数据方向 + 地址/通道 + 协议类型 + 命令码 + 数据项 + 校验和 + AA。
                7. 与终端检测装置电测协议的关键差异：电表硬件协议在数据长度之后多1字节“数据方向”。
                8. 数据方向：0x00表示PC到MCU，0x01表示MCU到PC，其他值预留。
                9. 地址/通道：单片机控制位号，从1开始；0xAA表示所有工位广播响应。
                10. 协议类型：0x00控制协议，0x01透传协议。
                11. 数据项：命令附加数据；无数据时保留一个0x00。
                12. 校验和：从数据长度低字节开始累加到校验和前一字节，取低字节；不包括起始字符55和结束字符AA。
                """;
        }

        private static string DescribeBitStates(byte[] dataItems, string label, int startIndex, int count, string oneText, string zeroText)
        {
            List<string> parts = new List<string>();
            int itemIndex = 0;
            for (int i = startIndex; i < startIndex + count; i++)
            {
                int byteIndex = itemIndex / 8;
                int bitIndex = itemIndex % 8;
                if (byteIndex >= dataItems.Length)
                {
                    break;
                }

                bool enabled = (dataItems[byteIndex] & (1 << bitIndex)) != 0;
                parts.Add($"{label}{i}:{(enabled ? oneText : zeroText)}");
                itemIndex++;
            }

            return string.Join("，", parts);
        }

        private static string DescribeControlState(byte[] dataItems)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"原始数据项：{ToHexString(dataItems)}");
            if (dataItems.Length >= 1)
            {
                builder.AppendLine("状态位：" + DescribeBits(dataItems[0], 8, "闭合/有/电平", "断开/无/脉冲"));
            }
            if (dataItems.Length >= 2)
            {
                builder.AppendLine("变化位：" + DescribeBits(dataItems[1], 8, "有变化", "无变化"));
            }
            if (dataItems.Length >= 4)
            {
                builder.AppendLine("补充状态位：" + DescribeBits(dataItems[2], 8, "闭合", "断开"));
                builder.AppendLine("补充变化位：" + DescribeBits(dataItems[3], 8, "有变化", "无变化"));
            }
            if (dataItems.Length >= 12)
            {
                builder.AppendLine($"遥控1脉冲宽度：{ReadUInt16LittleEndian(dataItems, 4)}");
                builder.AppendLine($"遥控2脉冲宽度：{ReadUInt16LittleEndian(dataItems, 6)}");
                builder.AppendLine($"遥控3脉冲宽度：{ReadUInt16LittleEndian(dataItems, 8)}");
                builder.AppendLine($"遥控4脉冲宽度：{ReadUInt16LittleEndian(dataItems, 10)}");
            }

            return builder.ToString().Trim();
        }

        private static string DescribePulseConfig(byte[] dataItems)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"脉冲通道：{(dataItems.Length > 0 ? DescribeBits(dataItems[0], 4, "选中", "未选") : "缺少")}");
            if (dataItems.Length > 1)
            {
                builder.AppendLine($"启动标志：{(dataItems[1] == 0x01 ? "启动脉冲发送" : dataItems[1] == 0x00 ? "停止脉冲发送" : $"未知 {ToHex(dataItems[1])}")}");
            }
            if (dataItems.Length >= 6)
            {
                builder.AppendLine($"输出脉冲频率：{ReadFloatLittleEndian(dataItems, 2)}");
            }
            if (dataItems.Length >= 10)
            {
                builder.AppendLine($"输出脉冲个数：{ReadUInt32LittleEndian(dataItems, 6)}");
            }
            if (dataItems.Length >= 11)
            {
                builder.AppendLine($"占空比：{dataItems[10]}%");
            }
            return builder.ToString().Trim();
        }

        private static string DescribeSingleSwitchState(byte[] dataItems, string label, string oneText, string zeroText)
        {
            byte value = dataItems[0];
            return $"{label}：{(value == 0x01 ? oneText : value == 0x00 ? zeroText : $"未知值 {ToHex(value)}")}。原始数据项：{ToHexString(dataItems)}";
        }

        private static string DescribeMeterInfo(byte[] dataItems, bool hasExtraStatus)
        {
            StringBuilder builder = new StringBuilder();
            if (dataItems.Length >= 6)
            {
                builder.AppendLine($"电表地址：{string.Concat(dataItems.Take(6).Select(ToHex))}");
            }
            if (dataItems.Length >= 7)
            {
                builder.AppendLine($"电能表协议类型：{DescribeMeterProtocol(dataItems[6])}");
            }
            if (dataItems.Length >= 11)
            {
                builder.AppendLine($"电能表485波特率：{ReadUInt32LittleEndian(dataItems, 7)}");
            }
            if (hasExtraStatus && dataItems.Length >= 13)
            {
                builder.AppendLine($"表位压接状态：{(dataItems[11] == 1 ? "接入" : "未接入")}");
                builder.AppendLine($"电能表跳闸状态：{(dataItems[12] == 1 ? "跳闸" : "未跳闸")}");
            }
            return builder.Length == 0 ? $"原始数据项：{ToHexString(dataItems)}" : builder.ToString().Trim();
        }

        private static string DescribeRs485Rs232(byte[] dataItems)
        {
            if (dataItems.Length < 2)
            {
                return $"切换终端RS485/RS232，数据项不足：{ToHexString(dataItems)}";
            }
            string port = dataItems[0] == 0x03 ? "485-3" : dataItems[0] == 0x04 ? "485-4" : $"未知端口{ToHex(dataItems[0])}";
            string mode = dataItems[1] == 0x00 ? "RS485" : dataItems[1] == 0x01 ? "RS232" : $"未知模式{ToHex(dataItems[1])}";
            return $"切换{port}到{mode}。";
        }

        private static string DescribeCanBaudrate(byte[] dataItems)
        {
            int value = dataItems.Length >= 2 ? (dataItems[0] << 8) + dataItems[1] : dataItems[0];
            return $"CAN波特率设置：{value}K。原始数据项：{ToHexString(dataItems)}";
        }

        private static string DescribeTemperatureHumidity(byte[] dataItems)
        {
            StringBuilder builder = new StringBuilder();
            if (dataItems.Length >= 4)
            {
                builder.AppendLine($"温度：{ReadFloatLittleEndian(dataItems, 0)}");
            }
            if (dataItems.Length >= 8)
            {
                builder.AppendLine($"湿度：{ReadFloatLittleEndian(dataItems, 4)}");
            }
            return builder.Length == 0 ? $"原始数据项：{ToHexString(dataItems)}" : builder.ToString().Trim();
        }

        private static string DescribePhaseBitmap(byte value, string label, string phaseA, string phaseB, string phaseC)
        {
            return $"{label}：{phaseA}={(value & 0x01) != 0}，{phaseB}={(value & 0x02) != 0}，{phaseC}={(value & 0x04) != 0}。原始值：{ToHex(value)}";
        }

        private static string DescribeLampBitmap(byte value, string label)
        {
            string color = (value & 0x07) switch
            {
                0x01 => "红色",
                0x02 => "绿色",
                0x04 => "黄色",
                0x00 => "熄灭/无颜色",
                _ => "组合颜色"
            };
            List<string> lamps = new List<string>();
            if ((value & 0x08) != 0) lamps.Add("LED1");
            if ((value & 0x10) != 0) lamps.Add("LED2");
            if ((value & 0x20) != 0) lamps.Add("LED3");
            if ((value & 0x40) != 0) lamps.Add("LED4");
            return $"{label}：颜色={color}，灯位={(lamps.Count == 0 ? "未指定" : string.Join("/", lamps))}。原始值：{ToHex(value)}";
        }

        private static string DescribeTaitiLamp(byte[] dataItems)
        {
            if (dataItems.Length < 2)
            {
                return $"台体运行状态指示灯控制，数据项不足：{ToHexString(dataItems)}";
            }
            string lamp = dataItems[0] switch { 0x01 => "红灯", 0x02 => "绿灯", 0x03 => "黄灯", _ => $"未知灯{ToHex(dataItems[0])}" };
            string state = dataItems[1] == 0x01 ? "亮" : dataItems[1] == 0x00 ? "灭" : $"未知状态{ToHex(dataItems[1])}";
            return $"{lamp}{state}。";
        }

        private static string DescribeTerminalType(byte value)
        {
            return value switch
            {
                0x00 => "终端类型：断开。",
                0x01 => "终端类型：台区智能融合终端。",
                0x02 => "终端类型：13版集中器I型。",
                0x03 => "终端类型：13版专变III型。",
                0x04 => "终端类型：22版集中器I型。",
                0x05 => "终端类型：22版专变III型。",
                0x06 => "终端类型：22版能源控制器。",
                0x07 => "终端类型：南网-负荷管理终端。",
                0x08 => "终端类型：南网-配变监测计量终端。",
                0x09 => "终端类型：南网-13集中器。",
                _ => $"终端类型：未知 {ToHex(value)}。"
            };
        }

        private static string DescribeStaSelection(byte value)
        {
            return value switch
            {
                0x00 => "STA1/STA2断开。",
                0x01 => "选择STA1。",
                0x02 => "选择STA2。",
                0x03 => "选择STA1和STA2。",
                _ => $"未知STA选择：{ToHex(value)}。"
            };
        }

        private static string DescribeStaPin(byte value)
        {
            List<string> pins = new List<string>();
            if ((value & 0x01) != 0) pins.Add("RST");
            if ((value & 0x02) != 0) pins.Add("SET");
            if ((value & 0x04) != 0) pins.Add("EVENT");
            return pins.Count == 0 ? "RST/SET/EVENT均拉低或关闭。" : $"RST/SET/EVENT状态：{string.Join("/", pins)}有效。";
        }

        private static string DescribeInterferencePulse(byte[] dataItems)
        {
            string lines = dataItems.Length > 0 ? DescribeBits((byte)(dataItems[0] & 0x1F), 5, "输出", "不输出") : "缺少遥信位";
            string width = dataItems.Length >= 3 ? $"{ReadUInt16LittleEndian(dataItems, 1)} ms" : "缺少脉宽";
            return $"遥信/脉冲端口干扰脉冲：{lines}；脉宽：{width}。";
        }

        private static string DescribeControlChangeTime(byte[] dataItems)
        {
            if (dataItems.Length == 1)
            {
                return dataItems[0] switch
                {
                    0x01 => "读取第一路控制信号时间变化。",
                    0x02 => "读取第二路控制信号时间变化。",
                    0x03 => "读取第三路控制信号时间变化。",
                    0x04 => "读取第四路控制信号时间变化。",
                    0xFF => "无效控制信号。",
                    _ => $"未知控制信号序号：{ToHex(dataItems[0])}。"
                };
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"控制端子变化时间数据项长度：{dataItems.Length} 字节。");
            for (int i = 0; i + 5 < dataItems.Length; i += 6)
            {
                string signal = dataItems[i] switch
                {
                    0x01 => "第一路",
                    0x02 => "第二路",
                    0x03 => "第三路",
                    0x04 => "第四路",
                    0xFF => "无效",
                    _ => $"未知{ToHex(dataItems[i])}"
                };
                string state = dataItems[i + 1] switch
                {
                    0x01 => "断开",
                    0x02 => "闭合",
                    0xFF => "无效",
                    _ => $"未知{ToHex(dataItems[i + 1])}"
                };
                uint time = ReadUInt32LittleEndian(dataItems, i + 2);
                builder.AppendLine($"{signal}控制信号：{state}，变化时间：{(time == 0xFFFFFFFF ? "无变化" : time.ToString(CultureInfo.InvariantCulture))}。");
            }

            return builder.ToString().Trim();
        }

        private static string DescribeSwitchBoardPower(byte[] dataItems)
        {
            if (dataItems.Length < 2)
            {
                return $"切换板交流上电，数据项不足：{ToHexString(dataItems)}";
            }
            string source = dataItems[0] switch { 0x01 => "源表", 0x02 => "功放", 0x03 => "先上源表后上功放", 0x04 => "读取状态", _ => $"未知{ToHex(dataItems[0])}" };
            string state = dataItems[1] == 0x01 ? "上电" : dataItems[1] == 0x00 ? "下电" : $"未知状态{ToHex(dataItems[1])}";
            return $"切换板：{source}，{state}。";
        }

        private static string DescribeSourceSwitch(byte[] dataItems)
        {
            return $"标准源/电工源切换：{(dataItems[0] == 0x00 ? "标准源" : dataItems[0] == 0x01 ? "电工源" : $"未知{ToHex(dataItems[0])}")}。";
        }

        private static string DescribeDateTimePayload(byte[] dataItems)
        {
            if (dataItems.Length == 0)
            {
                return "读取当前时间。";
            }
            if (dataItems.Length >= 6)
            {
                return $"设置/返回时间：20{dataItems[0]:D2}-{dataItems[1]:D2}-{dataItems[2]:D2} {dataItems[3]:D2}:{dataItems[4]:D2}:{dataItems[5]:D2}。";
            }
            return $"时间数据项：{ToHexString(dataItems)}";
        }

        private static string DescribeVoltageRead(byte[] dataItems)
        {
            if (dataItems.Length >= 12)
            {
                return $"A相电压:{ReadFloatLittleEndian(dataItems, 0)}，B相电压:{ReadFloatLittleEndian(dataItems, 4)}，C相电压:{ReadFloatLittleEndian(dataItems, 8)}。";
            }
            return $"读取电压命令数据项：{ToHexString(dataItems)}";
        }

        private static string DescribeVoltageSamples(byte[] dataItems)
        {
            if (dataItems.Length % 2 == 0 && dataItems.Length > 0)
            {
                List<string> values = new List<string>();
                for (int i = 0; i < dataItems.Length; i += 2)
                {
                    values.Add(ReadUInt16LittleEndian(dataItems, i).ToString(CultureInfo.InvariantCulture));
                }
                return "电压采样数据：" + string.Join("，", values);
            }
            return $"读取电压采样数据：{ToHexString(dataItems)}";
        }

        private static string DescribePanelRemovalInfo(byte[] dataItems)
        {
            return $"面板拆卸信息数据长度 {dataItems.Length} 字节。协议说明每条信息7字节，当前可解析条数：{dataItems.Length / 7}。原始：{ToHexString(dataItems)}";
        }

        private static string DescribeGroundFault(byte[] dataItems)
        {
            return $"接地故障测试数据项：{ToHexString(dataItems)}";
        }

        private static string DescribeSelfCheck(byte[] dataItems)
        {
            byte value = dataItems[0];
            return value switch
            {
                0x01 => "3相3开始自检。",
                0x02 => "3相4开始自检。",
                0x03 => "读取自检结果。",
                0x10 => "复位自检结果。",
                0xFF => "自检结果未准备好或无效。",
                _ => $"未知自检命令/结果：{ToHex(value)}。"
            };
        }

        private static string DescribeErrorExperiment(byte[] dataItems)
        {
            StringBuilder builder = new StringBuilder();
            if (dataItems.Length > 0)
            {
                builder.AppendLine($"实验类型：{(dataItems[0] == 1 ? "有功" : dataItems[0] == 2 ? "无功" : dataItems[0] == 3 ? "日计时" : $"未知{ToHex(dataItems[0])}")}");
            }
            if (dataItems.Length > 1)
            {
                builder.AppendLine($"脉冲类型：{(dataItems[1] == 1 ? "电脉冲" : dataItems[1] == 2 ? "蓝牙脉冲" : dataItems[1] == 3 ? "光脉冲" : $"未知{ToHex(dataItems[1])}")}");
            }
            if (dataItems.Length > 2)
            {
                builder.AppendLine($"操作：{(dataItems[2] == 1 ? "实验启动" : dataItems[2] == 2 ? "实验停止" : dataItems[2] == 3 ? "圈数设置" : dataItems[2] == 0xAA ? "结果获取" : $"未知{ToHex(dataItems[2])}")}");
            }
            return builder.Length == 0 ? $"误差实验数据项：{ToHexString(dataItems)}" : builder.ToString().Trim();
        }

        private static string DescribeConstantSetting(byte[] dataItems)
        {
            string type = dataItems[0] switch
            {
                0x01 => "设置标准表有功脉冲常数",
                0x02 => "设置标准表无功脉冲常数",
                0x03 => "设置待测表有功脉冲常数",
                0x04 => "设置待测表无功脉冲常数",
                0x05 => "设置时钟频率",
                _ => $"未知常数类型{ToHex(dataItems[0])}"
            };
            return $"{type}。原始数据项：{ToHexString(dataItems)}";
        }

        private static string DescribeModulePowerTest(byte[] dataItems)
        {
            if (dataItems.Length < 2)
            {
                return $"连续测量模块功耗命令数据项：{ToHexString(dataItems)}";
            }
            return $"功耗测试：{(dataItems[0] == 1 ? "启动" : "停止")}，模块{dataItems[1]}。";
        }

        private static string DescribeInterfaceVoltageTest(byte[] dataItems)
        {
            if (dataItems.Length < 2)
            {
                return $"连续测量模块接口电压命令数据项：{ToHexString(dataItems)}";
            }
            return $"接口电压测试：{(dataItems[0] == 1 ? "启动" : "停止")}，模块{dataItems[1]}。";
        }

        private static string DescribeVirtualModuleType(byte value, byte command)
        {
            if (command == 0x61)
            {
                return value switch
                {
                    0x01 => "虚拟模组类型：远程通信模块（3个ACM通道）。",
                    0x02 => "虚拟模组类型：本地通信模块（2个ACM通道）。",
                    0x03 => "虚拟模组类型：RS485/CAN/MBUS通信模块（4路-5个ACM通道）。",
                    0x04 => "虚拟模组类型：脉冲/遥信/遥控/模拟量采集通信模块。",
                    0x05 => "虚拟模组类型：RS485/CAN/MBUS通信模块（2路3个ACM通道）。",
                    _ => $"未知虚拟模组类型：{ToHex(value)}。"
                };
            }

            return value switch
            {
                0x01 => "设置远程通信模块。",
                0x02 => "设置本地通信模块。",
                0x03 => "设置2路RS485。",
                0x04 => "设置4路RS485。",
                0x05 => "设置2路MBUS。",
                0x06 => "设置4路MBUS。",
                0x07 => "设置1路CAN。",
                0x08 => "设置2路CAN。",
                0x09 => "设置4路CAN。",
                0x0A => "设置2路脉冲/遥信。",
                0x0B => "设置4路脉冲/遥信。",
                0x0C => "设置2路遥控。",
                0x0D => "设置模拟量采集模块。",
                0x0E => "设置回路状态巡检模块。",
                _ => $"未知虚拟模组类型：{ToHex(value)}。"
            };
        }

        private static string DescribePinSequenceTime(byte[] dataItems)
        {
            if (dataItems.Length < 1)
            {
                return "读取管脚电平和发生时间，缺少读取序列。";
            }
            string sequence = dataItems[0] switch { 0x01 => "序列1", 0x02 => "序列2", 0x03 => "序列3", _ => $"未知序列{ToHex(dataItems[0])}" };
            return $"{sequence}，原始数据项：{ToHexString(dataItems)}";
        }

        private static string DescribeSmaOrClearCache(byte[] dataItems)
        {
            if (dataItems.Length == 0)
            {
                return "清空RST及ON/OFF引脚电平缓存。";
            }
            return dataItems[0] switch
            {
                0x01 => "SMA接口连接示波器。",
                0x02 => "SMA接口连接频谱仪。",
                _ => $"SMA/缓存命令数据项：{ToHexString(dataItems)}"
            };
        }

        private static string DescribeSteadyLoad(byte[] dataItems)
        {
            int current = dataItems.Length >= 2 ? ReadUInt16LittleEndian(dataItems, 0) : dataItems[0];
            return current == 0 ? "关闭稳态带载。" : $"打开稳态带载，电流约 {current} mA。";
        }

        private static string DescribeAvalancheTest(byte[] dataItems)
        {
            if (dataItems.Length < 3)
            {
                return $"遥信雪崩测试数据项不足：{ToHexString(dataItems)}";
            }
            return $"时间：{dataItems[0]}秒，次数：{dataItems[1]}次，启动标志：{(dataItems[2] == 1 ? "开始" : "停止")}。";
        }

        private static string DescribePowerRead(byte[] dataItems)
        {
            if (dataItems.Length == 1)
            {
                return dataItems[0] switch
                {
                    0x01 => "读取A相功率数据。",
                    0x02 => "读取B相功率数据。",
                    0x03 => "读取C相功率数据。",
                    0x04 => "读取合相有功功率/无功功率/视在功率。",
                    0xAA => "校准/保存。",
                    0xBB => "复位。",
                    _ => $"未知功率读取类型：{ToHex(dataItems[0])}。"
                };
            }
            return $"功率数据项：{ToHexString(dataItems)}";
        }

        private static string DescribeBits(byte value, int count, string oneText, string zeroText)
        {
            return string.Join("，", Enumerable.Range(0, count).Select(i => $"bit{i}:{(((value & (1 << i)) != 0) ? oneText : zeroText)}"));
        }

        private static string DescribeMeterProtocol(byte value)
        {
            return value switch
            {
                0x02 => "645-2007",
                0x03 => "698.45",
                0x09 => "支持645和698双协议",
                _ => $"未知协议类型 {ToHex(value)}"
            };
        }

        private static ushort ReadUInt16LittleEndian(byte[] data, int index)
        {
            return index + 1 < data.Length ? (ushort)(data[index] | (data[index + 1] << 8)) : (ushort)0;
        }

        private static uint ReadUInt32LittleEndian(byte[] data, int index)
        {
            if (index + 3 >= data.Length)
            {
                return 0;
            }
            return (uint)(data[index] | (data[index + 1] << 8) | (data[index + 2] << 16) | (data[index + 3] << 24));
        }

        private static ulong ReadUInt64LittleEndian(byte[] data, int index)
        {
            if (index + 7 >= data.Length)
            {
                return 0;
            }

            ulong value = 0;
            for (int i = 0; i < 8; i++)
            {
                value |= ((ulong)data[index + i]) << (8 * i);
            }

            return value;
        }

        private static int ReadInt32LittleEndian(byte[] data, int index)
        {
            if (index + 3 >= data.Length)
            {
                return 0;
            }

            return data[index] | (data[index + 1] << 8) | (data[index + 2] << 16) | (data[index + 3] << 24);
        }

        private static float ReadFloatLittleEndian(byte[] data, int index)
        {
            if (index + 3 >= data.Length)
            {
                return 0;
            }
            return BitConverter.ToSingle(data, index);
        }

        private static string ToHex(byte value)
        {
            return value.ToString("X2", CultureInfo.InvariantCulture);
        }

        private static string ToHexString(IEnumerable<byte> bytes)
        {
            return string.Join(" ", bytes.Select(ToHex));
        }

        private sealed class ParsedFrame
        {
            public int Index { get; init; }
            public byte ProtocolType { get; init; }
            public string CommandText { get; init; } = string.Empty;
            public string LengthStatus { get; init; } = string.Empty;
            public string ChecksumStatus { get; init; } = string.Empty;
            public bool IsValid { get; init; }
            public List<ProtocolFieldRow> Fields { get; init; } = new List<ProtocolFieldRow>();
            public string DetailText { get; init; } = string.Empty;

            public static ParsedFrame CreateInvalid(int index, byte[] frameBytes, string error)
            {
                return new ParsedFrame
                {
                    Index = index,
                    CommandText = "无法解析",
                    LengthStatus = "异常",
                    ChecksumStatus = "未校验",
                    IsValid = false,
                    Fields = new List<ProtocolFieldRow>
                    {
                        new("原始数据", ToHexString(frameBytes), error, "异常")
                    },
                    DetailText = $"第 {index} 帧解析失败：{error}{Environment.NewLine}原始数据：{ToHexString(frameBytes)}"
                };
            }
        }

        private sealed record CommandAnalysis(
            string Title,
            string Summary,
            string DataItemDescription,
            string DataItemDetail,
            string ProtocolDocumentText);

        private sealed record MeterCommandAnalysis(
            string Title,
            string Summary,
            string DataSummary,
            string DataDetail);

        private sealed record ProtocolCommandInfo(string Title, string Text);

        private sealed record ParseHistoryEntry(
            string ProtocolName,
            string InputText,
            string NormalizedInputText,
            List<ParsedFrame> Frames,
            List<FrameSummaryRow> FrameRows);

        private sealed record ProtocolDefinition(
            string Name,
            bool HasDataDirection,
            string FrameFormatSummary,
            string DifferenceSummary)
        {
            public static ProtocolDefinition TerminalElectricTest { get; } = new ProtocolDefinition(
                TerminalProtocolName,
                false,
                "55 + 长度2 + 地址 + 协议类型 + 命令码 + 数据项 + 校验 + AA",
                "终端检测装置电测协议没有数据方向字段，长度之后直接是地址/通道。");

            public static ProtocolDefinition MeterHardware { get; } = new ProtocolDefinition(
                MeterHardwareProtocolName,
                true,
                "55 + 长度2 + 数据方向 + 地址 + 协议类型 + 命令码 + 数据项 + 校验 + AA",
                "电表检测装置硬件通信协议V1比终端协议多1字节数据方向：0x00为PC到MCU，0x01为MCU到PC。");
        }

        private sealed class FrameSummaryRow
        {
            public int Index { get; init; }
            public string ProtocolType { get; init; } = string.Empty;
            public string Command { get; init; } = string.Empty;
            public string LengthStatus { get; init; } = string.Empty;
            public string ChecksumStatus { get; init; } = string.Empty;
            public string Status { get; init; } = string.Empty;
        }

        private sealed record ProtocolFieldRow(string Name, string Value, string Description, string Status);
    }
}
