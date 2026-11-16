using System.Data;
using KnowledgeApp.Application.DTOs.GroupsTesting;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Domain.Planning;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed partial class GroupsTestingService
{
    public async Task<HodGroupsResponse> GetForHod(CancellationToken ct)
    {
        var department = await RequireDepartment(ct);
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            var state = await context.TestingPlanningStates.AsNoTracking().SingleAsync(s => s.Id == 1, ct);
            var snapshot = await Load(ct);
            var result = state.InitializedAt is null || state.CurrentSemesterId != snapshot.Current.Id
                ? new HodGroupsResponse(department.Id, department.Name, null, [])
                : ToHodResponse(snapshot, state.Version, department, Evaluate(snapshot));
            await tx.CommitAsync(ct);
            return result;
        });
    }

    public async Task<HodGroupsResponse> UpdateForHod(UpdateHodGroupsRequest? request, CancellationToken ct)
    {
        if (request is null || request.CurrentSemesterId <= 0 || request.Version is null or < 0
            || request.Decisions is null || request.Decisions.Count is 0 or > 1000
            || request.Decisions.Any(d => d is null || d.StudyProgramId <= 0 || d.SelectedGroupId is <= 0)
            || request.Decisions.Select(d => d.StudyProgramId).Distinct().Count() != request.Decisions.Count)
            throw new ArgumentException("Укажите текущий семестр, версию и уникальные решения по профилям.");
        var department = await RequireDepartment(ct);
        return await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, ct);
            var state = await context.LockTestingPlan(ct);
            var snapshot = await Load(ct);
            if (state.InitializedAt is null || state.CurrentSemesterId != snapshot.Current.Id
                || request.CurrentSemesterId != snapshot.Current.Id || request.Version != state.Version)
                throw new PlanningRuleException("План изменился. Обновите таблицу перед сохранением.");

            foreach (var input in request.Decisions)
            {
                var program = snapshot.Programs.SingleOrDefault(p => p.Id == input.StudyProgramId);
                if (program is null || program.DepartmentId != department.Id)
                    throw new DocumentAccessException("Профиль не относится к вашей кафедре.");
                var existing = snapshot.Decisions.SingleOrDefault(d => d.StudyProgramId == program.Id);
                var recommendation = snapshot.Assignments.SingleOrDefault(a => a.SemesterId == snapshot.Current.Id
                    && snapshot.GroupPrograms[a.GroupId] == program.Id);
                var recommended = existing is not null ? existing.RecommendedGroupId : recommendation?.GroupId;
                var reason = ReportDocumentService.NormalizeComment(input.Reason);
                if (input.SelectedGroupId != recommended && reason is null)
                    throw new ArgumentException("Укажите причину отказа или замены рекомендованной группы.");
                if (recommendation?.Author == "automatic" && input.SelectedGroupId is not null
                    && input.SelectedGroupId != recommended)
                    throw new PlanningRuleException("Назначение автоподбором можно отменить или восстановить. Выбрать другую группу нельзя.");
                if (input.SelectedGroupId is int selected && !EligibleGroups(snapshot, program).Contains(selected))
                    throw new PlanningRuleException("Замена должна быть активной группой того же профиля и соблюдать цикл тестирований.");
                var startedGroups = snapshot.Tests.Where(t => t.SemesterId == snapshot.Current.Id && t.Started
                    && snapshot.GroupPrograms[t.GroupId] == program.Id).Select(t => t.GroupId).Distinct().ToArray();
                if (startedGroups.Length > 0 && (startedGroups.Length != 1 || input.SelectedGroupId != startedGroups[0]))
                    throw new PlanningRuleException("Для группы уже выбраны дисциплины или начата организация проверки. Заменить её нельзя.");

                var decision = existing ?? new GroupParticipationDecision {
                    StudyProgramId = program.Id, SemesterId = snapshot.Current.Id, RecommendedGroupId = recommended };
                decision.SelectedGroupId = input.SelectedGroupId;
                decision.Reason = reason;
                decision.DecidedAt = clock.GetUtcNow().UtcDateTime;
                decision.DecidedByUserId = currentUser?.UserId;
                if (existing is null) { snapshot.Decisions.Add(decision); context.GroupParticipationDecisions.Add(decision); }
                else context.GroupParticipationDecisions.Update(decision);
            }

            // Смена текущей группы пересчитывает будущие автоматические назначения.
            // Конфликт с будущим ручным выбором ЛПР отклоняет весь пакет, ничего не стирая.
            var evaluation = Evaluate(snapshot);
            await SynchronizeRecommendations(snapshot, evaluation, ct);
            state.Version++;
            await context.SaveChangesAsync(ct);
            foreach (var input in request.Decisions)
            {
                var decision = snapshot.Decisions.Single(d => d.StudyProgramId == input.StudyProgramId);
                await UpdateDisciplineSlots(snapshot, decision, ct);
            }
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ToHodResponse(snapshot, state.Version, department, evaluation);
        });
    }

    private async Task UpdateDisciplineSlots(Snapshot snapshot, GroupParticipationDecision decision, CancellationToken ct)
    {
        var groupIds = snapshot.GroupPrograms.Where(p => p.Value == decision.StudyProgramId).Select(p => p.Key).ToArray();
        var testings = await context.Testings.Where(t => t.SemesterId == snapshot.Current.Id && groupIds.Contains(t.GroupId)).ToListAsync(ct);
        // Чужие пустые места можно убрать, заполненные записи защищены проверкой Started выше.
        context.Testings.RemoveRange(testings.Where(t => t.GroupId != decision.SelectedGroupId));
        if (decision.SelectedGroupId is not int selected) return;
        var selectedTests = testings.Where(t => t.GroupId == selected).ToList();
        foreach (var test in selectedTests) test.GroupParticipationDecisionId = decision.Id;
        var recommendation = await context.RecommendationHistory.SingleOrDefaultAsync(r => r.SemesterId == snapshot.Current.Id
            && r.StudyGroupId == selected, ct);
        for (var index = selectedTests.Count; index < GroupDisciplineService.MaxDisciplinesPerGroup; index++)
            context.Testings.Add(new Testing {
                GroupId = selected, SemesterId = snapshot.Current.Id,
                GroupParticipationDecisionId = decision.Id, RecommendationHistoryId = recommendation?.Id });
    }

    private static int[] EligibleGroups(Snapshot snapshot, StudyProgram program)
    {
        var active = program.StudyGroups.Where(g => g.IsActive).Select(g => g.Id).ToArray();
        var history = snapshot.History.Where(h => h.Completed && snapshot.GroupPrograms[h.GroupId] == program.Id)
            .Select(h => new Participation(h.GroupId, snapshot.Semesters[h.SemesterId].SemesterYear * 2
                + snapshot.Semesters[h.SemesterId].SemesterPart - 1)).ToArray();
        var current = new PlanningSemester(snapshot.Current.Id, snapshot.Current.SemesterYear, snapshot.Current.SemesterPart);
        var automatic = snapshot.Assignments.SingleOrDefault(a => a.SemesterId == current.Id
            && snapshot.GroupPrograms[a.GroupId] == program.Id && a.Author == "automatic");
        return active.Where(id => automatic is null || id == automatic.GroupId).Where(id =>
        {
            try { GroupTestingPlanner.Calculate(active, history, [current], [new(id, current.Id, "manual")]); return true; }
            catch (PlanningRuleException) { return false; }
        }).ToArray();
    }

    private static HodGroupsResponse ToHodResponse(Snapshot snapshot, long version, Department department,
        Dictionary<int, ProfilePlan> evaluation)
    {
        var plan = ToResponse(snapshot, version, evaluation, null, null, department.Id);
        var choices = snapshot.Programs.Where(p => p.DepartmentId == department.Id && p.StudyGroups.Count > 0).Select(p =>
        {
            var decision = snapshot.Decisions.SingleOrDefault(d => d.StudyProgramId == p.Id);
            var recommended = decision is not null ? decision.RecommendedGroupId : snapshot.Assignments
                .Where(a => a.SemesterId == snapshot.Current.Id && snapshot.GroupPrograms[a.GroupId] == p.Id)
                .Select(a => (int?)a.GroupId).SingleOrDefault();
            var started = snapshot.Tests.Any(t => t.SemesterId == snapshot.Current.Id && t.Started && snapshot.GroupPrograms[t.GroupId] == p.Id);
            var reason = started ? "Для группы уже выбраны дисциплины или начата организация проверки" : null;
            return new HodProfileChoiceDto(p.Id, recommended,
                decision is not null ? decision.SelectedGroupId : recommended, decision is not null,
                decision?.Reason, reason is null, reason, EligibleGroups(snapshot, p));
        }).ToArray();
        return new(department.Id, department.Name, plan, choices);
    }

    private async Task<Department> RequireDepartment(CancellationToken ct)
    {
        var departmentId = currentUser?.DepartmentId ?? throw new DocumentAccessException("Не определена кафедра пользователя.");
        return await context.Departments.AsNoTracking().SingleOrDefaultAsync(d => d.Id == departmentId, ct)
            ?? throw new DocumentAccessException("Кафедра пользователя не найдена.");
    }
}
