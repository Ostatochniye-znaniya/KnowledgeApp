namespace KnowledgeApp.Core.Models;

public class DepartmentModel
{
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

    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? FacultyId { get; set; }
}

