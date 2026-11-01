using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>
/// The Account state read from the database during a session (AD-17): <c>status</c>, <c>must_change_password</c>
/// and the security stamp. Read in system mode (AD-16), without tracking.
/// </summary>
public sealed record AccountState(AccountStatus Status, bool MustChangePassword, string? SecurityStamp)
{
    /// <summary>The state of the User with the given id, or <c>null</c> when the Account no longer exists.</summary>
    public static Task<AccountState?> FindAsync(AppDbContext db, int userId, CancellationToken cancellationToken = default) =>
        db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new AccountState(u.Status, u.MustChangePassword, u.SecurityStamp))
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>
    /// A session of <paramref name="principal"/> may go on: the Account exists, is <see cref="AccountStatus.Active"/>
    /// and its security stamp equals the one in the login cookie (every change of password, state or roles changes it).
    /// </summary>
    public static bool AllowsSession(AccountState? state, ClaimsPrincipal principal, IdentityOptions identityOptions) =>
        state is { Status: AccountStatus.Active }
        && state.SecurityStamp is not null
        && string.Equals(
            state.SecurityStamp,
            principal.FindFirstValue(identityOptions.ClaimsIdentity.SecurityStampClaimType),
            StringComparison.Ordinal);

    /// <summary>The User id from the <see cref="ClaimTypes.NameIdentifier"/> claim (AD-16), or <c>null</c>.</summary>
    public static int? UserIdOf(ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
        && userId > 0
            ? userId
            : null;
}

/// <summary>
/// The logged-in User's Account is not active (or no longer exists) — thrown in a Blazor circuit by
/// <see cref="CurrentUser"/> (AD-17). <c>Shared/MainLayout</c> navigates to <see cref="LoginUrl"/>.
/// </summary>
public sealed class AccountInactiveException(AccountStatus? status)
    : InvalidOperationException("Konto zalogowanego Usera nie jest aktywne.")
{
    /// <summary>The current state; <c>null</c> when the Account was deleted.</summary>
    public AccountStatus? Status { get; } = status;

    /// <summary>The login page with the <c>powod</c> message of the state (<c>pending</c> or <c>blocked</c>).</summary>
    public string LoginUrl => Status is AccountStatus.Pending or AccountStatus.Blocked
        ? $"{AccountEndpoints.LoginPath}?{Login.ReasonParameter}={Status.Value.ToString().ToLowerInvariant()}"
        : AccountEndpoints.LoginPath;
}

/// <summary>
/// The logged-in User must change the password first (<c>must_change_password</c>) — thrown in a Blazor circuit by
/// <see cref="CurrentUser"/> (AD-17). <c>Shared/MainLayout</c> navigates to <see cref="AccountEndpoints.ChangePasswordPath"/>.
/// </summary>
public sealed class PasswordChangeRequiredException()
    : InvalidOperationException("Zalogowany User musi najpierw zmienić hasło.");
