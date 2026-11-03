using KnowledgeApp.Application.DTOs.Documents;

namespace KnowledgeApp.Application.Services;

public static class AuditCatalog
{
    public const string ProcessReschedule = "process.reschedule";
    public const string ProcessRollback = "process.rollback";
    public const string UserCreate = "user.create";
    public const string UserRoleChange = "user.role_change";
    public const string UserStatusChange = "user.status_change";
    public const string SettingsUpdate = "settings.update";

    public const string EntityProcess = "process";
    public const string EntityUser = "user";
    public const string EntitySettings = "settings";

    private static readonly IReadOnlyList<(string Code, string Label)> Actions =
    [
        (ProcessReschedule, "Перенос даты процесса"),
        (ProcessRollback, "Откат этапа процесса"),
        (UserCreate, "Создание пользователя"),
        (UserRoleChange, "Смена роли"),
        (UserStatusChange, "Блокировка / активация"),
        (SettingsUpdate, "Изменение настроек")
    ];

    private static readonly IReadOnlyList<(string Code, string Label)> Entities =
    [
        (EntityProcess, "Процесс тестирования"),
        (EntityUser, "Пользователь"),
        (EntitySettings, "Настройки системы")
    ];

    public static string ActionLabel(string code) =>
        Actions.FirstOrDefault(a => a.Code == code).Label ?? code;

    public static string EntityLabel(string code) =>
        Entities.FirstOrDefault(e => e.Code == code).Label ?? code;

    public static List<FilterOptionDto> ActionOptions() =>
        Actions.Select(a => new FilterOptionDto { Value = a.Code, Label = a.Label }).ToList();

    public static List<FilterOptionDto> EntityOptions() =>
        Entities.Select(e => new FilterOptionDto { Value = e.Code, Label = e.Label }).ToList();
}
