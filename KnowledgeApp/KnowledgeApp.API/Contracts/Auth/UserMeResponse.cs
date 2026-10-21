using System.Text.Json.Serialization;

namespace KnowledgeApp.API.Contracts.Auth;

/// <summary>
/// Формат ответа профиля пользователя для /auth/me и /users/me,
/// полностью совместимый с интерфейсом UserMeResponse во фронтенде (Next.js).
/// </summary>
public class UserMeResponse
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("roles")]
    public List<string>? Roles { get; set; }

    [JsonPropertyName("status_id")]
    public int? StatusId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("faculty_id")]
    public int? FacultyId { get; set; }

    [JsonPropertyName("faculty")]
    public string? Faculty { get; set; }

    [JsonPropertyName("external_role")]
    public string? ExternalRole { get; set; }

    [JsonPropertyName("type_")]
    public string? Type_ { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; set; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; set; }

    [JsonPropertyName("surname")]
    public string? Surname { get; set; }

    [JsonPropertyName("patronymic")]
    public string? Patronymic { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("mail_box")]
    public string? MailBox { get; set; }

    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("last_login")]
    public string? LastLogin { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("sex")]
    public string? Sex { get; set; }

    [JsonPropertyName("study_status")]
    public string? StudyStatus { get; set; }

    [JsonPropertyName("degree_level")]
    public string? DegreeLevel { get; set; }

    [JsonPropertyName("study_group")]
    public string? StudyGroup { get; set; }

    [JsonPropertyName("specialization")]
    public string? Specialization { get; set; }

    [JsonPropertyName("finance")]
    public string? Finance { get; set; }

    [JsonPropertyName("form")]
    public string? Form { get; set; }

    [JsonPropertyName("enter_year")]
    public string? EnterYear { get; set; }

    [JsonPropertyName("course")]
    public string? Course { get; set; }

    [JsonPropertyName("department_code")]
    public string? DepartmentCode { get; set; }

    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("detail")]
    public object? Detail { get; set; }
}
