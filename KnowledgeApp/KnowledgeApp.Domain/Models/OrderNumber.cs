using System.Globalization;
using System.Text.RegularExpressions;

namespace KnowledgeApp.Domain.Entities;

public static class OrderNumber
{
    public static long NumericPart(string? number)
    {
        var match = Regex.Match(number?.Trim() ?? "", @"^([0-9]+)(?:-ОД)?$", RegexOptions.IgnoreCase);
        return match.Success && long.TryParse(match.Groups[1].Value, NumberStyles.None,
            CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    public static string Format(long number) => number > 0
        ? number.ToString(CultureInfo.InvariantCulture) + "-ОД"
        : throw new ArgumentOutOfRangeException(nameof(number));
}
