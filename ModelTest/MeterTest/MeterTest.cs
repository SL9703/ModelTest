using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ModelTest.MeterTest
{
    /// <summary>
    /// MeterTest 主窗体。
    /// 负责方案树加载、工位/资产表维护、测试执行、结果回填和日志输出。
    /// </summary>
    public partial class MeterTest : Form
    {
        /// <summary>用于给 WinForms 复合控件开启受保护的双缓冲属性。</summary>
        private static readonly PropertyInfo? DoubleBufferedProperty = typeof(Control).GetProperty(
            "DoubleBuffered",
            BindingFlags.Instance | BindingFlags.NonPublic);

        private const int MaxStationCount = 48;
        private const int MaxStationLogEntries = 2000;
        private const int MaxCommonLogEntries = 500;
        private const string SchemeStatusPendingImageKey = "StatusPending";
        private const string SchemeStatusRunningImageKey = "StatusRunning";
        private const string SchemeStatusPassedImageKey = "StatusPassed";
        private const string SchemeStatusFailedImageKey = "StatusFailed";
        private const string ReadMeterAddressTestName = "读取表位地址";
        private const string BroadcastReadAddressFrame = "68 17 00 43 05 AA AA AA AA AA AA 10 2B 3A 05 01 71 40 01 02 00 00 C7 C2 16";
        private const string DefaultStationIp = "127.0.0.1";
        private const string StationLogSeparator = "-----------------------------------------------------------------";
        private const int DefaultStationStartPort = 4001;
        private readonly Dictionary<string, IReadOnlyList<MeterTestAssetOptionData>> assetOptionCache =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Label> hardwareValueLabels = new();
        private readonly Dictionary<Button, OperationButtonVisualState> operationButtonVisualStates = new();
        private readonly MeterTestConfigService configService = new();
        private readonly MeterTestStationConfigService stationConfigService = new();
        private readonly MeterTestAccessDatabaseService accessDatabaseService = new();
        private readonly MeterTestBenchTypeSwitchService benchTypeSwitchService = new();
        private readonly MeterTestSourceControlService sourceControlService;
        private readonly MeterTestRunPreparationService runPreparationService;
        private readonly MeterTestSerialPortServerService serialPortServerService = new();
        private readonly MeterTestCommunicationAddressService communicationAddressService = new();
        private readonly MeterTestCommunicationTestService communicationTestService;
        private readonly MeterTestStationPowerService stationPowerService = new();
        private readonly MeterTestIndicatorLightService indicatorLightService = new();
        private readonly MeterTestControlPcbConnectionManager controlPcbConnectionManager = new();
        private readonly MeterTestControlPcbCommandService controlPcbCommandService;
        private readonly MeterTestStationTcpSessionService stationTcpSessionService = new();
        private readonly MeterTestBluetoothInterfaceService bluetoothInterfaceService;
        private readonly MeterTestCountdownService countdownService = new();
        private readonly MeterTestDailyTimingService dailyTimingService;
        private readonly MeterTestDeviceSelfCheckService deviceSelfCheckService;
        private readonly MeterTestConstantTestService constantTestService;
        private readonly MeterTestCreepingTestService creepingTestService;
        private readonly MeterTestBasicErrorService basicErrorService;
        private readonly MeterTestStartingErrorService startingErrorService;
        private readonly CancellationTokenSource stationPowerControlCts = new();
        private const string ThreePhasePlanSelectorText = "三相方案";
        private const string SinglePhasePlanSelectorText = "单相方案";
        private const string ThreePhasePlanConfigFileName = "MeterTestPlanConfig.xml";
        private const string SinglePhasePlanConfigFileName = "MeterTestPlanConfig.SinglePhase.xml";
        private string configFilePath;
        private readonly string stationConfigFilePath;
        private MeterTestPlanConfig meterTestPlanConfig = new();
        private CancellationTokenSource? executionCts;
        private bool isSourceShutdownInProgress;
        private readonly Dictionary<StationResultKey, StationDisplayState> stationResultCache = new();
        private readonly Dictionary<int, List<TestProcessLogEntry>> stationTestLogEntries = new();
        private readonly List<TestProcessLogEntry> commonTestLogEntries = new();
        private readonly object measurementSyncRoot = new();
        private readonly List<MeterTestMeasurementData> currentRunMeasurements = new();
        private ImageList? schemeStatusImageList;
        private string currentRunId = Guid.NewGuid().ToString("N");
        private DateTime currentRunStartedAt = DateTime.Now;
        private List<SelectedSubItemContext> lastExecutedContexts = new();
        private List<int> lastExecutedStationNumbers = new();
        private long testLogSequence;
        private int selectedTestLogStationNo = 1;
        private Task controlPcbInitializationTask = Task.CompletedTask;
        private bool isUpdatingStationSelection;
        private bool isLoadingStationConfig;
        private bool isLoadingMeterArchive;
        private bool isLoadingBarcodeSetting;
        private bool isApplyingBarcodeExtraction;
        private int assetBarcodeStartIndex = 9;
        private int assetBarcodeEndIndex = 20;
        private int assetBarcodeRule2FirstStart = 6;
        private int assetBarcodeRule2FirstLength = 2;
        private int assetBarcodeRule2SecondStart = 10;
        private int assetBarcodeRule2SecondLength = 10;
        private string assetBarcodeRuleType = MeterTestBarcodeExtractor.Rule1Range;
        private MeterTestGridViewMode currentGridViewMode = MeterTestGridViewMode.TestPlan;
        private MeterTestResultUserControl? resultUserControl;
        private bool initialDataLoadStarted;
        private bool initialDataLoaded;
        private bool schemeTreeStatusRefreshPending;
        private bool stationDisplayRestorePending;
        private DateTime? meterTestConfigLastWriteTimeUtc;
        private DateTime? stationConfigLastWriteTimeUtc;
        private readonly HashSet<string> loadedStationResultContextKeys = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 创建 MeterTest 主界面并装配所有流程服务。
        /// 本类只保留方案调度、UI 刷新和测试状态维护，硬件通信及业务流程由对应服务执行。
        /// </summary>
        public MeterTest()
        {
            InitializeComponent();
            controlPcbCommandService = new MeterTestControlPcbCommandService(controlPcbConnectionManager);
            dailyTimingService = new MeterTestDailyTimingService(controlPcbCommandService, countdownService);
            sourceControlService = new MeterTestSourceControlService(accessDatabaseService);
            runPreparationService = new MeterTestRunPreparationService(
                accessDatabaseService,
                benchTypeSwitchService,
                sourceControlService,
                controlPcbConnectionManager);
            communicationTestService = new MeterTestCommunicationTestService(
                serialPortServerService,
                communicationAddressService,
                stationTcpSessionService,
                accessDatabaseService);
            deviceSelfCheckService = new MeterTestDeviceSelfCheckService(
                sourceControlService,
                controlPcbCommandService);
            creepingTestService = new MeterTestCreepingTestService(
                controlPcbConnectionManager,
                accessDatabaseService,
                countdownService);
            constantTestService = new MeterTestConstantTestService(
                controlPcbCommandService,
                stationTcpSessionService,
                communicationAddressService,
                countdownService,
                accessDatabaseService);
            basicErrorService = new MeterTestBasicErrorService(
                sourceControlService,
                controlPcbConnectionManager,
                countdownService,
                accessDatabaseService);
            startingErrorService = new MeterTestStartingErrorService(
                controlPcbConnectionManager,
                accessDatabaseService,
                countdownService);
            bluetoothInterfaceService = new MeterTestBluetoothInterfaceService(communicationAddressService);
            ConfigureBufferedRendering();
            configFilePath = GetMeterTestConfigPath();
            stationConfigFilePath = GetMeterTestStationConfigPath();
            InitializePlanConfigSelector();
            ConfigureDataGridViewSorting();
            InitializeResultUserControl();
            InitializeHardwareCollectionGrid();
            BindEvents();
            InitializeSchemeStatusImages();
            LoadHeaderLogo();
            LoadOperationButtonImages();
            ConfigureOperationButtonStyles();
            ConfigureStationSelectionControlStyles();
            ConfigureWindowBounds();
            SetInitialLoadingState();
        }

        /// <summary>
        /// 开启窗体及主要复合控件的双缓冲，减少最大化、表格绑定和布局切换时的闪烁。
        /// </summary>
        private void ConfigureBufferedRendering()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            Control[] bufferedControls =
            {
                mainLayout,
                headerPanel,
                buttonGrid,
                middleArea,
                processLayout,
                stationSelectionPanel,
                countdownPanel,
                hardwareLayout,
                stationGrid,
                processGrid,
                schemeTreeView
            };

            foreach (Control control in bufferedControls)
            {
                DoubleBufferedProperty?.SetValue(control, true);
            }

            UpdateStyles();
        }

        /// <summary>
        /// 冻结首屏中会被批量更新的布局容器和表格。
        /// </summary>
        private void SuspendInitialLayout()
        {
            SuspendLayout();
            mainLayout.SuspendLayout();
            headerPanel.SuspendLayout();
            buttonGrid.SuspendLayout();
            middleArea.SuspendLayout();
            groupProcess.SuspendLayout();
            processLayout.SuspendLayout();
            stationSelectionPanel.SuspendLayout();
            countdownPanel.SuspendLayout();
            stationGrid.SuspendLayout();
            processGrid.SuspendLayout();
            groupHardware.SuspendLayout();
            hardwareLayout.SuspendLayout();
        }

        /// <summary>
        /// 按从内到外的顺序恢复布局，最后只执行一次完整布局计算。
        /// </summary>
        private void ResumeInitialLayout()
        {
            hardwareLayout.ResumeLayout(false);
            groupHardware.ResumeLayout(false);
            processGrid.ResumeLayout(false);
            stationGrid.ResumeLayout(false);
            countdownPanel.ResumeLayout(false);
            stationSelectionPanel.ResumeLayout(false);
            processLayout.ResumeLayout(false);
            groupProcess.ResumeLayout(false);
            middleArea.ResumeLayout(false);
            buttonGrid.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            mainLayout.ResumeLayout(false);
            ResumeLayout(true);
        }

        /// <summary>
        /// 配置 MeterTest 的最大化边界。
        ///
        /// 保持最大化显示，但将边界限制在屏幕工作区内，避免窗口覆盖 Windows 底部任务栏。
        /// </summary>
        private void ConfigureWindowBounds()
        {
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;

            Screen screen = IsHandleCreated
                ? Screen.FromHandle(Handle)
                : Screen.PrimaryScreen ?? Screen.AllScreens[0];
            MaximizedBounds = screen.WorkingArea;
            WindowState = FormWindowState.Maximized;
        }

        /// <summary>
        /// 首屏只显示界面骨架，把数据库、配置和控制PCB连接放到Shown之后懒加载。
        /// 这样窗体能先显示出来，避免用户看到长时间白屏或控件逐项蹦出。
        /// </summary>
        private void SetInitialLoadingState()
        {
            btnStartTest.Enabled = false;
            btnStopTest.Enabled = false;
            btnTestPlan.Enabled = false;
            btnAssetInfo.Enabled = false;
            btnTestResults.Enabled = false;
            btnSaveTestResults.Enabled = false;
            btnSaveAssetInfo.Enabled = false;
            btnBatchApplyAssetInfo.Enabled = false;
            btnSelectAllStations.Enabled = false;
            btnClearStationSelection.Enabled = false;
            btnShutDownSource.Enabled = false;
            groupProcess.Text = "测试过程区域（正在加载配置和本地数据...）";
            lblTestCountdown.Text = "倒计时：未开始";
        }

        /// <summary>
        /// 窗体显示后的延迟初始化入口。
        /// 数据库创建、资产选项、工位配置、方案树和历史状态都在这里加载；
        /// 控制PCB长连接只启动后台任务，不阻塞首屏和按钮刷新。
        /// </summary>
        private async Task InitializeMeterTestAfterShownAsync()
        {
            if (initialDataLoadStarted)
                return;

            initialDataLoadStarted = true;
            Cursor previousCursor = Cursor;
            Cursor = Cursors.WaitCursor;
            AddProcessInfoLog("系统", "MeterTest初始化", "加载中", "正在加载本地数据库、资产档案和测试方案...");

            try
            {
                // 让窗体先完成首帧绘制，再开始做本地数据库初始化。
                await Task.Yield();
                await Task.Run(accessDatabaseService.EnsureInitialized).ConfigureAwait(true);

                SuspendInitialLayout();
                try
                {
                    LoadAssetOptionDefinitions();
                    InitializeStationProcessGrid();
                    LoadAssetBarcodeSettingToInputs();
                    LoadMeterArchivesToGrid();
                    LoadMeterTestPlanConfig();
                    ApplyTestPlanView();
                }
                finally
                {
                    ResumeInitialLayout();
                }

                initialDataLoaded = true;
                groupProcess.Text = "测试过程区域";
                AddProcessLog("系统", "MeterTest初始化", true, "MeterTest 本地数据加载完成，控制PCB连接将在后台初始化。", 0);

                // 窗体首次显示后开始连接所有去重后的控制PCB端点；不await，避免继续卡住界面。
                controlPcbInitializationTask = InitializeControlPcbConnectionsAsync();
            }
            catch (Exception ex)
            {
                string message = $"MeterTest 初始化失败：{ex.Message}";
                LogMessage.Error("[MeterTest初始化] 初始化异常", ex);
                AddProcessLog("系统", "MeterTest初始化", false, message, 0);
                MessageBox.Show(message, "MeterTest", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = previousCursor;
                btnTestPlan.Enabled = initialDataLoaded;
                btnAssetInfo.Enabled = initialDataLoaded;
                btnTestResults.Enabled = initialDataLoaded;
                btnSaveTestResults.Enabled = initialDataLoaded;
                btnSelectAllStations.Enabled = initialDataLoaded;
                btnClearStationSelection.Enabled = initialDataLoaded;
                btnShutDownSource.Enabled = initialDataLoaded;
                UpdateTestExecutionButtonState();
            }
        }

        /// <summary>
        /// 绑定窗体事件。
        /// 这里统一把按钮、表格、方案树的交互行为连起来。
        /// </summary>
        private void BindEvents()
        {
            sourceControlService.StandardValuesUpdated += SourceControlService_StandardValuesUpdated;
            countdownService.StateChanged += CountdownService_StateChanged;
            btnStartTest.Click += async (_, _) => await StartSelectedTestAsync();
            btnStopTest.Click += async (_, _) => await StopRunningTestAndShutDownAsync();
            btnTestPlan.Click += async (_, _) => await RefreshTestPlanAndMeterArchiveAsync();
            btnAssetInfo.Click += (_, _) => RefreshMeterArchiveDisplay();
            btnTestResults.Click += async (_, _) => await ShowTestResultsAsync();
            btnSaveTestResults.Click += async (_, _) => await SaveCurrentTestResultsManuallyAsync();
            btnSaveAssetInfo.Click += async (_, _) => await SaveAllAssetInfoAsync();
            btnBatchApplyAssetInfo.Click += async (_, _) => await BatchApplyFirstStationAssetInfoAsync();
            btnSelectAllStations.Click += async (_, _) => await SetAllStationSelectionAsync(true);
            btnClearStationSelection.Click += async (_, _) => await SetAllStationSelectionAsync(false);
            btnShutDownSource.Click += async (_, _) => await ShutDownSourceAsync();
            cbxPlanConfigSelector.SelectedIndexChanged += async (_, _) => await SwitchPlanConfigAsync();
            rbSingleStation.CheckedChanged += async (_, _) => await ApplySingleStationSelectionRuleAsync();
            tbxBarcodeStartIndex.Leave += (_, _) => SaveBarcodeSettingFromInputs();
            tbxBarcodeEndIndex.Leave += (_, _) => SaveBarcodeSettingFromInputs();
            tbxBarcodeSecondStart.Leave += (_, _) => SaveBarcodeSettingFromInputs();
            tbxBarcodeSecondLength.Leave += (_, _) => SaveBarcodeSettingFromInputs();
            cbxBarcodeRule.SelectedIndexChanged += (_, _) => BarcodeRuleSelectionChanged();
            stationGrid.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (stationGrid.IsCurrentCellDirty)
                {
                    stationGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            stationGrid.CellClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    SelectTestLogStation(e.RowIndex);
                }
            };
            stationGrid.CellValueChanged += async (_, e) =>
            {
                if (!isUpdatingStationSelection && e.RowIndex >= 0 && e.ColumnIndex == colStationSelected.Index)
                {
                    await HandleStationSelectionChangedAsync(e.RowIndex);
                }

                if (isApplyingBarcodeExtraction)
                    return;

                if (!isLoadingStationConfig && e.RowIndex >= 0 &&
                    (e.ColumnIndex == colStationIp.Index || e.ColumnIndex == colStationPort.Index))
                {
                    SaveStationCommunicationConfig();
                }

                if (currentGridViewMode == MeterTestGridViewMode.AssetInfo &&
                    !isLoadingMeterArchive && e.RowIndex >= 0 && e.ColumnIndex == colStationBarcode.Index)
                {
                    DataGridViewRow changedRow = stationGrid.Rows[e.RowIndex];
                    ApplyBarcodeExtractionToRow(changedRow);
                    MeterArchiveData archiveSnapshot = CreateMeterArchiveSnapshot(changedRow);
                    await Task.Run(() => accessDatabaseService.SaveMeterArchive(archiveSnapshot));
                    await DeselectStationWithoutCompleteAssetAsync(changedRow);
                    RefreshSchemeTreeStatusIcons();
                    return;
                }

                if (currentGridViewMode == MeterTestGridViewMode.AssetInfo &&
                    !isLoadingMeterArchive && e.RowIndex >= 0 && IsEditableAssetColumn(e.ColumnIndex))
                {
                    DataGridViewRow changedRow = stationGrid.Rows[e.RowIndex];
                    if (e.ColumnIndex == colMeterAccessMode.Index)
                    {
                        ConfigureCurrentSpecificationCell(
                            changedRow,
                            GetCellText(changedRow, colMeterAccessMode, GetDefaultAssetOption("AccessMode")),
                            GetCellText(changedRow, colMeterCurrentSpecification, string.Empty));
                    }

                    SaveMeterArchiveFromRow(changedRow);
                    if (e.ColumnIndex == colStationMeterAddress.Index)
                    {
                        await DeselectStationWithoutCompleteAssetAsync(changedRow);
                        RefreshSchemeTreeStatusIcons();
                    }
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
                    RestoreStationDisplayForSelectedNode(loadFromAccess: false);
                    QueueSelectedNodeResultRestore();
                }
            };
            Shown += async (_, _) =>
            {
                await InitializeMeterTestAfterShownAsync();
            };
            FormClosed += async (_, _) =>
            {
                stationPowerControlCts.Cancel();
                sourceControlService.StandardValuesUpdated -= SourceControlService_StandardValuesUpdated;
                countdownService.StateChanged -= CountdownService_StateChanged;
                sourceControlService.Dispose();
                if (executionCts is null)
                {
                    communicationAddressService.EndRun();
                    bluetoothInterfaceService.EndRun();
                }
                try
                {
                    await controlPcbInitializationTask;
                }
                catch (OperationCanceledException)
                {
                }
                await controlPcbConnectionManager.DisposeAsync();
                stationTcpSessionService.Dispose();
                schemeTreeView.ImageList = null;
                schemeStatusImageList?.Dispose();
                schemeStatusImageList = null;
            };
        }

        /// <summary>
        /// 接收源控制服务每3秒采集到的标准表数据，并刷新台体信息采集区域。
        /// </summary>
        private void SourceControlService_StandardValuesUpdated(IReadOnlyDictionary<string, string> standValues)
        {
            RunOnUiThread(() => UpdateHardwareMetricsFromStandValues(standValues));
        }

        /// <summary>把统一倒计时服务的状态刷新到测试过程区域右侧红色标签。</summary>
        private void CountdownService_StateChanged(MeterTestCountdownState state)
        {
            RunOnUiThread(() =>
            {
                lblTestCountdown.ForeColor = state.IsActive
                    ? Color.Red
                    : Color.FromArgb(107, 114, 128);
                lblTestCountdown.Text = state.IsActive
                    ? $"{state.Title}：{state.RemainingSeconds}s"
                    : "倒计时：未开始";
            });
        }

        /// <summary>
        /// 切换到测试方案视图并刷新方案与结果缓存。
        /// </summary>
        private async Task RefreshTestPlanAndMeterArchiveAsync()
        {
            LoadMeterArchivesToGrid();
            bool configReloaded = ReloadMeterTestPlanConfigIfChanged();
            ApplyTestPlanView();
            // 扫码成功即表示该工位进入本轮测试范围；这里仅更新勾选状态，不触发控制PCB上下电。
            // 上下电仍由用户手动勾选/全选或执行测试前置流程负责，避免点击“测试方案”时卡住。
            SelectEligibleStationsForTestPlanWithoutPower();

            if (configReloaded)
            {
                // 方案文件可能刚被现场修改；初始化管理器只会为新增端点建连，已有端点不会重复连接。
                controlPcbInitializationTask = InitializeControlPcbConnectionsAsync();
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// 初始化方案筛选下拉框。
        /// 默认选中三相方案，保持老版本 MeterTestPlanConfig.xml 的完整测试范围。
        /// </summary>
        private void InitializePlanConfigSelector()
        {
            cbxPlanConfigSelector.BeginUpdate();
            try
            {
                cbxPlanConfigSelector.Items.Clear();
                cbxPlanConfigSelector.Items.Add(ThreePhasePlanSelectorText);
                cbxPlanConfigSelector.Items.Add(SinglePhasePlanSelectorText);
                cbxPlanConfigSelector.SelectedItem = ThreePhasePlanSelectorText;
            }
            finally
            {
                cbxPlanConfigSelector.EndUpdate();
            }
        }

        /// <summary>
        /// 根据用户选择切换方案配置文件，并立即刷新左侧方案树和右侧测试过程区域。
        /// </summary>
        private async Task SwitchPlanConfigAsync()
        {
            string selectedConfigPath = GetSelectedMeterTestConfigPath();
            if (string.Equals(configFilePath, selectedConfigPath, StringComparison.OrdinalIgnoreCase))
                return;

            configFilePath = selectedConfigPath;
            meterTestConfigLastWriteTimeUtc = null;

            if (!initialDataLoaded)
                return;

            Cursor previousCursor = Cursor;
            Cursor = Cursors.WaitCursor;
            cbxPlanConfigSelector.Enabled = false;
            AddProcessInfoLog("系统", "方案切换", "加载中", $"正在切换到：{cbxPlanConfigSelector.Text}");

            try
            {
                await Task.Yield();
                SuspendInitialLayout();
                try
                {
                    LoadMeterArchivesToGrid();
                    LoadMeterTestPlanConfig();
                    ApplyTestPlanView();
                    SelectEligibleStationsForTestPlanWithoutPower();
                    ForceSchemeTreeVisualRefresh();
                }
                finally
                {
                    ResumeInitialLayout();
                }

                controlPcbInitializationTask = InitializeControlPcbConnectionsAsync();
                AddProcessLog("系统", "方案切换", true, $"当前测试方案已切换为：{cbxPlanConfigSelector.Text}", 0);
            }
            catch (Exception ex)
            {
                LogMessage.Error("[方案切换] 切换测试方案异常", ex);
                AddProcessLog("系统", "方案切换", false, $"切换测试方案失败：{ex.Message}", 0);
                MessageBox.Show($"切换测试方案失败：{ex.Message}", "MeterTest", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = previousCursor;
                cbxPlanConfigSelector.Enabled = true;
                UpdateTestExecutionButtonState();
            }
        }

        /// <summary>
        /// 方案 XML 切换后立即刷新 TreeView 绘制。
        /// WinForms 在复杂布局挂起/恢复后偶尔会延迟重绘，这里显式刷新避免界面仍显示旧方案节点。
        /// </summary>
        private void ForceSchemeTreeVisualRefresh()
        {
            if (schemeTreeView.Nodes.Count > 0)
            {
                schemeTreeView.SelectedNode = schemeTreeView.Nodes[0];
                schemeTreeView.Nodes[0].Expand();
            }

            schemeTreeView.Invalidate();
            schemeTreeView.Update();
            schemeTreeView.Refresh();
        }

        /// <summary>把方案下拉框中的显示文本映射为实际 XML 配置路径。</summary>
        private string GetSelectedMeterTestConfigPath()
        {
            string selectedText = cbxPlanConfigSelector.SelectedItem?.ToString() ?? ThreePhasePlanSelectorText;
            string fileName = string.Equals(selectedText, SinglePhasePlanSelectorText, StringComparison.OrdinalIgnoreCase)
                ? SinglePhasePlanConfigFileName
                : ThreePhasePlanConfigFileName;
            return GetMeterTestConfigPath(fileName);
        }

        /// <summary>
        /// 切换到资产信息视图并刷新本地档案。
        /// </summary>
        private void RefreshMeterArchiveDisplay()
        {
            // 先从档案表覆盖测试视图中的临时“表位地址”，再切换资产列。
            // 地址读取结果只属于方案运行态，禁止反向写入资产信息的电表地址。
            LoadMeterArchivesToGrid();
            ApplyAssetInfoView();
            AddProcessLog("系统", "电表档案刷新", true, "电表档案已从本地数据库刷新到测试过程区域。", 0);
        }

        /// <summary>
        /// 从 XML 加载测试方案配置，并同步控制 PCB 配置到本地数据库。
        /// </summary>
        private void LoadMeterTestPlanConfig()
        {
            meterTestPlanConfig = configService.LoadOrCreate(configFilePath);
            MeterTestStationConfig stationConfig = stationConfigService.LoadOrCreate(
                stationConfigFilePath,
                MaxStationCount,
                DefaultStationIp,
                DefaultStationStartPort,
                meterTestPlanConfig);
            stationConfigService.ApplyRuntimeDeviceConfigs(meterTestPlanConfig, stationConfig);
            SaveControlPcbConfigToAccess();
            LoadAllStationResultsFromAccess();
            BuildSchemeTree();
            meterTestConfigLastWriteTimeUtc = GetFileLastWriteTimeUtc(configFilePath);
            stationConfigLastWriteTimeUtc = GetFileLastWriteTimeUtc(stationConfigFilePath);
            AddProcessLog(
                "系统",
                "配置加载",
                true,
                $"已加载测试方案：{configFilePath}\r\n已加载现场设备配置：{stationConfigFilePath}",
                0);
        }

        /// <summary>
        /// 测试方案按钮的轻量刷新入口。
        /// 只有方案XML或现场设备XML发生变化时才重建树和控制PCB连接，避免每次切回测试方案都卡顿。
        /// </summary>
        private bool ReloadMeterTestPlanConfigIfChanged()
        {
            DateTime? currentPlanWriteTime = GetFileLastWriteTimeUtc(configFilePath);
            DateTime? currentStationWriteTime = GetFileLastWriteTimeUtc(stationConfigFilePath);
            bool shouldReload =
                meterTestPlanConfig.Schemes.Count == 0 ||
                currentPlanWriteTime != meterTestConfigLastWriteTimeUtc ||
                currentStationWriteTime != stationConfigLastWriteTimeUtc;

            if (!shouldReload)
                return false;

            LoadMeterTestPlanConfig();
            return true;
        }

        /// <summary>读取文件最后修改时间；文件不存在时返回 null，方便和缓存时间戳比较。</summary>
        private static DateTime? GetFileLastWriteTimeUtc(string path)
        {
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : null;
        }

        /// <summary>
        /// 程序启动阶段按IP和端口去重连接控制PCB。
        /// 连接失败只记录状态，不在测试步骤中反复重连，避免同一端口遭到高频连接。
        /// </summary>
        private async Task InitializeControlPcbConnectionsAsync()
        {
            try
            {
                await controlPcbConnectionManager.InitializeAsync(
                    meterTestPlanConfig,
                    TimeSpan.FromSeconds(5),
                    message =>
                    {
                        LogMessage.Debug($"[控制PCB连接] {message}");
                        RunOnUiThread(() => HandleControlPcbConnectionStatus(message));
                    },
                    stationPowerControlCts.Token);
            }
            catch (OperationCanceledException) when (stationPowerControlCts.IsCancellationRequested)
            {
                // 窗体关闭时终止尚未完成的启动连接，不再写入UI。
            }
            catch (Exception ex)
            {
                LogMessage.Error("[控制PCB连接] 启动初始化异常", ex);
            }
        }

        /// <summary>
        /// 将控制PCB连接消息区分为过程提示和最终结论。
        /// “开始连接”不代表成功；底层状态变化只写Debug，避免与最终失败信息重复。
        /// </summary>
        private void HandleControlPcbConnectionStatus(string message)
        {
            string title = $"控制PCB连接 - {ExtractControlPcbConnectionTarget(message)}";
            if (message.StartsWith("控制PCB开始连接：", StringComparison.OrdinalIgnoreCase))
            {
                AddProcessInfoLog("系统", title, "连接中", message);
                return;
            }

            if (message.StartsWith("控制PCB连接成功：", StringComparison.OrdinalIgnoreCase))
            {
                AddProcessLog("系统", title, true, message, 0);
                return;
            }

            if (message.StartsWith("控制PCB连接失败：", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("控制PCB连接跳过：", StringComparison.OrdinalIgnoreCase) ||
                message.StartsWith("控制PCB应答处理异常：", StringComparison.OrdinalIgnoreCase))
            {
                AddProcessLog("系统", title, false, message, 0);
                return;
            }

            if (message.StartsWith("控制PCB连接状态变化：", StringComparison.OrdinalIgnoreCase))
            {
                // BatchTcpClientManager可能先上报底层失败状态，ConnectOnceAsync随后会给出唯一最终结论。
                return;
            }

            AddProcessInfoLog("系统", title, "提示", message);
        }

        /// <summary>从控制PCB状态文本中提取端点或配置名称，便于区分多条连接。</summary>
        private static string ExtractControlPcbConnectionTarget(string message)
        {
            int separatorIndex = message.IndexOf('：');
            if (separatorIndex < 0 || separatorIndex >= message.Length - 1)
                return "未指定端点";

            string detail = message[(separatorIndex + 1)..].Trim();
            int commaIndex = detail.IndexOf('，');
            return commaIndex > 0 ? detail[..commaIndex].Trim() : detail;
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
                    Tag = scheme,
                    ImageKey = SchemeStatusPendingImageKey,
                    SelectedImageKey = SchemeStatusPendingImageKey
                };

                foreach (MeterTestItem testItem in scheme.TestItems)
                {
                    TreeNode itemNode = new(testItem.Name)
                    {
                        Tag = testItem,
                        ImageKey = SchemeStatusPendingImageKey,
                        SelectedImageKey = SchemeStatusPendingImageKey
                    };

                    foreach (MeterTestSubItem subItem in testItem.TestSubItems)
                    {
                        if (!ShouldShowSubItemInSchemeTree(subItem))
                            continue;

                        itemNode.Nodes.Add(new TreeNode(subItem.Name)
                        {
                            Tag = subItem,
                            ImageKey = SchemeStatusPendingImageKey,
                            SelectedImageKey = SchemeStatusPendingImageKey
                        });
                    }

                    schemeNode.Nodes.Add(itemNode);
                }

                schemeTreeView.Nodes.Add(schemeNode);
            }

            if (schemeTreeView.Nodes.Count > 0)
            {
                TreeNode firstSchemeNode = schemeTreeView.Nodes[0];
                firstSchemeNode.Expand();
                schemeTreeView.SelectedNode = firstSchemeNode;
            }

            schemeTreeView.EndUpdate();
            RefreshSchemeTreeStatusIcons();
            UpdateStartButtonText();
        }

        /// <summary>
        /// 加载方案树状态图标。优先使用 png 目录中的灰、黄、绿、红灯，文件缺失时生成颜色占位灯。
        /// </summary>
        private void InitializeSchemeStatusImages()
        {
            schemeTreeView.ImageList = null;
            schemeStatusImageList?.Dispose();

            schemeStatusImageList = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(18, 18),
                TransparentColor = Color.Transparent
            };
            AddSchemeStatusImage(schemeStatusImageList, SchemeStatusPendingImageKey, "灰灯.png", Color.Gray);
            AddSchemeStatusImage(schemeStatusImageList, SchemeStatusRunningImageKey, "黄灯.png", Color.Gold);
            AddSchemeStatusImage(schemeStatusImageList, SchemeStatusPassedImageKey, "绿灯.png", Color.LimeGreen);
            AddSchemeStatusImage(schemeStatusImageList, SchemeStatusFailedImageKey, "红灯.png", Color.Red);
            schemeTreeView.ImageList = schemeStatusImageList;
            schemeTreeView.ItemHeight = Math.Max(schemeTreeView.ItemHeight, 24);
        }

        /// <summary>
        /// 向方案树 ImageList 添加状态图片。
        /// </summary>
        private static void AddSchemeStatusImage(ImageList imageList, string key, string fileName, Color fallbackColor)
        {
            foreach (string path in GetPngCandidates(fileName))
            {
                if (!File.Exists(path))
                    continue;

                try
                {
                    using Image source = Image.FromFile(path);

                    // ImageList 延迟创建底层图像句柄，因此加入集合后的 Bitmap 不能立即释放。
                    // Bitmap 的生命周期交给 ImageList，窗体关闭时随 ImageList 一起释放。
                    Bitmap bitmap = new(source, imageList.ImageSize);
                    imageList.Images.Add(key, bitmap);
                    return;
                }
                catch (ArgumentException ex)
                {
                    LogMessage.Debug($"方案树状态图标加载失败，使用颜色占位图：{path}，原因：{ex.Message}");
                }
                catch (ExternalException ex)
                {
                    LogMessage.Debug($"方案树状态图标加载失败，使用颜色占位图：{path}，原因：{ex.Message}");
                }
            }

            Bitmap fallback = new(imageList.ImageSize.Width, imageList.ImageSize.Height);
            using (Graphics graphics = Graphics.FromImage(fallback))
            using (SolidBrush brush = new(fallbackColor))
            {
                graphics.Clear(Color.Transparent);
                graphics.FillEllipse(brush, 2, 2, fallback.Width - 4, fallback.Height - 4);
            }

            // 占位图同样由 ImageList 管理生命周期，不能在这里 Dispose。
            imageList.Images.Add(key, fallback);
        }

        /// <summary>
        /// 从数据库一次性恢复所有方案节点结果到内存缓存。
        /// </summary>
        private void LoadAllStationResultsFromAccess()
        {
            foreach (MeterTestStoredStationResultData storedResult in accessDatabaseService.LoadAllStationResults())
            {
                if (storedResult.StationNo < 1 || storedResult.StationNo > MaxStationCount)
                    continue;

                StationResultKey key = new(
                    storedResult.SchemeName,
                    storedResult.TestItemName,
                    storedResult.TestSubItemName,
                    storedResult.StationNo);
                StationDisplayStateData state = storedResult.State;
                stationResultCache[key] = new StationDisplayState(
                    state.TestContent,
                    state.MeterAddress,
                    state.Result,
                    state.Time,
                    state.ResultColor,
                    state.Message);
                loadedStationResultContextKeys.Add(CreateStationResultContextKey(
                    storedResult.SchemeName,
                    storedResult.TestItemName,
                    storedResult.TestSubItemName));
            }
        }

        /// <summary>
        /// 根据当前具备测试资格的工位结果刷新方案树状态灯，并向上汇总测试项和方案状态。
        /// </summary>
        private void RefreshSchemeTreeStatusIcons()
        {
            if (schemeTreeView.IsDisposed)
                return;

            if (schemeTreeView.InvokeRequired)
            {
                try
                {
                    schemeTreeView.BeginInvoke(new Action(RefreshSchemeTreeStatusIcons));
                }
                catch (ObjectDisposedException)
                {
                    // TreeView 已释放时忽略延迟刷新请求。
                }
                catch (InvalidOperationException)
                {
                    // 窗体关闭时不再刷新树图标。
                }

                return;
            }

            if (schemeTreeStatusRefreshPending)
                return;

            schemeTreeStatusRefreshPending = true;
            if (!schemeTreeView.IsHandleCreated)
            {
                schemeTreeStatusRefreshPending = false;
                RefreshSchemeTreeStatusIconsCore();
                return;
            }

            try
            {
                schemeTreeView.BeginInvoke(new Action(() =>
                {
                    schemeTreeStatusRefreshPending = false;
                    RefreshSchemeTreeStatusIconsCore();
                }));
            }
            catch (ObjectDisposedException)
            {
                schemeTreeStatusRefreshPending = false;
            }
            catch (InvalidOperationException)
            {
                schemeTreeStatusRefreshPending = false;
            }
        }

        /// <summary>
        /// 实际执行方案树状态灯刷新。
        /// 外层 RefreshSchemeTreeStatusIcons 会合并短时间内的多次刷新请求，降低 UI 线程重绘压力。
        /// </summary>
        private void RefreshSchemeTreeStatusIconsCore()
        {
            if (schemeTreeView.IsDisposed)
                return;

            List<int> archivedEligibleStations = stationGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow && HasCompleteAssetForTest(row))
                .Select(row => Convert.ToInt32(row.Cells[colStationNo.Index].Value))
                .ToList();
            // 方案执行后的状态灯只汇总本轮实际参与的工位。
            // 否则已扫码但本轮未勾选的工位会使已完成节点一直保持灰色。
            List<int> eligibleStations = lastExecutedStationNumbers.Count > 0
                ? lastExecutedStationNumbers
                    .Where(archivedEligibleStations.Contains)
                    .Distinct()
                    .OrderBy(stationNo => stationNo)
                    .ToList()
                : archivedEligibleStations;

            schemeTreeView.BeginUpdate();
            try
            {
                foreach (TreeNode schemeNode in schemeTreeView.Nodes)
                {
                    if (schemeNode.Tag is not MeterTestScheme scheme)
                        continue;

                    foreach (TreeNode itemNode in schemeNode.Nodes)
                    {
                        if (itemNode.Tag is not MeterTestItem item)
                            continue;

                        foreach (TreeNode subItemNode in itemNode.Nodes)
                        {
                            SchemeNodeStatus status = subItemNode.Tag is MeterTestSubItem subItem && subItem.Enabled
                                ? GetSubItemNodeStatus(scheme.Name, item.Name, subItem.Name, eligibleStations)
                                : SchemeNodeStatus.Pending;
                            ApplySchemeNodeStatus(subItemNode, status);
                        }

                        ApplySchemeNodeStatus(itemNode, AggregateEnabledChildNodeStatus(itemNode));
                    }

                    ApplySchemeNodeStatus(schemeNode, AggregateEnabledChildNodeStatus(schemeNode));
                }
            }
            finally
            {
                schemeTreeView.EndUpdate();
            }
        }

        /// <summary>
        /// 计算一个测试小项的状态：测试中为黄灯，结束后任一不合格为红灯、全部合格为绿灯，其余为灰灯。
        /// </summary>
        private SchemeNodeStatus GetSubItemNodeStatus(
            string schemeName,
            string testItemName,
            string testSubItemName,
            IReadOnlyList<int> eligibleStations)
        {
            if (eligibleStations.Count == 0)
                return SchemeNodeStatus.Pending;

            bool allPassed = true;
            bool anyRunning = false;
            bool anyFailed = false;
            foreach (int stationNo in eligibleStations)
            {
                StationResultKey key = new(schemeName, testItemName, testSubItemName, stationNo);
                if (!stationResultCache.TryGetValue(key, out StationDisplayState? state))
                {
                    allPassed = false;
                    continue;
                }

                if (state.Result.Equals("测试中", StringComparison.OrdinalIgnoreCase))
                {
                    anyRunning = true;
                    allPassed = false;
                    continue;
                }

                if (state.Result.Equals("不合格", StringComparison.OrdinalIgnoreCase))
                {
                    anyFailed = true;
                    allPassed = false;
                    continue;
                }

                if (!state.Result.Equals("合格", StringComparison.OrdinalIgnoreCase))
                    allPassed = false;
            }

            if (anyRunning)
                return SchemeNodeStatus.Running;

            if (anyFailed)
                return SchemeNodeStatus.Failed;

            return allPassed ? SchemeNodeStatus.Passed : SchemeNodeStatus.Pending;
        }

        /// <summary>
        /// 汇总当前父节点下所有启用子节点的状态。
        /// </summary>
        private static SchemeNodeStatus AggregateEnabledChildNodeStatus(TreeNode parentNode)
        {
            List<TreeNode> enabledChildren = parentNode.Nodes
                .Cast<TreeNode>()
                .Where(node => node.Tag switch
                {
                    MeterTestSubItem subItem => subItem.Enabled,
                    MeterTestItem item => item.TestSubItems.Any(subItem => subItem.Enabled),
                    _ => true
                })
                .ToList();
            if (enabledChildren.Count == 0)
                return SchemeNodeStatus.Pending;

            if (enabledChildren.Any(node => GetSchemeNodeStatus(node) == SchemeNodeStatus.Running))
                return SchemeNodeStatus.Running;

            if (enabledChildren.Any(node => GetSchemeNodeStatus(node) == SchemeNodeStatus.Failed))
                return SchemeNodeStatus.Failed;

            return enabledChildren.All(node => GetSchemeNodeStatus(node) == SchemeNodeStatus.Passed)
                ? SchemeNodeStatus.Passed
                : SchemeNodeStatus.Pending;
        }

        /// <summary>
        /// 将状态枚举映射到树节点图标。
        /// </summary>
        private static void ApplySchemeNodeStatus(TreeNode node, SchemeNodeStatus status)
        {
            string imageKey = status switch
            {
                SchemeNodeStatus.Running => SchemeStatusRunningImageKey,
                SchemeNodeStatus.Passed => SchemeStatusPassedImageKey,
                SchemeNodeStatus.Failed => SchemeStatusFailedImageKey,
                _ => SchemeStatusPendingImageKey
            };
            node.ImageKey = imageKey;
            node.SelectedImageKey = imageKey;
        }

        /// <summary>
        /// 从树节点当前图标读取状态。
        /// </summary>
        private static SchemeNodeStatus GetSchemeNodeStatus(TreeNode node)
        {
            return node.ImageKey switch
            {
                SchemeStatusRunningImageKey => SchemeNodeStatus.Running,
                SchemeStatusPassedImageKey => SchemeNodeStatus.Passed,
                SchemeStatusFailedImageKey => SchemeNodeStatus.Failed,
                _ => SchemeNodeStatus.Pending
            };
        }

        /// <summary>
        /// 开始执行当前选中的方案、测试项或测试小项。
        /// 这是点击“执行方案/执行测试项/执行测试小项”后的统一入口。
        /// </summary>
        private async Task StartSelectedTestAsync()
        {
            if (isSourceShutdownInProgress)
            {
                MessageBox.Show("当前正在执行降源，请等待降源结束后再开始测试。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

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

            // 只有直接执行 Scheme 根节点才代表“整个方案”运行。
            // 单独执行某个测试项或测试小项时保留当前结果，不触发自动保存询问。
            bool isCompleteSchemeRun = selectedNode.Tag is MeterTestScheme;

            // 等待程序启动阶段的控制PCB连接任务结束；这里只等待，不会发起新的ConnectAsync。
            await controlPcbInitializationTask;

            executionCts = new CancellationTokenSource();
            currentRunId = Guid.NewGuid().ToString("N");
            currentRunStartedAt = DateTime.Now;
            lastExecutedContexts = new List<SelectedSubItemContext>();
            lastExecutedStationNumbers = new List<int>();
            lock (measurementSyncRoot)
            {
                currentRunMeasurements.Clear();
            }
            sourceControlService.BeginRun();
            basicErrorService.BeginRun();
            communicationTestService.BeginRun();
            // 每轮测试分别建立去重后的64444管理会话和蓝牙工位会话；各测试步骤只复用，不重复建连。
            communicationAddressService.BeginRun();
            stationTcpSessionService.BeginRun();
            bluetoothInterfaceService.BeginRun();
            dailyTimingService.BeginRun();
            creepingTestService.BeginRun();
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

                testContexts = FilterAssetAwareBasicErrorContexts(testContexts, selectedStations);
                if (testContexts.Count == 0)
                {
                    MessageBox.Show("当前选择的测试内容在所选工位资产类型下没有需要执行的测试点。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 前置业务链路由独立服务执行；窗体只展示子步骤结果并决定是否进入方案调度。
                MeterTestRunPreparationResult preparationResult =
                    await runPreparationService.ExecuteAsync(
                        meterTestPlanConfig,
                        selectedStations,
                        message => AddProcessInfoLog(
                            "系统/执行前准备",
                            "硬件接口",
                            "执行中",
                            message),
                        executionCts.Token);

                if (preparationResult.BenchSwitchResult is not null)
                {
                    AddProcessLog(
                        "系统",
                        "执行前台体类型切换",
                        preparationResult.BenchSwitchResult.Success,
                        preparationResult.BenchSwitchResult.Message,
                        preparationResult.ElapsedMilliseconds);
                }

                if (preparationResult.SourceInitializationResult is not null)
                {
                    AddProcessLog(
                        "系统",
                        "执行前源初始化",
                        preparationResult.SourceInitializationResult.Success,
                        preparationResult.SourceInitializationResult.Message,
                        preparationResult.ElapsedMilliseconds);
                }

                if (!preparationResult.Success)
                {
                    AddProcessLog(
                        "系统",
                        "执行前准备",
                        false,
                        preparationResult.Message,
                        preparationResult.ElapsedMilliseconds);
                    return;
                }

                lastExecutedContexts = testContexts.ToList();
                lastExecutedStationNumbers = selectedStations.Select(station => station.StationNo).ToList();

                // 正式执行前按所有启用的 ControlPcbGroup 同步一次工位电源状态：
                // 本次参与测试的工位上电压、通电流，其他映射工位断电流、下电压。
                await SynchronizeEnabledControlPcbStationPowerAsync(
                    selectedStations.Select(station => station.StationNo));

                HashSet<string> executedLedEffectSuites = new(StringComparer.OrdinalIgnoreCase);
                foreach (SelectedSubItemContext context in testContexts)
                {
                    executionCts.Token.ThrowIfCancellationRequested();
                    if (MeterTestWorkflowRouter.Resolve(context.SubItem) == MeterTestWorkflowKind.LedEffectTest)
                    {
                        string ledSuiteKey = $"{context.SchemeName}|{context.TestItemName}";
                        if (!executedLedEffectSuites.Add(ledSuiteKey))
                            continue;
                    }

                    await ExecuteTestContextAsync(context, selectedStations, executionCts.Token);
                }

                // 子项全部执行结束后，再生成“通信测试”“日计时”等父测试项的汇总结果。
                // 父节点使用独立结果记录，不覆盖树下各个测试小项的明细结果。
                SynchronizeParentTestConclusions(testContexts, selectedStations);
                await SetFinalResultIndicatorsAsync(testContexts, selectedStations, executionCts.Token);
                RestoreStationDisplayForSelectedNode();
                if (isCompleteSchemeRun)
                {
                    await PromptToSaveCompletedSchemeAsync(
                        testContexts,
                        selectedStations.Select(station => station.StationNo));
                }
            }
            catch (OperationCanceledException) when (executionCts?.IsCancellationRequested == true)
            {
                // 点击“停止测试”属于正常的用户操作，不应记录为执行异常或继续抛出。
                AddProcessLog("系统", "停止测试", true, "测试任务已由用户取消。", 0);
            }
            catch (Exception ex)
            {
                AddProcessLog("系统", "执行异常", false, ex.Message, 0);
            }
            finally
            {
                // 连接只在本轮测试内复用，结束或取消后统一关闭，下一轮重新建立干净会话。
                communicationAddressService.EndRun();
                stationTcpSessionService.EndRun();
                bluetoothInterfaceService.EndRun();
                executionCts.Dispose();
                executionCts = null;
                UpdateStartButtonText();
                UpdateTestExecutionButtonState();
            }
        }

        /// <summary>
        /// 停止当前测试并立即执行降源。
        /// 降源失败只写入界面日志和文件日志，避免测试过程中弹窗阻塞操作员。
        /// </summary>
        private async Task StopRunningTestAndShutDownAsync()
        {
            if (!btnStopTest.Enabled)
                return;

            btnStopTest.Enabled = false;
            executionCts?.Cancel();
            AddProcessLog("系统", "停止测试", true, "已发送测试取消信号，开始执行安全降源。", 0);
            await ExecuteSourceShutdownAsync("停止测试降源");
        }

        /// <summary>
        /// 整个 Scheme 执行完成后询问是否保存。
        /// 保存成功后清除方案运行态结果，使方案树和工位结果恢复为灰色待测试状态。
        /// </summary>
        private async Task PromptToSaveCompletedSchemeAsync(
            IReadOnlyList<SelectedSubItemContext> contexts,
            IEnumerable<int> stationNumbers)
        {
            DialogResult choice = MessageBox.Show(
                "当前方案的全部测试项已经执行结束，是否保存本次测试结果？\r\n\r\n"
                + "选择“否”将保留界面结果，但不会写入测试结果任务表。",
                "方案测试完成",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);
            if (choice != DialogResult.Yes)
            {
                AddProcessLog("系统", "方案结果保存", true, "用户选择不保存本次完整方案结果。", 0);
                return;
            }

            List<int> savedStationNumbers = stationNumbers.Distinct().OrderBy(station => station).ToList();
            bool saved = SaveCurrentTestResultSnapshot(
                contexts,
                savedStationNumbers,
                "Completed",
                "方案完成确认保存",
                showMessage: false);
            if (!saved)
                return;

            await TurnOffIndicatorsAfterSaveAsync(savedStationNumbers);
            string schemeName = contexts.Count > 0 ? contexts[0].SchemeName : string.Empty;
            ResetSchemeExecutionResults(schemeName);
            MessageBox.Show(
                "本次测试结果已保存，方案状态已恢复为待测试。",
                "保存完成",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>删除指定方案的运行态结论，并刷新方案树灰灯和工位待测试状态。</summary>
        private void ResetSchemeExecutionResults(string schemeName)
        {
            if (string.IsNullOrWhiteSpace(schemeName))
                return;

            accessDatabaseService.ClearStationResultsForScheme(schemeName);
            accessDatabaseService.ClearRuntimeMeasurementsForScheme(schemeName);
            List<StationResultKey> keys = stationResultCache.Keys
                .Where(key => key.SchemeName.Equals(schemeName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (StationResultKey key in keys)
            {
                stationResultCache.Remove(key);
            }

            List<string> loadedContextKeys = loadedStationResultContextKeys
                .Where(key => key.StartsWith($"{schemeName}\u001F", StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (string loadedContextKey in loadedContextKeys)
            {
                loadedStationResultContextKeys.Remove(loadedContextKey);
            }

            lock (measurementSyncRoot)
            {
                currentRunMeasurements.Clear();
            }

            lastExecutedContexts = new List<SelectedSubItemContext>();
            lastExecutedStationNumbers = new List<int>();
            ClearStationResultColumns();
            RefreshSchemeTreeStatusIcons();
            RestoreStationDisplayForSelectedNode();
            AddProcessLog("系统", "方案状态重置", true, $"方案“{schemeName}”已恢复为待测试。", 0);
        }

        /// <summary>手动将当前方案快照保存到测试结果库。</summary>
        private async Task SaveCurrentTestResultsManuallyAsync()
        {
            List<SelectedSubItemContext> selectedContexts = lastExecutedContexts.Count > 0
                ? lastExecutedContexts.ToList()
                : schemeTreeView.SelectedNode is not null
                    ? GetSelectedTestContexts(schemeTreeView.SelectedNode)
                    : new List<SelectedSubItemContext>();
            string schemeName = selectedContexts.FirstOrDefault()?.SchemeName ?? string.Empty;
            // 手动保存的语义是“当前方案快照”，不是仅保存最后一次点击执行的 TestItem。
            List<SelectedSubItemContext> contexts = string.IsNullOrWhiteSpace(schemeName)
                ? selectedContexts
                : GetEnabledSchemeContexts(schemeName);
            List<int> stationNumbers = lastExecutedStationNumbers.Count > 0
                ? lastExecutedStationNumbers.ToList()
                : stationGrid.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow && HasCompleteAssetForTest(row))
                    .Select(row => Convert.ToInt32(row.Cells[colStationNo.Index].Value))
                    .ToList();
            btnSaveTestResults.Enabled = false;
            Cursor previousCursor = Cursor;
            Cursor = Cursors.WaitCursor;
            try
            {
                bool saved = await Task.Run(() => SaveCurrentTestResultSnapshot(
                    contexts,
                    stationNumbers,
                    "ManualSaved",
                    "手动保存",
                    showMessage: false));
                if (saved)
                {
                    await TurnOffIndicatorsAfterSaveAsync(stationNumbers);
                }

                MessageBox.Show(
                    saved ? "测试结果已保存。" : "测试结果保存失败，详情请查看过程日志。",
                    "数据保存",
                    MessageBoxButtons.OK,
                    saved ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            finally
            {
                Cursor = previousCursor;
                btnSaveTestResults.Enabled = true;
            }
        }

        /// <summary>
        /// 测试结果保存成功后熄灭本轮工位 LED1-LED4。
        /// 灯光控制失败只写日志，不影响结果保存。
        /// </summary>
        private async Task TurnOffIndicatorsAfterSaveAsync(IEnumerable<int> stationNumbers)
        {
            List<int> stations = stationNumbers
                .Distinct()
                .OrderBy(station => station)
                .ToList();
            if (stations.Count == 0)
                return;

            foreach (int stationNo in stations)
            {
                try
                {
                    await indicatorLightService.TurnOffAllStationIndicatorsAsync(
                        meterTestPlanConfig,
                        controlPcbConnectionManager,
                        stationNo,
                        stationPowerControlCts.Token);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogMessage.Error($"[工位指示灯] 工位{stationNo}保存结果后熄灭LED异常。", ex);
                }
            }
        }

        /// <summary>
        /// 测试小项执行期间联动 LED3。
        /// running=true 时亮黄灯，running=false 时熄灭；失败只记录日志，不打断测试流程。
        /// </summary>
        private async Task SetTestingIndicatorsForStationsAsync(
            IEnumerable<StationCommunicationConfig> stations,
            bool running,
            CancellationToken cancellationToken)
        {
            foreach (int stationNo in stations.Select(station => station.StationNo).Distinct().OrderBy(station => station))
            {
                try
                {
                    await indicatorLightService.SetTestingIndicatorAsync(
                        meterTestPlanConfig,
                        controlPcbConnectionManager,
                        stationNo,
                        running,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogMessage.Error($"[工位指示灯] 工位{stationNo}LED3测试状态联动异常。", ex);
                }
            }
        }

        /// <summary>
        /// 设备自检小项完成后联动 LED2。
        /// 以当前设备自检小项的工位结果为准：合格绿灯，不合格红灯。
        /// </summary>
        private async Task SetSelfCheckIndicatorsAsync(
            SelectedSubItemContext context,
            IEnumerable<StationCommunicationConfig> stations,
            CancellationToken cancellationToken)
        {
            foreach (StationCommunicationConfig station in stations.OrderBy(item => item.StationNo))
            {
                bool passed = stationResultCache.TryGetValue(CreateStationResultKey(context, station.StationNo), out StationDisplayState? state) &&
                              state.Result.Equals("合格", StringComparison.OrdinalIgnoreCase);
                try
                {
                    await indicatorLightService.SetSelfCheckIndicatorAsync(
                        meterTestPlanConfig,
                        controlPcbConnectionManager,
                        station.StationNo,
                        passed,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogMessage.Error($"[工位指示灯] 工位{station.StationNo}LED2自检结果联动异常。", ex);
                }
            }
        }

        /// <summary>
        /// 整个方案执行结束后联动 LED4。
        /// 同一工位所有测试项父节点均为合格时亮绿灯，任意测试项不合格、未完成或缺失结果时亮红灯。
        /// </summary>
        private async Task SetFinalResultIndicatorsAsync(
            IReadOnlyList<SelectedSubItemContext> contexts,
            IReadOnlyList<StationCommunicationConfig> stations,
            CancellationToken cancellationToken)
        {
            List<SelectedSubItemContext> parentContexts = contexts
                .GroupBy(context => (context.SchemeName, context.TestItemName))
                .Select(group => CreateParentResultContext(group.Key.SchemeName, group.Key.TestItemName))
                .ToList();
            if (parentContexts.Count == 0)
                return;

            foreach (StationCommunicationConfig station in stations.OrderBy(item => item.StationNo))
            {
                bool passed = parentContexts.All(parentContext =>
                    stationResultCache.TryGetValue(CreateStationResultKey(parentContext, station.StationNo), out StationDisplayState? state) &&
                    state.Result.Equals("合格", StringComparison.OrdinalIgnoreCase));
                try
                {
                    await indicatorLightService.SetFinalResultIndicatorAsync(
                        meterTestPlanConfig,
                        controlPcbConnectionManager,
                        station.StationNo,
                        passed,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    LogMessage.Error($"[工位指示灯] 工位{station.StationNo}LED4最终结果联动异常。", ex);
                }
            }
        }

        /// <summary>在MeterTest主界面切换到测试结果视图，不创建新窗体。</summary>
        private async Task ShowTestResultsAsync()
        {
            currentGridViewMode = MeterTestGridViewMode.TestResults;
            groupScheme.Visible = false;
            groupProcess.Visible = false;
            resultUserControl!.Visible = true;
            resultUserControl.BringToFront();
            UpdateTestExecutionButtonState();
            await resultUserControl.RefreshDataAsync();
        }

        /// <summary>创建内嵌测试结果控件并铺满中间区域，避免以独立窗口展示结果。</summary>
        private void InitializeResultUserControl()
        {
            resultUserControl = new MeterTestResultUserControl(accessDatabaseService)
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Visible = false
            };
            middleArea.Controls.Add(resultUserControl, 0, 0);
            middleArea.SetColumnSpan(resultUserControl, 2);
        }

        /// <summary>
        /// 将当前 RunId 的小项结论、资产快照和数值结果合并保存。
        /// 部分测试、通信失败或数值缺失时使用“未完成”占位，不抛空值异常。
        /// </summary>
        private bool SaveCurrentTestResultSnapshot(
            IReadOnlyList<SelectedSubItemContext> contexts,
            IEnumerable<int> stationNumbers,
            string status,
            string saveMode,
            bool showMessage)
        {
            List<int> stations = stationNumbers.Where(number => number > 0).Distinct().OrderBy(number => number).ToList();
            if (contexts.Count == 0 || stations.Count == 0)
            {
                const string emptyMessage = "当前没有可保存的测试方案或工位数据。";
                AddProcessLog("系统", "测试结果保存", false, emptyMessage, 0);
                if (showMessage)
                {
                    MessageBox.Show(emptyMessage, "数据保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                return false;
            }

            try
            {
                string schemeName = contexts[0].SchemeName;
                bool aggregateWholeScheme = status.Equals("ManualSaved", StringComparison.OrdinalIgnoreCase);
                IReadOnlyList<MeterTestStoredStationResultData> storedResults = aggregateWholeScheme
                    ? accessDatabaseService.LoadAllStationResults()
                        .Where(result => result.SchemeName.Equals(schemeName, StringComparison.OrdinalIgnoreCase))
                        .ToList()
                    : accessDatabaseService.LoadStationResultsByRunId(currentRunId);
                if (storedResults.Count == 0)
                {
                    // 程序重启后手动保存时，允许使用该方案的最新结果缓存。
                    storedResults = accessDatabaseService.LoadAllStationResults()
                        .Where(result => result.SchemeName.Equals(schemeName, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                IReadOnlyDictionary<int, MeterArchiveData> archives =
                    accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
                List<MeterTestMeasurementData> measurements;
                lock (measurementSyncRoot)
                {
                    measurements = currentRunMeasurements.ToList();
                }
                IReadOnlyList<MeterTestMeasurementData> persistedMeasurements = aggregateWholeScheme
                    ? accessDatabaseService.LoadRuntimeMeasurementsForScheme(schemeName)
                    : accessDatabaseService.LoadRuntimeMeasurementsByRunId(currentRunId);
                measurements.AddRange(persistedMeasurements);

                MeterTestResultTaskSnapshot snapshot = MeterTestResultSnapshotBuilder.Build(
                    currentRunId,
                    schemeName,
                    currentRunStartedAt,
                    DateTime.Now,
                    status,
                    saveMode,
                    stations,
                    archives,
                    storedResults,
                    measurements,
                    contexts.Select(context => (context.TestItemName, context.SubItem.Name)));
                long taskId = accessDatabaseService.SaveTestResultTask(snapshot);
                string successMessage = $"测试任务已保存，TaskId={taskId}，{snapshot.ResultSummary}。";
                AddProcessLog("系统", "测试结果保存", true, successMessage, 0);
                if (showMessage)
                {
                    MessageBox.Show(successMessage, "数据保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                return true;
            }
            catch (Exception ex)
            {
                string failureMessage = $"测试结果保存失败：{ex.Message}";
                LogMessage.Error(failureMessage, ex);
                AddProcessLog("系统", "测试结果保存", false, failureMessage, 0);
                if (showMessage)
                {
                    MessageBox.Show(failureMessage, "数据保存", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                return false;
            }
        }

        /// <summary>
        /// 记录一个可结构化查询的测量值。
        /// 同工位、同测试项、同名称和同序号重复写入时以最新值为准。
        /// </summary>
        private void RecordMeasurement(string schemeName, MeterTestMeasurementData measurement)
        {
            lock (measurementSyncRoot)
            {
                currentRunMeasurements.RemoveAll(existing =>
                    existing.StationNo == measurement.StationNo &&
                    existing.TestItemName.Equals(measurement.TestItemName, StringComparison.OrdinalIgnoreCase) &&
                    existing.TestSubItemName.Equals(measurement.TestSubItemName, StringComparison.OrdinalIgnoreCase) &&
                    existing.MeasurementName.Equals(measurement.MeasurementName, StringComparison.OrdinalIgnoreCase) &&
                    existing.SequenceNo == measurement.SequenceNo);
                currentRunMeasurements.Add(measurement);
            }

            // 同步落库，避免分别执行多个 TestItem 时，下一次点击执行清空上一项数值。
            accessDatabaseService.SaveRuntimeMeasurement(currentRunId, schemeName, measurement);
        }

        /// <summary>
        /// 手动降源入口。若测试正在执行，先取消测试，避免后续节点在降源后重新升源。
        /// 具体厂家驱动由 SourceControlConfigs.protocol 路由，窗体不直接调用DLL。
        /// </summary>
        private async Task ShutDownSourceAsync()
        {
            if (!btnShutDownSource.Enabled)
                return;

            btnShutDownSource.Enabled = false;
            string originalText = btnShutDownSource.Text;
            btnShutDownSource.Text = "降源中...";
            try
            {
                if (executionCts is not null)
                {
                    LogMessage.Debug("[源控制][手动降源] 检测到测试正在执行，先发送取消信号。");
                    executionCts.Cancel();
                }

                await ExecuteSourceShutdownAsync("手动降源");
            }
            catch (OperationCanceledException) when (stationPowerControlCts.IsCancellationRequested)
            {
                // 窗口关闭时取消降源回调，不再访问已释放的UI。
            }
            catch (Exception ex)
            {
                string message = $"手动降源异常：{ex.Message}";
                LogMessage.Error($"[源控制][手动降源] {message}", ex);
                AddProcessLog("系统", "手动降源", false, message, 0);
            }
            finally
            {
                if (!IsDisposed && !Disposing)
                {
                    btnShutDownSource.Text = originalText;
                    btnShutDownSource.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 统一的无弹窗降源入口。
        /// 停止测试、手动降源和后续安全流程都通过该方法记录一致的结果日志。
        /// </summary>
        private async Task<bool> ExecuteSourceShutdownAsync(string operationName)
        {
            if (isSourceShutdownInProgress)
            {
                LogMessage.Debug($"[源控制][{operationName}] 已有降源任务正在执行，本次请求不重复调用驱动。");
                return false;
            }

            isSourceShutdownInProgress = true;
            btnStartTest.Enabled = false;
            try
            {
                // 降源必须使用点击时最新的运行目录配置，现场修改sourcePort后无需重启程序。
                MeterTestPlanConfig shutdownPlanConfig = configService.LoadOrCreate(configFilePath);
                MeterTestStationConfig shutdownStationConfig = stationConfigService.LoadOrCreate(
                    stationConfigFilePath,
                    MaxStationCount,
                    DefaultStationIp,
                    DefaultStationStartPort,
                    shutdownPlanConfig);
                stationConfigService.ApplyRuntimeDeviceConfigs(shutdownPlanConfig, shutdownStationConfig);
                string sourceConfigSummary = string.Join(
                    "；",
                    shutdownPlanConfig.SourceControlConfigs
                        .Where(config => config.Enabled)
                        .Select(config =>
                            $"{config.Name}[protocol={config.Protocol},sourcePort={config.SourcePort},shutMode={config.ShutMode}]"));
                LogMessage.Debug(
                    $"[源控制][{operationName}] 重新加载降源配置：现场配置文件={Path.GetFullPath(stationConfigFilePath)}；"
                    + $"启用配置={sourceConfigSummary}。");

                MeterTestSourceControlService.MeterTestSourceControlResult result =
                    await sourceControlService.ShutDownFromConfigurationAsync(
                        shutdownPlanConfig,
                        stationPowerControlCts.Token,
                        message => LogMessage.Debug($"[源控制][{operationName}] {message}")).ConfigureAwait(true);
                AddProcessLog("系统", operationName, result.Success, result.Message, 0);
                if (!result.Success)
                {
                    LogMessage.Error($"[源控制][{operationName}] {result.Message}", null);
                }

                return result.Success;
            }
            catch (OperationCanceledException) when (stationPowerControlCts.IsCancellationRequested)
            {
                return false;
            }
            catch (Exception ex)
            {
                string message = $"{operationName}异常：{ex.Message}";
                LogMessage.Error($"[源控制][{operationName}] {message}", ex);
                AddProcessLog("系统", operationName, false, message, 0);
                return false;
            }
            finally
            {
                isSourceShutdownInProgress = false;
                if (!IsDisposed && !Disposing && executionCts is null)
                {
                    UpdateTestExecutionButtonState();
                }
            }
        }

        /// <summary>
        /// 执行一个测试上下文。
        /// 台体类型和源Ini已在执行入口统一完成；此处只执行当前小项的源输出和具体测试流程。
        /// </summary>
        private async Task ExecuteTestContextAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            selectedStations = ResolveStationTcpChannelStations(context, selectedStations);
            MeterTestWorkflowKind workflowKind = MeterTestWorkflowRouter.Resolve(context.SubItem);
            await SetTestingIndicatorsForStationsAsync(selectedStations, running: true, cancellationToken);
            try
            {

            if (workflowKind == MeterTestWorkflowKind.DeviceSelfCheck)
            {
                await ExecuteDeviceSelfCheckStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.LedEffectTest)
            {
                await ExecuteLedEffectTestStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.BasicErrorPoint)
            {
                await ExecuteBasicErrorPointAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.StartingErrorPoint)
            {
                await ExecuteStartingErrorPointAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.BluetoothStationTcp)
            {
                await ExecuteBluetoothInterfaceStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            bool isCommunicationTest = IsCommunicationTestContext(context);
            bool sourceControlSucceeded = true;
            if (MeterTestWorkflowRouter.RequiresSourceControl(context.SubItem))
            {
                // 起动和潜动均由5个方案小项共同组成，此处只在第1步写入一次流程头。
                string? fiveStepSourceTitle = MeterTestWorkflowRouter.GetFiveStepSourceTitle(context.SubItem);
                if (fiveStepSourceTitle != null)
                {
                    foreach (StationCommunicationConfig station in selectedStations)
                    {
                        LogTestItemStationBlock(
                            context.TestItemName,
                            context.SubItem.Name,
                            station.StationNo,
                            "流程日志",
                            StationLogSeparator,
                            $"[流程开始] 测试项目：{context.TestItemName}",
                            $"[步骤1/5 {fiveStepSourceTitle}] 开始。");
                    }
                }

                sourceControlSucceeded = await TryExecuteSourceControlAsync(context, cancellationToken);
            }

            // 通信测试中的单个准备步骤失败后仍继续执行后续步骤，最后一定尝试地址读取。
            if (!sourceControlSucceeded && !isCommunicationTest)
            {
                return;
            }

            // StartingSource/CreepingSource 的完整结果就是“下发升源 + 20秒内标准表达标判断”，
            // 不再进入普通工位 TCP 一发一收，否则空请求会覆盖已经得到的升源结论。
            if (workflowKind is MeterTestWorkflowKind.SourceControl
                or MeterTestWorkflowKind.ConstantImaxSource
                or MeterTestWorkflowKind.ConstantVoltageSource)
            {
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.Planned)
            {
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    LogTestItemStationBlock(
                        context.TestItemName,
                        context.SubItem.Name,
                        station.StationNo,
                        "方案占位日志",
                        $"测试小项尚未接入执行器：{context.SubItem.Name}，未发送报文。");
                }

                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    false,
                    "该测试流程已加入方案树，但执行器和通信协议尚未接入，未发送报文。",
                    0);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.SerialPortServerBaudRate)
            {
                await ExecuteSerialPortServerBaudRateStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ControlPcbDailyTiming)
            {
                await ExecuteControlPcbDailyTimingStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ControlPcbCreepingStart)
            {
                await ExecuteControlPcbCreepingStartAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.CreepingWait)
            {
                await ExecuteCreepingWaitAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ControlPcbCreepingRead)
            {
                await ExecuteControlPcbCreepingReadAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.CreepingPulseJudge)
            {
                ExecuteCreepingPulseJudgeStep(context, selectedStations);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ControlPcbStartingError)
            {
                await ExecuteControlPcbStartingErrorStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.StartingTimeWait)
            {
                await ExecuteStartingTimeWaitAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ControlPcbStartingErrorRead)
            {
                await ExecuteControlPcbStartingErrorReadStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.StartingErrorJudge)
            {
                ExecuteStartingErrorJudgeStep(context, selectedStations);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ConstantEnergyRead)
            {
                await ExecuteConstantEnergyReadAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ControlPcbWalkingStart)
            {
                await ExecuteControlPcbWalkingStartAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ConstantWait)
            {
                await ExecuteConstantWaitAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ControlPcbWalkingStop)
            {
                await ExecuteControlPcbWalkingStopAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ControlPcbWalkingRead)
            {
                await ExecuteControlPcbWalkingReadAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (workflowKind == MeterTestWorkflowKind.ConstantResultJudge)
            {
                ExecuteConstantResultJudgeStep(context, selectedStations);
                return;
            }

            List<Task> stationTasks = selectedStations
                .Select(station => ExecuteStationSubItemAsync(station, context, cancellationToken))
                .ToList();

            await Task.WhenAll(stationTasks);
            }
            finally
            {
                await SetTestingIndicatorsForStationsAsync(selectedStations, running: false, cancellationToken);
            }
        }

        /// <summary>
        /// 根据测试小项stationTcpChannel配置切换485通信通道。
        /// 通信测试、通信测试-2等工位TCP流程都通过该能力从MeterTestStationConfig.xml读取指定485通道的IP/Port；
        /// 为空时保持测试过程区域当前选中工位端点。
        /// </summary>
        private List<StationCommunicationConfig> ResolveStationTcpChannelStations(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations)
        {
            string channelName = context.SubItem.StationTcpChannel.Trim();
            if (string.IsNullOrWhiteSpace(channelName))
                return selectedStations;

            MeterTestStationConfig stationConfig = stationConfigService.LoadOrCreate(
                stationConfigFilePath,
                MaxStationCount,
                DefaultStationIp,
                DefaultStationStartPort,
                meterTestPlanConfig);
            MeterTestStationTcpChannel? channel = stationConfig.StationTcpChannels
                .FirstOrDefault(item => item.Enabled &&
                                        item.Channel.Equals(channelName, StringComparison.OrdinalIgnoreCase));
            if (channel is null)
            {
                throw new InvalidOperationException($"测试小项“{context.SubItem.Name}”指定的485通道不存在或未启用：{channelName}。");
            }

            Dictionary<int, MeterTestStationCommunication> stationMap = channel.Stations
                .GroupBy(item => item.StationNo)
                .ToDictionary(group => group.Key, group => group.First());
            List<StationCommunicationConfig> remappedStations = new();
            foreach (StationCommunicationConfig station in selectedStations)
            {
                if (!stationMap.TryGetValue(station.StationNo, out MeterTestStationCommunication? channelStation))
                {
                    throw new InvalidOperationException($"485通道“{channelName}”未配置工位{station.StationNo}的IP和端口。");
                }

                remappedStations.Add(station with
                {
                    Ip = channelStation.Ip.Trim(),
                    Port = channelStation.Port
                });
            }

            LogMessage.Debug(
                $"[工位485通道] {context.TestItemName}/{context.SubItem.Name} 使用通道={channelName}，"
                + $"工位={string.Join(",", remappedStations.Select(item => $"{item.StationNo}@{item.Ip}:{item.Port}"))}。");
            return remappedStations;
        }

        /// <summary>
        /// 调度通信测试中的串口服务器步骤。
        /// 按 IP 去重、F3/F1 收发、参数核对和步骤日志分类全部由通信测试服务负责。
        /// </summary>
        private async Task ExecuteSerialPortServerBaudRateStepAsync(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestCommunicationBatchStepResult result =
                await communicationTestService.ExecuteSerialPortServerStepAsync(
                    context,
                    selectedStations,
                    (stationNo, lines) => LogTestItemStationBlock(
                        context.TestItemName,
                        context.SubItem.Name,
                        stationNo,
                        "串口服务器日志",
                        lines),
                    cancellationToken);

            RunOnUiThread(() =>
            {
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    MeterTestCommunicationStationResult stationResult =
                        result.StationResults.TryGetValue(
                            station.StationNo,
                            out MeterTestCommunicationStationResult? resolved)
                                ? resolved
                                : new MeterTestCommunicationStationResult(
                                    station.StationNo,
                                    false,
                                    string.Empty,
                                    "串口服务器流程未返回当前工位结果。",
                                    result.ElapsedMilliseconds);
                    ApplyStationExecutionResult(
                        station.StationNo,
                        context,
                        stationResult.Passed,
                        stationResult.Message);
                }

                RestoreStationDisplayForSelectedNode();
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    result.Passed,
                    result.Message,
                    result.ElapsedMilliseconds);
            });
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
            RunOnUiThread(() =>
            {
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    UpdateStationRunningState(station.StationNo, context);
                }
            });

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
            string? fiveStepPrefix = MeterTestWorkflowRouter.IsStartingSource(context.SubItem)
                ? "[步骤1/5 升源（启动电流）]"
                : MeterTestWorkflowRouter.IsCreepingSource(context.SubItem)
                    ? "[步骤1/5 升源（潜动电压）]"
                    : null;
            string sourceBatchKey = $"{currentRunId}|{context.SchemeName}|{context.TestItemName}|{context.SubItem.Name}";
            MeterTestSourceControlService.MeterTestSourceControlResult result = await sourceControlService.ExecuteBatchOnceAsync(
                sourceBatchKey,
                meterTestPlanConfig,
                context.SubItem,
                sourceStations,
                meterArchives,
                cancellationToken,
                message =>
                {
                    foreach (StationCommunicationConfig station in selectedStations)
                    {
                        LogTestItemStationBlock(
                            context.TestItemName,
                            context.SubItem.Name,
                            station.StationNo,
                            "源控制日志",
                            fiveStepPrefix == null ? message : $"{fiveStepPrefix} {message}");
                    }
                });

            if (fiveStepPrefix != null)
            {
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    LogTestItemStationBlock(
                        context.TestItemName,
                        context.SubItem.Name,
                        station.StationNo,
                        "源控制日志",
                        $"{fiveStepPrefix} 结论：{(result.Success ? "合格" : "不合格")}，{result.Message}");
                }
            }

            RunOnUiThread(() =>
            {
                if (result.StandValues is not null)
                {
                    UpdateHardwareMetricsFromStandValues(result.StandValues);
                }

                if (MeterTestWorkflowRouter.IsStartingSource(context.SubItem) ||
                    MeterTestWorkflowRouter.IsCreepingSource(context.SubItem) ||
                    MeterTestWorkflowRouter.Is(context.SubItem, MeterTestWorkflowKind.ConstantImaxSource) ||
                    MeterTestWorkflowRouter.Is(context.SubItem, MeterTestWorkflowKind.ConstantVoltageSource))
                {
                    Dictionary<int, bool> stationResults = selectedStations
                        .ToDictionary(station => station.StationNo, _ => result.Success);
                    SaveStationConclusions(context, selectedStations, stationResults, result.Message);
                    RestoreStationDisplayForSelectedNode();
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

        /// <summary>
        /// 执行一个有功基本误差测试点。
        /// 台体类型已在执行入口统一切换；窗体只负责参数收集和结果回填，内部五步由独立服务完成。
        /// </summary>
        private async Task ExecuteBasicErrorPointAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            foreach (StationCommunicationConfig station in selectedStations)
            {
                UpdateStationRunningState(station.StationNo, context);
            }

            MeterTestBasicErrorWorkflowResult workflowResult = await basicErrorService.ExecutePointAsync(
                currentRunId,
                meterTestPlanConfig,
                context,
                selectedStations,
                (stationNo, message) => LogBasicErrorStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    message),
                cancellationToken);
            MeterTestBasicErrorExecutionResult result = workflowResult.ExecutionResult;

            RunOnUiThread(() =>
            {
                if (result.StandValues is not null)
                {
                    UpdateHardwareMetricsFromStandValues(result.StandValues);
                }

                foreach (MeterTestMeasurementData measurement in workflowResult.Measurements)
                {
                    RecordMeasurement(context.SchemeName, measurement);
                }

                foreach (StationCommunicationConfig station in selectedStations)
                {
                    MeterTestBasicErrorStationResult stationResult = result.StationResults.TryGetValue(
                        station.StationNo,
                        out MeterTestBasicErrorStationResult? resolvedResult)
                            ? resolvedResult
                            : MeterTestBasicErrorStationResult.Fail(station.StationNo, "基本误差流程未返回工位结果。");
                    ApplyStationExecutionResult(
                        station.StationNo,
                        context,
                        stationResult.Success,
                        stationResult.Message);
                }

                RestoreStationDisplayForSelectedNode();
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    result.Success,
                    result.Message,
                    workflowResult.ElapsedMilliseconds);
            });
        }

        /// <summary>
        /// 执行一个起动误差测试点。
        /// 方案树只展示“正有/反有-H-1.0-1U-Ist”，内部仍复用原有起动五步流程。
        /// </summary>
        private async Task ExecuteStartingErrorPointAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            await startingErrorService.ExecutePointAsync(
                context,
                selectedStations,
                TryExecuteSourceControlAsync,
                (stationNo, runningContext) => RunOnUiThread(() => UpdateStationRunningState(stationNo, runningContext)),
                (stationNo, lines) => LogStartingErrorStationBlock(context.TestItemName, stationNo, lines),
                (group, target, lines) => LogControlPcbStationBlock(context.TestItemName, group, target, lines),
                (stationNo, resultContext, passed, message) =>
                    RunOnUiThread(() => ApplyStationExecutionResult(stationNo, resultContext, passed, message)),
                (scope, name, passed, message, elapsed) => RunOnUiThread(() => AddProcessLog(scope, name, passed, message, elapsed)),
                subItem => GetEnabledControlPcbGroups(subItem),
                () => RunOnUiThread(() => RestoreStationDisplayForSelectedNode()),
                measurement => RecordMeasurement(context.SchemeName, measurement),
                cancellationToken);
        }

        /// <summary>
        /// 执行一个国网智芯蓝牙接口检测小项。
        /// 每个工位只使用BluetoothTcpChannels中的专用IP/Port；同一轮的四个蓝牙步骤复用TCP连接。
        /// 资产信息中的IP/Port属于485通信，此流程不会回退使用。
        /// </summary>
        private async Task ExecuteBluetoothInterfaceStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            foreach (StationCommunicationConfig station in selectedStations)
            {
                UpdateStationRunningState(station.StationNo, context);
                LogBluetoothStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    station.StationNo,
                    StationLogSeparator,
                    $"开始蓝牙检测步骤：{context.SubItem.Name}，工位={station.StationNo}；"
                    + "蓝牙专用端点由服务从 BluetoothTcpChannels 唯一映射解析。");
            }

            IReadOnlyDictionary<int, MeterTestBluetoothStationResult> results =
                await bluetoothInterfaceService.ExecuteConfiguredStepAsync(
                    meterTestPlanConfig,
                    context.SubItem,
                    selectedStations,
                    (stationNo, message) => LogBluetoothStationBlock(
                        context.TestItemName,
                        context.SubItem.Name,
                        stationNo,
                        message),
                    cancellationToken);

            bool allPassed = true;
            foreach (StationCommunicationConfig station in selectedStations)
            {
                MeterTestBluetoothStationResult result = results.TryGetValue(
                    station.StationNo,
                    out MeterTestBluetoothStationResult? resolvedResult)
                        ? resolvedResult
                        : MeterTestBluetoothStationResult.Fail(station.StationNo, "蓝牙检测服务未返回工位结果。");
                allPassed &= result.Success;
                LogBluetoothStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    station.StationNo,
                    $"蓝牙步骤结束：{context.SubItem.Name}，结论={(result.Success ? "合格" : "不合格")}。",
                    StationLogSeparator);
                RunOnUiThread(() => ApplyStationExecutionResult(station, context, result.Message, result.Success));
                RunOnUiThread(() => AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}/工位{station.StationNo}",
                    context.SubItem.Name,
                    result.Success,
                    result.Message,
                    Math.Max(0, Environment.TickCount64 - startTicks)));
            }

            RunOnUiThread(() =>
            {
                RestoreStationDisplayForSelectedNode();
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    allPassed,
                    allPassed ? "所有选中工位蓝牙步骤完成。" : "蓝牙步骤存在失败工位。",
                    Math.Max(0, Environment.TickCount64 - startTicks));
            });
        }

        /// <summary>返回去除首尾空格后的文本；输入为空时使用指定默认值。</summary>
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
        /// 根据当前视图和测试状态统一刷新“开始测试/停止测试”按钮。
        /// 资产信息和测试结果视图只用于查看维护，不允许直接启动或停止测试。
        /// </summary>
        private void UpdateTestExecutionButtonState()
        {
            bool isTestPlanView = currentGridViewMode == MeterTestGridViewMode.TestPlan;
            bool isRunning = executionCts is not null;

            btnStartTest.Enabled = initialDataLoaded && isTestPlanView && !isRunning && !isSourceShutdownInProgress;
            btnStopTest.Enabled = initialDataLoaded && isTestPlanView && isRunning;
        }

        /// <summary>
        /// 向过程区表格和右侧测试日志框同时追加一行日志。
        /// </summary>
        private void AddProcessLog(string scope, string testName, bool passed, string message, long elapsedMilliseconds)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => AddProcessLog(scope, testName, passed, message, elapsedMilliseconds)));
                }
                catch (ObjectDisposedException)
                {
                    // 控件已释放时不再追加日志，避免关闭窗口阶段再次抛异常。
                }
                catch (InvalidOperationException)
                {
                    // 窗体正在关闭时，异步 UI 回调可能已经无法投递，直接忽略即可。
                }

                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string resultText = passed ? "合格" : "不合格";
            processGrid.Rows.Add(
                processGrid.Rows.Count + 1,
                $"{scope} - {testName}",
                resultText,
                $"{timestamp} / {elapsedMilliseconds} ms");

            if (processGrid.Rows.Count > 0)
            {
                DataGridViewRow row = processGrid.Rows[processGrid.Rows.Count - 1];
                row.Height = processGrid.RowTemplate.Height;
                row.MinimumHeight = processGrid.RowTemplate.Height;
                row.Cells[colProcessResult.Index].Style.ForeColor = passed ? Color.FromArgb(22, 101, 52) : Color.Red;
                row.Cells[colProcessItem.Index].ToolTipText = message;
                row.Cells[colProcessTime.Index].ToolTipText = message;
            }

            int? stationNo = TryExtractSingleStationNo(scope)
                ?? TryExtractSingleStationNo(message);
            StoreTestLogEntry(
                stationNo,
                $"[{timestamp}] [{resultText}] {scope} - {testName} ({elapsedMilliseconds} ms)"
                + Environment.NewLine
                + message
                + Environment.NewLine
                + StationLogSeparator
                + Environment.NewLine);
        }

        /// <summary>
        /// 向右侧日志追加不带合格结论的过程信息，不在底部结果表新增行。
        /// 用于“连接中”等尚未形成最终判断的状态。
        /// </summary>
        private void AddProcessInfoLog(string scope, string testName, string status, string message)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => AddProcessInfoLog(scope, testName, status, message)));
                }
                catch (ObjectDisposedException)
                {
                    // 窗体关闭时不再投递过程消息。
                }
                catch (InvalidOperationException)
                {
                    // 窗体句柄销毁后忽略后台状态回调。
                }

                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            StoreTestLogEntry(
                null,
                $"[{timestamp}] [{status}] {scope} - {testName}"
                + Environment.NewLine
                + message
                + Environment.NewLine
                + StationLogSeparator
                + Environment.NewLine);
        }

        /// <summary>
        /// 从SQLite加载资产下拉候选项。Designer不再维护任何业务候选值。
        /// </summary>
        private void LoadAssetOptionDefinitions()
        {
            assetOptionCache.Clear();
            foreach ((string category, string scope) in new[]
                     {
                         ("MeterType", ""),
                         ("AccessMode", ""),
                         ("Voltage", ""),
                         ("BasicCurrent", ""),
                         ("CurrentSpecification", "Direct"),
                         ("CurrentSpecification", "Transformer"),
                         ("ActiveClass", ""),
                         ("ActiveConstant", ""),
                         ("ReactiveClass", ""),
                         ("ReactiveConstant", ""),
                         ("BaudRate", "")
                     })
            {
                assetOptionCache[CreateAssetOptionKey(category, scope)] =
                    accessDatabaseService.LoadAssetOptions(category, scope);
            }

            SetComboColumnItems(colMeterType, GetAssetOptionValues("MeterType"));
            SetComboColumnItems(colMeterAccessMode, GetAssetOptionValues("AccessMode"));
            SetComboColumnItems(colMeterActiveClass, GetAssetOptionValues("ActiveClass"));
            SetComboColumnItems(colMeterReactiveClass, GetAssetOptionValues("ReactiveClass"));
            SetComboColumnItems(colMeterBaudRate, GetAssetOptionValues("BaudRate"));
        }

        /// <summary>从已加载的数据库资产选项缓存中取得指定类别和适用范围的候选值。</summary>
        private IReadOnlyList<string> GetAssetOptionValues(string category, string scope = "")
        {
            return assetOptionCache.TryGetValue(CreateAssetOptionKey(category, scope), out IReadOnlyList<MeterTestAssetOptionData>? options)
                ? options.Select(option => option.Value).ToList()
                : Array.Empty<string>();
        }

        /// <summary>取得数据库标记的默认资产选项；没有显式默认值时返回第一项。</summary>
        private string GetDefaultAssetOption(string category, string scope = "")
        {
            if (!assetOptionCache.TryGetValue(CreateAssetOptionKey(category, scope), out IReadOnlyList<MeterTestAssetOptionData>? options) ||
                options.Count == 0)
            {
                return string.Empty;
            }

            return options.FirstOrDefault(option => option.IsDefault)?.Value ?? options[0].Value;
        }

        /// <summary>为资产选项类别和适用范围构造不会发生文本拼接冲突的缓存键。</summary>
        private static string CreateAssetOptionKey(string category, string scope)
        {
            return $"{category}\u001f{scope}";
        }

        /// <summary>用数据库候选值重新绑定资产 DataGridView 下拉列。</summary>
        private static void SetComboColumnItems(DataGridViewComboBoxColumn column, IReadOnlyList<string> values)
        {
            column.Items.Clear();
            column.Items.AddRange(values.Cast<object>().ToArray());
        }

        /// <summary>
        /// 初始化工位表格。
        /// 默认补齐 1-48 工位，并预置通信参数和档案参数。
        /// </summary>
        private void InitializeStationProcessGrid()
        {
            isLoadingStationConfig = true;
            try
            {
                stationGrid.Rows.Clear();
                MeterTestPlanConfig fallbackPlanConfig = configService.LoadOrCreate(configFilePath);
                MeterTestStationConfig config = stationConfigService.LoadOrCreate(
                    stationConfigFilePath,
                    MaxStationCount,
                    DefaultStationIp,
                    DefaultStationStartPort,
                    fallbackPlanConfig);

                foreach (MeterTestStationCommunication station in config.Stations)
                {
                    string accessMode = GetDefaultAssetOption("AccessMode");
                    stationGrid.Rows.Add(
                        false,
                        station.StationNo,
                        string.IsNullOrWhiteSpace(station.Ip) ? DefaultStationIp : station.Ip,
                        station.Port <= 0 ? DefaultStationStartPort + station.StationNo - 1 : station.Port,
                        string.Empty,
                        ReadMeterAddressTestName,
                        GetDefaultAssetOption("MeterType"),
                        accessMode,
                        GetDefaultAssetOption("Voltage"),
                        GetDefaultAssetOption("BasicCurrent"),
                        GetDefaultAssetOption("CurrentSpecification", "Direct"),
                        GetDefaultAssetOption("ActiveClass"),
                        GetDefaultAssetOption("ActiveConstant"),
                        GetDefaultAssetOption("ReactiveClass"),
                        GetDefaultAssetOption("ReactiveConstant"),
                        string.Empty,
                        GetDefaultAssetOption("BaudRate"),
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
            SuspendLayout();
            groupProcess.SuspendLayout();
            processLayout.SuspendLayout();
            stationSelectionPanel.SuspendLayout();
            stationGrid.SuspendLayout();
            try
            {
                currentGridViewMode = MeterTestGridViewMode.TestPlan;
                resultUserControl?.Hide();
                groupProcess.Visible = true;
                groupProcess.Text = "测试过程区域";
                SetSchemeAreaVisibility(true);
                ApplyStationAssetVisibility(showAllStations: false);

                rbMultiStation.Visible = true;
                rbSingleStation.Visible = true;
                btnSelectAllStations.Visible = true;
                btnClearStationSelection.Visible = true;
                btnShutDownSource.Visible = true;
                btnSaveTestResults.Visible = true;
                btnSaveAssetInfo.Visible = false;
                btnBatchApplyAssetInfo.Visible = false;
                btnSaveAssetInfo.Enabled = false;
                btnBatchApplyAssetInfo.Enabled = false;
                lblBarcodeRule.Visible = false;
                cbxBarcodeRule.Visible = false;
                lblBarcodeStartIndex.Visible = false;
                tbxBarcodeStartIndex.Visible = false;
                lblBarcodeEndIndex.Visible = false;
                tbxBarcodeEndIndex.Visible = false;
                lblBarcodeSecondStart.Visible = false;
                tbxBarcodeSecondStart.Visible = false;
                lblBarcodeSecondLength.Visible = false;
                tbxBarcodeSecondLength.Visible = false;
                    processGrid.Visible = true;
                countdownPanel.Visible = true;
                SetProcessLogVisibility(true);

                SetProcessLayoutRows(66F, 72F, 28F);
                SetStationColumnVisibility(
                    showSelection: true,
                    showCommunication: false,
                    showAsset: false,
                    showBarcode: false,
                    showTest: true,
                    showMeterAddress: true,
                    showResult: true);
                ApplyColumnDisplayOrder(
                    colStationSelected,
                    colStationNo,
                    colStationTestContent,
                    colStationMeterAddress,
                    colStationResult,
                    colStationTime);
                ApplyTestPlanColumnWidths();
                SetStationColumnEditState(assetEditable: false);
                UpdateMeterAddressColumnHeader(false);
                RestoreStationDisplayForSelectedNode(loadFromAccess: false);
                UpdateStartButtonText();
                UpdateTestExecutionButtonState();
            }
            finally
            {
                stationGrid.ResumeLayout();
                stationSelectionPanel.ResumeLayout(true);
                ResetStationSelectionPanelLayout();
                processLayout.ResumeLayout(false);
                groupProcess.ResumeLayout(false);
                ResumeLayout(true);
            }

            QueueSelectedNodeResultRestore();
        }

        /// <summary>
        /// 切换到资产信息视图。
        /// 显示工位通信和电表档案可维护列。
        /// </summary>
        private void ApplyAssetInfoView()
        {
            currentGridViewMode = MeterTestGridViewMode.AssetInfo;
            resultUserControl?.Hide();
            groupProcess.Visible = true;
            groupProcess.Text = "资产信息维护";
            SetSchemeAreaVisibility(false);
            ApplyStationAssetVisibility(showAllStations: true);

            rbMultiStation.Visible = false;
            rbSingleStation.Visible = false;
            btnSelectAllStations.Visible = false;
            btnClearStationSelection.Visible = false;
            btnShutDownSource.Visible = false;
            btnSaveTestResults.Visible = false;
            btnSaveAssetInfo.Visible = true;
            btnBatchApplyAssetInfo.Visible = true;
            btnSaveAssetInfo.Enabled = initialDataLoaded;
            btnBatchApplyAssetInfo.Enabled = initialDataLoaded;
            lblBarcodeRule.Visible = true;
            cbxBarcodeRule.Visible = true;
            UpdateBarcodeRuleInputState();
            ResetStationSelectionPanelLayout();
            processGrid.Visible = false;
            countdownPanel.Visible = false;
            SetProcessLogVisibility(false);

            SetProcessLayoutRows(66F, 100F, 0F);
            SetStationColumnVisibility(
                showSelection: false,
                showCommunication: true,
                showAsset: true,
                showBarcode: true,
                showTest: false,
                showMeterAddress: true,
                showResult: false);
            ApplyColumnDisplayOrder(
                colStationNo,
                colStationBarcode,
                colStationMeterAddress,
                colMeterBaudRate,
                colStationIp,
                colStationPort,
                colMeterType,
                colMeterAccessMode,
                colMeterVoltage,
                colMeterCurrent,
                colMeterCurrentSpecification,
                colMeterActiveClass,
                colMeterActiveConstant,
                colMeterReactiveClass,
                colMeterReactiveConstant);
            ApplyAssetInfoColumnWidths();
            SetStationColumnEditState(assetEditable: true);
            UpdateMeterAddressColumnHeader(true);
            UpdateStartButtonText();
            UpdateTestExecutionButtonState();
        }

        /// <summary>
        /// 复位操作区 FlowLayoutPanel 的滚动位置并强制重新布局。
        /// 资产信息视图会显示较多条码规则控件并产生横向滚动；切回测试方案时必须清零滚动，
        /// 否则多工位/全选/降源等控件会被保留的滚动偏移挤到一起。
        /// </summary>
        private void ResetStationSelectionPanelLayout()
        {
            if (stationSelectionPanel.IsDisposed)
                return;

            try
            {
                stationSelectionPanel.AutoScrollPosition = Point.Empty;
            }
            catch (InvalidOperationException)
            {
                // 面板句柄销毁或布局正在释放时忽略，后续 PerformLayout 不再执行。
                return;
            }

            stationSelectionPanel.PerformLayout();
            stationSelectionPanel.Invalidate();
        }

        /// <summary>
        /// 控制左侧方案区域是否显示。
        /// 资产信息视图隐藏方案树并让资产维护区域占满；测试方案视图恢复 20%/80% 布局。
        /// </summary>
        private void SetSchemeAreaVisibility(bool visible)
        {
            middleArea.SuspendLayout();
            try
            {
                groupScheme.Visible = visible;
                middleArea.ColumnStyles[0].SizeType = SizeType.Percent;
                middleArea.ColumnStyles[1].SizeType = SizeType.Percent;
                middleArea.ColumnStyles[0].Width = visible ? 20F : 0F;
                middleArea.ColumnStyles[1].Width = visible ? 80F : 100F;
            }
            finally
            {
                middleArea.ResumeLayout(true);
            }
        }

        /// <summary>
        /// 根据资产完整性控制工位行显示。
        /// 资产信息视图显示全部48工位；测试方案视图只显示已扫码且已生成电表地址的工位。
        /// </summary>
        private void ApplyStationAssetVisibility(bool showAllStations)
        {
            stationGrid.CurrentCell = null;
            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (!row.IsNewRow)
                {
                    row.Visible = showAllStations || HasCompleteAssetForTest(row);
                }
            }
        }

        /// <summary>
        /// 判断工位是否具备测试资格：只要已经有电表地址，或能从条形码按当前规则提取地址，就允许参与测试。
        /// </summary>
        private bool HasCompleteAssetForTest(DataGridViewRow row)
        {
            if (row.IsNewRow)
                return false;

            string barcode = GetCellText(row, colStationBarcode, string.Empty).Trim();
            string meterAddress = GetCellText(row, colStationMeterAddress, string.Empty).Trim();
            return !string.IsNullOrWhiteSpace(meterAddress)
                || TryExtractMeterAddressFromBarcode(barcode, out _);
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
        /// 设置测试日志面板的显示状态，并同步调整中间区域的左右比例。
        /// 测试方案视图显示日志；资产信息视图让工位表独占宽度。
        /// </summary>
        private void SetProcessLogVisibility(bool visible)
        {
            groupTestLog.Visible = visible;
            processLayout.ColumnStyles[0].SizeType = SizeType.Percent;
            processLayout.ColumnStyles[1].SizeType = SizeType.Percent;
            processLayout.ColumnStyles[0].Width = visible ? 72F : 100F;
            processLayout.ColumnStyles[1].Width = visible ? 28F : 0F;

            if (visible)
            {
                RefreshTestLogForStation(selectedTestLogStationNo);
            }
        }

        /// <summary>
        /// 控制工位表在当前视图下显示哪些列。
        /// </summary>
        private void SetStationColumnVisibility(
            bool showSelection,
            bool showCommunication,
            bool showAsset,
            bool showBarcode,
            bool showTest,
            bool showMeterAddress,
            bool showResult)
        {
            colStationSelected.Visible = showSelection;
            colStationNo.Visible = true;
            colStationIp.Visible = showCommunication;
            colStationPort.Visible = showCommunication;
            colStationBarcode.Visible = showBarcode;
            colStationTestContent.Visible = showTest;
            colMeterType.Visible = showAsset;
            colMeterAccessMode.Visible = showAsset;
            colMeterVoltage.Visible = showAsset;
            colMeterCurrent.Visible = showAsset;
            colMeterCurrentSpecification.Visible = showAsset;
            colMeterActiveClass.Visible = showAsset;
            colMeterActiveConstant.Visible = showAsset;
            colMeterReactiveClass.Visible = showAsset;
            colMeterReactiveConstant.Visible = showAsset;
            colStationMeterAddress.Visible = showMeterAddress;
            colMeterBaudRate.Visible = showAsset;
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
            SetFixedColumnWidth(colStationIp, 250);
            SetFixedColumnWidth(colStationPort, 100);
            SetFixedColumnWidth(colStationBarcode, 300);
            SetFixedColumnWidth(colStationMeterAddress, 200);
            SetFixedColumnWidth(colMeterBaudRate, 150);
            SetFixedColumnWidth(colMeterType, 150);
            SetFixedColumnWidth(colMeterAccessMode, 150);
            SetFixedColumnWidth(colMeterVoltage, 150);
            SetFixedColumnWidth(colMeterCurrent, 150);
            SetFixedColumnWidth(colMeterCurrentSpecification, 220);
            SetFixedColumnWidth(colMeterActiveClass, 150);
            SetFixedColumnWidth(colMeterActiveConstant, 150);
            SetFixedColumnWidth(colMeterReactiveClass, 150);
            SetFixedColumnWidth(colMeterReactiveConstant, 150);
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
        /// 按传入顺序排列当前视图需要显示的列，未传入的隐藏列统一排到后面。
        /// 从后向前移动到首列，可以避免连续设置 DisplayIndex 时发生相互挤位。
        /// </summary>
        private static void ApplyColumnDisplayOrder(params DataGridViewColumn[] columns)
        {
            for (int index = columns.Length - 1; index >= 0; index--)
            {
                columns[index].DisplayIndex = 0;
            }
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
            colMeterCurrentSpecification.ReadOnly = !assetEditable;
            colMeterActiveClass.ReadOnly = !assetEditable;
            colMeterActiveConstant.ReadOnly = !assetEditable;
            colMeterReactiveClass.ReadOnly = !assetEditable;
            colMeterReactiveConstant.ReadOnly = !assetEditable;
            colStationBarcode.ReadOnly = !assetEditable;
            colStationMeterAddress.ReadOnly = !assetEditable;
            colMeterBaudRate.ReadOnly = !assetEditable;
            colStationResult.ReadOnly = true;
            colStationTime.ReadOnly = true;
        }

        /// <summary>
        /// 保存全部资产信息。
        /// </summary>
        private Task SaveAllAssetInfoAsync()
        {
            return SaveAllAssetInfoAsync(showMessage: true);
        }

        /// <summary>
        /// 保存全部资产信息；是否弹出提示由调用方决定。
        /// </summary>
        private async Task SaveAllAssetInfoAsync(bool showMessage)
        {
            stationGrid.EndEdit();
            SaveStationCommunicationConfig();
            SaveBarcodeSettingFromInputs();

            List<MeterArchiveData> archives = stationGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow)
                .Select(CreateMeterArchiveSnapshot)
                .ToList();

            await Task.Run(() => accessDatabaseService.SaveMeterArchives(archives));

            AddProcessLog("系统", "资产信息保存", true, "资产信息已保存到本地数据库。", 0);
            if (showMessage)
            {
                MessageBox.Show("资产信息已保存。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 用 1 工位的参数批量覆盖 2-48 工位。
        /// </summary>
        private async Task BatchApplyFirstStationAssetInfoAsync()
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

                    CopyCellValue(sourceRow, row, colMeterType);
                    CopyCellValue(sourceRow, row, colMeterAccessMode);
                    CopyCellValue(sourceRow, row, colMeterVoltage);
                    CopyCellValue(sourceRow, row, colMeterCurrent);
                    ConfigureCurrentSpecificationCell(
                        row,
                        GetCellText(sourceRow, colMeterAccessMode, GetDefaultAssetOption("AccessMode")),
                        GetCellText(sourceRow, colMeterCurrentSpecification, GetDefaultAssetOption("CurrentSpecification", "Direct")));
                    CopyCellValue(sourceRow, row, colMeterActiveClass);
                    CopyCellValue(sourceRow, row, colMeterActiveConstant);
                    CopyCellValue(sourceRow, row, colMeterReactiveClass);
                    CopyCellValue(sourceRow, row, colMeterReactiveConstant);
                    CopyCellValue(sourceRow, row, colMeterBaudRate);
                }
            }
            finally
            {
                isLoadingStationConfig = false;
                isLoadingMeterArchive = false;
            }

            await SaveAllAssetInfoAsync(showMessage: false);
            AddProcessLog("系统", "资产批量修改", true, "已按1工位参数批量覆盖2-48工位资产信息。", 0);
            MessageBox.Show("已按1工位参数批量修改2-48工位。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            SetComboCellValue(row, colMeterType, archive.MeterType, GetDefaultAssetOption("MeterType"));
            SetComboCellValue(row, colMeterAccessMode, archive.AccessMode, GetDefaultAssetOption("AccessMode"));
            row.Cells[colMeterVoltage.Index].Value = DefaultIfEmpty(archive.Voltage, GetDefaultAssetOption("Voltage"));
            row.Cells[colMeterCurrent.Index].Value = DefaultIfEmpty(archive.Current, GetDefaultAssetOption("BasicCurrent"));
            ConfigureCurrentSpecificationCell(
                row,
                archive.AccessMode,
                archive.CurrentSpecification);
            SetComboCellValue(row, colMeterActiveClass, archive.ActiveClass, GetDefaultAssetOption("ActiveClass"));
            row.Cells[colMeterActiveConstant.Index].Value = DefaultIfEmpty(archive.ActiveConstant, GetDefaultAssetOption("ActiveConstant"));
            SetComboCellValue(row, colMeterReactiveClass, archive.ReactiveClass, GetDefaultAssetOption("ReactiveClass"));
            row.Cells[colMeterReactiveConstant.Index].Value = DefaultIfEmpty(archive.ReactiveConstant, GetDefaultAssetOption("ReactiveConstant"));
            row.Cells[colStationBarcode.Index].Value = archive.Barcode;
            row.Cells[colStationMeterAddress.Index].Value =
                DefaultIfEmpty(
                    archive.MeterAddress,
                    TryExtractMeterAddressFromBarcode(archive.Barcode, out string extractedAddress) ? extractedAddress : string.Empty);
            SetComboCellValue(row, colMeterBaudRate, archive.BaudRate, GetDefaultAssetOption("BaudRate"));
        }

        /// <summary>
        /// 把表格行里的电表档案保存回数据库。
        /// </summary>
        private void SaveMeterArchiveFromRow(DataGridViewRow row)
        {
            if (row.IsNewRow)
                return;

            accessDatabaseService.SaveMeterArchive(CreateMeterArchiveSnapshot(row));
        }

        /// <summary>
        /// 从当前表格行构建一份可直接落库的电表档案快照。
        /// 扫码枪录入时会先在 UI 中更新行内容，再把快照交给后台保存，避免阻塞界面线程。
        /// </summary>
        private MeterArchiveData CreateMeterArchiveSnapshot(DataGridViewRow row)
        {
            int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
            return new MeterArchiveData(
                stationNo,
                GetCellText(row, colMeterType, GetDefaultAssetOption("MeterType")),
                GetCellText(row, colMeterAccessMode, GetDefaultAssetOption("AccessMode")),
                GetCellText(row, colMeterVoltage, GetDefaultAssetOption("Voltage")),
                GetCellText(row, colMeterCurrent, GetDefaultAssetOption("BasicCurrent")),
                GetCellText(row, colMeterCurrentSpecification, GetDefaultAssetOption("CurrentSpecification", "Direct")),
                GetCellText(row, colMeterActiveClass, GetDefaultAssetOption("ActiveClass")),
                GetCellText(row, colMeterActiveConstant, GetDefaultAssetOption("ActiveConstant")),
                GetCellText(row, colMeterReactiveClass, GetDefaultAssetOption("ReactiveClass")),
                GetCellText(row, colMeterReactiveConstant, GetDefaultAssetOption("ReactiveConstant")),
                GetCellText(row, colStationBarcode, string.Empty),
                GetCellText(row, colStationMeterAddress, string.Empty),
                GetCellText(row, colMeterBaudRate, GetDefaultAssetOption("BaudRate")));
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
                   columnIndex == colMeterCurrentSpecification.Index ||
                   columnIndex == colMeterActiveClass.Index ||
                   columnIndex == colMeterActiveConstant.Index ||
                   columnIndex == colMeterReactiveClass.Index ||
                   columnIndex == colMeterReactiveConstant.Index ||
                   columnIndex == colMeterBaudRate.Index ||
                   columnIndex == colStationBarcode.Index ||
                   columnIndex == colStationMeterAddress.Index;
        }

        /// <summary>
        /// 创建默认电表档案。
        /// </summary>
        private MeterArchiveData CreateDefaultMeterArchive(int stationNo)
        {
            return new MeterArchiveData(
                stationNo,
                GetDefaultAssetOption("MeterType"),
                GetDefaultAssetOption("AccessMode"),
                GetDefaultAssetOption("Voltage"),
                GetDefaultAssetOption("BasicCurrent"),
                GetDefaultAssetOption("CurrentSpecification", "Direct"),
                GetDefaultAssetOption("ActiveClass"),
                GetDefaultAssetOption("ActiveConstant"),
                GetDefaultAssetOption("ReactiveClass"),
                GetDefaultAssetOption("ReactiveConstant"),
                string.Empty,
                string.Empty,
                GetDefaultAssetOption("BaudRate"));
        }

        /// <summary>
        /// 从数据库载入条码截取起止位并回填界面。
        /// </summary>
        private void LoadAssetBarcodeSettingToInputs()
        {
            isLoadingBarcodeSetting = true;
            try
            {
                MeterTestAssetBarcodeSettingData setting = accessDatabaseService.LoadOrCreateAssetBarcodeSetting();
                assetBarcodeStartIndex = setting.BarcodeStartIndex;
                assetBarcodeEndIndex = setting.BarcodeEndIndex;
                assetBarcodeRuleType = string.Equals(
                    setting.RuleType,
                    MeterTestBarcodeExtractor.Rule2Composite,
                    StringComparison.OrdinalIgnoreCase)
                        ? MeterTestBarcodeExtractor.Rule2Composite
                        : MeterTestBarcodeExtractor.Rule1Range;
                if (assetBarcodeRuleType == MeterTestBarcodeExtractor.Rule1Range &&
                    assetBarcodeStartIndex == 8 &&
                    assetBarcodeEndIndex == 20)
                {
                    assetBarcodeStartIndex = 9;
                    assetBarcodeEndIndex = 20;
                    accessDatabaseService.SaveAssetBarcodeSetting(
                        assetBarcodeStartIndex,
                        assetBarcodeEndIndex,
                        assetBarcodeRuleType,
                        setting.Rule2FirstStart,
                        setting.Rule2FirstLength,
                        setting.Rule2SecondStart,
                        setting.Rule2SecondLength);
                }

                assetBarcodeRule2FirstStart = setting.Rule2FirstStart;
                assetBarcodeRule2FirstLength = setting.Rule2FirstLength;
                assetBarcodeRule2SecondStart = setting.Rule2SecondStart;
                assetBarcodeRule2SecondLength = setting.Rule2SecondLength;
                cbxBarcodeRule.SelectedIndex = assetBarcodeRuleType == MeterTestBarcodeExtractor.Rule2Composite ? 1 : 0;
                PopulateBarcodeRuleInputs();
                UpdateBarcodeRuleInputState();
            }
            finally
            {
                isLoadingBarcodeSetting = false;
            }

        }

        /// <summary>
        /// 根据当前输入保存条码截取起止位。
        /// </summary>
        private void SaveBarcodeSettingFromInputs()
        {
            if (isLoadingBarcodeSetting)
                return;

            string selectedRule = cbxBarcodeRule.SelectedIndex == 1
                ? MeterTestBarcodeExtractor.Rule2Composite
                : MeterTestBarcodeExtractor.Rule1Range;
            int startIndex = assetBarcodeStartIndex;
            int endIndex = assetBarcodeEndIndex;
            int firstStart = assetBarcodeRule2FirstStart;
            int firstLength = assetBarcodeRule2FirstLength;
            int secondStart = assetBarcodeRule2SecondStart;
            int secondLength = assetBarcodeRule2SecondLength;
            bool inputsValid = selectedRule == MeterTestBarcodeExtractor.Rule1Range
                ? TryReadBarcodeRangeFromInputs(out startIndex, out endIndex)
                : TryReadBarcodeCompositeFromInputs(out firstStart, out firstLength, out secondStart, out secondLength);
            if (!inputsValid)
            {
                return;
            }

            assetBarcodeStartIndex = startIndex;
            assetBarcodeEndIndex = endIndex;
            assetBarcodeRule2FirstStart = firstStart;
            assetBarcodeRule2FirstLength = firstLength;
            assetBarcodeRule2SecondStart = secondStart;
            assetBarcodeRule2SecondLength = secondLength;
            assetBarcodeRuleType = selectedRule;
            UpdateBarcodeRuleInputState();
            accessDatabaseService.SaveAssetBarcodeSetting(
                startIndex,
                endIndex,
                selectedRule,
                firstStart,
                firstLength,
                secondStart,
                secondLength);

            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                if (!string.IsNullOrWhiteSpace(Convert.ToString(row.Cells[colStationBarcode.Index].Value)))
                {
                    ApplyBarcodeExtractionToRow(row);
                    SaveMeterArchiveFromRow(row);
                }
            }

            RefreshSchemeTreeStatusIcons();
        }

        /// <summary>
        /// 读取规则1的起始位和截取长度，并换算为内部使用的起止位。
        /// </summary>
        private bool TryReadBarcodeRangeFromInputs(out int startIndex, out int endIndex)
        {
            startIndex = assetBarcodeStartIndex;
            endIndex = assetBarcodeEndIndex;

            if (!int.TryParse(tbxBarcodeStartIndex.Text.Trim(), out startIndex))
                return false;

            if (!int.TryParse(tbxBarcodeEndIndex.Text.Trim(), out int length))
                return false;

            if (startIndex < 0 || length <= 0)
                return false;

            endIndex = startIndex + length - 1;
            return true;
        }

        /// <summary>读取规则2的两段起始位置和长度，并验证所有索引均可用于条码截取。</summary>
        private bool TryReadBarcodeCompositeFromInputs(
            out int firstStart,
            out int firstLength,
            out int secondStart,
            out int secondLength)
        {
            firstStart = assetBarcodeRule2FirstStart;
            firstLength = assetBarcodeRule2FirstLength;
            secondStart = assetBarcodeRule2SecondStart;
            secondLength = assetBarcodeRule2SecondLength;
            bool parsed = int.TryParse(tbxBarcodeStartIndex.Text.Trim(), out firstStart);
            parsed &= int.TryParse(tbxBarcodeEndIndex.Text.Trim(), out firstLength);
            parsed &= int.TryParse(tbxBarcodeSecondStart.Text.Trim(), out secondStart);
            parsed &= int.TryParse(tbxBarcodeSecondLength.Text.Trim(), out secondLength);
            return parsed && firstStart >= 0 && firstLength > 0 && secondStart >= 0 && secondLength > 0;
        }

        /// <summary>
        /// 根据条形码和当前截取区间提取电表地址。
        /// </summary>
        private bool TryExtractMeterAddressFromBarcode(string barcode, out string meterAddress)
        {
            return MeterTestBarcodeExtractor.TryExtract(
                barcode,
                assetBarcodeRuleType,
                assetBarcodeStartIndex,
                assetBarcodeEndIndex,
                assetBarcodeRule2FirstStart,
                assetBarcodeRule2FirstLength,
                assetBarcodeRule2SecondStart,
                assetBarcodeRule2SecondLength,
                out meterAddress);
        }

        /// <summary>处理条码规则切换，刷新输入框含义并立即持久化当前规则配置。</summary>
        private void BarcodeRuleSelectionChanged()
        {
            if (isLoadingBarcodeSetting)
                return;

            assetBarcodeRuleType = cbxBarcodeRule.SelectedIndex == 1
                ? MeterTestBarcodeExtractor.Rule2Composite
                : MeterTestBarcodeExtractor.Rule1Range;
            isLoadingBarcodeSetting = true;
            try
            {
                PopulateBarcodeRuleInputs();
                UpdateBarcodeRuleInputState();
            }
            finally
            {
                isLoadingBarcodeSetting = false;
            }

            SaveBarcodeSettingFromInputs();
        }

        /// <summary>将当前规则1或规则2的截取参数回填到资产信息输入框。</summary>
        private void PopulateBarcodeRuleInputs()
        {
            bool composite = assetBarcodeRuleType == MeterTestBarcodeExtractor.Rule2Composite;
            tbxBarcodeStartIndex.Text = (composite ? assetBarcodeRule2FirstStart : assetBarcodeStartIndex).ToString();
            tbxBarcodeEndIndex.Text = (composite
                ? assetBarcodeRule2FirstLength
                : Math.Max(1, assetBarcodeEndIndex - assetBarcodeStartIndex + 1)).ToString();
            tbxBarcodeSecondStart.Text = assetBarcodeRule2SecondStart.ToString();
            tbxBarcodeSecondLength.Text = assetBarcodeRule2SecondLength.ToString();
        }

        /// <summary>根据规则显示单区间或双区间输入，规则2的两个片段均由用户配置。</summary>
        private void UpdateBarcodeRuleInputState()
        {
            bool assetView = currentGridViewMode == MeterTestGridViewMode.AssetInfo;
            bool composite = assetBarcodeRuleType == MeterTestBarcodeExtractor.Rule2Composite;
            lblBarcodeStartIndex.Text = composite ? "段1起始" : "条码起始位";
            lblBarcodeEndIndex.Text = composite ? "段1长度" : "截取长度";
            lblBarcodeStartIndex.Visible = assetView;
            tbxBarcodeStartIndex.Visible = assetView;
            lblBarcodeEndIndex.Visible = assetView;
            tbxBarcodeEndIndex.Visible = assetView;
            lblBarcodeSecondStart.Visible = assetView && composite;
            tbxBarcodeSecondStart.Visible = assetView && composite;
            lblBarcodeSecondLength.Visible = assetView && composite;
            tbxBarcodeSecondLength.Visible = assetView && composite;
        }

        /// <summary>
        /// 将当前行的条形码自动回填到电表地址。
        /// </summary>
        private void ApplyBarcodeExtractionToRow(DataGridViewRow row)
        {
            if (row.IsNewRow)
                return;

            string barcode = Convert.ToString(row.Cells[colStationBarcode.Index].Value)?.Trim() ?? string.Empty;
            string meterAddress = TryExtractMeterAddressFromBarcode(barcode, out string extractedAddress)
                ? extractedAddress
                : string.Empty;

            isApplyingBarcodeExtraction = true;
            try
            {
                row.Cells[colStationMeterAddress.Index].Value = meterAddress;
            }
            finally
            {
                isApplyingBarcodeExtraction = false;
            }
        }

        /// <summary>
        /// 条形码被清空或无法生成电表地址时，立即取消该工位选择并执行下电。
        /// </summary>
        private async Task DeselectStationWithoutCompleteAssetAsync(DataGridViewRow row)
        {
            if (HasCompleteAssetForTest(row) ||
                !IsStationRowSelected(row))
            {
                return;
            }

            int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
            isUpdatingStationSelection = true;
            try
            {
                row.Cells[colStationSelected.Index].Value = false;
            }
            finally
            {
                isUpdatingStationSelection = false;
            }

            await ExecuteStationPowerSelectionChangesAsync(
                new[] { new StationPowerSelectionChange(stationNo, false) });
            LogMessage.Debug($"[资产联动] 工位{stationNo}条形码或电表地址不完整，已取消测试选择并执行下电。");
        }

        /// <summary>
        /// 动态切换“电表地址”列标题。
        /// </summary>
        private void UpdateMeterAddressColumnHeader(bool assetInfoView)
        {
            colStationMeterAddress.HeaderText = assetInfoView ? "电表地址" : "表位地址";
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
        /// 根据接入方式绑定当前工位的电流规格候选项。
        /// 切换接入方式时，不再适用的旧值会回退到新规格的第一项。
        /// </summary>
        private void ConfigureCurrentSpecificationCell(
            DataGridViewRow row,
            string accessMode,
            string? preferredValue)
        {
            string scope = accessMode.Contains("互感", StringComparison.OrdinalIgnoreCase)
                ? "Transformer"
                : "Direct";
            IReadOnlyList<string> candidates = GetAssetOptionValues("CurrentSpecification", scope);
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException($"数据库缺少 {scope} 电流规格候选项。");
            }
            DataGridViewComboBoxCell cell = (DataGridViewComboBoxCell)row.Cells[colMeterCurrentSpecification.Index];
            cell.Items.Clear();
            cell.Items.AddRange(candidates.Cast<object>().ToArray());

            string normalized = preferredValue?.Trim() ?? string.Empty;
            cell.Value = candidates.Contains(normalized, StringComparer.OrdinalIgnoreCase)
                ? normalized
                : GetDefaultAssetOption("CurrentSpecification", scope);
        }

        /// <summary>
        /// 调度单个工位通信小项，并将服务返回的响应、结论和耗时更新到 UI 状态缓存。
        /// </summary>
        private async Task ExecuteStationSubItemAsync(
            StationCommunicationConfig station,
            SelectedSubItemContext context,
            CancellationToken cancellationToken)
        {
            RunOnUiThread(() => UpdateStationRunningState(station.StationNo, context));
            MeterTestCommunicationStationResult result =
                await communicationTestService.ExecuteStationStepAsync(
                    station,
                    context,
                    (stationNo, lines) => LogStationCommunicationBlock(
                        context.TestItemName,
                        station,
                        lines),
                    cancellationToken);

            RunOnUiThread(() =>
            {
                ApplyStationExecutionResult(
                    station,
                    context,
                    result.ResponseHex,
                    result.Passed);
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}/工位{station.StationNo}",
                    context.SubItem.Name,
                    result.Passed,
                    result.Message,
                    result.ElapsedMilliseconds);
            });
        }

        /// <summary>
        /// 执行方案树中的一个日计时小项。
        /// 第一次进入日计时时执行完整三轮流程，流程内在每个 Start/Wait/Read 完成时立即回填对应节点。
        /// 后续八个方案节点只读取已保存的阶段结果，不重复发送报文。
        /// </summary>
        private async Task ExecuteControlPcbDailyTimingStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestFlowStepResult result = await dailyTimingService.ExecuteStepAsync(
                meterTestPlanConfig,
                context,
                selectedStations,
                (stationNo, lines) => LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "日计时日志",
                    lines),
                (stationNo, stepContext) => RunOnUiThread(
                    () => UpdateStationRunningState(stationNo, stepContext)),
                (stationNo, stepContext, passed, message) => RunOnUiThread(
                    () => ApplyStationExecutionResult(stationNo, stepContext, passed, message)),
                measurement => RecordMeasurement(context.SchemeName, measurement),
                cancellationToken);
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
        }

        /// <summary>
        /// 执行潜动试验启动节点。各控制PCB分组并发，组内按配置间隔逐表位下发0x25+01。
        /// 只有收到数据项严格为01的启动应答，才会记录为后续等待/读取的有效工位。
        /// </summary>
        private async Task ExecuteControlPcbCreepingStartAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestCreepingStepResult result = await creepingTestService.StartAsync(
                context,
                selectedStations,
                GetEnabledControlPcbGroups(context.SubItem),
                Math.Max(0, context.SubItem.PacketIntervalMs),
                (stationNo, lines) => LogCreepingStationBlock(context.TestItemName, stationNo, lines),
                (targets, passed, message) => RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, passed, message, string.Empty)),
                stationNo => RunOnUiThread(() => UpdateStationRunningState(stationNo, context)),
                (stationNo, passed, message) => RunOnUiThread(() => ApplyStationExecutionResult(stationNo, context, passed, message)),
                cancellationToken);

            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Passed,
                result.Message,
                result.ElapsedMilliseconds);
        }

        /// <summary>
        /// 调度潜动试验步骤 3。资产参数读取、时间计算、统一倒计时和逐工位结论均由潜动服务负责。
        /// </summary>
        private async Task ExecuteCreepingWaitAsync(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestCreepingStepResult result = await creepingTestService.WaitAsync(
                context,
                selectedStations,
                (stationNo, lines) => LogCreepingStationBlock(context.TestItemName, stationNo, lines),
                stationNo => RunOnUiThread(() => UpdateStationRunningState(stationNo, context)),
                (stationNo, passed, message) =>
                    RunOnUiThread(() => ApplyStationExecutionResult(stationNo, context, passed, message)),
                cancellationToken);

            RunOnUiThread(() =>
            {
                RestoreStationDisplayForSelectedNode();
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    result.Passed,
                    result.Message,
                    result.ElapsedMilliseconds);
            });
        }

        /// <summary>
        /// 执行潜动脉冲读取节点。仅向已收到0x25启动应答的工位发送0x25+AA，
        /// 单个工位无应答或解析失败不会阻止同组及其他控制PCB组继续读取。
        /// </summary>
        private async Task ExecuteControlPcbCreepingReadAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestCreepingStepResult result = await creepingTestService.ReadAsync(
                context,
                selectedStations,
                GetEnabledControlPcbGroups(context.SubItem),
                Math.Max(0, context.SubItem.PacketIntervalMs),
                (stationNo, lines) => LogCreepingStationBlock(context.TestItemName, stationNo, lines),
                (targets, passed, message) => RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, passed, message, string.Empty)),
                stationNo => RunOnUiThread(() => UpdateStationRunningState(stationNo, context)),
                (stationNo, passed, message) => RunOnUiThread(() => ApplyStationExecutionResult(stationNo, context, passed, message)),
                cancellationToken);

            RestoreStationDisplayForSelectedNode();
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Passed,
                result.Message,
                result.ElapsedMilliseconds);
        }

        /// <summary>按累计脉冲数小于等于1判定潜动结果；0个或1个均为合格。</summary>
        private void ExecuteCreepingPulseJudgeStep(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations)
        {
            long startTicks = Environment.TickCount64;
            bool allPassed = creepingTestService.JudgeResults(
                context,
                selectedStations,
                (stationNo, lines) => LogCreepingStationBlock(
                    context.TestItemName,
                    stationNo,
                    lines.Concat(new[] { StationLogSeparator }).ToArray()),
                measurement => RecordMeasurement(context.SchemeName, measurement),
                (stationNo, passed, message) => ApplyStationExecutionResult(stationNo, context, passed, message));

            RestoreStationDisplayForSelectedNode();
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                allPassed,
                allPassed
                    ? "所有工位当前累计脉冲数均小于等于1个。"
                    : "存在累计脉冲数大于1个或未读取到脉冲结果的工位。",
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        /// <summary>
        /// 调度起动试验步骤 2。控制 PCB 报文构造、发送、应答解析和逐工位容错均由起动服务负责。
        /// </summary>
        private Task ExecuteControlPcbStartingErrorStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            return startingErrorService.StartControlPcbAsync(
                context,
                selectedStations,
                GetEnabledControlPcbGroups(context.SubItem),
                (stationNo, lines) => LogStartingErrorStationBlock(context.TestItemName, stationNo, lines),
                (group, target, lines) => LogControlPcbStationBlock(context.TestItemName, group, target, lines),
                (stationNo, runningContext) => RunOnUiThread(() => UpdateStationRunningState(stationNo, runningContext)),
                (stationNo, passed, message) =>
                    RunOnUiThread(() => ApplyStationExecutionResult(stationNo, context, passed, message)),
                (scope, name, passed, message, elapsed) =>
                    RunOnUiThread(() => AddProcessLog(scope, name, passed, message, elapsed)),
                () => RunOnUiThread(() => RestoreStationDisplayForSelectedNode()),
                cancellationToken);
        }

        /// <summary>
        /// 调度起动试验步骤 3。Tst 参数计算、统一倒计时和完整过程日志均由起动服务负责。
        /// </summary>
        private Task ExecuteStartingTimeWaitAsync(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            return startingErrorService.ExecuteWaitAsync(
                context,
                selectedStations,
                (stationNo, lines) => LogStartingErrorStationBlock(context.TestItemName, stationNo, lines),
                (stationNo, runningContext) =>
                    RunOnUiThread(() => UpdateStationRunningState(stationNo, runningContext)),
                (stationNo, passed, message) =>
                    RunOnUiThread(() => ApplyStationExecutionResult(stationNo, context, passed, message)),
                (scope, name, passed, message, elapsed) =>
                    RunOnUiThread(() => AddProcessLog(scope, name, passed, message, elapsed)),
                () => RunOnUiThread(() => RestoreStationDisplayForSelectedNode()),
                cancellationToken);
        }

        /// <summary>
        /// 调度起动试验步骤 4。0x38+AA 查询、float 解析及工位结果缓存均由起动服务负责。
        /// </summary>
        private Task ExecuteControlPcbStartingErrorReadStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            return startingErrorService.ReadControlPcbAsync(
                context,
                selectedStations,
                GetEnabledControlPcbGroups(context.SubItem),
                (stationNo, lines) => LogStartingErrorStationBlock(context.TestItemName, stationNo, lines),
                (group, target, lines) => LogControlPcbStationBlock(context.TestItemName, group, target, lines),
                (stationNo, runningContext) => RunOnUiThread(() => UpdateStationRunningState(stationNo, runningContext)),
                (stationNo, passed, message) =>
                    RunOnUiThread(() => ApplyStationExecutionResult(stationNo, context, passed, message)),
                (scope, name, passed, message, elapsed) =>
                    RunOnUiThread(() => AddProcessLog(scope, name, passed, message, elapsed)),
                () => RunOnUiThread(() => RestoreStationDisplayForSelectedNode()),
                cancellationToken);
        }

        /// <summary>
        /// 调度起动试验步骤 5。规程限值计算、60% 区间判定和测量值保存均由起动服务负责。
        /// </summary>
        private void ExecuteStartingErrorJudgeStep(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations)
        {
            startingErrorService.JudgeResults(
                context,
                selectedStations,
                (stationNo, lines) => LogStartingErrorStationBlock(context.TestItemName, stationNo, lines),
                (stationNo, passed, message) =>
                    RunOnUiThread(() => ApplyStationExecutionResult(stationNo, context, passed, message)),
                (scope, name, passed, message, elapsed) =>
                    RunOnUiThread(() => AddProcessLog(scope, name, passed, message, elapsed)),
                () => RunOnUiThread(() => RestoreStationDisplayForSelectedNode()),
                measurement => RecordMeasurement(context.SchemeName, measurement));
        }

        /// <summary>写入起动误差工位日志文件和右侧过程日志区域。</summary>
        private void LogStartingErrorStationBlock(string testItemName, int stationNo, params string[] lines)
        {
            string message = AddTimestampToFiveStepFlowMessage(string.Join(Environment.NewLine, lines));
            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}",
                "起动试验日志",
                message);
        }

        /// <summary>写入潜动试验工位日志文件和右侧过程日志区域。</summary>
        private void LogCreepingStationBlock(string testItemName, int stationNo, params string[] lines)
        {
            string message = AddTimestampToFiveStepFlowMessage(string.Join(Environment.NewLine, lines));
            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}",
                "潜动试验日志",
                message);
        }

        /// <summary>写入基本误差工位日志文件和右侧过程日志区域。</summary>
        private void LogBasicErrorStationBlock(
            string testItemName,
            string testSubItemName,
            int stationNo,
            params string[] lines)
        {
            string message = string.Join(Environment.NewLine, lines);
            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}/{testSubItemName}",
                "基本误差日志",
                message);
        }

        /// <summary>
        /// 写入国网智芯蓝牙专用TCP的工位级连接、收发和解析日志。
        /// 文件名始终使用父级TestItem，TestSubItem只进入日志内容和界面作用域。
        /// </summary>
        private void LogBluetoothStationBlock(
            string testItemName,
            string testSubItemName,
            int stationNo,
            params string[] lines)
        {
            string message = string.Join(Environment.NewLine, lines);
            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}/{testSubItemName}",
                "蓝牙接口日志",
                message);
        }

        /// <summary>
        /// 把通用TestSubItem及其内部步骤写入父级TestItem对应的工位日志文件。
        /// TestSubItem名称只用于日志内容和右侧界面作用域，不参与文件名生成。
        /// </summary>
        private void LogTestItemStationBlock(
            string testItemName,
            string testSubItemName,
            int stationNo,
            string logType,
            params string[] lines)
        {
            string message = string.Join(Environment.NewLine, lines);
            if (UsesTimestampedFiveStepFlow(testItemName))
            {
                message = AddTimestampToFiveStepFlowMessage(message);
            }

            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}/{testSubItemName}",
                logType,
                message);
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
        /// 在UI线程执行需要返回布尔值的交互操作。
        /// 设备自检服务通过该桥接显示安全确认框，服务本身不依赖WinForms。
        /// </summary>
        private bool RunOnUiThreadWithResult(Func<bool> action)
        {
            if (IsDisposed || Disposing)
                return false;

            return InvokeRequired
                ? (bool)Invoke(action)
                : action();
        }

        /// <summary>在UI线程执行需要返回复杂结果的交互操作。</summary>
        private T? RunOnUiThreadWithValue<T>(Func<T?> action)
        {
            if (IsDisposed || Disposing)
                return default;

            return InvokeRequired
                ? (T?)Invoke(action)
                : action();
        }

        /// <summary>
        /// 执行方案树中的一个设备自检小项。
        /// 短路检测先自动降源并复核无压，再由用户通过红色安全弹窗手动确认。
        /// </summary>
        private async Task ExecuteDeviceSelfCheckStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestFlowStepResult result = await deviceSelfCheckService.ExecuteAsync(
                meterTestPlanConfig,
                context,
                selectedStations,
                () => RunOnUiThreadWithResult(ShowShortCircuitSafetyConfirmation),
                (stationNo, lines) => LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "设备自检日志",
                    lines),
                (stationNo, stepContext) => RunOnUiThread(
                    () => UpdateStationRunningState(stationNo, stepContext)),
                (stationNo, stepContext, passed, message) => RunOnUiThread(
                    () => ApplyStationExecutionResult(stationNo, stepContext, passed, message)),
                measurement => RecordMeasurement(context.SchemeName, measurement),
                cancellationToken);
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
            await SetSelfCheckIndicatorsAsync(context, selectedStations, cancellationToken);
            RestoreStationDisplayForSelectedNode();
        }

        /// <summary>显示红色短路检测安全确认框，默认焦点保持在取消按钮。</summary>
        private bool ShowShortCircuitSafetyConfirmation()
        {
            using Form dialog = new()
            {
                Text = "短路检测安全确认",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(560, 230),
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };
            Label warningLabel = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 12),
                ForeColor = Color.Red,
                Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "危险操作：即将执行检测单元短路检测。\r\n\r\n"
                    + "确保线路无电压，确保单、三相电压已降。\r\n"
                    + "升源后严禁执行此流程。确认无压后点击“确认执行”，否则点击“取消”。"
            };
            FlowLayoutPanel buttons = new()
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };
            Button cancelButton = new() { Text = "取消", Width = 110, Height = 36, DialogResult = DialogResult.Cancel };
            Button confirmButton = new()
            {
                Text = "确认执行",
                Width = 120,
                Height = 36,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(190, 30, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(confirmButton);
            dialog.Controls.Add(warningLabel);
            dialog.Controls.Add(buttons);
            dialog.AcceptButton = cancelButton;
            dialog.CancelButton = cancelButton;
            return dialog.ShowDialog(this) == DialogResult.OK;
        }

        /// <summary>
        /// 执行LED效果灯测试小项。
        /// 具体0x2F报文、效果顺序、可配置等待时间和测试后灯光恢复由指示灯服务统一负责。
        /// </summary>
        private async Task ExecuteLedEffectTestStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestSubItem marqueeSubItem = context.SubItem;
            MeterTestSubItem blinkSubItem = context.SubItem;
            MeterTestItem? ledTestItem = meterTestPlanConfig.Schemes
                .FirstOrDefault(scheme => scheme.Name.Equals(context.SchemeName, StringComparison.OrdinalIgnoreCase))
                ?.TestItems.FirstOrDefault(item => item.Name.Equals(context.TestItemName, StringComparison.OrdinalIgnoreCase));
            if (ledTestItem is not null)
            {
                marqueeSubItem = ledTestItem.TestSubItems.FirstOrDefault(subItem =>
                    subItem.Enabled &&
                    subItem.ExecutionMode.Equals(MeterTestExecutionMode.LedEffectTest.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    subItem.LedEffectStep.Equals("Marquee", StringComparison.OrdinalIgnoreCase)) ?? marqueeSubItem;
                blinkSubItem = ledTestItem.TestSubItems.FirstOrDefault(subItem =>
                    subItem.Enabled &&
                    subItem.ExecutionMode.Equals(MeterTestExecutionMode.LedEffectTest.ToString(), StringComparison.OrdinalIgnoreCase) &&
                    subItem.LedEffectStep.Equals("Blink", StringComparison.OrdinalIgnoreCase)) ?? blinkSubItem;
            }

            List<SelectedSubItemContext> ledSubContexts = (ledTestItem?.TestSubItems ?? new List<MeterTestSubItem> { context.SubItem })
                .Where(subItem =>
                    subItem.Enabled &&
                    subItem.ExecutionMode.Equals(MeterTestExecutionMode.LedEffectTest.ToString(), StringComparison.OrdinalIgnoreCase))
                .Select(subItem => new SelectedSubItemContext(context.SchemeName, context.TestItemName, subItem))
                .ToList();
            if (ledSubContexts.Count == 0)
                ledSubContexts.Add(context);

            MeterTestFlowStepResult result = await indicatorLightService.ExecuteLedEffectSuiteAsync(
                meterTestPlanConfig,
                controlPcbConnectionManager,
                marqueeSubItem,
                blinkSubItem,
                selectedStations,
                (stationNo, lines) => LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "LED效果灯测试日志",
                    lines.ToArray()),
                message => RunOnUiThreadWithResult(() => ShowLedEffectStartConfirmation(message)),
                (message, stationNos) => RunOnUiThreadWithValue(
                    () => ShowLedEffectPanelResultConfirmation(message, stationNos)),
                (stationNos, passedStations, sendSucceeded, message) => RunOnUiThreadWithResult(() =>
                {
                    foreach (int stationNo in stationNos)
                    {
                        bool passed = sendSucceeded && passedStations.Contains(stationNo);
                        foreach (SelectedSubItemContext ledContext in ledSubContexts)
                        {
                            ApplyStationExecutionResult(stationNo, ledContext, passed, message);
                        }
                    }
                    return true;
                }),
                cancellationToken);

            foreach (StationCommunicationConfig station in selectedStations)
            {
                RunOnUiThread(() =>
                {
                    foreach (SelectedSubItemContext ledContext in ledSubContexts)
                    {
                        if (!stationResultCache.ContainsKey(new StationResultKey(
                                ledContext.SchemeName,
                                ledContext.TestItemName,
                                ledContext.SubItem.Name,
                                station.StationNo)))
                        {
                            ApplyStationExecutionResult(station.StationNo, ledContext, result.Success, result.Message);
                        }
                    }
                });
            }

            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
            RestoreStationDisplayForSelectedNode();
        }

        /// <summary>LED效果灯测试开始前的人工确认，避免现场错过目视观察起点。</summary>
        private bool ShowLedEffectStartConfirmation(string message)
        {
            return MessageBox.Show(
                this,
                $"{message}\r\n\r\n点击“确定”后将从第一个灯光控制面板开始测试。",
                "LED效果灯测试确认",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) == DialogResult.OK;
        }

        /// <summary>
        /// 每个灯光控制面板测试结束后的人工判定确认。
        /// 面板下的工位默认全部勾选，取消勾选的工位单独判为不合格。
        /// </summary>
        private IReadOnlyList<int>? ShowLedEffectPanelResultConfirmation(
            string message,
            IReadOnlyList<int> stationNumbers)
        {
            using Form dialog = new()
            {
                Text = "LED效果灯测试结果确认",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(560, 300),
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };
            Label messageLabel = new()
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(18, 14, 18, 8),
                Text = $"{message}\r\n\r\n请勾选目视确认合格的工位："
            };
            FlowLayoutPanel stationPanel = new()
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 4, 18, 4),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                AutoScroll = true
            };
            List<CheckBox> stationChecks = new();
            foreach (int stationNo in stationNumbers.OrderBy(item => item))
            {
                CheckBox checkBox = new()
                {
                    Text = $"工位{stationNo}",
                    Checked = true,
                    AutoSize = true,
                    Margin = new Padding(4, 4, 18, 4),
                    Font = new Font(Font.FontFamily, 10F, FontStyle.Bold)
                };
                stationChecks.Add(checkBox);
                stationPanel.Controls.Add(checkBox);
            }

            FlowLayoutPanel buttons = new()
            {
                Dock = DockStyle.Bottom,
                Height = 58,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };
            Button cancelButton = new()
            {
                Text = "取消测试",
                Width = 110,
                Height = 36,
                DialogResult = DialogResult.Cancel
            };
            Button confirmButton = new()
            {
                Text = "确认并继续",
                Width = 130,
                Height = 36,
                DialogResult = DialogResult.OK,
                BackColor = Color.FromArgb(36, 137, 95),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            buttons.Controls.Add(cancelButton);
            buttons.Controls.Add(confirmButton);
            dialog.Controls.Add(stationPanel);
            dialog.Controls.Add(buttons);
            dialog.Controls.Add(messageLabel);
            dialog.AcceptButton = confirmButton;
            dialog.CancelButton = cancelButton;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return null;

            return stationChecks
                .Where(checkBox => checkBox.Checked)
                .Select(checkBox => int.Parse(
                    checkBox.Text["工位".Length..],
                    CultureInfo.InvariantCulture))
                .ToArray();
        }

        /// <summary>
        /// 执行常数试验中的电能量读取步骤，使用MeterTestStationConfig中的工位485 TCP通道。
        /// 同一个IP:Port在本窗体生命周期内复用连接，避免每个步骤重复连接。
        /// </summary>
        private async Task ExecuteConstantEnergyReadAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            bool isStartRead = context.SubItem.ExecutionMode.Equals(
                MeterTestExecutionMode.ConstantEnergyReadStart.ToString(),
                StringComparison.OrdinalIgnoreCase);
            MeterTestFlowStepResult result = await constantTestService.ExecuteEnergyReadAsync(
                context,
                selectedStations,
                isStartRead,
                (stationNo, lines) => LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "常数试验日志",
                    lines),
                (stationNo, stepContext) => RunOnUiThread(
                    () => UpdateStationRunningState(stationNo, stepContext)),
                (stationNo, stepContext, passed, message) => RunOnUiThread(
                    () => ApplyStationExecutionResult(stationNo, stepContext, passed, message)),
                measurement => RecordMeasurement(context.SchemeName, measurement),
                cancellationToken);
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
        }

        /// <summary>执行0x37+00走字试验启动步骤。</summary>
        private async Task ExecuteControlPcbWalkingStartAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestFlowStepResult result = await ExecuteConstantWalkingOperationAsync(
                context,
                selectedStations,
                MeterTestWalkingOperation.Start,
                cancellationToken);
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
        }

        /// <summary>执行常数试验固定等待步骤。</summary>
        private async Task ExecuteConstantWaitAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestFlowStepResult result = await constantTestService.ExecuteWaitAsync(
                context,
                selectedStations,
                (stationNo, lines) => LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "常数试验日志",
                    lines),
                (stationNo, stepContext, passed, message) => RunOnUiThread(
                    () => ApplyStationExecutionResult(stationNo, stepContext, passed, message)),
                cancellationToken);
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
        }

        /// <summary>执行0x37+AA走字试验结果读取步骤。</summary>
        private async Task ExecuteControlPcbWalkingStopAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestFlowStepResult result = await ExecuteConstantWalkingOperationAsync(
                context,
                selectedStations,
                MeterTestWalkingOperation.Stop,
                cancellationToken);
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
        }

        /// <summary>执行0x37+AA走字试验结果读取步骤。</summary>
        private async Task ExecuteControlPcbWalkingReadAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            MeterTestFlowStepResult result = await ExecuteConstantWalkingOperationAsync(
                context,
                selectedStations,
                MeterTestWalkingOperation.ReadResult,
                cancellationToken);
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
        }

        /// <summary>
        /// 将常数试验0x37步骤转交给流程服务，并提供日志、状态及测量值的UI回调。
        /// 窗体不参与报文构造、发送、等待或解析。
        /// </summary>
        private Task<MeterTestFlowStepResult> ExecuteConstantWalkingOperationAsync(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            MeterTestWalkingOperation operation,
            CancellationToken cancellationToken)
        {
            return constantTestService.ExecuteWalkingOperationAsync(
                meterTestPlanConfig,
                context,
                selectedStations,
                operation,
                (stationNo, lines) => LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "常数试验日志",
                    lines),
                (stationNo, stepContext) => RunOnUiThread(
                    () => UpdateStationRunningState(stationNo, stepContext)),
                (stationNo, stepContext, passed, message) => RunOnUiThread(
                    () => ApplyStationExecutionResult(stationNo, stepContext, passed, message)),
                measurement => RecordMeasurement(context.SchemeName, measurement),
                cancellationToken);
        }

        /// <summary>常数试验最后一步：按 ek = 1000 * (n / k - e) / e 计算实际误差并与允许区间比对。</summary>
        private void ExecuteConstantResultJudgeStep(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations)
        {
            MeterTestFlowStepResult result = constantTestService.JudgeResults(
                context,
                selectedStations,
                (stationNo, lines) => LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "常数试验日志",
                    lines.Concat(new[] { StationLogSeparator }).ToArray()),
                measurement => RecordMeasurement(context.SchemeName, measurement),
                (stationNo, stepContext, passed, message) => RunOnUiThread(
                    () => ApplyStationExecutionResult(stationNo, stepContext, passed, message)));
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                result.Success,
                result.Message,
                result.ElapsedMilliseconds);
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
        /// 以工位为单位写入站点通信块日志。
        /// </summary>
        private void LogStationCommunicationBlock(string testItemName, StationCommunicationConfig station, params string[] lines)
        {
            string message = string.Join(Environment.NewLine, lines);
            LogMessage.MeterTestStationRawLog(testItemName, station.StationNo, message);
            AppendTestLog(
                station.StationNo,
                $"{testItemName}/工位{station.StationNo}",
                "通信日志",
                message);
        }

        /// <summary>
        /// 以工位为单位写入控制 PCB 日志。
        /// </summary>
        private void LogControlPcbStationBlock(string testItemName, MeterTestControlPcbGroup group, ControlPcbStationTarget target, params string[] lines)
        {
            string message = string.Join(Environment.NewLine, lines);
            if (UsesTimestampedFiveStepFlow(testItemName))
            {
                message = AddTimestampToFiveStepFlowMessage(message);
            }

            LogMessage.MeterTestStationRawLog(testItemName, target.StationNo, message);
            AppendTestLog(
                target.StationNo,
                $"{testItemName}/工位{target.StationNo}/{group.Name}",
                "控制 PCB 日志",
                message);
        }

        /// <summary>
        /// 给控制 PCB 组内所有工位同时写入同一段日志。
        /// </summary>
        private void LogControlPcbGroupBlock(string testItemName, MeterTestControlPcbGroup group, IEnumerable<ControlPcbStationTarget> targets, params string[] lines)
        {
            foreach (ControlPcbStationTarget target in targets)
            {
                LogControlPcbStationBlock(testItemName, group, target, lines);
            }
        }

        /// <summary>
        /// 将通信原始日志追加到右侧日志面板，不新增底部结果表行。
        /// 底部结果表记录结论，右侧面板记录完整过程，两个区域职责分开。
        /// </summary>
        private void AppendTestLog(int stationNo, string scope, string logType, string message)
        {
            if (IsDisposed || Disposing)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action(() => AppendTestLog(stationNo, scope, logType, message)));
                }
                catch (ObjectDisposedException)
                {
                    // 窗体关闭过程中不再投递日志。
                }
                catch (InvalidOperationException)
                {
                    // 窗体句柄已销毁时忽略后台日志回调。
                }

                return;
            }

            StoreTestLogEntry(
                stationNo,
                $"[{DateTime.Now:HH:mm:ss}] [{logType}] {scope}"
                + Environment.NewLine
                + message
                + Environment.NewLine);
        }

        /// <summary>
        /// 将一条日志保存到公共日志或指定工位日志，并只更新当前选中工位的显示内容。
        /// </summary>
        private void StoreTestLogEntry(int? stationNo, string text)
        {
            TestProcessLogEntry entry = new(++testLogSequence, stationNo, text);
            if (stationNo.HasValue && stationNo.Value is >= 1 and <= MaxStationCount)
            {
                if (!stationTestLogEntries.TryGetValue(stationNo.Value, out List<TestProcessLogEntry>? entries))
                {
                    entries = new List<TestProcessLogEntry>();
                    stationTestLogEntries[stationNo.Value] = entries;
                }

                entries.Add(entry);
                TrimLogEntries(entries, MaxStationLogEntries);
            }
            else
            {
                commonTestLogEntries.Add(entry with { StationNo = null });
                TrimLogEntries(commonTestLogEntries, MaxCommonLogEntries);
            }

            if (!stationNo.HasValue || stationNo.Value == selectedTestLogStationNo)
            {
                AppendVisibleTestLog(entry.Text);
            }
        }

        /// <summary>
        /// 点击工位表行后，将右侧日志切换到对应工位。
        /// </summary>
        private void SelectTestLogStation(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= stationGrid.Rows.Count)
                return;

            DataGridViewRow row = stationGrid.Rows[rowIndex];
            if (row.IsNewRow || !int.TryParse(Convert.ToString(row.Cells[colStationNo.Index].Value), out int stationNo))
                return;

            RefreshTestLogForStation(stationNo);
        }

        /// <summary>
        /// 按产生顺序合并公共日志和当前工位日志，刷新右侧日志框。
        /// </summary>
        private void RefreshTestLogForStation(int stationNo)
        {
            if (stationNo < 1 || stationNo > MaxStationCount ||
                rtbTestProcessLog is null || rtbTestProcessLog.IsDisposed)
            {
                return;
            }

            selectedTestLogStationNo = stationNo;
            groupTestLog.Text = $"测试日志 - 工位 {stationNo}";

            IEnumerable<TestProcessLogEntry> stationEntries = stationTestLogEntries.TryGetValue(
                stationNo,
                out List<TestProcessLogEntry>? entries)
                ? entries
                : Enumerable.Empty<TestProcessLogEntry>();
            string logText = string.Concat(
                commonTestLogEntries
                    .Concat(stationEntries)
                    .OrderBy(entry => entry.Sequence)
                    .Select(entry => entry.Text));

            rtbTestProcessLog.Text = logText;
            rtbTestProcessLog.SelectionStart = rtbTestProcessLog.TextLength;
            rtbTestProcessLog.ScrollToCaret();
        }

        /// <summary>
        /// 从日志范围或内容中提取唯一工位号；包含多个不同工位时按公共日志处理。
        /// </summary>
        private static int? TryExtractSingleStationNo(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            HashSet<int> stationNumbers = new();
            int searchIndex = 0;
            while (searchIndex < value.Length)
            {
                int stationIndex = value.IndexOf("工位", searchIndex, StringComparison.OrdinalIgnoreCase);
                if (stationIndex < 0)
                    break;

                int numberStart = stationIndex + 2;
                while (numberStart < value.Length &&
                       (char.IsWhiteSpace(value[numberStart]) || value[numberStart] is '=' or ':' or '：'))
                {
                    numberStart++;
                }

                int numberEnd = numberStart;
                while (numberEnd < value.Length && char.IsDigit(value[numberEnd]))
                {
                    numberEnd++;
                }

                if (numberEnd > numberStart &&
                    int.TryParse(value[numberStart..numberEnd], out int stationNo) &&
                    stationNo is >= 1 and <= MaxStationCount)
                {
                    stationNumbers.Add(stationNo);
                }

                searchIndex = Math.Max(numberEnd, stationIndex + 2);
            }

            return stationNumbers.Count == 1 ? stationNumbers.First() : null;
        }

        /// <summary>
        /// 追加当前可见日志并自动滚动到末尾。
        /// </summary>
        private void AppendVisibleTestLog(string text)
        {
            if (rtbTestProcessLog is null || rtbTestProcessLog.IsDisposed)
                return;

            rtbTestProcessLog.AppendText(text);
            rtbTestProcessLog.SelectionStart = rtbTestProcessLog.TextLength;
            rtbTestProcessLog.ScrollToCaret();
        }

        /// <summary>
        /// 限制界面日志缓存数量，避免长时间连续测试占用过多内存。
        /// </summary>
        private static void TrimLogEntries(List<TestProcessLogEntry> entries, int maximumCount)
        {
            int removeCount = entries.Count - maximumCount;
            if (removeCount > 0)
            {
                entries.RemoveRange(0, removeCount);
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
        /// 起动和潜动均是跨多个 TestSubItem 执行的五步流程，其工位日志每一行都需要保留实际发生时间。
        /// </summary>
        private static bool UsesTimestampedFiveStepFlow(string testItemName)
        {
            return testItemName.Equals("起动试验", StringComparison.OrdinalIgnoreCase) ||
                   testItemName.Equals("潜动试验", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 给五步流程日志每一行添加毫秒时间。已带时间的报文行、空行和分隔线保持原样。
        /// </summary>
        private static string AddTimestampToFiveStepFlowMessage(string message)
        {
            string timestamp = FormatStationLogTimestamp();
            string normalized = message.Replace("\r\n", "\n", StringComparison.Ordinal);
            return string.Join(
                Environment.NewLine,
                normalized.Split('\n').Select(line =>
                    string.IsNullOrWhiteSpace(line) ||
                    line == StationLogSeparator ||
                    HasStationLogTimestamp(line)
                        ? line
                        : $"{timestamp} - {line}"));
        }

        /// <summary>判断日志行是否已经以完整日期时间开头，防止报文收发日志重复加时间。</summary>
        private static bool HasStationLogTimestamp(string line)
        {
            return line.Length >= 26 &&
                   line[0] == '[' &&
                   char.IsDigit(line[1]) &&
                   line.IndexOf("] - ", StringComparison.Ordinal) > 0;
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
            string meterAddress = MeterTestWorkflowRouter.UsesSgcc698BroadcastAddressParser(subItem)
                ? station.MeterAddress.Trim()
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
        /// 按资产类型过滤基本误差分相测试点。
        /// 单相只保留 H 点；三相三线没有 B 相电流回路，跳过 B 相；三相四线保留 H/A/B/C。
        /// </summary>
        private List<SelectedSubItemContext> FilterAssetAwareBasicErrorContexts(
            List<SelectedSubItemContext> contexts,
            IReadOnlyList<StationCommunicationConfig> selectedStations)
        {
            if (contexts.Count == 0 || selectedStations.Count == 0)
                return contexts;

            IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
                accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
            List<MeterArchiveData> selectedArchives = selectedStations
                .Select(station => meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive) ? archive : null)
                .Where(archive => archive is not null)
                .Cast<MeterArchiveData>()
                .ToList();
            if (selectedArchives.Count == 0)
                return contexts;

            List<SelectedSubItemContext> filteredContexts = contexts;
            string skipMessage;
            if (selectedArchives.All(archive => IsSinglePhaseMeter(archive.MeterType)))
            {
                filteredContexts = contexts
                    .Where(context => !IsSplitPhaseBasicErrorContext(context))
                    .ToList();
                skipMessage = "所选工位均为单相电表，已跳过 {0} 个 A/B/C 分相基本误差测试点。";
            }
            else if (selectedArchives.All(archive => IsThreePhaseThreeWireMeter(archive.MeterType)))
            {
                filteredContexts = contexts
                    .Where(context => !IsBPhaseBasicErrorContext(context))
                    .ToList();
                skipMessage = "所选工位均为三相三线电表，已跳过 {0} 个有功 B 相基本误差测试点。";
            }
            else
            {
                return contexts;
            }

            int skippedCount = contexts.Count - filteredContexts.Count;
            if (skippedCount > 0)
            {
                AddProcessInfoLog(
                    "系统/方案过滤",
                    "资产相制",
                    "跳过",
                    string.Format(CultureInfo.CurrentCulture, skipMessage, skippedCount));
            }

            return filteredContexts;
        }

        /// <summary>
        /// 方案树根据当前已扫码资产隐藏不适用的小项，避免单相用户看到 A/B/C 分相点。
        /// </summary>
        private bool ShouldShowSubItemInSchemeTree(MeterTestSubItem subItem)
        {
            if (MeterTestWorkflowRouter.Resolve(subItem) != MeterTestWorkflowKind.BasicErrorPoint)
                return true;

            List<MeterArchiveData> activeArchives = GetActiveMeterArchivesForSchemeTree();
            if (activeArchives.Count == 0)
                return true;

            if (activeArchives.All(archive => IsSinglePhaseMeter(archive.MeterType)))
                return !IsSplitPhaseBasicErrorSubItem(subItem);

            if (activeArchives.All(archive => IsThreePhaseThreeWireMeter(archive.MeterType)))
                return !subItem.BasicErrorPhase.Equals("B", StringComparison.OrdinalIgnoreCase);

            return true;
        }

        /// <summary>读取当前具备测试资格的资产档案，供方案树做相制联动展示。</summary>
        private List<MeterArchiveData> GetActiveMeterArchivesForSchemeTree()
        {
            IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
                accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
            return meterArchives
                .Values
                .Where(archive =>
                {
                    string meterAddress = string.IsNullOrWhiteSpace(archive.MeterAddress) &&
                        TryExtractMeterAddressFromBarcode(archive.Barcode, out string extractedAddress)
                            ? extractedAddress
                            : archive.MeterAddress;
                    return !string.IsNullOrWhiteSpace(archive.Barcode) &&
                        !string.IsNullOrWhiteSpace(meterAddress);
                })
                .ToList();
        }

        /// <summary>判断资产电表类型是否为单相。</summary>
        private static bool IsSinglePhaseMeter(string meterType)
        {
            return meterType?.Contains("单相", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>判断资产电表类型是否为三相三线。</summary>
        private static bool IsThreePhaseThreeWireMeter(string meterType)
        {
            return meterType?.Contains("三相三线", StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>判断当前上下文是否为 A/B/C 分相基本误差点。</summary>
        private static bool IsSplitPhaseBasicErrorContext(SelectedSubItemContext context)
        {
            return IsSplitPhaseBasicErrorSubItem(context.SubItem);
        }

        /// <summary>判断当前小项是否为 A/B/C 分相基本误差点。</summary>
        private static bool IsSplitPhaseBasicErrorSubItem(MeterTestSubItem subItem)
        {
            return MeterTestWorkflowRouter.Resolve(subItem) == MeterTestWorkflowKind.BasicErrorPoint &&
                subItem.BasicErrorPhase is "A" or "B" or "C";
        }

        /// <summary>判断当前小项是否为 B 相基本误差点。</summary>
        private static bool IsBPhaseBasicErrorContext(SelectedSubItemContext context)
        {
            MeterTestSubItem subItem = context.SubItem;
            return MeterTestWorkflowRouter.Resolve(subItem) == MeterTestWorkflowKind.BasicErrorPoint &&
                subItem.BasicErrorPhase.Equals("B", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 获取指定方案下所有启用的测试小项。
        /// 手动数据保存使用该列表生成整个方案的完整性检查和快照。
        /// </summary>
        private List<SelectedSubItemContext> GetEnabledSchemeContexts(string schemeName)
        {
            MeterTestScheme? scheme = meterTestPlanConfig.Schemes.FirstOrDefault(candidate =>
                candidate.Name.Equals(schemeName, StringComparison.OrdinalIgnoreCase));
            if (scheme is null)
                return new List<SelectedSubItemContext>();

            return scheme.TestItems
                .SelectMany(item => item.TestSubItems
                    .Where(subItem => subItem.Enabled)
                    .Select(subItem => new SelectedSubItemContext(scheme.Name, item.Name, subItem)))
                .ToList();
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
            if (stationGrid.IsCurrentCellDirty)
            {
                stationGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }

            stationGrid.EndEdit();
            List<StationCommunicationConfig> stations = new();
            IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
                accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);

            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (row.IsNewRow || !IsStationRowSelected(row))
                    continue;

                int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                meterArchives.TryGetValue(stationNo, out MeterArchiveData? archive);
                string barcode = ResolveArchiveText(
                    archive?.Barcode,
                    () => GetCellText(row, colStationBarcode, string.Empty));
                string meterAddress = ResolveArchiveMeterAddress(
                    archive,
                    barcode,
                    () => GetCellText(row, colStationMeterAddress, string.Empty));
                if (string.IsNullOrWhiteSpace(meterAddress))
                {
                    LogMessage.Debug($"[资产联动] 工位{stationNo}未配置有效电表地址，本次测试已跳过。");
                    continue;
                }

                string ip = Convert.ToString(row.Cells[colStationIp.Index].Value)?.Trim() ?? string.Empty;
                string portText = Convert.ToString(row.Cells[colStationPort.Index].Value)?.Trim() ?? string.Empty;
                string baudRate = ResolveArchiveText(
                    archive?.BaudRate,
                    () => GetCellText(row, colMeterBaudRate, string.Empty));
                if (string.IsNullOrWhiteSpace(baudRate))
                {
                    baudRate = GetDefaultAssetOption("BaudRate");
                }

                if (string.IsNullOrWhiteSpace(ip) || !int.TryParse(portText, out int port) || port < 1 || port > 65535)
                {
                    throw new InvalidOperationException($"工位{stationNo} IP 或端口配置不正确。");
                }

                string uiMeterAddress = GetCellText(row, colStationMeterAddress, string.Empty).Trim();
                if (!NormalizeAssetMeterAddress(uiMeterAddress).Equals(
                        NormalizeAssetMeterAddress(meterAddress),
                        StringComparison.OrdinalIgnoreCase))
                {
                    LogMessage.Debug(
                        $"[资产联动] 工位{stationNo}测试下发地址使用资产库值：{meterAddress}；"
                        + $"当前方案表格显示值={uiMeterAddress}，显示值不反向作为协议地址。");
                }

                stations.Add(new StationCommunicationConfig(stationNo, ip, port, meterAddress, baudRate));
            }

            return stations;
        }

        /// <summary>
        /// 读取工位选择列的真实值，兼容 bool、CheckState 和字符串值。
        /// </summary>
        private bool IsStationRowSelected(DataGridViewRow row)
        {
            object? value = row.Cells[colStationSelected.Index].Value;
            return value switch
            {
                bool selected => selected,
                CheckState checkState => checkState == CheckState.Checked,
                string text => bool.TryParse(text, out bool selected) && selected,
                _ => false
            };
        }

        /// <summary>
        /// 读取资产字段；数据库是协议参数来源，界面只在数据库字段为空时作为兼容兜底。
        /// </summary>
        private static string ResolveArchiveText(string? archiveValue, Func<string> fallbackFactory)
        {
            string value = archiveValue?.Trim() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value) ? value : fallbackFactory().Trim();
        }

        /// <summary>
        /// 获取当前工位用于协议组帧的电表地址。
        /// 地址优先来自资产库，其次按资产库条形码和当前截取规则重新计算，最后才兼容界面列。
        /// </summary>
        private string ResolveArchiveMeterAddress(
            MeterArchiveData? archive,
            string barcode,
            Func<string> fallbackFactory)
        {
            string archiveAddress = NormalizeAssetMeterAddress(archive?.MeterAddress);
            if (!string.IsNullOrWhiteSpace(archiveAddress))
                return archiveAddress;

            if (TryExtractMeterAddressFromBarcode(barcode, out string extractedAddress))
                return NormalizeAssetMeterAddress(extractedAddress);

            return NormalizeAssetMeterAddress(fallbackFactory());
        }

        /// <summary>规范化资产电表地址为12位十六进制文本，防止空格或分隔符影响698组帧。</summary>
        private static string NormalizeAssetMeterAddress(string? meterAddress)
        {
            string normalized = new(
                (meterAddress ?? string.Empty)
                    .Where(Uri.IsHexDigit)
                    .Select(char.ToUpperInvariant)
                    .ToArray());
            return normalized.Length == 12 ? normalized : string.Empty;
        }

        /// <summary>
        /// 保存工位通信配置到 XML 和本地数据库。
        /// </summary>
        private void SaveStationCommunicationConfig()
        {
            if (isLoadingStationConfig)
                return;

            MeterTestStationConfig config = stationConfigService.LoadOrCreate(
                stationConfigFilePath,
                MaxStationCount,
                DefaultStationIp,
                DefaultStationStartPort,
                meterTestPlanConfig);
            config.Stations.Clear();
            List<MeterTestStationCommunication> stationSnapshots = new();
            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                string ip = Convert.ToString(row.Cells[colStationIp.Index].Value)?.Trim() ?? string.Empty;
                string portText = Convert.ToString(row.Cells[colStationPort.Index].Value)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(ip) || !int.TryParse(portText, out int port) || port < 1 || port > 65535)
                    continue;

                MeterTestStationCommunication station = new()
                {
                    StationNo = stationNo,
                    Ip = ip,
                    Port = port
                };
                config.Stations.Add(station);
                stationSnapshots.Add(station);
            }

            stationConfigService.Save(stationConfigFilePath, config);
            accessDatabaseService.SaveStationConfigs(stationSnapshots);
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
        /// 全选或全清工位，然后按所有启用的 ControlPcbGroup 执行一次全量电源同步。
        /// 单工位模式点击全选时只选择第一个工位。
        /// </summary>
        private async Task SetAllStationSelectionAsync(bool selected)
        {
            int firstEligibleRowIndex = stationGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow && row.Visible && HasCompleteAssetForTest(row))
                .Select(row => row.Index)
                .DefaultIfEmpty(-1)
                .First();

            isUpdatingStationSelection = true;
            try
            {
                foreach (DataGridViewRow row in stationGrid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    bool eligible = row.Visible && HasCompleteAssetForTest(row);
                    bool targetSelected = selected && eligible &&
                        (!rbSingleStation.Checked || row.Index == firstEligibleRowIndex);
                    bool currentSelected = IsStationRowSelected(row);
                    if (currentSelected == targetSelected)
                        continue;

                    row.Cells[colStationSelected.Index].Value = targetSelected;
                }
            }
            finally
            {
                isUpdatingStationSelection = false;
            }

            await SynchronizeEnabledControlPcbStationPowerAsync();
        }

        /// <summary>
        /// 切回测试方案视图时同步已扫码工位的勾选状态，但不触发控制PCB上下电。
        /// 该方法只服务 UI 快速刷新；实际电源动作仍由手动选择、全选或执行测试流程统一处理。
        /// </summary>
        private void SelectEligibleStationsForTestPlanWithoutPower()
        {
            int firstEligibleRowIndex = stationGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow && row.Visible && HasCompleteAssetForTest(row))
                .Select(row => row.Index)
                .DefaultIfEmpty(-1)
                .First();

            stationGrid.SuspendLayout();
            isUpdatingStationSelection = true;
            try
            {
                foreach (DataGridViewRow row in stationGrid.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    bool eligible = row.Visible && HasCompleteAssetForTest(row);
                    row.Cells[colStationSelected.Index].Value =
                        eligible && (!rbSingleStation.Checked || row.Index == firstEligibleRowIndex);
                }
            }
            finally
            {
                isUpdatingStationSelection = false;
                stationGrid.ResumeLayout();
            }
        }

        /// <summary>
        /// 遍历所有 enabled=true 的 ControlPcbGroup，把选中工位设为上电状态，
        /// 并对未选中工位明确下发断电流、下电压命令。
        /// </summary>
        private async Task SynchronizeEnabledControlPcbStationPowerAsync(
            IEnumerable<int>? selectedStationNumbers = null)
        {
            if (stationPowerControlCts.IsCancellationRequested)
                return;

            HashSet<int> selectedStations = selectedStationNumbers is null
                ? stationGrid.Rows
                    .Cast<DataGridViewRow>()
                    .Where(row => !row.IsNewRow &&
                                  IsStationRowSelected(row) &&
                                  HasCompleteAssetForTest(row))
                    .Select(row => Convert.ToInt32(row.Cells[colStationNo.Index].Value))
                    .ToHashSet()
                : selectedStationNumbers.ToHashSet();
            List<int> configuredStations = meterTestPlanConfig.ControlPcbGroups
                .Where(group => group.Enabled)
                .SelectMany(group => Enumerable.Range(
                    Math.Max(1, group.StationStart),
                    Math.Max(0, Math.Min(MaxStationCount, group.StationEnd) - Math.Max(1, group.StationStart) + 1)))
                .Distinct()
                .OrderBy(stationNo => stationNo)
                .ToList();
            if (configuredStations.Count == 0)
            {
                LogMessage.Debug("[工位电源][全量同步] 没有 enabled=true 的 ControlPcbGroup，跳过。");
                return;
            }

            List<StationPowerSelectionChange> changes = configuredStations
                .Select(stationNo => new StationPowerSelectionChange(
                    stationNo,
                    selectedStations.Contains(stationNo)))
                .ToList();
            string poweredStations = string.Join(",", configuredStations.Where(selectedStations.Contains));
            string poweredOffStations = string.Join(",", configuredStations.Where(stationNo => !selectedStations.Contains(stationNo)));
            LogMessage.Debug(
                $"[工位电源][全量同步] 启用PCB覆盖{configuredStations.Count}个工位；"
                + $"上电工位=[{poweredStations}]；下电工位=[{poweredOffStations}]。");
            await ExecuteStationPowerSelectionChangesAsync(changes);
        }

        /// <summary>
        /// 单工位模式下只保留当前第一个已选工位，其余工位取消选择并执行下电。
        /// 没有工位被选择时保持全不选，不再自动勾选工位1。
        /// </summary>
        private async Task ApplySingleStationSelectionRuleAsync()
        {
            if (!rbSingleStation.Checked)
                return;

            int selectedRowIndex = FindFirstSelectedStationRowIndex();
            if (selectedRowIndex < 0)
                return;

            List<StationPowerSelectionChange> changes = new();
            isUpdatingStationSelection = true;
            try
            {
                foreach (DataGridViewRow row in stationGrid.Rows)
                {
                    if (row.IsNewRow || row.Index == selectedRowIndex)
                        continue;

                    bool currentSelected = IsStationRowSelected(row);
                    if (!currentSelected)
                        continue;

                    row.Cells[colStationSelected.Index].Value = false;
                    int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                    changes.Add(new StationPowerSelectionChange(stationNo, false));
                }
            }
            finally
            {
                isUpdatingStationSelection = false;
            }

            await ExecuteStationPowerSelectionChangesAsync(changes);
        }

        /// <summary>
        /// 处理用户手动勾选或取消单个工位。
        /// 单工位模式勾选新工位时，会同时取消并下电之前已选择的工位。
        /// </summary>
        private async Task HandleStationSelectionChangedAsync(int rowIndex)
        {
            if (currentGridViewMode != MeterTestGridViewMode.TestPlan ||
                rowIndex < 0 || rowIndex >= stationGrid.Rows.Count)
            {
                return;
            }

            DataGridViewRow changedRow = stationGrid.Rows[rowIndex];
            if (changedRow.IsNewRow)
                return;

            bool isSelected = IsStationRowSelected(changedRow);
            int stationNo = Convert.ToInt32(changedRow.Cells[colStationNo.Index].Value);
            if (isSelected && !HasCompleteAssetForTest(changedRow))
            {
                isUpdatingStationSelection = true;
                try
                {
                    changedRow.Cells[colStationSelected.Index].Value = false;
                }
                finally
                {
                    isUpdatingStationSelection = false;
                }

                LogMessage.Debug($"[资产联动] 工位{stationNo}尚未扫码或电表地址为空，禁止选择参与测试。");
                return;
            }

            List<StationPowerSelectionChange> changes = new()
            {
                new StationPowerSelectionChange(stationNo, isSelected)
            };

            if (isSelected && rbSingleStation.Checked)
            {
                isUpdatingStationSelection = true;
                try
                {
                    foreach (DataGridViewRow row in stationGrid.Rows)
                    {
                        if (row.IsNewRow || row.Index == rowIndex ||
                            !IsStationRowSelected(row))
                        {
                            continue;
                        }

                        row.Cells[colStationSelected.Index].Value = false;
                        int otherStationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                        changes.Add(new StationPowerSelectionChange(otherStationNo, false));
                    }
                }
                finally
                {
                    isUpdatingStationSelection = false;
                }
            }

            await ExecuteStationPowerSelectionChangesAsync(changes);
        }

        /// <summary>
        /// 从资产信息数据库读取工位电表类型，并并行调用控制 PCB 电源服务。
        /// </summary>
        private async Task ExecuteStationPowerSelectionChangesAsync(
            IReadOnlyList<StationPowerSelectionChange> changes)
        {
            if (changes.Count == 0 || stationPowerControlCts.IsCancellationRequested)
                return;

            IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
                accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
            List<Task> tasks = changes
                .Select(change => ExecuteStationPowerSelectionChangeAsync(change, meterArchives))
                .ToList();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 执行单个工位的勾选上电或取消下电操作。
        /// </summary>
        private async Task ExecuteStationPowerSelectionChangeAsync(
            StationPowerSelectionChange change,
            IReadOnlyDictionary<int, MeterArchiveData> meterArchives)
        {
            try
            {
                string meterType = "资产类型未识别，使用ABC三相关闭兜底";
                bool isThreePhase = true;
                if (meterArchives.TryGetValue(change.StationNo, out MeterArchiveData? archive))
                {
                    meterType = archive.MeterType.Trim();
                    if (meterType.Contains("三相", StringComparison.OrdinalIgnoreCase))
                    {
                        isThreePhase = true;
                    }
                    else if (meterType.Contains("单相", StringComparison.OrdinalIgnoreCase))
                    {
                        isThreePhase = false;
                    }
                    else if (change.IsSelected)
                    {
                        LogMessage.Debug(
                            $"[工位电源] 工位{change.StationNo}资产信息中的电表类型无法识别：{meterType}，取消上电操作。");
                        return;
                    }
                }
                else if (change.IsSelected)
                {
                    LogMessage.Debug($"[工位电源] 工位{change.StationNo}未读取到资产信息，取消上电操作。");
                    return;
                }

                LogMessage.Debug(
                    $"[工位电源] 工位{change.StationNo}选择状态变更为{(change.IsSelected ? "选中" : "未选中")}，"
                    + $"数据库电表类型={meterType}，准备执行{(change.IsSelected ? "上电" : "下电")}流程。");
                MeterTestStationPowerResult result = await stationPowerService.SetStationPowerAsync(
                    meterTestPlanConfig,
                    controlPcbConnectionManager,
                    change.StationNo,
                    isThreePhase,
                    change.IsSelected,
                    stationPowerControlCts.Token);
                LogMessage.Debug(
                    $"[工位电源] 工位{change.StationNo}操作结论={(result.Success ? "成功" : "失败")}：{result.Message}");
                await indicatorLightService.SetPowerIndicatorAsync(
                    meterTestPlanConfig,
                    controlPcbConnectionManager,
                    change.StationNo,
                    change.IsSelected,
                    result.Success,
                    stationPowerControlCts.Token);
            }
            catch (OperationCanceledException)
            {
                // 窗体关闭时取消尚未完成的控制 PCB 操作，不继续输出错误。
            }
            catch (Exception ex)
            {
                LogMessage.Error($"[工位电源] 工位{change.StationNo}选择状态处理异常", ex);
            }
        }

        /// <summary>
        /// 找到第一个被勾选的工位行。
        /// </summary>
        private int FindFirstSelectedStationRowIndex()
        {
            foreach (DataGridViewRow row in stationGrid.Rows)
            {
                if (row.Visible && HasCompleteAssetForTest(row) &&
                    IsStationRowSelected(row))
                {
                    return row.Index;
                }
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
        private void RestoreStationDisplayForSelectedNode(bool loadFromAccess = true)
        {
            if (!TryGetSelectedDisplayContext(out SelectedSubItemContext context))
            {
                UpdateStationTestContent(GetSelectedTestContentText());
                ClearStationResultColumns();
                return;
            }

            if (loadFromAccess)
            {
                LoadStationResultsFromAccess(context);
            }

            stationGrid.SuspendLayout();
            try
            {
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
            finally
            {
                stationGrid.ResumeLayout();
            }
        }

        /// <summary>
        /// 延迟恢复当前节点结果。
        /// 测试方案视图先用内存缓存快速切换，数据库读取放到 UI 空闲后执行，避免点击按钮时卡住。
        /// </summary>
        private void QueueSelectedNodeResultRestore()
        {
            if (stationDisplayRestorePending || IsDisposed || !IsHandleCreated)
                return;

            stationDisplayRestorePending = true;
            try
            {
                BeginInvoke(new Action(() =>
                {
                    stationDisplayRestorePending = false;
                    if (currentGridViewMode == MeterTestGridViewMode.TestPlan && !IsDisposed)
                    {
                        RestoreStationDisplayForSelectedNode(loadFromAccess: true);
                    }
                }));
            }
            catch (ObjectDisposedException)
            {
                stationDisplayRestorePending = false;
            }
            catch (InvalidOperationException)
            {
                stationDisplayRestorePending = false;
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
                    if (selectedNode.Parent?.Tag is MeterTestScheme scheme)
                    {
                        // 测试项父节点使用独立的虚拟子项保存汇总结论。
                        // 这样通信测试下的“地址读取”和日计时下的“三轮读取”仍可保留各自明细。
                        context = CreateParentResultContext(scheme.Name, item.Name);
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
            loadedStationResultContextKeys.Add(CreateStationResultContextKey(context));
            SaveStationDisplayStateToAccess(context, stationNo, state);
            RefreshSchemeTreeStatusIcons();
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
            string contextKey = CreateStationResultContextKey(context);
            if (!loadedStationResultContextKeys.Add(contextKey))
                return;

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

        /// <summary>
        /// 将一套流程的结论同步到指定测试小项。
        /// 波特率检查由一个管理端连接统一完成，但界面结果按工位显示，因此这里逐工位落库。
        /// </summary>
        private void SaveStationConclusions(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> stations,
            IReadOnlyDictionary<int, bool> stationResults,
            string message)
        {
            string now = DateTime.Now.ToString("HH:mm:ss");
            foreach (StationCommunicationConfig station in stations)
            {
                bool passed = stationResults.TryGetValue(station.StationNo, out bool stationPassed)
                    ? stationPassed
                    : false;
                string result = passed ? "合格" : "不合格";
                Color resultColor = passed ? Color.FromArgb(22, 101, 52) : Color.Red;
                SaveStationDisplayState(
                    context,
                    station.StationNo,
                    context.SubItem.Name,
                    string.Empty,
                    result,
                    now,
                    resultColor,
                    message);
            }
        }

        /// <summary>
        /// 根据一组已执行的小项，生成测试项父节点的汇总结果。
        /// 汇总使用最后一个实际执行小项的逐工位结果，并单独保存为“测试项名称”子项记录。
        /// </summary>
        private void SynchronizeParentTestConclusions(
            IReadOnlyList<SelectedSubItemContext> contexts,
            IReadOnlyList<StationCommunicationConfig> stations)
        {
            foreach (IGrouping<(string SchemeName, string TestItemName), SelectedSubItemContext> group in contexts.GroupBy(
                         context => (context.SchemeName, context.TestItemName)))
            {
                SelectedSubItemContext finalContext = GetFinalResultContext(group.ToList());
                LoadStationResultsFromAccess(finalContext);
                SelectedSubItemContext parentContext = CreateParentResultContext(group.Key.SchemeName, group.Key.TestItemName);

                foreach (StationCommunicationConfig station in stations)
                {
                    if (!stationResultCache.TryGetValue(CreateStationResultKey(finalContext, station.StationNo), out StationDisplayState? state))
                    {
                        SaveStationConclusions(
                            parentContext,
                            new[] { station },
                            new Dictionary<int, bool> { [station.StationNo] = false },
                            "测试项未获取到最终结果。");
                        continue;
                    }

                    SaveStationDisplayState(
                        parentContext,
                        station.StationNo,
                        group.Key.TestItemName,
                        state.MeterAddress,
                        state.Result,
                        state.Time,
                        state.ResultColor,
                        state.ToolTip);
                }
            }
        }

        /// <summary>
        /// 创建测试项父节点的结果上下文。
        /// 虚拟子项名称与测试项名称相同，仅用于结果缓存和数据库索引，不参与实际执行。
        /// </summary>
        private static SelectedSubItemContext CreateParentResultContext(string schemeName, string testItemName)
        {
            return new SelectedSubItemContext(
                schemeName,
                testItemName,
                new MeterTestSubItem
                {
                    Name = testItemName,
                    Enabled = false,
                    Description = "测试项汇总结果",
                    ExecutionMode = MeterTestExecutionMode.StationTcp.ToString()
                });
        }

        /// <summary>
        /// 找到当前测试项最后一个实际执行的小项，作为父节点汇总的结果来源。
        /// 日计时优先使用第三轮读取结果，保证汇总的是三轮平均误差结论。
        /// </summary>
        private static SelectedSubItemContext GetFinalResultContext(IReadOnlyList<SelectedSubItemContext> contexts)
        {
            SelectedSubItemContext? dailyResult = contexts.LastOrDefault(context =>
                context.SubItem.ExecutionMode.Equals(
                    MeterTestExecutionMode.ControlPcbDailyTiming.ToString(),
                    StringComparison.OrdinalIgnoreCase) &&
                context.SubItem.DailyTimingStep.Equals("Read", StringComparison.OrdinalIgnoreCase) &&
                context.SubItem.DailyTimingRound == 3);

            return dailyResult ?? contexts[^1];
        }

        /// <summary>按方案、测试项、测试小项和工位构造唯一的界面结果缓存键。</summary>
        private static StationResultKey CreateStationResultKey(SelectedSubItemContext context, int stationNo)
        {
            return new StationResultKey(context.SchemeName, context.TestItemName, context.SubItem.Name, stationNo);
        }

        /// <summary>按方案、测试项和测试小项构造数据库结果加载缓存键。</summary>
        private static string CreateStationResultContextKey(SelectedSubItemContext context)
        {
            return CreateStationResultContextKey(context.SchemeName, context.TestItemName, context.SubItem.Name);
        }

        /// <summary>按方案、测试项和测试小项构造数据库结果加载缓存键。</summary>
        private static string CreateStationResultContextKey(string schemeName, string testItemName, string testSubItemName)
        {
            return string.Join('\u001F', schemeName, testItemName, testSubItemName);
        }

        /// <summary>
        /// 获取广播读地址的默认报文。
        /// </summary>
        public static string GetBroadcastReadAddressFrame()
        {
            return BroadcastReadAddressFrame;
        }

        /// <summary>
        /// 判断当前测试上下文是否属于通信测试。
        /// 通信测试中的准备步骤失败后仍继续执行后续步骤，最终尝试地址读取。
        /// </summary>
        private static bool IsCommunicationTestContext(SelectedSubItemContext context)
        {
            return context.TestItemName.Contains("通信", StringComparison.OrdinalIgnoreCase);
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
                Color.FromArgb(255, 217, 64),
                Color.FromArgb(35, 220, 94),
                Color.FromArgb(255, 71, 87)
            };

            hardwareLayout.SuspendLayout();
            groupHardware.BackColor = Color.FromArgb(232, 239, 236);
            groupHardware.ForeColor = Color.FromArgb(33, 78, 66);
            groupHardware.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            hardwareLayout.BackColor = Color.FromArgb(232, 239, 236);
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
            RoundedMetricPanel container = new()
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 3, 4, 3),
                Padding = new Padding(7, 4, 7, 4),
                BackColor = Color.FromArgb(73, 139, 119),
                BorderColor = Color.FromArgb(42, 105, 88),
                Radius = 8
            };

            TableLayoutPanel cellLayout = new()
            {
                BackColor = Color.Transparent,
                ColumnCount = 2,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                Padding = new Padding(0),
                RowCount = 1
            };
            cellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 48F));
            cellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label titleLabel = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = metricColor,
                Text = metricName,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };

            RoundedMetricValueLabel valueLabel = new()
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(247, 250, 248),
                BorderColor = Color.FromArgb(46, 111, 93),
                Font = new Font("Consolas", 10.5F, FontStyle.Bold, GraphicsUnit.Point),
                ForeColor = metricColor,
                Radius = 6,
                Text = "000.000000",
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(3, 0, 0, 0)
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

            valueLabel.Text = FormatHardwareMetricValue(value);
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
        /// 台体信息采集区域统一展示为 6 位小数，保持界面数值风格一致。
        /// </summary>
        private static string FormatHardwareMetricValue(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "000.000000";

            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal numericValue))
                return numericValue.ToString("0.000000", CultureInfo.InvariantCulture);

            return value.Trim();
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

                // 复制 Bitmap 后释放文件句柄，避免发布目录中的图片被锁定。
                using Image source = Image.FromFile(path);
                picLogo.Image = new Bitmap(source);
                return;
            }

            // 发布包可能只保留程序集文件，此时从内嵌资源加载顶部标识。
            using Stream? resourceStream = typeof(MeterTest).Assembly
                .GetManifestResourceStream("ModelTest.MeterTest.xckj.png");
            if (resourceStream is null)
                return;

            using Image resourceImage = Image.FromStream(resourceStream);
            picLogo.Image = new Bitmap(resourceImage);
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
            SetButtonImage(btnTestResults, "检测.png");
        }

        /// <summary>
        /// 顶部操作区按钮统一采用自绘圆角样式，压掉 WinForms 原生按钮质感。
        /// </summary>
        private void ConfigureOperationButtonStyles()
        {
            ApplyOperationButtonStyle(
                btnStartTest,
                baseColor: Color.FromArgb(31, 132, 92),
                hoverColor: Color.FromArgb(37, 151, 106),
                pressedColor: Color.FromArgb(21, 105, 74),
                borderColor: Color.FromArgb(19, 100, 70));

            ApplyOperationButtonStyle(
                btnStopTest,
                baseColor: Color.FromArgb(185, 54, 54),
                hoverColor: Color.FromArgb(207, 67, 67),
                pressedColor: Color.FromArgb(146, 39, 39),
                borderColor: Color.FromArgb(130, 36, 36));

            ApplyOperationButtonStyle(
                btnTestPlan,
                baseColor: Color.FromArgb(54, 113, 168),
                hoverColor: Color.FromArgb(65, 132, 194),
                pressedColor: Color.FromArgb(42, 88, 135),
                borderColor: Color.FromArgb(37, 83, 126));

            ApplyOperationButtonStyle(
                btnAssetInfo,
                baseColor: Color.FromArgb(88, 125, 112),
                hoverColor: Color.FromArgb(103, 144, 130),
                pressedColor: Color.FromArgb(70, 101, 90),
                borderColor: Color.FromArgb(63, 93, 82));

            ApplyOperationButtonStyle(
                btnTestResults,
                baseColor: Color.FromArgb(93, 93, 154),
                hoverColor: Color.FromArgb(109, 109, 181),
                pressedColor: Color.FromArgb(73, 73, 125),
                borderColor: Color.FromArgb(66, 66, 115));
        }

        /// <summary>
        /// 优化测试过程区域顶部的工位选择控件。
        /// 多工位/单工位仍使用 RadioButton 互斥逻辑，只把外观改成分段切换；
        /// 右侧命令按钮复用圆角自绘按钮，去掉 WinForms 原生质感。
        /// </summary>
        private void ConfigureStationSelectionControlStyles()
        {
            stationSelectionPanel.BackColor = Color.FromArgb(232, 239, 236);
            stationSelectionPanel.Padding = new Padding(12, 9, 0, 0);
            stationSelectionPanel.AutoScroll = true;
            stationSelectionPanel.WrapContents = false;

            groupTestLog.Margin = new Padding(8, 0, 0, 4);
            groupTestLog.Padding = new Padding(8, 5, 8, 8);
            groupTestLog.Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
            rtbTestProcessLog.Margin = new Padding(0);
            rtbTestProcessLog.BorderStyle = BorderStyle.FixedSingle;

            ApplyStationModeToggleStyle(rbMultiStation, isLeftSegment: true);
            ApplyStationModeToggleStyle(rbSingleStation, isLeftSegment: false);
            UpdateStationModeToggleVisualState();

            rbMultiStation.CheckedChanged -= StationModeToggle_CheckedChanged;
            rbMultiStation.CheckedChanged += StationModeToggle_CheckedChanged;
            rbSingleStation.CheckedChanged -= StationModeToggle_CheckedChanged;
            rbSingleStation.CheckedChanged += StationModeToggle_CheckedChanged;

            ApplyStationActionButtonStyle(
                btnSelectAllStations,
                baseColor: Color.FromArgb(43, 119, 174),
                hoverColor: Color.FromArgb(54, 137, 199),
                pressedColor: Color.FromArgb(35, 96, 143),
                borderColor: Color.FromArgb(31, 85, 126));
            ApplyStationActionButtonStyle(
                btnClearStationSelection,
                baseColor: Color.FromArgb(96, 113, 129),
                hoverColor: Color.FromArgb(112, 132, 151),
                pressedColor: Color.FromArgb(75, 90, 105),
                borderColor: Color.FromArgb(68, 82, 96));
            ApplyStationActionButtonStyle(
                btnShutDownSource,
                baseColor: Color.FromArgb(185, 28, 28),
                hoverColor: Color.FromArgb(211, 47, 47),
                pressedColor: Color.FromArgb(145, 24, 24),
                borderColor: Color.FromArgb(127, 29, 29));
            ApplyStationActionButtonStyle(
                btnSaveTestResults,
                baseColor: Color.FromArgb(86, 92, 154),
                hoverColor: Color.FromArgb(105, 112, 181),
                pressedColor: Color.FromArgb(68, 73, 124),
                borderColor: Color.FromArgb(61, 66, 112));
            btnSaveTestResults.Size = new Size(172, 46);
            btnSaveTestResults.MinimumSize = new Size(172, 46);
            ApplyStationActionButtonStyle(
                btnSaveAssetInfo,
                baseColor: Color.FromArgb(31, 132, 92),
                hoverColor: Color.FromArgb(37, 151, 106),
                pressedColor: Color.FromArgb(21, 105, 74),
                borderColor: Color.FromArgb(19, 100, 70));
            ApplyStationActionButtonStyle(
                btnBatchApplyAssetInfo,
                baseColor: Color.FromArgb(88, 125, 112),
                hoverColor: Color.FromArgb(103, 144, 130),
                pressedColor: Color.FromArgb(70, 101, 90),
                borderColor: Color.FromArgb(63, 93, 82));
        }

        /// <summary>
        /// 将工位模式 RadioButton 设置为分段按钮外观，保留 CheckedChanged 互斥行为。
        /// </summary>
        private void ApplyStationModeToggleStyle(RadioButton radioButton, bool isLeftSegment)
        {
            radioButton.Appearance = Appearance.Button;
            radioButton.AutoSize = false;
            radioButton.Size = new Size(130, 46);
            radioButton.Margin = isLeftSegment
                ? new Padding(0, 0, 0, 0)
                : new Padding(0, 0, 14, 0);
            radioButton.TextAlign = ContentAlignment.MiddleCenter;
            radioButton.FlatStyle = FlatStyle.Flat;
            radioButton.FlatAppearance.BorderSize = 1;
            radioButton.FlatAppearance.BorderColor = Color.FromArgb(81, 115, 104);
            radioButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(226, 241, 236);
            radioButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(190, 219, 209);
            radioButton.Font = new Font(radioButton.Font.FontFamily, 9.5F, FontStyle.Bold, GraphicsUnit.Point);
            radioButton.Cursor = Cursors.Hand;
            radioButton.Resize -= StationModeToggle_Resize;
            radioButton.Resize += StationModeToggle_Resize;
            ApplyRoundedRegion(radioButton, 10);
        }

        /// <summary>
        /// 统一测试过程区域命令按钮尺寸、间距和自绘状态。
        /// </summary>
        private void ApplyStationActionButtonStyle(
            Button button,
            Color baseColor,
            Color hoverColor,
            Color pressedColor,
            Color borderColor)
        {
            button.Size = new Size(150, 46);
            button.MinimumSize = new Size(150, 46);
            button.Margin = new Padding(10, 0, 0, 0);
            button.TextAlign = ContentAlignment.MiddleCenter;
            ApplyOperationButtonStyle(button, baseColor, hoverColor, pressedColor, borderColor);
            button.Padding = new Padding(8, 0, 8, 0);
        }

        /// <summary>工位模式分段切换状态变化后刷新选中/未选中配色。</summary>
        private void StationModeToggle_CheckedChanged(object? sender, EventArgs e)
        {
            UpdateStationModeToggleVisualState();
        }

        /// <summary>工位模式分段控件尺寸变化后同步圆角区域。</summary>
        private void StationModeToggle_Resize(object? sender, EventArgs e)
        {
            if (sender is RadioButton radioButton)
                ApplyRoundedRegion(radioButton, 10);
        }

        /// <summary>
        /// 刷新多工位/单工位分段切换按钮的选中和未选中颜色。
        /// </summary>
        private void UpdateStationModeToggleVisualState()
        {
            ApplyStationModeToggleState(rbMultiStation);
            ApplyStationModeToggleState(rbSingleStation);
        }

        /// <summary>刷新单个工位模式按钮视觉状态。</summary>
        private static void ApplyStationModeToggleState(RadioButton radioButton)
        {
            bool selected = radioButton.Checked;
            radioButton.BackColor = selected
                ? Color.FromArgb(31, 132, 92)
                : Color.FromArgb(245, 249, 247);
            radioButton.ForeColor = selected
                ? Color.White
                : Color.FromArgb(44, 75, 65);
            radioButton.FlatAppearance.BorderColor = selected
                ? Color.FromArgb(19, 100, 70)
                : Color.FromArgb(107, 138, 128);
        }

        /// <summary>
        /// 绑定单个操作按钮的现代化视觉状态和交互反馈。
        /// </summary>
        private void ApplyOperationButtonStyle(
            Button button,
            Color baseColor,
            Color hoverColor,
            Color pressedColor,
            Color borderColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = baseColor;
            button.FlatAppearance.MouseDownBackColor = pressedColor;
            button.UseVisualStyleBackColor = false;
            button.BackColor = Color.Transparent;
            button.ForeColor = Color.White;
            button.Cursor = Cursors.Hand;
            button.Padding = new Padding(16, 0, 16, 0);
            button.Font = new Font(button.Font.FontFamily, button.Font.Size, FontStyle.Bold);

            operationButtonVisualStates[button] = new OperationButtonVisualState(
                baseColor,
                hoverColor,
                pressedColor,
                borderColor,
                Color.White,
                radius: 14);

            button.Paint -= OperationButton_Paint;
            button.Paint += OperationButton_Paint;
            button.MouseEnter -= OperationButton_MouseEnter;
            button.MouseEnter += OperationButton_MouseEnter;
            button.MouseLeave -= OperationButton_MouseLeave;
            button.MouseLeave += OperationButton_MouseLeave;
            button.MouseDown -= OperationButton_MouseDown;
            button.MouseDown += OperationButton_MouseDown;
            button.MouseUp -= OperationButton_MouseUp;
            button.MouseUp += OperationButton_MouseUp;
            button.EnabledChanged -= OperationButton_StateChanged;
            button.EnabledChanged += OperationButton_StateChanged;
            button.Resize -= OperationButton_Resize;
            button.Resize += OperationButton_Resize;

            ApplyOperationButtonRegion(button);
        }

        /// <summary>
        /// 自绘按钮背景、圆角边框、图标和文字。
        /// </summary>
        private void OperationButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button button || !operationButtonVisualStates.TryGetValue(button, out OperationButtonVisualState state))
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new(0, 0, button.Width - 1, button.Height - 1);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            Color fillColor = GetOperationButtonFillColor(button, state);
            using GraphicsPath path = CreateRoundedRectanglePath(bounds, state.Radius);
            using SolidBrush backgroundBrush = new(fillColor);
            using Pen borderPen = new(button.Enabled ? state.BorderColor : Color.FromArgb(176, 184, 180), 1F);
            e.Graphics.FillPath(backgroundBrush, path);
            e.Graphics.DrawPath(borderPen, path);

            DrawOperationButtonContent(button, e.Graphics, bounds, button.Enabled ? state.ForeColor : Color.FromArgb(115, 124, 119));

            if (button.Focused && button.Enabled)
            {
                Rectangle focusBounds = Rectangle.Inflate(bounds, -5, -5);
                using GraphicsPath focusPath = CreateRoundedRectanglePath(focusBounds, Math.Max(6, state.Radius - 4));
                using Pen focusPen = new(Color.FromArgb(180, 255, 255, 255), 1F);
                e.Graphics.DrawPath(focusPen, focusPath);
            }
        }

        /// <summary>
        /// 绘制按钮图标和文字，保证图标加文字整体居中。
        /// </summary>
        private static void DrawOperationButtonContent(Button button, Graphics graphics, Rectangle bounds, Color textColor)
        {
            const int iconSize = 24;
            const int iconTextGap = 8;
            const int horizontalPadding = 16;
            int iconPartWidth = button.Image is null ? 0 : iconSize + iconTextGap;
            int maxTextWidth = Math.Max(20, bounds.Width - horizontalPadding * 2 - iconPartWidth);
            Size preferredTextSize = TextRenderer.MeasureText(button.Text, button.Font);
            int textWidth = Math.Min(preferredTextSize.Width, maxTextWidth);
            int contentWidth = iconPartWidth + textWidth;
            int startX = bounds.Left + Math.Max(horizontalPadding, (bounds.Width - contentWidth) / 2);

            if (button.Image is not null)
            {
                int iconY = bounds.Top + (bounds.Height - iconSize) / 2;
                graphics.DrawImage(button.Image, new Rectangle(startX, iconY, iconSize, iconSize));
                startX += iconPartWidth;
            }

            Rectangle textBounds = new(startX, bounds.Top, Math.Max(20, bounds.Right - startX - horizontalPadding), bounds.Height);
            TextRenderer.DrawText(
                graphics,
                button.Text,
                button.Font,
                textBounds,
                textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }

        /// <summary>
        /// 根据当前鼠标状态返回按钮背景色。
        /// </summary>
        private static Color GetOperationButtonFillColor(Button button, OperationButtonVisualState state)
        {
            if (!button.Enabled)
                return Color.FromArgb(218, 226, 222);

            return state.IsPressed
                ? state.PressedColor
                : state.IsHovered
                    ? state.HoverColor
                    : state.BaseColor;
        }

        /// <summary>进入操作按钮时启用悬停视觉状态。</summary>
        private void OperationButton_MouseEnter(object? sender, EventArgs e)
        {
            SetOperationButtonHoverState(sender, isHovered: true);
        }

        /// <summary>离开操作按钮时清除悬停和按压视觉状态。</summary>
        private void OperationButton_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button button && operationButtonVisualStates.TryGetValue(button, out OperationButtonVisualState state))
            {
                state.IsHovered = false;
                state.IsPressed = false;
                button.Invalidate();
            }
        }

        /// <summary>按下鼠标左键时启用操作按钮按压状态。</summary>
        private void OperationButton_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
                return;

            SetOperationButtonPressedState(sender, isPressed: true);
        }

        /// <summary>释放鼠标时清除操作按钮按压状态。</summary>
        private void OperationButton_MouseUp(object? sender, MouseEventArgs e)
        {
            SetOperationButtonPressedState(sender, isPressed: false);
        }

        /// <summary>按钮启用状态或文本变化后请求重新绘制现代化外观。</summary>
        private void OperationButton_StateChanged(object? sender, EventArgs e)
        {
            if (sender is Button button)
                button.Invalidate();
        }

        /// <summary>按钮尺寸变化后重新计算圆角点击区域。</summary>
        private void OperationButton_Resize(object? sender, EventArgs e)
        {
            if (sender is Button button)
                ApplyOperationButtonRegion(button);
        }

        /// <summary>设置指定操作按钮的悬停状态并触发重绘。</summary>
        private void SetOperationButtonHoverState(object? sender, bool isHovered)
        {
            if (sender is Button button && operationButtonVisualStates.TryGetValue(button, out OperationButtonVisualState state))
            {
                state.IsHovered = isHovered;
                button.Invalidate();
            }
        }

        /// <summary>设置指定操作按钮的按压状态并触发重绘。</summary>
        private void SetOperationButtonPressedState(object? sender, bool isPressed)
        {
            if (sender is Button button && operationButtonVisualStates.TryGetValue(button, out OperationButtonVisualState state))
            {
                state.IsPressed = isPressed;
                button.Invalidate();
            }
        }

        /// <summary>
        /// 同步控件点击热区为圆角，避免视觉圆角但鼠标区域仍是直角。
        /// </summary>
        private void ApplyOperationButtonRegion(Button button)
        {
            if (!operationButtonVisualStates.TryGetValue(button, out OperationButtonVisualState state) ||
                button.Width <= 0 ||
                button.Height <= 0)
            {
                return;
            }

            Rectangle bounds = new(0, 0, button.Width, button.Height);
            using GraphicsPath path = CreateRoundedRectanglePath(bounds, state.Radius);
            Region? oldRegion = button.Region;
            button.Region = new Region(path);
            oldRegion?.Dispose();
        }

        /// <summary>
        /// 创建圆角矩形路径。
        /// </summary>
        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            Rectangle arc = new(bounds.Location, new Size(diameter, diameter));
            GraphicsPath path = new();

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// 顶部操作区按钮的视觉状态。
        /// </summary>
        private sealed class OperationButtonVisualState
        {
            /// <summary>创建一组操作按钮基础色、交互色、边框色和圆角参数。</summary>
            public OperationButtonVisualState(
                Color baseColor,
                Color hoverColor,
                Color pressedColor,
                Color borderColor,
                Color foreColor,
                int radius)
            {
                BaseColor = baseColor;
                HoverColor = hoverColor;
                PressedColor = pressedColor;
                BorderColor = borderColor;
                ForeColor = foreColor;
                Radius = radius;
            }

            public Color BaseColor { get; }
            public Color HoverColor { get; }
            public Color PressedColor { get; }
            public Color BorderColor { get; }
            public Color ForeColor { get; }
            public int Radius { get; }
            public bool IsHovered { get; set; }
            public bool IsPressed { get; set; }
        }

        /// <summary>
        /// 台体信息采集区域的圆角指标容器，去掉WinForms原生直角边框。
        /// </summary>
        private sealed class RoundedMetricPanel : Panel
        {
            /// <summary>启用自绘、双缓冲和尺寸变化重绘，减少台体指标区域闪烁。</summary>
            public RoundedMetricPanel()
            {
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
            }

            public int Radius { get; set; } = 8;

            public Color BorderColor { get; set; } = Color.FromArgb(42, 105, 88);

            /// <summary>绘制指标容器的圆角背景和边框。</summary>
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle bounds = new(0, 0, Width - 1, Height - 1);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                using GraphicsPath path = CreateRoundedRectanglePath(bounds, Radius);
                using SolidBrush fillBrush = new(BackColor);
                using Pen borderPen = new(BorderColor, 1F);
                e.Graphics.FillPath(fillBrush, path);
                e.Graphics.DrawPath(borderPen, path);
            }

            /// <summary>尺寸变化时同步真实窗口区域，消除圆角外侧露白。</summary>
            protected override void OnResize(EventArgs eventargs)
            {
                base.OnResize(eventargs);
                ApplyRoundedRegion(this, Radius);
            }
        }

        /// <summary>
        /// 台体信息采集区域的圆角数值框，负责绘制粗体彩色读数。
        /// </summary>
        private sealed class RoundedMetricValueLabel : Label
        {
            /// <summary>启用数值标签自绘和双缓冲。</summary>
            public RoundedMetricValueLabel()
            {
                SetStyle(
                    ControlStyles.UserPaint |
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
            }

            public int Radius { get; set; } = 6;

            public Color BorderColor { get; set; } = Color.FromArgb(46, 111, 93);

            /// <summary>绘制圆角数值框、边框及居中的粗体彩色读数。</summary>
            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle bounds = new(0, 0, Width - 1, Height - 1);
                if (bounds.Width <= 0 || bounds.Height <= 0)
                    return;

                using GraphicsPath path = CreateRoundedRectanglePath(bounds, Radius);
                using SolidBrush fillBrush = new(BackColor);
                using Pen borderPen = new(BorderColor, 1F);
                e.Graphics.FillPath(fillBrush, path);
                e.Graphics.DrawPath(borderPen, path);

                Rectangle textBounds = Rectangle.Inflate(bounds, -5, 0);
                TextRenderer.DrawText(
                    e.Graphics,
                    Text,
                    Font,
                    textBounds,
                    ForeColor,
                    TextFormatFlags.HorizontalCenter |
                    TextFormatFlags.VerticalCenter |
                    TextFormatFlags.EndEllipsis |
                    TextFormatFlags.NoPadding);
            }

            /// <summary>尺寸变化时同步数值框的圆角窗口区域。</summary>
            protected override void OnResize(EventArgs e)
            {
                base.OnResize(e);
                ApplyRoundedRegion(this, Radius);
            }
        }

        /// <summary>
        /// 同步圆角控件的窗口区域，避免只绘制圆角但外侧矩形背景露白。
        /// </summary>
        private static void ApplyRoundedRegion(Control control, int radius)
        {
            if (control.Width <= 0 || control.Height <= 0)
                return;

            Rectangle bounds = new(0, 0, control.Width, control.Height);
            using GraphicsPath path = CreateRoundedRectanglePath(bounds, radius);
            Region? oldRegion = control.Region;
            control.Region = new Region(path);
            oldRegion?.Dispose();
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
        private static string GetMeterTestConfigPath(string fileName = ThreePhasePlanConfigFileName)
        {
            string outputConfigPath = Path.Combine(AppContext.BaseDirectory, "MeterTest", "config", fileName);
            if (File.Exists(outputConfigPath))
            {
                return outputConfigPath;
            }

            return Path.Combine(AppContext.BaseDirectory, "config", fileName);
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

        /// <summary>用户勾选变化对应的工位上电或下电请求。</summary>
        private sealed record StationPowerSelectionChange(int StationNo, bool IsSelected);

        /// <summary>工位结果缓存键。</summary>
        private sealed record StationResultKey(string SchemeName, string TestItemName, string TestSubItemName, int StationNo);

        /// <summary>工位在界面上的完整显示状态。</summary>
        private sealed record StationDisplayState(string TestContent, string MeterAddress, string Result, string Time, Color ResultColor, string ToolTip);

        /// <summary>右侧日志区域的一条有序日志记录。</summary>
        private sealed record TestProcessLogEntry(long Sequence, int? StationNo, string Text);

        /// <summary>
        /// 当前表格的显示模式。
        /// </summary>
        private enum MeterTestGridViewMode
        {
            TestPlan,
            AssetInfo,
            TestResults
        }

        /// <summary>方案树节点的四态测试结论。</summary>
        private enum SchemeNodeStatus
        {
            Pending,
            Running,
            Passed,
            Failed
        }
    }
}
