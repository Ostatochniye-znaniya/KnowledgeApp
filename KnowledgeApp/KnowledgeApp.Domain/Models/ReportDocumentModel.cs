using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Domain.Entities;

public class ReportDocumentModel
{
    public int ReportId { get; set; }
    public int? FacultyId { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? DisciplineName { get; set; }
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
    public string? GroupNumber { get; set; }
    public string? StudyProgramCode { get; set; }
    public int? SemesterYear { get; set; }
    public int? SemesterPart { get; set; }
    public int? YearPart { get; set; }
    public string? FileKey { get; set; }
    public string? FileName { get; set; }
    public DateTime? UploadedAt { get; set; }
    public DocumentStatus? Status { get; set; }
    public string? ReviewComment { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
