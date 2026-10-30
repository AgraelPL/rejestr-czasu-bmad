# Przegląd spine'u architektury — Rejestr czasu pracy

- **Przeglądany dokument:** `architecture-rejestr-czasu.md` (status: draft, 2026-10-30)
- **Źródła:** `initiative-rejestr-czasu.md`, `glossary.md`, `export-layout.md`, `.memlog.md` (dziennik decyzji)
- **Data przeglądu:** 2026-10-30
- **Zakres:** Część A: zgodność z ticketem. Część B: lista kontrolna dobrego spine'u. Spine nie był edytowany.

## Werdykt

Spine trafnie ustala główne punkty rozjazdu: jedno źródło sum, minuty jako `int`, krótko żyjący `DbContext`, integralność w bazie, jedną siatkę i jeden sposób konfiguracji. Świadome odstępstwa od ticketu są poprawnie zapisane w memlogu. Brakuje jednak decyzji o wspólnym formularzu Wpisu i stanie Sesji wpisywania, z których korzystają trzy epiki. R16 (sama klawiatura, 10 s) i R2 (dezaktywacja) nie mają żadnej reguły. AD-13 (pobranie pliku) jest sprzeczne z AD-1 (bez JavaScriptu). Część kopertu operacyjnego (środowisko uruchomieniowe, ładowanie `appsettings.Local.json`, hasło w `docker-compose.yml`, właściciel `Startup/`) jest otwarta, choć spine o tym nie mówi. **Ocena: do poprawy przed rozpisaniem epików. Potrzebne są 3–4 nowe lub rozszerzone AD; przebudowa nie jest potrzebna.**

---

## Część A — Zgodność z ticketem

### A1. Wymagania pominięte lub naruszone BEZ zapisanej decyzji

| # | Wymaganie | Co mówi spine | Problem | Waga |
|---|---|---|---|---|
| A1.1 | **R16**: każdy kolejny Wpis ≤ 10 s, **wyłącznie klawiaturą** | Mapa przypisuje R16 do `Features/Wpisy` z AD-4/6/8/10. Żadne z tych AD nie mówi o klawiaturze ani o czasie. | Nie ma reguły dla kolejności fokusu, skrótu zapisu (np. Enter / Ctrl+Enter), powrotu fokusu na pierwsze pole po zapisie ani dla sposobu wpisywania Czasu (pole tekstowe `h:mm` czy pickery). AD-10 („operacje istotne → `MessageBox`”) nie wyklucza, że zapis Wpisu zostanie uznany za istotny i będzie wymagał kliknięcia w okno, co łamie R16. Pickery Fluent UI (data, combobox) mają różną obsługę klawiatury i tę różnicę trzeba rozstrzygnąć architektonicznie. | **wysoka** |
| A1.2 | **R5**: formularz zostaje otwarty, data zachowana, pozostałe pola czyszczone; domyślnie dzisiejsza data | Brak | Stan Sesji wpisywania (bieżąca data formularza) nie ma właściciela. R7 („data ustawiona w formularzu”) zakłada, że ten stan jest dostępny także przy Kopii z Listy wpisów i Widoku miesiąca. Nie ustalono, gdzie stan żyje (np. usługa `Scoped` na obwód), ani czy „dziś” to `DateTime.Now` (czas lokalny), a nie `UtcNow`. | **wysoka** (razem z B1.1) |
| A1.3 | **R2**: dezaktywacja; nieaktywne znikają z list wyboru, ale zostają przy edycji istniejącego Wpisu, w filtrach, Widoku i Zestawieniu | Brak. Diagram ER nie ma atrybutów, nie ma kolumny aktywności ani reguły zapytań. | Każdy epik sam zdecyduje, czy filtrować `is_active`. Zbyt gorliwy filtr w Widoku miesiąca albo w eksporcie wytnie Wpisy i złamie R10 lub R12. Lista wyboru przy edycji musi zawierać „aktywne + bieżącą wartość Wpisu”, a R7 wymaga pustego pola przy nieaktywnej. To ta sama reguła w trzech miejscach. | **średnia** |
| A1.4 | **NFR-4**: interfejs **tylko po polsku** | AD-6 ustawia kulturę `pl-PL` (liczby, dni). | Fluent UI Blazor ma wbudowane teksty po angielsku (puste stany siatki, popover filtra, paginacja, kalendarz, przyciski `MessageBox`). Kultura `pl-PL` ich nie tłumaczy. Trzeba ustalić jeden `IFluentLocalizer` (lub odpowiednik w v5) w `Shared/`. Bez tego każdy epik będzie nadpisywał teksty po swojemu albo zostawi angielskie. | **średnia** |
| A1.5 | **R9**: edycja, kopia i usunięcie Wpisu **przeliczają sumy od razu** | AD-9 daje przycisk Odśwież. AD-5 daje `MonthReport.Build`. | Nie ustalono, jak ekran dowiaduje się o zmianie wykonanej w formularzu (callback z okna, ponowne `Build` po zamknięciu, zdarzenie). Jeśli formularz otwiera się z Listy, Widoku i menu, każdy epik może to zrobić inaczej. Przycisk Odśwież nie spełnia słów „od razu”. | **średnia** |
| A1.6 | **R11**: opisy złączanych Wpisów „w kolejności dodania” | AD-5: kolejność liczy `MonthReport`. Konwencja: `id` z sekwencji. | Nie zapisano, że kluczem kolejności dodania jest `id` (lub `created_at`) i że typ domeny `Wpis` w `Reporting/` musi go mieć. Sortowanie po dacie lub opisie złamie R11 bez widocznego błędu. | **niska** |
| A1.7 | **R9 / export-layout**: w dniu według kodu Projektu, potem **nazwy** Typu pracy | AD-5 (kolejność w `MonthReport`) | Nie ustalono porównania tekstów: kulturowe `pl-PL` czy porządkowe. Ma to znaczenie dla nazw z polskimi znakami. Jeśli Widok i eksport idą przez `MonthReport`, ryzyko rozjazdu jest małe, ale reguła powinna to powiedzieć wprost. | **niska** |
| A1.8 | **R12**: Zestawienie „jednym kliknięciem **w Widoku miesiąca**” | AD-10: pozycja „Eksport” w menu bocznym, w sekcji Rejestr (memlog, wzorzec UI). | Pozycja menu „Eksport” nie jest sprzeczna z R12, ale spine nie mówi, że przycisk eksportu jest też w Widoku miesiąca. Nie mówi też, czym jest ekran „Eksport”: wyborem Miesiąca czy kolejnym klikiem. Bez tego R12 może zostać zrealizowany tylko przez menu, czyli dwoma kliknięciami. | **niska / średnia** |
| A1.9 | **NFR-2**: źródłowy xlsx i pliki prywatne poza gitem | AD-12 obejmuje konfigurację i dane testowe. | Nie ma wzorców `.gitignore` dla `*.xlsx` (źródłowy oryginał, pobrane Zestawienia) ani dla katalogu kopii, gdyby ktoś ustawił go w repo. Hasło do PostgreSQL w `docker-compose.yml` jest opisane w B3.3. | **niska** |
| A1.10 | **NFR-1**: zero połączeń wychodzących | AD-11 | Reguła jest dobra, ale nie ma jak jej sprawdzić (patrz B2). Pobranie obrazu `postgres:18` przy pierwszym `docker compose up` wymaga internetu. To jednorazowe działanie środowiska, nie aplikacji, ale warto je nazwać, żeby nie było sporu. | **niska** |
| A1.11 | **NFR-2 / prywatność logów** | Konwencja: `ILogger` do konsoli | Nie ma zakazu `EnableSensitiveDataLogging` ani logowania treści opisów Wpisów. Treść jest lokalna, ale ograniczenie warto zapisać jednym zdaniem. | **niska** |

Bez uwag (pokryte): R1 (AD-7, AD-8), R3 (AD-7: `RESTRICT` i liczba z zapytania), R4 (AD-6, AD-8), R6 (AD-10: potwierdzenie usunięcia), R8 suma (wyjątek w AD-5), R10 (AD-5), R11 złączanie (AD-5), R12 sumy jako wartości (AD-13), R13 prefiks w konfiguracji (AD-12), R14 kolejność i rotacja (AD-3), NFR-1 nasłuch na `127.0.0.1` (AD-2), NFR-5 (AD-6), terminy ze słownika (konwencja nazewnictwa).

### A2. Świadome odstępstwa zapisane w memlogu → „wymaga aktualizacji ticketu”

To nie są luki spine'u. Ticket (i PRD) trzeba z nimi zrównać:

1. **Stos:** ograniczenie z addendum „Node.js” → .NET 10 / Blazor Web App Interactive Server / Fluent UI Blazor 5 / FluentValidation / EF Core (memlog l.14).
2. **Baza:** „SQLite w jednym pliku” → PostgreSQL 18 w kontenerze Docker, dane w nazwanym wolumenie (memlog l.15, l.21).
3. **Uruchomienie, NFR-4 i Done when SM-2:** `npm start` → `docker compose up -d` + `dotnet run`. Docker Desktop staje się nowym wymaganiem wstępnym.
4. **R14:** nazwa kopii `.sqlite` → `rejestr-RRRR-MM-DD_GGMMSS.sql`, tworzona przez `pg_dump` (memlog l.46).
5. **R15:** „ścieżka bazy z konfiguracji” nie ma już odpowiednika. Baza żyje w wolumenie Dockera (w praktyce w VHDX Docker Desktop w `%LOCALAPPDATA%`), a z konfiguracji pochodzą parametry połączenia i nazwa kontenera. Ticket powinien to powiedzieć.
6. **Siatka:** filtry wartości w każdej kolumnie, okruszki, przycisk Odśwież, licznik i Widok miesiąca jako siatka grupowana Tydzień → Dzień wykraczają poza R8 i R9 (memlog l.31, l.43–45).
7. **UI:** menu boczne z pozycją „Eksport”, sekcja Administracja, skala 90–125% i motyw jasny/ciemny (memlog l.22–23). Ticket ich nie zna. Warto rozstrzygnąć relację „Eksport” w menu z R12 (A1.8).

---

## Część B — Lista kontrolna

### B1. Czy spine ustala prawdziwe punkty rozjazdu dla epików i żadnego nie pomija?

Ścieżka epików z ticketu: **Słowniki → Wpis w Sesji wpisywania → Widok miesiąca z sumami → Zestawienie xlsx**. Do tego dochodzi praca przekrojowa: scaffold, `Startup/` i `ReportGrid`.

**Dobrze ustalone:** stos (AD-1), dostęp do bazy (AD-4), jedno źródło sum (AD-5, najważniejsze dla R10), reprezentacja Czasu i dat (AD-6), duplikaty i usuwanie (AD-7), wzorzec walidacji (AD-8), jedna siatka (AD-9), wzorzec komunikatów (AD-10), konfiguracja (AD-12), biblioteka xlsx (AD-13).

**Pominięte punkty rozjazdu:**

| # | Punkt rozjazdu | Dlaczego to rozjazd między epikami | Waga |
|---|---|---|---|
| B1.1 | **Wspólny formularz Wpisu (dodaj / edytuj / kopiuj / usuń) i stan Sesji wpisywania** | Formularz buduje epik 2 (Wpis). Epik 3 (Widok miesiąca, R9) i Lista wpisów (R8) muszą z niego korzystać przy edycji, kopii i usuwaniu, R6–R7 także. Spine nie ustala, czy to jeden komponent (okno dialogowe / panel / strona z trasą), jak się go otwiera z innych ekranów, gdzie żyje data Sesji i jak ekran-wywołujący dostaje sygnał zmiany (A1.5). „Wygląd ekranów” w Deferred przerzuca to na historie, a historie różnych epików rozstrzygną to różnie. | **wysoka** |
| B1.2 | **Formatowanie i parsowanie Czasu `h:mm`** | AD-6 ustala przechowywanie (`int minutes`), ale nie mówi, kto zamienia `"1:30"` ↔ 90 i 90 → `"1:30"` lub `1,5`. Formularz (epik 2), Lista i Widok (epik 3), eksport (epik 4) oraz walidator (AD-8) potrzebują tej samej funkcji. Diagram paradygmatu pokazuje typ domeny „Czas”, a konwencje mówią `int minutes`. Nie wiadomo, czy `Czas` jest typem wartościowym, czy liczbą całkowitą z funkcjami pomocniczymi. Typowe rozjazdy: `"0:15"` vs `"00:15"`, `"24:00"` vs `"1.00:00"`, kropka vs przecinek. | **wysoka / średnia** |
| B1.3 | **Model danych i flaga aktywności** | Diagram ER nie ma atrybutów. Nie ustalono nazw i typów kolumn (`is_active`, długość `description`, `created_at`?), ani tego, kto tworzy pierwszą migrację i jak kolejne epiki dodają swoje. Przy `Database.Migrate()` przy starcie (AD-3) kolizje migracji z równoległych historii są realnym ryzykiem. Reguła list wyboru z R2 opisana w A1.3. | **średnia** |
| B1.4 | **Właściciel i czas powstania `ReportGrid<T>` oraz `Startup/`** | Memlog: siatka „powstaje przed pierwszym raportem”. AD-9 używa jej też na ekranach Administracji, czyli w epiku 1. `Startup/` (AD-3, R14) i README odtwarzania („przy epiku startu”) nie mają epiku na ścieżce z ticketu. Spine powinien przypisać jedno i drugie do epiku 1 (scaffold), inaczej pierwszy epik zrobi prowizoryczną siatkę. | **średnia** |
| B1.5 | **Lokalizacja tekstów Fluent UI** | Patrz A1.4. Jeden mechanizm w `Shared/`. | **średnia** |
| B1.6 | **Granica błędu w obwodzie Blazor Server** | Konwencja „nieobsłużony wyjątek → `MessageBar`” nie zadziała bez `ErrorBoundary` w układzie (lub w ekranach). Bez niej nieobsłużony wyjątek zrywa obwód. Trzeba ustalić jedno miejsce. | **niska / średnia** |
| B1.7 | **Trasy i nazwy stron** | Menu (AD-10) linkuje do ekranów z różnych epików. Brak konwencji tras (np. `/miesiac/{rok}/{miesiac}`, `/wpisy`, `/administracja/projekty`) i nazewnictwa folderów (`Features/Miesiac` po polsku przy kodzie po angielsku — konwencje są tu niespójne). | **niska** |

### B2. Czy każda reguła AD jest egzekwowalna i faktycznie zapobiega swojemu rozjazdowi?

| AD | Egzekwowalna? | Zapobiega? | Uwagi |
|---|---|---|---|
| AD-1 | częściowo | tak | „Bez JavaScriptu poza Fluent UI” jest sprzeczne z samym Blazorem (`blazor.web.js`) i z **AD-13**: pobranie pliku w Blazor Server wymaga albo JS interop (`DotNetStreamReference` + skrypt), albo osobnego endpointu HTTP (minimal API `GET` zwracający `File(...)`). Spine tego nie rozstrzyga. Pierwsza historia eksportu albo złamie AD-1, albo wymyśli obejście. **Wysoka.** Rekomendacja: endpoint minimal API, link `<a href download>`, zero własnego JS. |
| AD-2 | tak | tak | Nasłuch na `127.0.0.1` warto przypiąć w `appsettings.json` (`Kestrel:Endpoints` / `Urls`), nie w `launchSettings.json`, które może być pominięte. Hasło bazy: patrz B3.3. |
| AD-3 | tak | tak | Dobra, konkretna, testowalna reguła. Braki: (a) testy integracyjne przez `WebApplicationFactory` uruchomią krok 2 (`docker exec` na nieistniejącym kontenerze). Potrzebny jest przełącznik w konfiguracji albo wydzielenie kroków startu. (b) `docker` musi być w `PATH`. (c) Nie ma żadnej kontroli, że zrzut da się odtworzyć (B3.4). |
| AD-4 | tak (przegląd kodu) | tak | Bez uwag. |
| AD-5 | tak | **częściowo** | AD-9 daje `ReportGrid` „stałe grupowanie… z sumą w nagłówku grupy”. Ogólny komponent z funkcją sumowania kusi, żeby liczył sumy sam, co daje drugą implementację, którą AD-5 ma wykluczyć. Reguła powinna brzmieć: *`ReportGrid` nigdy nie sumuje; sumy grup przyjmuje jako dane od ekranu*. Wyjątek R8 (suma Listy) warto też przypisać do jednego miejsca (zapytanie albo `Reporting/`). |
| AD-6 | tak | tak | Należy dodać: „dziś” = `DateOnly.FromDateTime(DateTime.Now)`. Brakuje funkcji formatu i parsowania `h:mm` (B1.2). Nazwy miesięcy z `pl-PL` są małą literą. Nagłówek arkusza „Wrzesień 2026” i „RAZEM WRZESIEŃ 2026” wymagają jawnej zmiany wielkości liter. To szczegół historii, ale dobrze go wskazać. |
| AD-7 | tak | tak | `lower(name)` dla polskich znaków zależy od `LC_CTYPE` bazy. Domyślny obraz `postgres` (`en_US.utf8`) działa poprawnie, ale warto przypiąć to w `docker-compose.yml`. Rozpoznawanie duplikatu po `SqlState 23505` i nazwie indeksu warto ująć w jednej funkcji pomocniczej, nie w każdym ekranie osobno. |
| AD-8 | częściowo | częściowo | „Podpięty do formularza Fluent UI” nie mówi jak. FluentValidation nie ma natywnego mostu do `EditContext`. Trzeba wybrać jedną paczkę (np. Blazored.FluentValidation) albo własny komponent `FluentValidationValidator` w `Shared/`. Bez tego każdy z trzech formularzy podepnie walidację inaczej. |
| AD-9 | częściowo | tak | Zakres v1 jest niejasny w dwóch punktach. Wyszukiwarka we wszystkich kolumnach (memlog l.30) ani nie jest w v1, ani nie trafiła do Deferred. Sortowanie jednokolumnowe przez użytkownika też nie jest rozstrzygnięte (odłożone jest tylko wielokolumnowe). R8 wymaga sortowania od najnowszej daty, czyli stałego sortowania zadanego przez ekran. Nie ustalono, czy filtrowanie jest w pamięci, czy w zapytaniu. Zakres dat R8 (domyślnie bieżący Miesiąc) to filtr zapytania, a filtr wartości kolumny to filtr w pamięci; to dwa różne mechanizmy. Nie ustalono też stabilności Fluent UI 5 (memlog l.19: data wydania niepotwierdzona). Warto mieć plan B (4.x) zapisany jako ryzyko. |
| AD-10 | **nie w pełni** | częściowo | „Operacje istotne → `MessageBox`” nie jest rozstrzygalne bez listy operacji. Należy wymienić je wprost i zapisać, że zapis Wpisu w Sesji wpisywania **nie** wymaga potwierdzenia (R16). Skali „automatycznie według szerokości okna” nie da się sprawdzić bez progów (np. punkty przełamania CSS). |
| AD-11 | słabo | tak | Brak mechanizmu sprawdzenia. Proponowany test: brak `http(s)://` w `wwwroot` i w `App.razor`, a w testach bUnit/E2E brak żądań poza `127.0.0.1`. „Telemetria .NET wyłączona” dotyczy CLI (`DOTNET_CLI_TELEMETRY_OPTOUT`), nie aplikacji. Warto to doprecyzować, żeby nikt nie szukał nieistniejącego przełącznika w kodzie. |
| AD-12 | **nie w obecnym brzmieniu** | częściowo | (a) ASP.NET Core **nie ładuje** `appsettings.Local.json` domyślnie. Potrzebne jest `builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true)`, inaczej konfiguracja „działa” tylko u tego, kto to dopisze. (b) Sprzeczność: katalog kopii jest „tylko w Local”, a jednocześnie ma „domyślny katalog poza repo”. Skąd ta wartość domyślna, skoro `appsettings.json` ma tylko bezpieczne wartości? (c) Hasło w `docker-compose.yml` (B3.3). |
| AD-13 | tak | tak | Mechanizm pobrania: patrz AD-1. Komunikat zamiast pliku dla pustego Miesiąca (R13) jest prostszy przy endpoincie, jeśli ekran sprawdzi liczbę Wpisów przed nawigacją. |

### B3. Czy coś w Deferred pozwala dwóm jednostkom się rozjechać?

| Pozycja Deferred | Ryzyko rozjazdu | Ocena |
|---|---|---|
| Grupowanie użytkownika, sortowanie wielokolumnowe, wybór kolumn w `ReportGrid` | Jeden komponent, więc rozjazd jest mały. Brakuje rozstrzygnięcia wyszukiwarki i sortowania jednokolumnowego (B2, AD-9). | niskie |
| **Wygląd ekranów — „decyduje historia”** | **Tak.** Obejmuje formularz Wpisu, który jest wspólny dla epików 2, 3 i Listy (B1.1), a także wpisywanie Czasu i obsługę klawiatury (R16). Kolumny jednego ekranu mogą być w historii, ale wspólny formularz nie. | **wysokie** |
| Szczegóły Testcontainers — „pierwszy epik z testami zapisu” | Epik 1 (Słowniki) pierwszy zapisze do bazy, więc pierwszy też zdecyduje. Rozjazd tylko wtedy, gdy historie biegną równolegle. Dodatkowo trzeba wyłączyć krok kopii w testach (AD-3). | niskie / średnie |
| Wdrożenie poza komputer Autora, konta, HTTPS | Zgodne z Boundaries ticketu. | brak ryzyka |
| Odtwarzanie z Kopii — ręcznie, README „przy epiku startu” | Nie ma „epiku startu” na ścieżce z ticketu (B1.4). Procedura nie ma właściciela ani testu. | średnie (operacyjne) |

### B4. Kopert operacyjny: czy każdy wymiar jest rozstrzygnięty, odłożony albo otwarty?

| Wymiar | Status w spine | Uwagi |
|---|---|---|
| Środowisko docelowe | **rozstrzygnięty** | Windows, komputer Autora, Docker Desktop, `127.0.0.1` (AD-2, Structural Seed). |
| Środowiska (Development/Production) | **otwarty, nienazwany** | `dotnet run` domyślnie uruchamia w `Development` (strona wyjątków dewelopera, szczegółowe błędy obwodu, `appsettings.Development.json`). Do prawdziwych rozliczeń od stycznia trzeba zdecydować: `dotnet run -c Release` z `ASPNETCORE_ENVIRONMENT=Production`, `dotnet publish` + plik `.exe`, czy świadomie `Development`. To też wpływa na to, czy działa `appsettings.Local.json` (B2, AD-12). |
| Start | **rozstrzygnięty** | AD-3, kolejność i komunikaty. Otwarte: polityka `restart:` w `docker-compose.yml` wobec ograniczenia „bez autostartu” (NFR-4). Docker Desktop startujący z Windows plus `restart: unless-stopped` daje autostart bazy. |
| Zatrzymanie / praca ciągła | **pominięty** | Ctrl+C dla aplikacji. Czy kontener zostaje włączony (`docker compose stop`?). Drobne, ale warto jedno zdanie. |
| Konfiguracja i sekrety | **częściowo** | AD-12, z lukami z B2 (ładowanie pliku, hasło w compose). |
| Hasło bazy w `docker-compose.yml` (B3.3) | **otwarty** | Compose potrzebuje `POSTGRES_PASSWORD`. AD-12 kładzie hasło w `appsettings.Local.json`, którego compose nie czyta. Bez decyzji (np. plik `.env` w `.gitignore` z `.env.example` w repo) hasło trafi do `docker-compose.yml` w gicie. |
| Kopia bazy | **rozstrzygnięty** | AD-3: dobra reguła, zgodna z R14 (poza zmianą rozszerzenia). |
| Odtwarzanie (B3.4) | **odłożony, słabo** | Ręczny `psql` + README, bez właściciela i bez próby odtworzenia. Kopia, której nikt nie odtworzył, jest niesprawdzona. Rekomendacja: jeden test integracyjny (Testcontainers) dump → restore → porównanie liczby Wpisów albo przynajmniej krok w README sprawdzony przy Teście wrześniowym. Zrzut w formacie plain `.sql` warto robić z `--clean --if-exists`, żeby odtworzenie na istniejącą bazę było deterministyczne. |
| Migracje | **rozstrzygnięty** | `Database.Migrate()` po kopii. Brak konwencji tworzenia migracji między epikami (B1.3). |
| Aktualizacja PostgreSQL (18 → 19) | **pominięty** | Zmiana wersji głównej wymaga dump/restore. Wystarczy jedno zdanie w Deferred („aktualizacja wersji głównej przez Kopię bazy”). |
| Logi / obserwowalność | **rozstrzygnięty** | Konsola, bez zewnętrznych ujść. Dla lokalnej aplikacji wystarczy. |
| Kolizje portów | **pominięty** | `5432` może być zajęty przez inny lokalny PostgreSQL. Port powinien być w konfiguracji (compose + connection string). Niska waga. |
| Bezpieczeństwo sieci | **rozstrzygnięty** | `127.0.0.1` po obu stronach (AD-2), HTTPS świadomie odłożony. |

---

## Rekomendacje (kolejność)

1. **Nowe AD: Formularz Wpisu i Sesja wpisywania** (R5, R6, R7, R8, R9, R16). Jeden komponent w `Features/Wpisy`, otwierany z menu, Listy i Widoku. Data Sesji w usłudze `Scoped` (na obwód). Callback „zmieniono” dla wywołującego, który wywołuje ponowne `MonthReport.Build`. Czas jako pole tekstowe `h:mm`. Kolejność fokusu, Enter zapisuje, po zapisie fokus wraca na Projekt. Zapis Wpisu nigdy nie otwiera `MessageBox`.
2. **AD-13 + AD-1:** pobranie przez endpoint minimal API (`GET /zestawienie/{rok}/{miesiac}`), bez własnego JS. Doprecyzować AD-1 („poza `blazor.web.js` i Fluent UI”).
3. **AD-6 rozszerzyć:** jedno miejsce formatu i parsowania `h:mm` oraz konwersji na godziny dziesiętne (np. `Reporting/Czas`). „Dziś” jako czas lokalny.
4. **AD-7 rozszerzyć o R2:** kolumna `is_active`. Reguła list wyboru „aktywne + bieżąca wartość” (edycja) / „aktywne, inaczej puste” (Kopia). Widok, Lista i eksport nigdy nie filtrują po aktywności. Atrybuty encji w diagramie ER. Konwencja migracji.
5. **AD-12 + koperta:** jawne ładowanie `appsettings.Local.json`, `.env` dla compose w `.gitignore`, wybór środowiska uruchomieniowego, `restart: "no"` w compose, port w konfiguracji.
6. **AD-9 / AD-5:** `ReportGrid` nie sumuje. Rozstrzygnąć wyszukiwarkę i sortowanie jednokolumnowe. Filtr zakresu dat R8 w zapytaniu, filtr wartości w pamięci.
7. **AD-10:** lista operacji „istotnych”. Lokalizacja Fluent UI w `Shared/`. `ErrorBoundary` w `MainLayout`.
8. **Własność:** scaffold, `Startup/`, `ReportGrid` i README odtwarzania przypisać do epiku 1. Dodać test odtworzenia kopii.
9. **Ticket:** przekazać Sofii/Johnowi listę z A2.
