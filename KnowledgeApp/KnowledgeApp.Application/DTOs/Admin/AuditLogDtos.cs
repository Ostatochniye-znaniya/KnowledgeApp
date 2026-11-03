using KnowledgeApp.Application.DTOs.Documents;

namespace KnowledgeApp.Application.DTOs.Admin;

public sealed record AuditLogQuery(
    DateOnly? DateFrom,
    DateOnly? DateTo,
    int? UserId,
    string? Action,
    string? EntityType,
    string? EntityId,
    string? Search,
    int Page,
    int PageSize);

public sealed record AuditLogDto(
    long Id,
    DateTimeOffset Timestamp,
    int? UserId,
    string User,
    string? Role,
    string Action,
    string ActionLabel,
    string EntityType,
    string EntityTypeLabel,
    string? EntityId,
    string? Entity,
    string? PreviousState,
    string? NewState,
    string? Reason,
    string? IpAddress);

public sealed record AuditLogPageDto(
    IReadOnlyList<AuditLogDto> Items,
    int Total,
    int Page,
    int PageSize);

public sealed record AuditLogFiltersDto(
    IReadOnlyList<FilterOptionDto> Users,
    IReadOnlyList<FilterOptionDto> Actions,
    IReadOnlyList<FilterOptionDto> EntityTypes);
