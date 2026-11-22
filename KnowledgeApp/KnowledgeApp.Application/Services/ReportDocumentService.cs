using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Repositories;
using KnowledgeApp.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace KnowledgeApp.Application.Services;

public class ReportDocumentService
{
    private readonly ReportDocumentRepository _repository;
    private readonly DepartmentRepository _departmentRepository;
    private readonly FacultyRepository _facultyRepository;
    private readonly IFileStorage _storage;
    private readonly ICurrentUserProvider _currentUser;
    private readonly ILogger<ReportDocumentService> _logger;

    public ReportDocumentService(ReportDocumentRepository repository, DepartmentRepository departmentRepository,
        IFileStorage storage, ICurrentUserProvider currentUser, ILogger<ReportDocumentService> logger,
        FacultyRepository facultyRepository)
    {
        _repository = repository;
        _departmentRepository = departmentRepository;
        _facultyRepository = facultyRepository;
        _storage = storage;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ReportDocumentDto> UploadFile(int reportId, Stream content, string? fileName, long? declaredSize,
        CancellationToken cancellationToken)
    {
        PdfUpload.EnsureDeclaredSize(declaredSize);

        var report = await GetTeacherReport(reportId, cancellationToken);
        if (report.Status == DocumentStatus.Signed)
            throw new DocumentConflictException("Электронный отчёт уже принят, заменить файл нельзя");

        using var upload = await PdfUpload.ReadAsync(content, fileName, declaredSize, cancellationToken);
        return await StoreUpload(
            reportId, upload.Content, upload.FileName, ".pdf", "application/pdf", cancellationToken);
    }

    public async Task<ReportDocumentDto> UploadDocx(
        int reportId,
        Stream content,
        string? fileName,
        long? declaredSize,
        CancellationToken cancellationToken)
    {
        DocxUpload.EnsureDeclaredSize(declaredSize);

        var report = await GetTeacherReport(reportId, cancellationToken);
        if (report.Status == DocumentStatus.Signed)
            throw new DocumentConflictException("Электронный отчёт уже принят, заменить файл нельзя");

        using var upload = await DocxUpload.ReadAsync(content, fileName, declaredSize, cancellationToken);
        return await StoreUpload(
            reportId,
            upload.Content,
            upload.FileName,
            ".docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            cancellationToken);
    }

    private async Task<ReportDocumentDto> StoreUpload(
        int reportId,
        Stream content,
        string fileName,
        string extension,
        string contentType,
        CancellationToken cancellationToken)
    {
        var key = $"reports/{reportId}/{Guid.NewGuid():N}{extension}";
        await _storage.SaveAsync(key, content, contentType, cancellationToken);

        (bool Replaced, string? PreviousKey) result;
        try
        {
            result = await _repository.AttachFile(reportId, key, fileName, DateTime.UtcNow);
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
        var report = await GetTeacherReport(reportId, cancellationToken);
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

    public async Task<List<ReportDocumentDto>> GetForLpr(int? facultyId, int? departmentId,
        string? academicYear, string? status, CancellationToken cancellationToken)
    {
        ValidateFilterIds(facultyId, departmentId);
        var statusFilter = DocumentStatusInfo.Parse(status);
        var reports = await _repository.GetSubmitted(facultyId, departmentId, statusFilter, cancellationToken);
        return reports.Select(ToDto)
            .Where(r => string.IsNullOrWhiteSpace(academicYear) || r.AcademicYear == academicYear.Trim())
            .OrderBy(r => r.Status == "pending" ? 0 : 1)
            .ThenByDescending(r => r.UploadedAt).ThenByDescending(r => r.Id).ToList();
    }

    public async Task<LprReportFilterOptionsDto> GetLprFilterOptions(int? facultyId, CancellationToken cancellationToken)
    {
        ValidateFilterIds(facultyId, null);
        var faculties = await _facultyRepository.GetAllFaculties();
        var departments = await _departmentRepository.GetAllDepartments();
        var reports = await _repository.GetSubmitted(facultyId, null, null, cancellationToken);
        return new LprReportFilterOptionsDto
        {
            Faculties = faculties.OrderBy(f => f.FacultyName)
                .Select(f => new FilterOptionDto { Value = f.Id.ToString(), Label = f.FacultyName }).ToList(),
            Departments = departments.Where(d => facultyId == null || d.FacultyId == facultyId)
                .OrderBy(d => d.Name)
                .Select(d => new FilterOptionDto { Value = d.Id.ToString(), Label = d.Name }).ToList(),
            AcademicYears = reports.Select(r => AcademicPeriod.FormatAcademicYear(r.SemesterYear, r.SemesterPart, r.YearPart))
                .Where(y => y != null).Distinct().OrderByDescending(y => y)
                .Select(y => new FilterOptionDto { Value = y!, Label = y! }).ToList(),
            Statuses = DocumentStatusInfo.Options(reportWorkflow: true)
        };
    }

    public async Task<DocumentFileDto> OpenFileForLpr(int reportId, string? fileVersion, CancellationToken cancellationToken)
    {
        var report = await GetLprReport(reportId, fileVersion, cancellationToken);
        return await Open(report, cancellationToken);
    }

    public Task<ReportDocumentDto> ApproveForLpr(int reportId, string? fileVersion, CancellationToken cancellationToken) =>
        ReviewForLpr(reportId, DocumentStatus.Signed, "Нет", fileVersion, cancellationToken);

    public Task<ReportDocumentDto> RejectForLpr(int reportId, string? comment, string? fileVersion,
        CancellationToken cancellationToken)
    {
        var normalized = NormalizeComment(comment);
        if (normalized is null)
            throw new DocumentValidationException("Опишите замечания проверки электронного отчёта.");
        return ReviewForLpr(reportId, DocumentStatus.Rejected, normalized, fileVersion, cancellationToken);
    }

    private async Task<ReportDocumentDto> ReviewForLpr(int reportId, DocumentStatus status, string comment,
        string? fileVersion, CancellationToken cancellationToken)
    {
        var report = await GetLprReport(reportId, fileVersion, cancellationToken);
        var changed = await _repository.Review(reportId, status, comment, DateTime.UtcNow,
            _currentUser.UserId, report.FileKey, cancellationToken);
        if (!changed)
            throw new DocumentConflictException("Отчёт уже рассмотрен или преподаватель заменил файл. Обновите список.");
        var updated = await _repository.GetById(reportId, cancellationToken)
            ?? throw new DocumentNotFoundException("Отчёт не найден");
        return ToDto(updated);
    }

    private async Task<ReportDocumentModel> GetLprReport(int reportId, string? fileVersion, CancellationToken cancellationToken)
    {
        if (reportId <= 0 || string.IsNullOrWhiteSpace(fileVersion))
            throw new DocumentValidationException("Выберите отчёт из актуального списка.");
        var report = await _repository.GetById(reportId, cancellationToken);
        if (report is null || string.IsNullOrWhiteSpace(report.FileKey))
            throw new DocumentNotFoundException("Загруженный отчёт не найден");
        if (!string.Equals(FileVersion(report.FileKey), fileVersion, StringComparison.Ordinal))
            throw new DocumentConflictException("Преподаватель заменил файл отчёта. Обновите список перед проверкой.");
        return report;
    }

    private static void ValidateFilterIds(int? facultyId, int? departmentId)
    {
        if (facultyId is <= 0 || departmentId is <= 0)
            throw new DocumentValidationException("Некорректный факультет или кафедра.");
    }

    private static string FileVersion(string? key) => string.IsNullOrEmpty(key)
        ? string.Empty : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));

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
            Statuses = DocumentStatusInfo.Options(reportWorkflow: true)
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

    public async Task<ReportDocumentDto> SetPaperDelivery(
        int reportId,
        bool submitted,
        CancellationToken cancellationToken)
    {
        var report = await GetDeanReport(reportId);
        if (report.Status != DocumentStatus.Signed)
            throw new DocumentConflictException(
                "Статус бумажного отчёта можно менять только после принятия электронного отчёта.");
        if (!await _repository.SetPaperDelivery(reportId, submitted, cancellationToken))
            throw new DocumentConflictException(
                "Электронный отчёт изменился. Обновите страницу и повторите действие.");

        var updated = await _repository.GetById(reportId)
            ?? throw new DocumentNotFoundException("Отчёт не найден");
        return ToDto(updated);
    }

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

    private async Task<ReportDocumentModel> GetTeacherReport(int reportId, CancellationToken cancellationToken)
    {
        var facultyId = RequireFaculty();
        var report = await _repository.GetById(reportId);

        if (report == null || report.FacultyId != facultyId)
            throw new DocumentNotFoundException("Отчёт не найден");

        if (_currentUser.UserId is int teacherId &&
            report.TeacherId != teacherId &&
            !await _repository.IsAssignedToTeacher(reportId, teacherId, cancellationToken))
            throw new DocumentNotFoundException("Отчёт не найден");

        return report;
    }

    private async Task<ReportDocumentModel> GetDeanReport(int reportId)
    {
        var facultyId = RequireFaculty();
        var report = await _repository.GetById(reportId);

        if (report == null || report.FileKey == null || report.FacultyId != facultyId)
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
            FileName = report.FileName ?? $"Отчёт_{report.ReportId}{Path.GetExtension(report.FileKey)}",
            ContentType = Path.GetExtension(report.FileName ?? report.FileKey).ToLowerInvariant() switch
            {
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            }
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
            StatusLabel = DocumentStatusInfo.Label(status, reportWorkflow: true),
            FileName = report.FileName,
            FileVersion = FileVersion(report.FileKey),
            UploadedAt = DocumentStatusInfo.AsUtc(report.UploadedAt),
            ReviewComment = report.ReviewComment,
            ReviewedAt = DocumentStatusInfo.AsUtc(report.ReviewedAt),
            PaperStatus = DocumentStatusInfo.PaperCode(status, report.DoneInPaperForm),
            PaperStatusLabel = DocumentStatusInfo.PaperLabel(status, report.DoneInPaperForm),
            DoneInPaperForm = report.DoneInPaperForm,
            DoneInElectronicForm = report.DoneInElectronicForm,
            AllDone = report.AllDone
        };
    }
}
