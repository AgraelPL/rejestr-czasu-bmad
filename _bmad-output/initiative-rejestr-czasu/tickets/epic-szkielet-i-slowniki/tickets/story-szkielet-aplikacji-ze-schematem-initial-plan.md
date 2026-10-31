---
title: 'Szkielet aplikacji ze schematem Initial'
type: 'feature'
ticket: '1'
created: '2026-10-30'
status: 'done'
baseline_revision: '43f18efb5063c58996d33bf5c5f0f4d0ecfa7628'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-szkielet-i-slowniki/epic-szkielet-i-slowniki.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Repozytorium nie ma kodu; kolejne historie (Kopia bazy, układ, typy domeny, ReportGrid, Słowniki) potrzebują działającej aplikacji, kompletnego schematu i infrastruktury testów.

**Approach:** Scaffold Blazor Web App (.NET 10, Interactive Server, Fluent UI 5) z PostgreSQL 18 w Dockerze, migracją `Initial` ze wszystkimi encjami, startem „sprawdź bazę → Migrate() → serwer na 127.0.0.1”, CSP, konfiguracją lokalną poza gitem, README oraz projektem testów xUnit + bUnit + Testcontainers.

## Boundaries & Constraints

**Always:** AD-1 (stos i wersje z tabeli Stack, scaffold `dotnet new blazor --interactivity Server --all-interactive --empty --no-https`), AD-2 (`postgres:18`, `127.0.0.1:5432:5432`, nazwany wolumen na `/var/lib/postgresql`, `restart: "no"`, stały `container_name`, hasło z `.env`), AD-3 kroki 1, 3, 4 w tej kolejności, AD-4 (`IDbContextFactory`), AD-6 (kultura `pl-PL`, `TimeProvider.System` w DI), AD-7 (encje, indeksy, FK RESTRICT, kod Projektu wielkimi literami w `SaveChanges`), AD-12 (zasoby lokalne, CSP `script-src 'self'`), AD-13 (`appsettings.Local.json` przez `AddJsonFile(optional: true)`, sekcje `Database`, `Backup`, `Export` przez `IOptions<T>`). Kod po angielsku, UI i komunikaty po polsku.

**Never:** krok Kopii bazy (historia 2), `MainLayout` z menu, `Notify`, `Confirm` (historia 3), `Domain/*` (historia 4), `ReportGrid` (historia 5), ekrany Słowników, `DbErrors`, `DictionaryOptions`. Bez własnego JavaScriptu, CDN, zewnętrznych czcionek, `EnsureCreated()`, innych bibliotek UI, HTTPS. Bez prawdziwych danych w repo.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Start poprawny | kontener działa, konfiguracja OK | migracje zastosowane, strona na `http://127.0.0.1:5080` z nagłówkiem CSP | — |
| Baza niedostępna | kontener zatrzymany | proces kończy się kodem ≠ 0 przed startem serwera | konsola: „Brak połączenia z bazą: uruchom Docker Desktop i `docker compose up -d`.” |
| Migracja nieudana | wyjątek w `Migrate()` | proces kończy się kodem ≠ 0 | konsola: czytelny komunikat z przyczyną |
| Kod Projektu małymi literami | `Code = "rdz"` zapisany | w bazie `RDZ` | — |
| Duplikat kodu / nazwy Typu | `RDZ` i `rdz`; `Dev` i `dev` | drugi zapis odrzuca baza | wyjątek `DbUpdateException` (obsługa w historii 7) |
| Usunięcie użytego Projektu | Projekt we Wpisie | baza odrzuca usunięcie | FK RESTRICT |

</frozen-after-approval>

## Code Map

Repozytorium nie ma kodu (tylko `.gitignore`, `docs/`, `_bmad*`). `.gitignore` już wyklucza `appsettings.Local.json`, `.env`, `bin/`, `obj/`, `rejestr-*.sql` — zachować, dopisać tylko brakujące wpisy.

- Pakiety dostępne na nuget.org: Fluent UI 5.0.1, Npgsql EF 10.0.3, EFCore.NamingConventions 10.0.1, EF Design 10.0.x, Testcontainers.PostgreSql 4.16.0, bunit 2.13.3, xunit.v3 4.0.2. `dotnet-ef` nie jest zainstalowany — narzędzie lokalne.
- Docker Desktop 29.9.2 (per-user), .NET SDK 10.0.401.

## Tasks & Acceptance

**Execution:**
- [x] `RejestrCzasu.sln`, `src/RejestrCzasu/` -- scaffold poleceniem z AD-1; usunąć HTTPS i przykładowe strony; `global.json` z SDK 10.0.401 -- baza projektu
- [x] `.config/dotnet-tools.json` -- lokalne `dotnet-ef` 10.0.x -- migracje bez instalacji globalnej
- [x] `docker-compose.yml`, `.env.example` -- według AD-2; `container_name: rejestr-czasu-db`; `POSTGRES_USER/PASSWORD/DB` z `.env` -- baza lokalna
- [x] `src/RejestrCzasu/appsettings.json`, `appsettings.Local.example.json`, `Properties/launchSettings.json` -- Kestrel `http://127.0.0.1:5080`; sekcje `Database` (Host, Port, Name, User, Password, ContainerName), `Backup` (Directory, pusty), `Export` (Prefix, pusty) z bezpiecznymi wartościami -- AD-13
- [x] `src/RejestrCzasu/Options/*.cs` -- `DatabaseOptions`, `BackupOptions`, `ExportOptions`; connection string budowany z `DatabaseOptions` -- jedno źródło parametrów
- [x] `src/RejestrCzasu/Data/Project.cs`, `WorkType.cs`, `TimeEntry.cs`, `AppDbContext.cs` -- encje AD-7, `UseSnakeCaseNamingConvention()`, unikalny indeks na `code`, FK `Restrict`, normalizacja kodu w `SaveChanges`/`SaveChangesAsync` -- schemat dla całego projektu
- [x] `src/RejestrCzasu/Data/Migrations/*_Initial.cs` -- wygenerowana przez `dotnet ef`, plus `migrationBuilder.Sql` dla unikalnego indeksu `lower(name)` na `work_type` (i `DROP` w `Down`) -- AD-7
- [x] `src/RejestrCzasu/Startup/DatabaseStartup.cs` -- krok 1 (sprawdzenie połączenia z krótkim timeoutem) i krok 3 (`Migrate()`); punkt wstawienia kroku 2 oznaczony komentarzem dla historii 2; błąd → komunikat w konsoli i kod wyjścia 1 -- AD-3
- [x] `src/RejestrCzasu/Program.cs` -- `AddJsonFile("appsettings.Local.json", optional: true)`, opcje, `AddDbContextFactory`, `AddFluentUIComponents()` (z usługą dialogów), `TimeProvider.System`, kultura `pl-PL`, middleware nagłówka CSP, `DatabaseStartup` przed `app.Run()` -- szkielet usług dla historii 2–9
- [x] `src/RejestrCzasu/Components/App.razor`, `Routes.razor`, `Layout/MainLayout.razor`, `Pages/Home.razor` -- zasoby Fluent UI z `_content`, Segoe UI, `lang="pl"`, prosta strona startowa „Rejestr czasu pracy” z jednym komponentem Fluent -- tracer przez UI
- [x] `tests/RejestrCzasu.Tests/` -- xunit.v3, bunit, Testcontainers.PostgreSql, `Microsoft.AspNetCore.Mvc.Testing`; `PostgresFixture` (jeden kontener `postgres:18` na zestaw testów, schemat przez `Migrate()`, `ResetAsync()` = `TRUNCATE … RESTART IDENTITY CASCADE`) -- infrastruktura testów zapisu dla historii 7–9
- [x] `tests/RejestrCzasu.Tests/*Tests.cs` -- test zapisu (normalizacja kodu, odrzucenie duplikatu nazwy Typu bez względu na wielkość liter, RESTRICT), test aplikacji przez `WebApplicationFactory` na bazie z kontenera (odpowiedź 200 z nagłówkiem CSP), test bUnit strony startowej z usługami Fluent UI -- dowód macierzy I/O
- [x] `README.md` -- wymagania (Docker Desktop, SDK), tworzenie `.env` i `appsettings.Local.json` z przykładów, `docker compose up -d`, `dotnet run --project src/RejestrCzasu`, `dotnet test`, podgląd tabel przez `docker exec rejestr-czasu-db psql`; miejsce na sekcje z historii 2 -- krok hitl Autora

**Acceptance Criteria:**
- Given działający kontener i pliki lokalne utworzone według README, when Autor uruchamia `dotnet run`, then strona startowa otwiera się na `http://127.0.0.1:5080`, odpowiedź ma nagłówek `Content-Security-Policy`, a karta Sieć w DevTools pokazuje tylko żądania do 127.0.0.1.
- Given uruchomiona aplikacja, when Autor wykonuje `docker exec rejestr-czasu-db psql -U <user> -d <db> -c "\dt"`, then widzi tabele `project`, `work_type`, `time_entry` i `__EFMigrationsHistory`.
- Given zatrzymany kontener, when Autor uruchamia `dotnet run`, then proces kończy się komunikatem z „uruchom Docker Desktop i docker compose up -d”, bez startu serwera.
- Given repozytorium po zmianie, when `git status` po utworzeniu `.env` i `appsettings.Local.json`, then żaden z nich nie jest śledzony.

## Implementation Notes

- `DatabaseStartup.RunAsync()` wywoływany w `Program.cs` po `Build()`, przed `app.RunAsync()`; błąd → `DatabaseStartupException`, `Program` wypisuje komunikat na stderr i zwraca 1. Wariant z `IHostedLifecycleService` odrzucony: host logował pełny stos wyjątku („Hosting failed to start”).
- SDK 10 domyślnie tworzy `.slnx` i manifest narzędzi w katalogu głównym — utworzono `RejestrCzasu.sln` (`--format sln`) i przeniesiono manifest do `.config/`.
- `dotnet test` na SDK 10 z xunit.v3 wymaga `"test": { "runner": "Microsoft.Testing.Platform" }` w `global.json`; bez `Microsoft.NET.Test.Sdk`/`xunit.runner.visualstudio`.
- Jawne `Microsoft.EntityFrameworkCore.Relational` 10.0.14 w aplikacji: Npgsql 10.0.3 ciągnie EF 10.0.4, `Design` 10.0.14 — konflikt MSB3277 w projekcie testów.
- Blazor .NET 10 dodaje drugi nagłówek CSP (`frame-ancestors 'self'`) — wyłączony `ContentSecurityFrameAncestorsPolicy = null`; nasz CSP ma `frame-ancestors 'none'`.
- Usunięto `<ImportMap />` i `ReconnectModal` (własny plik JS szablonu); Blazor używa wbudowanego UI ponownego łączenia.
- Test „baza niedostępna” uruchamia prawdziwy proces aplikacji (port 1) i sprawdza kod 1, komunikat i brak „Now listening”.
- Przy pierwszym starcie na pustej bazie EF loguje `fail` zapytania o `__EFMigrationsHistory` (zachowanie EF/Npgsql, nieszkodliwe) — opisane w README.
- Audyt macierzy (krok 3): brakował test wiersza „Migracja nieudana” — dodano `tests/RejestrCzasu.Tests/DatabaseStartupTests.cs` (baza z kolidującą tabelą `project` → `DatabaseStartupException` „Migracja bazy nie powiodła się…”); `dotnet test` 11/11.
- Po przeglądzie: poprawki 1–6 z Review Triage Log; poprawka 6 nadpisała `DatabaseStartupTests.cs` — test „Migracja nieudana” przywrócony obok testu złego hasła. `dotnet build -warnaserror` 0 ostrzeżeń, `dotnet test` 12/12, brak podatnych pakietów.

## Plan Change Log

## Review Triage Log

Przebieg 1 (quick): medium 2, low 4, false 1, maybe-false 0.

| # | Znalezisko | Werdykt | Ścieżka | Dowód |
|---|---|---|---|---|
| 1 | `Program.cs`: `appsettings.Local.json` dodany po zmiennych środowiskowych i CLI, więc je nadpisuje | medium | patch | `AddJsonFile` po `CreateBuilder` dokłada źródło na koniec; `Database__*`/`--Database:*` przegrywają z plikiem lokalnym, a test „baza niedostępna” na nich polega. Poprawka: ponownie dodać env i CLI po pliku lokalnym. |
| 2 | `ApplicationTests`: `WebApplicationFactory` czyta prawdziwy `appsettings.Local.json` Autora (`Backup`, `Export`) | low | patch | Content root to `src/RejestrCzasu`; nadpisany jest tylko `Database:*`, więc historia 2 pisałaby kopie do katalogu Autora. Poprawka: nadpisać `Backup:Directory` (katalog tymczasowy) i `Export:Prefix` w `ConfigurationOverrides`. |
| 3 | `Program.cs`: CSP ginie na odpowiedziach błędu (`ExceptionHandler` czyści nagłówki) | low | patch | Middleware ustawia nagłówek przed `next()`, a `ExceptionHandlerMiddleware` czyści odpowiedź przed `/Error`. Poprawka: ustawiać nagłówek w `Response.OnStarting`. |
| 4 | `README.md`: zła nazwa wolumenu (Compose dodaje prefiks projektu) | low | patch | `volumes: rejestr-czasu-data:` bez `name:` daje `rejestr-czasu-bmad_rejestr-czasu-data`. Poprawka: `name: rejestr-czasu-data` w compose. |
| 5 | `docker-compose.yml`: brak `.env` daje puste zmienne zamiast błędu | low | patch | `${VAR}` bez `:?` podstawia pusty tekst z ostrzeżeniem. Poprawka: `${VAR:?…}` dla trzech zmiennych. |
| 6 | `DatabaseStartup`: każdy błąd połączenia jako „uruchom Docker” | medium | patch | Złe hasło (`28P01`) lub brak bazy (`3D000`) to `PostgresException` z osiągalnego serwera, a nagłówek kieruje Autora do Dockera; README sam ostrzega przed rozjazdem `.env`/`appsettings.Local.json`. Poprawka: osobny komunikat dla `PostgresException`. |
| 7 | Backticki w komunikacie konsoli vs AC 3 | false | — | Macierz I/O (zamrożona) podaje komunikat z `docker compose up -d` jako kodem; AC sprawdza treść, którą komunikat zawiera. Brak złego skutku. |

## Design Notes

- CSP: `default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; connect-src 'self'; img-src 'self' data:; font-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'`. Szablon .NET 10 wstawia `<ImportMap />`, który generuje skrypt inline blokowany przez `script-src 'self'` — usunąć go albo potwierdzić w przeglądarce, że konsola nie zgłasza naruszeń CSP. Fluent UI 5 rejestruje usługi i dostawców inaczej niż 4.x — sprawdzić w dokumentacji pakietu 5.0.1, nie kopiować wzorców z v4.
- Długości kolumn (wybór planu, poprawialny w historiach Słowników): `code` 20, `name` 200, `description` bez limitu (`text`), `minutes` `int`, `date` `date`.
- Test aplikacji podmienia `Database:*` na parametry kontenera Testcontainers, więc ścieżka startu (sprawdzenie bazy, `Migrate()`) jest tą samą, co u Autora.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln -warnaserror` -- expected: 0 błędów
- `dotnet test` -- expected: wszystkie testy przechodzą (Docker Desktop uruchomiony)
- `dotnet list package --vulnerable` -- expected: brak podatnych pakietów

**Manual checks (if no CLI):**
- Autor: kryteria akceptacji 1–3 na swoim komputerze po krokach z README.
