namespace ModelTest
{
    partial class DatabaseTestForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            groupBoxConnection = new GroupBox();
            lblDatabaseType = new Label();
            cmbDatabaseType = new ComboBox();
            lblIp = new Label();
            tbxIp = new TextBox();
            lblPort = new Label();
            tbxPort = new TextBox();
            lblDatabase = new Label();
            tbxDatabase = new TextBox();
            lblUser = new Label();
            tbxUser = new TextBox();
            lblPassword = new Label();
            tbxPassword = new TextBox();
            lblDatabaseMode = new Label();
            cmbDatabaseMode = new ComboBox();
            btnConnect = new Button();
            btnTestPort = new Button();
            lblStatusTitle = new Label();
            lblStatus = new Label();
            groupBoxQuery = new GroupBox();
            lblTaskNo = new Label();
            tbxTaskNo = new TextBox();
            btnQuery = new Button();
            lblQueryStatusTitle = new Label();
            lblQueryStatus = new Label();
            lblSql = new Label();
            tbxSql = new TextBox();
            lblCustomSql = new Label();
            tbxCustomSql = new TextBox();
            btnExecuteSql = new Button();
            groupBoxResult = new GroupBox();
            dgvResult = new DataGridView();
            groupBoxLog = new GroupBox();
            rtbLog = new RichTextBox();
            groupBoxConnection.SuspendLayout();
            groupBoxQuery.SuspendLayout();
            groupBoxResult.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvResult).BeginInit();
            groupBoxLog.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxConnection
            // 
            groupBoxConnection.Controls.Add(lblDatabaseType);
            groupBoxConnection.Controls.Add(cmbDatabaseType);
            groupBoxConnection.Controls.Add(lblIp);
            groupBoxConnection.Controls.Add(tbxIp);
            groupBoxConnection.Controls.Add(lblPort);
            groupBoxConnection.Controls.Add(tbxPort);
            groupBoxConnection.Controls.Add(lblDatabase);
            groupBoxConnection.Controls.Add(tbxDatabase);
            groupBoxConnection.Controls.Add(lblUser);
            groupBoxConnection.Controls.Add(tbxUser);
            groupBoxConnection.Controls.Add(lblPassword);
            groupBoxConnection.Controls.Add(tbxPassword);
            groupBoxConnection.Controls.Add(lblDatabaseMode);
            groupBoxConnection.Controls.Add(cmbDatabaseMode);
            groupBoxConnection.Controls.Add(btnConnect);
            groupBoxConnection.Controls.Add(btnTestPort);
            groupBoxConnection.Controls.Add(lblStatusTitle);
            groupBoxConnection.Controls.Add(lblStatus);
            groupBoxConnection.Location = new Point(40, 32);
            groupBoxConnection.Name = "groupBoxConnection";
            groupBoxConnection.Size = new Size(1840, 180);
            groupBoxConnection.TabIndex = 0;
            groupBoxConnection.TabStop = false;
            groupBoxConnection.Text = "连接参数";
            // 
            // lblDatabaseType
            // 
            lblDatabaseType.AutoSize = true;
            lblDatabaseType.Location = new Point(32, 58);
            lblDatabaseType.Name = "lblDatabaseType";
            lblDatabaseType.Size = new Size(62, 28);
            lblDatabaseType.TabIndex = 0;
            lblDatabaseType.Text = "类型:";
            // 
            // cmbDatabaseType
            // 
            cmbDatabaseType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDatabaseType.FormattingEnabled = true;
            cmbDatabaseType.Items.AddRange(new object[] { "Oracle", "MySQL" });
            cmbDatabaseType.Location = new Point(106, 54);
            cmbDatabaseType.Name = "cmbDatabaseType";
            cmbDatabaseType.Size = new Size(150, 36);
            cmbDatabaseType.TabIndex = 1;
            cmbDatabaseType.SelectedIndexChanged += DatabaseConnectionInfo_TextChanged;
            // 
            // lblIp
            // 
            lblIp.AutoSize = true;
            lblIp.Location = new Point(286, 58);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(34, 28);
            lblIp.TabIndex = 2;
            lblIp.Text = "IP:";
            // 
            // tbxIp
            // 
            tbxIp.Location = new Point(332, 54);
            tbxIp.Name = "tbxIp";
            tbxIp.Size = new Size(230, 34);
            tbxIp.TabIndex = 3;
            tbxIp.TextChanged += DatabaseConnectionInfo_TextChanged;
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(592, 58);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(59, 28);
            lblPort.TabIndex = 4;
            lblPort.Text = "Port:";
            // 
            // tbxPort
            // 
            tbxPort.Name = "tbxPort";
            tbxPort.Location = new Point(663, 54);
            tbxPort.Size = new Size(120, 34);
            tbxPort.TabIndex = 5;
            tbxPort.TextChanged += DatabaseConnectionInfo_TextChanged;
            // 
            // lblDatabase
            // 
            lblDatabase.AutoSize = true;
            lblDatabase.Location = new Point(812, 58);
            lblDatabase.Name = "lblDatabase";
            lblDatabase.Size = new Size(62, 28);
            lblDatabase.TabIndex = 6;
            lblDatabase.Text = "库名:";
            // 
            // tbxDatabase
            // 
            tbxDatabase.Location = new Point(886, 54);
            tbxDatabase.Name = "tbxDatabase";
            tbxDatabase.Size = new Size(170, 34);
            tbxDatabase.TabIndex = 7;
            tbxDatabase.TextChanged += DatabaseConnectionInfo_TextChanged;
            // 
            // lblUser
            // 
            lblUser.AutoSize = true;
            lblUser.Location = new Point(1086, 58);
            lblUser.Name = "lblUser";
            lblUser.Size = new Size(62, 28);
            lblUser.TabIndex = 8;
            lblUser.Text = "账户:";
            // 
            // tbxUser
            // 
            tbxUser.Location = new Point(1160, 54);
            tbxUser.Name = "tbxUser";
            tbxUser.Size = new Size(160, 34);
            tbxUser.TabIndex = 9;
            tbxUser.TextChanged += DatabaseConnectionInfo_TextChanged;
            // 
            // lblPassword
            // 
            lblPassword.AutoSize = true;
            lblPassword.Location = new Point(32, 116);
            lblPassword.Name = "lblPassword";
            lblPassword.Size = new Size(62, 28);
            lblPassword.TabIndex = 10;
            lblPassword.Text = "密码:";
            // 
            // tbxPassword
            // 
            tbxPassword.Location = new Point(106, 112);
            tbxPassword.Name = "tbxPassword";
            tbxPassword.PasswordChar = '*';
            tbxPassword.Size = new Size(232, 34);
            tbxPassword.TabIndex = 11;
            tbxPassword.TextChanged += DatabaseConnectionInfo_TextChanged;
            // 
            // lblDatabaseMode
            // 
            lblDatabaseMode.AutoSize = true;
            lblDatabaseMode.Location = new Point(374, 116);
            lblDatabaseMode.Name = "lblDatabaseMode";
            lblDatabaseMode.Size = new Size(62, 28);
            lblDatabaseMode.TabIndex = 12;
            lblDatabaseMode.Text = "模式:";
            // 
            // cmbDatabaseMode
            // 
            cmbDatabaseMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDatabaseMode.FormattingEnabled = true;
            cmbDatabaseMode.Items.AddRange(new object[] { "SERVICE_NAME", "SID" });
            cmbDatabaseMode.Location = new Point(448, 112);
            cmbDatabaseMode.Name = "cmbDatabaseMode";
            cmbDatabaseMode.Size = new Size(180, 36);
            cmbDatabaseMode.TabIndex = 13;
            cmbDatabaseMode.SelectedIndexChanged += DatabaseConnectionInfo_TextChanged;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(660, 108);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(160, 42);
            btnConnect.TabIndex = 14;
            btnConnect.Text = "连接数据库";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // btnTestPort
            // 
            btnTestPort.Location = new Point(840, 108);
            btnTestPort.Name = "btnTestPort";
            btnTestPort.Size = new Size(160, 42);
            btnTestPort.TabIndex = 15;
            btnTestPort.Text = "测试端口";
            btnTestPort.UseVisualStyleBackColor = true;
            btnTestPort.Click += btnTestPort_Click;
            // 
            // lblStatusTitle
            // 
            lblStatusTitle.AutoSize = true;
            lblStatusTitle.Location = new Point(1032, 115);
            lblStatusTitle.Name = "lblStatusTitle";
            lblStatusTitle.Size = new Size(62, 28);
            lblStatusTitle.TabIndex = 16;
            lblStatusTitle.Text = "状态:";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(1106, 115);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(72, 28);
            lblStatus.TabIndex = 17;
            lblStatus.Text = "未连接";
            // 
            // groupBoxQuery
            // 
            groupBoxQuery.Controls.Add(lblCustomSql);
            groupBoxQuery.Controls.Add(tbxCustomSql);
            groupBoxQuery.Controls.Add(btnExecuteSql);
            groupBoxQuery.Controls.Add(lblTaskNo);
            groupBoxQuery.Controls.Add(tbxTaskNo);
            groupBoxQuery.Controls.Add(btnQuery);
            groupBoxQuery.Controls.Add(lblQueryStatusTitle);
            groupBoxQuery.Controls.Add(lblQueryStatus);
            groupBoxQuery.Controls.Add(lblSql);
            groupBoxQuery.Controls.Add(tbxSql);
            groupBoxQuery.Location = new Point(40, 230);
            groupBoxQuery.Name = "groupBoxQuery";
            groupBoxQuery.Size = new Size(1840, 260);
            groupBoxQuery.TabIndex = 1;
            groupBoxQuery.TabStop = false;
            groupBoxQuery.Text = "SQL执行";
            // 
            // lblTaskNo
            // 
            lblTaskNo.TextAlign = ContentAlignment.MiddleRight;
            lblTaskNo.Location = new Point(32, 42);
            lblTaskNo.Name = "lblTaskNo";
            lblTaskNo.Size = new Size(110, 28);
            lblTaskNo.TabIndex = 0;
            lblTaskNo.Text = "任务编号:";
            // 
            // tbxTaskNo
            // 
            tbxTaskNo.Location = new Point(170, 38);
            tbxTaskNo.Name = "tbxTaskNo";
            tbxTaskNo.Size = new Size(320, 34);
            tbxTaskNo.TabIndex = 1;
            // 
            // btnQuery
            // 
            btnQuery.Location = new Point(520, 35);
            btnQuery.Name = "btnQuery";
            btnQuery.Size = new Size(126, 42);
            btnQuery.TabIndex = 2;
            btnQuery.Text = "查询";
            btnQuery.UseVisualStyleBackColor = true;
            btnQuery.Enabled = false;
            btnQuery.Click += btnQuery_Click;
            // 
            // lblQueryStatusTitle
            // 
            lblQueryStatusTitle.AutoSize = true;
            lblQueryStatusTitle.Location = new Point(680, 42);
            lblQueryStatusTitle.Name = "lblQueryStatusTitle";
            lblQueryStatusTitle.Size = new Size(62, 28);
            lblQueryStatusTitle.TabIndex = 3;
            lblQueryStatusTitle.Text = "状态:";
            // 
            // lblQueryStatus
            // 
            lblQueryStatus.AutoSize = true;
            lblQueryStatus.Location = new Point(754, 42);
            lblQueryStatus.Name = "lblQueryStatus";
            lblQueryStatus.Size = new Size(72, 28);
            lblQueryStatus.TabIndex = 4;
            lblQueryStatus.Text = "未查询";
            // 
            // lblSql
            // 
            lblSql.TextAlign = ContentAlignment.MiddleRight;
            lblSql.Location = new Point(32, 86);
            lblSql.Name = "lblSql";
            lblSql.Size = new Size(110, 28);
            lblSql.TabIndex = 5;
            lblSql.Text = "固定SQL:";
            // 
            // tbxSql
            // 
            tbxSql.Location = new Point(170, 82);
            tbxSql.Name = "tbxSql";
            tbxSql.ReadOnly = true;
            tbxSql.Size = new Size(1544, 34);
            tbxSql.TabIndex = 6;
            tbxSql.Text = "select t.detect_equip_no as \"检测装置编号\" from MT_DETECT_TMNL_RSLT t where t.detect_task_no = :task编号";
            // 
            // lblCustomSql
            // 
            lblCustomSql.TextAlign = ContentAlignment.MiddleRight;
            lblCustomSql.Location = new Point(32, 136);
            lblCustomSql.Name = "lblCustomSql";
            lblCustomSql.Size = new Size(110, 28);
            lblCustomSql.TabIndex = 7;
            lblCustomSql.Text = "自定义SQL:";
            // 
            // tbxCustomSql
            // 
            tbxCustomSql.AcceptsReturn = true;
            tbxCustomSql.AcceptsTab = true;
            tbxCustomSql.Location = new Point(170, 132);
            tbxCustomSql.Multiline = true;
            tbxCustomSql.Name = "tbxCustomSql";
            tbxCustomSql.ScrollBars = ScrollBars.Vertical;
            tbxCustomSql.Size = new Size(1384, 94);
            tbxCustomSql.TabIndex = 8;
            // 
            // btnExecuteSql
            // 
            btnExecuteSql.Enabled = false;
            btnExecuteSql.Location = new Point(1584, 132);
            btnExecuteSql.Name = "btnExecuteSql";
            btnExecuteSql.Size = new Size(130, 42);
            btnExecuteSql.TabIndex = 9;
            btnExecuteSql.Text = "执行SQL";
            btnExecuteSql.UseVisualStyleBackColor = true;
            btnExecuteSql.Click += btnExecuteSql_Click;
            // 
            // groupBoxResult
            // 
            groupBoxResult.Controls.Add(dgvResult);
            groupBoxResult.Location = new Point(40, 510);
            groupBoxResult.Name = "groupBoxResult";
            groupBoxResult.Size = new Size(1240, 520);
            groupBoxResult.TabIndex = 2;
            groupBoxResult.TabStop = false;
            groupBoxResult.Text = "查询结果";
            // 
            // dgvResult
            // 
            dgvResult.AllowUserToAddRows = false;
            dgvResult.AllowUserToDeleteRows = false;
            dgvResult.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgvResult.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvResult.Dock = DockStyle.Fill;
            dgvResult.Location = new Point(3, 30);
            dgvResult.Name = "dgvResult";
            dgvResult.ReadOnly = true;
            dgvResult.RowHeadersWidth = 72;
            dgvResult.Size = new Size(1234, 487);
            dgvResult.TabIndex = 0;
            // 
            // groupBoxLog
            // 
            groupBoxLog.Controls.Add(rtbLog);
            groupBoxLog.Location = new Point(1300, 510);
            groupBoxLog.Name = "groupBoxLog";
            groupBoxLog.Size = new Size(580, 520);
            groupBoxLog.TabIndex = 3;
            groupBoxLog.TabStop = false;
            groupBoxLog.Text = "日志";
            // 
            // rtbLog
            // 
            rtbLog.Dock = DockStyle.Fill;
            rtbLog.Location = new Point(3, 30);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(574, 487);
            rtbLog.TabIndex = 0;
            rtbLog.Text = "";
            // 
            // DatabaseTestForm
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(88, 149, 127);
            ClientSize = new Size(1920, 1080);
            Controls.Add(groupBoxLog);
            Controls.Add(groupBoxResult);
            Controls.Add(groupBoxQuery);
            Controls.Add(groupBoxConnection);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "DatabaseTestForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "数据库测试";
            groupBoxConnection.ResumeLayout(false);
            groupBoxConnection.PerformLayout();
            groupBoxQuery.ResumeLayout(false);
            groupBoxQuery.PerformLayout();
            groupBoxResult.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvResult).EndInit();
            groupBoxLog.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxConnection;
        private Label lblDatabaseType;
        private ComboBox cmbDatabaseType;
        private Label lblIp;
        private TextBox tbxIp;
        private Label lblPort;
        private TextBox tbxPort;
        private Label lblDatabase;
        private TextBox tbxDatabase;
        private Label lblUser;
        private TextBox tbxUser;
        private Label lblPassword;
        private TextBox tbxPassword;
        private Label lblDatabaseMode;
        private ComboBox cmbDatabaseMode;
        private Button btnConnect;
        private Button btnTestPort;
        private Label lblStatusTitle;
        private Label lblStatus;
        private GroupBox groupBoxQuery;
        private Label lblTaskNo;
        private TextBox tbxTaskNo;
        private Button btnQuery;
        private Label lblQueryStatusTitle;
        private Label lblQueryStatus;
        private Label lblSql;
        private TextBox tbxSql;
        private Label lblCustomSql;
        private TextBox tbxCustomSql;
        private Button btnExecuteSql;
        private GroupBox groupBoxResult;
        private DataGridView dgvResult;
        private GroupBox groupBoxLog;
        private RichTextBox rtbLog;
    }
}
