using KnowledgeApp.Application.DTOs.Documents;

namespace KnowledgeApp.Application.DTOs.Admin;

public sealed record AdminProcessQuery(
    string? Search,
    IReadOnlyCollection<string> ApprovalStatuses,
    IReadOnlyCollection<string> Stages,
    IReadOnlyCollection<string> ReportStatuses,
    int? SemesterId,
    int? FacultyId,
    int? DepartmentId,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int Page,
    int PageSize);

public sealed record AdminProcessTeacherDto(
    int TeacherId,
    string? Name,
    string ApprovalStatus,
    string ApprovalStatusLabel,
    string? Comment,
    DateTimeOffset? RespondedAt);

public sealed record AdminProcessDto(
    int Id,
    int GroupId,
    string? GroupName,
    int? DisciplineId,
    string? DisciplineName,
    int? DepartmentId,
    string? DepartmentName,
    string? FacultyName,
    int? SemesterId,
    string? SemesterLabel,
    IReadOnlyList<AdminProcessTeacherDto> Teachers,
    string TeacherNames,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    int? ScheduledPair,
    bool? IsInPerson,
    string? Room,
    string? LmsUrl,
    string ApprovalStatus,
    string ApprovalStatusLabel,
    string Stage,
    string StageLabel,
    int? ReportId,
    string ReportStatus,
    string ReportStatusLabel,
    bool IsLocked);

public sealed record AdminStatusCountDto(string Value, string Label, int Count);

public sealed record AdminProcessPageDto(
    IReadOnlyList<AdminProcessDto> Items,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<AdminStatusCountDto> ApprovalStatusCounts);

public sealed record AdminProcessFiltersDto(
    IReadOnlyList<FilterOptionDto> ApprovalStatuses,
    IReadOnlyList<FilterOptionDto> Stages,
    IReadOnlyList<FilterOptionDto> ReportStatuses,
    IReadOnlyList<FilterOptionDto> Semesters,
    IReadOnlyList<FilterOptionDto> Faculties,
    IReadOnlyList<FilterOptionDto> Departments);
