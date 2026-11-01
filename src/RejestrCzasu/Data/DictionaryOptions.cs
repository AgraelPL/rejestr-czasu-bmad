using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace RejestrCzasu.Data;

/// <summary>An option of the Project list of the Entry form.</summary>
public sealed record ProjectOption(int Id, string Code, string Name, bool IsActive);

/// <summary>An option of the Work type list of the Entry form.</summary>
public sealed record WorkTypeOption(int Id, string Name, bool IsActive);

/// <summary>
/// The only source of Project and Work type options for the Entry form (R2, AD-10): the logged-in User's active
/// items plus, when editing, the Entry's current item even if inactive. User mode only (AD-16), one context per call
/// (AD-4). Sorting is done in C# with the <c>pl-PL</c> culture, so it does not depend on the machine or the database.
/// </summary>
public sealed class DictionaryOptions(UserDbContextFactory contextFactory)
{
    private static readonly StringComparer PolishComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("pl-PL"), ignoreCase: true);

    /// <summary>
    /// Active Projects of the logged-in User, sorted by code, plus the Project <paramref name="currentProjectId"/>
    /// when it is the User's (also inactive). <c>null</c> means a new Entry or a copy (R7).
    /// </summary>
    public async Task<IReadOnlyList<ProjectOption>> ProjectsAsync(int? currentProjectId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var options = await db.Projects
            .AsNoTracking()
            .Where(p => p.IsActive || p.Id == currentProjectId)
            .Select(p => new ProjectOption(p.Id, p.Code, p.Name, p.IsActive))
            .ToListAsync(cancellationToken);
        return options
            .OrderBy(p => p.Code, PolishComparer)
            .ThenBy(p => p.Id)
            .ToList();
    }

    /// <summary>
    /// Active Work types of the logged-in User, sorted by name, plus the Work type <paramref name="currentWorkTypeId"/>
    /// when it is the User's (also inactive). <c>null</c> means a new Entry or a copy (R7).
    /// </summary>
    public async Task<IReadOnlyList<WorkTypeOption>> WorkTypesAsync(int? currentWorkTypeId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var options = await db.WorkTypes
            .AsNoTracking()
            .Where(w => w.IsActive || w.Id == currentWorkTypeId)
            .Select(w => new WorkTypeOption(w.Id, w.Name, w.IsActive))
            .ToListAsync(cancellationToken);
        return options
            .OrderBy(w => w.Name, PolishComparer)
            .ThenBy(w => w.Id)
            .ToList();
    }
}
