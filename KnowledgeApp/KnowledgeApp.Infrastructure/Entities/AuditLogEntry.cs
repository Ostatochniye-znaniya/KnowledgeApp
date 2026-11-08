namespace KnowledgeApp.Infrastructure.Entities;

public sealed class AuditLogEntry
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? ActorUserId { get; set; }

    public string ActorName { get; set; } = null!;

    public string? ActorRole { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public string? EntityId { get; set; }

    public string? EntityLabel { get; set; }

    public string? PreviousState { get; set; }

    public string? NewState { get; set; }

    public string? Reason { get; set; }

    public string? IpAddress { get; set; }
}
