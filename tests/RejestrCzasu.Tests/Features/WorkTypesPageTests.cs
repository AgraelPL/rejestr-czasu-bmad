using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Features.TypyPracy;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Typy pracy screen (R1–R3, R17, AD-8, AD-9, AD-11) on EodPage with EodReport and the Work type window, in bUnit
/// with the real <see cref="WorkTypeStore"/> on the test database, so duplicates and Entry counts come from PostgreSQL
/// as in the application.
/// </summary>
public sealed class WorkTypesPageTests : BunitContext, IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private readonly CapturingLogger<AppErrorBoundary> _errors = new();
    private int _user;

    public WorkTypesPageTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        this.AddAppUi();
        // Created on first use, after InitializeAsync has created the User.
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AppErrorBoundary>>(_errors);
        Services.AddScoped(_ => new WorkTypeStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        AddAuthorization().SetAuthorized("ekran-typy-pracy");
        // The application renders its screens in an Interactive Server circuit.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _postgres.ResetAsync();
        _user = await _postgres.CreateUserAsync("ekran-typy-pracy");
    }

    private WorkTypeStore Store => new(_postgres.CreateLoggedInUserDbContextFactory(_user));

    [Fact]
    public async Task List_EodPageWithReport_StoreOrder_Counter_NoExport()
    {
        await SeedAsync(("Spotkanie", true), ("Analiza", false));

        var cut = RenderScreen();

        Assert.Equal("Typy pracy", cut.Find(".eod-page h1").TextContent);
        Assert.Equal("Dodaj Typ pracy", cut.Find(".eod-page-head .work-types-add").TextContent.Trim());
        Assert.Equal("/pomoc/typy-pracy", cut.Find(".app-help").GetAttribute("href"));
        Assert.Equal(["Nazwa", "Stan"], Headers(cut));
        Assert.Equal(["Analiza", "Spotkanie"], Names(cut));
        Assert.Equal(["Nieaktywny", "Aktywny"], Column(cut, 1));
        Assert.Equal("2 pozycje", Footer(cut));
        Assert.DoesNotContain("Eksport CSV", cut.Markup);
    }

    [Fact]
    public async Task Filter_StateNieaktywny_ShowsOnlyInactive_CounterAfterFilters()
    {
        await SeedAsync(("Spotkanie", true), ("Analiza", false));
        var cut = RenderScreen();

        await OpenColumnFilterAsync(cut, "Stan");
        var active = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "Aktywny (1)");
        await cut.InvokeAsync(() => active.Instance.ValueChanged.InvokeAsync(false));

        Assert.Equal(["Analiza"], Names(cut));
        Assert.Equal("1 pozycja · po filtrach, z 2", Footer(cut));
    }

    [Fact]
    public async Task Actions_SubtleIcons_DeactivateOnlyForActive_RestoreOnlyForInactive()
    {
        await SeedAsync(("Aktywny", true), ("Nieaktywny", false));

        var cut = RenderScreen();

        Assert.Equal(["Edytuj", "Dezaktywuj", "Usuń"], ActionLabels(Row(cut, "Aktywny")));
        Assert.Equal(["Edytuj", "Przywróć", "Usuń"], ActionLabels(Row(cut, "Nieaktywny")));
        Assert.All(Row(cut, "Aktywny").QuerySelectorAll("fluent-button[aria-label]"),
            b => Assert.Equal("subtle", b.GetAttribute("appearance")));
    }

    [Fact]
    public async Task Add_WindowWithFocusOnName_SavesTrimmedNameAsTyped_ClosesWindow_ShowsSuccess()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        Assert.Contains("Dodaj Typ pracy", cut.Find("fluent-dialog").TextContent);
        Assert.True(Input(cut).Instance.Autofocus);
        Assert.Equal(["Zapisz", "Anuluj"], cut.FindAll("fluent-dialog form fluent-button").Select(b => b.TextContent.Trim()));
        await FillAndSubmitAsync(cut, "  Development ");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Equal(["Development"], Names(cut)));
        Assert.Equal("Aktywny", State(cut, "Development"));
        WaitForMessage(cut, "success", "Dodano Typ pracy Development.");
    }

    [Fact]
    public async Task Add_DuplicateName_ErrorInWindow_WindowStaysOpenWithValue()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "development");

        cut.WaitForAssertion(() => Assert.Equal("Typ pracy o nazwie development już istnieje.",
            cut.Find("fluent-dialog .work-type-dialog-error").TextContent.Trim()), TimeSpan.FromSeconds(5));
        Assert.Equal(WorkTypesPage.DuplicateMessage("development"), cut.Find(".work-type-dialog-error").TextContent.Trim());
        Assert.Equal("alert", cut.Find(".work-type-dialog-error").GetAttribute("role"));
        Assert.Empty(cut.FindAll(".eod-messages fluent-message-bar"));
        Assert.Single(cut.FindAll("fluent-dialog"));
        Assert.Equal("development", Input(cut).Instance.Value);
        Assert.Equal(["Development"], Names(cut));
    }

    [Fact]
    public async Task Add_DuplicateThenEmptyName_InvalidSubmit_ClearsDuplicateError()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "development");
        cut.WaitForElement("fluent-dialog .work-type-dialog-error", TimeSpan.FromSeconds(5));
        await FillAndSubmitAsync(cut, "");

        cut.WaitForAssertion(() => Assert.Contains("Podaj nazwę Typu pracy.", cut.Markup));
        Assert.Empty(cut.FindAll(".work-type-dialog-error"));
        Assert.Single(cut.FindAll("fluent-dialog"));
    }

    [Theory]
    [InlineData("", "Podaj nazwę Typu pracy.")]
    [InlineData("   ", "Podaj nazwę Typu pracy.")]
    [InlineData(null, "Nazwa może mieć najwyżej 200 znaków.")]
    public async Task Add_Invalid_ShowsPolishMessageAtField_WindowStaysOpen_SavesNothing(string? name, string message)
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, name ?? new string('n', 201));

        // The browser's own Required check must not block submit before the validator.
        Assert.True(cut.Find("form.work-type-form").HasAttribute("novalidate"));
        cut.WaitForAssertion(() => Assert.Contains(message, cut.Markup));
        Assert.Single(cut.FindAll("fluent-dialog"));
        Assert.Empty(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Cancel_ClosesWindowWithoutSaving()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await cut.InvokeAsync(() => Input(cut).Instance.ValueChanged.InvokeAsync("Development"));
        await cut.Find(".work-type-cancel").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")), TimeSpan.FromSeconds(5));
        Assert.Empty(await Store.ListAsync(Ct));
        Assert.Empty(cut.FindAll("fluent-message-bar"));
    }

    [Fact]
    public async Task Escape_ClosesWindowWithoutSaving()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();

        var action = OpenEdit(cut, "Development");
        await cut.InvokeAsync(() => Input(cut).Instance.ValueChanged.InvokeAsync("Zmieniony"));
        var dialogId = cut.FindComponent<WorkTypeDialog>().Instance.Dialog.Id;
        // Escape closes the native dialog; the web component reports it as "beforetoggle" from open to closed.
        await cut.Find("fluent-dialog").TriggerEventAsync("ondialogbeforetoggle",
            new DialogToggleEventArgs { Id = dialogId, Type = "beforetoggle", OldState = "open", NewState = "closed" });

        await action.WaitAsync(TimeSpan.FromSeconds(5), Ct);
        Assert.Equal("Development", (await Store.ListAsync(Ct)).Single().Name);
        Assert.Empty(cut.FindAll("fluent-message-bar"));
    }

    [Fact]
    public async Task Edit_WindowWithValue_SavesNewName_ShowsSuccess()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();

        _ = OpenEdit(cut, "Development");
        Assert.Contains("Edytuj Typ pracy Development", cut.Find("fluent-dialog").TextContent);
        Assert.Equal("Development", Input(cut).Instance.Value);
        await FillAndSubmitAsync(cut, "Programowanie");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Equal(["Programowanie"], Names(cut)));
        WaitForMessage(cut, "success", "Zapisano Typ pracy Programowanie.");
    }

    [Fact]
    public async Task Edit_Title_EncodesName()
    {
        await SeedAsync(("Spotkanie <b>x</b>", true));
        var cut = RenderScreen();

        _ = OpenEdit(cut, "Spotkanie <b>x</b>");

        var dialog = cut.Find("fluent-dialog");
        Assert.Contains("Edytuj Typ pracy Spotkanie <b>x</b>", dialog.TextContent);
        Assert.Empty(dialog.QuerySelectorAll("b"));
    }

    [Fact]
    public async Task Edit_ToExistingName_ErrorInWindow_WindowStaysOpen()
    {
        await SeedAsync(("Development", true), ("Spotkanie", true));
        var cut = RenderScreen();

        _ = OpenEdit(cut, "Spotkanie");
        await FillAndSubmitAsync(cut, "DEVELOPMENT");

        cut.WaitForAssertion(() => Assert.Equal("Typ pracy o nazwie DEVELOPMENT już istnieje.",
            cut.Find("fluent-dialog .work-type-dialog-error").TextContent.Trim()), TimeSpan.FromSeconds(5));
        Assert.Single(cut.FindAll("fluent-dialog"));
        Assert.Equal("DEVELOPMENT", Input(cut).Instance.Value);
        Assert.Equal(["Development", "Spotkanie"], Names(cut));
    }

    [Fact]
    public async Task Edit_WorkTypeDeletedInMeantime_ClosesWindow_Warning_ListReloaded()
    {
        var ids = await SeedAsync(("Development", true));
        var cut = RenderScreen();

        _ = OpenEdit(cut, "Development");
        await DeleteDirectlyAsync(ids[0]);
        await FillAndSubmitAsync(cut, "Programowanie");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")), TimeSpan.FromSeconds(5));
        WaitForMessage(cut, "warning", WorkTypesPage.NotFoundMessage);
        cut.WaitForAssertion(() => Assert.Empty(Names(cut)));
    }

    [Fact]
    public async Task Deactivate_ThenRestore_SavedAtOnce_StateChanges_ShowsSuccess()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();

        await Action(cut, "Development", "Dezaktywuj").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Equal("Nieaktywny", State(cut, "Development")));
        Assert.False((await Store.ListAsync(Ct)).Single().IsActive);
        WaitForMessage(cut, "success", "Dezaktywowano Typ pracy Development.");
        Assert.Equal(["Edytuj", "Przywróć", "Usuń"], ActionLabels(Row(cut, "Development")));

        await Action(cut, "Development", "Przywróć").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Equal("Aktywny", State(cut, "Development")));
        Assert.True((await Store.ListAsync(Ct)).Single().IsActive);
        WaitForMessage(cut, "success", "Przywrócono Typ pracy Development.");
    }

    [Fact]
    public async Task Delete_WithoutEntries_Confirmed_RemovesRow()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();

        Action(cut, "Development", "Usuń").Click();
        cut.WaitForElement(".eod-confirm-primary").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Development", Names(cut)));
        Assert.Empty(await Store.ListAsync(Ct));
        WaitForMessage(cut, "success", "Usunięto Typ pracy Development.");
    }

    [Fact]
    public async Task Delete_Window_EncodesValues_EnterDoesNotConfirm()
    {
        JSInterop.Setup<bool>("Microsoft.FluentUI.Blazor.Components.Dialog.ShouldHandleShortcut", _ => true).SetResult(true);
        await SeedAsync(("Spotkanie <b>x</b>", true));
        var cut = RenderScreen();

        Action(cut, "Spotkanie <b>x</b>", "Usuń").Click();
        cut.WaitForElement(".eod-confirm-secondary");

        var dialog = cut.Find("fluent-dialog");
        Assert.Contains("Usunąć Typ pracy Spotkanie <b>x</b>?", dialog.TextContent);
        Assert.Empty(dialog.QuerySelectorAll(".message b"));
        Assert.True(cut.Find(".eod-confirm-secondary").HasAttribute("autofocus"));
        await dialog.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.NotEmpty(cut.FindAll("fluent-dialog"));
        Assert.Single(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Delete_Cancelled_DoesNothing()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();

        Action(cut, "Development", "Usuń").Click();
        cut.WaitForElement(".eod-confirm-secondary").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")));
        Assert.Single(await Store.ListAsync(Ct));
        Assert.Equal(["Development"], Names(cut));
    }

    [Theory]
    [InlineData(1, "Typ pracy Development występuje w 1 Wpisie. Zamiast go usuwać, dezaktywuj go.")]
    [InlineData(3, "Typ pracy Development występuje w 3 Wpisach. Zamiast go usuwać, dezaktywuj go.")]
    public async Task Delete_WithEntries_ShowsCountAndSuggestsDeactivation_KeepsRow(int entryCount, string message)
    {
        var ids = await SeedAsync(("Development", true));
        await AddEntriesAsync(ids[0], entryCount);
        var cut = RenderScreen();

        Action(cut, "Development", "Usuń").Click();
        cut.WaitForElement(".eod-confirm-primary").Click();

        WaitForMessage(cut, "warning", message);
        Assert.Equal(["Development"], Names(cut));
        Assert.Single(await Store.ListAsync(Ct));
    }

    [Theory]
    [InlineData(1, "Wpisie")]
    [InlineData(2, "Wpisach")]
    [InlineData(5, "Wpisach")]
    [InlineData(21, "Wpisach")]
    public void InUseMessage_UsesWpisieOnlyForOne(int count, string word) =>
        Assert.Equal($"Typ pracy ABC występuje w {count} {word}. Zamiast go usuwać, dezaktywuj go.", WorkTypesPage.InUseMessage("ABC", count));

    [Fact]
    public void ToRow_HasIdOutsideColumns_AndColumnKeys()
    {
        var row = WorkTypesPage.ToRow(new WorkTypeRow(7, "Development", true));

        Assert.Equal(new Dictionary<string, object?> { ["Id"] = 7, ["Name"] = "Development", ["State"] = "Aktywny" }, row);
        Assert.DoesNotContain(WorkTypesPage.Report.Columns, c => c.Key == "Id");
        Assert.All(WorkTypesPage.Report.Columns, c => Assert.Equal(ColumnType.List, c.Type));
        Assert.Equal("typy-pracy", WorkTypesPage.Report.Key);
    }

    /// <summary>The screen in MainLayout, as the router renders it: "?" of the top bar, message area, dialogs.</summary>
    private IRenderedComponent<MainLayout> RenderScreen()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, b =>
        {
            b.OpenComponent<WorkTypesPage>(0);
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

    private static async Task FillAndSubmitAsync(IRenderedComponent<MainLayout> cut, string name)
    {
        await cut.InvokeAsync(() => Input(cut).Instance.ValueChanged.InvokeAsync(name));
        await cut.Find("form.work-type-form").SubmitAsync();
    }

    private static IRenderedComponent<FluentTextInput> Input(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponents<FluentTextInput>().Single(i => i.Instance.Class == "work-type-name");

    private static List<string> Headers(IRenderedComponent<MainLayout> cut) =>
        cut.FindAll("thead th[col-index] [col-sort-button]").Select(e => e.TextContent.Trim()).ToList();

    private static List<string> Column(IRenderedComponent<MainLayout> cut, int index) =>
        cut.FindAll("tbody tr[hover]").Select(tr => tr.QuerySelectorAll("td")[index].TextContent.Trim()).ToList();

    private static List<string> Names(IRenderedComponent<MainLayout> cut) => Column(cut, 0);

    private static string Footer(IRenderedComponent<MainLayout> cut) => cut.Find(".eod-card-foot .eod-num").TextContent.Trim();

    private static IElement Row(IRenderedComponent<MainLayout> cut, string name) =>
        cut.FindAll("tbody tr[hover]").Single(tr => tr.QuerySelector("td")?.TextContent.Trim() == name);

    private static string State(IRenderedComponent<MainLayout> cut, string name) =>
        Row(cut, name).QuerySelectorAll("td")[1].TextContent.Trim();

    private static IElement Action(IRenderedComponent<MainLayout> cut, string name, string label) =>
        Row(cut, name).QuerySelector($"fluent-button[aria-label='{label}']")!;

    private static List<string?> ActionLabels(IElement row) =>
        row.QuerySelectorAll("fluent-button[aria-label]").Select(b => b.GetAttribute("aria-label")).ToList();

    private static async Task OpenColumnFilterAsync(IRenderedComponent<MainLayout> cut, string label)
    {
        var button = cut.FindAll("th fluent-button").Single(b => b.GetAttribute("title")?.EndsWith($": {label}", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => button.Click());
    }

    /// <summary>Waits for a message bar of the intent with the text; on failure lists the bars and logged errors.</summary>
    private void WaitForMessage(IRenderedComponent<MainLayout> cut, string intent, string text) =>
        cut.WaitForAssertion(() => Assert.Contains($"{intent}: {text}", Messages(cut)), TimeSpan.FromSeconds(5));

    private List<string> Messages(IRenderedComponent<MainLayout> cut) =>
    [
        .. cut.FindAll("fluent-message-bar").Select(b => $"{b.GetAttribute("intent")}: {b.TextContent.Trim()}"),
        .. _errors.Entries.Select(e => $"log: {e.Exception}"),
    ];

    private static void OpenAdd(IRenderedComponent<MainLayout> cut)
    {
        _ = cut.Find(".work-types-add").ClickAsync(new MouseEventArgs());
        cut.WaitForElement("fluent-dialog form.work-type-form", TimeSpan.FromSeconds(5));
    }

    /// <summary>Clicks Edytuj and waits for the window (the row action stays pending until the window closes).</summary>
    private static Task OpenEdit(IRenderedComponent<MainLayout> cut, string name)
    {
        var action = Action(cut, name, "Edytuj").ClickAsync(new MouseEventArgs());
        cut.WaitForElement("fluent-dialog form.work-type-form", TimeSpan.FromSeconds(5));
        return action;
    }

    private async Task<List<int>> SeedAsync(params (string Name, bool IsActive)[] workTypes)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var entities = workTypes.Select(w => new WorkType { Name = w.Name, IsActive = w.IsActive }).ToList();
        db.WorkTypes.AddRange(entities);
        await db.SaveChangesAsync(Ct);
        return entities.Select(e => e.Id).ToList();
    }

    private async Task DeleteDirectlyAsync(int id)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        db.WorkTypes.Remove(db.WorkTypes.Single(w => w.Id == id));
        await db.SaveChangesAsync(Ct);
    }

    private async Task AddEntriesAsync(int workTypeId, int count)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var project = new Project { Code = "RDZ", Name = "Rozwój" };
        db.Projects.Add(project);
        await db.SaveChangesAsync(Ct);
        for (var i = 0; i < count; i++)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                Date = new DateOnly(2026, 9, 1 + i), ProjectId = project.Id, WorkTypeId = workTypeId, Description = $"Wpis {i}", Minutes = 60,
            });
        }
        await db.SaveChangesAsync(Ct);
    }
}
