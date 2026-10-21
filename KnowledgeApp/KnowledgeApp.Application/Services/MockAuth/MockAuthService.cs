using System.Security.Claims;
using KnowledgeApp.Application.DTOs.MockAuth;

namespace KnowledgeApp.Application.Services.MockAuth;

/// <summary>
/// Реализация сервиса MOCK-авторизации со всеми системными ролями.
/// Позволяет проверять права и авторизовываться под любой ролью без сторонних провайдеров.
/// </summary>
public class MockAuthService : IMockAuthService
{
    private readonly MockAuthOptions _options;
    private readonly List<MockUserProfile> _users;

    public bool IsEnabled => _options.Enabled;
    public string DefaultRole => _options.DefaultRole;
    public string HeaderName => _options.HeaderName;

    public MockAuthService(MockAuthOptions options)
    {
        _options = options ?? new MockAuthOptions();
        _users = InitializeMockUsers();
    }

    public IReadOnlyList<MockUserProfile> GetAllMockUsers()
    {
        return _users.AsReadOnly();
    }

    public MockUserProfile? GetMockUser(string? roleIdentifier)
    {
        if (string.IsNullOrWhiteSpace(roleIdentifier))
        {
            roleIdentifier = _options.DefaultRole;
        }

        var normalized = NormalizeIdentifier(roleIdentifier);

        // 1. Поиск по RoleSlug
        var user = _users.FirstOrDefault(u => u.RoleSlug.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (user != null) return user;

        // 2. Поиск по SystemRoleName (английскому названию)
        user = _users.FirstOrDefault(u => u.SystemRoleName.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (user != null) return user;

        // 3. Поиск по русскому названию или подстроке
        user = _users.FirstOrDefault(u => u.Role.Equals(roleIdentifier.Trim(), StringComparison.OrdinalIgnoreCase));
        if (user != null) return user;

        // 4. Поиск по числовому Id (если передан например "1", "2")
        if (int.TryParse(roleIdentifier.Trim(), out var parsedId))
        {
            user = _users.FirstOrDefault(u => u.Id == parsedId);
            if (user != null) return user;
        }

        // 5. Поиск по алиасам
        var matchedSlug = ResolveAliasToSlug(normalized);
        if (matchedSlug != null)
        {
            return _users.FirstOrDefault(u => u.RoleSlug.Equals(matchedSlug, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }

    public MockUserProfile? GetMockUserById(int id)
    {
        return _users.FirstOrDefault(u => u.Id == id);
    }

    public ClaimsPrincipal CreatePrincipal(MockUserProfile user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim("sub", user.Id.ToString()),
            new Claim("user_id", user.Id.ToString()),
            new Claim("external_id", user.ExternalId),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("login", user.Login),
            new Claim("status", user.Status),
            new Claim("status_id", user.StatusId.ToString()),
            new Claim("role_slug", user.RoleSlug)
        };

        if (user.FacultyId.HasValue)
        {
            claims.Add(new Claim("faculty_id", user.FacultyId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(user.Faculty))
        {
            claims.Add(new Claim("faculty", user.Faculty));
        }

        if (user.DepartmentId.HasValue)
        {
            claims.Add(new Claim("department_id", user.DepartmentId.Value.ToString()));
        }

        if (!string.IsNullOrEmpty(user.DepartmentCode))
        {
            claims.Add(new Claim("department_code", user.DepartmentCode));
        }

        if (!string.IsNullOrEmpty(user.StudyGroup))
        {
            claims.Add(new Claim("study_group", user.StudyGroup));
        }

        // Добавляем роли во всех форматах, чтобы [Authorize(Roles = "...")] срабатывал
        // при любой конвенции: как "Admin", так и "admin", и "Администратор"
        AddRoleClaimIfNotExists(claims, user.SystemRoleName);
        AddRoleClaimIfNotExists(claims, user.RoleSlug);
        AddRoleClaimIfNotExists(claims, user.Role);

        foreach (var r in user.Roles)
        {
            AddRoleClaimIfNotExists(claims, r);
        }

        var identity = new ClaimsIdentity(claims, "MockAuth", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    private static void AddRoleClaimIfNotExists(List<Claim> claims, string role)
    {
        if (string.IsNullOrWhiteSpace(role)) return;
        if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value.Equals(role, StringComparison.Ordinal)))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Также добавляем варианты в верхнем и нижнем регистре для максимальной совместимости с [Authorize(Roles = "...")]
        var lower = role.ToLowerInvariant();
        if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value.Equals(lower, StringComparison.Ordinal)))
        {
            claims.Add(new Claim(ClaimTypes.Role, lower));
        }

        var upper = role.ToUpperInvariant();
        if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value.Equals(upper, StringComparison.Ordinal)))
        {
            claims.Add(new Claim(ClaimTypes.Role, upper));
        }
    }

    private static string NormalizeIdentifier(string input)
    {
        var clean = input.Trim().ToLowerInvariant();
        if (clean.StartsWith("mock-", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean.Substring("mock-".Length);
        }
        else if (clean.StartsWith("mock_", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean.Substring("mock_".Length);
        }
        return clean.Trim();
    }

    private static string? ResolveAliasToSlug(string normalized)
    {
        return normalized switch
        {
            "admin" or "administrator" or "админ" or "администратор" => MockRoleType.AdminSlug,
            "lpr" or "лпр" or "лицо, принимающее решения" or "лицо, принимающее решения (лпр)" => MockRoleType.LprSlug,
            "hod" or "зав" or "заведующий" or "заведующий кафедрой" or "руководитель подразделения" 
                or "руководитель подразделения (заведующий кафедрой)" => MockRoleType.HodSlug,
            "teacher" or "преподаватель" or "препод" or "учитель" => MockRoleType.TeacherSlug,
            "rod" or "декан" or "руководитель факультета" or "руководитель института" 
                or "руководитель факультета / института" => MockRoleType.RodSlug,
            "departmentresponsible" or "department_responsible" or "ответственный за подразделение" 
                or "ответственный" or "resp" => MockRoleType.DepartmentResponsibleSlug,
            "student" or "студент" => MockRoleType.StudentSlug,
            "guest" or "гость" => MockRoleType.GuestSlug,
            _ => null
        };
    }

    private static List<MockUserProfile> InitializeMockUsers()
    {
        return new List<MockUserProfile>
        {
            // 1. Администратор
            new MockUserProfile
            {
                Id = 1,
                ExternalId = "mock-admin-001",
                SystemRoleName = MockRoleType.Admin,
                Role = MockRoleType.AdminRu,
                RoleSlug = MockRoleType.AdminSlug,
                Roles = new List<string> { MockRoleType.AdminRu, MockRoleType.AdminSlug, MockRoleType.Admin },
                Name = "Алексей Владимирович Смирнов",
                FullName = "Смирнов Алексей Владимирович",
                FirstName = "Алексей",
                LastName = "Смирнов",
                Surname = "Смирнов",
                Patronymic = "Владимирович",
                Email = "admin.mock@mospolytech.ru",
                Login = "admin_mock",
                Status = "Активен",
                StatusId = 1,
                FacultyId = 1,
                Faculty = "Факультет информационных технологий",
                DepartmentId = 1,
                DepartmentCode = "ИТ-01",
                DepartmentName = "Кафедра программной инженерии",
                Description = "Полный доступ ко всем модулям, администрированию пользователей и отчетам"
            },

            // 2. Лицо, принимающее решения (ЛПР)
            new MockUserProfile
            {
                Id = 2,
                ExternalId = "mock-lpr-002",
                SystemRoleName = MockRoleType.Lpr,
                Role = MockRoleType.LprRu,
                RoleSlug = MockRoleType.LprSlug,
                Roles = new List<string> { MockRoleType.LprRu, MockRoleType.LprSlug, MockRoleType.Lpr, "ЛПР" },
                Name = "Елена Николаевна Кузнецова",
                FullName = "Кузнецова Елена Николаевна",
                FirstName = "Елена",
                LastName = "Кузнецова",
                Surname = "Кузнецова",
                Patronymic = "Николаевна",
                Email = "lpr.mock@mospolytech.ru",
                Login = "lpr_mock",
                Status = "Активен",
                StatusId = 1,
                FacultyId = 1,
                Faculty = "Факультет информационных технологий",
                DepartmentId = 1,
                DepartmentCode = "ИТ-01",
                DepartmentName = "Кафедра программной инженерии",
                Description = "Утверждение результатов, просмотр сводных отчетов и ключевых показателей (ЛПР)"
            },

            // 3. Руководитель подразделения (заведующий кафедрой)
            new MockUserProfile
            {
                Id = 3,
                ExternalId = "mock-hod-003",
                SystemRoleName = MockRoleType.Hod,
                Role = MockRoleType.HodRu,
                RoleSlug = MockRoleType.HodSlug,
                Roles = new List<string> { MockRoleType.HodRu, MockRoleType.HodSlug, MockRoleType.Hod, "Заведующий кафедрой" },
                Name = "Дмитрий Сергеевич Васильев",
                FullName = "Васильев Дмитрий Сергеевич",
                FirstName = "Дмитрий",
                LastName = "Васильев",
                Surname = "Васильев",
                Patronymic = "Сергеевич",
                Email = "hod.mock@mospolytech.ru",
                Login = "hod_mock",
                Status = "Активен",
                StatusId = 1,
                FacultyId = 1,
                Faculty = "Факультет информационных технологий",
                DepartmentId = 2,
                DepartmentCode = "ИТ-02",
                DepartmentName = "Кафедра прикладной информатики",
                Description = "Управление кафедрой, распределение нагрузки преподавателей, согласование графиков"
            },

            // 4. Преподаватель
            new MockUserProfile
            {
                Id = 4,
                ExternalId = "mock-teacher-004",
                SystemRoleName = MockRoleType.Teacher,
                Role = MockRoleType.TeacherRu,
                RoleSlug = MockRoleType.TeacherSlug,
                Roles = new List<string> { MockRoleType.TeacherRu, MockRoleType.TeacherSlug, MockRoleType.Teacher },
                Name = "Михаил Александрович Петров",
                FullName = "Петров Михаил Александрович",
                FirstName = "Михаил",
                LastName = "Петров",
                Surname = "Петров",
                Patronymic = "Александрович",
                Email = "teacher.mock@mospolytech.ru",
                Login = "teacher_mock",
                Status = "Активен",
                StatusId = 1,
                FacultyId = 1,
                Faculty = "Факультет информационных технологий",
                DepartmentId = 2,
                DepartmentCode = "ИТ-02",
                DepartmentName = "Кафедра прикладной информатики",
                Description = "Проведение тестирования, выставление баллов, просмотр своих дисциплин и групп"
            },

            // 5. Руководитель факультета / института (РОД / Декан)
            new MockUserProfile
            {
                Id = 5,
                ExternalId = "mock-rod-005",
                SystemRoleName = MockRoleType.Rod,
                Role = MockRoleType.RodRu,
                RoleSlug = MockRoleType.RodSlug,
                Roles = new List<string> { MockRoleType.RodRu, MockRoleType.RodSlug, MockRoleType.Rod },
                Name = "Ольга Игоревна Морозова",
                FullName = "Морозова Ольга Игоревна",
                FirstName = "Ольга",
                LastName = "Морозова",
                Surname = "Морозова",
                Patronymic = "Игоревна",
                Email = "rod.mock@mospolytech.ru",
                Login = "rod_mock",
                Status = "Активен",
                StatusId = 1,
                FacultyId = 1,
                Faculty = "Факультет информационных технологий",
                DepartmentId = 1,
                DepartmentCode = "ИТ-01",
                DepartmentName = "Кафедра программной инженерии",
                Description = "Контроль успеваемости на уровне факультета/института, сводная статистика"
            },

            // 6. Ответственный за подразделение
            new MockUserProfile
            {
                Id = 6,
                ExternalId = "mock-resp-006",
                SystemRoleName = MockRoleType.DepartmentResponsible,
                Role = MockRoleType.DepartmentResponsibleRu,
                RoleSlug = MockRoleType.DepartmentResponsibleSlug,
                Roles = new List<string> { MockRoleType.DepartmentResponsibleRu, MockRoleType.DepartmentResponsibleSlug, "hod" },
                Name = "Сергей Павлович Воронов",
                FullName = "Воронов Сергей Павлович",
                FirstName = "Сергей",
                LastName = "Воронов",
                Surname = "Воронов",
                Patronymic = "Павлович",
                Email = "resp.mock@mospolytech.ru",
                Login = "resp_mock",
                Status = "Активен",
                StatusId = 1,
                FacultyId = 1,
                Faculty = "Факультет информационных технологий",
                DepartmentId = 2,
                DepartmentCode = "ИТ-02",
                DepartmentName = "Кафедра прикладной информатики",
                Description = "Координация расписания и тестирования в подразделении"
            },

            // 7. Студент
            new MockUserProfile
            {
                Id = 7,
                ExternalId = "mock-student-007",
                SystemRoleName = MockRoleType.Student,
                Role = MockRoleType.StudentRu,
                RoleSlug = MockRoleType.StudentSlug,
                Roles = new List<string> { MockRoleType.StudentRu, MockRoleType.StudentSlug, MockRoleType.Student },
                Name = "Иван Сергеевич Федоров",
                FullName = "Федоров Иван Сергеевич",
                FirstName = "Иван",
                LastName = "Федоров",
                Surname = "Федоров",
                Patronymic = "Сергеевич",
                Email = "student.mock@mospolytech.ru",
                Login = "student_mock",
                Status = "Активен",
                StatusId = 1,
                FacultyId = 1,
                Faculty = "Факультет информационных технологий",
                DepartmentId = 2,
                DepartmentCode = "ИТ-02",
                DepartmentName = "Кафедра прикладной информатики",
                StudyGroup = "221-321",
                Description = "Прохождение тестирования, просмотр личных результатов и расписания"
            },

            // 8. Гость
            new MockUserProfile
            {
                Id = 8,
                ExternalId = "mock-guest-008",
                SystemRoleName = MockRoleType.Guest,
                Role = MockRoleType.GuestRu,
                RoleSlug = MockRoleType.GuestSlug,
                Roles = new List<string> { MockRoleType.GuestRu, MockRoleType.GuestSlug, MockRoleType.Guest },
                Name = "Гостевой Пользователь",
                FullName = "Гостевой Пользователь",
                FirstName = "Гость",
                LastName = "Пользователь",
                Surname = "Пользователь",
                Patronymic = null,
                Email = "guest.mock@mospolytech.ru",
                Login = "guest_mock",
                Status = "Активен",
                StatusId = 1,
                FacultyId = null,
                Faculty = null,
                DepartmentId = null,
                DepartmentCode = null,
                DepartmentName = null,
                Description = "Базовый доступ гостя: подача заявки на получение роли сотрудника"
            }
        };
    }
}
