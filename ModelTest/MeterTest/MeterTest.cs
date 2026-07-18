using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ModelTest.Tools;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// MeterTest 主窗体。
    /// 负责方案树加载、工位/资产表维护、测试执行、结果回填和日志输出。
    /// </summary>
    public partial class MeterTest : Form
    {
        private const int MaxStationCount = 20;
        private const string ReadMeterAddressTestName = "读取表位地址";
        private const string BroadcastReadAddressFrame = "68 17 00 43 05 AA AA AA AA AA AA 10 2B 3A 05 01 71 40 01 02 00 00 C7 C2 16";
        private const string DefaultStationIp = "127.0.0.1";
        private const string StationLogSeparator = "-----------------------------------------------------------------";
        private const int DefaultStationStartPort = 4001;
        private const byte MeterFrameStartV1 = 0x55;
        private const byte MeterFrameStopV1 = 0xAA;
        private const byte MeterFrameStartV2A = 0x55;
        private const byte MeterFrameStartV2B = 0x44;
        private const byte MeterFrameStopV2A = 0xAA;
        private const byte MeterFrameStopV2B = 0xBB;
        private const byte MeterDirectionPcToMcu = 0x00;
        private const byte MeterDirectionMcuToPc = 0x01;
        private const byte MeterControlProtocolV1 = 0x00;
        private const byte MeterControlProtocolV2 = 0x02;
        private const byte MeterDailyTimingCommand = 0x36;
        private const byte DailyTimingStartDataItem = 0x00;
        private const byte DailyTimingResultDataItem = 0xAA;
        private readonly Dictionary<string, Label> hardwareValueLabels = new();
        private readonly MeterTestConfigService configService = new();
        private readonly MeterTestStationConfigService stationConfigService = new();
        private readonly MeterTestAccessDatabaseService accessDatabaseService = new();
        private readonly MeterTestSourceControlService sourceControlService = new();
        private readonly string configFilePath;
        private readonly string stationConfigFilePath;
        private MeterTestPlanConfig meterTestPlanConfig = new();
        private CancellationTokenSource? executionCts;
        private readonly Dictionary<StationResultKey, StationDisplayState> stationResultCache = new();
        private string currentRunId = Guid.NewGuid().ToString("N");
        private bool isUpdatingStationSelection;
        private bool isLoadingStationConfig;
        private bool isLoadingMeterArchive;
        private MeterTestGridViewMode currentGridViewMode = MeterTestGridViewMode.TestPlan;

        public MeterTest()
        {
            InitializeComponent();
            configFilePath = GetMeterTestConfigPath();
            stationConfigFilePath = GetMeterTestStationConfigPath();
            ConfigureDataGridViewSorting();
            InitializeStationProcessGrid();
            InitializeHardwareCollectionGrid();
            BindEvents();
            accessDatabaseService.EnsureInitialized();
            LoadMeterArchivesToGrid();
            SaveStationCommunicationConfig();
            LoadMeterTestPlanConfig();
            LoadHeaderLogo();
            LoadOperationButtonImages();
            ApplyTestPlanView();
        }

        /// <summary>
        /// 绑定窗体事件。
        /// 这里统一把按钮、表格、方案树的交互行为连起来。
        /// </summary>
        private void BindEvents()
        {
            btnStartTest.Click += async (_, _) => await StartSelectedTestAsync();
            btnStopTest.Click += (_, _) => CancelRunningTest();
            btnTestPlan.Click += (_, _) => RefreshTestPlanAndMeterArchive();
            btnAssetInfo.Click += (_, _) => RefreshMeterArchiveDisplay();
            btnSaveAssetInfo.Click += (_, _) => SaveAllAssetInfo();
            btnBatchApplyAssetInfo.Click += (_, _) => BatchApplyFirstStationAssetInfo();
            btnSelectAllStations.Click += (_, _) => SetAllStationSelection(true);
            btnClearStationSelection.Click += (_, _) => SetAllStationSelection(false);
            rbSingleStation.CheckedChanged += (_, _) => ApplySingleStationSelectionRule();
            stationGrid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (stationGrid.IsCurrentCellDirty)
                {
                    stationGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            stationGrid.CellValueChanged += (_, e) =>
            {
                if (!isUpdatingStationSelection && e.RowIndex >= 0 && e.ColumnIndex == colStationSelected.Index)
                {
                    bool isSelected = Convert.ToBoolean(stationGrid.Rows[e.RowIndex].Cells[colStationSelected.Index].Value ?? false);
                    if (isSelected)
                    {
                        ApplySingleStationSelectionRule(e.RowIndex);
                    }
                }

                if (!isLoadingStationConfig && e.RowIndex >= 0 &&
                    (e.ColumnIndex == colStationIp.Index || e.ColumnIndex == colStationPort.Index))
                {
                    SaveStationCommunicationConfig();
                }

                if (!isLoadingMeterArchive && e.RowIndex >= 0 && IsEditableAssetColumn(e.ColumnIndex))
                {
                    SaveMeterArchiveFromRow(stationGrid.Rows[e.RowIndex]);
                }
            };
            stationGrid.CellEndEdit += (_, e) =>
            {
                if (!isLoadingStationConfig && e.RowIndex >= 0 &&
                    (e.ColumnIndex == colStationIp.Index || e.ColumnIndex == colStationPort.Index))
                {
                    SaveStationCommunicationConfig();
                }
            };
            stationGrid.DataError += (_, e) =>
            {
                e.ThrowException = false;
            };
            schemeTreeView.AfterSelect += (_, _) =>
            {
                UpdateStartButtonText();
                if (currentGridViewMode == MeterTestGridViewMode.TestPlan)
                {
                    RestoreStationDisplayForSelectedNode();
                }
            };
        }

        /// <summary>
        /// 切换到测试方案视图并刷新方案与结果缓存。
        /// </summary>
        private void RefreshTestPlanAndMeterArchive()
        {
            ApplyTestPlanView();
            LoadMeterTestPlanConfig();
            RestoreStationDisplayForSelectedNode();
        }

        /// <summary>
        /// 切换到资产信息视图并刷新本地档案。
        /// </summary>
        private void RefreshMeterArchiveDisplay()
        {
            ApplyAssetInfoView();
            LoadMeterArchivesToGrid();
            AddProcessLog("系统", "电表档案刷新", true, "电表档案已从本地数据库刷新到测试过程区域。", 0);
        }

        /// <summary>
        /// 从 XML 加载测试方案配置，并同步控制 PCB 配置到本地数据库。
        /// </summary>
        private void LoadMeterTestPlanConfig()
        {
            meterTestPlanConfig = configService.LoadOrCreate(configFilePath);
            SaveControlPcbConfigToAccess();
            BuildSchemeTree();
            AddProcessLog("系统", "配置加载", true, $"已加载配置：{configFilePath}", 0);
        }

        /// <summary>
        /// 按配置构建方案树。
        /// TreeView 的层级是：方案 -> 测试项 -> 测试小项。
        /// </summary>
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

        /// <summary>
        /// 开始执行当前选中的方案、测试项或测试小项。
        /// 这是点击“执行方案/执行测试项/执行测试小项”后的统一入口。
        /// </summary>
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
            currentRunId = Guid.NewGuid().ToString("N");
            btnStartTest.Enabled = false;
            btnStopTest.Enabled = true;

            try
            {
                List<SelectedSubItemContext> testContexts = GetSelectedTestContexts(selectedNode);
                List<StationCommunicationConfig> selectedStations = GetSelectedStations();

                if (testContexts.Count == 0)
                {
                    MessageBox.Show("当前选择的方案、测试项或测试小项没有启用的测试内容。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (selectedStations.Count == 0)
                {
                    MessageBox.Show("请至少选择一个工位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                foreach (SelectedSubItemContext context in testContexts)
                {
                    executionCts.Token.ThrowIfCancellationRequested();
                    await ExecuteTestContextAsync(context, selectedStations, executionCts.Token);
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

        /// <summary>
        /// 取消当前测试流程。
        /// </summary>
        private void CancelRunningTest()
        {
            executionCts?.Cancel();
        }

        /// <summary>
        /// 执行一个测试上下文。
        /// 先升源，再按测试模式分发到工位测试或控制 PCB 日计时。
        /// </summary>
        private async Task ExecuteTestContextAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            if (!await TryExecuteSourceControlAsync(context, cancellationToken))
            {
                return;
            }

            if (UsesControlPcbDailyTimingExecution(context.SubItem))
            {
                await ExecuteControlPcbDailyTimingAsync(context, selectedStations, cancellationToken);
                return;
            }

            List<Task> stationTasks = selectedStations
                .Select(station => ExecuteStationSubItemAsync(station, context, cancellationToken))
                .ToList();

            await Task.WhenAll(stationTasks);
        }

        /// <summary>
        /// 在正式测试前执行源控制。
        /// 这里仅负责收集当前工位和资产档案，再交给独立的源控制服务处理。
        /// </summary>
        private async Task<bool> TryExecuteSourceControlAsync(
            SelectedSubItemContext context,
            CancellationToken cancellationToken)
        {
            List<StationCommunicationConfig> selectedStations = GetSelectedStations();
            IReadOnlyDictionary<int, MeterArchiveData> meterArchives = accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
            List<MeterTestStationCommunication> sourceStations = selectedStations
                .Select(station => new MeterTestStationCommunication
                {
                    StationNo = station.StationNo,
                    Ip = station.Ip,
                    Port = station.Port
                })
                .ToList();

            long startTicks = Environment.TickCount64;
            MeterTestSourceControlService.MeterTestSourceControlResult result = await sourceControlService.ExecuteAsync(
                meterTestPlanConfig,
                context.SubItem,
                sourceStations,
                meterArchives,
                cancellationToken);

            RunOnUiThread(() =>
            {
                if (result.StandValues is not null)
                {
                    UpdateHardwareMetricsFromStandValues(result.StandValues);
                }

                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    $"{context.SubItem.Name}-升源",
                    result.Success,
                    result.Message,
                    Math.Max(0, Environment.TickCount64 - startTicks));
            });

            return result.Success;
        }

        private static string DefaultIfEmpty(string value, string defaultValue)
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        /// <summary>
        /// 根据当前树节点动态修改开始按钮文案。
        /// </summary>
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

        /// <summary>
        /// 将执行结果追加到界面过程日志，并在需要时回写表位地址。
        /// </summary>
        private void AddExecutionResult(MeterTestExecutionResult result)
        {
            AddProcessLog(
                $"{result.SchemeName}/{result.TestItemName}",
                result.TestSubItemName,
                result.Passed,
                $"{result.Message} 应答：{result.Response}",
                result.ElapsedMilliseconds);

            if (IsLegacyAddressTestName(result.TestSubItemName))
            {
                UpdateStationAddressResult(1, result.Response, result.Passed);
            }
        }

        /// <summary>
        /// 向过程区表格追加一行日志。
        /// </summary>
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
                row.Height = processGrid.RowTemplate.Height;
                row.MinimumHeight = processGrid.RowTemplate.Height;
                row.Cells[colProcessResult.Index].Style.ForeColor = passed ? Color.FromArgb(22, 101, 52) : Color.Red;
                row.Cells[colProcessItem.Index].ToolTipText = message;
                row.Cells[colProcessTime.Index].ToolTipText = message;
            }
        }

        /// <summary>
        /// 初始化工位表格。
        /// 默认补齐 1-20 工位，并预置通信参数和档案参数。
        /// </summary>
        private void InitializeStationProcessGrid()
        {
            isLoadingStationConfig = true;
            try
            {
                stationGrid.Rows.Clear();
                MeterTestStationConfig config = stationConfigService.LoadOrCreate(
                    stationConfigFilePath,
                    MaxStationCount,
                    DefaultStationIp,
                    DefaultStationStartPort);

                foreach (MeterTestStationCommunication station in config.Stations)
                {
                    stationGrid.Rows.Add(
                        true,
                        station.StationNo,
                        string.IsNullOrWhiteSpace(station.Ip) ? DefaultStationIp : station.Ip,
                        station.Port <= 0 ? DefaultStationStartPort + station.StationNo - 1 : station.Port,
                        ReadMeterAddressTestName,
                        "单相",
                        "直接式",
                        "220V",
                        "5A",
                        "A",
                        "1000",
                        "2.0",
                        "1000",
                        string.Empty,
                        "待测试",
                        string.Empty);
                }

                ApplyFixedGridRowHeight(stationGrid);
            }
            finally
            {
                isLoadingStationConfig = false;
            }
        }

        /// <summary>
        /// 从本地数据库加载电表档案并回填到工位表。
        /// </summary>
        private void LoadMeterArchivesToGrid()
        {
            if (stationGrid.Rows.Count == 0)
                return;

            isLoadingMeterArchive = true;
            try
            {
                Dictionary<int, MeterArchiveData> archives = accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
                foreach (DataGridViewRow row in stationGrid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                    if (!archives.TryGetValue(stationNo, out MeterArchiveData? archive))
                        archive = CreateDefaultMeterArchive(stationNo);

                    ApplyMeterArchiveToRow(row, archive);
                }
            }
            finally
            {
                isLoadingMeterArchive = false;
            }
        }

        /// <summary>
        /// 切换到测试方案视图。
        /// 只显示测试执行需要的列。
        /// </summary>
        private void ApplyTestPlanView()
        {
            currentGridViewMode = MeterTestGridViewMode.TestPlan;
            groupProcess.Text = "测试过程区域";

            rbMultiStation.Visible = true;
            rbSingleStation.Visible = true;
            btnSelectAllStations.Visible = true;
            btnClearStationSelection.Visible = true;
            btnSaveAssetInfo.Visible = false;
            btnBatchApplyAssetInfo.Visible = false;
            processGrid.Visible = true;

            SetProcessLayoutRows(66F, 72F, 28F);
            SetStationColumnVisibility(
                showSelection: true,
                showCommunication: false,
                showAsset: false,
                showTest: true,
                showMeterAddress: true,
                showResult: true);
            ApplyTestPlanColumnWidths();
            SetStationColumnEditState(assetEditable: false);
            RestoreStationDisplayForSelectedNode();
        }

        /// <summary>
        /// 切换到资产信息视图。
        /// 显示工位通信和电表档案可维护列。
        /// </summary>
        private void ApplyAssetInfoView()
        {
            currentGridViewMode = MeterTestGridViewMode.AssetInfo;
            groupProcess.Text = "资产信息维护";

            rbMultiStation.Visible = false;
            rbSingleStation.Visible = false;
            btnSelectAllStations.Visible = false;
            btnClearStationSelection.Visible = false;
            btnSaveAssetInfo.Visible = true;
            btnBatchApplyAssetInfo.Visible = true;
            processGrid.Visible = false;

            SetProcessLayoutRows(66F, 100F, 0F);
            SetStationColumnVisibility(
                showSelection: false,
                showCommunication: true,
                showAsset: true,
                showTest: false,
                showMeterAddress: false,
                showResult: false);
            ApplyAssetInfoColumnWidths();
            SetStationColumnEditState(assetEditable: true);
        }

        /// <summary>
        /// 设置测试过程区域的三行布局比例。
        /// </summary>
        private void SetProcessLayoutRows(float selectorHeight, float stationPercent, float processPercent)
        {
            processLayout.RowStyles[0].SizeType = SizeType.Absolute;
            processLayout.RowStyles[0].Height = selectorHeight;
            processLayout.RowStyles[1].SizeType = SizeType.Percent;
            processLayout.RowStyles[1].Height = stationPercent;
            processLayout.RowStyles[2].SizeType = SizeType.Percent;
            processLayout.RowStyles[2].Height = processPercent;
        }

        /// <summary>
        /// 控制工位表在当前视图下显示哪些列。
        /// </summary>
        private void SetStationColumnVisibility(
            bool showSelection,
            bool showCommunication,
            bool showAsset,
            bool showTest,
            bool showMeterAddress,
            bool showResult)
        {
            colStationSelected.Visible = showSelection;
            colStationNo.Visible = true;
            colStationIp.Visible = showCommunication;
            colStationPort.Visible = showCommunication;
            colStationTestContent.Visible = showTest;
            colMeterType.Visible = showAsset;
            colMeterAccessMode.Visible = showAsset;
            colMeterVoltage.Visible = showAsset;
            colMeterCurrent.Visible = showAsset;
            colMeterActiveClass.Visible = showAsset;
            colMeterActiveConstant.Visible = showAsset;
            colMeterReactiveClass.Visible = showAsset;
            colMeterReactiveConstant.Visible = showAsset;
            colStationMeterAddress.Visible = showMeterAddress;
            colStationResult.Visible = showResult;
            colStationTime.Visible = showResult;
        }

        /// <summary>
        /// 测试方案视图的列宽固定规则。
        /// </summary>
        private void ApplyTestPlanColumnWidths()
        {
            SetFixedColumnWidth(colStationSelected, 100);
            SetFixedColumnWidth(colStationNo, 100);
            SetFixedColumnWidth(colStationTestContent, 400);
            SetFixedColumnWidth(colStationMeterAddress, 300);
            SetFixedColumnWidth(colStationResult, 100);
            SetFixedColumnWidth(colStationTime, 200);
        }

        /// <summary>
        /// 资产信息视图的列宽固定规则。
        /// </summary>
        private void ApplyAssetInfoColumnWidths()
        {
            SetFixedColumnWidth(colStationNo, 100);
            SetFixedColumnWidth(colStationIp, 350);
            SetFixedColumnWidth(colStationPort, 100);
            SetFixedColumnWidth(colMeterType, 200);
            SetFixedColumnWidth(colMeterAccessMode, 200);
            SetFixedColumnWidth(colMeterVoltage, 200);
            SetFixedColumnWidth(colMeterCurrent, 200);
            SetFixedColumnWidth(colMeterActiveClass, 200);
            SetFixedColumnWidth(colMeterActiveConstant, 200);
            SetFixedColumnWidth(colMeterReactiveClass, 200);
            SetFixedColumnWidth(colMeterReactiveConstant, 200);
        }

        /// <summary>
        /// 给指定列固定宽度并禁止用户拖拽。
        /// </summary>
        private static void SetFixedColumnWidth(DataGridViewColumn column, int width)
        {
            column.MinimumWidth = width;
            column.Width = width;
            column.Resizable = DataGridViewTriState.False;
        }

        /// <summary>
        /// 切换列的可编辑状态。
        /// 资产视图允许编辑通信和档案，测试视图只读。
        /// </summary>
        private void SetStationColumnEditState(bool assetEditable)
        {
            colStationSelected.ReadOnly = false;
            colStationNo.ReadOnly = true;
            colStationIp.ReadOnly = !assetEditable;
            colStationPort.ReadOnly = !assetEditable;
            colStationTestContent.ReadOnly = true;
            colMeterType.ReadOnly = !assetEditable;
            colMeterAccessMode.ReadOnly = !assetEditable;
            colMeterVoltage.ReadOnly = !assetEditable;
            colMeterCurrent.ReadOnly = !assetEditable;
            colMeterActiveClass.ReadOnly = !assetEditable;
            colMeterActiveConstant.ReadOnly = !assetEditable;
            colMeterReactiveClass.ReadOnly = !assetEditable;
            colMeterReactiveConstant.ReadOnly = !assetEditable;
            colStationMeterAddress.ReadOnly = true;
            colStationResult.ReadOnly = true;
            colStationTime.ReadOnly = true;
        }

        /// <summary>
        /// 保存全部资产信息。
        /// </summary>
        private void SaveAllAssetInfo()
        {
            SaveAllAssetInfo(showMessage: true);
        }

        /// <summary>
        /// 保存全部资产信息；是否弹出提示由调用方决定。
        /// </summary>
        private void SaveAllAssetInfo(bool showMessage)
        {
            stationGrid.EndEdit();
            SaveStationCommunicationConfig();

            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                SaveMeterArchiveFromRow(row);
            }

            AddProcessLog("系统", "资产信息保存", true, "资产信息已保存到本地数据库。", 0);
            if (showMessage)
            {
                MessageBox.Show("资产信息已保存。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 用 1 工位的参数批量覆盖 2-20 工位。
        /// </summary>
        private void BatchApplyFirstStationAssetInfo()
        {
            stationGrid.EndEdit();
            if (stationGrid.Rows.Count == 0)
                return;

            DataGridViewRow sourceRow = stationGrid.Rows[0];
            isLoadingStationConfig = true;
            isLoadingMeterArchive = true;
            try
            {
                foreach (DataGridViewRow row in stationGrid.Rows)
                {
                    if (row.IsNewRow || row.Index == 0)
                        continue;

                    CopyCellValue(sourceRow, row, colStationIp);
                    CopyCellValue(sourceRow, row, colStationPort);
                    CopyCellValue(sourceRow, row, colMeterType);
                    CopyCellValue(sourceRow, row, colMeterAccessMode);
                    CopyCellValue(sourceRow, row, colMeterVoltage);
                    CopyCellValue(sourceRow, row, colMeterCurrent);
                    CopyCellValue(sourceRow, row, colMeterActiveClass);
                    CopyCellValue(sourceRow, row, colMeterActiveConstant);
                    CopyCellValue(sourceRow, row, colMeterReactiveClass);
                    CopyCellValue(sourceRow, row, colMeterReactiveConstant);
                }
            }
            finally
            {
                isLoadingStationConfig = false;
                isLoadingMeterArchive = false;
            }

            SaveAllAssetInfo(showMessage: false);
            AddProcessLog("系统", "资产批量修改", true, "已按1工位参数批量覆盖2-20工位资产信息。", 0);
            MessageBox.Show("已按1工位参数批量修改2-20工位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 复制同一列在两行之间的值。
        /// </summary>
        private static void CopyCellValue(DataGridViewRow sourceRow, DataGridViewRow targetRow, DataGridViewColumn column)
        {
            targetRow.Cells[column.Index].Value = sourceRow.Cells[column.Index].Value;
        }

        /// <summary>
        /// 把电表档案对象回填到表格行。
        /// </summary>
        private void ApplyMeterArchiveToRow(DataGridViewRow row, MeterArchiveData archive)
        {
            SetComboCellValue(row, colMeterType, archive.MeterType, "单相");
            SetComboCellValue(row, colMeterAccessMode, archive.AccessMode, "直接式");
            row.Cells[colMeterVoltage.Index].Value = DefaultIfEmpty(archive.Voltage, "220V");
            row.Cells[colMeterCurrent.Index].Value = DefaultIfEmpty(archive.Current, "5A");
            SetComboCellValue(row, colMeterActiveClass, archive.ActiveClass, "A");
            row.Cells[colMeterActiveConstant.Index].Value = DefaultIfEmpty(archive.ActiveConstant, "1000");
            SetComboCellValue(row, colMeterReactiveClass, archive.ReactiveClass, "2.0");
            row.Cells[colMeterReactiveConstant.Index].Value = DefaultIfEmpty(archive.ReactiveConstant, "1000");
            row.Cells[colStationMeterAddress.Index].Value = archive.MeterAddress;
        }

        /// <summary>
        /// 把表格行里的电表档案保存回数据库。
        /// </summary>
        private void SaveMeterArchiveFromRow(DataGridViewRow row)
        {
            if (row.IsNewRow)
                return;

            int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
            accessDatabaseService.SaveMeterArchive(new MeterArchiveData(
                stationNo,
                GetCellText(row, colMeterType, "单相"),
                GetCellText(row, colMeterAccessMode, "直接式"),
                GetCellText(row, colMeterVoltage, "220V"),
                GetCellText(row, colMeterCurrent, "5A"),
                GetCellText(row, colMeterActiveClass, "A"),
                GetCellText(row, colMeterActiveConstant, "1000"),
                GetCellText(row, colMeterReactiveClass, "2.0"),
                GetCellText(row, colMeterReactiveConstant, "1000"),
                GetCellText(row, colStationMeterAddress, string.Empty)));
        }

        /// <summary>
        /// 判断当前列是否属于资产维护可编辑列。
        /// </summary>
        private bool IsEditableAssetColumn(int columnIndex)
        {
            return columnIndex == colMeterType.Index ||
                   columnIndex == colMeterAccessMode.Index ||
                   columnIndex == colMeterVoltage.Index ||
                   columnIndex == colMeterCurrent.Index ||
                   columnIndex == colMeterActiveClass.Index ||
                   columnIndex == colMeterActiveConstant.Index ||
                   columnIndex == colMeterReactiveClass.Index ||
                   columnIndex == colMeterReactiveConstant.Index;
        }

        /// <summary>
        /// 创建默认电表档案。
        /// </summary>
        private static MeterArchiveData CreateDefaultMeterArchive(int stationNo)
        {
            return new MeterArchiveData(stationNo, "单相", "直接式", "220V", "5A", "A", "1000", "2.0", "1000", string.Empty);
        }

        /// <summary>
        /// 安全读取单元格文本，空值时回退默认值。
        /// </summary>
        private static string GetCellText(DataGridViewRow row, DataGridViewColumn column, string defaultValue)
        {
            string value = Convert.ToString(row.Cells[column.Index].Value)?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
        }

        /// <summary>
        /// 给 ComboBox 列回填值，必要时自动补充候选项。
        /// </summary>
        private static void SetComboCellValue(
            DataGridViewRow row,
            DataGridViewComboBoxColumn column,
            string value,
            string defaultValue)
        {
            string targetValue = DefaultIfEmpty(value, defaultValue);
            if (!column.Items.Contains(targetValue))
            {
                column.Items.Add(targetValue);
            }

            row.Cells[column.Index].Value = targetValue;
        }

        /// <summary>
        /// 串口或串口服务器收到指定工位返回后，统一调用这里刷新 20 工位表格。
        /// </summary>
        public void UpdateStationAddressResult(int stationNo, string responseHex, bool? passed = null)
        {
            if (stationNo < 1 || stationNo > MaxStationCount)
                return;

            DataGridViewRow row = stationGrid.Rows[stationNo - 1];
            string normalizedResponse = NormalizeHex(responseHex);
            Sgcc698BroadcastAddressParseResult parseResult = SGCCTools.ParseBroadcastAddressResponse(normalizedResponse);
            string meterAddress = parseResult.IsValid
                ? parseResult.MeterAddress
                : string.Empty;
            bool isPassed = passed ?? !string.IsNullOrWhiteSpace(meterAddress);

            row.Cells[colStationTestContent.Index].Value = ReadMeterAddressTestName;
            row.Cells[colStationMeterAddress.Index].Value = meterAddress;
            row.Cells[colStationResult.Index].Value = isPassed ? "合格" : "不合格";
            row.Cells[colStationTime.Index].Value = DateTime.Now.ToString("HH:mm:ss");
            row.Cells[colStationResult.Index].Style.ForeColor = isPassed ? Color.FromArgb(22, 101, 52) : Color.Red;
            SaveMeterArchiveFromRow(row);
        }

        private async Task ExecuteStationSubItemAsync(
            StationCommunicationConfig station,
            SelectedSubItemContext context,
            CancellationToken cancellationToken)
        {
            DateTime startedAt = DateTime.Now;
            long startTicks = Environment.TickCount64;
            string response = string.Empty;
            bool passed = false;
            string message;

            RunOnUiThread(() => UpdateStationRunningState(station.StationNo, context));

            try
            {
                response = await SendStationRequestAsync(station, context, cancellationToken);
                if (UsesSgcc698BroadcastAddressParser(context.SubItem))
                {
                    if (string.IsNullOrWhiteSpace(response))
                    {
                        passed = false;
                        message = "电表无响应";
                        LogStationCommunicationBlock(context.TestItemName, station, message, StationLogSeparator);
                    }
                    else
                    {
                        Sgcc698BroadcastAddressParseResult parseResult = ParseSgcc698BroadcastAddressResponse(context.SubItem, response);
                        passed = parseResult.IsValid;
                        message = parseResult.IsValid ? "电表响应正常" : $"电表响应异常：{parseResult.Message}";
                        LogStationCommunicationBlock(context.TestItemName, station, message, StationLogSeparator);
                    }
                }
                else
                {
                    passed = IsResponseMatched(context.SubItem, response);
                    message = passed ? "应答匹配，测试通过。" : $"应答不匹配，期望：{context.SubItem.ExpectedResponse}，实际：{response}";
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                passed = false;
                message = UsesSgcc698BroadcastAddressParser(context.SubItem)
                    ? "电表无响应"
                    : $"等待超时，超时时间 {context.SubItem.TimeoutMs} ms。";
                LogStationCommunicationBlock(context.TestItemName, station, message, StationLogSeparator);
            }
            catch (StationConnectionException ex)
            {
                passed = false;
                message = ex.Message;
            }
            catch (Exception ex)
            {
                message = $"执行异常：{ex.Message}";
                LogStationCommunicationBlock(context.TestItemName, station, message, StationLogSeparator);
            }

            long elapsed = Math.Max(0, Environment.TickCount64 - startTicks);
            RunOnUiThread(() => ApplyStationExecutionResult(station, context, response, passed));
            RunOnUiThread(() =>
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}/工位{station.StationNo}",
                    context.SubItem.Name,
                    passed,
                    message,
                    elapsed));
        }

        private async Task ExecuteStationTestsAsync(
            StationCommunicationConfig station,
            List<SelectedSubItemContext> testContexts,
            CancellationToken cancellationToken)
        {
            foreach (SelectedSubItemContext context in testContexts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ExecuteStationSubItemAsync(station, context, cancellationToken);
            }
        }

        /// <summary>
        /// 执行控制 PCB 日计时测试。
        /// 先按配置找到启用的控制 PCB 组，再对每组并发执行。
        /// </summary>
        private async Task ExecuteControlPcbDailyTimingAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            if (!TryGetDailyTimingConfig(context.SubItem, out byte testTime, out byte testCount, out int packetIntervalMs))
            {
                RunOnUiThread(() => AddProcessLog(context.SchemeName, context.SubItem.Name, false, "日计时配置不正确。", 0));
                return;
            }

            List<Task> groupTasks = GetEnabledControlPcbGroups(context.SubItem)
                .Select(group => ExecuteControlPcbDailyTimingGroupAsync(group, selectedStations, context, testTime, testCount, packetIntervalMs, cancellationToken))
                .ToList();

            if (groupTasks.Count == 0)
            {
                RunOnUiThread(() => AddProcessLog(context.SchemeName, context.SubItem.Name, false, "未找到可用控制PCB分组，请检查 ControlPcbGroups。", 0));
                return;
            }

            await Task.WhenAll(groupTasks);
        }

        /// <summary>
        /// 执行单个控制 PCB 组的日计时流程。
        /// 流程为：连接 -> 发送开始报文 -> 等待 -> 发送结果获取报文 -> 回填结果。
        /// </summary>
        private async Task ExecuteControlPcbDailyTimingGroupAsync(
            MeterTestControlPcbGroup group,
            List<StationCommunicationConfig> selectedStations,
            SelectedSubItemContext context,
            byte testTime,
            byte testCount,
            int packetIntervalMs,
            CancellationToken cancellationToken)
        {
            List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
            if (targets.Count == 0)
                return;

            long startTicks = Environment.TickCount64;
            foreach (ControlPcbStationTarget target in targets)
            {
                RunOnUiThread(() => UpdateStationRunningState(target.StationNo, context));
            }

            LogControlPcbGroupBlock(
                context.TestItemName,
                group,
                targets,
                StationLogSeparator,
                $" 准备连接控制PCB：{group.Ip}:{group.Port}，测试内容={context.SubItem.Name}，工位={string.Join(",", targets.Select(x => x.StationNo))}",
                StationLogSeparator);

            using TcpClient client = new();
            using CancellationTokenSource connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectCts.CancelAfter(Math.Max(100, context.SubItem.TimeoutMs));

            try
            {
                await client.ConnectAsync(group.Ip, group.Port, connectCts.Token);
            }
            catch (Exception ex)
            {
                string message = $"控制PCB连接失败：{group.Ip}:{group.Port}";
                LogControlPcbGroupBlock(context.TestItemName, group, targets, message, StationLogSeparator);
                RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, false, message, string.Empty));
                RunOnUiThread(() => AddProcessLog($"{context.SchemeName}/{context.TestItemName}/{group.Name}", context.SubItem.Name, false, ex.Message, 0));
                return;
            }

            LogControlPcbGroupBlock(context.TestItemName, group, targets, $" 连接成功：{group.Ip}:{group.Port}", StationLogSeparator);

            await using NetworkStream stream = client.GetStream();
            Dictionary<byte, byte[]> startResponses = await SendControlPcbPacketsAndCollectResponsesAsync(
                context.TestItemName,
                stream,
                group,
                targets,
                target => BuildDailyTimingPacket(group.ProtocolVersion, target.MeterAddress, DailyTimingStartDataItem, testTime, testCount),
                target => $"日计时开始[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 时间={testTime}s, 次数={testCount}]",
                rawData => TryGetDailyTimingResponse(rawData, group.ProtocolVersion, DailyTimingStartDataItem, testTime, testCount, out byte meterAddress) ? meterAddress : null,
                TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
                TimeSpan.FromMilliseconds(packetIntervalMs),
                cancellationToken);

            List<ControlPcbStationTarget> activeTargets = targets
                .Where(target => startResponses.ContainsKey(target.MeterAddress))
                .ToList();

            foreach (ControlPcbStationTarget target in targets.Except(activeTargets))
            {
                string message = $"表位 {target.MeterAddress:X2} 开始日计时未收到正确应答";
                LogControlPcbStationBlock(context.TestItemName, group, target, message);
                RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, false, string.Empty));
            }

            if (activeTargets.Count == 0)
            {
                RunOnUiThread(() => AddProcessLog($"{context.SchemeName}/{context.TestItemName}/{group.Name}", context.SubItem.Name, false, "所有表位开始日计时均无正确应答。", Math.Max(0, Environment.TickCount64 - startTicks)));
                return;
            }

            int waitSeconds = (testTime * testCount) + testCount;
            LogControlPcbGroupBlock(
                context.TestItemName,
                group,
                activeTargets,
                $"日计时开始应答正常，表位={string.Join(",", activeTargets.Select(x => x.MeterAddress.ToString("X2")))}，等待 {waitSeconds} 秒后获取结果");

            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);

            Dictionary<byte, byte[]> resultResponses = await SendControlPcbPacketsAndCollectResponsesAsync(
                context.TestItemName,
                stream,
                group,
                activeTargets,
                target => BuildDailyTimingPacket(group.ProtocolVersion, target.MeterAddress, DailyTimingResultDataItem, testTime, testCount),
                target => $"日计时结果获取[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 时间={testTime}s, 次数={testCount}]",
                rawData => TryGetDailyTimingResponse(rawData, group.ProtocolVersion, DailyTimingResultDataItem, testTime, testCount, out byte meterAddress) ? meterAddress : null,
                TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
                TimeSpan.FromMilliseconds(packetIntervalMs),
                cancellationToken);

            foreach (ControlPcbStationTarget target in activeTargets)
            {
                bool passed = resultResponses.ContainsKey(target.MeterAddress);
                string responseHex = passed ? BitConverter.ToString(resultResponses[target.MeterAddress]).Replace("-", " ") : string.Empty;
                string message = passed ? "日计时结果获取应答正常" : "日计时结果获取未收到正确应答";
                LogControlPcbStationBlock(context.TestItemName, group, target, message, StationLogSeparator);
                RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, passed, responseHex));
            }

            bool groupPassed = activeTargets.All(target => resultResponses.ContainsKey(target.MeterAddress));
            RunOnUiThread(() =>
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}/{group.Name}",
                    context.SubItem.Name,
                    groupPassed,
                    groupPassed ? "控制PCB日计时全部通过。" : "控制PCB日计时存在未通过表位。",
                    Math.Max(0, Environment.TickCount64 - startTicks)));
        }

        /// <summary>
        /// 在线程池中执行 UI 更新。
        /// </summary>
        private void RunOnUiThread(Action action)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(action);
                return;
            }

            action();
        }

        /// <summary>
        /// 向单个工位发送测试报文并等待响应。
        /// 适用于 StationTcp 模式下的一发一收通信测试。
        /// </summary>
        private static async Task<string> SendStationRequestAsync(
            StationCommunicationConfig station,
            SelectedSubItemContext context,
            CancellationToken cancellationToken)
        {
            MeterTestSubItem subItem = context.SubItem;
            byte[] requestBytes = ParseHexBytes(subItem.RequestHex);
            if (requestBytes.Length == 0)
            {
                throw new InvalidOperationException("请求报文为空或不是合法 HEX。");
            }

            LogStationCommunicationBlock(
                context.TestItemName,
                station,
                StationLogSeparator,
                $" 准备连接：{station.Ip}:{station.Port}，测试内容={subItem.Name}",
                StationLogSeparator);

            using TcpClient client = new();
            using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Math.Max(100, subItem.TimeoutMs));

            try
            {
                await client.ConnectAsync(station.Ip, station.Port, timeoutCts.Token);
            }
            catch (Exception ex)
            {
                LogStationCommunicationBlock(
                    context.TestItemName,
                    station,
                    $"连接失败：{station.Ip}:{station.Port}",
                    StationLogSeparator);
                throw new StationConnectionException($"连接失败：{station.Ip}:{station.Port}", ex);
            }

            LogStationCommunicationBlock(
                context.TestItemName,
                station,
                $" 连接成功：{station.Ip}:{station.Port}",
                StationLogSeparator);

            await using NetworkStream stream = client.GetStream();
            LogStationCommunicationBlock(context.TestItemName, station, $"{FormatStationLogTimestamp()} - 发送报文：{NormalizeHex(subItem.RequestHex)}");
            await stream.WriteAsync(requestBytes, timeoutCts.Token);
            await stream.FlushAsync(timeoutCts.Token);

            byte[] buffer = new byte[4096];
            int length;
            try
            {
                length = await stream.ReadAsync(buffer, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                LogStationCommunicationBlock(context.TestItemName, station, $"{FormatStationLogTimestamp()} - 接受报文：");
                return string.Empty;
            }

            if (length <= 0)
            {
                LogStationCommunicationBlock(context.TestItemName, station, $"{FormatStationLogTimestamp()} - 接受报文：");
                return string.Empty;
            }

            byte[] responseBytes = new byte[length];
            Array.Copy(buffer, responseBytes, length);
            string responseHex = BitConverter.ToString(responseBytes).Replace("-", " ");
            LogStationCommunicationBlock(context.TestItemName, station, $"{FormatStationLogTimestamp()} - 接受报文：{responseHex}");
            return responseHex;
        }

        /// <summary>
        /// 向控制 PCB 发送多工位报文，并按表位地址收集每个工位的响应。
        /// </summary>
        private async Task<Dictionary<byte, byte[]>> SendControlPcbPacketsAndCollectResponsesAsync(
            string testItemName,
            NetworkStream stream,
            MeterTestControlPcbGroup group,
            List<ControlPcbStationTarget> targets,
            Func<ControlPcbStationTarget, byte[]> packetFactory,
            Func<ControlPcbStationTarget, string> packetNameFactory,
            Func<byte[], byte?> responseAddressResolver,
            TimeSpan timeout,
            TimeSpan packetInterval,
            CancellationToken cancellationToken)
        {
            object pendingLock = new();
            Dictionary<byte, TaskCompletionSource<byte[]>> pending = targets.ToDictionary(
                target => target.MeterAddress,
                _ => new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously));

            using CancellationTokenSource readCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Task readTask = ReadControlPcbFramesAsync(
                stream,
                group.ProtocolVersion,
                frame =>
                {
                    byte? meterAddress = responseAddressResolver(frame);
                    if (!meterAddress.HasValue)
                        return;

                    lock (pendingLock)
                    {
                        if (pending.TryGetValue(meterAddress.Value, out TaskCompletionSource<byte[]>? completionSource))
                        {
                            completionSource.TrySetResult(frame);
                        }
                    }
                },
                readCts.Token);

            try
            {
                for (int index = 0; index < targets.Count; index++)
                {
                    ControlPcbStationTarget target = targets[index];
                    byte[] packet = packetFactory(target);
                    string packetHex = BitConverter.ToString(packet).Replace("-", " ");
                    LogControlPcbStationBlock(testItemName, group, target, $"{FormatStationLogTimestamp()} - 发送报文：{packetHex}，{packetNameFactory(target)}");
                    await stream.WriteAsync(packet, cancellationToken);
                    await stream.FlushAsync(cancellationToken);

                    if (index < targets.Count - 1)
                    {
                        await Task.Delay(packetInterval, cancellationToken);
                    }
                }

                Task allResponsesTask = Task.WhenAll(pending.Values.Select(source => source.Task));
                Task completedTask = await Task.WhenAny(allResponsesTask, Task.Delay(timeout, cancellationToken));
                if (completedTask != allResponsesTask)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                Dictionary<byte, byte[]> responses = new();
                foreach ((byte meterAddress, TaskCompletionSource<byte[]> completionSource) in pending)
                {
                    if (!completionSource.Task.IsCompletedSuccessfully)
                        continue;

                    responses[meterAddress] = completionSource.Task.Result;
                    ControlPcbStationTarget? target = targets.FirstOrDefault(item => item.MeterAddress == meterAddress);
                    if (target != null)
                    {
                        string responseHex = BitConverter.ToString(completionSource.Task.Result).Replace("-", " ");
                        LogControlPcbStationBlock(testItemName, group, target, $"{FormatStationLogTimestamp()} - 接受报文：{responseHex}");
                    }
                }

                return responses;
            }
            finally
            {
                readCts.Cancel();
                try
                {
                    await readTask;
                }
                catch (OperationCanceledException)
                {
                }
                catch (IOException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        /// <summary>
        /// 持续从控制 PCB 网络流中读取帧，并把完整帧交给回调。
        /// </summary>
        private static async Task ReadControlPcbFramesAsync(
            NetworkStream stream,
            string protocolVersion,
            Action<byte[]> onFrameReceived,
            CancellationToken cancellationToken)
        {
            byte[] readBuffer = new byte[4096];
            List<byte> frameBuffer = new();

            while (!cancellationToken.IsCancellationRequested)
            {
                int length = await stream.ReadAsync(readBuffer, cancellationToken);
                if (length <= 0)
                    return;

                frameBuffer.AddRange(readBuffer.Take(length));
                while (TryExtractControlPcbFrame(frameBuffer, protocolVersion, out byte[] frame))
                {
                    onFrameReceived(frame);
                }
            }
        }

        /// <summary>
        /// 从缓冲区中提取一帧控制 PCB 数据。
        /// V1 和 V2 帧头/帧尾格式不同，因此这里根据协议版本分流。
        /// </summary>
        private static bool TryExtractControlPcbFrame(List<byte> buffer, string protocolVersion, out byte[] frame)
        {
            frame = Array.Empty<byte>();
            bool isV2 = IsControlPcbV2(protocolVersion);

            while (buffer.Count > 0)
            {
                if (isV2)
                {
                    int startIndex = FindV2FrameStart(buffer);
                    if (startIndex < 0)
                    {
                        buffer.Clear();
                        return false;
                    }

                    if (startIndex > 0)
                    {
                        buffer.RemoveRange(0, startIndex);
                    }

                    if (buffer.Count < 11)
                        return false;

                    int dataLength = buffer[2] | (buffer[3] << 8);
                    int totalLength = dataLength + 4;
                    if (totalLength < 11)
                    {
                        buffer.RemoveAt(0);
                        continue;
                    }

                    if (buffer.Count < totalLength)
                        return false;

                    if (buffer[totalLength - 2] != MeterFrameStopV2A || buffer[totalLength - 1] != MeterFrameStopV2B)
                    {
                        buffer.RemoveAt(0);
                        continue;
                    }

                    frame = buffer.Take(totalLength).ToArray();
                    buffer.RemoveRange(0, totalLength);
                    return true;
                }

                int v1StartIndex = buffer.IndexOf(MeterFrameStartV1);
                if (v1StartIndex < 0)
                {
                    buffer.Clear();
                    return false;
                }

                if (v1StartIndex > 0)
                {
                    buffer.RemoveRange(0, v1StartIndex);
                }

                if (buffer.Count < 10)
                    return false;

                int frameLength = buffer[1] | (buffer[2] << 8);
                int totalV1Length = frameLength + 2;
                if (totalV1Length < 10)
                {
                    buffer.RemoveAt(0);
                    continue;
                }

                if (buffer.Count < totalV1Length)
                    return false;

                if (buffer[totalV1Length - 1] != MeterFrameStopV1)
                {
                    buffer.RemoveAt(0);
                    continue;
                }

                frame = buffer.Take(totalV1Length).ToArray();
                buffer.RemoveRange(0, totalV1Length);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 查找 V2 控制 PCB 帧头 0x55 0x44 的位置。
        /// </summary>
        private static int FindV2FrameStart(List<byte> buffer)
        {
            for (int index = 0; index < buffer.Count - 1; index++)
            {
                if (buffer[index] == MeterFrameStartV2A && buffer[index + 1] == MeterFrameStartV2B)
                    return index;
            }

            return -1;
        }

        /// <summary>
        /// 统一关闭工位表和过程表的列排序/拖动/行高自动变化。
        /// </summary>
        private void ConfigureDataGridViewSorting()
        {
            stationGrid.AllowUserToOrderColumns = false;
            processGrid.AllowUserToOrderColumns = false;
            stationGrid.AllowUserToResizeColumns = false;
            processGrid.AllowUserToResizeColumns = false;
            stationGrid.AllowUserToResizeRows = false;
            processGrid.AllowUserToResizeRows = false;
            stationGrid.RowTemplate.Height = 34;
            processGrid.RowTemplate.Height = 40;

            foreach (DataGridViewColumn column in stationGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            foreach (DataGridViewColumn column in processGrid.Columns)
            {
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            }
        }

        /// <summary>
        /// 将表格中所有已有行固定为模板行高。
        /// </summary>
        private static void ApplyFixedGridRowHeight(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                row.Height = grid.RowTemplate.Height;
                row.MinimumHeight = grid.RowTemplate.Height;
            }
        }

        /// <summary>
        /// 写入工位级日志。
        /// </summary>
        private static void LogMeterTestStation(StationCommunicationConfig station, string message)
        {
            LogMessage.MeterTestStationLog(station.Ip, station.Port, station.StationNo, message);
        }

        /// <summary>
        /// 以工位为单位写入站点通信块日志。
        /// </summary>
        private static void LogStationCommunicationBlock(string testItemName, StationCommunicationConfig station, params string[] lines)
        {
            LogMessage.MeterTestStationRawLog(testItemName, station.StationNo, string.Join(Environment.NewLine, lines));
        }

        /// <summary>
        /// 以工位为单位写入控制 PCB 日志。
        /// </summary>
        private static void LogControlPcbStationBlock(string testItemName, MeterTestControlPcbGroup group, ControlPcbStationTarget target, params string[] lines)
        {
            LogMessage.MeterTestStationRawLog(testItemName, target.StationNo, string.Join(Environment.NewLine, lines));
        }

        /// <summary>
        /// 给控制 PCB 组内所有工位同时写入同一段日志。
        /// </summary>
        private static void LogControlPcbGroupBlock(string testItemName, MeterTestControlPcbGroup group, IEnumerable<ControlPcbStationTarget> targets, params string[] lines)
        {
            foreach (ControlPcbStationTarget target in targets)
            {
                LogControlPcbStationBlock(testItemName, group, target, lines);
            }
        }

        /// <summary>
        /// 统一的日志时间戳格式。
        /// </summary>
        private static string FormatStationLogTimestamp()
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}]";
        }

        /// <summary>
        /// 把指定工位的状态切换为“测试中”。
        /// </summary>
        private void UpdateStationRunningState(int stationNo, SelectedSubItemContext context)
        {
            DataGridViewRow row = stationGrid.Rows[stationNo - 1];
            string testContent = context.SubItem.Name;
            string now = DateTime.Now.ToString("HH:mm:ss");
            Color resultColor = Color.FromArgb(180, 83, 9);

            row.Cells[colStationTestContent.Index].Value = testContent;
            row.Cells[colStationResult.Index].Value = "测试中";
            row.Cells[colStationTime.Index].Value = now;
            row.Cells[colStationResult.Index].Style.ForeColor = resultColor;
            row.Cells[colStationResult.Index].ToolTipText = string.Empty;

            SaveStationDisplayState(context, stationNo, testContent, string.Empty, "测试中", now, resultColor, string.Empty);
        }

        /// <summary>
        /// 将工位执行结果应用到界面，并在需要时解析表位地址。
        /// </summary>
        private void ApplyStationExecutionResult(StationCommunicationConfig station, SelectedSubItemContext context, string responseHex, bool passed)
        {
            DataGridViewRow row = stationGrid.Rows[station.StationNo - 1];
            MeterTestSubItem subItem = context.SubItem;
            string meterAddress = UsesSgcc698BroadcastAddressParser(subItem) && TryParseMeterAddress(subItem, responseHex, out string parsedAddress)
                ? parsedAddress
                : string.Empty;
            string result = passed ? "合格" : "不合格";
            string now = DateTime.Now.ToString("HH:mm:ss");
            Color resultColor = passed ? Color.FromArgb(22, 101, 52) : Color.Red;

            row.Cells[colStationTestContent.Index].Value = subItem.Name;
            if (!string.IsNullOrWhiteSpace(meterAddress))
            {
                row.Cells[colStationMeterAddress.Index].Value = meterAddress;
            }
            row.Cells[colStationResult.Index].Value = result;
            row.Cells[colStationTime.Index].Value = now;
            row.Cells[colStationResult.Index].Style.ForeColor = resultColor;
            row.Cells[colStationResult.Index].ToolTipText = responseHex;

            SaveStationDisplayState(context, station.StationNo, subItem.Name, meterAddress, result, now, resultColor, responseHex);
            if (!string.IsNullOrWhiteSpace(meterAddress))
            {
                SaveMeterArchiveFromRow(row);
            }
        }

        /// <summary>
        /// 仅按工位号回填执行结果，常用于控制 PCB 流程。
        /// </summary>
        private void ApplyStationExecutionResult(int stationNo, SelectedSubItemContext context, bool passed, string responseHex)
        {
            DataGridViewRow row = stationGrid.Rows[stationNo - 1];
            string testName = context.SubItem.Name;
            string result = passed ? "合格" : "不合格";
            string now = DateTime.Now.ToString("HH:mm:ss");
            Color resultColor = passed ? Color.FromArgb(22, 101, 52) : Color.Red;

            row.Cells[colStationTestContent.Index].Value = testName;
            row.Cells[colStationResult.Index].Value = result;
            row.Cells[colStationTime.Index].Value = now;
            row.Cells[colStationResult.Index].Style.ForeColor = resultColor;
            row.Cells[colStationResult.Index].ToolTipText = responseHex;

            SaveStationDisplayState(context, stationNo, testName, string.Empty, result, now, resultColor, responseHex);
        }

        /// <summary>
        /// 把控制 PCB 组结果统一回填到所有目标工位。
        /// </summary>
        private void ApplyControlPcbGroupResult(IEnumerable<ControlPcbStationTarget> targets, SelectedSubItemContext context, bool passed, string message, string responseHex)
        {
            foreach (ControlPcbStationTarget target in targets)
            {
                ApplyStationExecutionResult(target.StationNo, context, passed, responseHex);
                stationGrid.Rows[target.StationNo - 1].Cells[colStationResult.Index].ToolTipText = message;
                CacheStationToolTip(context, target.StationNo, message);
            }
        }

        /// <summary>
        /// 根据当前选中的树节点，解析出实际要执行的测试上下文列表。
        /// </summary>
        private List<SelectedSubItemContext> GetSelectedTestContexts(TreeNode selectedNode)
        {
            List<SelectedSubItemContext> contexts = new();

            switch (selectedNode.Tag)
            {
                case MeterTestScheme scheme:
                    foreach (MeterTestItem item in scheme.TestItems)
                    {
                        foreach (MeterTestSubItem subItem in item.TestSubItems.Where(subItem => subItem.Enabled))
                        {
                            contexts.Add(new SelectedSubItemContext(scheme.Name, item.Name, subItem));
                        }
                    }
                    break;
                case MeterTestItem item:
                    if (selectedNode.Parent?.Tag is not MeterTestScheme parentScheme)
                        throw new InvalidOperationException("测试项未找到所属方案。");

                    foreach (MeterTestSubItem subItem in item.TestSubItems.Where(subItem => subItem.Enabled))
                    {
                        contexts.Add(new SelectedSubItemContext(parentScheme.Name, item.Name, subItem));
                    }
                    break;
                case MeterTestSubItem subItem:
                    if (!subItem.Enabled)
                        break;

                    if (selectedNode.Parent?.Tag is not MeterTestItem parentItem ||
                        selectedNode.Parent.Parent?.Tag is not MeterTestScheme parentSchemeOfSubItem)
                    {
                        throw new InvalidOperationException("测试小项层级不完整。");
                    }

                    contexts.Add(new SelectedSubItemContext(parentSchemeOfSubItem.Name, parentItem.Name, subItem));
                    break;
            }

            return contexts;
        }

        /// <summary>
        /// 兼容旧命名：包含“地址/读地址”的测试会触发表位地址解析回填。
        /// </summary>
        private static bool IsLegacyAddressTestName(string testName)
        {
            return testName.Contains("地址", StringComparison.OrdinalIgnoreCase)
                || testName.Contains("读地址", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 读取当前被勾选的工位，并校验 IP/Port 合法性。
        /// </summary>
        private List<StationCommunicationConfig> GetSelectedStations()
        {
            stationGrid.EndEdit();
            List<StationCommunicationConfig> stations = new();

            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (row.IsNewRow || !Convert.ToBoolean(row.Cells[colStationSelected.Index].Value ?? false))
                    continue;

                int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                string ip = Convert.ToString(row.Cells[colStationIp.Index].Value)?.Trim() ?? string.Empty;
                string portText = Convert.ToString(row.Cells[colStationPort.Index].Value)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(ip) || !int.TryParse(portText, out int port) || port < 1 || port > 65535)
                {
                    throw new InvalidOperationException($"工位{stationNo} IP 或端口配置不正确。");
                }

                stations.Add(new StationCommunicationConfig(stationNo, ip, port));
            }

            return stations;
        }

        /// <summary>
        /// 保存工位通信配置到 XML 和本地数据库。
        /// </summary>
        private void SaveStationCommunicationConfig()
        {
            if (isLoadingStationConfig)
                return;

            MeterTestStationConfig config = new();
            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                string ip = Convert.ToString(row.Cells[colStationIp.Index].Value)?.Trim() ?? string.Empty;
                string portText = Convert.ToString(row.Cells[colStationPort.Index].Value)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(ip) || !int.TryParse(portText, out int port) || port < 1 || port > 65535)
                    continue;

                config.Stations.Add(new MeterTestStationCommunication
                {
                    StationNo = stationNo,
                    Ip = ip,
                    Port = port
                });

                accessDatabaseService.SaveStationConfig(stationNo, ip, port, true);
            }

            stationConfigService.Save(stationConfigFilePath, config);
        }

        /// <summary>
        /// 把控制 PCB 配置同步到本地数据库。
        /// </summary>
        private void SaveControlPcbConfigToAccess()
        {
            foreach (MeterTestControlPcbGroup group in meterTestPlanConfig.ControlPcbGroups)
            {
                accessDatabaseService.SaveControlPcbConfig(group);
            }
        }

        /// <summary>
        /// 全选或全清工位。
        /// </summary>
        private void SetAllStationSelection(bool selected)
        {
            isUpdatingStationSelection = true;
            try
            {
                foreach (DataGridViewRow row in stationGrid.Rows)
                {
                    if (!Equals(row.Cells[colStationSelected.Index].Value, selected))
                    {
                        row.Cells[colStationSelected.Index].Value = selected;
                    }
                }
            }
            finally
            {
                isUpdatingStationSelection = false;
            }

            ApplySingleStationSelectionRule();
        }

        /// <summary>
        /// 单工位模式下，只保留一个工位被选中。
        /// </summary>
        private void ApplySingleStationSelectionRule(int changedRowIndex = -1)
        {
            if (!rbSingleStation.Checked)
                return;

            int selectedRowIndex = changedRowIndex >= 0 ? changedRowIndex : FindFirstSelectedStationRowIndex();
            if (selectedRowIndex < 0)
                selectedRowIndex = 0;

            isUpdatingStationSelection = true;
            try
            {
                foreach (DataGridViewRow row in stationGrid.Rows)
                {
                    bool shouldSelect = row.Index == selectedRowIndex;
                    if (!Equals(row.Cells[colStationSelected.Index].Value, shouldSelect))
                    {
                        row.Cells[colStationSelected.Index].Value = shouldSelect;
                    }
                }
            }
            finally
            {
                isUpdatingStationSelection = false;
            }
        }

        /// <summary>
        /// 找到第一个被勾选的工位行。
        /// </summary>
        private int FindFirstSelectedStationRowIndex()
        {
            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (Convert.ToBoolean(row.Cells[colStationSelected.Index].Value ?? false))
                    return row.Index;
            }

            return -1;
        }

        /// <summary>
        /// 获取当前 TreeView 节点对应的测试内容文本。
        /// </summary>
        private string GetSelectedTestContentText()
        {
            return schemeTreeView.SelectedNode?.Tag switch
            {
                MeterTestSubItem subItem => subItem.Name,
                MeterTestItem testItem => testItem.TestSubItems.Count == 1 ? testItem.TestSubItems[0].Name : testItem.Name,
                MeterTestScheme scheme => scheme.Name,
                _ => ReadMeterAddressTestName
            };
        }

        /// <summary>
        /// 批量更新工位表中的测试内容列。
        /// </summary>
        private void UpdateStationTestContent(string testContent)
        {
            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                row.Cells[colStationTestContent.Index].Value = testContent;
            }
        }

        /// <summary>
        /// 根据当前方案树节点，恢复对应的工位测试状态显示。
        /// </summary>
        private void RestoreStationDisplayForSelectedNode()
        {
            if (!TryGetSelectedDisplayContext(out SelectedSubItemContext context))
            {
                UpdateStationTestContent(GetSelectedTestContentText());
                ClearStationResultColumns();
                return;
            }

            LoadStationResultsFromAccess(context);

            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                StationResultKey key = CreateStationResultKey(context, stationNo);
                if (stationResultCache.TryGetValue(key, out StationDisplayState? state))
                {
                    ApplyCachedStationDisplay(row, state);
                    continue;
                }

                row.Cells[colStationTestContent.Index].Value = context.SubItem.Name;
                row.Cells[colStationResult.Index].Value = "待测试";
                row.Cells[colStationTime.Index].Value = string.Empty;
                row.Cells[colStationResult.Index].Style.ForeColor = Color.FromArgb(31, 41, 55);
                row.Cells[colStationResult.Index].ToolTipText = string.Empty;
            }
        }

        /// <summary>
        /// 判断当前树节点是否能映射到一个可展示/可回填的测试上下文。
        /// </summary>
        private bool TryGetSelectedDisplayContext(out SelectedSubItemContext context)
        {
            context = default!;
            TreeNode? selectedNode = schemeTreeView.SelectedNode;
            if (selectedNode?.Tag is null)
                return false;

            switch (selectedNode.Tag)
            {
                case MeterTestSubItem subItem:
                    if (selectedNode.Parent?.Tag is MeterTestItem parentItem &&
                        selectedNode.Parent.Parent?.Tag is MeterTestScheme parentScheme)
                    {
                        context = new SelectedSubItemContext(parentScheme.Name, parentItem.Name, subItem);
                        return true;
                    }

                    return false;

                case MeterTestItem item:
                    if (item.TestSubItems.Count == 1 &&
                        selectedNode.Parent?.Tag is MeterTestScheme scheme)
                    {
                        context = new SelectedSubItemContext(scheme.Name, item.Name, item.TestSubItems[0]);
                        return true;
                    }

                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 清空工位结果列，回到待测试状态。
        /// </summary>
        private void ClearStationResultColumns()
        {
            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                row.Cells[colStationResult.Index].Value = "待测试";
                row.Cells[colStationTime.Index].Value = string.Empty;
                row.Cells[colStationResult.Index].Style.ForeColor = Color.FromArgb(31, 41, 55);
                row.Cells[colStationResult.Index].ToolTipText = string.Empty;
            }
        }

        /// <summary>
        /// 将缓存的工位显示状态重新应用到表格行。
        /// </summary>
        private void ApplyCachedStationDisplay(DataGridViewRow row, StationDisplayState state)
        {
            row.Cells[colStationTestContent.Index].Value = state.TestContent;
            if (!string.IsNullOrWhiteSpace(state.MeterAddress))
            {
                row.Cells[colStationMeterAddress.Index].Value = state.MeterAddress;
            }
            row.Cells[colStationResult.Index].Value = state.Result;
            row.Cells[colStationTime.Index].Value = state.Time;
            row.Cells[colStationResult.Index].Style.ForeColor = state.ResultColor;
            row.Cells[colStationResult.Index].ToolTipText = state.ToolTip;
        }

        /// <summary>
        /// 缓存工位显示状态，并写入本地数据库。
        /// </summary>
        private void SaveStationDisplayState(
            SelectedSubItemContext context,
            int stationNo,
            string testContent,
            string meterAddress,
            string result,
            string time,
            Color resultColor,
            string toolTip)
        {
            StationResultKey key = CreateStationResultKey(context, stationNo);
            StationDisplayState state = new(testContent, meterAddress, result, time, resultColor, toolTip);
            stationResultCache[key] = state;
            SaveStationDisplayStateToAccess(context, stationNo, state);
        }

        /// <summary>
        /// 仅刷新某个工位的提示信息缓存。
        /// </summary>
        private void CacheStationToolTip(SelectedSubItemContext context, int stationNo, string toolTip)
        {
            StationResultKey key = CreateStationResultKey(context, stationNo);
            if (!stationResultCache.TryGetValue(key, out StationDisplayState? state))
                return;

            StationDisplayState updatedState = state with { ToolTip = toolTip };
            stationResultCache[key] = updatedState;
            SaveStationDisplayStateToAccess(context, stationNo, updatedState);
        }

        /// <summary>
        /// 从本地数据库读取结果缓存。
        /// </summary>
        private void LoadStationResultsFromAccess(SelectedSubItemContext context)
        {
            Dictionary<int, StationDisplayStateData> persistedResults = accessDatabaseService.LoadStationResults(
                context.SchemeName,
                context.TestItemName,
                context.SubItem.Name);

            foreach ((int stationNo, StationDisplayStateData state) in persistedResults)
            {
                StationResultKey key = CreateStationResultKey(context, stationNo);
                if (stationResultCache.ContainsKey(key))
                    continue;

                stationResultCache[key] = new StationDisplayState(
                    state.TestContent,
                    state.MeterAddress,
                    state.Result,
                    state.Time,
                    state.ResultColor,
                    state.Message);
            }
        }

        /// <summary>
        /// 把当前工位显示状态保存到本地数据库。
        /// </summary>
        private void SaveStationDisplayStateToAccess(SelectedSubItemContext context, int stationNo, StationDisplayState state)
        {
            accessDatabaseService.SaveStationResult(
                currentRunId,
                context.SchemeName,
                context.TestItemName,
                context.SubItem.Name,
                stationNo,
                new StationDisplayStateData(
                    state.TestContent,
                    state.MeterAddress,
                    state.Result,
                    state.Time,
                    state.ResultColor,
                    state.ToolTip,
                    state.ToolTip));
        }

        private static StationResultKey CreateStationResultKey(SelectedSubItemContext context, int stationNo)
        {
            return new StationResultKey(context.SchemeName, context.TestItemName, context.SubItem.Name, stationNo);
        }

        /// <summary>
        /// 获取广播读地址的默认报文。
        /// </summary>
        public static string GetBroadcastReadAddressFrame()
        {
            return BroadcastReadAddressFrame;
        }

        /// <summary>
        /// 普通 HEX 应答匹配逻辑。
        /// </summary>
        private static bool IsResponseMatched(MeterTestSubItem subItem, string? response)
        {
            string normalizedResponse = NormalizeHex(response ?? string.Empty).Replace(" ", string.Empty);
            string normalizedExpected = NormalizeHex(subItem.ExpectedResponse).Replace(" ", string.Empty);

            if (string.IsNullOrEmpty(normalizedExpected))
                return !string.IsNullOrEmpty(normalizedResponse);

            ResponseMatchMode matchMode = Enum.TryParse(subItem.MatchMode, true, out ResponseMatchMode mode)
                ? mode
                : ResponseMatchMode.Contains;

            return matchMode switch
            {
                ResponseMatchMode.Exact => normalizedResponse.Equals(normalizedExpected, StringComparison.OrdinalIgnoreCase),
                ResponseMatchMode.StartsWith => normalizedResponse.StartsWith(normalizedExpected, StringComparison.OrdinalIgnoreCase),
                _ => normalizedResponse.Contains(normalizedExpected, StringComparison.OrdinalIgnoreCase)
            };
        }

        /// <summary>
        /// 尝试从 698 广播应答中解析表位地址。
        /// </summary>
        private static bool TryParseMeterAddress(MeterTestSubItem subItem, string responseHex, out string meterAddress)
        {
            Sgcc698BroadcastAddressParseResult parseResult = ParseSgcc698BroadcastAddressResponse(subItem, responseHex);
            meterAddress = parseResult.MeterAddress;
            return parseResult.IsValid;
        }

        /// <summary>
        /// 判断测试小项是否需要走 698 广播地址解析器。
        /// </summary>
        private static bool UsesSgcc698BroadcastAddressParser(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ResponseParser, true, out ResponseParserType parserType)
                && parserType == ResponseParserType.Sgcc698BroadcastAddress;
        }

        private static Sgcc698BroadcastAddressParseResult ParseSgcc698BroadcastAddressResponse(MeterTestSubItem subItem, string responseHex)
        {
            return SGCCTools.ParseBroadcastAddressResponse(
                responseHex,
                subItem.ExpectedOad,
                subItem.ExpectedApdu,
                subItem.ExpectedDataType,
                subItem.ExpectedDataLength);
        }

        /// <summary>
        /// 判断测试小项是否是控制 PCB 日计时流程。
        /// </summary>
        private static bool UsesControlPcbDailyTimingExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.ControlPcbDailyTiming;
        }

        /// <summary>
        /// 获取当前小项可用的控制 PCB 组。
        /// </summary>
        private List<MeterTestControlPcbGroup> GetEnabledControlPcbGroups(MeterTestSubItem subItem)
        {
            string configuredGroup = subItem.ControlPcbGroup.Trim();
            return meterTestPlanConfig.ControlPcbGroups
                .Where(group => group.Enabled)
                .Where(group => string.IsNullOrWhiteSpace(configuredGroup) ||
                                group.Name.Equals(configuredGroup, StringComparison.OrdinalIgnoreCase))
                .Where(group => !string.IsNullOrWhiteSpace(group.Ip) && group.Port is >= 1 and <= 65535)
                .ToList();
        }

        /// <summary>
        /// 从测试小项读取日计时的时间、次数和报文间隔。
        /// </summary>
        private static bool TryGetDailyTimingConfig(MeterTestSubItem subItem, out byte testTime, out byte testCount, out int packetIntervalMs)
        {
            testTime = 0;
            testCount = 0;
            packetIntervalMs = Math.Max(0, subItem.PacketIntervalMs);

            if (subItem.DailyTimingTime < 1 || subItem.DailyTimingTime > 99)
                return false;

            if (subItem.DailyTimingCount < 1 || subItem.DailyTimingCount > 10)
                return false;

            testTime = (byte)subItem.DailyTimingTime;
            testCount = (byte)subItem.DailyTimingCount;
            return true;
        }

        /// <summary>
        /// 根据控制 PCB 组和当前选中工位，计算实际需要下发的目标工位集合。
        /// </summary>
        private static List<ControlPcbStationTarget> GetControlPcbStationTargets(
            MeterTestControlPcbGroup group,
            List<StationCommunicationConfig> selectedStations)
        {
            if (group.StationStart < 1 || group.StationEnd < group.StationStart || group.MeterAddressStart < 1)
                return new List<ControlPcbStationTarget>();

            List<ControlPcbStationTarget> targets = new();
            foreach (StationCommunicationConfig station in selectedStations)
            {
                if (station.StationNo < group.StationStart || station.StationNo > group.StationEnd)
                    continue;

                int meterAddress = group.MeterAddressStart + (station.StationNo - group.StationStart);
                if (meterAddress < 1 || meterAddress > 48)
                    continue;

                targets.Add(new ControlPcbStationTarget(station.StationNo, (byte)meterAddress));
            }

            return targets;
        }

        /// <summary>
        /// 构造控制 PCB 日计时报文。
        /// </summary>
        private static byte[] BuildDailyTimingPacket(
            string protocolVersion,
            byte meterAddress,
            byte operation,
            byte testTime,
            byte testCount)
        {
            return IsControlPcbV2(protocolVersion)
                ? BuildV2MeterPacket(meterAddress, MeterDailyTimingCommand, operation, testTime, testCount)
                : BuildV1MeterPacket(meterAddress, MeterDailyTimingCommand, operation, testTime, testCount);
        }

        /// <summary>
        /// 构造 V1 电表控制报文。
        /// </summary>
        private static byte[] BuildV1MeterPacket(byte meterAddress, byte command, params byte[] dataItems)
        {
            byte[] payload = dataItems.Length == 0 ? new byte[] { 0x00 } : dataItems;
            int frameLength = 7 + payload.Length;
            byte[] packet = new byte[frameLength + 2];

            packet[0] = MeterFrameStartV1;
            packet[1] = (byte)(frameLength & 0xFF);
            packet[2] = (byte)((frameLength >> 8) & 0xFF);
            packet[3] = MeterDirectionPcToMcu;
            packet[4] = meterAddress;
            packet[5] = MeterControlProtocolV1;
            packet[6] = command;
            Array.Copy(payload, 0, packet, 7, payload.Length);
            packet[frameLength] = CalculateChecksum(packet, 1, frameLength - 1);
            packet[frameLength + 1] = MeterFrameStopV1;
            return packet;
        }

        /// <summary>
        /// 构造 V2 电表控制报文。
        /// </summary>
        private static byte[] BuildV2MeterPacket(byte meterAddress, byte command, params byte[] dataItems)
        {
            byte[] payload = dataItems.Length == 0 ? new byte[] { 0x00 } : dataItems;
            int dataLength = 2 + 1 + 1 + 1 + 1 + payload.Length + 1;
            byte[] packet = new byte[2 + dataLength + 2];

            packet[0] = MeterFrameStartV2A;
            packet[1] = MeterFrameStartV2B;
            packet[2] = (byte)(dataLength & 0xFF);
            packet[3] = (byte)((dataLength >> 8) & 0xFF);
            packet[4] = MeterDirectionPcToMcu;
            packet[5] = meterAddress;
            packet[6] = MeterControlProtocolV2;
            packet[7] = command;
            Array.Copy(payload, 0, packet, 8, payload.Length);
            packet[8 + payload.Length] = CalculateChecksum(packet, 2, dataLength - 1);
            packet[9 + payload.Length] = MeterFrameStopV2A;
            packet[10 + payload.Length] = MeterFrameStopV2B;
            return packet;
        }

        /// <summary>
        /// 校验并提取日计时应答帧中的表位地址。
        /// </summary>
        private static bool TryGetDailyTimingResponse(
            byte[] rawData,
            string protocolVersion,
            byte operation,
            byte testTime,
            byte testCount,
            out byte meterAddress)
        {
            meterAddress = 0x00;
            if (!TryGetControlPcbPacketDataItems(rawData, protocolVersion, MeterDailyTimingCommand, out meterAddress, out byte[] dataItems))
                return false;

            if (dataItems.Length < 3 ||
                dataItems[0] != operation ||
                dataItems[1] != testTime ||
                dataItems[2] != testCount)
            {
                return false;
            }

            return operation != DailyTimingResultDataItem || dataItems.Length >= 3;
        }

        /// <summary>
        /// 从控制 PCB 报文中拆出命令相关的数据项。
        /// </summary>
        private static bool TryGetControlPcbPacketDataItems(
            byte[] rawData,
            string protocolVersion,
            byte command,
            out byte meterAddress,
            out byte[] dataItems)
        {
            meterAddress = 0x00;
            dataItems = Array.Empty<byte>();
            return IsControlPcbV2(protocolVersion)
                ? TryGetV2MeterPacketDataItems(rawData, command, out meterAddress, out dataItems)
                : TryGetV1MeterPacketDataItems(rawData, command, out meterAddress, out dataItems);
        }

        /// <summary>
        /// 从 V1 电表报文中提取表位地址和数据项。
        /// </summary>
        private static bool TryGetV1MeterPacketDataItems(byte[] rawData, byte command, out byte meterAddress, out byte[] dataItems)
        {
            meterAddress = 0x00;
            dataItems = Array.Empty<byte>();
            if (rawData.Length < 10 || rawData[0] != MeterFrameStartV1 || rawData[^1] != MeterFrameStopV1)
                return false;

            int frameLength = rawData[1] | (rawData[2] << 8);
            if (rawData.Length != frameLength + 2 || frameLength < 8)
                return false;

            int dataItemLength = frameLength - 7;
            if (dataItemLength < 0 || CalculateChecksum(rawData, 1, frameLength - 1) != rawData[frameLength])
                return false;

            if (rawData[3] != MeterDirectionMcuToPc || rawData[5] != MeterControlProtocolV1 || rawData[6] != command)
                return false;

            meterAddress = rawData[4];
            dataItems = rawData.Skip(7).Take(dataItemLength).ToArray();
            return true;
        }

        /// <summary>
        /// 从 V2 电表报文中提取表位地址和数据项。
        /// </summary>
        private static bool TryGetV2MeterPacketDataItems(byte[] rawData, byte command, out byte meterAddress, out byte[] dataItems)
        {
            meterAddress = 0x00;
            dataItems = Array.Empty<byte>();
            if (rawData.Length < 11 ||
                rawData[0] != MeterFrameStartV2A ||
                rawData[1] != MeterFrameStartV2B ||
                rawData[^2] != MeterFrameStopV2A ||
                rawData[^1] != MeterFrameStopV2B)
            {
                return false;
            }

            int dataLength = rawData[2] | (rawData[3] << 8);
            if (rawData.Length != dataLength + 4 || dataLength < 7)
                return false;

            int dataItemLength = dataLength - 7;
            if (dataItemLength < 0 || CalculateChecksum(rawData, 2, dataLength - 1) != rawData[^3])
                return false;

            if (rawData[4] != MeterDirectionMcuToPc || rawData[6] != MeterControlProtocolV2 || rawData[7] != command)
                return false;

            meterAddress = rawData[5];
            dataItems = rawData.Skip(8).Take(dataItemLength).ToArray();
            return true;
        }

        /// <summary>
        /// 计算累加和校验字节。
        /// </summary>
        private static byte CalculateChecksum(byte[] data, int startIndex, int count)
        {
            int sum = 0;
            for (int index = startIndex; index < startIndex + count; index++)
            {
                sum += data[index];
            }

            return (byte)sum;
        }

        /// <summary>
        /// 判断控制 PCB 是否采用 V2 协议。
        /// </summary>
        private static bool IsControlPcbV2(string protocolVersion)
        {
            return !protocolVersion.Equals(MeterControlPcbProtocolVersion.V1.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 把 HEX 字符串转换为字节数组。
        /// </summary>
        private static byte[] ParseHexBytes(string hex)
        {
            string normalized = NormalizeHex(hex).Replace(" ", string.Empty);
            if (normalized.Length == 0 || normalized.Length % 2 != 0)
                return Array.Empty<byte>();

            byte[] bytes = new byte[normalized.Length / 2];
            for (int index = 0; index < bytes.Length; index++)
            {
                if (!byte.TryParse(normalized.Substring(index * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out bytes[index]))
                    return Array.Empty<byte>();
            }

            return bytes;
        }

        /// <summary>
        /// 归一化 HEX 字符串，便于比较和日志输出。
        /// </summary>
        private static string NormalizeHex(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string raw = value.Replace(" ", string.Empty)
                .Replace("-", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();

            if (raw.Length == 0)
                return string.Empty;

            List<string> bytes = new();
            for (int index = 0; index < raw.Length; index += 2)
            {
                int take = Math.Min(2, raw.Length - index);
                bytes.Add(raw.Substring(index, take).ToUpperInvariant());
            }

            return string.Join(" ", bytes);
        }

        /// <summary>
        /// 初始化台体信息采集区域。
        /// </summary>
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

        /// <summary>
        /// 创建一个台体指标显示单元。
        /// </summary>
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

        /// <summary>
        /// 外部调用入口：刷新指定台体指标值。
        /// </summary>
        public void UpdateHardwareMetric(string metricName, string value)
        {
            if (!hardwareValueLabels.TryGetValue(metricName, out Label? valueLabel))
                return;

            valueLabel.Text = string.IsNullOrWhiteSpace(value) ? "000.0000" : value;
        }

        /// <summary>
        /// 标准表读取成功后，把 15 组标准表数据同步到台体信息采集区域。
        /// 标准表当前返回：Ua/Ub/Uc、Ia/Ib/Ic、Φa/Φb/Φc、Pa/Pb/Pc、Qa/Qb/Qc。
        /// </summary>
        private void UpdateHardwareMetricsFromStandValues(IReadOnlyDictionary<string, string> standValues)
        {
            foreach (KeyValuePair<string, string> item in standValues)
            {
                UpdateHardwareMetric(item.Key, item.Value);
            }
        }

        /// <summary>
        /// 从 png 目录加载头图。
        /// </summary>
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

        /// <summary>
        /// 为顶部操作按钮加载图标。
        /// </summary>
        private void LoadOperationButtonImages()
        {
            SetButtonImage(btnStartTest, "startTest.png");
            SetButtonImage(btnStopTest, "StopTest.png");
            SetButtonImage(btnTestPlan, "TestPlan.png");
            SetButtonImage(btnAssetInfo, "资产.png");
        }

        /// <summary>
        /// 给按钮设置图标。
        /// </summary>
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

        /// <summary>
        /// 返回图标候选路径，支持运行目录和源码目录两种位置。
        /// </summary>
        private static string[] GetPngCandidates(string fileName)
        {
            return new[]
            {
                Path.Combine(AppContext.BaseDirectory, "png", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "png", fileName)
            };
        }

        /// <summary>
        /// 获取测试方案 XML 的优先路径。
        /// </summary>
        private static string GetMeterTestConfigPath()
        {
            string outputConfigPath = Path.Combine(AppContext.BaseDirectory, "MeterTest", "config", "MeterTestPlanConfig.xml");
            if (File.Exists(outputConfigPath))
            {
                return outputConfigPath;
            }

            return Path.Combine(AppContext.BaseDirectory, "config", "MeterTestPlanConfig.xml");
        }

        /// <summary>
        /// 获取工位通信配置 XML 的优先路径。
        /// </summary>
        private static string GetMeterTestStationConfigPath()
        {
            string outputConfigPath = Path.Combine(AppContext.BaseDirectory, "MeterTest", "config", "MeterTestStationConfig.xml");
            if (File.Exists(outputConfigPath))
            {
                return outputConfigPath;
            }

            return Path.Combine(AppContext.BaseDirectory, "config", "MeterTestStationConfig.xml");
        }

        /// <summary>单个工位的通信参数。</summary>
        private sealed record StationCommunicationConfig(int StationNo, string Ip, int Port);

        /// <summary>控制 PCB 流程中的目标工位与表位地址。</summary>
        private sealed record ControlPcbStationTarget(int StationNo, byte MeterAddress);

        /// <summary>当前被执行的小项上下文。</summary>
        private sealed record SelectedSubItemContext(string SchemeName, string TestItemName, MeterTestSubItem SubItem);

        /// <summary>工位结果缓存键。</summary>
        private sealed record StationResultKey(string SchemeName, string TestItemName, string TestSubItemName, int StationNo);

        /// <summary>工位在界面上的完整显示状态。</summary>
        private sealed record StationDisplayState(string TestContent, string MeterAddress, string Result, string Time, Color ResultColor, string ToolTip);

        /// <summary>运行时连接异常，用于和普通执行异常区分。</summary>
        private sealed class StationConnectionException : Exception
        {
            public StationConnectionException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        /// <summary>
        /// 当前表格的显示模式。
        /// </summary>
        private enum MeterTestGridViewMode
        {
            TestPlan,
            AssetInfo
        }
    }
}
