---
title: 'Strona błędu i „Sprawdzanie uprawnień” na EodPage'
type: 'refactor'
ticket: '7'
created: '2026-11-05'
status: 'built'
baseline_revision: '9c39a88dd69d17b62d400fef20921a50359c111b'
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

**Problem:** `/Error` i komunikat `<Authorizing>` w `Routes.razor` renderują się w `EodShell` bez `EodPage`, więc odstęp strony daje im reguła przejściowa `.eod-content:not(:has(> .eod-page))` w `app.css` (deferred-work, plan 7.x refactor sweep), a wyglądają inaczej niż reszta wzorca.

**Approach:** obie treści owinąć w `EodPage` (+ `EodCard`) z tymi samymi tekstami, sprawdzić, że żadna treść w `EodShell` nie zostaje bez `.eod-page`, i usunąć regułę wraz z komentarzem.

## Boundaries & Constraints

**Always:** teksty bez zmian („Błąd”, „Podczas przetwarzania żądania wystąpił błąd.”, „Identyfikator żądania:” + `RequestId`, „Sprawdzanie uprawnień...”); `Error.razor` zachowuje `[AllowAnonymous]`, `[AllowedDuringPasswordChange]`, `@page "/Error"` i odczyt `RequestId`; tylko klasy i komponenty kopii wzorca (`Ui/`); `NotAuthorized` bez zmian.

**Never:** zmiany w `Program.cs` (`UseExceptionHandler("/Error")`, `UseStatusCodePagesWithReExecute("/not-found")`), `AppErrorBoundary`/`MainLayout` (AD-17), `Ui/` (kopia wzorca), `AccessDeniedContent`, stronach `/konto` (`AccountLayout`, poza `EodShell`); nowy temat pomocy (AD-22) dla `/Error`; nowe style zastępujące regułę.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| /Error bezpośrednio | anonimowy `GET /Error` | 200; `.eod-shell .eod-page` z `h1.eod-h1` „Błąd”, opis „Podczas przetwarzania żądania wystąpił błąd.”; identyfikator żądania w `EodCard`, gdy jest | No error expected |
| /Error po wyjątku HTTP | wyjątek w potoku (nie-Development) → re-execute `/Error` | ta sama strona `EodPage`; status 500 jak dotąd | handler bez zmian |
| Wyjątek w obwodzie | ekran rzuca w trakcie renderu | `AppErrorBoundary` jak dotąd (pasek błędu, przekierowania AD-17, `.app-error` bez shella) | istniejące testy `ErrorBoundary_*` zielone bez zmian |
| Sprawdzanie uprawnień | stan uwierzytelnienia w toku (`SetAuthorizing`) | `.eod-shell .eod-page .eod-card` z „Sprawdzanie uprawnień...”, bez nagłówka strony | No error expected |
| Brak reguły przejściowej | każda trasa w `MainLayout` | treść `.eod-content` zawsze ma bezpośrednie dziecko `.eod-page` (odstęp z `eod.css` `.eod-page`) | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Components/Pages/Error.razor` -- strona błędu, dziś `<h1>`/`<p>` + `<PageTitle>Błąd</PageTitle>`; domyślny layout `MainLayout` z `Routes`
- `src/RejestrCzasu/Components/Routes.razor` -- `<Authorizing><p>Sprawdzanie uprawnień...</p></Authorizing>`; renderuje się w `DefaultLayout` (`MainLayout` → `EodShell`)
- `src/RejestrCzasu/wwwroot/app.css` (l. 15–18) -- reguła przejściowa i jej komentarz do usunięcia
- `src/RejestrCzasu/Ui/Page/EodPage.razor` -- `Title`/`Description`/`ChildContent`, sama emituje `<PageTitle>` („{Title} · Rejestr czasu”, bez `Title` „Rejestr czasu”)
- `src/RejestrCzasu/Components/Pages/NotFound.razor` -- wzór: strona `EodPage` w `MainLayout`
- `src/RejestrCzasu/Accounts/AccessDeniedContent.razor` -- `NotAuthorized` już ma `section.eod-page`
- `tests/RejestrCzasu.Tests/Shared/LayoutTests.cs` -- `RenderLayout`, `_auth`, test `NotFound_IsEodPageInTheShell` (wzór), testy `ErrorBoundary_*`
- `tests/RejestrCzasu.Tests/AccountsTests.cs`, `AppClient.cs` -- testy HTTP (`app.Http.GetAsync`)

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Components/Pages/Error.razor` -- zamienić `<PageTitle>`, `<h1>` i `<p>` na `<EodPage Title="Błąd" Description="Podczas przetwarzania żądania wystąpił błąd.">`; blok identyfikatora żądania (bez zmian treści) w `<EodCard>` tylko przy `ShowRequestId` -- jedna anatomia strony jak `NotFound`
- [x] `src/RejestrCzasu/Components/Routes.razor` -- `<Authorizing>`: `<EodPage><EodCard><p>Sprawdzanie uprawnień...</p></EodCard></EodPage>` (bez `Title`) -- odstęp i karta z `eod.css`
- [x] `src/RejestrCzasu/wwwroot/app.css` -- usunąć regułę `.eod-content:not(:has(> .eod-page))` z komentarzem -- po grepie nic jej już nie potrzebuje (Design Notes)
- [x] `tests/RejestrCzasu.Tests/Shared/LayoutTests.cs` -- dodać `Error_IsEodPageInTheShell` (RenderLayout z `Error`: `.eod-shell .eod-page`, H1 „Błąd”, opis) i `Authorizing_IsEodPageInTheShell` (render `Routes` przy `_auth.SetAuthorizing()`: `.eod-shell .eod-page .eod-card` z „Sprawdzanie uprawnień...”, brak `.eod-page-head`) -- macierz
- [x] `tests/RejestrCzasu.Tests/AccountsTests.cs` -- test HTTP: anonimowy `GET /Error` → 200, HTML zawiera `eod-page` i „Podczas przetwarzania żądania wystąpił błąd.” -- strona realnie w potoku (atrybuty dostępu)

**Acceptance Criteria:**
- Given aplikacja zbudowana, when szukam `:has(> .eod-page)` w `src/`, then brak wyników.
- Given każda strona `@page` z `MainLayout` (Home, NotFound, Error, MonthPage, HelpPage, ProjectsPage, WorkTypesPage, EntriesPage, NewEntryPage) i treści `Routes` (`Authorizing`, `NotAuthorized`), when renderują się w `EodShell`, then bezpośrednim dzieckiem `.eod-content` jest `.eod-page`.
- Given istniejące testy `ErrorBoundary_*` i `NotFound_IsEodPageInTheShell`, when `dotnet test`, then przechodzą bez zmian.

## Design Notes

Grep `@page` (16 plików): bez `<EodPage>` są tylko `Error.razor` (ta historia) i `Accounts/{AccessDenied,ChangePassword,Login,Register}.razor` — te mają `AccountLayout` (bez `EodShell`/`.eod-content`), więc reguła ich nie dotyczy. `NotAuthorized` → `AccessDeniedContent` (`section.eod-page`) albo `RedirectToLogin` (bez treści); stan „ekran pada przy każdym renderze” `AppErrorBoundary` pokazuje bez shella (`.app-error`). Zatem po tej historii regułę można usunąć. Historie 3.4–3.6 nie dodają stron bez `EodPage`; jeśli przy implementacji grep pokaże nową stronę `@page` w `MainLayout` bez `EodPage`, regułę zostawić i zapisać to w Implementation Notes.

Założenie: tytuł karty przeglądarki `/Error` zmienia się z „Błąd” na „Błąd · Rejestr czasu” (domyślny `EodPage`, AD-23 poz. 7) — to nie jest zmiana treści strony, tylko jednolity format jak „Nie znaleziono · Rejestr czasu”.

Założenie: „Sprawdzanie uprawnień...” bez `Title` (bez H1 i nagłówka) — to stan przejściowy, nie strona; `EodPage` ustawi chwilowo tytuł karty „Rejestr czasu”, co jest akceptowalne. `FocusOnNavigate Selector="h1"` po zakończeniu autoryzacji trafia w H1 docelowej strony.

Założenie: `/Error` bez `PageHelp` — brak tematu w `HelpTopics` (AD-22); `MainLayout` daje domyślne „?” do spisu `/pomoc`.

Założenie: jeśli render całego `Routes` w bUnit okaże się niepraktyczny (Router/nawigacja), test renderuje `AuthorizeRouteView` z `RouteData` strony `Home`, `DefaultLayout=MainLayout` i tym samym fragmentem `Authorizing` przy `SetAuthorizing()`; decyzję zapisać w Implementation Notes.

Założenie: `deferred-work.md` i architektura bez zmian (brak w nich zapisu reguły poza deferred-work; oznaczenie pozycji jako zamkniętej zostaje dla PO przy zamknięciu epiku).

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie testy zielone, w tym nowe `Error_*`, `Authorizing_*` i test HTTP `/Error`
- `grep -rn "has(> .eod-page" src/` -- expected: brak wyników

**Manual checks (if no CLI):**
- Lokalnie `/Error` i (przy wolnym połączeniu) „Sprawdzanie uprawnień...” mają odstęp i kartę jak `/not-found`; pozostałe ekrany bez zmiany odstępów.

## Implementation Notes

- Test `Authorizing_IsEodPageInTheShell` renderuje pełne `Routes` (bez wariantu zapasowego). Sprawdzone grepem: każda strona w `MainLayout`, także `/ustawienia-konta` z historii 5, zaczyna się od `EodPage`; reguła przejściowa nie jest już potrzebna.
- Założenie z Design Notes o fokusie na H1 po autoryzacji jest błędne: `FocusOnNavigate` próbuje raz, przy renderze treści `Authorizing` (bez H1). Zachowanie sprzed zmiany, odłożone do `deferred-work.md`.

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `Routes.razor` `FocusOnNavigate` + `Authorizing` | po autoryzacji fokus nie trafia w H1 strony | medium | `FocusOnNavigate` działa raz, przy treści `Authorizing` bez H1; tak samo przed zmianą | defer |
| quick | `LayoutTests.Error_IsEodPageInTheShell` | gałąź identyfikatora żądania bez testu | low | treść identyfikatora bez zmian, tylko w `EodCard` | reject |
| quick | `AccountsTests.ErrorPage_Anonymous_IsEodPage` | asercja `eod-page` jako podciąg | low | spełnia zadanie; układ sprawdza test bUnit | reject |
| quick | plan Implementation Notes | brak notatek o fokusie i `/ustawienia-konta` | low | uzupełnione | patch |
