using System.Text.Json.Serialization;

namespace KnowledgeApp.API.Contracts.Auth;

public class AuthCallbackRequest
{
    [JsonPropertyName("access")]
    public string? Access { get; set; }

    [JsonPropertyName("refresh")]
    public string? Refresh { get; set; }
}

public class MockRoleSwitchRequest
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
}

public class MockLoginRequest
{
    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

public class MockStatusResponse
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("default_role")]
    public string DefaultRole { get; set; } = string.Empty;

    [JsonPropertyName("header_name")]
    public string HeaderName { get; set; } = string.Empty;

    [JsonPropertyName("current_active_role")]
    public string? CurrentActiveRole { get; set; }

    [JsonPropertyName("available_roles")]
    public List<MockRoleDto> AvailableRoles { get; set; } = new();
}

public class MockRoleDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("role_slug")]
    public string RoleSlug { get; set; } = string.Empty;

    [JsonPropertyName("system_role_name")]
    public string SystemRoleName { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}
