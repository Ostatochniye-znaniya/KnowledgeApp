namespace KnowledgeApp.API.Contracts;

public class DisciplineResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
}