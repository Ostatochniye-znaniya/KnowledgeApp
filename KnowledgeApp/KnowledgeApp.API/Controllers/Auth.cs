using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using KnowledgeApp.Application.DTOs.MockAuth;
using KnowledgeApp.Application.Services.MockAuth;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.API.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly KnowledgeTestDbContext _context;
    private readonly IMockAuthService _mockAuthService;

    private readonly string _adminApiBase;

    public AuthController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        KnowledgeTestDbContext context,
        IMockAuthService mockAuthService)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _context = context;
        _mockAuthService = mockAuthService;

        _adminApiBase = Environment.GetEnvironmentVariable("ADMIN_API_BASE_URL")
                        ?? _configuration["Auth:AdminApiBaseUrl"]
                        ?? "https://admin.kd.mospolytech.ru";
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] AuthCallbackRequest request)
    {
        // 1. Поддержка MOCK режима
        if (_mockAuthService.IsEnabled)
        {
            var mockUser = ResolveCurrentMockUser();
            if (mockUser != null)
            {
                SetAuthCookies(mockUser);
                return Ok(new { ok = true, user_id = mockUser.Id.ToString() });
            }
        }

        // 2. Реальная авторизация через Admin API
        if (string.IsNullOrWhiteSpace(request.Access) || string.IsNullOrWhiteSpace(request.Refresh))
        {
            return BadRequest(new { detail = "Missing tokens" });
        }

        var (decodedId, _) = DecodeJwt(request.Access);
        var userProfile = await FetchAdminUserProfile(request.Access);
        var userId = decodedId ?? userProfile?.Id ?? "unknown";

        // Синхронизация профиля сотрудника/пользователя в базе данных
        if (userProfile != null && !string.IsNullOrWhiteSpace(userProfile.Email))
        {
            var fullName = $"{userProfile.Surname} {userProfile.Name} {userProfile.Patronymic}".Trim();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userProfile.Email || (!string.IsNullOrEmpty(u.ExternalId) && u.ExternalId == userId));

            if (existingUser == null)
            {
                var newUser = new User
                {
                    ExternalId = userId != "unknown" ? userId : userProfile.Id,
                    Name = fullName,
                    Email = userProfile.Email,
                    Password = string.Empty,
                    StatusId = null,
                    FacultyId = null
                };

                await _context.Users.AddAsync(newUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created new user {Email} with ExternalId {ExternalId} in database", userProfile.Email, newUser.ExternalId);
            }
            else
            {
                bool modified = false;
                if (!string.IsNullOrWhiteSpace(fullName) && existingUser.Name != fullName)
                {
                    existingUser.Name = fullName;
                    modified = true;
                }

                if (string.IsNullOrEmpty(existingUser.ExternalId) && userId != "unknown")
                {
                    existingUser.ExternalId = userId;
                    modified = true;
                }

                if (modified)
                {
                    _context.Users.Update(existingUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated profile for user {Email} (ExternalId: {ExternalId})", existingUser.Email, existingUser.ExternalId);
                }
            }
        }

        string deviceId = Request.Cookies["device_id"] ?? Guid.NewGuid().ToString();

        SetCookie("access_token", request.Access);
        SetCookie("refresh_token", request.Refresh);
        SetCookie("session_id", Guid.NewGuid().ToString());
        SetCookie("device_id", deviceId);

        return Ok(new { ok = true, user_id = userId });
    }

    [HttpGet("me")]

    public async Task<IActionResult> Me([FromQuery] string? role = null)
    {
        // 1. Поддержка MOCK режима
        if (_mockAuthService.IsEnabled)
        {
            var mockUser = ResolveCurrentMockUser(role);
            if (mockUser == null)
            {
                return Unauthorized(new { detail = "MOCK пользователь не найден" });
            }

            // Синхронизируем cookie для веб-интерфейса
            SetAuthCookies(mockUser);

            return Ok(new
            {
                id = mockUser.Id.ToString(),
                external_id = mockUser.ExternalId,
                name = mockUser.FirstName,
                full_name = mockUser.FullName,
                first_name = mockUser.FirstName,
                last_name = mockUser.LastName,
                surname = mockUser.Surname,
                patronymic = mockUser.Patronymic,
                email = mockUser.Email,
                mail_box = mockUser.Email,
                role = mockUser.Role,
                roleSlug = mockUser.RoleSlug,
                roles = mockUser.Roles,
                status_id = mockUser.StatusId,
                status = mockUser.Status,
                faculty_id = mockUser.FacultyId,
                faculty = mockUser.Faculty,
                department_code = mockUser.DepartmentCode,
                study_group = mockUser.StudyGroup,
                access_token = $"mock-access-token-{mockUser.RoleSlug}",
                refresh_token = $"mock-refresh-token-{mockUser.RoleSlug}"
            });
        }

        // 2. Реальная авторизация
        var access = GetTokenFromRequest();

        if (string.IsNullOrWhiteSpace(access))
        {
            return Unauthorized(new { detail = "Not authenticated" });
        }

        var (userId, _) = DecodeJwt(access);

        // Ищем пользователя в нашей БД по external_id
        var dbUser = await _context.Users
            .Include(u => u.Status)
            .Include(u => u.Faculty)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                !string.IsNullOrEmpty(userId) && userId != "unknown" && u.ExternalId == userId);

        // Если в БД пользователь еще не привязан к external_id, верифицируем токен и синхронизируем
        if (dbUser == null)
        {
            var profile = await FetchAdminUserProfile(access);

            if (profile == null)
            {
                _logger.LogInformation("Profile fetch failed with current token, attempting refresh");

                var refreshToken = GetRefreshTokenFromRequest();
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return Unauthorized(new { detail = "Token expired and refresh token missing" });
                }

                var refreshedTokens = await RefreshWithAdminApi(access, refreshToken, userId ?? string.Empty);
                if (refreshedTokens == null || string.IsNullOrWhiteSpace(refreshedTokens.AccessToken))
                {
                    return Unauthorized(new { detail = "Failed to refresh token" });
                }

                access = refreshedTokens.AccessToken;
                SetCookie("access_token", refreshedTokens.AccessToken);
                if (!string.IsNullOrEmpty(refreshedTokens.RefreshToken))
                {
                    SetCookie("refresh_token", refreshedTokens.RefreshToken);
                }

                var (newUserId, _) = DecodeJwt(access);
                if (!string.IsNullOrEmpty(newUserId))
                {
                    userId = newUserId;
                }

                profile = await FetchAdminUserProfile(access);
                if (profile == null)
                {
                    return Unauthorized(new { detail = "Failed to authenticate after token refresh" });
                }
            }

            if (string.IsNullOrEmpty(userId) || userId == "unknown")
            {
                userId = profile.Id;
            }

            if (!string.IsNullOrEmpty(profile.Email))
            {
                dbUser = await _context.Users
                    .Include(u => u.Status)
                    .Include(u => u.Faculty)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == profile.Email || (!string.IsNullOrEmpty(u.ExternalId) && u.ExternalId == userId));
            }

            if (dbUser == null && !string.IsNullOrEmpty(profile.Email))
            {
                var fullName = $"{profile.Surname} {profile.Name} {profile.Patronymic}".Trim();
                dbUser = new User
                {
                    ExternalId = userId,
                    Name = fullName,
                    Email = profile.Email,
                    Password = string.Empty
                };

                await _context.Users.AddAsync(dbUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created user {Email} in local database during /me", profile.Email);
            }
            else if (dbUser != null)
            {
                if (string.IsNullOrEmpty(dbUser.ExternalId) && !string.IsNullOrEmpty(userId) && userId != "unknown")
                {
                    dbUser.ExternalId = userId;
                    _context.Users.Update(dbUser);
                    await _context.SaveChangesAsync();
                }
            }
        }

        if (dbUser == null)
        {
            return Unauthorized(new { detail = "User not found in local database" });
        }

        var roles = dbUser.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrEmpty(ur.Role.RoleName))
            .Select(ur => ur.Role!.RoleName)
            .ToList();

        if (roles.Count == 0)
        {
            roles.Add("Гость");
        }

        var nameParts = (dbUser.Name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string lastName = nameParts.Length > 0 ? nameParts[0] : "";
        string firstName = nameParts.Length > 1 ? nameParts[1] : "";
        string patronymic = nameParts.Length > 2 ? nameParts[2] : "";

        return Ok(new
        {
            id = dbUser.Id,
            external_id = dbUser.ExternalId,
            name = firstName,
            full_name = dbUser.Name,
            first_name = firstName,
            last_name = lastName,
            surname = lastName,
            patronymic = patronymic,
            email = dbUser.Email,
            mail_box = dbUser.Email,
            status_id = dbUser.StatusId,
            status = dbUser.Status?.StatusName,
            faculty_id = dbUser.FacultyId,
            faculty = dbUser.Faculty?.FacultyName,
            roles = roles,
            access_token = access,
            refresh_token = GetRefreshTokenFromRequest()
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] AuthRefreshRequest? request)
    {
        // 1. Поддержка MOCK режима
        if (_mockAuthService.IsEnabled)
        {
            var mockUser = ResolveCurrentMockUser();
            var roleSlug = mockUser?.RoleSlug ?? "admin";
            return Ok(new
            {
                access_token = $"mock-access-token-{roleSlug}",
                refresh_token = $"mock-refresh-token-{roleSlug}"
            });
        }

        // 2. Реальный refresh
        var access = request?.AccessToken ?? GetTokenFromRequest();
        var refresh = request?.RefreshToken ?? GetRefreshTokenFromRequest();
        var userId = request?.UserId;

        if (string.IsNullOrWhiteSpace(access) || string.IsNullOrWhiteSpace(refresh))
        {
            return BadRequest(new { detail = "Missing access or refresh token" });
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            var (decodedId, _) = DecodeJwt(access);
            userId = decodedId ?? "";
        }

        var tokens = await RefreshWithAdminApi(access, refresh, userId);
        if (tokens == null)
        {
            return Unauthorized(new { detail = "Refresh failed" });
        }

        SetCookie("access_token", tokens.AccessToken);
        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            SetCookie("refresh_token", tokens.RefreshToken);
        }

        return Ok(new
        {
            access_token = tokens.AccessToken,
            refresh_token = tokens.RefreshToken
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        ClearCookie("access_token");
        ClearCookie("refresh_token");
        ClearCookie("session_id");
        ClearCookie("device_id");
        ClearCookie("is_authenticated");
        ClearCookie("user_data");
        ClearCookie("mock_role");

        foreach (var cookieKey in Request.Cookies.Keys)
        {
            ClearCookie(cookieKey);
        }

        return Ok(new { ok = true, message = "Успешный выход из системы" });
    }

    /// <summary>
    /// Список всех доступных MOCK ролей в системе.
    /// </summary>
    [HttpGet("mock/roles")]
    public IActionResult GetMockRoles()
    {
        var users = _mockAuthService.GetAllMockUsers();
        var dtos = users.Select(u => new
        {
            id = u.Id,
            role = u.Role,
            role_slug = u.RoleSlug,
            system_role_name = u.SystemRoleName,
            name = u.FullName,
            email = u.Email,
            description = u.Description
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Статус MOCK-авторизации: включена ли она, дефолтная роль, текущая активная роль.
    /// </summary>
    [HttpGet("mock/status")]
    public IActionResult GetMockStatus()
    {
        var currentUser = ResolveCurrentMockUser();
        var users = _mockAuthService.GetAllMockUsers();

        return Ok(new
        {
            enabled = _mockAuthService.IsEnabled,
            default_role = _mockAuthService.DefaultRole,
            header_name = _mockAuthService.HeaderName,
            current_active_role = currentUser?.RoleSlug,
            available_roles = users.Select(u => new
            {
                id = u.Id,
                role = u.Role,
                role_slug = u.RoleSlug,
                system_role_name = u.SystemRoleName,
                name = u.FullName,
                email = u.Email,
                description = u.Description
            }).ToList()
        });
    }

    /// <summary>
    /// Быстрое переключение MOCK роли через GET (?role=teacher) или POST.
    /// </summary>
    [HttpGet("mock/switch")]
    [HttpPost("mock/switch")]
    public IActionResult SwitchRole([FromQuery] string? role = null)
    {
        if (!_mockAuthService.IsEnabled)
        {
            return BadRequest(new { detail = "MOCK авторизация отключена в конфигурации .env" });
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return BadRequest(new { detail = "Укажите роль для переключения, например ?role=teacher" });
        }

        var mockUser = _mockAuthService.GetMockUser(role);
        if (mockUser == null)
        {
            return NotFound(new { detail = $"Роль '{role}' не найдена среди доступных MOCK ролей" });
        }

        SetAuthCookies(mockUser);

        return Ok(new
        {
            success = true,
            message = $"Успешно переключено на роль: {mockUser.Role} ({mockUser.RoleSlug})",
            active_role = mockUser.RoleSlug,
            user = mockUser
        });
    }

    private MockUserProfile? ResolveCurrentMockUser(string? explicitRole = null)
    {
        if (!_mockAuthService.IsEnabled) return null;

        if (!string.IsNullOrWhiteSpace(explicitRole))
        {
            var user = _mockAuthService.GetMockUser(explicitRole);
            if (user != null) return user;
        }

        if (Request.Headers.TryGetValue(_mockAuthService.HeaderName, out var headerRole) && !string.IsNullOrWhiteSpace(headerRole))
        {
            var user = _mockAuthService.GetMockUser(headerRole);
            if (user != null) return user;
        }

        if (Request.Headers.TryGetValue("X-Role", out var xRole) && !string.IsNullOrWhiteSpace(xRole))
        {
            var user = _mockAuthService.GetMockUser(xRole);
            if (user != null) return user;
        }

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var user = _mockAuthService.GetMockUser(token);
            if (user != null) return user;
        }

        if (Request.Cookies.TryGetValue("mock_role", out var cookieRole) && !string.IsNullOrWhiteSpace(cookieRole))
        {
            var user = _mockAuthService.GetMockUser(cookieRole);
            if (user != null) return user;
        }

        return _mockAuthService.GetMockUser(_mockAuthService.DefaultRole);
    }

    private void SetAuthCookies(MockUserProfile user)
    {
        var cookieData = new
        {
            id = user.Id.ToString(),
            external_id = user.ExternalId,
            name = user.Name,
            full_name = user.FullName,
            first_name = user.FirstName,
            last_name = user.LastName,
            surname = user.Surname,
            patronymic = user.Patronymic,
            email = user.Email,
            role = user.Role,
            roleSlug = user.RoleSlug,
            roles = user.Roles,
            status_id = user.StatusId,
            status = user.Status,
            faculty_id = user.FacultyId,
            faculty = user.Faculty
        };

        var json = JsonSerializer.Serialize(cookieData);
        var cookieOptions = new CookieOptions
        {
            Path = "/",
            HttpOnly = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        };

        Response.Cookies.Append("user_data", Uri.EscapeDataString(json), cookieOptions);
        Response.Cookies.Append("is_authenticated", "true", cookieOptions);
        Response.Cookies.Append("mock_role", user.RoleSlug, cookieOptions);
    }

    private string? GetTokenFromRequest()
    {
        if (Request.Cookies.TryGetValue("access_token", out var token) && !string.IsNullOrWhiteSpace(token))
        {
            return token;
        }

        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader["Bearer ".Length..].Trim();
        }

        return null;
    }

    private string? GetRefreshTokenFromRequest()
    {
        if (Request.Cookies.TryGetValue("refresh_token", out var refresh) && !string.IsNullOrWhiteSpace(refresh))
        {
            return refresh;
        }

        return null;
    }

    private void SetCookie(string key, string value)
    {
        Response.Cookies.Append(key, value, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });
    }

    private void ClearCookie(string key)
    {
        Response.Cookies.Delete(key, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        Response.Cookies.Delete(key, new CookieOptions
        {
            HttpOnly = false,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });
    }

    private async Task<UserProfileDto?> FetchAdminUserProfile(string accessToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var endpoint = $"{_adminApiBase}/api/v1/users/me";
            var response = await client.GetAsync(endpoint);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to fetch profile from Admin API: {Error}", errorText);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<UserProfileDto>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while fetching user profile from Admin API");
            return null;
        }
    }

    private async Task<PolytechTokenResponse?> RefreshWithAdminApi(string access, string refresh, string userId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            var endpoint = $"{_adminApiBase}/api/v1/users/refresh";
            var payload = new
            {
                user_id = userId,
                userID = userId,
                access_token = access,
                refresh_token = refresh
            };

            var response = await client.PostAsJsonAsync(endpoint, payload);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to refresh token in Admin API: {Error}", errorText);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new PolytechTokenResponse
            {
                AccessToken = root.TryGetProperty("access_token", out var act) ? act.GetString() ?? "" : "",
                RefreshToken = root.TryGetProperty("refresh_token", out var rft) ? rft.GetString() ?? "" : ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while refreshing token in Admin API");
            return null;
        }
    }

    private static (string? userId, DateTime? exp) DecodeJwt(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token)) return (null, null);

            var jwt = handler.ReadJwtToken(token);
            var userId = jwt.Claims.FirstOrDefault(c =>
                c.Type == "user_id" ||
                c.Type == "sub" ||
                c.Type == "id" ||
                c.Type.EndsWith("/nameidentifier"))?.Value;

            DateTime? exp = null;
            if (jwt.Payload.Expiration.HasValue)
            {
                exp = jwt.ValidTo;
            }
            else if (jwt.Payload.TryGetValue("expires_at", out var expObj))
            {
                if (expObj is JsonElement je && je.ValueKind == JsonValueKind.String && DateTime.TryParse(je.GetString(), out var dt))
                {
                    exp = dt.ToUniversalTime();
                }
                else if (DateTime.TryParse(expObj?.ToString(), out var dt2))
                {
                    exp = dt2.ToUniversalTime();
                }
            }

            return (userId, exp);
        }
        catch
        {
            return (null, null);
        }
    }
}

public class AuthCallbackRequest
{
    [JsonPropertyName("access")]
    public string Access { get; set; } = string.Empty;

    [JsonPropertyName("refresh")]
    public string Refresh { get; set; } = string.Empty;
}

public class UserProfileDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("surname")]
    public string? Surname { get; set; }

    [JsonPropertyName("patronymic")]
    public string? Patronymic { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}

public class AuthRefreshRequest
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
