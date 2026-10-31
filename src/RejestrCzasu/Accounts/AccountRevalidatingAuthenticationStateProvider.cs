using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>
/// The <see cref="AuthenticationStateProvider"/> of Blazor circuits (AD-17): every
/// <see cref="Interval"/> checks the security stamp and <c>status</c> of the logged-in User, so an idle circuit of a
/// blocked or deleted Account, or of a session ended by a password change elsewhere, ends too. A failed check makes
/// the User anonymous in the circuit; the next HTTP request then deletes the cookie (<see cref="AccountCookieValidator"/>).
/// </summary>
public sealed class AccountRevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IDbContextFactory<AppDbContext> contextFactory,
    IOptions<IdentityOptions> identityOptions)
    : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override TimeSpan RevalidationInterval => Interval;

    protected override Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken) =>
        IsValidAsync(authenticationState.User, cancellationToken);

    /// <summary>The check run every <see cref="Interval"/>; internal so tests call it without waiting.</summary>
    internal async Task<bool> IsValidAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        if (AccountState.UserIdOf(principal) is not { } userId)
        {
            return false;
        }
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var state = await AccountState.FindAsync(db, userId, cancellationToken);
        return AccountState.AllowsSession(state, principal, identityOptions.Value);
    }
}
