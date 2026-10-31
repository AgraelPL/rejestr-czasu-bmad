---
title: 'Obraz aplikacji gotowy do pracy za proxy'
type: 'feature'
ticket: '5'
created: '2026-10-31'
status: 'built'
baseline_revision: '9b095294c6c72a67ea0800af2a529b33e1b78504'
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

**Problem:** Aplikacji nie da się jeszcze uruchomić jako obrazu za Caddy. Nie ma Dockerfile ani `/health`, a za proxy limiter widziałby adres proxy zamiast klienta. Klucze Data Protection nie są trwałe, więc restart kontenera wylogowywałby wszystkich.

**Approach:** Wieloetapowy Dockerfile (sdk → aspnet:10.0), `UseForwardedHeaders` z zaufaną siecią z konfiguracji jako pierwszy element potoku, klucze Data Protection w bazie i anonimowy `/health` ze stanem bazy i tagiem obrazu (AD-19, AD-20, AD-6, AD-13).

## Boundaries & Constraints

**Always:**
- `Dockerfile` w korzeniu repozytorium:
  - etap budujący `mcr.microsoft.com/dotnet/sdk:10.0.403` (wersja z `global.json`), `dotnet publish src/RejestrCzasu -c Release`;
  - etap końcowy `mcr.microsoft.com/dotnet/aspnet:10.0` (nie chiseled, z ICU i tzdata);
  - `ENV ASPNETCORE_HTTP_PORTS=8080`, `ARG`/`ENV APP_IMAGE_TAG` z domyślnym `local`;
  - `.dockerignore` wyklucza `**/bin`, `**/obj`, `**/appsettings.Local.json`, `**/.env`, `_bmad*`, `.git` i `tests`. Obraz nie zawiera sekretów ani danych (AD-13).
- `UseForwardedHeaders` jako pierwszy `app.Use…` w `Program.cs`, przed middleware CSP:
  - `XForwardedFor | XForwardedProto`;
  - zaufana podsieć (CIDR) z `ForwardedHeaders:KnownNetwork` (na serwerze `ForwardedHeaders__KnownNetwork`) dodana do `KnownIPNetworks`, czytana przez `IOptions<T>` (AD-13);
  - bez klucza zostają tylko domyślne sieci loopback;
  - niepoprawny CIDR kończy start czytelnym polskim komunikatem.
- `AddDataProtection().PersistKeysToDbContext<AppDbContext>().SetApplicationName("RejestrCzasu")`: tabela `data_protection_keys` już istnieje, bez nowej migracji.
- `GET /health`:
  - `AddHealthChecks().AddDbContextCheck<AppDbContext>()`, `[AllowAnonymous]` i `.AllowDuringPasswordChange()` (znacznik z 4.4);
  - odpowiedź JSON `{"status":"Healthy","tag":"<APP_IMAGE_TAG>"}`;
  - 200 przy zdrowej bazie, 503 z tym samym kształtem przy niezdrowej;
  - tag czytany przez `IOptions<T>`, domyślnie `local`.
- README i `docs/admin/README.md`:
  - jak zbudować i uruchomić obraz lokalnie z bazą z `docker compose`;
  - umowa zmiennych `APP_IMAGE_TAG` i `ForwardedHeaders__KnownNetwork` dla historii 4.8.

**Never:**
- `deploy/compose.yml`, `Caddyfile`, workflow wdrożenia, GHCR (4.8).
- Ustawianie `TZ` albo zależność od strefy serwera.
- Zmiana `docker-compose.yml` i migracji.
- Zaufanie do nagłówków spoza `KnownIPNetworks` (`KnownProxies` czyste, bez `ForwardLimit = null`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Zdrowa aplikacja | `GET /health`, baza działa, `APP_IMAGE_TAG=abc123` | 200, `{"status":"Healthy","tag":"abc123"}`, bez logowania | — |
| Brak tagu | `GET /health` bez `APP_IMAGE_TAG` | 200, `tag` = `local` | — |
| Baza niedostępna po starcie | `GET /health`, baza nie odpowiada | 503, `status` ≠ `Healthy` | — |
| Wymuszona zmiana hasła | zalogowany z `must_change_password`, `GET /health` | 200 (bez przekierowania) | — |
| Proxy zaufane | żądania z adresu w `KnownNetwork` z `X-Forwarded-For: A` | limit 10 POST liczony dla A; inny klient B ma osobny limit | 429 po 10 dla A |
| Proxy niezaufane | żądania spoza `KnownNetwork` z różnymi `X-Forwarded-For` | nagłówek ignorowany, limit liczony dla adresu połączenia | 429 po 10 łącznie |
| `X-Forwarded-Proto: https` | z zaufanej sieci | `Request.Scheme == "https"` | — |
| Zły CIDR | `ForwardedHeaders:KnownNetwork=nie-cidr` | start się nie kończy sukcesem | polski komunikat |
| Restart | logowanie w instancji 1, nowa instancja na tej samej bazie | ciasteczko z instancji 1 działa w instancji 2; klucz w `data_protection_keys` | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Program.cs`:
  - L46-55: middleware CSP; `UseForwardedHeaders` idzie przed nim;
  - L63: komentarz zapowiadający 4.5, do zastąpienia;
  - L17-24: źródła konfiguracji i wzorzec `AddOptions<T>().Bind`;
  - L77: `MapAccountEndpoints`, a obok `MapHealthChecks`.
- `src/RejestrCzasu/Options/DatabaseOptions.cs`, `FirstAdminOptions.cs`: wzorzec klasy opcji (`SectionName`).
- `src/RejestrCzasu/Data/AppDbContext.cs`: L25 już implementuje `IDataProtectionKeyContext`, L42 `DataProtectionKeys`, L90 tabela `data_protection_keys`. Pakiet `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` jest w csproj; `AddDataProtection` nie jest nigdzie wołane.
- `src/RejestrCzasu/Data/DatabaseServiceCollectionExtensions.cs`: scoped `AppDbContext` w trybie systemowym (AD-16), z którego korzystają `PersistKeysToDbContext` i `AddDbContextCheck`. Data Protection rejestrować obok `AddAppDatabase` albo w `IdentitySetup`, z komentarzem AD-19.
- `src/RejestrCzasu/RejestrCzasu.csproj`: dodać `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore` w wersji zgodnej z pozostałymi 10.0.x.
- `src/RejestrCzasu/Accounts/PasswordChangeRedirect.cs`: `.AllowDuringPasswordChange()` dla `/health`.
- `src/RejestrCzasu/Accounts/AccountRateLimit.cs`: partycja po `Connection.RemoteIpAddress`. Bez zmian, bo forwarded headers podmieniają ten adres przed limiterem.
- `tests/RejestrCzasu.Tests/AppClient.cs`: klient per test na `WebApplicationFactory` z `ConfigurationOverrides`. TestServer daje `RemoteIpAddress == null`, więc testy proxy ustawiają adres połączenia przez `IStartupFilter` dodany w `WithWebHostBuilder`; filtr uruchamia się przed potokiem z `Program.cs`. Zła baza po starcie: `PostgresFixture` / osobna fabryka, w której po starcie podmienia się `Database:Port` albo zatrzymuje dostęp.
- `tests/RejestrCzasu.Tests/ApplicationTests.cs`: wzorzec testu procesu `RunApplicationAsync` (dla złego CIDR, kod różny od 0 i komunikat).
- `.github/workflows/pr.yml`: bez zmian (obraz buduje dopiero 4.8).

## Tasks & Acceptance

**Execution:**
- [x] `Dockerfile`, `.dockerignore` -- obraz wieloetapowy -- AD-20, AD-6, AD-13.
- [x] `src/RejestrCzasu/Options/` (opcje forwarded headers i tagu), `Program.cs`, rejestracja Data Protection, `/health` z writerem JSON, `RejestrCzasu.csproj` -- AD-19, AD-15.
- [x] `README.md`, `docs/admin/README.md` -- obraz lokalnie i kontrakt zmiennych.
- [x] `tests/RejestrCzasu.Tests/` -- każdy wiersz I/O Matrix.

**Acceptance Criteria:**
- Given wszystkie zmiany, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone.
- Given baza z `docker compose up -d`, when uruchomię obraz komendami poniżej, then:
  - na `http://127.0.0.1:8080` jest strona logowania;
  - `/health` daje 200 z tagiem `test-4-5`;
  - po `docker restart` zalogowana sesja w przeglądarce trwa;
  - w kontenerze istnieje `/usr/share/zoneinfo/Europe/Warsaw`;
  - aplikacja startuje z kulturą `pl-PL`, co nie działa bez ICU.
- Given obraz, when listuję `/app`, then nie ma tam `appsettings.Local.json` ani `.env`.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone
- `dotnet ef migrations has-pending-model-changes --project src/RejestrCzasu` -- expected: brak zmian
- `docker build --build-arg APP_IMAGE_TAG=test-4-5 -t rejestr-czasu:test .` -- expected: sukces
- `docker run -d --name rejestr-app-test --network rejestr-czasu-bmad_default -p 127.0.0.1:8080:8080 -e Database__Host=db -e Database__Password=<z .env> -e FirstAdmin__UserName=admin -e FirstAdmin__Password=<hasło> rejestr-czasu:test`, potem `curl http://127.0.0.1:8080/health` -- expected: 200 z `test-4-5`. Po sprawdzeniu `docker rm -f rejestr-app-test`.

Uruchomiona przez Autora aplikacja może blokować `src/RejestrCzasu/bin`. Wtedy build i testy uruchamiaj na kopii repozytorium w scratchpadzie, nie zatrzymuj jego procesu i nie restartuj jego `docker compose`.

## Implementation Notes

- Opcje: `Options/ProxyOptions` (sekcja `ForwardedHeaders`, `KnownNetwork`) i `Options/AppImageOptions` (klucz najwyższego poziomu `APP_IMAGE_TAG`, domyślnie `local`). `ForwardedHeadersSetup` konfiguruje `ForwardedHeadersOptions` przez `IOptions<ProxyOptions>`; `Program` buduje je zaraz po `Build()` i przy złym CIDR wypisuje polski komunikat i kończy kodem 1 (przed bazą). `System.Net.IPNetwork.TryParse` akceptuje adres z bitami hosta (`172.18.0.1/16`), odrzuca adres bez maski i maskę > 32.
- `HealthEndpoint` (`AddAppHealthChecks`, `MapAppHealth`): `MapHealthChecks` z writerem JSON (`WriteAsJsonAsync`, camelCase), `.AllowAnonymous().AllowDuringPasswordChange()`, domyślne kody (Unhealthy → 503).
- `AddAppDataProtection` w `DatabaseServiceCollectionExtensions` obok `AddAppDatabase`.
- `.dockerignore` dodatkowo wyklucza `appsettings.Local.example.json`, `docs`, `.github`, `.agents`, `.claude` itd.; w `/app` zostaje tylko `appsettings.json`. Obraz działa jako użytkownik `app` (`USER $APP_UID`).
- Testy: `ForwardedHeadersTests` (zaufane/niezaufane proxy, limit per klient, `X-Forwarded-Proto`, adres IPv4-mapped, brak klucza → tylko loopback, walidacja CIDR), `HealthTests` (tag, `local`, wymuszona zmiana hasła, 503 po `DROP DATABASE ... WITH (FORCE)` osobnej bazy), `DataProtectionTests` (ciasteczko z instancji 1 w instancji 2, jeden klucz w `data_protection_keys`), `ApplicationTests.Startup_InvalidKnownNetwork_...` (proces, kod 1). `AppClient` przyjmuje dodatkową konfigurację i akcję `IWebHostBuilder`.
- W logu kontenera przy starcie pojawia się nieszkodliwe `Cannot load library libgssapi_krb5.so.2` (Npgsql sprawdza GSSAPI); opisane w README, obraz bez dodatkowych pakietów.

## Plan Change Log

## Review Triage Log

| # | Lens | Finding | Verdict | Route | Evidence |
|---|------|---------|---------|-------|----------|
| 1 | quick | `APP_IMAGE_TAG` z `.env` (przez `env_file`) nadpisuje tag wbudowany w obraz, więc `/health` nie wykryje złego obrazu | false | reject | Środowisko kontenera ustalane jest przy jego tworzeniu z tego samego `.env`, którym compose interpoluje nazwę obrazu; `/health` podaje więc tag, z którym ten kontener powstał. Kontener nieutworzony od nowa zgłasza stary tag, więc porównanie w 4.8 wykrywa rozjazd. |
| 2 | quick | `Health_WithoutTag_ReportsLocal` zależy od zmiennej `APP_IMAGE_TAG` w środowisku procesu testów | low | patch | `Program.cs` woła `AddEnvironmentVariables()`, a test nie nadpisuje klucza; poprawka: `["APP_IMAGE_TAG"] = null` w konfiguracji `AppClient`. |
| 3 | quick | `docs/admin/README.md` L71: „maska większa niż /32” jest prawdą tylko dla IPv4 | low | patch | `IPNetwork.TryParse` przyjmuje prefiksy IPv6 do /128; poprawka tekstu. |
