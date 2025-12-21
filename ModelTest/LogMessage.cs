using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public class LogMessage
    {
        private static string logDateTime = $"{DateTime.Now:yyyy-MM-dd}";
        private readonly static string _logDirectory = $"{logDateTime}";
        /// <summary>
        /// 调试日志
        /// </summary>
        /// <param name="ex"></param>
        public static void Debug(string? ex)
        {
            EnsureLogDirectoryExists();
            string logPath = Path.Combine(_logDirectory, $"Debuglog_{logDateTime}.log");
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:sss}] - {ex?.ToString()}";
            try
            {
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // 如果日志写入失败，忽略异常避免循环
            }
        }

        /// <summary>
        /// 运行日志
        /// </summary>
        /// <param name="ex"></param>
        public static void Info(string? ex)
        {
            EnsureLogDirectoryExists();
            string logPath = Path.Combine(_logDirectory, $"Infolog_{logDateTime}.log");
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:sss}] - {ex?.ToString()}";
            try
            {
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // 如果日志写入失败，忽略异常避免循环
            }
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="ex"></param>
        public static void Error(Exception? ex)
        {
            EnsureLogDirectoryExists();
            string logPath = Path.Combine(_logDirectory, $"Errorlog_{logDateTime}.log");
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:sss}] - {ex?.ToString()}";

            try
            {
                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // 如果日志写入失败，忽略异常避免循环
            }
        }
      
        private static void EnsureLogDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                    Console.WriteLine($"日志目录已创建: {_logDirectory}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建日志目录失败: {ex.Message}");
                throw;
            }
        }
    }
}
