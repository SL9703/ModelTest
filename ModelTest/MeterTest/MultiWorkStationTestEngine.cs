using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    internal class MultiWorkStationTestEngine
    {
        public event Action<WorkStation> WorkStationStatusChanged;
        public event Action<TestResult> TestResultUpdated;
        public event Action<string> LogMessage;
        public event Action<int, int> ProgressChanged; // 当前完成数，总数

        private readonly WorkStationConfiguration _config;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly SemaphoreSlim _semaphore;
        private int _completedWorkStations;
        private int _totalWorkStations;

        public MultiWorkStationTestEngine(WorkStationConfiguration config)
        {
            _config = config;
            _cancellationTokenSource = new CancellationTokenSource();
            _semaphore = new SemaphoreSlim(
                _config.EnableParallelExecution ? _config.MaxParallelWorkStations : 1,
                _config.EnableParallelExecution ? _config.MaxParallelWorkStations : 1
            );
        }

        public async Task<List<TestResult>> ExecuteTestPlanAsync(TestPlan testPlan, List<WorkStation> selectedWorkStations)
        {
            var results = new List<TestResult>();
            _completedWorkStations = 0;
            _totalWorkStations = selectedWorkStations.Count;

            OnLogMessage($"开始执行多工位测试，选择 {_totalWorkStations} 个工位");

            var tasks = new List<Task<TestResult>>();

            foreach (var workstation in selectedWorkStations.Where(ws => ws.IsSelected))
            {
                tasks.Add(ExecuteForWorkStationAsync(workstation, testPlan));
            }

            // 等待所有工位测试完成
            var completedTasks = await Task.WhenAll(tasks);
            results.AddRange(completedTasks);

            OnLogMessage($"所有工位测试完成，成功: {results.Count(r => r.OverallStatus == TestStatus.Passed)}，失败: {results.Count(r => r.OverallStatus == TestStatus.Failed)}");

            return results;
        }

        private async Task<TestResult> ExecuteForWorkStationAsync(WorkStation workstation, TestPlan testPlan)
        {
            var testResult = new TestResult
            {
                WorkStationId = workstation.Id,
                WorkStationName = workstation.Name,
                TestPlan = testPlan,
                StartTime = DateTime.Now
            };

            try
            {
                // 等待信号量（控制并行度）
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);

                UpdateWorkStationStatus(workstation, WorkStationStatus.Testing);
                OnLogMessage($"工位 {workstation.Name} 开始测试");

                // 连接下位机
                if (!await ConnectToDeviceAsync(workstation))
                {
                    throw new Exception($"连接工位 {workstation.Name} 失败");
                }

                // 执行测试方案
                var caseResults = new List<TestCaseResult>();

                foreach (var testCase in testPlan.TestCases.Where(tc => tc.IsEnabled))
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        break;

                    var caseResult = await ExecuteTestCaseForWorkStationAsync(workstation, testCase);
                    caseResults.Add(caseResult);

                    // 如果配置了"首次失败停止"，则中断
                    if (_config.StopOnFirstFailure && caseResult.Status == TestStatus.Failed)
                        break;
                }

                testResult.CaseResults = caseResults;
                testResult.EndTime = DateTime.Now;
                testResult.OverallStatus = testResult.CaseResults.All(r => r.Status == TestStatus.Passed)
                    ? TestStatus.Passed
                    : TestStatus.Failed;

                // 断开连接
                workstation.DeviceInterface?.Disconnect();
                workstation.IsConnected = false;

                UpdateWorkStationStatus(workstation,
                    testResult.OverallStatus == TestStatus.Passed ? WorkStationStatus.Completed : WorkStationStatus.Error);
            }
            catch (Exception ex)
            {
                testResult.EndTime = DateTime.Now;
                testResult.OverallStatus = TestStatus.Failed;
                testResult.ErrorMessage = ex.Message;

                UpdateWorkStationStatus(workstation, WorkStationStatus.Error);
                OnLogMessage($"工位 {workstation.Name} 测试失败: {ex.Message}");
            }
            finally
            {
                _semaphore.Release();

                // 更新进度
                Interlocked.Increment(ref _completedWorkStations);
                ProgressChanged?.Invoke(_completedWorkStations, _totalWorkStations);

                // 通知结果更新
                TestResultUpdated?.Invoke(testResult);
                workstation.TestResult = testResult;
            }

            return testResult;
        }

        private async Task<bool> ConnectToDeviceAsync(WorkStation workstation)
        {
            try
            {
                OnLogMessage($"连接工位 {workstation.Name} ({workstation.IPAddress}:{workstation.Port})");

                // 创建通信接口
                workstation.DeviceInterface = new MockDeviceCommunication(); // 实际使用时替换为真实实现
                                                                             // workstation.DeviceInterface = new ModbusTcpCommunication();

                bool connected = workstation.DeviceInterface.Connect(
                    workstation.IPAddress,
                    workstation.Port,
                    workstation.Timeout
                );

                if (connected)
                {
                    workstation.IsConnected = true;
                    workstation.ConnectionMessage = "连接成功";
                    workstation.LastCommunication = DateTime.Now;
                    UpdateWorkStationStatus(workstation, WorkStationStatus.Busy);

                    // 发送连接确认命令
                    var response = workstation.DeviceInterface.SendAndReceive("AT", 1000);
                    OnLogMessage($"工位 {workstation.Name} 连接响应: {response}");

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                workstation.ConnectionMessage = $"连接失败: {ex.Message}";
                return false;
            }
        }

        private async Task<TestCaseResult> ExecuteTestCaseForWorkStationAsync(WorkStation workstation, TestCase testCase)
        {
            var caseResult = new TestCaseResult
            {
                TestCaseId = testCase.Id,
                TestCaseName = testCase.Name
            };

            var stopwatch = Stopwatch.StartNew();
            var stepResults = new List<TestStepResult>();

            try
            {
                OnLogMessage($"工位 {workstation.Name} 执行测试用例: {testCase.Name}");

                foreach (var step in testCase.Steps)
                {
                    if (_cancellationTokenSource.IsCancellationRequested)
                        break;

                    var stepResult = await ExecuteTestStepForWorkStationAsync(workstation, step);
                    stepResults.Add(stepResult);

                    // 如果步骤失败，且配置了重试
                    if (stepResult.Status == TestStatus.Failed && _config.RetryOnFailure)
                    {
                        for (int retry = 1; retry <= _config.MaxRetryCount; retry++)
                        {
                            OnLogMessage($"工位 {workstation.Name} 重试步骤 {step.Name} ({retry}/{_config.MaxRetryCount})");

                            stepResult = await ExecuteTestStepForWorkStationAsync(workstation, step);
                            if (stepResult.Status == TestStatus.Passed)
                                break;
                        }
                    }

                    if (stepResult.Status == TestStatus.Failed)
                    {
                        caseResult.ErrorMessage = $"步骤 {step.Name} 失败: {stepResult.ErrorMessage}";
                        break;
                    }
                }

                stopwatch.Stop();
                caseResult.ExecutionTime = stopwatch.Elapsed;
                caseResult.StepResults = stepResults;
                caseResult.Status = stepResults.All(s => s.Status == TestStatus.Passed)
                    ? TestStatus.Passed
                    : TestStatus.Failed;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                caseResult.ExecutionTime = stopwatch.Elapsed;
                caseResult.Status = TestStatus.Failed;
                caseResult.ErrorMessage = ex.Message;
                OnLogMessage($"工位 {workstation.Name} 测试用例 {testCase.Name} 异常: {ex.Message}");
            }

            return caseResult;
        }

        private async Task<TestStepResult> ExecuteTestStepForWorkStationAsync(WorkStation workstation, TestStep step)
        {
            var stepResult = new TestStepResult
            {
                StepId = step.Id,
                StepName = step.Name
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 执行步骤动作（如果有）
                if (step.ExecuteAction != null)
                {
                    OnLogMessage($"工位 {workstation.Name} 执行步骤: {step.Name}");

                    // 这里可以添加特定的下位机通信逻辑
                    string command = GenerateCommandFromStep(step);
                    stepResult.CommandSent = command;

                    // 发送到下位机
                    if (workstation.DeviceInterface != null && workstation.IsConnected)
                    {
                        string response = workstation.DeviceInterface.SendAndReceive(command, 3000);
                        stepResult.ResponseReceived = response;

                        // 更新最后通信时间
                        workstation.LastCommunication = DateTime.Now;

                        // 验证响应
                        if (step.ValidationAction != null)
                        {
                            bool isValid = false;

                            // 可以在这里将响应传递给验证函数
                            if (step.ValidationAction.Method.GetParameters().Length > 0)
                            {
                                // 如果验证函数接受参数，传递响应
                                isValid = (bool)step.ValidationAction.DynamicInvoke(response);
                            }
                            else
                            {
                                isValid = step.ValidationAction();
                            }

                            stepResult.Status = isValid ? TestStatus.Passed : TestStatus.Failed;

                            if (!isValid)
                            {
                                stepResult.ErrorMessage = "验证失败";
                                OnLogMessage($"工位 {workstation.Name} 步骤 {step.Name} 验证失败，响应: {response}");
                            }
                        }
                        else
                        {
                            stepResult.Status = TestStatus.Passed;
                        }
                    }
                    else
                    {
                        throw new Exception("下位机未连接");
                    }
                }
                else
                {
                    // 没有执行动作，直接通过
                    stepResult.Status = TestStatus.Passed;
                }
            }
            catch (Exception ex)
            {
                stepResult.Status = TestStatus.Failed;
                stepResult.ErrorMessage = ex.Message;
                OnLogMessage($"工位 {workstation.Name} 步骤 {step.Name} 执行失败: {ex.Message}");
            }
            finally
            {
                stopwatch.Stop();
                stepResult.ExecutionTime = stopwatch.Elapsed;
            }

            return stepResult;
        }

        private string GenerateCommandFromStep(TestStep step)
        {
            // 根据步骤生成相应的下位机命令
            // 这里可以根据实际协议实现
            return step.Name switch
            {
                string s when s.Contains("启动") => "START_TEST",
                string s when s.Contains("停止") => "STOP_TEST",
                string s when s.Contains("读取") => "READ_DATA",
                string s when s.Contains("设置") => "SET_PARAM",
                string s when s.Contains("复位") => "RESET",
                _ => $"CMD:{step.Name.Replace(" ", "_").ToUpper()}"
            };
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            OnLogMessage("测试停止请求已发送");
        }

        private void UpdateWorkStationStatus(WorkStation workstation, WorkStationStatus status)
        {
            workstation.Status = status;
            WorkStationStatusChanged?.Invoke(workstation);
        }

        private void OnLogMessage(string message)
        {
            LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
