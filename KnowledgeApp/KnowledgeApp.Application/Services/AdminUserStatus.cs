using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public static class AdminUserStatus
{
    public const string Active = "active";
    public const string Blocked = "blocked";
    public const string Inactive = "inactive";
    public const string Pending = "pending";

    private static readonly IReadOnlyList<(string Code, string StatusName)> Known =
    [
        (Active, "Активен"),
        (Blocked, "Заблокирован"),
        (Inactive, "Неактивен"),
        (Pending, "Ожидает подтверждения")
    ];

    public static string CodeOf(string? statusName) =>
        statusName is null
            ? Active
            : Known.FirstOrDefault(k => string.Equals(k.StatusName, statusName.Trim(), StringComparison.OrdinalIgnoreCase))
                  .Code ?? Inactive;

    public static string LabelOf(string code) => Known.Single(k => k.Code == code).StatusName;

    public static List<FilterOptionDto> Options() =>
        Known.Select(k => new FilterOptionDto { Value = k.Code, Label = k.StatusName }).ToList();

    public static IReadOnlyCollection<string> Normalize(IEnumerable<string>? values)
    {
        var allowed = Known.Select(k => k.Code).ToHashSet();
        var result = (values ?? [])
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(v => v.ToLowerInvariant())
            .Distinct()
            .ToList();
        var unknown = result.FirstOrDefault(v => !allowed.Contains(v));
        if (unknown is not null)
            throw new DocumentValidationException(
                $"Неизвестный статус пользователя: {unknown}. Допустимо: {string.Join(", ", allowed)}");
        return result;
    }

    public static async Task<Dictionary<int, string>> LoadCodes(
        KnowledgeTestDbContext context,
        CancellationToken cancellationToken) =>
        (await context.Statuses.AsNoTracking().ToListAsync(cancellationToken))
            .ToDictionary(s => s.Id, s => CodeOf(s.StatusName));

    public static async Task<int> EnsureStatusId(
        KnowledgeTestDbContext context,
        string code,
        CancellationToken cancellationToken)
    {
        var name = LabelOf(code);
        var existing = await context.Statuses
            .Where(s => s.StatusName == name)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing.HasValue)
            return existing.Value;

        var status = new Status { StatusName = name };
        context.Statuses.Add(status);
        await context.SaveChangesAsync(cancellationToken);
        return status.Id;
    }
}
