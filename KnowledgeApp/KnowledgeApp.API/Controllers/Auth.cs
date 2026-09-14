using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    private readonly string _adminApiBase;

    public AuthController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AuthController> logger,
        KnowledgeTestDbContext context)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _context = context;

        _adminApiBase = Environment.GetEnvironmentVariable("ADMIN_API_BASE_URL")
                        ?? _configuration["Auth:AdminApiBaseUrl"]
                        ?? "https://admin.kd.mospolytech.ru";
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] AuthCallbackRequest request)
    {
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
                    AccessToken = request.Access,
                    RefreshToken = request.Refresh,
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

                if (existingUser.AccessToken != request.Access || existingUser.RefreshToken != request.Refresh)
                {
                    existingUser.AccessToken = request.Access;
                    existingUser.RefreshToken = request.Refresh;
                    modified = true;
                }

                if (modified)
                {
                    _context.Users.Update(existingUser);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Updated tokens and profile for user {Email} (ExternalId: {ExternalId})", existingUser.Email, existingUser.ExternalId);
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
    public async Task<IActionResult> Me()
    {
        var access = GetTokenFromRequest();

        if (string.IsNullOrWhiteSpace(access))
        {
            return Unauthorized(new { detail = "Not authenticated" });
        }

        var (userId, _) = DecodeJwt(access);

        // 1. Ищем пользователя в нашей БД по access_token или external_id
        var dbUser = await _context.Users
            .Include(u => u.Status)
            .Include(u => u.Faculty)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                (!string.IsNullOrEmpty(u.AccessToken) && u.AccessToken == access) ||
                (!string.IsNullOrEmpty(userId) && userId != "unknown" && u.ExternalId == userId));

        // 2. Если в БД пользователь еще не привязан к токену/external_id, верифицируем токен и синхронизируем
        if (dbUser == null)
        {
            var profile = await FetchAdminUserProfile(access);

            // Если внешний API отклонил токен, пробуем сделать refresh
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

            // Ищем пользователя в нашей БД по email
            if (!string.IsNullOrEmpty(profile.Email))
            {
                dbUser = await _context.Users
                    .Include(u => u.Status)
                    .Include(u => u.Faculty)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email == profile.Email || (!string.IsNullOrEmpty(u.ExternalId) && u.ExternalId == userId));
            }

            // Если пользователя еще нет в нашей БД — создаем
            if (dbUser == null && !string.IsNullOrEmpty(profile.Email))
            {
                var fullName = $"{profile.Surname} {profile.Name} {profile.Patronymic}".Trim();
                dbUser = new User
                {
                    ExternalId = userId,
                    Name = fullName,
                    Email = profile.Email,
                    Password = string.Empty,
                    AccessToken = access,
                    RefreshToken = GetRefreshTokenFromRequest()
                };

                await _context.Users.AddAsync(dbUser);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created user {Email} in local database during /me", profile.Email);
            }
            else if (dbUser != null)
            {
                // Привязываем external_id, access_token и refresh_token к существующему пользователю
                bool modified = false;
                if (string.IsNullOrEmpty(dbUser.ExternalId) && !string.IsNullOrEmpty(userId))
                {
                    dbUser.ExternalId = userId;
                    modified = true;
                }
                if (dbUser.AccessToken != access)
                {
                    dbUser.AccessToken = access;
                    modified = true;
                }
                var currentRefresh = GetRefreshTokenFromRequest();
                if (!string.IsNullOrEmpty(currentRefresh) && dbUser.RefreshToken != currentRefresh)
                {
                    dbUser.RefreshToken = currentRefresh;
                    modified = true;
                }
                if (modified)
                {
                    _context.Users.Update(dbUser);
                    await _context.SaveChangesAsync();
                }
            }
        }
        else
        {
            // Если пользователь найден сразу в БД, проверяем актуальность токенов
            var currentRefresh = GetRefreshTokenFromRequest();
            bool modified = false;
            if (dbUser.AccessToken != access)
            {
                dbUser.AccessToken = access;
                modified = true;
            }
            if (!string.IsNullOrEmpty(currentRefresh) && dbUser.RefreshToken != currentRefresh)
            {
                dbUser.RefreshToken = currentRefresh;
                modified = true;
            }
            if (modified)
            {
                _context.Users.Update(dbUser);
                await _context.SaveChangesAsync();
            }
        }

        if (dbUser == null)
        {
            return Unauthorized(new { detail = "User not found in local database" });
        }

        // Получение ролей из нашей БД
        var roles = dbUser.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrEmpty(ur.Role.RoleName))
            .Select(ur => ur.Role!.RoleName)
            .ToList();

        if (roles.Count == 0)
        {
            roles.Add("Гость");
        }

        // Разбор ФИО из нашей БД
        var nameParts = (dbUser.Name ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string lastName = nameParts.Length > 0 ? nameParts[0] : "";
        string firstName = nameParts.Length > 1 ? nameParts[1] : "";
        string surname = nameParts.Length > 2 ? nameParts[2] : "";

        // Возвращаем данные о пользователе и токены из НАШЕЙ базы данных
        return Ok(new
        {
            id = dbUser.Id,
            external_id = dbUser.ExternalId,
            name = dbUser.Name,
            first_name = firstName,
            last_name = lastName,
            surname = surname,
            email = dbUser.Email,
            mail_box = dbUser.Email,
            status_id = dbUser.StatusId,
            status = dbUser.Status?.StatusName,
            faculty_id = dbUser.FacultyId,
            faculty = dbUser.Faculty?.FacultyName,
            roles = roles,
            access_token = dbUser.AccessToken,
            refresh_token = dbUser.RefreshToken
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] AuthRefreshRequest? request)
    {
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

        // Обновляем токены в БД
        var userInDb = await _context.Users.FirstOrDefaultAsync(u => (!string.IsNullOrEmpty(u.ExternalId) && u.ExternalId == userId) || u.AccessToken == access);
        if (userInDb != null)
        {
            userInDb.AccessToken = tokens.AccessToken;
            if (!string.IsNullOrEmpty(tokens.RefreshToken))
            {
                userInDb.RefreshToken = tokens.RefreshToken;
            }
            _context.Users.Update(userInDb);
            await _context.SaveChangesAsync();
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

        return Ok(new { ok = true });
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
            Path = "/"
        });
    }

    private void ClearCookie(string key)
    {
        Response.Cookies.Delete(key, new CookieOptions
        {
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
