using System.Globalization;

namespace RejestrCzasu.Domain;

/// <summary>A Miesiąc: year and month (1–12), ordered chronologically.</summary>
public readonly record struct YearMonth : IComparable<YearMonth>
{
    /// <exception cref="ArgumentOutOfRangeException">Year outside 1–9999 or month outside 1–12.</exception>
    public YearMonth(int year, int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        Year = year;
        Month = month;
    }

    public int Year { get; }

    public int Month { get; }

    public int DaysInMonth => DateTime.DaysInMonth(Year, Month);

    public DateOnly FirstDay => new(Year, Month, 1);

    public DateOnly LastDay => new(Year, Month, DaysInMonth);

    public YearMonth Next => Month == 12 ? new(Year + 1, 1) : new(Year, Month + 1);

    public YearMonth Previous => Month == 1 ? new(Year - 1, 12) : new(Year, Month - 1);

    /// <summary>Polish name in the nominative ("Wrzesień 2026").</summary>
    public string Name => string.Create(CultureInfo.InvariantCulture, $"{PolishCalendar.MonthName(Month)} {Year}");

    public static YearMonth From(DateOnly date) => new(date.Year, date.Month);

    public bool Contains(DateOnly date) => date.Year == Year && date.Month == Month;

    public int CompareTo(YearMonth other) =>
        Year != other.Year ? Year.CompareTo(other.Year) : Month.CompareTo(other.Month);

    public static bool operator <(YearMonth left, YearMonth right) => left.CompareTo(right) < 0;

    public static bool operator >(YearMonth left, YearMonth right) => left.CompareTo(right) > 0;

    public static bool operator <=(YearMonth left, YearMonth right) => left.CompareTo(right) <= 0;

    public static bool operator >=(YearMonth left, YearMonth right) => left.CompareTo(right) >= 0;

    /// <summary>ISO form <c>yyyy-MM</c> ("2026-09").</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Year:0000}-{Month:00}");
}
