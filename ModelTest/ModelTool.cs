using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest
{
    public static class ModelTool
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? value.ToString();
        }

        public static IList<string> SplitString(string strData)
        {

            if (string.IsNullOrEmpty(strData))
                return Array.Empty<string>();
            return strData.Split(';', StringSplitOptions.RemoveEmptyEntries)
                 .Select(p => p == "None" ? null : p)
                 .ToList();
        }
        // 单独的调试输出方法,打印日志
        public static void LogSplitResult(IList<string> parts)
        {
            if (parts == null || parts.Count == 0)
            {
                LogMessage.Debug("分割结果为空");
                return;
            }

            LogMessage.Debug($"分割结果（共 {parts.Count} 项）:");
            for (int i = 0; i < parts.Count; i++)
            {
                LogMessage.Debug($"[{i}] {parts[i] ?? "null"}");
            }
        }
        public static string[] StringDataSplit(string ServerData)
        {
            // 分割字符串并移除空条目
            string[] parts = ServerData.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            // 处理特殊值：将 "None" 转换为 null，数值保持原样
            var ServerDataNew = parts.Select(p => p == "None" ? "None" : p).ToArray();
            return ServerDataNew;
        }
    }
}
