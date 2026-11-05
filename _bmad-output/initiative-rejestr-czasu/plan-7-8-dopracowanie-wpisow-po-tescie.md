---
title: 'Dopracowanie Wpisów po teście: pełna szerokość raportu, układ formularza, lista Miesiąca na Nowym wpisie'
type: 'feature'
ticket: ''
created: '2026-11-04'
status: 'built'
baseline_revision: '61404964ea1f544c28b77b9657d188b20c5ce341'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-wzorzec-ui/tickets/story-nowy-wpis-lista-wpisow-i-okno-wpisu-na-wzorcu-plan.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Po teście epiku 7 tabela `EodReport` nie wypełnia karty (Lista wpisów, Projekty, Typy pracy), pola `EntryForm` są wąskie, `/wpisy/nowy` nie pokazuje zapisanych Wpisów, a fokus startowy w Data jest blokowany („Autofocus processing was blocked because a document already has a focused element.”).

**Approach:** Przywrócić kaskadę stylów wzorca w `App.razor`; rozłożyć `EntryForm` siatką w `app.css`; wydzielić raport Wpisów do jednego komponentu `EntriesReport`, hostowanego na `/wpisy` i pod formularzem na `/wpisy/nowy` (Wpisy bieżącego Miesiąca); fokus Data ustawiany jawnie przez stronę. Eksport i paginacja to osobny plan 7.9 (`plan-7-9-eksport-i-paginacja-wpisow.md`); tu obie listy zostają z `ShowExport="false"`, bez paginacji.

**Decyzja (Autor, 2026-11-04):** lista na `/wpisy/nowy` zawsze pokazuje Wpisy bieżącego Miesiąca (czas polski), bez filtrów ekranu; te same kolumny, akcje wiersza i stopka „Wpisy: N, suma Czasu: X” co Lista wpisów; wczytuje się ponownie po każdym zapisie na stronie i po każdej akcji wiersza. Lista wpisów (`/wpisy`) bez zmian zachowania.

## Boundaries & Constraints

**Always:** eod-ui bez zmian; `Ui/` i `wwwroot/ui/` bez zmian (gdyby jednak — pozycja AD-23 i linia w `docs/ui/DOPASOWANIA.md` w tej samej zmianie); jedno źródło `Report`/`ToRow`/`Summary` (AD-9); suma stopki z listy `EntryStore.ListAsync` (AD-5); akcje wiersza tylko przez `EntryDialogs` z blokadą akcji w toku (AD-10); Sesja wpisywania bez zmian (R4, R5, R16): kolejność fokusu Data → Projekt → Typ pracy → Opis → Czas w DOM, Enter, fokus w Projekt po zapisie, data zostaje, Enter z przycisku menu nie zapisuje; `PolishCalendar`/`YearMonth`.

**Never:** zmiany `EntryStore`, walidatorów, schematu, sygnatur `EntryDialogs`; filtry ekranu na `/wpisy/nowy`; eksport i paginacja (plan 7.9); wizualne przestawianie pól (CSS `order`, `grid-area` wbrew DOM); zmiany w `Shared/` poza `EntryForm`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Szerokość | `/wpisy`, `/projekty`, `/typy-pracy`, ekran ≥ 1280 px | tabela na całą szerokość karty, „Akcje” przy prawej krawędzi | szeroka treść: przewijanie poziome jak dotąd |
| Formularz szeroki | karta ≥ ok. 900 px | jeden wiersz: Data i Czas wąskie, Projekt i Typ pracy szersze, Opis najszerszy; Zapisz pod spodem | — |
| Formularz wąski | okno `DialogSize.Medium`; telefon 360 px | zawijanie w kolejności DOM (np. Data+Projekt+Typ pracy / Opis+Czas; na telefonie pole pod polem), bez przewijania poziomego | — |
| Start Nowego wpisu | wejście z menu lub z adresu | fokus w Data, brak komunikatu Autofocus w konsoli | Enter z menu: nic się nie zapisuje |
| Zapis | Wpis z datą bieżącego Miesiąca | pasek sukcesu, fokus w Projekt, lista Miesiąca ponownie, nowy Wpis na górze | brakujący Słownik: jak dotąd, lista bez zmian |
| Wpis spoza Miesiąca | zapis z datą poprzedniego Miesiąca | zapis jak dotąd, lista go nie pokazuje | — |
| Przełom Miesiąca | strona otwarta 31.10 przed północą, zapis po północy | lista pokazuje listopad (Miesiąc liczony przy każdym wczytaniu) | — |
| Akcja wiersza | Edytuj/Kopiuj/Usuń na liście Nowego wpisu | okno lub potwierdzenie z `EntryDialogs`, po `true` lista ponownie | Wpis usunięty: ostrzeżenie, lista ponownie |
| Układ Usera | sortowanie/grupowanie na `/wpisy` | `/wpisy/nowy` ma osobny układ (`LayoutKey`) | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Components/App.razor` -- **przyczyna pełnej szerokości:** link `RejestrCzasu.styles.css` stoi po `EodHead` i `app.css`. Aplikacja nie ma własnych `.razor.css`, więc ten pakiet zawiera tylko `@import` `Microsoft.FluentUI…bundle.scp.css`, a reguła Fluent `.fluent-data-grid{width:auto}` wraca w kaskadzie po `eod.css:131` (`width:100%`, ta sama specyficzność). Odtworzone w Chrome bez okna na makiecie z prawdziwymi arkuszami: 788 px zamiast 1364 px. Demo eod-ui nie ma tego linku. Po poprawce wracają też inne reguły `eod.css` przykryte przez arkusz Fluent (zamierzony wygląd wzorca).
- `src/RejestrCzasu/Features/Wpisy/EntriesPage.razor` -- `Report`, `ToRow`, `Summary`, `RowActions` z `_busy`, `ReloadAfterAsync`, `EodReport` → do `EntriesReport`; zostają pasek filtrów, `ReloadAsync` z `_version`, `RangeErrorMessage`.
- `src/RejestrCzasu/Features/Wpisy/NewEntryPage.razor` -- `EodPage` bez `Fill`, jedna karta z `EntryForm`; `SaveAsync` (Done → `FocusProjectAsync`, pasek).
- `src/RejestrCzasu/Shared/EntryForm.razor` -- `Autofocus="true"` na Data; `FocusProjectAsync` jako wzór `FocusDateAsync`; komentarz o Enter z menu.
- `src/RejestrCzasu/Components/Routes.razor:18` -- `FocusOnNavigate Selector="h1"`: fokus na nagłówku `EodPage`, zanim formularz (po asynchronicznym wczytaniu opcji) się wyrenderuje, stąd blokada `autofocus`; Data prawdopodobnie bez fokusu po wejściu z menu — potwierdzić w przeglądarce.
- `src/RejestrCzasu/wwwroot/app.css` -- brak reguł układu `.entry-form` (tylko `.entry-form-buttons`).
- `src/RejestrCzasu/Features/Pomoc/Topics/WpisyNowyHelp.razor` -- temat Nowego wpisu; `WpisyHelp.razor` bez zmian.
- `tests/RejestrCzasu.Tests/Features/NewEntryPageTests.cs` (l. 71 `Assert.True(date.Autofocus)`, l. 116 wzór `VerifyFocusAsyncInvoke`), `EntriesPageTests.cs` (l. 227–237, 596: `EntriesPage.Report/ToRow/Summary`).

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Components/App.razor` -- usunąć link `RejestrCzasu.styles.css`, z komentarzem: aplikacja nie używa izolacji CSS, pakiet powtarzał arkusz Fluent po `eod.css`; przy pierwszym `.razor.css` link wraca przed `<EodHead />` -- kaskada wzorca (AD-11), pełna szerokość każdego `EodReport`.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor` (nowy) -- `internal static Report`, `ToRow`, `Summary` przeniesione bez zmian; parametry `Entries` (`IReadOnlyList<EntryRow>?`, `null` = `Loading`), `LayoutKey`, `EventCallback OnChanged`; `EodReport` z `ShowExport="false"`, `PreserveRowOrder`, `FooterSummary`, `RowActions` (Edytuj/Kopiuj/Usuń, blokada `_busy`) → `EntryDialogs` → `OnChanged` po `true` -- jedno źródło (AD-9, AD-10).
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesPage.razor` -- pasek filtrów + `<EntriesReport Entries="_entries" LayoutKey="@HelpTopics.Wpisy" OnChanged="ReloadAsync" />` w tej samej karcie.
- [x] `src/RejestrCzasu/Features/Wpisy/NewEntryPage.razor` -- `EodPage Fill`; karta 1: `EntryForm` z `AutofocusDate="false"`; karta 2 (`EodCard NoPadding Fill`): `EntriesReport` z `LayoutKey="@HelpTopics.WpisyNowy"`; `ReloadMonthAsync`: `m = YearMonth.From(PolishCalendar.Today(TimeProvider))` przy każdym wczytaniu, `Store.ListAsync(new EntryFilter(m.FirstDay, m.LastDay, null, null))`, z `_version`; po Done: `FocusProjectAsync`, pasek, potem `ReloadMonthAsync`; `OnAfterRenderAsync`: raz, gdy `_form` pierwszy raz istnieje, `_form.FocusDateAsync()`.
- [x] `src/RejestrCzasu/Shared/EntryForm.razor` -- `[Parameter] bool AutofocusDate = true` (okno Wpisu zostaje przy autofokusie natywnego `<dialog>`), `@ref` pola Data i `public Task FocusDateAsync()`; komentarz nagłówka zaktualizowany; kolejność i klasy pól bez zmian -- R4, R16.
- [x] `src/RejestrCzasu/wwwroot/app.css` -- `.entry-form`: `container-type: inline-size` i siatka; progi `@container`: szeroki (≥ ok. 900 px) 5 kolumn `minmax(9rem,10rem) minmax(12rem,1fr) minmax(12rem,1fr) minmax(16rem,2fr) minmax(7rem,8rem)`, średni (≥ ok. 480 px) 3 kolumny z Opisem przez 2 kolumny, wąski 1 kolumna; `.entry-form-buttons` przez całą szerokość; pola Fluent wewnątrz `width:100%` (w DevTools sprawdzić, który element niesie `class` — `fluent-field` czy pole) -- bez `order`.
- [x] `src/RejestrCzasu/Features/Pomoc/Topics/WpisyNowyHelp.razor` -- sekcja o liście Wpisów bieżącego Miesiąca pod formularzem: kolumny, ikony Edytuj/Kopiuj/Usuń, stopka z sumą, odświeżanie po zapisie, Wpis z innego Miesiąca widoczny na Liście wpisów -- R26.
- [x] `tests/RejestrCzasu.Tests/Features/EntriesPageTests.cs` -- odwołania na `EntriesReport.Report/ToRow/Summary`; zachowania bez zmian.
- [x] `tests/RejestrCzasu.Tests/Features/NewEntryPageTests.cs` -- zamiast `Autofocus`: `FocusAsync` na Data po starcie; lista Miesiąca (Wpis z poprzedniego Miesiąca niewidoczny, stopka z sumą); ponowne wczytanie po zapisie i po akcji wiersza; ostatnie `FocusAsync` po zapisie = Projekt mimo przeładowania; przełom Miesiąca przez `FakeTimeProvider`.

**Acceptance Criteria:**
- Given `/wpisy`, `/projekty` i `/typy-pracy` na szerokim ekranie, when strona się wczyta, then `table.fluent-data-grid` ma szerokość `.eod-report-wrap` (DevTools), a pozostałe ekrany wyglądają poprawnie po zmianie kaskady.
- Given `/wpisy/nowy` otwarte przyciskiem menu, when Sesja 10 Wpisów samą klawiaturą, then fokus startuje w Data, każdy Wpis ≤ 10 s, po zapisie fokus w Projekt i nowy Wpis na liście, w konsoli brak komunikatu Autofocus i naruszeń CSP.
- Given `git diff`, when przegląd, then brak zmian w `Ui/` i `wwwroot/ui/`, a `EntriesPage` nie definiuje `Report`/`ToRow`/`Summary`.

## Implementation Notes

- `EntriesReport` (`Features/Wpisy/EntriesReport.razor`) ma `Report`, `ToRow`, `Summary` bez zmian treści, wiersze budowane w `OnParametersSet` tylko dla nowej listy; blokada `_busy` i `EntryDialogs` → `OnChanged`. `EntriesPage` nie definiuje już `Report`/`ToRow`/`Summary`; `Ui/` i `wwwroot/ui/` bez zmian.
- **Fokus Data:** `FluentDatePicker` 5.0.1 nie ma `Element` (brak `ElementReference`), więc zamiast `@ref` pola `FocusDateAsync` woła istniejące `EodUI.focusElement` z `eod.js` (bez zmian w kopii) z selektorem `EntryForm.DateFieldSelector` (`form.entry-form .entry-form-date fluent-text-input`); na stronie jest wtedy tylko formularz Nowego wpisu. Test sprawdza kolejność wywołań: selektor Data na starcie, potem element Projekt po zapisie (przeładowanie listy nie przenosi fokusu).
- **Siatka `EntryForm`:** zapytanie `@container` nie może stylować samego kontenera, więc kontenerem (`container: entry-form / inline-size`) jest element zawierający formularz (`:has(> .entry-form)` — ciało karty, ciało okna), a siatka leży na `.entry-form`. Opis przez 2 kolumny w progu średnim wybierany `:is(.entry-description, :has(.entry-description))` (działa niezależnie od tego, czy `class` trafia na `fluent-field`, czy na pole). Bez `order`/`grid-area`.
- `NewEntryPageTests`: zegar `SettableTimeProvider` (w pliku testu) zamiast `FakeTimeProvider` (pakiet niepodpięty); nowe testy: lista Miesiąca (Wpis z sierpnia niewidoczny, stopka, ikony, własny `LayoutKey`), zapis z datą poprzedniego Miesiąca, przełom Miesiąca, Usuń i Edytuj z listy z ponownym wczytaniem.
- Weryfikacja: build 0/0; `dotnet test` 556/565 zielonych — build w osobnym `--artifacts-path`, bo uruchomiona aplikacja (`dotnet run`) blokowała `bin/`; 9 czerwonych to testy szukające katalogu repozytorium/dll aplikacji względem `bin/` (`ShellTests` ×2, `ApplicationTests` ×4, `DataIsolationGuardTests` ×3), niezwiązane ze zmianą — do powtórzenia zwykłym `dotnet test` po zatrzymaniu aplikacji. Kontrole ręczne (szerokość tabel w DevTools, konsola bez komunikatu Autofocus, 360 px, Sesja 10 Wpisów) niewykonane.

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `app.css` `@container entry-form (min-width: 900px)` | siatka 5 kolumn przelewa się przy 900–959 px | low | minima 56rem + 4rem odstępów = 60rem > 900px | patch |
| quick | `NewEntryPage.razor` `OnInitializedAsync` | formularz czeka na zapytanie listy Miesiąca | medium | jeden `OnInitializedAsync` renderuje dopiero po wszystkich await | patch |
| quick | `NewEntryPage.razor` `SaveAsync` + `EntryForm._saving` | szybki kolejny Enter ginie podczas przeładowania listy | medium | przeładowanie w `OnSave` trzyma blokadę zapisu; regresja R5/R16 | patch |
| quick | `EntriesReport.razor` `Report.Title` | tabela na Nowym wpisie ogłaszana jako „Lista wpisów” | low | `EodReport` renderuje `aria-label=Definition.Title` | patch |
| quick | `EntryForm.FocusDateAsync` | selektor globalny, cichy przy braku trafienia | maybe-false | przy starcie strony jedyny formularz; wymaga sprawdzenia w przeglądarce (test Autora) | reject |
| quick | AC 1–2 | kontrole ręczne niewykonane, testy 556/565 | false | pełny `dotnet test` po zatrzymaniu aplikacji: 565/565; kontrole ręczne w teście całości Autora | reject |
| quick | `NewEntryPageTests.cs` `TextInput` | `.Single` → `.First` osłabia testy | low | dotyczy wszystkich wywołań, potrzebne tylko w teście okna | patch |
| quick | `app.css` `:has(> .entry-form)` w oknie | ciało okna może się zwęzić przez `inline-size` | maybe-false | zależy od tego, czy `DialogSize.Medium` ma stałą szerokość; sprawdzenie w przeglądarce (test Autora) | reject |

## Design Notes

`FocusOnNavigate` (h1) wygrywa z `autofocus` pola wstawianego po asynchronicznym wczytaniu opcji, więc strona ustawia fokus jawnie po pierwszym renderze formularza; w oknie Wpisu działa autofokus `<dialog>`.

```razor
<EodCard NoPadding="true" Fill="true">
    <EntriesReport Entries="_month" LayoutKey="@HelpTopics.WpisyNowy" OnChanged="ReloadMonthAsync" />
</EodCard>
```

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)

**Manual checks (if no CLI):**
- Raz na końcu (Autor, `dotnet run`, razem z planem 7.9): szerokość tabel na trzech ekranach; formularz na Nowym wpisie, w oknie Edycji i przy 360 px; Sesja 10 Wpisów; lista Miesiąca po zapisie i po akcjach wiersza.
