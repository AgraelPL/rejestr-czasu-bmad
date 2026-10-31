# Rejestr czasu pracy

Aplikacja webowa (Blazor Server, .NET 10) do rejestrowania czasu pracy, z kontami Userów i rolą Administratora.
Docelowo działa pod `czas-pracy.kandula-consulting.pl` (wdrożenie: historia 4.8, instrukcje w [`docs/admin/`](docs/admin/README.md)).
Do pracy nad nią uruchamia się ją lokalnie: serwer nasłuchuje na `http://127.0.0.1:5080`, a baza PostgreSQL 18
działa w kontenerze Dockera z portem wystawionym tylko na `127.0.0.1`.

## Wymagania

- Docker Desktop (Windows), polecenie `docker` w `PATH`.
- .NET SDK 10.0.403 (wersja przypięta w `global.json`).
- Wyłączona telemetria .NET CLI — raz, jako zmienna użytkownika (PowerShell):

  ```powershell
  [Environment]::SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1", "User")
  ```

## Pierwsze uruchomienie

1. Utwórz pliki lokalne z przykładów (oba są wykluczone w `.gitignore` i nie trafiają do repozytorium):

   ```powershell
   Copy-Item .env.example .env
   Copy-Item src/RejestrCzasu/appsettings.Local.example.json src/RejestrCzasu/appsettings.Local.json
   ```

2. Ustaw własne hasło bazy: `POSTGRES_PASSWORD` w `.env` i to samo w `Database:Password`
   w `appsettings.Local.json`. Użytkownik i nazwa bazy w `.env` (`POSTGRES_USER`, `POSTGRES_DB`) muszą się zgadzać
   z `Database:User` i `Database:Name` (domyślnie `rejestr` w `appsettings.json`).

   Hasło z `.env` jest używane tylko przy pierwszym utworzeniu wolumenu bazy; późniejsza zmiana w `.env` nie zmienia hasła
   istniejącej bazy.

   Jeśli port 5432 jest zajęty (np. przez PostgreSQL zainstalowany w Windows jako usługa — kontener zostaje wtedy
   w stanie `Created`, a aplikacja zgłasza błąd hasła 28P01 z cudzego serwera), ustaw inny port, np. `5433`,
   w `POSTGRES_PORT` w `.env` i ten sam w `Database:Port` w `appsettings.Local.json`.

3. Uruchom bazę i aplikację:

   ```powershell
   docker compose up -d
   dotnet run --project src/RejestrCzasu
   ```

   Aplikacja otwiera się na `http://127.0.0.1:5080`. Przy każdym starcie, przed przyjęciem pierwszego żądania,
   sprawdza połączenie z bazą i stosuje migracje. Przy pierwszym starcie na pustej bazie EF Core może zapisać w konsoli
   błąd zapytania o tabelę `__EFMigrationsHistory` — to oczekiwane, tabela jest tworzona zaraz potem.

   Jeśli kontener nie działa, aplikacja kończy się komunikatem
   „Brak połączenia z bazą: uruchom Docker Desktop i `docker compose up -d`.” bez startu serwera.

## Codzienna praca

```powershell
docker compose up -d
dotnet run --project src/RejestrCzasu
```

Kontener ma `restart: "no"` — nie startuje sam z Dockerem. Zatrzymanie bazy: `docker compose stop`.
Dane bazy leżą w nazwanym wolumenie Dockera `rejestr-czasu-data` (polecenie `docker compose down -v` usuwa je bezpowrotnie).

## Podgląd bazy

```powershell
docker exec rejestr-czasu-db psql -U rejestr -d rejestr -c "\dt"
```

Zamień `rejestr` na wartości `POSTGRES_USER` i `POSTGRES_DB` z `.env`, jeśli je zmieniono.

## Konto lokalnie

Cała aplikacja wymaga logowania. Konto zakłada się na `/konto/rejestracja`; nowe Konto czeka na zatwierdzenie
(stan Oczekuje). Dopóki nie ma ekranu Użytkownicy, Konto zatwierdza się w bazie:

```powershell
docker exec rejestr-czasu-db psql -U rejestr -d rejestr -c "UPDATE users SET status = 'active' WHERE user_name = '<login>';"
```

Potem logowanie na `/konto/logowanie`. Rejestracja i logowanie przyjmują najwyżej 10 prób (POST) na minutę z jednego
adresu IP; po 5 złych hasłach z rzędu logowanie na Konto jest wstrzymane na 15 minut.

## Testy

```powershell
dotnet test
```

Testy zapisu do bazy i test aplikacji uruchamiają własny, tymczasowy kontener `postgres:18` (Testcontainers),
więc Docker Desktop musi działać; baza Autora nie jest używana.

Te same testy uruchamia job `build-and-test` (`.github/workflows/pr.yml`) na każdym pull requeście do `main`.

## Migracje

Narzędzie `dotnet-ef` jest narzędziem lokalnym repozytorium (`.config/dotnet-tools.json`):

```powershell
dotnet tool restore
dotnet ef migrations add <Nazwa> --project src/RejestrCzasu --output-dir Data/Migrations
```

Migracja `Initial` została wygenerowana od nowa (schemat z kontami Userów, tabele Identity, `user_id`).
Ostatnio (historia 4.3) doszły do niej role `Administrator` i `User`: są danymi schematu (`HasData` w `AppDbContext`
ze stałymi `Id`), więc `Initial` znów ma nowy znacznik czasu. Lokalna baza utworzona wcześniejszą wersją `Initial`
nie przejdzie startu — wolumen trzeba założyć od nowa, także jeśli robiłeś to już po poprzedniej zmianie
(usuwa wszystkie dane lokalnej bazy):

```powershell
docker compose down -v
docker compose up -d
```

Odśwież też własny `appsettings.Local.json` na podstawie `appsettings.Local.example.json` — klucze `Backup`, `Export`
i `Database:ContainerName` nie są już używane.

Po pierwszym wdrożeniu na serwer `Initial` jest zamrożona: kolejne zmiany schematu tylko nową migracją,
która wyłącznie dodaje (AD-21).
