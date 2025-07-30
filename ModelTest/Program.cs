

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
    }
}