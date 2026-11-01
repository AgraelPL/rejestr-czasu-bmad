using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Features.Projekty;
using RejestrCzasu.Shared;
using RejestrCzasu.Tests.Shared;

namespace RejestrCzasu.Tests.Features;

/// <summary>
/// The Projekty screen (R1–R3, R17, AD-8, AD-11) in bUnit with the real <see cref="ProjectStore"/> on the test database,
/// so duplicates and Entry counts come from PostgreSQL as in the application.
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
    public async Task Screen_HasHeaderHelpTopicAndGridWithStateAndCounter()
    {
        await SeedAsync(("RDZ", "Rozwój", true), ("NIE", "Nieaktywny projekt", false));

        var cut = RenderScreen();

        Assert.Equal("Projekty", cut.Find("h1").TextContent);
        Assert.Equal("/pomoc/projekty", cut.Find(".app-header .app-help").GetAttribute("href"));
        Assert.Empty(cut.FindAll(".page-breadcrumbs"));
        cut.WaitForAssertion(() => Assert.Equal(["NIE", "RDZ"], Codes(cut)));
        Assert.Equal(["Nieaktywny", "Aktywny"], cut.FindAll("tbody tr td:nth-child(3)").Select(td => td.TextContent));
        Assert.Equal("Pozycje: 2", cut.Find(".report-grid-counter").TextContent);
    }

    [Fact]
    public async Task Actions_DeactivateOnlyForActive_RestoreOnlyForInactive()
    {
        await SeedAsync(("AKT", "Aktywny", true), ("NIE", "Nieaktywny", false));

        var cut = RenderScreen();

        cut.WaitForAssertion(() => Assert.Equal(2, Codes(cut).Count));
        Assert.Equal(["Edytuj", "Dezaktywuj", "Usuń"], ActionLabels(Row(cut, "AKT")));
        Assert.Equal(["Edytuj", "Przywróć", "Usuń"], ActionLabels(Row(cut, "NIE")));
    }

    [Fact]
    public async Task Add_SavesUpperCaseCode_ClosesForm_ShowsSuccess()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "rdz", "Rozwój");

        cut.WaitForAssertion(() => Assert.Equal(["RDZ"], Codes(cut)));
        Assert.Empty(cut.FindAll("form.project-form"));
        WaitForMessage(cut, "success", "Dodano Projekt RDZ.");
    }

    [Fact]
    public async Task Add_DuplicateCode_ShowsError_FormStaysOpenWithValues()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Codes(cut)));

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, "Rdz", "Inny");

        WaitForMessage(cut, "error", ProjectsPage.DuplicateMessage("RDZ"));
        Assert.Equal("Projekt o kodzie RDZ już istnieje.", ProjectsPage.DuplicateMessage("RDZ"));
        Assert.Single(cut.FindAll("form.project-form"));
        Assert.Equal(["Rdz", "Inny"], Inputs(cut).Select(i => i.Instance.Value));
        Assert.Equal(["RDZ"], Codes(cut));
    }

    [Fact]
    public async Task Add_Empty_ShowsPolishMessagesAtFields_SavesNothing()
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
        Assert.Single(cut.FindAll("form.project-form"));
        Assert.Empty(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Add_CodeTooLong_ShowsMessage_SavesNothing()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await FillAndSubmitAsync(cut, new string('A', 21), "Rozwój");

        cut.WaitForAssertion(() => Assert.Contains("Kod może mieć najwyżej 20 znaków.", cut.Markup));
        Assert.Empty(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Cancel_ClosesFormWithoutSaving()
    {
        var cut = RenderScreen();

        OpenAdd(cut);
        await cut.InvokeAsync(() => Inputs(cut)[0].Instance.ValueChanged.InvokeAsync("RDZ"));
        cut.Find(".project-cancel").Click();

        Assert.Empty(cut.FindAll("form.project-form"));
        Assert.Empty(await Store.ListAsync(Ct));
    }

    [Fact]
    public async Task Edit_OpensFormWithValues_SavesNewCodeAndName()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Codes(cut)));

        // The icon's handler belongs to ReportGrid: the screen must render the form itself.
        Action(cut, "RDZ", "Edytuj").Click();
        cut.WaitForElement("form.project-form");
        Assert.Equal("Edycja Projektu RDZ", cut.Find(".project-form-title").TextContent);
        Assert.Equal(["RDZ", "Rozwój"], Inputs(cut).Select(i => i.Instance.Value));
        await FillAndSubmitAsync(cut, "abc", "Nowa nazwa");

        cut.WaitForAssertion(() => Assert.Equal(["ABC"], Codes(cut)));
        Assert.Equal("Nowa nazwa", Row(cut, "ABC").QuerySelectorAll("td")[1].TextContent);
        WaitForMessage(cut, "success", "Zapisano Projekt ABC.");
    }

    [Fact]
    public async Task Deactivate_ThenRestore_ChangesStateInGrid()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Codes(cut)));

        Action(cut, "RDZ", "Dezaktywuj").Click();
        cut.WaitForAssertion(() => Assert.Equal("Nieaktywny", State(cut, "RDZ")));
        Assert.Equal(["Edytuj", "Przywróć", "Usuń"], ActionLabels(Row(cut, "RDZ")));

        Action(cut, "RDZ", "Przywróć").Click();
        cut.WaitForAssertion(() => Assert.Equal("Aktywny", State(cut, "RDZ")));
    }

    [Fact]
    public async Task Delete_WithoutEntries_Confirmed_RemovesRow()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Codes(cut)));

        Action(cut, "RDZ", "Usuń").Click();
        cut.WaitForElement(".confirm-primary").Click();

        cut.WaitForAssertion(() => Assert.DoesNotContain("RDZ", Codes(cut)));
        Assert.Empty(await Store.ListAsync(Ct));
        WaitForMessage(cut, "success", "Usunięto Projekt RDZ.");
    }

    [Fact]
    public async Task Delete_Cancelled_DoesNothing()
    {
        await SeedAsync(("RDZ", "Rozwój", true));
        var cut = RenderScreen();
        cut.WaitForAssertion(() => Assert.Single(Codes(cut)));

        Action(cut, "RDZ", "Usuń").Click();
        cut.WaitForElement(".confirm-cancel").Click();

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
        cut.WaitForAssertion(() => Assert.Single(Codes(cut)));

        Action(cut, "RDZ", "Usuń").Click();
        cut.WaitForElement(".confirm-primary").Click();

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

    /// <summary>The screen in MainLayout, as the router renders it: "?" of the top bar, message area, dialogs.</summary>
    private IRenderedComponent<MainLayout> RenderScreen()
    {
        var cut = Render<MainLayout>(p => p.Add(l => l.Body, b =>
        {
            b.OpenComponent<ProjectsPage>(0);
            b.CloseComponent();
        }));
        // The list is loaded from the database; the grid appears after it.
        cut.WaitForElement(".report-grid", TimeSpan.FromSeconds(5));
        return cut;
    }

    private static async Task FillAndSubmitAsync(IRenderedComponent<MainLayout> cut, string code, string name)
    {
        var inputs = Inputs(cut);
        await cut.InvokeAsync(() => inputs[0].Instance.ValueChanged.InvokeAsync(code));
        await cut.InvokeAsync(() => inputs[1].Instance.ValueChanged.InvokeAsync(name));
        await cut.Find("form.project-form").SubmitAsync();
    }

    private static IReadOnlyList<IRenderedComponent<FluentTextInput>> Inputs(IRenderedComponent<MainLayout> cut) =>
        cut.FindComponents<FluentTextInput>();

    private static List<string> Codes(IRenderedComponent<MainLayout> cut) =>
        cut.FindAll("tbody tr td:first-child").Select(td => td.TextContent).ToList();

    private static IElement Row(IRenderedComponent<MainLayout> cut, string code) =>
        cut.FindAll("tbody tr").Single(tr => tr.QuerySelector("td")?.TextContent == code);

    private static string State(IRenderedComponent<MainLayout> cut, string code) =>
        Row(cut, code).QuerySelectorAll("td")[2].TextContent;

    private static IElement Action(IRenderedComponent<MainLayout> cut, string code, string label) =>
        Row(cut, code).QuerySelector($"fluent-button[aria-label='{label}']")!;

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
        cut.Find(".projects-add").Click();
        cut.WaitForElement("form.project-form");
    }

    private async Task<List<int>> SeedAsync(params (string Code, string Name, bool IsActive)[] projects)
    {
        await using var db = await _postgres.CreateUserContextAsync(_user);
        var entities = projects.Select(p => new Project { Code = p.Code, Name = p.Name, IsActive = p.IsActive }).ToList();
        db.Projects.AddRange(entities);
        await db.SaveChangesAsync(Ct);
        return entities.Select(e => e.Id).ToList();
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
