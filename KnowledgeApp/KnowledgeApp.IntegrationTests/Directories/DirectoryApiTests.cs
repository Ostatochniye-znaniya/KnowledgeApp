using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeApp.IntegrationTests.Infrastructure;

namespace KnowledgeApp.IntegrationTests.Directories;

/// <summary>
/// Контракт справочников, на который опирается фронтенд: адреса, коды ответа,
/// имена полей в JSON (camelCase) и данные из БД.
/// </summary>
public class DirectoryApiTests : IntegrationTestBase
{
    public DirectoryApiTests(KnowledgeAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Roles_ReturnsAllSeededRoles()
    {
        var roles = await GetArray("Role/GetRoles");

        Assert.Equal(TestSeed.Roles.Length, roles.Count);
        AssertFields(roles[0], "id", "roleName");
        Assert.Contains(roles, r => r.GetProperty("roleName").GetString() == "Преподаватель");
    }

    [Fact]
    public async Task RoleById_ReturnsRole()
    {
        var role = await Client.GetFromJsonAsync<JsonElement>("Role/GetRoleById/1");

        Assert.Equal(1, role.GetProperty("id").GetInt32());
        Assert.Equal("Администратор", role.GetProperty("roleName").GetString());
    }

    [Fact]
    public async Task RoleById_Unknown_ReturnsProblemWithReason()
    {
        // Сейчас контроллер отдаёт 500 через Results.Problem, а не 404: фиксируем, что ответ
        // не успешный и в нём есть причина, чтобы фронт мог её показать
        using var response = await Client.GetAsync("Role/GetRoleById/999");

        Assert.False(response.IsSuccessStatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("не существует", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Statuses_ReturnsSeededStatuses()
    {
        var statuses = await GetArray("Status/GetStatuses");

        AssertFields(statuses[0], "id", "statusName");
        Assert.Equal(new[] { "Активен", "Неактивен", "Заблокирован" },
            statuses.Select(s => s.GetProperty("statusName").GetString()));
    }

    [Fact]
    public async Task Faculties_ReturnsSeededFaculties()
    {
        var faculties = await GetArray("Faculty/GetFaculties");

        AssertFields(faculties[0], "id", "facultyName");
        Assert.Equal(
            new[] { TestSeed.ItFacultyId, TestSeed.BasicFacultyId },
            faculties.Select(f => f.GetProperty("id").GetInt32()).OrderBy(id => id));
    }

    [Fact]
    public async Task Departments_ReturnFacultyLink()
    {
        var departments = await GetArray("Department/GetDepartments");

        AssertFields(departments[0], "id", "name", "facultyId");
        var fbk = departments.Single(d => d.GetProperty("id").GetInt32() == TestSeed.FbkDepartmentId);
        Assert.Equal("ФБК", fbk.GetProperty("name").GetString());
        Assert.Equal(TestSeed.BasicFacultyId, fbk.GetProperty("facultyId").GetInt32());
    }

    [Fact]
    public async Task DepartmentById_ReturnsDepartment()
    {
        // Опечатка в имени действия (GetDepatmentById) — часть контракта, на неё завязан фронт
        var department = await Client.GetFromJsonAsync<JsonElement>($"Department/GetDepatmentById/{TestSeed.IiitDepartmentId}");

        Assert.Equal("ИиИТ", department.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Semesters_ReturnYearAndParts()
    {
        var semesters = await GetArray("Semester/GetSemesters");

        AssertFields(semesters[0], "id", "semesterYear", "semesterPart", "yearPart");
        var current = semesters.Single(s => s.GetProperty("id").GetInt32() == TestSeed.CurrentSemesterId);
        Assert.Equal(2026, current.GetProperty("semesterYear").GetInt32());
        Assert.Equal(2, current.GetProperty("semesterPart").GetInt32());
    }

    [Fact]
    public async Task StudyPrograms_ReturnDirectionCodeAndGroups()
    {
        var programs = await GetArray("StudyProgram/GetAll");

        var program = Assert.Single(programs);
        AssertFields(program, "id", "name", "departmentId", "cypherOfTheDirection", "studyGroupIds");
        Assert.Equal("09.03.01", program.GetProperty("cypherOfTheDirection").GetString());
        Assert.Equal(TestSeed.IiitDepartmentId, program.GetProperty("departmentId").GetInt32());
    }

    [Fact]
    public async Task StudyGroups_ReturnProgramLink()
    {
        var groups = await GetArray("StudyGroup/GetAll");

        var group = Assert.Single(groups);
        AssertFields(group, "id", "groupNumber", "studyProgramId", "studentIds", "testingIds");
        Assert.Equal(TestSeed.WebGroupNumber, group.GetProperty("groupNumber").GetString());
        Assert.Equal(TestSeed.WebProgramId, group.GetProperty("studyProgramId").GetInt32());
    }

    [Fact]
    public async Task StudyGroupById_Unknown_ReturnsNotFound()
    {
        using var response = await Client.GetAsync("StudyGroup/GetById/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateFaculty_ThenAppearsInList()
    {
        using var response = await Client.PostAsJsonAsync("Faculty/CreateFaculty",
            new { facultyName = "Факультет машиностроения" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var faculties = await GetArray("Faculty/GetFaculties");
        Assert.Contains(faculties, f => f.GetProperty("facultyName").GetString() == "Факультет машиностроения");
    }

    [Fact]
    public async Task CreateDepartment_IsLinkedToFaculty()
    {
        using var response = await Client.PostAsJsonAsync("Department/CreateDepartment",
            new { name = "Математика", facultyId = TestSeed.BasicFacultyId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = Factory.CreateDbContext();
        var created = db.Departments.Single(d => d.Name == "Математика");
        Assert.Equal(TestSeed.BasicFacultyId, created.FacultyId);
    }

    private async Task<List<JsonElement>> GetArray(string url)
    {
        using var response = await Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, json.ValueKind);
        var items = json.EnumerateArray().ToList();
        Assert.NotEmpty(items);
        return items;
    }

    private static void AssertFields(JsonElement item, params string[] fields)
    {
        foreach (var field in fields)
            Assert.True(item.TryGetProperty(field, out _), $"В ответе нет поля «{field}»: {item}");
    }
}
