using System.Net;
using System.Text.Json;
using Npgsql;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

/// <summary><c>GET /health</c> (AD-20): anonymous, database state and image tag, also during a required password change.</summary>
public sealed class HealthTests(PostgresFixture postgres) : IAsyncLifetime
{
    private const string Password = "haslo1";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await postgres.ResetAsync();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    [Fact]
    public async Task Health_HealthyDatabase_Returns200_WithTag_WithoutLogin()
    {
        await using var app = new AppClient(postgres, new Dictionary<string, string?> { ["APP_IMAGE_TAG"] = "abc123" });

        using var response = await app.Http.GetAsync(HealthEndpoint.Path, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("{\"status\":\"Healthy\",\"tag\":\"abc123\"}", await response.Content.ReadAsStringAsync(Ct));
    }

    [Fact]
    public async Task Health_WithoutTag_ReportsLocal()
    {
        // The in-memory null shadows an APP_IMAGE_TAG set in the environment of the test process.
        await using var app = new AppClient(postgres, new Dictionary<string, string?> { ["APP_IMAGE_TAG"] = null });

        using var response = await app.Http.GetAsync(HealthEndpoint.Path, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (status, tag) = await BodyAsync(response);
        Assert.Equal("Healthy", status);
        Assert.Equal("local", tag);
    }

    [Fact]
    public async Task Health_DuringRequiredPasswordChange_Returns200_WithoutRedirect()
    {
        await using var app = new AppClient(postgres);
        await app.CreateAccountAsync("wymuszona", Password, AccountStatus.Active, mustChangePassword: true);
        using (var login = await app.LoginAsync("wymuszona", Password))
        {
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        }
        // Precondition: the session is in the required password change.
        using (var home = await app.Http.GetAsync("/", Ct))
        {
            Assert.Equal(HttpStatusCode.Redirect, home.StatusCode);
        }

        using var response = await app.Http.GetAsync(HealthEndpoint.Path, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", (await BodyAsync(response)).Status);
    }

    /// <summary>
    /// The application starts on its own database in the container, which then disappears: <c>/health</c> answers 503
    /// in the same shape, so the deploy workflow sees that the database is not reachable.
    /// </summary>
    [Fact]
    public async Task Health_DatabaseGoneAfterStart_Returns503_WithSameShape()
    {
        const string database = "rejestr_health";
        await ExecuteAsync($"DROP DATABASE IF EXISTS {database} WITH (FORCE);");
        await ExecuteAsync($"CREATE DATABASE {database};");
        try
        {
            await using var app = new AppClient(postgres, new Dictionary<string, string?>
            {
                ["Database:Name"] = database,
                ["APP_IMAGE_TAG"] = "abc123",
            });
            using (var healthy = await app.Http.GetAsync(HealthEndpoint.Path, Ct))
            {
                Assert.Equal(HttpStatusCode.OK, healthy.StatusCode);
            }

            await ExecuteAsync($"DROP DATABASE {database} WITH (FORCE);");
            using var response = await app.Http.GetAsync(HealthEndpoint.Path, Ct);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            var (status, tag) = await BodyAsync(response);
            Assert.NotEqual("Healthy", status);
            Assert.Equal("abc123", tag);
        }
        finally
        {
            await ExecuteAsync($"DROP DATABASE IF EXISTS {database} WITH (FORCE);");
        }
    }

    private async Task ExecuteAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(postgres.DatabaseOptions.BuildConnectionString());
        await connection.OpenAsync(Ct);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(Ct);
    }

    private static async Task<(string? Status, string? Tag)> BodyAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var root = json.RootElement;
        Assert.Equal(2, root.EnumerateObject().Count());
        return (root.GetProperty("status").GetString(), root.GetProperty("tag").GetString());
    }
}
