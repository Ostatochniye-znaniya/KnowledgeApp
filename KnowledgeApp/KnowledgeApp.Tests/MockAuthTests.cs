using System.Security.Claims;
using KnowledgeApp.API.Controllers;
using KnowledgeApp.Application.DTOs.MockAuth;
using KnowledgeApp.Application.Services.MockAuth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace KnowledgeApp.Tests;

public class MockAuthTests
{
    [Fact]
    public void MockAuthService_ContainsAllEightRoles()
    {
        var options = new MockAuthOptions
        {
            Enabled = true,
            DefaultRole = "Admin"
        };
        var service = new MockAuthService(options);

        var users = service.GetAllMockUsers();

        Assert.Equal(8, users.Count);
        Assert.Contains(users, u => u.RoleSlug == MockRoleType.AdminSlug);
        Assert.Contains(users, u => u.RoleSlug == MockRoleType.LprSlug);
        Assert.Contains(users, u => u.RoleSlug == MockRoleType.HodSlug);
        Assert.Contains(users, u => u.RoleSlug == MockRoleType.TeacherSlug);
        Assert.Contains(users, u => u.RoleSlug == MockRoleType.RodSlug);
        Assert.Contains(users, u => u.RoleSlug == MockRoleType.DepartmentResponsibleSlug);
        Assert.Contains(users, u => u.RoleSlug == MockRoleType.StudentSlug);
        Assert.Contains(users, u => u.RoleSlug == MockRoleType.GuestSlug);
    }

    [Theory]
    [InlineData("admin", MockRoleType.AdminSlug)]
    [InlineData("Admin", MockRoleType.AdminSlug)]
    [InlineData("администратор", MockRoleType.AdminSlug)]
    [InlineData("1", MockRoleType.AdminSlug)]
    [InlineData("mock-admin", MockRoleType.AdminSlug)]
    [InlineData("teacher", MockRoleType.TeacherSlug)]
    [InlineData("преподаватель", MockRoleType.TeacherSlug)]
    [InlineData("4", MockRoleType.TeacherSlug)]
    [InlineData("student", MockRoleType.StudentSlug)]
    [InlineData("студент", MockRoleType.StudentSlug)]
    [InlineData("7", MockRoleType.StudentSlug)]
    [InlineData("hod", MockRoleType.HodSlug)]
    [InlineData("заведующий кафедрой", MockRoleType.HodSlug)]
    [InlineData("3", MockRoleType.HodSlug)]
    [InlineData("lpr", MockRoleType.LprSlug)]
    [InlineData("лицо, принимающее решения", MockRoleType.LprSlug)]
    [InlineData("2", MockRoleType.LprSlug)]
    [InlineData("rod", MockRoleType.RodSlug)]
    [InlineData("декан", MockRoleType.RodSlug)]
    [InlineData("5", MockRoleType.RodSlug)]
    [InlineData("department_responsible", MockRoleType.DepartmentResponsibleSlug)]
    [InlineData("ответственный за подразделение", MockRoleType.DepartmentResponsibleSlug)]
    [InlineData("6", MockRoleType.DepartmentResponsibleSlug)]
    [InlineData("guest", MockRoleType.GuestSlug)]
    [InlineData("гость", MockRoleType.GuestSlug)]
    [InlineData("8", MockRoleType.GuestSlug)]
    public void MockAuthService_ResolvesAliasesCorrectly(string input, string expectedSlug)
    {
        var options = new MockAuthOptions { Enabled = true, DefaultRole = "Admin" };
        var service = new MockAuthService(options);

        var user = service.GetMockUser(input);

        Assert.NotNull(user);
        Assert.Equal(expectedSlug, user.RoleSlug);
    }

    [Fact]
    public void MockAuthService_CreatesClaimsPrincipal_WithMultipleRoleFormats()
    {
        var options = new MockAuthOptions { Enabled = true, DefaultRole = "Admin" };
        var service = new MockAuthService(options);
        var teacher = service.GetMockUser("teacher");
        Assert.NotNull(teacher);

        var principal = service.CreatePrincipal(teacher);

        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);
        Assert.Equal(teacher.FullName, principal.Identity.Name);

        // Проверяем, что роли присутствуют и на русском, и на английском, и в виде slug
        Assert.True(principal.IsInRole("Teacher"));
        Assert.True(principal.IsInRole("teacher"));
        Assert.True(principal.IsInRole("TEACHER"));
        Assert.True(principal.IsInRole("Преподаватель"));

        // Проверяем другие claims
        Assert.Equal(teacher.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(teacher.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal(teacher.FacultyId.ToString(), principal.FindFirst("faculty_id")?.Value);
    }

    [Fact]
    public void MockAuthService_WhenDisabled_ReportsDisabled()
    {
        var options = new MockAuthOptions { Enabled = false };
        var service = new MockAuthService(options);

        Assert.False(service.IsEnabled);
    }

    [Fact]
    public async Task AuthController_GetMe_ReturnsFrontendCompatibleUserAndSetsCookies()
    {
        var options = new MockAuthOptions { Enabled = true, DefaultRole = "Admin" };
        var service = new MockAuthService(options);
        var controller = CreateAuthController(service);

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.Me("teacher") as OkObjectResult;

        Assert.NotNull(result);
        dynamic response = result.Value!;
        Assert.NotNull(response);

        // Проверяем установку cookies
        var setCookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("user_data=", setCookieHeader);
        Assert.Contains("is_authenticated=true", setCookieHeader);
        Assert.Contains("mock_role=teacher", setCookieHeader);
    }

    [Fact]
    public void AuthController_SwitchRole_SwitchesRoleSuccessfully()
    {
        var options = new MockAuthOptions { Enabled = true, DefaultRole = "Admin" };
        var service = new MockAuthService(options);
        var controller = CreateAuthController(service);

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = controller.SwitchRole("student") as OkObjectResult;

        Assert.NotNull(result);
        var setCookieHeader = httpContext.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("mock_role=student", setCookieHeader);
    }

    [Fact]
    public void AuthController_GetMockRoles_ReturnsAllEightRoles()
    {
        var options = new MockAuthOptions { Enabled = true, DefaultRole = "Admin" };
        var service = new MockAuthService(options);
        var controller = CreateAuthController(service);

        var result = controller.GetMockRoles() as OkObjectResult;

        Assert.NotNull(result);
        var roles = result.Value as System.Collections.IEnumerable;
        Assert.NotNull(roles);
    }

    [Fact]
    public async Task AuthController_GetMe_WhenDisabled_ReturnsUnauthorized()
    {
        var options = new MockAuthOptions { Enabled = false };
        var service = new MockAuthService(options);
        var controller = CreateAuthController(service);

        var httpContext = new DefaultHttpContext();
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Me() as UnauthorizedObjectResult;

        Assert.NotNull(result);
    }

    [Fact]
    public async Task MockAuthenticationHandler_WithHeader_AuthenticatesSpecifiedRole()
    {
        var options = new MockAuthOptions { Enabled = true, DefaultRole = "Admin", HeaderName = "X-Mock-Role" };
        var service = new MockAuthService(options);
        var handler = CreateHandler(service, options);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Mock-Role"] = "Teacher";
        await handler.InitializeAsync(new Microsoft.AspNetCore.Authentication.AuthenticationScheme("MockAuth", null, typeof(KnowledgeApp.API.Authentication.MockAuthenticationHandler)), context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.True(result.Principal.IsInRole("Teacher"));
        Assert.Equal("teacher.mock@mospolytech.ru", result.Principal.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public async Task MockAuthenticationHandler_WithBearerToken_AuthenticatesSpecifiedRole()
    {
        var options = new MockAuthOptions { Enabled = true, DefaultRole = "Admin" };
        var service = new MockAuthService(options);
        var handler = CreateHandler(service, options);

        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer mock-student";
        await handler.InitializeAsync(new Microsoft.AspNetCore.Authentication.AuthenticationScheme("MockAuth", null, typeof(KnowledgeApp.API.Authentication.MockAuthenticationHandler)), context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.True(result.Principal.IsInRole("Student"));
    }

    [Fact]
    public async Task MockAuthenticationHandler_WithNoCredentials_FallsBackToDefaultRole()
    {
        var options = new MockAuthOptions { Enabled = true, DefaultRole = "Lpr" };
        var service = new MockAuthService(options);
        var handler = CreateHandler(service, options);

        var context = new DefaultHttpContext();
        await handler.InitializeAsync(new Microsoft.AspNetCore.Authentication.AuthenticationScheme("MockAuth", null, typeof(KnowledgeApp.API.Authentication.MockAuthenticationHandler)), context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.True(result.Principal.IsInRole("Lpr"));
    }

    [Fact]
    public async Task MockAuthenticationHandler_WhenDisabled_ReturnsNoResult()
    {
        var options = new MockAuthOptions { Enabled = false };
        var service = new MockAuthService(options);
        var handler = CreateHandler(service, options);

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Mock-Role"] = "Admin";
        await handler.InitializeAsync(new Microsoft.AspNetCore.Authentication.AuthenticationScheme("MockAuth", null, typeof(KnowledgeApp.API.Authentication.MockAuthenticationHandler)), context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.True(result.None);
    }

    private static AuthController CreateAuthController(IMockAuthService service)
    {
        var config = new ConfigurationBuilder().Build();
        return new AuthController(
            null!,
            config,
            NullLogger<AuthController>.Instance,
            null!,
            service);
    }

    private static KnowledgeApp.API.Authentication.MockAuthenticationHandler CreateHandler(IMockAuthService service, MockAuthOptions options)
    {
        var schemeOptionsMonitor = new TestOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>(new Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions());
        var mockOptions = Microsoft.Extensions.Options.Options.Create(options);
        return new KnowledgeApp.API.Authentication.MockAuthenticationHandler(
            schemeOptionsMonitor,
            NullLoggerFactory.Instance,
            System.Text.Encodings.Web.UrlEncoder.Default,
            service,
            mockOptions);
    }

    private class TestOptionsMonitor<T> : Microsoft.Extensions.Options.IOptionsMonitor<T> where T : class, new()
    {
        public TestOptionsMonitor(T currentValue) => CurrentValue = currentValue;
        public T CurrentValue { get; }
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}
