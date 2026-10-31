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
