---
title: 'Wdrożenie na Droplet pod czas-pracy.kandula-consulting.pl'
type: 'feature'
ticket: '8'
created: '2026-11-01'
status: 'ready-for-dev'
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

**Problem:** Aplikacja działa tylko lokalnie. Merge do `main` niczego nie wdraża, a na serwerze nie ma ani układu usług, ani instrukcji przygotowania (R15, R27, R29).

**Approach:**
- pliki serwera: `deploy/compose.yml` i `deploy/Caddyfile`;
- workflow `.github/workflows/deploy.yml` po merge do `main`: obraz do GHCR, wdrożenie przez SSH, sprawdzenie `/health`;
- instrukcja `docs/admin/` od zera.

Autor przygotowuje Droplet, DNS i sekrety według instrukcji i sprawdza pierwsze wdrożenie. To wdrożenie zamraża migrację `Initial`, co egzekwuje test.

## Boundaries & Constraints

**Always:**
- **`deploy/compose.yml`:**
  - sieć domyślna z `ipam` `172.30.0.0/24`;
  - `caddy:2.11`: porty 80 i 443, wolumeny `/data` i `/config`, `SITE_DOMAIN` z `.env`;
  - `app`: obraz `ghcr.io/agraelpl/rejestr-czasu-bmad:${APP_IMAGE_TAG}`, bez portów, `env_file: .env`, `ForwardedHeaders__KnownNetwork=172.30.0.0/24`, `Database__Host=db`;
  - `db`: `postgres:18`, nazwany wolumen na dysku Dropletu, port tylko `127.0.0.1:5432`;
  - zależności: `app` po zdrowej `db` (healthcheck `pg_isready`);
  - każda usługa ma `restart: unless-stopped` i `logging` `json-file` z `max-size` i `max-file`;
  - jedna instancja `app` (AD-19).
- **`deploy/Caddyfile`:** `{$SITE_DOMAIN}` z `reverse_proxy app:8080`; automatyczne HTTPS Let's Encrypt.
- **`.github/workflows/deploy.yml`:**
  - `on: push` do `main`;
  - `concurrency` bez równoległych wdrożeń;
  - `permissions: contents: read, packages: write`;
  - job budujący: obraz z `Dockerfile` z `--build-arg APP_IMAGE_TAG=${{ github.sha }}`, push do GHCR z tagiem pełnego SHA, logowanie `GITHUB_TOKEN`;
  - job wdrażający (po budującym):
    - klucz SSH z `secrets.DEPLOY_SSH_KEY`, host z `secrets.DEPLOY_HOST`, użytkownik z `secrets.DEPLOY_USER`, `known_hosts` z `secrets.DEPLOY_KNOWN_HOSTS`; bez `StrictHostKeyChecking=no`;
    - kopiuje `deploy/compose.yml` i `deploy/Caddyfile` z tego commita do `/opt/rejestr-czasu`;
    - ustawia w `.env` serwera `APP_IMAGE_TAG=<sha>`; nie kopiuje `.env` i nie wypisuje jej zawartości;
    - uruchamia `docker compose pull && docker compose up -d`;
    - czeka do 60 s na `https://${{ vars.SITE_DOMAIN }}/health` ze statusem 200 i polem `tag` równym SHA; inaczej job kończy się błędem, bez automatycznego powrotu.
  - Tag i SHA są parametrami jobu wdrażającego (zmienna `IMAGE_TAG`), żeby historia 4.6 mogła ten sam krok uruchomić ręcznie z innym tagiem.
- **Zamrożenie `Initial` (AD-21)** w `tests/RejestrCzasu.Tests/MigrationRulesTests.cs`:
  - `Initial` rozpoznawana jako migracja o najwcześniejszym identyfikatorze z `[Migration]`, nie po nazwie klasy;
  - przypięty skrót SHA-256 pliku `Data/Migrations/*_Initial.cs` z końcami linii znormalizowanymi do `\n`;
  - zmiana pliku albo pojawienie się wcześniejszej migracji wywraca test z polskim komunikatem.
- **`docs/admin/README.md`** (nowe tematy w spisie, kolejność kroków jak niżej):
  1. założenie Dropletu FRA1 z włączonymi kopiami;
  2. Cloud Firewall 80/443/22;
  3. użytkownik wdrożeń w grupie `docker`, SSH tylko kluczem, osobny klucz dla potoku;
  4. rekord DNS A subdomeny;
  5. `/opt/rejestr-czasu/.env` z kontraktem AD-13 i `chmod 600`;
  6. GitHub Secrets (`DEPLOY_SSH_KEY`, `DEPLOY_HOST`, `DEPLOY_USER`, `DEPLOY_KNOWN_HOSTS`) i zmienna `SITE_DOMAIN`;
  7. pierwsze wdrożenie: pierwszy workflow (pull może się nie udać przy prywatnym pakiecie), przełączenie pakietu GHCR na publiczny, ponowne uruchomienie;
  8. rola `rejestr_ro` (tylko `SELECT`, także domyślne uprawnienia dla nowych tabel) i dostęp tunelem SSH;
  9. sprawdzenie portów z zewnątrz;
  10. ćwiczenie nieudanego wdrożenia (`docker compose stop db`, ponowne uruchomienie workflow, potem `start db`);
  11. zapytanie rolą `rejestr_ro` o role Administratora.
- **`.env.example`** dostaje sekcję zmiennych serwera z przykładowymi wartościami. Bez prawdziwych sekretów (NFR-2).

**Never:**
- Rollback przez `workflow_dispatch` (4.6) i automatyczny powrót do poprzedniej wersji.
- Sekrety w repozytorium, obrazie lub logu workflow; `set -x` w krokach z sekretami.
- Zmiana `pr.yml`, nowa migracja, zmiany aplikacji.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Wdrożenie | merge do `main`, serwer przygotowany | obraz `:<sha>` w GHCR, `/health` 200 z `tag` = sha, job zielony | — |
| Zdrowie nie przychodzi | baza zatrzymana | job czerwony po ≤ 60 s, na serwerze zostaje nowy `APP_IMAGE_TAG` | brak automatycznego powrotu |
| Zły tag w `/health` | odpowiada inny `tag` | job czerwony | komunikat z oczekiwanym i otrzymanym tagiem |
| Zmiana `Initial` | edycja `*_Initial.cs` | `dotnet test` czerwony | polski komunikat o zamrożonej `Initial` |
| Wcześniejsza migracja | migracja o id wcześniejszym niż `Initial` | `dotnet test` czerwony | komunikat |
| Konfiguracja compose | `docker compose -f deploy/compose.yml config` z przykładowym `.env` | poprawna konfiguracja, `app` bez `ports`, `db` tylko `127.0.0.1` | — |

</frozen-after-approval>

## Code Map

- `Dockerfile` (4.5): `ARG APP_IMAGE_TAG`, port 8080, użytkownik `$APP_UID`; kontekst budowania ogranicza `.dockerignore`.
- `.github/workflows/pr.yml`: wzorzec workflow (akcje `actions/checkout@v7`, `actions/setup-dotnet@v6`, `DOTNET_CLI_TELEMETRY_OPTOUT`); job `build-and-test` jest wymagany przez ochronę `main`, więc nie zmieniać nazwy.
- `docs/admin/README.md`:
  - spis tematów L5-8, „Ochrona gałęzi `main`”, „Obraz aplikacji i zmienne serwera” z tabelą zmiennych (L52-70; przykładowa podsieć `172.18.0.0/16` do zastąpienia `172.30.0.0/24`);
  - tematy `/health` i Data Protection.
- `.env.example`: dziś tylko zmienne lokalnego `docker-compose.yml`. Lokalny `docker-compose.yml` zostaje bez zmian.
- `tests/RejestrCzasu.Tests/MigrationRulesTests.cs` L12-27: dziś pomija `Initial` po nazwie klasy (`InitialMigrationName`). Migracja: `src/RejestrCzasu/Data/Migrations/20261031200135_Initial.cs`. Ścieżkę do pliku znaleźć od `AppContext.BaseDirectory` w górę, jak `ApplicationTests.FindApplicationDll`.
- Repozytorium `AgraelPL/rejestr-czasu-bmad`; nazwa obrazu w GHCR małymi literami (`ghcr.io/agraelpl/rejestr-czasu-bmad`).

## Tasks & Acceptance

**Execution:**
- [ ] `deploy/compose.yml`, `deploy/Caddyfile` -- usługi serwera -- AD-19, AD-2, AD-13.
- [ ] `.github/workflows/deploy.yml` -- budowanie i wdrożenie -- AD-20.
- [ ] `tests/RejestrCzasu.Tests/MigrationRulesTests.cs` -- zamrożenie `Initial` -- AD-21.
- [ ] `docs/admin/README.md`, `.env.example`, `README.md` (odnośnik do wdrożenia) -- instrukcja administratora -- R27.

**Acceptance Criteria:**
- Given wszystkie zmiany, when `dotnet build` i `dotnet test`, then 0 ostrzeżeń i wszystkie testy zielone.
- Given `deploy/compose.yml` i przykładowy `.env`, when `docker compose -f deploy/compose.yml --env-file <przykład> config`, then bez błędów.
- Given workflow, when `docker run --rm -v "$PWD:/repo" -w /repo rhysd/actionlint:latest`, then bez błędów dla `deploy.yml`.
- Given Autor przeszedł `docs/admin/` i zmergował zmianę, when workflow się kończy, then sprawdzenia ręczne z sekcji Verification przechodzą.

## Verification

**Commands:**
- `dotnet build` -- expected: 0 błędów, 0 ostrzeżeń
- `dotnet test` -- expected: wszystkie testy zielone
- `docker compose -f deploy/compose.yml --env-file .env.example config` -- expected: poprawna konfiguracja
- actionlint w kontenerze jak w Acceptance Criteria -- expected: bez błędów

**Manual checks (Autor, na serwerze, po merge):**
- `https://czas-pracy.kandula-consulting.pl` pokazuje stronę logowania z ważnym certyfikatem; `/health` daje 200 z pełnym SHA commita.
- Logowanie kontem pierwszego Administratora. Zapytanie rolą `rejestr_ro` pokazuje role Administrator i User.
- Nowa rejestracja daje Konto Oczekuje; 5 złych haseł blokuje logowanie; dobrowolna zmiana hasła nie wylogowuje.
- Skan z zewnątrz pokazuje tylko 22, 80 i 443; 5432 nie odpowiada.
- Ćwiczenie z zatrzymaną bazą daje czerwony workflow i nowy tag bez powrotu; po `start db` `/health` znowu daje 200.
- Log workflow nie zawiera sekretów.

Uruchomiona przez Autora aplikacja może blokować `src/RejestrCzasu/bin`. Wtedy build i testy uruchamiaj na kopii repozytorium w scratchpadzie i nie zatrzymuj jego procesu.

## Implementation Notes

## Plan Change Log

## Review Triage Log
