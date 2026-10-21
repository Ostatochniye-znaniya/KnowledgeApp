using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public class ScheduleDocumentModel
{
    public int Id { get; set; }
    public int FacultyId { get; set; }
    public int SemesterId { get; set; }
    public int SemesterYear { get; set; }
    public int SemesterPart { get; set; }
    public string FileKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public DocumentStatus Status { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
