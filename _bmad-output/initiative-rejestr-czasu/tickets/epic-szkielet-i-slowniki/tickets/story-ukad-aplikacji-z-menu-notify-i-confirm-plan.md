---
title: 'Układ aplikacji z menu, Notify i Confirm'
type: 'feature'
ticket: '3'
created: '2026-11-01'
status: 'built'
baseline_revision: '8e6a000490ada9760cf4298b601942b6ff2787fc'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/glossary.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Po zalogowaniu jest tylko tymczasowy `MainLayout` z napisem „Aplikacja działa.”: brak menu, belki z Wyloguj, pomocy, wspólnych komunikatów i potwierdzeń, a zerwane połączenie pokazuje angielski komunikat Blazora. Ekrany Słowników i epików 2, 3, 5 nie mają na czym stanąć (R19, R26, R31, AD-11, AD-22).

**Approach:** Prawdziwy `Shared/MainLayout` na `FluentLayout` z Fluent UI 5.0.1 (belka, menu boczne, treść), `Shared/PageHeader` jako jedyny nagłówek ekranu, usługi `Shared/Notify` i `Shared/Confirm`, granica błędów obsługująca wyjątki stanu Konta, mechanizm pomocy `Features/Pomoc` z `HelpTopics` i własny polski znacznik zerwanego połączenia.

## Boundaries & Constraints

**Always:**
- **Belka:** nazwa aplikacji, ikona Pomoc `?` (link do `/pomoc/<temat>` z `PageHeader`, bez tematu do `/pomoc`), przełącznik motywu jasny/ciemny (`IThemeService`), link Ustawienia konta (`/ustawienia-konta`) i Wyloguj jako `<form method="post" action="/konto/wyloguj">` z `<AntiforgeryToken />`.
- **Menu:** przycisk Nowy wpis (`/wpisy/nowy`); Rejestr: Widok miesiąca (`/miesiac`), Lista wpisów (`/wpisy`), Eksport (`/eksport`); Słowniki: Projekty (`/projekty`), Typy pracy (`/typy-pracy`); Administracja: Użytkownicy (`/uzytkownicy`) renderowana tylko dla roli `Administrator` (`AppRoles`). Trasy niezbudowanych ekranów prowadzą do istniejącej polskiej strony „nie znaleziono”. W wąskim oknie menu chowa się pod `FluentLayoutHamburger`.
- **`PageHeader`:** tytuł (`h1` + `PageTitle`), opcjonalny podtytuł, okruszki (własne, `nav` z listą linków — Fluent 5.0.1 nie ma breadcrumbu), opcjonalny przycisk Odśwież (`EventCallback`), klucz tematu pomocy przekazany do belki przez sekcję (wzorzec `AccountHelp` / `SectionContent`).
- **`Notify`:** scoped usługa na `INotificationService.ShowMessageBarAsync` z jedną sekcją wyświetlaną przez `FluentMessageBarProvider` w `MainLayout` pod nagłówkiem; metody Sukces / Informacja / Ostrzeżenie / Błąd; komunikaty znikają przy zmianie strony; bez toastów.
- **`Confirm`:** `Task<bool> AskAsync(tytuł, treść, etykieta przycisku głównego)`; Anuluj ma fokus, przycisk główny bez skrótu klawiszowego (Enter ani „Y” nie zatwierdzają), Escape = Anuluj.
- **Błędy ekranu:** granica błędów w `MainLayout` wokół `@Body`: `AccountInactiveException` → `NavigateTo(LoginUrl, forceLoad: true)`, `PasswordChangeRequiredException` → `NavigateTo(ChangePasswordPath, forceLoad: true)`, każdy inny wyjątek → wpis w logu, `Notify` z ogólnym polskim komunikatem i odtworzenie ekranu (`Recover`).
- **Pomoc (AD-22):** jedna trasa `/pomoc/{temat?}` w `Features/Pomoc/` z `[AllowAnonymous]`; `HelpTopics` to jedyna lista (klucz, tytuł, typ komponentu treści, dostęp Anonim / Zalogowany / Administrator); `/pomoc` generuje spis z tematów, do których bieżący User ma dostęp, z tematem `start` pierwszym; brak dostępu → `AccessDeniedContent`, anonim do tematu niepublicznego → logowanie; nieznany temat → polska strona „nie znaleziono”; tematy Anonim w układzie `AccountLayout`, pozostałe w `MainLayout`. W tej historii jeden temat: `start` (Zalogowany) — strona startowa i opis menu.
- **Zerwane połączenie (R31):** element `id="components-reconnect-modal"` z polskim tekstem i linkiem „Odśwież stronę”, pokazywany wyłącznie CSS-em po klasach nadawanych przez `blazor.web.js`; bez JS i bez przycisku ponowienia.
- **Skala** 90/100/110/125% przez media queries na szerokość okna w `app.css` (bez JS); polskie teksty Fluent UI przez istniejący `FluentLocalizer`.
- `Home` przechodzi na `PageHeader` z tematem `start`.

**Never:**
- Ekrany Słowników, Wpisów, Ustawień konta, Użytkowników ani tematy pomocy innych ekranów.
- Osobne `@page "/pomoc/..."`, Markdown lub biblioteki w treści pomocy.
- Skrypty JS własne lub z CDN, komponenty z AD-12 (`FluentNumberInput`, `MaskPattern`, `FluentSortableList`), toasty.
- Zmiany logowania, `CurrentUser`, `AccountLayout` stron `/konto` (poza użyciem `AccessDeniedContent`) i znacznika `[AllowedDuringPasswordChange]` na pomocy — decyduje o nim epik 5 wraz z tematami anonimowymi.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Menu Usera | zalogowany z rolą User | wszystkie sekcje bez Administracja | — |
| Menu Administratora | rola Administrator | widać Administracja → Użytkownicy | — |
| Wyloguj | klik w belce | POST z tokenem, ekran logowania | brak tokenu → 400 z endpointu 4.3 |
| Pomoc z ekranu | `PageHeader Topic="start"` | `?` prowadzi do `/pomoc/start` | brak tematu → `/pomoc` |
| Spis tematów | `/pomoc` | lista dostępnych tematów, `start` pierwszy | — |
| Temat nieznany | `/pomoc/xyz` | strona „nie znaleziono” | — |
| Temat Administratora | User bez roli | widok „Brak dostępu” | — |
| Confirm | Anuluj / Escape / przycisk główny | `false` / `false` / `true` | Enter z fokusem na Anuluj → `false` |
| Notify | `Notify.Error("…")` | `FluentMessageBar` z tekstem w obszarze komunikatów | nawigacja czyści komunikaty |
| Konto zablokowane w obwodzie | ekran rzuca `AccountInactiveException(Blocked)` | pełne przejście na `/konto/logowanie?powod=blocked` | — |
| Wymuszona zmiana hasła | `PasswordChangeRequiredException` | pełne przejście na `/konto/zmiana-hasla` | — |
| Inny wyjątek ekranu | `InvalidOperationException` | ogólny komunikat w `Notify`, wpis w logu, ekran działa dalej | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Components/Layout/MainLayout.razor` -- tymczasowy layout z `<FluentProviders />` i `#blazor-error-ui`; przenieść do `Shared/MainLayout.razor`, zachować `FluentProviders` i `blazor-error-ui`.
- `src/RejestrCzasu/Components/Routes.razor` -- `DefaultLayout="typeof(Layout.MainLayout)"`, `NotFoundPage`; zmienić na `Shared.MainLayout`; `NotAuthorized` bez zmian.
- `src/RejestrCzasu/Components/Pages/NotFound.razor` (`@layout MainLayout`), `Home.razor` (zaślepka) -- dostosować `@layout`/`using`; `Home` na `PageHeader`.
- `src/RejestrCzasu/Components/_Imports.razor` -- `@using RejestrCzasu.Components.Layout` usunąć po przeniesieniu.
- `src/RejestrCzasu/Accounts/AccountLayout.razor`, `AccountHelp.razor` -- wzorzec sekcji (`SectionOutlet`/`SectionContent`) dla tematu pomocy; nie zmieniać.
- `src/RejestrCzasu/Accounts/AccessDeniedContent.razor`, `RedirectToLogin.razor` -- użyć w pomocy dla braku dostępu / anonima.
- `src/RejestrCzasu/Accounts/AccountState.cs` -- `AccountInactiveException.LoginUrl`, `PasswordChangeRequiredException`; `AccountEndpoints.LogoutPath`, `ChangePasswordPath`.
- `src/RejestrCzasu/Data/AppRoles.cs` -- nazwa roli Administrator.
- `src/RejestrCzasu/Program.cs` -- `AddFluentUIComponents` z `FluentLocalizer`; dopisać rejestrację `Notify` i `Confirm` (scoped).
- `src/RejestrCzasu/wwwroot/app.css` -- style globalne; dopisać skalę, reconnect, belkę, okruszki.
- Fluent UI 5.0.1: `FluentLayout`/`FluentLayoutItem Area=LayoutArea.Header|Navigation|Content`, `FluentLayoutHamburger`, `FluentNav`/`FluentNavCategory Title`/`FluentNavItem Href Match`, `FluentAnchorButton`, `FluentButton (AutoFocus, Appearance)`, `INotificationService.ShowMessageBarAsync(MessageBarOptions{Section,Intent,Message})` + `FluentMessageBarProvider Section`, `IDialogService.ShowDialogAsync<T>(DialogOptions)` z `Footer.PrimaryAction/SecondaryAction` (`ShortCut`, `Visible`), `IThemeService.SwitchThemeAsync()`; ikony `Icons.Regular.Size20.QuestionCircle/SignOut/ArrowClockwise/WeatherMoon`.
- `tests/RejestrCzasu.Tests/HomePageTests.cs`, `Shared/ReportGridTests.cs` -- wzorzec bUnit (`JSInterop.Mode = Loose`, `AddFluentUIComponents(FluentLocalizer)`); `AccountsTests.cs` (`Home_IsInteractiveServer_ForLoggedInUser`, `AppClient`) -- wzorzec testu HTTP z zalogowanym Userem.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Shared/MainLayout.razor` (+ `.razor.css` jeśli potrzeba) -- belka, menu z `AuthorizeView Roles`, `SectionOutlet` tematu pomocy, `FluentMessageBarProvider`, granica błędów, znacznik reconnect, `FluentProviders`; usunąć `Components/Layout/` -- AD-11, AD-17, R19, R31.
- [x] `src/RejestrCzasu/Shared/AppErrorBoundary.cs` -- `ErrorBoundary` z obsługą wyjątków stanu Konta, logiem, `Notify` i `Recover` -- jedno miejsce reakcji (AD-17).
- [x] `src/RejestrCzasu/Shared/PageHeader.razor` -- tytuł, podtytuł, okruszki, Odśwież, `SectionContent` tematu -- jedyny nagłówek ekranu (AD-11).
- [x] `src/RejestrCzasu/Shared/Notify.cs` -- scoped usługa na `INotificationService`, czyszczenie przy `LocationChanged` -- AD-11.
- [x] `src/RejestrCzasu/Shared/Confirm.cs` + `Shared/ConfirmDialog.razor` -- okno z własnymi przyciskami w treści (Anuluj z `AutoFocus`, główny bez skrótu), stopka biblioteki ukryta, Escape = Anuluj -- AD-11.
- [x] `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `HelpPage.razor`, `HelpLayout.razor`, `Topics/StartHelp.razor` -- lista tematów, trasa, wybór układu, temat `start` -- AD-22, R26.
- [x] `src/RejestrCzasu/Components/Routes.razor`, `Components/Pages/Home.razor`, `NotFound.razor`, `Components/_Imports.razor`, `Program.cs`, `wwwroot/app.css` -- przepięcie na nowy layout, rejestracja usług, style.
- [x] `tests/RejestrCzasu.Tests/Shared/LayoutTests.cs`, `NotifyConfirmTests.cs`, `Features/HelpTests.cs` -- bUnit dla macierzy (menu wg roli, link `?`, Confirm, Notify, granica błędów z nawigacją `forceLoad`, pomoc: spis, nieznany, brak dostępu); test HTTP: zalogowany `GET /` zawiera formularz Wyloguj z tokenem i znacznik `components-reconnect-modal`.

**Acceptance Criteria:**
- Given aplikacja uruchomiona przez `dotnet run`, when Administrator się loguje, then widzi belkę z `?`, motywem, Ustawieniami konta i Wyloguj oraz menu z sekcją Administracja; User bez roli Administrator — bez niej.
- Given ekran z `PageHeader`, when User klika `?`, then otwiera się strona pomocy tematu ekranu, a z niej spis tematów.
- Given zalogowany User, when zatrzyma się serwer, then pojawia się polski komunikat z linkiem „Odśwież stronę”.
- Given szerokie i wąskie okno, when User zmienia szerokość, then skala się zmienia, a w wąskim menu jest pod hamburgerem; przełącznik zmienia motyw.

## Implementation Notes

- Dodatkowe małe pliki: `Shared/HelpButton.razor` (ikona `?`, używana przez `PageHeader` i jako domyślna treść sekcji w `MainLayout` — ekran bez `PageHeader`, np. `/Error` czy widok braku dostępu, też ma `?` do `/pomoc`; późniejszy `SectionContent` z `PageHeader` przejmuje sekcję), `Shared/Breadcrumb.cs`, `Features/Pomoc/HelpTopicView.razor` (decyzja dostępu dla jednego tematu — testowalna z tematem Administratora, którego jeszcze nie ma w `HelpTopics`).
- `Notify` i tytuł `Confirm` kodują tekst HTML-em: Fluent 5.0.1 renderuje `MessageBarOptions.Message` i `Header.Title` jako (sanityzowany) HTML.
- `Confirm`: akcje stopki biblioteki bez etykiety, niewidoczne i bez `ShortCut` (skrót w `FluentDialog` działa tylko dla widocznej akcji z etykietą); Escape zamyka natywne okno → `Cancel`.
- `AppErrorBoundary`: `Recover()` w `OnAfterRender` (po renderze błędu; `ErrorBoundaryBase` ustawia `CurrentException` dopiero po `OnErrorAsync`); najwyżej 3 automatyczne odtworzenia na stronę, potem komunikat z linkiem „Odśwież stronę” (ochrona przed pętlą ekranu, który rzuca przy każdym renderze); licznik zeruje zmiana strony.
- Nieznany temat pomocy: `NavigationManager.NotFound()` (.NET 10) → `NotFoundPage` routera, przy pełnym ładowaniu HTTP 404 (test HTTP).
- Skala przez `zoom` na `body` z `--app-zoom` (<1280 px: 90%, ≥1920: 110%, ≥2560: 125%); wysokość `FluentLayout` = `100dvh / zoom`.
- Menu i Wyloguj w `AuthorizeView`: anonim na `/not-found` widzi tylko belkę z nazwą, `?` i motywem.

## Plan Change Log

## Review Triage Log

Przebieg 1 (quick, 01.11.2026): medium 1, low 3 (w tym 1 odrzucona), false 0, maybe-false 0.

| Uwaga | Werdykt | Ścieżka | Dowód / działanie |
|---|---|---|---|
| `Notify` czyści komunikaty pokazane w synchronicznej inicjalizacji nowej strony (subskrypcja `LocationChanged` po Routerze) | medium | patch | Router subskrybuje wcześniej i renderuje nową stronę synchronicznie w swoim handlerze; handler `Notify` biegnie po nim i zamyka też nowe komunikaty, w tym ogólny komunikat granicy błędów dla ekranu rzucającego w `OnInitialized`. Poprawka: subskrypcja w konstruktorze, `Notify` tworzony w `Routes` przed Routerem, test bUnit. |
| `AppErrorBoundary` zeruje licznik odtworzeń po nawigacji, gdy nowa strona już zawiodła (do 6 prób zamiast 3) | low | odrzucona | Tylko ekran rzucający przy każdym renderze zaraz po nawigacji; skutek to kilka dodatkowych wpisów w logu, a poprawka wymaga dodatkowego mechanizmu kolejności zdarzeń. |
| Pomoc w `AccountLayout` (anonimowy spis, przyszłe tematy Anonim) bez polskiego znacznika zerwanego połączenia | low | patch | `HelpPage` jest interaktywna, `AccountLayout` nie ma `#components-reconnect-modal`, więc `blazor.web.js` pokaże angielski modal. Poprawka: wspólny komponent znacznika renderowany też przez `HelpLayout`. |
| AD-11 w architekturze nadal mówi `ShowMessageBoxAsync`, kod używa `ShowDialogAsync` | low | defer | Odstępstwo uzasadnione w Design Notes; architekturę zmienia `bmad-architecture`, nie build — wpis w `deferred-work.md`. |

## Design Notes

- **Confirm przez `ShowDialogAsync`, nie `ShowMessageBoxAsync`:** w 5.0.1 domyślny skrót przycisku głównego to Enter, a nie da się ustalić, który przycisk dostaje fokus. AD-11 wymaga fokusu na Anuluj i braku skrótu, więc własne okno w `IDialogService` z przyciskami w treści daje to deterministycznie i testowalnie. To nadal `IDialogService` — zmienia się tylko metoda.
- **`HelpLayout`:** `LayoutComponentBase`, który czyta temat z trasy i renderuje `<LayoutView Layout="…">` z `AccountLayout` (temat Anonim) albo `MainLayout` (reszta i spis dla zalogowanego).
- **Reconnect bez JS:** `blazor.web.js` nadaje elementowi `#components-reconnect-modal` klasy `components-reconnect-show` / `-failed` / `-rejected`; CSS ukrywa go domyślnie i pokazuje po tych klasach.

## Verification

**Commands:**
- `dotnet build` -- expected: bez błędów i ostrzeżeń.
- `dotnet test` -- expected: wszystkie testy zielone (Docker Desktop działa).

**Manual checks (if no CLI):**
- `docker compose up -d` i `dotnet run --project src/RejestrCzasu`: logowanie jako Administrator i jako User (Konto zatwierdzone SQL-em z README) — menu wg roli, Wyloguj wraca do logowania, `?` → pomoc, Ctrl+C na serwerze → polski komunikat zerwanego połączenia, motyw i zwężenie okna działają, karta Sieć tylko `127.0.0.1`.
