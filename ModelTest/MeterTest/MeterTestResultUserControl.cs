namespace ModelTest.MeterTest;

/// <summary>
/// 嵌入 MeterTest 主界面的历史测试结果视图。
/// 顶部选择任务，左下选择工位，右下展示该工位完整测试明细。
/// </summary>
public partial class MeterTestResultUserControl : UserControl
{
    private readonly MeterTestAccessDatabaseService databaseService;
    private IReadOnlyList<MeterTestResultTaskData> tasks = Array.Empty<MeterTestResultTaskData>();

    /// <summary>创建内嵌结果查询控件并绑定 MeterTest 本地数据库服务。</summary>
    public MeterTestResultUserControl(MeterTestAccessDatabaseService databaseService)
    {
        this.databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        InitializeComponent();
        ConfigureColumns();
        btnRefresh.Click += async (_, _) => await RefreshDataAsync();
        btnExport.Click += (_, _) => ExportSelectedTask();
        dgvTasks.SelectionChanged += (_, _) => LoadSelectedTaskStations();
        dgvStations.SelectionChanged += (_, _) => LoadSelectedStationDetails();
    }

    /// <summary>每次切换到“测试结果”视图时重新从SQLite加载任务。</summary>
    public void RefreshData()
    {
        tasks = databaseService.LoadTestResultTasks();
        BindTaskRows();
    }

    /// <summary>异步加载任务列表，避免历史结果较多时阻塞 MeterTest 主界面。</summary>
    public async Task RefreshDataAsync()
    {
        btnRefresh.Enabled = false;
        Cursor previousCursor = Cursor;
        Cursor = Cursors.WaitCursor;
        try
        {
            tasks = await Task.Run(databaseService.LoadTestResultTasks);
        }
        finally
        {
            Cursor = previousCursor;
            btnRefresh.Enabled = true;
        }

        BindTaskRows();
    }

    /// <summary>把已加载的测试任务绑定到界面，避免同步和异步刷新重复维护两套UI逻辑。</summary>
    private void BindTaskRows()
    {
        dgvTasks.Rows.Clear();
        foreach (MeterTestResultTaskData task in tasks)
        {
            int rowIndex = dgvTasks.Rows.Add(
                task.Id,
                task.SchemeName,
                task.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                task.EndedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                task.Status,
                task.SaveMode,
                task.StationCount,
                task.ResultSummary);
            dgvTasks.Rows[rowIndex].Tag = task;
        }

        SelectFirstRow(dgvTasks);
        if (dgvTasks.Rows.Count == 0)
        {
            dgvStations.Rows.Clear();
            dgvDetails.Rows.Clear();
        }
    }

    /// <summary>创建任务、工位和结果明细三张只读表格的固定列定义。</summary>
    private void ConfigureColumns()
    {
        AddColumn(dgvTasks, "任务ID", 90);
        AddColumn(dgvTasks, "方案", 220);
        AddColumn(dgvTasks, "开始时间", 190);
        AddColumn(dgvTasks, "结束时间", 190);
        AddColumn(dgvTasks, "状态", 120);
        AddColumn(dgvTasks, "保存方式", 140);
        AddColumn(dgvTasks, "工位数", 90);
        AddColumn(dgvTasks, "汇总", 420);

        AddColumn(dgvStations, "工位", 70);
        AddColumn(dgvStations, "条形码", 190);
        AddColumn(dgvStations, "电表地址", 150);
        AddColumn(dgvStations, "电表类型", 110);
        AddColumn(dgvStations, "接入方式", 110);
        AddColumn(dgvStations, "电压", 100);
        AddColumn(dgvStations, "基本电流", 110);
        AddColumn(dgvStations, "电流规格", 190);
        AddColumn(dgvStations, "总结论", 100);

        AddColumn(dgvDetails, "测试项", 180);
        AddColumn(dgvDetails, "测试小项", 260);
        AddColumn(dgvDetails, "结果", 90);
        AddColumn(dgvDetails, "时间", 100);
        AddColumn(dgvDetails, "数值名称", 190);
        AddColumn(dgvDetails, "序号", 70);
        AddColumn(dgvDetails, "数值", 120);
        AddColumn(dgvDetails, "平均值", 120);
        // 同时展示规程最大允许区间和60%实际判定区间，避免历史结果只看到一个裸阈值。
        AddColumn(dgvDetails, "标准/限值", 360);
        AddColumn(dgvDetails, "说明", 520);
    }

    /// <summary>加载当前选中测试任务包含的全部工位资产快照和汇总结论。</summary>
    private void LoadSelectedTaskStations()
    {
        dgvStations.Rows.Clear();
        dgvDetails.Rows.Clear();
        if (dgvTasks.CurrentRow?.Tag is not MeterTestResultTaskData task)
            return;

        foreach (MeterTestResultStationData station in databaseService.LoadTestResultStations(task.Id))
        {
            int rowIndex = dgvStations.Rows.Add(
                station.StationNo,
                station.Barcode,
                station.MeterAddress,
                station.MeterType,
                station.AccessMode,
                station.Voltage,
                station.BasicCurrent,
                station.CurrentSpecification,
                station.OverallResult);
            dgvStations.Rows[rowIndex].Tag = station;
            dgvStations.Rows[rowIndex].Cells[8].Style.ForeColor = GetResultColor(station.OverallResult);
        }

        SelectFirstRow(dgvStations);
    }

    /// <summary>加载当前任务及工位的全部测试项数值、平均值、允许区间和结论。</summary>
    private void LoadSelectedStationDetails()
    {
        dgvDetails.Rows.Clear();
        if (dgvTasks.CurrentRow?.Tag is not MeterTestResultTaskData task ||
            dgvStations.CurrentRow?.Tag is not MeterTestResultStationData station)
        {
            return;
        }

        foreach (MeterTestResultDetailData detail in databaseService.LoadTestResultDetails(task.Id, station.StationNo))
        {
            string numericValue = detail.NumericValue?.ToString("0.######") ?? detail.ValueText;
            string averageValue = detail.AverageValue?.ToString("0.######") ?? string.Empty;
            int rowIndex = dgvDetails.Rows.Add(
                detail.TestItemName,
                detail.TestSubItemName,
                detail.Result,
                detail.ResultTimeText,
                detail.MeasurementName,
                detail.SequenceNo == 0 ? string.Empty : detail.SequenceNo,
                AppendUnit(numericValue, detail.Unit),
                AppendUnit(averageValue, detail.Unit),
                detail.LimitText,
                detail.Message);
            dgvDetails.Rows[rowIndex].Cells[2].Style.ForeColor = GetResultColor(detail.Result);
            dgvDetails.Rows[rowIndex].Cells[8].ToolTipText = detail.LimitText;
            dgvDetails.Rows[rowIndex].Cells[9].ToolTipText = detail.Message;
        }
    }

    /// <summary>选择导出路径并把当前任务的完整数据库结果写入 Excel 模板。</summary>
    private void ExportSelectedTask()
    {
        if (dgvTasks.CurrentRow?.Tag is not MeterTestResultTaskData task)
        {
            MessageBox.Show("请先选择要导出的测试任务。", "数据导出", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using SaveFileDialog dialog = new()
        {
            Filter = "Excel 工作簿|*.xlsx",
            FileName = $"MeterTest_{task.EndedAt:yyyyMMdd_HHmmss}_{task.Id}.xlsx"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            new MeterTestResultExcelExporter(databaseService).Export(task.Id, dialog.FileName);
            MessageBox.Show("测试结果已导出。", "数据导出", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            LogMessage.Error("导出 MeterTest 测试结果失败", ex);
            MessageBox.Show($"数据导出失败：{ex.Message}", "数据导出", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>向结果表格添加固定宽度、不可排序的只读文本列。</summary>
    private static void AddColumn(DataGridView grid, string headerText, int width)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = headerText,
            MinimumWidth = width,
            ReadOnly = true,
            Resizable = DataGridViewTriState.False,
            Width = width
        });
    }

    /// <summary>有数据时选择首行并触发下一级结果加载。</summary>
    private static void SelectFirstRow(DataGridView grid)
    {
        if (grid.Rows.Count == 0)
            return;
        grid.ClearSelection();
        grid.Rows[0].Selected = true;
        grid.CurrentCell = grid.Rows[0].Cells[0];
    }

    /// <summary>按合格、不合格或未完成状态返回对应的结果文字颜色。</summary>
    private static Color GetResultColor(string result)
    {
        return result == "合格"
            ? Color.FromArgb(22, 101, 52)
            : result == "不合格" ? Color.Red : Color.FromArgb(180, 83, 9);
    }

    /// <summary>仅在数值和单位均存在时拼接结果展示文本。</summary>
    private static string AppendUnit(string value, string unit)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value + unit;
    }
}
