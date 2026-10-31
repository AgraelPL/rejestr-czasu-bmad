using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using RejestrCzasu.Data;

namespace RejestrCzasu.Accounts;

/// <summary>The only place that configures Identity, the login cookie and <see cref="CurrentUser"/> (AD-15, AD-16).</summary>
public static class IdentitySetup
{
    public const int MinPasswordLength = 6;

    /// <summary>Length of <c>users.user_name</c> (<c>character varying(256)</c>, Identity default).</summary>
    public const int MaxUserNameLength = 256;

    public const string UserNameTooLongMessage = "Login może mieć najwyżej 256 znaków.";
    public const int MaxFailedAccessAttempts = 5;
    public static readonly TimeSpan LockoutTime = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(14);

    public static IServiceCollection AddAccounts(this IServiceCollection services)
    {
        services.AddCascadingAuthenticationState();
        services.AddAuthorization();
        services.AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddIdentityCookies();

        services.AddIdentityCore<AppUser>(options =>
            {
                // Login: unique regardless of letter case (Identity compares normalized names), default characters.
                options.User.RequireUniqueEmail = false;

                // Password: at least 6 characters, no character classes required.
                options.Password.RequiredLength = MinPasswordLength;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                // Lockout only after failed attempts, never as a block by the Administrator.
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = MaxFailedAccessAttempts;
                options.Lockout.DefaultLockoutTimeSpan = LockoutTime;

                // No e-mail, no confirmation: the Account state is the status column only.
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddRoles<IdentityRole<int>>()
            // System mode: the scoped AppDbContext registered by AddAppDatabase (AD-16).
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager<AppSignInManager>()
            .AddErrorDescriber<PolishIdentityErrorDescriber>();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = AccountEndpoints.LoginPath;
            options.AccessDeniedPath = "/konto/brak-dostepu";
            options.ExpireTimeSpan = CookieLifetime;
            options.SlidingExpiration = true;
        });

        services.AddSingleton<IEmailSender<AppUser>, NoOpEmailSender>();
        services.AddScoped<AccountSignIn>();

        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<CircuitHandler, CurrentUserCircuitHandler>();

        services.AddAccountRateLimit();
        return services;
    }
}
