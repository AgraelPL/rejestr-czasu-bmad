---
title: 'ReportGrid: lista z filtrem, licznikiem i ikonami akcji'
type: 'feature'
ticket: '5'
created: '2026-10-31'
status: 'built'
baseline_revision: '872b6ae2c4c8bb9ced1ae689bc415bce454c5c9d'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Listy Słowników, Lista wpisów i ekran Użytkownicy potrzebują jednej siatki z filtrem wartości w każdej kolumnie, licznikiem i ikonami akcji (R17, AD-9). Bez niej każdy ekran zbudowałby własną.

**Approach:** `Shared/ReportGrid<T>` jako nakładka na `FluentDataGrid` z Fluent UI 5.0.1:
- ekran podaje wiersze (`IReadOnlyList<T>`), kolumny i akcje;
- siatka tylko ukrywa wiersze filtrem;
- licznik pokazuje wiersze widoczne po filtrze;
- kliknięcie ikony wywołuje akcję ekranu z wierszem.

## Boundaries & Constraints

**Always:**
- **Parametry `ReportGrid<T>`:**
  - `Items` jako `IReadOnlyList<T>`, nigdy `IQueryable`;
  - `Columns`: lista `ReportColumn<T>` (tytuł i `Func<T, string>` z tekstem komórki, który służy też do filtra);
  - `Actions`: lista `ReportAction<T>` (ikona Fluent, polska etykieta jako `title` i `aria-label`, opcjonalny warunek widoczności `Func<T, bool>`, wywołanie `Func<T, Task>`).
- **Kolejność:** wiersze zawsze w kolejności wejścia; siatka nie sortuje (kolumny bez sortowania) i nie liczy sum.
- **Filtr wartości** (wzorzec „zaznacz wartości”):
  - każda kolumna ma w opcjach kolumny (`ColumnOptions`) listę pól wyboru z różnymi wartościami tej kolumny, posortowanymi `pl-PL`, plus „Zaznacz wszystkie”;
  - odznaczenie ukrywa wiersze z tą wartością; filtry kolumn łączą się przez AND;
  - kolumna z aktywnym filtrem ma ustawione `Filtered`;
  - siatka pamięta odznaczone wartości, więc po nowym `Items` (np. po edycji) nowa wartość jest widoczna, a odznaczone zostają ukryte.
- **Licznik pod siatką:** „Pozycje: N”, a przy aktywnym filtrze „Pozycje: N z M”.
- **Pusta lista:** zostaje tekst `DataGrid_EmptyContent` z `FluentLocalizer`.
- **Ikony:** z pakietu `Microsoft.FluentUI.AspNetCore.Components.Icons` (SVG w komponencie, bez zasobów z sieci, AD-12).
- **Teksty:** wszystkie po polsku (AD-6).

**Never:**
- Grupowanie i sumy (historia 1.6).
- Sortowanie przez Usera, wyszukiwarka, wybór kolumn, edycja w siatce, eksport CSV (poza v1, AD-9).
- Ekran Słownika (1.7).
- Komponenty z AD-12 (`FluentNumberInput`, `MaskPattern`, `FluentSortableList`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Kolejność wejścia | wiersze C, A, B | wiersze w kolejności C, A, B | — |
| Licznik bez filtra | 3 wiersze | „Pozycje: 3” | — |
| Filtr ukrywa | odznaczona wartość „A” w kolumnie Kod | wiersz A ukryty, „Pozycje: 2 z 3”, kolumna oznaczona jako filtrowana | — |
| Dwie kolumny | filtr w kolumnie 1 i 2 | widoczne tylko wiersze spełniające oba | — |
| Zaznacz wszystkie | „A” odznaczona, potem zaznaczenie „Zaznacz wszystkie” w tej kolumnie | wszystkie wiersze z powrotem, „Pozycje: 3”; odznaczenie „Zaznacz wszystkie” ukrywa wszystkie wartości kolumny (jak w Excelu, decyzja Autora 31.10.2026) | — |
| Nowe `Items` | „A” odznaczona, ekran podaje listę z nowym wierszem „D” | D widoczny, A nadal ukryty | — |
| Akcja | klik ikony Edytuj w wierszu B | wywołanie akcji ekranu z obiektem B | — |
| Warunek widoczności | akcja z warunkiem fałszywym dla wiersza A | brak ikony w wierszu A | — |
| Pusta lista | `Items` puste | polski komunikat o braku danych, „Pozycje: 0” | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Shared/`:
  - nowy folder z `ReportGrid.razor`, `ReportColumn.cs`, `ReportAction.cs`;
  - przestrzeń nazw `RejestrCzasu.Shared`, dopisana w `src/RejestrCzasu/_Imports.razor`.
  - `_Imports.razor` ma `@attribute [Authorize]`; dla komponentu bez `@page` to bez znaczenia.
- **API Fluent UI 5.0.1** (dokumentacja XML w `~/.nuget/packages/microsoft.fluentui.aspnetcore.components/5.0.1/lib/net10.0/*.xml`):
  - `FluentDataGrid<T>.Items` przyjmuje `IQueryable<T>`, więc przefiltrowaną listę przekazać przez `AsQueryable()`;
  - `TemplateColumn<T>` / `PropertyColumn`, `ColumnBase.Sortable`, `ColumnOptions` (fragment w nagłówku), `Filtered`, `EmptyContent`;
  - API v5 różni się od v4: sprawdzać w XML, nie z pamięci.
- **Ikony:** pakiet `Microsoft.FluentUI.AspNetCore.Components.Icons` 5.0.0 jest w lokalnym cache NuGet; dodać go do `src/RejestrCzasu/RejestrCzasu.csproj`.
- **Teksty:** `src/RejestrCzasu/Localization/FluentLocalizer.cs` ma polskie teksty `DataGrid_*`. `tests/RejestrCzasu.Tests/PolishTextsTests.cs` sprawdza, że każdy klucz biblioteki jest przetłumaczony; nowy pakiet ikon nie może zostawić kluczy bez tłumaczenia.
- **Testy:**
  - `tests/RejestrCzasu.Tests/HomePageTests.cs` to wzorzec bUnit (`BunitContext`, `JSInterop.Mode = Loose`, `Services.AddFluentUIComponents()`);
  - pakiet `bunit` 2.13.3 jest w projekcie testów;
  - opcje kolumny otwiera przycisk w nagłówku; jeśli bUnit nie wyrenderuje popovera, test może wywołać `ShowColumnOptionsAsync` na znalezionej siatce.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Shared/ReportGrid.razor`, `ReportColumn.cs`, `ReportAction.cs`, `_Imports.razor`, `RejestrCzasu.csproj` -- siatka -- AD-9, AD-12.
- [x] `tests/RejestrCzasu.Tests/Shared/ReportGridTests.cs` -- bUnit dla każdego wiersza I/O Matrix.

**Acceptance Criteria:**
- Given wszystkie zmiany, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone.
- Given siatka wyrenderowana w aplikacji, when sprawdzam źródło, then siatka nie ładuje żadnego zasobu spoza aplikacji i nie dodaje skryptu inline (CSP z AD-12 bez zmian).

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone

Uruchomiona przez Autora aplikacja może blokować `src/RejestrCzasu/bin`. Wtedy build i testy uruchamiaj na kopii repozytorium w scratchpadzie i nie zatrzymuj jego procesu.

## Implementation Notes

- Filtr trzyma odznaczone wartości per indeks kolumny; kolumna jest `Filtered`, gdy odznaczona jest co najmniej jedna wartość obecna w bieżących `Items` (licznik „N z M” według tej samej reguły).
- „Zaznacz wszystkie”: zaznaczone, gdy kolumna nie jest filtrowana; zaznaczenie czyści filtr kolumny, odznaczenie ukrywa wszystkie wartości kolumny (wzorzec Excela).
- Pusta wartość komórki ma na liście filtra etykietę „(puste)”. Kolumna akcji ma tytuł „Akcje” i nie ma opcji.
- Pakiet ikon 5.0.0 deklaruje zależność od Components 4.14.4; NuGet wybiera bezpośrednie 5.0.1, build bez ostrzeżeń, ikony renderują się jako SVG w komponencie.
- Testy otwierają opcje kolumny przez `ShowColumnOptionsAsync(index)` i zmieniają pola przez `FluentCheckbox.ValueChanged`.


## Plan Change Log

## Review Triage Log

| # | Lens | Finding | Verdict | Route | Evidence |
|---|------|---------|---------|-------|----------|
| 1 | quick | Brak `ItemKey`: dwa równe rekordy w `Items` dają ten sam `@key` i błąd renderowania | medium | patch | XML 5.0.1: bez `ItemKey` kluczem jest sam obiekt, porównywany przez `Equals`; dwa identyczne Wpisy jako rekordy bez `Id` są realne. Poprawka: klucz z pozycji wejścia przez mapę z `ReferenceEqualityComparer`. |
| 2 | quick | `report-grid-filter` i `report-grid-counter` bez stylów; lista filtra bez układu i limitu wysokości | low | patch | Brak `.razor.css` i reguł w `app.css`; `RejestrCzasu.styles.css` jest podpięty w `App.razor` L11, więc wystarczy `ReportGrid.razor.css`. |
| 3 | quick | Filtr przypięty do indeksu kolumny | low | reject | Ekrany definiują kolumny statycznie; zmiana kolejności kolumn w trakcie życia siatki nie jest przewidziana (Implementation Notes); obsługa dodałaby gałęzie bez konsumenta. |
| 4 | quick | `OnParametersSet` przelicza wszystko przy każdym renderze rodzica | low | reject | Dane w pamięci, rząd dziesiątek–setek wierszy; sprawdzanie referencji to dodatkowy stan bez wykazanej szkody. |
| 5 | quick | `ValueLabel` rzuci `NullReferenceException`, gdy `Text` zwróci `null` | low | patch | `value.Length` przy `null`; poprawka jednowierszowa (`string.IsNullOrEmpty`) plus test etykiety „(puste)”, którego brakowało. |
| 6 | quick | Luki testów: „(puste)”, `Filtered` przy odznaczonym „Zaznacz wszystkie”, brak opcji w kolumnie Akcje, zdarzenia przez DOM | low | patch (część) | Test „(puste)” dochodzi z #5; pozostałe to dodatkowe asercje bez wykazanej usterki — odrzucone. |
| 7 | quick | Pakiet ikon 5.0.0 deklaruje zależność od Components 4.14.4 (`lib/net8.0`) | maybe-false | defer | Testy renderują ikony jako SVG, restore bez ostrzeżeń NU; rozstrzygnie uruchomienie ekranu z ikonami w aplikacji (1.7) albo wydanie pakietu ikon pod v5. |
