using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;

namespace KnowledgeApp.IntegrationTests.Infrastructure;

/// <summary>
/// Минимальный набор справочников по образцу DbBackups/seed_data_updated.sql.
/// Id пользователей совпадают с Id MOCK-пользователей (MockAuthService).
/// </summary>
public static class TestSeed
{
    public const int ItFacultyId = 1;
    public const int BasicFacultyId = 2;

    public const int IctDepartmentId = 1;
    public const int SmartDepartmentId = 2;
    public const int IiitDepartmentId = 4;
    public const int FbkDepartmentId = 6;

    public const int CurrentSemesterId = 4;
    public const int WebProgramId = 1;
    public const int WebGroupId = 1;
    public const string WebGroupNumber = "221-321";

    public const int AdminUserId = 1;
    public const int LprUserId = 2;
    public const int HodUserId = 3;
    public const int TeacherUserId = 4;

    public static readonly (int Id, string Name)[] Roles =
    {
        (1, "Администратор"),
        (2, "Лицо, принимающее решения (ЛПР)"),
        (3, "Руководитель подразделения (заведующий кафедрой)"),
        (4, "Преподаватель"),
        (5, "Руководитель факультета / института"),
        (6, "Ответственный за подразделение"),
        (7, "Гость"),
    };

    public static async Task SeedReferenceDataAsync(KnowledgeTestDbContext db)
    {
        db.Statuses.AddRange(
            new Status { Id = 1, StatusName = "Активен" },
            new Status { Id = 2, StatusName = "Неактивен" },
            new Status { Id = 3, StatusName = "Заблокирован" });

        db.Roles.AddRange(Roles.Select(r => new Role { Id = r.Id, RoleName = r.Name }));

        db.Faculties.AddRange(
            new Faculty { Id = ItFacultyId, FacultyName = "Факультет информационных технологий" },
            new Faculty { Id = BasicFacultyId, FacultyName = "Факультет базовых компетенций" });

        db.Departments.AddRange(
            new Department { Id = IctDepartmentId, Name = "ИКТ", FacultyId = ItFacultyId },
            new Department { Id = SmartDepartmentId, Name = "СМАРТ", FacultyId = ItFacultyId },
            new Department { Id = IiitDepartmentId, Name = "ИиИТ", FacultyId = ItFacultyId },
            new Department { Id = FbkDepartmentId, Name = "ФБК", FacultyId = BasicFacultyId });

        db.Semesters.AddRange(
            new Semester { Id = 3, SemesterYear = 2025, SemesterPart = 1, YearPart = 2 },
            new Semester { Id = CurrentSemesterId, SemesterYear = 2026, SemesterPart = 2, YearPart = 1 });

        db.StudyPrograms.Add(new StudyProgram
        {
            Id = WebProgramId,
            Name = "Веб-технологии",
            DepartmentId = IiitDepartmentId,
            CypherOfTheDirection = "09.03.01"
        });

        db.StudyGroups.Add(new StudyGroup { Id = WebGroupId, GroupNumber = WebGroupNumber, StudyProgramId = WebProgramId });

        db.Users.AddRange(
            NewUser(AdminUserId, "Админов Админ Админович", "admin.mock@mospolytech.ru", IctDepartmentId),
            NewUser(LprUserId, "Решалов Пётр Иванович", "lpr.mock@mospolytech.ru", IctDepartmentId),
            NewUser(HodUserId, "Заведующий Иван Петрович", "hod.mock@mospolytech.ru", SmartDepartmentId),
            NewUser(TeacherUserId, "Преподавателев Олег Сергеевич", "teacher.mock@mospolytech.ru", SmartDepartmentId));

        db.UserRoles.AddRange(
            new UserRole { UserId = AdminUserId, RoleId = 1 },
            new UserRole { UserId = LprUserId, RoleId = 2 },
            new UserRole { UserId = HodUserId, RoleId = 3 },
            new UserRole { UserId = TeacherUserId, RoleId = 4 });

        await db.SaveChangesAsync();
    }

    private static User NewUser(int id, string name, string email, int departmentId) => new()
    {
        Id = id,
        Name = name,
        Email = email,
        Password = string.Empty,
        StatusId = 1,
        FacultyId = ItFacultyId,
        DepartmentId = departmentId
    };
}
