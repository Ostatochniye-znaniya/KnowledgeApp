namespace KnowledgeApp.Application.DTOs.MockAuth;

/// <summary>
/// Полный профиль пользователя для MOCK-авторизации.
/// Содержит все поля, необходимые для API и фронтенда.
/// </summary>
public class MockUserProfile
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;              // Русскоязычное название: "Администратор", "Преподаватель"
    public string RoleSlug { get; set; } = string.Empty;          // Slug-код: "admin", "teacher", "hod"
    public string SystemRoleName { get; set; } = string.Empty;    // Английское системное имя: "Admin", "Teacher"
    public List<string> Roles { get; set; } = new();             // Список ролей для совместимости
    public int StatusId { get; set; } = 1;
    public string Status { get; set; } = "Активен";
    public int? FacultyId { get; set; }
    public string? Faculty { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentCode { get; set; }
    public string? DepartmentName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Login { get; set; } = string.Empty;
    public string? StudyGroup { get; set; }
    public string Description { get; set; } = string.Empty;
}
