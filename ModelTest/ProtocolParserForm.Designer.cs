namespace ModelTest
{
    partial class ProtocolParserForm
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
            tableLayoutPanelMain = new TableLayoutPanel();
            groupBoxInput = new GroupBox();
            tableLayoutPanelInput = new TableLayoutPanel();
            panelInputButtons = new Panel();
            btnPreviousFrame = new Button();
            btnPasteNew = new Button();
            btnNextFrame = new Button();
            lblProtocol = new Label();
            cmbProtocol = new ComboBox();
            btnOpenProtocol = new Button();
            txtProtocol = new TextBox();
            splitContainerResult = new SplitContainer();
            tableLayoutPanelLeft = new TableLayoutPanel();
            groupBoxFrames = new GroupBox();
            dgvFrames = new DataGridView();
            colFrameIndex = new DataGridViewTextBoxColumn();
            colFrameProtocol = new DataGridViewTextBoxColumn();
            colFrameCommand = new DataGridViewTextBoxColumn();
            colFrameLength = new DataGridViewTextBoxColumn();
            colFrameChecksum = new DataGridViewTextBoxColumn();
            colFrameStatus = new DataGridViewTextBoxColumn();
            groupBoxFields = new GroupBox();
            dgvFields = new DataGridView();
            colFieldName = new DataGridViewTextBoxColumn();
            colFieldValue = new DataGridViewTextBoxColumn();
            colFieldDescription = new DataGridViewTextBoxColumn();
            colFieldStatus = new DataGridViewTextBoxColumn();
            groupBoxResult = new GroupBox();
            txtResult = new TextBox();
            panelBottom = new Panel();
            lblStatus = new Label();
            btnClear = new Button();
            btnCopyResult = new Button();
            tableLayoutPanelMain.SuspendLayout();
            groupBoxInput.SuspendLayout();
            tableLayoutPanelInput.SuspendLayout();
            panelInputButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerResult).BeginInit();
            splitContainerResult.Panel1.SuspendLayout();
            splitContainerResult.Panel2.SuspendLayout();
            splitContainerResult.SuspendLayout();
            tableLayoutPanelLeft.SuspendLayout();
            groupBoxFrames.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFrames).BeginInit();
            groupBoxFields.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFields).BeginInit();
            groupBoxResult.SuspendLayout();
            panelBottom.SuspendLayout();
            SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.ColumnCount = 1;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Controls.Add(groupBoxInput, 0, 0);
            tableLayoutPanelMain.Controls.Add(splitContainerResult, 0, 1);
            tableLayoutPanelMain.Controls.Add(panelBottom, 0, 2);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 0);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.Padding = new Padding(12);
            tableLayoutPanelMain.RowCount = 3;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
            tableLayoutPanelMain.Size = new Size(1600, 900);
            tableLayoutPanelMain.TabIndex = 0;
            // 
            // groupBoxInput
            // 
            groupBoxInput.Controls.Add(tableLayoutPanelInput);
            groupBoxInput.Dock = DockStyle.Fill;
            groupBoxInput.Location = new Point(15, 15);
            groupBoxInput.Name = "groupBoxInput";
            groupBoxInput.Size = new Size(1570, 174);
            groupBoxInput.TabIndex = 0;
            groupBoxInput.TabStop = false;
            groupBoxInput.Text = "原始协议报文";
            // 
            // tableLayoutPanelInput
            // 
            tableLayoutPanelInput.ColumnCount = 1;
            tableLayoutPanelInput.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelInput.Controls.Add(panelInputButtons, 0, 0);
            tableLayoutPanelInput.Controls.Add(txtProtocol, 0, 1);
            tableLayoutPanelInput.Dock = DockStyle.Fill;
            tableLayoutPanelInput.Location = new Point(3, 30);
            tableLayoutPanelInput.Name = "tableLayoutPanelInput";
            tableLayoutPanelInput.RowCount = 2;
            tableLayoutPanelInput.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            tableLayoutPanelInput.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelInput.Size = new Size(1564, 141);
            tableLayoutPanelInput.TabIndex = 0;
            // 
            // panelInputButtons
            // 
            panelInputButtons.Controls.Add(btnPreviousFrame);
            panelInputButtons.Controls.Add(btnPasteNew);
            panelInputButtons.Controls.Add(btnNextFrame);
            panelInputButtons.Controls.Add(lblProtocol);
            panelInputButtons.Controls.Add(cmbProtocol);
            panelInputButtons.Controls.Add(btnOpenProtocol);
            panelInputButtons.Dock = DockStyle.Fill;
            panelInputButtons.Location = new Point(3, 3);
            panelInputButtons.Name = "panelInputButtons";
            panelInputButtons.Size = new Size(1558, 42);
            panelInputButtons.TabIndex = 0;
            panelInputButtons.SizeChanged += panelInputButtons_SizeChanged;
            // 
            // btnPreviousFrame
            // 
            btnPreviousFrame.Anchor = AnchorStyles.Top;
            btnPreviousFrame.Location = new Point(632, 0);
            btnPreviousFrame.Name = "btnPreviousFrame";
            btnPreviousFrame.Size = new Size(90, 38);
            btnPreviousFrame.TabIndex = 0;
            btnPreviousFrame.Text = "←";
            btnPreviousFrame.UseVisualStyleBackColor = true;
            btnPreviousFrame.Click += btnPreviousFrame_Click;
            // 
            // btnPasteNew
            // 
            btnPasteNew.Anchor = AnchorStyles.Top;
            btnPasteNew.Location = new Point(732, 0);
            btnPasteNew.Name = "btnPasteNew";
            btnPasteNew.Size = new Size(150, 38);
            btnPasteNew.TabIndex = 1;
            btnPasteNew.Text = "清空粘贴";
            btnPasteNew.UseVisualStyleBackColor = true;
            btnPasteNew.Click += btnPasteNew_Click;
            // 
            // btnNextFrame
            // 
            btnNextFrame.Anchor = AnchorStyles.Top;
            btnNextFrame.Location = new Point(892, 0);
            btnNextFrame.Name = "btnNextFrame";
            btnNextFrame.Size = new Size(90, 38);
            btnNextFrame.TabIndex = 2;
            btnNextFrame.Text = "→";
            btnNextFrame.UseVisualStyleBackColor = true;
            btnNextFrame.Click += btnNextFrame_Click;
            // 
            // lblProtocol
            // 
            lblProtocol.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblProtocol.AutoSize = true;
            lblProtocol.Location = new Point(1046, 6);
            lblProtocol.Name = "lblProtocol";
            lblProtocol.Size = new Size(62, 28);
            lblProtocol.TabIndex = 3;
            lblProtocol.Text = "协议:";
            // 
            // cmbProtocol
            // 
            cmbProtocol.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cmbProtocol.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbProtocol.FormattingEnabled = true;
            cmbProtocol.Items.AddRange(new object[] { "终端检测装置电测协议V1", "电表检测装置硬件通信协议V1" });
            cmbProtocol.Location = new Point(1114, 3);
            cmbProtocol.Name = "cmbProtocol";
            cmbProtocol.Size = new Size(320, 36);
            cmbProtocol.TabIndex = 4;
            cmbProtocol.SelectedIndexChanged += cmbProtocol_SelectedIndexChanged;
            // 
            // btnOpenProtocol
            // 
            btnOpenProtocol.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenProtocol.Location = new Point(1445, 0);
            btnOpenProtocol.Name = "btnOpenProtocol";
            btnOpenProtocol.Size = new Size(110, 38);
            btnOpenProtocol.TabIndex = 5;
            btnOpenProtocol.Text = "打开";
            btnOpenProtocol.UseVisualStyleBackColor = true;
            btnOpenProtocol.Click += btnOpenProtocol_Click;
            // 
            // txtProtocol
            // 
            txtProtocol.Dock = DockStyle.Fill;
            txtProtocol.Location = new Point(3, 51);
            txtProtocol.Multiline = true;
            txtProtocol.Name = "txtProtocol";
            txtProtocol.ScrollBars = ScrollBars.Both;
            txtProtocol.Size = new Size(1558, 87);
            txtProtocol.TabIndex = 1;
            txtProtocol.TextChanged += txtProtocol_TextChanged;
            // 
            // splitContainerResult
            // 
            splitContainerResult.Dock = DockStyle.Fill;
            splitContainerResult.Location = new Point(15, 195);
            splitContainerResult.Name = "splitContainerResult";
            // 
            // splitContainerResult.Panel1
            // 
            splitContainerResult.Panel1.Controls.Add(tableLayoutPanelLeft);
            // 
            // splitContainerResult.Panel2
            // 
            splitContainerResult.Panel2.Controls.Add(groupBoxResult);
            splitContainerResult.Size = new Size(1570, 634);
            splitContainerResult.SplitterDistance = 980;
            splitContainerResult.TabIndex = 1;
            // 
            // tableLayoutPanelLeft
            // 
            tableLayoutPanelLeft.ColumnCount = 1;
            tableLayoutPanelLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelLeft.Controls.Add(groupBoxFrames, 0, 0);
            tableLayoutPanelLeft.Controls.Add(groupBoxFields, 0, 1);
            tableLayoutPanelLeft.Dock = DockStyle.Fill;
            tableLayoutPanelLeft.Location = new Point(0, 0);
            tableLayoutPanelLeft.Name = "tableLayoutPanelLeft";
            tableLayoutPanelLeft.RowCount = 2;
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Absolute, 180F));
            tableLayoutPanelLeft.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelLeft.Size = new Size(980, 634);
            tableLayoutPanelLeft.TabIndex = 0;
            // 
            // groupBoxFrames
            // 
            groupBoxFrames.Controls.Add(dgvFrames);
            groupBoxFrames.Dock = DockStyle.Fill;
            groupBoxFrames.Location = new Point(3, 3);
            groupBoxFrames.Name = "groupBoxFrames";
            groupBoxFrames.Size = new Size(974, 174);
            groupBoxFrames.TabIndex = 0;
            groupBoxFrames.TabStop = false;
            groupBoxFrames.Text = "报文帧";
            // 
            // dgvFrames
            // 
            dgvFrames.AllowUserToAddRows = false;
            dgvFrames.AllowUserToDeleteRows = false;
            dgvFrames.AllowUserToResizeRows = false;
            dgvFrames.BackgroundColor = SystemColors.Window;
            dgvFrames.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvFrames.Columns.AddRange(new DataGridViewColumn[] { colFrameIndex, colFrameProtocol, colFrameCommand, colFrameLength, colFrameChecksum, colFrameStatus });
            dgvFrames.Dock = DockStyle.Fill;
            dgvFrames.Location = new Point(3, 30);
            dgvFrames.MultiSelect = false;
            dgvFrames.Name = "dgvFrames";
            dgvFrames.ReadOnly = true;
            dgvFrames.RowHeadersVisible = false;
            dgvFrames.RowHeadersWidth = 72;
            dgvFrames.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFrames.Size = new Size(968, 141);
            dgvFrames.TabIndex = 0;
            dgvFrames.SelectionChanged += dgvFrames_SelectionChanged;
            // 
            // colFrameIndex
            // 
            colFrameIndex.DataPropertyName = "Index";
            colFrameIndex.HeaderText = "序号";
            colFrameIndex.MinimumWidth = 9;
            colFrameIndex.Name = "colFrameIndex";
            colFrameIndex.ReadOnly = true;
            colFrameIndex.Width = 70;
            // 
            // colFrameProtocol
            // 
            colFrameProtocol.DataPropertyName = "ProtocolType";
            colFrameProtocol.HeaderText = "协议类型";
            colFrameProtocol.MinimumWidth = 9;
            colFrameProtocol.Name = "colFrameProtocol";
            colFrameProtocol.ReadOnly = true;
            colFrameProtocol.Width = 160;
            // 
            // colFrameCommand
            // 
            colFrameCommand.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colFrameCommand.DataPropertyName = "Command";
            colFrameCommand.HeaderText = "命令";
            colFrameCommand.MinimumWidth = 9;
            colFrameCommand.Name = "colFrameCommand";
            colFrameCommand.ReadOnly = true;
            // 
            // colFrameLength
            // 
            colFrameLength.DataPropertyName = "LengthStatus";
            colFrameLength.HeaderText = "长度";
            colFrameLength.MinimumWidth = 9;
            colFrameLength.Name = "colFrameLength";
            colFrameLength.ReadOnly = true;
            colFrameLength.Width = 90;
            // 
            // colFrameChecksum
            // 
            colFrameChecksum.DataPropertyName = "ChecksumStatus";
            colFrameChecksum.HeaderText = "校验";
            colFrameChecksum.MinimumWidth = 9;
            colFrameChecksum.Name = "colFrameChecksum";
            colFrameChecksum.ReadOnly = true;
            colFrameChecksum.Width = 140;
            // 
            // colFrameStatus
            // 
            colFrameStatus.DataPropertyName = "Status";
            colFrameStatus.HeaderText = "状态";
            colFrameStatus.MinimumWidth = 9;
            colFrameStatus.Name = "colFrameStatus";
            colFrameStatus.ReadOnly = true;
            colFrameStatus.Width = 90;
            // 
            // groupBoxFields
            // 
            groupBoxFields.Controls.Add(dgvFields);
            groupBoxFields.Dock = DockStyle.Fill;
            groupBoxFields.Location = new Point(3, 183);
            groupBoxFields.Name = "groupBoxFields";
            groupBoxFields.Size = new Size(974, 448);
            groupBoxFields.TabIndex = 1;
            groupBoxFields.TabStop = false;
            groupBoxFields.Text = "字段解析";
            // 
            // dgvFields
            // 
            dgvFields.AllowUserToAddRows = false;
            dgvFields.AllowUserToDeleteRows = false;
            dgvFields.AllowUserToResizeRows = false;
            dgvFields.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgvFields.BackgroundColor = SystemColors.Window;
            dgvFields.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvFields.Columns.AddRange(new DataGridViewColumn[] { colFieldName, colFieldValue, colFieldDescription, colFieldStatus });
            dgvFields.Dock = DockStyle.Fill;
            dgvFields.Font = new Font("Consolas", 10F);
            dgvFields.Location = new Point(3, 30);
            dgvFields.MultiSelect = false;
            dgvFields.Name = "dgvFields";
            dgvFields.ReadOnly = true;
            dgvFields.RowHeadersVisible = false;
            dgvFields.RowHeadersWidth = 72;
            dgvFields.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFields.Size = new Size(968, 415);
            dgvFields.TabIndex = 0;
            // 
            // colFieldName
            // 
            colFieldName.DataPropertyName = "Name";
            colFieldName.HeaderText = "字段";
            colFieldName.MinimumWidth = 9;
            colFieldName.Name = "colFieldName";
            colFieldName.ReadOnly = true;
            colFieldName.Width = 260;
            // 
            // colFieldValue
            // 
            colFieldValue.DataPropertyName = "Value";
            colFieldValue.HeaderText = "值";
            colFieldValue.MinimumWidth = 9;
            colFieldValue.Name = "colFieldValue";
            colFieldValue.ReadOnly = true;
            colFieldValue.Width = 170;
            // 
            // colFieldDescription
            // 
            colFieldDescription.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colFieldDescription.DataPropertyName = "Description";
            colFieldDescription.HeaderText = "说明";
            colFieldDescription.MinimumWidth = 9;
            colFieldDescription.Name = "colFieldDescription";
            colFieldDescription.ReadOnly = true;
            // 
            // colFieldStatus
            // 
            colFieldStatus.DataPropertyName = "Status";
            colFieldStatus.HeaderText = "状态";
            colFieldStatus.MinimumWidth = 9;
            colFieldStatus.Name = "colFieldStatus";
            colFieldStatus.ReadOnly = true;
            colFieldStatus.Width = 90;
            // 
            // groupBoxResult
            // 
            groupBoxResult.Controls.Add(txtResult);
            groupBoxResult.Dock = DockStyle.Fill;
            groupBoxResult.Location = new Point(0, 0);
            groupBoxResult.Name = "groupBoxResult";
            groupBoxResult.Size = new Size(586, 634);
            groupBoxResult.TabIndex = 0;
            groupBoxResult.TabStop = false;
            groupBoxResult.Text = "详细解析";
            // 
            // txtResult
            // 
            txtResult.Dock = DockStyle.Fill;
            txtResult.Location = new Point(3, 30);
            txtResult.Multiline = true;
            txtResult.Name = "txtResult";
            txtResult.ReadOnly = true;
            txtResult.ScrollBars = ScrollBars.Both;
            txtResult.Size = new Size(580, 601);
            txtResult.TabIndex = 0;
            // 
            // panelBottom
            // 
            panelBottom.Controls.Add(lblStatus);
            panelBottom.Controls.Add(btnClear);
            panelBottom.Controls.Add(btnCopyResult);
            panelBottom.Dock = DockStyle.Fill;
            panelBottom.Location = new Point(15, 735);
            panelBottom.Name = "panelBottom";
            panelBottom.Size = new Size(1570, 50);
            panelBottom.TabIndex = 2;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoEllipsis = true;
            lblStatus.Location = new Point(3, 12);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(1219, 28);
            lblStatus.TabIndex = 0;
            lblStatus.Text = "粘贴报文后自动解析。";
            // 
            // btnClear
            // 
            btnClear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClear.Location = new Point(1254, 4);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(140, 42);
            btnClear.TabIndex = 1;
            btnClear.Text = "清空";
            btnClear.UseVisualStyleBackColor = true;
            btnClear.Click += btnClear_Click;
            // 
            // btnCopyResult
            // 
            btnCopyResult.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopyResult.Location = new Point(1418, 4);
            btnCopyResult.Name = "btnCopyResult";
            btnCopyResult.Size = new Size(149, 42);
            btnCopyResult.TabIndex = 2;
            btnCopyResult.Text = "复制结果";
            btnCopyResult.UseVisualStyleBackColor = true;
            btnCopyResult.Click += btnCopyResult_Click;
            // 
            // ProtocolParserForm
            // 
            AutoScaleDimensions = new SizeF(13F, 28F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1920, 1080);
            Controls.Add(tableLayoutPanelMain);
            MinimumSize = new Size(1300, 760);
            Name = "ProtocolParserForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "报文解析工具";
            tableLayoutPanelMain.ResumeLayout(false);
            groupBoxInput.ResumeLayout(false);
            tableLayoutPanelInput.ResumeLayout(false);
            tableLayoutPanelInput.PerformLayout();
            panelInputButtons.ResumeLayout(false);
            splitContainerResult.Panel1.ResumeLayout(false);
            splitContainerResult.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerResult).EndInit();
            splitContainerResult.ResumeLayout(false);
            tableLayoutPanelLeft.ResumeLayout(false);
            groupBoxFrames.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvFrames).EndInit();
            groupBoxFields.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvFields).EndInit();
            groupBoxResult.ResumeLayout(false);
            groupBoxResult.PerformLayout();
            panelBottom.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tableLayoutPanelMain;
        private GroupBox groupBoxInput;
        private TableLayoutPanel tableLayoutPanelInput;
        private Panel panelInputButtons;
        private Button btnPreviousFrame;
        private Button btnPasteNew;
        private Button btnNextFrame;
        private Label lblProtocol;
        private ComboBox cmbProtocol;
        private Button btnOpenProtocol;
        private TextBox txtProtocol;
        private SplitContainer splitContainerResult;
        private TableLayoutPanel tableLayoutPanelLeft;
        private GroupBox groupBoxFrames;
        private DataGridView dgvFrames;
        private DataGridViewTextBoxColumn colFrameIndex;
        private DataGridViewTextBoxColumn colFrameProtocol;
        private DataGridViewTextBoxColumn colFrameCommand;
        private DataGridViewTextBoxColumn colFrameLength;
        private DataGridViewTextBoxColumn colFrameChecksum;
        private DataGridViewTextBoxColumn colFrameStatus;
        private GroupBox groupBoxFields;
        private DataGridView dgvFields;
        private DataGridViewTextBoxColumn colFieldName;
        private DataGridViewTextBoxColumn colFieldValue;
        private DataGridViewTextBoxColumn colFieldDescription;
        private DataGridViewTextBoxColumn colFieldStatus;
        private GroupBox groupBoxResult;
        private TextBox txtResult;
        private Panel panelBottom;
        private Label lblStatus;
        private Button btnClear;
        private Button btnCopyResult;
    }
}
