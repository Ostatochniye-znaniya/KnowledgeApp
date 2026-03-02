namespace KnowledgeApp.Domain.Entities;

public class SemesterModel
{
    public int Id { get; set; }
    public int SemesterYear { get; set; }
    public int SemesterPart { get; set; }

    public SemesterModel(int semesterYear, int semesterPart)
    {
        SemesterYear = semesterYear;
        SemesterPart = semesterPart;
    }

    public SemesterModel(int id, int semesterYear, int semesterPart)
    {
        Id = id;
        SemesterYear = semesterYear;
        SemesterPart = semesterPart;
    }
}