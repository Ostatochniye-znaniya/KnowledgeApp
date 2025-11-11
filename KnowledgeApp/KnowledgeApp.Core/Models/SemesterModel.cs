namespace KnowledgeApp.Core.Models;

public class SemesterModel
{
    public SemesterModel(int semesterYear, int semesterPart)
    {
        SemesterPart = semesterPart;
        SemesterYear = semesterYear;
    }
    public SemesterModel(int id, int semesterYear, int semesterPart)
    {
        Id = id;
        SemesterPart = semesterPart;
        SemesterYear = semesterYear;
    }

    public int Id { get; set; }

    public int SemesterYear { get; set; }
    public int SemesterPart { get; set; }
}