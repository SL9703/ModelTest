using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public sealed class LogMessage
    {
        public static LogMessage Instance { get; } = new();

        private readonly object _writeLock = new();
        private readonly string _baseLogDirectory;

        private LogMessage()
        {
            _baseLogDirectory = Path.Combine(AppContext.BaseDirectory, "XCKJ_logs");
        }

        /// <summary>
        /// 调试日志
        /// </summary>
        /// <param name="ex"></param>
        public static void Debug(string? ex) => Instance.WriteDailyLog("Debug", ex?.ToString());

        /// <summary>
        /// 运行日志
        /// </summary>
        /// <param name="ex"></param>
        public static void Info(string? ex) => Instance.WriteDailyLog("Info", ex?.ToString());

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="ex"></param>
        public static void Error(Exception? ex) => Instance.WriteDailyLog("Error", ex?.ToString());

        public static void Error(string errlog, Exception? ex)
        {
            string message = string.IsNullOrWhiteSpace(errlog)
                ? ex?.ToString() ?? string.Empty
                : $"{errlog}{Environment.NewLine}{ex}";
            Instance.WriteDailyLog("Error", message);
        }

        /// <summary>
        /// socket日志
        /// </summary>
        /// <param name="ex"></param>
        public static void SocketLog(string? ex) => Instance.WriteDailyLog("Socket", ex?.ToString());

        /// <summary>
        /// 测试日志
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="testName"></param>
        public static void TestLog(string? ex, string testName) => Instance.WriteTestLog(ex?.ToString(), testName);

        /// <summary>
        /// MeterTest 工位通信日志，按父级TestItem和工位号独立保存。
        /// testItemName不得传入TestSubItem名称，所有子步骤必须追加到同一个TestItem文件。
        /// </summary>
        public static void MeterTestStationLog(string testItemName, int stationNo, string? message)
            => Instance.WriteMeterTestStationLog(testItemName, stationNo, message);

        /// <summary>
        /// MeterTest 工位通信日志原样写入，按父级TestItem和工位号独立保存。
        /// testItemName不得传入TestSubItem名称。
        /// </summary>
        public static void MeterTestStationRawLog(string testItemName, int stationNo, string? message)
            => Instance.WriteMeterTestStationRawLog(testItemName, stationNo, message);

        private void WriteDailyLog(string level, string? message)
        {
            DateTime now = DateTime.Now;
            string logDateTime = $"{now:yyyy-MM-dd}";
            string logPath = Path.Combine(GetLogDirectory(logDateTime), $"Debuglog_{logDateTime}.log");
            string logMessage = $"[{now:yyyy-MM-dd HH:mm:ss:fff}] - [{level}] - {message}";
            AppendLine(logPath, logMessage);
        }

        private void WriteTestLog(string? message, string testName)
        {
            DateTime now = DateTime.Now;
            string logDateTime = $"{now:yyyy-MM-dd}";
            string safeTestName = SanitizeFileName(testName);
            string logPath = Path.Combine(GetLogDirectory(logDateTime), $"{safeTestName}TestLog_{logDateTime}.log");
            string logMessage = $"[{now:yyyy-MM-dd HH:mm:ss:fff}] - {message}";
            AppendLine(logPath, logMessage);
        }

        private void WriteMeterTestStationLog(string testItemName, int stationNo, string? message)
        {
            DateTime now = DateTime.Now;
            string logPath = GetMeterTestStationLogPath(testItemName, stationNo, now);
            string logMessage = $"[{now:yyyy-MM-dd HH:mm:ss:fff}] - {message}";
            AppendLine(logPath, logMessage);
        }

        private void WriteMeterTestStationRawLog(string testItemName, int stationNo, string? message)
        {
            string logPath = GetMeterTestStationLogPath(testItemName, stationNo, DateTime.Now);
            AppendLine(logPath, message ?? string.Empty);
        }

        private void AppendLine(string logPath, string logMessage)
        {
            try
            {
                lock (_writeLock)
                {
                    File.AppendAllText(logPath, logMessage + Environment.NewLine);
                }
            }
            catch
            {
                // 如果日志写入失败，忽略异常避免循环
            }
        }

        private string GetLogDirectory(string logDateTime)
        {
            string logDirectory = Path.Combine(_baseLogDirectory, logDateTime);
            EnsureLogDirectoryExists(logDirectory);
            return logDirectory;
        }

        private string GetMeterTestStationLogPath(string testItemName, int stationNo, DateTime now)
        {
            string logDirectory = Path.Combine(
                _baseLogDirectory,
                "TextLog",
                now.ToString("yy"),
                now.ToString("MM"),
                now.ToString("dd"));

            EnsureLogDirectoryExists(logDirectory);
            // 文件粒度固定为TestItem+工位；TestSubItem及内部步骤只能写入文件内容。
            string safeFileName = SanitizeFileName($"{testItemName}工位{stationNo}");
            return Path.Combine(logDirectory, $"{safeFileName}.log");
        }

        private static void EnsureLogDirectoryExists(string logDirectory)
        {
            try
            {
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建日志目录失败: {ex.Message}");
                throw;
            }
        }

        private static string SanitizeFileName(string? fileName)
        {
            string safeFileName = string.IsNullOrWhiteSpace(fileName) ? "Default" : fileName;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeFileName = safeFileName.Replace(invalidChar, '_');
            }

            return safeFileName;
        }
    }
}
