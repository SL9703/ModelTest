using System.Globalization;
using System.IO.Compression;
using System.Xml.Linq;

namespace ModelTest.MeterTest;

/// <summary>
/// 基于 MeterTestResultTemplate.xlsx 导出测试任务。
/// 模板的工作表名和数据起始行是固定变量，用户可在 Excel 中修改标题、字体、颜色、列宽和第2行数据样式。
/// </summary>
public sealed class MeterTestResultExcelExporter
{
    private const string TaskSheetName = "任务信息";
    private const string StationSheetName = "工位汇总";
    private const string DetailSheetName = "测试明细";
    private const string WorksheetProtectionPassword = "XCKJ-MeterTest-2026";
    private readonly MeterTestAccessDatabaseService databaseService;

    /// <summary>创建结果导出器，测试数据统一从本地数据库读取。</summary>
    public MeterTestResultExcelExporter(MeterTestAccessDatabaseService databaseService)
    {
        this.databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
    }

    /// <summary>将指定任务导出为真实 xlsx 工作簿。</summary>
    public void Export(long taskId, string outputPath)
    {
        MeterTestResultTaskData task = databaseService.LoadTestResultTasks()
            .FirstOrDefault(item => item.Id == taskId)
            ?? throw new InvalidOperationException($"未找到测试任务：{taskId}。");
        IReadOnlyList<MeterTestResultStationData> stations = databaseService.LoadTestResultStations(taskId);
        IReadOnlyList<MeterTestResultDetailData> details = databaseService.LoadTestResultDetails(taskId);

        string templatePath = ResolveTemplatePath();
        if (!File.Exists(templatePath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(templatePath)!);
            CreateDefaultTemplate(templatePath);
        }

        string? outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        File.Copy(templatePath, outputPath, overwrite: true);
        using ZipArchive archive = ZipFile.Open(outputPath, ZipArchiveMode.Update);
        Dictionary<string, string> sheetPaths = ResolveWorksheetPaths(archive);
        UpdateTaskSheet(archive, sheetPaths[TaskSheetName], task);
        UpdateTableSheet(
            archive,
            sheetPaths[StationSheetName],
            2,
            stations.Select(CreateStationRow).ToList(),
            14);
        UpdateTableSheet(
            archive,
            sheetPaths[DetailSheetName],
            2,
            details.Select(CreateDetailRow).ToList(),
            13);
    }

    /// <summary>解析运行目录下可由用户调整格式的 MeterTest Excel 模板路径。</summary>
    public static string ResolveTemplatePath()
    {
        return Path.Combine(
            AppContext.BaseDirectory,
            "MeterTest",
            "templates",
            "MeterTestResultTemplate.xlsx");
    }

    /// <summary>将任务基本信息写入模板任务摘要工作表。</summary>
    private static void UpdateTaskSheet(
        ZipArchive archive,
        string worksheetPath,
        MeterTestResultTaskData task)
    {
        XDocument document = ReadXmlEntry(archive, worksheetPath);
        SetCellValue(document, "B2", task.Id, numeric: true);
        SetCellValue(document, "B3", task.RunId, numeric: false);
        SetCellValue(document, "B4", task.SchemeName, numeric: false);
        SetCellValue(document, "B5", task.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"), numeric: false);
        SetCellValue(document, "B6", task.EndedAt.ToString("yyyy-MM-dd HH:mm:ss"), numeric: false);
        SetCellValue(document, "B7", task.Status, numeric: false);
        SetCellValue(document, "B8", task.SaveMode, numeric: false);
        SetCellValue(document, "B9", task.ResultSummary, numeric: false);
        ProtectWorksheet(document);
        ReplaceXmlEntry(archive, worksheetPath, document);
    }

    /// <summary>用数据库行集合更新指定明细工作表，同时保留模板样式。</summary>
    private static void UpdateTableSheet(
        ZipArchive archive,
        string worksheetPath,
        int dataStartRow,
        IReadOnlyList<IReadOnlyList<ExcelValue>> rows,
        int columnCount)
    {
        XDocument document = ReadXmlEntry(archive, worksheetPath);
        XNamespace ns = document.Root!.Name.Namespace;
        XElement sheetData = document.Root.Element(ns + "sheetData")
            ?? throw new InvalidDataException($"模板工作表缺少 sheetData：{worksheetPath}。");
        XElement? styleRow = sheetData.Elements(ns + "row")
            .FirstOrDefault(row => (int?)row.Attribute("r") == dataStartRow);
        Dictionary<int, string?> styles = ReadColumnStyles(styleRow, ns);

        foreach (XElement row in sheetData.Elements(ns + "row")
                     .Where(row => ((int?)row.Attribute("r") ?? 0) >= dataStartRow)
                     .ToList())
        {
            row.Remove();
        }

        int rowNumber = dataStartRow;
        foreach (IReadOnlyList<ExcelValue> values in rows)
        {
            XElement row = new(ns + "row", new XAttribute("r", rowNumber));
            for (int columnIndex = 1; columnIndex <= values.Count; columnIndex++)
            {
                row.Add(CreateCell(
                    ns,
                    GetColumnName(columnIndex) + rowNumber,
                    values[columnIndex - 1],
                    styles.TryGetValue(columnIndex, out string? styleId) ? styleId : null));
            }

            sheetData.Add(row);
            rowNumber++;
        }

        XElement? dimension = document.Root.Element(ns + "dimension");
        if (dimension is not null)
        {
            int lastRow = Math.Max(1, rowNumber - 1);
            dimension.SetAttributeValue("ref", $"A1:{GetColumnName(columnCount)}{lastRow}");
        }

        ProtectWorksheet(document);
        ReplaceXmlEntry(archive, worksheetPath, document);
    }

    /// <summary>
    /// 对导出工作表启用密码保护。
    /// 所有单元格保持 Excel 默认的 Locked 状态，用户可以选中和复制，但不能修改数据、格式或行列结构。
    /// </summary>
    private static void ProtectWorksheet(XDocument document)
    {
        XNamespace ns = document.Root!.Name.Namespace;
        document.Root.Elements(ns + "sheetProtection").Remove();
        XElement protection = new(
            ns + "sheetProtection",
            new XAttribute("password", ComputeLegacyWorksheetPasswordHash(WorksheetProtectionPassword)),
            new XAttribute("sheet", 1),
            new XAttribute("objects", 1),
            new XAttribute("scenarios", 1),
            new XAttribute("formatCells", 1),
            new XAttribute("formatColumns", 1),
            new XAttribute("formatRows", 1),
            new XAttribute("insertColumns", 1),
            new XAttribute("insertRows", 1),
            new XAttribute("insertHyperlinks", 1),
            new XAttribute("deleteColumns", 1),
            new XAttribute("deleteRows", 1),
            new XAttribute("sort", 1),
            new XAttribute("autoFilter", 1),
            new XAttribute("pivotTables", 1));
        XElement sheetData = document.Root.Element(ns + "sheetData")
            ?? throw new InvalidDataException("导出工作表缺少 sheetData，无法启用数据保护。");
        sheetData.AddAfterSelf(protection);
    }

    /// <summary>
    /// 计算 OOXML 工作表保护所使用的 Excel 兼容密码哈希。
    /// 该保护用于防止操作员误改检定结果，不用作密码学加密。
    /// </summary>
    private static string ComputeLegacyWorksheetPasswordHash(string password)
    {
        int hash = 0;
        for (int index = password.Length - 1; index >= 0; index--)
        {
            int highBit = (hash >> 14) & 1;
            hash = ((hash << 1) & 0x7FFF) | highBit;
            hash ^= password[index];
        }

        hash ^= password.Length;
        hash ^= 0xCE4B;
        return hash.ToString("X4", CultureInfo.InvariantCulture);
    }

    /// <summary>将工位资产快照转换为导出表的一行单元格值。</summary>
    private static IReadOnlyList<ExcelValue> CreateStationRow(MeterTestResultStationData station)
    {
        return new[]
        {
            ExcelValue.Number(station.StationNo),
            ExcelValue.Text(station.Barcode),
            ExcelValue.Text(station.MeterAddress),
            ExcelValue.Text(station.MeterType),
            ExcelValue.Text(station.AccessMode),
            ExcelValue.Text(station.Voltage),
            ExcelValue.Text(station.BasicCurrent),
            ExcelValue.Text(station.CurrentSpecification),
            ExcelValue.Text(station.ActiveClass),
            ExcelValue.Text(station.ActiveConstant),
            ExcelValue.Text(station.ReactiveClass),
            ExcelValue.Text(station.ReactiveConstant),
            ExcelValue.Text(station.OverallResult),
            ExcelValue.Text(station.CompletedAt.ToString("yyyy-MM-dd HH:mm:ss"))
        };
    }

    /// <summary>将测试结果明细转换为导出表的一行单元格值。</summary>
    private static IReadOnlyList<ExcelValue> CreateDetailRow(MeterTestResultDetailData detail)
    {
        return new[]
        {
            ExcelValue.Number(detail.StationNo),
            ExcelValue.Text(detail.TestItemName),
            ExcelValue.Text(detail.TestSubItemName),
            ExcelValue.Text(detail.Result),
            ExcelValue.Text(detail.ResultTimeText),
            ExcelValue.Text(detail.MeasurementName),
            detail.SequenceNo == 0 ? ExcelValue.Text(string.Empty) : ExcelValue.Number(detail.SequenceNo),
            ExcelValue.Text(detail.ValueText),
            detail.NumericValue.HasValue ? ExcelValue.Number(detail.NumericValue.Value) : ExcelValue.Text(string.Empty),
            ExcelValue.Text(detail.Unit),
            detail.AverageValue.HasValue ? ExcelValue.Number(detail.AverageValue.Value) : ExcelValue.Text(string.Empty),
            ExcelValue.Text(detail.LimitText),
            ExcelValue.Text(detail.Message)
        };
    }

    private static Dictionary<string, string> ResolveWorksheetPaths(ZipArchive archive)
    {
        XDocument workbook = ReadXmlEntry(archive, "xl/workbook.xml");
        XDocument relationships = ReadXmlEntry(archive, "xl/_rels/workbook.xml.rels");
        XNamespace workbookNs = workbook.Root!.Name.Namespace;
        XNamespace relationshipNs = relationships.Root!.Name.Namespace;
        XNamespace rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        Dictionary<string, string> targets = relationships.Root
            .Elements(relationshipNs + "Relationship")
            .ToDictionary(
                element => (string)element.Attribute("Id")!,
                element => (string)element.Attribute("Target")!);
        Dictionary<string, string> paths = new(StringComparer.OrdinalIgnoreCase);
        foreach (XElement sheet in workbook.Root.Element(workbookNs + "sheets")!.Elements(workbookNs + "sheet"))
        {
            string name = (string)sheet.Attribute("name")!;
            string relationshipId = (string)sheet.Attribute(rel + "id")!;
            string target = targets[relationshipId].Replace('\\', '/');
            paths[name] = target.StartsWith("/", StringComparison.Ordinal)
                ? target.TrimStart('/')
                : "xl/" + target.TrimStart('/');
        }

        foreach (string requiredSheet in new[] { TaskSheetName, StationSheetName, DetailSheetName })
        {
            if (!paths.ContainsKey(requiredSheet))
            {
                throw new InvalidDataException($"导出模板缺少工作表：{requiredSheet}。");
            }
        }

        return paths;
    }

    /// <summary>按单元格引用写入文本或数值，并保持模板中的原有样式编号。</summary>
    private static void SetCellValue(XDocument document, string cellReference, object? value, bool numeric)
    {
        XNamespace ns = document.Root!.Name.Namespace;
        XElement sheetData = document.Root.Element(ns + "sheetData")!;
        int rowNumber = int.Parse(new string(cellReference.Where(char.IsDigit).ToArray()), CultureInfo.InvariantCulture);
        XElement row = sheetData.Elements(ns + "row").FirstOrDefault(item => (int?)item.Attribute("r") == rowNumber)
            ?? new XElement(ns + "row", new XAttribute("r", rowNumber));
        if (row.Parent is null)
        {
            sheetData.Add(row);
        }

        XElement? existingCell = row.Elements(ns + "c")
            .FirstOrDefault(cell => string.Equals((string?)cell.Attribute("r"), cellReference, StringComparison.OrdinalIgnoreCase));
        string? styleId = (string?)existingCell?.Attribute("s");
        existingCell?.ReplaceWith(CreateCell(
            ns,
            cellReference,
            numeric ? ExcelValue.Number(Convert.ToDouble(value, CultureInfo.InvariantCulture)) : ExcelValue.Text(Convert.ToString(value, CultureInfo.InvariantCulture)),
            styleId));
        if (existingCell is null)
        {
            row.Add(CreateCell(
                ns,
                cellReference,
                numeric
                    ? ExcelValue.Number(Convert.ToDouble(value, CultureInfo.InvariantCulture))
                    : ExcelValue.Text(Convert.ToString(value, CultureInfo.InvariantCulture)),
                styleId));
        }
    }

    /// <summary>创建一个 OpenXML 单元格节点并应用指定样式。</summary>
    private static XElement CreateCell(XNamespace ns, string reference, ExcelValue value, string? styleId)
    {
        XElement cell = new(ns + "c", new XAttribute("r", reference));
        if (!string.IsNullOrWhiteSpace(styleId))
        {
            cell.SetAttributeValue("s", styleId);
        }

        if (value.IsNumeric)
        {
            cell.Add(new XElement(ns + "v", value.Value));
        }
        else
        {
            cell.SetAttributeValue("t", "inlineStr");
            cell.Add(new XElement(ns + "is", new XElement(ns + "t", value.Value)));
        }

        return cell;
    }

    private static Dictionary<int, string?> ReadColumnStyles(XElement? row, XNamespace ns)
    {
        Dictionary<int, string?> styles = new();
        if (row is null)
            return styles;
        foreach (XElement cell in row.Elements(ns + "c"))
        {
            string reference = (string?)cell.Attribute("r") ?? string.Empty;
            int column = GetColumnIndex(new string(reference.TakeWhile(char.IsLetter).ToArray()));
            styles[column] = (string?)cell.Attribute("s");
        }

        return styles;
    }

    /// <summary>从 xlsx ZIP 容器读取指定 XML 部件。</summary>
    private static XDocument ReadXmlEntry(ZipArchive archive, string path)
    {
        ZipArchiveEntry entry = archive.GetEntry(path)
            ?? throw new InvalidDataException($"Excel模板缺少文件：{path}。");
        using Stream stream = entry.Open();
        return XDocument.Load(stream, LoadOptions.PreserveWhitespace);
    }

    /// <summary>用新 XML 内容替换 xlsx ZIP 容器中的指定部件。</summary>
    private static void ReplaceXmlEntry(ZipArchive archive, string path, XDocument document)
    {
        archive.GetEntry(path)?.Delete();
        ZipArchiveEntry replacement = archive.CreateEntry(path, CompressionLevel.Optimal);
        using Stream stream = replacement.Open();
        document.Save(stream, SaveOptions.DisableFormatting);
    }

    /// <summary>将从 1 开始的列序号转换为 Excel A、B、AA 形式的列名。</summary>
    private static string GetColumnName(int columnNumber)
    {
        string name = string.Empty;
        while (columnNumber > 0)
        {
            columnNumber--;
            name = (char)('A' + columnNumber % 26) + name;
            columnNumber /= 26;
        }

        return name;
    }

    /// <summary>将 Excel 列名转换为从 1 开始的列序号。</summary>
    private static int GetColumnIndex(string columnName)
    {
        int index = 0;
        foreach (char character in columnName.ToUpperInvariant())
        {
            index = index * 26 + character - 'A' + 1;
        }

        return index;
    }

    /// <summary>
    /// 发布包丢失外部模板时的保底：生成包含固定工作表和变量单元格的最小 xlsx。
    /// </summary>
    private static void CreateDefaultTemplate(string path)
    {
        MeterTestResultTemplateFactory.Create(path);
    }

    /// <summary>导出单元格的规范化文本值及数值类型标记。</summary>
    private sealed record ExcelValue(string Value, bool IsNumeric)
    {
        /// <summary>创建按文本写入的单元格值。</summary>
        public static ExcelValue Text(string? value) => new(value ?? string.Empty, false);

        /// <summary>创建使用不受区域设置影响格式写入的数值单元格。</summary>
        public static ExcelValue Number(double value) => new(value.ToString("0.###############", CultureInfo.InvariantCulture), true);
    }
}
