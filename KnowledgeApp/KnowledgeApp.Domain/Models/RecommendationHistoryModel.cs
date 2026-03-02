namespace KnowledgeApp.Domain.Entities;

public class RecommendationHistoryModel
{
    public int Id { get; set; }
    public DateTime RecommendedAt { get; set; }
    public string? RecommendedBy { get; set; }
    public int? RecommendedById { get; set; }
    public int SemesterId { get; set; }
    public int StudyGroupId { get; set; }
    public string? SemesterName { get; set; }
    public string? StudyGroupName { get; set; }
}
