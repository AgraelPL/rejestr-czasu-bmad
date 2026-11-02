using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;
using RejestrCzasu.Domain;

namespace RejestrCzasu.Features.Wpisy;

/// <summary>
/// Screen filters of the Lista wpisów (R8): a <c>null</c> date is no bound on that side (bounds inclusive), a <c>null</c>
/// Project or Work type means all of them.
/// </summary>
public sealed record EntryFilter(DateOnly? From, DateOnly? To, int? ProjectId, int? WorkTypeId);

/// <summary>A row of the Lista wpisów; the ids are kept for the row actions (story 2.3).</summary>
public sealed record EntryRow(
    int Id, DateOnly Date, int ProjectId, string ProjectCode, int WorkTypeId, string WorkTypeName, string Description, int Minutes)
{
    /// <summary>Text of the Data column (<c>dd.MM.yyyy</c>).</summary>
    public string DateText => EntryMessages.Date(Date);

    /// <summary>Text of the Dzień column.</summary>
    public string DayName => PolishCalendar.DayName(Date);

    /// <summary>Text of the Czas column, like Jira.</summary>
    public string CzasText => Czas.Format(Minutes);
}

/// <summary>Outcome of a change made by <see cref="EntryStore"/>; unexpected errors are thrown, not returned.</summary>
public abstract record EntryChange
{
    private EntryChange()
    {
    }

    /// <summary>The Entry was saved with id <paramref name="Id"/> and <paramref name="Minutes"/> of Czas.</summary>
    public sealed record Done(int Id, int Minutes) : EntryChange;

    /// <summary>Nothing saved: the Project or the Work type no longer exists (e.g. deleted in another window).</summary>
    public sealed record MissingDictionaryItem : EntryChange;

    /// <summary>Nothing changed: the Entry no longer exists or is not the logged-in User's.</summary>
    public sealed record NotFound : EntryChange;
}

/// <summary>
/// Data of the Entry screens (R4–R6, R8): adding, listing, finding, editing and deleting Entries. User mode only (AD-16): <c>user_id</c> is assigned by
/// <see cref="AppDbContext"/>, never here; one context per operation (AD-4). A Dictionary item missing in the meantime
/// is recognized only from the foreign key through <see cref="DbErrors"/> (AD-7).
/// </summary>
public sealed class EntryStore(UserDbContextFactory contextFactory)
{
    /// <summary>
    /// Entries of the logged-in User matching <paramref name="filter"/> (filtered in the database), newest date first,
    /// within a day the latest saved (higher Id) first. Materialized in one context (AD-4).
    /// </summary>
    public async Task<IReadOnlyList<EntryRow>> ListAsync(EntryFilter filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.TimeEntries.AsNoTracking();
        if (filter.From is { } from)
        {
            query = query.Where(e => e.Date >= from);
        }
        if (filter.To is { } to)
        {
            query = query.Where(e => e.Date <= to);
        }
        if (filter.ProjectId is { } projectId)
        {
            query = query.Where(e => e.ProjectId == projectId);
        }
        if (filter.WorkTypeId is { } workTypeId)
        {
            query = query.Where(e => e.WorkTypeId == workTypeId);
        }
        return await ToRows(query
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>The Entry <paramref name="id"/> of the logged-in User, or <c>null</c> (also for another User's Entry).</summary>
    public async Task<EntryRow?> FindAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await ToRows(db.TimeEntries.AsNoTracking().Where(e => e.Id == id)).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Adds an Entry of the logged-in User. The description is trimmed; Czas is parsed by <see cref="Czas"/> and must
    /// be a valid Entry Czas (the form's validator has already checked it).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="czas"/> is not a valid Czas of an Entry.</exception>
    public async Task<EntryChange> AddAsync(
        DateOnly date, int projectId, int workTypeId, string description, string czas, CancellationToken cancellationToken = default)
    {
        var minutes = ParseCzas(czas);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = new TimeEntry
        {
            Date = date,
            ProjectId = projectId,
            WorkTypeId = workTypeId,
            Description = description.Trim(),
            Minutes = minutes,
        };
        db.TimeEntries.Add(entry);
        return await SaveAsync(db, entry, cancellationToken);
    }

    /// <summary>
    /// Changes every field of the Entry <paramref name="id"/> of the logged-in User (R6), also in past Months. Same
    /// rules as <see cref="AddAsync"/>; another User's Entry or one deleted in the meantime gives
    /// <see cref="EntryChange.NotFound"/>.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="czas"/> is not a valid Czas of an Entry.</exception>
    public async Task<EntryChange> UpdateAsync(
        int id, DateOnly date, int projectId, int workTypeId, string description, string czas, CancellationToken cancellationToken = default)
    {
        var minutes = ParseCzas(czas);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.TimeEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return new EntryChange.NotFound();
        }
        entry.Date = date;
        entry.ProjectId = projectId;
        entry.WorkTypeId = workTypeId;
        entry.Description = description.Trim();
        entry.Minutes = minutes;
        return await SaveAsync(db, entry, cancellationToken);
    }

    /// <summary>Deletes the Entry <paramref name="id"/> of the logged-in User (R6, AD-10 <c>DeleteEntry</c>).</summary>
    public async Task<EntryChange> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entry = await db.TimeEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (entry is null)
        {
            return new EntryChange.NotFound();
        }
        db.TimeEntries.Remove(entry);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new EntryChange.Done(entry.Id, entry.Minutes);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Deleted in another window between the read and the delete.
            return new EntryChange.NotFound();
        }
    }

    private static IQueryable<EntryRow> ToRows(IQueryable<TimeEntry> query) =>
        query.Select(e => new EntryRow(
            e.Id, e.Date, e.ProjectId, e.Project.Code, e.WorkTypeId, e.WorkType.Name, e.Description, e.Minutes));

    private static int ParseCzas(string czas) =>
        Czas.TryParse(czas, out var minutes) && Czas.IsValidEntry(minutes)
            ? minutes
            : throw new ArgumentException($"Nieprawidłowy Czas Wpisu: {czas}.", nameof(czas));

    private static async Task<EntryChange> SaveAsync(AppDbContext db, TimeEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new EntryChange.Done(entry.Id, entry.Minutes);
        }
        catch (DbUpdateException ex) when (DbErrors.IsForeignKeyViolation(ex))
        {
            // The composite foreign keys (user_id, project_id) and (user_id, work_type_id) also reject another User's item.
            return new EntryChange.MissingDictionaryItem();
        }
        catch (DbUpdateConcurrencyException)
        {
            // An edited Entry deleted in another window between the read and the save.
            return new EntryChange.NotFound();
        }
    }
}
