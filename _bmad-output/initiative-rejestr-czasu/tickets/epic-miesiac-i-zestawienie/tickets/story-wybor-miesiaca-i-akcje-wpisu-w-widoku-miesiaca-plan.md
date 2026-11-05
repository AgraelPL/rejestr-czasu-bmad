---
title: 'Wybór Miesiąca i akcje Wpisu w Widoku miesiąca'
type: 'feature'
ticket: '4'
created: '2026-11-05'
status: 'built'
baseline_revision: '5e5e82c953ba9aca663f3c1f1b153ff792d3930b'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Widok miesiąca pokazuje tylko bieżący Miesiąc i nie ma akcji Wpisu; na listach Wpisów pasek komunikatu pojawia się przed ponownym wczytaniem listy (`deferred-work.md`).

**Approach:** Miesiąc w adresie `/miesiac/RRRR-MM` z przyciskami Poprzedni / Następny / Bieżący; ikony Edytuj / Kopiuj / Usuń przez `EntryDialogs` po `Id`, po zmianie `MonthReport` od nowa z bazy; `EntryDialogs` przyjmuje opcjonalny `afterChange` i pokazuje pasek dopiero po nim — we wszystkich trzech hostach.

## Boundaries & Constraints

**Always:** sumy tylko z `MonthReport` (AD-5); akcje wyłącznie przez `EntryDialogs` (AD-10); `LayoutKey` `miesiac@RRRR-MM` (AD-9); jeden wątek akcji naraz (`_busy`); kopia bierze datę z `EntryDateMemory`, bez niej dziś w Polsce (R7, już w `CopyAsync`); pomoc `miesiac` w tej samej zmianie (AD-22).

**Never:** zmian w `Ui/` (kopia wzorca) i w eod-ui; zmiany typu wyniku `Task<bool>`; sum liczonych w ekranie; zapytań z `IQueryable` w ekranie.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Bez Miesiąca | `/miesiac`, dziś 05.11.2026 | listopad 2026, Bieżący wyłączony | — |
| Przełom roku | `/miesiac/2026-01` → Poprzedni; `/miesiac/2025-12` → Następny | adres `/miesiac/2025-12` / `/miesiac/2026-01`, dane i sumy tego Miesiąca | — |
| Zakładka | otwarcie `/miesiac/2026-09` | wrzesień 2026 (30/16/18/25/14, 103h) | — |
| Zły adres | `/miesiac/2026-13`, `/miesiac/abc`, `/miesiac/2026-9`, `/miesiac/0000-01` | strona „nie znaleziono” | `Navigation.NotFound()`, bez zapytania |
| Edycja do innego Miesiąca | Wpis z 02.09 → 02.10 | znika z września, sumy dnia/tygodnia/Miesiąca mniejsze; potem pasek „Zapisano Wpis…” | — |
| Usunięcie ostatniego | jedyny Wpis dnia/tygodnia | grupa dnia/tygodnia znika, sumy mniejsze; potem pasek „Usunięto Wpis…” | — |
| Kopia w bieżący | widok bieżącego, kopia z datą w nim | nowy wiersz, sumy większe | — |
| Wpis usunięty w międzyczasie | klik ikony, Wpis już skasowany | lista wczytana ponownie, potem ostrzeżenie `EntryMessages.NotFound` | bez okna |
| Podwójny klik | drugi klik ikony przy otwartym oknie | jedno okno | ignorowany (`_busy`) |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Features/Miesiac/MonthPage.razor` -- `@page "/miesiac"`, `<Actions>` (l. 16), `<EodReport>` (l. 29), `OnInitializedAsync` (l. 121) z `PolishCalendar.Today`; komentarz „no row actions (story 4)”.
- `src/RejestrCzasu/Features/Wpisy/EntryDialogs.cs` -- `EditAsync`/`CopyAsync`/`DeleteAsync` → `Task<bool>`; paski w `FindAsync`, `DeleteAsync`, `ShowAsync` przed powrotem.
- `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor:17-23, ChangeAfterAsync` -- wzór ikon Subtle, `_busy`, `OnChanged`; hosty `EntriesPage.razor:36` (`ReloadAsync`), `NewEntryPage.razor:29` (`ReloadMonthAsync`, wersja `_version`).
- `src/RejestrCzasu/Features/Pomoc/HelpPage.razor:48-57` -- wzór `Navigation.NotFound()`; test `HelpTests.Topic_Unknown_IsNotFound` (`OnNotFound`).
- `src/RejestrCzasu/Domain/YearMonth.cs` -- `Next`, `Previous`, `ToString()` „RRRR-MM”, konstruktor rzuca poza zakresem.
- `tests/RejestrCzasu.Tests/Features/MonthPageTests.cs` (`RenderScreen` l. 172, usługi l. 35-36), `EntriesPageTests.cs:40-50` (rejestracja `EntryDialogs` i zależności), `NewEntryPageTests.cs`.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Features/Wpisy/EntryDialogs.cs` -- `EditAsync(int entryId, Func<Task>? afterChange = null)` itd.; po każdej zmianie lub braku Wpisu najpierw `await afterChange()`, potem pasek; `false` (Anuluj, brak potwierdzenia) bez wywołania -- pasek po przeładowaniu, kontrakt bez zmiany typu.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor` -- `ChangeAfterAsync(Func<Func<Task>, Task<bool>>)` podaje `() => OnChanged.InvokeAsync()` jako `afterChange`, bez drugiego wywołania `OnChanged`; komentarz nagłówka -- Lista wpisów i Nowy wpis.
- [x] `src/RejestrCzasu/Features/Miesiac/MonthPage.razor` -- według Design Notes: trasa z segmentem, przyciski, `RowActions`, `_busy`, `ReloadAsync` z wersją; komentarz nagłówka.
- [x] `src/RejestrCzasu/Features/Pomoc/Topics/MiesiacHelp.razor` -- wybrany Miesiąc (domyślnie bieżący), przyciski, adres do zakładki, ikony wiersza, sumy od razu.
- [x] `_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md` (AD-10) -- „pasek … pokazuje `EntryDialogs` po ponownym wczytaniu danych przez ekran (`afterChange`)”; `deferred-work.md` -- pozycja o pasku do „Rozstrzygnięte (3.4)”.
- [x] `tests/RejestrCzasu.Tests/Features/MonthPageTests.cs` -- rejestracja zależności `EntryDialogs` jak w `EntriesPageTests`; wiersze matrycy (nawigacja: `NavigationManager.Uri` po kliku i `SetParametersAndRender` z `Miesiac`; zły adres przez `OnNotFound`; ikony w wierszach).
- [x] `tests/RejestrCzasu.Tests/Features/EntriesPageTests.cs`, `NewEntryPageTests.cs` -- po jednym teście: w chwili pojawienia się paska (bez dalszego `WaitForAssertion`) lista już po zmianie; istniejące testy bez zmian zielone.

**Acceptance Criteria:**
- Given `/miesiac/2026-09`, when User klika Edytuj/Kopiuj/Usuń i zapisuje/potwierdza, then Suma dnia, tygodnia i Miesiąca odpowiadają bazie, a pasek pojawia się, gdy lista już jest nowa.
- Given Lista wpisów lub Nowy wpis, when User zmienia Wpis z ikony, then pasek po przeładowaniu listy, reszta zachowania bez zmian.
- Given ikona `?` w Widoku miesiąca, then `/pomoc/miesiac` opisuje wybór Miesiąca i akcje wiersza.

## Implementation Notes

- `MonthPage.TryParse` sprawdza format znak po znaku (`char.IsAsciiDigit`) zamiast `\d` w regex (w .NET `\d` przyjmuje cyfry Unicode); test z cyframi arabsko-indyjskimi.
- Przy złym adresie strona nic nie renderuje (`_invalid`), bo `_month` byłby `default` (miesiąc 0).
- `ReloadAsync` woła `StateHasChanged`, bo `afterChange` biegnie wewnątrz `EntryDialogs`, a pasek ma się pojawić po wyrenderowaniu nowej listy.
- Przyciski mają klasy `month-previous` / `month-current` / `month-next` (testy).
- Test `September_WeeksDaysSumsAndRowsFromMonthReport` bierze pierwsze 4 komórki (nowa kolumna akcji); z `NoExportNoPagingNoSorting…` usunięta asercja „brak przycisków w wierszach”.
- Testy „pasek po przeładowaniu” sprawdzone mutacją: z paskiem przed `afterChange` w `DeleteAsync` padają wszystkie trzy (Lista wpisów, Nowy wpis, Widok miesiąca).

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `WpisyHelp.razor:53-54` | pomoc podaje kolejność pasek → przeładowanie, kod robi odwrotnie | low | `EntryDialogs` woła `afterChange` przed `ShowWarningAsync`; AD-22 | patch |
| quick | `MonthReport.WeekOf` + `/miesiac/9999-12` | Wpis 27–31.12.9999 rzuca w `AddDays(6)` | low | nierealna data; poprawka bez znaczenia dla Userów | reject |
| quick | `EntriesReport.razor:90` | komentarz `OnChanged` węższy niż kod | low | wołane też przy brakującym Wpisie | patch |
| quick | `MonthPageTests` przełom roku | dane i sumy 2025-12 i 2026-01 nie sprawdzone | low | test sprawdza tylko nagłówek i adres | patch |
| quick | `MonthPageTests` kopia | kolejność paska przy Kopiuj niejawna | low | asercje zaraz po pasku i tak ją sprawdzają | reject |

## Design Notes

`MonthPage`: `@page "/miesiac"` i `@page "/miesiac/{Miesiac}"` (`string?`). `OnParametersSetAsync`: brak → `YearMonth.From(PolishCalendar.Today)`; inaczej `^\d{4}-\d{2}$` + `YearMonth` w `try` (`ArgumentOutOfRangeException`) → przy błędzie `Navigation.NotFound()` i `return`. Przy nowym Miesiącu `_report = null` (szkielet), `ReloadAsync`; ten sam Miesiąc — nic. `EodReport` z `@key="_month"` (filtry kolumn nie przechodzą między Miesiącami).

```razor
<Actions>  @* przed DownloadLink *@
  <FluentButton OnClick="@(() => Go(_month.Previous))" Disabled="@(_month == Min)">Poprzedni</FluentButton>
  <FluentButton OnClick="@(() => Go(Current))" Disabled="@(_month == Current)">Bieżący</FluentButton>
  <FluentButton OnClick="@(() => Go(_month.Next))" Disabled="@(_month == Max)">Następny</FluentButton>
@code { void Go(YearMonth m) => Navigation.NavigateTo($"/miesiac/{m}"); }
```

`RowActions` jak `EntriesReport` (`id = (int)row[IdKey]!`), `ChangeAfterAsync(a => Dialogs.EditAsync(id, a))` z `_busy`, `afterChange = ReloadAsync`. `ReloadAsync`: `var v = ++_version; var r = MonthReport.Build(_month, await Query.LoadAsync(_month)); if (v == _version) { _rows = …; _report = r; }` — po akcji bez szkieletu.

Założenie: przyciski zawsze prowadzą na `/miesiac/RRRR-MM`, także Bieżący; samo `/miesiac` zostaje bieżącym (zakładka „zawsze bieżący”).
Założenie: Bieżący wyłączony na bieżącym Miesiącu; Poprzedni/Następny wyłączone na `0001-01`/`9999-12`.
Założenie: format adresu ścisły (dwie cyfry miesiąca); `/miesiac/2026-9` → „nie znaleziono”.
Założenie: `afterChange` to opcjonalny parametr — wywołanie bez niego działa jak dotąd (pasek, potem `true`); trzy ikony i `_busy` powielone w `MonthPage` zamiast wspólnego komponentu (blokada obejmuje cały raport) — do Refactor sweep (historia 9).

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów.
- `dotnet test` -- expected: wszystkie zielone, w tym `MonthPageTests`, `EntriesPageTests`, `NewEntryPageTests`, `HelpTests`.

**Manual checks (if no CLI):**
- Lokalnie: Poprzedni zmienia adres i dane; zakładka `/miesiac/2026-09` otwiera wrzesień; edycja, kopia, usunięcie przeliczają sumy od razu, pasek po odświeżeniu listy.
