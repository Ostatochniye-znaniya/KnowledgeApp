namespace KnowledgeApp.Infrastructure.Entities;

public sealed class TestingChange
{
    public int Id { get; set; }

    public int TestingId { get; set; }

    public string Action { get; set; } = null!;

    public string? PreviousValue { get; set; }

    public string? NewValue { get; set; }

    public string Reason { get; set; } = null!;

    public int? ChangedByUserId { get; set; }

    public string ChangedByName { get; set; } = null!;

    public DateTime ChangedAt { get; set; }
}
