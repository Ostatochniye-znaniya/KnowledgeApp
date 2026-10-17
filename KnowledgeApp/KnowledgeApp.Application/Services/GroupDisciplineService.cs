using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Infrastructure.Repositories;

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

    public GroupDisciplineService(GroupDisciplineRepository repository, ICurrentUserProvider currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
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

    /// <summary>Переданный семестр (с проверкой) или самый поздний, если не передан.</summary>
    public async Task<int> ResolveSemesterIdAsync(int? semesterId)
    {
        if (semesterId.HasValue)
        {
            if (!await _repository.SemesterExistsAsync(semesterId.Value))
                throw new KeyNotFoundException($"Семестр с id {semesterId.Value} не найден");
            return semesterId.Value;
        }

        return await _repository.GetLatestSemesterIdAsync()
            ?? throw new KeyNotFoundException("В базе нет ни одного семестра");
    }

    public async Task<List<GroupDisciplinesModel>> GetGroupsAsync(int? semesterId)
    {
        int departmentId = CurrentDepartmentId;
        int resolvedSemesterId = await ResolveSemesterIdAsync(semesterId);
        return await _repository.GetGroupsAsync(resolvedSemesterId, departmentId);
    }

    /// <summary>Дисциплины кафедры группы; группа должна относиться к кафедре пользователя.</summary>
    public async Task<List<DisciplineModel>> GetAvailableDisciplinesAsync(int groupId)
    {
        int departmentId = CurrentDepartmentId;
        var (found, groupDepartmentId) = await _repository.GetGroupDepartmentAsync(groupId);
        if (!found)
            throw new KeyNotFoundException($"Учебная группа с id {groupId} не найдена");
        if (groupDepartmentId != departmentId)
            throw new UnauthorizedAccessException("Группа не относится к вашей кафедре");

        return await _repository.GetDepartmentDisciplinesAsync(departmentId);
    }

    /// <summary>
    /// Заменяет выбор дисциплин группы целиком: записывает их в тестирования группы за семестр.
    /// Тестирования, где дисциплина уже стоит и остаётся выбранной, не трогаются;
    /// новые дисциплины занимают сначала пустые тестирования; у убранных дисциплина обнуляется.
    /// </summary>
    public async Task SetDisciplinesAsync(int groupId, IReadOnlyCollection<int> disciplineIds, int? semesterId)
    {
        var ids = disciplineIds.Distinct().ToList();
        if (ids.Count != disciplineIds.Count)
            throw new ArgumentException("Дисциплины в списке не должны повторяться");
        if (ids.Count > MaxDisciplinesPerGroup)
            throw new ArgumentException($"Для группы можно выбрать не больше {MaxDisciplinesPerGroup} дисциплин");

        int resolvedSemesterId = await ResolveSemesterIdAsync(semesterId);

        var availableIds = (await GetAvailableDisciplinesAsync(groupId)).Select(d => d.Id).ToHashSet();
        var foreignIds = ids.Where(id => !availableIds.Contains(id)).ToList();
        if (foreignIds.Count > 0)
            throw new ArgumentException(
                $"Дисциплины не относятся к кафедре группы: {string.Join(", ", foreignIds)}");

        List<Testing> testings = await _repository.GetGroupTestingsAsync(groupId, resolvedSemesterId);
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

        Testing? withReport = changes.Select(c => c.Testing).FirstOrDefault(t => t.ReportId != null);
        if (withReport != null)
            throw new InvalidOperationException(
                $"Нельзя изменить дисциплину тестирования {withReport.Id}: по нему уже загружен отчёт");

        foreach (var (testing, newDisciplineId) in changes)
            testing.DisciplineId = newDisciplineId;

        if (changes.Count > 0)
            await _repository.SaveChangesAsync();
    }
}
