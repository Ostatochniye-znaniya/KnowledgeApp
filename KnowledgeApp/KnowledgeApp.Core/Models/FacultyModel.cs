namespace KnowledgeApp.Core.Models;

public class FacultyModel
{
    public FacultyModel(string facultyName)
    {
        FacultyName = facultyName;
    }
    public FacultyModel(int id, string facultyName)
    {
        Id = id;
        FacultyName = facultyName;
    }

    public int Id { get; set; }

    public string FacultyName { get; set; } = null!;
}

