using System.Collections.Concurrent;

namespace RejestrCzasu.Shared;

/// <summary>
/// The date last set on <c>/wpisy/nowy</c>, per User (AD-10, R7): kept in server memory until logout or a restart of the
/// application. A singleton; the User id always comes from <c>Accounts/CurrentUser</c>. The copy of an Entry takes it,
/// and without it <c>PolishCalendar.Today</c>.
/// </summary>
public sealed class EntryDateMemory
{
    private readonly ConcurrentDictionary<int, DateOnly> _dates = new();

    /// <summary>Remembers <paramref name="date"/> as the User's last date of the Nowy wpis screen.</summary>
    public void Remember(int userId, DateOnly date) => _dates[userId] = date;

    /// <summary>The User's last date of the Nowy wpis screen, or <c>null</c> when the screen was not opened since login.</summary>
    public DateOnly? Find(int userId) => _dates.TryGetValue(userId, out var date) ? date : null;

    /// <summary>Forgets the User's date (logout).</summary>
    public void Forget(int userId) => _dates.TryRemove(userId, out _);
}
