namespace KnowledgeApp.Infrastructure.Entities;

public partial class GroupDiscipline
{
    public int Id { get; set; }

    public int? GroupId { get; set; }

    public int? DisciplineId { get; set; }

    public virtual StudyGroup? Group { get; set; }

    public virtual Discipline? Discipline { get; set; }
}