using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Pomoc;
using RejestrCzasu.Features.Projekty;
using RejestrCzasu.Features.TypyPracy;
using RejestrCzasu.Features.Wpisy;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;
using RejestrCzasu.Ui;
using FilterSelect = Microsoft.FluentUI.AspNetCore.Components.FluentSelect<
    RejestrCzasu.Features.Wpisy.EntriesPage.FilterOption, RejestrCzasu.Features.Wpisy.EntriesPage.FilterOption>;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Lista wpisów screen (R8, R17, R2, R26, AD-9) with the edit, copy and delete of an Entry (R6, R7, AD-10) in bUnit with the real stores on the test database and a fixed clock.
/// </summary>
public sealed class EntriesPageTests : BunitContext, IAsyncLifetime
{
    // 22:30 UTC on 2 September is already 3 September in Poland (CEST, UTC+2).
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 22, 30, 0, TimeSpan.Zero);

    private readonly PostgresFixture _postgres;
    private readonly CapturingLogger<AppErrorBoundary> _errors = new();
    private readonly EntryDateMemory _dateMemory = new();
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
        Services.AddScoped(_ => new DictionaryOptions(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        Services.AddScoped(_ => _postgres.CreateLoggedInCurrentUser(_user));
        Services.AddSingleton(_dateMemory);
        Services.AddScoped<EntryDialogs>();
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
    public async Task Open_CurrentMonth_NewestFirst_CountAndSum_EodPageWithReport_HelpTopic()
    {
        await SeedAsync();

        var cut = RenderScreen();

        Assert.Equal("Lista wpisów", cut.Find(".eod-page h1").TextContent);
        Assert.Equal(["Data", "Dzień", "Projekt", "Typ pracy", "Opis", "Czas"], Headers(cut));
        // The screen filters lie in the filter bar of the card, above the report.
        var bar = cut.Find(".eod-page .eod-card > .eod-formbar.eod-filterbar");
        Assert.Equal(4, bar.QuerySelectorAll(".entries-from, .entries-to, .entries-project, .entries-work-type").Length);
        Assert.Equal("/pomoc/wpisy", cut.Find(".app-help").GetAttribute("href"));
        var pickers = cut.FindComponents<FluentDatePicker<DateOnly?>>();
        Assert.Equal([new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30)], pickers.Select(p => p.Instance.Value));
        Assert.All(pickers, p => Assert.Equal(DatePickerRenderStyle.Native, p.Instance.RenderStyle));
        cut.WaitForAssertion(() => Assert.Equal(["Koniec września", "Drugi 1 września", "Pierwszy 1 września"], Descriptions(cut)));
        Assert.Equal(["30.09.2026", "Środa", "OLD", "Development", "Koniec września", "24h"],
            Rows(cut)[0].QuerySelectorAll("td").Take(6).Select(td => td.TextContent.Trim()));
        Assert.Equal("3 pozycje · Wpisy: 3, suma Czasu: 25h 30m", Summary(cut));
        Assert.Contains("Eksportuj", cut.Markup);
        Assert.DoesNotContain("Cudzy", cut.Markup);
    }

    [Fact]
    public async Task Options_AllFirst_IncludeInactive_NeverAnotherUsers()
    {
        await SeedAsync();

        var cut = RenderScreen();

        Assert.Equal(["Wszystkie", "OLD - Stary (nieaktywny)", "RDZ - Rozwój"], Options(ProjectSelect(cut)));
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

        await SelectAsync(cut, ProjectSelect(cut), "RDZ - Rozwój");
        cut.WaitForAssertion(() => Assert.Equal(["Drugi 1 września", "Pierwszy 1 września"], Descriptions(cut)));
        Assert.Equal("2 pozycje · Wpisy: 2, suma Czasu: 1h 30m", Summary(cut));

        await SetDateAsync(cut, 0, new DateOnly(2026, 8, 1));
        await SetDateAsync(cut, 1, new DateOnly(2026, 8, 31));
        cut.WaitForAssertion(() => Assert.Equal(["Sierpień"], Descriptions(cut)));
        Assert.Equal("1 pozycja · Wpisy: 1, suma Czasu: 2h", Summary(cut));

        await SelectAsync(cut, WorkTypeSelect(cut), "Spotkanie");
        cut.WaitForAssertion(() => Assert.Empty(Descriptions(cut)));
        Assert.Equal("0 pozycji · Wpisy: 0, suma Czasu: 0h", Summary(cut));
    }

    [Fact]
    public async Task InactiveProject_Filter_ShowsItsEntries()
    {
        await SeedAsync();
        var cut = RenderScreen();

        await SelectAsync(cut, ProjectSelect(cut), "OLD - Stary (nieaktywny)");

        cut.WaitForAssertion(() => Assert.Equal(["Koniec września"], Descriptions(cut)));
        Assert.Equal("1 pozycja · Wpisy: 1, suma Czasu: 24h", Summary(cut));
    }

    [Fact]
    public async Task EmptyFrom_ShowsEverythingUpToTo()
    {
        await SeedAsync();
        var cut = RenderScreen();

        await SetDateAsync(cut, 0, null);

        cut.WaitForAssertion(() => Assert.Equal(4, Descriptions(cut).Count));
        Assert.Equal("4 pozycje · Wpisy: 4, suma Czasu: 27h 30m", Summary(cut));
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
        Assert.Equal("Data „Od” jest późniejsza niż „Do”.", cut.Find(".eod-filterbar .entries-filter-error").TextContent);
        Assert.Equal("alert", cut.Find(".entries-filter-error").GetAttribute("role"));
        Assert.Equal("0 pozycji · Wpisy: 0, suma Czasu: 0h", Summary(cut));

        await SetDateAsync(cut, 1, new DateOnly(2026, 9, 30));
        cut.WaitForAssertion(() => Assert.Equal(["Koniec września"], Descriptions(cut)));
        Assert.Empty(cut.FindAll(".entries-filter-error"));
    }

    [Fact]
    public async Task NoEntries_EmptyGrid_ZeroSummary()
    {
        var cut = RenderScreen();

        cut.WaitForAssertion(() => Assert.Equal("0 pozycji · Wpisy: 0, suma Czasu: 0h", Summary(cut)));
        Assert.Empty(Descriptions(cut));
    }

    [Fact]
    public async Task Rows_HaveEditCopyDeleteIcons()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        Assert.All(Rows(cut), row => Assert.Equal(["Edytuj", "Kopiuj", "Usuń"], ActionLabels(row)));
        Assert.All(Rows(cut)[0].QuerySelectorAll("fluent-button[aria-label]"), b => Assert.Equal("subtle", b.GetAttribute("appearance")));
    }

    [Fact]
    public async Task ColumnFilter_HidesRows_CounterAfterFilters_SumOfScreenFiltersUnchanged()
    {
        await SeedAsync();
        var cut = RenderScreen();

        await OpenColumnFilterAsync(cut, "Projekt");
        var old = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "OLD (1)");
        await cut.InvokeAsync(() => old.Instance.ValueChanged.InvokeAsync(false));

        Assert.Equal(["Drugi 1 września", "Pierwszy 1 września"], Descriptions(cut));
        Assert.Equal("2 pozycje · Wpisy: 3, suma Czasu: 25h 30m · po filtrach, z 3", Summary(cut));
    }

    [Fact]
    public async Task SortByCzas_OrdersByMinutes_NotByText()
    {
        await SeedAsync();
        await AddEntriesAsync(new DateOnly(2026, 9, 15), ("Dziesięć godzin", 600), ("Kwadranse", 45), ("Dwie godziny", 120));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(6, Descriptions(cut).Count));

        var czas = cut.FindAll("thead th [col-sort-button]").Single(b => b.TextContent.Trim().StartsWith("Czas", StringComparison.Ordinal));
        await cut.InvokeAsync(() => czas.Click());

        cut.WaitForAssertion(() => Assert.Equal(["30m", "45m", "1h", "2h", "10h", "24h"], Column(cut, 5)));
    }

    [Fact]
    public async Task SameDay_HigherIdFirst_DateColumnFormatted()
    {
        await SeedAsync();
        await AddEntriesAsync(new DateOnly(2026, 9, 15), ("Rano", 60), ("Wieczorem", 60));
        var cut = RenderScreen();

        cut.WaitForAssertion(() => Assert.Equal(
            ["Koniec września", "Wieczorem", "Rano", "Drugi 1 września", "Pierwszy 1 września"], Descriptions(cut)));
        Assert.Equal(["30.09.2026", "15.09.2026", "15.09.2026", "01.09.2026", "01.09.2026"], Column(cut, 0));
    }

    [Fact]
    public void Report_StaticDefinition_ListColumnsByEntryRowProperties_IdOutsideColumns()
    {
        Assert.Equal(HelpTopics.Wpisy, EntriesReport.Report.Key);
        Assert.All(EntriesReport.Report.Columns, c => Assert.Equal(ColumnType.List, c.Type));
        Assert.Equal(
            [nameof(EntryRow.Date), nameof(EntryRow.DayName), nameof(EntryRow.ProjectCode), nameof(EntryRow.WorkTypeName),
                nameof(EntryRow.Description), nameof(EntryRow.Minutes)],
            EntriesReport.Report.Columns.Select(c => c.Key));
        var row = EntriesReport.ToRow(new EntryRow(7, new DateOnly(2026, 9, 3), 1, "RDZ", 2, "Development", "Opis", 90));
        Assert.Equal(7, row["Id"]);
        Assert.Equal(new DateOnly(2026, 9, 3), row[nameof(EntryRow.Date)]);
        Assert.Equal(90, row[nameof(EntryRow.Minutes)]);
        Assert.DoesNotContain(EntriesReport.Report.Columns, c => c.Key == "Id");
    }

    [Fact]
    public async Task Edit_PastMonth_ShowsSavedValues_SavesEveryChange_ReloadsList()
    {
        var ids = await SeedAsync();
        await using (var db = await _postgres.CreateUserContextAsync(_user))
        {
            db.Projects.Add(new Project { Code = "ABC", Name = "Abecadło" });
            await db.SaveChangesAsync(Ct);
        }
        var cut = RenderScreen();
        await SetDateAsync(cut, 0, new DateOnly(2026, 8, 1));
        cut.WaitForAssertion(() => Assert.Equal(4, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Sierpień", "Edytuj");

        Assert.Contains("Edycja Wpisu", cut.Find("fluent-dialog").TextContent);
        // Zapisz first, Anuluj next to it in the one button row of EntryForm.
        Assert.Equal(["Zapisz", "Anuluj"], cut.FindAll("fluent-dialog .entry-form-buttons fluent-button").Select(b => b.TextContent.Trim()));
        Assert.Equal(new DateOnly(2026, 8, 20), DialogDate(cut).Instance.Value);
        Assert.Equal("RDZ", ProjectList(cut).Instance.Value?.Code);
        Assert.Equal("Development", WorkTypeList(cut).Instance.Value?.Name);
        Assert.Equal(("Sierpień", "2h"), (TextInput(cut, "entry-description").Instance.Value, TextInput(cut, "entry-czas").Instance.Value));

        var abc = ProjectList(cut).Instance.Items!.Single(p => p.Code == "ABC");
        var spotkanie = WorkTypeList(cut).Instance.Items!.Single(w => w.Name == "Spotkanie");
        await cut.InvokeAsync(() => DialogDate(cut).Instance.ValueChanged.InvokeAsync(new DateOnly(2026, 8, 21)));
        await cut.InvokeAsync(() => ProjectList(cut).Instance.ValueChanged.InvokeAsync(abc));
        await cut.InvokeAsync(() => WorkTypeList(cut).Instance.ValueChanged.InvokeAsync(spotkanie));
        await cut.InvokeAsync(() => TextInput(cut, "entry-description").Instance.ValueChanged.InvokeAsync("Sierpień poprawiony"));
        await cut.InvokeAsync(() => TextInput(cut, "entry-czas").Instance.ValueChanged.InvokeAsync("3h 15m"));
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "success", "Zapisano Wpis: 21.08.2026, ABC, 3h 15m.");
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")));
        // The bar is shown by EntryDialogs after the window has closed, in the message section of the page.
        Assert.Contains("Zapisano Wpis: 21.08.2026", cut.Find(".eod-page .eod-messages").TextContent);
        var entry = await EntryAsync(ids["Sierpień"]);
        Assert.Equal(
            (new DateOnly(2026, 8, 21), abc.Id, spotkanie.Id, "Sierpień poprawiony", 195),
            (entry!.Date, entry.ProjectId, entry.WorkTypeId, entry.Description, entry.Minutes));
        cut.WaitForAssertion(() => Assert.Contains("Sierpień poprawiony", Descriptions(cut)));
        Assert.Equal("ABC", Row(cut, "Sierpień poprawiony").QuerySelectorAll("td")[2].TextContent);
        Assert.Equal("4 pozycje · Wpisy: 4, suma Czasu: 28h 45m", Summary(cut));
        Assert.Equal(4, await CountAsync());
    }

    [Fact]
    public async Task Edit_WorkTypeDeletedBeforeSave_WindowStaysOpenWithTypedValues_Error_OptionsReloaded()
    {
        var ids = await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Drugi 1 września", "Edytuj");
        var spotkanie = WorkTypeList(cut).Instance.Items!.Single(w => w.Name == "Spotkanie");
        await cut.InvokeAsync(() => WorkTypeList(cut).Instance.ValueChanged.InvokeAsync(spotkanie));
        await cut.InvokeAsync(() => TextInput(cut, "entry-czas").Instance.ValueChanged.InvokeAsync("5h"));
        // Spotkanie has no Entries, so it can be deleted in another window before Zapisz.
        await using (var db = await _postgres.CreateUserContextAsync(_user))
        {
            db.WorkTypes.Remove(await db.WorkTypes.SingleAsync(w => w.Id == spotkanie.Id, Ct));
            await db.SaveChangesAsync(Ct);
        }
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());

        // In the window itself (AD-10), not in the page bar under the modal.
        cut.WaitForAssertion(() => Assert.Contains("Wybrany Projekt lub Typ pracy już nie istnieje.",
            cut.Find("fluent-dialog .entry-dialog-error").TextContent), TimeSpan.FromSeconds(5));
        Assert.Equal("alert", cut.Find(".entry-dialog-error").GetAttribute("role"));
        Assert.DoesNotContain(cut.FindAll(".eod-messages fluent-message-bar"), b => b.TextContent.Contains("już nie istnieje"));
        Assert.NotEmpty(cut.FindAll("fluent-dialog"));
        Assert.Equal("5h", TextInput(cut, "entry-czas").Instance.Value);
        Assert.Equal("Drugi 1 września", TextInput(cut, "entry-description").Instance.Value);
        cut.WaitForAssertion(() => Assert.DoesNotContain("Spotkanie", WorkTypeList(cut).Instance.Items!.Select(w => w.Name)));
        Assert.Equal(60, (await EntryAsync(ids["Drugi 1 września"]))!.Minutes);
    }

    [Fact]
    public async Task Edit_SaveErrorThenInvalidSubmit_ClearsErrorInWindow()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Drugi 1 września", "Edytuj");
        var spotkanie = WorkTypeList(cut).Instance.Items!.Single(w => w.Name == "Spotkanie");
        await cut.InvokeAsync(() => WorkTypeList(cut).Instance.ValueChanged.InvokeAsync(spotkanie));
        await using (var db = await _postgres.CreateUserContextAsync(_user))
        {
            db.WorkTypes.Remove(await db.WorkTypes.SingleAsync(w => w.Id == spotkanie.Id, Ct));
            await db.SaveChangesAsync(Ct);
        }
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());
        cut.WaitForElement("fluent-dialog .entry-dialog-error", TimeSpan.FromSeconds(5));

        await cut.InvokeAsync(() => TextInput(cut, "entry-czas").Instance.ValueChanged.InvokeAsync(""));
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Assert.Contains(EntryFormValidator.CzasMessage, cut.Markup));
        Assert.Empty(cut.FindAll(".entry-dialog-error"));
        Assert.NotEmpty(cut.FindAll("fluent-dialog"));
    }

    [Fact]
    public async Task Edit_InactiveProject_IsSelectedAndOffered()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Koniec września", "Edytuj");

        Assert.Equal("OLD", ProjectList(cut).Instance.Value?.Code);
        Assert.Contains("OLD", ProjectList(cut).Instance.Items!.Select(p => p.Code));
    }

    [Fact]
    public async Task Copy_WithRememberedDate_TakesItAndOriginalValues_SavesNewEntryOnlyOnZapisz()
    {
        var ids = await SeedAsync();
        _dateMemory.Remember(_user, new DateOnly(2026, 9, 10));
        var cut = RenderScreen();
        await SetDateAsync(cut, 0, new DateOnly(2026, 8, 1));
        cut.WaitForAssertion(() => Assert.Equal(4, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Sierpień", "Kopiuj");

        Assert.Contains("Kopia Wpisu", cut.Find("fluent-dialog").TextContent);
        Assert.Equal(new DateOnly(2026, 9, 10), DialogDate(cut).Instance.Value);
        Assert.Equal(("RDZ", "Development"), (ProjectList(cut).Instance.Value?.Code, WorkTypeList(cut).Instance.Value?.Name));
        Assert.Equal(("Sierpień", "2h"), (TextInput(cut, "entry-description").Instance.Value, TextInput(cut, "entry-czas").Instance.Value));
        Assert.Equal(4, await CountAsync());

        await cut.InvokeAsync(() => DialogDate(cut).Instance.ValueChanged.InvokeAsync(new DateOnly(2026, 9, 11)));
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "success", "Zapisano Wpis: 11.09.2026, RDZ, 2h.");
        cut.WaitForAssertion(() => Assert.Equal(5, Descriptions(cut).Count));
        Assert.Equal(5, await CountAsync());
        Assert.Equal(new DateOnly(2026, 8, 20), (await EntryAsync(ids["Sierpień"]))!.Date);
        // The copy window never changes the date remembered by Nowy wpis.
        Assert.Equal(new DateOnly(2026, 9, 10), _dateMemory.Find(_user));
    }

    [Fact]
    public async Task Copy_WithoutRememberedDate_TakesTodayInPoland()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Drugi 1 września", "Kopiuj");

        Assert.Equal(new DateOnly(2026, 9, 3), DialogDate(cut).Instance.Value);
        Assert.Null(_dateMemory.Find(_user));
    }

    [Fact]
    public async Task Copy_InactiveProject_LeavesFieldEmpty_NotOffered()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Koniec września", "Kopiuj");

        Assert.Null(ProjectList(cut).Instance.Value);
        Assert.DoesNotContain("OLD", ProjectList(cut).Instance.Items!.Select(p => p.Code));
        Assert.Equal("Development", WorkTypeList(cut).Instance.Value?.Name);
        Assert.Equal(("Koniec września", "24h"), (TextInput(cut, "entry-description").Instance.Value, TextInput(cut, "entry-czas").Instance.Value));
    }

    [Theory]
    [InlineData("Edytuj")]
    [InlineData("Kopiuj")]
    public async Task Cancel_AfterChanges_SavesNothing(string action)
    {
        var ids = await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Drugi 1 września", action);
        await cut.InvokeAsync(() => TextInput(cut, "entry-czas").Instance.ValueChanged.InvokeAsync("5h"));
        await cut.Find(".entry-dialog-cancel").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")));
        Assert.Equal(4, await CountAsync());
        Assert.Equal(60, (await EntryAsync(ids["Drugi 1 września"]))!.Minutes);
        Assert.Empty(cut.FindAll("fluent-message-bar"));
    }

    [Fact]
    public async Task Escape_ClosesWindow_SavesNothing()
    {
        var ids = await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        var action = OpenWindow(cut, "Drugi 1 września", "Edytuj");
        await cut.InvokeAsync(() => TextInput(cut, "entry-czas").Instance.ValueChanged.InvokeAsync("5h"));
        var dialogId = cut.FindComponent<EntryDialog>().Instance.Dialog.Id;
        // Escape closes the native dialog; the web component reports it as "beforetoggle" from open to closed.
        await cut.Find("fluent-dialog").TriggerEventAsync("ondialogbeforetoggle",
            new DialogToggleEventArgs { Id = dialogId, Type = "beforetoggle", OldState = "open", NewState = "closed" });

        // The row action ends once the window has closed as cancelled.
        await action.WaitAsync(TimeSpan.FromSeconds(5), Ct);
        Assert.Equal(60, (await EntryAsync(ids["Drugi 1 września"]))!.Minutes);
        Assert.Empty(cut.FindAll("fluent-message-bar"));
    }

    [Fact]
    public async Task Delete_Confirmed_RemovesEntry_ReloadsList()
    {
        var ids = await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Koniec września", "Usuń", ".eod-confirm-primary");
        Assert.Contains("Usunąć Wpis z 30.09.2026 (OLD, 24h): Koniec września?", cut.Find("fluent-dialog").TextContent);
        await cut.Find(".eod-confirm-primary").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "success", "Usunięto Wpis: 30.09.2026, OLD, 24h.");
        cut.WaitForAssertion(() => Assert.Equal(["Drugi 1 września", "Pierwszy 1 września"], Descriptions(cut)));
        Assert.Equal("2 pozycje · Wpisy: 2, suma Czasu: 1h 30m", Summary(cut));
        Assert.Null(await EntryAsync(ids["Koniec września"]));
    }

    [Theory]
    [InlineData("Edytuj")]
    [InlineData("Kopiuj")]
    public async Task EditAndCopy_PresetProjectAndWorkType_ShowTheirTextThroughEodUi(string action)
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Drugi 1 września", action);

        var project = ProjectList(cut).Instance.Value!;
        var workType = WorkTypeList(cut).Instance.Value!;
        cut.WaitForAssertion(() => Assert.Equal(
            [$"{project.Code} - {project.Name}", workType.Name],
            JSInterop.Invocations.Where(i => i.Identifier == "EodUI.showSelectedText").Select(i => (string?)i.Arguments[1])));
        // The workaround lives only in eod.js of the eod-ui copy (AD-1): no module of EntryForm.
        Assert.DoesNotContain(JSInterop.Invocations, i => i.Identifier == "import" && i.Arguments.Any(a => a is string path && path.Contains("EntryForm", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Delete_EnterInTheWindow_DoesNotConfirm_EntryStays()
    {
        await SeedAsync();
        JSInterop.Setup<bool>("Microsoft.FluentUI.Blazor.Components.Dialog.ShouldHandleShortcut", _ => true).SetResult(true);
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        var action = OpenWindow(cut, "Koniec września", "Usuń", ".eod-confirm-secondary");
        Assert.True(cut.Find(".eod-confirm-secondary").HasAttribute("autofocus"));
        await cut.Find("fluent-dialog").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.False(action.IsCompleted);
        Assert.NotEmpty(cut.FindAll("fluent-dialog"));
        Assert.Equal(4, await CountAsync());
    }

    [Fact]
    public async Task Delete_Cancelled_KeepsEntry()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Koniec września", "Usuń", ".eod-confirm-secondary");
        await cut.Find(".eod-confirm-secondary").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")));
        Assert.Equal(4, await CountAsync());
        Assert.Equal(3, Descriptions(cut).Count);
    }

    [Fact]
    public async Task Delete_SecondClickWhileConfirmationOpen_OpensNoSecondWindow()
    {
        await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Koniec września", "Usuń", ".eod-confirm-secondary");
        await Action(cut, "Koniec września", "Usuń").ClickAsync(new MouseEventArgs());

        Assert.Single(cut.FindAll("fluent-dialog"));
        Assert.Single(cut.FindAll(".eod-confirm-secondary"));
    }

    [Theory]
    [InlineData("Edytuj")]
    [InlineData("Kopiuj")]
    [InlineData("Usuń")]
    public async Task EntryDeletedInTheMeantime_Warning_ListReloaded(string action)
    {
        var ids = await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));
        await DeleteDirectlyAsync(ids["Koniec września"]);

        await Action(cut, "Koniec września", action).ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "warning", "Ten Wpis już nie istnieje. Lista została wczytana ponownie.");
        cut.WaitForAssertion(() => Assert.Equal(["Drugi 1 września", "Pierwszy 1 września"], Descriptions(cut)));
        Assert.Empty(cut.FindAll("fluent-dialog"));
    }

    [Fact]
    public async Task Edit_EntryDeletedBeforeSave_Warning_WindowClosed_ListReloaded()
    {
        var ids = await SeedAsync();
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(3, Descriptions(cut).Count));

        _ = OpenWindow(cut, "Koniec września", "Edytuj");
        await DeleteDirectlyAsync(ids["Koniec września"]);
        await cut.Find(".entry-save").ClickAsync(new MouseEventArgs());

        WaitForMessage(cut, "warning", "Ten Wpis już nie istnieje. Lista została wczytana ponownie.");
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")));
        cut.WaitForAssertion(() => Assert.Equal(2, Descriptions(cut).Count));
        Assert.Equal(3, await CountAsync());
    }

    [Fact]
    public async Task AnotherUsersEntryId_IsNotFound_AndUntouched()
    {
        await SeedAsync();
        var cut = RenderScreen();
        int otherId;
        await using (var other = await _postgres.CreateUserContextAsync(_otherUser))
        {
            otherId = (await other.TimeEntries.SingleAsync(Ct)).Id;
        }
        var dialogs = Services.GetRequiredService<EntryDialogs>();

        Assert.True(await cut.InvokeAsync(() => dialogs.DeleteAsync(otherId)));
        Assert.True(await cut.InvokeAsync(() => dialogs.EditAsync(otherId)));
        Assert.True(await cut.InvokeAsync(() => dialogs.CopyAsync(otherId)));

        Assert.Empty(cut.FindAll("fluent-dialog"));
        await using var check = await _postgres.CreateUserContextAsync(_otherUser);
        Assert.Single(await check.TimeEntries.ToListAsync(Ct));
    }

    [Fact]
    public void Summary_TwoEntriesOf24h_Is48h()
    {
        var day = new DateOnly(2026, 9, 3);
        EntryRow[] rows = [new(1, day, 1, "RDZ", 1, "Development", "A", 1440), new(2, day, 1, "RDZ", 1, "Development", "B", 1440)];

        Assert.Equal("Wpisy: 2, suma Czasu: 48h", EntriesReport.Summary(rows));
    }

    [Fact]
    public async Task Pages_30PerPage_FooterForAllRows_ScreenFilterChange_BackToPage1()
    {
        await SeedAsync();
        await AddEntriesAsync(new DateOnly(2026, 9, 2), [.. Enumerable.Range(1, 40).Select(i => ($"Seria {i}", 90))]);
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(30, Rows(cut).Count));

        Assert.Equal(("Pozycje 1–30 z 43", "Strona 1 z 2"), PaginatorTexts(cut));
        Assert.Equal("43 pozycje · Wpisy: 43, suma Czasu: 85h 30m", Summary(cut));

        await GoToNextPageAsync(cut);
        Assert.Equal(13, Rows(cut).Count);
        Assert.Equal("Strona 2 z 2", PaginatorTexts(cut).Page);

        await SelectAsync(cut, ProjectSelect(cut), "RDZ - Rozwój");
        cut.WaitForAssertion(() => Assert.Equal(("Pozycje 1–30 z 42", "Strona 1 z 2"), PaginatorTexts(cut)));
        Assert.Equal("Seria 40", Descriptions(cut)[0]);
    }

    [Fact]
    public async Task Eksportuj_ScreenFilter_ColumnFilter_Search_AllPages_XlsxWithDateCellsAndNumberHours()
    {
        await SeedAsync();
        await AddEntriesAsync(new DateOnly(2026, 9, 2), [.. Enumerable.Range(1, 40).Select(i => ($"Seria {i}", 90))]);
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(30, Rows(cut).Count));

        await SelectAsync(cut, ProjectSelect(cut), "RDZ - Rozwój");
        cut.WaitForAssertion(() => Assert.StartsWith("42 pozycje", Summary(cut)));
        await OpenColumnFilterAsync(cut, "Czas");
        var checkbox = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "30m (1)");
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(false));
        await cut.InvokeAsync(() => cut.FindComponents<FluentTextInput>()
            .Single(i => i.Instance.AriaLabel == "Szukaj we wszystkich kolumnach").Instance.ValueChanged.InvokeAsync("Seria"));
        Assert.Equal(("Pozycje 1–30 z 40", "Strona 1 z 2"), PaginatorTexts(cut));

        await cut.InvokeAsync(() => ExportButton(cut).Click());

        var download = Assert.Single(JSInterop.Invocations, i => i.Identifier == "EodUI.downloadStream");
        // 22:30 UTC on 2 September is 3 September in Poland.
        Assert.Equal("wpisy-2026-09-03.xlsx", download.Arguments[0]);
        Assert.Equal(EntriesXlsx.ContentType, download.Arguments[2]);
        using var workbook = XlsxDownload.Open(download);
        var sheet = workbook.Worksheet(EntriesXlsx.SheetName);
        Assert.Equal(["Data", "Dzień", "Projekt", "Typ pracy", "Opis", "Godziny"], sheet.Row(1).CellsUsed().Select(c => c.GetText()));
        Assert.Equal(41, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal(ClosedXML.Excel.XLDataType.DateTime, sheet.Cell(2, 1).DataType);
        Assert.Equal(new DateTime(2026, 9, 2), sheet.Cell(2, 1).GetDateTime());
        Assert.Equal(["Środa", "RDZ", "Development", "Seria 40"], new[] { 2, 3, 4, 5 }.Select(c => sheet.Cell(2, c).GetText()));
        Assert.Equal(ClosedXML.Excel.XLDataType.Number, sheet.Cell(2, 6).DataType);
        Assert.Equal(1.5, sheet.Cell(2, 6).GetDouble());
        Assert.Equal("Seria 1", sheet.Cell(41, 5).GetText());
        // Title and text of the message bar (UiMessages): the export counts all 40 rows, not the page.
        cut.WaitForAssertion(() => Assert.Contains(Messages(cut), m =>
            m.StartsWith("success: Wyeksportowano raport.", StringComparison.Ordinal)
            && m.EndsWith("wpisy-2026-09-03.xlsx: 40 wierszy, 6 kolumn.", StringComparison.Ordinal)), TimeSpan.FromSeconds(5));
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
        // The list is loaded from the database; the report shows skeleton rows until then.
        cut.WaitForAssertion(() =>
        {
            Assert.NotEmpty(cut.FindAll(".eod-page table"));
            Assert.Empty(cut.FindAll(".eod-skeleton-row"));
        }, TimeSpan.FromSeconds(5));
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

    private static IReadOnlyList<IElement> Rows(IRenderedComponent<MainLayout> cut) => cut.FindAll("tbody tr[hover]");

    private static List<string> Column(IRenderedComponent<MainLayout> cut, int index) =>
        Rows(cut).Select(tr => tr.QuerySelectorAll("td")[index].TextContent.Trim()).ToList();

    private static List<string> Headers(IRenderedComponent<MainLayout> cut) =>
        cut.FindAll("thead th[col-index] [col-sort-button]").Select(e => e.TextContent.Trim()).ToList();

    private static async Task OpenColumnFilterAsync(IRenderedComponent<MainLayout> cut, string label)
    {
        var button = cut.FindAll("th fluent-button").Single(b => b.GetAttribute("title")?.EndsWith($": {label}", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => button.Click());
    }

    private static IElement Row(IRenderedComponent<MainLayout> cut, string description) =>
        Rows(cut).Single(tr => tr.QuerySelectorAll("td")[4].TextContent.Trim() == description);

    private static IElement Action(IRenderedComponent<MainLayout> cut, string description, string label) =>
        Row(cut, description).QuerySelector($"fluent-button[aria-label='{label}']")!;

    private static List<string?> ActionLabels(IElement row) =>
        row.QuerySelectorAll("fluent-button[aria-label]").Select(b => b.GetAttribute("aria-label")).ToList();

    /// <summary>Clicks a row icon and waits for the window it opens (the action stays pending until the window closes).</summary>
    private static Task OpenWindow(IRenderedComponent<MainLayout> cut, string description, string label, string waitFor = ".entry-dialog-cancel")
    {
        var action = Action(cut, description, label).ClickAsync(new MouseEventArgs());
        cut.WaitForElement(waitFor, TimeSpan.FromSeconds(5));
        return action;
    }

    private static IRenderedComponent<FluentDatePicker<DateOnly?>> DialogDate(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponents<FluentDatePicker<DateOnly?>>().Single(p => p.Instance.Class == "entry-date");

    private static IRenderedComponent<FluentCombobox<ProjectOption, ProjectOption?>> ProjectList(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponent<FluentCombobox<ProjectOption, ProjectOption?>>();

    private static IRenderedComponent<FluentCombobox<WorkTypeOption, WorkTypeOption?>> WorkTypeList(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponent<FluentCombobox<WorkTypeOption, WorkTypeOption?>>();

    private static IRenderedComponent<FluentTextInput> TextInput(IRenderedComponent<MainLayout> cut, string cssClass) =>
        cut.FindComponents<FluentTextInput>().Single(i => i.Instance.Class == cssClass);

    private void WaitForMessage(IRenderedComponent<MainLayout> cut, string intent, string text) =>
        cut.WaitForAssertion(() => Assert.Contains($"{intent}: {text}", Messages(cut)), TimeSpan.FromSeconds(5));

    private List<string> Messages(IRenderedComponent<MainLayout> cut) =>
    [
        .. cut.FindAll("fluent-message-bar").Select(b => $"{b.GetAttribute("intent")}: {b.TextContent.Trim()}"),
        .. _errors.Entries.Select(e => $"log: {e.Exception}"),
    ];

    private async Task<TimeEntry?> EntryAsync(int id)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        return await db.TimeEntries.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id, Ct);
    }

    private async Task<int> CountAsync()
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        return await db.TimeEntries.CountAsync(Ct);
    }

    private async Task DeleteDirectlyAsync(int id)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        db.TimeEntries.Remove(await db.TimeEntries.SingleAsync(e => e.Id == id, Ct));
        await db.SaveChangesAsync(Ct);
    }

    private static List<string> Descriptions(IRenderedComponent<MainLayout> cut) => Column(cut, 4);

    private static IElement ExportButton(IRenderedComponent<MainLayout> cut) =>
        cut.FindAll(".eod-report .eod-formbar fluent-button").Single(b => b.TextContent.Trim() == "Eksportuj");

    private static (string Range, string Page) PaginatorTexts(IRenderedComponent<MainLayout> cut) =>
        (cut.Find(".eod-paginator .summary").TextContent.Trim(), cut.Find(".eod-paginator .pagination-text").TextContent.Trim());

    // The third button of FluentPaginator: first, previous, next, last.
    private static Task GoToNextPageAsync(IRenderedComponent<MainLayout> cut)
    {
        var next = cut.FindComponent<FluentPaginator>().FindComponents<FluentButton>()[2];
        return cut.InvokeAsync(() => next.Instance.OnClick.InvokeAsync());
    }

    /// <summary>The report footer: the counter of visible rows, then the screen's text of count and sum (AD-5).</summary>
    private static string Summary(IRenderedComponent<MainLayout> cut) => cut.Find(".eod-card-foot .eod-num").TextContent.Trim();

    /// <summary>Adds Entries of RDZ / Development on <paramref name="date"/>, one save each, so the Ids follow the order.</summary>
    private async Task AddEntriesAsync(DateOnly date, params (string Description, int Minutes)[] entries)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var rdz = await db.Projects.SingleAsync(p => p.Code == "RDZ", Ct);
        var development = await db.WorkTypes.SingleAsync(w => w.Name == "Development", Ct);
        foreach (var (description, minutes) in entries)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                Date = date, ProjectId = rdz.Id, WorkTypeId = development.Id, Description = description, Minutes = minutes,
            });
            await db.SaveChangesAsync(Ct);
        }
    }

    /// <summary>
    /// RDZ (active) and OLD (inactive); Development, Spotkanie and inactive Archiwalny. Entries: August RDZ 2h; September
    /// RDZ 30m and 1h on the 1st (in this order), OLD 24h on the 30th. Another User has its own CUD Entry in September.
    /// Returns the User's Entry ids by description.
    /// </summary>
    private async Task<Dictionary<string, int>> SeedAsync()
    {
        var ids = new Dictionary<string, int>();
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
                var entry = new TimeEntry
                {
                    Date = date, ProjectId = project.Id, WorkTypeId = development.Id, Description = description, Minutes = minutes,
                };
                db.TimeEntries.Add(entry);
                // One save per Entry, so the Ids follow the order above.
                await db.SaveChangesAsync(Ct);
                ids[description] = entry.Id;
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
        return ids;
    }
}
