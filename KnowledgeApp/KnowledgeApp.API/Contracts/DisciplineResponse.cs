namespace KnowledgeApp.API.Contracts;

public class DisciplineResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? FacultyId { get; set; }
    public string? FacultyName { get; set; }
    public bool IsInStudyProgram { get; set; } = true;
    public bool Locked { get; set; }
}
