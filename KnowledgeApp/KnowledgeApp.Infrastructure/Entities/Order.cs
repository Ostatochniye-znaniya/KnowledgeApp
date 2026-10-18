namespace KnowledgeApp.Infrastructure.Entities;

public class Order
{
    public int Id { get; set; }

    public string Number { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateOnly OrderDate { get; set; }

    public int SemesterId { get; set; }

    public int GroupId { get; set; }

    public string EducationControlDepartmentHead { get; set; } = null!;

    public string EducationalMethodicalCenterHead { get; set; } = null!;

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

    public string FileKey { get; set; } = null!;

    public long FileSize { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? CreatedByUserId { get; set; }

    public virtual Semester? Semester { get; set; }

    public virtual StudyGroup? Group { get; set; }
}
