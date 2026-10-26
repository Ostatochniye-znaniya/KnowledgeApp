namespace KnowledgeApp.Domain.Planning;

public static class PlanningCalendar
{
    public static IReadOnlyList<PlanningSemester> GetWindow(IEnumerable<PlanningSemester> available,
        DateOnly today, int springStartMonth = 2, int autumnStartMonth = 8)
    {
        if (springStartMonth is < 1 or > 12 || autumnStartMonth is < 1 or > 12
            || springStartMonth >= autumnStartMonth)
            throw new PlanningRuleException("Некорректные границы семестров");
        var part = today.Month >= springStartMonth && today.Month < autumnStartMonth ? 1 : 2;
        var year = today.Month < springStartMonth ? today.Year - 1 : today.Year;
        var position = year * 2 + part - 1;
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
