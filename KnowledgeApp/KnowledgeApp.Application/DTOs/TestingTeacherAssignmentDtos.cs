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
