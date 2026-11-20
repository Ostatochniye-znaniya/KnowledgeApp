using System.Net;
using KnowledgeApp.IntegrationTests.Infrastructure;

namespace KnowledgeApp.IntegrationTests.Smoke;

/// <summary>Проверка самой инфраструктуры: приложение стартует на тестовой БД и читает из неё.</summary>
public class ApiStartupTests : IntegrationTestBase
{
    public ApiStartupTests(KnowledgeAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Api_ReadsSeededData_ThroughPathBase()
    {
        var response = await Client.GetAsync("Faculty/GetFaculties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Факультет информационных технологий", body);
    }
}
