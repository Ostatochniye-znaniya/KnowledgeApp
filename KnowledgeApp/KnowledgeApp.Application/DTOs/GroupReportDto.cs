namespace KnowledgeApp.Application.DTOs;

public sealed class GroupReportDto
{
    public int DisciplineId { get; init; }
    public string AcademicYear { get; init; } = string.Empty;
    public string StudyProgramCode { get; init; } = string.Empty;
    public string StudyProgramName { get; init; } = string.Empty;
    public string DisciplineName { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public IReadOnlyList<string> StudentNames { get; init; } = Array.Empty<string>();
    public string DepartmentName { get; init; } = string.Empty;
    public string FacultyName { get; init; } = string.Empty;
}
