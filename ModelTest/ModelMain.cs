using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using System.Text;
using System.Net.Http;
using System.ComponentModel;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.IO.Ports;
using System.Runtime.InteropServices;
namespace ModelTest
{
    public partial class ModelMain : Form
    {
        
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
        string CMD_2D = "2D";
        string CMD_21 = "21";
        string CMD_22 = "22";
        string CMD_29 = "29";
        string CMD_2A = "2A";
        string CMD_2C = "2C";
        string UABC = string.Empty;
        string IABCN = string.Empty;
        public ModelMain() => InitializeComponent();
        private SerialPort MainSerialPort = new SerialPort();//初始化串口
        private void ModelMain_Load(object sender, EventArgs e)
        {
            // 窗体加载时需要执行的初始化代码
            cbxTerminalCLASS.DataSource = Enum.GetValues(typeof(TerminalCLASS)).Cast<TerminalCLASS>().Select(x => new
            {
                终端类型 = x.GetDescription()
            }).ToList();
            SerialPortinitialization();

            // 例如：初始化数据、配置控件等
            Control.CheckForIllegalCrossThreadCalls = false;//跨线程
            btn_cilentSocket_Close.Enabled = false;
            btn_cilentSocket.Enabled = true;

            cbxTerminalV1.DataSource = Enum.GetValues(typeof(TerminalV1CLASS)).Cast<TerminalV1CLASS>().Select(x => new
            {
                终端类型 = x.GetDescription()
            }).ToList();
            AddLog("应用程序已启动成功");
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
                            await SendHexAsync(mCU);
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

        private void AddLog(String Message)
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
                var Terminal_GreenLoop = A0700_DataLength + MCUAddr + MCUAddr + CMD_2A + "40";
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
                var Terminal_GreenLoop = A0700_DataLength + MCUAddr + MCUAddr + CMD_2A + "10";
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
        /// <summary>
        /// 台体运行指示灯红
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pBTaiti_Red_Click(object sender, EventArgs e)
        {
            MCUAddr = A_GetDescription.BW_Addr(tbxTerminalAdds.Text);//地址
        }

        private bool TaiTiRed = false;
        private bool TaiTiGreen = false;
        private bool TaiTiYellow = false;
        /// <summary>
        /// 台体运行指示绿灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pBTaiti_Green_Click(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// 台体运行指示黄灯
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pBTaiti_yellow_Click(object sender, EventArgs e)
        {

        }
        #region 控源XY

        private void buttonXY_x0E_Click(object sender, EventArgs e)
        {
            string x0E = "x0E";
            SerialPortSendACSIIData(x0E);
        }
        [DllImport("xyctr.dll")]
        private static extern int ReadStandValue(string StandModel, byte[] iStandValue);
        private void btn_ReadStandMeter_Click(object sender, EventArgs e)
        {
            byte[] sStandValue = new byte[255];
            try
            {
                int ReadStandMeter_data = ReadStandValue("XYD", sStandValue);
                if (ReadStandMeter_data == 0)
                {
                    AddLog("标准表数据返回成功");
                }
                else
                {
                    AddLog("标准表数据返回失败");
                }
                AddLog("标准表数据：" + System.Text.Encoding.Default.GetString(sStandValue));
            }
            catch (Exception ex)
            {
                AddLog("调用失败:"+ex.ToString());
            }
           
        }
        [DllImport("xyctr.dll")]
        public static extern int ReadTestData(int ReadType, int iPosition, byte[] sResultData);
       private void CmdReadMeterData_Click(object sender, EventArgs e)
        {
            byte[] sResultData;
            sResultData = new byte[255];
            //int iMeterPosition = Convert.ToInt16(this.CmbMeterPosition.Text);
            int iResult = ReadTestData(0, 0, sResultData);
            AddLog( System.Text.Encoding.Default.GetString(sResultData));
        }
        #endregion
    }
    public static class A_GetDescription
    {
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
