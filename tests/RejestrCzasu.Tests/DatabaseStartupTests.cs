using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
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

    private DatabaseStartup CreateStartup(DatabaseOptions options)
    {
        // The application's registration (AddAppDatabase), so the test uses the same, unpooled factory.
        var services = new ServiceCollection();
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
        services.AddAppDatabase();
        var provider = services.BuildServiceProvider();
        _providers.Add(provider);
        return new DatabaseStartup(
            Microsoft.Extensions.Options.Options.Create(options),
            provider.GetRequiredService<IDbContextFactory<AppDbContext>>(),
            NullLogger<DatabaseStartup>.Instance);
    }
}
