namespace KnowledgeApp.Infrastructure.Entities;

// Одно итоговое решение кафедры на профиль и семестр; рекомендация ЛПР остаётся отдельно.
public sealed class GroupParticipationDecision
{
    public int Id { get; set; }
    public int StudyProgramId { get; set; }
    public int SemesterId { get; set; }
    public int? RecommendedGroupId { get; set; }
    public int? SelectedGroupId { get; set; }
    public string? Reason { get; set; }
    public int? DecidedByUserId { get; set; }
    public DateTime DecidedAt { get; set; }
}
