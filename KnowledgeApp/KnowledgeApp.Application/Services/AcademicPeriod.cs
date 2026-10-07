namespace KnowledgeApp.Application.Services;

public static class AcademicPeriod
{
    public static string? FormatAcademicYear(int? semesterYear, int? semesterPart, int? yearPart)
    {
        if (semesterYear is not > 0) return null;

        var isSpring = yearPart == 2 || ((yearPart is null or 0) && semesterPart == 1);
        var startYear = isSpring ? semesterYear.Value - 1 : semesterYear.Value;
        return $"{startYear}/{startYear + 1}";
    }

    public static string FormatSemester(int semesterYear, int semesterPart) =>
        $"{semesterYear}, {(semesterPart == 1 ? "весенний" : "осенний")} семестр";
}
