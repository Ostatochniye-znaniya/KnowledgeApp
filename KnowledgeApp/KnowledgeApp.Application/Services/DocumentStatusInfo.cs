using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Domain.Enums;

namespace KnowledgeApp.Application.Services;

public static class DocumentStatusInfo
{
    public static string Code(DocumentStatus status) => status.ToString().ToLowerInvariant();

    public static string Label(DocumentStatus status) => status switch
    {
        DocumentStatus.Pending => "Ожидает подписи",
        DocumentStatus.Signed => "Подписан",
        DocumentStatus.Rejected => "Отклонён",
        _ => status.ToString()
    };

    public static DocumentStatus? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        foreach (var status in Enum.GetValues<DocumentStatus>())
        {
            if (string.Equals(Code(status), value.Trim(), StringComparison.OrdinalIgnoreCase))
                return status;
        }

        throw new DocumentValidationException("Неизвестный статус. Допустимо: pending, signed, rejected");
    }

    // datetime из MySQL читается без зоны, а пишем мы всегда UtcNow
    public static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static DateTime? AsUtc(DateTime? value) => value.HasValue ? AsUtc(value.Value) : null;

    public static List<FilterOptionDto> Options() =>
        Enum.GetValues<DocumentStatus>()
            .Select(s => new FilterOptionDto { Value = Code(s), Label = Label(s) })
            .ToList();
}
