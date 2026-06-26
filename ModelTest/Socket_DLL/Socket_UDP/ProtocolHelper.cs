using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ModelTest.Tools;

namespace ModelTest.Socket_DLL.Socket_UDP
{
    public static class ProtocolHelper
    {
        private static readonly string ConfigPath = Path.Combine(Application.StartupPath, "XCKJcomfig.ini");
        private static readonly string PcbConfigPath = Path.Combine(Application.StartupPath, "PcbCommunicationConfig.ini");
        private const string TaiTiNoSection = "TaiTiNo";
        private const string SgccTaiTiNoKey = "SGCCTaitiNo";
        private const string DefaultSgccTaiTiNo = "xxxxxxxxxxxxxx";
        private const string Pcb0401Section = "0401PostSend";
        private const string Pcb0401AfterResponseDelayKey = "AfterResponseDelayMs";
        private const int PcbCommunicationMethod = 6;
        private const int PcbBroadcastCommunicationMethod = 7;
        private const int DefaultPcbPowerOnAfterResponseDelayMs = 100;
        private const int DefaultPcbIndex = 1;
        private static Func<string?, int, int, string, Task<(bool Success, string CallResult)>>? _messageSender;
        private static readonly Channel<List<string>> PcbPowerOnQueue = Channel.CreateUnbounded<List<string>>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private static int _pcbPowerOnQueueStarted;
        private static readonly XYCtr SourceController = new XYCtr();
        public static void RegisterMessageSender(Func<string?, int, int, string, Task<(bool Success, string CallResult)>> messageSender)
        {
            _messageSender = messageSender;
        }

        public static bool OpenSourcePort(int sourcePort, Action<string>? logAction = null)
        {
            try
            {
                if (XYCtr.IsSourcePortOpen)
                {
                    logAction?.Invoke($"源串口已打开，无需重复打开，端口号: {sourcePort}");
                    return true;
                }

                if (!IsSourcePortConfigured(sourcePort, logAction))
                {
                    return false;
                }

                var result = SourceController.CallOpenComm(sourcePort);
                if (result.Success)
                {
                    logAction?.Invoke($"源串口打开成功，端口号: {sourcePort}");
                    return true;
                }

                logAction?.Invoke($"源串口打开失败，端口号: {sourcePort}，错误代码: {result.Result}");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage.Error("打开源串口异常", ex);
                logAction?.Invoke($"打开源串口异常: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> OpenSourcePortAsync(int sourcePort, Action<string>? logAction = null)
        {
            try
            {
                if (XYCtr.IsSourcePortOpen)
                {
                    logAction?.Invoke($"源串口已打开，无需重复打开，端口号: {sourcePort}");
                    return true;
                }

                if (!IsSourcePortConfigured(sourcePort, logAction))
                {
                    return false;
                }

                var result = await SourceController.CallOpenCommAsync(sourcePort).ConfigureAwait(false);
                if (result.Success)
                {
                    logAction?.Invoke($"源串口打开成功，端口号: {sourcePort}");
                    return true;
                }

                string failMessage = result.Result switch
                {
                    XYCtr.TimeoutResult => $"源串口打开超时，端口号: {sourcePort}",
                    XYCtr.BusyResult => $"源串口正在处理中，拒绝重复打开，端口号: {sourcePort}",
                    _ => $"源串口打开失败，端口号: {sourcePort}，错误代码: {result.Result}"
                };
                logAction?.Invoke(failMessage);
                return false;
            }
            catch (Exception ex)
            {
                LogMessage.Error("打开源串口异常", ex);
                logAction?.Invoke($"打开源串口异常: {ex.Message}");
                return false;
            }
        }

        public static bool OpenSourcePort(Action<string>? logAction = null)
        {
            string source = Confighelper.ReadIni("TaiTiSource", "Source", "", 255, ConfigPath).Trim();
            string sourcePortText = Confighelper.ReadIni("TaiTiSource", "SourcePort", "", 255, ConfigPath).Trim();

            if (!string.Equals(source, "XY", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(source))
                {
                    logAction?.Invoke($"当前源类型暂不支持自动开串口，Source={source}");
                }

                return false;
            }

            if (!int.TryParse(sourcePortText, out int sourcePort))
            {
                logAction?.Invoke($"源串口配置无效，SourcePort={sourcePortText}");
                return false;
            }

            return OpenSourcePort(sourcePort, logAction);
        }

        public static async Task<bool> OpenSourcePortAsync(Action<string>? logAction = null)
        {
            string source = Confighelper.ReadIni("TaiTiSource", "Source", "", 255, ConfigPath).Trim();
            string sourcePortText = Confighelper.ReadIni("TaiTiSource", "SourcePort", "", 255, ConfigPath).Trim();

            if (!string.Equals(source, "XY", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(source))
                {
                    logAction?.Invoke($"当前源类型暂不支持自动开串口，Source={source}");
                }

                return false;
            }

            if (!int.TryParse(sourcePortText, out int sourcePort))
            {
                logAction?.Invoke($"源串口配置无效，SourcePort={sourcePortText}");
                return false;
            }

            return await OpenSourcePortAsync(sourcePort, logAction).ConfigureAwait(false);
        }

        private static bool IsSourcePortConfigured(int sourcePort, Action<string>? logAction = null)
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames();
                string expectedPortName = $"COM{sourcePort}";
                bool exists = portNames.Any(port => string.Equals(port, expectedPortName, StringComparison.OrdinalIgnoreCase));
                if (exists)
                {
                    return true;
                }

                string message = $"源串口不存在，配置端口号: {sourcePort}，期望串口: {expectedPortName}";
                LogMessage.Info(message);
                logAction?.Invoke(message);
                return false;
            }
            catch (Exception ex)
            {
                LogMessage.Error("校验源串口是否存在异常", ex);
                logAction?.Invoke($"校验源串口是否存在异常: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// 处理接收到的命令
        /// </summary>
        /// <param name="receiveData">接收到的数据字符串，如 "cmd=0201,data=0;63;220;1.5;0;50;0"</param>
        /// <returns>返回结果字符串，如 "cmd=0201,ret=0,data=null" 或 "cmd=0201,ret=1,data=null"</returns>
        public static async Task<string> ProcessCommandAsync(string clientId, string receiveData)
        {
            try
            {
                // 1. 解析接收到的数据
                var (cmd, dataList) = ParseReceiveData(receiveData);

                // 2. 根据命令字分发处理
                switch (cmd)
                {
                    case "0101":
                        return Process0101(dataList);
                    case "0102":
                        return Process0102(dataList);
                    case "0104":
                        return Process0104(dataList);
                    case "0201":
                        return await Process0201Async(dataList).ConfigureAwait(false);
                    case "0202":
                        return Process0202(dataList);
                    case "0510":
                        return Process0510(dataList);
                    case "0301":
                        return await Process0301Async(dataList).ConfigureAwait(false);
                    case "0401":
                        return Process0401(dataList);
                    case "1001":
                        await Process1001Async(clientId, dataList);
                        return string.Empty;
                    case "1002":
                        await Process1002Async(clientId, dataList);
                        return string.Empty;
                    case "1003":
                    default:
                        return EncodeReturnData(cmd, 1, new List<string> { $"未知命令字: {cmd}" });
                }
            }
            catch (Exception ex)
            {
                // 解析失败，返回失败
                LogMessage.Error("命令解析失败", ex);
                return EncodeReturnData(TryExtractCommand(receiveData), 1, null);
            }
        }

        private static async Task Process1001Async(string clientId, List<string> dataList)
        {
            //表位号; 通讯方式;符合DL/T 698规约要求的16进制字节串的数据帧【说明：表位号：1～16，对应台体的各表位】
            //【说明：通讯方式：0～3，0：232_1方式；1：485_1方式；2：网口方式；3：红外方式；4：232_2方式；5：485_2方式；】
            //cmd=1001,data = 1; 0; 6839003900684A0021010000057C0000100400000116
            int MeterPosition = int.Parse(dataList[0]);
            int CommunicationMethod = int.Parse(dataList[1]);
            string DataFrame = dataList[2];
            try
            {
                if (_messageSender == null)
                {
                    LogMessage.Info("1001发送失败: 发送器未注册");
                    return;
                }

                if (CommunicationMethod is not (0 or 1 or 5))
                {
                    LogMessage.Info($"1001发送失败: 不支持的通信方式 {CommunicationMethod}");
                    return;
                }

                // 1001 业务根据通信方式和表位号路由:
                // 通信方式=0 -> 232_No_{表位号}
                // 通信方式=1 -> 485_1No_{表位号}
                // 通信方式=5 -> 485_2No_{表位号}
                var result = await _messageSender(clientId, MeterPosition, CommunicationMethod, DataFrame);
                if (result.Success)
                {
                    LogMessage.Info("1001发送成功");
                    return;
                }

                LogMessage.Info("1001发送失败");
                return;

            }
            catch (Exception ex)
            {
                LogMessage.Error($"1001发送异常", ex);
            }
        }

        private static async Task Process1002Async(string clientId, List<string> dataList)
        {
            // data=表位号;符合DL/T 645/698规约要求的16进制字节串的数据帧
            int MeterPosition = int.Parse(dataList[0]);
            string DataFrame = dataList[1];
            try
            {
                if (_messageSender == null)
                {
                    LogMessage.Info("1002发送失败: 发送器未注册");
                    return;
                }

                var result = await _messageSender(clientId, MeterPosition, 1, DataFrame);
                if (result.Success)
                {
                    LogMessage.Info("1002发送成功");
                    return;
                }

                LogMessage.Info("1002发送失败");
            }
            catch (Exception ex)
            {
                LogMessage.Error($"1002发送异常", ex);
            }
        }

        public static string BuildReturnData(string cmd, int retCode, List<string>? dataList)
        {
            return EncodeReturnData(cmd, retCode, dataList);
        }

        private static string Process0510(List<string> dataList)
        {
            try
            {
                int TaitiMeterNo = int.Parse(dataList[0]);
                int ModelClass = int.Parse(dataList[1]);
                string TaiTiNo = GetOrCreateSgccTaiTiNo();
                if (TaitiMeterNo == 0 && ModelClass == 0)//返回台体编号
                {
                    return EncodeReturnData("0510", 0, new List<string> { "0", "0", TaiTiNo });
                }
                else
                {
                    return EncodeReturnData("0510", 1, new List<string> { null });
                }
            }
            catch (Exception)
            {
                return EncodeReturnData("0510", 1, null);
            }
        }

        private static string GetOrCreateSgccTaiTiNo()
        {
            string taiTiNo = Confighelper
                .ReadIni(TaiTiNoSection, SgccTaiTiNoKey, "", 255, ConfigPath)
                .Trim();

            if (!string.IsNullOrWhiteSpace(taiTiNo))
            {
                return taiTiNo;
            }

            Confighelper.WriteIni(TaiTiNoSection, SgccTaiTiNoKey, DefaultSgccTaiTiNo, ConfigPath);
            return DefaultSgccTaiTiNo;
        }

        private static string Process0101(List<string> dataList)
        {
            try
            {
                string ip = dataList[0];
                string port = dataList[1];
                return EncodeReturnData("0101", 0, null);
            }
            catch (Exception)
            {
                return EncodeReturnData("0101", 1, null);
            }
        }
        /// <summary>
        /// 初始化台体多路服务器串口
        /// </summary>
        /// <param name="dataList"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static string Process0102(List<string> dataList)
        {
            try
            {
                string ServerTaitiNum = dataList[0];
                string PortClass = dataList[1];
                return EncodeReturnData("0102", 0, null);
            }
            catch (Exception)
            {
                return EncodeReturnData("0101", 1, null);
            }
        }

        /// <summary>
        /// 处理 0104 协议参数配置。
        /// 发送格式: cmd=0104,data=0;0;0;9600-n-8-1
        /// </summary>
        private static string Process0104(List<string> dataList)
        {
            try
            {
                string taitiMeterNo = dataList[0];
                string modelClass = dataList[1];
                string portNo = dataList[2];
                string portParameter = dataList[3];
                LogMessage.Info($"0104参数配置: 台体表位={taitiMeterNo}, 模块类型={modelClass}, 端口={portNo}, 参数={portParameter}");
                return EncodeReturnData("0104", 0, null);
            }
            catch (Exception ex)
            {
                LogMessage.Error("0104参数配置处理异常", ex);
                return EncodeReturnData("0104", 1, null);
            }
        }

        /// <summary>
        /// 处理 0401 初始化台体表位。
        /// 收到后先直接回复成功，具体业务后续补充。
        /// </summary>
        private static string Process0401(List<string> dataList)
        {
            try
            {
                LogMessage.Info($"0401初始化台体表位命令已收到，data={string.Join(";", dataList ?? [])}");
                return EncodeReturnData("0401", 0, null);
            }
            catch (Exception ex)
            {
                LogMessage.Error("0401初始化台体表位处理异常", ex);
                return EncodeReturnData("0401", 1, null);
            }
        }

        public static void Execute0401PostReplyBusiness(string receiveData)
        {
            var (_, dataList) = ParseReceiveData(receiveData);
            if (dataList == null)
            {
                return;
            }

            Ensure0401QueueConsumerStarted();
            if (!PcbPowerOnQueue.Writer.TryWrite(new List<string>(dataList)))
            {
                LogMessage.Info("0401后置PCB任务入队失败");
            }
        }

        private static void Ensure0401QueueConsumerStarted()
        {
            if (Interlocked.CompareExchange(ref _pcbPowerOnQueueStarted, 1, 0) != 0)
            {
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var dataList in PcbPowerOnQueue.Reader.ReadAllAsync().ConfigureAwait(false))
                    {
                        await SendPcbPowerOnAfter0401Async(dataList).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage.Error("0401后置PCB队列消费者异常", ex);
                }
            });
        }

        private static async Task SendPcbPowerOnAfter0401Async(List<string> dataList)
        {
            if (_messageSender == null)
            {
                LogMessage.Info("0401后置PCB上电失败: 发送器未注册");
                return;
            }

            var pcbPowerOnMessages = BuildPcbPowerOnMessages(dataList);
            bool isBroadcast = IsPcbBroadcastData(dataList);
            int communicationMethod = isBroadcast
                ? PcbBroadcastCommunicationMethod
                : PcbCommunicationMethod;

            if (isBroadcast)
            {
                string broadcastMessage = string.Concat(pcbPowerOnMessages);
                var result = await _messageSender(null, DefaultPcbIndex, communicationMethod, broadcastMessage).ConfigureAwait(false);
                if (!result.Success)
                {
                    LogMessage.Info($"0401后置PCB广播上电命令发送失败: {result.CallResult}");
                    return;
                }

                LogMessage.Info("0401后置PCB广播上电命令发送成功");
                return;
            }

            string pcbPowerOnMessage = string.Concat(pcbPowerOnMessages);
            var sendResult = await _messageSender(null, DefaultPcbIndex, communicationMethod, pcbPowerOnMessage).ConfigureAwait(false);
            if (sendResult.Success)
            {
                LogMessage.Info($"0401后置PCB上电命令发送成功: {sendResult.CallResult}");
                await Task.Delay(GetPcbPowerOnAfterResponseDelayMs()).ConfigureAwait(false);
                return;
            }

            LogMessage.Info($"0401后置PCB上电命令发送失败: {sendResult.CallResult}");
        }

        private static List<string> BuildPcbPowerOnMessages(List<string> dataList)
        {
            if (dataList.Count < 2)
            {
                throw new ArgumentException("0401数据项不足，无法生成PCB表位上电命令");
            }

            string addressByte = ToOneByteHex(dataList[0]);
            string powerByte = dataList[1].Trim() == "1" ? "07" : "00";
            string voltageMessage = BuildPcbFrame(addressByte, "21", powerByte);
            string currentMessage = BuildPcbFrame(addressByte, "22", powerByte);
            return new List<string> { voltageMessage, currentMessage };
        }

        private static string BuildPcbFrame(string addressByte, string command, string dataByte)
        {
            string frameBody = $"0700{addressByte}00{command}{dataByte}";
            string checksum = MessagesCheckSum.CalculateChecksum(frameBody);
            return $"55{frameBody}{checksum}AA";
        }

        private static bool IsPcbBroadcastData(List<string> dataList)
        {
            return dataList.Count > 0 && dataList[0].Trim() == "0";
        }

        private static int GetPcbPowerOnAfterResponseDelayMs()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PcbConfigPath) ?? Application.StartupPath);
                string configuredDelay = Confighelper
                    .ReadIni(Pcb0401Section, Pcb0401AfterResponseDelayKey, "", 255, PcbConfigPath)
                    .Trim();

                if (int.TryParse(configuredDelay, out int delayMs) && delayMs >= 0)
                {
                    return delayMs;
                }

                Confighelper.WriteIni(
                    Pcb0401Section,
                    Pcb0401AfterResponseDelayKey,
                    DefaultPcbPowerOnAfterResponseDelayMs.ToString(CultureInfo.InvariantCulture),
                    PcbConfigPath);
            }
            catch (Exception ex)
            {
                LogMessage.Error("读取0401后置PCB发送延迟配置异常", ex);
            }

            return DefaultPcbPowerOnAfterResponseDelayMs;
        }

        private static string ToOneByteHex(string value)
        {
            if (!int.TryParse(value?.Trim(), out int number) || number < 0 || number > 255)
            {
                throw new ArgumentException($"表位号无效: {value}");
            }

            if (number == 0)
            {
                return "FF";
            }

            return number.ToString("X2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 处理命令 0201 - 根据传入数据执行操作
        /// 数据格式: data=0;63;220;1.5;0;50;0
        /// 索引0: 参数1, 索引1: 参数2, 索引2: 参数3, 等等...
        /// </summary>
        private static async Task<string> Process0201Async(List<string> dataList)
        {
            try
            {
                // 根据实际业务需求解析各个参数
                // 示例：假设数据格式为 [模式, 温度, 速度, 比例, 标志, 阈值, 状态]
                int param1 = int.Parse(dataList[0]);      // 0
                int param2 = int.Parse(dataList[1]);      // 63
                double param3 = int.Parse(dataList[2]);      // 220
                double param4 = double.Parse(dataList[3]); // 1.5
                int param5 = int.Parse(dataList[4]);      // 0
                int param6 = int.Parse(dataList[5]);      // 50
                int param7 = int.Parse(dataList[6]);      // 0

                // TODO: 在这里编写您的实际业务逻辑
                // 例如：控制设备、保存数据、计算等
                bool isSuccess = await ExecuteBusinessLogicAsync(param1, param2, param3, param4, param5, param6, param7).ConfigureAwait(false);

                // 根据处理结果返回
                if (isSuccess)
                {
                    // 处理成功，可以返回相关数据
                    // 没有返回数据时填 null
                    return EncodeReturnData("0201", 0, null);
                }
                else
                {
                    return EncodeReturnData("0201", 1, null);
                }
            }
            catch (FormatException ex)
            {
                return EncodeReturnData("0201", 1, new List<string> { $"数据格式错误: {ex.Message}" });
            }
            catch (IndexOutOfRangeException)
            {
                return EncodeReturnData("0201", 1, new List<string> { $"数据项数量不足，需要7个参数，实际收到{dataList.Count}个" });
            }
            catch (Exception ex)
            {
                return EncodeReturnData("0201", 1, new List<string> { $"处理异常: {ex.Message}" });
            }
        }

        /// <summary>
        /// 处理命令 0202 - 无数据示例
        /// </summary>
        private static string Process0202(List<string> dataList)
        {
            // 无数据的命令处理逻辑
            bool isSuccess = DoSomething();

            if (isSuccess)
            {
                return EncodeReturnData("0202", 0, null);
            }
            else
            {
                return EncodeReturnData("0202", 1, new List<string> { "操作失败" });
            }
        }

        private static async Task<string> Process0301Async(List<string> dataList)
        {
            try
            {
                if (!XYCtr.IsSourcePortOpen)
                {
                    if (!await OpenSourcePortAsync().ConfigureAwait(false))
                    {
                        LogMessage.Info("0301读取标准表失败: 源串口未打开且自动打开失败");
                        return EncodeReturnData("0301", 1, null);
                    }
                }
                var sStandValue = new byte[1024];
                Array.Clear(sStandValue, 0, sStandValue.Length);

                var readResult = await SourceController.CallReadStandValueAsync("model1", sStandValue).ConfigureAwait(false);
                if (!readResult.Success)
                {
                    string message = readResult.Result switch
                    {
                        XYCtr.TimeoutResult => "读取标准表超时",
                        XYCtr.BusyResult => "读取标准表正在处理中",
                        _ => "读取标准表失败"
                    };
                    LogMessage.Info($"0301{message}");
                    return EncodeReturnData("0301", 1, null);
                }

                var rawText = Encoding.Default.GetString(sStandValue).TrimEnd('\0', '\r', '\n', ' ');
                var parts = ModelTool.SplitString(rawText);
                var data = Build0301Data(parts);
                if (data == null || data.Count < 16)
                {
                    LogMessage.Info("0301标准表数据项不足");
                    return EncodeReturnData("0301", 1, null);
                }

                return EncodeReturnData("0301", 0, data);
            }
            catch (Exception ex)
            {
                LogMessage.Error("0301命令处理异常", ex);
                return EncodeReturnData("0301", 1, null);
            }
        }

        private static List<string>? Build0301Data(IList<string> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                return null;
            }

            if (parts.Count == 16)
            {
                return new List<string>(parts);
            }

            if (parts.Count < 16)
            {
                return null;
            }

            if (parts.Count >= 22)
            {
                var totalActive = SumParts(parts, 9, 3);
                var totalReactive = SumParts(parts, 12, 3);
                var powerFactor = CalcPowerFactor(totalActive, totalReactive);

                return new List<string>
                {
                    parts[0],
                    parts[6],
                    parts[1],
                    parts[7],
                    parts[2],
                    parts[8],
                    parts[3],
                    parts[6],
                    parts[4],
                    parts[7],
                    parts[5],
                    parts[8],
                    FormatNumber(totalActive),
                    FormatNumber(totalReactive),
                    FormatNumber(powerFactor),
                    parts[15]
                };
            }

            return parts.Count >= 16
                ? parts.Take(16).ToList()
                : null;
        }

        private static double SumParts(IList<string> parts, int startIndex, int count)
        {
            double total = 0;
            for (int i = startIndex; i < startIndex + count && i < parts.Count; i++)
            {
                if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
                    double.TryParse(parts[i], NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                {
                    total += value;
                }
            }

            return total;
        }

        private static double CalcPowerFactor(double activePower, double reactivePower)
        {
            var denominator = Math.Sqrt(activePower * activePower + reactivePower * reactivePower);
            if (denominator == 0)
            {
                return 0;
            }

            return Math.Abs(activePower) / denominator;
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// 业务逻辑执行示例（请根据实际需求修改）
        /// </summary>
        private static async Task<bool> ExecuteBusinessLogicAsync(int p1, int p2, double p3, double p4, int p5, int p6, int p7)
        {
            string sourceWho = Confighelper.ReadIni("TaiTiSource", "Source", "", 255, ConfigPath);
            // TODO: 替换为您的实际业务逻辑
            //接线方式; 电压电流输出控制; 电压幅值; 电流幅值; 功率因数角φ; 频率; 谐波开关
            // 模拟成功条件（可以根据实际需求修改）
            if (p1 == 0 || sourceWho == "XY")
            {
                if (!XYCtr.IsSourcePortOpen && !await OpenSourcePortAsync().ConfigureAwait(false))
                {
                    LogMessage.Info("0201控源失败: 源串口未打开且自动打开失败");
                    return false;
                }

                string ui = $"{p3}_{p3}_{p3}_{p4}_{p4}_{p4}_0_0_0_120_240";
                int pulst = 0;
                var resutlt = SourceController.CallAnyUIOutput(ui, pulst);
                return resutlt.Success;  // 成功
            }

            return false; // 失败
        }

        private static bool DoSomething()
        {
            // 其他业务逻辑
            return true;
        }

        /// <summary>
        /// 解析接收到的数据
        /// </summary>
        private static (string cmd, List<string> dataList) ParseReceiveData(string dataStr)
        {
            if (string.IsNullOrWhiteSpace(dataStr))
            {
                throw new ArgumentException("接收数据不能为空");
            }
            // 正则匹配: cmd=xxxx,data=xxx
            string pattern = @"^cmd=([a-fA-F0-9]{4})(?:,ret=\d+)?,data=(.+)$";
            Match match = Regex.Match(dataStr.Trim(), pattern, RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                throw new ArgumentException($"数据格式不合法: {dataStr}");
            }

            string cmd = match.Groups[1].Value;
            string dataValue = match.Groups[2].Value;

            List<string> dataList = null;
            if (!dataValue.Equals("null", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(dataValue))
            {
                dataList = new List<string>(dataValue.Split(';'));
            }

            return (cmd, dataList);
        }

        /// <summary>
        /// 编码返回数据
        /// </summary>
        private static string EncodeReturnData(string cmd, int retCode, List<string>? dataList)
        {
            string dataPart = retCode == 1
                ? NormalizeFailureData(dataList)
                : EncodeSuccessData(dataList);
            return $"cmd={cmd},ret={retCode},data={dataPart}";
        }

        private static string EncodeSuccessData(List<string>? dataList)
        {
            if (dataList == null || dataList.Count == 0)
            {
                return "null";
            }

            return string.Join(";", dataList);
        }

        private static string NormalizeFailureData(List<string>? dataList)
        {
            if (dataList == null || dataList.Count == 0)
            {
                return "null";
            }

            if (dataList.Count == 1)
            {
                string value = dataList[0]?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(value) || value.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    return "null";
                }

                if (value == "0" || value.Equals("O", StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }

                if (value.Equals("0F", StringComparison.OrdinalIgnoreCase))
                {
                    return "0F";
                }
            }

            return "null";
        }

        private static string TryExtractCommand(string? receiveData)
        {
            if (!string.IsNullOrWhiteSpace(receiveData))
            {
                Match match = Regex.Match(receiveData, @"cmd=([a-fA-F0-9]{4})", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return "0000";
        }
    }
}
