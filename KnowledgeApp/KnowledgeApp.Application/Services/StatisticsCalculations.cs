using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Entities;

namespace KnowledgeApp.Application.Services;

public static class StatisticsCalculations
{
    public static int CalculateGroupNorm(int profilesCount, int coursesCount, bool isSpring, int magistracyCount)
    {
        var springFirstCourseAdd = isSpring ? 1 : 0;
        return profilesCount * (2 * coursesCount + springFirstCourseAdd) + magistracyCount;
    }

    public static string ShortCode(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "—";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
            return parts[0].Length <= 6 ? parts[0].ToUpperInvariant() : parts[0][..3].ToUpperInvariant();
        return string.Concat(parts.Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0])));
    }

    public static int GuessCourse(string? groupNumber)
    {
        if (string.IsNullOrWhiteSpace(groupNumber)) return 0;
        var digits = new string(groupNumber.Where(char.IsDigit).ToArray());
        if (digits.Length >= 3 && int.TryParse(digits.AsSpan(0, 1), out var course))
            return course;
        return 0;
    }

    public static string MapElectronicStatus(Report? report)
    {
        if (report is null) return "pending";
        if (report.Status == DocumentStatus.Rejected) return "remarks";
        if (report.DoneInElectronicForm == true || report.Status == DocumentStatus.Signed) return "done";
        return "pending";
    }

    public static string MapPaperStatus(Report? report)
    {
        if (report is null) return "pending";
        if (report.DoneInPaperForm == true) return "done";
        if (report.Status == DocumentStatus.Rejected && report.DoneInPaperForm == false) return "remarks";
        return "pending";
    }
}
