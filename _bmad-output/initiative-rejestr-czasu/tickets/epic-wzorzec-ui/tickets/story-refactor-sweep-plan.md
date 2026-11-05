---
title: 'Refactor sweep'
type: 'refactor'
ticket: '7'
created: '2026-11-03'
status: 'built'
baseline_revision: '3c33870ca515ae311de190f294c0f20240c2c0bf'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/docs/ui/DOPASOWANIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Po historiach 7.1–7.6 wszystkie ekrany stoją na kopii wzorca (`EodPage`, `EodReport`, `UiMessages`), ale w `src/` i `tests/` zostały wycofane elementy bez użytkowników (`Shared/ReportGrid` z typami, `Notify`, `Confirm`, `ConfirmDialog`, `PageHeader`, `Breadcrumb`, własny `FluentLocalizer`), ich testy i reguły CSS, a jedyny żywy kawałek — `Confirm.HideLibraryFooter` — wołają okna Projektu, Typu pracy i Wpisu. Lista odłożonych uwag z planów 7.1–7.6 nie jest zamknięta.

**Approach:** Przenieść `HideLibraryFooter` (bez zmiany działania) do nowego, niewycofanego miejsca w `Shared/`, potem usunąć wycofane pliki, ich testy i martwe reguły `app.css`; wyczyścić nieużywane reguły `eod.css` z linią w DOPASOWANIA.md; każdą odłożoną uwagę zamknąć albo przenieść w `deferred-work.md`.

## Boundaries & Constraints

**Always:** wyłącznie sprzątanie — żaden widoczny tekst, wygląd ani zachowanie ekranu się nie zmienia; `HideLibraryFooter` działa identycznie (oba przyciski stopki: `Label = null`, `Visible = false`, `ShortCut = null`); istniejące testy ekranów i `UiMessagesTests` przechodzą bez zmian logiki; każda zmiana w `Ui/` lub `wwwroot/ui/` ma linię w `docs/ui/DOPASOWANIA.md`; build bez ostrzeżeń.

**Never:** zmiany w repozytorium eod-ui; zmiany w `src/RejestrCzasu/Ui/*.cs|.razor` (pomocnik nie idzie do `UiMessages`); usuwanie reguły `.eod-content:not(:has(> .eod-page))` (nadal daje odstęp `/Error` i „Sprawdzanie uprawnień...”); dopisywanie kluczy do `PolishLocalizer`; realizacja uwag oznaczonych niżej jako „przeniesione”; zmiany architektury.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Okno Projektu / Typu pracy / Wpisu | otwarcie okna po przeniesieniu pomocnika | stopka biblioteki ukryta, Enter w oknie nie zapisuje ani nie zamyka; Zapisz i Anuluj z formularza okna | No error expected |
| Potwierdzenie usunięcia | `UiMessages.ConfirmAsync` | bez zmian: fokus na Anuluj, Enter nie zatwierdza, Escape = Anuluj | No error expected |
| Teksty Fluent | dowolny komponent Fluent na ekranie | tekst z `PolishLocalizer` jak dotąd (`FluentLocalizer` nie był zarejestrowany) | No error expected |
| `/Error` w `EodShell` | strona bez `EodPage` | odstęp strony jak dotąd (reguła zostaje, nowy komentarz) | No error expected |

**Decyzje (Autor, 2026-11-03):** (1) martwe reguły `eod.css` usuwamy w tej historii i dopisujemy do linii poz. 10 w `docs/ui/DOPASOWANIA.md` (bez nowej pozycji AD-23); (2) `FluentLocalizer` z testami tylko usuwamy, a uzupełnienie kluczy `PolishLocalizer` z testem pokrycia to osobna historia w epiku 3 (wpis w `deferred-work.md`).

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Shared/Confirm.cs` -- `AskAsync` bez użytkowników; `internal static HideLibraryFooter(DialogOptions)` wołany z 3 okien
- `src/RejestrCzasu/Features/Projekty/ProjectDialog.razor:51`, `Features/TypyPracy/WorkTypeDialog.razor:50`, `Features/Wpisy/EntryDialogs.cs:118` -- wywołania `Confirm.HideLibraryFooter`
- `src/RejestrCzasu/Ui/Messages/UiMessages.cs` -- własna prywatna `HideLibraryAction` (7.2); bez zmian
- `src/RejestrCzasu/Shared/{ConfirmDialog.razor, Notify.cs, PageHeader.razor, Breadcrumb.cs, ReportGrid.razor, ReportGrid.razor.css, ReportColumn.cs, ReportAction.cs, ReportGroup.cs}` -- wycofane, bez użytkowników w `src/` (`Breadcrumb` tylko dla `PageHeader`)
- `src/RejestrCzasu/Localization/FluentLocalizer.cs` -- niezarejestrowany (`Program.cs` → `AddEodPlatformUI` → `PolishLocalizer`); jedyny plik katalogu
- `src/RejestrCzasu/wwwroot/app.css` -- `.page-header-messages`, `.page-header`, `.page-header-row`, `.page-subtitle`, `.page-breadcrumbs*` (tylko `PageHeader`); komentarz „Until story 7.7” przy `.eod-content:not(:has(> .eod-page))`
- `src/RejestrCzasu/wwwroot/ui/eod.css` -- reguły bez użytkowników: `.eod-placeholder*` (bez `.eod-skeleton-row`, używanej w `EodReport`), `.eod-edit-*`, `.eod-row-added/modified`, `.eod-due`, `.eod-list` (także człon `.eod-card-fill > .eod-list`), `.eod-roles`, `.eod-kind`, `.eod-cell-line`, `.eod-cell-center`, `.eod-order-no`
- `tests/RejestrCzasu.Tests/Shared/{NotifyConfirmTests.cs, ReportGridTests.cs, ReportGridGroupingTests.cs}` -- testy wycofanych elementów
- `tests/RejestrCzasu.Tests/Shared/UiTestServices.cs` -- `AddLegacyNotifyConfirm`
- `tests/RejestrCzasu.Tests/Shared/LayoutTests.cs:137` -- `HelpIcon_WithPageHeaderTopic_LinksToTopic` (ten sam przypadek pokrywa `HelpIcon_WithPageHelpTopic_LinksToTopic`)
- `tests/RejestrCzasu.Tests/HomePageTests.cs:22` -- asercja `FindComponents<PageHeader>()`
- `tests/RejestrCzasu.Tests/PolishTextsTests.cs` -- dwa testy `FluentLocalizer_*`, `using RejestrCzasu.Localization`, komentarz „until story 7.7”
- `_bmad-output/initiative-rejestr-czasu/deferred-work.md` -- trzy wpisy z planów 7.2 i 7.3

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Shared/DialogFooter.cs` -- nowa `internal static class DialogFooter` z `HideLibraryFooter(DialogOptions)` przeniesioną 1:1 z `Confirm` (z prywatną `HideLibraryAction`) -- jedyna żywa część `Confirm`, bez zależności `Shared/` → `Ui/` i bez zmiany kopii
- [x] `ProjectDialog.razor`, `WorkTypeDialog.razor`, `EntryDialogs.cs` -- `Confirm.HideLibraryFooter` → `DialogFooter.HideLibraryFooter` -- zachowanie bez zmian
- [x] `tests/RejestrCzasu.Tests/Shared/DialogFooterTests.cs` -- test: po wywołaniu obie akcje stopki mają `Visible == false`, `Label == null`, `ShortCut == null` -- zastępuje pokrycie z usuwanego `Confirm_Window_..._NoLibraryFooter`
- [x] usunąć `Shared/Confirm.cs`, `ConfirmDialog.razor`, `Notify.cs`, `PageHeader.razor`, `Breadcrumb.cs`, `ReportGrid.razor`, `ReportGrid.razor.css`, `ReportColumn.cs`, `ReportAction.cs`, `ReportGroup.cs`, `Localization/FluentLocalizer.cs` (z pustym katalogiem) -- wycofane
- [x] usunąć `NotifyConfirmTests.cs`, `ReportGridTests.cs`, `ReportGridGroupingTests.cs`; z `UiTestServices.cs` `AddLegacyNotifyConfirm`; z `LayoutTests.cs` `HelpIcon_WithPageHeaderTopic_LinksToTopic`; z `HomePageTests.cs` asercję `PageHeader`; z `PolishTextsTests.cs` oba testy `FluentLocalizer_*`, zbędny `using` i dopisek o 7.7 w komentarzu (test `AddEodPlatformUI_RegistersPolishLocalizer_OfTheCopy` zostaje) -- testy wycofanych elementów
- [x] `src/RejestrCzasu/wwwroot/app.css` -- usunąć reguły `.page-header*`, `.page-subtitle`, `.page-breadcrumbs*`; komentarz reguły `.eod-content:not(:has(> .eod-page))` → „Treść w EodShell bez EodPage (/Error, Sprawdzanie uprawnień) dostaje odstęp strony z eod.css.” -- martwe reguły, reguła żywa zostaje
- [x] `src/RejestrCzasu/wwwroot/ui/eod.css` -- usunąć reguły z Code Map po sprawdzeniu każdej klasy grepem w `src/` (`.razor`, `.cs`, `.js`); selektory łączone skracać tylko o martwy człon -- uwaga 7.2 (decyzja Autora 2026-11-03)
- [x] `docs/ui/DOPASOWANIA.md` -- poz. 10 dopisać usunięte reguły `eod.css` (AD-23 (10) „nieużywane komponenty”) -- zasada „każda zmiana ma linię”
- [x] `_bmad-output/initiative-rejestr-czasu/deferred-work.md` -- sekcja „Rozstrzygnięte (7.7)” dla trzech wpisów z 7.2/7.3 oraz nowe wpisy „przeniesione” z Design Notes -- verify ticketu

**Acceptance Criteria:**
- Given gałąź po zmianie, when `grep -rnE --exclude-dir=bin --exclude-dir=obj "ReportGrid|ReportColumn<|ReportAction|ReportGroup<|\bNotify\b|\bConfirm\b|\bConfirmDialog\b|PageHeader|FluentLocalizer|AddLegacyNotifyConfirm|page-header|page-breadcrumbs|page-subtitle|report-grid|[Uu]ntil story 7\.7" src/`, then brak trafień.
- Given `tests/` po zmianie, when ten sam grep bez `\bConfirm\b` (nazwa pomocnika w `UiMessagesTests`), then brak trafień.
- Given okno Projektu, Typu pracy lub Wpisu, when testy ekranów (`ProjectsPageTests`, `WorkTypesPageTests`, `EntriesPageTests`, `NewEntryPageTests`) i `UiMessagesTests`, then zielone bez zmian ich treści.
- Given każda odłożona uwaga z tabeli Design Notes, when czytam `deferred-work.md`, then jest „rozstrzygnięta” albo „przeniesiona” z propozycją epiku/historii.

## Implementation Notes

- AC 2 (grep `page-header` w `tests/`) wymagał usunięcia negatywnej asercji `Assert.Empty(cut.FindAll(".page-header"))` z `ProjectsPageTests`, `WorkTypesPageTests`, `EntriesPageTests` i `NewEntryPageTests` (jak asercji `PageHeader` w `HomePageTests`); logika tych testów bez zmian.
- Grep z AC 1 trafia w `src/RejestrCzasu/Ui/Common/PolishLocalizer.cs` (`IFluentLocalizer`, interfejs biblioteki Fluent UI): wzorzec `FluentLocalizer` bez granicy słowa; `Ui/` bez zmian (Never), więc trafienie zostaje jako fałszywie dodatnie.
- `PolishTextsTests.cs`: oprócz `using RejestrCzasu.Localization` usunięte też nieużywane już `using System.Collections` i `using System.Resources`.

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `wwwroot/ui/eod.css` ~149, ~294, ~310 | Komentarze/nagłówki nazywają usunięte elementy (`.eod-list`, `EodPlaceholder`, tabela edycyjna) | low | sprzeczne z linią poz. 10 DOPASOWANIA.md („z ich komentarzami”) | patch |
| quick | AC1 grep `FluentLocalizer` | trafia `IFluentLocalizer` w `Ui/Common/PolishLocalizer.cs` | false | to interfejs biblioteki, nie wycofany komponent; poprawka to tylko edycja planu | reject |
| quick | AC3, cztery testy ekranów | usunięta asercja `.page-header` zmienia treść testów | false | zachowanie testów bez zmian; asercja dotyczyła usuniętej klasy; poprawka to tylko edycja planu | reject |
| quick | `deferred-work.md` sekcja „Rozstrzygnięte” | pozycja Playwright jest przeniesiona, nie rozstrzygnięta | low | wpis sam mówi „przeniesione: nowa historia” | patch |
| quick | `deferred-work.md` stare wpisy 7.2/7.3 | wpisy nadal wyglądają na otwarte | false | reguła pliku zabrania zmian istniejących wpisów; rozstrzygnięcie dopisywane sekcją | reject |

## Design Notes

Odłożone uwagi z planów 7.1–7.6 i ich los:

| Źródło | Uwaga | Los |
|---|---|---|
| 7.2 przegląd #1, deferred-work | martwe reguły `eod.css` | sprzątanie tutaj (decyzja Autora 2026-11-03) |
| 7.2 przegląd #3, deferred-work | `EodUI.showSelectedText` i `eod-scale.js` bez testu zachowania w przeglądarce | przeniesione: nowa historia techniczna „testy E2E w przeglądarce (Playwright)” — propozycja po epiku 3, gdy powstanie Widok miesiąca; wymaga nowego narzędzia, nie sprzątanie |
| 7.3 przegląd #4, deferred-work | pasek po zamknięciu okna wysyła `EntryDialog` zamiast `EntryDialogs` | rozstrzygnięte w 7.5: `EntryDialogs.ShowAsync` pokazuje pasek z wyniku `EntryChange`; tylko wpis w `deferred-work.md` |
| 7.3 Implementation Notes | reguła przejściowa `.eod-content:not(:has(> .eod-page))` | zostaje (żywa dla `/Error` i `Authorizing`), nowy komentarz; przeniesienie `/Error` i „Sprawdzanie uprawnień...” na `EodPage` zmienia wygląd → przeniesione: drobna historia w epiku 3 |
| 7.5 przegląd #3 (reject) | pasek przed ponownym wczytaniem Listy wpisów | przeniesione do rozważenia w historii Widoku miesiąca (epik 3), bo poprawka przebudowuje `EntryDialogs`; zmiana zachowania |
| 7.5 Implementation Notes | kontrola ręczna (Sesja 10 Wpisów ≤ 10 s, CSP) niewykonana | nie kod — kontrola ręczna Autora przed PR epiku (Manual checks niżej) |
| odkryte w 7.7 | po usunięciu `FluentLocalizer` żaden test nie pilnuje, że każdy klucz Fluent ma polski tekst; `PolishLocalizer` nie ma m.in. `FluentInputBase_RequiredMessage`, `PresenceStatus_*` | przeniesione: nowa historia w epiku 3 (decyzja Autora 2026-11-03) |

7.1 #3 i 7.2 #4 były odrzucone, 7.4 i 7.6 nie zostawiły uwag (reguły `.project-form*` itd. już usunięte w 7.4).

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)
- grep z pierwszych dwóch AC -- expected: brak trafień

**Manual checks (if no CLI):**
- `git diff --stat`: poza nowym `DialogFooter` i testem tylko usunięcia, komentarz `app.css`, `eod.css`, DOPASOWANIA.md, deferred-work.md; `git diff` w `src/RejestrCzasu/Ui/` pusty.
- Przed PR epiku (Autor): `dotnet run` — okna Projektu i Wpisu bez stopki biblioteki, Enter nie potwierdza usunięcia, Sesja 10 Wpisów ≤ 10 s, konsola bez naruszeń CSP.
