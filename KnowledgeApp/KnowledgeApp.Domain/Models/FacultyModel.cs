namespace KnowledgeApp.Domain.Entities;

public class FacultyModel
{
    public int Id { get; set; }
    public string FacultyName { get; set; } = null!;

    public FacultyModel(string facultyName)
    {
        FacultyName = facultyName;
    }
    public FacultyModel(int id, string facultyName)
    {
        Id = id;
        FacultyName = facultyName;
    }
}

