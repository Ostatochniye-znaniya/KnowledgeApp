using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Application.Services;

namespace KnowledgeApp.UnitTests.Documents;

public class DocxGeneratorServiceTests
{
    private readonly DocxGeneratorService _service = new();

    [Fact]
    public void GenerateBlankReportDocx_ReturnsValidDocument()
    {
        var docx = _service.GenerateBlankReportDocx();

        using var document = WordprocessingDocument.Open(new MemoryStream(docx), false);
        var tables = document.MainDocumentPart!.Document.Body!.Elements<Table>().ToList();
        Assert.True(tables.Count >= 2, "В бланке должны быть таблица отчёта и таблица подписей");
    }

    [Fact]
    public void GenerateGroupReportDocx_FillsReportDetails()
    {
        var text = DocumentText.FromDocx(_service.GenerateGroupReportDocx(Report(3)));

        Assert.Contains("Учебный год: 2026/2027", text);
        Assert.Contains("Код и наименование направления (специальности): 09.03.01", text);
        Assert.Contains("Наименование образовательной программы: Веб-технологии", text);
        Assert.Contains("Наименование дисциплины: Базы данных", text);
        Assert.Contains("Учебная группа № 221-321", text);
        Assert.Contains("Заведующий кафедрой ИиИТ", text);
        Assert.Contains("Декан факультета/Директор института Факультет информационных технологий", text);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(40)] // больше строк, чем в шаблоне: строки добавляются
    public void GenerateGroupReportDocx_HasRowPerStudent(int studentCount)
    {
        var report = Report(studentCount);

        var docx = _service.GenerateGroupReportDocx(report);

        var text = DocumentText.FromDocx(docx);
        Assert.Contains($"Всего в группе: {studentCount} (чел.)", text);
        foreach (var name in report.StudentNames)
            Assert.Contains(name, text);

        using var document = WordprocessingDocument.Open(new MemoryStream(docx), false);
        var studentRows = document.MainDocumentPart!.Document.Body!.Elements<Table>().First()
            .Elements<TableRow>()
            .Count(r => report.StudentNames.Contains(r.Elements<TableCell>().ElementAtOrDefault(1)?.InnerText));
        Assert.Equal(studentCount, studentRows);
    }

    [Fact]
    public void GenerateGroupReportDocx_NumbersStudentsInOrder()
    {
        var report = Report(3);

        using var document = WordprocessingDocument.Open(new MemoryStream(_service.GenerateGroupReportDocx(report)), false);
        var numbers = document.MainDocumentPart!.Document.Body!.Elements<Table>().First()
            .Elements<TableRow>()
            .Where(r => report.StudentNames.Contains(r.Elements<TableCell>().ElementAtOrDefault(1)?.InnerText))
            .Select(r => r.Elements<TableCell>().First().InnerText);
        Assert.Equal(new[] { "1", "2", "3" }, numbers);
    }

    internal static GroupReportDto Report(int studentCount) => new()
    {
        DisciplineId = 1,
        AcademicYear = "2026/2027",
        StudyProgramCode = "09.03.01",
        StudyProgramName = "Веб-технологии",
        DisciplineName = "Базы данных",
        GroupName = "221-321",
        StudentNames = Enumerable.Range(1, studentCount).Select(i => $"Студентов Студент {i:D2}").ToList(),
        DepartmentName = "ИиИТ",
        FacultyName = "Факультет информационных технологий"
    };
}
