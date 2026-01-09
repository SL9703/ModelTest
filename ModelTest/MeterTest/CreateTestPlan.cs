using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 创建测试方案类
    /// </summary>
    internal static class CreateTestPlan
    {
        public static TestPlan MeterCreateTestPlan() {

            var testPlan = new TestPlan
            {
                Name = " 电表测试方案",
                Description = "这是一个测试方案"
            };

            // 测试用例1
            var testCase1 = new TestCase
            {
                Name = "电表通信功能测试",
                Description = "测试电表通信功能"
            };

            testCase1.Steps.Add(new TestStep
            {
                Name = " 读取通信地址",
                Description = "698通信测试",
                ExecuteAction = () => LogMessage.TestLog("读取通信地址.", testCase1.Name)
            });

            // 测试用例2
            var testCase2 = new TestCase
            {
                Name = "波特率设置",
                Description = "测试波特率"
            };

            testCase2.Steps.Add(new TestStep
            {
                Name = " 读取通信地址",
                Description = "读取通信地址",
                ExecuteAction =  () =>
                {
                    LogMessage.TestLog("读取通信地址", testCase2.Name);
                }
            });

            testCase2.Steps.Add(new TestStep
            {
                Name = " 读取安全模式参数",
                Description = "读取安全模式参数",
                ExecuteAction = () => LogMessage.TestLog("读取安全模式参数", testCase2.Name),
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 读取表号",
                Description = "读取表号",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("读取表号", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 读取ESAM信息",
                Description = "读取ESAM信息",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("读取ESAM信息", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 建立应用连接",
                Description = "建立应用连接",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("建立应用连接", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 设置RS485通信参数（2400bps-e-8-1,无流控）",
                Description = "设置RS485通信参数（2400bps-e-8-1,无流控）",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog(" 设置RS485通信参数（2400bps-e-8-1,无流控）", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 延时等待5秒",
                Description = " 延时等待5秒",
                ExecuteAction =  () =>
                {
                    LogMessage.TestLog("延时等待5秒", testCase2.Name);
                    Task.Delay(5000);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 初始化通道参数（2400bps-e-8-1）",
                Description = " 设置RS485通信参数 （ 2400bps-e-8-1,无流控 ）",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("初始化通道参数 （ 2400bps-e-8-1 ）", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 读取RS485通信参数",
                Description = "读取RS485通信参数",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("读取RS485通信参数", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 设置RS485通信参数（4800bps-e-8-1,无流控）",
                Description = "设置RS485通信参数 （ 4800bps-e-8-1,无流控 ）",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("设置RS485通信参数 （ 4800bps-e-8-1,无流控 ）", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 延时等待5秒",
                Description = "延时等待5秒",
                ExecuteAction =  () =>
                {
                    LogMessage.TestLog("延时等待5秒", testCase2.Name);
                     Task.Delay(5000);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 初始化通道参数（4800bps-e-8-1）",
                Description = "初始化通道参数 （4800bps-e-8-1 ）",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("初始化通道参数 （4800bps-e-8-1 ）", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 读取RS485通信参数",
                Description = "读取RS485通信参数",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("读取RS485通信参数", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 设置RS485通信参数（9600bps-e-8-1,无流控）",
                Description = "设置RS485通信参数 （ 9600bps-e-8-1,无流控 ）",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("设置RS485通信参数 （ 9600bps-e-8-1,无流控 ）", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 延时等待5秒",
                Description = "延时等待5秒",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("延时等待5秒", testCase2.Name);
                    Task.Delay(5000);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 初始化通道参数（9600bps-e-8-1）",
                Description = "初始化通道参数（9600bps-e-8-1 ）",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("初始化通道参数（9600bps-e-8-1 ）", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 读取RS485通信参数",
                Description = "读取RS485通信参数",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("读取RS485通信参数", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 设置RS485通信参数（115200bps-e-8-1,无流控）",
                Description = "设置RS485通信参数（ 115200bps-e-8-1,无流控 ）",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("设置RS485通信参数（115200bps-e-8-1,无流控）", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 延时等待5秒",
                Description = "延时等待5秒",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("延时等待5秒", testCase2.Name);
                    Task.Delay(5000);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 初始化通道参数（115200bps-e-8-1）",
                Description = "初始化通道参数（115200bps-e-8-1）",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("初始化通道参数（115200bps-e-8-1）", testCase2.Name);
                }
            });
            testCase2.Steps.Add(new TestStep
            {
                Name = " 读取RS485通信参数",
                Description = "读取RS485通信参数",
                ExecuteAction = () =>
                {
                    LogMessage.TestLog("读取RS485通信参数", testCase2.Name);
                }
            });
            testPlan.TestCases.Add(testCase1);
            testPlan.TestCases.Add(testCase2);
            return testPlan;

        }
    }
}
