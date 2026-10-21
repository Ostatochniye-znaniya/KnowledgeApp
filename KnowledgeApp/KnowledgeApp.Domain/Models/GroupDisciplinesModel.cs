namespace KnowledgeApp.Domain.Entities;

/// <summary>
/// Учебная группа и дисциплины, выбранные заведующим кафедрой для проверки
/// остаточных знаний в семестре (ТЗ, п. 2.4).
/// </summary>
public class GroupDisciplinesModel
{
    public int GroupId { get; set; }
    public string GroupNumber { get; set; } = null!;
    public int StudyProgramId { get; set; }
    public string? StudyProgramName { get; set; }
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? FacultyName { get; set; }
    public int SemesterId { get; set; }

    /// <summary>Сколько тестирований создано для группы в семестре (слоты под дисциплины).</summary>
    public int SlotCount { get; set; }

    public List<DisciplineModel> Disciplines { get; set; } = new();
}
