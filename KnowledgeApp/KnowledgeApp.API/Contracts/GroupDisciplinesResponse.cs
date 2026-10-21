namespace KnowledgeApp.API.Contracts;

public class GroupDisciplinesResponse
{
    public int GroupId { get; set; }
    public string GroupNumber { get; set; } = null!;
    public int StudyProgramId { get; set; }
    public string? StudyProgramName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? FacultyName { get; set; }
    public int SemesterId { get; set; }

    /// <summary>Сколько дисциплин можно выбрать: не больше двух и не больше числа тестирований группы.</summary>
    public int MaxDisciplines { get; set; }

    public List<DisciplineResponse> Disciplines { get; set; } = new();
}
