namespace KnowledgeApp.Domain.Entities;

public class DisciplineModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? DepartmentId { get; set; }

    public DisciplineModel(string name, int? departmentId)
    {
        Name = name;
        DepartmentId = departmentId;
    }

    public DisciplineModel(int id, string name, int? departmentId)
    {
        Id = id;
        Name = name;
        DepartmentId = departmentId;
    }
}
