namespace ModelTest.MeterTest
{
    /// <summary>
    /// MeterTest 窗体的 Designer 部分。
    /// 仅保留控件声明和布局初始化，业务逻辑统一在 MeterTest.cs 中维护。
    /// </summary>
    partial class MeterTest
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
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            mainLayout = new TableLayoutPanel();
            headerPanel = new Panel();
            lblSystemTitle = new Label();
            picLogo = new PictureBox();
            groupOperation = new GroupBox();
            buttonGrid = new TableLayoutPanel();
            btnStartTest = new Button();
            btnStopTest = new Button();
            btnTestPlan = new Button();
            btnAssetInfo = new Button();
            middleArea = new TableLayoutPanel();
            groupScheme = new GroupBox();
            schemeTreeView = new TreeView();
            groupProcess = new GroupBox();
            processLayout = new TableLayoutPanel();
            stationSelectionPanel = new FlowLayoutPanel();
            rbMultiStation = new RadioButton();
            rbSingleStation = new RadioButton();
            btnSelectAllStations = new Button();
            btnClearStationSelection = new Button();
            btnSaveAssetInfo = new Button();
            btnBatchApplyAssetInfo = new Button();
            stationGrid = new DataGridView();
            colStationSelected = new DataGridViewCheckBoxColumn();
            colStationNo = new DataGridViewTextBoxColumn();
            colStationIp = new DataGridViewTextBoxColumn();
            colStationPort = new DataGridViewTextBoxColumn();
            colStationTestContent = new DataGridViewTextBoxColumn();
            colMeterType = new DataGridViewComboBoxColumn();
            colMeterAccessMode = new DataGridViewComboBoxColumn();
            colMeterVoltage = new DataGridViewTextBoxColumn();
            colMeterCurrent = new DataGridViewTextBoxColumn();
            colMeterActiveClass = new DataGridViewComboBoxColumn();
            colMeterActiveConstant = new DataGridViewTextBoxColumn();
            colMeterReactiveClass = new DataGridViewComboBoxColumn();
            colMeterReactiveConstant = new DataGridViewTextBoxColumn();
            colStationMeterAddress = new DataGridViewTextBoxColumn();
            colStationResult = new DataGridViewTextBoxColumn();
            colStationTime = new DataGridViewTextBoxColumn();
            processGrid = new DataGridView();
            colProcessNo = new DataGridViewTextBoxColumn();
            colProcessItem = new DataGridViewTextBoxColumn();
            colProcessResult = new DataGridViewTextBoxColumn();
            colProcessTime = new DataGridViewTextBoxColumn();
            groupHardware = new GroupBox();
            hardwareLayout = new TableLayoutPanel();
            mainLayout.SuspendLayout();
            headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            groupOperation.SuspendLayout();
            buttonGrid.SuspendLayout();
            middleArea.SuspendLayout();
            groupScheme.SuspendLayout();
            groupProcess.SuspendLayout();
            processLayout.SuspendLayout();
            stationSelectionPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)stationGrid).BeginInit();
            ((System.ComponentModel.ISupportInitialize)processGrid).BeginInit();
            groupHardware.SuspendLayout();
            hardwareLayout.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 1;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainLayout.Controls.Add(headerPanel, 0, 0);
            mainLayout.Controls.Add(groupOperation, 0, 1);
            mainLayout.Controls.Add(middleArea, 0, 2);
            mainLayout.Controls.Add(groupHardware, 0, 3);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Name = "mainLayout";
            mainLayout.Padding = new Padding(10);
            mainLayout.RowCount = 4;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 81F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 124F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 85F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 15F));
            mainLayout.Size = new Size(1280, 760);
            mainLayout.TabIndex = 0;
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.White;
            headerPanel.Controls.Add(lblSystemTitle);
            headerPanel.Controls.Add(picLogo);
            headerPanel.Dock = DockStyle.Fill;
            headerPanel.Location = new Point(10, 10);
            headerPanel.Margin = new Padding(0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(1260, 81);
            headerPanel.TabIndex = 0;
            // 
            // lblSystemTitle
            // 
            lblSystemTitle.Dock = DockStyle.Fill;
            lblSystemTitle.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
            lblSystemTitle.ForeColor = Color.FromArgb(31, 41, 55);
            lblSystemTitle.Location = new Point(883, 0);
            lblSystemTitle.Name = "lblSystemTitle";
            lblSystemTitle.Padding = new Padding(24, 0, 0, 0);
            lblSystemTitle.Size = new Size(377, 81);
            lblSystemTitle.TabIndex = 1;
            lblSystemTitle.Text = "自动测试系统";
            lblSystemTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // picLogo
            // 
            picLogo.Dock = DockStyle.Left;
            picLogo.Location = new Point(0, 0);
            picLogo.Name = "picLogo";
            picLogo.Size = new Size(883, 81);
            picLogo.SizeMode = PictureBoxSizeMode.StretchImage;
            picLogo.TabIndex = 0;
            picLogo.TabStop = false;
            // 
            // groupOperation
            // 
            groupOperation.Controls.Add(buttonGrid);
            groupOperation.Dock = DockStyle.Fill;
            groupOperation.Location = new Point(13, 94);
            groupOperation.Name = "groupOperation";
            groupOperation.Padding = new Padding(8);
            groupOperation.Size = new Size(1254, 118);
            groupOperation.TabIndex = 1;
            groupOperation.TabStop = false;
            groupOperation.Text = "操作区";
            // 
            // buttonGrid
            // 
            buttonGrid.ColumnCount = 4;
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320F));
            buttonGrid.Controls.Add(btnStartTest, 0, 0);
            buttonGrid.Controls.Add(btnStopTest, 1, 0);
            buttonGrid.Controls.Add(btnTestPlan, 2, 0);
            buttonGrid.Controls.Add(btnAssetInfo, 3, 0);
            buttonGrid.Dock = DockStyle.Left;
            buttonGrid.Location = new Point(8, 35);
            buttonGrid.Name = "buttonGrid";
            buttonGrid.RowCount = 1;
            buttonGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            buttonGrid.Size = new Size(1280, 75);
            buttonGrid.TabIndex = 0;
            // 
            // btnStartTest
            // 
            btnStartTest.FlatStyle = FlatStyle.Flat;
            btnStartTest.ImageAlign = ContentAlignment.MiddleLeft;
            btnStartTest.Location = new Point(0, 0);
            btnStartTest.Margin = new Padding(0);
            btnStartTest.Name = "btnStartTest";
            btnStartTest.Size = new Size(300, 75);
            btnStartTest.TabIndex = 1;
            btnStartTest.Text = "开始测试";
            btnStartTest.TextAlign = ContentAlignment.MiddleCenter;
            btnStartTest.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnStartTest.UseVisualStyleBackColor = true;
            // 
            // btnStopTest
            // 
            btnStopTest.FlatStyle = FlatStyle.Flat;
            btnStopTest.ImageAlign = ContentAlignment.MiddleLeft;
            btnStopTest.Location = new Point(320, 0);
            btnStopTest.Margin = new Padding(0);
            btnStopTest.Name = "btnStopTest";
            btnStopTest.Size = new Size(300, 75);
            btnStopTest.TabIndex = 2;
            btnStopTest.Text = "停止测试";
            btnStopTest.TextAlign = ContentAlignment.MiddleCenter;
            btnStopTest.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnStopTest.UseVisualStyleBackColor = true;
            // 
            // btnTestPlan
            // 
            btnTestPlan.FlatStyle = FlatStyle.Flat;
            btnTestPlan.ImageAlign = ContentAlignment.MiddleLeft;
            btnTestPlan.Location = new Point(640, 0);
            btnTestPlan.Margin = new Padding(0);
            btnTestPlan.Name = "btnTestPlan";
            btnTestPlan.Size = new Size(300, 75);
            btnTestPlan.TabIndex = 3;
            btnTestPlan.Text = "测试方案";
            btnTestPlan.TextAlign = ContentAlignment.MiddleCenter;
            btnTestPlan.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnTestPlan.UseVisualStyleBackColor = true;
            // 
            // btnAssetInfo
            // 
            btnAssetInfo.FlatStyle = FlatStyle.Flat;
            btnAssetInfo.ImageAlign = ContentAlignment.MiddleLeft;
            btnAssetInfo.Location = new Point(960, 0);
            btnAssetInfo.Margin = new Padding(0);
            btnAssetInfo.Name = "btnAssetInfo";
            btnAssetInfo.Size = new Size(300, 75);
            btnAssetInfo.TabIndex = 4;
            btnAssetInfo.Text = "资产信息";
            btnAssetInfo.TextAlign = ContentAlignment.MiddleCenter;
            btnAssetInfo.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnAssetInfo.UseVisualStyleBackColor = true;
            // 
            // middleArea
            // 
            middleArea.ColumnCount = 2;
            middleArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            middleArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            middleArea.Controls.Add(groupScheme, 0, 0);
            middleArea.Controls.Add(groupProcess, 1, 0);
            middleArea.Dock = DockStyle.Fill;
            middleArea.Location = new Point(13, 218);
            middleArea.Name = "middleArea";
            middleArea.RowCount = 1;
            middleArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            middleArea.Size = new Size(1254, 319);
            middleArea.TabIndex = 2;
            // 
            // groupScheme
            // 
            groupScheme.Controls.Add(schemeTreeView);
            groupScheme.Dock = DockStyle.Fill;
            groupScheme.Location = new Point(3, 3);
            groupScheme.Name = "groupScheme";
            groupScheme.Padding = new Padding(8);
            groupScheme.Size = new Size(244, 296);
            groupScheme.TabIndex = 0;
            groupScheme.TabStop = false;
            groupScheme.Text = "方案区域";
            // 
            // schemeTreeView
            // 
            schemeTreeView.BorderStyle = BorderStyle.FixedSingle;
            schemeTreeView.Dock = DockStyle.Fill;
            schemeTreeView.FullRowSelect = true;
            schemeTreeView.HideSelection = false;
            schemeTreeView.Location = new Point(8, 35);
            schemeTreeView.Name = "schemeTreeView";
            schemeTreeView.Size = new Size(228, 253);
            schemeTreeView.TabIndex = 0;
            // 
            // groupProcess
            // 
            groupProcess.Controls.Add(processLayout);
            groupProcess.Dock = DockStyle.Fill;
            groupProcess.Location = new Point(253, 3);
            groupProcess.Name = "groupProcess";
            groupProcess.Padding = new Padding(8);
            groupProcess.Size = new Size(998, 296);
            groupProcess.TabIndex = 1;
            groupProcess.TabStop = false;
            groupProcess.Text = "测试过程区域";
            // 
            // processLayout
            // 
            processLayout.ColumnCount = 1;
            processLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            processLayout.Controls.Add(stationSelectionPanel, 0, 0);
            processLayout.Controls.Add(stationGrid, 0, 1);
            processLayout.Controls.Add(processGrid, 0, 2);
            processLayout.Dock = DockStyle.Fill;
            processLayout.Location = new Point(8, 35);
            processLayout.Margin = new Padding(0);
            processLayout.Name = "processLayout";
            processLayout.RowCount = 3;
            processLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
            processLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 72F));
            processLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
            processLayout.Size = new Size(982, 253);
            processLayout.TabIndex = 1;
            // 
            // stationSelectionPanel
            // 
            stationSelectionPanel.Controls.Add(rbMultiStation);
            stationSelectionPanel.Controls.Add(rbSingleStation);
            stationSelectionPanel.Controls.Add(btnSelectAllStations);
            stationSelectionPanel.Controls.Add(btnClearStationSelection);
            stationSelectionPanel.Controls.Add(btnSaveAssetInfo);
            stationSelectionPanel.Controls.Add(btnBatchApplyAssetInfo);
            stationSelectionPanel.Dock = DockStyle.Fill;
            stationSelectionPanel.Location = new Point(0, 0);
            stationSelectionPanel.Margin = new Padding(0, 0, 0, 4);
            stationSelectionPanel.Name = "stationSelectionPanel";
            stationSelectionPanel.Padding = new Padding(2, 4, 0, 0);
            stationSelectionPanel.Size = new Size(982, 62);
            stationSelectionPanel.TabIndex = 0;
            stationSelectionPanel.WrapContents = false;
            // 
            // rbMultiStation
            // 
            rbMultiStation.Checked = true;
            rbMultiStation.Location = new Point(5, 7);
            rbMultiStation.Name = "rbMultiStation";
            rbMultiStation.Size = new Size(120, 28);
            rbMultiStation.TabIndex = 0;
            rbMultiStation.TabStop = true;
            rbMultiStation.Text = "多工位选取";
            rbMultiStation.UseVisualStyleBackColor = true;
            // 
            // rbSingleStation
            // 
            rbSingleStation.Location = new Point(131, 7);
            rbSingleStation.Name = "rbSingleStation";
            rbSingleStation.Size = new Size(120, 28);
            rbSingleStation.TabIndex = 1;
            rbSingleStation.Text = "单工位选取";
            rbSingleStation.UseVisualStyleBackColor = true;
            // 
            // btnSelectAllStations
            // 
            btnSelectAllStations.FlatStyle = FlatStyle.Flat;
            btnSelectAllStations.Location = new Point(257, 6);
            btnSelectAllStations.Margin = new Padding(3, 2, 8, 0);
            btnSelectAllStations.Name = "btnSelectAllStations";
            btnSelectAllStations.Size = new Size(150, 58);
            btnSelectAllStations.TabIndex = 2;
            btnSelectAllStations.Text = "全选";
            btnSelectAllStations.UseVisualStyleBackColor = true;
            // 
            // btnClearStationSelection
            // 
            btnClearStationSelection.FlatStyle = FlatStyle.Flat;
            btnClearStationSelection.Location = new Point(418, 6);
            btnClearStationSelection.Margin = new Padding(3, 2, 3, 0);
            btnClearStationSelection.Name = "btnClearStationSelection";
            btnClearStationSelection.Size = new Size(150, 58);
            btnClearStationSelection.TabIndex = 3;
            btnClearStationSelection.Text = "清空";
            btnClearStationSelection.UseVisualStyleBackColor = true;
            // 
            // btnSaveAssetInfo
            // 
            btnSaveAssetInfo.FlatStyle = FlatStyle.Flat;
            btnSaveAssetInfo.Location = new Point(574, 6);
            btnSaveAssetInfo.Margin = new Padding(3, 2, 8, 0);
            btnSaveAssetInfo.Name = "btnSaveAssetInfo";
            btnSaveAssetInfo.Size = new Size(150, 58);
            btnSaveAssetInfo.TabIndex = 4;
            btnSaveAssetInfo.Text = "保存";
            btnSaveAssetInfo.UseVisualStyleBackColor = true;
            btnSaveAssetInfo.Visible = false;
            // 
            // btnBatchApplyAssetInfo
            // 
            btnBatchApplyAssetInfo.FlatStyle = FlatStyle.Flat;
            btnBatchApplyAssetInfo.Location = new Point(735, 6);
            btnBatchApplyAssetInfo.Margin = new Padding(3, 2, 3, 0);
            btnBatchApplyAssetInfo.Name = "btnBatchApplyAssetInfo";
            btnBatchApplyAssetInfo.Size = new Size(150, 58);
            btnBatchApplyAssetInfo.TabIndex = 5;
            btnBatchApplyAssetInfo.Text = "批量修改";
            btnBatchApplyAssetInfo.UseVisualStyleBackColor = true;
            btnBatchApplyAssetInfo.Visible = false;
            // 
            // stationGrid
            // 
            stationGrid.AllowUserToAddRows = false;
            stationGrid.AllowUserToDeleteRows = false;
            stationGrid.AllowUserToResizeColumns = false;
            stationGrid.AllowUserToResizeRows = false;
            stationGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            stationGrid.BackgroundColor = Color.White;
            stationGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            stationGrid.Columns.AddRange(new DataGridViewColumn[] { colStationSelected, colStationNo, colStationIp, colStationPort, colStationTestContent, colMeterType, colMeterAccessMode, colMeterVoltage, colMeterCurrent, colMeterActiveClass, colMeterActiveConstant, colMeterReactiveClass, colMeterReactiveConstant, colStationMeterAddress, colStationResult, colStationTime });
            stationGrid.Dock = DockStyle.Fill;
            stationGrid.Location = new Point(0, 66);
            stationGrid.Margin = new Padding(0, 0, 0, 4);
            stationGrid.Name = "stationGrid";
            stationGrid.ReadOnly = false;
            stationGrid.RowHeadersVisible = false;
            stationGrid.RowHeadersWidth = 82;
            stationGrid.RowTemplate.Height = 34;
            stationGrid.Size = new Size(982, 132);
            stationGrid.TabIndex = 1;
            // 
            // colStationSelected
            // 
            colStationSelected.FillWeight = 32F;
            colStationSelected.HeaderText = "选择";
            colStationSelected.MinimumWidth = 100;
            colStationSelected.Name = "colStationSelected";
            colStationSelected.Resizable = DataGridViewTriState.False;
            colStationSelected.Width = 100;
            // 
            // colStationNo
            // 
            colStationNo.FillWeight = 36F;
            colStationNo.HeaderText = "工位";
            colStationNo.MinimumWidth = 100;
            colStationNo.Name = "colStationNo";
            colStationNo.ReadOnly = true;
            colStationNo.Resizable = DataGridViewTriState.False;
            colStationNo.Width = 100;
            // 
            // colStationIp
            // 
            colStationIp.FillWeight = 82F;
            colStationIp.HeaderText = "IP";
            colStationIp.MinimumWidth = 350;
            colStationIp.Name = "colStationIp";
            colStationIp.Resizable = DataGridViewTriState.False;
            colStationIp.Width = 350;
            // 
            // colStationPort
            // 
            colStationPort.FillWeight = 52F;
            colStationPort.HeaderText = "Port";
            colStationPort.MinimumWidth = 100;
            colStationPort.Name = "colStationPort";
            colStationPort.Resizable = DataGridViewTriState.False;
            colStationPort.Width = 100;
            // 
            // colStationTestContent
            // 
            colStationTestContent.FillWeight = 100F;
            colStationTestContent.HeaderText = "测试内容";
            colStationTestContent.MinimumWidth = 400;
            colStationTestContent.Name = "colStationTestContent";
            colStationTestContent.ReadOnly = true;
            colStationTestContent.Resizable = DataGridViewTriState.False;
            colStationTestContent.Width = 400;
            // 
            // colMeterType
            // 
            colMeterType.HeaderText = "电表类型";
            colMeterType.Items.AddRange(new object[] { "单相", "三相" });
            colMeterType.MinimumWidth = 200;
            colMeterType.Name = "colMeterType";
            colMeterType.Resizable = DataGridViewTriState.False;
            colMeterType.Width = 200;
            // 
            // colMeterAccessMode
            // 
            colMeterAccessMode.HeaderText = "接入方式";
            colMeterAccessMode.Items.AddRange(new object[] { "直接式", "互感式" });
            colMeterAccessMode.MinimumWidth = 200;
            colMeterAccessMode.Name = "colMeterAccessMode";
            colMeterAccessMode.Resizable = DataGridViewTriState.False;
            colMeterAccessMode.Width = 200;
            // 
            // colMeterVoltage
            // 
            colMeterVoltage.HeaderText = "电压";
            colMeterVoltage.MinimumWidth = 200;
            colMeterVoltage.Name = "colMeterVoltage";
            colMeterVoltage.Resizable = DataGridViewTriState.False;
            colMeterVoltage.Width = 200;
            // 
            // colMeterCurrent
            // 
            colMeterCurrent.HeaderText = "基本电流";
            colMeterCurrent.MinimumWidth = 200;
            colMeterCurrent.Name = "colMeterCurrent";
            colMeterCurrent.Resizable = DataGridViewTriState.False;
            colMeterCurrent.Width = 200;
            // 
            // colMeterActiveClass
            // 
            colMeterActiveClass.HeaderText = "有功等级";
            colMeterActiveClass.Items.AddRange(new object[] { "A", "B", "C", "D" });
            colMeterActiveClass.MinimumWidth = 200;
            colMeterActiveClass.Name = "colMeterActiveClass";
            colMeterActiveClass.Resizable = DataGridViewTriState.False;
            colMeterActiveClass.Width = 200;
            // 
            // colMeterActiveConstant
            // 
            colMeterActiveConstant.HeaderText = "有功常数";
            colMeterActiveConstant.MinimumWidth = 200;
            colMeterActiveConstant.Name = "colMeterActiveConstant";
            colMeterActiveConstant.Resizable = DataGridViewTriState.False;
            colMeterActiveConstant.Width = 200;
            // 
            // colMeterReactiveClass
            // 
            colMeterReactiveClass.HeaderText = "无功等级";
            colMeterReactiveClass.Items.AddRange(new object[] { "2.0", "3.0", "1S", "0.5S" });
            colMeterReactiveClass.MinimumWidth = 200;
            colMeterReactiveClass.Name = "colMeterReactiveClass";
            colMeterReactiveClass.Resizable = DataGridViewTriState.False;
            colMeterReactiveClass.Width = 200;
            // 
            // colMeterReactiveConstant
            // 
            colMeterReactiveConstant.HeaderText = "无功常数";
            colMeterReactiveConstant.MinimumWidth = 200;
            colMeterReactiveConstant.Name = "colMeterReactiveConstant";
            colMeterReactiveConstant.Resizable = DataGridViewTriState.False;
            colMeterReactiveConstant.Width = 200;
            // 
            // colStationMeterAddress
            // 
            colStationMeterAddress.FillWeight = 88F;
            colStationMeterAddress.HeaderText = "表位地址";
            colStationMeterAddress.MinimumWidth = 300;
            colStationMeterAddress.Name = "colStationMeterAddress";
            colStationMeterAddress.Resizable = DataGridViewTriState.False;
            colStationMeterAddress.Width = 300;
            // 
            // colStationResult
            // 
            colStationResult.FillWeight = 55F;
            colStationResult.HeaderText = "结果";
            colStationResult.MinimumWidth = 100;
            colStationResult.Name = "colStationResult";
            colStationResult.ReadOnly = true;
            colStationResult.Resizable = DataGridViewTriState.False;
            colStationResult.Width = 100;
            // 
            // colStationTime
            // 
            colStationTime.FillWeight = 62F;
            colStationTime.HeaderText = "时间";
            colStationTime.MinimumWidth = 200;
            colStationTime.Name = "colStationTime";
            colStationTime.ReadOnly = true;
            colStationTime.Resizable = DataGridViewTriState.False;
            colStationTime.Width = 200;
            // 
            // processGrid
            // 
            processGrid.AllowUserToAddRows = false;
            processGrid.AllowUserToDeleteRows = false;
            processGrid.AllowUserToResizeRows = false;
            processGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            processGrid.BackgroundColor = Color.White;
            processGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            processGrid.Columns.AddRange(new DataGridViewColumn[] { colProcessNo, colProcessItem, colProcessResult, colProcessTime });
            processGrid.Dock = DockStyle.Fill;
            processGrid.Location = new Point(0, 195);
            processGrid.Margin = new Padding(0);
            processGrid.Name = "processGrid";
            processGrid.ReadOnly = true;
            processGrid.RowHeadersVisible = false;
            processGrid.RowHeadersWidth = 82;
            processGrid.RowTemplate.Height = 40;
            processGrid.Size = new Size(982, 58);
            processGrid.TabIndex = 2;
            // 
            // colProcessNo
            // 
            colProcessNo.FillWeight = 35F;
            colProcessNo.HeaderText = "序号";
            colProcessNo.MinimumWidth = 10;
            colProcessNo.Name = "colProcessNo";
            colProcessNo.ReadOnly = true;
            // 
            // colProcessItem
            // 
            colProcessItem.HeaderText = "测试项";
            colProcessItem.MinimumWidth = 10;
            colProcessItem.Name = "colProcessItem";
            colProcessItem.ReadOnly = true;
            // 
            // colProcessResult
            // 
            colProcessResult.FillWeight = 55F;
            colProcessResult.HeaderText = "结果";
            colProcessResult.MinimumWidth = 10;
            colProcessResult.Name = "colProcessResult";
            colProcessResult.ReadOnly = true;
            // 
            // colProcessTime
            // 
            colProcessTime.FillWeight = 65F;
            colProcessTime.HeaderText = "时间";
            colProcessTime.MinimumWidth = 10;
            colProcessTime.Name = "colProcessTime";
            colProcessTime.ReadOnly = true;
            // 
            // groupHardware
            // 
            groupHardware.Controls.Add(hardwareLayout);
            groupHardware.Dock = DockStyle.Fill;
            groupHardware.Location = new Point(13, 523);
            groupHardware.Name = "groupHardware";
            groupHardware.Padding = new Padding(8);
            groupHardware.Size = new Size(1254, 224);
            groupHardware.TabIndex = 3;
            groupHardware.TabStop = false;
            groupHardware.Text = "台体信息采集区域";
            // 
            // hardwareLayout
            // 
            hardwareLayout.ColumnCount = 8;
            hardwareLayout.Dock = DockStyle.Fill;
            hardwareLayout.Location = new Point(8, 35);
            hardwareLayout.Margin = new Padding(0);
            hardwareLayout.Name = "hardwareLayout";
            hardwareLayout.Padding = new Padding(8);
            hardwareLayout.RowCount = 3;
            hardwareLayout.Size = new Size(1238, 181);
            hardwareLayout.TabIndex = 0;
            // 
            // MeterTest
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(232, 239, 236);
            ClientSize = new Size(1280, 760);
            Controls.Add(mainLayout);
            MaximizeBox = false;
            MinimizeBox = true;
            Name = "MeterTest";
            StartPosition = FormStartPosition.CenterParent;
            Text = "电表测试";
            WindowState = FormWindowState.Maximized;
            mainLayout.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            groupOperation.ResumeLayout(false);
            buttonGrid.ResumeLayout(false);
            middleArea.ResumeLayout(false);
            groupScheme.ResumeLayout(false);
            groupProcess.ResumeLayout(false);
            processLayout.ResumeLayout(false);
            stationSelectionPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)stationGrid).EndInit();
            ((System.ComponentModel.ISupportInitialize)processGrid).EndInit();
            groupHardware.ResumeLayout(false);
            hardwareLayout.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel mainLayout;
        private Panel headerPanel;
        private PictureBox picLogo;
        private Label lblSystemTitle;
        private GroupBox groupOperation;
        private TableLayoutPanel buttonGrid;
        private Button btnStartTest;
        private Button btnStopTest;
        private Button btnTestPlan;
        private Button btnAssetInfo;
        private TableLayoutPanel middleArea;
        private GroupBox groupScheme;
        private TreeView schemeTreeView;
        private GroupBox groupProcess;
        private TableLayoutPanel processLayout;
        private FlowLayoutPanel stationSelectionPanel;
        private RadioButton rbMultiStation;
        private RadioButton rbSingleStation;
        private Button btnSelectAllStations;
        private Button btnClearStationSelection;
        private Button btnSaveAssetInfo;
        private Button btnBatchApplyAssetInfo;
        private DataGridView stationGrid;
        private DataGridViewCheckBoxColumn colStationSelected;
        private DataGridViewTextBoxColumn colStationNo;
        private DataGridViewTextBoxColumn colStationIp;
        private DataGridViewTextBoxColumn colStationPort;
        private DataGridViewTextBoxColumn colStationTestContent;
        private DataGridViewComboBoxColumn colMeterType;
        private DataGridViewComboBoxColumn colMeterAccessMode;
        private DataGridViewTextBoxColumn colMeterVoltage;
        private DataGridViewTextBoxColumn colMeterCurrent;
        private DataGridViewComboBoxColumn colMeterActiveClass;
        private DataGridViewTextBoxColumn colMeterActiveConstant;
        private DataGridViewComboBoxColumn colMeterReactiveClass;
        private DataGridViewTextBoxColumn colMeterReactiveConstant;
        private DataGridViewTextBoxColumn colStationMeterAddress;
        private DataGridViewTextBoxColumn colStationResult;
        private DataGridViewTextBoxColumn colStationTime;
        private DataGridView processGrid;
        private DataGridViewTextBoxColumn colProcessNo;
        private DataGridViewTextBoxColumn colProcessItem;
        private DataGridViewTextBoxColumn colProcessResult;
        private DataGridViewTextBoxColumn colProcessTime;
        private GroupBox groupHardware;
        private TableLayoutPanel hardwareLayout;
    }
}
