---
title: 'Refactor sweep'
type: 'refactor'
ticket: '9'
created: '2026-11-07'
status: 'built'
baseline_revision: '7a6e51b860457fb92b94d64338fa978d7e181923'
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

**Problem:** Ikony wiersza Edytuj / Kopiuj / Usuń i blokada „jedna akcja naraz” (`_busy`, `ChangeAfterAsync`) są powielone w `MonthPage` i `EntriesReport` (Założenie 3.4 „do Refactor sweep”); odłożone uwagi z planów 3.1–3.7 nie są zamknięte w `deferred-work.md`.

**Approach:** Wydzielić ikony do wspólnego komponentu w `Features/Wpisy/`, a blokadę do małej klasy, której instancję trzyma ekran (blokada obejmuje cały raport); każdą uwagę zamknąć albo przenieść nową sekcją `deferred-work.md`.

## Boundaries & Constraints

**Always:** wyłącznie sprzątanie — markup przycisków (ikona, `Title`, `aria-label`, `Appearance`), kolejność „przeładowanie, potem pasek” (AD-10) i ignorowanie drugiego kliknięcia w trakcie akcji bez zmian; istniejące testy przechodzą bez zmian logiki; build bez ostrzeżeń.

**Never:** zmiany w repozytorium eod-ui ani w `src/RejestrCzasu/Ui/`, `wwwroot/ui/` (bez nowej pozycji AD-23); zmiany ikon w Projektach i Typach pracy (inny epik, inne akcje); realizacja uwag „przeniesionych”; zmiana istniejących wpisów `deferred-work.md`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Akcja w wierszu | Edytuj / Kopiuj / Usuń w Widoku miesiąca, Liście wpisów, Nowym wpisie | okno jak dotąd; po zmianie dane ekranu wczytane od nowa, potem pasek | No error expected |
| Drugie kliknięcie | klik innej ikony, gdy okno akcji otwarte | zignorowane (jedno okno) | No error expected |
| Akcja rzuca | wyjątek w akcji | blokada zwolniona, wyjątek dalej do `AppErrorBoundary` jak dotąd | `finally` |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Features/Miesiac/MonthPage.razor` -- `RowActions` z trzema `FluentButton`, `_busy`, `ChangeAfterAsync` (`afterChange = ReloadAsync`), `@inject EntryDialogs`, `using Icons`
- `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor:17-25,79,119-136` -- te same ikony i `_busy`; `afterChange = OnChanged.InvokeAsync`
- `src/RejestrCzasu/Features/Wpisy/EntryDialogs.cs` -- `EditAsync/CopyAsync/DeleteAsync(id, afterChange)`; bez zmian
- `tests/RejestrCzasu.Tests/Features/{MonthPageTests,EntriesPageTests,NewEntryPageTests}.cs` -- szukają przycisków po `Title`; brak testu drugiego kliknięcia
- `_bmad-output/initiative-rejestr-czasu/deferred-work.md` -- wpis 3.7 (fokus H1) i przeniesione z 7.7

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Features/Wpisy/SingleAction.cs` -- `internal sealed class SingleAction` z `Task RunAsync(Func<Task> action)`: gdy trwa poprzednia, wraca od razu; flaga zwalniana w `finally` -- jedno miejsce reguły `_busy`
- [x] `src/RejestrCzasu/Features/Wpisy/EntryRowActions.razor` -- parametry `Id` (`int`), `Gate` (`SingleAction`), `AfterChange` (`Func<Task>`), wszystkie `EditorRequired`; trzy przyciski 1:1 z obecnego markupu wołają `Gate.RunAsync(() => Dialogs.XAsync(Id, AfterChange))` -- usuwa powielone ikony
- [x] `MonthPage.razor` -- `<EntryRowActions Id="id" Gate="_gate" AfterChange="ReloadAsync" />`; usunąć `_busy`, `ChangeAfterAsync`, `@inject EntryDialogs`, `using Icons`; komentarz nagłówka bez zmian treści -- jedna instancja `SingleAction` na ekran
- [x] `EntriesReport.razor` -- `AfterChange="() => OnChanged.InvokeAsync()"`; usunąć `_busy`, `ChangeAfterAsync`, zbędne `@inject`/`using` -- jak wyżej
- [x] `tests/RejestrCzasu.Tests/Features/SingleActionTests.cs` -- drugie wywołanie w trakcie pierwszego się nie wykonuje; po zakończeniu (także wyjątkiem) kolejne się wykonuje -- pokrycie reguły bez testu
- [x] `_bmad-output/initiative-rejestr-czasu/deferred-work.md` -- dopisać na końcu sekcję „## Rozstrzygnięte / Przeniesione (3.9, Refactor sweep, 07.11.2026)” według tabeli Design Notes (rozstrzygnięte i przeniesione z propozycją); nowe wpisy `source_plan` tego planu dla uwag przeniesionych bez wpisu -- verify ticketu

**Acceptance Criteria:**
- Given gałąź po zmianie, when `grep -rnE "_busy|ChangeAfterAsync|Size20\.(Edit|Copy|Delete)" src/RejestrCzasu/Features/Miesiac src/RejestrCzasu/Features/Wpisy`, then trafienia tylko w `EntryRowActions.razor`.
- Given testy ekranów Widoku miesiąca, Listy wpisów i Nowego wpisu, when `dotnet test`, then zielone bez zmian ich treści.
- Given każda uwaga z tabeli Design Notes, when czytam `deferred-work.md`, then jest rozstrzygnięta albo przeniesiona z propozycją.
- Given `git diff`, when sprawdzam `src/RejestrCzasu/Ui/` i `wwwroot/`, then pusty.

## Implementation Notes

- `SingleAction` jest `public sealed`, nie `internal`: parametr `Gate` publicznego komponentu `EntryRowActions` wymaga typu co najmniej tak dostępnego (CS0053).
- Pole flagi w `SingleAction` nazywa się `_running`, żeby grep z pierwszego AC trafiał tylko w `EntryRowActions.razor`.

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `SingleActionTests` | brak testu komponentu „jedna blokada na ekran” | low | plan go nie wymaga; logika bez zmian względem `_busy` | reject |
| quick | `deferred-work.md:97` Playwright | wpis mówi „bez zmian”, choć termin przesunęła decyzja 05.11 | low | wpis 7.7 mówi „po epiku 3” | patch |

## Design Notes

| Źródło | Uwaga | Los |
|---|---|---|
| 3.4 Design Notes, Założenie | ikony i `_busy` powielone w `MonthPage` i `EntriesReport` | sprzątanie tutaj |
| 3.7 przegląd (defer), deferred-work | po autoryzacji fokus nie trafia w H1 (`FocusOnNavigate` raz, przy „Sprawdzanie uprawnień...”) | przeniesione: nowa drobna historia dostępności po Teście wrześniowym (zmienia zachowanie fokusu) |
| 3.1 przegląd (reject) | „Szukaj” trafia w ukryte kolumny Tydzień i Dzień | przeniesione: propozycja historii z pozycją AD-23 (zmiana silnika kopii `Ui/`, zachowanie wszystkich raportów) do decyzji Autora |
| 7.7 przeniesione | reguła przejściowa `.eod-content:not(:has(> .eod-page))` | już rozstrzygnięte w 3.7 (grep w `app.css`: 0) |
| 7.7 przeniesione | klucze `PolishLocalizer` bez testu pokrycia | już rozstrzygnięte w 3.6 (AD-23 poz. 16) |
| 3.3 przegląd (patch) | klasy `month-summary*` bez CSS | już rozstrzygnięte w 3.3 (grep: 0) |
| 3.3 Plan Change Log | literówka nagłówka w zamrożonej Matrix | już rozstrzygnięte: test sprawdza poprawny nagłówek; plan zamrożony, bez działań |
| 3.6 Plan Change Log | nieznany klucz rzuca jak biblioteka | już rozstrzygnięte (DOPASOWANIA poz. 16) |
| 3.4 przegląd (reject) | `/miesiac/9999-12` z Wpisem 27–31.12 rzuca | bez działań (nierealna data) |
| 3.5 Założenie | link `/konto/zmiana-hasla` z pełnym przeładowaniem | nie kod — kontrola ręczna Autora przed PR epiku |
| 7.7 przeniesione, Decision epiku 05.11 | testy Playwright | bez zmian: osobna historia po Teście wrześniowym |

Grep 07.11: brak `TODO`/`FIXME`, brak `/eksport` poza testem `LayoutTests` pilnującym braku pozycji menu (zostaje); nowe klasy CSS epiku (`app-download-link`) używane; `month-*`, `account-settings-password` to zaczepy testów.

Założenie: nazwy `SingleAction` i `EntryRowActions` dobiera implementacja; ważne, że blokada jest jedna na ekran, nie na wiersz.
Założenie: sweep nie czeka na Test wrześniowy (8, hitl) — jego różnice stają się nowymi zgłoszeniami wg opisu historii 8.
Założenie: uwagi odrzucone w przeglądach 3.x poza dwiema w tabeli nie wymagają wpisu.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)
- grep z pierwszego AC -- expected: trafienia tylko w `EntryRowActions.razor`

**Manual checks (if no CLI):**
- `git diff --stat`: dwa nowe pliki w `Features/Wpisy/`, test, `MonthPage.razor`, `EntriesReport.razor`, `deferred-work.md`.
