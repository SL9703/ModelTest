namespace ModelTest.MeterTest;

partial class MeterTestResultUserControl
{
    private System.ComponentModel.IContainer components = null!;
    private TableLayoutPanel rootLayout = null!;
    private FlowLayoutPanel toolbarPanel = null!;
    private Button btnRefresh = null!;
    private Button btnExport = null!;
    private GroupBox groupTasks = null!;
    private DataGridView dgvTasks = null!;
    private TableLayoutPanel detailLayout = null!;
    private GroupBox groupStations = null!;
    private DataGridView dgvStations = null!;
    private GroupBox groupDetails = null!;
    private DataGridView dgvDetails = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        rootLayout = new TableLayoutPanel();
        toolbarPanel = new FlowLayoutPanel();
        btnRefresh = new Button();
        btnExport = new Button();
        groupTasks = new GroupBox();
        dgvTasks = new DataGridView();
        detailLayout = new TableLayoutPanel();
        groupStations = new GroupBox();
        dgvStations = new DataGridView();
        groupDetails = new GroupBox();
        dgvDetails = new DataGridView();
        SuspendLayout();

        rootLayout.ColumnCount = 1;
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        rootLayout.Controls.Add(toolbarPanel, 0, 0);
        rootLayout.Controls.Add(groupTasks, 0, 1);
        rootLayout.Controls.Add(detailLayout, 0, 2);
        rootLayout.Dock = DockStyle.Fill;
        rootLayout.Padding = new Padding(8);
        rootLayout.RowCount = 3;
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 190F));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        toolbarPanel.Controls.Add(btnRefresh);
        toolbarPanel.Controls.Add(btnExport);
        toolbarPanel.Dock = DockStyle.Fill;
        toolbarPanel.Padding = new Padding(0, 3, 0, 0);
        toolbarPanel.WrapContents = false;

        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.Margin = new Padding(0, 0, 12, 0);
        btnRefresh.Size = new Size(150, 46);
        btnRefresh.Text = "刷新";
        btnRefresh.UseVisualStyleBackColor = true;

        btnExport.BackColor = Color.FromArgb(47, 112, 91);
        btnExport.FlatAppearance.BorderColor = Color.FromArgb(32, 78, 63);
        btnExport.FlatStyle = FlatStyle.Flat;
        btnExport.ForeColor = Color.White;
        btnExport.Margin = new Padding(0);
        btnExport.Size = new Size(170, 46);
        btnExport.Text = "数据导出";
        btnExport.UseVisualStyleBackColor = false;

        groupTasks.Controls.Add(dgvTasks);
        groupTasks.Dock = DockStyle.Fill;
        groupTasks.Padding = new Padding(8);
        groupTasks.Text = "测试任务";
        ConfigureReadOnlyGrid(dgvTasks);
        dgvTasks.Dock = DockStyle.Fill;

        detailLayout.ColumnCount = 2;
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
        detailLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
        detailLayout.Controls.Add(groupStations, 0, 0);
        detailLayout.Controls.Add(groupDetails, 1, 0);
        detailLayout.Dock = DockStyle.Fill;
        detailLayout.RowCount = 1;
        detailLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        groupStations.Controls.Add(dgvStations);
        groupStations.Dock = DockStyle.Fill;
        groupStations.Padding = new Padding(8);
        groupStations.Text = "测试工位电表对象";
        ConfigureReadOnlyGrid(dgvStations);
        dgvStations.Dock = DockStyle.Fill;

        groupDetails.Controls.Add(dgvDetails);
        groupDetails.Dock = DockStyle.Fill;
        groupDetails.Padding = new Padding(8);
        groupDetails.Text = "工位测试明细";
        ConfigureReadOnlyGrid(dgvDetails);
        dgvDetails.Dock = DockStyle.Fill;

        AutoScaleDimensions = new SizeF(13F, 28F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.FromArgb(232, 239, 236);
        Controls.Add(rootLayout);
        Name = "MeterTestResultUserControl";
        Size = new Size(1280, 640);
        ResumeLayout(false);
    }

    private static void ConfigureReadOnlyGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        grid.BackgroundColor = Color.White;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.RowTemplate.Height = 36;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }
}
