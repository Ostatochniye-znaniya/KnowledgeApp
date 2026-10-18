using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Application.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using Microsoft.Extensions.Logging;
using iText.IO.Font;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace KnowledgeApp.Application.Services
{
    public class PdfGeneratorService : IPdfGeneratorService
    {
        private readonly ILogger<PdfGeneratorService> _logger;
        private PdfFont _timesNewRoman;
        private PdfFont _timesNewRomanBold;

        public PdfGeneratorService(ILogger<PdfGeneratorService> logger)
        {
            _logger = logger;
            (_timesNewRoman, _timesNewRomanBold) = LoadFonts();
        }

        private (PdfFont regular, PdfFont bold) LoadFonts()
        {
            PdfFont? regular = null;
            PdfFont? bold = null;

            try
            {
                string fontsDir = System.IO.Path.Combine(AppContext.BaseDirectory, "Fonts");
                string regularPath = System.IO.Path.Combine(fontsDir, "times.ttf");
                string boldPath = System.IO.Path.Combine(fontsDir, "timesbd.ttf");

                if (File.Exists(regularPath))
                {
                    regular = PdfFontFactory.CreateFont(regularPath, PdfEncodings.IDENTITY_H, 
                        PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                }

                if (File.Exists(boldPath))
                {
                    bold = PdfFontFactory.CreateFont(boldPath, PdfEncodings.IDENTITY_H, 
                        PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
                }

                if (regular == null)
                {
                    throw new FileNotFoundException("Не найден шрифт с поддержкой кириллицы!");
                }

                if (bold == null) bold = regular;

                _logger.LogInformation("Загружен шрифт: {FontName}", 
                    regular.GetFontProgram().GetFontNames().GetFontName());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка загрузки шрифтов");
                throw;
            }

            return (regular, bold);
        }

        private Cell CreateCell(string text)
        {
            return new Cell()
                .Add(new Paragraph(text ?? "-")
                    .SetFont(_timesNewRoman)
                    .SetFontSize(9)
                    .SetTextAlignment(TextAlignment.CENTER))
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(new SolidBorder(0.5f))
                .SetPadding(2);
        }

        private Cell CreateDateTimeCell(TestingScheduleDto item)
        {
            var date = item.Date == DateTime.MinValue ? "-" : item.Date.ToString("dd.MM.yyyy");
            var value = $"{date}\n{item.Time.ToString(@"hh\:mm")}";

            return CreateCell(value);
        }

        private Cell CreateLocationCell(TestingScheduleDto item)
        {
            var paragraph = new Paragraph()
                .SetFont(_timesNewRoman)
                .SetFontSize(9)
                .SetTextAlignment(TextAlignment.CENTER);

            if (!string.IsNullOrWhiteSpace(item.Room))
            {
                paragraph.Add(item.Room);
            }
            else if (Uri.TryCreate(item.LmsUrl, UriKind.Absolute, out var lmsUri) &&
                     (lmsUri.Scheme == Uri.UriSchemeHttp || lmsUri.Scheme == Uri.UriSchemeHttps))
            {
                paragraph.Add(new Link("LMS", PdfAction.CreateURI(lmsUri.AbsoluteUri))
                    .SetFont(_timesNewRoman)
                    .SetFontSize(9)
                    .SetUnderline());
            }
            else
            {
                paragraph.Add("LMS");
            }

            return new Cell()
                .Add(paragraph)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(new SolidBorder(0.5f))
                .SetPadding(2);
        }

        public byte[] GenerateTestingSchedulePdf(List<TestingScheduleDto> schedule, string? facultyName = null, int facultyId = 0, string? semesterPeriod = null, int semesterId = 0)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var writer = new PdfWriter(ms);
                    var pdf = new PdfDocument(writer);

                    var pageSize = PageSize.A4.Rotate();
                    pdf.SetDefaultPageSize(pageSize);

                    var document = new Document(pdf, pageSize);
                    // Увеличиваем левый отступ документа
                    document.SetMargins(25, 25, 25, 45);

                    string facultyTitle;
                    if (facultyId == 0 || string.IsNullOrEmpty(facultyName) || facultyName == "Все факультеты")
                    {
                        facultyTitle = "Все факультеты";
                    }
                    else
                    {
                        facultyTitle = facultyName.Trim();
                    }

                    Paragraph facultyPara = new Paragraph(facultyTitle)
                        .SetFontSize(17)
                        .SetFont(_timesNewRomanBold)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(0)
                        .SetMarginTop(0);
                    facultyPara.SetMultipliedLeading(1.0f);
                    document.Add(facultyPara);

                    string titleText;
                    if (semesterId == 0 || string.IsNullOrEmpty(semesterPeriod) || semesterPeriod == "Все семестры")
                    {
                        titleText = "График проверки остаточных знаний";
                    }
                    else
                    {
                        titleText = $"График проверки остаточных знаний {semesterPeriod}";
                    }

                    Paragraph titlePara = new Paragraph(titleText)
                        .SetFontSize(17)
                        .SetFont(_timesNewRomanBold)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(15);
                    titlePara.SetMultipliedLeading(1.0f);
                    document.Add(titlePara);

                    float[] columnProportions = { 0.4f, 0.9f, 2.8f, 2.7f, 1.7f, 1.2f, 2.0f, 1.2f, 1.1f };
                    Table table = new Table(UnitValue.CreatePercentArray(columnProportions));
                    table.SetWidth(UnitValue.CreatePercentValue(100));
                    // Увеличиваем левый отступ таблицы
                    table.SetMarginLeft(30);

                    string[] headers = {
                        "№", "Группа", "Наименование профиля подготовки",
                        "Наименование дисциплины", "Институт / школа", "Кафедра", "ФИО ППС",
                        "Дата и время", "Аудитория"
                    };

                    foreach (string header in headers)
                    {
                        Cell headerCell = new Cell()
                            .Add(new Paragraph(header)
                                .SetFont(_timesNewRomanBold)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.CENTER))
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(2);
                        table.AddHeaderCell(headerCell);
                    }

                    if (schedule != null && schedule.Any())
                    {
                        var sortedSchedule = schedule
                            .OrderBy(x => x.GroupName)
                            .ThenBy(x => x.Date)
                            .ThenBy(x => x.Time)
                            .ToList();

                        int currentNumber = 1;
                        int i = 0;

                        while (i < sortedSchedule.Count)
                        {
                            var currentGroupName = sortedSchedule[i].GroupName;

                            int groupStart = i;
                            int groupEnd = i;
                            while (groupEnd + 1 < sortedSchedule.Count &&
                                   sortedSchedule[groupEnd + 1].GroupName == currentGroupName)
                            {
                                groupEnd++;
                            }

                            int groupSize = groupEnd - groupStart + 1;

                            for (int k = 0; k < groupSize; k++)
                            {
                                var item = sortedSchedule[groupStart + k];

                                if (k == 0)
                                {
                                    // Номер (rowspan)
                                    table.AddCell(new Cell(groupSize, 1)
                                        .Add(new Paragraph(currentNumber.ToString())
                                            .SetFont(_timesNewRoman)
                                            .SetFontSize(9)
                                            .SetTextAlignment(TextAlignment.CENTER))
                                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                                        .SetBorder(new SolidBorder(0.5f))
                                        .SetPadding(2));

                                    // Группа (rowspan)
                                    table.AddCell(new Cell(groupSize, 1)
                                        .Add(new Paragraph(item.GroupName ?? "-")
                                            .SetFont(_timesNewRoman)
                                            .SetFontSize(9)
                                            .SetTextAlignment(TextAlignment.CENTER))
                                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                                        .SetBorder(new SolidBorder(0.5f))
                                        .SetPadding(2));

                                    // Профиль (rowspan)
                                    table.AddCell(new Cell(groupSize, 1)
                                        .Add(new Paragraph(item.ProgramName ?? "-")
                                            .SetFont(_timesNewRoman)
                                            .SetFontSize(9)
                                            .SetTextAlignment(TextAlignment.CENTER))
                                        .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                                        .SetBorder(new SolidBorder(0.5f))
                                        .SetPadding(2));
                                }

                                // Ячейки добавляются для КАЖДОЙ строки
                                table.AddCell(CreateCell(item.DisciplineName ?? "-"));
                                table.AddCell(CreateCell(item.FacultyName ?? "-"));
                                table.AddCell(CreateCell(item.DepartmentName ?? "-"));
                                table.AddCell(CreateCell(item.TeacherName ?? "-"));
                                table.AddCell(CreateDateTimeCell(item));
                                table.AddCell(CreateLocationCell(item));
                            }

                            currentNumber++;
                            i = groupEnd + 1;
                        }
                    }
                    else
                    {
                        Cell messageCell = new Cell(1, 9)
                            .Add(new Paragraph("Нет данных для отображения")
                                .SetFont(_timesNewRoman)
                                .SetFontSize(10)
                                .SetTextAlignment(TextAlignment.CENTER))
                            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                            .SetBorder(new SolidBorder(1))
                            .SetPadding(10);
                        table.AddCell(messageCell);
                    }

                    document.Add(table);

                    var facultySignatures = schedule?
                        .Where(item => item.FacultyId > 0)
                        .GroupBy(item => item.FacultyId)
                        .Select(group => group.First())
                        .OrderBy(item => item.FacultyName)
                        .ToList() ?? new List<TestingScheduleDto>();

                    foreach (var faculty in facultySignatures)
                    {
                        var facultyDisplayName = faculty.FacultyName.Trim();
                        var position = facultyDisplayName.StartsWith("Факультет ", StringComparison.OrdinalIgnoreCase)
                            ? $"Декан факультета {facultyDisplayName["Факультет ".Length..]}"
                            : $"Руководитель {facultyDisplayName}";

                        var signatureTable = new Table(UnitValue.CreatePercentArray(new float[] { 50f, 50f }));
                        signatureTable.SetWidth(UnitValue.CreatePercentValue(90));
                        signatureTable.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                        signatureTable.SetMarginTop(8);
                        signatureTable.SetBorder(Border.NO_BORDER);

                        var positionCell = new Cell()
                            .Add(new Paragraph(position)
                                .SetFontSize(9)
                                .SetFont(_timesNewRoman)
                                .SetTextAlignment(TextAlignment.LEFT))
                            .SetBorder(Border.NO_BORDER)
                            .SetPadding(0)
                            .SetVerticalAlignment(VerticalAlignment.BOTTOM);
                        signatureTable.AddCell(positionCell);

                        var nameCell = new Cell()
                            .Add(new Paragraph(faculty.FacultyDeanName)
                                .SetFontSize(9)
                                .SetFont(_timesNewRoman)
                                .SetTextAlignment(TextAlignment.RIGHT))
                            .SetBorder(Border.NO_BORDER)
                            .SetPadding(0)
                            .SetVerticalAlignment(VerticalAlignment.BOTTOM);
                        signatureTable.AddCell(nameCell);

                        document.Add(signatureTable);
                    }

                    document.Close();
                    pdf.Close();

                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации PDF");
                throw new Exception($"Ошибка при генерации PDF: {ex.Message}", ex);
            }
        }

        public byte[] GenerateGroupReportPdf(GroupReportDto report)
        {
            try
            {
                var (regularFont, boldFont) = LoadFonts();
                using var reportsStream = new MemoryStream();
                using (var document = new Document(
                           new PdfDocument(new PdfWriter(reportsStream)),
                           PageSize.A4))
                {
                    var compactLayout = report.StudentNames.Count > 30;
                    document.SetMargins(compactLayout ? 24 : 40, 32, compactLayout ? 20 : 30, 32);
                    var studentFontSize = report.StudentNames.Count > 40
                        ? 6
                        : report.StudentNames.Count > 30 ? 7 : report.StudentNames.Count > 20 ? 8 : 9;
                    var cellPadding = compactLayout ? 0.15f : 0.5f;

                    document.Add(new Paragraph("Отчет о проверке остаточных знаний")
                        .SetFont(boldFont)
                        .SetFontSize(12)
                        .SetTextAlignment(TextAlignment.CENTER)
                        .SetMarginBottom(5));

                    var details = new Table(UnitValue.CreatePercentArray(new float[] { 0.65f, 9.35f }))
                        .SetWidth(UnitValue.CreatePercentValue(87))
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                    AddReportCell(details, "№", TextAlignment.CENTER, boldFont, 9, 6, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(details, $"Учебный год: {report.AcademicYear}", TextAlignment.LEFT, boldFont, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(details, $"Код и наименование направления (специальности): {report.StudyProgramCode}", TextAlignment.LEFT, boldFont, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(details, $"Наименование образовательной программы: {report.StudyProgramName}", TextAlignment.LEFT, boldFont, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(details, $"Наименование дисциплины: {report.DisciplineName}", TextAlignment.LEFT, boldFont, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(details, $"ФИО педагогического работника, проводившего занятия в предыдущем семестре: {report.TeacherNames}", TextAlignment.LEFT, boldFont, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(details, $"Учебная группа № {report.GroupName}", TextAlignment.LEFT, boldFont, padding: cellPadding, defaultFont: regularFont);
                    document.Add(details);

                    var results = new Table(UnitValue.CreatePercentArray(new float[] { 0.65f, 4.35f, 2.5f, 2.5f }))
                        .SetWidth(UnitValue.CreatePercentValue(87))
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);
                    AddReportCell(results, string.Empty, TextAlignment.CENTER, boldFont, studentFontSize, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(results, "ФИО обучающегося", TextAlignment.CENTER, boldFont, studentFontSize, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(results, "Результаты\nпромежуточной\nаттестации", TextAlignment.CENTER, boldFont, studentFontSize, padding: cellPadding, defaultFont: regularFont);
                    AddReportCell(results, "Результаты\nтестирования", TextAlignment.CENTER, boldFont, studentFontSize, padding: cellPadding, defaultFont: regularFont);

                    for (var studentIndex = 0; studentIndex < report.StudentNames.Count; studentIndex++)
                    {
                        AddReportCell(results, (studentIndex + 1).ToString(), TextAlignment.CENTER, null, studentFontSize, padding: cellPadding, defaultFont: regularFont);
                        AddReportCell(results, report.StudentNames[studentIndex], TextAlignment.LEFT, null, studentFontSize, padding: cellPadding, defaultFont: regularFont);
                        AddReportCell(results, string.Empty, TextAlignment.CENTER, null, studentFontSize, padding: cellPadding, defaultFont: regularFont);
                        AddReportCell(results, string.Empty, TextAlignment.CENTER, null, studentFontSize, padding: cellPadding, defaultFont: regularFont);
                    }

                    AddReportSummary(results, "Всего в группе: __________ (чел.)", studentFontSize, cellPadding, regularFont);
                    AddReportSummary(results, "Прошли тестирование: __________ (чел.)", studentFontSize, cellPadding, regularFont);
                    AddReportSummary(results, "Не допущены к тестированию: __________ (чел.)", studentFontSize, cellPadding, regularFont);
                    AddReportSummary(results, "Не явилось на тестирование: __________ (чел.)", studentFontSize, cellPadding, regularFont);
                    document.Add(results);

                    var signatures = new Table(UnitValue.CreatePercentArray(new float[] { 6.5f, 3.5f }))
                        .SetWidth(UnitValue.CreatePercentValue(87))
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER)
                        .SetMarginTop(compactLayout ? 2 : 6)
                        .SetKeepTogether(true);
                    AddSignature(signatures,
                        $"Заведующий кафедрой\n{report.DepartmentName}",
                        "________________\nФИО\nподпись",
                        regularFont);
                    AddSignature(signatures,
                        $"Декан факультета / директор института\n{report.FacultyName}",
                        "________________\nФИО\nподпись",
                        regularFont);
                    document.Add(signatures);
                }

                return reportsStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при генерации PDF-отчёта по группе");
                throw new Exception($"Ошибка при генерации PDF-отчёта: {ex.Message}", ex);
            }
        }

        private void AddReportSummary(Table table, string text, float fontSize, float padding, PdfFont font)
        {
            table.AddCell(new Cell(1, 4)
                .Add(new Paragraph(text).SetFont(font).SetFontSize(fontSize))
                .SetBorder(new SolidBorder(0.5f))
                .SetPadding(padding));
        }

        private void AddReportCell(
            Table table,
            string text,
            TextAlignment alignment,
            PdfFont? font = null,
            float fontSize = 9,
            int rowSpan = 1,
            int colSpan = 1,
            float padding = 0.5f,
            PdfFont? defaultFont = null)
        {
            table.AddCell(new Cell(rowSpan, colSpan)
                .Add(new Paragraph(text)
                    .SetFont(font ?? defaultFont ?? _timesNewRoman)
                    .SetFontSize(fontSize)
                    .SetTextAlignment(alignment))
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(new SolidBorder(0.5f))
                .SetPadding(padding));
        }

        private void AddSignature(Table table, string title, string signature, PdfFont font)
        {
            table.AddCell(new Cell()
                .Add(new Paragraph(title).SetFont(font).SetFontSize(9))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(0));
            table.AddCell(new Cell()
                .Add(new Paragraph(signature).SetFont(font).SetFontSize(9).SetTextAlignment(TextAlignment.RIGHT))
                .SetBorder(Border.NO_BORDER)
                .SetPadding(0));
        }

    }
}