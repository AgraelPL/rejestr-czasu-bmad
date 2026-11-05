using Bunit;
using ClosedXML.Excel;
using Microsoft.JSInterop;
using RejestrCzasu.Features.Wpisy;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Tests.Features;

/// <summary>The xlsx writer of the lists of Entries (EntriesXlsx), read back with ClosedXML: one test per I/O matrix row.</summary>
public sealed class EntriesXlsxTests
{
    private static readonly IReadOnlyList<ReportColumn> AllColumns = EntriesReport.Report.Columns;

    [Fact]
    public void Write_FileType_SheetAndHeaderWithGodziny()
    {
        var file = EntriesXlsx.Write(AllColumns, [Row(new DateOnly(2026, 11, 3), "Opis", 90)]);

        Assert.Equal("xlsx", file.Extension);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", file.ContentType);
        using var workbook = XlsxDownload.Open(file.Content);
        var sheet = Assert.Single(workbook.Worksheets);
        Assert.Equal("Wpisy", sheet.Name);
        Assert.Equal(["Data", "Dzień", "Projekt", "Typ pracy", "Opis", "Godziny"], Header(sheet));
        Assert.True(sheet.Cell(1, 1).Style.Font.Bold);
    }

    [Fact]
    public void Write_DataIsDateCell_GodzinyIsNumber_RestText()
    {
        var file = EntriesXlsx.Write(AllColumns, [Row(new DateOnly(2026, 11, 3), "Opis", 90), Row(new DateOnly(2026, 11, 4), "Drugi", 60)]);

        using var workbook = XlsxDownload.Open(file.Content);
        var sheet = workbook.Worksheet("Wpisy");
        var date = sheet.Cell(2, 1);
        Assert.Equal(XLDataType.DateTime, date.DataType);
        Assert.Equal(new DateTime(2026, 11, 3), date.GetDateTime());
        Assert.Equal("dd.mm.yyyy", date.Style.NumberFormat.Format);
        Assert.Equal(XLDataType.Number, sheet.Cell(2, 6).DataType);
        Assert.Equal(1.5, sheet.Cell(2, 6).GetDouble());
        Assert.Equal(1.0, sheet.Cell(3, 6).GetDouble());
        Assert.Equal(0, sheet.Cell(3, 6).Style.NumberFormat.NumberFormatId);
        Assert.Equal("", sheet.Cell(3, 6).Style.NumberFormat.Format);
        foreach (var column in new[] { 2, 3, 4, 5 })
        {
            Assert.Equal(XLDataType.Text, sheet.Cell(2, column).DataType);
        }
        Assert.Equal(["Wtorek", "RDZ", "Development", "Opis"], [.. new[] { 2, 3, 4, 5 }.Select(c => sheet.Cell(2, c).GetText())]);
    }

    [Fact]
    public void Write_PolishLetters_Unchanged()
    {
        var file = EntriesXlsx.Write(AllColumns, [Row(new DateOnly(2026, 11, 3), "Zażółć gęślą jaźń", 90, project: "ŁÓD")]);

        using var workbook = XlsxDownload.Open(file.Content);
        var sheet = workbook.Worksheet("Wpisy");
        Assert.Equal("Zażółć gęślą jaźń", sheet.Cell(2, 5).GetText());
        Assert.Equal("ŁÓD", sheet.Cell(2, 3).GetText());
    }

    [Theory]
    [InlineData("=SUMA(A1)")]
    [InlineData("0012")]
    [InlineData("1,5")]
    [InlineData("03.11.2026")]
    public void Write_TextLikeFormulaOrNumber_StaysLiteralText(string description)
    {
        var file = EntriesXlsx.Write(AllColumns, [Row(new DateOnly(2026, 11, 3), description, 90)]);

        using var workbook = XlsxDownload.Open(file.Content);
        var cell = workbook.Worksheet("Wpisy").Cell(2, 5);
        Assert.False(cell.HasFormula);
        Assert.Equal(XLDataType.Text, cell.DataType);
        Assert.Equal(description, cell.GetText());
    }

    [Fact]
    public void Write_ColumnsInGivenOrder_HiddenColumnLeftOut()
    {
        // The screen's order with "Dzień" hidden and "Opis" moved before "Projekt".
        var columns = new[] { "Date", "Description", "ProjectCode", "WorkTypeName", "Minutes" }
            .Select(k => AllColumns.Single(c => c.Key == k)).ToList();

        var file = EntriesXlsx.Write(columns, [Row(new DateOnly(2026, 11, 3), "Opis", 45)]);

        using var workbook = XlsxDownload.Open(file.Content);
        var sheet = workbook.Worksheet("Wpisy");
        Assert.Equal(["Data", "Opis", "Projekt", "Typ pracy", "Godziny"], Header(sheet));
        Assert.Equal("Opis", sheet.Cell(2, 2).GetText());
        Assert.Equal(0.75, sheet.Cell(2, 5).GetDouble());
        Assert.True(sheet.Cell(1, 6).IsEmpty());
    }

    [Fact]
    public void Write_EmptyList_HeaderOnly()
    {
        var file = EntriesXlsx.Write(AllColumns, []);

        using var workbook = XlsxDownload.Open(file.Content);
        var sheet = workbook.Worksheet("Wpisy");
        Assert.Equal(6, Header(sheet).Count);
        Assert.Equal(1, sheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void Write_SumRowUnderEntries_NumberOfGodziny()
    {
        var file = EntriesXlsx.Write(AllColumns, [Row(new DateOnly(2026, 11, 3), "Opis", 90), Row(new DateOnly(2026, 11, 4), "Drugi", 60)]);

        using var workbook = XlsxDownload.Open(file.Content);
        var sheet = workbook.Worksheet("Wpisy");
        Assert.Equal(4, sheet.LastRowUsed()!.RowNumber());
        Assert.Equal("Suma", sheet.Cell(4, 1).GetText());
        Assert.True(sheet.Cell(4, 1).Style.Font.Bold);
        // A stored number, not a formula: Excel's Protected View shows it without calculating.
        Assert.False(sheet.Cell(4, 6).HasFormula);
        Assert.Equal(XLDataType.Number, sheet.Cell(4, 6).DataType);
        Assert.Equal(2.5, sheet.Cell(4, 6).GetDouble());
        Assert.True(sheet.Cell(4, 6).Style.Font.Bold);
    }

    [Fact]
    public void Write_GodzinyHidden_NoSumRow()
    {
        var file = EntriesXlsx.Write([.. AllColumns.Where(c => c.Key != nameof(EntryRow.Minutes))], [Row(new DateOnly(2026, 11, 3), "Opis", 90)]);

        using var workbook = XlsxDownload.Open(file.Content);
        Assert.Equal(2, workbook.Worksheet("Wpisy").LastRowUsed()!.RowNumber());
    }

    private static List<string> Header(IXLWorksheet sheet) =>
        [.. sheet.Row(1).CellsUsed().Select(c => c.GetText())];

    private static IReadOnlyDictionary<string, object?> Row(DateOnly date, string description, int minutes, string project = "RDZ") =>
        EntriesReport.ToRow(new EntryRow(1, date, 1, project, 1, "Development", description, minutes));
}

/// <summary>Opens the xlsx of an export: the bytes of a file or the stream of an <c>EodUI.downloadStream</c> call.</summary>
internal static class XlsxDownload
{
    public static XLWorkbook Open(byte[] content) => new(new MemoryStream(content));

    /// <summary>The workbook passed to <c>EodUI.downloadStream</c> (arguments: file name, stream, MIME type).</summary>
    public static XLWorkbook Open(JSRuntimeInvocation download)
    {
        var stream = Assert.IsType<DotNetStreamReference>(download.Arguments[1]);
        // EodReport disposes the stream after the call; a MemoryStream still gives its bytes.
        return Open(Assert.IsType<MemoryStream>(stream.Stream).ToArray());
    }
}
