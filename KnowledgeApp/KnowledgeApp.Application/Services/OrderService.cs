using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.DTOs.Orders;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Repositories;
using KnowledgeApp.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Application.Services;

public class OrderService
{
    private const int MaxNumberLength = 50;
    private const int MaxPersonNameLength = 200;

    private readonly OrderRepository _repository;
    private readonly OrderPdfGenerator _pdfGenerator;
    private readonly IPdfGeneratorService _groupReportPdfGenerator;
    private readonly GroupReportService _groupReportService;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserProvider _currentUser;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        OrderRepository repository,
        OrderPdfGenerator pdfGenerator,
        IPdfGeneratorService groupReportPdfGenerator,
        GroupReportService groupReportService,
        IFileStorage storage,
        ICurrentUserProvider currentUser,
        ILogger<OrderService> logger)
    {
        _repository = repository;
        _pdfGenerator = pdfGenerator;
        _groupReportPdfGenerator = groupReportPdfGenerator;
        _groupReportService = groupReportService;
        _storage = storage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<int> Create(CreateOrderDto? request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new DocumentValidationException("Данные приказа не переданы");

        var order = new OrderModel
        {
            Number = RequireText(request.Number, "Номер приказа", MaxNumberLength),
            Name = OrderPdfGenerator.OrderName,
            OrderDate = RequireDate(request.OrderDate, "Дата приказа"),
            GroupId = RequireId(request.GroupId, "Группа"),
            EducationControlDepartmentHead = RequireText(request.EducationControlDepartmentHead,
                "ФИО начальника отдела контроля образовательного процесса", MaxPersonNameLength),
            EducationalMethodicalCenterHead = RequireText(request.EducationalMethodicalCenterHead,
                "ФИО начальника центра по учебно-методической работе", MaxPersonNameLength),
            InspectionSummaryDeadline = RequireDate(request.InspectionSummaryDeadline,
                "Срок сводного отчёта по проверке"),
            SurveySummaryDeadline = RequireDate(request.SurveySummaryDeadline,
                "Срок сводного отчёта по анкетированию"),
            PaperReportDeadline = RequireDate(request.PaperReportDeadline, "Срок сдачи бумажных отчётов"),
            ElectronicReportDeadline = RequireDate(request.ElectronicReportDeadline, "Срок сдачи электронных отчётов"),
            TestingStartDate = RequireDate(request.TestingStartDate, "Начало тестирования"),
            TestingEndDate = RequireDate(request.TestingEndDate, "Конец тестирования"),
            SurveyStartDate = RequireDate(request.SurveyStartDate, "Начало анкетирования"),
            SurveyEndDate = RequireDate(request.SurveyEndDate, "Окончание анкетирования"),
            PreparationStartDate = RequireDate(request.PreparationStartDate, "Начало подготовительного этапа"),
            PreparationEndDate = RequireDate(request.PreparationEndDate, "Окончание подготовительного этапа"),
            ResultsProcessingStartDate = RequireDate(
                request.ResultsProcessingStartDate, "Начало этапа обработки результатов")
        };

        if (order.TestingEndDate < order.TestingStartDate)
            throw new DocumentValidationException("Конец тестирования раньше его начала");
        if (order.SurveyEndDate < order.SurveyStartDate)
            throw new DocumentValidationException("Окончание анкетирования раньше его начала");
        if (order.SurveySummaryDeadline < order.SurveyEndDate)
            throw new DocumentValidationException("Сводный отчёт по анкетированию нельзя сдавать до окончания анкетирования");
        if (order.PreparationEndDate < order.PreparationStartDate)
            throw new DocumentValidationException("Окончание подготовительного этапа раньше его начала");
        if (order.PreparationStartDate < order.OrderDate || order.PreparationEndDate > order.TestingStartDate)
            throw new DocumentValidationException(
                "Подготовительный этап должен проходить между датой приказа и началом тестирования");
        if (order.ResultsProcessingStartDate < order.TestingEndDate ||
            order.InspectionSummaryDeadline < order.ResultsProcessingStartDate)
            throw new DocumentValidationException(
                "Этап обработки результатов должен идти после тестирования и завершиться до срока сводного отчёта");

        if (request.SemesterId == null)
            throw new DocumentValidationException("Семестр не указан");
        var semester = await _repository.GetSemester(request.SemesterId.Value)
            ?? throw new DocumentValidationException("Указанный семестр не существует");

        order.SemesterId = semester.Id;
        order.SemesterYear = semester.SemesterYear;
        order.SemesterPart = semester.SemesterPart;
        var reportPages = await _groupReportService.GetReportPagesAsync(
            order.GroupId, order.SemesterId, cancellationToken);
        if (reportPages.Count != 2)
            throw new DocumentValidationException(
                $"Для приказа необходимо выбрать группу ровно с двумя дисциплинами; найдено: {reportPages.Count}");

        order.FileKey = $"orders/{Guid.NewGuid():N}.pdf";

        try
        {
            var pdf = _pdfGenerator.GenerateWithGroupReports(
                order, reportPages, _groupReportPdfGenerator);
            order.FileSize = pdf.Length;

            using var content = new MemoryStream(pdf);
            await _storage.SaveAsync(order.FileKey, content, "application/pdf", cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            throw new DocumentGenerationException("Не удалось сформировать файл приказа", e);
        }

        try
        {
            return await _repository.Create(order, DateTime.UtcNow, _currentUser.UserId);
        }
        catch
        {
            await DeleteQuietly(order.FileKey);
            throw;
        }
    }

    public async Task<List<OrderEssentialsDto>> GetEssentials()
    {
        var orders = await _repository.GetAll();
        return orders.Select(o => new OrderEssentialsDto
        {
            Id = o.Id,
            Number = o.Number,
            Name = o.Name,
            OrderDate = o.OrderDate,
            Semester = new OrderSemesterDto
            {
                Id = o.SemesterId,
                SemesterYear = o.SemesterYear,
                SemesterPart = o.SemesterPart
            }
        }).ToList();
    }

    public async Task<DocumentFileDto> OpenFile(int id, CancellationToken cancellationToken)
    {
        var fileKey = await _repository.GetFileKey(id)
            ?? throw new DocumentNotFoundException("Приказ не найден");

        var stream = await _storage.OpenReadAsync(fileKey, cancellationToken)
            ?? throw new StoredFileMissingException("Файл приказа не найден в хранилище");

        return new DocumentFileDto { Content = stream, FileName = $"order-{id}.pdf" };
    }

    private static string RequireText(string? value, string field, int maxLength)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text))
            throw new DocumentValidationException($"Не заполнено поле «{field}»");
        if (text.Length > maxLength)
            throw new DocumentValidationException($"Поле «{field}» длиннее {maxLength} символов");
        return text;
    }

    private static DateOnly RequireDate(DateOnly? value, string field) =>
        value ?? throw new DocumentValidationException($"Не заполнено поле «{field}»");

    private static int RequireId(int? value, string field) =>
        value is > 0 ? value.Value : throw new DocumentValidationException($"Не указана корректная сущность «{field}»");

    private async Task DeleteQuietly(string key)
    {
        try
        {
            await _storage.DeleteAsync(key);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Не удалось удалить объект {Key} из хранилища", key);
        }
    }
}
