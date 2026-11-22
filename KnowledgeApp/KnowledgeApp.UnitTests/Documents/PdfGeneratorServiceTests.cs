using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace KnowledgeApp.UnitTests.Documents;

public class PdfGeneratorServiceTests
{
    private readonly PdfGeneratorService _service = new(NullLogger<PdfGeneratorService>.Instance);

    [Fact]
    public void GenerateTestingSchedulePdf_ContainsTitleAndRows()
    {
        var pdf = _service.GenerateTestingSchedulePdf(Schedule(), "Факультет информационных технологий", 1,
            "2026, осенний семестр", 4);

        var text = DocumentText.FromPdf(pdf);
        Assert.Contains("Факультет информационных технологий", text);
        Assert.Contains("График проверки остаточных знаний 2026, осенний семестр", text);
        Assert.Contains("221-321", text);
        Assert.Contains("221-322", text);
        Assert.Contains("Базы данных", text);
        Assert.Contains("Петров П.П.", text);
    }

    [Fact]
    public void GenerateTestingSchedulePdf_WithoutFilters_UsesGeneralTitle()
    {
        var text = DocumentText.FromPdf(_service.GenerateTestingSchedulePdf(Schedule()));

        Assert.Contains("Все факультеты", text);
        Assert.Contains("График проверки остаточных знаний", text);
        Assert.DoesNotContain("семестр", text);
    }

    [Fact]
    public void GenerateTestingSchedulePdf_AddsDeanSignaturePerFaculty()
    {
        var text = DocumentText.FromPdf(_service.GenerateTestingSchedulePdf(Schedule()));

        Assert.Contains("Декан факультета информационных технологий", text);
        Assert.Contains("Иванов И.И.", text);
    }

    [Fact]
    public void GenerateTestingSchedulePdf_EmptySchedule_ShowsPlaceholder()
    {
        var text = DocumentText.FromPdf(_service.GenerateTestingSchedulePdf(new List<TestingScheduleDto>()));

        Assert.Contains("Нет данных для отображения", text);
    }

    [Fact]
    public void GenerateGroupReportPdf_ContainsReportDetailsAndStudents()
    {
        var report = DocxGeneratorServiceTests.Report(5);

        var text = DocumentText.FromPdf(_service.GenerateGroupReportPdf(report));

        Assert.Contains("221-321", text);
        Assert.Contains("Базы данных", text);
        Assert.Contains("Веб-технологии", text);
        foreach (var name in report.StudentNames)
            Assert.Contains(name, text);
    }

    private static List<TestingScheduleDto> Schedule() =>
    [
        Row(1, "221-321", "Базы данных", "Петров П.П.", new DateTime(2026, 11, 24), new TimeSpan(9, 0, 0)),
        Row(2, "221-321", "Веб-программирование", "Сидоров С.С.", new DateTime(2026, 11, 25), new TimeSpan(10, 40, 0)),
        Row(3, "221-322", "Базы данных", "Петров П.П.", new DateTime(2026, 11, 24), new TimeSpan(12, 40, 0)),
    ];

    private static TestingScheduleDto Row(int id, string group, string discipline, string teacher, DateTime date, TimeSpan time) => new()
    {
        Id = id,
        Number = id,
        FacultyId = 1,
        SemesterId = 4,
        GroupName = group,
        ProgramName = "Веб-технологии",
        DisciplineName = discipline,
        FacultyName = "Факультет информационных технологий",
        FacultyDeanName = "Иванов И.И.",
        DepartmentName = "ИиИТ",
        TeacherName = teacher,
        Date = date,
        Time = time,
        Room = "Пр2301",
        IsInPerson = true
    };
}

public class OrderPdfGeneratorTests
{
    private readonly OrderPdfGenerator _generator = new();

    [Fact]
    public void Generate_ContainsRequisitesAndTitle()
    {
        var text = DocumentText.FromPdf(_generator.Generate(Order()), out var pages);

        Assert.True(pages >= 3, "Приказ и два приложения");
        Assert.Contains("ПРИКАЗ", text);
        Assert.Contains("05.11.2026", text);
        Assert.Contains("1234-ОД", text);
        Assert.Contains(OrderPdfGenerator.OrderName, text);
    }

    [Fact]
    public void Generate_DeclinesResponsiblePersons()
    {
        var text = DocumentText.FromPdf(_generator.Generate(Order()));

        // Начальнику управления — дательный падеж, руководителя центра — винительный
        Assert.Contains("А.С. Кузнецову", text);
        Assert.Contains("Е.В. Смирнову", text);
    }

    [Fact]
    public void Generate_ContainsAppendices()
    {
        var text = DocumentText.FromPdf(_generator.Generate(Order()));

        Assert.Contains("Приложение № 1 к приказу Московского Политеха", text);
        Assert.Contains("Приложение № 2 к приказу Московского Политеха", text);
    }

    private static OrderModel Order() => new()
    {
        Id = 1,
        Number = "1234-ОД",
        Name = OrderPdfGenerator.OrderName,
        OrderDate = new DateOnly(2026, 11, 5),
        SemesterId = 4,
        SemesterYear = 2026,
        SemesterPart = 2,
        EducationControlDepartmentHead = "Кузнецов Андрей Сергеевич",
        EducationalMethodicalCenterHead = "Смирнова Елена Викторовна",
        InspectionSummaryDeadline = new DateOnly(2026, 12, 20),
        SurveySummaryDeadline = new DateOnly(2026, 12, 22),
        PaperReportDeadline = new DateOnly(2026, 12, 25),
        ElectronicReportDeadline = new DateOnly(2026, 12, 18),
        TestingStartDate = new DateOnly(2026, 11, 23),
        TestingEndDate = new DateOnly(2026, 12, 12),
        SurveyStartDate = new DateOnly(2026, 11, 23),
        SurveyEndDate = new DateOnly(2026, 12, 12),
        PreparationStartDate = new DateOnly(2026, 11, 9),
        PreparationEndDate = new DateOnly(2026, 11, 20),
        ResultsProcessingStartDate = new DateOnly(2026, 12, 14)
    };
}
