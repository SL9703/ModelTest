using System.Globalization;
using System.Drawing.Drawing2D;
using ModelTest.Protocol;

namespace ModelTest;

/// <summary>
/// 通用串口服务器查看和配置窗体。
/// 该窗体只负责按钮事件、输入校验和结果展示；TCP连接、协议发送和解析由 SerialPortServerConfigService 处理。
/// </summary>
public sealed class SerialPortServerConfigForm : Form
{
    private readonly SerialPortServerConfigService serialPortServerService = new();
    private readonly TextBox tbxIp = new();
    private readonly TextBox tbxManagementPort = new();
    private readonly TextBox tbxTcpPort = new();
    private readonly ComboBox cbxBaudRate = new();
    private readonly ComboBox cbxDataBits = new();
    private readonly ComboBox cbxParity = new();
    private readonly ComboBox cbxStopBits = new();
    private readonly CheckBox chkStandardTcpBase = new();
    private readonly CheckBox chkPowerSave = new();
    private readonly CheckBox chkNoPowerSave = new();
    private readonly ModernButton btnConnect = new("连接");
    private readonly ModernButton btnView = new("查看");
    private readonly ModernButton btnSet = new("设置");
    private readonly ModernButton btnSave = new("保存");
    private readonly DataGridView gridPorts = new();
    private readonly RichTextBox rtbLog = new();

    public SerialPortServerConfigForm()
    {
        Text = "串口服务器查看和配置";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1920, 1080);
        MinimumSize = new Size(1280, 780);
        BackColor = Color.FromArgb(235, 242, 239);
        Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
        FormBorderStyle = FormBorderStyle.None;
        Padding = new Padding(14);
        DoubleBuffered = true;
        serialPortServerService.LogRequested += AppendLog;

        BuildLayout();
        BindEvents();
        ApplyRoundedRegion();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ApplyRoundedRegion();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        serialPortServerService.Dispose();
        base.OnFormClosed(e);
    }

    private void BuildLayout()
    {
        RoundedPanel root = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(18)
        };
        Controls.Add(root);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        root.Controls.Add(layout);

        Panel titlePanel = new() { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        Label title = new()
        {
            Text = "串口服务器查看和配置",
            Dock = DockStyle.Left,
            AutoSize = false,
            Width = 520,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold, GraphicsUnit.Point, 134),
            ForeColor = Color.FromArgb(34, 54, 66)
        };
        ModernButton btnClose = new("关闭")
        {
            Dock = DockStyle.Right,
            Width = 120,
            NormalColor = Color.FromArgb(104, 119, 133),
            HoverColor = Color.FromArgb(82, 96, 110)
        };
        btnClose.Click += (_, _) => Close();
        titlePanel.Controls.Add(title);
        titlePanel.Controls.Add(btnClose);
        layout.Controls.Add(titlePanel, 0, 0);

        RoundedPanel configPanel = new()
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(246, 250, 248),
            Padding = new Padding(14)
        };
        layout.Controls.Add(configPanel, 0, 1);

        TableLayoutPanel configLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 8,
            RowCount = 4
        };
        for (int i = 0; i < 8; i++)
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
        configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        configLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        configPanel.Controls.Add(configLayout);

        AddLabel(configLayout, "管理IP", 0, 0);
        AddLabel(configLayout, "管理端口", 2, 0);
        AddLabel(configLayout, "目标TCP端口", 4, 0);
        AddLabel(configLayout, "串口参数", 6, 0);

        tbxIp.Text = "192.168.127.101";
        tbxManagementPort.Text = "64444";
        tbxTcpPort.Text = "951";
        chkStandardTcpBase.Text = "4001起始";
        chkStandardTcpBase.AutoSize = true;
        chkStandardTcpBase.Checked = false;
        chkStandardTcpBase.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 134);
        cbxBaudRate.Items.AddRange(new object[] { "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" });
        cbxBaudRate.Text = "9600";
        cbxDataBits.Items.AddRange(new object[] { "7", "8" });
        cbxDataBits.Text = "8";
        cbxParity.Items.AddRange(new object[] { "N", "E", "O" });
        cbxParity.Text = "E";
        cbxStopBits.Items.AddRange(new object[] { "1", "2" });
        cbxStopBits.Text = "1";

        AddControl(configLayout, tbxIp, 0, 1, 2);
        AddControl(configLayout, tbxManagementPort, 2, 1, 2);
        AddControl(configLayout, tbxTcpPort, 4, 1, 2);
        configLayout.Controls.Add(chkStandardTcpBase, 4, 2);
        configLayout.SetColumnSpan(chkStandardTcpBase, 2);

        FlowLayoutPanel profilePanel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0)
        };
        profilePanel.Controls.AddRange(new Control[] { cbxBaudRate, cbxDataBits, cbxParity, cbxStopBits });
        foreach (Control control in profilePanel.Controls)
        {
            control.Width = 76;
            control.Height = 42;
            control.Margin = new Padding(0, 0, 12, 0);
        }
        configLayout.Controls.Add(profilePanel, 6, 1);
        configLayout.SetColumnSpan(profilePanel, 2);

        chkPowerSave.Text = "断电保存";
        chkNoPowerSave.Text = "不断电保存";
        chkNoPowerSave.Checked = true;
        chkPowerSave.AutoSize = true;
        chkNoPowerSave.AutoSize = true;
        chkPowerSave.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 134);
        chkNoPowerSave.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold, GraphicsUnit.Point, 134);
        FlowLayoutPanel savePanel = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        savePanel.Controls.Add(chkNoPowerSave);
        savePanel.Controls.Add(chkPowerSave);
        configLayout.Controls.Add(savePanel, 0, 3);
        configLayout.SetColumnSpan(savePanel, 2);

        btnConnect.NormalColor = Color.FromArgb(36, 137, 95);
        btnView.NormalColor = Color.FromArgb(53, 126, 184);
        btnSet.NormalColor = Color.FromArgb(239, 149, 42);
        btnSave.NormalColor = Color.FromArgb(93, 92, 166);
        AddControl(configLayout, btnConnect, 2, 3);
        AddControl(configLayout, btnView, 3, 3);
        AddControl(configLayout, btnSet, 4, 3);
        AddControl(configLayout, btnSave, 5, 3);

        ConfigureGrid();
        layout.Controls.Add(gridPorts, 0, 2);

        rtbLog.Dock = DockStyle.Fill;
        rtbLog.BorderStyle = BorderStyle.None;
        rtbLog.BackColor = Color.FromArgb(248, 250, 252);
        rtbLog.Font = new Font("Consolas", 11F, FontStyle.Regular, GraphicsUnit.Point);
        rtbLog.ReadOnly = true;
        layout.Controls.Add(rtbLog, 0, 3);
    }

    private void BindEvents()
    {
        btnConnect.Click += async (_, _) => await RunButtonActionAsync(btnConnect, ConnectButton_ClickAsync);
        btnView.Click += async (_, _) => await RunButtonActionAsync(btnView, ViewButton_ClickAsync);
        btnSet.Click += async (_, _) => await RunButtonActionAsync(btnSet, SetButton_ClickAsync);
        btnSave.Click += async (_, _) => await RunButtonActionAsync(btnSave, SaveButton_ClickAsync);
        chkPowerSave.CheckedChanged += (_, _) =>
        {
            if (chkPowerSave.Checked)
                chkNoPowerSave.Checked = false;
            else if (!chkNoPowerSave.Checked)
                chkNoPowerSave.Checked = true;
        };
        chkNoPowerSave.CheckedChanged += (_, _) =>
        {
            if (chkNoPowerSave.Checked)
                chkPowerSave.Checked = false;
            else if (!chkPowerSave.Checked)
                chkPowerSave.Checked = true;
        };
        chkStandardTcpBase.CheckedChanged += (_, _) =>
        {
            tbxTcpPort.Text = chkStandardTcpBase.Checked
                ? GenericSerialPortServerProtocol.FirstStandardTcpPort.ToString(CultureInfo.InvariantCulture)
                : GenericSerialPortServerProtocol.FirstLegacyTcpPort.ToString(CultureInfo.InvariantCulture);
            RefreshGridTcpPortBase();
        };
    }

    /// <summary>
    /// 统一保护窗体按钮事件，避免串口服务器连接或协议异常冒泡影响主窗体。
    /// </summary>
    private async Task RunButtonActionAsync(Control triggerButton, Func<Task> action)
    {
        try
        {
            triggerButton.Enabled = false;
            await action();
        }
        catch (Exception ex)
        {
            AppendLog($"操作异常：{ex.Message}");
            LogMessage.Error(ex);
        }
        finally
        {
            if (!IsDisposed)
                triggerButton.Enabled = true;
        }
    }

    /// <summary>连接/断开按钮：只处理输入校验和按钮文字，连接细节交给服务。</summary>
    private async Task ConnectButton_ClickAsync()
    {
        if (serialPortServerService.IsConnected)
        {
            serialPortServerService.Disconnect();
            btnConnect.Text = "连接";
            return;
        }

        if (!TryReadEndpoint(out string ip, out int port))
            return;

        if (await serialPortServerService.ConnectAsync(ip, port))
        {
            btnConnect.Text = "断开";
        }
    }

    /// <summary>查看按钮：调用服务读取端口参数，并刷新表格。</summary>
    private async Task ViewButton_ClickAsync()
    {
        if (!await EnsureConnectedAsync())
            return;

        IReadOnlyList<GenericSerialPortChannelInfo> channels = await serialPortServerService.ViewPortsAsync();
        if (channels.Count == 0)
            return;

        gridPorts.Rows.Clear();
        foreach (GenericSerialPortChannelInfo channel in channels)
        {
            string parity = GenericSerialPortServerProtocol.FormatParity(channel.Parity);
            int tcpPort = GetDisplayTcpPort(channel.ChannelIndex);
            gridPorts.Rows.Add(
                channel.ChannelIndex + 1,
                $"0x{channel.ChannelIndex:X2}",
                tcpPort,
                channel.BaudRate == 0 ? "未知" : channel.BaudRate.ToString(CultureInfo.InvariantCulture),
                channel.DataBits,
                parity,
                channel.StopBits,
                $"{channel.BaudRateCode:X2} {channel.DataBitsCode:X2} {channel.StopBitsCode:X2} {channel.ParityCode:X2}");
        }
    }

    /// <summary>按当前勾选的TCP起始端口，把通道00-0F换算成951-966或4001-4016。</summary>
    private int GetDisplayTcpPort(int channelIndex)
    {
        int firstTcpPort = chkStandardTcpBase.Checked
            ? GenericSerialPortServerProtocol.FirstStandardTcpPort
            : GenericSerialPortServerProtocol.FirstLegacyTcpPort;
        return GenericSerialPortServerProtocol.GetTcpPortFromChannelIndex(channelIndex, firstTcpPort);
    }

    /// <summary>切换951/4001起始端口时，同步刷新已读取出来的表格TCP端口列。</summary>
    private void RefreshGridTcpPortBase()
    {
        foreach (DataGridViewRow row in gridPorts.Rows)
        {
            if (row.IsNewRow)
                continue;
            if (int.TryParse(Convert.ToString(row.Cells["ComNo"].Value, CultureInfo.InvariantCulture), out int comNo))
                row.Cells["TcpPort"].Value = GetDisplayTcpPort(comNo - 1);
        }
    }

    /// <summary>设置按钮：读取目标端口和串口参数，具体解锁、设置命令由服务发送。</summary>
    private async Task SetButton_ClickAsync()
    {
        if (!await EnsureConnectedAsync() || !TryReadTargetProfile(out int tcpPort, out string profile))
            return;

        if (await serialPortServerService.SetPortAsync(tcpPort, profile))
            AppendLog(chkPowerSave.Checked
                ? "端口参数已设置为立即生效；如需断电保存，请点击“保存”。"
                : "端口参数已设置为立即生效；当前选择不断电保存。");
    }

    /// <summary>保存按钮：只有勾选断电保存时才发送保存指令。</summary>
    private async Task SaveButton_ClickAsync()
    {
        if (!await EnsureConnectedAsync())
            return;

        if (!chkPowerSave.Checked)
        {
            AppendLog("当前选择“不断电保存”，未发送保存指令。");
            return;
        }

        await serialPortServerService.SaveAsync();
    }

    private async Task<bool> EnsureConnectedAsync()
    {
        if (serialPortServerService.IsConnected)
            return true;

        await ConnectButton_ClickAsync();
        return serialPortServerService.IsConnected;
    }

    private bool TryReadEndpoint(out string ip, out int port)
    {
        ip = tbxIp.Text.Trim();
        if (string.IsNullOrWhiteSpace(ip))
        {
            AppendLog("管理IP不能为空。");
            port = 0;
            return false;
        }

        if (!int.TryParse(tbxManagementPort.Text.Trim(), out port) || port is <= 0 or > 65535)
        {
            AppendLog("管理端口必须是1-65535。");
            return false;
        }

        return true;
    }

    private bool TryReadTargetProfile(out int tcpPort, out string profile)
    {
        profile = $"{cbxBaudRate.Text.Trim()}-{cbxDataBits.Text.Trim()}-{cbxParity.Text.Trim()}-{cbxStopBits.Text.Trim()}";
        if (!int.TryParse(tbxTcpPort.Text.Trim(), out tcpPort))
        {
            AppendLog("目标TCP端口无效。");
            return false;
        }

        if (!GenericSerialPortServerProtocol.TryParseSerialProfile(profile, out _, out _, out _, out _, out string error))
        {
            AppendLog(error);
            return false;
        }

        return true;
    }

    private void ConfigureGrid()
    {
        gridPorts.Dock = DockStyle.Fill;
        gridPorts.BorderStyle = BorderStyle.None;
        gridPorts.BackgroundColor = Color.White;
        gridPorts.AllowUserToAddRows = false;
        gridPorts.AllowUserToDeleteRows = false;
        gridPorts.AllowUserToResizeRows = false;
        gridPorts.AllowUserToResizeColumns = false;
        gridPorts.RowHeadersVisible = false;
        gridPorts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridPorts.ColumnHeadersHeight = 38;
        gridPorts.RowTemplate.Height = 34;
        gridPorts.EnableHeadersVisualStyles = false;
        gridPorts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(46, 83, 103);
        gridPorts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        gridPorts.ColumnHeadersDefaultCellStyle.Font = new Font(Font, FontStyle.Bold);
        gridPorts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(217, 235, 228);
        gridPorts.DefaultCellStyle.SelectionForeColor = Color.FromArgb(24, 44, 56);
        gridPorts.Columns.Add("ComNo", "COM");
        gridPorts.Columns.Add("Channel", "通道");
        gridPorts.Columns.Add("TcpPort", "TCP端口");
        gridPorts.Columns.Add("BaudRate", "波特率");
        gridPorts.Columns.Add("DataBits", "数据位");
        gridPorts.Columns.Add("Parity", "校验");
        gridPorts.Columns.Add("StopBits", "停止位");
        gridPorts.Columns.Add("Raw", "原始");
        gridPorts.Columns["ComNo"].Width = 100;
        gridPorts.Columns["Channel"].Width = 100;
        gridPorts.Columns["TcpPort"].Width = 150;
        gridPorts.Columns["BaudRate"].Width = 150;
        gridPorts.Columns["DataBits"].Width = 100;
        gridPorts.Columns["Parity"].Width = 100;
        gridPorts.Columns["StopBits"].Width = 100;
        gridPorts.Columns["Raw"].Width = 180;
        foreach (DataGridViewColumn column in gridPorts.Columns)
        {
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.Resizable = DataGridViewTriState.False;
        }
    }

    private void AppendLog(string message)
    {
        rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        rtbLog.SelectionStart = rtbLog.TextLength;
        rtbLog.ScrollToCaret();
    }

    private static void AddLabel(TableLayoutPanel layout, string text, int column, int row)
    {
        Label label = new()
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(53, 75, 86),
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 134)
        };
        layout.Controls.Add(label, column, row);
        layout.SetColumnSpan(label, 2);
    }

    private static void AddControl(TableLayoutPanel layout, Control control, int column, int row, int columnSpan = 1)
    {
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(4);
        layout.Controls.Add(control, column, row);
        if (columnSpan > 1)
            layout.SetColumnSpan(control, columnSpan);
    }

    private void ApplyRoundedRegion()
    {
        using GraphicsPath path = RoundedPanel.CreateRoundPath(ClientRectangle, 18);
        Region = new Region(path);
    }

    private sealed class RoundedPanel : Panel
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using GraphicsPath path = CreateRoundPath(ClientRectangle, 14);
            Region = new Region(path);
        }

        public static GraphicsPath CreateRoundPath(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new();
            if (rectangle.Width <= 0 || rectangle.Height <= 0)
                return path;

            int diameter = radius * 2;
            Rectangle arc = new(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.X;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    private sealed class ModernButton : Button
    {
        public Color NormalColor { get; set; } = Color.FromArgb(42, 135, 104);
        public Color HoverColor { get; set; } = Color.FromArgb(35, 113, 88);
        private bool isHover;

        public ModernButton(string text)
        {
            Text = text;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            ForeColor = Color.White;
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 134);
            Cursor = Cursors.Hand;
            Height = 40;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            isHover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            isHover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            pevent.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using GraphicsPath path = RoundedPanel.CreateRoundPath(ClientRectangle, 10);
            using SolidBrush brush = new(isHover ? HoverColor : NormalColor);
            pevent.Graphics.FillPath(brush, path);
            TextRenderer.DrawText(
                pevent.Graphics,
                Text,
                Font,
                ClientRectangle,
                ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }
}
