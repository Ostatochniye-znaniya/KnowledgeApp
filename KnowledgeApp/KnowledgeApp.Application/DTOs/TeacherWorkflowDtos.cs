namespace KnowledgeApp.Application.DTOs;

/// <summary>A testing scheduled for the current responsible teacher.</summary>
public sealed record TeacherTestingApprovalDto(
    int TestingId,
    int? ReportId,
    string GroupName,
    string DisciplineName,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    int? ScheduledPair,
    DateOnly? SelectionPeriodStart,
    DateOnly? SelectionPeriodEnd,
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
    string PaperStatus,
    string PaperStatusLabel,
    bool? DoneInPaperForm,
    bool? DoneInElectronicForm,
    bool? AllDone);

/// <summary>Deadlines for the latest testing order.</summary>
public sealed record TeacherStageDeadlinesDto(
    int OrderId,
    string OrderNumber,
    DateOnly OrderDate,
    DateOnly PreparationStartDate,
    DateOnly PreparationEndDate,
    DateOnly TestingStartDate,
    DateOnly TestingEndDate,
    DateOnly SurveyStartDate,
    DateOnly SurveyEndDate,
    DateOnly ResultsProcessingStartDate,
    DateOnly InspectionSummaryDeadline,
    DateOnly SurveySummaryDeadline,
    DateOnly PaperReportDeadline,
    DateOnly ElectronicReportDeadline);
