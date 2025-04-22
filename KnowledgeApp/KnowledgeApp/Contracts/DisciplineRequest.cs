namespace KnowledgeApp.API.Contracts;

public class DisciplineRequest
{
    public string Name { get; set; } = null!;
    public int? DepartmentId { get; set; }
}
