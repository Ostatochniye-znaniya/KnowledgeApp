namespace KnowledgeApp.Application.DTOs.Orders;

public class CreateOrderDto
{
    public DateOnly? OrderDate { get; set; }
    public string? Number { get; set; }
    public int? SemesterId { get; set; }
    public string? EducationControlDepartmentHead { get; set; }
    public string? EducationalMethodicalCenterHead { get; set; }
    public DateOnly? InspectionSummaryDeadline { get; set; }
    public DateOnly? SurveySummaryDeadline { get; set; }
    public DateOnly? PaperReportDeadline { get; set; }
    public DateOnly? ElectronicReportDeadline { get; set; }
    public DateOnly? TestingStartDate { get; set; }
    public DateOnly? TestingEndDate { get; set; }
    public DateOnly? SurveyStartDate { get; set; }
    public DateOnly? SurveyEndDate { get; set; }
    public DateOnly? PreparationStartDate { get; set; }
    public DateOnly? PreparationEndDate { get; set; }
    public DateOnly? ResultsProcessingStartDate { get; set; }
}
