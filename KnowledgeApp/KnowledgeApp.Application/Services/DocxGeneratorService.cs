using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Application.Interfaces;

namespace KnowledgeApp.Application.Services;

public sealed class DocxGeneratorService : IDocxGeneratorService
{
    private const int DetailRows = 6;
    private const int SummaryRows = 4;

    public byte[] GenerateBlankReportDocx() => ReadTemplate();

    public byte[] GenerateGroupReportDocx(GroupReportDto report)
    {
        using var stream = new MemoryStream();
        stream.Write(ReadTemplate());
        stream.Position = 0;
        using (var document = WordprocessingDocument.Open(stream, true))
        {
            var body = document.MainDocumentPart?.Document.Body
                ?? throw new InvalidDataException("В шаблоне отчёта отсутствует основное содержимое DOCX.");
            var tables = body.Elements<Table>().ToList();
            if (tables.Count < 2)
                throw new InvalidDataException("В шаблоне отчёта отсутствуют таблицы данных или подписей.");

            var reportTable = tables[0];
            var rows = reportTable.Elements<TableRow>().ToList();
            if (rows.Count < DetailRows + 1 + SummaryRows)
                throw new InvalidDataException("Структура таблицы отчёта не соответствует шаблону.");

            var details = new[]
            {
                $"Учебный год: {report.AcademicYear}",
                $"Код и наименование направления (специальности): {report.StudyProgramCode}",
                $"Наименование образовательной программы: {report.StudyProgramName}",
                $"Наименование дисциплины: {report.DisciplineName}",
                null,
                $"Учебная группа № {report.GroupName}"
            };

            for (var index = 0; index < DetailRows; index++)
            {
                if (details[index] is not null)
                    SetCellText(rows[index].Elements<TableCell>().Last(), details[index]!);
            }

            var studentStartIndex = DetailRows + 1;
            var summaryStartIndex = rows.Count - SummaryRows;
            var studentRows = rows.Skip(studentStartIndex).Take(summaryStartIndex - studentStartIndex).ToList();
            var firstSummaryRow = rows[summaryStartIndex];
            while (studentRows.Count < report.StudentNames.Count)
            {
                var additionalRow = (TableRow)studentRows[^1].CloneNode(true);
                reportTable.InsertBefore(additionalRow, firstSummaryRow);
                studentRows.Add(additionalRow);
            }

            while (studentRows.Count > report.StudentNames.Count)
            {
                studentRows[^1].Remove();
                studentRows.RemoveAt(studentRows.Count - 1);
            }

            for (var index = 0; index < studentRows.Count; index++)
            {
                var cells = studentRows[index].Elements<TableCell>().ToList();
                if (cells.Count != 4)
                    throw new InvalidDataException("Строка студента в шаблоне должна содержать 4 ячейки.");

                SetCellText(cells[0], (index + 1).ToString());
                SetCellText(cells[1], report.StudentNames[index]);
            }

            var summaryRow = rows[summaryStartIndex];
            SetCellText(summaryRow.Elements<TableCell>().First(),
                $"Всего в группе: {report.StudentNames.Count} (чел.)");

            var signatures = tables[1].Elements<TableRow>().ToList();
            if (signatures.Count >= 3)
            {
                SetCellText(signatures[0].Elements<TableCell>().First(),
                    $"Заведующий кафедрой {report.DepartmentName}",
                    italic: true);
                SetCellText(signatures[2].Elements<TableCell>().First(),
                    $"Декан факультета/Директор института {report.FacultyName}",
                    italic: true);
            }

            document.MainDocumentPart!.Document.Save();
        }

        return stream.ToArray();
    }

    private static byte[] ReadTemplate()
    {
        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "GroupReportTemplate.docx");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException("Не найден шаблон отчёта DOCX.", templatePath);

        using var stream = new MemoryStream(File.ReadAllBytes(templatePath));
        using (var document = WordprocessingDocument.Open(stream, true))
        {
            var body = document.MainDocumentPart?.Document.Body
                ?? throw new InvalidDataException("В шаблоне отчёта отсутствует основное содержимое DOCX.");
            var reportTable = body.Elements<Table>().FirstOrDefault()
                ?? throw new InvalidDataException("В шаблоне отчёта отсутствует таблица данных.");
            NormalizeHeaderLayout(reportTable);
            document.MainDocumentPart!.Document.Save();
        }

        return stream.ToArray();
    }

    private static void NormalizeHeaderLayout(Table reportTable)
    {
        var rows = reportTable.Elements<TableRow>().Take(7).ToList();
        if (rows.Count < 7)
            throw new InvalidDataException("В шаблоне отчёта отсутствуют строки заголовка.");

        for (var index = 0; index < 5; index++)
        {
            var cells = rows[index].Elements<TableCell>().ToList();
            if (cells.Count != 2)
                throw new InvalidDataException("Строка заголовка отчёта не соответствует шаблону.");

            cells[0].Remove();
            var fullWidthCell = rows[index].Elements<TableCell>().Single();
            var properties = fullWidthCell.GetFirstChild<TableCellProperties>()
                ?? fullWidthCell.PrependChild(new TableCellProperties());
            var gridSpan = properties.GetFirstChild<GridSpan>();
            if (gridSpan is null)
                properties.Append(new GridSpan { Val = 4 });
            else
                gridSpan.Val = 4;
        }

        var numberCell = rows[5].Elements<TableCell>().FirstOrDefault()
            ?? throw new InvalidDataException("В шаблоне отчёта отсутствует ячейка номера.");
        var numberProperties = numberCell.GetFirstChild<TableCellProperties>()
            ?? numberCell.PrependChild(new TableCellProperties());
        numberProperties.GetFirstChild<VerticalMerge>()?.Remove();
        var verticalAlignment = numberProperties.GetFirstChild<TableCellVerticalAlignment>();
        if (verticalAlignment is null)
            numberProperties.Append(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Bottom });
        else
            verticalAlignment.Val = TableVerticalAlignmentValues.Bottom;

        foreach (var runProperties in numberCell.Descendants<RunProperties>())
        {
            runProperties.GetFirstChild<FontSize>()?.Remove();
            runProperties.GetFirstChild<FontSizeComplexScript>()?.Remove();
            runProperties.Append(new FontSize { Val = "16" });
            runProperties.Append(new FontSizeComplexScript { Val = "16" });
        }

        var tableHeaderNumberCell = rows[6].Elements<TableCell>().FirstOrDefault();
        tableHeaderNumberCell?.GetFirstChild<TableCellProperties>()?
            .GetFirstChild<VerticalMerge>()?.Remove();
    }

    private static void SetCellText(TableCell cell, string text, bool italic = false)
    {
        var texts = cell.Descendants<Text>().ToList();
        if (texts.Count == 0)
        {
            var paragraph = cell.Elements<Paragraph>().FirstOrDefault();
            if (paragraph is null)
            {
                paragraph = new Paragraph();
                cell.Append(paragraph);
            }

            paragraph.Append(CreateRun(text, italic));
            return;
        }

        texts[0].Text = text;
        texts[0].Space = SpaceProcessingModeValues.Preserve;
        var runProperties = texts[0].Parent?.GetFirstChild<RunProperties>();
        if (italic && runProperties is not null && !runProperties.Elements<Italic>().Any())
            runProperties.Append(new Italic());
        foreach (var extraText in texts.Skip(1))
            extraText.Text = string.Empty;
    }

    private static Run CreateRun(string text, bool italic)
    {
        var runProperties = new RunProperties();
        if (italic)
            runProperties.Append(new Italic());

        return new Run(
            runProperties,
            new Text(text) { Space = SpaceProcessingModeValues.Preserve });
    }
}
