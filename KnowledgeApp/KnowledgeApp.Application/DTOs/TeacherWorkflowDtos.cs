namespace KnowledgeApp.Application.DTOs;

/// <summary>A testing scheduled for the current responsible teacher.</summary>
public sealed record TeacherTestingApprovalDto(
    int TestingId,
    int? ReportId,
    string GroupName,
    string DisciplineName,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    bool? IsInPerson,
    string? Room,
    string? LmsUrl,
    string ApprovalStatus,
    string? Comment,
    DateTimeOffset? RespondedAt);

/// <summary>A report task assigned to the current responsible teacher.</summary>
public sealed record TeacherReportTaskDto(
    int? ReportId,
    int GroupId,
    int SemesterId,
    int DisciplineId,
    string GroupName,
    string DisciplineName,
    string? FileName,
    string Status,
    string StatusLabel,
    string? ReviewComment,
    DateTimeOffset? UploadedAt,
    bool? DoneInPaperForm,
    bool? DoneInElectronicForm,
    bool? AllDone);
