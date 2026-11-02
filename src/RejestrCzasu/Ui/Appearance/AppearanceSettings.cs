using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RejestrCzasu.Ui;

/// <summary>Rozmiar interfejsu (jedna skala dla tekstu, kontrolek i odstępów – CSS zoom).</summary>
/// <remarks>Specyfikacja UI, sekcje 6 i 15. Punkty łamania FluentGrid liczą się od rzeczywistej szerokości okna, nie od skali.</remarks>
public enum UiScale
{
    /// <summary>Wg szerokości okna: &lt; 1440 px → 90%, 1440–2199 px → 100%, ≥ 2200 px → 110%.</summary>
    Auto,

    /// <summary>Stała skala 90%.</summary>
    Percent90,

    /// <summary>Stała skala 100%.</summary>
    Percent100,

    /// <summary>Stała skala 110%.</summary>
    Percent110,

    /// <summary>Stała skala 125% – dla osób, które potrzebują większego tekstu.</summary>
    Percent125,
}

/// <summary>Motyw interfejsu (tokeny Fluent 2 jasne albo ciemne).</summary>
public enum UiTheme
{
    /// <summary>Jak w systemie operacyjnym – motyw zmienia się razem z ustawieniem systemu.</summary>
    System,

    /// <summary>Zawsze jasny.</summary>
    Light,

    /// <summary>Zawsze ciemny.</summary>
    Dark,
}

/// <summary>
/// Ustawienia wyglądu użytkownika: skala interfejsu (atrybut <c>data-scale</c> na &lt;html&gt;, pamiętana w przeglądarce)
/// i motyw (IThemeService biblioteki Fluent UI). Docelowo oba ustawienia trafią do profilu użytkownika.
/// </summary>
/// <remarks>
/// Usługa o zasięgu połączenia (scoped), rejestrowana przez <see cref="ServiceCollectionExtensions.AddEodPlatformUI"/>.
/// Korzysta z JS (<c>eod.js</c>, <c>window.EodUI</c>), więc metody wywołuje się dopiero po pierwszym renderowaniu
/// interaktywnym (<c>OnAfterRenderAsync</c>), nie w czasie prerenderowania. Zapisaną skalę stosuje przed pierwszym
/// renderowaniem skrypt z <c>EodHead</c>.
/// </remarks>
/// <example>
/// <code>
/// @inject AppearanceSettings Appearance
/// …
/// _scale = await Appearance.GetScaleAsync();
/// await Appearance.SetThemeAsync(UiTheme.Dark);
/// </code>
/// </example>
/// <param name="js">Środowisko JS przeglądarki (funkcje <c>EodUI.*</c>).</param>
/// <param name="themeService">Usługa motywów Fluent UI – stosuje tokeny jasne / ciemne.</param>
public sealed class AppearanceSettings(IJSRuntime js, IThemeService themeService)
{
    /// <summary>Wczytuje skalę zapisaną w przeglądarce.</summary>
    /// <returns>Zapisana skala albo <see cref="UiScale.Auto"/>, gdy użytkownik jej nie wybrał.</returns>
    public async Task<UiScale> GetScaleAsync()
        => await js.InvokeAsync<string>("EodUI.getScale") switch
        {
            "90" => UiScale.Percent90,
            "100" => UiScale.Percent100,
            "110" => UiScale.Percent110,
            "125" => UiScale.Percent125,
            _ => UiScale.Auto,
        };

    /// <summary>Ustawia skalę od razu (atrybut <c>data-scale</c> na &lt;html&gt;) i zapamiętuje ją w przeglądarce.</summary>
    /// <param name="scale">Nowa skala; <see cref="UiScale.Auto"/> usuwa wybór ręczny.</param>
    /// <returns>Zadanie zakończone po zmianie skali.</returns>
    public async Task SetScaleAsync(UiScale scale)
        => await js.InvokeVoidAsync("EodUI.setScale", scale switch
        {
            UiScale.Percent90 => "90",
            UiScale.Percent100 => "100",
            UiScale.Percent110 => "110",
            UiScale.Percent125 => "125",
            _ => "auto",
        });

    /// <summary>Opis bieżącej skali, np. „Okno przeglądarki: 1920 × 1080 px. Tryb automatyczny ustawia teraz 100%.”</summary>
    /// <returns>Tekst do wyświetlenia pod wyborem skali w ustawieniach użytkownika.</returns>
    public async Task<string> DescribeScaleAsync()
    {
        var window = await js.InvokeAsync<BrowserWindow>("EodUI.describeWindow");
        return $"Okno przeglądarki: {window.Width} × {window.Height} px. "
             + (window.Manual ? $"Wybrano ręcznie {window.Scale}%." : $"Tryb automatyczny ustawia teraz {window.Scale}%.");
    }

    /// <summary>Wczytuje motyw zapamiętany w przeglądarce.</summary>
    /// <returns>Zapamiętany motyw albo <see cref="UiTheme.System"/>, gdy użytkownik go nie wybrał.</returns>
    public async Task<UiTheme> GetThemeAsync()
        => await js.InvokeAsync<string>("EodUI.getTheme") switch
        {
            "dark" => UiTheme.Dark,
            "light" => UiTheme.Light,
            _ => UiTheme.System,
        };

    /// <summary>Ustawia motyw (IThemeService biblioteki stosuje tokeny jasne/ciemne) i zapamiętuje wybór.</summary>
    /// <param name="theme">Nowy motyw.</param>
    /// <returns>Zadanie zakończone po zmianie motywu.</returns>
    public async Task SetThemeAsync(UiTheme theme)
    {
        var (mode, code) = theme switch
        {
            UiTheme.Dark => (ThemeMode.Dark, "dark"),
            UiTheme.Light => (ThemeMode.Light, "light"),
            _ => (ThemeMode.System, "system"),
        };
        await themeService.SetThemeAsync(mode);
        await js.InvokeVoidAsync("EodUI.saveTheme", code);
    }

    /// <summary>Wymiary okna i bieżąca skala zwracane przez <c>EodUI.describeWindow</c>.</summary>
    /// <param name="Width">Szerokość okna w pikselach CSS.</param>
    /// <param name="Height">Wysokość okna w pikselach CSS.</param>
    /// <param name="Scale">Skala w procentach (np. 90, 100).</param>
    /// <param name="Manual"><c>true</c>, gdy skalę wybrał użytkownik (atrybut <c>data-scale</c>).</param>
    internal sealed record BrowserWindow(int Width, int Height, int Scale, bool Manual);
}
