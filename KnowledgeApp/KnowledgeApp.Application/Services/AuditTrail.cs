using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public static class AuditTrail
{
    public static async Task Add(
        KnowledgeTestDbContext context,
        AdminActor actor,
        string action,
        string entityType,
        object? entityId,
        string? entityLabel,
        string? previousState,
        string? newState,
        string? reason,
        CancellationToken cancellationToken)
    {
        var actorName = await AdminActors.NameOf(context, actor, cancellationToken);
        var actorRole = actor.Role;
        if (string.IsNullOrWhiteSpace(actorRole) && actor.UserId.HasValue)
        {
            actorRole = await context.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == actor.UserId && ur.Role != null)
                .OrderBy(ur => ur.RoleId)
                .Select(ur => ur.Role!.RoleName)
                .FirstOrDefaultAsync(cancellationToken);
        }

        context.AuditLogs.Add(new AuditLogEntry
        {
            CreatedAt = DateTime.UtcNow,
            ActorUserId = actor.UserId,
            ActorName = Cut(actorName, 255)!,
            ActorRole = Cut(actorRole, 255),
            Action = action,
            EntityType = entityType,
            EntityId = Cut(entityId?.ToString(), 64),
            EntityLabel = Cut(entityLabel, 255),
            PreviousState = Cut(previousState, 2000),
            NewState = Cut(newState, 2000),
            Reason = Cut(reason, 1000),
            IpAddress = Cut(actor.IpAddress, 64)
        });
    }

    private static string? Cut(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
