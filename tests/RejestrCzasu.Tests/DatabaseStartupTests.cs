using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Options;
using RejestrCzasu.Startup;

namespace RejestrCzasu.Tests;

public sealed class DatabaseStartupTests(PostgresFixture postgres) : IAsyncDisposable
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Service providers built by <see cref="CreateStartup"/>, disposed when the test ends.</summary>
    private readonly List<ServiceProvider> _providers = [];

    public async ValueTask DisposeAsync()
    {
        foreach (var provider in _providers)
        {
            await provider.DisposeAsync();
        }
    }

    [Fact]
    public async Task WrongPassword_ReportsSettingsProblem_NotDockerProblem()
    {
        var options = postgres.DatabaseOptions;
        options.Password = "wrong-password";

        var ex = await Assert.ThrowsAsync<DatabaseStartupException>(() => CreateStartup(options).RunAsync(Ct));

        Assert.StartsWith(DatabaseStartup.ServerRejectedMessage, ex.Message);
        Assert.DoesNotContain("Docker Desktop", ex.Message);
        Assert.Contains("28P01", ex.Message);
    }

    /// <summary>A database where Migrate() cannot create the schema: step 3 fails with a console message.</summary>
    [Fact]
    public async Task MigrationFailure_ThrowsWithReadableMessage()
    {
        const string brokenDatabase = "rejestr_broken";
        await using (var admin = new NpgsqlConnection(postgres.DatabaseOptions.BuildConnectionString()))
        {
            await admin.OpenAsync(Ct);
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS {brokenDatabase};", admin);
            await drop.ExecuteNonQueryAsync(Ct);
            await using var create = new NpgsqlCommand($"CREATE DATABASE {brokenDatabase};", admin);
            await create.ExecuteNonQueryAsync(Ct);
        }

        var options = postgres.DatabaseOptions;
        options.Name = brokenDatabase;
        await using (var broken = new NpgsqlConnection(options.BuildConnectionString()))
        {
            await broken.OpenAsync(Ct);
            // A table the Initial migration wants to create, without migration history.
            await using var conflict = new NpgsqlCommand("CREATE TABLE project (id int);", broken);
            await conflict.ExecuteNonQueryAsync(Ct);
        }

        var ex = await Assert.ThrowsAsync<DatabaseStartupException>(() => CreateStartup(options).RunAsync(Ct));

        Assert.StartsWith("Migracja bazy nie powiodła się", ex.Message);
    }

    [Fact]
    public async Task FirstStart_WithoutAdministrator_CreatesActiveAdministrator_WithRolesAdministratorAndUser()
    {
        await postgres.ResetAsync();
        // A regular Account does not count as an Administrator.
        var regularUser = await postgres.CreateUserAsync("zwykly");

        await CreateStartup(postgres.DatabaseOptions, FirstAdmin("autor", "haslo-autora")).RunAsync(Ct);

        await using var db = postgres.CreateContext();
        var admin = await db.Users.SingleAsync(u => u.Id != regularUser, Ct);
        Assert.Equal("autor", admin.UserName);
        Assert.Equal(AccountStatus.Active, admin.Status);
        Assert.False(admin.MustChangePassword);
        Assert.False(string.IsNullOrEmpty(admin.SecurityStamp));
        Assert.Equal([AppRoles.Administrator, AppRoles.User], await RolesOfAsync(db, admin.Id));

        // The password is the configured one (hashed by Identity).
        var hasher = new PasswordHasher<AppUser>();
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(admin, admin.PasswordHash!, "haslo-autora"));

        // No account_settings row (AD-7).
        await using var userDb = await postgres.CreateUserContextAsync(admin.Id);
        Assert.Equal(0, await userDb.AccountSettings.CountAsync(Ct));
    }

    [Fact]
    public async Task NextStart_AdministratorExists_IgnoresOtherConfiguration()
    {
        await postgres.ResetAsync();
        await CreateStartup(postgres.DatabaseOptions, FirstAdmin("autor", "haslo-autora")).RunAsync(Ct);
        string before;
        await using (var db = postgres.CreateContext())
        {
            before = (await db.Users.SingleAsync(Ct)).PasswordHash!;
        }

        await CreateStartup(postgres.DatabaseOptions, FirstAdmin("ktos-inny", "inne-haslo")).RunAsync(Ct);
        // Without any configuration the start succeeds as well: an Administrator exists.
        await CreateStartup(postgres.DatabaseOptions, new FirstAdminOptions()).RunAsync(Ct);

        await using var check = postgres.CreateContext();
        var admin = await check.Users.SingleAsync(Ct);
        Assert.Equal("autor", admin.UserName);
        Assert.Equal(before, admin.PasswordHash);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("autor", null)]
    [InlineData(null, "haslo-autora")]
    [InlineData("  ", "haslo-autora")]
    public async Task NoAdministrator_NoConfiguration_FailsWithPolishMessage(string? userName, string? password)
    {
        await postgres.ResetAsync();

        var ex = await Assert.ThrowsAsync<DatabaseStartupException>(
            () => CreateStartup(postgres.DatabaseOptions, FirstAdmin(userName, password)).RunAsync(Ct));

        Assert.Equal(FirstAdministrator.MissingConfigurationMessage, ex.Message);
        await using var db = postgres.CreateContext();
        Assert.Equal(0, await db.Users.CountAsync(Ct));
    }

    [Fact]
    public async Task NoAdministrator_PasswordRejectedByIdentity_FailsWithPolishIdentityError_WithoutAccount()
    {
        await postgres.ResetAsync();

        var ex = await Assert.ThrowsAsync<DatabaseStartupException>(
            () => CreateStartup(postgres.DatabaseOptions, FirstAdmin("autor", "12345")).RunAsync(Ct));

        Assert.StartsWith(FirstAdministrator.RejectedMessagePrefix, ex.Message);
        Assert.Contains("Hasło musi mieć co najmniej 6 znaków.", ex.Message);
        Assert.DoesNotContain("12345", ex.Message);
        await using var db = postgres.CreateContext();
        Assert.Equal(0, await db.Users.CountAsync(Ct));
        Assert.Equal(0, await db.UserRoles.CountAsync(Ct));
    }

    private static FirstAdminOptions FirstAdmin(string? userName, string? password) =>
        new() { UserName = userName, Password = password };

    private static async Task<List<string?>> RolesOfAsync(AppDbContext db, int userId)
    {
        var roleIds = await db.UserRoles.Where(r => r.UserId == userId).Select(r => r.RoleId).ToListAsync(Ct);
        return await db.Roles.Where(r => roleIds.Contains(r.Id)).OrderBy(r => r.Id).Select(r => r.Name).ToListAsync(Ct);
    }

    private DatabaseStartup CreateStartup(DatabaseOptions options, FirstAdminOptions? firstAdmin = null)
    {
        // The application's registrations (AddAppDatabase, AddAccounts, AddStartupSequence), so the test uses the same
        // unpooled factory and the same Identity configuration (password rules, Polish errors).
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(firstAdmin ?? new FirstAdminOptions()));
        services.AddAppDatabase();
        services.AddAccounts();
        services.AddStartupSequence();
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return provider.GetRequiredService<DatabaseStartup>();
    }
}
