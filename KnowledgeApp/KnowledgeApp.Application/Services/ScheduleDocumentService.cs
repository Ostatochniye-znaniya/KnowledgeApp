using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Repositories;
using KnowledgeApp.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Application.Services;

public class ScheduleDocumentService
{
    private readonly ScheduleDocumentRepository _repository;
    private readonly SemesterRepository _semesterRepository;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserProvider _currentUser;
    private readonly ILogger<ScheduleDocumentService> _logger;

    public ScheduleDocumentService(ScheduleDocumentRepository repository, SemesterRepository semesterRepository,
        IFileStorage storage, ICurrentUserProvider currentUser, ILogger<ScheduleDocumentService> logger)
    {
        _repository = repository;
        _semesterRepository = semesterRepository;
        _storage = storage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ScheduleDocumentDto> Upload(int semesterId, Stream content, string? fileName, long? declaredSize,
        CancellationToken cancellationToken)
    {
        PdfUpload.EnsureDeclaredSize(declaredSize);
        var facultyId = RequireFaculty();

        if (!await _repository.SemesterExists(semesterId))
            throw new DocumentValidationException("Семестр не найден");
        if (!await _repository.FacultyExists(facultyId))
            throw new DocumentValidationException("Факультет не найден");

        var existing = await _repository.GetByFacultyAndSemester(facultyId, semesterId);
        if (existing?.Status == DocumentStatus.Signed)
            throw new DocumentConflictException("График на этот семестр уже подписан деканом");

        using var upload = await PdfUpload.ReadAsync(content, fileName, declaredSize, cancellationToken);

        var key = $"schedules/{facultyId}/{semesterId}/{Guid.NewGuid():N}.pdf";
        await _storage.SaveAsync(key, upload.Content, "application/pdf", cancellationToken);

        (bool Saved, string? PreviousKey) result;
        try
        {
            result = await _repository.Upsert(facultyId, semesterId, key, upload.FileName, upload.Size,
                DateTime.UtcNow, _currentUser.UserId);
        }
        catch
        {
            await DeleteQuietly(key);
            throw;
        }

        if (!result.Saved)
        {
            await DeleteQuietly(key);
            throw new DocumentConflictException("График изменился во время загрузки (уже подписан или заменён) — обновите страницу");
        }

        if (result.PreviousKey != null && result.PreviousKey != key)
            await DeleteQuietly(result.PreviousKey);

        var saved = await _repository.GetByFacultyAndSemester(facultyId, semesterId)
            ?? throw new DocumentNotFoundException("График не найден");
        return ToDto(saved);
    }

    public async Task<ScheduleDocumentDto?> GetCurrent(int semesterId)
    {
        var facultyId = RequireFaculty();
        var document = await _repository.GetByFacultyAndSemester(facultyId, semesterId);
        return document == null ? null : ToDto(document);
    }

    public async Task<List<ScheduleDocumentDto>> GetForDean(int? semesterId, string? status)
    {
        var facultyId = RequireFaculty();
        var statusFilter = DocumentStatusInfo.Parse(status);

        var documents = await _repository.GetByFaculty(facultyId, semesterId, statusFilter);
        return documents.Select(ToDto).ToList();
    }

    public async Task<ScheduleFilterOptionsDto> GetFilterOptions()
    {
        RequireFaculty();

        var semesters = (await _semesterRepository.GetAllSemesters())
            .OrderByDescending(s => s.SemesterYear)
            .ThenByDescending(s => s.SemesterPart)
            .Select(s => new FilterOptionDto
            {
                Value = s.Id.ToString(),
                Label = AcademicPeriod.FormatSemester(s.SemesterYear, s.SemesterPart)
            })
            .ToList();

        return new ScheduleFilterOptionsDto
        {
            Semesters = semesters,
            Statuses = DocumentStatusInfo.Options()
        };
    }

    public async Task<DocumentFileDto> OpenFile(int id, CancellationToken cancellationToken)
    {
        var document = await GetOwnDocument(id);

        var stream = await _storage.OpenReadAsync(document.FileKey, cancellationToken)
            ?? throw new StoredFileMissingException("Файл графика не найден в хранилище");

        return new DocumentFileDto { Content = stream, FileName = document.FileName };
    }

    public Task<ScheduleDocumentDto> Approve(int id) =>
        Review(id, DocumentStatus.Signed, null);

    public Task<ScheduleDocumentDto> Reject(int id, string? comment) =>
        Review(id, DocumentStatus.Rejected, ReportDocumentService.NormalizeComment(comment));

    private async Task<ScheduleDocumentDto> Review(int id, DocumentStatus newStatus, string? comment)
    {
        await GetOwnDocument(id);

        var changed = await _repository.Review(id, newStatus, comment, DateTime.UtcNow, _currentUser.UserId);
        if (!changed)
            throw new DocumentConflictException("График уже рассмотрен");

        var updated = await _repository.GetById(id)
            ?? throw new DocumentNotFoundException("График не найден");
        return ToDto(updated);
    }

    private async Task<ScheduleDocumentModel> GetOwnDocument(int id)
    {
        var facultyId = RequireFaculty();
        var document = await _repository.GetById(id);

        if (document == null || document.FacultyId != facultyId)
            throw new DocumentNotFoundException("График не найден");

        return document;
    }

    private int RequireFaculty() =>
        _currentUser.FacultyId ?? throw new DocumentAccessException("Не определён факультет пользователя");

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

    private static ScheduleDocumentDto ToDto(ScheduleDocumentModel document) => new()
    {
        Id = document.Id,
        FacultyId = document.FacultyId,
        SemesterId = document.SemesterId,
        SemesterYear = document.SemesterYear,
        SemesterPart = document.SemesterPart,
        Period = AcademicPeriod.FormatSemester(document.SemesterYear, document.SemesterPart),
        Status = DocumentStatusInfo.Code(document.Status),
        StatusLabel = DocumentStatusInfo.Label(document.Status),
        FileName = document.FileName,
        FileSize = document.FileSize,
        UploadedAt = DocumentStatusInfo.AsUtc(document.UploadedAt),
        ReviewComment = document.ReviewComment,
        ReviewedAt = DocumentStatusInfo.AsUtc(document.ReviewedAt)
    };
}
