namespace KnowledgeApp.Domain.Entities;

public class TestingModel
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public int DisciplineId { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public TimeSpan? ScheduledTime { get; set; }
    public string? Room { get; set; }
    public string? LmsUrl { get; set; }
    public string? Status { get; set; }
    public string? ResultOfTesting { get; set; }
    public int ReportId { get; set; }
    public int? SemesterId { get; set; }

    public TestingModel(
        int groupId,
        int disciplineId,
        DateTime? scheduledDate,
        TimeSpan? scheduledTime,
        string? status,
        string? resultOfTesting,
        int reportId,
        int? semesterId,
        string? room = null,
        string? lmsUrl = null)
    {
        GroupId = groupId;
        DisciplineId = disciplineId;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        Room = room;
        LmsUrl = lmsUrl;
        Status = status;
        ResultOfTesting = resultOfTesting;
        ReportId = reportId;
        SemesterId = semesterId;
    }

    public TestingModel(
        int id,
        int groupId,
        int disciplineId,
        DateTime? scheduledDate,
        TimeSpan? scheduledTime,
        string? status,
        string? resultOfTesting,
        int reportId,
        int? semesterId,
        string? room = null,
        string? lmsUrl = null)
    {
        Id = id;
        GroupId = groupId;
        DisciplineId = disciplineId;
        ScheduledDate = scheduledDate;
        ScheduledTime = scheduledTime;
        Room = room;
        LmsUrl = lmsUrl;
        Status = status;
        ResultOfTesting = resultOfTesting;
        ReportId = reportId;
        SemesterId = semesterId;
    }
}