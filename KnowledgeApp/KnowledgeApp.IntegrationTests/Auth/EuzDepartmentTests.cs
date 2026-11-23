using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeApp.API.Authentication;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeApp.IntegrationTests.Auth;

public sealed class EuzDepartmentTests(KnowledgeAppFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task DisciplinePageUsesDepartmentOfEachVerifiedEuzUser()
    {
        await using (var db = Factory.CreateDbContext())
        {
            var first = await db.Users.SingleAsync(u => u.Id == TestSeed.HodUserId);
            first.ExternalId = "euz-smart";
            var second = await db.Users.SingleAsync(u => u.Id == TestSeed.AdminUserId);
            second.ExternalId = "euz-iiit";
            second.DepartmentId = TestSeed.IiitDepartmentId;
            db.Disciplines.Add(new Discipline { Id = 100, Name = "Учебная дисциплина", DepartmentId = TestSeed.IiitDepartmentId });
            db.Testings.Add(new Testing
            {
                GroupId = TestSeed.WebGroupId, SemesterId = TestSeed.CurrentSemesterId, DisciplineId = 100
            });
            await db.SaveChangesAsync();
        }

        using var client = RealAuthClient();
        using var firstDepartment = await Read(client, "GroupDiscipline/GetDepartment", "session-smart");
        using var secondDepartment = await Read(client, "GroupDiscipline/GetDepartment", "session-iiit");
        Assert.Equal(TestSeed.SmartDepartmentId, (await firstDepartment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());
        Assert.Equal(TestSeed.IiitDepartmentId, (await secondDepartment.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32());

        var groupPath = $"GroupDiscipline/GetGroups?semesterId={TestSeed.CurrentSemesterId}";
        using var otherDepartmentGroups = await Read(client, groupPath, "session-smart");
        using var ownDepartmentGroups = await Read(client, groupPath, "session-iiit");
        Assert.Equal(0, (await otherDepartmentGroups.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength());
        var group = Assert.Single((await ownDepartmentGroups.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray());
        Assert.Equal(TestSeed.WebGroupId, group.GetProperty("groupId").GetInt32());

        using var forbiddenDiscipline = await Read(client,
            $"GroupDiscipline/GetAvailableDisciplines/{TestSeed.WebGroupId}", "session-smart");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenDiscipline.StatusCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-session")]
    public async Task RealModeDoesNotUseTestDepartmentWithoutVerifiedSession(string? session)
    {
        using var client = RealAuthClient();
        using var response = await Read(client, "GroupDiscipline/GetDepartment", session);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient RealAuthClient() => Factory.CreateApiClientWithoutMockAuth(services =>
        services.AddHttpClient(EuzAuthenticationHandler.ProfileClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new EuzProfileStub()));

    private static async Task<HttpResponseMessage> Read(HttpClient client, string path, string? session)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Mock-Role", "admin");
        if (session is not null)
            request.Headers.Add("Cookie", $"access_token={session}");
        return await client.SendAsync(request);
    }

    private sealed class EuzProfileStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal("/api/v1/users/me", request.RequestUri?.AbsolutePath);
            var externalId = request.Headers.Authorization?.Parameter switch
            {
                "session-smart" => "euz-smart",
                "session-iiit" => "euz-iiit",
                _ => null
            };
            return Task.FromResult(externalId is null
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { id = externalId }) });
        }
    }
}
