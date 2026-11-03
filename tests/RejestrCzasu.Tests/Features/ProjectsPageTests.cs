using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Projekty;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Projekty screen (R1–R3, R17, AD-8, AD-9, AD-11) on EodPage with EodReport and the Project window, in bUnit with
/// the real <see cref="ProjectStore"/> on the test database, so duplicates and Entry counts come from PostgreSQL as in
/// the application.
/// </summary>
public sealed class ProjectsPageTests : BunitContext, IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private readonly CapturingLogger<AppErrorBoundary> _errors = new();
    private int _user;

    public ProjectsPageTests(PostgresFixture postgres)
    {
        _postgres = postgres;
        this.AddAppUi();
        // Created on first use, after InitializeAsync has created the User.
        Services.AddSingleton<Microsoft.Extensions.Logging.ILogger<AppErrorBoundary>>(_errors);
        Services.AddScoped(_ => new ProjectStore(_postgres.CreateLoggedInUserDbContextFactory(_user)));
        AddAuthorization().SetAuthorized("ekran-projekty");
        // The application renders its screens in an Interactive Server circuit.
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private static CancellationToken Ct => Xunit.TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        await _postgres.ResetAsync();
        _user = await _postgres.CreateUserAsync("ekran-projekty");
    }

    private ProjectStore Store => new(_postgres.CreateLoggedInUserDbContextFactory(_user));

    [Fact]
    public async Task List_EodPageWithReport_StoreOrder_Counter_NoExport()
    {
        await SeedAsync(("RDZ", "Rozwój", true), ("NIE", "Nieaktywny projekt", false));

        var cut = RenderScreen();

        Assert.Equal("Projekty", cut.Find(".eod-page h1").TextContent);
        Assert.Equal("Dodaj Projekt", cut.Find(".eod-page-head .projects-add").TextContent.Trim());
        Assert.Equal("/pomoc/projekty", cut.Find(".app-help").GetAttribute("href"));
        Assert.Equal(["Kod", "Nazwa", "Stan"], Headers(cut));
        Assert.Equal(["NIE", "RDZ"], Codes(cut));
        Assert.Equal(["Nieaktywny", "Aktywny"], Column(cut, 2));
        Assert.Equal("2 pozycje", Footer(cut));
        Assert.DoesNotContain("Eksport CSV", cut.Markup);
    }

    [Fact]
    public async Task Filter_StateNieaktywny_ShowsOnlyInactive_CounterAfterFilters()
    {
        await SeedAsync(("AKT", "Aktywny", true), ("NIE", "Nieaktywny", false));
        var cut = RenderScreen();

        await OpenColumnFilterAsync(cut, "Stan");
        Assert.Equal(["Aktywny (1)", "Nieaktywny (1)"], cut.FindComponents<FluentCheckbox>().Select(c => c.Instance.Label));
        var active = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "Aktywny (1)");
        await cut.InvokeAsync(() => active.Instance.ValueChanged.InvokeAsync(false));

        Assert.Equal(["NIE"], Codes(cut));
        Assert.Equal("1 pozycja · po filtrach, z 2", Footer(cut));
    }

    [Fact]
    public async Task Actions_SubtleIcons_DeactivateOnlyForActive_RestoreOnlyForInactive()
    {
        await SeedAsync(("AKT", "Aktywny", true), ("NIE", "Nieaktywny", false));

        var cut = RenderScreen();

        Assert.Equal(["Edytuj", "Dezaktywuj", "Usuń"], ActionLabels(Row(cut, "AKT")));
        Assert.Equal(["Edytuj", "Przywróć", "Usuń"], ActionLabels(Row(cut, "NIE")));
        Assert.All(Row(cut, "AKT").QuerySelectorAll("fluent-button[aria-label]"),
            b => Assert.Equal("subtle", b.GetAttribute("appearance")));
    }

    [Fact]
    public async Task Add_WindowWithFocusOnCode_SavesUpperCaseCode_ClosesWindow_ShowsSuccess()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        Assert.Contains("Dodaj Projekt", cut.Find("fluent-dialog").TextContent);
        Assert.True(Input(cut, "project-code").Instance.Autofocus);
        Assert.Equal(["Zapisz", "Anuluj"], cut.FindAll("fluent-dialog form fluent-button").Select(b => b.TextContent.Trim()));
        await FillAndSubmitAsync(cut, "rdz", "Rozwój");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Equal(["RDZ"], Codes(cut)));
        WaitForMessage(cut, "success", "Dodano Projekt RDZ.");
    }

    [Fact]
    public async Task Add_DuplicateCode_ErrorInWindow_WindowStaysOpenWithValues()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "Rdz", "Inny");

        cut.WaitForAssertion(() => Assert.Equal("Projekt o kodzie RDZ już istnieje.",
            cut.Find("fluent-dialog .project-dialog-error").TextContent.Trim()), TimeSpan.FromSeconds(5));
        Assert.Equal(ProjectsPage.DuplicateMessage("RDZ"), cut.Find(".project-dialog-error").TextContent.Trim());
        Assert.Equal("alert", cut.Find(".project-dialog-error").GetAttribute("role"));
        Assert.Empty(cut.FindAll(".eod-messages fluent-message-bar"));
        Assert.Single(cut.FindAll("fluent-dialog"));
        Assert.Equal(["Rdz", "Inny"], Inputs(cut).Select(i => i.Instance.Value));
        Assert.Equal(["RDZ"], Codes(cut));
    }

    [Fact]
    public async Task Add_DuplicateThenEmptyCode_InvalidSubmit_ClearsDuplicateError()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "RDZ", "Inny");
        cut.WaitForElement("fluent-dialog .project-dialog-error", TimeSpan.FromSeconds(5));
        await FillAndSubmitAsync(cut, "", "Inny");

        cut.WaitForAssertion(() => Assert.Contains("Podaj kod Projektu.", cut.Markup));
        Assert.Empty(cut.FindAll(".project-dialog-error"));
        Assert.Single(cut.FindAll("fluent-dialog"));
    }

    [Fact]
    public async Task Add_Empty_ShowsPolishMessagesAtFields_WindowStaysOpen_SavesNothing()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "  ", "");

        // The browser's own Required check must not block submit before the validator.
        Assert.True(cut.Find("form.project-form").HasAttribute("novalidate"));
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Podaj kod Projektu.", cut.Markup);
            Assert.Contains("Podaj nazwę Projektu.", cut.Markup);
        });
        Assert.Single(cut.FindAll("fluent-dialog"));
        Assert.Empty(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Add_CodeTooLong_ShowsMessage_SavesNothing()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, new string('A', 21), "Rozwój");

        cut.WaitForAssertion(() => Assert.Contains("Kod może mieć najwyżej 20 znaków.", cut.Markup));
        Assert.Single(cut.FindAll("fluent-dialog"));
        Assert.Empty(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Cancel_ClosesWindowWithoutSaving()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await cut.InvokeAsync(() => Input(cut, "project-code").Instance.ValueChanged.InvokeAsync("RDZ"));
        await cut.Find(".project-cancel").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")), TimeSpan.FromSeconds(5));
        Assert.Empty(await Store.ListAsync(Ct));
        Assert.Empty(cut.FindAll("fluent-message-bar"));
    }

    [Fact]
    public async Task Escape_ClosesWindowWithoutSaving()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();

        var action = OpenEdit(cut, "RDZ");
        await cut.InvokeAsync(() => Input(cut, "project-name").Instance.ValueChanged.InvokeAsync("Zmieniona"));
        var dialogId = cut.FindComponent<ProjectDialog>().Instance.Dialog.Id;
        // Escape closes the native dialog; the web component reports it as "beforetoggle" from open to closed.
        await cut.Find("fluent-dialog").TriggerEventAsync("ondialogbeforetoggle",
            new DialogToggleEventArgs { Id = dialogId, Type = "beforetoggle", OldState = "open", NewState = "closed" });

        await action.WaitAsync(TimeSpan.FromSeconds(5), Ct);
        Assert.Equal("Rozwój", (await Store.ListAsync(Ct)).Single().Name);
        Assert.Empty(cut.FindAll("fluent-message-bar"));
    }

    [Fact]
    public async Task Edit_WindowWithValues_SavesNewCodeAndName_ShowsSuccess()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();

        _ = OpenEdit(cut, "RDZ");
        Assert.Contains("Edytuj Projekt RDZ", cut.Find("fluent-dialog").TextContent);
        Assert.Equal(["RDZ", "Rozwój"], Inputs(cut).Select(i => i.Instance.Value));
        Assert.True(Input(cut, "project-code").Instance.Autofocus);
        await FillAndSubmitAsync(cut, "abc", "Nowa nazwa");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")), TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Equal(["ABC"], Codes(cut)));
        Assert.Equal("Nowa nazwa", Row(cut, "ABC").QuerySelectorAll("td")[1].TextContent.Trim());
        WaitForMessage(cut, "success", "Zapisano Projekt ABC.");
    }

    [Fact]
    public async Task Edit_Title_EncodesCode()
    {
        await SeedAsync(("<B>X</B>", "Rozwój", true));
        var cut = RenderScreen();

        _ = OpenEdit(cut, "<B>X</B>");

        var dialog = cut.Find("fluent-dialog");
        Assert.Contains("Edytuj Projekt <B>X</B>", dialog.TextContent);
        Assert.Empty(dialog.QuerySelectorAll("b"));
    }

    [Fact]
    public async Task Edit_ProjectDeletedInMeantime_ClosesWindow_Warning_ListReloaded()
    {
        var ids = await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();

        _ = OpenEdit(cut, "RDZ");
        await DeleteDirectlyAsync(ids[0]);
        await FillAndSubmitAsync(cut, "RDZ", "Nowa nazwa");

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")), TimeSpan.FromSeconds(5));
        WaitForMessage(cut, "warning", ProjectsPage.NotFoundMessage);
        cut.WaitForAssertion(() => Assert.Empty(Codes(cut)));
    }

    [Fact]
    public async Task Deactivate_ThenRestore_SavedAtOnce_StateChanges_ShowsSuccess()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();

        await Action(cut, "RDZ", "Dezaktywuj").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Equal("Nieaktywny", State(cut, "RDZ")));
        Assert.False((await Store.ListAsync(Ct)).Single().IsActive);
        WaitForMessage(cut, "success", "Dezaktywowano Projekt RDZ.");
        Assert.Equal(["Edytuj", "Przywróć", "Usuń"], ActionLabels(Row(cut, "RDZ")));

        await Action(cut, "RDZ", "Przywróć").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Equal("Aktywny", State(cut, "RDZ")));
        Assert.True((await Store.ListAsync(Ct)).Single().IsActive);
        WaitForMessage(cut, "success", "Przywrócono Projekt RDZ.");
    }

    [Fact]
    public async Task Delete_WithoutEntries_Confirmed_RemovesRow()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();

        Action(cut, "RDZ", "Usuń").Click();
        cut.WaitForElement(".eod-confirm-primary").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("RDZ", Codes(cut)));
        Assert.Empty(await Store.ListAsync(Ct));
        WaitForMessage(cut, "success", "Usunięto Projekt RDZ.");
    }

    [Fact]
    public async Task Delete_Window_EncodesValues_EnterDoesNotConfirm()
    {
        JSInterop.Setup<bool>("Microsoft.FluentUI.Blazor.Components.Dialog.ShouldHandleShortcut", _ => true).SetResult(true);
        await SeedAsync(("RDZ", "Rozwój <b>x</b>", true));
        var cut = RenderScreen();

        Action(cut, "RDZ", "Usuń").Click();
        cut.WaitForElement(".eod-confirm-secondary");

        var dialog = cut.Find("fluent-dialog");
        Assert.Contains("Usunąć Projekt RDZ (Rozwój <b>x</b>)?", dialog.TextContent);
        Assert.Empty(dialog.QuerySelectorAll(".message b"));
        Assert.True(cut.Find(".eod-confirm-secondary").HasAttribute("autofocus"));
        await dialog.KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        Assert.NotEmpty(cut.FindAll("fluent-dialog"));
        Assert.Single(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Delete_Cancelled_DoesNothing()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();

        Action(cut, "RDZ", "Usuń").Click();
        cut.WaitForElement(".eod-confirm-secondary").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("fluent-dialog")));
        Assert.Single(await Store.ListAsync(Ct));
        Assert.Equal(["RDZ"], Codes(cut));
    }

    [Theory]
    [InlineData(1, "Projekt RDZ występuje w 1 Wpisie. Zamiast go usuwać, dezaktywuj go.")]
    [InlineData(3, "Projekt RDZ występuje w 3 Wpisach. Zamiast go usuwać, dezaktywuj go.")]
    public async Task Delete_WithEntries_ShowsCountAndSuggestsDeactivation_KeepsRow(int entryCount, string message)
    {
        var ids = await SeedAsync(("RDZ", "Rozwój", true));
        await AddEntriesAsync(ids[0], entryCount);
        var cut = RenderScreen();

        Action(cut, "RDZ", "Usuń").Click();
        cut.WaitForElement(".eod-confirm-primary").Click();

        WaitForMessage(cut, "warning", message);
        Assert.Equal(["RDZ"], Codes(cut));
        Assert.Single(await Store.ListAsync(Ct));
    }

    [Theory]
    [InlineData(1, "Wpisie")]
    [InlineData(2, "Wpisach")]
    [InlineData(5, "Wpisach")]
    [InlineData(21, "Wpisach")]
    public void InUseMessage_UsesWpisieOnlyForOne(int count, string word) =>
        Assert.Equal($"Projekt ABC występuje w {count} {word}. Zamiast go usuwać, dezaktywuj go.", ProjectsPage.InUseMessage("ABC", count));

    [Fact]
    public void ToRow_HasIdOutsideColumns_AndColumnKeys()
    {
        var row = ProjectsPage.ToRow(new ProjectRow(7, "RDZ", "Rozwój", false));

        Assert.Equal(new Dictionary<string, object?> { ["Id"] = 7, ["Code"] = "RDZ", ["Name"] = "Rozwój", ["State"] = "Nieaktywny" }, row);
        Assert.DoesNotContain(ProjectsPage.Report.Columns, c => c.Key == "Id");
        Assert.All(ProjectsPage.Report.Columns, c => Assert.Equal(ColumnType.List, c.Type));
        Assert.Equal("projekty", ProjectsPage.Report.Key);
    }

    /// <summary>The screen in MainLayout, as the router renders it: "?" of the top bar, message area, dialogs.</summary>
    private IRenderedComponent<MainLayout> RenderScreen()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, b =>
        {
            b.OpenComponent<ProjectsPage>(0);
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

    private static async Task FillAndSubmitAsync(IRenderedComponent<MainLayout> cut, string code, string name)
    {
        await cut.InvokeAsync(() => Input(cut, "project-code").Instance.ValueChanged.InvokeAsync(code));
        await cut.InvokeAsync(() => Input(cut, "project-name").Instance.ValueChanged.InvokeAsync(name));
        await cut.Find("form.project-form").SubmitAsync();
    }

    private static IReadOnlyList<IRenderedComponent<FluentTextInput>> Inputs(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponents<FluentTextInput>().Where(i => i.Instance.Class is "project-code" or "project-name").ToList();

    private static IRenderedComponent<FluentTextInput> Input(IRenderedComponent<MainLayout> cut, string cssClass) =>
        cut.FindComponents<FluentTextInput>().Single(i => i.Instance.Class == cssClass);

    private static List<string> Headers(IRenderedComponent<MainLayout> cut) =>
        cut.FindAll("thead th[col-index] [col-sort-button]").Select(e => e.TextContent.Trim()).ToList();

    private static List<string> Column(IRenderedComponent<MainLayout> cut, int index) =>
        cut.FindAll("tbody tr[hover]").Select(tr => tr.QuerySelectorAll("td")[index].TextContent.Trim()).ToList();

    private static List<string> Codes(IRenderedComponent<MainLayout> cut) => Column(cut, 0);

    private static string Footer(IRenderedComponent<MainLayout> cut) => cut.Find(".eod-card-foot .eod-num").TextContent.Trim();

    private static IElement Row(IRenderedComponent<MainLayout> cut, string code) =>
        cut.FindAll("tbody tr[hover]").Single(tr => tr.QuerySelector("td")?.TextContent.Trim() == code);

    private static string State(IRenderedComponent<MainLayout> cut, string code) =>
        Row(cut, code).QuerySelectorAll("td")[2].TextContent.Trim();

    private static IElement Action(IRenderedComponent<MainLayout> cut, string code, string label) =>
        Row(cut, code).QuerySelector($"fluent-button[aria-label='{label}']")!;

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
        _ = cut.Find(".projects-add").ClickAsync(new MouseEventArgs());
        cut.WaitForElement("fluent-dialog form.project-form", TimeSpan.FromSeconds(5));
    }

    /// <summary>Clicks Edytuj and waits for the window (the row action stays pending until the window closes).</summary>
    private static Task OpenEdit(IRenderedComponent<MainLayout> cut, string code)
    {
        var action = Action(cut, code, "Edytuj").ClickAsync(new MouseEventArgs());
        cut.WaitForElement("fluent-dialog form.project-form", TimeSpan.FromSeconds(5));
        return action;
    }

    private async Task<List<int>> SeedAsync(params (string Code, string Name, bool IsActive)[] projects)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var entities = projects.Select(p => new Project { Code = p.Code, Name = p.Name, IsActive = p.IsActive }).ToList();
        db.Projects.AddRange(entities);
        await db.SaveChangesAsync(Ct);
        return entities.Select(e => e.Id).ToList();
    }

    private async Task DeleteDirectlyAsync(int id)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        db.Projects.Remove(db.Projects.Single(p => p.Id == id));
        await db.SaveChangesAsync(Ct);
    }

    private async Task AddEntriesAsync(int projectId, int count)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var workType = new WorkType { Name = "Development" };
        db.WorkTypes.Add(workType);
        await db.SaveChangesAsync(Ct);
        for (var i = 0; i < count; i++)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                Date = new DateOnly(2026, 9, 1 + i), ProjectId = projectId, WorkTypeId = workType.Id, Description = $"Wpis {i}", Minutes = 60,
            });
        }
        await db.SaveChangesAsync(Ct);
    }
}
