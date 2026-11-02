using RejestrCzasu.Domain;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Ui;

/// <summary>ReportEngine of the eod-ui copy: Rejestr adjustments AD-23 items 2 (Format) and 3 (PreserveRowOrder).</summary>
public sealed class ReportEngineTests
{
    private static readonly ReportColumn Kod = new("kod", "Kod") { IsKey = true };
    private static readonly ReportColumn Projekt = new("projekt", "Projekt", ColumnType.List);
    private static readonly ReportColumn CzasFormatted = new("czas", "Czas", ColumnType.List) { Format = v => Czas.Format((int)v!) };
    private static readonly ReportColumn CzasRaw = new("czas", "Czas", ColumnType.List);

    private static readonly ReportDefinition Definition = new()
    {
        Key = "test",
        Title = "Test",
        Description = "Test",
        FileName = "test",
        Columns = [Kod, Projekt, CzasFormatted],
    };

    // Input order C, D, B, A; projects P2, P1, P2, P1; equal times in pairs.
    private static readonly IReadOnlyDictionary<string, object?>[] Source =
    [
        Row("C", "P2", 30),
        Row("D", "P1", 60),
        Row("B", "P2", 60),
        Row("A", "P1", 30),
    ];

    [Fact]
    public void Display_WithFormat_UsesFormat()
    {
        Assert.Equal("1h 30m", ReportEngine.Display(CzasFormatted, 90));
        Assert.Equal("90", ReportEngine.Display(CzasRaw, 90));
        Assert.Equal(string.Empty, ReportEngine.Display(CzasFormatted, null));
    }

    [Fact]
    public void Compare_WithFormat_ComparesRawNumbers()
    {
        Assert.True(ReportEngine.Compare(CzasFormatted, 120, 30) > 0);
        Assert.True(ReportEngine.Compare(CzasFormatted, 30, 45) < 0);
        Assert.Equal(0, ReportEngine.Compare(CzasFormatted, 45, 45));
        Assert.True(ReportEngine.Compare(CzasFormatted, null, 15) < 0);
    }

    [Fact]
    public void Compare_WithFormatAndValueOrder_UsesValueOrder()
    {
        var column = new ReportColumn("etap", "Etap", ColumnType.List) { Format = v => $"[{v}]", ValueOrder = ["Zgłoszenie", "Akceptacja"] };

        Assert.True(ReportEngine.Compare(column, "Zgłoszenie", "Akceptacja") < 0);
    }

    [Fact]
    public void Compare_WithFormatAndStrings_UsesPolishCulture()
    {
        var column = new ReportColumn("nazwa", "Nazwa", ColumnType.List) { Format = v => $"[{v}]" };

        // Polish order: L < Ł < M (ordinal would put Ł after M)
        Assert.True(ReportEngine.Compare(column, "Ł", "M") < 0);
        Assert.True(ReportEngine.Compare(column, "L", "Ł") < 0);
    }

    [Fact]
    public void Compare_WithoutFormat_ListComparesText()
    {
        Assert.True(ReportEngine.Compare(CzasRaw, 120, 30) < 0);
    }

    [Fact]
    public void DistinctValues_WithFormat_InRawOrder()
    {
        var rows = new[] { Row("A", "P", 120), Row("B", "P", 30), Row("C", "P", 45) };

        Assert.Equal([30, 45, 120], ReportEngine.DistinctValues(CzasFormatted, rows).Cast<int>());
    }

    [Fact]
    public void DescribeFilter_WithFormatAndRows_ShowsFormattedValuesInRawOrder()
    {
        var rows = new[] { Row("A", "P", 120), Row("B", "P", 30), Row("C", "P", 45) };
        var filter = new ListFilter(new HashSet<string> { "120", "30" });

        Assert.Equal("30m, 2h", ReportEngine.DescribeFilter(CzasFormatted, filter, rows));
    }

    [Fact]
    public void Matches_ListFilterWithFormat_FiltersByRawValue()
    {
        var filter = new ListFilter(new HashSet<string> { "30" });

        Assert.True(ReportEngine.Matches(CzasFormatted, filter, 30));
        Assert.False(ReportEngine.Matches(CzasFormatted, filter, 45));
    }

    [Fact]
    public void Rows_PreserveRowOrder_WithoutSorting_KeepsInputOrder()
    {
        var layout = new ReportLayout(Definition);

        Assert.Equal(["C", "D", "B", "A"], Codes(ReportEngine.Rows(layout, Source, preserveRowOrder: true)));
    }

    [Fact]
    public void Rows_Default_OrdersByKeyColumn()
    {
        var layout = new ReportLayout(Definition);

        Assert.Equal(["A", "B", "C", "D"], Codes(ReportEngine.Rows(layout, Source)));
    }

    [Fact]
    public void Rows_PreserveRowOrder_WithGrouping_KeepsInputOrderInsideGroups()
    {
        var layout = new ReportLayout(Definition);
        layout.Grouping.Add("projekt");

        Assert.Equal(["D", "A", "C", "B"], Codes(ReportEngine.Rows(layout, Source, preserveRowOrder: true)));
        Assert.Equal(["A", "D", "B", "C"], Codes(ReportEngine.Rows(layout, Source)));
    }

    [Fact]
    public void Rows_PreserveRowOrder_WithSorting_TiesKeepInputOrder()
    {
        var layout = new ReportLayout(Definition);
        layout.Sorting.Add(new ReportSort("czas", Descending: true));

        Assert.Equal(["D", "B", "C", "A"], Codes(ReportEngine.Rows(layout, Source, preserveRowOrder: true)));
        Assert.Equal(["B", "D", "A", "C"], Codes(ReportEngine.Rows(layout, Source)));
    }

    [Fact]
    public void Groups_CarryRawKeyOfFirstRow()
    {
        var layout = new ReportLayout(Definition);
        layout.Grouping.Add("czas");
        var rows = ReportEngine.Rows(layout, Source, preserveRowOrder: true);

        var groups = ReportEngine.Groups(layout, rows, 0, string.Empty);

        Assert.Equal(["30m", "1h"], groups.Select(g => g.Value));
        Assert.Equal([30, 60], groups.Select(g => (int)g.Key!));
    }

    private static IReadOnlyDictionary<string, object?> Row(string kod, string projekt, int czas) =>
        new Dictionary<string, object?> { ["kod"] = kod, ["projekt"] = projekt, ["czas"] = czas };

    private static IEnumerable<string> Codes(IEnumerable<IReadOnlyDictionary<string, object?>> rows) =>
        rows.Select(r => (string)r["kod"]!);
}
