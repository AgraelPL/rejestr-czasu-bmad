---
title: 'Lista wpisów z filtrem zakresu, Projektu i Typu pracy'
type: 'feature'
ticket: '2'
created: '2026-11-01'
status: 'built'
baseline_revision: '0ff6fc0158197b0f3e9d86eff2d6003e828d6c02'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-wpisy-i-lista/epic-wpisy-i-lista.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/glossary.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Pozycja menu Lista wpisów (`/wpisy`) prowadzi do „nie znaleziono”; User nie widzi zapisanych Wpisów ani ich sumy Czasu (R8, R17, R2, R26).

**Approach:** Strona `Features/Wpisy/EntriesPage.razor` na `/wpisy` z filtrami ekranu (zakres dat, Projekt, Typ pracy) nad `ReportGrid<EntryRow>`; dane przez nową metodę `EntryStore.ListAsync(EntryFilter)` (zapytanie z filtrami w bazie, wynik zmaterializowany); liczba Wpisów i suma Czasu liczone przez ekran z wyniku zapytania; temat pomocy `wpisy`.

## Boundaries & Constraints

**Always:**
- **Kolumny** (w tej kolejności): Data (`dd.MM.yyyy`), Dzień (`PolishCalendar.DayName`), Projekt (kod), Typ pracy (nazwa), Opis, Czas (`Czas.Format`, np. `1h 30m`). Filtr wartości kolumn i licznik pozycji daje `ReportGrid` bez zmian.
- **Sortowanie:** data malejąco, w obrębie dnia `Id` malejąco (najnowszy zapis wyżej); robi to `EntryStore`, siatka zachowuje kolejność.
- **Filtry ekranu:** „Od” i „Do” jako `FluentDatePicker<DateOnly?>` z `RenderStyle = Native`, domyślnie pierwszy i ostatni dzień bieżącego Miesiąca (`YearMonth.From(PolishCalendar.Today(TimeProvider))`); Projekt i Typ pracy jako `FluentSelect` z pierwszą opcją „Wszystkie” i wszystkimi pozycjami Usera, także nieaktywnymi (z dopiskiem „(nieaktywny)”), z `ProjectStore.ListAsync` / `WorkTypeStore.ListAsync`. Każda zmiana filtra od razu wczytuje listę ponownie. Puste „Od” lub „Do” = brak tej granicy (granice włącznie).
- **„Od” późniejsze niż „Do”:** bez zapytania, lista pusta, komunikat przy filtrach „Data „Od” jest późniejsza niż „Do”.”.
- **Podsumowanie pod listą:** „Wpisy: N, suma Czasu: X” — N i X (`Czas.Format`, sumy ponad 24 h, zero `0h`) z wyniku zapytania, czyli dla filtrów ekranu; filtr wartości kolumny ich nie zmienia (AD-9).
- **`EntryStore.ListAsync(EntryFilter filter, CancellationToken)`:** jeden kontekst `UserDbContextFactory` (AD-4, AD-16), `AsNoTracking`, filtry w `Where` po stronie bazy, projekcja do `EntryRow(Id, Date, ProjectId, ProjectCode, WorkTypeId, WorkTypeName, Description, Minutes)` (Id-ki dla historii 2.3), `ToListAsync`. `EntryFilter(DateOnly? From, DateOnly? To, int? ProjectId, int? WorkTypeId)`.
- `PageHeader` „Lista wpisów” z `Topic="wpisy"`; temat `wpisy` (Zalogowany) w `HelpTopics` z opisem filtrów, kolumn, sortowania, sumy i filtra wartości.

**Never:**
- Edycja, kopia, usuwanie i ikony akcji w wierszu (historia 2.3); zmiany `ReportGrid`, `DictionaryOptions`, schematu.
- Sumowanie w `ReportGrid` lub z wierszy widocznych po filtrze kolumny; `IQueryable` przekazany do siatki; ręczne `UserId` / `IgnoreQueryFilters`; `DateTime.Today`/`Now`.
- Eksport CSV.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Otwarcie | dziś 2026-09-03, Wpisy z sierpnia i września | tylko wrzesień, od najnowszej daty; liczba i suma tych Wpisów | — |
| Zakres | Od 2026-08-01, Do 2026-08-31 | tylko sierpień, liczba i suma zmienione | — |
| Granice | Wpis 2026-09-01 i 2026-09-30, zakres wrzesień | oba widoczne | — |
| Projekt | filtr RDZ | tylko Wpisy RDZ, suma tylko z nich | — |
| Nieaktywny | Projekt OLD nieaktywny z Wpisami | OLD w filtrze „(nieaktywny)”, jego Wpisy na liście | — |
| Puste granice | Od puste | wszystkie Wpisy do „Do” | — |
| Od > Do | Od 2026-09-10, Do 2026-09-01 | lista pusta, liczba 0, suma `0h` | komunikat przy filtrach |
| Suma > 24 h | dwa Wpisy po `24h` | suma `48h` | — |
| Drugi User | Wpisy Usera B | nigdy na liście Usera A ani w jego filtrach | — |
| Brak Wpisów | zakres bez Wpisów | pusta siatka, „Wpisy: 0, suma Czasu: 0h” | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Features/Wpisy/EntryStore.cs` -- dopisać `EntryFilter`, `EntryRow` (z właściwościami tekstowymi kolumn jak `ProjectRow.State`) i `ListAsync`; nie zmieniać `AddAsync`.
- `src/RejestrCzasu/Features/Projekty/ProjectStore.cs` `ListAsync` (`ProjectRow` z `IsActive`), `Features/TypyPracy/WorkTypeStore.cs` `ListAsync` -- źródło opcji filtrów (wszystkie, posortowane `pl-PL`); już zarejestrowane w `AddAppScreens`.
- `src/RejestrCzasu/Features/Projekty/ProjectsPage.razor` -- wzorzec ekranu: `PageHeader`, statyczne `Columns`, `ReportGrid`, `ReloadAsync`.
- `src/RejestrCzasu/Features/Wpisy/NewEntryPage.razor` -- wzorzec `TimeProvider` + `PolishCalendar.Today`, `FluentDatePicker` Native.
- `src/RejestrCzasu/Shared/ReportGrid.razor` -- `Items`, `Columns`; bez zmian.
- `src/RejestrCzasu/Domain/Czas.cs` (`Format`), `Domain/PolishCalendar.cs` (`Today`, `DayName`), `Domain/YearMonth.cs` (`From`, `FirstDay`, `LastDay`).
- `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/WpisyNowyHelp.razor` -- dopisać stałą `Wpisy = "wpisy"` i `WpisyHelp.razor`.
- `src/RejestrCzasu/Shared/MainLayout.razor:81` -- menu już prowadzi do `/wpisy`.
- Fluent UI 5.0.1 `FluentSelect<TOption,TValue>`: `Items`, `OptionText`, `@bind-Value`, `Label` (z `FluentListBase`).
- Testy: `tests/RejestrCzasu.Tests/Features/EntryStoreTests.cs` (Testcontainers, `StoreFor`, `SeedDictionariesAsync`), `Features/NewEntryPageTests.cs` (bUnit z `FixedTimeProvider`, `CreateLoggedInUserDbContextFactory`, `AddAppUi`).

## Tasks & Acceptance

**Execution:**
- [ ] `src/RejestrCzasu/Features/Wpisy/EntryStore.cs` -- `EntryFilter`, `EntryRow`, `ListAsync` -- R8, AD-4, AD-16.
- [ ] `src/RejestrCzasu/Features/Wpisy/EntriesPage.razor` -- `/wpisy` z filtrami, siatką i podsumowaniem -- R8, R17, R2.
- [ ] `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/WpisyHelp.razor` -- temat `wpisy` -- R26.
- [ ] `tests/RejestrCzasu.Tests/Features/EntryStoreTests.cs` -- `ListAsync`: zakres z granicami, puste granice, Projekt, Typ pracy, sortowanie data/Id, nieaktywny Projekt, drugi User nigdy (AD-16).
- [ ] `tests/RejestrCzasu.Tests/Features/EntriesPageTests.cs` -- bUnit: domyślny zakres bieżącego Miesiąca, kolejność, liczba i suma, zmiana filtra Projektu, nieaktywny w opcjach, Od > Do, temat pomocy; `HelpTests` dalej zielone.

**Acceptance Criteria:**
- Given `dotnet run` i zalogowany User z Wpisami z dwóch Miesięcy, when otwiera Listę wpisów i zmienia zakres oraz Projekt, then lista, liczba i suma zmieniają się zgodnie z filtrami, a `?` otwiera temat `wpisy`.

## Implementation Notes

## Plan Change Log

## Review Triage Log

| # | Lens | Finding | Verdict | Route | Evidence |
|---|------|---------|---------|-------|----------|
| 1 | quick | `EntriesPage.razor` FluentSelect: wybór opcji w przeglądarce nieprzetestowany (brak `OptionValue`, domyślne `ToString()` rekordu) | maybe-false | defer (unverified medium) | Testy bUnit wołają `ValueChanged` wprost; rozstrzyga ręczne `dotnet run` na `/wpisy` (wybór Projektu i „Wszystkie”). |
| 2 | quick | `ReloadAsync` nie anuluje wypartych zapytań | low | reject | Licznik wersji chroni wynik; zbędne zapytanie to tylko koszt, poprawka wymaga CTS i `IDisposable`. |
| 3 | quick | Brak testu renderu `/pomoc/wpisy` | low | reject | `HelpPage` renderuje temat generycznie z `HelpTopics`, ta ścieżka ma testy w `HelpTests`; rejestracja `wpisy` przetestowana. |

## Design Notes

- Wybrana opcja filtra Projektu/Typu pracy: mały rekord opcji z `int? Id` (null = „Wszystkie”) zamiast `ProjectRow?`, żeby „Wszystkie” było zwykłą pozycją listy.
- Zmiana filtra podczas trwającego zapytania: wczytywanie po każdej zmianie; wynik starszego zapytania nie może nadpisać nowszego (np. licznik wersji zapytania).

## Verification

**Commands:**
- `dotnet build -c Release` -- expected: bez błędów i ostrzeżeń.
- `dotnet test -c Release --no-build` -- expected: wszystkie testy zielone (Docker Desktop działa).

**Manual checks (if no CLI):**
- `dotnet run`: `/wpisy` pokazuje Wpisy bieżącego Miesiąca od najnowszych; nieaktywny Projekt w filtrze; zmiana zakresu i Projektu zmienia liczbę i sumę.
