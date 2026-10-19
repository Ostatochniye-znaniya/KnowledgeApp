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
            join approval in approvals on testing.Id equals approval.TestingId
            where testing.SelectionPeriodStart.HasValue && testing.SelectionPeriodEnd.HasValue
            orderby testing.ScheduledDate, testing.ScheduledTime
            select new
            {
                testing.Id,
                testing.ReportId,
                GroupName = testing.Group!.GroupNumber,
                DisciplineName = testing.Discipline!.Name,
                testing.ScheduledDate,
                testing.ScheduledTime,
                testing.ScheduledPair,
                testing.SelectionPeriodStart,
                testing.SelectionPeriodEnd,
                testing.IsInPerson,
                testing.Room,
                testing.LmsUrl,
                ApprovalStatus = approval.Status,
                Comment = approval.Comment,
                RespondedAt = approval.RespondedAt
            })
            .ToListAsync(cancellationToken);

        return rows.Select(t => new TeacherTestingApprovalDto(
                t.Id,
                t.ReportId,
                t.GroupName,
                t.DisciplineName,
                t.ScheduledDate.HasValue ? DateOnly.FromDateTime(t.ScheduledDate.Value) : null,
                t.ScheduledTime.HasValue ? TimeOnly.FromTimeSpan(t.ScheduledTime.Value) : null,
                t.ScheduledPair,
                t.SelectionPeriodStart.HasValue ? DateOnly.FromDateTime(t.SelectionPeriodStart.Value) : null,
                t.SelectionPeriodEnd.HasValue ? DateOnly.FromDateTime(t.SelectionPeriodEnd.Value) : null,
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
                                       t.SelectionPeriodStart.HasValue &&
                                       t.SelectionPeriodEnd.HasValue &&
                                       _context.TestingTeacherApprovals.Any(a =>
                                           a.TestingId == t.Id && a.TeacherId == teacherId),
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

    public async Task<TeacherTestingApprovalDto> ChooseTestingTime(
        int testingId,
        DateOnly selectedDate,
        int pairNumber,
        CancellationToken cancellationToken)
    {
        var pair = TestingPairSchedule.All.SingleOrDefault(slot => slot.PairNumber == pairNumber);
        if (pair is null)
            throw new DocumentValidationException("Указан несуществующий номер пары.");

        var teacherId = RequireTeacher();
        var testing = await _context.Testings
            .SingleOrDefaultAsync(t => t.Id == testingId &&
                _context.TestingTeacherApprovals.Any(a =>
                    a.TestingId == t.Id && a.TeacherId == teacherId),
                cancellationToken);
        if (testing is null)
            throw new DocumentNotFoundException("Назначенное тестирование не найдено");

        var approval = await _context.TestingTeacherApprovals
            .AsNoTracking()
            .SingleAsync(a => a.TestingId == testingId && a.TeacherId == teacherId, cancellationToken);
        if (!string.Equals(approval.Status, PendingApproval, StringComparison.Ordinal))
            throw new DocumentConflictException("Ответ по этому тестированию уже отправлен.");
        if (!testing.SelectionPeriodStart.HasValue || !testing.SelectionPeriodEnd.HasValue)
            throw new DocumentValidationException("Для тестирования не задан период выбора времени.");
        if ((testing.ScheduledDate.HasValue || testing.ScheduledTime.HasValue) &&
            await _context.TestingTeacherApprovals.AnyAsync(
                a => a.TestingId == testingId && a.Status == "approved",
                cancellationToken))
        {
            throw new DocumentConflictException("Время уже выбрал преподаватель. Подтвердите его или отклоните.");
        }
        if (selectedDate < DateOnly.FromDateTime(testing.SelectionPeriodStart.Value) ||
            selectedDate > DateOnly.FromDateTime(testing.SelectionPeriodEnd.Value))
        {
            throw new DocumentValidationException("Выбранная дата должна входить в заданный период.");
        }

        var scheduledDate = selectedDate.ToDateTime(TimeOnly.MinValue);
        var scheduledTime = pair.StartsAt.ToTimeSpan();
        var currentScheduledDate = testing.ScheduledDate;
        var currentScheduledTime = testing.ScheduledTime;
        await using var transaction = await _context.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        await EnsureSlotIsAvailable(testing, selectedDate, pair.StartsAt, cancellationToken);

        var changed = await _context.Testings
            .Where(t => t.Id == testingId &&
                        t.ScheduledDate == currentScheduledDate &&
                        t.ScheduledTime == currentScheduledTime &&
                        !_context.TestingTeacherApprovals.Any(a =>
                            a.TestingId == t.Id && a.Status == "approved") &&
                        t.SelectionPeriodStart <= scheduledDate &&
                        t.SelectionPeriodEnd >= scheduledDate)
            .ExecuteUpdateAsync(update => update
                .SetProperty(t => t.ScheduledDate, scheduledDate)
                .SetProperty(t => t.ScheduledTime, scheduledTime)
                .SetProperty(t => t.ScheduledPair, pairNumber), cancellationToken);
        if (changed == 0)
            throw new DocumentConflictException("Время уже выбрал другой преподаватель.");

        var respondedAt = DateTime.UtcNow;
        var approvalUpdated = await _context.TestingTeacherApprovals
            .Where(a => a.TestingId == testingId &&
                        a.TeacherId == teacherId &&
                        a.Status == PendingApproval)
            .ExecuteUpdateAsync(update => update
                .SetProperty(a => a.Status, "approved")
                .SetProperty(a => a.Comment, (string?)null)
                .SetProperty(a => a.RespondedAt, respondedAt), cancellationToken);
        if (approvalUpdated == 0)
            throw new DocumentConflictException("Ответ по этому тестированию уже отправлен.");

        var otherTeacherIds = await _context.TestingTeacherApprovals
            .Where(a => a.TestingId == testingId &&
                        a.TeacherId != teacherId &&
                        a.Status == PendingApproval)
            .Select(a => a.TeacherId)
            .ToListAsync(cancellationToken);
        foreach (var otherTeacherId in otherTeacherIds)
        {
            _context.WorkflowNotifications.Add(new WorkflowNotification
            {
                RecipientUserId = otherTeacherId,
                TestingId = testingId,
                Type = "testing_time_proposed",
                Title = "Преподаватель выбрал время проверки",
                Message = $"Выбрана {pairNumber}-я пара ({pair.StartsAt:HH:mm}–{pair.EndsAt:HH:mm}) на {selectedDate:dd.MM.yyyy}. Подтвердите или отклоните время.",
                CreatedAt = respondedAt
            });
        }
        var departmentId = await _context.Disciplines
            .Where(d => d.Id == testing.DisciplineId)
            .Select(d => d.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);
        if (departmentId.HasValue)
        {
            _context.WorkflowNotifications.Add(new WorkflowNotification
            {
                RecipientDepartmentId = departmentId.Value,
                TestingId = testingId,
                Type = "testing_time_selected",
                Title = "Выбрано время проверки",
                Message = $"Для тестирования №{testingId} выбрана {pairNumber}-я пара ({pair.StartsAt:HH:mm}–{pair.EndsAt:HH:mm}) на {selectedDate:dd.MM.yyyy}.",
                CreatedAt = respondedAt
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return (await GetTestings(cancellationToken))
            .Single(t => t.TestingId == testingId);
    }

    public async Task<IReadOnlyList<WorkflowNotificationDto>> GetNotifications(
        bool unreadOnly,
        CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var notifications = _context.WorkflowNotifications
            .AsNoTracking()
            .Where(n => n.RecipientUserId == teacherId);
        if (unreadOnly)
            notifications = notifications.Where(n => n.ReadAt == null);

        return await notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .Select(n => new WorkflowNotificationDto(
                n.Id,
                n.TestingId,
                n.Type,
                n.Title,
                n.Message,
                new DateTimeOffset(DateTime.SpecifyKind(n.CreatedAt, DateTimeKind.Utc)),
                n.ReadAt.HasValue
                    ? new DateTimeOffset(DateTime.SpecifyKind(n.ReadAt.Value, DateTimeKind.Utc))
                    : null))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkNotificationRead(int notificationId, CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var notification = await _context.WorkflowNotifications
            .SingleOrDefaultAsync(n => n.Id == notificationId && n.RecipientUserId == teacherId,
                cancellationToken);
        if (notification is null)
            throw new DocumentNotFoundException("Уведомление не найдено.");
        notification.ReadAt ??= DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSlotIsAvailable(
        Testing currentTesting,
        DateOnly selectedDate,
        TimeOnly selectedStart,
        CancellationToken cancellationToken)
    {
        var assignedTeacherIds = await _context.TestingTeacherApprovals
            .Where(a => a.TestingId == currentTesting.Id)
            .Select(a => a.TeacherId)
            .ToListAsync(cancellationToken);
        if (assignedTeacherIds.Count == 0)
            throw new DocumentAccessException("На тестирование не назначены преподаватели.");

        var dayStart = selectedDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var otherTestings = await _context.Testings
            .Where(t => t.Id != currentTesting.Id &&
                        t.GroupId == currentTesting.GroupId &&
                        t.ScheduledDate >= dayStart &&
                        t.ScheduledDate < dayEnd &&
                        t.ScheduledTime.HasValue)
            .Select(t => new { t.Id, t.ScheduledTime })
            .ToListAsync(cancellationToken);
        if (otherTestings.Any(t =>
                TestingPairSchedule.Overlaps(selectedStart, TimeOnly.FromTimeSpan(t.ScheduledTime!.Value))))
        {
            throw new DocumentConflictException("У группы уже есть проверка в это время.");
        }

        var scheduledTestingIds = await _context.Testings
            .Where(t => t.Id != currentTesting.Id &&
                        t.ScheduledDate >= dayStart &&
                        t.ScheduledDate < dayEnd &&
                        t.ScheduledTime.HasValue)
            .Select(t => new { t.Id, t.ScheduledTime })
            .ToListAsync(cancellationToken);
        var overlappingTestingIds = scheduledTestingIds
            .Where(t => TestingPairSchedule.Overlaps(
                selectedStart, TimeOnly.FromTimeSpan(t.ScheduledTime!.Value)))
            .Select(t => t.Id)
            .ToList();
        if (overlappingTestingIds.Count == 0)
            return;

        var hasTeacherConflict = await _context.TestingTeacherApprovals
            .AnyAsync(a => overlappingTestingIds.Contains(a.TestingId) &&
                           assignedTeacherIds.Contains(a.TeacherId), cancellationToken);
        if (hasTeacherConflict)
            throw new DocumentConflictException("У одного из назначенных преподавателей уже есть проверка в это время.");
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

    public byte[] GenerateBlankReportTemplate() =>
        _groupReportService.GenerateBlankReportTemplate();

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

        return await _reportDocumentService.UploadDocx(reportId, content, fileName, size, cancellationToken);
    }

    private async Task<TeacherTestingApprovalDto> ReviewTesting(
        int testingId,
        string status,
        string? comment,
        CancellationToken cancellationToken)
    {
        var teacherId = RequireTeacher();
        var assigned = await _context.TestingTeacherApprovals
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.TestingId == testingId && a.TeacherId == teacherId,
                cancellationToken);
        if (assigned is null)
        {
            var exists = await _context.Testings.AnyAsync(t => t.Id == testingId, cancellationToken);
            if (!exists)
                throw new DocumentNotFoundException("Тестирование не найдено");
            throw new DocumentNotFoundException("Назначенное тестирование не найдено");
        }

        var testing = await _context.Testings
            .AsNoTracking()
            .SingleAsync(t => t.Id == testingId, cancellationToken);
        if (!testing.SelectionPeriodStart.HasValue || !testing.SelectionPeriodEnd.HasValue)
            throw new DocumentNotFoundException("Назначенное тестирование не найдено");
        if (status == "approved" && (!testing.ScheduledDate.HasValue || !testing.ScheduledTime.HasValue))
            throw new DocumentValidationException("Сначала выберите дату и время тестирования.");
        if (!string.Equals(assigned.Status, PendingApproval, StringComparison.Ordinal))
        {
            throw new DocumentConflictException("Ответ по этому тестированию уже отправлен.");
        }

        var respondedAt = DateTime.UtcNow;
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var updated = await _context.TestingTeacherApprovals
            .Where(a => a.TestingId == testingId &&
                        a.TeacherId == teacherId &&
                        a.Status == PendingApproval)
            .ExecuteUpdateAsync(update => update
                .SetProperty(a => a.Status, status)
                .SetProperty(a => a.Comment, comment)
                .SetProperty(a => a.RespondedAt, respondedAt), cancellationToken);
        if (updated == 0)
            throw new DocumentConflictException("Ответ по этому тестированию уже отправлен.");

        var departmentId = await _context.Disciplines
            .Where(d => d.Id == testing.DisciplineId)
            .Select(d => d.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);
        if (departmentId.HasValue)
        {
            _context.WorkflowNotifications.Add(new WorkflowNotification
            {
                RecipientDepartmentId = departmentId.Value,
                TestingId = testingId,
                Type = status == "rejected" ? "testing_rejected" : "testing_approved",
                Title = status == "rejected" ? "Преподаватель отклонил время" : "Преподаватель подтвердил время",
                Message = status == "rejected"
                    ? $"Преподаватель отклонил время тестирования №{testingId}. Причина: {comment}"
                    : $"Преподаватель подтвердил время тестирования №{testingId}.",
                CreatedAt = respondedAt
            });
        }
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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
