using Bunit;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Pomoc;
using RejestrCzasu.Features.Wpisy;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Nowy wpis screen (R4, R5, R16, AD-8, AD-10) in bUnit with the real <see cref="EntryStore"/> and
/// <see cref="DictionaryOptions"/> on the test database and a clock set by the test; under the form the Entries of the
/// current Month (EntriesReport, AD-9).
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
    private readonly SettableTimeProvider _clock = new(Now);
    private int _user;

    public NewEntryPageTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        this.AddAppUi();
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AppErrorBoundary>>(_errors);
        Services.AddSingleton<TimeProvider>(_clock);
        Services.AddSingleton(_dateMemory);
        // Created on first use, after InitializeAsync has created the User.
        Services.AddScoped(_ => _postgres.CreateLoggedInCurrentUser(_user));
        Services.AddScoped(_ => new EntryStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped(_ => new DictionaryOptions(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped<EntryDialogs>();
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
    public void Open_TodayInPoland_EmptyFields_EodPageWithCard_HelpTopic()
    {
        var cut = RenderScreen();

        Assert.Equal("Nowy wpis", cut.Find(".eod-page h1").TextContent);
        Assert.Equal("Kolejne Wpisy samą klawiaturą: data, Projekt, Typ pracy, opis, Czas i Enter.",
            cut.Find(".eod-page-head .eod-caption").TextContent);
        Assert.NotNull(cut.Find(".eod-page .eod-card form.entry-form"));
        Assert.Equal(["Zapisz"], cut.FindAll(".entry-form-buttons fluent-button").Select(b => b.TextContent.Trim()));
        Assert.Equal("/pomoc/wpisy-nowy", cut.Find(".app-help").GetAttribute("href"));
        var date = DatePicker(cut).Instance;
        Assert.Equal(Today, date.Value);
        Assert.Equal(DatePickerRenderStyle.Native, date.RenderStyle);
        // No autofocus (blocked after FocusOnNavigate): the page focuses Data itself after the form's first render.
        Assert.False(date.Autofocus);
        cut.WaitForAssertion(() => Assert.Equal([EntryForm.DateFieldSelector], FocusedFields(cut)));
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
        // The bar lies in the message section of EodPage, under the header.
        Assert.Contains("Zapisano Wpis", cut.Find(".eod-page .eod-messages").TextContent);
        var entry = Assert.Single(await EntriesAsync());
        Assert.Equal(
            (_user, Today, projects["RDZ"].Id, workTypes["Development"].Id, "Spotkanie", 90),
            (entry.UserId, entry.Date, entry.ProjectId, entry.WorkTypeId, entry.Description, entry.Minutes));
        Assert.Equal(Today, DatePicker(cut).Instance.Value);
        Assert.Null(ProjectList(cut).Instance.Value);
        Assert.Null(WorkTypeList(cut).Instance.Value);
        Assert.Equal(("", ""), (Description(cut).Instance.Value, Czas(cut).Instance.Value));
        // Data at the start, Projekt after the save; the reload of the list does not move the focus.
        cut.WaitForAssertion(() => Assert.Equal(["Spotkanie"], Descriptions(cut)));
        Assert.Equal([EntryForm.DateFieldSelector, ProjectList(cut).Instance.Element.Id], FocusedFields(cut));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task MonthList_EntriesOfCurrentMonthOnly_NewestFirst_FooterSum_IconsAndOwnLayout()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 8, 31), "Sierpień", 120);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 9, 1), "Pierwszy", 30);
        await AddEntryAsync(projects["ABC"], workTypes["Development"], new DateOnly(2026, 9, 30), "Koniec", 1440);

        var cut = RenderScreen();

        cut.WaitForAssertion(() => Assert.Equal(["Koniec", "Pierwszy"], Descriptions(cut)));
        Assert.Equal(["30.09.2026", "01.09.2026"], Column(cut, 0));
        Assert.Equal(["24h", "30m"], Column(cut, 5));
        Assert.Equal("2 pozycje · Wpisy: 2, suma Czasu: 24h 30m", Summary(cut));
        Assert.All(Rows(cut), row => Assert.Equal(["Edytuj", "Kopiuj", "Usuń"],
            row.QuerySelectorAll("fluent-button[aria-label]").Select(b => b.GetAttribute("aria-label"))));
        Assert.Contains("Eksportuj", cut.Markup);
        // The list has its own layout key, not the one of Lista wpisów.
        Assert.Equal(HelpTopics.WpisyNowy, cut.FindComponent<EodReport>().Instance.LayoutKey);
        Assert.Equal("Wpisy bieżącego Miesiąca", cut.Find("table.fluent-data-grid").GetAttribute("aria-label"));
        Assert.Equal(EntriesReport.Report.Columns, EntriesReport.CurrentMonth.Columns);
        Assert.NotNull(cut.Find(".eod-page.eod-page-fill > .eod-card.eod-card-fill .eod-report"));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task Eksportuj_CurrentMonthOnly_PolishDateInFileName_DecimalHours_PageSize30()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 8, 31), "Sierpień", 120);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 9, 1), "Pierwszy; \"cytat\"", 90);
        await AddEntryAsync(projects["ABC"], workTypes["Development"], new DateOnly(2026, 9, 30), "Koniec", 45);
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(2, Descriptions(cut).Count));
        Assert.Equal(EntriesReport.PageSize, cut.FindComponent<EodReport>().Instance.PageSize);

        var export = cut.FindAll(".eod-report .eod-formbar fluent-button").Single(b => b.TextContent.Trim() == "Eksportuj");
        await cut.InvokeAsync(() => export.Click());

        var download = Assert.Single(JSInterop.Invocations, i => i.Identifier == "EodUI.downloadStream");
        Assert.Equal("wpisy-2026-09-03.xlsx", download.Arguments[0]);
        using var workbook = XlsxDownload.Open(download);
        var sheet = workbook.Worksheet(EntriesXlsx.SheetName);
        Assert.Equal(["Data", "Dzień", "Projekt", "Typ pracy", "Opis", "Godziny"], sheet.Row(1).CellsUsed().Select(c => c.GetText()));
        Assert.Equal(4, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal(new DateTime(2026, 9, 30), sheet.Cell(2, 1).GetDateTime());
        Assert.Equal(["Środa", "ABC", "Development", "Koniec"], new[] { 2, 3, 4, 5 }.Select(c => sheet.Cell(2, c).GetText()));
        Assert.Equal(0.75, sheet.Cell(2, 6).GetDouble());
        Assert.Equal(new DateTime(2026, 9, 1), sheet.Cell(3, 1).GetDateTime());
        Assert.Equal("Pierwszy; \"cytat\"", sheet.Cell(3, 5).GetText());
        Assert.Equal(1.5, sheet.Cell(3, 6).GetDouble());
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task SaveWithDateOfPreviousMonth_IsSaved_ButNotOnTheList()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 9, 2), "Wrzesień", 60);
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(["Wrzesień"], Descriptions(cut)));

        await cut.InvokeAsync(() => DatePicker(cut).Instance.ValueChanged.InvokeAsync(new DateOnly(2026, 8, 31)));
        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Sierpniowy");
        await EnterAsync(cut, Czas(cut), "1h");

        WaitForMessage(cut, "success", "Zapisano Wpis: 31.08.2026, RDZ, 1h.");
        Assert.Equal(2, (await EntriesAsync()).Count);
        Assert.Equal(["Wrzesień"], Descriptions(cut));
        Assert.Equal("1 pozycja · Wpisy: 1, suma Czasu: 1h", Summary(cut));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task MonthChangesWhileOpen_ListIsReadForTheMonthOfEveryLoad()
    {
        // 21:59 UTC on 30 September is 23:59 in Poland; two minutes later it is 1 October.
        _clock.UtcNow = new DateTimeOffset(2026, 9, 30, 21, 59, 0, TimeSpan.Zero);
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 9, 30), "Wrzesień", 60);
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(["Wrzesień"], Descriptions(cut)));

        _clock.UtcNow = new DateTimeOffset(2026, 9, 30, 22, 1, 0, TimeSpan.Zero);
        await cut.InvokeAsync(() => DatePicker(cut).Instance.ValueChanged.InvokeAsync(new DateOnly(2026, 10, 1)));
        await FillAsync(cut, projects["RDZ"], workTypes["Development"], "Październik");
        await EnterAsync(cut, Czas(cut), "2h");

        WaitForMessage(cut, "success", "Zapisano Wpis: 01.10.2026, RDZ, 2h.");
        cut.WaitForAssertion(() => Assert.Equal(["Październik"], Descriptions(cut)));
        Assert.Equal("1 pozycja · Wpisy: 1, suma Czasu: 2h", Summary(cut));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task RowDelete_Confirmed_ReloadsList()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 9, 1), "Pierwszy", 30);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 9, 2), "Drugi", 60);
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(["Drugi", "Pierwszy"], Descriptions(cut)));

        _ = RowAction(cut, "Drugi", "Usuń").ClickAsync(new MouseEventArgs());
        cut.WaitForElement(".eod-confirm-primary", TimeSpan.FromSeconds(5));
        await cut.Find(".eod-confirm-primary").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "success", "Usunięto Wpis: 02.09.2026, RDZ, 1h.");
        cut.WaitForAssertion(() => Assert.Equal(["Pierwszy"], Descriptions(cut)));
        Assert.Equal("1 pozycja · Wpisy: 1, suma Czasu: 30m", Summary(cut));
    }

    [Fact(Timeout = TestTimeoutMs)]
    public async Task RowEdit_Saved_ReloadsList()
    {
        var (projects, workTypes) = await SeedAsync(Xunit.TestContext.Current.CancellationToken);
        await AddEntryAsync(projects["RDZ"], workTypes["Development"], new DateOnly(2026, 9, 1), "Przed edycją", 30);
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(["Przed edycją"], Descriptions(cut)));

        _ = RowAction(cut, "Przed edycją", "Edytuj").ClickAsync(new MouseEventArgs());
        cut.WaitForElement(".entry-dialog-cancel", TimeSpan.FromSeconds(5));
        var dialogCzas = cut.FindComponents<FluentTextInput>().Last(i => i.Instance.Class == "entry-czas");
        await cut.InvokeAsync(() => dialogCzas.Instance.ValueChanged.InvokeAsync("45m"));
        await cut.Find("fluent-dialog .entry-save").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "success", "Zapisano Wpis: 01.09.2026, RDZ, 45m.");
        cut.WaitForAssertion(() => Assert.Equal(["45m"], Column(cut, 5)));
        Assert.Equal("1 pozycja · Wpisy: 1, suma Czasu: 45m", Summary(cut));
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
        Assert.Equal("RDZ - Rozwój", EntryForm.ProjectText(projects.Items!.Last()));
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
        Assert.Contains("już nie istnieje", cut.Find(".eod-page .eod-messages").TextContent);
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

    /// <summary>Every focus in order: Data by its selector (EodUI.focusElement), the other fields by their element id.</summary>
    private List<string?> FocusedFields(IRenderedComponent<MainLayout> cut) =>
        JSInterop.Invocations
            .Where(i => i.Identifier is "Blazor._internal.domWrapper.focus" or "EodUI.focusElement")
            .Select(i => i.Arguments[0] switch { ElementReference e => e.Id, string selector => selector, _ => null })
            .ToList();

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<MainLayout> cut) => cut.FindAll("tbody tr[hover]");

    private static List<string> Column(IRenderedComponent<MainLayout> cut, int index) =>
        Rows(cut).Select(tr => tr.QuerySelectorAll("td")[index].TextContent.Trim()).ToList();

    private static List<string> Descriptions(IRenderedComponent<MainLayout> cut) => Column(cut, 4);

    private static IElement RowAction(IRenderedComponent<MainLayout> cut, string description, string label) =>
        Rows(cut).Single(tr => tr.QuerySelectorAll("td")[4].TextContent.Trim() == description)
            .QuerySelector($"fluent-button[aria-label='{label}']")!;

    /// <summary>The report footer: the counter of visible rows, then the screen's text of count and sum (AD-5).</summary>
    private static string Summary(IRenderedComponent<MainLayout> cut) => cut.Find(".eod-card-foot .eod-num").TextContent.Trim();

    private async Task AddEntryAsync(ProjectOption project, WorkTypeOption workType, DateOnly date, string description, int minutes)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        db.TimeEntries.Add(new TimeEntry
        {
            Date = date, ProjectId = project.Id, WorkTypeId = workType.Id, Description = description, Minutes = minutes,
        });
        await db.SaveChangesAsync(Ct);
    }

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
}

/// <summary>A clock the test moves (e.g. over midnight at the end of a Month), for "today" in Poland.</summary>
internal sealed class SettableTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = utcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
