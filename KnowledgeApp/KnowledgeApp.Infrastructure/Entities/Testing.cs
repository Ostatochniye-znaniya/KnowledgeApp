namespace KnowledgeApp.Infrastructure.Entities;

public partial class Testing
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public int? RecommendationHistoryId { get; set; }
    public virtual RecommendationHistory? RecommendationHistory { get; set; }

    public int? DisciplineId { get; set; }

    public int? SemesterId { get; set; }

    public DateTime? ScheduledDate { get; set; }

    public TimeSpan? ScheduledTime { get; set; }

    public int? ScheduledPair { get; set; }

    public DateTime? SelectionPeriodStart { get; set; }

    public DateTime? SelectionPeriodEnd { get; set; }

    /// <summary>Когда кафедра группы отправила заявку кафедре другого факультета (дисциплина чужого факультета).</summary>
    public DateTime? ExternalRequestSentAt { get; set; }

    public string? Room { get; set; }

    public string? LmsUrl { get; set; }

    public bool? IsInPerson { get; set; }

    public string? Status { get; set; }

    public string? ResultOfTesting { get; set; }

    public int? ReportId { get; set; }

    public virtual Discipline? Discipline { get; set; }

    public virtual StudyGroup? Group { get; set; }

    public virtual Report? Report { get; set; }
}
