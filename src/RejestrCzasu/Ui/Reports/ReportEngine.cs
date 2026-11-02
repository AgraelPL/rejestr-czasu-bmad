using System.Globalization;
using System.Text;

namespace RejestrCzasu.Ui;

/// <summary>Grupa wierszy raportu na jednym poziomie grupowania.</summary>
/// <param name="Id">Ścieżka grupy (np. <c>"/stage:Zgłoszenie/department:Kadry"</c>) – identyfikator do zwijania i rozwijania.</param>
/// <param name="Column">Kolumna grupująca.</param>
/// <param name="Value">Wartość grupy, sformatowana jak w komórce (<see cref="ReportEngine.Display"/>).</param>
/// <param name="Level">Poziom grupowania liczony od 0 – wcięcie wiersza grupy to 28 px na poziom.</param>
/// <param name="Rows">Wiersze należące do grupy (ze wszystkich niższych poziomów).</param>
/// <param name="Key">
/// Surowa wartość klucza grupy (z pierwszego wiersza grupy) – dla sum podawanych przez ekran
/// (<see cref="EodReport.GroupSummary"/>; dopasowanie Rejestru, AD-23 poz. 4).
/// </param>
public sealed record ReportGroup(string Id, ReportColumn Column, string Value, int Level, IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows, object? Key = null);

/// <summary>
/// Logika raportu bez UI: odczyt i format wartości, porządek, filtry, sortowanie z grupowaniem, sumy i eksport CSV.
/// Używa jej <see cref="EodReport"/>; można ją testować i wywoływać bez komponentu.
/// </summary>
/// <remarks>Wiersz raportu to słownik: klucz kolumny (<see cref="ReportColumn.Key"/>) → wartość. Specyfikacja UI, sekcja 9.7.</remarks>
public static class ReportEngine
{
    private static readonly CultureInfo Culture = Formats.Culture;

    // ── Wartości ─────────────────────────────────────────────────────────

    /// <summary>Odczytuje wartość kolumny z wiersza.</summary>
    /// <param name="row">Wiersz raportu.</param>
    /// <param name="key">Klucz kolumny.</param>
    /// <returns>Wartość albo <see langword="null"/>, gdy wiersz nie ma takiego klucza.</returns>
    public static object? GetValue(IReadOnlyDictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? value : null;

    /// <summary>Zamienia wartość komórki na liczbę (do porównań, filtrów zakresu i sum).</summary>
    /// <param name="value">
    /// Wartość: <see cref="decimal"/>, <see cref="int"/>, <see cref="long"/>, <see cref="double"/> albo tekst liczby
    /// w zapisie polskim.
    /// </param>
    /// <returns>Liczba; 0 dla <see langword="null"/> i wartości, których nie da się odczytać.</returns>
    public static decimal ToNumber(object? value) => value switch
    {
        null => 0m,
        decimal d => d,
        int i => i,
        long l => l,
        double db => (decimal)db,
        _ => decimal.TryParse(value.ToString(), NumberStyles.Number, Culture, out var x) ? x : 0m,
    };

    /// <summary>Zamienia wartość komórki na datę (do porównań, filtrów okresu i formatu dd.mm.rrrr).</summary>
    /// <param name="value">Wartość: <see cref="DateOnly"/>, <see cref="DateTime"/> albo tekst daty w zapisie niezależnym od kultury (RRRR-MM-DD).</param>
    /// <returns>Data albo <see langword="null"/>, gdy wartość nie jest datą.</returns>
    public static DateOnly? ToDate(object? value) => value switch
    {
        DateOnly d => d,
        DateTime dt => DateOnly.FromDateTime(dt),
        string s when DateOnly.TryParse(s, CultureInfo.InvariantCulture, out var d) => d,
        _ => null,
    };

    /// <summary>Formatuje wartość do wyświetlenia w komórce, wierszu grupy i filtrze.</summary>
    /// <param name="column">Kolumna, która decyduje o formacie.</param>
    /// <param name="value">Wartość komórki.</param>
    /// <returns>
    /// Tekst: wynik <see cref="ReportColumn.Format"/>, gdy kolumna go ma; inaczej daty dd.mm.rrrr, kwoty „12 000,00 zł”,
    /// procenty „60%”, liczby z odstępem tysięcy; pusty dla <see langword="null"/>.
    /// </returns>
    public static string Display(ReportColumn column, object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return column.Format is { } format ? format(value) : TypeDisplay(column, value);
    }

    // Format wg typu kolumny (bez ReportColumn.Format) – także dla granic filtra zakresu, które są liczbami decimal.
    private static string TypeDisplay(ReportColumn column, object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return column.Type switch
        {
            ColumnType.Date => ToDate(value) is { } d ? Formats.Date(d) : value.ToString() ?? string.Empty,
            ColumnType.Number when column.IsAmount => Formats.Amount(ToNumber(value)),
            ColumnType.Number when column.IsPercent => Formats.Percent(ToNumber(value)),
            ColumnType.Number => ToNumber(value).ToString("#,0.##", Culture),
            _ => value.ToString() ?? string.Empty,
        };
    }

    /// <summary>
    /// Porównuje dwie wartości kolumny. Porządek zależy od typu: liczby i daty rosnąco, listy wg
    /// <see cref="ReportColumn.ValueOrder"/>, tekst alfabetycznie (kultura polska).
    /// </summary>
    /// <param name="column">Kolumna, której typ decyduje o porządku.</param>
    /// <param name="a">Pierwsza wartość.</param>
    /// <param name="b">Druga wartość.</param>
    /// <returns>Liczba ujemna, zero albo dodatnia – jak w <see cref="IComparer{T}.Compare"/>.</returns>
    /// <remarks>
    /// Kolumna z <see cref="ReportColumn.Format"/> porównuje surowe wartości tego samego typu <see cref="IComparable"/>
    /// (np. <see cref="int"/> minut liczbowo, także w kolumnie <see cref="ColumnType.List"/>); <see langword="null"/> jest
    /// przed każdą wartością. Wartości różnych typów, tekst (<see cref="string"/>) i kolumny z
    /// <see cref="ReportColumn.ValueOrder"/> porównuje porządek typu kolumny.
    /// </remarks>
    public static int Compare(ReportColumn column, object? a, object? b)
    {
        if (column.Format is not null && column.ValueOrder is not { Count: > 0 } && a is not string && b is not string)
        {
            switch (a, b)
            {
                case (null, null):
                    return 0;
                case (null, _):
                    return -1;
                case (_, null):
                    return 1;
                case (IComparable ca, _) when a.GetType() == b.GetType():
                    return ca.CompareTo(b);
            }
        }

        switch (column.Type)
        {
            case ColumnType.Number:
                return ToNumber(a).CompareTo(ToNumber(b));
            case ColumnType.Date:
                return Nullable.Compare(ToDate(a), ToDate(b));
            case ColumnType.List when column.ValueOrder is { Count: > 0 } order:
                var ia = IndexOf(order, a?.ToString());
                var ib = IndexOf(order, b?.ToString());
                return ia != ib ? ia.CompareTo(ib) : string.Compare(a?.ToString(), b?.ToString(), Culture, CompareOptions.None);
            default:
                return string.Compare(a?.ToString(), b?.ToString(), Culture, CompareOptions.None);
        }
    }

    /// <summary>Zwraca różne wartości kolumny w porządku kolumny (pola wyboru filtra listy).</summary>
    /// <param name="column">Kolumna.</param>
    /// <param name="rows">Wiersze raportu (zwykle wszystkie, przed filtrami).</param>
    /// <returns>Różne wartości (porównywane jako tekst), uporządkowane wg <see cref="Compare"/>.</returns>
    public static IReadOnlyList<object?> DistinctValues(ReportColumn column, IEnumerable<IReadOnlyDictionary<string, object?>> rows)
        => rows.Select(r => GetValue(r, column.Key))
            .DistinctBy(v => v?.ToString())
            .Order(Comparer<object?>.Create((a, b) => Compare(column, a, b)))
            .ToList();

    // ── Filtry ───────────────────────────────────────────────────────────

    /// <summary>Sprawdza, czy wartość komórki spełnia filtr kolumny.</summary>
    /// <param name="column">Kolumna filtrowana.</param>
    /// <param name="filter">Filtr kolumny.</param>
    /// <param name="value">Wartość komórki.</param>
    /// <returns>
    /// <see langword="true"/>, gdy wartość spełnia filtr. Filtr tekstowy porównuje wartość wyświetlaną, bez rozróżniania
    /// wielkości liter; granice zakresów są włączne; nieznany rodzaj filtra przepuszcza każdą wartość.
    /// </returns>
    public static bool Matches(ReportColumn column, ColumnFilter filter, object? value) => filter switch
    {
        TextFilter t => Display(column, value).Contains(t.Phrase, StringComparison.CurrentCultureIgnoreCase),
        ListFilter l => l.Values.Contains(value?.ToString() ?? string.Empty),
        NumberFilter n => (n.From is null || ToNumber(value) >= n.From) && (n.To is null || ToNumber(value) <= n.To),
        DateFilter d => ToDate(value) is { } date && (d.From is null || date >= d.From) && (d.To is null || date <= d.To),
        _ => true,
    };

    /// <summary>Opisuje filtr słownie – tekst żetonu filtra na pasku układu.</summary>
    /// <param name="column">Kolumna filtrowana.</param>
    /// <param name="filter">Filtr kolumny.</param>
    /// <param name="rows">
    /// Wiersze raportu – potrzebne kolumnie z <see cref="ReportColumn.Format"/>, żeby filtr listy opisać tekstem
    /// z formatu w porządku surowych wartości; bez nich opis zawiera surowe wartości.
    /// </param>
    /// <returns>Np. „zawiera „x””, „Zgłoszenie, Opis stanowiska”, „01.10.2026 – 31.10.2026”, „od 5”, „do 31.10.2026”.</returns>
    public static string DescribeFilter(ReportColumn column, ColumnFilter filter, IEnumerable<IReadOnlyDictionary<string, object?>>? rows = null) => filter switch
    {
        TextFilter t => $"zawiera „{t.Phrase}”",
        ListFilter l when l.Values.Count == 0 => "nic nie wybrano",
        ListFilter l when column.Format is not null && rows is not null => string.Join(", ", DistinctValues(column, rows)
            .Where(v => l.Values.Contains(v?.ToString() ?? string.Empty))
            .Select(v => Display(column, v))),
        ListFilter l => string.Join(", ", l.Values.Order(Comparer<string>.Create((a, b) => Compare(column, a, b)))),
        NumberFilter n => DescribeRange(n.From is null ? null : TypeDisplay(column, n.From), n.To is null ? null : TypeDisplay(column, n.To)),
        DateFilter d => DescribeRange(d.From is null ? null : Formats.Date(d.From.Value), d.To is null ? null : Formats.Date(d.To.Value)),
        _ => string.Empty,
    };

    private static string DescribeRange(string? from, string? to) => (from, to) switch
    {
        ({ } a, { } b) => $"{a} – {b}",
        ({ } a, null) => $"od {a}",
        (null, { } b) => $"do {b}",
        _ => string.Empty,
    };

    // ── Wiersze po filtrach, w kolejności wyświetlania ──────────────────

    /// <summary>
    /// Zwraca wiersze po wyszukiwaniu i filtrach, w kolejności wyświetlania: najpierw poziomy grupowania, potem kolejne
    /// poziomy sortowania, na końcu wartość kolumny kluczowej.
    /// </summary>
    /// <param name="layout">Układ raportu (fraza, filtry, grupowanie, sortowanie).</param>
    /// <param name="source">Wszystkie wiersze raportu.</param>
    /// <param name="preserveRowOrder">
    /// <see langword="true"/> – porządek stabilny: remisy grupowania i sortowania (a bez nich wszystkie wiersze) zostają
    /// w kolejności wejścia zamiast wg kolumny kluczowej (dopasowanie Rejestru, AD-23 poz. 3).
    /// </param>
    /// <returns>Nowa lista wierszy spełniających wyszukiwanie i wszystkie filtry.</returns>
    /// <remarks>
    /// Kierunek poziomu grupowania jest taki jak kierunek sortowania tej samej kolumny (domyślnie rosnąco).
    /// Szukana fraza porównywana jest z wartościami wyświetlanymi wszystkich kolumn, także ukrytych.
    /// </remarks>
    public static IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows(ReportLayout layout, IEnumerable<IReadOnlyDictionary<string, object?>> source, bool preserveRowOrder = false)
    {
        var definition = layout.Definition;
        var phrase = layout.Search.Trim();
        var result = source.Where(r =>
            (phrase.Length == 0 || definition.Columns.Any(c => Display(c, GetValue(r, c.Key)).Contains(phrase, StringComparison.CurrentCultureIgnoreCase)))
            && layout.Filters.All(f => definition.Column(f.Key) is not { } c || Matches(c, f.Value, GetValue(r, f.Key))));

        // Najpierw poziomy grupowania (kierunek jak w sortowaniu tej kolumny, domyślnie rosnąco), potem kolejne poziomy sortowania
        var ordering = layout.Grouping
            .Select(g => (Column: definition.Column(g), Descending: layout.Sorting.FirstOrDefault(s => s.Key == g)?.Descending ?? false))
            .Concat(layout.Sorting.Select(s => (Column: definition.Column(s.Key), s.Descending)))
            .Where(p => p.Column is not null)
            .ToList();
        if (preserveRowOrder)
        {
            // Stabilnie: remis rozstrzyga indeks wejścia (List.Sort jest niestabilny)
            var indexed = result.Select((row, index) => (Row: row, Index: index)).ToList();
            indexed.Sort((a, b) =>
            {
                var cmp = CompareBy(ordering, a.Row, b.Row);
                return cmp != 0 ? cmp : a.Index.CompareTo(b.Index);
            });
            return indexed.Select(p => p.Row).ToList();
        }

        var keyColumn = definition.Columns.FirstOrDefault(c => c.IsKey) ?? definition.Columns[0];

        var list = result.ToList();
        list.Sort((a, b) =>
        {
            foreach (var (column, descending) in ordering)
            {
                var cmp = Compare(column!, GetValue(a, column!.Key), GetValue(b, column.Key));
                if (cmp != 0)
                {
                    return descending ? -cmp : cmp;
                }
            }

            return string.CompareOrdinal(GetValue(a, keyColumn.Key)?.ToString(), GetValue(b, keyColumn.Key)?.ToString());
        });
        return list;
    }

    private static int CompareBy(List<(ReportColumn? Column, bool Descending)> ordering, IReadOnlyDictionary<string, object?> a, IReadOnlyDictionary<string, object?> b)
    {
        foreach (var (column, descending) in ordering)
        {
            var cmp = Compare(column!, GetValue(a, column!.Key), GetValue(b, column.Key));
            if (cmp != 0)
            {
                return descending ? -cmp : cmp;
            }
        }

        return 0;
    }

    /// <summary>Dzieli wiersze na grupy jednego poziomu grupowania.</summary>
    /// <param name="layout">Układ raportu (kolumny grupujące).</param>
    /// <param name="rows">Wiersze ułożone już wg kolumn grupujących (wynik <see cref="Rows"/> albo wiersze grupy nadrzędnej).</param>
    /// <param name="level">Poziom grupowania liczony od 0.</param>
    /// <param name="path">Ścieżka grupy nadrzędnej (<see cref="ReportGroup.Id"/>); pusty tekst dla poziomu 0.</param>
    /// <returns>Grupy w kolejności wierszy; pusta lista, gdy poziom przekracza liczbę poziomów grupowania.</returns>
    public static IReadOnlyList<ReportGroup> Groups(ReportLayout layout, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, int level, string path)
    {
        if (level >= layout.Grouping.Count || layout.Definition.Column(layout.Grouping[level]) is not { } column)
        {
            return [];
        }

        var groups = new List<ReportGroup>();
        foreach (var g in rows.GroupBy(r => Display(column, GetValue(r, column.Key))))
        {
            var groupRows = g.ToList();
            groups.Add(new ReportGroup($"{path}/{column.Key}:{g.Key}", column, g.Key, level, groupRows, GetValue(groupRows[0], column.Key)));
        }

        return groups;
    }

    /// <summary>Tworzy podsumowanie wierszy: liczba pozycji i – gdy definicja ma <see cref="ReportDefinition.Total"/> – suma.</summary>
    /// <param name="definition">Definicja raportu.</param>
    /// <param name="rows">Podsumowywane wiersze (całego raportu albo grupy).</param>
    /// <returns>Np. „12 pozycji · budżet miesięczny: 190 000,00 zł”.</returns>
    public static string Summary(ReportDefinition definition, IReadOnlyCollection<IReadOnlyDictionary<string, object?>> rows)
    {
        var text = PolishPlural.WithCount(rows.Count, "pozycja", "pozycje", "pozycji");
        if (definition.Total is { } total)
        {
            var sum = rows.Sum(r => ToNumber(GetValue(r, total.Key)));
            text += $" · {total.Label}: {(total.IsAmount ? Formats.Amount(sum) : sum.ToString("#,0.##", Culture))}";
        }

        return text;
    }

    // ── Eksport CSV ──────────────────────────────────────────────────────

    /// <summary>
    /// Tworzy treść pliku CSV: widoczne kolumny i wiersze po filtrach w kolejności z ekranu.
    /// </summary>
    /// <param name="layout">Układ raportu (widoczne kolumny i ich kolejność).</param>
    /// <param name="rows">Wiersze po filtrach, w kolejności wyświetlania (wynik <see cref="Rows"/>).</param>
    /// <returns>Treść CSV z wierszem nagłówków.</returns>
    /// <remarks>
    /// Separator „;”, przecinek dziesiętny bez odstępów tysięcy, daty dd.mm.rrrr, końce wierszy CRLF; pola ze średnikiem,
    /// cudzysłowem lub końcem wiersza są ujmowane w cudzysłów. BOM UTF-8 dokłada pobranie pliku (<c>EodUI.downloadFile</c>).
    /// </remarks>
    public static string ToCsv(ReportLayout layout, IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var columns = layout.Visible;
        var sb = new StringBuilder();
        sb.Append(string.Join(';', columns.Select(c => Field(c.Label)))).Append("\r\n");
        foreach (var r in rows)
        {
            sb.Append(string.Join(';', columns.Select(c => Field(CsvValue(c, GetValue(r, c.Key)))))).Append("\r\n");
        }

        return sb.ToString();

        static string Field(string s) => s.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }

    private static string CsvValue(ReportColumn column, object? value) => column.Type switch
    {
        ColumnType.Date => ToDate(value) is { } d ? Formats.Date(d) : string.Empty,
        ColumnType.Number when column.IsAmount => ToNumber(value).ToString("0.00", Culture),
        ColumnType.Number => ToNumber(value).ToString("0.##", Culture),
        _ => value?.ToString() ?? string.Empty,
    };

    private static int IndexOf(IReadOnlyList<string> list, string? value)
    {
        for (var i = 0; i < list.Count; i++)
        {
            if (list[i] == value)
            {
                return i;
            }
        }

        return int.MaxValue;
    }
}
