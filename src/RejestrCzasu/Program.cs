using RejestrCzasu;
using RejestrCzasu.Accounts;
using RejestrCzasu.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Features;
using RejestrCzasu.Features.Zestawienie;
using RejestrCzasu.Options;
using RejestrCzasu.Shared;
using RejestrCzasu.Startup;
using RejestrCzasu.Ui;

var builder = WebApplication.CreateBuilder(args);

// Local, git-ignored configuration (AD-13): database password and port.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
// Environment variables and command line keep precedence over the local file.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services.AddOptions<DatabaseOptions>().Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddOptions<FirstAdminOptions>().Bind(builder.Configuration.GetSection(FirstAdminOptions.SectionName));
builder.Services.AddOptions<ProxyOptions>().Bind(builder.Configuration.GetSection(ProxyOptions.SectionName));
// A top-level key (APP_IMAGE_TAG, set by the Dockerfile and the server's .env), not a section (AD-13, AD-20).
builder.Services.AddOptions<AppImageOptions>().Configure<IConfiguration>((options, configuration) =>
{
    var tag = configuration[AppImageOptions.TagKey];
    if (!string.IsNullOrWhiteSpace(tag))
    {
        options.Tag = tag.Trim();
    }
});

// System mode (Startup, Identity, Data Protection) and User mode (UserDbContextFactory), without pooling (AD-16).
builder.Services.AddAppDatabase();

// Data Protection keys in the database with a fixed application name: a restart does not log anybody out (AD-19).
builder.Services.AddAppDataProtection();

// Client address and scheme from Caddy, trusted only from ForwardedHeaders:KnownNetwork and loopback (AD-19).
builder.Services.AddAppForwardedHeaders();

// GET /health: database state and image tag (AD-20).
builder.Services.AddAppHealthChecks();

// Identity, login cookie, authorization, CurrentUser and the login/registration rate limit (AD-15, AD-16).
builder.Services.AddAccounts();

builder.Services.AddSingleton(TimeProvider.System);

// Startup sequence (AD-21), run below before app.RunAsync().
builder.Services.AddStartupSequence();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// The eod-ui copy (AD-6, AD-11, AD-23): Fluent UI 5 with the Polish texts of PolishLocalizer, UiMessages and
// AppearanceSettings; the providers are rendered by EodShell in MainLayout.
builder.Services.AddEodPlatformUI();
// The data of the screens (AD-4, AD-10), one instance per circuit.
builder.Services.AddAppScreens();

var app = builder.Build();

// A wrong ForwardedHeaders:KnownNetwork ends the start with a Polish message, before the database and the server.
try
{
    app.Services.EnsureForwardedHeadersConfiguration();
}
catch (ProxyConfigurationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

// First in the pipeline: everything after it (CSP, authentication, the rate limiter) sees the client address and
// scheme sent by Caddy, never headers from outside the trusted networks (AD-15, AD-19).
app.UseForwardedHeaders();

app.Use(async (context, next) =>
{
    // OnStarting: the exception handler clears headers before re-executing /Error.
    context.Response.OnStarting(() =>
    {
        context.Response.Headers.ContentSecurityPolicy = SecurityHeaders.ContentSecurityPolicy;
        return Task.CompletedTask;
    });
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
// pl-PL with the short date dd.MM.yyyy for threads and every request (AD-6).
app.UseEodPolishCulture();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
// A required password change (must_change_password) redirects every page and endpoint to /konto/zmiana-hasla (AD-15).
app.UsePasswordChangeRedirect();
// After UseRouting: the limiter reads [EnableRateLimiting] of the selected endpoint.
app.UseRateLimiter();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    // frame-ancestors is already part of our CSP header; avoid a second CSP header from Blazor.
    .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = null);
app.MapAccountEndpoints();
app.MapSummaryEndpoint();
app.MapAppHealth();

// Startup sequence (AD-21) before the server starts: connection check, migrations, first Administrator.
try
{
    await app.Services.GetRequiredService<DatabaseStartup>().RunAsync();
}
catch (DatabaseStartupException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

await app.RunAsync();
return 0;

public partial class Program;
