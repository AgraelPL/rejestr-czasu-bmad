using System.Globalization;
using RejestrCzasu.Domain;

namespace RejestrCzasu.Features.Zestawienie;

/// <summary>The file name of the Zestawienie (R5): <c>Zestawienie-wrzesień_2026.xlsx</c>, with a prefix <c>ABC_Zestawienie-…</c>.</summary>
public static class SummaryFile
{
    /// <summary>
    /// The name for <paramref name="month"/>; a non-empty <paramref name="prefix"/> goes in front with "_". The prefix of
    /// the Ustawienia konta comes in story 5; until then the endpoint passes <see langword="null"/>.
    /// </summary>
    public static string Name(YearMonth month, string? prefix)
    {
        var name = string.Create(CultureInfo.InvariantCulture,
            $"Zestawienie-{PolishCalendar.MonthName(month.Month).ToLowerInvariant()}_{month.Year:0000}.xlsx");
        return string.IsNullOrEmpty(prefix) ? name : $"{prefix}_{name}";
    }
}
