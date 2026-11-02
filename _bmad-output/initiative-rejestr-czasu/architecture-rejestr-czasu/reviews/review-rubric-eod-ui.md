# Recenzja rubric — aktualizacja spine'a 02.11.2026 (wzorzec UI eod-ui)

- Spine: `architecture-rejestr-czasu.md` (updated 02.11.2026), AD-1, AD-5, AD-9, AD-10, AD-11, AD-12, AD-17, AD-22, konwencje, stos, seed, mapa, Deferred
- Sprawdzone względem: `initiative-rejestr-czasu.md` (R1–R31, Notes 02.11.2026), `tickets/epic-wzorzec-ui/epic-wzorzec-ui.md`, klon eod-ui @ `8e66aea` (`docs/UI_SPEC.md`, `src/Eod.Platform.UI`), kod aplikacji `src/RejestrCzasu`
- Data: 02.11.2026

## Werdykt

**Warunkowo NIE gotowy do refinementu epiku 7.** Kierunek jest dobry: podmoduł, jedna droga komunikatów, jeden silnik raportu, JS tylko we wzorcu, CSP bez wyjątków. Spine opisuje jednak `EodReport` inaczej, niż ten działa naprawdę. Silnik sam sortuje wiersze, sam liczy sumy z wierszy po filtrach i nie umie formatować Czasu. Z tego powodu AD-5 i AD-9 w obecnym brzmieniu są niewykonalne dla R8 i R9. Lista zmian we wzorcu, którą AD-9 wylicza jako „brakujące parametry”, jest niepełna i nie ma ustalonego kształtu API. To dokładnie ten punkt, w którym epik 7 (dodaje parametry) i epik 3 (używa ich w Widoku miesiąca) się rozjadą. Do tego AD-9 nie deklaruje, że odchodzi od UI_SPEC 9.13/12 dla list administracyjnych, a AD-6 nie uwzględnia lokalizatora z `AddEodPlatformUI`.

Liczba ustaleń: 0 critical, 4 high, 6 medium, 4 low.

---

## Ustalenia

### H-1 (high) — `EodReport` nie zachowuje kolejności ekranu, a spine twierdzi, że zachowuje (AD-5, AD-9, R9)

**Dowód:** `ReportEngine.Rows` zawsze sortuje listę: najpierw po kolumnach grupowania (domyślnie rosnąco, `Compare` według typu kolumny), potem po `Sorting`, na końcu `string.CompareOrdinal` po kolumnie kluczowej (`ReportEngine.cs`, ok. l. 170–200). `ReportEngine.Groups` grupuje po wartości **wyświetlanej** (`Display`), a nie po kluczu.

**Skutek:**
- AD-9 mówi: „Ekran podaje wiersze zmaterializowane … w swojej kolejności”. AD-5 mówi, że kolejność dni i wierszy liczy wyłącznie `MonthReport.Build`. W bibliotece to nieprawda: o kolejności decyduje silnik.
- R9 wymaga w dniu kolejności „według kodu Projektu, potem nazwy Typu pracy”. Silnik da kolejność po kolumnie kluczowej porównywanej ordinalnie, czyli bez `pl-PL`.
- R9 wymaga też nagłówka dnia z dniem tygodnia. Kolumna `Date` wyświetla tylko `dd.mm.rrrr`. Kolumna `Text` „pon. 01.09” posortuje się alfabetycznie, więc dni się pomieszają. Tygodnie mają ten sam problem.
- Epik 7 i epik 3 rozwiążą to każdy inaczej, na przykład przez `ValueOrder`, przez ukryte kolumny sortujące albo przez parametr wzorca.

**Poprawka:** dopisać do AD-9 parametr wzorca „zachowaj kolejność wejścia” (bez sortowania silnika przy układzie zablokowanym) oraz etykietę grupy podawaną przez ekran (np. `Func<ReportGroup, string>` albo klucz grupy ≠ tekst). Jako alternatywę wprost ustalić sposób (`ValueOrder` z `MonthReport` dla kolumn Tydzień i Dzień). Jedno z dwóch, nazwane w AD.

### H-2 (high) — Reguła „`EodReport` nigdy nie liczy sum Czasu” łamie R8 dla Listy wpisów (AD-5, AD-9)

**Dowód:** filtry, wyszukiwanie i licznik działają wewnątrz komponentu (`_filteredRows`). `EodReport` nie ma zdarzenia ani callbacku z wierszami po filtrach. Stopka to `ReportEngine.Summary(Definition, _filteredRows)`, a wiersz grupy to `Summary(Definition, g.Rows)`.

**Skutek:** R8 wymaga sumy Czasu Listy wpisów. Lista ma filtr wartości w każdej kolumnie (R17) i pełny układ użytkownika z grupowaniem (AD-9), więc suma musi dotyczyć wierszy **po filtrach** i grup ustawionych przez Usera. Ekran tych wierszy nie zna. AD-5 nakazuje liczyć sumę Listy „z tej samej materializowanej listy co wiersze”. Po filtrze kolumny Projekt w `EodReport` suma przestanie zgadzać się z widocznymi wierszami i licznikiem. AD-5 i AD-9 przypisują tę samą odpowiedzialność („sumy podaje ekran”) do dwóch przypadków o przeciwnej semantyce:
- Widok miesiąca: suma całego Miesiąca, niezależna od filtra (R9, R17).
- Lista wpisów: suma tego, co widać (R8).

**Poprawka:** rozdzielić w AD-9:
- (a) Widok miesiąca: sumy grup i stopki **zadane przez ekran** z `MonthReport`, niezależne od filtra.
- (b) Lista wpisów i Słowniki: sumę liczy silnik z wierszy po filtrach, ale formatuje ją `Domain/Czas` przez formatter kolumny (H-3).

Wyjątek z AD-5 przenieść z „ekran liczy z listy” na „silnik `EodReport` sumuje minuty, format z `Czas`”.

### H-3 (high) — Brak formatowania Czasu w `EodReport`; lista „brakujących parametrów” w AD-9 jest niepełna (AD-6, AD-9, R8, R9, R17)

**Dowód:** `ReportColumn` ma tylko `Type` (Text/List/Number/Date), `IsAmount` i `IsPercent`. Nie ma własnego formatu. `Display`, filtry zakresu, CSV i `Summary` formatują liczby jako `#,0.##` lub kwotę.

**Skutek:**
- Czas jako `int` minut wyświetli się jako „90”, a nie „1h 30m”. To łamie AD-6, który mówi, że jedyne miejsca konwersji to `Domain/Czas`.
- Czas jako tekst sortuje i filtruje się leksykalnie („10h” < „2h”).
- Filtr `List` na kolumnie Czas pokaże surowe minuty.

AD-9 wylicza tylko trzy zmiany we wzorcu: wyłączenie eksportu, sumy od ekranu i blokadę układu. Epik 7 ma te same trzy plus potwierdzenie i Fluent 5.0.1. Brakuje formattera wartości kolumny (i sumy), zachowania kolejności (H-1) i etykiety grupy. Każdy epik dopisze je wtedy po swojemu.

**Poprawka:** w AD-9 dodać pełną, zamkniętą listę zmian we wzorcu z ich kształtem. Przykład:
- `ReportColumn.Format : Func<object?, string>?`, używany w komórce, grupie, filtrze i sumie;
- `EodReport.ShowExport = false`;
- `EodReport.LockLayout = true`;
- `EodReport.PreserveRowOrder`;
- `EodReport.GroupSummary : Func<ReportGroup, string>?` i `FooterSummary : string?`.

Wskazać też, że kształt ustala epik 7, a epik 3 tylko z niego korzysta.

### H-4 (high) — AD-9 przeczy UI_SPEC 9.13 i 12 bez deklaracji wyjątku (lista Kont, listy Słowników)

**Dowód:** UI_SPEC 9.13 A mówi: „Tabela: `FluentDataGrid` (sekcja 12), **nie `EodReport`** – lista administracyjna nie ma grupowania, eksportu ani układu użytkownika”. Sekcja 12 mówi: „Lista administracyjna (konta, role, struktura): `FluentDataGrid` … Nie `EodReport`”. Wzorcem słownika jest tam `EodEditableTable` (9.13 B).

AD-11 mówi: „według specyfikacji wzorca … z wyjątkami z tego AD”. Wyjątek dotyczący list jest jednak w AD-9 i nie jest nazwany wyjątkiem.

**Skutek:**
- Dla list Słowników wybór `EodReport` jest uzasadniony przez R17 (filtr wartości w każdej kolumnie), ale nie jest zapisany jako świadome odejście od wzorca.
- Dla listy Kont (Użytkownicy, epik 5) R17 nie obowiązuje, bo R17 mówi o „raportach i liście Słownika”. Nic nie wymaga więc `EodReport`, a spec mówi przeciwnie.
- Agent epiku 5 czytający UI_SPEC (AD-11 każe go czytać) zrobi `FluentDataGrid`. Agent czytający AD-9 zrobi `EodReport`. Klasyczna rozbieżność.

**Poprawka:** w AD-11 dopisać jawny wyjątek: „listy Słowników i Kont na `EodReport` (R17), wbrew UI_SPEC 9.13/12; `EodEditableTable` nieużywany”. Druga możliwość: przywrócić `FluentDataGrid` według 9.13 A dla Użytkowników i zostawić `EodReport` tylko tam, gdzie wymaga go R17.

### M-1 (medium) — AD-6 niezgodny z `AddEodPlatformUI` (lokalizator i kultura)

**Dowód:**
- `AddEodPlatformUI` sam wywołuje `AddFluentUIComponents` z `options.Localizer = new PolishLocalizer()`. Komentarz w kodzie mówi: „Aplikacja nie wywołuje już osobno `AddFluentUIComponents`”.
- Aplikacja ma `Localization/FluentLocalizer.cs` i `Program.cs:63` z `AddFluentUIComponents(options => options.Localizer = new FluentLocalizer())`.
- AD-6 nadal mówi: „teksty Fluent UI przez własny `IFluentLocalizer`”.
- Wzorzec ma też `UseEodPolishCulture()` (kultura `Formats.Culture`, `RequestCultureProviders = []`). Spine o nim milczy.

**Skutek:** podwójna rejestracja Fluent UI albo spór, który lokalizator wygrywa. Klucze, które aplikacja przetłumaczyła, a wzorzec nie, mogą wrócić po angielsku.

**Poprawka:** w AD-6 ustalić, że teksty Fluent daje `PolishLocalizer` wzorca, a brakujące klucze dopisuje się w eod-ui. Zapisać też, że aplikacja wywołuje tylko `AddEodPlatformUI()` i `UseEodPolishCulture()`, a `Localization/FluentLocalizer` jest usuwany.

### M-2 (medium) — Tytuł karty przeglądarki i nazwa „MOJE EOD” wpisane na stałe we wzorzec

**Dowód:** `EodPage` renderuje `<PageTitle>` jako `"{Title} · MOJE EOD"`, chyba że ekran poda `BrowserTitle`. `EodShell.AppName` ma wartość domyślną „MOJE EOD”.

**Skutek:** spine nic o tym nie mówi. Każdy ekran albo poda `BrowserTitle` (różne formaty w różnych epikach), albo pokaże „MOJE EOD” w karcie przeglądarki. Dotyczy to też strony „nie znaleziono” i pomocy.

**Poprawka:** zmiana we wzorcu (nazwa produktu w opcjach `AddEodPlatformUI`, używana przez `EodPage` i `EodShell`) dopisana do listy z H-3. Jako alternatywę wprowadzić konwencję: „`BrowserTitle` = `{Title} · Rejestr czasu` na każdym ekranie”.

### M-3 (medium) — „Komunikaty wyłącznie przez `UiMessages`” nie ma gdzie się pokazać w dwóch hostach (AD-7, AD-10, AD-11)

**Dowód:**
- `UiMessages` wysyła paski tylko do sekcji `EodSections.Page`, której provider jest w `EodPage`. Bez providera pasek po cichu się nie pokazuje (catch `FluentServiceProviderException`, zwraca `null`).
- (a) Okno edycji i kopii Wpisu (AD-10 host b): pasek błędu `DbErrors` (AD-7 „ekran pokazuje je w MessageBar”) i błąd walidacji trafią do strony **pod modalnym oknem**. UI_SPEC 9.13 mówi wprost: „Walidacja po Zapisz: komunikaty pod polami, okno zostaje otwarte”.
- (b) Statyczne strony `/konto/...` (SSR) nie mają obwodu, `EodShell` ani `FluentProviders`. `UiMessages` tam nie działa. Aplikacja ma `Accounts/AccountMessages.razor`, o którym spine milczy.

**Poprawka:** w AD-11 dopisać dwa nazwane wyjątki:
- w oknie dialogowym błędy pod polami i pasek wewnątrz okna (albo provider sekcji w oknie, zmiana we wzorcu);
- strony SSR `/konto/...` pokazują komunikaty statycznym znacznikiem `eod-*` (`AccountMessages`), bez `UiMessages`.

### M-4 (medium) — Kolizja nazwy `Shared/HelpTopic` z istniejącym typem (AD-22, AD-11)

**Dowód:** w `Features/Pomoc/HelpTopics.cs:33` istnieje `public sealed record HelpTopic(string Key, string Title, Type Content, HelpAccess Access)`. AD-22 wprowadza komponent `Shared/HelpTopic`. W `_Imports.razor` obu przestrzeni nazw `<HelpTopic>` i `HelpTopic?` w `HelpPage.razor` staną się niejednoznaczne.

Istniejące `Shared/HelpButton`, `PageHeader(Topic=…)` i `Features/Pomoc/HelpLayout` (wybór `AccountLayout`/`MainLayout`) nie są wymienione jako zastąpione ani zachowane. Spine wycofuje tylko `ReportGrid`.

**Poprawka:** zmienić nazwę komponentu (np. `Shared/HelpTopicMarker` albo `ScreenHelp`) i wymienić w opisie paradygmatu, co z `Shared/` jest usuwane: `PageHeader`, `Notify`, `Confirm`, `ConfirmDialog`, `ReportGrid*`, `ReportColumn`, `ReportGroup`, `ReportAction`, `Breadcrumb`, `EntryForm.razor.js`. Wymienić też, co zostaje: `HelpButton`, `HelpLayout`, `ReconnectModal`, `AppErrorBoundary`, `EntryCount`. Obecnie tylko memlog (ASSUMPTION) wspomina o wycofaniu `Notify`/`Confirm`/`PageHeader`.

### M-5 (medium) — Lista wpisów: zakres dat R8 i domyślne sortowanie nie są przypisane do mechanizmu

**Dowód:**
- R8: „filtruje po zakresie dat (domyślnie bieżący Miesiąc) … sortuje od najnowszej daty”.
- `ReportDefinition` ma domyślne `Sorting`, ale **nie ma domyślnych filtrów**.
- Filtr `Date` jest częścią układu użytkownika, a ten zgodnie z AD-9 żyje do odświeżenia.

**Skutek:** zakres dat może być ładowaniem po stronie ekranu (zapytanie, pole na pasku), filtrem `Date` w `EodReport` z ustawieniem początkowym (którego API nie ma) albo jednym i drugim. Epik 7 przenosi istniejącą Listę i musi wybrać. Od tego zależy też, co znaczą licznik i suma (H-2).

**Poprawka:** w AD-9 jednym zdaniem: „zakres dat Listy wpisów to parametr zapytania ekranu (pole nad raportem, domyślnie bieżący Miesiąc z `PolishCalendar`); `EodReport` dostaje tylko wiersze z zakresu; domyślne `Sorting` = data malejąco”.

### M-6 (medium) — Potwierdzenie bez Enter i z fokusem na Anuluj: zależność od API Fluent nie jest zweryfikowana (AD-11)

**Dowód:** `UiMessages.CreateOptions` ustawia `PrimaryShortCut = "Enter"`. Zdjęcie skrótu jest proste. „Fokus na Anuluj” wymaga jednak, żeby `MessageBoxOptions` lub `ShowMessageBoxAsync` pozwalały ustawić początkowy fokus na przycisku drugim. W `MessageBoxOptions` nie ma takiej właściwości (widać tylko `PrimaryButton/SecondaryButton/*ShortCut/Icon`). Może być potrzebny własny komponent okna we wzorcu albo JS w `eod.js`.

**Skutek:** jeśli zmiana wymaga własnego okna, „jedna droga potwierdzeń” we wzorcu zmienia kształt, a UI_SPEC 11 („Enter wybiera przycisk główny”) trzeba zaktualizować w eod-ui.

**Poprawka:** dodać do AD-11 zdanie: „zmiana obejmuje też UI_SPEC 11 i dokumentację `ConfirmAsync`; jeśli `MessageBoxOptions` nie pozwala ustawić fokusu, wzorzec dostaje własne okno potwierdzenia za tym samym `ConfirmAsync`”.

### L-1 (low) — `EodReport.DataAsOf` domyślnie `DateOnly.FromDateTime(DateTime.Today)`

Zakaz z AD-6 dotyczy aplikacji, ale ta wartość jest teraz w zależności przypiętej do aplikacji. Przy wyłączonym CSV nie ma skutku. **Poprawka:** konwencja „ekran podaje `DataAsOf = PolishCalendar.Today`” albo usunięcie domyślnej wartości przy zmianie eksportu.

### L-2 (low) — Interaktywne tematy pomocy w `AccountLayout` bez `FluentProviders`

AD-11 opisuje `AccountLayout` jako layout stron SSR. Kod (`Features/Pomoc/HelpLayout.razor`) renderuje w nim **interaktywne** anonimowe tematy pomocy, z `ReconnectModal`. Bez `EodShell` nie ma `FluentProviders`, więc tooltip, okno lub pasek w treści tematu nie zadziała. **Poprawka:** zdanie w AD-22: „treść tematów pomocy nie używa okien, pasków ani tooltipów” albo „`AccountLayout` w trybie interaktywnym renderuje `FluentProviders`”.

### L-3 (low) — Wersje i build podmodułu

- eod-ui ma CPM (`ManagePackageVersionsCentrally`, Fluent **5.0.0**) w `ui/Directory.*.props`. Aplikacja nie ma CPM (`PackageReference Version="5.0.1"`). Build zadziała, ale do czasu podbicia we wzorcu da konflikt wersji: NU1605/NU1608 albo cichy wybór 5.0.1 w aplikacji. Konfliktuje to z „build bez ostrzeżeń” z epiku 7. Spine to zauważa w Stack. Warto dopisać, że **podbicie Fluent w eod-ui to pierwsza zmiana we wzorcu**, przed `ProjectReference`.
- W `Directory.Packages.props` wzorca są wersje Syncfusion. `Eod.Platform.UI.csproj` ich nie referuje, więc wykluczenie Syncfusion jest zgodne z kodem. To jest OK.

### L-4 (low) — Deferred i mapa

- Deferred „Wygląd poszczególnych ekranów (kolumny…)” jest bezpieczny tylko pod warunkiem H-3. Typ kolumny i format Czasu decydują o R17 i AD-6, więc nie mogą być decyzją historii.
- Mapa: R17 → tylko AD-9; warto dodać AD-1 (zmiany we wzorcu). R31 → `Shared/MainLayout` i AD-11; aktualny kod renderuje `ReconnectModal` także w `HelpLayout`, warto to wpisać. NFR-6 nie ma wiersza w mapie (pokrywa go AD-11).

---

## Weryfikacja twierdzeń o bibliotece (stan `8e66aea`)

| Twierdzenie spine'a | Stan w kodzie | Ocena |
| --- | --- | --- |
| `EodShell` ma belkę, `HeaderActions`, miejsce „Nowa sprawa”, menu; bez przełącznika i powiadomień | Parametry `AppName`, `HomeHref`, `ContextSwitcher`, `HeaderActions`, `NewCase`, `Menu`, `MenuFooter`; zawiera `FluentProviders`; przy zmianie strony `UiMessages.ClearAsync` i `EodUI.scrollToTop` | zgodne; `AppName` trzeba podać (M-2) |
| `EodPage` (tytuł, opis, okruszki, akcje, `Fill`), paski pod nagłówkiem | Tak; provider `EodSections.Page` pod nagłówkiem; `<PageTitle>` z „MOJE EOD” | zgodne; M-2 |
| `UiMessages`: sukces i informacja 7 s, ostrzeżenie i błąd do zamknięcia, zmiana strony czyści | Tak (`BarLifetime` 7 s; auto-zamykanie tylko bez akcji; najwyżej 3 paski) | zgodne |
| `UiMessages.ConfirmAsync` bez Enter, fokus na Anuluj — **zmiana we wzorcu** | Obecnie `PrimaryShortCut = "Enter"`, `SecondaryShortCut = "Escape"` | zgodne z oznaczeniem „do zmiany”; M-6 |
| `EodHead` ładuje IMask i SortableJS lokalnie; skrypt skali inline do przeniesienia | Tak; `ScaleScript` jako inline `<script>` `MarkupString` | zgodne z oznaczeniem „do zmiany” (AD-12) |
| `EodReport` z `ReportDefinition`, kolumny `List` z filtrem i licznikami, licznik w stopce, `RowActions` | Tak | zgodne |
| `EodReport` nie liczy sum; sumy podaje ekran | Liczy (`ReportTotal`, `Summary` z wierszy po filtrach i z grup) | oznaczone jako zmiana we wzorcu, ale semantyka niejednoznaczna (H-2) |
| Ekran podaje wiersze „w swojej kolejności” | Silnik zawsze sortuje | **niezgodne** (H-1) |
| Układ żyje do odświeżenia strony | `ReportLayoutStore` scoped, w pamięci | zgodne |
| Eksport CSV wyłączony parametrem | Przycisk zawsze renderowany (`EodReport.razor:49`); parametru brak | zgodne z oznaczeniem „do zmiany” |
| `AppearanceSettings` | Scoped, rejestrowany przez `AddEodPlatformUI` | zgodne |
| `AddEodPlatformUI` w testach bUnit | Rejestruje Fluent + `UiMessages` + `AppearanceSettings` + `ReportLayoutStore` | zgodne; M-1 (lokalizator) |
| Brak połączeń zewnętrznych w `eod.css`/`eod.js` | Brak URL-i http(s) | zgodne (NFR-1) |

## Pokrycie wymagań (wskazane)

| Wym. | Pokrycie | Uwagi |
| --- | --- | --- |
| R8 | częściowe | suma i zakres dat nie mają mechanizmu zgodnego z silnikiem (H-2, M-5) |
| R9 | częściowe | kolejność w dniu, etykiety Tydzień i Dzień z dniem tygodnia, sumy grup od ekranu bez API (H-1, H-3) |
| R10 | OK pod warunkiem H-1/H-3 | jedno źródło sum (AD-5) zachowane, jeśli silnik nie przelicza grup |
| R16 | OK | AD-10 bez zmian; poprawka comboboxa do wzorca (AD-1); epik 7 sprawdza 10 s |
| R17 | OK z zastrzeżeniem | `List` + licznik + eksport wyłączony; „filtr nie zmienia sum” tylko dla Widoku (H-2); format Czasu w filtrze (H-3) |
| R26 | OK z zastrzeżeniem | `?` w `HeaderActions`; kolizja nazwy (M-4); tematy w `AccountLayout` (L-2) |
| R31 | OK | własny znacznik, bez JS; spójne z `ReconnectModal.razor` |
| NFR-1 | OK | AD-12 + inline skrypt do usunięcia; zasoby lokalne potwierdzone |
| NFR-6 | OK | menu i belka w AD-11, `NewCase` → Nowy wpis |

## Wymiary pominięte w całości

- **Formatowanie wartości w komponentach wzorca** (Czas w raporcie, sumach i filtrach) nie ma właściciela. Zob. H-3.
- **Kolejność dostarczenia zmian we wzorcu względem historii** (epik 7, Open question) zostaje otwarta, a od niej zależy, czy epik 3 czeka na API raportu. Spine powinien przynajmniej ustalić, że wszystkie zmiany `EodReport` i `UiMessages` wchodzą do eod-ui w epiku 7, przed epikiem 3.
- **Responsywność, dostępność, motyw** spine deleguje do UI_SPEC (15, 16, AD-11). To jest OK i nie wymaga AD.
