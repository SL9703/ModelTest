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
            groupBox3 = new GroupBox();
            label4 = new Label();
            tbxMeterV1Addr = new TextBox();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
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
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(350, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(2238, 1024);
            panel1.TabIndex = 1;
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
    }
}
