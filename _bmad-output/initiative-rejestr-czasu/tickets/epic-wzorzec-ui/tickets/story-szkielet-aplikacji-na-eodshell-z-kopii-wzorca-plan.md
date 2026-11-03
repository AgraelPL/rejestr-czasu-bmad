---
title: 'Szkielet aplikacji na EodShell z kopii wzorca'
type: 'feature'
ticket: '3'
created: '2026-11-03'
status: 'built'
baseline_revision: 'b7f810d899852f3a707720c99585b861d34dbe3e'
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
---

<frozen-after-approval reason="human-owned intent â€” do not modify unless human renegotiates">

## Intent

**Problem:** Kopia wzorca w `Ui/` (7.1, 7.2) nie jest podĹ‚Ä…czona: aplikacja ma wĹ‚asny ukĹ‚ad (`MainLayout` na `FluentLayout`), wĹ‚asny lokalizator, `Notify` i `Confirm`, a granica bĹ‚Ä™dĂłw nie obejmuje okien.

**Approach:** Tracer bullet. PodĹ‚Ä…czyÄ‡ kopiÄ™ (`AddEodPlatformUI`, `UseEodPolishCulture`, `EodHead` w `App.razor`), oprzeÄ‡ `Shared/MainLayout` na `EodShell` wedĹ‚ug AD-11, przenieĹ›Ä‡ wszystkie komunikaty i potwierdzenia na `UiMessages`, pomoc, spis pomocy, stronÄ™ startowÄ… i â€žnie znalezionoâ€ť na `EodPage`, ? w belce na `Shared/PageHelp` (AD-22), a obejĹ›cie comboboxa EntryForm na `EodUI.showSelectedText`. Ekrany SĹ‚ownikĂłw i WpisĂłw zostajÄ… na `PageHeader` do 7.4/7.5.

## Boundaries & Constraints

**Always:** CSP `script-src 'self'` bez wyjÄ…tkĂłw i bez zasobĂłw z zewnÄ…trz (NFR-1); zachowanie R1â€“R8, R16, R19 bez zmian poza wyglÄ…dem i zasadami komunikatĂłw z AD-11 (sukces/informacja 7 s, ostrzeĹĽenie/bĹ‚Ä…d zostajÄ…, potwierdzenie bez Enter, fokus Anuluj, Escape = Anuluj); wartoĹ›ci Usera w treĹ›ci `ConfirmAsync` kodowane `HtmlEncode`; wyjÄ…tki stanu Konta (AD-17) z treĹ›ci strony i z okna (np. zapis w `EntryDialog`) obsĹ‚uguje ta sama `AppErrorBoundary` w `MainLayout`; build bez ostrzeĹĽeĹ„, testy Testcontainers bez zmian logiki.

**Never:** zmiany w eod-ui ani w kopii `Ui/` (nowa potrzeba = nowa pozycja AD-23, wtedy HALT); przenoszenie ProjektĂłw, TypĂłw pracy, Nowego wpisu, Listy wpisĂłw i okna Wpisu na `EodPage`/`EodReport` (7.4, 7.5); zmiany `Accounts/AccountLayout` i stron `/konto` (7.6); usuwanie plikĂłw `Notify`, `Confirm`, `ConfirmDialog`, `PageHeader`, `ReportGrid`, `FluentLocalizer` (7.7); zmiany `Dockerfile` i workflow CI.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Belka i menu | zalogowany User | belka â€žRejestr czasuâ€ť, ?, motyw, Ustawienia konta, Wyloguj (POST z antyforgery); Primary Nowy wpis; Rejestr, SĹ‚owniki; bez Administracji | â€” |
| Administrator | rola Administrator | dodatkowo Administracja â†’ UĹĽytkownicy | â€” |
| ? ekranu | ekran z `PageHelp Topic="projekty"` (teĹĽ przez `PageHeader`) | ? prowadzi do `/pomoc/projekty` | bez `PageHelp` â€” `/pomoc` |
| Nie znaleziono | `/xyz` albo pozycja menu bez ekranu | `EodPage` â€žNie znalezionoâ€ť w `EodShell` | â€” |
| UsuniÄ™cie | UsuĹ„ Projekt / Typ pracy / Wpis | okno `ConfirmAsync`, Enter nie potwierdza, Escape anuluje; wynik w pasku strony | â€” |
| BĹ‚Ä…d ekranu | wyjÄ…tek w treĹ›ci strony | log + pasek bĹ‚Ä™du `GeneralMessage` w stronie po odtworzeniu | po 3 prĂłbach widok z linkiem â€žOdĹ›wieĹĽ stronÄ™â€ť |
| Stan Konta w oknie | `AccountInactiveException` przy zapisie w `EntryDialog` | `NavigateTo` logowania z `forceLoad` | â€” |
| Zerwane poĹ‚Ä…czenie | `components-reconnect-modal` pokazany | polski tekst w klasach `eod-*`, link â€žOdĹ›wieĹĽ stronÄ™â€ť | bez JS i ponowienia |
| Edycja / kopia Wpisu | okno z ustawionym Projektem i Typem pracy | pola pokazujÄ… tekst wartoĹ›ci (`EodUI.showSelectedText`) | â€” |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Program.cs` -- `AddFluentUIComponents(FluentLocalizer)` â†’ `AddEodPlatformUI()`; rÄ™czne `CultureInfo.DefaultThread*` + `UseRequestLocalization(polish.Name)` â†’ `UseEodPolishCulture()` (to samo miejsce w potoku).
- `src/RejestrCzasu/Components/App.razor` -- link CSS Fluent â†’ `<EodHead />` (ma CSS Fluent, reboot, `ui/eod.css`, skrypty); `app.css` i `RejestrCzasu.styles.css` zostajÄ… po nim. `Routes.razor` -- `@inject Notify` i komentarz usunÄ…Ä‡ (czyszczenie paskĂłw robi `EodShell`).
- `src/RejestrCzasu/Ui/Layout/EodShell.razor` -- sloty `HeaderActions`, `NewCase`, `Menu` (`FluentNavItem`/`FluentNavCategory` wewnÄ…trz `FluentNav` kopii), `ChildContent`; zawiera `FluentProviders`, wiÄ™c `AppErrorBoundary` musi objÄ…Ä‡ caĹ‚y `EodShell`. Przyciski belki: `Appearance="Transparent"` + `eod-onbrand`. Tylko uĹĽywaÄ‡.
- `src/RejestrCzasu/Shared/MainLayout.razor` -- obecny ukĹ‚ad, menu i formularz Wyloguj do przeniesienia; `HelpSection` zostaje staĹ‚Ä… sekcji ?.
- `src/RejestrCzasu/Shared/AppErrorBoundary.cs` -- `Notify.Error` â†’ `UiMessages.ShowErrorAsync`; pasek musi trafiÄ‡ do sekcji `EodSections.Page` odtworzonej strony (gdy granica obejmuje `EodShell`, przy bĹ‚Ä™dzie znika teĹĽ dostawca paskĂłw).
- `src/RejestrCzasu/Shared/HelpButton.razor` -- przycisk ?; dodaÄ‡ `eod-onbrand`. Nowy `Shared/PageHelp.razor` = `SectionContent` z `HelpButton Topic`. `PageHeader.razor` -- zamiast wĹ‚asnego `SectionContent` uĹĽywa `PageHelp`; przejĹ›ciowo renderuje `FluentMessageBarProvider Section="@EodSections.Page"` pod nagĹ‚Ăłwkiem (inaczej paski ekranĂłw jeszcze bez `EodPage` znikajÄ… â€” `UiMessages` poĹ‚yka brak dostawcy).
- `Components/Pages/{Home,NotFound}.razor`, `Features/Pomoc/{HelpPage,HelpTopicView}.razor` -- `PageHeader` â†’ `EodPage` (+ `EodCard`, `PageHelp`, `Breadcrumb` jako `BreadcrumbItem`); NotFound z `EodEmptyState`. `HelpLayout` i `AccountHelp` bez zmian (tematy anonimowe w `AccountLayout` â€” 7.6).
- `Features/Projekty/ProjectsPage.razor`, `Features/TypyPracy/WorkTypesPage.razor`, `Features/Wpisy/{NewEntryPage,EntryDialog}.razor`, `Features/Wpisy/EntryDialogs.cs` -- `Notify.X(msg)` â†’ `Messages.ShowXAsync(msg)`; `Confirm.AskAsync(title, msg, "UsuĹ„")` â†’ `ConfirmAsync("UsunÄ…Ä‡ â€¦?", "<p>â€¦</p>", "UsuĹ„")`. `Confirm.HideLibraryFooter` w `EntryDialogs` zostaje.
- `Features/ScreenServiceCollectionExtensions.cs` -- bez rejestracji `Notify`/`Confirm`; komentarz: wymaga `AddEodPlatformUI`.
- `Shared/ReconnectModal.razor` -- znacznik i tekst bez zmian, klasy `eod-*` (np. `eod-card`, `eod-h*`, `eod-caption`); style widocznoĹ›ci w `app.css`.
- `Shared/EntryForm.razor` (`OnAfterRenderAsync`) -- import moduĹ‚u â†’ `JS.InvokeVoidAsync("EodUI.showSelectedText", ...)`; usunÄ…Ä‡ `Shared/EntryForm.razor.js`.
- `wwwroot/app.css` -- usunÄ…Ä‡ reguĹ‚y starego ukĹ‚adu (`app-header`, `app-nav`, `app-menu`, `app-messages`, `--app-layout-height`â€¦), zostawiÄ‡ reguĹ‚y ekranĂłw na `PageHeader` i `/konto`.
- `Features/Pomoc/Topics/StartHelp.razor` -- opis belki i menu po zmianie (R26).
- `tests/RejestrCzasu.Tests/Shared/UiTestServices.cs` -- `AddAppUi` przez `AddEodPlatformUI()` (+ `Notify`/`Confirm` tylko dla `NotifyConfirmTests`). Testy do dopasowania: `Shared/LayoutTests`, `HomePageTests`, `Features/{Help,ProjectsPage,WorkTypesPage,EntriesPage,NewEntryPage}Tests`, `PolishTextsTests`; wzĂłr okna: `Ui/UiMessagesTests.cs`.

## Tasks & Acceptance

**Execution:**
- [x] `Program.cs`, `Components/App.razor`, `Components/Routes.razor` -- podĹ‚Ä…czenie kopii jak w Code Map.
- [x] `Shared/MainLayout.razor` -- `AppErrorBoundary` wokĂłĹ‚ `EodShell`; `HeaderActions`: `SectionOutlet` ?, motyw (`AppearanceSettings.SetThemeAsync` jasnyâ†”ciemny), Ustawienia konta, Wyloguj; `NewCase`: Primary Nowy wpis; `Menu` wedĹ‚ug NFR-6 z `AuthorizeView Roles` dla Administracji; `ReconnectModal`; domyĹ›lny `PageHelp` bez tematu.
- [x] `Shared/AppErrorBoundary.cs` -- `UiMessages`; pasek bĹ‚Ä™du pokazany po odtworzeniu treĹ›ci (granica obejmuje okna i `FluentProviders`).
- [x] `Shared/PageHelp.razor`, `Shared/HelpButton.razor`, `Shared/PageHeader.razor` -- jak w Code Map.
- [x] `Components/Pages/Home.razor`, `NotFound.razor`, `Features/Pomoc/HelpPage.razor`, `HelpTopicView.razor` -- `EodPage`.
- [x] Ekrany i `EntryDialogs.cs`, `ScreenServiceCollectionExtensions.cs` -- `UiMessages` zamiast `Notify`/`Confirm`.
- [x] `Shared/ReconnectModal.razor`, `wwwroot/app.css` -- klasy wzorca, sprzÄ…tanie starego ukĹ‚adu.
- [x] `Shared/EntryForm.razor` -- `EodUI.showSelectedText`; usunÄ…Ä‡ `Shared/EntryForm.razor.js`.
- [x] `Features/Pomoc/Topics/StartHelp.razor` -- treĹ›Ä‡ zgodna z belkÄ… i menu.
- [x] Testy -- `UiTestServices` na `AddEodPlatformUI`; dopasowaÄ‡ wymienione testy do `EodShell`/`UiMessages`; dodaÄ‡ bUnit dla wierszy macierzy (belka i menu wg roli, ? z `PageHelp` i domyĹ›lne, NotFound na `EodPage`, usuniÄ™cie bez Enter, bĹ‚Ä…d ekranu â†’ pasek w stronie, `AccountInactiveException` z okna â†’ `forceLoad`, reconnect, wywoĹ‚anie `EodUI.showSelectedText`).

**Acceptance Criteria:**
- Given aplikacja po zmianie, when `grep` w `src/` po `Notify`, `Confirm.AskAsync`, `EntryForm.razor.js`, `AddFluentUIComponents`, then trafienia tylko w plikach do usuniÄ™cia w 7.7 i `Ui/`.
- Given `dotnet run` lokalnie, when przejĹ›cie po wszystkich ekranach i oknach, then konsola przeglÄ…darki bez bĹ‚Ä™dĂłw CSP, a funkcje R1â€“R8 dziaĹ‚ajÄ… jak przed zmianÄ….

## Implementation Notes

- Strona startowa: H1 „Rejestr czasu pracy”, tytuł karty „Rejestr czasu” (`BrowserTitle`) — test Testcontainers logowania szuka „Rejestr czasu pracy” na `/`.
- Menu: `FluentNavSectionHeader` (Rejestr, Słowniki, Administracja) jak w MainLayout wzorca zamiast `FluentNavCategory`; pozycje z ikonami Regular/Filled 20.
- Motyw: `AppearanceSettings.GetThemeAsync`; przy „system” stan bieżący z `IThemeService.IsDarkModeAsync`, potem `SetThemeAsync` na przeciwny.
- `AppErrorBoundary`: po `Recover()` do 3 renderów próby `UiMessages.ShowErrorAsync` (null = brak sekcji → kolejny render); widok „Odśwież stronę” renderuje się bez `EodShell` (klasy `eod-page`, `eod-h2`).
- Ekrany na `PageHeader`: odstęp strony z reguły przejściowej `.eod-content:not(:has(> .eod-page))` w `app.css`.
- `EntryMessages.DeleteQuestionHtml` — treść okna usunięcia Wpisu kodowana `HtmlEncode`.
- `docs/ui/DOPASOWANIA.md` poz. 9: `Shared/EntryForm.razor.js` usunięty w tej historii.

## Plan Change Log

## Review Triage Log

**Przebieg 1 (quick):** high 0 · medium 1 · low 3 · false 0 · maybe-false 0.

| # | Znalezisko | Werdykt | Trasa | Dowód / działanie |
|---|-----------|---------|-------|-------------------|
| 1 | Błąd `MissingDictionaryItem` w `EntryDialog` idzie paskiem do sekcji strony pod modalem | medium | patch | `UiMessages` zawsze kieruje do `EodSections.Page`; dawny panel `Notify` pływał nad oknem (z-index 1000), więc to regres; poprawka: błąd w treści okna (AD-10) |
| 2 | `_messageAttempts` zostaje po ostatnim nieudanym odtworzeniu; po nawigacji ogólny błąd na zdrowej stronie | low | patch | `OnErrorAsync` zeruje licznik tylko w gałęzi `< MaxRecoveriesPerPage`; `OnLocationChanged` → `Recover` → pasek; poprawka: zerowanie w `OnLocationChanged` |
| 3 | `PageHeader` daje tytuł karty „{Title} — Rejestr czasu pracy”, `EodPage` „{Title} · Rejestr czasu” | low | patch | `PageTitleText` bez zmian, a AD-23 poz. 7 ustala „Rejestr czasu”; poprawka: format `EodPage` |
| 4 | Pasek sukcesu po zamknięciu okna wysyła `EntryDialog`, a nie `EntryDialogs` (AD-10) | low | defer | Tak było już z `Notify` przed zmianą, działa (usługa scoped); okno Wpisu na wzorcu to 7.5 |

## Design Notes

Granica bĹ‚Ä™dĂłw obejmuje `EodShell`, bo `FluentProviders` (okna) siedzi w nim; przy bĹ‚Ä™dzie caĹ‚a treĹ›Ä‡ Ĺ‚Ä…cznie z dostawcÄ… paskĂłw jest na chwilÄ™ zdjÄ™ta, dlatego pasek ogĂłlnego bĹ‚Ä™du idzie dopiero po `Recover()`, gdy strona znowu ma sekcjÄ™ `EodSections.Page` (sprawdziÄ‡ testem; w razie wyĹ›cigu pokazaÄ‡ go po kolejnym renderze).

Przycisk motywu zostaje w belce (funkcja z epiku 1, opisana w StartHelp), teraz przez `AppearanceSettings` wzorca zamiast `IThemeService`.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeĹĽeĹ„, 0 bĹ‚Ä™dĂłw
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)

**Manual checks (if no CLI):**
- `dotnet run`: szkielet `EodShell` z menu, ? otwiera temat ekranu, `/xyz` daje â€žnie znalezionoâ€ť na `EodPage`, usuniÄ™cie Projektu pyta w oknie i Enter nie potwierdza, edycja i kopia Wpisu pokazujÄ… Projekt i Typ pracy, konsola bez bĹ‚Ä™dĂłw CSP.
