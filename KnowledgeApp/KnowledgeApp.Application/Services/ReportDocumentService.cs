using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Repositories;
using KnowledgeApp.Infrastructure.Storage;
using Microsoft.Extensions.Logging;

namespace KnowledgeApp.Application.Services;

public class ReportDocumentService
{
    private readonly ReportDocumentRepository _repository;
    private readonly DepartmentRepository _departmentRepository;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserProvider _currentUser;
    private readonly ILogger<ReportDocumentService> _logger;

    public ReportDocumentService(ReportDocumentRepository repository, DepartmentRepository departmentRepository,
        IFileStorage storage, ICurrentUserProvider currentUser, ILogger<ReportDocumentService> logger)
    {
        _repository = repository;
        _departmentRepository = departmentRepository;
        _storage = storage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ReportDocumentDto> UploadFile(int reportId, Stream content, string? fileName, long? declaredSize,
        CancellationToken cancellationToken)
    {
        PdfUpload.EnsureDeclaredSize(declaredSize);

        var report = await GetTeacherReport(reportId);
        if (report.Status == DocumentStatus.Signed)
            throw new DocumentConflictException("Отчёт уже подписан деканом, заменить файл нельзя");

        using var upload = await PdfUpload.ReadAsync(content, fileName, declaredSize, cancellationToken);

        var key = $"reports/{reportId}/{Guid.NewGuid():N}.pdf";
        await _storage.SaveAsync(key, upload.Content, "application/pdf", cancellationToken);

        (bool Replaced, string? PreviousKey) result;
        try
        {
            result = await _repository.AttachFile(reportId, key, upload.FileName, DateTime.UtcNow);
        }
        catch
        {
            await DeleteQuietly(key);
            throw;
        }

        if (!result.Replaced)
        {
            await DeleteQuietly(key);
            throw new DocumentConflictException("Отчёт изменился во время загрузки (уже подписан или заменён) — обновите страницу");
        }

        if (result.PreviousKey != null && result.PreviousKey != key)
            await DeleteQuietly(result.PreviousKey);

        var updated = await _repository.GetById(reportId)
            ?? throw new DocumentNotFoundException("Отчёт не найден");
        return ToDto(updated);
    }

    public async Task<DocumentFileDto> OpenFile(int reportId, CancellationToken cancellationToken)
    {
        var report = await GetTeacherReport(reportId);
        return await Open(report, cancellationToken);
    }

    public async Task<List<ReportDocumentDto>> GetForDean(int? departmentId, string? academicYear, string? status)
    {
        var facultyId = RequireFaculty();
        var statusFilter = DocumentStatusInfo.Parse(status);

        var reports = await _repository.GetSubmittedByFaculty(facultyId, departmentId, statusFilter);

        var result = reports.Select(ToDto);
        if (!string.IsNullOrWhiteSpace(academicYear))
            result = result.Where(r => r.AcademicYear == academicYear.Trim());

        return result
            .OrderBy(r => r.Status == "pending" ? 0 : 1)
            .ThenByDescending(r => r.AcademicYear)
            .ThenByDescending(r => r.UploadedAt)
            .ToList();
    }

    public async Task<ReportFilterOptionsDto> GetFilterOptions()
    {
        var facultyId = RequireFaculty();

        var departments = (await _departmentRepository.GetAllDepartments())
            .Where(d => d.FacultyId == facultyId)
            .OrderBy(d => d.Name)
            .Select(d => new FilterOptionDto { Value = d.Id.ToString(), Label = d.Name })
            .ToList();

        var years = (await _repository.GetSubmittedByFaculty(facultyId, null, null))
            .Select(r => AcademicPeriod.FormatAcademicYear(r.SemesterYear, r.SemesterPart, r.YearPart))
            .Where(y => y != null)
            .Distinct()
            .OrderByDescending(y => y)
            .Select(y => new FilterOptionDto { Value = y!, Label = y! })
            .ToList();

        return new ReportFilterOptionsDto
        {
            Departments = departments,
            AcademicYears = years,
            Statuses = DocumentStatusInfo.Options()
        };
    }

    public async Task<DocumentFileDto> OpenFileForDean(int reportId, CancellationToken cancellationToken)
    {
        var report = await GetDeanReport(reportId);
        return await Open(report, cancellationToken);
    }

    public Task<ReportDocumentDto> Approve(int reportId) =>
        Review(reportId, DocumentStatus.Signed, null);

    public Task<ReportDocumentDto> Reject(int reportId, string? comment) =>
        Review(reportId, DocumentStatus.Rejected, NormalizeComment(comment));

    private async Task<ReportDocumentDto> Review(int reportId, DocumentStatus newStatus, string? comment)
    {
        await GetDeanReport(reportId);

        var changed = await _repository.Review(reportId, newStatus, comment, DateTime.UtcNow, _currentUser.UserId);
        if (!changed)
            throw new DocumentConflictException("Отчёт уже рассмотрен");

        var updated = await _repository.GetById(reportId)
            ?? throw new DocumentNotFoundException("Отчёт не найден");
        return ToDto(updated);
    }

    private async Task<ReportDocumentModel> GetTeacherReport(int reportId)
    {
        var facultyId = RequireFaculty();
        var report = await _repository.GetById(reportId);

        if (report == null || report.FacultyId != facultyId)
            throw new DocumentNotFoundException("Отчёт не найден");

        if (_currentUser.UserId.HasValue && report.TeacherId != _currentUser.UserId)
            throw new DocumentNotFoundException("Отчёт не найден");

        return report;
    }

    private async Task<ReportDocumentModel> GetDeanReport(int reportId)
    {
        var facultyId = RequireFaculty();
        var report = await _repository.GetById(reportId);

        if (report == null || report.Status == null || report.FacultyId != facultyId)
            throw new DocumentNotFoundException("Отчёт не найден");

        return report;
    }

    private async Task<DocumentFileDto> Open(ReportDocumentModel report, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(report.FileKey))
            throw new DocumentNotFoundException("Файл отчёта ещё не загружен");

        var stream = await _storage.OpenReadAsync(report.FileKey, cancellationToken)
            ?? throw new StoredFileMissingException("Файл отчёта не найден в хранилище");

        return new DocumentFileDto
        {
            Content = stream,
            FileName = report.FileName ?? $"Отчёт_{report.ReportId}.pdf"
        };
    }

    private int RequireFaculty() =>
        _currentUser.FacultyId ?? throw new DocumentAccessException("Не определён факультет пользователя");

    internal static string? NormalizeComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment)) return null;

        var trimmed = comment.Trim();
        if (trimmed.Length > 1000)
            throw new DocumentValidationException("Комментарий длиннее 1000 символов");
        return trimmed;
    }

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

    private static ReportDocumentDto ToDto(ReportDocumentModel report)
    {
        var status = report.Status ?? DocumentStatus.Pending;
        return new ReportDocumentDto
        {
            Id = report.ReportId,
            AcademicYear = AcademicPeriod.FormatAcademicYear(report.SemesterYear, report.SemesterPart, report.YearPart),
            StudyProgramCode = report.StudyProgramCode,
            GroupNumber = report.GroupNumber,
            DisciplineName = report.DisciplineName,
            TeacherName = report.TeacherName,
            DepartmentId = report.DepartmentId,
            DepartmentName = report.DepartmentName,
            Status = DocumentStatusInfo.Code(status),
            StatusLabel = DocumentStatusInfo.Label(status),
            FileName = report.FileName,
            UploadedAt = DocumentStatusInfo.AsUtc(report.UploadedAt),
            ReviewComment = report.ReviewComment,
            ReviewedAt = DocumentStatusInfo.AsUtc(report.ReviewedAt)
        };
    }
}
