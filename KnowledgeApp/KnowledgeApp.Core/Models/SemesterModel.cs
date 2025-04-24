namespace KnowledgeApp.Core.Models;

public class SemesterModel
{
    public SemesterModel(string semesterName)
    {
        SemesterName = semesterName;
    }
    public SemesterModel(int id, string semesterName)
    {
        Id = id;
        SemesterName = semesterName;
    }

    public int Id { get; set; }

    public string SemesterName { get; set; } = null!;
}