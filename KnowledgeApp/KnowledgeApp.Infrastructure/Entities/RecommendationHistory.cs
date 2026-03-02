namespace KnowledgeApp.Infrastructure.Entities;

public partial class RecommendationHistory
{
    public int Id { get; set; }
    public DateTime RecommendedAt { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public int SemesterId { get; set; }
    public Semester? Semester { get; set; }
    public int StudyGroupId { get; set; }
    public StudyGroup? StudyGroup { get; set; }
}
