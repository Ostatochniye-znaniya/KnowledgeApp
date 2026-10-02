using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

[ApiController]
[Route("auth_polytech")]
public class AuthPolytechController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthPolytechController> _logger;

    private readonly string _authApiUrl;

    public AuthPolytechController(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AuthPolytechController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;

        _authApiUrl = Environment.GetEnvironmentVariable("AUTH_API_URL")
                      ?? _configuration["Auth:AuthApiUrl"]
                      ?? "https://admin.kd.mospolytech.ru/api/v1/users";
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] PolytechLoginRequest request)
    {


        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { detail = "Логин и пароль обязательны" });
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var endpoint = $"{_authApiUrl}/login";
        var payload = new
        {
            login = request.Login,
            raw_password = request.Password,
            service_name = "empl_eval_sys"
        };

        try
        {
            var response = await client.PostAsJsonAsync(endpoint, payload);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auth API login error ({StatusCode}): {Response}", response.StatusCode, responseText);
                return StatusCode((int)response.StatusCode, new { detail = $"Auth API error: {responseText}" });
            }

            return Ok(new Dictionary<string, string> { { "status", "OK" } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Auth API login");
            return StatusCode(500, new { detail = $"Failed to communicate with Auth API: {ex.Message}" });
        }
    }

    [HttpPost("verify")]
    public async Task<IActionResult> Verify([FromBody] PolytechVerifyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login))
        {
            return BadRequest(new { detail = "Логин обязателен" });
        }

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        var endpoint = $"{_authApiUrl}/verification_auth_code";

        string codeValue = request.Code.ValueKind switch
        {
            JsonValueKind.String => request.Code.GetString() ?? string.Empty,
            JsonValueKind.Number => request.Code.GetRawText(),
            _ => request.Code.GetString() ?? request.Code.GetRawText()
        };

        var payload = new
        {
            login = request.Login,
            code = codeValue,
            service_name = "empl_eval_sys"
        };

        try
        {
            var response = await client.PostAsJsonAsync(endpoint, payload);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Auth API verify error ({StatusCode}): {Response}", response.StatusCode, responseText);
                return StatusCode((int)response.StatusCode, new { detail = $"Auth API error: {responseText}" });
            }

            using var doc = JsonDocument.Parse(responseText);
            var root = doc.RootElement;

            string userId = root.TryGetProperty("user_id", out var uid) ? uid.GetString() ?? "" : "";
            string accessToken = root.TryGetProperty("access_token", out var act) ? act.GetString() ?? "" : "";
            string refreshToken = root.TryGetProperty("refresh_token", out var rft) ? rft.GetString() ?? "" : "";

            return Ok(new PolytechTokenResponse
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Auth API verify");
            return StatusCode(500, new { detail = $"Failed to communicate with Auth API: {ex.Message}" });
        }
    }
}

public class PolytechLoginRequest
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

}

public class PolytechVerifyRequest
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public JsonElement Code { get; set; }

}

public class PolytechTokenResponse
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
}
