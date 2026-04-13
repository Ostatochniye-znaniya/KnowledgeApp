namespace KnowledgeApp.Infrastructure.Entities;

public partial class Semester
{
    public int Id { get; set; }
    public int SemesterYear { get; set; }
    public int SemesterPart { get; set; }

    public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    public virtual ICollection<Testing> Testings { get; set; } = new List<Testing>();
    public virtual ICollection<RecommendationHistory> RecommendationHistories { get; set; } = new List<RecommendationHistory>();
    public virtual ICollection<TestingOrder> TestingOrders { get; set; } = new List<TestingOrder>();
}
