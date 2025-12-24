using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModelTest.SerialPortImp
{
    public class SerialPortSocket
    {
        private static SerialPort _serialPort { get; set; }//串口对象
        private static string _serialPortName { get; set; }//串口号
        private static Int32 _serialPortBaudRate { get; set; }//波特率
        private static Int32 _serialPortDataBit { get; set; }//数据位
        private static string _serialPortCheckBit { get; set; }//校验位
        private static string _serialPortStopBit { get; set; }//停止位
        public SerialPortSocket() { }
        public static int ByteNumber = 0;   //获取本次发送字节数
        private static long SerialPortNumber = 0; //发送数据字节记录
        private static long SerialPortCount = 0;//接收字节数记录 全局变量
        int series_x = 0;//图形显示需要
        private static byte[] temp = new byte[1];//缓存数据
        private static string pattern = @"\s";//正则
        private static string replacement = "";//替换
        private static string bufferSendData { get; set; }
        private static string SeriPortSendData;
        private static StringBuilder sb = new StringBuilder();     //为了避免在接收处理函数中反复调用，依然声明为一个全局变量
        //1.打开串口
        public bool OpenSerialPort(SerialPort serialPort, string SpName, Int32 SpBaudrate, Int32 SpDatabit, string SpCheckbit, string SpStopbit)
        {
            _serialPort = serialPort;
            _serialPortName = SpName;
            _serialPortBaudRate = SpBaudrate;
            _serialPortDataBit = SpDatabit;
            _serialPortCheckBit = SpCheckbit;
            _serialPortStopBit = SpStopbit;
            try
            {
                if (_serialPort.IsOpen)//串口处于打开状态
                {
                    _serialPort.Close();
                    LogMessage.Debug($"打开串口{_serialPort.PortName}失败");
                    return true;
                }
                else
                {
                    //串口号
                    _serialPort.PortName = _serialPortName;
                    //波特率
                    _serialPort.BaudRate = SpBaudrate;
                    //数据位
                    _serialPort.DataBits = SpDatabit;
                    //校验位
                    if (_serialPortCheckBit.Equals("NONE"))
                        serialPort.Parity = System.IO.Ports.Parity.None;
                    if (_serialPortCheckBit.Equals("ODD"))
                        serialPort.Parity = System.IO.Ports.Parity.Odd;
                    if (_serialPortCheckBit.Equals("EVEN"))
                        serialPort.Parity = System.IO.Ports.Parity.Even;
                    if (_serialPortCheckBit.Equals("MARK"))
                        serialPort.Parity = System.IO.Ports.Parity.Mark;
                    if (_serialPortCheckBit.Equals("SPACE"))
                        serialPort.Parity = System.IO.Ports.Parity.Space;
                    //停止位
                    if (_serialPortStopBit.Equals("1"))
                        serialPort.StopBits = System.IO.Ports.StopBits.One;
                    if (_serialPortStopBit.Equals("1.5"))
                        serialPort.StopBits = System.IO.Ports.StopBits.OnePointFive;
                    if (_serialPortStopBit.Equals("2"))
                        serialPort.StopBits = System.IO.Ports.StopBits.Two;
                    _serialPort.Open();
                    LogMessage.Debug($"打开串口{_serialPort.PortName}成功");
                    return false;
                }
            }
            catch (Exception ex)
            {
                SerialPortException(ex);
                return true;
            }
        }


        //2.发送消息hex&string数据
        /// <summary>
        /// 发送hex数据或者acsii数据  true发送hex数据 false发送aciss数据
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sendhexOrAciss"></param>
        /// <returns></returns>
        public long SerialPortSendACSIIDataOrHexData(string data, bool sendhexOrAciss)
        {
            bufferSendData = data;
            try
            {
                if (data.Length != 0 && _serialPort.IsOpen && sendhexOrAciss == false)
                {
                    SerialPortNumber = data.Length;
                    _serialPort.Write(data);
                    LogMessage.Debug(data);
                    return SerialPortNumber;
                }
                else if (data.Length != 0 && _serialPort.IsOpen && sendhexOrAciss == true) //发送hex数据
                {

                    Regex rgx = new Regex(pattern);//正则表达式
                    SeriPortSendData = rgx.Replace(bufferSendData, replacement);
                    ByteNumber = (SeriPortSendData.Length - SeriPortSendData.Length % 2) / 2;//获取字节数
                    for (int i = 0; i < ByteNumber; i++)
                    {
                        temp[0] = Convert.ToByte(SeriPortSendData.Substring(i * 2, 2), 16);
                        _serialPort.Write(temp, 0, 1);  //循环发送
                    }
                    //如果用户输入的字符是奇数，则单独处理
                    if (SeriPortSendData.Length % 2 != 0)
                    {
                        temp[0] = Convert.ToByte(SeriPortSendData.Substring(bufferSendData.Length - 1, 1), 16);
                        _serialPort.Write(temp, 0, 1);
                        ByteNumber++;
                    }
                    LogMessage.Debug(data);
                    SerialPortNumber += ByteNumber;
                    return SerialPortNumber;
                }
            }
            catch (Exception ex)
            {
                SerialPortException(ex);
                return SerialPortNumber;
            }
            return SerialPortNumber;
        }
        //3.接受消息
        /// <summary>
        /// 接受消息 showhexOrAciss true接受hex数据  false接受aciss数据
        /// </summary>
        /// <param name="showhexOrAciss"></param>
        /// <returns></returns>
        public string SeriPortDataRevice(bool showhexOrAciss)
        {
            try
            {
                int num = _serialPort.BytesToRead;      //获取接收缓冲区中的字节数
                byte[] received_buf = new byte[num];    //声明一个大小为num的字节数据用于存放读出的byte型数据
                SerialPortCount += num;
                _serialPort.Read(received_buf, 0, num);   //读取接收缓冲区中num个字节到byte数组中
                sb.Clear();//清空缓存
                if (showhexOrAciss == true)
                {
                    //hex show
                    foreach (byte b in received_buf)
                    {
                        sb.Append(b.ToString("X2") + ' ');//将byte型数据转化为2位16进制文本显示,用空格隔开
                    }

                    LogMessage.Debug(sb.ToString());
                    return sb.ToString();
                }
                else
                {
                    //ascii show
                    sb.Append(Encoding.ASCII.GetString(received_buf));  //将整个数组解码为ASCII数组
                    LogMessage.Debug(sb.ToString());
                    return sb.ToString();
                }
            }
            catch (Exception ex)
            {
                SerialPortException(ex);
                return ex.ToString();
            }

        }
        //4.刷新串口
        public static string[] GetPort()
        {
            return SerialPort.GetPortNames();
        }
        //5.异常处理
        public void SerialPortException(Exception ex)
        {
            //捕获到异常，创建一个新的对象，之前的不可以再用
            _serialPort = new SerialPort();
            System.Media.SystemSounds.Beep.Play();
            MessageBox.Show(ex.Message, "提示", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            LogMessage.Error(ex);
        }

    }
}
