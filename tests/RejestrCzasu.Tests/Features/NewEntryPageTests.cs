using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Wpisy;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Nowy wpis screen (R4, R5, R16, AD-8, AD-10) in bUnit with the real <see cref="EntryStore"/> and
/// <see cref="DictionaryOptions"/> on the test database and a fixed clock.
/// </summary>
public sealed class NewEntryPageTests : BunitContext, IAsyncLifetime
{
    // 22:30 UTC on 2 September is already 3 September (Thursday) in Poland (CEST, UTC+2).
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 22, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 3);

    /// <summary>Every async test fails instead of hanging the run.</summary>
    private const int TestTimeoutMs = 60_000;

    private readonly PostgresFixture _postgres;
    private readonly CapturingLogger<AppErrorBoundary> _errors = new();
    private readonly EntryDateMemory _dateMemory = new();
    private int _user;

    public NewEntryPageTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        this.AddAppUi();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AppErrorBoundary>>(_errors);
        Services.AddSingleton<TimeProvider>(new FixedTimeProvider(Now));
        Services.AddSingleton(_dateMemory);
        // Created on first use, after InitializeAsync has created the User.
        Services.AddScoped(_ => _postgres.CreateLoggedInCurrentUser(_user));
        Services.AddScoped(_ => new EntryStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped(_ => new DictionaryOptions(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        AddAuthorization().SetAuthorized("ekran-nowy-wpis");
        // The application renders its screens in an Interactive Server circuit.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _postgres.ResetAsync();
        _user = await _postgres.CreateUserAsync("ekran-nowy-wpis");
    }

    [Fact]
    public void Open_TodayInPoland_EmptyFields_HeaderAndHelpTopic()
    {
        var cut = RenderScreen();

        Assert.Equal("Nowy wpis", cut.Find("h1").TextContent);
        Assert.Equal("/pomoc/wpisy-nowy", cut.Find(".app-header .app-help").GetAttribute("href"));
        var date = DatePicker(cut).Instance;
        Assert.Equal(Today, date.Value);
        Assert.Equal(DatePickerRenderStyle.Native, date.RenderStyle);
        Assert.True(date.Autofocus);
        Assert.Equal("Czwartek", cut.Find(".entry-day-name").TextContent);
        Assert.Null(ProjectList(cut).Instance.Value);
        Assert.Null(WorkTypeList(cut).Instance.Value);
        Assert.Equal(("", ""), (Description(cut).Instance.Value, Czas(cut).Instance.Value));
        Assert.True(cut.Find("form.entry-form").HasAttribute("novalidate"));
        Assert.Equal(Today, _dateMemory.Find(_user));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task FocusOrder_FollowsMarkup_DataProjektTypPracyOpisCzas()
    {
        await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        var fields = cut.FindAll(".entry-date, .entry-project, .entry-work-type, .entry-description, .entry-czas")
            .Select(e => e.ClassList.First(c => c.StartsWith("entry-", StringComparison.Ordinal)))
            .ToList();

        Assert.Equal(["entry-date", "entry-project", "entry-work-type", "entry-description", "entry-czas"], fields);
        Assert.DoesNotContain(cut.FindAll("form.entry-form [tabindex]"),
            e => int.TryParse(e.GetAttribute("tabindex"), out var index) && index > 0);
        Assert.Equal("date", cut.Find("form.entry-form fluent-text-input").GetAttribute("type"));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task EnterInCzas_SavesEntry_KeepsDate_ClearsRest_FocusesProjekt_ShowsSuccess()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Spotkanie");
        await EnterAsync(cut, Czas(cut), "1h 30m");

        WaitForMessage(cut, "success", "Zapisano Wpis: 03.09.2026, RDZ, 1h 30m.");
        var entry = Assert.Single(await EntriesAsync());
        Assert.Equal(
            (_user, Today, projects["RDZ"].Id, workTypes["Development"].Id, "Spotkanie", 90),
            (entry.UserId, entry.Date, entry.ProjectId, entry.WorkTypeId, entry.Description, entry.Minutes));
        Assert.Equal(Today, DatePicker(cut).Instance.Value);
        Assert.Null(ProjectList(cut).Instance.Value);
        Assert.Null(WorkTypeList(cut).Instance.Value);
        Assert.Equal(("", ""), (Description(cut).Instance.Value, Czas(cut).Instance.Value));
        var focus = JSInterop.VerifyFocusAsyncInvoke();
        Assert.Equal(ProjectList(cut).Instance.Element.Id, focus.Arguments[0] is ElementReference e ? e.Id : null);
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task EnterInOpis_AndInData_AndSaveButton_SaveToo()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "", "0:15");
        await EnterAsync(cut, Description(cut), "Z Enterem w opisie");
        WaitForMessage(cut, "success", "Zapisano Wpis: 03.09.2026, RDZ, 15m.");

        await FillAsync(cut, projects["ABC"], workTypes["Development"], "Z Enterem w dacie", "24h");
        await cut.Find(".entry-form-date").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        await cut.Find(".entry-form-date").KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });
        WaitForMessage(cut, "success", "Zapisano Wpis: 03.09.2026, ABC, 24h.");

        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Przyciskiem", "1,5h");
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());
        WaitForMessage(cut, "success", "Zapisano Wpis: 03.09.2026, RDZ, 1h 30m.");

        Assert.Equal(
            [("Z Enterem w opisie", 15), ("Z Enterem w dacie", 1440), ("Przyciskiem", 90)],
            (await EntriesAsync()).OrderBy(e => e.Id).Select(e => (e.Description, e.Minutes)));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task OtherKeysInData_DoNotSave()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Opis", "1:00");
        await cut.Find(".entry-form-date").KeyDownAsync(new KeyboardEventArgs { Key = "a" });
        await cut.Find(".entry-form-date").KeyUpAsync(new KeyboardEventArgs { Key = "a" });
        await cut.Find(".entry-form-date").KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });
        await cut.Find(".entry-form-date").KeyUpAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        Assert.Empty(await EntriesAsync());
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task EnterKeyUpInData_WithoutKeyDownInData_DoesNotSubmit()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        // The key-up of the Enter that opened the screen (menu button Nowy wpis) lands in the autofocused Data.
        await cut.Find(".entry-form-date").KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.DoesNotContain(EntryFormValidator.ProjectMessage, cut.Markup);
        Assert.DoesNotContain(EntryFormValidator.CzasMessage, cut.Markup);

        // A filled form is not saved either, and the key-up has not left a pending Enter behind.
        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Opis", "1h");
        await cut.Find(".entry-form-date").KeyUpAsync(new KeyboardEventArgs { Key = "Enter" });
        Assert.Empty(await EntriesAsync());
    }

    [Theory(Timeout = TestTimeoutMs)]
    [InlineData("0:10")]
    [InlineData("24:15")]
    [InlineData("0:00")]
    [InlineData("1,5")]
    [InlineData("1h 75m")]
    [InlineData("10m")]
    [InlineData("")]
    public async Task WrongCzas_ShowsMessageAtCzas_SavesNothing_KeepsValues(string czas)
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Spotkanie");
        await EnterAsync(cut, Czas(cut), czas);

        cut.WaitForAssertion(() => Assert.Contains(EntryFormValidator.CzasMessage, cut.Markup));
        Assert.Empty(await EntriesAsync());
        Assert.Equal(projects["RDZ"], ProjectList(cut).Instance.Value);
        Assert.Equal("Spotkanie", Description(cut).Instance.Value);
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task EmptyForm_ShowsPolishMessagesAtFields_SavesNothing()
    {
        await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            Assert.Contains(EntryFormValidator.ProjectMessage, cut.Markup);
            Assert.Contains(EntryFormValidator.WorkTypeMessage, cut.Markup);
            Assert.Contains(EntryFormValidator.DescriptionMessage, cut.Markup);
            Assert.Contains(EntryFormValidator.CzasMessage, cut.Markup);
        });
        Assert.DoesNotContain(EntryFormValidator.DateMessage, cut.Markup);
        Assert.Empty(await EntriesAsync());
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task ClearedDate_ShowsMessage_SavesNothing()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Opis");
        await cut.InvokeAsync(() => DatePicker(cut).Instance.ValueChanged.InvokeAsync(null));
        await EnterAsync(cut, Czas(cut), "1:00");

        cut.WaitForAssertion(() => Assert.Contains(EntryFormValidator.DateMessage, cut.Markup));
        Assert.Empty(await EntriesAsync());
        Assert.Equal(Today, _dateMemory.Find(_user));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task Lists_OfferOnlyActiveItems_WithCodeAndName()
    {
        await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        var projects = ProjectList(cut).Instance;
        Assert.Equal(["ABC", "RDZ"], projects.Items!.Select(p => p.Code));
        Assert.Equal("RDZ — Rozwój", EntryForm.ProjectText(projects.Items!.Last()));
        Assert.Equal(["Development"], WorkTypeList(cut).Instance.Items!.Select(w => w.Name));
        Assert.DoesNotContain("NIE", cut.Markup);
        Assert.DoesNotContain("Archiwalny", cut.Markup);
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task ProjectDeletedAfterOpening_ShowsError_ReloadsOptions_KeepsValues()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();
        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Spotkanie");
        await using (var db = await _postgres.CreateUserContextAsync(_user))
        {
            db.Projects.Remove(await db.Projects.SingleAsync(p => p.Code == "RDZ", Ct));
            await db.SaveChangesAsync(Ct);
        }

        await EnterAsync(cut, Czas(cut), "1:30");

        WaitForMessage(cut, "error", "Wybrany Projekt lub Typ pracy już nie istnieje.");
        Assert.Empty(await EntriesAsync());
        cut.WaitForAssertion(() => Assert.Equal(["ABC"], ProjectList(cut).Instance.Items!.Select(p => p.Code)));
        Assert.Equal(projects["RDZ"], ProjectList(cut).Instance.Value);
        Assert.Equal(("Spotkanie", "1:30"), (Description(cut).Instance.Value, Czas(cut).Instance.Value));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task DateChange_IsRemembered_ForThisUserOnly_AndKeptAfterSave()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();

        await cut.InvokeAsync(() => DatePicker(cut).Instance.ValueChanged.InvokeAsync(new DateOnly(2026, 9, 1)));

        Assert.Equal(new DateOnly(2026, 9, 1), _dateMemory.Find(_user));
        Assert.Null(_dateMemory.Find(_user + 1));
        Assert.Equal("Wtorek", cut.Find(".entry-day-name").TextContent);

        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Wczoraj");
        await EnterAsync(cut, Czas(cut), "2:00");

        WaitForMessage(cut, "success", "Zapisano Wpis: 01.09.2026, RDZ, 2h.");
        Assert.Equal(new DateOnly(2026, 9, 1), DatePicker(cut).Instance.Value);
        Assert.Equal(new DateOnly(2026, 9, 1), Assert.Single(await EntriesAsync()).Date);
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task SecondSave_WhileFirstIsRunning_IsIgnored()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        var cut = RenderScreen();
        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Raz", "1:00");

        // Two Enters dispatched together: the second one arrives while the first save is still running.
        var czas = Czas(cut);
        var first = cut.InvokeAsync(() => czas.Instance.ChangeAfterKeyPressHandlerAsync("1:00", KeyPress.For(KeyCode.Enter)));
        var second = cut.InvokeAsync(() => czas.Instance.ChangeAfterKeyPressHandlerAsync("1:00", KeyPress.For(KeyCode.Enter)));
        await Task.WhenAll(first, second);

        WaitForMessage(cut, "success", "Zapisano Wpis: 03.09.2026, RDZ, 1h.");
        Assert.Single(await EntriesAsync());
    }

    /// <summary>The screen in MainLayout, as the router renders it: "?" of the top bar, message area.</summary>
    private IRenderedComponent<MainLayout> RenderScreen()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, b =>
        {
            b.OpenComponent<NewEntryPage>(0);
            b.CloseComponent();
        }));
        // The options are loaded from the database; the form appears after them.
        cut.WaitForElement("form.entry-form", TimeSpan.FromSeconds(5));
        return cut;
    }

    private static async Task FillAsync(
        IRenderedComponent<MainLayout> cut, ProjectOption project, WorkTypeOption workType, string description, string? czas = null)
    {
        await cut.InvokeAsync(() => ProjectList(cut).Instance.ValueChanged.InvokeAsync(project));
        await cut.InvokeAsync(() => WorkTypeList(cut).Instance.ValueChanged.InvokeAsync(workType));
        await cut.InvokeAsync(() => Description(cut).Instance.ValueChanged.InvokeAsync(description));
        if (czas is not null)
        {
            await cut.InvokeAsync(() => Czas(cut).Instance.ValueChanged.InvokeAsync(czas));
        }
    }

    /// <summary>Enter in a text field: the Fluent UI script sends the field's current text with the key (ChangeAfterKeyPress).</summary>
    private static Task EnterAsync(IRenderedComponent<MainLayout> cut, IRenderedComponent<FluentTextInput> input, string text)
    {
        Assert.Equal([KeyPress.For(KeyCode.Enter)], input.Instance.ChangeAfterKeyPress ?? []);
        return cut.InvokeAsync(() => input.Instance.ChangeAfterKeyPressHandlerAsync(text, KeyPress.For(KeyCode.Enter)));
    }

    private static IRenderedComponent<FluentDatePicker<DateOnly?>> DatePicker(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponent<FluentDatePicker<DateOnly?>>();

    private static IRenderedComponent<FluentCombobox<ProjectOption, ProjectOption?>> ProjectList(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponent<FluentCombobox<ProjectOption, ProjectOption?>>();

    private static IRenderedComponent<FluentCombobox<WorkTypeOption, WorkTypeOption?>> WorkTypeList(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponent<FluentCombobox<WorkTypeOption, WorkTypeOption?>>();

    private static IRenderedComponent<FluentTextInput> Description(IRenderedComponent<MainLayout> cut) => TextInput(cut, "entry-description");

    private static IRenderedComponent<FluentTextInput> Czas(IRenderedComponent<MainLayout> cut) => TextInput(cut, "entry-czas");

    private static IRenderedComponent<FluentTextInput> TextInput(IRenderedComponent<MainLayout> cut, string cssClass) =>
        cut.FindComponents<FluentTextInput>().Single(i => i.Instance.Class == cssClass);

    private void WaitForMessage(IRenderedComponent<MainLayout> cut, string intent, string text) =>
        cut.WaitForAssertion(() => Assert.Contains($"{intent}: {text}", Messages(cut)), TimeSpan.FromSeconds(5));

    private List<string> Messages(IRenderedComponent<MainLayout> cut) =>
    [
        .. cut.FindAll("fluent-message-bar").Select(b => $"{b.GetAttribute("intent")}: {b.TextContent.Trim()}"),
        .. _errors.Entries.Select(e => $"log: {e.Exception}"),
    ];

    private async Task<List<TimeEntry>> EntriesAsync()
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        return await db.TimeEntries.AsNoTracking().ToListAsync();
    }

    /// <summary>Active RDZ and ABC, inactive NIE; active Development, inactive Archiwalny. Returns the active options.</summary>
    private async Task<(Dictionary<string, ProjectOption> Projects, Dictionary<string, WorkTypeOption> WorkTypes)> SeedAsync(CancellationToken cancellationToken)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        db.Projects.AddRange(
            new Project { Code = "RDZ", Name = "Rozwój" },
            new Project { Code = "ABC", Name = "Abecadło" },
            new Project { Code = "NIE", Name = "Nieaktywny", IsActive = false });
        db.WorkTypes.AddRange(
            new WorkType { Name = "Development" },
            new WorkType { Name = "Archiwalny", IsActive = false });
        await db.SaveChangesAsync(cancellationToken);
        var projects = await db.Projects.Where(p => p.IsActive)
            .Select(p => new ProjectOption(p.Id, p.Code, p.Name, p.IsActive)).ToListAsync(cancellationToken);
        var workTypes = await db.WorkTypes.Where(w => w.IsActive)
            .Select(w => new WorkTypeOption(w.Id, w.Name, w.IsActive)).ToListAsync(cancellationToken);
        return (projects.ToDictionary(p => p.Code), workTypes.ToDictionary(w => w.Name));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
