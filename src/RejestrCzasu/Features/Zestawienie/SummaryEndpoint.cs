using System.Globalization;
using RejestrCzasu.Domain;
using RejestrCzasu.Reporting;

namespace RejestrCzasu.Features.Zestawienie;

/// <summary>
/// <c>GET /zestawienie/{rok}/{miesiac}</c> (AD-14): the Zestawienie of a Miesiąc of the logged-in User as xlsx. The
/// Wpisy only through <see cref="MonthEntriesQuery"/> (User mode, AD-16), every number from <see cref="MonthReport"/>
/// (AD-5). Opened only by a link with <c>download</c> from the Widok miesiąca, never by the Blazor router.
/// </summary>
public static class SummaryEndpoint
{
    public const string Route = "/zestawienie/{rok:int}/{miesiac:int}";

    /// <summary>The address of the Zestawienie of <paramref name="month"/>, e.g. <c>/zestawienie/2026/9</c>.</summary>
    public static string Path(YearMonth month) =>
        string.Create(CultureInfo.InvariantCulture, $"/zestawienie/{month.Year}/{month.Month}");

    /// <summary>The text of the 404 of a Miesiąc without Wpisy.</summary>
    public static string EmptyMessage(YearMonth month) =>
        $"Brak Wpisów w Miesiącu {month.Name} - Zestawienia nie ma do pobrania.";

    public static IEndpointRouteBuilder MapSummaryEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(Route, async (int rok, int miesiac, MonthEntriesQuery query, CancellationToken cancellationToken) =>
            {
                if (rok is < 1 or > 9999 || miesiac is < 1 or > 12)
                {
                    // Without a body: UseStatusCodePagesWithReExecute shows the "nie znaleziono" page.
                    return Results.NotFound();
                }
                var month = new YearMonth(rok, miesiac);
                var report = MonthReport.Build(month, await query.LoadAsync(month, cancellationToken));
                if (report.Rows.Count == 0)
                {
                    // With a body, so UseStatusCodePagesWithReExecute does not turn it into "nie znaleziono".
                    return Results.Text(EmptyMessage(month), "text/plain; charset=utf-8", statusCode: StatusCodes.Status404NotFound);
                }
                return Results.File(SummaryXlsx.Write(report), SummaryXlsx.ContentType, SummaryFile.Name(month, prefix: null));
            })
            .RequireAuthorization();
        return endpoints;
    }
}
