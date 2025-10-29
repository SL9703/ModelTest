


namespace ModelTest
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        // 在文件顶部添加

    [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            try
            {
                // 添加全局异常处理
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += Application_ThreadException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new ModelMain());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"发生未处理异常：{ex.Message}\n\n{ex.StackTrace}",
                    "应用程序错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
    }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void HandleException(Exception ex)
        {
            if (ex != null)
            {
                // 记录日志
                LogException(ex);

                // 显示友好错误信息
                MessageBox.Show(
                    $"程序发生错误：{ex.Message}\n\n请联系技术支持。",
                    "系统错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                // 可以选择是否退出程序
                // Application.Exit();
            }
        }

        private static void LogException(Exception ex)
        {
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.ToString()}\n\n";

            try
            {
                File.AppendAllText(logPath, logMessage);
            }
            catch
            {
                // 如果日志写入失败，忽略异常避免循环
            }
        }
    }
}