using Microsoft.FluentUI.AspNetCore.Components;

namespace RejestrCzasu.Ui;

/// <summary>Stan terminu sprawy wyliczany z liczby dni do terminu (specyfikacja UI, sekcja 14).</summary>
/// <remarks>Wylicza go <see cref="Statuses.GetDueState"/>; etykietę i kolor znaczka dają <see cref="Statuses.Label"/> i <see cref="Statuses.Color(DueState)"/>.</remarks>
public enum DueState
{
    /// <summary>Więcej niż 3 dni do terminu – znaczek „W toku” w kolorze marki.</summary>
    OnTrack,

    /// <summary>Od 0 do 3 dni do terminu – znaczek „Termin blisko” w kolorze ostrzeżenia.</summary>
    DueSoon,

    /// <summary>Termin minął – znaczek „Po terminie” w kolorze zagrożenia.</summary>
    Overdue,
}

/// <summary>
/// Mapa kolorów znaczków statusu (FluentBadge Appearance="Tint"). Kolory statusów są zarezerwowane dla statusów.
/// </summary>
/// <remarks>
/// Specyfikacja UI, sekcja 14. Status nigdy nie jest pokazywany samym kolorem – znaczek zawsze ma tekst (sekcja 16).
/// NIE WOLNO używać <c>Appearance="Outline"</c> z kolorem (błąd biblioteki: tekst w kolorze tła).
/// </remarks>
/// <example>
/// <code>
/// &lt;FluentBadge Appearance="BadgeAppearance.Tint" Color="@Statuses.Color(item.Status)"&gt;@item.Status&lt;/FluentBadge&gt;
/// </code>
/// </example>
public static class Statuses
{
    /// <summary>Próg „termin blisko” w dniach: termin za tyle dni lub mniej (ale jeszcze nie miniony) to <see cref="DueState.DueSoon"/>.</summary>
    public const int DueSoonThresholdDays = 3;

    /// <summary>Wylicza stan terminu sprawy.</summary>
    /// <param name="daysToDue">Liczba dni do terminu; ujemna – termin minął, 0 – termin dziś.</param>
    /// <returns><see cref="DueState.Overdue"/>, <see cref="DueState.DueSoon"/> (0–<see cref="DueSoonThresholdDays"/> dni) albo <see cref="DueState.OnTrack"/>.</returns>
    public static DueState GetDueState(int daysToDue) => daysToDue switch
    {
        < 0 => DueState.Overdue,
        <= DueSoonThresholdDays => DueState.DueSoon,
        _ => DueState.OnTrack,
    };

    /// <summary>Tekst znaczka dla stanu terminu.</summary>
    /// <param name="state">Stan terminu.</param>
    /// <returns>„Po terminie”, „Termin blisko” albo „W toku”.</returns>
    public static string Label(DueState state) => state switch
    {
        DueState.Overdue => "Po terminie",
        DueState.DueSoon => "Termin blisko",
        _ => "W toku",
    };

    /// <summary>Kolor znaczka dla stanu terminu.</summary>
    /// <param name="state">Stan terminu.</param>
    /// <returns><see cref="BadgeColor.Danger"/>, <see cref="BadgeColor.Warning"/> albo <see cref="BadgeColor.Brand"/>.</returns>
    public static BadgeColor Color(DueState state) => state switch
    {
        DueState.Overdue => BadgeColor.Danger,
        DueState.DueSoon => BadgeColor.Warning,
        _ => BadgeColor.Brand,
    };

    /// <summary>Domyślne kolory dla wartości statusów używanych we wzorcu. Nieznana wartość = Informative.</summary>
    /// <remarks>Klucze bez rozróżniania wielkości liter. Kolumna raportu z innymi wartościami podaje własną mapę.</remarks>
    public static readonly IReadOnlyDictionary<string, BadgeColor> DefaultColors = new Dictionary<string, BadgeColor>(StringComparer.OrdinalIgnoreCase)
    {
        // w toku / etap
        ["W toku"] = BadgeColor.Brand,
        ["Do uzupełnienia"] = BadgeColor.Brand,
        ["Nowa"] = BadgeColor.Brand,          // wiersz tabeli edycyjnej dodany, niezapisany
        ["Zmieniona"] = BadgeColor.Brand,     // wiersz tabeli edycyjnej zmieniony, niezapisany
        // termin blisko / oczekuje / ostrzeżenie
        ["Termin blisko"] = BadgeColor.Warning,
        ["Oczekuje"] = BadgeColor.Warning,
        ["Zaproszony"] = BadgeColor.Warning,  // konto użytkownika: zaproszenie wysłane, bez pierwszego logowania
        // po terminie / odrzucono / wymagany
        ["Po terminie"] = BadgeColor.Danger,
        ["Odrzucono"] = BadgeColor.Danger,
        ["Odrzucony"] = BadgeColor.Danger,
        ["Wymagany"] = BadgeColor.Danger,
        ["Zablokowany"] = BadgeColor.Danger,  // konto użytkownika: logowanie zablokowane
        // wynik pozytywny / aktywne
        ["Zatrudniono"] = BadgeColor.Success,
        ["Zaakceptowano"] = BadgeColor.Success,
        ["Zaakceptowany"] = BadgeColor.Success,
        ["Wdrożenie zakończone"] = BadgeColor.Success,
        ["Aktywne"] = BadgeColor.Success,
        ["Aktywny"] = BadgeColor.Success,     // konto użytkownika
        // neutralne zakończenie / informacja (także sprawa jeszcze poza obiegiem)
        ["Szkic"] = BadgeColor.Informative,
        ["Anulowano"] = BadgeColor.Informative,
        ["Przerwano"] = BadgeColor.Informative,
        ["Zaplanowane"] = BadgeColor.Informative,
        ["Oczekuje na opis"] = BadgeColor.Informative,
        ["Zwrócono do poprawy"] = BadgeColor.Informative,
        // wygaszone
        ["Zakończone"] = BadgeColor.Subtle,
        ["Tylko do odczytu"] = BadgeColor.Subtle,
    };

    /// <summary>Kolor znaczka dla wartości statusu (tekstu znaczka).</summary>
    /// <param name="value">Wartość statusu, np. „Zatrudniono”.</param>
    /// <param name="colors">Własna mapa kolorów; domyślnie <see cref="DefaultColors"/>.</param>
    /// <returns>Kolor z mapy albo <see cref="BadgeColor.Informative"/>, gdy wartości nie ma w mapie lub jest pusta.</returns>
    public static BadgeColor Color(string? value, IReadOnlyDictionary<string, BadgeColor>? colors = null)
        => value is not null && (colors ?? DefaultColors).TryGetValue(value, out var color) ? color : BadgeColor.Informative;
}
