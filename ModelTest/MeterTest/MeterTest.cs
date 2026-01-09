using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static ModelTest.MeterTest.MeterTestStatus;

namespace ModelTest.MeterTest
{
    public partial class MeterTest : Form
    {
        public Form OwnerForm { get; set; }//拥有者窗体
       
        // 控件属性
        private TreeView TreeView { get; set; }
        private TabControl TabControl { get; set; }
        private Button BtnStart { get; set; }
        private ProgressBar ProgressBar { get; set; }
        private Label LblStatus { get; set; }
        private RichTextBox LogTextBox { get; set; }
        private PropertyGrid DetailPropertyGrid { get; set; }
        /// <summary>
        /// 表位控件属性
        /// </summary>

        private WorkStationConfiguration _workStationConfig;
        private MultiWorkStationTestEngine _multiStationEngine;//多表位测试引擎
        private List<WorkStation> _availableWorkStations = new List<WorkStation>();
        private DataGridView _dgvWorkStations;
        private Panel _workStationPanel;
        private TestExecutionEngine _executionEngine;
        private TestPlan _currentTestPlan;
        private Dictionary<string, TreeNode> _nodeMap = new Dictionary<string, TreeNode>();
        // 事件处理方法
        private void OnWorkStationStatusChanged(WorkStation workstation)
        {
            if (_dgvWorkStations.InvokeRequired)
            {
                _dgvWorkStations.Invoke(new Action(() =>
                    OnWorkStationStatusChanged(workstation)));
                return;
            }

            // 更新对应行的状态显示
            foreach (DataGridViewRow row in _dgvWorkStations.Rows)
            {
                if (row.Tag is WorkStation ws && ws.Id == workstation.Id)
                {
                    row.Cells["Status"].Value = workstation.Status.ToString();
                    row.DefaultCellStyle.BackColor = GetStatusColor(workstation.Status);

                    if (workstation.TestResult != null)
                    {
                        row.Cells["TestResult"].Value = workstation.TestResult.OverallStatus.ToString();
                    }

                    break;
                }
            }
        }

        private void OnTestResultUpdated(TestResult result)
        {
            OnLogMessage($"表位 {result.WorkStationName} 测试完成: {result.OverallStatus}");
        }

        private void OnProgressChanged(int completed, int total)
        {
            if (ProgressBar.InvokeRequired)
            {
                ProgressBar.Invoke(new Action(() => OnProgressChanged(completed, total)));
                return;
            }

            if (total > 0)
            {
                ProgressBar.Value = (int)((double)completed / total * 100);
                LblStatus.Text = $"表位测试进度: {completed}/{total}";
            }
        }
        public MeterTest()
        {
            InitializeComponent();
            InitializeWorkStationConfiguration(); // 新增表位信息
            MeterTest_Load();
            // 订阅关闭事件
            this.FormClosed += (s, e) =>
            {
                // 关闭时显示主窗体
                OwnerForm?.Show();
            };
            InitializeTestExecutionEngine();
            InitializeSampleTestPlan();
            // 添加表位管理页签
            this.Load += (s, e) =>
            {
                if (_currentTestPlan != null)
                {
                    UpdateTestTree();
                }
            };
        }

        private async void BtnTestSelected_Click(object? sender, EventArgs e)
        {
            try
            {
                var selectedWorkStations = _availableWorkStations
                    .Where(ws => ws.IsSelected)
                    .ToList();

                if (selectedWorkStations.Count == 0)
                {
                    MessageBox.Show("请至少选择一个表位", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_currentTestPlan == null)
                {
                    MessageBox.Show("请先创建或选择测试方案", "提示",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                // 禁用按钮，启用停止按钮
                ((Button)sender).Enabled = false;
                _dgvWorkStations.Enabled = false;

                OnLogMessage($"开始多表位测试，选择 {selectedWorkStations.Count} 个表位");

                // 执行测试
                var results = await _multiStationEngine.ExecuteTestPlanAsync(
                    _currentTestPlan,
                    selectedWorkStations
                );

                // 显示汇总结果
                ShowTestSummary(results);

                // 重新启用控件
                ((Button)sender).Enabled = true;
                _dgvWorkStations.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"测试执行失败: {ex.Message}", "错误",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 显示测试结果汇总
        /// </summary>
        /// <param name="results"></param>
        private void ShowTestSummary(List<TestResult> results)
        {
            var summaryDialog = new Form
            {
                Text = "测试结果汇总",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterParent
            };

            var dgvSummary = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true
            };

            dgvSummary.Columns.Add("WorkStationId", "表位ID");
            dgvSummary.Columns.Add("WorkStationName", "表位名称");
            dgvSummary.Columns.Add("Status", "测试状态");
            dgvSummary.Columns.Add("StartTime", "开始时间");
            dgvSummary.Columns.Add("EndTime", "结束时间");
            dgvSummary.Columns.Add("TotalTime", "总耗时");
            dgvSummary.Columns.Add("PassedCases", "通过用例");
            dgvSummary.Columns.Add("TotalCases", "总用例");
            dgvSummary.Columns.Add("SuccessRate", "成功率");
            dgvSummary.Columns.Add("ErrorMessage", "错误信息");

            foreach (var result in results)
            {
                int rowIndex = dgvSummary.Rows.Add(
                    result.WorkStationId,
                    result.WorkStationName,
                    result.OverallStatus.ToString(),
                    result.StartTime.ToString("HH:mm:ss"),
                    result.EndTime.ToString("HH:mm:ss"),
                    result.TotalTime.ToString(@"hh\:mm\:ss"),
                    result.PassedCases,
                    result.TotalCases,
                    result.TotalCases > 0 ?
                        $"{(double)result.PassedCases / result.TotalCases * 100:F1}%" : "0%",
                    result.ErrorMessage
                );

                // 设置行颜色
                var row = dgvSummary.Rows[rowIndex];
                row.DefaultCellStyle.BackColor = result.OverallStatus == TestStatus.Passed
                    ? Color.LightGreen
                    : Color.LightPink;
            }
            // 添加统计信息
            var statsPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.LightGray,
                Padding = new Padding(10)
            };

            int totalWorkStations = results.Count;
            int passedWorkStations = results.Count(r => r.OverallStatus == TestStatus.Passed);
            int totalCases = results.Sum(r => r.TotalCases);
            int passedCases = results.Sum(r => r.PassedCases);

            var lblStats = new Label
            {
                Text = $"总计: {totalWorkStations} 个表位 | " +
                       $"通过: {passedWorkStations} 个 | " +
                       $"成功率: {(double)passedWorkStations / totalWorkStations * 100:F1}% | " +
                       $"测试用例: {passedCases}/{totalCases}",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("微软雅黑", 10, FontStyle.Bold)
            };

            statsPanel.Controls.Add(lblStats);

            summaryDialog.Controls.Add(dgvSummary);
            summaryDialog.Controls.Add(statsPanel);

            summaryDialog.ShowDialog();
        }
        private void SelectAllWorkStations(bool select)
        {
            foreach (var ws in _availableWorkStations)
            {
                ws.IsSelected = select;
            }
            UpdateWorkStationGrid();
        }
        /// <summary>
        /// 更新表位列表显示
        /// </summary>
        private void UpdateWorkStationGrid()
        {
            _dgvWorkStations.Rows.Clear();

            foreach (var ws in _availableWorkStations)
            {
                int rowIndex = _dgvWorkStations.Rows.Add(
                    ws.IsSelected,
                    ws.Name,
                    ws.Id,
                    ws.IPAddress,
                    ws.Port,
                    ws.Status.ToString(),
                    ws.TestResult?.OverallStatus.ToString() ?? "未测试"
                );

                // 根据状态设置行颜色
                var row = _dgvWorkStations.Rows[rowIndex];
                row.DefaultCellStyle.BackColor = GetStatusColor(ws.Status);

                // 保存表位引用
                row.Tag = ws;
            }
            // 更新选择列的事件
            var selectedColumn = _dgvWorkStations.Columns["Selected"];
            if (selectedColumn is DataGridViewCheckBoxColumn checkColumn)
            {
                checkColumn.ValueType = typeof(bool);
            }

            _dgvWorkStations.CellValueChanged += (s, e) =>
            {
                if (e.ColumnIndex == 0 && e.RowIndex >= 0) // Selected列
                {
                    var row = _dgvWorkStations.Rows[e.RowIndex];
                    if (row.Tag is WorkStation ws)
                    {
                        ws.IsSelected = (bool)row.Cells[0].Value;
                    }
                }
            };
        }
        private Color GetStatusColor(WorkStationStatus status)
        {
            return status switch
            {
                WorkStationStatus.Idle => Color.LightGray,
                WorkStationStatus.Busy => Color.LightBlue,
                WorkStationStatus.Testing => Color.LightYellow,
                WorkStationStatus.Completed => Color.LightGreen,
                WorkStationStatus.Error => Color.LightPink,
                WorkStationStatus.Disabled => Color.Gray,
                _ => Color.White
            };
        }

        /// <summary>
        /// 表位信息
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        private void InitializeWorkStationConfiguration()
        {
            // 创建表位配置
            _workStationConfig = new WorkStationConfiguration
            {
                EnableParallelExecution = true,
                MaxParallelWorkStations = 5,
                StopOnFirstFailure = false,
                RetryOnFailure = true,
                MaxRetryCount = 3
            };

            // 初始化20个表位
            _availableWorkStations.Clear();
            for (int i = 1; i <= 48; i++)
            {
                _availableWorkStations.Add(new WorkStation
                {
                    Id = i,
                    Name = $"表位 {i:D2}",
                    Description = $"测试表位 {i}",
                    IPAddress = $"192.168.127.{130 + i}",
                    Port = 8888,
                    IsSelected = i <= 5, // 默认选择前5个表位
                    Status = WorkStationStatus.Idle
                });
            }

            _workStationConfig.WorkStations = _availableWorkStations;
            // 初始化多表位测试引擎
            _multiStationEngine = new MultiWorkStationTestEngine(_workStationConfig);
            _multiStationEngine.LogMessage += OnLogMessage;
            _multiStationEngine.WorkStationStatusChanged += OnWorkStationStatusChanged;
            _multiStationEngine.TestResultUpdated += OnTestResultUpdated;
            _multiStationEngine.ProgressChanged += OnProgressChanged;

        }

        /// <summary>
        /// 窗体加载
        /// </summary>
        private void MeterTest_Load()
        {

            //设置背景颜色58957f
            this.BackColor = Color.FromArgb(88, 149, 127);
            //设置测试名字
            this.Text = "习承电能表检测DEMO";
            //设置窗体大小1920x1080
            this.Size = new Size(1920, 1080);
            //设置窗体居中显示
            this.StartPosition = FormStartPosition.CenterScreen;
            // 分割容器
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.FromArgb(88, 149, 127)
            };
            // 左侧面板 - 测试树
            var leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 600,
                BackColor = Color.FromArgb(88, 149, 127)
            };
            // #ECECE7  236, 236, 231
            var treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                CheckBoxes = true,
                ImageList = CreateImageList(),
                BackColor = Color.FromArgb(236, 236, 231),
                Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold)
            };
            // 中间面板 -测试表位
            var Centerpanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(88, 149, 127)
            };
            // 表位配置区域
            var configGroup = new GroupBox
            {
                Text = "表位配置",
                Dock = DockStyle.Top,
                Height = 120,
                Padding = new Padding(10)
            };
            var flowConfig = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true
            };
            // 并行执行配置
            var chkParallel = new CheckBox
            {
                Text = "启用并行执行",
                Dock = DockStyle.Left,
                Checked = _workStationConfig.EnableParallelExecution,
                Width = 200
            };
            chkParallel.CheckedChanged += (s, e) =>
                _workStationConfig.EnableParallelExecution = chkParallel.Checked;

            var lblMaxParallel = new Label
            {
                Text = "最大并行数:",
                Width = 120,
                Dock = DockStyle.Left,
                Padding = new Padding(0, 4, 0, 0)
            };
            var numMaxParallel = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 9999,
                Value = _workStationConfig.MaxParallelWorkStations,
                Width = 100
            };
            numMaxParallel.ValueChanged += (s, e) =>
                _workStationConfig.MaxParallelWorkStations = (int)numMaxParallel.Value;

            var chkStopOnFailure = new CheckBox
            {
                Text = "首次失败停止",
                Checked = _workStationConfig.StopOnFirstFailure,
                Width = 200,
                Dock = DockStyle.Left
            };
            chkStopOnFailure.CheckedChanged += (s, e) =>
                _workStationConfig.StopOnFirstFailure = chkStopOnFailure.Checked;

            var chkRetryOnFailure = new CheckBox
            {
                Text = "失败重试",
                Checked = _workStationConfig.RetryOnFailure,
                Width = 200,
                Dock = DockStyle.Left
            };
            chkRetryOnFailure.CheckedChanged += (s, e) =>
                _workStationConfig.RetryOnFailure = chkRetryOnFailure.Checked;

            var lblMaxRetry = new Label
            {
                Dock = DockStyle.Left,
                Padding = new Padding(0, 4, 0, 0),
                Text = "最大重试次数:",
                Width = 120
            };
            var numMaxRetry = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 10,
                Value = _workStationConfig.MaxRetryCount,
                Width = 60,
                Dock = DockStyle.Left
            };
            numMaxRetry.ValueChanged += (s, e) =>
                _workStationConfig.MaxRetryCount = (int)numMaxRetry.Value;

            flowConfig.Controls.AddRange(new Control[]
            {
            chkParallel, lblMaxParallel, numMaxParallel,
            chkStopOnFailure, chkRetryOnFailure, lblMaxRetry, numMaxRetry
            });

            configGroup.Controls.Add(flowConfig);

            // 表位列表区域
            var listGroup = new GroupBox
            {
                Text = "表位列表 (选择要测试的表位)",
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            _dgvWorkStations = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                ForeColor = Color.FromArgb(88, 149, 127),
                BackgroundColor = Color.FromArgb(88, 149, 127)

            };

            // 配置列
            _dgvWorkStations.Columns.Add("Selected", "选择");
            _dgvWorkStations.Columns["Selected"].Width = 100;
            _dgvWorkStations.Columns["Selected"].ValueType = typeof(bool);

            _dgvWorkStations.Columns.Add("Name", "表位名称");
            _dgvWorkStations.Columns["Name"].Width = 100;
            _dgvWorkStations.Columns["Name"].ReadOnly = true;

            _dgvWorkStations.Columns.Add("Id", "样品编号");
            _dgvWorkStations.Columns["Id"].Width = 300;

            _dgvWorkStations.Columns.Add("IPAddress", "IP地址");
            _dgvWorkStations.Columns["IPAddress"].Width = 300;

            _dgvWorkStations.Columns.Add("Port", "端口");
            _dgvWorkStations.Columns["Port"].Width = 150;

            _dgvWorkStations.Columns.Add("Status", "表位状态");
            _dgvWorkStations.Columns["Status"].Width = 200;
            _dgvWorkStations.Columns["Status"].ReadOnly = true;

            _dgvWorkStations.Columns.Add("TestResult", "测试结果");
            _dgvWorkStations.Columns["TestResult"].Width = 150;
            _dgvWorkStations.Columns["TestResult"].ReadOnly = true;

            // 绑定数据
            UpdateWorkStationGrid();

            listGroup.Controls.Add(_dgvWorkStations);
            //listGroup.Controls.Add(buttonPanel);

            Centerpanel.Controls.Add(listGroup);
            Centerpanel.Controls.Add(configGroup);

            leftPanel.Controls.Add(treeView);
            // 右侧面板 - 多页签
            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(88, 149, 127)
            };
            // 执行控制面板
            var executionTab = CreateExecutionTab();
            tabControl.TabPages.Add(executionTab);

            // 日志面板
            var logTab = CreateLogTab();
            tabControl.TabPages.Add(logTab);

            // 详细面板
            var detailTab = CreateDetailTab();
            tabControl.TabPages.Add(detailTab);
            // 设置列比例
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20)); // 左侧30%
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50)); // 中间40%
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // 右侧30%

            tableLayout.Controls.Add(leftPanel);
            tableLayout.Controls.Add(Centerpanel);
            tableLayout.Controls.Add(tabControl);

            this.Controls.Add(tableLayout);

            // 保存控件引用
            TreeView = treeView;
            TabControl = tabControl;
        }

        private void InitializeSampleTestPlan()
        {
            _currentTestPlan = CreateTestPlan.MeterCreateTestPlan();
            UpdateTestTree();
        }
        /// <summary>
        /// 更新测试结果树
        /// </summary>
        private void UpdateTestTree()
        {
            try
            {
                TreeView.Nodes.Clear();
                _nodeMap.Clear();

                var planNode = new TreeNode(_currentTestPlan.Name)
                {
                    Tag = _currentTestPlan,
                    ImageKey = "Plan",
                    SelectedImageKey = "Plan"
                };

                foreach (var testCase in _currentTestPlan.TestCases)
                {
                    var caseNode = new TreeNode($"[{(testCase.IsEnabled ? "✓" : "✗")}] ★{testCase.Name}")
                    {
                        Tag = testCase,
                        ImageKey = "Case",
                        SelectedImageKey = "Case"
                    };

                    foreach (var step in testCase.Steps)
                    {
                        var stepNode = new TreeNode(step.Name)
                        {
                            Tag = step,
                            ImageKey = "Step",
                            SelectedImageKey = "Step"
                        };
                        caseNode.Nodes.Add(stepNode);
                        _nodeMap[step.Id] = stepNode;
                    }

                    planNode.Nodes.Add(caseNode);
                    _nodeMap[testCase.Id] = caseNode;
                }

                TreeView.Nodes.Add(planNode);
                _nodeMap[_currentTestPlan.Id] = planNode;
                planNode.ExpandAll();
            }
            catch (Exception ex)
            {
                ModelTest.LogMessage.Error(ex);
            }

        }
        private ImageList CreateImageList()
        {
            var imageList = new ImageList();
            imageList.Images.Add("Plan", SystemIcons.Information);
            imageList.Images.Add("Case", SystemIcons.Question);
            imageList.Images.Add("Step", SystemIcons.WinLogo);
            return imageList;
        }
        private TabPage CreateExecutionTab()
        {
            var tabPage = new TabPage("执行控制");

            var flowPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.FromArgb(88, 149, 127)
            };

            // 控制按钮
            var btnExport = new Button
            {
                Text = "导出报告",
                Size = new Size(150, 50)
            };
            btnExport.Click += BtnExport_Click;
            // 状态标签 进度
            var lblStatus = new Label
            {
                Text = "就绪",
                Size = new Size(280, 30),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 进度条
            var progressBar = new ProgressBar
            {
                Size = new Size(500, 30),
                Style = ProgressBarStyle.Continuous
            };
            var btnSelectAll = new Button
            {
                Text = "全选",
                Size = new Size(200, 75)
            };
            btnSelectAll.Click += (s, e) => SelectAllWorkStations(true);

            var btnSelectNone = new Button
            {
                Text = "全不选",
                Size = new Size(200, 75)
            };
            btnSelectNone.Click += (s, e) => SelectAllWorkStations(false);

            var btnTestSelected = new Button
            {
                Text = "开始测试",
                Size = new Size(200, 75),
                BackColor = Color.LightGreen
            };
            btnTestSelected.Click += BtnTestSelected_Click;

            var btnStopTest = new Button
            {
                Text = "停止测试",
                Size = new Size(200, 75),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            btnStopTest.Click += (s, e) => _multiStationEngine?.Stop();
            flowPanel.Controls.AddRange(new Control[] {
                btnExport,
                lblStatus,
                progressBar,
                btnSelectAll,
                btnSelectNone,
                btnTestSelected,
                btnStopTest });

            // 保存控件引用
            ProgressBar = progressBar;
            LblStatus = lblStatus;

            tabPage.Controls.Add(flowPanel);
            return tabPage;
        }
        /// <summary>
        /// 导出测试报告
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnExport_Click(object sender, EventArgs e)
        {
            var report = TestReportGenerator.GenerateReport(_currentTestPlan);
            var saveDialog = new SaveFileDialog
            {
                Filter = "HTML文件|*.html|文本文件|*.txt",
                FileName = $"测试报告_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                System.IO.File.WriteAllText(saveDialog.FileName, report);
                MessageBox.Show("报告导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private TabPage CreateLogTab()
        {
            var tabPage = new TabPage("执行日志");

            var textBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Consolas", 10),
                BackColor = Color.Black,
                ForeColor = Color.White
            };

            tabPage.Controls.Add(textBox);
            LogTextBox = textBox;
            return tabPage;
        }
        /// <summary>
        /// 测试详细信息面板
        /// </summary>
        /// <returns></returns>

        private TabPage CreateDetailTab()
        {
            var tabPage = new TabPage("详细信息");

            var propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                ToolbarVisible = false
            };

            tabPage.Controls.Add(propertyGrid);
            DetailPropertyGrid = propertyGrid;
            return tabPage;
        }
        private void InitializeTestExecutionEngine()
        {
            _executionEngine = new TestExecutionEngine();
            _executionEngine.LogMessage += OnLogMessage;
            _executionEngine.TestPlanStarted += OnTestPlanStarted;
            _executionEngine.TestPlanCompleted += OnTestPlanCompleted;
            _executionEngine.TestCaseStarted += OnTestCaseStarted;
            _executionEngine.TestCaseCompleted += OnTestCaseCompleted;
            _executionEngine.TestStepStarted += OnTestStepStarted;
            _executionEngine.TestStepCompleted += OnTestStepCompleted;
        }

        // 事件处理方法
        private void OnLogMessage(string message)
        {
            if (LogTextBox.InvokeRequired)
            {
                LogTextBox.Invoke(new Action(() => OnLogMessage(message)));
                return;
            }

            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToCaret();
        }

        private void OnTestPlanStarted(TestPlan plan)
        {
            UpdateNodeStatus(plan.Id, TestStatus.Running);
        }

        private void OnTestPlanCompleted(TestPlan plan)
        {
            UpdateNodeStatus(plan.Id, plan.Status);
            LblStatus.Text = $"执行完成 - {plan.Status}";
            ProgressBar.Value = 100;
        }

        private void OnTestCaseStarted(TestCase testCase)
        {
            UpdateNodeStatus(testCase.Id, TestStatus.Running);
        }

        private void OnTestCaseCompleted(TestCase testCase)
        {
            UpdateNodeStatus(testCase.Id, testCase.Status);
        }

        private void OnTestStepStarted(TestStep step)
        {
            UpdateNodeStatus(step.Id, TestStatus.Running);
            DetailPropertyGrid.SelectedObject = step;
        }

        private void OnTestStepCompleted(TestStep step)
        {
            UpdateNodeStatus(step.Id, step.Status);
            UpdateProgress();
        }

        private void UpdateNodeStatus(string id, TestStatus status)
        {
            if (_nodeMap.TryGetValue(id, out var node))
            {
                if (TreeView.InvokeRequired)
                {
                    TreeView.Invoke(new Action(() => UpdateNodeStatus(id, status)));
                    return;
                }

                var prefix = status switch
                {
                    TestStatus.Running => "▶",
                    TestStatus.Passed => "✓",
                    TestStatus.Failed => "✗",
                    TestStatus.Skipped => "➤",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(prefix))
                {
                    node.Text = $"[{prefix}] {node.Text.Substring(node.Text.IndexOf(']') + 2)}";
                }

                node.ForeColor = status switch
                {
                    TestStatus.Passed => Color.FromArgb(88, 149, 127),
                    TestStatus.Failed => Color.Red,
                    TestStatus.Running => Color.Blue,
                    _ => Color.Black
                };
            }
        }
        private void UpdateProgress()
        {
            var totalSteps = 0;
            var completedSteps = 0;

            foreach (var testCase in _currentTestPlan.TestCases)
            {
                if (!testCase.IsEnabled) continue;

                foreach (var step in testCase.Steps)
                {
                    totalSteps++;
                    if (step.Status == TestStatus.Passed || step.Status == TestStatus.Failed)
                    {
                        completedSteps++;
                    }
                }
            }

            if (totalSteps > 0)
            {
                var progress = (int)((double)completedSteps / totalSteps * 100);
                ProgressBar.Value = progress;
            }
        }
    }
}
