using System.ComponentModel;
using System.Data;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Transactions;
using System.Windows.Forms;
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
        string CMD_2D = "2D";//切换终端类型
        string CMD_21 = "21";//终端电压上电
        string CMD_22 = "22";//终端电流上电
        string CMD_29 = "29";//压接
        string CMD_2A = "2A";//表位运行指示灯
        string CMD_2C = "2C";//台体运行指示灯
        string CMD_3A = "3A";//sta上直流电
        string CMD_85 = "85";//sta上交流电
        string CMD_3B = "3B";//设置单相表模块(STA1)RST、SET、EVENT引脚状态命令（0x3B）
        string CMD_3C = "3C";//读取单相表模块(STA1)STA引脚电平命令（0x3C）
        string CMD_86 = "86";//设置单相表模块(STA2)RST、SET、EVENT引脚状态命令（0x86）
        string CMD_87 = "87";//读取单相表模块(STA2)STA引脚电平命令（0x87）
        string CMD_30 = "30";//表位led灯控制
        string UABC = string.Empty;
        string IABCN = string.Empty;
        public ModelMain() => InitializeComponent();
        private SerialPort MainSerialPort = new SerialPort();//初始化串口
        private SerialPort SourceSerialPort = new SerialPort();//初始化源串口
        private void ModelMain_Load(object sender, EventArgs e)
        {
            // 窗体加载时需要执行的初始化代码
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // 禁用最大化按钮
            this.MaximizeBox = false;

            // 可选：禁用最小化按钮
            // this.MinimizeBox = false;

            // 可选：设置窗体不能最大化（额外保障）
            this.MaximumSize = this.MinimumSize = this.Size;
            cbxTerminalCLASS.DataSource = Enum.GetValues(typeof(TerminalCLASS)).Cast<TerminalCLASS>().Select(x => new
            {
                终端类型 = x.GetDescription()
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

            AddLog("应用程序已启动成功");
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
            cbxTerminalV1.DataSource = Enum.GetValues(typeof(TerminalV1CLASS)).Cast<TerminalV1CLASS>().Select(x => new
            {
                终端类型 = x.GetDescription()
            }).ToList();
        }

        /// <summary>
        /// 连接client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btn_clientSocket_Click(object sender, EventArgs e)
        {
            IPAddress ip = IPAddress.Parse(textBoxIP.Text);
            int port = int.Parse(textBoxPort.Text);
            bool success = false;
            try
            {
                if (client == null)
                {
                    client = new TcpClient();
                    ConnectionStatusChanged += status => this.BeginInvoke((Action)(() => lblconnectStatus.Text = $"状态：{status}"));
                    success = await ConnectAsync(
                      ip,
                      port);
                    //btn_cilentSocket.Enabled = false;
                    //btn_cilentSocket_Close.Enabled = true;
                }
                else
                {
                    Disconnect();
                    client = null;
                    AddLog("状态：已断开");
                    lblconnectStatus.ForeColor = Color.Red;
                    //btn_cilentSocket_Close.Enabled = false;
                    //btn_cilentSocket.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                AddLog($"Error: {ex.Message}");
            }
            if (!success)
            {
                client = null;
                lblconnectStatus.Text = "状态：连接失败";
                lblconnectStatus.ForeColor = Color.Red;
                btn_cilentSocket_Close.Enabled = false;
                btn_cilentSocket.Enabled = true;
            }
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
                            if (!cbxIsNoPortSeed.Checked)
                            {
                                await SendHexAsync(mCU);
                            }
                            else
                            {
                                SerialPortSendACSIIData(mCU);
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

        string DataLength = "0800";
        string MCUCtrl = "00";//控制协议
        string MCUData_1 = string.Empty;
        string MCUData_2 = string.Empty;
        string CommandCode = string.Empty;
        string MCUAddr = string.Empty;

        private async void btnPowerOn_DC_Click(object sender, EventArgs e)
        {
            string MCUDCOn_55AA = JXMethod();
            await SeedMethod(MCUDCOn_55AA);
        }

        private string JXMethod()
        {
            //55 起始符
            //08 00  数据长度
            //01   地址通道
            //00    协议类型
            //01    命令码
            //03 01 数据项
            //0E    校验码
            //AA
            MCUAddr = A_GetDescription.BW_Addr(tbx_addr.Text);//地址
            Commande();//命令码
            MCUData_1 = TerminalClass(MCUData_1);
            ModelNumber();
            string MCUDCOn = DataLength + MCUAddr + MCUCtrl + CommandCode + MCUData_1 + MCUData_2;
            var Check = A_GetDescription.CalculateChecksum(MCUDCOn);
            string MCUDCOn_55AA = "55" + MCUDCOn + Check + "AA";
            // AddLog(MCUDCOn_55AA);
            return MCUDCOn_55AA;
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
        /// terminal class
        /// </summary>
        /// <param name="MCUData_1"></param>
        /// <returns></returns>
        private string TerminalClass(string MCUData_1)
        {
            switch (cbxTerminalCLASS.SelectedIndex)
            {
                case 0:
                    MCUData_1 = "01";
                    break;
                case 1:
                    MCUData_1 = "02";
                    break;
                case 2:
                    MCUData_1 = "03";
                    break;
                case 3:
                    MCUData_1 = "04";
                    break;
                case 4:
                    MCUData_1 = "05";
                    break;
                case 5:
                    MCUData_1 = "06";
                    break;
                case 6:
                    MCUData_1 = "07";
                    break;
                case 7:
                    MCUData_1 = "08";
                    break;
            }

            return MCUData_1;
        }
        private string TerminalV1Class()
        {
            switch (cbxTerminalV1.SelectedIndex)
            {
                case 0:
                    MCUData_1 = "00";
                    break;
                case 1:
                    MCUData_1 = "01";
                    break;
                case 2:
                    MCUData_1 = "02";
                    break;
                case 3:
                    MCUData_1 = "03";
                    break;
                case 4:
                    MCUData_1 = "04";
                    break;
                case 5:
                    MCUData_1 = "05";
                    break;
                case 6:
                    MCUData_1 = "06";
                    break;
                case 7:
                    MCUData_1 = "07";
                    break;
                case 8:
                    MCUData_1 = "08";
                    break;
                case 9:
                    MCUData_1 = "09";
                    break;
            }

            return MCUData_1;
        }
        /// <summary>
        /// 直流下电按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerDown_DC_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbx_addr.Text);//地址
            Commande();//命令码
            MCUData_1 = TerminalClass(MCUData_1);
            MCUData_2 = "00"; //下电数据
            //CommandCode = "00";//下电命令字
            ///直流下电
            var MCUDCDown = DataLength + MCUAddr + MCUCtrl + CommandCode + MCUData_1 + MCUData_2;
            var Check = A_GetDescription.CalculateChecksum(MCUDCDown);
            var MCUDCOn_55AA = "55" + MCUDCDown + Check + "AA";
            await SeedMethod(MCUDCOn_55AA);
        }
        /// <summary>
        /// 交流上电命令
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerOn_AC_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbx_addr.Text);//地址
            CommandCode = "21";//交流电上电命令
            MCUData_1 = TerminalClass(MCUData_1);//终端类型，表地址
            AC_ABCN();
            var MCUACOn = DataLength + MCUAddr + MCUCtrl + CommandCode + MCUData_1 + MCUData_2;
            var Check = A_GetDescription.CalculateChecksum(MCUACOn);
            var MCUACOn_55AA = "55" + MCUACOn + Check + "AA";
            await SeedMethod(MCUACOn_55AA);
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
            MCUAddr = A_GetDescription.BW_Addr(tbx_addr.Text);//地址
            CommandCode = "21";//交流电上电命令
            MCUData_1 = TerminalClass(MCUData_1);//终端类型，表地址
            MCUData_2 = "00"; //下电数据
            var MCUACDown = DataLength + MCUAddr + MCUCtrl + CommandCode + MCUData_1 + MCUData_2;
            var Check = A_GetDescription.CalculateChecksum(MCUACDown);
            var MCUACDown_55AA = "55" + MCUACDown + Check + "AA";
            await SeedMethod(MCUACDown_55AA);
        }

        private void AddLog(string Message)
        {
            textBoxlog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {Message}\r\n");
            textBoxlog.ScrollToCaret();
        }

        #region tcpclient 代码
        private TcpClient? client;
        private NetworkStream stream;
        private volatile bool isConnected;
        private IPEndPoint _remoteEndPoint;
        private readonly byte[] buffer = new byte[4096];
        //private readonly ThreadSafeLogger logger = ThreadSafeLogger.Instance;
        private readonly StringBuilder _messageBuffer = new StringBuilder();
        private readonly object _lock = new object();
        private CancellationTokenSource _cts;
        //public event Action<string> MessageReceived;
        public event Action<string> ConnectionStatusChanged;
        private IPAddress serverIP;
        private int serverPort;
        public async Task<bool> ConnectAsync(IPAddress ip, int port, int timeout = 3000)
        {
            serverIP = ip;
            serverPort = port;
            try
            {
                client = new TcpClient();
                _remoteEndPoint = new IPEndPoint(ip, port);
                _cts = new CancellationTokenSource();

                var connectTask = client.ConnectAsync(ip, port);
                // client.Connect(ip, port);
                btn_cilentSocket.Enabled = false;
                btn_cilentSocket_Close.Enabled = true;
                // 设置连接超时
                if (await Task.WhenAny(connectTask, Task.Delay(timeout)) != connectTask)
                {
                    AddLog($"连接超时 ({ip}:{port})");
                    ConnectionStatusChanged?.Invoke("连接超时");
                    return false;
                }
                await connectTask; // 确保异常被捕获
                stream = client.GetStream();
                //_ = ReceiveHexAsync(); // 启动接收线程
                isConnected = true;
                ConnectionStatusChanged?.Invoke("已连接");
                AddLog($"成功连接到TCP服务器 {ip}:{port}");
                await Task.Run(ReceiveHexAsync);
                return true;
            }
            catch (SocketException ex)
            {
                HandleConnectionError(ex, ip, port);
                return false;
            }
            catch (Exception ex)
            {
                AddLog($"连接异常: {ex.GetType().Name} - {ex.Message}");
                ConnectionStatusChanged?.Invoke("连接异常");
                return false;
            }
        }
        private void HandleConnectionError(SocketException ex, IPAddress ip, int port)
        {
            string errorMsg;
            if (ex.SocketErrorCode == SocketError.ConnectionRefused)
            {
                errorMsg = "服务器拒绝连接";
            }
            else if (ex.SocketErrorCode == SocketError.TimedOut)
            {
                errorMsg = "连接超时";
            }
            else if (ex.SocketErrorCode == SocketError.HostUnreachable)
            {
                errorMsg = "主机不可达";
            }
            else if (ex.SocketErrorCode == SocketError.NetworkUnreachable)
            {
                errorMsg = "网络不可达";
            }
            else
            {
                errorMsg = $"Socket错误: {ex.SocketErrorCode}";
            }
            string fullError = $"{errorMsg} ({ip}:{port})";
            AddLog(fullError);
            ConnectionStatusChanged?.Invoke(errorMsg);
        }
        // byte[] CheckMCUData = new byte[1024];
        /// <summary>
        /// 接收16进制数据
        /// </summary>
        public async Task ReceiveHexAsync()
        {
            int bytesRead = 0;
            string message = string.Empty;
            while (!_cts.IsCancellationRequested && isConnected)
            {
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        AddLog("服务器主动断开连接");
                        break;
                    }
                    // message = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    message = ByteArrayToHex(buffer, false);
                    // AddLog($"服务器消息ACSII：{message}");
                    lock (_lock)
                    {
                        _messageBuffer.Clear();
                        _messageBuffer.Append(message.TrimEnd('0'));
                    }
                }
                catch (IOException ex)
                {
                    AddLog($"网络错误: {ex.Message}");
                    break;
                }
                catch (Exception ex)
                {
                    AddLog($"接收异常: {ex.GetType().Name} - {ex.Message}");
                    break;
                }
                AddLog($"MCU-->PC：{_messageBuffer.Replace("-", " ")}\r\n");
            }
            Disconnect();
        }
        public static string ByteArrayToHex(byte[] bytes, bool upperCase = true)
        {
            char[] hexChars = upperCase
                ? "0123456789ABCDEF".ToCharArray()
                : "0123456789abcdef".ToCharArray();

            char[] result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int value = bytes[i];
                result[2 * i] = hexChars[value >> 4];    // 高4位
                result[2 * i + 1] = hexChars[value & 0xF]; // 低4位
            }
            return new string(result);
        }
        byte[] SendMCUData = new byte[1024];
        public async Task SendHexAsync(string hexString)
        {
            if (client == null || !client.Connected)
            {
                AddLog("尝试发送数据但连接已断开");
                ConnectionStatusChanged?.Invoke("发送失败: 连接未建立");
                return;
            }
            try
            {
                SendMCUData = HexStringToByteArray(hexString);
                await stream.WriteAsync(SendMCUData, 0, SendMCUData.Length);
                AddLog($"[PC-->MCU成功] {BitConverter.ToString(SendMCUData).Replace("-", " ")}");
            }
            catch (Exception ex)
            {
                AddLog($"Hex发送失败: {ex.Message}");
                throw;
            }
        }
        private byte[] HexStringToByteArray(string hex)
        {
            // 移除所有空白字符
            hex = hex.Replace(" ", "").Replace("\t", "").Replace("\n", "");

            if (hex.Length % 2 != 0)
            {
                throw new ArgumentException("Hex字符串长度必须是偶数");
            }

            byte[] data = new byte[hex.Length / 2];
            for (int i = 0; i < data.Length; i++)
            {
                string byteValue = hex.Substring(i * 2, 2);
                data[i] = Convert.ToByte(byteValue, 16);
            }
            return data;
        }
        public void Disconnect()
        {
            if (!isConnected) return;
            isConnected = false;
            try
            {
                stream?.Close();
                client?.Close();
                AddLog("TCP连接已主动断开");
                ConnectionStatusChanged?.Invoke("已断开");
            }
            catch (Exception ex)
            {
                AddLog($"断开连接时出错: {ex.Message}");
            }
        }
        public new void Dispose() => Disconnect();

        #endregion

        private void btn_cilentSocket_Close_Click(object sender, EventArgs e)
        {
            Dispose();
            btn_cilentSocket_Close.Enabled = false;
            btn_cilentSocket.Enabled = true;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                checkBox2.Checked = false;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                checkBox1.Checked = false;
            }
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
            try
            {
                if (MainSerialPort.IsOpen)
                {
                    //串口是打开的状态
                    MainSerialPort.Close();
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
                    MainSerialPort.PortName = comboBoxCOM.Text;//串口号
                    MainSerialPort.BaudRate = Convert.ToInt32(comboBoxBaute.Text); //波特率
                    MainSerialPort.DataBits = Convert.ToInt32(textBoxdatabit.Text);//数据位
                    //校验位
                    if (comboBoxparity.Text.Equals("NONE"))
                        MainSerialPort.Parity = Parity.None;
                    if (comboBoxparity.Text.Equals("ODD"))
                        MainSerialPort.Parity = Parity.Odd;
                    if (comboBoxparity.Text.Equals("EVEN"))
                        MainSerialPort.Parity = Parity.Even;
                    if (comboBoxparity.Text.Equals("MARK"))
                        MainSerialPort.Parity = Parity.Mark;
                    if (comboBoxparity.Text.Equals("SPACE"))
                        MainSerialPort.Parity = Parity.Space;
                    //停止位
                    if (textBoxstopbit.Text.Equals("1"))
                        MainSerialPort.StopBits = StopBits.One;
                    if (textBoxstopbit.Text.Equals("1.5"))
                        MainSerialPort.StopBits = StopBits.OnePointFive;
                    if (textBoxstopbit.Text.Equals("2"))
                        MainSerialPort.StopBits = StopBits.Two;

                    MainSerialPort.Open();
                    buttonOpen.Text = "CLOSE";
                    buttonOpen.BackColor = Color.IndianRed;
                }
            }
            catch (Exception ex_prot)
            {
                //AddLog(ex_prot.ToString());
                SerialPortException(ex_prot);
            }

        }

        private void SerialPortException(object ex)
        {
            MainSerialPort = new SerialPort();
            comboBoxCOM.Items.Clear();
            comboBoxCOM.Items.AddRange(SerialPort.GetPortNames());
            //响铃并显示异常展示给客户
            System.Media.SystemSounds.Beep.Play();
            buttonOpen.Text = "OPEN";
            buttonOpen.BackColor = Color.YellowGreen;
            AddLog(ex.ToString());
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
            comboBoxCOM.Items.AddRange(SerialPort.GetPortNames());
        }
        /// <summary>
        /// 串口发送 acsii数据
        /// </summary>
        /// <param name="data"></param>
        public void SerialPortSendACSIIData(string data)
        {
            try
            {
                if (data.Length != 0 && MainSerialPort.IsOpen)
                {
                    MainSerialPort.Write(data);
                }

            }
            catch (Exception ex)
            {
                AddLog(ex.ToString());
            }
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
            try
            {
                int series_x = 0;
                int num = MainSerialPort.BytesToRead;//获取缓冲区的字节
                byte[] reviced_buf = new byte[num];
                receive_count += num;//接收字节变量
                MainSerialPort.Read(reviced_buf, 0, num);//读取缓冲期num字节存到字节数组中
                SerialSB.Clear();
                if (checkBoxISNOHEX.Checked)//hex
                {
                    foreach (var item in reviced_buf)
                    {
                        SerialSB.Append(item.ToString("X2" + " "));//将byte数组转换成16进制数据，空格隔开
                    }
                }
                else
                {
                    SerialSB.Append(Encoding.ASCII.GetString(reviced_buf));//将数组转换成ascii数组
                }
                AddLog(SerialSB.ToString());

            }
            catch (Exception ex)
            {
                System.Media.SystemSounds.Beep.Play();
                AddLog(ex.ToString());
            }
        }


        /// <summary>
        /// 
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
        string A0700_DataLength = "0700";
        /// <summary>
        /// CCO直流上电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CCODCOn_Click(object sender, EventArgs e)
        {
            //55 07 00 addr MCUCtrl 01&31  01模组1  02模组2 check AA
            MCUAddr = A_GetDescription.BW_Addr(tbx_addr.Text);//地址
            Commande();//命令码
            ModelNumber();//得到模块地址01 02  
            var CCODCOn = A0700_DataLength + MCUAddr + MCUCtrl + CommandCode + MCUData_2;//07 00 01 00 01 01
            var check = A_GetDescription.CalculateChecksum(CCODCOn);
            string CCODCOn_55AA = "55" + CCODCOn + check + "AA";
            await SeedMethod(CCODCOn_55AA);

        }
        /// <summary>
        /// CCO直流下电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CCODCDown_Click(object sender, EventArgs e)
        {
            //55 07 00 addr MCUCtrl 01&31  01模组1  02模组2 check AA
            MCUAddr = A_GetDescription.BW_Addr(tbx_addr.Text);//地址
            Commande();//命令码
            var CCODCDown = A0700_DataLength + MCUAddr + MCUCtrl + CommandCode + "00";//07 00 01 00 01 00
            var check = A_GetDescription.CalculateChecksum(CCODCDown);
            string CCODCDown_55AA = "55" + CCODCDown + check + "AA";
            await SeedMethod(CCODCDown_55AA);
        }
        string ccoCd_AC = "02";
        private async void CCOACOn_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbx_addr.Text);//地址
            AC_ABCN();
            var CCOACOn = A0700_DataLength + MCUAddr + MCUCtrl + ccoCd_AC + MCUData_2;
            var check = A_GetDescription.CalculateChecksum(CCOACOn);
            string CCOACOn_55AA = "55" + CCOACOn + check + "AA";
            await SeedMethod(CCOACOn_55AA);
        }

        private async void CCOACDown_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbx_addr.Text);//地址
            AC_ABCN();
            var CCOACDown = A0700_DataLength + MCUAddr + MCUCtrl + ccoCd_AC + "00";
            var check = A_GetDescription.CalculateChecksum(CCOACDown);
            string CCOACDown_55AA = "55" + CCOACDown + check + "AA";
            await SeedMethod(CCOACDown_55AA);
        }
        /// <summary>
        /// 终端单元切换终端类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnChangeTerminalClass_Click(object sender, EventArgs e)
        {
            //命令字2d

            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            MCUData_1 = TerminalV1Class();
            var ChangeTerminalCls = A0700_DataLength + MCUAddr + MCUCtrl + CMD_2D + MCUData_1;// 07 00 01 00 2d 00
            var check = A_GetDescription.CalculateChecksum(ChangeTerminalCls);
            string ChangeTerminalCls_55AA = "55" + ChangeTerminalCls + check + "AA";
            await SeedMethod(ChangeTerminalCls_55AA);

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
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            TerminalV1_UABC();
            var Terminal_PowerOn_V = A0700_DataLength + MCUAddr + MCUCtrl + CMD_21 + UABC;//07 00 01 21 00
            var check = A_GetDescription.CalculateChecksum(Terminal_PowerOn_V);
            string Terminal_PowerOn_V_55AA = "55" + Terminal_PowerOn_V + check + "AA";
            await SeedMethod(Terminal_PowerOn_V_55AA);
        }
        /// <summary>
        /// 断开电压21
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_VDown_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            var Terminal_PowerDown_V = A0700_DataLength + MCUAddr + MCUCtrl + CMD_21 + "00";//07 00 01 21 00
            var check = A_GetDescription.CalculateChecksum(Terminal_PowerDown_V);
            string Terminal_PowerDwon_V_55AA = "55" + Terminal_PowerDown_V + check + "AA";
            await SeedMethod(Terminal_PowerDwon_V_55AA);
        }
        /// <summary>
        /// 接入电流22
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_AOn_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            TerminalV1_IABC();
            var Terminal_PowerOn_A = A0700_DataLength + MCUAddr + MCUCtrl + CMD_22 + IABCN;
            var check = A_GetDescription.CalculateChecksum(Terminal_PowerOn_A);
            string Terminal_PowerOn_A_55AA = "55" + Terminal_PowerOn_A + check + "AA";
            await SeedMethod(Terminal_PowerOn_A_55AA);
        }
        /// <summary>
        /// 断开电流22
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalBW_ADown_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            var Terminal_PowerDown_A = A0700_DataLength + MCUAddr + MCUCtrl + CMD_22 + "00";
            var check = A_GetDescription.CalculateChecksum(Terminal_PowerDown_A);
            string Terminal_PowerDwon_A_55AA = "55" + Terminal_PowerDown_A + check + "AA";
            await SeedMethod(Terminal_PowerDwon_A_55AA);
        }
        /// <summary>
        /// 电机压接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalV1MotorCrimping_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            var Terminal_MotorCrimping = A0700_DataLength + MCUAddr + MCUCtrl + CMD_29 + "01";
            var check = A_GetDescription.CalculateChecksum(Terminal_MotorCrimping);
            string Terminal_MotorCrimping_55AA = "55" + Terminal_MotorCrimping + check + "AA";
            await SeedMethod(Terminal_MotorCrimping_55AA);
        }
        /// <summary>
        /// 电机退压接
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnTerminalV1MotorCrimpingreturn_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            var Terminal_MotorCrimping = A0700_DataLength + MCUAddr + MCUCtrl + CMD_29 + "00";
            var check = A_GetDescription.CalculateChecksum(Terminal_MotorCrimping);
            string Terminal_MotorCrimpingreturn_55AA = "55" + Terminal_MotorCrimping + check + "AA";
            await SeedMethod(Terminal_MotorCrimpingreturn_55AA);
        }
        /// <summary>
        /// 红灯控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool REDFlas = false;
        private async void pictureBoxRed_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (!REDFlas)
            {
                var Terminal_RedLoop = A0700_DataLength + MCUAddr + MCUAddr + CMD_2A + "20";
                var check = A_GetDescription.CalculateChecksum(Terminal_RedLoop);
                string Terminal_RedLoop_55AA = "55" + Terminal_RedLoop + check + "AA";
                await SeedMethod(Terminal_RedLoop_55AA);
                if (Terminal_RedLoop_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "红灯.png");
                    REDFlas = true;
                }
            }
            else
            {
                var Terminal_RedLoop = A0700_DataLength + MCUAddr + MCUAddr + CMD_2A + "10";
                var check = A_GetDescription.CalculateChecksum(Terminal_RedLoop);
                string Terminal_RedLoop_55AA = "55" + Terminal_RedLoop + check + "AA";
                await SeedMethod(Terminal_RedLoop_55AA);
                if (Terminal_RedLoop_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                    REDFlas = false;
                }
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
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (!GreenFlas)
            {
                var Terminal_GreenLoop = A0700_DataLength + MCUAddr + MCUCtrl + CMD_2A + "40";
                var check = A_GetDescription.CalculateChecksum(Terminal_GreenLoop);
                string Terminal_GreenLoop_55AA = "55" + Terminal_GreenLoop + check + "AA";
                await SeedMethod(Terminal_GreenLoop_55AA);
                if (Terminal_GreenLoop_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "绿灯.png");
                    GreenFlas = true;
                }

            }
            else
            {
                var Terminal_GreenLoop = A0700_DataLength + MCUAddr + MCUCtrl + CMD_2A + "10";
                var check = A_GetDescription.CalculateChecksum(Terminal_GreenLoop);
                string Terminal_GreenLoop_55AA = "55" + Terminal_GreenLoop + check + "AA";
                await SeedMethod(Terminal_GreenLoop_55AA);
                if (Terminal_GreenLoop_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                    GreenFlas = false;
                }

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
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (TaiTiRed)
            {
                var check = A_GetDescription.CalculateChecksum(DataLength + MCUAddr + MCUCtrl + CMD_2C + "01" + "01");
                string Terminal_TaiTiRed_55AA = "55" + DataLength + MCUAddr + MCUCtrl + CMD_2C + "01" + "01" + check + "AA";
                await SeedMethod(Terminal_TaiTiRed_55AA);
                if (Terminal_TaiTiRed_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "红灯.png");
                    GreenFlas = true;
                }
            }
            else
            {
                var check = A_GetDescription.CalculateChecksum(DataLength + MCUAddr + MCUCtrl + CMD_2C + "01" + "00");
                string Terminal_TaiTiRed_55AA = "55" + DataLength + MCUAddr + MCUCtrl + CMD_2C + "01" + "00" + check + "AA";
                await SeedMethod(Terminal_TaiTiRed_55AA);
                if (Terminal_TaiTiRed_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                    GreenFlas = true;
                }
            }
        }
        /// <summary>
        /// 台体运行指示绿灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pBTaiti_Green_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (TaiTiRed)
            {
                var check = A_GetDescription.CalculateChecksum(DataLength + MCUAddr + MCUCtrl + CMD_2C + "02" + "01");
                string Terminal_TaiTiRed_55AA = "55" + DataLength + MCUAddr + MCUCtrl + CMD_2C + "01" + "01" + check + "AA";
                await SeedMethod(Terminal_TaiTiRed_55AA);
                if (Terminal_TaiTiRed_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "绿灯.png");
                    GreenFlas = true;
                }
            }
            else
            {
                var check = A_GetDescription.CalculateChecksum(DataLength + MCUAddr + MCUCtrl + CMD_2C + "02" + "00");
                string Terminal_TaiTiRed_55AA = "55" + DataLength + MCUAddr + MCUCtrl + CMD_2C + "01" + "00" + check + "AA";
                await SeedMethod(Terminal_TaiTiRed_55AA);
                if (Terminal_TaiTiRed_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                    GreenFlas = true;
                }
            }
        }
        /// <summary>
        /// 台体运行指示黄灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void pBTaiti_yellow_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (TaiTiRed)
            {
                var check = A_GetDescription.CalculateChecksum(DataLength + MCUAddr + MCUCtrl + CMD_2C + "03" + "01");
                string Terminal_TaiTiRed_55AA = "55" + DataLength + MCUAddr + MCUCtrl + CMD_2C + "03" + "01" + check + "AA";
                await SeedMethod(Terminal_TaiTiRed_55AA);
                if (Terminal_TaiTiRed_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "红灯.png");
                    GreenFlas = true;
                }
            }
            else
            {
                var check = A_GetDescription.CalculateChecksum(DataLength + MCUAddr + MCUCtrl + CMD_2C + "03" + "00");
                string Terminal_TaiTiRed_55AA = "55" + DataLength + MCUAddr + MCUCtrl + CMD_2C + "03" + "00" + check + "AA";
                await SeedMethod(Terminal_TaiTiRed_55AA);
                if (Terminal_TaiTiRed_55AA.Contains(BitConverter.ToString(buffer)))
                {
                    this.pictureBoxRed.Image = Image.FromFile(Application.StartupPath + "\\png\\" + "灰灯.png");
                    GreenFlas = true;
                }
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
                string sta = A_GetDescription.GetSta1_2(cbbxSTAModel.Text);
                MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
                if (btnT1_DCCTRL.Text == "上直流电")
                {
                    var check = A_GetDescription.CalculateChecksum(A0700_DataLength + MCUAddr + MCUCtrl + CMD_3A + sta);
                    var Terminal_STADCUP_55AA = "55" + A0700_DataLength + MCUAddr + MCUCtrl + CMD_3A + sta + check + "AA";
                    //AddLog(Terminal_STADCUP_55AA);
                    await SeedMethod(Terminal_STADCUP_55AA);
                    btnT1_DCCTRL.Text = "下直流电";
                }
                else if (btnT1_DCCTRL.Text == "下直流电")
                {
                    var check = A_GetDescription.CalculateChecksum(A0700_DataLength + MCUAddr + MCUCtrl + CMD_3A + "00");
                    var Terminal_STADCDown_55AA = "55" + A0700_DataLength + MCUAddr + MCUCtrl + CMD_3A + "00" + check + "AA";
                    //AddLog(Terminal_STADCUP_55AA);
                    await SeedMethod(Terminal_STADCDown_55AA);
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
                string sta = A_GetDescription.GetSta1_2(cbbxSTAModel.Text);
                MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
                if (btnT1_ACCTRL.Text == "上交流电")
                {
                    var check = A_GetDescription.CalculateChecksum(A0700_DataLength + MCUAddr + MCUCtrl + CMD_85 + sta);
                    var Terminal_STAACUP_55AA = "55" + A0700_DataLength + MCUAddr + MCUCtrl + CMD_85 + sta + check + "AA";
                    //AddLog(Terminal_STAACUP_55AA);
                    await SeedMethod(Terminal_STAACUP_55AA);
                    btnT1_ACCTRL.Text = "下交流电";
                }
                else if (btnT1_ACCTRL.Text == "下交流电")
                {
                    var check = A_GetDescription.CalculateChecksum(A0700_DataLength + MCUAddr + MCUCtrl + CMD_85 + "00");
                    var Terminal_STAACDown_55AA = "55" + A0700_DataLength + MCUAddr + MCUCtrl + CMD_85 + "00" + check + "AA";
                    //AddLog(Terminal_STAACDown_55AA);
                    await SeedMethod(Terminal_STAACDown_55AA);
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
            string STAPINSTATUS = A_GetDescription.GetSTAPin(cbxSTAModePinStatus.Text);//获取设置)RST、SET、EVENT
            string STAPINSET = string.Empty;
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (cbxSTAModePinStatus.Text.Contains("_1"))
            {
                STAPINSET = A0700_DataLength + MCUAddr + MCUCtrl + CMD_3B + STAPINSTATUS;//sta1设置高
            }
            else if (cbxSTAModePinStatus.Text.Contains("_2"))
            {
                STAPINSET = A0700_DataLength + MCUAddr + MCUCtrl + CMD_86 + STAPINSTATUS;//sta1设置高
            }
            var check = A_GetDescription.CalculateChecksum(STAPINSET);
            var STAPINSET_55AA = "55" + STAPINSET + check + "AA";
            await SeedMethod(STAPINSET_55AA);
        }
        /// <summary>
        /// 设置sta模组低电平
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttnSTALPin_Click(object sender, EventArgs e)
        {
            var STAPINSET = string.Empty;
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (cbxSTAModePinStatus.Text.Contains("_1"))
            {
                STAPINSET = A0700_DataLength + MCUAddr + MCUCtrl + CMD_3B + "00";//sta1设置高
            }
            else if (cbxSTAModePinStatus.Text.Contains("_2"))
            {
                STAPINSET = A0700_DataLength + MCUAddr + MCUCtrl + CMD_86 + "00";//sta1设置高
            }
            var check = A_GetDescription.CalculateChecksum(STAPINSET);
            var STAPINSET_55AA = "55" + STAPINSET + check + "AA";
            await SeedMethod(STAPINSET_55AA);
        }
        /// <summary>
        /// 读取sta模组电平状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttnReadSTAPinStatus_Click(object sender, EventArgs e)
        {
            var STAPINREAD = string.Empty;
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (comboBoxSTAStutas.Text.Contains("1"))
            {
                STAPINREAD = "0600" + MCUAddr + MCUCtrl + CMD_3C;//读取sta1状态
            }
            if (comboBoxSTAStutas.Text.Contains("2"))
            {
                STAPINREAD = "0600" + MCUAddr + MCUCtrl + CMD_87;//读取sta2状态
            }
            var check = A_GetDescription.CalculateChecksum(STAPINREAD);
            var STAPINREAD_55AA = "55" + STAPINREAD + check + "AA";
            await SeedMethod(STAPINREAD_55AA);

            if (BitConverter.ToString(buffer) != string.Empty)
            {
                AddLog("开始解析读取状态……");
                //不知道怎么数据是什么样子，暂时不解析
            }

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
            var LED_OneCtrl = string.Empty;
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (chexblx_LEDRGY.GetItemChecked(0))
            {
                LED_OneCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "09";
                button_SETLED1.BackColor = Color.Red;
                button_SETLED1.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(1))
            {
                LED_OneCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "0A";
                button_SETLED1.BackColor = Color.Green;
                button_SETLED1.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(2))
            {
                LED_OneCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "0C";
                button_SETLED1.BackColor = Color.Yellow;
                button_SETLED1.ForeColor = Color.Black;
            }
            else
            {
                LED_OneCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "00";
                button_SETLED1.BackColor = Color.Transparent;
                button_SETLED1.ForeColor = Color.Black;
            }
            try
            {
                var check = A_GetDescription.CalculateChecksum(LED_OneCtrl);
                var LED_OneCtrl_55AA = "55" + LED_OneCtrl + check + "AA";
                AddLog(LED_OneCtrl_55AA);
                await SeedMethod(LED_OneCtrl_55AA);
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
            var LED_TwoCtrl = string.Empty;
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (chexblx_LEDRGY.GetItemChecked(0))
            {
                LED_TwoCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "11";
                button_SETLED2.BackColor = Color.Red;
                button_SETLED2.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(1))
            {
                LED_TwoCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "12";
                button_SETLED2.BackColor = Color.Green;
                button_SETLED2.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(2))
            {
                LED_TwoCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "14";
                button_SETLED2.BackColor = Color.Yellow;
                button_SETLED2.ForeColor = Color.Black;
            }
            else
            {
                LED_TwoCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "00";
                button_SETLED2.BackColor = Color.Transparent;
                button_SETLED2.ForeColor = Color.Black;
            }
            try
            {
                var check = A_GetDescription.CalculateChecksum(LED_TwoCtrl);
                var LED_TwoCtrl_55AA = "55" + LED_TwoCtrl + check + "AA";
                AddLog(LED_TwoCtrl_55AA);
                await SeedMethod(LED_TwoCtrl_55AA);
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
            var LED_ThreeCtrl = string.Empty;
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (chexblx_LEDRGY.GetItemChecked(0))
            {
                LED_ThreeCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "21";
                button_SETLED3.BackColor = Color.Red;
                button_SETLED3.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(1))
            {
                LED_ThreeCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "22";
                button_SETLED3.BackColor = Color.Green;
                button_SETLED3.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(2))
            {
                LED_ThreeCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "24";
                button_SETLED3.BackColor = Color.Yellow;
                button_SETLED3.ForeColor = Color.Black;
            }
            else
            {
                LED_ThreeCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "00";
                button_SETLED3.BackColor = Color.Transparent;
                button_SETLED3.ForeColor = Color.Black;
            }
            try
            {
                var check = A_GetDescription.CalculateChecksum(LED_ThreeCtrl);
                var LED_ThreeCtrl_55AA = "55" + LED_ThreeCtrl + check + "AA";
                AddLog(LED_ThreeCtrl_55AA);
                await SeedMethod(LED_ThreeCtrl_55AA);
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
            var LED_FourCtrl = string.Empty;
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
            if (chexblx_LEDRGY.GetItemChecked(0))
            {
                LED_FourCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "41";
                button_SETLED4.BackColor = Color.Red;
                button_SETLED4.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(1))
            {
                LED_FourCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "42";
                button_SETLED4.BackColor = Color.Green;
                button_SETLED4.ForeColor = Color.White;
            }
            else if (chexblx_LEDRGY.GetItemChecked(2))
            {
                LED_FourCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "44";
                button_SETLED4.BackColor = Color.Yellow;
                button_SETLED4.ForeColor = Color.Black;
            }
            else
            {
                LED_FourCtrl = A0700_DataLength + MCUAddr + MCUCtrl + CMD_30 + "00";
                button_SETLED4.BackColor = Color.Transparent;
                button_SETLED4.ForeColor = Color.Black;
            }
            try
            {
                var check = A_GetDescription.CalculateChecksum(LED_FourCtrl);
                var LED_FourCtrl_55AA = "55" + LED_FourCtrl + check + "AA";
                AddLog(LED_FourCtrl_55AA);
                await SeedMethod(LED_FourCtrl_55AA);
            }
            catch (Exception ex)
            {
                AddLog(ex.ToString());
            }
        }
        #region 控源XY
        string XYModel = "model1";
        /// <summary>
        /// 降源接口
        /// 0-----电压、电流同时停止输出1-----电压停止输出 2-----电流停止输出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int ShutPowerSource([In] int ShutPowerSource);
        public void CallShutPowerSource(int shutPowerSource)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int PowerSource = ShutPowerSource(shutPowerSource);
                    if (PowerSource == 1)
                    {
                        AddLog("降源正常" + PowerSource);
                    }
                    else
                    {
                        AddLog("调用降源接口异常" + PowerSource);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用降源接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        private void buttonXY_x0E_Click(object sender, EventArgs e)
        {
            //string x0E = "x0E";
            //SerialPortSendACSIIData(x0E);
            if (cbxShutdownUI0.Checked)
            {
                int ShutDownUI = 0;
                AddLog("输出给源电压电流参数：" + ShutDownUI);
                CallShutPowerSource(ShutDownUI);
            }
            //AddLog("输出给源电压电流参数：" + ShutDownUI);
            //CallShutPowerSource(ShutDownUI);
        }
        [DllImport("xyctr.dll")]
        private static extern int OpenComm(int Port);
        [DllImport("xyctr.dll")]
        private static extern int CloseComm();
        /// <summary>
        /// 初始化源的串口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        int OpenComm_data = 0;
        private void btn_SourcePort_Click(object sender, EventArgs e)
        {
            try
            {
                if (OpenComm_data == 1)
                {
                    //串口是打开的状态
                    //SourceSerialPort.Close();
                    CloseComm();
                    btn_SourcePort.Text = "OPEN";
                    btn_SourcePort.BackColor = Color.YellowGreen;
                    AddLog("源串口已关闭");
                    OpenComm_data = 0;
                }
                else
                {
                    //串口已经关闭状态，需要设置好属性后打开
                    AddLog("源串口已打开");
                    btn_SourcePort.Text = "CLOSE";
                    btn_SourcePort.BackColor = Color.IndianRed;
                    //初始化源串口
                    OpenComm_data = OpenComm(int.Parse(tbx_sourcePort.Text));
                    if (OpenComm_data == 1)
                    {
                        AddLog("源串口打开成功");
                    }
                    else
                    {
                        AddLog("源串口打开失败，错误代码：" + OpenComm_data);
                    }
                }
            }
            catch (Exception ex_prot)
            {
                //AddLog(ex_prot.ToString());
                SerialPortException(ex_prot);
            }
        }
        int ReadStandMeter_data;
        static byte[] sStandValue = new byte[1024];
        [DllImport("xyctr.dll")]
        private static extern int ReadStandValue([In, Out] string StandModel, [Out] byte[] sStandValue);
        private void btn_ReadStandMeter_Click(object sender, EventArgs e)
        {
            object lockObject = new object();
            lock (lockObject)
            {
                Thread thread = new Thread(() =>
                {
                    try
                    {
                        ReadStandMeter_data = ReadStandValue(XYModel, sStandValue);
                        if (ReadStandMeter_data == 1)
                        {
                            AddLog("标准表数据返回成功");
                            AddLog("新跃源标准表数据：" + System.Text.Encoding.Default.GetString(sStandValue));
                            ShowTextReadStandValue(System.Text.Encoding.Default.GetString(sStandValue));
                        }
                        else
                        {

                            AddLog("标准表数据返回失败，错误代码：" + ReadStandMeter_data);
                        }
                    }
                    catch (Exception ex)
                    {
                        AddLog("调用标准表接口异常" + ex.ToString());
                    }
                });
                thread.IsBackground = true;
                thread.Start();
            }
        }
        /// <summary>
        /// 显示标准表的数据
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
      //  string ModelXYStandValue;
        private void ShowTextReadStandValue(string iStandValue)
        {
            // 分割字符串并移除空条目
            string[] parts = iStandValue.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            // 处理特殊值：将 "None" 转换为 null，数值保持原样
            var processedParts = parts.Select(p => p == "None" ? "None" : p).ToArray();
            // 打印结果（带索引）
            AddLog($"分割结果（共 {processedParts.Length} 项）:");
            for (int i = 0; i < processedParts.Length; i++)
            {
                AddLog($"[{i}] {processedParts[i] ?? "null"}");
            }
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

        [DllImport("xyctr.dll")]
        public static extern int ReadTestData([In, Out] int ReadType, int iPosition, [Out] byte[] sResultData);
        private void CmdReadMeterData_Click(object sender, EventArgs e)
        {
            byte[] sResultData;
            sResultData = new byte[255];
            //int iMeterPosition = Convert.ToInt16(this.CmbMeterPosition.Text);
            int iResult = ReadTestData(0, 0, sResultData);
            if (iResult == 1)
            {
                AddLog("读取装置信息成功：" + System.Text.Encoding.Default.GetString(sResultData));
            }
            else
            {
                AddLog("读取装置信息失败，错误代码：" + iResult);
            }

        }
        /// <summary>
        /// AnyUIOutput  电压电流输出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int AnyUIOutput(string StrUICommand, int iPulse);
        public void CallAnyUIOutput(string StrUICommand, int iPulse)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int callAnyUI = AnyUIOutput(StrUICommand, iPulse);
                    if (callAnyUI == 1)
                    {
                        AddLog("控源正常" + callAnyUI);
                    }
                    else
                    {
                        AddLog("调用控源接口异常" + callAnyUI);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用控源接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        private void buttonCtrlUI_Click(object sender, EventArgs e)
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
            CallAnyUIOutput(ui, iPulse);
        }
        /// <summary>
        /// 读取常数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int ReadStandConst([Out] byte[] constanst);
        private void btn_ReadContans_Click(object sender, EventArgs e)
        {
            // string RC00E = "RC00E";//读取常数
            //SerialPortSendACSIIData(RC00E);
            Thread thread = new Thread(() =>
            {
                try
                {
                    byte[] constas = new byte[1024];
                    int readConstans = ReadStandConst(constas);
                    if (readConstans == 1)
                    {
                        AddLog("读取常数接口正常" + readConstans);
                        tb_contans.Text = System.Text.Encoding.Default.GetString(constas);
                    }
                    else
                    {
                        AddLog("调用读取常数接口异常" + readConstans);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用读取常数接口异常" + ex.ToString());
                }
            });

        }
        /// <summary>
        /// 初始化电表参数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int SendCommand(string Cmd, bool AdjTags);
        public void CallSendCommand(string cmd, bool AdjTags)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int callcmd = SendCommand(cmd, AdjTags);
                    if (callcmd == 1)
                    {
                        AddLog("SendCommand接口正常" + callcmd);
                    }
                    else
                    {
                        AddLog("调用SendCommand接口异常" + callcmd);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用SendCommand接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        string MeterConnection;
        string MeterV;
        string LC;
        private void btn_Init_Click(object sender, EventArgs e)
        {
            string MeterInit = string.Empty;
            Init_meterConnection();
            Init_meterV();
            MeterInit = $"Ini_{MeterConnection}_{MeterV}_{cbx_ratedcurrent.Text}_{cbx_meterconstant.Text}_E";
            AddLog("初始化电表参数" + MeterInit);
            CallSendCommand(MeterInit, true);
        }
        /// <summary>
        /// adj 升源接口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void btn_XY_ADJ_Click(object sender, EventArgs e)
        {
            string AdjCMD = string.Empty;
            ADJLC_CHANGE();
            AdjCMD = $"Adj_{tbx_V_5.Text}_{tbx_A_5.Text}_{cbx_HABC.Text}_{LC}_{tbxiPulse.Text}_E";
            AddLog("ADJ升源接口指令" + AdjCMD);
            CallSendCommand(AdjCMD, true);
        }
        int BluetoothMode = 0; //接线模式 0-常规接线 1-蓝牙 2-双光电头
        int BluetoothChannel = 3; //通道号 3-有功 4-无功 
        /// <summary>
        /// 设置蓝牙模式和通道
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int Set_BlueTooth_Channel(int IchanngelNo);
        public void CallSet_BlueTooth_Channel(int IchanngelNo)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int callSet_BlueTooth_Channel = Set_BlueTooth_Channel(IchanngelNo);
                    if (callSet_BlueTooth_Channel == 1)
                    {
                        AddLog("设置Set_BlueTooth_Channel接口正常" + callSet_BlueTooth_Channel);
                    }
                    else
                    {
                        AddLog("调用设置Set_BlueTooth_Channel接口异常" + callSet_BlueTooth_Channel);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用设置Set_BlueTooth_Channel接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        bool BoolTooth = true;//模式选择
        private void bttn_settooth_Click(object sender, EventArgs e)
        {
            //先设置模式
            //常规接线模式
            //蓝牙模块接线模式
            //双光电头接线模式
            if (BoolTooth)
            {
                if (cbbx_BlueTooth.Text.Equals("常规接线模式"))
                {
                    BluetoothMode = 0;
                }
                else if (cbbx_BlueTooth.Text.Equals("蓝牙模块接线模式"))
                {
                    BluetoothMode = 1;
                }
                else if (cbbx_BlueTooth.Text.Equals("双光电头接线模式"))
                {
                    BluetoothMode = 2;
                }
                AddLog("设置模式：" + cbbx_BlueTooth.Text);
                CallSet_BlueTooth_Channel(BluetoothMode);
                bttn_settooth.Text = "设置通道"; //切换到设置通道
                BoolTooth = false; //切换到设置通道 
            }
            else
            {
                //设置通道
                if (cbbx_ToosNum.Text.Equals("有功通道"))
                {
                    BluetoothChannel = 3;
                }
                else if (cbbx_ToosNum.Text.Equals("无功通道"))
                {
                    BluetoothChannel = 4;
                }
                AddLog("设置通道：" + cbbx_ToosNum.Text);
                CallSet_BlueTooth_Channel(BluetoothChannel);
                bttn_settooth.Text = "设置模式"; //切换到设置模式
                BoolTooth = true; //切换到设置模式
            }
        }
        /// <summary>
        /// 测试时钟误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int Clock_Start(int iPulse);
        [DllImport("xyctr.dll")]
        private static extern int Read_Test([In, Out] int iMeterNo, [Out] byte[] MeterError);
        public void Call_Clock_Start(int iPulse)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int clockStart_stutas = Clock_Start(iPulse);
                    if (clockStart_stutas == 1)
                    {
                        AddLog("调用设置Clock_Start接口正常" + clockStart_stutas);
                    }
                    else
                    {
                        AddLog("调用设置Clock_Start接口异常" + clockStart_stutas);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用设置Clock_Start接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        public void Call_Read_TestError(int iMeterNo, byte[] MeterError)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int readTestError_Status = Read_Test(iMeterNo, MeterError);
                    if (readTestError_Status == 1)
                    {
                        // int.TryParse(System.Text.Encoding.Default.GetString(MeterError), out double resut);

                        AddLog($"读取{iMeterNo}表位误差数据成功：" + MeterError);
                    }
                    else
                    {
                        AddLog($"读取{iMeterNo}误差数据失败，错误代码：" + readTestError_Status);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用读取误差数据接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        /// <summary>
        /// 时钟误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void bttn_ClockStart_Click(object sender, EventArgs e)
        {
            bttn_ClockStart.Enabled = false;
            int iPulse = int.Parse(tbxclockpulse.Text);//时钟误差数
            AddLog("时钟误差数：" + iPulse);
            Call_Clock_Start(iPulse);
            AddLog($"开始测试时钟误差,延迟等待{iPulse + iPulse}秒，等待结束自动读取误差数据。");
            await Task.Delay(iPulse * 1000 + iPulse * 1000);//延迟等待
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
                    ReadTestData(1, i, MeterError);
                    AddLog($"正在读取{i}表位误差数据...");
                    AddLog($"误差数据" + MeterError + "+\r\n");
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
            //string[] meterNo = tbx_MeterNo.Text.Split('-');//分割字符串
            for (int i = 1; i <= meter; i++)
            {
                AddLog($"正在读取{i}表位误差数据...");
                Call_Read_TestError(i, MeterError);
            }
        }
        [DllImport("xyctr.dll")]
        private static extern int Error_Start(string MeterConstant, int iMeterCount, int iPulse);
        /// <summary>
        /// 误差试验
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Call_Error_Start(string meterConstant, int iMeterCount, int iPulse)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int errorStart_Status = Error_Start(meterConstant, iMeterCount, iPulse);
                    if (errorStart_Status == 1)
                    {
                        AddLog("调用设置Error_Start(误差试验)接口正常" + errorStart_Status);
                    }
                    else
                    {
                        AddLog("调用设置Error_Start(误差试验)接口异常" + errorStart_Status);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用设置Error_Start(误差试验)接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        private async void bttn_ErrorStart_Click(object sender, EventArgs e)
        {
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
            Call_Error_Start(CMeterConstant.ToString(), MeterCount, Pulse);
            //添加延迟等待
            await Task.Delay(int.Parse(tbx_TaskDelay.Text) * 1000);
            //读取实验误差
            ReadTestError_1(MeterCount);
        }
        [DllImport("xyctr.dll")]
        private static extern int Stop_Test();
        [DllImport("xyctr.dll")]
        private static extern int Error_Start();
        public void Call_Stop_Test()
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int stopTest_Status = Stop_Test();
                    if (stopTest_Status == 1)
                    {
                        AddLog("调用设置Stop_Test(停止误差)接口正常" + stopTest_Status);
                    }
                    else
                    {
                        AddLog("调用设置Stop_Test(停止误差)接口异常" + stopTest_Status);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用设置Stop_Test(停止误差)接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        public void Call_Error_Start()
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int errorStart_Status = Error_Start();
                    if (errorStart_Status == 1)
                    {
                        AddLog("调用设置Error_Start(清除误差)接口正常" + errorStart_Status);
                    }
                    else
                    {
                        AddLog("调用设置Error_Start(清除误差)接口异常" + errorStart_Status);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用设置Error_Start(清除误差)接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// 停止误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bttn_StopError_Click(object sender, EventArgs e)
        {
            Call_Stop_Test();
        }

        /// <summary>
        /// 清除误差
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void bttn_ClearError_Click(object sender, EventArgs e)
        {
            Call_Error_Start();
        }

        [DllImport("xyctr.dll")]
        private static extern int Read_Pulse([In, Out] int iMeterNo, [Out] byte[] MeterError);
        public void Call_Read_Pulse(int iMeterNo, byte[] MeterError)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    int readPulse_status = Read_Pulse(iMeterNo, MeterError);
                    if (readPulse_status == 1)
                    {
                        AddLog("调用设置Read_Pulse(读取脉冲数)接口正常" + readPulse_status);
                        AddLog($"读取表位{iMeterNo}脉冲数数为：{MeterError}");
                    }
                    else
                    {
                        AddLog("调用设置Read_Pulse(读取脉冲数)接口异常" + readPulse_status);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("调用设置Read_Pulset(读取脉冲数)接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }        /// <summary>
                 /// 读取表位的脉冲数
                 /// </summary>
                 /// <param name="sender"></param>
                 /// <param name="e"></param>
        private void buttonRead_Pulset_Click(object sender, EventArgs e)
        {
            byte[] MeterError = new byte[1024];
            Call_Read_Pulse(int.Parse(tbxXYMeterPulse.Text), MeterError);
        }
        [DllImport("xyctr.dll")]
        private static extern int FunctionReadVersion([Out] byte[] StrVer);
        /// <summary>
        /// 读取新跃dll版本日期
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_ReadTime_Click(object sender, EventArgs e)
        {
            byte[] StrVer = new byte[1024];
            Thread thread = new Thread(() =>
            {
                try
                {
                    int readPulse_status = FunctionReadVersion(StrVer);
                    if (readPulse_status == 1)
                    {
                        AddLog("读取dll版本日期接口正常" + readPulse_status);
                        label112.Text = "Dll版本日期：" + StrVer.ToString();
                    }
                    else
                    {
                        AddLog("读取dll版本日期接口异常" + readPulse_status);
                    }
                }
                catch (Exception ex)
                {
                    AddLog("读取dll版本日期接口异常" + ex.ToString());
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
        private void ADJLC_CHANGE()
        {
            //            0.25L
            //0.5L
            //0.8L
            //1.0
            //0.8C
            //0.5C
            //0.25C
            //0C
            //0.25L - 反向
            //0.5L - 反向
            //0.8L - 反向
            //1.0 - 反向
            //0.8C - 反向
            //0.5C - 反向
            //0.25C - 反向
            //0L - 反向
            if (cbx_LC.Text.Equals("0.25L"))
                LC = "0";
            if (cbx_LC.Text.Equals("0.5L"))
                LC = "1";
            if (cbx_LC.Text.Equals("0.8L"))
                LC = "2";
            if (cbx_LC.Text.Equals("1.0"))
                LC = "3";
            if (cbx_LC.Text.Equals("0.8C"))
                LC = "4";
            if (cbx_LC.Text.Equals("0.5C"))
                LC = "5";
            if (cbx_LC.Text.Equals("0.25C"))
                LC = "6";
            if (cbx_LC.Text.Equals("0C"))
                LC = "7";
            if (cbx_LC.Text.Equals("0.25L-反向"))
                LC = "8";
            if (cbx_LC.Text.Equals("0.5L-反向"))
                LC = "9";
            if (cbx_LC.Text.Equals("0.8L-反向"))
                LC = "A";
            if (cbx_LC.Text.Equals("1.0-反向"))
                LC = "B";
            if (cbx_LC.Text.Equals("0.8C-反向"))
                LC = "C";
            if (cbx_LC.Text.Equals("0.5C-反向"))
                LC = "D";
            if (cbx_LC.Text.Equals("0.25C-反向"))
                LC = "E";
            if (cbx_LC.Text.Equals("0L-反向"))
                LC = "F";
        }

        private void Init_meterV()
        {
            /* 额定电压57.7
            //100
            //220
            //380
            //110
            //120
            //if (cbx_ratedvoltage.Text.Equals("57.7"))
            //    MeterV = "0";
            //if (cbx_ratedvoltage.Text.Equals("100"))
            //    MeterV = "1";
            //if (cbx_ratedvoltage.Text.Equals("220"))
            //    MeterV = "2";
            //if (cbx_ratedvoltage.Text.Equals("380"))
            //    MeterV = "3";
            //if (cbx_ratedvoltage.Text.Equals("110"))
            //    MeterV = "4";
            //if (cbx_ratedvoltage.Text.Equals("120"))
            //    MeterV = "5";**/
            switch (cbx_ratedvoltage.Text)
            {
                case "57.7":
                    MeterV = "0";
                    break;
                case "100":
                    MeterV = "1";
                    break;
                case "220":
                    MeterV = "2";
                    break;
                case "380":
                    MeterV = "3";
                    break;
                case "110":
                    MeterV = "4";
                    break;
                case "120":
                    MeterV = "5";
                    break;
            }
        }

        private void Init_meterConnection()
        {
            /*电表常数
            //单相有功
            //三相四线有功
            //三相三线有功
            //90°无功
            //60°无功
            //四线正弦无功
            //三线正弦无功
            //三相四线视在
            //三相三线视在
            //二相三线有功(AC相)
            //单相无功
            //单相三线(AC相)
            //单相三线(BC相)
            //单相三线(AB相)
            //二相三线有功(BC相)
            //二相三线有功(AB相)
            //二相三线无功（AB相）
            //二相三线无功（AC相）
            //二相三线无功（BC相）
            if (cbx_Connection.Text.Equals("单相有功"))
                MeterConnection = "0";
            if (cbx_Connection.Text.Equals("三相四线有功"))
                MeterConnection = "1";
            if (cbx_Connection.Text.Equals("三相三线有功"))
                MeterConnection = "2";
            if (cbx_Connection.Text.Equals("90°无功"))
                MeterConnection = "3";
            if (cbx_Connection.Text.Equals("60°无功"))
                MeterConnection = "4";
            if (cbx_Connection.Text.Equals("四线正弦无功"))
                MeterConnection = "5";
            if (cbx_Connection.Text.Equals("三线正弦无功"))
                MeterConnection = "6";
            if (cbx_Connection.Text.Equals("三相四线视在"))
                MeterConnection = "7";
            if (cbx_Connection.Text.Equals("三相三线视在"))
                MeterConnection = "8";
            if (cbx_Connection.Text.Equals("二相三线有功(AC相)"))
                MeterConnection = "9";
            if (cbx_Connection.Text.Equals("单相无功"))
                MeterConnection = "10";
            if (cbx_Connection.Text.Equals("单相三线(AC相)"))
                MeterConnection = "11";
            if (cbx_Connection.Text.Equals("单相三线(BC相)"))
                MeterConnection = "12";
            if (cbx_Connection.Text.Equals("单相三线(AB相)"))
                MeterConnection = "13";
            if (cbx_Connection.Text.Equals("二相三线有功(BC相)"))
                MeterConnection = "14";
            if (cbx_Connection.Text.Equals("二相三线有功(AB相)"))
                MeterConnection = "15";
            if (cbx_Connection.Text.Equals("二相三线无功（AB相）"))
                MeterConnection = "16";
            if (cbx_Connection.Text.Equals("二相三线无功（AC相）"))
                MeterConnection = "17";
            if (cbx_Connection.Text.Equals("二相三线无功（BC相）"))
                MeterConnection = "18";**/
            switch (cbx_Connection.Text)
            {
                case "单相有功":
                    MeterConnection = "0";
                    break;
                case "三相四线有功":
                    MeterConnection = "1";
                    break;
                case "三相三线有功":
                    MeterConnection = "2";
                    break;
                case "90°无功":
                    MeterConnection = "3";
                    break;
                case "60°无功":
                    MeterConnection = "4";
                    break;
                case "四线正弦无功":
                    MeterConnection = "5";
                    break;
                case "三线正弦无功":
                    MeterConnection = "6";
                    break;
                case "三相四线视在":
                    MeterConnection = "7";
                    break;
                case "三相三线视在":
                    MeterConnection = "8";
                    break;
                case "二相三线有功(AC相)":
                    MeterConnection = "9";
                    break;
                case "单相无功":
                    MeterConnection = "10";
                    break;
                case "单相三线(AC相)":
                    MeterConnection = "11";
                    break;
                case "单相三线(BC相)":
                    MeterConnection = "12";
                    break;
                case "单相三线(AB相)":
                    MeterConnection = "13";
                    break;
                case "二相三线有功(BC相)":
                    MeterConnection = "14";
                    break;
                case "二相三线有功(AB相)":
                    MeterConnection = "15";
                    break;
                case "二相三线无功（AB相）":
                    MeterConnection = "16";
                    break;
                case "二相三线无功（AC相）":
                    MeterConnection = "17";
                    break;
                case "二相三线无功（BC相）":
                    MeterConnection = "18";
                    break;
            }
        }
        /// <summary>
        /// setui设置电压和电流量程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int SetUIRange(int iUI, int iValue);
        private void button7_Click(object sender, EventArgs e)
        {
            //得到数据，分割数据，
            string ServerData = textBoxSetUIRange.Text;

            string[] ServerDataNew = StringDataSplit(ServerData);
            int Iui = int.Parse(ServerDataNew[0]);
            int Ivalue = int.Parse(ServerDataNew[1]);
            Thread thread = new Thread(() =>
            {

                try
                {
                    int SetUIResult = SetUIRange(Iui, Ivalue);
                    if (SetUIResult > 0)
                    {
                        AddLog("SetUIRange设置电压和电流量程接口正常" + SetUIResult);
                    }
                    else if (true)
                    {
                        AddLog("SetUIRange设置电压和电流量程接口异常" + SetUIResult);
                    }
                }
                catch (Exception ex)
                {
                    AddLog(ex.Message);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        private static string[] StringDataSplit(string ServerData)
        {
            // 分割字符串并移除空条目
            string[] parts = ServerData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            // 处理特殊值：将 "None" 转换为 null，数值保持原样
            var ServerDataNew = parts.Select(p => p == "None" ? "None" : p).ToArray();
            return ServerDataNew;
        }

        /// <summary>
        /// rangui
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [DllImport("xyctr.dll")]
        private static extern int RangeOutputUI(string StrUICommand);
        private void RangeOutputUI_Click(object sender, EventArgs e)
        {
            string RangeOutputUIData = textBoxRangeOutputUI.Text;
            string[] ServerDataNew = StringDataSplit(RangeOutputUIData);
            string ua = ServerDataNew[0];
            string ub = ServerDataNew[1];
            string uc = ServerDataNew[2];
            string ia = ServerDataNew[3];
            string ib = ServerDataNew[4];
            string ic = ServerDataNew[5];
            string StrUICommand = $"{ua}_{ub}_{uc}_{ia}_{ib}_{ic}";
            Thread thread = new Thread(() =>
            {

                try
                {
                    int SetUIResult = RangeOutputUI(StrUICommand);
                    if (SetUIResult == 1)
                    {
                        AddLog("RangeOutputUI设置电压和电流量程接口正常" + SetUIResult);
                    }
                    else
                    {
                        AddLog("RangeOutputUI设置电压和电流量程接口异常" + SetUIResult);
                    }
                }
                catch (Exception ex)
                {
                    AddLog(ex.Message);
                }
            });
            thread.IsBackground = true;
            thread.Start();
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
                bshBack = new SolidBrush(Color.FromArgb(255, 255, 0)); //选中的标签颜色变为红色
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

#if false
        private void chexblx_LEDRGY_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (chexblx_LEDRGY.SelectedIndex == 0)
            {
                chexblx_LEDRGY.SetItemChecked(1, false);
                chexblx_LEDRGY.SetItemChecked(2, false);
            }
            else if (chexblx_LEDRGY.SelectedIndex == 1)
            {
                chexblx_LEDRGY.SetItemChecked(0, false);
                chexblx_LEDRGY.SetItemChecked(2, false);
            }
            else if (chexblx_LEDRGY.SelectedIndex == 2)
            {
                chexblx_LEDRGY.SetItemChecked(0, false);
                chexblx_LEDRGY.SetItemChecked(1, false);
            }

        } 
#endif
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
                    int ret = winSocketServer.CreateRandEx(byte.Parse("8"), OutRandNum);
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
                            AddLog($"传入参数iOperateMode{ServerDataNew[0]}，cTESAMID{ServerDataNew[1]}，" +
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
                            if (result == 0)
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------成功");
                                richTextBox1.AppendText($"加密机返回数据：cOutSID={System.Text.Encoding.Default.GetString(cOutSID)}，" +
                                    $"cOutAttachData={System.Text.Encoding.Default.GetString(cOutAttachData)}," +
                                    $"cOutData={System.Text.Encoding.Default.GetString(cOutData)}," +
                                    $"cOutMAC={System.Text.Encoding.Default.GetString(cOutMAC)}");
                            }
                            else
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------失败,返回值：{result}");
                            }
                        });
                        break;
                    case "CloseDevice":
                        thread = new Thread(() =>
                        {
                            AddLog($"调用接口：{ServerImp.Text}");
                            result = winSocketServer.CloseDeviceEx();
                            if (result == 0)
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------成功");
                            }
                            else
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------失败,返回值：{result}");
                            }
                        });
                        break;
                    case "ClseUsbkey":
                        thread = new Thread(() =>
                        {
                            AddLog($"调用接口：{ServerImp.Text}");
                            result = winSocketServer.ClseUsbkeyEx();
                            if (result == 0)
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------成功");
                            }
                            else
                            {
                                AddLog($"调用接口：{ServerImp.Text}----------失败,返回值：{result}");
                            }
                        });
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
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                AddLog(ex.Message);
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
                    AddLog($"选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("无参数");
                    break;
                case "ClseUsbkey":
                    AddLog($"选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("无参数");
                    break;
                case "Obj_Meter_Formal_SetESAMData":
                    AddLog($"选择加密机函数 {ServerImpType}，调用接口参数：");
                    AddLog("int InKeyState,int InOperateMode,char * cESAMNO, char * cSessionKey, char * cMeterNo, char * cESAMRand, char * cData, char * OutSID,char * OutAddData, char * OutData,char * OutMAC");
                    break;
                default:
                    break;
            }
        }
        private void CheckItemSetUpFrom()
        {
            List<string> ComBoxServerImp = new List<string>()
            {
                "RESAM_Formal_GetKeyData_AppLayer",
                "BT_WESAM_Formal_GetTrmKeyData",
                "CT_Terminal_Formal_CalCTESAMMac",
                "CT_Terminal_Formal_CalCTTESAMMac",
                "CT_Terminal_Formal_CalVerifyCTESAMMac",
                "CT_Terminal_Formal_CalVerifyCTTESAMMac",
                "ClearKeyInfo",
                "CloseDevice",
                "ClseUsbkey",
                "ConnectDevice",
                "CreateRand",
                "Create_Rand",
                "DisEncrptUserInfor",
                "IdentityAuthentication",
                "InCreasePurse",
                "KeyUpdate",
                "LgServer",
                "LgoutServer",
                "Log_WriteLogFile_FlieName",
                "Maccheck",
                "Meter_Formal_DataClear1",
                "Meter_Formal_DataClear2",
                "Meter_Formal_EncMacWrite",
                "Meter_Formal_GetAuthKey",
                "Meter_Formal_IdentityAuthentication",
                "Meter_Formal_IdentityAuthentication_",
"Meter_Formal_InCreasePurse",
"Meter_Formal_InfraredAuth",
"Meter_Formal_InfraredAuth_TermnalToMeter",
"Meter_Formal_InfraredRand",
"Meter_Formal_InintPurse",
"Meter_Formal_KeyUpdateV2",
"Meter_Formal_KeyUpdate_20201118",
"Meter_Formal_MacCheck",
"Meter_Formal_MacWrite",
"Meter_Formal_ParameterElseUpdate",
"Meter_Formal_ParameterUpdate",
"Meter_Formal_ParameterUpdate1",
"Meter_Formal_ParameterUpdate2",
"Meter_Formal_UserControl",
"NMeter_Formal_DataClear1",
"NMeter_Formal_DataClear2",
"NMeter_Formal_GetTrmKeyData",
"NMeter_Formal_ParameterElseUpdate",
"NMeter_Formal_ParameterUpdate",
"NMeter_PowerOff",
"NMeter_PowerOn",
"NMeter_VerifyPowerOff",
"Obj_CT_ESAM_Formal_GetSessionData",
"Obj_CT_ESAM_Formal_GetTrmKeyData",
"Obj_CT_ESAM_Formal_InitSession",
"Obj_CT_ESAM_Formal_VerifyCTData",
"Obj_CT_ESAM_Formal_VerifySession",
"Obj_CT_TESAM_Formal_GetTrmKeyData",
"Obj_ESAM_GN_Formal_GetMacKey",
"Obj_ESAM_GN_Formal_GetPubKey",
"Obj_ESAM_GN_Formal_GetSm4Key",
"Obj_Formal_GetRandHost",
"Obj_InterFace_Formal_BusinessData",
"Obj_InterFace_Formal_GetTrmKeyData",
"Obj_InterFace_Formal_InitSession",
"Obj_InterFace_Formal_ParameterElseUpdate",
"Obj_InterFace_Formal_VerifyBusinessData",
"Obj_InterFace_Formal_VerifyMeterData",
"Obj_InterFace_Formal_VerifySession",
"Obj_InterFace_GetSessionData",
"Obj_JL_Formal_InitSession",
"Obj_JL_Formal_VerifySession",
"Obj_Meter_Formal_EncForCompare",
"Obj_Meter_Formal_GenReadData",
"Obj_Meter_Formal_GetESAMData",
"Obj_Meter_Formal_GetESAMFileData",
"Obj_Meter_Formal_GetGrpBrdCstData",
"Obj_Meter_Formal_GetGrpBrdCstDataNew",
"Obj_Meter_Formal_GetMeterSetData",
"Obj_Meter_Formal_GetPurseData",
"Obj_Meter_Formal_GetResponseData",
"Obj_Meter_Formal_GetSessionData",
"Obj_Meter_Formal_GetTrmKeyData",
"Obj_Meter_Formal_GetTrmKeyData_ForCheck",
"Obj_Meter_Formal_InitSession",
"Obj_Meter_Formal_InitTrmKeyData",
"Obj_Meter_Formal_SetESAMData",
"Obj_Meter_Formal_SetESAMDataNew",
"Obj_Meter_Formal_VerifyESAMData",
"Obj_Meter_Formal_VerifyMeterData",
"Obj_Meter_Formal_VerifyReadData",
"Obj_Meter_Formal_VerifyReportData",
"Obj_Meter_Formal_VerifySession",
"Obj_Meter_Formal_VerifySessionForECard",
"Obj_Meter_JL_VerifyReadData",
"Obj_Meter_JL_VerifyReportData",
"Obj_Meter_Test_GetTrmKeyData",
"Obj_Meter_Test_VerifyESAMData",
"Obj_NMeter_Formal_GetESAMData",
"Obj_NMeter_Formal_SetESAMData",
"Obj_Normal_Formal_InitSession",
"Obj_Normal_Formal_VerifySession",
"Obj_Send_Formal_Data",
"Obj_Send_Formal_DataForGetKey",
"Obj_Terminal_Formal_ChangeDataAuthorize",
"Obj_Terminal_Formal_ExternalAuth",
"Obj_Terminal_Formal_GetCACertificateData",
"Obj_Terminal_Formal_GetGrpBrdCstData",
"Obj_Terminal_Formal_GetMeterSessionKey",
"Obj_Terminal_Formal_GetResponseData",
"Obj_Terminal_Formal_GetSessionData",
"Obj_Terminal_Formal_GetSessionDataForMeter",
"Obj_Terminal_Formal_GetTerminalSetData",
"Obj_Terminal_Formal_GetTerminlMeterKey",
"Obj_Terminal_Formal_GetTrmKeyData",
"Obj_Terminal_Formal_InitSession",
"Obj_Terminal_Formal_InitTrmKeyData",
"Obj_Terminal_Formal_VerifyReadData",
"Obj_Terminal_Formal_VerifyReportData",
"Obj_Terminal_Formal_VerifySession",
"Obj_Terminal_Formal_VerifyTerminalData",
"OpenDevice",
"OpenUsbkey",
"ParameterElseUpdate",
"ParameterUpdate",
"ParameterUpdate1",
"ParameterUpdate2",
"Pcsc_CloseDevice",
"Pcsc_GetDeviceList",
"Pcsc_OpenDevice",
"Pcsc_OpenDeviceSgchip",
"RDID_Formal_RFIDChangeKey",
"RDID_Formal_RFIDCheckData",
"RDID_Formal_RFIDDataMAC",
"RDID_Formal_RFIDDisEncrptData",
"RDID_Formal_RFIDEncrptData",
"RDID_Formal_RFIDGetPin",
"RDID_Formal_SealRFIDChangeKey",
"Seal_ChangekeyF",
"Seal_ChangekeySgchip",
"Seal_ReadData",
"Seal_ReadDataSgchip",
"Seal_WriteCodeDataF",
"Seal_WriteCodeDataSgchip",
"Seal_WriteDataSgchip",
"Set_MeterType",
"Terminal_Formal_CACertificateUpdate",
"Terminal_Formal_CertificateStateChange",
"Terminal_Formal_ChangeDataAuthorize",
"Terminal_Formal_EncTaskData",
"Terminal_Formal_ExternalAuth",
"Terminal_Formal_GetCipherMeterKey",
"Terminal_Formal_GetR1",
"Terminal_Formal_GroupBroadcast",
"Terminal_Formal_InternalAuth",
"Terminal_Formal_MACVerify",
"Terminal_Formal_SessionConsultVerify",
"Terminal_Formal_SessionConsultVerify_",
"Terminal_Formal_SessionInitRec",
"Terminal_Formal_SessionKeyConsult",
"Terminal_Formal_SessionKeyConsult_",
"Terminal_Formal_SessionRecoveryVerify",
"Terminal_Formal_SessionRecoveryVerify_",
"Terminal_Formal_SetOfflineCounter",
"Terminal_Formal_SymmetricKeyUpdate",
"Terminal_Formal_SymmetricKeyUpdateCT",
"Terminal_Formal_SymmetricKeyUpdateNT",
"Terminal_Formal_SystemBroadcast",
"Terminal_Formal_WriteTEsam",
"UserControl",
"WESAM_Formal_EncrypteData",
"WESAM_Formal_GetSessionData",
"WESAM_Formal_GetTrmKeyDataForMeteringBox",
"WESAM_Formal_InitSession",
"WESAM_Formal_SetESAMData",
"WESAM_Formal_VerifyData",
"WESAM_Formal_VerifyReadData",
"WESAM_Formal_VerifyReportData",
"WESAM_Formal_VerifySession",
"Write_SealRDID",
"Write_SealRFIDForCheckData",
"Write_SealRFIDForSceneData",
"YESAM_Formal_ChangeSealKey",
"YESAM_Formal_GetSealKey",
"YESAM_Formal_GetSessionData",
"YESAM_Formal_GetTrmKeyData",
"YESAM_Formal_GetWESAMEncrptKey",
"YESAM_Formal_GetWESAMSessionKeyForMeteringBox",
"YESAM_Formal_InitSessionOffline",
"YESAM_Formal_VerifyData",
"YESAM_Formal_VerifySessionOffline",
"testapi",
            }
            ;
            ServerImp.SelectedIndexChanged -= ServerImp_SelectedIndexChanged;
            ServerImp.DataSource = ComBoxServerImp;
            ServerImp.SelectedIndex = -1;
            ServerImp.SelectedIndexChanged += ServerImp_SelectedIndexChanged;
        }


    }
    public static class A_GetDescription
    {
        /// <summary>
        /// STA1-STA2 03 STA1 01 STA2 02
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetSta1_2(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                switch (s)
                {
                    case "STA1-STA2":
                        return "03";
                    case "STA1":
                        return "01";
                    case "STA2":
                        return "02";
                }
            }
            return "03";
        }
        /// <summary>
        /// RST_1
        //        SET_1
        //        EVENT_1
        //          RST_2
        //          SET_2
        //          EVENT_2
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string GetSTAPin(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                switch (s)
                {
                    case "RST_1":
                        return "01";
                    case "SET_1":
                        return "02";
                    case "EVENT_1":
                        return "04";
                    case "RST_2":
                        return "01";
                    case "SET_2":
                        return "02";
                    case "EVENT_2":
                        return "04";
                }
            }
            return default;
        }
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? value.ToString();
        }
        /// <summary>
        /// 计算16进制字符串的累加和
        /// </summary>
        /// <param name="hexString">16进制字符串（长度需为偶数）</param>
        /// <param name="isComplement">是否计算补码校验和（默认为false）</param>
        /// <returns>两位16进制校验和字符串</returns>
        public static string CalculateChecksum(string hexString, bool isComplement = false)
        {
            // 验证输入有效性
            if (string.IsNullOrEmpty(hexString))
                throw new ArgumentException("输入字符串不能为空");

            if (hexString.Length % 2 != 0)
                throw new ArgumentException("输入字符串长度必须为偶数");

            // 移除可能存在的空格
            hexString = hexString.Replace(" ", "");

            // 转换为字节数组
            byte[] bytes = new byte[hexString.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                string hexByte = hexString.Substring(i * 2, 2);
                bytes[i] = Convert.ToByte(hexByte, 16);
            }

            // 计算累加和
            int sum = bytes.Sum(b => (int)b);

            // 处理校验和
            byte checksum;
            if (isComplement)
            {
                // 计算补码校验和：取反加一
                checksum = (byte)(~(byte)sum + 1);
            }
            else
            {
                // 标准累加和：取低8位
                checksum = (byte)sum;
            }

            // 返回两位16进制字符串
            return checksum.ToString("X2");
        }

        public static string BW_Addr(string BWTest)
        {
            if (!string.IsNullOrEmpty(BWTest))
            {
                switch (BWTest)
                {
                    case "AA":
                        return "AA";
                    case "255":
                        return "FF";
                    case "1":
                        return "01";
                    case "2":
                        return "02";
                    case "3":
                        return "03";
                    case "4":
                        return "04";
                    case "5":
                        return "05";
                    case "6":
                        return "06";
                    case "7":
                        return "07";
                    case "8":
                        return "08";
                    case "9":
                        return "09";
                    case "10":
                        return "0A";
                    case "11":
                        return "0B";
                    case "12":
                        return "0C";
                    case "13":
                        return "0D";
                    case "14":
                        return "0E";
                    case "15":
                        return "0F";
                    case "16":
                        return "10";
                    case "17":
                        return "11";
                    case "18":
                        return "12";
                    case "19":
                        return "13";
                    case "20":
                        return "14";
                    case "21":
                        return "15";
                    case "22":
                        return "16";
                    case "23":
                        return "17";
                    case "24":
                        return "18";
                    case "25":
                        return "19";
                    case "26":
                        return "1A";
                    case "27":
                        return "1B";
                    case "28":
                        return "1C";
                    case "29":
                        return "1D";
                    case "30":
                        return "1E";
                    case "31":
                        return "1F";
                    case "32":
                        return "20";
                    case "33":
                        return "21";
                    case "34":
                        return "22";
                    case "35":
                        return "23";
                    case "36":
                        return "27";
                    case "37":
                        return "25";
                    case "38":
                        return "26";
                    case "39":
                        return "27";
                    case "40":
                        return "28";
                    case "41":
                        return "29";
                    case "42":
                        return "2A";
                    case "43":
                        return "2B";
                    case "44":
                        return "2C";
                    case "45":
                        return "2D";
                    case "46":
                        return "2E";
                    case "47":
                        return "2F";
                    case "48":
                        return "30";
                    default:
                        break;
                }
            }
            return default;
        }

    }
}
