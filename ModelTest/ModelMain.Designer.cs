
using System.IO.Ports;
using System.Threading.Tasks;

namespace ModelTest
{
    partial class ModelMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private async Task InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModelMain));
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            TestUnit = new GroupBox();
            CCOACDown = new Button();
            CCOACOn = new Button();
            CCODCDown = new Button();
            CCODCOn = new Button();
            label21 = new Label();
            label20 = new Label();
            label3 = new Label();
            checkBoxN = new CheckBox();
            checkBoxC = new CheckBox();
            checkBoxB = new CheckBox();
            checkBoxA = new CheckBox();
            checkBox2 = new CheckBox();
            checkBox1 = new CheckBox();
            btnPowerDown_AC = new Button();
            btnPowerOn_AC = new Button();
            label8 = new Label();
            tbxModelNumber = new TextBox();
            cbxTerminalCLASS = new ComboBox();
            tbx_addr = new TextBox();
            label7 = new Label();
            label6 = new Label();
            label5 = new Label();
            btnPowerDown_DC = new Button();
            btnPowerOn_DC = new Button();
            label4 = new Label();
            tabPage2 = new TabPage();
            panel16 = new Panel();
            SGCC698FF = new Button();
            buttonKZHLStatus = new Button();
            label10 = new Label();
            label13 = new Label();
            buttonKZHLID = new Button();
            label18 = new Label();
            CSG698FF = new Button();
            label19 = new Label();
            label11 = new Label();
            SGCC645FF = new Button();
            label9 = new Label();
            tabPage3 = new TabPage();
            tabControl2 = new TabControl();
            tabPage9 = new TabPage();
            panel4 = new Panel();
            tableLayoutPanel1 = new TableLayoutPanel();
            groupBox10 = new GroupBox();
            bttnReadSTAPinStatus = new Button();
            comboBoxSTAStutas = new ComboBox();
            bttnSTALPin = new Button();
            bttnSTAHPin = new Button();
            cbxSTAModePinStatus = new ComboBox();
            label109 = new Label();
            label108 = new Label();
            btnT1_ACCTRL = new Button();
            btnT1_DCCTRL = new Button();
            cbbxSTAModel = new ComboBox();
            label107 = new Label();
            groupBox11 = new GroupBox();
            panel15 = new Panel();
            btnelectriciansource = new Button();
            btnstandardSource = new Button();
            label123 = new Label();
            panel9 = new Panel();
            btn_changePCBDownAC = new Button();
            btn_changePCBUPAC = new Button();
            cbx_changePCBUPAC = new ComboBox();
            label22 = new Label();
            panel6 = new Panel();
            label110 = new Label();
            chexblx_LEDRGY = new CheckedListBox();
            button_SETLED1 = new Button();
            button_SETLED4 = new Button();
            button_SETLED2 = new Button();
            button_SETLED3 = new Button();
            panel5 = new Panel();
            groupBox5 = new GroupBox();
            label30 = new Label();
            label29 = new Label();
            label28 = new Label();
            label27 = new Label();
            pBTaiti_yellow = new PictureBox();
            pBTaiti_Green = new PictureBox();
            pBTaiti_Red = new PictureBox();
            groupBox2 = new GroupBox();
            pictureBoxRed = new PictureBox();
            label26 = new Label();
            label24 = new Label();
            pictureBoxGreen = new PictureBox();
            groupBox1 = new GroupBox();
            btnTerminalV1MotorCrimpingreturn = new Button();
            btnTerminalV1MotorCrimping = new Button();
            groupBox4 = new GroupBox();
            btnChangeTerminalClass = new Button();
            cbxTerminalV1 = new ComboBox();
            groupBox3 = new GroupBox();
            label106 = new Label();
            btnTerminalBW_ADown = new Button();
            btnTerminalBW_AOn = new Button();
            cbx_TerminalV1_UC = new CheckBox();
            cbx_TerminalV1_IA = new CheckBox();
            btnTerminalBW_VOn = new Button();
            cbx_TerminalV1_UB = new CheckBox();
            btnTerminalBW_VDown = new Button();
            cbx_TerminalV1_IB = new CheckBox();
            cbx_TerminalV1_UA = new CheckBox();
            cbx_TerminalV1_IC = new CheckBox();
            label25 = new Label();
            tbxTerminalAdds = new TextBox();
            cbx_TerminalV1_IN = new CheckBox();
            tabPage10 = new TabPage();
            tabPage4 = new TabPage();
            tabPage5 = new TabPage();
            tabPage6 = new TabPage();
            tabPage7 = new TabPage();
            label112 = new Label();
            btn_ReadTime = new Button();
            label111 = new Label();
            tbxXYMeterPulse = new TextBox();
            buttonRead_Pulset = new Button();
            groupBox9 = new GroupBox();
            cbx_HABC = new ComboBox();
            cbx_LC = new ComboBox();
            label94 = new Label();
            label93 = new Label();
            label92 = new Label();
            label91 = new Label();
            label90 = new Label();
            tbx_A_5 = new TextBox();
            tbx_V_5 = new TextBox();
            btn_XY_ADJ = new Button();
            groupBox8 = new GroupBox();
            label89 = new Label();
            label88 = new Label();
            label87 = new Label();
            cbx_meterconstant = new ComboBox();
            cbx_ratedcurrent = new ComboBox();
            cbx_ratedvoltage = new ComboBox();
            cbx_Connection = new ComboBox();
            label86 = new Label();
            label85 = new Label();
            label84 = new Label();
            label83 = new Label();
            btn_Initmeter = new Button();
            label70 = new Label();
            btn_SourcePort = new Button();
            tbx_sourcePort = new TextBox();
            label69 = new Label();
            btn_ReadContans = new Button();
            groupBox7 = new GroupBox();
            tabControl3 = new TabControl();
            tabPage11 = new TabPage();
            label97 = new Label();
            bttn_settooth = new Button();
            label96 = new Label();
            cbbx_ToosNum = new ComboBox();
            label95 = new Label();
            cbbx_BlueTooth = new ComboBox();
            tabPage12 = new TabPage();
            bttn_ClearError = new Button();
            bttn_StopError = new Button();
            label101 = new Label();
            label100 = new Label();
            tbx_MeterNo = new TextBox();
            bttn_ClockStart = new Button();
            label99 = new Label();
            tbxclockpulse = new TextBox();
            label98 = new Label();
            tabPage13 = new TabPage();
            tbx_TaskDelay = new TextBox();
            label105 = new Label();
            bttn_ErrorStart = new Button();
            tbx_iPulse = new TextBox();
            label104 = new Label();
            tbx_iMeterCount = new TextBox();
            label103 = new Label();
            tbx_MeterConstant = new TextBox();
            label102 = new Label();
            button3 = new Button();
            button4 = new Button();
            tabPage17 = new TabPage();
            label118 = new Label();
            textBoxRangeOutputUI = new TextBox();
            tbxRangeOutputUI = new Button();
            button7 = new Button();
            label117 = new Label();
            textBoxSetUIRange = new TextBox();
            cbxShutdownUI0 = new CheckBox();
            tbxiPulse = new TextBox();
            label82 = new Label();
            cbxUac = new ComboBox();
            cbxUab = new ComboBox();
            label80 = new Label();
            label81 = new Label();
            cbxICJ = new ComboBox();
            cbxIBJ = new ComboBox();
            cbxIAJ = new ComboBox();
            label77 = new Label();
            label78 = new Label();
            label79 = new Label();
            label65 = new Label();
            label66 = new Label();
            label67 = new Label();
            label62 = new Label();
            label63 = new Label();
            label64 = new Label();
            comboBoxIC = new ComboBox();
            comboBoxIB = new ComboBox();
            comboBoxIA = new ComboBox();
            comboBoxVC = new ComboBox();
            comboBoxVB = new ComboBox();
            comboBoxVA = new ComboBox();
            label59 = new Label();
            label56 = new Label();
            label60 = new Label();
            buttonCtrlUI = new Button();
            label61 = new Label();
            label57 = new Label();
            buttonXY_x0E = new Button();
            label58 = new Label();
            buttonCmdReadMeterData = new Button();
            btn_ReadStandMeter = new Button();
            groupBox6 = new GroupBox();
            tb_A_LC = new TextBox();
            tb_V_LC = new TextBox();
            tb_xx = new TextBox();
            label74 = new Label();
            label75 = new Label();
            label76 = new Label();
            tb_Uca = new TextBox();
            label72 = new Label();
            tb_Uba = new TextBox();
            label73 = new Label();
            tb_Alarm = new TextBox();
            label71 = new Label();
            tb_contans = new TextBox();
            label68 = new Label();
            tb_HZ = new TextBox();
            label55 = new Label();
            tb_EQ = new TextBox();
            tb_EP = new TextBox();
            tb_ES = new TextBox();
            tb_XC = new TextBox();
            tb_XB = new TextBox();
            tb_XA = new TextBox();
            tb_PFC = new TextBox();
            tb_PFB = new TextBox();
            tb_PFA = new TextBox();
            tb_SC = new TextBox();
            tb_SB = new TextBox();
            tb_SA = new TextBox();
            tb_QC = new TextBox();
            tb_QB = new TextBox();
            tb_QA = new TextBox();
            tb_PC = new TextBox();
            tb_PB = new TextBox();
            tb_PA = new TextBox();
            tb_IC = new TextBox();
            tb_IB = new TextBox();
            tb_IA = new TextBox();
            tb_UC = new TextBox();
            tb_UB = new TextBox();
            tb_UA = new TextBox();
            label52 = new Label();
            label53 = new Label();
            label54 = new Label();
            label49 = new Label();
            label50 = new Label();
            label51 = new Label();
            label46 = new Label();
            label47 = new Label();
            label48 = new Label();
            label43 = new Label();
            label44 = new Label();
            label45 = new Label();
            label40 = new Label();
            label41 = new Label();
            label42 = new Label();
            label37 = new Label();
            label38 = new Label();
            label39 = new Label();
            label34 = new Label();
            label35 = new Label();
            label36 = new Label();
            label33 = new Label();
            label32 = new Label();
            label31 = new Label();
            checkBoxISNOHEX = new CheckBox();
            tabPage8 = new TabPage();
            tabControl4 = new TabControl();
            tabPage14 = new TabPage();
            richTextBox1 = new RichTextBox();
            button6 = new Button();
            button5 = new Button();
            textBox5 = new TextBox();
            tabPage15 = new TabPage();
            tabPage16 = new TabPage();
            panel7 = new Panel();
            label116 = new Label();
            ServerImp = new ComboBox();
            label115 = new Label();
            textBox4 = new TextBox();
            textBox3 = new TextBox();
            LgServer = new Button();
            label114 = new Label();
            label113 = new Label();
            tabPage18 = new TabPage();
            tabControl6 = new TabControl();
            tabPage24 = new TabPage();
            groupBox18 = new GroupBox();
            btnSendData = new Button();
            rtbxSendData = new RichTextBox();
            panel11 = new Panel();
            cbxIsBroadcastMessage = new CheckBox();
            button1 = new Button();
            cbxClientConnc = new ComboBox();
            label122 = new Label();
            button8 = new Button();
            button2 = new Button();
            groupBox17 = new GroupBox();
            rtbxRevcData = new RichTextBox();
            panel10 = new Panel();
            groupBox19 = new GroupBox();
            cbxSendHEX = new CheckBox();
            cbxSendASCII = new CheckBox();
            groupBox16 = new GroupBox();
            cbxRevcHEX = new CheckBox();
            cbxRevcASCII = new CheckBox();
            groupBox15 = new GroupBox();
            TCPServerConnent = new Button();
            cbxPort = new ComboBox();
            cbxIp = new ComboBox();
            label121 = new Label();
            cbxSocketClass = new ComboBox();
            label120 = new Label();
            label23 = new Label();
            LogUnit = new GroupBox();
            panellog = new Panel();
            textBoxlog = new RichTextBox();
            contextMenuStrip1 = new ContextMenuStrip(components);
            清空ToolStripMenuItem = new ToolStripMenuItem();
            复制ToolStripMenuItem = new ToolStripMenuItem();
            切换背景色ToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            lblconnectStatus = new ToolStripStatusLabel();
            socketUnit = new GroupBox();
            groupBox14 = new GroupBox();
            label119 = new Label();
            groupBox13 = new GroupBox();
            cbxIsNoPortSeed = new CheckBox();
            comboBoxCOM = new ComboBox();
            btnflushPort = new Button();
            label12 = new Label();
            comboBoxBaute = new ComboBox();
            textBoxdatabit = new ComboBox();
            textBoxstopbit = new ComboBox();
            comboBoxparity = new ComboBox();
            label14 = new Label();
            label17 = new Label();
            label15 = new Label();
            label16 = new Label();
            buttonOpen = new Button();
            groupBox12 = new GroupBox();
            panel8 = new Panel();
            textBoxIP = new TextBox();
            btn_cilentSocket_Close = new Button();
            label1 = new Label();
            btn_cilentSocket = new Button();
            textBoxPort = new TextBox();
            label2 = new Label();
            panel1 = new Panel();
            toolStrip1 = new ToolStrip();
            tsbtnTerminalTest = new ToolStripButton();
            tsbtnMeterTest = new ToolStripButton();
            panel2 = new Panel();
            panel3 = new Panel();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            TestUnit.SuspendLayout();
            tabPage2.SuspendLayout();
            panel16.SuspendLayout();
            tabPage3.SuspendLayout();
            tabControl2.SuspendLayout();
            tabPage9.SuspendLayout();
            panel4.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            groupBox10.SuspendLayout();
            groupBox11.SuspendLayout();
            panel15.SuspendLayout();
            panel9.SuspendLayout();
            panel6.SuspendLayout();
            panel5.SuspendLayout();
            groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pBTaiti_yellow).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pBTaiti_Green).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pBTaiti_Red).BeginInit();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxRed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxGreen).BeginInit();
            groupBox1.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox3.SuspendLayout();
            tabPage7.SuspendLayout();
            groupBox9.SuspendLayout();
            groupBox8.SuspendLayout();
            groupBox7.SuspendLayout();
            tabControl3.SuspendLayout();
            tabPage11.SuspendLayout();
            tabPage12.SuspendLayout();
            tabPage13.SuspendLayout();
            tabPage17.SuspendLayout();
            groupBox6.SuspendLayout();
            tabPage8.SuspendLayout();
            tabControl4.SuspendLayout();
            tabPage14.SuspendLayout();
            panel7.SuspendLayout();
            tabPage18.SuspendLayout();
            tabControl6.SuspendLayout();
            tabPage24.SuspendLayout();
            groupBox18.SuspendLayout();
            panel11.SuspendLayout();
            groupBox17.SuspendLayout();
            panel10.SuspendLayout();
            groupBox19.SuspendLayout();
            groupBox16.SuspendLayout();
            groupBox15.SuspendLayout();
            LogUnit.SuspendLayout();
            panellog.SuspendLayout();
            contextMenuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            socketUnit.SuspendLayout();
            groupBox14.SuspendLayout();
            groupBox13.SuspendLayout();
            groupBox12.SuspendLayout();
            panel8.SuspendLayout();
            panel1.SuspendLayout();
            toolStrip1.SuspendLayout();
            panel2.SuspendLayout();
            panel3.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Controls.Add(tabPage5);
            tabControl1.Controls.Add(tabPage6);
            tabControl1.Controls.Add(tabPage7);
            tabControl1.Controls.Add(tabPage8);
            tabControl1.Controls.Add(tabPage18);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Margin = new Padding(5, 4, 5, 4);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1628, 582);
            tabControl1.TabIndex = 0;
            tabControl1.DrawItem += tabControl1_DrawItem;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(TestUnit);
            tabPage1.Location = new Point(4, 33);
            tabPage1.Margin = new Padding(5, 4, 5, 4);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(5, 4, 5, 4);
            tabPage1.Size = new Size(1620, 545);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "模组测试单元";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // TestUnit
            // 
            TestUnit.Controls.Add(CCOACDown);
            TestUnit.Controls.Add(CCOACOn);
            TestUnit.Controls.Add(CCODCDown);
            TestUnit.Controls.Add(CCODCOn);
            TestUnit.Controls.Add(label21);
            TestUnit.Controls.Add(label20);
            TestUnit.Controls.Add(label3);
            TestUnit.Controls.Add(checkBoxN);
            TestUnit.Controls.Add(checkBoxC);
            TestUnit.Controls.Add(checkBoxB);
            TestUnit.Controls.Add(checkBoxA);
            TestUnit.Controls.Add(checkBox2);
            TestUnit.Controls.Add(checkBox1);
            TestUnit.Controls.Add(btnPowerDown_AC);
            TestUnit.Controls.Add(btnPowerOn_AC);
            TestUnit.Controls.Add(label8);
            TestUnit.Controls.Add(tbxModelNumber);
            TestUnit.Controls.Add(cbxTerminalCLASS);
            TestUnit.Controls.Add(tbx_addr);
            TestUnit.Controls.Add(label7);
            TestUnit.Controls.Add(label6);
            TestUnit.Controls.Add(label5);
            TestUnit.Controls.Add(btnPowerDown_DC);
            TestUnit.Controls.Add(btnPowerOn_DC);
            TestUnit.Controls.Add(label4);
            TestUnit.Dock = DockStyle.Fill;
            TestUnit.Location = new Point(5, 4);
            TestUnit.Margin = new Padding(5, 4, 5, 4);
            TestUnit.Name = "TestUnit";
            TestUnit.Padding = new Padding(5, 4, 5, 4);
            TestUnit.Size = new Size(1610, 537);
            TestUnit.TabIndex = 1;
            TestUnit.TabStop = false;
            TestUnit.Text = "测试单元";
            // 
            // CCOACDown
            // 
            CCOACDown.Location = new Point(498, 210);
            CCOACDown.Margin = new Padding(5, 4, 5, 4);
            CCOACDown.Name = "CCOACDown";
            CCOACDown.Size = new Size(141, 35);
            CCOACDown.TabIndex = 28;
            CCOACDown.Text = "CCO下电";
            CCOACDown.UseVisualStyleBackColor = true;
            CCOACDown.Click += CCOACDown_Click;
            // 
            // CCOACOn
            // 
            CCOACOn.Location = new Point(347, 210);
            CCOACOn.Margin = new Padding(5, 4, 5, 4);
            CCOACOn.Name = "CCOACOn";
            CCOACOn.Size = new Size(141, 35);
            CCOACOn.TabIndex = 27;
            CCOACOn.Text = "CCO上交流电";
            CCOACOn.UseVisualStyleBackColor = true;
            CCOACOn.Click += CCOACOn_Click;
            // 
            // CCODCDown
            // 
            CCODCDown.Location = new Point(497, 134);
            CCODCDown.Margin = new Padding(5, 4, 5, 4);
            CCODCDown.Name = "CCODCDown";
            CCODCDown.Size = new Size(141, 35);
            CCODCDown.TabIndex = 26;
            CCODCDown.Text = "CCO下直电";
            CCODCDown.UseVisualStyleBackColor = true;
            CCODCDown.Click += CCODCDown_Click;
            // 
            // CCODCOn
            // 
            CCODCOn.Location = new Point(346, 134);
            CCODCOn.Margin = new Padding(5, 4, 5, 4);
            CCODCOn.Name = "CCODCOn";
            CCODCOn.Size = new Size(141, 35);
            CCODCOn.TabIndex = 25;
            CCODCOn.Text = "CCO上直电";
            CCODCOn.UseVisualStyleBackColor = true;
            CCODCOn.Click += CCODCOn_Click;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(347, 182);
            label21.Margin = new Padding(5, 0, 5, 0);
            label21.Name = "label21";
            label21.Size = new Size(394, 24);
            label21.TabIndex = 24;
            label21.Text = "CCO模组三相四线（380V)上下电指令（0x02）";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(347, 106);
            label20.Margin = new Padding(5, 0, 5, 0);
            label20.Name = "label20";
            label20.Size = new Size(303, 24);
            label20.TabIndex = 23;
            label20.Text = "CCO模组上下电指令（0x01/0x31）";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Bottom;
            label3.ForeColor = Color.Red;
            label3.Location = new Point(5, 509);
            label3.Margin = new Padding(5, 0, 5, 0);
            label3.Name = "label3";
            label3.Size = new Size(437, 24);
            label3.TabIndex = 22;
            label3.Text = "总控制端口号：4000，串口参数设置端口号：64444.";
            // 
            // checkBoxN
            // 
            checkBoxN.AutoSize = true;
            checkBoxN.Location = new Point(431, 21);
            checkBoxN.Name = "checkBoxN";
            checkBoxN.Size = new Size(69, 28);
            checkBoxN.TabIndex = 21;
            checkBoxN.Text = "N相";
            checkBoxN.UseVisualStyleBackColor = true;
            // 
            // checkBoxC
            // 
            checkBoxC.AutoSize = true;
            checkBoxC.Checked = true;
            checkBoxC.CheckState = CheckState.Checked;
            checkBoxC.Location = new Point(357, 21);
            checkBoxC.Name = "checkBoxC";
            checkBoxC.Size = new Size(66, 28);
            checkBoxC.TabIndex = 20;
            checkBoxC.Text = "C相";
            checkBoxC.UseVisualStyleBackColor = true;
            // 
            // checkBoxB
            // 
            checkBoxB.AutoSize = true;
            checkBoxB.Checked = true;
            checkBoxB.CheckState = CheckState.Checked;
            checkBoxB.Location = new Point(281, 21);
            checkBoxB.Name = "checkBoxB";
            checkBoxB.Size = new Size(65, 28);
            checkBoxB.TabIndex = 19;
            checkBoxB.Text = "B相";
            checkBoxB.UseVisualStyleBackColor = true;
            // 
            // checkBoxA
            // 
            checkBoxA.AutoSize = true;
            checkBoxA.Checked = true;
            checkBoxA.CheckState = CheckState.Checked;
            checkBoxA.Location = new Point(206, 21);
            checkBoxA.Name = "checkBoxA";
            checkBoxA.Size = new Size(67, 28);
            checkBoxA.TabIndex = 18;
            checkBoxA.Text = "A相";
            checkBoxA.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new Point(97, 22);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new Size(78, 28);
            checkBox2.TabIndex = 1;
            checkBox2.Text = "0x31";
            checkBox2.UseVisualStyleBackColor = true;
            checkBox2.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Checked = true;
            checkBox1.CheckState = CheckState.Checked;
            checkBox1.Location = new Point(13, 22);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(78, 28);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "0x01";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            // 
            // btnPowerDown_AC
            // 
            btnPowerDown_AC.Location = new Point(160, 210);
            btnPowerDown_AC.Margin = new Padding(5, 4, 5, 4);
            btnPowerDown_AC.Name = "btnPowerDown_AC";
            btnPowerDown_AC.Size = new Size(141, 35);
            btnPowerDown_AC.TabIndex = 17;
            btnPowerDown_AC.Text = "交流下电";
            btnPowerDown_AC.UseVisualStyleBackColor = true;
            btnPowerDown_AC.Click += btnPowerDown_AC_Click;
            // 
            // btnPowerOn_AC
            // 
            btnPowerOn_AC.Location = new Point(5, 210);
            btnPowerOn_AC.Margin = new Padding(5, 4, 5, 4);
            btnPowerOn_AC.Name = "btnPowerOn_AC";
            btnPowerOn_AC.Size = new Size(141, 35);
            btnPowerOn_AC.TabIndex = 16;
            btnPowerOn_AC.Text = "交流上电";
            btnPowerOn_AC.UseVisualStyleBackColor = true;
            btnPowerOn_AC.Click += btnPowerOn_AC_Click;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(9, 182);
            label8.Margin = new Padding(5, 0, 5, 0);
            label8.Name = "label8";
            label8.Size = new Size(340, 24);
            label8.TabIndex = 15;
            label8.Text = "功能模块三相四线交流上电指令（0x21）";
            // 
            // tbxModelNumber
            // 
            tbxModelNumber.Location = new Point(610, 58);
            tbxModelNumber.Margin = new Padding(5, 4, 5, 4);
            tbxModelNumber.Name = "tbxModelNumber";
            tbxModelNumber.Size = new Size(87, 30);
            tbxModelNumber.TabIndex = 14;
            tbxModelNumber.Text = "1";
            tbxModelNumber.KeyPress += TextboxOnlyNumber_KeyPressed;
            // 
            // cbxTerminalCLASS
            // 
            cbxTerminalCLASS.FormattingEnabled = true;
            cbxTerminalCLASS.Location = new Point(269, 55);
            cbxTerminalCLASS.Margin = new Padding(5, 4, 5, 4);
            cbxTerminalCLASS.Name = "cbxTerminalCLASS";
            cbxTerminalCLASS.Size = new Size(251, 32);
            cbxTerminalCLASS.TabIndex = 13;
            // 
            // tbx_addr
            // 
            tbx_addr.Location = new Point(61, 58);
            tbx_addr.Margin = new Padding(5, 4, 5, 4);
            tbx_addr.Name = "tbx_addr";
            tbx_addr.Size = new Size(87, 30);
            tbx_addr.TabIndex = 12;
            tbx_addr.Text = "1";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(531, 62);
            label7.Margin = new Padding(5, 0, 5, 0);
            label7.Name = "label7";
            label7.Size = new Size(64, 24);
            label7.TabIndex = 11;
            label7.Text = "模块号";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(160, 62);
            label6.Margin = new Padding(5, 0, 5, 0);
            label6.Name = "label6";
            label6.Size = new Size(82, 24);
            label6.TabIndex = 10;
            label6.Text = "终端类型";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(9, 62);
            label5.Margin = new Padding(5, 0, 5, 0);
            label5.Name = "label5";
            label5.Size = new Size(46, 24);
            label5.TabIndex = 9;
            label5.Text = "地址";
            // 
            // btnPowerDown_DC
            // 
            btnPowerDown_DC.Location = new Point(160, 134);
            btnPowerDown_DC.Margin = new Padding(5, 4, 5, 4);
            btnPowerDown_DC.Name = "btnPowerDown_DC";
            btnPowerDown_DC.Size = new Size(141, 35);
            btnPowerDown_DC.TabIndex = 8;
            btnPowerDown_DC.Text = "直流下电";
            btnPowerDown_DC.UseVisualStyleBackColor = true;
            btnPowerDown_DC.Click += btnPowerDown_DC_Click;
            // 
            // btnPowerOn_DC
            // 
            btnPowerOn_DC.Location = new Point(5, 134);
            btnPowerOn_DC.Margin = new Padding(5, 4, 5, 4);
            btnPowerOn_DC.Name = "btnPowerOn_DC";
            btnPowerOn_DC.Size = new Size(141, 35);
            btnPowerOn_DC.TabIndex = 7;
            btnPowerOn_DC.Text = "直流上电";
            btnPowerOn_DC.UseVisualStyleBackColor = true;
            btnPowerOn_DC.Click += btnPowerOn_DC_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(9, 106);
            label4.Margin = new Padding(5, 0, 5, 0);
            label4.Name = "label4";
            label4.Size = new Size(318, 24);
            label4.TabIndex = 0;
            label4.Text = "功能模块直流上电指令（0x01/0x31）";
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(panel16);
            tabPage2.Controls.Add(label9);
            tabPage2.Location = new Point(4, 33);
            tabPage2.Margin = new Padding(5, 4, 5, 4);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(5, 4, 5, 4);
            tabPage2.Size = new Size(1620, 545);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "国网测试";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // panel16
            // 
            panel16.Controls.Add(SGCC698FF);
            panel16.Controls.Add(buttonKZHLStatus);
            panel16.Controls.Add(label10);
            panel16.Controls.Add(label13);
            panel16.Controls.Add(buttonKZHLID);
            panel16.Controls.Add(label18);
            panel16.Controls.Add(CSG698FF);
            panel16.Controls.Add(label19);
            panel16.Controls.Add(label11);
            panel16.Controls.Add(SGCC645FF);
            panel16.Dock = DockStyle.Left;
            panel16.Location = new Point(5, 4);
            panel16.Name = "panel16";
            panel16.Size = new Size(823, 513);
            panel16.TabIndex = 28;
            // 
            // SGCC698FF
            // 
            SGCC698FF.Location = new Point(614, 3);
            SGCC698FF.Name = "SGCC698FF";
            SGCC698FF.Size = new Size(196, 34);
            SGCC698FF.TabIndex = 6;
            SGCC698FF.Text = "国网698广播";
            SGCC698FF.UseVisualStyleBackColor = true;
            SGCC698FF.Click += SGCC698FF_Click;
            // 
            // buttonKZHLStatus
            // 
            buttonKZHLStatus.Location = new Point(614, 141);
            buttonKZHLStatus.Name = "buttonKZHLStatus";
            buttonKZHLStatus.Size = new Size(196, 34);
            buttonKZHLStatus.TabIndex = 25;
            buttonKZHLStatus.Text = "控制回路检测仪状态";
            buttonKZHLStatus.UseVisualStyleBackColor = true;
            buttonKZHLStatus.Click += buttonKZHLStatus_Click;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.ForeColor = Color.Red;
            label10.Location = new Point(8, 13);
            label10.Name = "label10";
            label10.Size = new Size(585, 24);
            label10.TabIndex = 1;
            label10.Text = "6817004345AAAAAAAAAAAA005B4F0501004001020000ED0316";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.ForeColor = Color.Red;
            label13.Location = new Point(8, 100);
            label13.Name = "label13";
            label13.Size = new Size(530, 24);
            label13.TabIndex = 5;
            label13.Text = "6810001000684AFFFFFFFFFFFF010A710000210100E0C216";
            // 
            // buttonKZHLID
            // 
            buttonKZHLID.Location = new Point(614, 188);
            buttonKZHLID.Name = "buttonKZHLID";
            buttonKZHLID.Size = new Size(196, 34);
            buttonKZHLID.TabIndex = 27;
            buttonKZHLID.Text = "控制回路检测仪ID";
            buttonKZHLID.UseVisualStyleBackColor = true;
            buttonKZHLID.Click += buttonKZHLID_Click;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.ForeColor = Color.Red;
            label18.Location = new Point(8, 146);
            label18.Name = "label18";
            label18.Size = new Size(578, 24);
            label18.TabIndex = 24;
            label18.Text = "6817004345AAAAAAAAAAAA10da5f05013DFF140200006c6816";
            // 
            // CSG698FF
            // 
            CSG698FF.Location = new Point(614, 96);
            CSG698FF.Name = "CSG698FF";
            CSG698FF.Size = new Size(196, 34);
            CSG698FF.TabIndex = 8;
            CSG698FF.Text = "南网698广播";
            CSG698FF.UseVisualStyleBackColor = true;
            CSG698FF.Click += CSG698FF_Click;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.ForeColor = Color.Red;
            label19.Location = new Point(8, 189);
            label19.Name = "label19";
            label19.Size = new Size(590, 24);
            label19.TabIndex = 26;
            label19.Text = "6817004345AAAAAAAAAAAA10DA5F050127F10002000027D316";
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.ForeColor = Color.Red;
            label11.Location = new Point(8, 57);
            label11.Name = "label11";
            label11.Size = new Size(380, 24);
            label11.TabIndex = 3;
            label11.Text = "FEFEFEFE68AAAAAAAAAAAA681300DF16";
            // 
            // SGCC645FF
            // 
            SGCC645FF.Location = new Point(614, 50);
            SGCC645FF.Name = "SGCC645FF";
            SGCC645FF.Size = new Size(196, 34);
            SGCC645FF.TabIndex = 7;
            SGCC645FF.Text = "国网645广播";
            SGCC645FF.UseVisualStyleBackColor = true;
            SGCC645FF.Click += SGCC645FF_Click;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Dock = DockStyle.Bottom;
            label9.ForeColor = Color.Red;
            label9.Location = new Point(5, 517);
            label9.Margin = new Padding(5, 0, 5, 0);
            label9.Name = "label9";
            label9.Size = new Size(275, 24);
            label9.TabIndex = 23;
            label9.Text = "通道端口：485-2，232，红外等";
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(tabControl2);
            tabPage3.Location = new Point(4, 33);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(1620, 545);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "终端测试单元：V1";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            tabControl2.Controls.Add(tabPage9);
            tabControl2.Controls.Add(tabPage10);
            tabControl2.Dock = DockStyle.Fill;
            tabControl2.Location = new Point(3, 3);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(1614, 539);
            tabControl2.TabIndex = 0;
            // 
            // tabPage9
            // 
            tabPage9.Controls.Add(panel4);
            tabPage9.Location = new Point(4, 33);
            tabPage9.Name = "tabPage9";
            tabPage9.Padding = new Padding(3);
            tabPage9.Size = new Size(1606, 502);
            tabPage9.TabIndex = 0;
            tabPage9.Text = "主控";
            tabPage9.UseVisualStyleBackColor = true;
            // 
            // panel4
            // 
            panel4.BorderStyle = BorderStyle.FixedSingle;
            panel4.Controls.Add(tableLayoutPanel1);
            panel4.Controls.Add(panel5);
            panel4.Dock = DockStyle.Fill;
            panel4.Location = new Point(3, 3);
            panel4.Name = "panel4";
            panel4.Size = new Size(1600, 496);
            panel4.TabIndex = 19;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Controls.Add(groupBox10, 0, 0);
            tableLayoutPanel1.Controls.Add(groupBox11, 1, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(495, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanel1.Size = new Size(1103, 494);
            tableLayoutPanel1.TabIndex = 31;
            // 
            // groupBox10
            // 
            groupBox10.Controls.Add(bttnReadSTAPinStatus);
            groupBox10.Controls.Add(comboBoxSTAStutas);
            groupBox10.Controls.Add(bttnSTALPin);
            groupBox10.Controls.Add(bttnSTAHPin);
            groupBox10.Controls.Add(cbxSTAModePinStatus);
            groupBox10.Controls.Add(label109);
            groupBox10.Controls.Add(label108);
            groupBox10.Controls.Add(btnT1_ACCTRL);
            groupBox10.Controls.Add(btnT1_DCCTRL);
            groupBox10.Controls.Add(cbbxSTAModel);
            groupBox10.Controls.Add(label107);
            groupBox10.Dock = DockStyle.Fill;
            groupBox10.Location = new Point(3, 3);
            groupBox10.Name = "groupBox10";
            groupBox10.Size = new Size(545, 241);
            groupBox10.TabIndex = 0;
            groupBox10.TabStop = false;
            groupBox10.Text = "模块控制";
            // 
            // bttnReadSTAPinStatus
            // 
            bttnReadSTAPinStatus.Location = new Point(306, 113);
            bttnReadSTAPinStatus.Name = "bttnReadSTAPinStatus";
            bttnReadSTAPinStatus.Size = new Size(140, 34);
            bttnReadSTAPinStatus.TabIndex = 10;
            bttnReadSTAPinStatus.Tag = "0";
            bttnReadSTAPinStatus.Text = "读取状态";
            bttnReadSTAPinStatus.UseVisualStyleBackColor = true;
            bttnReadSTAPinStatus.Click += bttnReadSTAPinStatus_Click;
            // 
            // comboBoxSTAStutas
            // 
            comboBoxSTAStutas.FormattingEnabled = true;
            comboBoxSTAStutas.Items.AddRange(new object[] { "STA1", "STA2" });
            comboBoxSTAStutas.Location = new Point(162, 113);
            comboBoxSTAStutas.Name = "comboBoxSTAStutas";
            comboBoxSTAStutas.Size = new Size(117, 32);
            comboBoxSTAStutas.TabIndex = 9;
            // 
            // bttnSTALPin
            // 
            bttnSTALPin.Location = new Point(574, 76);
            bttnSTALPin.Name = "bttnSTALPin";
            bttnSTALPin.Size = new Size(82, 34);
            bttnSTALPin.TabIndex = 8;
            bttnSTALPin.Tag = "0";
            bttnSTALPin.Text = "低电平";
            bttnSTALPin.UseVisualStyleBackColor = true;
            bttnSTALPin.Click += bttnSTALPin_Click;
            // 
            // bttnSTAHPin
            // 
            bttnSTAHPin.Location = new Point(465, 76);
            bttnSTAHPin.Name = "bttnSTAHPin";
            bttnSTAHPin.Size = new Size(82, 34);
            bttnSTAHPin.TabIndex = 7;
            bttnSTAHPin.Tag = "0";
            bttnSTAHPin.Text = "高电平";
            bttnSTAHPin.UseVisualStyleBackColor = true;
            bttnSTAHPin.Click += bttnSTAHPin_Click;
            // 
            // cbxSTAModePinStatus
            // 
            cbxSTAModePinStatus.FormattingEnabled = true;
            cbxSTAModePinStatus.Items.AddRange(new object[] { "RST_1", "SET_1", "EVENT_1", "RST_2", "SET_2", "EVENT_2" });
            cbxSTAModePinStatus.Location = new Point(330, 76);
            cbxSTAModePinStatus.Name = "cbxSTAModePinStatus";
            cbxSTAModePinStatus.Size = new Size(117, 32);
            cbxSTAModePinStatus.TabIndex = 6;
            // 
            // label109
            // 
            label109.AutoSize = true;
            label109.Location = new Point(5, 114);
            label109.Name = "label109";
            label109.Size = new Size(151, 24);
            label109.TabIndex = 5;
            label109.Text = "STA模块引脚电平";
            // 
            // label108
            // 
            label108.AutoSize = true;
            label108.Location = new Point(5, 79);
            label108.Name = "label108";
            label108.Size = new Size(306, 24);
            label108.TabIndex = 4;
            label108.Text = "STA模块RST、SET、EVENT引脚状态";
            // 
            // btnT1_ACCTRL
            // 
            btnT1_ACCTRL.Location = new Point(423, 27);
            btnT1_ACCTRL.Name = "btnT1_ACCTRL";
            btnT1_ACCTRL.Size = new Size(112, 34);
            btnT1_ACCTRL.TabIndex = 3;
            btnT1_ACCTRL.Text = "上交流电";
            btnT1_ACCTRL.UseVisualStyleBackColor = true;
            btnT1_ACCTRL.Click += btnT1_ACCTRL_Click;
            // 
            // btnT1_DCCTRL
            // 
            btnT1_DCCTRL.Location = new Point(289, 27);
            btnT1_DCCTRL.Name = "btnT1_DCCTRL";
            btnT1_DCCTRL.Size = new Size(112, 34);
            btnT1_DCCTRL.TabIndex = 2;
            btnT1_DCCTRL.Tag = "0";
            btnT1_DCCTRL.Text = "上直流电";
            btnT1_DCCTRL.UseVisualStyleBackColor = true;
            btnT1_DCCTRL.Click += btnT1_DCCTRL_Click;
            // 
            // cbbxSTAModel
            // 
            cbbxSTAModel.FormattingEnabled = true;
            cbbxSTAModel.Items.AddRange(new object[] { "STA1-STA2", "STA1", "STA2" });
            cbbxSTAModel.Location = new Point(86, 28);
            cbbxSTAModel.Name = "cbbxSTAModel";
            cbbxSTAModel.Size = new Size(180, 32);
            cbbxSTAModel.TabIndex = 1;
            // 
            // label107
            // 
            label107.AutoSize = true;
            label107.Location = new Point(3, 33);
            label107.Name = "label107";
            label107.Size = new Size(71, 24);
            label107.TabIndex = 0;
            label107.Text = "sta模块";
            // 
            // groupBox11
            // 
            groupBox11.Controls.Add(panel15);
            groupBox11.Controls.Add(panel9);
            groupBox11.Controls.Add(panel6);
            groupBox11.Dock = DockStyle.Fill;
            groupBox11.Location = new Point(554, 3);
            groupBox11.Name = "groupBox11";
            groupBox11.Size = new Size(546, 241);
            groupBox11.TabIndex = 1;
            groupBox11.TabStop = false;
            groupBox11.Text = "河南模组化新增";
            // 
            // panel15
            // 
            panel15.Controls.Add(btnelectriciansource);
            panel15.Controls.Add(btnstandardSource);
            panel15.Controls.Add(label123);
            panel15.Dock = DockStyle.Left;
            panel15.Location = new Point(360, 26);
            panel15.Name = "panel15";
            panel15.Size = new Size(168, 212);
            panel15.TabIndex = 9;
            // 
            // btnelectriciansource
            // 
            btnelectriciansource.ForeColor = Color.Black;
            btnelectriciansource.Location = new Point(3, 69);
            btnelectriciansource.Name = "btnelectriciansource";
            btnelectriciansource.Size = new Size(112, 31);
            btnelectriciansource.TabIndex = 10;
            btnelectriciansource.Text = "电工源";
            btnelectriciansource.UseVisualStyleBackColor = true;
            btnelectriciansource.Click += btnelectriciansource_Click;
            // 
            // btnstandardSource
            // 
            btnstandardSource.ForeColor = Color.Black;
            btnstandardSource.Location = new Point(3, 36);
            btnstandardSource.Name = "btnstandardSource";
            btnstandardSource.Size = new Size(112, 31);
            btnstandardSource.TabIndex = 9;
            btnstandardSource.Text = "标准源";
            btnstandardSource.UseVisualStyleBackColor = true;
            btnstandardSource.Click += btnstandardSource_Click;
            // 
            // label123
            // 
            label123.AutoSize = true;
            label123.Location = new Point(3, 9);
            label123.Name = "label123";
            label123.Size = new Size(154, 24);
            label123.TabIndex = 8;
            label123.Text = "标准源电工源切换";
            // 
            // panel9
            // 
            panel9.Controls.Add(btn_changePCBDownAC);
            panel9.Controls.Add(btn_changePCBUPAC);
            panel9.Controls.Add(cbx_changePCBUPAC);
            panel9.Controls.Add(label22);
            panel9.Dock = DockStyle.Left;
            panel9.Location = new Point(212, 26);
            panel9.Name = "panel9";
            panel9.Size = new Size(148, 212);
            panel9.TabIndex = 8;
            // 
            // btn_changePCBDownAC
            // 
            btn_changePCBDownAC.ForeColor = Color.Black;
            btn_changePCBDownAC.Location = new Point(7, 129);
            btn_changePCBDownAC.Name = "btn_changePCBDownAC";
            btn_changePCBDownAC.Size = new Size(112, 31);
            btn_changePCBDownAC.TabIndex = 18;
            btn_changePCBDownAC.Text = "下电";
            btn_changePCBDownAC.UseVisualStyleBackColor = true;
            btn_changePCBDownAC.Click += btn_changePCBDownAC_Click;
            // 
            // btn_changePCBUPAC
            // 
            btn_changePCBUPAC.ForeColor = Color.Black;
            btn_changePCBUPAC.Location = new Point(7, 91);
            btn_changePCBUPAC.Name = "btn_changePCBUPAC";
            btn_changePCBUPAC.Size = new Size(112, 31);
            btn_changePCBUPAC.TabIndex = 7;
            btn_changePCBUPAC.Text = "上电";
            btn_changePCBUPAC.UseVisualStyleBackColor = true;
            btn_changePCBUPAC.Click += btn_changePCBUPAC_Click;
            // 
            // cbx_changePCBUPAC
            // 
            cbx_changePCBUPAC.FormattingEnabled = true;
            cbx_changePCBUPAC.Items.AddRange(new object[] { "源表", "功放", "源表功放", "读取状态" });
            cbx_changePCBUPAC.Location = new Point(7, 46);
            cbx_changePCBUPAC.Name = "cbx_changePCBUPAC";
            cbx_changePCBUPAC.Size = new Size(97, 32);
            cbx_changePCBUPAC.TabIndex = 17;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.Location = new Point(3, 5);
            label22.Name = "label22";
            label22.Size = new Size(136, 24);
            label22.TabIndex = 7;
            label22.Text = "切换版上交流电";
            // 
            // panel6
            // 
            panel6.BorderStyle = BorderStyle.FixedSingle;
            panel6.Controls.Add(label110);
            panel6.Controls.Add(chexblx_LEDRGY);
            panel6.Controls.Add(button_SETLED1);
            panel6.Controls.Add(button_SETLED4);
            panel6.Controls.Add(button_SETLED2);
            panel6.Controls.Add(button_SETLED3);
            panel6.Dock = DockStyle.Left;
            panel6.Location = new Point(3, 26);
            panel6.Name = "panel6";
            panel6.Size = new Size(209, 212);
            panel6.TabIndex = 7;
            // 
            // label110
            // 
            label110.AutoSize = true;
            label110.Location = new Point(3, 6);
            label110.Name = "label110";
            label110.Size = new Size(115, 24);
            label110.TabIndex = 1;
            label110.Text = "LED控制命令";
            // 
            // chexblx_LEDRGY
            // 
            chexblx_LEDRGY.FormattingEnabled = true;
            chexblx_LEDRGY.Items.AddRange(new object[] { "红", "绿", "黄" });
            chexblx_LEDRGY.Location = new Point(3, 33);
            chexblx_LEDRGY.Name = "chexblx_LEDRGY";
            chexblx_LEDRGY.Size = new Size(73, 85);
            chexblx_LEDRGY.TabIndex = 6;
            chexblx_LEDRGY.ItemCheck += chexblx_LEDRGY_ItemCheck;
            // 
            // button_SETLED1
            // 
            button_SETLED1.ForeColor = Color.Black;
            button_SETLED1.Location = new Point(83, 33);
            button_SETLED1.Name = "button_SETLED1";
            button_SETLED1.Size = new Size(112, 31);
            button_SETLED1.TabIndex = 2;
            button_SETLED1.Text = "LED1";
            button_SETLED1.UseVisualStyleBackColor = true;
            button_SETLED1.Click += button_SETLED1_Click;
            // 
            // button_SETLED4
            // 
            button_SETLED4.Location = new Point(83, 141);
            button_SETLED4.Name = "button_SETLED4";
            button_SETLED4.Size = new Size(112, 31);
            button_SETLED4.TabIndex = 5;
            button_SETLED4.Text = "LED4";
            button_SETLED4.UseVisualStyleBackColor = true;
            button_SETLED4.Click += button_SETLED4_Click;
            // 
            // button_SETLED2
            // 
            button_SETLED2.Location = new Point(83, 69);
            button_SETLED2.Name = "button_SETLED2";
            button_SETLED2.Size = new Size(112, 31);
            button_SETLED2.TabIndex = 3;
            button_SETLED2.Text = "LED2";
            button_SETLED2.UseVisualStyleBackColor = true;
            button_SETLED2.Click += button_SETLED2_Click;
            // 
            // button_SETLED3
            // 
            button_SETLED3.Location = new Point(83, 105);
            button_SETLED3.Name = "button_SETLED3";
            button_SETLED3.Size = new Size(112, 31);
            button_SETLED3.TabIndex = 4;
            button_SETLED3.Text = "LED3";
            button_SETLED3.UseVisualStyleBackColor = true;
            button_SETLED3.Click += button_SETLED3_Click;
            // 
            // panel5
            // 
            panel5.BorderStyle = BorderStyle.FixedSingle;
            panel5.Controls.Add(groupBox5);
            panel5.Controls.Add(groupBox2);
            panel5.Controls.Add(groupBox1);
            panel5.Controls.Add(groupBox4);
            panel5.Controls.Add(groupBox3);
            panel5.Dock = DockStyle.Left;
            panel5.Location = new Point(0, 0);
            panel5.Name = "panel5";
            panel5.Size = new Size(495, 494);
            panel5.TabIndex = 30;
            // 
            // groupBox5
            // 
            groupBox5.Controls.Add(label30);
            groupBox5.Controls.Add(label29);
            groupBox5.Controls.Add(label28);
            groupBox5.Controls.Add(label27);
            groupBox5.Controls.Add(pBTaiti_yellow);
            groupBox5.Controls.Add(pBTaiti_Green);
            groupBox5.Controls.Add(pBTaiti_Red);
            groupBox5.Dock = DockStyle.Fill;
            groupBox5.Location = new Point(0, 421);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new Size(493, 71);
            groupBox5.TabIndex = 38;
            groupBox5.TabStop = false;
            groupBox5.Text = "台体运行指示灯（0x2C）";
            // 
            // label30
            // 
            label30.AutoSize = true;
            label30.Location = new Point(2, 33);
            label30.Name = "label30";
            label30.Size = new Size(145, 24);
            label30.TabIndex = 11;
            label30.Text = "地址在上边填↑：";
            // 
            // label29
            // 
            label29.AutoSize = true;
            label29.ForeColor = Color.Yellow;
            label29.Location = new Point(383, 34);
            label29.Name = "label29";
            label29.Size = new Size(46, 24);
            label29.TabIndex = 10;
            label29.Text = "黄灯";
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.ForeColor = Color.Lime;
            label28.Location = new Point(277, 34);
            label28.Name = "label28";
            label28.Size = new Size(46, 24);
            label28.TabIndex = 9;
            label28.Text = "绿灯";
            // 
            // label27
            // 
            label27.AutoSize = true;
            label27.ForeColor = Color.Red;
            label27.Location = new Point(171, 34);
            label27.Name = "label27";
            label27.Size = new Size(46, 24);
            label27.TabIndex = 6;
            label27.Text = "红灯";
            // 
            // pBTaiti_yellow
            // 
            pBTaiti_yellow.Image = Properties.Resources.灰灯;
            pBTaiti_yellow.Location = new Point(435, 17);
            pBTaiti_yellow.Name = "pBTaiti_yellow";
            pBTaiti_yellow.Size = new Size(42, 42);
            pBTaiti_yellow.SizeMode = PictureBoxSizeMode.Zoom;
            pBTaiti_yellow.TabIndex = 8;
            pBTaiti_yellow.TabStop = false;
            pBTaiti_yellow.Click += pBTaiti_yellow_Click;
            // 
            // pBTaiti_Green
            // 
            pBTaiti_Green.Image = Properties.Resources.灰灯;
            pBTaiti_Green.Location = new Point(328, 17);
            pBTaiti_Green.Name = "pBTaiti_Green";
            pBTaiti_Green.Size = new Size(42, 42);
            pBTaiti_Green.SizeMode = PictureBoxSizeMode.Zoom;
            pBTaiti_Green.TabIndex = 7;
            pBTaiti_Green.TabStop = false;
            pBTaiti_Green.Click += pBTaiti_Green_Click;
            // 
            // pBTaiti_Red
            // 
            pBTaiti_Red.Image = Properties.Resources.灰灯;
            pBTaiti_Red.Location = new Point(223, 17);
            pBTaiti_Red.Name = "pBTaiti_Red";
            pBTaiti_Red.Size = new Size(42, 42);
            pBTaiti_Red.SizeMode = PictureBoxSizeMode.Zoom;
            pBTaiti_Red.TabIndex = 6;
            pBTaiti_Red.TabStop = false;
            pBTaiti_Red.Click += pBTaiti_Red_Click;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(pictureBoxRed);
            groupBox2.Controls.Add(label26);
            groupBox2.Controls.Add(label24);
            groupBox2.Controls.Add(pictureBoxGreen);
            groupBox2.Dock = DockStyle.Top;
            groupBox2.Location = new Point(0, 352);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(493, 69);
            groupBox2.TabIndex = 37;
            groupBox2.TabStop = false;
            groupBox2.Text = "表位运行指示灯控制命令（0x2A）";
            // 
            // pictureBoxRed
            // 
            pictureBoxRed.Image = Properties.Resources.灰灯;
            pictureBoxRed.Location = new Point(97, 24);
            pictureBoxRed.Name = "pictureBoxRed";
            pictureBoxRed.Size = new Size(42, 42);
            pictureBoxRed.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxRed.TabIndex = 5;
            pictureBoxRed.TabStop = false;
            pictureBoxRed.Click += pictureBoxRed_Click;
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.Location = new Point(223, 39);
            label26.Name = "label26";
            label26.Size = new Size(82, 24);
            label26.TabIndex = 4;
            label26.Text = "绿灯控制";
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.Location = new Point(9, 39);
            label24.Name = "label24";
            label24.Size = new Size(82, 24);
            label24.TabIndex = 3;
            label24.Text = "红灯控制";
            // 
            // pictureBoxGreen
            // 
            pictureBoxGreen.Image = Properties.Resources.灰灯;
            pictureBoxGreen.Location = new Point(330, 24);
            pictureBoxGreen.Name = "pictureBoxGreen";
            pictureBoxGreen.Size = new Size(42, 42);
            pictureBoxGreen.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxGreen.TabIndex = 2;
            pictureBoxGreen.TabStop = false;
            pictureBoxGreen.Click += pictureBoxGreen_Click;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(btnTerminalV1MotorCrimpingreturn);
            groupBox1.Controls.Add(btnTerminalV1MotorCrimping);
            groupBox1.Dock = DockStyle.Top;
            groupBox1.Location = new Point(0, 278);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(493, 74);
            groupBox1.TabIndex = 36;
            groupBox1.TabStop = false;
            groupBox1.Text = "压接电机控制命令（0x29）";
            // 
            // btnTerminalV1MotorCrimpingreturn
            // 
            btnTerminalV1MotorCrimpingreturn.Location = new Point(159, 30);
            btnTerminalV1MotorCrimpingreturn.Margin = new Padding(5, 4, 5, 4);
            btnTerminalV1MotorCrimpingreturn.Name = "btnTerminalV1MotorCrimpingreturn";
            btnTerminalV1MotorCrimpingreturn.Size = new Size(141, 35);
            btnTerminalV1MotorCrimpingreturn.TabIndex = 35;
            btnTerminalV1MotorCrimpingreturn.Text = "电机退压接";
            btnTerminalV1MotorCrimpingreturn.UseVisualStyleBackColor = true;
            btnTerminalV1MotorCrimpingreturn.Click += btnTerminalV1MotorCrimpingreturn_Click;
            // 
            // btnTerminalV1MotorCrimping
            // 
            btnTerminalV1MotorCrimping.Location = new Point(8, 30);
            btnTerminalV1MotorCrimping.Margin = new Padding(5, 4, 5, 4);
            btnTerminalV1MotorCrimping.Name = "btnTerminalV1MotorCrimping";
            btnTerminalV1MotorCrimping.Size = new Size(141, 35);
            btnTerminalV1MotorCrimping.TabIndex = 34;
            btnTerminalV1MotorCrimping.Text = "电机压接";
            btnTerminalV1MotorCrimping.UseVisualStyleBackColor = true;
            btnTerminalV1MotorCrimping.Click += btnTerminalV1MotorCrimping_Click;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(btnChangeTerminalClass);
            groupBox4.Controls.Add(cbxTerminalV1);
            groupBox4.Dock = DockStyle.Top;
            groupBox4.Location = new Point(0, 199);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(493, 79);
            groupBox4.TabIndex = 35;
            groupBox4.TabStop = false;
            groupBox4.Text = "终端类型切换0x2D";
            // 
            // btnChangeTerminalClass
            // 
            btnChangeTerminalClass.Location = new Point(336, 30);
            btnChangeTerminalClass.Margin = new Padding(5, 4, 5, 4);
            btnChangeTerminalClass.Name = "btnChangeTerminalClass";
            btnChangeTerminalClass.Size = new Size(141, 35);
            btnChangeTerminalClass.TabIndex = 18;
            btnChangeTerminalClass.Text = "切换";
            btnChangeTerminalClass.UseVisualStyleBackColor = true;
            btnChangeTerminalClass.Click += btnChangeTerminalClass_Click;
            // 
            // cbxTerminalV1
            // 
            cbxTerminalV1.FormattingEnabled = true;
            cbxTerminalV1.Location = new Point(14, 34);
            cbxTerminalV1.Margin = new Padding(5, 4, 5, 4);
            cbxTerminalV1.Name = "cbxTerminalV1";
            cbxTerminalV1.Size = new Size(321, 32);
            cbxTerminalV1.TabIndex = 15;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(label106);
            groupBox3.Controls.Add(btnTerminalBW_ADown);
            groupBox3.Controls.Add(btnTerminalBW_AOn);
            groupBox3.Controls.Add(cbx_TerminalV1_UC);
            groupBox3.Controls.Add(cbx_TerminalV1_IA);
            groupBox3.Controls.Add(btnTerminalBW_VOn);
            groupBox3.Controls.Add(cbx_TerminalV1_UB);
            groupBox3.Controls.Add(btnTerminalBW_VDown);
            groupBox3.Controls.Add(cbx_TerminalV1_IB);
            groupBox3.Controls.Add(cbx_TerminalV1_UA);
            groupBox3.Controls.Add(cbx_TerminalV1_IC);
            groupBox3.Controls.Add(label25);
            groupBox3.Controls.Add(tbxTerminalAdds);
            groupBox3.Controls.Add(cbx_TerminalV1_IN);
            groupBox3.Dock = DockStyle.Top;
            groupBox3.Location = new Point(0, 0);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(493, 199);
            groupBox3.TabIndex = 34;
            groupBox3.TabStop = false;
            groupBox3.Text = "电压0x21电流0x22";
            // 
            // label106
            // 
            label106.AutoSize = true;
            label106.ForeColor = Color.Red;
            label106.Location = new Point(14, 81);
            label106.Name = "label106";
            label106.Size = new Size(115, 24);
            label106.TabIndex = 6;
            label106.Text = "广播地址255";
            // 
            // btnTerminalBW_ADown
            // 
            btnTerminalBW_ADown.Location = new Point(336, 153);
            btnTerminalBW_ADown.Margin = new Padding(5, 4, 5, 4);
            btnTerminalBW_ADown.Name = "btnTerminalBW_ADown";
            btnTerminalBW_ADown.Size = new Size(141, 35);
            btnTerminalBW_ADown.TabIndex = 32;
            btnTerminalBW_ADown.Text = "表位下电流";
            btnTerminalBW_ADown.UseVisualStyleBackColor = true;
            btnTerminalBW_ADown.Click += btnTerminalBW_ADown_Click;
            // 
            // btnTerminalBW_AOn
            // 
            btnTerminalBW_AOn.Location = new Point(181, 153);
            btnTerminalBW_AOn.Margin = new Padding(5, 4, 5, 4);
            btnTerminalBW_AOn.Name = "btnTerminalBW_AOn";
            btnTerminalBW_AOn.Size = new Size(141, 35);
            btnTerminalBW_AOn.TabIndex = 33;
            btnTerminalBW_AOn.Text = "表位上电流";
            btnTerminalBW_AOn.UseVisualStyleBackColor = true;
            btnTerminalBW_AOn.Click += btnTerminalBW_AOn_Click;
            // 
            // cbx_TerminalV1_UC
            // 
            cbx_TerminalV1_UC.AutoSize = true;
            cbx_TerminalV1_UC.Checked = true;
            cbx_TerminalV1_UC.CheckState = CheckState.Checked;
            cbx_TerminalV1_UC.Location = new Point(336, 42);
            cbx_TerminalV1_UC.Name = "cbx_TerminalV1_UC";
            cbx_TerminalV1_UC.Size = new Size(61, 28);
            cbx_TerminalV1_UC.TabIndex = 24;
            cbx_TerminalV1_UC.Text = "UC";
            cbx_TerminalV1_UC.UseVisualStyleBackColor = true;
            // 
            // cbx_TerminalV1_IA
            // 
            cbx_TerminalV1_IA.AutoSize = true;
            cbx_TerminalV1_IA.Location = new Point(185, 117);
            cbx_TerminalV1_IA.Name = "cbx_TerminalV1_IA";
            cbx_TerminalV1_IA.Size = new Size(54, 28);
            cbx_TerminalV1_IA.TabIndex = 25;
            cbx_TerminalV1_IA.Text = "IA";
            cbx_TerminalV1_IA.UseVisualStyleBackColor = true;
            // 
            // btnTerminalBW_VOn
            // 
            btnTerminalBW_VOn.Location = new Point(181, 75);
            btnTerminalBW_VOn.Margin = new Padding(5, 4, 5, 4);
            btnTerminalBW_VOn.Name = "btnTerminalBW_VOn";
            btnTerminalBW_VOn.Size = new Size(141, 35);
            btnTerminalBW_VOn.TabIndex = 31;
            btnTerminalBW_VOn.Text = "表位上电压";
            btnTerminalBW_VOn.UseVisualStyleBackColor = true;
            btnTerminalBW_VOn.Click += btnTerminalBW_VOn_Click;
            // 
            // cbx_TerminalV1_UB
            // 
            cbx_TerminalV1_UB.AutoSize = true;
            cbx_TerminalV1_UB.Checked = true;
            cbx_TerminalV1_UB.CheckState = CheckState.Checked;
            cbx_TerminalV1_UB.Location = new Point(262, 42);
            cbx_TerminalV1_UB.Name = "cbx_TerminalV1_UB";
            cbx_TerminalV1_UB.Size = new Size(60, 28);
            cbx_TerminalV1_UB.TabIndex = 23;
            cbx_TerminalV1_UB.Text = "UB";
            cbx_TerminalV1_UB.UseVisualStyleBackColor = true;
            // 
            // btnTerminalBW_VDown
            // 
            btnTerminalBW_VDown.Location = new Point(336, 75);
            btnTerminalBW_VDown.Margin = new Padding(5, 4, 5, 4);
            btnTerminalBW_VDown.Name = "btnTerminalBW_VDown";
            btnTerminalBW_VDown.Size = new Size(141, 35);
            btnTerminalBW_VDown.TabIndex = 30;
            btnTerminalBW_VDown.Text = "表位下电压";
            btnTerminalBW_VDown.UseVisualStyleBackColor = true;
            btnTerminalBW_VDown.Click += btnTerminalBW_VDown_Click;
            // 
            // cbx_TerminalV1_IB
            // 
            cbx_TerminalV1_IB.AutoSize = true;
            cbx_TerminalV1_IB.Location = new Point(262, 117);
            cbx_TerminalV1_IB.Name = "cbx_TerminalV1_IB";
            cbx_TerminalV1_IB.Size = new Size(52, 28);
            cbx_TerminalV1_IB.TabIndex = 26;
            cbx_TerminalV1_IB.Text = "IB";
            cbx_TerminalV1_IB.UseVisualStyleBackColor = true;
            // 
            // cbx_TerminalV1_UA
            // 
            cbx_TerminalV1_UA.AutoSize = true;
            cbx_TerminalV1_UA.Checked = true;
            cbx_TerminalV1_UA.CheckState = CheckState.Checked;
            cbx_TerminalV1_UA.Location = new Point(185, 42);
            cbx_TerminalV1_UA.Name = "cbx_TerminalV1_UA";
            cbx_TerminalV1_UA.Size = new Size(62, 28);
            cbx_TerminalV1_UA.TabIndex = 22;
            cbx_TerminalV1_UA.Text = "UA";
            cbx_TerminalV1_UA.UseVisualStyleBackColor = true;
            // 
            // cbx_TerminalV1_IC
            // 
            cbx_TerminalV1_IC.AutoSize = true;
            cbx_TerminalV1_IC.Location = new Point(336, 117);
            cbx_TerminalV1_IC.Name = "cbx_TerminalV1_IC";
            cbx_TerminalV1_IC.Size = new Size(53, 28);
            cbx_TerminalV1_IC.TabIndex = 27;
            cbx_TerminalV1_IC.Text = "IC";
            cbx_TerminalV1_IC.UseVisualStyleBackColor = true;
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.Location = new Point(14, 42);
            label25.Margin = new Padding(5, 0, 5, 0);
            label25.Name = "label25";
            label25.Size = new Size(46, 24);
            label25.TabIndex = 19;
            label25.Text = "地址";
            // 
            // tbxTerminalAdds
            // 
            tbxTerminalAdds.Location = new Point(66, 38);
            tbxTerminalAdds.Margin = new Padding(5, 4, 5, 4);
            tbxTerminalAdds.Name = "tbxTerminalAdds";
            tbxTerminalAdds.Size = new Size(87, 30);
            tbxTerminalAdds.TabIndex = 20;
            tbxTerminalAdds.Text = "1";
            tbxTerminalAdds.KeyPress += TextboxOnlyNumber_KeyPressed;
            // 
            // cbx_TerminalV1_IN
            // 
            cbx_TerminalV1_IN.AutoSize = true;
            cbx_TerminalV1_IN.Location = new Point(402, 117);
            cbx_TerminalV1_IN.Name = "cbx_TerminalV1_IN";
            cbx_TerminalV1_IN.Size = new Size(56, 28);
            cbx_TerminalV1_IN.TabIndex = 29;
            cbx_TerminalV1_IN.Text = "IN";
            cbx_TerminalV1_IN.UseVisualStyleBackColor = true;
            // 
            // tabPage10
            // 
            tabPage10.Location = new Point(4, 33);
            tabPage10.Name = "tabPage10";
            tabPage10.Padding = new Padding(3);
            tabPage10.Size = new Size(1606, 502);
            tabPage10.TabIndex = 1;
            tabPage10.Text = "遥信";
            tabPage10.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            tabPage4.Location = new Point(4, 33);
            tabPage4.Name = "tabPage4";
            tabPage4.Padding = new Padding(3);
            tabPage4.Size = new Size(1620, 545);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "电表测试单元：V1";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // tabPage5
            // 
            tabPage5.Location = new Point(4, 33);
            tabPage5.Name = "tabPage5";
            tabPage5.Padding = new Padding(3);
            tabPage5.Size = new Size(1620, 545);
            tabPage5.TabIndex = 4;
            tabPage5.Text = "终端测试单元：V2";
            tabPage5.UseVisualStyleBackColor = true;
            // 
            // tabPage6
            // 
            tabPage6.Location = new Point(4, 33);
            tabPage6.Name = "tabPage6";
            tabPage6.Padding = new Padding(3);
            tabPage6.Size = new Size(1620, 545);
            tabPage6.TabIndex = 5;
            tabPage6.Text = "电表测试单元：V2";
            tabPage6.UseVisualStyleBackColor = true;
            // 
            // tabPage7
            // 
            tabPage7.Controls.Add(label112);
            tabPage7.Controls.Add(btn_ReadTime);
            tabPage7.Controls.Add(label111);
            tabPage7.Controls.Add(tbxXYMeterPulse);
            tabPage7.Controls.Add(buttonRead_Pulset);
            tabPage7.Controls.Add(groupBox9);
            tabPage7.Controls.Add(groupBox8);
            tabPage7.Controls.Add(label70);
            tabPage7.Controls.Add(btn_SourcePort);
            tabPage7.Controls.Add(tbx_sourcePort);
            tabPage7.Controls.Add(label69);
            tabPage7.Controls.Add(btn_ReadContans);
            tabPage7.Controls.Add(groupBox7);
            tabPage7.Controls.Add(buttonCmdReadMeterData);
            tabPage7.Controls.Add(btn_ReadStandMeter);
            tabPage7.Controls.Add(groupBox6);
            tabPage7.Controls.Add(checkBoxISNOHEX);
            tabPage7.Location = new Point(4, 33);
            tabPage7.Name = "tabPage7";
            tabPage7.Padding = new Padding(3);
            tabPage7.Size = new Size(1620, 545);
            tabPage7.TabIndex = 6;
            tabPage7.Text = "新跃控源参数";
            tabPage7.UseVisualStyleBackColor = true;
            // 
            // label112
            // 
            label112.AutoSize = true;
            label112.ForeColor = Color.Red;
            label112.Location = new Point(530, 61);
            label112.Margin = new Padding(5, 0, 5, 0);
            label112.Name = "label112";
            label112.Size = new Size(124, 24);
            label112.TabIndex = 88;
            label112.Text = "Dll版本日期：";
            // 
            // btn_ReadTime
            // 
            btn_ReadTime.Location = new Point(410, 51);
            btn_ReadTime.Name = "btn_ReadTime";
            btn_ReadTime.Size = new Size(104, 62);
            btn_ReadTime.TabIndex = 87;
            btn_ReadTime.Text = "读取版本日期";
            btn_ReadTime.UseVisualStyleBackColor = true;
            btn_ReadTime.Click += btn_ReadTime_Click;
            // 
            // label111
            // 
            label111.AutoSize = true;
            label111.ForeColor = Color.Red;
            label111.Location = new Point(3, 98);
            label111.Margin = new Padding(5, 0, 5, 0);
            label111.Name = "label111";
            label111.Size = new Size(226, 24);
            label111.TabIndex = 86;
            label111.Text = "读取指定表位的测试脉冲数";
            // 
            // tbxXYMeterPulse
            // 
            tbxXYMeterPulse.Location = new Point(9, 137);
            tbxXYMeterPulse.Margin = new Padding(5, 4, 5, 4);
            tbxXYMeterPulse.Name = "tbxXYMeterPulse";
            tbxXYMeterPulse.Size = new Size(75, 30);
            tbxXYMeterPulse.TabIndex = 21;
            tbxXYMeterPulse.Text = "0";
            // 
            // buttonRead_Pulset
            // 
            buttonRead_Pulset.Location = new Point(93, 133);
            buttonRead_Pulset.Name = "buttonRead_Pulset";
            buttonRead_Pulset.Size = new Size(112, 34);
            buttonRead_Pulset.TabIndex = 33;
            buttonRead_Pulset.Text = "读取脉冲数";
            buttonRead_Pulset.UseVisualStyleBackColor = true;
            buttonRead_Pulset.Click += buttonRead_Pulset_Click;
            // 
            // groupBox9
            // 
            groupBox9.Controls.Add(cbx_HABC);
            groupBox9.Controls.Add(cbx_LC);
            groupBox9.Controls.Add(label94);
            groupBox9.Controls.Add(label93);
            groupBox9.Controls.Add(label92);
            groupBox9.Controls.Add(label91);
            groupBox9.Controls.Add(label90);
            groupBox9.Controls.Add(tbx_A_5);
            groupBox9.Controls.Add(tbx_V_5);
            groupBox9.Controls.Add(btn_XY_ADJ);
            groupBox9.Dock = DockStyle.Right;
            groupBox9.Location = new Point(707, 3);
            groupBox9.Name = "groupBox9";
            groupBox9.Size = new Size(380, 202);
            groupBox9.TabIndex = 32;
            groupBox9.TabStop = false;
            groupBox9.Text = "百分比接口升源";
            // 
            // cbx_HABC
            // 
            cbx_HABC.FormattingEnabled = true;
            cbx_HABC.Items.AddRange(new object[] { "H", "A", "B", "C" });
            cbx_HABC.Location = new Point(9, 122);
            cbx_HABC.Name = "cbx_HABC";
            cbx_HABC.Size = new Size(98, 32);
            cbx_HABC.TabIndex = 84;
            // 
            // cbx_LC
            // 
            cbx_LC.FormattingEnabled = true;
            cbx_LC.Items.AddRange(new object[] { "0.25L", "0.5L", "0.8L", "1.0", "0.8C", "0.5C", "0.25C", "0C", "0.25L-反向", "0.5L-反向", "0.8L-反向", "1.0-反向", "0.8C-反向", "0.5C-反向", "0.25C-反向", "0L-反向" });
            cbx_LC.Location = new Point(8, 165);
            cbx_LC.Name = "cbx_LC";
            cbx_LC.Size = new Size(98, 32);
            cbx_LC.TabIndex = 79;
            // 
            // label94
            // 
            label94.AutoSize = true;
            label94.ForeColor = Color.Red;
            label94.Location = new Point(218, 122);
            label94.Margin = new Padding(5, 0, 5, 0);
            label94.Name = "label94";
            label94.Size = new Size(172, 24);
            label94.TabIndex = 83;
            label94.Text = "圈数（填误差圈数）";
            // 
            // label93
            // 
            label93.AutoSize = true;
            label93.ForeColor = Color.Red;
            label93.Location = new Point(127, 166);
            label93.Margin = new Padding(5, 0, 5, 0);
            label93.Name = "label93";
            label93.Size = new Size(46, 24);
            label93.TabIndex = 82;
            label93.Text = "相位";
            // 
            // label92
            // 
            label92.AutoSize = true;
            label92.ForeColor = Color.Red;
            label92.Location = new Point(127, 129);
            label92.Margin = new Padding(5, 0, 5, 0);
            label92.Name = "label92";
            label92.Size = new Size(64, 24);
            label92.TabIndex = 80;
            label92.Text = "合分元";
            // 
            // label91
            // 
            label91.AutoSize = true;
            label91.ForeColor = Color.Red;
            label91.Location = new Point(127, 85);
            label91.Margin = new Padding(5, 0, 5, 0);
            label91.Name = "label91";
            label91.Size = new Size(100, 24);
            label91.TabIndex = 78;
            label91.Text = "电流百分比";
            // 
            // label90
            // 
            label90.AutoSize = true;
            label90.ForeColor = Color.Red;
            label90.Location = new Point(127, 41);
            label90.Margin = new Padding(5, 0, 5, 0);
            label90.Name = "label90";
            label90.Size = new Size(100, 24);
            label90.TabIndex = 33;
            label90.Text = "电压百分比";
            // 
            // tbx_A_5
            // 
            tbx_A_5.Location = new Point(9, 81);
            tbx_A_5.Margin = new Padding(5, 4, 5, 4);
            tbx_A_5.Name = "tbx_A_5";
            tbx_A_5.Size = new Size(97, 30);
            tbx_A_5.TabIndex = 77;
            tbx_A_5.Text = "0";
            // 
            // tbx_V_5
            // 
            tbx_V_5.Location = new Point(9, 41);
            tbx_V_5.Margin = new Padding(5, 4, 5, 4);
            tbx_V_5.Name = "tbx_V_5";
            tbx_V_5.Size = new Size(97, 30);
            tbx_V_5.TabIndex = 33;
            tbx_V_5.Text = "100";
            // 
            // btn_XY_ADJ
            // 
            btn_XY_ADJ.Location = new Point(262, 157);
            btn_XY_ADJ.Name = "btn_XY_ADJ";
            btn_XY_ADJ.Size = new Size(112, 34);
            btn_XY_ADJ.TabIndex = 76;
            btn_XY_ADJ.Text = "升源ADJ";
            btn_XY_ADJ.UseVisualStyleBackColor = true;
            btn_XY_ADJ.Click += btn_XY_ADJ_Click;
            // 
            // groupBox8
            // 
            groupBox8.Controls.Add(label89);
            groupBox8.Controls.Add(label88);
            groupBox8.Controls.Add(label87);
            groupBox8.Controls.Add(cbx_meterconstant);
            groupBox8.Controls.Add(cbx_ratedcurrent);
            groupBox8.Controls.Add(cbx_ratedvoltage);
            groupBox8.Controls.Add(cbx_Connection);
            groupBox8.Controls.Add(label86);
            groupBox8.Controls.Add(label85);
            groupBox8.Controls.Add(label84);
            groupBox8.Controls.Add(label83);
            groupBox8.Controls.Add(btn_Initmeter);
            groupBox8.Dock = DockStyle.Right;
            groupBox8.Location = new Point(1087, 3);
            groupBox8.Name = "groupBox8";
            groupBox8.Size = new Size(530, 202);
            groupBox8.TabIndex = 31;
            groupBox8.TabStop = false;
            groupBox8.Text = "初始化电表";
            // 
            // label89
            // 
            label89.AutoSize = true;
            label89.Font = new Font("黑体", 10.5F);
            label89.ForeColor = SystemColors.ActiveCaptionText;
            label89.Location = new Point(283, 162);
            label89.Name = "label89";
            label89.Size = new Size(87, 21);
            label89.TabIndex = 78;
            label89.Text = "imp/kwh";
            // 
            // label88
            // 
            label88.AutoSize = true;
            label88.Font = new Font("黑体", 10.5F);
            label88.ForeColor = SystemColors.ActiveCaptionText;
            label88.Location = new Point(283, 87);
            label88.Name = "label88";
            label88.Size = new Size(21, 21);
            label88.TabIndex = 77;
            label88.Text = "V";
            // 
            // label87
            // 
            label87.AutoSize = true;
            label87.Font = new Font("黑体", 10.5F);
            label87.ForeColor = SystemColors.ActiveCaptionText;
            label87.Location = new Point(283, 127);
            label87.Name = "label87";
            label87.Size = new Size(21, 21);
            label87.TabIndex = 76;
            label87.Text = "A";
            // 
            // cbx_meterconstant
            // 
            cbx_meterconstant.FormattingEnabled = true;
            cbx_meterconstant.Items.AddRange(new object[] { "400", "600", "800", "1000", "1200", "6400", "10000" });
            cbx_meterconstant.Location = new Point(140, 159);
            cbx_meterconstant.Name = "cbx_meterconstant";
            cbx_meterconstant.Size = new Size(112, 32);
            cbx_meterconstant.TabIndex = 37;
            // 
            // cbx_ratedcurrent
            // 
            cbx_ratedcurrent.FormattingEnabled = true;
            cbx_ratedcurrent.Items.AddRange(new object[] { "1.5", "5", "10", "20" });
            cbx_ratedcurrent.Location = new Point(140, 122);
            cbx_ratedcurrent.Name = "cbx_ratedcurrent";
            cbx_ratedcurrent.Size = new Size(112, 32);
            cbx_ratedcurrent.TabIndex = 36;
            // 
            // cbx_ratedvoltage
            // 
            cbx_ratedvoltage.FormattingEnabled = true;
            cbx_ratedvoltage.Items.AddRange(new object[] { "57.7", "100", "220", "380", "110", "120" });
            cbx_ratedvoltage.Location = new Point(140, 83);
            cbx_ratedvoltage.Name = "cbx_ratedvoltage";
            cbx_ratedvoltage.Size = new Size(112, 32);
            cbx_ratedvoltage.TabIndex = 35;
            // 
            // cbx_Connection
            // 
            cbx_Connection.FormattingEnabled = true;
            cbx_Connection.Items.AddRange(new object[] { "单相有功", "三相四线有功", "三相三线有功", "90°无功", "60°无功", "四线正弦无功", "三线正弦无功", "三相四线视在", "三相三线视在", "二相三线有功 (AC相)", "单相无功", "单相三线 (AC相)", "单相三线 (BC相) ", "单相三线 (AB相)", "二相三线有功 (BC相)", "二相三线有功 (AB相)", "二相三线无功（AB相）", "二相三线无功（AC相）", "二相三线无功（BC相）" });
            cbx_Connection.Location = new Point(140, 45);
            cbx_Connection.Name = "cbx_Connection";
            cbx_Connection.Size = new Size(281, 32);
            cbx_Connection.TabIndex = 21;
            // 
            // label86
            // 
            label86.AutoSize = true;
            label86.Location = new Point(31, 165);
            label86.Margin = new Padding(5, 0, 5, 0);
            label86.Name = "label86";
            label86.Size = new Size(86, 24);
            label86.TabIndex = 34;
            label86.Text = "电表常数:";
            // 
            // label85
            // 
            label85.AutoSize = true;
            label85.Location = new Point(31, 126);
            label85.Margin = new Padding(5, 0, 5, 0);
            label85.Name = "label85";
            label85.Size = new Size(86, 24);
            label85.TabIndex = 33;
            label85.Text = "额定电流:";
            // 
            // label84
            // 
            label84.AutoSize = true;
            label84.Location = new Point(31, 87);
            label84.Margin = new Padding(5, 0, 5, 0);
            label84.Name = "label84";
            label84.Size = new Size(86, 24);
            label84.TabIndex = 32;
            label84.Text = "额定电压:";
            // 
            // label83
            // 
            label83.AutoSize = true;
            label83.Location = new Point(31, 48);
            label83.Margin = new Padding(5, 0, 5, 0);
            label83.Name = "label83";
            label83.Size = new Size(86, 24);
            label83.TabIndex = 31;
            label83.Text = "接线方式:";
            // 
            // btn_Initmeter
            // 
            btn_Initmeter.Location = new Point(412, 161);
            btn_Initmeter.Name = "btn_Initmeter";
            btn_Initmeter.Size = new Size(112, 34);
            btn_Initmeter.TabIndex = 30;
            btn_Initmeter.Text = "初始化电表";
            btn_Initmeter.UseVisualStyleBackColor = true;
            btn_Initmeter.Click += btn_Init_Click;
            // 
            // label70
            // 
            label70.AutoSize = true;
            label70.ForeColor = Color.Red;
            label70.Location = new Point(482, 10);
            label70.Margin = new Padding(5, 0, 5, 0);
            label70.Name = "label70";
            label70.Size = new Size(103, 24);
            label70.TabIndex = 29;
            label70.Text = "4800;N;8;1";
            // 
            // btn_SourcePort
            // 
            btn_SourcePort.Location = new Point(599, 7);
            btn_SourcePort.Margin = new Padding(5, 4, 5, 4);
            btn_SourcePort.Name = "btn_SourcePort";
            btn_SourcePort.Size = new Size(141, 35);
            btn_SourcePort.TabIndex = 21;
            btn_SourcePort.Text = "OPEN";
            btn_SourcePort.UseVisualStyleBackColor = true;
            btn_SourcePort.Click += btn_SourcePort_Click;
            // 
            // tbx_sourcePort
            // 
            tbx_sourcePort.Location = new Point(365, 7);
            tbx_sourcePort.Margin = new Padding(5, 4, 5, 4);
            tbx_sourcePort.Name = "tbx_sourcePort";
            tbx_sourcePort.Size = new Size(97, 30);
            tbx_sourcePort.TabIndex = 21;
            tbx_sourcePort.KeyPress += TextboxOnlyNumber_KeyPressed;
            // 
            // label69
            // 
            label69.AutoSize = true;
            label69.Location = new Point(288, 10);
            label69.Margin = new Padding(5, 0, 5, 0);
            label69.Name = "label69";
            label69.Size = new Size(68, 24);
            label69.TabIndex = 21;
            label69.Text = "串口号:";
            // 
            // btn_ReadContans
            // 
            btn_ReadContans.Location = new Point(267, 51);
            btn_ReadContans.Name = "btn_ReadContans";
            btn_ReadContans.Size = new Size(112, 34);
            btn_ReadContans.TabIndex = 28;
            btn_ReadContans.Text = "读取常数";
            btn_ReadContans.UseVisualStyleBackColor = true;
            btn_ReadContans.Click += btn_ReadContans_Click;
            // 
            // groupBox7
            // 
            groupBox7.BackColor = Color.Transparent;
            groupBox7.Controls.Add(tabControl3);
            groupBox7.Controls.Add(cbxShutdownUI0);
            groupBox7.Controls.Add(tbxiPulse);
            groupBox7.Controls.Add(label82);
            groupBox7.Controls.Add(cbxUac);
            groupBox7.Controls.Add(cbxUab);
            groupBox7.Controls.Add(label80);
            groupBox7.Controls.Add(label81);
            groupBox7.Controls.Add(cbxICJ);
            groupBox7.Controls.Add(cbxIBJ);
            groupBox7.Controls.Add(cbxIAJ);
            groupBox7.Controls.Add(label77);
            groupBox7.Controls.Add(label78);
            groupBox7.Controls.Add(label79);
            groupBox7.Controls.Add(label65);
            groupBox7.Controls.Add(label66);
            groupBox7.Controls.Add(label67);
            groupBox7.Controls.Add(label62);
            groupBox7.Controls.Add(label63);
            groupBox7.Controls.Add(label64);
            groupBox7.Controls.Add(comboBoxIC);
            groupBox7.Controls.Add(comboBoxIB);
            groupBox7.Controls.Add(comboBoxIA);
            groupBox7.Controls.Add(comboBoxVC);
            groupBox7.Controls.Add(comboBoxVB);
            groupBox7.Controls.Add(comboBoxVA);
            groupBox7.Controls.Add(label59);
            groupBox7.Controls.Add(label56);
            groupBox7.Controls.Add(label60);
            groupBox7.Controls.Add(buttonCtrlUI);
            groupBox7.Controls.Add(label61);
            groupBox7.Controls.Add(label57);
            groupBox7.Controls.Add(buttonXY_x0E);
            groupBox7.Controls.Add(label58);
            groupBox7.Dock = DockStyle.Bottom;
            groupBox7.Location = new Point(3, 205);
            groupBox7.Name = "groupBox7";
            groupBox7.Size = new Size(1614, 166);
            groupBox7.TabIndex = 27;
            groupBox7.TabStop = false;
            groupBox7.Text = "任意控制命令";
            // 
            // tabControl3
            // 
            tabControl3.Controls.Add(tabPage11);
            tabControl3.Controls.Add(tabPage12);
            tabControl3.Controls.Add(tabPage13);
            tabControl3.Controls.Add(tabPage17);
            tabControl3.Dock = DockStyle.Right;
            tabControl3.Location = new Point(844, 26);
            tabControl3.Name = "tabControl3";
            tabControl3.SelectedIndex = 0;
            tabControl3.Size = new Size(767, 137);
            tabControl3.TabIndex = 76;
            // 
            // tabPage11
            // 
            tabPage11.Controls.Add(label97);
            tabPage11.Controls.Add(bttn_settooth);
            tabPage11.Controls.Add(label96);
            tabPage11.Controls.Add(cbbx_ToosNum);
            tabPage11.Controls.Add(label95);
            tabPage11.Controls.Add(cbbx_BlueTooth);
            tabPage11.Location = new Point(4, 33);
            tabPage11.Name = "tabPage11";
            tabPage11.Padding = new Padding(3);
            tabPage11.Size = new Size(759, 100);
            tabPage11.TabIndex = 0;
            tabPage11.Text = "设置蓝牙模式及通道";
            tabPage11.UseVisualStyleBackColor = true;
            // 
            // label97
            // 
            label97.AutoSize = true;
            label97.ForeColor = Color.Red;
            label97.Location = new Point(300, 14);
            label97.Margin = new Padding(5, 0, 5, 0);
            label97.Name = "label97";
            label97.Size = new Size(298, 24);
            label97.TabIndex = 85;
            label97.Text = "备注：先发送接线模式，在发送通道";
            // 
            // bttn_settooth
            // 
            bttn_settooth.Location = new Point(300, 58);
            bttn_settooth.Name = "bttn_settooth";
            bttn_settooth.Size = new Size(112, 34);
            bttn_settooth.TabIndex = 77;
            bttn_settooth.Text = "设置模式";
            bttn_settooth.UseVisualStyleBackColor = true;
            bttn_settooth.Click += bttn_settooth_Click;
            // 
            // label96
            // 
            label96.AutoSize = true;
            label96.Location = new Point(13, 61);
            label96.Margin = new Padding(5, 0, 5, 0);
            label96.Name = "label96";
            label96.Size = new Size(86, 24);
            label96.TabIndex = 80;
            label96.Text = "通道类型:";
            // 
            // cbbx_ToosNum
            // 
            cbbx_ToosNum.FormattingEnabled = true;
            cbbx_ToosNum.Items.AddRange(new object[] { "有功通道", "无功通道" });
            cbbx_ToosNum.Location = new Point(105, 58);
            cbbx_ToosNum.Name = "cbbx_ToosNum";
            cbbx_ToosNum.Size = new Size(178, 32);
            cbbx_ToosNum.TabIndex = 81;
            // 
            // label95
            // 
            label95.AutoSize = true;
            label95.Location = new Point(13, 14);
            label95.Margin = new Padding(5, 0, 5, 0);
            label95.Name = "label95";
            label95.Size = new Size(86, 24);
            label95.TabIndex = 79;
            label95.Text = "蓝牙模式:";
            // 
            // cbbx_BlueTooth
            // 
            cbbx_BlueTooth.FormattingEnabled = true;
            cbbx_BlueTooth.Items.AddRange(new object[] { "常规接线模式", "蓝牙模块接线模式", "双光电头接线模式" });
            cbbx_BlueTooth.Location = new Point(105, 11);
            cbbx_BlueTooth.Name = "cbbx_BlueTooth";
            cbbx_BlueTooth.Size = new Size(178, 32);
            cbbx_BlueTooth.TabIndex = 79;
            // 
            // tabPage12
            // 
            tabPage12.Controls.Add(bttn_ClearError);
            tabPage12.Controls.Add(bttn_StopError);
            tabPage12.Controls.Add(label101);
            tabPage12.Controls.Add(label100);
            tabPage12.Controls.Add(tbx_MeterNo);
            tabPage12.Controls.Add(bttn_ClockStart);
            tabPage12.Controls.Add(label99);
            tabPage12.Controls.Add(tbxclockpulse);
            tabPage12.Controls.Add(label98);
            tabPage12.Location = new Point(4, 33);
            tabPage12.Name = "tabPage12";
            tabPage12.Padding = new Padding(3);
            tabPage12.Size = new Size(759, 100);
            tabPage12.TabIndex = 1;
            tabPage12.Text = "时钟误差";
            tabPage12.UseVisualStyleBackColor = true;
            // 
            // bttn_ClearError
            // 
            bttn_ClearError.Location = new Point(611, 62);
            bttn_ClearError.Margin = new Padding(5, 4, 5, 4);
            bttn_ClearError.Name = "bttn_ClearError";
            bttn_ClearError.Size = new Size(141, 35);
            bttn_ClearError.TabIndex = 91;
            bttn_ClearError.Text = "清除误差";
            bttn_ClearError.UseVisualStyleBackColor = true;
            bttn_ClearError.Click += bttn_ClearError_Click;
            // 
            // bttn_StopError
            // 
            bttn_StopError.Location = new Point(457, 62);
            bttn_StopError.Margin = new Padding(5, 4, 5, 4);
            bttn_StopError.Name = "bttn_StopError";
            bttn_StopError.Size = new Size(141, 35);
            bttn_StopError.TabIndex = 90;
            bttn_StopError.Text = "停止误差";
            bttn_StopError.UseVisualStyleBackColor = true;
            bttn_StopError.Click += bttn_StopError_Click;
            // 
            // label101
            // 
            label101.AutoSize = true;
            label101.ForeColor = Color.Red;
            label101.Location = new Point(226, 3);
            label101.Margin = new Padding(5, 0, 5, 0);
            label101.Name = "label101";
            label101.Size = new Size(278, 24);
            label101.TabIndex = 89;
            label101.Text = "表位按照1-N填写，读取误差需要";
            // 
            // label100
            // 
            label100.AutoSize = true;
            label100.Location = new Point(218, 33);
            label100.Name = "label100";
            label100.Size = new Size(100, 24);
            label100.TabIndex = 88;
            label100.Text = "测试表位：";
            // 
            // tbx_MeterNo
            // 
            tbx_MeterNo.Location = new Point(324, 30);
            tbx_MeterNo.Margin = new Padding(5, 4, 5, 4);
            tbx_MeterNo.Name = "tbx_MeterNo";
            tbx_MeterNo.Size = new Size(97, 30);
            tbx_MeterNo.TabIndex = 87;
            tbx_MeterNo.Text = "1-12";
            // 
            // bttn_ClockStart
            // 
            bttn_ClockStart.Location = new Point(8, 62);
            bttn_ClockStart.Margin = new Padding(5, 4, 5, 4);
            bttn_ClockStart.Name = "bttn_ClockStart";
            bttn_ClockStart.Size = new Size(211, 35);
            bttn_ClockStart.TabIndex = 33;
            bttn_ClockStart.Text = "开始日记时误差";
            bttn_ClockStart.UseVisualStyleBackColor = true;
            bttn_ClockStart.Click += bttn_ClockStart_Click;
            // 
            // label99
            // 
            label99.AutoSize = true;
            label99.Location = new Point(0, 33);
            label99.Name = "label99";
            label99.Size = new Size(118, 24);
            label99.TabIndex = 86;
            label99.Text = "测试脉冲数：";
            // 
            // tbxclockpulse
            // 
            tbxclockpulse.Location = new Point(121, 30);
            tbxclockpulse.Margin = new Padding(5, 4, 5, 4);
            tbxclockpulse.Name = "tbxclockpulse";
            tbxclockpulse.Size = new Size(97, 30);
            tbxclockpulse.TabIndex = 85;
            tbxclockpulse.Text = "10";
            // 
            // label98
            // 
            label98.AutoSize = true;
            label98.ForeColor = Color.Red;
            label98.Location = new Point(2, 3);
            label98.Margin = new Padding(5, 0, 5, 0);
            label98.Name = "label98";
            label98.Size = new Size(154, 24);
            label98.TabIndex = 85;
            label98.Text = "开始测试时钟频率";
            // 
            // tabPage13
            // 
            tabPage13.Controls.Add(tbx_TaskDelay);
            tabPage13.Controls.Add(label105);
            tabPage13.Controls.Add(bttn_ErrorStart);
            tabPage13.Controls.Add(tbx_iPulse);
            tabPage13.Controls.Add(label104);
            tabPage13.Controls.Add(tbx_iMeterCount);
            tabPage13.Controls.Add(label103);
            tabPage13.Controls.Add(tbx_MeterConstant);
            tabPage13.Controls.Add(label102);
            tabPage13.Controls.Add(button3);
            tabPage13.Controls.Add(button4);
            tabPage13.Location = new Point(4, 33);
            tabPage13.Name = "tabPage13";
            tabPage13.Padding = new Padding(3);
            tabPage13.Size = new Size(759, 100);
            tabPage13.TabIndex = 2;
            tabPage13.Text = "基本误差";
            tabPage13.UseVisualStyleBackColor = true;
            // 
            // tbx_TaskDelay
            // 
            tbx_TaskDelay.Location = new Point(632, 7);
            tbx_TaskDelay.Margin = new Padding(5, 4, 5, 4);
            tbx_TaskDelay.Name = "tbx_TaskDelay";
            tbx_TaskDelay.Size = new Size(97, 30);
            tbx_TaskDelay.TabIndex = 102;
            tbx_TaskDelay.Text = "12";
            // 
            // label105
            // 
            label105.AutoSize = true;
            label105.ForeColor = Color.Red;
            label105.Location = new Point(321, 7);
            label105.Margin = new Padding(5, 0, 5, 0);
            label105.Name = "label105";
            label105.Size = new Size(316, 24);
            label105.TabIndex = 101;
            label105.Text = "延迟等待多少时间读取误差（单位秒）";
            // 
            // bttn_ErrorStart
            // 
            bttn_ErrorStart.Location = new Point(310, 61);
            bttn_ErrorStart.Margin = new Padding(5, 4, 5, 4);
            bttn_ErrorStart.Name = "bttn_ErrorStart";
            bttn_ErrorStart.Size = new Size(141, 35);
            bttn_ErrorStart.TabIndex = 100;
            bttn_ErrorStart.Text = "开始误差";
            bttn_ErrorStart.UseVisualStyleBackColor = true;
            bttn_ErrorStart.Click += bttn_ErrorStart_Click;
            // 
            // tbx_iPulse
            // 
            tbx_iPulse.Location = new Point(170, 69);
            tbx_iPulse.Margin = new Padding(5, 4, 5, 4);
            tbx_iPulse.Name = "tbx_iPulse";
            tbx_iPulse.Size = new Size(97, 30);
            tbx_iPulse.TabIndex = 99;
            tbx_iPulse.Text = "4";
            // 
            // label104
            // 
            label104.AutoSize = true;
            label104.Location = new Point(5, 72);
            label104.Name = "label104";
            label104.Size = new Size(172, 24);
            label104.TabIndex = 98;
            label104.Text = "测试圈数或脉冲数：";
            // 
            // tbx_iMeterCount
            // 
            tbx_iMeterCount.Location = new Point(170, 35);
            tbx_iMeterCount.Margin = new Padding(5, 4, 5, 4);
            tbx_iMeterCount.Name = "tbx_iMeterCount";
            tbx_iMeterCount.Size = new Size(97, 30);
            tbx_iMeterCount.TabIndex = 97;
            tbx_iMeterCount.Text = "12";
            // 
            // label103
            // 
            label103.AutoSize = true;
            label103.Location = new Point(5, 38);
            label103.Name = "label103";
            label103.Size = new Size(136, 24);
            label103.TabIndex = 96;
            label103.Text = "装置表位数目：";
            // 
            // tbx_MeterConstant
            // 
            tbx_MeterConstant.Location = new Point(170, 3);
            tbx_MeterConstant.Margin = new Padding(5, 4, 5, 4);
            tbx_MeterConstant.Name = "tbx_MeterConstant";
            tbx_MeterConstant.Size = new Size(97, 30);
            tbx_MeterConstant.TabIndex = 95;
            tbx_MeterConstant.Text = "10000";
            // 
            // label102
            // 
            label102.AutoSize = true;
            label102.Location = new Point(3, 3);
            label102.Name = "label102";
            label102.Size = new Size(118, 24);
            label102.TabIndex = 94;
            label102.Text = "被检表常数：";
            // 
            // button3
            // 
            button3.Location = new Point(614, 59);
            button3.Margin = new Padding(5, 4, 5, 4);
            button3.Name = "button3";
            button3.Size = new Size(141, 35);
            button3.TabIndex = 93;
            button3.Text = "清除误差";
            button3.UseVisualStyleBackColor = true;
            button3.Click += bttn_ClearError_Click;
            // 
            // button4
            // 
            button4.Location = new Point(460, 59);
            button4.Margin = new Padding(5, 4, 5, 4);
            button4.Name = "button4";
            button4.Size = new Size(141, 35);
            button4.TabIndex = 92;
            button4.Text = "停止误差";
            button4.UseVisualStyleBackColor = true;
            button4.Click += bttn_StopError_Click;
            // 
            // tabPage17
            // 
            tabPage17.Controls.Add(label118);
            tabPage17.Controls.Add(textBoxRangeOutputUI);
            tabPage17.Controls.Add(tbxRangeOutputUI);
            tabPage17.Controls.Add(button7);
            tabPage17.Controls.Add(label117);
            tabPage17.Controls.Add(textBoxSetUIRange);
            tabPage17.Location = new Point(4, 33);
            tabPage17.Name = "tabPage17";
            tabPage17.Padding = new Padding(3);
            tabPage17.Size = new Size(759, 100);
            tabPage17.TabIndex = 3;
            tabPage17.Text = "SETUIRangOutUI";
            tabPage17.UseVisualStyleBackColor = true;
            // 
            // label118
            // 
            label118.AutoSize = true;
            label118.ForeColor = Color.Red;
            label118.Location = new Point(426, 6);
            label118.Margin = new Padding(5, 0, 5, 0);
            label118.Name = "label118";
            label118.Size = new Size(252, 24);
            label118.TabIndex = 93;
            label118.Text = "调整的电压值(V)/ 电流值(A)：";
            // 
            // textBoxRangeOutputUI
            // 
            textBoxRangeOutputUI.Location = new Point(426, 34);
            textBoxRangeOutputUI.Margin = new Padding(5, 4, 5, 4);
            textBoxRangeOutputUI.Name = "textBoxRangeOutputUI";
            textBoxRangeOutputUI.Size = new Size(303, 30);
            textBoxRangeOutputUI.TabIndex = 92;
            textBoxRangeOutputUI.Text = "220,110,110,5,0.02,0.02";
            // 
            // tbxRangeOutputUI
            // 
            tbxRangeOutputUI.Location = new Point(426, 62);
            tbxRangeOutputUI.Name = "tbxRangeOutputUI";
            tbxRangeOutputUI.Size = new Size(269, 34);
            tbxRangeOutputUI.TabIndex = 91;
            tbxRangeOutputUI.Text = "RangeOutputUI";
            tbxRangeOutputUI.UseVisualStyleBackColor = true;
            tbxRangeOutputUI.Click += RangeOutputUI_Click;
            // 
            // button7
            // 
            button7.Location = new Point(8, 63);
            button7.Name = "button7";
            button7.Size = new Size(269, 34);
            button7.TabIndex = 90;
            button7.Text = "SetUIRange";
            button7.UseVisualStyleBackColor = true;
            button7.Click += button7_Click;
            // 
            // label117
            // 
            label117.AutoSize = true;
            label117.ForeColor = Color.Red;
            label117.Location = new Point(5, 6);
            label117.Margin = new Padding(5, 0, 5, 0);
            label117.Name = "label117";
            label117.Size = new Size(420, 24);
            label117.TabIndex = 89;
            label117.Text = "设置的电压值(mV)/ 电流值(mA)：0,电压，1，电流";
            // 
            // textBoxSetUIRange
            // 
            textBoxSetUIRange.Location = new Point(8, 34);
            textBoxSetUIRange.Margin = new Padding(5, 4, 5, 4);
            textBoxSetUIRange.Name = "textBoxSetUIRange";
            textBoxSetUIRange.Size = new Size(303, 30);
            textBoxSetUIRange.TabIndex = 85;
            textBoxSetUIRange.Text = "0,220000";
            // 
            // cbxShutdownUI0
            // 
            cbxShutdownUI0.AutoSize = true;
            cbxShutdownUI0.Checked = true;
            cbxShutdownUI0.CheckState = CheckState.Checked;
            cbxShutdownUI0.Location = new Point(869, 117);
            cbxShutdownUI0.Name = "cbxShutdownUI0";
            cbxShutdownUI0.Size = new Size(245, 28);
            cbxShutdownUI0.TabIndex = 75;
            cbxShutdownUI0.Text = "0电压、电流同时停止输出";
            cbxShutdownUI0.UseVisualStyleBackColor = true;
            // 
            // tbxiPulse
            // 
            tbxiPulse.Location = new Point(808, 127);
            tbxiPulse.Margin = new Padding(5, 4, 5, 4);
            tbxiPulse.Name = "tbxiPulse";
            tbxiPulse.Size = new Size(46, 30);
            tbxiPulse.TabIndex = 30;
            tbxiPulse.Text = "2";
            // 
            // label82
            // 
            label82.AutoSize = true;
            label82.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label82.ForeColor = SystemColors.ActiveCaptionText;
            label82.Location = new Point(644, 131);
            label82.Name = "label82";
            label82.Size = new Size(171, 21);
            label82.TabIndex = 74;
            label82.Text = "测试误差圈数：";
            // 
            // cbxUac
            // 
            cbxUac.FormattingEnabled = true;
            cbxUac.Items.AddRange(new object[] { "120", "240" });
            cbxUac.Location = new Point(731, 79);
            cbxUac.Name = "cbxUac";
            cbxUac.Size = new Size(97, 32);
            cbxUac.TabIndex = 73;
            cbxUac.Text = "240";
            // 
            // cbxUab
            // 
            cbxUab.FormattingEnabled = true;
            cbxUab.Items.AddRange(new object[] { "120", "240" });
            cbxUab.Location = new Point(731, 31);
            cbxUab.Name = "cbxUab";
            cbxUab.Size = new Size(97, 32);
            cbxUab.TabIndex = 72;
            cbxUab.Text = "120";
            // 
            // label80
            // 
            label80.AutoSize = true;
            label80.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label80.ForeColor = SystemColors.ActiveCaptionText;
            label80.Location = new Point(644, 79);
            label80.Name = "label80";
            label80.Size = new Size(80, 21);
            label80.TabIndex = 71;
            label80.Text = "Φac：";
            // 
            // label81
            // 
            label81.AutoSize = true;
            label81.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label81.ForeColor = SystemColors.ActiveCaptionText;
            label81.Location = new Point(644, 31);
            label81.Name = "label81";
            label81.Size = new Size(80, 21);
            label81.TabIndex = 70;
            label81.Text = "Φab：";
            // 
            // cbxICJ
            // 
            cbxICJ.FormattingEnabled = true;
            cbxICJ.Items.AddRange(new object[] { "0", "30", "60" });
            cbxICJ.Location = new Point(515, 127);
            cbxICJ.Name = "cbxICJ";
            cbxICJ.Size = new Size(97, 32);
            cbxICJ.TabIndex = 69;
            // 
            // cbxIBJ
            // 
            cbxIBJ.FormattingEnabled = true;
            cbxIBJ.Items.AddRange(new object[] { "0", "30", "60" });
            cbxIBJ.Location = new Point(515, 79);
            cbxIBJ.Name = "cbxIBJ";
            cbxIBJ.Size = new Size(97, 32);
            cbxIBJ.TabIndex = 68;
            // 
            // cbxIAJ
            // 
            cbxIAJ.FormattingEnabled = true;
            cbxIAJ.Items.AddRange(new object[] { "0", "30", "60" });
            cbxIAJ.Location = new Point(515, 31);
            cbxIAJ.Name = "cbxIAJ";
            cbxIAJ.Size = new Size(97, 32);
            cbxIAJ.TabIndex = 67;
            // 
            // label77
            // 
            label77.AutoSize = true;
            label77.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label77.ForeColor = SystemColors.ActiveCaptionText;
            label77.Location = new Point(449, 133);
            label77.Name = "label77";
            label77.Size = new Size(68, 21);
            label77.TabIndex = 66;
            label77.Text = "Φc：";
            // 
            // label78
            // 
            label78.AutoSize = true;
            label78.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label78.ForeColor = SystemColors.ActiveCaptionText;
            label78.Location = new Point(449, 85);
            label78.Name = "label78";
            label78.Size = new Size(68, 21);
            label78.TabIndex = 65;
            label78.Text = "Φb：";
            // 
            // label79
            // 
            label79.AutoSize = true;
            label79.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label79.ForeColor = SystemColors.ActiveCaptionText;
            label79.Location = new Point(449, 37);
            label79.Name = "label79";
            label79.Size = new Size(68, 21);
            label79.TabIndex = 64;
            label79.Text = "Φa：";
            // 
            // label65
            // 
            label65.AutoSize = true;
            label65.Font = new Font("黑体", 10.5F);
            label65.ForeColor = SystemColors.ActiveCaptionText;
            label65.Location = new Point(412, 131);
            label65.Name = "label65";
            label65.Size = new Size(21, 21);
            label65.TabIndex = 63;
            label65.Text = "A";
            // 
            // label66
            // 
            label66.AutoSize = true;
            label66.Font = new Font("黑体", 10.5F);
            label66.ForeColor = SystemColors.ActiveCaptionText;
            label66.Location = new Point(412, 83);
            label66.Name = "label66";
            label66.Size = new Size(21, 21);
            label66.TabIndex = 62;
            label66.Text = "A";
            // 
            // label67
            // 
            label67.AutoSize = true;
            label67.Font = new Font("黑体", 10.5F);
            label67.ForeColor = SystemColors.ActiveCaptionText;
            label67.Location = new Point(412, 35);
            label67.Name = "label67";
            label67.Size = new Size(21, 21);
            label67.TabIndex = 61;
            label67.Text = "A";
            // 
            // label62
            // 
            label62.AutoSize = true;
            label62.Font = new Font("黑体", 10.5F);
            label62.ForeColor = SystemColors.ActiveCaptionText;
            label62.Location = new Point(192, 131);
            label62.Name = "label62";
            label62.Size = new Size(21, 21);
            label62.TabIndex = 60;
            label62.Text = "V";
            // 
            // label63
            // 
            label63.AutoSize = true;
            label63.Font = new Font("黑体", 10.5F);
            label63.ForeColor = SystemColors.ActiveCaptionText;
            label63.Location = new Point(192, 83);
            label63.Name = "label63";
            label63.Size = new Size(21, 21);
            label63.TabIndex = 59;
            label63.Text = "V";
            // 
            // label64
            // 
            label64.AutoSize = true;
            label64.Font = new Font("黑体", 10.5F);
            label64.ForeColor = SystemColors.ActiveCaptionText;
            label64.Location = new Point(192, 35);
            label64.Name = "label64";
            label64.Size = new Size(21, 21);
            label64.TabIndex = 58;
            label64.Text = "V";
            // 
            // comboBoxIC
            // 
            comboBoxIC.FormattingEnabled = true;
            comboBoxIC.Items.AddRange(new object[] { "5", "1", "0.75", "0.15" });
            comboBoxIC.Location = new Point(292, 127);
            comboBoxIC.Name = "comboBoxIC";
            comboBoxIC.Size = new Size(97, 32);
            comboBoxIC.TabIndex = 57;
            // 
            // comboBoxIB
            // 
            comboBoxIB.FormattingEnabled = true;
            comboBoxIB.Items.AddRange(new object[] { "5", "1", "0.75", "0.15" });
            comboBoxIB.Location = new Point(292, 79);
            comboBoxIB.Name = "comboBoxIB";
            comboBoxIB.Size = new Size(97, 32);
            comboBoxIB.TabIndex = 56;
            // 
            // comboBoxIA
            // 
            comboBoxIA.FormattingEnabled = true;
            comboBoxIA.Items.AddRange(new object[] { "5", "1", "0.75", "0.15" });
            comboBoxIA.Location = new Point(292, 31);
            comboBoxIA.Name = "comboBoxIA";
            comboBoxIA.Size = new Size(97, 32);
            comboBoxIA.TabIndex = 55;
            // 
            // comboBoxVC
            // 
            comboBoxVC.FormattingEnabled = true;
            comboBoxVC.Items.AddRange(new object[] { "57.7", "100", "220", "240", "380" });
            comboBoxVC.Location = new Point(82, 127);
            comboBoxVC.Name = "comboBoxVC";
            comboBoxVC.Size = new Size(97, 32);
            comboBoxVC.TabIndex = 54;
            // 
            // comboBoxVB
            // 
            comboBoxVB.FormattingEnabled = true;
            comboBoxVB.Items.AddRange(new object[] { "57.7", "100", "220", "240", "380" });
            comboBoxVB.Location = new Point(82, 79);
            comboBoxVB.Name = "comboBoxVB";
            comboBoxVB.Size = new Size(97, 32);
            comboBoxVB.TabIndex = 53;
            // 
            // comboBoxVA
            // 
            comboBoxVA.FormattingEnabled = true;
            comboBoxVA.Items.AddRange(new object[] { "57.7", "100", "220", "240", "380" });
            comboBoxVA.Location = new Point(82, 31);
            comboBoxVA.Name = "comboBoxVA";
            comboBoxVA.Size = new Size(97, 32);
            comboBoxVA.TabIndex = 21;
            // 
            // label59
            // 
            label59.AutoSize = true;
            label59.Font = new Font("黑体", 10.5F, FontStyle.Bold | FontStyle.Italic);
            label59.ForeColor = SystemColors.ActiveCaptionText;
            label59.Location = new Point(223, 131);
            label59.Name = "label59";
            label59.Size = new Size(57, 21);
            label59.TabIndex = 52;
            label59.Text = "Ic：";
            // 
            // label56
            // 
            label56.AutoSize = true;
            label56.Font = new Font("黑体", 10.5F, FontStyle.Bold | FontStyle.Italic);
            label56.ForeColor = SystemColors.ActiveCaptionText;
            label56.Location = new Point(19, 131);
            label56.Name = "label56";
            label56.Size = new Size(57, 21);
            label56.TabIndex = 52;
            label56.Text = "Uc：";
            // 
            // label60
            // 
            label60.AutoSize = true;
            label60.Font = new Font("黑体", 10.5F, FontStyle.Bold | FontStyle.Italic);
            label60.ForeColor = SystemColors.ActiveCaptionText;
            label60.Location = new Point(223, 83);
            label60.Name = "label60";
            label60.Size = new Size(57, 21);
            label60.TabIndex = 51;
            label60.Text = "Ib：";
            // 
            // buttonCtrlUI
            // 
            buttonCtrlUI.Location = new Point(869, 28);
            buttonCtrlUI.Name = "buttonCtrlUI";
            buttonCtrlUI.Size = new Size(112, 34);
            buttonCtrlUI.TabIndex = 26;
            buttonCtrlUI.Text = "升源";
            buttonCtrlUI.UseVisualStyleBackColor = true;
            buttonCtrlUI.Click += buttonCtrlUI_Click;
            // 
            // label61
            // 
            label61.AutoSize = true;
            label61.Font = new Font("黑体", 10.5F, FontStyle.Bold | FontStyle.Italic);
            label61.ForeColor = SystemColors.ActiveCaptionText;
            label61.Location = new Point(223, 35);
            label61.Name = "label61";
            label61.Size = new Size(57, 21);
            label61.TabIndex = 50;
            label61.Text = "Ia：";
            // 
            // label57
            // 
            label57.AutoSize = true;
            label57.Font = new Font("黑体", 10.5F, FontStyle.Bold | FontStyle.Italic);
            label57.ForeColor = SystemColors.ActiveCaptionText;
            label57.Location = new Point(19, 83);
            label57.Name = "label57";
            label57.Size = new Size(57, 21);
            label57.TabIndex = 51;
            label57.Text = "Ub：";
            // 
            // buttonXY_x0E
            // 
            buttonXY_x0E.Location = new Point(869, 78);
            buttonXY_x0E.Name = "buttonXY_x0E";
            buttonXY_x0E.Size = new Size(112, 34);
            buttonXY_x0E.TabIndex = 0;
            buttonXY_x0E.Text = "降源x0E";
            buttonXY_x0E.UseVisualStyleBackColor = true;
            buttonXY_x0E.Click += buttonXY_x0E_Click;
            // 
            // label58
            // 
            label58.AutoSize = true;
            label58.Font = new Font("黑体", 10.5F, FontStyle.Bold | FontStyle.Italic);
            label58.ForeColor = SystemColors.ActiveCaptionText;
            label58.Location = new Point(19, 35);
            label58.Name = "label58";
            label58.Size = new Size(57, 21);
            label58.TabIndex = 50;
            label58.Text = "Ua：";
            // 
            // buttonCmdReadMeterData
            // 
            buttonCmdReadMeterData.Location = new Point(3, 51);
            buttonCmdReadMeterData.Name = "buttonCmdReadMeterData";
            buttonCmdReadMeterData.Size = new Size(112, 34);
            buttonCmdReadMeterData.TabIndex = 25;
            buttonCmdReadMeterData.Text = "读装置信息";
            buttonCmdReadMeterData.UseVisualStyleBackColor = true;
            buttonCmdReadMeterData.Click += CmdReadMeterData_Click;
            // 
            // btn_ReadStandMeter
            // 
            btn_ReadStandMeter.Location = new Point(134, 51);
            btn_ReadStandMeter.Name = "btn_ReadStandMeter";
            btn_ReadStandMeter.Size = new Size(112, 34);
            btn_ReadStandMeter.TabIndex = 24;
            btn_ReadStandMeter.Text = "读取标准表";
            btn_ReadStandMeter.UseVisualStyleBackColor = true;
            btn_ReadStandMeter.Click += btn_ReadStandMeter_Click;
            // 
            // groupBox6
            // 
            groupBox6.BackColor = Color.DarkGray;
            groupBox6.Controls.Add(tb_A_LC);
            groupBox6.Controls.Add(tb_V_LC);
            groupBox6.Controls.Add(tb_xx);
            groupBox6.Controls.Add(label74);
            groupBox6.Controls.Add(label75);
            groupBox6.Controls.Add(label76);
            groupBox6.Controls.Add(tb_Uca);
            groupBox6.Controls.Add(label72);
            groupBox6.Controls.Add(tb_Uba);
            groupBox6.Controls.Add(label73);
            groupBox6.Controls.Add(tb_Alarm);
            groupBox6.Controls.Add(label71);
            groupBox6.Controls.Add(tb_contans);
            groupBox6.Controls.Add(label68);
            groupBox6.Controls.Add(tb_HZ);
            groupBox6.Controls.Add(label55);
            groupBox6.Controls.Add(tb_EQ);
            groupBox6.Controls.Add(tb_EP);
            groupBox6.Controls.Add(tb_ES);
            groupBox6.Controls.Add(tb_XC);
            groupBox6.Controls.Add(tb_XB);
            groupBox6.Controls.Add(tb_XA);
            groupBox6.Controls.Add(tb_PFC);
            groupBox6.Controls.Add(tb_PFB);
            groupBox6.Controls.Add(tb_PFA);
            groupBox6.Controls.Add(tb_SC);
            groupBox6.Controls.Add(tb_SB);
            groupBox6.Controls.Add(tb_SA);
            groupBox6.Controls.Add(tb_QC);
            groupBox6.Controls.Add(tb_QB);
            groupBox6.Controls.Add(tb_QA);
            groupBox6.Controls.Add(tb_PC);
            groupBox6.Controls.Add(tb_PB);
            groupBox6.Controls.Add(tb_PA);
            groupBox6.Controls.Add(tb_IC);
            groupBox6.Controls.Add(tb_IB);
            groupBox6.Controls.Add(tb_IA);
            groupBox6.Controls.Add(tb_UC);
            groupBox6.Controls.Add(tb_UB);
            groupBox6.Controls.Add(tb_UA);
            groupBox6.Controls.Add(label52);
            groupBox6.Controls.Add(label53);
            groupBox6.Controls.Add(label54);
            groupBox6.Controls.Add(label49);
            groupBox6.Controls.Add(label50);
            groupBox6.Controls.Add(label51);
            groupBox6.Controls.Add(label46);
            groupBox6.Controls.Add(label47);
            groupBox6.Controls.Add(label48);
            groupBox6.Controls.Add(label43);
            groupBox6.Controls.Add(label44);
            groupBox6.Controls.Add(label45);
            groupBox6.Controls.Add(label40);
            groupBox6.Controls.Add(label41);
            groupBox6.Controls.Add(label42);
            groupBox6.Controls.Add(label37);
            groupBox6.Controls.Add(label38);
            groupBox6.Controls.Add(label39);
            groupBox6.Controls.Add(label34);
            groupBox6.Controls.Add(label35);
            groupBox6.Controls.Add(label36);
            groupBox6.Controls.Add(label33);
            groupBox6.Controls.Add(label32);
            groupBox6.Controls.Add(label31);
            groupBox6.Dock = DockStyle.Bottom;
            groupBox6.ForeColor = Color.Gray;
            groupBox6.Location = new Point(3, 371);
            groupBox6.Name = "groupBox6";
            groupBox6.Size = new Size(1614, 171);
            groupBox6.TabIndex = 23;
            groupBox6.TabStop = false;
            groupBox6.Text = "仪表台";
            // 
            // tb_A_LC
            // 
            tb_A_LC.BorderStyle = BorderStyle.None;
            tb_A_LC.Location = new Point(1779, 127);
            tb_A_LC.Name = "tb_A_LC";
            tb_A_LC.ReadOnly = true;
            tb_A_LC.Size = new Size(101, 23);
            tb_A_LC.TabIndex = 63;
            // 
            // tb_V_LC
            // 
            tb_V_LC.BorderStyle = BorderStyle.None;
            tb_V_LC.Location = new Point(1779, 81);
            tb_V_LC.Name = "tb_V_LC";
            tb_V_LC.ReadOnly = true;
            tb_V_LC.Size = new Size(101, 23);
            tb_V_LC.TabIndex = 62;
            // 
            // tb_xx
            // 
            tb_xx.BorderStyle = BorderStyle.None;
            tb_xx.Location = new Point(1779, 31);
            tb_xx.Name = "tb_xx";
            tb_xx.ReadOnly = true;
            tb_xx.Size = new Size(101, 23);
            tb_xx.TabIndex = 61;
            // 
            // label74
            // 
            label74.AutoSize = true;
            label74.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label74.ForeColor = Color.Black;
            label74.Location = new Point(1652, 81);
            label74.Name = "label74";
            label74.Size = new Size(125, 21);
            label74.TabIndex = 60;
            label74.Text = "电压量程：";
            // 
            // label75
            // 
            label75.AutoSize = true;
            label75.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label75.ForeColor = SystemColors.ActiveCaptionText;
            label75.Location = new Point(1652, 129);
            label75.Name = "label75";
            label75.Size = new Size(125, 21);
            label75.TabIndex = 59;
            label75.Text = "电流量程：";
            // 
            // label76
            // 
            label76.AutoSize = true;
            label76.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label76.ForeColor = Color.Black;
            label76.Location = new Point(1652, 33);
            label76.Name = "label76";
            label76.Size = new Size(79, 21);
            label76.TabIndex = 58;
            label76.Text = "相线：";
            // 
            // tb_Uca
            // 
            tb_Uca.BorderStyle = BorderStyle.None;
            tb_Uca.Location = new Point(1542, 79);
            tb_Uca.Name = "tb_Uca";
            tb_Uca.ReadOnly = true;
            tb_Uca.Size = new Size(101, 23);
            tb_Uca.TabIndex = 57;
            // 
            // label72
            // 
            label72.AutoSize = true;
            label72.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label72.ForeColor = Color.Black;
            label72.Location = new Point(1472, 89);
            label72.Name = "label72";
            label72.Size = new Size(58, 21);
            label72.TabIndex = 56;
            label72.Text = "Uca:";
            // 
            // tb_Uba
            // 
            tb_Uba.BorderStyle = BorderStyle.None;
            tb_Uba.Location = new Point(1542, 31);
            tb_Uba.Name = "tb_Uba";
            tb_Uba.ReadOnly = true;
            tb_Uba.Size = new Size(101, 23);
            tb_Uba.TabIndex = 55;
            // 
            // label73
            // 
            label73.AutoSize = true;
            label73.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label73.ForeColor = Color.Black;
            label73.Location = new Point(1472, 41);
            label73.Name = "label73";
            label73.Size = new Size(58, 21);
            label73.TabIndex = 54;
            label73.Text = "Uba:";
            // 
            // tb_Alarm
            // 
            tb_Alarm.BorderStyle = BorderStyle.None;
            tb_Alarm.Location = new Point(1356, 79);
            tb_Alarm.Name = "tb_Alarm";
            tb_Alarm.ReadOnly = true;
            tb_Alarm.Size = new Size(101, 23);
            tb_Alarm.TabIndex = 53;
            // 
            // label71
            // 
            label71.AutoSize = true;
            label71.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label71.ForeColor = Color.Black;
            label71.Location = new Point(1287, 89);
            label71.Name = "label71";
            label71.Size = new Size(79, 21);
            label71.TabIndex = 52;
            label71.Text = "报警：";
            // 
            // tb_contans
            // 
            tb_contans.BorderStyle = BorderStyle.None;
            tb_contans.Location = new Point(1372, 127);
            tb_contans.Name = "tb_contans";
            tb_contans.ReadOnly = true;
            tb_contans.Size = new Size(181, 23);
            tb_contans.TabIndex = 51;
            // 
            // label68
            // 
            label68.AutoSize = true;
            label68.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label68.ForeColor = SystemColors.ActiveCaptionText;
            label68.Location = new Point(1287, 137);
            label68.Name = "label68";
            label68.Size = new Size(79, 21);
            label68.TabIndex = 50;
            label68.Text = "常数：";
            // 
            // tb_HZ
            // 
            tb_HZ.BorderStyle = BorderStyle.None;
            tb_HZ.Location = new Point(1356, 31);
            tb_HZ.Name = "tb_HZ";
            tb_HZ.ReadOnly = true;
            tb_HZ.Size = new Size(101, 23);
            tb_HZ.TabIndex = 49;
            // 
            // label55
            // 
            label55.AutoSize = true;
            label55.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label55.ForeColor = Color.Black;
            label55.Location = new Point(1287, 41);
            label55.Name = "label55";
            label55.Size = new Size(57, 21);
            label55.TabIndex = 48;
            label55.Text = "HZ：";
            // 
            // tb_EQ
            // 
            tb_EQ.BorderStyle = BorderStyle.None;
            tb_EQ.Location = new Point(1171, 137);
            tb_EQ.Name = "tb_EQ";
            tb_EQ.ReadOnly = true;
            tb_EQ.Size = new Size(101, 23);
            tb_EQ.TabIndex = 47;
            // 
            // tb_EP
            // 
            tb_EP.BorderStyle = BorderStyle.None;
            tb_EP.Location = new Point(1171, 89);
            tb_EP.Name = "tb_EP";
            tb_EP.ReadOnly = true;
            tb_EP.Size = new Size(101, 23);
            tb_EP.TabIndex = 46;
            // 
            // tb_ES
            // 
            tb_ES.BorderStyle = BorderStyle.None;
            tb_ES.Location = new Point(1171, 41);
            tb_ES.Name = "tb_ES";
            tb_ES.ReadOnly = true;
            tb_ES.Size = new Size(101, 23);
            tb_ES.TabIndex = 45;
            // 
            // tb_XC
            // 
            tb_XC.BorderStyle = BorderStyle.None;
            tb_XC.Location = new Point(1010, 137);
            tb_XC.Name = "tb_XC";
            tb_XC.ReadOnly = true;
            tb_XC.Size = new Size(101, 23);
            tb_XC.TabIndex = 44;
            // 
            // tb_XB
            // 
            tb_XB.BorderStyle = BorderStyle.None;
            tb_XB.Location = new Point(1010, 89);
            tb_XB.Name = "tb_XB";
            tb_XB.ReadOnly = true;
            tb_XB.Size = new Size(101, 23);
            tb_XB.TabIndex = 43;
            // 
            // tb_XA
            // 
            tb_XA.BorderStyle = BorderStyle.None;
            tb_XA.Location = new Point(1010, 41);
            tb_XA.Name = "tb_XA";
            tb_XA.ReadOnly = true;
            tb_XA.Size = new Size(101, 23);
            tb_XA.TabIndex = 42;
            // 
            // tb_PFC
            // 
            tb_PFC.BorderStyle = BorderStyle.None;
            tb_PFC.Location = new Point(852, 137);
            tb_PFC.Name = "tb_PFC";
            tb_PFC.ReadOnly = true;
            tb_PFC.Size = new Size(101, 23);
            tb_PFC.TabIndex = 41;
            // 
            // tb_PFB
            // 
            tb_PFB.BorderStyle = BorderStyle.None;
            tb_PFB.Location = new Point(852, 89);
            tb_PFB.Name = "tb_PFB";
            tb_PFB.ReadOnly = true;
            tb_PFB.Size = new Size(101, 23);
            tb_PFB.TabIndex = 40;
            // 
            // tb_PFA
            // 
            tb_PFA.BorderStyle = BorderStyle.None;
            tb_PFA.Location = new Point(852, 41);
            tb_PFA.Name = "tb_PFA";
            tb_PFA.ReadOnly = true;
            tb_PFA.Size = new Size(101, 23);
            tb_PFA.TabIndex = 39;
            // 
            // tb_SC
            // 
            tb_SC.BorderStyle = BorderStyle.None;
            tb_SC.Location = new Point(693, 137);
            tb_SC.Name = "tb_SC";
            tb_SC.ReadOnly = true;
            tb_SC.Size = new Size(101, 23);
            tb_SC.TabIndex = 38;
            // 
            // tb_SB
            // 
            tb_SB.BorderStyle = BorderStyle.None;
            tb_SB.Location = new Point(693, 89);
            tb_SB.Name = "tb_SB";
            tb_SB.ReadOnly = true;
            tb_SB.Size = new Size(101, 23);
            tb_SB.TabIndex = 37;
            // 
            // tb_SA
            // 
            tb_SA.BorderStyle = BorderStyle.None;
            tb_SA.Location = new Point(693, 41);
            tb_SA.Name = "tb_SA";
            tb_SA.ReadOnly = true;
            tb_SA.Size = new Size(101, 23);
            tb_SA.TabIndex = 36;
            // 
            // tb_QC
            // 
            tb_QC.BorderStyle = BorderStyle.None;
            tb_QC.Location = new Point(534, 138);
            tb_QC.Name = "tb_QC";
            tb_QC.ReadOnly = true;
            tb_QC.Size = new Size(101, 23);
            tb_QC.TabIndex = 35;
            // 
            // tb_QB
            // 
            tb_QB.BorderStyle = BorderStyle.None;
            tb_QB.Location = new Point(534, 90);
            tb_QB.Name = "tb_QB";
            tb_QB.ReadOnly = true;
            tb_QB.Size = new Size(101, 23);
            tb_QB.TabIndex = 34;
            // 
            // tb_QA
            // 
            tb_QA.BorderStyle = BorderStyle.None;
            tb_QA.Location = new Point(534, 42);
            tb_QA.Name = "tb_QA";
            tb_QA.ReadOnly = true;
            tb_QA.Size = new Size(101, 23);
            tb_QA.TabIndex = 33;
            // 
            // tb_PC
            // 
            tb_PC.BorderStyle = BorderStyle.None;
            tb_PC.Location = new Point(376, 138);
            tb_PC.Name = "tb_PC";
            tb_PC.ReadOnly = true;
            tb_PC.Size = new Size(101, 23);
            tb_PC.TabIndex = 32;
            // 
            // tb_PB
            // 
            tb_PB.BorderStyle = BorderStyle.None;
            tb_PB.Location = new Point(376, 90);
            tb_PB.Name = "tb_PB";
            tb_PB.ReadOnly = true;
            tb_PB.Size = new Size(101, 23);
            tb_PB.TabIndex = 31;
            // 
            // tb_PA
            // 
            tb_PA.BorderStyle = BorderStyle.None;
            tb_PA.Location = new Point(376, 42);
            tb_PA.Name = "tb_PA";
            tb_PA.ReadOnly = true;
            tb_PA.Size = new Size(101, 23);
            tb_PA.TabIndex = 30;
            // 
            // tb_IC
            // 
            tb_IC.BorderStyle = BorderStyle.None;
            tb_IC.Location = new Point(215, 137);
            tb_IC.Name = "tb_IC";
            tb_IC.ReadOnly = true;
            tb_IC.Size = new Size(101, 23);
            tb_IC.TabIndex = 29;
            // 
            // tb_IB
            // 
            tb_IB.BorderStyle = BorderStyle.None;
            tb_IB.Location = new Point(215, 89);
            tb_IB.Name = "tb_IB";
            tb_IB.ReadOnly = true;
            tb_IB.Size = new Size(101, 23);
            tb_IB.TabIndex = 28;
            // 
            // tb_IA
            // 
            tb_IA.BorderStyle = BorderStyle.None;
            tb_IA.Location = new Point(215, 41);
            tb_IA.Name = "tb_IA";
            tb_IA.ReadOnly = true;
            tb_IA.Size = new Size(101, 23);
            tb_IA.TabIndex = 27;
            // 
            // tb_UC
            // 
            tb_UC.BorderStyle = BorderStyle.None;
            tb_UC.Location = new Point(57, 137);
            tb_UC.Name = "tb_UC";
            tb_UC.ReadOnly = true;
            tb_UC.Size = new Size(101, 23);
            tb_UC.TabIndex = 26;
            // 
            // tb_UB
            // 
            tb_UB.BorderStyle = BorderStyle.None;
            tb_UB.Location = new Point(57, 89);
            tb_UB.Name = "tb_UB";
            tb_UB.ReadOnly = true;
            tb_UB.Size = new Size(101, 23);
            tb_UB.TabIndex = 25;
            // 
            // tb_UA
            // 
            tb_UA.BorderStyle = BorderStyle.None;
            tb_UA.Location = new Point(57, 41);
            tb_UA.Name = "tb_UA";
            tb_UA.ReadOnly = true;
            tb_UA.Size = new Size(101, 23);
            tb_UA.TabIndex = 24;
            // 
            // label52
            // 
            label52.AutoSize = true;
            label52.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label52.ForeColor = Color.Red;
            label52.Location = new Point(1114, 140);
            label52.Name = "label52";
            label52.Size = new Size(68, 21);
            label52.TabIndex = 23;
            label52.Text = "ΣQ：";
            // 
            // label53
            // 
            label53.AutoSize = true;
            label53.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label53.ForeColor = Color.Green;
            label53.Location = new Point(1114, 92);
            label53.Name = "label53";
            label53.Size = new Size(68, 21);
            label53.TabIndex = 22;
            label53.Text = "ΣP：";
            // 
            // label54
            // 
            label54.AutoSize = true;
            label54.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label54.ForeColor = Color.Yellow;
            label54.Location = new Point(1114, 44);
            label54.Name = "label54";
            label54.Size = new Size(68, 21);
            label54.TabIndex = 21;
            label54.Text = "ΣS：";
            // 
            // label49
            // 
            label49.AutoSize = true;
            label49.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label49.ForeColor = Color.Red;
            label49.Location = new Point(960, 140);
            label49.Name = "label49";
            label49.Size = new Size(56, 21);
            label49.TabIndex = 20;
            label49.Text = "Φ：";
            // 
            // label50
            // 
            label50.AutoSize = true;
            label50.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label50.ForeColor = Color.Green;
            label50.Location = new Point(960, 92);
            label50.Name = "label50";
            label50.Size = new Size(56, 21);
            label50.TabIndex = 19;
            label50.Text = "Φ：";
            // 
            // label51
            // 
            label51.AutoSize = true;
            label51.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label51.ForeColor = Color.Yellow;
            label51.Location = new Point(960, 44);
            label51.Name = "label51";
            label51.Size = new Size(56, 21);
            label51.TabIndex = 18;
            label51.Text = "Φ：";
            // 
            // label46
            // 
            label46.AutoSize = true;
            label46.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label46.ForeColor = Color.Red;
            label46.Location = new Point(797, 140);
            label46.Name = "label46";
            label46.Size = new Size(69, 21);
            label46.TabIndex = 17;
            label46.Text = "Pfc：";
            // 
            // label47
            // 
            label47.AutoSize = true;
            label47.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label47.ForeColor = Color.Green;
            label47.Location = new Point(797, 92);
            label47.Name = "label47";
            label47.Size = new Size(69, 21);
            label47.TabIndex = 16;
            label47.Text = "Pfb：";
            // 
            // label48
            // 
            label48.AutoSize = true;
            label48.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label48.ForeColor = Color.Yellow;
            label48.Location = new Point(797, 44);
            label48.Name = "label48";
            label48.Size = new Size(69, 21);
            label48.TabIndex = 15;
            label48.Text = "Pfa：";
            // 
            // label43
            // 
            label43.AutoSize = true;
            label43.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label43.ForeColor = Color.Red;
            label43.Location = new Point(643, 140);
            label43.Name = "label43";
            label43.Size = new Size(57, 21);
            label43.TabIndex = 14;
            label43.Text = "Sc：";
            // 
            // label44
            // 
            label44.AutoSize = true;
            label44.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label44.ForeColor = Color.Green;
            label44.Location = new Point(643, 92);
            label44.Name = "label44";
            label44.Size = new Size(57, 21);
            label44.TabIndex = 13;
            label44.Text = "Sb：";
            // 
            // label45
            // 
            label45.AutoSize = true;
            label45.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label45.ForeColor = Color.Yellow;
            label45.Location = new Point(643, 44);
            label45.Name = "label45";
            label45.Size = new Size(57, 21);
            label45.TabIndex = 12;
            label45.Text = "Sa：";
            // 
            // label40
            // 
            label40.AutoSize = true;
            label40.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label40.ForeColor = Color.Red;
            label40.Location = new Point(489, 140);
            label40.Name = "label40";
            label40.Size = new Size(57, 21);
            label40.TabIndex = 11;
            label40.Text = "Qc：";
            // 
            // label41
            // 
            label41.AutoSize = true;
            label41.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label41.ForeColor = Color.Green;
            label41.Location = new Point(489, 92);
            label41.Name = "label41";
            label41.Size = new Size(57, 21);
            label41.TabIndex = 10;
            label41.Text = "Qb：";
            // 
            // label42
            // 
            label42.AutoSize = true;
            label42.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label42.ForeColor = Color.Yellow;
            label42.Location = new Point(489, 44);
            label42.Name = "label42";
            label42.Size = new Size(57, 21);
            label42.TabIndex = 9;
            label42.Text = "Qa：";
            // 
            // label37
            // 
            label37.AutoSize = true;
            label37.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label37.ForeColor = Color.Red;
            label37.Location = new Point(324, 140);
            label37.Name = "label37";
            label37.Size = new Size(57, 21);
            label37.TabIndex = 8;
            label37.Text = "Pc：";
            // 
            // label38
            // 
            label38.AutoSize = true;
            label38.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label38.ForeColor = Color.Green;
            label38.Location = new Point(324, 92);
            label38.Name = "label38";
            label38.Size = new Size(57, 21);
            label38.TabIndex = 7;
            label38.Text = "Pb：";
            // 
            // label39
            // 
            label39.AutoSize = true;
            label39.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label39.ForeColor = Color.Yellow;
            label39.Location = new Point(324, 44);
            label39.Name = "label39";
            label39.Size = new Size(57, 21);
            label39.TabIndex = 6;
            label39.Text = "Pa：";
            // 
            // label34
            // 
            label34.AutoSize = true;
            label34.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label34.ForeColor = Color.Red;
            label34.Location = new Point(163, 140);
            label34.Name = "label34";
            label34.Size = new Size(57, 21);
            label34.TabIndex = 5;
            label34.Text = "Ic：";
            // 
            // label35
            // 
            label35.AutoSize = true;
            label35.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label35.ForeColor = Color.Green;
            label35.Location = new Point(163, 92);
            label35.Name = "label35";
            label35.Size = new Size(57, 21);
            label35.TabIndex = 4;
            label35.Text = "Ib：";
            // 
            // label36
            // 
            label36.AutoSize = true;
            label36.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label36.ForeColor = Color.Yellow;
            label36.Location = new Point(163, 44);
            label36.Name = "label36";
            label36.Size = new Size(57, 21);
            label36.TabIndex = 3;
            label36.Text = "Ia：";
            // 
            // label33
            // 
            label33.AutoSize = true;
            label33.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label33.ForeColor = Color.Red;
            label33.Location = new Point(5, 140);
            label33.Name = "label33";
            label33.Size = new Size(57, 21);
            label33.TabIndex = 2;
            label33.Text = "Uc：";
            // 
            // label32
            // 
            label32.AutoSize = true;
            label32.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label32.ForeColor = Color.Green;
            label32.Location = new Point(5, 92);
            label32.Name = "label32";
            label32.Size = new Size(57, 21);
            label32.TabIndex = 1;
            label32.Text = "Ub：";
            // 
            // label31
            // 
            label31.AutoSize = true;
            label31.Font = new Font("黑体", 10.5F, FontStyle.Bold);
            label31.ForeColor = Color.Yellow;
            label31.Location = new Point(5, 44);
            label31.Name = "label31";
            label31.Size = new Size(57, 21);
            label31.TabIndex = 0;
            label31.Text = "Ua：";
            // 
            // checkBoxISNOHEX
            // 
            checkBoxISNOHEX.AutoSize = true;
            checkBoxISNOHEX.Location = new Point(6, 6);
            checkBoxISNOHEX.Name = "checkBoxISNOHEX";
            checkBoxISNOHEX.Size = new Size(216, 28);
            checkBoxISNOHEX.TabIndex = 22;
            checkBoxISNOHEX.Text = "是否HEX发送或者显示";
            checkBoxISNOHEX.UseVisualStyleBackColor = true;
            // 
            // tabPage8
            // 
            tabPage8.Controls.Add(tabControl4);
            tabPage8.Controls.Add(panel7);
            tabPage8.Location = new Point(4, 33);
            tabPage8.Name = "tabPage8";
            tabPage8.Padding = new Padding(3);
            tabPage8.Size = new Size(1620, 545);
            tabPage8.TabIndex = 7;
            tabPage8.Text = "加密机";
            tabPage8.UseVisualStyleBackColor = true;
            // 
            // tabControl4
            // 
            tabControl4.Controls.Add(tabPage14);
            tabControl4.Controls.Add(tabPage15);
            tabControl4.Controls.Add(tabPage16);
            tabControl4.Dock = DockStyle.Fill;
            tabControl4.Location = new Point(3, 59);
            tabControl4.Margin = new Padding(5, 4, 5, 4);
            tabControl4.Name = "tabControl4";
            tabControl4.SelectedIndex = 0;
            tabControl4.Size = new Size(1614, 483);
            tabControl4.TabIndex = 10;
            // 
            // tabPage14
            // 
            tabPage14.Controls.Add(richTextBox1);
            tabPage14.Controls.Add(button6);
            tabPage14.Controls.Add(button5);
            tabPage14.Controls.Add(textBox5);
            tabPage14.Location = new Point(4, 33);
            tabPage14.Margin = new Padding(5, 4, 5, 4);
            tabPage14.Name = "tabPage14";
            tabPage14.Padding = new Padding(5, 4, 5, 4);
            tabPage14.Size = new Size(1606, 446);
            tabPage14.TabIndex = 0;
            tabPage14.Text = "加解密区";
            tabPage14.UseVisualStyleBackColor = true;
            // 
            // richTextBox1
            // 
            richTextBox1.Dock = DockStyle.Fill;
            richTextBox1.Location = new Point(5, 192);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.ReadOnly = true;
            richTextBox1.Size = new Size(1596, 250);
            richTextBox1.TabIndex = 3;
            richTextBox1.Text = "使用接口函数参数使用,隔开，例如：01,02,03        请在上边输入框输入加密机参数";
            // 
            // button6
            // 
            button6.Dock = DockStyle.Top;
            button6.Location = new Point(5, 157);
            button6.Margin = new Padding(5, 4, 5, 4);
            button6.Name = "button6";
            button6.Size = new Size(1596, 35);
            button6.TabIndex = 2;
            button6.Text = "解密数据";
            button6.UseVisualStyleBackColor = true;
            button6.Click += button6_Click;
            // 
            // button5
            // 
            button5.Dock = DockStyle.Top;
            button5.Location = new Point(5, 122);
            button5.Margin = new Padding(5, 4, 5, 4);
            button5.Name = "button5";
            button5.Size = new Size(1596, 35);
            button5.TabIndex = 1;
            button5.Text = "加密数据";
            button5.UseVisualStyleBackColor = true;
            button5.Click += button5_Click;
            // 
            // textBox5
            // 
            textBox5.Dock = DockStyle.Top;
            textBox5.Location = new Point(5, 4);
            textBox5.Margin = new Padding(5, 4, 5, 4);
            textBox5.Multiline = true;
            textBox5.Name = "textBox5";
            textBox5.Size = new Size(1596, 118);
            textBox5.TabIndex = 0;
            // 
            // tabPage15
            // 
            tabPage15.Location = new Point(4, 33);
            tabPage15.Margin = new Padding(5, 4, 5, 4);
            tabPage15.Name = "tabPage15";
            tabPage15.Padding = new Padding(5, 4, 5, 4);
            tabPage15.Size = new Size(1606, 446);
            tabPage15.TabIndex = 1;
            tabPage15.Text = "tabPage15";
            tabPage15.UseVisualStyleBackColor = true;
            // 
            // tabPage16
            // 
            tabPage16.Location = new Point(4, 33);
            tabPage16.Margin = new Padding(5, 4, 5, 4);
            tabPage16.Name = "tabPage16";
            tabPage16.Padding = new Padding(5, 4, 5, 4);
            tabPage16.Size = new Size(1606, 446);
            tabPage16.TabIndex = 2;
            tabPage16.Text = "tabPage16";
            tabPage16.UseVisualStyleBackColor = true;
            // 
            // panel7
            // 
            panel7.BorderStyle = BorderStyle.FixedSingle;
            panel7.Controls.Add(label116);
            panel7.Controls.Add(ServerImp);
            panel7.Controls.Add(label115);
            panel7.Controls.Add(textBox4);
            panel7.Controls.Add(textBox3);
            panel7.Controls.Add(LgServer);
            panel7.Controls.Add(label114);
            panel7.Controls.Add(label113);
            panel7.Dock = DockStyle.Top;
            panel7.Location = new Point(3, 3);
            panel7.Margin = new Padding(5, 4, 5, 4);
            panel7.Name = "panel7";
            panel7.Size = new Size(1614, 56);
            panel7.TabIndex = 9;
            // 
            // label116
            // 
            label116.AutoSize = true;
            label116.Dock = DockStyle.Right;
            label116.Font = new Font("Microsoft YaHei UI", 14.1428576F, FontStyle.Bold, GraphicsUnit.Point, 134);
            label116.ForeColor = Color.Red;
            label116.Location = new Point(1043, 0);
            label116.Margin = new Padding(5, 0, 5, 0);
            label116.Name = "label116";
            label116.Size = new Size(75, 39);
            label116.TabIndex = 22;
            label116.Text = "接口";
            // 
            // ServerImp
            // 
            ServerImp.Dock = DockStyle.Right;
            ServerImp.FormattingEnabled = true;
            ServerImp.Location = new Point(1118, 0);
            ServerImp.Name = "ServerImp";
            ServerImp.Size = new Size(494, 32);
            ServerImp.TabIndex = 21;
            ServerImp.SelectedIndexChanged += ServerImp_SelectedIndexChanged;
            // 
            // label115
            // 
            label115.AutoSize = true;
            label115.Location = new Point(685, 11);
            label115.Margin = new Padding(5, 0, 5, 0);
            label115.Name = "label115";
            label115.Size = new Size(154, 24);
            label115.TabIndex = 9;
            label115.Text = "服务器连接状态：";
            // 
            // textBox4
            // 
            textBox4.Location = new Point(36, 3);
            textBox4.Margin = new Padding(5, 4, 5, 4);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(205, 30);
            textBox4.TabIndex = 7;
            textBox4.Text = "22.58.244.70";
            // 
            // textBox3
            // 
            textBox3.Location = new Point(299, 3);
            textBox3.Margin = new Padding(5, 4, 5, 4);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(205, 30);
            textBox3.TabIndex = 8;
            textBox3.Text = "8001";
            textBox3.KeyPress += TextboxOnlyNumber_KeyPressed;
            // 
            // LgServer
            // 
            LgServer.Location = new Point(520, 3);
            LgServer.Margin = new Padding(5, 4, 5, 4);
            LgServer.Name = "LgServer";
            LgServer.Size = new Size(127, 45);
            LgServer.TabIndex = 0;
            LgServer.Text = "登录加密机";
            LgServer.UseVisualStyleBackColor = true;
            LgServer.Click += LgServer_Click;
            // 
            // label114
            // 
            label114.AutoSize = true;
            label114.Location = new Point(2, 9);
            label114.Margin = new Padding(5, 0, 5, 0);
            label114.Name = "label114";
            label114.Size = new Size(30, 24);
            label114.TabIndex = 5;
            label114.Text = "IP:";
            // 
            // label113
            // 
            label113.AutoSize = true;
            label113.Location = new Point(244, 9);
            label113.Margin = new Padding(5, 0, 5, 0);
            label113.Name = "label113";
            label113.Size = new Size(50, 24);
            label113.TabIndex = 6;
            label113.Text = "Port:";
            // 
            // tabPage18
            // 
            tabPage18.Controls.Add(tabControl6);
            tabPage18.Location = new Point(4, 33);
            tabPage18.Name = "tabPage18";
            tabPage18.Padding = new Padding(3);
            tabPage18.Size = new Size(1620, 545);
            tabPage18.TabIndex = 8;
            tabPage18.Text = "多功能通信";
            tabPage18.UseVisualStyleBackColor = true;
            // 
            // tabControl6
            // 
            tabControl6.Controls.Add(tabPage24);
            tabControl6.Dock = DockStyle.Fill;
            tabControl6.Location = new Point(3, 3);
            tabControl6.Name = "tabControl6";
            tabControl6.SelectedIndex = 0;
            tabControl6.Size = new Size(1614, 539);
            tabControl6.TabIndex = 0;
            // 
            // tabPage24
            // 
            tabPage24.Controls.Add(groupBox18);
            tabPage24.Controls.Add(groupBox17);
            tabPage24.Controls.Add(panel10);
            tabPage24.Location = new Point(4, 33);
            tabPage24.Name = "tabPage24";
            tabPage24.Padding = new Padding(3);
            tabPage24.Size = new Size(1606, 502);
            tabPage24.TabIndex = 2;
            tabPage24.Text = "TCP&&UDP自定义消息";
            tabPage24.UseVisualStyleBackColor = true;
            // 
            // groupBox18
            // 
            groupBox18.Controls.Add(btnSendData);
            groupBox18.Controls.Add(rtbxSendData);
            groupBox18.Controls.Add(panel11);
            groupBox18.Dock = DockStyle.Fill;
            groupBox18.Location = new Point(239, 300);
            groupBox18.Name = "groupBox18";
            groupBox18.Size = new Size(1364, 199);
            groupBox18.TabIndex = 2;
            groupBox18.TabStop = false;
            groupBox18.Text = "发送数据";
            // 
            // btnSendData
            // 
            btnSendData.Dock = DockStyle.Left;
            btnSendData.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 134);
            btnSendData.ForeColor = Color.Green;
            btnSendData.Location = new Point(3, 158);
            btnSendData.Margin = new Padding(5, 4, 5, 4);
            btnSendData.Name = "btnSendData";
            btnSendData.Size = new Size(234, 38);
            btnSendData.TabIndex = 2;
            btnSendData.Text = "发送";
            btnSendData.UseVisualStyleBackColor = true;
            btnSendData.Click += btnSendData_Click;
            // 
            // rtbxSendData
            // 
            rtbxSendData.Dock = DockStyle.Top;
            rtbxSendData.Location = new Point(3, 79);
            rtbxSendData.Name = "rtbxSendData";
            rtbxSendData.Size = new Size(1358, 79);
            rtbxSendData.TabIndex = 1;
            rtbxSendData.Text = "xichengkeji";
            // 
            // panel11
            // 
            panel11.BorderStyle = BorderStyle.FixedSingle;
            panel11.Controls.Add(cbxIsBroadcastMessage);
            panel11.Controls.Add(button1);
            panel11.Controls.Add(cbxClientConnc);
            panel11.Controls.Add(label122);
            panel11.Controls.Add(button8);
            panel11.Controls.Add(button2);
            panel11.Dock = DockStyle.Top;
            panel11.Location = new Point(3, 26);
            panel11.Name = "panel11";
            panel11.Size = new Size(1358, 53);
            panel11.TabIndex = 0;
            // 
            // cbxIsBroadcastMessage
            // 
            cbxIsBroadcastMessage.AutoSize = true;
            cbxIsBroadcastMessage.Location = new Point(372, 3);
            cbxIsBroadcastMessage.Name = "cbxIsBroadcastMessage";
            cbxIsBroadcastMessage.Size = new Size(144, 28);
            cbxIsBroadcastMessage.TabIndex = 12;
            cbxIsBroadcastMessage.Text = "是否广播消息";
            cbxIsBroadcastMessage.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            button1.Location = new Point(255, 0);
            button1.Margin = new Padding(5, 4, 5, 4);
            button1.Name = "button1";
            button1.Size = new Size(110, 31);
            button1.TabIndex = 11;
            button1.Text = "断开";
            button1.UseVisualStyleBackColor = true;
            // 
            // cbxClientConnc
            // 
            cbxClientConnc.Dock = DockStyle.Left;
            cbxClientConnc.FormattingEnabled = true;
            cbxClientConnc.Location = new Point(68, 0);
            cbxClientConnc.Name = "cbxClientConnc";
            cbxClientConnc.Size = new Size(180, 32);
            cbxClientConnc.TabIndex = 10;
            // 
            // label122
            // 
            label122.AutoSize = true;
            label122.Dock = DockStyle.Left;
            label122.Location = new Point(0, 0);
            label122.Margin = new Padding(5, 0, 5, 0);
            label122.Name = "label122";
            label122.Size = new Size(68, 24);
            label122.TabIndex = 9;
            label122.Text = "客户端:";
            // 
            // button8
            // 
            button8.Dock = DockStyle.Right;
            button8.Font = new Font("Microsoft YaHei UI", 10.7142859F, FontStyle.Bold);
            button8.ForeColor = SystemColors.ControlDarkDark;
            button8.Location = new Point(1074, 0);
            button8.Margin = new Padding(5, 4, 5, 4);
            button8.Name = "button8";
            button8.Size = new Size(141, 51);
            button8.TabIndex = 2;
            button8.Text = "↓清空";
            button8.UseVisualStyleBackColor = true;
            button8.Click += button8_Click;
            // 
            // button2
            // 
            button2.Dock = DockStyle.Right;
            button2.Font = new Font("Microsoft YaHei UI", 10.7142859F, FontStyle.Bold);
            button2.ForeColor = SystemColors.ControlDarkDark;
            button2.Location = new Point(1215, 0);
            button2.Margin = new Padding(5, 4, 5, 4);
            button2.Name = "button2";
            button2.Size = new Size(141, 51);
            button2.TabIndex = 1;
            button2.Text = "清空↑";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // groupBox17
            // 
            groupBox17.Controls.Add(rtbxRevcData);
            groupBox17.Dock = DockStyle.Top;
            groupBox17.Location = new Point(239, 3);
            groupBox17.Name = "groupBox17";
            groupBox17.Size = new Size(1364, 297);
            groupBox17.TabIndex = 1;
            groupBox17.TabStop = false;
            groupBox17.Text = "接受数据";
            // 
            // rtbxRevcData
            // 
            rtbxRevcData.Dock = DockStyle.Fill;
            rtbxRevcData.Location = new Point(3, 26);
            rtbxRevcData.Name = "rtbxRevcData";
            rtbxRevcData.Size = new Size(1358, 268);
            rtbxRevcData.TabIndex = 2;
            rtbxRevcData.Text = "";
            // 
            // panel10
            // 
            panel10.Controls.Add(groupBox19);
            panel10.Controls.Add(groupBox16);
            panel10.Controls.Add(groupBox15);
            panel10.Dock = DockStyle.Left;
            panel10.Location = new Point(3, 3);
            panel10.Name = "panel10";
            panel10.Size = new Size(236, 496);
            panel10.TabIndex = 0;
            // 
            // groupBox19
            // 
            groupBox19.Controls.Add(cbxSendHEX);
            groupBox19.Controls.Add(cbxSendASCII);
            groupBox19.Dock = DockStyle.Top;
            groupBox19.Location = new Point(0, 398);
            groupBox19.Name = "groupBox19";
            groupBox19.Size = new Size(236, 101);
            groupBox19.TabIndex = 2;
            groupBox19.TabStop = false;
            groupBox19.Text = "发送设置";
            // 
            // cbxSendHEX
            // 
            cbxSendHEX.AutoSize = true;
            cbxSendHEX.Checked = true;
            cbxSendHEX.CheckState = CheckState.Checked;
            cbxSendHEX.Location = new Point(93, 28);
            cbxSendHEX.Name = "cbxSendHEX";
            cbxSendHEX.Size = new Size(72, 28);
            cbxSendHEX.TabIndex = 2;
            cbxSendHEX.Text = "HEX";
            cbxSendHEX.UseVisualStyleBackColor = true;
            cbxSendHEX.CheckedChanged += cbxSendHEX_CheckedChanged;
            // 
            // cbxSendASCII
            // 
            cbxSendASCII.AutoSize = true;
            cbxSendASCII.Location = new Point(11, 28);
            cbxSendASCII.Name = "cbxSendASCII";
            cbxSendASCII.Size = new Size(81, 28);
            cbxSendASCII.TabIndex = 1;
            cbxSendASCII.Text = "ASCII";
            cbxSendASCII.UseVisualStyleBackColor = true;
            cbxSendASCII.CheckedChanged += cbxSendHEX_CheckedChanged;
            // 
            // groupBox16
            // 
            groupBox16.Controls.Add(cbxRevcHEX);
            groupBox16.Controls.Add(cbxRevcASCII);
            groupBox16.Dock = DockStyle.Top;
            groupBox16.Location = new Point(0, 297);
            groupBox16.Name = "groupBox16";
            groupBox16.Size = new Size(236, 101);
            groupBox16.TabIndex = 1;
            groupBox16.TabStop = false;
            groupBox16.Text = "接受设置";
            // 
            // cbxRevcHEX
            // 
            cbxRevcHEX.AutoSize = true;
            cbxRevcHEX.Checked = true;
            cbxRevcHEX.CheckState = CheckState.Checked;
            cbxRevcHEX.Location = new Point(93, 21);
            cbxRevcHEX.Name = "cbxRevcHEX";
            cbxRevcHEX.Size = new Size(72, 28);
            cbxRevcHEX.TabIndex = 1;
            cbxRevcHEX.Text = "HEX";
            cbxRevcHEX.UseVisualStyleBackColor = true;
            cbxRevcHEX.CheckedChanged += cbxRevcHEX_CheckedChanged;
            // 
            // cbxRevcASCII
            // 
            cbxRevcASCII.AutoSize = true;
            cbxRevcASCII.Location = new Point(11, 21);
            cbxRevcASCII.Name = "cbxRevcASCII";
            cbxRevcASCII.Size = new Size(81, 28);
            cbxRevcASCII.TabIndex = 0;
            cbxRevcASCII.Text = "ASCII";
            cbxRevcASCII.UseVisualStyleBackColor = true;
            cbxRevcASCII.CheckedChanged += cbxRevcHEX_CheckedChanged;
            // 
            // groupBox15
            // 
            groupBox15.Controls.Add(TCPServerConnent);
            groupBox15.Controls.Add(cbxPort);
            groupBox15.Controls.Add(cbxIp);
            groupBox15.Controls.Add(label121);
            groupBox15.Controls.Add(cbxSocketClass);
            groupBox15.Controls.Add(label120);
            groupBox15.Controls.Add(label23);
            groupBox15.Dock = DockStyle.Top;
            groupBox15.Location = new Point(0, 0);
            groupBox15.Name = "groupBox15";
            groupBox15.Size = new Size(236, 297);
            groupBox15.TabIndex = 0;
            groupBox15.TabStop = false;
            groupBox15.Text = "网络设置";
            // 
            // TCPServerConnent
            // 
            TCPServerConnent.Location = new Point(21, 249);
            TCPServerConnent.Margin = new Padding(5, 4, 5, 4);
            TCPServerConnent.Name = "TCPServerConnent";
            TCPServerConnent.Size = new Size(179, 35);
            TCPServerConnent.TabIndex = 30;
            TCPServerConnent.Text = "TCP客户端连接";
            TCPServerConnent.UseVisualStyleBackColor = true;
            TCPServerConnent.Click += TCPServerConnent_Click;
            // 
            // cbxPort
            // 
            cbxPort.FormattingEnabled = true;
            cbxPort.Location = new Point(21, 211);
            cbxPort.Name = "cbxPort";
            cbxPort.Size = new Size(180, 32);
            cbxPort.TabIndex = 12;
            // 
            // cbxIp
            // 
            cbxIp.FormattingEnabled = true;
            cbxIp.Location = new Point(21, 133);
            cbxIp.Name = "cbxIp";
            cbxIp.Size = new Size(180, 32);
            cbxIp.TabIndex = 11;
            // 
            // label121
            // 
            label121.AutoSize = true;
            label121.Location = new Point(8, 34);
            label121.Margin = new Padding(5, 0, 5, 0);
            label121.Name = "label121";
            label121.Size = new Size(129, 24);
            label121.TabIndex = 10;
            label121.Text = "（1）协议类型";
            // 
            // cbxSocketClass
            // 
            cbxSocketClass.FormattingEnabled = true;
            cbxSocketClass.Items.AddRange(new object[] { "UDP", "TCPClient", "TCPServer" });
            cbxSocketClass.Location = new Point(21, 61);
            cbxSocketClass.Name = "cbxSocketClass";
            cbxSocketClass.Size = new Size(180, 32);
            cbxSocketClass.TabIndex = 9;
            cbxSocketClass.SelectedIndexChanged += cbxSocketClass_SelectedIndexChanged;
            // 
            // label120
            // 
            label120.AutoSize = true;
            label120.Location = new Point(8, 176);
            label120.Margin = new Padding(5, 0, 5, 0);
            label120.Name = "label120";
            label120.Size = new Size(165, 24);
            label120.TabIndex = 6;
            label120.Text = "（3）远程主机端口";
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.Location = new Point(8, 100);
            label23.Margin = new Padding(5, 0, 5, 0);
            label23.Name = "label23";
            label23.Size = new Size(165, 24);
            label23.TabIndex = 5;
            label23.Text = "（2）远程主机地址";
            // 
            // LogUnit
            // 
            LogUnit.Controls.Add(panellog);
            LogUnit.Dock = DockStyle.Fill;
            LogUnit.Font = new Font("微软雅黑", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            LogUnit.ForeColor = Color.Green;
            LogUnit.Location = new Point(0, 0);
            LogUnit.Margin = new Padding(5, 4, 5, 4);
            LogUnit.Name = "LogUnit";
            LogUnit.Padding = new Padding(5, 4, 5, 4);
            LogUnit.Size = new Size(1628, 107);
            LogUnit.TabIndex = 2;
            LogUnit.TabStop = false;
            LogUnit.Text = "日志单元-右击清空日志";
            // 
            // panellog
            // 
            panellog.Controls.Add(textBoxlog);
            panellog.Dock = DockStyle.Fill;
            panellog.Location = new Point(5, 28);
            panellog.Margin = new Padding(5, 4, 5, 4);
            panellog.Name = "panellog";
            panellog.Size = new Size(1618, 75);
            panellog.TabIndex = 0;
            // 
            // textBoxlog
            // 
            textBoxlog.BackColor = SystemColors.MenuText;
            textBoxlog.Dock = DockStyle.Fill;
            textBoxlog.ForeColor = Color.Lime;
            textBoxlog.Location = new Point(0, 0);
            textBoxlog.Name = "textBoxlog";
            textBoxlog.Size = new Size(1618, 75);
            textBoxlog.TabIndex = 0;
            textBoxlog.Text = "";
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(24, 24);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { 清空ToolStripMenuItem, 复制ToolStripMenuItem, 切换背景色ToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(171, 94);
            // 
            // 清空ToolStripMenuItem
            // 
            清空ToolStripMenuItem.Name = "清空ToolStripMenuItem";
            清空ToolStripMenuItem.Size = new Size(170, 30);
            清空ToolStripMenuItem.Text = "清空";
            清空ToolStripMenuItem.Click += 清空ToolStripMenuItem_Click;
            // 
            // 复制ToolStripMenuItem
            // 
            复制ToolStripMenuItem.Name = "复制ToolStripMenuItem";
            复制ToolStripMenuItem.Size = new Size(170, 30);
            复制ToolStripMenuItem.Text = "复制";
            复制ToolStripMenuItem.Click += 复制ToolStripMenuItem_Click;
            // 
            // 切换背景色ToolStripMenuItem
            // 
            切换背景色ToolStripMenuItem.Name = "切换背景色ToolStripMenuItem";
            切换背景色ToolStripMenuItem.Size = new Size(170, 30);
            切换背景色ToolStripMenuItem.Text = "切换背景色";
            切换背景色ToolStripMenuItem.Click += 切换背景色ToolStripMenuItem_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Items.AddRange(new ToolStripItem[] { lblconnectStatus });
            statusStrip1.Location = new Point(0, 878);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(2, 0, 22, 0);
            statusStrip1.Size = new Size(1628, 31);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // lblconnectStatus
            // 
            lblconnectStatus.ForeColor = Color.Red;
            lblconnectStatus.Name = "lblconnectStatus";
            lblconnectStatus.Size = new Size(64, 24);
            lblconnectStatus.Text = "未连接";
            // 
            // socketUnit
            // 
            socketUnit.Controls.Add(groupBox14);
            socketUnit.Controls.Add(groupBox13);
            socketUnit.Controls.Add(groupBox12);
            socketUnit.Dock = DockStyle.Fill;
            socketUnit.Location = new Point(0, 37);
            socketUnit.Margin = new Padding(5, 4, 5, 4);
            socketUnit.Name = "socketUnit";
            socketUnit.Padding = new Padding(5, 4, 5, 4);
            socketUnit.Size = new Size(1626, 150);
            socketUnit.TabIndex = 0;
            socketUnit.TabStop = false;
            socketUnit.Text = "通信单元";
            // 
            // groupBox14
            // 
            groupBox14.Controls.Add(label119);
            groupBox14.Dock = DockStyle.Fill;
            groupBox14.Location = new Point(1171, 27);
            groupBox14.Name = "groupBox14";
            groupBox14.Size = new Size(450, 119);
            groupBox14.TabIndex = 9;
            groupBox14.TabStop = false;
            groupBox14.Text = "TCP或UDP服务器";
            // 
            // label119
            // 
            label119.AutoSize = true;
            label119.ForeColor = Color.Red;
            label119.Location = new Point(8, 28);
            label119.Margin = new Padding(5, 0, 5, 0);
            label119.Name = "label119";
            label119.Size = new Size(208, 24);
            label119.TabIndex = 29;
            label119.Text = "多功能串口通信配置参数";
            // 
            // groupBox13
            // 
            groupBox13.Controls.Add(cbxIsNoPortSeed);
            groupBox13.Controls.Add(comboBoxCOM);
            groupBox13.Controls.Add(btnflushPort);
            groupBox13.Controls.Add(label12);
            groupBox13.Controls.Add(comboBoxBaute);
            groupBox13.Controls.Add(textBoxdatabit);
            groupBox13.Controls.Add(textBoxstopbit);
            groupBox13.Controls.Add(comboBoxparity);
            groupBox13.Controls.Add(label14);
            groupBox13.Controls.Add(label17);
            groupBox13.Controls.Add(label15);
            groupBox13.Controls.Add(label16);
            groupBox13.Controls.Add(buttonOpen);
            groupBox13.Dock = DockStyle.Left;
            groupBox13.Location = new Point(450, 27);
            groupBox13.Name = "groupBox13";
            groupBox13.Size = new Size(721, 119);
            groupBox13.TabIndex = 8;
            groupBox13.TabStop = false;
            groupBox13.Text = "串口通信";
            // 
            // cbxIsNoPortSeed
            // 
            cbxIsNoPortSeed.AutoSize = true;
            cbxIsNoPortSeed.Location = new Point(572, 26);
            cbxIsNoPortSeed.Name = "cbxIsNoPortSeed";
            cbxIsNoPortSeed.Size = new Size(144, 28);
            cbxIsNoPortSeed.TabIndex = 20;
            cbxIsNoPortSeed.Text = "是否串口发送";
            cbxIsNoPortSeed.UseVisualStyleBackColor = true;
            // 
            // comboBoxCOM
            // 
            comboBoxCOM.FormattingEnabled = true;
            comboBoxCOM.Location = new Point(90, 26);
            comboBoxCOM.Name = "comboBoxCOM";
            comboBoxCOM.Size = new Size(97, 32);
            comboBoxCOM.TabIndex = 7;
            // 
            // btnflushPort
            // 
            btnflushPort.Location = new Point(569, 62);
            btnflushPort.Margin = new Padding(5, 4, 5, 4);
            btnflushPort.Name = "btnflushPort";
            btnflushPort.Size = new Size(141, 35);
            btnflushPort.TabIndex = 19;
            btnflushPort.Text = "刷新串口";
            btnflushPort.UseVisualStyleBackColor = true;
            btnflushPort.Click += btnflushPort_Click;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(11, 31);
            label12.Margin = new Padding(5, 0, 5, 0);
            label12.Name = "label12";
            label12.Size = new Size(68, 24);
            label12.TabIndex = 8;
            label12.Text = "串口号:";
            // 
            // comboBoxBaute
            // 
            comboBoxBaute.FormattingEnabled = true;
            comboBoxBaute.Items.AddRange(new object[] { "300", "600", "900", "1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200" });
            comboBoxBaute.Location = new Point(90, 62);
            comboBoxBaute.Name = "comboBoxBaute";
            comboBoxBaute.Size = new Size(97, 32);
            comboBoxBaute.TabIndex = 10;
            // 
            // textBoxdatabit
            // 
            textBoxdatabit.FormattingEnabled = true;
            textBoxdatabit.Items.AddRange(new object[] { "8", "7" });
            textBoxdatabit.Location = new Point(272, 26);
            textBoxdatabit.Name = "textBoxdatabit";
            textBoxdatabit.Size = new Size(97, 32);
            textBoxdatabit.TabIndex = 18;
            // 
            // textBoxstopbit
            // 
            textBoxstopbit.FormattingEnabled = true;
            textBoxstopbit.Items.AddRange(new object[] { "1", "1.5", "2" });
            textBoxstopbit.Location = new Point(273, 61);
            textBoxstopbit.Name = "textBoxstopbit";
            textBoxstopbit.Size = new Size(97, 32);
            textBoxstopbit.TabIndex = 17;
            // 
            // comboBoxparity
            // 
            comboBoxparity.FormattingEnabled = true;
            comboBoxparity.Items.AddRange(new object[] { "NONE", "EVEN", "ODD", "MARK", "SPACE" });
            comboBoxparity.Location = new Point(468, 24);
            comboBoxparity.Name = "comboBoxparity";
            comboBoxparity.Size = new Size(97, 32);
            comboBoxparity.TabIndex = 16;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(9, 68);
            label14.Margin = new Padding(5, 0, 5, 0);
            label14.Name = "label14";
            label14.Size = new Size(68, 24);
            label14.TabIndex = 9;
            label14.Text = "波特率:";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(379, 30);
            label17.Margin = new Padding(5, 0, 5, 0);
            label17.Name = "label17";
            label17.Size = new Size(86, 24);
            label17.TabIndex = 13;
            label17.Text = "奇偶校验:";
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(196, 31);
            label15.Margin = new Padding(5, 0, 5, 0);
            label15.Name = "label15";
            label15.Size = new Size(68, 24);
            label15.TabIndex = 11;
            label15.Text = "数据位:";
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(196, 66);
            label16.Margin = new Padding(5, 0, 5, 0);
            label16.Name = "label16";
            label16.Size = new Size(68, 24);
            label16.TabIndex = 12;
            label16.Text = "停止位:";
            // 
            // buttonOpen
            // 
            buttonOpen.Location = new Point(418, 62);
            buttonOpen.Margin = new Padding(5, 4, 5, 4);
            buttonOpen.Name = "buttonOpen";
            buttonOpen.Size = new Size(141, 35);
            buttonOpen.TabIndex = 6;
            buttonOpen.Text = "OPEN";
            buttonOpen.UseVisualStyleBackColor = true;
            buttonOpen.Click += buttonOpen_Click;
            // 
            // groupBox12
            // 
            groupBox12.Controls.Add(panel8);
            groupBox12.Dock = DockStyle.Left;
            groupBox12.Location = new Point(5, 27);
            groupBox12.Name = "groupBox12";
            groupBox12.Size = new Size(445, 119);
            groupBox12.TabIndex = 7;
            groupBox12.TabStop = false;
            groupBox12.Text = "TCPClient";
            // 
            // panel8
            // 
            panel8.Controls.Add(textBoxIP);
            panel8.Controls.Add(btn_cilentSocket_Close);
            panel8.Controls.Add(label1);
            panel8.Controls.Add(btn_cilentSocket);
            panel8.Controls.Add(textBoxPort);
            panel8.Controls.Add(label2);
            panel8.Dock = DockStyle.Fill;
            panel8.Location = new Point(3, 26);
            panel8.Name = "panel8";
            panel8.Size = new Size(439, 90);
            panel8.TabIndex = 6;
            // 
            // textBoxIP
            // 
            textBoxIP.Location = new Point(64, 10);
            textBoxIP.Margin = new Padding(5, 4, 5, 4);
            textBoxIP.Name = "textBoxIP";
            textBoxIP.Size = new Size(205, 30);
            textBoxIP.TabIndex = 3;
            textBoxIP.Text = "192.168.127.201";
            // 
            // btn_cilentSocket_Close
            // 
            btn_cilentSocket_Close.Location = new Point(280, 48);
            btn_cilentSocket_Close.Margin = new Padding(5, 4, 5, 4);
            btn_cilentSocket_Close.Name = "btn_cilentSocket_Close";
            btn_cilentSocket_Close.Size = new Size(141, 35);
            btn_cilentSocket_Close.TabIndex = 5;
            btn_cilentSocket_Close.Text = "断开";
            btn_cilentSocket_Close.UseVisualStyleBackColor = true;
            btn_cilentSocket_Close.Click += btn_cilentSocket_Close_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(24, 10);
            label1.Margin = new Padding(5, 0, 5, 0);
            label1.Name = "label1";
            label1.Size = new Size(30, 24);
            label1.TabIndex = 1;
            label1.Text = "IP:";
            // 
            // btn_cilentSocket
            // 
            btn_cilentSocket.Location = new Point(280, 10);
            btn_cilentSocket.Margin = new Padding(5, 4, 5, 4);
            btn_cilentSocket.Name = "btn_cilentSocket";
            btn_cilentSocket.Size = new Size(141, 35);
            btn_cilentSocket.TabIndex = 0;
            btn_cilentSocket.Text = "连接";
            btn_cilentSocket.UseVisualStyleBackColor = true;
            btn_cilentSocket.Click += btn_clientSocket_Click;
            // 
            // textBoxPort
            // 
            textBoxPort.Location = new Point(64, 51);
            textBoxPort.Margin = new Padding(5, 4, 5, 4);
            textBoxPort.Name = "textBoxPort";
            textBoxPort.Size = new Size(205, 30);
            textBoxPort.TabIndex = 4;
            textBoxPort.Text = "4000";
            textBoxPort.KeyPress += TextboxOnlyNumber_KeyPressed;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(5, 52);
            label2.Margin = new Padding(5, 0, 5, 0);
            label2.Name = "label2";
            label2.Size = new Size(50, 24);
            label2.TabIndex = 2;
            label2.Text = "Port:";
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(socketUnit);
            panel1.Controls.Add(toolStrip1);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1628, 189);
            panel1.TabIndex = 3;
            // 
            // toolStrip1
            // 
            toolStrip1.ImageScalingSize = new Size(28, 28);
            toolStrip1.Items.AddRange(new ToolStripItem[] { tsbtnTerminalTest, tsbtnMeterTest });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(1626, 37);
            toolStrip1.TabIndex = 10;
            toolStrip1.Text = "toolStrip1";
            // 
            // tsbtnTerminalTest
            // 
            tsbtnTerminalTest.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbtnTerminalTest.Image = (Image)resources.GetObject("tsbtnTerminalTest.Image");
            tsbtnTerminalTest.ImageTransparentColor = Color.Magenta;
            tsbtnTerminalTest.Name = "tsbtnTerminalTest";
            tsbtnTerminalTest.Size = new Size(34, 32);
            tsbtnTerminalTest.Text = "终端检测";
            tsbtnTerminalTest.Click += tsbtnTerminalTest_Click;
            // 
            // tsbtnMeterTest
            // 
            tsbtnMeterTest.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbtnMeterTest.Image = (Image)resources.GetObject("tsbtnMeterTest.Image");
            tsbtnMeterTest.ImageTransparentColor = Color.Magenta;
            tsbtnMeterTest.Name = "tsbtnMeterTest";
            tsbtnMeterTest.Size = new Size(34, 32);
            tsbtnMeterTest.Text = "电表检测";
            tsbtnMeterTest.Click += tsbtnMeterTest_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(tabControl1);
            panel2.Dock = DockStyle.Top;
            panel2.Location = new Point(0, 189);
            panel2.Name = "panel2";
            panel2.Size = new Size(1628, 582);
            panel2.TabIndex = 4;
            // 
            // panel3
            // 
            panel3.Controls.Add(LogUnit);
            panel3.Dock = DockStyle.Fill;
            panel3.Location = new Point(0, 771);
            panel3.Name = "panel3";
            panel3.Size = new Size(1628, 107);
            panel3.TabIndex = 5;
            // 
            // ModelMain
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1628, 909);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            Controls.Add(statusStrip1);
            Margin = new Padding(5, 4, 5, 4);
            MaximizeBox = false;
            Name = "ModelMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "习承科技测试";
            WindowState = FormWindowState.Maximized;
            Load += ModelMain_Load;
            SizeChanged += ModelMain_SizeChanged;
            Resize += ModelMain_Resize;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            TestUnit.ResumeLayout(false);
            TestUnit.PerformLayout();
            tabPage2.ResumeLayout(false);
            tabPage2.PerformLayout();
            panel16.ResumeLayout(false);
            panel16.PerformLayout();
            tabPage3.ResumeLayout(false);
            tabControl2.ResumeLayout(false);
            tabPage9.ResumeLayout(false);
            panel4.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            groupBox10.ResumeLayout(false);
            groupBox10.PerformLayout();
            groupBox11.ResumeLayout(false);
            panel15.ResumeLayout(false);
            panel15.PerformLayout();
            panel9.ResumeLayout(false);
            panel9.PerformLayout();
            panel6.ResumeLayout(false);
            panel6.PerformLayout();
            panel5.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pBTaiti_yellow).EndInit();
            ((System.ComponentModel.ISupportInitialize)pBTaiti_Green).EndInit();
            ((System.ComponentModel.ISupportInitialize)pBTaiti_Red).EndInit();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxRed).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxGreen).EndInit();
            groupBox1.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            tabPage7.ResumeLayout(false);
            tabPage7.PerformLayout();
            groupBox9.ResumeLayout(false);
            groupBox9.PerformLayout();
            groupBox8.ResumeLayout(false);
            groupBox8.PerformLayout();
            groupBox7.ResumeLayout(false);
            groupBox7.PerformLayout();
            tabControl3.ResumeLayout(false);
            tabPage11.ResumeLayout(false);
            tabPage11.PerformLayout();
            tabPage12.ResumeLayout(false);
            tabPage12.PerformLayout();
            tabPage13.ResumeLayout(false);
            tabPage13.PerformLayout();
            tabPage17.ResumeLayout(false);
            tabPage17.PerformLayout();
            groupBox6.ResumeLayout(false);
            groupBox6.PerformLayout();
            tabPage8.ResumeLayout(false);
            tabControl4.ResumeLayout(false);
            tabPage14.ResumeLayout(false);
            tabPage14.PerformLayout();
            panel7.ResumeLayout(false);
            panel7.PerformLayout();
            tabPage18.ResumeLayout(false);
            tabControl6.ResumeLayout(false);
            tabPage24.ResumeLayout(false);
            groupBox18.ResumeLayout(false);
            panel11.ResumeLayout(false);
            panel11.PerformLayout();
            groupBox17.ResumeLayout(false);
            panel10.ResumeLayout(false);
            groupBox19.ResumeLayout(false);
            groupBox19.PerformLayout();
            groupBox16.ResumeLayout(false);
            groupBox16.PerformLayout();
            groupBox15.ResumeLayout(false);
            groupBox15.PerformLayout();
            LogUnit.ResumeLayout(false);
            panellog.ResumeLayout(false);
            contextMenuStrip1.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            socketUnit.ResumeLayout(false);
            groupBox14.ResumeLayout(false);
            groupBox14.PerformLayout();
            groupBox13.ResumeLayout(false);
            groupBox13.PerformLayout();
            groupBox12.ResumeLayout(false);
            panel8.ResumeLayout(false);
            panel8.PerformLayout();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            panel2.ResumeLayout(false);
            panel3.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private GroupBox socketUnit;
        private GroupBox TestUnit;
        private Button btn_cilentSocket;
        private Label label1;
        private Label label2;
        private TextBox textBoxPort;
        private TextBox textBoxIP;
        private Button btn_cilentSocket_Close;
        private GroupBox LogUnit;
        private Panel panellog;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel lblconnectStatus;
        private Label label4;
        private Button btnPowerDown_DC;
        private Button btnPowerOn_DC;
        private Label label5;
        private Label label6;
        private Label label7;
        private TextBox tbxModelNumber;
        private ComboBox cbxTerminalCLASS;
        private TextBox tbx_addr;
        private Label label8;
        private Button btnPowerDown_AC;
        private Button btnPowerOn_AC;
        private CheckBox checkBox2;
        private CheckBox checkBox1;
        private CheckBox checkBoxN;
        private CheckBox checkBoxC;
        private CheckBox checkBoxB;
        private CheckBox checkBoxA;
        private Panel panel1;
        private Panel panel2;
        private Panel panel3;
        private TabPage tabPage3;
        private Label label10;
        private Label label11;
        private Label label13;
        private Button SGCC698FF;
        private Button CSG698FF;
        private Button SGCC645FF;
        private Label label3;
        private Label label9;
        private Label label12;
        private ComboBox comboBoxCOM;
        private Button buttonOpen;
        private Label label14;
        private Label label15;
        private ComboBox comboBoxBaute;
        private Label label16;
        private Label label17;
        private ComboBox comboBoxparity;
        private Label label18;
        private Button buttonKZHLStatus;
        private Label label19;
        private Button buttonKZHLID;
        private Label label20;
        private Label label21;
        private Button CCODCOn;
        private Button CCODCDown;
        private Button CCOACDown;
        private Button CCOACOn;
        private TabPage tabPage4;
        private TabPage tabPage5;
        private TabPage tabPage6;
        private TabPage tabPage7;
        private TabPage tabPage8;
        private TabControl tabControl2;
        private TabPage tabPage9;
        private TabPage tabPage10;
        private ComboBox cbxTerminalV1;
        private Panel panel4;
        private Button btnChangeTerminalClass;
        private TextBox tbxTerminalAdds;
        private Label label25;
        private CheckBox cbx_TerminalV1_IA;
        private CheckBox cbx_TerminalV1_UC;
        private CheckBox cbx_TerminalV1_UB;
        private CheckBox cbx_TerminalV1_UA;
        private CheckBox cbx_TerminalV1_IN;
        private CheckBox cbx_TerminalV1_IC;
        private CheckBox cbx_TerminalV1_IB;
        private Panel panel5;
        private Button btnTerminalBW_VOn;
        private Button btnTerminalBW_VDown;
        private Button btnTerminalBW_AOn;
        private Button btnTerminalBW_ADown;
        private Button btnTerminalV1MotorCrimpingreturn;
        private Button btnTerminalV1MotorCrimping;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private GroupBox groupBox4;
        private Label label24;
        private PictureBox pictureBoxGreen;
        private Label label26;
        private PictureBox pictureBoxRed;
        private ContextMenuStrip contextMenuStrip1;
        private ToolStripMenuItem 清空ToolStripMenuItem;
        private ToolStripMenuItem 复制ToolStripMenuItem;
        private ToolStripMenuItem 切换背景色ToolStripMenuItem;
        private GroupBox groupBox5;
        private PictureBox pBTaiti_yellow;
        private PictureBox pBTaiti_Green;
        private PictureBox pBTaiti_Red;
        private Label label27;
        private Label label29;
        private Label label28;
        private Label label30;
        private ComboBox textBoxdatabit;
        private ComboBox textBoxstopbit;
        private Button btnflushPort;
        private Button buttonXY_x0E;
        private CheckBox checkBoxISNOHEX;
        private GroupBox groupBox6;
        private Label label31;
        private Label label46;
        private Label label47;
        private Label label48;
        private Label label43;
        private Label label44;
        private Label label45;
        private Label label40;
        private Label label41;
        private Label label42;
        private Label label37;
        private Label label38;
        private Label label39;
        private Label label34;
        private Label label35;
        private Label label36;
        private Label label33;
        private Label label32;
        private Label label49;
        private Label label50;
        private Label label51;
        private Label label52;
        private Label label53;
        private Label label54;
        private TextBox tb_UA;
        private TextBox tb_EQ;
        private TextBox tb_EP;
        private TextBox tb_ES;
        private TextBox tb_XC;
        private TextBox tb_XB;
        private TextBox tb_XA;
        private TextBox tb_PFC;
        private TextBox tb_PFB;
        private TextBox tb_PFA;
        private TextBox tb_SC;
        private TextBox tb_SB;
        private TextBox tb_SA;
        private TextBox tb_QC;
        private TextBox tb_QB;
        private TextBox tb_QA;
        private TextBox tb_PC;
        private TextBox tb_PB;
        private TextBox tb_PA;
        private TextBox tb_IC;
        private TextBox tb_IB;
        private TextBox tb_IA;
        private TextBox tb_UC;
        private TextBox tb_UB;
        private Button btn_ReadStandMeter;
        private Button buttonCmdReadMeterData;
        private Label label55;
        private TextBox tb_HZ;
        private CheckBox cbxIsNoPortSeed;
        private Button buttonCtrlUI;
        private GroupBox groupBox7;
        private Label label59;
        private Label label56;
        private Label label60;
        private Label label61;
        private Label label57;
        private Label label58;
        private ComboBox comboBoxIC;
        private ComboBox comboBoxIB;
        private ComboBox comboBoxIA;
        private ComboBox comboBoxVC;
        private ComboBox comboBoxVB;
        private ComboBox comboBoxVA;
        private Label label62;
        private Label label63;
        private Label label64;
        private Label label65;
        private Label label66;
        private Label label67;
        private Button btn_ReadContans;
        private TextBox tb_contans;
        private Label label68;
        private TextBox tbx_sourcePort;
        private Label label69;
        private Label label70;
        private Button btn_SourcePort;
        private TextBox tb_Alarm;
        private Label label71;
        private TextBox tb_Uca;
        private Label label72;
        private TextBox tb_Uba;
        private Label label73;
        private Label label74;
        private Label label75;
        private Label label76;
        private TextBox tb_A_LC;
        private TextBox tb_V_LC;
        private TextBox tb_xx;
        private Label label77;
        private Label label78;
        private Label label79;
        private ComboBox cbxICJ;
        private ComboBox cbxIBJ;
        private ComboBox cbxIAJ;
        private Label label80;
        private Label label81;
        private ComboBox cbxUac;
        private ComboBox cbxUab;
        private Label label82;
        private TextBox tbxiPulse;
        private CheckBox cbxShutdownUI0;
        private Button btn_Initmeter;
        private GroupBox groupBox8;
        private Label label83;
        private Label label84;
        private Label label85;
        private Label label86;
        private ComboBox cbx_Connection;
        private ComboBox cbx_meterconstant;
        private ComboBox cbx_ratedcurrent;
        private ComboBox cbx_ratedvoltage;
        private Label label89;
        private Label label88;
        private Label label87;
        private GroupBox groupBox9;
        private Button btn_XY_ADJ;
        private Label label90;
        private TextBox tbx_A_5;
        private TextBox tbx_V_5;
        private Label label91;
        private Label label93;
        private Label label92;
        private Label label94;
        private ComboBox cbx_HABC;
        private ComboBox cbx_LC;
        private TabControl tabControl3;
        private TabPage tabPage11;
        private TabPage tabPage12;
        private Label label95;
        private ComboBox cbbx_BlueTooth;
        private Label label96;
        private ComboBox cbbx_ToosNum;
        private Button bttn_settooth;
        private Label label97;
        private Label label98;
        private Label label99;
        private TextBox tbxclockpulse;
        private Button bttn_ClockStart;
        private TextBox tbx_MeterNo;
        private Label label100;
        private Label label101;
        private TabPage tabPage13;
        private Button bttn_ClearError;
        private Button bttn_StopError;
        private Button button3;
        private Button button4;
        private Label label102;
        private TextBox tbx_MeterConstant;
        private Label label103;
        private TextBox tbx_iMeterCount;
        private Label label104;
        private TextBox tbx_iPulse;
        private Button bttn_ErrorStart;
        private Label label105;
        private TextBox tbx_TaskDelay;
        private Label label106;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox groupBox10;
        private Label label107;
        private ComboBox cbbxSTAModel;
        private Button btnT1_ACCTRL;
        private Button btnT1_DCCTRL;
        private Label label108;
        private Label label109;
        private ComboBox cbxSTAModePinStatus;
        private Button bttnSTAHPin;
        private Button bttnSTALPin;
        private Button bttnReadSTAPinStatus;
        private ComboBox comboBoxSTAStutas;
        private GroupBox groupBox11;
        private Label label110;
        private Button button_SETLED1;
        private Button button_SETLED4;
        private Button button_SETLED3;
        private Button button_SETLED2;
        private CheckedListBox chexblx_LEDRGY;
        private Panel panel6;
        private Button buttonRead_Pulset;
        private Label label111;
        private TextBox tbxXYMeterPulse;
        private Button btn_ReadTime;
        private Label label112;
        private Panel panel7;
        private TextBox textBox4;
        private TextBox textBox3;
        private Button LgServer;
        private Label label114;
        private Label label113;
        private Label label115;
        private TabControl tabControl4;
        private TabPage tabPage14;
        private TabPage tabPage15;
        private Button button5;
        private TextBox textBox5;
        private Button button6;
        private TabPage tabPage16;
        private ComboBox ServerImp;
        private Label label116;
        private RichTextBox richTextBox1;
        private TabPage tabPage17;
        private TextBox textBoxSetUIRange;
        private Label label117;
        private Button button7;
        private Button tbxRangeOutputUI;
        private TextBox textBoxRangeOutputUI;
        private Label label118;
        private Panel panel8;
        private GroupBox groupBox12;
        private GroupBox groupBox13;
        private GroupBox groupBox14;
        private Label label119;
        private Button TCPServerConnent;
        private Panel panel9;
        private Label label22;
        private Button btn_changePCBDownAC;
        private Button btn_changePCBUPAC;
        private ComboBox cbx_changePCBUPAC;
        private ToolStrip toolStrip1;
        private ToolStripSplitButton toolStripButton1;
        private TabPage tabPage18;
        private TabControl tabControl6;
        private TabPage tabPage24;
        private Panel panel10;
        private Label label23;
        private Label label120;
        private ComboBox cbxSocketClass;
        private Label label121;
        private GroupBox groupBox15;
        private ComboBox cbxIp;
        private ComboBox cbxPort;
        private GroupBox groupBox16;
        private GroupBox groupBox17;
        private GroupBox groupBox18;
        private GroupBox groupBox19;
        private CheckBox cbxRevcASCII;
        private CheckBox cbxSendHEX;
        private CheckBox cbxSendASCII;
        private CheckBox cbxRevcHEX;
        private Panel panel11;
        private Button btnSendData;
        private RichTextBox rtbxSendData;
        private Button button8;
        private Button button2;
        private RichTextBox rtbxRevcData;
        private Label label122;
        private ComboBox cbxClientConnc;
        private Button button1;
        private CheckBox cbxIsBroadcastMessage;
        private Panel panel15;
        private Label label123;
        private Button btnelectriciansource;
        private Button btnstandardSource;
        private Panel panel16;
        private Button BtnTerminalTest;
        private Button BtnMeterTest;
        private ToolStripButton tsbtnTerminalTest;
        private ToolStripButton tsbtnMeterTest;
        private RichTextBox textBoxlog;
    }
}
