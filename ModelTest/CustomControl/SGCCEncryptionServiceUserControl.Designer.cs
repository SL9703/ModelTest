namespace ModelTest.CustomControl
{
    partial class SGCCEncryptionServiceUserControl
    {
        private System.ComponentModel.IContainer components = null;
        private TableLayoutPanel mainLayoutPanel;
        private TableLayoutPanel topLayoutPanel;
        private Panel headerPanel;
        private TableLayoutPanel headerLayoutPanel;
        private TableLayoutPanel keyModeLayoutPanel;
        private Panel keyModeDividerPanel;
        private Label lblIp;
        private Label lblPort;
        private Label lblStatusValue;
        private Label lblInterface;
        private CheckBox cbxPublicKey;
        private CheckBox cbxPrivateKey;
        private TextBox tbxServerIp;
        private TextBox tbxServerPort;
        private Button btnLogin;
        private ComboBox cbxServerImp;
        private TextBox tbxParameters;
        private Button btnEncrypt;
        private Button btnTerminalEncryptionMenu;
        private Button btnMeterEncryptionMenu;
        private RichTextBox rtbxOutput;
        private ContextMenuStrip cmsTerminalEncryptionFunctions;
        private ContextMenuStrip cmsMeterEncryptionFunctions;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                _heartbeatTimer.Stop();
                _heartbeatTimer.Tick -= HeartbeatTimer_Tick;
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            mainLayoutPanel = new TableLayoutPanel();
            topLayoutPanel = new TableLayoutPanel();
            headerPanel = new Panel();
            headerLayoutPanel = new TableLayoutPanel();
            keyModeLayoutPanel = new TableLayoutPanel();
            keyModeDividerPanel = new Panel();
            lblIp = new Label();
            lblPort = new Label();
            lblStatusValue = new Label();
            lblInterface = new Label();
            cbxPublicKey = new CheckBox();
            cbxPrivateKey = new CheckBox();
            tbxServerIp = new TextBox();
            tbxServerPort = new TextBox();
            btnLogin = new Button();
            cbxServerImp = new ComboBox();
            tbxParameters = new TextBox();
            btnEncrypt = new Button();
            btnTerminalEncryptionMenu = new Button();
            btnMeterEncryptionMenu = new Button();
            rtbxOutput = new RichTextBox();
            cmsTerminalEncryptionFunctions = new ContextMenuStrip(components);
            cmsMeterEncryptionFunctions = new ContextMenuStrip(components);
            mainLayoutPanel.SuspendLayout();
            topLayoutPanel.SuspendLayout();
            headerPanel.SuspendLayout();
            headerLayoutPanel.SuspendLayout();
            keyModeLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayoutPanel
            // 
            mainLayoutPanel.ColumnCount = 2;
            mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 85F));
            mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
            mainLayoutPanel.Controls.Add(topLayoutPanel, 0, 0);
            mainLayoutPanel.Controls.Add(headerPanel, 1, 0);
            mainLayoutPanel.Dock = DockStyle.Fill;
            mainLayoutPanel.Location = new Point(0, 0);
            mainLayoutPanel.Name = "mainLayoutPanel";
            mainLayoutPanel.RowCount = 1;
            mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayoutPanel.Size = new Size(2220, 800);
            mainLayoutPanel.TabIndex = 0;
            // 
            // topLayoutPanel
            // 
            topLayoutPanel.ColumnCount = 1;
            topLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topLayoutPanel.Controls.Add(tbxParameters, 0, 0);
            topLayoutPanel.Controls.Add(rtbxOutput, 0, 1);
            topLayoutPanel.Dock = DockStyle.Fill;
            topLayoutPanel.Location = new Point(3, 3);
            topLayoutPanel.Margin = new Padding(3);
            topLayoutPanel.Name = "topLayoutPanel";
            topLayoutPanel.RowCount = 2;
            topLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            topLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
            topLayoutPanel.Size = new Size(1770, 794);
            topLayoutPanel.TabIndex = 0;
            // 
            // headerPanel
            // 
            headerPanel.BorderStyle = BorderStyle.FixedSingle;
            headerPanel.Controls.Add(headerLayoutPanel);
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Location = new Point(1779, 3);
            headerPanel.Margin = new Padding(3);
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new Padding(8, 16, 8, 8);
            headerPanel.Size = new Size(438, 794);
            headerPanel.TabIndex = 1;
            // 
            // headerLayoutPanel
            // 
            headerLayoutPanel.ColumnCount = 1;
            headerLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayoutPanel.Controls.Add(lblIp, 0, 0);
            headerLayoutPanel.Controls.Add(tbxServerIp, 0, 1);
            headerLayoutPanel.Controls.Add(lblPort, 0, 2);
            headerLayoutPanel.Controls.Add(tbxServerPort, 0, 3);
            headerLayoutPanel.Controls.Add(btnLogin, 0, 4);
            headerLayoutPanel.Controls.Add(lblStatusValue, 0, 5);
            headerLayoutPanel.Controls.Add(keyModeLayoutPanel, 0, 6);
            headerLayoutPanel.Controls.Add(lblInterface, 0, 7);
            headerLayoutPanel.Controls.Add(cbxServerImp, 0, 8);
            headerLayoutPanel.Controls.Add(btnEncrypt, 0, 9);
            headerLayoutPanel.Controls.Add(btnTerminalEncryptionMenu, 0, 10);
            headerLayoutPanel.Controls.Add(btnMeterEncryptionMenu, 0, 11);
            headerLayoutPanel.Controls.Add(new Panel(), 0, 12);
            headerLayoutPanel.Dock = DockStyle.Fill;
            headerLayoutPanel.Location = new Point(8, 16);
            headerLayoutPanel.Name = "headerLayoutPanel";
            headerLayoutPanel.RowCount = 13;
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
            headerLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            headerLayoutPanel.Size = new Size(420, 768);
            headerLayoutPanel.TabIndex = 0;
            // 
            // keyModeLayoutPanel
            // 
            keyModeLayoutPanel.BackColor = Color.FromArgb(28, 64, 54);
            keyModeLayoutPanel.ColumnCount = 3;
            keyModeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            keyModeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 1F));
            keyModeLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            keyModeLayoutPanel.Controls.Add(cbxPublicKey, 0, 0);
            keyModeLayoutPanel.Controls.Add(keyModeDividerPanel, 1, 0);
            keyModeLayoutPanel.Controls.Add(cbxPrivateKey, 2, 0);
            keyModeLayoutPanel.Dock = DockStyle.Fill;
            keyModeLayoutPanel.Location = new Point(3, 294);
            keyModeLayoutPanel.Margin = new Padding(3, 10, 3, 0);
            keyModeLayoutPanel.Name = "keyModeLayoutPanel";
            keyModeLayoutPanel.Padding = new Padding(1);
            keyModeLayoutPanel.RowCount = 1;
            keyModeLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            keyModeLayoutPanel.Size = new Size(414, 58);
            keyModeLayoutPanel.TabIndex = 3;
            // 
            // keyModeDividerPanel
            // 
            keyModeDividerPanel.BackColor = Color.FromArgb(28, 64, 54);
            keyModeDividerPanel.Dock = DockStyle.Fill;
            keyModeDividerPanel.Location = new Point(210, 1);
            keyModeDividerPanel.Margin = new Padding(0);
            keyModeDividerPanel.Name = "keyModeDividerPanel";
            keyModeDividerPanel.Size = new Size(1, 56);
            keyModeDividerPanel.TabIndex = 2;
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Dock = DockStyle.Fill;
            lblIp.Location = new Point(3, 0);
            lblIp.Margin = new Padding(3, 0, 3, 0);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(414, 32);
            lblIp.TabIndex = 0;
            lblIp.Text = "IP:";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Dock = DockStyle.Fill;
            lblPort.Location = new Point(3, 90);
            lblPort.Margin = new Padding(3, 10, 3, 0);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(414, 32);
            lblPort.TabIndex = 1;
            lblPort.Text = "Port:";
            // 
            // lblStatusValue
            // 
            lblStatusValue.AutoEllipsis = true;
            lblStatusValue.Dock = DockStyle.Fill;
            lblStatusValue.ForeColor = Color.FromArgb(58, 74, 67);
            lblStatusValue.Location = new Point(3, 248);
            lblStatusValue.Margin = new Padding(3, 10, 3, 0);
            lblStatusValue.Name = "lblStatusValue";
            lblStatusValue.Size = new Size(414, 46);
            lblStatusValue.TabIndex = 2;
            lblStatusValue.Text = "加密服务器连接状态：未连接";
            lblStatusValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbxPublicKey
            // 
            cbxPublicKey.Appearance = Appearance.Button;
            cbxPublicKey.AutoSize = true;
            cbxPublicKey.Dock = DockStyle.Fill;
            cbxPublicKey.FlatAppearance.BorderSize = 0;
            cbxPublicKey.FlatStyle = FlatStyle.Flat;
            cbxPublicKey.Location = new Point(1, 1);
            cbxPublicKey.Margin = new Padding(0);
            cbxPublicKey.Name = "cbxPublicKey";
            cbxPublicKey.Size = new Size(206, 56);
            cbxPublicKey.TabIndex = 3;
            cbxPublicKey.Text = "公钥";
            cbxPublicKey.TextAlign = ContentAlignment.MiddleCenter;
            cbxPublicKey.UseCompatibleTextRendering = true;
            cbxPublicKey.UseVisualStyleBackColor = true;
            cbxPublicKey.CheckedChanged += CbxPublicKey_CheckedChanged;
            // 
            // cbxPrivateKey
            // 
            cbxPrivateKey.Appearance = Appearance.Button;
            cbxPrivateKey.AutoSize = true;
            cbxPrivateKey.Checked = true;
            cbxPrivateKey.CheckState = CheckState.Checked;
            cbxPrivateKey.Dock = DockStyle.Fill;
            cbxPrivateKey.FlatAppearance.BorderSize = 0;
            cbxPrivateKey.FlatStyle = FlatStyle.Flat;
            cbxPrivateKey.Location = new Point(211, 1);
            cbxPrivateKey.Margin = new Padding(0);
            cbxPrivateKey.Name = "cbxPrivateKey";
            cbxPrivateKey.Size = new Size(206, 56);
            cbxPrivateKey.TabIndex = 4;
            cbxPrivateKey.Text = "私钥";
            cbxPrivateKey.TextAlign = ContentAlignment.MiddleCenter;
            cbxPrivateKey.UseCompatibleTextRendering = true;
            cbxPrivateKey.UseVisualStyleBackColor = true;
            cbxPrivateKey.CheckedChanged += CbxPrivateKey_CheckedChanged;
            // 
            // lblInterface
            // 
            lblInterface.AutoEllipsis = true;
            lblInterface.Dock = DockStyle.Fill;
            lblInterface.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point, 134);
            lblInterface.ForeColor = Color.FromArgb(37, 47, 41);
            lblInterface.Location = new Point(3, 362);
            lblInterface.Margin = new Padding(3, 10, 3, 0);
            lblInterface.Name = "lblInterface";
            lblInterface.Size = new Size(414, 26);
            lblInterface.TabIndex = 5;
            lblInterface.Text = "国家电网加密机接口函数";
            lblInterface.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tbxServerIp
            // 
            tbxServerIp.Dock = DockStyle.Fill;
            tbxServerIp.Location = new Point(3, 27);
            tbxServerIp.Margin = new Padding(3, 3, 3, 0);
            tbxServerIp.Name = "tbxServerIp";
            tbxServerIp.Size = new Size(414, 58);
            tbxServerIp.TabIndex = 6;
            // 
            // tbxServerPort
            // 
            tbxServerPort.Dock = DockStyle.Fill;
            tbxServerPort.Location = new Point(3, 125);
            tbxServerPort.Margin = new Padding(3, 3, 3, 0);
            tbxServerPort.Name = "tbxServerPort";
            tbxServerPort.Size = new Size(414, 58);
            tbxServerPort.TabIndex = 7;
            tbxServerPort.KeyPress += TbxServerPort_KeyPress;
            // 
            // btnLogin
            // 
            btnLogin.Dock = DockStyle.Fill;
            btnLogin.BackColor = Color.FromArgb(36, 92, 79);
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(3, 183);
            btnLogin.Margin = new Padding(3, 10, 3, 0);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(414, 58);
            btnLogin.TabIndex = 8;
            btnLogin.Text = "登录加密机";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += BtnLogin_Click;
            // 
            // cbxServerImp
            // 
            cbxServerImp.Dock = DockStyle.Fill;
            cbxServerImp.FormattingEnabled = true;
            cbxServerImp.Location = new Point(3, 404);
            cbxServerImp.Margin = new Padding(3, 10, 3, 0);
            cbxServerImp.Name = "cbxServerImp";
            cbxServerImp.Size = new Size(414, 58);
            cbxServerImp.TabIndex = 9;
            cbxServerImp.SelectedIndexChanged += CbxServerImp_SelectedIndexChanged;
            // 
            // tbxParameters
            // 
            tbxParameters.Dock = DockStyle.Fill;
            tbxParameters.Location = new Point(3, 3);
            tbxParameters.Margin = new Padding(3, 3, 3, 3);
            tbxParameters.Multiline = true;
            tbxParameters.Name = "tbxParameters";
            tbxParameters.ScrollBars = ScrollBars.Both;
            tbxParameters.Size = new Size(1764, 588);
            tbxParameters.TabIndex = 0;
            tbxParameters.WordWrap = false;
            // 
            // btnEncrypt
            // 
            btnEncrypt.Dock = DockStyle.Top;
            btnEncrypt.BackColor = Color.FromArgb(214, 226, 221);
            btnEncrypt.FlatStyle = FlatStyle.Flat;
            btnEncrypt.ForeColor = Color.FromArgb(37, 47, 41);
            btnEncrypt.Location = new Point(3, 462);
            btnEncrypt.Margin = new Padding(3, 10, 3, 0);
            btnEncrypt.Name = "btnEncrypt";
            btnEncrypt.Size = new Size(414, 58);
            btnEncrypt.TabIndex = 10;
            btnEncrypt.Text = "加密数据";
            btnEncrypt.UseVisualStyleBackColor = false;
            btnEncrypt.Click += BtnEncrypt_Click;
            // 
            // btnTerminalEncryptionMenu
            // 
            btnTerminalEncryptionMenu.Dock = DockStyle.Top;
            btnTerminalEncryptionMenu.BackColor = Color.FromArgb(232, 238, 235);
            btnTerminalEncryptionMenu.ContextMenuStrip = cmsTerminalEncryptionFunctions;
            btnTerminalEncryptionMenu.FlatStyle = FlatStyle.Flat;
            btnTerminalEncryptionMenu.ForeColor = Color.FromArgb(37, 47, 41);
            btnTerminalEncryptionMenu.Location = new Point(3, 530);
            btnTerminalEncryptionMenu.Margin = new Padding(3, 10, 3, 0);
            btnTerminalEncryptionMenu.Name = "btnTerminalEncryptionMenu";
            btnTerminalEncryptionMenu.Size = new Size(414, 58);
            btnTerminalEncryptionMenu.TabIndex = 11;
            btnTerminalEncryptionMenu.Text = "终端加密函数调用";
            btnTerminalEncryptionMenu.UseVisualStyleBackColor = false;
            btnTerminalEncryptionMenu.Click += BtnTerminalEncryptionMenu_Click;
            // 
            // btnMeterEncryptionMenu
            // 
            btnMeterEncryptionMenu.Dock = DockStyle.Top;
            btnMeterEncryptionMenu.BackColor = Color.FromArgb(232, 238, 235);
            btnMeterEncryptionMenu.ContextMenuStrip = cmsMeterEncryptionFunctions;
            btnMeterEncryptionMenu.FlatStyle = FlatStyle.Flat;
            btnMeterEncryptionMenu.ForeColor = Color.FromArgb(37, 47, 41);
            btnMeterEncryptionMenu.Location = new Point(3, 598);
            btnMeterEncryptionMenu.Margin = new Padding(3, 10, 3, 0);
            btnMeterEncryptionMenu.Name = "btnMeterEncryptionMenu";
            btnMeterEncryptionMenu.Size = new Size(414, 58);
            btnMeterEncryptionMenu.TabIndex = 12;
            btnMeterEncryptionMenu.Text = "电表加密函数调用";
            btnMeterEncryptionMenu.UseVisualStyleBackColor = false;
            btnMeterEncryptionMenu.Click += BtnMeterEncryptionMenu_Click;
            // 
            // rtbxOutput
            // 
            rtbxOutput.Dock = DockStyle.Fill;
            rtbxOutput.Location = new Point(3, 597);
            rtbxOutput.Margin = new Padding(3, 3, 3, 3);
            rtbxOutput.Name = "rtbxOutput";
            rtbxOutput.ReadOnly = true;
            rtbxOutput.Size = new Size(1764, 194);
            rtbxOutput.TabIndex = 4;
            rtbxOutput.Text = "使用接口函数参数使用,隔开，例如：01,02,03        请在上边输入框输入加密机参数";
            // 
            // SGCCEncryptionServiceUserControl
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(mainLayoutPanel);
            Name = "SGCCEncryptionServiceUserControl";
            Size = new Size(2220, 800);
            mainLayoutPanel.ResumeLayout(false);
            topLayoutPanel.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            headerLayoutPanel.ResumeLayout(false);
            headerLayoutPanel.PerformLayout();
            keyModeLayoutPanel.ResumeLayout(false);
            keyModeLayoutPanel.PerformLayout();
            ResumeLayout(false);
        }
    }
}
