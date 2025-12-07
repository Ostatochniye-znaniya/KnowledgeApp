namespace KnowledgeApp.API.Contracts;

public class TestingRequest
{
    public int? GroupId { get; set; }
    public int DisciplineId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public string? Status { get; set; }
    public string? ResultOfTesting { get; set; }
    public int? ReportId { get; set; }
    public int? SemesterId { get; set; }
}