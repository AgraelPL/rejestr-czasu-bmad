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

3. Ustaw pierwszego Administratora: `FirstAdmin:UserName` i `FirstAdmin:Password` w `appsettings.Local.json`
   (hasło co najmniej 6 znaków). Lokalny start wymaga tych ustawień, dopóki w bazie nie ma żadnego Administratora:
   przy pierwszym starcie na pustej bazie aplikacja zakłada z nich aktywne Konto z rolami Administrator i User.
   Gdy Administrator już jest, ustawienia są pomijane (zmiana hasła w pliku nie zmienia hasła Konta).

4. Uruchom bazę i aplikację:

   ```powershell
   docker compose up -d
   dotnet run --project src/RejestrCzasu
   ```

   Aplikacja otwiera się na `http://127.0.0.1:5080`. Przy każdym starcie, przed przyjęciem pierwszego żądania,
   sprawdza połączenie z bazą, stosuje migracje i w razie potrzeby zakłada pierwszego Administratora. Przy pierwszym starcie na pustej bazie EF Core może zapisać w konsoli
   błąd zapytania o tabelę `__EFMigrationsHistory` — to oczekiwane, tabela jest tworzona zaraz potem.

   Jeśli kontener nie działa, aplikacja kończy się komunikatem
   „Brak połączenia z bazą: uruchom Docker Desktop i `docker compose up -d`.” bez startu serwera.
   Tak samo, z polskim komunikatem, kończy się start bez Administratora w bazie i bez ustawień `FirstAdmin`
   albo z hasłem `FirstAdmin:Password` odrzuconym przez reguły haseł.

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

Cała aplikacja wymaga logowania. Po pierwszym starcie logujesz się na `/konto/logowanie` jako Administrator
z ustawień `FirstAdmin`. Własne hasło zmienia się na `/konto/zmiana-hasla` (inne zalogowane sesje tego Konta
kończą się). Zablokowanie Konta w trakcie sesji wylogowuje je przy następnym żądaniu.

Kolejne Konta zakłada się na `/konto/rejestracja`; nowe Konto czeka na zatwierdzenie
(stan Oczekuje). Dopóki nie ma ekranu Użytkownicy, Konto zatwierdza się w bazie:

```powershell
docker exec rejestr-czasu-db psql -U rejestr -d rejestr -c "UPDATE users SET status = 'active' WHERE user_name = '<login>';"
```

Potem logowanie na `/konto/logowanie`. Rejestracja i logowanie przyjmują najwyżej 10 prób (POST) na minutę z jednego
adresu IP; po 5 złych hasłach z rzędu logowanie na Konto jest wstrzymane na 15 minut.

### Wpis testowy (blokada usunięcia Projektu)

Dopóki nie ma formularza Wpisu, Wpis do ręcznego sprawdzenia blokady usunięcia Projektu (Słowniki → Projekty)
dodaje się w bazie. Najpierw `id` Konta:

```powershell
docker exec rejestr-czasu-db psql -U rejestr -d rejestr -c "SELECT id, user_name FROM users;"
```

Potem, po dodaniu Projektu na ekranie Projekty, polecenie dodaje testowy Typ pracy (z bieżącym czasem w nazwie, więc
można je powtarzać) i jeden Wpis z tym Projektem; zamień `<user_id>` i `<KOD>` (kod Projektu wielkimi literami):

```powershell
docker exec rejestr-czasu-db psql -U rejestr -d rejestr -c "WITH p AS (SELECT id, user_id FROM project WHERE user_id = <user_id> AND code = '<KOD>'), w AS (INSERT INTO work_type (user_id, name, is_active) SELECT user_id, 'Typ testowy ' || clock_timestamp(), true FROM p RETURNING id) INSERT INTO time_entry (user_id, date, project_id, work_type_id, description, minutes) SELECT p.user_id, DATE '2026-09-01', p.id, w.id, 'Wpis testowy', 60 FROM p, w;"
```

`INSERT 0 1` oznacza dodany Wpis (`INSERT 0 0` — nie ma takiego Projektu tego Konta; wtedy nie powstaje też Typ pracy). Usunięcie tego Projektu
na ekranie Projekty pokaże wtedy liczbę Wpisów i propozycję dezaktywacji, a Projekt zostanie na liście.

## Obraz aplikacji (Docker)

Na serwerze aplikacja działa jako obraz kontenera za Caddy (wdrożenie: historia 4.8). `Dockerfile` w katalogu głównym
buduje ją w SDK 10.0.403 i uruchamia na `mcr.microsoft.com/dotnet/aspnet:10.0` (z ICU i tzdata). Obraz nie zawiera
`appsettings.Local.json`, `.env` ani testów (`.dockerignore`), więc całą konfigurację dostaje ze zmiennych środowiskowych.

Lokalny próbny przebieg na bazie z `docker compose` (PowerShell, z katalogu głównego; hasło i nazwy z własnego `.env`):

```powershell
docker compose up -d
docker build --build-arg APP_IMAGE_TAG=test-lokalny -t rejestr-czasu:test .
docker run -d --name rejestr-app-test --network rejestr-czasu-bmad_default -p 127.0.0.1:8080:8080 `
  -e Database__Host=db -e Database__User=rejestr -e Database__Name=rejestr -e Database__Password=<POSTGRES_PASSWORD z .env> `
  -e FirstAdmin__UserName=<login> -e FirstAdmin__Password=<hasło> rejestr-czasu:test
curl http://127.0.0.1:8080/health
docker rm -f rejestr-app-test
```

- Sieć `rejestr-czasu-bmad_default` tworzy `docker compose` (nazwa katalogu repozytorium + `_default`); w niej baza
  ma nazwę `db`. Port aplikacji w kontenerze to zawsze `8080` (`ASPNETCORE_HTTP_PORTS`).
- `FirstAdmin__*` są potrzebne tylko, gdy w bazie nie ma jeszcze Administratora (jak przy `dotnet run`).
- Aplikacja otwiera się na `http://127.0.0.1:8080`. `GET /health` nie wymaga logowania i zwraca
  `{"status":"Healthy","tag":"test-lokalny"}` (200), a gdy baza nie odpowiada — 503 z tym samym kształtem.
  Bez `--build-arg APP_IMAGE_TAG` tag to `local`; tak samo przy `dotnet run`.
- Klucze Data Protection (ciasteczka logowania i formularzy) leżą w tabeli `data_protection_keys`, więc
  `docker restart rejestr-app-test` i nowy obraz na tej samej bazie nie wylogowują.
- Komunikat `Cannot load library libgssapi_krb5.so.2` w logu kontenera przy starcie jest nieszkodliwy: sterownik
  bazy sprawdza obsługę Kerberos, której aplikacja nie używa.

Umowa zmiennych dla serwera (`APP_IMAGE_TAG`, `ForwardedHeaders__KnownNetwork`) jest w
[`docs/admin/`](docs/admin/README.md#obraz-aplikacji-i-zmienne-serwera).

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
