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

// Local, git-ignored configuration (AD-13): password, backup directory, export prefix.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
// Environment variables and command line keep precedence over the local file.
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddCommandLine(args);

builder.Services.AddOptions<DatabaseOptions>().Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.AddOptions<BackupOptions>().Bind(builder.Configuration.GetSection(BackupOptions.SectionName));
builder.Services.AddOptions<ExportOptions>().Bind(builder.Configuration.GetSection(ExportOptions.SectionName));

builder.Services.AddDbContextFactory<AppDbContext>((services, options) =>
    options
        .UseNpgsql(services.GetRequiredService<IOptions<DatabaseOptions>>().Value.BuildConnectionString())
        .UseSnakeCaseNamingConvention());

builder.Services.AddSingleton(TimeProvider.System);

// Startup sequence (AD-3), run below before app.RunAsync().
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

// Startup sequence (AD-3) before the server starts: connection check, [backup], migrations.
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
