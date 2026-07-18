namespace ModelTest.CustomControl
{
    partial class MultifunctionalcommunicationUserControl
    {
        private readonly Color _panelBackColor = Color.FromArgb(88, 149, 127);

        private TableLayoutPanel rootLayout = null!;
        private TableLayoutPanel dataLayout = null!;
        private TableLayoutPanel sendLayout = null!;
        private FlowLayoutPanel sendButtonPanel = null!;
        private GroupBox groupBoxReceive = null!;
        private GroupBox groupBoxSend = null!;
        private ComboBox cbxSocketClass = null!;
        private ComboBox cbxIp = null!;
        private ComboBox cbxPort = null!;
        private Button btnConnect = null!;
        private Label lblStatusValue = null!;
        private CheckBox cbxRevcASCII = null!;
        private CheckBox cbxRevcHEX = null!;
        private CheckBox cbxSendASCII = null!;
        private CheckBox cbxSendHEX = null!;
        private ComboBox cbxClientConnc = null!;
        private CheckBox cbxIsBroadcastMessage = null!;
        private RichTextBox rtbxSendData = null!;
        private Button btnSendData = null!;
        private Button btnClearSend = null!;
        private Button btnClearReceive = null!;
        private RichTextBox rtbxRevcData = null!;

        /// <summary>
        /// 初始化多功能通信控件的界面布局；业务逻辑保留在 .cs 文件中。
        /// </summary>
        private void InitializeComponent()
        {
            rootLayout = new TableLayoutPanel();
            dataLayout = new TableLayoutPanel();
            groupBoxReceive = new GroupBox();
            rtbxRevcData = new RichTextBox();
            groupBoxSend = new GroupBox();
            sendLayout = new TableLayoutPanel();
            rtbxSendData = new RichTextBox();
            sendButtonPanel = new FlowLayoutPanel();
            btnSendData = new Button();
            btnClearSend = new Button();
            btnClearReceive = new Button();
            cbxSocketClass = new ComboBox();
            cbxIp = new ComboBox();
            cbxPort = new ComboBox();
            btnConnect = new Button();
            lblStatusValue = new Label();
            cbxRevcASCII = new CheckBox();
            cbxRevcHEX = new CheckBox();
            cbxSendASCII = new CheckBox();
            cbxSendHEX = new CheckBox();
            cbxClientConnc = new ComboBox();
            cbxIsBroadcastMessage = new CheckBox();

            SuspendLayout();
            rootLayout.SuspendLayout();
            dataLayout.SuspendLayout();
            groupBoxReceive.SuspendLayout();
            groupBoxSend.SuspendLayout();
            sendLayout.SuspendLayout();
            sendButtonPanel.SuspendLayout();

            BackColor = _panelBackColor;
            Dock = DockStyle.Fill;
            Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            Name = "MultifunctionalcommunicationUserControl";
            Size = new Size(1200, 720);

            rootLayout.BackColor = _panelBackColor;
            rootLayout.ColumnCount = 2;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 85F));
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new Padding(0);
            rootLayout.RowCount = 1;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(1200, 720);
            rootLayout.TabIndex = 0;

            dataLayout.BackColor = _panelBackColor;
            dataLayout.ColumnCount = 1;
            dataLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dataLayout.Dock = DockStyle.Fill;
            dataLayout.Location = new Point(3, 3);
            dataLayout.Margin = new Padding(3);
            dataLayout.Name = "dataLayout";
            dataLayout.RowCount = 2;
            dataLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            dataLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
            dataLayout.Size = new Size(1004, 704);
            dataLayout.TabIndex = 0;

            groupBoxReceive.BackColor = _panelBackColor;
            groupBoxReceive.Dock = DockStyle.Fill;
            groupBoxReceive.Location = new Point(3, 3);
            groupBoxReceive.Name = "groupBoxReceive";
            groupBoxReceive.Padding = new Padding(8);
            groupBoxReceive.Size = new Size(998, 402);
            groupBoxReceive.TabIndex = 0;
            groupBoxReceive.TabStop = false;
            groupBoxReceive.Text = "接收数据";

            rtbxRevcData.BackColor = Color.White;
            rtbxRevcData.Dock = DockStyle.Fill;
            rtbxRevcData.Location = new Point(8, 27);
            rtbxRevcData.Name = "rtbxRevcData";
            rtbxRevcData.ReadOnly = true;
            rtbxRevcData.Size = new Size(982, 367);
            rtbxRevcData.TabIndex = 0;
            rtbxRevcData.Text = "";
            rtbxRevcData.WordWrap = false;
            groupBoxReceive.Controls.Add(rtbxRevcData);

            groupBoxSend.BackColor = _panelBackColor;
            groupBoxSend.Dock = DockStyle.Fill;
            groupBoxSend.Location = new Point(3, 411);
            groupBoxSend.Name = "groupBoxSend";
            groupBoxSend.Padding = new Padding(8);
            groupBoxSend.Size = new Size(998, 290);
            groupBoxSend.TabIndex = 1;
            groupBoxSend.TabStop = false;
            groupBoxSend.Text = "发送数据";

            sendLayout.BackColor = _panelBackColor;
            sendLayout.ColumnCount = 1;
            sendLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            sendLayout.Dock = DockStyle.Fill;
            sendLayout.Location = new Point(8, 27);
            sendLayout.Name = "sendLayout";
            sendLayout.Padding = new Padding(6);
            sendLayout.RowCount = 2;
            sendLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            sendLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            sendLayout.Size = new Size(982, 255);
            sendLayout.TabIndex = 0;

            rtbxSendData.Dock = DockStyle.Fill;
            rtbxSendData.Location = new Point(9, 9);
            rtbxSendData.Name = "rtbxSendData";
            rtbxSendData.Size = new Size(964, 191);
            rtbxSendData.TabIndex = 0;
            rtbxSendData.Text = "xichengkeji";
            rtbxSendData.WordWrap = false;

            sendButtonPanel.BackColor = _panelBackColor;
            sendButtonPanel.Dock = DockStyle.Fill;
            sendButtonPanel.FlowDirection = FlowDirection.RightToLeft;
            sendButtonPanel.Location = new Point(9, 206);
            sendButtonPanel.Name = "sendButtonPanel";
            sendButtonPanel.Size = new Size(964, 50);
            sendButtonPanel.TabIndex = 1;
            sendButtonPanel.WrapContents = false;

            btnSendData.Location = new Point(861, 3);
            btnSendData.Name = "btnSendData";
            btnSendData.TabIndex = 0;
            btnSendData.Text = "发送";
            ConfigureActionButton(btnSendData);

            btnClearSend.Location = new Point(755, 3);
            btnClearSend.Name = "btnClearSend";
            btnClearSend.TabIndex = 1;
            btnClearSend.Text = "清空发送";
            ConfigureActionButton(btnClearSend);

            btnClearReceive.Location = new Point(649, 3);
            btnClearReceive.Name = "btnClearReceive";
            btnClearReceive.TabIndex = 2;
            btnClearReceive.Text = "清空接收";
            ConfigureActionButton(btnClearReceive);

            sendButtonPanel.Controls.Add(btnSendData);
            sendButtonPanel.Controls.Add(btnClearSend);
            sendButtonPanel.Controls.Add(btnClearReceive);
            sendLayout.Controls.Add(rtbxSendData, 0, 0);
            sendLayout.Controls.Add(sendButtonPanel, 0, 1);
            groupBoxSend.Controls.Add(sendLayout);

            dataLayout.Controls.Add(groupBoxReceive, 0, 0);
            dataLayout.Controls.Add(groupBoxSend, 0, 1);

            rootLayout.Controls.Add(dataLayout, 0, 0);
            rootLayout.Controls.Add(CreateControlPanelContainer(), 1, 0);
            Controls.Add(rootLayout);
            InitializeDefaultControlData();

            sendButtonPanel.ResumeLayout(false);
            sendLayout.ResumeLayout(false);
            groupBoxSend.ResumeLayout(false);
            groupBoxReceive.ResumeLayout(false);
            dataLayout.ResumeLayout(false);
            rootLayout.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Control CreateControlPanelContainer()
        {
            var container = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = _panelBackColor,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(3),
                Padding = new Padding(8, 16, 8, 8),
                Name = "controlPanelContainer",
            };

            container.Controls.Add(CreateControlPanel());
            return container;
        }

        private Control CreateControlPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = _panelBackColor,
                ColumnCount = 1,
                RowCount = 16,
                Padding = new Padding(0),
                Name = "controlPanel",
                AutoScroll = true
            };

            foreach (var height in new float[]
            {
                32F, 58F,
                32F, 58F,
                32F, 58F,
                58F,
                56F,
                36F, 58F,
                36F, 58F,
                36F, 58F,
                58F,
                100F
            })
            {
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
            }

            panel.Controls.Add(CreateLabel("协议类型"), 0, 0);
            panel.Controls.Add(cbxSocketClass, 0, 1);

            panel.Controls.Add(CreateLabel("IP"), 0, 2);
            panel.Controls.Add(cbxIp, 0, 3);

            panel.Controls.Add(CreateLabel("Port"), 0, 4);
            panel.Controls.Add(cbxPort, 0, 5);

            btnConnect.Dock = DockStyle.Fill;
            btnConnect.Text = "连接TCP服务器";
            btnConnect.BackColor = Color.FromArgb(36, 92, 79);
            btnConnect.FlatStyle = FlatStyle.Flat;
            btnConnect.ForeColor = Color.White;
            btnConnect.Margin = new Padding(3, 10, 3, 0);
            btnConnect.UseVisualStyleBackColor = false;
            panel.Controls.Add(btnConnect, 0, 6);

            lblStatusValue.Dock = DockStyle.Fill;
            lblStatusValue.Text = "通信连接状态：未连接";
            lblStatusValue.ForeColor = Color.FromArgb(58, 74, 67);
            lblStatusValue.AutoEllipsis = true;
            lblStatusValue.TextAlign = ContentAlignment.MiddleLeft;
            lblStatusValue.Margin = new Padding(3, 10, 3, 0);
            panel.Controls.Add(lblStatusValue, 0, 7);

            panel.Controls.Add(CreateLabel("接收格式"), 0, 8);
            panel.Controls.Add(CreateFormatPanel(cbxRevcASCII, cbxRevcHEX), 0, 9);

            panel.Controls.Add(CreateLabel("发送格式"), 0, 10);
            panel.Controls.Add(CreateFormatPanel(cbxSendASCII, cbxSendHEX), 0, 11);

            panel.Controls.Add(CreateLabel("服务端客户端"), 0, 12);
            panel.Controls.Add(cbxClientConnc, 0, 13);

            cbxIsBroadcastMessage.Dock = DockStyle.Fill;
            cbxIsBroadcastMessage.Text = "广播消息";
            cbxIsBroadcastMessage.AutoSize = false;
            cbxIsBroadcastMessage.Margin = new Padding(3, 10, 3, 0);
            panel.Controls.Add(cbxIsBroadcastMessage, 0, 14);

            return panel;
        }

        private Label CreateLabel(string text)
        {
            return new Label
            {
                Dock = DockStyle.Fill,
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.FromArgb(37, 47, 41),
                AutoSize = false,
                Margin = new Padding(3, 10, 3, 0),
            };
        }

        private static void ConfigureActionButton(Button button)
        {
            button.BackColor = Color.FromArgb(232, 238, 235);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(37, 47, 41);
            button.ForeColor = Color.FromArgb(37, 47, 41);
            button.Margin = new Padding(6, 6, 0, 6);
            button.Size = new Size(116, 38);
            button.UseVisualStyleBackColor = false;
        }

        private Control CreateFormatPanel(CheckBox ascii, CheckBox hex)
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.FromArgb(28, 64, 54),
                Margin = new Padding(3, 10, 3, 0),
                Padding = new Padding(1),
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            ConfigureCheckBox(ascii, "ASCII");
            ConfigureCheckBox(hex, "HEX");
            panel.Controls.Add(ascii, 0, 0);
            panel.Controls.Add(hex, 1, 0);
            return panel;
        }

        private void ConfigureCheckBox(CheckBox checkBox, string text)
        {
            checkBox.Dock = DockStyle.Fill;
            checkBox.Text = text;
            checkBox.AutoSize = false;
            checkBox.TextAlign = ContentAlignment.MiddleCenter;
            checkBox.Appearance = Appearance.Button;
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.FlatAppearance.BorderSize = 0;
            checkBox.ForeColor = Color.FromArgb(37, 47, 41);
            checkBox.Margin = new Padding(0);
            checkBox.UseVisualStyleBackColor = false;
            checkBox.MouseEnter += SegmentCheckBox_MouseEnter;
            checkBox.MouseLeave += SegmentCheckBox_MouseLeave;
        }

        /// <summary>
        /// 设置控件默认选项和初始展示值。
        /// </summary>
        private void InitializeDefaultControlData()
        {
            cbxSocketClass.Dock = DockStyle.Fill;
            cbxSocketClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxSocketClass.Margin = new Padding(3, 3, 3, 0);
            cbxSocketClass.Items.AddRange(new object[] { "TCPClient", "TCPServer" });
            cbxSocketClass.SelectedIndex = 0;

            cbxIp.Dock = DockStyle.Fill;
            cbxIp.Margin = new Padding(3, 3, 3, 0);
            cbxIp.Items.AddRange(GetLocalIPv4Addresses());
            if (cbxIp.Items.Count == 0)
            {
                cbxIp.Items.Add("127.0.0.1");
            }
            cbxIp.Text = "127.0.0.1";

            cbxPort.Dock = DockStyle.Fill;
            cbxPort.Margin = new Padding(3, 3, 3, 0);
            cbxPort.Items.AddRange(new object[] { "4001", "5000", "8001" });
            cbxPort.Text = "4001";

            cbxClientConnc.Dock = DockStyle.Fill;
            cbxClientConnc.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxClientConnc.Margin = new Padding(3, 10, 3, 0);

            cbxRevcHEX.Checked = true;
            cbxSendHEX.Checked = true;
        }
    }
}
