namespace KnowledgeApp.API.Contracts;

public class RecommendationHistoryResponse
{
    public string Author { get; set; } = "manual";
    public int? RecommendedById { get; set; }
    public int Id { get; set; }
    public DateTime RecommendedAt { get; set; }
    public string? RecommendedBy { get; set; } = string.Empty;
    public int SemesterId { get; set; }
    public string? SemesterName { get; set; } = string.Empty;
    public int StudyGroupId { get; set; }
    public string? StudyGroupName { get; set; } = string.Empty;
}
