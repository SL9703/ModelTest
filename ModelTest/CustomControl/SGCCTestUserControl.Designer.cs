namespace ModelTest.CustomControl
{
    partial class SGCCTestUserControl
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

        private void InitializeComponent()
        {
            scrollPanel = new Panel();
            rootLayout = new TableLayoutPanel();
            groupBroadcast = new GroupBox();
            broadcastLayout = new TableLayoutPanel();
            label11 = new TextBox();
            SGCC645FF = new Button();
            label13 = new TextBox();
            CSG698FF = new Button();
            label18 = new TextBox();
            buttonKZHLStatus = new Button();
            label19 = new TextBox();
            buttonKZHLID = new Button();
            groupRead = new GroupBox();
            readLayout = new TableLayoutPanel();
            label32 = new Label();
            tbxMeterTerminalAddr = new TextBox();
            oadLineLayout = new TableLayoutPanel();
            labelOadCategory = new Label();
            cbxSgccOadCategory = new ComboBox();
            labelOad = new Label();
            cbxSgccOAD = new ComboBox();
            btnReadMSG = new Button();
            targetLayout = new FlowLayoutPanel();
            cbxSGCC_Terminal = new CheckBox();
            cbxSGCC_Meter = new CheckBox();
            label9 = new Label();
            scrollPanel.SuspendLayout();
            rootLayout.SuspendLayout();
            groupBroadcast.SuspendLayout();
            broadcastLayout.SuspendLayout();
            groupRead.SuspendLayout();
            readLayout.SuspendLayout();
            oadLineLayout.SuspendLayout();
            targetLayout.SuspendLayout();
            SuspendLayout();
            // 
            // scrollPanel
            // 
            scrollPanel.AutoScroll = true;
            scrollPanel.Controls.Add(rootLayout);
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.Location = new Point(0, 0);
            scrollPanel.Name = "scrollPanel";
            scrollPanel.Size = new Size(2236, 851);
            scrollPanel.TabIndex = 0;
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(groupBroadcast, 0, 0);
            rootLayout.Controls.Add(groupRead, 0, 1);
            rootLayout.Dock = DockStyle.Top;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new Padding(10);
            rootLayout.RowCount = 2;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 230F));
            rootLayout.Size = new Size(2236, 500);
            rootLayout.TabIndex = 0;
            // 
            // groupBroadcast
            // 
            groupBroadcast.Controls.Add(broadcastLayout);
            groupBroadcast.Dock = DockStyle.Fill;
            groupBroadcast.Location = new Point(14, 14);
            groupBroadcast.Margin = new Padding(4);
            groupBroadcast.Name = "groupBroadcast";
            groupBroadcast.Padding = new Padding(10);
            groupBroadcast.Size = new Size(2208, 242);
            groupBroadcast.TabIndex = 0;
            groupBroadcast.TabStop = false;
            groupBroadcast.Text = "广播与控制回路报文";
            // 
            // broadcastLayout
            // 
            broadcastLayout.ColumnCount = 2;
            broadcastLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            broadcastLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260F));
            broadcastLayout.Controls.Add(label11, 0, 0);
            broadcastLayout.Controls.Add(SGCC645FF, 1, 0);
            broadcastLayout.Controls.Add(label13, 0, 1);
            broadcastLayout.Controls.Add(CSG698FF, 1, 1);
            broadcastLayout.Controls.Add(label18, 0, 2);
            broadcastLayout.Controls.Add(buttonKZHLStatus, 1, 2);
            broadcastLayout.Controls.Add(label19, 0, 3);
            broadcastLayout.Controls.Add(buttonKZHLID, 1, 3);
            broadcastLayout.Dock = DockStyle.Fill;
            broadcastLayout.Location = new Point(10, 40);
            broadcastLayout.Name = "broadcastLayout";
            broadcastLayout.RowCount = 4;
            broadcastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            broadcastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            broadcastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            broadcastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            broadcastLayout.Size = new Size(2188, 192);
            broadcastLayout.TabIndex = 0;
            // 
            // label11
            // 
            label11.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label11.BackColor = Color.White;
            label11.BorderStyle = BorderStyle.FixedSingle;
            label11.ForeColor = Color.Red;
            label11.Location = new Point(5, 5);
            label11.Margin = new Padding(5);
            label11.Name = "label11";
            label11.ReadOnly = true;
            label11.Size = new Size(1918, 37);
            label11.TabIndex = 3;
            label11.Text = "FEFEFEFE68AAAAAAAAAAAA681300DF16";
            // 
            // SGCC645FF
            // 
            SGCC645FF.Anchor = AnchorStyles.Left;
            SGCC645FF.Location = new Point(1933, 5);
            SGCC645FF.Margin = new Padding(5);
            SGCC645FF.Name = "SGCC645FF";
            SGCC645FF.Size = new Size(190, 38);
            SGCC645FF.TabIndex = 7;
            SGCC645FF.Text = "国网645广播";
            SGCC645FF.UseVisualStyleBackColor = true;
            SGCC645FF.Click += SGCC645FF_Click;
            // 
            // label13
            // 
            label13.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label13.BackColor = Color.White;
            label13.BorderStyle = BorderStyle.FixedSingle;
            label13.ForeColor = Color.Red;
            label13.Location = new Point(5, 53);
            label13.Margin = new Padding(5);
            label13.Name = "label13";
            label13.ReadOnly = true;
            label13.Size = new Size(1918, 37);
            label13.TabIndex = 5;
            label13.Text = "6810001000684AFFFFFFFFFFFF010A710000210100E0C216";
            // 
            // CSG698FF
            // 
            CSG698FF.Anchor = AnchorStyles.Left;
            CSG698FF.Location = new Point(1933, 53);
            CSG698FF.Margin = new Padding(5);
            CSG698FF.Name = "CSG698FF";
            CSG698FF.Size = new Size(190, 38);
            CSG698FF.TabIndex = 8;
            CSG698FF.Text = "南网698广播";
            CSG698FF.UseVisualStyleBackColor = true;
            CSG698FF.Click += CSG698FF_Click;
            // 
            // label18
            // 
            label18.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label18.BackColor = Color.White;
            label18.BorderStyle = BorderStyle.FixedSingle;
            label18.ForeColor = Color.Red;
            label18.Location = new Point(5, 101);
            label18.Margin = new Padding(5);
            label18.Name = "label18";
            label18.ReadOnly = true;
            label18.Size = new Size(1918, 37);
            label18.TabIndex = 24;
            label18.Text = "6817004345AAAAAAAAAAAA10da5f05013DFF140200006c6816";
            // 
            // buttonKZHLStatus
            // 
            buttonKZHLStatus.Anchor = AnchorStyles.Left;
            buttonKZHLStatus.Location = new Point(1933, 101);
            buttonKZHLStatus.Margin = new Padding(5);
            buttonKZHLStatus.Name = "buttonKZHLStatus";
            buttonKZHLStatus.Size = new Size(190, 38);
            buttonKZHLStatus.TabIndex = 25;
            buttonKZHLStatus.Text = "控制回路检测仪状态";
            buttonKZHLStatus.UseVisualStyleBackColor = true;
            buttonKZHLStatus.Click += buttonKZHLStatus_Click;
            // 
            // label19
            // 
            label19.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label19.BackColor = Color.White;
            label19.BorderStyle = BorderStyle.FixedSingle;
            label19.ForeColor = Color.Red;
            label19.Location = new Point(5, 149);
            label19.Margin = new Padding(5);
            label19.Name = "label19";
            label19.ReadOnly = true;
            label19.Size = new Size(1918, 37);
            label19.TabIndex = 26;
            label19.Text = "6817004345AAAAAAAAAAAA10DA5F050127F10002000027D316";
            // 
            // buttonKZHLID
            // 
            buttonKZHLID.Anchor = AnchorStyles.Left;
            buttonKZHLID.Location = new Point(1933, 149);
            buttonKZHLID.Margin = new Padding(5);
            buttonKZHLID.Name = "buttonKZHLID";
            buttonKZHLID.Size = new Size(190, 38);
            buttonKZHLID.TabIndex = 27;
            buttonKZHLID.Text = "控制回路检测仪ID";
            buttonKZHLID.UseVisualStyleBackColor = true;
            buttonKZHLID.Click += buttonKZHLID_Click;
            // 
            // groupRead
            // 
            groupRead.Controls.Add(readLayout);
            groupRead.Dock = DockStyle.Fill;
            groupRead.Location = new Point(14, 264);
            groupRead.Margin = new Padding(4);
            groupRead.Name = "groupRead";
            groupRead.Padding = new Padding(10);
            groupRead.Size = new Size(2208, 222);
            groupRead.TabIndex = 1;
            groupRead.TabStop = false;
            groupRead.Text = "国网698读取";
            // 
            // readLayout
            // 
            readLayout.ColumnCount = 4;
            readLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            readLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 760F));
            readLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            readLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            readLayout.Controls.Add(label32, 0, 0);
            readLayout.Controls.Add(tbxMeterTerminalAddr, 1, 0);
            readLayout.Controls.Add(oadLineLayout, 0, 1);
            readLayout.Controls.Add(targetLayout, 1, 2);
            readLayout.Dock = DockStyle.Fill;
            readLayout.Location = new Point(10, 40);
            readLayout.Name = "readLayout";
            readLayout.RowCount = 3;
            readLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            readLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            readLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            readLayout.Size = new Size(2188, 172);
            readLayout.TabIndex = 0;
            // 
            // label32
            // 
            label32.Anchor = AnchorStyles.Left;
            label32.AutoSize = true;
            label32.Location = new Point(5, 0);
            label32.Margin = new Padding(5, 0, 5, 0);
            label32.Name = "label32";
            label32.Size = new Size(206, 52);
            label32.TabIndex = 29;
            label32.Text = "电表地址或者终端地址";
            // 
            // tbxMeterTerminalAddr
            // 
            tbxMeterTerminalAddr.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxMeterTerminalAddr.Location = new Point(235, 7);
            tbxMeterTerminalAddr.Margin = new Padding(5);
            tbxMeterTerminalAddr.Name = "tbxMeterTerminalAddr";
            tbxMeterTerminalAddr.Size = new Size(750, 37);
            tbxMeterTerminalAddr.TabIndex = 28;
            tbxMeterTerminalAddr.Text = "000000000001";
            // 
            // oadLineLayout
            // 
            oadLineLayout.ColumnCount = 5;
            readLayout.SetColumnSpan(oadLineLayout, 4);
            oadLineLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            oadLineLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            oadLineLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            oadLineLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 460F));
            oadLineLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            oadLineLayout.Controls.Add(labelOadCategory, 0, 0);
            oadLineLayout.Controls.Add(cbxSgccOadCategory, 1, 0);
            oadLineLayout.Controls.Add(labelOad, 2, 0);
            oadLineLayout.Controls.Add(cbxSgccOAD, 3, 0);
            oadLineLayout.Controls.Add(btnReadMSG, 4, 0);
            oadLineLayout.Dock = DockStyle.Fill;
            oadLineLayout.Location = new Point(0, 52);
            oadLineLayout.Margin = new Padding(0);
            oadLineLayout.Name = "oadLineLayout";
            oadLineLayout.RowCount = 1;
            oadLineLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            oadLineLayout.Size = new Size(2188, 52);
            oadLineLayout.TabIndex = 38;
            // 
            // labelOadCategory
            // 
            labelOadCategory.Anchor = AnchorStyles.Left;
            labelOadCategory.AutoSize = true;
            labelOadCategory.Location = new Point(5, 10);
            labelOadCategory.Margin = new Padding(5, 0, 5, 0);
            labelOadCategory.Name = "labelOadCategory";
            labelOadCategory.Size = new Size(117, 31);
            labelOadCategory.TabIndex = 36;
            labelOadCategory.Text = "OAD类型";
            // 
            // cbxSgccOadCategory
            // 
            cbxSgccOadCategory.Anchor = AnchorStyles.Left;
            cbxSgccOadCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxSgccOadCategory.FormattingEnabled = true;
            cbxSgccOadCategory.Location = new Point(235, 8);
            cbxSgccOadCategory.Margin = new Padding(5);
            cbxSgccOadCategory.Name = "cbxSgccOadCategory";
            cbxSgccOadCategory.Size = new Size(220, 38);
            cbxSgccOadCategory.TabIndex = 37;
            cbxSgccOadCategory.SelectedIndexChanged += cbxSgccOadCategory_SelectedIndexChanged;
            // 
            // labelOad
            // 
            labelOad.Anchor = AnchorStyles.Left;
            labelOad.AutoSize = true;
            labelOad.Location = new Point(485, 0);
            labelOad.Margin = new Padding(5, 0, 5, 0);
            labelOad.Name = "labelOad";
            labelOad.Size = new Size(93, 52);
            labelOad.TabIndex = 34;
            labelOad.Text = "OAD项目";
            // 
            // cbxSgccOAD
            // 
            cbxSgccOAD.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxSgccOAD.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxSgccOAD.DropDownWidth = 450;
            cbxSgccOAD.FormattingEnabled = true;
            cbxSgccOAD.Location = new Point(605, 8);
            cbxSgccOAD.Margin = new Padding(5);
            cbxSgccOAD.Name = "cbxSgccOAD";
            cbxSgccOAD.Size = new Size(450, 38);
            cbxSgccOAD.TabIndex = 31;
            // 
            // btnReadMSG
            // 
            btnReadMSG.Anchor = AnchorStyles.Left;
            btnReadMSG.Location = new Point(1065, 5);
            btnReadMSG.Margin = new Padding(5);
            btnReadMSG.Name = "btnReadMSG";
            btnReadMSG.Size = new Size(150, 42);
            btnReadMSG.TabIndex = 30;
            btnReadMSG.Text = "读取";
            btnReadMSG.UseVisualStyleBackColor = true;
            btnReadMSG.Click += btnReadMSG_Click;
            // 
            // targetLayout
            // 
            targetLayout.Controls.Add(cbxSGCC_Terminal);
            targetLayout.Controls.Add(cbxSGCC_Meter);
            targetLayout.Dock = DockStyle.Fill;
            targetLayout.Location = new Point(233, 107);
            targetLayout.Name = "targetLayout";
            targetLayout.Size = new Size(754, 62);
            targetLayout.TabIndex = 35;
            // 
            // cbxSGCC_Terminal
            // 
            cbxSGCC_Terminal.AutoSize = true;
            cbxSGCC_Terminal.Checked = true;
            cbxSGCC_Terminal.CheckState = CheckState.Checked;
            cbxSGCC_Terminal.Location = new Point(5, 5);
            cbxSGCC_Terminal.Margin = new Padding(5);
            cbxSGCC_Terminal.Name = "cbxSGCC_Terminal";
            cbxSGCC_Terminal.Size = new Size(88, 35);
            cbxSGCC_Terminal.TabIndex = 32;
            cbxSGCC_Terminal.Text = "终端";
            cbxSGCC_Terminal.UseVisualStyleBackColor = true;
            // 
            // cbxSGCC_Meter
            // 
            cbxSGCC_Meter.AutoSize = true;
            cbxSGCC_Meter.Location = new Point(103, 5);
            cbxSGCC_Meter.Margin = new Padding(5);
            cbxSGCC_Meter.Name = "cbxSGCC_Meter";
            cbxSGCC_Meter.Size = new Size(88, 35);
            cbxSGCC_Meter.TabIndex = 33;
            cbxSGCC_Meter.Text = "电表";
            cbxSGCC_Meter.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            label9.BackColor = Color.White;
            label9.BorderStyle = BorderStyle.FixedSingle;
            label9.Dock = DockStyle.Bottom;
            label9.ForeColor = Color.Red;
            label9.Location = new Point(0, 819);
            label9.Margin = new Padding(6, 0, 6, 0);
            label9.Name = "label9";
            label9.Padding = new Padding(12, 0, 0, 0);
            label9.Size = new Size(2236, 32);
            label9.TabIndex = 23;
            label9.Text = "通道端口：485-2，232，红外等";
            label9.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // SGCCTestUserControl
            // 
            AutoScaleDimensions = new SizeF(14F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(88, 149, 127);
            Controls.Add(label9);
            Controls.Add(scrollPanel);
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
            Margin = new Padding(4);
            Name = "SGCCTestUserControl";
            Size = new Size(2236, 851);
            scrollPanel.ResumeLayout(false);
            rootLayout.ResumeLayout(false);
            groupBroadcast.ResumeLayout(false);
            broadcastLayout.ResumeLayout(false);
            broadcastLayout.PerformLayout();
            groupRead.ResumeLayout(false);
            readLayout.ResumeLayout(false);
            readLayout.PerformLayout();
            oadLineLayout.ResumeLayout(false);
            oadLineLayout.PerformLayout();
            targetLayout.ResumeLayout(false);
            targetLayout.PerformLayout();
            ResumeLayout(false);
        }

        private Panel scrollPanel;
        private TableLayoutPanel rootLayout;
        private GroupBox groupBroadcast;
        private TableLayoutPanel broadcastLayout;
        private TextBox label11;
        private Button SGCC645FF;
        private TextBox label13;
        private Button CSG698FF;
        private TextBox label18;
        private Button buttonKZHLStatus;
        private TextBox label19;
        private Button buttonKZHLID;
        private GroupBox groupRead;
        private TableLayoutPanel readLayout;
        private Label label32;
        private TextBox tbxMeterTerminalAddr;
        private TableLayoutPanel oadLineLayout;
        private Label labelOadCategory;
        private ComboBox cbxSgccOadCategory;
        private Label labelOad;
        private ComboBox cbxSgccOAD;
        private FlowLayoutPanel targetLayout;
        private CheckBox cbxSGCC_Terminal;
        private CheckBox cbxSGCC_Meter;
        private Button btnReadMSG;
        private Label label9;
    }
}
