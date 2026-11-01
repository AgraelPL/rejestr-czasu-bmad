using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Features.TypyPracy;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Typy pracy screen (R1–R3, R17, AD-8, AD-11) in bUnit with the real <see cref="WorkTypeStore"/> on the test
/// database, so duplicates and Entry counts come from PostgreSQL as in the application.
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
    public async Task Screen_HasHeaderHelpTopicAndGridWithStateAndCounter()
    {
        await SeedAsync(("Spotkanie", true), ("Analiza", false));

        var cut = RenderScreen();

        Assert.Equal("Typy pracy", cut.Find("h1").TextContent);
        Assert.Equal("/pomoc/typy-pracy", cut.Find(".app-header .app-help").GetAttribute("href"));
        Assert.Empty(cut.FindAll(".page-breadcrumbs"));
        cut.WaitForAssertion(() => Assert.Equal(["Analiza", "Spotkanie"], Names(cut)));
        Assert.Equal(["Nieaktywny", "Aktywny"], cut.FindAll("tbody tr td:nth-child(2)").Select(td => td.TextContent));
        Assert.Equal("Pozycje: 2", cut.Find(".report-grid-counter").TextContent);
    }

    [Fact]
    public async Task Actions_DeactivateOnlyForActive_RestoreOnlyForInactive()
    {
        await SeedAsync(("Aktywny", true), ("Nieaktywny", false));

        var cut = RenderScreen();

        cut.WaitForAssertion(() => Assert.Equal(2, Names(cut).Count));
        Assert.Equal(["Edytuj", "Dezaktywuj", "Usuń"], ActionLabels(Row(cut, "Aktywny")));
        Assert.Equal(["Edytuj", "Przywróć", "Usuń"], ActionLabels(Row(cut, "Nieaktywny")));
    }

    [Fact]
    public async Task Add_SavesTrimmedNameAsTyped_ClosesForm_ShowsSuccess()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "  Development ");

        cut.WaitForAssertion(() => Assert.Equal(["Development"], Names(cut)));
        Assert.Equal("Aktywny", State(cut, "Development"));
        Assert.Empty(cut.FindAll("form.work-type-form"));
        WaitForMessage(cut, "success", "Dodano Typ pracy Development.");
    }

    [Fact]
    public async Task Add_DuplicateName_ShowsError_FormStaysOpenWithValue()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Names(cut)));

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "development");

        WaitForMessage(cut, "error", WorkTypesPage.DuplicateMessage("development"));
        Assert.Equal("Typ pracy o nazwie development już istnieje.", WorkTypesPage.DuplicateMessage("development"));
        Assert.Single(cut.FindAll("form.work-type-form"));
        Assert.Equal("development", Input(cut).Instance.Value);
        Assert.Equal(["Development"], Names(cut));
    }

    [Theory]
    [InlineData("", "Podaj nazwę Typu pracy.")]
    [InlineData("   ", "Podaj nazwę Typu pracy.")]
    [InlineData(null, "Nazwa może mieć najwyżej 200 znaków.")]
    public async Task Add_Invalid_ShowsPolishMessageAtField_SavesNothing(string? name, string message)
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, name ?? new string('n', 201));

        // The browser's own Required check must not block submit before the validator.
        Assert.True(cut.Find("form.work-type-form").HasAttribute("novalidate"));
        cut.WaitForAssertion(() => Assert.Contains(message, cut.Markup));
        Assert.Single(cut.FindAll("form.work-type-form"));
        Assert.Empty(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Cancel_ClosesFormWithoutSaving()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await cut.InvokeAsync(() => Input(cut).Instance.ValueChanged.InvokeAsync("Development"));
        cut.Find(".work-type-cancel").Click();

        Assert.Empty(cut.FindAll("form.work-type-form"));
        Assert.Empty(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Edit_OpensFormWithValue_SavesNewName()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Names(cut)));

        // The icon's handler belongs to ReportGrid: the screen must render the form itself.
        Action(cut, "Development", "Edytuj").Click();
        cut.WaitForElement("form.work-type-form");
        Assert.Equal("Edycja Typu pracy Development", cut.Find(".work-type-form-title").TextContent);
        Assert.Equal("Development", Input(cut).Instance.Value);
        await FillAndSubmitAsync(cut, "Programowanie");

        cut.WaitForAssertion(() => Assert.Equal(["Programowanie"], Names(cut)));
        WaitForMessage(cut, "success", "Zapisano Typ pracy Programowanie.");
    }

    [Fact]
    public async Task Edit_ToExistingName_ShowsError_FormStaysOpen()
    {
        await SeedAsync(("Development", true), ("Spotkanie", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Equal(2, Names(cut).Count));

        Action(cut, "Spotkanie", "Edytuj").Click();
        cut.WaitForElement("form.work-type-form");
        await FillAndSubmitAsync(cut, "DEVELOPMENT");

        WaitForMessage(cut, "error", "Typ pracy o nazwie DEVELOPMENT już istnieje.");
        Assert.Single(cut.FindAll("form.work-type-form"));
        Assert.Equal("DEVELOPMENT", Input(cut).Instance.Value);
        Assert.Equal(["Development", "Spotkanie"], Names(cut));
    }

    [Fact]
    public async Task Deactivate_ThenRestore_ChangesStateInGrid()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Names(cut)));

        Action(cut, "Development", "Dezaktywuj").Click();
        cut.WaitForAssertion(() => Assert.Equal("Nieaktywny", State(cut, "Development")));
        Assert.Equal(["Edytuj", "Przywróć", "Usuń"], ActionLabels(Row(cut, "Development")));

        Action(cut, "Development", "Przywróć").Click();
        cut.WaitForAssertion(() => Assert.Equal("Aktywny", State(cut, "Development")));
    }

    [Fact]
    public async Task Delete_WithoutEntries_Confirmed_RemovesRow()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Names(cut)));

        Action(cut, "Development", "Usuń").Click();
        cut.WaitForElement(".confirm-primary").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("Development", Names(cut)));
        Assert.Empty(await Store.ListAsync(Ct));
        WaitForMessage(cut, "success", "Usunięto Typ pracy Development.");
    }

    [Fact]
    public async Task Delete_Cancelled_DoesNothing()
    {
        await SeedAsync(("Development", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Names(cut)));

        Action(cut, "Development", "Usuń").Click();
        cut.WaitForElement(".confirm-cancel").Click();

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
        cut.WaitForAssertion(() => Assert.Single(Names(cut)));

        Action(cut, "Development", "Usuń").Click();
        cut.WaitForElement(".confirm-primary").Click();

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
        Assert.Equal($"Typ pracy Spotkanie występuje w {count} {word}. Zamiast go usuwać, dezaktywuj go.",
            WorkTypesPage.InUseMessage("Spotkanie", count));

    /// <summary>The screen in MainLayout, as the router renders it: "?" of the top bar, message area, dialogs.</summary>
    private IRenderedComponent<MainLayout> RenderScreen()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, b =>
        {
            b.OpenComponent<WorkTypesPage>(0);
            b.CloseComponent();
        }));
        // The list is loaded from the database; the grid appears after it.
        cut.WaitForElement(".report-grid", TimeSpan.FromSeconds(5));
        return cut;
    }

    private static async Task FillAndSubmitAsync(IRenderedComponent<MainLayout> cut, string name)
    {
        var input = Input(cut);
        await cut.InvokeAsync(() => input.Instance.ValueChanged.InvokeAsync(name));
        await cut.Find("form.work-type-form").SubmitAsync();
    }

    private static IRenderedComponent<FluentTextInput> Input(IRenderedComponent<MainLayout> cut) =>
        Assert.Single(cut.FindComponents<FluentTextInput>());

    private static List<string> Names(IRenderedComponent<MainLayout> cut) =>
        cut.FindAll("tbody tr td:first-child").Select(td => td.TextContent).ToList();

    private static IElement Row(IRenderedComponent<MainLayout> cut, string name) =>
        cut.FindAll("tbody tr").Single(tr => tr.QuerySelector("td")?.TextContent == name);

    private static string State(IRenderedComponent<MainLayout> cut, string name) =>
        Row(cut, name).QuerySelectorAll("td")[1].TextContent;

    private static IElement Action(IRenderedComponent<MainLayout> cut, string name, string label) =>
        Row(cut, name).QuerySelector($"fluent-button[aria-label='{label}']")!;

    private static List<string?> ActionLabels(IElement row) =>
        row.QuerySelectorAll("fluent-button[aria-label]").Select(b => b.GetAttribute("aria-label")).ToList();

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
        cut.Find(".work-types-add").Click();
        cut.WaitForElement("form.work-type-form");
    }

    private async Task<List<int>> SeedAsync(params (string Name, bool IsActive)[] workTypes)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var entities = workTypes.Select(w => new WorkType { Name = w.Name, IsActive = w.IsActive }).ToList();
        db.WorkTypes.AddRange(entities);
        await db.SaveChangesAsync(Ct);
        return entities.Select(e => e.Id).ToList();
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
