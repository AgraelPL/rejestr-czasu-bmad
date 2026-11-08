using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;
using RejestrCzasu.Domain;

namespace RejestrCzasu.Reporting;

/// <summary>
/// Reads the Wpisy of a Miesiąc of the logged-in User for <see cref="MonthReport.Build"/> (AD-5). User mode only
/// through <see cref="UserDbContextFactory"/> (AD-16), one context (AD-4). Sums nothing: every sum comes from
/// <see cref="MonthReport"/>.
/// </summary>
public sealed class MonthEntriesQuery(UserDbContextFactory contextFactory)
{
    /// <summary>The Wpisy of <paramref name="month"/> (first to last day, inclusive), materialized, in no particular order.</summary>
    public async Task<IReadOnlyList<MonthEntry>> LoadAsync(YearMonth month, CancellationToken cancellationToken = default)
    {
        var first = month.FirstDay;
        var last = month.LastDay;
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TimeEntries.AsNoTracking()
            .Where(e => e.Date >= first && e.Date <= last)
            .Select(e => new MonthEntry(
                e.Id, e.Date, e.ProjectId, e.Project.Code, e.WorkTypeId, e.WorkType.Name, e.Description, e.Minutes))
            .ToListAsync(cancellationToken);
    }
}
