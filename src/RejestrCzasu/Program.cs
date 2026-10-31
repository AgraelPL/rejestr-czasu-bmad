using System.Globalization;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu;
using RejestrCzasu.Accounts;
using RejestrCzasu.Components;
using RejestrCzasu.Data;
using RejestrCzasu.Localization;
using RejestrCzasu.Options;
using RejestrCzasu.Startup;

var polish = CultureInfo.GetCultureInfo("pl-PL");
CultureInfo.DefaultThreadCurrentCulture = polish;
CultureInfo.DefaultThreadCurrentUICulture = polish;

var builder = WebApplication.CreateBuilder(args);

// Local, git-ignored configuration (AD-13): database password and port.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
// Environment variables and command line keep precedence over the local file.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services.AddOptions<DatabaseOptions>().Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddOptions<FirstAdminOptions>().Bind(builder.Configuration.GetSection(FirstAdminOptions.SectionName));

// System mode (Startup, Identity, Data Protection) and User mode (UserDbContextFactory), without pooling (AD-16).
builder.Services.AddAppDatabase();

// Identity, login cookie, authorization, CurrentUser and the login/registration rate limit (AD-15, AD-16).
builder.Services.AddAccounts();

builder.Services.AddSingleton(TimeProvider.System);

// Startup sequence (AD-21), run below before app.RunAsync().
builder.Services.AddStartupSequence();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Fluent UI 5: components and services (incl. IDialogService); providers rendered by <FluentProviders /> in MainLayout.
// Polish texts of the Fluent UI components (AD-6).
builder.Services.AddFluentUIComponents(options => options.Localizer = new FluentLocalizer());

var app = builder.Build();

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
app.UseRequestLocalization(polish.Name);
// Story 4.5 puts UseForwardedHeaders before this point, so the rate limiter sees the client address (AD-15, AD-19).
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
