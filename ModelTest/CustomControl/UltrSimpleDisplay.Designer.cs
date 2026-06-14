namespace ModelTest.CustomControl
{
    partial class UltrSimpleDisplay
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
            errorPanel = new Panel();
            errorLayout = new TableLayoutPanel();
            labelErrorClass = new Label();
            cbxErrorTextClass = new ComboBox();
            labelErrorMode = new Label();
            cbxErrorTest = new ComboBox();
            labelVoltage = new Label();
            tbxVoltage = new TextBox();
            labelCurrent = new Label();
            tbxCurrent = new TextBox();
            labelStandardConstant = new Label();
            tbxBZBC = new TextBox();
            labelMeterConstant = new Label();
            tbxDNBC = new TextBox();
            labelClockCircle = new Label();
            tbxRJSC = new TextBox();
            labelOperation = new Label();
            operationPanel = new FlowLayoutPanel();
            btnStartErrorTerminal = new Button();
            btnStopErrorTerminal = new Button();
            errorPanel.SuspendLayout();
            errorLayout.SuspendLayout();
            operationPanel.SuspendLayout();
            SuspendLayout();
            // 
            // errorPanel
            // 
            errorPanel.BackColor = Color.FromArgb(88, 149, 127);
            errorPanel.Controls.Add(errorLayout);
            errorPanel.Dock = DockStyle.Bottom;
            errorPanel.Location = new Point(12, 218);
            errorPanel.Name = "errorPanel";
            errorPanel.Padding = new Padding(8, 5, 8, 7);
            errorPanel.Size = new Size(836, 122);
            errorPanel.TabIndex = 0;
            // 
            // errorLayout
            // 
            errorLayout.ColumnCount = 4;
            errorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            errorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            errorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            errorLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            errorLayout.Controls.Add(labelErrorClass, 0, 0);
            errorLayout.Controls.Add(cbxErrorTextClass, 0, 1);
            errorLayout.Controls.Add(labelErrorMode, 1, 0);
            errorLayout.Controls.Add(cbxErrorTest, 1, 1);
            errorLayout.Controls.Add(labelVoltage, 2, 0);
            errorLayout.Controls.Add(tbxVoltage, 2, 1);
            errorLayout.Controls.Add(labelCurrent, 3, 0);
            errorLayout.Controls.Add(tbxCurrent, 3, 1);
            errorLayout.Controls.Add(labelStandardConstant, 0, 2);
            errorLayout.Controls.Add(tbxBZBC, 0, 3);
            errorLayout.Controls.Add(labelMeterConstant, 1, 2);
            errorLayout.Controls.Add(tbxDNBC, 1, 3);
            errorLayout.Controls.Add(labelClockCircle, 2, 2);
            errorLayout.Controls.Add(tbxRJSC, 2, 3);
            errorLayout.Controls.Add(labelOperation, 3, 2);
            errorLayout.Controls.Add(operationPanel, 3, 3);
            errorLayout.Dock = DockStyle.Fill;
            errorLayout.Location = new Point(8, 5);
            errorLayout.Name = "errorLayout";
            errorLayout.RowCount = 4;
            errorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            errorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            errorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
            errorLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            errorLayout.Size = new Size(820, 110);
            errorLayout.TabIndex = 0;
            // 
            // labelErrorClass
            // 
            labelErrorClass.Anchor = AnchorStyles.Left;
            labelErrorClass.AutoSize = true;
            labelErrorClass.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelErrorClass.ForeColor = Color.Red;
            labelErrorClass.Location = new Point(8, 0);
            labelErrorClass.Margin = new Padding(8, 0, 4, 0);
            labelErrorClass.Name = "labelErrorClass";
            labelErrorClass.Size = new Size(96, 23);
            labelErrorClass.TabIndex = 0;
            labelErrorClass.Text = "实验类型";
            // 
            // cbxErrorTextClass
            // 
            cbxErrorTextClass.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxErrorTextClass.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxErrorTextClass.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            cbxErrorTextClass.FormattingEnabled = true;
            cbxErrorTextClass.Items.AddRange(new object[] { "有功", "无功", "日记时" });
            cbxErrorTextClass.Location = new Point(8, 26);
            cbxErrorTextClass.Margin = new Padding(8, 3, 10, 2);
            cbxErrorTextClass.Name = "cbxErrorTextClass";
            cbxErrorTextClass.Size = new Size(187, 38);
            cbxErrorTextClass.TabIndex = 1;
            // 
            // labelErrorMode
            // 
            labelErrorMode.Anchor = AnchorStyles.Left;
            labelErrorMode.AutoSize = true;
            labelErrorMode.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelErrorMode.ForeColor = Color.Red;
            labelErrorMode.Location = new Point(213, 0);
            labelErrorMode.Margin = new Padding(8, 0, 4, 0);
            labelErrorMode.Name = "labelErrorMode";
            labelErrorMode.Size = new Size(96, 23);
            labelErrorMode.TabIndex = 2;
            labelErrorMode.Text = "实验方式";
            // 
            // cbxErrorTest
            // 
            cbxErrorTest.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cbxErrorTest.DropDownStyle = ComboBoxStyle.DropDownList;
            cbxErrorTest.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            cbxErrorTest.FormattingEnabled = true;
            cbxErrorTest.Items.AddRange(new object[] { "电脉冲", "蓝牙脉冲", "光脉冲" });
            cbxErrorTest.Location = new Point(213, 26);
            cbxErrorTest.Margin = new Padding(8, 3, 10, 2);
            cbxErrorTest.Name = "cbxErrorTest";
            cbxErrorTest.Size = new Size(187, 38);
            cbxErrorTest.TabIndex = 3;
            // 
            // labelVoltage
            // 
            labelVoltage.Anchor = AnchorStyles.Left;
            labelVoltage.AutoSize = true;
            labelVoltage.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelVoltage.ForeColor = Color.Red;
            labelVoltage.Location = new Point(418, 0);
            labelVoltage.Margin = new Padding(8, 0, 4, 0);
            labelVoltage.Name = "labelVoltage";
            labelVoltage.Size = new Size(82, 23);
            labelVoltage.TabIndex = 4;
            labelVoltage.Text = "电压(V)";
            // 
            // tbxVoltage
            // 
            tbxVoltage.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxVoltage.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            tbxVoltage.Location = new Point(418, 26);
            tbxVoltage.Margin = new Padding(8, 3, 10, 2);
            tbxVoltage.Name = "tbxVoltage";
            tbxVoltage.Size = new Size(187, 36);
            tbxVoltage.TabIndex = 5;
            tbxVoltage.TextChanged += VoltageOrCurrent_TextChanged;
            // 
            // labelCurrent
            // 
            labelCurrent.Anchor = AnchorStyles.Left;
            labelCurrent.AutoSize = true;
            labelCurrent.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelCurrent.ForeColor = Color.Red;
            labelCurrent.Location = new Point(623, 0);
            labelCurrent.Margin = new Padding(8, 0, 4, 0);
            labelCurrent.Name = "labelCurrent";
            labelCurrent.Size = new Size(83, 23);
            labelCurrent.TabIndex = 6;
            labelCurrent.Text = "电流(A)";
            // 
            // tbxCurrent
            // 
            tbxCurrent.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxCurrent.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            tbxCurrent.Location = new Point(623, 26);
            tbxCurrent.Margin = new Padding(8, 3, 10, 2);
            tbxCurrent.Name = "tbxCurrent";
            tbxCurrent.Size = new Size(187, 36);
            tbxCurrent.TabIndex = 7;
            tbxCurrent.TextChanged += VoltageOrCurrent_TextChanged;
            // 
            // labelStandardConstant
            // 
            labelStandardConstant.Anchor = AnchorStyles.Left;
            labelStandardConstant.AutoSize = true;
            labelStandardConstant.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelStandardConstant.ForeColor = Color.Red;
            labelStandardConstant.Location = new Point(8, 55);
            labelStandardConstant.Margin = new Padding(8, 0, 4, 0);
            labelStandardConstant.Name = "labelStandardConstant";
            labelStandardConstant.Size = new Size(117, 23);
            labelStandardConstant.TabIndex = 8;
            labelStandardConstant.Text = "标准表常数";
            // 
            // tbxBZBC
            // 
            tbxBZBC.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxBZBC.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            tbxBZBC.Location = new Point(8, 81);
            tbxBZBC.Margin = new Padding(8, 3, 10, 2);
            tbxBZBC.Name = "tbxBZBC";
            tbxBZBC.Size = new Size(187, 36);
            tbxBZBC.TabIndex = 9;
            // 
            // labelMeterConstant
            // 
            labelMeterConstant.Anchor = AnchorStyles.Left;
            labelMeterConstant.AutoSize = true;
            labelMeterConstant.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelMeterConstant.ForeColor = Color.Red;
            labelMeterConstant.Location = new Point(213, 55);
            labelMeterConstant.Margin = new Padding(8, 0, 4, 0);
            labelMeterConstant.Name = "labelMeterConstant";
            labelMeterConstant.Size = new Size(117, 23);
            labelMeterConstant.TabIndex = 10;
            labelMeterConstant.Text = "电能表常数";
            // 
            // tbxDNBC
            // 
            tbxDNBC.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxDNBC.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            tbxDNBC.Location = new Point(213, 81);
            tbxDNBC.Margin = new Padding(8, 3, 10, 2);
            tbxDNBC.Name = "tbxDNBC";
            tbxDNBC.Size = new Size(187, 36);
            tbxDNBC.TabIndex = 11;
            // 
            // labelClockCircle
            // 
            labelClockCircle.Anchor = AnchorStyles.Left;
            labelClockCircle.AutoSize = true;
            labelClockCircle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelClockCircle.ForeColor = Color.Red;
            labelClockCircle.Location = new Point(418, 55);
            labelClockCircle.Margin = new Padding(8, 0, 4, 0);
            labelClockCircle.Name = "labelClockCircle";
            labelClockCircle.Size = new Size(54, 23);
            labelClockCircle.TabIndex = 12;
            labelClockCircle.Text = "圈数";
            // 
            // tbxRJSC
            // 
            tbxRJSC.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            tbxRJSC.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point, 134);
            tbxRJSC.Location = new Point(418, 81);
            tbxRJSC.Margin = new Padding(8, 3, 10, 2);
            tbxRJSC.Name = "tbxRJSC";
            tbxRJSC.Size = new Size(187, 36);
            tbxRJSC.TabIndex = 13;
            // 
            // labelOperation
            // 
            labelOperation.Anchor = AnchorStyles.Left;
            labelOperation.AutoSize = true;
            labelOperation.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelOperation.ForeColor = Color.Red;
            labelOperation.Location = new Point(623, 55);
            labelOperation.Margin = new Padding(8, 0, 4, 0);
            labelOperation.Name = "labelOperation";
            labelOperation.Size = new Size(54, 23);
            labelOperation.TabIndex = 14;
            labelOperation.Text = "操作";
            // 
            // operationPanel
            // 
            operationPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            operationPanel.Controls.Add(btnStartErrorTerminal);
            operationPanel.Controls.Add(btnStopErrorTerminal);
            operationPanel.Location = new Point(623, 81);
            operationPanel.Margin = new Padding(8, 3, 10, 2);
            operationPanel.Name = "operationPanel";
            operationPanel.Size = new Size(187, 27);
            operationPanel.TabIndex = 15;
            operationPanel.WrapContents = false;
            // 
            // btnStartErrorTerminal
            // 
            btnStartErrorTerminal.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            btnStartErrorTerminal.ForeColor = Color.Black;
            btnStartErrorTerminal.Location = new Point(0, 0);
            btnStartErrorTerminal.Margin = new Padding(0, 0, 6, 0);
            btnStartErrorTerminal.Name = "btnStartErrorTerminal";
            btnStartErrorTerminal.Size = new Size(88, 30);
            btnStartErrorTerminal.TabIndex = 0;
            btnStartErrorTerminal.Text = "开始试验";
            btnStartErrorTerminal.UseVisualStyleBackColor = true;
            btnStartErrorTerminal.Click += btnStartErrorTerminal_Click;
            // 
            // btnStopErrorTerminal
            // 
            btnStopErrorTerminal.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            btnStopErrorTerminal.ForeColor = Color.Black;
            btnStopErrorTerminal.Location = new Point(94, 0);
            btnStopErrorTerminal.Margin = new Padding(0);
            btnStopErrorTerminal.Name = "btnStopErrorTerminal";
            btnStopErrorTerminal.Size = new Size(88, 30);
            btnStopErrorTerminal.TabIndex = 1;
            btnStopErrorTerminal.Text = "停止试验";
            btnStopErrorTerminal.UseVisualStyleBackColor = true;
            btnStopErrorTerminal.Click += btnStopErrorTerminal_Click;
            // 
            // UltrSimpleDisplay
            // 
            AutoScaleDimensions = new SizeF(14F, 30F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            Controls.Add(errorPanel);
            DoubleBuffered = true;
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
            ForeColor = Color.White;
            MinimumSize = new Size(760, 350);
            Name = "UltrSimpleDisplay";
            Padding = new Padding(12, 8, 12, 10);
            Size = new Size(860, 350);
            Paint += simpleDisplay;
            Resize += UltrSimpleDisplay_Resize;
            errorPanel.ResumeLayout(false);
            errorLayout.ResumeLayout(false);
            errorLayout.PerformLayout();
            operationPanel.ResumeLayout(false);
            ResumeLayout(false);


        }

        #endregion

        private int digitCount = 12;
        private int digitSpacing = 6;
        private int defaultDigitWidth = 42;
        private int defaultDigitHeight = 82;
        private Color displayBackColor = Color.FromArgb(8, 10, 8);
        private Color onColor = Color.LimeGreen;
        private Color offColor = Color.FromArgb(32, 44, 32);
        private Color frameColor = Color.FromArgb(55, 80, 55);
        private Color mutedTextColor = Color.FromArgb(150, 170, 150);
        private Panel errorPanel;
        private TableLayoutPanel errorLayout;
        private Label labelErrorClass;
        private ComboBox cbxErrorTextClass;
        private Label labelErrorMode;
        private ComboBox cbxErrorTest;
        private Label labelVoltage;
        private TextBox tbxVoltage;
        private Label labelCurrent;
        private TextBox tbxCurrent;
        private Label labelStandardConstant;
        private TextBox tbxBZBC;
        private Label labelMeterConstant;
        private TextBox tbxDNBC;
        private Label labelClockCircle;
        private TextBox tbxRJSC;
        private Label labelOperation;
        private FlowLayoutPanel operationPanel;
        private Button btnStartErrorTerminal;
        private Button btnStopErrorTerminal;
    }
}
