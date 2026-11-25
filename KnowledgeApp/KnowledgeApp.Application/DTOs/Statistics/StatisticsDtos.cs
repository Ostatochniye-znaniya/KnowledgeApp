namespace KnowledgeApp.Application.DTOs.Statistics;

public sealed class StatisticsFilterOptionsDto
{
    public List<StatisticsSemesterOptionDto> Semesters { get; set; } = [];
}

public sealed class StatisticsSemesterOptionDto
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsSpring { get; set; }
}

public sealed class StatisticsSummaryDto
{
    public int SemesterId { get; set; }
    public string SemesterLabel { get; set; } = string.Empty;
    public bool IsSpring { get; set; }
    public int? OrderId { get; set; }
    public bool RequiresPaperReports { get; set; }
    public List<FacultyStatisticsDto> Faculties { get; set; } = [];
}

public sealed class FacultyStatisticsDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<DepartmentStatisticsDto> Departments { get; set; } = [];
}

public sealed class DepartmentStatisticsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ProfilesCount { get; set; }
    public int CoursesCount { get; set; }
    public int MagistracyCount { get; set; }
    public int GroupNorm { get; set; }
    public int GroupFact { get; set; }
    public int ClassNorm { get; set; }
    public int ClassFact { get; set; }
    public int EReportDone { get; set; }
    public int EReportRemarks { get; set; }
    public int PReportDone { get; set; }
    public int PReportRemarks { get; set; }
}

public sealed class DepartmentStatisticsDetailsDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string? FacultyCode { get; set; }
    public int SemesterId { get; set; }
    public bool RequiresPaperReports { get; set; }
    public List<DisciplineStatisticsDto> Disciplines { get; set; } = [];
}

public sealed class DisciplineStatisticsDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Course { get; set; }
    public string Semester { get; set; } = string.Empty;
    public int ProfilesCount { get; set; }
    public int GroupFact { get; set; }
    public int ClassNorm { get; set; }
    public int ClassFact { get; set; }
    public int EReportDone { get; set; }
    public int EReportRemarks { get; set; }
    public int PReportDone { get; set; }
    public int PReportRemarks { get; set; }
    public List<DisciplineGroupStatisticsDto> Groups { get; set; } = [];
}

public sealed class DisciplineGroupStatisticsDto
{
    public string GroupNumber { get; set; } = string.Empty;
    public int Course { get; set; }
    public string Teacher { get; set; } = string.Empty;
    public string EReportStatus { get; set; } = "pending";
    public string PReportStatus { get; set; } = "pending";
    public bool AllPassed { get; set; }
}
