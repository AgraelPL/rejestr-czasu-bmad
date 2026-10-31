using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace RejestrCzasu.Tests;

/// <summary>
/// The whole application on the container database: the startup path (connection check, Migrate())
/// is the same one the Author runs; only <c>Database:*</c> is overridden.
/// </summary>
public sealed class ApplicationTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HomePage_WithoutLogin_RedirectsToLogin_WithContentSecurityPolicy()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        using var response = await client.GetAsync("/", Ct);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/konto/logowanie", response.Headers.Location?.AbsolutePath);
        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var values));
        Assert.Equal(SecurityHeaders.ContentSecurityPolicy, Assert.Single(values));
    }

    [Fact]
    public async Task LoginPage_Returns200_WithContentSecurityPolicy()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/konto/logowanie", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Content-Security-Policy", out var values));
        var csp = Assert.Single(values);
        Assert.Equal(SecurityHeaders.ContentSecurityPolicy, csp);
        Assert.Contains("script-src 'self'", csp);

        var html = await response.Content.ReadAsStringAsync(Ct);
        Assert.Contains("Rejestr czasu pracy", html);
        Assert.Contains("<html lang=\"pl\">", html);
        Assert.DoesNotContain("importmap", html);
        Assert.DoesNotContain("https://", html);
    }

    /// <summary>Runs the real application process against a closed port: exit code 1, message, no server.</summary>
    [Fact]
    public async Task Startup_WithoutDatabase_ExitsWithMessage_BeforeServerStarts()
    {
        var (exitCode, stdout, stderr) = await RunApplicationAsync(
            new Dictionary<string, string>
            {
                ["Database__Host"] = "127.0.0.1",
                ["Database__Port"] = "1",
                ["Database__Password"] = "unused",
            },
            []);

        Assert.Equal(1, exitCode);
        // ASCII part only: the console code page of the child process is not necessarily UTF-8.
        Assert.Contains("uruchom Docker Desktop i `docker compose up -d`", stderr);
        Assert.DoesNotContain("Now listening", stdout);
    }

    /// <summary>
    /// The real application process on an empty container database without an Administrator: a missing or rejected
    /// <c>FirstAdmin</c> ends the start with exit code 1 and a message, no server, no Account (AD-21, step 3).
    /// Command-line arguments come last in the configuration, so they override the Author's appsettings.Local.json.
    /// </summary>
    [Theory]
    [InlineData("", "", "`FirstAdmin:UserName`")]
    [InlineData("autor", "12345", "co najmniej 6 znak")]
    public async Task Startup_WithoutAdministrator_InvalidFirstAdmin_ExitsWithMessage_BeforeServerStarts(
        string userName, string password, string expectedMessagePart)
    {
        await postgres.ResetAsync();
        var environment = postgres.ConfigurationOverrides
            .Where(o => o.Key.StartsWith("Database:", StringComparison.Ordinal))
            .ToDictionary(o => o.Key.Replace(":", "__", StringComparison.Ordinal), o => o.Value ?? "");

        var (exitCode, stdout, stderr) = await RunApplicationAsync(
            environment,
            [$"--FirstAdmin:UserName={userName}", $"--FirstAdmin:Password={password}"]);

        Assert.Equal(1, exitCode);
        Assert.Contains(expectedMessagePart, stderr);
        Assert.DoesNotContain("Now listening", stdout);
        await using var db = postgres.CreateContext();
        Assert.Equal(0, await db.Users.CountAsync(Ct));
    }

    private static async Task<(int ExitCode, string Stdout, string Stderr)> RunApplicationAsync(
        IReadOnlyDictionary<string, string> environment, IReadOnlyList<string> arguments)
    {
        var appDll = FindApplicationDll();
        var startInfo = new ProcessStartInfo("dotnet")
        {
            ArgumentList = { appDll },
            WorkingDirectory = Path.GetDirectoryName(appDll)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
            UseShellExecute = false,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        startInfo.Environment["Urls"] = "http://127.0.0.1:0";
        foreach (var (key, value) in environment)
        {
            startInfo.Environment[key] = value;
        }

        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync(Ct);
        var stderr = process.StandardError.ReadToEndAsync(Ct);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(60));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        return (process.ExitCode, await stdout, await stderr);
    }

    private WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(postgres.ConfigurationOverrides)));

    private static string FindApplicationDll()
    {
        // tests/RejestrCzasu.Tests/bin/<Configuration>/<tfm>/ → src/RejestrCzasu/bin/<Configuration>/<tfm>/RejestrCzasu.dll
        var testDir = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        var tfm = testDir.Name;
        var configuration = testDir.Parent!.Name;
        var root = testDir;
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "RejestrCzasu.sln")))
        {
            root = root.Parent;
        }
        Assert.NotNull(root);
        var dll = Path.Combine(root.FullName, "src", "RejestrCzasu", "bin", configuration, tfm, "RejestrCzasu.dll");
        Assert.True(File.Exists(dll), $"Nie znaleziono aplikacji: {dll}");
        return dll;
    }
}
