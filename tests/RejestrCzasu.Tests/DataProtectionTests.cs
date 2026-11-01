using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

/// <summary>
/// Data Protection keys in <c>data_protection_keys</c> with a fixed application name (AD-19): a new instance of the
/// application on the same database (a container restart or a new image) accepts the login cookie of the previous one.
/// </summary>
public sealed class DataProtectionTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "haslo1";
    private static readonly string CookieName = ".AspNetCore." + IdentityConstants.ApplicationScheme;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Restart_NewInstanceOnSameDatabase_AcceptsLoginCookie_KeyStoredInDatabase()
    {
        string cookie;
        await using (var first = new AppClient(postgres))
        {
            await first.CreateAccountAsync("jan", Password, AccountStatus.Active);
            using var login = await first.LoginAsync("jan", Password);
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            cookie = login.Headers.GetValues("Set-Cookie")
                .Select(c => c.Split(';')[0])
                .Single(c => c.StartsWith(CookieName + "=", StringComparison.Ordinal));
        }

        await using (var db = postgres.CreateContext())
        {
            Assert.Equal(1, await db.DataProtectionKeys.CountAsync(Ct));
        }

        await using var second = new AppClient(postgres);
        Assert.Equal(
            DatabaseServiceCollectionExtensions.DataProtectionApplicationName,
            second.Services.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Cookie", cookie);
        using var home = await second.Http.SendAsync(request, Ct);

        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        // The second instance used the key ring from the database instead of creating its own key.
        await using var check = postgres.CreateContext();
        Assert.Equal(1, await check.DataProtectionKeys.CountAsync(Ct));
    }
}
