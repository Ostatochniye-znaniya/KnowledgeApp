using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed class AuditLogService
{
    private const int MaxPageSize = 200;
    private static readonly TimeSpan MoscowOffset = TimeSpan.FromHours(3);
    private readonly KnowledgeTestDbContext _context;

    public AuditLogService(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLogPageDto> GetLogs(AuditLogQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            throw new DocumentValidationException("Номер страницы должен быть больше нуля.");
        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            throw new DocumentValidationException($"Размер страницы должен быть от 1 до {MaxPageSize}.");
        if (query.DateFrom.HasValue && query.DateTo.HasValue && query.DateFrom > query.DateTo)
            throw new DocumentValidationException("Дата «с» позже даты «по».");

        var logs = _context.AuditLogs.AsNoTracking();
        if (query.DateFrom.HasValue)
        {
            var from = MoscowDayStartUtc(query.DateFrom.Value);
            logs = logs.Where(l => l.CreatedAt >= from);
        }
        if (query.DateTo.HasValue)
        {
            var to = MoscowDayStartUtc(query.DateTo.Value.AddDays(1));
            logs = logs.Where(l => l.CreatedAt < to);
        }
        if (query.UserId.HasValue)
            logs = logs.Where(l => l.ActorUserId == query.UserId);
        if (!string.IsNullOrWhiteSpace(query.Action))
            logs = logs.Where(l => l.Action == query.Action.Trim());
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            logs = logs.Where(l => l.EntityType == query.EntityType.Trim());
        if (!string.IsNullOrWhiteSpace(query.EntityId))
            logs = logs.Where(l => l.EntityId == query.EntityId.Trim());

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            logs = logs.Where(l =>
                l.ActorName.Contains(search) ||
                (l.EntityLabel != null && l.EntityLabel.Contains(search)) ||
                (l.PreviousState != null && l.PreviousState.Contains(search)) ||
                (l.NewState != null && l.NewState.Contains(search)) ||
                (l.Reason != null && l.Reason.Contains(search)));
        }

        var total = await logs.CountAsync(cancellationToken);
        var rows = await logs
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(l => new AuditLogDto(
                l.Id,
                DocumentStatusInfo.AsUtc(l.CreatedAt),
                l.ActorUserId,
                l.ActorName,
                l.ActorRole,
                l.Action,
                AuditCatalog.ActionLabel(l.Action),
                l.EntityType,
                AuditCatalog.EntityLabel(l.EntityType),
                l.EntityId,
                l.EntityLabel,
                l.PreviousState,
                l.NewState,
                l.Reason,
                l.IpAddress))
            .ToList();

        return new AuditLogPageDto(items, total, query.Page, query.PageSize);
    }

    public async Task<AuditLogFiltersDto> GetFilters(CancellationToken cancellationToken)
    {
        var actors = await _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.ActorUserId != null)
            .GroupBy(l => l.ActorUserId)
            .Select(g => new { UserId = g.Key!.Value, Name = g.OrderByDescending(l => l.CreatedAt).First().ActorName })
            .ToListAsync(cancellationToken);

        return new AuditLogFiltersDto(
            actors
                .OrderBy(a => a.Name)
                .Select(a => new FilterOptionDto { Value = a.UserId.ToString(), Label = a.Name })
                .ToList(),
            AuditCatalog.ActionOptions(),
            AuditCatalog.EntityOptions());
    }

    private static DateTime MoscowDayStartUtc(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue) - MoscowOffset;
}
