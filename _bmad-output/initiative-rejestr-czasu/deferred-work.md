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
