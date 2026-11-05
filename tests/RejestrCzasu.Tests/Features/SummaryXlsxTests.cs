using ClosedXML.Excel;
using RejestrCzasu.Domain;
using RejestrCzasu.Features.Zestawienie;
using RejestrCzasu.Reporting;
using RejestrCzasu.Tests.Reporting;

namespace RejestrCzasu.Tests.Features;

/// <summary>The xlsx writer of the Zestawienie (SummaryXlsx, export-layout.md) read back with ClosedXML.</summary>
public sealed class SummaryXlsxTests
{
    private static readonly YearMonth September = new(2026, 9);

    private static MonthReport SeptemberReport() => MonthReport.Build(September, SeptemberShape.Shuffled());

    [Fact]
    public void September_SheetHeaderFrozenAndBold()
    {
        using var workbook = XlsxDownload.Open(SummaryXlsx.Write(SeptemberReport()));

        var sheet = Assert.Single(workbook.Worksheets);
        Assert.Equal("Wrzesień 2026", sheet.Name);
        Assert.Equal(
            ["Dzień tygodnia", "Data", "Kod", "Typ", "Opis", "Godziny", "Suma dnia", "Suma tygodnia"],
            Enumerable.Range(1, 8).Select(c => sheet.Cell(1, c).GetText()));
        Assert.All(Enumerable.Range(1, 8), c => Assert.True(sheet.Cell(1, c).Style.Font.Bold));
        Assert.Equal(1, sheet.SheetView.SplitRow);
        Assert.Equal(0, sheet.SheetView.SplitColumn);
    }

    [Fact]
    public void September_RowsTwoToTwentyNine_EmptyThirty_RazemThirtyOne()
    {
        var report = SeptemberReport();
        Assert.Equal(28, report.Rows.Count);

        using var workbook = XlsxDownload.Open(SummaryXlsx.Write(report));
        var sheet = workbook.Worksheet(1);

        Assert.All(Enumerable.Range(2, 28), r => Assert.False(sheet.Cell(r, 3).IsEmpty()));
        Assert.True(sheet.Row(30).IsEmpty());
        Assert.Equal("RAZEM WRZESIEŃ 2026", sheet.Cell(31, 5).GetText());
        Assert.Equal(103.0, sheet.Cell(31, 6).GetDouble());
        Assert.Equal(103.0, sheet.Cell(31, 8).GetDouble());
        Assert.All(new[] { 5, 6, 8 }, c => Assert.True(sheet.Cell(31, c).Style.Font.Bold));
        Assert.True(sheet.Row(31).Style.Font.Bold);
        Assert.Equal(31, sheet.LastRowUsed()!.RowNumber());
        // RAZEM equals the sums of the columns F and H, which the writer never adds up itself.
        Assert.Equal(103.0, Enumerable.Range(2, 28).Sum(r => sheet.Cell(r, 6).GetDouble()));
        Assert.Equal(103.0, Enumerable.Range(2, 28).Sum(r => sheet.Cell(r, 8).IsEmpty() ? 0 : sheet.Cell(r, 8).GetDouble()));
    }

    [Fact]
    public void September_WeekSumOnlyInFirstRowOfWeek_DaySumDayNameAndDateOnlyInFirstRowOfDay()
    {
        var report = SeptemberReport();
        using var workbook = XlsxDownload.Open(SummaryXlsx.Write(report));
        var sheet = workbook.Worksheet(1);

        var weekSums = new List<double>();
        var weekFirsts = new HashSet<DateOnly>();
        DateOnly? previousDay = null;
        for (var i = 0; i < report.Rows.Count; i++)
        {
            var row = report.Rows[i];
            var r = i + 2;
            var firstOfDay = row.Date != previousDay;
            var firstOfWeek = weekFirsts.Add(MonthReport.WeekFirst(row.Date));
            previousDay = row.Date;

            var h = sheet.Cell(r, 8);
            Assert.Equal(firstOfWeek, !h.IsEmpty());
            if (firstOfWeek)
            {
                Assert.Equal(XLDataType.Number, h.DataType);
                weekSums.Add(h.GetDouble());
            }
            foreach (var c in new[] { 1, 2, 7 })
            {
                Assert.Equal(firstOfDay, !sheet.Cell(r, c).IsEmpty());
            }
            if (firstOfDay)
            {
                Assert.Equal(PolishCalendar.DayName(row.Date), sheet.Cell(r, 1).GetText());
                var date = sheet.Cell(r, 2);
                Assert.Equal(XLDataType.DateTime, date.DataType);
                Assert.Equal(row.Date.ToDateTime(TimeOnly.MinValue), date.GetDateTime());
                Assert.Equal("dd.mm.yyyy", date.Style.NumberFormat.Format);
                Assert.Equal(XLDataType.Number, sheet.Cell(r, 7).DataType);
                Assert.Equal((double)Czas.ToDecimalHours(report.DayMinutes(row.Date)), sheet.Cell(r, 7).GetDouble());
            }
            Assert.Equal(XLDataType.Number, sheet.Cell(r, 6).DataType);
            Assert.Equal((double)Czas.ToDecimalHours(row.Minutes), sheet.Cell(r, 6).GetDouble());
            Assert.Equal(XLDataType.Text, sheet.Cell(r, 3).DataType);
            Assert.Equal(XLDataType.Text, sheet.Cell(r, 4).DataType);
            Assert.True(sheet.Cell(r, 5).Style.Alignment.WrapText);
        }
        Assert.Equal([30.0, 16, 18, 25, 14], weekSums);
    }

    [Fact]
    public void September_MergedDescriptions_InIdOrder_OnNewLines_AndSrodowiskoInD()
    {
        using var workbook = XlsxDownload.Open(SummaryXlsx.Write(SeptemberReport()));
        var sheet = workbook.Worksheet(1);
        var rows = Enumerable.Range(2, 28).ToList();

        var merged = rows.Single(r =>
            sheet.Cell(r, 3).GetText() == "AAA" && sheet.Cell(r, 4).GetText() == "Development"
            && sheet.Cell(r, 5).GetText().Contains("Zadanie 4", StringComparison.Ordinal));
        Assert.Equal("Zadanie 4\nZadanie 9", sheet.Cell(merged, 5).GetText());
        Assert.True(sheet.Cell(merged, 5).Style.Alignment.WrapText);
        Assert.Equal(new DateTime(2026, 9, 2), sheet.Cell(merged, 2).GetDateTime());
        Assert.Equal(4.0, sheet.Cell(merged, 6).GetDouble());
        Assert.Contains(rows, r => sheet.Cell(r, 4).GetText() == "Środowisko");
        Assert.Equal("Zadanie 14\nZadanie 16\nZadanie 17",
            rows.Select(r => sheet.Cell(r, 5).GetText()).Single(t => t.StartsWith("Zadanie 14", StringComparison.Ordinal)));
    }

    [Fact]
    public void September_NoCellHasAFormula()
    {
        using var workbook = XlsxDownload.Open(SummaryXlsx.Write(SeptemberReport()));

        Assert.All(workbook.Worksheet(1).CellsUsed(), cell => Assert.False(cell.HasFormula, cell.Address.ToString()));
    }

    [Fact]
    public void TextLikeFormulaOrNumber_StaysText()
    {
        var report = MonthReport.Build(September,
            [new MonthEntry(1, new DateOnly(2026, 9, 1), 1, "0012", 1, "=1+1", "=SUMA(A1)", 90)]);

        using var workbook = XlsxDownload.Open(SummaryXlsx.Write(report));
        var sheet = workbook.Worksheet(1);

        foreach (var (column, text) in new[] { (3, "0012"), (4, "=1+1"), (5, "=SUMA(A1)") })
        {
            var cell = sheet.Cell(2, column);
            Assert.False(cell.HasFormula);
            Assert.Equal(XLDataType.Text, cell.DataType);
            Assert.Equal(text, cell.GetText());
        }
        Assert.Equal(1.5, sheet.Cell(2, 6).GetDouble());
        Assert.True(sheet.Row(3).IsEmpty());
        Assert.Equal("RAZEM WRZESIEŃ 2026", sheet.Cell(4, 5).GetText());
        Assert.Equal(1.5, sheet.Cell(4, 6).GetDouble());
        Assert.Equal(1.5, sheet.Cell(4, 8).GetDouble());
    }

    [Fact]
    public void ContentType_IsXlsx() =>
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", SummaryXlsx.ContentType);
}
