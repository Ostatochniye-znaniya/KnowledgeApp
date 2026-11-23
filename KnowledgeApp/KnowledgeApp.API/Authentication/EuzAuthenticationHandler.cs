using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Infrastructure.Context;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.API.Authentication;

public sealed class EuzAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IHttpClientFactory httpClients,
    KnowledgeTestDbContext db) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Euz";
    public const string ProfileClientName = "EuzProfile";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var accessToken = ReadAccessToken(Request);
        if (accessToken is null)
            return AuthenticateResult.NoResult();

        EuzUserProfile? profile;
        try
        {
            using var client = httpClients.CreateClient(ProfileClientName);
            profile = await GetProfileAsync(client, accessToken, Context.RequestAborted);
        }
        catch (Exception e) when (!Context.RequestAborted.IsCancellationRequested &&
            e is HttpRequestException or OperationCanceledException or JsonException or FormatException)
        {
            Logger.LogWarning("Не удалось проверить сессию в ЕУЗ: {ErrorType}", e.GetType().Name);
            return AuthenticateResult.Fail("Не удалось проверить сессию в ЕУЗ.");
        }

        if (profile is null)
            return AuthenticateResult.Fail("ЕУЗ не подтвердил сессию.");

        // Используем тот же внешний идентификатор, что и при входе. JWT читается только после
        // того, как ЕУЗ принял этот токен и вернул профиль.
        var externalId = ReadExternalId(accessToken) ?? profile.Id;
        if (string.IsNullOrWhiteSpace(externalId) || externalId == "unknown")
            return AuthenticateResult.Fail("ЕУЗ не вернул идентификатор пользователя.");

        var users = await db.Users.AsNoTracking()
            .Where(u => u.ExternalId == externalId)
            .Select(u => new
            {
                u.Id, u.Name, u.FacultyId, u.DepartmentId,
                Status = u.Status != null ? u.Status.StatusName : null,
                Roles = u.UserRoles.Where(r => r.Role != null).Select(r => r.Role!.RoleName).ToList()
            })
            .Take(2)
            .ToListAsync(Context.RequestAborted);

        if (users.Count != 1 || AdminUserStatus.CodeOf(users[0].Status) == AdminUserStatus.Blocked)
            return AuthenticateResult.Fail("Учётная запись приложения недоступна.");

        var user = users[0];
        var userId = user.Id.ToString(CultureInfo.InvariantCulture);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("user_id", userId),
            new(ClaimTypes.Name, user.Name ?? string.Empty)
        };

        if (user.DepartmentId is int departmentId)
            claims.Add(new("department_id", departmentId.ToString(CultureInfo.InvariantCulture)));
        if (user.FacultyId is int facultyId)
            claims.Add(new("faculty_id", facultyId.ToString(CultureInfo.InvariantCulture)));
        claims.AddRange(user.Roles.Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => new Claim(ClaimTypes.Role, r!.Trim())));

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }

    internal static string? ReadAccessToken(HttpRequest request)
    {
        if (request.Cookies.TryGetValue("access_token", out var cookie) && !string.IsNullOrWhiteSpace(cookie))
            return cookie;

        return AuthenticationHeaderValue.TryParse(request.Headers.Authorization.ToString(), out var header)
            && header.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(header.Parameter)
                ? header.Parameter : null;
    }

    internal static string? ReadExternalId(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return null;

            return handler.ReadJwtToken(token).Claims.FirstOrDefault(c =>
                c.Type is "user_id" or "sub" or "id" || c.Type.EndsWith("/nameidentifier", StringComparison.Ordinal))?.Value;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    internal static async Task<EuzUserProfile?> GetProfileAsync(
        HttpClient client, string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/v1/users/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<EuzUserProfile>(cancellationToken)
            : null;
    }
}
