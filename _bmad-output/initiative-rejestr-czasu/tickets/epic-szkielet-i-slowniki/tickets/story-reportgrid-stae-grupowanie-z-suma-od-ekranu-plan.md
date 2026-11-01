---
title: 'ReportGrid: stałe grupowanie z sumą od ekranu'
type: 'feature'
ticket: '6'
created: '2026-11-01'
status: 'built'
baseline_revision: 'a0482a34e5536fce00728d6b5e0adf28bb51b011'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-szkielet-i-slowniki/tickets/story-reportgrid-lista-z-filtrem-licznikiem-i-ikonami-akcji-plan.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Widok miesiąca (epik 3) to siatka grupowana Tydzień → Dzień z Sumą tygodnia i Sumą dnia w nagłówkach grup. Sumy liczy `MonthReport` dla całego Miesiąca, więc filtr nie może ich zmieniać (R9, R17, AD-9). `ReportGrid` z 1.5 nie grupuje.

**Approach:** `ReportGrid<T>` dostaje opcjonalne stałe grupowanie zadane przez ekran:
- jeden lub więcej poziomów;
- dla każdego poziomu klucz, tytuł nagłówka i suma podana przez ekran;
- siatka wstawia wiersze nagłówków między wiersze danych. Filtr ukrywa tylko wiersze danych, a grupa bez widocznych wierszy znika.

## Boundaries & Constraints

**Always:**
- **Parametr `Groups`:** lista `ReportGroup<T>` (zewnętrzny poziom pierwszy) z polami:
  - `Key: Func<T, string>`;
  - `Title: Func<string, string>` (klucz → tekst nagłówka);
  - `Sum: Func<string, string>?` (klucz → tekst sumy).
- **Bez grupowania:** gdy `Groups` jest puste, siatka działa dokładnie jak w 1.5; wszystkie testy 1.5 przechodzą bez zmian.
- **Kolejność:** grupy w kolejności pierwszego wystąpienia klucza w `Items`; wiersze w grupie w kolejności wejścia. Siatka nie sortuje.
- **Suma:** siatka nigdy jej nie liczy, tylko wyświetla tekst `Sum(klucz)` w kolumnie wskazanej parametrem `GroupSumColumn` (indeks w `Columns`, domyślnie ostatnia kolumna danych).
- **Nagłówek grupy:**
  - tytuł w pierwszej kolumnie;
  - bez ikon akcji i bez treści w pozostałych kolumnach;
  - klasa wiersza zależna od poziomu (wcięcie i wyróżnienie w `ReportGrid.razor.css`).
- **Filtr:**
  - lista wartości i liczenie biorą tylko wiersze danych;
  - nagłówek zostaje, gdy grupa ma choć jeden widoczny wiersz danych;
  - tekst sumy się nie zmienia.
- **Licznik** liczy tylko wiersze danych („Pozycje: N z M” jak w 1.5).
- **Klucze wierszy w `FluentDataGrid` są unikalne:**
  - nagłówek: poziom i ścieżka kluczy grup;
  - wiersz danych: pozycja wejścia, jak w 1.5.
- **Akcja** wiersza danych dostaje obiekt `T`, nigdy wiersz nagłówka.

**Never:**
- Zwijanie i rozwijanie grup, grupowanie wybierane przez Usera, sortowanie, sumy liczone w siatce (AD-9).
- `MonthReport` i Widok miesiąca (epik 3).
- Wiersze hierarchiczne `FluentDataGrid` (`HierarchicalGridItem`): nie pasują do stałych grup z sumą od ekranu.

## I/O & Edge-Case Matrix

Dane przykładowe: Wpisy w tygodniach T1 (dni D1, D2) i T2 (dzień D3), sumy podane przez test: T1 „10:00”, T2 „3:00”, D1 „6:00”, D2 „4:00”, D3 „3:00”.

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Dwa poziomy | grupy Tydzień → Dzień | kolejno: T1 „10:00”, D1 „6:00”, wiersze D1, D2 „4:00”, wiersze D2, T2 „3:00”, D3 „3:00”, wiersze D3 | — |
| Filtr ukrywa wiersze | odznaczona wartość ukrywająca część wierszy D1 | wiersze ukryte; nagłówki D1 i T1 z tymi samymi sumami „6:00” i „10:00” | — |
| Pusta grupa znika | filtr ukrywa wszystkie wiersze D2 | brak nagłówka D2; T1 nadal z „10:00” | — |
| Pusta grupa nadrzędna | filtr ukrywa wszystkie wiersze T2 | brak nagłówków T2 i D3 | — |
| Licznik | jak w „Filtr ukrywa wiersze” | „Pozycje: N z M” liczone tylko z wierszy danych | — |
| Kolumna sumy | `GroupSumColumn` wskazuje kolumnę Czas | sumy w komórkach tej kolumny w wierszach nagłówków | — |
| Kolejność pierwszego wystąpienia | `Items` z kluczami T2 przed T1 | T2 przed T1 | — |
| Akcje | klik ikony w wierszu danych | akcja z obiektem `T`; wiersze nagłówków bez ikon | — |
| Lista wartości filtra | grupowanie włączone | tylko wartości z wierszy danych | — |
| Bez grup | `Groups` puste | zachowanie i testy 1.5 bez zmian | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Shared/ReportGrid.razor`:
  - dziś `FluentDataGrid TGridItem="T"` z `Items="@_visibleQuery"` i `ItemKey="@RowKey"` (pozycja wejścia przez `ReferenceEqualityComparer`);
  - filtr `_excluded` po indeksie kolumny, `ApplyFilter()`, `Counter`;
  - kolumny `TemplateColumn` z `ColumnOptions` (lista pól wyboru), kolumna „Akcje”.
  - Grupowanie najprościej zrobić przez wewnętrzny typ wiersza (nagłówek albo wiersz danych) jako `TGridItem`. Kolumny renderują wtedy treść zależnie od rodzaju wiersza, a `RowClass` nadaje klasę poziomu.
- `src/RejestrCzasu/Shared/ReportGrid.razor.css`: izolowane style (`RejestrCzasu.styles.css` jest podpięty w `Components/App.razor`).
- `src/RejestrCzasu/Shared/ReportColumn.cs`, `ReportAction.cs`: wzorzec rekordów publicznego API; `ReportGroup.cs` analogicznie.
- `tests/RejestrCzasu.Tests/Shared/ReportGridTests.cs`:
  - 13 testów 1.5, które mają przejść bez zmian;
  - opcje kolumny otwierają się przez `ShowColumnOptionsAsync(index)`, a pola zmieniają przez `FluentCheckbox.ValueChanged`.
- Implementation Notes planu 1.5 (w `context:`): decyzje o filtrze, „Zaznacz wszystkie”, „(puste)” i kolumnie „Akcje” obowiązują dalej.
- **API Fluent UI 5.0.1:** dokumentacja XML w `~/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.1/lib/net10.0/`. `FluentDataGrid` ma `RowClass` i `RowStyle`; nie ma wbudowanego grupowania.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Shared/ReportGroup.cs`, `ReportGrid.razor`, `ReportGrid.razor.css` -- stałe grupowanie -- AD-9.
- [x] `tests/RejestrCzasu.Tests/Shared/ReportGridGroupingTests.cs` -- bUnit dla każdego wiersza I/O Matrix na danych przykładowych.

**Acceptance Criteria:**
- Given wszystkie zmiany, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone, w tym niezmienione testy 1.5.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone

Uruchomiona przez Autora aplikacja może blokować `src/RejestrCzasu/bin`. Wtedy build i testy uruchamiaj na kopii repozytorium w scratchpadzie i nie zatrzymuj jego procesu.

## Implementation Notes

- Bez grup siatka zostaje `FluentDataGrid<T>` (testy 1.5 szukają `FluentDataGrid<Row>` i `TemplateColumn<Row>`), z grupami `FluentDataGrid<ReportGrid<T>.GridRow>` (wewnętrzny typ: nagłówek albo wiersz danych). Jeden znacznik siatki w generycznej metodzie `Grid<TRow>`, więc filtr, „Zaznacz wszystkie” i kolumna „Akcje” nie są zdublowane.
- Filtr liczy widoczność po pozycji wejścia; nagłówki budowane rekurencyjnie w kolejności pierwszego wystąpienia klucza w całych `Items` (nie tylko widocznych), więc kolejność grup nie skacze przy filtrze.
- Klucze wierszy: nagłówek `HeaderKey(poziom, ścieżka kluczy)` (separator U+001F), wiersz danych `int` pozycji wejścia.
- `GroupSumColumn` to `int?`, `null` = ostatnia kolumna danych. Gdy wskazuje kolumnę 0, tytuł i suma są w tej samej komórce jako dwa `span`.
- Siatka i licznik są opakowane w `div.report-grid`, żeby izolowane style dosięgły `tr` renderowanych przez `FluentDataGrid` (`::deep`). Klasy nagłówka: `report-grid-group report-grid-group-{poziom}`; tokeny kolorów `--colorNeutralBackground2/3` z Fluent UI.
- Testy grupowania otwierają opcje przez `FluentDataGrid<ReportGrid<Row>.GridRow>.ShowColumnOptionsAsync` (InternalsVisibleTo).

## Plan Change Log

## Review Triage Log

| # | Lens | Finding | Verdict | Route | Evidence |
|---|------|---------|---------|-------|----------|
| 1 | quick | Ścieżka nagłówka łączona znakiem U+001F może kolidować | low | reject | Kolizja wymaga U+001F w kluczu grupy; klucze dają ekrany (daty, tygodnie), znak sterujący w nich nie występuje. Strukturalny klucz to dodatkowy typ bez wykazanej szkody. |
| 2 | quick | Przy `GroupSumColumn = 0` tytuł i suma zlewają się w jednej komórce | low | patch | Oba `span` w jednej komórce bez odstępu; brak reguły dla `.report-grid-group-sum`. Poprawka: odstęp w CSS. |
| 3 | quick | Zły `GroupSumColumn` po cichu ukrywa sumy | low | reject | Błąd konfiguracji ekranu widoczny od razu w jego testach; walidacja to dodatkowa gałąź (guard). |
| 4 | quick | Style nagłówków tylko dla dwóch poziomów | low | reject | Jedyny konsument (Widok miesiąca) ma dwa poziomy; reguły dla kolejnych poziomów bez konsumenta. |
| 5 | quick | `FilterValues_ComeOnlyFromDataRows` nie może się nie powieść | low | patch | `_values` liczone z `Items`; test nie sprawdza wartości, która mogłaby przeciec z nagłówka. Poprawka: asercja, że lista wartości kolumny sumy nie zawiera sum nagłówków („10:00”, „6:00”). |
| 6 | quick | Dodatkowy `div.report-grid` także bez grup | false | reject | Zmiana znacznika bez zmiany działania; testy 1.5 bez zmian przechodzą, a żaden kod ani CSS w `src/` nie zależy od starej struktury. |
