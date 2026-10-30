using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
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
        ContainerName = _container.Name.TrimStart('/'),
    };

    /// <summary>Per-run temporary backup directory, so the Author's local settings are never used by tests.</summary>
    public string BackupDirectory { get; } =
        Path.Combine(Path.GetTempPath(), "rejestr-czasu-tests", Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Configuration overrides pointing the application at the container (<c>Database:*</c>) and at test-only
    /// <c>Backup</c>/<c>Export</c> values, so the Author's appsettings.Local.json does not leak into tests.
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
                ["Database:ContainerName"] = options.ContainerName,
                ["Backup:Directory"] = BackupDirectory,
                ["Export:Prefix"] = "Test_Testowy",
            };
        }
    }

    public async ValueTask InitializeAsync()
    {
        Directory.CreateDirectory(BackupDirectory);
        await _container.StartAsync();
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(DatabaseOptions.BuildConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options);
    }

    /// <summary>Removes all data and restarts identity sequences.</summary>
    public async Task ResetAsync()
    {
        await using var connection = new NpgsqlConnection(DatabaseOptions.BuildConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "TRUNCATE time_entry, project, work_type RESTART IDENTITY CASCADE;", connection);
        await command.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        try
        {
            Directory.Delete(BackupDirectory, recursive: true);
        }
        catch (IOException)
        {
            // Best effort cleanup of the temporary directory.
        }
    }
}
