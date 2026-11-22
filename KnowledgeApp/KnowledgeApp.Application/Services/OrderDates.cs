using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Domain.Entities;

namespace KnowledgeApp.Application.Services;

public static class OrderDates
{
    public static void Validate(OrderModel order)
    {
        if (order.TestingEndDate < order.TestingStartDate)
            throw new DocumentValidationException("Конец общего периода раньше его начала");
        var dates = new (DateOnly? Date, string Label)[]
        {
            (order.PreparationStartDate, "Начало подготовительного этапа"),
            (order.PreparationEndDate, "Окончание подготовительного этапа"),
            (order.TestingStageStartDate, "Начало второго этапа"),
            (order.TestingStageEndDate, "Окончание второго этапа"),
            (order.ResultsProcessingStartDate, "Начало обработки результатов"),
            (order.InspectionSummaryDeadline, "Окончание обработки результатов / сводный отчёт"),
            (order.SurveyStartDate, "Начало анкетирования"),
            (order.SurveyEndDate, "Окончание анкетирования"),
            (order.SurveySummaryDeadline, "Сводный отчёт по анкетированию"),
            (order.ElectronicReportDeadline, "Сдача электронных отчётов"),
            (order.OnlineSeminarDate, "Обучающий онлайн-семинар"),
        };
        if (order.RequiresPaperReports)
            dates = dates.Append((order.PaperReportDeadline, "Сдача бумажных отчётов")).ToArray();
        foreach (var (date, label) in dates)
        {
            if (date is null || date == DateOnly.MinValue)
                throw new DocumentValidationException($"Не заполнено поле «{label}»");
            if (date < order.TestingStartDate || date > order.TestingEndDate)
                throw new DocumentValidationException($"«{label}» должно входить в общий период проведения проверки");
        }
        if (order.PreparationEndDate < order.PreparationStartDate ||
            order.TestingStageEndDate < order.TestingStageStartDate ||
            order.InspectionSummaryDeadline < order.ResultsProcessingStartDate ||
            order.TestingStageStartDate < order.PreparationEndDate ||
            order.ResultsProcessingStartDate < order.TestingStageEndDate)
            throw new DocumentValidationException("Три этапа должны идти последовательно: подготовка, тестирование, обработка результатов");
        if (order.SurveyEndDate < order.SurveyStartDate || order.SurveySummaryDeadline < order.SurveyEndDate)
            throw new DocumentValidationException("Проверьте последовательность дат анкетирования и сводного отчёта");
    }
}
