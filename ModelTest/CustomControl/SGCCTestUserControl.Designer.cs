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
            targetLayout = new FlowLayoutPanel();
            cbxSGCC_Terminal = new CheckBox();
            cbxSGCC_Meter = new CheckBox();
            btnReadMSG = new Button();
            groupJjg596 = new GroupBox();
            jjg596Layout = new TableLayoutPanel();
            labelJjg596MeasurementUnit = new Label();
            cbxJjg596MeasurementUnit = new ComboBox();
            labelJjg596Voltage = new Label();
            cbxJjg596Voltage = new ComboBox();
            labelJjg596Current = new Label();
            cbxJjg596Current = new ComboBox();
            labelJjg596ActiveClass = new Label();
            cbxJjg596ActiveClass = new ComboBox();
            labelJjg596ReactiveClass = new Label();
            cbxJjg596ReactiveClass = new ComboBox();
            labelJjg596MeterConstant = new Label();
            cbxJjg596MeterConstant = new ComboBox();
            labelJjg596AccessMode = new Label();
            cbxJjg596AccessMode = new ComboBox();
            labelJjg596Imin = new Label();
            tbxJjg596Imin = new TextBox();
            labelJjg596Itr = new Label();
            tbxJjg596Itr = new TextBox();
            labelJjg596Imax = new Label();
            tbxJjg596Imax = new TextBox();
            labelJjg596ReferenceCurrent = new Label();
            tbxJjg596ReferenceCurrent = new TextBox();
            labelJjg596Hint = new Label();
            btnOpenJjg596Pdf = new Button();
            groupJjg596StartTime = new GroupBox();
            tableJjg596StartTime = new TableLayoutPanel();
            labelJjg596StartFormula = new Label();
            labelJjg596StartDescription = new Label();
            labelJjg596StartCurrent = new Label();
            tbxJjg596StartCurrent = new TextBox();
            labelJjg596StartPst = new Label();
            tbxJjg596StartPst = new TextBox();
            labelJjg596StartTimeLower = new Label();
            tbxJjg596StartTimeLower = new TextBox();
            labelJjg596StartTimeUpper = new Label();
            tbxJjg596StartTimeUpper = new TextBox();
            groupJjg596ErrorTime = new GroupBox();
            tableJjg596ErrorTime = new TableLayoutPanel();
            labelJjg596ErrorFormula = new Label();
            labelJjg596ErrorDescription = new Label();
            labelJjg596ErrorPowerType = new Label();
            cbxJjg596ErrorPowerType = new ComboBox();
            labelJjg596ErrorPowerFactor = new Label();
            cbxJjg596ErrorPowerFactor = new ComboBox();
            labelJjg596ErrorPhase = new Label();
            cbxJjg596ErrorPhase = new ComboBox();
            labelJjg596ErrorCurrent = new Label();
            tbxJjg596ErrorCurrent = new TextBox();
            labelJjg596ErrorPulseCount = new Label();
            tbxJjg596ErrorPulseCount = new TextBox();
            labelJjg596ErrorPower = new Label();
            tbxJjg596ErrorPower = new TextBox();
            labelJjg596ErrorTime = new Label();
            tbxJjg596ErrorTime = new TextBox();
            labelJjg596ErrorCorrectedPulseCount = new Label();
            tbxJjg596ErrorCorrectedPulseCount = new TextBox();
            labelJjg596ErrorHint = new Label();
            groupJjg596CreepTime = new GroupBox();
            tableJjg596CreepTime = new TableLayoutPanel();
            labelJjg596CreepFormula = new Label();
            labelJjg596CreepDescription = new Label();
            labelJjg596CreepHours = new Label();
            tbxJjg596CreepHours = new TextBox();
            labelJjg596CreepMinutes = new Label();
            tbxJjg596CreepMinutes = new TextBox();
            labelJjg596CreepSeconds = new Label();
            tbxJjg596CreepSeconds = new TextBox();
            label9 = new Label();
            scrollPanel.SuspendLayout();
            rootLayout.SuspendLayout();
            groupBroadcast.SuspendLayout();
            broadcastLayout.SuspendLayout();
            groupRead.SuspendLayout();
            readLayout.SuspendLayout();
            oadLineLayout.SuspendLayout();
            targetLayout.SuspendLayout();
            groupJjg596.SuspendLayout();
            groupJjg596StartTime.SuspendLayout();
            tableJjg596StartTime.SuspendLayout();
            groupJjg596ErrorTime.SuspendLayout();
            tableJjg596ErrorTime.SuspendLayout();
            jjg596Layout.SuspendLayout();
            groupJjg596CreepTime.SuspendLayout();
            tableJjg596CreepTime.SuspendLayout();
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
            rootLayout.Controls.Add(groupJjg596, 0, 2);
            rootLayout.Dock = DockStyle.Top;
            rootLayout.Location = new Point(0, 0);
            rootLayout.Name = "rootLayout";
            rootLayout.Padding = new Padding(10);
            rootLayout.RowCount = 3;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 230F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 920F));
            rootLayout.Size = new Size(2236, 1480);
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
            broadcastLayout.Location = new Point(10, 37);
            broadcastLayout.Name = "broadcastLayout";
            broadcastLayout.RowCount = 4;
            broadcastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            broadcastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            broadcastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            broadcastLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            broadcastLayout.Size = new Size(2188, 195);
            broadcastLayout.TabIndex = 0;
            // 
            // label11
            // 
            label11.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            label11.BackColor = Color.White;
            label11.BorderStyle = BorderStyle.FixedSingle;
            label11.ForeColor = Color.Red;
            label11.Location = new Point(5, 7);
            label11.Margin = new Padding(5);
            label11.Name = "label11";
            label11.ReadOnly = true;
            label11.Size = new Size(1918, 30);
            label11.TabIndex = 3;
            label11.Text = "FEFEFEFE68AAAAAAAAAAAA681300DF16";
            // 
            // SGCC645FF
            // 
            SGCC645FF.Anchor = AnchorStyles.Left;
            SGCC645FF.Location = new Point(1933, 5);
            SGCC645FF.Margin = new Padding(5);
            SGCC645FF.Name = "SGCC645FF";
            SGCC645FF.Size = new Size(190, 42);
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
            label13.Location = new Point(5, 58);
            label13.Margin = new Padding(5);
            label13.Name = "label13";
            label13.ReadOnly = true;
            label13.Size = new Size(1918, 30);
            label13.TabIndex = 5;
            label13.Text = "6810001000684AFFFFFFFFFFFF010A710000210100E0C216";
            // 
            // CSG698FF
            // 
            CSG698FF.Anchor = AnchorStyles.Left;
            CSG698FF.Location = new Point(1933, 56);
            CSG698FF.Margin = new Padding(5);
            CSG698FF.Name = "CSG698FF";
            CSG698FF.Size = new Size(190, 42);
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
            label18.Location = new Point(5, 109);
            label18.Margin = new Padding(5);
            label18.Name = "label18";
            label18.ReadOnly = true;
            label18.Size = new Size(1918, 30);
            label18.TabIndex = 24;
            label18.Text = "6817004345AAAAAAAAAAAA10da5f05013DFF140200006c6816";
            // 
            // buttonKZHLStatus
            // 
            buttonKZHLStatus.Anchor = AnchorStyles.Left;
            buttonKZHLStatus.Location = new Point(1933, 107);
            buttonKZHLStatus.Margin = new Padding(5);
            buttonKZHLStatus.Name = "buttonKZHLStatus";
            buttonKZHLStatus.Size = new Size(190, 42);
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
            label19.Location = new Point(5, 161);
            label19.Margin = new Padding(5);
            label19.Name = "label19";
            label19.ReadOnly = true;
            label19.Size = new Size(1918, 30);
            label19.TabIndex = 26;
            label19.Text = "6817004345AAAAAAAAAAAA10DA5F050127F10002000027D316";
            // 
            // buttonKZHLID
            // 
            buttonKZHLID.Anchor = AnchorStyles.Left;
            buttonKZHLID.Location = new Point(1933, 158);
            buttonKZHLID.Margin = new Padding(5);
            buttonKZHLID.Name = "buttonKZHLID";
            buttonKZHLID.Size = new Size(190, 42);
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
            readLayout.Location = new Point(10, 37);
            readLayout.Name = "readLayout";
            readLayout.RowCount = 3;
            readLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            readLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            readLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            readLayout.Size = new Size(2188, 175);
            readLayout.TabIndex = 0;
            // 
            // label32
            // 
            label32.Anchor = AnchorStyles.Left;
            label32.AutoSize = true;
            label32.Location = new Point(5, 9);
            label32.Margin = new Padding(5, 0, 5, 0);
            label32.Name = "label32";
            label32.Size = new Size(222, 28);
            label32.TabIndex = 29;
            label32.Text = "电表地址或者终端地址";
            // 
            // tbxMeterTerminalAddr
            // 
            tbxMeterTerminalAddr.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxMeterTerminalAddr.Location = new Point(235, 6);
            tbxMeterTerminalAddr.Margin = new Padding(5);
            tbxMeterTerminalAddr.Name = "tbxMeterTerminalAddr";
            tbxMeterTerminalAddr.Size = new Size(750, 28);
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
            labelOadCategory.Location = new Point(5, 16);
            labelOadCategory.Margin = new Padding(5, 0, 5, 0);
            labelOadCategory.Name = "labelOadCategory";
            labelOadCategory.Size = new Size(93, 20);
            labelOadCategory.TabIndex = 36;
            labelOadCategory.Text = "OAD类型";
            // 
            // cbxSgccOadCategory
            // 
            cbxSgccOadCategory.Anchor = AnchorStyles.Left;
            cbxSgccOadCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxSgccOadCategory.FormattingEnabled = true;
            cbxSgccOadCategory.Location = new Point(235, 12);
            cbxSgccOadCategory.Margin = new Padding(5);
            cbxSgccOadCategory.Name = "cbxSgccOadCategory";
            cbxSgccOadCategory.Size = new Size(220, 28);
            cbxSgccOadCategory.TabIndex = 37;
            cbxSgccOadCategory.SelectedIndexChanged += cbxSgccOadCategory_SelectedIndexChanged;
            // 
            // labelOad
            // 
            labelOad.Anchor = AnchorStyles.Left;
            labelOad.AutoSize = true;
            labelOad.Location = new Point(485, 16);
            labelOad.Margin = new Padding(5, 0, 5, 0);
            labelOad.Name = "labelOad";
            labelOad.Size = new Size(93, 20);
            labelOad.TabIndex = 34;
            labelOad.Text = "OAD项目";
            // 
            // cbxSgccOAD
            // 
            cbxSgccOAD.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxSgccOAD.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxSgccOAD.DropDownWidth = 450;
            cbxSgccOAD.FormattingEnabled = true;
            cbxSgccOAD.Location = new Point(605, 12);
            cbxSgccOAD.Margin = new Padding(5);
            cbxSgccOAD.Name = "cbxSgccOAD";
            cbxSgccOAD.Size = new Size(450, 28);
            cbxSgccOAD.TabIndex = 31;
            // 
            // targetLayout
            // 
            targetLayout.Controls.Add(cbxSGCC_Terminal);
            targetLayout.Controls.Add(cbxSGCC_Meter);
            targetLayout.Dock = DockStyle.Fill;
            targetLayout.Location = new Point(233, 107);
            targetLayout.Name = "targetLayout";
            targetLayout.Size = new Size(754, 40);
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
            cbxSGCC_Terminal.Size = new Size(80, 32);
            cbxSGCC_Terminal.TabIndex = 32;
            cbxSGCC_Terminal.Text = "终端";
            cbxSGCC_Terminal.UseVisualStyleBackColor = true;
            // 
            // cbxSGCC_Meter
            // 
            cbxSGCC_Meter.AutoSize = true;
            cbxSGCC_Meter.Location = new Point(95, 5);
            cbxSGCC_Meter.Margin = new Padding(5);
            cbxSGCC_Meter.Name = "cbxSGCC_Meter";
            cbxSGCC_Meter.Size = new Size(80, 32);
            cbxSGCC_Meter.TabIndex = 33;
            cbxSGCC_Meter.Text = "电表";
            cbxSGCC_Meter.UseVisualStyleBackColor = true;
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
            // groupJjg596
            // 
            groupJjg596.Controls.Add(groupJjg596ErrorTime);
            groupJjg596.Controls.Add(groupJjg596StartTime);
            groupJjg596.Controls.Add(groupJjg596CreepTime);
            groupJjg596.Controls.Add(jjg596Layout);
            groupJjg596.Dock = DockStyle.Fill;
            groupJjg596.Location = new Point(14, 494);
            groupJjg596.Margin = new Padding(4);
            groupJjg596.Name = "groupJjg596";
            groupJjg596.Padding = new Padding(10);
            groupJjg596.Size = new Size(2208, 912);
            groupJjg596.TabIndex = 2;
            groupJjg596.TabStop = false;
            groupJjg596.Text = "JJG596-2026";
            // 
            // jjg596Layout
            // 
            jjg596Layout.ColumnCount = 9;
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            jjg596Layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            jjg596Layout.Controls.Add(labelJjg596MeasurementUnit, 0, 0);
            jjg596Layout.Controls.Add(cbxJjg596MeasurementUnit, 1, 0);
            jjg596Layout.Controls.Add(labelJjg596Voltage, 2, 0);
            jjg596Layout.Controls.Add(cbxJjg596Voltage, 3, 0);
            jjg596Layout.Controls.Add(labelJjg596Current, 4, 0);
            jjg596Layout.Controls.Add(cbxJjg596Current, 5, 0);
            jjg596Layout.Controls.Add(labelJjg596ActiveClass, 6, 0);
            jjg596Layout.Controls.Add(cbxJjg596ActiveClass, 7, 0);
            jjg596Layout.Controls.Add(labelJjg596ReactiveClass, 0, 1);
            jjg596Layout.Controls.Add(cbxJjg596ReactiveClass, 1, 1);
            jjg596Layout.Controls.Add(labelJjg596MeterConstant, 2, 1);
            jjg596Layout.Controls.Add(cbxJjg596MeterConstant, 3, 1);
            jjg596Layout.Controls.Add(labelJjg596AccessMode, 4, 1);
            jjg596Layout.Controls.Add(cbxJjg596AccessMode, 5, 1);
            jjg596Layout.Controls.Add(btnOpenJjg596Pdf, 6, 1);
            jjg596Layout.Controls.Add(labelJjg596Imin, 0, 2);
            jjg596Layout.Controls.Add(tbxJjg596Imin, 1, 2);
            jjg596Layout.Controls.Add(labelJjg596Itr, 2, 2);
            jjg596Layout.Controls.Add(tbxJjg596Itr, 3, 2);
            jjg596Layout.Controls.Add(labelJjg596Imax, 4, 2);
            jjg596Layout.Controls.Add(tbxJjg596Imax, 5, 2);
            jjg596Layout.Controls.Add(labelJjg596ReferenceCurrent, 6, 2);
            jjg596Layout.Controls.Add(tbxJjg596ReferenceCurrent, 7, 2);
            jjg596Layout.Controls.Add(labelJjg596Hint, 0, 3);
            jjg596Layout.Dock = DockStyle.Top;
            jjg596Layout.Location = new Point(10, 37);
            jjg596Layout.Name = "jjg596Layout";
            jjg596Layout.RowCount = 4;
            jjg596Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            jjg596Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            jjg596Layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            jjg596Layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            jjg596Layout.Size = new Size(2188, 265);
            jjg596Layout.TabIndex = 0;
            // 
            // labelJjg596MeasurementUnit
            // 
            labelJjg596MeasurementUnit.Anchor = AnchorStyles.Left;
            labelJjg596MeasurementUnit.AutoSize = true;
            labelJjg596MeasurementUnit.Location = new Point(5, 10);
            labelJjg596MeasurementUnit.Margin = new Padding(5, 0, 5, 0);
            labelJjg596MeasurementUnit.Name = "labelJjg596MeasurementUnit";
            labelJjg596MeasurementUnit.Size = new Size(79, 20);
            labelJjg596MeasurementUnit.TabIndex = 0;
            labelJjg596MeasurementUnit.Text = "测量单元";
            // 
            // cbxJjg596MeasurementUnit
            // 
            cbxJjg596MeasurementUnit.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596MeasurementUnit.FormattingEnabled = true;
            cbxJjg596MeasurementUnit.Location = new Point(125, 10);
            cbxJjg596MeasurementUnit.Margin = new Padding(5);
            cbxJjg596MeasurementUnit.Name = "cbxJjg596MeasurementUnit";
            cbxJjg596MeasurementUnit.Size = new Size(165, 28);
            cbxJjg596MeasurementUnit.TabIndex = 1;
            cbxJjg596MeasurementUnit.SelectedIndexChanged += cbxJjg596MeasurementUnit_SelectedIndexChanged;
            // 
            // labelJjg596Voltage
            // 
            labelJjg596Voltage.Anchor = AnchorStyles.Left;
            labelJjg596Voltage.AutoSize = true;
            labelJjg596Voltage.Location = new Point(275, 10);
            labelJjg596Voltage.Margin = new Padding(5, 0, 5, 0);
            labelJjg596Voltage.Name = "labelJjg596Voltage";
            labelJjg596Voltage.Size = new Size(49, 20);
            labelJjg596Voltage.TabIndex = 2;
            labelJjg596Voltage.Text = "电压";
            // 
            // cbxJjg596Voltage
            // 
            cbxJjg596Voltage.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596Voltage.FormattingEnabled = true;
            cbxJjg596Voltage.Location = new Point(370, 10);
            cbxJjg596Voltage.Margin = new Padding(5);
            cbxJjg596Voltage.Name = "cbxJjg596Voltage";
            cbxJjg596Voltage.Size = new Size(165, 28);
            cbxJjg596Voltage.TabIndex = 3;
            cbxJjg596Voltage.SelectedIndexChanged += cbxJjg596Voltage_SelectedIndexChanged;
            cbxJjg596Voltage.TextChanged += cbxJjg596Voltage_TextChanged;
            // 
            // labelJjg596Current
            // 
            labelJjg596Current.Anchor = AnchorStyles.Left;
            labelJjg596Current.AutoSize = true;
            labelJjg596Current.Location = new Point(545, 10);
            labelJjg596Current.Margin = new Padding(5, 0, 5, 0);
            labelJjg596Current.Name = "labelJjg596Current";
            labelJjg596Current.Size = new Size(49, 20);
            labelJjg596Current.TabIndex = 4;
            labelJjg596Current.Text = "电流";
            // 
            // cbxJjg596Current
            // 
            cbxJjg596Current.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596Current.FormattingEnabled = true;
            cbxJjg596Current.Location = new Point(640, 10);
            cbxJjg596Current.Margin = new Padding(5);
            cbxJjg596Current.Name = "cbxJjg596Current";
            cbxJjg596Current.Size = new Size(165, 28);
            cbxJjg596Current.TabIndex = 5;
            cbxJjg596Current.SelectedIndexChanged += cbxJjg596Current_SelectedIndexChanged;
            cbxJjg596Current.TextChanged += cbxJjg596Current_TextChanged;
            // 
            // labelJjg596ActiveClass
            // 
            labelJjg596ActiveClass.Anchor = AnchorStyles.Left;
            labelJjg596ActiveClass.AutoSize = true;
            labelJjg596ActiveClass.Location = new Point(815, 10);
            labelJjg596ActiveClass.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ActiveClass.Name = "labelJjg596ActiveClass";
            labelJjg596ActiveClass.Size = new Size(79, 20);
            labelJjg596ActiveClass.TabIndex = 6;
            labelJjg596ActiveClass.Text = "有功等级";
            // 
            // cbxJjg596ActiveClass
            // 
            cbxJjg596ActiveClass.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596ActiveClass.FormattingEnabled = true;
            cbxJjg596ActiveClass.Location = new Point(910, 10);
            cbxJjg596ActiveClass.Margin = new Padding(5);
            cbxJjg596ActiveClass.Name = "cbxJjg596ActiveClass";
            cbxJjg596ActiveClass.Size = new Size(165, 28);
            cbxJjg596ActiveClass.TabIndex = 7;
            cbxJjg596ActiveClass.SelectedIndexChanged += cbxJjg596ActiveClass_SelectedIndexChanged;
            cbxJjg596ActiveClass.TextChanged += cbxJjg596ActiveClass_TextChanged;
            // 
            // labelJjg596ReactiveClass
            // 
            labelJjg596ReactiveClass.Anchor = AnchorStyles.Left;
            labelJjg596ReactiveClass.AutoSize = true;
            labelJjg596ReactiveClass.Location = new Point(5, 58);
            labelJjg596ReactiveClass.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ReactiveClass.Name = "labelJjg596ReactiveClass";
            labelJjg596ReactiveClass.Size = new Size(79, 20);
            labelJjg596ReactiveClass.TabIndex = 8;
            labelJjg596ReactiveClass.Text = "无功等级";
            // 
            // cbxJjg596ReactiveClass
            // 
            cbxJjg596ReactiveClass.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596ReactiveClass.FormattingEnabled = true;
            cbxJjg596ReactiveClass.Location = new Point(100, 58);
            cbxJjg596ReactiveClass.Margin = new Padding(5);
            cbxJjg596ReactiveClass.Name = "cbxJjg596ReactiveClass";
            cbxJjg596ReactiveClass.Size = new Size(165, 28);
            cbxJjg596ReactiveClass.TabIndex = 9;
            // 
            // labelJjg596MeterConstant
            // 
            labelJjg596MeterConstant.Anchor = AnchorStyles.Left;
            labelJjg596MeterConstant.AutoSize = true;
            labelJjg596MeterConstant.Location = new Point(275, 58);
            labelJjg596MeterConstant.Margin = new Padding(5, 0, 5, 0);
            labelJjg596MeterConstant.Name = "labelJjg596MeterConstant";
            labelJjg596MeterConstant.Size = new Size(109, 20);
            labelJjg596MeterConstant.TabIndex = 10;
            labelJjg596MeterConstant.Text = "电能表常数";
            // 
            // cbxJjg596MeterConstant
            // 
            cbxJjg596MeterConstant.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596MeterConstant.FormattingEnabled = true;
            cbxJjg596MeterConstant.Location = new Point(370, 58);
            cbxJjg596MeterConstant.Margin = new Padding(5);
            cbxJjg596MeterConstant.Name = "cbxJjg596MeterConstant";
            cbxJjg596MeterConstant.Size = new Size(165, 28);
            cbxJjg596MeterConstant.TabIndex = 11;
            cbxJjg596MeterConstant.SelectedIndexChanged += cbxJjg596MeterConstant_SelectedIndexChanged;
            cbxJjg596MeterConstant.TextChanged += cbxJjg596MeterConstant_TextChanged;
            // 
            // labelJjg596AccessMode
            // 
            labelJjg596AccessMode.Anchor = AnchorStyles.Left;
            labelJjg596AccessMode.AutoSize = true;
            labelJjg596AccessMode.Location = new Point(545, 58);
            labelJjg596AccessMode.Margin = new Padding(5, 0, 5, 0);
            labelJjg596AccessMode.Name = "labelJjg596AccessMode";
            labelJjg596AccessMode.Size = new Size(79, 20);
            labelJjg596AccessMode.TabIndex = 12;
            labelJjg596AccessMode.Text = "接入方式";
            // 
            // cbxJjg596AccessMode
            // 
            cbxJjg596AccessMode.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596AccessMode.FormattingEnabled = true;
            cbxJjg596AccessMode.Location = new Point(640, 58);
            cbxJjg596AccessMode.Margin = new Padding(5);
            cbxJjg596AccessMode.Name = "cbxJjg596AccessMode";
            cbxJjg596AccessMode.Size = new Size(165, 28);
            cbxJjg596AccessMode.TabIndex = 13;
            cbxJjg596AccessMode.SelectedIndexChanged += cbxJjg596AccessMode_SelectedIndexChanged;
            cbxJjg596AccessMode.TextChanged += cbxJjg596AccessMode_TextChanged;
            // 
            // labelJjg596Imin
            // 
            labelJjg596Imin.Anchor = AnchorStyles.Left;
            labelJjg596Imin.AutoSize = true;
            labelJjg596Imin.Location = new Point(5, 106);
            labelJjg596Imin.Margin = new Padding(5, 0, 5, 0);
            labelJjg596Imin.Name = "labelJjg596Imin";
            labelJjg596Imin.Size = new Size(48, 20);
            labelJjg596Imin.TabIndex = 14;
            labelJjg596Imin.Text = "Imin";
            // 
            // tbxJjg596Imin
            // 
            tbxJjg596Imin.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596Imin.Location = new Point(100, 106);
            tbxJjg596Imin.Margin = new Padding(5);
            tbxJjg596Imin.Name = "tbxJjg596Imin";
            tbxJjg596Imin.ReadOnly = true;
            tbxJjg596Imin.Size = new Size(165, 28);
            tbxJjg596Imin.TabIndex = 15;
            // 
            // labelJjg596Itr
            // 
            labelJjg596Itr.Anchor = AnchorStyles.Left;
            labelJjg596Itr.AutoSize = true;
            labelJjg596Itr.Location = new Point(275, 106);
            labelJjg596Itr.Margin = new Padding(5, 0, 5, 0);
            labelJjg596Itr.Name = "labelJjg596Itr";
            labelJjg596Itr.Size = new Size(32, 20);
            labelJjg596Itr.TabIndex = 16;
            labelJjg596Itr.Text = "Itr";
            // 
            // tbxJjg596Itr
            // 
            tbxJjg596Itr.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596Itr.Location = new Point(370, 106);
            tbxJjg596Itr.Margin = new Padding(5);
            tbxJjg596Itr.Name = "tbxJjg596Itr";
            tbxJjg596Itr.ReadOnly = true;
            tbxJjg596Itr.Size = new Size(165, 28);
            tbxJjg596Itr.TabIndex = 17;
            // 
            // labelJjg596Imax
            // 
            labelJjg596Imax.Anchor = AnchorStyles.Left;
            labelJjg596Imax.AutoSize = true;
            labelJjg596Imax.Location = new Point(545, 106);
            labelJjg596Imax.Margin = new Padding(5, 0, 5, 0);
            labelJjg596Imax.Name = "labelJjg596Imax";
            labelJjg596Imax.Size = new Size(51, 20);
            labelJjg596Imax.TabIndex = 18;
            labelJjg596Imax.Text = "Imax";
            // 
            // tbxJjg596Imax
            // 
            tbxJjg596Imax.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596Imax.Location = new Point(640, 106);
            tbxJjg596Imax.Margin = new Padding(5);
            tbxJjg596Imax.Name = "tbxJjg596Imax";
            tbxJjg596Imax.ReadOnly = true;
            tbxJjg596Imax.Size = new Size(165, 28);
            tbxJjg596Imax.TabIndex = 19;
            // 
            // labelJjg596ReferenceCurrent
            // 
            labelJjg596ReferenceCurrent.Anchor = AnchorStyles.Left;
            labelJjg596ReferenceCurrent.AutoSize = true;
            labelJjg596ReferenceCurrent.Location = new Point(815, 106);
            labelJjg596ReferenceCurrent.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ReferenceCurrent.Name = "labelJjg596ReferenceCurrent";
            labelJjg596ReferenceCurrent.Size = new Size(53, 20);
            labelJjg596ReferenceCurrent.TabIndex = 20;
            labelJjg596ReferenceCurrent.Text = "Ib/In";
            // 
            // tbxJjg596ReferenceCurrent
            // 
            tbxJjg596ReferenceCurrent.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596ReferenceCurrent.Location = new Point(925, 106);
            tbxJjg596ReferenceCurrent.Margin = new Padding(5);
            tbxJjg596ReferenceCurrent.Name = "tbxJjg596ReferenceCurrent";
            tbxJjg596ReferenceCurrent.ReadOnly = true;
            tbxJjg596ReferenceCurrent.Size = new Size(165, 28);
            tbxJjg596ReferenceCurrent.TabIndex = 21;
            // 
            // labelJjg596Hint
            // 
            jjg596Layout.SetColumnSpan(labelJjg596Hint, 9);
            labelJjg596Hint.Dock = DockStyle.Fill;
            labelJjg596Hint.ForeColor = Color.White;
            labelJjg596Hint.Location = new Point(5, 144);
            labelJjg596Hint.Margin = new Padding(5, 0, 5, 0);
            labelJjg596Hint.Name = "labelJjg596Hint";
            labelJjg596Hint.Size = new Size(2178, 121);
            labelJjg596Hint.TabIndex = 22;
            labelJjg596Hint.Text = "所有下拉框默认展示第一项，并支持直接输入。\r\n电流选择如 0.25-0.5(60)A 时，会自动拆分 Imin、Itr、Imax，并根据接入方式计算 Ib/In。";
            labelJjg596Hint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnOpenJjg596Pdf
            // 
            jjg596Layout.SetColumnSpan(btnOpenJjg596Pdf, 2);
            btnOpenJjg596Pdf.Anchor = AnchorStyles.Left;
            btnOpenJjg596Pdf.Location = new Point(820, 53);
            btnOpenJjg596Pdf.Margin = new Padding(5);
            btnOpenJjg596Pdf.Name = "btnOpenJjg596Pdf";
            btnOpenJjg596Pdf.Size = new Size(140, 36);
            btnOpenJjg596Pdf.TabIndex = 23;
            btnOpenJjg596Pdf.Text = "打开规程PDF";
            btnOpenJjg596Pdf.UseVisualStyleBackColor = true;
            btnOpenJjg596Pdf.Click += btnOpenJjg596Pdf_Click;
            // 
            // groupJjg596StartTime
            // 
            groupJjg596StartTime.Controls.Add(tableJjg596StartTime);
            groupJjg596StartTime.Dock = DockStyle.Top;
            groupJjg596StartTime.Location = new Point(10, 442);
            groupJjg596StartTime.Name = "groupJjg596StartTime";
            groupJjg596StartTime.Size = new Size(2188, 180);
            groupJjg596StartTime.TabIndex = 2;
            groupJjg596StartTime.TabStop = false;
            groupJjg596StartTime.Text = "启动时间计算";
            // 
            // tableJjg596StartTime
            // 
            tableJjg596StartTime.ColumnCount = 8;
            tableJjg596StartTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            tableJjg596StartTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            tableJjg596StartTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            tableJjg596StartTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            tableJjg596StartTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            tableJjg596StartTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            tableJjg596StartTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            tableJjg596StartTime.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableJjg596StartTime.Controls.Add(labelJjg596StartFormula, 0, 0);
            tableJjg596StartTime.Controls.Add(labelJjg596StartDescription, 0, 1);
            tableJjg596StartTime.Controls.Add(labelJjg596StartCurrent, 0, 2);
            tableJjg596StartTime.Controls.Add(tbxJjg596StartCurrent, 1, 2);
            tableJjg596StartTime.Controls.Add(labelJjg596StartPst, 2, 2);
            tableJjg596StartTime.Controls.Add(tbxJjg596StartPst, 3, 2);
            tableJjg596StartTime.Controls.Add(labelJjg596StartTimeLower, 4, 2);
            tableJjg596StartTime.Controls.Add(tbxJjg596StartTimeLower, 5, 2);
            tableJjg596StartTime.Controls.Add(labelJjg596StartTimeUpper, 6, 2);
            tableJjg596StartTime.Controls.Add(tbxJjg596StartTimeUpper, 7, 2);
            tableJjg596StartTime.Dock = DockStyle.Fill;
            tableJjg596StartTime.Location = new Point(3, 26);
            tableJjg596StartTime.Name = "tableJjg596StartTime";
            tableJjg596StartTime.RowCount = 3;
            tableJjg596StartTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableJjg596StartTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 62F));
            tableJjg596StartTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            tableJjg596StartTime.Size = new Size(2182, 151);
            tableJjg596StartTime.TabIndex = 0;
            // 
            // labelJjg596StartFormula
            // 
            tableJjg596StartTime.SetColumnSpan(labelJjg596StartFormula, 8);
            labelJjg596StartFormula.Dock = DockStyle.Fill;
            labelJjg596StartFormula.Location = new Point(5, 0);
            labelJjg596StartFormula.Margin = new Padding(5, 0, 5, 0);
            labelJjg596StartFormula.Name = "labelJjg596StartFormula";
            labelJjg596StartFormula.Size = new Size(2172, 34);
            labelJjg596StartFormula.TabIndex = 0;
            labelJjg596StartFormula.Text = "计算公式：(1-Est)×K ≤ Tst ≤ (1+Est)×K，K = 3.6×10^6 / (C×Pst×Ki×Ku)";
            labelJjg596StartFormula.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelJjg596StartDescription
            // 
            tableJjg596StartTime.SetColumnSpan(labelJjg596StartDescription, 8);
            labelJjg596StartDescription.Dock = DockStyle.Fill;
            labelJjg596StartDescription.Location = new Point(5, 34);
            labelJjg596StartDescription.Margin = new Padding(5, 0, 5, 0);
            labelJjg596StartDescription.Name = "labelJjg596StartDescription";
            labelJjg596StartDescription.Size = new Size(2172, 62);
            labelJjg596StartDescription.TabIndex = 1;
            labelJjg596StartDescription.Text = "Tst：相邻两个脉冲间隔(s)；Est：最大允许误差绝对值，A=2.5%、B=1.5%、C=1.0%、D=0.4%；C：仪表常数(imp/kWh)。\r\nPst = U×Ist×d，d：单相=1、三相三线=2、三相四线=3；Ki、Ku 默认按 1 计算。";
            labelJjg596StartDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelJjg596StartCurrent
            // 
            labelJjg596StartCurrent.Anchor = AnchorStyles.Left;
            labelJjg596StartCurrent.AutoSize = true;
            labelJjg596StartCurrent.Location = new Point(5, 104);
            labelJjg596StartCurrent.Margin = new Padding(5, 0, 5, 0);
            labelJjg596StartCurrent.Name = "labelJjg596StartCurrent";
            labelJjg596StartCurrent.Size = new Size(29, 20);
            labelJjg596StartCurrent.TabIndex = 2;
            labelJjg596StartCurrent.Text = "Ist";
            // 
            // tbxJjg596StartCurrent
            // 
            tbxJjg596StartCurrent.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596StartCurrent.Location = new Point(95, 100);
            tbxJjg596StartCurrent.Margin = new Padding(5);
            tbxJjg596StartCurrent.Name = "tbxJjg596StartCurrent";
            tbxJjg596StartCurrent.ReadOnly = true;
            tbxJjg596StartCurrent.Size = new Size(170, 28);
            tbxJjg596StartCurrent.TabIndex = 3;
            tbxJjg596StartCurrent.TextChanged += tbxJjg596StartCurrent_TextChanged;
            // 
            // labelJjg596StartPst
            // 
            labelJjg596StartPst.Anchor = AnchorStyles.Left;
            labelJjg596StartPst.AutoSize = true;
            labelJjg596StartPst.Location = new Point(275, 104);
            labelJjg596StartPst.Margin = new Padding(5, 0, 5, 0);
            labelJjg596StartPst.Name = "labelJjg596StartPst";
            labelJjg596StartPst.Size = new Size(34, 20);
            labelJjg596StartPst.TabIndex = 4;
            labelJjg596StartPst.Text = "Pst";
            // 
            // tbxJjg596StartPst
            // 
            tbxJjg596StartPst.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596StartPst.Location = new Point(365, 100);
            tbxJjg596StartPst.Margin = new Padding(5);
            tbxJjg596StartPst.Name = "tbxJjg596StartPst";
            tbxJjg596StartPst.ReadOnly = true;
            tbxJjg596StartPst.Size = new Size(210, 28);
            tbxJjg596StartPst.TabIndex = 5;
            // 
            // labelJjg596StartTimeLower
            // 
            labelJjg596StartTimeLower.Anchor = AnchorStyles.Left;
            labelJjg596StartTimeLower.AutoSize = true;
            labelJjg596StartTimeLower.Location = new Point(585, 104);
            labelJjg596StartTimeLower.Margin = new Padding(5, 0, 5, 0);
            labelJjg596StartTimeLower.Name = "labelJjg596StartTimeLower";
            labelJjg596StartTimeLower.Size = new Size(75, 20);
            labelJjg596StartTimeLower.TabIndex = 6;
            labelJjg596StartTimeLower.Text = "Tst下限";
            // 
            // tbxJjg596StartTimeLower
            // 
            tbxJjg596StartTimeLower.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596StartTimeLower.Location = new Point(685, 100);
            tbxJjg596StartTimeLower.Margin = new Padding(5);
            tbxJjg596StartTimeLower.Name = "tbxJjg596StartTimeLower";
            tbxJjg596StartTimeLower.ReadOnly = true;
            tbxJjg596StartTimeLower.Size = new Size(210, 28);
            tbxJjg596StartTimeLower.TabIndex = 7;
            // 
            // labelJjg596StartTimeUpper
            // 
            labelJjg596StartTimeUpper.Anchor = AnchorStyles.Left;
            labelJjg596StartTimeUpper.AutoSize = true;
            labelJjg596StartTimeUpper.Location = new Point(905, 104);
            labelJjg596StartTimeUpper.Margin = new Padding(5, 0, 5, 0);
            labelJjg596StartTimeUpper.Name = "labelJjg596StartTimeUpper";
            labelJjg596StartTimeUpper.Size = new Size(75, 20);
            labelJjg596StartTimeUpper.TabIndex = 8;
            labelJjg596StartTimeUpper.Text = "Tst上限";
            // 
            // tbxJjg596StartTimeUpper
            // 
            tbxJjg596StartTimeUpper.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596StartTimeUpper.Location = new Point(1005, 100);
            tbxJjg596StartTimeUpper.Margin = new Padding(5);
            tbxJjg596StartTimeUpper.Name = "tbxJjg596StartTimeUpper";
            tbxJjg596StartTimeUpper.ReadOnly = true;
            tbxJjg596StartTimeUpper.Size = new Size(1172, 28);
            tbxJjg596StartTimeUpper.TabIndex = 9;
            // 
            // groupJjg596ErrorTime
            // 
            groupJjg596ErrorTime.Controls.Add(tableJjg596ErrorTime);
            groupJjg596ErrorTime.Dock = DockStyle.Top;
            groupJjg596ErrorTime.Location = new Point(10, 622);
            groupJjg596ErrorTime.Name = "groupJjg596ErrorTime";
            groupJjg596ErrorTime.Size = new Size(2188, 280);
            groupJjg596ErrorTime.TabIndex = 3;
            groupJjg596ErrorTime.TabStop = false;
            groupJjg596ErrorTime.Text = "基本误差时间计算";
            // 
            // tableJjg596ErrorTime
            // 
            tableJjg596ErrorTime.ColumnCount = 8;
            tableJjg596ErrorTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            tableJjg596ErrorTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            tableJjg596ErrorTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            tableJjg596ErrorTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            tableJjg596ErrorTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 95F));
            tableJjg596ErrorTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            tableJjg596ErrorTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125F));
            tableJjg596ErrorTime.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorFormula, 0, 0);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorDescription, 0, 1);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorPowerType, 0, 2);
            tableJjg596ErrorTime.Controls.Add(cbxJjg596ErrorPowerType, 1, 2);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorPowerFactor, 2, 2);
            tableJjg596ErrorTime.Controls.Add(cbxJjg596ErrorPowerFactor, 3, 2);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorPhase, 4, 2);
            tableJjg596ErrorTime.Controls.Add(cbxJjg596ErrorPhase, 5, 2);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorCurrent, 6, 2);
            tableJjg596ErrorTime.Controls.Add(tbxJjg596ErrorCurrent, 7, 2);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorPulseCount, 0, 3);
            tableJjg596ErrorTime.Controls.Add(tbxJjg596ErrorPulseCount, 1, 3);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorPower, 2, 3);
            tableJjg596ErrorTime.Controls.Add(tbxJjg596ErrorPower, 3, 3);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorTime, 4, 3);
            tableJjg596ErrorTime.Controls.Add(tbxJjg596ErrorTime, 5, 3);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorCorrectedPulseCount, 6, 3);
            tableJjg596ErrorTime.Controls.Add(tbxJjg596ErrorCorrectedPulseCount, 7, 3);
            tableJjg596ErrorTime.Controls.Add(labelJjg596ErrorHint, 0, 4);
            tableJjg596ErrorTime.Dock = DockStyle.Fill;
            tableJjg596ErrorTime.Location = new Point(3, 26);
            tableJjg596ErrorTime.Name = "tableJjg596ErrorTime";
            tableJjg596ErrorTime.RowCount = 5;
            tableJjg596ErrorTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableJjg596ErrorTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            tableJjg596ErrorTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            tableJjg596ErrorTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            tableJjg596ErrorTime.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableJjg596ErrorTime.Size = new Size(2182, 251);
            tableJjg596ErrorTime.TabIndex = 0;
            // 
            // labelJjg596ErrorFormula
            // 
            tableJjg596ErrorTime.SetColumnSpan(labelJjg596ErrorFormula, 8);
            labelJjg596ErrorFormula.Dock = DockStyle.Fill;
            labelJjg596ErrorFormula.Location = new Point(5, 0);
            labelJjg596ErrorFormula.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorFormula.Name = "labelJjg596ErrorFormula";
            labelJjg596ErrorFormula.Size = new Size(2172, 34);
            labelJjg596ErrorFormula.TabIndex = 0;
            labelJjg596ErrorFormula.Text = "计算公式：T = (3.6×10^6×N) / (C×Ki×Ku×P)";
            labelJjg596ErrorFormula.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelJjg596ErrorDescription
            // 
            tableJjg596ErrorTime.SetColumnSpan(labelJjg596ErrorDescription, 8);
            labelJjg596ErrorDescription.Dock = DockStyle.Fill;
            labelJjg596ErrorDescription.Location = new Point(5, 34);
            labelJjg596ErrorDescription.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorDescription.Name = "labelJjg596ErrorDescription";
            labelJjg596ErrorDescription.Size = new Size(2172, 70);
            labelJjg596ErrorDescription.TabIndex = 1;
            labelJjg596ErrorDescription.Text = "P：标准功率(W)。有功按 P=系数×U×I×cosφ，无功按 P=系数×U×I×sinφ；单相系数=1，三相系数=√3。\r\n三相电压按线电压取值，如 3×220/380V 取 380；C：仪表常数；Ki、Ku 默认按 1。";
            labelJjg596ErrorDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelJjg596ErrorPowerType
            // 
            labelJjg596ErrorPowerType.Anchor = AnchorStyles.Left;
            labelJjg596ErrorPowerType.AutoSize = true;
            labelJjg596ErrorPowerType.Location = new Point(5, 114);
            labelJjg596ErrorPowerType.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorPowerType.Name = "labelJjg596ErrorPowerType";
            labelJjg596ErrorPowerType.Size = new Size(79, 20);
            labelJjg596ErrorPowerType.TabIndex = 2;
            labelJjg596ErrorPowerType.Text = "功率类型";
            // 
            // cbxJjg596ErrorPowerType
            // 
            cbxJjg596ErrorPowerType.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596ErrorPowerType.FormattingEnabled = true;
            cbxJjg596ErrorPowerType.Location = new Point(100, 110);
            cbxJjg596ErrorPowerType.Margin = new Padding(5);
            cbxJjg596ErrorPowerType.Name = "cbxJjg596ErrorPowerType";
            cbxJjg596ErrorPowerType.Size = new Size(170, 28);
            cbxJjg596ErrorPowerType.TabIndex = 3;
            cbxJjg596ErrorPowerType.SelectedIndexChanged += cbxJjg596ErrorPowerType_SelectedIndexChanged;
            // 
            // labelJjg596ErrorPowerFactor
            // 
            labelJjg596ErrorPowerFactor.Anchor = AnchorStyles.Left;
            labelJjg596ErrorPowerFactor.AutoSize = true;
            labelJjg596ErrorPowerFactor.Location = new Point(280, 114);
            labelJjg596ErrorPowerFactor.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorPowerFactor.Name = "labelJjg596ErrorPowerFactor";
            labelJjg596ErrorPowerFactor.Size = new Size(79, 20);
            labelJjg596ErrorPowerFactor.TabIndex = 4;
            labelJjg596ErrorPowerFactor.Text = "功率因数";
            // 
            // cbxJjg596ErrorPowerFactor
            // 
            cbxJjg596ErrorPowerFactor.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596ErrorPowerFactor.FormattingEnabled = true;
            cbxJjg596ErrorPowerFactor.Location = new Point(375, 110);
            cbxJjg596ErrorPowerFactor.Margin = new Padding(5);
            cbxJjg596ErrorPowerFactor.Name = "cbxJjg596ErrorPowerFactor";
            cbxJjg596ErrorPowerFactor.Size = new Size(170, 28);
            cbxJjg596ErrorPowerFactor.TabIndex = 5;
            cbxJjg596ErrorPowerFactor.SelectedIndexChanged += cbxJjg596ErrorPowerFactor_SelectedIndexChanged;
            cbxJjg596ErrorPowerFactor.TextChanged += cbxJjg596ErrorPowerFactor_TextChanged;
            // 
            // labelJjg596ErrorPhase
            // 
            labelJjg596ErrorPhase.Anchor = AnchorStyles.Left;
            labelJjg596ErrorPhase.AutoSize = true;
            labelJjg596ErrorPhase.Location = new Point(555, 114);
            labelJjg596ErrorPhase.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorPhase.Name = "labelJjg596ErrorPhase";
            labelJjg596ErrorPhase.Size = new Size(49, 20);
            labelJjg596ErrorPhase.TabIndex = 6;
            labelJjg596ErrorPhase.Text = "相别";
            // 
            // cbxJjg596ErrorPhase
            // 
            cbxJjg596ErrorPhase.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxJjg596ErrorPhase.FormattingEnabled = true;
            cbxJjg596ErrorPhase.Location = new Point(650, 110);
            cbxJjg596ErrorPhase.Margin = new Padding(5);
            cbxJjg596ErrorPhase.Name = "cbxJjg596ErrorPhase";
            cbxJjg596ErrorPhase.Size = new Size(210, 28);
            cbxJjg596ErrorPhase.TabIndex = 7;
            cbxJjg596ErrorPhase.SelectedIndexChanged += cbxJjg596ErrorPhase_SelectedIndexChanged;
            cbxJjg596ErrorPhase.TextChanged += cbxJjg596ErrorPhase_TextChanged;
            // 
            // labelJjg596ErrorCurrent
            // 
            labelJjg596ErrorCurrent.Anchor = AnchorStyles.Left;
            labelJjg596ErrorCurrent.AutoSize = true;
            labelJjg596ErrorCurrent.Location = new Point(875, 114);
            labelJjg596ErrorCurrent.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorCurrent.Name = "labelJjg596ErrorCurrent";
            labelJjg596ErrorCurrent.Size = new Size(49, 20);
            labelJjg596ErrorCurrent.TabIndex = 8;
            labelJjg596ErrorCurrent.Text = "电流";
            // 
            // tbxJjg596ErrorCurrent
            // 
            tbxJjg596ErrorCurrent.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596ErrorCurrent.Location = new Point(1000, 110);
            tbxJjg596ErrorCurrent.Margin = new Padding(5);
            tbxJjg596ErrorCurrent.Name = "tbxJjg596ErrorCurrent";
            tbxJjg596ErrorCurrent.Size = new Size(1177, 28);
            tbxJjg596ErrorCurrent.TabIndex = 9;
            tbxJjg596ErrorCurrent.TextChanged += tbxJjg596ErrorCurrent_TextChanged;
            // 
            // labelJjg596ErrorPulseCount
            // 
            labelJjg596ErrorPulseCount.Anchor = AnchorStyles.Left;
            labelJjg596ErrorPulseCount.AutoSize = true;
            labelJjg596ErrorPulseCount.Location = new Point(5, 156);
            labelJjg596ErrorPulseCount.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorPulseCount.Name = "labelJjg596ErrorPulseCount";
            labelJjg596ErrorPulseCount.Size = new Size(21, 20);
            labelJjg596ErrorPulseCount.TabIndex = 10;
            labelJjg596ErrorPulseCount.Text = "N";
            // 
            // tbxJjg596ErrorPulseCount
            // 
            tbxJjg596ErrorPulseCount.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596ErrorPulseCount.Location = new Point(100, 153);
            tbxJjg596ErrorPulseCount.Margin = new Padding(5);
            tbxJjg596ErrorPulseCount.Name = "tbxJjg596ErrorPulseCount";
            tbxJjg596ErrorPulseCount.Size = new Size(170, 28);
            tbxJjg596ErrorPulseCount.TabIndex = 11;
            tbxJjg596ErrorPulseCount.TextChanged += tbxJjg596ErrorPulseCount_TextChanged;
            // 
            // labelJjg596ErrorPower
            // 
            labelJjg596ErrorPower.Anchor = AnchorStyles.Left;
            labelJjg596ErrorPower.AutoSize = true;
            labelJjg596ErrorPower.Location = new Point(280, 156);
            labelJjg596ErrorPower.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorPower.Name = "labelJjg596ErrorPower";
            labelJjg596ErrorPower.Size = new Size(22, 20);
            labelJjg596ErrorPower.TabIndex = 12;
            labelJjg596ErrorPower.Text = "P";
            // 
            // tbxJjg596ErrorPower
            // 
            tbxJjg596ErrorPower.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596ErrorPower.Location = new Point(375, 153);
            tbxJjg596ErrorPower.Margin = new Padding(5);
            tbxJjg596ErrorPower.Name = "tbxJjg596ErrorPower";
            tbxJjg596ErrorPower.ReadOnly = true;
            tbxJjg596ErrorPower.Size = new Size(170, 28);
            tbxJjg596ErrorPower.TabIndex = 13;
            // 
            // labelJjg596ErrorTime
            // 
            labelJjg596ErrorTime.Anchor = AnchorStyles.Left;
            labelJjg596ErrorTime.AutoSize = true;
            labelJjg596ErrorTime.Location = new Point(555, 156);
            labelJjg596ErrorTime.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorTime.Name = "labelJjg596ErrorTime";
            labelJjg596ErrorTime.Size = new Size(20, 20);
            labelJjg596ErrorTime.TabIndex = 14;
            labelJjg596ErrorTime.Text = "T";
            // 
            // tbxJjg596ErrorTime
            // 
            tbxJjg596ErrorTime.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596ErrorTime.Location = new Point(650, 153);
            tbxJjg596ErrorTime.Margin = new Padding(5);
            tbxJjg596ErrorTime.Name = "tbxJjg596ErrorTime";
            tbxJjg596ErrorTime.ReadOnly = true;
            tbxJjg596ErrorTime.Size = new Size(210, 28);
            tbxJjg596ErrorTime.TabIndex = 15;
            // 
            // labelJjg596ErrorCorrectedPulseCount
            // 
            labelJjg596ErrorCorrectedPulseCount.Anchor = AnchorStyles.Left;
            labelJjg596ErrorCorrectedPulseCount.AutoSize = true;
            labelJjg596ErrorCorrectedPulseCount.Location = new Point(875, 156);
            labelJjg596ErrorCorrectedPulseCount.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorCorrectedPulseCount.Name = "labelJjg596ErrorCorrectedPulseCount";
            labelJjg596ErrorCorrectedPulseCount.Size = new Size(84, 20);
            labelJjg596ErrorCorrectedPulseCount.TabIndex = 16;
            labelJjg596ErrorCorrectedPulseCount.Text = "修正后N";
            // 
            // tbxJjg596ErrorCorrectedPulseCount
            // 
            tbxJjg596ErrorCorrectedPulseCount.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596ErrorCorrectedPulseCount.Location = new Point(1000, 153);
            tbxJjg596ErrorCorrectedPulseCount.Margin = new Padding(5);
            tbxJjg596ErrorCorrectedPulseCount.Name = "tbxJjg596ErrorCorrectedPulseCount";
            tbxJjg596ErrorCorrectedPulseCount.ReadOnly = true;
            tbxJjg596ErrorCorrectedPulseCount.Size = new Size(1177, 28);
            tbxJjg596ErrorCorrectedPulseCount.TabIndex = 17;
            // 
            // labelJjg596ErrorHint
            // 
            tableJjg596ErrorTime.SetColumnSpan(labelJjg596ErrorHint, 8);
            labelJjg596ErrorHint.Dock = DockStyle.Fill;
            labelJjg596ErrorHint.ForeColor = Color.Red;
            labelJjg596ErrorHint.Location = new Point(5, 188);
            labelJjg596ErrorHint.Margin = new Padding(5, 0, 5, 0);
            labelJjg596ErrorHint.Name = "labelJjg596ErrorHint";
            labelJjg596ErrorHint.Size = new Size(2172, 63);
            labelJjg596ErrorHint.TabIndex = 14;
            labelJjg596ErrorHint.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // groupJjg596CreepTime
            // 
            groupJjg596CreepTime.Controls.Add(tableJjg596CreepTime);
            groupJjg596CreepTime.Dock = DockStyle.Top;
            groupJjg596CreepTime.Location = new Point(10, 302);
            groupJjg596CreepTime.Name = "groupJjg596CreepTime";
            groupJjg596CreepTime.Size = new Size(2188, 140);
            groupJjg596CreepTime.TabIndex = 1;
            groupJjg596CreepTime.TabStop = false;
            groupJjg596CreepTime.Text = "潜动时间计算";
            // 
            // tableJjg596CreepTime
            // 
            tableJjg596CreepTime.ColumnCount = 6;
            tableJjg596CreepTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableJjg596CreepTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            tableJjg596CreepTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableJjg596CreepTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            tableJjg596CreepTime.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableJjg596CreepTime.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableJjg596CreepTime.Controls.Add(labelJjg596CreepFormula, 0, 0);
            tableJjg596CreepTime.Controls.Add(labelJjg596CreepDescription, 0, 1);
            tableJjg596CreepTime.Controls.Add(labelJjg596CreepHours, 0, 2);
            tableJjg596CreepTime.Controls.Add(tbxJjg596CreepHours, 1, 2);
            tableJjg596CreepTime.Controls.Add(labelJjg596CreepMinutes, 2, 2);
            tableJjg596CreepTime.Controls.Add(tbxJjg596CreepMinutes, 3, 2);
            tableJjg596CreepTime.Controls.Add(labelJjg596CreepSeconds, 4, 2);
            tableJjg596CreepTime.Controls.Add(tbxJjg596CreepSeconds, 5, 2);
            tableJjg596CreepTime.Dock = DockStyle.Fill;
            tableJjg596CreepTime.Location = new Point(3, 26);
            tableJjg596CreepTime.Name = "tableJjg596CreepTime";
            tableJjg596CreepTime.RowCount = 3;
            tableJjg596CreepTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableJjg596CreepTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            tableJjg596CreepTime.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            tableJjg596CreepTime.Size = new Size(2182, 111);
            tableJjg596CreepTime.TabIndex = 0;
            // 
            // labelJjg596CreepFormula
            // 
            tableJjg596CreepTime.SetColumnSpan(labelJjg596CreepFormula, 6);
            labelJjg596CreepFormula.Dock = DockStyle.Fill;
            labelJjg596CreepFormula.Location = new Point(5, 0);
            labelJjg596CreepFormula.Margin = new Padding(5, 0, 5, 0);
            labelJjg596CreepFormula.Name = "labelJjg596CreepFormula";
            labelJjg596CreepFormula.Size = new Size(2172, 34);
            labelJjg596CreepFormula.TabIndex = 0;
            labelJjg596CreepFormula.Text = "计算公式：Δt = (100×10^3) / (1.1×b×C×d×U×Imin)";
            labelJjg596CreepFormula.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelJjg596CreepDescription
            // 
            tableJjg596CreepTime.SetColumnSpan(labelJjg596CreepDescription, 6);
            labelJjg596CreepDescription.Dock = DockStyle.Fill;
            labelJjg596CreepDescription.Location = new Point(5, 34);
            labelJjg596CreepDescription.Margin = new Padding(5, 0, 5, 0);
            labelJjg596CreepDescription.Name = "labelJjg596CreepDescription";
            labelJjg596CreepDescription.Size = new Size(2172, 54);
            labelJjg596CreepDescription.TabIndex = 1;
            labelJjg596CreepDescription.Text = "b：Imin 时的基本最大允许误差极限(%)；k：电能表常数(imp/kWh)；Unom：标称电压(V)；\r\nImin：最小电流(A)。三相电压按相电压取值，如 3×220/380V 取 220。";
            labelJjg596CreepDescription.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelJjg596CreepHours
            // 
            labelJjg596CreepHours.Anchor = AnchorStyles.Left;
            labelJjg596CreepHours.AutoSize = true;
            labelJjg596CreepHours.Location = new Point(5, 95);
            labelJjg596CreepHours.Margin = new Padding(5, 0, 5, 0);
            labelJjg596CreepHours.Name = "labelJjg596CreepHours";
            labelJjg596CreepHours.Size = new Size(84, 20);
            labelJjg596CreepHours.TabIndex = 2;
            labelJjg596CreepHours.Text = "Δt (小时)";
            // 
            // tbxJjg596CreepHours
            // 
            tbxJjg596CreepHours.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596CreepHours.Location = new Point(125, 91);
            tbxJjg596CreepHours.Margin = new Padding(5);
            tbxJjg596CreepHours.Name = "tbxJjg596CreepHours";
            tbxJjg596CreepHours.ReadOnly = true;
            tbxJjg596CreepHours.Size = new Size(210, 28);
            tbxJjg596CreepHours.TabIndex = 3;
            // 
            // labelJjg596CreepMinutes
            // 
            labelJjg596CreepMinutes.Anchor = AnchorStyles.Left;
            labelJjg596CreepMinutes.AutoSize = true;
            labelJjg596CreepMinutes.Location = new Point(345, 95);
            labelJjg596CreepMinutes.Margin = new Padding(5, 0, 5, 0);
            labelJjg596CreepMinutes.Name = "labelJjg596CreepMinutes";
            labelJjg596CreepMinutes.Size = new Size(95, 20);
            labelJjg596CreepMinutes.TabIndex = 4;
            labelJjg596CreepMinutes.Text = "Δt (分钟)";
            // 
            // tbxJjg596CreepMinutes
            // 
            tbxJjg596CreepMinutes.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596CreepMinutes.Location = new Point(465, 91);
            tbxJjg596CreepMinutes.Margin = new Padding(5);
            tbxJjg596CreepMinutes.Name = "tbxJjg596CreepMinutes";
            tbxJjg596CreepMinutes.ReadOnly = true;
            tbxJjg596CreepMinutes.Size = new Size(190, 28);
            tbxJjg596CreepMinutes.TabIndex = 5;
            // 
            // labelJjg596CreepSeconds
            // 
            labelJjg596CreepSeconds.Anchor = AnchorStyles.Left;
            labelJjg596CreepSeconds.AutoSize = true;
            labelJjg596CreepSeconds.Location = new Point(665, 95);
            labelJjg596CreepSeconds.Margin = new Padding(5, 0, 5, 0);
            labelJjg596CreepSeconds.Name = "labelJjg596CreepSeconds";
            labelJjg596CreepSeconds.Size = new Size(80, 20);
            labelJjg596CreepSeconds.TabIndex = 6;
            labelJjg596CreepSeconds.Text = "Δt (秒)";
            // 
            // tbxJjg596CreepSeconds
            // 
            tbxJjg596CreepSeconds.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxJjg596CreepSeconds.Location = new Point(785, 91);
            tbxJjg596CreepSeconds.Margin = new Padding(5);
            tbxJjg596CreepSeconds.Name = "tbxJjg596CreepSeconds";
            tbxJjg596CreepSeconds.ReadOnly = true;
            tbxJjg596CreepSeconds.Size = new Size(1392, 28);
            tbxJjg596CreepSeconds.TabIndex = 7;
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
            AutoScaleDimensions = new SizeF(10F, 20F);
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
            groupRead.ResumeLayout(false);
            readLayout.ResumeLayout(false);
            readLayout.PerformLayout();
            oadLineLayout.ResumeLayout(false);
            oadLineLayout.PerformLayout();
            targetLayout.ResumeLayout(false);
            targetLayout.PerformLayout();
            groupJjg596.ResumeLayout(false);
            groupJjg596StartTime.ResumeLayout(false);
            tableJjg596StartTime.ResumeLayout(false);
            tableJjg596StartTime.PerformLayout();
            groupJjg596ErrorTime.ResumeLayout(false);
            tableJjg596ErrorTime.ResumeLayout(false);
            tableJjg596ErrorTime.PerformLayout();
            jjg596Layout.ResumeLayout(false);
            groupJjg596CreepTime.ResumeLayout(false);
            tableJjg596CreepTime.ResumeLayout(false);
            tableJjg596CreepTime.PerformLayout();
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
        private GroupBox groupJjg596;
        private TableLayoutPanel jjg596Layout;
        private Label labelJjg596MeasurementUnit;
        private ComboBox cbxJjg596MeasurementUnit;
        private Label labelJjg596Voltage;
        private ComboBox cbxJjg596Voltage;
        private Label labelJjg596Current;
        private ComboBox cbxJjg596Current;
        private Label labelJjg596ActiveClass;
        private ComboBox cbxJjg596ActiveClass;
        private Label labelJjg596ReactiveClass;
        private ComboBox cbxJjg596ReactiveClass;
        private Label labelJjg596MeterConstant;
        private ComboBox cbxJjg596MeterConstant;
        private Label labelJjg596AccessMode;
        private ComboBox cbxJjg596AccessMode;
        private Label labelJjg596Imin;
        private TextBox tbxJjg596Imin;
        private Label labelJjg596Itr;
        private TextBox tbxJjg596Itr;
        private Label labelJjg596Imax;
        private TextBox tbxJjg596Imax;
        private Label labelJjg596ReferenceCurrent;
        private TextBox tbxJjg596ReferenceCurrent;
        private Label labelJjg596Hint;
        private Button btnOpenJjg596Pdf;
        private GroupBox groupJjg596StartTime;
        private TableLayoutPanel tableJjg596StartTime;
        private Label labelJjg596StartFormula;
        private Label labelJjg596StartDescription;
        private Label labelJjg596StartCurrent;
        private TextBox tbxJjg596StartCurrent;
        private Label labelJjg596StartPst;
        private TextBox tbxJjg596StartPst;
        private Label labelJjg596StartTimeLower;
        private TextBox tbxJjg596StartTimeLower;
        private Label labelJjg596StartTimeUpper;
        private TextBox tbxJjg596StartTimeUpper;
        private GroupBox groupJjg596ErrorTime;
        private TableLayoutPanel tableJjg596ErrorTime;
        private Label labelJjg596ErrorFormula;
        private Label labelJjg596ErrorDescription;
        private Label labelJjg596ErrorPowerType;
        private ComboBox cbxJjg596ErrorPowerType;
        private Label labelJjg596ErrorPowerFactor;
        private ComboBox cbxJjg596ErrorPowerFactor;
        private Label labelJjg596ErrorPhase;
        private ComboBox cbxJjg596ErrorPhase;
        private Label labelJjg596ErrorCurrent;
        private TextBox tbxJjg596ErrorCurrent;
        private Label labelJjg596ErrorPulseCount;
        private TextBox tbxJjg596ErrorPulseCount;
        private Label labelJjg596ErrorPower;
        private TextBox tbxJjg596ErrorPower;
        private Label labelJjg596ErrorTime;
        private TextBox tbxJjg596ErrorTime;
        private Label labelJjg596ErrorCorrectedPulseCount;
        private TextBox tbxJjg596ErrorCorrectedPulseCount;
        private Label labelJjg596ErrorHint;
        private GroupBox groupJjg596CreepTime;
        private TableLayoutPanel tableJjg596CreepTime;
        private Label labelJjg596CreepFormula;
        private Label labelJjg596CreepDescription;
        private Label labelJjg596CreepHours;
        private TextBox tbxJjg596CreepHours;
        private Label labelJjg596CreepMinutes;
        private TextBox tbxJjg596CreepMinutes;
        private Label labelJjg596CreepSeconds;
        private TextBox tbxJjg596CreepSeconds;
        private Label label9;
    }
}
