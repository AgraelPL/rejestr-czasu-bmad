using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;
using RejestrCzasu.Domain;

namespace RejestrCzasu.Features.Wpisy;

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
}

/// <summary>
/// Data of the Entry screens (R4, R5): adding an Entry. User mode only (AD-16): <c>user_id</c> is assigned by
/// <see cref="AppDbContext"/>, never here; one context per operation (AD-4). A Dictionary item missing in the meantime
/// is recognized only from the foreign key through <see cref="DbErrors"/> (AD-7).
/// </summary>
public sealed class EntryStore(UserDbContextFactory contextFactory)
{
    /// <summary>
    /// Adds an Entry of the logged-in User. The description is trimmed; Czas is parsed by <see cref="Czas"/> and must
    /// be a valid Entry Czas (the form's validator has already checked it).
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="czas"/> is not a valid Czas of an Entry.</exception>
    public async Task<EntryChange> AddAsync(
        DateOnly date, int projectId, int workTypeId, string description, string czas, CancellationToken cancellationToken = default)
    {
        if (!Czas.TryParse(czas, out var minutes) || !Czas.IsValidEntry(minutes))
        {
            throw new ArgumentException($"Nieprawidłowy Czas Wpisu: {czas}.", nameof(czas));
        }

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
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return new EntryChange.Done(entry.Id, minutes);
        }
        catch (DbUpdateException ex) when (DbErrors.IsForeignKeyViolation(ex))
        {
            // The composite foreign keys (user_id, project_id) and (user_id, work_type_id) also reject another User's item.
            return new EntryChange.MissingDictionaryItem();
        }
    }
}
