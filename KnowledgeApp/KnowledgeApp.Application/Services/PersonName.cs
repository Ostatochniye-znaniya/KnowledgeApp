namespace KnowledgeApp.Application.Services;

public static class PersonName
{
    public static string Dative(string fullName) => Format(fullName, dative: true);

    public static string Accusative(string fullName) => Format(fullName, dative: false);

    private static string Format(string fullName, bool dative)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || parts.Any(p => !p.All(c => char.IsLetter(c) || c == '-')))
            return fullName;

        var patronymic = parts[2];
        Func<string, string> decline = EndsWith(patronymic, "ич") ? s => DeclineMale(s, dative)
            : EndsWith(patronymic, "на") ? s => DeclineFemale(s, dative)
            : s => s;
        var surname = string.Join('-', parts[0].Split('-').Select(p => p.Length > 1 ? decline(p) : p));

        return $"{char.ToUpper(parts[1][0])}.{char.ToUpper(patronymic[0])}.\u00A0{surname}";
    }

    private static string DeclineMale(string s, bool dative)
    {
        if (EndsWith(s, "ов", "ев", "ёв", "ин", "ын"))
            return s + (dative ? "у" : "а");
        if (EndsWith(s, "ский", "цкий") || (EndsWith(s, "ой", "ый") && s.Length > 4))
            return s[..^2] + (dative ? "ому" : "ого");
        if (EndsWith(s, "ий") && s.Length > 3)
        {
            var before = char.ToLower(s[^3]);
            if ("гкх".Contains(before)) return s[..^2] + (dative ? "ому" : "ого");
            if ("жшчщн".Contains(before)) return s[..^2] + (dative ? "ему" : "его");
        }
        if (EndsWith(s, "й", "ь"))
            return s[..^1] + (dative ? "ю" : "я");
        if (EndsWith(s, "ых", "их"))
            return s;
        if (EndsWith(s, "а", "я"))
            return DeclineSoft(s, dative);
        if ("бвгджзклмнпрстфхцчшщ".Contains(char.ToLower(s[^1])))
            return s + (dative ? "у" : "а");
        return s;
    }

    private static string DeclineFemale(string s, bool dative)
    {
        if (EndsWith(s, "ова", "ева", "ёва", "ина", "ына"))
            return s[..^1] + (dative ? "ой" : "у");
        if (EndsWith(s, "ая") && s.Length > 3)
            return s[..^2] + (dative ? ("жшчщ".Contains(char.ToLower(s[^3])) ? "ей" : "ой") : "ую");
        if (EndsWith(s, "яя"))
            return s[..^2] + (dative ? "ей" : "юю");
        if (EndsWith(s, "а", "я"))
            return DeclineSoft(s, dative);
        return s;
    }

    private static string DeclineSoft(string s, bool dative)
    {
        if (EndsWith(s, "ия"))
            return s[..^1] + (dative ? "и" : "ю");
        if (EndsWith(s, "я"))
            return s[..^1] + (dative ? "е" : "ю");
        return s[..^1] + (dative ? "е" : "у");
    }

    private static bool EndsWith(string value, params string[] endings) =>
        endings.Any(e => value.EndsWith(e, StringComparison.OrdinalIgnoreCase));
}
