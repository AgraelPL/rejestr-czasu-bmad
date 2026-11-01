# Instrukcja administratora

Ręczne kroki w GitHubie i na serwerze, których nie wykonuje potok ani aplikacja.

## Spis tematów

1. [Ochrona gałęzi `main`](#ochrona-gałęzi-main)
2. [Obraz aplikacji i zmienne serwera](#obraz-aplikacji-i-zmienne-serwera)

## Ochrona gałęzi `main`

Workflow `.github/workflows/pr.yml` uruchamia się na każdy pull request do `main` i ma jeden job
`build-and-test`: build, wszystkie testy (w tym kontrola migracji dodających), `dotnet ef migrations
has-pending-model-changes`. Reguła ochrony sprawia, że do `main` trafia tylko PR, w którym ten job jest zielony.

Regułę ustawia się raz, po pierwszym uruchomieniu workflow (GitHub podpowiada na liście tylko joby, które już
raz się wykonały — najpierw otwórz PR z gałęzi zawierającej `pr.yml` i poczekaj na wynik joba).

1. Otwórz repozytorium na GitHubie i przejdź do **Settings** → **Branches**.
2. W sekcji **Branch protection rules** kliknij **Add classic branch protection rule**.
3. W polu **Branch name pattern** wpisz `main`.
4. Zaznacz **Require a pull request before merging**. GitHub zaznacza wtedy domyślnie **Require approvals** —
   odznacz tę opcję. Autor pracuje sam i nie może zatwierdzić własnego PR, a z krokiem 6 nie dałoby się scalić
   żadnego PR.
5. Zaznacz **Require status checks to pass before merging**, a pod nim:
   - zaznacz **Require branches to be up to date before merging** (wymagana aktualność gałęzi względem `main`);
   - w polu wyszukiwania wpisz `build-and-test` i wybierz ten job z listy.
6. Zaznacz **Do not allow bypassing the above settings**, żeby reguła obowiązywała także administratora repozytorium.
7. Upewnij się, że **Allow force pushes** i **Allow deletions** są odznaczone.
8. Kliknij **Create** na dole strony.

Od tej chwili bezpośredni `git push` do `main` jest odrzucany, a przycisk scalenia PR jest zablokowany,
dopóki job `build-and-test` nie jest zielony i gałąź nie jest aktualna względem `main`.

### Sprawdzenie

- Na liście PR otwórz dowolny PR do `main`: w sekcji sprawdzeń widać `build-and-test` oznaczony jako **Required**.
- PR z czerwonym testem: przycisk **Merge pull request** jest nieaktywny, z komunikatem o wymaganym sprawdzeniu.
- `git push origin main` z lokalnego repozytorium kończy się błędem `protected branch hook declined`.

### Gdy zmienia się nazwa joba

Nazwa `build-and-test` w `pr.yml` i w regule muszą być takie same. Jeśli job zostanie przemianowany, reguła
czeka w nieskończoność na sprawdzenie, które już nie istnieje — wtedy w kroku 5 usuń starą nazwę i dodaj nową.

## Obraz aplikacji i zmienne serwera

Obraz buduje `Dockerfile` z katalogu głównego repozytorium (budowanie i próbny przebieg lokalnie: README,
sekcja „Obraz aplikacji (Docker)”). Na serwerze obraz uruchamia usługa `app` w `deploy/compose.yml` za Caddy
(historia 4.8); poniżej umowa zmiennych, którą ta historia musi spełnić.

### Zmienne aplikacji w kontenerze

| Zmienna | Skąd | Znaczenie |
| --- | --- | --- |
| `ASPNETCORE_HTTP_PORTS` | ustawiona w obrazie na `8080` | port HTTP aplikacji w kontenerze; Caddy przekazuje do `app:8080`. Nie publikować go na zewnątrz. |
| `APP_IMAGE_TAG` | `--build-arg APP_IMAGE_TAG=<pełny SHA commita>` przy budowaniu; na serwerze także w `.env` | tag obrazu, który zwraca `GET /health` (`{"status":"Healthy","tag":"<APP_IMAGE_TAG>"}`). Bez niej `local`. Workflow wdrożenia porównuje go z wdrażanym tagiem. |
| `ForwardedHeaders__KnownNetwork` | `.env` serwera | podsieć sieci compose, z której łączy się Caddy, w zapisie CIDR (np. `172.18.0.0/16`). Tylko z tej podsieci (i z loopback) aplikacja przyjmuje `X-Forwarded-For` i `X-Forwarded-Proto`: adres klienta dla limitu prób logowania i schemat `https`. |
| `Database__Host`, `Database__Name`, `Database__User`, `Database__Password` | `.env` serwera | połączenie z bazą; na serwerze `Database__Host=db`. |
| `FirstAdmin__UserName`, `FirstAdmin__Password` | `.env` serwera | pierwszy Administrator, używane tylko gdy w bazie nie ma żadnego Administratora. |

Stałej podsieci najlepiej nadać sieci compose jawnie (`ipam` w `deploy/compose.yml`), żeby `ForwardedHeaders__KnownNetwork`
nie zależała od kolejności tworzenia sieci przez Dockera. Sprawdzenie podsieci istniejącej sieci:

```bash
docker network inspect <nazwa-sieci> --format '{{range .IPAM.Config}}{{.Subnet}}{{end}}'
```

- Bez `ForwardedHeaders__KnownNetwork` aplikacja ufa nagłówkom tylko z loopback: za Caddy w innym kontenerze
  wszyscy klienci dzieliliby wtedy jeden limit 10 prób logowania na minutę (adres Caddy).
- Niepoprawna wartość (nie CIDR, np. `172.18.0.1` bez maski albo maska większa niż `/32` dla IPv4 lub `/128` dla IPv6) kończy start
  aplikacji kodem 1 i komunikatem o `ForwardedHeaders:KnownNetwork`, przed połączeniem z bazą.
- Nie ustawiaj `TZ`: aplikacja sama liczy czas polski (`Europe/Warsaw` z tzdata obrazu).

### Stan aplikacji: `GET /health`

Bez logowania, także dla Konta w trakcie wymuszonej zmiany hasła. Zwraca 200 z `"status":"Healthy"`, gdy aplikacja
łączy się z bazą, a 503 (status `Unhealthy`), gdy baza nie odpowiada; w obu przypadkach z polem `tag`.

### Klucze Data Protection

Klucze szyfrujące ciasteczka logowania leżą w bazie (tabela `data_protection_keys`) ze stałą nazwą aplikacji
`RejestrCzasu`. Restart kontenera, nowy obraz i rollback na tej samej bazie nie wylogowują Userów. Odtworzenie bazy
z kopii przywraca też klucze; nowa, pusta baza oznacza jednorazowe wylogowanie wszystkich.
