namespace KnowledgeApp.Application.DTOs.Orders;

public sealed class OrderDetailsDto : OrderEssentialsDto
{
    public string EducationControlDepartmentHead { get; set; } = string.Empty;
    public string EducationalMethodicalCenterHead { get; set; } = string.Empty;
    public DateOnly InspectionSummaryDeadline { get; set; }
    public DateOnly SurveySummaryDeadline { get; set; }
    public bool RequiresPaperReports { get; set; }
    public DateOnly? PaperReportDeadline { get; set; }
    public DateOnly ElectronicReportDeadline { get; set; }
    public DateOnly? SurveyStartDate { get; set; }
    public DateOnly? SurveyEndDate { get; set; }
    public DateOnly? PreparationStartDate { get; set; }
    public DateOnly? PreparationEndDate { get; set; }
    public DateOnly? ResultsProcessingStartDate { get; set; }
    public DateOnly? TestingStageStartDate { get; set; }
    public DateOnly? TestingStageEndDate { get; set; }
    public DateOnly? OnlineSeminarDate { get; set; }
    public long FileSize { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
}
