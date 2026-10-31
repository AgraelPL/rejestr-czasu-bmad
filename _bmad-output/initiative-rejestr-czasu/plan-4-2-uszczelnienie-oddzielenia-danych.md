---
title: 'Uszczelnienie oddzielenia danych (uzupełnienie historii 4.2)'
type: 'feature'
ticket: ''
created: '2026-10-31'
status: 'built'
baseline_revision: '85006e04fb5535fbad88e15a9b3ab542d025c324'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-konta-i-wdrozenie/tickets/story-nowy-schemat-initial-z-identity-i-userid-plan.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/walkthrough-4-2-nowy-schemat-initial/walkthrough-4-2-nowy-schemat-initial-log.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** W przeglądzie historii 4.2 druga opinia znalazła furtki w oddzieleniu danych (AD-16) poza `SaveChanges`. Po pierwsze, `ExecuteUpdate` może zmienić `user_id`. Po drugie, `IgnoreQueryFilters`, `SqlQuery` i `ExecuteSql` omijają filtr. Po trzecie, kontekst Usera może czytać i zapisywać tabele Identity oraz klucze Data Protection. Doszły dwie pułapki na przyszłość: pula kontekstów i nowa encja bez `UserId`. `Initial` nie jest jeszcze zamrożony, więc ochronę w bazie można dodać teraz.

**Approach:** Na gałęzi 4.2, przed scaleniem PR, zamknąć furtki 1–7 z wpisu 4 dziennika przeglądu:
1. Trigger w `Initial` blokuje zmianę `user_id`.
2. Analizator zakazanych API.
3. Kontekst Usera ma dostęp tylko do encji `IUserOwned`.
4. Reguła bez puli kontekstów.
5. Ostrzejszy test modelu.

Każda furtka dostaje swój test.

## Boundaries & Constraints

**Always:**
- **Trigger (furtka 1):** jedna funkcja PL/pgSQL i triggery `BEFORE UPDATE` na `project`, `work_type`, `time_entry`, `account_settings`. Gdy `OLD.user_id IS DISTINCT FROM NEW.user_id`, trigger zgłasza błąd. Wszystko dopisane SQL-em w obecnej migracji `Initial`, z usunięciem w `Down`.
- **Zakazane API (furtki 2–3):** `Microsoft.CodeAnalysis.BannedApiAnalyzers` 5.6.0 i `BannedSymbols.txt` tylko w projekcie aplikacji. Zakaz obejmuje:
  - `IgnoreQueryFilters` (wszystkie przeciążenia);
  - `RelationalDatabaseFacadeExtensions.SqlQuery`, `SqlQueryRaw`, `ExecuteSql`, `ExecuteSqlRaw`, `ExecuteSqlInterpolated` i ich wersje async;
  - `FromSqlRaw`.

  Wyłączenie ostrzeżenia RS0030 wolno dodać tylko w `Accounts/AccountStats.cs` i `Accounts/DeleteAccount.cs` (AD-16). Pliki jeszcze nie istnieją, a test pilnuje tej listy. Ostrzeżenie RS0030 ma być błędem kompilacji.
- **Tryb Usera (furtki 4–5):** `PrepareChanges` rzuca wyjątek dla każdej dodanej, zmienionej lub usuniętej encji spoza `IUserOwned`. Wszystkie encje Identity i `DataProtectionKey` mają filtr `CurrentUserId == null`, więc w trybie Usera są niewidoczne. Tryb systemowy działa jak dotąd, także dla Identity i Data Protection.
- **Bez puli (furtka 6):** rejestracja `AppDbContext` i `UserDbContextFactory` przeniesiona do jednej metody rozszerzającej w `Data/`, wołanej z `Program.cs`. Komentarz zakazuje `AddPooledDbContextFactory`, a test sprawdza, że rejestracja nie używa puli.
- **Test modelu (furtka 7):** każda encja modelu spoza typów Identity i `DataProtectionKey` musi być `IUserOwned` albo stać na jawnej, pustej dziś liście wyjątków w teście.

**Never:**
- Bez nowej migracji. Zmienia się wyłącznie istniejąca `Initial`, a `has-pending-model-changes` zostaje bez zmian.
- Bez zmian zachowania trybu systemowego.
- Analizator nie trafia do projektu testów, bo testy używają `SqlQueryRaw` do sprawdzania schematu.
- Bez `Accounts/`, logowania i UI (historie 4.3+).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| `ExecuteUpdate` zmienia `user_id` | kontekst A: `Projects.Where(id).ExecuteUpdateAsync(SetProperty(UserId, B))` na Projekcie bez Wpisów | Projekt zostaje u A | `PostgresException` z triggera |
| Surowy UPDATE `user_id` | `UPDATE work_type SET user_id = B` z połączenia testowego | bez zmian | `PostgresException` z triggera |
| Zwykły UPDATE bez zmiany `user_id` | zmiana nazwy Projektu w trybie Usera | zapisane | No error expected |
| Odczyt Identity w trybie Usera | kontekst A: `Users`, `UserRoles`, `Roles`, `DataProtectionKeys` | puste wyniki | No error expected |
| Zapis Identity w trybie Usera | kontekst A dodaje `IdentityUserRole` albo zmienia `AppUser.Status` | nic nie zapisane | wyjątek przed zapisem |
| Identity w trybie systemowym | kontekst systemowy czyta i zapisuje `AppUser` | działa | No error expected |
| Zakazane API w aplikacji | wywołanie `IgnoreQueryFilters()` w `src/` | build nie przechodzi | błąd RS0030 |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Data/AppDbContext.cs` -- `OnModelCreating`, pętla `ConfigureUserOwned` i `PrepareChanges`/`ApplyUserOwnership`. Tu dojdą filtry Identity i DataProtection oraz odrzucanie zapisu spoza `IUserOwned` w trybie Usera.
- `src/RejestrCzasu/Data/Migrations/20261031164517_Initial.cs:339` -- istniejący `migrationBuilder.Sql` z indeksem; trigger dopisać obok, `DROP` w `Down` przy linii 345.
- `src/RejestrCzasu/Program.cs:25-30` -- `AddDbContextFactory` i `AddScoped<UserDbContextFactory>`, do przeniesienia do metody rozszerzającej.
- `src/RejestrCzasu/RejestrCzasu.csproj` -- pakiety, `InternalsVisibleTo`. Tu dodać analizator, `BannedSymbols.txt` jako `AdditionalFiles` i RS0030 jako błąd.
- `tests/RejestrCzasu.Tests/DataIsolationTests.cs` -- wzorzec testów trybu Usera i systemowego oraz `EveryEntityWithUserId_ImplementsIUserOwned` (do zaostrzenia).
- `tests/RejestrCzasu.Tests/PostgresFixture.cs:70,74,91,119` -- kontekst Usera, `CreateUserAsync`, `ResetAsync` i budowa opcji. Po wprowadzeniu metody rozszerzającej fixture może z niej korzystać.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Data/Migrations/20261031164517_Initial.cs` -- funkcja i 4 triggery w `Up`, usunięcie w `Down` -- furtka 1.
- [x] `src/RejestrCzasu/Data/AppDbContext.cs` -- filtry `CurrentUserId == null` na encjach Identity i `DataProtectionKey`; w trybie Usera wyjątek dla zmian encji spoza `IUserOwned` -- furtki 4–5.
- [x] `src/RejestrCzasu/Data/DatabaseServiceCollectionExtensions.cs` i `Program.cs` -- jedna metoda rejestrująca bazę, komentarz o zakazie puli -- furtka 6.
- [x] `src/RejestrCzasu/RejestrCzasu.csproj` i `src/RejestrCzasu/BannedSymbols.txt` -- analizator i lista symboli z komentarzem o dozwolonych plikach -- furtki 2–3.
- [x] `tests/RejestrCzasu.Tests/` -- testy z I/O Matrix (bez ostatniego wiersza, który sprawdza build), test rejestracji bez puli, test, że RS0030 jest wyłączany tylko w dozwolonych plikach (skan `src/`), zaostrzony test modelu -- furtki 1–7.

**Acceptance Criteria:**
- Given drzewo po zmianie, when tymczasowo dopisać `IgnoreQueryFilters()` w dowolnym pliku `src/` i zbudować, then build kończy się błędem RS0030, a po usunięciu przechodzi.
- Given nowa wersja `Initial`, when `dotnet ef migrations has-pending-model-changes`, then zmian brak, a w `Data/Migrations/` jest dalej jedna migracja.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone, w tym nowe z I/O Matrix
- `dotnet ef migrations has-pending-model-changes --project src/RejestrCzasu` -- expected: brak zmian

**Manual checks (if no CLI):**
- Kryterium z RS0030: tymczasowe wywołanie `IgnoreQueryFilters()` w `src/` daje błąd builda, po usunięciu wywołania build przechodzi.

## Review Triage Log

Pass 1 (quick): high 0, medium 2, low 2, false 1, maybe-false 0 (+1 low odrzucone).

| # | Verdict | Route | Evidence |
|---|---|---|---|
| 1 | low | reject | Wyłączenie RS0030 bez tekstu „RS0030” (gołe `#pragma warning disable`, `.editorconfig` dla kategorii, `RunAnalyzers=false`) — realne, ale mało prawdopodobne u agentów, a poprawka to kolejne sprawdzenia w skanie. |
| 2 | false | reject | RS0030 działa w `.razor`: plik próbny `Components/Pages/ProbeBanned.razor` z `IgnoreQueryFilters()` dał błąd RS0030 w buildzie; plik usunięty, build 0 błędów. |
| 3 | medium | patch | `BannedSymbols_CoverEveryOverload` liczy wpisy zamiast porównywać identyfikatory; zmiana sygnatury w EF albo literówka zostawi przeciążenie dozwolone przy zgodnej liczbie. Poprawka: budować identyfikator dokumentacji z `MethodInfo` i porównywać całe identyfikatory. |
| 4 | medium | patch | Lista tabel z triggerem jest wpisana na sztywno; nowa encja `IUserOwned` po zamrożeniu `Initial` nie dostanie triggera i nic tego nie zgłosi. Poprawka: test czyta `pg_trigger` dla tabeli każdej encji `IUserOwned` z modelu. |
| 5 | low | patch | `DatabaseStartupTests.CreateStartup` buduje `ServiceProvider` bez zwalniania. Poprawka: zwalniać kontener po teście. |
| 6 | low | reject | Regex rejestracji obejdzie wywołanie bez jawnego `<T>`; mało prawdopodobne, a kontrola zachowania w `AddAppDatabase_DoesNotPoolContexts` dalej działa dla rejestracji aplikacji. |
