using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Pomoc;
using RejestrCzasu.Features.Projekty;
using RejestrCzasu.Features.TypyPracy;
using RejestrCzasu.Features.Wpisy;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;
using FilterSelect = Microsoft.FluentUI.AspNetCore.Components.FluentSelect<
    RejestrCzasu.Features.Wpisy.EntriesPage.FilterOption, RejestrCzasu.Features.Wpisy.EntriesPage.FilterOption>;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Lista wpisów screen (R8, R17, R2, R26, AD-9) in bUnit with the real stores on the test database and a fixed clock.
/// </summary>
public sealed class EntriesPageTests : BunitContext, IAsyncLifetime
{
    // 22:30 UTC on 2 September is already 3 September in Poland (CEST, UTC+2).
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 22, 30, 0, TimeSpan.Zero);

    private readonly PostgresFixture _postgres;
    private readonly CapturingLogger<AppErrorBoundary> _errors = new();
    private int _user;
    private int _otherUser;

    public EntriesPageTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        this.AddAppUi();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AppErrorBoundary>>(_errors);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        // Created on first use, after InitializeAsync has created the User.
        Services.AddScoped(_ => new EntryStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped(_ => new ProjectStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped(_ => new WorkTypeStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        AddAuthorization().SetAuthorized("ekran-lista-wpisow");
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _postgres.ResetAsync();
        _user = await _postgres.CreateUserAsync("ekran-lista-wpisow");
        _otherUser = await _postgres.CreateUserAsync("ekran-lista-wpisow-b");
    }

    [Fact]
    public async Task Open_CurrentMonth_NewestFirst_CountAndSum_HeaderAndHelpTopic()
    {
        await SeedAsync();

        var cut = RenderScreen();

        Assert.Equal("Lista wpisów", cut.Find("h1").TextContent);
        Assert.Equal("/pomoc/wpisy", cut.Find(".app-header .app-help").GetAttribute("href"));
        var pickers = cut.FindComponents<FluentDatePicker<DateOnly?>>();
        Assert.Equal([new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)], pickers.Select(p => p.Instance.Value));
        Assert.All(pickers, p => Assert.Equal(DatePickerRenderStyle.Native, p.Instance.RenderStyle));
        cut.WaitForAssertion(() => Assert.Equal(["Koniec września", "Drugi 1 września", "Pierwszy 1 września"], Descriptions(cut)));
        Assert.Equal(["30.09.2026", "Środa", "OLD", "Development", "Koniec września", "24h"],
            cut.FindAll("tbody tr")[0].QuerySelectorAll("td").Select(td => td.TextContent));
        Assert.Equal("Wpisy: 3, suma Czasu: 25h 30m", Summary(cut));
        Assert.Equal("Pozycje: 3", cut.Find(".report-grid-counter").TextContent);
        Assert.DoesNotContain("Cudzy", cut.Markup);
    }

    [Fact]
    public async Task Options_AllFirst_IncludeInactive_NeverAnotherUsers()
    {
        await SeedAsync();

        var cut = RenderScreen();

        Assert.Equal(["Wszystkie", "OLD — Stary (nieaktywny)", "RDZ — Rozwój"], Options(ProjectSelect(cut)));
        Assert.Equal(["Wszystkie", "Archiwalny (nieaktywny)", "Development", "Spotkanie"], Options(WorkTypeSelect(cut)));
        Assert.Equal("Wszystkie", ProjectSelect(cut).Instance.Value?.Text);
        Assert.DoesNotContain("CUD", Options(ProjectSelect(cut)).Select(o => o[..3]));
    }

    [Fact]
    public async Task ProjectFilter_And_Range_ChangeListCountAndSum()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        await SelectAsync(cut, ProjectSelect(cut), "RDZ — Rozwój");
        cut.WaitForAssertion(() => Assert.Equal(["Drugi 1 września", "Pierwszy 1 września"], Descriptions(cut)));
        Assert.Equal("Wpisy: 2, suma Czasu: 1h 30m", Summary(cut));

        await SetDateAsync(cut, 0, new DateOnly(2026, 8, 1));
        await SetDateAsync(cut, 1, new DateOnly(2026, 8, 31));
        cut.WaitForAssertion(() => Assert.Equal(["Sierpień"], Descriptions(cut)));
        Assert.Equal("Wpisy: 1, suma Czasu: 2h", Summary(cut));

        await SelectAsync(cut, WorkTypeSelect(cut), "Spotkanie");
        cut.WaitForAssertion(() => Assert.Empty(Descriptions(cut)));
        Assert.Equal("Wpisy: 0, suma Czasu: 0h", Summary(cut));
    }

    [Fact]
    public async Task InactiveProject_Filter_ShowsItsEntries()
    {
        await SeedAsync();
        var cut = RenderScreen();

        await SelectAsync(cut, ProjectSelect(cut), "OLD — Stary (nieaktywny)");

        cut.WaitForAssertion(() => Assert.Equal(["Koniec września"], Descriptions(cut)));
        Assert.Equal("Wpisy: 1, suma Czasu: 24h", Summary(cut));
    }

    [Fact]
    public async Task EmptyFrom_ShowsEverythingUpToTo()
    {
        await SeedAsync();
        var cut = RenderScreen();

        await SetDateAsync(cut, 0, null);

        cut.WaitForAssertion(() => Assert.Equal(4, Descriptions(cut).Count));
        Assert.Equal("Wpisy: 4, suma Czasu: 27h 30m", Summary(cut));
    }

    [Fact]
    public async Task FromAfterTo_EmptyList_Message_ZeroSum()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        await SetDateAsync(cut, 0, new DateOnly(2026, 9, 10));
        await SetDateAsync(cut, 1, new DateOnly(2026, 9, 1));

        cut.WaitForAssertion(() => Assert.Empty(Descriptions(cut)));
        Assert.Equal("Data „Od” jest późniejsza niż „Do”.", cut.Find(".entries-filter-error").TextContent);
        Assert.Equal("Wpisy: 0, suma Czasu: 0h", Summary(cut));

        await SetDateAsync(cut, 1, new DateOnly(2026, 9, 30));
        cut.WaitForAssertion(() => Assert.Equal(["Koniec września"], Descriptions(cut)));
        Assert.Empty(cut.FindAll(".entries-filter-error"));
    }

    [Fact]
    public async Task NoEntries_EmptyGrid_ZeroSummary()
    {
        var cut = RenderScreen();

        cut.WaitForAssertion(() => Assert.Equal("Wpisy: 0, suma Czasu: 0h", Summary(cut)));
        Assert.Empty(Descriptions(cut));
    }

    [Fact]
    public void Summary_TwoEntriesOf24h_Is48h()
    {
        var day = new DateOnly(2026, 9, 3);
        EntryRow[] rows = [new(1, day, 1, "RDZ", 1, "Development", "A", 1440), new(2, day, 1, "RDZ", 1, "Development", "B", 1440)];

        Assert.Equal("Wpisy: 2, suma Czasu: 48h", EntriesPage.Summary(rows));
    }

    [Fact]
    public void HelpTopic_Wpisy_IsForLoggedInUsers()
    {
        var topic = HelpTopics.Find("wpisy");

        Assert.NotNull(topic);
        Assert.Equal(("Lista wpisów", HelpAccess.LoggedIn), (topic.Title, topic.Access));
    }

    private IRenderedComponent<MainLayout> RenderScreen()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, b =>
        {
            b.OpenComponent<EntriesPage>(0);
            b.CloseComponent();
        }));
        cut.WaitForElement(".entries-summary", TimeSpan.FromSeconds(5));
        return cut;
    }

    private static Task SelectAsync(IRenderedComponent<MainLayout> cut, IRenderedComponent<FilterSelect> select, string text)
    {
        var option = select.Instance.Items!.Single(o => o.Text == text);
        return cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(option));
    }

    private static Task SetDateAsync(IRenderedComponent<MainLayout> cut, int index, DateOnly? value)
    {
        var picker = cut.FindComponents<FluentDatePicker<DateOnly?>>()[index];
        return cut.InvokeAsync(() => picker.Instance.ValueChanged.InvokeAsync(value));
    }

    private static IRenderedComponent<FilterSelect> ProjectSelect(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponents<FilterSelect>().Single(s => s.Instance.Class == "entries-project");

    private static IRenderedComponent<FilterSelect> WorkTypeSelect(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponents<FilterSelect>().Single(s => s.Instance.Class == "entries-work-type");

    private static List<string> Options(IRenderedComponent<FilterSelect> select) =>
        select.Instance.Items!.Select(o => o.Text).ToList();

    private static List<string> Descriptions(IRenderedComponent<MainLayout> cut) =>
        cut.FindAll("tbody tr td:nth-child(5)").Select(td => td.TextContent).ToList();

    private static string Summary(IRenderedComponent<MainLayout> cut) => cut.Find(".entries-summary").TextContent;

    /// <summary>
    /// RDZ (active) and OLD (inactive); Development, Spotkanie and inactive Archiwalny. Entries: August RDZ 2h; September
    /// RDZ 30m and 1h on the 1st (in this order), OLD 24h on the 30th. Another User has its own CUD Entry in September.
    /// </summary>
    private async Task SeedAsync()
    {
        await using (var db = await _postgres.CreateUserContextAsync(_user))
        {
            var rdz = new Project { Code = "RDZ", Name = "Rozwój" };
            var old = new Project { Code = "OLD", Name = "Stary", IsActive = false };
            var development = new WorkType { Name = "Development" };
            db.Projects.AddRange(rdz, old);
            db.WorkTypes.AddRange(development, new WorkType { Name = "Spotkanie" }, new WorkType { Name = "Archiwalny", IsActive = false });
            await db.SaveChangesAsync(Ct);
            foreach (var (date, project, description, minutes) in new[]
            {
                (new DateOnly(2026, 8, 20), rdz, "Sierpień", 120),
                (new DateOnly(2026, 9, 1), rdz, "Pierwszy 1 września", 30),
                (new DateOnly(2026, 9, 1), rdz, "Drugi 1 września", 60),
                (new DateOnly(2026, 9, 30), old, "Koniec września", 1440),
            })
            {
                db.TimeEntries.Add(new TimeEntry
                {
                    Date = date, ProjectId = project.Id, WorkTypeId = development.Id, Description = description, Minutes = minutes,
                });
                // One save per Entry, so the Ids follow the order above.
                await db.SaveChangesAsync(Ct);
            }
        }

        await using var other = await _postgres.CreateUserContextAsync(_otherUser);
        var cud = new Project { Code = "CUD", Name = "Cudzy" };
        var otherWorkType = new WorkType { Name = "Cudza praca" };
        other.Projects.Add(cud);
        other.WorkTypes.Add(otherWorkType);
        await other.SaveChangesAsync(Ct);
        other.TimeEntries.Add(new TimeEntry
        {
            Date = new DateOnly(2026, 9, 2), ProjectId = cud.Id, WorkTypeId = otherWorkType.Id, Description = "Cudzy", Minutes = 60,
        });
        await other.SaveChangesAsync(Ct);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
