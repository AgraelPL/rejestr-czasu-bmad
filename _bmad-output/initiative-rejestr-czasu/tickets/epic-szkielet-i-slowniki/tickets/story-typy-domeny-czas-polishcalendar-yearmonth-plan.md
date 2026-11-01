---
title: 'Typy domeny: Czas, PolishCalendar, YearMonth'
type: 'feature'
ticket: '4'
created: '2026-10-31'
status: done
baseline_revision: '872b6ae2c4c8bb9ced1ae689bc415bce454c5c9d'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Epiki 2 i 3 (Wpisy, Widok miesiąca, Zestawienie) potrzebują jednego miejsca konwersji Czasu, dat i Miesiąca. Bez niego każdy ekran parsowałby `h:mm`, liczył tydzień i „dzisiaj” po swojemu (AD-6).

**Approach:** Trzy czyste typy w `src/RejestrCzasu/Domain/` bez EF i UI: `Czas` (konwersje minut `int`), `PolishCalendar` (polskie nazwy, tydzień od poniedziałku, „dzisiaj” i czas polski z `TimeProvider`), `YearMonth` (Miesiąc). Testy xUnit bez bazy.

## Boundaries & Constraints

**Always:**
- Czas to zawsze `int` minut; `Czas` jest statyczną klasą konwersji, nie typem opakowującym.
- `Czas` udostępnia:
  - `TryParse` dla `h:mm`: godziny bez wiodących zer wymaganych, minuty dokładnie dwie cyfry 00–59, spacje na brzegach ignorowane;
  - `Format`: `h:mm`, także sumy ponad 24 h (1830 → `30:30`, 0 → `0:00`); minuty ujemne dają `ArgumentOutOfRangeException`;
  - regułę Wpisu `IsValidEntry` (15–1440, wielokrotność 15) i jej stałe dla przyszłego walidatora (AD-8);
  - `ToDecimalHours` → `decimal` (90 → 1,5; 45 → 0,75).
- `PolishCalendar` udostępnia:
  - nazwy dni i miesięcy w mianowniku z wielkiej litery, jak w `export-layout.md` („Środa”, „Wrzesień”), zapisane w kodzie, nie z ICU;
  - `WeekStart(DateOnly)` → poniedziałek;
  - `Today(TimeProvider)` → `DateOnly` w strefie `Europe/Warsaw`;
  - `ToPolishTime(DateTime utc)` do wyświetlania znaczników UTC (AD-6).
- `YearMonth` to `readonly record struct (Year, Month)`:
  - walidacja miesiąca 1–12 w konstruktorze;
  - `FirstDay`, `LastDay`, `DaysInMonth`, `Contains(DateOnly)`, `From(DateOnly)`, `Next`, `Previous`;
  - porządek chronologiczny (`IComparable`);
  - nazwa „Wrzesień 2026” przez `PolishCalendar`.
- `BannedSymbols.txt` zakazuje `DateTime.Now`, `DateTime.Today` i `DateTimeOffset.Now` (AD-6) wraz z polskim powodem; build pozostaje bez ostrzeżeń.

**Never:**
- Encje, EF, UI, FluentValidation, `MonthReport` i tygodnie Miesiąca (epik 3).
- Zależność od strefy albo kultury maszyny.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Czas Wpisu na granicach | `0:15`, `24:00` | przyjęte (15, 1440) | — |
| Czas Wpisu odrzucony | `0:10`, `24:15`, `0:00` | parsuje się, `IsValidEntry` = false | — |
| Zły zapis | `abc`, `1:5`, `1:60`, `-1:00`, `1:30:00`, pusty, `null` | `TryParse` = false | — |
| Suma ponad 24 h | 1830 minut | `30:30` | — |
| Godziny dziesiętne | 90 / 45 / 240 | 1,5 / 0,75 / 4 | — |
| Dzień tygodnia | 1.09.2026 | „Wtorek”; `WeekStart` = 31.08.2026 | — |
| Tydzień od poniedziałku | niedziela 6.09.2026 | `WeekStart` = 31.08.2026 | — |
| Dzisiaj | `TimeProvider` 31.12.2026 23:30 UTC | 1.01.2027 (Warszawa UTC+1) | — |
| Miesiąc | `YearMonth(2026, 2)` | `LastDay` 28.02, `Next` 2026-03; `YearMonth(2026, 12).Next` = 2027-01; nazwa „Luty 2026” | — |
| Zły Miesiąc | `YearMonth(2026, 13)` | — | `ArgumentOutOfRangeException` |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Domain/`: nowy folder i przestrzeń nazw `RejestrCzasu.Domain`. Pierwszy kod domeny, bez wzorca do naśladowania.
- `src/RejestrCzasu/BannedSymbols.txt`:
  - format jest w nagłówku: pełne ID dokumentacji, `;powód`; obecne wpisy dotyczą AD-16;
  - nowe wpisy: `P:System.DateTime.Now`, `P:System.DateTime.Today`, `P:System.DateTimeOffset.Now`;
  - `src` ich dziś nie używa (sprawdzone grep). `RS0030` jest błędem (`RejestrCzasu.csproj`); testy nie są analizowane.
- `tests/RejestrCzasu.Tests/`:
  - xUnit v3; `PostgresFixture` jest fixture całego assembly, ale testy domeny jej nie wstrzykują;
  - `TimeProvider` w testach: własna klasa testowa z `GetUtcNow()` (bez nowego pakietu).
- `_bmad-output/initiative-rejestr-czasu/export-layout.md` L7, L15, L20: wzór nazw („Wrzesień 2026”, „Środa”) i godzin dziesiętnych.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Domain/Czas.cs`, `PolishCalendar.cs`, `YearMonth.cs` -- typy domeny -- AD-6.
- [x] `src/RejestrCzasu/BannedSymbols.txt` -- zakaz zegara serwera -- AD-6.
- [x] `tests/RejestrCzasu.Tests/Domain/` -- każdy wiersz I/O Matrix.

**Acceptance Criteria:**
- Given wszystkie zmiany, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone.
- Given użycie `DateTime.Now` w `src`, when build, then błąd `RS0030` z powodem AD-6 (sprawdzić ręcznie i cofnąć).

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone

Uruchomiona przez Autora aplikacja może blokować `src/RejestrCzasu/bin`. Wtedy build i testy uruchamiaj na kopii repozytorium w scratchpadzie i nie zatrzymuj jego procesu.

## Implementation Notes

- `Czas.TryParse` przyjmuje 1–5 cyfr ASCII godzin (także z wiodącym zerem, np. `01:30`); cyfry spoza ASCII i znak `+` są odrzucane.
- `PolishCalendar.ToPolishTime` czyta `DateTimeKind.Unspecified` jako UTC (znaczniki z bazy), a `Local` odrzuca `ArgumentException`. Strefa: `Europe/Warsaw`, z zapasowym identyfikatorem Windows `Central European Standard Time`, gdy brak ICU.
- `YearMonth` waliduje też rok 1–9999 (zakres `DateOnly`); `ToString()` daje `yyyy-MM`, polska nazwa jest w `Name`; dodano operatory `<`, `>`, `<=`, `>=`. `default(YearMonth)` (0, 0) pozostaje możliwy, jak w każdej strukturze.
- Powody w `BannedSymbols.txt` bez polskich znaków i bez średników, jak w istniejących wpisach (średnik rozdziela ID i powód).
- Sprawdzenie ręczne: tymczasowy plik z `DateTime.Now`, `DateTime.Today`, `DateTimeOffset.Now` dał 3 błędy `RS0030` z powodem AD-6; plik usunięty.

## Plan Change Log

## Review Triage Log

| # | Lens | Finding | Verdict | Route | Evidence |
|---|------|---------|---------|-------|----------|
| 1 | quick | `YearMonth` nie jest pozycyjny, brak `Deconstruct` | low | reject | Nikt jeszcze nie dekonstruuje `YearMonth`; walidacja w konstruktorze jest celowa, a dodanie `Deconstruct` to nowa powierzchnia bez konsumenta. |
| 2 | quick | Brak niegenerycznego `IComparable` | low | reject | Sortowanie w C# i `FluentDataGrid` idzie przez `Comparer<T>.Default` (ścieżka generyczna); brak wskazanego wywołującego z `object[]`. |
| 3 | quick | Zakaz w `BannedSymbols.txt` nie obejmuje strefy serwera (`TimeZoneInfo.Local`, `ToLocalTime`, `GetLocalNow`) ani zegara bez `TimeProvider` (`UtcNow`), a komentarz obiecuje więcej | low | patch | Grep: `src` używa tylko `timeProvider.GetUtcNow()`; dopisanie zakazów to bezpośrednia poprawka zgodna z komentarzem i AD-6. |
| 4 | quick | `Trim()` zdejmuje każdy biały znak Unicode, nie tylko spacje | low | reject | Tabulator lub twarda spacja na brzegu pola Czasu przyjęte jako ten sam Czas nie szkodzi Userowi; zawężenie dodaje regułę bez korzyści. |
| 5 | quick | Statyczny inicjator strefy w `PolishCalendar` psuje `DayName`/`MonthName`, gdy strefy brak | low | patch | `TimeZone { get; } = FindWarsaw()` wykonuje się przy pierwszym użyciu typu; leniwe ładowanie strefy tylko dla `Today`/`ToPolishTime` to mała, lokalna poprawka. |
