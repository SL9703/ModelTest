


namespace ModelTest
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        // 在文件顶部添加
        // 静态主窗体引用
        public static ModelMain MainForm { get; private set; }
        public static DatabaseTestForm _databaseTestForm { get; private set; }

        /// <summary>
        /// 同一Windows登录会话只允许启动一个MeterTest进程，防止多个实例同时占用源、串口和PCB连接。
        /// </summary>
        private const string MeterTestSingleInstanceMutexName = @"Local\XCKJ.ModelTest.MeterTest.SingleInstance";

        [STAThread]
        static void Main()
        {
            using Mutex singleInstanceMutex = new(
              initiallyOwned: true,
              MeterTestSingleInstanceMutexName,
              out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show(
                    "MeterTest 已经在运行，只允许同时打开一个测试用例。请关闭已有窗口后再试。",
                    "重复启动提醒",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
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
                MainForm = new ModelMain();
                Application.Run(MainForm);
                //_databaseTestForm = new DatabaseTestForm();
                //Application.Run(_databaseTestForm);
                //Application.Run(new MeterTest.MeterTest());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"发生未处理异常：{ex.Message}\n\n{ex.StackTrace}",
                    "应用程序错误",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }finally
            {
                singleInstanceMutex.ReleaseMutex();
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