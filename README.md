# Rejestr czasu pracy

Lokalna aplikacja (Blazor Server, .NET 10) do rejestrowania czasu pracy. Działa tylko na komputerze Autora:
serwer nasłuchuje na `http://127.0.0.1:5080`, baza PostgreSQL 18 działa w kontenerze Dockera
z portem wystawionym tylko na `127.0.0.1`.

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
   W `appsettings.Local.json` ustaw też `Backup:Directory` (katalog kopii poza repozytorium i poza folderami
   synchronizowanymi z chmurą) oraz `Export:Prefix`.

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

## Testy

```powershell
dotnet test
```

Testy zapisu do bazy i test aplikacji uruchamiają własny, tymczasowy kontener `postgres:18` (Testcontainers),
więc Docker Desktop musi działać; baza Autora nie jest używana.

## Migracje

Narzędzie `dotnet-ef` jest narzędziem lokalnym repozytorium (`.config/dotnet-tools.json`):

```powershell
dotnet tool restore
dotnet ef migrations add <Nazwa> --project src/RejestrCzasu --output-dir Data/Migrations
```

## Kopia bazy

<!-- Sekcja uzupełniana w historii 2 „Kopia bazy”: katalog kopii, nazwy plików, rotacja, odtworzenie przez psql. -->
