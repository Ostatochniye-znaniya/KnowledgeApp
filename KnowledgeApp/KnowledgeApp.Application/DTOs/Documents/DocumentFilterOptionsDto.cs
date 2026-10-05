namespace KnowledgeApp.Application.DTOs.Documents;

public class FilterOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class ReportFilterOptionsDto
{
    public List<FilterOptionDto> Departments { get; set; } = new();
    public List<FilterOptionDto> AcademicYears { get; set; } = new();
    public List<FilterOptionDto> Statuses { get; set; } = new();
}

public class ScheduleFilterOptionsDto
{
    public List<FilterOptionDto> Semesters { get; set; } = new();
    public List<FilterOptionDto> Statuses { get; set; } = new();
}
