namespace KnowledgeApp.Core.Models;

public class TestingModel
{
    public TestingModel(
        int? groupId,
        int? disciplineId,
        DateTime? scheduledDate,
        TimeSpan? scheduledTime,
        string? status,
        string? resultOfTesting,
        int? reportId,
        int? semesterId)
    {
        GroupId = groupId;
        DisciplineId = disciplineId;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        Status = status;
        ResultOfTesting = resultOfTesting;
        ReportId = reportId;
        SemesterId = semesterId;
    }

    public TestingModel(
        int id,
        int? groupId,
        int? disciplineId,
        DateTime? scheduledDate,
        TimeSpan? scheduledTime,
        string? status,
        string? resultOfTesting,
        int? reportId,
        int? semesterId)
    {
        Id = id;
        GroupId = groupId;
        DisciplineId = disciplineId;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        Status = status;
        ResultOfTesting = resultOfTesting;
        ReportId = reportId;
        SemesterId = semesterId;
    }

    public int Id { get; set; }
    public int? GroupId { get; set; }
    public int? DisciplineId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public string? Status { get; set; }
    public string? ResultOfTesting { get; set; }
    public int? ReportId { get; set; }
    public int? SemesterId { get; set; }
}