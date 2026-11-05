using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace RejestrCzasu.Ui;

/// <summary>
/// Raport tabelaryczny – jeden silnik dla wszystkich raportów (specyfikacja UI, sekcja 9.7).
/// Raport różni się tylko definicją (<see cref="ReportDefinition"/>) i wierszami.
/// </summary>
/// <remarks>
/// Komponent ma pasek narzędzi (szukaj, Kolumny, Eksportuj), pasek układu (grupowanie, sortowanie, filtry), tabelę
/// w wyglądzie FluentDataGrid z przyklejonym nagłówkiem, przypiętą kolumną kluczową i wierszami grup oraz stopkę
/// z podsumowaniem. Umieszczaj go na stronie <c>EodPage Fill="true"</c> – raport zajmuje całą przestrzeń, przewija się
/// tylko tabela. Kliknięcie wiersza nic nie robi; do sprawy prowadzi link w kolumnie kluczowej. Układ ustawiony przez
/// użytkownika pamięta <see cref="ReportLayoutStore"/> pod kluczem <see cref="LayoutKey"/>; logika bez UI jest
/// w <see cref="ReportEngine"/>.
/// </remarks>
/// <example>
/// <code>
/// &lt;EodPage Title="Moje zadania" Fill="true">
///     &lt;EodReport Definition="@DemoReports.MyTasks"
///                Rows="@_rows"
///                LayoutKey="zadania@rekrutacja"
///                ExtraHiddenColumns="@(["process"])"
///                RowHref="@(row => Data.CaseHref((string)row["id"]!))"
///                DataAsOf="@Data.Today">
///         &lt;RowActions Context="row">…&lt;/RowActions>
///     &lt;/EodReport>
/// &lt;/EodPage>
/// </code>
/// </example>
public partial class EodReport
{
    private readonly string _id = "eod-report-" + Guid.NewGuid().ToString("N")[..8];
    private IReadOnlyList<ReportColumn> _visibleColumns = [];
    private IReadOnlyList<IReadOnlyDictionary<string, object?>> _filteredRows = [];
    private IReadOnlyList<IReadOnlyDictionary<string, object?>> _pageRows = [];
    private PaginationState? _pagination;
    private ReportColumn? _popoverColumn;
    private string? _draggedKey;
    private string? _dropTargetKey;

    [Inject]
    private ReportLayoutStore LayoutStore { get; set; } = default!;

    [Inject]
    private UiMessages Messages { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    /// <summary>Definicja raportu: kolumny, układ domyślny, suma i nazwa pliku eksportu.</summary>
    [Parameter, EditorRequired]
    public ReportDefinition Definition { get; set; } = default!;

    /// <summary>Wiersze raportu: klucz kolumny (<see cref="ReportColumn.Key"/>) → wartość (tekst, liczba, <see cref="DateOnly"/>).</summary>
    /// <remarks>Wszystkie wiersze, przed filtrami – wyszukiwanie, filtry i sortowanie wykonuje komponent.</remarks>
    [Parameter, EditorRequired]
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];

    /// <summary>
    /// Klucz pamięci układu, np. <c>"zadania@rekrutacja"</c> – osobny układ tego samego raportu w kontekście procesu.
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – klucz raportu (<see cref="ReportDefinition.Key"/>).</value>
    [Parameter]
    public string? LayoutKey { get; set; }

    /// <summary>
    /// Klucze kolumn ukrytych dodatkowo w układzie domyślnym (np. „Proces” w kontekście jednego procesu).
    /// Użytkownik może je włączyć w menu „Kolumny”.
    /// </summary>
    [Parameter]
    public IReadOnlyCollection<string>? ExtraHiddenColumns { get; set; }

    /// <summary>
    /// Adres formularza sprawy dla wiersza – używany w kolumnach z <see cref="ReportColumn.IsLink"/>.
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – link prowadzi do „#”.</value>
    [Parameter]
    public Func<IReadOnlyDictionary<string, object?>, string>? RowHref { get; set; }

    /// <summary>
    /// Zawartość kolumny „Akcje” (przypiętej z prawej) dla wiersza: najwyżej dwie ikony Subtle i menu „Więcej”.
    /// </summary>
    /// <remarks>Kontekstem szablonu jest wiersz raportu. Kolumna „Akcje” jest zawsze ostatnia i nie trafia do pliku eksportu.</remarks>
    /// <value>Domyślnie <see langword="null"/> – tabela bez kolumny akcji.</value>
    [Parameter]
    public RenderFragment<IReadOnlyDictionary<string, object?>>? RowActions { get; set; }

    /// <summary>Data stanu danych – trafia do nazwy pliku eksportu („{FileName}-RRRR-MM-DD.{rozszerzenie}”).</summary>
    /// <value>
    /// Domyślnie <see langword="default"/> – ekran z eksportem podaje datę z <c>PolishCalendar.Today</c>
    /// (dopasowanie Rejestru: zakaz <c>DateTime.Today</c>, AD-6).
    /// </value>
    [Parameter]
    public DateOnly DataAsOf { get; set; }

    /// <summary>
    /// Przycisk „Eksportuj” na pasku narzędzi: plik z <see cref="ExportFile"/> z widocznymi kolumnami i wszystkimi
    /// wierszami po wyszukiwaniu i filtrach (ze wszystkich stron), w kolejności z ekranu.
    /// </summary>
    /// <value>
    /// Domyślnie <see langword="true"/> – jak we wzorcu; w Rejestrze <see langword="true"/> tylko na listach Wpisów,
    /// Słowniki podają <see langword="false"/>. Bez <see cref="ExportFile"/> przycisku nie ma.
    /// </value>
    /// <remarks>Dopasowanie Rejestru, AD-23 poz. 1, 12 (etykieta „Eksportuj” zamiast „Eksport CSV”) i 14.</remarks>
    [Parameter]
    public bool ShowExport { get; set; } = true;

    /// <summary>
    /// Pisarz pliku eksportu: widoczne kolumny (w kolejności z ekranu) i wszystkie wiersze po wyszukiwaniu i filtrach
    /// (ze wszystkich stron) → plik (<see cref="ReportFile"/>). Nagłówek kolumny w pliku: <see cref="ReportColumn.ExportLabel"/>
    /// albo <see cref="ReportColumn.Label"/>.
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – bez przycisku „Eksportuj” (eksport CSV wzorca usunięty).</value>
    /// <remarks>Dopasowanie Rejestru, AD-23 poz. 14.</remarks>
    [Parameter]
    public Func<IReadOnlyList<ReportColumn>, IReadOnlyList<IReadOnlyDictionary<string, object?>>, ReportFile>? ExportFile { get; set; }

    /// <summary>
    /// Bez sortowania użytkownika wiersze zostają w kolejności wejścia (także wewnątrz grup), zamiast porządku wg kolumny
    /// kluczowej; remisy sortowania rozstrzyga kolejność wejścia.
    /// </summary>
    /// <value>Domyślnie <see langword="false"/> – porządek wzorca.</value>
    /// <remarks>Dopasowanie Rejestru, AD-23 poz. 3.</remarks>
    [Parameter]
    public bool PreserveRowOrder { get; set; }

    /// <summary>
    /// Tekst podsumowania wiersza grupy po liczniku pozycji: poziom grupowania (od 0) i surowa wartość klucza grupy
    /// (<see cref="ReportGroup.Key"/>) → tekst, np. suma Czasu podana przez ekran.
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – suma z <see cref="ReportDefinition.Total"/> jak we wzorcu.</value>
    /// <remarks>Dopasowanie Rejestru, AD-23 poz. 4 (sumy liczy ekran, AD-5). Gdy podany, <see cref="ReportDefinition.Total"/> jest pomijany.</remarks>
    [Parameter]
    public Func<int, object?, string>? GroupSummary { get; set; }

    /// <summary>Tekst stopki po liczniku pozycji, np. suma Czasu podana przez ekran.</summary>
    /// <value>Domyślnie <see langword="null"/> – suma z <see cref="ReportDefinition.Total"/> jak we wzorcu.</value>
    /// <remarks>Dopasowanie Rejestru, AD-23 poz. 4 (sumy liczy ekran, AD-5). Gdy podany, <see cref="ReportDefinition.Total"/> jest pomijany.</remarks>
    [Parameter]
    public string? FooterSummary { get; set; }

    /// <summary>
    /// Układ zablokowany: użytkownik nie zmienia grupowania, sortowania ani kolejności kolumn (nagłówek bez sortowania
    /// i przeciągania, żetony bez przycisków, okienko kolumny tylko z filtrem); filtry i zwijanie grup działają.
    /// </summary>
    /// <value>Domyślnie <see langword="false"/> – układ użytkownika jak we wzorcu.</value>
    /// <remarks>Dopasowanie Rejestru, AD-23 poz. 5.</remarks>
    [Parameter]
    public bool LockedLayout { get; set; }

    /// <summary>
    /// Liczba wierszy na stronie: przy większej liczbie wierszy po wyszukiwaniu i filtrach pod tabelą jest
    /// <see cref="FluentPaginator"/> („Pozycje 1–30 z 95”, „Strona 1 z 4”). Przy grupowaniu bez paginacji (całe grupy).
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – raport przewija się w całości, jak we wzorcu.</value>
    /// <remarks>
    /// Dopasowanie Rejestru, AD-23 poz. 11. Stopka i eksport liczą wszystkie wiersze po filtrach, nie stronę. Zmiana
    /// wyszukiwania, filtra, sortowania, grupowania lub układu wraca na pierwszą stronę; nowe <see cref="Rows"/> zostawiają
    /// bieżącą stronę, przyciętą do ostatniej istniejącej.
    /// </remarks>
    [Parameter]
    public int? PageSize { get; set; }

    /// <summary>
    /// Dane raportu się wczytują: zamiast wierszy – szkielety (FluentSkeleton) w kolumnach bieżącego układu, w stopce
    /// „Wczytywanie danych…”, tabela z <c>aria-busy="true"</c>.
    /// </summary>
    /// <value>Domyślnie <see langword="false"/>.</value>
    /// <remarks>
    /// Pasek narzędzi i pasek układu zostają – użytkownik widzi, co się wczytuje. Przycisk, który uruchomił wczytywanie
    /// (np. „Odśwież”), ma w tym czasie <c>Loading="true"</c> (specyfikacja UI, sekcje 9.14 i 11).
    /// </remarks>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>
    /// Stan pusty raportu bez żadnych wierszy (przed wyszukiwaniem i filtrami) – zwykle <see cref="EodEmptyState"/> z <c>Compact="true"</c>,
    /// np. „Nie masz zadań do wykonania”.
    /// </summary>
    /// <value>Domyślnie <see langword="null"/> – „Raport nie zawiera pozycji.”.</value>
    /// <remarks>
    /// Wiersze ukryte przez wyszukiwanie lub filtry dają zawsze stan „Brak pozycji spełniających filtry.” z akcją „Wyczyść filtry”
    /// (specyfikacja UI, sekcje 5 i 12).
    /// </remarks>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    // Liczba wierszy szkieletu i długości „tekstu” w komórkach (różne – szkielet wygląda jak treść)
    private const int LoadingRows = 8;
    private static readonly string[] SkeletonWidths = ["72%", "48%", "64%", "36%", "56%"];

    private ReportLayout Layout { get; set; } = default!;

    private string FooterText
        => (FooterSummary is null ? ReportEngine.Summary(Definition, _filteredRows) : WithCount(_filteredRows.Count, FooterSummary))
         + (_filteredRows.Count < Rows.Count ? $" · po filtrach, z {Rows.Count}" : string.Empty);

    // Paginacja tylko bez grupowania: strona przecinająca grupy dałaby liczniki części grupy przy sumach całości.
    private bool Paged => PageSize is > 0 && Layout.Grouping.Count == 0;

    private bool ShowPaginator => Paged && !Loading && _pagination is not null && _filteredRows.Count > PageSize;

    private string ItemRangeText
    {
        get
        {
            var total = _filteredRows.Count;
            var size = PageSize ?? 1;
            var index = _pagination?.CurrentPageIndex ?? 0;
            var first = total == 0 ? 0 : index * size + 1;
            var last = Math.Min(total, (index + 1) * size);
            return $"Pozycje {first}–{last} z {total}";
        }
    }

    private string PageText => $"Strona {(_pagination?.CurrentPageIndex ?? 0) + 1} z {(_pagination?.LastPageIndex ?? 0) + 1}";

    private string GroupText(ReportGroup group)
        => GroupSummary is null
            ? ReportEngine.Summary(Definition, group.Rows)
            : WithCount(group.Rows.Count, GroupSummary(group.Level, group.Key));

    // Podsumowanie zawsze zaczyna się od licznika pozycji (R17); tekst ekranu dochodzi po „ · ”.
    private static string WithCount(int count, string? text)
    {
        var counter = PolishPlural.WithCount(count, "pozycja", "pozycje", "pozycji");
        return string.IsNullOrEmpty(text) ? counter : $"{counter} · {text}";
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Layout = LayoutStore.Get(LayoutKey ?? Definition.Key, Definition, ExtraHiddenColumns);
        Refresh();
    }

    // firstPage: zmiana wyszukiwania, filtra, sortowania, grupowania lub układu wraca na pierwszą stronę (AD-23 poz. 11);
    // w pozostałych przypadkach (np. nowe Rows) strona zostaje, przycięta do ostatniej istniejącej.
    private void Refresh(bool firstPage = false)
    {
        _visibleColumns = Layout.Visible;
        _filteredRows = ReportEngine.Rows(Layout, Rows, PreserveRowOrder);
        if (PageSize is not ({ } size and > 0))
        {
            _pageRows = _filteredRows;
            return;
        }

        _pagination ??= new PaginationState { ItemsPerPage = size };
        _pagination.ItemsPerPage = size;
        var lastPage = Math.Max(0, (_filteredRows.Count - 1) / size);
        var page = firstPage ? 0 : Math.Min(_pagination.CurrentPageIndex, lastPage);
        if (page != _pagination.CurrentPageIndex)
        {
            Run(_pagination.SetCurrentPageIndexAsync(page));
        }

        Run(_pagination.SetTotalItemCountAsync(_filteredRows.Count));
        SlicePage();
    }

    private void SlicePage()
    {
        if (!Paged || _pagination is null)
        {
            _pageRows = _filteredRows;
            return;
        }

        var size = PageSize!.Value;
        _pageRows = _filteredRows.Skip(_pagination.CurrentPageIndex * size).Take(size).ToList();
    }

    // Strona wybrana przyciskami FluentPaginator (PaginationState już ją ma).
    private void OnPageChanged(int pageIndex) => SlicePage();

    // Założenie (Fluent UI 5.0.1): PaginationState zmienia stan od razu, a zadanie tylko powiadamia FluentPaginator
    // (jedyny subskrybent, odświeżenie komponentu), więc kończy się synchronicznie. Rzucany jest tylko błąd zadania już
    // zakończonego; zadanie jeszcze niezakończone (inna wersja Fluent UI) nie jest obserwowane (DOPASOWANIA poz. 11).
    private static void Run(Task task)
    {
        if (task.IsFaulted)
        {
            task.GetAwaiter().GetResult();
        }
    }

    private string FilterButtonId(ReportColumn column) => $"{_id}-filter-{column.Key}";

    private string LabelOf(string key) => Definition.Column(key)?.Label ?? key;

    // ── Pasek narzędzi ───────────────────────────────────────────────────

    private void SetSearch(string? phrase)
    {
        Layout.Search = phrase ?? string.Empty;
        Refresh(firstPage: true);
    }

    private void SetColumnVisible(ReportColumn column, bool visible)
    {
        if (column.IsKey)
        {
            return;
        }

        if (visible)
        {
            Layout.Hidden.Remove(column.Key);
        }
        else
        {
            Layout.Hidden.Add(column.Key);
        }

        _popoverColumn = null;
        Refresh();
    }

    private void ResetLayout()
    {
        Layout.Reset();
        _popoverColumn = null;
        Refresh(firstPage: true);
    }

    private async Task ExportAsync()
    {
        if (ExportFile is null)
        {
            return;
        }

        // Dopasowanie Rejestru (AD-23 poz. 14): plik buduje ekran, do przeglądarki idzie strumieniem obwodu.
        var columns = Layout.Visible;
        var file = ExportFile(columns, _filteredRows);
        var fileName = $"{Definition.FileName}-{DataAsOf:yyyy-MM-dd}.{file.Extension}";
        using var stream = new DotNetStreamReference(new MemoryStream(file.Content));
        await JS.InvokeVoidAsync("EodUI.downloadStream", fileName, stream, file.ContentType);
        await Messages.ShowSuccessAsync("Wyeksportowano raport.",
            $"{fileName}: {PolishPlural.WithCount(_filteredRows.Count, "wiersz", "wiersze", "wierszy")}, {PolishPlural.WithCount(columns.Count, "kolumna", "kolumny", "kolumn")}.");
    }

    // ── Grupowanie ───────────────────────────────────────────────────────

    private void AddGrouping(string key)
    {
        if (!Layout.Grouping.Contains(key))
        {
            Layout.Grouping.Add(key);
        }

        Layout.Collapsed.Clear();
        _popoverColumn = null;
        Refresh(firstPage: true);
    }

    private void RemoveGrouping(string key)
    {
        Layout.Grouping.Remove(key);
        Layout.Collapsed.Clear();
        _popoverColumn = null;
        Refresh(firstPage: true);
    }

    private void ToggleGroup(string id)
    {
        if (!Layout.Collapsed.Remove(id))
        {
            Layout.Collapsed.Add(id);
        }
    }

    private void ExpandAll() => Layout.Collapsed.Clear();

    private void CollapseAll()
    {
        void Collect(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, int level, string path)
        {
            foreach (var g in ReportEngine.Groups(Layout, rows, level, path))
            {
                Layout.Collapsed.Add(g.Id);
                Collect(g.Rows, level + 1, g.Id);
            }
        }

        Collect(_filteredRows, 0, string.Empty);
    }

    // ── Sortowanie ───────────────────────────────────────────────────────

    // Klik: sortuj tylko po tej kolumnie (ponowny klik odwraca kierunek). Shift + klik: dodaj kolumnę jako kolejny poziom.
    private void SortByHeader(ReportColumn column, MouseEventArgs e)
    {
        var current = Layout.Sorting.FindIndex(s => s.Key == column.Key);
        if (e.ShiftKey)
        {
            if (current >= 0)
            {
                Layout.Sorting[current] = Layout.Sorting[current] with { Descending = !Layout.Sorting[current].Descending };
            }
            else
            {
                Layout.Sorting.Add(new ReportSort(column.Key));
            }
        }
        else
        {
            var descending = current >= 0 && Layout.Sorting.Count == 1 && !Layout.Sorting[current].Descending;
            Layout.Sorting.Clear();
            Layout.Sorting.Add(new ReportSort(column.Key, descending));
        }

        _popoverColumn = null;
        Refresh(firstPage: true);
    }

    private void ReverseSort(string key)
    {
        var i = Layout.Sorting.FindIndex(s => s.Key == key);
        if (i >= 0)
        {
            Layout.Sorting[i] = Layout.Sorting[i] with { Descending = !Layout.Sorting[i].Descending };
            Refresh(firstPage: true);
        }
    }

    private void RemoveSort(string key)
    {
        Layout.Sorting.RemoveAll(s => s.Key == key);
        _popoverColumn = null;
        Refresh(firstPage: true);
    }

    private void SetSortDirection(string key, bool descending)
    {
        var i = Layout.Sorting.FindIndex(s => s.Key == key);
        if (i >= 0)
        {
            Layout.Sorting[i] = Layout.Sorting[i] with { Descending = descending };
        }
        else
        {
            Layout.Sorting.Clear();
            Layout.Sorting.Add(new ReportSort(key, descending));
        }

        _popoverColumn = null;
        Refresh(firstPage: true);
    }

    private void AddSort(string key)
    {
        if (!Layout.Sorting.Exists(s => s.Key == key))
        {
            Layout.Sorting.Add(new ReportSort(key));
        }

        _popoverColumn = null;
        Refresh(firstPage: true);
    }

    // ── Filtry (działają od razu, bez „Zastosuj”) ────────────────────────

    private void SetFilter(string key, ColumnFilter? filter)
    {
        if (filter is null)
        {
            Layout.Filters.Remove(key);
        }
        else
        {
            Layout.Filters[key] = filter;
        }

        Refresh(firstPage: true);
    }

    // Zaznaczenie wszystkich wartości listy = brak filtra.
    private void ToggleListValue(ReportColumn column, string value, bool selected)
    {
        var all = ReportEngine.DistinctValues(column, Rows).Select(v => v?.ToString() ?? string.Empty).ToHashSet();
        var chosen = (Layout.Filters.GetValueOrDefault(column.Key) as ListFilter)?.Values.ToHashSet() ?? new HashSet<string>(all);
        if (selected)
        {
            chosen.Add(value);
        }
        else
        {
            chosen.Remove(value);
        }

        SetFilter(column.Key, chosen.SetEquals(all) ? null : new ListFilter(chosen));
    }

    private void SetNumberRange(string key, decimal? from, decimal? to)
        => SetFilter(key, from is null && to is null ? null : new NumberFilter(from, to));

    private void SetDateBound(string key, bool isFrom, DateTime? date)
    {
        var current = Layout.Filters.GetValueOrDefault(key) as DateFilter;
        var value = date is null ? (DateOnly?)null : DateOnly.FromDateTime(date.Value);
        var from = isFrom ? value : current?.From;
        var to = isFrom ? current?.To : value;
        SetFilter(key, from is null && to is null ? null : new DateFilter(from, to));
    }

    private void ClearFilter(string key)
    {
        Layout.Filters.Remove(key);
        _popoverColumn = null;
        Refresh(firstPage: true);
    }

    private void ClearFilters()
    {
        Layout.Filters.Clear();
        Refresh(firstPage: true);
    }

    // Akcja stanu „Brak pozycji spełniających filtry.”: czyści też wyszukiwanie (oba ukrywają wiersze)
    private void ClearSearchAndFilters()
    {
        Layout.Search = string.Empty;
        Layout.Filters.Clear();
        Refresh(firstPage: true);
    }

    // ── Okienko kolumny ──────────────────────────────────────────────────

    private void TogglePopover(ReportColumn column) => _popoverColumn = _popoverColumn?.Key == column.Key ? null : column;

    private void OnPopoverOpenedChanged(bool opened)
    {
        if (!opened)
        {
            _popoverColumn = null;
        }
    }

    private void ClosePopover() => _popoverColumn = null;

    // Przestawienie kolumny przyciskami – okienko zostaje otwarte przy tej samej kolumnie.
    private void MoveColumn(string key, string direction)
    {
        if (Layout.Move(key, direction))
        {
            Refresh();
        }
    }

    // ── Przeciąganie nagłówków (kolejność kolumn) ────────────────────────

    private void StartDrag(ReportColumn column)
    {
        if (column.IsKey || LockedLayout)
        {
            return;
        }

        _draggedKey = column.Key;
        _popoverColumn = null;
    }

    private void DragOver(ReportColumn column)
    {
        var target = _draggedKey is not null && !column.IsKey && column.Key != _draggedKey ? column.Key : null;
        _dropTargetKey = target;
    }

    private void DropOn(ReportColumn column)
    {
        if (_draggedKey is not null && !column.IsKey && Layout.MoveTo(_draggedKey, column.Key))
        {
            Refresh();
        }

        EndDrag();
    }

    private void EndDrag()
    {
        _draggedKey = null;
        _dropTargetKey = null;
    }
}
