using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

/// <summary>
/// The Account state during a session (AD-17) and the change of the own password (AD-15): the login cookie validator,
/// <see cref="CurrentUser"/> in a circuit, the circuit revalidation, <c>/konto/zmiana-hasla</c> and its redirect.
/// </summary>
public sealed class AccountSessionTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "haslo1";
    private const string NewPassword = "nowe-haslo";
    private static readonly string CookieName = ".AspNetCore." + IdentityConstants.ApplicationScheme;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task FirstAdministrator_CreatedAtStartup_LogsIn_WithoutSql()
    {
        await using var app = new AppClient(postgres);

        using var login = await app.LoginAsync(PostgresFixture.FirstAdminUserName, PostgresFixture.FirstAdminPassword);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        using var home = await app.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
    }

    // --- Login cookie validator (every HTTP request) ---

    [Fact]
    public async Task BlockedDuringSession_NextRequest_GoesToLogin_AndDeletesCookie()
    {
        await using var app = new AppClient(postgres);
        var userId = await app.CreateAccountAsync("jan", Password, AccountStatus.Active);
        using (var login = await app.LoginAsync("jan", Password))
        {
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        }
        using (var home = await app.Http.GetAsync("/", Ct))
        {
            Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        }

        // Only the status changes (same security stamp): the validator reads the status itself.
        await SetStatusAsync(userId, AccountStatus.Blocked);
        using var response = await app.Http.GetAsync("/", Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AccountEndpoints.LoginPath, response.Headers.Location?.AbsolutePath);
        Assert.True(DeletesLoginCookie(response));

        // Active again: the deleted cookie does not come back.
        await SetStatusAsync(userId, AccountStatus.Active);
        using var again = await app.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.Redirect, again.StatusCode);
    }

    [Fact]
    public async Task DeletedDuringSession_NextRequest_GoesToLogin_AndDeletesCookie()
    {
        await using var app = await LoggedInAsync("usuniety");
        await using (var db = postgres.CreateContext())
        {
            var user = await db.Users.SingleAsync(u => u.UserName == "usuniety", Ct);
            await db.UserRoles.Where(r => r.UserId == user.Id).ExecuteDeleteAsync(Ct);
            await db.Users.Where(u => u.Id == user.Id).ExecuteDeleteAsync(Ct);
        }

        using var response = await app.Http.GetAsync("/", Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(DeletesLoginCookie(response));
    }

    [Fact]
    public async Task SecurityStampOfOtherUserChanged_SessionOfUserGoesOn_SessionOfOtherUserEnds()
    {
        await using var appA = await LoggedInAsync("user.a");
        await using var appB = await LoggedInAsync("user.b");

        await using (var scope = appB.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var userB = await userManager.FindByNameAsync("user.b");
            await userManager.UpdateSecurityStampAsync(userB!);
        }

        using var homeA = await appA.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.OK, homeA.StatusCode);
        using var homeB = await appB.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.Redirect, homeB.StatusCode);
        Assert.True(DeletesLoginCookie(homeB));
    }

    [Theory]
    [InlineData("pending", Login.PendingMessage)]
    [InlineData("blocked", Login.BlockedMessage)]
    public async Task LoginPage_WithReason_ShowsAccountStateMessage(string reason, string message)
    {
        await using var app = new AppClient(postgres);

        var (html, _) = await app.GetPageAsync($"/konto/logowanie?powod={reason}");

        Assert.Contains(message, html);
    }

    [Fact]
    public async Task LoginPage_LoggedInUser_RedirectsToReturnUrl_AnonymousSeesForm()
    {
        await using var anonymous = new AppClient(postgres);
        var (form, _) = await anonymous.GetPageAsync("/konto/logowanie?ReturnUrl=%2F");
        Assert.Contains("id=\"login-password\"", form);

        await using var app = await LoggedInAsync("druga.karta");
        using var response = await app.Http.GetAsync("/konto/logowanie?ReturnUrl=%2F", Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task LoginPage_WithUnknownReason_ShowsNoMessage()
    {
        await using var app = new AppClient(postgres);

        var (html, _) = await app.GetPageAsync("/konto/logowanie?powod=cokolwiek");

        Assert.DoesNotContain(Login.PendingMessage, html);
        Assert.DoesNotContain(Login.BlockedMessage, html);
    }

    [Theory]
    [InlineData(AccountStatus.Pending, "/konto/logowanie?powod=pending")]
    [InlineData(AccountStatus.Blocked, "/konto/logowanie?powod=blocked")]
    [InlineData(null, "/konto/logowanie")]
    public void AccountInactiveException_LoginUrl_CarriesState(AccountStatus? status, string expected) =>
        Assert.Equal(expected, new AccountInactiveException(status).LoginUrl);

    // --- CurrentUser in a circuit (every User-mode context) ---

    [Theory]
    [InlineData(AccountStatus.Pending)]
    [InlineData(AccountStatus.Blocked)]
    public async Task Circuit_InactiveAccount_CreateDbContext_ThrowsAccountInactive_WithState(AccountStatus status)
    {
        var userId = await postgres.CreateUserAsync("w.obwodzie");
        var factory = CircuitFactory(userId);
        await using (await factory.CreateDbContextAsync(Ct))
        {
        }

        await SetStatusAsync(userId, status);

        var ex = await Assert.ThrowsAsync<AccountInactiveException>(() => factory.CreateDbContextAsync(Ct));
        Assert.Equal(status, ex.Status);
    }

    [Fact]
    public async Task Circuit_DeletedAccount_CreateDbContext_ThrowsAccountInactive_WithoutState()
    {
        var factory = CircuitFactory(987654);

        var ex = await Assert.ThrowsAsync<AccountInactiveException>(() => factory.CreateDbContextAsync(Ct));
        Assert.Null(ex.Status);
    }

    [Fact]
    public async Task Circuit_PasswordChangeRequired_CreateDbContext_ThrowsPasswordChangeRequired()
    {
        var userId = await postgres.CreateUserAsync("zmiana.w.obwodzie");
        await using (var db = postgres.CreateContext())
        {
            await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.MustChangePassword, true), Ct);
        }

        await Assert.ThrowsAsync<PasswordChangeRequiredException>(() => CircuitFactory(userId).CreateDbContextAsync(Ct));
    }

    // --- Circuit revalidation (every minute) ---

    [Fact]
    public async Task Revalidation_IsTheAppProvider_EveryMinute()
    {
        await using var app = new AppClient(postgres);
        await using var scope = app.Services.CreateAsyncScope();

        Assert.IsType<AccountRevalidatingAuthenticationStateProvider>(
            scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>());
        Assert.Equal(TimeSpan.FromMinutes(1), AccountRevalidatingAuthenticationStateProvider.Interval);
    }

    [Fact]
    public async Task Revalidation_RejectsBlockedAccount_AndChangedStamp()
    {
        await using var app = new AppClient(postgres);
        var userId = await app.CreateAccountAsync("rewalidacja", Password, AccountStatus.Active);
        using var login = await app.LoginAsync("rewalidacja", Password);
        var principal = PrincipalOf(app, login);
        await using var scope = app.Services.CreateAsyncScope();
        var provider = Revalidation(scope);

        Assert.True(await provider.IsValidAsync(principal, Ct));

        await SetStatusAsync(userId, AccountStatus.Blocked);
        Assert.False(await provider.IsValidAsync(principal, Ct));

        await SetStatusAsync(userId, AccountStatus.Active);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        await userManager.UpdateSecurityStampAsync((await userManager.FindByIdAsync(userId.ToString(System.Globalization.CultureInfo.InvariantCulture)))!);
        Assert.False(await provider.IsValidAsync(principal, Ct));
    }

    // --- Required password change ---

    [Fact]
    public async Task RequiredChange_EveryPage_RedirectsToChangePassword_StaticAssetsAndLogoutWork()
    {
        await using var app = await LoggedInAsync("wymuszona", mustChangePassword: true);

        foreach (var path in new[] { "/", "/not-found", "/konto/brak-dostepu", "/konto/logowanie", "/nie-ma-takiej" })
        {
            using var page = await app.Http.GetAsync(path, Ct);
            Assert.Equal(HttpStatusCode.Redirect, page.StatusCode);
            Assert.Equal(AccountEndpoints.ChangePasswordPath, page.Headers.Location?.OriginalString);
        }

        foreach (var asset in new[] { "/app.css", "/_framework/blazor.web.js" })
        {
            using var resource = await app.Http.GetAsync(asset, Ct);
            Assert.Equal(HttpStatusCode.OK, resource.StatusCode);
        }

        // The circuit endpoint is not redirected (it answers itself).
        using (var negotiate = await app.Http.PostAsync("/_blazor/negotiate?negotiateVersion=1", null, Ct))
        {
            Assert.NotEqual(HttpStatusCode.Redirect, negotiate.StatusCode);
        }

        var (html, fields) = await app.GetPageAsync(AccountEndpoints.ChangePasswordPath);
        Assert.Contains(ChangePassword.RequiredNotice, html);
        Assert.DoesNotContain("Obecne hasło", html);
        Assert.Contains($"action=\"{AccountEndpoints.LogoutPath}\"", html);

        using var logout = await app.Http.PostAsync(AccountEndpoints.LogoutPath, new FormUrlEncodedContent(fields), Ct);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal(AccountEndpoints.LoginPath, logout.Headers.Location?.OriginalString);
        using var after = await app.Http.GetAsync(AccountEndpoints.ChangePasswordPath, Ct);
        Assert.Equal(AccountEndpoints.LoginPath, after.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task RequiredChange_NewPasswordTwice_ClearsFlag_KeepsSession_HomeAvailable()
    {
        await using var app = await LoggedInAsync("wymuszona", mustChangePassword: true);
        var stampBefore = await StampOfAsync("wymuszona");

        using var response = await SubmitChangeAsync(app, current: null, NewPassword, NewPassword);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.AbsolutePath);
        await using (var db = postgres.CreateContext())
        {
            var user = await db.Users.SingleAsync(u => u.UserName == "wymuszona", Ct);
            Assert.False(user.MustChangePassword);
            Assert.NotEqual(stampBefore, user.SecurityStamp);
        }
        using var home = await app.Http.GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);

        await using var other = new AppClient(postgres);
        using var login = await other.LoginAsync("wymuszona", NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
    }

    [Fact]
    public async Task RequiredChange_PasswordTooShort_IsRejected_InPolish_FlagStays()
    {
        await using var app = await LoggedInAsync("wymuszona", mustChangePassword: true);

        using var response = await SubmitChangeAsync(app, current: null, "12345", "12345");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hasło musi mieć co najmniej 6 znaków.", await TextAsync(response));
        await using var db = postgres.CreateContext();
        Assert.True(await db.Users.Where(u => u.UserName == "wymuszona").Select(u => u.MustChangePassword).SingleAsync(Ct));
    }

    [Fact]
    public async Task ChangePassword_DifferentConfirmation_IsRejected_InPolish()
    {
        await using var app = await LoggedInAsync("rozne");

        using var response = await SubmitChangeAsync(app, Password, NewPassword, "inne-haslo");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Hasła nie są takie same.", await TextAsync(response));
    }

    [Fact]
    public async Task VoluntaryChange_WrongCurrentPassword_IsRejected_InPolish()
    {
        await using var app = await LoggedInAsync("dobrowolna");
        var stampBefore = await StampOfAsync("dobrowolna");

        var (form, _) = await app.GetPageAsync(AccountEndpoints.ChangePasswordPath);
        Assert.Contains("Obecne hasło", form);
        Assert.DoesNotContain(ChangePassword.RequiredNotice, form);

        using var response = await SubmitChangeAsync(app, "zle-haslo", NewPassword, NewPassword);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(AccountPassword.WrongCurrentPasswordMessage, await TextAsync(response));
        Assert.Equal(stampBefore, await StampOfAsync("dobrowolna"));

        using var withoutCurrent = await SubmitChangeAsync(app, "", NewPassword, NewPassword);
        Assert.Contains(AccountPassword.CurrentPasswordRequiredMessage, await TextAsync(withoutCurrent));
    }

    [Fact]
    public async Task VoluntaryChange_CorrectPassword_KeepsThisSession_EndsOtherSession_AndRevalidationAcceptsNewStamp()
    {
        await using var other = await LoggedInAsync("dobrowolna");
        await using var app = new AppClient(postgres);
        using var oldLogin = await app.LoginAsync("dobrowolna", Password);
        var oldPrincipal = PrincipalOf(app, oldLogin);

        using var response = await SubmitChangeAsync(app, Password, NewPassword, NewPassword);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"{AccountEndpoints.ChangePasswordPath}?zmieniono=1", response.Headers.Location?.PathAndQuery);
        var (done, _) = await app.GetPageAsync(AccountEndpoints.ChangePasswordPath + "?zmieniono=1");
        Assert.Contains(ChangePassword.ChangedMessage, done);
        using (var home = await app.Http.GetAsync("/", Ct))
        {
            Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        }

        // The other client (old cookie, old stamp) is logged out on its next request.
        using (var otherHome = await other.Http.GetAsync("/", Ct))
        {
            Assert.Equal(HttpStatusCode.Redirect, otherHome.StatusCode);
            Assert.True(DeletesLoginCookie(otherHome));
        }

        // The circuit check after more than a minute: the cookie refreshed by RefreshSignInAsync carries the new stamp.
        await using var scope = app.Services.CreateAsyncScope();
        var provider = Revalidation(scope);
        Assert.True(await provider.IsValidAsync(PrincipalOf(app, response), Ct));
        Assert.False(await provider.IsValidAsync(oldPrincipal, Ct));

        await using var fresh = new AppClient(postgres);
        using var newLogin = await fresh.LoginAsync("dobrowolna", NewPassword);
        Assert.Equal(HttpStatusCode.Redirect, newLogin.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Anonymous_RedirectsToLogin()
    {
        await using var app = new AppClient(postgres);

        using var response = await app.Http.GetAsync(AccountEndpoints.ChangePasswordPath, Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(AccountEndpoints.LoginPath, response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task ChangePassword_IsStatic()
    {
        await using var app = await LoggedInAsync("statyczna");

        var (html, _) = await app.GetPageAsync(AccountEndpoints.ChangePasswordPath);

        Assert.DoesNotContain("\"type\":\"server\"", html);
        Assert.Contains("href=\"/pomoc/zmiana-hasla\"", html);
    }

    private async Task<AppClient> LoggedInAsync(string userName, bool mustChangePassword = false)
    {
        var app = new AppClient(postgres);
        await app.CreateAccountAsync(userName, Password, AccountStatus.Active, mustChangePassword);
        using var response = await app.LoginAsync(userName, Password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return app;
    }

    private static Task<HttpResponseMessage> SubmitChangeAsync(AppClient app, string? current, string newPassword, string confirm)
    {
        var values = new Dictionary<string, string>
        {
            ["Input.NewPassword"] = newPassword,
            ["Input.ConfirmPassword"] = confirm,
        };
        if (current is not null)
        {
            values["Input.CurrentPassword"] = current;
        }
        return app.SubmitFormAsync(AccountEndpoints.ChangePasswordPath, values);
    }

    private async Task SetStatusAsync(int userId, AccountStatus status)
    {
        await using var db = postgres.CreateContext();
        await db.Users.Where(u => u.Id == userId).ExecuteUpdateAsync(s => s.SetProperty(u => u.Status, status), Ct);
    }

    private async Task<string?> StampOfAsync(string userName)
    {
        await using var db = postgres.CreateContext();
        return await db.Users.Where(u => u.UserName == userName).Select(u => u.SecurityStamp).SingleAsync(Ct);
    }

    /// <summary><see cref="UserDbContextFactory"/> as in a Blazor circuit of the given User.</summary>
    private UserDbContextFactory CircuitFactory(int userId)
    {
        var provider = new ServerAuthenticationStateProvider();
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString(System.Globalization.CultureInfo.InvariantCulture))], "test")))));
        var currentUser = new CurrentUser(new HttpContextAccessor(), provider, postgres.ContextFactory);
        _ = new CurrentUserCircuitHandler(currentUser);
        return new UserDbContextFactory(postgres.ContextFactory, currentUser);
    }

    private static AccountRevalidatingAuthenticationStateProvider Revalidation(AsyncServiceScope scope) =>
        Assert.IsType<AccountRevalidatingAuthenticationStateProvider>(
            scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>());

    /// <summary>The principal in the login cookie set by <paramref name="response"/>, as a circuit would get it.</summary>
    private static ClaimsPrincipal PrincipalOf(AppClient app, HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var cookie = cookies.Single(c => c.StartsWith(CookieName + "=", StringComparison.Ordinal));
        var value = cookie[(CookieName.Length + 1)..].Split(';')[0];
        var options = app.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
        var ticket = options.TicketDataFormat.Unprotect(value);
        Assert.NotNull(ticket);
        return ticket.Principal;
    }

    private static bool DeletesLoginCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var cookies)
        && cookies.Any(c => c.StartsWith(CookieName + "=", StringComparison.Ordinal)
            && c.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));

    private static async Task<string> TextAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Ct));
}
