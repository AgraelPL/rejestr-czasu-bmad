using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using RejestrCzasu.Data;
using RejestrCzasu.Shared;

namespace RejestrCzasu.Accounts;

/// <summary>Account endpoints outside Razor components (AD-15).</summary>
public static class AccountEndpoints
{
    public const string LogoutPath = "/konto/wyloguj";
    public const string LoginPath = "/konto/logowanie";
    public const string ChangePasswordPath = "/konto/zmiana-hasla";

    public const string InvalidLogoutMessage = "Nieprawidłowe żądanie wylogowania. Odśwież stronę i spróbuj ponownie.";

    /// <summary>
    /// <c>POST /konto/wyloguj</c> with an antiforgery token ends the session and returns to the login page.
    /// There is no GET: a link or an image cannot log anybody out. Before signing out it forgets the User's date of the
    /// Nowy wpis screen (<see cref="EntryDateMemory"/>, AD-10).
    /// </summary>
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(LogoutPath, async (HttpContext context, IAntiforgery antiforgery, SignInManager<AppUser> signInManager,
                CurrentUser currentUser, EntryDateMemory entryDateMemory) =>
            {
                // The endpoint binds no form, so the antiforgery middleware would not check it; check here.
                if (!await antiforgery.IsRequestValidAsync(context))
                {
                    // With a body, so UseStatusCodePagesWithReExecute does not turn it into "Nie znaleziono".
                    return Results.Text(InvalidLogoutMessage, "text/plain; charset=utf-8", statusCode: StatusCodes.Status400BadRequest);
                }
                if (await currentUser.FindUserIdAsync() is { } userId)
                {
                    entryDateMemory.Forget(userId);
                }
                await signInManager.SignOutAsync();
                return Results.LocalRedirect(LoginPath);
            })
            .RequireAuthorization()
            .AllowDuringPasswordChange();
        return endpoints;
    }
}
