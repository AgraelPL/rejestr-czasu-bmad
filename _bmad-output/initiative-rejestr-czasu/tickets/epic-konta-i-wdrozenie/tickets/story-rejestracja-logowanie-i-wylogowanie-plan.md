---
title: 'Rejestracja, logowanie i wylogowanie'
type: 'feature'
ticket: '3'
created: '2026-10-31'
status: 'built'
baseline_revision: '8f63f2c93d2a0afb82c843e3a389814b90594200'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Schemat z Kontami istnieje (4.2), ale nikt nie może założyć Konta, zalogować się ani wylogować, a aplikacja jest otwarta bez logowania. Bez tego nie ma `CurrentUser`, więc ekrany nie mogą użyć trybu Usera (AD-16).

**Approach:** ASP.NET Core Identity na istniejącym `AppDbContext`, statyczne strony `/konto/...` w `Accounts/AccountLayout`, autoryzacja całej aplikacji jednym mechanizmem i `Accounts/CurrentUser` zasilający `UserDbContextFactory` — wszystko według AD-15, AD-16, AD-6, AD-11, AD-12.

## Boundaries & Constraints

**Always:**
- Konfiguracja Identity dokładnie z AD-15: login unikalny bez względu na wielkość liter, bez e-maila (`IEmailSender<AppUser>` bez wysyłki), hasło min. 6 znaków bez klas, lockout 5 prób / 15 min (`lockoutOnFailure: true`), ciasteczko 14 dni z przedłużaniem, `LoginPath`, `AccessDeniedPath`.
- Strony `/konto/logowanie`, `/konto/rejestracja`, `/konto/brak-dostepu` statyczne (`[ExcludeFromInteractiveRouting]`); `App.razor` wybiera tryb przez `HttpContext.AcceptsInteractiveRouting()`. `/konto/wyloguj` tylko POST z antyforgery; GET nie wylogowuje.
- Rejestracja: Konto `pending` z rolą `User`, bez wiersza `account_settings`; formularz ma zdanie, że Administrator ma przez bazę dostęp do wszystkich danych.
- Logowanie: najpierw hasło, potem stan (`CanSignInAsync`); przy złym haśle zawsze „Nieprawidłowy login lub hasło”; stan `pending`/`blocked` ujawniany tylko przy poprawnym haśle; po sukcesie zapis `last_login_at` (UTC).
- Limit 10 POST/min/IP na rejestracji i logowaniu (`[EnableRateLimiting]`), odpowiedź limitera po polsku. Polski `IdentityErrorDescriber`, polskie teksty Fluent UI przez `IFluentLocalizer`.
- Jeden mechanizm autoryzacji: `[Authorize]` w `_Imports.razor` (strony `/konto` i `NotFound`, `Error` z `[AllowAnonymous]`), `RequireAuthorization()` na endpointach, `NotAuthorized` w `Routes`.
- `Accounts/CurrentUser`: w obwodzie `AuthenticationStateProvider`, w żądaniu HTTP `HttpContext.User`, `UserId` z `NameIdentifier`; `UserDbContextFactory` bierze z niego Usera i rzuca, gdy brak zalogowanego. Identity działa w trybie systemowym.
- CSP z `SecurityHeaders` na każdej odpowiedzi, także `/konto` i 429. Link Pomoc w `AccountLayout` do `/pomoc/<temat>` (dziś trafia na „nie znaleziono”).
- Decision (Autor, 31.10.2026): role `Administrator` i `User` są danymi schematu — `HasData` w `AppDbContext` ze stałymi `Id`, `NormalizedName` i `ConcurrencyStamp`, a `Initial` (jeszcze niezamrożona, AD-21) wygenerowana od nowa z zachowaniem ręcznego indeksu `(user_id, lower(name))`; README przypomina o `docker compose down -v`.
- Decision (Autor, 31.10.2026): znaki loginu domyślne Identity (`a-z A-Z 0-9 - . _ @ +`).

**Never:**
- Passkeys, e-mail, potwierdzanie Konta, reset hasła, zewnętrzni dostawcy logowania, ekrany zarządzania Kontem z szablonu.
- Pierwszy Administrator, sprawdzanie stanu Konta w sesji, zmiana hasła (historia 4.4); `UseForwardedHeaders` (4.5); przycisk Wyloguj w belce (1.3).
- Logowanie lub zmiana stanu przez obwód Blazora; `IgnoreQueryFilters`; ustalanie Usera po loginie.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Rejestracja | nowy login, hasło ≥ 6 | Konto `pending`, rola `User`, komunikat o oczekiwaniu na zatwierdzenie | No error expected |
| Login zajęty | login istniejący innymi wielkościami liter | odrzucone | polski komunikat Identity |
| Hasło za krótkie | 5 znaków | odrzucone | polski komunikat Identity |
| Złe hasło | dowolny stan Konta | „Nieprawidłowy login lub hasło” | licznik nieudanych prób rośnie |
| Konto Oczekuje / Zablokowane | poprawne hasło | brak logowania, komunikat o stanie | — |
| Konto Aktywne | poprawne hasło | zalogowany, `last_login_at` ustawione, przekierowanie na `returnUrl` lokalny albo `/` | — |
| 5 złych haseł | Konto Aktywne | 6. próba z poprawnym hasłem odrzucona przez 15 min | komunikat o wstrzymaniu |
| Wylogowanie | POST z tokenem / GET | POST kończy sesję; GET nie wylogowuje | brak tokenu → 400 |
| Bez logowania | GET `/` | przekierowanie na `/konto/logowanie?ReturnUrl=...` | — |
| Limit prób | 11. POST w minucie z jednego IP | 429 z polskim tekstem i CSP | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Program.cs` -- CSP inline L39-48 (zostaje pierwszym middleware); dodać Identity, uwierzytelnianie, autoryzację, `AddCascadingAuthenticationState`, rate limiter, endpoint wylogowania. `UseRateLimiter` po wyborze endpointu, tak by 4.5 mogła wstawić `UseForwardedHeaders` przed nim.
- `src/RejestrCzasu/Data/DatabaseServiceCollectionExtensions.cs` L19-25 -- `AddDbContextFactory<AppDbContext>` rejestruje też scoped `AppDbContext` (tryb systemowy) dla `AddEntityFrameworkStores`; `UserDbContextFactory` scoped.
- `src/RejestrCzasu/Data/UserDbContextFactory.cs` -- dziś `CreateDbContextAsync(int userId)`; publiczna metoda bez parametru z `CurrentUser`, wariant z `int` zostaje `internal` dla testów (`InternalsVisibleTo` w csproj L13; `PostgresFixture.CreateUserContextAsync` go używa).
- `src/RejestrCzasu/Data/AppUser.cs`, `AccountStatus.cs` -- `Status` domyślnie `Pending`, `RegisteredAt`, `LastLoginAt`. Tabela `roles` jest dziś pusta.
- `src/RejestrCzasu/Data/AppDbContext.cs` L80 (konfiguracja `roles`) -- miejsce na `HasData` ról. `src/RejestrCzasu/Data/Migrations/*_Initial*` i snapshot -- generować od nowa (`dotnet ef migrations remove`/usunięcie plików, potem `dotnet ef migrations add Initial --output-dir Data/Migrations`); obecny `Initial` ma ręczny `migrationBuilder.Sql` dla indeksu `(user_id, lower(name))` — przenieść go do nowej. `README.md` sekcja „Migracje” mówi już o `down -v`.
- `src/RejestrCzasu/Components/App.razor` L13, L17 -- dziś stałe `@rendermode="InteractiveServer"` na `HeadOutlet` i `Routes`; bez `<ImportMap />` (CSP).
- `src/RejestrCzasu/Components/Routes.razor`, `_Imports.razor`, `Layout/MainLayout.razor`, `Pages/NotFound.razor` (`/not-found`), `Pages/Error.razor` -- `RouteView` → `AuthorizeRouteView` z `NotAuthorized`.
- `src/RejestrCzasu/SecurityHeaders.cs` -- stała CSP; nie zmieniać.
- `src/RejestrCzasu/BannedSymbols.txt` -- `IgnoreQueryFilters` i surowy SQL zabronione (RS0030 to błąd builda).
- `tests/RejestrCzasu.Tests/ApplicationTests.cs` L147-157 -- wzorzec `WebApplicationFactory<Program>` z `postgres.ConfigurationOverrides` i test CSP; test `GET /` trzeba dopasować do przekierowania. `HomePageTests.cs` -- wzorzec bUnit z Fluent UI. `PostgresFixture.CreateUserAsync` wstawia Usera bez hasła.
- Pakiety są: Identity.EntityFrameworkCore 10.0.14, Fluent UI 5.0.1, bunit 2.13.3, Mvc.Testing 10.0.14.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Data/AppDbContext.cs`, `Data/Migrations/`, `README.md` -- `HasData` ról `Administrator` i `User`, `Initial` od nowa z indeksem `lower(name)`, README: role w schemacie i ponowne `down -v` -- Decision o rolach.
- [x] `src/RejestrCzasu/Accounts/` -- `IdentitySetup` (rejestracja usług z AD-15), `NoOpEmailSender`, `PolishIdentityErrorDescriber`, `CurrentUser`, `AccountLayout.razor`, strony `Login`, `Register`, `AccessDenied`, endpoint `POST /konto/wyloguj` -- AD-15, AD-16.
- [x] `src/RejestrCzasu/Program.cs`, `Data/UserDbContextFactory.cs`, `Data/DatabaseServiceCollectionExtensions.cs` -- podpięcie usług, potoku, limitera z polskim `OnRejected`, fabryki na `CurrentUser` -- AD-15, AD-16.
- [x] `src/RejestrCzasu/Components/App.razor`, `Routes.razor`, `_Imports.razor`, `Pages/NotFound.razor`, `Pages/Error.razor` -- tryb renderowania, `[Authorize]`, `NotAuthorized` -- AD-15.
- [x] `src/RejestrCzasu/Localization/FluentLocalizer.cs` -- polskie teksty Fluent UI przez `IFluentLocalizer` -- AD-6.
- [x] `tests/RejestrCzasu.Tests/AccountsTests.cs` (+ helper klienta HTTP z ciasteczkami i tokenem antyforgery) -- każdy wiersz I/O Matrix, CSP na `/konto/*` i na 429, `CurrentUser`/fabryka rzucają bez zalogowanego; `ApplicationTests` dopasowany do przekierowania.

**Acceptance Criteria:**
- Given wszystkie zmiany, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone.
- Given aplikacja lokalnie, when Autor zakłada Konto, ustawia `status='active'` w bazie i loguje się, then widzi stronę główną, a POST wylogowania wraca na logowanie.

## Design Notes

Statyczne strony nie mają interaktywności, więc formularze to `EditForm` z `FormName` i `[SupplyParameterFromForm]`; pola przez standardowe `InputText` (wysyłane natywnie), a Fluent UI tylko tam, gdzie działa bez obwodu (np. `FluentMessageBar` z komunikatem). Wylogowanie jako minimal API:

```csharp
app.MapPost("/konto/wyloguj", async (SignInManager<AppUser> s) => { await s.SignOutAsync(); return Results.LocalRedirect("/konto/logowanie"); })
   .RequireAuthorization();   // antyforgery: formularz z <AntiforgeryToken />
```

Limiter: polityka partycjonowana po IP, `NoLimiter` dla metod innych niż POST.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone
- `dotnet ef migrations has-pending-model-changes --project src/RejestrCzasu` -- expected: brak zmian

**Manual checks (if no CLI):**
- Lokalnie: rejestracja, aktywacja w bazie, logowanie, wylogowanie POST-em; nagłówek CSP w narzędziach przeglądarki na `/konto/logowanie`.

## Implementation Notes

- `[Authorize]` stoi w nowym głównym `src/RejestrCzasu/_Imports.razor` (nie w `Components/_Imports.razor`), żeby obejmował też `Accounts/` i przyszłe `Features/` — „główny `_Imports.razor` stron” z AD-15; `Components/_Imports.razor` trzyma już tylko usingi `Components`.
- Kolejność AD-15 (hasło przed stanem) w `Accounts/AppSignInManager`: `PreSignInCheck` sprawdza tylko lockout, `CheckPasswordSignInAsync` po poprawnym haśle woła `CanSignInAsync` (tylko `active`) i zwraca `NotAllowed`. Strona używa `PasswordSignInAsync(user, …, isPersistent: true, lockoutOnFailure: true)`, więc złe hasło liczy się w każdym stanie. Ciasteczko trwałe (14 dni, przedłużane).
- Logika rejestracji i logowania w `Accounts/AccountSignIn` (scope żądania, tryb systemowy); strony nie wstrzykują `UserManager`/`SignInManager`. Rejestracja w transakcji scoped `AppDbContext` (Konto + rola razem). Formularz ma pole „Powtórz hasło” (brak resetu hasła).
- `CurrentUser`: obwód rozpoznaje `CurrentUserCircuitHandler` (tworzony z obwodem, przed komponentami), poza nim `HttpContext.User`; brak Usera → `NoCurrentUserException`.
- `POST /konto/wyloguj` sprawdza token jawnie (`IAntiforgery.IsRequestValidAsync`), bo endpoint bez wiązania formularza nie jest sprawdzany przez middleware antyforgery; GET daje 405 → strona „nie znaleziono”.
- Limiter: jedna pula 10 POST/min na IP wspólna dla logowania i rejestracji; `UseRouting()` jawnie, potem `UseAuthentication`, `UseAuthorization`, `UseRateLimiter`, `UseAntiforgery`.
- Bez przycisku Wyloguj (Decyzja epiku 31.10.2026: przycisk przychodzi z 1.3); wylogowanie sprawdza test POST-em z tokenem.
- Link Pomoc: `AccountHelp` + `SectionOutlet` w `AccountLayout`; `/konto/brak-dostepu` linkuje do `/pomoc` (brak tematu w AD-22).
- Testy: `PostgresFixture.ResetAsync` nie czyści `roles` (dane schematu); `DataIsolationTests` używa zasianej roli Administrator. Blazor koduje polskie litery jako encje HTML, więc testy dekodują treść.
- Poza zakresem i nie zrobione: klucze Data Protection w bazie (AD-19), `OnValidatePrincipal`/revalidating provider (AD-17, 4.4).

## Plan Change Log

## Review Triage Log

Przebieg 1 (quick): medium 1, low 6, false 1.

| # | Lens | Lokalizacja | Znalezisko | Werdykt | Uzasadnienie | Trasa |
|---|------|-------------|------------|---------|--------------|-------|
| 1 | quick | `Accounts/AccountSignIn.cs` `LoginAsync` | wynik `UpdateAsync` dla `last_login_at` ignorowany | low | `UpdateAsync` może zwrócić ConcurrencyFailure, ale encja jest śledzona w tym samym zakresie zaraz po zalogowaniu; mało prawdopodobne, poprawka wymaga loggera. | reject |
| 2 | quick | `Accounts/Register.razor` | login > 256 znaków → `DbUpdateException` i 500 | medium | Kolumna `character varying(256)`, brak limitu w formularzu i walidatorze Identity; poprawka: limit długości w modelu formularza i `maxlength`. | patch |
| 3 | quick | `Accounts/AccountEndpoints.cs` | 400 bez treści → `UseStatusCodePagesWithReExecute` pokazuje „Nie znaleziono” | low | `Results.BadRequest()` bez ciała jest ponownie wykonywane jako `/not-found`; poprawka: polski tekst w odpowiedzi 400. | patch |
| 4 | quick | `Components/Routes.razor` | domyślne angielskie „Authorizing...” w `AuthorizeRouteView` | low | Brak `<Authorizing>`; AD-6 wymaga polskich tekstów; poprawka bezpośrednia. | patch |
| 5 | quick | `Accounts/Login.razor`, `Register.razor` | nieważny token antyforgery → angielski komunikat 400 | low | Realne przy logowaniu w innej karcie, ale rzadkie; poprawka wymaga własnej obsługi błędu antyforgery w SSR. | reject |
| 6 | quick | architektura, Consistency Conventions „Autoryzacja” | lista `[AllowAnonymous]` nie zawiera `NotFound` i `Error`, które plan zatwierdził | low | Rozjazd dokumentu z kodem; poprawka: dopisać obie strony do wiersza. | patch |
| 7 | quick | `Data/UserDbContextFactory.cs` | brak sprawdzenia Aktywnego Usera | false | Intencja wyklucza sprawdzanie stanu Konta w sesji (historia 4.4). | reject |
| 8 | main | `Components/Pages/Home.razor` | tymczasowy `LogoutForm` na stronie głównej | medium | Decision Autora w epiku (31.10.2026): „bez tymczasowego przycisku Wyloguj — historia 3 sprawdza wylogowanie POST-em i testem”; poprawka: usunąć z `Home` (komponent zostaje dla 1.3 albo usunąć). | patch |
