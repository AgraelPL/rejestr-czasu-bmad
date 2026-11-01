using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Shared;

namespace RejestrCzasu.Tests;

/// <summary>Registration, login and logout (AD-15) and <see cref="CurrentUser"/> (AD-16), through HTTP.</summary>
public sealed class AccountsTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "haslo1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Register_NewLogin_CreatesPendingAccount_WithRoleUser_WithoutSettings()
    {
        await using var app = new AppClient(postgres);
        var before = DateTime.UtcNow;

        using var response = await app.RegisterAsync("nowy.user", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await TextAsync(response);
        Assert.Contains("oczekuje na zatwierdzenie przez Administratora", html);

        await using var db = postgres.CreateContext();
        var user = await NonAdministrators(db).SingleAsync(Ct);
        Assert.Equal("nowy.user", user.UserName);
        Assert.Equal(AccountStatus.Pending, user.Status);
        Assert.InRange(user.RegisteredAt, before.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        Assert.Null(user.LastLoginAt);
        Assert.True(user.LockoutEnabled);
        var roleIds = await db.UserRoles.Where(r => r.UserId == user.Id).Select(r => r.RoleId).ToListAsync(Ct);
        var roles = await db.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToListAsync(Ct);
        Assert.Equal([AppRoles.User], roles);

        // No account_settings row (AD-7); the table is IUserOwned, so it is counted through a User-mode context.
        await using var userDb = await postgres.CreateUserContextAsync(user.Id);
        Assert.Equal(0, await userDb.AccountSettings.CountAsync(Ct));
    }

    [Fact]
    public async Task Register_Form_SaysThatAdministratorSeesAllData()
    {
        await using var app = new AppClient(postgres);

        var (html, _) = await app.GetPageAsync("/konto/rejestracja");

        Assert.Contains(Register.AdministratorAccessNotice, html);
    }

    [Fact]
    public async Task Register_LoginTakenWithOtherLetterCase_IsRejected_InPolish()
    {
        await using var app = new AppClient(postgres);
        await app.CreateAccountAsync("Kowalski", Password, AccountStatus.Active);

        using var response = await app.RegisterAsync("kOWALSKI", Password);

        var html = await TextAsync(response);
        Assert.Contains("Login „kOWALSKI” jest już zajęty.", html);
        await using var db = postgres.CreateContext();
        Assert.Equal(1, await NonAdministrators(db).CountAsync(Ct));
    }

    [Fact]
    public async Task Register_PasswordTooShort_IsRejected_InPolish()
    {
        await using var app = new AppClient(postgres);

        using var response = await app.RegisterAsync("krotkie", "12345");

        var html = await TextAsync(response);
        Assert.Contains("Hasło musi mieć co najmniej 6 znaków.", html);
        await using var db = postgres.CreateContext();
        Assert.Equal(0, await NonAdministrators(db).CountAsync(Ct));
    }

    [Fact]
    public async Task Register_LoginOver256Characters_IsRejected_InPolish_WithoutError()
    {
        await using var app = new AppClient(postgres);
        var (form, _) = await app.GetPageAsync("/konto/rejestracja");
        Assert.Contains("maxlength=\"256\"", form);

        using var response = await app.RegisterAsync(new string('a', IdentitySetup.MaxUserNameLength + 1), Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(IdentitySetup.UserNameTooLongMessage, await TextAsync(response));
        await using var db = postgres.CreateContext();
        Assert.Equal(0, await NonAdministrators(db).CountAsync(Ct));
    }

    [Fact]
    public async Task Login_LoginOver256Characters_IsRejected_WithoutError()
    {
        await using var app = new AppClient(postgres);

        using var response = await app.LoginAsync(new string('a', IdentitySetup.MaxUserNameLength + 1), Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(IdentitySetup.UserNameTooLongMessage, await TextAsync(response));
    }

    [Fact]
    public async Task Register_SixCharacterPassword_WithoutCharacterClasses_IsAccepted()
    {
        await using var app = new AppClient(postgres);

        using var response = await app.RegisterAsync("szesc", "aaaaaa");

        Assert.Contains(Register.RegisteredMessage, await TextAsync(response));
    }

    [Theory]
    [InlineData(AccountStatus.Pending)]
    [InlineData(AccountStatus.Active)]
    [InlineData(AccountStatus.Blocked)]
    public async Task Login_WrongPassword_InAnyState_SaysInvalidCredentials_AndCountsFailure(AccountStatus status)
    {
        await using var app = new AppClient(postgres);
        var userId = await app.CreateAccountAsync("jan", Password, status);

        using var response = await app.LoginAsync("jan", "zle-haslo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await TextAsync(response);
        Assert.Contains(Login.InvalidCredentialsMessage, html);
        Assert.DoesNotContain(Login.PendingMessage, html);
        Assert.DoesNotContain(Login.BlockedMessage, html);
        Assert.False(HasLoginCookie(response));
        await using var db = postgres.CreateContext();
        Assert.Equal(1, await db.Users.Where(u => u.Id == userId).Select(u => u.AccessFailedCount).SingleAsync(Ct));
    }

    [Fact]
    public async Task Login_UnknownLogin_SaysInvalidCredentials()
    {
        await using var app = new AppClient(postgres);

        using var response = await app.LoginAsync("nikt", Password);

        Assert.Contains(Login.InvalidCredentialsMessage, await TextAsync(response));
        Assert.False(HasLoginCookie(response));
    }

    [Theory]
    [InlineData(AccountStatus.Pending, Login.PendingMessage)]
    [InlineData(AccountStatus.Blocked, Login.BlockedMessage)]
    public async Task Login_CorrectPassword_InactiveAccount_ShowsState_WithoutSignIn(AccountStatus status, string message)
    {
        await using var app = new AppClient(postgres);
        var userId = await app.CreateAccountAsync("anna", Password, status);

        using var response = await app.LoginAsync("anna", Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(message, await TextAsync(response));
        Assert.False(HasLoginCookie(response));
        await using var db = postgres.CreateContext();
        Assert.Null(await db.Users.Where(u => u.Id == userId).Select(u => u.LastLoginAt).SingleAsync(Ct));

        using var home = await app.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
    }

    [Fact]
    public async Task Login_ActiveAccount_SignsIn_SavesLastLogin_AndRedirectsToLocalReturnUrl()
    {
        await using var app = new AppClient(postgres);
        var userId = await app.CreateAccountAsync("ewa", Password, AccountStatus.Active);
        var before = DateTime.UtcNow;

        using var response = await app.LoginAsync("EWA", Password, "/konto/logowanie?ReturnUrl=%2Fnot-found%3Fx%3D1");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/not-found?x=1", response.Headers.Location?.OriginalString.Replace("http://localhost", ""));
        Assert.True(HasLoginCookie(response));
        await using var db = postgres.CreateContext();
        var lastLogin = await db.Users.Where(u => u.Id == userId).Select(u => u.LastLoginAt).SingleAsync(Ct);
        Assert.NotNull(lastLogin);
        Assert.Equal(DateTimeKind.Utc, lastLogin.Value.Kind);
        Assert.InRange(lastLogin.Value, before.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));

        using var home = await app.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Contains("Rejestr czasu pracy", await TextAsync(home));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?ReturnUrl=https%3A%2F%2Fevil.example%2F")]
    [InlineData("?ReturnUrl=%2F%2Fevil.example%2F")]
    [InlineData("?ReturnUrl=%2F%5Cevil.example%2F")]
    public async Task Login_ActiveAccount_WithoutLocalReturnUrl_RedirectsHome(string query)
    {
        await using var app = new AppClient(postgres);
        await app.CreateAccountAsync("ola", Password, AccountStatus.Active);

        using var response = await app.LoginAsync("ola", Password, "/konto/logowanie" + query);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("/", location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString);
        if (location.IsAbsoluteUri)
        {
            Assert.Equal("localhost", location.Host);
        }
    }

    [Fact]
    public async Task Login_FiveWrongPasswords_SuspendsLogin_EvenWithCorrectPassword()
    {
        await using var app = new AppClient(postgres);
        var userId = await app.CreateAccountAsync("piotr", Password, AccountStatus.Active);

        for (var attempt = 0; attempt < IdentitySetup.MaxFailedAccessAttempts; attempt++)
        {
            using var wrong = await app.LoginAsync("piotr", "zle-haslo");
        }
        using var response = await app.LoginAsync("piotr", Password);

        Assert.Contains(Login.LockedOutMessage, await TextAsync(response));
        Assert.False(HasLoginCookie(response));
        await using var db = postgres.CreateContext();
        var lockoutEnd = await db.Users.Where(u => u.Id == userId).Select(u => u.LockoutEnd).SingleAsync(Ct);
        Assert.NotNull(lockoutEnd);
        Assert.InRange(lockoutEnd.Value - DateTimeOffset.UtcNow, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task Logout_PostWithToken_EndsSession_AndReturnsToLogin()
    {
        await using var app = await LoggedInAsync();
        // A token issued for the logged-in User (any page with a form; the logout button comes with story 1.3).
        var (_, fields) = await app.GetPageAsync(AccountEndpoints.ChangePasswordPath);
        Assert.True(fields.ContainsKey("__RequestVerificationToken"));

        using var response = await app.Http.PostAsync("/konto/wyloguj", new FormUrlEncodedContent(fields), Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/konto/logowanie", response.Headers.Location?.OriginalString);
        using var home = await app.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
    }

    [Fact]
    public async Task Logout_ForgetsTheUsersDateOfNowyWpis_OnlyTheirs()
    {
        await using var app = new AppClient(postgres);
        var userId = await app.CreateAccountAsync("zalogowany", Password, AccountStatus.Active);
        using (var login = await app.LoginAsync("zalogowany", Password))
        {
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        }
        var memory = app.Services.GetRequiredService<EntryDateMemory>();
        memory.Remember(userId, new DateOnly(2026, 9, 3));
        memory.Remember(userId + 1000, new DateOnly(2026, 9, 4));
        var (_, fields) = await app.GetPageAsync(AccountEndpoints.ChangePasswordPath);

        using var response = await app.Http.PostAsync(AccountEndpoints.LogoutPath, new FormUrlEncodedContent(fields), Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Null(memory.Find(userId));
        Assert.Equal(new DateOnly(2026, 9, 4), memory.Find(userId + 1000));
    }

    [Fact]
    public async Task Logout_PostWithoutToken_Returns400_AndKeepsSession()
    {
        await using var app = await LoggedInAsync();

        using var response = await app.Http.PostAsync("/konto/wyloguj", new FormUrlEncodedContent([]), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(AccountEndpoints.InvalidLogoutMessage, await response.Content.ReadAsStringAsync(Ct));
        using var home = await app.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
    }

    [Fact]
    public async Task Logout_Get_DoesNotLogOut()
    {
        await using var app = await LoggedInAsync();

        using var response = await app.Http.GetAsync("/konto/wyloguj", Ct);

        Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
        Assert.False(response.IsSuccessStatusCode);
        using var home = await app.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
    }

    [Fact]
    public async Task Anonymous_GetHome_RedirectsToLogin_WithReturnUrl()
    {
        await using var app = new AppClient(postgres);

        using var response = await app.Http.GetAsync("/", Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!;
        Assert.Equal("/konto/logowanie", location.AbsolutePath);
        Assert.Equal("?ReturnUrl=%2F", location.Query);
        AssertCsp(response);
    }

    [Theory]
    [InlineData("/konto/logowanie")]
    [InlineData("/konto/rejestracja")]
    [InlineData("/konto/brak-dostepu")]
    public async Task AccountPages_AreAnonymous_Static_WithCsp_AndHelpLink(string path)
    {
        await using var app = new AppClient(postgres);

        using var response = await app.Http.GetAsync(path, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertCsp(response);
        var html = await TextAsync(response);
        // Static rendering: no interactive Server component markers (AD-15).
        Assert.DoesNotContain("\"type\":\"server\"", html);
        Assert.DoesNotContain("https://", html);
        Assert.Contains("href=\"/pomoc", html);
    }

    [Fact]
    public async Task Home_IsInteractiveServer_ForLoggedInUser()
    {
        await using var app = await LoggedInAsync();

        var (html, _) = await app.GetPageAsync("/");

        Assert.Contains("\"type\":\"server\"", html);
    }

    [Fact]
    public async Task Home_ForLoggedInUser_HasLogoutFormWithToken_AndReconnectMarker()
    {
        await using var app = await LoggedInAsync();

        var (html, fields) = await app.GetPageAsync("/");

        Assert.Contains($"<form method=\"post\" action=\"{AccountEndpoints.LogoutPath}\"", html);
        Assert.True(fields.ContainsKey("__RequestVerificationToken"));
        Assert.Contains("id=\"components-reconnect-modal\"", html);
        Assert.Contains("Odśwież stronę", html);

        // The token of the top bar form logs out (AD-15).
        using var logout = await app.Http.PostAsync(AccountEndpoints.LogoutPath, new FormUrlEncodedContent(fields), Ct);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal(AccountEndpoints.LoginPath, logout.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Help_UnknownTopic_IsPolishNotFound_StartTopicIsAvailable()
    {
        await using var app = await LoggedInAsync();

        using var unknown = await app.Http.GetAsync("/pomoc/xyz", Ct);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Contains("Nie znaleziono", await TextAsync(unknown));

        var (html, _) = await app.GetPageAsync("/pomoc/start");
        Assert.Contains("Strona startowa i menu", html);
        var (index, _) = await app.GetPageAsync("/pomoc");
        Assert.Contains("href=\"/pomoc/start\"", index);
    }

    [Fact]
    public async Task RateLimit_EleventhPostInAMinute_Returns429_InPolish_WithCsp()
    {
        await using var app = new AppClient(postgres);
        var (_, fields) = await app.GetPageAsync("/konto/logowanie");
        fields["Input.UserName"] = "nikt";
        fields["Input.Password"] = Password;

        for (var attempt = 0; attempt < AccountRateLimit.PermitLimit; attempt++)
        {
            using var allowed = await app.Http.PostAsync("/konto/logowanie", new FormUrlEncodedContent(fields), Ct);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        using var response = await app.Http.PostAsync("/konto/logowanie", new FormUrlEncodedContent(fields), Ct);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(AccountRateLimit.RejectedMessage, await TextAsync(response));
        AssertCsp(response);

        // The limit is shared with registration; GET is not limited.
        using var register = await app.Http.PostAsync("/konto/rejestracja", new FormUrlEncodedContent(fields), Ct);
        Assert.Equal(HttpStatusCode.TooManyRequests, register.StatusCode);
        using var get = await app.Http.GetAsync("/konto/logowanie", Ct);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
    }

    [Fact]
    public async Task UserDbContextFactory_WithoutLoggedInUser_Throws()
    {
        var factory = postgres.CreateUserDbContextFactory();

        await Assert.ThrowsAsync<NoCurrentUserException>(() => factory.CreateDbContextAsync(Ct));
    }

    [Fact]
    public async Task CurrentUser_InHttpRequest_ReadsNameIdentifier_FromHttpContextUser()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = Principal(42) } };
        var currentUser = new CurrentUser(accessor, new ServerAuthenticationStateProvider(), postgres.ContextFactory);

        Assert.Equal(42, await currentUser.GetUserIdAsync());

        accessor.HttpContext = new DefaultHttpContext();
        Assert.Null(await currentUser.FindUserIdAsync());
        await Assert.ThrowsAsync<NoCurrentUserException>(currentUser.GetUserIdAsync);
    }

    [Fact]
    public async Task CurrentUser_InCircuit_ReadsAuthenticationStateProvider_NotHttpContext()
    {
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = Principal(1) } };
        var provider = new ServerAuthenticationStateProvider();
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(Principal(7))));
        var currentUser = new CurrentUser(accessor, provider, postgres.ContextFactory);
        _ = new CurrentUserCircuitHandler(currentUser);

        Assert.Equal(7, await currentUser.GetUserIdAsync());

        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        await Assert.ThrowsAsync<NoCurrentUserException>(currentUser.GetUserIdAsync);
    }

    [Fact]
    public async Task UserDbContextFactory_UsesCurrentUser()
    {
        var userId = await postgres.CreateUserAsync("kontekst");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = Principal(userId) } };
        var factory = new UserDbContextFactory(
            postgres.ContextFactory, new CurrentUser(accessor, new ServerAuthenticationStateProvider(), postgres.ContextFactory));

        await using var db = await factory.CreateDbContextAsync(Ct);

        Assert.Equal(userId, db.CurrentUserId);
    }

    [Fact]
    public void Roles_AreSchemaData()
    {
        using var db = postgres.CreateContext();
        var roles = db.Roles.OrderBy(r => r.Id).Select(r => new { r.Id, r.Name, r.NormalizedName }).ToList();

        Assert.Equal(
            [new { Id = 1, Name = (string?)"Administrator", NormalizedName = (string?)"ADMINISTRATOR" },
             new { Id = 2, Name = (string?)"User", NormalizedName = (string?)"USER" }],
            roles);
    }

    private async Task<AppClient> LoggedInAsync()
    {
        var app = new AppClient(postgres);
        await app.CreateAccountAsync("zalogowany", Password, AccountStatus.Active);
        using var response = await app.LoginAsync("zalogowany", Password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return app;
    }

    /// <summary>Accounts other than the first Administrator, which every started application creates (AD-21).</summary>
    private static IQueryable<AppUser> NonAdministrators(AppDbContext db) =>
        db.Users.Where(u => u.UserName != PostgresFixture.FirstAdminUserName);

    private static ClaimsPrincipal Principal(int userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture))], "test"));

    private static bool HasLoginCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
        && cookies.Any(c => c.StartsWith(IdentityConstants.ApplicationScheme.Insert(0, ".AspNetCore.") + "=", StringComparison.Ordinal)
            && !c.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

    /// <summary>The response body with HTML entities decoded (Blazor encodes non-ASCII characters such as Polish letters).</summary>
    private static async Task<string> TextAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Ct));

    private static void AssertCsp(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var values));
        Assert.Equal(SecurityHeaders.ContentSecurityPolicy, Assert.Single(values));
    }
}
