namespace ModelTest.CustomControl
{
    partial class ElectricEnergyMeterControlV1
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
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            label3 = new Label();
            btn_MeterTCPConnect = new Button();
            tbx_MeterPort = new TextBox();
            tbx_MeterIP = new TextBox();
            label2 = new Label();
            label1 = new Label();
            panel1 = new Panel();
            btnTestMeterCommunication = new Button();
            btnResetCommand = new Button();
            btnAcVoltagePower = new Button();
            cbxPhaseA = new CheckBox();
            cbxPhaseB = new CheckBox();
            cbxPhaseC = new CheckBox();
            btnAcCurrentPower = new Button();
            cbxCurrentPhaseA = new CheckBox();
            cbxCurrentPhaseB = new CheckBox();
            cbxCurrentPhaseC = new CheckBox();
            groupBoxBasicCommand = new GroupBox();
            groupBoxAcVoltageControl = new GroupBox();
            groupBoxAcCurrentControl = new GroupBox();
            groupBoxDailyTiming = new GroupBox();
            labelDailyTimingTime = new Label();
            tbxDailyTimingTime = new TextBox();
            labelDailyTimingCount = new Label();
            tbxDailyTimingCount = new TextBox();
            btnStartDailyTiming = new Button();
            btnGetDailyTimingResult = new Button();
            btnStopDailyTiming = new Button();
            labelDailyTimingCountdown = new Label();
            groupBoxStationDetection = new GroupBox();
            labelVoltageShortCircuitDetection = new Label();
            btnStartVoltageShortCircuitDetection = new Button();
            btnGetVoltageShortCircuitDetectionResult = new Button();
            labelMeterPresenceDetection = new Label();
            btnStartMeterPresenceDetection = new Button();
            btnGetMeterPresenceDetectionResult = new Button();
            labelStationDetectionSummary = new Label();
            groupBoxMotorCrimping = new GroupBox();
            btnMotorCrimpPress = new Button();
            btnMotorCrimpRelease = new Button();
            btnMotorCrimpPowerOff = new Button();
            groupBox3 = new GroupBox();
            label4 = new Label();
            tbxMeterV1Addr = new TextBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBoxBasicCommand.SuspendLayout();
            groupBoxAcVoltageControl.SuspendLayout();
            groupBoxAcCurrentControl.SuspendLayout();
            groupBoxDailyTiming.SuspendLayout();
            groupBoxStationDetection.SuspendLayout();
            groupBoxMotorCrimping.SuspendLayout();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(groupBox3);
            groupBox1.Controls.Add(groupBox2);
            groupBox1.Dock = DockStyle.Left;
            groupBox1.Location = new Point(0, 0);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(350, 1024);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "电表TCP/IP连接配置";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(label3);
            groupBox2.Controls.Add(btn_MeterTCPConnect);
            groupBox2.Controls.Add(tbx_MeterPort);
            groupBox2.Controls.Add(tbx_MeterIP);
            groupBox2.Controls.Add(label2);
            groupBox2.Controls.Add(label1);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.Location = new Point(3, 30);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(344, 187);
            groupBox2.TabIndex = 0;
            groupBox2.TabStop = false;
            groupBox2.Text = "电表主控连接";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.ForeColor = Color.Transparent;
            label3.Location = new Point(6, 156);
            label3.Name = "label3";
            label3.Size = new Size(75, 28);
            label3.TabIndex = 5;
            label3.Text = "状态：";
            // 
            // btn_MeterTCPConnect
            // 
            btn_MeterTCPConnect.Location = new Point(181, 98);
            btn_MeterTCPConnect.Name = "btn_MeterTCPConnect";
            btn_MeterTCPConnect.Size = new Size(117, 34);
            btn_MeterTCPConnect.TabIndex = 4;
            btn_MeterTCPConnect.Text = "链接";
            btn_MeterTCPConnect.UseVisualStyleBackColor = true;
            btn_MeterTCPConnect.Click += btn_MeterTCPConnect_Click;
            // 
            // tbx_MeterPort
            // 
            tbx_MeterPort.Location = new Point(70, 98);
            tbx_MeterPort.Name = "tbx_MeterPort";
            tbx_MeterPort.Size = new Size(88, 34);
            tbx_MeterPort.TabIndex = 3;
            tbx_MeterPort.Text = "1001";
            // 
            // tbx_MeterIP
            // 
            tbx_MeterIP.Location = new Point(70, 37);
            tbx_MeterIP.Name = "tbx_MeterIP";
            tbx_MeterIP.Size = new Size(228, 34);
            tbx_MeterIP.TabIndex = 2;
            tbx_MeterIP.Text = "192.168.127.101";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 104);
            label2.Name = "label2";
            label2.Size = new Size(59, 28);
            label2.TabIndex = 1;
            label2.Text = "Port:";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(6, 40);
            label1.Name = "label1";
            label1.Size = new Size(36, 28);
            label1.TabIndex = 0;
            label1.Text = "IP:";
            // 
            // panel1
            // 
            panel1.Controls.Add(groupBoxMotorCrimping);
            panel1.Controls.Add(groupBoxStationDetection);
            panel1.Controls.Add(groupBoxDailyTiming);
            panel1.Controls.Add(groupBoxBasicCommand);
            panel1.Controls.Add(groupBoxAcCurrentControl);
            panel1.Controls.Add(groupBoxAcVoltageControl);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(350, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(2238, 1024);
            panel1.TabIndex = 1;
            // 
            // groupBoxBasicCommand
            // 
            groupBoxBasicCommand.Controls.Add(btnResetCommand);
            groupBoxBasicCommand.Controls.Add(btnTestMeterCommunication);
            groupBoxBasicCommand.Location = new Point(24, 20);
            groupBoxBasicCommand.Name = "groupBoxBasicCommand";
            groupBoxBasicCommand.Size = new Size(620, 90);
            groupBoxBasicCommand.TabIndex = 0;
            groupBoxBasicCommand.TabStop = false;
            groupBoxBasicCommand.Text = "基础命令";
            // 
            // btnTestMeterCommunication
            // 
            btnTestMeterCommunication.Location = new Point(12, 34);
            btnTestMeterCommunication.Name = "btnTestMeterCommunication";
            btnTestMeterCommunication.Size = new Size(226, 46);
            btnTestMeterCommunication.TabIndex = 0;
            btnTestMeterCommunication.Text = "测试表位通信";
            btnTestMeterCommunication.UseVisualStyleBackColor = true;
            btnTestMeterCommunication.Click += btnTestMeterCommunication_Click;
            // 
            // btnResetCommand
            // 
            btnResetCommand.Location = new Point(280, 34);
            btnResetCommand.Name = "btnResetCommand";
            btnResetCommand.Size = new Size(226, 46);
            btnResetCommand.TabIndex = 1;
            btnResetCommand.Text = "复位命令";
            btnResetCommand.UseVisualStyleBackColor = true;
            btnResetCommand.Click += btnResetCommand_Click;
            // 
            // groupBoxAcVoltageControl
            // 
            groupBoxAcVoltageControl.Controls.Add(btnAcVoltagePower);
            groupBoxAcVoltageControl.Controls.Add(cbxPhaseA);
            groupBoxAcVoltageControl.Controls.Add(cbxPhaseB);
            groupBoxAcVoltageControl.Controls.Add(cbxPhaseC);
            groupBoxAcVoltageControl.Location = new Point(24, 126);
            groupBoxAcVoltageControl.Name = "groupBoxAcVoltageControl";
            groupBoxAcVoltageControl.Size = new Size(620, 108);
            groupBoxAcVoltageControl.TabIndex = 1;
            groupBoxAcVoltageControl.TabStop = false;
            groupBoxAcVoltageControl.Text = "交流电压控制";
            // 
            // btnAcVoltagePower
            // 
            btnAcVoltagePower.Location = new Point(12, 42);
            btnAcVoltagePower.Name = "btnAcVoltagePower";
            btnAcVoltagePower.Size = new Size(226, 46);
            btnAcVoltagePower.TabIndex = 4;
            btnAcVoltagePower.Text = "上电";
            btnAcVoltagePower.UseVisualStyleBackColor = true;
            btnAcVoltagePower.Click += btnAcVoltagePower_Click;
            // 
            // cbxPhaseA
            // 
            cbxPhaseA.AutoSize = true;
            cbxPhaseA.Location = new Point(280, 50);
            cbxPhaseA.Name = "cbxPhaseA";
            cbxPhaseA.Size = new Size(84, 32);
            cbxPhaseA.TabIndex = 1;
            cbxPhaseA.Text = "A相";
            cbxPhaseA.UseVisualStyleBackColor = true;
            // 
            // cbxPhaseB
            // 
            cbxPhaseB.AutoSize = true;
            cbxPhaseB.Location = new Point(387, 50);
            cbxPhaseB.Name = "cbxPhaseB";
            cbxPhaseB.Size = new Size(84, 32);
            cbxPhaseB.TabIndex = 2;
            cbxPhaseB.Text = "B相";
            cbxPhaseB.UseVisualStyleBackColor = true;
            // 
            // cbxPhaseC
            // 
            cbxPhaseC.AutoSize = true;
            cbxPhaseC.Location = new Point(494, 50);
            cbxPhaseC.Name = "cbxPhaseC";
            cbxPhaseC.Size = new Size(84, 32);
            cbxPhaseC.TabIndex = 3;
            cbxPhaseC.Text = "C相";
            cbxPhaseC.UseVisualStyleBackColor = true;
            // 
            // groupBoxAcCurrentControl
            // 
            groupBoxAcCurrentControl.Controls.Add(btnAcCurrentPower);
            groupBoxAcCurrentControl.Controls.Add(cbxCurrentPhaseA);
            groupBoxAcCurrentControl.Controls.Add(cbxCurrentPhaseB);
            groupBoxAcCurrentControl.Controls.Add(cbxCurrentPhaseC);
            groupBoxAcCurrentControl.Location = new Point(24, 252);
            groupBoxAcCurrentControl.Name = "groupBoxAcCurrentControl";
            groupBoxAcCurrentControl.Size = new Size(620, 108);
            groupBoxAcCurrentControl.TabIndex = 2;
            groupBoxAcCurrentControl.TabStop = false;
            groupBoxAcCurrentControl.Text = "交流电流控制";
            // 
            // btnAcCurrentPower
            // 
            btnAcCurrentPower.Location = new Point(12, 42);
            btnAcCurrentPower.Name = "btnAcCurrentPower";
            btnAcCurrentPower.Size = new Size(226, 46);
            btnAcCurrentPower.TabIndex = 8;
            btnAcCurrentPower.Text = "通电流";
            btnAcCurrentPower.UseVisualStyleBackColor = true;
            btnAcCurrentPower.Click += btnAcCurrentPower_Click;
            // 
            // cbxCurrentPhaseA
            // 
            cbxCurrentPhaseA.AutoSize = true;
            cbxCurrentPhaseA.Location = new Point(280, 50);
            cbxCurrentPhaseA.Name = "cbxCurrentPhaseA";
            cbxCurrentPhaseA.Size = new Size(84, 32);
            cbxCurrentPhaseA.TabIndex = 5;
            cbxCurrentPhaseA.Text = "A相";
            cbxCurrentPhaseA.UseVisualStyleBackColor = true;
            // 
            // cbxCurrentPhaseB
            // 
            cbxCurrentPhaseB.AutoSize = true;
            cbxCurrentPhaseB.Location = new Point(387, 50);
            cbxCurrentPhaseB.Name = "cbxCurrentPhaseB";
            cbxCurrentPhaseB.Size = new Size(84, 32);
            cbxCurrentPhaseB.TabIndex = 6;
            cbxCurrentPhaseB.Text = "B相";
            cbxCurrentPhaseB.UseVisualStyleBackColor = true;
            // 
            // cbxCurrentPhaseC
            // 
            cbxCurrentPhaseC.AutoSize = true;
            cbxCurrentPhaseC.Location = new Point(494, 50);
            cbxCurrentPhaseC.Name = "cbxCurrentPhaseC";
            cbxCurrentPhaseC.Size = new Size(84, 32);
            cbxCurrentPhaseC.TabIndex = 7;
            cbxCurrentPhaseC.Text = "C相";
            cbxCurrentPhaseC.UseVisualStyleBackColor = true;
            // 
            // groupBoxDailyTiming
            // 
            groupBoxDailyTiming.Controls.Add(labelDailyTimingCountdown);
            groupBoxDailyTiming.Controls.Add(btnStopDailyTiming);
            groupBoxDailyTiming.Controls.Add(btnGetDailyTimingResult);
            groupBoxDailyTiming.Controls.Add(btnStartDailyTiming);
            groupBoxDailyTiming.Controls.Add(tbxDailyTimingCount);
            groupBoxDailyTiming.Controls.Add(labelDailyTimingCount);
            groupBoxDailyTiming.Controls.Add(tbxDailyTimingTime);
            groupBoxDailyTiming.Controls.Add(labelDailyTimingTime);
            groupBoxDailyTiming.Location = new Point(24, 378);
            groupBoxDailyTiming.Name = "groupBoxDailyTiming";
            groupBoxDailyTiming.Size = new Size(980, 120);
            groupBoxDailyTiming.TabIndex = 3;
            groupBoxDailyTiming.TabStop = false;
            groupBoxDailyTiming.Text = "日计时试验";
            // 
            // labelDailyTimingTime
            // 
            labelDailyTimingTime.AutoSize = true;
            labelDailyTimingTime.Location = new Point(16, 50);
            labelDailyTimingTime.Name = "labelDailyTimingTime";
            labelDailyTimingTime.Size = new Size(59, 28);
            labelDailyTimingTime.TabIndex = 0;
            labelDailyTimingTime.Text = "时间";
            // 
            // tbxDailyTimingTime
            // 
            tbxDailyTimingTime.Location = new Point(81, 47);
            tbxDailyTimingTime.Name = "tbxDailyTimingTime";
            tbxDailyTimingTime.Size = new Size(84, 34);
            tbxDailyTimingTime.TabIndex = 1;
            tbxDailyTimingTime.Text = "10";
            // 
            // labelDailyTimingCount
            // 
            labelDailyTimingCount.AutoSize = true;
            labelDailyTimingCount.Location = new Point(188, 50);
            labelDailyTimingCount.Name = "labelDailyTimingCount";
            labelDailyTimingCount.Size = new Size(59, 28);
            labelDailyTimingCount.TabIndex = 2;
            labelDailyTimingCount.Text = "次数";
            // 
            // tbxDailyTimingCount
            // 
            tbxDailyTimingCount.Location = new Point(253, 47);
            tbxDailyTimingCount.Name = "tbxDailyTimingCount";
            tbxDailyTimingCount.Size = new Size(84, 34);
            tbxDailyTimingCount.TabIndex = 3;
            tbxDailyTimingCount.Text = "10";
            // 
            // btnStartDailyTiming
            // 
            btnStartDailyTiming.Location = new Point(368, 39);
            btnStartDailyTiming.Name = "btnStartDailyTiming";
            btnStartDailyTiming.Size = new Size(170, 46);
            btnStartDailyTiming.TabIndex = 4;
            btnStartDailyTiming.Text = "开始日计时";
            btnStartDailyTiming.UseVisualStyleBackColor = true;
            btnStartDailyTiming.Click += btnStartDailyTiming_Click;
            // 
            // btnGetDailyTimingResult
            // 
            btnGetDailyTimingResult.Location = new Point(566, 39);
            btnGetDailyTimingResult.Name = "btnGetDailyTimingResult";
            btnGetDailyTimingResult.Size = new Size(190, 46);
            btnGetDailyTimingResult.TabIndex = 5;
            btnGetDailyTimingResult.Text = "日计时结果获取";
            btnGetDailyTimingResult.UseVisualStyleBackColor = true;
            btnGetDailyTimingResult.Click += btnGetDailyTimingResult_Click;
            // 
            // btnStopDailyTiming
            // 
            btnStopDailyTiming.Location = new Point(784, 39);
            btnStopDailyTiming.Name = "btnStopDailyTiming";
            btnStopDailyTiming.Size = new Size(170, 46);
            btnStopDailyTiming.TabIndex = 6;
            btnStopDailyTiming.Text = "停止日计时";
            btnStopDailyTiming.UseVisualStyleBackColor = true;
            btnStopDailyTiming.Click += btnStopDailyTiming_Click;
            // 
            // labelDailyTimingCountdown
            // 
            labelDailyTimingCountdown.AutoSize = true;
            labelDailyTimingCountdown.ForeColor = Color.FromArgb(255, 255, 192);
            labelDailyTimingCountdown.Location = new Point(16, 86);
            labelDailyTimingCountdown.Name = "labelDailyTimingCountdown";
            labelDailyTimingCountdown.Size = new Size(131, 28);
            labelDailyTimingCountdown.TabIndex = 7;
            labelDailyTimingCountdown.Text = "倒计时：未开始";
            // 
            // groupBoxStationDetection
            // 
            groupBoxStationDetection.Controls.Add(labelStationDetectionSummary);
            groupBoxStationDetection.Controls.Add(btnGetMeterPresenceDetectionResult);
            groupBoxStationDetection.Controls.Add(btnStartMeterPresenceDetection);
            groupBoxStationDetection.Controls.Add(labelMeterPresenceDetection);
            groupBoxStationDetection.Controls.Add(btnGetVoltageShortCircuitDetectionResult);
            groupBoxStationDetection.Controls.Add(btnStartVoltageShortCircuitDetection);
            groupBoxStationDetection.Controls.Add(labelVoltageShortCircuitDetection);
            groupBoxStationDetection.Location = new Point(24, 516);
            groupBoxStationDetection.Name = "groupBoxStationDetection";
            groupBoxStationDetection.Size = new Size(980, 176);
            groupBoxStationDetection.TabIndex = 4;
            groupBoxStationDetection.TabStop = false;
            groupBoxStationDetection.Text = "表位检测";
            // 
            // labelVoltageShortCircuitDetection
            // 
            labelVoltageShortCircuitDetection.AutoSize = true;
            labelVoltageShortCircuitDetection.Location = new Point(16, 44);
            labelVoltageShortCircuitDetection.Name = "labelVoltageShortCircuitDetection";
            labelVoltageShortCircuitDetection.Size = new Size(194, 28);
            labelVoltageShortCircuitDetection.TabIndex = 0;
            labelVoltageShortCircuitDetection.Text = "表位电压短路检测";
            // 
            // btnStartVoltageShortCircuitDetection
            // 
            btnStartVoltageShortCircuitDetection.Location = new Point(280, 34);
            btnStartVoltageShortCircuitDetection.Name = "btnStartVoltageShortCircuitDetection";
            btnStartVoltageShortCircuitDetection.Size = new Size(170, 46);
            btnStartVoltageShortCircuitDetection.TabIndex = 1;
            btnStartVoltageShortCircuitDetection.Text = "开始检测";
            btnStartVoltageShortCircuitDetection.UseVisualStyleBackColor = true;
            btnStartVoltageShortCircuitDetection.Click += btnStartVoltageShortCircuitDetection_Click;
            // 
            // btnGetVoltageShortCircuitDetectionResult
            // 
            btnGetVoltageShortCircuitDetectionResult.Location = new Point(490, 34);
            btnGetVoltageShortCircuitDetectionResult.Name = "btnGetVoltageShortCircuitDetectionResult";
            btnGetVoltageShortCircuitDetectionResult.Size = new Size(170, 46);
            btnGetVoltageShortCircuitDetectionResult.TabIndex = 2;
            btnGetVoltageShortCircuitDetectionResult.Text = "结果获取";
            btnGetVoltageShortCircuitDetectionResult.UseVisualStyleBackColor = true;
            btnGetVoltageShortCircuitDetectionResult.Click += btnGetVoltageShortCircuitDetectionResult_Click;
            // 
            // labelMeterPresenceDetection
            // 
            labelMeterPresenceDetection.AutoSize = true;
            labelMeterPresenceDetection.Location = new Point(16, 98);
            labelMeterPresenceDetection.Name = "labelMeterPresenceDetection";
            labelMeterPresenceDetection.Size = new Size(194, 28);
            labelMeterPresenceDetection.TabIndex = 3;
            labelMeterPresenceDetection.Text = "表位有无电表检测";
            // 
            // btnStartMeterPresenceDetection
            // 
            btnStartMeterPresenceDetection.Location = new Point(280, 88);
            btnStartMeterPresenceDetection.Name = "btnStartMeterPresenceDetection";
            btnStartMeterPresenceDetection.Size = new Size(170, 46);
            btnStartMeterPresenceDetection.TabIndex = 4;
            btnStartMeterPresenceDetection.Text = "开始检测";
            btnStartMeterPresenceDetection.UseVisualStyleBackColor = true;
            btnStartMeterPresenceDetection.Click += btnStartMeterPresenceDetection_Click;
            // 
            // btnGetMeterPresenceDetectionResult
            // 
            btnGetMeterPresenceDetectionResult.Location = new Point(490, 88);
            btnGetMeterPresenceDetectionResult.Name = "btnGetMeterPresenceDetectionResult";
            btnGetMeterPresenceDetectionResult.Size = new Size(170, 46);
            btnGetMeterPresenceDetectionResult.TabIndex = 5;
            btnGetMeterPresenceDetectionResult.Text = "结果获取";
            btnGetMeterPresenceDetectionResult.UseVisualStyleBackColor = true;
            btnGetMeterPresenceDetectionResult.Click += btnGetMeterPresenceDetectionResult_Click;
            // 
            // labelStationDetectionSummary
            // 
            labelStationDetectionSummary.BorderStyle = BorderStyle.FixedSingle;
            labelStationDetectionSummary.Location = new Point(690, 30);
            labelStationDetectionSummary.Name = "labelStationDetectionSummary";
            labelStationDetectionSummary.Size = new Size(270, 114);
            labelStationDetectionSummary.TabIndex = 6;
            labelStationDetectionSummary.Text = "电压短路检测：未检测\r\n有无电表/电流线路：未检测";
            // 
            // groupBoxMotorCrimping
            // 
            groupBoxMotorCrimping.Controls.Add(btnMotorCrimpPowerOff);
            groupBoxMotorCrimping.Controls.Add(btnMotorCrimpRelease);
            groupBoxMotorCrimping.Controls.Add(btnMotorCrimpPress);
            groupBoxMotorCrimping.Location = new Point(24, 710);
            groupBoxMotorCrimping.Name = "groupBoxMotorCrimping";
            groupBoxMotorCrimping.Size = new Size(620, 104);
            groupBoxMotorCrimping.TabIndex = 5;
            groupBoxMotorCrimping.TabStop = false;
            groupBoxMotorCrimping.Text = "电机压接";
            // 
            // btnMotorCrimpPress
            // 
            btnMotorCrimpPress.Location = new Point(12, 38);
            btnMotorCrimpPress.Name = "btnMotorCrimpPress";
            btnMotorCrimpPress.Size = new Size(170, 46);
            btnMotorCrimpPress.TabIndex = 0;
            btnMotorCrimpPress.Text = "压接";
            btnMotorCrimpPress.UseVisualStyleBackColor = true;
            btnMotorCrimpPress.Click += btnMotorCrimpPress_Click;
            // 
            // btnMotorCrimpRelease
            // 
            btnMotorCrimpRelease.Location = new Point(224, 38);
            btnMotorCrimpRelease.Name = "btnMotorCrimpRelease";
            btnMotorCrimpRelease.Size = new Size(170, 46);
            btnMotorCrimpRelease.TabIndex = 1;
            btnMotorCrimpRelease.Text = "弹开";
            btnMotorCrimpRelease.UseVisualStyleBackColor = true;
            btnMotorCrimpRelease.Click += btnMotorCrimpRelease_Click;
            // 
            // btnMotorCrimpPowerOff
            // 
            btnMotorCrimpPowerOff.Location = new Point(436, 38);
            btnMotorCrimpPowerOff.Name = "btnMotorCrimpPowerOff";
            btnMotorCrimpPowerOff.Size = new Size(170, 46);
            btnMotorCrimpPowerOff.TabIndex = 2;
            btnMotorCrimpPowerOff.Text = "电机断电";
            btnMotorCrimpPowerOff.UseVisualStyleBackColor = true;
            btnMotorCrimpPowerOff.Click += btnMotorCrimpPowerOff_Click;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(tbxMeterV1Addr);
            groupBox3.Controls.Add(label4);
            groupBox3.Dock = DockStyle.Top;
            groupBox3.Location = new Point(3, 217);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(344, 156);
            groupBox3.TabIndex = 1;
            groupBox3.TabStop = false;
            groupBox3.Text = "表位地址";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.BackColor = Color.Transparent;
            label4.ForeColor = Color.Transparent;
            label4.Location = new Point(6, 42);
            label4.Name = "label4";
            label4.Size = new Size(306, 28);
            label4.TabIndex = 6;
            label4.Text = "输入表位地址，默认1，广播AA";
            // 
            // tbxMeterV1Addr
            // 
            tbxMeterV1Addr.Location = new Point(16, 88);
            tbxMeterV1Addr.Name = "tbxMeterV1Addr";
            tbxMeterV1Addr.Size = new Size(296, 34);
            tbxMeterV1Addr.TabIndex = 7;
            tbxMeterV1Addr.Text = "1";
            // 
            // ElectricEnergyMeterControlV1
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panel1);
            Controls.Add(groupBox1);
            Name = "ElectricEnergyMeterControlV1";
            Size = new Size(2588, 1024);
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            groupBoxBasicCommand.ResumeLayout(false);
            groupBoxAcVoltageControl.ResumeLayout(false);
            groupBoxAcVoltageControl.PerformLayout();
            groupBoxAcCurrentControl.ResumeLayout(false);
            groupBoxAcCurrentControl.PerformLayout();
            groupBoxDailyTiming.ResumeLayout(false);
            groupBoxDailyTiming.PerformLayout();
            groupBoxStationDetection.ResumeLayout(false);
            groupBoxStationDetection.PerformLayout();
            groupBoxMotorCrimping.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Panel panel1;
        private Label label2;
        private Label label1;
        private TextBox tbx_MeterIP;
        private TextBox tbx_MeterPort;
        private Button btn_MeterTCPConnect;
        private Label label3;
        private GroupBox groupBox3;
        private Label label4;
        private TextBox tbxMeterV1Addr;
        private Button btnTestMeterCommunication;
        private Button btnResetCommand;
        private Button btnAcVoltagePower;
        private CheckBox cbxPhaseA;
        private CheckBox cbxPhaseB;
        private CheckBox cbxPhaseC;
        private Button btnAcCurrentPower;
        private CheckBox cbxCurrentPhaseA;
        private CheckBox cbxCurrentPhaseB;
        private CheckBox cbxCurrentPhaseC;
        private GroupBox groupBoxBasicCommand;
        private GroupBox groupBoxAcVoltageControl;
        private GroupBox groupBoxAcCurrentControl;
        private GroupBox groupBoxDailyTiming;
        private Label labelDailyTimingTime;
        private TextBox tbxDailyTimingTime;
        private Label labelDailyTimingCount;
        private TextBox tbxDailyTimingCount;
        private Button btnStartDailyTiming;
        private Button btnGetDailyTimingResult;
        private Button btnStopDailyTiming;
        private Label labelDailyTimingCountdown;
        private GroupBox groupBoxStationDetection;
        private Label labelVoltageShortCircuitDetection;
        private Button btnStartVoltageShortCircuitDetection;
        private Button btnGetVoltageShortCircuitDetectionResult;
        private Label labelMeterPresenceDetection;
        private Button btnStartMeterPresenceDetection;
        private Button btnGetMeterPresenceDetectionResult;
        private Label labelStationDetectionSummary;
        private GroupBox groupBoxMotorCrimping;
        private Button btnMotorCrimpPress;
        private Button btnMotorCrimpRelease;
        private Button btnMotorCrimpPowerOff;
    }
}
