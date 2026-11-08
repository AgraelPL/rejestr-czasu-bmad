---
title: 'Widok miesiąca z sumami i Wierszami zestawienia z MonthReport'
type: 'feature'
ticket: '1'
created: '2026-11-05'
status: 'built'
baseline_revision: '415bc7698e2f4a2c56ea8fecb6e87f52e15db4c9'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/export-layout.md'
  - '{project-root}/docs/ui/DOPASOWANIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `/miesiac` prowadzi do „nie znaleziono”, a sum Miesiąca i Wierszy zestawienia nie liczy nic; Widok i Zestawienie (historia 3) muszą mieć jedno źródło sum (AD-5, R2).

**Approach:** `Reporting/MonthEntriesQuery` + czysty `MonthReport.Build` z kompletnymi typami (dni, tygodnie, Wpisy w kolejności, Wiersze zestawienia, suma), testowany na zestawie o kształcie września; ekran `/miesiac` (bieżący Miesiąc) na `EodPage` + `EodReport` z grupowaniem Tydzień → Dzień i sumami z `MonthReport`; temat pomocy `miesiac`.

## Boundaries & Constraints

**Always:** Wpisy tylko przez `UserDbContextFactory` (AD-16), jeden kontekst (AD-4); sumy wyłącznie z `MonthReport` (AD-5), filtr kolumny tylko ukrywa wiersze; tydzień pn–nd przez `PolishCalendar.WeekStart`, zakres tylko z dni Miesiąca; kolejność: dzień, kod Projektu, nazwa Typu pracy (porównanie `pl-PL`), `Id`; złączanie po (dzień, `ProjectId`, `WorkTypeId`), opisy rosnąco po `Id`; `ShowExport="false"`, `LockedLayout`, `PreserveRowOrder`, bez `PageSize` i `ExportFile`; nowa potrzeba wobec wzorca = nowa pozycja AD-23 + linia w `DOPASOWANIA.md`.

**Never:** sumowanie Czasu w ekranie lub zapytaniu; Wiersze zestawienia w Widoku (wiersz = jeden Wpis); wybór Miesiąca, akcje wiersza (historia 4); pisarz xlsx, przycisk, usunięcie menu „Eksport” (historia 3); prawdziwe dane w repozytorium.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Wrzesień 2026 | zestaw testowy (Design Notes) | tygodnie 30/16/18/25/14 h, suma 103 h, Wiersze z sumą dnia/tygodnia tylko przy pierwszym wierszu dnia/tygodnia | — |
| Pusty Miesiąc | brak Wpisów | `Weeks`/`Rows` puste, suma 0; ekran: `EmptyContent` „Brak Wpisów w tym Miesiącu.”, stopka „0 pozycji · Suma Miesiąca: 0h” | — |
| Przełom Miesięcy | Wpisy 31.08 i 01.10 w bazie | `LoadAsync(2026-09)` ich nie zwraca; Tydzień 1 = 01.09–06.09, Tydzień 5 = 28.09–30.09 | Wpis spoza Miesiąca w `Build` → `ArgumentException` |
| Dzień 1 to niedziela | listopad 2026, Wpis 01.11 | „Tydzień 1 (01.11–01.11)”, 02.11 w Tygodniu 2 | — |
| Remisy | ten sam dzień: kody `ABD`/`ABC`, typy `Testy`/`Środowisko`/`Serwis`, wejście w losowej kolejności | `ABC` przed `ABD`; Serwis < Środowisko < Testy; potem `Id` | — |
| Filtr ukrywa cały dzień | filtr Projektu bez Wpisów danego dnia | grupa dnia znika; Suma tygodnia i Miesiąca bez zmian, licznik mniejszy, stopka „· po filtrach, z N” | — |
| Oddzielenie | Wpisy innego Usera w tym Miesiącu | niewidoczne w `LoadAsync` i na ekranie | — |

**Decyzja (Autor, 2026-11-05):** numer tygodnia kalendarzowy w Miesiącu (tydzień z dniem 1 to „Tydzień 1”); tydzień bez Wpisów nie ma grupy, więc numery mogą przeskoczyć (np. „Tydzień 1”, „Tydzień 3”).

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Domain/PolishCalendar.cs`, `YearMonth.cs`, `Czas.cs` -- `WeekStart`, `DayName`, `FirstDay`/`LastDay`/`Contains`/`Name`, `Format` (0 → „0h”).
- `src/RejestrCzasu/Data/TimeEntry.cs`, `UserDbContextFactory.cs` -- encja (`Project.Code`, `WorkType.Name`), kontekst Usera.
- `src/RejestrCzasu/Features/Wpisy/EntryStore.cs:62-84` -- wzór zapytania (`AsNoTracking`, projekcja, materializacja).
- `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor` -- wzór definicji, `ToRow`, `IdKey`, `FooterSummary`; `MonthReport` (l. 68) koliduje nazwą z `Reporting.MonthReport`; użycia: `NewEntryPage.razor:29`, `NewEntryPageTests.cs:149`.
- `src/RejestrCzasu/Ui/Reports/EodReport.razor:432` -- wiersz grupy „`{Column.Label}: {Value}`”, `GroupText` → `GroupSummary(level, group.Key)` z licznikiem; `EodReport.razor.cs:198` stopka; `ReportEngine.Rows/Groups` grupy rosnąco po surowej wartości, wewnątrz kolejność wejścia.
- `src/RejestrCzasu/Ui/Reports/ReportDefinition.cs` -- `ReportColumn` (`Hidden`, `Format`), `Grouping`.
- `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/WpisyHelp.razor` -- lista tematów, wzór treści.
- `src/RejestrCzasu/Features/ScreenServiceCollectionExtensions.cs` -- rejestracja usług ekranów.
- `tests/.../Features/EntriesPageTests.cs`, `EntryStoreTests.cs`, `PostgresFixture.cs` -- wzorce bUnit i bazy.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Reporting/MonthReport.cs` -- typy i `Build` (Design Notes), `static string WeekLabel(DateOnly)`, `DayMinutes(DateOnly)`, `WeekMinutes(DateOnly)` -- jedno źródło sum (AD-5).
- [x] `src/RejestrCzasu/Reporting/MonthEntriesQuery.cs` -- `LoadAsync(YearMonth, CancellationToken)`: Wpisy `FirstDay..LastDay` → `MonthEntry`; rejestracja scoped w `ScreenServiceCollectionExtensions` -- AD-5, AD-16.
- [x] `src/RejestrCzasu/Ui/Reports/ReportDefinition.cs`, `EodReport.razor` -- AD-23 poz. 15: `ReportColumn.GroupLabel` (`bool`, domyślnie `true`); `false` → wiersz grupy tylko z wartością -- nagłówek „Tydzień 1 (01.09–06.09)” bez „Tydzień: ”.
- [x] `docs/ui/DOPASOWANIA.md`, `_bmad-output/.../architecture-rejestr-czasu.md` (AD-23) -- linia i pozycja 15.
- [x] `src/RejestrCzasu/Features/Miesiac/MonthPage.razor` -- `@page "/miesiac"`, Miesiąc z `PolishCalendar.Today`, `PageHelp`, definicja i mapowanie (Design Notes), `LayoutKey="miesiac@RRRR-MM"`, `Loading` do wczytania.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor`, `NewEntryPage.razor`, `tests/.../NewEntryPageTests.cs` -- `EntriesReport.MonthReport` → `CurrentMonth` -- bez kolizji nazw.
- [x] `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/MiesiacHelp.razor` -- temat `miesiac` „Widok miesiąca”: grupy, sumy, filtr nie zmienia sum, tydzień tylko z dni Miesiąca.
- [x] `tests/RejestrCzasu.Tests/Reporting/SeptemberShape.cs`, `MonthReportTests.cs` -- zestaw i macierz (bez bazy).
- [x] `tests/RejestrCzasu.Tests/Reporting/MonthEntriesQueryTests.cs` -- przełom Miesięcy, oddzielenie Userów, projekcja kodu i nazwy.
- [x] `tests/RejestrCzasu.Tests/Features/MonthPageTests.cs` -- bUnit z `FixedTimeProvider` (wrzesień 2026).

**Acceptance Criteria:**
- Given zestaw wrześniowy w bazie, when User otwiera `/miesiac`, then widzi grupy „Tydzień 1 (01.09–06.09)” … „Tydzień 5 (28.09–30.09)” z „N pozycji · Suma tygodnia: 30h” itd., dni „Wtorek, 01.09.2026” z „Suma dnia: …”, stopkę „… · Suma Miesiąca: 103h”, wiersze Projekt/Typ pracy/Opis/Czas w kolejności `MonthReport.Entries`.
- Given ten ekran, when User filtruje kolumnę Projekt, then wiersze i liczniki maleją, a Sumy tygodnia, dnia i Miesiąca są te same.
- Given ekran, then brak przycisku „Eksportuj” i paginatora, nagłówki bez sortowania, `?` prowadzi do `/pomoc/miesiac`, a temat jest w spisie `/pomoc`.

## Implementation Notes

- `MonthPage` w `Features/Miesiac/`; kolumny Week/Day ukryte, `GroupLabel=false`; `Week` = `max(WeekStart(d), FirstDay)`, więc grupy rosną po surowym `DateOnly`.
- Porównanie kodu i nazwy: `StringComparer` `pl-PL` z rozróżnianiem wielkości liter.
- Temat `miesiac` w `HelpTopics.All` zaraz po `start`.

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `MonthPage.razor` `ToRow` | ekran sam liczy tydzień Wpisu (powtórzenie `MonthReport.WeekOf`) | medium | AD-5: tygodnie tylko z `MonthReport`; zmiana reguły rozjechałaby nagłówki i sumy | patch |
| quick | `MonthPage` + `ReportEngine` szukanie | „Szukaj” trafia w ukryte kolumny Tydzień i Dzień | low | zachowanie silnika kopii dla wszystkich ekranów; poprawka zmienia `Ui/` | reject |
| quick | `MonthPageTests` brak paginatora | asercja przechodzi niezależnie od `PageSize` | low | grupowanie wyłącza paginację | patch |
| quick | testy pomocy | brak testu „Widok miesiąca” w spisie `/pomoc` po `start` | low | `HelpTests` porównuje tylko liczbę linków | patch |

## Design Notes

```csharp
namespace RejestrCzasu.Reporting;
public sealed record MonthEntry(int Id, DateOnly Date, int ProjectId, string ProjectCode,
    int WorkTypeId, string WorkTypeName, string Description, int Minutes);
public sealed record MonthDay(DateOnly Date, int Minutes, IReadOnlyList<MonthEntry> Entries);
public sealed record MonthWeek(int Number, DateOnly First, DateOnly Last, int Minutes, IReadOnlyList<MonthDay> Days);
// Wiersz zestawienia: DayMinutes tylko w 1. wierszu dnia, WeekMinutes tylko w 1. wierszu tygodnia.
public sealed record SummaryRow(DateOnly Date, string ProjectCode, string WorkTypeName,
    IReadOnlyList<string> Descriptions, int Minutes, int? DayMinutes, int? WeekMinutes);
public sealed record MonthReport(YearMonth Month, IReadOnlyList<MonthEntry> Entries,
    IReadOnlyList<MonthWeek> Weeks, IReadOnlyList<SummaryRow> Rows, int Minutes);
```

Tylko tygodnie i dni z Wpisami. Numer tygodnia: `(WeekStart(d) − WeekStart(Month.FirstDay)).Days / 7 + 1`; `First = max(WeekStart(d), FirstDay)`, `Last = min(WeekStart(d)+6, LastDay)`; `WeekLabel(d)` liczy to z samej daty (`YearMonth.From(d)`), więc `Format` w statycznej definicji go użyje.

Mapowanie na `EodReport` (`MonthPage.ToRow(MonthEntry)`): `Id`; `Week` = `First` tygodnia (`DateOnly`, `List`, `Hidden`, `GroupLabel=false`, `Format = WeekLabel`); `Day` = `Date` (`List`, `Hidden`, `GroupLabel=false`, `Format` „Wtorek, 01.09.2026”); `ProjectCode` „Projekt”, `WorkTypeName` „Typ pracy”, `Description` „Opis”, `Minutes` „Czas” (`Czas.Format`), wszystkie `List`. `Grouping = [Week, Day]`. `GroupSummary = (level, key) => level == 0 ? $"Suma tygodnia: {Czas.Format(r.WeekMinutes((DateOnly)key!))}" : $"Suma dnia: {…DayMinutes…}"`; `FooterSummary = $"Suma Miesiąca: {Czas.Format(r.Minutes)}"`.

Zestaw `SeptemberShape` (kody `AAA`, `BBB`, `CCC`, typy `Analiza`, `Development`, `Serwis`, `Środowisko`, `Testy`; opisy „Zadanie N”): W1 01–04.09 po 8/8/8/6 h; W2 07–08.09 8/8; W3 14–16.09 6/6/6; W4 21–23.09 8/8/8 + 26.09 (sobota) 1 h; W5 28–29.09 6/8 → 30/16/18/25/14 = 103 h. 31 Wpisów → 28 Wierszy zestawienia: 02.09 dwa Wpisy `AAA`/Development (Id 9 i 4 → opisy w kolejności 4, 9); 15.09 trzy Wpisy `BBB`/Analiza z Wpisem `BBB`/Testy o Id pomiędzy; ten sam `AAA`/Development w różnych dniach się nie złącza. Wejście do `Build` przetasowane.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów.
- `dotnet test` -- expected: wszystkie testy zielone, w tym `MonthReportTests`, `MonthEntriesQueryTests`, `MonthPageTests`, `HelpTests`, `EodReportTests` (poz. 15).

**Manual checks (if no CLI):**
- Lokalnie `/miesiac` z danymi testowymi: grupy, sumy i filtr jak w AC.
