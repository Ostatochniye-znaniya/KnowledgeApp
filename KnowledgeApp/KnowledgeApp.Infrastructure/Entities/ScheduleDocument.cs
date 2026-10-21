using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Infrastructure.Entities;

public class ScheduleDocument
{
    public int Id { get; set; }

    public int FacultyId { get; set; }

    public int SemesterId { get; set; }

    public string FileKey { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; }

    public int? UploadedByUserId { get; set; }

    public DocumentStatus Status { get; set; }

    public string? ReviewComment { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public int? ReviewedByUserId { get; set; }

    public virtual Faculty? Faculty { get; set; }

    public virtual Semester? Semester { get; set; }
}
