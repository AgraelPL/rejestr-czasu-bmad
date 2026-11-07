# Deferred work

- source_plan: `tickets/epic-konta-i-wdrozenie/tickets/story-ci-na-pr-z-kontrola-migracji-i-ochrona-gaezi-main-plan.md`
  summary: Historia 4.8 zakłada, że do `main` trafia tylko kod z zielonym CI, a ochrona `main` nie jest egzekwowana (prywatne repo na GitHub Free).
  evidence: Reguła ochrony ma status Not enforced; Autor wybrał zasadę ręczną (docs/admin/README.md). Wdrożenie z `main` w 4.8 powinno samo sprawdzać zielony `build-and-test` dla wdrażanego commita albo uruchamiać testy przed budową obrazu.
- source_plan: `tickets/epic-konta-i-wdrozenie/tickets/story-ci-na-pr-z-kontrola-migracji-i-ochrona-gaezi-main-plan.md`
  summary: Po pierwszym wdrożeniu (4.8) nic nie pilnuje zamrożenia migracji `Initial`.
  evidence: MigrationRulesTests pomija klasę `Initial` po nazwie, a `has-pending-model-changes` nie wykrywa zmiany jej `Up`. W 4.8 dodać kontrolę, że `Initial` jest najwcześniejsza wg ID z `[Migration]` i że jej plik się nie zmienił (np. przypięty hash).
- source_plan: `tickets/epic-konta-i-wdrozenie/tickets/story-ci-na-pr-z-kontrola-migracji-i-ochrona-gaezi-main-plan.md`
  summary: Dockerfile z historii 4.5 powinien używać obrazu SDK w wersji z `global.json` (10.0.403).
  evidence: CI bierze SDK z `global.json` przez `setup-dotnet`; inny tag `mcr.microsoft.com/dotnet/sdk` w obrazie oznacza build innym kompilatorem niż ten sprawdzony na PR.
- source_plan: `_bmad-output/initiative-rejestr-czasu/tickets/epic-konta-i-wdrozenie/tickets/story-nowy-schemat-initial-z-identity-i-userid-plan.md`
  summary: Tworzenie wiersza account_settings przy rejestracji i przy pierwszym Administratorze koliduje z regułą AD-16, że tryb systemowy nie zapisuje encji IUserOwned.
  evidence: AccountSettings implementuje IUserOwned (historia 4.2), ApplyUserOwnership rzuca w trybie systemowym, a AD-7 i AD-21 każą tworzyć ten wiersz w Accounts/ i Startup/ (tryb systemowy); trzeba nazwać trzecie miejsce w AD-16 (np. Accounts wiąże kontekst z nowym Kontem) przed planowaniem historii 4.3.

## Rozstrzygnięte (Sofia, 31.10.2026)

- „Historia 4.8 zakłada zielone CI w `main`…” — nieaktualne: repozytorium staje się publiczne (prd NFR-2), ochrona `main` będzie egzekwowana (Decision w epic-konta-i-wdrozenie).
- „Po pierwszym wdrożeniu nic nie pilnuje zamrożenia `Initial`” — przeniesione do notatek historii 4.8.
- „Dockerfile z historii 4.5 … SDK 10.0.403” — przeniesione do notatek historii 4.5.
- „Tworzenie wiersza account_settings … koliduje z AD-16” — rozstrzygnięte: Decision z 31.10.2026 w epic-konta-i-wdrozenie (wiersz powstaje leniwie w epiku 3); opisy historii 4.3 i 4.4 to uwzględniają.

- source_plan: `_bmad-output/initiative-rejestr-czasu/tickets/epic-szkielet-i-slowniki/tickets/story-reportgrid-lista-z-filtrem-licznikiem-i-ikonami-akcji-plan.md`
  summary: Pakiet `Microsoft.FluentUI.AspNetCore.Components.Icons` 5.0.0 jest zbudowany pod Fluent UI 4.14.4 (`lib/net8.0`), a aplikacja używa Components 5.0.1 (medium, niezweryfikowane).
  evidence: Testy bUnit renderują ikony jako SVG i restore nie zgłasza ostrzeżeń NU; rozstrzygnie uruchomienie pierwszego ekranu z ikonami akcji w przeglądarce (historia 1.7) albo pakiet ikon wydany pod v5.

- source_plan: `_bmad-output/initiative-rejestr-czasu/tickets/epic-szkielet-i-slowniki/tickets/story-ukad-aplikacji-z-menu-notify-i-confirm-plan.md`
  summary: AD-11 w `architecture-rejestr-czasu.md` każe budować `Shared/Confirm` na `IDialogService.ShowMessageBoxAsync`, a historia 1.3 używa `ShowDialogAsync<ConfirmDialog>` (low).
  evidence: W Fluent UI 5.0.1 przycisk główny okna komunikatu ma domyślny skrót Enter i nieustalony fokus, sprzeczne z resztą AD-11; zaktualizować AD-11 przez `bmad-architecture` przy najbliższej zmianie architektury.

- source_plan: `_bmad-output/initiative-rejestr-czasu/tickets/epic-szkielet-i-slowniki/tickets/story-projekty-lista-dodawanie-edycja-dezaktywacja-i-usuwanie-plan.md`
  summary: Kliknięcie ikony akcji w `ReportGrid` odświeża tylko siatkę, nie ekran; Projekty obchodzą to opakowaniem `ThenRender`, które musi powtórzyć każdy kolejny ekran (low).
  evidence: Akcja wiersza to `Func<T, Task>` wywoływany z handlera komponentu `ReportGrid`; poprawka w siatce (np. `EventCallback` lub odświeżenie rodzica) usunie obejście z ekranów — kandydat do Refactor sweep 1.10.

- Rozstrzygnięte 01.11.2026 (Autor, przed historią 1.10):
  - `ReportGrid` sam renderuje ekran, do którego należy akcja wiersza, i ignoruje kliknięcie w trakcie trwającej akcji; obejście `ThenRender` usunięte z Projektów i Typów pracy.
  - AD-11 zaktualizowane: `Confirm` na `ShowDialogAsync` z `ConfirmDialog`; komunikaty `Notify` znikają po 5 s, w pływającym panelu w prawym górnym rogu.
  - Pakiet ikon: `Microsoft.FluentUI.AspNetCore.Components.Icons` ma własną numerację i deklaruje zależność minimalną od rdzenia 4.14.x (także najnowsza 5.0.1), więc z Components 5.0.1 jest zgodny; podniesiony do 5.0.1, ikony renderują się w testach i w przeglądarce.


- source_plan: `tickets/epic-wzorzec-ui/tickets/story-dopasowanie-komunikatow-szkieletu-i-eodhead-w-kopii-wzorca-plan.md`
  summary: W kopii wzorca `wwwroot/ui/eod.css` zostały reguły pominiętych komponentów (`.eod-placeholder*`, `.eod-edit-*`, `.eod-row-added/modified`, `.eod-due`, `.eod-list`, `.eod-roles`, `.eod-kind`) bez użytkowników w `Ui/`.
  evidence: Przegląd 7.2 sprawdził każdą klasę `.eod-*` z eod.css wobec `src/RejestrCzasu/Ui`; kandydat do Refactor sweep 7.7.
- source_plan: `tickets/epic-wzorzec-ui/tickets/story-dopasowanie-komunikatow-szkieletu-i-eodhead-w-kopii-wzorca-plan.md`
  summary: `EodUI.showSelectedText` (i `eod-scale.js`) mają tylko testy tekstu źródła, bez testu zachowania w przeglądarce.
  evidence: Repo nie ma środowiska testów JS; zachowanie (tekst po pojawieniu się pola, koniec po 30 klatkach) sprawdzono ręcznie w node w 7.2.
- source_plan: `tickets/epic-wzorzec-ui/tickets/story-szkielet-aplikacji-na-eodshell-z-kopii-wzorca-plan.md`
  summary: Pasek sukcesu i ostrzeżenia po zamknięciu okna Wpisu wysyła sam `EntryDialog` po `Dialog.CloseAsync()`, a AD-10 każe robić to `EntryDialogs` po zamknięciu okna.
  evidence: Przegląd 7.3; tak było już z `Notify`, działa dzięki usłudze scoped; do uporządkowania w 7.5 (okno Wpisu na wzorcu).

## Rozstrzygnięte (7.7, Refactor sweep, 03.11.2026)

- „W kopii wzorca `wwwroot/ui/eod.css` zostały reguły pominiętych komponentów…” — rozstrzygnięte w 7.7: reguły `.eod-placeholder*`, `.eod-edit-*`, `.eod-row-added/modified`, `.eod-due`, `.eod-list`, `.eod-roles`, `.eod-kind`, `.eod-cell-line`, `.eod-cell-center`, `.eod-order-no` usunięte, linia w poz. 10 `docs/ui/DOPASOWANIA.md` (decyzja Autora 03.11.2026).
- „Pasek sukcesu i ostrzeżenia po zamknięciu okna Wpisu wysyła sam `EntryDialog`…” — rozstrzygnięte w 7.5: `EntryDialogs.ShowAsync` pokazuje pasek z wyniku `EntryChange` po zamknięciu okna.

## Przeniesione (7.7, Refactor sweep, 03.11.2026)

- „`EodUI.showSelectedText` (i `eod-scale.js`) mają tylko testy tekstu źródła…” — nowa historia techniczna „testy E2E w przeglądarce (Playwright)”, propozycja po epiku 3, gdy powstanie Widok miesiąca; wymaga nowego narzędzia.

- source_plan: `tickets/epic-wzorzec-ui/tickets/story-refactor-sweep-plan.md`
  summary: Reguła przejściowa `.eod-content:not(:has(> .eod-page))` w `app.css` zostaje, bo daje odstęp strony `/Error` i „Sprawdzanie uprawnień...” w `EodShell` bez `EodPage`.
  evidence: Przeniesione: drobna historia w epiku 3 — przeniesienie `/Error` i „Sprawdzanie uprawnień...” na `EodPage` (zmienia wygląd), potem usunięcie reguły.
- source_plan: `tickets/epic-wzorzec-ui/tickets/story-refactor-sweep-plan.md`
  summary: Pasek komunikatu pojawia się przed ponownym wczytaniem Listy wpisów (przegląd 7.5 #3, odrzucone w 7.5).
  evidence: Przeniesione do rozważenia w historii Widoku miesiąca (epik 3), bo poprawka przebudowuje `EntryDialogs` i zmienia zachowanie.
- source_plan: `tickets/epic-wzorzec-ui/tickets/story-refactor-sweep-plan.md`
  summary: Po usunięciu `FluentLocalizer` (7.7) żaden test nie pilnuje, że każdy klucz Fluent UI ma polski tekst; `PolishLocalizer` nie ma m.in. `FluentInputBase_RequiredMessage`, `PresenceStatus_*`.
  evidence: Przeniesione: nowa historia w epiku 3 — uzupełnienie kluczy `PolishLocalizer` z testem pokrycia zasobu `LanguageResource.resources` (decyzja Autora 03.11.2026).
- source_plan: `tickets/epic-wzorzec-ui/tickets/story-refactor-sweep-plan.md`
  summary: Kontrola ręczna z 7.5 (Sesja 10 Wpisów ≤ 10 s, konsola bez naruszeń CSP) niewykonana.
  evidence: Nie kod — kontrola ręczna Autora przed PR epiku 7 (`dotnet run`: okna Projektu i Wpisu bez stopki biblioteki, Enter nie potwierdza usunięcia, Sesja 10 Wpisów ≤ 10 s, konsola bez naruszeń CSP).
- source_plan: `plan-7-8-dopracowanie-wpisow-po-tescie.md`
  summary: Eksport „Eksportuj” i paginacja po 30 wierszy na listach Wpisów wydzielone z planu 7.8 do planu `plan-7-9-eksport-i-paginacja-wpisow.md`.
  evidence: Decyzja Autora 2026-11-04 (podział): zmiany w kopii `EodReport` (AD-23 poz. 11–13) i odejście od R17 to inne ryzyko niż poprawki układu z 7.8; 7.9 zależy od `EntriesReport` z 7.8.
- source_plan: `plan-7-9-eksport-i-paginacja-wpisow.md`
  summary: Ekran „Eksport” z pozycją w menu (epic-miesiac-i-zestawienie, R12/FR-12, NFR-6) odpada, a R17 „brak eksportu CSV” wymaga przepisania (initiative R17 i decyzja 02.11.2026, PRD FR-16 i „Poza zakresem”, epic-wzorzec-ui R1 i Done when).
  evidence: Decyzja Autora 2026-11-04: eksport tylko przyciskiem „Eksportuj” na Liście wpisów i liście Nowego wpisu, Zestawienie xlsx zostaje w Widoku miesiąca; tickety i PRD do aktualizacji przez PO.

## Rozstrzygnięte (3.4, Widok miesiąca, 05.11.2026)

- „Pasek komunikatu pojawia się przed ponownym wczytaniem Listy wpisów (przegląd 7.5 #3)…” — rozstrzygnięte w 3.4: `EntryDialogs` przyjmuje opcjonalny `afterChange` (ponowne wczytanie danych ekranu) i pokazuje pasek dopiero po nim; tak robią Lista wpisów, Nowy wpis i Widok miesiąca (AD-10).
- source_plan: `tickets/epic-miesiac-i-zestawienie/tickets/story-strona-bedu-i-sprawdzanie-uprawnien-na-eodpage-plan.md`
  summary: Po wejściu na stronę wymagającą logowania fokus nie trafia w H1, bo `FocusOnNavigate Selector="h1"` (Routes.razor) próbuje raz, przy treści „Sprawdzanie uprawnień...” bez H1.
  evidence: Przegląd 3.7; zachowanie sprzed zmiany (stary `<p>` też bez H1). Dotyka dostępności nawigacji klawiaturą; poprawka np. ponowny fokus po zakończeniu autoryzacji - osobna historia.
