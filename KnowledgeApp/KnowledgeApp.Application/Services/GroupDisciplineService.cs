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
            throw new UnauthorizedAccessException("Группа не относится к вашей кафедре");

        return await _repository.GetStudyProgramDisciplinesAsync(groupId);
    }

    /// <summary>
    /// Сохраняет выбор и создаёт недостающие тестирования группы за семестр.
    /// Тестирования по сохранённым дисциплинам сохраняют свои ID и данные.
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

        // Для старых данных переиспользуем доступные записи, сохраняя прежний выбор и его ID.
        var lockedIds = (await _repository.GetLockedTestingIdsAsync(resolvedSemesterId, groupId)).ToHashSet();
        var kept = new HashSet<int>();
        var freeSlots = new List<Testing>();
        foreach (var testing in testings.OrderByDescending(t => lockedIds.Contains(t.Id)))
        {
            if (testing.DisciplineId is int current && ids.Contains(current) && kept.Add(current))
                continue;
            if (lockedIds.Contains(testing.Id))
            {
                if (testing.DisciplineId.HasValue)
                    throw new InvalidOperationException(
                        $"Нельзя изменить дисциплину тестирования {testing.Id}: уже начата работа с преподавателями, графиком или отчётом");
                continue;
            }
            freeSlots.Add(testing);
        }

        var toAssign = ids.Where(id => !kept.Contains(id)).ToList();
        // Сначала пустые слоты, чтобы без нужды не перезаписывать заполненные.
        freeSlots = freeSlots
            .OrderBy(t => t.DisciplineId.HasValue)
            .ThenBy(t => t.Id)
            .ToList();

        for (int i = 0; i < freeSlots.Count; i++)
            freeSlots[i].DisciplineId = i < toAssign.Count ? toAssign[i] : null;

        if (toAssign.Count > freeSlots.Count)
            await _repository.AddTestingsAsync(groupId, resolvedSemesterId, toAssign.Skip(freeSlots.Count));
    }
}
