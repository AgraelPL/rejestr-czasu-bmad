using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Accounts;

namespace RejestrCzasu.Data;

/// <summary>
/// The only source of User-mode contexts (AD-16): screens inject this factory and create one context per operation
/// (AD-4). The User comes from <see cref="CurrentUser"/>; without a logged-in User the factory throws.
/// </summary>
public sealed class UserDbContextFactory(IDbContextFactory<AppDbContext> contextFactory, CurrentUser currentUser)
{
    /// <summary>A context bound to the logged-in User; throws <see cref="NoCurrentUserException"/> without one.</summary>
    public async Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetUserIdAsync();
        return await CreateDbContextAsync(userId, cancellationToken);
    }

    /// <summary>A context bound to the given User. Only for tests: the application never picks the User itself.</summary>
    internal async Task<AppDbContext> CreateDbContextAsync(int userId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.BindUser(userId);
        return db;
    }
}
