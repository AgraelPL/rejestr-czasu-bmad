using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>
/// Change of the User's own password, only on the static <c>/konto/zmiana-hasla</c> (AD-15), in the request scope and
/// system mode (AD-16). Two modes: required (<c>must_change_password</c>: only the new password) and voluntary
/// (current password and the new one). Success clears <c>must_change_password</c>, gives a new security stamp and
/// refreshes the login cookie (<see cref="SignInManager{TUser}.RefreshSignInAsync"/>): this session goes on, other
/// sessions of the User end on their next request (<see cref="AccountCookieValidator"/>).
/// </summary>
public sealed class AccountPassword(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
{
    public const string WrongCurrentPasswordMessage = "Obecne hasło jest nieprawidłowe.";
    public const string CurrentPasswordRequiredMessage = "Podaj obecne hasło.";

    /// <summary><c>true</c> when the logged-in User must change the password (required mode).</summary>
    public async Task<bool> IsChangeRequiredAsync(ClaimsPrincipal principal) =>
        (await userManager.GetUserAsync(principal))?.MustChangePassword == true;

    /// <summary>
    /// Changes the logged-in User's password. In the voluntary mode <paramref name="currentPassword"/> is required and
    /// checked; in the required mode it is ignored. Errors come from the Identity configuration, in Polish (AD-8).
    /// </summary>
    public async Task<IdentityResult> ChangeAsync(ClaimsPrincipal principal, string? currentPassword, string newPassword)
    {
        var user = await userManager.GetUserAsync(principal) ?? throw new NoCurrentUserException();

        IdentityResult result;
        if (user.MustChangePassword)
        {
            result = await SetRequiredPasswordAsync(user, newPassword);
        }
        else if (string.IsNullOrEmpty(currentPassword))
        {
            result = IdentityResult.Failed(new IdentityError
            {
                Code = nameof(CurrentPasswordRequiredMessage),
                Description = CurrentPasswordRequiredMessage,
            });
        }
        else
        {
            // Checks the current password, validates the new one, sets a new security stamp; one save.
            result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                result = IdentityResult.Failed(result.Errors
                    .Select(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)
                        ? new IdentityError { Code = e.Code, Description = WrongCurrentPasswordMessage }
                        : e)
                    .ToArray());
            }
        }

        if (result.Succeeded)
        {
            await signInManager.RefreshSignInAsync(user);
        }
        return result;
    }

    /// <summary>
    /// Required mode: the new password replaces the one set by the Administrator without the old one. Validated by the
    /// Identity password validators first, then hash, flag and security stamp are saved together (one update).
    /// </summary>
    private async Task<IdentityResult> SetRequiredPasswordAsync(AppUser user, string newPassword)
    {
        var errors = new List<IdentityError>();
        foreach (var validator in userManager.PasswordValidators)
        {
            errors.AddRange((await validator.ValidateAsync(userManager, user, newPassword)).Errors);
        }
        if (errors.Count > 0)
        {
            return IdentityResult.Failed([.. errors]);
        }

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        // Sets a new stamp and saves the whole user (hash and flag included).
        return await userManager.UpdateSecurityStampAsync(user);
    }
}
