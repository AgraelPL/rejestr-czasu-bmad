using Microsoft.AspNetCore.Identity;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>Result of a login attempt on <c>/konto/logowanie</c> (AD-15).</summary>
public enum LoginOutcome
{
    /// <summary>Signed in; <c>last_login_at</c> saved.</summary>
    Success,

    /// <summary>Unknown login or wrong password, always the same message.</summary>
    InvalidCredentials,

    /// <summary>Too many failed attempts: login suspended for the lockout time.</summary>
    LockedOut,

    /// <summary>Correct password, the Account waits for approval.</summary>
    Pending,

    /// <summary>Correct password, the Account is blocked.</summary>
    Blocked,
}

/// <summary>
/// Registration and login for the static <c>/konto</c> pages (AD-15), in the request scope and system mode (AD-16).
/// Only an HTTP response can set the login cookie, so this never runs in a Blazor circuit.
/// </summary>
public sealed class AccountSignIn(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    AppDbContext db,
    TimeProvider timeProvider)
{
    /// <summary>
    /// Creates a <see cref="AccountStatus.Pending"/> Account with the <see cref="AppRoles.User"/> role, all or nothing.
    /// No <c>account_settings</c> row (AD-7). Errors come from the Identity configuration, in Polish.
    /// </summary>
    public async Task<IdentityResult> RegisterAsync(string userName, string password)
    {
        var user = new AppUser
        {
            UserName = userName,
            Status = AccountStatus.Pending,
            RegisteredAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        // UserManager saves through the same scoped AppDbContext, so both steps share this transaction.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            result = await userManager.AddToRoleAsync(user, AppRoles.User);
        }
        if (result.Succeeded)
        {
            await transaction.CommitAsync();
        }
        return result;
    }

    /// <summary>
    /// Password first, then the Account state (<see cref="AppSignInManager"/>); a wrong password counts towards the
    /// lockout in every state. On success the login cookie is set and <c>last_login_at</c> is saved (UTC).
    /// </summary>
    public async Task<LoginOutcome> LoginAsync(string userName, string password)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            // Same work as for an existing login, so the response time does not reveal which logins exist.
            userManager.PasswordHasher.HashPassword(new AppUser(), password);
            return LoginOutcome.InvalidCredentials;
        }

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            user.LastLoginAt = timeProvider.GetUtcNow().UtcDateTime;
            await userManager.UpdateAsync(user);
            return LoginOutcome.Success;
        }
        if (result.IsLockedOut)
        {
            return LoginOutcome.LockedOut;
        }
        if (result.IsNotAllowed)
        {
            return user.Status == AccountStatus.Blocked ? LoginOutcome.Blocked : LoginOutcome.Pending;
        }
        return LoginOutcome.InvalidCredentials;
    }
}
