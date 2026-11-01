using RejestrCzasu.Domain;

namespace RejestrCzasu.Tests.Domain;

/// <summary>Polish calendar (AD-6): names, Monday weeks, "today" in Warsaw; no database.</summary>
public sealed class PolishCalendarTests
{
    [Fact]
    public void DayName_OfTuesday_AndWeekStartsOnMonday()
    {
        var date = new DateOnly(2026, 9, 1);

        Assert.Equal("Wtorek", PolishCalendar.DayName(date));
        Assert.Equal(new DateOnly(2026, 8, 31), PolishCalendar.WeekStart(date));
    }

    [Fact]
    public void WeekStart_OfSunday_IsThePreviousMonday()
    {
        Assert.Equal(new DateOnly(2026, 8, 31), PolishCalendar.WeekStart(new DateOnly(2026, 9, 6)));
    }

    [Fact]
    public void WeekStart_OfMonday_IsTheSameDay()
    {
        Assert.Equal(new DateOnly(2026, 9, 7), PolishCalendar.WeekStart(new DateOnly(2026, 9, 7)));
    }

    [Fact]
    public void Names_AreNominativeAndCapitalised()
    {
        Assert.Equal("Środa", PolishCalendar.DayName(DayOfWeek.Wednesday));
        Assert.Equal("Niedziela", PolishCalendar.DayName(DayOfWeek.Sunday));
        Assert.Equal("Wrzesień", PolishCalendar.MonthName(9));
        Assert.Equal("Styczeń", PolishCalendar.MonthName(1));
        Assert.Equal("Grudzień", PolishCalendar.MonthName(12));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolishCalendar.MonthName(13));
    }

    [Fact]
    public void Today_IsTheDateInWarsaw()
    {
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2027, 1, 1), PolishCalendar.Today(clock));
    }

    [Fact]
    public void ToPolishTime_AppliesSummerAndWinterTime()
    {
        Assert.Equal(new DateTime(2026, 7, 1, 12, 0, 0), PolishCalendar.ToPolishTime(new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(new DateTime(2026, 1, 1, 11, 0, 0), PolishCalendar.ToPolishTime(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Unspecified)));
    }

    [Fact]
    public void ToPolishTime_RejectsLocalTime()
    {
        Assert.Throws<ArgumentException>(() => PolishCalendar.ToPolishTime(new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Local)));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
