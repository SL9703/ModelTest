namespace ModelTest.MeterTest
{
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
            schemeGrid = new DataGridView();
            colSchemeNo = new DataGridViewTextBoxColumn();
            colSchemeName = new DataGridViewTextBoxColumn();
            colSchemeStatus = new DataGridViewTextBoxColumn();
            groupProcess = new GroupBox();
            processGrid = new DataGridView();
            colProcessNo = new DataGridViewTextBoxColumn();
            colProcessItem = new DataGridViewTextBoxColumn();
            colProcessResult = new DataGridViewTextBoxColumn();
            colProcessTime = new DataGridViewTextBoxColumn();
            groupHardware = new GroupBox();
            hardwareLayout = new TableLayoutPanel();
            lblComTitle = new Label();
            lblComValue = new Label();
            lblBaudTitle = new Label();
            lblBaudValue = new Label();
            lblMeterTitle = new Label();
            lblMeterValue = new Label();
            lblDeviceTitle = new Label();
            lblDeviceValue = new Label();
            mainLayout.SuspendLayout();
            headerPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
            groupOperation.SuspendLayout();
            buttonGrid.SuspendLayout();
            middleArea.SuspendLayout();
            groupScheme.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)schemeGrid).BeginInit();
            groupProcess.SuspendLayout();
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
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));
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
            groupOperation.Size = new Size(1254, 98);
            groupOperation.TabIndex = 1;
            groupOperation.TabStop = false;
            groupOperation.Text = "操作区";
            // 
            // buttonGrid
            // 
            buttonGrid.ColumnCount = 4;
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonGrid.Controls.Add(btnStartTest, 0, 0);
            buttonGrid.Controls.Add(btnStopTest, 1, 0);
            buttonGrid.Controls.Add(btnTestPlan, 2, 0);
            buttonGrid.Controls.Add(btnAssetInfo, 3, 0);
            buttonGrid.Dock = DockStyle.Fill;
            buttonGrid.Location = new Point(8, 35);
            buttonGrid.Name = "buttonGrid";
            buttonGrid.RowCount = 1;
            buttonGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            buttonGrid.Size = new Size(1238, 55);
            buttonGrid.TabIndex = 0;
            // 
            // btnStartTest
            // 
            btnStartTest.Dock = DockStyle.Fill;
            btnStartTest.FlatStyle = FlatStyle.Flat;
            btnStartTest.ImageAlign = ContentAlignment.MiddleLeft;
            btnStartTest.Location = new Point(4, 4);
            btnStartTest.Margin = new Padding(4);
            btnStartTest.Name = "btnStartTest";
            btnStartTest.Size = new Size(299, 47);
            btnStartTest.TabIndex = 1;
            btnStartTest.Text = "开始测试";
            btnStartTest.TextAlign = ContentAlignment.MiddleCenter;
            btnStartTest.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnStartTest.UseVisualStyleBackColor = true;
            // 
            // btnStopTest
            // 
            btnStopTest.Dock = DockStyle.Fill;
            btnStopTest.FlatStyle = FlatStyle.Flat;
            btnStopTest.ImageAlign = ContentAlignment.MiddleLeft;
            btnStopTest.Location = new Point(311, 4);
            btnStopTest.Margin = new Padding(4);
            btnStopTest.Name = "btnStopTest";
            btnStopTest.Size = new Size(299, 47);
            btnStopTest.TabIndex = 2;
            btnStopTest.Text = "停止测试";
            btnStopTest.TextAlign = ContentAlignment.MiddleCenter;
            btnStopTest.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnStopTest.UseVisualStyleBackColor = true;
            // 
            // btnTestPlan
            // 
            btnTestPlan.Dock = DockStyle.Fill;
            btnTestPlan.FlatStyle = FlatStyle.Flat;
            btnTestPlan.ImageAlign = ContentAlignment.MiddleLeft;
            btnTestPlan.Location = new Point(618, 4);
            btnTestPlan.Margin = new Padding(4);
            btnTestPlan.Name = "btnTestPlan";
            btnTestPlan.Size = new Size(299, 47);
            btnTestPlan.TabIndex = 3;
            btnTestPlan.Text = "测试方案";
            btnTestPlan.TextAlign = ContentAlignment.MiddleCenter;
            btnTestPlan.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnTestPlan.UseVisualStyleBackColor = true;
            // 
            // btnAssetInfo
            // 
            btnAssetInfo.Dock = DockStyle.Fill;
            btnAssetInfo.FlatStyle = FlatStyle.Flat;
            btnAssetInfo.ImageAlign = ContentAlignment.MiddleLeft;
            btnAssetInfo.Location = new Point(925, 4);
            btnAssetInfo.Margin = new Padding(4);
            btnAssetInfo.Name = "btnAssetInfo";
            btnAssetInfo.Size = new Size(309, 47);
            btnAssetInfo.TabIndex = 4;
            btnAssetInfo.Text = "资产信息";
            btnAssetInfo.TextAlign = ContentAlignment.MiddleCenter;
            btnAssetInfo.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnAssetInfo.UseVisualStyleBackColor = true;
            // 
            // middleArea
            // 
            middleArea.ColumnCount = 2;
            middleArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            middleArea.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            middleArea.Controls.Add(groupScheme, 0, 0);
            middleArea.Controls.Add(groupProcess, 1, 0);
            middleArea.Dock = DockStyle.Fill;
            middleArea.Location = new Point(13, 198);
            middleArea.Name = "middleArea";
            middleArea.RowCount = 1;
            middleArea.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            middleArea.Size = new Size(1254, 319);
            middleArea.TabIndex = 2;
            // 
            // groupScheme
            // 
            groupScheme.Controls.Add(schemeGrid);
            groupScheme.Dock = DockStyle.Fill;
            groupScheme.Location = new Point(3, 3);
            groupScheme.Name = "groupScheme";
            groupScheme.Padding = new Padding(8);
            groupScheme.Size = new Size(470, 313);
            groupScheme.TabIndex = 0;
            groupScheme.TabStop = false;
            groupScheme.Text = "方案区域";
            // 
            // schemeGrid
            // 
            schemeGrid.AllowUserToAddRows = false;
            schemeGrid.AllowUserToDeleteRows = false;
            schemeGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            schemeGrid.BackgroundColor = Color.White;
            schemeGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            schemeGrid.Columns.AddRange(new DataGridViewColumn[] { colSchemeNo, colSchemeName, colSchemeStatus });
            schemeGrid.Dock = DockStyle.Fill;
            schemeGrid.Location = new Point(8, 35);
            schemeGrid.Name = "schemeGrid";
            schemeGrid.ReadOnly = true;
            schemeGrid.RowHeadersVisible = false;
            schemeGrid.RowHeadersWidth = 82;
            schemeGrid.RowTemplate.Height = 40;
            schemeGrid.Size = new Size(454, 270);
            schemeGrid.TabIndex = 0;
            // 
            // colSchemeNo
            // 
            colSchemeNo.FillWeight = 35F;
            colSchemeNo.HeaderText = "序号";
            colSchemeNo.MinimumWidth = 10;
            colSchemeNo.Name = "colSchemeNo";
            colSchemeNo.ReadOnly = true;
            // 
            // colSchemeName
            // 
            colSchemeName.HeaderText = "方案名称";
            colSchemeName.MinimumWidth = 10;
            colSchemeName.Name = "colSchemeName";
            colSchemeName.ReadOnly = true;
            // 
            // colSchemeStatus
            // 
            colSchemeStatus.FillWeight = 55F;
            colSchemeStatus.HeaderText = "状态";
            colSchemeStatus.MinimumWidth = 10;
            colSchemeStatus.Name = "colSchemeStatus";
            colSchemeStatus.ReadOnly = true;
            // 
            // groupProcess
            // 
            groupProcess.Controls.Add(processGrid);
            groupProcess.Dock = DockStyle.Fill;
            groupProcess.Location = new Point(479, 3);
            groupProcess.Name = "groupProcess";
            groupProcess.Padding = new Padding(8);
            groupProcess.Size = new Size(772, 313);
            groupProcess.TabIndex = 1;
            groupProcess.TabStop = false;
            groupProcess.Text = "测试过程区域";
            // 
            // processGrid
            // 
            processGrid.AllowUserToAddRows = false;
            processGrid.AllowUserToDeleteRows = false;
            processGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            processGrid.BackgroundColor = Color.White;
            processGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            processGrid.Columns.AddRange(new DataGridViewColumn[] { colProcessNo, colProcessItem, colProcessResult, colProcessTime });
            processGrid.Dock = DockStyle.Fill;
            processGrid.Location = new Point(8, 35);
            processGrid.Name = "processGrid";
            processGrid.ReadOnly = true;
            processGrid.RowHeadersVisible = false;
            processGrid.RowHeadersWidth = 82;
            processGrid.RowTemplate.Height = 40;
            processGrid.Size = new Size(756, 270);
            processGrid.TabIndex = 0;
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
            groupHardware.Text = "硬件信息区域";
            // 
            // hardwareLayout
            // 
            hardwareLayout.ColumnCount = 4;
            hardwareLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            hardwareLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            hardwareLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            hardwareLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            hardwareLayout.Controls.Add(lblComTitle, 0, 0);
            hardwareLayout.Controls.Add(lblComValue, 1, 0);
            hardwareLayout.Controls.Add(lblBaudTitle, 2, 0);
            hardwareLayout.Controls.Add(lblBaudValue, 3, 0);
            hardwareLayout.Controls.Add(lblMeterTitle, 0, 1);
            hardwareLayout.Controls.Add(lblMeterValue, 1, 1);
            hardwareLayout.Controls.Add(lblDeviceTitle, 2, 1);
            hardwareLayout.Controls.Add(lblDeviceValue, 3, 1);
            hardwareLayout.Dock = DockStyle.Top;
            hardwareLayout.Location = new Point(8, 35);
            hardwareLayout.Name = "hardwareLayout";
            hardwareLayout.RowCount = 2;
            hardwareLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            hardwareLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            hardwareLayout.Size = new Size(1238, 100);
            hardwareLayout.TabIndex = 0;
            // 
            // lblComTitle
            // 
            lblComTitle.Dock = DockStyle.Fill;
            lblComTitle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblComTitle.Location = new Point(3, 0);
            lblComTitle.Name = "lblComTitle";
            lblComTitle.Size = new Size(134, 48);
            lblComTitle.TabIndex = 0;
            lblComTitle.Text = "通讯端口";
            lblComTitle.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblComValue
            // 
            lblComValue.Dock = DockStyle.Fill;
            lblComValue.Location = new Point(143, 0);
            lblComValue.Name = "lblComValue";
            lblComValue.Size = new Size(473, 48);
            lblComValue.TabIndex = 1;
            lblComValue.Text = "COM1 / 未连接";
            lblComValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblBaudTitle
            // 
            lblBaudTitle.Dock = DockStyle.Fill;
            lblBaudTitle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblBaudTitle.Location = new Point(622, 0);
            lblBaudTitle.Name = "lblBaudTitle";
            lblBaudTitle.Size = new Size(134, 48);
            lblBaudTitle.TabIndex = 2;
            lblBaudTitle.Text = "波特率";
            lblBaudTitle.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblBaudValue
            // 
            lblBaudValue.Dock = DockStyle.Fill;
            lblBaudValue.Location = new Point(762, 0);
            lblBaudValue.Name = "lblBaudValue";
            lblBaudValue.Size = new Size(473, 48);
            lblBaudValue.TabIndex = 3;
            lblBaudValue.Text = "2400 / 9600";
            lblBaudValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblMeterTitle
            // 
            lblMeterTitle.Dock = DockStyle.Fill;
            lblMeterTitle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblMeterTitle.Location = new Point(3, 48);
            lblMeterTitle.Name = "lblMeterTitle";
            lblMeterTitle.Size = new Size(134, 52);
            lblMeterTitle.TabIndex = 4;
            lblMeterTitle.Text = "电表地址";
            lblMeterTitle.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblMeterValue
            // 
            lblMeterValue.Dock = DockStyle.Fill;
            lblMeterValue.Location = new Point(143, 48);
            lblMeterValue.Name = "lblMeterValue";
            lblMeterValue.Size = new Size(473, 52);
            lblMeterValue.TabIndex = 5;
            lblMeterValue.Text = "000000000000";
            lblMeterValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDeviceTitle
            // 
            lblDeviceTitle.Dock = DockStyle.Fill;
            lblDeviceTitle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            lblDeviceTitle.Location = new Point(622, 48);
            lblDeviceTitle.Name = "lblDeviceTitle";
            lblDeviceTitle.Size = new Size(134, 52);
            lblDeviceTitle.TabIndex = 6;
            lblDeviceTitle.Text = "硬件状态";
            lblDeviceTitle.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblDeviceValue
            // 
            lblDeviceValue.Dock = DockStyle.Fill;
            lblDeviceValue.ForeColor = Color.FromArgb(22, 101, 52);
            lblDeviceValue.Location = new Point(762, 48);
            lblDeviceValue.Name = "lblDeviceValue";
            lblDeviceValue.Size = new Size(473, 52);
            lblDeviceValue.TabIndex = 7;
            lblDeviceValue.Text = "待检测";
            lblDeviceValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // MeterTest
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(232, 239, 236);
            ClientSize = new Size(1280, 760);
            Controls.Add(mainLayout);
            MaximizeBox = false;
            MinimizeBox = false;
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
            ((System.ComponentModel.ISupportInitialize)schemeGrid).EndInit();
            groupProcess.ResumeLayout(false);
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
        private DataGridView schemeGrid;
        private DataGridViewTextBoxColumn colSchemeNo;
        private DataGridViewTextBoxColumn colSchemeName;
        private DataGridViewTextBoxColumn colSchemeStatus;
        private GroupBox groupProcess;
        private DataGridView processGrid;
        private DataGridViewTextBoxColumn colProcessNo;
        private DataGridViewTextBoxColumn colProcessItem;
        private DataGridViewTextBoxColumn colProcessResult;
        private DataGridViewTextBoxColumn colProcessTime;
        private GroupBox groupHardware;
        private TableLayoutPanel hardwareLayout;
        private Label lblComTitle;
        private Label lblComValue;
        private Label lblBaudTitle;
        private Label lblBaudValue;
        private Label lblMeterTitle;
        private Label lblMeterValue;
        private Label lblDeviceTitle;
        private Label lblDeviceValue;
    }
}
