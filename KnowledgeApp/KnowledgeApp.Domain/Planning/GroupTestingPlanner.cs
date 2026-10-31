namespace KnowledgeApp.Domain.Planning;

public sealed record PlanningSemester(int Id, int Year, int Part)
{
    public int Position => checked(Year * 2 + Part - 1);
}

public sealed record Participation(int GroupId, int SemesterPosition);
public sealed record PlanAssignment(int GroupId, int SemesterId, string Author, bool Locked = false);
public sealed record PlanCell(bool Planned, string? Author, bool Editable, string? RestrictionReason);
public sealed record ProfilePlan(
    IReadOnlyList<PlanAssignment> Assignments,
    IReadOnlyDictionary<(int GroupId, int SemesterId), PlanCell> Cells);

public sealed class PlanningRuleException(string message) : Exception(message);

/// <summary>Чистый расчёт: не читает БД, часы или настройки HTTP.</summary>
public static class GroupTestingPlanner
{
    public static bool HasCompletedTesting(IEnumerable<string?> statuses) => statuses.Any(status =>
        string.Equals(status?.Trim(), "Completed", StringComparison.OrdinalIgnoreCase));

    public static ProfilePlan Calculate(
        IReadOnlyCollection<int> groupIds,
        IEnumerable<Participation> history,
        IReadOnlyList<PlanningSemester> semesters,
        IReadOnlyCollection<PlanAssignment> existing)
    {
        var groups = groupIds.ToHashSet();
        var cells = new Dictionary<(int, int), PlanCell>();
        var result = new List<PlanAssignment>();
        if (groups.Count == 0) return new(result, cells);
        if (semesters.Count == 0) throw new PlanningRuleException("Не задан период планирования");
        var orderedSemesters = semesters.OrderBy(s => s.Position).ToArray();

        var visited = new HashSet<int>();
        var previous = new HashSet<int>();
        int? previousPosition = null;
        void Advance(IEnumerable<int> ids, int position)
        {
            previous = ids.ToHashSet();
            previousPosition = position;
            visited.UnionWith(previous);
            if (visited.Count == groups.Count) visited.Clear();
        }

        // Внутри одного семестра нет искусственного порядка по Id тестирования.
        foreach (var batch in history.Where(h => groups.Contains(h.GroupId)
                     && h.SemesterPosition < orderedSemesters[0].Position)
                     .GroupBy(h => h.SemesterPosition).OrderBy(h => h.Key))
            Advance(batch.Select(h => h.GroupId), batch.Key);

        // Производные автоматические назначения считаются заново. Ручные и назначения
        // с уже существующими тестированиями не меняются без явного действия пользователя.
        var fixedChoices = existing.Where(a => a.Author == "manual" || a.Locked)
            .GroupBy(a => a.SemesterId).ToDictionary(g => g.Key, g => g.ToList());
        var waiting = false;
        foreach (var semester in orderedSemesters)
        {
            fixedChoices.TryGetValue(semester.Id, out var fixedInSemester);
            if (fixedInSemester?.Select(a => a.GroupId).Distinct().Count() > 1)
                throw new PlanningRuleException($"На семестр {semester.Id} уже назначено несколько групп одного профиля");
            var choice = fixedInSemester?.FirstOrDefault();
            var eligible = groups.Where(id => !visited.Contains(id)).ToHashSet();
            if (groups.Count == 1) eligible = groups.ToHashSet();
            else if (groups.Count == 2 && previous.Count == 1)
                eligible.ExceptWith(previous); // Чередование сохраняется и при пропусках семестров.
            else if (previousPosition == semester.Position - 1)
                eligible.ExceptWith(previous);

            if (choice is not null && !choice.Locked
                && (waiting || !eligible.Contains(choice.GroupId)))
                throw new PlanningRuleException($"Ручной выбор группы {choice.GroupId} на семестр {semester.Id} нарушает цикл или зависит от предыдущего выбора. Исправьте связанные ячейки одним пакетом.");

            if (choice is null && !waiting && eligible.Count == 1)
                choice = new PlanAssignment(eligible.Single(), semester.Id, "automatic");

            var forced = !waiting && eligible.Count == 1;
            foreach (var groupId in groups)
            {
                var selected = choice?.GroupId == groupId;
                string? reason = null;
                if (choice?.Locked == true)
                    reason = "Для назначения уже существуют тестирования";
                else if (waiting)
                    reason = "Сначала сохраните выбор в предыдущем семестре";
                else if (forced || choice?.Author == "automatic")
                    reason = selected ? "Назначено автоматическим подбором" : "На этот семестр автоматически выбрана другая группа профиля";
                else if (!eligible.Contains(groupId))
                    reason = previousPosition == semester.Position - 1 && previous.Contains(groupId)
                        ? "Группа не может тестироваться два семестра подряд"
                        : "Группа уже участвовала в текущем цикле";
                cells[(groupId, semester.Id)] = new PlanCell(selected,
                    selected ? choice!.Author : null, reason is null, reason);
            }

            if (choice is not null)
            {
                result.Add(choice);
                Advance([choice.GroupId], semester.Position);
                waiting = false;
            }
            else if (eligible.Count > 1) waiting = true;
        }
        return new(result, cells);
    }
}
