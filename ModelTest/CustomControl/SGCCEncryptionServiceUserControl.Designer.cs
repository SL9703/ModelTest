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
            tbxParameters = new TextBox();
            rtbxOutput = new RichTextBox();
            headerPanel = new Panel();
            headerLayoutPanel = new TableLayoutPanel();
            lblIp = new Label();
            tbxServerIp = new TextBox();
            lblPort = new Label();
            tbxServerPort = new TextBox();
            btnLogin = new Button();
            lblStatusValue = new Label();
            keyModeLayoutPanel = new TableLayoutPanel();
            cbxPublicKey = new CheckBox();
            keyModeDividerPanel = new Panel();
            cbxPrivateKey = new CheckBox();
            lblInterface = new Label();
            cbxServerImp = new ComboBox();
            btnEncrypt = new Button();
            btnTerminalEncryptionMenu = new Button();
            cmsTerminalEncryptionFunctions = new ContextMenuStrip(components);
            btnMeterEncryptionMenu = new Button();
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
            topLayoutPanel.Name = "topLayoutPanel";
            topLayoutPanel.RowCount = 2;
            topLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            topLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 200F));
            topLayoutPanel.Size = new Size(1881, 794);
            topLayoutPanel.TabIndex = 0;
            // 
            // tbxParameters
            // 
            tbxParameters.Dock = DockStyle.Fill;
            tbxParameters.Location = new Point(3, 3);
            tbxParameters.Multiline = true;
            tbxParameters.Name = "tbxParameters";
            tbxParameters.ScrollBars = ScrollBars.Both;
            tbxParameters.Size = new Size(1875, 588);
            tbxParameters.TabIndex = 0;
            tbxParameters.WordWrap = false;
            // 
            // rtbxOutput
            // 
            rtbxOutput.Dock = DockStyle.Fill;
            rtbxOutput.Location = new Point(3, 597);
            rtbxOutput.Name = "rtbxOutput";
            rtbxOutput.ReadOnly = true;
            rtbxOutput.Size = new Size(1875, 194);
            rtbxOutput.TabIndex = 4;
            rtbxOutput.Text = "使用接口函数参数使用,隔开，例如：01,02,03        请在上边输入框输入加密机参数";
            // 
            // headerPanel
            // 
            headerPanel.BorderStyle = BorderStyle.FixedSingle;
            headerPanel.Controls.Add(headerLayoutPanel);
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Location = new Point(1890, 3);
            headerPanel.Name = "headerPanel";
            headerPanel.Padding = new Padding(8, 16, 8, 8);
            headerPanel.Size = new Size(327, 794);
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
            headerLayoutPanel.Size = new Size(309, 768);
            headerLayoutPanel.TabIndex = 0;
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Dock = DockStyle.Fill;
            lblIp.Location = new Point(3, 0);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(303, 32);
            lblIp.TabIndex = 0;
            lblIp.Text = "IP:";
            // 
            // tbxServerIp
            // 
            tbxServerIp.Dock = DockStyle.Fill;
            tbxServerIp.Location = new Point(3, 35);
            tbxServerIp.Margin = new Padding(3, 3, 3, 0);
            tbxServerIp.Name = "tbxServerIp";
            tbxServerIp.Size = new Size(303, 34);
            tbxServerIp.TabIndex = 6;
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Dock = DockStyle.Fill;
            lblPort.Location = new Point(3, 100);
            lblPort.Margin = new Padding(3, 10, 3, 0);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(303, 22);
            lblPort.TabIndex = 1;
            lblPort.Text = "Port:";
            // 
            // tbxServerPort
            // 
            tbxServerPort.Dock = DockStyle.Fill;
            tbxServerPort.Location = new Point(3, 125);
            tbxServerPort.Margin = new Padding(3, 3, 3, 0);
            tbxServerPort.Name = "tbxServerPort";
            tbxServerPort.Size = new Size(303, 34);
            tbxServerPort.TabIndex = 7;
            tbxServerPort.KeyPress += TbxServerPort_KeyPress;
            // 
            // btnLogin
            // 
            btnLogin.BackColor = Color.FromArgb(36, 92, 79);
            btnLogin.Dock = DockStyle.Fill;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.ForeColor = Color.White;
            btnLogin.Location = new Point(3, 190);
            btnLogin.Margin = new Padding(3, 10, 3, 0);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(303, 48);
            btnLogin.TabIndex = 8;
            btnLogin.Text = "登录加密机";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += BtnLogin_Click;
            // 
            // lblStatusValue
            // 
            lblStatusValue.AutoEllipsis = true;
            lblStatusValue.Dock = DockStyle.Fill;
            lblStatusValue.ForeColor = Color.FromArgb(58, 74, 67);
            lblStatusValue.Location = new Point(3, 248);
            lblStatusValue.Margin = new Padding(3, 10, 3, 0);
            lblStatusValue.Name = "lblStatusValue";
            lblStatusValue.Size = new Size(303, 46);
            lblStatusValue.TabIndex = 2;
            lblStatusValue.Text = "加密服务器连接状态：未连接";
            lblStatusValue.TextAlign = ContentAlignment.MiddleLeft;
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
            keyModeLayoutPanel.Location = new Point(3, 304);
            keyModeLayoutPanel.Margin = new Padding(3, 10, 3, 0);
            keyModeLayoutPanel.Name = "keyModeLayoutPanel";
            keyModeLayoutPanel.Padding = new Padding(1);
            keyModeLayoutPanel.RowCount = 1;
            keyModeLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            keyModeLayoutPanel.Size = new Size(303, 48);
            keyModeLayoutPanel.TabIndex = 3;
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
            cbxPublicKey.Size = new Size(150, 46);
            cbxPublicKey.TabIndex = 3;
            cbxPublicKey.Text = "公钥";
            cbxPublicKey.TextAlign = ContentAlignment.MiddleCenter;
            cbxPublicKey.UseCompatibleTextRendering = true;
            cbxPublicKey.UseVisualStyleBackColor = true;
            cbxPublicKey.CheckedChanged += CbxPublicKey_CheckedChanged;
            // 
            // keyModeDividerPanel
            // 
            keyModeDividerPanel.BackColor = Color.FromArgb(28, 64, 54);
            keyModeDividerPanel.Dock = DockStyle.Fill;
            keyModeDividerPanel.Location = new Point(151, 1);
            keyModeDividerPanel.Margin = new Padding(0);
            keyModeDividerPanel.Name = "keyModeDividerPanel";
            keyModeDividerPanel.Size = new Size(1, 46);
            keyModeDividerPanel.TabIndex = 2;
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
            cbxPrivateKey.Location = new Point(152, 1);
            cbxPrivateKey.Margin = new Padding(0);
            cbxPrivateKey.Name = "cbxPrivateKey";
            cbxPrivateKey.Size = new Size(150, 46);
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
            lblInterface.Size = new Size(303, 26);
            lblInterface.TabIndex = 5;
            lblInterface.Text = "国家电网加密机接口函数";
            lblInterface.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cbxServerImp
            // 
            cbxServerImp.Dock = DockStyle.Fill;
            cbxServerImp.FormattingEnabled = true;
            cbxServerImp.Location = new Point(3, 398);
            cbxServerImp.Margin = new Padding(3, 10, 3, 0);
            cbxServerImp.Name = "cbxServerImp";
            cbxServerImp.Size = new Size(303, 36);
            cbxServerImp.TabIndex = 9;
            cbxServerImp.SelectedIndexChanged += CbxServerImp_SelectedIndexChanged;
            // 
            // btnEncrypt
            // 
            btnEncrypt.BackColor = Color.FromArgb(214, 226, 221);
            btnEncrypt.Dock = DockStyle.Top;
            btnEncrypt.FlatStyle = FlatStyle.Flat;
            btnEncrypt.ForeColor = Color.FromArgb(37, 47, 41);
            btnEncrypt.Location = new Point(3, 456);
            btnEncrypt.Margin = new Padding(3, 10, 3, 0);
            btnEncrypt.Name = "btnEncrypt";
            btnEncrypt.Size = new Size(303, 48);
            btnEncrypt.TabIndex = 10;
            btnEncrypt.Text = "加密数据";
            btnEncrypt.UseVisualStyleBackColor = false;
            btnEncrypt.Click += BtnEncrypt_Click;
            // 
            // btnTerminalEncryptionMenu
            // 
            btnTerminalEncryptionMenu.BackColor = Color.FromArgb(232, 238, 235);
            btnTerminalEncryptionMenu.ContextMenuStrip = cmsTerminalEncryptionFunctions;
            btnTerminalEncryptionMenu.Dock = DockStyle.Top;
            btnTerminalEncryptionMenu.FlatStyle = FlatStyle.Flat;
            btnTerminalEncryptionMenu.ForeColor = Color.FromArgb(37, 47, 41);
            btnTerminalEncryptionMenu.Location = new Point(3, 514);
            btnTerminalEncryptionMenu.Margin = new Padding(3, 10, 3, 0);
            btnTerminalEncryptionMenu.Name = "btnTerminalEncryptionMenu";
            btnTerminalEncryptionMenu.Size = new Size(303, 48);
            btnTerminalEncryptionMenu.TabIndex = 11;
            btnTerminalEncryptionMenu.Text = "终端加密函数调用";
            btnTerminalEncryptionMenu.UseVisualStyleBackColor = false;
            btnTerminalEncryptionMenu.Click += BtnTerminalEncryptionMenu_Click;
            // 
            // cmsTerminalEncryptionFunctions
            // 
            cmsTerminalEncryptionFunctions.ImageScalingSize = new Size(28, 28);
            cmsTerminalEncryptionFunctions.Name = "cmsTerminalEncryptionFunctions";
            cmsTerminalEncryptionFunctions.Size = new Size(61, 4);
            // 
            // btnMeterEncryptionMenu
            // 
            btnMeterEncryptionMenu.BackColor = Color.FromArgb(232, 238, 235);
            btnMeterEncryptionMenu.ContextMenuStrip = cmsMeterEncryptionFunctions;
            btnMeterEncryptionMenu.Dock = DockStyle.Top;
            btnMeterEncryptionMenu.FlatStyle = FlatStyle.Flat;
            btnMeterEncryptionMenu.ForeColor = Color.FromArgb(37, 47, 41);
            btnMeterEncryptionMenu.Location = new Point(3, 572);
            btnMeterEncryptionMenu.Margin = new Padding(3, 10, 3, 0);
            btnMeterEncryptionMenu.Name = "btnMeterEncryptionMenu";
            btnMeterEncryptionMenu.Size = new Size(303, 48);
            btnMeterEncryptionMenu.TabIndex = 12;
            btnMeterEncryptionMenu.Text = "电表加密函数调用";
            btnMeterEncryptionMenu.UseVisualStyleBackColor = false;
            btnMeterEncryptionMenu.Click += BtnMeterEncryptionMenu_Click;
            // 
            // cmsMeterEncryptionFunctions
            // 
            cmsMeterEncryptionFunctions.ImageScalingSize = new Size(28, 28);
            cmsMeterEncryptionFunctions.Name = "cmsMeterEncryptionFunctions";
            cmsMeterEncryptionFunctions.Size = new Size(61, 4);
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
            topLayoutPanel.PerformLayout();
            headerPanel.ResumeLayout(false);
            headerLayoutPanel.ResumeLayout(false);
            headerLayoutPanel.PerformLayout();
            keyModeLayoutPanel.ResumeLayout(false);
            keyModeLayoutPanel.PerformLayout();
            ResumeLayout(false);
        }
    }
}
