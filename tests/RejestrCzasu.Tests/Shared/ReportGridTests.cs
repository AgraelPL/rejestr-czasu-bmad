using Bunit;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu.Localization;
using RejestrCzasu.Shared;
using Icons = Microsoft.FluentUI.AspNetCore.Components.Icons;

namespace RejestrCzasu.Tests.Shared;

/// <summary>ReportGrid (AD-9, R17): input order, value filter, counter, row actions; one test per I/O matrix row.</summary>
public sealed class ReportGridTests : BunitContext
{
    public sealed record Row(string Code, string Name);

    private static readonly Row A = new("A", "Alfa");
    private static readonly Row B = new("B", "Beta");
    private static readonly Row C = new("C", "Alfa");

    private static readonly ReportColumn<Row>[] Columns =
    [
        new("Kod", r => r.Code),
        new("Nazwa", r => r.Name),
    ];

    private readonly List<Row> _invoked = [];

    public ReportGridTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddFluentUIComponents(options => options.Localizer = new FluentLocalizer());
    }

    [Fact]
    public void Rows_KeepInputOrder()
    {
        var cut = RenderGrid([C, A, B]);

        Assert.Equal(["C", "A", "B"], Codes(cut));
    }

    [Fact]
    public void Counter_WithoutFilter_ShowsAllRows()
    {
        var cut = RenderGrid([C, A, B]);

        Assert.Equal("Pozycje: 3", Counter(cut));
    }

    [Fact]
    public async Task UncheckedValue_HidesItsRows_AndMarksColumnFiltered()
    {
        var cut = RenderGrid([C, A, B]);

        await SetValueAsync(cut, 0, "A", false);

        Assert.Equal(["C", "B"], Codes(cut));
        Assert.Equal("Pozycje: 2 z 3", Counter(cut));
        Assert.True(Column(cut, 0).Filtered);
        Assert.False(Column(cut, 1).Filtered);
    }

    [Fact]
    public async Task ColumnOptions_ListDistinctValuesInPolishOrder_AfterSelectAll()
    {
        var cut = RenderGrid([new Row("Ż", "x"), new Row("Ł", "x"), new Row("L", "x"), new Row("Z", "x"), new Row("L", "y")]);

        await ShowOptionsAsync(cut, 0);

        Assert.Equal(["Zaznacz wszystkie", "L", "Ł", "Z", "Ż"], cut.FindComponents<FluentCheckbox>().Select(c => c.Instance.Label));
    }

    [Fact]
    public async Task FiltersOfTwoColumns_Combine()
    {
        var cut = RenderGrid([C, A, B]);

        await SetValueAsync(cut, 0, "C", false);
        await SetValueAsync(cut, 1, "Beta", false);

        Assert.Equal(["A"], Codes(cut));
        Assert.Equal("Pozycje: 1 z 3", Counter(cut));
        Assert.True(Column(cut, 0).Filtered);
        Assert.True(Column(cut, 1).Filtered);
    }

    [Fact]
    public async Task SelectAll_BringsBackEveryRow()
    {
        var cut = RenderGrid([C, A, B]);
        await SetValueAsync(cut, 0, "A", false);

        await SetValueAsync(cut, 0, "Zaznacz wszystkie", true);

        Assert.Equal(["C", "A", "B"], Codes(cut));
        Assert.Equal("Pozycje: 3", Counter(cut));
        Assert.False(Column(cut, 0).Filtered);
    }

    [Fact]
    public async Task SelectAll_Unchecked_HidesEveryRow()
    {
        var cut = RenderGrid([C, A, B]);

        await SetValueAsync(cut, 0, "Zaznacz wszystkie", false);

        Assert.Equal(["Brak danych do wyświetlenia."], Codes(cut));
        Assert.Equal("Pozycje: 0 z 3", Counter(cut));
    }

    [Fact]
    public async Task NewItems_ShowNewValue_AndKeepUncheckedValuesHidden()
    {
        var cut = RenderGrid([C, A, B]);
        await SetValueAsync(cut, 0, "A", false);

        cut.Render(p => p.Add(g => g.Items, new[] { C, A, B, new Row("D", "Delta") }));

        cut.WaitForAssertion(() => Assert.Equal(["C", "B", "D"], Codes(cut)));
        Assert.Equal("Pozycje: 3 z 4", Counter(cut));
        Assert.True(Column(cut, 0).Filtered);
    }

    [Fact]
    public void ActionIcon_CallsScreenActionWithRow()
    {
        var cut = RenderGrid([C, A, B]);

        var button = cut.FindAll("tbody tr")[2].QuerySelector("fluent-button[aria-label='Edytuj']")!;
        Assert.Equal("Edytuj", button.GetAttribute("title"));
        button.Click();

        Assert.Equal([B], _invoked);
    }

    [Fact]
    public void ActionWithFalseCondition_HasNoIconInThatRow()
    {
        var cut = RenderGrid([C, A, B]);

        var rows = cut.FindAll("tbody tr");
        Assert.Null(rows[1].QuerySelector("fluent-button[aria-label='Usuń']"));
        Assert.NotNull(rows[0].QuerySelector("fluent-button[aria-label='Usuń']"));
        Assert.NotNull(rows[1].QuerySelector("fluent-button[aria-label='Edytuj']"));
    }

    [Fact]
    public void EmptyItems_ShowPolishEmptyText_AndZeroCounter()
    {
        var cut = RenderGrid([]);

        Assert.Contains("Brak danych do wyświetlenia.", cut.Markup);
        Assert.Equal("Pozycje: 0", Counter(cut));
    }

    [Fact]
    public void EqualRecords_RenderAsSeparateRows()
    {
        var cut = RenderGrid([new Row("A", "Alfa"), new Row("A", "Alfa"), B]);

        Assert.Equal(["A", "A", "B"], Codes(cut));
        Assert.Equal("Pozycje: 3", Counter(cut));
    }

    [Fact]
    public async Task EmptyCellValue_IsListedAsEmpty()
    {
        var cut = Render<ReportGrid<Row>>(p => p
            .Add(g => g.Items, new[] { A, new Row("", "x") })
            .Add(g => g.Columns, new ReportColumn<Row>[] { new("Kod", r => r.Code), new("Brak", _ => null!) }));

        await ShowOptionsAsync(cut, 0);
        Assert.Equal(["Zaznacz wszystkie", "(puste)", "A"], cut.FindComponents<FluentCheckbox>().Select(c => c.Instance.Label));

        await ShowOptionsAsync(cut, 1);
        Assert.Equal(["Zaznacz wszystkie", "(puste)"], cut.FindComponents<FluentCheckbox>().Select(c => c.Instance.Label));
    }

    private IRenderedComponent<ReportGrid<Row>> RenderGrid(Row[] items) =>
        Render<ReportGrid<Row>>(p => p
            .Add(g => g.Items, items)
            .Add(g => g.Columns, Columns)
            .Add(g => g.Actions, new ReportAction<Row>[]
            {
                new(new Icons.Regular.Size20.Edit(), "Edytuj", row => { _invoked.Add(row); return Task.CompletedTask; }),
                new(new Icons.Regular.Size20.Delete(), "Usuń", _ => Task.CompletedTask) { Visible = row => row != A },
            }));

    private static async Task ShowOptionsAsync(IRenderedComponent<ReportGrid<Row>> cut, int column)
    {
        var grid = cut.FindComponent<FluentDataGrid<Row>>();
        await cut.InvokeAsync(() => grid.Instance.ShowColumnOptionsAsync(column));
    }

    private static async Task SetValueAsync(IRenderedComponent<ReportGrid<Row>> cut, int column, string label, bool value)
    {
        await ShowOptionsAsync(cut, column);
        var checkbox = cut.FindComponents<FluentCheckbox>().Single(c => c.Instance.Label == label);
        await cut.InvokeAsync(() => checkbox.Instance.ValueChanged.InvokeAsync(value));
    }

    private static TemplateColumn<Row> Column(IRenderedComponent<ReportGrid<Row>> cut, int index) =>
        cut.FindComponents<TemplateColumn<Row>>()[index].Instance;

    private static List<string> Codes(IRenderedComponent<ReportGrid<Row>> cut) =>
        cut.FindAll("tbody tr td:first-child").Select(td => td.TextContent).ToList();

    private static string Counter(IRenderedComponent<ReportGrid<Row>> cut) => cut.Find(".report-grid-counter").TextContent;
}
