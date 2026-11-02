using System.Globalization;

namespace RejestrCzasu.Ui;

/// <summary>
/// Formaty wyświetlania zgodne ze specyfikacją UI (sekcja 5): daty dd.mm.rrrr, kwoty „12 000,00 zł”,
/// terminy względne („jutro”, „za 4 dni”, „2 dni po terminie”).
/// </summary>
/// <remarks>
/// Ekrany formatują daty, kwoty i liczby wyłącznie przez tę klasę – nigdy własnym <c>ToString("…")</c>,
/// żeby ten sam rodzaj wartości wyglądał tak samo w całej aplikacji.
/// </remarks>
/// <example>
/// <code>
/// Formats.Date(new DateOnly(2026, 10, 8));   // „08.10.2026”
/// Formats.Amount(12000m);                     // „12 000,00 zł”
/// Formats.RelativeDueDate(-2);                // „2 dni po terminie”
/// </code>
/// </example>
public static class Formats
{
    /// <summary>
    /// Kultura interfejsu: polski z krótką datą „dd.MM.yyyy” i godziną „HH:mm”.
    /// Dane ICU dla pl-PL (Linux) mają krótką datę „d.MM.yyyy”, przez co FluentDatePicker pokazywał „9.10.2026”.
    /// Ustawiana jako kultura żądań i wątków przez <see cref="ServiceCollectionExtensions.UseEodPolishCulture"/>.
    /// </summary>
    /// <remarks>Obiekt tylko do odczytu – wspólny dla wszystkich żądań.</remarks>
    public static readonly CultureInfo Culture = CreateCulture();

    /// <summary>Data w formacie „dd.mm.rrrr”, np. „08.10.2026”.</summary>
    /// <param name="date">Data do wyświetlenia.</param>
    /// <returns>Data z zerami wiodącymi w dniu i miesiącu.</returns>
    public static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", Culture);

    /// <summary>Data (bez godziny) w formacie „dd.mm.rrrr”, np. „08.10.2026”.</summary>
    /// <param name="date">Data i godzina; godzina jest pomijana.</param>
    /// <returns>Data z zerami wiodącymi w dniu i miesiącu.</returns>
    public static string Date(DateTime date) => date.ToString("dd.MM.yyyy", Culture);

    /// <summary>Data z godziną w formacie „dd.mm.rrrr gg:mm”, np. „08.10.2026 14:05” (komentarze, załączniki, historia).</summary>
    /// <param name="dateTime">Data i godzina do wyświetlenia.</param>
    /// <returns>Data i godzina w zapisie 24-godzinnym.</returns>
    public static string DateAndTime(DateTime dateTime) => dateTime.ToString("dd.MM.yyyy HH:mm", Culture);

    /// <summary>„12 000,00 zł” – w tekście i tabelach. W polu kwoty walutę pokazuje sufiks „PLN”.</summary>
    /// <param name="amount">Kwota w złotych.</param>
    /// <returns>Kwota z dwoma miejscami po przecinku, separatorem tysięcy i skrótem „zł”.</returns>
    public static string Amount(decimal amount) => amount.ToString("N2", Culture) + " zł";

    /// <summary>Liczba z separatorem tysięcy (spacja niełamliwa), np. „14 000” albo „14 000,00”.</summary>
    /// <param name="value">Liczba do wyświetlenia.</param>
    /// <param name="decimals">Liczba miejsc po przecinku; domyślnie 0.</param>
    /// <returns>Liczba sformatowana w kulturze <see cref="Culture"/>.</returns>
    public static string Number(decimal value, int decimals = 0) => value.ToString("N" + decimals, Culture);

    /// <summary>„45%” – bez spacji, najwyżej dwa miejsca po przecinku („12,5%”).</summary>
    /// <param name="value">Wartość w procentach (45 = 45%, nie 0,45).</param>
    /// <returns>Wartość ze znakiem „%”.</returns>
    public static string Percent(decimal value) => value.ToString("0.##", Culture) + "%";

    /// <summary>Termin względem dnia dzisiejszego: „dziś”, „jutro”, „za 4 dni”, „2 dni po terminie”.</summary>
    /// <param name="daysToDue">Liczba dni do terminu; ujemna – termin minął, 0 – termin dziś.</param>
    /// <returns>Opis terminu do wyświetlenia obok daty (np. w <c>EodDueDate</c>).</returns>
    public static string RelativeDueDate(int daysToDue) => daysToDue switch
    {
        < 0 => $"{PolishPlural.WithCount(-daysToDue, "dzień", "dni", "dni")} po terminie",
        0 => "dziś",
        1 => "jutro",
        _ => $"za {daysToDue} dni",
    };

    /// <summary>Dzień tygodnia i data słownie, np. „Czwartek, 8 października 2026” (nagłówek strony startowej).</summary>
    /// <param name="date">Data do wyświetlenia.</param>
    /// <returns>Opis daty z wielką literą na początku.</returns>
    public static string DayAndDate(DateOnly date)
    {
        var text = date.ToString("dddd, d MMMM yyyy", Culture);
        return char.ToUpper(text[0], Culture) + text[1..];
    }

    private static CultureInfo CreateCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("pl-PL").Clone();
        culture.DateTimeFormat.ShortDatePattern = "dd.MM.yyyy";
        culture.DateTimeFormat.ShortTimePattern = "HH:mm";
        return CultureInfo.ReadOnly(culture);
    }
}

/// <summary>Odmiana rzeczowników po liczebnikach: 1 sprawa, 2–4 sprawy (bez 12–14), 5 spraw.</summary>
/// <remarks>Specyfikacja UI, sekcja 5. Formy podaje się zawsze w kolejności: jeden, kilka (2–4), wiele (0, 5+, 12–14).</remarks>
/// <example>
/// <code>
/// PolishPlural.WithCount(3, "sprawa", "sprawy", "spraw");       // „3 sprawy”
/// PolishPlural.Form(12, "Został", "Zostały", "Zostało");        // „Zostało”
/// </code>
/// </example>
public static class PolishPlural
{
    /// <summary>Wybiera formę rzeczownika pasującą do liczby (bez samej liczby).</summary>
    /// <param name="n">Liczba; znak jest pomijany.</param>
    /// <param name="one">Forma dla 1, np. „sprawa”.</param>
    /// <param name="few">Forma dla 2–4, 22–24…, np. „sprawy”.</param>
    /// <param name="many">Forma dla 0, 5–21, 25–31…, np. „spraw”.</param>
    /// <returns>Jedna z form <paramref name="one"/>, <paramref name="few"/>, <paramref name="many"/>.</returns>
    public static string Form(long n, string one, string few, string many)
    {
        n = Math.Abs(n);
        if (n == 1)
        {
            return one;
        }

        var lastDigit = n % 10;
        var lastTwoDigits = n % 100;
        return lastDigit is >= 2 and <= 4 && lastTwoDigits is < 12 or > 14 ? few : many;
    }

    /// <summary>Liczba z odmienionym rzeczownikiem, np. „5 spraw”.</summary>
    /// <param name="n">Liczba wyświetlana przed rzeczownikiem.</param>
    /// <param name="one">Forma dla 1, np. „sprawa”.</param>
    /// <param name="few">Forma dla 2–4, 22–24…, np. „sprawy”.</param>
    /// <param name="many">Forma dla 0, 5–21, 25–31…, np. „spraw”.</param>
    /// <returns>Tekst „{liczba} {forma}”.</returns>
    public static string WithCount(long n, string one, string few, string many) => $"{n} {Form(n, one, few, many)}";
}
