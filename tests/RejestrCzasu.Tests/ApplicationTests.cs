using System.Diagnostics;
using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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
    public async Task HomePage_Returns200_WithContentSecurityPolicy()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(postgres.ConfigurationOverrides)));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/", Ct);

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
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        startInfo.Environment["Urls"] = "http://127.0.0.1:0";
        startInfo.Environment["Database__Host"] = "127.0.0.1";
        startInfo.Environment["Database__Port"] = "1";
        startInfo.Environment["Database__Password"] = "unused";

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

        Assert.Equal(1, process.ExitCode);
        // ASCII part only: the console code page of the child process is not necessarily UTF-8.
        Assert.Contains("uruchom Docker Desktop i `docker compose up -d`", await stderr);
        Assert.DoesNotContain("Now listening", await stdout);
    }

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
