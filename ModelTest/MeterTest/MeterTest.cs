using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.Text.RegularExpressions;
using ModelTest.CustomControl;
using ModelTest.Protocol;
using ModelTest.Tools;

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
        private const string SchemeStatusPassedImageKey = "StatusPassed";
        private const string SchemeStatusFailedImageKey = "StatusFailed";
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
        private const byte MeterStandardActiveConstantCommand = 0xA2;
        private const byte MeterActiveConstantCommand = 0xA0;
        private const byte MeterBasicErrorCommand38 = 0x38;
        private const byte BasicErrorStartOperation = 0x00;
        private const byte BasicErrorResultOperation = 0xAA;
        private const byte ActivePulseType = 0x00;
        private readonly Dictionary<string, Label> hardwareValueLabels = new();
        private readonly MeterTestConfigService configService = new();
        private readonly MeterTestStationConfigService stationConfigService = new();
        private readonly MeterTestAccessDatabaseService accessDatabaseService = new();
        private readonly MeterTestBenchTypeSwitchService benchTypeSwitchService = new();
        private readonly MeterTestSourceControlService sourceControlService = new();
        private readonly MeterTestSerialPortServerService serialPortServerService = new();
        private readonly MeterTestStationPowerService stationPowerService = new();
        private readonly MeterTestControlPcbConnectionManager controlPcbConnectionManager = new();
        private readonly MeterTestBluetoothInterfaceService bluetoothInterfaceService = new();
        private readonly MeterTestBasicErrorService basicErrorService;
        private readonly CancellationTokenSource stationPowerControlCts = new();
        private readonly string configFilePath;
        private readonly string stationConfigFilePath;
        private MeterTestPlanConfig meterTestPlanConfig = new();
        private CancellationTokenSource? executionCts;
        private readonly Dictionary<StationResultKey, StationDisplayState> stationResultCache = new();
        private readonly ConcurrentDictionary<int, float> startingErrorResults = new();
        private readonly Dictionary<int, List<TestProcessLogEntry>> stationTestLogEntries = new();
        private readonly List<TestProcessLogEntry> commonTestLogEntries = new();
        private ImageList? schemeStatusImageList;
        private string currentRunId = Guid.NewGuid().ToString("N");
        private long testLogSequence;
        private int selectedTestLogStationNo = 1;
        private bool serialPortServerBaudFlowExecuted;
        private bool serialPortServerBaudFlowSucceeded;
        private IReadOnlyDictionary<int, bool> serialPortServerBaudStationResults = new Dictionary<int, bool>();
        private IReadOnlyDictionary<int, SerialPortServerStationTrace> serialPortServerStationTraces =
            new Dictionary<int, SerialPortServerStationTrace>();
        private bool dailyTimingFlowExecuted;
        private bool dailyTimingFlowSucceeded;
        private readonly ConcurrentDictionary<int, byte> creepingActiveStations = new();
        private readonly ConcurrentDictionary<int, CreepingPulseMeasurement> creepingPulseResults = new();
        private Task controlPcbInitializationTask = Task.CompletedTask;
        private bool isUpdatingStationSelection;
        private bool isLoadingStationConfig;
        private bool isLoadingMeterArchive;
        private bool isLoadingBarcodeSetting;
        private bool isApplyingBarcodeExtraction;
        private int assetBarcodeStartIndex = 8;
        private int assetBarcodeEndIndex = 20;
        private MeterTestGridViewMode currentGridViewMode = MeterTestGridViewMode.TestPlan;

        public MeterTest()
        {
            InitializeComponent();
            basicErrorService = new MeterTestBasicErrorService(sourceControlService, controlPcbConnectionManager);
            ConfigureBufferedRendering();

            // 首屏初始化期间隐藏并冻结根布局，避免数据库、表格和动态控件逐项显示。
            mainLayout.Visible = false;
            SuspendInitialLayout();
            try
            {
                configFilePath = GetMeterTestConfigPath();
                stationConfigFilePath = GetMeterTestStationConfigPath();
                ConfigureDataGridViewSorting();
                InitializeStationProcessGrid();
                accessDatabaseService.EnsureInitialized();
                LoadAssetBarcodeSettingToInputs();
                InitializeHardwareCollectionGrid();
                BindEvents();
                LoadMeterArchivesToGrid();
                InitializeSchemeStatusImages();
                LoadMeterTestPlanConfig();
                LoadHeaderLogo();
                LoadOperationButtonImages();
                ApplyTestPlanView();
                ConfigureWindowBounds();
            }
            finally
            {
                ResumeInitialLayout();
                mainLayout.Visible = true;
            }
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
        /// 绑定窗体事件。
        /// 这里统一把按钮、表格、方案树的交互行为连起来。
        /// </summary>
        private void BindEvents()
        {
            sourceControlService.StandardValuesUpdated += SourceControlService_StandardValuesUpdated;
            btnStartTest.Click += async (_, _) => await StartSelectedTestAsync();
            btnStopTest.Click += (_, _) => CancelRunningTest();
            btnTestPlan.Click += async (_, _) => await RefreshTestPlanAndMeterArchiveAsync();
            btnAssetInfo.Click += (_, _) => RefreshMeterArchiveDisplay();
            btnSaveAssetInfo.Click += (_, _) => SaveAllAssetInfo();
            btnBatchApplyAssetInfo.Click += (_, _) => BatchApplyFirstStationAssetInfo();
            btnSelectAllStations.Click += async (_, _) => await SetAllStationSelectionAsync(true);
            btnClearStationSelection.Click += async (_, _) => await SetAllStationSelectionAsync(false);
            rbSingleStation.CheckedChanged += async (_, _) => await ApplySingleStationSelectionRuleAsync();
            tbxBarcodeStartIndex.TextChanged += (_, _) => SaveBarcodeSettingFromInputs();
            tbxBarcodeEndIndex.TextChanged += (_, _) => SaveBarcodeSettingFromInputs();
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

                if (!isLoadingMeterArchive && e.RowIndex >= 0 && e.ColumnIndex == colStationBarcode.Index)
                {
                    DataGridViewRow changedRow = stationGrid.Rows[e.RowIndex];
                    ApplyBarcodeExtractionToRow(changedRow);
                    SaveMeterArchiveFromRow(changedRow);
                    await DeselectStationWithoutCompleteAssetAsync(changedRow);
                    RefreshSchemeTreeStatusIcons();
                    return;
                }

                if (!isLoadingMeterArchive && e.RowIndex >= 0 && IsEditableAssetColumn(e.ColumnIndex))
                {
                    DataGridViewRow changedRow = stationGrid.Rows[e.RowIndex];
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
                    RestoreStationDisplayForSelectedNode();
                }
            };
            Shown += async (_, _) =>
            {
                // 窗体首次显示后立即连接所有去重后的控制PCB端点；测试步骤只等待此初始化任务，不再建连。
                controlPcbInitializationTask = InitializeControlPcbConnectionsAsync();
                await controlPcbInitializationTask;
            };
            FormClosed += async (_, _) =>
            {
                stationPowerControlCts.Cancel();
                sourceControlService.StandardValuesUpdated -= SourceControlService_StandardValuesUpdated;
                sourceControlService.Dispose();
                try
                {
                    await controlPcbInitializationTask;
                }
                catch (OperationCanceledException)
                {
                }
                await controlPcbConnectionManager.DisposeAsync();
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

        /// <summary>
        /// 切换到测试方案视图并刷新方案与结果缓存。
        /// </summary>
        private async Task RefreshTestPlanAndMeterArchiveAsync()
        {
            LoadMeterArchivesToGrid();
            LoadMeterTestPlanConfig();
            ApplyTestPlanView();
            // 方案文件可能刚被现场修改；初始化管理器只会为新增端点建连，已有端点不会重复连接。
            controlPcbInitializationTask = InitializeControlPcbConnectionsAsync();
            await controlPcbInitializationTask;

            // 扫码成功即表示该工位进入本轮测试范围；切回方案视图时统一勾选并执行上电联动。
            await SetAllStationSelectionAsync(true);
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
            LoadAllStationResultsFromAccess();
            BuildSchemeTree();
            AddProcessLog("系统", "配置加载", true, $"已加载配置：{configFilePath}", 0);
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
                        RunOnUiThread(() => AddProcessLog("系统", "控制PCB连接", !message.Contains("失败", StringComparison.OrdinalIgnoreCase), message, 0));
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

            schemeTreeView.ExpandAll();

            if (schemeTreeView.Nodes.Count > 0)
            {
                schemeTreeView.SelectedNode = schemeTreeView.Nodes[0];
            }

            schemeTreeView.EndUpdate();
            RefreshSchemeTreeStatusIcons();
            UpdateStartButtonText();
        }

        /// <summary>
        /// 加载方案树状态图标。优先使用 png 目录中的红灯、灰灯、绿灯，文件缺失时生成颜色占位灯。
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

            List<int> eligibleStations = stationGrid.Rows
                .Cast<DataGridViewRow>()
                .Where(row => !row.IsNewRow && HasCompleteAssetForTest(row))
                .Select(row => Convert.ToInt32(row.Cells[colStationNo.Index].Value))
                .ToList();

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
        /// 计算一个测试小项的状态：任一不合格为红灯，全部合格为绿灯，其余为灰灯。
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
            foreach (int stationNo in eligibleStations)
            {
                StationResultKey key = new(schemeName, testItemName, testSubItemName, stationNo);
                if (!stationResultCache.TryGetValue(key, out StationDisplayState? state))
                {
                    allPassed = false;
                    continue;
                }

                if (state.Result.Equals("不合格", StringComparison.OrdinalIgnoreCase))
                    return SchemeNodeStatus.Failed;

                if (!state.Result.Equals("合格", StringComparison.OrdinalIgnoreCase))
                    allPassed = false;
            }

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

            // 等待程序启动阶段的控制PCB连接任务结束；这里只等待，不会发起新的ConnectAsync。
            await controlPcbInitializationTask;

            executionCts = new CancellationTokenSource();
            currentRunId = Guid.NewGuid().ToString("N");
            serialPortServerBaudFlowExecuted = false;
            serialPortServerBaudFlowSucceeded = false;
            serialPortServerBaudStationResults = new Dictionary<int, bool>();
            serialPortServerStationTraces = new Dictionary<int, SerialPortServerStationTrace>();
            dailyTimingFlowExecuted = false;
            dailyTimingFlowSucceeded = false;
            creepingActiveStations.Clear();
            creepingPulseResults.Clear();
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

                // 子项全部执行结束后，再生成“通信测试”“日计时”等父测试项的汇总结果。
                // 父节点使用独立结果记录，不覆盖树下各个测试小项的明细结果。
                SynchronizeParentTestConclusions(testContexts, selectedStations);
                RestoreStationDisplayForSelectedNode();
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
        /// 通信测试中的串口服务器波特率检查流程。
        /// 按 IP 分组读取并核对端口参数，只对不一致端口发送解锁和设置报文。
        /// 设置参数使用立即生效模式，不发送保存重启报文。
        /// </summary>
        private async Task<SerialPortServerBaudFlowResult> EnsureSerialServerBaudRatesAsync(
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            // 管理端口 64444 是按串口服务器 IP 连接的，同一 IP 下不论有多少工位都只能建立一次连接。
            // 先规范化并物化分组，后续创建任务和映射结果都复用同一组列表，避免重复分组或顺序偏差。
            List<IGrouping<string, StationCommunicationConfig>> serverGroups = selectedStations
                .GroupBy(station => NormalizeSerialPortServerIp(station.Ip), StringComparer.OrdinalIgnoreCase)
                .ToList();
            List<Task<MeterTestSerialPortServerResult>> tasks = serverGroups
                .Select(group => serialPortServerService.EnsureBaudRatesAsync(
                    group.Key,
                    group.Select(station => new MeterTestSerialPortBaudRequirement(
                        station.StationNo,
                        station.Port,
                        station.BaudRate)).ToList(),
                    cancellationToken))
                .ToList();

            MeterTestSerialPortServerResult[] results = await Task.WhenAll(tasks);
            bool allSucceeded = true;
            Dictionary<int, bool> stationResults = new();
            Dictionary<int, SerialPortServerStationTrace> stationTraces = new();

            AddProcessLog(
                "串口服务器/64444",
                "连接去重",
                true,
                $"选中 {selectedStations.Count} 个工位，按 IP 去重后建立 {serverGroups.Count} 个管理端连接。",
                0);

            for (int groupIndex = 0; groupIndex < serverGroups.Count; groupIndex++)
            {
                IGrouping<string, StationCommunicationConfig> group = serverGroups[groupIndex];
                MeterTestSerialPortServerResult result = results[groupIndex];
                string sharedConnectionMessage =
                    $"同一IP的工位 {string.Join(",", group.Select(station => station.StationNo))} 共用一次 {group.Key}:{MeterTestSerialPortServerService.ManagementPort} 管理端连接。";
                List<string> sharedDetails = new() { sharedConnectionMessage };
                sharedDetails.AddRange(result.Details);
                string details = sharedDetails.Count == 0
                    ? result.Message
                    : $"{result.Message}{Environment.NewLine}{string.Join(Environment.NewLine, sharedDetails)}";

                AddProcessLog(
                    $"串口服务器/{group.Key}:64444",
                    "电表波特率检查",
                    result.Success,
                    details,
                    0);

                foreach (StationCommunicationConfig station in group)
                {
                    // 一个管理连接对应一个 IP 下的多个工位；当前服务返回的是该管理流程的整体结果，
                    // 因此将同一组结果同步给该 IP 下的每个工位，保证四个波特率子节点都能回显结论。
                    stationResults[station.StationNo] = result.Success;
                    stationTraces[station.StationNo] = new SerialPortServerStationTrace(
                        group.Key,
                        result.Success,
                        result.Message,
                        sharedDetails);
                }

                allSucceeded &= result.Success;
            }

            return new SerialPortServerBaudFlowResult(allSucceeded, stationResults, stationTraces);
        }

        /// <summary>
        /// 规范化串口服务器 IP，确保带空格、大小写差异或等价 IP 文本不会产生重复的 64444 连接。
        /// </summary>
        private static string NormalizeSerialPortServerIp(string ipAddress)
        {
            string normalized = ipAddress.Trim();
            return IPAddress.TryParse(normalized, out IPAddress? parsedAddress)
                ? parsedAddress.ToString()
                : normalized.ToUpperInvariant();
        }

        /// <summary>
        /// 执行一个测试上下文。
        /// 配置源控制的小项先切换台体类型并等待0x82应答，再执行源控制和具体测试流程。
        /// </summary>
        private async Task ExecuteTestContextAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            if (UsesBasicErrorPointExecution(context.SubItem))
            {
                await ExecuteBasicErrorPointAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesBluetoothStationTcpExecution(context.SubItem))
            {
                await ExecuteBluetoothInterfaceStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            bool isCommunicationTest = IsCommunicationTestContext(context);
            bool sourceControlSucceeded = true;
            if (!string.IsNullOrWhiteSpace(context.SubItem.SourceControlConfig) ||
                UsesStartingSourceExecution(context.SubItem) ||
                UsesCreepingSourceExecution(context.SubItem))
            {
                // 台体类型是控源前置条件。0x82未收到正确应答时不允许打开源串口或升源。
                bool benchTypeSwitchSucceeded = await TryExecuteBenchTypeSwitchAsync(
                    context,
                    selectedStations,
                    cancellationToken);
                sourceControlSucceeded = benchTypeSwitchSucceeded &&
                    await TryExecuteSourceControlAsync(context, cancellationToken);
            }

            // 通信测试中的单个准备步骤失败后仍继续执行后续步骤，最后一定尝试地址读取。
            if (!sourceControlSucceeded && !isCommunicationTest)
            {
                return;
            }

            // StartingSource/CreepingSource 的完整结果就是“下发升源 + 20秒内标准表达标判断”，
            // 不再进入普通工位 TCP 一发一收，否则空请求会覆盖已经得到的升源结论。
            if (UsesStartingSourceExecution(context.SubItem) ||
                UsesCreepingSourceExecution(context.SubItem))
            {
                return;
            }

            if (UsesPlannedTestExecution(context.SubItem))
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

            if (UsesSerialPortServerBaudRateExecution(context.SubItem))
            {
                await ExecuteSerialPortServerBaudRateStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesControlPcbDailyTimingExecution(context.SubItem))
            {
                await ExecuteControlPcbDailyTimingStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesControlPcbCreepingStartExecution(context.SubItem))
            {
                await ExecuteControlPcbCreepingStartAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesCreepingWaitExecution(context.SubItem))
            {
                await ExecuteCreepingWaitAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesControlPcbCreepingReadExecution(context.SubItem))
            {
                await ExecuteControlPcbCreepingReadAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesCreepingPulseJudgeExecution(context.SubItem))
            {
                ExecuteCreepingPulseJudgeStep(context, selectedStations);
                return;
            }

            if (UsesControlPcbStartingErrorExecution(context.SubItem))
            {
                await ExecuteControlPcbStartingErrorStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesStartingTimeWaitExecution(context.SubItem))
            {
                await ExecuteStartingTimeWaitAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesControlPcbStartingErrorReadExecution(context.SubItem))
            {
                await ExecuteControlPcbStartingErrorReadStepAsync(context, selectedStations, cancellationToken);
                return;
            }

            if (UsesStartingErrorJudgeExecution(context.SubItem))
            {
                ExecuteStartingErrorJudgeStep(context, selectedStations);
                return;
            }

            List<Task> stationTasks = selectedStations
                .Select(station => ExecuteStationSubItemAsync(station, context, cancellationToken))
                .ToList();

            await Task.WhenAll(stationTasks);
        }

        /// <summary>
        /// 执行方案树中的一个波特率检查小项。
        /// 完整串口服务器流程只发送一次，后续树节点用于展示流程阶段并继续向地址读取推进。
        /// </summary>
        private async Task ExecuteSerialPortServerBaudRateStepAsync(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            if (!serialPortServerBaudFlowExecuted)
            {
                serialPortServerBaudFlowExecuted = true;
                SerialPortServerBaudFlowResult flowResult = await EnsureSerialServerBaudRatesAsync(
                    selectedStations,
                    cancellationToken);
                serialPortServerBaudFlowSucceeded = flowResult.Succeeded;
                serialPortServerBaudStationResults = flowResult.StationResults;
                serialPortServerStationTraces = flowResult.StationTraces;

                SaveStationConclusions(
                    context,
                    selectedStations,
                    serialPortServerBaudStationResults,
                    "串口服务器波特率流程完成。请查看过程日志了解读取、校验和修改明细。");
                WriteSerialPortServerStepLogs(context, selectedStations);

                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    serialPortServerBaudFlowSucceeded,
                    serialPortServerBaudFlowSucceeded
                        ? "串口服务器波特率检查流程完成，继续执行后续测试。"
                        : "串口服务器波特率检查存在失败步骤，已继续执行后续测试。",
                    Math.Max(0, Environment.TickCount64 - startTicks));
                return;
            }

            SaveStationConclusions(
                context,
                selectedStations,
                serialPortServerBaudStationResults,
                serialPortServerBaudFlowSucceeded
                    ? "该波特率步骤已由前置流程完成。"
                    : "前置波特率流程存在失败，但当前步骤不阻断后续地址读取。");
            WriteSerialPortServerStepLogs(context, selectedStations);

            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                serialPortServerBaudFlowSucceeded,
                serialPortServerBaudFlowSucceeded
                    ? "该步骤已由前置波特率流程完成，无需重复发送报文。"
                    : "前置波特率流程存在失败，当前步骤不阻断后续地址读取。",
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        /// <summary>
        /// 将串口服务器完整跟踪信息按当前方案子项拆分，并逐工位写入右侧日志和通信日志文件。
        /// 同一 IP 下的管理报文只实际发送一次，但每个工位都需要能够查看与自身相关的流程记录。
        /// </summary>
        private void WriteSerialPortServerStepLogs(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> stations)
        {
            foreach (StationCommunicationConfig station in stations)
            {
                if (!serialPortServerStationTraces.TryGetValue(station.StationNo, out SerialPortServerStationTrace? trace))
                {
                    continue;
                }

                List<string> stepDetails = trace.Details
                    .Where(detail => IsSerialPortServerDetailForStation(detail, station))
                    .Where(detail => IsSerialPortServerDetailForStep(detail, context.SubItem.SerialPortServerStep))
                    .ToList();
                if (stepDetails.Count == 0)
                {
                    stepDetails.Add(GetSerialPortServerStepFallback(context.SubItem.SerialPortServerStep, trace));
                }

                string resultText = trace.Success ? "合格" : "不合格";
                string logBlock = string.Join(
                    Environment.NewLine,
                    new[]
                    {
                        StationLogSeparator,
                        $"测试小项：{context.SubItem.Name}",
                        $"串口服务器：{trace.IpAddress}:{MeterTestSerialPortServerService.ManagementPort}",
                        $"工位配置：工位{station.StationNo}，端口={station.Port}，波特率={station.BaudRate}"
                    }
                    .Concat(stepDetails)
                    .Concat(new[]
                    {
                        $"步骤结论：{resultText}",
                        StationLogSeparator
                    }));

                LogMessage.MeterTestStationRawLog(context.TestItemName, station.StationNo, logBlock);
                AppendTestLog(
                    station.StationNo,
                    $"{context.TestItemName}/工位{station.StationNo}/{context.SubItem.Name}",
                    "串口服务器日志",
                    logBlock);
            }
        }

        /// <summary>
        /// 排除同一 IP 组内其他工位的端口和结论明细，保留公共连接/报文日志。
        /// </summary>
        private static bool IsSerialPortServerDetailForStation(
            string detail,
            StationCommunicationConfig station)
        {
            int? detailStationNo = TryExtractSingleStationNo(detail);
            if (detailStationNo.HasValue && detailStationNo.Value != station.StationNo)
            {
                return false;
            }

            if (detail.StartsWith("读取端口 ", StringComparison.OrdinalIgnoreCase))
            {
                return detail.Contains($"读取端口 {station.Port}（", StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        /// <summary>
        /// 将完整波特率同步日志按 Connect、ReadParameters、Compare、Apply 四个方案节点分类。
        /// </summary>
        private static bool IsSerialPortServerDetailForStep(string detail, string step)
        {
            return step.Trim().ToUpperInvariant() switch
            {
                "CONNECT" => detail.Contains("连接", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("准备", StringComparison.OrdinalIgnoreCase),
                "READPARAMETERS" => detail.Contains("读取", StringComparison.OrdinalIgnoreCase),
                "COMPARE" => detail.Contains("待检查", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("一致", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("不一致", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("匹配", StringComparison.OrdinalIgnoreCase),
                "APPLY" => detail.Contains("解锁", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("修改", StringComparison.OrdinalIgnoreCase)
                    || detail.Contains("设置", StringComparison.OrdinalIgnoreCase),
                _ => true
            };
        }

        /// <summary>
        /// 当前步骤没有独立报文时给出明确说明，避免日志区域看起来像没有执行。
        /// </summary>
        private static string GetSerialPortServerStepFallback(
            string step,
            SerialPortServerStationTrace trace)
        {
            return step.Trim().ToUpperInvariant() switch
            {
                "CONNECT" => trace.Message,
                "READPARAMETERS" => $"未取得独立读取明细。完整流程结论：{trace.Message}",
                "COMPARE" => $"未取得独立校验明细。完整流程结论：{trace.Message}",
                "APPLY" when trace.Success => "所有目标端口参数一致，本步骤无需修改。",
                "APPLY" => $"未完成参数修改。完整流程结论：{trace.Message}",
                _ => trace.Message
            };
        }

        /// <summary>
        /// 根据当前选中工位的资产信息切换台体类型。
        /// 具体的模式判定、0x82组帧、TCP收发和应答校验全部由独立服务处理。
        /// </summary>
        private async Task<bool> TryExecuteBenchTypeSwitchAsync(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
                accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
            List<int> stationNumbers = selectedStations
                .Select(station => station.StationNo)
                .Distinct()
                .OrderBy(stationNo => stationNo)
                .ToList();

            foreach (int stationNo in stationNumbers)
            {
                LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "台体类型切换日志",
                    $"开始执行测试小项前置台体类型切换：{context.SubItem.Name}。");
            }

            long startTicks = Environment.TickCount64;
            MeterTestBenchTypeSwitchResult result = await benchTypeSwitchService.ExecuteAsync(
                meterTestPlanConfig.BenchTypeSwitchConfig,
                stationNumbers,
                meterArchives,
                controlPcbConnectionManager,
                cancellationToken);

            foreach (int stationNo in stationNumbers)
            {
                LogTestItemStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    "台体类型切换日志",
                    $"台体类型切换结束：{result.Message}，结论={(result.Success ? "合格" : "不合格")}。");
            }

            RunOnUiThread(() => AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                $"{context.SubItem.Name}-台体类型切换",
                result.Success,
                result.Message,
                Math.Max(0, Environment.TickCount64 - startTicks)));

            return result.Success;
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
                            message);
                    }
                });

            RunOnUiThread(() =>
            {
                if (result.StandValues is not null)
                {
                    UpdateHardwareMetricsFromStandValues(result.StandValues);
                }

                if (UsesStartingSourceExecution(context.SubItem) ||
                    UsesCreepingSourceExecution(context.SubItem))
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
        /// 窗体仅负责台体切换、参数收集和结果回填，内部五步全部由独立服务完成。
        /// </summary>
        private async Task ExecuteBasicErrorPointAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            foreach (StationCommunicationConfig station in selectedStations)
            {
                UpdateStationRunningState(station.StationNo, context);
                LogBasicErrorStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    station.StationNo,
                    StationLogSeparator,
                    $"开始基本误差测试小项：{context.SubItem.Name}。");
            }

            bool benchTypeSwitchSucceeded = await TryExecuteBenchTypeSwitchAsync(
                context,
                selectedStations,
                cancellationToken);
            if (!benchTypeSwitchSucceeded)
            {
                const string message = "台体类型切换失败，基本误差测试点未执行。";
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    LogBasicErrorStationBlock(context.TestItemName, context.SubItem.Name, station.StationNo, message);
                    ApplyStationExecutionResult(station.StationNo, context, false, message);
                }

                AddProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
                return;
            }

            IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
                accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
            List<MeterTestStationCommunication> sourceStations = selectedStations
                .Select(station => new MeterTestStationCommunication
                {
                    StationNo = station.StationNo,
                    Ip = station.Ip,
                    Port = station.Port
                })
                .ToList();
            MeterTestBasicErrorExecutionResult result = await basicErrorService.ExecuteAsync(
                meterTestPlanConfig,
                context.SubItem,
                sourceStations,
                meterArchives,
                (stationNo, message) => LogBasicErrorStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    stationNo,
                    message),
                cancellationToken);

            RunOnUiThread(() =>
            {
                if (result.StandValues is not null)
                {
                    UpdateHardwareMetricsFromStandValues(result.StandValues);
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
                    Math.Max(0, Environment.TickCount64 - startTicks));
            });
        }

        /// <summary>
        /// 执行一个国网智芯蓝牙接口检测小项。
        /// 每个工位只使用BluetoothTcpChannels中的专用IP/Port新建TCP连接，各工位任务并发执行。
        /// 资产信息中的IP/Port属于485通信，此流程不会回退使用。
        /// </summary>
        private async Task ExecuteBluetoothInterfaceStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            List<MeterTestBluetoothStation> bluetoothStations = selectedStations
                .Select(CreateBluetoothStation)
                .ToList();
            foreach (MeterTestBluetoothStation station in bluetoothStations)
            {
                UpdateStationRunningState(station.StationNo, context);
                string endpoint = string.IsNullOrWhiteSpace(station.ConfigurationError)
                    ? $"{station.Ip}:{station.Port}"
                    : "未配置";
                LogBluetoothStationBlock(
                    context.TestItemName,
                    context.SubItem.Name,
                    station.StationNo,
                    StationLogSeparator,
                    $"开始蓝牙检测步骤：{context.SubItem.Name}，工位={station.StationNo}，蓝牙Endpoint={endpoint}。");
            }

            IReadOnlyDictionary<int, MeterTestBluetoothStationResult> results =
                await bluetoothInterfaceService.ExecuteStepAsync(
                    context.SubItem,
                    bluetoothStations,
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

        /// <summary>
        /// 按工位号解析蓝牙专用TCP配置。
        /// 缺失、重复、禁用或端点非法时把原因交给蓝牙服务判定失败，绝不使用资产485端点兜底。
        /// </summary>
        private MeterTestBluetoothStation CreateBluetoothStation(StationCommunicationConfig station)
        {
            List<MeterTestBluetoothTcpChannel> matches = meterTestPlanConfig.BluetoothTcpChannels
                .Where(channel => channel.Station == station.StationNo)
                .ToList();
            if (matches.Count == 0)
            {
                return new MeterTestBluetoothStation(
                    station.StationNo,
                    string.Empty,
                    0,
                    station.MeterAddress,
                    $"工位{station.StationNo}未配置蓝牙专用TCP通道，请维护BluetoothTcpChannels。");
            }

            if (matches.Count > 1)
            {
                return new MeterTestBluetoothStation(
                    station.StationNo,
                    string.Empty,
                    0,
                    station.MeterAddress,
                    $"工位{station.StationNo}存在{matches.Count}条蓝牙TCP配置，请保留唯一映射。");
            }

            MeterTestBluetoothTcpChannel channel = matches[0];
            if (!channel.Enabled)
            {
                return new MeterTestBluetoothStation(
                    station.StationNo,
                    channel.Ip.Trim(),
                    channel.Port,
                    station.MeterAddress,
                    $"工位{station.StationNo}的蓝牙专用TCP通道未启用。");
            }

            string ip = channel.Ip.Trim();
            string configurationError = string.IsNullOrWhiteSpace(ip) || channel.Port is < 1 or > 65535
                ? $"工位{station.StationNo}的蓝牙专用TCP端点无效：{ip}:{channel.Port}。"
                : string.Empty;
            return new MeterTestBluetoothStation(
                station.StationNo,
                ip,
                channel.Port,
                station.MeterAddress,
                configurationError);
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
        /// 初始化工位表格。
        /// 默认补齐 1-48 工位，并预置通信参数和档案参数。
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
                        false,
                        station.StationNo,
                        string.IsNullOrWhiteSpace(station.Ip) ? DefaultStationIp : station.Ip,
                        station.Port <= 0 ? DefaultStationStartPort + station.StationNo - 1 : station.Port,
                        string.Empty,
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
                        "9600-8-E-1",
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
            SetSchemeAreaVisibility(true);
            ApplyStationAssetVisibility(showAllStations: false);

            rbMultiStation.Visible = true;
            rbSingleStation.Visible = true;
            btnSelectAllStations.Visible = true;
            btnClearStationSelection.Visible = true;
            btnSaveAssetInfo.Visible = false;
            btnBatchApplyAssetInfo.Visible = false;
            lblBarcodeStartIndex.Visible = false;
            tbxBarcodeStartIndex.Visible = false;
            lblBarcodeEndIndex.Visible = false;
            tbxBarcodeEndIndex.Visible = false;
            processGrid.Visible = true;
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
            SetSchemeAreaVisibility(false);
            ApplyStationAssetVisibility(showAllStations: true);

            rbMultiStation.Visible = false;
            rbSingleStation.Visible = false;
            btnSelectAllStations.Visible = false;
            btnClearStationSelection.Visible = false;
            btnSaveAssetInfo.Visible = true;
            btnBatchApplyAssetInfo.Visible = true;
            lblBarcodeStartIndex.Visible = true;
            tbxBarcodeStartIndex.Visible = true;
            lblBarcodeEndIndex.Visible = true;
            tbxBarcodeEndIndex.Visible = true;
            processGrid.Visible = false;
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
                colMeterActiveClass,
                colMeterActiveConstant,
                colMeterReactiveClass,
                colMeterReactiveConstant);
            ApplyAssetInfoColumnWidths();
            SetStationColumnEditState(assetEditable: true);
            UpdateMeterAddressColumnHeader(true);
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
        /// 判断工位是否具备测试资格：必须已经扫码，并按当前截取规则得到电表地址。
        /// </summary>
        private bool HasCompleteAssetForTest(DataGridViewRow row)
        {
            if (row.IsNewRow)
                return false;

            string barcode = GetCellText(row, colStationBarcode, string.Empty).Trim();
            string meterAddress = GetCellText(row, colStationMeterAddress, string.Empty).Trim();
            return !string.IsNullOrWhiteSpace(barcode) && !string.IsNullOrWhiteSpace(meterAddress);
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
            SaveBarcodeSettingFromInputs();

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
        /// 用 1 工位的参数批量覆盖 2-48 工位。
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
                    CopyCellValue(sourceRow, row, colMeterBaudRate);
                }
            }
            finally
            {
                isLoadingStationConfig = false;
                isLoadingMeterArchive = false;
            }

            SaveAllAssetInfo(showMessage: false);
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
            SetComboCellValue(row, colMeterType, archive.MeterType, "单相");
            SetComboCellValue(row, colMeterAccessMode, archive.AccessMode, "直接式");
            row.Cells[colMeterVoltage.Index].Value = DefaultIfEmpty(archive.Voltage, "220V");
            row.Cells[colMeterCurrent.Index].Value = DefaultIfEmpty(archive.Current, "5A");
            SetComboCellValue(row, colMeterActiveClass, archive.ActiveClass, "A");
            row.Cells[colMeterActiveConstant.Index].Value = DefaultIfEmpty(archive.ActiveConstant, "1000");
            SetComboCellValue(row, colMeterReactiveClass, archive.ReactiveClass, "2.0");
            row.Cells[colMeterReactiveConstant.Index].Value = DefaultIfEmpty(archive.ReactiveConstant, "1000");
            row.Cells[colStationBarcode.Index].Value = archive.Barcode;
            row.Cells[colStationMeterAddress.Index].Value =
                DefaultIfEmpty(
                    archive.MeterAddress,
                    TryExtractMeterAddressFromBarcode(archive.Barcode, out string extractedAddress) ? extractedAddress : string.Empty);
            SetComboCellValue(row, colMeterBaudRate, archive.BaudRate, "9600-8-E-1");
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
                GetCellText(row, colStationBarcode, string.Empty),
                GetCellText(row, colStationMeterAddress, string.Empty),
                GetCellText(row, colMeterBaudRate, "9600-8-E-1")));
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
                   columnIndex == colMeterReactiveConstant.Index ||
                   columnIndex == colMeterBaudRate.Index ||
                   columnIndex == colStationBarcode.Index ||
                   columnIndex == colStationMeterAddress.Index;
        }

        /// <summary>
        /// 创建默认电表档案。
        /// </summary>
        private static MeterArchiveData CreateDefaultMeterArchive(int stationNo)
        {
            return new MeterArchiveData(stationNo, "单相", "直接式", "220V", "5A", "A", "1000", "2.0", "1000", string.Empty, string.Empty, "9600-8-E-1");
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
                tbxBarcodeStartIndex.Text = assetBarcodeStartIndex.ToString();
                tbxBarcodeEndIndex.Text = assetBarcodeEndIndex.ToString();
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

            if (!TryReadBarcodeRangeFromInputs(out int startIndex, out int endIndex))
                return;

            assetBarcodeStartIndex = startIndex;
            assetBarcodeEndIndex = endIndex;
            accessDatabaseService.SaveAssetBarcodeSetting(startIndex, endIndex);

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
        /// 读取条码截取规则输入。
        /// </summary>
        private bool TryReadBarcodeRangeFromInputs(out int startIndex, out int endIndex)
        {
            startIndex = assetBarcodeStartIndex;
            endIndex = assetBarcodeEndIndex;

            if (!int.TryParse(tbxBarcodeStartIndex.Text.Trim(), out startIndex))
                return false;

            if (!int.TryParse(tbxBarcodeEndIndex.Text.Trim(), out endIndex))
                return false;

            return startIndex >= 0 && endIndex >= startIndex;
        }

        /// <summary>
        /// 根据条形码和当前截取区间提取电表地址。
        /// </summary>
        private bool TryExtractMeterAddressFromBarcode(string barcode, out string meterAddress)
        {
            meterAddress = string.Empty;
            barcode = barcode.Trim();
            if (string.IsNullOrWhiteSpace(barcode))
                return false;

            if (assetBarcodeStartIndex < 0 || assetBarcodeEndIndex < assetBarcodeStartIndex)
                return false;

            if (barcode.Length <= assetBarcodeEndIndex)
                return false;

            int length = assetBarcodeEndIndex - assetBarcodeStartIndex + 1;
            if (length <= 0 || assetBarcodeStartIndex + length > barcode.Length)
                return false;

            meterAddress = barcode.Substring(assetBarcodeStartIndex, length);
            return !string.IsNullOrWhiteSpace(meterAddress);
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
                !Convert.ToBoolean(row.Cells[colStationSelected.Index].Value ?? false))
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
                    string actualAddress = NormalizeMeterAddressForComparison(station.MeterAddress);
                    if (string.IsNullOrWhiteSpace(response))
                    {
                        passed = false;
                        message = "电表无响应";
                        LogStationCommunicationBlock(
                            context.TestItemName,
                            station,
                            message,
                            $"实际地址：{actualAddress}；返回地址：空；结论：不合格",
                            StationLogSeparator);
                    }
                    else
                    {
                        Sgcc698BroadcastAddressParseResult parseResult = ParseSgcc698BroadcastAddressResponse(context.SubItem, response);
                        string returnedAddress = parseResult.IsValid
                            ? NormalizeMeterAddressForComparison(parseResult.MeterAddress)
                            : string.Empty;
                        passed = parseResult.IsValid &&
                                 actualAddress.Equals(returnedAddress, StringComparison.OrdinalIgnoreCase);
                        message = !parseResult.IsValid
                            ? $"电表响应异常：{parseResult.Message}"
                            : passed
                                ? "电表响应正常"
                                : "电表响应地址不一致";
                        LogStationCommunicationBlock(
                            context.TestItemName,
                            station,
                            message,
                            $"实际地址：{actualAddress}；返回地址：{(string.IsNullOrWhiteSpace(returnedAddress) ? "解析失败" : returnedAddress)}；结论：{(passed ? "合格" : "不合格")}",
                            StationLogSeparator);
                    }
                }
                else
                {
                    passed = IsResponseMatched(context.SubItem, response);
                    message = passed ? "应答匹配，测试通过。" : $"应答不匹配，期望：{context.SubItem.ExpectedResponse}，实际：{response}";
                }
            }
            catch (OperationCanceledException)
            {
                passed = false;
                message = cancellationToken.IsCancellationRequested
                    ? "测试被取消，当前工位未收到完整应答。"
                    : UsesSgcc698BroadcastAddressParser(context.SubItem)
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
        /// 执行方案树中的一个日计时小项。
        /// 第一次进入日计时时执行完整三轮流程，后续八个节点只展示阶段状态，不重复发送整套报文。
        /// </summary>
        private async Task ExecuteControlPcbDailyTimingStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            if (!dailyTimingFlowExecuted)
            {
                dailyTimingFlowExecuted = true;
                dailyTimingFlowSucceeded = await ExecuteControlPcbDailyTimingFlowAsync(
                    context,
                    selectedStations,
                    cancellationToken);

                // 完整日计时流程只在第一次进入时发送一次；将最终三轮平均结果同步到当前“开始”节点，
                // 这样用户点击任意日计时步骤时，都能看到已经完成的工位结论。
                SynchronizeDailyTimingStepResults(
                    context,
                    selectedStations,
                    dailyTimingFlowSucceeded
                        ? "三轮日计时流程完成。"
                        : "三轮日计时流程存在失败工位或结果不足。");

                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    dailyTimingFlowSucceeded,
                    dailyTimingFlowSucceeded
                        ? "三轮日计时流程完成，平均误差已计算。"
                        : "三轮日计时流程存在失败工位或结果不足，已完成流程。",
                    Math.Max(0, Environment.TickCount64 - startTicks));
                return;
            }

            SynchronizeDailyTimingStepResults(
                context,
                selectedStations,
                dailyTimingFlowSucceeded
                    ? "该日计时步骤已由前置完整流程完成。"
                    : "前置三轮日计时流程存在失败，当前步骤不阻断后续测试。");

            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                dailyTimingFlowSucceeded,
                dailyTimingFlowSucceeded
                    ? $"该步骤已在第一个日计时小项中完成，本节点仅展示流程阶段：{context.SubItem.DailyTimingStep}。"
                    : "前置三轮日计时流程存在失败，当前节点不阻断方案后续执行。",
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        /// <summary>
        /// 执行完整三轮日计时流程，并汇总所有控制 PCB 组的最终结论。
        /// </summary>
        private async Task<bool> ExecuteControlPcbDailyTimingFlowAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            if (!TryGetDailyTimingConfig(context.SubItem, out byte testTime, out byte testCount, out int packetIntervalMs))
            {
                AddProcessLog(context.SchemeName, context.SubItem.Name, false, "日计时配置不正确。", 0);
                return false;
            }

            List<Task<bool>> groupTasks = GetEnabledControlPcbGroups(context.SubItem)
                .Select(group => ExecuteControlPcbDailyTimingGroupAsync(
                    group,
                    selectedStations,
                    context,
                    testTime,
                    testCount,
                    packetIntervalMs,
                    cancellationToken))
                .ToList();

            if (groupTasks.Count == 0)
            {
                AddProcessLog(context.SchemeName, context.SubItem.Name, false, "未找到可用控制PCB分组，请检查 ControlPcbGroups。", 0);
                return false;
            }

            bool[] groupResults = await Task.WhenAll(groupTasks);
            return groupResults.Length > 0 && groupResults.All(result => result);
        }

        /// <summary>
        /// 执行潜动走字试验启动节点。各控制PCB分组并发连接，组内按100ms间隔逐表位下发0x35启动报文。
        /// 只有完整回显操作码、脉冲数和手动配置时间的工位，才会记录为后续等待/读取的有效工位。
        /// </summary>
        private async Task ExecuteControlPcbCreepingStartAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            if (!TryGetCreepingTestConfig(
                    context.SubItem,
                    out byte pulseCount,
                    out uint timeSeconds,
                    out int packetIntervalMs))
            {
                const string message = "潜动走字配置无效：脉冲数必须为1-255，时间必须大于0秒。";
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    ApplyStationExecutionResult(station.StationNo, context, false, message);
                }

                AddProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
                return;
            }

            foreach (StationCommunicationConfig station in selectedStations)
            {
                creepingActiveStations.TryRemove(station.StationNo, out _);
            }

            List<Task<bool>> groupTasks = GetEnabledControlPcbGroups(context.SubItem)
                .Select(group => ExecuteControlPcbCreepingStartGroupAsync(
                    group,
                    selectedStations,
                    context,
                    pulseCount,
                    timeSeconds,
                    packetIntervalMs,
                    cancellationToken))
                .ToList();

            if (groupTasks.Count == 0)
            {
                const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    ApplyStationExecutionResult(station.StationNo, context, false, message);
                }

                AddProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
                return;
            }

            bool[] groupResults = await Task.WhenAll(groupTasks);
            bool passed = groupResults.All(result => result);
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                passed,
                passed
                    ? $"全部选中工位已收到0x35启动应答，手动等待时间={timeSeconds}s。"
                    : "潜动走字启动完成，但存在未连接或未收到正确应答的工位；成功工位继续后续流程。",
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        /// <summary>执行一个控制PCB分组的潜动走字启动命令，不因单个工位失败而中断同组其它工位。</summary>
        private async Task<bool> ExecuteControlPcbCreepingStartGroupAsync(
            MeterTestControlPcbGroup group,
            List<StationCommunicationConfig> selectedStations,
            SelectedSubItemContext context,
            byte pulseCount,
            uint timeSeconds,
            int packetIntervalMs,
            CancellationToken cancellationToken)
        {
            List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
            if (targets.Count == 0)
                return true;

            if (!IsControlPcbV2(group.ProtocolVersion))
            {
                string message = $"控制PCB组 {group.Name} 使用 {group.ProtocolVersion}，0x35潜动走字只支持V2协议。";
                LogControlPcbGroupBlock(context.TestItemName, group, targets, message, StationLogSeparator);
                RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, false, message, string.Empty));
                return false;
            }

            foreach (ControlPcbStationTarget target in targets)
            {
                RunOnUiThread(() => UpdateStationRunningState(target.StationNo, context));
            }

            if (!controlPcbConnectionManager.TryGetConnectedConnection(
                    group,
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                LogControlPcbGroupBlock(context.TestItemName, group, targets, connectionError, StationLogSeparator);
                RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, false, connectionError, string.Empty));
                return false;
            }

            LogControlPcbGroupBlock(context.TestItemName, group, targets, $" 复用控制PCB长连接：{connection.DisplayName}", StationLogSeparator);
            Dictionary<byte, byte[]> responses = await SendControlPcbPacketsAndCollectResponsesAsync(
                context.TestItemName,
                connection,
                group,
                targets,
                target => ElectricEnergyMeterControlV2.BuildCreepingTestStartPacket(
                    target.MeterAddress,
                    pulseCount,
                    timeSeconds),
                target => $"0x35开启潜动走字试验[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 脉冲数={pulseCount}, 时间={timeSeconds}s]",
                frame => ResolveCreepingTestStartResponse(frame, pulseCount, timeSeconds),
                TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
                TimeSpan.FromMilliseconds(packetIntervalMs),
                cancellationToken);

            bool groupPassed = true;
            foreach (ControlPcbStationTarget target in targets)
            {
                bool stationPassed = responses.ContainsKey(target.MeterAddress);
                groupPassed &= stationPassed;
                if (stationPassed)
                {
                    creepingActiveStations[target.StationNo] = target.MeterAddress;
                }
                else
                {
                    creepingActiveStations.TryRemove(target.StationNo, out _);
                }

                string message = stationPassed
                    ? $"0x35启动应答正常，脉冲数={pulseCount}，手动等待时间={timeSeconds}s。"
                    : "0x35启动未收到正确应答，当前工位不进入后续潜动等待。";
                LogControlPcbStationBlock(
                    context.TestItemName,
                    group,
                    target,
                    $"结论：{(stationPassed ? "合格" : "不合格")}，{message}",
                    StationLogSeparator);
                RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, stationPassed, message));
            }

            return groupPassed;
        }

        /// <summary>执行XML手动配置的潜动等待时间，只输出开始和结束两条倒计时日志。</summary>
        private async Task ExecuteCreepingWaitAsync(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            int waitSeconds = context.SubItem.CreepingTimeSeconds;
            if (waitSeconds <= 0)
            {
                const string message = "潜动等待时间必须在XML中配置为大于0的整数秒。";
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    ApplyStationExecutionResult(station.StationNo, context, false, message);
                }

                AddProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
                return;
            }

            List<StationCommunicationConfig> activeStations = selectedStations
                .Where(station => creepingActiveStations.ContainsKey(station.StationNo))
                .ToList();
            if (activeStations.Count == 0)
            {
                const string message = "没有工位收到潜动启动应答，跳过潜动等待。";
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    ApplyStationExecutionResult(station.StationNo, context, false, message);
                }

                AddProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
                return;
            }

            await DelayTestWithCountdownAsync(
                waitSeconds,
                $"开始潜动倒计时：{waitSeconds}s",
                "潜动倒计时结束",
                message =>
                {
                    foreach (StationCommunicationConfig station in activeStations)
                    {
                        LogCreepingStationBlock(context.TestItemName, station.StationNo, message);
                    }
                },
                cancellationToken);

            foreach (StationCommunicationConfig station in selectedStations)
            {
                bool passed = creepingActiveStations.ContainsKey(station.StationNo);
                string message = passed
                    ? $"已完成手动配置的{waitSeconds}s潜动等待。"
                    : "潜动启动未成功，未进入等待。";
                ApplyStationExecutionResult(station.StationNo, context, passed, message);
            }

            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                activeStations.Count == selectedStations.Count,
                $"潜动等待结束，有效工位={string.Join(",", activeStations.Select(station => station.StationNo))}，等待={waitSeconds}s。",
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        /// <summary>
        /// 执行潜动脉冲读取节点。仅向已收到0x35启动应答的工位发送0x35+AA，
        /// 单个工位无应答或解析失败不会阻止同组及其他控制PCB组继续读取。
        /// </summary>
        private async Task ExecuteControlPcbCreepingReadAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            foreach (StationCommunicationConfig station in selectedStations)
            {
                creepingPulseResults.TryRemove(station.StationNo, out _);
            }

            List<MeterTestControlPcbGroup> groups = GetEnabledControlPcbGroups(context.SubItem);
            if (groups.Count == 0)
            {
                const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
                foreach (StationCommunicationConfig station in selectedStations)
                {
                    ApplyStationExecutionResult(station.StationNo, context, false, message);
                }

                AddProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
                return;
            }

            HashSet<int> mappedStationNumbers = groups
                .SelectMany(group => GetControlPcbStationTargets(group, selectedStations))
                .Select(target => target.StationNo)
                .ToHashSet();
            foreach (StationCommunicationConfig station in selectedStations.Where(
                         station => !mappedStationNumbers.Contains(station.StationNo)))
            {
                const string message = "当前工位未映射到可用控制PCB分组，未发送0x35+AA读取报文。";
                LogCreepingStationBlock(context.TestItemName, station.StationNo, message);
                ApplyStationExecutionResult(station.StationNo, context, false, message);
            }

            int packetIntervalMs = Math.Max(0, context.SubItem.PacketIntervalMs);
            List<Task<bool>> groupTasks = groups
                .Select(group => ExecuteControlPcbCreepingReadGroupAsync(
                    group,
                    selectedStations,
                    context,
                    packetIntervalMs,
                    cancellationToken))
                .ToList();
            bool[] groupResults = await Task.WhenAll(groupTasks);
            bool passed = mappedStationNumbers.Count == selectedStations.Count &&
                groupResults.Length > 0 &&
                groupResults.All(result => result);
            int resultCount = selectedStations.Count(station => creepingPulseResults.ContainsKey(station.StationNo));

            RestoreStationDisplayForSelectedNode();
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                passed,
                $"潜动脉冲读取完成，成功读取={resultCount}/{selectedStations.Count}个工位；失败工位不影响其他工位读取。",
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        /// <summary>复用指定控制PCB长连接，逐工位发送0x35+AA并解析累计脉冲数和累计时间。</summary>
        private async Task<bool> ExecuteControlPcbCreepingReadGroupAsync(
            MeterTestControlPcbGroup group,
            List<StationCommunicationConfig> selectedStations,
            SelectedSubItemContext context,
            int packetIntervalMs,
            CancellationToken cancellationToken)
        {
            List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
            if (targets.Count == 0)
                return true;

            if (!IsControlPcbV2(group.ProtocolVersion))
            {
                string message = $"控制PCB组 {group.Name} 使用 {group.ProtocolVersion}，0x35潜动读取只支持V2协议。";
                LogControlPcbGroupBlock(context.TestItemName, group, targets, message, StationLogSeparator);
                RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, false, message, string.Empty));
                return false;
            }

            List<ControlPcbStationTarget> activeTargets = targets
                .Where(target => creepingActiveStations.ContainsKey(target.StationNo))
                .ToList();
            foreach (ControlPcbStationTarget target in targets.Where(
                         target => !creepingActiveStations.ContainsKey(target.StationNo)))
            {
                const string message = "潜动启动未成功，未发送0x35+AA结果读取报文。";
                LogControlPcbStationBlock(context.TestItemName, group, target, message, StationLogSeparator);
                RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, false, message));
            }

            if (activeTargets.Count == 0)
                return false;

            foreach (ControlPcbStationTarget target in activeTargets)
            {
                RunOnUiThread(() => UpdateStationRunningState(target.StationNo, context));
            }

            if (!controlPcbConnectionManager.TryGetConnectedConnection(
                    group,
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                LogControlPcbGroupBlock(context.TestItemName, group, activeTargets, connectionError, StationLogSeparator);
                RunOnUiThread(() => ApplyControlPcbGroupResult(activeTargets, context, false, connectionError, string.Empty));
                return false;
            }

            LogControlPcbGroupBlock(
                context.TestItemName,
                group,
                activeTargets,
                $" 复用控制PCB长连接：{connection.DisplayName}",
                StationLogSeparator);
            Dictionary<byte, byte[]> responses = await SendControlPcbPacketsAndCollectResponsesAsync(
                context.TestItemName,
                connection,
                group,
                activeTargets,
                target => ElectricEnergyMeterControlV2.BuildCreepingTestResultPacket(target.MeterAddress),
                target => $"0x35读取潜动脉冲[工位={target.StationNo}, 表位={target.MeterAddress:X2}]",
                ResolveCreepingTestResultResponse,
                TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
                TimeSpan.FromMilliseconds(packetIntervalMs),
                cancellationToken);

            bool groupPassed = activeTargets.Count == targets.Count;
            foreach (ControlPcbStationTarget target in activeTargets)
            {
                bool hasResponse = responses.TryGetValue(target.MeterAddress, out byte[]? response);
                byte pulseCount = 0;
                uint timeSeconds = 0;
                bool parsed = hasResponse && ElectricEnergyMeterControlV2.TryParseCreepingTestResponse(
                    response!,
                    target.MeterAddress,
                    ElectricEnergyMeterControlV2.CreepingTestResultOperation,
                    out pulseCount,
                    out timeSeconds);
                if (parsed)
                {
                    creepingPulseResults[target.StationNo] = new CreepingPulseMeasurement(pulseCount, timeSeconds);
                    string message = $"潜动结果读取成功，当前脉冲个数：{pulseCount}，累计时间：{timeSeconds}s。";
                    LogControlPcbStationBlock(context.TestItemName, group, target, message, StationLogSeparator);
                    RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, true, message));
                }
                else
                {
                    groupPassed = false;
                    string message = hasResponse
                        ? "收到0x35结果应答，但脉冲数或累计时间解析失败。"
                        : "未收到0x35+AA潜动结果应答。";
                    LogControlPcbStationBlock(context.TestItemName, group, target, message, StationLogSeparator);
                    RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, false, message));
                }
            }

            return groupPassed;
        }

        /// <summary>按累计脉冲数小于等于1判定潜动结果；0个或1个均为合格。</summary>
        private void ExecuteCreepingPulseJudgeStep(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations)
        {
            long startTicks = Environment.TickCount64;
            bool allPassed = true;
            foreach (StationCommunicationConfig station in selectedStations)
            {
                bool hasResult = creepingPulseResults.TryGetValue(
                    station.StationNo,
                    out CreepingPulseMeasurement? measurement);
                bool passed = hasResult && measurement!.PulseCount <= 1;
                allPassed &= passed;
                string pulseText = hasResult ? measurement!.PulseCount.ToString(CultureInfo.InvariantCulture) : "未读取";
                string message = $"当前脉冲个数：{pulseText}，标准脉冲个数≦1个，结论：{(passed ? "合格" : "不合格")}";
                LogCreepingStationBlock(context.TestItemName, station.StationNo, message);
                ApplyStationExecutionResult(station.StationNo, context, passed, message);
            }

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
        /// 执行单个控制 PCB 组的三轮日计时：开始、倒计时、读取结果。
        /// 每轮结果按工位保存，最后按工位计算三轮平均误差。
        /// </summary>
        private async Task<bool> ExecuteControlPcbDailyTimingGroupAsync(
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
                return true;

            long startTicks = Environment.TickCount64;
            Dictionary<int, List<float>> stationRoundAverages = targets.ToDictionary(
                target => target.StationNo,
                _ => new List<float>());

            foreach (ControlPcbStationTarget target in targets)
            {
                RunOnUiThread(() => UpdateStationRunningState(target.StationNo, context));
            }

            if (!controlPcbConnectionManager.TryGetConnectedConnection(
                    group,
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                LogControlPcbGroupBlock(context.TestItemName, group, targets, connectionError, StationLogSeparator);
                RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, false, connectionError, string.Empty));
                RunOnUiThread(() => AddProcessLog($"{context.SchemeName}/{context.TestItemName}/{group.Name}", context.SubItem.Name, false, connectionError, 0));
                return false;
            }

            LogControlPcbGroupBlock(context.TestItemName, group, targets, $" 复用控制PCB长连接：{connection.DisplayName}", StationLogSeparator);
            // 日计时等待按试验总时长增加10%余量并向上取整：60s × 1次 = 66s。
            int waitSeconds = (int)Math.Ceiling(testTime * testCount * 1.1m);

            for (int round = 1; round <= 3; round++)
            {
                LogControlPcbGroupBlock(
                    context.TestItemName,
                    group,
                    targets,
                    $"第{round}轮：开始日计时实验");

                Dictionary<byte, byte[]> startResponses = await SendControlPcbPacketsAndCollectResponsesAsync(
                    context.TestItemName,
                    connection,
                    group,
                    targets,
                    target => BuildDailyTimingPacket(group.ProtocolVersion, target.MeterAddress, DailyTimingStartDataItem, testTime, testCount),
                    target => $"第{round}轮日计时开始[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 时间={testTime}s, 次数={testCount}]",
                    rawData => TryGetDailyTimingResponse(rawData, group.ProtocolVersion, DailyTimingStartDataItem, testTime, testCount, out byte meterAddress) ? meterAddress : null,
                    TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
                    TimeSpan.FromMilliseconds(packetIntervalMs),
                    cancellationToken);

                List<ControlPcbStationTarget> activeTargets = targets
                    .Where(target => startResponses.ContainsKey(target.MeterAddress))
                    .ToList();

                foreach (ControlPcbStationTarget target in targets.Except(activeTargets))
                {
                    LogControlPcbStationBlock(
                        context.TestItemName,
                        group,
                        target,
                        $"第{round}轮表位 {target.MeterAddress:X2} 开始日计时未收到正确应答");
                }

                if (activeTargets.Count == 0)
                {
                    LogControlPcbGroupBlock(
                        context.TestItemName,
                        group,
                        targets,
                        $"第{round}轮没有表位收到开始应答，继续下一轮");
                    continue;
                }

                LogControlPcbGroupBlock(
                    context.TestItemName,
                    group,
                    activeTargets,
                    $"第{round}轮开始应答正常");
                await DelayTestWithCountdownAsync(
                    waitSeconds,
                    $"第{round}轮开始倒计时：{waitSeconds}s",
                    $"第{round}轮倒计时结束",
                    message => LogControlPcbGroupBlock(context.TestItemName, group, activeTargets, message),
                    cancellationToken);

                Dictionary<byte, byte[]> resultResponses = await SendControlPcbPacketsAndCollectResponsesAsync(
                    context.TestItemName,
                    connection,
                    group,
                    activeTargets,
                    target => BuildDailyTimingPacket(group.ProtocolVersion, target.MeterAddress, DailyTimingResultDataItem, testTime, testCount),
                    target => $"第{round}轮读取日计时结果[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 时间={testTime}s, 次数={testCount}]",
                    rawData => TryGetDailyTimingResponse(rawData, group.ProtocolVersion, DailyTimingResultDataItem, testTime, testCount, out byte meterAddress) ? meterAddress : null,
                    TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
                    TimeSpan.FromMilliseconds(packetIntervalMs),
                    cancellationToken);

                foreach (ControlPcbStationTarget target in activeTargets)
                {
                    IReadOnlyList<float> values = Array.Empty<float>();
                    string parseMessage = string.Empty;
                    bool hasResponse = resultResponses.TryGetValue(target.MeterAddress, out byte[]? rawResponse);
                    bool parsed = hasResponse && TryParseDailyTimingResults(
                        rawResponse!,
                        group.ProtocolVersion,
                        testTime,
                        testCount,
                        out values,
                        out parseMessage);
                    if (!parsed)
                    {
                        LogControlPcbStationBlock(
                            context.TestItemName,
                            group,
                            target,
                            $"第{round}轮日计时结果解析失败：{parseMessage}");
                        continue;
                    }

                    float roundAverage = values.Average();
                    stationRoundAverages[target.StationNo].Add(roundAverage);
                    string valuesText = string.Join(", ", values.Select(value => value.ToString("0.####", CultureInfo.InvariantCulture)));
                    LogControlPcbStationBlock(
                        context.TestItemName,
                        group,
                        target,
                        $"第{round}轮日计时结果获取正常",
                        $"误差值：{valuesText}",
                        $"本轮平均误差：{roundAverage.ToString("0.####", CultureInfo.InvariantCulture)}");
                }
            }

            bool groupPassed = true;
            SelectedSubItemContext resultContext = GetFinalDailyTimingResultContext(context);
            foreach (ControlPcbStationTarget target in targets)
            {
                List<float> roundAverages = stationRoundAverages[target.StationNo];
                bool hasThreeResults = roundAverages.Count == 3;
                double finalAverage = hasThreeResults ? roundAverages.Average() : double.NaN;
                bool passed = hasThreeResults && Math.Abs(finalAverage) < 0.5;
                groupPassed &= passed;

                string resultText = hasThreeResults
                    ? $"三轮平均误差={finalAverage.ToString("0.####", CultureInfo.InvariantCulture)}，判定={(passed ? "合格" : "不合格")}（阈值：绝对值<0.5）"
                    : $"仅获取到 {roundAverages.Count}/3 轮有效结果，判定不合格";
                LogControlPcbStationBlock(context.TestItemName, group, target, resultText, StationLogSeparator);
                RunOnUiThread(() => ApplyStationExecutionResult(
                    target.StationNo,
                    resultContext,
                    passed,
                    resultText));
            }

            RunOnUiThread(() =>
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}/{group.Name}",
                    resultContext.SubItem.Name,
                    groupPassed,
                    groupPassed
                        ? "控制PCB三轮日计时全部完成，所有工位平均误差合格。"
                        : "控制PCB三轮日计时完成，但存在结果不足或平均误差不合格工位。",
                    Math.Max(0, Environment.TickCount64 - startTicks)));
            return groupPassed;
        }

        /// <summary>
        /// 执行起动误差试验启动节点：读取标准表脉冲常数，然后通过控制PCB下发A2、A0和0x38。
        /// 标准表常数只读取一次；不同控制PCB组并行执行，同组内失败工位不会阻断其他工位。
        /// </summary>
        private async Task ExecuteControlPcbStartingErrorStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            if (!TryGetStartingErrorConfig(
                    context.SubItem,
                    out byte pulseCount,
                    out byte testCount,
                    out byte pulseType,
                    out int packetIntervalMs,
                    out string? configError))
            {
                RunOnUiThread(() =>
                {
                    SaveStationConclusions(
                        context,
                        selectedStations,
                        selectedStations.ToDictionary(station => station.StationNo, _ => false),
                        configError ?? "起动误差试验配置错误。");
                    AddProcessLog(context.SchemeName, context.SubItem.Name, false, configError ?? "起动误差试验配置错误。", 0);
                });
                return;
            }

            (bool constantRead, ulong standardConstant, string constantMessage) =
                await ReadStandardActiveConstantAsync(cancellationToken);
            LogMessage.Debug($"[起动试验] {constantMessage}");
            if (!constantRead)
            {
                RunOnUiThread(() =>
                {
                    SaveStationConclusions(
                        context,
                        selectedStations,
                        selectedStations.ToDictionary(station => station.StationNo, _ => false),
                        constantMessage);
                    RestoreStationDisplayForSelectedNode();
                    AddProcessLog(
                        context.SchemeName,
                        context.SubItem.Name,
                        false,
                        constantMessage,
                        Math.Max(0, Environment.TickCount64 - startTicks));
                });
                return;
            }

            IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
                accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
            List<Task<bool>> groupTasks = GetEnabledControlPcbGroups(context.SubItem)
                .Select(group => ExecuteControlPcbStartingErrorGroupAsync(
                    group,
                    selectedStations,
                    meterArchives,
                    context,
                    standardConstant,
                    pulseCount,
                    testCount,
                    pulseType,
                    packetIntervalMs,
                    cancellationToken))
                .ToList();

            if (groupTasks.Count == 0)
            {
                const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
                RunOnUiThread(() =>
                {
                    SaveStationConclusions(
                        context,
                        selectedStations,
                        selectedStations.ToDictionary(station => station.StationNo, _ => false),
                        message);
                    AddProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0);
                });
                return;
            }

            bool[] groupResults = await Task.WhenAll(groupTasks);
            bool passed = groupResults.Length > 0 && groupResults.All(result => result);
            RunOnUiThread(() =>
            {
                RestoreStationDisplayForSelectedNode();
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    passed,
                    passed
                        ? $"A2、A0和0x38启动命令全部完成，标准表常数={standardConstant}。"
                        : $"起动误差启动流程存在失败工位，标准表常数={standardConstant}，请查看工位日志。",
                    Math.Max(0, Environment.TickCount64 - startTicks));
            });
        }

        /// <summary>通过XYCtr读取标准表有功脉冲常数并解析为无符号整数。</summary>
        private static async Task<(bool Success, ulong Constant, string Message)> ReadStandardActiveConstantAsync(
            CancellationToken cancellationToken)
        {
            if (!XYCtr.IsSourcePortOpen)
            {
                return (false, 0, "源串口尚未打开，无法读取标准表脉冲常数；请先执行升源（启动电流）。");
            }

            using XYCtr xyCtr = new();
            byte[] constantBuffer = new byte[1024];
            cancellationToken.ThrowIfCancellationRequested();
            (bool success, int result) = await xyCtr
                .CallReadStandConstAsync(constantBuffer, TimeSpan.FromSeconds(5))
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!success)
            {
                return (false, 0, $"读取标准表脉冲常数失败，XYCtr返回值={result}。");
            }

            string rawValue = Encoding.Default.GetString(constantBuffer).TrimEnd('\0', '\r', '\n', ' ');
            if (!TryParseUnsignedConstant(rawValue, out ulong standardConstant) || standardConstant == 0)
            {
                return (false, 0, $"标准表脉冲常数解析失败，原始返回={rawValue}。");
            }

            return (true, standardConstant, $"读取标准表脉冲常数成功：{standardConstant}，原始返回={rawValue}。");
        }

        /// <summary>
        /// 执行单个控制PCB组的A2、A0和0x38启动流程。
        /// A2或A0未正确应答的工位会从后续步骤移除，其他工位继续执行。
        /// </summary>
        private async Task<bool> ExecuteControlPcbStartingErrorGroupAsync(
            MeterTestControlPcbGroup group,
            List<StationCommunicationConfig> selectedStations,
            IReadOnlyDictionary<int, MeterArchiveData> meterArchives,
            SelectedSubItemContext context,
            ulong standardConstant,
            byte pulseCount,
            byte testCount,
            byte pulseType,
            int packetIntervalMs,
            CancellationToken cancellationToken)
        {
            List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
            if (targets.Count == 0)
                return true;

            Dictionary<int, string> failureReasons = new();
            if (!IsControlPcbV2(group.ProtocolVersion))
            {
                string message = $"控制PCB组 {group.Name} 使用 {group.ProtocolVersion}，起动误差A2/A0/0x38流程只支持V2协议。";
                RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, false, message, string.Empty));
                LogControlPcbGroupBlock(context.TestItemName, group, targets, message, StationLogSeparator);
                return false;
            }

            Dictionary<byte, uint> meterConstants = new();
            foreach (ControlPcbStationTarget target in targets)
            {
                if (!meterArchives.TryGetValue(target.StationNo, out MeterArchiveData? archive) ||
                    !TryParseUnsignedConstant(archive.ActiveConstant, out ulong activeConstant) ||
                    activeConstant is 0 or > uint.MaxValue)
                {
                    failureReasons[target.StationNo] = $"资产信息有功常数无效：{archive?.ActiveConstant ?? "空"}";
                    continue;
                }

                meterConstants[target.MeterAddress] = (uint)activeConstant;
                RunOnUiThread(() => UpdateStationRunningState(target.StationNo, context));
            }

            List<ControlPcbStationTarget> activeTargets = targets
                .Where(target => meterConstants.ContainsKey(target.MeterAddress))
                .ToList();
            if (activeTargets.Count == 0)
            {
                ApplyStartingErrorGroupResults(group, targets, context, failureReasons, Array.Empty<byte>());
                return false;
            }

            if (!controlPcbConnectionManager.TryGetConnectedConnection(
                    group,
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                string message = connectionError;
                foreach (ControlPcbStationTarget target in activeTargets)
                {
                    failureReasons[target.StationNo] = message;
                }

                LogControlPcbGroupBlock(context.TestItemName, group, targets, message, StationLogSeparator);
                ApplyStartingErrorGroupResults(group, targets, context, failureReasons, Array.Empty<byte>());
                return false;
            }

            LogControlPcbGroupBlock(context.TestItemName, group, targets, $" 复用控制PCB长连接：{connection.DisplayName}", StationLogSeparator);
            TimeSpan responseTimeout = TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs));
            TimeSpan packetInterval = TimeSpan.FromMilliseconds(packetIntervalMs);

            byte[] standardPayload = ToLittleEndianBytes(standardConstant);
            Dictionary<byte, byte[]> a2ExpectedPayloads = activeTargets.ToDictionary(
                target => target.MeterAddress,
                _ => standardPayload);
            Dictionary<byte, byte[]> a2Responses = await SendControlPcbPacketsAndCollectResponsesAsync(
                context.TestItemName,
                connection,
                group,
                activeTargets,
                target => BuildV2MeterPacket(target.MeterAddress, MeterStandardActiveConstantCommand, standardPayload),
                target => $"A2设置标准表有功常数[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 常数={standardConstant}]",
                frame => ResolveExpectedControlPcbResponse(frame, group.ProtocolVersion, MeterStandardActiveConstantCommand, a2ExpectedPayloads),
                responseTimeout,
                packetInterval,
                cancellationToken);
            activeTargets = KeepRespondedStartingErrorTargets(
                activeTargets,
                a2Responses,
                failureReasons,
                "A2设置标准表常数未收到正确应答");

            if (activeTargets.Count > 0)
            {
                Dictionary<byte, byte[]> a0ExpectedPayloads = activeTargets.ToDictionary(
                    target => target.MeterAddress,
                    target => ToLittleEndianBytes(meterConstants[target.MeterAddress]));
                Dictionary<byte, byte[]> a0Responses = await SendControlPcbPacketsAndCollectResponsesAsync(
                    context.TestItemName,
                    connection,
                    group,
                    activeTargets,
                    target => BuildV2MeterPacket(target.MeterAddress, MeterActiveConstantCommand, a0ExpectedPayloads[target.MeterAddress]),
                    target => $"A0设置电能表有功常数[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 常数={meterConstants[target.MeterAddress]}]",
                    frame => ResolveExpectedControlPcbResponse(frame, group.ProtocolVersion, MeterActiveConstantCommand, a0ExpectedPayloads),
                    responseTimeout,
                    packetInterval,
                    cancellationToken);
                activeTargets = KeepRespondedStartingErrorTargets(
                    activeTargets,
                    a0Responses,
                    failureReasons,
                    "A0设置电能表常数未收到正确应答");
            }

            if (activeTargets.Count > 0)
            {
                byte[] startPayload = { BasicErrorStartOperation, pulseCount, testCount, pulseType };
                Dictionary<byte, byte[]> startExpectedPayloads = activeTargets.ToDictionary(
                    target => target.MeterAddress,
                    _ => startPayload);
                Dictionary<byte, byte[]> startResponses = await SendControlPcbPacketsAndCollectResponsesAsync(
                    context.TestItemName,
                    connection,
                    group,
                    activeTargets,
                    target => BuildV2MeterPacket(target.MeterAddress, MeterBasicErrorCommand38, startPayload),
                    target => $"0x38开启起动试验[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 脉冲数={pulseCount}, 次数={testCount}, 类型={(pulseType == ActivePulseType ? "有功" : "无功")}]",
                    frame => ResolveExpectedControlPcbResponse(frame, group.ProtocolVersion, MeterBasicErrorCommand38, startExpectedPayloads),
                    responseTimeout,
                    packetInterval,
                    cancellationToken);
                activeTargets = KeepRespondedStartingErrorTargets(
                    activeTargets,
                    startResponses,
                    failureReasons,
                    "0x38开启起动试验未收到正确应答");
            }

            HashSet<byte> successfulAddresses = activeTargets
                .Select(target => target.MeterAddress)
                .ToHashSet();
            ApplyStartingErrorGroupResults(group, targets, context, failureReasons, successfulAddresses);
            bool groupPassed = targets.All(target => successfulAddresses.Contains(target.MeterAddress));
            LogControlPcbGroupBlock(
                context.TestItemName,
                group,
                targets,
                groupPassed ? "A2、A0和0x38启动命令全部应答正常" : "起动误差启动流程存在失败工位",
                StationLogSeparator);
            return groupPassed;
        }

        /// <summary>保留正确应答的工位，并为未应答工位记录失败原因。</summary>
        private static List<ControlPcbStationTarget> KeepRespondedStartingErrorTargets(
            IEnumerable<ControlPcbStationTarget> targets,
            IReadOnlyDictionary<byte, byte[]> responses,
            IDictionary<int, string> failureReasons,
            string failureReason)
        {
            List<ControlPcbStationTarget> respondedTargets = new();
            foreach (ControlPcbStationTarget target in targets)
            {
                if (responses.ContainsKey(target.MeterAddress))
                {
                    respondedTargets.Add(target);
                }
                else
                {
                    failureReasons[target.StationNo] = failureReason;
                }
            }

            return respondedTargets;
        }

        /// <summary>把起动误差启动流程的逐工位结论写入界面、缓存和数据库。</summary>
        private void ApplyStartingErrorGroupResults(
            MeterTestControlPcbGroup group,
            IEnumerable<ControlPcbStationTarget> targets,
            SelectedSubItemContext context,
            IReadOnlyDictionary<int, string> failureReasons,
            IEnumerable<byte> successfulAddresses)
        {
            HashSet<byte> successSet = successfulAddresses.ToHashSet();
            foreach (ControlPcbStationTarget target in targets)
            {
                bool passed = successSet.Contains(target.MeterAddress);
                string message = passed
                    ? "A2标准表常数、A0电能表常数和0x38起动误差启动命令应答正常。"
                    : failureReasons.TryGetValue(target.StationNo, out string? reason)
                        ? reason
                        : "起动误差启动流程未完成。";
                LogControlPcbStationBlock(context.TestItemName, group, target, $"结论：{(passed ? "合格" : "不合格")}，{message}");
                RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, passed, message));
            }
        }

        /// <summary>校验控制PCB应答命令和数据项，并返回应答所属表位地址。</summary>
        private static byte? ResolveExpectedControlPcbResponse(
            byte[] frame,
            string protocolVersion,
            byte command,
            IReadOnlyDictionary<byte, byte[]> expectedPayloads)
        {
            if (!TryGetControlPcbPacketDataItems(frame, protocolVersion, command, out byte meterAddress, out byte[] dataItems) ||
                !expectedPayloads.TryGetValue(meterAddress, out byte[]? expectedPayload) ||
                !dataItems.SequenceEqual(expectedPayload))
            {
                return null;
            }

            return meterAddress;
        }

        /// <summary>读取并校验起动误差试验的XML参数。</summary>
        private static bool TryGetStartingErrorConfig(
            MeterTestSubItem subItem,
            out byte pulseCount,
            out byte testCount,
            out byte pulseType,
            out int packetIntervalMs,
            out string? errorMessage)
        {
            pulseCount = 0;
            testCount = 0;
            pulseType = 0;
            packetIntervalMs = Math.Max(0, subItem.PacketIntervalMs);
            errorMessage = null;

            if (subItem.BasicErrorPulseCount is < 1 or > 99)
            {
                errorMessage = "0x38脉冲数必须在1-99之间。";
                return false;
            }

            if (subItem.BasicErrorTestCount is < 1 or > 10)
            {
                errorMessage = "0x38试验次数必须在1-10之间。";
                return false;
            }

            if (subItem.BasicErrorPulseType is < 0 or > 1)
            {
                errorMessage = "0x38脉冲类型只支持0（有功）或1（无功）。";
                return false;
            }

            pulseCount = (byte)subItem.BasicErrorPulseCount;
            testCount = (byte)subItem.BasicErrorTestCount;
            pulseType = (byte)subItem.BasicErrorPulseType;
            return true;
        }

        /// <summary>从纯数字或带说明文本中提取第一个无符号整数常数。</summary>
        private static bool TryParseUnsignedConstant(string? value, out ulong constant)
        {
            constant = 0;
            string normalized = value?.Trim() ?? string.Empty;
            if (ulong.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out constant))
                return true;

            Match match = Regex.Match(normalized, @"\d+");
            return match.Success &&
                   ulong.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out constant);
        }

        /// <summary>把ulong按控制PCB协议要求编码成8字节小端数据。</summary>
        private static byte[] ToLittleEndianBytes(ulong value)
        {
            byte[] bytes = new byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(bytes, value);
            return bytes;
        }

        /// <summary>把uint按控制PCB协议要求编码成4字节小端数据。</summary>
        private static byte[] ToLittleEndianBytes(uint value)
        {
            byte[] bytes = new byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
            return bytes;
        }

        /// <summary>
        /// 按资产信息逐工位计算Tst上限，并以最大Tst向上取整作为统一等待时间。
        /// 选中工位的起动试验已经同时启动，因此等待最大值可以覆盖所有有效工位。
        /// </summary>
        private async Task ExecuteStartingTimeWaitAsync(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            IReadOnlyDictionary<int, MeterArchiveData> meterArchives =
                accessDatabaseService.LoadOrCreateMeterArchives(MaxStationCount);
            List<MeterTestStartingTimeResult> calculations = new();
            bool allParametersValid = true;

            foreach (StationCommunicationConfig station in selectedStations)
            {
                MeterTestStartingTimeResult? calculation = null;
                string? calculationError;
                if (!meterArchives.TryGetValue(station.StationNo, out MeterArchiveData? archive))
                {
                    calculationError = "缺少资产信息";
                }
                else if (!MeterTestStartingTestCalculator.TryCalculateStartingTime(
                             archive,
                             out calculation,
                             out calculationError))
                {
                    // calculationError 由计算器返回具体的无效资产字段。
                }

                if (calculation is null)
                {
                    allParametersValid = false;
                    string errorMessage = $"起动时间计算失败：{calculationError ?? "未知参数错误"}";
                    LogStartingTimeStationBlock(context.TestItemName, station.StationNo, errorMessage);
                    RunOnUiThread(() => ApplyStationExecutionResult(station.StationNo, context, false, errorMessage));
                    continue;
                }

                calculations.Add(calculation);
                string calculationMessage = FormatStartingTimeCalculation(calculation);
                LogMessage.Debug($"[起动试验] 工位{station.StationNo}{calculationMessage}");
                LogStartingTimeStationBlock(context.TestItemName, station.StationNo, calculationMessage);
                RunOnUiThread(() => UpdateStationRunningState(station.StationNo, context));
            }

            if (calculations.Count == 0)
            {
                RunOnUiThread(() => AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    false,
                    "所有选中工位的起动时间参数均无效，未执行等待。",
                    Math.Max(0, Environment.TickCount64 - startTicks)));
                return;
            }

            int waitSeconds = calculations.Max(calculation => calculation.WaitSeconds);
            string limitingStations = string.Join(",", calculations
                .Where(calculation => calculation.WaitSeconds == waitSeconds)
                .Select(calculation => calculation.StationNo));
            await DelayTestWithCountdownAsync(
                waitSeconds,
                $"开始起动时间倒计时：{waitSeconds}s，按最大Tst工位={limitingStations}",
                $"起动时间倒计时结束：{waitSeconds}s",
                message =>
                {
                    LogMessage.Debug($"[起动试验] {message}");
                    foreach (MeterTestStartingTimeResult calculation in calculations)
                    {
                        LogStartingTimeStationBlock(context.TestItemName, calculation.StationNo, message);
                    }
                },
                cancellationToken);

            foreach (MeterTestStartingTimeResult calculation in calculations)
            {
                string resultMessage = $"已按最大Tst统一等待{waitSeconds}s；本工位Tst上限={calculation.UpperSeconds:0.####}s。";
                RunOnUiThread(() => ApplyStationExecutionResult(calculation.StationNo, context, true, resultMessage));
            }

            bool passed = allParametersValid && calculations.Count == selectedStations.Count;
            RunOnUiThread(() =>
            {
                RestoreStationDisplayForSelectedNode();
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    passed,
                    passed
                        ? $"起动时间计算完成，按最大Tst向上取整等待{waitSeconds}s。"
                        : $"有效工位已等待{waitSeconds}s，但存在资产参数无效工位。",
                    Math.Max(0, Environment.TickCount64 - startTicks));
            });
        }

        /// <summary>格式化单个工位的Tst计算过程，便于现场复核参数来源。</summary>
        private static string FormatStartingTimeCalculation(MeterTestStartingTimeResult result)
        {
            return $"起动时间参数：等级={result.ActiveClass}，Est={result.EstPercent:0.###}%={result.EstRatio:0.#####}，"
                + $"C={result.MeterConstant:0.###}imp/kWh，U={result.Voltage:0.###}V，"
                + $"Ist={result.StartingCurrent:0.#########}A，d={result.UnitFactor:0}，"
                + $"Pst=U×Ist×d={result.StartingPower:0.######}W，Ki=1，Ku=1，"
                + $"Tst下限={result.LowerSeconds:0.####}s，Tst上限={result.UpperSeconds:0.####}s，"
                + $"等待={result.WaitSeconds}s；{result.CalculationNote}。";
        }

        /// <summary>写入起动时间工位日志文件和右侧过程日志区域。</summary>
        private void LogStartingTimeStationBlock(string testItemName, int stationNo, params string[] lines)
        {
            string message = string.Join(Environment.NewLine, lines);
            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}",
                "起动时间日志",
                message);
        }

        /// <summary>
        /// 通过控制PCB向各工位发送0x38+AA读取起动误差，并缓存解析出的float结果。
        /// </summary>
        private async Task ExecuteControlPcbStartingErrorReadStepAsync(
            SelectedSubItemContext context,
            List<StationCommunicationConfig> selectedStations,
            CancellationToken cancellationToken)
        {
            long startTicks = Environment.TickCount64;
            if (!TryGetStartingErrorReadConfig(
                    context.SubItem,
                    out byte pulseCount,
                    out byte testCount,
                    out int packetIntervalMs,
                    out string? configError))
            {
                RunOnUiThread(() =>
                {
                    SaveStationConclusions(
                        context,
                        selectedStations,
                        selectedStations.ToDictionary(station => station.StationNo, _ => false),
                        configError ?? "读取起动误差配置错误。");
                    AddProcessLog(context.SchemeName, context.SubItem.Name, false, configError ?? "读取起动误差配置错误。", 0);
                });
                return;
            }

            foreach (StationCommunicationConfig station in selectedStations)
            {
                startingErrorResults.TryRemove(station.StationNo, out _);
            }

            List<Task<bool>> groupTasks = GetEnabledControlPcbGroups(context.SubItem)
                .Select(group => ExecuteControlPcbStartingErrorReadGroupAsync(
                    group,
                    selectedStations,
                    context,
                    pulseCount,
                    testCount,
                    packetIntervalMs,
                    cancellationToken))
                .ToList();
            if (groupTasks.Count == 0)
            {
                const string message = "未找到可用控制PCB分组，请检查 ControlPcbGroups。";
                RunOnUiThread(() => AddProcessLog(context.SchemeName, context.SubItem.Name, false, message, 0));
                return;
            }

            bool[] groupResults = await Task.WhenAll(groupTasks);
            bool passed = groupResults.Length > 0 && groupResults.All(result => result);
            RunOnUiThread(() =>
            {
                RestoreStationDisplayForSelectedNode();
                AddProcessLog(
                    $"{context.SchemeName}/{context.TestItemName}",
                    context.SubItem.Name,
                    passed,
                    passed ? "所有选中工位均已读取并解析起动误差结果。" : "存在未读取到有效起动误差结果的工位。",
                    Math.Max(0, Environment.TickCount64 - startTicks));
            });
        }

        /// <summary>执行单个控制PCB组的0x38起动误差读取。</summary>
        private async Task<bool> ExecuteControlPcbStartingErrorReadGroupAsync(
            MeterTestControlPcbGroup group,
            List<StationCommunicationConfig> selectedStations,
            SelectedSubItemContext context,
            byte pulseCount,
            byte testCount,
            int packetIntervalMs,
            CancellationToken cancellationToken)
        {
            List<ControlPcbStationTarget> targets = GetControlPcbStationTargets(group, selectedStations);
            if (targets.Count == 0)
                return true;

            if (!IsControlPcbV2(group.ProtocolVersion))
            {
                string message = $"控制PCB组 {group.Name} 使用 {group.ProtocolVersion}，0x38误差读取只支持V2协议。";
                LogControlPcbGroupBlock(context.TestItemName, group, targets, message, StationLogSeparator);
                RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, false, message, string.Empty));
                return false;
            }

            foreach (ControlPcbStationTarget target in targets)
            {
                RunOnUiThread(() => UpdateStationRunningState(target.StationNo, context));
            }

            if (!controlPcbConnectionManager.TryGetConnectedConnection(
                    group,
                    out MeterTestControlPcbConnection connection,
                    out string connectionError))
            {
                LogControlPcbGroupBlock(context.TestItemName, group, targets, connectionError, StationLogSeparator);
                RunOnUiThread(() => ApplyControlPcbGroupResult(targets, context, false, connectionError, string.Empty));
                return false;
            }

            LogControlPcbGroupBlock(context.TestItemName, group, targets, $" 复用控制PCB长连接：{connection.DisplayName}", StationLogSeparator);
            byte[] resultPayload = { BasicErrorResultOperation, pulseCount, testCount };
            Dictionary<byte, byte[]> responses = await SendControlPcbPacketsAndCollectResponsesAsync(
                context.TestItemName,
                connection,
                group,
                targets,
                target => BuildV2MeterPacket(target.MeterAddress, MeterBasicErrorCommand38, resultPayload),
                target => $"0x38读取起动误差[工位={target.StationNo}, 表位={target.MeterAddress:X2}, 脉冲数={pulseCount}, 次数={testCount}]",
                frame => ResolveStartingErrorResultResponse(frame, group.ProtocolVersion, pulseCount, testCount),
                TimeSpan.FromMilliseconds(Math.Max(100, context.SubItem.TimeoutMs)),
                TimeSpan.FromMilliseconds(packetIntervalMs),
                cancellationToken);

            bool groupPassed = true;
            foreach (ControlPcbStationTarget target in targets)
            {
                bool hasResponse = responses.TryGetValue(target.MeterAddress, out byte[]? response);
                float errorValue = 0;
                string parseMessage = "未收到0x38误差结果应答。";
                bool parsed = hasResponse && TryParseStartingErrorResult(
                    response!,
                    group.ProtocolVersion,
                    pulseCount,
                    testCount,
                    out _,
                    out errorValue,
                    out parseMessage);
                if (parsed)
                {
                    startingErrorResults[target.StationNo] = errorValue;
                    string message = $"误差结果读取成功，误差值：{errorValue.ToString("0.######", CultureInfo.InvariantCulture)}；{parseMessage}";
                    LogControlPcbStationBlock(context.TestItemName, group, target, message, StationLogSeparator);
                    RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, true, message));
                }
                else
                {
                    groupPassed = false;
                    string message = hasResponse
                        ? $"误差结果解析失败：{parseMessage}"
                        : "未收到0x38误差结果应答。";
                    LogControlPcbStationBlock(context.TestItemName, group, target, message, StationLogSeparator);
                    RunOnUiThread(() => ApplyStationExecutionResult(target.StationNo, context, false, message));
                }
            }

            return groupPassed;
        }

        /// <summary>按配置阈值判断已读取的起动误差结果。</summary>
        private void ExecuteStartingErrorJudgeStep(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> selectedStations)
        {
            long startTicks = Environment.TickCount64;
            decimal standardValue = context.SubItem.BasicErrorLimit > 0
                ? context.SubItem.BasicErrorLimit
                : 1.5m;
            bool allPassed = true;

            foreach (StationCommunicationConfig station in selectedStations)
            {
                bool hasResult = startingErrorResults.TryGetValue(station.StationNo, out float errorValue);
                bool passed = hasResult && Math.Abs((decimal)errorValue) < standardValue;
                allPassed &= passed;
                string errorText = hasResult
                    ? errorValue.ToString("0.######", CultureInfo.InvariantCulture)
                    : "未读取";
                string message = $"标准值：{standardValue.ToString("0.######", CultureInfo.InvariantCulture)}，误差值：{errorText}，结论：{(passed ? "合格" : "不合格")}";
                LogStartingErrorStationBlock(context.TestItemName, station.StationNo, message);
                ApplyStationExecutionResult(station.StationNo, context, passed, message);
            }

            RestoreStationDisplayForSelectedNode();
            AddProcessLog(
                $"{context.SchemeName}/{context.TestItemName}",
                context.SubItem.Name,
                allPassed,
                allPassed
                    ? $"所有工位误差绝对值均小于{standardValue:0.######}。"
                    : $"存在误差绝对值不小于{standardValue:0.######}或未读取结果的工位。",
                Math.Max(0, Environment.TickCount64 - startTicks));
        }

        /// <summary>校验0x38结果应答并返回表位地址。</summary>
        private static byte? ResolveStartingErrorResultResponse(
            byte[] frame,
            string protocolVersion,
            byte pulseCount,
            byte testCount)
        {
            return TryParseStartingErrorResult(
                frame,
                protocolVersion,
                pulseCount,
                testCount,
                out byte meterAddress,
                out _,
                out _)
                ? meterAddress
                : null;
        }

        /// <summary>校验0x35潜动启动应答，并返回应答所属表位地址。</summary>
        private static byte? ResolveCreepingTestStartResponse(
            byte[] frame,
            byte expectedPulseCount,
            uint expectedTimeSeconds)
        {
            if (frame == null || frame.Length < 11)
                return null;

            byte meterAddress = frame[5];
            return ElectricEnergyMeterControlV2.TryParseCreepingTestResponse(
                       frame,
                       meterAddress,
                       ElectricEnergyMeterControlV2.CreepingTestStartOperation,
                       out byte pulseCount,
                       out uint timeSeconds) &&
                   pulseCount == expectedPulseCount &&
                   timeSeconds == expectedTimeSeconds
                ? meterAddress
                : null;
        }

        /// <summary>校验0x35+AA潜动结果应答，并返回应答所属表位地址。</summary>
        private static byte? ResolveCreepingTestResultResponse(byte[] frame)
        {
            if (frame == null || frame.Length < 11)
                return null;

            byte meterAddress = frame[5];
            return ElectricEnergyMeterControlV2.TryParseCreepingTestResponse(
                frame,
                meterAddress,
                ElectricEnergyMeterControlV2.CreepingTestResultOperation,
                out _,
                out _)
                ? meterAddress
                : null;
        }

        /// <summary>解析0x38+AA应答中的小端float结果，多个结果时返回平均值。</summary>
        private static bool TryParseStartingErrorResult(
            byte[] frame,
            string protocolVersion,
            byte pulseCount,
            byte testCount,
            out byte meterAddress,
            out float errorValue,
            out string message)
        {
            meterAddress = 0;
            errorValue = 0;
            message = string.Empty;
            if (!TryGetControlPcbPacketDataItems(
                    frame,
                    protocolVersion,
                    MeterBasicErrorCommand38,
                    out meterAddress,
                    out byte[] dataItems))
            {
                message = "报文帧格式、方向、协议类型、命令码或校验和错误。";
                return false;
            }

            if (dataItems.Length < 3 ||
                dataItems[0] != BasicErrorResultOperation ||
                dataItems[1] != pulseCount ||
                dataItems[2] != testCount)
            {
                message = $"结果头不匹配，期望AA {pulseCount:X2} {testCount:X2}。";
                return false;
            }

            int resultDataLength = dataItems.Length - 3;
            if (resultDataLength < 4 || resultDataLength % 4 != 0)
            {
                message = $"误差数据长度{resultDataLength}不是有效float长度。";
                return false;
            }

            int resultCount = resultDataLength / 4;
            if (resultCount > testCount)
            {
                message = $"返回结果数量{resultCount}超过配置试验次数{testCount}。";
                return false;
            }

            List<float> results = new(resultCount);
            for (int index = 3; index < dataItems.Length; index += 4)
            {
                int bits = BinaryPrimitives.ReadInt32LittleEndian(dataItems.AsSpan(index, 4));
                float value = BitConverter.Int32BitsToSingle(bits);
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    message = $"第{results.Count + 1}个误差结果不是有效float。";
                    return false;
                }

                results.Add(value);
            }

            errorValue = results.Average();
            message = resultCount == 1
                ? "成功解析1个误差结果。"
                : $"成功解析{resultCount}个误差结果，使用平均值。";
            return true;
        }

        /// <summary>读取并校验0x38误差结果查询参数。</summary>
        private static bool TryGetStartingErrorReadConfig(
            MeterTestSubItem subItem,
            out byte pulseCount,
            out byte testCount,
            out int packetIntervalMs,
            out string? errorMessage)
        {
            pulseCount = 0;
            testCount = 0;
            packetIntervalMs = Math.Max(0, subItem.PacketIntervalMs);
            errorMessage = null;
            if (subItem.BasicErrorPulseCount is < 1 or > 99)
            {
                errorMessage = "0x38结果查询脉冲数必须在1-99之间。";
                return false;
            }

            if (subItem.BasicErrorTestCount is < 1 or > 10)
            {
                errorMessage = "0x38结果查询试验次数必须在1-10之间。";
                return false;
            }

            pulseCount = (byte)subItem.BasicErrorPulseCount;
            testCount = (byte)subItem.BasicErrorTestCount;
            return true;
        }

        /// <summary>写入起动误差工位日志文件和右侧过程日志区域。</summary>
        private void LogStartingErrorStationBlock(string testItemName, int stationNo, params string[] lines)
        {
            string message = string.Join(Environment.NewLine, lines);
            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}",
                "起动误差日志",
                message);
        }

        /// <summary>写入潜动走字工位日志文件和右侧过程日志区域。</summary>
        private void LogCreepingStationBlock(string testItemName, int stationNo, params string[] lines)
        {
            string message = string.Join(Environment.NewLine, lines);
            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}",
                "潜动走字日志",
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
            LogMessage.MeterTestStationRawLog(testItemName, stationNo, message);
            AppendTestLog(
                stationNo,
                $"{testItemName}/工位{stationNo}/{testSubItemName}",
                logType,
                message);
        }

        /// <summary>
        /// 执行通用测试倒计时。
        /// 日计时和起动试验共用该方法，只记录开始和结束，不逐秒刷日志。
        /// </summary>
        private static async Task DelayTestWithCountdownAsync(
            int waitSeconds,
            string startMessage,
            string completedMessage,
            Action<string> logAction,
            CancellationToken cancellationToken)
        {
            logAction(startMessage);
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds), cancellationToken);
            logAction(completedMessage);
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
        private async Task<string> SendStationRequestAsync(
            StationCommunicationConfig station,
            SelectedSubItemContext context,
            CancellationToken cancellationToken)
        {
            MeterTestSubItem subItem = context.SubItem;
            string requestHex = BuildStationRequestHex(station, context);
            byte[] requestBytes = ParseHexBytes(requestHex);
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
            LogStationCommunicationBlock(context.TestItemName, station, $"{FormatStationLogTimestamp()} - 发送报文：{NormalizeHex(requestHex)}");
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
        /// 根据测试小项生成工位实际发送的请求报文。
        /// 698 地址读取使用每个工位自己的电表地址，其他测试仍使用 XML 中配置的报文。
        /// </summary>
        private static string BuildStationRequestHex(
            StationCommunicationConfig station,
            SelectedSubItemContext context)
        {
            if (UsesSgcc698BroadcastAddressParser(context.SubItem))
            {
                if (string.IsNullOrWhiteSpace(station.MeterAddress))
                {
                    throw new InvalidOperationException($"工位{station.StationNo} 未配置电表地址，无法生成定址 698 读地址报文。");
                }

                return SGCCTools.BuildMeterAddressReadRequest(station.MeterAddress);
            }

            return context.SubItem.RequestHex;
        }

        /// <summary>
        /// 向控制 PCB 发送多工位报文，并按表位地址收集每个工位的响应。
        /// </summary>
        private async Task<Dictionary<byte, byte[]>> SendControlPcbPacketsAndCollectResponsesAsync(
            string testItemName,
            MeterTestControlPcbConnection connection,
            MeterTestControlPcbGroup group,
            List<ControlPcbStationTarget> targets,
            Func<ControlPcbStationTarget, byte[]> packetFactory,
            Func<ControlPcbStationTarget, string> packetNameFactory,
            Func<byte[], byte?> responseAddressResolver,
            TimeSpan timeout,
            TimeSpan packetInterval,
            CancellationToken cancellationToken)
        {
            Dictionary<byte, TaskCompletionSource<byte[]>> pending = targets.ToDictionary(
                target => target.MeterAddress,
                _ => new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously));

            using IDisposable subscription = connection.Subscribe(frame =>
            {
                byte? meterAddress = responseAddressResolver(frame);
                if (meterAddress.HasValue && pending.TryGetValue(meterAddress.Value, out TaskCompletionSource<byte[]>? completionSource))
                {
                    completionSource.TrySetResult(frame);
                }
            });

            byte[][] packets = targets.Select(packetFactory).ToArray();
            await connection.SendSequenceAsync(
                    packets,
                    packetInterval,
                    (index, packet) =>
                    {
                        ControlPcbStationTarget target = targets[index];
                        string packetHex = BitConverter.ToString(packet).Replace("-", " ");
                        LogControlPcbStationBlock(
                            testItemName,
                            group,
                            target,
                            $"{FormatStationLogTimestamp()} - 发送报文：{packetHex}，{packetNameFactory(target)}");
                    },
                    cancellationToken);

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
            string meterAddress = UsesSgcc698BroadcastAddressParser(subItem)
                ? NormalizeMeterAddressForComparison(station.MeterAddress)
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
                if (!HasCompleteAssetForTest(row))
                {
                    LogMessage.Debug($"[资产联动] 工位{stationNo}未完成条形码扫码或电表地址提取，本次测试已跳过。");
                    continue;
                }

                string ip = Convert.ToString(row.Cells[colStationIp.Index].Value)?.Trim() ?? string.Empty;
                string portText = Convert.ToString(row.Cells[colStationPort.Index].Value)?.Trim() ?? string.Empty;
                string meterAddress = Convert.ToString(row.Cells[colStationMeterAddress.Index].Value)?.Trim() ?? string.Empty;
                string baudRate = Convert.ToString(row.Cells[colMeterBaudRate.Index].Value)?.Trim() ?? "9600-8-E-1";

                if (string.IsNullOrWhiteSpace(ip) || !int.TryParse(portText, out int port) || port < 1 || port > 65535)
                {
                    throw new InvalidOperationException($"工位{stationNo} IP 或端口配置不正确。");
                }

                stations.Add(new StationCommunicationConfig(stationNo, ip, port, meterAddress, baudRate));
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
        /// 全选或全清工位，并对实际发生变化的工位执行对应的上电或下电操作。
        /// 单工位模式点击全选时只选择第一个工位。
        /// </summary>
        private async Task SetAllStationSelectionAsync(bool selected)
        {
            List<StationPowerSelectionChange> changes = new();
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
                    bool currentSelected = Convert.ToBoolean(row.Cells[colStationSelected.Index].Value ?? false);
                    if (currentSelected == targetSelected)
                        continue;

                    row.Cells[colStationSelected.Index].Value = targetSelected;
                    int stationNo = Convert.ToInt32(row.Cells[colStationNo.Index].Value);
                    changes.Add(new StationPowerSelectionChange(stationNo, targetSelected));
                }
            }
            finally
            {
                isUpdatingStationSelection = false;
            }

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

                    bool currentSelected = Convert.ToBoolean(row.Cells[colStationSelected.Index].Value ?? false);
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

            bool isSelected = Convert.ToBoolean(changedRow.Cells[colStationSelected.Index].Value ?? false);
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
                            !Convert.ToBoolean(row.Cells[colStationSelected.Index].Value ?? false))
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
                if (!meterArchives.TryGetValue(change.StationNo, out MeterArchiveData? archive))
                {
                    LogMessage.Debug($"[工位电源] 工位{change.StationNo}未读取到资产信息，取消电源操作。");
                    return;
                }

                string meterType = archive.MeterType.Trim();
                bool isThreePhase;
                if (meterType.Contains("三相", StringComparison.OrdinalIgnoreCase))
                {
                    isThreePhase = true;
                }
                else if (meterType.Contains("单相", StringComparison.OrdinalIgnoreCase))
                {
                    isThreePhase = false;
                }
                else
                {
                    LogMessage.Debug(
                        $"[工位电源] 工位{change.StationNo}资产信息中的电表类型无法识别：{meterType}，取消电源操作。");
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
                    Convert.ToBoolean(row.Cells[colStationSelected.Index].Value ?? false))
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
        /// 将日计时最终结果复制到当前流程节点。
        /// 最终三轮平均值仍以第三轮“读取结果”节点为原始数据源，其他节点只同步展示结论。
        /// </summary>
        private void SynchronizeDailyTimingStepResults(
            SelectedSubItemContext context,
            IReadOnlyList<StationCommunicationConfig> stations,
            string fallbackMessage)
        {
            SelectedSubItemContext finalContext = GetFinalDailyTimingResultContext(context);
            LoadStationResultsFromAccess(finalContext);

            foreach (StationCommunicationConfig station in stations)
            {
                if (stationResultCache.TryGetValue(CreateStationResultKey(finalContext, station.StationNo), out StationDisplayState? state))
                {
                    SaveStationDisplayState(
                        context,
                        station.StationNo,
                        context.SubItem.Name,
                        state.MeterAddress,
                        state.Result,
                        state.Time,
                        state.ResultColor,
                        string.IsNullOrWhiteSpace(state.ToolTip) ? fallbackMessage : state.ToolTip);
                }
                else
                {
                    SaveStationConclusions(
                        context,
                        new[] { station },
                        new Dictionary<int, bool> { [station.StationNo] = false },
                        fallbackMessage);
                }
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

        /// <summary>
        /// 判断当前测试上下文是否属于通信测试。
        /// 通信测试中的准备步骤失败后仍继续执行后续步骤，最终尝试地址读取。
        /// </summary>
        private static bool IsCommunicationTestContext(SelectedSubItemContext context)
        {
            return context.TestItemName.Contains("通信", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断测试小项是否为方案树中的串口服务器波特率检查步骤。
        /// </summary>
        private static bool UsesSerialPortServerBaudRateExecution(MeterTestSubItem subItem)
        {
            return subItem.ExecutionMode.Equals(
                MeterTestExecutionMode.SerialPortServerBaudRateSync.ToString(),
                StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 判断测试小项是否是起动试验的启动电流升源步骤。
        /// 该步骤仍复用源控制服务，但初始化电流改为根据资产档案计算出的 Ist。
        /// </summary>
        private static bool UsesStartingSourceExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.StartingSource;
        }

        /// <summary>
        /// 判断测试小项是否是潜动试验的1.1倍额定电压升源步骤。
        /// </summary>
        private static bool UsesCreepingSourceExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.CreepingSource;
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

        /// <summary>判断测试小项是否负责通过V2控制PCB启动0x35潜动走字试验。</summary>
        private static bool UsesControlPcbCreepingStartExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.ControlPcbCreepingStart;
        }

        /// <summary>判断测试小项是否负责按XML中的固定秒数执行潜动等待。</summary>
        private static bool UsesCreepingWaitExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.CreepingWait;
        }

        /// <summary>判断测试小项是否负责通过V2控制PCB读取0x35潜动累计结果。</summary>
        private static bool UsesControlPcbCreepingReadExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.ControlPcbCreepingRead;
        }

        /// <summary>判断测试小项是否负责按累计脉冲数小于等于1判定潜动结果。</summary>
        private static bool UsesCreepingPulseJudgeExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.CreepingPulseJudge;
        }

        /// <summary>判断测试小项是否通过统一服务执行完整有功基本误差测试点。</summary>
        private static bool UsesBasicErrorPointExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.BasicErrorPoint;
        }

        /// <summary>判断当前小项是否需要按工位建立独立蓝牙TCP连接。</summary>
        private static bool UsesBluetoothStationTcpExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.BluetoothStationTcp;
        }

        /// <summary>判断测试小项是否负责通过控制PCB开启0x38起动误差试验。</summary>
        private static bool UsesControlPcbStartingErrorExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.ControlPcbStartingError;
        }

        /// <summary>判断测试小项是否负责计算并等待起动时间。</summary>
        private static bool UsesStartingTimeWaitExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.StartingTimeWait;
        }

        /// <summary>判断测试小项是否负责读取0x38起动误差结果。</summary>
        private static bool UsesControlPcbStartingErrorReadExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.ControlPcbStartingErrorRead;
        }

        /// <summary>判断测试小项是否负责按阈值判定起动误差。</summary>
        private static bool UsesStartingErrorJudgeExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.StartingErrorJudge;
        }

        /// <summary>
        /// 判断测试小项是否只是预置节点。
        /// 预置节点不允许落入默认 StationTcp 执行路径，避免空报文误发到工位。
        /// </summary>
        private static bool UsesPlannedTestExecution(MeterTestSubItem subItem)
        {
            return Enum.TryParse(subItem.ExecutionMode, true, out MeterTestExecutionMode executionMode)
                && executionMode == MeterTestExecutionMode.Planned;
        }

        /// <summary>
        /// 找到日计时第三轮“读取结果”小项，最终平均误差按该节点回填结果。
        /// </summary>
        private SelectedSubItemContext GetFinalDailyTimingResultContext(SelectedSubItemContext context)
        {
            MeterTestSubItem? resultSubItem = meterTestPlanConfig.Schemes
                .FirstOrDefault(scheme => scheme.Name.Equals(context.SchemeName, StringComparison.OrdinalIgnoreCase))?
                .TestItems
                .FirstOrDefault(item => item.Name.Equals(context.TestItemName, StringComparison.OrdinalIgnoreCase))?
                .TestSubItems
                .LastOrDefault(subItem =>
                    subItem.ExecutionMode.Equals(
                        MeterTestExecutionMode.ControlPcbDailyTiming.ToString(),
                        StringComparison.OrdinalIgnoreCase) &&
                    subItem.DailyTimingStep.Equals("Read", StringComparison.OrdinalIgnoreCase) &&
                    subItem.DailyTimingRound == 3);

            return resultSubItem is null
                ? context
                : new SelectedSubItemContext(context.SchemeName, context.TestItemName, resultSubItem);
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

        /// <summary>读取并校验0x35潜动走字配置，启动和等待节点使用同一套手动时间。</summary>
        private static bool TryGetCreepingTestConfig(
            MeterTestSubItem subItem,
            out byte pulseCount,
            out uint timeSeconds,
            out int packetIntervalMs)
        {
            pulseCount = 0;
            timeSeconds = 0;
            packetIntervalMs = Math.Max(0, subItem.PacketIntervalMs);
            if (subItem.CreepingPulseCount < 1 || subItem.CreepingPulseCount > byte.MaxValue)
                return false;

            if (subItem.CreepingTimeSeconds < 1)
                return false;

            pulseCount = (byte)subItem.CreepingPulseCount;
            timeSeconds = (uint)subItem.CreepingTimeSeconds;
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
        /// 解析日计时结果响应中的 float 误差值。
        /// V2 报文在 AA、时间、次数之后按 4 字节小端格式连续返回结果。
        /// </summary>
        private static bool TryParseDailyTimingResults(
            byte[] rawData,
            string protocolVersion,
            byte testTime,
            byte testCount,
            out IReadOnlyList<float> values,
            out string message)
        {
            values = Array.Empty<float>();
            message = string.Empty;

            if (!TryGetControlPcbPacketDataItems(
                    rawData,
                    protocolVersion,
                    MeterDailyTimingCommand,
                    out _,
                    out byte[] dataItems))
            {
                message = "报文帧格式、长度、协议类型、命令码或校验和错误。";
                return false;
            }

            if (dataItems.Length < 7 ||
                dataItems[0] != DailyTimingResultDataItem ||
                dataItems[1] != testTime ||
                dataItems[2] != testCount)
            {
                message = "结果头不匹配，期望 AA、测试时间和测试次数。";
                return false;
            }

            int resultDataLength = dataItems.Length - 3;
            if (resultDataLength < 4 || resultDataLength % 4 != 0)
            {
                message = $"结果数据长度 {resultDataLength} 不是 4 字节 float 的整数倍。";
                return false;
            }

            List<float> parsedValues = new(resultDataLength / 4);
            for (int index = 3; index < dataItems.Length; index += 4)
            {
                float value = BitConverter.ToSingle(dataItems, index);
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    message = $"第 {parsedValues.Count + 1} 个误差结果不是有效 float。";
                    return false;
                }

                parsedValues.Add(value);
            }

            values = parsedValues;
            message = $"成功解析 {parsedValues.Count} 个误差结果。";
            return true;
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
        /// 将电表地址统一为 12 个连续的大写十六进制字符，用于实际地址与响应地址比较。
        /// 非 6 字节地址返回空字符串，避免格式差异造成误判。
        /// </summary>
        private static string NormalizeMeterAddressForComparison(string? meterAddress)
        {
            byte[] addressBytes = ParseHexBytes(meterAddress ?? string.Empty);
            return addressBytes.Length == 6
                ? BitConverter.ToString(addressBytes).Replace("-", string.Empty)
                : string.Empty;
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
        private sealed record StationCommunicationConfig(int StationNo, string Ip, int Port, string MeterAddress, string BaudRate);

        /// <summary>控制 PCB 流程中的目标工位与表位地址。</summary>
        private sealed record ControlPcbStationTarget(int StationNo, byte MeterAddress);

        /// <summary>0x35+AA返回的单个工位潜动累计脉冲数和累计时间。</summary>
        private sealed record CreepingPulseMeasurement(byte PulseCount, uint TimeSeconds);

        /// <summary>用户勾选变化对应的工位上电或下电请求。</summary>
        private sealed record StationPowerSelectionChange(int StationNo, bool IsSelected);

        /// <summary>当前被执行的小项上下文。</summary>
        private sealed record SelectedSubItemContext(string SchemeName, string TestItemName, MeterTestSubItem SubItem);

        /// <summary>串口服务器波特率完整流程的整体结论和逐工位结论。</summary>
        private sealed record SerialPortServerBaudFlowResult(
            bool Succeeded,
            IReadOnlyDictionary<int, bool> StationResults,
            IReadOnlyDictionary<int, SerialPortServerStationTrace> StationTraces);

        /// <summary>串口服务器完整流程在单个工位上的可回放日志。</summary>
        private sealed record SerialPortServerStationTrace(
            string IpAddress,
            bool Success,
            string Message,
            IReadOnlyList<string> Details);

        /// <summary>工位结果缓存键。</summary>
        private sealed record StationResultKey(string SchemeName, string TestItemName, string TestSubItemName, int StationNo);

        /// <summary>工位在界面上的完整显示状态。</summary>
        private sealed record StationDisplayState(string TestContent, string MeterAddress, string Result, string Time, Color ResultColor, string ToolTip);

        /// <summary>右侧日志区域的一条有序日志记录。</summary>
        private sealed record TestProcessLogEntry(long Sequence, int? StationNo, string Text);

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

        /// <summary>方案树节点的三态测试结论。</summary>
        private enum SchemeNodeStatus
        {
            Pending,
            Passed,
            Failed
        }
    }
}
