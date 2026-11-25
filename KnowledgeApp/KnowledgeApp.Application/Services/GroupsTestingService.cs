using System.Data;
using KnowledgeApp.Application.DTOs.GroupsTesting;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Planning;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Application.Services;

public sealed partial class GroupsTestingService(
    KnowledgeTestDbContext context, IOptions<GroupsTestingOptions> settings, TimeProvider clock,
    ICurrentUserProvider? currentUser = null)
{
    private readonly GroupsTestingOptions options = settings.Value;

    public async Task<GroupsTestingResponse?> Get(int? facultyId, string? direction, CancellationToken ct)
    {
        if (facultyId is <= 0 || direction?.Length > 50)
            throw new ArgumentException("Некорректные фильтры факультета или направления");
        // Все чтения относятся к одному согласованному снимку, GET ничего не сохраняет.
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            var state = await context.TestingPlanningStates.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, ct);
            var snapshot = await Load(ct);
            if (state?.CurrentSemesterId != snapshot.Current.Id || state.InitializedAt is null) return null;
            var response = ToResponse(snapshot, state.Version, Evaluate(snapshot), facultyId, direction?.Trim());
            await tx.CommitAsync(ct);
            return response;
        });
    }

    public Task<GroupsTestingResponse> AutoFill(CancellationToken ct) => Write(null, ct);

    public Task<GroupsTestingResponse> Update(UpdateGroupsTestingRequest? request, CancellationToken ct)
    {
        if (request is null || request.CurrentSemesterId <= 0 || request.Version is null or < 0
            || request.Changes is null || (request.Changes.Count == 0 && request.StudentListFormationPeriod is null)
            || request.Changes.Count > 10000
            || request.Changes.Any(c => c is null || c.GroupId <= 0 || c.SemesterId <= 0)
            || request.Changes.GroupBy(c => (c.GroupId, c.SemesterId)).Any(g => g.Count() > 1))
            throw new ArgumentException("Укажите currentSemesterId, version и изменения назначений или период формирования списка студентов");
        if (request.StudentListFormationPeriod is { } period
            && (period.StartDate == default || period.EndDate == default || period.EndDate < period.StartDate))
            throw new ArgumentException("Укажите обе даты формирования списка студентов. Окончание должно быть не раньше начала.");
        return Write(request, ct);
    }

    private Task<GroupsTestingResponse> Write(UpdateGroupsTestingRequest? request, CancellationToken ct) =>
        context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            // Повтор transient-транзакции начинается без оставшихся отслеживаемых объектов.
            context.ChangeTracker.Clear();
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            // Общая строка сериализует POST/PATCH, в том числе первый подбор и пустой план.
            var locked = await context.TestingPlanningStates
                .FromSqlRaw("SELECT * FROM testing_planning_state WHERE id = 1 FOR UPDATE")
                .ToListAsync(ct);
            var state = locked.Single();
            var snapshot = await Load(ct);
            if (request is not null)
            {
                if (state.InitializedAt is null || state.CurrentSemesterId != snapshot.Current.Id
                    || request.CurrentSemesterId != snapshot.Current.Id || request.Version != state.Version)
                    throw new PlanningRuleException("Период или версия плана изменились. Обновите список перед сохранением.");
                ApplyChanges(snapshot, request.Changes!);
            }

            var periodChanged = false;
            if (request?.StudentListFormationPeriod is { } period)
            {
                var semester = snapshot.Semesters[snapshot.Current.Id];
                periodChanged = semester.StudentListFormationStartDate != period.StartDate
                    || semester.StudentListFormationEndDate != period.EndDate;
                if (periodChanged)
                {
                    semester.StudentListFormationStartDate = period.StartDate;
                    semester.StudentListFormationEndDate = period.EndDate;
                    context.Semesters.Attach(semester);
                    context.Entry(semester).Property(s => s.StudentListFormationStartDate).IsModified = true;
                    context.Entry(semester).Property(s => s.StudentListFormationEndDate).IsModified = true;
                }
            }
            var evaluation = Evaluate(snapshot);
            var newPeriod = state.CurrentSemesterId != snapshot.Current.Id || state.InitializedAt is null;
            var changed = await SynchronizeRecommendations(snapshot, evaluation, ct) || newPeriod || periodChanged;
            if (changed)
            {
                state.CurrentSemesterId = snapshot.Current.Id;
                if (newPeriod) state.InitializedAt = clock.GetUtcNow().UtcDateTime;
                state.Version++;
                await context.SaveChangesAsync(ct);
            }
            var response = ToResponse(snapshot, state.Version, evaluation, null, null);
            await tx.CommitAsync(ct);
            return response;
        });

    private void ApplyChanges(Snapshot snapshot, IReadOnlyList<GroupsTestingChange> changes)
    {
        var groups = snapshot.Programs.SelectMany(p => p.StudyGroups).Select(g => g.Id).ToHashSet();
        foreach (var change in changes)
        {
            if (!groups.Contains(change.GroupId) || !snapshot.PlanningIds.Contains(change.SemesterId))
                throw new ArgumentException("Группа или семестр не входят в текущий план");
            if (change.SemesterId == snapshot.Current.Id && snapshot.Decisions.Any(d =>
                    d.StudyProgramId == snapshot.GroupPrograms[change.GroupId]))
                throw new PlanningRuleException("Участие по этому профилю уже подтверждено кафедрой.");
            if (!snapshot.Programs.SelectMany(p => p.StudyGroups).Single(g => g.Id == change.GroupId).IsActive && change.Planned)
                throw new PlanningRuleException("Архивную группу нельзя назначить на тестирование.");
            var existing = snapshot.Assignments.Find(a => a.GroupId == change.GroupId && a.SemesterId == change.SemesterId);
            if (existing?.Locked == true || existing?.Author == "automatic")
            {
                if (change.Planned == (existing is not null)) continue; // Идемпотентное подтверждение.
                throw new PlanningRuleException("Нельзя напрямую менять автоматические назначения или назначения с тестированиями");
            }
            if (existing is not null) snapshot.Assignments.Remove(existing);
            if (change.Planned) snapshot.Assignments.Add(new(change.GroupId, change.SemesterId, "manual"));
        }
        // Проверяется итог всего пакета, поэтому порядок снятия и назначения не важен.
    }

    private Dictionary<int, ProfilePlan> Evaluate(Snapshot snapshot)
    {
        var history = snapshot.History.Where(h => h.Completed).ToLookup(h => snapshot.GroupPrograms[h.GroupId]);
        var window = snapshot.Window.Where(s => s.Period != "past")
            .Select(s => new PlanningSemester(s.Id, s.SemesterYear, s.SemesterPart)).ToArray();
        return snapshot.Programs.ToDictionary(p => p.Id, p =>
        {
            var active = p.StudyGroups.Where(g => g.IsActive).Select(g => g.Id).ToArray();
            var assignments = snapshot.Assignments.Where(a => snapshot.GroupPrograms[a.GroupId] == p.Id
                && (active.Contains(a.GroupId) || a.Locked)).ToList();
            var decision = snapshot.Decisions.SingleOrDefault(d => d.StudyProgramId == p.Id);
            var skipped = new HashSet<int>();
            if (decision is not null)
            {
                assignments.RemoveAll(a => a.SemesterId == snapshot.Current.Id);
                if (decision.SelectedGroupId is int selected)
                    assignments.Add(new(selected, snapshot.Current.Id, "manual", Locked: true));
                else skipped.Add(snapshot.Current.Id);
            }
            return GroupTestingPlanner.Calculate(active,
                history[p.Id].Select(h => new Participation(h.GroupId, snapshot.Semesters[h.SemesterId].SemesterYear * 2
                    + snapshot.Semesters[h.SemesterId].SemesterPart - 1)), window, assignments, skipped);
        });
    }

    // Для расчёта будущего учитываем решение HOD, в recommendation_history сохраняем исходный выбор ЛПР.
    private static Dictionary<(int, int), PlanAssignment> RecommendationAssignments(Snapshot snapshot,
        Dictionary<int, ProfilePlan> evaluation)
    {
        var result = evaluation.Values.SelectMany(p => p.Assignments).ToDictionary(a => (a.GroupId, a.SemesterId));
        // Архивирование после выпуска рекомендаций не стирает исходную строку текущего семестра.
        // HOD сможет отказаться от неё и указать замену, сохранив объяснение.
        var archived = snapshot.Programs.SelectMany(p => p.StudyGroups).Where(g => !g.IsActive).Select(g => g.Id).ToHashSet();
        foreach (var original in snapshot.Assignments.Where(a => a.SemesterId == snapshot.Current.Id && archived.Contains(a.GroupId)))
        {
            var program = snapshot.GroupPrograms[original.GroupId];
            foreach (var key in result.Keys.Where(k => k.SemesterId == snapshot.Current.Id && snapshot.GroupPrograms[k.GroupId] == program).ToArray())
                result.Remove(key);
            result[(original.GroupId, original.SemesterId)] = original;
        }
        foreach (var decision in snapshot.Decisions)
        {
            foreach (var key in result.Keys.Where(k => k.SemesterId == snapshot.Current.Id
                && snapshot.GroupPrograms[k.GroupId] == decision.StudyProgramId).ToArray())
                result.Remove(key);
            foreach (var original in snapshot.Assignments.Where(a => a.SemesterId == snapshot.Current.Id
                && snapshot.GroupPrograms[a.GroupId] == decision.StudyProgramId))
                result[(original.GroupId, original.SemesterId)] = original;
        }
        return result;
    }

    private async Task<bool> SynchronizeRecommendations(Snapshot snapshot, Dictionary<int, ProfilePlan> evaluation,
        CancellationToken ct)
    {
        var desired = RecommendationAssignments(snapshot, evaluation);
        var stored = await context.RecommendationHistory.Where(p => snapshot.PlanningIds.Contains(p.SemesterId)).ToListAsync(ct);
        var changed = false;
        foreach (var assignment in stored)
        {
            if (!desired.TryGetValue((assignment.StudyGroupId, assignment.SemesterId), out var next))
            {
                context.RecommendationHistory.Remove(assignment); changed = true;
            }
            else if (assignment.Author != next.Author)
            {
                assignment.Author = next.Author;
                if (next.Author == "manual") assignment.UserId = currentUser?.UserId ?? assignment.UserId;
                assignment.RecommendedAt = clock.GetUtcNow().UtcDateTime;
                changed = true;
            }
        }
        var existingKeys = stored.Select(p => (p.StudyGroupId, p.SemesterId)).ToHashSet();
        foreach (var item in desired.Values.Where(a => !existingKeys.Contains((a.GroupId, a.SemesterId))))
        {
            context.RecommendationHistory.Add(new RecommendationHistory {
                StudyGroupId = item.GroupId, SemesterId = item.SemesterId, Author = item.Author,
                RecommendedAt = clock.GetUtcNow().UtcDateTime, UserId = currentUser?.UserId });
            changed = true;
        }
        snapshot.Assignments = desired.Values.ToList();
        return changed;
    }

    private async Task<Snapshot> Load(CancellationToken ct)
    {
        var semesters = await context.Semesters.AsNoTracking().ToListAsync(ct);
        var window = GetWindow(semesters);
        var current = window.Single(s => s.Period == "current").Id;
        var planningIds = window.Where(s => s.Period != "past").Select(s => s.Id).ToArray();
        var programs = await context.StudyPrograms.AsNoTracking().Include(p => p.StudyGroups)
            .Include(p => p.Department).ThenInclude(d => d!.Faculty).OrderBy(p => p.Id).ToListAsync(ct);
        var groupPrograms = programs.SelectMany(p => p.StudyGroups.Select(g => (g.Id, Program: p.Id)))
            .ToDictionary(g => g.Id, g => g.Program);
        var decisions = await context.GroupParticipationDecisions.AsNoTracking().Where(d => d.SemesterId == current).ToListAsync(ct);
        var tests = await context.Testings.AsNoTracking().Where(t => t.SemesterId != null)
            .Select(t => new {
                t.Id, t.GroupId, SemesterId = t.SemesterId!.Value, t.Status,
                Started = t.DisciplineId != null || t.ReportId != null || t.ScheduledDate != null
                    || t.ScheduledTime != null || t.SelectionPeriodStart != null || t.SelectionPeriodEnd != null
                    || t.IsInPerson != null || t.Room != null || t.LmsUrl != null || t.ResultOfTesting != null
                    || context.TestingTeacherApprovals.Any(a => a.TestingId == t.Id) })
            .ToListAsync(ct);
        var semesterIds = semesters.Select(s => s.Id).ToHashSet();
        var validTests = tests.Where(t => groupPrograms.ContainsKey(t.GroupId) && semesterIds.Contains(t.SemesterId)).ToList();
        var history = validTests.GroupBy(t => (t.GroupId, t.SemesterId))
            .Select(g => new HistoryRow(g.Key.GroupId, g.Key.SemesterId,
                GroupTestingPlanner.HasCompletedTesting(g.Select(t => t.Status)))).ToList();
        var historyKeys = history.Select(h => (h.GroupId, h.SemesterId)).ToHashSet();
        var rows = await context.RecommendationHistory.AsNoTracking().Where(p => planningIds.Contains(p.SemesterId)).ToListAsync(ct);
        var assignments = rows.Select(p => new PlanAssignment(p.StudyGroupId, p.SemesterId, p.Author,
            historyKeys.Contains((p.StudyGroupId, p.SemesterId)))).ToList();
        var assignmentKeys = assignments.Select(a => (a.GroupId, a.SemesterId)).ToHashSet();
        foreach (var row in history.Where(h => planningIds.Contains(h.SemesterId)
            && !(h.SemesterId == current && decisions.Any(d => d.StudyProgramId == groupPrograms[h.GroupId]))))
            if (assignmentKeys.Add((row.GroupId, row.SemesterId)))
                assignments.Add(new(row.GroupId, row.SemesterId, "manual", Locked: true));
        return new(programs, semesters.ToDictionary(s => s.Id), window, groupPrograms, history, assignments, decisions,
            validTests.Select(t => new TestingRow(t.Id, t.GroupId, t.SemesterId,
                t.Started || GroupTestingPlanner.HasCompletedTesting([t.Status]))).ToList());
    }

    private IReadOnlyList<TestingSemesterDto> GetWindow(List<Semester> semesters)
    {
        var now = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId));
        return PlanningCalendar.GetWindow(semesters.Select(s => new PlanningSemester(s.Id, s.SemesterYear, s.SemesterPart)),
                DateOnly.FromDateTime(now.Date), options.SpringStartMonth, options.AutumnStartMonth)
            .Select((s, index) => new TestingSemesterDto(s.Id, s.Year, s.Part,
                index < 8 ? "past" : index == 8 ? "current" : "future")).ToArray();
    }

    private static GroupsTestingResponse ToResponse(Snapshot snapshot, long version,
        Dictionary<int, ProfilePlan> evaluation, int? facultyId, string? direction, int? departmentId = null)
    {
        var stored = snapshot.Assignments.ToDictionary(a => (a.GroupId, a.SemesterId));
        var expected = RecommendationAssignments(snapshot, evaluation);
        var requiresRecalculation = stored.Count != expected.Count || expected.Any(p =>
            !stored.TryGetValue(p.Key, out var existing) || existing.Author != p.Value.Author);
        var past = snapshot.Window.Where(s => s.Period == "past").ToList();
        var history = snapshot.History.ToDictionary(h => (h.GroupId, h.SemesterId));
        var programs = snapshot.Programs.Where(p => p.StudyGroups.Count > 0
                && (!facultyId.HasValue || p.Department?.FacultyId == facultyId)
                && (!departmentId.HasValue || p.DepartmentId == departmentId)
                && (string.IsNullOrEmpty(direction) || string.Equals(p.CypherOfTheDirection, direction, StringComparison.OrdinalIgnoreCase)))
            .Select(p => new TestingStudyProgramDto(p.Id, p.Name, p.CypherOfTheDirection,
                p.Department?.FacultyId, p.Department?.Faculty?.FacultyName,
                p.StudyGroups.OrderBy(g => g.GroupNumber).Select(g => new TestingGroupDto(g.Id, g.GroupNumber, null,
                    past.Select(s => new TestingHistoryCellDto(s.Id,
                        history.GetValueOrDefault((g.Id, s.Id))?.Completed)).ToArray(),
                    snapshot.Window.Where(s => s.Period != "past").Select(s =>
                    {
                        stored.TryGetValue((g.Id, s.Id), out var actual);
                        var cell = evaluation[p.Id].Cells.GetValueOrDefault((g.Id, s.Id))
                            ?? new PlanCell(false, null, false, "Группа в архиве");
                        var confirmed = s.Id == snapshot.Current.Id && snapshot.Decisions.Any(d => d.StudyProgramId == p.Id);
                        return new PlannedTestingCellDto(s.Id, actual is not null, actual?.Author,
                            !requiresRecalculation && cell.Editable && !confirmed && g.IsActive,
                            requiresRecalculation ? "История изменилась. Выполните пересчёт плана."
                                : confirmed ? "Участие подтверждено кафедрой" : cell.RestrictionReason);
                    }).ToArray(), g.IsActive)).ToArray(),
                snapshot.Decisions.Where(d => d.StudyProgramId == p.Id)
                    .Select(d => new ParticipationDecisionDto(d.RecommendedGroupId, d.SelectedGroupId, d.Reason, d.DecidedAt))
                    .SingleOrDefault())).ToArray();
        var semester = snapshot.Semesters[snapshot.Current.Id];
        var period = semester.StudentListFormationStartDate is { } start && semester.StudentListFormationEndDate is { } end
            ? new StudentListFormationPeriodDto(start, end) : null;
        return new(snapshot.Current.Id, version, requiresRecalculation, snapshot.Window, programs, period);
    }

    private sealed record TestingRow(int Id, int GroupId, int SemesterId, bool Started);
    private sealed record HistoryRow(int GroupId, int SemesterId, bool Completed);
    private sealed class Snapshot(List<StudyProgram> programs, Dictionary<int, Semester> semesters,
        IReadOnlyList<TestingSemesterDto> window, Dictionary<int, int> groupPrograms,
        List<HistoryRow> history, List<PlanAssignment> assignments, List<GroupParticipationDecision> decisions,
        List<TestingRow> tests)
    {
        public List<GroupParticipationDecision> Decisions { get; } = decisions;
        public List<TestingRow> Tests { get; } = tests;
        public List<StudyProgram> Programs { get; } = programs;
        public Dictionary<int, Semester> Semesters { get; } = semesters;
        public IReadOnlyList<TestingSemesterDto> Window { get; } = window;
        public TestingSemesterDto Current => Window.Single(s => s.Period == "current");
        public int[] PlanningIds => Window.Where(s => s.Period != "past").Select(s => s.Id).ToArray();
        public Dictionary<int, int> GroupPrograms { get; } = groupPrograms;
        public List<HistoryRow> History { get; } = history;
        public List<PlanAssignment> Assignments { get; set; } = assignments;
    }
}
