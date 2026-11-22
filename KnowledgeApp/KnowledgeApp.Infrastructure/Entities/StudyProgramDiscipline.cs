namespace KnowledgeApp.Infrastructure.Entities;

// Перечень дисциплин профиля, из которого HOD выбирает дисциплины проверки.
public class StudyProgramDiscipline
{
    public int StudyProgramId { get; set; }
    public int DisciplineId { get; set; }
    public virtual StudyProgram StudyProgram { get; set; } = null!;
    public virtual Discipline Discipline { get; set; } = null!;
}
