using System.Globalization;
using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;

namespace RejestrCzasu.Features.TypyPracy;

/// <summary>A row of the Typy pracy screen.</summary>
public sealed record WorkTypeRow(int Id, string Name, bool IsActive)
{
    /// <summary>Text of the Stan column.</summary>
    public string State => IsActive ? "Aktywny" : "Nieaktywny";
}

/// <summary>Outcome of a change made by <see cref="WorkTypeStore"/>; unexpected errors are thrown, not returned.</summary>
public abstract record WorkTypeChange
{
    private WorkTypeChange()
    {
    }

    /// <summary>The change was saved; <paramref name="Name"/> is the Work type name as stored.</summary>
    public sealed record Done(string Name) : WorkTypeChange;

    /// <summary>Nothing saved: the User already has a Work type with this name (case-insensitively).</summary>
    public sealed record DuplicateName(string Name) : WorkTypeChange;

    /// <summary>Nothing deleted: the Work type is used by <paramref name="EntryCount"/> Entries (R3).</summary>
    public sealed record InUse(string Name, int EntryCount) : WorkTypeChange;

    /// <summary>Nothing saved: the Work type no longer exists (e.g. deleted in another window).</summary>
    public sealed record NotFound : WorkTypeChange;
}

/// <summary>
/// Data of the Typy pracy screen (R1–R3): list, add, edit, deactivate/restore and delete without Entries. User mode
/// only (AD-16), one context per operation with materialized results (AD-4). A duplicate name is recognized only from
/// the unique functional index through <see cref="DbErrors"/> (AD-7), never by a query before saving.
/// </summary>
public sealed class WorkTypeStore(UserDbContextFactory contextFactory)
{
    private static readonly StringComparer PolishComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("pl-PL"), ignoreCase: true);

    /// <summary>All Work types of the logged-in User, active and inactive, sorted by name (<c>pl-PL</c>), then by Id.</summary>
    public async Task<IReadOnlyList<WorkTypeRow>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await db.WorkTypes
            .AsNoTracking()
            .Select(w => new WorkTypeRow(w.Id, w.Name, w.IsActive))
            .ToListAsync(cancellationToken);
        return rows
            .OrderBy(w => w.Name, PolishComparer)
            .ThenBy(w => w.Id)
            .ToList();
    }

    /// <summary>Adds an active Work type; the name is trimmed and stored as typed (letter case kept).</summary>
    public async Task<WorkTypeChange> AddAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var workType = new WorkType { Name = name.Trim() };
        db.WorkTypes.Add(workType);
        return await SaveAsync(db, workType, cancellationToken);
    }

    /// <summary>Changes the name of the Work type; its Entries keep pointing to it.</summary>
    public async Task<WorkTypeChange> UpdateAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var workType = await db.WorkTypes.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (workType is null)
        {
            return new WorkTypeChange.NotFound();
        }
        workType.Name = name.Trim();
        return await SaveAsync(db, workType, cancellationToken);
    }

    /// <summary>Deactivates (<paramref name="isActive"/> false) or restores the Work type.</summary>
    public async Task<WorkTypeChange> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var workType = await db.WorkTypes.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (workType is null)
        {
            return new WorkTypeChange.NotFound();
        }
        workType.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        return new WorkTypeChange.Done(workType.Name);
    }

    /// <summary>
    /// Deletes the Work type only when no Entry uses it (R3). The Entry count comes from a query before the attempt; a
    /// foreign key violation on the delete itself (an Entry added in the meantime) gives the count queried again.
    /// </summary>
    public async Task<WorkTypeChange> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        string name;
        await using (var db = await contextFactory.CreateDbContextAsync(cancellationToken))
        {
            var workType = await db.WorkTypes.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
            if (workType is null)
            {
                return new WorkTypeChange.NotFound();
            }
            name = workType.Name;
            var count = await db.TimeEntries.CountAsync(e => e.WorkTypeId == id, cancellationToken);
            if (count > 0)
            {
                return new WorkTypeChange.InUse(name, count);
            }
            db.WorkTypes.Remove(workType);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
                return new WorkTypeChange.Done(name);
            }
            catch (DbUpdateException ex) when (DbErrors.IsForeignKeyViolation(ex))
            {
                // An Entry was added between the count and the delete: count again below, in a fresh context.
            }
        }

        var recounted = await CountEntriesAsync(id, cancellationToken);
        return recounted > 0
            ? new WorkTypeChange.InUse(name, recounted)
            : throw new InvalidOperationException($"Usunięcie Typu pracy {name} naruszyło klucz obcy bez Wpisów.");
    }

    private async Task<int> CountEntriesAsync(int id, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.TimeEntries.CountAsync(e => e.WorkTypeId == id, cancellationToken);
    }

    private static async Task<WorkTypeChange> SaveAsync(AppDbContext db, WorkType workType, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new WorkTypeChange.Done(workType.Name);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbErrors.WorkTypeNameIndex))
        {
            return new WorkTypeChange.DuplicateName(workType.Name);
        }
    }
}
