using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed class TechLogService
{
    private const int MaxPageSize = 200;
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);

    private static readonly IReadOnlyList<(string Code, string Label)> Levels =
    [
        ("error", "ERROR"),
        ("warning", "WARN"),
        ("info", "INFO")
    ];

    private readonly KnowledgeTestDbContext _context;

    public TechLogService(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<TechLogPageDto> GetLogs(TechLogQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            throw new DocumentValidationException("Номер страницы должен быть больше нуля.");
        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            throw new DocumentValidationException($"Размер страницы должен быть от 1 до {MaxPageSize}.");
        if (query.DateFrom.HasValue && query.DateTo.HasValue && query.DateFrom > query.DateTo)
            throw new DocumentValidationException("Дата «с» позже даты «по».");

        var levels = query.Levels
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(v => v.ToLowerInvariant() switch { "warn" => "warning", var level => level })
            .Distinct()
            .ToList();
        var unknown = levels.FirstOrDefault(l => Levels.All(known => known.Code != l));
        if (unknown is not null)
            throw new DocumentValidationException($"Неизвестный уровень: {unknown}. Допустимо: error, warning, info");

        var logs = _context.SystemLogs.AsNoTracking();
        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue) - MoscowOffset;
            logs = logs.Where(l => l.CreatedAt >= from);
        }
        if (query.DateTo.HasValue)
        {
            var to = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue) - MoscowOffset;
            logs = logs.Where(l => l.CreatedAt < to);
        }
        if (!string.IsNullOrWhiteSpace(query.Service))
            logs = logs.Where(l => l.Service == query.Service.Trim());
        if (!string.IsNullOrWhiteSpace(query.TraceId))
            logs = logs.Where(l => l.TraceId == query.TraceId.Trim());
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            logs = logs.Where(l => l.Message.Contains(search) || l.Category.Contains(search));

        var levelGroups = await logs
            .GroupBy(l => l.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);
        var counts = Levels
            .Select(l => new AdminStatusCountDto(
                l.Code,
                l.Label,
                levelGroups.Where(g => g.Level == l.Code).Sum(g => g.Count)))
            .ToList();

        if (levels.Count > 0)
            logs = logs.Where(l => levels.Contains(l.Level));

        var total = await logs.CountAsync(cancellationToken);
        var items = await logs
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(l => new
            {
                l.Id,
                l.CreatedAt,
                l.Level,
                l.Service,
                l.Category,
                l.Message,
                l.TraceId,
                l.RequestMethod,
                l.RequestPath,
                HasDetails = l.Details != null
            })
            .ToListAsync(cancellationToken);

        return new TechLogPageDto(
            items
                .Select(l => new TechLogDto(
                    l.Id,
                    DocumentStatusInfo.AsUtc(l.CreatedAt),
                    l.Level,
                    l.Service,
                    l.Category,
                    l.Message,
                    l.TraceId,
                    l.RequestMethod,
                    l.RequestPath,
                    l.HasDetails))
                .ToList(),
            total,
            query.Page,
            query.PageSize,
            counts);
    }

    public async Task<TechLogDetailsDto> GetLog(long id, CancellationToken cancellationToken)
    {
        var log = await _context.SystemLogs.AsNoTracking().SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
                  ?? throw new DocumentNotFoundException("Запись лога не найдена");
        return new TechLogDetailsDto(
            log.Id,
            DocumentStatusInfo.AsUtc(log.CreatedAt),
            log.Level,
            log.Service,
            log.Category,
            log.Message,
            log.TraceId,
            log.RequestMethod,
            log.RequestPath,
            log.Details);
    }

    public async Task<TechLogFiltersDto> GetFilters(CancellationToken cancellationToken)
    {
        var services = await _context.SystemLogs
            .AsNoTracking()
            .Select(l => l.Service)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);

        return new TechLogFiltersDto(
            Levels.Select(l => new FilterOptionDto { Value = l.Code, Label = l.Label }).ToList(),
            services.Select(s => new FilterOptionDto { Value = s, Label = s }).ToList());
    }
}
