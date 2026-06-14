namespace ModelTest.CustomControl
{
    partial class SHUserControl
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
            groupConnection = new GroupBox();
            connectionLayout = new FlowLayoutPanel();
            labelIp = new Label();
            txtPowerIp = new TextBox();
            labelPort = new Label();
            nudPowerPort = new NumericUpDown();
            btnConnect = new Button();
            btnDisconnect = new Button();
            groupOutput = new GroupBox();
            outputLayout = new FlowLayoutPanel();
            btnAllOn = new Button();
            btnAllOff = new Button();
            btnVoltageOn = new Button();
            btnVoltageOff = new Button();
            btnCurrentOn = new Button();
            btnCurrentOff = new Button();
            labelVoltagePhase = new Label();
            cmbVoltagePhase = new ComboBox();
            btnPhaseVoltageOn = new Button();
            btnPhaseVoltageOff = new Button();
            btnPhaseCurrentOn = new Button();
            btnPhaseCurrentOff = new Button();
            contentLayout = new TableLayoutPanel();
            groupAmplitudePhase = new GroupBox();
            amplitudeLayout = new TableLayoutPanel();
            labelThreeVoltage = new Label();
            nudThreeVoltage = new NumericUpDown();
            btnSetThreeVoltage = new Button();
            labelPhaseVoltageValue = new Label();
            cmbVoltagePhaseAngle = new ComboBox();
            nudPhaseVoltage = new NumericUpDown();
            btnSetPhaseVoltage = new Button();
            labelVoltagePercent = new Label();
            nudVoltagePercent = new NumericUpDown();
            btnSetVoltagePercent = new Button();
            labelThreeCurrent = new Label();
            nudThreeCurrent = new NumericUpDown();
            labelMaxCurrent = new Label();
            nudMaxCurrent = new NumericUpDown();
            btnSetThreeCurrent = new Button();
            labelPhaseCurrentValue = new Label();
            cmbCurrentPhaseAngle = new ComboBox();
            nudPhaseCurrent = new NumericUpDown();
            btnSetPhaseCurrent = new Button();
            labelCurrentPercent = new Label();
            nudCurrentPercent = new NumericUpDown();
            btnSetCurrentPercent = new Button();
            labelVoltagePhaseAngle = new Label();
            cmbVoltagePhaseControl = new ComboBox();
            nudVoltagePhaseAngle = new NumericUpDown();
            btnSetVoltagePhase = new Button();
            labelPhaseAngle = new Label();
            cmbCurrentPhaseControl = new ComboBox();
            nudPhaseAngle = new NumericUpDown();
            btnSetCurrentPhase = new Button();
            labelFrequency = new Label();
            nudFrequency = new NumericUpDown();
            btnSetFrequency = new Button();
            groupParameters = new GroupBox();
            parameterLayout = new TableLayoutPanel();
            labelWireType = new Label();
            cmbWireType = new ComboBox();
            btnSetWireType = new Button();
            labelAccuracy = new Label();
            nudAccuracy = new NumericUpDown();
            btnSetAccuracy = new Button();
            labelReferenceVoltage = new Label();
            cmbReferenceVoltage = new ComboBox();
            btnSetReferenceVoltage = new Button();
            labelBasicCurrent = new Label();
            nudBasicCurrent = new NumericUpDown();
            labelRatedCurrent = new Label();
            nudRatedCurrent = new NumericUpDown();
            btnSetCurrentRange = new Button();
            labelRunEnergy = new Label();
            nudRunEnergy = new NumericUpDown();
            btnSetRunEnergy = new Button();
            cmbPulseSource = new ComboBox();
            btnSetPulseSource = new Button();
            cmbCurrentAccessMode = new ComboBox();
            btnSetCurrentAccess = new Button();
            cmbPulseMergeLineCount = new ComboBox();
            btnSetPulseMerge = new Button();
            groupTest = new GroupBox();
            testLayout = new FlowLayoutPanel();
            btnStartTest = new Button();
            btnCreepTest = new Button();
            btnEnterRunning = new Button();
            btnStartRunning = new Button();
            btnVoltageLoss = new Button();
            btnDiskError = new Button();
            btnConstantTest = new Button();
            btnExitTest = new Button();
            cmbStatusMode = new ComboBox();
            btnSetStatusMode = new Button();
            cmbTestCommand = new ComboBox();
            btnExecuteTestCommand = new Button();
            groupHarmonic = new GroupBox();
            harmonicLayout = new TableLayoutPanel();
            labelHarmonicTarget = new Label();
            cmbHarmonicTarget = new ComboBox();
            labelHarmonicPhase = new Label();
            nudHarmonicPhase = new NumericUpDown();
            labelHarmonicAmplitude = new Label();
            nudHarmonicAmplitude = new NumericUpDown();
            labelHarmonicOrder = new Label();
            nudHarmonicOrder = new NumericUpDown();
            chkUa = new CheckBox();
            chkUb = new CheckBox();
            chkUc = new CheckBox();
            chkIa = new CheckBox();
            chkIb = new CheckBox();
            chkIc = new CheckBox();
            btnSetHarmonic = new Button();
            btnApplyHarmonicPhase = new Button();
            btnExitHarmonic = new Button();
            btnClearHarmonic = new Button();
            groupQuery = new GroupBox();
            queryLayout = new TableLayoutPanel();
            cmbMiscCommand = new ComboBox();
            btnExecuteMisc = new Button();
            btnReadVersion = new Button();
            btnCheckStatus = new Button();
            btnQueryMeterStatus = new Button();
            btnReadSourceIp = new Button();
            btnReadSourceMac = new Button();
            labelLamp = new Label();
            cmbCheckLamp = new ComboBox();
            cmbWithstandLamp = new ComboBox();
            btnSetLamp = new Button();
            labelPulseParam = new Label();
            cmbOutputPulseChannel = new ComboBox();
            nudPulsePeriod = new NumericUpDown();
            nudPulseWidth = new NumericUpDown();
            cmbPulseLevelMode = new ComboBox();
            btnSetPulseParam = new Button();
            labelPulseRun = new Label();
            cmbRunPulseChannel = new ComboBox();
            chkPulseRunning = new CheckBox();
            nudPulseCount = new NumericUpDown();
            btnSetPulseRun = new Button();
            groupOperation = new GroupBox();
            rootLayout.SuspendLayout();
            groupConnection.SuspendLayout();
            connectionLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudPowerPort).BeginInit();
            groupOutput.SuspendLayout();
            outputLayout.SuspendLayout();
            contentLayout.SuspendLayout();
            groupAmplitudePhase.SuspendLayout();
            amplitudeLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudThreeVoltage).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPhaseVoltage).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudVoltagePercent).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudThreeCurrent).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudMaxCurrent).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPhaseCurrent).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudCurrentPercent).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudVoltagePhaseAngle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPhaseAngle).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudFrequency).BeginInit();
            groupParameters.SuspendLayout();
            parameterLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudAccuracy).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudBasicCurrent).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudRatedCurrent).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudRunEnergy).BeginInit();
            groupTest.SuspendLayout();
            testLayout.SuspendLayout();
            groupHarmonic.SuspendLayout();
            harmonicLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudHarmonicPhase).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudHarmonicAmplitude).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudHarmonicOrder).BeginInit();
            groupQuery.SuspendLayout();
            queryLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudPulsePeriod).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPulseWidth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudPulseCount).BeginInit();
            groupOperation.SuspendLayout();
            SuspendLayout();
            // 
            // scrollPanel
            // 
            scrollPanel.AutoScroll = true;
            scrollPanel.Controls.Add(rootLayout);
            scrollPanel.Dock = DockStyle.Fill;
            scrollPanel.Location = new Point(0, 0);
            scrollPanel.Name = "scrollPanel";
            scrollPanel.Size = new Size(2588, 990);
            scrollPanel.TabIndex = 0;
            // 
            // rootLayout
            // 
            rootLayout.ColumnCount = 1;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.Controls.Add(groupConnection, 0, 0);
            rootLayout.Controls.Add(groupOutput, 0, 1);
            rootLayout.Controls.Add(contentLayout, 0, 2);
            rootLayout.Dock = DockStyle.Top;
            rootLayout.Location = new Point(0, 0);
            rootLayout.MinimumSize = new Size(1200, 1400);
            rootLayout.Name = "rootLayout";
            rootLayout.RowCount = 3;
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 96F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.Size = new Size(2588, 1400);
            rootLayout.TabIndex = 0;
            // 
            // groupConnection
            // 
            groupConnection.Controls.Add(connectionLayout);
            groupConnection.Dock = DockStyle.Fill;
            groupConnection.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            groupConnection.ForeColor = Color.White;
            groupConnection.Location = new Point(4, 4);
            groupConnection.Margin = new Padding(4);
            groupConnection.Name = "groupConnection";
            groupConnection.Size = new Size(2580, 88);
            groupConnection.TabIndex = 0;
            groupConnection.TabStop = false;
            groupConnection.Text = "连接区：IP、端口、连接、断开";
            // 
            // connectionLayout
            // 
            connectionLayout.Controls.Add(labelIp);
            connectionLayout.Controls.Add(txtPowerIp);
            connectionLayout.Controls.Add(labelPort);
            connectionLayout.Controls.Add(nudPowerPort);
            connectionLayout.Controls.Add(btnConnect);
            connectionLayout.Controls.Add(btnDisconnect);
            connectionLayout.Dock = DockStyle.Fill;
            connectionLayout.Location = new Point(3, 31);
            connectionLayout.Name = "connectionLayout";
            connectionLayout.Padding = new Padding(12, 8, 12, 8);
            connectionLayout.Size = new Size(2574, 54);
            connectionLayout.TabIndex = 0;
            connectionLayout.WrapContents = false;
            // 
            // labelIp
            // 
            labelIp.AutoSize = true;
            labelIp.ForeColor = Color.White;
            labelIp.Location = new Point(15, 15);
            labelIp.Margin = new Padding(3, 7, 6, 0);
            labelIp.Name = "labelIp";
            labelIp.Size = new Size(36, 28);
            labelIp.TabIndex = 0;
            labelIp.Text = "IP";
            // 
            // txtPowerIp
            // 
            txtPowerIp.Location = new Point(60, 11);
            txtPowerIp.Margin = new Padding(3, 3, 18, 3);
            txtPowerIp.Name = "txtPowerIp";
            txtPowerIp.Size = new Size(210, 34);
            txtPowerIp.TabIndex = 1;
            // 
            // labelPort
            // 
            labelPort.AutoSize = true;
            labelPort.ForeColor = Color.White;
            labelPort.Location = new Point(291, 15);
            labelPort.Margin = new Padding(3, 7, 6, 0);
            labelPort.Name = "labelPort";
            labelPort.Size = new Size(54, 28);
            labelPort.TabIndex = 2;
            labelPort.Text = "端口";
            // 
            // nudPowerPort
            // 
            nudPowerPort.Location = new Point(354, 11);
            nudPowerPort.Margin = new Padding(3, 3, 18, 3);
            nudPowerPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            nudPowerPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudPowerPort.Name = "nudPowerPort";
            nudPowerPort.Size = new Size(120, 34);
            nudPowerPort.TabIndex = 3;
            nudPowerPort.Value = new decimal(new int[] { 4001, 0, 0, 0 });
            // 
            // groupOutput
            // 
            groupOutput.Controls.Add(outputLayout);
            groupOutput.Dock = DockStyle.Fill;
            groupOutput.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            groupOutput.ForeColor = Color.White;
            groupOutput.Location = new Point(4, 100);
            groupOutput.Margin = new Padding(4);
            groupOutput.Name = "groupOutput";
            groupOutput.Size = new Size(2580, 110);
            groupOutput.TabIndex = 1;
            groupOutput.TabStop = false;
            groupOutput.Text = "输出控制区：全部、电压、电流、分相输出";
            // 
            // outputLayout
            // 
            outputLayout.Dock = DockStyle.Fill;
            outputLayout.Location = new Point(3, 31);
            outputLayout.Name = "outputLayout";
            outputLayout.Padding = new Padding(12, 8, 12, 8);
            outputLayout.Size = new Size(2574, 76);
            outputLayout.TabIndex = 0;
            outputLayout.WrapContents = true;
            // 
            // contentLayout
            // 
            contentLayout.ColumnCount = 3;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33F));
            contentLayout.Controls.Add(groupAmplitudePhase, 0, 0);
            contentLayout.Controls.Add(groupParameters, 1, 0);
            contentLayout.Controls.Add(groupQuery, 2, 0);
            contentLayout.Controls.Add(groupOperation, 0, 1);
            contentLayout.SetColumnSpan(groupOperation, 3);
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.Location = new Point(3, 217);
            contentLayout.Name = "contentLayout";
            contentLayout.RowCount = 2;
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            contentLayout.Size = new Size(2582, 923);
            contentLayout.TabIndex = 2;
            // 
            // groupAmplitudePhase
            // 
            groupAmplitudePhase.Controls.Add(amplitudeLayout);
            groupAmplitudePhase.Dock = DockStyle.Fill;
            groupAmplitudePhase.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            groupAmplitudePhase.ForeColor = Color.White;
            groupAmplitudePhase.Location = new Point(4, 4);
            groupAmplitudePhase.Margin = new Padding(4);
            groupAmplitudePhase.Name = "groupAmplitudePhase";
            groupAmplitudePhase.Size = new Size(869, 393);
            groupAmplitudePhase.TabIndex = 0;
            groupAmplitudePhase.TabStop = false;
            groupAmplitudePhase.Text = "幅值相位区：电压、电流、相位、频率";
            // 
            // amplitudeLayout
            // 
            amplitudeLayout.ColumnCount = 4;
            amplitudeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            amplitudeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            amplitudeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            amplitudeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            amplitudeLayout.Dock = DockStyle.Fill;
            amplitudeLayout.Location = new Point(3, 31);
            amplitudeLayout.Name = "amplitudeLayout";
            amplitudeLayout.Padding = new Padding(12, 8, 12, 8);
            amplitudeLayout.RowCount = 10;
            amplitudeLayout.Size = new Size(863, 359);
            amplitudeLayout.TabIndex = 0;
            // 
            // groupParameters
            // 
            groupParameters.Controls.Add(parameterLayout);
            groupParameters.Dock = DockStyle.Fill;
            groupParameters.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            groupParameters.ForeColor = Color.White;
            groupParameters.Location = new Point(881, 4);
            groupParameters.Margin = new Padding(4);
            groupParameters.Name = "groupParameters";
            groupParameters.Size = new Size(843, 393);
            groupParameters.TabIndex = 1;
            groupParameters.TabStop = false;
            groupParameters.Text = "参数区：接线方式、精度、参比电压、电流量程、走字电能";
            // 
            // parameterLayout
            // 
            parameterLayout.ColumnCount = 4;
            parameterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            parameterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
            parameterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            parameterLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            parameterLayout.Dock = DockStyle.Fill;
            parameterLayout.Location = new Point(3, 31);
            parameterLayout.Name = "parameterLayout";
            parameterLayout.Padding = new Padding(12, 8, 12, 8);
            parameterLayout.RowCount = 7;
            parameterLayout.Size = new Size(837, 359);
            parameterLayout.TabIndex = 0;
            // 
            // groupTest
            // 
            groupTest.Controls.Add(testLayout);
            groupTest.Dock = DockStyle.Fill;
            groupTest.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            groupTest.ForeColor = Color.White;
            groupTest.Location = new Point(1732, 4);
            groupTest.Margin = new Padding(4);
            groupTest.Name = "groupTest";
            groupTest.Size = new Size(846, 393);
            groupTest.TabIndex = 2;
            groupTest.TabStop = false;
            groupTest.Text = "试验区：启动、潜动、走字、失压、盘转、常数、退出";
            // 
            // testLayout
            // 
            testLayout.Dock = DockStyle.Fill;
            testLayout.Location = new Point(3, 31);
            testLayout.Name = "testLayout";
            testLayout.Padding = new Padding(12, 8, 12, 8);
            testLayout.Size = new Size(840, 359);
            testLayout.TabIndex = 0;
            // 
            // groupHarmonic
            // 
            groupHarmonic.Controls.Add(harmonicLayout);
            groupHarmonic.Dock = DockStyle.Fill;
            groupHarmonic.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            groupHarmonic.ForeColor = Color.White;
            groupHarmonic.Location = new Point(4, 405);
            groupHarmonic.Margin = new Padding(4);
            groupHarmonic.Name = "groupHarmonic";
            groupHarmonic.Size = new Size(869, 364);
            groupHarmonic.TabIndex = 3;
            groupHarmonic.TabStop = false;
            groupHarmonic.Text = "谐波区：谐波点、相别勾选、应用、退出、清除";
            // 
            // harmonicLayout
            // 
            harmonicLayout.ColumnCount = 4;
            harmonicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            harmonicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            harmonicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            harmonicLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            harmonicLayout.Dock = DockStyle.Fill;
            harmonicLayout.Location = new Point(3, 31);
            harmonicLayout.Name = "harmonicLayout";
            harmonicLayout.Padding = new Padding(12, 8, 12, 8);
            harmonicLayout.RowCount = 7;
            harmonicLayout.Size = new Size(863, 330);
            harmonicLayout.TabIndex = 0;
            // 
            // groupQuery
            // 
            groupQuery.Controls.Add(queryLayout);
            groupQuery.Dock = DockStyle.Fill;
            groupQuery.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            groupQuery.ForeColor = Color.White;
            groupQuery.Location = new Point(1732, 4);
            groupQuery.Margin = new Padding(4);
            groupQuery.Name = "groupQuery";
            groupQuery.Size = new Size(846, 393);
            groupQuery.TabIndex = 4;
            groupQuery.TabStop = false;
            groupQuery.Text = "查询区：版本、综合查询、表位通信状态、源 IP/MAC 查询";
            // 
            // queryLayout
            // 
            queryLayout.ColumnCount = 6;
            queryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            queryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            queryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            queryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            queryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            queryLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            queryLayout.Dock = DockStyle.Fill;
            queryLayout.Location = new Point(3, 31);
            queryLayout.Name = "queryLayout";
            queryLayout.Padding = new Padding(12, 8, 12, 8);
            queryLayout.RowCount = 6;
            queryLayout.Size = new Size(840, 359);
            queryLayout.TabIndex = 0;
            // 
            // groupOperation
            // 
            groupOperation.Dock = DockStyle.Fill;
            groupOperation.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            groupOperation.ForeColor = Color.White;
            groupOperation.Location = new Point(4, 405);
            groupOperation.Margin = new Padding(4);
            groupOperation.Name = "groupOperation";
            groupOperation.Size = new Size(2574, 364);
            groupOperation.TabIndex = 5;
            groupOperation.TabStop = false;
            groupOperation.Text = "操作区";
            // 
            // SHUserControl
            // 
            AutoScaleDimensions = new SizeF(12F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(88, 149, 127);
            Controls.Add(scrollPanel);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            Name = "SHUserControl";
            Size = new Size(2588, 990);
            BuildDynamicLayout();
            rootLayout.ResumeLayout(false);
            groupConnection.ResumeLayout(false);
            connectionLayout.ResumeLayout(false);
            connectionLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudPowerPort).EndInit();
            groupOutput.ResumeLayout(false);
            outputLayout.ResumeLayout(false);
            outputLayout.PerformLayout();
            contentLayout.ResumeLayout(false);
            groupAmplitudePhase.ResumeLayout(false);
            amplitudeLayout.ResumeLayout(false);
            amplitudeLayout.PerformLayout();
            groupParameters.ResumeLayout(false);
            parameterLayout.ResumeLayout(false);
            parameterLayout.PerformLayout();
            groupTest.ResumeLayout(false);
            testLayout.ResumeLayout(false);
            testLayout.PerformLayout();
            groupHarmonic.ResumeLayout(false);
            harmonicLayout.ResumeLayout(false);
            harmonicLayout.PerformLayout();
            groupQuery.ResumeLayout(false);
            queryLayout.ResumeLayout(false);
            queryLayout.PerformLayout();
            groupOperation.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)nudThreeVoltage).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPhaseVoltage).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudVoltagePercent).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudThreeCurrent).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudMaxCurrent).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPhaseCurrent).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudCurrentPercent).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudVoltagePhaseAngle).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPhaseAngle).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudFrequency).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudAccuracy).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudBasicCurrent).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudRatedCurrent).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudRunEnergy).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudHarmonicPhase).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudHarmonicAmplitude).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudHarmonicOrder).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPulsePeriod).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPulseWidth).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudPulseCount).EndInit();
            ResumeLayout(false);
        }

        private void BuildDynamicLayout()
        {
            ConfigureFixedRows(amplitudeLayout, 10, 52F);
            ConfigureFixedRows(parameterLayout, 7, 50F);
            ConfigureFixedRows(harmonicLayout, 7, 52F);
            ConfigureFixedRows(queryLayout, 6, 58F);

            AddButton(connectionLayout, btnConnect, "连接");
            AddButton(connectionLayout, btnDisconnect, "断开");

            AddButton(outputLayout, btnAllOn, "全部输出");
            AddButton(outputLayout, btnAllOff, "全部关闭");
            AddButton(outputLayout, btnVoltageOn, "电压输出");
            AddButton(outputLayout, btnVoltageOff, "电压关闭");
            AddButton(outputLayout, btnCurrentOn, "电流输出");
            AddButton(outputLayout, btnCurrentOff, "电流关闭");
            AddFlowLabel(outputLayout, labelVoltagePhase, "相别");
            AddComboToFlow(outputLayout, cmbVoltagePhase, 96);
            AddButton(outputLayout, btnPhaseVoltageOn, "分相电压开");
            AddButton(outputLayout, btnPhaseVoltageOff, "分相电压关");
            AddButton(outputLayout, btnPhaseCurrentOn, "分相电流开");
            AddButton(outputLayout, btnPhaseCurrentOff, "分相电流关");

            ConfigureNumber(nudThreeVoltage, 0.001m, 115.000m, 100m, 3);
            AddRow(amplitudeLayout, 0, labelThreeVoltage, "三相电压", nudThreeVoltage, null, btnSetThreeVoltage, "设置三相电压");
            ConfigureNumber(nudThreeCurrent, 0.001m, 9999m, 5m, 3);
            ConfigureNumber(nudMaxCurrent, 0.001m, 9999m, 100m, 3);
            AddRow(amplitudeLayout, 1, labelThreeCurrent, "三相电流", nudThreeCurrent, null, btnSetThreeCurrent, "设置三相电流");
            AddRow(amplitudeLayout, 2, labelMaxCurrent, "最大电流", nudMaxCurrent, null, null, "");
            ConfigureNumber(nudPhaseVoltage, 0.001m, 115.000m, 100m, 3);
            AddRow(amplitudeLayout, 3, labelPhaseVoltageValue, "分相电压", cmbVoltagePhaseAngle, nudPhaseVoltage, btnSetPhaseVoltage, "设置分相电压");
            ConfigureNumber(nudVoltagePercent, 0m, 999.9m, 100m, 1);
            AddRow(amplitudeLayout, 4, labelVoltagePercent, "电压百分比", nudVoltagePercent, null, btnSetVoltagePercent, "设置电压百分比");
            ConfigureNumber(nudPhaseCurrent, 0.001m, 9999m, 5m, 3);
            AddRow(amplitudeLayout, 5, labelPhaseCurrentValue, "分相电流", cmbCurrentPhaseAngle, nudPhaseCurrent, btnSetPhaseCurrent, "设置分相电流");
            ConfigureNumber(nudCurrentPercent, 0m, 999.9m, 100m, 1);
            AddRow(amplitudeLayout, 6, labelCurrentPercent, "电流百分比", nudCurrentPercent, null, btnSetCurrentPercent, "设置电流百分比");
            ConfigureNumber(nudVoltagePhaseAngle, 0.01m, 359.99m, 120m, 3);
            AddRow(amplitudeLayout, 7, labelVoltagePhaseAngle, "电压相位", cmbVoltagePhaseControl, nudVoltagePhaseAngle, btnSetVoltagePhase, "设置电压相位");
            ConfigureNumber(nudPhaseAngle, 0.01m, 359.99m, 120m, 3);
            AddRow(amplitudeLayout, 8, labelPhaseAngle, "电流相位", cmbCurrentPhaseControl, nudPhaseAngle, btnSetCurrentPhase, "设置电流相位");
            ConfigureNumber(nudFrequency, 45m, 65m, 50m, 3);
            AddRow(amplitudeLayout, 9, labelFrequency, "频率", nudFrequency, null, btnSetFrequency, "设置频率");

            AddRow(parameterLayout, 0, labelWireType, "接线方式", cmbWireType, null, btnSetWireType, "设置接线方式");
            ConfigureNumber(nudAccuracy, 0.01m, 99.9m, 0.5m, 2);
            AddRow(parameterLayout, 1, labelAccuracy, "精度等级", nudAccuracy, null, btnSetAccuracy, "设置精度等级");
            cmbReferenceVoltage.Items.AddRange(new object[] { "57.7", "100", "220", "380" });
            cmbReferenceVoltage.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbReferenceVoltage.SelectedIndex = 2;
            AddRow(parameterLayout, 2, labelReferenceVoltage, "参比电压", cmbReferenceVoltage, null, btnSetReferenceVoltage, "设置参比电压");
            ConfigureNumber(nudBasicCurrent, 0.001m, 9999m, 1.5m, 3);
            ConfigureNumber(nudRatedCurrent, 0.001m, 9999m, 6m, 3);
            AddRow(parameterLayout, 3, labelBasicCurrent, "基本电流", nudBasicCurrent, null, null, "");
            AddRow(parameterLayout, 4, labelRatedCurrent, "额定电流", nudRatedCurrent, null, btnSetCurrentRange, "设置电流量程");
            ConfigureNumber(nudRunEnergy, 0.001m, 9999m, 0.1m, 3);
            AddRow(parameterLayout, 5, labelRunEnergy, "走字电能", nudRunEnergy, null, btnSetRunEnergy, "设置走字电能");
            AddComboRow(parameterLayout, 6, cmbPulseSource, btnSetPulseSource, "脉冲选择", cmbCurrentAccessMode, btnSetCurrentAccess, "电流接入", cmbPulseMergeLineCount, btnSetPulseMerge, "组合路数");

            cmbPulseMergeLineCount.Items.AddRange(new object[] { "2", "3", "4" });
            AddButton(testLayout, btnStartTest, "启动试验");
            AddButton(testLayout, btnCreepTest, "潜动试验");
            AddButton(testLayout, btnEnterRunning, "进入走字");
            AddButton(testLayout, btnStartRunning, "开始走字");
            AddButton(testLayout, btnVoltageLoss, "失压试验");
            AddButton(testLayout, btnDiskError, "盘转误差");
            AddButton(testLayout, btnConstantTest, "常数测试");
            AddButton(testLayout, btnExitTest, "退出试验");
            AddComboToFlow(testLayout, cmbStatusMode, 220);
            AddButton(testLayout, btnSetStatusMode, "设置状态");
            AddComboToFlow(testLayout, cmbTestCommand, 260);
            AddButton(testLayout, btnExecuteTestCommand, "执行命令");

            ConfigureNumber(nudHarmonicPhase, 0m, 359.9m, 0m, 1);
            ConfigureNumber(nudHarmonicAmplitude, 0.1m, 600m, 1m, 1);
            ConfigureNumber(nudHarmonicOrder, 2, 21, 2, 0);
            AddRow(harmonicLayout, 0, labelHarmonicTarget, "谐波目标", cmbHarmonicTarget, null, null, "");
            AddRow(harmonicLayout, 1, labelHarmonicPhase, "相位", nudHarmonicPhase, null, null, "");
            AddRow(harmonicLayout, 2, labelHarmonicAmplitude, "幅度百分比", nudHarmonicAmplitude, null, null, "");
            AddRow(harmonicLayout, 3, labelHarmonicOrder, "次数", nudHarmonicOrder, null, btnSetHarmonic, "设置谐波点");
            AddCheckRow(harmonicLayout, 4, chkUa, "UA", chkUb, "UB", chkUc, "UC");
            AddCheckRow(harmonicLayout, 5, chkIa, "IA", chkIb, "IB", chkIc, "IC");
            AddButtonsRow(harmonicLayout, 6, btnApplyHarmonicPhase, "应用相别", btnExitHarmonic, "退出谐波", btnClearHarmonic, "清除谐波");

            AddRow(queryLayout, 0, new Label(), "杂项命令", cmbMiscCommand, null, btnExecuteMisc, "执行");
            AddButtonsRow(queryLayout, 1, btnReadVersion, "版本", btnCheckStatus, "综合查询", btnQueryMeterStatus, "表位通信");
            AddButtonsRow(queryLayout, 2, btnReadSourceIp, "源IP查询", btnReadSourceMac, "源MAC查询", null, "");
            AddRow(queryLayout, 3, labelLamp, "表位灯", cmbCheckLamp, cmbWithstandLamp, btnSetLamp, "设置灯");
            cmbOutputPulseChannel.Items.AddRange(new object[] { "全部", "脉冲1", "脉冲2" });
            ConfigureNumber(nudPulsePeriod, 1, 9999, 1000, 0);
            ConfigureNumber(nudPulseWidth, 1, 9999, 500, 0);
            AddPulseParamRow();
            cmbRunPulseChannel.Items.AddRange(new object[] { "脉冲1", "脉冲2" });
            ConfigureNumber(nudPulseCount, 0, 999, 10, 0);
            AddPulseRunRow();
        }

        private void AddPulseParamRow()
        {
            AddGridLabel(queryLayout, labelPulseParam, "脉冲参数", 4, 0);
            AddGridControl(queryLayout, cmbOutputPulseChannel, 4, 1);
            AddGridControl(queryLayout, nudPulsePeriod, 4, 2);
            AddGridControl(queryLayout, nudPulseWidth, 4, 3);
            AddGridControl(queryLayout, cmbPulseLevelMode, 4, 4);
            AddGridButton(queryLayout, btnSetPulseParam, "设置参数", 4, 5);
        }

        private void AddPulseRunRow()
        {
            AddGridLabel(queryLayout, labelPulseRun, "脉冲运行", 5, 0);
            AddGridControl(queryLayout, cmbRunPulseChannel, 5, 1);
            chkPulseRunning.Text = "运行";
            AddGridControl(queryLayout, chkPulseRunning, 5, 2);
            AddGridControl(queryLayout, nudPulseCount, 5, 3);
            AddGridButton(queryLayout, btnSetPulseRun, "运行设置", 5, 4);
        }

        private void AddRow(TableLayoutPanel panel, int row, Label label, string text, Control first, Control second, Button button, string buttonText)
        {
            AddGridLabel(panel, label, text, row, 0);
            AddGridControl(panel, first, row, 1);
            if (second != null)
                AddGridControl(panel, second, row, 2);
            if (button != null)
                AddGridButton(panel, button, buttonText, row, 3);
        }

        private void AddComboRow(TableLayoutPanel panel, int row, ComboBox firstCombo, Button firstButton, string firstText, ComboBox secondCombo, Button secondButton, string secondText, ComboBox thirdCombo, Button thirdButton, string thirdText)
        {
            AddGridLabel(panel, new Label(), firstText, row, 0);
            AddGridControl(panel, firstCombo, row, 1);
            AddGridButton(panel, firstButton, firstText, row, 2);
            FlowLayoutPanel flow = new()
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                WrapContents = false
            };
            AddCompactComboToFlow(flow, secondCombo, 108);
            AddCompactButton(flow, secondButton, secondText, 98);
            AddCompactComboToFlow(flow, thirdCombo, 62);
            AddCompactButton(flow, thirdButton, thirdText, 98);
            panel.Controls.Add(flow, 3, row);
        }

        private void AddCheckRow(TableLayoutPanel panel, int row, CheckBox first, string firstText, CheckBox second, string secondText, CheckBox third, string thirdText)
        {
            first.Text = firstText;
            second.Text = secondText;
            third.Text = thirdText;
            AddGridControl(panel, first, row, 1);
            AddGridControl(panel, second, row, 2);
            AddGridControl(panel, third, row, 3);
        }

        private void AddButtonsRow(TableLayoutPanel panel, int row, Button first, string firstText, Button second, string secondText, Button third, string thirdText)
        {
            if (first != null)
                AddGridButton(panel, first, firstText, row, 1);
            if (second != null)
                AddGridButton(panel, second, secondText, row, 2);
            if (third != null)
                AddGridButton(panel, third, thirdText, row, 3);
        }

        private void AddGridLabel(TableLayoutPanel panel, Label label, string text, int row, int column)
        {
            label.AutoSize = true;
            label.Dock = DockStyle.Fill;
            label.ForeColor = Color.White;
            label.Margin = new Padding(3, 8, 6, 3);
            label.Text = text;
            panel.Controls.Add(label, column, row);
        }

        private void AddGridControl(TableLayoutPanel panel, Control control, int row, int column)
        {
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(3, 4, 8, 4);
            panel.Controls.Add(control, column, row);
        }

        private void AddGridButton(TableLayoutPanel panel, Button button, string text, int row, int column)
        {
            button.Dock = DockStyle.Fill;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            button.ForeColor = Color.Black;
            button.Margin = new Padding(3, 4, 8, 4);
            button.Text = text;
            button.UseVisualStyleBackColor = true;
            button.Click += LogButtonClick;
            panel.Controls.Add(button, column, row);
        }

        private void AddFlowLabel(FlowLayoutPanel panel, Label label, string text)
        {
            label.AutoSize = true;
            label.ForeColor = Color.White;
            label.Margin = new Padding(16, 8, 6, 0);
            label.Text = text;
            panel.Controls.Add(label);
        }

        private void AddComboToFlow(FlowLayoutPanel panel, ComboBox comboBox, int width)
        {
            comboBox.Width = width;
            comboBox.Margin = new Padding(3, 4, 8, 4);
            panel.Controls.Add(comboBox);
        }

        private void AddButton(FlowLayoutPanel panel, Button button, string text)
        {
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            button.ForeColor = Color.Black;
            button.Margin = new Padding(3, 4, 8, 4);
            button.Size = new Size(128, 40);
            button.Text = text;
            button.UseVisualStyleBackColor = true;
            button.Click += LogButtonClick;
            panel.Controls.Add(button);
        }

        private void AddCompactComboToFlow(FlowLayoutPanel panel, ComboBox comboBox, int width)
        {
            comboBox.Width = width;
            comboBox.Margin = new Padding(2, 5, 4, 4);
            panel.Controls.Add(comboBox);
        }

        private void AddCompactButton(FlowLayoutPanel panel, Button button, string text, int width)
        {
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            button.ForeColor = Color.Black;
            button.Margin = new Padding(2, 4, 4, 4);
            button.Size = new Size(width, 40);
            button.Text = text;
            button.UseVisualStyleBackColor = true;
            button.Click += LogButtonClick;
            panel.Controls.Add(button);
        }

        private static void ConfigureNumber(NumericUpDown number, decimal min, decimal max, decimal value, int decimalPlaces)
        {
            number.DecimalPlaces = decimalPlaces;
            number.Maximum = max;
            number.Minimum = min;
            number.Value = value;
        }

        private static void ConfigureFixedRows(TableLayoutPanel panel, int rowCount, float rowHeight)
        {
            panel.Dock = DockStyle.Top;
            panel.Height = (int)Math.Ceiling(rowCount * rowHeight) + panel.Padding.Vertical;
            panel.RowCount = rowCount;
            panel.RowStyles.Clear();
            for (int i = 0; i < rowCount; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
            }
        }

        private Panel scrollPanel;
        private TableLayoutPanel rootLayout;
        private GroupBox groupConnection;
        private FlowLayoutPanel connectionLayout;
        private Label labelIp;
        private TextBox txtPowerIp;
        private Label labelPort;
        private NumericUpDown nudPowerPort;
        private Button btnConnect;
        private Button btnDisconnect;
        private GroupBox groupOutput;
        private FlowLayoutPanel outputLayout;
        private Button btnAllOn;
        private Button btnAllOff;
        private Button btnVoltageOn;
        private Button btnVoltageOff;
        private Button btnCurrentOn;
        private Button btnCurrentOff;
        private Label labelVoltagePhase;
        private ComboBox cmbVoltagePhase;
        private Button btnPhaseVoltageOn;
        private Button btnPhaseVoltageOff;
        private Button btnPhaseCurrentOn;
        private Button btnPhaseCurrentOff;
        private TableLayoutPanel contentLayout;
        private GroupBox groupAmplitudePhase;
        private TableLayoutPanel amplitudeLayout;
        private Label labelThreeVoltage;
        private NumericUpDown nudThreeVoltage;
        private Button btnSetThreeVoltage;
        private Label labelPhaseVoltageValue;
        private ComboBox cmbVoltagePhaseAngle;
        private NumericUpDown nudPhaseVoltage;
        private Button btnSetPhaseVoltage;
        private Label labelVoltagePercent;
        private NumericUpDown nudVoltagePercent;
        private Button btnSetVoltagePercent;
        private Label labelThreeCurrent;
        private NumericUpDown nudThreeCurrent;
        private Label labelMaxCurrent;
        private NumericUpDown nudMaxCurrent;
        private Button btnSetThreeCurrent;
        private Label labelPhaseCurrentValue;
        private ComboBox cmbCurrentPhaseAngle;
        private NumericUpDown nudPhaseCurrent;
        private Button btnSetPhaseCurrent;
        private Label labelCurrentPercent;
        private NumericUpDown nudCurrentPercent;
        private Button btnSetCurrentPercent;
        private Label labelVoltagePhaseAngle;
        private ComboBox cmbVoltagePhaseControl;
        private NumericUpDown nudVoltagePhaseAngle;
        private Button btnSetVoltagePhase;
        private Label labelPhaseAngle;
        private ComboBox cmbCurrentPhaseControl;
        private NumericUpDown nudPhaseAngle;
        private Button btnSetCurrentPhase;
        private Label labelFrequency;
        private NumericUpDown nudFrequency;
        private Button btnSetFrequency;
        private GroupBox groupParameters;
        private TableLayoutPanel parameterLayout;
        private Label labelWireType;
        private ComboBox cmbWireType;
        private Button btnSetWireType;
        private Label labelAccuracy;
        private NumericUpDown nudAccuracy;
        private Button btnSetAccuracy;
        private Label labelReferenceVoltage;
        private ComboBox cmbReferenceVoltage;
        private Button btnSetReferenceVoltage;
        private Label labelBasicCurrent;
        private NumericUpDown nudBasicCurrent;
        private Label labelRatedCurrent;
        private NumericUpDown nudRatedCurrent;
        private Button btnSetCurrentRange;
        private Label labelRunEnergy;
        private NumericUpDown nudRunEnergy;
        private Button btnSetRunEnergy;
        private ComboBox cmbPulseSource;
        private Button btnSetPulseSource;
        private ComboBox cmbCurrentAccessMode;
        private Button btnSetCurrentAccess;
        private ComboBox cmbPulseMergeLineCount;
        private Button btnSetPulseMerge;
        private GroupBox groupTest;
        private FlowLayoutPanel testLayout;
        private Button btnStartTest;
        private Button btnCreepTest;
        private Button btnEnterRunning;
        private Button btnStartRunning;
        private Button btnVoltageLoss;
        private Button btnDiskError;
        private Button btnConstantTest;
        private Button btnExitTest;
        private ComboBox cmbStatusMode;
        private Button btnSetStatusMode;
        private ComboBox cmbTestCommand;
        private Button btnExecuteTestCommand;
        private GroupBox groupHarmonic;
        private TableLayoutPanel harmonicLayout;
        private Label labelHarmonicTarget;
        private ComboBox cmbHarmonicTarget;
        private Label labelHarmonicPhase;
        private NumericUpDown nudHarmonicPhase;
        private Label labelHarmonicAmplitude;
        private NumericUpDown nudHarmonicAmplitude;
        private Label labelHarmonicOrder;
        private NumericUpDown nudHarmonicOrder;
        private CheckBox chkUa;
        private CheckBox chkUb;
        private CheckBox chkUc;
        private CheckBox chkIa;
        private CheckBox chkIb;
        private CheckBox chkIc;
        private Button btnSetHarmonic;
        private Button btnApplyHarmonicPhase;
        private Button btnExitHarmonic;
        private Button btnClearHarmonic;
        private GroupBox groupQuery;
        private TableLayoutPanel queryLayout;
        private ComboBox cmbMiscCommand;
        private Button btnExecuteMisc;
        private Button btnReadVersion;
        private Button btnCheckStatus;
        private Button btnQueryMeterStatus;
        private Button btnReadSourceIp;
        private Button btnReadSourceMac;
        private Label labelLamp;
        private ComboBox cmbCheckLamp;
        private ComboBox cmbWithstandLamp;
        private Button btnSetLamp;
        private Label labelPulseParam;
        private ComboBox cmbOutputPulseChannel;
        private NumericUpDown nudPulsePeriod;
        private NumericUpDown nudPulseWidth;
        private ComboBox cmbPulseLevelMode;
        private Button btnSetPulseParam;
        private Label labelPulseRun;
        private ComboBox cmbRunPulseChannel;
        private CheckBox chkPulseRunning;
        private NumericUpDown nudPulseCount;
        private Button btnSetPulseRun;
        private GroupBox groupOperation;
    }
}
