namespace KnowledgeApp.Application.DTOs.MockAuth;

/// <summary>
/// Системные роли пользователей для MOCK-авторизации.
/// Включает все роли, поддерживаемые системой и базой данных.
/// </summary>
public static class MockRoleType
{
    public const string Admin = "Admin";
    public const string Lpr = "Lpr";
    public const string Hod = "Hod";
    public const string Teacher = "Teacher";
    public const string Rod = "Rod";
    public const string DepartmentResponsible = "DepartmentResponsible";
    public const string Student = "Student";
    public const string Guest = "Guest";

    // Русскоязычные наименования
    public const string AdminRu = "Администратор";
    public const string LprRu = "Лицо, принимающее решения (ЛПР)";
    public const string HodRu = "Руководитель подразделения (заведующий кафедрой)";
    public const string TeacherRu = "Преподаватель";
    public const string RodRu = "Руководитель факультета / института";
    public const string DepartmentResponsibleRu = "Ответственный за подразделение";
    public const string StudentRu = "Студент";
    public const string GuestRu = "Гость";

    // Слоги для frontend и url
    public const string AdminSlug = "admin";
    public const string LprSlug = "lpr";
    public const string HodSlug = "hod";
    public const string TeacherSlug = "teacher";
    public const string RodSlug = "rod";
    public const string DepartmentResponsibleSlug = "department_responsible";
    public const string StudentSlug = "student";
    public const string GuestSlug = "guest";
}
