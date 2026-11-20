using KnowledgeApp.Application.Services;

namespace KnowledgeApp.UnitTests.Services;

public class AcademicPeriodTests
{
    [Theory]
    [InlineData(2026, 2, 1, "2026/2027")] // осенний семестр начинает учебный год
    [InlineData(2025, 1, 2, "2024/2025")] // весенний семестр завершает учебный год
    [InlineData(2025, 1, null, "2024/2025")] // без year_part весна определяется по semester_part
    [InlineData(2025, 1, 0, "2024/2025")]
    [InlineData(2025, 2, null, "2025/2026")]
    public void FormatAcademicYear_ReturnsStartAndEndYear(int year, int part, int? yearPart, string expected)
    {
        Assert.Equal(expected, AcademicPeriod.FormatAcademicYear(year, part, yearPart));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void FormatAcademicYear_WithoutYear_ReturnsNull(int? year)
    {
        Assert.Null(AcademicPeriod.FormatAcademicYear(year, 1, 2));
    }

    [Theory]
    [InlineData(2026, 1, "2026, весенний семестр")]
    [InlineData(2026, 2, "2026, осенний семестр")]
    public void FormatSemester_NamesSeason(int year, int part, string expected)
    {
        Assert.Equal(expected, AcademicPeriod.FormatSemester(year, part));
    }
}
