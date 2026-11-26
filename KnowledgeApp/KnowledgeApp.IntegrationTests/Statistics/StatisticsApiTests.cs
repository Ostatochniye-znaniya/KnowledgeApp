using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.IntegrationTests.Infrastructure;

namespace KnowledgeApp.IntegrationTests.Statistics;

/// <summary>
/// Контракт статистики для фронта: /Statistics/GetFilters, GetSummary, GetDepartmentDetails.
/// </summary>
public class StatisticsApiTests : IntegrationTestBase
{
    public StatisticsApiTests(KnowledgeAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetFilters_ReturnsSeededSemesters()
    {
        using var response = await Client.GetAsync("Statistics/GetFilters");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var semesters = json.GetProperty("semesters").EnumerateArray().ToList();
        Assert.NotEmpty(semesters);
        AssertFields(semesters[0], "id", "label", "isSpring");
        Assert.Contains(semesters, s => s.GetProperty("id").GetInt32() == TestSeed.CurrentSemesterId);
    }

    [Fact]
    public async Task GetSummary_EmptySemester_ReturnsFacultyScopedDepartmentsWithZeroFacts()
    {
        // semester 3 есть в TestSeed, но без testing/reports
        using var response = await Client.GetAsync(
            $"Statistics/GetSummary?semesterId=3&facultyId={TestSeed.ItFacultyId}&departmentId={TestSeed.IiitDepartmentId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, json.GetProperty("semesterId").GetInt32());
        AssertFields(json, "semesterId", "semesterLabel", "isSpring", "requiresPaperReports", "faculties");

        var faculties = json.GetProperty("faculties").EnumerateArray().ToList();
        Assert.NotEmpty(faculties);
        var dept = faculties[0].GetProperty("departments").EnumerateArray().Single();
        Assert.Equal(TestSeed.IiitDepartmentId, dept.GetProperty("id").GetInt32());
        Assert.Equal(0, dept.GetProperty("groupFact").GetInt32());
        Assert.Equal(0, dept.GetProperty("classFact").GetInt32());
        Assert.Equal(0, dept.GetProperty("eReportDone").GetInt32());
    }

    [Fact]
    public async Task GetSummary_WithTestingAndOrder_ReturnsAggregatesAndPaperFlag()
    {
        await SeedStatisticsDataAsync(requiresPaperReports: false);

        using var response = await Client.GetAsync(
            $"Statistics/GetSummary?semesterId={TestSeed.CurrentSemesterId}&facultyId={TestSeed.ItFacultyId}&departmentId={TestSeed.IiitDepartmentId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(json.GetProperty("requiresPaperReports").GetBoolean());
        Assert.Equal(TestSeed.CurrentSemesterId, json.GetProperty("semesterId").GetInt32());

        var dept = json.GetProperty("faculties")[0].GetProperty("departments").EnumerateArray().Single();
        Assert.Equal(TestSeed.IiitDepartmentId, dept.GetProperty("id").GetInt32());
        Assert.True(dept.GetProperty("groupFact").GetInt32() >= 1);
        Assert.True(dept.GetProperty("classFact").GetInt32() >= 1);
        Assert.True(dept.GetProperty("eReportDone").GetInt32() >= 1);
        Assert.True(dept.GetProperty("profilesCount").GetInt32() >= 1);
    }

    [Fact]
    public async Task GetSummary_WithPaperRequiredOrder_SetsRequiresPaperReportsTrue()
    {
        await SeedStatisticsDataAsync(requiresPaperReports: true);

        using var response = await Client.GetAsync(
            $"Statistics/GetSummary?semesterId={TestSeed.CurrentSemesterId}&facultyId={TestSeed.ItFacultyId}&departmentId={TestSeed.IiitDepartmentId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("requiresPaperReports").GetBoolean());
    }

    [Fact]
    public async Task GetDepartmentDetails_ReturnsDisciplineGroupsFromTesting()
    {
        await SeedStatisticsDataAsync(requiresPaperReports: true);

        using var response = await Client.GetAsync(
            $"Statistics/GetDepartmentDetails?semesterId={TestSeed.CurrentSemesterId}&departmentId={TestSeed.IiitDepartmentId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(TestSeed.IiitDepartmentId, json.GetProperty("departmentId").GetInt32());
        Assert.True(json.GetProperty("requiresPaperReports").GetBoolean());

        var disciplines = json.GetProperty("disciplines").EnumerateArray().ToList();
        Assert.NotEmpty(disciplines);
        AssertFields(disciplines[0], "id", "title", "groups", "eReportDone", "pReportDone");
        Assert.NotEmpty(disciplines[0].GetProperty("groups").EnumerateArray().ToList());
    }

    [Fact]
    public async Task GetDepartmentDetails_WithoutDepartment_ReturnsBadRequest()
    {
        // departmentId=0 отключает fallback на TEST_DEPARTMENT_ID и оставляет пустой scope
        using var response = await Client.GetAsync(
            $"Statistics/GetDepartmentDetails?semesterId={TestSeed.CurrentSemesterId}&departmentId=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task SeedStatisticsDataAsync(bool requiresPaperReports)
    {
        await using var db = Factory.CreateDbContext();

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        db.Orders.Add(new Order
        {
            Id = 100,
            Name = "Тестовый приказ статистики",
            SemesterId = TestSeed.CurrentSemesterId,
            Status = "issued",
            RequiresPaperReports = requiresPaperReports,
            OrderDate = today,
            EducationControlDepartmentHead = "Иванов И. И.",
            EducationalMethodicalCenterHead = "Петров П. П.",
            InspectionSummaryDeadline = today.AddDays(30),
            SurveySummaryDeadline = today.AddDays(20),
            ElectronicReportDeadline = today.AddDays(25),
            TestingStartDate = today.AddDays(-10),
            TestingEndDate = today.AddDays(10),
            FileKey = "orders/test.docx",
            FileSize = 1024,
            CreatedAt = DateTime.UtcNow
        });

        db.Disciplines.Add(new Discipline
        {
            Id = 50,
            Name = "Сети и телекоммуникации",
            DepartmentId = TestSeed.IiitDepartmentId
        });

        db.Reports.Add(new Report
        {
            Id = 70,
            DisciplineId = 50,
            TeacherId = TestSeed.TeacherUserId,
            DoneInElectronicForm = true,
            DoneInPaperForm = requiresPaperReports,
            Status = DocumentStatus.Signed,
            FileName = "report.pdf",
            FilePath = "reports/report.pdf"
        });

        db.Testings.Add(new Testing
        {
            Id = 80,
            GroupId = TestSeed.WebGroupId,
            DisciplineId = 50,
            SemesterId = TestSeed.CurrentSemesterId,
            ReportId = 70,
            Status = "scheduled"
        });

        await db.SaveChangesAsync();
    }

    private static void AssertFields(JsonElement item, params string[] fields)
    {
        foreach (var field in fields)
            Assert.True(item.TryGetProperty(field, out _), $"В ответе нет поля «{field}»: {item}");
    }
}
