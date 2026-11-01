namespace RejestrCzasu.Domain;

/// <summary>
/// Polish calendar rules (AD-6): names written in code (not from ICU), weeks start on Monday,
/// "today" and displayed times are in Europe/Warsaw, never in the server's zone or culture.
/// </summary>
public static class PolishCalendar
{
    private static readonly string[] DayNames =
        ["Niedziela", "Poniedziałek", "Wtorek", "Środa", "Czwartek", "Piątek", "Sobota"];

    private static readonly string[] MonthNames =
        ["Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec",
         "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień"];

    /// <summary>The Europe/Warsaw time zone.</summary>
    public static TimeZoneInfo TimeZone => WarsawZone.Value;

    // Loaded on first use, so names keep working even if the zone cannot be resolved.
    private static readonly Lazy<TimeZoneInfo> WarsawZone = new(FindWarsaw);

    /// <summary>Day name in the nominative, capitalised ("Środa").</summary>
    public static string DayName(DayOfWeek day)
    {
        if (day is < DayOfWeek.Sunday or > DayOfWeek.Saturday)
        {
            throw new ArgumentOutOfRangeException(nameof(day), day, "Nieznany dzień tygodnia.");
        }

        return DayNames[(int)day];
    }

    /// <summary>Day name of a date in the nominative, capitalised ("Wtorek").</summary>
    public static string DayName(DateOnly date) => DayName(date.DayOfWeek);

    /// <summary>Month name (1–12) in the nominative, capitalised ("Wrzesień").</summary>
    public static string MonthName(int month)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        return MonthNames[month - 1];
    }

    /// <summary>Monday of the week that contains the date.</summary>
    public static DateOnly WeekStart(DateOnly date)
    {
        var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-daysSinceMonday);
    }

    /// <summary>Today's date in Poland.</summary>
    public static DateOnly Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return DateOnly.FromDateTime(ToPolishTime(timeProvider.GetUtcNow().UtcDateTime));
    }

    /// <summary>
    /// Polish time of a UTC timestamp, for display. <see cref="DateTimeKind.Unspecified"/> is read as UTC
    /// (timestamps read from the database).
    /// </summary>
    /// <exception cref="ArgumentException">The value is local time of the machine.</exception>
    public static DateTime ToPolishTime(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Local)
        {
            throw new ArgumentException("Oczekiwano czasu UTC, nie czasu lokalnego serwera.", nameof(utc));
        }

        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), TimeZone);
    }

    private static TimeZoneInfo FindWarsaw()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows without ICU knows only its own id of the same zone.
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
        }
    }
}
