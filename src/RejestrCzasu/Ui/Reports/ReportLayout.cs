namespace RejestrCzasu.Ui;

/// <summary>
/// Filtr kolumny raportu. Rodzaj filtra zależy od typu kolumny (<see cref="ColumnType"/>); filtry działają od razu,
/// bez przycisku „Zastosuj”.
/// </summary>
public abstract record ColumnFilter;

/// <summary>Filtr kolumny typu <see cref="ColumnType.Text"/>: wiersze, w których wyświetlana wartość zawiera frazę.</summary>
/// <param name="Phrase">Szukany fragment tekstu (bez rozróżniania wielkości liter).</param>
public sealed record TextFilter(string Phrase) : ColumnFilter;

/// <summary>Filtr kolumny typu <see cref="ColumnType.List"/>: wiersze o jednej z wybranych wartości.</summary>
/// <param name="Values">Wybrane wartości; pusty zbiór oznacza „nic nie wybrano” (brak wierszy).</param>
public sealed record ListFilter(IReadOnlySet<string> Values) : ColumnFilter;

/// <summary>Filtr kolumny typu <see cref="ColumnType.Number"/>: zakres Od – Do (obie granice włącznie).</summary>
/// <param name="From">Dolna granica zakresu; <see langword="null"/> – bez dolnej granicy.</param>
/// <param name="To">Górna granica zakresu; <see langword="null"/> – bez górnej granicy.</param>
public sealed record NumberFilter(decimal? From, decimal? To) : ColumnFilter;

/// <summary>Filtr kolumny typu <see cref="ColumnType.Date"/>: okres Od – Do (obie granice włącznie).</summary>
/// <param name="From">Początek okresu; <see langword="null"/> – bez dolnej granicy.</param>
/// <param name="To">Koniec okresu; <see langword="null"/> – bez górnej granicy.</param>
public sealed record DateFilter(DateOnly? From, DateOnly? To) : ColumnFilter;

/// <summary>
/// Układ raportu ustawiony przez użytkownika: widoczność i kolejność kolumn, filtry, grupowanie, sortowanie,
/// zwinięte grupy i szukana fraza.
/// </summary>
/// <remarks>
/// Układ żyje do zmiany kontekstu procesu lub odświeżenia strony (przechowuje go <see cref="ReportLayoutStore"/>);
/// trwały zapis układów jest poza wzorcem. Specyfikacja UI, sekcja 9.7.
/// </remarks>
public sealed class ReportLayout
{
    private HashSet<string> _extraHidden = [];

    /// <summary>Tworzy układ domyślny raportu na podstawie definicji.</summary>
    /// <param name="definition">Definicja raportu, z której pochodzi układ domyślny.</param>
    /// <param name="extraHidden">
    /// Klucze kolumn ukrytych dodatkowo w układzie domyślnym (np. „Proces” w kontekście jednego procesu); zapamiętywane
    /// także dla <see cref="Reset(IEnumerable{string})"/>.
    /// </param>
    public ReportLayout(ReportDefinition definition, IEnumerable<string>? extraHidden = null)
    {
        Definition = definition;
        Reset(extraHidden);
    }

    /// <summary>Definicja raportu, do której należy układ.</summary>
    public ReportDefinition Definition { get; }

    /// <summary>Poziomy sortowania w kolejności ważności.</summary>
    public List<ReportSort> Sorting { get; } = [];

    /// <summary>Klucze kolumn grupujących, kolejno od najwyższego poziomu.</summary>
    public List<string> Grouping { get; } = [];

    /// <summary>Filtry kolumn: klucz kolumny → filtr.</summary>
    public Dictionary<string, ColumnFilter> Filters { get; } = [];

    /// <summary>Klucze kolumn ukrytych.</summary>
    public HashSet<string> Hidden { get; } = [];

    /// <summary>Klucze wszystkich kolumn w kolejności użytkownika (kluczowa zawsze pierwsza).</summary>
    public List<string> Order { get; } = [];

    /// <summary>Identyfikatory zwiniętych grup (<see cref="ReportGroup.Id"/>).</summary>
    public HashSet<string> Collapsed { get; } = [];

    /// <summary>Fraza szukana we wszystkich kolumnach (pole „Szukaj we wszystkich kolumnach”).</summary>
    /// <value>Domyślnie pusty tekst – bez wyszukiwania.</value>
    public string Search { get; set; } = string.Empty;

    /// <summary>Kolumny widoczne w kolejności użytkownika (kolumna kluczowa zawsze pierwsza).</summary>
    public IReadOnlyList<ReportColumn> Visible => Order
        .Select(Definition.Column)
        .OfType<ReportColumn>()
        .Where(c => !Hidden.Contains(c.Key))
        .ToList();

    /// <summary>Wszystkie kolumny, także ukryte, w kolejności użytkownika (menu „Kolumny”).</summary>
    public IReadOnlyList<ReportColumn> AllInOrder => Order
        .Select(Definition.Column)
        .OfType<ReportColumn>()
        .ToList();

    /// <summary>Klucze kolumn, które można przestawiać: widoczne, bez kolumny kluczowej.</summary>
    public IReadOnlyList<string> Movable => Visible.Where(c => !c.IsKey).Select(c => c.Key).ToList();

    /// <summary>
    /// Przywraca układ domyślny z definicji: widoczność i kolejność kolumn, grupowanie i sortowanie; rozwija wszystkie grupy.
    /// Filtry i szukana fraza zostają.
    /// </summary>
    /// <param name="extraHidden">
    /// Nowe klucze kolumn ukrytych dodatkowo; <see langword="null"/> – zostają te podane wcześniej.
    /// </param>
    public void Reset(IEnumerable<string>? extraHidden = null)
    {
        if (extraHidden is not null)
        {
            _extraHidden = [.. extraHidden];
        }

        Order.Clear();
        Order.AddRange(Definition.Columns.OrderByDescending(c => c.IsKey).Select(c => c.Key));
        Hidden.Clear();
        Hidden.UnionWith(Definition.Columns.Where(c => c.Hidden).Select(c => c.Key));
        Hidden.UnionWith(_extraHidden);
        Grouping.Clear();
        Grouping.AddRange(Definition.Grouping);
        Sorting.Clear();
        Sorting.AddRange(Definition.Sorting);
        Collapsed.Clear();
    }

    /// <summary>Przestawia kolumnę na miejsce innej widocznej kolumny (przeciąganie nagłówka).</summary>
    /// <param name="key">Klucz przestawianej kolumny.</param>
    /// <param name="target">Klucz kolumny, na której miejsce trafia przestawiana kolumna.</param>
    /// <returns>
    /// <see langword="true"/>, gdy kolejność się zmieniła; <see langword="false"/>, gdy którejś kolumny nie można
    /// przestawiać (ukryta, kluczowa, nieznana) albo obie to ta sama kolumna.
    /// </returns>
    public bool MoveTo(string key, string target)
    {
        var movable = Movable;
        int i = IndexOf(movable, key), j = IndexOf(movable, target);
        if (i < 0 || j < 0 || i == j)
        {
            return false;
        }

        Order.Remove(key);
        Order.Insert(Order.IndexOf(target) + (j > i ? 1 : 0), key);
        return true;
    }

    /// <summary>Przesuwa kolumnę wśród kolumn przestawialnych (przyciski kolejności w okienku kolumny).</summary>
    /// <param name="key">Klucz przesuwanej kolumny.</param>
    /// <param name="direction">
    /// Kierunek: <c>"start"</c> (na początek), <c>"left"</c> (w lewo), <c>"right"</c> (w prawo), <c>"end"</c> (na koniec);
    /// inna wartość działa jak <c>"right"</c>.
    /// </param>
    /// <returns>
    /// <see langword="true"/>, gdy kolejność się zmieniła; <see langword="false"/>, gdy kolumna jest już na skraju
    /// albo nie można jej przestawiać.
    /// </returns>
    public bool Move(string key, string direction)
    {
        var movable = Movable;
        var i = IndexOf(movable, key);
        var j = direction switch
        {
            "start" => 0,
            "end" => movable.Count - 1,
            "left" => i - 1,
            _ => i + 1,
        };
        return i >= 0 && j >= 0 && j < movable.Count && MoveTo(key, movable[j]);
    }

    private static int IndexOf(IReadOnlyList<string> list, string key)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] == key)
            {
                return i;
            }
        }

        return -1;
    }
}

/// <summary>
/// Pamięć układów raportów na czas sesji użytkownika (usługa Scoped, rejestruje ją
/// <see cref="ServiceCollectionExtensions.AddEodPlatformUI"/>).
/// </summary>
/// <remarks>Zmiana kontekstu procesu czyści wszystkie układy (<see cref="Clear"/>).</remarks>
public sealed class ReportLayoutStore
{
    private readonly Dictionary<string, ReportLayout> _layouts = [];

    /// <summary>
    /// Zwraca zapamiętany układ raportu albo tworzy układ domyślny, gdy go brak lub gdy zmieniła się definicja raportu.
    /// </summary>
    /// <param name="key">Klucz pamięci układu, np. <c>"zadania"</c> albo <c>"zadania@rekrutacja"</c> (osobny układ w kontekście procesu).</param>
    /// <param name="definition">Definicja raportu.</param>
    /// <param name="extraHidden">Klucze kolumn ukrytych dodatkowo w nowo tworzonym układzie domyślnym.</param>
    /// <returns>Układ raportu – ten sam obiekt przy kolejnych wywołaniach z tym samym kluczem i definicją.</returns>
    public ReportLayout Get(string key, ReportDefinition definition, IEnumerable<string>? extraHidden = null)
    {
        if (!_layouts.TryGetValue(key, out var layout) || layout.Definition != definition)
        {
            layout = new ReportLayout(definition, extraHidden);
            _layouts[key] = layout;
        }

        return layout;
    }

    /// <summary>Usuwa wszystkie zapamiętane układy – kolejne <see cref="Get"/> zwrócą układy domyślne.</summary>
    public void Clear() => _layouts.Clear();
}
