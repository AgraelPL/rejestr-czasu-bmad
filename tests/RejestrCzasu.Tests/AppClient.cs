using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RejestrCzasu.Data;

namespace RejestrCzasu.Tests;

/// <summary>
/// The whole application on the container database with an HTTP client that keeps cookies (login, antiforgery) and
/// does not follow redirects. One instance per test: the rate limiter state lives in the application.
/// </summary>
public sealed partial class AppClient : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    /// <param name="configuration">Extra configuration on top of <see cref="PostgresFixture.ConfigurationOverrides"/>.</param>
    /// <param name="configure">Extra host setup, e.g. an <c>IStartupFilter</c> that sets the connection address.</param>
    public AppClient(
        PostgresFixture postgres,
        IReadOnlyDictionary<string, string?>? configuration = null,
        Action<IWebHostBuilder>? configure = null)
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(postgres.ConfigurationOverrides);
                if (configuration is not null)
                {
                    config.AddInMemoryCollection(configuration);
                }
            });
            configure?.Invoke(builder);
        });
        Http = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
    }

    public HttpClient Http { get; }

    public IServiceProvider Services => _factory.Services;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>Creates an Account through Identity (hashed password, role User) in the given state.</summary>
    public async Task<int> CreateAccountAsync(string userName, string password, AccountStatus status, bool mustChangePassword = false)
    {
        await using var scope = Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            UserName = userName,
            Status = status,
            RegisteredAt = DateTime.UtcNow,
            MustChangePassword = mustChangePassword,
        };
        AssertSucceeded(await userManager.CreateAsync(user, password));
        AssertSucceeded(await userManager.AddToRoleAsync(user, AppRoles.User));
        return user.Id;
    }

    /// <summary>GET of a page and its hidden form fields (antiforgery token, Blazor form handler).</summary>
    public async Task<(string Html, Dictionary<string, string> HiddenFields)> GetPageAsync(string path)
    {
        using var response = await Http.GetAsync(path, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync(Ct);
        // Text decoded for assertions (Blazor encodes Polish letters as entities); fields parsed from the markup.
        return (WebUtility.HtmlDecode(html), HiddenFields(html));
    }

    /// <summary>POST of the page's form with its hidden fields and the given values.</summary>
    public async Task<HttpResponseMessage> SubmitFormAsync(string path, IReadOnlyDictionary<string, string> values)
    {
        var (_, fields) = await GetPageAsync(path);
        foreach (var (name, value) in values)
        {
            fields[name] = value;
        }
        return await Http.PostAsync(path, new FormUrlEncodedContent(fields), Ct);
    }

    public Task<HttpResponseMessage> LoginAsync(string userName, string password, string path = "/konto/logowanie") =>
        SubmitFormAsync(path, new Dictionary<string, string>
        {
            ["Input.UserName"] = userName,
            ["Input.Password"] = password,
        });

    public Task<HttpResponseMessage> RegisterAsync(string userName, string password, string? confirmPassword = null) =>
        SubmitFormAsync("/konto/rejestracja", new Dictionary<string, string>
        {
            ["Input.UserName"] = userName,
            ["Input.Password"] = password,
            ["Input.ConfirmPassword"] = confirmPassword ?? password,
        });

    public static Dictionary<string, string> HiddenFields(string html)
    {
        var fields = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match input in HiddenInputRegex().Matches(html))
        {
            var name = NameRegex().Match(input.Value);
            var value = ValueRegex().Match(input.Value);
            if (name.Success)
            {
                fields[WebUtility.HtmlDecode(name.Groups[1].Value)] =
                    value.Success ? WebUtility.HtmlDecode(value.Groups[1].Value) : "";
            }
        }
        return fields;
    }

    public async ValueTask DisposeAsync()
    {
        Http.Dispose();
        await _factory.DisposeAsync();
    }

    private static void AssertSucceeded(IdentityResult result) =>
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));

    [GeneratedRegex("<input[^>]*type=\"hidden\"[^>]*>")]
    private static partial Regex HiddenInputRegex();

    [GeneratedRegex("name=\"([^\"]*)\"")]
    private static partial Regex NameRegex();

    [GeneratedRegex("value=\"([^\"]*)\"")]
    private static partial Regex ValueRegex();
}
