using System.Diagnostics;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// 测试引擎
    /// </summary>
    internal class TestExecutionEngine
    {
        public event Action<TestPlan> TestPlanStarted;
        public event Action<TestPlan> TestPlanCompleted;
        public event Action<TestCase> TestCaseStarted;
        public event Action<TestCase> TestCaseCompleted;
        public event Action<TestStep> TestStepStarted;
        public event Action<TestStep> TestStepCompleted;
        public event Action<string> LogMessage;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        public async Task<TestPlan> ExecuteTestPlanAsync(TestPlan testPlan)
        {
            try
            {
                OnLogMessage($"开始执行测试方案: {testPlan.Name}");

                testPlan.Status = TestStatus.Running;
                testPlan.StartTime = DateTime.Now;

                TestPlanStarted?.Invoke(testPlan);

                foreach (var testCase in testPlan.TestCases)
                {
                    if (!testCase.IsEnabled)
                    {
                        testCase.Status = TestStatus.Skipped;
                        OnLogMessage($"跳过测试用例: {testCase.Name}");
                        continue;
                    }

                    await ExecuteTestCaseAsync(testCase);
                }

                testPlan.EndTime = DateTime.Now;
                testPlan.Status = testPlan.TestCases.Exists(tc => tc.Status == TestStatus.Failed)
                    ? TestStatus.Failed
                    : TestStatus.Passed;

                OnLogMessage($"测试方案执行完成: {testPlan.Name}, 状态: {testPlan.Status}");
                TestPlanCompleted?.Invoke(testPlan);

                return testPlan;
            }
            catch (Exception ex)
            {
                testPlan.Status = TestStatus.Failed;
                OnLogMessage($"测试方案执行失败: {ex.Message}");
                throw;
            }
        }

        private async Task ExecuteTestCaseAsync(TestCase testCase)
        {
            try
            {
                OnLogMessage($"开始执行测试用例: {testCase.Name}");

                testCase.Status = TestStatus.Running;
                _stopwatch.Restart();

                TestCaseStarted?.Invoke(testCase);

                foreach (var step in testCase.Steps)
                {
                    await ExecuteTestStepAsync(step);

                    // 如果步骤失败，整个测试用例失败
                    if (step.Status == TestStatus.Failed)
                    {
                        testCase.Status = TestStatus.Failed;
                        break;
                    }
                }

                _stopwatch.Stop();
                testCase.ExecutionTime = _stopwatch.Elapsed;

                if (testCase.Status == TestStatus.Running)
                {
                    testCase.Status = TestStatus.Passed;
                }

                OnLogMessage($"测试用例执行完成: {testCase.Name}, 状态: {testCase.Status}, 耗时: {testCase.ExecutionTime}");
                TestCaseCompleted?.Invoke(testCase);
            }
            catch (Exception ex)
            {
                testCase.Status = TestStatus.Failed;
                OnLogMessage($"测试用例执行异常: {testCase.Name}, 错误: {ex.Message}");
                throw;
            }
        }

        private async Task ExecuteTestStepAsync(TestStep step)
        {
            try
            {
                OnLogMessage($"开始执行测试步骤: {step.Name}");

                step.Status = TestStatus.Running;
                _stopwatch.Restart();

                TestStepStarted?.Invoke(step);

                // 执行测试步骤
                if (step.ExecuteAction != null)
                {
                    if (step.ExecuteAction.Method.ReturnType == typeof(Task))
                    {
                        await (Task)step.ExecuteAction.DynamicInvoke();
                    }
                    else
                    {
                        step.ExecuteAction();
                    }
                }

                // 执行验证
                if (step.ValidationAction != null)
                {
                    bool isValid = false;

                    if (step.ValidationAction.Method.ReturnType == typeof(Task<bool>))
                    {
                        isValid = await (Task<bool>)step.ValidationAction.DynamicInvoke();
                    }
                    else
                    {
                        isValid = step.ValidationAction();
                    }

                    step.Status = isValid ? TestStatus.Passed : TestStatus.Failed;
                }
                else
                {
                    step.Status = TestStatus.Passed;
                }

                _stopwatch.Stop();
                step.ExecutionTime = _stopwatch.Elapsed;

                OnLogMessage($"测试步骤执行完成: {step.Name}, 状态: {step.Status}");
                TestStepCompleted?.Invoke(step);
            }
            catch (Exception ex)
            {
                step.Status = TestStatus.Failed;
                step.ErrorMessage = ex.Message;
                OnLogMessage($"测试步骤执行失败: {step.Name}, 错误: {ex.Message}");
            }
        }

        private void OnLogMessage(string message)
        {
            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

    }
}
