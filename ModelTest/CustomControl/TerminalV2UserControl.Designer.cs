namespace ModelTest.CustomControl
{
    partial class TerminalV2UserControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeTcpClient();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            groupTcpClient = new GroupBox();
            connectionLayout = new TableLayoutPanel();
            lblIp = new Label();
            ipTextBox = new TextBox();
            lblPort = new Label();
            portTextBox = new TextBox();
            lblStation = new Label();
            stationTextBox = new TextBox();
            connectButton = new Button();
            lblStatus = new Label();
            operationPanel = new Panel();
            groupOperation = new GroupBox();
            operationLayout = new FlowLayoutPanel();
            lblModuleNumber = new Label();
            moduleNumberTextBox = new TextBox();
            btnResetMcu = new Button();
            btnReadVersion = new Button();
            groupRealModulePower = new GroupBox();
            realModulePowerLayout = new FlowLayoutPanel();
            btnDcPowerOn = new Button();
            btnDcPowerOff = new Button();
            btnAcPowerOn = new Button();
            btnAcPowerOff = new Button();
            groupCarrierModulePower = new GroupBox();
            carrierModulePowerLayout = new FlowLayoutPanel();
            btnCarrierDcPowerOn = new Button();
            btnCarrierDcPowerOff = new Button();
            btnCarrierAcPowerOn = new Button();
            btnCarrierAcPowerOff = new Button();
            groupCarrierPinLevel = new GroupBox();
            carrierPinLevelLayout = new FlowLayoutPanel();
            btnSetRstHigh = new Button();
            btnSetRstLow = new Button();
            btnSetSetHigh = new Button();
            btnSetSetLow = new Button();
            btnSetEventHigh = new Button();
            btnSetEventLow = new Button();
            btnReadStaPin = new Button();
            groupVoltageCurrentControl = new GroupBox();
            voltageCurrentLayout = new TableLayoutPanel();
            chkVoltageUa = new CheckBox();
            chkVoltageUb = new CheckBox();
            chkVoltageUc = new CheckBox();
            btnVoltageOn = new Button();
            btnVoltageOff = new Button();
            chkCurrentIa = new CheckBox();
            chkCurrentIb = new CheckBox();
            chkCurrentIc = new CheckBox();
            chkCurrentIn = new CheckBox();
            btnCurrentOn = new Button();
            btnCurrentOff = new Button();
            btnSwitchCurrentLoop = new Button();
            meterVoltageLoopPowerComboBox = new ComboBox();
            btnReadMeterVoltageLoopPower = new Button();
            btnMeterMotorPress = new Button();
            btnMeterMotorRelease = new Button();
            btnMagnetMotorPress = new Button();
            btnMagnetMotorRelease = new Button();
            groupDcMeasure = new GroupBox();
            dcMeasureLayout = new TableLayoutPanel();
            measureControlLayout = new FlowLayoutPanel();
            measureTopRowPanel = new FlowLayoutPanel();
            measureBottomRowPanel = new FlowLayoutPanel();
            lblMeasureModuleType = new Label();
            measureModuleTypeComboBox = new ComboBox();
            lblMeasureReadItem = new Label();
            measureReadItemComboBox = new ComboBox();
            lblMeasureMode = new Label();
            measureModeComboBox = new ComboBox();
            lblMeasureRate = new Label();
            measureRateTextBox = new TextBox();
            btnMeasureSend = new Button();
            measureResultTextBox = new TextBox();
            groupVirtualModuleControl = new GroupBox();
            virtualModuleLayout = new FlowLayoutPanel();
            lblVirtualModuleMode = new Label();
            virtualModuleModeComboBox = new ComboBox();
            lblVirtualModuleType = new Label();
            virtualModuleTypeComboBox = new ComboBox();
            btnSetVirtualModuleType = new Button();
            lblVirtualUsbState = new Label();
            virtualUsbStateComboBox = new ComboBox();
            btnSetVirtualUsbState = new Button();
            lblVirtualLoad = new Label();
            virtualLoadTypeComboBox = new ComboBox();
            virtualLoadStateComboBox = new ComboBox();
            btnSetVirtualLoad = new Button();
            lblVirtualRipple = new Label();
            virtualRippleStateComboBox = new ComboBox();
            btnSetVirtualRipple = new Button();
            lblVirtualVoltageMode = new Label();
            virtualVoltageModeComboBox = new ComboBox();
            btnReadVirtualVoltage = new Button();
            lblVirtualStatusPin = new Label();
            virtualStatusPinComboBox = new ComboBox();
            btnSetVirtualStatusPin = new Button();
            lblVirtualPinRead = new Label();
            virtualPinTypeComboBox = new ComboBox();
            virtualPinSequenceComboBox = new ComboBox();
            btnReadVirtualPinTime = new Button();
            lblVirtualCacheClear = new Label();
            virtualClearOnOffCheckBox = new CheckBox();
            virtualClearRstCheckBox = new CheckBox();
            btnClearVirtualPinCache = new Button();
            btnSetVirtualActiveReport = new Button();
            btnSwitchVirtualModule = new Button();
            btnSwitchRealModule = new Button();
            groupTcpClient.SuspendLayout();
            connectionLayout.SuspendLayout();
            operationPanel.SuspendLayout();
            groupOperation.SuspendLayout();
            operationLayout.SuspendLayout();
            groupRealModulePower.SuspendLayout();
            realModulePowerLayout.SuspendLayout();
            groupCarrierModulePower.SuspendLayout();
            carrierModulePowerLayout.SuspendLayout();
            groupCarrierPinLevel.SuspendLayout();
            carrierPinLevelLayout.SuspendLayout();
            groupVoltageCurrentControl.SuspendLayout();
            voltageCurrentLayout.SuspendLayout();
            groupDcMeasure.SuspendLayout();
            dcMeasureLayout.SuspendLayout();
            measureControlLayout.SuspendLayout();
            measureTopRowPanel.SuspendLayout();
            measureBottomRowPanel.SuspendLayout();
            groupVirtualModuleControl.SuspendLayout();
            virtualModuleLayout.SuspendLayout();
            SuspendLayout();
            // 
            // groupTcpClient
            // 
            groupTcpClient.Controls.Add(connectionLayout);
            groupTcpClient.Dock = DockStyle.Top;
            groupTcpClient.Font = new Font("Microsoft YaHei UI", 9F);
            groupTcpClient.Location = new Point(0, 0);
            groupTcpClient.Name = "groupTcpClient";
            groupTcpClient.Padding = new Padding(12);
            groupTcpClient.Size = new Size(2588, 120);
            groupTcpClient.TabIndex = 0;
            groupTcpClient.TabStop = false;
            groupTcpClient.Text = "TCPClient连接";
            // 
            // operationPanel
            // 
            operationPanel.Controls.Add(groupVirtualModuleControl);
            operationPanel.Controls.Add(groupDcMeasure);
            operationPanel.Controls.Add(groupVoltageCurrentControl);
            operationPanel.Controls.Add(groupCarrierPinLevel);
            operationPanel.Controls.Add(groupCarrierModulePower);
            operationPanel.Controls.Add(groupRealModulePower);
            operationPanel.Controls.Add(groupOperation);
            operationPanel.AutoScroll = true;
            operationPanel.Dock = DockStyle.Fill;
            operationPanel.Location = new Point(0, 120);
            operationPanel.Name = "operationPanel";
            operationPanel.Size = new Size(2588, 944);
            operationPanel.TabIndex = 1;
            operationPanel.SizeChanged += operationPanel_SizeChanged;
            // 
            // groupOperation
            // 
            groupOperation.Controls.Add(operationLayout);
            groupOperation.Font = new Font("Microsoft YaHei UI", 9F);
            groupOperation.Location = new Point(24, 16);
            groupOperation.Name = "groupOperation";
            groupOperation.Padding = new Padding(12);
            groupOperation.Size = new Size(700, 92);
            groupOperation.TabIndex = 1;
            groupOperation.TabStop = false;
            groupOperation.Text = "操作区";
            // 
            // operationLayout
            // 
            operationLayout.Controls.Add(lblStation);
            operationLayout.Controls.Add(stationTextBox);
            operationLayout.Controls.Add(lblModuleNumber);
            operationLayout.Controls.Add(moduleNumberTextBox);
            operationLayout.Controls.Add(btnResetMcu);
            operationLayout.Controls.Add(btnReadVersion);
            operationLayout.Dock = DockStyle.Fill;
            operationLayout.FlowDirection = FlowDirection.LeftToRight;
            operationLayout.Location = new Point(12, 39);
            operationLayout.Name = "operationLayout";
            operationLayout.Size = new Size(676, 41);
            operationLayout.TabIndex = 0;
            operationLayout.WrapContents = true;
            // 
            // lblStation
            // 
            lblStation.Location = new Point(6, 8);
            lblStation.Margin = new Padding(6, 8, 0, 0);
            lblStation.Name = "lblStation";
            lblStation.Size = new Size(82, 34);
            lblStation.TabIndex = 0;
            lblStation.Text = "地址：";
            lblStation.TextAlign = ContentAlignment.MiddleRight;
            // 
            // stationTextBox
            // 
            stationTextBox.Location = new Point(96, 8);
            stationTextBox.Margin = new Padding(4, 8, 24, 0);
            stationTextBox.Name = "stationTextBox";
            stationTextBox.Size = new Size(80, 34);
            stationTextBox.TabIndex = 1;
            stationTextBox.Text = "1";
            stationTextBox.KeyPress += stationTextBox_KeyPress;
            // 
            // lblModuleNumber
            // 
            lblModuleNumber.Location = new Point(194, 8);
            lblModuleNumber.Margin = new Padding(0, 8, 0, 0);
            lblModuleNumber.Name = "lblModuleNumber";
            lblModuleNumber.Size = new Size(90, 34);
            lblModuleNumber.TabIndex = 2;
            lblModuleNumber.Text = "模块号：";
            lblModuleNumber.TextAlign = ContentAlignment.MiddleRight;
            // 
            // moduleNumberTextBox
            // 
            moduleNumberTextBox.Location = new Point(292, 8);
            moduleNumberTextBox.Margin = new Padding(8, 8, 24, 0);
            moduleNumberTextBox.Name = "moduleNumberTextBox";
            moduleNumberTextBox.Size = new Size(80, 34);
            moduleNumberTextBox.TabIndex = 3;
            moduleNumberTextBox.Text = "1";
            moduleNumberTextBox.KeyPress += moduleNumberTextBox_KeyPress;
            // 
            // btnResetMcu
            // 
            btnResetMcu.Location = new Point(396, 0);
            btnResetMcu.Margin = new Padding(0, 0, 16, 0);
            btnResetMcu.Name = "btnResetMcu";
            btnResetMcu.Size = new Size(112, 38);
            btnResetMcu.TabIndex = 4;
            btnResetMcu.Text = "复位";
            btnResetMcu.UseVisualStyleBackColor = true;
            btnResetMcu.Click += btnResetMcu_Click;
            // 
            // btnReadVersion
            // 
            btnReadVersion.Location = new Point(532, 0);
            btnReadVersion.Margin = new Padding(0, 0, 16, 0);
            btnReadVersion.Name = "btnReadVersion";
            btnReadVersion.Size = new Size(124, 38);
            btnReadVersion.TabIndex = 5;
            btnReadVersion.Text = "读取版本";
            btnReadVersion.UseVisualStyleBackColor = true;
            btnReadVersion.Click += btnReadVersion_Click;
            // 
            // groupRealModulePower
            // 
            groupRealModulePower.Controls.Add(realModulePowerLayout);
            groupRealModulePower.Font = new Font("Microsoft YaHei UI", 9F);
            groupRealModulePower.Location = new Point(24, 124);
            groupRealModulePower.Name = "groupRealModulePower";
            groupRealModulePower.Padding = new Padding(12);
            groupRealModulePower.Size = new Size(700, 104);
            groupRealModulePower.TabIndex = 2;
            groupRealModulePower.TabStop = false;
            groupRealModulePower.Text = "真实模组上电控制";
            // 
            // realModulePowerLayout
            // 
            realModulePowerLayout.Controls.Add(btnDcPowerOn);
            realModulePowerLayout.Controls.Add(btnDcPowerOff);
            realModulePowerLayout.Controls.Add(btnAcPowerOn);
            realModulePowerLayout.Controls.Add(btnAcPowerOff);
            realModulePowerLayout.Dock = DockStyle.Fill;
            realModulePowerLayout.FlowDirection = FlowDirection.LeftToRight;
            realModulePowerLayout.Location = new Point(12, 39);
            realModulePowerLayout.Name = "realModulePowerLayout";
            realModulePowerLayout.Size = new Size(676, 53);
            realModulePowerLayout.TabIndex = 0;
            realModulePowerLayout.WrapContents = true;
            // 
            // btnDcPowerOn
            // 
            btnDcPowerOn.Location = new Point(12, 3);
            btnDcPowerOn.Margin = new Padding(12, 3, 18, 0);
            btnDcPowerOn.Name = "btnDcPowerOn";
            btnDcPowerOn.Size = new Size(128, 38);
            btnDcPowerOn.TabIndex = 0;
            btnDcPowerOn.Text = "直流上电";
            btnDcPowerOn.UseVisualStyleBackColor = true;
            btnDcPowerOn.Click += btnDcPowerOn_Click;
            // 
            // btnDcPowerOff
            // 
            btnDcPowerOff.Location = new Point(190, 3);
            btnDcPowerOff.Margin = new Padding(0, 3, 18, 0);
            btnDcPowerOff.Name = "btnDcPowerOff";
            btnDcPowerOff.Size = new Size(128, 38);
            btnDcPowerOff.TabIndex = 1;
            btnDcPowerOff.Text = "直流断电";
            btnDcPowerOff.UseVisualStyleBackColor = true;
            btnDcPowerOff.Click += btnDcPowerOff_Click;
            // 
            // btnAcPowerOn
            // 
            btnAcPowerOn.Location = new Point(368, 3);
            btnAcPowerOn.Margin = new Padding(0, 3, 18, 0);
            btnAcPowerOn.Name = "btnAcPowerOn";
            btnAcPowerOn.Size = new Size(148, 38);
            btnAcPowerOn.TabIndex = 2;
            btnAcPowerOn.Text = "交流220V上电";
            btnAcPowerOn.UseVisualStyleBackColor = true;
            btnAcPowerOn.Click += btnAcPowerOn_Click;
            // 
            // btnAcPowerOff
            // 
            btnAcPowerOff.Location = new Point(566, 3);
            btnAcPowerOff.Margin = new Padding(0, 3, 18, 0);
            btnAcPowerOff.Name = "btnAcPowerOff";
            btnAcPowerOff.Size = new Size(148, 38);
            btnAcPowerOff.TabIndex = 3;
            btnAcPowerOff.Text = "交流220V断电";
            btnAcPowerOff.UseVisualStyleBackColor = true;
            btnAcPowerOff.Click += btnAcPowerOff_Click;
            // 
            // groupCarrierModulePower
            // 
            groupCarrierModulePower.Controls.Add(carrierModulePowerLayout);
            groupCarrierModulePower.Font = new Font("Microsoft YaHei UI", 9F);
            groupCarrierModulePower.Location = new Point(24, 244);
            groupCarrierModulePower.Name = "groupCarrierModulePower";
            groupCarrierModulePower.Padding = new Padding(12);
            groupCarrierModulePower.Size = new Size(700, 104);
            groupCarrierModulePower.TabIndex = 3;
            groupCarrierModulePower.TabStop = false;
            groupCarrierModulePower.Text = "载波模组上电控制";
            // 
            // carrierModulePowerLayout
            // 
            carrierModulePowerLayout.Controls.Add(btnCarrierDcPowerOn);
            carrierModulePowerLayout.Controls.Add(btnCarrierDcPowerOff);
            carrierModulePowerLayout.Controls.Add(btnCarrierAcPowerOn);
            carrierModulePowerLayout.Controls.Add(btnCarrierAcPowerOff);
            carrierModulePowerLayout.Dock = DockStyle.Fill;
            carrierModulePowerLayout.FlowDirection = FlowDirection.LeftToRight;
            carrierModulePowerLayout.Location = new Point(12, 39);
            carrierModulePowerLayout.Name = "carrierModulePowerLayout";
            carrierModulePowerLayout.Size = new Size(676, 53);
            carrierModulePowerLayout.TabIndex = 0;
            carrierModulePowerLayout.WrapContents = true;
            // 
            // btnCarrierDcPowerOn
            // 
            btnCarrierDcPowerOn.Location = new Point(12, 3);
            btnCarrierDcPowerOn.Margin = new Padding(12, 3, 18, 0);
            btnCarrierDcPowerOn.Name = "btnCarrierDcPowerOn";
            btnCarrierDcPowerOn.Size = new Size(128, 38);
            btnCarrierDcPowerOn.TabIndex = 0;
            btnCarrierDcPowerOn.Text = "直流上电";
            btnCarrierDcPowerOn.UseVisualStyleBackColor = true;
            btnCarrierDcPowerOn.Click += btnCarrierDcPowerOn_Click;
            // 
            // btnCarrierDcPowerOff
            // 
            btnCarrierDcPowerOff.Location = new Point(190, 3);
            btnCarrierDcPowerOff.Margin = new Padding(0, 3, 18, 0);
            btnCarrierDcPowerOff.Name = "btnCarrierDcPowerOff";
            btnCarrierDcPowerOff.Size = new Size(128, 38);
            btnCarrierDcPowerOff.TabIndex = 1;
            btnCarrierDcPowerOff.Text = "直流断电";
            btnCarrierDcPowerOff.UseVisualStyleBackColor = true;
            btnCarrierDcPowerOff.Click += btnCarrierDcPowerOff_Click;
            // 
            // btnCarrierAcPowerOn
            // 
            btnCarrierAcPowerOn.Location = new Point(368, 3);
            btnCarrierAcPowerOn.Margin = new Padding(0, 3, 18, 0);
            btnCarrierAcPowerOn.Name = "btnCarrierAcPowerOn";
            btnCarrierAcPowerOn.Size = new Size(148, 38);
            btnCarrierAcPowerOn.TabIndex = 2;
            btnCarrierAcPowerOn.Text = "交流220V上电";
            btnCarrierAcPowerOn.UseVisualStyleBackColor = true;
            btnCarrierAcPowerOn.Click += btnCarrierAcPowerOn_Click;
            // 
            // btnCarrierAcPowerOff
            // 
            btnCarrierAcPowerOff.Location = new Point(566, 3);
            btnCarrierAcPowerOff.Margin = new Padding(0, 3, 18, 0);
            btnCarrierAcPowerOff.Name = "btnCarrierAcPowerOff";
            btnCarrierAcPowerOff.Size = new Size(148, 38);
            btnCarrierAcPowerOff.TabIndex = 3;
            btnCarrierAcPowerOff.Text = "交流220V断电";
            btnCarrierAcPowerOff.UseVisualStyleBackColor = true;
            btnCarrierAcPowerOff.Click += btnCarrierAcPowerOff_Click;
            // 
            // groupCarrierPinLevel
            // 
            groupCarrierPinLevel.Controls.Add(carrierPinLevelLayout);
            groupCarrierPinLevel.Font = new Font("Microsoft YaHei UI", 9F);
            groupCarrierPinLevel.Location = new Point(24, 364);
            groupCarrierPinLevel.Name = "groupCarrierPinLevel";
            groupCarrierPinLevel.Padding = new Padding(12);
            groupCarrierPinLevel.Size = new Size(700, 156);
            groupCarrierPinLevel.TabIndex = 4;
            groupCarrierPinLevel.TabStop = false;
            groupCarrierPinLevel.Text = "载波模组引脚电平控制";
            // 
            // carrierPinLevelLayout
            // 
            carrierPinLevelLayout.Controls.Add(btnSetRstHigh);
            carrierPinLevelLayout.Controls.Add(btnSetRstLow);
            carrierPinLevelLayout.Controls.Add(btnSetSetHigh);
            carrierPinLevelLayout.Controls.Add(btnSetSetLow);
            carrierPinLevelLayout.Controls.Add(btnSetEventHigh);
            carrierPinLevelLayout.Controls.Add(btnSetEventLow);
            carrierPinLevelLayout.Controls.Add(btnReadStaPin);
            carrierPinLevelLayout.Dock = DockStyle.Fill;
            carrierPinLevelLayout.FlowDirection = FlowDirection.LeftToRight;
            carrierPinLevelLayout.Location = new Point(12, 39);
            carrierPinLevelLayout.Name = "carrierPinLevelLayout";
            carrierPinLevelLayout.Size = new Size(676, 105);
            carrierPinLevelLayout.TabIndex = 0;
            carrierPinLevelLayout.WrapContents = true;
            // 
            // btnSetRstHigh
            // 
            btnSetRstHigh.Location = new Point(12, 3);
            btnSetRstHigh.Margin = new Padding(12, 3, 12, 0);
            btnSetRstHigh.Name = "btnSetRstHigh";
            btnSetRstHigh.Size = new Size(92, 38);
            btnSetRstHigh.TabIndex = 0;
            btnSetRstHigh.Text = "RST高";
            btnSetRstHigh.UseVisualStyleBackColor = true;
            btnSetRstHigh.Click += btnSetRstHigh_Click;
            // 
            // btnSetRstLow
            // 
            btnSetRstLow.Location = new Point(154, 3);
            btnSetRstLow.Margin = new Padding(0, 3, 12, 0);
            btnSetRstLow.Name = "btnSetRstLow";
            btnSetRstLow.Size = new Size(92, 38);
            btnSetRstLow.TabIndex = 1;
            btnSetRstLow.Text = "RST低";
            btnSetRstLow.UseVisualStyleBackColor = true;
            btnSetRstLow.Click += btnSetRstLow_Click;
            // 
            // btnSetSetHigh
            // 
            btnSetSetHigh.Location = new Point(284, 3);
            btnSetSetHigh.Margin = new Padding(0, 3, 12, 0);
            btnSetSetHigh.Name = "btnSetSetHigh";
            btnSetSetHigh.Size = new Size(92, 38);
            btnSetSetHigh.TabIndex = 2;
            btnSetSetHigh.Text = "SET高";
            btnSetSetHigh.UseVisualStyleBackColor = true;
            btnSetSetHigh.Click += btnSetSetHigh_Click;
            // 
            // btnSetSetLow
            // 
            btnSetSetLow.Location = new Point(414, 3);
            btnSetSetLow.Margin = new Padding(0, 3, 12, 0);
            btnSetSetLow.Name = "btnSetSetLow";
            btnSetSetLow.Size = new Size(92, 38);
            btnSetSetLow.TabIndex = 3;
            btnSetSetLow.Text = "SET低";
            btnSetSetLow.UseVisualStyleBackColor = true;
            btnSetSetLow.Click += btnSetSetLow_Click;
            // 
            // btnSetEventHigh
            // 
            btnSetEventHigh.Location = new Point(544, 3);
            btnSetEventHigh.Margin = new Padding(0, 3, 12, 0);
            btnSetEventHigh.Name = "btnSetEventHigh";
            btnSetEventHigh.Size = new Size(108, 38);
            btnSetEventHigh.TabIndex = 4;
            btnSetEventHigh.Text = "EVENT高";
            btnSetEventHigh.UseVisualStyleBackColor = true;
            btnSetEventHigh.Click += btnSetEventHigh_Click;
            // 
            // btnSetEventLow
            // 
            btnSetEventLow.Location = new Point(688, 3);
            btnSetEventLow.Margin = new Padding(0, 3, 12, 0);
            btnSetEventLow.Name = "btnSetEventLow";
            btnSetEventLow.Size = new Size(108, 38);
            btnSetEventLow.TabIndex = 5;
            btnSetEventLow.Text = "EVENT低";
            btnSetEventLow.UseVisualStyleBackColor = true;
            btnSetEventLow.Click += btnSetEventLow_Click;
            // 
            // btnReadStaPin
            // 
            btnReadStaPin.Location = new Point(832, 3);
            btnReadStaPin.Margin = new Padding(0, 3, 12, 0);
            btnReadStaPin.Name = "btnReadStaPin";
            btnReadStaPin.Size = new Size(108, 38);
            btnReadStaPin.TabIndex = 6;
            btnReadStaPin.Text = "读取STA";
            btnReadStaPin.UseVisualStyleBackColor = true;
            btnReadStaPin.Click += btnReadStaPin_Click;
            // 
            // groupVoltageCurrentControl
            // 
            groupVoltageCurrentControl.Controls.Add(voltageCurrentLayout);
            groupVoltageCurrentControl.Font = new Font("Microsoft YaHei UI", 9F);
            groupVoltageCurrentControl.Location = new Point(748, 16);
            groupVoltageCurrentControl.Name = "groupVoltageCurrentControl";
            groupVoltageCurrentControl.Padding = new Padding(12);
            groupVoltageCurrentControl.Size = new Size(820, 230);
            groupVoltageCurrentControl.TabIndex = 5;
            groupVoltageCurrentControl.TabStop = false;
            groupVoltageCurrentControl.Text = "电压电流控制";
            // 
            // voltageCurrentLayout
            // 
            voltageCurrentLayout.ColumnCount = 7;
            voltageCurrentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            voltageCurrentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            voltageCurrentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            voltageCurrentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76F));
            voltageCurrentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            voltageCurrentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132F));
            voltageCurrentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            voltageCurrentLayout.Controls.Add(chkVoltageUa, 0, 0);
            voltageCurrentLayout.Controls.Add(chkVoltageUb, 1, 0);
            voltageCurrentLayout.Controls.Add(chkVoltageUc, 2, 0);
            voltageCurrentLayout.Controls.Add(btnVoltageOn, 4, 0);
            voltageCurrentLayout.Controls.Add(btnVoltageOff, 5, 0);
            voltageCurrentLayout.Controls.Add(chkCurrentIa, 0, 1);
            voltageCurrentLayout.Controls.Add(chkCurrentIb, 1, 1);
            voltageCurrentLayout.Controls.Add(chkCurrentIc, 2, 1);
            voltageCurrentLayout.Controls.Add(chkCurrentIn, 3, 1);
            voltageCurrentLayout.Controls.Add(btnCurrentOn, 4, 1);
            voltageCurrentLayout.Controls.Add(btnCurrentOff, 5, 1);
            voltageCurrentLayout.Controls.Add(meterVoltageLoopPowerComboBox, 0, 2);
            voltageCurrentLayout.Controls.Add(btnReadMeterVoltageLoopPower, 2, 2);
            voltageCurrentLayout.Controls.Add(btnSwitchCurrentLoop, 4, 2);
            voltageCurrentLayout.Controls.Add(btnMeterMotorPress, 0, 3);
            voltageCurrentLayout.Controls.Add(btnMeterMotorRelease, 2, 3);
            voltageCurrentLayout.Controls.Add(btnMagnetMotorPress, 4, 3);
            voltageCurrentLayout.Controls.Add(btnMagnetMotorRelease, 5, 3);
            voltageCurrentLayout.Dock = DockStyle.Fill;
            voltageCurrentLayout.Location = new Point(12, 39);
            voltageCurrentLayout.Name = "voltageCurrentLayout";
            voltageCurrentLayout.RowCount = 4;
            voltageCurrentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            voltageCurrentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            voltageCurrentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
            voltageCurrentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
            voltageCurrentLayout.Size = new Size(796, 179);
            voltageCurrentLayout.TabIndex = 0;
            // 
            // chkVoltageUa
            // 
            chkVoltageUa.Location = new Point(8, 4);
            chkVoltageUa.Margin = new Padding(8, 4, 0, 0);
            chkVoltageUa.Name = "chkVoltageUa";
            chkVoltageUa.Size = new Size(68, 34);
            chkVoltageUa.TabIndex = 0;
            chkVoltageUa.Text = "UA";
            chkVoltageUa.UseVisualStyleBackColor = true;
            // 
            // chkVoltageUb
            // 
            chkVoltageUb.Location = new Point(84, 4);
            chkVoltageUb.Margin = new Padding(8, 4, 0, 0);
            chkVoltageUb.Name = "chkVoltageUb";
            chkVoltageUb.Size = new Size(68, 34);
            chkVoltageUb.TabIndex = 1;
            chkVoltageUb.Text = "UB";
            chkVoltageUb.UseVisualStyleBackColor = true;
            // 
            // chkVoltageUc
            // 
            chkVoltageUc.Location = new Point(160, 4);
            chkVoltageUc.Margin = new Padding(8, 4, 0, 0);
            chkVoltageUc.Name = "chkVoltageUc";
            chkVoltageUc.Size = new Size(68, 34);
            chkVoltageUc.TabIndex = 2;
            chkVoltageUc.Text = "UC";
            chkVoltageUc.UseVisualStyleBackColor = true;
            // 
            // btnVoltageOn
            // 
            btnVoltageOn.Location = new Point(312, 3);
            btnVoltageOn.Margin = new Padding(8, 3, 8, 0);
            btnVoltageOn.Name = "btnVoltageOn";
            btnVoltageOn.Size = new Size(116, 38);
            btnVoltageOn.TabIndex = 3;
            btnVoltageOn.Text = "上电压";
            btnVoltageOn.UseVisualStyleBackColor = true;
            btnVoltageOn.Click += btnVoltageOn_Click;
            // 
            // btnVoltageOff
            // 
            btnVoltageOff.Location = new Point(444, 3);
            btnVoltageOff.Margin = new Padding(8, 3, 8, 0);
            btnVoltageOff.Name = "btnVoltageOff";
            btnVoltageOff.Size = new Size(116, 38);
            btnVoltageOff.TabIndex = 4;
            btnVoltageOff.Text = "下电压";
            btnVoltageOff.UseVisualStyleBackColor = true;
            btnVoltageOff.Click += btnVoltageOff_Click;
            // 
            // chkCurrentIa
            // 
            chkCurrentIa.Location = new Point(8, 50);
            chkCurrentIa.Margin = new Padding(8, 4, 0, 0);
            chkCurrentIa.Name = "chkCurrentIa";
            chkCurrentIa.Size = new Size(68, 34);
            chkCurrentIa.TabIndex = 5;
            chkCurrentIa.Text = "IA";
            chkCurrentIa.UseVisualStyleBackColor = true;
            // 
            // chkCurrentIb
            // 
            chkCurrentIb.Location = new Point(84, 50);
            chkCurrentIb.Margin = new Padding(8, 4, 0, 0);
            chkCurrentIb.Name = "chkCurrentIb";
            chkCurrentIb.Size = new Size(68, 34);
            chkCurrentIb.TabIndex = 6;
            chkCurrentIb.Text = "IB";
            chkCurrentIb.UseVisualStyleBackColor = true;
            // 
            // chkCurrentIc
            // 
            chkCurrentIc.Location = new Point(160, 50);
            chkCurrentIc.Margin = new Padding(8, 4, 0, 0);
            chkCurrentIc.Name = "chkCurrentIc";
            chkCurrentIc.Size = new Size(68, 34);
            chkCurrentIc.TabIndex = 7;
            chkCurrentIc.Text = "IC";
            chkCurrentIc.UseVisualStyleBackColor = true;
            chkCurrentIc.CheckedChanged += chkCurrentIc_CheckedChanged;
            // 
            // chkCurrentIn
            // 
            chkCurrentIn.Location = new Point(236, 50);
            chkCurrentIn.Margin = new Padding(8, 4, 0, 0);
            chkCurrentIn.Name = "chkCurrentIn";
            chkCurrentIn.Size = new Size(68, 34);
            chkCurrentIn.TabIndex = 8;
            chkCurrentIn.Text = "IN";
            chkCurrentIn.UseVisualStyleBackColor = true;
            chkCurrentIn.CheckedChanged += chkCurrentIn_CheckedChanged;
            // 
            // btnCurrentOn
            // 
            btnCurrentOn.Location = new Point(312, 49);
            btnCurrentOn.Margin = new Padding(8, 3, 8, 0);
            btnCurrentOn.Name = "btnCurrentOn";
            btnCurrentOn.Size = new Size(116, 38);
            btnCurrentOn.TabIndex = 9;
            btnCurrentOn.Text = "上电流";
            btnCurrentOn.UseVisualStyleBackColor = true;
            btnCurrentOn.Click += btnCurrentOn_Click;
            // 
            // btnCurrentOff
            // 
            btnCurrentOff.Location = new Point(444, 49);
            btnCurrentOff.Margin = new Padding(8, 3, 8, 0);
            btnCurrentOff.Name = "btnCurrentOff";
            btnCurrentOff.Size = new Size(116, 38);
            btnCurrentOff.TabIndex = 10;
            btnCurrentOff.Text = "下电流";
            btnCurrentOff.UseVisualStyleBackColor = true;
            btnCurrentOff.Click += btnCurrentOff_Click;
            // 
            // btnSwitchCurrentLoop
            // 
            btnSwitchCurrentLoop.Location = new Point(312, 95);
            btnSwitchCurrentLoop.Margin = new Padding(8, 3, 8, 0);
            btnSwitchCurrentLoop.Name = "btnSwitchCurrentLoop";
            btnSwitchCurrentLoop.Size = new Size(160, 38);
            btnSwitchCurrentLoop.TabIndex = 11;
            btnSwitchCurrentLoop.Text = "切电流";
            btnSwitchCurrentLoop.UseVisualStyleBackColor = true;
            btnSwitchCurrentLoop.Click += btnSwitchCurrentLoop_Click;
            // 
            // meterVoltageLoopPowerComboBox
            // 
            meterVoltageLoopPowerComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            meterVoltageLoopPowerComboBox.FormattingEnabled = true;
            meterVoltageLoopPowerComboBox.Items.AddRange(new object[] { "A相功率", "B相功率", "C相功率", "合相功率", "校准/保存", "复位" });
            meterVoltageLoopPowerComboBox.Location = new Point(8, 98);
            meterVoltageLoopPowerComboBox.Margin = new Padding(8, 6, 8, 0);
            meterVoltageLoopPowerComboBox.Name = "meterVoltageLoopPowerComboBox";
            voltageCurrentLayout.SetColumnSpan(meterVoltageLoopPowerComboBox, 2);
            meterVoltageLoopPowerComboBox.Size = new Size(144, 36);
            meterVoltageLoopPowerComboBox.TabIndex = 12;
            // 
            // btnReadMeterVoltageLoopPower
            // 
            btnReadMeterVoltageLoopPower.Location = new Point(160, 95);
            btnReadMeterVoltageLoopPower.Margin = new Padding(8, 3, 8, 0);
            btnReadMeterVoltageLoopPower.Name = "btnReadMeterVoltageLoopPower";
            voltageCurrentLayout.SetColumnSpan(btnReadMeterVoltageLoopPower, 2);
            btnReadMeterVoltageLoopPower.Size = new Size(136, 38);
            btnReadMeterVoltageLoopPower.TabIndex = 13;
            btnReadMeterVoltageLoopPower.Text = "读取功率";
            btnReadMeterVoltageLoopPower.UseVisualStyleBackColor = true;
            btnReadMeterVoltageLoopPower.Click += btnReadMeterVoltageLoopPower_Click;
            // 
            // btnMeterMotorPress
            // 
            btnMeterMotorPress.Location = new Point(8, 141);
            btnMeterMotorPress.Margin = new Padding(8, 3, 8, 0);
            btnMeterMotorPress.Name = "btnMeterMotorPress";
            voltageCurrentLayout.SetColumnSpan(btnMeterMotorPress, 2);
            btnMeterMotorPress.Size = new Size(136, 38);
            btnMeterMotorPress.TabIndex = 14;
            btnMeterMotorPress.Text = "表位压接";
            btnMeterMotorPress.UseVisualStyleBackColor = true;
            btnMeterMotorPress.Click += btnMeterMotorPress_Click;
            // 
            // btnMeterMotorRelease
            // 
            btnMeterMotorRelease.Location = new Point(160, 141);
            btnMeterMotorRelease.Margin = new Padding(8, 3, 8, 0);
            btnMeterMotorRelease.Name = "btnMeterMotorRelease";
            voltageCurrentLayout.SetColumnSpan(btnMeterMotorRelease, 2);
            btnMeterMotorRelease.Size = new Size(136, 38);
            btnMeterMotorRelease.TabIndex = 15;
            btnMeterMotorRelease.Text = "表位断开";
            btnMeterMotorRelease.UseVisualStyleBackColor = true;
            btnMeterMotorRelease.Click += btnMeterMotorRelease_Click;
            // 
            // btnMagnetMotorPress
            // 
            btnMagnetMotorPress.Location = new Point(312, 141);
            btnMagnetMotorPress.Margin = new Padding(8, 3, 8, 0);
            btnMagnetMotorPress.Name = "btnMagnetMotorPress";
            btnMagnetMotorPress.Size = new Size(116, 38);
            btnMagnetMotorPress.TabIndex = 16;
            btnMagnetMotorPress.Text = "磁铁压接";
            btnMagnetMotorPress.UseVisualStyleBackColor = true;
            btnMagnetMotorPress.Click += btnMagnetMotorPress_Click;
            // 
            // btnMagnetMotorRelease
            // 
            btnMagnetMotorRelease.Location = new Point(444, 141);
            btnMagnetMotorRelease.Margin = new Padding(8, 3, 8, 0);
            btnMagnetMotorRelease.Name = "btnMagnetMotorRelease";
            btnMagnetMotorRelease.Size = new Size(116, 38);
            btnMagnetMotorRelease.TabIndex = 17;
            btnMagnetMotorRelease.Text = "磁铁断开";
            btnMagnetMotorRelease.UseVisualStyleBackColor = true;
            btnMagnetMotorRelease.Click += btnMagnetMotorRelease_Click;
            // 
            // groupDcMeasure
            // 
            groupDcMeasure.Controls.Add(dcMeasureLayout);
            groupDcMeasure.Font = new Font("Microsoft YaHei UI", 9F);
            groupDcMeasure.Location = new Point(748, 262);
            groupDcMeasure.Name = "groupDcMeasure";
            groupDcMeasure.Padding = new Padding(12);
            groupDcMeasure.Size = new Size(980, 244);
            groupDcMeasure.TabIndex = 6;
            groupDcMeasure.TabStop = false;
            groupDcMeasure.Text = "模组直流电压/电流/功耗读取";

            // 
            // groupVirtualModuleControl
            // 
            groupVirtualModuleControl.Controls.Add(virtualModuleLayout);
            groupVirtualModuleControl.Font = new Font("Microsoft YaHei UI", 9F);
            groupVirtualModuleControl.Location = new Point(2052, 16);
            groupVirtualModuleControl.Name = "groupVirtualModuleControl";
            groupVirtualModuleControl.Padding = new Padding(12);
            groupVirtualModuleControl.Size = new Size(512, 1460);
            groupVirtualModuleControl.TabIndex = 6;
            groupVirtualModuleControl.TabStop = false;
            groupVirtualModuleControl.Text = "虚拟模组控制";
            // 
            // virtualModuleLayout
            // 
            virtualModuleLayout.AutoScroll = false;
            virtualModuleLayout.Controls.Add(btnSwitchVirtualModule);
            virtualModuleLayout.Controls.Add(btnSwitchRealModule);
            virtualModuleLayout.Controls.Add(lblVirtualModuleMode);
            virtualModuleLayout.Controls.Add(virtualModuleModeComboBox);
            virtualModuleLayout.Controls.Add(lblVirtualModuleType);
            virtualModuleLayout.Controls.Add(virtualModuleTypeComboBox);
            virtualModuleLayout.Controls.Add(btnSetVirtualModuleType);
            virtualModuleLayout.Controls.Add(lblVirtualUsbState);
            virtualModuleLayout.Controls.Add(virtualUsbStateComboBox);
            virtualModuleLayout.Controls.Add(btnSetVirtualUsbState);
            virtualModuleLayout.Controls.Add(lblVirtualLoad);
            virtualModuleLayout.Controls.Add(virtualLoadTypeComboBox);
            virtualModuleLayout.Controls.Add(virtualLoadStateComboBox);
            virtualModuleLayout.Controls.Add(btnSetVirtualLoad);
            virtualModuleLayout.Controls.Add(lblVirtualRipple);
            virtualModuleLayout.Controls.Add(virtualRippleStateComboBox);
            virtualModuleLayout.Controls.Add(btnSetVirtualRipple);
            virtualModuleLayout.Controls.Add(lblVirtualVoltageMode);
            virtualModuleLayout.Controls.Add(virtualVoltageModeComboBox);
            virtualModuleLayout.Controls.Add(btnReadVirtualVoltage);
            virtualModuleLayout.Controls.Add(lblVirtualStatusPin);
            virtualModuleLayout.Controls.Add(virtualStatusPinComboBox);
            virtualModuleLayout.Controls.Add(btnSetVirtualStatusPin);
            virtualModuleLayout.Controls.Add(lblVirtualPinRead);
            virtualModuleLayout.Controls.Add(virtualPinTypeComboBox);
            virtualModuleLayout.Controls.Add(virtualPinSequenceComboBox);
            virtualModuleLayout.Controls.Add(btnReadVirtualPinTime);
            virtualModuleLayout.Controls.Add(lblVirtualCacheClear);
            virtualModuleLayout.Controls.Add(virtualClearOnOffCheckBox);
            virtualModuleLayout.Controls.Add(virtualClearRstCheckBox);
            virtualModuleLayout.Controls.Add(btnClearVirtualPinCache);
            virtualModuleLayout.Controls.Add(btnSetVirtualActiveReport);
            virtualModuleLayout.Dock = DockStyle.Fill;
            virtualModuleLayout.FlowDirection = FlowDirection.TopDown;
            virtualModuleLayout.Location = new Point(12, 39);
            virtualModuleLayout.Name = "virtualModuleLayout";
            virtualModuleLayout.Size = new Size(488, 1409);
            virtualModuleLayout.TabIndex = 0;
            virtualModuleLayout.WrapContents = false;
            // 
            // btnSwitchVirtualModule
            // 
            btnSwitchVirtualModule.Location = new Point(8, 3);
            btnSwitchVirtualModule.Margin = new Padding(8, 3, 8, 10);
            btnSwitchVirtualModule.Name = "btnSwitchVirtualModule";
            btnSwitchVirtualModule.Size = new Size(448, 38);
            btnSwitchVirtualModule.TabIndex = 0;
            btnSwitchVirtualModule.Text = "切换虚拟模组";
            btnSwitchVirtualModule.UseVisualStyleBackColor = true;
            btnSwitchVirtualModule.Click += btnSwitchVirtualModule_Click;
            // 
            // btnSwitchRealModule
            // 
            btnSwitchRealModule.Location = new Point(8, 62);
            btnSwitchRealModule.Margin = new Padding(8, 0, 8, 18);
            btnSwitchRealModule.Name = "btnSwitchRealModule";
            btnSwitchRealModule.Size = new Size(448, 38);
            btnSwitchRealModule.TabIndex = 1;
            btnSwitchRealModule.Text = "切换真实模组";
            btnSwitchRealModule.UseVisualStyleBackColor = true;
            btnSwitchRealModule.Click += btnSwitchRealModule_Click;
            // 
            // lblVirtualModuleMode
            // 
            lblVirtualModuleMode.Location = new Point(8, 126);
            lblVirtualModuleMode.Margin = new Padding(8, 0, 8, 4);
            lblVirtualModuleMode.Name = "lblVirtualModuleMode";
            lblVirtualModuleMode.Size = new Size(448, 30);
            lblVirtualModuleMode.TabIndex = 2;
            lblVirtualModuleMode.Text = "设置类型模式";
            lblVirtualModuleMode.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualModuleModeComboBox
            // 
            virtualModuleModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualModuleModeComboBox.FormattingEnabled = true;
            virtualModuleModeComboBox.Items.AddRange(new object[] { "互换性", "APP" });
            virtualModuleModeComboBox.Location = new Point(8, 160);
            virtualModuleModeComboBox.Margin = new Padding(8, 0, 8, 14);
            virtualModuleModeComboBox.Name = "virtualModuleModeComboBox";
            virtualModuleModeComboBox.Size = new Size(448, 36);
            virtualModuleModeComboBox.TabIndex = 3;
            virtualModuleModeComboBox.SelectedIndexChanged += virtualModuleModeComboBox_SelectedIndexChanged;
            // 
            // lblVirtualModuleType
            // 
            lblVirtualModuleType.Location = new Point(8, 210);
            lblVirtualModuleType.Margin = new Padding(8, 0, 8, 4);
            lblVirtualModuleType.Name = "lblVirtualModuleType";
            lblVirtualModuleType.Size = new Size(448, 30);
            lblVirtualModuleType.TabIndex = 4;
            lblVirtualModuleType.Text = "虚拟模组类型";
            lblVirtualModuleType.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualModuleTypeComboBox
            // 
            virtualModuleTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualModuleTypeComboBox.FormattingEnabled = true;
            virtualModuleTypeComboBox.Location = new Point(8, 244);
            virtualModuleTypeComboBox.Margin = new Padding(8, 0, 8, 18);
            virtualModuleTypeComboBox.Name = "virtualModuleTypeComboBox";
            virtualModuleTypeComboBox.Size = new Size(448, 36);
            virtualModuleTypeComboBox.TabIndex = 5;
            // 
            // btnSetVirtualModuleType
            // 
            btnSetVirtualModuleType.Location = new Point(8, 298);
            btnSetVirtualModuleType.Margin = new Padding(8, 0, 8, 14);
            btnSetVirtualModuleType.Name = "btnSetVirtualModuleType";
            btnSetVirtualModuleType.Size = new Size(448, 38);
            btnSetVirtualModuleType.TabIndex = 6;
            btnSetVirtualModuleType.Text = "设置虚拟模组类型";
            btnSetVirtualModuleType.UseVisualStyleBackColor = true;
            btnSetVirtualModuleType.Click += btnSetVirtualModuleType_Click;
            // 
            // lblVirtualUsbState
            // 
            lblVirtualUsbState.Location = new Point(8, 350);
            lblVirtualUsbState.Margin = new Padding(8, 0, 8, 4);
            lblVirtualUsbState.Name = "lblVirtualUsbState";
            lblVirtualUsbState.Size = new Size(448, 30);
            lblVirtualUsbState.TabIndex = 7;
            lblVirtualUsbState.Text = "USB连接状态";
            lblVirtualUsbState.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualUsbStateComboBox
            // 
            virtualUsbStateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualUsbStateComboBox.FormattingEnabled = true;
            virtualUsbStateComboBox.Items.AddRange(new object[] { "恢复连接", "断开连接", "USB重启" });
            virtualUsbStateComboBox.Location = new Point(8, 384);
            virtualUsbStateComboBox.Margin = new Padding(8, 0, 8, 6);
            virtualUsbStateComboBox.Name = "virtualUsbStateComboBox";
            virtualUsbStateComboBox.Size = new Size(448, 36);
            virtualUsbStateComboBox.TabIndex = 8;
            // 
            // btnSetVirtualUsbState
            // 
            btnSetVirtualUsbState.Location = new Point(8, 426);
            btnSetVirtualUsbState.Margin = new Padding(8, 0, 8, 14);
            btnSetVirtualUsbState.Name = "btnSetVirtualUsbState";
            btnSetVirtualUsbState.Size = new Size(448, 38);
            btnSetVirtualUsbState.TabIndex = 9;
            btnSetVirtualUsbState.Text = "设置USB状态";
            btnSetVirtualUsbState.UseVisualStyleBackColor = true;
            btnSetVirtualUsbState.Click += btnSetVirtualUsbState_Click;
            // 
            // lblVirtualLoad
            // 
            lblVirtualLoad.Location = new Point(8, 478);
            lblVirtualLoad.Margin = new Padding(8, 0, 8, 4);
            lblVirtualLoad.Name = "lblVirtualLoad";
            lblVirtualLoad.Size = new Size(448, 30);
            lblVirtualLoad.TabIndex = 10;
            lblVirtualLoad.Text = "带载控制";
            lblVirtualLoad.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualLoadTypeComboBox
            // 
            virtualLoadTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualLoadTypeComboBox.FormattingEnabled = true;
            virtualLoadTypeComboBox.Items.AddRange(new object[] { "稳态带载", "瞬态带载1", "瞬态带载2", "短路", "电源2带载", "散热风扇" });
            virtualLoadTypeComboBox.Location = new Point(8, 512);
            virtualLoadTypeComboBox.Margin = new Padding(8, 0, 8, 6);
            virtualLoadTypeComboBox.Name = "virtualLoadTypeComboBox";
            virtualLoadTypeComboBox.Size = new Size(448, 36);
            virtualLoadTypeComboBox.TabIndex = 11;
            // 
            // virtualLoadStateComboBox
            // 
            virtualLoadStateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualLoadStateComboBox.FormattingEnabled = true;
            virtualLoadStateComboBox.Items.AddRange(new object[] { "关闭带载", "打开带载" });
            virtualLoadStateComboBox.Location = new Point(8, 554);
            virtualLoadStateComboBox.Margin = new Padding(8, 0, 8, 6);
            virtualLoadStateComboBox.Name = "virtualLoadStateComboBox";
            virtualLoadStateComboBox.Size = new Size(448, 36);
            virtualLoadStateComboBox.TabIndex = 12;
            // 
            // btnSetVirtualLoad
            // 
            btnSetVirtualLoad.Location = new Point(8, 596);
            btnSetVirtualLoad.Margin = new Padding(8, 0, 8, 14);
            btnSetVirtualLoad.Name = "btnSetVirtualLoad";
            btnSetVirtualLoad.Size = new Size(448, 38);
            btnSetVirtualLoad.TabIndex = 13;
            btnSetVirtualLoad.Text = "设置带载";
            btnSetVirtualLoad.UseVisualStyleBackColor = true;
            btnSetVirtualLoad.Click += btnSetVirtualLoad_Click;
            // 
            // lblVirtualRipple
            // 
            lblVirtualRipple.Location = new Point(8, 648);
            lblVirtualRipple.Margin = new Padding(8, 0, 8, 4);
            lblVirtualRipple.Name = "lblVirtualRipple";
            lblVirtualRipple.Size = new Size(448, 30);
            lblVirtualRipple.TabIndex = 14;
            lblVirtualRipple.Text = "纹波连接";
            lblVirtualRipple.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualRippleStateComboBox
            // 
            virtualRippleStateComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualRippleStateComboBox.FormattingEnabled = true;
            virtualRippleStateComboBox.Items.AddRange(new object[] { "纹波断开", "纹波连接" });
            virtualRippleStateComboBox.Location = new Point(8, 682);
            virtualRippleStateComboBox.Margin = new Padding(8, 0, 8, 6);
            virtualRippleStateComboBox.Name = "virtualRippleStateComboBox";
            virtualRippleStateComboBox.Size = new Size(448, 36);
            virtualRippleStateComboBox.TabIndex = 15;
            // 
            // btnSetVirtualRipple
            // 
            btnSetVirtualRipple.Location = new Point(8, 724);
            btnSetVirtualRipple.Margin = new Padding(8, 0, 8, 14);
            btnSetVirtualRipple.Name = "btnSetVirtualRipple";
            btnSetVirtualRipple.Size = new Size(448, 38);
            btnSetVirtualRipple.TabIndex = 16;
            btnSetVirtualRipple.Text = "设置纹波连接";
            btnSetVirtualRipple.UseVisualStyleBackColor = true;
            btnSetVirtualRipple.Click += btnSetVirtualRipple_Click;
            // 
            // lblVirtualVoltageMode
            // 
            lblVirtualVoltageMode.Location = new Point(8, 776);
            lblVirtualVoltageMode.Margin = new Padding(8, 0, 8, 4);
            lblVirtualVoltageMode.Name = "lblVirtualVoltageMode";
            lblVirtualVoltageMode.Size = new Size(448, 30);
            lblVirtualVoltageMode.TabIndex = 17;
            lblVirtualVoltageMode.Text = "接口电压读取";
            lblVirtualVoltageMode.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualVoltageModeComboBox
            // 
            virtualVoltageModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualVoltageModeComboBox.FormattingEnabled = true;
            virtualVoltageModeComboBox.Items.AddRange(new object[] { "单次读取", "连续读取", "停止连续" });
            virtualVoltageModeComboBox.Location = new Point(8, 810);
            virtualVoltageModeComboBox.Margin = new Padding(8, 0, 8, 6);
            virtualVoltageModeComboBox.Name = "virtualVoltageModeComboBox";
            virtualVoltageModeComboBox.Size = new Size(448, 36);
            virtualVoltageModeComboBox.TabIndex = 18;
            // 
            // btnReadVirtualVoltage
            // 
            btnReadVirtualVoltage.Location = new Point(8, 852);
            btnReadVirtualVoltage.Margin = new Padding(8, 0, 8, 14);
            btnReadVirtualVoltage.Name = "btnReadVirtualVoltage";
            btnReadVirtualVoltage.Size = new Size(448, 38);
            btnReadVirtualVoltage.TabIndex = 19;
            btnReadVirtualVoltage.Text = "读取接口电压";
            btnReadVirtualVoltage.UseVisualStyleBackColor = true;
            btnReadVirtualVoltage.Click += btnReadVirtualVoltage_Click;
            // 
            // lblVirtualStatusPin
            // 
            lblVirtualStatusPin.Location = new Point(8, 904);
            lblVirtualStatusPin.Margin = new Padding(8, 0, 8, 4);
            lblVirtualStatusPin.Name = "lblVirtualStatusPin";
            lblVirtualStatusPin.Size = new Size(448, 30);
            lblVirtualStatusPin.TabIndex = 20;
            lblVirtualStatusPin.Text = "状态管脚";
            lblVirtualStatusPin.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualStatusPinComboBox
            // 
            virtualStatusPinComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualStatusPinComboBox.FormattingEnabled = true;
            virtualStatusPinComboBox.Items.AddRange(new object[] { "无模块", "有模块" });
            virtualStatusPinComboBox.Location = new Point(8, 938);
            virtualStatusPinComboBox.Margin = new Padding(8, 0, 8, 6);
            virtualStatusPinComboBox.Name = "virtualStatusPinComboBox";
            virtualStatusPinComboBox.Size = new Size(448, 36);
            virtualStatusPinComboBox.TabIndex = 21;
            // 
            // btnSetVirtualStatusPin
            // 
            btnSetVirtualStatusPin.Location = new Point(8, 980);
            btnSetVirtualStatusPin.Margin = new Padding(8, 0, 8, 14);
            btnSetVirtualStatusPin.Name = "btnSetVirtualStatusPin";
            btnSetVirtualStatusPin.Size = new Size(448, 38);
            btnSetVirtualStatusPin.TabIndex = 22;
            btnSetVirtualStatusPin.Text = "设置状态管脚";
            btnSetVirtualStatusPin.UseVisualStyleBackColor = true;
            btnSetVirtualStatusPin.Click += btnSetVirtualStatusPin_Click;
            // 
            // lblVirtualPinRead
            // 
            lblVirtualPinRead.Location = new Point(8, 1032);
            lblVirtualPinRead.Margin = new Padding(8, 0, 8, 4);
            lblVirtualPinRead.Name = "lblVirtualPinRead";
            lblVirtualPinRead.Size = new Size(448, 30);
            lblVirtualPinRead.TabIndex = 23;
            lblVirtualPinRead.Text = "读取引脚电平和发生时间";
            lblVirtualPinRead.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualPinTypeComboBox
            // 
            virtualPinTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualPinTypeComboBox.FormattingEnabled = true;
            virtualPinTypeComboBox.Items.AddRange(new object[] { "ON/OFF", "RST" });
            virtualPinTypeComboBox.Location = new Point(8, 1066);
            virtualPinTypeComboBox.Margin = new Padding(8, 0, 8, 6);
            virtualPinTypeComboBox.Name = "virtualPinTypeComboBox";
            virtualPinTypeComboBox.Size = new Size(448, 36);
            virtualPinTypeComboBox.TabIndex = 24;
            virtualPinTypeComboBox.SelectedIndexChanged += virtualPinTypeComboBox_SelectedIndexChanged;
            // 
            // virtualPinSequenceComboBox
            // 
            virtualPinSequenceComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            virtualPinSequenceComboBox.FormattingEnabled = true;
            virtualPinSequenceComboBox.Location = new Point(8, 1108);
            virtualPinSequenceComboBox.Margin = new Padding(8, 0, 8, 6);
            virtualPinSequenceComboBox.Name = "virtualPinSequenceComboBox";
            virtualPinSequenceComboBox.Size = new Size(448, 36);
            virtualPinSequenceComboBox.TabIndex = 25;
            // 
            // btnReadVirtualPinTime
            // 
            btnReadVirtualPinTime.Location = new Point(8, 1150);
            btnReadVirtualPinTime.Margin = new Padding(8, 0, 8, 14);
            btnReadVirtualPinTime.Name = "btnReadVirtualPinTime";
            btnReadVirtualPinTime.Size = new Size(448, 38);
            btnReadVirtualPinTime.TabIndex = 26;
            btnReadVirtualPinTime.Text = "读取引脚时间";
            btnReadVirtualPinTime.UseVisualStyleBackColor = true;
            btnReadVirtualPinTime.Click += btnReadVirtualPinTime_Click;
            // 
            // lblVirtualCacheClear
            // 
            lblVirtualCacheClear.Location = new Point(8, 1202);
            lblVirtualCacheClear.Margin = new Padding(8, 0, 8, 4);
            lblVirtualCacheClear.Name = "lblVirtualCacheClear";
            lblVirtualCacheClear.Size = new Size(448, 30);
            lblVirtualCacheClear.TabIndex = 27;
            lblVirtualCacheClear.Text = "清空引脚缓存";
            lblVirtualCacheClear.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // virtualClearOnOffCheckBox
            // 
            virtualClearOnOffCheckBox.Location = new Point(8, 1236);
            virtualClearOnOffCheckBox.Margin = new Padding(8, 0, 8, 4);
            virtualClearOnOffCheckBox.Name = "virtualClearOnOffCheckBox";
            virtualClearOnOffCheckBox.Size = new Size(448, 30);
            virtualClearOnOffCheckBox.TabIndex = 28;
            virtualClearOnOffCheckBox.Text = "ON/OFF";
            virtualClearOnOffCheckBox.UseVisualStyleBackColor = true;
            // 
            // virtualClearRstCheckBox
            // 
            virtualClearRstCheckBox.Location = new Point(8, 1270);
            virtualClearRstCheckBox.Margin = new Padding(8, 0, 8, 6);
            virtualClearRstCheckBox.Name = "virtualClearRstCheckBox";
            virtualClearRstCheckBox.Size = new Size(448, 30);
            virtualClearRstCheckBox.TabIndex = 29;
            virtualClearRstCheckBox.Text = "RST";
            virtualClearRstCheckBox.UseVisualStyleBackColor = true;
            // 
            // btnClearVirtualPinCache
            // 
            btnClearVirtualPinCache.Location = new Point(8, 1306);
            btnClearVirtualPinCache.Margin = new Padding(8, 0, 8, 14);
            btnClearVirtualPinCache.Name = "btnClearVirtualPinCache";
            btnClearVirtualPinCache.Size = new Size(448, 38);
            btnClearVirtualPinCache.TabIndex = 30;
            btnClearVirtualPinCache.Text = "清空缓存";
            btnClearVirtualPinCache.UseVisualStyleBackColor = true;
            btnClearVirtualPinCache.Click += btnClearVirtualPinCache_Click;
            // 
            // btnSetVirtualActiveReport
            // 
            btnSetVirtualActiveReport.Location = new Point(8, 1358);
            btnSetVirtualActiveReport.Margin = new Padding(8, 0, 8, 0);
            btnSetVirtualActiveReport.Name = "btnSetVirtualActiveReport";
            btnSetVirtualActiveReport.Size = new Size(448, 38);
            btnSetVirtualActiveReport.TabIndex = 31;
            btnSetVirtualActiveReport.Text = "设置主动上报运行模式";
            btnSetVirtualActiveReport.UseVisualStyleBackColor = true;
            btnSetVirtualActiveReport.Click += btnSetVirtualActiveReport_Click;
            // 
            // dcMeasureLayout
            // 
            dcMeasureLayout.ColumnCount = 1;
            dcMeasureLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            dcMeasureLayout.Controls.Add(measureControlLayout, 0, 0);
            dcMeasureLayout.Controls.Add(measureResultTextBox, 0, 1);
            dcMeasureLayout.Dock = DockStyle.Fill;
            dcMeasureLayout.Location = new Point(12, 39);
            dcMeasureLayout.Name = "dcMeasureLayout";
            dcMeasureLayout.RowCount = 2;
            dcMeasureLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 122F));
            dcMeasureLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            dcMeasureLayout.Size = new Size(956, 193);
            dcMeasureLayout.TabIndex = 0;
            // 
            // measureControlLayout
            // 
            measureControlLayout.Controls.Add(measureTopRowPanel);
            measureControlLayout.Controls.Add(measureBottomRowPanel);
            measureControlLayout.Dock = DockStyle.Fill;
            measureControlLayout.FlowDirection = FlowDirection.LeftToRight;
            measureControlLayout.Location = new Point(0, 0);
            measureControlLayout.Margin = new Padding(0);
            measureControlLayout.Name = "measureControlLayout";
            measureControlLayout.Size = new Size(956, 122);
            measureControlLayout.TabIndex = 0;
            measureControlLayout.WrapContents = true;
            // 
            // measureTopRowPanel
            // 
            measureTopRowPanel.Controls.Add(lblMeasureModuleType);
            measureTopRowPanel.Controls.Add(measureModuleTypeComboBox);
            measureTopRowPanel.Controls.Add(lblMeasureReadItem);
            measureTopRowPanel.Controls.Add(measureReadItemComboBox);
            measureTopRowPanel.Controls.Add(lblMeasureMode);
            measureTopRowPanel.Controls.Add(measureModeComboBox);
            measureTopRowPanel.Location = new Point(0, 0);
            measureTopRowPanel.Margin = new Padding(0);
            measureTopRowPanel.Name = "measureTopRowPanel";
            measureTopRowPanel.Size = new Size(956, 48);
            measureTopRowPanel.TabIndex = 0;
            measureTopRowPanel.WrapContents = false;
            // 
            // measureBottomRowPanel
            // 
            measureBottomRowPanel.Controls.Add(lblMeasureRate);
            measureBottomRowPanel.Controls.Add(measureRateTextBox);
            measureBottomRowPanel.Controls.Add(btnMeasureSend);
            measureBottomRowPanel.Location = new Point(0, 60);
            measureBottomRowPanel.Margin = new Padding(0);
            measureBottomRowPanel.Name = "measureBottomRowPanel";
            measureBottomRowPanel.Size = new Size(956, 48);
            measureBottomRowPanel.TabIndex = 1;
            // 
            // lblMeasureModuleType
            // 
            lblMeasureModuleType.Location = new Point(12, 7);
            lblMeasureModuleType.Margin = new Padding(12, 7, 0, 0);
            lblMeasureModuleType.Name = "lblMeasureModuleType";
            lblMeasureModuleType.Size = new Size(128, 34);
            lblMeasureModuleType.TabIndex = 0;
            lblMeasureModuleType.Text = "模组类型：";
            lblMeasureModuleType.TextAlign = ContentAlignment.MiddleRight;
            // 
            // measureModuleTypeComboBox
            // 
            measureModuleTypeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            measureModuleTypeComboBox.FormattingEnabled = true;
            measureModuleTypeComboBox.Items.AddRange(new object[] { "真实模组", "sta模组" });
            measureModuleTypeComboBox.Location = new Point(148, 6);
            measureModuleTypeComboBox.Margin = new Padding(8, 6, 8, 0);
            measureModuleTypeComboBox.Name = "measureModuleTypeComboBox";
            measureModuleTypeComboBox.Size = new Size(118, 36);
            measureModuleTypeComboBox.TabIndex = 1;
            // 
            // lblMeasureReadItem
            // 
            lblMeasureReadItem.Location = new Point(274, 7);
            lblMeasureReadItem.Margin = new Padding(8, 7, 0, 0);
            lblMeasureReadItem.Name = "lblMeasureReadItem";
            lblMeasureReadItem.Size = new Size(108, 34);
            lblMeasureReadItem.TabIndex = 2;
            lblMeasureReadItem.Text = "读取项：";
            lblMeasureReadItem.TextAlign = ContentAlignment.MiddleRight;
            // 
            // measureReadItemComboBox
            // 
            measureReadItemComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            measureReadItemComboBox.FormattingEnabled = true;
            measureReadItemComboBox.Items.AddRange(new object[] { "读取电压", "读取电流", "读取电压电流", "读取功耗" });
            measureReadItemComboBox.Location = new Point(390, 6);
            measureReadItemComboBox.Margin = new Padding(8, 6, 8, 0);
            measureReadItemComboBox.Name = "measureReadItemComboBox";
            measureReadItemComboBox.Size = new Size(142, 36);
            measureReadItemComboBox.TabIndex = 3;
            // 
            // lblMeasureMode
            // 
            lblMeasureMode.Location = new Point(548, 7);
            lblMeasureMode.Margin = new Padding(8, 7, 0, 0);
            lblMeasureMode.Name = "lblMeasureMode";
            lblMeasureMode.Size = new Size(128, 34);
            lblMeasureMode.TabIndex = 4;
            lblMeasureMode.Text = "读取方式：";
            lblMeasureMode.TextAlign = ContentAlignment.MiddleRight;
            // 
            // measureModeComboBox
            // 
            measureModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            measureModeComboBox.FormattingEnabled = true;
            measureModeComboBox.Items.AddRange(new object[] { "单次读取", "连续读取", "停止连续" });
            measureModeComboBox.Location = new Point(684, 6);
            measureModeComboBox.Margin = new Padding(8, 6, 0, 0);
            measureModeComboBox.Name = "measureModeComboBox";
            measureModeComboBox.Size = new Size(118, 36);
            measureModeComboBox.TabIndex = 5;
            // 
            // lblMeasureRate
            // 
            lblMeasureRate.Location = new Point(12, 7);
            lblMeasureRate.Margin = new Padding(12, 7, 0, 0);
            lblMeasureRate.Name = "lblMeasureRate";
            lblMeasureRate.Size = new Size(112, 34);
            lblMeasureRate.TabIndex = 6;
            lblMeasureRate.Text = "频率：";
            lblMeasureRate.TextAlign = ContentAlignment.MiddleRight;
            // 
            // measureRateTextBox
            // 
            measureRateTextBox.Location = new Point(132, 7);
            measureRateTextBox.Margin = new Padding(8, 7, 24, 0);
            measureRateTextBox.Name = "measureRateTextBox";
            measureRateTextBox.Size = new Size(70, 34);
            measureRateTextBox.TabIndex = 7;
            measureRateTextBox.Text = "10";
            // 
            // btnMeasureSend
            // 
            btnMeasureSend.Location = new Point(226, 3);
            btnMeasureSend.Margin = new Padding(0, 3, 0, 0);
            btnMeasureSend.Name = "btnMeasureSend";
            btnMeasureSend.Size = new Size(120, 42);
            btnMeasureSend.TabIndex = 8;
            btnMeasureSend.Text = "读取";
            btnMeasureSend.UseVisualStyleBackColor = true;
            btnMeasureSend.Click += btnMeasureSend_Click;
            // 
            // measureResultTextBox
            // 
            measureResultTextBox.Dock = DockStyle.Fill;
            measureResultTextBox.Location = new Point(0, 122);
            measureResultTextBox.Margin = new Padding(0);
            measureResultTextBox.Multiline = true;
            measureResultTextBox.Name = "measureResultTextBox";
            measureResultTextBox.ReadOnly = true;
            measureResultTextBox.ScrollBars = ScrollBars.Vertical;
            measureResultTextBox.Size = new Size(956, 71);
            measureResultTextBox.TabIndex = 1;
            // 
            // connectionLayout
            // 
            connectionLayout.ColumnCount = 6;
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90F));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            connectionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            connectionLayout.Controls.Add(lblIp, 0, 0);
            connectionLayout.Controls.Add(ipTextBox, 1, 0);
            connectionLayout.Controls.Add(lblPort, 2, 0);
            connectionLayout.Controls.Add(portTextBox, 3, 0);
            connectionLayout.Controls.Add(connectButton, 4, 0);
            connectionLayout.Controls.Add(lblStatus, 5, 0);
            connectionLayout.Dock = DockStyle.Fill;
            connectionLayout.Location = new Point(12, 39);
            connectionLayout.Name = "connectionLayout";
            connectionLayout.RowCount = 1;
            connectionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            connectionLayout.Size = new Size(1076, 69);
            connectionLayout.TabIndex = 0;
            // 
            // lblIp
            // 
            lblIp.Dock = DockStyle.Fill;
            lblIp.Location = new Point(3, 0);
            lblIp.Name = "lblIp";
            lblIp.Size = new Size(84, 69);
            lblIp.TabIndex = 0;
            lblIp.Text = "IP：";
            lblIp.TextAlign = ContentAlignment.MiddleRight;
            // 
            // ipTextBox
            // 
            ipTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            ipTextBox.Location = new Point(93, 17);
            ipTextBox.Name = "ipTextBox";
            ipTextBox.Size = new Size(214, 34);
            ipTextBox.TabIndex = 1;
            ipTextBox.Text = "127.0.0.1";
            // 
            // lblPort
            // 
            lblPort.Dock = DockStyle.Fill;
            lblPort.Location = new Point(313, 0);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(84, 69);
            lblPort.TabIndex = 2;
            lblPort.Text = "端口：";
            lblPort.TextAlign = ContentAlignment.MiddleRight;
            // 
            // portTextBox
            // 
            portTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            portTextBox.Location = new Point(403, 17);
            portTextBox.Name = "portTextBox";
            portTextBox.Size = new Size(134, 34);
            portTextBox.TabIndex = 3;
            portTextBox.Text = "10001";
            // 
            // connectButton
            // 
            connectButton.Anchor = AnchorStyles.Left;
            connectButton.Location = new Point(552, 13);
            connectButton.Margin = new Padding(12, 3, 3, 3);
            connectButton.Name = "connectButton";
            connectButton.Size = new Size(110, 42);
            connectButton.TabIndex = 4;
            connectButton.Text = "连接";
            connectButton.UseVisualStyleBackColor = true;
            connectButton.Click += connectButton_Click;
            // 
            // lblStatus
            // 
            lblStatus.Dock = DockStyle.Fill;
            lblStatus.ForeColor = Color.FromArgb(55, 65, 81);
            lblStatus.Location = new Point(673, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Padding = new Padding(12, 0, 0, 0);
            lblStatus.Size = new Size(400, 69);
            lblStatus.TabIndex = 5;
            lblStatus.Text = "状态：未连接";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // TerminalV2UserControl
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(88, 149, 127);
            Controls.Add(operationPanel);
            Controls.Add(groupTcpClient);
            Font = new Font("Microsoft YaHei UI", 9F);
            Name = "TerminalV2UserControl";
            Size = new Size(2588, 1024);
            groupTcpClient.ResumeLayout(false);
            connectionLayout.ResumeLayout(false);
            connectionLayout.PerformLayout();
            operationPanel.ResumeLayout(false);
            groupOperation.ResumeLayout(false);
            operationLayout.ResumeLayout(false);
            operationLayout.PerformLayout();
            groupRealModulePower.ResumeLayout(false);
            realModulePowerLayout.ResumeLayout(false);
            groupCarrierModulePower.ResumeLayout(false);
            carrierModulePowerLayout.ResumeLayout(false);
            groupCarrierPinLevel.ResumeLayout(false);
            carrierPinLevelLayout.ResumeLayout(false);
            groupVoltageCurrentControl.ResumeLayout(false);
            voltageCurrentLayout.ResumeLayout(false);
            groupDcMeasure.ResumeLayout(false);
            dcMeasureLayout.ResumeLayout(false);
            dcMeasureLayout.PerformLayout();
            measureControlLayout.ResumeLayout(false);
            measureTopRowPanel.ResumeLayout(false);
            measureTopRowPanel.PerformLayout();
            measureBottomRowPanel.ResumeLayout(false);
            measureBottomRowPanel.PerformLayout();
            groupVirtualModuleControl.ResumeLayout(false);
            virtualModuleLayout.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private GroupBox groupTcpClient;
        private TableLayoutPanel connectionLayout;
        private Label lblIp;
        private TextBox ipTextBox;
        private Label lblPort;
        private TextBox portTextBox;
        private Label lblStation;
        private TextBox stationTextBox;
        private Button connectButton;
        private Label lblStatus;
        private Panel operationPanel;
        private GroupBox groupOperation;
        private FlowLayoutPanel operationLayout;
        private Button btnResetMcu;
        private Button btnReadVersion;
        private GroupBox groupRealModulePower;
        private FlowLayoutPanel realModulePowerLayout;
        private Label lblModuleNumber;
        private TextBox moduleNumberTextBox;
        private Button btnDcPowerOn;
        private Button btnDcPowerOff;
        private Button btnAcPowerOn;
        private Button btnAcPowerOff;
        private GroupBox groupCarrierModulePower;
        private FlowLayoutPanel carrierModulePowerLayout;
        private Button btnCarrierDcPowerOn;
        private Button btnCarrierDcPowerOff;
        private Button btnCarrierAcPowerOn;
        private Button btnCarrierAcPowerOff;
        private GroupBox groupCarrierPinLevel;
        private FlowLayoutPanel carrierPinLevelLayout;
        private Button btnSetRstHigh;
        private Button btnSetRstLow;
        private Button btnSetSetHigh;
        private Button btnSetSetLow;
        private Button btnSetEventHigh;
        private Button btnSetEventLow;
        private Button btnReadStaPin;
        private GroupBox groupVoltageCurrentControl;
        private TableLayoutPanel voltageCurrentLayout;
        private CheckBox chkVoltageUa;
        private CheckBox chkVoltageUb;
        private CheckBox chkVoltageUc;
        private Button btnVoltageOn;
        private Button btnVoltageOff;
        private CheckBox chkCurrentIa;
        private CheckBox chkCurrentIb;
        private CheckBox chkCurrentIc;
        private CheckBox chkCurrentIn;
        private Button btnCurrentOn;
        private Button btnCurrentOff;
        private Button btnSwitchCurrentLoop;
        private ComboBox meterVoltageLoopPowerComboBox;
        private Button btnReadMeterVoltageLoopPower;
        private Button btnMeterMotorPress;
        private Button btnMeterMotorRelease;
        private Button btnMagnetMotorPress;
        private Button btnMagnetMotorRelease;
        private GroupBox groupDcMeasure;
        private TableLayoutPanel dcMeasureLayout;
        private FlowLayoutPanel measureControlLayout;
        private FlowLayoutPanel measureTopRowPanel;
        private FlowLayoutPanel measureBottomRowPanel;
        private Label lblMeasureModuleType;
        private ComboBox measureModuleTypeComboBox;
        private Label lblMeasureReadItem;
        private ComboBox measureReadItemComboBox;
        private Label lblMeasureMode;
        private ComboBox measureModeComboBox;
        private Label lblMeasureRate;
        private TextBox measureRateTextBox;
        private Button btnMeasureSend;
        private TextBox measureResultTextBox;
        private GroupBox groupVirtualModuleControl;
        private FlowLayoutPanel virtualModuleLayout;
        private Label lblVirtualModuleMode;
        private ComboBox virtualModuleModeComboBox;
        private Label lblVirtualModuleType;
        private ComboBox virtualModuleTypeComboBox;
        private Button btnSetVirtualModuleType;
        private Label lblVirtualUsbState;
        private ComboBox virtualUsbStateComboBox;
        private Button btnSetVirtualUsbState;
        private Label lblVirtualLoad;
        private ComboBox virtualLoadTypeComboBox;
        private ComboBox virtualLoadStateComboBox;
        private Button btnSetVirtualLoad;
        private Label lblVirtualRipple;
        private ComboBox virtualRippleStateComboBox;
        private Button btnSetVirtualRipple;
        private Label lblVirtualVoltageMode;
        private ComboBox virtualVoltageModeComboBox;
        private Button btnReadVirtualVoltage;
        private Label lblVirtualStatusPin;
        private ComboBox virtualStatusPinComboBox;
        private Button btnSetVirtualStatusPin;
        private Label lblVirtualPinRead;
        private ComboBox virtualPinTypeComboBox;
        private ComboBox virtualPinSequenceComboBox;
        private Button btnReadVirtualPinTime;
        private Label lblVirtualCacheClear;
        private CheckBox virtualClearOnOffCheckBox;
        private CheckBox virtualClearRstCheckBox;
        private Button btnClearVirtualPinCache;
        private Button btnSetVirtualActiveReport;
        private Button btnSwitchVirtualModule;
        private Button btnSwitchRealModule;
        private GroupBox groupErrorInstrument;
        private UltrSimpleDisplay ultrSimpleDisplayV2;
        private FlowLayoutPanel remoteControlLayout;
        private readonly List<CheckBox> remoteSignalStateCheckBoxes = [];
        private readonly List<CheckBox> remotePulseChannelCheckBoxes = [];
        private readonly List<CheckBox> remoteTestChannelCheckBoxes = [];
        private ComboBox pulseStartComboBox;
        private TextBox pulseFrequencyTextBox;
        private TextBox pulseCountTextBox;
        private TextBox pulseDutyTextBox;
        private ComboBox portSwitchTypeComboBox;
        private ComboBox portSwitchTargetComboBox;
        private ComboBox usbStateComboBox;
        private TextBox remoteInterferenceDurationTextBox;
        private TextBox remoteDebounceDurationTextBox;
        private TextBox remoteDebounceIntervalTextBox;
        private TextBox remoteAvalancheCountTextBox;
        private TextBox remoteAvalancheIntervalTextBox;
        private CheckBox can1CheckBox;
        private CheckBox can2CheckBox;
        private ComboBox canBaudRateComboBox;
        private ComboBox meterSignalTypeComboBox;
        private ComboBox meterSignalOperationComboBox;
        private ComboBox meterSignalValueComboBox;
        private ComboBox ledColorComboBox;
        private ComboBox ledTypeComboBox;
        private readonly List<CheckBox> ledChannelCheckBoxes = [];
        private ComboBox ledModeComboBox;
        private TextBox ledBlinkTimeTextBox;
        private ComboBox terminalTypeComboBox;
        private readonly List<CheckBox> auxPowerLoadCheckBoxes = [];
        private ComboBox auxPowerLoadStateComboBox;
        private ComboBox auxPowerVoltageChannelComboBox;
        private ComboBox auxPowerVoltageOperationComboBox;
        private TextBox auxPowerVoltageCalibrationTextBox;
        private ComboBox smaStateComboBox;
        private ComboBox sourcePowerTargetComboBox;
        private ComboBox sourcePowerStateComboBox;
        private ComboBox sourceSwitchComboBox;
        private ComboBox singlePhaseAccessComboBox;
        private ComboBox timeOperationComboBox;
        private ComboBox samplingVoltageTypeComboBox;
        private ComboBox samplingVoltageOperationComboBox;
        private TextBox samplingVoltageCalibrationTextBox;
        private ComboBox panelRemovalOperationComboBox;
        private ComboBox groundFaultComboBox;
        private ComboBox loopSelfCheckComboBox;
        private readonly List<CheckBox> impedanceVoltageCheckBoxes = [];
        private ComboBox impedanceItemComboBox;
        private ComboBox impedanceFunctionComboBox;

        private void InitializeRemoteControlGroup()
        {
            GroupBox groupRemoteControl = new()
            {
                Font = new Font("Microsoft YaHei UI", 9F),
                Location = new Point(24, 540),
                Name = "groupRemoteControl",
                Padding = new Padding(12),
                Size = new Size(1180, 850),
                TabIndex = 7,
                TabStop = false,
                Text = "遥控"
            };

            remoteControlLayout = new FlowLayoutPanel
            {
                AutoScroll = false,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            groupRemoteControl.Controls.Add(remoteControlLayout);
            operationPanel.Controls.Add(groupRemoteControl);

            remoteControlLayout.Controls.Add(CreateLabel("遥信状态量"));
            FlowLayoutPanel remoteStateRow1 = CreateRow();
            FlowLayoutPanel remoteStateRow2 = CreateRow();
            AddCheckBoxes(remoteStateRow1, remoteSignalStateCheckBoxes, "YX", 1, 8, checkedByDefault: false);
            AddCheckBoxes(remoteStateRow2, remoteSignalStateCheckBoxes, "YX", 9, 12, checkedByDefault: false);
            AddCheckBoxes(remoteStateRow2, remoteSignalStateCheckBoxes, "门", 1, 4, checkedByDefault: false);
            remoteControlLayout.Controls.Add(remoteStateRow1);
            remoteControlLayout.Controls.Add(remoteStateRow2);
            remoteControlLayout.Controls.Add(CreateButton("设置遥信状态", btnRemoteSignalStateSend_Click));

            remoteControlLayout.Controls.Add(CreateLabel("脉冲参数"));
            remoteControlLayout.Controls.Add(CreateCheckBoxRow(remotePulseChannelCheckBoxes, "脉冲", 4, checkedByDefault: true));
            FlowLayoutPanel pulseRow = CreateRow();
            pulseStartComboBox = CreateComboBox(["启动脉冲", "停止脉冲"], 112);
            pulseFrequencyTextBox = CreateTextBox("1", 76);
            pulseCountTextBox = CreateTextBox("1", 76);
            pulseDutyTextBox = CreateTextBox("50", 64);
            pulseRow.Controls.Add(pulseStartComboBox);
            pulseRow.Controls.Add(CreateInlineLabel("Hz"));
            pulseRow.Controls.Add(pulseFrequencyTextBox);
            pulseRow.Controls.Add(CreateInlineLabel("个数"));
            pulseRow.Controls.Add(pulseCountTextBox);
            pulseRow.Controls.Add(CreateInlineLabel("占空比"));
            pulseRow.Controls.Add(pulseDutyTextBox);
            pulseRow.Controls.Add(CreateButton("设置脉冲", btnPulseParameterSend_Click, 108));
            remoteControlLayout.Controls.Add(pulseRow);

            remoteControlLayout.Controls.Add(CreateLabel("接口切换"));
            FlowLayoutPanel switchRow = CreateRow();
            portSwitchTypeComboBox = CreateComboBox(["485/CAN切换", "485-3/232切换", "485-4/232切换"], 150);
            portSwitchTargetComboBox = CreateComboBox(["切换到485", "切换到232/CAN"], 150);
            usbStateComboBox = CreateComboBox(["打开USB", "关闭USB"], 112);
            switchRow.Controls.Add(portSwitchTypeComboBox);
            switchRow.Controls.Add(portSwitchTargetComboBox);
            switchRow.Controls.Add(CreateButton("切换端口", btnPortSwitchSend_Click, 108));
            switchRow.Controls.Add(usbStateComboBox);
            switchRow.Controls.Add(CreateButton("切换USB", btnUsbStateSend_Click, 108));
            remoteControlLayout.Controls.Add(switchRow);

            remoteControlLayout.Controls.Add(CreateLabel("遥信测试通道"));
            remoteControlLayout.Controls.Add(CreateCheckBoxRow(remoteTestChannelCheckBoxes, "YX", 8, checkedByDefault: true));
            FlowLayoutPanel testRow = CreateRow();
            remoteInterferenceDurationTextBox = CreateTextBox("100", 72);
            remoteDebounceDurationTextBox = CreateTextBox("100", 72);
            remoteDebounceIntervalTextBox = CreateTextBox("10", 58);
            remoteAvalancheCountTextBox = CreateTextBox("2", 58);
            remoteAvalancheIntervalTextBox = CreateTextBox("1", 58);
            testRow.Controls.Add(CreateInlineLabel("干扰ms"));
            testRow.Controls.Add(remoteInterferenceDurationTextBox);
            testRow.Controls.Add(CreateButton("干扰测试", btnRemoteInterferenceSend_Click, 108));
            testRow.Controls.Add(CreateInlineLabel("防抖ms"));
            testRow.Controls.Add(remoteDebounceDurationTextBox);
            testRow.Controls.Add(CreateInlineLabel("间隔"));
            testRow.Controls.Add(remoteDebounceIntervalTextBox);
            testRow.Controls.Add(CreateButton("防抖测试", btnRemoteDebounceSend_Click, 108));
            remoteControlLayout.Controls.Add(testRow);

            FlowLayoutPanel avalancheRow = CreateRow();
            avalancheRow.Controls.Add(CreateInlineLabel("雪崩次数"));
            avalancheRow.Controls.Add(remoteAvalancheCountTextBox);
            avalancheRow.Controls.Add(CreateInlineLabel("间隔s"));
            avalancheRow.Controls.Add(remoteAvalancheIntervalTextBox);
            avalancheRow.Controls.Add(CreateButton("雪崩测试", btnRemoteAvalancheSend_Click, 108));
            can1CheckBox = CreateCheckBox("CAN1", true);
            can2CheckBox = CreateCheckBox("CAN2", false);
            canBaudRateComboBox = CreateComboBox(["10K", "25K", "50K", "100K", "125K", "250K", "500K", "1000K"], 90);
            canBaudRateComboBox.SelectedItem = "125K";
            avalancheRow.Controls.Add(can1CheckBox);
            avalancheRow.Controls.Add(can2CheckBox);
            avalancheRow.Controls.Add(canBaudRateComboBox);
            avalancheRow.Controls.Add(CreateButton("CAN波特率", btnCanBaudRateSend_Click, 120));
            avalancheRow.Controls.Add(CreateButton("读温湿度", btnTemperatureHumidityRead_Click, 108));
            remoteControlLayout.Controls.Add(avalancheRow);

            remoteControlLayout.Controls.Add(CreateLabel("遥控/告警读取"));
            FlowLayoutPanel remoteReadRow = CreateRow();
            remoteReadRow.Controls.Add(CreateButton("读遥控状态", btnRemoteControlStatusRead_Click, 120));
            remoteReadRow.Controls.Add(CreateButton("读遥控脉冲时间", btnRemoteControlPulseTimeRead_Click, 150));
            remoteReadRow.Controls.Add(CreateButton("读告警状态", btnAlarmStatusRead_Click, 120));
            remoteControlLayout.Controls.Add(remoteReadRow);

            remoteControlLayout.Controls.Add(CreateLabel("电能表控制与反馈信号"));
            FlowLayoutPanel meterSignalRow = CreateRow();
            meterSignalTypeComboBox = CreateComboBox(["控制信号", "反馈信号"], 112);
            meterSignalOperationComboBox = CreateComboBox(["设置状态", "读取状态"], 112);
            meterSignalValueComboBox = CreateComboBox(["开", "关"], 80);
            meterSignalRow.Controls.Add(meterSignalTypeComboBox);
            meterSignalRow.Controls.Add(meterSignalOperationComboBox);
            meterSignalRow.Controls.Add(meterSignalValueComboBox);
            meterSignalRow.Controls.Add(CreateButton("发送信号", btnMeterControlFeedbackSignalSend_Click, 108));
            remoteControlLayout.Controls.Add(meterSignalRow);
        }

        private void InitializeControlGroup()
        {
            GroupBox groupControl = new()
            {
                Font = new Font("Microsoft YaHei UI", 9F),
                Location = new Point(1228, 540),
                Name = "groupControl",
                Padding = new Padding(12),
                Size = new Size(820, 1160),
                TabIndex = 8,
                TabStop = false,
                Text = "控制"
            };

            FlowLayoutPanel controlLayout = new()
            {
                AutoScroll = false,
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            groupControl.Controls.Add(controlLayout);
            operationPanel.Controls.Add(groupControl);

            controlLayout.Controls.Add(CreateControlLabel("指示灯控制"));
            FlowLayoutPanel ledChannelRow = CreateControlRow();
            AddCheckBoxes(ledChannelRow, ledChannelCheckBoxes, "LED", 1, 4, checkedByDefault: true);
            controlLayout.Controls.Add(ledChannelRow);
            FlowLayoutPanel ledRow = CreateControlRow();
            ledColorComboBox = CreateComboBox(["红色", "绿色", "黄色"], 90);
            ledTypeComboBox = CreateComboBox(["表位LED", "台体LED"], 100);
            ledModeComboBox = CreateComboBox(["灭", "常亮", "闪烁"], 80);
            ledBlinkTimeTextBox = CreateTextBox("500", 72);
            ledRow.Controls.Add(ledColorComboBox);
            ledRow.Controls.Add(ledTypeComboBox);
            ledRow.Controls.Add(ledModeComboBox);
            ledRow.Controls.Add(CreateInlineLabel("ms"));
            ledRow.Controls.Add(ledBlinkTimeTextBox);
            ledRow.Controls.Add(CreateButton("设置指示灯", btnIndicatorLightControl_Click, 120));
            controlLayout.Controls.Add(ledRow);

            controlLayout.Controls.Add(CreateControlLabel("终端/电源"));
            FlowLayoutPanel terminalRow = CreateControlRow();
            terminalTypeComboBox = CreateComboBox(["断开", "台区智能融合终端", "13版集中器I型", "13版专变III型", "22版集中器I型", "22版专变III型", "能源控制器", "南网-负荷管理终端", "南网-配变监测计量终端", "南网-13集中器", "智能融合终端IFT"], 260);
            terminalRow.Controls.Add(terminalTypeComboBox);
            terminalRow.Controls.Add(CreateButton("设置终端类型", btnTerminalTypeSet_Click, 130));
            controlLayout.Controls.Add(terminalRow);

            FlowLayoutPanel auxLoadRow = CreateControlRow();
            auxPowerLoadStateComboBox = CreateComboBox(["带载", "不带载"], 90);
            AddCheckBoxes(auxLoadRow, auxPowerLoadCheckBoxes, "12V", 1, 2, checkedByDefault: true);
            auxLoadRow.Controls.Add(auxPowerLoadStateComboBox);
            auxLoadRow.Controls.Add(CreateButton("设置12V带载", btnAuxPowerLoadSet_Click, 130));
            controlLayout.Controls.Add(auxLoadRow);

            FlowLayoutPanel auxVoltageRow = CreateControlRow();
            auxPowerVoltageChannelComboBox = CreateComboBox(["第一路12V", "第二路12V", "第三路12V"], 140);
            auxPowerVoltageOperationComboBox = CreateComboBox(["读取", "校准"], 80);
            auxPowerVoltageCalibrationTextBox = CreateTextBox("12000", 84);
            auxVoltageRow.Controls.Add(auxPowerVoltageChannelComboBox);
            auxVoltageRow.Controls.Add(auxPowerVoltageOperationComboBox);
            auxVoltageRow.Controls.Add(CreateInlineLabel("mV"));
            auxVoltageRow.Controls.Add(auxPowerVoltageCalibrationTextBox);
            auxVoltageRow.Controls.Add(CreateButton("辅助电源电压", btnAuxPowerVoltageSend_Click, 130));
            controlLayout.Controls.Add(auxVoltageRow);

            controlLayout.Controls.Add(CreateControlLabel("接口/源切换"));
            FlowLayoutPanel interfaceRow = CreateControlRow();
            smaStateComboBox = CreateComboBox(["断开SMA", "示波器SMA", "频谱仪SMA"], 140);
            interfaceRow.Controls.Add(smaStateComboBox);
            interfaceRow.Controls.Add(CreateButton("设置SMA", btnSmaControl_Click, 120));
            controlLayout.Controls.Add(interfaceRow);

            FlowLayoutPanel sourcePowerRow = CreateControlRow();
            sourcePowerTargetComboBox = CreateComboBox(["源表", "功放", "源表后功放", "读取状态"], 140);
            sourcePowerStateComboBox = CreateComboBox(["上电", "下电"], 80);
            sourcePowerRow.Controls.Add(sourcePowerTargetComboBox);
            sourcePowerRow.Controls.Add(sourcePowerStateComboBox);
            sourcePowerRow.Controls.Add(CreateButton("源表功放", btnSourcePowerSend_Click, 120));
            controlLayout.Controls.Add(sourcePowerRow);

            FlowLayoutPanel sourceSwitchRow = CreateControlRow();
            sourceSwitchComboBox = CreateComboBox(["标准源", "电工源"], 90);
            singlePhaseAccessComboBox = CreateComboBox(["不接入", "接入A相", "接入B相", "接入C相"], 100);
            sourceSwitchRow.Controls.Add(sourceSwitchComboBox);
            sourceSwitchRow.Controls.Add(CreateButton("源切换", btnSourceSwitchSend_Click, 120));
            sourceSwitchRow.Controls.Add(singlePhaseAccessComboBox);
            sourceSwitchRow.Controls.Add(CreateButton("单相接入", btnSinglePhaseAccessSend_Click, 120));
            controlLayout.Controls.Add(sourceSwitchRow);

            controlLayout.Controls.Add(CreateControlLabel("时间/采样/面板"));
            FlowLayoutPanel timeRow = CreateControlRow();
            timeOperationComboBox = CreateComboBox(["读取当前时间", "设置当前时间"], 140);
            timeRow.Controls.Add(timeOperationComboBox);
            timeRow.Controls.Add(CreateButton("时间", btnCurrentTimeSend_Click, 100));
            controlLayout.Controls.Add(timeRow);

            FlowLayoutPanel sampleRow = CreateControlRow();
            samplingVoltageTypeComboBox = CreateComboBox(["24V电压", "12V电压", "电池电压"], 100);
            samplingVoltageOperationComboBox = CreateComboBox(["读取", "校准"], 80);
            samplingVoltageCalibrationTextBox = CreateTextBox("12000", 84);
            sampleRow.Controls.Add(samplingVoltageTypeComboBox);
            sampleRow.Controls.Add(samplingVoltageOperationComboBox);
            sampleRow.Controls.Add(CreateInlineLabel("mV"));
            sampleRow.Controls.Add(samplingVoltageCalibrationTextBox);
            sampleRow.Controls.Add(CreateButton("采样电压", btnSamplingVoltageSend_Click, 120));
            controlLayout.Controls.Add(sampleRow);

            FlowLayoutPanel panelRow = CreateControlRow();
            panelRemovalOperationComboBox = CreateComboBox(["读取", "清除"], 80);
            panelRow.Controls.Add(panelRemovalOperationComboBox);
            panelRow.Controls.Add(CreateButton("面板拆卸", btnPanelRemovalSend_Click, 120));
            controlLayout.Controls.Add(panelRow);

            controlLayout.Controls.Add(CreateControlLabel("故障/复位/自检"));
            FlowLayoutPanel faultRow = CreateControlRow();
            groundFaultComboBox = CreateComboBox(["正常", "A相接地", "B相接地", "C相接地"], 100);
            loopSelfCheckComboBox = CreateComboBox(["3相3开始自检", "3相4开始自检", "读取自检结果", "复位自检结果"], 170);
            faultRow.Controls.Add(groundFaultComboBox);
            faultRow.Controls.Add(CreateButton("接地故障", btnGroundFaultSend_Click, 120));
            faultRow.Controls.Add(CreateButton("PT复位", btnPtResetSend_Click, 110));
            faultRow.Controls.Add(loopSelfCheckComboBox);
            faultRow.Controls.Add(CreateButton("回路自检", btnLoopSelfCheckSend_Click, 120));
            controlLayout.Controls.Add(faultRow);

            controlLayout.Controls.Add(CreateControlLabel("阻抗箱控制"));
            FlowLayoutPanel impedanceRow = CreateControlRow();
            AddCheckBoxes(impedanceRow, impedanceVoltageCheckBoxes, "U", 1, 3, checkedByDefault: true);
            impedanceItemComboBox = CreateComboBox(["电流回路切换", "TA一次侧控制", "TA二次侧控制", "TA二次侧阻抗"], 170);
            impedanceFunctionComboBox = CreateComboBox([
                "R1 100R",
                "R2 150R",
                "R3 250R",
                "R4 2K",
                "R5 3K",
                "R6 3.6K",
                "R7 3.9K",
                "R8 20K",
                "R9 25K",
                "R10 40K",
                "R11 50K",
                "R12 60K",
                "R13 70K"
            ], 130);
            impedanceRow.Controls.Add(impedanceItemComboBox);
            impedanceRow.Controls.Add(impedanceFunctionComboBox);
            impedanceRow.Controls.Add(CreateButton("设置阻抗箱", btnImpedanceBoxSend_Click, 140));
            controlLayout.Controls.Add(impedanceRow);
        }

        private void InitializeErrorInstrumentGroup()
        {
            groupErrorInstrument = new GroupBox
            {
                Font = new Font("Microsoft YaHei UI", 9F),
                Location = new Point(24, 1410),
                Name = "groupErrorInstrument",
                Padding = new Padding(12),
                Size = new Size(1180, 490),
                TabIndex = 10,
                TabStop = false,
                Text = "误差实验"
            };

            ultrSimpleDisplayV2 = new UltrSimpleDisplay
            {
                Dock = DockStyle.Fill,
                MinimumSize = new Size(1000, 400),
                Name = "ultrSimpleDisplayV2",
                ProtocolVersion = ErrorInstrumentProtocolVersion.V2,
                TabIndex = 0
            };

            groupErrorInstrument.Controls.Add(ultrSimpleDisplayV2);
            operationPanel.Controls.Add(groupErrorInstrument);
        }

        private static Label CreateLabel(string text)
        {
            return new Label
            {
                AutoSize = false,
                Margin = new Padding(8, 8, 8, 2),
                Size = new Size(1120, 28),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateControlLabel(string text)
        {
            return new Label
            {
                AutoSize = false,
                Margin = new Padding(8, 8, 8, 2),
                Size = new Size(764, 28),
                Text = text,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateInlineLabel(string text)
        {
            return new Label
            {
                AutoSize = false,
                Margin = new Padding(8, 7, 0, 0),
                Size = new Size(Math.Max(44, text.Length * 18), 30),
                Text = text,
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        private static FlowLayoutPanel CreateRow()
        {
            return new FlowLayoutPanel
            {
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 4),
                Size = new Size(1128, 44),
                WrapContents = false
            };
        }

        private static FlowLayoutPanel CreateControlRow()
        {
            return new FlowLayoutPanel
            {
                AutoSize = false,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 4),
                Size = new Size(768, 44),
                WrapContents = false
            };
        }

        private static FlowLayoutPanel CreateCheckBoxRow(List<CheckBox> target, string prefix, int count, bool checkedByDefault)
        {
            FlowLayoutPanel row = CreateRow();
            target.Clear();
            for (int i = 1; i <= count; i++)
            {
                CheckBox checkBox = CreateCheckBox($"{prefix}{i}", checkedByDefault);
                target.Add(checkBox);
                row.Controls.Add(checkBox);
            }

            return row;
        }

        private static void AddCheckBoxes(FlowLayoutPanel row, List<CheckBox> target, string prefix, int start, int end, bool checkedByDefault)
        {
            for (int i = start; i <= end; i++)
            {
                CheckBox checkBox = CreateCheckBox($"{prefix}{i}", checkedByDefault);
                target.Add(checkBox);
                row.Controls.Add(checkBox);
            }
        }

        private static CheckBox CreateCheckBox(string text, bool isChecked)
        {
            return new CheckBox
            {
                Checked = isChecked,
                Margin = new Padding(8, 6, 4, 0),
                Size = new Size(Math.Max(76, text.Length * 16 + 36), 30),
                Text = text,
                UseVisualStyleBackColor = true
            };
        }

        private static ComboBox CreateComboBox(object[] items, int width)
        {
            ComboBox comboBox = new()
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Margin = new Padding(8, 5, 4, 0),
                Size = new Size(width, 36)
            };
            comboBox.Items.AddRange(items);
            comboBox.SelectedIndex = 0;
            return comboBox;
        }

        private static TextBox CreateTextBox(string text, int width)
        {
            return new TextBox
            {
                Margin = new Padding(6, 6, 4, 0),
                Size = new Size(width, 34),
                Text = text
            };
        }

        private static Button CreateButton(string text, EventHandler handler, int width = 120)
        {
            int preferredWidth = Math.Max(width, text.Length * 20 + 36);
            Button button = new()
            {
                Margin = new Padding(8, 3, 4, 0),
                Size = new Size(preferredWidth, 38),
                Text = text,
                UseVisualStyleBackColor = true
            };
            button.Click += handler;
            return button;
        }
    }
}
