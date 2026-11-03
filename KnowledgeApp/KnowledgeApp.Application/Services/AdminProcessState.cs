using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Services;

public static class AdminProcessState
{
    public const string NotAssigned = "not_assigned";
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string Expired = "expired";

    public const string StageNotAssigned = "not_assigned";
    public const string StageTimeSelection = "time_selection";
    public const string StageApproval = "approval";
    public const string StageApproved = "approved";
    public const string StageReportReview = "report_review";
    public const string StageReportSigned = "report_signed";
    public const string StagePaperSubmitted = "paper_submitted";

    public const string ReportNotSubmitted = "not_submitted";
    public const string ReportElectronicSubmitted = "electronic_submitted";
    public const string ReportRejected = "rejected";
    public const string ReportSigned = "signed";
    public const string ReportPaperSubmitted = "paper_submitted";

    private static readonly IReadOnlyList<(string Code, string Label)> ApprovalLabels =
    [
        (NotAssigned, "Не назначено"),
        (Pending, "На согласовании"),
        (Approved, "Согласовано"),
        (Rejected, "Отклонено"),
        (Expired, "Срок истёк")
    ];

    private static readonly IReadOnlyList<(string Code, string Label)> StageLabels =
    [
        (StageNotAssigned, "Преподаватели не назначены"),
        (StageTimeSelection, "Выбор времени"),
        (StageApproval, "Согласование времени"),
        (StageApproved, "Время согласовано"),
        (StageReportReview, "Отчёт на проверке"),
        (StageReportSigned, "Отчёт подписан"),
        (StagePaperSubmitted, "Бумажный отчёт сдан")
    ];

    private static readonly IReadOnlyList<(string Code, string Label)> ReportLabels =
    [
        (ReportNotSubmitted, "Не сдан"),
        (ReportElectronicSubmitted, "Электронный сдан"),
        (ReportRejected, "На доработке"),
        (ReportSigned, "Подписан"),
        (ReportPaperSubmitted, "Бумажный сдан")
    ];

    public sealed record Snapshot(
        IReadOnlyCollection<string> TeacherStatuses,
        DateTime? PeriodStart,
        DateTime? PeriodEnd,
        DateTime? ScheduledDate,
        TimeSpan? ScheduledTime,
        bool ReportUploaded,
        DocumentStatus? ReportStatus,
        bool? PaperSubmitted);

    public static string ApprovalStatus(Snapshot process, DateOnly today)
    {
        if (process.TeacherStatuses.Count == 0 || !process.PeriodStart.HasValue || !process.PeriodEnd.HasValue)
            return NotAssigned;
        if (process.TeacherStatuses.Any(s => s == Rejected))
            return Rejected;
        if (process.TeacherStatuses.All(s => s == Approved))
            return Approved;
        return DateOnly.FromDateTime(process.PeriodEnd.Value) < today ? Expired : Pending;
    }

    public static string ReportStatus(Snapshot process)
    {
        if (!process.ReportUploaded)
            return ReportNotSubmitted;
        return process.ReportStatus switch
        {
            DocumentStatus.Signed when process.PaperSubmitted == true => ReportPaperSubmitted,
            DocumentStatus.Signed => ReportSigned,
            DocumentStatus.Rejected => ReportRejected,
            _ => ReportElectronicSubmitted
        };
    }

    public static string Stage(Snapshot process, DateOnly today)
    {
        var approval = ApprovalStatus(process, today);
        if (approval == NotAssigned)
            return StageNotAssigned;
        if (!process.ScheduledDate.HasValue || !process.ScheduledTime.HasValue)
            return StageTimeSelection;
        if (approval != Approved)
            return StageApproval;

        return ReportStatus(process) switch
        {
            ReportElectronicSubmitted => StageReportReview,
            ReportSigned => StageReportSigned,
            ReportPaperSubmitted => StagePaperSubmitted,
            _ => StageApproved
        };
    }

    public static int StageOrder(string stage) =>
        StageLabels.Select((s, index) => (s.Code, index)).Single(s => s.Code == stage).index;

    public static string? PreviousStage(string stage)
    {
        var order = StageOrder(stage);
        return order <= 1 ? null : StageLabels[order - 1].Code;
    }

    public static string ApprovalLabel(string code) => Label(ApprovalLabels, code);

    public static string StageLabel(string code) => Label(StageLabels, code);

    public static string ReportLabel(string code) => Label(ReportLabels, code);

    public static string TeacherStatusLabel(string? status) => status switch
    {
        Approved => "Согласовал",
        Rejected => "Отклонил",
        _ => "Ожидает ответа"
    };

    public static List<FilterOptionDto> ApprovalOptions() => Options(ApprovalLabels);

    public static List<FilterOptionDto> StageOptions() => Options(StageLabels);

    public static List<FilterOptionDto> ReportOptions() => Options(ReportLabels);

    public static IReadOnlyCollection<string> Normalize(
        IEnumerable<string>? values,
        Func<List<FilterOptionDto>> options,
        string name)
    {
        var allowed = options().Select(o => o.Value).ToHashSet(StringComparer.Ordinal);
        var result = (values ?? [])
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(v => v.ToLowerInvariant())
            .Distinct()
            .ToList();
        var unknown = result.FirstOrDefault(v => !allowed.Contains(v));
        if (unknown is not null)
            throw new Exceptions.DocumentValidationException(
                $"Неизвестное значение фильтра {name}: {unknown}. Допустимо: {string.Join(", ", allowed)}");
        return result;
    }

    private static string Label(IReadOnlyList<(string Code, string Label)> labels, string code) =>
        labels.Single(l => l.Code == code).Label;

    private static List<FilterOptionDto> Options(IReadOnlyList<(string Code, string Label)> labels) =>
        labels.Select(l => new FilterOptionDto { Value = l.Code, Label = l.Label }).ToList();
}
