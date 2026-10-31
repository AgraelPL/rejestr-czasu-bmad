# Walkthrough: 4.1 CI na PR z kontrolą migracji i ochroną gałęzi main

Target: commit `9f570e4753d85783da14e49e85c3f7ede0f48507` na gałęzi `4-1-ci-na-pr` (względem `6c510584869a68e5a051806d5be9728d0c19bfca`).

Bieżący blok: — (przegląd zakończony)

## - [x] 1. Intencja — zaakceptowany

Źródło: sekcja Intent z [planu historii](../tickets/epic-konta-i-wdrozenie/tickets/story-ci-na-pr-z-kontrola-migracji-i-ochrona-gaezi-main-plan.md), wklejona dosłownie.

> **Problem:** Zmiany trafiają do `main` bez automatycznego sprawdzenia. Nic nie pilnuje też zasady „po pierwszym wdrożeniu tylko migracje dodające” (AD-21), od której zależy bezpieczny rollback (R28, R30).
>
> **Approach:** Workflow GitHub Actions na każdy PR do `main`: build, wszystkie testy na Testcontainers, `has-pending-model-changes` i kontrola migracji dodających jako test xUnit (działa też lokalnie). Do tego przypięte SDK i pierwsza strona `docs/admin/` z instrukcją ochrony `main`.

## - [x] 2. W skrócie — zaakceptowany

Zmiana dodaje bramkę przed `main`: workflow uruchamia build i testy, jeden z testów pilnuje AD-21, a ręcznie ustawiona reguła GitHuba blokuje scalenie bez zielonego joba.

1. [pr.yml](../../../.github/workflows/pr.yml) — workflow z jobem `build-and-test`.
2. [MigrationRulesTests.cs:65](../../../tests/RejestrCzasu.Tests/MigrationRulesTests.cs#L65) — `FindViolations`, czyli reguła AD-21 jako switch po operacjach migracji.
3. [MigrationRulesTests.cs:15](../../../tests/RejestrCzasu.Tests/MigrationRulesTests.cs#L15) — test, który stosuje regułę do prawdziwych migracji aplikacji.
4. [docs/admin/README.md](../../../docs/admin/README.md) — ręczne ustawienie ochrony `main`.

## - [x] 3. Workflow PR — zaakceptowany

Jeden job `build-and-test` na `ubuntu-latest`, uruchamiany przez `pull_request` do `main`. Nazwa joba jest kontraktem z regułą ochrony, więc nie wolno jej zmieniać bez zmiany reguły. Workflow ma tylko `contents: read`, wyłączoną telemetrię i nie używa sekretów; baza i hasło w testach pochodzą z Testcontainers.

- [pr.yml:7-12](../../../.github/workflows/pr.yml#L7) — uprawnienia i zmienne środowiska.
- [pr.yml:16](../../../.github/workflows/pr.yml#L16) — nazwa joba.
- [pr.yml:19-36](../../../.github/workflows/pr.yml#L19) — kroki: checkout, SDK z `global.json`, `dotnet tool restore`, build, `dotnet test --no-build`, `has-pending-model-changes`.

## - [x] 4. Kontrola migracji dodających — zaakceptowany

Zmieniony w trakcie przeglądu: przejście z listy zakazów na listę dozwolonych.

Reguła AD-21 jest testem xUnit, więc działa tak samo lokalnie i w CI. Test bierze wszystkie klasy `Migration` z assembly aplikacji, wymaga dokładnie jednej klasy `Initial`, pomija ją i przegląda `UpOperations` pozostałych. `FindViolations` to lista dozwolonych operacji: `CreateTable`, `EnsureSchema`, `CreateSequence`, `AddColumn` z `NULL` lub z wartością domyślną, nieunikalny `CreateIndex`; unikalny indeks, ograniczenie unikalne, klucz główny i klucz obcy tylko na tabeli utworzonej w tej samej migracji. Każda inna operacja to naruszenie z nazwą migracji, operacji i tabeli.

- [MigrationRulesTests.cs:15-29](../../../tests/RejestrCzasu.Tests/MigrationRulesTests.cs#L15) — test na prawdziwych migracjach.
- [MigrationRulesTests.cs:60-93](../../../tests/RejestrCzasu.Tests/MigrationRulesTests.cs#L60) — `FindViolations`.
- [MigrationRulesTests.cs:31-56](../../../tests/RejestrCzasu.Tests/MigrationRulesTests.cs#L31) — test migracji dodającej i teoria z migracjami niszczącymi.
- [MigrationRulesTests.cs:95](../../../tests/RejestrCzasu.Tests/MigrationRulesTests.cs#L95) — migracje testowe jako prywatne klasy bez `[DbContext]` i `[Migration]`, więc EF ich nie widzi.

## - [x] 5. Instrukcja ochrony main — zaakceptowany

Zmieniony w trakcie przeglądu: reguła nieegzekwowana na prywatnym repo Free, instrukcja opisuje zasadę ręczną.

Reguły ochrony nie da się ustawić z repozytorium, więc jest instrukcja krok po kroku dla klasycznej reguły: wymagany PR bez wymaganych zatwierdzeń, wymagany job `build-and-test`, aktualność gałęzi, bez obejścia, bez force push i usuwania.

- [docs/admin/README.md:16](../../../docs/admin/README.md#L16) — kiedy ustawić regułę (po pierwszym uruchomieniu joba).
- [docs/admin/README.md:21](../../../docs/admin/README.md#L21) — odznaczenie **Require approvals**.
- [docs/admin/README.md:24](../../../docs/admin/README.md#L24) — wymagany job i aktualność gałęzi.

## - [x] 6. Peryferia — zaakceptowany

- [global.json](../../../global.json) — SDK 10.0.403.
- [README.md](../../../README.md) — wymagana wersja SDK w opisie zgodna z `global.json`.
- [plan historii](../tickets/epic-konta-i-wdrozenie/tickets/story-ci-na-pr-z-kontrola-migracji-i-ochrona-gaezi-main-plan.md) — status `built` i Review Triage Log.
- [deferred-work.md](../deferred-work.md) — ryzyko dla 4.8 (CI przy wdrożeniu) i przypięcie `Initial`.
