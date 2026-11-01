using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using RejestrCzasu.Data;
using RejestrCzasu.Options;

namespace RejestrCzasu.Startup;

/// <summary>
/// Step 3 of the startup sequence (AD-21), in system mode (AD-16): when the database has no Account with the
/// <see cref="AppRoles.Administrator"/> role, creates an <see cref="AccountStatus.Active"/> Account with the roles
/// <see cref="AppRoles.Administrator"/> and <see cref="AppRoles.User"/> from <see cref="FirstAdminOptions"/>,
/// without an <c>account_settings</c> row (AD-7). When an Administrator exists, the configuration is ignored.
/// The only path that creates an Administrator outside the Users screen.
/// </summary>
public sealed class FirstAdministrator(
    IOptions<FirstAdminOptions> options,
    UserManager<AppUser> userManager,
    AppDbContext db,
    TimeProvider timeProvider,
    ILogger<FirstAdministrator> logger)
{
    public const string MissingConfigurationMessage =
        "W bazie nie ma Administratora, a nie ustawiono pierwszego Administratora: dodaj `FirstAdmin:UserName` " +
        "i `FirstAdmin:Password` w appsettings.Local.json (na serwerze `FirstAdmin__UserName` i " +
        "`FirstAdmin__Password` w .env).";

    public const string RejectedMessagePrefix = "Nie można utworzyć pierwszego Administratora";

    /// <summary>Creates the first Administrator if needed; throws <see cref="DatabaseStartupException"/> when it cannot.</summary>
    public async Task EnsureAsync(CancellationToken cancellationToken = default)
    {
        if ((await userManager.GetUsersInRoleAsync(AppRoles.Administrator)).Count > 0)
        {
            return;
        }

        var userName = options.Value.UserName?.Trim();
        var password = options.Value.Password;
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            throw new DatabaseStartupException(MissingConfigurationMessage);
        }

        var user = new AppUser
        {
            UserName = userName,
            Status = AccountStatus.Active,
            RegisteredAt = timeProvider.GetUtcNow().UtcDateTime,
        };

        // UserManager saves through the same scoped AppDbContext, so the Account and both roles are all or nothing.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            result = await userManager.AddToRolesAsync(user, [AppRoles.Administrator, AppRoles.User]);
        }
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(e => e.Description));
            throw new DatabaseStartupException(
                $"{RejectedMessagePrefix} „{userName}” z ustawień `FirstAdmin`: {errors}");
        }
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Utworzono pierwszego Administratora {UserName}.", userName);
    }
}
