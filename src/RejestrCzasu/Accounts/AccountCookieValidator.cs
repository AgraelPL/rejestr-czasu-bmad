using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>
/// <see cref="CookieAuthenticationEvents.OnValidatePrincipal"/> of the login cookie, on every HTTP request (AD-17).
/// Reads <c>status</c>, <c>must_change_password</c> and the security stamp from the database (system mode, AD-16);
/// an Account that is not active, was deleted or has another stamp loses the session: the identity is rejected and
/// the cookie deleted. Together with <c>/konto/wyloguj</c> the only place that deletes the login cookie.
/// The state read here is kept for the request (<see cref="StateOf"/>), so the password change redirect
/// (<see cref="PasswordChangeRedirect"/>) does not read it again.
/// </summary>
public static class AccountCookieValidator
{
    private static readonly object StateKey = new();

    public static async Task ValidatePrincipalAsync(CookieValidatePrincipalContext context)
    {
        if (context.Principal is not { } principal)
        {
            return;
        }

        var services = context.HttpContext.RequestServices;
        var identityOptions = services.GetRequiredService<IOptions<IdentityOptions>>().Value;
        var state = AccountState.UserIdOf(principal) is { } userId
            ? await AccountState.FindAsync(services.GetRequiredService<AppDbContext>(), userId, context.HttpContext.RequestAborted)
            : null;

        if (!AccountState.AllowsSession(state, principal, identityOptions))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        context.HttpContext.Items[StateKey] = state;
    }

    /// <summary>The Account state validated on this request, or <c>null</c> without a valid login cookie.</summary>
    public static AccountState? StateOf(HttpContext context) =>
        context.Items.TryGetValue(StateKey, out var state) ? state as AccountState : null;
}
