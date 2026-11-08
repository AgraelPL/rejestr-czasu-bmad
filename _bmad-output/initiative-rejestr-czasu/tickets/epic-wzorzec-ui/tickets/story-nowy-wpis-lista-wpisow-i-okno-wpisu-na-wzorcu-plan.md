---
title: 'Nowy wpis, Lista wpisów i okno Wpisu na wzorcu'
type: 'feature'
ticket: '5'
created: '2026-11-03'
status: done
baseline_revision: '6495c7e5e62f45b5a107b7078f2e03b6a3445d63'
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
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-wzorzec-ui/tickets/story-projekty-i-typy-pracy-na-wzorcu-plan.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** `/wpisy/nowy` i `/wpisy` stoją jeszcze na `PageHeader` i `Shared/ReportGrid`, a okno Wpisu odbiega od okien wzorca z 7.4 (Anuluj w osobnym wierszu, pasek sukcesu z okna).

**Approach:** Oba ekrany na `EodPage` + `EodCard`; Lista wpisów na `EodReport` według AD-9 z filtrami ekranu w pasku `eod-formbar eod-filterbar` nad raportem i sumą przez `FooterSummary` (AD-5); akcje wiersza wyłącznie `EntryDialogs` po `Id` (AD-10). `EntryForm` bez zmian zachowania (R4, R5, R16). Tematy pomocy `wpisy-nowy` i `wpisy` według nowego wyglądu. Ryzyko high: R16 (10 s samą klawiaturą), wspólny `EntryForm` dwóch hostów i ścieżka usuwania.

## Boundaries & Constraints

**Always:** `Report` jako `static readonly` (`Key`/`LayoutKey` = `HelpTopics.Wpisy`, `ShowExport="false"`, `PreserveRowOrder`), kolumny `List` z kluczami = nazwy właściwości `EntryRow`; Data z `DateOnly` i `Format` = `EntryMessages.Date`, Czas z `int` minut (`Minutes`) i `Format` = `Czas.Format`; wiersze z jednej `static ToRow(EntryRow)` z `Id` poza kolumnami; suma tylko z listy zwróconej przez `EntryStore.ListAsync` dla filtrów ekranu; dane tylko przez `EntryStore`, `DictionaryOptions`, `EntryDialogs`; usunięcie przez `UiMessages.ConfirmAsync` (Enter nie potwierdza, fokus Anuluj); błędy zapisu okna w jego treści, czyszczone też przy nieudanej walidacji; `Autofocus` (Fluent 5.0.1), nie `AutoFocus`.

**Never:** zmiany w repozytorium eod-ui; zmiany w `Ui/` bez nowej pozycji AD-23 i linii w `docs/ui/DOPASOWANIA.md` (spodziewane: żadnych — `ShowExport`, `Format`, `PreserveRowOrder`, `FooterSummary`, `Loading` już są; gdyby czegoś brakowało: HALT, potem pozycja 11 w obu miejscach w tej samej zmianie); obejście braku parametru w ekranie; zmiana kolejności fokusu, Enter i pamięci daty w `EntryForm`; zmiany `EntryStore`, walidatorów, schematu; sumowanie w `EodReport` lub z przefiltrowanych wierszy; usuwanie `ReportGrid`, `PageHeader`, `Confirm` (7.7); zmiana sygnatur `EntryDialogs` (`Task<bool>`, używa ich Widok miesiąca).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Sesja wpisywania | `/wpisy/nowy`, Data dziś, Tab/Enter | zapis, pasek „Zapisano Wpis: …” pod nagłówkiem, data zostaje, fokus w Projekt | brakujący Słownik: pasek błędu, wartości zostają, listy ponownie |
| Lista | 3 Wpisy bieżącego Miesiąca | od najnowszej daty (w dniu wyższe Id wyżej), Czas „1h 30m”, stopka „3 pozycje · Wpisy: 3, suma Czasu: …”, brak „Eksport CSV” | — |
| Filtr ekranu | Projekt / zakres dat | lista, licznik i suma z nowego zapytania | Od > Do: komunikat w pasku filtrów, pusta lista, suma 0h |
| Filtr kolumny | odznaczenie wartości w kolumnie | ukryte wiersze, licznik „… · po filtrach, z N”, suma bez zmian | — |
| Sortowanie Czasu | klik nagłówka Czas | porządek liczbowy minut (45m < 2h < 10h) | — |
| Edytuj / Kopiuj | ikona w wierszu | okno z Projektem i Typem pracy widocznymi w polach (`EodUI.showSelectedText`), Zapisz pierwszy, Anuluj obok | brakujący Słownik: błąd w oknie; nieudana walidacja czyści błąd; Wpis usunięty: okno zamknięte, ostrzeżenie, lista ponownie |
| Po zapisie okna | Done | okno zamknięte, pasek sukcesu na stronie, lista ponownie | — |
| Usuń | ikona → potwierdzenie | Enter nic nie robi, Usuń kasuje, Anuluj/Escape zostawia | Wpis już usunięty: ostrzeżenie, lista ponownie |

**Decyzja (Autor, 2026-11-03):** `FooterSummary` zachowuje tekst „Wpisy: N, suma Czasu: X” po liczniku wzorca, np. „3 pozycje · Wpisy: 3, suma Czasu: 5h”; przy filtrze kolumny „1 pozycja · Wpisy: 3, suma Czasu: 5h · po filtrach, z 3”.

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Features/Wpisy/EntriesPage.razor` -- przepisać na `EodPage Fill` + `PageHelp` + `EodCard NoPadding Fill` z paskiem filtrów (te same `FluentDatePicker`/`FluentSelect`, `FilterOption`, `All`, `RangeErrorMessage`, `_version`, `Inactive`) i `EodReport` z `RowActions` (ikony Subtle Edytuj/Kopiuj/Usuń → `ReloadAfterAsync(Dialogs.XxxAsync((int)row["Id"]))`), `Loading` do pierwszego wczytania. `Summary(rows)` zostaje źródłem `FooterSummary`.
- `Features/Wpisy/EntryStore.cs` -- `EntryRow` (`Id`, `Date`, `DayName`, `ProjectCode`, `WorkTypeName`, `Description`, `Minutes`); `ListAsync` już sortuje Data↓, Id↓. Bez zmian; `ToRow` na ekranie lub w `EntryRow` (jedna metoda).
- `Features/Wpisy/EntryDialog.razor` -- jak `ProjectDialog`: `_error` czyszczony w `OnInvalid`, wynik okna = `EntryChange` (`Done`/`NotFound`), bez wywołań `UiMessages` w oknie; przeładowanie opcji po `MissingDictionaryItem` zostaje.
- `Features/Wpisy/EntryDialogs.cs` -- `ShowAsync` pokazuje pasek sukcesu (`EntryMessages.Saved`) lub ostrzeżenie (`NotFound`) po zamknięciu okna według wyniku (AD-10); `dialog.Size = DialogSize.Medium`; reszta bez zmian.
- `Shared/EntryForm.razor` -- tylko dwa opcjonalne parametry addytywne: `RenderFragment? Buttons` (po Zapisz w `.entry-form-buttons`, okno podaje Anuluj) i `EventCallback OnInvalid` (gdy `Validate()` nie przejdzie). Zachowanie fokusu, Enter, `ChangeAfterKeyPress`, `showSelectedText` bez zmian.
- `Features/Wpisy/NewEntryPage.razor` -- `EodPage` (tytuł, opis z `PageHeader`) + `PageHelp Topic=WpisyNowy` + `EodCard` z `EntryForm`; logika bez zmian.
- `Ui/Reports/EodReport.razor(.cs)`, `ReportDefinition.cs` -- tylko użycie; wzór: `Features/Projekty/ProjectsPage.razor`, `tests/RejestrCzasu.Tests/Ui/EodReportTests.cs`. Stopka = `WithCount(filtrowane) · FooterSummary · po filtrach, z N` (`.eod-card-foot`).
- `wwwroot/ui/eod.css` -- `.eod-formbar`, `.eod-filterbar` (gotowe, bez zmian).
- `Features/Pomoc/Topics/WpisyHelp.razor`, `WpisyNowyHelp.razor` -- treść według ekranu (pasek filtrów, ikony, okno, stopka, filtr kolumn, sortowanie i grupowanie Usera do odświeżenia strony).
- `wwwroot/app.css` -- usunąć `.entries-summary`, `.entry-dialog-buttons`, `.entries-filters` (zastąpione klasami wzorca); `.entries-filter-error` zostaje lub na token wzorca.
- `tests/RejestrCzasu.Tests/Features/EntriesPageTests.cs`, `NewEntryPageTests.cs` -- bUnit na Testcontainers; selektory na `EodReport` (`.eod-card-foot`, komórki po kolumnie akcji), okno i `EodPage`; pokrycie dotychczasowych zachowań zostaje.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Shared/EntryForm.razor` -- parametry `Buttons` i `OnInvalid` -- okno wzorca bez zmiany zachowania formularza.
- [x] `src/RejestrCzasu/Features/Wpisy/EntryDialog.razor`, `EntryDialogs.cs` -- Anuluj obok Zapisz, czyszczenie błędu, paski po zamknięciu w `EntryDialogs` -- AD-10.
- [x] `src/RejestrCzasu/Features/Wpisy/NewEntryPage.razor` -- `EodPage` + `EodCard` -- AD-11.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesPage.razor` -- `Report`, `ToRow`, pasek filtrów, `EodReport` z `FooterSummary` i `RowActions` -- AD-9, AD-5.
- [x] `src/RejestrCzasu/Features/Pomoc/Topics/WpisyHelp.razor`, `WpisyNowyHelp.razor` -- treść zgodna z ekranem -- R26, AD-22.
- [x] `src/RejestrCzasu/wwwroot/app.css` -- sprzątanie reguł starych ekranów.
- [x] `tests/RejestrCzasu.Tests/Features/EntriesPageTests.cs`, `NewEntryPageTests.cs` -- przepisać i dodać wiersze macierzy: filtr kolumny nie zmienia sumy, brak Eksport CSV, sortowanie Czasu liczbowo, kolejność od najnowszej, błąd okna znika przy nieudanej walidacji, Anuluj obok Zapisz, pasek sukcesu po zamknięciu okna.

**Acceptance Criteria:**
- Given aplikacja po zmianie, when `grep` w `Features/Wpisy` po `ReportGrid`, `PageHeader`, `Notify`, then brak trafień; `git diff` nie dotyka `Ui/` (albo dotyka z nową linią w `DOPASOWANIA.md` i AD-23).
- Given `dotnet run` i `/wpisy/nowy`, when Sesja wpisywania 10 Wpisów samą klawiaturą, then każdy w ≤ 10 s, fokus po zapisie w Projekt, konsola bez błędów CSP.
- Given Lista wpisów, when edycja i kopia z ikon, then pola Projekt i Typ pracy pokazują wartości, a po zapisie okno się zamyka, lista się wczytuje i pasek sukcesu jest na stronie.

## Implementation Notes

- `ToRow` jest na ekranie (`EntriesPage.ToRow`), `Data` jest kolumną kluczową (`IsKey`), wszystkie kolumny `List`; `Ui/` bez zmian (żadnej nowej pozycji AD-23).
- Komunikat Od > Do leży w pasku filtrów (`span.entries-filter-error`, `role="alert"`); `_entries` (lista ze `EntryStore`) jest źródłem `FooterSummary`, `_rows` to wiersze raportu.
- `EntryDialogs.ShowAsync` pokazuje pasek po zamknięciu okna z wyniku `EntryChange`; tekst sukcesu z modelu okna (data, kod Projektu po zapisie) i minut z `Done`.
- `app.css`: zamiast `.entry-dialog-buttons` reguła `.entry-form-buttons` (Zapisz i Anuluj w jednym wierszu, także odstęp przycisku na Nowym wpisie); `.entries-filter-error` na tokenie `--colorStatusDangerForeground1`.
- Weryfikacja ręczna (`dotnet run`, Sesja 10 Wpisów ≤ 10 s, CSP w konsoli) nie była wykonana w tej sesji.

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `EntriesPage.razor` `RowActions`/`ReloadAfterAsync` | Brak blokady akcji w toku: podwójne kliknięcie otwiera dwa okna/potwierdzenia | medium | stary `ReportGrid.RunAsync` miał `_running`; nowy kod woła `Dialogs.*Async` od razu, bez flagi | patch |
| quick | `EntryDialogs.cs` `ShowAsync` | Każdy wynik inny niż `Done` pokazany jako `NotFound` | false | `EntryDialog` zamyka się wyłącznie z `Done` albo `NotFound`; innej ścieżki nie ma | reject |
| quick | `EntryDialogs.cs` / `EntriesPage.razor` | Pasek przed ponownym wczytaniem listy (odwrotnie niż 7.4) | low | pasek widoczny chwilę przy starych wierszach; poprawka wymaga przebudowy `EntryDialogs` (także Widok miesiąca) | reject |
| quick | `WpisyHelp.razor` (+ `ProjektyHelp`, `TypyPracyHelp`) | Pomoc nazywa pole „Szukaj”, ekran pokazuje „Szukaj we wszystkich kolumnach” | low | `Ui/Reports/EodReport.razor:23-24` — tylko placeholder/aria-label | patch |
| quick | AC Sesja wpisywania | Kontrola ręczna nie wykonana | false | to nie wada kodu; kontrola ręczna Autora przed PR | reject |

## Design Notes

Stopka: `EodReport` sam dokleja licznik widocznych wierszy i „po filtrach, z N”; ekran podaje tylko tekst sumy z listy filtrów ekranu:

```razor
<EodReport Definition="Report" Rows="_rows" LayoutKey="@HelpTopics.Wpisy" ShowExport="false"
           PreserveRowOrder="true" Loading="_entries is null" FooterSummary="@Summary(_entries ?? [])">
```

Kolumna Czasu: `new(nameof(EntryRow.Minutes), "Czas", ColumnType.List) { Format = v => Czas.Format((int)v!) }`.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)

**Manual checks (if no CLI):**
- `dotnet run`: Sesja 10 Wpisów samą klawiaturą (≤ 10 s każdy); edycja/kopia z Projektem i Typem pracy w polach; Lista od najnowszych z sumą dla filtrów ekranu; usunięcie — Enter nie potwierdza.
