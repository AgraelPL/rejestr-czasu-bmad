using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;

namespace RejestrCzasu.Features.UstawieniaKonta;

/// <summary>
/// Data of the Ustawienia konta (R7, R25): the prefix of the Zestawienie file name of the logged-in User. User mode only
/// (AD-16), one context per operation (AD-4). The <c>account_settings</c> row is created on the first save, never at
/// registration or on reading (AD-7); without a row the prefix is empty.
/// </summary>
public sealed class AccountSettingsStore(UserDbContextFactory contextFactory)
{
    /// <summary>The saved prefix, or <c>""</c> when the User has never saved the Ustawienia konta.</summary>
    public async Task<string> GetPrefixAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var prefix = await db.AccountSettings
            .AsNoTracking()
            .Select(s => s.ExportPrefix)
            .FirstOrDefaultAsync(cancellationToken);
        return prefix ?? "";
    }

    /// <summary>
    /// Saves the prefix trimmed (<c>null</c> or only spaces as <c>""</c>); creates the row on the first save. Validation
    /// is the form's job (AD-8). Two first saves at once (two tabs) collide on the primary key: the second is retried
    /// once as an update.
    /// </summary>
    public async Task SavePrefixAsync(string? prefix, CancellationToken cancellationToken = default)
    {
        var value = (prefix ?? "").Trim();
        try
        {
            await SaveOnceAsync(value, cancellationToken);
        }
        catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex, DbErrors.AccountSettingsKey))
        {
            // The row was created in the meantime: the retry finds it and updates it.
            await SaveOnceAsync(value, cancellationToken);
        }
    }

    private async Task SaveOnceAsync(string value, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var settings = await db.AccountSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            // UserId is set by AppDbContext from the bound User (AD-16).
            db.AccountSettings.Add(new AccountSettings { ExportPrefix = value });
        }
        else
        {
            settings.ExportPrefix = value;
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
