---
title: 'Pierwszy Administrator, stan Konta w sesji i zmiana hasła'
type: 'feature'
ticket: '4'
created: '2026-10-31'
status: 'built'
baseline_revision: '4c807aa99c42715c6f098a108b73538d1bea2043'
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

**Problem:** Po 4.3 pierwsze Konto trzeba aktywować ręcznym SQL-em, a zalogowany User zachowuje dostęp przez 14 dni nawet po zablokowaniu Konta. Nie da się też zmienić hasła, więc hasło ustawione przez Administratora (epik 5) nie może wymusić zmiany.

**Approach:** Krok 3 startu z AD-21 tworzy pierwszego Administratora z konfiguracji; stan Konta jest sprawdzany na każdym żądaniu HTTP, w obwodzie przy każdym kontekście Usera i co minutę (AD-17); statyczna `/konto/zmiana-hasla` w dwóch trybach z przekierowaniem przy `must_change_password` (AD-15).

## Boundaries & Constraints

**Always:**
- Start (AD-21, krok 3 po migracjach, w `Startup/`, tryb systemowy): brak Konta z rolą `Administrator` → Konto `active` z rolami `Administrator` i `User`, bez `account_settings`, z `FirstAdmin:UserName` i `FirstAdmin:Password` (`IOptions<T>`, AD-13); Administrator jest → konfiguracja ignorowana; brak Administratora i brak konfiguracji albo hasło odrzucone przez Identity → koniec startu z czytelnym polskim komunikatem (jak `DatabaseStartupException`).
- `CookieAuthenticationEvents.OnValidatePrincipal` na każdym żądaniu: czyta `status`, `must_change_password` i security stamp z bazy; Konto nieaktywne, usunięte albo stamp niezgodny → `RejectPrincipal` i wylogowanie ciasteczka. To jedyne miejsce kasujące ciasteczko poza `/konto/wyloguj`.
- `CurrentUser` w obwodzie przy każdym `UserDbContextFactory.CreateDbContextAsync()` czyta `status` i `must_change_password` z bazy i rzuca `AccountInactiveException` (ze stanem) albo `PasswordChangeRequiredException`. Obsługę w `MainLayout` robi historia 1.3.
- `RevalidatingServerAuthenticationStateProvider` co 1 minutę sprawdza security stamp i `status`.
- `/konto/zmiana-hasla` statyczna (`[ExcludeFromInteractiveRouting]`), dla zalogowanego: tryb wymuszony (`must_change_password`: nowe hasło dwa razy) i dobrowolny (obecne + nowe dwa razy); po sukcesie `must_change_password = false`, nowy security stamp, `RefreshSignInAsync` — bieżąca sesja trwa, inne sesje tego Usera kończą się.
- Middleware po uwierzytelnieniu: zalogowany z `must_change_password` → przekierowanie każdej strony i endpointu na `/konto/zmiana-hasla`, poza endpointami rozpoznanymi po metadanych: zasoby `MapStaticAssets`, `/_framework`, `/_blazor`, `/konto/zmiana-hasla`, `/konto/wyloguj` oraz endpointy oznaczone jawnym znacznikiem metadanych (dla `/health` z 4.5 i anonimowych tematów pomocy).
- `/konto/logowanie?powod=pending|blocked` pokazuje polski komunikat o stanie Konta (cel `NavigateTo` z AD-17).
- `FirstAdmin` w `appsettings.Local.example.json` z przykładowymi wartościami; `appsettings.json` bez nich (AD-13); README: lokalny start wymaga `FirstAdmin`.

**Never:**
- Obsługa wyjątków stanu Konta w `MainLayout` (1.3), ekrany Administratora i ustawianie hasła przez Administratora (epik 5), `/health` i `UseForwardedHeaders` (4.5).
- Lockout Identity do blokowania przez Administratora; zmiana hasła w obwodzie Blazora; drugi mechanizm kasowania ciasteczka.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Pierwszy start | brak Administratora, `FirstAdmin` ustawione | Konto `active`, role `Administrator` i `User` | No error expected |
| Kolejny start | Administrator jest, inna `FirstAdmin` | nic się nie zmienia | — |
| Brak konfiguracji | brak Administratora i `FirstAdmin` | start kończy się kodem 1 | polski komunikat |
| Za krótkie hasło | `FirstAdmin:Password` 5 znaków | start kończy się kodem 1, Konto nie powstaje | polski komunikat z błędem Identity |
| Blokada w trakcie sesji | zalogowany, `status=blocked` | następne żądanie HTTP → logowanie, ciasteczko skasowane | — |
| Obwód | zalogowany w obwodzie, `status=blocked` / `must_change_password` | `CreateDbContextAsync()` rzuca odpowiedni wyjątek | — |
| Wymuszona zmiana | `must_change_password=true` | każda strona → `/konto/zmiana-hasla`; zasoby statyczne i wylogowanie działają | — |
| Zmiana wymuszona | nowe hasło ×2 | flaga zdjęta, User zalogowany, strona główna dostępna | błędy Identity po polsku |
| Zmiana dobrowolna | złe obecne hasło | odrzucona | polski komunikat |
| Zmiana dobrowolna | poprawne obecne + nowe ×2 | hasło zmienione, User nadal zalogowany; stara sesja w innym kliencie wylogowana | — |
| Inny User | zmiana stampu Usera B | sesja Usera A trwa | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Startup/DatabaseStartup.cs` L24-31 (`RunAsync`: krok 1 L33-50, krok 2 L52-64), `DatabaseStartupException(string, Exception)` L68-69 (konstruktor wymaga `inner`); komentarz L4-6 zapowiada krok 3. `Program.cs` L77-85 łapie wyjątek, pisze na stderr, zwraca 1.
- `src/RejestrCzasu/Options/DatabaseOptions.cs` -- wzorzec opcji (`SectionName`, `Bind` w `Program.cs` L23). Źródła konfiguracji L18-21.
- `src/RejestrCzasu/Accounts/IdentitySetup.cs` L20-73 -- `ConfigureApplicationCookie` L56-62 bez `Events`; domyślny `AuthenticationStateProvider` z `AddInteractiveServerComponents`.
- `src/RejestrCzasu/Accounts/CurrentUser.cs` -- `FindUserIdAsync`/`GetUserIdAsync`, `InCircuit`, `NoCurrentUserException`; `Data/UserDbContextFactory.cs` woła `GetUserIdAsync`.
- `src/RejestrCzasu/Accounts/Login.razor` -- wzorzec strony statycznej (atrybuty L1-5, `AccountHelp`, `AccountMessages`, `EditForm` z `FormName`, model formularza, `LocalUrl`); `AccountSignIn.cs` -- wzorzec usługi stron `/konto`; `AppSignInManager.CanSignInAsync` wymaga `Active`.
- `src/RejestrCzasu/Program.cs` L63-74 -- `UseRouting`, `UseAuthentication`, `UseAuthorization`, `UseRateLimiter`, `UseAntiforgery`, mapowania; middleware przekierowania po `UseAuthorization`.
- `src/RejestrCzasu/Data/AppUser.cs` -- `Status`, `MustChangePassword`; stamp nadaje `UserManager.CreateAsync`; role w `Data/AppRoles.cs`.
- `tests/RejestrCzasu.Tests/AppClient.cs` -- klient per test (`CreateAccountAsync(userName, password, status)`, `LoginAsync`, `SubmitFormAsync`); `DatabaseStartupTests.cs` L67-80 buduje `DatabaseStartup` ręcznie; brak sztucznego `TimeProvider` (interwał 1 min testować wywołując walidację bezpośrednio).

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Options/FirstAdminOptions.cs`, `Startup/FirstAdministrator.cs`, `Startup/DatabaseStartup.cs`, `appsettings.Local.example.json`, `README.md` -- krok 3 startu -- AD-21, AD-13.
- [x] `src/RejestrCzasu/Accounts/` -- walidacja ciasteczka (`OnValidatePrincipal`), `AccountRevalidatingAuthenticationStateProvider`, wyjątki stanu w `CurrentUser`, `ChangePassword.razor` z usługą, middleware przekierowania z metadanymi wyjątków, komunikat `powod` w `Login.razor` -- AD-15, AD-17.
- [x] `tests/RejestrCzasu.Tests/` -- każdy wiersz I/O Matrix; start przez proces lub `DatabaseStartup` jak w `DatabaseStartupTests`.

**Acceptance Criteria:**
- Given wszystkie zmiany, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone.
- Given pusty wolumen i `FirstAdmin` w `appsettings.Local.json`, when `dotnet run`, then Autor loguje się jako Administrator bez SQL, a drugi start nic nie zmienia.
- Given dobrowolna zmiana hasła w przeglądarce, when minie ponad minuta (rewalidacja obwodu), then User w tej karcie jest nadal zalogowany (ticket: „User zalogowany po 2 minutach”); test sprawdza, że rewalidacja przyjmuje nowy stamp po `RefreshSignInAsync`.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone
- `dotnet ef migrations has-pending-model-changes --project src/RejestrCzasu` -- expected: brak zmian

## Implementation Notes

- Krok 3: `Startup/FirstAdministrator` (scoped) wywoływany przez `DatabaseStartup` we własnym scope; rejestracja `AddStartupSequence()` wspólna dla `Program` i testów. `DatabaseStartupException` dostał konstruktor bez `inner`.
- Stan Konta: `Accounts/AccountState` (odczyt i reguła sesji: `active` + zgodny stamp), wspólny dla `AccountCookieValidator` (zastępuje `SecurityStampValidator` Identity) i `AccountRevalidatingAuthenticationStateProvider`. Walidator zapisuje stan w `HttpContext.Items`, middleware go czyta (bez drugiego zapytania).
- `CurrentUser.GetActiveUserIdAsync` sprawdza stan tylko w obwodzie (w HTTP robi to walidator ciasteczka); `AccountInactiveException.LoginUrl` daje cel `NavigateTo` dla 1.3.
- Middleware `PasswordChangeRedirect`: wyjątki po `StaticAssetDescriptor`, znaczniku `[AllowedDuringPasswordChange]` / `.AllowDuringPasswordChange()` oraz wzorcu trasy dopasowanego endpointu `/_blazor*` i `/_framework/` (endpointy frameworka nie przyjmują własnych metadanych).
- Zmiana wymuszona bez starego hasła: walidatory haseł Identity, potem hash + flaga + nowy stamp jednym `UpdateSecurityStampAsync`. Dobrowolna: `ChangePasswordAsync` (błąd `PasswordMismatch` jako „Obecne hasło jest nieprawidłowe.”), po sukcesie PRG na `?zmieniono=1`.
- Testy: `PostgresFixture.ConfigurationOverrides` ma fikcyjny `FirstAdmin`, więc każda aplikacja w testach zakłada Administratora; liczniki Kont w `AccountsTests` pomijają go.

## Plan Change Log

## Review Triage Log

| # | Lens | Finding | Verdict | Route | Evidence |
|---|------|---------|---------|-------|----------|
| 1 | quick | Inne karty tej samej przeglądarki po dobrowolnej zmianie hasła lądują na formularzu logowania mimo ważnego ciasteczka | medium | patch | Obwód trzyma principal ze startu (stary stamp); rewalidacja po ≤1 min daje anonima, `Routes.razor` → `RedirectToLogin` (forceLoad), a `Login.razor` nie przekierowuje zalogowanego. Poprawka: zalogowany na GET `/konto/logowanie` → `LocalUrl.OrHome(ReturnUrl)`. |
| 2 | quick | `/Error` bez `[AllowedDuringPasswordChange]` — błąd przy wymuszonej zmianie staje się 302, możliwa pętla | medium | patch | `Program.cs` L59 `UseExceptionHandler("/Error")` ponownie przechodzi przez `UsePasswordChangeRedirect`; `Error.razor` ma tylko `[AllowAnonymous]`. |
| 3 | quick | Obecne hasło na `/konto/zmiana-hasla` można zgadywać bez limitu | medium | patch | `ChangePasswordAsync` nie liczy nieudanych prób; strona nie ma `[EnableRateLimiting(AccountRateLimit.PolicyName)]` w przeciwieństwie do `Login.razor` L5 i `Register.razor` L5. |
