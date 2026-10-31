---
title: 'Kopia bazy przy starcie i odtwarzanie z README'
type: 'feature'
ticket: '2'
created: '2026-10-31'
status: dropped
baseline_revision: ''
route: 'full'
route_source: 'auto'
risk: 'high'
review: ''
review_source: ''
lenses_ran: []
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Start aplikacji stosuje migracje bez Kopii bazy (R14), a README nie mówi, jak odtworzyć bazę z kopii ani jak wyłączyć telemetrię CLI (AD-12).

**Approach:** Krok 2 z AD-3 w `DatabaseStartup`: `docker exec <ContainerName> pg_dump` do katalogu `Backup:Directory`, zapis binarny do pliku tymczasowego i zmiana nazwy po sukcesie, potem rotacja do 10 plików. Domyślny katalog `%LOCALAPPDATA%\RejestrCzasu\kopie`. README dostaje sekcję odtwarzania przez `psql` i `DOTNET_CLI_TELEMETRY_OPTOUT=1`.

## Boundaries & Constraints

**Always:** kolejność AD-3: sprawdzenie bazy → Kopia bazy → `Migrate()` → serwer. Nazwa `rejestr-RRRR-MM-DD_GGMMSS.sql` z czasu lokalnego z `TimeProvider`. Bez `-t` w `docker exec`. Wyjście `pg_dump` kopiowane bajt w bajt ze strumienia, nigdy przez tekst. Najstarsze pliki usuwane dopiero po udanym zapisie nowego. Rotacja obejmuje wyłącznie pliki pasujące do wzorca nazwy. Komunikaty po polsku, kod po angielsku. Testy na Testcontainers (prawdziwy `docker exec` na kontenerze testowym), katalog kopii w testach tymczasowy.

**Never:** kopie w trakcie działania aplikacji; `pg_dump` z hosta (wersja klienta ≠ serwer); hasło w argumentach procesu; usuwanie plików innych niż `rejestr-*.sql`; domyślny katalog w repozytorium, w Dokumentach ani w OneDrive; nowe pakiety NuGet.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Kopia udana | kontener działa, katalog istnieje lub nie | nowy niepusty `rejestr-…sql` z treścią `pg_dump`, potem migracje | — |
| Rotacja | 10 starszych kopii + nowa | zostaje 10 najnowszych, najstarsza usunięta | — |
| Obce pliki | w katalogu `notatka.txt`, `inna.sql` | nietknięte, nie liczą się do 10 | — |
| Zła nazwa kontenera | `Database:ContainerName` błędna | brak nowego pliku, stare kopie nietknięte, bez migracji | konsola: „Kopia bazy nie powiodła się: …” ze stderr `docker`, kod 1 |
| Brak `docker` w PATH | proces nie startuje | jak wyżej | komunikat wskazuje Docker Desktop / PATH |
| Pusty katalog w konfiguracji | `Backup:Directory` = "" | bez kopii i migracji | konsola: „ustaw `Backup:Directory`…”, kod 1 |
| Katalog niezapisywalny | brak uprawnień | jak wyżej | komunikat z przyczyną, kod 1 |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Startup/DatabaseStartup.cs` -- `RunAsync`: komentarz „Step 2” to miejsce wywołania; wzorzec błędów: `DatabaseStartupException` z polskim komunikatem, `when (!cancellationToken.IsCancellationRequested)`. Nie zmieniać kroków 1 i 3.
- `src/RejestrCzasu/Options/BackupOptions.cs` -- `Directory`; `DatabaseOptions` daje `ContainerName`, `User`, `Name`.
- `src/RejestrCzasu/Program.cs` -- rejestracja `DatabaseStartup` (transient); `TimeProvider.System` już w DI.
- `src/RejestrCzasu/appsettings.json` -- `Backup:Directory` dziś pusty.
- `tests/RejestrCzasu.Tests/PostgresFixture.cs` -- `DatabaseOptions.ContainerName` = nazwa kontenera Testcontainers; `BackupDirectory` tymczasowy, już w `ConfigurationOverrides`.
- `tests/RejestrCzasu.Tests/DatabaseStartupTests.cs` -- helper `CreateStartup` buduje `DatabaseStartup` ręcznie; zaktualizować o nowe zależności.
- `README.md` -- sekcja `## Kopia bazy` to placeholder (komentarz HTML); dane w wolumenie `rejestr-czasu-data`.
- `.gitignore` -- już wyklucza `rejestr-*.sql` i `backups/`.

## Tasks & Acceptance

**Execution:**
- [ ] `src/RejestrCzasu/Startup/DatabaseBackup.cs` -- nowa klasa: tworzy katalog (z `Environment.ExpandEnvironmentVariables`), uruchamia `docker exec <container> pg_dump -U <user> -d <db> --clean --if-exists`, kopiuje stdout do `<nazwa>.sql.part`, przy kodzie 0 i niepustym pliku zmienia nazwę na `.sql`, potem rotuje; przy błędzie usuwa `.part` i rzuca `DatabaseStartupException` -- krok 2 AD-3
- [ ] `src/RejestrCzasu/Startup/DatabaseStartup.cs`, `Program.cs` -- wstrzyknięcie i wywołanie kroku 2; komentarze kolejności -- AD-3
- [ ] `src/RejestrCzasu/appsettings.json`, `appsettings.Local.example.json` -- domyślny `Backup:Directory` = `%LOCALAPPDATA%\\RejestrCzasu\\kopie` -- R15
- [ ] `tests/RejestrCzasu.Tests/DatabaseBackupTests.cs` + aktualizacja `DatabaseStartupTests.cs` -- wszystkie wiersze macierzy; kolejność: przy złym kontenerze na świeżej bazie nie ma `__EFMigrationsHistory` -- dowód macierzy
- [ ] `README.md` -- sekcja „Kopia bazy”: katalog, nazwy, rotacja, odtworzenie (`docker cp` + `docker exec … psql -v ON_ERROR_STOP=1 -f`), zatrzymanie aplikacji przed odtworzeniem; sekcja telemetrii z `[Environment]::SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT","1","User")` -- R14, AD-12

**Acceptance Criteria:**
- Given działająca baza, when Autor uruchamia `dotnet run` 11 razy, then w katalogu kopii jest 10 plików `rejestr-…sql`, najnowszy z ostatniego startu.
- Given błędny `Database:ContainerName` w `appsettings.Local.json`, when `dotnet run`, then proces kończy się komunikatem „Kopia bazy nie powiodła się…” i serwer nie startuje.
- Given Kopia bazy z danymi, when Autor zmienia dane i odtwarza kopię według README, then widzi dane z chwili kopii.

## Implementation Notes

## Plan Change Log

## Review Triage Log

## Design Notes

- `--clean --if-exists`: kopia odtwarza się na istniejącą bazę z migracjami bez ręcznego czyszczenia.
- Odtwarzanie przez `docker cp` + `psql -f`, bo przekierowanie `<` nie istnieje w PowerShell, a potok `Get-Content | docker exec -i` w PowerShell 5.1 psuje kodowanie i końce linii.
- `docker exec` łączy się przez gniazdo w kontenerze (`local trust` obrazu `postgres`), więc hasło nie jest potrzebne.
- Sortowanie po nazwie pliku = sortowanie po czasie (format `RRRR-MM-DD_GGMMSS`).

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln -warnaserror` -- expected: 0 błędów i ostrzeżeń
- `dotnet test` -- expected: wszystkie testy przechodzą
- `dotnet list package --vulnerable` -- expected: brak podatnych pakietów

**Manual checks (if no CLI):**
- Autor: kryteria akceptacji 1–3, w tym jednorazowe odtworzenie kopii według README.
