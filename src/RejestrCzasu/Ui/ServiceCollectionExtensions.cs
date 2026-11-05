using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

namespace RejestrCzasu.Ui;

/// <summary>
/// Podłączenie kopii wzorca UI eod-ui do aplikacji: rejestracja usług biblioteki i polska kultura interfejsu.
/// Aplikacja wywołuje obie metody raz, w <c>Program.cs</c>.
/// </summary>
/// <remarks>Specyfikacja UI, sekcje 3.1 (pułapki Fluent UI Blazor v5) i 18 (biblioteka – komponenty i usługi).</remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Rejestruje Fluent UI Blazor v5 (z polskimi tekstami komponentów) i usługi wzorca UI:
    /// <see cref="UiMessages"/>, <see cref="AppearanceSettings"/>, <see cref="ReportLayoutStore"/>.
    /// </summary>
    /// <param name="services">Kolekcja usług aplikacji.</param>
    /// <returns>Ta sama kolekcja <paramref name="services"/> – do łączenia wywołań.</returns>
    /// <remarks>
    /// Ustawia <see cref="PolishLocalizer"/> jako lokalizator komponentów Fluent i wyłącza domyślną klasę pola
    /// <c>my-3-o</c> – odstępy między polami daje układ (specyfikacja UI, sekcja 3.1).
    /// Aplikacja nie wywołuje już osobno <c>AddFluentUIComponents</c>.
    /// </remarks>
    /// <example>
    /// <code>builder.Services.AddEodPlatformUI();</code>
    /// W App.razor w &lt;head&gt;: <c>&lt;EodHead /&gt;</c>; layout: <c>&lt;EodShell&gt;</c> (zawiera FluentProviders).
    /// </example>
    public static IServiceCollection AddEodPlatformUI(this IServiceCollection services)
    {
        services.AddFluentUIComponents(options =>
        {
            options.Localizer = new PolishLocalizer();

            // Fluent v5 dodaje każdemu polu z etykietą klasę „my-3-o” (12 px marginesu nad i pod polem).
            // Odstępy między polami daje układ (.eod-stack, FluentGrid) – margines pola je podwajał.
            options.DefaultStyles.FluentFieldClass = null;
        });
        services.AddScoped<UiMessages>();
        services.AddScoped<AppearanceSettings>();
        services.AddScoped<ReportLayoutStore>();
        return services;
    }

    /// <summary>
    /// Interfejs po polsku: kultura <see cref="Formats.Culture"/> (daty „dd.MM.yyyy”, liczby „12 000,00”, tydzień od poniedziałku)
    /// dla wątków i każdego żądania – także w FluentDatePicker / FluentCalendar i w połączeniu Interactive Server.
    /// </summary>
    /// <param name="app">Potok żądań aplikacji.</param>
    /// <returns>Ten sam obiekt <paramref name="app"/> – do łączenia wywołań.</returns>
    /// <remarks>
    /// Język interfejsu nie zależy od ustawień przeglądarki – wzorzec ma tylko wersję polską.
    /// Bez tej metody FluentDatePicker na Linuksie pokazuje „9.10.2026” zamiast „09.10.2026” (specyfikacja UI, sekcja 3.1).
    /// </remarks>
    /// <example><code>app.UseEodPolishCulture();</code> (przed MapRazorComponents)</example>
    public static IApplicationBuilder UseEodPolishCulture(this IApplicationBuilder app)
    {
        CultureInfo.DefaultThreadCurrentCulture = Formats.Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Formats.Culture;

        return app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(Formats.Culture),
            SupportedCultures = [Formats.Culture],
            SupportedUICultures = [Formats.Culture],
            // Język interfejsu nie zależy od przeglądarki (wzorzec ma tylko wersję polską)
            RequestCultureProviders = [],
        });
    }
}
