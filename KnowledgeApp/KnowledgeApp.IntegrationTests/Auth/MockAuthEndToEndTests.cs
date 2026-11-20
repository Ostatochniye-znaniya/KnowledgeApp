using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeApp.Application.DTOs.MockAuth;
using KnowledgeApp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.IntegrationTests.Auth;

/// <summary>
/// MOCK-авторизация через весь конвейер: MockAuthenticationHandler → контроллер → сервис.
/// В API нет [Authorize] и проверок ролей, поэтому здесь проверяется, что личность
/// пользователя корректно определяется и доходит до бизнес-логики, а не запреты доступа.
/// </summary>
public class MockAuthEndToEndTests : IntegrationTestBase
{
    public MockAuthEndToEndTests(KnowledgeAppFactory factory) : base(factory)
    {
    }

    public static TheoryData<string, string> AllRoles => new()
    {
        { "admin", MockRoleType.AdminSlug },
        { "lpr", MockRoleType.LprSlug },
        { "hod", MockRoleType.HodSlug },
        { "teacher", MockRoleType.TeacherSlug },
        { "rod", MockRoleType.RodSlug },
        { "department_responsible", MockRoleType.DepartmentResponsibleSlug },
        { "student", MockRoleType.StudentSlug },
        { "guest", MockRoleType.GuestSlug },
    };

    [Theory]
    [MemberData(nameof(AllRoles))]
    public async Task Me_WithMockRoleHeader_ReturnsThatRole(string role, string expectedSlug)
    {
        var me = await GetMe(request => request.Headers.Add("X-Mock-Role", role));

        Assert.Equal(expectedSlug, me.GetProperty("roleSlug").GetString());
        Assert.Equal($"mock-access-token-{expectedSlug}", me.GetProperty("access_token").GetString());
    }

    [Fact]
    public async Task Me_WithBearerMockToken_ReturnsThatRole()
    {
        var me = await GetMe(request => request.Headers.Add("Authorization", "Bearer mock-teacher"));

        Assert.Equal(MockRoleType.TeacherSlug, me.GetProperty("roleSlug").GetString());
    }

    [Fact]
    public async Task Me_WithMockRoleCookie_ReturnsThatRole()
    {
        var me = await GetMe(request => request.Headers.Add("Cookie", "mock_role=hod"));

        Assert.Equal(MockRoleType.HodSlug, me.GetProperty("roleSlug").GetString());
    }

    [Fact]
    public async Task Me_WithoutCredentials_ReturnsDefaultRole()
    {
        var me = await GetMe(_ => { });

        Assert.Equal(MockRoleType.AdminSlug, me.GetProperty("roleSlug").GetString());
    }

    [Fact]
    public async Task Me_SetsFrontendCookies()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "auth/me");
        request.Headers.Add("X-Mock-Role", "student");

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookies = string.Join("; ", response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("mock_role=student", cookies);
        Assert.Contains("is_authenticated=true", cookies);
    }

    [Fact]
    public async Task MockRoles_ListsAllEightRoles()
    {
        var roles = await Client.GetFromJsonAsync<JsonElement>("auth/mock/roles");

        var slugs = roles.EnumerateArray().Select(r => r.GetProperty("role_slug").GetString()).ToList();
        Assert.Equal(AllRoles.Select(r => (string)r[1]).OrderBy(s => s), slugs.OrderBy(s => s));
    }

    [Fact]
    public async Task Me_WhenMockAuthDisabled_ReturnsUnauthorized()
    {
        using var client = Factory.CreateApiClientWithoutMockAuth();
        using var request = new HttpRequestMessage(HttpMethod.Get, "auth/me");
        request.Headers.Add("X-Mock-Role", "admin");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SwitchRole_WhenMockAuthDisabled_ReturnsBadRequest()
    {
        using var client = Factory.CreateApiClientWithoutMockAuth();

        using var response = await client.GetAsync("auth/mock/switch?role=teacher");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminAction_RecordsMockUserAsActorInAuditLog()
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"admin/users/{TestSeed.TeacherUserId}/role")
        {
            Content = JsonContent.Create(new { roleId = 7, reason = "проверка аудита" })
        };
        request.Headers.Add("X-Mock-Role", "lpr");

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var db = Factory.CreateDbContext();
        var entry = await db.AuditLogs.AsNoTracking().OrderByDescending(e => e.Id).FirstAsync();
        Assert.Equal(TestSeed.LprUserId, entry.ActorUserId);
        Assert.Equal(TestSeed.TeacherUserId.ToString(), entry.EntityId);
    }

    [Fact]
    public async Task AdminAction_UsesMockUserIdentityInBusinessRules()
    {
        // MOCK-админ имеет Id = 1, как и админ в сиде: снять роль администратора с себя нельзя
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"admin/users/{TestSeed.AdminUserId}/role")
        {
            Content = JsonContent.Create(new { roleId = 7 })
        };
        request.Headers.Add("X-Mock-Role", "admin");

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("самого себя", await response.Content.ReadAsStringAsync());
    }

    private async Task<JsonElement> GetMe(Action<HttpRequestMessage> configure)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "auth/me");
        configure(request);

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
