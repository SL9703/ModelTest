namespace ModelTest.CustomControl
{
    partial class UDPMessageUserControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            groupBoxstatus = new GroupBox();
            label4 = new Label();
            NUDBWNumbers = new NumericUpDown();
            btnUDPServerListenColse = new Button();
            chkBroadcast = new CheckBox();
            chkAutoCleanup = new CheckBox();
            txtTimeout = new TextBox();
            label3 = new Label();
            btnUDPServerListen = new Button();
            tbx_UDPServerport = new TextBox();
            label1 = new Label();
            groupBox2 = new GroupBox();
            tbxUDPMessageLog = new RichTextBox();
            groupBox3 = new GroupBox();
            tbxTCPClientManner = new TextBox();
            label5 = new Label();
            labelServernum = new Label();
            lblStatus = new Label();
            lstClients = new ListBox();
            label2 = new Label();
            groupBoxstatus.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)NUDBWNumbers).BeginInit();
            groupBox2.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // groupBoxstatus
            // 
            groupBoxstatus.Controls.Add(label4);
            groupBoxstatus.Controls.Add(NUDBWNumbers);
            groupBoxstatus.Controls.Add(btnUDPServerListenColse);
            groupBoxstatus.Controls.Add(chkBroadcast);
            groupBoxstatus.Controls.Add(chkAutoCleanup);
            groupBoxstatus.Controls.Add(txtTimeout);
            groupBoxstatus.Controls.Add(label3);
            groupBoxstatus.Controls.Add(btnUDPServerListen);
            groupBoxstatus.Controls.Add(tbx_UDPServerport);
            groupBoxstatus.Controls.Add(label1);
            groupBoxstatus.Dock = DockStyle.Top;
            groupBoxstatus.Location = new Point(0, 0);
            groupBoxstatus.Name = "groupBoxstatus";
            groupBoxstatus.Size = new Size(2588, 114);
            groupBoxstatus.TabIndex = 0;
            groupBoxstatus.TabStop = false;
            groupBoxstatus.Text = "开启监听服务面板";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(546, 66);
            label4.Name = "label4";
            label4.Size = new Size(117, 28);
            label4.TabIndex = 10;
            label4.Text = "表位数量：";
            // 
            // NUDBWNumbers
            // 
            NUDBWNumbers.Location = new Point(727, 66);
            NUDBWNumbers.Name = "NUDBWNumbers";
            NUDBWNumbers.Size = new Size(86, 34);
            NUDBWNumbers.TabIndex = 9;
            NUDBWNumbers.Value = new decimal(new int[] { 8, 0, 0, 0 });
            // 
            // btnUDPServerListenColse
            // 
            btnUDPServerListenColse.Location = new Point(329, 60);
            btnUDPServerListenColse.Name = "btnUDPServerListenColse";
            btnUDPServerListenColse.Size = new Size(162, 40);
            btnUDPServerListenColse.TabIndex = 8;
            btnUDPServerListenColse.Text = "关闭UDP监听";
            btnUDPServerListenColse.UseVisualStyleBackColor = true;
            btnUDPServerListenColse.Click += btnUDPServerListenColse_Click;
            // 
            // chkBroadcast
            // 
            chkBroadcast.AutoSize = true;
            chkBroadcast.Checked = true;
            chkBroadcast.CheckState = CheckState.Checked;
            chkBroadcast.Location = new Point(1077, 20);
            chkBroadcast.Name = "chkBroadcast";
            chkBroadcast.Size = new Size(122, 32);
            chkBroadcast.TabIndex = 7;
            chkBroadcast.Text = "启用广播";
            chkBroadcast.UseVisualStyleBackColor = true;
            // 
            // chkAutoCleanup
            // 
            chkAutoCleanup.AutoSize = true;
            chkAutoCleanup.Checked = true;
            chkAutoCleanup.CheckState = CheckState.Checked;
            chkAutoCleanup.Location = new Point(844, 19);
            chkAutoCleanup.Name = "chkAutoCleanup";
            chkAutoCleanup.Size = new Size(227, 32);
            chkAutoCleanup.TabIndex = 6;
            chkAutoCleanup.Text = "自动清理超时客户端";
            chkAutoCleanup.UseVisualStyleBackColor = true;
            // 
            // txtTimeout
            // 
            txtTimeout.Location = new Point(727, 17);
            txtTimeout.Name = "txtTimeout";
            txtTimeout.Size = new Size(95, 34);
            txtTimeout.TabIndex = 4;
            txtTimeout.Text = "30000";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(546, 20);
            label3.Name = "label3";
            label3.Size = new Size(162, 28);
            label3.TabIndex = 3;
            label3.Text = "UDP超时时间：";
            // 
            // btnUDPServerListen
            // 
            btnUDPServerListen.Location = new Point(329, 20);
            btnUDPServerListen.Name = "btnUDPServerListen";
            btnUDPServerListen.Size = new Size(162, 40);
            btnUDPServerListen.TabIndex = 2;
            btnUDPServerListen.Text = "开启UDP监听";
            btnUDPServerListen.UseVisualStyleBackColor = true;
            btnUDPServerListen.Click += btnUDPServerListen_Click;
            // 
            // tbx_UDPServerport
            // 
            tbx_UDPServerport.Location = new Point(165, 45);
            tbx_UDPServerport.Name = "tbx_UDPServerport";
            tbx_UDPServerport.Size = new Size(133, 34);
            tbx_UDPServerport.TabIndex = 1;
            tbx_UDPServerport.Text = "10001";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(25, 45);
            label1.Name = "label1";
            label1.Size = new Size(120, 28);
            label1.TabIndex = 0;
            label1.Text = "UDP端口：";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(tbxUDPMessageLog);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.Location = new Point(0, 114);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(2588, 500);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "重要消息面板";
            // 
            // tbxUDPMessageLog
            // 
            tbxUDPMessageLog.BackColor = SystemColors.WindowText;
            tbxUDPMessageLog.Dock = DockStyle.Fill;
            tbxUDPMessageLog.Location = new Point(3, 30);
            tbxUDPMessageLog.Name = "tbxUDPMessageLog";
            tbxUDPMessageLog.ReadOnly = true;
            tbxUDPMessageLog.Size = new Size(2582, 467);
            tbxUDPMessageLog.TabIndex = 0;
            tbxUDPMessageLog.Text = "";
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(tbxTCPClientManner);
            groupBox3.Controls.Add(label5);
            groupBox3.Controls.Add(labelServernum);
            groupBox3.Controls.Add(lblStatus);
            groupBox3.Controls.Add(lstClients);
            groupBox3.Controls.Add(label2);
            groupBox3.Dock = DockStyle.Top;
            groupBox3.Location = new Point(0, 614);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(2588, 202);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "统计信息服务面板";
            // 
            // tbxTCPClientManner
            // 
            tbxTCPClientManner.Location = new Point(1301, 45);
            tbxTCPClientManner.Multiline = true;
            tbxTCPClientManner.Name = "tbxTCPClientManner";
            tbxTCPClientManner.ReadOnly = true;
            tbxTCPClientManner.ScrollBars = ScrollBars.Both;
            tbxTCPClientManner.Size = new Size(528, 127);
            tbxTCPClientManner.TabIndex = 6;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(1097, 45);
            label5.Name = "label5";
            label5.Size = new Size(198, 28);
            label5.TabIndex = 5;
            label5.Text = "TCP需要链接列表：";
            // 
            // labelServernum
            // 
            labelServernum.AutoSize = true;
            labelServernum.Location = new Point(651, 48);
            labelServernum.Name = "labelServernum";
            labelServernum.Size = new Size(162, 28);
            labelServernum.TabIndex = 4;
            labelServernum.Text = "UDP服务统计：";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(25, 147);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(120, 28);
            lblStatus.TabIndex = 3;
            lblStatus.Text = "UDP状态：";
            // 
            // lstClients
            // 
            lstClients.FormattingEnabled = true;
            lstClients.ItemHeight = 28;
            lstClients.Location = new Point(169, 47);
            lstClients.Name = "lstClients";
            lstClients.Size = new Size(446, 88);
            lstClients.TabIndex = 2;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(25, 48);
            label2.Name = "label2";
            label2.Size = new Size(138, 28);
            label2.TabIndex = 1;
            label2.Text = "客户端列表：";
            // 
            // UDPMessageUserControl
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(groupBox3);
            Controls.Add(groupBox2);
            Controls.Add(groupBoxstatus);
            Name = "UDPMessageUserControl";
            Size = new Size(2588, 1080);
            groupBoxstatus.ResumeLayout(false);
            groupBoxstatus.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)NUDBWNumbers).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBoxstatus;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private Label label1;
        private TextBox tbx_UDPServerport;
        private Button btnUDPServerListen;
        private RichTextBox tbxUDPMessageLog;
        private Label label2;
        private ListBox lstClients;
        private Label label3;
        private TextBox txtTimeout;
        private CheckBox chkAutoCleanup;
        private CheckBox chkBroadcast;
        private Button btnUDPServerListenColse;
        private Label lblStatus;
        private Label labelServernum;
        private Label label4;
        private NumericUpDown NUDBWNumbers;
        private Label label5;
        private TextBox tbxTCPClientManner;
    }
}
