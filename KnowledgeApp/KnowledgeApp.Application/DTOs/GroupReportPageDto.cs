namespace KnowledgeApp.Application.DTOs;

public sealed class GroupReportPageDto
{
    public string AcademicYear { get; init; } = string.Empty;
    public string StudyProgramCode { get; init; } = string.Empty;
    public string StudyProgramName { get; init; } = string.Empty;
    public string DisciplineName { get; init; } = string.Empty;
    public string TeacherNames { get; init; } = string.Empty;
    public string GroupName { get; init; } = string.Empty;
    public IReadOnlyList<string> StudentNames { get; init; } = Array.Empty<string>();
    public string DepartmentName { get; init; } = string.Empty;
    public string FacultyName { get; init; } = string.Empty;
}
