using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelTest.MeterTest
{
    internal class TestReportGenerator
    {
        public static string GenerateReport(TestPlan testPlan)
        {
            var html = new StringBuilder();

            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<title>测试报告</title>");
            html.AppendLine("<style>");
            html.AppendLine(CssStyles());
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine($"<h1>测试报告 - {testPlan.Name}</h1>");
            html.AppendLine($"<div class='summary'>");
            html.AppendLine($"<p><strong>方案描述:</strong> {testPlan.Description}</p>");
            html.AppendLine($"<p><strong>开始时间:</strong> {testPlan.StartTime:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine($"<p><strong>结束时间:</strong> {testPlan.EndTime:yyyy-MM-dd HH:mm:ss}</p>");
            html.AppendLine($"<p><strong>总耗时:</strong> {testPlan.TotalExecutionTime}</p>");
            html.AppendLine($"<p><strong>最终状态:</strong> <span class='status-{testPlan.Status.ToString().ToLower()}'>{testPlan.Status}</span></p>");
            html.AppendLine("</div>");

            foreach (var testCase in testPlan.TestCases)
            {
                html.AppendLine($"<div class='test-case'>");
                html.AppendLine($"<h2>📋 {testCase.Name} <span class='status-{testCase.Status.ToString().ToLower()}'>{testCase.Status}</span></h2>");
                html.AppendLine($"<p><em>{testCase.Description}</em></p>");

                if (testCase.Steps.Count > 0)
                {
                    html.AppendLine("<table>");
                    html.AppendLine("<tr><th>步骤</th><th>描述</th><th>状态</th><th>耗时</th><th>错误信息</th></tr>");

                    foreach (var step in testCase.Steps)
                    {
                        html.AppendLine("<tr>");
                        html.AppendLine($"<td>{step.Name}</td>");
                        html.AppendLine($"<td>{step.Description}</td>");
                        html.AppendLine($"<td class='status-{step.Status.ToString().ToLower()}'>{step.Status}</td>");
                        html.AppendLine($"<td>{step.ExecutionTime}</td>");
                        html.AppendLine($"<td>{step.ErrorMessage}</td>");
                        html.AppendLine("</tr>");
                    }

                    html.AppendLine("</table>");
                }
                html.AppendLine("</div>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private static string CssStyles()
        {
            return @"
                body { font-family: Arial, sans-serif; margin: 20px; }
                h1 { color: #333; border-bottom: 2px solid #4CAF50; padding-bottom: 10px; }
                .summary { background: #f5f5f5; padding: 15px; border-radius: 5px; margin-bottom: 20px; }
                .test-case { border: 1px solid #ddd; margin-bottom: 20px; padding: 15px; border-radius: 5px; }
                table { width: 100%; border-collapse: collapse; margin-top: 10px; }
                th { background: #4CAF50; color: white; padding: 10px; text-align: left; }
                td { padding: 10px; border-bottom: 1px solid #ddd; }
                .status-passed { color: green; font-weight: bold; }
                .status-failed { color: red; font-weight: bold; }
                .status-running { color: blue; font-weight: bold; }
                .status-notstarted { color: gray; }
                .status-skipped { color: orange; }
            ";
        }
    }
}
