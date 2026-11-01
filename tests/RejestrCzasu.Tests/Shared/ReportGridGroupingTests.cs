using Bunit;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Localization;
using RejestrCzasu.Shared;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace RejestrCzasu.Tests.Shared;

/// <summary>ReportGrid fixed grouping (AD-9, R9, R17): headers with sums given by the screen; one test per I/O matrix row.</summary>
public sealed class ReportGridGroupingTests : BunitContext
{
    public sealed record Row(string Week, string Day, string Code, string Time, string Note);

    // Weeks T1 (days D1, D2) and T2 (day D3).
    private static readonly Row A = new("T1", "D1", "A", "2:00", "a");
    private static readonly Row B = new("T1", "D1", "B", "4:00", "b");
    private static readonly Row C = new("T1", "D2", "C", "4:00", "c");
    private static readonly Row D = new("T2", "D3", "D", "3:00", "d");

    private static readonly Dictionary<string, string> Sums = new()
    {
        ["T1"] = "10:00", ["T2"] = "3:00", ["D1"] = "6:00", ["D2"] = "4:00", ["D3"] = "3:00",
    };

    private static readonly ReportColumn<Row>[] Columns =
    [
        new("Kod", r => r.Code),
        new("Czas", r => r.Time),
        new("Opis", r => r.Note),
    ];

    private static readonly ReportGroup<Row>[] WeekDay =
    [
        new(r => r.Week, key => $"Tydzień {key}", key => Sums[key]),
        new(r => r.Day, key => $"Dzień {key}", key => Sums[key]),
    ];

    private readonly List<Row> _invoked = [];

    public ReportGridGroupingTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents(options => options.Localizer = new FluentLocalizer());
    }

    [Fact]
    public void TwoLevels_InsertHeadersWithScreenSums()
    {
        var cut = RenderGrid([A, B, C, D]);

        Assert.Equal(
            ["Tydzień T1||10:00", "Dzień D1||6:00", "A", "B", "Dzień D2||4:00", "C",
             "Tydzień T2||3:00", "Dzień D3||3:00", "D"],
            Rows(cut));
        Assert.Equal(
            ["report-grid-group-0", "report-grid-group-1", "", "", "report-grid-group-1", "", "report-grid-group-0", "report-grid-group-1", ""],
            RowLevels(cut));
    }

    [Fact]
    public async Task Filter_HidesDataRows_AndKeepsHeaderSums()
    {
        var cut = RenderGrid([A, B, C, D]);

        await SetValueAsync(cut, 0, "A", false);

        Assert.Equal(
            ["Tydzień T1||10:00", "Dzień D1||6:00", "B", "Dzień D2||4:00", "C",
             "Tydzień T2||3:00", "Dzień D3||3:00", "D"],
            Rows(cut));
    }

    [Fact]
    public async Task GroupWithoutShownRows_Disappears()
    {
        var cut = RenderGrid([A, B, C, D]);

        await SetValueAsync(cut, 0, "C", false);

        Assert.Equal(
            ["Tydzień T1||10:00", "Dzień D1||6:00", "A", "B", "Tydzień T2||3:00", "Dzień D3||3:00", "D"],
            Rows(cut));
    }

    [Fact]
    public async Task OuterGroupWithoutShownRows_DisappearsWithItsInnerGroups()
    {
        var cut = RenderGrid([A, B, C, D]);

        await SetValueAsync(cut, 0, "D", false);

        Assert.Equal(["Tydzień T1||10:00", "Dzień D1||6:00", "A", "B", "Dzień D2||4:00", "C"], Rows(cut));
    }

    [Fact]
    public async Task Counter_CountsOnlyDataRows()
    {
        var cut = RenderGrid([A, B, C, D]);
        Assert.Equal("Pozycje: 4", Counter(cut));

        await SetValueAsync(cut, 0, "A", false);

        Assert.Equal("Pozycje: 3 z 4", Counter(cut));
    }

    [Fact]
    public void GroupSumColumn_PutsSumsInThatColumn()
    {
        var cut = RenderGrid([A, B, C, D], sumColumn: 1);

        Assert.Equal(
            ["Tydzień T1|10:00|", "Dzień D1|6:00|", "A|2:00|a", "B|4:00|b", "Dzień D2|4:00|", "C|4:00|c",
             "Tydzień T2|3:00|", "Dzień D3|3:00|", "D|3:00|d"],
            Cells(cut));
    }

    [Fact]
    public void Groups_FollowFirstAppearanceOfKey()
    {
        var cut = RenderGrid([D, A, C, B]);

        Assert.Equal(
            ["Tydzień T2||3:00", "Dzień D3||3:00", "D", "Tydzień T1||10:00", "Dzień D1||6:00", "A", "B",
             "Dzień D2||4:00", "C"],
            Rows(cut));
    }

    [Fact]
    public void ActionIcon_CallsScreenActionWithItem_AndHeadersHaveNoIcons()
    {
        var cut = RenderGrid([A, B, C, D]);

        var rows = cut.FindAll("tbody tr");
        Assert.All(rows.Where(r => r.ClassList.Contains("report-grid-group")),
            header => Assert.Null(header.QuerySelector("fluent-button")));
        rows[3].QuerySelector("fluent-button[aria-label='Edytuj']")!.Click();

        Assert.Equal([B], _invoked);
    }

    [Fact]
    public async Task FilterValues_ComeOnlyFromDataRows()
    {
        var cut = RenderGrid([A, B, C, D]);

        // Column 2 (Opis) is the default sum column: header sums render there, and no data row has them as values.
        var sumCells = cut.FindAll("tbody tr.report-grid-group").Select(tr => tr.QuerySelectorAll("td")[2].TextContent.Trim()).ToList();
        Assert.Equal(["10:00", "6:00", "4:00", "3:00", "3:00"], sumCells);

        await ShowOptionsAsync(cut, 2);
        var labels = cut.FindComponents<FluentCheckbox>().Select(c => c.Instance.Label).ToList();
        Assert.Equal(["Zaznacz wszystkie", "a", "b", "c", "d"], labels);
        Assert.Empty(labels.Intersect(sumCells));
    }

    [Fact]
    public void NoGroups_RendersPlainRows()
    {
        var cut = Render<ReportGrid<Row>>(p => p
            .Add(g => g.Items, new[] { D, A, C, B })
            .Add(g => g.Columns, Columns)
            .Add(g => g.Groups, Array.Empty<ReportGroup<Row>>()));

        Assert.Equal(["D", "A", "C", "B"], Rows(cut));
        Assert.Empty(cut.FindAll("tr.report-grid-group"));
        Assert.Single(cut.FindComponents<FluentDataGrid<Row>>());
    }

    [Fact]
    public void SameInnerKeyInTwoOuterGroups_RendersBothHeaders_AndEqualRecordsStaySeparate()
    {
        var cut = RenderGrid([new Row("T1", "Pn", "A", "1:00", "a"), new Row("T1", "Pn", "A", "1:00", "a"), new Row("T2", "Pn", "B", "2:00", "b")],
            groups:
            [
                new(r => r.Week, key => $"Tydzień {key}"),
                new(r => r.Day, key => $"Dzień {key}"),
            ]);

        Assert.Equal(["Tydzień T1||", "Dzień Pn||", "A", "A", "Tydzień T2||", "Dzień Pn||", "B"], Rows(cut));
        Assert.Equal("Pozycje: 3", Counter(cut));
    }

    private IRenderedComponent<ReportGrid<Row>> RenderGrid(Row[] items, int? sumColumn = null, ReportGroup<Row>[]? groups = null) =>
        Render<ReportGrid<Row>>(p => p
            .Add(g => g.Items, items)
            .Add(g => g.Columns, Columns)
            .Add(g => g.Groups, groups ?? WeekDay)
            .Add(g => g.GroupSumColumn, sumColumn)
            .Add(g => g.Actions, new ReportAction<Row>[]
            {
                new(new Icons.Regular.Size20.Edit(), "Edytuj", row => { _invoked.Add(row); return Task.CompletedTask; }),
            }));

    private static async Task ShowOptionsAsync(IRenderedComponent<ReportGrid<Row>> cut, int column)
    {
        var grid = cut.FindComponent<FluentDataGrid<ReportGrid<Row>.GridRow>>();
        await cut.InvokeAsync(() => grid.Instance.ShowColumnOptionsAsync(column));
    }

    private static async Task SetValueAsync(IRenderedComponent<ReportGrid<Row>> cut, int column, string label, bool value)
    {
        await ShowOptionsAsync(cut, column);
        var checkbox = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == label);
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(value));
    }

    // Header rows as "title|...|sum" over the data columns; data rows as their first cell.
    private static List<string> Rows(IRenderedComponent<ReportGrid<Row>> cut) =>
        cut.FindAll("tbody tr")
            .Select(tr => tr.ClassList.Contains("report-grid-group")
                ? string.Join("|", tr.QuerySelectorAll("td").Take(Columns.Length).Select(td => td.TextContent.Trim())).Trim()
                : tr.QuerySelector("td")!.TextContent)
            .ToList();

    private static List<string> Cells(IRenderedComponent<ReportGrid<Row>> cut) =>
        cut.FindAll("tbody tr")
            .Select(tr => string.Join("|", tr.QuerySelectorAll("td").Take(Columns.Length).Select(td => td.TextContent.Trim())).Trim())
            .ToList();

    private static List<string> RowLevels(IRenderedComponent<ReportGrid<Row>> cut) =>
        cut.FindAll("tbody tr")
            .Select(tr => tr.ClassList.FirstOrDefault(c => c.StartsWith("report-grid-group-", StringComparison.Ordinal)) ?? "")
            .ToList();

    private static string Counter(IRenderedComponent<ReportGrid<Row>> cut) => cut.Find(".report-grid-counter").TextContent;
}
