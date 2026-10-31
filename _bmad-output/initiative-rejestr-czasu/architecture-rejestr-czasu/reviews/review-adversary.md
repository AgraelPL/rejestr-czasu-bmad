---
type: review
lens: adversary (pary niekompatybilne)
target: architecture-rejestr-czasu.md (AD-1..AD-22, status draft, updated 2026-10-31)
date: 2026-10-31
---

# Przegląd adwersarza: spine „Rejestr czasu pracy” (po zmianie: Konta i wdrożenie)

## Werdykt

**Nie gotowy do budowy epiku 4 bez poprawek.** Spine po zmianie dobrze nazywa *co* ma powstać (kontekst Usera, stan Konta, statyczne strony `/konto`, migracje dodające, pomoc), ale prawie nigdzie nie ustala *kontraktu na szwie* między epikiem 4 (który buduje fundament Kont) a epikami 1, 2, 3 i 5, które go używają. Najgroźniejsze dziury są w trzech miejscach: (1) skąd `CurrentUser` bierze Usera w obwodzie, w statycznym SSR i w endpoincie Minimal API, (2) jak `AppDbContext` zachowuje się bez Usera (Identity, Data Protection, `/health`, `Startup`) i (3) jak sprawdzanie stanu Konta, wymuszona zmiana hasła i wylogowanie współgrają z obwodem Blazora, w którym nie da się ustawić ani skasować ciasteczka. Każdą z par poniżej da się zbudować zgodnie z literą AD, a wynik się nie złoży albo złoży się z cichym błędem bezpieczeństwa.

Oznaczenia jednostek: **E4** epic-konta-i-wdrozenie, **E1** epic-szkielet-i-slowniki (historie 1.x z `tickets.toml` epiku), **E2** epic-wpisy-i-lista, **E3** epic-miesiac-i-zestawienie, **E5** epic-administracja-kont-i-pomoc. Historie E4, E2, E3, E5 nie są jeszcze rozpisane, więc jednostką jest „historia E-n, która buduje X” zgodnie z opisem epiku.

Skala: **Krytyczna** — przeciek danych między Userami albo aplikacja nie startuje/nie działa dla nikogo; **Wysoka** — funkcja z wymagania nie działa lub obchodzi zabezpieczenie, wykrywalne dopiero przy integracji epików; **Średnia** — dwie implementacje tej samej rzeczy, przeróbka współdzielonego komponentu, rozjazd zachowania; **Niska** — koszt, kosmetyka, niejasność do domknięcia w historii.

| Waga | Liczba |
| --- | --- |
| Krytyczna | 2 |
| Wysoka | 9 |
| Średnia | 8 |
| Niska | 4 |
| **Razem** | **23** |

---

## Krytyczne

### D1 — Dwa (trzy) sposoby ustalenia bieżącego Usera — **Krytyczna**

**Jednostka A (E4, historia „UserId z globalnym filtrem”):** implementuje `Accounts/CurrentUser` przez `AuthenticationStateProvider.GetAuthenticationStateAsync()` i `ClaimTypes.NameIdentifier`. W obwodzie Interactive Server działa; bUnit też (fake `AuthenticationStateProvider`).

**Jednostka B (E3, historia „endpoint `/zestawienie`”):** zgodnie z AD-14 wywołuje `MonthEntriesQuery`, która tworzy kontekst przez `UserDbContextFactory` → `CurrentUser`. W endpoincie Minimal API `AuthenticationStateProvider` nie jest zasilony stanem (zasila go dopiero endpoint komponentów Razor), więc `GetAuthenticationStateAsync` rzuca albo zwraca anonima → AD-16 „brak Usera = wyjątek” → 500 przy każdym eksporcie. Odwrotny wariant: A używa `IHttpContextAccessor` (żeby endpoint działał) — wtedy w obwodzie `HttpContext` jest `null` albo jest kontekstem żądania, które otworzyło obwód; po `Wyloguj`/zalogowaniu na inne Konto w tej samej karcie (nowy obwód, ale ten sam wzorzec) agent „naprawiający” to przez cache w polu singletonu daje przeciek Usera.

**Trzeci sposób (E3 Ustawienia konta, E5 Użytkownicy):** `UserManager.GetUserAsync(principal)` / `FindByNameAsync(User.Identity.Name)` — po loginie zamiast po `Id`; zmiana loginu (dozwolona przez `UserManager`) albo różnica normalizacji daje innego Usera niż `CurrentUser`.

**Dlaczego zgodne z AD:** AD-16 mówi „`UserId` zalogowanego Usera z `Accounts/CurrentUser`”, AD-17 „i w endpoincie Zestawienia”, ale żaden AD nie mówi, *skąd* `CurrentUser` czyta tożsamość w trzech hostach (obwód, statyczny SSR, Minimal API) ani jaki claim jest kluczem.

**Nowa reguła (AD-16, doprecyzowanie):** `CurrentUser` (scoped) ma jedno źródło tożsamości na host: w obwodzie `AuthenticationStateProvider` (ustawiany przez `RevalidatingServerAuthenticationStateProvider`), w żądaniu HTTP (statyczny SSR, endpoint) `HttpContext.User` przez `IHttpContextAccessor` — wybór robi sam `CurrentUser` (gdy obwód: `HttpContext` nie jest używany nigdy). `UserId` = `int.Parse(claim NameIdentifier)`; zakaz ustalania Usera po loginie. Nikt poza `CurrentUser` nie czyta `AuthenticationState`/`HttpContext.User` w celu ustalenia Id (wyjątek: `AuthorizeView` do renderowania menu). E4 dostarcza testowy `CurrentUser` dla bUnit i Testcontainers oraz test, że endpoint Zestawienia i ekran danych widzą tego samego Usera.

### D2 — `AppDbContext` „bez Usera”: Identity, Data Protection, `/health`, `Startup` kontra „brak Usera = wyjątek” — **Krytyczna**

**Jednostka A (E4, historia filtra):** aby spełnić AD-16 „brak zalogowanego, Aktywnego Usera = wyjątek, nie pusty wynik” do litery, `AppDbContext` dostaje w konstruktorze `ICurrentUser` i rzuca, gdy brak Usera (albo `SaveChanges` rzuca, gdy `UserId == null`).

**Jednostka B (E4, historie wdrożenia / logowania, i E5):** `AddEntityFrameworkStores<AppDbContext>()`, `PersistKeysToDbContext<AppDbContext>()`, `AddDbContextCheck<AppDbContext>()` dla `/health`, `Startup` (migracje, pierwszy Administrator) — wszystkie rozwiązują `AppDbContext` z DI, bez Usera (logowanie z definicji jest przed zalogowaniem). Wynik: logowanie, `/health` po wdrożeniu (AD-20) i odczyt kluczy DP padają. Wariant odwrotny: A robi filtr `user_id = UserId` z `UserId == null` → kontekst systemowy po cichu widzi zero wierszy, a `SaveChanges` nadaje `user_id = null`/0 — `Accounts/DeleteAccount` bez `IgnoreQueryFilters()` w jednym zapytaniu usuwa „nic” i potem pada na FK `RESTRICT` (albo, gdy ktoś dopisze `IgnoreQueryFilters()` „bo nie działało”, powstaje trzecie, nienazwane miejsce omijające filtr).

**Dlaczego zgodne z AD:** AD-16 każe rzucać bez Usera, AD-19 każe trzymać klucze DP w tym samym `AppDbContext`, AD-15 każe używać Identity na tym kontekście; żaden AD nie rozróżnia trybu systemowego.

**Nowa reguła (AD-16):** `AppDbContext` ma dwa jawne tryby: *użytkownika* (tylko przez `UserDbContextFactory`, `UserId` ustawiony) i *systemowy* (rejestracja DI, dla Identity, DP, health-check, `Startup`, `Accounts/`). W trybie systemowym filtr na encjach z `user_id` daje `false` (zero wierszy), a `SaveChanges` rzuca przy dodaniu/zmianie takiej encji; tabele `users`/`roles`/`data_protection_keys` nie mają filtra. Konstruktor nie rzuca nigdy; wyjątek „brak Usera” rzuca `UserDbContextFactory`. Test: tryb systemowy nie zwraca wierszy z `time_entry` i odrzuca ich zapis.

---

## Wysokie

### D3 — Lista tabel z `user_id` wpisana na sztywno w trzech miejscach — **Wysoka**

**A (E4):** globalny filtr dokładnie na czterech encjach wymienionych w AD-16; `DeleteAccount` (E5, AD-18) usuwa dokładnie: Wpisy → Słowniki → `account_settings` → Konto.
**B (E3 lub E5 po pierwszym wdrożeniu):** dodaje nową tabelę per User (np. zapamiętany ostatni Miesiąc eksportu, preferencje widoku) nową migracją dodającą (AD-21 to dopuszcza). Tabela nie jest na liście AD-16 → bez filtra (przeciek), a `DeleteAccount` jej nie zna → FK `RESTRICT` blokuje usunięcie Konta albo, bez FK, zostają sieroty. Po rollbacku (AD-20) stary obraz w ogóle nie zna nowej tabeli i jego `DeleteAccount` pada na FK.
**Reguła:** każda encja z `user_id` implementuje `IUserOwned`; filtr i nadawanie `user_id` stosowane w `OnModelCreating`/`SaveChanges` po interfejsie, nie po liście; `DeleteAccount` usuwa wszystkie `IUserOwned` w kolejności zależności FK wyliczonej z modelu; test potoku: każda encja z kolumną `user_id` jest `IUserOwned` i jest objęta testem oddzielenia. Nowe tabele per User po pierwszym wdrożeniu mają FK do `users` z `ON DELETE RESTRICT` (zgodnie z AD-18) i wymagają wpisu w teście usuwania Konta.

### D4 — `UserManager` w obwodzie = długo żyjący `AppDbContext`; `DeleteAccount` na dwóch kontekstach — **Wysoka**

**A (E5, ekran Użytkownicy):** AD-17 każe zmieniać stan „przez `UserManager`”, więc komponent wstrzykuje `UserManager<User>`. `UserManager` jest scoped, a scope w Interactive Server = cały obwód → jego `AppDbContext` żyje tyle co karta. Szybkie dwa kliknięcia „Zatwierdź” → „A second operation was started on this context”; po zmianie z innej karty (drugi Administrator, `psql`) ekran pracuje na śledzonych, nieaktualnych encjach — dokładnie to, czemu zapobiega AD-4, ale AD-4 obejmuje tylko `UserDbContextFactory`.
**B (E5, `Accounts/DeleteAccount`):** usuwa Wpisy kontekstem z `IDbContextFactory` (krótko żyjący, `IgnoreQueryFilters`), a Konto przez `UserManager.DeleteAsync` (kontekst obwodu). Dwa połączenia → „jedna transakcja, wszystko albo nic” z AD-18 nie zachodzi.
**To samo w E3:** zmiana własnego hasła w Ustawieniach konta przez wstrzyknięty `UserManager`.
**Reguła (AD-4 + AD-15):** każda operacja na Kontach w obwodzie tworzy nowy scope (`IServiceScopeFactory.CreateAsyncScope()`) i bierze z niego `UserManager`/`SignInManager`; operacje żyją w `Accounts/` (`AccountAdmin`, `AccountSettingsOps`, `DeleteAccount`), komponent nigdy nie wstrzykuje `UserManager`. `DeleteAccount` używa `AppDbContext` z tego samego scope co `UserManager` i jednej `BeginTransactionAsync` na nim.

### D5 — Zmiana własnego hasła w obwodzie wylogowuje Usera po minucie — **Wysoka**

**A (E3, Ustawienia konta, R25):** interaktywny formularz (AD-8 każe go walidować Blazillą — więc jest interaktywny), `ChangePasswordAsync` → zgodnie z AD-15 aktualizuje security stamp.
**B (E4, AD-17):** `RevalidatingServerAuthenticationStateProvider` co minutę porównuje stamp z ciasteczka z bazą → stamp się różni → obwód traci uwierzytelnienie, następne żądanie HTTP z ciasteczkiem też (gdy włączony `SecurityStampValidator`). User zmienia hasło i jest wylogowany. W obwodzie nie da się wywołać `RefreshSignInAsync` (brak odpowiedzi HTTP, AD-15 sam to mówi). Ten sam błąd w E4 na statycznej `/konto/zmiana-hasla`, jeśli historia zapomni `RefreshSignInAsync` po zdjęciu `must_change_password`.
**Reguła (AD-15):** każda zmiana *własnego* hasła (wymuszona i z Ustawień konta) odbywa się na statycznej stronie SSR pod `/konto/...` (np. `/konto/zmiana-hasla` w dwóch trybach: wymuszona / dobrowolna z obecnym hasłem) i kończy się `RefreshSignInAsync`; Ustawienia konta pokazują tylko link do niej. Ustawienia konta w AD-8 obejmują tylko prefiks. Test: po zmianie hasła User po 2 minutach jest dalej zalogowany.

### D6 — Wymuszona zmiana hasła: przekierowanie bez nazwanego mechanizmu i z niepełną listą wyjątków — **Wysoka**

**A (E4):** middleware po `UseAuthentication`: gdy `must_change_password`, każde żądanie poza `/konto/zmiana-hasla` i `/konto/wyloguj` → 302. Do litery AD-15 przekierowuje też `/_framework/blazor.web.js`, `/_content/Microsoft.FluentUI...` (CSS strony zmiany hasła), `/favicon`, `/_blazor` i **`/pomoc/zmiana-hasla`** — temat, który E5 pisze (R26) i który AD-22 każe udostępnić bez logowania. Strona zmiany hasła renderuje się bez stylów, a pomoc do niej jest nieosiągalna.
**B (E1, MainLayout, albo E4 przez `CurrentUser`):** drugi mechanizm — sprawdzenie w `MainLayout`/`Routes` i `NavigateTo`. Nawigacja wewnątrz obwodu nie robi żądania HTTP, więc middleware A jej nie widzi; mechanizm B z kolei nie działa dla endpointu Zestawienia. Każdy epik dopisuje swój.
**Reguła (AD-15):** jeden mechanizm: middleware dla żądań HTTP *stron i endpointów* (rozpoznanie po metadanych endpointu, nie po ścieżce: zasoby statyczne `MapStaticAssets`, `/_framework`, `/_blazor`, `/health` i trasy `[AllowAnonymous]` — w tym anonimowe tematy pomocy — są wyłączone) plus kontrola `CurrentUser` w operacjach (AD-17), która w obwodzie robi `NavigateTo("/konto/zmiana-hasla", forceLoad: true)`. Lista wyjątków w AD z nazwy; test na każdą pozycję listy.

### D7 — AD-17 „UI przechodzi na `/konto/wyloguj`” kontra wylogowanie tylko POST i zakaz JS; co zwraca endpoint — **Wysoka**

**A (E4, `/konto/wyloguj`):** zgodnie z AD-11 statyczna strona/endpoint przyjmujący POST z antyforgery (jak w szablonie Identity).
**B (E4/E1, reakcja na zablokowane Konto w obwodzie, AD-17):** `NavigateTo("/konto/wyloguj", forceLoad: true)` → GET → 405 albo strona „czy wylogować?” wymagająca kliknięcia (auto-submit wymagałby JS, zabroniony w AD-1). Jeśli A doda obsługę GET, powstaje wylogowanie przez CSRF (`<img src=/konto/wyloguj>`). Z obwodu nie da się skasować ciasteczka, więc zablokowany User po przeładowaniu dalej ma ważne ciasteczko do czasu rewalidacji.
**C (E3, endpoint Zestawienia):** „stan Konta sprawdzany jak w AD-17” — ale AD-17 opisuje reakcję UI, nie HTTP; jedna historia zwróci 403 (cookie auth przekieruje na domyślne `/Account/AccessDenied` → 404), inna 500 z wyjątku `UserDbContextFactory`, inna pusty plik.
**Reguła (AD-17):** stan Konta i security stamp sprawdza na każdym żądaniu HTTP `CookieAuthenticationEvents.OnValidatePrincipal` (odczyt `status`, `must_change_password`, stamp; przy niezgodności `RejectPrincipal` + `SignOutAsync`) — to jedyne miejsce, które kasuje ciasteczko. W obwodzie `CurrentUser` przy niepowodzeniu rzuca `AccountInactiveException`, a `MainLayout` (E1) łapie ją i robi `NavigateTo("/konto/logowanie?powod=<stan>", forceLoad: true)`; walidator kasuje ciasteczko na tym żądaniu. `/konto/wyloguj` przyjmuje wyłącznie POST z antyforgery. Endpoint Zestawienia dla nieaktywnego Konta zwraca to samo co dla anonima: 302 na `/konto/logowanie`.

### D8 — Domyślna autoryzacja: `FallbackPolicy` kontra `[Authorize]` w `_Imports`; ścieżki logowania i treść „brak dostępu” — **Wysoka**

**A (E4):** „domyślnie każda trasa wymaga zalogowania” realizuje `AuthorizationOptions.FallbackPolicy`. Polityka obejmuje też endpointy `MapStaticAssets` i hub `/_blazor` → anonim nie dostaje CSS Fluent UI na stronie logowania i nie zestawi obwodu dla anonimowych tematów pomocy (AD-22). Ciasteczko bez skonfigurowanych `LoginPath`/`AccessDeniedPath` przekierowuje na `/Account/Login`.
**B (E1/E5):** `[Authorize]` w `_Imports.razor` + `AuthorizeRouteView`; zalogowany User bez roli wchodzi na `/uzytkownicy` (link z pomocy, zakładka) → `<NotAuthorized>` w `Routes` napisany przez E4 jako `RedirectToLogin` → strona logowania dla zalogowanego, pętla albo angielskie „Not authorized”. Dla `/pomoc/administracja` to samo.
**Reguła (konwencja „Autoryzacja”):** jeden mechanizm: `[Authorize]` w głównym `_Imports.razor` stron + `RequireAuthorization()` na każdym endpoincie poza `/health`; bez `FallbackPolicy` (albo z jawnym `.AllowAnonymous()` na `MapStaticAssets` i `/_blazor` — wybrać jedno). `LoginPath=/konto/logowanie`, `AccessDeniedPath=/konto/brak-dostepu` (nowa trasa w konwencji Tras). `<NotAuthorized>` w `Routes`: anonim → przekierowanie z `forceLoad` na logowanie z `ReturnUrl`; zalogowany bez roli → polska treść „brak dostępu” w `MainLayout`.

### D9 — Link do Zestawienia przechwytuje router Blazora — **Wysoka**

**A (E3, AD-14):** „Ekran Eksport i przycisk w Widoku miesiąca to zwykły link do endpointu”: `<a href="/zestawienie/2026/9">`.
**B (E1, AD-11, `Routes`/`NotFound`):** w trybie globalnym Interactive Server router przechwytuje kliknięcia linków wewnętrznych; `/zestawienie/...` nie jest komponentem → router renderuje polską stronę „nie znaleziono” zamiast pobrać plik. Obie jednostki dosłownie zgodne z AD-11 i AD-14.
**Reguła (AD-14):** link do endpointu ma atrybut `download` (router go nie przechwytuje) — albo komponent `Shared/DownloadLink`, który to gwarantuje; test bUnit sprawdza atrybut. Zakaz `NavigateTo` do endpointu.

### D10 — Pomoc: jedna trasa `/pomoc/{temat}` z różnym dostępem per wartość parametru; strony `/konto` bez belki — **Wysoka**

**A (E1, historia 1.3):** jeden komponent `@page "/pomoc/{temat}"` + `HelpTopics` (klucz → typ komponentu treści przez `DynamicComponent`). Atrybut `[Authorize]`/`[AllowAnonymous]` działa na komponent, nie na wartość `{temat}`, więc albo wszystkie tematy wymagają logowania (łamie AD-22 dla `/pomoc/logowanie`), albo wszystkie są anonimowe, a `/pomoc/administracja` chroni tylko `if` w kodzie, którego E1 nie ma powodu napisać (temat dopisuje E5).
**B (E5):** dopisuje `/pomoc/logowanie` i `/pomoc/administracja` jako osobne `@page` z atrybutami → dwie trasy dla tego samego tematu, spis z `HelpTopics` i trasy literalne się rozjeżdżają; albo dodaje klucz `Logowanie`, a E1 użył `logowanie` / `konto-logowanie` (konwencja klucza nieustalona).
**C (E4, `AccountLayout`):** AD-11 każe statycznym `/konto/...` mieć „prosty układ bez menu” — bez belki, więc bez ikony `?`, a AD-22 każe ikonie `?` w belce otwierać temat bieżącego ekranu. Strony logowania/rejestracji nie mają jak otworzyć pomocy. Anonim na `/pomoc/logowanie` dostaje `MainLayout` z menu Rejestr/Słowniki i przyciskiem Wyloguj.
**Reguła (AD-22):** jedna trasa `/pomoc/{temat}` oznaczona `[AllowAnonymous]`; o dostępie decyduje sam komponent według `HelpTopics.Access` (Anonim / Zalogowany / Administrator), brak dostępu → ten sam widok co D8. Klucz tematu = segment trasy ekranu w kebab-case (`projekty`, `typy-pracy`, `wpisy-nowy`, `logowanie`, `rejestracja`, `zmiana-hasla`, `administracja`), stałe w `HelpTopics`, zakaz osobnych `@page "/pomoc/..."`. `AccountLayout` ma link „Pomoc” do tematu strony. Tematy anonimowe renderują się w `AccountLayout` (bez menu), pozostałe w `MainLayout`.

### D11 — Migracje równoległych epików po pierwszym wdrożeniu — **Wysoka**

**A (E5, „może iść równolegle z epikami 1–3”):** dodaje migrację (np. kolumnę do notatki odrzucenia, indeks na `status`).
**B (E2/E3 w tym samym czasie):** dodaje własną migrację. Obie gałęzie zmieniają `AppDbContextModelSnapshot.cs`; git scala go tekstowo „czysto” albo z konfliktem, a plik `.Designer.cs` każdej migracji zawiera model bez zmian drugiej. Następne `migrations add` generuje fantomowe zmiany; EF Core 9+ przy `Migrate()` zgłasza `PendingModelChangesWarning` jako błąd → start pada na serwerze (AD-21 krok 2), workflow kończy się błędem `/health`. Obie gałęzie przechodzą PR osobno, bo kontrola migracji z AD-20 sprawdza tylko „czy dodająca”.
**Także w E4:** AD-7 mówi „przepisuje migrację `Initial`”, AD-21 „od pierwszego wdrożenia tylko dodające” — jeśli historia wdrożenia w E4 idzie przed historią rejestracji/`must_change_password`, `Initial` jest już na serwerze, a schemat jeszcze niepełny; „pierwsze wdrożenie” nie jest zdefiniowane.
**Reguła (AD-21):** (1) `Initial` jest zamrożona w historii E4, która jako pierwsza uruchamia workflow wdrożenia, i ta historia zawiera *cały* schemat z AD-7; (2) potok PR uruchamia `dotnet ef migrations has-pending-model-changes` i sprawdza, że gałąź jest aktualna względem `main` w części `Data/Migrations` (wymóg „branch up to date” w ochronie `main`); przy konflikcie migracja jest usuwana i generowana od nowa na `main`; (3) `migrationBuilder.Sql(...)` po `Initial` zakazane albo oznaczone komentarzem do ręcznego przeglądu, bo kontrola „tylko dodające” nie parsuje SQL; (4) nowy indeks unikalny na istniejących danych liczy się jako zmiana ryzykowna (może wywrócić start).

---

## Średnie

### D12 — Nagłówek treści, temat pomocy i „Odśwież”: brak kontraktu ekran ↔ `MainLayout` — **Średnia**

**A (E1, 1.3):** `MainLayout` renderuje tytuł, podtytuł, okruszki, `Odśwież` i `?` na podstawie mapy „trasa → tytuł/temat” w `MainLayout`.
**B (E2, Lista wpisów):** renderuje własny nagłówek w treści i własny `Odśwież` (bo tylko ekran wie, co przeładować), a temat pomocy ustawia przez scoped serwis. Trasy z parametrami (`/miesiac/{rok}/{miesiac}`) nie pasują do mapy A → `?` otwiera spis tematów zamiast tematu ekranu.
**Reguła (AD-11 + AD-22):** `Shared/PageHeader` (Title, Subtitle, Breadcrumbs, HelpTopic, OnRefresh) — jedyny sposób, w jaki ekran zgłasza nagłówek; przekazuje dane do `MainLayout` przez kaskadowy `PageContext`; `?` i `Odśwież` w belce/nagłówku czytają z niego. Ekran bez `PageHeader` nie przechodzi testu bUnit szablonu ekranu.

### D13 — Klucze Data Protection: kolejność względem migracji i rejestracja kontekstu — **Średnia**

**A (E4, `Startup`):** migracje i pierwszy Administrator jako `IHostedService` („przed przyjęciem pierwszego żądania” — AD-21 to dopuszcza).
**B (E4, AD-19):** `PersistKeysToDbContext<AppDbContext>()`; `DataProtectionHostedService` startuje wcześniej i na świeżej bazie czyta nieistniejącą tabelę → błąd w logu i klucz w pamięci, a po wdrożeniu na pustej bazie (odtworzenie na nowy Droplet, AD-19) pierwsze ciasteczka są szyfrowane kluczem, który nie trafił do bazy → po restarcie wszyscy wylogowani; w testach Testcontainers losowe błędy przy pierwszym teście. Dodatkowo `PersistKeysToDbContext` wymaga `AppDbContext` w DI (wiąże się z D2) i że `DbSet<DataProtectionKey>` nie jest objęty filtrem.
**Reguła (AD-21 + AD-19):** kroki 1–3 wykonywane synchronicznie w `Program.cs` po `Build()`, przed `app.Run()`; zakaz `IHostedService` dla migracji. Tabela `data_protection_keys` w `Initial`, bez filtra, poza `IUserOwned`; test: dwa kolejne starty aplikacji na tej samej bazie odczytują to samo ciasteczko.

### D14 — `account_settings`: dwie ścieżki tworzenia wiersza i brak wiersza dla pierwszego Administratora — **Średnia**

**A (E4, rejestracja):** tworzy wiersz `account_settings` razem z Kontem (pusty prefiks).
**B (E3, Ustawienia konta):** przy zapisie robi `Add` nowego wiersza, gdy go „nie ma w kontekście” → naruszenie unikalności `user_id` dla Kont z rejestracji; endpoint Zestawienia (E3) zakłada istnienie wiersza → `NullReferenceException` dla pierwszego Administratora, którego tworzy `Startup` (AD-21) bez rejestracji. Klucz tabeli (osobne `id` czy `user_id`) nieustalony.
**Reguła (AD-7):** `account_settings` ma klucz główny `user_id` (1:1), wiersz opcjonalny; jedyny odczyt to `AccountSettingsQuery.GetAsync()` zwracający wartości domyślne przy braku wiersza; jedyny zapis to upsert w Ustawieniach konta. Ani rejestracja, ani `Startup` nie tworzą wiersza.

### D15 — `DictionaryOptions` nie zna trybu kopii; Lista wpisów potrzebuje nieaktywnych — **Średnia**

**A (E1, 1.9):** `DictionaryOptions.LoadAsync(TimeEntry? editedEntry)` — „aktywne plus bieżąca wartość edytowanego Wpisu”.
**B (E2, kopia z R7):** `EntryForm` w trybie kopii przekazuje oryginalny Wpis (bo z niego bierze pola) → nieaktywny Projekt pojawia się w opcjach i zostaje zapisany w nowym Wpisie; R7 wymaga pustego pola. Drugi wariant: E2 do filtrów Listy wpisów (R8: „także nieaktywnych”) bierze `DictionaryOptions`, bo to „jedyne źródło opcji” wg AD-10 → filtr bez nieaktywnych.
**Reguła (AD-10):** sygnatura `DictionaryOptions.LoadAsync(int? keepProjectId, int? keepWorkTypeId)`; tryb kopii i nowy Wpis przekazują `null` i czyszczą pole, gdy wartość nie jest w opcjach; filtry raportów biorą wszystkie pozycje Usera z osobnego `DictionaryOptions.LoadAllAsync()` (bez filtra aktywności).

### D16 — Kontrakt akcji w `ReportGrid`: trzy stałe ikony kontra akcje Konta zależne od stanu — **Średnia**

**A (E1, 1.5):** `ReportGrid<T>` z parametrami `OnEdit`, `OnCopy`, `OnDelete` (`EventCallback<T>`) — opis historii wymienia tylko te trzy.
**B (E5):** potrzebuje Zatwierdź/Odrzuć (tylko `pending`), Zablokuj (`active`), Odblokuj (`blocked`), Ustaw hasło, Usuń (`blocked`), ukrytych dla własnego wiersza. Dopisuje `OnApprove`, `OnBlock`... i predykaty widoczności → przeróbka komponentu z epiku 1 w epiku równoległym (konflikt z E2/E3, którzy w tym czasie go używają).
**Reguła (AD-9):** akcje wiersza to `IReadOnlyList<RowAction<T>>` (ikona, etykieta po polsku, `Func<T,bool> Visible`, `Func<T,Task> Execute`); edycja/kopia/usunięcie to gotowe fabryki w `Shared/`. Historia 1.5 buduje ten kontrakt od razu.

### D17 — Operacje Administratora rozproszone po ekranie; `Accounts/` omija kontrolę stanu — **Średnia**

**A (E5):** AD-17 każe „zmieniać `status`, hasło i stamp przez `UserManager`”, AD-18 każe „sprawdzać w operacji” blokadę własnego Konta — ale jedyną nazwaną operacją jest `DeleteAccount`. Zatwierdź/Zablokuj/Odblokuj/Ustaw hasło trafiają do handlerów w `Features/Uzytkownicy` — kontrola „nie własne Konto” i aktualizacja stampa są w czterech miejscach albo w żadnym.
**B (E4, AD-17):** kontrola stanu bieżącego Usera siedzi w `UserDbContextFactory`; ekran Użytkownicy i Ustawienia konta (hasło) nie używają tej fabryki, więc zablokowany w międzyczasie Administrator (drugi Administrator go zablokował) dalej zatwierdza Konta z otwartego obwodu do minuty.
**Reguła (AD-17 + AD-18):** `Accounts/AccountAdmin` z metodami `ApproveAsync`, `RejectAsync`, `BlockAsync`, `UnblockAsync`, `SetPasswordAsync` (ustawia `must_change_password`, czyści lockout, aktualizuje stamp) — każda zaczyna od `CurrentUser.RequireActiveAsync(role: Administrator)`, odrzuca własne Konto i działa w świeżym scope (D4). Ustawienie hasła na własnym Koncie też zabronione (przez ekran Użytkownicy).

### D18 — `DbErrors` zależy od nazw ograniczeń wybranych w innym epiku; normalizacja kodu tylko w `SaveChanges` — **Średnia**

**A (E4, `Initial`):** indeks `(user_id, code)` nazwany konwencją EF (`ix_project_user_id_code`), indeks funkcyjny Typu pracy dodany SQL-em z nazwą wymyśloną w migracji.
**B (E1, 1.7/1.8):** `Data/DbErrors` tworzony w 1.7 rozpoznaje duplikat po nazwie ograniczenia (żeby odróżnić duplikat kodu od innych naruszeń) z nazwą zgadniętą inaczej → duplikat pokazany jako ogólny błąd. Do tego weryfikacja 1.7 mówi o unikalności `(user_id, lower(code))`, a AD-7 o `(user_id, code)` z normalizacją w `SaveChanges`; polecenie `psql INSERT` z README (w weryfikacji 1.7, a w AD-19 dostęp Administratora do bazy) omija `SaveChanges` → `rdz` i `RDZ` współistnieją, łamiąc R1.
**Reguła (AD-7):** nazwy ograniczeń unikalności i FK jako stałe w `Data/DbConstraints`, użyte w migracji i w `DbErrors`; `DbErrors.IsUniqueViolation(ex, constraint)`. Unikalność kodu: indeks `(user_id, upper(code))` plus `CHECK (code = upper(code))` — baza, nie `SaveChanges`, jest strażnikiem.

### D19 — Limit prób według IP za Caddy — **Średnia**

**A (E4, logowanie):** `[EnableRateLimiting]` z partycją po `RemoteIpAddress`, middleware limitera w kolejności z szablonu.
**B (E4, wdrożenie, AD-19):** `UseForwardedHeaders` z „zaufaną siecią compose w `KnownIPNetworks`”, ale podsieć sieci compose przydziela Docker losowo; gdy nie pasuje albo `UseForwardedHeaders` stoi za limiterem, wszyscy mają IP Caddy → pięć nieudanych prób kogokolwiek blokuje rejestrację i logowanie wszystkim. Limiter na stronie SSR liczy też GET (samo otwarcie strony).
**Reguła (AD-19 + AD-15):** `deploy/compose.yml` definiuje sieć z jawną podsiecią, ta sama wartość w konfiguracji `KnownIPNetworks`; `UseForwardedHeaders` jako pierwszy middleware; limiter liczy tylko POST formularzy logowania i rejestracji; test integracyjny z nagłówkiem `X-Forwarded-For`.

---

## Niskie

### D20 — Reprezentacja `status` — **Niska**

E4 mapuje `enum AccountStatus` domyślnie na `int`; E5 w `docs/admin/` (R27) i ćwiczenie odtworzenia opisuje `UPDATE users SET status='active'`, a AD-7 wymienia wartości tekstowe. **Reguła:** `HasConversion<string>()` z małymi literami i `CHECK (status IN ('pending','active','blocked'))` w `Initial`.

### D21 — `DeleteEntry` i kontrakt `EntryForm` między E2 a E3 — **Niska**

E2 umieszcza `DeleteEntry` i stan „ostatnio ustawionej daty” w `Features/Wpisy`; E3 (`Features/Miesiac`) musi zależeć od innej funkcji, czego diagram zależności nie przewiduje (Features → Shared). `EntryForm` przyjmuje w E2 `EntryId`, a E3 ma pod ręką `MonthEntry`. **Reguła (AD-10):** `DeleteEntry`, `EntrySession` (scoped, ostatnia data) i `EntryForm` w `Shared/`; `EntryForm` w trybie edycji/kopii przyjmuje wyłącznie `EntryId` i sam ładuje Wpis.

### D22 — Suma pod Listą wpisów a filtr kolumn w siatce — **Niska**

AD-5 każe liczyć sumę Listy „z tej samej materializowanej listy co wiersze”, AD-9 mówi, że filtr siatki tylko ukrywa wiersze i siatka nie wystawia przefiltrowanego zbioru. E2 może pokazać sumę całości przy licznikach po filtrze (licznik i suma się nie zgadzają) albo dopisać do `ReportGrid` zdarzenie z widocznymi wierszami. **Reguła:** jawnie: suma Listy wpisów dotyczy wyników filtrów ekranu (zakres dat, Projekt, Typ pracy), nie filtrów kolumn siatki; podpis sumy to mówi.

### D23 — Krok 3 startu w testach i lokalnie; role Autora — **Niska**

AD-21 kończy proces, gdy nie ma Administratora i brak loginu/hasła w konfiguracji → każdy test na `WebApplicationFactory` i pierwszy `dotnet run` po przepisaniu wolumenu pada, dopóki ktoś nie doda sekretów do testów (pokusa wpisania ich do `appsettings.json`, łamie AD-13). R20 wymaga ról User *i* Administrator dla Konta Autora, AD-21 mówi tylko „z rolą Administrator”. **Reguła:** projekt testów ustawia konfigurację Administratora w kodzie; README podaje klucze do `appsettings.Local.json`; Startup nadaje obie role.

---

## Uwagi poboczne (poza parami)

- Historia 1.1 w `tickets.toml` epiku 1 nadal odwołuje się do `#ad-3` (wycofane), a historia 1.2 (Kopia bazy) wciąż jest na liście, choć AD-3 i R14 są wycofane — porządek w ticketach, nie w spine.
- Historia 1.3 weryfikuje „Wyloguj wraca do ekranu logowania”, ale przycisk Wyloguj zależy od kontraktu z D7 (POST + antyforgery w komponencie interaktywnym: `<form method="post" action="/konto/wyloguj">` z `<AntiforgeryToken />` i bez `@onsubmit`); warto to wpisać do AD-11.
