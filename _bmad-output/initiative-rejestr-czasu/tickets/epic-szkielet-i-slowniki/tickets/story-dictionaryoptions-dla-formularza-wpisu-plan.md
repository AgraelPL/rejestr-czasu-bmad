---
title: 'DictionaryOptions dla formularza Wpisu'
type: 'feature'
ticket: '9'
created: '2026-11-01'
status: done
baseline_revision: '872b6ae2c4c8bb9ced1ae689bc415bce454c5c9d'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Formularz Wpisu (epik 2) potrzebuje jednego źródła opcji Projektu i Typu pracy. Nieaktywne pozycje mają zniknąć z nowego Wpisu i z kopii, ale zostać przy edycji Wpisu, który ich używa (R2, AD-10).

**Approach:** `Data/DictionaryOptions` w trybie Usera (`UserDbContextFactory`):
- aktywne pozycje zalogowanego Usera plus opcjonalna bieżąca wartość edytowanego Wpisu;
- sortowanie w C# według `pl-PL`: Projekty po kodzie, Typy pracy po nazwie.

## Boundaries & Constraints

**Always:**
- **Metody** `ProjectsAsync(int? currentProjectId, CancellationToken)` i `WorkTypesAsync(int? currentWorkTypeId, CancellationToken)`:
  - zwracają `IReadOnlyList` rekordów opcji: `ProjectOption(Id, Code, Name, IsActive)` i `WorkTypeOption(Id, Name, IsActive)`;
  - `null` oznacza nowy Wpis albo kopię (bez bieżącej wartości, R7);
  - id spoza pozycji Usera daje tylko aktywne pozycje, bez wyjątku, bo filtr Usera (AD-16) go nie znajdzie.
- **Kontekst:** jeden na wywołanie (AD-4), wyłącznie z `UserDbContextFactory.CreateDbContextAsync()`; zakaz `IgnoreQueryFilters` i filtrowania po `UserId` ręcznie (AD-16).
- **Sortowanie:** `StringComparer.Create(pl-PL, ignoreCase: true)`, remis rozstrzyga `Id`. Wynik jest deterministyczny i nie zależy od kultury maszyny ani bazy.
- **Rejestracja:** scoped, w `DatabaseServiceCollectionExtensions.AddAppDatabase` obok `UserDbContextFactory`.

**Never:**
- Formularz Wpisu, komponenty list wyboru i ekrany Słowników (epik 2, historie 1.7 i 1.8).
- Filtrowanie po aktywności w raportach (AD-10: Widok, Lista i eksport nigdy nie filtrują po aktywności).
- Nowa migracja.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Nowy Wpis | User ma Projekt aktywny A i nieaktywny B, `currentProjectId = null` | tylko A | — |
| Edycja Wpisu z nieaktywnym | `currentProjectId = B.Id` | A i B; B z `IsActive = false` | — |
| Edycja z aktywnym | `currentProjectId = A.Id` | A raz (bez duplikatu) | — |
| Cudza pozycja | `currentProjectId` = id Projektu innego Usera | tylko aktywne pozycje bieżącego Usera | — |
| Inny User | drugi User ma aktywne Projekty | nigdy nie pojawiają się w opcjach | — |
| Sortowanie Projektów | kody `MIELEC`, `ŁÓDŹ`, `LUBLIN` | `LUBLIN`, `ŁÓDŹ`, `MIELEC` | — |
| Sortowanie Typów pracy | nazwy „Mielec”, „łódź”, „Lublin” | „Lublin”, „łódź”, „Mielec” | — |
| Typy pracy | to samo co dla Projektów (nowy, edycja z nieaktywnym, cudza pozycja) | jak wyżej | — |
| Brak Usera | wywołanie bez zalogowanego Usera | — | `NoCurrentUserException` z fabryki |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Data/Project.cs`, `WorkType.cs`: `Id`, `Code`, `Name`, `IsActive` (encje `IUserOwned`).
- `src/RejestrCzasu/Data/UserDbContextFactory.cs`:
  - publiczne `CreateDbContextAsync()` bierze Usera z `CurrentUser`;
  - `internal CreateDbContextAsync(userId)` jest tylko dla testów; `DictionaryOptions` używa wyłącznie publicznego.
- `src/RejestrCzasu/Data/DatabaseServiceCollectionExtensions.cs` L22-28: `AddAppDatabase`, miejsce rejestracji.
- **Testy:**
  - `tests/RejestrCzasu.Tests/DataIsolationTests.cs`: wzorzec Testcontainers (`postgres.ResetAsync()`, `postgres.CreateUserAsync`, `postgres.CreateUserContextAsync(userId)` do zapisu danych testowych);
  - `tests/RejestrCzasu.Tests/AccountsTests.cs` L388-397: wzorzec `UserDbContextFactory` z `CurrentUser(HttpContextAccessor z principalem NameIdentifier, new ServerAuthenticationStateProvider())`;
  - testy budują `DictionaryOptions` dla danego Usera przez jedną metodę pomocniczą w pliku testów. Gałąź historii 4.4 dodaje do konstruktora `CurrentUser` trzeci parametr (`IDbContextFactory<AppDbContext>`), więc po jej scaleniu poprawka dotknie jednego miejsca.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Data/DictionaryOptions.cs` (z rekordami opcji), `DatabaseServiceCollectionExtensions.cs` -- źródło opcji -- AD-10, AD-16, AD-4.
- [x] `tests/RejestrCzasu.Tests/DictionaryOptionsTests.cs` -- Testcontainers, każdy wiersz I/O Matrix.

**Acceptance Criteria:**
- Given wszystkie zmiany, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone (także `DataIsolationGuardTests`).

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone

Uruchomiona przez Autora aplikacja może blokować `src/RejestrCzasu/bin`. Wtedy build i testy uruchamiaj na kopii repozytorium w scratchpadzie i nie zatrzymuj jego procesu.

## Implementation Notes

## Plan Change Log

## Review Triage Log

| # | Lens | Finding | Verdict | Route | Evidence |
|---|------|---------|---------|-------|----------|
| 1 | quick | Testy sortowania nie odróżniają `pl-PL` od invariant/en-US ani nie sprawdzają `ignoreCase` i remisu po `Id` | low | patch | `Ł` jest osobną literą także w kolacji root, więc dane z Matrix przejdą z `InvariantCultureIgnoreCase`; w `pl-PL` `Ą` jest literą po `A` (root: różnica drugorzędna), więc np. `ĄB` vs `AC` odróżnia; dopisać przypadki zamiast zmieniać istniejące. |
| 2 | quick | `OptionsFor` ustawia statyczny `AsyncLocal` `HttpContextAccessor`; drugi wywołany helper podmienia Usera pierwszego | low | patch | `HttpContextAccessor` trzyma kontekst w statycznym `AsyncLocal`; dziś żaden test nie woła helpera dwa razy, ale helper z parametrem `userId` do tego zachęca. Poprawka: tryb obwodu (`ServerAuthenticationStateProvider` + `CurrentUserCircuitHandler`), stan na instancję. |
| 3 | quick | Rejestracja `DictionaryOptions` jako scoped nie jest testowana | low | patch | Żaden test nie rozwiązuje usługi; `AddSingleton` złapałby scoped `CurrentUser` i przeniósł Usera między obwodami. Test z `validateScopes: true` rozwiązujący usługę w scope i sprawdzający `ServiceLifetime.Scoped`. |
