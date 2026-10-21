using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using KnowledgeApp.Application.DTOs;

namespace KnowledgeApp.Application.Services;

public static class GroupReportPdfTemplate
{
    public static void AddToDocument(
        Document document,
        GroupReportDto report,
        PdfFont regularFont,
        PdfFont boldFont,
        bool isBlankTemplate = false)
    {
        var compactLayout = report.StudentNames.Count > 30;
        var studentFontSize = report.StudentNames.Count > 40
            ? 6
            : report.StudentNames.Count > 30 ? 7 : report.StudentNames.Count > 20 ? 8 : 9;
        var cellPadding = compactLayout ? 0.15f : 0.5f;

        document.Add(new Paragraph("Отчет о проверке остаточных знаний")
            .SetFont(boldFont)
            .SetFontSize(12)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(5));

        var details = new Table(UnitValue.CreatePercentArray([0.65f, 9.35f]))
            .SetWidth(UnitValue.CreatePercentValue(100))
            .SetHorizontalAlignment(HorizontalAlignment.CENTER);
        AddCell(details, "№", TextAlignment.CENTER, boldFont, 9, 6, padding: cellPadding, regularFont: regularFont);
        AddCell(details, $"Учебный год: {report.AcademicYear}", TextAlignment.LEFT, boldFont, padding: cellPadding, regularFont: regularFont);
        AddCell(details, $"Код и наименование направления (специальности):{ValueSuffix(report.StudyProgramCode)}",
            TextAlignment.LEFT, boldFont, padding: cellPadding, regularFont: regularFont);
        AddCell(details, $"Наименование образовательной программы:{ValueSuffix(report.StudyProgramName)}",
            TextAlignment.LEFT, boldFont, padding: cellPadding, regularFont: regularFont);
        AddCell(details, $"Наименование дисциплины:{ValueSuffix(report.DisciplineName)}",
            TextAlignment.LEFT, boldFont, padding: cellPadding, regularFont: regularFont);
        AddCell(details,
            "ФИО педагогического работника, проводившего занятия в предыдущем семестре:",
            TextAlignment.LEFT, boldFont, padding: cellPadding, regularFont: regularFont);
        AddCell(details, $"Учебная группа №{ValueSuffix(report.GroupName)}",
            TextAlignment.LEFT, boldFont, padding: cellPadding, regularFont: regularFont);
        document.Add(details);

        var results = new Table(UnitValue.CreatePercentArray([0.65f, 4.35f, 2.5f, 2.5f]))
            .SetWidth(UnitValue.CreatePercentValue(100))
            .SetHorizontalAlignment(HorizontalAlignment.CENTER);
        AddCell(results, string.Empty, TextAlignment.CENTER, boldFont, studentFontSize,
            padding: cellPadding, regularFont: regularFont);
        AddCell(results, "ФИО обучающегося", TextAlignment.CENTER, boldFont, studentFontSize,
            padding: cellPadding, regularFont: regularFont);
        AddCell(results, "Результаты\nпромежуточной\nаттестации", TextAlignment.CENTER, boldFont,
            studentFontSize, padding: cellPadding, regularFont: regularFont);
        AddCell(results, "Результаты\nтестирования", TextAlignment.CENTER, boldFont,
            studentFontSize, padding: cellPadding, regularFont: regularFont);

        if (isBlankTemplate)
        {
            foreach (var row in new[] { "1", "2", "3", "…", "n" })
                AddBlankStudentRow(results, row, regularFont, studentFontSize, cellPadding);
        }
        else
        {
            for (var studentIndex = 0; studentIndex < report.StudentNames.Count; studentIndex++)
            {
                AddCell(results, (studentIndex + 1).ToString(), TextAlignment.CENTER, null,
                    studentFontSize, padding: cellPadding, regularFont: regularFont);
                AddCell(results, report.StudentNames[studentIndex], TextAlignment.LEFT, null,
                    studentFontSize, padding: cellPadding, regularFont: regularFont);
                AddCell(results, string.Empty, TextAlignment.CENTER, null, studentFontSize,
                    padding: cellPadding, regularFont: regularFont);
                AddCell(results, string.Empty, TextAlignment.CENTER, null, studentFontSize,
                    padding: cellPadding, regularFont: regularFont);
            }
        }

        AddSummary(results, "Всего в группе: __________ (чел.)", studentFontSize, cellPadding, regularFont);
        AddSummary(results, "Прошли тестирование: __________ (чел.)", studentFontSize, cellPadding, regularFont);
        AddSummary(results, "Не допущены к тестированию: __________ (чел.)", studentFontSize, cellPadding, regularFont);
        AddSummary(results, "Не явилось на тестирование: __________ (чел.)", studentFontSize, cellPadding, regularFont);
        document.Add(results);

        var signatures = new Table(UnitValue.CreatePercentArray([6.5f, 3.5f]))
            .SetWidth(UnitValue.CreatePercentValue(100))
            .SetHorizontalAlignment(HorizontalAlignment.CENTER)
            .SetMarginTop(compactLayout ? 2 : 6)
            .SetKeepTogether(true);
        AddSignature(signatures,
            $"Заведующий кафедрой {SignatureOrganization(report.DepartmentName)}",
            regularFont);
        AddSignature(signatures,
            $"Декан факультета / директор института {SignatureOrganization(report.FacultyName)}",
            regularFont);
        document.Add(signatures);
    }

    private static string ValueSuffix(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : $" {value}";

    private static string SignatureOrganization(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "(наименование полностью)" : value;

    private static void AddBlankStudentRow(
        Table table,
        string number,
        PdfFont font,
        float fontSize,
        float padding)
    {
        AddCell(table, number, TextAlignment.CENTER, null, fontSize, padding: padding, regularFont: font);
        AddCell(table, string.Empty, TextAlignment.LEFT, null, fontSize, padding: padding, regularFont: font);
        AddCell(table, string.Empty, TextAlignment.CENTER, null, fontSize, padding: padding, regularFont: font);
        AddCell(table, string.Empty, TextAlignment.CENTER, null, fontSize, padding: padding, regularFont: font);
    }

    private static void AddSummary(Table table, string text, float fontSize, float padding, PdfFont font)
    {
        table.AddCell(new Cell(1, 4)
            .Add(new Paragraph(text).SetFont(font).SetFontSize(fontSize))
            .SetBorder(new SolidBorder(0.5f))
            .SetPadding(padding));
    }

    private static void AddCell(
        Table table,
        string text,
        TextAlignment alignment,
        PdfFont? font = null,
        float fontSize = 9,
        int rowSpan = 1,
        int colSpan = 1,
        float padding = 0.5f,
        PdfFont? regularFont = null)
    {
        table.AddCell(new Cell(rowSpan, colSpan)
            .Add(new Paragraph(text)
                .SetFont(font ?? regularFont ?? throw new InvalidOperationException("Для ячейки отчёта не указан шрифт."))
                .SetFontSize(fontSize)
                .SetTextAlignment(alignment))
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetBorder(new SolidBorder(0.5f))
            .SetPadding(padding));
    }

    private static void AddSignature(Table table, string title, PdfFont font)
    {
        table.AddCell(new Cell()
            .Add(new Paragraph(title).SetFont(font).SetFontSize(9))
            .SetBorder(Border.NO_BORDER)
            .SetPadding(0));
        table.AddCell(new Cell()
            .Add(new Paragraph("________________\nФИО\nподпись")
                .SetFont(font)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.RIGHT))
            .SetBorder(Border.NO_BORDER)
            .SetPadding(0));
    }
}
