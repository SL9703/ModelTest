using System.ComponentModel;
using System.Data;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ModelTest.SerialPortImp;
using ModelTest.Socket_DLL;
using ModelTest.Socket_DLL.Socket_Client;
namespace ModelTest
{
    public partial class ModelMain : Form
    {

        // 在窗体类内部定义这个结构
        private struct ControlInfo
        {
            public string Name;
            public float FontSize;
            public System.Drawing.SizeF Size;
            public System.Drawing.Point Location;
        }

        //加密机对象
        private WinSocketServer winSocketServer = new WinSocketServer();
        //自定义串口对象
        private SerialPortSocket portSocket = new SerialPortSocket();
        // 获取UI线程的SynchronizationContext
        private readonly SynchronizationContext _uiContext;

        // 定义字典来存储所有控件的初始信息
        private Dictionary<System.Windows.Forms.Control, ControlInfo> _originalControlsInfo = new Dictionary<System.Windows.Forms.Control, ControlInfo>();
        private System.Drawing.Size _originalFormSize;
        public enum TerminalCLASS : byte
        {
            [Description("专变III")]
            Terminal_1 = 0x01,
            [Description("集中器")]
            Terminal_2 = 0x02,
            [Description("(模组化)专变")]
            Terminal_3 = 0x03,
            [Description("智能融合终端")]
            Terminal_4 = 0x04,
            [Description("单相物联网表")]
            Terminal_5 = 0x05,
            [Description("三相物联网表")]
            Terminal_6 = 0x06,
            [Description("单相智能电表")]
            Terminal_7 = 0x07,
            [Description("三相智能电表")]
            Terminal_8 = 0x08
        }
        public enum TerminalV1CLASS : byte
        {
            [Description("断开-无终端类型")]
            Terminal_0 = 0x00,
            [Description("台区智能融合终端")]
            Terminal_1 = 0x01,
            [Description("13版集中器I型")]
            Terminal_2 = 0x02,
            [Description("13版专变III型")]
            Terminal_3 = 0x03,
            [Description("22版集中器I型")]
            Terminal_4 = 0x04,
            [Description("22版专变III型")]
            Terminal_5 = 0x05,
            [Description("22版能源控制器")]
            Terminal_6 = 0x06,
            [Description("南网-负荷管理终端")]
            Terminal_7 = 0x07,
            [Description("南网-配变监测计量终端")]
            Terminal_8 = 0x08,
            [Description("南网-13集中器")]
            Terminal_9 = 0x09
        }
        string MCUStartByte = "55";
        string MCUStopByte = "AA";
        string UABC = string.Empty;
        string IABCN = string.Empty;
        string STAPINSET = string.Empty;
        public ModelMain()
        {
            InitializeComponent();
            _uiContext = SynchronizationContext.Current;
            // 处理UI线程异常
            Application.ThreadException += (sender, e) =>
            {
                MessageBox.Show($"UI线程异常: {e.Exception.Message}");
                LogMessage.Error(e.Exception);
            };
        }
        private SerialPort MainSerialPort = new SerialPort();//初始化串口
        private void ModelMain_Load(object sender, EventArgs e)
        {
            // 窗体加载时需要执行的初始化代码
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // 禁用最大化按钮
            this.MaximizeBox = false;

            // 可选：禁用最小化按钮
            // this.MinimizeBox = false;
            //设置背景颜色58957f
            this.BackColor = Color.FromArgb(88, 149, 127);

            // 可选：设置窗体不能最大化（额外保障）
            this.MaximumSize = this.MinimumSize = this.Size;
            cbxTerminalCLASS.DataSource = Enum.GetValues(typeof(TerminalCLASS)).Cast<TerminalCLASS>().Select(x => new
            {
                终端类型 = ModelTool.GetDescription(x)
            }).ToList();
            SerialPortinitialization();
            // 例如：初始化数据、配置控件等
            Control.CheckForIllegalCrossThreadCalls = false;//跨线程

            _originalFormSize = this.Size; // 保存窗体的初始大小
            // 递归遍历窗体上的所有控件（包括容器内的控件）
            StoreControlInfo(this);

            // 为窗体本身启用双缓冲
            this.DoubleBuffered = true;
            // 更有效的方法是设置以下样式，这对包含大量控件的窗体更有效
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();

            CheckItemSetUpFrom();
            checkBox1_CheckedChanged(sender, e);//初始化模组0x01 0x31命令选择状态
            cbxRevcHEX_CheckedChanged(sender, e);//初始化接收HEX状态。tcpserver用到
            cbxSendHEX_CheckedChanged(sender, e);//初始化发送HEX状态。tcpserver用到
            AddLog("应用程序已启动成功");
            LogMessage.Info("应用程序已启动成功");
        }


        private void StoreControlInfo(Control parentCtrl)
        {
            foreach (Control ctrl in parentCtrl.Controls)
            {
                // 存储当前控件的信息
                _originalControlsInfo.Add(ctrl, new ControlInfo
                {
                    Name = ctrl.Name,
                    FontSize = ctrl.Font.Size,
                    Size = ctrl.Size,
                    Location = ctrl.Location
                });

                // 如果当前控件本身也是一个容器（如Panel, GroupBox），则递归处理其子控件
                if (ctrl.Controls.Count > 0)
                {
                    StoreControlInfo(ctrl);
                }
            }
        }

        /// <summary>
        /// initialization port
        /// </summary>
        private void SerialPortinitialization()
        {

            comboBoxBaute.SelectedIndex = 6;
            comboBoxparity.SelectedIndex = 1;
            textBoxstopbit.SelectedIndex = 0;
            textBoxdatabit.SelectedIndex = 0;
            buttonOpen.BackColor = Color.YellowGreen;
            comboBoxCOM.Items.AddRange(SerialPort.GetPortNames());
            this.MainSerialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.MainSerialPort_DataReceived);
            btn_cilentSocket_Close.Enabled = false;
            btn_cilentSocket.Enabled = true;
            //初始化新跃电压电流选择信息
            comboBoxVA.SelectedIndex = 2;
            comboBoxVB.SelectedIndex = 2;
            comboBoxVC.SelectedIndex = 2;
            comboBoxIA.SelectedIndex = 0;
            comboBoxIB.SelectedIndex = 0;
            comboBoxIC.SelectedIndex = 0;
            cbxIAJ.SelectedIndex = 0;
            cbxIBJ.SelectedIndex = 0;
            cbxICJ.SelectedIndex = 0;
            cbx_Connection.SelectedIndex = 1;
            cbx_ratedcurrent.SelectedIndex = 1;
            cbx_ratedvoltage.SelectedIndex = 2;
            cbx_meterconstant.SelectedIndex = 3;
            cbx_HABC.SelectedIndex = 0;
            cbx_LC.SelectedIndex = 3;
            cbbx_BlueTooth.SelectedIndex = 0;
            cbbx_ToosNum.SelectedIndex = 0;
            cbbxSTAModel.SelectedIndex = 0;//选择sta模组
            cbxSTAModePinStatus.SelectedIndex = 0;//sta模块引脚状态
            comboBoxSTAStutas.SelectedIndex = 0;//读取sta模块状态用到
            cbxSocketClass.SelectedIndex = 1;//socket类型选择 tcpclient

            cbxTerminalV1.DataSource = Enum.GetValues(typeof(TerminalV1CLASS)).Cast<TerminalV1CLASS>().Select(x => new
            {
                终端类型 = ModelTool.GetDescription(x)
            }).ToList();
        }

        /// <summary>
        /// 连接client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private EnhancedTcpClient client;
        private async void btn_clientSocket_Click(object sender, EventArgs e)
        {
            string ip = textBoxIP.Text;
            int port = int.Parse(textBoxPort.Text);
            try
            {
                if (client == null)
                {
                    client = new EnhancedTcpClient();
                    // 订阅事件
                    client.MessageReceived += OnMCUMessageReceived;//监听服务器传来的消息事件
                    client.MessageSent += OnMessageSent;//传输文件事件
                    client.ConnectionStatusChanged += OnMCUConnectionStatusChanged;//连接状态改变事件
                    client.ErrorOccurred += OnErrorOccurred;
                    client.BytesTransferred += OnBytesTransferred;
                    bool connected = await client.ConnectAsync(ip, port);
                    if (connected)
                    {
                        btn_cilentSocket.Text = "关闭";
                        lblconnectStatus.Text = "TCP客户端状态：已连接";
                        lblconnectStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        AddLog($"TCP客户端 - 连接 {ip}:{port} 失败");
                        btn_cilentSocket.Text = "连接";
                        lblconnectStatus.Text = "TCP客户端状态：未连接";
                        lblconnectStatus.ForeColor = Color.Red;
                    }
                }
                else
                {
                    client.Disconnect();
                    client = null;
                    AddLog("状态：已断开");
                    lblconnectStatus.Text = "TCP客户端状态：未连接";
                    lblconnectStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error: {ex.Message}");
            }
        }
        private void OnMCUMessageReceived(object sender, TcpClientMessageEventArgs e)
        {

            UpdateUI(() =>
            {
                //显示原始数据
                string hexData = BitConverter.ToString(e.RawData).Replace("-", " ");
                string asciiData = Encoding.ASCII.GetString(e.RawData);
                // 更新状态显示
                if (cbxRevcASCII.Checked)
                {
                    AddLog($"接收消息成功[PC<--MCU]: {asciiData}", Color.Lime);
                    LogMessage.Debug($"接受消息成功[PC<-- MCU]的数据: {asciiData}");
                }
                else
                {
                    AddLog($"接收消息成功[PC<--MCU] : {hexData}", Color.Lime);
                    LogMessage.Debug($"接受消息成功[PC<-- MCU]的数据: {hexData}");
                }
            });
        }
        private void OnMCUConnectionStatusChanged(object sender, TcpClientStatusEventArgs e)
        {
            UpdateUI(() =>
            {
                string statusText = e.IsConnected ? "✅ 已连接" : "❌ 已断开";
                string color = e.IsConnected ? "Green" : "Red";

                AddLog($"[{e.Timestamp:HH:mm:ss}] {statusText}: {e.Status}");
                // 更新窗体标题
                if (e.IsConnected)
                {
                    this.Text = $"TCP客户端 - 已连接到 {client.ServerEndpoint}";
                }
                else if (client.Status == "Disconnected")
                {
                    this.Text = "TCP客户端 - 未连接";
                }
            });
        }
        /// <summary>
        /// 判断空方法
        /// </summary>
        /// <param name="mCU"></param>
        /// <returns></returns>
        private async Task SeedMethod(string mCU)
        {
            if (tbx_addr != null)
            {
                if (cbxTerminalCLASS.Items != null)
                {
                    if (tbxModelNumber != null)
                    {
                        if (mCU != null)
                        {
                            if (!cbxIsNoPortSeed.Checked && client != null)
                            {
                                bool send = await client.SendBytesAsync(ModelTool.HexStringToByteArray(mCU));
                                if (send)
                                {
                                    AddLog($"发送消息成功[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(mCU)).Replace("-", " ")}", Color.Red);
                                }
                                else
                                {
                                    AddLog($"发送消息成功[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(mCU)).Replace("-", " ")}", Color.Red);
                                }
                            }
                            else if (buttonOpen.Text == "CLOSE")
                            {
                                portSocket.SerialPortSendACSIIDataOrHexData(mCU, true);
                            }
                        }
                    }
                    else
                    {
                        AddLog("模块号不能为空");
                    }
                }
                else
                {
                    AddLog("终端类型不能为空");
                }
            }
            else
            {
                AddLog("地址不能为空");
            }
        }

        string A0600_DataLength = "0600";
        string A0700_DataLength = "0700";
        string A0800_DataLength = "0800";
        string MCUCtrl = "00";//控制协议
        string MCUTransparent = "00";//透传协议
        string MCUData_1 = string.Empty;
        string MCUData_2 = string.Empty;
        string CommandCode = string.Empty;
        string MCUAddr = string.Empty;
        string STA = string.Empty;
        string STAPINREAD = string.Empty;
        /// <summary>
        /// 直流上电按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerOn_DC_Click(object sender, EventArgs e)
        {
            //55 起始符
            //08 00  数据长度
            //01   地址通道
            //00    协议类型
            //01    命令码
            //03 01 数据项
            //0E    校验码
            //AA     
            LogMessage.Info(sender.ToString());
            MCUAddr = tbx_addr.Text;//地址
            Commande();//命令码
            MCUData_1 = ModuleModel.TerminalMeterAddr(cbxTerminalCLASS.SelectedIndex);
            ModelNumber();
            string MCUDCOn = ModuleModel.ModuleByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, CommandCode, MCUData_1 + MCUData_2, MCUStopByte);
            await SeedMethod(MCUDCOn);
        }
        private void Commande()
        {
            if (checkBox1.Checked)
            {
                CommandCode = "01";
            }
            else if (checkBox2.Checked)
            {
                CommandCode = "31";
            }
        }

        private void ModelNumber()
        {
            if (tbxModelNumber.Text == "1" || tbxModelNumber.Text == "01")
            {
                MCUData_2 = "01";
            }
            else if (tbxModelNumber.Text == "2" || tbxModelNumber.Text == "02")
            {
                MCUData_2 = "02";
            }
            else
            if (tbxModelNumber.Text == "3" || tbxModelNumber.Text == "03")
            {
                MCUData_2 = "04";
            }
            else if (tbxModelNumber.Text == "4" || tbxModelNumber.Text == "04")
            {
                MCUData_2 = "08";
            }
            else if (tbxModelNumber.Text == "5" || tbxModelNumber.Text == "05")
            {
                MCUData_2 = "10";
            }
        }
        /// <summary>
        /// 直流下电按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerDown_DC_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbx_addr.Text;//地址
            Commande();//命令码
            MCUData_1 = ModuleModel.TerminalMeterAddr(cbxTerminalCLASS.SelectedIndex);
            var MCUDCDown = ModuleModel.ModuleByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, CommandCode, MCUData_1 + "00", MCUStopByte);
            await SeedMethod(MCUDCDown);
        }
        /// <summary>
        /// 交流上电命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerOn_AC_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbx_addr.Text;//地址
            MCUData_1 = ModuleModel.TerminalMeterAddr(cbxTerminalCLASS.SelectedIndex);//终端类型，表地址
            AC_ABCN();
            var MCUACOn = ModuleModel.ModuleByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, "21", MCUData_1 + MCUData_2, MCUStopByte);
            await SeedMethod(MCUACOn);
        }
        private void AC_ABCN()
        {
            if (checkBoxA.Checked && checkBoxB.Checked && checkBoxC.Checked && !checkBoxN.Checked)
            {
                MCUData_2 = "07";
            }
            else if (checkBoxA.Checked && checkBoxB.Checked && !checkBoxC.Checked && !checkBoxN.Checked)
            {
                MCUData_2 = "03";
            }
            else if (checkBoxA.Checked && checkBoxC.Checked && !checkBoxB.Checked && !checkBoxN.Checked)
            {
                MCUData_2 = "05";
            }
            else if (checkBoxB.Checked && checkBoxC.Checked && !checkBoxA.Checked && !checkBoxN.Checked)
            {
                MCUData_2 = "06";
            }
            else if (checkBoxA.Checked && !checkBoxB.Checked && !checkBoxC.Checked && !checkBoxN.Checked)
            {
                MCUData_2 = "01";
            }
            else if (!checkBoxA.Checked && checkBoxB.Checked && !checkBoxC.Checked && !checkBoxN.Checked)
            {
                MCUData_2 = "02";
            }
            else if (!checkBoxA.Checked && !checkBoxB.Checked && checkBoxC.Checked && !checkBoxN.Checked)
            {
                MCUData_2 = "04";
            }
            else if (!checkBoxA.Checked && !checkBoxB.Checked && !checkBoxC.Checked && checkBoxN.Checked)
            {
                MCUData_2 = "08";
            }
            else
            {
                MCUData_2 = "00";
            }
        }

        /// <summary>
        /// 交流下电命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerDown_AC_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbx_addr.Text;//地址
            MCUData_1 = ModuleModel.TerminalMeterAddr(cbxTerminalCLASS.SelectedIndex);//终端类型，表地址
            var MCUACDown = ModuleModel.ModuleByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, "21", MCUData_1 + "00", MCUStopByte);
            await SeedMethod(MCUACDown);
        }
        private void AddLog(string Message)
        {
            textBoxlog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {Message}\r\n");
            textBoxlog.ScrollToCaret();
            LogMessage.Debug(Message);
        }
        /// <summary>
        /// 带颜色的日志输出
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="color"></param>
        private void AddLog(string Message, Color? color = null)
        {
            textBoxlog.SelectionLength = 0;
            textBoxlog.SelectionColor = color.Value;
            textBoxlog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {Message}+{Environment.NewLine}");
            textBoxlog.SelectionColor = textBoxlog.ForeColor;
            textBoxlog.ScrollToCaret();
            LogMessage.Debug(Message);
        }
        private void btn_cilentSocket_Close_Click(object sender, EventArgs e)
        {
            Dispose();
            btn_cilentSocket_Close.Enabled = false;
            btn_cilentSocket.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            // 初始化设置
            checkBox1.CheckedChanged += (s, e) =>
            {
                if (checkBox1.Checked)
                {
                    checkBox2.Checked = false;
                }
            };

            checkBox2.CheckedChanged += (s, e) =>
            {
                if (checkBox2.Checked)
                {
                    checkBox1.Checked = false;
                }
            };
        }
        /// <summary>
        /// 国网广播报文
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SGCC698FF_Click(object sender, EventArgs e)
        {
            await SeedMethod(label10.Text);
        }
        /// <summary>
        /// 国网645报文
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void SGCC645FF_Click(object sender, EventArgs e)
        {
            await SeedMethod(label11.Text);
        }
        /// <summary>
        /// 南网698报文
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CSG698FF_Click(object sender, EventArgs e)
        {
            await SeedMethod(label13.Text);
        }

        /// <summary>
        /// 打开串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOpen_Click(object sender, EventArgs e)
        {
            bool portIsopen = portSocket.OpenSerialPort(
                 MainSerialPort,
                 comboBoxCOM.Text,
                 Convert.ToInt32(comboBoxBaute.Text),
                 Convert.ToInt32(textBoxdatabit.Text),
                 comboBoxparity.Text,
                 textBoxstopbit.Text
                 );
            try
            {
                if (portIsopen)
                {
                    buttonOpen.Text = "OPEN";
                    buttonOpen.BackColor = Color.YellowGreen;
                    comboBoxCOM.Enabled = true;
                    comboBoxBaute.Enabled = true;
                    textBoxdatabit.Enabled = true;
                    textBoxstopbit.Enabled = true;
                    comboBoxparity.Enabled = true;
                    AddLog("串口已关闭");
                }
                else
                {
                    //串口已经关闭状态，需要设置好属性后打开
                    comboBoxCOM.Enabled = false;
                    comboBoxBaute.Enabled = false;
                    textBoxdatabit.Enabled = false;
                    textBoxstopbit.Enabled = false;
                    comboBoxparity.Enabled = false;
                    AddLog("串口已打开");
                    buttonOpen.Text = "CLOSE";
                    buttonOpen.BackColor = Color.IndianRed;
                }
            }
            catch (Exception ex_prot)
            {
                portSocket.SerialPortException(ex_prot);
                SerialPortException(ex_prot);
            }

        }

        private void SerialPortException(object ex)
        {
            comboBoxCOM.Items.Clear();
            comboBoxCOM.Items.AddRange(SerialPortSocket.GetPort());
            buttonOpen.Text = "OPEN";
            buttonOpen.BackColor = Color.YellowGreen;
            AddLog(ex?.ToString());
            comboBoxCOM.Enabled = true;
            comboBoxBaute.Enabled = true;
            textBoxdatabit.Enabled = true;
            textBoxstopbit.Enabled = true;
            comboBoxparity.Enabled = true;
        }
        /// <summary>
        /// 刷新串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnflushPort_Click(object sender, EventArgs e)
        {
            comboBoxCOM.Items.Clear();
            comboBoxCOM.Items.AddRange(SerialPortSocket.GetPort());
        }

        private long receive_count = 0;//接收字节数，全局变量
        private StringBuilder SerialSB = new StringBuilder();//
        /// <summary>
        /// 接收串口消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void MainSerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string portstr = portSocket.SeriPortDataRevice(true);
            AddLog(portstr);
        }


        /// <summary>
        /// 控制回路
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private async void buttonKZHLStatus_Click(object sender, EventArgs e)
        {
            await SeedMethod(label18.Text);
        }

        private async void buttonKZHLID_Click(object sender, EventArgs e)
        {
            await SeedMethod(label19.Text);
        }

        /// <summary>
        /// CCO直流上电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CCODCOn_Click(object sender, EventArgs e)
        {
            //55 07 00 addr MCUCtrl 01&31  01模组1  02模组2 check AA
            LogMessage.Info(sender.ToString());
            MCUAddr = tbx_addr.Text;//地址
            Commande();//命令码
            ModelNumber();//得到模块地址01 02  
            var CCODCOn = ModuleModel.ModuleByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, CommandCode, MCUData_2, MCUStopByte);
            await SeedMethod(CCODCOn);
        }
        /// <summary>
        /// CCO直流下电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CCODCDown_Click(object sender, EventArgs e)
        {
            //55 07 00 addr MCUCtrl 01&31  01模组1  02模组2 check AA
            LogMessage.Info(sender.ToString());
            MCUAddr = tbx_addr.Text;//地址
            Commande();//命令码
            var CCODCDown = ModuleModel.ModuleByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, CommandCode, MCUData_2, MCUStopByte);
            await SeedMethod(CCODCDown);
        }
        private async void CCOACOn_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbx_addr.Text;//地址
            AC_ABCN();
            var CCOACOn = ModuleModel.ModuleByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "02", MCUData_2, MCUStopByte);
            await SeedMethod(CCOACOn);
        }

        private async void CCOACDown_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbx_addr.Text;//地址
            AC_ABCN();
            var CCOACDown = ModuleModel.ModuleByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "02", "00", MCUStopByte);
            await SeedMethod(CCOACDown);
        }
        /// <summary>
        /// 终端单元切换终端类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnChangeTerminalClass_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            MCUData_1 = TerminalModel.GetTerminalClass(cbxTerminalV1.SelectedIndex);//选择终端类型
            var ChangeTerminalCls = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "2D", MCUData_1, MCUStopByte);//07 00 01 00 2d 00
            await SeedMethod(ChangeTerminalCls);
        }
        public void TerminalV1_UABC()
        {
            if (cbx_TerminalV1_UA.Checked && cbx_TerminalV1_UB.Checked && cbx_TerminalV1_UC.Checked)
            {
                UABC = "07";
            }
            else if (!cbx_TerminalV1_UA.Checked && cbx_TerminalV1_UB.Checked && cbx_TerminalV1_UC.Checked)
            {
                UABC = "06";
            }
            else if (cbx_TerminalV1_UA.Checked && !cbx_TerminalV1_UB.Checked && cbx_TerminalV1_UC.Checked)
            {
                UABC = "05";
            }
            else if (!cbx_TerminalV1_UA.Checked && !cbx_TerminalV1_UB.Checked && cbx_TerminalV1_UC.Checked)
            {
                UABC = "04";
            }
            else if (cbx_TerminalV1_UA.Checked && cbx_TerminalV1_UB.Checked && !cbx_TerminalV1_UC.Checked)
            {
                UABC = "03";
            }
            else if (!cbx_TerminalV1_UA.Checked && cbx_TerminalV1_UB.Checked && !cbx_TerminalV1_UC.Checked)
            {
                UABC = "02";
            }
            else if (cbx_TerminalV1_UA.Checked && !cbx_TerminalV1_UB.Checked && !cbx_TerminalV1_UC.Checked)
            {
                UABC = "01";
            }
            else if (!cbx_TerminalV1_UA.Checked && !cbx_TerminalV1_UB.Checked && !cbx_TerminalV1_UC.Checked)
            {
                UABC = "00";
            }
            else
            {
                UABC = "00";
            }
        }
        public void TerminalV1_IABC()
        {
            if (cbx_TerminalV1_IA.Checked && cbx_TerminalV1_IB.Checked && cbx_TerminalV1_IC.Checked)
            {
                IABCN = "07";
            }
            else if (!cbx_TerminalV1_IA.Checked && cbx_TerminalV1_IB.Checked && cbx_TerminalV1_IC.Checked)
            {
                IABCN = "06";
            }
            else if (cbx_TerminalV1_IA.Checked && !cbx_TerminalV1_IB.Checked && cbx_TerminalV1_IC.Checked)
            {
                IABCN = "05";
            }
            else if (!cbx_TerminalV1_IA.Checked && !cbx_TerminalV1_IB.Checked && cbx_TerminalV1_IC.Checked)
            {
                IABCN = "04";
            }
            else if (cbx_TerminalV1_IA.Checked && cbx_TerminalV1_IB.Checked && !cbx_TerminalV1_IC.Checked)
            {
                IABCN = "03";
            }
            else if (!cbx_TerminalV1_IA.Checked && cbx_TerminalV1_IB.Checked && !cbx_TerminalV1_IC.Checked)
            {
                IABCN = "02";
            }
            else if (cbx_TerminalV1_IA.Checked && !cbx_TerminalV1_IB.Checked && !cbx_TerminalV1_IC.Checked)
            {
                IABCN = "01";
            }
            else if (!cbx_TerminalV1_IA.Checked && !cbx_TerminalV1_IB.Checked && !cbx_TerminalV1_IC.Checked)
            {
                IABCN = "00";
            }
        }
        /// <summary>
        /// 接入电压 21
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_VOn_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            TerminalV1_UABC();//数据项
            var Terminal_PowerOn_V = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "21", UABC, MCUStopByte);//07 00 01 00 21 Uabc
            await SeedMethod(Terminal_PowerOn_V);
        }
        /// <summary>
        /// 断开电压21
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_VDown_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            var Terminal_PowerDown_V = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "21", "00", MCUStopByte);//07 00 01 00 21 00
            await SeedMethod(Terminal_PowerDown_V);
        }
        /// <summary>
        /// 接入电流22
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_AOn_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            TerminalV1_IABC();
            var Terminal_PowerOn_A = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "22", IABCN, MCUStopByte);//07 00 01 00 22 Iabc
            await SeedMethod(Terminal_PowerOn_A);
        }
        /// <summary>
        /// 断开电流22
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_ADown_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            var Terminal_PowerDown_A = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "22", "00", MCUStopByte);
            await SeedMethod(Terminal_PowerDown_A);
        }
        /// <summary>
        /// 电机压接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalV1MotorCrimping_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            var Terminal_MotorCrimping = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "29", "01", MCUStopByte);
            await SeedMethod(Terminal_MotorCrimping);
        }
        /// <summary>
        /// 电机退压接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalV1MotorCrimpingreturn_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            var Terminal_MotorCrimping = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "29", "00", MCUStopByte);
            await SeedMethod(Terminal_MotorCrimping);
        }
        /// <summary>
        /// 红灯控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool REDFlas = false;
        private async void pictureBoxRed_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (!REDFlas)
            {
                var Terminal_RedLoop = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUAddr, "2A", "20", MCUStopByte);
                await SeedMethod(Terminal_RedLoop);
                //if (Terminal_RedLoop.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "红灯.png");
                //    REDFlas = true;
                //}
            }
            else
            {
                var Terminal_RedLoop = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUAddr, "2A", "10", MCUStopByte);
                await SeedMethod(Terminal_RedLoop);
                //if (Terminal_RedLoop.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                //    REDFlas = false;
                //}
            }

        }
        /// <summary>
        /// 绿灯控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool GreenFlas = false;
        private async void pictureBoxGreen_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (!GreenFlas)
            {
                var Terminal_GreenLoop = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUAddr, "2A", "40", MCUStopByte);
                await SeedMethod(Terminal_GreenLoop);
                //if (Terminal_GreenLoop.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "绿灯.png");
                //    GreenFlas = true;
                //}
            }
            else
            {
                var Terminal_GreenLoop = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUAddr, "2A", "10", MCUStopByte);
                await SeedMethod(Terminal_GreenLoop);
                //if (Terminal_GreenLoop.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                //    GreenFlas = false;
                //}

            }
        }
        /// <summary>
        /// 清空日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 清空ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxlog.Text = "";
        }
        /// <summary>
        /// 切换背景色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 切换背景色ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxlog.ForeColor = Color.Black;
            textBoxlog.BackColor = Color.White;
        }
        /// <summary>
        /// 复制日志内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string strCopy = textBoxlog.SelectedText;
            Clipboard.SetDataObject(strCopy);
        }
        private bool TaiTiRed = false;
        private bool TaiTiGreen = false;
        private bool TaiTiYellow = false;
        /// <summary>
        /// 台体运行指示灯红
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pBTaiti_Red_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (TaiTiRed)
            {
                var Terminal_TaiTiRed = TerminalModel.TerminalByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, "2C", "0101", MCUStopByte);
                await SeedMethod(Terminal_TaiTiRed);
                //if (Terminal_TaiTiRed.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "红灯.png");
                //    GreenFlas = true;
                //}
            }
            else
            {
                var Terminal_TaiTiRed = TerminalModel.TerminalByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, "2C", "0100", MCUStopByte);
                await SeedMethod(Terminal_TaiTiRed);
                //if (Terminal_TaiTiRed.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                //    GreenFlas = true;
                //}
            }
        }
        /// <summary>
        /// 台体运行指示绿灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pBTaiti_Green_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (TaiTiGreen)
            {
                var Terminal_TaiTiGreen = TerminalModel.TerminalByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, "2C", "0201", MCUStopByte);
                await SeedMethod(Terminal_TaiTiGreen);
                //if (Terminal_TaiTiGreen.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "绿灯.png");
                //    GreenFlas = true;
                //}
            }
            else
            {
                var Terminal_TaiTiGreen = TerminalModel.TerminalByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, "2C", "0200", MCUStopByte);
                await SeedMethod(Terminal_TaiTiGreen);
                //if (Terminal_TaiTiGreen.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                //    GreenFlas = true;
                //}
            }
        }
        /// <summary>
        /// 台体运行指示黄灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pBTaiti_yellow_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (TaiTiYellow)
            {
                var Terminal_TaiTiYellow = TerminalModel.TerminalByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, "2C", "0301", MCUStopByte);
                await SeedMethod(Terminal_TaiTiYellow);
                //if (Terminal_TaiTiYellow.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "红灯.png");
                //    GreenFlas = true;
                //}
            }
            else
            {
                var Terminal_TaiTiYellow = TerminalModel.TerminalByte(MCUStartByte, A0800_DataLength, MCUAddr, MCUCtrl, "2C", "0300", MCUStopByte);
                await SeedMethod(Terminal_TaiTiYellow);
                //if (Terminal_TaiTiYellow.Contains(BitConverter.ToString(buffer)))
                //{
                //    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                //    GreenFlas = true;
                //}
            }
        }
        /// <summary>
        /// sta上下DC（直流电）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnT1_DCCTRL_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage.Info(sender.ToString());
                STA = TerminalModel.GetTerminalSTA1STA2Byte(cbbxSTAModel.Text);
                MCUAddr = tbxTerminalAdds.Text;//地址
                if (btnT1_DCCTRL.Text == "上直流电")
                {
                    var Terminal_STADCUP = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "3A", STA, MCUStopByte);
                    await SeedMethod(Terminal_STADCUP);
                    btnT1_DCCTRL.Text = "下直流电";
                }
                else if (btnT1_DCCTRL.Text == "下直流电")
                {
                    var Terminal_STADCDown = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "3A", "00", MCUStopByte);
                    await SeedMethod(Terminal_STADCDown);
                    btnT1_DCCTRL.Text = "上直流电";
                }
            }
            catch (Exception ex)
            {
                AddLog(ex.Message);
            }
        }
        /// <summary>
        /// sta上下AC（交流电）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnT1_ACCTRL_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage.Info(sender.ToString());
                STA = TerminalModel.GetTerminalSTA1STA2Byte(cbbxSTAModel.Text);
                MCUAddr = tbxTerminalAdds.Text;//地址
                if (btnT1_ACCTRL.Text == "上交流电")
                {
                    var Terminal_STAACUP = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "85", STA, MCUStopByte);
                    await SeedMethod(Terminal_STAACUP);
                    btnT1_ACCTRL.Text = "下交流电";
                }
                else if (btnT1_ACCTRL.Text == "下交流电")
                {
                    var Terminal_STAACUP = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "85", "00", MCUStopByte);
                    await SeedMethod(Terminal_STAACUP);
                    btnT1_ACCTRL.Text = "上交流电";
                }
            }
            catch (Exception ex)
            {
                AddLog(ex.Message);
            }
        }
        /// <summary>
        /// 设置sta模块高电平
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttnSTAHPin_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string STAPINSTATUS = TerminalModel.GetTerminalSTAPINByte(cbxSTAModePinStatus.Text);//获取设置)RST、SET、EVENT
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (cbxSTAModePinStatus.Text.Contains("_1"))
            {
                //设置单相表模块(STA1)RST、SET、EVENT引脚状态命令（0x3B）
                STAPINSET = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "3B", STAPINSTATUS, MCUStopByte); //sta1设置高
            }
            else if (cbxSTAModePinStatus.Text.Contains("_2"))
            {
                //设置单相表模块(STA2)RST、SET、EVENT引脚状态命令（0x86）
                STAPINSET = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "86", STAPINSTATUS, MCUStopByte); //sta1设置高
            }
            await SeedMethod(STAPINSET);
        }
        /// <summary>
        /// 设置sta模组低电平
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttnSTALPin_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (cbxSTAModePinStatus.Text.Contains("_1"))
            {
                //设置单相表模块(STA1)RST、SET、EVENT引脚状态命令（0x3B）
                STAPINSET = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "3B", "00", MCUStopByte); //sta1设置高
            }
            else if (cbxSTAModePinStatus.Text.Contains("_2"))
            {
                //设置单相表模块(STA2)RST、SET、EVENT引脚状态命令（0x86）
                STAPINSET = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "86", "00", MCUStopByte); //sta1设置高
            }
            await SeedMethod(STAPINSET);
        }
        /// <summary>
        /// 读取sta模组电平状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttnReadSTAPinStatus_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (comboBoxSTAStutas.Text.Contains("1"))
            {
                STAPINREAD = TerminalModel.TerminalByte(MCUStartByte, A0600_DataLength, MCUAddr, MCUCtrl, "3C", null, MCUStopByte);//读取sta1状态
            }
            if (comboBoxSTAStutas.Text.Contains("2"))
            {
                STAPINREAD = TerminalModel.TerminalByte(MCUStartByte, A0600_DataLength, MCUAddr, MCUCtrl, "87", null, MCUStopByte);//读取sta1状态
            }
            await SeedMethod(STAPINREAD);
        }
        /// <summary>
        /// led1点灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_SETLED1_Click(object sender, EventArgs e)
        {
            //55 0700 01 00 30 f1   xxAA
            //BIT0~BIT2分别表示LED亮红色1、绿色2、黄色4
            //BIT4~BIT8分别表示控制LED1=8,LED2,LED3,LED4（可以同时控制，也可单独控制）
            LogMessage.Info(sender.ToString());
            var LED_OneCtrl = string.Empty;
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (chexblx_LEDRGY.GetItemChecked(0))
            {
                LED_OneCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "09", MCUStopByte);
                button_SETLED1.BackColor = Color.Red;
                button_SETLED1.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(1))
            {
                LED_OneCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "0A", MCUStopByte);
                button_SETLED1.BackColor = Color.Green;
                button_SETLED1.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(2))
            {
                LED_OneCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "0C", MCUStopByte);
                button_SETLED1.BackColor = Color.Yellow;
                button_SETLED1.ForeColor = Color.Black;
            }
            else
            {
                LED_OneCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "00", MCUStopByte);
                button_SETLED1.BackColor = Color.Transparent;
                button_SETLED1.ForeColor = Color.Black;
            }
            try
            {
                await SeedMethod(LED_OneCtrl);
            }
            catch (Exception ex)
            {
                AddLog(ex.ToString());
            }

        }
        /// <summary>
        /// led2点灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_SETLED2_Click(object sender, EventArgs e)
        {
            //55 0700 01 00 30 f1   xxAA
            //BIT0~BIT2分别表示LED亮红色1、绿色2、黄色4
            //BIT4~BIT8分别表示控制LED1=8,LED2=16,LED3=32,LED4=32（可以同时控制，也可单独控制）
            LogMessage.Info(sender.ToString());
            var LED_TwoCtrl = string.Empty;
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (chexblx_LEDRGY.GetItemChecked(0))
            {
                LED_TwoCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "11", MCUStopByte);
                button_SETLED2.BackColor = Color.Red;
                button_SETLED2.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(1))
            {
                LED_TwoCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "12", MCUStopByte);
                button_SETLED2.BackColor = Color.Green;
                button_SETLED2.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(2))
            {
                LED_TwoCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "14", MCUStopByte);
                button_SETLED2.BackColor = Color.Yellow;
                button_SETLED2.ForeColor = Color.Black;
            }
            else
            {
                LED_TwoCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "00", MCUStopByte);
                button_SETLED2.BackColor = Color.Transparent;
                button_SETLED2.ForeColor = Color.Black;
            }
            try
            {
                await SeedMethod(LED_TwoCtrl);
            }
            catch (Exception ex)
            {
                AddLog(ex.ToString());
            }
        }
        /// <summary>
        /// led3点灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_SETLED3_Click(object sender, EventArgs e)
        {
            //55 0700 01 00 30 f1   xxAA
            //BIT0~BIT2分别表示LED亮红色1、绿色2、黄色4
            //BIT4~BIT8分别表示控制LED1=8,LED2=16,LED3=32,LED4=32（可以同时控制，也可单独控制）
            LogMessage.Info(sender.ToString());
            var LED_ThreeCtrl = string.Empty;
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (chexblx_LEDRGY.GetItemChecked(0))
            {
                LED_ThreeCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "21", MCUStopByte);
                button_SETLED3.BackColor = Color.Red;
                button_SETLED3.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(1))
            {
                LED_ThreeCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "22", MCUStopByte);
                button_SETLED3.BackColor = Color.Green;
                button_SETLED3.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(2))
            {
                LED_ThreeCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "24", MCUStopByte);
                button_SETLED3.BackColor = Color.Yellow;
                button_SETLED3.ForeColor = Color.Black;
            }
            else
            {
                LED_ThreeCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "00", MCUStopByte);
                button_SETLED3.BackColor = Color.Transparent;
                button_SETLED3.ForeColor = Color.Black;
            }
            try
            {
                await SeedMethod(LED_ThreeCtrl);
            }
            catch (Exception ex)
            {
                AddLog(ex.ToString());
            }
        }
        /// <summary>
        /// led4点灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_SETLED4_Click(object sender, EventArgs e)
        {

            //55 0700 01 00 30 f1   xxAA
            //BIT0~BIT2分别表示LED亮红色1、绿色2、黄色4
            //BIT4~BIT8分别表示控制LED1=8,LED2=16,LED3=32,LED4=64（可以同时控制，也可单独控制）
            LogMessage.Info(sender.ToString());
            var LED_FourCtrl = string.Empty;
            MCUAddr = tbxTerminalAdds.Text;//地址
            if (chexblx_LEDRGY.GetItemChecked(0))
            {
                LED_FourCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "41", MCUStopByte);
                button_SETLED4.BackColor = Color.Red;
                button_SETLED4.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(1))
            {
                LED_FourCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "42", MCUStopByte);
                button_SETLED4.BackColor = Color.Green;
                button_SETLED4.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(2))
            {
                LED_FourCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "44", MCUStopByte);
                button_SETLED4.BackColor = Color.Yellow;
                button_SETLED4.ForeColor = Color.Black;
            }
            else
            {
                LED_FourCtrl = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "30", "00", MCUStopByte);
                button_SETLED4.BackColor = Color.Transparent;
                button_SETLED4.ForeColor = Color.Black;
            }
            try
            {
                await SeedMethod(LED_FourCtrl);
            }
            catch (Exception ex)
            {
                AddLog(ex.ToString());
            }
        }
        /// <summary>
        /// 切换版上电 0x41
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btn_changePCBUPAC_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            var sourcestatus = TerminalModel.GetTerminalSourceType(cbx_changePCBUPAC.SelectedIndex);
            var Terminal_ChangePCBUpAC = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "41", $"{sourcestatus}01", MCUStopByte);//07 00 01 00 41 01 00
            await SeedMethod(Terminal_ChangePCBUpAC);
        }
        /// <summary>
        /// 切换版下电 0x41
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private async void btn_changePCBDownAC_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MCUAddr = tbxTerminalAdds.Text;//地址
            var sourcestatus = TerminalModel.GetTerminalSourceType(cbx_changePCBUPAC.SelectedIndex);
            var Terminal_ChangePCBDownAC = TerminalModel.TerminalByte(MCUStartByte, A0700_DataLength, MCUAddr, MCUCtrl, "41", $"{sourcestatus}00", MCUStopByte);//07 00 01 00 41 01 00
            await SeedMethod(Terminal_ChangePCBDownAC);
        }
        /// <summary>
        /// 标准表切换源 0x42 0x00 切换标准表源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnstandardSource_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 电工源切换源 0x42 0x01 切换电工源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnelectriciansource_Click(object sender, EventArgs e)
        {

        }
        #region 控源XY
        // 修复：实例化 XYCtr 对象以调用实例方法
        XYCtr xyCtr = new XYCtr();
        int OpenComm_data = 0;
        //private static int XYIresult; //源接口返回值
        public byte[] sStandValue = new byte[1024];//标准表数据缓存
        public string XYModel = "model1";//新跃源类型
        /// <summary>
        /// 降源按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonXY_x0E_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            if (cbxShutdownUI0.Checked && OpenComm_data == 1)
            {
                int ShutDownUI = 0;
                AddLog("输出给源电压电流参数：" + ShutDownUI);
                var result = xyCtr.CallShutPowerSource(ShutDownUI);
                if (result.Result == 1)
                {
                    AddLog("降源接口正常 返回值：" + result.Result.ToString());
                }
                else
                {
                    AddLog("降源失败，错误代码：" + result.Result.ToString());
                }
            }
            else
            {
                AddLog("降源异常，请检查串口是否链接");
            }
        }

        /// <summary>
        /// 初始化源的串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_SourcePort_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            try
            {
                if (OpenComm_data == 1)
                {
                    //串口是打开的状态
                    XYCtr.CallCloseComm();
                    btn_SourcePort.Text = "OPEN";
                    btn_SourcePort.BackColor = Color.YellowGreen;
                    AddLog("源串口已关闭");
                    OpenComm_data = 0;
                }
                else
                {
                    //初始化源串口
                    var result = xyCtr.CallOpenComm(int.Parse(tbx_sourcePort.Text));
                    if (result.Result == 1)
                    {
                        OpenComm_data = 1;
                        AddLog("源串口打开成功");
                        //串口已经关闭状态，需要设置好属性后打开
                        AddLog("源串口已打开");
                        btn_SourcePort.Text = "CLOSE";
                        btn_SourcePort.BackColor = Color.IndianRed;
                    }
                    else
                    {
                        AddLog("源串口打开失败，错误代码：" + OpenComm_data);
                        XYCtr.CallCloseComm();
                        btn_SourcePort.Text = "OPEN";
                        btn_SourcePort.BackColor = Color.YellowGreen;
                    }
                }
            }
            catch (Exception ex_prot)
            {
                SerialPortException(ex_prot);
            }
        }
        /// <summary>
        /// 读取标准表数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void btn_ReadStandMeter_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            if (OpenComm_data == 1)
            {

                var result = xyCtr.CallReadStandValue(XYModel, sStandValue);
                int ReadStandMeterResult = result.Result;
                if (ReadStandMeterResult == 1)
                {
                    AddLog("读标准表接口正常 返回值：" + ReadStandMeterResult.ToString());
                    var StandResult = ModelTool.SplitString(System.Text.Encoding.Default.GetString(sStandValue));
                    ShowTextReadStandValue(StandResult);//winform显示标准表数据
                    ModelTool.LogSplitResult(StandResult);
                }
                else
                {
                    AddLog("读标准表失败，错误代码：" + ReadStandMeterResult.ToString());
                }
            }
        }
        private void ShowTextReadStandValue(IList<string> processedParts)
        {

            try
            {
                //电压
                tb_UA.Text = processedParts[0];
                tb_UB.Text = processedParts[1];
                tb_UC.Text = processedParts[2];
                //电流       
                tb_IA.Text = processedParts[3];
                tb_IB.Text = processedParts[4];
                tb_IC.Text = processedParts[5];
                //相位角     
                tb_XA.Text = processedParts[6];
                tb_XB.Text = processedParts[7];
                tb_XC.Text = processedParts[8];
                //有功       
                tb_PA.Text = processedParts[9];
                tb_PB.Text = processedParts[10];
                tb_PC.Text = processedParts[11];
                //无功       
                tb_QA.Text = processedParts[12];
                tb_QB.Text = processedParts[13];
                tb_QC.Text = processedParts[14];
                //频率       
                tb_HZ.Text = processedParts[15];
                //报警
                tb_Alarm.Text = processedParts[16];
                //uba
                tb_Uba.Text = processedParts[17];
                //uca
                tb_Uca.Text = processedParts[18];
                //相线
                tb_xx.Text = processedParts[19];
                //电压量程
                tb_V_LC.Text = processedParts[20];
                //电流量程
                tb_A_LC.Text = processedParts[21];
                //
                //tb_SA.Text = processedParts[0];
                //tb_SB.Text = processedParts[0];
                //tb_SC.Text = processedParts[0];

                //tb_PFA.Text = processedParts[0];
                //tb_PFB.Text = processedParts[0];
                //tb_PFC.Text = processedParts[0];

                //tb_EP.Text = processedParts[0];
                //tb_EQ.Text = processedParts[0];
                //tb_ES.Text = processedParts[0];
            }
            catch (Exception ex)
            {
                AddLog("显示标准表信息异常:" + ex.ToString());
            }
        }
        private void CmdReadMeterData_Click(object sender, EventArgs e)
        {
            byte[] sResultData;
            sResultData = new byte[255];
            var result = xyCtr.CallReadTestData(0, 0, sResultData);
            if (result.Result == 1)
            {
                AddLog("读取装置信息成功：" + System.Text.Encoding.Default.GetString(sResultData));
            }
            else
            {
                AddLog("读取装置信息失败，错误代码：" + result.Result);
            }
        }
        /// <summary>
        /// 电压电流输出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonCtrlUI_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            if (OpenComm_data == 1)
            {
                //源参数;电压电流 电压电流夹脚， uab uac夹脚
                //byte[] U_I_F_Uab_Uac = new byte[1024];
                string ui = comboBoxVA.Text + "_" +
                comboBoxVB.Text + "_" +
                comboBoxVC.Text + "_" +
                comboBoxIA.Text + "_" +
                comboBoxIB.Text + "_" +
                comboBoxIC.Text + "_" +
                cbxIAJ.Text + "_" +
                cbxIBJ.Text + "_" +
                cbxICJ.Text + "_" +
                cbxUab.Text + "_" +
                cbxUac.Text;
                //合并数组
                AddLog("输出给源电压电流参数：" + ui);
                //是否进行误差仪计算
                int iPulse = int.Parse(tbxiPulse.Text);
                var result = xyCtr.CallAnyUIOutput(ui, iPulse);
                if (result.Result == 1)
                {
                    AddLog("控源接口正常 返回值：" + result.Result.ToString());
                }
                else
                {
                    AddLog("控源失败，错误代码：" + result.Result.ToString());
                }
            }
        }
        /// <summary>
        /// 读取常数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ReadContans_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            byte[] constas = new byte[1024];
            var result = xyCtr.CallReadStandConst(constas);
            if (result.Result == 1)
            {
                AddLog("读取常数接口正常" + result.Result);
                tb_contans.Text = System.Text.Encoding.Default.GetString(constas);
            }
            else
            {
                AddLog("调用读取常数接口异常" + result.Result);
            }
        }
        /// <summary>
        /// 初始化电表参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        string MeterConnection;
        string MeterV;
        string LC;
        private void btn_Init_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            MeterConnection = XYCtr.Init_meterConnection(cbx_Connection.Text);
            MeterV = XYCtr.Init_meterV(cbx_ratedvoltage.Text);
            var MeterInit = $"Ini_{MeterConnection}_{MeterV}_{cbx_ratedcurrent.Text}_{cbx_meterconstant.Text}_E";
            AddLog("初始化电表参数" + MeterInit);
            var result = xyCtr.CallSendCommand(MeterInit, true);
            if (result.Result == 1)
            {
                AddLog("初始化电表接口正常" + result.Result);
            }
            else
            {
                AddLog("初始化电表接口异常" + result.Result);
            }
        }
        /// <summary>
        /// adj 升源接口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_XY_ADJ_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            LC = XYCtr.ADJLC_CHANGE(cbx_LC.Text);
            var AdjCMD = $"Adj_{tbx_V_5.Text}_{tbx_A_5.Text}_{cbx_HABC.Text}_{LC}_{tbxiPulse.Text}_E";
            AddLog("ADJ升源接口指令" + AdjCMD);
            var result = xyCtr.CallSendCommand(AdjCMD, true);
            if (result.Result == 1)
            {
                AddLog("ADJ升源接口正常" + result.Result);
            }
            else
            {
                AddLog("ADJ升源接口异常" + result.Result);
            }
        }
        int BluetoothMode = 0; //接线模式 0-常规接线 1-蓝牙 2-双光电头
        int BluetoothChannel = 3; //通道号 3-有功 4-无功 
        /// <summary>
        /// 设置蓝牙模式和通道 
        /// 先设置模式
        ///常规接线模式
        ///蓝牙模块接线模式
        ///双光电头接线模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        bool BoolTooth = true;//模式选择
        private void bttn_settooth_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            if (BoolTooth)
            {
                BluetoothMode = XYCtr.BlueTooth_Channel(cbbx_BlueTooth.Text);
                AddLog("设置模式：" + cbbx_BlueTooth.Text);
                var result = xyCtr.CallSet_BlueTooth_Channel(BluetoothMode);
                if (result.Result == 1)
                {
                    AddLog("设置成功");
                }
                else
                {
                    AddLog("设置失败，错误代码：" + result.Result);
                }
                bttn_settooth.Text = "设置通道"; //切换到设置通道
                BoolTooth = false; //切换到设置通道 
            }
            else
            {
                //设置通道
                BluetoothChannel = XYCtr.BlueTooth_Channel(cbbx_ToosNum.Text);
                AddLog("设置通道：" + cbbx_ToosNum.Text);
                var result = xyCtr.CallSet_BlueTooth_Channel(BluetoothChannel);
                if (result.Result == 1)
                {
                    AddLog("设置成功");
                }
                else
                {
                    AddLog("设置失败，错误代码：" + result.Result);
                }
                bttn_settooth.Text = "设置模式"; //切换到设置模式
                BoolTooth = true; //切换到设置模式
            }
        }
        /// <summary>
        /// 时钟误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttn_ClockStart_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            bttn_ClockStart.Enabled = false;
            int iPulse = int.Parse(tbxclockpulse.Text);//时钟误差数
            AddLog("时钟误差数：" + iPulse);

            var result = xyCtr.Call_Clock_Start(iPulse);
            if (result.Result == 1)
            {
                AddLog($"开始测试时钟误差,延迟等待{iPulse + iPulse}秒，等待结束自动读取误差数据。");
                await Task.Delay(iPulse * 1000 + iPulse * 1000);//延迟等待
            }
            //读取误差
            ReadTestError();
            bttn_ClockStart.Enabled = true;

        }
        /// <summary>
        /// 读取时钟误差
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void ReadTestError()
        {
            byte[] MeterError = new byte[1024];
            string[] meterNo = tbx_MeterNo.Text.Split('-');//分割字符串
            if (meterNo.Length == 2 && int.TryParse(meterNo[0], out int firtNo) && int.TryParse(meterNo[1], out int ListNo))
            {
                for (int i = firtNo; i < ListNo + 1; i++)
                {
                    var result = xyCtr.CallReadTestData(1, i, MeterError);
                    if (result.Result == 1)
                    {
                        AddLog($"正在读取{i}表位误差数据...");
                        AddLog($"误差数据" + MeterError + "+\r\n");
                    }
                }
            }
            else if (meterNo.Length != 2)
            {
                AddLog("电表编号格式错误，请输入正确的格式，如：1-1,1-12");
                return;
            }
        }
        private void ReadTestError_1(int meter)
        {
            byte[] MeterError = new byte[1024];
            for (int i = 1; i <= meter; i++)
            {
                AddLog($"正在读取{i}表位误差数据...");
                var result = xyCtr.Call_Read_TestError(i, MeterError);
                if (result.Result == 1)
                {
                    AddLog($"误差数据" + MeterError + "+\r\n");
                }
            }
        }
        private async void bttn_ErrorStart_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            //获取传入的参数
            string MeterConstant = tbx_MeterConstant.Text;//电表常数
            int MeterCount = int.Parse(tbx_iMeterCount.Text);//表位数
            int Pulse = int.Parse(tbx_iPulse.Text);//圈数
            StringBuilder CMeterConstant = new StringBuilder();
            for (int i = 1; i <= MeterCount; i++)
            {
                if (i < MeterCount)
                {
                    CMeterConstant.Append(tbx_MeterConstant.Text + ",");
                }
                else
                {
                    CMeterConstant.Append(tbx_MeterConstant.Text);
                }
            }
            AddLog($"误差启动=>  Error_Start({CMeterConstant.ToString()},{MeterCount},{Pulse})");
            var result = xyCtr.Call_Error_Start(CMeterConstant.ToString(), MeterCount, Pulse);
            if (result.Result == 1)
            {
                AddLog("启动误差接口正常" + result.Result);
                //添加延迟等待
                await Task.Delay(int.Parse(tbx_TaskDelay.Text) * 1000);
                //读取实验误差
                ReadTestError_1(MeterCount);
            }
            else
            {
                AddLog("调用启动误差接口异常" + result.Result);
            }

        }

        /// <summary>
        /// 停止误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bttn_StopError_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            var result = xyCtr.Call_Stop_Test();
            if (result.Result == 1)
            {
                AddLog("停止误差接口正常" + result.Result);
            }
            else
            {
                AddLog("调用停止误差接口异常" + result.Result);
            }
        }
        /// <summary>
        /// 清除误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bttn_ClearError_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            var result = xyCtr.Call_Error_Clear();
            if (result.Result == 1)
            {
                AddLog("清除误差接口正常" + result.Result);
            }
            else
            {
                AddLog(("调用清除误差接口异常" + result.Result));
            }
        }
        /// <summary>
        /// 读取表位的脉冲数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRead_Pulset_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            byte[] MeterError = new byte[1024];
            var result = xyCtr.Call_Read_Pulse(int.Parse(tbxXYMeterPulse.Text), MeterError);
            if (result.Result == 1)
            {
                AddLog("读取表位的脉冲数接口正常" + result.Result);
            }
            else
            {
                AddLog(("读取表位的脉冲数接口异常" + result.Result));
            }
        }

        /// <summary>
        /// 读取新跃dll版本日期
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ReadTime_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            byte[] StrVer = new byte[1024];
            var result = xyCtr.CallFunctionReadVersion(StrVer);
            if (result.Result == 1)
            {
                AddLog("读取新跃dll版本日期接口正常" + result.Result);
            }
            else
            {
                AddLog("读取新跃dll版本日期接口异常" + result.Result);
            }
        }
        /// <summary>
        /// setui设置电压和电流量程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            //得到数据，分割数据，
            string ServerData = textBoxSetUIRange.Text;
            string[] ServerDataNew = ModelTool.StringDataSplit(ServerData);
            int Iui = int.Parse(ServerDataNew[0]);
            int Ivalue = int.Parse(ServerDataNew[1]);
            var result = xyCtr.CallSetUIRange(Iui, Ivalue);
            if (result.Result > 0)
            {
                AddLog("设置setui接口正常" + result.Result);
            }
            else
            {
                AddLog("设置setui接口异常" + result.Result);
            }
        }

        /// <summary>
        /// rangui
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void RangeOutputUI_Click(object sender, EventArgs e)
        {
            string RangeOutputUIData = textBoxRangeOutputUI.Text;
            string[] ServerDataNew = ModelTool.StringDataSplit(RangeOutputUIData);
            string ua = ServerDataNew[0];
            string ub = ServerDataNew[1];
            string uc = ServerDataNew[2];
            string ia = ServerDataNew[3];
            string ib = ServerDataNew[4];
            string ic = ServerDataNew[5];
            string StrUICommand = $"{ua}_{ub}_{uc}_{ia}_{ib}_{ic}";
            var result = xyCtr.CallRangeOutputUI(StrUICommand);
            if (result.Result == 1)
            {
                AddLog("设置RangeOutputUI接口正常" + result.Result);
            }
            else
            {
                AddLog("设置RangeOutputUI接口异常" + result.Result);
            }
        }

        #endregion
        private void ModelMain_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                Screen currentScreen = Screen.FromControl(this);
                this.MaximizedBounds = currentScreen.WorkingArea;

                // 双重保障
                this.Height = currentScreen.WorkingArea.Height;
                this.Top = currentScreen.WorkingArea.Top;
            }
            // 防止在窗体最小化时执行计算
            if (this.WindowState == FormWindowState.Minimized || _originalFormSize.Width == 0 || _originalFormSize.Height == 0)
                return;

            // 计算宽度和高度的缩放比例
            float scaleX = (float)this.Width / _originalFormSize.Width;
            float scaleY = (float)this.Height / _originalFormSize.Height;

            // 选择一个保守的比例（通常取最小值以保证内容不会溢出）
            float scale = Math.Min(scaleX, scaleY);
            // 应用缩放到所有控件
            ApplyScaling(this, scale);
        }

        private void ApplyScaling(Control parentCtrl, float scale)
        {
            foreach (Control ctrl in parentCtrl.Controls)
            {
                // 从字典中获取该控件的原始信息
                if (_originalControlsInfo.TryGetValue(ctrl, out ControlInfo originalInfo))
                {
                    // 缩放大小
                    ctrl.Width = (int)(originalInfo.Size.Width * scale);
                    ctrl.Height = (int)(originalInfo.Size.Height * scale);

                    // 缩放位置
                    ctrl.Left = (int)(originalInfo.Location.X * scale);
                    ctrl.Top = (int)(originalInfo.Location.Y * scale);

                    // 缩放字体（可选，根据需求决定）
                    // 创建一个新的Font对象，基于原始字体大小进行缩放
                    ctrl.Font = new Font(ctrl.Font.FontFamily, originalInfo.FontSize * scale, ctrl.Font.Style);
                }

                // 递归处理子控件
                if (ctrl.Controls.Count > 0)
                {
                    ApplyScaling(ctrl, scale);
                }
            }
        }

        private void ModelMain_SizeChanged(object sender, EventArgs e)
        {
            // 直接调用Resize事件的处理逻辑
            ModelMain_Resize(sender, e);
        }

        private void tabControl1_DrawItem(object sender, DrawItemEventArgs e)
        {
            Font fntTab;
            Brush bshBack;
            Brush bshFore;
            if (e.Index == this.tabControl1.SelectedIndex)    //当前Tab页的样式
            {
                fntTab = e.Font;
                bshBack = new SolidBrush(Color.FromArgb(88, 149, 127)); //选中的标签颜色变为国网绿色
                bshFore = new SolidBrush(Color.Black);
            }
            else    //其余Tab页的样式
            {
                fntTab = new Font(e.Font, FontStyle.Bold);
                bshBack = new System.Drawing.Drawing2D.LinearGradientBrush(e.Bounds, SystemColors.Control, SystemColors.Control,
                                                                           System.Drawing.Drawing2D.LinearGradientMode.BackwardDiagonal);
                bshFore = Brushes.Black;
            }
            //画样式
            string tabName = this.tabControl1.TabPages[e.Index].Text;
            StringFormat sftTab = new StringFormat();
            sftTab.Alignment = StringAlignment.Near;  //水平方向居中
            sftTab.LineAlignment = StringAlignment.Center;   //垂直方向居中 
            e.Graphics.FillRectangle(bshBack, e.Bounds);
            Rectangle recTab = e.Bounds;
            recTab = new Rectangle(recTab.X, recTab.Y, recTab.Width + 20, recTab.Height - 4);
            e.Graphics.DrawString(tabName, fntTab, bshFore, recTab, sftTab);
        }
        private int NumMax = 1;//任意给值
        private int beforeindex = 0;
        private void chexblx_LEDRGY_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.CurrentValue == CheckState.Checked) return;
            int truecount = 0;
            for (int i = 0; i < chexblx_LEDRGY.Items.Count; i++)
            {
                if (chexblx_LEDRGY.GetItemChecked(i))
                {
                    truecount++;
                }
            }
            if (truecount >= NumMax)//判断当前选项是否超出限制范围
            {
                ((CheckedListBox)sender).SetItemChecked(beforeindex, false);
            }
            beforeindex = e.Index;//记住前一次选择的索引值
            e.NewValue = CheckState.Checked;
        }

        private void LgServer_Click(object sender, EventArgs e)
        {
            byte[] OutRandNum = new byte[128];
            string ServerIp = textBox4.Text;
            string ServerPort = textBox3.Text;
            Thread thread = new Thread(() =>
            {
                int rtnConnnt = winSocketServer.ConnectDeviceEx(ServerIp, ServerPort, "8000");
                if (rtnConnnt == 0)
                {
                    AddLog("连接服务器成功！");
                    label115.Text = "服务器连接状态：已连接";
                    //获取随机数
                    int ret = winSocketServer.CreateRandEx(16, OutRandNum);
                    if (ret == 0)
                    {
                        richTextBox1.AppendText("获取随机数成功！" + System.Text.Encoding.Default.GetString(OutRandNum));
                    }
                    else
                    {
                        richTextBox1.AppendText("获取随机数失败！" + System.Text.Encoding.Default.GetString(OutRandNum));
                    }

                }
                else
                {
                    AddLog("连接服务器失败，错误代码：" + rtnConnnt);
                    label115.Text = "服务器连接状态：连接服务器失败";
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// 加密数据接口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(() => { });
            //cOutSID, cOutAttachData, cOutData, cOutMAC
            byte[] cOutSID = new byte[256];
            byte[] cOutAttachData = new byte[256];
            byte[] cOutData = new byte[256];
            byte[] cOutMAC = new byte[256];
            try
            {
                //得到数据，分割数据，
                string ServerData = textBox5.Text;

                // 分割字符串并移除空条目
                string[] parts = ServerData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                // 处理特殊值：将 "None" 转换为 null，数值保持原样
                var ServerDataNew = parts.Select(p => p == "None" ? "None" : p).ToArray();
                int result = 0;
                switch (ServerImp.Text)
                {

                    case "":
                        AddLog("请右上角选择加密算法！");
                        break;
                    case "RESAM_Formal_GetKeyData_AppLayer":
                        thread = new Thread(() =>
                        {
                            AddLog($"调用接口：{ServerImp.Text}");
                            AddLog($"传入参数===>iOperateMode{ServerDataNew[0]}，cTESAMID{ServerDataNew[1]}，" +
                                $"cSessionKey{ServerDataNew[2]}，cTaskType{ServerDataNew[3]}，cTaskData{ServerDataNew[4]}");
                            result = winSocketServer.ReSAM_Formal_GetKeyData_AppLayer
                              (int.Parse(ServerDataNew[0]),
                              ServerDataNew[1],
                              ServerDataNew[2],
                              int.Parse(ServerDataNew[3]),
                              ServerDataNew[4],
                               cOutSID,
                               cOutAttachData,
                               cOutData,
                               cOutMAC
                              );
                            //cOutSID, cOutAttachData, cOutData, cOutMAC
                            PrintServerMeassRes(result);
                            richTextBox1.AppendText($"加密机返回数据：cOutSID={System.Text.Encoding.Default.GetString(cOutSID)}，" +
                                                                $"cOutAttachData={System.Text.Encoding.Default.GetString(cOutAttachData)}," +
                                                                $"cOutData={System.Text.Encoding.Default.GetString(cOutData)}," +
                                                                $"cOutMAC={System.Text.Encoding.Default.GetString(cOutMAC)}");
                        });
                        break;
                    case "CloseDevice":
                        thread = new Thread(() =>
                        {
                            AddLog($"调用接口：{ServerImp.Text}");
                            result = winSocketServer.CloseDeviceEx();
                            PrintServerMeassRes(result);
                        });
                        break;
                    case "ClseUsbkey":
                        thread = new Thread(() =>
                        {
                            AddLog($"调用接口：{ServerImp.Text}");
                            result = winSocketServer.ClseUsbkeyEx();
                            PrintServerMeassRes(result);
                        });
                        break;
                    case "Meter_Formal_DataClear1":
                        AddLog($"调用接口：{ServerImp.Text}");
                        AddLog($"传入参数===>Flag={ServerDataNew[0]},PutRand={ServerDataNew[1]}," +
                           $"PutDiv={ServerDataNew[2]},PutData={ServerDataNew[3]}");
                        result = winSocketServer.Meter_Formal_DataClear1Ex(int.Parse(ServerDataNew[0]), ServerDataNew[1], ServerDataNew[2], ServerDataNew[3], cOutData);
                        PrintServerMeassRes(result);
                        richTextBox1.AppendText($"加密机返回数据cOutMAC={System.Text.Encoding.Default.GetString(cOutData)}");
                        break;
                    case "Meter_Formal_DataClear2":
                        AddLog($"调用接口：{ServerImp.Text}");
                        AddLog($"传入参数===>Flag={ServerDataNew[0]},PutRand={ServerDataNew[1]}," +
                           $"PutDiv={ServerDataNew[2]},PutData={ServerDataNew[3]}");
                        result = winSocketServer.Meter_Formal_DataClear2Ex(int.Parse(ServerDataNew[0]), ServerDataNew[1], ServerDataNew[2], ServerDataNew[3], cOutData);
                        PrintServerMeassRes(result);
                        richTextBox1.AppendText($"加密机返回数据cOutMAC={System.Text.Encoding.Default.GetString(cOutData)}");
                        break;
                    case "Obj_Meter_Formal_SetESAMData":
                        AddLog($"调用接口：{ServerImp.Text}");
                        AddLog($"传入参数InKeyState={ServerDataNew[0]},InOperateMode={ServerDataNew[1]}," +
                            $"cESAMNO={ServerDataNew[2]},cSessionKey={ServerDataNew[3]}," +
                            $"cMeterNo={ServerDataNew[4]},cESAMRand={ServerDataNew[5]},cData={ServerDataNew[6]}");
                        thread = new Thread(() =>
                        {
                            result = winSocketServer.Call_Obj_Meter_Formal_SetESAMData
                                (int.Parse(ServerDataNew[0]),
                                int.Parse(ServerDataNew[1]),
                                ServerDataNew[2],
                                ServerDataNew[3],
                                ServerDataNew[4],
                                ServerDataNew[5],
                                ServerDataNew[6],
                                 cOutSID,
                                 cOutAttachData,
                                 cOutData,
                                 cOutMAC
                                );
                            if (result == 0)
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------成功");
                                Thread.Sleep(2000);
                                richTextBox1.AppendText($"加密机返回数据：cOutSID={System.Text.Encoding.Default.GetString(cOutSID)}+\r\n");
                                richTextBox1.AppendText($"cOutAttachData={System.Text.Encoding.Default.GetString(cOutAttachData)}+\r\n");
                                richTextBox1.AppendText($"cOutData={System.Text.Encoding.Default.GetString(cOutData)}+\r\n");
                                richTextBox1.AppendText($"cOutMAC={System.Text.Encoding.Default.GetString(cOutMAC)}+\r\n");
                            }
                            else
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------失败,返回值：{result}");
                            }
                        });
                        thread.IsBackground = true;
                        thread.Start();
                        break;
                    case "Obj_Terminal_Formal_GetTrmKeyData":
                        AddLog($"调用接口：{ServerImp.Text}");
                        AddLog($"传入参数iKeyVersion={ServerDataNew[0]},strEsamNo={ServerDataNew[1]}," +
                            $"strSessionKey={ServerDataNew[2]},cTerminalAddress={ServerDataNew[3]}," +
                            $"strKeyType={ServerDataNew[4]}");
                        thread = new Thread(() =>
                        {
                            result = winSocketServer.Obj_Terminal_Formal_GetTrmKeyDataEx(
                                int.Parse(ServerDataNew[0]),
                                ServerDataNew[1],
                                ServerDataNew[2],
                                ServerDataNew[3],
                                ServerDataNew[4],
                                 cOutSID,
                                 cOutAttachData,
                                 cOutData,
                                 cOutMAC
                                );
                            if (result == 0)
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------成功");
                                Thread.Sleep(2000);
                                richTextBox1.AppendText($"加密机返回数据：cOutSID={System.Text.Encoding.Default.GetString(cOutSID)}+\r\n");
                                richTextBox1.AppendText($"cOutAttachData={System.Text.Encoding.Default.GetString(cOutAttachData)}+\r\n");
                                richTextBox1.AppendText($"strOutTrmKeyData={System.Text.Encoding.Default.GetString(cOutData)}+\r\n");
                                richTextBox1.AppendText($"cOutMAC={System.Text.Encoding.Default.GetString(cOutMAC)}+\r\n");
                            }
                            else
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------失败,返回值：{result}");
                            }
                        });
                        thread.IsBackground = true;
                        thread.Start();
                        break;
                    case "Obj_Terminal_Formal_InitSession":
                        AddLog($"调用接口：{ServerImp.Text}");
                        AddLog($"传入参数iKeyVersion={ServerDataNew[0]},strEsamNo={ServerDataNew[1]}," +
                           $"strSessionKey={ServerDataNew[2]},cTerminalAddress={ServerDataNew[3]}," +
                           $"strKeyType={ServerDataNew[4]}");
                        thread = new Thread(() =>
                        {
                            int result = winSocketServer.Obj_Terminal_Formal_InitSessionEx(
                                 int.Parse(ServerDataNew[0]),
                                 ServerDataNew[1],
                                 ServerDataNew[2],
                                 ServerDataNew[3],
                                 ServerDataNew[4],
                                  cOutSID,
                                  cOutAttachData,
                                  cOutData
                                 );
                            if (result == 0)
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------成功");
                                Thread.Sleep(2000);
                                richTextBox1.AppendText($"加密机返回数据：cOutSID={System.Text.Encoding.Default.GetString(cOutSID)}+\r\n");
                                richTextBox1.AppendText($"cOutAttachData={System.Text.Encoding.Default.GetString(cOutAttachData)}+\r\n");
                                richTextBox1.AppendText($"strOutTrmKeyData={System.Text.Encoding.Default.GetString(cOutData)}+\r\n");
                            }
                            else
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------失败,返回值：{result}");
                            }
                        });
                        thread.IsBackground = true;
                        thread.Start();
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                AddLog(ex.Message);
            }

        }

        private void PrintServerMeassRes(int result)
        {
            if (result == 0)
            {
                AddLog($"调用接口：{ServerImp.Text}----------成功");
            }
            else
            {
                AddLog($"调用接口：{ServerImp.Text}----------失败,返回值：{result}");
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 加密机接口选项发生改变之后
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerImp_SelectedIndexChanged(object sender, EventArgs e)
        {
            string ServerImpType = (string)ServerImp.SelectedItem;
            switch (ServerImpType)
            {
                case "RESAM_Formal_GetKeyData_AppLayer":
                    AddLog($"选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("int iOperateMode,char * cTESAMID, char * cSessionKey,int cTaskType, char * cTaskData, char * cOutSID,char * cOutAttachData, char * cOutData ,char * cOutMAC");
                    break;
                case "CloseDevice":
                    AddLog($"连接密码机，用于断开服务器或密码机连接；选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("无参数");
                    break;
                case "ClseUsbkey":
                    AddLog($"释放服务器登录权限，兼容 09 版电能表使用的函数；选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("无参数");
                    break;
                case "Meter_Formal_DataClear1":
                    AddLog($"用于远程费控电能表清零；选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("int Flag整型,0:公钥;1,私钥；10，双协议公钥；11，双协议私钥；,char *PutRand随机数 2,电表身份认证成功返回, 4 字节,char *PutDiv分散因子,8 字节,“0000”+表号,char *PutData入参,清零数据,char *Outdata  20字节密文；");
                    break;
                case "Meter_Formal_DataClear2":
                    AddLog($"用于事件或需量清零函数；选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("int Flag13 版，整型,0:公钥;1,私钥； 16 版，1,私钥；10，面向对象；,char *PutRand随机数 2,电表身份认证成功返回, 4 字节,char *PutDiv分散因子,8 字节,“0000”+表号,char *PutData入参,清零数据,char *Outdata  20字节密文；");
                    break;
                case "Obj_Meter_Formal_SetESAMData":
                    AddLog($"选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("int InKeyState,int InOperateMode,char * cESAMNO, char * cSessionKey, char * cMeterNo, char * cESAMRand, char * cData, char * OutSID,char * OutAddData, char * OutData,char * OutMAC");
                    break;
                case "Obj_Terminal_Formal_GetTrmKeyData":
                    AddLog($"选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("char* iKeyVersion 密钥更新的目标状态  “00000000000000000000000000000000” 表示恢复到公钥，其他相同长度非全零数据表示更新到私钥\r\nchar* strEsamNo ESAM 序列号\r\nchar* strSessionKey 会话密钥\r\nchar* cTerminalAddress 终端地址(8 Bytes)\r\nchar* strKeyType 密钥类型，00 应用密钥，01 链路密钥");
                    break;
                case "Obj_Terminal_Formal_InitSession":
                    AddLog($"选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("输入参数iKeyState，cTESAMID，cASCTR，cFLG，cMasterCert");
                    break;
                default:
                    break;
            }
        }
        private void CheckItemSetUpFrom()
        {

            ServerImp.SelectedIndexChanged -= ServerImp_SelectedIndexChanged;
            ServerImp.DataSource = winSocketServer.WinSocketSericeImp();
            ServerImp.SelectedIndex = -1;
            ServerImp.SelectedIndexChanged += ServerImp_SelectedIndexChanged;
        }
        /// <summary>
        /// textbox只能输入数字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxOnlyNumber_KeyPressed(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar >= '0' && e.KeyChar <= '9' || e.KeyChar == 8)
            {
                e.Handled = false;//这可以输入
            }
            else
            {
                e.Handled = true;//不能输入
            }
        }
        /// <summary>
        /// textbox只能输入字母
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextboxOnlyAz_keyPress(object sender, KeyPressEventArgs e)
        {

            // e.KeyChar == 8 退格 删除
            if ((e.KeyChar >= 'a' && e.KeyChar <= 'z') || (e.KeyChar >= 'A' && e.KeyChar <= 'Z') || e.KeyChar == 8)
            {
                e.Handled = false;//这可以输入
            }
            else
            {
                e.Handled = true;//不能输入
            }
        }
        /// <summary>
        /// 启动tcp侦听服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private Socket_DLL.Socket_DLL server;
        private EnhancedTcpClient _tcpClient;
        //获取客户端名称字典
        private readonly Dictionary<string, string> _clientNames = new Dictionary<string, string>();
        private async void TCPServerConnent_Click(object sender, EventArgs e)
        {
            try
            {
                string ServerIp = cbxIp.Text;
                int ServerPort = int.Parse(cbxPort.Text);
                server = new Socket_DLL.Socket_DLL(ServerIp, ServerPort);
                if (ServerPort < 1 || ServerPort > 65535)
                {
                    AddLog(ServerIp + "端口号输入不正确，请输入1-65535之间的数字！");
                    return;
                }
                switch (TCPServerConnent.Text)
                {
                    case "TCP服务器侦听":
                        //启动服务
                        var serverTask = Task.Run(() => server.StartAsync());
                        // 订阅服务器事件
                        server.MessageReceived += OnMessageReceived;
                        server.ClientConnected += OnClientConnected;
                        server.ClientDisconnected += OnClientDisconnected;
                        server.ServerError += OnServerError;
                        server.ServerStatusChanged += OnServerStatusChanged;
                        AddLog("启动TCP侦听服务器成功，监听IP：" + ServerIp + "，端口：" + ServerPort);
                        lblconnectStatus.Text = "TCP服务器状态：已启动";
                        lblconnectStatus.ForeColor = Color.Green;
                        TCPServerConnent.Text = "关闭TCP服务器";
                        break;
                    case "关闭TCP服务器":
                        //关闭服务
                        server.Stop();
                        server.Dispose();
                        AddLog("关闭TCP侦听服务器成功");
                        lblconnectStatus.Text = "TCP服务器状态：已关闭";
                        lblconnectStatus.ForeColor = Color.Red;
                        TCPServerConnent.Text = "TCP服务器侦听";
                        break;
                    case "TCP客户端连接":
                        _tcpClient = new EnhancedTcpClient();
                        // 订阅事件
                        _tcpClient.MessageReceived += OnMessageReceived;//监听服务器传来的消息事件
                        _tcpClient.MessageSent += OnMessageSent;//传输文件事件
                        _tcpClient.ConnectionStatusChanged += OnConnectionStatusChanged;//连接状态改变事件
                        _tcpClient.ErrorOccurred += OnErrorOccurred;
                        _tcpClient.BytesTransferred += OnBytesTransferred;
                        bool connected = await _tcpClient.ConnectAsync(ServerIp, ServerPort);
                        if (connected)
                        {
                            TCPServerConnent.Text = "关闭TCP客户端";
                            lblconnectStatus.Text = "TCP客户端状态：已连接";
                            lblconnectStatus.ForeColor = Color.Green;
                        }
                        else
                        {
                            AddLog($"TCP客户端 - 连接 {ServerIp}:{ServerPort} 失败");
                            TCPServerConnent.Text = "TCP客户端连接";
                            lblconnectStatus.Text = "TCP客户端状态：未连接";
                            lblconnectStatus.ForeColor = Color.Red;
                        }
                        break;
                    case "关闭TCP客户端":
                        _tcpClient.Disconnect();
                        TCPServerConnent.Text = "TCP客户端连接";
                        lblconnectStatus.Text = "TCP客户端状态：断开连接";
                        lblconnectStatus.ForeColor = Color.Red;
                        break;
                    case "UDP服务器侦听":
                        break;
                    case "关闭UDP服务器":
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogMessage.Error(ex);
            }
        }
        /// <summary>
        /// 客户端发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <exception cref="NotImplementedException"></exception>
        private async Task SendClienttMessage(string message, bool IsASCIIorHex)
        {
            bool sent = false;
            if (IsASCIIorHex)
            {
                sent = await _tcpClient.SendAsync(message);//发送acsii消息
            }
            else
            {
                sent = await _tcpClient.SendBytesAsync(ModelTool.HexStringToByteArray(message));//发送hex消息
            }
            UpdateUI(() =>
            {
                if (sent)
                {
                    AddLog($"发送: {message}");
                    LogMessage.SocketLog($"发送消息--> {message} 至服务器成功");
                }
                else
                {
                    AddLog($"发送失败: {message}");
                    LogMessage.SocketLog($"发送消息--> {message} 至服务器失败");
                }
            });
        }
        /// <summary>
        /// 客户端 、服务器发送消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSendData_Click(object sender, EventArgs e)
        {
            switch (TCPServerConnent.Text)
            {
                case "关闭TCP客户端":
                    if (_tcpClient == null)
                    {
                        AddLog("错误，客户端未连接");
                        return;
                    }
                    _ = SendClienttMessage(rtbxSendData.Text, cbxSendASCII.Checked);
                    break;
                case "关闭TCP服务器":
                    if (server == null || !server.IsRunning)
                    {
                        AddLog("错误，服务器未启动");
                        return;
                    }
                    if (!cbxIsBroadcastMessage.Checked)//非广播
                    {
                        _ = SendMessage(cbxClientConnc.Text, rtbxSendData.Text);
                    }
                    else
                    {
                        _ = BroadcastMessage(rtbxSendData.Text);//广播
                    }
                    break;

                default:
                    break;
            }


        }
        /// <summary>
        /// 发送消息到指定客户端
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SendMessage(string clientId, string message)
        {
            bool success;
            if (string.IsNullOrEmpty(clientId))
            {
                AddLog("错误,请选择或输入客户端ID");
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                AddLog("错误消息不能为空");
                return;
            }
            if (cbxSendHEX.Checked)
            {
                success = await server.SendAsync(clientId, message, true);
            }
            else
            {
                success = await server.SendAsync(clientId, message, false);
            }
            if (success)
            {
                LogMessage.SocketLog($"发送消息--> {message} 至客户端 {clientId}");
                rtbxRevcData.AppendText($"[{DateTime.Now:HH:mm:ss.fff}]发送消息--> {message} 至客户端 {clientId}\r\n");
            }
            else
            {
                //AddLog($"发送失败,无法发送消息到客户端 {clientId}");
                rtbxRevcData.AppendText($"[{DateTime.Now:HH:mm:ss.fff}]发送失败,无法发送消息到客户端{clientId}\r\n");
            }
        }
        /// <summary>
        /// 广播消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task BroadcastMessage(string message)
        {
            bool success;
            if (string.IsNullOrEmpty(message))
            {
                rtbxRevcData.AppendText("错误，消息不能为空");
                return;
            }
            if (cbxSendHEX.Checked)
            {
                success = await server.BroadcastAsync(message, true);
            }
            else
            {
                success = await server.BroadcastAsync(message, false);
            }
            rtbxRevcData.AppendText(success ? "广播消息成功" : "广播消息失败");
        }

        #region TCP客户端事件处理
        private void OnMessageReceived(object sender, TcpClientMessageEventArgs e)
        {

            UpdateUI(() =>
            {
                //显示原始数据
                string hexData = BitConverter.ToString(e.RawData).Replace("-", " ");
                string asciiData = Encoding.ASCII.GetString(e.RawData);
                // 更新状态显示
                if (cbxRevcASCII.Checked)
                {
                    rtbxRevcData.AppendText($"接收 [{e.Timestamp:HH:mm:ss.fff}]: {asciiData}\r\n");
                    LogMessage.SocketLog($"接受消息<-- 服务器 的数据: {asciiData}");
                }
                else
                {
                    rtbxRevcData.AppendText($"接收 [{e.Timestamp:HH:mm:ss.fff}]: {hexData}\r\n");
                    LogMessage.SocketLog($"接受消息<-- 服务器 的数据: {hexData}");
                }
                UpdateStatusDisplay();
            });
        }
        private void UpdateStatusDisplay()
        {
            rtbxRevcData.AppendText(_tcpClient.GetStatistics());
        }
        private void OnMessageSent(object sender, TcpClientMessageEventArgs e)
        {
            UpdateUI(() =>
            {
                // 发送消息已经在BtnSend_Click中记录，这里只记录文件传输等特殊消息
                if (e.Message.Contains("文件传输进度") || e.Message.Contains("FILE_"))
                {
                    AddLog($"发送: {e.Message}");
                }
            });
        }
        private void OnConnectionStatusChanged(object sender, TcpClientStatusEventArgs e)
        {
            UpdateUI(() =>
            {
                string statusText = e.IsConnected ? "✅ 已连接" : "❌ 已断开";
                string color = e.IsConnected ? "Green" : "Red";

                AddLog($"[{e.Timestamp:HH:mm:ss}] {statusText}: {e.Status}");
                // 更新窗体标题
                if (e.IsConnected)
                {
                    this.Text = $"TCP客户端 - 已连接到 {_tcpClient.ServerEndpoint}";
                }
                else if (_tcpClient.Status == "Disconnected")
                {
                    this.Text = "TCP客户端 - 未连接";
                }

                // 更新状态显示
                UpdateStatusDisplay();
            });
        }

        private void OnErrorOccurred(object sender, string errorMessage)
        {
            UpdateUI(() =>
            {
                AddLog($"[错误] {errorMessage}");
            });
        }

        private void OnBytesTransferred(object sender, long bytes)
        {
            // 可以在这里更新传输统计
        }
        #endregion
        private void UpdateUI(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }
        /// <summary>
        /// tcp服务器打印日志更新连接状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="statusMessage"></param>
        private void OnServerStatusChanged(object? sender, string statusMessage)
        {
            UpdateUI(() =>
            {
                AddLog($"[状态] {statusMessage}");
            });
        }
        /// <summary>
        /// tcp服务器报错日志打印
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="error"></param>

        private void OnServerError(object? sender, string error)
        {
            UpdateUI(() =>
            {
                AddLog($"[错误] {error}");
            });
        }
        /// <summary>
        /// tcp服务端事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClientDisconnected(object sender, ClientStatusChangedEventArgs e)
        {
            UpdateUI(() =>
            {
                _clientNames.Remove(e.ClientId);
                UpdateClientList();
                AddLog($"[{e.ChangeTime:HH:mm:ss}] 客户端断开: {e.ClientEndpoint}");
            });
        }

        /// <summary>
        /// tcp服务端事件处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClientConnected(object sender, ClientStatusChangedEventArgs e)
        {
            UpdateUI(() =>
            {
                _clientNames[e.ClientId] = $"客户端 {e.ClientId.Substring(0, 8)}";
                UpdateClientList();
                AddLog($"[{e.ChangeTime:HH:mm:ss}] 客户端连接: {e.ClientEndpoint}");
            });
        }
        /// <summary>
        /// tcp服务器收到消息处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();
            UpdateUI(() =>
            {
                // 更新客户端列表活动状态
                UpdateClientList();

                // 添加到日志
                LogMessage.SocketLog($"接受消息<-- {e.ClientEndpoint} 的数据: {e.Message}");
                //转换成byte数组数据
                byte[] dataBytes = System.Text.Encoding.Default.GetBytes(e.Message);
                if (cbxRevcHEX.Checked)
                {
                    foreach (byte b in dataBytes)
                    {
                        sb.Append(b.ToString("X2") + ' ');//将byte型数据转化为2位16进制文本显示,用空格隔开
                    }
                }
                else
                {
                    sb.Append(Encoding.ASCII.GetString(dataBytes));  //将整个数组解码为ASCII数组
                    //var log = $"[{e.ReceivedTime:HH:mm:ss}] 来自 {e.ClientEndpoint} 的数据: {e.Message}";
                    //rtbxRevcData.AppendText(log + "\r\n");
                }
                var log = $"[{e.ReceivedTime:HH:mm:ss}] 来自 {e.ClientEndpoint} 的数据: {sb.ToString()}";
                rtbxRevcData.AppendText(log + "\r\n");
                // 可以在这里处理特定消息并返回给特定控件
            });
        }
        private void UpdateClientList()
        {
            cbxClientConnc.Items.Clear();
            foreach (var clientInfo in server.GetAllClientInfos())
            {
                var displayName = _clientNames.ContainsKey(clientInfo.Id)
                    ? _clientNames[clientInfo.Id]
                    : clientInfo.Endpoint;

                var item = $"{clientInfo.Id.ToString()}";
                cbxClientConnc.Items.Add(item);
            }
        }
        /// <summary>
        /// 网络类型选择改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbxSocketClass_SelectedIndexChanged(object sender, EventArgs e)
        {
            //UDP
            //TCPClient
            //TCPServer
            string SocketType = cbxSocketClass.Text;

            switch (SocketType)
            {
                case "UDP":
                    SelectNet();
                    TCPServerConnent.Text = "UDP服务器侦听";
                    break;
                case "TCPClient":
                    SelectNet();
                    TCPServerConnent.Text = "TCP客户端连接";
                    break;
                case "TCPServer":
                    SelectNet();
                    TCPServerConnent.Text = "TCP服务器侦听";
                    break;
                default:
                    break;
            }
        }

        private void SelectNet()
        {
            var localIPs = GetLocalIPv4Addresses();
            cbxIp.Items.Clear();
            label23.Text = "（2）远程主机地址";
            label120.Text = "（3）远程主机端口";
            foreach (var ip in localIPs)
            {
                cbxIp.Items.Add(ip.ToString());
            }
            cbxIp.SelectedIndex = 0;
        }

        static string[] GetLocalIPv4Addresses()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToArray();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            rtbxRevcData.Text = "";
        }

        private void button8_Click(object sender, EventArgs e)
        {
            rtbxSendData.Text = "";
        }

        private void cbxRevcHEX_CheckedChanged(object sender, EventArgs e)
        {
            // 初始化设置
            cbxRevcHEX.CheckedChanged += (s, e) =>
            {
                if (cbxRevcHEX.Checked)
                {
                    cbxRevcASCII.Checked = false;
                }
            };

            cbxRevcASCII.CheckedChanged += (s, e) =>
            {
                if (cbxRevcASCII.Checked)
                {
                    cbxRevcHEX.Checked = false;
                }
            };
        }

        private void cbxSendHEX_CheckedChanged(object sender, EventArgs e)
        {
            // 初始化设置
            cbxSendHEX.CheckedChanged += (s, e) =>
            {
                if (cbxSendHEX.Checked)
                {
                    cbxSendASCII.Checked = false;
                }
            };

            cbxSendASCII.CheckedChanged += (s, e) =>
            {
                if (cbxSendASCII.Checked)
                {
                    cbxSendHEX.Checked = false;
                }
            };
        }
        /// <summary>
        /// 终端检测
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void tsbtnTerminalTest_Click(object sender, EventArgs e)
        {
            TerminalTest terminalTest = new TerminalTest();
            terminalTest.OwnerForm = this;
            this.Hide();
            terminalTest.Show();
        }
        /// <summary>
        /// 电表检测
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbtnMeterTest_Click(object sender, EventArgs e)
        {
            MeterTest.MeterTest meterTest = new MeterTest.MeterTest();
            meterTest.OwnerForm = this;
            this.Hide();
            meterTest.Show();
        }
        
    }
}
