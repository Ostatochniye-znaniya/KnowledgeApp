using KnowledgeApp.Application.DTOs;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed class TestingAssignmentService
{
    private readonly KnowledgeTestDbContext _context;
    private readonly ICurrentUserProvider _currentUser;

    public TestingAssignmentService(KnowledgeTestDbContext context, ICurrentUserProvider currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<TestingTeacherAssignmentDetailsDto> GetAssignments(
        int testingId,
        CancellationToken cancellationToken)
    {
        var testing = await GetTestingInDepartment(testingId, cancellationToken);
        var approvals = await (
            from approval in _context.TestingTeacherApprovals.AsNoTracking()
            join teacher in _context.Users.AsNoTracking() on approval.TeacherId equals teacher.Id
            where approval.TestingId == testingId
            orderby teacher.Name
            select new
            {
                approval.TeacherId,
                teacher.Name,
                approval.Status,
                approval.Comment,
                approval.RespondedAt
            })
            .ToListAsync(cancellationToken);

        return new TestingTeacherAssignmentDetailsDto(
            testing.Id,
            testing.Group?.GroupNumber ?? string.Empty,
            testing.Discipline?.Name ?? string.Empty,
            testing.SelectionPeriodStart.HasValue
                ? DateOnly.FromDateTime(testing.SelectionPeriodStart.Value)
                : null,
            testing.SelectionPeriodEnd.HasValue
                ? DateOnly.FromDateTime(testing.SelectionPeriodEnd.Value)
                : null,
            testing.ScheduledDate.HasValue ? DateOnly.FromDateTime(testing.ScheduledDate.Value) : null,
            testing.ScheduledTime.HasValue ? TimeOnly.FromTimeSpan(testing.ScheduledTime.Value) : null,
            testing.ScheduledPair,
            approvals.Select(a => new TestingTeacherAssignmentDto(
                    a.TeacherId,
                    a.Name,
                    a.Status ?? "pending",
                    a.Comment,
                    a.RespondedAt.HasValue
                        ? new DateTimeOffset(DateTime.SpecifyKind(a.RespondedAt.Value, DateTimeKind.Utc))
                        : null))
                .ToList());
    }

    public async Task<IReadOnlyList<AvailableTestingTeacherDto>> GetAvailableTeachers(
        int testingId,
        CancellationToken cancellationToken)
    {
        var testing = await GetTestingInDepartment(testingId, cancellationToken);
        if (!testing.DisciplineId.HasValue)
            throw new DocumentValidationException("Для тестирования не задана дисциплина.");

        return await (
            from relation in _context.DisciplineTeachers.AsNoTracking()
            join teacher in _context.Users.AsNoTracking()
                on relation.ResponsibleTeacherId equals (int?)teacher.Id
            where relation.DisciplineId == testing.DisciplineId
            orderby teacher.Name
            select new AvailableTestingTeacherDto(teacher.Id, teacher.Name))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<TestingTeacherAssignmentDetailsDto> UpdateAssignments(
        int testingId,
        DateOnly periodStart,
        DateOnly periodEnd,
        IReadOnlyList<int> teacherIds,
        CancellationToken cancellationToken)
    {
        if (periodEnd < periodStart)
            throw new DocumentValidationException("Конец периода не может быть раньше его начала.");
        if (teacherIds.Count == 0)
            throw new DocumentValidationException("Назначьте хотя бы одного преподавателя.");
        if (teacherIds.Any(id => id <= 0) || teacherIds.Distinct().Count() != teacherIds.Count)
            throw new DocumentValidationException("Список преподавателей содержит некорректные или повторяющиеся ID.");

        var testing = await GetTestingInDepartment(testingId, cancellationToken);
        if (!testing.DisciplineId.HasValue)
            throw new DocumentValidationException("Для тестирования не задана дисциплина.");

        var eligibleTeacherIds = await _context.DisciplineTeachers
            .Where(dt => dt.DisciplineId == testing.DisciplineId &&
                         dt.ResponsibleTeacherId.HasValue &&
                         teacherIds.Contains(dt.ResponsibleTeacherId.Value))
            .Select(dt => dt.ResponsibleTeacherId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (eligibleTeacherIds.Count != teacherIds.Count)
        {
            throw new DocumentValidationException(
                "Все назначаемые преподаватели должны быть привязаны к дисциплине этого тестирования.");
        }

        var startDate = periodStart.ToDateTime(TimeOnly.MinValue);
        var endDate = periodEnd.ToDateTime(TimeOnly.MinValue);
        var periodChanged = testing.SelectionPeriodStart != startDate ||
                            testing.SelectionPeriodEnd != endDate;
        var existing = await _context.TestingTeacherApprovals
            .Where(a => a.TestingId == testingId)
            .ToListAsync(cancellationToken);
        var existingTeacherIds = existing.Select(a => a.TeacherId).ToHashSet();
        var assignmentsChanged = !existingTeacherIds.SetEquals(teacherIds);

        if (periodChanged || assignmentsChanged)
        {
            await _context.InTransactionAsync(async () =>
            {
                var requestedTeacherIds = teacherIds.ToHashSet();
                var removed = existing.Where(a => !requestedTeacherIds.Contains(a.TeacherId)).ToList();
                _context.TestingTeacherApprovals.RemoveRange(removed);

                if (periodChanged)
                {
                    testing.ScheduledDate = null;
                    testing.ScheduledTime = null;
                    testing.ScheduledPair = null;
                    foreach (var assignment in existing.Except(removed))
                    {
                        assignment.Status = "pending";
                        assignment.Comment = null;
                        assignment.RespondedAt = null;
                    }
                }

                foreach (var teacherId in teacherIds.Where(id => !existingTeacherIds.Contains(id)))
                {
                    _context.TestingTeacherApprovals.Add(new TestingTeacherApproval
                    {
                        TestingId = testingId,
                        TeacherId = teacherId,
                        Status = "pending"
                    });
                }

                testing.SelectionPeriodStart = startDate;
                testing.SelectionPeriodEnd = endDate;
                var notificationTeacherIds = periodChanged
                    ? teacherIds
                    : teacherIds.Where(id => !existingTeacherIds.Contains(id));
                foreach (var teacherId in notificationTeacherIds)
                {
                    _context.WorkflowNotifications.Add(new WorkflowNotification
                    {
                        RecipientUserId = teacherId,
                        TestingId = testingId,
                        Type = periodChanged ? "testing_period_updated" : "testing_assigned",
                        Title = periodChanged ? "Изменился период проверки" : "Назначена проверка",
                        Message = periodChanged
                            ? $"Период выбора времени: {periodStart:dd.MM.yyyy}–{periodEnd:dd.MM.yyyy}."
                            : $"Вам назначена проверка. Выберите пару с {periodStart:dd.MM.yyyy} по {periodEnd:dd.MM.yyyy}.",
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _context.SaveChangesAsync(cancellationToken);
            }, cancellationToken);
        }

        return await GetAssignments(testingId, cancellationToken);
    }

    public IReadOnlyList<TestingPairSlotDto> GetPairSlots() => TestingPairSchedule.All;

    /// <summary>
    /// Проверки для страницы «Выбор преподавателей» в последнем семестре:
    /// группы своей кафедры с выбранными дисциплинами (own — назначаем сами, outgoing — нужна заявка)
    /// и заявки от кафедр других факультетов по дисциплинам своей кафедры (incoming).
    /// </summary>
    public async Task<IReadOnlyList<DepartmentTestingDto>> GetDepartmentTestings(CancellationToken cancellationToken)
    {
        var departmentId = RequireDepartment();
        var semesterId = await _context.Semesters
            .OrderByDescending(s => s.SemesterYear)
            .ThenByDescending(s => s.SemesterPart)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (semesterId is null)
            return Array.Empty<DepartmentTestingDto>();

        var rows = await _context.Testings
            .AsNoTracking()
            .Where(t => t.SemesterId == semesterId && t.DisciplineId != null)
            .Where(t => t.Group!.StudyProgram!.DepartmentId == departmentId ||
                        (t.Discipline!.DepartmentId == departmentId && t.ExternalRequestSentAt != null))
            .Select(t => new
            {
                t.Id,
                t.GroupId,
                GroupName = t.Group!.GroupNumber,
                GroupDepartmentId = t.Group.StudyProgram!.DepartmentId,
                GroupDepartmentName = t.Group.StudyProgram.Department!.Name,
                GroupFacultyId = t.Group.StudyProgram.Department.FacultyId,
                GroupFacultyName = t.Group.StudyProgram.Department.Faculty!.FacultyName,
                DisciplineId = t.DisciplineId!.Value,
                DisciplineName = t.Discipline!.Name,
                DisciplineDepartmentId = t.Discipline.DepartmentId,
                DisciplineDepartmentName = t.Discipline.Department!.Name,
                DisciplineFacultyId = t.Discipline.Department.FacultyId,
                t.ExternalRequestSentAt,
                t.SelectionPeriodStart,
                t.SelectionPeriodEnd,
                t.ScheduledDate,
                t.ScheduledTime,
                t.ScheduledPair
            })
            .ToListAsync(cancellationToken);

        var testingIds = rows.Select(r => r.Id).ToList();
        var teachers = await (
            from approval in _context.TestingTeacherApprovals.AsNoTracking()
            join teacher in _context.Users.AsNoTracking() on approval.TeacherId equals teacher.Id
            where testingIds.Contains(approval.TestingId)
            orderby teacher.Name
            select new { approval.TestingId, approval.TeacherId, teacher.Name, approval.Status, approval.Comment, approval.RespondedAt })
            .ToListAsync(cancellationToken);
        var teachersByTesting = teachers.ToLookup(a => a.TestingId);

        return rows
            .Select(r =>
            {
                var info = new TestingManagingDepartment.Info(
                    r.GroupDepartmentId, r.GroupFacultyId,
                    r.DisciplineDepartmentId, r.DisciplineFacultyId, r.ExternalRequestSentAt);
                var kind = r.GroupDepartmentId == departmentId
                    ? (info.IsExternal ? "outgoing" : "own")
                    : "incoming";
                return new DepartmentTestingDto(
                    r.Id,
                    r.GroupId,
                    r.GroupName,
                    r.GroupDepartmentName,
                    r.GroupFacultyName,
                    r.DisciplineId,
                    r.DisciplineName,
                    r.DisciplineDepartmentName,
                    kind,
                    info.ManagingDepartmentId == departmentId,
                    r.ExternalRequestSentAt.HasValue ? ToUtc(r.ExternalRequestSentAt.Value) : null,
                    r.SelectionPeriodStart.HasValue ? DateOnly.FromDateTime(r.SelectionPeriodStart.Value) : null,
                    r.SelectionPeriodEnd.HasValue ? DateOnly.FromDateTime(r.SelectionPeriodEnd.Value) : null,
                    r.ScheduledDate.HasValue ? DateOnly.FromDateTime(r.ScheduledDate.Value) : null,
                    r.ScheduledTime.HasValue ? TimeOnly.FromTimeSpan(r.ScheduledTime.Value) : null,
                    r.ScheduledPair,
                    teachersByTesting[r.Id]
                        .Select(a => new TestingTeacherAssignmentDto(
                            a.TeacherId,
                            a.Name,
                            a.Status ?? "pending",
                            a.Comment,
                            a.RespondedAt.HasValue ? ToUtc(a.RespondedAt.Value) : null))
                        .ToList());
            })
            .OrderBy(r => r.Kind == "incoming")
            .ThenBy(r => r.GroupName)
            .ThenBy(r => r.DisciplineName)
            .ToList();
    }

    /// <summary>
    /// Заявка кафедре другого факультета: дисциплина группы своей кафедры закреплена за ней,
    /// поэтому преподавателей и период назначает она. Повторная отправка ничего не меняет.
    /// </summary>
    public async Task<DateTimeOffset> SendExternalRequest(int testingId, CancellationToken cancellationToken)
    {
        var departmentId = RequireDepartment();
        var testing = await _context.Testings
            .Include(t => t.Group)
            .Include(t => t.Discipline)
            .SingleOrDefaultAsync(t => t.Id == testingId, cancellationToken)
            ?? throw new DocumentNotFoundException("Тестирование не найдено");
        var info = await TestingManagingDepartment.LoadAsync(_context, testingId, cancellationToken);
        if (info?.GroupDepartmentId != departmentId)
            throw new DocumentAccessException("Заявку отправляет кафедра, за которой закреплена группа.");
        if (!testing.DisciplineId.HasValue || !info.IsExternal || info.DisciplineDepartmentId is null)
            throw new DocumentValidationException("Заявка нужна только для дисциплин кафедр другого факультета.");

        if (testing.ExternalRequestSentAt is null)
        {
            var groupDepartmentName = await _context.Departments
                .Where(d => d.Id == departmentId)
                .Select(d => d.Name)
                .SingleOrDefaultAsync(cancellationToken);
            testing.ExternalRequestSentAt = DateTime.UtcNow;
            _context.WorkflowNotifications.Add(new WorkflowNotification
            {
                RecipientDepartmentId = info.DisciplineDepartmentId,
                TestingId = testingId,
                Type = "testing_external_request",
                Title = "Заявка на проверку остаточных знаний",
                Message = $"Кафедра {groupDepartmentName} просит назначить преподавателей и период проверки: " +
                          $"группа {testing.Group?.GroupNumber}, дисциплина «{testing.Discipline?.Name}».",
                CreatedAt = testing.ExternalRequestSentAt.Value
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        return ToUtc(testing.ExternalRequestSentAt.Value);
    }

    private static DateTimeOffset ToUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public async Task<IReadOnlyList<WorkflowNotificationDto>> GetDepartmentNotifications(
        bool unreadOnly,
        CancellationToken cancellationToken)
    {
        var departmentId = RequireDepartment();
        var notifications = _context.WorkflowNotifications
            .AsNoTracking()
            .Where(n => n.RecipientDepartmentId == departmentId);
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

    public async Task MarkDepartmentNotificationRead(
        int notificationId,
        CancellationToken cancellationToken)
    {
        var departmentId = RequireDepartment();
        var notification = await _context.WorkflowNotifications
            .SingleOrDefaultAsync(n => n.Id == notificationId &&
                                      n.RecipientDepartmentId == departmentId,
                cancellationToken);
        if (notification is null)
            throw new DocumentNotFoundException("Уведомление не найдено.");
        notification.ReadAt ??= DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Testing> GetTestingInDepartment(
        int testingId,
        CancellationToken cancellationToken)
    {
        var departmentId = RequireDepartment();
        if (!_currentUser.UserId.HasValue)
            throw new DocumentAccessException("Не определён текущий пользователь или его кафедра.");

        var testing = await _context.Testings
            .Include(t => t.Group)
            .Include(t => t.Discipline)
            .SingleOrDefaultAsync(t => t.Id == testingId, cancellationToken);
        if (testing is null)
            throw new DocumentNotFoundException("Тестирование не найдено");

        var info = await TestingManagingDepartment.LoadAsync(_context, testingId, cancellationToken);
        if (info?.ManagingDepartmentId != departmentId)
            throw new DocumentAccessException(info is { IsExternal: true, ExternalRequestSentAt: null }
                ? "Дисциплина закреплена за кафедрой другого факультета: отправьте ей заявку."
                : "Нельзя управлять тестированием другой кафедры.");

        return testing;
    }

    private int RequireDepartment() =>
        _currentUser.DepartmentId ??
        throw new DocumentAccessException("Не определена кафедра текущего пользователя.");
}
