using System.Text.Json.Serialization;

namespace KnowledgeApp.API.Authentication;

internal sealed class EuzUserProfile
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
