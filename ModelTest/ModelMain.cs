using System.Net.Sockets;
using System.Net;
using System.Windows.Forms;
using Microsoft.VisualBasic.Devices;
using System.Text;
using System.Net.Http;
using System.ComponentModel;
using System.Reflection;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;

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
        public ModelMain() => InitializeComponent();
        private void ModelMain_Load(object sender, EventArgs e)
        {
            // 窗体加载时需要执行的初始化代码
            cbxTerminalCLASS.DataSource = Enum.GetValues(typeof(TerminalCLASS)).Cast<TerminalCLASS>().Select(x => new
            {
                终端类型 = x.GetDescription()
            }).ToList();
            comboBoxBaute.SelectedIndex = 6;
            comboBoxparity.SelectedIndex = 1;
            // 例如：初始化数据、配置控件等
            Control.CheckForIllegalCrossThreadCalls = false;//跨线程
            btn_cilentSocket_Close.Enabled = false;
            btn_cilentSocket.Enabled = true;
            AddLog("应用程序已启动成功");
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
            string MCUAddr = "0" + tbx_addr.Text;
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

        /// <summary>
        /// 直流下电按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void btnPowerDown_DC_Click(object sender, EventArgs e)
        {
            MCUAddr = "0" + tbx_addr.Text;
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
            MCUAddr = "0" + tbx_addr.Text;//地址
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
            MCUAddr = "0" + tbx_addr.Text;//地址
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
                AddLog($"MCU-->PC：{_messageBuffer}\r\n");
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
                byte[] data = HexStringToByteArray(hexString);
                await stream.WriteAsync(data, 0, data.Length);
                AddLog($"[PC-->MCU成功] {BitConverter.ToString(data).Replace("-", " ")}");
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

        private void buttonOpen_Click(object sender, EventArgs e)
        {

        }

        private async void buttonKZHLStatus_Click(object sender, EventArgs e)
        {
            await SeedMethod(label18.Text);
        }

        private async void buttonKZHLID_Click(object sender, EventArgs e)
        {
            await SeedMethod(label19.Text);
        }
        string cco_DataLength = "0700";
        /// <summary>
        /// CCO直流上电
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void CCODCOn_Click(object sender, EventArgs e)
        {
            //55 07 00 addr MCUCtrl 01&31  01模组1  02模组2 check AA
            MCUAddr = "0" + tbx_addr.Text;//地址
            Commande();//命令码
            ModelNumber();//得到模块地址01 02  
            var CCODCOn = cco_DataLength + MCUAddr + MCUCtrl + CommandCode + MCUData_2;//07 00 01 00 01 01
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
            MCUAddr = "0" + tbx_addr.Text;//地址
            Commande();//命令码
            var CCODCDown = cco_DataLength + MCUAddr + MCUCtrl + CommandCode + "00";//07 00 01 00 01 00
            var check = A_GetDescription.CalculateChecksum(CCODCDown);
            string CCODCDown_55AA = "55" + CCODCDown + check + "AA";
            await SeedMethod(CCODCDown_55AA);
        }

        private void CCOACOn_Click(object sender, EventArgs e)
        {

        }

        private void CCOACDown_Click(object sender, EventArgs e)
        {

        }
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

    }
}
