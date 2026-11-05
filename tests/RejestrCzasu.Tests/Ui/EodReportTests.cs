using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Domain;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Ui;

/// <summary>EodReport of the eod-ui copy: Rejestr adjustments AD-23 items 1–5 and 11–15, one test per I/O matrix row.</summary>
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
    public void ShowExportFalse_HidesExportButton_EvenWithExportFile()
    {
        var cut = RenderReport(Plain, p => p.Add(r => r.ShowExport, false).Add(r => r.ExportFile, new Writer().Write));

        Assert.DoesNotContain("Eksport", cut.Markup);
    }

    [Fact]
    public void ShowExportOmitted_WithoutExportFile_NoExportButton()
    {
        var cut = RenderReport(Plain);

        Assert.DoesNotContain("Eksport", cut.Markup);
    }

    [Fact]
    public void ShowExportOmitted_WithExportFile_ShowsEksportujButton()
    {
        var cut = RenderReport(Plain, p => p.Add(r => r.ExportFile, new Writer().Write));

        Assert.Equal("Eksportuj", ExportButton(cut).TextContent.Trim());
        Assert.DoesNotContain("Eksport CSV", cut.Markup);
    }

    [Fact]
    public async Task PageSize_31Rows_TwoPages_FooterCountsAllRows()
    {
        var cut = RenderPaged(Numbered(31));

        Assert.Equal(30, Cells(cut, 0).Count);
        Assert.Equal("K01", Cells(cut, 0)[0]);
        Assert.Equal(("Pozycje 1–30 z 31", "Strona 1 z 2"), PaginatorTexts(cut));
        Assert.Equal("31 pozycji · ekran", Footer(cut));
        // The paginator lies between the table and the footer.
        Assert.Equal("eod-card-foot", cut.Find(".eod-paginator").NextElementSibling?.ClassName);

        await GoToNextPageAsync(cut);

        Assert.Equal(["K31"], Cells(cut, 0));
        Assert.Equal(("Pozycje 31–31 z 31", "Strona 2 z 2"), PaginatorTexts(cut));
        Assert.Equal("31 pozycji · ekran", Footer(cut));
    }

    [Fact]
    public void PageSize_30RowsOrFewer_NoPaginator()
    {
        var cut = RenderPaged(Numbered(30));

        Assert.Equal(30, Cells(cut, 0).Count);
        Assert.Empty(cut.FindAll(".eod-paginator"));
    }

    [Fact]
    public async Task PageSize_SearchFilterOrSorting_BackToFirstPage()
    {
        var cut = RenderPaged(Numbered(61));

        await GoToNextPageAsync(cut);
        Assert.Equal("Strona 2 z 3", PaginatorTexts(cut).Page);
        await cut.InvokeAsync(() => cut.FindComponents<FluentTextInput>()
            .Single(i => i.Instance.AriaLabel == "Szukaj we wszystkich kolumnach").Instance.ValueChanged.InvokeAsync("K"));
        Assert.Equal("Strona 1 z 3", PaginatorTexts(cut).Page);
        Assert.Equal("K01", Cells(cut, 0)[0]);

        await GoToNextPageAsync(cut);
        await OpenPopoverAsync(cut, "Czas");
        var checkbox = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "15m (1)");
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(false));
        Assert.Equal(("Pozycje 1–30 z 60", "Strona 1 z 2"), PaginatorTexts(cut));
        Assert.Equal("60 pozycji · ekran · po filtrach, z 61", Footer(cut));

        await GoToNextPageAsync(cut);
        await cut.InvokeAsync(() => cut.FindAll("th [col-sort-button]")[0].Click());
        Assert.Equal("Strona 1 z 2", PaginatorTexts(cut).Page);
    }

    [Fact]
    public async Task PageSize_GroupingOnAndOff_BackToFirstPage()
    {
        var cut = RenderPaged(Numbered(61));
        await GoToNextPageAsync(cut);
        Assert.Equal("Strona 2 z 3", PaginatorTexts(cut).Page);

        await OpenPopoverAsync(cut, "Projekt");
        await cut.InvokeAsync(() => cut.FindAll(".eod-filter-body fluent-button")
            .Single(b => b.TextContent.Trim() == "Grupuj według tej kolumny").Click());
        Assert.Empty(cut.FindAll(".eod-paginator"));
        Assert.Equal(61, Cells(cut, 0).Count);

        await cut.InvokeAsync(() => cut.Find("button[aria-label='Usuń poziom grupowania: Projekt']").Click());
        Assert.Equal(("Pozycje 1–30 z 61", "Strona 1 z 3"), PaginatorTexts(cut));
        Assert.Equal("K01", Cells(cut, 0)[0]);
    }

    [Fact]
    public async Task PageSize_ResetLayout_BackToFirstPage()
    {
        var cut = RenderPaged(Numbered(61));
        await GoToNextPageAsync(cut);
        await GoToNextPageAsync(cut);
        Assert.Equal("Strona 3 z 3", PaginatorTexts(cut).Page);

        var reset = cut.FindComponents<FluentMenuItem>().Single(i => i.Markup.Contains("Przywróć układ domyślny", StringComparison.Ordinal));
        await cut.InvokeAsync(() => reset.Instance.OnClick.InvokeAsync());

        Assert.Equal(("Pozycje 1–30 z 61", "Strona 1 z 3"), PaginatorTexts(cut));
        Assert.Equal("K01", Cells(cut, 0)[0]);
    }

    [Fact]
    public async Task PageSize_NewRows_KeepPage_TrimmedToLastPage()
    {
        var cut = RenderPaged(Numbered(61));
        await GoToNextPageAsync(cut);
        await GoToNextPageAsync(cut);
        Assert.Equal(["K61"], Cells(cut, 0));

        cut.Render(p => p.Add(r => r.Rows, Numbered(60)));
        Assert.Equal(("Pozycje 31–60 z 60", "Strona 2 z 2"), PaginatorTexts(cut));

        cut.Render(p => p.Add(r => r.Rows, Numbered(45)));
        Assert.Equal(("Pozycje 31–45 z 45", "Strona 2 z 2"), PaginatorTexts(cut));
        Assert.Equal("K31", Cells(cut, 0)[0]);

        cut.Render(p => p.Add(r => r.Rows, Numbered(20)));
        Assert.Empty(cut.FindAll(".eod-paginator"));
        Assert.Equal(20, Cells(cut, 0).Count);
    }

    [Fact]
    public void PageSize_WithGrouping_NoPaginator_WholeGroups()
    {
        var cut = RenderReport(GroupedByProjekt, p => p.Add(r => r.PageSize, 30), Numbered(31));

        Assert.Equal(31, Cells(cut, 0).Count);
        Assert.Empty(cut.FindAll(".eod-paginator"));
    }

    [Fact]
    public void PageSizeOmitted_NoPaginator_AllRows()
    {
        var cut = RenderReport(Plain, rows: Numbered(31));

        Assert.Equal(31, Cells(cut, 0).Count);
        Assert.Empty(cut.FindAll(".eod-paginator"));
    }

    [Fact]
    public async Task Export_AfterColumnFilter_WriterGetsVisibleColumnsAndAllPagesOfFilteredRows()
    {
        var writer = new Writer();
        var cut = RenderPaged(Numbered(61), p => p.Add(r => r.DataAsOf, new DateOnly(2026, 11, 4)).Add(r => r.ExportFile, writer.Write));
        await OpenPopoverAsync(cut, "Czas");
        var checkbox = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == "15m (1)");
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(false));
        Assert.Equal(30, Cells(cut, 0).Count);

        await cut.InvokeAsync(() => ExportButton(cut).Click());

        Assert.Equal(["kod", "projekt", "czas"], writer.Columns!.Select(c => c.Key));
        Assert.Equal(60, writer.Rows!.Count);
        Assert.Equal("K02", writer.Rows[0]["kod"]);
        Assert.Equal("K61", writer.Rows[^1]["kod"]);
        var download = Assert.Single(JSInterop.Invocations, i => i.Identifier == "EodUI.downloadStream");
        Assert.Equal("raport-2026-11-04.txt", download.Arguments[0]);
        var stream = Assert.IsType<Microsoft.JSInterop.DotNetStreamReference>(download.Arguments[1]);
        Assert.Equal(Writer.Content, Assert.IsType<MemoryStream>(stream.Stream).ToArray());
        Assert.Equal("text/plain", download.Arguments[2]);
    }

    [Fact]
    public async Task Export_HiddenColumn_NotGivenToWriter()
    {
        var writer = new Writer();
        var cut = RenderReport(Plain, p => p.Add(r => r.ExportFile, writer.Write));
        var layout = Services.GetRequiredService<ReportLayoutStore>().Get("plain", Plain);
        layout.Hidden.Add("projekt");
        cut.Render();

        await cut.InvokeAsync(() => ExportButton(cut).Click());

        Assert.Equal(["kod", "czas"], writer.Columns!.Select(c => c.Key));
        Assert.Equal(3, writer.Rows!.Count);
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
    public void GroupLabelFalse_GroupRowShowsOnlyTheValue_DefaultWithColumnLabel()
    {
        var withLabel = RenderReport(GroupedByProjekt);
        Assert.Equal(["Projekt: P1", "Projekt: P2"], GroupLabels(withLabel).Order());

        var definition = GroupedByProjekt with
        {
            Columns = [Kod, Projekt with { GroupLabel = false, Format = v => $"Grupa {v}" }, CzasColumn],
        };
        var cut = RenderReport(definition);

        Assert.Equal(["Grupa P1", "Grupa P2"], GroupLabels(cut).Order());
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

    private IRenderedComponent<EodReport> RenderPaged(
        IReadOnlyDictionary<string, object?>[] rows,
        Action<ComponentParameterCollectionBuilder<EodReport>>? parameters = null) =>
        RenderReport(Plain, p =>
        {
            p.Add(r => r.PageSize, 30).Add(r => r.PreserveRowOrder, true).Add(r => r.FooterSummary, "ekran");
            parameters?.Invoke(p);
        }, rows);

    // K01…Kn in input order, Projekt P1 for odd and P2 for even numbers, Czas 15m × number (each value once).
    private static IReadOnlyDictionary<string, object?>[] Numbered(int count) =>
        [.. Enumerable.Range(1, count).Select(i => Row($"K{i:00}", i % 2 == 1 ? "P1" : "P2", 15 * i))];

    private static List<string> GroupLabels(IRenderedComponent<EodReport> cut) =>
        cut.FindAll("tr.eod-group-row .eod-group > span:not(.eod-caption)").Select(e => e.TextContent.Trim()).ToList();

    private static AngleSharp.Dom.IElement ExportButton(IRenderedComponent<EodReport> cut) =>
        cut.FindAll(".eod-formbar fluent-button").Single(b => b.TextContent.Trim().StartsWith("Eksport", StringComparison.Ordinal));

    private static (string Range, string Page) PaginatorTexts(IRenderedComponent<EodReport> cut) =>
        (cut.Find(".eod-paginator .summary").TextContent.Trim(), cut.Find(".eod-paginator .pagination-text").TextContent.Trim());

    // The third button of FluentPaginator: first, previous, next, last.
    private static Task GoToNextPageAsync(IRenderedComponent<EodReport> cut)
    {
        var next = cut.FindComponent<FluentPaginator>().FindComponents<FluentButton>()[2];
        return cut.InvokeAsync(() => next.Instance.OnClick.InvokeAsync());
    }

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

    /// <summary>A fake writer of the export file: records what EodReport gives it.</summary>
    private sealed class Writer
    {
        public static readonly byte[] Content = [1, 2, 3];

        public IReadOnlyList<ReportColumn>? Columns { get; private set; }

        public IReadOnlyList<IReadOnlyDictionary<string, object?>>? Rows { get; private set; }

        public ReportFile Write(IReadOnlyList<ReportColumn> columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
        {
            Columns = columns;
            Rows = rows;
            return new ReportFile("txt", "text/plain", Content);
        }
    }
}
