# Walkthrough: 4.3 Rejestracja, logowanie i wylogowanie

Target: commit `1be1d2c` na gałęzi `4-3-rejestracja-logowanie` (względem `8f63f2c93d2a0afb82c843e3a389814b90594200`).

Bieżący blok: — (przegląd zakończony)

## - [x] 1. Intencja — zaakceptowany

Źródło: sekcja Intent z [planu historii](../tickets/epic-konta-i-wdrozenie/tickets/story-rejestracja-logowanie-i-wylogowanie-plan.md), wklejona dosłownie.

> **Problem:** Schemat z Kontami istnieje (4.2), ale nikt nie może założyć Konta, zalogować się ani wylogować, a aplikacja jest otwarta bez logowania. Bez tego nie ma `CurrentUser`, więc ekrany nie mogą użyć trybu Usera (AD-16).
>
> **Approach:** ASP.NET Core Identity na istniejącym `AppDbContext`, statyczne strony `/konto/...` w `Accounts/AccountLayout`, autoryzacja całej aplikacji jednym mechanizmem i `Accounts/CurrentUser` zasilający `UserDbContextFactory` — wszystko według AD-15, AD-16, AD-6, AD-11, AD-12.

## - [x] 2. W skrócie — zaakceptowany

Identity siedzi na istniejącym `AppDbContext` w trybie systemowym. Logowanie i rejestracja to statyczne strony, bo tylko odpowiedź HTTP ustawia ciasteczko. Reszta aplikacji wymaga zalogowania, a ekrany dostają Usera przez `CurrentUser`.

1. [IdentitySetup.cs:27](../../../src/RejestrCzasu/Accounts/IdentitySetup.cs#L27) — cała konfiguracja Identity i ciasteczka w jednym miejscu.
2. [AppSignInManager.cs:13](../../../src/RejestrCzasu/Accounts/AppSignInManager.cs#L13) — kolejność: blokada, hasło, dopiero potem stan Konta.
3. [Login.razor:1](../../../src/RejestrCzasu/Accounts/Login.razor#L1) — statyczna strona logowania z limitem prób.
4. [_Imports.razor:12](../../../src/RejestrCzasu/_Imports.razor#L12) — `[Authorize]` dla wszystkich stron.
5. [CurrentUser.cs:13](../../../src/RejestrCzasu/Accounts/CurrentUser.cs#L13) — skąd aplikacja wie, kto jest zalogowany.

## - [x] 3. Logowanie i rejestracja według AD-15 — zaakceptowany

`AppSignInManager` przestawia kolejność sprawdzeń Identity: przed hasłem sprawdza tylko blokadę, a stan Konta (`pending`/`blocked`) dopiero przy poprawnym haśle, więc zły login nie zdradza stanu i każda zła próba liczy się do blokady. `AccountSignIn` zakłada Konto `pending` z rolą `User` w jednej transakcji i po udanym logowaniu zapisuje `last_login_at`.

- [IdentitySetup.cs:27-54](../../../src/RejestrCzasu/Accounts/IdentitySetup.cs#L27) — hasło, blokada, bez e-maila.
- [IdentitySetup.cs:56](../../../src/RejestrCzasu/Accounts/IdentitySetup.cs#L56) — ciasteczko 14 dni z przedłużaniem.
- [AppSignInManager.cs:24-39](../../../src/RejestrCzasu/Accounts/AppSignInManager.cs#L24) — `CanSignInAsync`, `CheckPasswordSignInAsync`, `PreSignInCheck`.
- [AccountSignIn.cs:39](../../../src/RejestrCzasu/Accounts/AccountSignIn.cs#L39) — rejestracja.
- [AccountSignIn.cs:66](../../../src/RejestrCzasu/Accounts/AccountSignIn.cs#L66) — logowanie i `last_login_at`.

## - [x] 4. Statyczne strony /konto i wylogowanie — zaakceptowany

`App.razor` wybiera tryb renderowania na żądanie: strony z `[ExcludeFromInteractiveRouting]` renderują się statycznie, reszta w obwodzie. Formularze to `EditForm` z `FormName`, wysyłane zwykłym POST-em. Wylogowanie to osobny endpoint, który sam sprawdza token antyforgery.

- [App.razor:30](../../../src/RejestrCzasu/Components/App.razor#L30) — wybór trybu.
- [Login.razor:1-5](../../../src/RejestrCzasu/Accounts/Login.razor#L1) i [Register.razor:1-5](../../../src/RejestrCzasu/Accounts/Register.razor#L1) — trasy, tryb statyczny, limit prób.
- [AccountLayout.razor](../../../src/RejestrCzasu/Accounts/AccountLayout.razor) — układ bez menu, z linkiem Pomoc.
- [AccountEndpoints.cs:21](../../../src/RejestrCzasu/Accounts/AccountEndpoints.cs#L21) — `POST /konto/wyloguj`.

## - [x] 5. Autoryzacja całej aplikacji — zaakceptowany

Jeden mechanizm: `[Authorize]` w głównym `_Imports.razor`, wyjątki przez `[AllowAnonymous]` na stronach `/konto`, „nie znaleziono” i błędu. `AuthorizeRouteView` przekierowuje anonima na logowanie z `ReturnUrl`, a zalogowanego bez uprawnień pokazuje widok braku dostępu.

- [_Imports.razor:12](../../../src/RejestrCzasu/_Imports.razor#L12) — `[Authorize]`.
- [Routes.razor:3-17](../../../src/RejestrCzasu/Components/Routes.razor#L3) — `Authorizing` i `NotAuthorized`.
- [RedirectToLogin.razor](../../../src/RejestrCzasu/Accounts/RedirectToLogin.razor) i [LocalUrl.cs](../../../src/RejestrCzasu/Accounts/LocalUrl.cs) — przekierowanie i ochrona `ReturnUrl`.
- [Program.cs:63-67](../../../src/RejestrCzasu/Program.cs#L63) — kolejność middleware.

## - [x] 6. CurrentUser i fabryka kontekstu Usera — zaakceptowany

`CurrentUser` ma jedno źródło na host: w obwodzie Blazora `AuthenticationStateProvider`, w żądaniu HTTP `HttpContext.User`; o tym, że zakres jest obwodem, mówi mu `CurrentUserCircuitHandler`. `UserDbContextFactory` bierze z niego `UserId` i rzuca, gdy nikt nie jest zalogowany; wariant z jawnym `UserId` jest `internal`, tylko dla testów.

- [CurrentUser.cs:13-45](../../../src/RejestrCzasu/Accounts/CurrentUser.cs#L13).
- [UserDbContextFactory.cs:13-25](../../../src/RejestrCzasu/Data/UserDbContextFactory.cs#L13).

## - [x] 7. Limit prób i polskie teksty — zaakceptowany

Limit 10 POST na minutę na IP, wspólny dla logowania i rejestracji; inne metody bez limitu; odpowiedź 429 po polsku. Błędy Identity przez polski `IdentityErrorDescriber`, teksty Fluent UI przez `FluentLocalizer`.

- [AccountRateLimit.cs:21](../../../src/RejestrCzasu/Accounts/AccountRateLimit.cs#L21).
- [PolishIdentityErrorDescriber.cs](../../../src/RejestrCzasu/Accounts/PolishIdentityErrorDescriber.cs).
- [FluentLocalizer.cs](../../../src/RejestrCzasu/Localization/FluentLocalizer.cs) i [Program.cs:41](../../../src/RejestrCzasu/Program.cs#L41).

## - [x] 8. Role jako dane schematu — zaakceptowany

Role `Administrator` i `User` są w `HasData` ze stałymi identyfikatorami, a `Initial` wygenerowano od nowa (z ręcznym indeksem `lower(name)` i triggerem `user_id`). Lokalna baza wymaga `docker compose down -v`.

- [AppRoles.cs](../../../src/RejestrCzasu/Data/AppRoles.cs) i [AppDbContext.cs:82](../../../src/RejestrCzasu/Data/AppDbContext.cs#L82).
- [20261031200135_Initial.cs](../../../src/RejestrCzasu/Data/Migrations/20261031200135_Initial.cs).

## - [x] 9. Peryferia — zaakceptowany

- [AccountsTests.cs](../../../tests/RejestrCzasu.Tests/AccountsTests.cs) — scenariusze z I/O Matrix, CSP, limit, `CurrentUser`.
- [AppClient.cs](../../../tests/RejestrCzasu.Tests/AppClient.cs) — klient HTTP z ciasteczkami i tokenem antyforgery.
- [PolishTextsTests.cs](../../../tests/RejestrCzasu.Tests/PolishTextsTests.cs) — polskie teksty.
- [README.md](../../../README.md) — ponowne `down -v`, sekcja „Konto lokalnie”.
- [app.css](../../../src/RejestrCzasu/wwwroot/app.css) — style stron `/konto`.
- [architektura, wiersz „Autoryzacja”](../architecture-rejestr-czasu/architecture-rejestr-czasu.md) — strony anonimowe.
