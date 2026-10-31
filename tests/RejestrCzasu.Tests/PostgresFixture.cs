using System.Globalization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using RejestrCzasu.Accounts;
using RejestrCzasu.Data;
using RejestrCzasu.Options;
using Testcontainers.PostgreSql;

namespace RejestrCzasu.Tests;

/// <summary>
/// One <c>postgres:18</c> container per test run. The schema is created by <c>Migrate()</c>
/// (never <c>EnsureCreated()</c>); <see cref="ResetAsync"/> clears the data between tests.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string DatabaseName = "rejestr_test";
    private const string UserName = "rejestr_test";
    private const string Password = "test-only-password";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18")
        .WithDatabase(DatabaseName)
        .WithUsername(UserName)
        .WithPassword(Password)
        .Build();

    /// <summary>Connection parameters of the container, in the application's configuration shape.</summary>
    public DatabaseOptions DatabaseOptions => new()
    {
        Host = _container.Hostname,
        Port = _container.GetMappedPublicPort(PostgreSqlBuilder.PostgreSqlPort),
        Name = DatabaseName,
        User = UserName,
        Password = Password,
    };

    /// <summary>
    /// Configuration overrides pointing the application at the container (<c>Database:*</c>),
    /// so the Author's appsettings.Local.json does not leak into tests.
    /// </summary>
    public IReadOnlyDictionary<string, string?> ConfigurationOverrides
    {
        get
        {
            var options = DatabaseOptions;
            return new Dictionary<string, string?>
            {
                ["Database:Host"] = options.Host,
                ["Database:Port"] = options.Port.ToString(CultureInfo.InvariantCulture),
                ["Database:Name"] = options.Name,
                ["Database:User"] = options.User,
                ["Database:Password"] = options.Password,
            };
        }
    }

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    /// <summary>A system-mode context (no User): <see cref="IUserOwned"/> data is invisible and read-only.</summary>
    public AppDbContext CreateContext() => ContextFactory.CreateDbContext();

    /// <summary>The application's <see cref="IDbContextFactory{TContext}"/> shape, on the container database.</summary>
    public IDbContextFactory<AppDbContext> ContextFactory => new TestContextFactory(DatabaseOptions.BuildConnectionString());

    /// <summary>A User-mode context, created the only allowed way: through <see cref="UserDbContextFactory"/>.</summary>
    public Task<AppDbContext> CreateUserContextAsync(int userId) =>
        CreateUserDbContextFactory().CreateDbContextAsync(userId);

    /// <summary>
    /// <see cref="UserDbContextFactory"/> on the container database with a <see cref="CurrentUser"/> outside any
    /// request or circuit (nobody logged in); tests pass the User to the internal overload.
    /// </summary>
    public UserDbContextFactory CreateUserDbContextFactory() =>
        new(ContextFactory, new CurrentUser(new HttpContextAccessor(), new ServerAuthenticationStateProvider()));

    /// <summary>Creates an active Account with a fictional login and returns its id.</summary>
    public async Task<int> CreateUserAsync(string userName)
    {
        await using var db = CreateContext();
        var user = new AppUser
        {
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            Status = AccountStatus.Active,
            RegisteredAt = DateTime.UtcNow,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    /// <summary>
    /// Removes all data and restarts identity sequences: every table except the migration history and <c>roles</c>,
    /// whose rows are schema data seeded by the Initial migration (AD-15).
    /// </summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(DatabaseOptions.BuildConnectionString());
        await connection.OpenAsync();
        var tables = new List<string>();
        await using (var list = new NpgsqlCommand(
            "SELECT quote_ident(table_name) FROM information_schema.tables " +
            "WHERE table_schema = 'public' AND table_type = 'BASE TABLE' AND table_name NOT IN ('__EFMigrationsHistory', 'roles');",
            connection))
        await using (var reader = await list.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }
        await using var truncate = new NpgsqlCommand(
            $"TRUNCATE {string.Join(", ", tables)} RESTART IDENTITY;", connection);
        await truncate.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    private sealed class TestContextFactory(string connectionString) : IDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext() =>
            new(AppDbContextOptions(connectionString));
    }

    /// <summary>The application's database options (<see cref="DatabaseServiceCollectionExtensions.UseAppDatabase"/>).</summary>
    private static DbContextOptions<AppDbContext> AppDbContextOptions(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<AppDbContext>();
        builder.UseAppDatabase(connectionString);
        return builder.Options;
    }
}
