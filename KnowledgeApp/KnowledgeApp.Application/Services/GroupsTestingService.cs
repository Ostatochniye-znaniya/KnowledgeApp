using System.Data;
using KnowledgeApp.Application.DTOs.GroupsTesting;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Planning;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Application.Services;

public sealed class GroupsTestingService(
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
            var state = await context.TestingPlanningStates.AsNoTracking().SingleAsync(s => s.Id == 1, ct);
            var snapshot = await Load(ct);
            if (state.CurrentSemesterId != snapshot.Current.Id || state.InitializedAt is null) return null;
            var response = ToResponse(snapshot, state.Version, Evaluate(snapshot), facultyId, direction?.Trim());
            await tx.CommitAsync(ct);
            return response;
        });
    }

    public Task<GroupsTestingResponse> AutoFill(CancellationToken ct) => Write(null, ct);

    public Task<GroupsTestingResponse> Update(UpdateGroupsTestingRequest? request, CancellationToken ct)
    {
        if (request is null || request.CurrentSemesterId <= 0 || request.Version is null or < 0
            || request.Changes is null || request.Changes.Count == 0 || request.Changes.Count > 10000
            || request.Changes.Any(c => c is null || c.GroupId <= 0 || c.SemesterId <= 0)
            || request.Changes.GroupBy(c => (c.GroupId, c.SemesterId)).Any(g => g.Count() > 1))
            throw new ArgumentException("Укажите currentSemesterId, version и непустой пакет уникальных изменений");
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

            var evaluation = Evaluate(snapshot);
            var desired = evaluation.Values.SelectMany(p => p.Assignments)
                .ToDictionary(a => (a.GroupId, a.SemesterId));
            var stored = await context.RecommendationHistory
                .Where(p => snapshot.PlanningIds.Contains(p.SemesterId)).ToListAsync(ct);
            var newPeriod = state.CurrentSemesterId != snapshot.Current.Id || state.InitializedAt is null;
            var changed = newPeriod;
            foreach (var assignment in stored)
            {
                if (!desired.TryGetValue((assignment.StudyGroupId, assignment.SemesterId), out var next))
                {
                    context.RecommendationHistory.Remove(assignment);
                    changed = true;
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
            foreach (var next in desired.Values.Where(a => !existingKeys.Contains((a.GroupId, a.SemesterId))))
            {
                context.RecommendationHistory.Add(new RecommendationHistory
                {
                    StudyGroupId = next.GroupId, SemesterId = next.SemesterId,
                    Author = next.Author, RecommendedAt = clock.GetUtcNow().UtcDateTime,
                    UserId = currentUser?.UserId,
                });
                changed = true;
            }
            if (changed)
            {
                state.CurrentSemesterId = snapshot.Current.Id;
                if (newPeriod) state.InitializedAt = clock.GetUtcNow().UtcDateTime;
                state.Version++;
                await context.SaveChangesAsync(ct);
            }
            snapshot.Assignments = desired.Values.ToList();
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
        var assignments = snapshot.Assignments.ToLookup(a => snapshot.GroupPrograms[a.GroupId]);
        var window = snapshot.Window.Where(s => s.Period != "past")
            .Select(s => new PlanningSemester(s.Id, s.SemesterYear, s.SemesterPart)).ToArray();
        return snapshot.Programs.ToDictionary(p => p.Id, p => GroupTestingPlanner.Calculate(
            p.StudyGroups.Select(g => g.Id).ToArray(),
            history[p.Id].Select(h => new Participation(h.GroupId, snapshot.Semesters[h.SemesterId].SemesterYear * 2
                + snapshot.Semesters[h.SemesterId].SemesterPart - 1)), window, assignments[p.Id].ToArray()));
    }

    private async Task<Snapshot> Load(CancellationToken ct)
    {
        var semesters = await context.Semesters.AsNoTracking().ToListAsync(ct);
        var window = GetWindow(semesters);
        var planningIds = window.Where(s => s.Period != "past").Select(s => s.Id).ToArray();
        var programs = await context.StudyPrograms.AsNoTracking().Include(p => p.StudyGroups)
            .Include(p => p.Department).ThenInclude(d => d!.Faculty).OrderBy(p => p.Id).ToListAsync(ct);
        var groupPrograms = programs.SelectMany(p => p.StudyGroups.Select(g => (g.Id, Program: p.Id)))
            .ToDictionary(g => g.Id, g => g.Program);
        var tests = await context.Testings.AsNoTracking().Where(t => t.SemesterId != null)
            .Select(t => new {
                GroupId = t.RecommendationHistory != null ? t.RecommendationHistory.StudyGroupId : t.GroupId,
                SemesterId = t.RecommendationHistory != null ? t.RecommendationHistory.SemesterId : t.SemesterId!.Value,
                t.Status })
            .ToListAsync(ct);
        var semesterIds = semesters.Select(s => s.Id).ToHashSet();
        var history = tests.Where(t => groupPrograms.ContainsKey(t.GroupId) && semesterIds.Contains(t.SemesterId))
            .GroupBy(t => (t.GroupId, t.SemesterId))
            .Select(g => new HistoryRow(g.Key.GroupId, g.Key.SemesterId,
                GroupTestingPlanner.HasCompletedTesting(g.Select(t => t.Status)))).ToList();
        var historyKeys = history.Select(h => (h.GroupId, h.SemesterId)).ToHashSet();
        var rows = await context.RecommendationHistory.AsNoTracking()
            .Where(p => planningIds.Contains(p.SemesterId)).ToListAsync(ct);
        var assignments = rows.Select(p => new PlanAssignment(p.StudyGroupId, p.SemesterId, p.Author,
            historyKeys.Contains((p.StudyGroupId, p.SemesterId)))).ToList();
        var assignmentKeys = assignments.Select(a => (a.GroupId, a.SemesterId)).ToHashSet();
        // Старые CRUD Testing не знали новой таблицы. Существующие расписания сохраняем
        // как фиксированные ручные назначения; уже известный источник не перезаписываем.
        foreach (var row in history.Where(h => planningIds.Contains(h.SemesterId)))
            if (assignmentKeys.Add((row.GroupId, row.SemesterId)))
                assignments.Add(new(row.GroupId, row.SemesterId, "manual", Locked: true));
        return new(programs, semesters.ToDictionary(s => s.Id), window, groupPrograms, history, assignments);
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
        Dictionary<int, ProfilePlan> evaluation, int? facultyId, string? direction)
    {
        var stored = snapshot.Assignments.ToDictionary(a => (a.GroupId, a.SemesterId));
        var expected = evaluation.Values.SelectMany(p => p.Assignments).ToDictionary(a => (a.GroupId, a.SemesterId));
        var requiresRecalculation = stored.Count != expected.Count || expected.Any(p =>
            !stored.TryGetValue(p.Key, out var existing) || existing.Author != p.Value.Author);
        var past = snapshot.Window.Where(s => s.Period == "past").ToList();
        var history = snapshot.History.ToDictionary(h => (h.GroupId, h.SemesterId));
        var programs = snapshot.Programs.Where(p => p.StudyGroups.Count > 0
                && (!facultyId.HasValue || p.Department?.FacultyId == facultyId)
                && (string.IsNullOrEmpty(direction) || string.Equals(p.CypherOfTheDirection, direction, StringComparison.OrdinalIgnoreCase)))
            .Select(p => new TestingStudyProgramDto(p.Id, p.Name, p.CypherOfTheDirection,
                p.Department?.FacultyId, p.Department?.Faculty?.FacultyName,
                p.StudyGroups.OrderBy(g => g.GroupNumber).Select(g => new TestingGroupDto(g.Id, g.GroupNumber, null,
                    past.Select(s => new TestingHistoryCellDto(s.Id,
                        history.GetValueOrDefault((g.Id, s.Id))?.Completed)).ToArray(),
                    snapshot.Window.Where(s => s.Period != "past").Select(s =>
                    {
                        stored.TryGetValue((g.Id, s.Id), out var actual);
                        var cell = evaluation[p.Id].Cells[(g.Id, s.Id)];
                        return new PlannedTestingCellDto(s.Id, actual is not null, actual?.Author,
                            !requiresRecalculation && cell.Editable,
                            requiresRecalculation ? "История изменилась. Выполните пересчёт плана." : cell.RestrictionReason);
                    }).ToArray())).ToArray())).ToArray();
        return new(snapshot.Current.Id, version, requiresRecalculation, snapshot.Window, programs);
    }

    private sealed record HistoryRow(int GroupId, int SemesterId, bool Completed);
    private sealed class Snapshot(List<StudyProgram> programs, Dictionary<int, Semester> semesters,
        IReadOnlyList<TestingSemesterDto> window, Dictionary<int, int> groupPrograms,
        List<HistoryRow> history, List<PlanAssignment> assignments)
    {
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
