namespace KnowledgeApp.Domain.Entities;

public class ReportModel
{
    public int Id { get; set; }
    public int? DisciplineId { get; set; }
    public int? TeacherId { get; set; }
    public string? FilePath { get; set; }
    public bool? IsCorrect { get; set; }
    public string? ResultOfAttestation { get; set; }
    public bool? DoneInPaperForm { get; set; }
    public bool? DoneInElectronicForm { get; set; }
    public bool? AllDone { get; set; }

    public ReportModel(int id, int? disciplineId, int? teacherId, string? filePath, bool? isCorrect, string? resultOfAttestation, bool? doneInPaperForm, bool? doneInElectronicForm, bool? allDone)
    {
        Id = id;
        DisciplineId = disciplineId;
        TeacherId = teacherId;
        FilePath = filePath;
        IsCorrect = isCorrect;
        ResultOfAttestation = resultOfAttestation;
        DoneInPaperForm = doneInPaperForm;
        DoneInElectronicForm = doneInElectronicForm;
        AllDone = allDone;
    }

    public ReportModel(int? disciplineId, int? teacherId, string? filePath, bool? isCorrect, string? resultOfAttestation, bool? doneInPaperForm, bool? doneInElectronicForm, bool? allDone)
    {
        DisciplineId = disciplineId;
        TeacherId = teacherId;
        FilePath = filePath;
        IsCorrect = isCorrect;
        ResultOfAttestation = resultOfAttestation;
        DoneInPaperForm = doneInPaperForm;
        DoneInElectronicForm = doneInElectronicForm;
        AllDone = allDone;
    }
}
