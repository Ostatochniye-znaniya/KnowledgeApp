namespace KnowledgeApp.Domain.Planning;

public static class PlanningCalendar
{
    public static PlanningSemester GetCurrent(IEnumerable<PlanningSemester> available, DateOnly today,
        int springStartMonth = 2, int autumnStartMonth = 8)
    {
        var position = CurrentPosition(today, springStartMonth, autumnStartMonth);
        var matches = available.Where(s => s.Part is 1 or 2 && s.Position == position).ToArray();
        if (matches.Length != 1) throw new PlanningRuleException("В справочнике должен быть ровно один текущий семестр.");
        return matches[0];
    }

    private static int CurrentPosition(DateOnly today, int springStartMonth, int autumnStartMonth)
    {
        if (springStartMonth is < 1 or > 12 || autumnStartMonth is < 1 or > 12 || springStartMonth >= autumnStartMonth)
            throw new PlanningRuleException("Некорректные границы семестров");
        var part = today.Month >= springStartMonth && today.Month < autumnStartMonth ? 1 : 2;
        var year = today.Month < springStartMonth ? today.Year - 1 : today.Year;
        return year * 2 + part - 1;
    }

    public static IReadOnlyList<PlanningSemester> GetWindow(IEnumerable<PlanningSemester> available,
        DateOnly today, int springStartMonth = 2, int autumnStartMonth = 8)
    {
        var position = CurrentPosition(today, springStartMonth, autumnStartMonth);
        var lookup = available.Where(s => s.Part is 1 or 2).ToLookup(s => s.Position);
        var result = new List<PlanningSemester>();
        for (var offset = -8; offset <= 2; offset++)
        {
            var key = position + offset;
            var matches = lookup[key].ToList();
            if (matches.Count != 1)
                throw new PlanningRuleException($"В справочнике нужен ровно один семестр: {key / 2}, часть {key % 2 + 1}. Заполните календарь перед подбором.");
            result.Add(matches[0]);
        }
        return result;
    }
}
