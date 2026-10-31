namespace KnowledgeApp.Infrastructure.Entities;

public partial class RecommendationHistory
{
    public int Id { get; set; }
    public DateTime RecommendedAt { get; set; }
    public string Author { get; set; } = "manual";
    public int? UserId { get; set; }
    public User? User { get; set; }
    public int SemesterId { get; set; }
    public Semester? Semester { get; set; }
    public int StudyGroupId { get; set; }
    public StudyGroup? StudyGroup { get; set; }
    public ICollection<Testing> Testings { get; set; } = new List<Testing>();
}
