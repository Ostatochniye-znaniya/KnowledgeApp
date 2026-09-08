using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Application.Interfaces;
using iText.Kernel.Pdf;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using Microsoft.Extensions.Logging;
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
            PdfFont regular = null;
            PdfFont bold = null;

            try
            {
                string timesPath = @"C:\Windows\Fonts\times.ttf";
                string timesBoldPath = @"C:\Windows\Fonts\timesbd.ttf";

                if (File.Exists(timesPath))
                {
                    regular = PdfFontFactory.CreateFont(timesPath, "Identity-H");
                }
                else
                {
                    string[] altPaths = {
                        @"C:\Windows\Fonts\arial.ttf",
                        @"C:\Windows\Fonts\calibri.ttf"
                    };
                    foreach (var path in altPaths)
                    {
                        if (File.Exists(path))
                        {
                            regular = PdfFontFactory.CreateFont(path, "Identity-H");
                            break;
                        }
                    }
                }

                if (File.Exists(timesBoldPath))
                {
                    bold = PdfFontFactory.CreateFont(timesBoldPath, "Identity-H");
                }

                if (regular == null)
                {
                    regular = PdfFontFactory.CreateFont();
                    _logger.LogWarning("Шрифт не найден, используется стандартный");
                }
                if (bold == null)
                {
                    bold = regular;
                }

                _logger.LogInformation("Шрифты загружены");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось загрузить шрифты, используется стандартный");
                regular = PdfFontFactory.CreateFont();
                bold = regular;
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

        public byte[] GenerateTestingSchedulePdf(List<TestingScheduleDto> schedule, string facultyName = null, int facultyId = 0, string semesterPeriod = null, int semesterId = 0)
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
                        facultyTitle = $"Факультет {facultyName}";
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

                    float[] columnProportions = { 0.4f, 0.9f, 3.2f, 3.0f, 1.2f, 2.0f, 0.9f, 0.8f };
                    Table table = new Table(UnitValue.CreatePercentArray(columnProportions));
                    table.SetWidth(UnitValue.CreatePercentValue(100));
                    // Увеличиваем левый отступ таблицы
                    table.SetMarginLeft(30);

                    string[] headers = {
                        "№", "Группа", "Наименование профиля подготовки",
                        "Наименование дисциплины", "Кафедра", "ФИО ППС",
                        "Дата проведения", "Время проведения"
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
                                table.AddCell(CreateCell(item.DepartmentName ?? "-"));
                                table.AddCell(CreateCell(item.TeacherName ?? "-"));
                                table.AddCell(CreateCell(item.Date.ToString("dd.MM.yyyy")));
                                table.AddCell(CreateCell(item.Time.ToString(@"hh\:mm")));
                            }

                            currentNumber++;
                            i = groupEnd + 1;
                        }
                    }
                    else
                    {
                        Cell messageCell = new Cell(1, 8)
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

                    // Добавляем отступ перед подписью
                    document.Add(new Paragraph("\n"));

                    // Создаем таблицу для центрирования подписи
                    Table signatureWrapper = new Table(UnitValue.CreatePercentArray(new float[] { 5f, 90f, 5f }));
                    signatureWrapper.SetWidth(UnitValue.CreatePercentValue(100));
                    signatureWrapper.SetMarginTop(8);
                    signatureWrapper.SetBorder(Border.NO_BORDER);

                    // Левая пустая ячейка
                    Cell leftMarginCell = new Cell();
                    leftMarginCell.SetBorder(Border.NO_BORDER);
                    leftMarginCell.SetPadding(0);
                    signatureWrapper.AddCell(leftMarginCell);

                    // Центральная ячейка с подписью
                    Cell signatureCell = new Cell();
                    signatureCell.SetBorder(Border.NO_BORDER);
                    signatureCell.SetPadding(0);

                    // Таблица для подписи (2 колонки)
                    Table signatureTable = new Table(UnitValue.CreatePercentArray(new float[] { 50f, 50f }));
                    signatureTable.SetWidth(UnitValue.CreatePercentValue(100));
                    signatureTable.SetBorder(Border.NO_BORDER);

                    // Левая часть - должность
                    Cell positionCell = new Cell();
                    positionCell.Add(new Paragraph("Декан факультета информационных технологий")
                        .SetFontSize(10)
                        .SetFont(_timesNewRoman)
                        .SetTextAlignment(TextAlignment.LEFT));
                    positionCell.SetBorder(Border.NO_BORDER);
                    positionCell.SetPadding(0);
                    positionCell.SetVerticalAlignment(VerticalAlignment.BOTTOM);
                    signatureTable.AddCell(positionCell);

                    // Правая часть - ФИО
                    Cell nameCell = new Cell();
                    nameCell.Add(new Paragraph("Д.Г. Демидов")
                        .SetFontSize(10)
                        .SetFont(_timesNewRoman)
                        .SetTextAlignment(TextAlignment.RIGHT));
                    nameCell.SetBorder(Border.NO_BORDER);
                    nameCell.SetPadding(0);
                    nameCell.SetVerticalAlignment(VerticalAlignment.BOTTOM);
                    signatureTable.AddCell(nameCell);

                    signatureCell.Add(signatureTable);
                    signatureWrapper.AddCell(signatureCell);

                    // Правая пустая ячейка
                    Cell rightMarginCell = new Cell();
                    rightMarginCell.SetBorder(Border.NO_BORDER);
                    rightMarginCell.SetPadding(0);
                    signatureWrapper.AddCell(rightMarginCell);

                    document.Add(signatureWrapper);

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
    }
}