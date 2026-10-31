---
title: 'Nowy schemat Initial z Identity i UserId'
type: 'feature'
ticket: '2'
created: '2026-10-31'
status: 'built'
baseline_revision: '4034a504018ffeadb3792c8700b3feb1365aad1b'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Schemat z historii 1.1 nie zna Userów: brak tabel Identity, `user_id` i oddzielenia danych, a po pierwszym wdrożeniu (historia 4.8) `Initial` zostaje zamrożony i wolno już tylko dodawać (AD-21).

**Approach:** Usunąć starą migrację i wygenerować nową `Initial` z całym schematem AD-7 oraz wprowadzić warstwę oddzielenia danych z AD-16: `IUserOwned`, dwa tryby `AppDbContext` i `UserDbContextFactory` przyjmującą `UserId`. Źródło Usera (`CurrentUser`) dokłada historia 4.3.

## Boundaries & Constraints

**Always:**
- Cały schemat AD-7 w jednej migracji `Initial`. Tabele Identity mają klucze `int` i nazwy snake_case nadane jawnie: `users`, `roles`, `user_roles`, `user_claims`, `user_logins`, `user_tokens`, `role_claims`.
- Użytkownik ma kolumny `status` (`pending` | `active` | `blocked`, tekst), `registered_at` i `last_login_at` (`timestamptz`, UTC, `last_login_at` może być `NULL`) oraz `must_change_password`.
- Tabele `account_settings` (klucz `user_id`, prefiks Zestawienia) i `data_protection_keys`.
- `Project`, `WorkType`, `TimeEntry` i `AccountSettings` implementują `IUserOwned`.
- Indeksy unikalne `(user_id, code)` i `(user_id, lower(name))`, ten drugi przez SQL w `Initial`.
- Klucze alternatywne `(user_id, id)` w Słownikach i złożone klucze obce Wpisu. Każdy `user_id` ma klucz obcy do `users`. Wszystko z `RESTRICT`.
- Tryb Usera: filtr `user_id = UserId`; `SaveChanges` nadaje `user_id` nowym rekordom `IUserOwned` i odrzuca zmianę `user_id`.
- Tryb systemowy (bez `UserId`): `IUserOwned` jest niewidoczne, a ich zapis rzuca wyjątek.
- Filtr i nadawanie `user_id` działają po interfejsie, nie po liście tabel.
- Normalizacja kodu Projektu do wielkich liter zostaje.

**Never:**
- Bez stron logowania, `CurrentUser`, `UserManager`, pierwszego Administratora i zmian w UI (historie 4.3 i 4.4).
- Bez `ON DELETE CASCADE`.
- Bez `IgnoreQueryFilters()` w tej historii.
- Bez zmian w `.github/` (historia 4.1).
- Bez danych prawdziwych w testach.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Oddzielenie odczytu | Userzy A i B mają po Projekcie, Typie pracy i Wpisie | kontekst A widzi tylko swoje, B tylko swoje | No error expected |
| Ten sam kod u dwóch Userów | A i B dodają Projekt `RDZ` | oba zapisane | No error expected |
| Duplikat u jednego Usera | A dodaje `rdz` po `RDZ`; Typ `dev` po `Dev` | odrzucone | `DbUpdateException` |
| Wpis z cudzym Słownikiem | Wpis A z `ProjectId` Projektu B (wprost przez Id) | odrzucony przez bazę | `DbUpdateException` |
| Zapis w trybie systemowym | dodanie `Project` kontekstem bez `UserId` | nic nie zapisane | wyjątek przed zapisem |
| Zmiana `user_id` | edycja `UserId` istniejącego Projektu w trybie Usera | nic nie zapisane | wyjątek przed zapisem |
| Usunięcie użytego Słownika | Projekt lub Typ pracy z Wpisem | odrzucone | `DbUpdateException` |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Data/AppDbContext.cs` -- dziś `DbContext` z 3 encjami i `NormalizeProjectCodes` w obu `SaveChanges`. Ma stać się `IdentityDbContext<AppUser, IdentityRole<int>, int>` + `IDataProtectionKeyContext`; normalizację zachować.
- `src/RejestrCzasu/Data/Project.cs`, `WorkType.cs`, `TimeEntry.cs` -- encje, do których dochodzi `UserId`; stałe długości bez zmian.
- `src/RejestrCzasu/Data/Migrations/*` -- `20261030223604_Initial*` i `AppDbContextModelSnapshot.cs` do usunięcia. Obecny `Sql(...)` dla `lower(name)` jest wzorcem dla nowego indeksu.
- `src/RejestrCzasu/Options/BackupOptions.cs`, `ExportOptions.cs` -- do usunięcia razem z rejestracją w `Program.cs:24-25`. `DatabaseOptions.ContainerName` jest martwe po wycofaniu AD-3, też do usunięcia.
- `src/RejestrCzasu/Program.cs` -- `AddDbContextFactory<AppDbContext>` (zostaje dla trybu systemowego) plus rejestracja `UserDbContextFactory`.
- `src/RejestrCzasu/Startup/DatabaseStartup.cs` -- odwołania do AD-3 i krok „Kopia bazy” w komentarzach zmienić na AD-21; logika bez zmian.
- `src/RejestrCzasu/appsettings.json`, `appsettings.Local.example.json`, `README.md:29-30` -- usunąć `Urls`, `Backup`, `Export`, `ContainerName`. Port lokalny jest już w `Properties/launchSettings.json` (`127.0.0.1:5080`). `ApplicationTests.cs:56` ustawia `Urls` zmienną środowiskową, więc działa bez klucza.
- `tests/RejestrCzasu.Tests/PostgresFixture.cs` -- `CreateContext()` bez Usera i `ResetAsync` z listą 3 tabel. Do zmiany: kontekst dla `UserId`, helper tworzący użytkownika, czyszczenie wszystkich tabel poza `__EFMigrationsHistory`, usunięcie Backup/Export z nadpisań.
- `tests/RejestrCzasu.Tests/SchemaTests.cs` -- 6 testów z 1.1; przepisać na tryb Usera.
- `.config/dotnet-tools.json` -- `dotnet-ef` 10.0.14 (`dotnet tool restore`).

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/RejestrCzasu.csproj` -- dodać `Microsoft.AspNetCore.Identity.EntityFrameworkCore` i `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` w wersji 10.0.14 -- Stack.
- [x] `src/RejestrCzasu/Data/IUserOwned.cs`, `AppUser.cs`, `AccountStatus.cs`, `AccountSettings.cs` -- nowe typy; `AppUser : IdentityUser<int>` z kolumnami z Boundaries -- AD-7.
- [x] `src/RejestrCzasu/Data/Project.cs`, `WorkType.cs`, `TimeEntry.cs` -- `IUserOwned`, `UserId` -- AD-16.
- [x] `src/RejestrCzasu/Data/AppDbContext.cs` -- Identity, jawne nazwy tabel, klucze i indeksy, filtr i `SaveChanges` z dwoma trybami, `DataProtectionKeys` -- AD-7, AD-16.
- [x] `src/RejestrCzasu/Data/UserDbContextFactory.cs` -- `CreateDbContextAsync(int userId)` na `IDbContextFactory<AppDbContext>`; jedyne miejsce ustawiające tryb Usera; rejestracja w `Program.cs` -- AD-16.
- [x] `src/RejestrCzasu/Data/Migrations/` -- usunąć stare, `dotnet ef migrations add Initial`, dopisać SQL indeksu `(user_id, lower(name))` -- AD-21.
- [x] `src/RejestrCzasu/Options/*`, `Program.cs`, `Startup/DatabaseStartup.cs`, `appsettings*.json`, `README.md` -- sprzątanie z Code Map; README: lokalny wolumen zakłada się od nowa (`docker compose down -v`).
- [x] `tests/RejestrCzasu.Tests/PostgresFixture.cs`, `SchemaTests.cs`, nowy `DataIsolationTests.cs` -- testy z I/O Matrix oraz test, że każda encja modelu z `UserId` spoza typów Identity i `DataProtectionKey` implementuje `IUserOwned`.

**Acceptance Criteria:**
- Given pusty lokalny wolumen, when `docker compose up -d` i `dotnet run`, then aplikacja startuje, a baza ma tabele `users`, `roles`, `user_roles`, `user_claims`, `user_logins`, `user_tokens`, `role_claims`, `account_settings`, `data_protection_keys`, `project`, `work_type`, `time_entry` i jedną migrację w `__EFMigrationsHistory`.
- Given nowa `Initial`, when `dotnet ef migrations has-pending-model-changes`, then zmian brak.
- Given kod po zmianie, when szukać `Backup`, `ExportOptions`, `ContainerName` w `src/` i `tests/`, then brak trafień.

## Design Notes

Tryb kontekstu to właściwość, którą raz ustawia tylko `UserDbContextFactory`. Filtr czyta ją z kontekstu, więc EF parametryzuje ją na każde zapytanie:

```csharp
public int? CurrentUserId { get; private set; }
internal void BindUser(int userId) { if (CurrentUserId is not null) throw new InvalidOperationException(); CurrentUserId = userId; }
// filter per IUserOwned type, built in OnModelCreating:
// e => CurrentUserId != null && e.UserId == CurrentUserId
```

Złożony klucz obcy: `HasAlternateKey(p => new { p.UserId, p.Id })` w Słowniku i `HasForeignKey(e => new { e.UserId, e.ProjectId }).HasPrincipalKey(p => new { p.UserId, p.Id })` we Wpisie.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone, w tym nowe z I/O Matrix
- `dotnet ef migrations has-pending-model-changes --project src/RejestrCzasu` -- expected: brak zmian
- `dotnet ef migrations script --project src/RejestrCzasu` -- expected: nazwy tabel Identity i klucze złożone jak w Boundaries (notatka ticketu)

## Review Triage Log

Pass 1 (quick): high 1, medium 2, low 2, false 0, maybe-false 0.

| # | Verdict | Route | Evidence |
|---|---|---|---|
| 1 | high | patch | `ApplyUserOwnership` porównuje `UserId` tylko z change trackerem; stub z własnym `UserId` i cudzym `Id` przez `Update`/`Remove` daje `UPDATE/DELETE ... WHERE id = @p` bez `user_id` — przeciek między Userami (AD-16); typowy wzorzec formularza edycji. Poprawka: `UserId` jako token współbieżności w `ConfigureUserOwned` + test stubem. |
| 2 | medium | patch | Indeks gita: stara migracja i snapshot usunięte w indeksie, nowe pliki tylko intent-to-add — commit samego indeksu straciłby nową `Initial`. Poprawka przy commicie: `git add -A` ścieżek zmiany. |
| 3 | low | patch | Lokalny, ignorowany `appsettings.Local.json` Autora nadal ma `ContainerName` i `Backup`; README nie mówi, żeby go odświeżyć z przykładu. Poprawka: zdanie w README. |
| 4 | low | patch | `UserContext_CannotBeBoundTwice_OrToInvalidUser` nie sprawdza ponownego wiązania (`BindUser` internal, brak `InternalsVisibleTo`). Poprawka: `InternalsVisibleTo` dla testów i asercja podwójnego wiązania. |
| 5 | medium | defer | `AccountSettings` jest `IUserOwned`, a zapis `IUserOwned` w trybie systemowym rzuca; AD-7/AD-21 każą tworzyć wiersz przy rejestracji i pierwszym Administratorze w trybie systemowym. Sprzeczność w architekturze, ujawni się w 4.3; wpis w deferred-work.md. |
