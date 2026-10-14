using KnowledgeApp.Application.Interfaces;

namespace KnowledgeApp.API.Auth;

// заглушка, пока нет авторизации
public class TestCurrentUserProvider : ICurrentUserProvider
{
    public int? UserId { get; } = ReadInt("TEST_USER_ID");

    public int? FacultyId { get; } = ReadInt("TEST_FACULTY_ID") ?? 1;

    // кафедра заведующего кафедрой; 4 — ИиИТ в тестовом сиде
    public int? DepartmentId { get; } = ReadInt("TEST_DEPARTMENT_ID") ?? 4;

    private static int? ReadInt(string variable) =>
        int.TryParse(Environment.GetEnvironmentVariable(variable), out var value) && value > 0 ? value : null;
}
