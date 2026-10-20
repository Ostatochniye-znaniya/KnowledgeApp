namespace KnowledgeApp.Application.DTOs;

public sealed record TestingTeacherAssignmentDetailsDto(
    int TestingId,
    string GroupName,
    string DisciplineName,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    int? ScheduledPair,
    IReadOnlyList<TestingTeacherAssignmentDto> Teachers);

public sealed record TestingTeacherAssignmentDto(
    int TeacherId,
    string? TeacherName,
    string Status,
    string? Comment,
    DateTimeOffset? RespondedAt);

public sealed record AvailableTestingTeacherDto(int TeacherId, string? TeacherName);

public sealed record TestingPairSlotDto(int PairNumber, TimeOnly StartsAt, TimeOnly EndsAt);

public sealed record WorkflowNotificationDto(
    int Id,
    int? TestingId,
    string Type,
    string Title,
    string Message,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

/// <summary>
/// Строка страницы «Выбор преподавателей». Kind: own — дисциплина своего факультета, назначаем сами;
/// outgoing — дисциплина другого факультета, нужна заявка; incoming — заявка от кафедры другого факультета.
/// </summary>
public sealed record DepartmentTestingDto(
    int TestingId,
    int GroupId,
    string GroupName,
    string? GroupDepartmentName,
    string? GroupFacultyName,
    int DisciplineId,
    string DisciplineName,
    string? DisciplineDepartmentName,
    string Kind,
    bool CanManage,
    DateTimeOffset? ExternalRequestSentAt,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    DateOnly? ScheduledDate,
    TimeOnly? ScheduledTime,
    int? ScheduledPair,
    IReadOnlyList<TestingTeacherAssignmentDto> Teachers);
