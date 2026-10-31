using Microsoft.EntityFrameworkCore;

namespace RejestrCzasu.Data;

/// <summary>
/// The only source of User-mode contexts (AD-16): screens inject this factory and create one context per operation
/// (AD-4). The User comes from <c>Accounts/CurrentUser</c> (added by a later story); until then the caller passes it.
/// </summary>
public sealed class UserDbContextFactory(IDbContextFactory<AppDbContext> contextFactory)
{
    public async Task<AppDbContext> CreateDbContextAsync(int userId, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.BindUser(userId);
        return db;
    }
}
