namespace KnowledgeApp.Domain.Entities;

public class OrderModel
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly OrderDate { get; set; }
    public int SemesterId { get; set; }
    public int SemesterYear { get; set; }
    public int SemesterPart { get; set; }
    public string EducationControlDepartmentHead { get; set; } = string.Empty;
    public string EducationalMethodicalCenterHead { get; set; } = string.Empty;
    public DateOnly InspectionSummaryDeadline { get; set; }
    public DateOnly SurveySummaryDeadline { get; set; }
    public DateOnly PaperReportDeadline { get; set; }
    public DateOnly ElectronicReportDeadline { get; set; }
    public DateOnly TestingStartDate { get; set; }
    public DateOnly TestingEndDate { get; set; }
    public DateOnly SurveyStartDate { get; set; }
    public DateOnly SurveyEndDate { get; set; }
    public DateOnly PreparationStartDate { get; set; }
    public DateOnly PreparationEndDate { get; set; }
    public DateOnly ResultsProcessingStartDate { get; set; }
    public int GroupId { get; set; }
    public string FileKey { get; set; } = string.Empty;
    public long FileSize { get; set; }
}
