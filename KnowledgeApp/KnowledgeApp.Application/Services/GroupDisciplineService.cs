using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Infrastructure.Repositories;
using KnowledgeApp.Domain.Planning;
using Microsoft.Extensions.Options;

namespace KnowledgeApp.Application.Services;

/// <summary>
/// Выбор дисциплин для групп заведующим кафедрой (ТЗ, п. 2.4).
/// Заведующий видит и меняет только группы своей кафедры (кафедра — из текущего пользователя).
/// Ошибки: KeyNotFoundException — нет группы/семестра/кафедры, ArgumentException — неверный запрос,
/// UnauthorizedAccessException — кафедра пользователя не определена или группа чужой кафедры,
/// InvalidOperationException — изменение невозможно из-за текущих данных.
/// </summary>
public class GroupDisciplineService
{
    /// <summary>По ТЗ: для каждой выбранной группы определяются две дисциплины.</summary>
    public const int MaxDisciplinesPerGroup = 2;

    private readonly GroupDisciplineRepository _repository;
    private readonly ICurrentUserProvider _currentUser;
    private readonly GroupsTestingOptions _options;
    private readonly TimeProvider _clock;

    public GroupDisciplineService(GroupDisciplineRepository repository, ICurrentUserProvider currentUser,
        IOptions<GroupsTestingOptions>? options = null, TimeProvider? clock = null)
    {
        _repository = repository;
        _currentUser = currentUser;
        _options = options?.Value ?? new GroupsTestingOptions();
        _clock = clock ?? TimeProvider.System;
    }

    private int CurrentDepartmentId =>
        _currentUser.DepartmentId ?? throw new UnauthorizedAccessException("Не определена кафедра пользователя");

    /// <summary>Кафедра текущего пользователя (для подписи на странице).</summary>
    public async Task<(int Id, string Name)> GetCurrentDepartmentAsync()
    {
        int departmentId = CurrentDepartmentId;
        string name = await _repository.GetDepartmentNameAsync(departmentId)
            ?? throw new KeyNotFoundException($"Кафедра с id {departmentId} не найдена");
        return (departmentId, name);
    }

    /// <summary>Переданный семестр (с проверкой) или текущий по календарю планирования.</summary>
    public async Task<int> ResolveSemesterIdAsync(int? semesterId)
    {
        if (semesterId.HasValue)
        {
            if (!await _repository.SemesterExistsAsync(semesterId.Value))
                throw new KeyNotFoundException($"Семестр с id {semesterId.Value} не найден");
            return semesterId.Value;
        }

        var now = TimeZoneInfo.ConvertTime(_clock.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(_options.TimeZoneId));
        return PlanningCalendar.GetCurrent(await _repository.GetCalendarAsync(), DateOnly.FromDateTime(now.Date),
            _options.SpringStartMonth, _options.AutumnStartMonth).Id;
    }

    public async Task<List<GroupDisciplinesModel>> GetGroupsAsync(int? semesterId)
    {
        int departmentId = CurrentDepartmentId;
        int resolvedSemesterId = await ResolveSemesterIdAsync(semesterId);
        return await _repository.GetGroupsAsync(resolvedSemesterId, departmentId);
    }

    /// <summary>Дисциплины программы группы; группа должна относиться к кафедре пользователя.</summary>
    public async Task<List<DisciplineModel>> GetAvailableDisciplinesAsync(int groupId)
    {
        int departmentId = CurrentDepartmentId;
        var (found, groupDepartmentId) = await _repository.GetGroupDepartmentAsync(groupId);
        if (!found)
            throw new KeyNotFoundException($"Учебная группа с id {groupId} не найдена");
        if (groupDepartmentId != departmentId)
            throw new UnauthorizedAccessException($"Группа не относится к вашей кафедре; departmentId = {departmentId}; CurrentDepartmentId = {CurrentDepartmentId}; groupDepartmentId = {groupDepartmentId}");

        return await _repository.GetStudyProgramDisciplinesAsync(groupId);
    }

    /// <summary>
    /// Заменяет выбор дисциплин группы целиком: записывает их в тестирования группы за семестр.
    /// Для выбранных дисциплин создаёт недостающие отчёты и связывает их с тестированиями.
    /// </summary>
    public Task SetDisciplinesAsync(int groupId, IReadOnlyCollection<int> disciplineIds, int? semesterId) =>
        _repository.WithPlanLock(() => SetDisciplinesCoreAsync(groupId, disciplineIds, semesterId));

    private async Task SetDisciplinesCoreAsync(int groupId, IReadOnlyCollection<int> disciplineIds, int? semesterId)
    {
        var ids = disciplineIds.Distinct().ToList();
        if (ids.Count != disciplineIds.Count)
            throw new ArgumentException("Дисциплины в списке не должны повторяться");
        if (ids.Count > MaxDisciplinesPerGroup)
            throw new ArgumentException($"Для группы можно выбрать не больше {MaxDisciplinesPerGroup} дисциплин");

        int resolvedSemesterId = await ResolveSemesterIdAsync(semesterId);

        var availableIds = (await GetAvailableDisciplinesAsync(groupId)).Select(d => d.Id).ToHashSet();
        if (!await _repository.IsSelectedAsync(groupId, resolvedSemesterId))
            throw new InvalidOperationException("Сначала подтвердите участие группы на странице выбора групп.");
        List<Testing> testings = await _repository.GetGroupTestingsAsync(groupId, resolvedSemesterId);
        // Старые назначения можно сохранить, даже если перечень программы ещё не перенесён.
        var existingIds = testings.Where(t => t.DisciplineId.HasValue).Select(t => t.DisciplineId!.Value).ToHashSet();
        var foreignIds = ids.Where(id => !availableIds.Contains(id) && !existingIds.Contains(id)).ToList();
        if (foreignIds.Count > 0)
            throw new ArgumentException(
                $"Дисциплины не входят в учебную программу профиля группы: {string.Join(", ", foreignIds)}");

        if (testings.Count == 0)
            throw new ArgumentException(
                "Группа не выбрана для проверки в этом семестре: для неё нет тестирований");

        // Тестирования, где выбранная дисциплина уже стоит, остаются как есть
        // (у них могут быть дата, преподаватель, отчёт). Остальные — свободные слоты.
        var kept = new HashSet<int>();
        var freeSlots = new List<Testing>();
        foreach (var testing in testings)
        {
            if (testing.DisciplineId is int current && ids.Contains(current) && kept.Add(current))
                continue;
            freeSlots.Add(testing);
        }

        var toAssign = ids.Where(id => !kept.Contains(id)).ToList();
        if (toAssign.Count > freeSlots.Count)
            throw new ArgumentException(
                $"Для группы в семестре создано только {testings.Count} тестирований — нельзя выбрать {ids.Count} дисциплин");

        // Сначала пустые слоты, чтобы без нужды не перезаписывать заполненные.
        freeSlots = freeSlots
            .OrderBy(t => t.DisciplineId.HasValue)
            .ThenBy(t => t.Id)
            .ToList();

        var changes = new List<(Testing Testing, int? NewDisciplineId)>();
        for (int i = 0; i < freeSlots.Count; i++)
        {
            int? newDisciplineId = i < toAssign.Count ? toAssign[i] : null;
            if (freeSlots[i].DisciplineId != newDisciplineId)
                changes.Add((freeSlots[i], newDisciplineId));
        }

        var lockedIds = await _repository.GetLockedTestingIdsAsync(resolvedSemesterId, groupId);
        Testing? locked = changes.Select(c => c.Testing).FirstOrDefault(t => lockedIds.Contains(t.Id));
        if (locked != null)
            throw new InvalidOperationException(
                $"Нельзя изменить дисциплину тестирования {locked.Id}: уже начата работа с преподавателями, графиком или отчётом");

        var discardedReportIds = new List<int>();
        foreach (var (testing, newDisciplineId) in changes)
        {
            if (testing.ReportId is int previousReportId)
                discardedReportIds.Add(previousReportId);
            testing.DisciplineId = newDisciplineId;
            testing.ReportId = null;
            testing.Report = null;
        }

        foreach (var testing in testings)
            if (testing.DisciplineId is int disciplineId && testing.ReportId is null)
                testing.Report = new Report { DisciplineId = disciplineId };

        if (discardedReportIds.Count > 0)
            await _repository.RemoveUnusedEmptyReportsAsync(discardedReportIds);
    }
}
