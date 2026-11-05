---
title: 'Eksportuj i paginacja na listach Wpisów'
type: 'feature'
ticket: ''
created: '2026-11-04'
status: 'built'
baseline_revision: 'ec5a67bfe62e8138823af5019c9c2bb34aa5eda8'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/docs/ui/DOPASOWANIA.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/plan-7-8-dopracowanie-wpisow-po-tescie.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Listy Wpisów (`/wpisy` i lista bieżącego Miesiąca na `/wpisy/nowy`) przewijają się w całości i nie dają pliku z Wpisami; kopia `EodReport` nie ma paginacji, a jej eksport CSV pisze Czas jako surowe minuty i jest w Rejestrze wyłączony (R17).

**Approach:** W kopii `EodReport` dodać opcjonalną paginację (`PageSize`), przycisk „Eksportuj” zamiast „Eksport CSV” oraz wartość i nagłówek kolumny tylko dla eksportu (`ReportColumn.ExportFormat`, `ExportLabel`) — AD-23 poz. 11–13; `EntriesReport` (z planu 7.8) włącza eksport i 30 wierszy na stronę. **Zależy od planu 7.8** (`EntriesReport`, lista na `/wpisy/nowy`, kaskada stylów).

**Decyzje (Autor, 2026-11-04):**
- Eksport obejmuje wszystkie wiersze aktualnie spełniające wszystko, co zawęża listę — filtry ekranu (`/wpisy`: Od/Do/Projekt/Typ pracy; `/wpisy/nowy`: zawsze bieżący Miesiąc), filtry kolumn i „Szukaj we wszystkich kolumnach” — ze wszystkich stron.
- 30 wierszy na stronę na obu listach; Słowniki bez eksportu i paginacji.
- Czas w pliku w godzinach dziesiętnych z przecinkiem („1,5”, „0,75”), jak kolumna „Godziny” Zestawienia; nagłówek kolumny w pliku „Godziny”.
- Plik `wpisy-RRRR-MM-DD.csv` (data eksportu w Polsce).
- Osobny ekran „Eksport” (epik 3) odpada; eksport jest tylko na tych dwóch listach, Zestawienie xlsx zostaje w Widoku miesiąca.

## Boundaries & Constraints

**Always:** eod-ui bez zmian; każda zmiana w `Ui/`/`wwwroot/ui/` = pozycja AD-23 w architekturze i linia w `docs/ui/DOPASOWANIA.md` w tej samej zmianie; `EodReport` bez `PageSize`, `ExportFormat` i `ExportLabel` działa jak dotąd; stopka (licznik, `FooterSummary`) i eksport liczą wszystkie wiersze po filtrach, nie stronę (AD-5); `DataAsOf` z `PolishCalendar.Today`.

**Never:** zmiany `EntryStore`, `EntryDialogs`, `EntryForm`; eksport przez ekran zamiast przez `EodReport` (AD-9); xlsx; edycja ticketów, PRD i initiative (tylko notatka dla PO niżej).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Paginacja | 95 wierszy po filtrach | 30 na stronę, „Strona 1 z 4”, zakres „1–30 z 95”, stopka dla 95 | ≤ 30: bez paginatora |
| Zmiana zawężenia | strona 3, zmiana szukania, filtra kolumny, sortowania lub filtra ekranu | strona 1 | — |
| Nowe wiersze | usunięcie ostatniego wiersza strony 4 lub zapis na Nowym wpisie | bieżąca strona, przycięta do ostatniej istniejącej | — |
| Grupowanie Usera | dowolny poziom | bez paginacji, całe grupy | — |
| Eksportuj `/wpisy` | filtry ekranu + filtr kolumny + szukanie, 95 wierszy na 4 stronach | plik z 95 wierszami w kolejności ekranu, widoczne kolumny, bez Akcje i Id; Data `dd.mm.rrrr`, „Godziny” `1,5`; `;`, UTF-8 z BOM, CRLF; pasek „Wyeksportowano raport. wpisy-2026-11-04.csv: 95 wierszy, 6 kolumn.” | — |
| Eksportuj `/wpisy/nowy` | bieżący Miesiąc (+ ewent. filtr kolumny/szukanie) | jak wyżej, tylko Wpisy bieżącego Miesiąca | — |
| Pusta lista | 0 wierszy | plik z samym nagłówkiem (zachowanie wzorca) | — |
| Opis z `;`, `"`, nową linią | tekst Opisu | pole w cudzysłowie, `"` podwojony (jak `ToCsv`) | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Ui/Reports/EodReport.razor(.cs)` -- `ShowExport`, `ExportAsync` (l. 234: `_filteredRows` = po szukaniu i filtrach kolumn, wszystkie wiersze; plik `{Definition.FileName}-{DataAsOf:yyyy-MM-dd}.csv`, `DataAsOf` domyślnie `default`), `RenderLevel(_filteredRows…)`, stopka `FooterText`; tu `PageSize`, paginator, etykieta.
- `src/RejestrCzasu/Ui/Reports/ReportDefinition.cs` -- `ReportColumn` (`Format` l. 86); tu `ExportFormat`, `ExportLabel`.
- `src/RejestrCzasu/Ui/Reports/ReportEngine.cs:329-350` -- `ToCsv` (nagłówek `c.Label`), `CsvValue` (kolumna `List` → `value.ToString()`, czyli Czas „90”).
- `wwwroot/ui/eod.js:56` `downloadFile` (BOM), `wwwroot/ui/eod.css:146` `.eod-paginator` -- gotowe, bez zmian.
- `C:\Users\kamil\Documents\eod-ui\src\Eod.Demo\Pages\Admin\Users.razor:128-217` -- wzór paginatora (tylko do czytania): `PaginationState { ItemsPerPage }`, `FluentPaginator Class="eod-paginator"` z `SummaryTemplate`/`PaginationTextTemplate`, `SetCurrentPageIndexAsync(0)`; Fluent 5.0.1 ma `SetTotalItemCountAsync`, `TotalItemCountChanged`.
- `src/RejestrCzasu/Domain/Czas.cs:73` -- `ToDecimalHours(int)`.
- `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor` (z 7.8) -- `Report` i parametry `EodReport`.
- `src/RejestrCzasu/Features/Pomoc/Topics/WpisyHelp.razor`, `WpisyNowyHelp.razor` -- treść.
- `tests/RejestrCzasu.Tests/Ui/EodReportTests.cs:48,56`, `Features/EntriesPageTests.cs:82`, `ProjectsPageTests.cs:62`, `WorkTypesPageTests.cs:62` -- asercje „Eksport CSV”.
- `_bmad-output/.../architecture-rejestr-czasu.md` AD-9 („Eksport CSV wyłączony … na każdym raporcie”), AD-23 lista (1) „Rejestr zawsze `false`”.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Ui/Reports/ReportDefinition.cs`, `ReportEngine.cs` -- `ReportColumn.ExportFormat` (`Func<object?, string>?`) i `ExportLabel` (`string?`); `ToCsv` bierze `ExportLabel ?? Label` i `ExportFormat` przed dotychczasowym `CsvValue` -- AD-23 poz. 13.
- [x] `src/RejestrCzasu/Ui/Reports/EodReport.razor(.cs)` -- `[Parameter] int? PageSize`; przy `PageSize` i braku grupowania `RenderLevel` dostaje tylko bieżącą stronę `_filteredRows`; `FluentPaginator` (`eod-paginator`, teksty jak w demo Users) między tabelą a stopką, gdy wierszy > `PageSize`; zmiana szukania/filtra/sortu/grupowania → strona 0; nowe `Rows` → przycięcie strony; etykieta „Eksport CSV” → „Eksportuj” -- AD-23 poz. 11, 12.
- [x] `docs/ui/DOPASOWANIA.md` -- poz. 1: Słowniki `false`, listy Wpisów `true`; nowe 11 (`PageSize`, bez paginacji przy grupowaniu; UI_SPEC 9.7, 12 — odstępstwo od „raport przewija się w całości”), 12 („Eksportuj”, UI_SPEC 9.7 „Eksport CSV”), 13 (`ExportFormat`/`ExportLabel`, UI_SPEC 9.7).
- [x] `_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md` -- AD-23: (1) przepisana, (11)–(13) dopisane; AD-9: eksport na Liście wpisów i liście Nowego wpisu (wszystko, co zawęża listę), paginacja 30, Słowniki bez eksportu.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor` -- `ShowExport="true"`, `PageSize="30"`, `DataAsOf="PolishCalendar.Today(TimeProvider)"`; kolumna Data `ExportFormat = EntryMessages.Date`, Czas `ExportLabel = "Godziny"`, `ExportFormat = v => Czas.ToDecimalHours((int)v!).ToString("0.##", pl-PL)` -- decyzje Autora.
- [x] `src/RejestrCzasu/Features/Pomoc/Topics/WpisyHelp.razor`, `WpisyNowyHelp.razor` -- „Eksportuj” (zakres: wszystko, co zawęża listę, wszystkie strony; plik CSV dla Excela, Czas w kolumnie „Godziny” dziesiętnie) i strony po 30 -- R26.
- [x] `tests/RejestrCzasu.Tests/Ui/EodReportTests.cs` -- paginacja (31 wierszy → 2 strony, licznik 31, reset po filtrze, przycięcie, brak przy grupowaniu, brak bez `PageSize`), „Eksportuj”, `ExportFormat`/`ExportLabel` w `ToCsv`, eksport wszystkich stron po filtrze kolumny (wywołanie `EodUI.downloadFile` w bUnit `JSInterop`).
- [x] `tests/RejestrCzasu.Tests/Features/EntriesPageTests.cs`, `NewEntryPageTests.cs`, `ProjectsPageTests.cs`, `WorkTypesPageTests.cs` -- „Eksportuj” na listach Wpisów (nazwa pliku z datą polską, „Godziny” `1,5`), brak „Eksportuj” na Słownikach.

**Acceptance Criteria:**
- Given Lista wpisów z filtrem ekranu, filtrem kolumny i szukaniem, when Eksportuj, then plik otwiera się w Excelu z polskimi znakami, ma tylko pasujące wiersze ze wszystkich stron, a kolumnę „Godziny” da się zsumować.
- Given `git diff`, when przegląd, then każda zmiana w `Ui/` ma linię w `DOPASOWANIA.md` i pozycję AD-23.

## Notatka dla PO (tickety, bez edycji)

- `epic-miesiac-i-zestawienie` (epik 3; R12/FR-12, NFR-6, AD-11 menu): ekran „Eksport” i jego pozycja w menu (`Shared/MainLayout.razor:58`) odpadają (decyzja Autora 2026-11-04); Zestawienie xlsx zostaje w Widoku miesiąca.
- R17 „brak eksportu CSV”: `initiative-rejestr-czasu.md` R17 i decyzja z 02.11.2026, PRD FR-16 („Siatka nie ma eksportu CSV”) i „Poza zakresem”, `epic-wzorzec-ui` R1 i Done when — do przepisania: eksport CSV na Liście wpisów i liście Nowego wpisu.
- Zapisane w `deferred-work.md`.

## Implementation Notes

- **Paginacja w `EodReport`:** `PaginationState` (Fluent 5.0.1) zmienia stan tylko metodami async, a jedynym subskrybentem jest `FluentPaginator` (odświeżenie bez kodu ekranu), więc zadania kończą się synchronicznie; `Refresh(firstPage)` woła je przez `Run` (błąd zgłoszony od razu nie ginie). `Refresh(firstPage: true)` przy szukaniu, filtrach, sortowaniu, grupowaniu i „Przywróć układ domyślny”; `OnParametersSet` (nowe `Rows`) tylko przycina stronę; ukrycie/przestawienie kolumny nie zmienia strony. Teksty paginatora jak w demo Users: „Pozycje 1–30 z 95”, „Strona 1 z 4”.
- **Filtr ekranu → strona 1:** `EodReport` nie odróżni nowych `Rows` po zmianie filtra ekranu od przeładowania po akcji wiersza, więc `EntriesPage` daje `EntriesReport` `@key` z wartości filtrów ekranu (nowy komponent zaczyna od strony 1; układ Usera zostaje w `ReportLayoutStore`). Bez zmian w `Ui/` poza AD-23 poz. 11–13.
- **Nazwa pliku:** `MonthReport.FileName` = `wpisy`, więc oba eksporty dają `wpisy-RRRR-MM-DD.csv`; `DataAsOf` z `PolishCalendar.Today(TimeProvider)` w `EntriesReport`.
- Architektura: AD-11 nadal wymienia pozycję menu „Eksport” — zostawione dla PO (notatka niżej), plan zmienia tylko AD-9 i AD-23.
- Weryfikacja: `dotnet build RejestrCzasu.sln` 0/0; `dotnet test` 576/576. Kontrole ręczne (Excel, przeglądarka) niewykonane — w teście całości Autora.

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `EntriesReport.razor:15` `DataAsOf` | nazwa pliku z datą z ostatniego renderu (po północy bez renderu — dzień wcześniej) | low | rzadkie; poprawka wymaga nowego mechanizmu w `EodReport` | reject |
| quick | DOPASOWANIA poz. 1 / AD-23 (1) vs AD-9 | różne sformułowanie reguły `ShowExport` | low | AD-9 wymienia Konta i Widok miesiąca, pozostałe tylko Słowniki | patch |
| quick | `EodReport.razor.cs` `Run(Task)` | komentarz obiecuje więcej niż kod; założenie Fluent 5.0.1 nieopisane | low | niezakończone zadanie nie jest obserwowane | patch |
| quick | `EntriesPage.razor` `@key` + `ReloadAsync` | po zmianie filtra ekranu chwilowo stare wiersze, także w Eksportuj | low | nowy `EntriesReport` dostaje stare `_entries` z `Loading` false do powrotu zapytania | patch |
| quick | `WpisyNowyHelp.razor`, `WpisyHelp.razor` | lista akcji wracających na stronę 1 niepełna | low | kod wraca też przy grupowaniu i „Przywróć układ domyślny” | patch |
| quick | `EodReportTests.cs` | brak testów powrotu na stronę 1 przy grupowaniu i resecie układu | low | `Refresh(firstPage: true)` w tych ścieżkach bez pokrycia | patch |

## Design Notes

Paginacja tylko bez grupowania: strona przecinająca grupy dałaby nagłówki grup z licznikami części grupy, a sumy grup (`GroupSummary`) z całości. Eksport z `_filteredRows` zostaje bez zmian — już spełnia decyzję o zakresie.

```csharp
new(nameof(EntryRow.Minutes), "Czas", ColumnType.List)
{
    Format = v => Czas.Format((int)v!),
    ExportLabel = "Godziny",
    ExportFormat = v => Czas.ToDecimalHours((int)v!).ToString("0.##", Polish),
},
```

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)

**Manual checks (if no CLI):**
- Raz na końcu (Autor, razem z 7.8): paginacja na obu listach; plik z „Eksportuj” z `/wpisy` (z filtrem kolumny) i z `/wpisy/nowy` otwarty w Excelu; Słowniki bez „Eksportuj”.
