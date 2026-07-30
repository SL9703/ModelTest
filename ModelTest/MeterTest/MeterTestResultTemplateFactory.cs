using System.IO.Compression;
using System.Security;
using System.Text;

namespace ModelTest.MeterTest;

/// <summary>
/// MeterTest Excel 模板保底工厂。
/// 正常发布使用 templates 目录中的可编辑模板，该工厂只在模板缺失时生成同结构工作簿。
/// </summary>
public static class MeterTestResultTemplateFactory
{
    /// <summary>创建包含任务、工位和结果明细工作表的默认 xlsx 导出模板。</summary>
    public static void Create(string outputPath)
    {
        string? directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using FileStream stream = new(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using ZipArchive archive = new(stream, ZipArchiveMode.Create);
        WriteEntry(archive, "[Content_Types].xml", ContentTypesXml);
        WriteEntry(archive, "_rels/.rels", RootRelationshipsXml);
        WriteEntry(archive, "docProps/app.xml", AppPropertiesXml);
        WriteEntry(archive, "docProps/core.xml", CorePropertiesXml);
        WriteEntry(archive, "xl/workbook.xml", WorkbookXml);
        WriteEntry(archive, "xl/_rels/workbook.xml.rels", WorkbookRelationshipsXml);
        WriteEntry(archive, "xl/styles.xml", StylesXml);
        WriteEntry(archive, "xl/worksheets/sheet1.xml", CreateTaskSheetXml());
        WriteEntry(archive, "xl/worksheets/sheet2.xml", CreateTableSheetXml(StationHeaders, 14));
        WriteEntry(archive, "xl/worksheets/sheet3.xml", CreateTableSheetXml(DetailHeaders, 13));
    }

    /// <summary>生成任务摘要工作表的初始 OpenXML 内容。</summary>
    private static string CreateTaskSheetXml()
    {
        string[] labels = { "任务ID", "RunId", "方案名称", "开始时间", "结束时间", "任务状态", "保存方式", "结果汇总" };
        StringBuilder rows = new();
        rows.Append(CreateRow(1, new[] { "MeterTest 测试任务信息", string.Empty }, 2));
        for (int index = 0; index < labels.Length; index++)
        {
            rows.Append(CreateRow(index + 2, new[] { labels[index], $"${{{labels[index]}}}" }, 1));
        }

        return WorksheetEnvelope(rows.ToString(), "A1:B9", new[] { 20d, 76d });
    }

    /// <summary>按列标题生成表格型工作表的初始 OpenXML 内容。</summary>
    private static string CreateTableSheetXml(IReadOnlyList<string> headers, int columnCount)
    {
        string headerRow = CreateRow(1, headers, 2);
        string markerRow = CreateRow(2, Enumerable.Repeat("${DATA}", columnCount).ToArray(), 3);
        return WorksheetEnvelope(
            headerRow + markerRow,
            $"A1:{GetColumnName(columnCount)}2",
            Enumerable.Range(0, columnCount).Select(index => index == columnCount - 1 ? 48d : 18d).ToArray());
    }

    /// <summary>将行数据、有效区域和列宽包装为完整 worksheet XML。</summary>
    private static string WorksheetEnvelope(string rows, string dimension, IReadOnlyList<double> widths)
    {
        StringBuilder columns = new();
        for (int index = 0; index < widths.Count; index++)
        {
            columns.Append($"<col min=\"{index + 1}\" max=\"{index + 1}\" width=\"{widths[index]}\" customWidth=\"1\"/>");
        }

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
              <dimension ref="{dimension}"/>
              <sheetViews><sheetView workbookViewId="0"/></sheetViews>
              <sheetFormatPr defaultRowHeight="22"/>
              <cols>{columns}</cols>
              <sheetData>{rows}</sheetData>
            </worksheet>
            """;
    }

    /// <summary>生成指定行号、文本值和样式编号的 OpenXML 行节点。</summary>
    private static string CreateRow(int rowNumber, IReadOnlyList<string> values, int styleId)
    {
        StringBuilder cells = new();
        for (int index = 0; index < values.Count; index++)
        {
            string reference = GetColumnName(index + 1) + rowNumber;
            string encoded = SecurityElement.Escape(values[index]) ?? string.Empty;
            cells.Append($"<c r=\"{reference}\" s=\"{styleId}\" t=\"inlineStr\"><is><t>{encoded}</t></is></c>");
        }

        return $"<row r=\"{rowNumber}\" ht=\"24\" customHeight=\"1\">{cells}</row>";
    }

    /// <summary>向 xlsx ZIP 容器写入 UTF-8 XML 部件。</summary>
    private static void WriteEntry(ZipArchive archive, string path, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using StreamWriter writer = new(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    /// <summary>将从 1 开始的列序号转换为 Excel 列名。</summary>
    private static string GetColumnName(int number)
    {
        string result = string.Empty;
        while (number > 0)
        {
            number--;
            result = (char)('A' + number % 26) + result;
            number /= 26;
        }

        return result;
    }

    private static readonly string[] StationHeaders =
    {
        "工位", "条形码", "电表地址", "电表类型", "接入方式", "额定电压", "基本电流",
        "电流规格", "有功等级", "有功常数", "无功等级", "无功常数", "总结论", "完成时间"
    };

    private static readonly string[] DetailHeaders =
    {
        "工位", "测试项", "测试小项", "结果", "时间", "数值名称", "序号", "显示值", "数值", "单位", "平均值", "标准/限值", "说明"
    };

    private const string ContentTypesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/worksheets/sheet3.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
          <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
          <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
        </Types>
        """;

    private const string RootRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
          <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
        </Relationships>
        """;

    private const string WorkbookXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
          <sheets>
            <sheet name="任务信息" sheetId="1" r:id="rId1"/>
            <sheet name="工位汇总" sheetId="2" r:id="rId2"/>
            <sheet name="测试明细" sheetId="3" r:id="rId3"/>
          </sheets>
        </workbook>
        """;

    private const string WorkbookRelationshipsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
          <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet3.xml"/>
          <Relationship Id="rId4" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    private const string StylesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="3">
            <font><sz val="11"/><name val="Microsoft YaHei"/></font>
            <font><b/><sz val="14"/><color rgb="FFFFFFFF"/><name val="Microsoft YaHei"/></font>
            <font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Microsoft YaHei"/></font>
          </fonts>
          <fills count="4">
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FF2F705B"/><bgColor indexed="64"/></patternFill></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FFE8EFEC"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders count="2">
            <border><left/><right/><top/><bottom/><diagonal/></border>
            <border><left style="thin"><color rgb="FF9CA3AF"/></left><right style="thin"><color rgb="FF9CA3AF"/></right><top style="thin"><color rgb="FF9CA3AF"/></top><bottom style="thin"><color rgb="FF9CA3AF"/></bottom><diagonal/></border>
          </borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="4">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="0" fillId="3" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center"/></xf>
            <xf numFmtId="0" fontId="2" fillId="2" borderId="1" xfId="0" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf>
            <xf numFmtId="0" fontId="0" fillId="0" borderId="1" xfId="0" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;

    private const string AppPropertiesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes"><Application>ModelTest</Application></Properties>
        """;

    private const string CorePropertiesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><dc:creator>ModelTest</dc:creator><dc:title>MeterTest Result Template</dc:title></cp:coreProperties>
        """;
}
