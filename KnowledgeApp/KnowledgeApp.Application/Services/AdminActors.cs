using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public static class AdminActors
{
    public const int MaxReasonLength = 1000;

    public static async Task<string> NameOf(
        KnowledgeTestDbContext context,
        AdminActor actor,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(actor.Name))
            return actor.Name.Trim();

        if (actor.UserId.HasValue)
        {
            var name = await context.Users
                .AsNoTracking()
                .Where(u => u.Id == actor.UserId)
                .Select(u => u.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();
        }

        return "Неизвестный пользователь";
    }

    public static string RequireReason(string? reason, string emptyMessage)
    {
        var normalized = reason?.Trim();
        if (string.IsNullOrEmpty(normalized))
            throw new DocumentValidationException(emptyMessage);
        if (normalized.Length > MaxReasonLength)
            throw new DocumentValidationException($"Причина длиннее {MaxReasonLength} символов.");
        return normalized;
    }
}
