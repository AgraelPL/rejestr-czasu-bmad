---
title: 'Strony /konto na wzorcu'
type: 'feature'
ticket: '6'
created: '2026-11-03'
status: 'built'
baseline_revision: '2483e128113e64f27deb20d98530ccb9c4398584'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/docs/ui/DOPASOWANIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Statyczne strony `/konto/logowanie`, `/konto/rejestracja`, `/konto/zmiana-hasla`, `/konto/brak-dostepu` i `Accounts/AccountLayout` mają własny wygląd (`account-*` z kolorami hex, „Rejestr czasu pracy”), niezgodny z wzorcem eod-ui.

**Approach:** `AccountLayout` dostaje belkę w kolorze marki (nazwa „Rejestr czasu”, link Pomoc `eod-onbrand`) i wyśrodkowaną kolumnę treści; strony składają się z czystego HTML z klasami `eod-*` (`section.eod-page`, `h1.eod-h1`, `p.eod-caption`) i `EodCard` z formularzem. Logika stron, nazwy pól i komunikaty bez zmian (AD-15).

## Boundaries & Constraints

**Always:** strony zostają SSR (`[ExcludeFromInteractiveRouting]`, `[AllowAnonymous]`, `[EnableRateLimiting]`, `[AllowedDuringPasswordChange]` jak dziś); `EditForm` z `FormName`, `method="post"`, natywne `InputText`/`<button type="submit">` (nazwy `Input.*`, `id`, `autocomplete`, `maxlength`, `autofocus` bez zmian — testy i Testcontainers je wysyłają); błędy i stany przez `AccountMessages` w treści strony; style tylko tokenami Fluent 2 (bez hex) w `wwwroot/app.css`; tytuł karty „{Tytuł} · Rejestr czasu” (AD-23 poz. 7); CSP `script-src 'self'` bez wyjątków, żadnych skryptów inline ani zasobów z zewnątrz (AD-12).

**Never:** `EodShell`, menu, `EodPage` i `UiMessages` na stronach `/konto` (EodPage niesie dostawcę pasków `UiMessages`); interaktywne komponenty Fluent (`FluentTextInput`, `FluentButton`, `FluentValidator`) w formularzach SSR; zmiany `AccountSignIn` (trim loginu z e797afb i 2d91999), `AccountPassword`, `AccountEndpoints`, `IdentitySetup`, middleware, CSP; zmiany w eod-ui i w `Ui/`/`wwwroot/ui/` (potrzeba = nowa pozycja AD-23 w `docs/ui/DOPASOWANIA.md`, wtedy HALT); nowe tematy pomocy `logowanie`/`rejestracja`/`zmiana-hasla` (epic-administracja-kont-i-pomoc); usuwanie `PageHeader`/`Notify` (7.7).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Wygląd | GET dowolnej strony `/konto/...` | belka marki „Rejestr czasu” + link Pomoc do tematu strony; `h1.eod-h1`, formularz w `.eod-card`; bez znaczników `"type":"server"` | — |
| Logowanie OK | aktywne Konto, login ze spacjami | przekierowanie na lokalny `ReturnUrl`, ciasteczko | — |
| Złe hasło / stan | złe hasło; poprawne przy `pending`/`blocked`; `?powod=blocked` | komunikat w treści (`AccountMessages`, błąd/ostrzeżenie) jak dziś | po 5 próbach `LockedOutMessage` |
| Rejestracja | poprawne dane / zajęty login / krótkie hasło | `RegisteredMessage` + link logowania / błędy Identity w treści, wpisany login zostaje | walidacja pól pod polem (`ValidationMessage`) |
| Zmiana hasła | dobrowolna / wymuszona | po zmianie `?zmieniono=1` z sukcesem / `/`; wymuszona bez pola „Obecne hasło”, z Wyloguj (POST z antyforgery) | błędy w treści strony |
| Brak dostępu | `/konto/brak-dostepu`; zalogowany bez roli w `Routes` | ten sam `AccessDeniedContent` w klasach wzorca, w `AccountLayout` i w `EodShell` | — |
| Pomoc anonimowa | anonim na `/pomoc`; temat `Anonymous` | `HelpLayout` → nowy `AccountLayout` z `ReconnectModal`; temat z linkiem Pomoc przez `AccountHelp` | temat niepubliczny → logowanie |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Accounts/AccountLayout.razor` -- `header` (belka, `eod-appname` „Rejestr czasu”, `eod-header-spacer`, `SectionOutlet HelpSection`) + `main` (kolumna treści). Stała `HelpSection` zostaje (używa jej `AccountHelp`).
- `Accounts/AccountHelp.razor` -- link `<a>` z klasą `eod-onbrand` (zostaje `href` `/pomoc/{temat}` lub `/pomoc`).
- `Accounts/{Login,Register,ChangePassword,AccessDenied}.razor` -- tylko znaczniki i `PageTitle`; blok `@code` bez zmian. Struktura: `section.eod-page` › `h1.eod-h1` (+ `p.eod-caption` np. z `AdministratorAccessNotice`) › `EodCard` z `AccountMessages` i `EditForm` › akapit z linkiem (rejestracja/logowanie/„Wróć do aplikacji”).
- `Accounts/AccessDeniedContent.razor` -- wspólny dla `/konto/brak-dostepu`, `Routes.NotAuthorized` i `HelpTopicView`: `section.eod-page` + `h1.eod-h1` + `EodCard` z tekstem i linkiem (h1 „Brak dostępu” zostaje — `HelpTests`).
- `Accounts/AccountMessages.razor` -- `FluentMessageBar` zostaje (wygląd paska wzorca, UI_SPEC 11); dodać `role` alert/status jak `Ui/Messages/EodMessageBar.razor`; klasa odstępu na token.
- `Ui/Page/EodCard.razor` -- tylko użyć (czysty HTML, działa w SSR). `Ui/Page/EodPage.razor` -- nie używać na `/konto`.
- `wwwroot/ui/eod.css` -- źródło klas (`eod-page`, `eod-h1`, `eod-caption`, `eod-card*`, `eod-appname`, `eod-onbrand`, `--eod-header-height`, `--eod-gutter`); bez zmian.
- `wwwroot/app.css` -- sekcja „Static /konto pages”: przepisać `account-*` na tokeny (belka `--colorBrandBackground`/`--colorNeutralForegroundOnBrand`, wysokość `--eod-header-height`; pola i przycisk wyglądem Fluent: `--colorNeutralStroke1`, `--borderRadiusMedium`, Primary `--colorBrandBackground`, fokus `--colorStrokeFocus2`); `.validation-message` na `--colorStatusDangerForeground1`; usunąć nieużywane reguły.
- `Features/Pomoc/HelpLayout.razor`, `HelpTopicView.razor`, `HelpTopics.cs` -- bez zmian; nowy `AccountLayout` musi poprawnie zmieścić `EodPage` pomocy (interaktywna, w kolumnie treści).
- Testy: `tests/RejestrCzasu.Tests/AccountsTests.cs`, `AccountSessionTests.cs`, `AppClient.cs`, `ForwardedHeadersTests.cs` (Testcontainers, POST `Input.*`, CSP, `href="/pomoc…"`) — muszą przejść bez zmian logiki; `Features/HelpTests.cs` (AccountLayout, brak dostępu).

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Accounts/AccountLayout.razor`, `AccountHelp.razor` -- belka i kolumna jak w Code Map.
- [x] `Accounts/Login.razor`, `Register.razor`, `ChangePassword.razor`, `AccessDenied.razor`, `AccessDeniedContent.razor` -- znaczniki wzorca, `PageTitle` „… · Rejestr czasu”, logika nietknięta.
- [x] `Accounts/AccountMessages.razor` -- `role` wg rodzaju, odstęp tokenem.
- [x] `wwwroot/app.css` -- reguły `/konto` na tokenach Fluent, bez hex.
- [x] Testy -- bUnit w nowym `tests/RejestrCzasu.Tests/Shared/AccountLayoutTests.cs` (lub w `HelpTests`): belka z „Rejestr czasu” i linkiem Pomoc do tematu z `AccountHelp`, bez `EodShell`/`.eod-nav`; `HelpTopicView` z tematem `Anonymous` w `AccountLayout` daje link Pomoc tematu; `AccessDeniedContent` w klasach `eod-*`. W `AccountsTests` (test GET stron `/konto`) dodać asercje `class="eod-h1"` i brak `eod-shell`; reszta testów Konta bez zmian.

**Acceptance Criteria:**
- Given aplikacja po zmianie, when `grep` w `src/RejestrCzasu/Accounts` i sekcji `/konto` w `app.css` po `#[0-9a-f]{3,6}` i „Rejestr czasu pracy”, then brak trafień.
- Given `git diff` historii, when przegląd `Accounts/*.cs` i bloków `@code` stron `/konto`, then bez zmian (poza `AccountMessages`).
- Given `dotnet run` lokalnie, when rejestracja, logowanie (zły login, `pending`, `blocked`, 5 nieudanych prób), wylogowanie, zmiana hasła dobrowolna i wymuszona oraz `/pomoc` jako anonim, then działają jak przed zmianą w wyglądzie wzorca (także motyw ciemny i szerokość telefonu), konsola bez błędów CSP.

## Implementation Notes

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `ApplicationTests.cs:47` | Asercja tytułu z „·” nie przejdzie przez kodowanie HTML | false | `LoginPage_Returns200_WithContentSecurityPolicy` uruchomiony: 1/1 zielony | reject |
| quick | `app.css` `.account-header` | Brak reguły wąskiego ekranu z `eod.css` (padding 8px, safe-area) | low | `wwwroot/ui/eod.css` `@media (max-width: 900px)` ma override dla belki `EodShell`; belka `/konto` go nie ma | patch |

## Design Notes

UI_SPEC wyłącza logowanie z zakresu wzorca, więc strony `/konto` składają się z klas wzorca bez nowego komponentu w `Ui/`. Fluent web components i `FluentValidator` wymagają obwodu lub nie wysyłają pól pod nazwami `Input.*` w formularzu SSR — dlatego natywne pola wystylowane tokenami. `section.eod-page` w `AccessDeniedContent` jest poprawną stroną także w `EodShell` (bez reguły przejściowej `.eod-content:not(:has(> .eod-page))`).

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone, w tym `AccountsTests`, `AccountSessionTests`, `ForwardedHeadersTests`, `HelpTests` (Docker dla Testcontainers)

**Manual checks (if no CLI):**
- `dotnet run`: scenariusze z ostatniego kryterium; DevTools → Console bez naruszeń CSP, Network tylko `localhost`.
