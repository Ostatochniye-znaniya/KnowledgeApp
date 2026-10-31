using System.Text.Encodings.Web;
using KnowledgeApp.Application.DTOs.MockAuth;
using KnowledgeApp.Application.Services.MockAuth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.API.Authentication;

/// <summary>
/// Обработчик схемы MOCK-авторизации.
/// Автоматически извлекает указанную роль из заголовков, токена Bearer, query-параметров или cookies,
/// и формирует ClaimsPrincipal с правами запрошенной роли.
/// </summary>
public class MockAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IMockAuthService _mockAuthService;
    private readonly MockAuthOptions _options;

    public MockAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IMockAuthService mockAuthService,
        IOptions<MockAuthOptions> options)
        : base(schemeOptions, logger, encoder)
    {
        _mockAuthService = mockAuthService;
        _options = options.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Если MOCK-авторизация отключена в .env — пропускаем аутентификацию
        if (!_mockAuthService.IsEnabled)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? roleCandidate = null;

        // 1. Проверяем кастомный заголовок (по умолчанию X-Mock-Role)
        if (!string.IsNullOrWhiteSpace(_options.HeaderName) &&
            Request.Headers.TryGetValue(_options.HeaderName, out var customHeaderVal) &&
            !string.IsNullOrWhiteSpace(customHeaderVal))
        {
            roleCandidate = customHeaderVal.ToString();
        }
        // 2. Проверяем стандартный заголовок X-Role
        else if (Request.Headers.TryGetValue("X-Role", out var xRoleVal) && !string.IsNullOrWhiteSpace(xRoleVal))
        {
            roleCandidate = xRoleVal.ToString();
        }
        // 3. Проверяем Authorization: Bearer mock-{role} или Bearer {role}
        else if (Request.Headers.TryGetValue("Authorization", out var authHeaderVal))
        {
            var authHeader = authHeaderVal.ToString().Trim();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                if (!string.IsNullOrWhiteSpace(token))
                {
                    roleCandidate = token;
                }
            }
        }

        // 4. Проверяем query-параметры (?mock_role=... или ?role=...)
        if (string.IsNullOrWhiteSpace(roleCandidate))
        {
            if (Request.Query.TryGetValue("mock_role", out var qMockRole) && !string.IsNullOrWhiteSpace(qMockRole))
            {
                roleCandidate = qMockRole.ToString();
            }
            else if (Request.Query.TryGetValue("role", out var qRole) && !string.IsNullOrWhiteSpace(qRole))
            {
                roleCandidate = qRole.ToString();
            }
        }

        // 5. Проверяем cookies (mock_role или user_role)
        if (string.IsNullOrWhiteSpace(roleCandidate))
        {
            if (Request.Cookies.TryGetValue("mock_role", out var cMockRole) && !string.IsNullOrWhiteSpace(cMockRole))
            {
                roleCandidate = cMockRole;
            }
            else if (Request.Cookies.TryGetValue("user_role", out var cUserRole) && !string.IsNullOrWhiteSpace(cUserRole))
            {
                roleCandidate = cUserRole;
            }
        }

        // 6. Если роль не была явно передана — используем дефолтную роль из .env (по умолчанию Admin)
        if (string.IsNullOrWhiteSpace(roleCandidate))
        {
            roleCandidate = _options.DefaultRole;
        }

        var mockUser = _mockAuthService.GetMockUser(roleCandidate)
                       ?? _mockAuthService.GetMockUser(_options.DefaultRole);

        if (mockUser == null)
        {
            return Task.FromResult(AuthenticateResult.Fail($"Роль MOCK авторизации '{roleCandidate}' не найдена."));
        }

        var principal = _mockAuthService.CreatePrincipal(mockUser);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
