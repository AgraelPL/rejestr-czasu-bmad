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
