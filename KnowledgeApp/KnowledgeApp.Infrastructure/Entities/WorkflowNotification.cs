namespace KnowledgeApp.Infrastructure.Entities;

public sealed class WorkflowNotification
{
    public int Id { get; set; }

    public int? RecipientUserId { get; set; }

    public int? RecipientDepartmentId { get; set; }

    public int? TestingId { get; set; }

    public string Type { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? ReadAt { get; set; }
}
