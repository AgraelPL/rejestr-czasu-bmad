using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using RejestrCzasu;
using RejestrCzasu.Components;
using RejestrCzasu.Data;
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

builder.Services.AddDbContextFactory<AppDbContext>((services, options) =>
    options
        .UseNpgsql(services.GetRequiredService<IOptions<DatabaseOptions>>().Value.BuildConnectionString())
        .UseSnakeCaseNamingConvention());
// System mode is the plain factory above (Startup, Identity, Data Protection); User mode only through this one (AD-16).
builder.Services.AddScoped<UserDbContextFactory>();

builder.Services.AddSingleton(TimeProvider.System);

// Startup sequence (AD-21), run below before app.RunAsync().
builder.Services.AddTransient<DatabaseStartup>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Fluent UI 5: components and services (incl. IDialogService); providers rendered by <FluentProviders /> in MainLayout.
builder.Services.AddFluentUIComponents();

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
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    // frame-ancestors is already part of our CSP header; avoid a second CSP header from Blazor.
    .AddInteractiveServerRenderMode(options => options.ContentSecurityFrameAncestorsPolicy = null);

// Startup sequence (AD-21) before the server starts: connection check, migrations.
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
