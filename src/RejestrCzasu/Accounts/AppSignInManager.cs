using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>
/// Sign-in order from AD-15: lockout, then the password (a wrong one counts as a failed attempt in every Account
/// state), and only after a correct password the Account state (<see cref="CanSignInAsync"/>: only
/// <see cref="AccountStatus.Active"/>). So the state is never revealed without the password.
/// </summary>
public sealed class AppSignInManager(
    UserManager<AppUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<AppUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<AppUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<AppUser> confirmation)
    : SignInManager<AppUser>(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
{
    /// <summary>Only an Active Account may sign in (AD-15); the base adds the Identity checks.</summary>
    public override async Task<bool> CanSignInAsync(AppUser user) =>
        user.Status == AccountStatus.Active && await base.CanSignInAsync(user);

    /// <summary>The password is checked first; the state check moves after it (<see cref="CheckPasswordSignInAsync"/>).</summary>
    public override async Task<SignInResult> CheckPasswordSignInAsync(AppUser user, string password, bool lockoutOnFailure)
    {
        var result = await base.CheckPasswordSignInAsync(user, password, lockoutOnFailure);
        return result.Succeeded && !await CanSignInAsync(user) ? SignInResult.NotAllowed : result;
    }

    /// <summary>
    /// Before the password only the lockout is checked; the base would check <see cref="CanSignInAsync"/> here and
    /// reveal the Account state (and skip counting the failed attempt) for a wrong password.
    /// </summary>
    protected override async Task<SignInResult?> PreSignInCheck(AppUser user) =>
        await IsLockedOut(user) ? await LockedOut(user) : null;
}
