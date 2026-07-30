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
            btnTestResults = new Button();
            middleArea = new TableLayoutPanel();
            groupScheme = new GroupBox();
            schemeTreeView = new TreeView();
            groupProcess = new GroupBox();
            processLayout = new TableLayoutPanel();
            countdownPanel = new Panel();
            lblTestCountdown = new Label();
            groupTestLog = new GroupBox();
            rtbTestProcessLog = new RichTextBox();
            stationSelectionPanel = new FlowLayoutPanel();
            rbMultiStation = new RadioButton();
            rbSingleStation = new RadioButton();
            btnSelectAllStations = new Button();
            btnClearStationSelection = new Button();
            btnShutDownSource = new Button();
            btnSaveTestResults = new Button();
            btnSaveAssetInfo = new Button();
            btnBatchApplyAssetInfo = new Button();
            lblBarcodeRule = new Label();
            cbxBarcodeRule = new ComboBox();
            lblBarcodeStartIndex = new Label();
            tbxBarcodeStartIndex = new TextBox();
            lblBarcodeEndIndex = new Label();
            tbxBarcodeEndIndex = new TextBox();
            lblBarcodeSecondStart = new Label();
            tbxBarcodeSecondStart = new TextBox();
            lblBarcodeSecondLength = new Label();
            tbxBarcodeSecondLength = new TextBox();
            stationGrid = new DataGridView();
            colStationSelected = new DataGridViewCheckBoxColumn();
            colStationNo = new DataGridViewTextBoxColumn();
            colStationIp = new DataGridViewTextBoxColumn();
            colStationPort = new DataGridViewTextBoxColumn();
            colStationBarcode = new DataGridViewTextBoxColumn();
            colStationTestContent = new DataGridViewTextBoxColumn();
            colMeterType = new DataGridViewComboBoxColumn();
            colMeterAccessMode = new DataGridViewComboBoxColumn();
            colMeterVoltage = new DataGridViewTextBoxColumn();
            colMeterCurrent = new DataGridViewTextBoxColumn();
            colMeterCurrentSpecification = new DataGridViewComboBoxColumn();
            colMeterActiveClass = new DataGridViewComboBoxColumn();
            colMeterActiveConstant = new DataGridViewTextBoxColumn();
            colMeterReactiveClass = new DataGridViewComboBoxColumn();
            colMeterReactiveConstant = new DataGridViewTextBoxColumn();
            colStationMeterAddress = new DataGridViewTextBoxColumn();
            colMeterBaudRate = new DataGridViewComboBoxColumn();
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
            countdownPanel.SuspendLayout();
            groupTestLog.SuspendLayout();
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
            buttonGrid.ColumnCount = 5;
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            buttonGrid.Controls.Add(btnStartTest, 0, 0);
            buttonGrid.Controls.Add(btnStopTest, 1, 0);
            buttonGrid.Controls.Add(btnTestPlan, 2, 0);
            buttonGrid.Controls.Add(btnAssetInfo, 3, 0);
            buttonGrid.Controls.Add(btnTestResults, 4, 0);
            buttonGrid.Dock = DockStyle.Fill;
            buttonGrid.Location = new Point(8, 35);
            buttonGrid.Name = "buttonGrid";
            buttonGrid.RowCount = 1;
            buttonGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            buttonGrid.Size = new Size(1238, 75);
            buttonGrid.TabIndex = 0;
            // 
            // btnStartTest
            // 
            btnStartTest.FlatStyle = FlatStyle.Flat;
            btnStartTest.ImageAlign = ContentAlignment.MiddleLeft;
            btnStartTest.Location = new Point(0, 0);
            btnStartTest.Name = "btnStartTest";
            btnStartTest.Dock = DockStyle.Fill;
            btnStartTest.Margin = new Padding(0, 0, 10, 0);
            btnStartTest.Size = new Size(238, 75);
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
            btnStopTest.Location = new Point(247, 0);
            btnStopTest.Dock = DockStyle.Fill;
            btnStopTest.Margin = new Padding(0, 0, 10, 0);
            btnStopTest.Name = "btnStopTest";
            btnStopTest.Size = new Size(238, 75);
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
            btnTestPlan.Location = new Point(494, 0);
            btnTestPlan.Dock = DockStyle.Fill;
            btnTestPlan.Margin = new Padding(0, 0, 10, 0);
            btnTestPlan.Name = "btnTestPlan";
            btnTestPlan.Size = new Size(238, 75);
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
            btnAssetInfo.Location = new Point(741, 0);
            btnAssetInfo.Dock = DockStyle.Fill;
            btnAssetInfo.Margin = new Padding(0, 0, 10, 0);
            btnAssetInfo.Name = "btnAssetInfo";
            btnAssetInfo.Size = new Size(238, 75);
            btnAssetInfo.TabIndex = 4;
            btnAssetInfo.Text = "资产信息";
            btnAssetInfo.TextAlign = ContentAlignment.MiddleCenter;
            btnAssetInfo.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnAssetInfo.UseVisualStyleBackColor = true;
            // 
            // btnTestResults
            // 
            btnTestResults.Dock = DockStyle.Fill;
            btnTestResults.FlatStyle = FlatStyle.Flat;
            btnTestResults.ImageAlign = ContentAlignment.MiddleLeft;
            btnTestResults.Location = new Point(988, 0);
            btnTestResults.Margin = new Padding(0);
            btnTestResults.Name = "btnTestResults";
            btnTestResults.Size = new Size(250, 75);
            btnTestResults.TabIndex = 5;
            btnTestResults.Text = "测试结果";
            btnTestResults.TextAlign = ContentAlignment.MiddleCenter;
            btnTestResults.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnTestResults.UseVisualStyleBackColor = true;
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
            processLayout.ColumnCount = 2;
            processLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            processLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            processLayout.Controls.Add(stationSelectionPanel, 0, 0);
            processLayout.Controls.Add(countdownPanel, 1, 0);
            processLayout.Controls.Add(stationGrid, 0, 1);
            processLayout.Controls.Add(groupTestLog, 1, 1);
            processLayout.Controls.Add(processGrid, 0, 2);
            processLayout.SetColumnSpan(processGrid, 2);
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
            // countdownPanel
            // 
            countdownPanel.Controls.Add(lblTestCountdown);
            countdownPanel.Dock = DockStyle.Fill;
            countdownPanel.Location = new Point(710, 0);
            countdownPanel.Margin = new Padding(6, 0, 0, 4);
            countdownPanel.Name = "countdownPanel";
            countdownPanel.Padding = new Padding(8, 4, 8, 4);
            countdownPanel.Size = new Size(266, 62);
            countdownPanel.TabIndex = 4;
            // 
            // lblTestCountdown
            // 
            lblTestCountdown.BorderStyle = BorderStyle.FixedSingle;
            lblTestCountdown.Dock = DockStyle.Fill;
            lblTestCountdown.Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold, GraphicsUnit.Point);
            lblTestCountdown.ForeColor = Color.FromArgb(107, 114, 128);
            lblTestCountdown.Location = new Point(8, 4);
            lblTestCountdown.Name = "lblTestCountdown";
            lblTestCountdown.Padding = new Padding(8, 0, 8, 0);
            lblTestCountdown.Size = new Size(250, 54);
            lblTestCountdown.TabIndex = 0;
            lblTestCountdown.Text = "倒计时：未开始";
            lblTestCountdown.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // groupTestLog
            // 
            groupTestLog.Controls.Add(rtbTestProcessLog);
            groupTestLog.Dock = DockStyle.Fill;
            groupTestLog.Location = new Point(710, 69);
            groupTestLog.Margin = new Padding(6, 0, 0, 4);
            groupTestLog.Name = "groupTestLog";
            groupTestLog.Padding = new Padding(6, 8, 6, 6);
            groupTestLog.Size = new Size(266, 128);
            groupTestLog.TabIndex = 3;
            groupTestLog.TabStop = false;
            groupTestLog.Text = "测试日志";
            // 
            // rtbTestProcessLog
            // 
            rtbTestProcessLog.BackColor = Color.White;
            rtbTestProcessLog.BorderStyle = BorderStyle.FixedSingle;
            rtbTestProcessLog.Dock = DockStyle.Fill;
            rtbTestProcessLog.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            rtbTestProcessLog.HideSelection = false;
            rtbTestProcessLog.Location = new Point(6, 31);
            rtbTestProcessLog.Name = "rtbTestProcessLog";
            rtbTestProcessLog.ReadOnly = true;
            rtbTestProcessLog.ScrollBars = RichTextBoxScrollBars.Both;
            rtbTestProcessLog.Size = new Size(254, 91);
            rtbTestProcessLog.TabIndex = 0;
            rtbTestProcessLog.Text = "";
            // 
            // stationSelectionPanel
            // 
            stationSelectionPanel.Controls.Add(rbMultiStation);
            stationSelectionPanel.Controls.Add(rbSingleStation);
            stationSelectionPanel.Controls.Add(btnSelectAllStations);
            stationSelectionPanel.Controls.Add(btnClearStationSelection);
            stationSelectionPanel.Controls.Add(btnShutDownSource);
            stationSelectionPanel.Controls.Add(btnSaveTestResults);
            stationSelectionPanel.Controls.Add(btnSaveAssetInfo);
            stationSelectionPanel.Controls.Add(btnBatchApplyAssetInfo);
            stationSelectionPanel.Controls.Add(lblBarcodeRule);
            stationSelectionPanel.Controls.Add(cbxBarcodeRule);
            stationSelectionPanel.Controls.Add(lblBarcodeStartIndex);
            stationSelectionPanel.Controls.Add(tbxBarcodeStartIndex);
            stationSelectionPanel.Controls.Add(lblBarcodeEndIndex);
            stationSelectionPanel.Controls.Add(tbxBarcodeEndIndex);
            stationSelectionPanel.Controls.Add(lblBarcodeSecondStart);
            stationSelectionPanel.Controls.Add(tbxBarcodeSecondStart);
            stationSelectionPanel.Controls.Add(lblBarcodeSecondLength);
            stationSelectionPanel.Controls.Add(tbxBarcodeSecondLength);
            stationSelectionPanel.Dock = DockStyle.Fill;
            stationSelectionPanel.AutoScroll = true;
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
            // btnShutDownSource
            // 
            btnShutDownSource.BackColor = Color.FromArgb(185, 28, 28);
            btnShutDownSource.FlatAppearance.BorderColor = Color.FromArgb(127, 29, 29);
            btnShutDownSource.FlatStyle = FlatStyle.Flat;
            btnShutDownSource.ForeColor = Color.White;
            btnShutDownSource.Location = new Point(574, 6);
            btnShutDownSource.Margin = new Padding(3, 2, 8, 0);
            btnShutDownSource.Name = "btnShutDownSource";
            btnShutDownSource.Size = new Size(150, 58);
            btnShutDownSource.TabIndex = 4;
            btnShutDownSource.Text = "降源";
            btnShutDownSource.UseVisualStyleBackColor = false;
            // 
            // btnSaveTestResults
            // 
            btnSaveTestResults.FlatStyle = FlatStyle.Flat;
            btnSaveTestResults.Location = new Point(735, 6);
            btnSaveTestResults.Margin = new Padding(3, 2, 8, 0);
            btnSaveTestResults.Name = "btnSaveTestResults";
            btnSaveTestResults.Size = new Size(150, 58);
            btnSaveTestResults.TabIndex = 5;
            btnSaveTestResults.Text = "数据保存";
            btnSaveTestResults.UseVisualStyleBackColor = true;
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
            // lblBarcodeRule
            // 
            lblBarcodeRule.Location = new Point(893, 12);
            lblBarcodeRule.Margin = new Padding(5, 8, 3, 0);
            lblBarcodeRule.Name = "lblBarcodeRule";
            lblBarcodeRule.Size = new Size(72, 32);
            lblBarcodeRule.TabIndex = 6;
            lblBarcodeRule.Text = "条码规则";
            lblBarcodeRule.TextAlign = ContentAlignment.MiddleLeft;
            lblBarcodeRule.Visible = false;
            // 
            // cbxBarcodeRule
            // 
            cbxBarcodeRule.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxBarcodeRule.FormattingEnabled = true;
            cbxBarcodeRule.Items.AddRange(new object[] { "规则1：单区间", "规则2：双区间" });
            cbxBarcodeRule.Location = new Point(989, 10);
            cbxBarcodeRule.Margin = new Padding(3, 6, 8, 0);
            cbxBarcodeRule.Name = "cbxBarcodeRule";
            cbxBarcodeRule.Size = new Size(160, 36);
            cbxBarcodeRule.TabIndex = 7;
            cbxBarcodeRule.Visible = false;
            // 
            // lblBarcodeStartIndex
            // 
            lblBarcodeStartIndex.Location = new Point(893, 12);
            lblBarcodeStartIndex.Margin = new Padding(5, 8, 3, 0);
            lblBarcodeStartIndex.Name = "lblBarcodeStartIndex";
            lblBarcodeStartIndex.Size = new Size(88, 32);
            lblBarcodeStartIndex.TabIndex = 6;
            lblBarcodeStartIndex.Text = "条码起始位";
            lblBarcodeStartIndex.TextAlign = ContentAlignment.MiddleLeft;
            lblBarcodeStartIndex.Visible = false;
            // 
            // tbxBarcodeStartIndex
            // 
            tbxBarcodeStartIndex.Location = new Point(1019, 10);
            tbxBarcodeStartIndex.Margin = new Padding(3, 6, 8, 0);
            tbxBarcodeStartIndex.Name = "tbxBarcodeStartIndex";
            tbxBarcodeStartIndex.Size = new Size(52, 34);
            tbxBarcodeStartIndex.TabIndex = 7;
            tbxBarcodeStartIndex.Text = "8";
            tbxBarcodeStartIndex.Visible = false;
            // 
            // lblBarcodeEndIndex
            // 
            lblBarcodeEndIndex.Location = new Point(1090, 12);
            lblBarcodeEndIndex.Margin = new Padding(3, 8, 3, 0);
            lblBarcodeEndIndex.Name = "lblBarcodeEndIndex";
            lblBarcodeEndIndex.Size = new Size(88, 32);
            lblBarcodeEndIndex.TabIndex = 8;
            lblBarcodeEndIndex.Text = "条码结束位";
            lblBarcodeEndIndex.TextAlign = ContentAlignment.MiddleLeft;
            lblBarcodeEndIndex.Visible = false;
            // 
            // tbxBarcodeEndIndex
            // 
            tbxBarcodeEndIndex.Location = new Point(1216, 10);
            tbxBarcodeEndIndex.Margin = new Padding(3, 6, 3, 0);
            tbxBarcodeEndIndex.Name = "tbxBarcodeEndIndex";
            tbxBarcodeEndIndex.Size = new Size(52, 34);
            tbxBarcodeEndIndex.TabIndex = 9;
            tbxBarcodeEndIndex.Text = "20";
            tbxBarcodeEndIndex.Visible = false;
            // 
            // lblBarcodeSecondStart
            // 
            lblBarcodeSecondStart.Margin = new Padding(3, 8, 3, 0);
            lblBarcodeSecondStart.Name = "lblBarcodeSecondStart";
            lblBarcodeSecondStart.Size = new Size(76, 32);
            lblBarcodeSecondStart.TabIndex = 10;
            lblBarcodeSecondStart.Text = "段2起始";
            lblBarcodeSecondStart.TextAlign = ContentAlignment.MiddleLeft;
            lblBarcodeSecondStart.Visible = false;
            // 
            // tbxBarcodeSecondStart
            // 
            tbxBarcodeSecondStart.Margin = new Padding(3, 6, 8, 0);
            tbxBarcodeSecondStart.Name = "tbxBarcodeSecondStart";
            tbxBarcodeSecondStart.Size = new Size(52, 34);
            tbxBarcodeSecondStart.TabIndex = 11;
            tbxBarcodeSecondStart.Text = "10";
            tbxBarcodeSecondStart.Visible = false;
            // 
            // lblBarcodeSecondLength
            // 
            lblBarcodeSecondLength.Margin = new Padding(3, 8, 3, 0);
            lblBarcodeSecondLength.Name = "lblBarcodeSecondLength";
            lblBarcodeSecondLength.Size = new Size(76, 32);
            lblBarcodeSecondLength.TabIndex = 12;
            lblBarcodeSecondLength.Text = "段2长度";
            lblBarcodeSecondLength.TextAlign = ContentAlignment.MiddleLeft;
            lblBarcodeSecondLength.Visible = false;
            // 
            // tbxBarcodeSecondLength
            // 
            tbxBarcodeSecondLength.Margin = new Padding(3, 6, 3, 0);
            tbxBarcodeSecondLength.Name = "tbxBarcodeSecondLength";
            tbxBarcodeSecondLength.Size = new Size(52, 34);
            tbxBarcodeSecondLength.TabIndex = 13;
            tbxBarcodeSecondLength.Text = "10";
            tbxBarcodeSecondLength.Visible = false;
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
            stationGrid.Columns.AddRange(new DataGridViewColumn[] { colStationSelected, colStationNo, colStationIp, colStationPort, colStationBarcode, colStationTestContent, colMeterType, colMeterAccessMode, colMeterVoltage, colMeterCurrent, colMeterCurrentSpecification, colMeterActiveClass, colMeterActiveConstant, colMeterReactiveClass, colMeterReactiveConstant, colStationMeterAddress, colMeterBaudRate, colStationResult, colStationTime });
            stationGrid.Dock = DockStyle.Fill;
            stationGrid.Location = new Point(0, 66);
            stationGrid.Margin = new Padding(0, 0, 0, 4);
            stationGrid.MultiSelect = false;
            stationGrid.Name = "stationGrid";
            stationGrid.ReadOnly = false;
            stationGrid.RowHeadersVisible = false;
            stationGrid.RowHeadersWidth = 82;
            stationGrid.RowTemplate.Height = 34;
            stationGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
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
            colStationIp.MinimumWidth = 250;
            colStationIp.Name = "colStationIp";
            colStationIp.Resizable = DataGridViewTriState.False;
            colStationIp.Width = 250;
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
            // colStationBarcode
            // 
            colStationBarcode.FillWeight = 88F;
            colStationBarcode.HeaderText = "条形码";
            colStationBarcode.MinimumWidth = 300;
            colStationBarcode.Name = "colStationBarcode";
            colStationBarcode.Resizable = DataGridViewTriState.False;
            colStationBarcode.Width = 300;
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
            colMeterType.MinimumWidth = 150;
            colMeterType.Name = "colMeterType";
            colMeterType.Resizable = DataGridViewTriState.False;
            colMeterType.Width = 150;
            // 
            // colMeterAccessMode
            // 
            colMeterAccessMode.HeaderText = "接入方式";
            colMeterAccessMode.MinimumWidth = 150;
            colMeterAccessMode.Name = "colMeterAccessMode";
            colMeterAccessMode.Resizable = DataGridViewTriState.False;
            colMeterAccessMode.Width = 150;
            // 
            // colMeterVoltage
            // 
            colMeterVoltage.HeaderText = "额定电压";
            colMeterVoltage.MinimumWidth = 150;
            colMeterVoltage.Name = "colMeterVoltage";
            colMeterVoltage.Resizable = DataGridViewTriState.False;
            colMeterVoltage.Width = 150;
            // 
            // colMeterCurrent
            // 
            colMeterCurrent.HeaderText = "基本电流";
            colMeterCurrent.MinimumWidth = 150;
            colMeterCurrent.Name = "colMeterCurrent";
            colMeterCurrent.Resizable = DataGridViewTriState.False;
            colMeterCurrent.Width = 150;
            // 
            // colMeterCurrentSpecification
            // 
            colMeterCurrentSpecification.HeaderText = "电流规格";
            colMeterCurrentSpecification.MinimumWidth = 220;
            colMeterCurrentSpecification.Name = "colMeterCurrentSpecification";
            colMeterCurrentSpecification.Resizable = DataGridViewTriState.False;
            colMeterCurrentSpecification.Width = 220;
            // 
            // colMeterActiveClass
            // 
            colMeterActiveClass.HeaderText = "有功等级";
            colMeterActiveClass.MinimumWidth = 150;
            colMeterActiveClass.Name = "colMeterActiveClass";
            colMeterActiveClass.Resizable = DataGridViewTriState.False;
            colMeterActiveClass.Width = 150;
            // 
            // colMeterActiveConstant
            // 
            colMeterActiveConstant.HeaderText = "有功常数";
            colMeterActiveConstant.MinimumWidth = 150;
            colMeterActiveConstant.Name = "colMeterActiveConstant";
            colMeterActiveConstant.Resizable = DataGridViewTriState.False;
            colMeterActiveConstant.Width = 150;
            // 
            // colMeterReactiveClass
            // 
            colMeterReactiveClass.HeaderText = "无功等级";
            colMeterReactiveClass.MinimumWidth = 150;
            colMeterReactiveClass.Name = "colMeterReactiveClass";
            colMeterReactiveClass.Resizable = DataGridViewTriState.False;
            colMeterReactiveClass.Width = 150;
            // 
            // colMeterReactiveConstant
            // 
            colMeterReactiveConstant.HeaderText = "无功常数";
            colMeterReactiveConstant.MinimumWidth = 150;
            colMeterReactiveConstant.Name = "colMeterReactiveConstant";
            colMeterReactiveConstant.Resizable = DataGridViewTriState.False;
            colMeterReactiveConstant.Width = 150;
            // 
            // colStationMeterAddress
            // 
            colStationMeterAddress.FillWeight = 88F;
            colStationMeterAddress.HeaderText = "电表地址";
            colStationMeterAddress.MinimumWidth = 200;
            colStationMeterAddress.Name = "colStationMeterAddress";
            colStationMeterAddress.Resizable = DataGridViewTriState.False;
            colStationMeterAddress.Width = 200;
            // 
            // colMeterBaudRate
            // 
            colMeterBaudRate.HeaderText = "波特率";
            colMeterBaudRate.MinimumWidth = 150;
            colMeterBaudRate.Name = "colMeterBaudRate";
            colMeterBaudRate.Resizable = DataGridViewTriState.False;
            colMeterBaudRate.Width = 150;
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
            MaximizeBox = true;
            MinimizeBox = true;
            Name = "MeterTest";
            StartPosition = FormStartPosition.CenterParent;
            Text = "电表测试";
            mainLayout.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
            groupOperation.ResumeLayout(false);
            buttonGrid.ResumeLayout(false);
            middleArea.ResumeLayout(false);
            groupScheme.ResumeLayout(false);
            groupProcess.ResumeLayout(false);
            processLayout.ResumeLayout(false);
            countdownPanel.ResumeLayout(false);
            groupTestLog.ResumeLayout(false);
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
        private Button btnTestResults;
        private TableLayoutPanel middleArea;
        private GroupBox groupScheme;
        private TreeView schemeTreeView;
        private GroupBox groupProcess;
        private TableLayoutPanel processLayout;
        private GroupBox groupTestLog;
        private RichTextBox rtbTestProcessLog;
        private FlowLayoutPanel stationSelectionPanel;
        private RadioButton rbMultiStation;
        private RadioButton rbSingleStation;
        private Button btnSelectAllStations;
        private Button btnClearStationSelection;
        private Button btnShutDownSource;
        private Button btnSaveTestResults;
        private Button btnSaveAssetInfo;
        private Button btnBatchApplyAssetInfo;
        private Label lblBarcodeRule;
        private ComboBox cbxBarcodeRule;
        private Label lblBarcodeStartIndex;
        private TextBox tbxBarcodeStartIndex;
        private Label lblBarcodeEndIndex;
        private TextBox tbxBarcodeEndIndex;
        private Label lblBarcodeSecondStart;
        private TextBox tbxBarcodeSecondStart;
        private Label lblBarcodeSecondLength;
        private TextBox tbxBarcodeSecondLength;
        private DataGridView stationGrid;
        private DataGridViewCheckBoxColumn colStationSelected;
        private DataGridViewTextBoxColumn colStationNo;
        private DataGridViewTextBoxColumn colStationIp;
        private DataGridViewTextBoxColumn colStationPort;
        private DataGridViewTextBoxColumn colStationBarcode;
        private DataGridViewTextBoxColumn colStationTestContent;
        private DataGridViewComboBoxColumn colMeterType;
        private DataGridViewComboBoxColumn colMeterAccessMode;
        private DataGridViewTextBoxColumn colMeterVoltage;
        private DataGridViewTextBoxColumn colMeterCurrent;
        private DataGridViewComboBoxColumn colMeterCurrentSpecification;
        private DataGridViewComboBoxColumn colMeterActiveClass;
        private DataGridViewTextBoxColumn colMeterActiveConstant;
        private DataGridViewComboBoxColumn colMeterReactiveClass;
        private DataGridViewTextBoxColumn colMeterReactiveConstant;
        private DataGridViewTextBoxColumn colStationMeterAddress;
        private DataGridViewComboBoxColumn colMeterBaudRate;
        private DataGridViewTextBoxColumn colStationResult;
        private DataGridViewTextBoxColumn colStationTime;
        private DataGridView processGrid;
        private Panel countdownPanel;
        private Label lblTestCountdown;
        private DataGridViewTextBoxColumn colProcessNo;
        private DataGridViewTextBoxColumn colProcessItem;
        private DataGridViewTextBoxColumn colProcessResult;
        private DataGridViewTextBoxColumn colProcessTime;
        private GroupBox groupHardware;
        private TableLayoutPanel hardwareLayout;
    }
}
