using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Entities;

namespace KnowledgeApp.UnitTests.Services;

public class StatisticsCalculationsTests
{
    [Theory]
    [InlineData(3, 3, false, 1, 19)] // 3*(2*3+0)+1
    [InlineData(3, 3, true, 1, 22)]  // 3*(2*3+1)+1
    [InlineData(0, 3, true, 0, 0)]
    public void CalculateGroupNorm_MatchesFrontendFormula(
        int profiles, int courses, bool isSpring, int magistracy, int expected)
    {
        Assert.Equal(
            expected,
            StatisticsCalculations.CalculateGroupNorm(profiles, courses, isSpring, magistracy));
    }

    [Theory]
    [InlineData("Факультет информационных технологий", "ФИТ")]
    [InlineData("ИиИТ", "ИИИТ")]
    [InlineData("", "—")]
    [InlineData("   ", "—")]
    public void ShortCode_BuildsAbbreviation(string name, string expected)
    {
        Assert.Equal(expected, StatisticsCalculations.ShortCode(name));
    }

    [Theory]
    [InlineData("221-111", 2)]
    [InlineData("341-201", 3)]
    [InlineData("ab", 0)]
    [InlineData(null, 0)]
    public void GuessCourse_ReadsLeadingDigitFromGroupNumber(string? groupNumber, int expected)
    {
        Assert.Equal(expected, StatisticsCalculations.GuessCourse(groupNumber));
    }

    [Fact]
    public void MapElectronicStatus_UsesReportFlags()
    {
        Assert.Equal("pending", StatisticsCalculations.MapElectronicStatus(null));
        Assert.Equal("done", StatisticsCalculations.MapElectronicStatus(new Report
        {
            DoneInElectronicForm = true,
            Status = DocumentStatus.Pending
        }));
        Assert.Equal("done", StatisticsCalculations.MapElectronicStatus(new Report
        {
            Status = DocumentStatus.Signed
        }));
        Assert.Equal("remarks", StatisticsCalculations.MapElectronicStatus(new Report
        {
            Status = DocumentStatus.Rejected
        }));
    }

    [Fact]
    public void MapPaperStatus_UsesPaperFlagAndRejection()
    {
        Assert.Equal("pending", StatisticsCalculations.MapPaperStatus(null));
        Assert.Equal("done", StatisticsCalculations.MapPaperStatus(new Report { DoneInPaperForm = true }));
        Assert.Equal("remarks", StatisticsCalculations.MapPaperStatus(new Report
        {
            DoneInPaperForm = false,
            Status = DocumentStatus.Rejected
        }));
        Assert.Equal("pending", StatisticsCalculations.MapPaperStatus(new Report
        {
            DoneInPaperForm = false,
            Status = DocumentStatus.Pending
        }));
    }
}
