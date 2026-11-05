using ClosedXML.Excel;
using RejestrCzasu.Domain;
using RejestrCzasu.Ui;

namespace RejestrCzasu.Features.Wpisy;

/// <summary>
/// The xlsx writer of the export of the lists of Entries (AD-9, AD-23 poz. 14): <c>EodReport</c> gives the visible
/// columns in the screen's order and every row after the filters; the file has one sheet "Wpisy" with a bold header
/// row. Data is a date cell (dd.mm.yyyy), "Godziny" a number of decimal hours (it sums in Excel), every other column
/// literal text: never a formula and never converted to a number.
/// </summary>
internal static class EntriesXlsx
{
    /// <summary>The MIME type of an xlsx workbook.</summary>
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>The name of the only sheet.</summary>
    public const string SheetName = "Wpisy";

    private const string DateFormat = "dd.mm.yyyy";

    // Fixed widths (characters) instead of AdjustToContents: the server image has no fonts to measure with.
    private static readonly Dictionary<string, double> Widths = new()
    {
        [nameof(EntryRow.Date)] = 12,
        [nameof(EntryRow.DayName)] = 14,
        [nameof(EntryRow.ProjectCode)] = 36,
        [nameof(EntryRow.WorkTypeName)] = 28,
        [nameof(EntryRow.Description)] = 60,
        [nameof(EntryRow.Minutes)] = 10,
    };

    private const double DefaultWidth = 20;

    /// <summary>Writes the workbook: a header (<c>ExportLabel ?? Label</c>) and one row per report row.</summary>
    /// <param name="columns">The visible columns of the report, in the screen's order.</param>
    /// <param name="rows">The report rows after the search and the filters, from all pages, in the screen's order.</param>
    public static ReportFile Write(IReadOnlyList<ReportColumn> columns, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        for (var c = 0; c < columns.Count; c++)
        {
            var column = columns[c];
            var header = sheet.Cell(1, c + 1);
            header.Value = column.ExportLabel ?? column.Label;
            header.Style.Font.Bold = true;
            sheet.Column(c + 1).Width = Widths.GetValueOrDefault(column.Key, DefaultWidth);
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Count; c++)
            {
                var column = columns[c];
                rows[r].TryGetValue(column.Key, out var value);
                SetCell(sheet.Cell(r + 2, c + 1), column, value);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new ReportFile("xlsx", ContentType, stream.ToArray());
    }

    private static void SetCell(IXLCell cell, ReportColumn column, object? value)
    {
        switch (column.Key, value)
        {
            case (nameof(EntryRow.Date), DateOnly date):
                cell.Value = date.ToDateTime(TimeOnly.MinValue);
                cell.Style.NumberFormat.Format = DateFormat;
                break;
            case (nameof(EntryRow.Minutes), int minutes):
                // General format: 90 → 1.5, 60 → 1 (Excel shows the decimal comma of the User's locale).
                cell.Value = (double)Czas.ToDecimalHours(minutes);
                break;
            default:
                // A string value is a text cell as is: "=SUMA(A1)", "0012" or "1,5" stay text.
                cell.Value = value is null ? "" : column.Format?.Invoke(value) ?? value.ToString() ?? "";
                break;
        }
    }
}
