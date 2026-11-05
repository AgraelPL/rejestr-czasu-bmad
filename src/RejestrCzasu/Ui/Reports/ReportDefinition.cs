using Microsoft.FluentUI.AspNetCore.Components;

namespace RejestrCzasu.Ui;

/// <summary>
/// Typ kolumny raportu – decyduje o rodzaju filtra w okienku kolumny, o porządku wartości przy sortowaniu
/// i o formacie wyświetlania.
/// </summary>
/// <remarks>Specyfikacja UI, sekcja 9.7.</remarks>
public enum ColumnType
{
    /// <summary>Tekst: filtr „zawiera”, porządek alfabetyczny (kultura polska).</summary>
    Text,

    /// <summary>
    /// Lista wartości: filtr z polami wyboru i licznikami wierszy; porządek wg <see cref="ReportColumn.ValueOrder"/>,
    /// a gdy go brak – alfabetyczny.
    /// </summary>
    List,

    /// <summary>Liczba (<see cref="decimal"/> lub <see cref="int"/>): filtr zakresu Od – Do, wartości wyrównane do prawej.</summary>
    Number,

    /// <summary>Data (<see cref="DateOnly"/>): filtr okresu Od – Do, wyświetlanie w formacie dd.mm.rrrr.</summary>
    Date,
}

/// <summary>
/// Kolumna raportu: klucz wartości w wierszu, nagłówek i typ, a we właściwościach inicjowanych – sposób prezentacji
/// (link, znaczek statusu, kwota, procent) i zachowanie w układzie domyślnym.
/// </summary>
/// <param name="Key">Klucz wartości w słowniku wiersza (niewidoczny dla użytkownika, np. <c>"stage"</c>).</param>
/// <param name="Label">
/// Nagłówek kolumny – krótki („Start pracy”, „Budżet mies.”), żeby układ domyślny mieścił się w szerokości 1920 px.
/// </param>
/// <param name="Type">Typ kolumny; domyślnie <see cref="ColumnType.Text"/>.</param>
/// <example>
/// <code>
/// new ReportColumn("id", "Numer sprawy") { IsKey = true, IsLink = true },
/// new ReportColumn("stage", "Etap", ColumnType.List) { ValueOrder = ["Zgłoszenie", "Opis stanowiska"] },
/// new ReportColumn("budget", "Budżet mies.", ColumnType.Number) { IsAmount = true },
/// </code>
/// </example>
public sealed record ReportColumn(string Key, string Label, ColumnType Type = ColumnType.Text)
{
    /// <summary>
    /// Kolumna kluczowa (zwykle numer sprawy): zawsze pierwsza, przypięta do lewej krawędzi, nie da się jej ukryć ani przestawić.
    /// </summary>
    public bool IsKey { get; init; }

    /// <summary>
    /// Wartość wyświetlana jako link do formularza sprawy (kolor linku, pogrubienie); adres podaje
    /// <see cref="EodReport.RowHref"/>.
    /// </summary>
    public bool IsLink { get; init; }

    /// <summary>Kolumna ukryta w układzie domyślnym (rzadziej potrzebna); użytkownik może ją włączyć w menu „Kolumny”.</summary>
    public bool Hidden { get; init; }

    /// <summary>
    /// Własna kolejność wartości kolumny typu <see cref="ColumnType.List"/> (etapy wg przebiegu procesu, statusy wg pilności).
    /// Wartości spoza listy trafiają na koniec, w porządku alfabetycznym.
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – porządek alfabetyczny.</value>
    public IReadOnlyList<string>? ValueOrder { get; init; }

    /// <summary>
    /// Mapa kolorów znaczków: wartość wyświetlana jako <c>FluentBadge Appearance="Tint"</c> w kolorze z mapy
    /// (specyfikacja UI, sekcja 14; zob. <see cref="Statuses.Color(string, IReadOnlyDictionary{string, BadgeColor})"/>).
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – zwykły tekst.</value>
    public IReadOnlyDictionary<string, BadgeColor>? BadgeColors { get; init; }

    /// <summary>Kwota: wyświetlana jako „12 000,00 zł”, w filtrze zakresu pola mają sufiks „zł”.</summary>
    public bool IsAmount { get; init; }

    /// <summary>Wartość procentowa: wyświetlana jako „60%”.</summary>
    public bool IsPercent { get; init; }

    /// <summary>
    /// Własny format wartości: tekst w komórce, w wierszu grupy, w wyszukiwaniu i w filtrze kolumny (pola wyboru, żeton).
    /// Sortowanie i filtr listy działają dalej po surowej wartości (np. Czas jako <see cref="int"/> minut z tekstem „1h 30m”).
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – format wg <see cref="Type"/>.</value>
    /// <remarks>Dopasowanie Rejestru (AD-23 poz. 2, docs/ui/DOPASOWANIA.md). Funkcja nie dostaje <see langword="null"/>.</remarks>
    public Func<object?, string>? Format { get; init; }

    /// <summary>
    /// Nagłówek kolumny tylko w pliku eksportu (<see cref="EodReport.ExportFile"/>; np. „Godziny” zamiast „Czas”).
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – nagłówek <see cref="Label"/>.</value>
    /// <remarks>Dopasowanie Rejestru (AD-23 poz. 13, docs/ui/DOPASOWANIA.md); czyta go pisarz pliku ekranu.</remarks>
    public string? ExportLabel { get; init; }

    /// <summary>
    /// Wiersz grupy tej kolumny zaczyna się od nagłówka („Kolumna: wartość”); <see langword="false"/> – sama wartość
    /// (np. „Tydzień 1 (01.09–06.09)” zamiast „Tydzień: Tydzień 1 (01.09–06.09)”).
    /// </summary>
    /// <value>Domyślnie <see langword="true"/> – jak we wzorcu.</value>
    /// <remarks>Dopasowanie Rejestru (AD-23 poz. 15, docs/ui/DOPASOWANIA.md).</remarks>
    public bool GroupLabel { get; init; } = true;
}

/// <summary>Jeden poziom sortowania raportu (kolejne poziomy rozstrzygają remisy poprzednich).</summary>
/// <param name="Key">Klucz sortowanej kolumny (<see cref="ReportColumn.Key"/>).</param>
/// <param name="Descending"><see langword="true"/> – malejąco; domyślnie rosnąco.</param>
public sealed record ReportSort(string Key, bool Descending = false);

/// <summary>
/// Suma liczona w wierszach grup i w stopce raportu, np. <c>new ReportTotal("budget", "budżet miesięczny", IsAmount: true)</c>
/// daje „12 pozycji · budżet miesięczny: 190 000,00 zł”.
/// </summary>
/// <param name="Key">Klucz sumowanej kolumny liczbowej (<see cref="ReportColumn.Key"/>).</param>
/// <param name="Label">Opis sumy w podsumowaniu (małą literą, np. „budżet miesięczny”).</param>
/// <param name="IsAmount"><see langword="true"/> – suma formatowana jako kwota („190 000,00 zł”).</param>
public sealed record ReportTotal(string Key, string Label, bool IsAmount = false);

/// <summary>
/// Definicja raportu – dane, nie kod ekranu. Jeden silnik (<see cref="EodReport"/>) obsługuje wszystkie raporty;
/// raport różni się tylko definicją i wierszami.
/// </summary>
/// <remarks>
/// Specyfikacja UI, sekcja 9.7. Uprawnienia (<see cref="Roles"/>) są tu tylko opisem do katalogu raportów –
/// egzekwowanie dostępu nie należy do wzorca.
/// </remarks>
/// <example>
/// <code>
/// public static readonly ReportDefinition Recruitments = new()
/// {
///     Key = "rekrutacje",
///     Title = "Rekrutacje",
///     Description = "Wszystkie zlecenia rekrutacji z etapem, departamentem i budżetem.",
///     Process = "rekrutacja",
///     FileName = "rekrutacje",
///     Columns = [new ReportColumn("id", "Numer sprawy") { IsKey = true, IsLink = true }, /* … */],
///     Grouping = ["stage"],
///     Sorting = [new ReportSort("due")],
///     Total = new ReportTotal("budget", "budżet miesięczny", IsAmount: true),
/// };
/// </code>
/// </example>
public sealed record ReportDefinition
{
    /// <summary>Klucz raportu – część adresu (<c>/raporty/{Key}</c>) i domyślny klucz pamięci układu.</summary>
    public required string Key { get; init; }

    /// <summary>Tytuł raportu – nagłówek strony, pozycja w katalogu raportów i etykieta tabeli dla czytników ekranu.</summary>
    public required string Title { get; init; }

    /// <summary>Jedno zdanie: co pokazuje raport (opis strony i katalogu raportów).</summary>
    public required string Description { get; init; }

    /// <summary>Kod procesu, którego dotyczy raport.</summary>
    /// <value><see langword="null"/> – raport przekrojowy (wszystkie procesy).</value>
    public string? Process { get; init; }

    /// <summary>Role, które dają dostęp do raportu (opis w katalogu raportów).</summary>
    public IReadOnlyList<string> Roles { get; init; } = [];

    /// <summary>
    /// Raport dostępny wprost z menu bocznego (Moje zadania, Obserwowane, Zakończone) – bez ścieżki „Raporty ›”.
    /// </summary>
    public bool InMenu { get; init; }

    /// <summary>Początek nazwy pliku eksportu: „{FileName}-RRRR-MM-DD.{rozszerzenie}” (data stanu danych).</summary>
    public required string FileName { get; init; }

    /// <summary>Kolumny raportu w kolejności domyślnej (kolumna kluczowa zawsze trafia na początek).</summary>
    public required IReadOnlyList<ReportColumn> Columns { get; init; }

    /// <summary>Grupowanie w układzie domyślnym: klucze kolumn, kolejno od najwyższego poziomu.</summary>
    public IReadOnlyList<string> Grouping { get; init; } = [];

    /// <summary>Sortowanie w układzie domyślnym: kolejne poziomy sortowania.</summary>
    public IReadOnlyList<ReportSort> Sorting { get; init; } = [];

    /// <summary>Kolumna sumowana w wierszach grup i w stopce (opcjonalnie).</summary>
    public ReportTotal? Total { get; init; }

    /// <summary>Rodzaj raportu wyświetlany w katalogu raportów: „Tabela” albo „Tabela z grupowaniem”.</summary>
    public string Kind => Grouping.Count > 0 ? "Tabela z grupowaniem" : "Tabela";

    /// <summary>Wyszukuje kolumnę raportu po kluczu.</summary>
    /// <param name="key">Klucz kolumny (<see cref="ReportColumn.Key"/>).</param>
    /// <returns>Kolumna o podanym kluczu albo <see langword="null"/>, gdy definicja jej nie zawiera.</returns>
    public ReportColumn? Column(string key) => Columns.FirstOrDefault(c => c.Key == key);
}
