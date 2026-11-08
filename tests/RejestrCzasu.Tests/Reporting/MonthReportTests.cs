using RejestrCzasu.Domain;
using RejestrCzasu.Reporting;

namespace RejestrCzasu.Tests.Reporting;

/// <summary>The one source of the sums of a Miesiąc (AD-5, R2): <see cref="MonthReport.Build"/> without the database.</summary>
public sealed class MonthReportTests
{
    private static readonly YearMonth September = new(2026, 9);
    private static readonly YearMonth November = new(2026, 11);

    // The order of MonthReport.Entries for SeptemberShape: day, Projekt code, Typ pracy name (pl-PL), Id.
    private static readonly int[] SeptemberOrder =
        [1, 2, 3, 4, 9, 5, 6, 8, 7, 10, 11, 12, 13, 18, 14, 16, 17, 15, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31];

    [Fact]
    public void September_WeeksDaysAndSum()
    {
        var report = MonthReport.Build(September, SeptemberShape.Shuffled());

        Assert.Equal(September, report.Month);
        Assert.Equal([1, 2, 3, 4, 5], report.Weeks.Select(w => w.Number));
        Assert.Equal([30 * 60, 16 * 60, 18 * 60, 25 * 60, 14 * 60], report.Weeks.Select(w => w.Minutes));
        Assert.Equal(103 * 60, report.Minutes);
        Assert.Equal((new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 6)), (report.Weeks[0].First, report.Weeks[0].Last));
        Assert.Equal((new DateOnly(2026, 9, 28), new DateOnly(2026, 9, 30)), (report.Weeks[4].First, report.Weeks[4].Last));
        Assert.Equal([1, 2, 3, 4], report.Weeks[0].Days.Select(d => d.Date.Day));
        Assert.Equal([480, 480, 480, 360], report.Weeks[0].Days.Select(d => d.Minutes));
        Assert.Equal([21, 22, 23, 26], report.Weeks[3].Days.Select(d => d.Date.Day));
        Assert.Equal(15, report.Weeks.Sum(w => w.Days.Count));
        Assert.Equal(SeptemberOrder, report.Entries.Select(e => e.Id));
        Assert.Equal(SeptemberOrder, report.Weeks.SelectMany(w => w.Days).SelectMany(d => d.Entries).Select(e => e.Id));
    }

    [Fact]
    public void September_SummaryRows_MergedByDayProjectAndWorkType_SumsOnlyInFirstRows()
    {
        var report = MonthReport.Build(September, SeptemberShape.Shuffled());

        Assert.Equal(28, report.Rows.Count);
        Assert.Equal(report.Minutes, report.Rows.Sum(r => r.Minutes));
        Assert.Equal(report.Minutes, report.Rows.Sum(r => r.WeekMinutes ?? 0));
        Assert.Equal(report.Minutes, report.Rows.Sum(r => r.DayMinutes ?? 0));

        var second = report.Rows.Where(r => r.Date.Day == 2).ToList();
        Assert.Equal(
            [("AAA", "Development", 240, (int?)480), ("CCC", "Analiza", 240, null)],
            second.Select(r => (r.ProjectCode, r.WorkTypeName, r.Minutes, r.DayMinutes)));
        Assert.Equal(["Zadanie 4", "Zadanie 9"], second[0].Descriptions);

        var fifteenth = report.Rows.Where(r => r.Date.Day == 15).ToList();
        Assert.Equal(
            [("AAA", "Serwis", 60), ("BBB", "Analiza", 240), ("BBB", "Testy", 60)],
            fifteenth.Select(r => (r.ProjectCode, r.WorkTypeName, r.Minutes)));
        Assert.Equal(["Zadanie 14", "Zadanie 16", "Zadanie 17"], fifteenth[1].Descriptions);

        // The same Projekt and Typ pracy on different days are never merged.
        Assert.Equal([1, 2, 14, 21, 28],
            report.Rows.Where(r => (r.ProjectCode, r.WorkTypeName) == ("AAA", "Development")).Select(r => r.Date.Day));

        // Suma dnia only in the first row of a day, Suma tygodnia only in the first row of a week.
        Assert.Equal(15, report.Rows.Count(r => r.DayMinutes is not null));
        Assert.Equal(
            [(1, 30 * 60), (7, 16 * 60), (14, 18 * 60), (21, 25 * 60), (28, 14 * 60)],
            report.Rows.Where(r => r.WeekMinutes is not null).Select(r => (r.Date.Day, r.WeekMinutes!.Value)));
        Assert.All(report.Rows.GroupBy(r => r.Date), day =>
        {
            Assert.NotNull(day.First().DayMinutes);
            Assert.All(day.Skip(1), r => Assert.Null(r.DayMinutes));
        });
    }

    [Fact]
    public void DayAndWeekMinutes_FromTheReport_ZeroWithoutWpisy()
    {
        var report = MonthReport.Build(September, SeptemberShape.Shuffled());

        Assert.Equal(480, report.DayMinutes(new DateOnly(2026, 9, 2)));
        Assert.Equal(0, report.DayMinutes(new DateOnly(2026, 9, 5)));
        Assert.Equal(25 * 60, report.WeekMinutes(new DateOnly(2026, 9, 21)));
        Assert.Equal(25 * 60, report.WeekMinutes(new DateOnly(2026, 9, 27)));
        Assert.Equal(30 * 60, report.WeekMinutes(new DateOnly(2026, 9, 6)));
    }

    [Fact]
    public void WeekLabel_RangeOnlyFromDaysOfTheMonth()
    {
        Assert.Equal("Tydzień 1 (01.09–06.09)", MonthReport.WeekLabel(new DateOnly(2026, 9, 1)));
        Assert.Equal("Tydzień 1 (01.09–06.09)", MonthReport.WeekLabel(new DateOnly(2026, 9, 6)));
        Assert.Equal("Tydzień 4 (21.09–27.09)", MonthReport.WeekLabel(new DateOnly(2026, 9, 26)));
        Assert.Equal("Tydzień 5 (28.09–30.09)", MonthReport.WeekLabel(new DateOnly(2026, 9, 28)));
        Assert.Equal("Tydzień 1 (01.11–01.11)", MonthReport.WeekLabel(new DateOnly(2026, 11, 1)));
        Assert.Equal("Tydzień 2 (02.11–08.11)", MonthReport.WeekLabel(new DateOnly(2026, 11, 2)));
    }

    [Fact]
    public void EmptyMonth_NoWeeksNoRows_SumZero()
    {
        var report = MonthReport.Build(September, []);

        Assert.Empty(report.Entries);
        Assert.Empty(report.Weeks);
        Assert.Empty(report.Rows);
        Assert.Equal(0, report.Minutes);
        Assert.Equal(0, report.WeekMinutes(new DateOnly(2026, 9, 1)));
    }

    [Theory]
    [InlineData(2026, 8, 31)]
    [InlineData(2026, 10, 1)]
    public void EntryOutsideTheMonth_Throws(int year, int month, int day)
    {
        var entries = SeptemberShape.Entries().Append(Entry(99, new DateOnly(year, month, day), "AAA", "Testy", 60));

        Assert.Throws<ArgumentException>(() => MonthReport.Build(September, entries));
    }

    [Fact]
    public void FirstDayOnSunday_IsWeek1Alone_MondayIsWeek2()
    {
        var report = MonthReport.Build(November,
            [Entry(1, new DateOnly(2026, 11, 2), "AAA", "Testy", 60), Entry(2, new DateOnly(2026, 11, 1), "AAA", "Testy", 30)]);

        Assert.Equal(
            [(1, new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 1), 30), (2, new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 8), 60)],
            report.Weeks.Select(w => (w.Number, w.First, w.Last, w.Minutes)));
    }

    [Fact]
    public void WeekWithoutWpisy_HasNoGroup_NumbersSkip()
    {
        var report = MonthReport.Build(September,
            [Entry(1, new DateOnly(2026, 9, 2), "AAA", "Testy", 60), Entry(2, new DateOnly(2026, 9, 16), "AAA", "Testy", 60)]);

        Assert.Equal([1, 3], report.Weeks.Select(w => w.Number));
        Assert.Equal([60, 60], report.Rows.Select(r => r.WeekMinutes ?? 0));
    }

    [Fact]
    public void Ties_ProjectCodeThenWorkTypeNameInPolish_ThenId()
    {
        var day = new DateOnly(2026, 9, 10);
        MonthEntry[] entries =
        [
            Entry(6, day, "ABD", "Serwis", 15),
            Entry(3, day, "ABC", "Testy", 15),
            Entry(5, day, "ABC", "Serwis", 15),
            Entry(1, day, "ABC", "Środowisko", 15),
            Entry(4, day, "ABC", "Serwis", 15),
            Entry(2, day, "ABD", "Testy", 15),
        ];

        var report = MonthReport.Build(September, entries);

        Assert.Equal(
            [("ABC", "Serwis", 4), ("ABC", "Serwis", 5), ("ABC", "Środowisko", 1), ("ABC", "Testy", 3), ("ABD", "Serwis", 6), ("ABD", "Testy", 2)],
            report.Entries.Select(e => (e.ProjectCode, e.WorkTypeName, e.Id)));
    }

    // Projekt and Typ pracy ids follow the names, so the same code and name merge.
    private static MonthEntry Entry(int id, DateOnly date, string project, string workType, int minutes) =>
        new(id, date, project.GetHashCode(StringComparison.Ordinal), project,
            workType.GetHashCode(StringComparison.Ordinal), workType, $"Zadanie {id}", minutes);
}
