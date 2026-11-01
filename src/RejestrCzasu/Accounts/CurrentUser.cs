using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.EntityFrameworkCore;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>
/// The only way to find out who is logged in (AD-16). One source per host: in a Blazor circuit the
/// <see cref="AuthenticationStateProvider"/>, in an HTTP request (static pages, endpoints) <c>HttpContext.User</c>.
/// The User id always comes from the <see cref="ClaimTypes.NameIdentifier"/> claim, never from the login.
/// </summary>
public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor,
    AuthenticationStateProvider authenticationStateProvider,
    IDbContextFactory<AppDbContext> contextFactory)
{
    /// <summary>Set by <see cref="CurrentUserCircuitHandler"/> when this scope belongs to a Blazor circuit.</summary>
    internal bool InCircuit { get; set; }

    /// <summary>The logged-in User's id, or <c>null</c> when nobody is logged in.</summary>
    public async Task<int?> FindUserIdAsync()
    {
        var principal = await GetPrincipalAsync();
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        return AccountState.UserIdOf(principal);
    }

    /// <summary>The logged-in User's id; throws <see cref="NoCurrentUserException"/> when nobody is logged in.</summary>
    public async Task<int> GetUserIdAsync() =>
        await FindUserIdAsync() ?? throw new NoCurrentUserException();

    /// <summary>
    /// The logged-in User's id for a User-mode context (<see cref="UserDbContextFactory"/>). In a Blazor circuit the
    /// Account state is read from the database every time (AD-17): an Account that is not active or was deleted throws
    /// <see cref="AccountInactiveException"/>, a required password change <see cref="PasswordChangeRequiredException"/>.
    /// In an HTTP request the login cookie validator has already checked the state on this request.
    /// </summary>
    public async Task<int> GetActiveUserIdAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdAsync();
        if (!InCircuit)
        {
            return userId;
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var state = await AccountState.FindAsync(db, userId, cancellationToken);
        if (state is not { Status: AccountStatus.Active })
        {
            throw new AccountInactiveException(state?.Status);
        }
        if (state.MustChangePassword)
        {
            throw new PasswordChangeRequiredException();
        }
        return userId;
    }

    private async Task<ClaimsPrincipal?> GetPrincipalAsync()
    {
        if (InCircuit)
        {
            return (await authenticationStateProvider.GetAuthenticationStateAsync()).User;
        }
        return httpContextAccessor.HttpContext?.User;
    }
}

/// <summary>No logged-in User where one is required (AD-16).</summary>
public sealed class NoCurrentUserException() : InvalidOperationException("Brak zalogowanego Usera.");

/// <summary>
/// Marks the scope of a Blazor circuit for <see cref="CurrentUser"/>. Circuit handlers are created with the circuit,
/// before any component is rendered, so the flag is set in the constructor.
/// </summary>
internal sealed class CurrentUserCircuitHandler : CircuitHandler
{
    public CurrentUserCircuitHandler(CurrentUser currentUser) => currentUser.InCircuit = true;
}
