---
title: 'CI na PR z kontrolą migracji i ochroną gałęzi main'
type: 'chore'
ticket: '1'
created: '2026-10-31'
status: 'ready-for-dev'
baseline_revision: ''
route: 'full'
route_source: 'auto'
risk: 'low'
review: ''
review_source: ''
lenses_ran: []
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Zmiany trafiają do `main` bez automatycznego sprawdzenia. Nic nie pilnuje też zasady „po pierwszym wdrożeniu tylko migracje dodające” (AD-21), od której zależy bezpieczny rollback (R28, R30).

**Approach:** Workflow GitHub Actions na każdy PR do `main`: build, wszystkie testy na Testcontainers, `has-pending-model-changes` i kontrola migracji dodających jako test xUnit (działa też lokalnie). Do tego przypięte SDK i pierwsza strona `docs/admin/` z instrukcją ochrony `main`.

## Boundaries & Constraints

**Always:**
- `.github/workflows/pr.yml`, uruchamiany przez `pull_request` do `main`. Jeden job o stałej nazwie `build-and-test`, bo na tę nazwę wskazuje ochrona `main`.
- Runner `ubuntu-latest`. Kroki: `actions/checkout@v7`, `actions/setup-dotnet@v6` z `global-json-file: global.json`, `dotnet tool restore`, `dotnet build`, `dotnet test`, `dotnet ef migrations has-pending-model-changes --project src/RejestrCzasu`.
- `DOTNET_CLI_TELEMETRY_OPTOUT=1` na poziomie workflow. `permissions: contents: read`.
- Bez sekretów i bez wypisywania zmiennych środowiska. Testy biorą hasła tylko z Testcontainers.
- Kontrola migracji jako test w projekcie testów, przez `Migration.UpOperations` każdej migracji z assembly aplikacji z wyjątkiem migracji o nazwie `Initial`. Odrzuca:
  - `DropTableOperation`, `DropColumnOperation`, `RenameTableOperation`, `RenameColumnOperation`;
  - `AlterColumnOperation`;
  - `CreateIndexOperation` z `IsUnique`;
  - `SqlOperation`.

  Komunikat wskazuje migrację i operację.
- `global.json`: `version` 10.0.403, `rollForward` bez zmian.
- `docs/admin/README.md`, po polsku: spis tematów instrukcji administratora (na razie jeden) oraz krok po kroku ustawienie reguły ochrony `main` w interfejsie GitHuba (wymagany job `build-and-test`, wymagana aktualność gałęzi, zakaz bezpośredniego push).

**Never:**
- Bez wdrożenia, GHCR i SSH (historia 4.8).
- Bez zmian w kodzie aplikacji i w migracjach.
- Bez skryptu kontroli migracji poza testami.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Tylko `Initial` | migracje z tego repozytorium | test kontroli przechodzi | No error expected |
| Migracja dodająca | migracja testowa z `AddColumn` dopuszczającym `NULL` i `CreateIndex` nieunikalnym | dozwolona | No error expected |
| Migracja niszcząca | migracja testowa z `DropColumn`, `RenameColumn`, `AlterColumn`, unikalnym `CreateIndex` albo `Sql` | odrzucona | asercja z nazwą migracji i operacji |

</frozen-after-approval>

## Code Map

- `global.json` -- dziś `"version": "10.0.401"`, `rollForward: latestPatch`, `test.runner: Microsoft.Testing.Platform`. Zmienić tylko wersję.
- `.config/dotnet-tools.json` -- `dotnet-ef` 10.0.14, odtwarzany przez `dotnet tool restore`.
- `src/RejestrCzasu/Data/Migrations/` -- na `main` jest migracja `Initial` z historii 1.1. Po scaleniu 4.2 będzie nowa, też o nazwie `Initial`, dlatego kontrola rozpoznaje ją po nazwie klasy, a nie po identyfikatorze.
- `tests/RejestrCzasu.Tests/` -- xUnit v3 na Microsoft.Testing.Platform. `PostgresFixture` (Testcontainers `postgres:18`) i `ApplicationTests` (uruchamia `dotnet` jako proces) działają na `ubuntu-latest`, bo Docker jest tam preinstalowany.
- `.gitignore` -- `docs/*.xlsx` i `projekt-testowy-material.md` są ignorowane. Nowy `docs/admin/` jest śledzony.
- `.github/` -- jeszcze nie istnieje.

## Tasks & Acceptance

**Execution:**
- [ ] `global.json` -- `version` 10.0.403 -- AD-1.
- [ ] `.github/workflows/pr.yml` -- workflow z Boundaries -- R28, AD-20.
- [ ] `tests/RejestrCzasu.Tests/MigrationRulesTests.cs` -- kontrola migracji dodających na prawdziwych migracjach i testy z I/O Matrix (wiersze 2 i 3) na migracjach testowych zdefiniowanych w samym teście, poza assembly aplikacji -- AD-21.
- [ ] `docs/admin/README.md` -- spis i instrukcja ochrony `main` -- R27, AD-20.

**Acceptance Criteria:**
- Given lokalne drzewo, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone, w tym kontrola migracji.
- Given wypchnięta gałąź i PR do `main`, when workflow się wykona, then job `build-and-test` jest zielony, a log nie zawiera haseł. Sprawdza Autor na GitHubie.
- Given ochrona `main` ustawiona według `docs/admin/README.md`, when PR ma czerwony test, then GitHub nie pozwala go scalić. Sprawdza Autor, krok człowieka.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone
- `dotnet ef migrations has-pending-model-changes --project src/RejestrCzasu` -- expected: brak zmian

**Manual checks (if no CLI):**
- Na GitHubie: job `build-and-test` zielony na PR tej gałęzi. Po ustawieniu ochrony `main` przycisk scalenia zablokowany dla PR z czerwonym testem.

## Review Triage Log
