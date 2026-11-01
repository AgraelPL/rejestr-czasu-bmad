using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;

namespace RejestrCzasu.Features.Projekty;

/// <summary>A row of the Projekty screen.</summary>
public sealed record ProjectRow(int Id, string Code, string Name, bool IsActive)
{
    /// <summary>Text of the Stan column.</summary>
    public string State => IsActive ? "Aktywny" : "Nieaktywny";
}

/// <summary>Outcome of a change made by <see cref="ProjectStore"/>; unexpected errors are thrown, not returned.</summary>
public abstract record ProjectChange
{
    private ProjectChange()
    {
    }

    /// <summary>The change was saved; <paramref name="Code"/> is the Project code as stored (upper-case).</summary>
    public sealed record Done(string Code) : ProjectChange;

    /// <summary>Nothing saved: the User already has a Project with this code (stored upper-case).</summary>
    public sealed record DuplicateCode(string Code) : ProjectChange;

    /// <summary>Nothing deleted: the Project is used by <paramref name="EntryCount"/> Entries (R3).</summary>
    public sealed record InUse(string Code, int EntryCount) : ProjectChange;

    /// <summary>Nothing saved: the Project no longer exists (e.g. deleted in another window).</summary>
    public sealed record NotFound : ProjectChange;
}

/// <summary>
/// Data of the Projekty screen (R1–R3): list, add, edit, deactivate/restore and delete without Entries. User mode only
/// (AD-16), one context per operation with materialized results (AD-4). A duplicate code is recognized only from the
/// unique index through <see cref="DbErrors"/> (AD-7), never by a query before saving.
/// </summary>
public sealed class ProjectStore(UserDbContextFactory contextFactory)
{
    private static readonly StringComparer PolishComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("pl-PL"), ignoreCase: true);

    /// <summary>All Projects of the logged-in User, active and inactive, sorted by code (<c>pl-PL</c>), then by Id.</summary>
    public async Task<IReadOnlyList<ProjectRow>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.Projects
            .AsNoTracking()
            .Select(p => new ProjectRow(p.Id, p.Code, p.Name, p.IsActive))
            .ToListAsync(cancellationToken);
        return rows
            .OrderBy(p => p.Code, PolishComparer)
            .ThenBy(p => p.Id)
            .ToList();
    }

    /// <summary>Adds an active Project; code and name are trimmed, the code is stored upper-case by <see cref="AppDbContext"/>.</summary>
    public async Task<ProjectChange> AddAsync(string code, string name, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var project = new Project { Code = code.Trim(), Name = name.Trim() };
        db.Projects.Add(project);
        return await SaveAsync(db, project, cancellationToken);
    }

    /// <summary>Changes code and name of the Project; its Entries keep pointing to it.</summary>
    public async Task<ProjectChange> UpdateAsync(int id, string code, string name, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return new ProjectChange.NotFound();
        }
        project.Code = code.Trim();
        project.Name = name.Trim();
        return await SaveAsync(db, project, cancellationToken);
    }

    /// <summary>Deactivates (<paramref name="isActive"/> false) or restores the Project.</summary>
    public async Task<ProjectChange> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (project is null)
        {
            return new ProjectChange.NotFound();
        }
        project.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        return new ProjectChange.Done(project.Code);
    }

    /// <summary>
    /// Deletes the Project only when no Entry uses it (R3). The Entry count comes from a query before the attempt; a
    /// foreign key violation on the delete itself (an Entry added in the meantime) gives the count queried again.
    /// </summary>
    public async Task<ProjectChange> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        string code;
        await using (var db = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
            if (project is null)
            {
                return new ProjectChange.NotFound();
            }
            code = project.Code;
            var count = await db.TimeEntries.CountAsync(e => e.ProjectId == id, cancellationToken);
            if (count > 0)
            {
                return new ProjectChange.InUse(code, count);
            }
            db.Projects.Remove(project);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return new ProjectChange.Done(project.Code);
            }
            catch (DbUpdateException ex) when (DbErrors.IsForeignKeyViolation(ex))
            {
                // An Entry was added between the count and the delete: count again below, in a fresh context.
            }
        }

        var recounted = await CountEntriesAsync(id, cancellationToken);
        return recounted > 0
            ? new ProjectChange.InUse(code, recounted)
            : throw new InvalidOperationException($"Usunięcie Projektu {code} naruszyło klucz obcy bez Wpisów.");
    }

    private async Task<int> CountEntriesAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TimeEntries.CountAsync(e => e.ProjectId == id, cancellationToken);
    }

    private static async Task<ProjectChange> SaveAsync(AppDbContext db, Project project, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new ProjectChange.Done(project.Code);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbErrors.ProjectCodeIndex))
        {
            // AppDbContext has already normalized the code to upper case before the failed save.
            return new ProjectChange.DuplicateCode(project.Code);
        }
    }
}
