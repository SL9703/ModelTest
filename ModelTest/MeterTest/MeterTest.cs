using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelTest.MeterTest
{
    public partial class MeterTest : Form
    {
        private readonly Dictionary<string, Label> hardwareValueLabels = new();
        private readonly MeterTestConfigService configService = new();
        private readonly MeterTestExecutor meterTestExecutor = new();
        private readonly string configFilePath;
        private MeterTestPlanConfig meterTestPlanConfig = new();
        private CancellationTokenSource? executionCts;

        public MeterTest()
        {
            InitializeComponent();
            InitializeHardwareCollectionGrid();
            configFilePath = GetMeterTestConfigPath();
            meterTestExecutor.SendAndReceiveAsync = SimulateSendAndReceiveAsync;
            BindEvents();
            LoadMeterTestPlanConfig();
            LoadHeaderLogo();
            LoadOperationButtonImages();
        }

        private void BindEvents()
        {
            btnStartTest.Click += async (_, _) => await StartSelectedTestAsync();
            btnStopTest.Click += (_, _) => CancelRunningTest();
            btnTestPlan.Click += (_, _) => LoadMeterTestPlanConfig();
            schemeTreeView.AfterSelect += (_, _) => UpdateStartButtonText();
        }

        private void LoadMeterTestPlanConfig()
        {
            meterTestPlanConfig = configService.LoadOrCreate(configFilePath);
            BuildSchemeTree();
            AddProcessLog("系统", "配置加载", true, $"已加载配置：{configFilePath}", 0);
        }

        private void BuildSchemeTree()
        {
            schemeTreeView.BeginUpdate();
            schemeTreeView.Nodes.Clear();

            foreach (MeterTestScheme scheme in meterTestPlanConfig.Schemes)
            {
                TreeNode schemeNode = new(scheme.Name)
                {
                    Tag = scheme
                };

                foreach (MeterTestItem testItem in scheme.TestItems)
                {
                    TreeNode itemNode = new(testItem.Name)
                    {
                        Tag = testItem
                    };

                    foreach (MeterTestSubItem subItem in testItem.TestSubItems)
                    {
                        itemNode.Nodes.Add(new TreeNode(subItem.Name)
                        {
                            Tag = subItem
                        });
                    }

                    schemeNode.Nodes.Add(itemNode);
                }

                schemeTreeView.Nodes.Add(schemeNode);
            }

            schemeTreeView.ExpandAll();

            if (schemeTreeView.Nodes.Count > 0)
            {
                schemeTreeView.SelectedNode = schemeTreeView.Nodes[0];
            }

            schemeTreeView.EndUpdate();
            UpdateStartButtonText();
        }

        private async Task StartSelectedTestAsync()
        {
            if (executionCts is not null)
            {
                MessageBox.Show("当前有测试正在执行，请先停止或等待结束。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TreeNode? selectedNode = schemeTreeView.SelectedNode;
            if (selectedNode?.Tag is null)
            {
                MessageBox.Show("请先在方案树中选择要执行的方案、测试项或测试小项。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            executionCts = new CancellationTokenSource();
            btnStartTest.Enabled = false;
            btnStopTest.Enabled = true;

            try
            {
                List<MeterTestExecutionResult> results = new();

                switch (selectedNode.Tag)
                {
                    case MeterTestScheme scheme:
                        AddProcessLog(scheme.Name, "方案启动", true, "开始执行整套方案。", 0);
                        results.AddRange(await meterTestExecutor.ExecuteSchemeAsync(scheme, executionCts.Token));
                        break;
                    case MeterTestItem testItem:
                        if (selectedNode.Parent?.Tag is not MeterTestScheme parentScheme)
                        {
                            throw new InvalidOperationException("测试项未找到所属方案。");
                        }

                        AddProcessLog(parentScheme.Name, testItem.Name, true, "开始执行测试项。", 0);
                        results.AddRange(await meterTestExecutor.ExecuteItemAsync(parentScheme.Name, testItem, executionCts.Token));
                        break;
                    case MeterTestSubItem subItem:
                        if (selectedNode.Parent?.Tag is not MeterTestItem parentItem ||
                            selectedNode.Parent.Parent?.Tag is not MeterTestScheme parentSchemeOfSubItem)
                        {
                            throw new InvalidOperationException("测试小项层级不完整。");
                        }

                        AddProcessLog(parentSchemeOfSubItem.Name, subItem.Name, true, "开始执行测试小项。", 0);
                        results.Add(await meterTestExecutor.ExecuteSubItemAsync(parentSchemeOfSubItem.Name, parentItem.Name, subItem, executionCts.Token));
                        break;
                }

                foreach (MeterTestExecutionResult result in results)
                {
                    AddExecutionResult(result);
                }
            }
            catch (Exception ex)
            {
                AddProcessLog("系统", "执行异常", false, ex.Message, 0);
            }
            finally
            {
                executionCts.Dispose();
                executionCts = null;
                btnStartTest.Enabled = true;
                btnStopTest.Enabled = false;
                UpdateStartButtonText();
            }
        }

        private void CancelRunningTest()
        {
            executionCts?.Cancel();
        }

        private void UpdateStartButtonText()
        {
            btnStartTest.Text = schemeTreeView.SelectedNode?.Tag switch
            {
                MeterTestScheme => "执行方案",
                MeterTestItem => "执行测试项",
                MeterTestSubItem => "执行测试小项",
                _ => "开始测试"
            };
        }

        private void AddExecutionResult(MeterTestExecutionResult result)
        {
            AddProcessLog(
                $"{result.SchemeName}/{result.TestItemName}",
                result.TestSubItemName,
                result.Passed,
                $"{result.Message} 应答：{result.Response}",
                result.ElapsedMilliseconds);
        }

        private void AddProcessLog(string scope, string testName, bool passed, string message, long elapsedMilliseconds)
        {
            processGrid.Rows.Add(
                processGrid.Rows.Count + 1,
                $"{scope} - {testName}",
                passed ? "合格" : "不合格",
                $"{DateTime.Now:HH:mm:ss} / {elapsedMilliseconds} ms");

            if (processGrid.Rows.Count > 0)
            {
                DataGridViewRow row = processGrid.Rows[processGrid.Rows.Count - 1];
                row.Cells[colProcessResult.Index].Style.ForeColor = passed ? Color.FromArgb(22, 101, 52) : Color.Red;
                row.Cells[colProcessItem.Index].ToolTipText = message;
                row.Cells[colProcessTime.Index].ToolTipText = message;
            }
        }

        private async Task<string?> SimulateSendAndReceiveAsync(MeterTestSubItem subItem, CancellationToken cancellationToken)
        {
            int simulatedDelay = Math.Min(Math.Max(100, subItem.TimeoutMs / 3), 1200);
            await Task.Delay(simulatedDelay, cancellationToken);
            return string.IsNullOrWhiteSpace(subItem.MockResponse)
                ? subItem.ExpectedResponse
                : subItem.MockResponse;
        }

        private void InitializeHardwareCollectionGrid()
        {
            string[][] metricRows =
            {
                new[] { "Ua", "Ia", "Pa", "Qa", "Sa", "Pfa", "Φa", "ΣS" },
                new[] { "Ub", "Ib", "Pb", "Qb", "Sb", "Pfb", "Φb", "ΣP" },
                new[] { "Uc", "Ic", "Pc", "Qc", "Sc", "Pfc", "Φc", "ΣQ" }
            };

            Color[] phaseColors =
            {
                Color.FromArgb(255, 235, 59),
                Color.Lime,
                Color.Red
            };

            hardwareLayout.SuspendLayout();
            hardwareLayout.Controls.Clear();
            hardwareLayout.ColumnStyles.Clear();
            hardwareLayout.RowStyles.Clear();

            for (int column = 0; column < 8; column++)
            {
                hardwareLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 12.5F));
            }

            for (int row = 0; row < 3; row++)
            {
                hardwareLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333F));
            }

            for (int row = 0; row < metricRows.Length; row++)
            {
                for (int column = 0; column < metricRows[row].Length; column++)
                {
                    Control metricCell = CreateHardwareMetricCell(metricRows[row][column], phaseColors[row]);
                    hardwareLayout.Controls.Add(metricCell, column, row);
                }
            }

            hardwareLayout.ResumeLayout();
        }

        private Control CreateHardwareMetricCell(string metricName, Color metricColor)
        {
            Panel container = new()
            {
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Margin = new Padding(3),
                Padding = new Padding(2),
                BackColor = Color.FromArgb(95, 156, 135)
            };

            TableLayoutPanel cellLayout = new()
            {
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            cellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54F));
            cellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label titleLabel = new()
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = metricColor,
                Text = metricName,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(2, 0, 0, 0),
                Margin = new Padding(0)
            };

            Label valueLabel = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
                ForeColor = Color.FromArgb(31, 41, 55),
                Text = "000.0000",
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(2, 0, 0, 0)
            };

            hardwareValueLabels[metricName] = valueLabel;
            cellLayout.Controls.Add(titleLabel, 0, 0);
            cellLayout.Controls.Add(valueLabel, 1, 0);
            container.Controls.Add(cellLayout);
            return container;
        }

        public void UpdateHardwareMetric(string metricName, string value)
        {
            if (!hardwareValueLabels.TryGetValue(metricName, out Label? valueLabel))
                return;

            valueLabel.Text = string.IsNullOrWhiteSpace(value) ? "000.0000" : value;
        }

        private void LoadHeaderLogo()
        {
            foreach (string path in GetPngCandidates("xckj.png"))
            {
                if (!File.Exists(path))
                    continue;

                picLogo.Image = Image.FromFile(path);
                return;
            }
        }

        private void LoadOperationButtonImages()
        {
            SetButtonImage(btnStartTest, "startTest.png");
            SetButtonImage(btnStopTest, "StopTest.png");
            SetButtonImage(btnTestPlan, "TestPlan.png");
            SetButtonImage(btnAssetInfo, "资产.png");
        }

        private void SetButtonImage(Button button, string fileName)
        {
            foreach (string path in GetPngCandidates(fileName))
            {
                if (!File.Exists(path))
                    continue;

                using Image source = Image.FromFile(path);
                button.Image = new Bitmap(source, new Size(24, 24));
                return;
            }
        }

        private static string[] GetPngCandidates(string fileName)
        {
            return new[]
            {
                Path.Combine(AppContext.BaseDirectory, "png", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "png", fileName)
            };
        }

        private static string GetMeterTestConfigPath()
        {
            string outputConfigPath = Path.Combine(AppContext.BaseDirectory, "MeterTest", "config", "MeterTestPlanConfig.xml");
            if (File.Exists(outputConfigPath))
            {
                return outputConfigPath;
            }

            return Path.Combine(AppContext.BaseDirectory, "config", "MeterTestPlanConfig.xml");
        }
    }
}
