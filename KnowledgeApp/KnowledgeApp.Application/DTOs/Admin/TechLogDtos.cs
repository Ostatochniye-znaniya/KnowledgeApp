using KnowledgeApp.Application.DTOs.Documents;

namespace KnowledgeApp.Application.DTOs.Admin;

public sealed record TechLogQuery(
    IReadOnlyCollection<string> Levels,
    string? Service,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string? TraceId,
    string? Search,
    int Page,
    int PageSize);

public sealed record TechLogDto(
    long Id,
    DateTimeOffset Timestamp,
    string Level,
    string Service,
    string Category,
    string Message,
    string? TraceId,
    string? RequestMethod,
    string? RequestPath,
    bool HasDetails);

public sealed record TechLogDetailsDto(
    long Id,
    DateTimeOffset Timestamp,
    string Level,
    string Service,
    string Category,
    string Message,
    string? TraceId,
    string? RequestMethod,
    string? RequestPath,
    string? Details);

public sealed record TechLogPageDto(
    IReadOnlyList<TechLogDto> Items,
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<AdminStatusCountDto> LevelCounts);

public sealed record TechLogFiltersDto(
    IReadOnlyList<FilterOptionDto> Levels,
    IReadOnlyList<FilterOptionDto> Services);
