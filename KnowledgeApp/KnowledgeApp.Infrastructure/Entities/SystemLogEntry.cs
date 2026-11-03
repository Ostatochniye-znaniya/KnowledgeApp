namespace KnowledgeApp.Infrastructure.Entities;

public sealed class SystemLogEntry
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Level { get; set; } = null!;

    public string Service { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string? TraceId { get; set; }

    public string? Details { get; set; }

    public string? RequestMethod { get; set; }

    public string? RequestPath { get; set; }
}
