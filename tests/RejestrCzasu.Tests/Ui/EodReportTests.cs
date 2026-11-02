using Bunit;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Domain;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Ui;

/// <summary>EodReport of the eod-ui copy: Rejestr adjustments AD-23 items 1–5, one test per I/O matrix row.</summary>
public sealed class EodReportTests : BunitContext
{
    private static readonly ReportColumn Kod = new("kod", "Kod") { IsKey = true };
    private static readonly ReportColumn Projekt = new("projekt", "Projekt", ColumnType.List);
    private static readonly ReportColumn CzasColumn = new("czas", "Czas", ColumnType.List) { Format = v => Czas.Format((int)v!) };

    private static readonly ReportDefinition Plain = new()
    {
        Key = "plain",
        Title = "Raport",
        Description = "Raport testowy",
        FileName = "raport",
        Columns = [Kod, Projekt, CzasColumn],
        Total = new ReportTotal("czas", "suma wzorca"),
    };

    private static readonly ReportDefinition SortedByCzas = Plain with { Key = "sorted", Sorting = [new ReportSort("czas")] };

    private static readonly ReportDefinition GroupedByProjekt = Plain with { Key = "grouped", Grouping = ["projekt"] };

    // Input order C, A, B
    private static readonly IReadOnlyDictionary<string, object?>[] Source =
    [
        Row("C", "P2", 120),
        Row("A", "P1", 30),
        Row("B", "P2", 45),
    ];

    public EodReportTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddEodPlatformUI();
    }

    [Fact]
    public void ShowExportFalse_HidesExportButton()
    {
        var cut = RenderReport(Plain, p => p.Add(r => r.ShowExport, false));

        Assert.DoesNotContain("Eksport CSV", cut.Markup);
    }

    [Fact]
    public void ShowExportOmitted_ShowsExportButton()
    {
        var cut = RenderReport(Plain);

        Assert.Contains("Eksport CSV", cut.Markup);
    }

    [Fact]
    public void Format_ShowsCzasText_AndSortsByMinutes()
    {
        var cut = RenderReport(SortedByCzas);

        Assert.Equal(["30m", "45m", "2h"], Cells(cut, 2));
        Assert.Equal(["A", "B", "C"], Cells(cut, 0));
    }

    [Fact]
    public async Task Format_InListFilter_ShowsText_AndFiltersByValue()
    {
        var cut = RenderReport(SortedByCzas);

        await OpenPopoverAsync(cut, "Czas");
        Assert.Equal(["30m (1)", "45m (1)", "2h (1)"], cut.FindComponents<FluentCheckbox>().Select(c => c.Instance.Label));

        var checkbox = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "45m (1)");
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(false));

        Assert.Equal(["30m", "2h"], Cells(cut, 2));
        Assert.Contains("Czas: 30m, 2h", cut.Find(".eod-statebar").TextContent);
    }

    [Fact]
    public void PreserveRowOrder_WithoutSorting_KeepsInputOrder()
    {
        var cut = RenderReport(Plain, p => p.Add(r => r.PreserveRowOrder, true));

        Assert.Equal(["C", "A", "B"], Cells(cut, 0));
    }

    [Fact]
    public void PreserveRowOrder_WithGrouping_KeepsInputOrderInsideGroups()
    {
        var rows = new[] { Row("C", "P2", 120), Row("A", "P1", 30), Row("B", "P2", 45), Row("D", "P1", 15) };

        var cut = RenderReport(GroupedByProjekt, p => p.Add(r => r.PreserveRowOrder, true), rows);

        Assert.Equal(["A", "D", "C", "B"], Cells(cut, 0));
    }

    [Fact]
    public void PreserveRowOrderOmitted_OrdersByKeyColumn()
    {
        var cut = RenderReport(Plain);

        Assert.Equal(["A", "B", "C"], Cells(cut, 0));
    }

    [Fact]
    public void GroupSummary_ReplacesTotal_WithCounterAndScreenTextFromRawKey()
    {
        var calls = new List<(int Level, object? Key)>();
        var cut = RenderReport(GroupedByProjekt, p => p
            .Add(r => r.PreserveRowOrder, true)
            .Add(r => r.GroupSummary, (level, key) => { calls.Add((level, key)); return $"suma {key}"; }));

        var summaries = cut.FindAll("tr.eod-group-row .eod-caption").Select(e => e.TextContent.Trim()).ToList();

        Assert.Equal(["1 pozycja · suma P1", "2 pozycje · suma P2"], summaries);
        Assert.Contains((0, (object?)"P1"), calls);
        Assert.All(cut.FindAll("tr.eod-group-row"), g => Assert.DoesNotContain("suma wzorca", g.TextContent));
    }

    [Fact]
    public void GroupSummaryOmitted_UsesDefinitionTotal()
    {
        var cut = RenderReport(GroupedByProjekt);

        Assert.Contains("suma wzorca", cut.Find("tr.eod-group-row .eod-caption").TextContent);
    }

    [Fact]
    public async Task FooterSummary_ReplacesTotal_AndAddsFilterNote()
    {
        var cut = RenderReport(SortedByCzas, p => p.Add(r => r.FooterSummary, "suma 3h 15m"));

        Assert.Equal("3 pozycje · suma 3h 15m", Footer(cut));

        await OpenPopoverAsync(cut, "Czas");
        var checkbox = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "45m (1)");
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(false));

        Assert.Equal("2 pozycje · suma 3h 15m · po filtrach, z 3", Footer(cut));
    }

    [Fact]
    public void FooterSummaryOmitted_UsesDefinitionTotal()
    {
        var cut = RenderReport(Plain);

        Assert.Equal("3 pozycje · suma wzorca: 195", Footer(cut));
    }

    [Fact]
    public async Task LockedLayout_HasNoLayoutChanges_ButFilterAndCollapseWork()
    {
        var definition = GroupedByProjekt with { Sorting = [new ReportSort("czas")] };
        var cut = RenderReport(definition, p => p.Add(r => r.LockedLayout, true));

        Assert.Empty(cut.FindAll("th [col-sort-button]"));
        Assert.Empty(cut.FindAll("th[draggable]"));
        Assert.Empty(cut.FindAll(".eod-statebar .eod-chip button"));
        Assert.DoesNotContain("Dodaj poziom", cut.Markup);
        Assert.Equal(["Kod", "Projekt", "Czas"], cut.FindAll("th .eod-th-text").Select(e => e.TextContent));

        await OpenPopoverAsync(cut, "Czas");
        var popover = cut.Find(".eod-filter-body").TextContent;
        Assert.DoesNotContain("Rosnąco", popover);
        Assert.DoesNotContain("grupowania", popover);
        Assert.DoesNotContain("Kolejność", popover);
        var checkbox = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "45m (1)");
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(false));
        Assert.Equal(["A", "C"], Cells(cut, 0));

        await cut.InvokeAsync(() => cut.Find("tr.eod-group-row fluent-button").Click());
        Assert.Equal(["C"], Cells(cut, 0));
    }

    [Fact]
    public void LockedLayoutOmitted_HasSortButtonsAndDragging()
    {
        var cut = RenderReport(Plain);

        Assert.Equal(3, cut.FindAll("th [col-sort-button]").Count);
        Assert.Equal(2, cut.FindAll("th[draggable]").Count);
        Assert.Contains("Dodaj poziom", cut.Markup);
    }

    private IRenderedComponent<EodReport> RenderReport(
        ReportDefinition definition,
        Action<ComponentParameterCollectionBuilder<EodReport>>? parameters = null,
        IReadOnlyDictionary<string, object?>[]? rows = null) =>
        Render<EodReport>(p =>
        {
            p.Add(r => r.Definition, definition).Add(r => r.Rows, rows ?? Source);
            parameters?.Invoke(p);
        });

    private static async Task OpenPopoverAsync(IRenderedComponent<EodReport> cut, string label)
    {
        var button = cut.FindAll("th fluent-button").Single(b => b.GetAttribute("title")?.EndsWith($": {label}", StringComparison.Ordinal) == true);
        await cut.InvokeAsync(() => button.Click());
    }

    private static List<string> Cells(IRenderedComponent<EodReport> cut, int column) =>
        cut.FindAll("tbody tr[hover]").Select(r => r.QuerySelectorAll("td")[column].TextContent.Trim()).ToList();

    private static string Footer(IRenderedComponent<EodReport> cut) => cut.Find(".eod-card-foot span").TextContent.Trim();

    private static IReadOnlyDictionary<string, object?> Row(string kod, string projekt, int czas) =>
        new Dictionary<string, object?> { ["kod"] = kod, ["projekt"] = projekt, ["czas"] = czas };
}
