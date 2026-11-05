using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Domain;
using RejestrCzasu.Features.Miesiac;
using RejestrCzasu.Features.Pomoc;
using RejestrCzasu.Reporting;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Reporting;
using RejestrCzasu.Tests.Shared;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Widok miesiąca (R2, R17, AD-5, AD-9) in bUnit with <see cref="MonthEntriesQuery"/> on the test database and a
/// clock fixed in September 2026.
/// </summary>
public sealed class MonthPageTests : BunitContext, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);

    private readonly PostgresFixture _postgres;
    private int _user;
    private int _otherUser;

    public MonthPageTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        this.AddAppUi();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AppErrorBoundary>>(new CapturingLogger<AppErrorBoundary>());
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        Services.AddScoped(_ => new MonthEntriesQuery(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped(_ => _postgres.CreateLoggedInCurrentUser(_user));
        AddAuthorization().SetAuthorized("ekran-miesiac");
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _postgres.ResetAsync();
        _user = await _postgres.CreateUserAsync("ekran-miesiac");
        _otherUser = await _postgres.CreateUserAsync("ekran-miesiac-b");
    }

    [Fact]
    public async Task September_WeeksDaysSumsAndRowsFromMonthReport()
    {
        await SeedSeptemberAsync();
        var expected = MonthReport.Build(new YearMonth(2026, 9), SeptemberShape.Entries());

        var cut = RenderScreen();

        Assert.Equal("Widok miesiąca", cut.Find(".eod-page h1").TextContent);
        Assert.Equal(
            [
                ("Tydzień 1 (01.09–06.09)", "9 pozycji · Suma tygodnia: 30h"),
                ("Tydzień 2 (07.09–13.09)", "3 pozycje · Suma tygodnia: 16h"),
                ("Tydzień 3 (14.09–20.09)", "7 pozycji · Suma tygodnia: 18h"),
                ("Tydzień 4 (21.09–27.09)", "5 pozycji · Suma tygodnia: 25h"),
                ("Tydzień 5 (28.09–30.09)", "7 pozycji · Suma tygodnia: 14h"),
            ],
            Groups(cut, 0));
        var days = Groups(cut, 1);
        Assert.Equal(15, days.Count);
        Assert.Equal(("Wtorek, 01.09.2026", "3 pozycje · Suma dnia: 8h"), days[0]);
        Assert.Equal(("Piątek, 04.09.2026", "2 pozycje · Suma dnia: 6h"), days[3]);
        Assert.Equal(("Sobota, 26.09.2026", "1 pozycja · Suma dnia: 1h"), days[12]);
        Assert.Equal("31 pozycji · Suma Miesiąca: 103h", Footer(cut));
        Assert.Equal(["Projekt", "Typ pracy", "Opis", "Czas"], cut.FindAll("thead th .eod-th-text").Select(e => e.TextContent));
        Assert.Equal(expected.Entries.Select(e => e.Description), Column(cut, 2));
        Assert.Equal(["AAA", "Development", "Zadanie 1", "4h"], Rows(cut)[0].QuerySelectorAll("td").Select(td => td.TextContent.Trim()));
    }

    [Fact]
    public async Task ColumnFilter_HidesRowsAndDay_SumsStayTheSame()
    {
        await SeedSeptemberAsync();
        var cut = RenderScreen();

        await OpenColumnFilterAsync(cut, "Projekt");
        var bbb = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "BBB (10)");
        await cut.InvokeAsync(() => bbb.Instance.ValueChanged.InvokeAsync(false));

        Assert.Equal(21, Rows(cut).Count);
        Assert.DoesNotContain("BBB", Column(cut, 0));
        Assert.Equal(("Tydzień 1 (01.09–06.09)", "7 pozycji · Suma tygodnia: 30h"), Groups(cut, 0)[0]);
        var days = Groups(cut, 1);
        Assert.Equal(("Wtorek, 01.09.2026", "2 pozycje · Suma dnia: 8h"), days[0]);
        // 03.09 has only BBB: its group disappears.
        Assert.DoesNotContain(days, d => d.Label == "Czwartek, 03.09.2026");
        Assert.Equal("21 pozycji · Suma Miesiąca: 103h · po filtrach, z 31", Footer(cut));
    }

    [Fact]
    public async Task NoExportNoPagingNoSorting_HelpTopic_AnotherUsersWpisyHidden()
    {
        await SeedSeptemberAsync();
        await SeedOtherUserAsync();

        var cut = RenderScreen();

        Assert.DoesNotContain("Eksportuj", cut.Markup);
        var report = cut.FindComponent<EodReport>().Instance;
        Assert.Null(report.PageSize);
        Assert.Null(report.ExportFile);
        Assert.Empty(cut.FindAll("th [col-sort-button]"));
        Assert.Empty(cut.FindAll("tbody tr[hover] fluent-button"));
        Assert.Equal("/pomoc/miesiac", cut.Find(".app-help").GetAttribute("href"));
        Assert.DoesNotContain("Cudzy", cut.Markup);
        Assert.Equal("31 pozycji · Suma Miesiąca: 103h", Footer(cut));
    }

    [Fact]
    public async Task EmptyMonth_EmptyState_FooterZero()
    {
        await SeedOtherUserAsync();

        var cut = RenderScreen();

        Assert.Contains(MonthPage.EmptyText, cut.Find("tbody").TextContent);
        Assert.Empty(Rows(cut));
        Assert.Equal("0 pozycji · Suma Miesiąca: 0h", Footer(cut));
    }

    [Fact]
    public void Definition_LayoutKey_HelpTopic()
    {
        Assert.Equal(HelpTopics.Miesiac, MonthPage.Report.Key);
        Assert.Equal([MonthPage.WeekKey, MonthPage.DayKey], MonthPage.Report.Grouping);
        Assert.All(MonthPage.Report.Columns, c => Assert.Equal(ColumnType.List, c.Type));
        Assert.Equal("miesiac@2026-09", MonthPage.LayoutKey(new YearMonth(2026, 9)));
        var row = MonthPage.ToRow(new MonthEntry(7, new DateOnly(2026, 11, 1), 1, "AAA", 2, "Testy", "Opis", 90));
        Assert.Equal((7, new DateOnly(2026, 11, 1)), (row["Id"], row[MonthPage.WeekKey]));
        Assert.Equal(new DateOnly(2026, 9, 28), MonthPage.ToRow(SeptemberShape.Entries()[^1])[MonthPage.WeekKey]);

        var topic = HelpTopics.Find("miesiac");
        Assert.NotNull(topic);
        Assert.Equal(("Widok miesiąca", HelpAccess.LoggedIn), (topic.Title, topic.Access));
    }

    private IRenderedComponent<MainLayout> RenderScreen()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, b =>
        {
            b.OpenComponent<MonthPage>(0);
            b.CloseComponent();
        }));
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll(".eod-page table"));
            Assert.Empty(cut.FindAll(".eod-skeleton-row"));
        }, TimeSpan.FromSeconds(5));
        return cut;
    }

    private static List<(string Label, string Summary)> Groups(IRenderedComponent<MainLayout> cut, int level) =>
        cut.FindAll($"tr.eod-group-row[data-level='{level}'] .eod-group")
            .Select(g => (g.QuerySelector("span:not(.eod-caption)")!.TextContent.Trim(), g.QuerySelector(".eod-caption")!.TextContent.Trim()))
            .ToList();

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<MainLayout> cut) => cut.FindAll("tbody tr[hover]");

    private static List<string> Column(IRenderedComponent<MainLayout> cut, int index) =>
        Rows(cut).Select(tr => tr.QuerySelectorAll("td")[index].TextContent.Trim()).ToList();

    private static string Footer(IRenderedComponent<MainLayout> cut) => cut.Find(".eod-card-foot .eod-num").TextContent.Trim();

    private static async Task OpenColumnFilterAsync(IRenderedComponent<MainLayout> cut, string label)
    {
        var button = cut.FindAll("th fluent-button").Single(b => b.GetAttribute("title")?.EndsWith($": {label}", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => button.Click());
    }

    /// <summary>The <see cref="SeptemberShape"/> Wpisy of the User, one save each in Id order, so the ids match.</summary>
    private async Task SeedSeptemberAsync()
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var projects = SeptemberShape.Projects.Select(c => new Project { Code = c, Name = c }).ToList();
        var workTypes = SeptemberShape.WorkTypes.Select(n => new WorkType { Name = n }).ToList();
        db.Projects.AddRange(projects);
        db.WorkTypes.AddRange(workTypes);
        await db.SaveChangesAsync(Ct);
        foreach (var e in SeptemberShape.Raw.OrderBy(r => r.Id))
        {
            db.TimeEntries.Add(new TimeEntry
            {
                Date = new DateOnly(2026, 9, e.Day),
                ProjectId = projects.Single(p => p.Code == e.Project).Id,
                WorkTypeId = workTypes.Single(w => w.Name == e.WorkType).Id,
                Description = SeptemberShape.Description(e.Id),
                Minutes = e.Minutes,
            });
            await db.SaveChangesAsync(Ct);
        }
    }

    private async Task SeedOtherUserAsync()
    {
        await using var other = await _postgres.CreateUserContextAsync(_otherUser);
        var project = new Project { Code = "CUD", Name = "Cudzy" };
        var workType = new WorkType { Name = "Cudza praca" };
        other.Projects.Add(project);
        other.WorkTypes.Add(workType);
        await other.SaveChangesAsync(Ct);
        other.TimeEntries.Add(new TimeEntry
        {
            Date = new DateOnly(2026, 9, 2), ProjectId = project.Id, WorkTypeId = workType.Id, Description = "Cudzy", Minutes = 60,
        });
        await other.SaveChangesAsync(Ct);
    }
}
