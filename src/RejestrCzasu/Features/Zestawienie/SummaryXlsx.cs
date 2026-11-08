using ClosedXML.Excel;
using RejestrCzasu.Domain;
using RejestrCzasu.Features.Wpisy;
using RejestrCzasu.Reporting;

namespace RejestrCzasu.Features.Zestawienie;

/// <summary>
/// The xlsx writer of the Zestawienie (R4, R11, R12, export-layout.md): one sheet named after the Miesiąc, a frozen bold
/// header, one sheet row per Wiersz zestawienia of <see cref="MonthReport.Rows"/>, one empty row and the bold RAZEM row.
/// Every number (Godziny, Suma dnia, Suma tygodnia, RAZEM) comes from <see cref="MonthReport"/> as a value of decimal
/// hours (AD-5): the writer sums nothing and writes no formula. Texts are text cells as they are.
/// </summary>
internal static class SummaryXlsx
{
    /// <summary>The MIME type of an xlsx workbook, shared with the export of the lists of Wpisy.</summary>
    public const string ContentType = EntriesXlsx.ContentType;

    /// <summary>The header row, columns A–H.</summary>
    public static readonly string[] Headers =
        ["Dzień tygodnia", "Data", "Kod", "Typ", "Opis", "Godziny", "Suma dnia", "Suma tygodnia"];

    private const string DateFormat = "dd.mm.yyyy";

    // Fixed widths (characters) instead of AdjustToContents: the server image has no fonts to measure with.
    private static readonly double[] Widths = [16, 12, 10, 18, 60, 10, 11, 14];

    /// <summary>Writes the Zestawienie of <paramref name="report"/>.</summary>
    public static byte[] Write(MonthReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(report.Month.Name);

        for (var c = 0; c < Headers.Length; c++)
        {
            var header = sheet.Cell(1, c + 1);
            header.Value = Headers[c];
            header.Style.Font.Bold = true;
            sheet.Column(c + 1).Width = Widths[c];
        }
        sheet.SheetView.FreezeRows(1);

        for (var i = 0; i < report.Rows.Count; i++)
        {
            var row = report.Rows[i];
            var r = i + 2;
            // A, B and G only in the first row of a day: only there DayMinutes is set.
            if (row.DayMinutes is { } dayMinutes)
            {
                sheet.Cell(r, 1).Value = PolishCalendar.DayName(row.Date);
                var date = sheet.Cell(r, 2);
                date.Value = row.Date.ToDateTime(TimeOnly.MinValue);
                date.Style.NumberFormat.Format = DateFormat;
                sheet.Cell(r, 7).Value = Hours(dayMinutes);
            }
            sheet.Cell(r, 3).Value = row.ProjectCode;
            sheet.Cell(r, 4).Value = row.WorkTypeName;
            var description = sheet.Cell(r, 5);
            description.Value = string.Join("\n", row.Descriptions);
            description.Style.Alignment.WrapText = true;
            sheet.Cell(r, 6).Value = Hours(row.Minutes);
            if (row.WeekMinutes is { } weekMinutes)
            {
                sheet.Cell(r, 8).Value = Hours(weekMinutes);
            }
        }

        // RAZEM after one empty row; F and H are the sum of the Miesiąc from the report.
        var total = report.Rows.Count + 3;
        sheet.Cell(total, 5).Value = "RAZEM " + report.Month.Name.ToUpperInvariant();
        sheet.Cell(total, 6).Value = Hours(report.Minutes);
        sheet.Cell(total, 8).Value = Hours(report.Minutes);
        sheet.Row(total).Style.Font.Bold = true;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // General format: 90 → 1.5 (Excel shows the decimal comma of the User's locale).
    private static double Hours(int minutes) => (double)Czas.ToDecimalHours(minutes);
}
