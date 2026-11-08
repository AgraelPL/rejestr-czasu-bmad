using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Domain;
using RejestrCzasu.Features.Miesiac;
using RejestrCzasu.Features.Pomoc;
using RejestrCzasu.Features.Wpisy;
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
    private readonly CapturingLogger<AppErrorBoundary> _errors = new();
    private readonly EntryDateMemory _dateMemory = new();
    private int _user;
    private int _otherUser;

    public MonthPageTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        this.AddAppUi();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AppErrorBoundary>>(_errors);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        Services.AddScoped(_ => new MonthEntriesQuery(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped(_ => _postgres.CreateLoggedInCurrentUser(_user));
        // The row actions (AD-10), as on Lista wpisów.
        Services.AddScoped(_ => new EntryStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped(_ => new DictionaryOptions(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddSingleton(_dateMemory);
        Services.AddScoped<EntryDialogs>();
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
        Assert.Equal(["AAA", "Development", "Zadanie 1", "4h"], Rows(cut)[0].QuerySelectorAll("td").Take(4).Select(td => td.TextContent.Trim()));
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
    public async Task September_DownloadLink_ToSummaryEndpoint_WithDownloadAttribute()
    {
        await SeedSeptemberAsync();

        var cut = RenderScreen();

        var link = cut.Find(".eod-page a[download]");
        Assert.Equal("/zestawienie/2026/9", link.GetAttribute("href"));
        Assert.Equal(MonthPage.DownloadText, link.TextContent.Trim());
        Assert.Contains("app-download-link", link.ClassList);
        Assert.DoesNotContain(MonthPage.NoSummaryText, cut.Markup);
    }

    [Fact]
    public async Task EmptyMonth_NoDownloadLink_Text()
    {
        await SeedOtherUserAsync();

        var cut = RenderScreen();

        Assert.Empty(cut.FindAll("a[download]"));
        Assert.Contains(MonthPage.NoSummaryText, cut.Find(".eod-page").TextContent);
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

    [Fact]
    public async Task NoMonth_CurrentMonth_CurrentDisabled()
    {
        await SeedSeptemberAsync();

        var cut = RenderScreen();

        Assert.StartsWith("Wrzesień 2026:", Caption(cut));
        Assert.Equal("31 pozycji · Suma Miesiąca: 103h", Footer(cut));
        Assert.True(Button(cut, "month-current").Instance.Disabled);
        Assert.False(Button(cut, "month-previous").Instance.Disabled);
        Assert.False(Button(cut, "month-next").Instance.Disabled);
        Assert.Equal(["Poprzedni", "Bieżący", "Następny"],
            new[] { "month-previous", "month-current", "month-next" }.Select(c => cut.Find($"fluent-button.{c}").TextContent.Trim()));
    }

    [Fact]
    public async Task Bookmark_OpensThatMonth_SumsOfIt()
    {
        await SeedSeptemberAsync();

        var cut = RenderScreen("2026-08");
        Assert.StartsWith("Sierpień 2026:", Caption(cut));
        Assert.Equal("0 pozycji · Suma Miesiąca: 0h", Footer(cut));
        Assert.False(Button(cut, "month-current").Instance.Disabled);

        SetMonth(cut, "2026-09");

        Assert.StartsWith("Wrzesień 2026:", Caption(cut));
        Assert.Equal(
            ["9 pozycji · Suma tygodnia: 30h", "3 pozycje · Suma tygodnia: 16h", "7 pozycji · Suma tygodnia: 18h",
             "5 pozycji · Suma tygodnia: 25h", "7 pozycji · Suma tygodnia: 14h"],
            Groups(cut, 0).Select(g => g.Summary));
        Assert.Equal("31 pozycji · Suma Miesiąca: 103h", Footer(cut));
        Assert.Equal("/zestawienie/2026/9", cut.Find(".eod-page a[download]").GetAttribute("href"));
    }

    [Fact]
    public async Task PreviousNextCurrent_GoToAddress_AcrossTheYear()
    {
        await SeedSeptemberAsync();
        await AddEntryAsync(new DateOnly(2025, 12, 31), "Sylwester", 90);
        await AddEntryAsync(new DateOnly(2026, 1, 2), "Nowy rok", 150);
        var navigation = Services.GetRequiredService<NavigationManager>();

        var cut = RenderScreen("2026-01");
        Assert.Equal(["AAA", "Development", "Nowy rok", "2h 30m"], Rows(cut).Single().QuerySelectorAll("td").Take(4).Select(td => td.TextContent.Trim()));
        Assert.Equal("1 pozycja · Suma Miesiąca: 2h 30m", Footer(cut));
        await cut.InvokeAsync(() => Button(cut, "month-previous").Instance.OnClick.InvokeAsync());
        Assert.EndsWith("/miesiac/2025-12", navigation.Uri, StringComparison.Ordinal);

        SetMonth(cut, "2025-12");
        Assert.StartsWith("Grudzień 2025:", Caption(cut));
        Assert.Equal(["AAA", "Development", "Sylwester", "1h 30m"], Rows(cut).Single().QuerySelectorAll("td").Take(4).Select(td => td.TextContent.Trim()));
        Assert.Equal("1 pozycja · Suma Miesiąca: 1h 30m", Footer(cut));
        await cut.InvokeAsync(() => Button(cut, "month-next").Instance.OnClick.InvokeAsync());
        Assert.EndsWith("/miesiac/2026-01", navigation.Uri, StringComparison.Ordinal);

        SetMonth(cut, "2026-01");
        Assert.Equal(["Nowy rok"], Column(cut, 2));
        Assert.Equal("1 pozycja · Suma Miesiąca: 2h 30m", Footer(cut));

        await cut.InvokeAsync(() => Button(cut, "month-current").Instance.OnClick.InvokeAsync());
        Assert.EndsWith("/miesiac/2026-09", navigation.Uri, StringComparison.Ordinal);
        SetMonth(cut, "2026-09");
        Assert.Equal("31 pozycji · Suma Miesiąca: 103h", Footer(cut));
        Assert.True(Button(cut, "month-current").Instance.Disabled);
    }

    [Fact]
    public void FirstAndLastMonth_PreviousAndNextDisabled()
    {
        var cut = RenderScreen("0001-01");
        Assert.True(Button(cut, "month-previous").Instance.Disabled);
        Assert.False(Button(cut, "month-next").Instance.Disabled);

        SetMonth(cut, "9999-12");
        Assert.False(Button(cut, "month-previous").Instance.Disabled);
        Assert.True(Button(cut, "month-next").Instance.Disabled);
    }

    [Theory]
    [InlineData("2026-13")]
    [InlineData("abc")]
    [InlineData("2026-9")]
    [InlineData("0000-01")]
    [InlineData("٢٠٢٦-09")]
    public void WrongAddress_IsNotFound(string month)
    {
        var notFound = false;
        Services.GetRequiredService<NavigationManager>().OnNotFound += (_, _) => notFound = true;

        var cut = Render<MainLayout>(p => p.Add(l => l.Body, Body(month)));

        Assert.True(notFound);
        Assert.Empty(cut.FindComponents<EodReport>());
        Assert.False(MonthPage.TryParse(month, out _));
    }

    [Fact]
    public void TryParse_StrictFormat()
    {
        Assert.True(MonthPage.TryParse("2026-09", out var month));
        Assert.Equal(new YearMonth(2026, 9), month);
        Assert.True(MonthPage.TryParse("9999-12", out _));
        Assert.False(MonthPage.TryParse("2026/09", out _));
        Assert.False(MonthPage.TryParse("+026-09", out _));
    }

    [Fact]
    public async Task Rows_HaveEditCopyDeleteIcons()
    {
        await SeedSeptemberAsync();

        var cut = RenderScreen("2026-09");

        Assert.All(Rows(cut), row => Assert.Equal(["Edytuj", "Kopiuj", "Usuń"],
            row.QuerySelectorAll("fluent-button[aria-label]").Select(b => b.GetAttribute("aria-label"))));
        Assert.All(Rows(cut)[0].QuerySelectorAll("fluent-button[aria-label]"), b => Assert.Equal("subtle", b.GetAttribute("appearance")));
    }

    [Fact]
    public async Task Edit_ToAnotherMonth_LeavesSeptember_SumsSmaller_BarAfterReload()
    {
        await SeedSeptemberAsync();
        var cut = RenderScreen("2026-09");

        // Zadanie 9: 02.09, AAA/Development, 2h.
        _ = OpenWindow(cut, "Zadanie 9", "Edytuj");
        await cut.InvokeAsync(() => DialogDate(cut).Instance.ValueChanged.InvokeAsync(new DateOnly(2026, 10, 2)));
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "success", "Zapisano Wpis: 02.10.2026, AAA, 2h.");
        // No further wait: when the bar is there, the Miesiąc has already been read again (afterChange).
        Assert.DoesNotContain("Zadanie 9", Column(cut, 2));
        Assert.Equal(("Środa, 02.09.2026", "2 pozycje · Suma dnia: 6h"), Groups(cut, 1)[1]);
        Assert.Equal(("Tydzień 1 (01.09–06.09)", "8 pozycji · Suma tygodnia: 28h"), Groups(cut, 0)[0]);
        Assert.Equal("30 pozycji · Suma Miesiąca: 101h", Footer(cut));
    }

    [Fact]
    public async Task Delete_LastOfDay_DayGroupGone_SumsSmaller_BarAfterReload()
    {
        await SeedSeptemberAsync();
        var cut = RenderScreen("2026-09");

        // Zadanie 6 is the only Wpis of 03.09 (BBB/Development, 8h).
        _ = OpenWindow(cut, "Zadanie 6", "Usuń", ".eod-confirm-primary");
        await cut.Find(".eod-confirm-primary").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "success", "Usunięto Wpis: 03.09.2026, BBB, 8h.");
        Assert.DoesNotContain(Groups(cut, 1), d => d.Label == "Czwartek, 03.09.2026");
        Assert.Equal(("Tydzień 1 (01.09–06.09)", "8 pozycji · Suma tygodnia: 22h"), Groups(cut, 0)[0]);
        Assert.Equal("30 pozycji · Suma Miesiąca: 95h", Footer(cut));
    }

    [Fact]
    public async Task Copy_IntoCurrentMonth_NewRow_SumsLarger()
    {
        await SeedSeptemberAsync();
        var cut = RenderScreen();

        // Without a date from Nowy wpis the copy takes today in Poland, 15.09.2026 (R7).
        _ = OpenWindow(cut, "Zadanie 1", "Kopiuj");
        Assert.Equal(new DateOnly(2026, 9, 15), DialogDate(cut).Instance.Value);
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "success", "Zapisano Wpis: 15.09.2026, AAA, 4h.");
        Assert.Equal(32, Rows(cut).Count);
        Assert.Equal(("Wtorek, 15.09.2026", "6 pozycji · Suma dnia: 10h"), Groups(cut, 1).Single(d => d.Label.EndsWith("15.09.2026", StringComparison.Ordinal)));
        Assert.Equal("32 pozycje · Suma Miesiąca: 107h", Footer(cut));
    }

    [Theory]
    [InlineData("Edytuj")]
    [InlineData("Kopiuj")]
    [InlineData("Usuń")]
    public async Task EntryDeletedInTheMeantime_ListReloaded_ThenWarning_NoWindow(string action)
    {
        await SeedSeptemberAsync();
        var cut = RenderScreen("2026-09");
        await using (var db = await _postgres.CreateUserContextAsync(_user))
        {
            await db.TimeEntries.Where(e => e.Description == "Zadanie 6").ExecuteDeleteAsync(Ct);
        }

        await Action(cut, "Zadanie 6", action).ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "warning", EntryMessages.NotFound);
        Assert.DoesNotContain("Zadanie 6", Column(cut, 2));
        Assert.Equal("30 pozycji · Suma Miesiąca: 95h", Footer(cut));
        Assert.Empty(cut.FindAll("fluent-dialog"));
    }

    [Fact]
    public async Task SecondClickWhileWindowOpen_OpensNoSecondWindow()
    {
        await SeedSeptemberAsync();
        var cut = RenderScreen("2026-09");

        _ = OpenWindow(cut, "Zadanie 6", "Usuń", ".eod-confirm-secondary");
        await Action(cut, "Zadanie 1", "Edytuj").ClickAsync(new MouseEventArgs());

        Assert.Single(cut.FindAll("fluent-dialog"));
        Assert.Empty(cut.FindAll(".entry-dialog-cancel"));
    }

    private IRenderedComponent<MainLayout> RenderScreen(string? month = null)
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, Body(month)));
        WaitForReport(cut);
        return cut;
    }

    private static RenderFragment Body(string? month) => b =>
    {
        b.OpenComponent<MonthPage>(0);
        b.AddAttribute(1, nameof(MonthPage.Miesiac), month);
        b.CloseComponent();
    };

    /// <summary>The route sets a new segment: the same MonthPage gets the parameter again.</summary>
    private static void SetMonth(IRenderedComponent<MainLayout> cut, string month)
    {
        cut.Render(p => p.Add(l => l.Body, Body(month)));
        cut.WaitForAssertion(
            () => Assert.Equal($"miesiac@{month}", cut.FindComponent<EodReport>().Instance.LayoutKey), TimeSpan.FromSeconds(5));
        WaitForReport(cut);
    }

    private static void WaitForReport(IRenderedComponent<MainLayout> cut) =>
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll(".eod-page table"));
            Assert.Empty(cut.FindAll(".eod-skeleton-row"));
        }, TimeSpan.FromSeconds(5));

    private static string Caption(IRenderedComponent<MainLayout> cut) => cut.Find(".eod-page-head .eod-caption").TextContent;

    private static IRenderedComponent<FluentButton> Button(IRenderedComponent<MainLayout> cut, string cssClass) =>
        cut.FindComponents<FluentButton>().Single(b => b.Instance.Class == cssClass);

    private static IElement Action(IRenderedComponent<MainLayout> cut, string description, string label) =>
        Rows(cut).Single(tr => tr.QuerySelectorAll("td")[2].TextContent.Trim() == description)
            .QuerySelector($"fluent-button[aria-label='{label}']")!;

    /// <summary>Clicks a row icon and waits for the window it opens (the action stays pending until the window closes).</summary>
    private static Task OpenWindow(IRenderedComponent<MainLayout> cut, string description, string label, string waitFor = ".entry-dialog-cancel")
    {
        var action = Action(cut, description, label).ClickAsync(new MouseEventArgs());
        cut.WaitForElement(waitFor, TimeSpan.FromSeconds(5));
        return action;
    }

    private static IRenderedComponent<FluentDatePicker<DateOnly?>> DialogDate(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponents<FluentDatePicker<DateOnly?>>().Single(p => p.Instance.Class == "entry-date");

    private void WaitForMessage(IRenderedComponent<MainLayout> cut, string intent, string text) =>
        cut.WaitForAssertion(() => Assert.Contains($"{intent}: {text}", Messages(cut)), TimeSpan.FromSeconds(5));

    private List<string> Messages(IRenderedComponent<MainLayout> cut) =>
    [
        .. cut.FindAll("fluent-message-bar").Select(b => $"{b.GetAttribute("intent")}: {b.TextContent.Trim()}"),
        .. _errors.Entries.Select(e => $"log: {e.Exception}"),
    ];

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

    private Task SeedSeptemberAsync() => SeptemberShape.SeedAsync(_postgres, _user, Ct);

    /// <summary>Adds a Wpis of AAA / Development (from <see cref="SeedSeptemberAsync"/>) on <paramref name="date"/>.</summary>
    private async Task AddEntryAsync(DateOnly date, string description, int minutes)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var project = await db.Projects.SingleAsync(p => p.Code == "AAA", Ct);
        var workType = await db.WorkTypes.SingleAsync(w => w.Name == "Development", Ct);
        db.TimeEntries.Add(new TimeEntry
        {
            Date = date, ProjectId = project.Id, WorkTypeId = workType.Id, Description = description, Minutes = minutes,
        });
        await db.SaveChangesAsync(Ct);
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
