using System.ComponentModel;
using System.Data;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ModelTest.CustomControl;
using ModelTest.Protocol;
using ModelTest.SerialPortImp;
using ModelTest.Socket_DLL;
using ModelTest.Socket_DLL.Socket_Client;
using ModelTest.Tools;
using static System.Windows.Forms.AxHost;
namespace ModelTest
{
    public partial class ModelMain : Form
    {
        private sealed record TerminalTypeOption<TEnum>(TEnum Value, string Text) where TEnum : struct, Enum;
        //自定义串口对象
        private SerialPortSocket portSocket = new SerialPortSocket();
        // 获取UI线程的SynchronizationContext
        private readonly SynchronizationContext _uiContext;
        private ShowStandValueUserControl _standValueUserControl;
        private TerminalV1YXUserControl _terminalV1YXUserControl;
        private ElectricEnergyMeterControlV1 _MeterV1UserControl;
        private ElectricEnergyMeterControlV2 _MeterV2UserControl;
        private UDPMessageUserControl _udpMessageUserControl;
        private SHUserControl _shUserControl;
        private SGCCEncryptionServiceUserControl? _sgccEncryptionService;
        private MultifunctionalcommunicationUserControl? _multifunctionalcommunicationUserControl;
        private TerminalTest? _terminalTestForm;
        private MeterTest.MeterTest? _meterTestForm;
        private DatabaseTestForm? _databaseTestForm;
        private LinuxCommandForm? _linuxCommandForm;
        private ProtocolParserForm? _protocolParserForm;
        private TerminalV2UserControl? _terminalV2UserControl;
        private SerialPortServerConfigForm? _serialPortServerConfigForm;
        public ModelMain()
        {
            InitializeComponent();
            InitializeSerialPortServerConfigEntry();//串口服务器窗体加载
            ConfigureMainWindowBounds();
            LoadApplicationIcon();
            InitializeMeterTestDatabase();
            UpdateStatusTime();
            InitializeSGCCTestTab();
            InitializeMultifunctionalCommunicationTab();

            ultrSimpleDisplay1.TerminalAddressProvider = () => tbxTerminalAdds.Text;
            ultrSimpleDisplay1.LogRequested += AddLog;
            ultrSimpleDisplay1.SendCommandRequested += SeedMethod;
            //源界面初始化
            _standValueUserControl = new ShowStandValueUserControl();
            _standValueUserControl.OnUpdateRequested += MyControl_OnUpdateRequested;
            panel13.Controls.Add(_standValueUserControl);
            _standValueUserControl.Dock = DockStyle.Fill;
            //终端界面遥信初始化
            _terminalV1YXUserControl = new TerminalV1YXUserControl();
            _terminalV1YXUserControl.OnUpdateRequestedTYXLog += MyControl_OnUpdateRequested;
            tabPage10.Controls.Add(_terminalV1YXUserControl);
            _terminalV1YXUserControl.Dock = DockStyle.Fill;
            //终端V2界面初始化
            _terminalV2UserControl = new TerminalV2UserControl();
            _terminalV2UserControl.OnUpdateRequestedTerminalV2Log += MyControl_OnUpdateRequested;
            tabPage5.Controls.Add(_terminalV2UserControl);
            _terminalV2UserControl.Dock = DockStyle.Fill;
            //电表V1界面初始化
            _MeterV1UserControl = new ElectricEnergyMeterControlV1();
            _MeterV1UserControl.OnUpdateRequested_MeterV1 += MyControl_OnUpdateRequested;
            tabPage4.Controls.Add(_MeterV1UserControl);
            _MeterV1UserControl.Dock = DockStyle.Fill;

            //电表V2界面初始化
            _MeterV2UserControl = new ElectricEnergyMeterControlV2();
            _MeterV2UserControl.OnUpdateRequested_MeterV2 += MyControl_OnUpdateRequested;
            tabPage6.Controls.Add(_MeterV2UserControl);
            _MeterV2UserControl.Dock = DockStyle.Fill;


            //UDP 消息界面
            _udpMessageUserControl = new UDPMessageUserControl();
            _udpMessageUserControl.OnUpdateRequested_UDPMessage += MyControl_OnUpdateRequested;
            tabPage_UDP.Controls.Add(_udpMessageUserControl);
            _udpMessageUserControl.Dock = DockStyle.Fill;

            //SH源控制界面
            _shUserControl = new SHUserControl();
            _shUserControl.OnUpdateRequestedSHLog += MyControl_OnUpdateRequested;
            tabPage11.Controls.Add(_shUserControl);
            _shUserControl.Dock = DockStyle.Fill;
            //加密机控制界面
            _sgccEncryptionService = new SGCCEncryptionServiceUserControl();
            _sgccEncryptionService.OnUpdateRequestedEncryptionLog += MyControl_OnUpdateRequested;
            tabPage8.Controls.Clear();
            tabPage8.Controls.Add(_sgccEncryptionService);
            _sgccEncryptionService.Dock = DockStyle.Fill;
            _uiContext = SynchronizationContext.Current;
            // 处理UI线程异常
            Application.ThreadException += (sender, e) =>
            {
                MessageBox.Show($"UI线程异常: {e.Exception.Message}");
                LogMessage.Error(e.Exception);
            };
        }
        /// <summary>
        /// 在通信单元的 TCP/UDP 服务器区域增加串口服务器查看和配置入口。
        /// 按钮点击后打开独立窗体，窗体内部使用 GenericSerialPortServerProtocol 发送查看、设置和保存指令。
        /// </summary>
        private void InitializeSerialPortServerConfigEntry()
        {
            Panel hostPanel = new()
            {
                Name = "panelSerialPortServerConfigHost",
                Dock = DockStyle.Right,
                Width = 200,
                Padding = new Padding(15, 24, 15, 0),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            Button button = new()
            {
                Name = "btnSerialPortServerConfig",
                Text = "串口服务器查看和配置",
                Dock = DockStyle.Top,
                Height = 75,
                BackColor = Color.FromArgb(42, 135, 104),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 134),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += SerialPortServerConfigButton_Click;

            hostPanel.Controls.Add(button);
            groupBox14.Controls.Add(hostPanel);
            hostPanel.BringToFront();
            button.BringToFront();
        }

        /// <summary>显示串口服务器查看和配置窗体；已有窗体时激活，避免重复打开。</summary>
        private void SerialPortServerConfigButton_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_serialPortServerConfigForm is { IsDisposed: false })
                {
                    _serialPortServerConfigForm.Activate();
                    return;
                }

                _serialPortServerConfigForm = new SerialPortServerConfigForm();
                _serialPortServerConfigForm.Show();
            }
            catch (Exception ex)
            {
                LogMessage.Error(ex);
                MessageBox.Show($"打开串口服务器配置窗体失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// 配置主窗体的启动边界。
        ///
        /// 主窗体保持最大化，但将最大化边界限制为当前屏幕工作区，避免覆盖 Windows 底部任务栏。
        /// </summary>
        private void ConfigureMainWindowBounds()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;

            Screen screen = IsHandleCreated
                ? Screen.FromHandle(Handle)
                : Screen.PrimaryScreen ?? Screen.AllScreens[0];
            Rectangle workingArea = screen.WorkingArea;
            MaximizedBounds = workingArea;
            WindowState = FormWindowState.Maximized;
        }

        private void UpdateMainWindowMaximizedBounds()
        {
            Screen screen = IsHandleCreated
                ? Screen.FromHandle(Handle)
                : Screen.PrimaryScreen ?? Screen.AllScreens[0];
            MaximizedBounds = screen.WorkingArea;
            WindowState = FormWindowState.Maximized;
        }

        private void UpdateStatusTime()
        {
            toolStripStatusTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        private void InitializeMeterTestDatabase()
        {
            try
            {
                MeterTest.MeterTestAccessDatabaseService accessDatabaseService = new();
                accessDatabaseService.EnsureInitialized();
            }
            catch (Exception ex)
            {
                LogMessage.Debug($"MeterTest 本地数据库主界面初始化失败：{ex.Message}");
            }
        }

        private void StatusTimeTimer_Tick(object? sender, EventArgs e)
        {
            UpdateStatusTime();
        }

        private void LoadApplicationIcon()
        {
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (!File.Exists(iconPath))
            {
                iconPath = Path.Combine(Application.StartupPath, "Assets", "AppIcon.ico");
            }
            if (File.Exists(iconPath))
            {
                Icon = new Icon(iconPath);
            }
        }
        private void InitializeSGCCTestTab()
        {
            SGCCTestUserControl sgccTestUserControl = new SGCCTestUserControl
            {
                Dock = DockStyle.Fill
            };
            sgccTestUserControl.SendMessageRequested += SeedMethod;
            sgccTestUserControl.LogRequested += AddLog;

            tabPage2.Controls.Clear();
            tabPage2.Controls.Add(sgccTestUserControl);
        }
        private SerialPort MainSerialPort = new SerialPort();//初始化串口
        private void ModelMain_Load(object sender, EventArgs e)
        {
            // 窗体加载时重新读取当前屏幕工作区，确保最大化边界不覆盖 Windows 底部任务栏。
            UpdateMainWindowMaximizedBounds();
            //设置背景颜色58957f
            this.BackColor = Color.FromArgb(88, 149, 127);
            BindTerminalClassOptions();
            SerialPortinitialization();
            // 例如：初始化数据、配置控件等
            Control.CheckForIllegalCrossThreadCalls = false;//跨线程
            // 为窗体本身启用双缓冲
            this.DoubleBuffered = true;
            // 更有效的方法是设置以下样式，这对包含大量控件的窗体更有效
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            this.UpdateStyles();
            ModelTool.BindMutexCheckBoxes(checkBox1, checkBox2);//初始化模组0x01 0x31命令选择状态
            ModelTool.BindMutexCheckBoxes(cbxRealModel01, cbxRealModel31);//初始化真实模组0x01 0x31命令选择状态
            ModelTool.BindMutexCheckBoxes(checkBoxC, checkBoxN);//初始化模组IC和IN命令选择状态
            ModelTool.BindMutexCheckBoxes(cbx_TerminalV1_IC, cbx_TerminalV1_IN);//初始化终端IC和IN命令选择状态

            AddLog("应用程序已启动成功");
            LogMessage.Info("应用程序已启动成功");
        }
        /// <summary>
        /// 多功能通信页已迁移到独立用户控件，ModelMain 只负责挂载控件和承接发送日志。
        /// </summary>
        private void InitializeMultifunctionalCommunicationTab()
        {
            _multifunctionalcommunicationUserControl = new MultifunctionalcommunicationUserControl
            {
                Dock = DockStyle.Fill
            };
            _multifunctionalcommunicationUserControl.OnUpdateRequestedMultifunctionalLog += AddLog;

            tabPage24.Controls.Clear();
            tabPage24.Controls.Add(_multifunctionalcommunicationUserControl);
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
            cbbxSTAModel.SelectedIndex = 0;//选择sta模组
            cbxSTAModePinStatus.SelectedIndex = 0;//sta模块引脚状态
            comboBoxSTAStutas.SelectedIndex = 0;//读取sta模块状态用到
            BindTerminalV1ClassOptions();
        }
        private void BindTerminalClassOptions()
        {
            List<TerminalTypeOption<ITerminalTypeDefinitions.TerminalClass>> options =
                Enum.GetValues(typeof(ITerminalTypeDefinitions.TerminalClass))
                    .Cast<ITerminalTypeDefinitions.TerminalClass>()
                    .Select(x => new TerminalTypeOption<ITerminalTypeDefinitions.TerminalClass>(x, ModelTool.GetDescription(x)))
                    .ToList();

            cbxTerminalCLASS.DisplayMember = nameof(TerminalTypeOption<ITerminalTypeDefinitions.TerminalClass>.Text);
            cbxTerminalCLASS.ValueMember = nameof(TerminalTypeOption<ITerminalTypeDefinitions.TerminalClass>.Value);
            cbxTerminalCLASS.DataSource = options;
        }
        private void BindTerminalV1ClassOptions()
        {
            List<TerminalTypeOption<ITerminalTypeDefinitions.TerminalV1Class>> options =
                Enum.GetValues(typeof(ITerminalTypeDefinitions.TerminalV1Class))
                    .Cast<ITerminalTypeDefinitions.TerminalV1Class>()
                    .Select(x => new TerminalTypeOption<ITerminalTypeDefinitions.TerminalV1Class>(x, ModelTool.GetDescription(x)))
                    .ToList();

            cbxTerminalV1.DisplayMember = nameof(TerminalTypeOption<ITerminalTypeDefinitions.TerminalV1Class>.Text);
            cbxTerminalV1.ValueMember = nameof(TerminalTypeOption<ITerminalTypeDefinitions.TerminalV1Class>.Value);
            cbxTerminalV1.DataSource = options;
        }
        private byte GetSelectedTerminalClassValue()
        {
            if (cbxTerminalCLASS.SelectedValue is ITerminalTypeDefinitions.TerminalClass terminalClass)
            {
                return (byte)terminalClass;
            }

            return 0x00;
        }

        private byte GetSelectedTerminalV1ClassValue()
        {
            if (cbxTerminalV1.SelectedValue is ITerminalTypeDefinitions.TerminalV1Class terminalClass)
            {
                return (byte)terminalClass;
            }

            return 0x00;
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
                    btn_cilentSocket.Text = "连接";
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
                //string asciiData = Encoding.ASCII.GetString(e.RawData);
                // 更新状态显示
                //if (cbxRevcASCII.Checked)
                //{
                //    AddLog($"接收消息成功[PC<--MCU]: {asciiData}", Color.Lime);
                //    LogMessage.Debug($"接受消息成功[PC<-- MCU]的数据: {asciiData}");
                //}
                //else
                //{
                AddLog($"接收消息成功[PC<--MCU] : {hexData}", Color.Lime);
                LogMessage.Debug($"接受消息成功[PC<-- MCU]的数据: {hexData}");
                ultrSimpleDisplay1.HandleReceivedMessage(hexData);
                //}
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
                    this.Text = $"习承科技测试    TCP客户端 - 已连接到 {client.ServerEndpoint}";
                }
                else if (client.Status == "Disconnected")
                {
                    this.Text = "习承科技测试    TCP客户端 - 未连接";
                }
            });
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
                                    AddLog($"发送消息失败[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(mCU)).Replace("-", " ")}", Color.White);
                                }
                            }
                            else if (buttonOpen.Text == "CLOSE")
                            {
                                var msglong = portSocket.SerialPortSendACSIIDataOrHexData(mCU, true);
                                if (msglong != 0)
                                {
                                    AddLog($"发送消息成功[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(mCU)).Replace("-", " ")}", Color.Red);
                                }
                                else
                                {
                                    AddLog($"发送消息失败[PC-->MCU] : {BitConverter.ToString(ModelTool.HexStringToByteArray(mCU)).Replace("-", " ")}", Color.White);
                                }

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
            string commandCode = ModuleModel.GetDcCommandCode(checkBox1.Checked, checkBox2.Checked);
            string terminalAddress = ModuleModel.TerminalMeterAddr(GetSelectedTerminalClassValue());
            string moduleMask = ModuleModel.GetModuleNumberMask(tbxModelNumber.Text);
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbx_addr.Text,
                commandCode,
                terminalAddress + moduleMask));
        }
        /// <summary>
        /// 直流下电按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerDown_DC_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string commandCode = ModuleModel.GetDcCommandCode(checkBox1.Checked, checkBox2.Checked);
            string terminalAddress = ModuleModel.TerminalMeterAddr(GetSelectedTerminalClassValue());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbx_addr.Text,
                commandCode,
                terminalAddress + "00"));
        }
        /// <summary>
        /// 交流上电命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerOn_AC_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string terminalAddress = ModuleModel.TerminalMeterAddr(GetSelectedTerminalClassValue());
            string phaseMask = ModuleModel.GetAcPhaseMask(
                checkBoxA.Checked,
                checkBoxB.Checked,
                checkBoxC.Checked,
                checkBoxN.Checked);
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbx_addr.Text,
                TerminalControlPcbProtocol.MeterAcCommand,
                terminalAddress + phaseMask));
        }
        /// <summary>
        /// 交流下电命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerDown_AC_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string terminalAddress = ModuleModel.TerminalMeterAddr(GetSelectedTerminalClassValue());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbx_addr.Text,
                TerminalControlPcbProtocol.MeterAcCommand,
                terminalAddress + "00"));
        }
        public void AddLog(string Message)
        {
            if (IsDisposed || !IsHandleCreated || textBoxlog.IsDisposed)
            {
                LogMessage.Debug(Message);
                return;
            }
            textBoxlog.SelectionLength = 0;
            textBoxlog.AppendText($"[{DateTime.Now:HH:mm:ss}] {Message}+{Environment.NewLine}");
            textBoxlog.ScrollToCaret();
            LogMessage.Debug(Message);
        }
        /// <summary>
        /// 带颜色的日志输出
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="color"></param>
        public void AddLog(string Message, Color? color = null)
        {
            if (IsDisposed || !IsHandleCreated || textBoxlog.IsDisposed)
            {
                LogMessage.Debug(Message);
                return;
            }
            textBoxlog.SelectionLength = 0;
            textBoxlog.SelectionColor = color.Value;
            textBoxlog.AppendText($"[{DateTime.Now:HH:mm:ss}] {Message}+{Environment.NewLine}");
            textBoxlog.SelectionColor = textBoxlog.ForeColor;
            textBoxlog.ScrollToCaret();
            LogMessage.Debug(Message);
        }
        private void AddLogThreadSafe(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AddLogThreadSafe), message);
                return;
            }

            AddLog(message);
        }
        public void MyControl_OnUpdateRequested(string message, Color? color = null)
        {
            // 确保在UI线程执行
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(MyControl_OnUpdateRequested), message);
                return;
            }
            if (color == null)
            {
                color = Color.Red;
            }
            AddLog(message, color);
        }
        /// <summary>
        /// contaol x
        /// </summary>
        /// <param name="message"></param>
        public void MyControl_OnUpdateRequested(string message)
        {
            // 确保在UI线程执行
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(MyControl_OnUpdateRequested), message);
                return;
            }
            AddLog(message);
        }

        /// <summary>
        /// 关闭连接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_cilentSocket_Close_Click(object sender, EventArgs e)
        {
            Dispose();
            btn_cilentSocket_Close.Enabled = false;
            btn_cilentSocket.Enabled = true;
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
            ultrSimpleDisplay1.HandleReceivedMessage(portstr);
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
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbx_addr.Text,
                ModuleModel.GetDcCommandCode(checkBox1.Checked, checkBox2.Checked),
                 ModuleModel.GetModuleNumberMask(tbxModelNumber.Text)));
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
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbx_addr.Text,
                ModuleModel.GetDcCommandCode(checkBox1.Checked, checkBox2.Checked),
               "00"
                ));
        }
        private async void CCOACOn_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string phaseMask = ModuleModel.GetAcPhaseMask(
                 checkBoxA.Checked,
                 checkBoxB.Checked,
                 checkBoxC.Checked,
                 checkBoxN.Checked);
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbx_addr.Text,
                TerminalControlPcbProtocol.CcoAcCommand,
                phaseMask));
        }

        private async void CCOACDown_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                 tbx_addr.Text,
                 TerminalControlPcbProtocol.CcoAcCommand,
                 "00"));
        }
        /// <summary>
        /// 终端单元切换终端类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnChangeTerminalClass_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string frame = TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text,TerminalControlPcbProtocol.TerminalTypeCommand,GetSelectedTerminalV1ClassValue().ToString("X2"));
            await SeedMethod(frame);
        }
        /// <summary>
        /// 接入电压 21
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_VOn_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string phaseMask = TerminalModel.GetThreePhaseSelectionByte(
                cbx_TerminalV1_UA.Checked,
                cbx_TerminalV1_UB.Checked,
                cbx_TerminalV1_UC.Checked);
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text,TerminalControlPcbProtocol.VoltageCommand,phaseMask));
        }
        /// <summary>
        /// 断开电压21
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_VDown_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text,TerminalControlPcbProtocol.VoltageCommand,"00"));
        }
        /// <summary>
        /// 接入电流22
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_AOn_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string phaseMask = TerminalModel.GetThreePhaseSelectionByte(
               cbx_TerminalV1_IA.Checked,
               cbx_TerminalV1_IB.Checked,
               cbx_TerminalV1_IC.Checked);
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
               tbxTerminalAdds.Text,
               TerminalControlPcbProtocol.CurrentCommand,
               phaseMask));
        }
        /// <summary>
        /// 断开电流22
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_ADown_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbxTerminalAdds.Text,
                TerminalControlPcbProtocol.CurrentCommand,
                "00"));
        }
        /// <summary>
        /// 电机压接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalV1MotorCrimping_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                 tbxTerminalAdds.Text,
                 TerminalControlPcbProtocol.MotorCrimpingCommand,
                 "01"));
        }
        /// <summary>
        /// 电机退压接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalV1MotorCrimpingreturn_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                           tbxTerminalAdds.Text,
                           TerminalControlPcbProtocol.MotorCrimpingCommand,
                           "00"));
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
            string data = REDFlas
                 ? "10"
                 : "20";
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbxTerminalAdds.Text,
                TerminalControlPcbProtocol.IndicatorLightCommand,
                data));
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
            string data = GreenFlas
                 ? "10"
                 : "40";
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(
                tbxTerminalAdds.Text,
                TerminalControlPcbProtocol.IndicatorLightCommand,
                data));
        }
        /// <summary>
        /// 清空日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 清空ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxlog.Clear();
        }
        /// <summary>
        /// 切换背景色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 切换背景色ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool useLightTheme = textBoxlog.BackColor == SystemColors.MenuText || textBoxlog.BackColor == Color.Black;
            textBoxlog.ForeColor = useLightTheme ? Color.Black : Color.Lime;
            textBoxlog.BackColor = useLightTheme ? Color.White : SystemColors.MenuText;
        }
        /// <summary>
        /// 复制日志内容
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void 复制ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string strCopy = string.IsNullOrWhiteSpace(textBoxlog.SelectedText)
                ? textBoxlog.Text
                : textBoxlog.SelectedText;
            if (!string.IsNullOrEmpty(strCopy))
            {
                Clipboard.SetText(strCopy);
            }
        }
        private void 复制全部ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxlog.Text))
            {
                Clipboard.SetText(textBoxlog.Text);
            }
        }

        private void 全选ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            textBoxlog.SelectAll();
            textBoxlog.Focus();
        }
        private void 保存日志ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBoxlog.Text))
            {
                AddLog("当前没有可保存的日志");
                return;
            }

            using SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "保存日志",
                Filter = "日志文件|*.log|文本文件|*.txt|所有文件|*.*",
                FileName = $"ModelTest_{DateTime.Now:yyyyMMdd_HHmmss}.log"
            };

            if (saveFileDialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            File.WriteAllText(saveFileDialog.FileName, textBoxlog.Text, Encoding.UTF8);
            AddLog($"日志已保存：{saveFileDialog.FileName}");
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
            string data = TaiTiRed ? "0101" : "0100";
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.TaitiIndicatorlLightCommand, data));
        }
        /// <summary>
        /// 台体运行指示绿灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pBTaiti_Green_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string data = TaiTiGreen ? "0201" : "0200";
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.TaitiIndicatorlLightCommand, data));
        }
        /// <summary>
        /// 台体运行指示黄灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pBTaiti_yellow_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            string data = TaiTiYellow ? "0301" : "0300";
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.TaitiIndicatorlLightCommand, data));
        }
        /// <summary>
        /// 真实模组上下电（直流电）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnRealModelDCUP_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage.Info(sender?.ToString() ?? nameof(btnRealModelDCUP_Click));

                var RealModel = TerminalModel.GetTerminalRealModelByte(cbbxrealModel.Text);
                string? moduleCode = cbxRealModel01.Checked
                    ? "01"
                    : cbxRealModel31.Checked
                        ? "31"
                        : null;

                if (moduleCode == null)
                {
                    AddLog("请选择真实模块类型");
                    return;
                }

                bool isPowerOn = btnRealModelDCUP.Text == "模块上电";
                string modelValue = isPowerOn ? RealModel : "00";

                await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, moduleCode, modelValue));
                btnRealModelDCUP.Text = isPowerOn ? "模块下电" : "模块上电";
            }
            catch (Exception ex)
            {
                AddLog($"真实模块上下电失败：{ex.Message}");
                LogMessage.Error(ex);
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
                if (btnT1_DCCTRL.Text == "上直流电")
                {
                    await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.StaDcCommand, TerminalModel.GetTerminalSTA1STA2Byte(cbbxSTAModel.Text)));
                    btnT1_DCCTRL.Text = "下直流电";
                }
                else if (btnT1_DCCTRL.Text == "下直流电")
                {
                    await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.StaDcCommand, "00"));
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
                if (btnT1_ACCTRL.Text == "上交流电")
                {
                    await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.StaAcCommand, TerminalModel.GetTerminalSTA1STA2Byte(cbbxSTAModel.Text)));
                    btnT1_ACCTRL.Text = "下交流电";
                }
                else if (btnT1_ACCTRL.Text == "下交流电")
                {
                    await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.StaAcCommand, "00"));
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
            var staPinStatus = cbxSTAModePinStatus.Text;
            var command = staPinStatus switch
            {

                "_1" => TerminalControlPcbProtocol.Sta1SetPinCommand, //设置单相表模块(STA1)RST、SET、EVENT引脚状态命令（0x3B）
                "_2" => TerminalControlPcbProtocol.Sta2SetPinCommand,//设置单相表模块(STA2)RST、SET、EVENT引脚状态命令（0x86）
                _ => throw new InvalidOperationException($"参数不正确: {staPinStatus}")
            };
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, command, TerminalModel.GetTerminalSTAPINByte(cbxSTAModePinStatus.Text)));
        }
        /// <summary>
        /// 设置sta模组低电平
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttnSTALPin_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            var staPinStatus = cbxSTAModePinStatus.Text;
            var command = staPinStatus switch
            {
                "_1" => TerminalControlPcbProtocol.Sta1SetPinCommand, //设置单相表模块(STA1)RST、SET、EVENT引脚状态命令（0x3B）
                "_2" => TerminalControlPcbProtocol.Sta2SetPinCommand,//设置单相表模块(STA2)RST、SET、EVENT引脚状态命令（0x86）
                _ => throw new InvalidOperationException($"参数不正确: {staPinStatus}")
            };
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, command, "00"));
        }
        /// <summary>
        /// 读取sta模组电平状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttnReadSTAPinStatus_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            var staText = comboBoxSTAStutas.Text;
            var command = staText switch
            {
                "1" => TerminalControlPcbProtocol.Sta1ReadPinCommand,
                "2" => TerminalControlPcbProtocol.Sta2ReadPinCommand,
                _ => throw new InvalidOperationException($"未知的STA状态: {staText}")
            };
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, command));
        }
        /// <summary>
        /// led1点灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_SETLED1_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SetLedAsync(button_SETLED1, 0);
        }
        /// <summary>
        /// led2点灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_SETLED2_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SetLedAsync(button_SETLED2, 1);
        }
        /// <summary>
        /// led3点灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_SETLED3_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SetLedAsync(button_SETLED3, 2);
        }
        /// <summary>
        /// led4点灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void button_SETLED4_Click(object sender, EventArgs e)
        {

            LogMessage.Info(sender.ToString());
            await SetLedAsync(button_SETLED4, 3);
        }
        private async Task SetLedAsync(Button button, int ledIndex)
        {
            int selectedColor = chexblx_LEDRGY.GetItemChecked(0) ? 0
                : chexblx_LEDRGY.GetItemChecked(1) ? 1
                : chexblx_LEDRGY.GetItemChecked(2) ? 2
                : -1;
            byte colorMask = selectedColor switch
            {
                0 => 0x01,
                1 => 0x02,
                2 => 0x04,
                _ => 0x00
            };
            byte ledMask = (byte)(0x08 << ledIndex);
            string data = (colorMask == 0 ? 0 : ledMask | colorMask).ToString("X2");

            button.BackColor = selectedColor switch
            {
                0 => Color.Red,
                1 => Color.Green,
                2 => Color.Yellow,
                _ => Color.Transparent
            };
            button.ForeColor = selectedColor is 0 or 1 ? Color.White : Color.Black;

            try
            {
                string frame = TerminalControlPcbProtocol.BuildFrame(
                    tbxTerminalAdds.Text,
                    TerminalControlPcbProtocol.LedCommand,
                    data);
                await SeedMethod(frame);
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
            var sourcestatus = TerminalModel.GetTerminalSourceType(cbx_changePCBUPAC.SelectedIndex);
            var Terminal_ChangePCBUpAC = TerminalControlPcbProtocol.BuildFrame(
                tbxTerminalAdds.Text,
                TerminalControlPcbProtocol.SwitchBoardAcCommand,
                $"{sourcestatus}01");
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
            var sourcestatus = TerminalModel.GetTerminalSourceType(cbx_changePCBUPAC.SelectedIndex);
            var Terminal_ChangePCBDownAC = TerminalControlPcbProtocol.BuildFrame(
                tbxTerminalAdds.Text,
                TerminalControlPcbProtocol.SwitchBoardAcCommand,
                $"{sourcestatus}00");
            await SeedMethod(Terminal_ChangePCBDownAC);
        }
        /// <summary>
        /// 切换电源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnChangePower_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.ChangePowerCommand,"01"));
        }
        /// <summary>
        /// 不切换电源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnNoChangePower_Click(object sender, EventArgs e)
        {
            LogMessage.Info(sender.ToString());
            await SeedMethod(TerminalControlPcbProtocol.BuildFrame(tbxTerminalAdds.Text, TerminalControlPcbProtocol.ChangePowerCommand, "00"));
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
        private async void btnelectriciansource_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 施加永磁体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnApplyingmagnet_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 释放永磁体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnReleasemagnet_Click(object sender, EventArgs e)
        {

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
        /// 终端检测
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void tsbtnTerminalTest_Click(object sender, EventArgs e)
        {
            ShowNonModalToolWindow(_terminalTestForm, () => new TerminalTest(), form => _terminalTestForm = form);
        }
        /// <summary>
        /// 电表检测
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbtnMeterTest_Click(object sender, EventArgs e)
        {
            if (_meterTestForm is not null && !_meterTestForm.IsDisposed)
            {
                MessageBox.Show(
                    this,
                    "MeterTest 已经打开，只允许同时运行一个测试用例窗口。",
                    "重复打开提醒",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                if (_meterTestForm.WindowState == FormWindowState.Minimized)
                {
                    _meterTestForm.WindowState = FormWindowState.Maximized;
                }

                _meterTestForm.Show();
                _meterTestForm.Activate();
                _meterTestForm.BringToFront();
                return;
            }
            ShowNonModalToolWindow(_meterTestForm, () => new MeterTest.MeterTest(), form => _meterTestForm = form);
        }
        /// <summary>
        /// 新窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbtnNewWindow_Click(object sender, EventArgs e)
        {
            ShowNonModalToolWindow(_databaseTestForm, () => new DatabaseTestForm(), form => _databaseTestForm = form);
        }
        /// <summary>
        /// Linux命令大全
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbtnLinuxCommand_Click(object sender, EventArgs e)
        {
            ShowNonModalToolWindow(_linuxCommandForm, () => new LinuxCommandForm(), form => _linuxCommandForm = form);
        }
        /// <summary>
        /// 报文解析工具
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tsbtnProtocolParser_Click(object sender, EventArgs e)
        {
            ShowNonModalToolWindow(_protocolParserForm, () => new ProtocolParserForm(), form => _protocolParserForm = form);
        }
       

        private void ShowNonModalToolWindow<TForm>(TForm? form, Func<TForm> factory, Action<TForm?> setForm)
                    where TForm : Form
        {
            if (form != null && !form.IsDisposed)
            {
                if (form.WindowState == FormWindowState.Minimized)
                {
                    form.WindowState = FormWindowState.Normal;
                }

                form.Activate();
                return;
            }

            form = factory();
            setForm(form);
            form.FormClosed += (_, _) => setForm(null);
            form.Show(this);
        }
    }
}
