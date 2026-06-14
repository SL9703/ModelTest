namespace ModelTest
{
    partial class LinuxCommandForm
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
            splitContainerMain = new SplitContainer();
            groupBoxCategory = new GroupBox();
            lstCategory = new ListBox();
            tableLayoutPanelRight = new TableLayoutPanel();
            groupBoxSearch = new GroupBox();
            txtKeyword = new TextBox();
            lblKeyword = new Label();
            groupBoxCommands = new GroupBox();
            dgvCommands = new DataGridView();
            colCategory = new DataGridViewTextBoxColumn();
            colCommand = new DataGridViewTextBoxColumn();
            colDescription = new DataGridViewTextBoxColumn();
            colRisk = new DataGridViewTextBoxColumn();
            groupBoxDetail = new GroupBox();
            txtDetail = new TextBox();
            panelBottom = new Panel();
            lblStatus = new Label();
            btnCopy = new Button();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            groupBoxCategory.SuspendLayout();
            tableLayoutPanelRight.SuspendLayout();
            groupBoxSearch.SuspendLayout();
            groupBoxCommands.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvCommands).BeginInit();
            groupBoxDetail.SuspendLayout();
            panelBottom.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.FixedPanel = FixedPanel.Panel1;
            splitContainerMain.Location = new Point(0, 0);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(groupBoxCategory);
            splitContainerMain.Panel1.Padding = new Padding(12);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(tableLayoutPanelRight);
            splitContainerMain.Panel2.Padding = new Padding(0, 12, 12, 12);
            splitContainerMain.Size = new Size(1920, 1080);
            splitContainerMain.SplitterDistance = 220;
            splitContainerMain.TabIndex = 0;
            // 
            // groupBoxCategory
            // 
            groupBoxCategory.Controls.Add(lstCategory);
            groupBoxCategory.Dock = DockStyle.Fill;
            groupBoxCategory.Location = new Point(12, 12);
            groupBoxCategory.Name = "groupBoxCategory";
            groupBoxCategory.Size = new Size(196, 1056);
            groupBoxCategory.TabIndex = 0;
            groupBoxCategory.TabStop = false;
            groupBoxCategory.Text = "命令分类";
            // 
            // lstCategory
            // 
            lstCategory.Dock = DockStyle.Fill;
            lstCategory.FormattingEnabled = true;
            lstCategory.ItemHeight = 28;
            lstCategory.Location = new Point(3, 30);
            lstCategory.Name = "lstCategory";
            lstCategory.Size = new Size(190, 1023);
            lstCategory.TabIndex = 0;
            lstCategory.SelectedIndexChanged += lstCategory_SelectedIndexChanged;
            // 
            // tableLayoutPanelRight
            // 
            tableLayoutPanelRight.ColumnCount = 1;
            tableLayoutPanelRight.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelRight.Controls.Add(groupBoxSearch, 0, 0);
            tableLayoutPanelRight.Controls.Add(groupBoxCommands, 0, 1);
            tableLayoutPanelRight.Controls.Add(groupBoxDetail, 0, 2);
            tableLayoutPanelRight.Controls.Add(panelBottom, 0, 3);
            tableLayoutPanelRight.Dock = DockStyle.Fill;
            tableLayoutPanelRight.Location = new Point(0, 12);
            tableLayoutPanelRight.Name = "tableLayoutPanelRight";
            tableLayoutPanelRight.RowCount = 4;
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 86F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
            tableLayoutPanelRight.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            tableLayoutPanelRight.Size = new Size(1684, 1056);
            tableLayoutPanelRight.TabIndex = 0;
            // 
            // groupBoxSearch
            // 
            groupBoxSearch.Controls.Add(txtKeyword);
            groupBoxSearch.Controls.Add(lblKeyword);
            groupBoxSearch.Dock = DockStyle.Fill;
            groupBoxSearch.Location = new Point(3, 3);
            groupBoxSearch.Name = "groupBoxSearch";
            groupBoxSearch.Size = new Size(1678, 80);
            groupBoxSearch.TabIndex = 0;
            groupBoxSearch.TabStop = false;
            groupBoxSearch.Text = "搜索";
            // 
            // txtKeyword
            // 
            txtKeyword.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtKeyword.Location = new Point(118, 31);
            txtKeyword.Name = "txtKeyword";
            txtKeyword.Size = new Size(1529, 34);
            txtKeyword.TabIndex = 1;
            txtKeyword.TextChanged += txtKeyword_TextChanged;
            // 
            // lblKeyword
            // 
            lblKeyword.AutoSize = true;
            lblKeyword.Location = new Point(24, 34);
            lblKeyword.Name = "lblKeyword";
            lblKeyword.Size = new Size(86, 28);
            lblKeyword.TabIndex = 0;
            lblKeyword.Text = "关键字:";
            // 
            // groupBoxCommands
            // 
            groupBoxCommands.Controls.Add(dgvCommands);
            groupBoxCommands.Dock = DockStyle.Fill;
            groupBoxCommands.Location = new Point(3, 89);
            groupBoxCommands.Name = "groupBoxCommands";
            groupBoxCommands.Size = new Size(1678, 738);
            groupBoxCommands.TabIndex = 1;
            groupBoxCommands.TabStop = false;
            groupBoxCommands.Text = "Linux命令用例";
            // 
            // dgvCommands
            // 
            dgvCommands.AllowUserToAddRows = false;
            dgvCommands.AllowUserToDeleteRows = false;
            dgvCommands.AllowUserToResizeRows = false;
            dgvCommands.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvCommands.BackgroundColor = SystemColors.Window;
            dgvCommands.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvCommands.Columns.AddRange(new DataGridViewColumn[] { colCategory, colCommand, colDescription, colRisk });
            dgvCommands.Dock = DockStyle.Fill;
            dgvCommands.Location = new Point(3, 30);
            dgvCommands.MultiSelect = false;
            dgvCommands.Name = "dgvCommands";
            dgvCommands.ReadOnly = true;
            dgvCommands.RowHeadersVisible = false;
            dgvCommands.RowHeadersWidth = 72;
            dgvCommands.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvCommands.Size = new Size(1672, 705);
            dgvCommands.TabIndex = 0;
            dgvCommands.CellDoubleClick += dgvCommands_CellDoubleClick;
            dgvCommands.SelectionChanged += dgvCommands_SelectionChanged;
            // 
            // colCategory
            // 
            colCategory.DataPropertyName = "Category";
            colCategory.HeaderText = "分类";
            colCategory.MinimumWidth = 9;
            colCategory.Name = "colCategory";
            colCategory.ReadOnly = true;
            colCategory.Width = 130;
            // 
            // colCommand
            // 
            colCommand.DataPropertyName = "Command";
            colCommand.HeaderText = "命令";
            colCommand.MinimumWidth = 9;
            colCommand.Name = "colCommand";
            colCommand.ReadOnly = true;
            colCommand.Width = 300;
            // 
            // colDescription
            // 
            colDescription.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colDescription.DataPropertyName = "Description";
            colDescription.HeaderText = "详细描述";
            colDescription.MinimumWidth = 9;
            colDescription.Name = "colDescription";
            colDescription.ReadOnly = true;
            // 
            // colRisk
            // 
            colRisk.DataPropertyName = "Risk";
            colRisk.HeaderText = "风险";
            colRisk.MinimumWidth = 9;
            colRisk.Name = "colRisk";
            colRisk.ReadOnly = true;
            colRisk.Width = 90;
            // 
            // groupBoxDetail
            // 
            groupBoxDetail.Controls.Add(txtDetail);
            groupBoxDetail.Dock = DockStyle.Fill;
            groupBoxDetail.Location = new Point(3, 553);
            groupBoxDetail.Name = "groupBoxDetail";
            groupBoxDetail.Size = new Size(1678, 164);
            groupBoxDetail.TabIndex = 2;
            groupBoxDetail.TabStop = false;
            groupBoxDetail.Text = "命令详情";
            // 
            // txtDetail
            // 
            txtDetail.Dock = DockStyle.Fill;
            txtDetail.Location = new Point(3, 30);
            txtDetail.Multiline = true;
            txtDetail.Name = "txtDetail";
            txtDetail.ReadOnly = true;
            txtDetail.ScrollBars = ScrollBars.Vertical;
            txtDetail.Size = new Size(1672, 131);
            txtDetail.TabIndex = 0;
            // 
            // panelBottom
            // 
            panelBottom.Controls.Add(lblStatus);
            panelBottom.Controls.Add(btnCopy);
            panelBottom.Dock = DockStyle.Fill;
            panelBottom.Location = new Point(3, 1003);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new Size(1678, 50);
            panelBottom.TabIndex = 3;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoEllipsis = true;
            lblStatus.Location = new Point(3, 12);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(1474, 28);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "双击命令行可复制。";
            // 
            // btnCopy
            // 
            btnCopy.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopy.Location = new Point(1513, 4);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(160, 42);
            btnCopy.TabIndex = 1;
            btnCopy.Text = "复制命令";
            btnCopy.UseVisualStyleBackColor = true;
            btnCopy.Click += btnCopy_Click;
            // 
            // LinuxCommandForm
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1920, 1080);
            Controls.Add(splitContainerMain);
            MinimumSize = new Size(1000, 650);
            Name = "LinuxCommandForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Linux命令大全";
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            groupBoxCategory.ResumeLayout(false);
            tableLayoutPanelRight.ResumeLayout(false);
            groupBoxSearch.ResumeLayout(false);
            groupBoxSearch.PerformLayout();
            groupBoxCommands.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvCommands).EndInit();
            groupBoxDetail.ResumeLayout(false);
            groupBoxDetail.PerformLayout();
            panelBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainerMain;
        private GroupBox groupBoxCategory;
        private ListBox lstCategory;
        private TableLayoutPanel tableLayoutPanelRight;
        private GroupBox groupBoxSearch;
        private TextBox txtKeyword;
        private Label lblKeyword;
        private GroupBox groupBoxCommands;
        private DataGridView dgvCommands;
        private DataGridViewTextBoxColumn colCategory;
        private DataGridViewTextBoxColumn colCommand;
        private DataGridViewTextBoxColumn colDescription;
        private DataGridViewTextBoxColumn colRisk;
        private GroupBox groupBoxDetail;
        private TextBox txtDetail;
        private Panel panelBottom;
        private Label lblStatus;
        private Button btnCopy;
    }
}
