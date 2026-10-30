---
type: review
lens: adversary (pary niekompatybilne)
target: architecture-rejestr-czasu.md (AD-1..AD-13, status draft)
date: 2026-10-30
---

# Przegląd adwersarza: spine „Rejestr czasu pracy”

## Werdykt

Spine dobrze zamyka stos, start i jedno źródło sum, ale zostawia otwarte **kształty danych na szwach między epikami**: wejście i wyjście `MonthReport`, schemat encji, sposób hostowania formularza Wpisu, kontrakt `ReportGrid<T>` i mechanizm pobrania pliku. Dwóch agentów, z których każdy przestrzega AD co do litery, zbuduje w tych miejscach rzeczy, które się nie złożą albo złożą się z cichą rozbieżnością sum (R10). Poniżej 10 par; pary 1–4 są blokujące przed rozpisaniem epików.

Założone epiki: **E1** Słowniki + szkielet/start/kopia, **E2** Wpisy + Sesja wpisywania, **E3** Lista wpisów + Widok miesiąca, **E4** Zestawienie + Test wrześniowy. Paradygmat (komponenty → `IDbContextFactory`, czysty `MonthReport`) przyjmuję jako dany; poprawki go nie podważają, tylko ustalają kontrakty na granicach.

Skala: **Wysoka** — nie złoży się albo złamie R10/R11 bez błędu kompilacji; **Średnia** — rozjazd zachowania/UI lub duplikacja logiki, wykrywalna przy integracji; **Niska** — kosmetyka lub koszt przeróbki.

---

## Para 1 — Dwóch właścicieli `MonthReport` i nieustalony kształt wejścia/wyjścia — **Wysoka**

**Jednostka A (E3, Widok miesiąca):** tworzy `Reporting/MonthReport.Build(int year, int month, IEnumerable<Entry> entries)`, gdzie `Entry` to `record(int Id, DateOnly Date, string ProjectCode, string WorkTypeName, string Description, int Minutes)`. Wynik: dni z listą Wpisów (z `Id`, bo R9 wymaga edycji/kopii/usunięcia z widoku), Suma dnia, Suma tygodnia, suma Miesiąca. Złączania nie robi — Widok miesiąca pokazuje Wpisy, nie Wiersze zestawienia. Zapytanie sortuje wejście po `date`.

**Jednostka B (E4, Zestawienie):** AD-5 mówi, że złączanie Wpisów w Wiersze zestawienia liczy „wyłącznie `MonthReport.Build`”, więc rozszerza go: zmienia sygnaturę na `Build(DateOnly month, IReadOnlyList<ReportEntry> entries)`, wprowadza własny typ wejściowy z polem `Sequence` (R11: „opisy w kolejności dodania”) i zwraca `IReadOnlyList<ReportRow>`. Kolejność dodania bierze z nowej kolumny `created_at`, którą dodaje własną migracją.

**Dlaczego obaj są zgodni z AD:** AD-5 nazywa funkcję, ale nie jej typy ani właściciela; diagram mówi o „typach domeny: Czas, Wpis (bez EF)”, lecz struktura katalogów nie ma `Domain/`, a konwencja nazewnicza każe pisać po angielsku (`TimeEntry` jest już zajęte przez encję). „Kolejność dodania” nie ma zdefiniowanego nośnika (id z sekwencji? `created_at`? kolejność w zapytaniu?).

**Skutek:** E4 łamie kompilację E3 albo powstają dwa przeciążenia `Build` z dwiema implementacjami sum tygodnia (dokładnie to, czemu AD-5 miało zapobiec). Dodatkowo: dwa zapytania „Wpisy Miesiąca” (jedno w `Features/Miesiac`, drugie w `Features/Zestawienie`) z różnym sortowaniem → przy złączaniu opisy w różnej kolejności niż w Widoku miesiąca; edycja Wpisu z `created_at` vs `id` daje różną kolejność opisów.

**Proponowany AD (nowy):**

> ### AD-14 — Kontrakt `MonthReport`
> - **Binds:** R9, R10, R11, R12
> - **Prevents:** dwóm sygnaturom i dwóm właścicielom rdzenia raportowego; różnej kolejności opisów.
> - **Rule:** `Reporting/` zawiera typy wejścia i wyjścia rdzenia i nic więcej o nich nie decyduje: wejście `ReportEntry(int Id, DateOnly Date, string ProjectCode, string WorkTypeName, string Description, int Minutes)`; wynik `MonthReport` z polami `Month` (typ `YearMonth`, AD-18), `Weeks` (każdy: `Index` 1..n w Miesiącu, `Minutes`, `Days`), każdy dzień: `Date`, `DayName`, `Minutes`, `Entries` (lista `ReportEntry` w kolejności wyświetlania Widoku miesiąca) i `Rows` (Wiersze zestawienia: `ProjectCode`, `WorkTypeName`, `Descriptions` w kolejności dodania, `Minutes`), oraz `TotalMinutes`. Kolejność dodania = rosnące `Id` Wpisu (sekwencja); edycja Wpisu jej nie zmienia; kolumny `created_at` nie ma. `MonthReport` sam sortuje wejście — kolejność argumentu nie ma znaczenia. Wpisy Miesiąca ładuje jedna funkcja `Features/Miesiac/MonthEntriesQuery.LoadAsync(AppDbContext, YearMonth)` zwracająca `IReadOnlyList<ReportEntry>`; eksport wywołuje tę samą funkcję. Całość `MonthReport` (sumy i złączanie) powstaje w jednej historii przed pierwszym ekranem, który go używa; późniejsze epiki go nie rozszerzają bez zmiany tego AD.

---

## Para 2 — `ReportGrid<T>` liczy sumy i filtruje sam, a Widok miesiąca/Lista wpisów liczą gdzie indziej — **Wysoka**

**Jednostka A (E1, Słowniki — pierwszy użytkownik siatki, więc ją buduje):** AD-9 każe dać „stałe grupowanie zadane przez ekran z sumą w nagłówku grupy” i „filtr wartości w każdej kolumnie”. Agent robi to generycznie: parametr `GroupBy: Func<T,object>`, `SumOf: Func<T,int>`, a siatka po zastosowaniu filtrów kolumn liczy w pamięci sumę pozycji widocznych w grupie i licznik pozycji. Dane: `IQueryable<T> Items` (bo `FluentDataGrid` to przyjmuje).

**Jednostka B (E3, Widok miesiąca i Lista wpisów):** AD-5 zabrania sumowania poza `MonthReport`, więc przekazuje gotowe sumy z `MonthReport` do nagłówków (szablon nagłówka grupy). Na Liście wpisów suma R8 (dopuszczony wyjątek AD-5) jest liczona zapytaniem SQL po filtrach R8 (zakres dat, Projekt, Typ pracy).

**Dlaczego obaj są zgodni z AD:** AD-9 wymaga sumy w nagłówku grupy, ale nie mówi, kto ją liczy; AD-5 zakazuje sumowania „komponentom”, a `ReportGrid` jest w `Shared/`, nie w ekranie. AD-9 każe dać filtr wartości „w każdej kolumnie” każdego raportu — także Widoku miesiąca.

**Skutek:**
- Autor filtruje kolumnę Projekt w Widoku miesiąca: wiersze zawężone, a Suma dnia/tygodnia w nagłówku nadal z `MonthReport` (całość) — albo, gdy siatka liczy sama, inna niż w Zestawieniu (łamie R10 po cichu).
- Lista wpisów: licznik pozycji siatki (po filtrach kolumn w pamięci) ≠ „liczba Wpisów” i suma R8 (z SQL, bez filtrów kolumn).
- `IQueryable` z kontekstu utworzonego `await using` w `OnInitializedAsync` jest wykonywany przez siatkę po `Dispose` → `ObjectDisposedException` (AD-4 łamie nie ekran, lecz kontrakt siatki).
- Dwie implementacje sumy minut (siatka + `MonthReport`).

**Proponowany AD (zaostrzenie AD-9, tekst do dopisania):**

> - **Rule (dopisek):** `ReportGrid<T>` przyjmuje wyłącznie zmaterializowaną `IReadOnlyList<T>` i nigdy nie liczy agregatów: zawartość nagłówka grupy i stopki (sumy, liczby) dostarcza ekran jako gotowe wartości lub szablon. Licznik pozycji pokazuje liczbę pozycji po filtrach kolumn. Na ekranach z sumą biznesową (Widok miesiąca, Lista wpisów) filtry wartości kolumn są wyłączone — zawężanie odbywa się tylko filtrami ekranu (R8) albo wyborem Miesiąca, a suma i liczba Wpisów dotyczą dokładnie pokazanych wierszy. Suma pod Listą wpisów to `Sum(Minutes)` w tym samym zapytaniu, które zwraca wiersze, nie osobne zapytanie. Siatka zachowuje kolejność wejścia; sortowanie przez kliknięcie nagłówka jest wyłączone na Widoku miesiąca (kolejność należy do `MonthReport`). Przycisk Odśwież wywołuje `OnRefresh` ekranu, który ponownie ładuje dane z bazy.

---

## Para 3 — Formularz Wpisu: strona vs dialog i dwie ścieżki odświeżenia po zmianie — **Wysoka**

**Jednostka A (E2, Wpisy + Sesja wpisywania):** formularz to strona `/wpisy/nowy` (przycisk „Nowy wpis” w menu), po zapisie zostaje otwarty z tą samą datą (R5). Edycja: `/wpisy/{id}`. Usunięcie: przycisk „Usuń” na stronie edycji z `MessageBox`, po usunięciu nawigacja do `/wpisy/nowy`.

**Jednostka B (E3, Lista wpisów + Widok miesiąca):** R6/R7/R9 wymagają edycji, kopii i usunięcia z obu ekranów i „przeliczenia sum od razu”. Agent otwiera `EntryForm` w `FluentDialog` (żeby nie tracić kontekstu Miesiąca), kopię przekazuje przez parametr dialogu z obiektem Wpisu, usuwa z wiersza siatki własnym `MessageBox` i po usunięciu wyjmuje wiersz z listy w pamięci i przelicza `MonthReport` lokalnie.

**Dlaczego obaj są zgodni z AD:** AD-10 ustala tylko MessageBar/MessageBox i układ; spine w „Deferred” oddaje wygląd ekranów historiom. Nic nie mówi, czy Wpis edytuje się na stronie czy w dialogu, jak przekazuje się kopię, dokąd wraca się po zapisie ani jak ekran odświeża stan po mutacji.

**Skutek:** dwa hosty jednego formularza (strona z logiką „zostań z datą” i dialog, który musi ją wyłączyć), dwie ścieżki usuwania z różnymi tekstami potwierdzenia, dwie ścieżki odświeżenia (reload z bazy vs łatanie listy w pamięci). Łatanie w pamięci rozjeżdża się z bazą przy zmianie daty Wpisu na inny Miesiąc lub przy zmianie Projektu (kod w wierszu nieaktualny). Kopia przekazywana obiektem omija regułę R7 „nieaktywny Projekt → pole puste”, jeśli tę regułę zaimplementowano tylko w ścieżce `?kopia=`.

**Proponowany AD (nowy):**

> ### AD-15 — Jeden formularz Wpisu i ścieżka mutacji
> - **Binds:** R5, R6, R7, R8, R9, R16
> - **Prevents:** dwóm hostom formularza Wpisu, dwóm ścieżkom usuwania i odświeżania.
> - **Rule:** Wpis tworzy się, edytuje i kopiuje wyłącznie na stronie `Features/Wpisy/EntryPage.razor` pod adresami `/wpisy/nowy`, `/wpisy/{id:int}` i `/wpisy/nowy?kopia={id:int}`; opcjonalny parametr `powrot` (lokalny adres względny) wskazuje stronę, na którą wraca zapis edycji, kopii i usunięcie. Bez `powrot` strona działa jako Sesja wpisywania (R5). Regułę R7 (nieaktywny Projekt/Typ pracy → puste pole) stosuje tylko ta strona przy wczytaniu `kopia`. Usunięcie Wpisu ma jedną implementację `Features/Wpisy/DeleteEntry` (potwierdzenie `MessageBox` + usunięcie) wywoływaną z siatek i ze strony edycji. Po każdej mutacji ekran ponownie ładuje dane z bazy i ponownie wywołuje `MonthReport.Build`; zakaz modyfikowania załadowanych list w pamięci jako zamiennika ponownego odczytu. Dialogi nie hostują formularzy edycji.

(Jeżeli Autor woli dialog — reguła ma brzmieć analogicznie z jednym `EntryDialog`; ważne, by był dokładnie jeden host.)

---

## Para 4 — Eksport: mechanizm pobrania i punkt wejścia — **Wysoka**

**Jednostka A (E3, Widok miesiąca):** R12 mówi „jedno kliknięcie w Widoku miesiąca”, więc agent dodaje przycisk „Eksport” w nagłówku Widoku miesiąca. Pozycja menu „Eksport” (AD-10) prowadzi do `/miesiac`.

**Jednostka B (E4, Zestawienie):** AD-10 wymienia w menu osobną pozycję „Eksport”, a AD-13 każe dać „pobranie w przeglądarce”. Agent tworzy stronę `/eksport` z wyborem Miesiąca. Plik przesyła do przeglądarki przez `DotNetStreamReference` + funkcję JS `downloadFileFromStream` (standardowy przepis Microsoftu dla Blazor Server).

**Dlaczego obaj są zgodni z AD (prawie):** AD-13 nie mówi jak; AD-1 zakazuje JavaScriptu poza Fluent UI, więc przepis Microsoftu jest de facto zakazany — agent B albo łamie AD-1, albo utyka, albo wymyśla trzeci sposób (data-URI w `NavigationManager`, endpoint). R13 („Miesiąc bez Wpisów → komunikat zamiast pliku”) przy zwykłym linku do endpointu nie ma gdzie pokazać `MessageBar`.

**Skutek:** dwa punkty wejścia z różnym wyborem Miesiąca; ryzyko JS w repo; komunikat R13 zaimplementowany raz w stronie `/eksport`, a przycisk w Widoku miesiąca pobiera pusty plik lub stronę błędu 404.

**Proponowany AD (zaostrzenie AD-13, tekst do dopisania):**

> - **Rule (dopisek):** pobranie realizuje endpoint Minimal API `GET /zestawienie/{rok:int}/{miesiac:int}` zdefiniowany w `Features/Zestawienie/`, zwracający `Results.File` z nazwą wg R13 (nagłówek z `filename*` UTF-8); ładuje dane przez `MonthEntriesQuery` (AD-14) i `IDbContextFactory`. Przycisk „Eksport” w nagłówku Widoku miesiąca to zwykły odnośnik do tego endpointu dla wyświetlanego Miesiąca — bez JavaScriptu i bez `DotNetStreamReference`. Gdy `MonthReport` wyświetlanego Miesiąca jest pusty, Widok miesiąca zamiast odnośnika pokazuje `MessageBar` R13; endpoint dla pustego Miesiąca zwraca 404 jako zabezpieczenie. Pozycja menu „Eksport” prowadzi do Widoku miesiąca bieżącego Miesiąca; osobnej strony eksportu nie ma.

---

## Para 5 — Schemat encji i migracje mają dwóch autorów — **Wysoka/Średnia**

**Jednostka A (E1, Słowniki):** tworzy `Project(Id, Code, Name, IsActive)`, `WorkType(Id, Name, IsActive)` i pierwszą migrację. Kod zamienia na wielkie litery w komponencie przed zapisem (`model.Code.ToUpperInvariant()`). R3 (liczba Wpisów) wymaga `TimeEntry`, więc dokłada minimalną encję `TimeEntry(Id, Date, ProjectId, WorkTypeId)` z FK RESTRICT.

**Jednostka B (E2, Wpisy):** tworzy własną `TimeEntry(Id, EntryDate, ProjectId, WorkTypeId, Description, Minutes, CreatedAt)` z migracją `AddTimeEntries`. Flagę aktywności czyta jako `Active`/`DeactivatedAt` z własnego założenia.

**Dlaczego obaj są zgodni z AD:** AD-7 ustala indeksy i FK, AD-6 nazwę kolumny `minutes`, konwencje — `snake_case` i `int id`. Żaden AD nie podaje pełnej listy encji i pól, właściciela encji ani zasady migracji; AD-7 mówi „kod zapisywany wielkimi literami”, ale nie gdzie (komponent? walidator? konwerter?). Zamiana naruszenia indeksu na `MessageBar` (AD-7) — każdy ekran sam rozpoznaje `PostgresException` 23505 i nazwę ograniczenia, której nikt nie ustalił.

**Skutek:** konflikt w `AppDbContextModelSnapshot`, podwójna encja lub migracja zmieniająca nazwę kolumny; ścieżka zapisu omijająca komponent (dane testowe, seed Testu wrześniowego) zapisze `rdz` obok `RDZ` — indeks na `code` (nie na `lower(code)`) duplikatu nie złapie; dwie wersje rozpoznawania 23505.

**Proponowany AD (nowy):**

> ### AD-16 — Schemat i właściciel danych
> - **Binds:** R1, R2, R3, R4, R11
> - **Prevents:** dwóm definicjom encji, konfliktom migracji i różnym ścieżkom normalizacji.
> - **Rule:** encje i ich pola są stałe: `Project(Id, Code, Name, IsActive)`, `WorkType(Id, Name, IsActive)`, `TimeEntry(Id, Date, ProjectId, WorkTypeId, Description, Minutes)`; nazwy kolumn `snake_case` (`is_active`, `project_id`…). Wszystkie trzy encje, indeksy (`ux_project_code`, `ux_work_type_name_lower`) i FK RESTRICT tworzy pierwsza historia szkieletu w jednej migracji `Initial`; późniejsze historie dodają encję lub kolumnę tylko po zmianie tego AD i każda dodaje dokładnie jedną nową migrację (nigdy nie edytuje istniejącej). Normalizacja kodu Projektu (trim, wielkie litery `ToUpperInvariant`) i trim nazw dzieje się w jednym miejscu: w `AppDbContext.SaveChanges[Async]` dla zmienionych encji. Naruszenia ograniczeń tłumaczy jedna funkcja `Data/DbErrors.TryGetViolation(DbUpdateException, out string constraint)`; ekrany mapują nazwę ograniczenia na tekst `MessageBar`.

---

## Para 6 — Czas, nazwy dni i miesięcy formatowane w czterech miejscach — **Średnia**

**Jednostka A (E2):** pole Czas jako tekst `h:mm`, parser w walidatorze (`"1:30"` → 90, akceptuje też `"1,5"`). Formatowanie w Liście wpisów: `TimeSpan.FromMinutes(m).ToString(@"h\:mm")`.

**Jednostka B (E3/E4):** Widok miesiąca formatuje sumy tygodnia tym samym `TimeSpan` → dla 30 h wychodzi `6:00` (godziny modulo 24) albo `1.06:00`; Zestawienie liczy godziny dziesiętne `m / 60.0` i nazwę dnia `date.ToString("dddd")` → „środa” małą literą, nazwę arkusza z `MonthNames` (mianownik) a nazwę pliku z `MonthGenitiveNames` albo z polskimi znakami „wrzesień” vs „wrzesien”.

**Dlaczego zgodni z AD:** AD-6 ustala `int` minut i `pl-PL`, konwencja — „formatowanie `h:mm` w UI”, ale nie ma jednego formatera ani parsera, a diagram wspomina typ domeny `Czas` bez lokalizacji. Kultura `pl-PL` daje nazwy małą literą — układ wymaga „Środa”, „Wrzesień 2026”, „RAZEM WRZESIEŃ 2026”.

**Skutek:** Suma tygodnia 30 h pokazana jako `6:00` (Test wrześniowy porównuje 30/16/18/25/14); różne nazwy dni w Widoku miesiąca i w xlsx; nazwa pliku niezgodna z R13; parser w walidatorze akceptuje formaty, których kopia (R7) nie potrafi wypisać.

**Proponowany AD (zaostrzenie AD-6, tekst do dopisania):**

> - **Rule (dopisek):** jedyne miejsca konwersji Czasu i nazw kalendarza to statyczne `Reporting/Czas` (`TryParse("h:mm") → int`, `Format(int) → "h:mm"` bez zawijania doby, np. `30:00`; `ToDecimalHours(int) → decimal`) i `Reporting/PolishCalendar` (`DayName(DateOnly)` z wielkiej litery, `MonthName(YearMonth)` w mianowniku z wielkiej litery, `FileMonthName(YearMonth)` małą literą z polskimi znakami). Pole Czas w formularzu to tekst przyjmujący wyłącznie `h:mm` (oraz `hh:mm`). Zakaz `TimeSpan` i format stringów `dddd`/`MMMM` w kodzie ekranów i eksportu.

---

## Para 7 — Identyfikator Miesiąca i adresy ekranów — **Średnia**

**A (E3):** Widok miesiąca pod `/miesiac?rok=2026&m=9`, typ `DateOnly` (pierwszy dzień). **B (E4/E2):** `MonthReport.Build(int year, int month)`, Lista wpisów `?od=2026-09-01&do=2026-09-30`, `powrot` z E2 zakłada inny format. Nawigacja „wróć po edycji do tego samego Miesiąca” i odnośnik eksportu składają adresy po swojemu.

**Proponowany AD (nowy, krótki):**

> ### AD-18 — Miesiąc i adresy
> - **Rule:** Miesiąc w kodzie to `Reporting/YearMonth(int Year, int Month)`. Adresy: Widok miesiąca `/miesiac/{rok:int}/{miesiac:int}` (bez parametrów → bieżący), Lista wpisów `/wpisy?od=RRRR-MM-DD&do=RRRR-MM-DD&projekt={id}&typ={id}`, Wpis wg AD-15, Zestawienie wg AD-13. „Dziś” pochodzi z wstrzykiwanego `TimeProvider` (czas lokalny), nigdy z `DateTime.Now`.

---

## Para 8 — Komunikaty: host `MessageBar`, Toast i treść potwierdzeń — **Średnia**

**A (E1):** `MessageBar` jako globalny pasek w `MainLayout` sterowany usługą (`IMessageService` Fluent UI), potwierdzenia `MessageBox` z przyciskami „Tak/Nie”. **B (E2):** inline `<FluentMessageBar>` nad formularzem, sukces zapisu w Sesji wpisywania jako `Toast` (nie objęty AD-10, więc „dozwolony”), potwierdzenie usunięcia „Usuń/Anuluj” z fokusem na „Usuń” (Enter od razu kasuje — ryzyko przy pracy klawiaturą R16).

**Proponowany AD (zaostrzenie AD-10, tekst do dopisania):**

> - **Rule (dopisek):** `MessageBar` wyświetla się w jednym miejscu: w obszarze komunikatów pod nagłówkiem strony z `MainLayout`, przez jedną usługę `Shared/Notify` (`Info`, `Success`, `Warning`, `Error`); komunikaty walidacji pól — przy polach (FluentValidation). Potwierdzenia przez jedną funkcję `Shared/Confirm.AskAsync(tytuł, treść, przyciskAkcji)` z przyciskami „<akcja>” / „Anuluj” i domyślnym fokusem na „Anuluj”. Toastów i `alert` się nie używa.

---

## Para 9 — Listy wyboru Projektu/Typu pracy i ich nieaktywne pozycje — **Średnia**

**A (E2):** formularz Wpisu ładuje aktywne Projekty `ORDER BY name`, pokazuje „Nazwa (KOD)” w `FluentSelect`; przy edycji Wpisu z nieaktywnym Projektem pole jest puste (pozycji brak na liście) — R2 łamany przy zapisie. **B (E3):** filtr Listy wpisów ładuje wszystkie Projekty `ORDER BY code` w `FluentCombobox` z etykietą „KOD — Nazwa” i dopiskiem „(nieaktywny)”. Każdy ekran ma własne zapytanie i własną etykietę; kolejność w Widoku miesiąca (AD-14: wg kodu porównywanego w `MonthReport`) różni się od kolejności z PostgreSQL (`ORDER BY code` z kolacją bazy).

**Proponowany AD (nowy):**

> ### AD-17 — Wybór pozycji Słownika
> - **Binds:** R2, R7, R8, R16
> - **Rule:** listy wyboru Projektu i Typu pracy dostarcza jedna funkcja `Features/Slowniki/DictionaryOptions.LoadAsync(AppDbContext, includeInactive, ensureIds)`; formularz Wpisu woła ją z `includeInactive: false` i `ensureIds` = pozycje edytowanego Wpisu, filtry z `includeInactive: true`. Etykieta: Projekt `KOD — nazwa`, Typ pracy `nazwa`, nieaktywne z dopiskiem „(nieaktywny)”. Kolejność: Projekty wg kodu, Typy pracy wg nazwy, porównanie `StringComparer.Create(pl-PL, ignoreCase: true)` w pamięci — to samo porównanie stosuje `MonthReport`. Kontrolka: `FluentCombobox`/autouzupełnianie z wpisywaniem z klawiatury (R16), ta sama w formularzu i filtrach.

---

## Para 10 — Start, testy i konfiguracja — **Średnia/Niska**

**A (E1):** kroki AD-3 w `IHostedService`, opcje `Backup:Directory`, `Database:ContainerName`, połączenie w `ConnectionStrings:Default`. **B (E2/E4, pierwsze testy Testcontainers):** w `WebApplicationFactory` hosted service próbuje `docker exec` na nieistniejącym kontenerze i wywala testy, więc agent tworzy schemat przez `EnsureCreated()` (inny schemat niż z migracji — brak indeksu `lower(name)` z surowego SQL) i dodaje `Backup:Enabled=false`. E4 czyta prefiks z `Export:FilePrefix`, a `appsettings.Local.json` Autora ma `Export:Prefix`.

**Proponowany AD (zaostrzenie AD-3 i AD-12):**

> - **AD-3, dopisek:** kroki 1–3 wykonuje `Startup/StartupSequence.RunAsync` wywołane w `Program.cs` po `Build()` i przed `Run()` (nie `IHostedService`); test integracyjny pomija krok 2 przez `Backup:Enabled=false` i zawsze buduje schemat przez `Database.Migrate()`, nigdy `EnsureCreated()`.
> - **AD-12, dopisek:** klucze konfiguracji są stałe: `ConnectionStrings:Rejestr`; `Backup:Enabled`, `Backup:Directory`, `Backup:ContainerName`, `Backup:Keep` (=10); `Export:FilePrefix`. Klasy opcji `BackupOptions`, `ExportOptions` w `Startup/` i `Features/Zestawienie/`.

---

## Drobniejsze dziury (bez pary, do dopisania przy okazji)

- **AD-13, typy komórek:** „sumy jako wartości” nie wyklucza zapisu `"1,5"` jako tekstu. Dopisek: „kolumny F, G, H i suma RAZEM jako liczby (`decimal`/`double`), kolumna B jako data Excela z formatem `dd.mm.yyyy`; żadna liczba ani data nie trafia do komórki jako tekst”.
- **Zależność `Shared → Reporting`** w diagramie sugeruje, że `ReportGrid` zna `MonthReport`. Po AD-9 (dopisek) siatka jest generyczna; strzałka powinna dotyczyć tylko `Reporting/Czas` (formatowanie). Zaostrzyć: „`Shared/` korzysta z `Reporting/` wyłącznie przez `Czas` i `PolishCalendar`”.
- **AD-8 a R2:** walidator „nie sprawdza bazy”, więc nikt nie blokuje zapisu nowego Wpisu z Projektem dezaktywowanym w innej karcie. Ryzyko niskie (jeden użytkownik) — świadomie zaakceptować w Deferred.
- **R14 vs AD-3:** inicjatywa nadal mówi `.sqlite` i „ścieżki bazy” — wiadomo z memlogu; dopóki Sofia nie poprawi R14/R15, agent E1 ma dwa sprzeczne źródła. Nie dziura spine, ale blokada.

## Podsumowanie poprawek

| # | Para | Skala | Poprawka |
| --- | --- | --- | --- |
| 1 | E3 vs E4: `MonthReport` | Wysoka | nowy AD-14 (typy wejścia/wyjścia, kolejność = `Id`, jedno zapytanie `MonthEntriesQuery`) |
| 2 | E1 vs E3: `ReportGrid` liczy sumy/filtruje | Wysoka | dopisek AD-9 (siatka bez agregatów, `IReadOnlyList`, filtry kolumn wyłączone przy sumach) |
| 3 | E2 vs E3: host formularza i odświeżanie | Wysoka | nowy AD-15 (jedna strona Wpisu, `powrot`, jedno `DeleteEntry`, reload po mutacji) |
| 4 | E3 vs E4: pobranie i wejście eksportu | Wysoka | dopisek AD-13 (endpoint Minimal API, odnośnik bez JS, R13 w Widoku miesiąca) |
| 5 | E1 vs E2: schemat encji | Wysoka/Średnia | nowy AD-16 (pola encji, migracja `Initial`, normalizacja w `SaveChanges`, `DbErrors`) |
| 6 | E2 vs E3/E4: formatowanie Czasu i nazw | Średnia | dopisek AD-6 (`Czas`, `PolishCalendar`, zakaz `TimeSpan`/`dddd`) |
| 7 | E2/E3/E4: Miesiąc i adresy | Średnia | nowy AD-18 (`YearMonth`, trasy, `TimeProvider`) |
| 8 | E1 vs E2: komunikaty | Średnia | dopisek AD-10 (`Notify`, `Confirm`, fokus na Anuluj, bez Toastów) |
| 9 | E2 vs E3: listy Słowników | Średnia | nowy AD-17 (`DictionaryOptions`, etykiety, porównanie pl-PL) |
| 10 | E1 vs testy/E4: start i konfiguracja | Średnia/Niska | dopiski AD-3, AD-12 (klucze, `Migrate()` w testach) |
