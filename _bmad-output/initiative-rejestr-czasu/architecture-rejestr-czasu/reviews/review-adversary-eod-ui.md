---
type: review
lens: adversary
target: architecture-rejestr-czasu.md (aktualizacja 02.11.2026, przejęcie wzorca eod-ui)
changed-ads: [AD-1, AD-5, AD-9, AD-10, AD-11, AD-12, AD-17, AD-22]
date: '2026-11-02'
evidence:
  - ui (eod-ui @ 8e66aea): src/Eod.Platform.UI/Reports/{EodReport.razor(.cs), ReportDefinition.cs, ReportLayout.cs, ReportEngine.cs}
  - ui: Messages/UiMessages.cs, Layout/{EodShell,EodHead}.razor, Page/EodPage.razor, ServiceCollectionExtensions.cs, Directory.{Build,Packages}.props, docs/UI_SPEC.md (6, 7, 9.7, 9.9, 9.13, 11, 12)
  - app: src/RejestrCzasu/{Program.cs, Components/App.razor, Shared/MainLayout.razor, Shared/Notify.cs, Features/Pomoc/HelpLayout.razor, Features/Wpisy/EntriesPage.razor, Accounts/AccountLayout.razor}, Dockerfile, .dockerignore, .github/workflows/pr.yml
---

# Przegląd adwersarza — spine po przejęciu wzorca eod-ui

## Werdykt

Spine jest spójny dla jednej historii, ale **nie dla dwóch historii budowanych równolegle lub kolejno w różnych epikach**: zmienione AD odsyłają do API eod-ui, którego jeszcze nie ma (sumy od ekranu, blokada układu, wyłączenie eksportu, format Czasu, wyjątek Enter), i nie ustalają jego kształtu, więc epik 7 i epik 3 wymyślą dwa różne kontrakty. Do tego AD-11 importuje UI_SPEC w całości, a UI_SPEC 9.13/12 wprost zabrania `EodReport` dla list, które AD-9 każe na nim zbudować. Trzeba domknąć 11 dziur; 2 krytyczne, 5 wysokich.

Metoda: dla każdej dziury dwie jednostki o jeden poziom niżej (historie), każda zgodna z literą AD, a mimo to niezgodne ze sobą. Każda para kończy się proponowanym tekstem reguły (nowej lub zaostrzonej).

---

## H1 — AD-9 kontra UI_SPEC 9.13/12 przez AD-11: lista Słownika i lista Kont na `EodReport` czy na `FluentDataGrid`/`EodEditableTable` — **krytyczna**

**Fakty.** AD-9: „każdy raport …, lista Słownika i lista Kont na ekranie Użytkownicy używa `EodReport`”. AD-11: „wygląd i zachowanie UI według UI_SPEC (… tabele 12 …), z wyjątkami z tego AD”. UI_SPEC 12: „Lista administracyjna (konta, role, struktura): `FluentDataGrid` … Nie `EodReport` – ten jest dla raportów”. UI_SPEC 9.13 A: Użytkownicy = `FluentDataGrid` z paskiem filtrów i paginacją; 9.13 B: słownik = `EodEditableTable` + `EditSession` z zapisem zbiorczym. AD-11 nie wymienia tego jako wyjątku (wyjątek leży w AD-9, a AD-11 mówi „z wyjątkami z **tego** AD”).

**Para.**
- *Epik 5, historia „Użytkownicy”* czyta AD-11 → UI_SPEC 9.13 A: `FluentDataGrid`, filtry `FluentSelect` nad tabelą, licznik „1 użytkownik z 17”, menu „Więcej”.
- *Epik 7, historia „Słowniki na wzorzec”* czyta AD-9: `EodReport` z kolumną `List` „Aktywny” i edycją w oknie. Ktoś inny w tym samym epiku czyta UI_SPEC 9.13 B i robi `EodEditableTable` z zapisem zbiorczym — co łamie AD-7/AD-8 (walidator per formularz, unikalność z `DbErrors` po pojedynczym zapisie) i R3 (komunikat z liczbą Wpisów przed usunięciem).
- Wynik: trzy różne listy administracyjne, dwa sposoby filtrowania (R17 „filtr wartości z licznikami” istnieje tylko w `EodReport`), dwa sposoby zapisu.

**Proponowana reguła (AD-11, nowe zdanie po „z wyjątkami z tego AD”):**
> Pierwszeństwo: przy sprzeczności z UI_SPEC wygrywa ten spine. Odstępstwa od UI_SPEC obowiązujące w Rejestrze: (1) listy Słowników i lista Kont to `EodReport` (AD-9), nie `FluentDataGrid` ani `EodEditableTable` z 9.13 — z 9.13 A brana jest tylko anatomia akcji wiersza (Edytuj + Więcej, czynności zależne od stanu) i okna edycji; `EodEditableTable`, `EditSession` i zapis zbiorczy są w Rejestrze zakazane; (2) zmiana hasła wyłącznie na `/konto/zmiana-hasla` (AD-15), nie w oknie z 9.9; (3) bez menu użytkownika z 6 — akcje w `HeaderActions` (AD-11); (4) potwierdzenia według AD-11 (bez Enter). Każde nowe odstępstwo dopisuje się do tej listy w tej samej zmianie.

---

## H2 — Sumy: `EodReport` liczy je sam, AD-5 tego zakazuje, a kształtu zamiennika nikt nie ustala — **krytyczna**

**Fakty.** `ReportEngine.Summary(Definition, rows)` liczy „N pozycji · {Total.Label}: {suma}” z `Definition.Total` — w stopce (z `_filteredRows`) i w każdym wierszu grupy (`ReportEngine.Summary(Definition, g.Rows)`). Brak typu kolumny dla Czasu: `Number` pokazuje surowe `int` (90), `Text` („1h 30m”) sortuje się alfabetycznie i filtruje „zawiera”. Nie ma parametru zwrotnego z wierszami po filtrach. AD-5/AD-9: „sumy grup i stopki podaje ekran”; „brakujące parametry … dodaje się w eod-ui” — bez nazwy, sygnatury ani właściciela.

**Pary.**
1. *Epik 7, „Lista wpisów na EodReport”* dodaje w eod-ui `[Parameter] string? FooterSummary` (gotowy tekst). *Epik 3, „Widok miesiąca”* potrzebuje sum per grupa, więc dodaje `Func<ReportGroup, string>? GroupSummary`. `ReportGroup.Value` to **tekst wyświetlany** (`Display`), a `Id` to ścieżka sklejona przez silnik — ekran musi parsować „02.11.2026” albo etykietę tygodnia z powrotem do klucza `MonthReport`. Trzecia osoba w epiku 5 ustawia `Definition.Total` (bo istnieje i działa) — zgodnie z literą AD-9? Nie, ale ekran „Użytkownicy” nie ma Czasu, więc reguła go nie dotyczy, i silnik sam liczy sumę liczby Wpisów. Trzy kontrakty w jednej bibliotece.
2. *Lista wpisów (R8)* „pokazuje liczbę Wpisów oraz sumę Czasu”, filtruje po Projekcie i Typie pracy. Jeśli filtry Projekt/Typ pracy są filtrami kolumn `EodReport` (AD-9: kolumny filtrowane po wartościach mają typ `List`), to licznik w stopce jest „po filtrach”, a suma podana przez ekran jest **przed filtrami** — ekran nie zna `_filteredRows`. Historia A trzyma zakres dat, Projekt i Typ pracy jako filtry ekranu (zapytanie) i dubluje je filtrami kolumn; historia B przenosi je do `EodReport` i suma rozjeżdża się z licznikiem. AD-9 „Prevents: sumom liczonym z przefiltrowanych wierszy” mówi coś przeciwnego niż R8 dla Listy.
3. *Czas w kolumnie:* historia A daje `ColumnType.Number` z minutami (filtr Od–Do w minutach, wyświetla „90”), historia B daje `Text` z `Czas.Format` (sortowanie „10h” < „2h”). Obie zgodne z AD-6 („jedyne miejsce konwersji `Domain/Czas`”).

**Proponowana reguła (nowy AD-23 „Kontrakt `EodReport` w Rejestrze”, punkt sumy; AD-5 i AD-9 odsyłają do niego):**
> Rozszerzenia eod-ui wymagane przez Rejestr, w jednej wersji, przed pierwszym ekranem na `EodReport`: (a) `ReportColumn.Format: Func<object?, string>?` — wyświetlanie, filtr i CSV używają go zamiast formatu typu; kolumna Czasu to `Number` z wartością `int` minut i `Format = Czas.Format` (sortowanie i filtr Od–Do po minutach, pola filtra z sufiksem „min”); (b) `[Parameter] Func<ReportGroupKey, string>? GroupSummary`, gdzie `ReportGroupKey` niesie **surowe** wartości kolumn grupujących od poziomu 0 (`IReadOnlyList<object?>`), nie tekst wyświetlany; (c) `[Parameter] Func<IReadOnlyList<IReadOnlyDictionary<string, object?>>, string>? FooterSummary` wywoływany z wierszami **po filtrach**; (d) gdy podano (b) lub (c), silnik nie liczy `Definition.Total` i Rejestr nie ustawia `Total` nigdy. Widok miesiąca: (b) i (c) zwracają sumy z `MonthReport` dla całego Miesiąca i ignorują argument wierszy (filtr tylko ukrywa). Lista wpisów: zakres dat jest filtrem ekranu (zapytanie), Projekt i Typ pracy to filtry kolumn `List`; (c) sumuje `Czas` z wierszy po filtrach, które dostaje — to jest „ta sama materializowana lista” z AD-5 i jedyny dopuszczony wyjątek. Tekst sumy formatuje wyłącznie `Czas`.

---

## H3 — Kształt wiersza: słowniki, klucze kolumn i to, jak `RowActions` dostaje `Id` — **wysoka**

**Fakty.** `Rows: IReadOnlyList<IReadOnlyDictionary<string, object?>>`, `RowActions: RenderFragment<IReadOnlyDictionary<string, object?>>`. Kolumna `IsKey` jest przypięta, zawsze widoczna i nieukrywalna (numer sprawy) — Rejestr nie pokazuje `Id`. Kolumna `Hidden` użytkownik może włączyć w menu „Kolumny” (i wtedy trafia do szukania i CSV). AD-9 mówi tylko „definicja zadana przez ekran”.

**Para.**
- *Epik 7, Lista wpisów:* `Id` w ukrytej kolumnie `"id"`, akcje robią `(int)row["id"]!`; klucze kolumn po angielsku `"date"`, `"project"`, `"workType"`, `"minutes"`.
- *Epik 3, Widok miesiąca:* klucze polskie `"data"`, `"projekt"`, `"czas"`, a w wierszu pod kluczem spoza definicji `"__entry"` cały `MonthEntry`; akcje robią `((MonthEntry)row["__entry"]!).Id`.
- Wspólny komponent akcji Wpisu (H6) nie może obsłużyć obu; User włącza kolumnę „id” na Liście i widzi numery z bazy; wartości `List` mają różne źródła tekstu (kod Projektu vs „kod – nazwa”), więc filtr Projektu w obu raportach wygląda inaczej.

**Proponowana reguła (AD-23, punkt wiersze):**
> Wiersz `EodReport` buduje wyłącznie statyczna metoda `ToRow(...)` obok definicji raportu w `Features/<Funkcja>/<Nazwa>Report.cs`; ekran nie składa słowników sam. Klucze kolumn: angielskie camelCase, wspólne dla Wpisu w każdym raporcie (`date`, `weekday`, `week`, `project`, `workType`, `description`, `minutes`); identyfikator rekordu leży pod kluczem zastrzeżonym `"$id"` (`int`), który **nie jest** kolumną definicji i nie trafia do szukania, filtrów ani CSV; `RowActions` czyta wyłącznie `row["$id"]`. Zakaz wkładania encji lub rekordów do wiersza i zakaz kolumny `IsKey` w Rejestrze. Wartość kolumny Projektu to zawsze kod Projektu, Typu pracy — nazwa (pl-PL).

---

## H4 — Stan układu: rekordowa równość definicji, klucz układu i filtry przenoszone między Miesiącami — **wysoka**

**Fakty.** `ReportLayoutStore.Get` tworzy układ od nowa, gdy `layout.Definition != definition`; `ReportDefinition` to `record` z `IReadOnlyList<…>`, więc równość list jest **referencyjna**. Klucz domyślny = `Definition.Key`. `ListFilter` przechowuje **wybrane** wartości — wartość, której nie było przy ustawianiu filtra, jest ukryta. Blokada układu (AD-9) nie istnieje we wzorcu i nie ma definicji, co blokuje (ukrywanie kolumn? „Przywróć układ”? szukanie?).

**Pary.**
1. *Lista wpisów* trzyma definicję w `static readonly` → filtry przeżywają przeładowanie po `RowActions`. *Widok miesiąca* buduje definicję w `OnParametersSet` (tytuł/opis z nazwą Miesiąca, `ValueOrder` z dni) → po każdej edycji z okna (przeładowanie, AD-4) filtry i zwinięte grupy znikają. Obie zgodne z AD-9 („układ żyje do odświeżenia strony”).
2. Widok miesiąca z jednym kluczem `"miesiac"`: filtr Projektu ustawiony w październiku przechodzi na listopad i chowa Wpisy z Projektem, którego w październiku nie było — sumy z `MonthReport` (cały Miesiąc) nie zgadzają się z widocznymi wierszami bez widocznego powodu. Inna historia daje `LayoutKey = $"miesiac@{rok}-{miesiac}"` i zachowuje się odwrotnie.
3. Na Liście User filtruje Projekt A, kopiuje Wpis na Projekt B → po przeładowaniu kopia jest niewidoczna (filtr `List`), a pasek sukcesu mówi „Skopiowano”.
4. Blokada: historia A blokuje sortowanie, grupowanie i kolejność; historia B także menu „Kolumny” i „Przywróć układ”; historia C chowa pasek układu.

**Proponowana reguła (AD-23, punkt układ):**
> Każda `ReportDefinition` w Rejestrze to `static readonly` pole klasy `…Report`; zakaz tworzenia definicji w cyklu życia komponentu (tytuł i opis strony daje `EodPage`, nie definicja). `Definition.Key` = segment trasy ekranu (`wpisy`, `miesiac`, `projekty`, `typy-pracy`, `uzytkownicy`); `LayoutKey` podaje tylko Widok miesiąca: `miesiac@RRRR-MM` (osobny układ i filtry per Miesiąc). Blokada układu to jeden parametr eod-ui `LockedLayout`: wyłącza zmianę sortowania, grupowania, kolejności i widoczności kolumn oraz „Przywróć układ”; szukanie, filtry i zwijanie grup działają. Po zapisie z `RowActions`, gdy zapisany rekord nie przechodzi aktywnych filtrów, ekran pokazuje pasek informacji „Wpis jest ukryty przez filtry” z akcją „Wyczyść filtry”.

---

## H5 — eod-ui: dwa źródła commitów, równoległe podbicia, wersja Fluent i obraz bez `ui/` — **wysoka**

**Fakty.** AD-1: zmiana we wzorcu osobnym PR w eod-ui, potem podbicie podmodułu. Nie mówi, **z której gałęzi eod-ui** wolno przypiąć commit, kto podbija, ani jak rozwiązać konflikt wskaźnika. `ui/Directory.Build.props` włącza Central Package Management, `ui/Directory.Packages.props` ma Fluent **5.0.0**; aplikacja ma `PackageReference` Fluent **5.0.1**. `Dockerfile` kopiuje tylko `src/RejestrCzasu/*.csproj` przed `dotnet restore` (restore `ProjectReference` do `../../ui/src/...` padnie), `.dockerignore` wyklucza `.git` (więc „Dockerfile pobiera podmoduły” jest niewykonalne — kontekst musi już zawierać `ui/`). `pr.yml` robi `actions/checkout@v7` bez `submodules`. Pamięć projektu: jeden PR na epik — epik 7 trafia na `main` jednym PR na końcu.

**Pary.**
1. *Epik 7, historia 7.x* potrzebuje w eod-ui `FooterSummary`; PR w eod-ui jeszcze w review, więc przypina commit z gałęzi `feature/footer` (AD-1 tego nie zabrania). *Epik 3* przypina commit z `feature/locked-layout`. Konflikt wskaźnika rozwiązany „wybierz nowszy” gubi parametr drugiego epiku; atrybut Razor bez pasującego parametru nie zawsze daje błąd kompilacji, a Blazor rzuca dopiero w działaniu („does not have a property matching the name”), więc błąd wychodzi na ekranie, nie w CI. Po squash-merge w eod-ui commit z gałęzi znika i podmoduł wskazuje na nieosiągalny commit.
2. *eod-ui* podbija Fluent do 5.0.2 (właściciel wersji = eod-ui) → aplikacja z bezpośrednim 5.0.1 dostaje NU1605 (obniżenie pakietu = błąd). Odwrotnie: aplikacja podbija do 5.0.2, eod-ui kompiluje się i testuje na 5.0.0. Spine mówi „5.0.1 (także w eod-ui)”, a dziś to nieprawda.
3. *Epik 3/5* startuje z `main`, na którym nie ma jeszcze `EodShell`, `UiMessages`, `EodHead` (epik 7 = jeden PR na końcu), więc buduje na `Shared/ReportGrid`, `Notify`, `Confirm` (AD-9 mówi „wycofany”, ale kod jest na `main`) albo wkłada `EodPage` do starego `MainLayout` (dwa miejsca komunikatów: `app-messages` i `eod-page`).

**Proponowana reguła (AD-1, zamiast „Zmiana we wzorcu idzie osobnym PR…”):**
> Podmoduł `ui/` wskazuje wyłącznie commit osiągalny z `origin/main` eod-ui; podbicie tylko do potomka bieżącego wskaźnika (CI: `git merge-base --is-ancestor <stary> <nowy>` i `git branch -r --contains <nowy>` zawiera `origin/main`). Konflikt wskaźnika rozwiązuje się podbiciem do bieżącego `main` eod-ui, nigdy wyborem jednej ze stron. Historia, która potrzebuje zmiany we wzorcu, ma ją zmergowaną w eod-ui przed swoim merge'em; listę wymaganych zmian eod-ui z właścicielem prowadzi AD-23. Wersję Fluent UI ustala wyłącznie `ui/Directory.Packages.props`; aplikacja nie ma własnego `PackageReference` do Fluent (dostaje go przez `ProjectReference`), a test potoku sprawdza, że w `obj/project.assets.json` jest jedna wersja Fluent. Obraz: kontekst budowania zawiera `ui/` (potok robi checkout z `submodules: recursive` przed `docker build`); `Dockerfile` kopiuje `ui/Directory.Build.props`, `ui/Directory.Packages.props` i `ui/src/Eod.Platform.UI/*.csproj` przed `restore`; `.dockerignore` wyklucza `ui/docs`, `ui/src/Eod.Demo`, `ui/src/*Syncfusion*`. Kolejność epików: historia fundamentu epiku 7 (podmoduł, CI, `Dockerfile`, `EodHead`, `MainLayout` na `EodShell`, `UiMessages`, usunięcie `Notify`/`Confirm`/`ReportGrid`/`PageHeader`) merguje się do `main` osobnym PR przed pierwszą historią epiku 3 i 5 — wyjątek od zasady „jeden PR na epik”.

---

## H6 — Okno Wpisu z dwóch raportów: kto przeładowuje, kto pokazuje wynik, gdzie trafia błąd — **wysoka**

**Fakty.** AD-10: okno z ikon edycji i kopii, „po zapisie okno się zamyka, raport wczytuje dane ponownie”; usunięcie przez `DeleteEntry` po `ConfirmAsync`. Dziś `Features/Wpisy/EntryDialogs` zwraca `Task<bool>`, a `EntriesPage` sam przeładowuje. `UiMessages` wysyła paski do sekcji `eod-page`, której provider siedzi w `EodPage` — **pod modalnym oknem**. `FluentProviders` (a więc okna) renderuje `EodShell` poza `ChildContent`, więc poza `AppErrorBoundary` z `MainLayout`.

**Pary.**
1. *Epik 3, Widok miesiąca* robi własne `MonthEntryActions` z `EventCallback OnChanged` i pokazuje pasek sukcesu w ekranie; *epik 7, Lista* zostaje przy `EntryDialogs` → `bool` i pasek w `EntryForm`. Po edycji z Widoku pasek pojawia się dwa razy albo wcale (zależnie od tego, kto zamknął okno przed `ShowSuccessAsync`).
2. *Błąd w oknie:* naruszenie unikalności, ogólny błąd ekranu albo `AccountInactiveException` przy zapisie z okna. Historia A pokazuje go przez `UiMessages.ShowErrorAsync` (AD-11 „wyłącznie”) — pasek ląduje pod modalem, User go nie widzi, okno wisi. Historia B wstawia `FluentMessageBar` w treść okna (łamie „wyłącznie `UiMessages`”). `AccountInactiveException` rzucony w treści okna omija `AppErrorBoundary` (AD-17 „MainLayout łapie”) i zrywa obwód zamiast przekierować na logowanie.

**Proponowana reguła (AD-10 i AD-11, zaostrzenie):**
> Akcje wiersza Wpisu (Edytuj, Kopiuj, Usuń) daje jeden komponent `Features/Wpisy/EntryRowActions` z parametrami `int EntryId` i `Func<Task> Reload`; używa go każdy raport z Wpisami. Komponent czeka na wynik okna, po sukcesie wywołuje `Reload`, a dopiero po nim pokazuje pasek sukcesu na stronie (okno już zamknięte); `EntryForm` w oknie nie pokazuje pasków. Błędy w oknie (walidacja, `DbErrors`, błąd ogólny) pokazuje okno: komunikat pod polem albo `EodMessageBar` w treści okna — jedyny wyjątek od „wyłącznie `UiMessages`”. `EntryRowActions` łapie `AccountInactiveException` i `PasswordChangeRequiredException` i przekazuje je tej samej obsłudze co `MainLayout` (wspólna metoda `Shared/AccountStateRedirect`), bo treść okna leży poza `AppErrorBoundary`.

---

## H7 — Temat pomocy: „usługa obwodu” kontra sekcje, wyścig z `LocationChanged` — **wysoka**

**Fakty.** AD-22: `Shared/HelpTopic` „ustawia temat w usłudze obwodu, z której czyta go przycisk `?` w belce `EodShell`”; strona `/konto` deklaruje temat w `AccountLayout`. Dziś działa to przez `SectionContent`/`SectionOutlet` (`MainLayout.HelpSection`, `AccountLayout.HelpSection`), które same czyszczą się przy usunięciu komponentu. Nie ma właściciela usługi ani zasady zerowania. `EodShell` obsługuje `LocationChanged` przez `_ = InvokeAsync(...)`, a Router renderuje nową stronę we własnym handlerze, zarejestrowanym wcześniej.

**Para.**
- *Historia A (epik 7, MainLayout)* tworzy `Shared/HelpTopicState` (Scoped) i zeruje temat w `LocationChanged`, żeby ekran bez `HelpTopic` nie dziedziczył cudzego tematu. Nowa strona ustawia temat w `OnInitialized` **przed** odłożonym handlerem → handler go zeruje → `?` zawsze prowadzi do spisu.
- *Historia B (epik 3, Ustawienia konta)* nie zeruje i nie subskrybuje zmiany — `?` nie odświeża się, bo belka nie wie, że stan się zmienił; albo kładzie usługę w `Features/Pomoc/` (AD-22 „Pomoc: `Features/Pomoc/`”) obok drugiej w `Shared/`.
- Strony `/konto` i interaktywne tematy anonimowe w `AccountLayout` (`HelpLayout` wybiera układ dynamicznie) nie mają obwodu lub usługi — trzeci mechanizm.

**Proponowana reguła (AD-22, zamiast „ustawia on temat w usłudze obwodu…”):**
> `Shared/HelpTopic` renderuje `SectionContent` do sekcji `help-topic` z przyciskiem `?` tego tematu; `SectionOutlet` tej sekcji stoi w `HeaderActions` `EodShell` (`MainLayout`) i w nagłówku `AccountLayout`, a domyślna treść sekcji (spis `/pomoc`) jest w obu układach. Bez usługi stanu tematu i bez obsługi `LocationChanged` dla pomocy; temat znika razem z komponentem strony. `HelpTopic` jest jedynym sposobem deklaracji tematu na ekranie interaktywnym i statycznym.

---

## H8 — Potwierdzenia: `ConfirmAsync` kontra wpisanie loginu, Enter jako zmiana globalna — **wysoka**

**Fakty.** AD-11: każde potwierdzenie (także „akcje Konta”) przez `UiMessages.ConfirmAsync`, bez Enter — „wyjątek od wzorca wprowadzany w eod-ui”. AD-18: usunięcie Konta `blocked` „po wpisaniu loginu”. `ConfirmAsync(title, messageHtml, primary, secondary, kind)` nie ma pola tekstowego; `CreateOptions` ma na sztywno `PrimaryShortCut = "Enter"`. `messageHtml` to HTML.

**Pary.**
1. *Epik 5, „Usuń Konto”* — zgodnie z AD-11 robi `ConfirmAsync` bez wpisywania loginu (łamie AD-18), albo zgodnie z AD-18 robi własne okno `Dialogs/DeleteAccountDialog` (łamie AD-11 „wyłącznie”).
2. *PR w eod-ui od epiku 7* usuwa `PrimaryShortCut` dla wszystkich (zmienia zachowanie każdej aplikacji EOD i UI_SPEC 11); *PR od epiku 5* dodaje parametr `bool primaryOnEnter = true`. Dwa sprzeczne PR do jednego pliku.
3. Ekran Projekty wkłada nazwę Projektu do `messageHtml` bez kodowania; ekran Użytkownicy koduje login. Nazwa z `<`/`&` psuje treść albo wstrzykuje znaczniki.

**Proponowana reguła (AD-11, zaostrzenie):**
> Potwierdzenie bez danych: `UiMessages.ConfirmAsync`; potwierdzenie z danymi (wyłącznie usunięcie Konta `blocked` z wpisaniem loginu, AD-18): okno `Features/Uzytkownicy/DeleteAccountDialog` w anatomii okna UI_SPEC 11 (fokus w polu, „Usuń” aktywne dopiero po zgodnym loginie, Escape = Anuluj). Wyjątek Enter wchodzi do eod-ui jako opcja `ConfirmAsync(..., primaryShortcut: null)` z domyślnym zachowaniem bez zmian; Rejestr woła `ConfirmAsync` wyłącznie przez `Shared/Confirm.AskAsync`, który ustawia ją zawsze. Każda wartość Usera w `messageHtml` i w treści paska przechodzi przez `WebUtility.HtmlEncode` (w `Shared/Confirm` i testem bUnit).

---

## H9 — `MainLayout` kontra `EodShell`: podwójne providery, lokalizator, kultura, motyw — **średnia**

**Fakty.** `EodShell` zawiera `<FluentProviders />` („aplikacja nie dodaje ich drugi raz”); dzisiejszy `MainLayout` też. `AddEodPlatformUI` woła `AddFluentUIComponents` z `options.Localizer = new PolishLocalizer()`; `Program.cs` woła `AddFluentUIComponents(options => options.Localizer = new FluentLocalizer())` (AD-6: „własny `IFluentLocalizer`”). `UseEodPolishCulture()` kontra własne `UseRequestLocalization(polish.Name)`. `MainLayout` ma przełącznik motywu `IThemeService`; AD-11: „skala i motyw przez `AppearanceSettings`”, UI_SPEC 9.9 kładzie je w Ustawieniach użytkownika (u nas Ustawienia konta, epik 3).

**Para.**
- *Epik 7, MainLayout* zostawia `FluentProviders`, `FluentLocalizer` i przełącznik w belce; *epik 3, Ustawienia konta* dodaje kartę „Wygląd” z `AppearanceSettings`. Dwa wpisy w `localStorage` motywu, dwa zestawy tekstów Fluent (który wygrywa, zależy od kolejności rejestracji), dwa providery okien (okno otwiera się dwa razy albo w złym miejscu).

**Proponowana reguła (AD-11 i AD-6):**
> `Program.cs` woła wyłącznie `AddEodPlatformUI()` i `UseEodPolishCulture()` — bez własnego `AddFluentUIComponents`, `UseRequestLocalization` i `RejestrCzasu.Localization.FluentLocalizer`; brakujące polskie teksty Fluent dopisuje się do `PolishLocalizer` w eod-ui. `FluentProviders` renderuje tylko `EodShell`. Skalę i motyw zmienia wyłącznie karta „Wygląd” na `/ustawienia-konta` przez `AppearanceSettings` (zapis w przeglądarce, nie w `account_settings`); belka nie ma przełącznika motywu. `MainLayout` = `EodShell` + `AppErrorBoundary` wokół `@Body` + `ReconnectModal` + `blazor-error-ui`; nic więcej.

---

## H10 — Statyczne `/konto` i `EodHead`/CSP — **średnia**

**Fakty.** `EodHead` renderuje inline `<script>` skali (zablokowany przez `script-src 'self'`, AD-12 zakazuje hashy) i linkuje arkusz Fluent, który `App.razor` już linkuje przez `@Assets[...]`. AD-11 każe użyć `EodHead` w `AccountLayout` — ale układ renderuje się w `<body>`, a `<head>` jest jeden, w `App.razor`, wspólny dla stron statycznych i interaktywnych. `UiMessages` bez obwodu nic nie pokaże, a AD-11 mówi „komunikaty wyłącznie przez `UiMessages`”, podczas gdy `/konto` pokazuje błędy `IdentityResult` (AD-8). Zmiana skryptu skali w eod-ui jest zapowiedziana, nie zrobiona (`8e66aea`).

**Para.**
- *Historia A (epik 7, App.razor)* wstawia `<EodHead />` do `<head>` i zostawia własny link Fluent (podwójny CSS); skrypt skali blokowany → przy przypiętym `8e66aea` skala „skacze”, w konsoli błąd CSP; ktoś „naprawia” to hashem w CSP (łamie AD-12) albo kopią skryptu w `wwwroot` (łamie AD-1 „aplikacja nie ma własnego JS”).
- *Historia B (epik 7, AccountLayout)* czyta AD-11 dosłownie i wstawia drugi `EodHead` w `AccountLayout` — w `<body>`, z drugim `eod.js`.
- Strona logowania pokazuje błędy przez `UiMessages` (nic się nie wyświetla) albo przez obecne `AccountMessages` (łamie „wyłącznie”).

**Proponowana reguła (AD-11 i AD-12):**
> `EodHead` występuje raz, w `<head>` `App.razor`, dla wszystkich stron; `App.razor` nie linkuje samodzielnie zasobów Fluent ani eod-ui. Historia, która wprowadza `EodHead`, przypina commit eod-ui, w którym skrypt skali jest plikiem (`_content/Eod.Platform.UI/eod-scale.js`), a test potoku sprawdza, że odpowiedź każdej strony (także `/konto/logowanie`) nie zawiera inline `<script>` bez `src`. Strony statyczne `/konto` pokazują komunikaty komponentem `Accounts/AccountMessages` renderującym statyczny `EodMessageBar` (klasy `eod-*`, bez `INotificationService`) — jedyny wyjątek od `UiMessages`; `AccountLayout` nie renderuje `EodHead` ani `EodShell`.

---

## H11 — Drobne, ale budujące się rozbieżnie — **niska**

- **`DataAsOf` i `BannedSymbols`.** `EodReport.DataAsOf` ma domyślnie `DateOnly.FromDateTime(DateTime.Today)` — zegar serwera, który AD-6 i `BannedSymbols.txt` banują w aplikacji, ale analizator nie widzi go w `ui/`. Przy wyłączonym CSV nieszkodliwe, dopóki ktoś nie użyje go w opisie „Stan danych” (UI_SPEC 9.7). *Reguła (AD-23):* każdy `EodReport` w Rejestrze dostaje `DataAsOf="@PolishCalendar.Today(...)"`, a opis „Stan danych” bierze czas z `PolishCalendar`.
- **Menu dla anonima.** `EodShell` renderuje zawsze `Menu` i `NewCase`; dzisiejszy `MainLayout` chowa nawigację w `AuthorizeView`. `/not-found` i `/Error` są `[AllowAnonymous]` i idą przez `MainLayout`. *Reguła (AD-11):* `Menu`, `NewCase` i akcje Konta w `HeaderActions` są w `<AuthorizeView>`; anonim widzi belkę z nazwą i `?`.
- **Nazwa i strona startowa.** `EodShell.AppName` domyślnie „MOJE EOD”, `EodPage` dokleja „· MOJE EOD” do tytułu karty. *Reguła (AD-11):* `AppName="Rejestr czasu pracy"`, `HomeHref="/miesiac"`, każdy `EodPage` z `BrowserTitle="{Tytuł} · Rejestr czasu pracy"` (albo parametr nazwy produktu w eod-ui).

---

## Proponowany nowy AD-23 — kontrakt `EodReport` i zmian eod-ui (zbiorczo)

- **Binds:** R2, R8, R9, R10, R17, R21
- **Prevents:** dwóm kontraktom sum, wierszy i układu w jednej bibliotece oraz zmianom eod-ui bez właściciela.
- **Rule:** punkty z H2, H3, H4 i H11 (pierwszy). Wymagane zmiany eod-ui, każda z historią-właścicielem w epiku 7 i zmergowana w eod-ui przed pierwszym ekranem, który jej używa: `ReportColumn.Format`, `GroupSummary` (surowe klucze), `FooterSummary` (wiersze po filtrach), `LockedLayout`, `ShowExport=false`, `ConfirmAsync(primaryShortcut: null)`, skrypt skali z pliku, Fluent 5.0.1 w `Directory.Packages.props`. Nowa potrzeba dopisuje się do tej listy w tej samej zmianie spine'u.
