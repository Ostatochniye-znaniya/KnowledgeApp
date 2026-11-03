namespace KnowledgeApp.Infrastructure.Entities;

public sealed class UserRoleChange
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? PreviousRoles { get; set; }

    public string NewRoles { get; set; } = null!;

    public string? Reason { get; set; }

    public int? ChangedByUserId { get; set; }

    public string ChangedByName { get; set; } = null!;

    public DateTime ChangedAt { get; set; }
}
