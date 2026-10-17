namespace KnowledgeApp.Domain.Entities;

public class SemesterModel
{
    public int Id { get; set; }
    public int SemesterYear { get; set; }
    public int SemesterPart { get; set; }
    public int YearPart { get; set; }

    public SemesterModel(int semesterYear, int semesterPart, int yearPart)
    {
        SemesterYear = semesterYear;
        SemesterPart = semesterPart;
        YearPart = yearPart;
    }

    public SemesterModel(int id, int semesterYear, int semesterPart, int yearPart)
    {
        Id = id;
        SemesterYear = semesterYear;
        SemesterPart = semesterPart;
        YearPart = yearPart;
    }
}