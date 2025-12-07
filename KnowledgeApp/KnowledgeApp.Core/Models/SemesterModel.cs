namespace KnowledgeApp.Core.Models;

public class SemesterModel
{
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

    public int Id { get; set; }

    public int SemesterYear { get; set; }

    public int SemesterPart { get; set; }
}