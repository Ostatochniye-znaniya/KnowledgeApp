namespace KnowledgeApp.Infrastructure.Entities;

// Одна строка — маркер запуска подбора и блокировка параллельных записей ЛПР.
// Пустой результат автоподбора тоже является сформированным планом.
public sealed class TestingPlanningState
{
    public int Id { get; set; }
    public int? CurrentSemesterId { get; set; }
    public DateTime? InitializedAt { get; set; }
    public long Version { get; set; }
}
