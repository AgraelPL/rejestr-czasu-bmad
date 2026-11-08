using System.Globalization;
using RejestrCzasu.Domain;

namespace RejestrCzasu.Reporting;

/// <summary>A Wpis of a Miesiąc as <see cref="MonthEntriesQuery"/> reads it: ids for merging, code and name for order and display.</summary>
public sealed record MonthEntry(int Id, DateOnly Date, int ProjectId, string ProjectCode,
    int WorkTypeId, string WorkTypeName, string Description, int Minutes);

/// <summary>A day with Wpisy: its sum of Czas and its Wpisy in the order of <see cref="MonthReport.Entries"/>.</summary>
public sealed record MonthDay(DateOnly Date, int Minutes, IReadOnlyList<MonthEntry> Entries);

/// <summary>
/// A week (Monday–Sunday) of the Miesiąc with Wpisy: number counted in the calendar of the Miesiąc (the week with
/// day 1 is 1), first and last day only from days of the Miesiąc, its sum and its days with Wpisy.
/// </summary>
public sealed record MonthWeek(int Number, DateOnly First, DateOnly Last, int Minutes, IReadOnlyList<MonthDay> Days);

/// <summary>
/// A Wiersz zestawienia (R11): the Wpisy of one day, Projekt and Typ pracy merged, descriptions ascending by Id.
/// <see cref="DayMinutes"/> only in the first row of a day, <see cref="WeekMinutes"/> only in the first row of a week.
/// </summary>
public sealed record SummaryRow(DateOnly Date, string ProjectCode, string WorkTypeName,
    IReadOnlyList<string> Descriptions, int Minutes, int? DayMinutes, int? WeekMinutes);

/// <summary>
/// The one source of the sums of a Miesiąc (AD-5, R2): the Widok miesiąca and the Zestawienie read the days, weeks,
/// the order of Wpisy, the Wiersze zestawienia and the sum of the Miesiąc only from here. Built by the pure
/// <see cref="Build"/>, without the database.
/// </summary>
public sealed record MonthReport(YearMonth Month, IReadOnlyList<MonthEntry> Entries,
    IReadOnlyList<MonthWeek> Weeks, IReadOnlyList<SummaryRow> Rows, int Minutes)
{
    private static readonly StringComparer Polish = StringComparer.Create(CultureInfo.GetCultureInfo("pl-PL"), ignoreCase: false);

    /// <summary>
    /// Order of Wpisy: day, code of the Projekt, name of the Typ pracy (both compared in <c>pl-PL</c>), then Id.
    /// </summary>
    private static readonly Comparison<MonthEntry> Order = (a, b) =>
    {
        var cmp = a.Date.CompareTo(b.Date);
        if (cmp == 0)
        {
            cmp = Polish.Compare(a.ProjectCode, b.ProjectCode);
        }
        if (cmp == 0)
        {
            cmp = Polish.Compare(a.WorkTypeName, b.WorkTypeName);
        }
        return cmp != 0 ? cmp : a.Id.CompareTo(b.Id);
    };

    /// <summary>
    /// Builds the report of <paramref name="month"/> from its Wpisy in any order. Only weeks and days with Wpisy;
    /// Wiersze zestawienia merge the Wpisy of a day with the same <c>ProjectId</c> and <c>WorkTypeId</c>.
    /// </summary>
    /// <exception cref="ArgumentException">A Wpis lies outside <paramref name="month"/>.</exception>
    public static MonthReport Build(YearMonth month, IEnumerable<MonthEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var ordered = entries.ToList();
        if (ordered.FirstOrDefault(e => !month.Contains(e.Date)) is { } outside)
        {
            throw new ArgumentException(
                $"Wpis {outside.Id} z dnia {outside.Date:dd.MM.yyyy} nie należy do Miesiąca {month}.", nameof(entries));
        }
        ordered.Sort(Order);

        var weeks = ordered
            .GroupBy(e => WeekFirst(e.Date))
            .Select(week =>
            {
                var days = week
                    .GroupBy(e => e.Date)
                    .Select(day => new MonthDay(day.Key, day.Sum(e => e.Minutes), day.ToList()))
                    .ToList();
                var (number, first, last) = WeekOf(week.Key);
                return new MonthWeek(number, first, last, days.Sum(d => d.Minutes), days);
            })
            .ToList();

        var rows = new List<SummaryRow>();
        foreach (var week in weeks)
        {
            var firstOfWeek = true;
            foreach (var day in week.Days)
            {
                var firstOfDay = true;
                foreach (var merged in day.Entries.GroupBy(e => (e.ProjectId, e.WorkTypeId)))
                {
                    var head = merged.First();
                    rows.Add(new SummaryRow(
                        day.Date,
                        head.ProjectCode,
                        head.WorkTypeName,
                        merged.OrderBy(e => e.Id).Select(e => e.Description).ToList(),
                        merged.Sum(e => e.Minutes),
                        firstOfDay ? day.Minutes : null,
                        firstOfWeek ? week.Minutes : null));
                    firstOfDay = false;
                    firstOfWeek = false;
                }
            }
        }

        return new MonthReport(month, ordered, weeks, rows, weeks.Sum(w => w.Minutes));
    }

    /// <summary>
    /// Header of the week of <paramref name="date"/> in its Miesiąc, e.g. „Tydzień 1 (01.09–06.09)”: the range only
    /// from days of the Miesiąc.
    /// </summary>
    public static string WeekLabel(DateOnly date)
    {
        var (number, first, last) = WeekOf(date);
        return string.Create(CultureInfo.InvariantCulture, $"Tydzień {number} ({first:dd.MM}–{last:dd.MM})");
    }

    /// <summary>Sum of Czas of the day <paramref name="date"/>; 0 for a day without Wpisy.</summary>
    public int DayMinutes(DateOnly date) =>
        Weeks.SelectMany(w => w.Days).FirstOrDefault(d => d.Date == date)?.Minutes ?? 0;

    /// <summary>Sum of Czas of the week (only days of the Miesiąc) that contains <paramref name="date"/>; 0 without Wpisy.</summary>
    public int WeekMinutes(DateOnly date) =>
        Weeks.FirstOrDefault(w => w.First <= date && date <= w.Last)?.Minutes ?? 0;

    /// <summary>
    /// First day of the week of <paramref name="date"/> within its Miesiąc: the Monday, or day 1 when the week begins in
    /// the previous Miesiąc. The key of a week (<see cref="MonthWeek.First"/>).
    /// </summary>
    public static DateOnly WeekFirst(DateOnly date)
    {
        var monday = PolishCalendar.WeekStart(date);
        var firstDay = YearMonth.From(date).FirstDay;
        return monday < firstDay ? firstDay : monday;
    }

    // Number in the Miesiąc (the week with day 1 is 1) and range only from days of the Miesiąc, of the week of a date.
    private static (int Number, DateOnly First, DateOnly Last) WeekOf(DateOnly date)
    {
        var month = YearMonth.From(date);
        var monday = PolishCalendar.WeekStart(date);
        var number = (monday.DayNumber - PolishCalendar.WeekStart(month.FirstDay).DayNumber) / 7 + 1;
        var sunday = monday.AddDays(6);
        var last = sunday > month.LastDay ? month.LastDay : sunday;
        return (number, WeekFirst(date), last);
    }
}
