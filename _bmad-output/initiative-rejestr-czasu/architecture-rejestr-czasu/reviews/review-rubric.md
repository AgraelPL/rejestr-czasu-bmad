# Recenzja według listy kontrolnej — architecture-rejestr-czasu.md (aktualizacja 31.10.2026)

- Recenzowany dokument: `architecture-rejestr-czasu.md` (altitude: initiative, purpose: build-substrate)
- Ticket: `initiative-rejestr-czasu.md` (R1–R31, R14 wycofane; NFR-1…NFR-9)
- Źródła pomocnicze: `.memlog.md`, kod brownfield `src/RejestrCzasu` (historia 1.1)
- Data: 31.10.2026

## Werdykt

**Wymaga poprawek przed planowaniem historii epiku Konta i wdrożenie.** Kręgosłup jest dojrzały: pokrywa wszystkie wymagania ticketu, wersje są zweryfikowane w memlogu, a większość reguł da się egzekwować. Zostały jednak cztery punkty rozjazdu o wadze wysokiej, które trafią dokładnie w pierwsze historie (Identity w obwodzie, zmiana hasła w obwodzie, kontrakt konfiguracji kontenera, dostarczanie plików `deploy/` na serwer), i kilka średnich luk w otoczeniu operacyjnym i ratyfikacji kodu brownfield.

| Waga | Liczba |
| --- | --- |
| critical | 0 |
| high | 4 |
| medium | 8 |
| low | 7 |

## Wyniki

### H-1 — `UserManager`/`SignInManager` w obwodzie Interactive Server łamie AD-4; transakcja AD-18 nie jest atomowa (high)

- **Gdzie:** AD-4, AD-16 („Operacje na Kontach idą przez `UserManager`/`SignInManager`”), AD-17, AD-18, ekran `Features/Uzytkownicy`.
- **Problem:** `UserManager<T>` i jego store są scoped i trzymają `AppDbContext` z DI. W obwodzie Interactive Server scope żyje tyle co obwód, więc ekran Użytkownicy (interaktywny: `ReportGrid`, dialogi, `Confirm`) dostaje długo żyjący, śledzący `AppDbContext` — dokładnie to, czemu AD-4 zapobiega (nieaktualne dane, wyjątki współbieżności przy dwóch szybkich akcjach). Dodatkowo AD-18 wymaga „jednej transakcji: Wpisy → Słowniki → `account_settings` → `UserManager.DeleteAsync`”, ale nie mówi, że transakcja i `UserManager` muszą używać tego samego wystąpienia kontekstu. Dwie historie (epik Konta, epik Administracja) mogą to rozwiązać różnie, a naiwne rozwiązanie da usunięcie niepełne.
- **Poprawka:** dopisać regułę: komponent interaktywny wywołuje operacje Kont wyłącznie przez usługi `Accounts/` (np. `AccountAdmin`), które na każdą operację tworzą nowy scope (`IServiceScopeFactory.CreateAsyncScope()`) i z niego biorą `UserManager` oraz `AppDbContext`; `DeleteAccount` otwiera transakcję na kontekście tego scope'u, który jest też kontekstem store'a Identity. Zakaz wstrzykiwania `UserManager`/`SignInManager` do komponentów interaktywnych.

### H-2 — Zmiana własnego hasła w interaktywnych Ustawieniach konta wyloguje Usera (high)

- **Gdzie:** AD-8 (walidator „Ustawienia konta” przez Blazilla, czyli formularz interaktywny), AD-15 („każda zmiana hasła … aktualizuje security stamp”; „tylko odpowiedź HTTP ustawia ciasteczko”), AD-17 (rewalidacja co 1 min), diagram paradygmatu (`Features -.->|tylko Użytkownicy| Accounts`), R25.
- **Problem:** `ChangePasswordAsync` zmienia security stamp; odświeżenia ciasteczka (`RefreshSignInAsync`) nie da się zrobić z obwodu, więc najpóźniej po minucie rewalidacja zamknie sesję Usera, który właśnie zmienił hasło. Do tego diagram zabrania `Features/UstawieniaKonta` zależności od `Accounts/`, a bez niej ekran nie zmieni hasła. Szablon .NET trzyma zmianę hasła na statycznej stronie właśnie z tego powodu. Epik Widok miesiąca i Zestawienie (właściciel ekranu) i epik Konta rozjadą się tu na pewno.
- **Poprawka:** zdecydować jawnie: zmiana własnego hasła to statyczna strona `/konto/zmiana-hasla` (ta sama co wymuszona zmiana, AD-15), do której link prowadzi z `/ustawienia-konta`; interaktywne Ustawienia konta mają tylko prefiks. Uzupełnić AD-8 (lista formularzy) i diagram.

### H-3 — Brak kontraktu konfiguracji aplikacji w kontenerze; brownfield go łamie (high)

- **Gdzie:** AD-2, AD-13, AD-19, AD-20, AD-21 krok 3; brownfield `appsettings.json` (`"Urls": "http://127.0.0.1:5080"`, sekcja `Database` z `Host: 127.0.0.1`), `Options/DatabaseOptions.cs`.
- **Problem:** kręgosłup nie ustala: (a) adresu i portu nasłuchu aplikacji w kontenerze (brownfield nasłuchuje na `127.0.0.1`, więc Caddy z innego kontenera się nie połączy), ani portu w `reverse_proxy`; (b) nazw zmiennych środowiskowych, które aplikacja czyta z `env_file` (`Database__Host=db`, `Database__Password`, klucze loginu i hasła pierwszego Administratora), i tego, jak to samo hasło trafia jednocześnie do `POSTGRES_PASSWORD` usługi `db` i do aplikacji; (c) `ASPNETCORE_ENVIRONMENT` na serwerze; (d) nazwy zmiennej z tagiem obrazu w `.env`, którą zmienia potok. Historie Dockerfile, `deploy/compose.yml`, pierwszego Administratora i workflow wdrożenia wybiorą to niezależnie.
- **Poprawka:** dodać do AD-13 lub AD-19 tabelę kontraktu: port kontenera (np. `ASPNETCORE_HTTP_PORTS=8080`, `Urls` z `appsettings.json` usunięte lub nadpisane), lista kluczy `.env` serwera z ich odbiorcami (`db`, `app`, potok), nazwa sekcji pierwszego Administratora, zmienna tagu (np. `APP_IMAGE_TAG`).

### H-4 — Nie wiadomo, jak `deploy/compose.yml` i `deploy/Caddyfile` trafiają na Droplet (high)

- **Gdzie:** AD-19, AD-20 („Jedyne ręczne kroki na serwerze to przygotowanie opisane w `docs/admin/`”), R29, SM-4.
- **Problem:** pliki leżą w repozytorium, a potok robi tylko `docker compose pull && up -d` w `/opt/rejestr-czasu`. Kręgosłup nie mówi, czy potok kopiuje `deploy/*` na serwer przy każdym wdrożeniu, czy są one kopiowane ręcznie przy przygotowaniu. W drugim przypadku każda zmiana compose'a lub Caddyfile wymaga ręcznego kroku na serwerze, co łamie R29 i SM-4; rollback (AD-20) nie cofa też tych plików.
- **Poprawka:** dopisać do AD-20: workflow wdrożenia kopiuje `deploy/compose.yml` i `deploy/Caddyfile` z commita odpowiadającego tagowi (scp/rsync przez SSH) przed `pull`/`up`; rollback robi to samo dla wskazanego tagu; `.env` jest jedynym plikiem tylko na serwerze.

### M-1 — Przeniesienie `Components/` i sprzątanie brownfield nie są nazwane (medium)

- **Gdzie:** Design Paradigm (tabela katalogów), Structural Seed (drzewo), brownfield `Components/` (`App.razor`, `Routes.razor`, `_Imports.razor`, `Layout/MainLayout.razor`, `Pages/Error.razor`, `Pages/Home.razor`, `Pages/NotFound.razor`), `Options/` (`DatabaseOptions`, `BackupOptions`, `ExportOptions`), `SecurityHeaders.cs`, komentarze „AD-3” w `Program.cs` i `Startup/DatabaseStartup.cs`.
- **Problem:** kręgosłup opisuje nowy układ (`Features/`, `Shared/`, `Accounts/`), ale nigdzie nie mówi, że `Components/` jest przenoszone, ani gdzie lądują `App.razor`, `Routes.razor`, `_Imports.razor`, `Error`, `NotFound`. `Options/` i `SecurityHeaders.cs` nie występują w drzewie, choć `DatabaseOptions` zostaje. `BackupOptions` i krok kopii w `DatabaseStartup` wynikają z wycofanego AD-3, a `ExportOptions.Prefix` przeczy AD-13 („prefiks … nie konfiguracją”) — usunięcie obu jest tylko domniemane. Agent może zostawić stare katalogi obok nowych.
- **Poprawka:** jedno zdanie w Structural Seed: „`Components/` → `Shared/` (layout, `App`, `Routes`, `Error`, `NotFound`), `Options/` zostaje dla `DatabaseOptions`, `BackupOptions` i `ExportOptions` usuwa historia epiku Konta i wdrożenie”; dopisać `Options/` i `SecurityHeaders.cs` do drzewa (lub wskazać ich nowe miejsce).

### M-2 — Kto tworzy role i czy pierwszy Administrator ma obie role (medium)

- **Gdzie:** AD-7, AD-15, AD-21 krok 3, R20.
- **Problem:** memlog mówi, że role `Administrator` i `User` powstają w migracji `Initial`, ale AD-7 tego nie zapisuje. AD-21 krok 3 tworzy „Konto `active`”, bez ról, a R20 wymaga, żeby Konto Autora miało role User i Administrator. Dwie historie (Initial, Startup) mogą obie założyć role albo żadna.
- **Poprawka:** w AD-7 „role `Administrator` i `User` jako dane w migracji `Initial`” (lub w Startup, ale jednoznacznie); w AD-21 krok 3 „z rolami `Administrator` i `User`”.

### M-3 — Właściciel wiersza `account_settings` nieustalony (medium)

- **Gdzie:** AD-7, AD-14, AD-16, R13, R25.
- **Problem:** nie wiadomo, czy wiersz powstaje przy rejestracji (epik Konta), przy pierwszym zapisie Ustawień konta (epik Widok miesiąca i Zestawienie), czy brak wiersza znaczy „bez prefiksu”. Endpoint Zestawienia, rejestracja, pierwszy Administrator i `DeleteAccount` muszą przyjąć to samo.
- **Poprawka:** jedno zdanie w AD-7: np. „wiersz powstaje przy pierwszym zapisie Ustawień konta; brak wiersza = brak prefiksu”.

### M-4 — Skąd `CurrentUser` bierze Usera w obwodzie, w endpoincie i na stronach SSR (medium)

- **Gdzie:** AD-16 („`UserDbContextFactory` (scoped w obwodzie)”), AD-17, AD-14.
- **Problem:** w obwodzie User pochodzi z `AuthenticationStateProvider`, w endpoincie Minimal API z `HttpContext.User` (tam `AuthenticationStateProvider` nie jest wiarygodny). Reguła nie mówi, jak jedna klasa obsłuży oba hosty, więc endpoint Zestawienia (epik 4) i ekrany (epik 1–3) mogą zbudować dwie wersje `CurrentUser`.
- **Poprawka:** zapisać źródło: `CurrentUser` czyta `AuthenticationStateProvider` w komponentach i `IHttpContextAccessor` poza obwodem (albo endpoint woła wariant z jawnym `ClaimsPrincipal`); oba ścieżki sprawdzają `status` tak samo.

### M-5 — `/konto/wyloguj`: POST w AD-11, przejście nawigacją w AD-17 (medium)

- **Gdzie:** AD-11 (Wyloguj = formularz POST), AD-17 („UI przechodzi na `/konto/wyloguj`”).
- **Problem:** z obwodu nie da się wysłać POST bez JS ani formularza; `NavigateTo(..., forceLoad)` wysyła GET. Nie ustalono, czy `/konto/wyloguj` obsługuje GET (np. strona z automatycznym formularzem lub wylogowanie na GET dla Konta nieaktywnego). Dwie historie zrobią to różnie albo wcale nie zadziała.
- **Poprawka:** dopisać do AD-15/AD-17: Konto nieaktywne przechodzi na GET `/konto/zablokowane` (statyczna, wylogowuje w odpowiedzi HTTP) albo wprost: `/konto/wyloguj` przyjmuje GET i POST, z antiforgery na POST.

### M-6 — Brakujące pozycje w Stack (medium)

- **Gdzie:** Stack, AD-19 (`PersistKeysToDbContext`), AD-20 (budowanie obrazu).
- **Problem:** `PersistKeysToDbContext` wymaga pakietu `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` (10.0.x), którego nie ma w tabeli; brak też obrazu budującego `mcr.microsoft.com/dotnet/sdk:10.0` (wieloetapowy Dockerfile). Agent dobierze wersję sam.
- **Poprawka:** dopisać oba wiersze z wersjami zgodnymi z linią 10.0.14.

### M-7 — NFR-8 przekazuje wartości bezpieczeństwa architekturze, a kręgosłup je odkłada (medium)

- **Gdzie:** Deferred („Wartości limitów prób rejestracji i logowania, reguły długości hasła, czas życia sesji”), AD-15, NFR-8 („progi, reguły haseł i czas sesji to bezpieczne wartości domyślne z architektury”).
- **Problem:** ticket oczekuje wartości z architektury; kręgosłup oddaje je historii. Ryzyko rozjazdu jest małe (jedno miejsce konfiguracji), ale wymaganie formalnie nie jest pokryte, a weryfikacja historii nie ma progu odniesienia.
- **Poprawka:** wpisać wartości do AD-15 (np. limiter: 5 prób/min na IP dla logowania, 3/godz. dla rejestracji; hasło: domyślne Identity z `RequiredLength = 10`; ciasteczko 8 h z przesuwnym wygaśnięciem) albo podnieść to jako jawne pytanie do Autora i poprawić NFR-8.

### M-8 — Luki w otoczeniu operacyjnym (medium)

- **Gdzie:** AD-19, AD-20, Consistency Conventions (Logi), Deferred.
- **Problem:** decyzje są, ale brakuje kilku elementów, które historie wdrożenia ustalą osobno:
  - rotacja logów Dockera (domyślny `json-file` bez limitu zapełni dysk małego Dropletu);
  - kto i kiedy tworzy rolę bazy tylko do odczytu (migracja, skrypt w `docs/admin/`, ręcznie);
  - okno oczekiwania na `/health` po `up -d` (migracje trwają; bez ponawiania potok będzie fałszywie czerwony) i czy `/health` jest wołane przez Caddy po HTTPS, czy lokalnie na Droplecie;
  - aktualizacje systemu i Dockera na Droplecie (np. `unattended-upgrades`) — decyzja albo jawne odłożenie;
  - czy aplikacja łączy się z bazą jako superużytkownik `POSTGRES_USER`, czy osobną rolą właściciela schematu.
- **Poprawka:** dopisać do AD-19/AD-20: `logging: max-size/max-file` w `deploy/compose.yml`; właściciel roli tylko do odczytu; `/health` z ponawianiem do N sekund; pozostałe jako Deferred z właścicielem `docs/admin/`.

### L-1 — Rationale w regułach (low)

- AD-15 „bo tylko odpowiedź HTTP ustawia ciasteczko”, AD-19 „więc wdrożenie nie wylogowuje”, AD-21 „Dzięki temu poprzedni obraz działa na nowszym schemacie i rollback nie cofa bazy”, Deferred „odłożone przez Autora”. Uzasadnienia są w memlogu; w kręgosłupie wystarczy reguła. Drobne, nie blokuje.

### L-2 — `last_login_at` i `registered_at`: kto zapisuje (low)

- AD-7 definiuje kolumny, AD-15 nie mówi, że strona logowania ustawia `last_login_at` po udanym logowaniu, a rejestracja i pierwszy Administrator — `registered_at`. Jedno zdanie w AD-15.

### L-3 — Równoległe wdrożenia (low)

- AD-20 nie ustala `concurrency` dla workflow wdrożenia; dwa szybkie merge'e lub merge z rollbackiem mogą się przeplatać. Dopisać „jedna grupa `concurrency` dla wdrożenia i rollbacku, bez anulowania trwającego”.

### L-4 — Endpoint Zestawienia dla pustego Miesiąca (low)

- AD-14 każe ekranowi sprawdzić Wpisy przed pokazaniem linku, ale nie mówi, co zwraca endpoint wywołany bezpośrednio dla pustego Miesiąca (pusty plik, 404, przekierowanie). Jedno słowo w regule.

### L-5 — Region Dropletu a R16 (low)

- R16 (10 s na Wpis przez internet) zależy od opóźnienia obwodu Blazor Server; Deferred wymienia rozmiar Dropletu, nie region. Dopisać „region UE (FRA1/AMS3)” do decyzji Autora w `docs/admin/`.

### L-6 — Capability Map bez NFR-2…NFR-9 (low)

- Mapa ma tylko NFR-1; NFR-2 (AD-13, AD-20), NFR-7 (AD-6), NFR-8 (AD-15, AD-19), NFR-9 (AD-7, AD-21) są pokryte w regułach, ale nie w mapie.

### L-7 — Nieaktualne ograniczenie w tickecie (low, do Sofii)

- `initiative-rejestr-czasu.md`, Constraints: „kręgosłup … (AD-1…AD-14) wiąże każdy epik, poza AD-2 i AD-3, które PRD … zastępuje do czasu aktualizacji architektury” oraz notatka „Architecture invalidated (31.10.2026)”. Po tej aktualizacji ograniczenie i notatka są nieaktualne (AD-1…AD-22, AD-3 wycofane). To poprawka ticketu, nie kręgosłupa.

## Sprawdzenia, które przeszły

- **Pokrycie ticketu:** `binds` obejmuje R1–R31 bez R14; każde R ma AD w regułach lub w mapie; NFR-1 (AD-12, AD-19), NFR-2 (AD-13), NFR-4 (AD-2, AD-19), NFR-5/7 (AD-6), NFR-6 (AD-11), NFR-8 (AD-15, AD-19 — z zastrzeżeniem M-7), NFR-9 (AD-7, AD-21). Granice (brak e-maila, kodu kopii, CSV, środowiska przedprodukcyjnego) są zachowane.
- **Egzekwowalność:** AD-5, AD-6, AD-7 (złożone klucze obce, indeksy), AD-9, AD-10, AD-12 (lista zakazanych komponentów), AD-16 (dwa nazwane miejsca `IgnoreQueryFilters` z testami), AD-21 (kontrola migracji w PR) dają się sprawdzić testem, grepem lub w przeglądzie.
- **Deferred:** żadna pozycja nie pozwala dwóm jednostkom rozjechać się w kodzie (wygląd ekranów spinają AD-9/10/11/22; wartości bezpieczeństwa mają jedno miejsce — zob. M-7 co do pokrycia).
- **Wersje:** wszystkie pozycje Stack mają wpis weryfikacji z 31.10.2026 w memlogu; brownfield (`RejestrCzasu.csproj`, testy) używa tych samych wersji (EF/Identity 10.0.14, Npgsql 10.0.3, NamingConventions 10.0.1, Fluent UI 5.0.1, bUnit 2.13.3, xunit.v3 4.0.2, Testcontainers 4.16.0).
- **Brownfield ratyfikowany:** `docker-compose.yml` (`postgres:18`, nazwany wolumen na `/var/lib/postgresql`, `127.0.0.1:${POSTGRES_PORT}`), `appsettings.Local.json` ładowany jawnie, kultura `pl-PL`, `TimeProvider`, CSP `script-src 'self'` na każdej odpowiedzi, kolejność startu z wyjściem kodem 1, `AddDbContextFactory`, normalizacja kodu w `SaveChanges`, indeks `lower(name)` SQL-em, `RESTRICT`, testy Testcontainers z `Migrate()` — zgodne z AD-1, AD-2, AD-6, AD-7, AD-12, AD-13, AD-21. Przepisanie `Initial` (AD-7) jest stwierdzone jawnie. Wyjątki: M-1 (układ katalogów i opcje), H-3 (`Urls` na `127.0.0.1`).
- **Otoczenie operacyjne:** środowiska (lokalne + serwer, bez przedprodukcji), infrastruktura (Droplet, Cloud Firewall, Caddy, GHCR), sekrety (`.env` 600, GitHub Secrets), kopie (kopie Dropletu, odtworzenie na nowy Droplet, ćwiczenie w Deferred z właścicielem), obserwowalność (konsola + `/health`, monitoring jawnie poza v1) — zdecydowane lub jawnie odłożone; luki w H-3, H-4, M-8.
