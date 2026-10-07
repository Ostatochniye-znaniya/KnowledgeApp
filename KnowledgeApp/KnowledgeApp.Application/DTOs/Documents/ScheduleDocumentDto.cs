namespace KnowledgeApp.Application.DTOs.Documents;

public class ScheduleDocumentDto
{
    public int Id { get; set; }
    public int FacultyId { get; set; }
    public int SemesterId { get; set; }
    public int SemesterYear { get; set; }

    public int SemesterPart { get; set; }

    public string Period { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
