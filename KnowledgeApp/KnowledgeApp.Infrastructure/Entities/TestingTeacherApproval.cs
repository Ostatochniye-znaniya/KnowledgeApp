namespace KnowledgeApp.Infrastructure.Entities;

public sealed class TestingTeacherApproval
{
    public int TestingId { get; set; }

    public int TeacherId { get; set; }

    public string? Status { get; set; }

    public string? Comment { get; set; }

    public DateTime? RespondedAt { get; set; }
}
