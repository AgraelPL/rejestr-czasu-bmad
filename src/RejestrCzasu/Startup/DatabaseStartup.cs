using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using RejestrCzasu.Data;
using RejestrCzasu.Options;

namespace RejestrCzasu.Startup;

/// <summary>
/// Startup sequence from AD-21, run by <c>Program</c> after the app is built and before the server starts:
/// (1) database connection check, (2) migrations. The first Administrator step (3) is added by a later story;
/// then (4) the server starts.
/// A failure of any step throws <see cref="DatabaseStartupException"/>; <c>Program</c> prints its
/// message and exits with code 1 without starting the server.
/// </summary>
public sealed class DatabaseStartup(
    IOptions<DatabaseOptions> databaseOptions,
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<DatabaseStartup> logger)
{
    public const int ConnectionTimeoutSeconds = 3;

    public const string NoConnectionMessage =
        "Brak połączenia z bazą: uruchom Docker Desktop i `docker compose up -d`.";

    public const string ServerRejectedMessage =
        "Baza odrzuciła połączenie: sprawdź ustawienia `Database:*` w appsettings.Local.json " +
        "i zgodność z wartościami POSTGRES_USER, POSTGRES_PASSWORD, POSTGRES_DB w pliku .env.";

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        // Step 1: connection check with a short timeout.
        await CheckConnectionAsync(cancellationToken);

        // Step 2: migrations.
        await MigrateAsync(cancellationToken);
    }

    private async Task CheckConnectionAsync(CancellationToken cancellationToken)
    {
        var connectionString = databaseOptions.Value.BuildConnectionString(ConnectionTimeoutSeconds);
        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
        }
        catch (PostgresException ex)
        {
            // The server is reachable but rejected the connection (e.g. 28P01 wrong password, 3D000 missing database).
            throw new DatabaseStartupException($"{ServerRejectedMessage}{Environment.NewLine}Szczegóły: {ex.Message}", ex);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DatabaseStartupException($"{NoConnectionMessage}{Environment.NewLine}Szczegóły: {ex.Message}", ex);
        }
    }

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
            await db.Database.MigrateAsync(cancellationToken);
            logger.LogInformation("Schemat bazy aktualny.");
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DatabaseStartupException($"Migracja bazy nie powiodła się: {ex.Message}", ex);
        }
    }
}

/// <summary>Startup failure with a message meant for the console (Polish, without a stack trace).</summary>
public sealed class DatabaseStartupException(string message, Exception innerException)
    : Exception(message, innerException);
