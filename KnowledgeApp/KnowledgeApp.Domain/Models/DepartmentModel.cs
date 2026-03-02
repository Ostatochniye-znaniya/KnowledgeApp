namespace KnowledgeApp.Domain.Entities;

public class DepartmentModel
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int? FacultyId { get; set; }

    public DepartmentModel(string name, int? facultyId)
    {
        Name = name;
        FacultyId = facultyId;
    }
    public DepartmentModel(int id, string name, int? facultyId)
    {
        Id = id;
        Name = name;
        FacultyId = facultyId;
    }
}

