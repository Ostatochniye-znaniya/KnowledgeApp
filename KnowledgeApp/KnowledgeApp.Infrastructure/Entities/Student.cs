namespace KnowledgeApp.Infrastructure.Entities;

public partial class Student
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Year { get; set; }

    public int GroupId { get; set; }

    public string Status { get; set; } = null!;

    public virtual StudyGroup Group { get; set; } = null!;
}
