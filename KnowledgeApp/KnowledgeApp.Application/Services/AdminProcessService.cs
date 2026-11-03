using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.DTOs.Documents;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed class AdminProcessService
{
    private const int MaxPageSize = 200;
    private const string RescheduleAction = "reschedule";

    private static readonly IReadOnlyDictionary<string, string> ActionLabels = new Dictionary<string, string>
    {
        [RescheduleAction] = "Перенос даты и времени"
    };

    private readonly KnowledgeTestDbContext _context;

    public AdminProcessService(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<AdminProcessPageDto> GetProcesses(AdminProcessQuery query, CancellationToken cancellationToken)
    {
        if (query.Page < 1)
            throw new DocumentValidationException("Номер страницы должен быть больше нуля.");
        if (query.PageSize < 1 || query.PageSize > MaxPageSize)
            throw new DocumentValidationException($"Размер страницы должен быть от 1 до {MaxPageSize}.");
        if (query.DateFrom.HasValue && query.DateTo.HasValue && query.DateFrom > query.DateTo)
            throw new DocumentValidationException("Дата «с» позже даты «по».");

        var approvalFilter = AdminProcessState.Normalize(
            query.ApprovalStatuses, AdminProcessState.ApprovalOptions, "approvalStatus");
        var stageFilter = AdminProcessState.Normalize(
            query.Stages, AdminProcessState.StageOptions, "stage");
        var reportFilter = AdminProcessState.Normalize(
            query.ReportStatuses, AdminProcessState.ReportOptions, "reportStatus");

        var testings = _context.Testings.AsNoTracking();
        if (query.SemesterId.HasValue)
            testings = testings.Where(t => t.SemesterId == query.SemesterId);
        if (query.DepartmentId.HasValue)
            testings = testings.Where(t => t.Discipline!.DepartmentId == query.DepartmentId);
        if (query.FacultyId.HasValue)
            testings = testings.Where(t => t.Discipline!.Department!.FacultyId == query.FacultyId);
        if (query.DateFrom.HasValue)
        {
            var from = query.DateFrom.Value.ToDateTime(TimeOnly.MinValue);
            testings = testings.Where(t => t.ScheduledDate >= from);
        }
        if (query.DateTo.HasValue)
        {
            var to = query.DateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            testings = testings.Where(t => t.ScheduledDate < to);
        }

        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            testings = testings.Where(t =>
                t.Group!.GroupNumber.Contains(search) ||
                t.Discipline!.Name.Contains(search) ||
                t.Discipline.Department!.Name.Contains(search) ||
                _context.TestingTeacherApprovals.Any(a =>
                    a.TestingId == t.Id &&
                    _context.Users.Any(u => u.Id == a.TeacherId && u.Name!.Contains(search))));
        }

        var processes = await Load(testings, cancellationToken);
        var filtered = processes
            .Where(p => approvalFilter.Count == 0 || approvalFilter.Contains(p.ApprovalStatus))
            .Where(p => stageFilter.Count == 0 || stageFilter.Contains(p.Stage))
            .Where(p => reportFilter.Count == 0 || reportFilter.Contains(p.ReportStatus))
            .ToList();

        var counts = AdminProcessState.ApprovalOptions()
            .Select(o => new AdminStatusCountDto(
                o.Value,
                o.Label,
                processes.Count(p => p.ApprovalStatus == o.Value &&
                                     (stageFilter.Count == 0 || stageFilter.Contains(p.Stage)) &&
                                     (reportFilter.Count == 0 || reportFilter.Contains(p.ReportStatus)))))
            .ToList();

        var items = filtered
            .OrderBy(p => p.ScheduledDate is null)
            .ThenBy(p => p.ScheduledDate)
            .ThenBy(p => p.ScheduledTime)
            .ThenBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new AdminProcessPageDto(items, filtered.Count, query.Page, query.PageSize, counts);
    }

    public async Task<AdminProcessDto> GetProcess(int testingId, CancellationToken cancellationToken)
    {
        var process = (await Load(_context.Testings.AsNoTracking().Where(t => t.Id == testingId), cancellationToken))
            .SingleOrDefault();
        return process ?? throw new DocumentNotFoundException("Процесс тестирования не найден");
    }

    public async Task<AdminProcessFiltersDto> GetFilters(CancellationToken cancellationToken)
    {
        var semesters = await _context.Semesters
            .AsNoTracking()
            .OrderByDescending(s => s.SemesterYear)
            .ThenByDescending(s => s.SemesterPart)
            .Select(s => new { s.Id, s.SemesterYear, s.SemesterPart })
            .ToListAsync(cancellationToken);
        var faculties = await _context.Faculties
            .AsNoTracking()
            .OrderBy(f => f.FacultyName)
            .Select(f => new FilterOptionDto { Value = f.Id.ToString(), Label = f.FacultyName })
            .ToListAsync(cancellationToken);
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .Select(d => new FilterOptionDto { Value = d.Id.ToString(), Label = d.Name })
            .ToListAsync(cancellationToken);

        return new AdminProcessFiltersDto(
            AdminProcessState.ApprovalOptions(),
            AdminProcessState.StageOptions(),
            AdminProcessState.ReportOptions(),
            semesters
                .Select(s => new FilterOptionDto
                {
                    Value = s.Id.ToString(),
                    Label = AcademicPeriod.FormatSemester(s.SemesterYear, s.SemesterPart)
                })
                .ToList(),
            faculties,
            departments);
    }

    public async Task<AdminProcessDto> Reschedule(
        int testingId,
        DateOnly? date,
        string? timeText,
        string? reason,
        AdminActor actor,
        CancellationToken cancellationToken)
    {
        if (!date.HasValue || string.IsNullOrWhiteSpace(timeText))
            throw new DocumentValidationException("Укажите новую дату и время.");
        if (!TimeOnly.TryParseExact(timeText.Trim(), ["HH:mm", "HH:mm:ss"], out var parsedTime))
            throw new DocumentValidationException("Время указывается в формате ЧЧ:ММ.");
        TimeOnly? time = parsedTime;
        var normalizedReason = AdminActors.RequireReason(reason, "Укажите причину переноса.");

        var current = await GetProcess(testingId, cancellationToken);
        if (current.ApprovalStatus != AdminProcessState.Approved)
            throw new DocumentConflictException("Вручную переносятся только согласованные процессы.");
        if (current.ScheduledDate == date && current.ScheduledTime == time)
            throw new DocumentValidationException("Новые дата и время совпадают с текущими.");

        var testing = await _context.Testings.AsNoTracking().SingleAsync(t => t.Id == testingId, cancellationToken);
        var scheduledDate = date.Value.ToDateTime(TimeOnly.MinValue);
        var scheduledTime = time.Value.ToTimeSpan();
        var pairNumber = TestingPairSchedule.All.FirstOrDefault(slot => slot.StartsAt == time.Value)?.PairNumber;
        var previousValue = FormatSlot(current.ScheduledDate, current.ScheduledTime);
        var newValue = FormatSlot(date, time);
        var actorName = await AdminActors.NameOf(_context, actor, cancellationToken);

        await _context.InTransactionAsync(System.Data.IsolationLevel.Serializable, async () =>
        {
            await TestingSlotGuard.EnsureSlotIsAvailable(_context, testing, date.Value, time.Value, cancellationToken);

            var changed = await _context.Testings
                .Where(t => t.Id == testingId &&
                            t.ScheduledDate == testing.ScheduledDate &&
                            t.ScheduledTime == testing.ScheduledTime)
                .ExecuteUpdateAsync(update => update
                    .SetProperty(t => t.ScheduledDate, scheduledDate)
                    .SetProperty(t => t.ScheduledTime, scheduledTime)
                    .SetProperty(t => t.ScheduledPair, pairNumber), cancellationToken);
            if (changed == 0)
                throw new DocumentConflictException("Дата процесса уже изменилась. Обновите страницу и повторите действие.");

            var changedAt = DateTime.UtcNow;
            _context.TestingChanges.Add(new TestingChange
            {
                TestingId = testingId,
                Action = RescheduleAction,
                PreviousValue = previousValue,
                NewValue = newValue,
                Reason = normalizedReason,
                ChangedByUserId = actor.UserId,
                ChangedByName = actorName,
                ChangedAt = changedAt
            });
            await NotifyParticipants(
                testingId,
                "testing_rescheduled",
                "Администратор перенёс проверку",
                $"Проверка группы {current.GroupName} по дисциплине «{current.DisciplineName}» перенесена с {previousValue} на {newValue}. Причина: {normalizedReason}",
                changedAt,
                cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return await GetProcess(testingId, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminProcessChangeDto>> GetHistory(int testingId, CancellationToken cancellationToken)
    {
        if (!await _context.Testings.AnyAsync(t => t.Id == testingId, cancellationToken))
            throw new DocumentNotFoundException("Процесс тестирования не найден");

        var changes = await _context.TestingChanges
            .AsNoTracking()
            .Where(c => c.TestingId == testingId)
            .OrderByDescending(c => c.ChangedAt)
            .ThenByDescending(c => c.Id)
            .ToListAsync(cancellationToken);

        return changes
            .Select(c => new AdminProcessChangeDto(
                c.Id,
                c.Action,
                ActionLabels.GetValueOrDefault(c.Action, c.Action),
                c.PreviousValue,
                c.NewValue,
                c.Reason,
                c.ChangedByUserId,
                c.ChangedByName,
                DocumentStatusInfo.AsUtc(c.ChangedAt)))
            .ToList();
    }

    private async Task NotifyParticipants(
        int testingId,
        string type,
        string title,
        string message,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var teacherIds = await _context.TestingTeacherApprovals
            .Where(a => a.TestingId == testingId)
            .Select(a => a.TeacherId)
            .ToListAsync(cancellationToken);
        foreach (var teacherId in teacherIds)
        {
            _context.WorkflowNotifications.Add(new WorkflowNotification
            {
                RecipientUserId = teacherId,
                TestingId = testingId,
                Type = type,
                Title = title,
                Message = message,
                CreatedAt = createdAt
            });
        }

        var departmentId = (await TestingManagingDepartment.LoadAsync(_context, testingId, cancellationToken))
            ?.ManagingDepartmentId;
        if (departmentId.HasValue)
        {
            _context.WorkflowNotifications.Add(new WorkflowNotification
            {
                RecipientDepartmentId = departmentId.Value,
                TestingId = testingId,
                Type = type,
                Title = title,
                Message = message,
                CreatedAt = createdAt
            });
        }
    }

    private static string? FormatSlot(DateOnly? date, TimeOnly? time) =>
        date.HasValue
            ? time.HasValue ? $"{date:dd.MM.yyyy} {time:HH:mm}" : $"{date:dd.MM.yyyy}"
            : null;

    private async Task<List<AdminProcessDto>> Load(
        IQueryable<Infrastructure.Entities.Testing> testings,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from t in testings
            join s in _context.Semesters on t.SemesterId equals s.Id into semesters
            from s in semesters.DefaultIfEmpty()
            join r in _context.Reports on t.ReportId equals r.Id into reports
            from r in reports.DefaultIfEmpty()
            select new
            {
                t.Id,
                t.GroupId,
                GroupName = t.Group!.GroupNumber,
                t.DisciplineId,
                DisciplineName = t.Discipline!.Name,
                DepartmentId = t.Discipline.DepartmentId,
                DepartmentName = t.Discipline.Department!.Name,
                FacultyName = t.Discipline.Department.Faculty!.FacultyName,
                t.SemesterId,
                SemesterYear = s != null ? (int?)s.SemesterYear : null,
                SemesterPart = s != null ? (int?)s.SemesterPart : null,
                t.SelectionPeriodStart,
                t.SelectionPeriodEnd,
                t.ScheduledDate,
                t.ScheduledTime,
                t.ScheduledPair,
                t.IsInPerson,
                t.Room,
                t.LmsUrl,
                t.ReportId,
                ReportUploaded = r != null && r.FilePath != null,
                ReportStatus = r != null ? r.Status : null,
                PaperSubmitted = r != null ? r.DoneInPaperForm : null
            })
            .ToListAsync(cancellationToken);

        var ids = rows.Select(r => r.Id).ToList();
        var teachers = await (
                from a in _context.TestingTeacherApprovals.AsNoTracking()
                where ids.Contains(a.TestingId)
                join u in _context.Users on a.TeacherId equals u.Id into users
                from u in users.DefaultIfEmpty()
                orderby u.Name
                select new { a.TestingId, a.TeacherId, u.Name, a.Status, a.Comment, a.RespondedAt })
            .ToListAsync(cancellationToken);
        var teachersByTesting = teachers.ToLookup(t => t.TestingId);
        var today = DateOnly.FromDateTime(DateTime.Now);

        return rows.Select(r =>
            {
                var processTeachers = teachersByTesting[r.Id]
                    .Select(t => new AdminProcessTeacherDto(
                        t.TeacherId,
                        t.Name,
                        t.Status ?? AdminProcessState.Pending,
                        AdminProcessState.TeacherStatusLabel(t.Status),
                        t.Comment,
                        t.RespondedAt.HasValue ? DocumentStatusInfo.AsUtc(t.RespondedAt.Value) : null))
                    .ToList();
                var snapshot = new AdminProcessState.Snapshot(
                    processTeachers.Select(t => t.ApprovalStatus).ToList(),
                    r.SelectionPeriodStart,
                    r.SelectionPeriodEnd,
                    r.ScheduledDate,
                    r.ScheduledTime,
                    r.ReportUploaded,
                    r.ReportStatus,
                    r.PaperSubmitted);
                var approval = AdminProcessState.ApprovalStatus(snapshot, today);
                var stage = AdminProcessState.Stage(snapshot, today);
                var report = AdminProcessState.ReportStatus(snapshot);

                return new AdminProcessDto(
                    r.Id,
                    r.GroupId,
                    r.GroupName,
                    r.DisciplineId,
                    r.DisciplineName,
                    r.DepartmentId,
                    r.DepartmentName,
                    r.FacultyName,
                    r.SemesterId,
                    r.SemesterYear.HasValue && r.SemesterPart.HasValue
                        ? AcademicPeriod.FormatSemester(r.SemesterYear.Value, r.SemesterPart.Value)
                        : null,
                    processTeachers,
                    string.Join(", ", processTeachers.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n))),
                    r.SelectionPeriodStart.HasValue ? DateOnly.FromDateTime(r.SelectionPeriodStart.Value) : null,
                    r.SelectionPeriodEnd.HasValue ? DateOnly.FromDateTime(r.SelectionPeriodEnd.Value) : null,
                    r.ScheduledDate.HasValue ? DateOnly.FromDateTime(r.ScheduledDate.Value) : null,
                    r.ScheduledTime.HasValue ? TimeOnly.FromTimeSpan(r.ScheduledTime.Value) : null,
                    r.ScheduledPair,
                    r.IsInPerson,
                    r.Room,
                    r.LmsUrl,
                    approval,
                    AdminProcessState.ApprovalLabel(approval),
                    stage,
                    AdminProcessState.StageLabel(stage),
                    r.ReportId,
                    report,
                    AdminProcessState.ReportLabel(report),
                    approval is AdminProcessState.Approved or AdminProcessState.Expired);
            })
            .ToList();
    }
}
