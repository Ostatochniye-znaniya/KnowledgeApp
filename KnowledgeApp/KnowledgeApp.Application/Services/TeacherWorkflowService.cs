using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Enums;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed class TeacherWorkflowService
{
    private const string PendingApproval = "pending";
    private readonly KnowledgeTestDbContext _context;
    private readonly ICurrentUserProvider _currentUser;
    private readonly GroupReportService _groupReportService;
    private readonly ReportDocumentService _reportDocumentService;

    public TeacherWorkflowService(
        KnowledgeTestDbContext context,
        ICurrentUserProvider currentUser,
        GroupReportService groupReportService,
        ReportDocumentService reportDocumentService)
    {
        _context = context;
        _currentUser = currentUser;
        _groupReportService = groupReportService;
        _reportDocumentService = reportDocumentService;
    }

    public async Task<IReadOnlyList<TeacherTestingApprovalDto>> GetTestings(CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var approvals = _context.TestingTeacherApprovals
            .AsNoTracking()
            .Where(a => a.TeacherId == teacherId);
        var rows = await (
            from testing in _context.Testings.AsNoTracking()
            join approval in approvals on testing.Id equals approval.TestingId into approvalJoin
            from approval in approvalJoin.DefaultIfEmpty()
            where testing.DisciplineId != null &&
                  _context.DisciplineTeachers.Any(dt =>
                      dt.DisciplineId == testing.DisciplineId &&
                      dt.ResponsibleTeacherId == teacherId)
            orderby testing.ScheduledDate, testing.ScheduledTime
            select new
            {
                testing.Id,
                testing.ReportId,
                GroupName = testing.Group!.GroupNumber,
                DisciplineName = testing.Discipline!.Name,
                testing.ScheduledDate,
                testing.ScheduledTime,
                testing.IsInPerson,
                testing.Room,
                testing.LmsUrl,
                ApprovalStatus = approval == null ? null : approval.Status,
                Comment = approval == null ? null : approval.Comment,
                RespondedAt = approval == null ? null : approval.RespondedAt
            })
            .ToListAsync(cancellationToken);

        return rows.Select(t => new TeacherTestingApprovalDto(
                t.Id,
                t.ReportId,
                t.GroupName,
                t.DisciplineName,
                t.ScheduledDate.HasValue ? DateOnly.FromDateTime(t.ScheduledDate.Value) : null,
                t.ScheduledTime.HasValue ? TimeOnly.FromTimeSpan(t.ScheduledTime.Value) : null,
                t.IsInPerson,
                t.Room,
                t.LmsUrl,
                t.ApprovalStatus ?? PendingApproval,
                t.Comment,
                t.RespondedAt.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(t.RespondedAt.Value, DateTimeKind.Utc))
                    : null))
            .ToList();
    }

    public async Task<TeacherTestingApprovalDto> UpdateTestingDelivery(
        int testingId,
        bool isInPerson,
        string? room,
        string? lmsUrl,
        CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var normalizedRoom = string.IsNullOrWhiteSpace(room) ? null : room.Trim();
        var normalizedLmsUrl = string.IsNullOrWhiteSpace(lmsUrl) ? null : lmsUrl.Trim();

        if (normalizedRoom?.Length > 255)
            throw new DocumentValidationException("Аудитория длиннее 255 символов.");
        if (normalizedLmsUrl?.Length > 2048)
            throw new DocumentValidationException("Ссылка LMS длиннее 2048 символов.");
        if (normalizedLmsUrl is not null &&
            (!Uri.TryCreate(normalizedLmsUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)))
        {
            throw new DocumentValidationException("Укажите корректную ссылку LMS с протоколом http или https.");
        }

        var testing = await _context.Testings
            .SingleOrDefaultAsync(t => t.Id == testingId &&
                                       t.DisciplineId != null &&
                                       _context.DisciplineTeachers.Any(dt =>
                                           dt.DisciplineId == t.DisciplineId &&
                                           dt.ResponsibleTeacherId == teacherId),
                cancellationToken);
        if (testing is null)
            throw new DocumentNotFoundException("Тестирование не найдено");

        testing.IsInPerson = isInPerson;
        testing.Room = normalizedRoom;
        testing.LmsUrl = normalizedLmsUrl;
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetTestings(cancellationToken))
            .Single(t => t.TestingId == testingId);
    }

    public async Task<TeacherTestingApprovalDto> ApproveTesting(int testingId, CancellationToken cancellationToken) =>
        await ReviewTesting(testingId, "approved", null, cancellationToken);

    public async Task<TeacherTestingApprovalDto> RejectTesting(
        int testingId,
        string? comment,
        CancellationToken cancellationToken)
    {
        var normalizedComment = comment?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedComment))
            throw new DocumentValidationException("Укажите причину отказа от назначенного времени.");
        if (normalizedComment.Length > 1000)
            throw new DocumentValidationException("Комментарий длиннее 1000 символов.");

        return await ReviewTesting(testingId, "rejected", normalizedComment, cancellationToken);
    }

    public async Task<IReadOnlyList<TeacherReportTaskDto>> GetReports(CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var rows = await (
            from testing in _context.Testings.AsNoTracking()
            join report in _context.Reports.AsNoTracking()
                on testing.ReportId equals (int?)report.Id into reportJoin
            from report in reportJoin.DefaultIfEmpty()
            where testing.DisciplineId != null &&
                  _context.DisciplineTeachers.Any(dt =>
                      dt.DisciplineId == testing.DisciplineId &&
                      dt.ResponsibleTeacherId == teacherId)
            select new
            {
                testing.ReportId,
                testing.GroupId,
                GroupName = testing.Group!.GroupNumber,
                SemesterId = testing.SemesterId ?? 0,
                DisciplineId = testing.DisciplineId!.Value,
                DisciplineName = testing.Discipline!.Name,
                FileName = report != null ? report.FileName : null,
                FilePath = report != null ? report.FilePath : null,
                Status = report != null ? report.Status : null,
                ReviewComment = report != null ? report.ReviewComment : null,
                UploadedAt = report != null ? report.UploadedAt : null,
                DoneInPaperForm = report != null ? report.DoneInPaperForm : null,
                DoneInElectronicForm = report != null ? report.DoneInElectronicForm : null,
                AllDone = report != null ? report.AllDone : null
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(t => new { t.ReportId, t.GroupId, t.SemesterId, t.DisciplineId })
            .Select(group =>
            {
                var task = group.First();
                var hasUploadedFile = !string.IsNullOrWhiteSpace(task.FilePath) &&
                                      !string.IsNullOrWhiteSpace(task.FileName) &&
                                      task.UploadedAt.HasValue;
                var status = !hasUploadedFile
                    ? "not_submitted"
                    : DocumentStatusInfo.Code(task.Status ?? DocumentStatus.Pending);
                var label = !hasUploadedFile
                    ? "Не загружен"
                    : DocumentStatusInfo.Label(task.Status ?? DocumentStatus.Pending, reportWorkflow: true);
                return new TeacherReportTaskDto(
                    task.ReportId,
                    task.GroupId,
                    task.SemesterId,
                    task.DisciplineId,
                    task.GroupName,
                    task.DisciplineName,
                    task.FileName,
                    status,
                    label,
                    task.ReviewComment,
                    task.UploadedAt.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(task.UploadedAt.Value, DateTimeKind.Utc))
                        : null,
                    task.DoneInPaperForm,
                    task.DoneInElectronicForm,
                    task.AllDone);
            })
            .OrderBy(t => t.GroupName)
            .ThenBy(t => t.DisciplineName)
            .ToList();
    }

    public async Task<byte[]> GenerateReport(
        int groupId,
        int semesterId,
        int disciplineId,
        CancellationToken cancellationToken)
    {
        await EnsureAssigned(groupId, semesterId, disciplineId, cancellationToken);
        return await _groupReportService.GenerateTemplate(groupId, semesterId, disciplineId, cancellationToken);
    }

    public async Task<ReportDocumentDto> UploadReport(
        int reportId,
        Stream content,
        string? fileName,
        long? size,
        CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var ownsReport = await _context.Testings
            .AnyAsync(t => t.ReportId == reportId && t.DisciplineId != null &&
                           _context.DisciplineTeachers.Any(dt =>
                               dt.DisciplineId == t.DisciplineId &&
                               dt.ResponsibleTeacherId == teacherId), cancellationToken);
        if (!ownsReport)
            throw new DocumentNotFoundException("Отчёт не найден");

        return await _reportDocumentService.UploadFile(reportId, content, fileName, size, cancellationToken);
    }

    private async Task<TeacherTestingApprovalDto> ReviewTesting(
        int testingId,
        string status,
        string? comment,
        CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var assigned = await _context.Testings
            .AnyAsync(t => t.Id == testingId &&
                           t.ScheduledDate != null &&
                           t.ScheduledTime != null &&
                           t.DisciplineId != null &&
                           _context.DisciplineTeachers.Any(dt =>
                               dt.DisciplineId == t.DisciplineId &&
                               dt.ResponsibleTeacherId == teacherId),
                cancellationToken);
        if (!assigned)
        {
            var exists = await _context.Testings.AnyAsync(t => t.Id == testingId, cancellationToken);
            if (!exists)
                throw new DocumentNotFoundException("Тестирование не найдено");
            throw new DocumentValidationException("Согласовать можно только тестирование с назначенными датой и временем.");
        }

        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO testing_teacher_approval
                 (testing_id, teacher_id, status, comment, responded_at)
             VALUES ({testingId}, {teacherId}, {status}, {comment}, {DateTime.UtcNow})
             ON DUPLICATE KEY UPDATE
                 status = IF(status = 'pending', VALUES(status), status),
                 comment = IF(status = 'pending', VALUES(comment), comment),
                 responded_at = IF(status = 'pending', VALUES(responded_at), responded_at)
             """,
            cancellationToken);

        var approval = await _context.TestingTeacherApprovals
            .AsNoTracking()
            .SingleAsync(a => a.TestingId == testingId && a.TeacherId == teacherId, cancellationToken);
        if (!string.Equals(approval.Status, status, StringComparison.Ordinal))
        {
            throw new DocumentConflictException("Решение по тестированию уже отправлено.");
        }

        return (await GetTestings(cancellationToken))
            .Single(t => t.TestingId == testingId);
    }

    private async Task EnsureAssigned(
        int groupId,
        int semesterId,
        int disciplineId,
        CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var assigned = await _context.Testings
            .AnyAsync(t => t.GroupId == groupId &&
                           t.SemesterId == semesterId &&
                           t.DisciplineId == disciplineId &&
                           _context.DisciplineTeachers.Any(dt =>
                               dt.DisciplineId == disciplineId &&
                               dt.ResponsibleTeacherId == teacherId),
                cancellationToken);
        if (!assigned)
            throw new DocumentNotFoundException("Отчёт не найден");
    }

    private int RequireTeacher() =>
        _currentUser.UserId ?? throw new DocumentAccessException("Не определён текущий преподаватель.");
}
