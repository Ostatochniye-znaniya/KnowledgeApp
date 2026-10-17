namespace KnowledgeApp.Application.DTOs.Documents;

public class ReportDocumentDto
{
    public int Id { get; set; }

    public string? AcademicYear { get; set; }

    public string? StudyProgramCode { get; set; }

    public string? GroupNumber { get; set; }
    public string? DisciplineName { get; set; }
    public string? TeacherName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }

    public string Status { get; set; } = string.Empty;

    public string StatusLabel { get; set; } = string.Empty;

    public string? FileName { get; set; }
    public DateTime? UploadedAt { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
