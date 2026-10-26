using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.DTOs.Orders;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Repositories;
using KnowledgeApp.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Application.Services;

public class OrderService
{
    private const int MaxPersonNameLength = 200;

    private readonly OrderRepository _repository;
    private readonly OrderPdfGenerator _pdfGenerator;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserProvider _currentUser;
    private readonly ILogger<OrderService> _logger;

    public OrderService(OrderRepository repository, OrderPdfGenerator pdfGenerator, IFileStorage storage,
        ICurrentUserProvider currentUser, ILogger<OrderService> logger)
    {
        _repository = repository;
        _pdfGenerator = pdfGenerator;
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
            Number = null,
            Name = OrderPdfGenerator.OrderName,
            Status = RequireStatus(request.Status),
            OrderDate = RequireDate(request.OrderDate, "Дата приказа"),
            EducationControlDepartmentHead = RequireText(request.EducationControlDepartmentHead,
                "ФИО начальника отдела контроля образовательного процесса", MaxPersonNameLength),
            EducationalMethodicalCenterHead = RequireText(request.EducationalMethodicalCenterHead,
                "ФИО начальника центра по учебно-методической работе", MaxPersonNameLength),
            InspectionSummaryDeadline = RequireDate(request.InspectionSummaryDeadline,
                "Срок сводного отчёта по проверке"),
            SurveySummaryDeadline = RequireDate(request.SurveySummaryDeadline,
                "Срок сводного отчёта по анкетированию"),
            RequiresPaperReports = request.RequiresPaperReports,
            PaperReportDeadline = request.RequiresPaperReports
                ? RequireDate(request.PaperReportDeadline, "Срок сдачи бумажных отчётов") : null,
            ElectronicReportDeadline = RequireDate(request.ElectronicReportDeadline, "Срок сдачи электронных отчётов"),
            TestingStartDate = RequireDate(request.TestingStartDate, "Начало тестирования"),
            TestingEndDate = RequireDate(request.TestingEndDate, "Конец тестирования"),
            SurveyStartDate = RequireDate(request.SurveyStartDate, "Начало анкетирования"),
            SurveyEndDate = RequireDate(request.SurveyEndDate, "Окончание анкетирования"),
            PreparationStartDate = RequireDate(request.PreparationStartDate, "Начало подготовительного этапа"),
            PreparationEndDate = RequireDate(request.PreparationEndDate, "Окончание подготовительного этапа"),
            ResultsProcessingStartDate = RequireDate(
                request.ResultsProcessingStartDate, "Начало этапа обработки результатов"),
            TestingStageStartDate = RequireDate(request.TestingStageStartDate, "Начало второго этапа"),
            TestingStageEndDate = RequireDate(request.TestingStageEndDate, "Окончание второго этапа"),
            OnlineSeminarDate = RequireDate(request.OnlineSeminarDate, "Обучающий онлайн-семинар")
        };

        OrderDates.Validate(order);

        if (request.SemesterId == null)
            throw new DocumentValidationException("Семестр не указан");
        var semester = await _repository.GetSemester(request.SemesterId.Value)
            ?? throw new DocumentValidationException("Указанный семестр не существует");

        order.SemesterId = semester.Id;
        order.SemesterYear = semester.SemesterYear;
        order.SemesterPart = semester.SemesterPart;

        var token = Guid.NewGuid().ToString("N");
        var uploaded = new HashSet<string>();
        try
        {
            var id = await _repository.Write(async () =>
            {
                // Проверка токена также покрывает повтор после неопределённого результата COMMIT.
                var existing = await _repository.FindCreation(token);
                if (existing.HasValue) return existing.Value;
                if (order.Status == OrderStatuses.Issued)
                {
                    await _repository.LockNumbers(cancellationToken);
                    order.Number = await _repository.AllocateNumber(cancellationToken);
                }
                await GenerateFile(order, uploaded, cancellationToken);
                return await _repository.Create(order, DateTime.UtcNow, _currentUser.UserId, token);
            }, cancellationToken);
            await CleanupUnreferenced(uploaded);
            return id;
        }
        catch
        {
            await CleanupUnreferenced(uploaded);
            throw;
        }
    }

    public async Task<OrderDetailsDto> Sign(int id, CancellationToken ct)
    {
        var uploaded = new HashSet<string>();
        string? oldKey = null;
        try
        {
            await _repository.Write(async () =>
            {
                await _repository.LockNumbers(ct);
                var entity = await _repository.LockOrder(id, ct)
                    ?? throw new DocumentNotFoundException("Приказ не найден");
                if (entity.Status == OrderStatuses.Issued) return true; // Повторное подписание не выдаёт новый номер.
                var model = await _repository.GetDetails(id, ct)
                    ?? throw new DocumentNotFoundException("Приказ не найден");
                OrderDates.Validate(model);
                model.Status = OrderStatuses.Issued;
                model.Number = await _repository.AllocateNumber(ct);
                await GenerateFile(model, uploaded, ct);
                oldKey = entity.FileKey;
                entity.Status = model.Status;
                entity.Number = model.Number;
                entity.FileKey = model.FileKey;
                entity.FileSize = model.FileSize;
                await _repository.Save(ct);
                return true;
            }, ct);
            if (oldKey is not null) uploaded.Add(oldKey);
            await CleanupUnreferenced(uploaded);
            return await GetDetails(id, ct);
        }
        catch
        {
            // При неизвестном результате COMMIT не удаляем файл, пока БД не подтвердила, что на него нет ссылки.
            await CleanupUnreferenced(uploaded);
            throw;
        }
    }

    private async Task GenerateFile(OrderModel order, ISet<string> uploaded, CancellationToken ct)
    {
        order.FileKey = $"orders/{Guid.NewGuid():N}.pdf";
        try
        {
            var pdf = _pdfGenerator.Generate(order);
            order.FileSize = pdf.Length;
            uploaded.Add(order.FileKey);
            using var content = new MemoryStream(pdf);
            await _storage.SaveAsync(order.FileKey, content, "application/pdf", ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            throw new DocumentGenerationException("Не удалось сформировать файл приказа", e);
        }
    }

    private async Task CleanupUnreferenced(IEnumerable<string> keys)
    {
        foreach (var key in keys)
        {
            try
            {
                if (!await _repository.IsFileReferenced(key)) await DeleteQuietly(key);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Не удалось проверить ссылку на файл {Key}; файл сохранён", key);
            }
        }
    }

    public async Task<List<OrderEssentialsDto>> GetEssentials(string? status = null)
    {
        var orders = await _repository.GetAll(status is null ? null : RequireStatus(status));
        return orders.Select(o => new OrderEssentialsDto
        {
            Id = o.Id,
            Number = o.Number,
            Name = o.Name,
            Status = o.Status,
            OrderDate = o.OrderDate,
            TestingStartDate = o.TestingStartDate,
            TestingEndDate = o.TestingEndDate,
            Semester = new OrderSemesterDto
            {
                Id = o.SemesterId,
                SemesterYear = o.SemesterYear,
                SemesterPart = o.SemesterPart
            }
        }).ToList();
    }

    public async Task<OrderDetailsDto> GetDetails(int id, CancellationToken cancellationToken)
    {
        var order = await _repository.GetDetails(id, cancellationToken)
            ?? throw new DocumentNotFoundException("Приказ не найден");
        return new OrderDetailsDto
        {
            Id = order.Id, Number = order.Number, Name = order.Name, Status = order.Status,
            OrderDate = order.OrderDate,
            Semester = new OrderSemesterDto
            {
                Id = order.SemesterId, SemesterYear = order.SemesterYear, SemesterPart = order.SemesterPart
            },
            TestingStartDate = order.TestingStartDate, TestingEndDate = order.TestingEndDate,
            EducationControlDepartmentHead = order.EducationControlDepartmentHead,
            EducationalMethodicalCenterHead = order.EducationalMethodicalCenterHead,
            InspectionSummaryDeadline = order.InspectionSummaryDeadline,
            SurveySummaryDeadline = order.SurveySummaryDeadline,
            PaperReportDeadline = order.PaperReportDeadline, RequiresPaperReports = order.RequiresPaperReports,
            PreparationStartDate = order.PreparationStartDate, PreparationEndDate = order.PreparationEndDate,
            TestingStageStartDate = order.TestingStageStartDate, TestingStageEndDate = order.TestingStageEndDate,
            ResultsProcessingStartDate = order.ResultsProcessingStartDate,
            SurveyStartDate = order.SurveyStartDate, SurveyEndDate = order.SurveyEndDate,
            OnlineSeminarDate = order.OnlineSeminarDate,
            ElectronicReportDeadline = order.ElectronicReportDeadline,
            FileSize = order.FileSize, CreatedAt = DateTime.SpecifyKind(order.CreatedAt, DateTimeKind.Utc),
            CreatedByUserId = order.CreatedByUserId, CreatedByName = order.CreatedByName
        };
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

    private static string RequireStatus(string? value)
    {
        var status = value?.Trim().ToLowerInvariant() ?? OrderStatuses.Issued;
        if (!OrderStatuses.IsValid(status))
            throw new DocumentValidationException("Допустимые статусы приказа: draft, issued");
        return status;
    }

    private static DateOnly RequireDate(DateOnly? value, string field) =>
        value ?? throw new DocumentValidationException($"Не заполнено поле «{field}»");

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
