namespace KnowledgeApp.Domain.Entities;

public class ReportModel
{
    public int Id { get; set; }
    public int? DisciplineId { get; set; }
    public int? TeacherId { get; set; }
    public int? SemesterId { get; set; }
    public string? FilePath { get; set; }
    public bool? IsCorrect { get; set; }
    public bool? DoneInPaperForm { get; set; }
    public bool? DoneInElectronicForm { get; set; }
    public bool? AllDone { get; set; }

    public ReportModel(int id, int? disciplineId, int? teacherId, int? semesterId, string? filePath, bool? isCorrect, bool? doneInPaperForm, bool? doneInElectronicForm, bool? allDone)
    {
        Id = id;
        DisciplineId = disciplineId;
        TeacherId = teacherId;
        SemesterId = semesterId;
        FilePath = filePath;
        IsCorrect = isCorrect;
        DoneInPaperForm = doneInPaperForm;
        DoneInElectronicForm = doneInElectronicForm;
        AllDone = allDone;
    }

    public ReportModel(int? disciplineId, int? teacherId, int? semesterId, string? filePath, bool? isCorrect, bool? doneInPaperForm, bool? doneInElectronicForm, bool? allDone)
    {
        DisciplineId = disciplineId;
        TeacherId = teacherId;
        SemesterId = semesterId;
        FilePath = filePath;
        IsCorrect = isCorrect;
        DoneInPaperForm = doneInPaperForm;
        DoneInElectronicForm = doneInElectronicForm;
        AllDone = allDone;
    }
}
