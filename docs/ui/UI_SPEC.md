# MOJE EOD – specyfikacja UI

Wersja 1.5 · 2026-10-18 · wzorzec zatwierdzony przez właściciela produktu (Kamil Kandula).
Wersja 1.5: etapy obiegu sprawy (formularz startowy, widok akceptującego, sprawa zakończona – 9.3.1–9.3.3), wzorzec administracji (9.13), stany ekranów: wczytywanie, brak danych, brak dostępu (9.14); komponenty Syncfusion zbudowane w `Eod.Platform.UI.Syncfusion` na licencji próbnej, ryzyka sprawdzone w demo, telemetria Syncfusion wyłączona (3.2, 9.12, 18.1). Wersja 1.4: Nazwy w kodzie po angielsku (komponenty, parametry, usługi, klasy CSS), dokumentacja XML po polsku; próg „termin blisko” ≤ 3 dni potwierdzony – sekcje 3.3 i 20. Wersja 1.3: diagram (Syncfusion) służy do schematu organizacyjnego – departamenty, pracownicy, zależności; przebieg sprawy pokazują kamienie milowe. Wersja 1.2: komponenty **Syncfusion Blazor** w wybranych obszarach (terminarz, menedżer plików, schemat organizacyjny, edytor treści, edytor DOCX, dokumenty, wykresy) – sekcje 3.2 i 9.12. Wersja 1.1: biblioteka `Eod.Platform.UI` i demo `Eod.Demo` skompilowane i uruchomione, zrzuty w `docs/ekrany` pochodzą z demo, pułapki Fluent v5 – sekcja 3.1.
Dokument normatywny: **MUSI** / **NIE WOLNO** to wymagania, **ZALECANE** to domyślny wybór, od którego odstępstwo trzeba uzasadnić.

---

## 0. Jak korzystać z tego dokumentu (dla ludzi i agentów)

Repozytorium `eod-ui` jest kontekstem UI dla każdego, kto buduje MOJE EOD. Zawiera:

| Ścieżka | Co to jest | Do czego służy |
|---|---|---|
| `docs/UI_SPEC.md` | ten dokument | zasady: jak ma wyglądać i zachowywać się aplikacja |
| `src/Eod.Platform.UI` | biblioteka komponentów Razor `Eod*` | **z tego składa się ekrany** – nie kopiuje się ich markupu |
| `src/Eod.Platform.UI.Syncfusion` | biblioteka komponentów wzorca na Syncfusion Blazor (`EodScheduler`, `EodFileManager`, `EodRichTextEditor`, `EodDocumentEditor`, `EodOrgChart`) | obszary z sekcji 3.2 – komponenty Syncfusion na ekranie tylko przez nią |
| `src/Eod.Demo` | aplikacja demo (Blazor Web App) | gotowe ekrany wzorca na danych przykładowych – przykład, jak złożyć ekran |
| `docs/ekrany/*.png` | zrzuty ekranów aplikacji demo (jasny 1920, ciemny 1366, telefon 390; `tools/screenshots.mjs`) | jak ekran ma wyglądać |
| `docs/prototyp/moje-eod.html` | klikalny prototyp HTML, z którego powstał wzorzec | archiwum; rozstrzyga tylko szczegóły wizualne nieopisane gdzie indziej |

**Kolejność ważności przy sprzeczności:** specyfikacja → komponenty `Eod.Platform.UI` → ekrany `Eod.Demo` → zrzuty → prototyp HTML. Sprzeczność trzeba zgłosić (issue / komentarz w PR), a nie rozstrzygać po cichu.

**Przed zbudowaniem ekranu** agent MUSI: (1) znaleźć w katalogu ekranów (sekcja 8) najbliższy wzorzec, (2) otworzyć jego plik w `Eod.Demo` i zrzut w `docs/ekrany`, (3) złożyć ekran z komponentów `Eod*` i Fluent UI, (4) przejść listę kontrolną z sekcji 19.

---

## 1. Produkt

**MOJE EOD** to platforma procesów (workflow) dla firm. Użytkownik pracuje na **sprawach** w **procesach** (np. Zlecenie rekrutacji, Onboarding pracownika, Wnioski urlopowe, później obieg faktur). Sprawa przechodzi przez **etapy**; na każdym etapie ktoś jest **wykonawcą kroku**. Procesy dołącza się do platformy jak moduły (patrz `platforma-procesow.md` w projekcie).

Problem, który rozwiązuje UI: użytkownik ma w jednym miejscu swoje zadania ze wszystkich procesów, szybko widzi, co jest pilne, i w ten sam sposób obsługuje każdą sprawę, niezależnie od procesu. **Każdy proces wygląda i działa tak samo** – różni się tylko treścią sekcji formularza i definicjami raportów.

Zakres wzorca (zamknięty 2026-10-12): szkielet aplikacji, strona startowa, strona główna procesu, formularz sprawy (z załącznikami, komentarzami, akceptacjami, historią), raport tabelaryczny i katalog raportów, ustawienia użytkownika, zastępstwa, powiadomienia, zestaw kontrolek, komunikaty i okna. **Poza zakresem** (zaprojektujemy osobno): logowanie, trwałe zapamiętywanie układu raportu, ekrany administracji poza wzorcem administracyjnym (wzorzec: lista z oknem edycji i tabela edycyjna słownika – sekcja 9.13). Grafiki i kalendarze, biblioteka dokumentów, edycja dokumentów, schemat organizacyjny i wykresy powstają na komponentach Syncfusion – zasady w sekcjach 3.2 i 9.12; ekrany wzorca są w demo (licencja próbna), wykresy – do zbudowania.

## 2. Użytkownicy i role

| Rola | Co robi w UI | Skutki dla UI |
|---|---|---|
| Pracownik | składa wnioski, wykonuje swoje zadania, obserwuje sprawy | widzi Start, Moje zadania, Obserwowane, Zakończone, raporty swoich ról |
| Kierownik departamentu | zleca rekrutacje, opisuje stanowiska, akceptuje urlopy, ogląda raporty departamentu | najczęstszy użytkownik formularza sprawy i raportów z grupowaniem |
| Opiekun wdrożenia | prowadzi plan wdrożenia nowego pracownika | raport „Wdrożenia w toku” |
| Administrator klienta | role, użytkownicy, struktura, raporty do ról, parametry procesów | sekcja „Administracja” w menu; ekrany wg wzorca 9.13 (lista z oknem edycji – „Użytkownicy”), pozostałe z tych samych elementów |
| Administrator biznesowy | słowniki (wartości list wyboru) | tabela edycyjna słownika – „Słowniki” (9.13) |
| Administrator systemu | moduły, integracje, języki | jw. |

Osoba przykładowa we wzorcu: **Marta Wysocka, Kierownik departamentu IT**. Dane przykładowe są „zamrożone” w dniu **08.10.2026**.

**Uprawnienia i reguły biznesowe nie są częścią wzorca UI.** Wzorzec pokazuje widoki; kto co widzi i może, ustala definicja procesu (`platforma-procesow.md`). UI MUSI ukrywać niedostępne akcje (nie tylko je wyłączać), chyba że użytkownik powinien wiedzieć, że akcja istnieje – wtedy przycisk wyłączony z podpowiedzią „dlaczego”.

## 3. Stos technologiczny UI

- **Blazor Web App, .NET 10, tryb Interactive Server** (globalnie, `Routes @rendermode="InteractiveServer"`).
- **Microsoft.FluentUI.AspNetCore.Components 5.x** (stabilna 5.0.0 z 2026-09-25) + **Microsoft.FluentUI.AspNetCore.Components.Icons**. v5 opakowuje `@fluentui/web-components` 3.x i daje komponenty istniejące tylko w Blazor (FluentLayout, FluentNav, FluentGrid, FluentWizard, FluentDataGrid, FluentMessageBox…).
- **Syncfusion Blazor** (`Syncfusion.Blazor.*`, motyw Fluent 2) – **tylko** w obszarach, w których Fluent nie ma komponentu: terminarz (Scheduler), menedżer plików (File Manager), schemat organizacyjny (Diagram), edytor treści (Rich Text Editor), edytor DOCX (DOCX Editor), wykresy (Chart, założenie robocze) – i zawsze przez komponenty wzorca `Eod*` z `Eod.Platform.UI.Syncfusion`. Generowanie dokumentów: Syncfusion Document SDK po stronie serwera. Zasady – sekcja 3.2.
- **NIE WOLNO** dokładać innych bibliotek UI (Bootstrap, MudBlazor, Radzen, Tailwind, ikon spoza Fluent System Icons) ani używać komponentów Syncfusion poza obszarami z sekcji 3.2. Bootstrap z szablonu Blazor usuwamy.
- Style: **wyłącznie tokeny Fluent 2** (`var(--colorNeutralBackground1)`, `var(--spacingHorizontalM)`, `var(--fontSizeBase300)`…) + arkusz `eod.css` z biblioteki `Eod.Platform.UI`. **NIE WOLNO** wpisywać kolorów szesnastkowych ani własnych rozmiarów czcionek w komponentach ekranów.
- CSS komponentów Fluent v5 jest globalny (klasy `fluent-*`); `eod.css` uzupełnia go klasami `eod-*`. Style izolowane (`.razor.css`) wolno stosować w komponentach ekranów tylko do układu specyficznego dla jednego ekranu.
- Skrypty IMask (FluentNumberInput, pola z maską) i SortableJS (FluentSortableList), które Fluent v5 pobiera domyślnie z unpkg.com, ładuje lokalnie `EodHead` (`wwwroot/lib`, licencja MIT) – aplikacja działa bez dostępu do internetu.

### 3.1 Pułapki Fluent UI Blazor v5 (sprawdzone w demo – stosować zawsze)

| Pułapka | Skutek | Rozwiązanie we wzorcu |
|---|---|---|
| Pole tekstowe, liczbowe, wielowierszowe i daty ma domyślny `MessageCondition` | własny `Message` (podpowiedź, błąd, ostrzeżenie) nie pokazuje się; widać tylko „pole wymagane” po opuszczeniu pola | `MessageCondition="@FieldConditions.WhenHasMessage"` przy każdym takim polu z `Message` (Select, RadioGroup, Autocomplete tego nie potrzebują) |
| Klasa `my-3-o` (12 px marginesu) na każdym polu z etykietą | podwójne odstępy w siatkach pól | wyłączona w `AddEodPlatformUI` (`DefaultStyles.FluentFieldClass = null`) – odstępy daje układ |
| Kultura pl-PL z ICU ma krótką datę `d.MM.yyyy` | FluentDatePicker pokazuje „9.10.2026” | `app.UseEodPolishCulture()` (kultura `Formats.Culture` z `dd.MM.yyyy`) |
| `FluentPopover` z `Opened="true"` od pierwszego renderowania | okienko się nie otwiera (`showPopover` na elemencie spoza dokumentu) | popover stale w drzewie, otwierany zmianą `Opened` (wzór: `EodReport`) |
| `FluentDataGrid` z `OnRowClick` (lambda) | błąd kompilacji – typ wiersza to `object` | jawny `TGridItem="…"` |
| `FluentDataGrid` wpisuje wysokość wiersza w styl komórki (32 px) | wiersze niższe niż 44 px ze specyfikacji | `RowSize="DataGridRowSize.Medium"` (44 px) w zwykłych tabelach |
| `FluentTextArea` ma wewnętrzną szerokość 18rem | pole nie wypełnia sekcji | atrybut `block="true"` (+ `Width="100%"`) |
| `FluentAvatar Slot="start"` nie trafia do znacznika; parametry Blazor nie rozróżniają wielkości liter (`slot` = `Slot`) | awatar w przycisku źle wyrównany | opakowanie `<span slot="start">` |
| Treść opcji listy (awatar, podpis) przechwytuje kliknięcie | wybór opcji nie działa po kliknięciu w awatar | reguła `fluent-option * { pointer-events: none }` w `eod.css` |
| Wywołanie JS w `DisposeAsync` komponentu prerenderowanego | wyjątek w logu serwera przy każdym wejściu | JS tylko po pierwszym `OnAfterRenderAsync` (wzór: `EodCaseForm`, `EodComments`) |
| Rola przycisku `fluent-button` nadawana przez ElementInternals | `getByRole` w testach Playwright nie znajduje przycisków | w testach lokatory CSS (`fluent-button`, `hasText`, `[title]`) |
| Komórka `FluentDataGrid` ma `white-space: nowrap` i `overflow: hidden` | komunikat walidacji pod polem w komórce tabeli edycyjnej jest ucięty | tabela edycyjna przez `EodEditableTable` (klasa `eod-edit-table`: zawijanie, komórki wyrównane do góry) |
| Komponent generyczny z ograniczeniem `class` i `[CascadingTypeParameter]` (`EodEditableTable`) | przy wnioskowaniu typu z parametru kompilator Razor gubi ograniczenie – błąd CS0452 w kolumnach | jawny `TGridItem="…"` na `EodEditableTable` |
| `FluentCounterBadge` z `Count="0"` | puste kółko w miejscu licznika (karta bez załączników, menu bez zadań) | `ShowEmpty="false"` przy licznikach, które mogą mieć 0 |
| Stan wczytywania przycisku ustawiony po `await` okna (potwierdzenie, okno z danymi) | przycisk nie pokazuje `Loading` – Blazor odświeża komponent sam tylko po pierwszym `await` obsługi zdarzenia | po ustawieniu stanu `StateHasChanged()` (wzór: `NewCase`, `LeaveRequestCase`; sekcja 9.14) |

### 3.2 Komponenty Syncfusion Blazor (wybrane obszary)

Fluent UI Blazor v5 jest biblioteką podstawową. Tam, gdzie Fluent nie ma komponentu, a budowa własnego byłaby kosztowna, MOJE EOD używa **Syncfusion Blazor** (pakiety NuGet `Syncfusion.Blazor.*`). Ekrany w tych obszarach projektujemy pod możliwości i wygląd komponentów Syncfusion – **nie budujemy własnych odpowiedników**. Licencja (Community License) i koszty: dokument „EOD – Tech Spec: biblioteki UI i licencjonowanie (Syncfusion)” w projekcie (`claude/eod-tech-spec-syncfusion.md`); tu są tylko skutki dla UI i kodu.

**Status: zbudowane (licencja próbna), 2026-10-18.** Biblioteka `Eod.Platform.UI.Syncfusion` (pakiety `35.1.40`) z komponentami `EodScheduler`, `EodFileManager`, `EodRichTextEditor`, `EodDocumentEditor`, `EodOrgChart` (API – sekcja 18.1) i ekrany demo (sekcja 8, wzorce – sekcja 9.12). Demo działa **bez klucza licencji** (decyzja właściciela): komponenty pokazują baner i okno wersji próbnej, dokumenty PDF mają znak wodny – wygląd i zachowanie sprawdzono na tej wersji. `EodChart` i `EodGantt` – nadal założenie robocze, do zbudowania. Wyniki sprawdzenia pułapek – tabela na końcu sekcji (kolumna „Wynik w demo”), skrypt `tools/syncfusion-checks.mjs`.

| Obszar | Komponent Syncfusion | Pakiet NuGet | Komponent wzorca | Gdzie w MOJE EOD |
|---|---|---|---|---|
| Kalendarz, rezerwacje, zdarzenia, grafiki | Scheduler `SfSchedule<T>` | `Syncfusion.Blazor.Schedule` | `EodScheduler<TItem>` | grafiki obecności i zmian (oś czasu: osoby × dni), rezerwacje sal i zasobów, kalendarz zdarzeń procesu (np. rozmowy z kandydatami) |
| Repozytorium plików z folderami | File Manager `SfFileManager<T>` | `Syncfusion.Blazor.FileManager` | `EodFileManager` | biblioteka dokumentów (regulacje, szablony, dokumenty departamentu); **nie** załączniki sprawy |
| Schemat organizacyjny: departamenty, pracownicy, zależności | Diagram `SfDiagramComponent` (układ `OrganizationalChart`) | `Syncfusion.Blazor.Diagram` | `EodOrgChart` | Intranet › Książka adresowa (widok „Schemat”), Administracja › Struktura organizacyjna |
| Treść formatowana | Rich Text Editor `SfRichTextEditor` | `Syncfusion.Blazor.RichTextEditor` | `EodRichTextEditor` | treść ogłoszenia, aktualności i komunikaty intranetu, szablony e-maili |
| Edycja dokumentu Word | DOCX Editor `SfDocumentEditorContainer` | `Syncfusion.Blazor.WordProcessor` | `EodDocumentEditor` | dokumenty sprawy przed wysłaniem (umowa, ogłoszenie), szablony dokumentów |
| Wykresy (założenie robocze, sekcja 20) | Chart `SfChart`, `SfAccumulationChart` | `Syncfusion.Blazor.Charts` | `EodChart` | wykresy w raportach i na stronach procesów |
| Harmonogram z zależnościami (założenie robocze) | Gantt `SfGantt<T>` | `Syncfusion.Blazor.Gantt` | `EodGantt<TItem>` | plan wdrożenia, plany projektów – gdy proces tego wymaga |
| Generowanie i konwersja PDF / Word / Excel / PowerPoint | Document SDK (DocIO, PDF, XlsIO, Presentation) | `Syncfusion.DocIO.Net.Core`, `Syncfusion.DocIORenderer.Net.Core`, `Syncfusion.Pdf.Net.Core`, `Syncfusion.XlsIO.Net.Core`, `Syncfusion.Presentation.Net.Core` | usługa serwera (poza biblioteką UI) | „Generuj PDF”, eksport Excel raportu, dokumenty w karcie „Dokumenty i wiadomości” |

Wspólne pakiety: `Syncfusion.Blazor.Themes` (motyw), `Syncfusion.Licensing` (licencja), `Syncfusion.Telemetry` (zależność pakietów 35.x – zasada 8). **Wszystkie pakiety `Syncfusion.*` w tej samej wersji** (jeden wpis wersji `SyncfusionVersion` w `Directory.Packages.props`). Konwersja do PDF na Linuksie: `SkiaSharp.NativeAssets.Linux.NoDependencies` i `HarfBuzzSharp.NativeAssets.Linux` w wersjach, które rozwiązuje `Syncfusion.DocIORenderer.Net.Core` (dla 35.1.40: 4.150.1 i 14.2.1.1).

**Zasady (MUSI):**
1. **Syncfusion tylko w obszarach z tabeli.** Przyciski, pola, listy, okna, paski komunikatów, menu, karty, tabele, podpowiedzi – Fluent UI Blazor. NIE WOLNO używać na ekranach m.in. `SfButton`, `SfTextBox`, `SfDropDownList`, `SfDatePicker`, `SfDialog`, `SfToast`, `SfTab`, `SfGrid`, `SfMenu` (tabele: `FluentDataGrid` i `EodReport`).
2. **Komponent Syncfusion na ekranie – zawsze przez komponent wzorca** `Eod*` z biblioteki `Eod.Platform.UI.Syncfusion` (osobny projekt: procesy bez tych funkcji nie zależą od Syncfusion). Komponent wzorca ustawia polską lokalizację, formaty dat i godzin, pierwszy dzień tygodnia, zestaw przycisków paska narzędzi, motyw i obsługę zdarzeń zgodną z sekcją 11. Ekran tego nie konfiguruje; brakującą opcję dodaje się do komponentu wzorca.
3. **Miejsce na stronie:** komponent Syncfusion stoi w `EodCard` (zwykle `NoPadding="true" Fill="true"`) na stronie `EodPage` (często w trybie `Fill`). Nagłówek, ścieżka, akcja główna i miejsce komunikatów zostają z `EodPage`.
4. **NIE WOLNO osadzać komponentów Syncfusion w oknach i panelach Fluent** (`FluentDialog`, panel boczny). Okna Fluent v5 to element `<dialog>` w warstwie górnej przeglądarki; wyskakujące elementy Syncfusion (listy, menu, okienka) są dołączane do `<body>`, więc znalazłyby się pod oknem i byłyby nieaktywne. Terminarz, edytory, schemat organizacyjny i menedżer plików stoją na stronach; w oknach – tylko kontrolki Fluent.
5. **Okna i komunikaty komponentu zastępujemy wzorcem**, gdy komponent na to pozwala: edycja wpisu terminarza → okno z `Dialogs/` (zdarzenie otwarcia okna komponentu anulowane, `args.Cancel = true`); potwierdzenia (usunięcie pliku, wpisu) → `UiMessages.ConfirmAsync`; wyniki i błędy → paski `UiMessages`. Wewnętrzne elementy komponentu (pasek narzędzi edytora, menu kontekstowe, okno wstawiania linku, nawigacja terminarza) zostają w wyglądzie Syncfusion.
6. **Jedna akcja główna widoku** (sekcja 4) – przyciski paska narzędzi komponentu nie są akcją główną. „Zapisz dokument”, „Dodaj rezerwację” itp. stoją w nagłówku strony (Fluent).
7. Uprawnienia do operacji (wysyłanie pliku, przeciąganie wpisu, edycja dokumentu) przychodzą z platformy; komponent wzorca ukrywa niedostępne operacje (sekcja 2).
8. **Telemetria Syncfusion wyłączona.** Pakiety 35.x zależą od `Syncfusion.Telemetry`, który wysyła zdarzenia użycia do Azure Application Insights. `AddEodSyncfusion` MUSI wywołać `Syncfusion.Telemetry.Telemetry.Disable()` przed pierwszym użyciem komponentu lub Document SDK; włączenie tylko jawnie (`Syncfusion:Telemetry = true`, ostrzeżenie w logu). Powód: prywatność danych klientów i działanie bez internetu. Narzędzia i programy pomocnicze używające Document SDK poza aplikacją też MUSZĄ wyłączyć telemetrię. Pomiar w demo – tabela pułapek.
9. **Parametry-kolekcje komponentu Syncfusion stałe między renderowaniami.** Pasek narzędzi, menu, lista formatów, nazwy grup itp. – pole lub stała komponentu wzorca, nie nowa lista w każdym renderowaniu: nowa lista to dla komponentu nowa konfiguracja i ciągłe renderowanie (w demo: menedżer plików wysyłał ok. 140 ramek SignalR/s, 1,2 MB/s, bez działania użytkownika).

**Wygląd:**
- **Motyw Syncfusion Fluent 2** – ten sam system projektowy co Fluent UI Blazor v5 (kolor marki motywu `#0f6cbd`, czcionka Segoe UI). Jeden motyw Syncfusion na aplikację: `_content/Syncfusion.Blazor.Themes/fluent2.css` (opcjonalnie lżejszy `fluent2-lite.css`). Bez CDN – arkusze i skrypty z pakietów (działanie bez internetu, sekcja 3).
- **Mostek tokenów:** jedyne miejsce, w którym dotykamy wyglądu Syncfusion, to arkusz `eod-syncfusion.css` w bibliotece. Zmienne motywu Fluent 2 Syncfusion (`--color-sf-primary`, `--color-sf-primary-*`, `--color-sf-content-bg-color*`, `--e-font-name`…) wskazują tokeny Fluent 2 (np. `--color-sf-primary: var(--colorBrandBackground)`, `--e-font-name: var(--fontFamilyBase)`) – w demo ok. 80 zmiennych, w `:root` i `.e-dark-mode`, więc oba motywy idą przez tokeny. Zmiana koloru marki (decyzja 3) = zmiana tokenów Fluent, bez ruszania Syncfusion. NIE WOLNO nadpisywać klas `e-*` na ekranach. Reguły dla klas `e-*` są tylko w `eod-syncfusion.css` (np. typografia treści edytora jak w trybie tylko do odczytu, ramka edytora jak pola Fluent, style linii schematu przez prefiks identyfikatora łącznika).
- **Motyw ciemny** przełącza się razem z motywem Fluent (`AppearanceSettings`). **Rozstrzygnięte (decyzja 17):** klasa `.e-dark-mode` na `<body>` – `eod-syncfusion.js` ustawia ją razem z atrybutem `data-theme="dark"`, który Fluent v5 nadaje `<body>` (MutationObserver, także przy zmianie motywu w „Ustawieniach” bez przeładowania). Arkusz `fluent2.css` ma wartości ciemne w `.e-dark-mode`, a wyskakujące elementy Syncfusion trafiają do `<body>`, więc też są ciemne; osobny arkusz `fluent2-dark.css` (drugi motyw do ładowania i przełączania) nie jest potrzebny.
- **Kolory danych** (rodzaje wpisów terminarza, serie wykresów): najwyżej 6 kolorów z palety person Fluent 2 (`colorPaletteBlue…`, `colorPaletteBrass…`, `colorPaletteMagenta…`, `colorPaletteTeal…`, `colorPaletteLavender…`, `colorPaletteMarigold…` w wariantach `Background2` / `Foreground2` / `BorderActive`), zdefiniowane raz w bibliotece. Kolory statusów (sekcja 14) zostają zarezerwowane dla statusów.
- **Ikony:** wewnątrz komponentu – ikony motywu Syncfusion (czcionka `e-icons`, styl Fluent); własne przyciski dodawane do paska narzędzi komponentu – Fluent System Icons ze słownika (sekcja 13).
- **Skala interfejsu** (`zoom` na `<body>`, sekcja 15): komponenty Syncfusion pozycjonują wyskakujące elementy w JS – działanie przy 90 / 100 / 110 / 125% MUSI być sprawdzone w demo. Przesunięte okienko to błąd blokujący wzorzec. Chromium (zoom zgodny ze standardem) podaje `getBoundingClientRect()` i współrzędne zdarzeń w pikselach okna, a `offsetLeft` i `style.left` – w pikselach powiększonej treści; Syncfusion miesza oba układy. Poprawki są w `eod-syncfusion.js` (komentarz w pliku): pozycja okienek kotwiczonych (`sf.popups.Popup`), współrzędne zdarzeń wskaźnika w terminarzu, menedżerze plików i schemacie, cel menu kontekstowego menedżera plików, pozycja list przycisków (`DropDownButton`). Przy skali 100% nic się nie zmienia. Wyniki – tabela pułapek.

**Lokalizacja i formaty:**
- Teksty komponentów po polsku: zasoby `SfResources.resx` + `SfResources.pl.resx` z repozytorium [syncfusion/blazor-locale](https://github.com/syncfusion/blazor-locale), skopiowane do biblioteki i aktualizowane z wersją pakietów; implementacja `ISyncfusionStringLocalizer` (`SyncfusionPolishLocalizer`) rejestrowana w `AddEodSyncfusion`. Tłumaczenia niezgodne z terminologią (sekcja 21, np. „dział”) poprawiamy w kopii. Stan: kopia z tagu `v35.1.37` (źródło w komentarzu pliku), 174 poprawki z komentarzem `EOD: poprawka tłumaczenia, było „…”` – tłumaczenie maszynowe dawało m.in. „Stół” (tabela), „Wizerunek” (obraz), „Przerobić” (ponów), „Odnaleźć” (znajdź), „Ustęp” (akapit), „Nieruchomości” (właściwości), „Kule” (punktory). Przy aktualizacji pakietów: nowa kopia, poprawki z komentarzy przenieść, przejrzeć teksty widoczne na ekranach wzorca.
- Formaty z kultury `Formats.Culture` (sekcja 5): daty `dd.mm.rrrr`, godziny `HH:mm` (24 h), tydzień od poniedziałku. Komponent wzorca ustawia je jawnie (np. `FirstDayOfWeek` terminarza), nie polega na ustawieniach domyślnych.

**Rejestracja i licencja (wymagania techniczne):**

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(o => o.MaximumReceiveMessageSize = 2 * 1024 * 1024);   // świadomie – tabela pułapek
builder.Services.AddEodPlatformUI();
builder.Services.AddEodSyncfusion(builder.Configuration);   // telemetria wyłączona, licencja z konfiguracji, AddSyncfusionBlazor(), polskie teksty
```

W `App.razor` po `<EodHead />`: `<EodSyncfusionHead />` – motyw `fluent2.css`, `eod-syncfusion.css` i skrypt `_content/Syncfusion.Blazor.Core/scripts/syncfusion-blazor.min.js`; komponenty z własnym skryptem (edytor DOCX: `_content/Syncfusion.Blazor.WordProcessor/scripts/syncfusion-blazor-documenteditor.min.js`) dokłada komponent wzorca.

- Klucz licencji MUSI być zarejestrowany (`Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense`) w aplikacji hosta przed pierwszym użyciem komponentu – `AddEodSyncfusion`, wywoływane w `Program.cs` hosta, robi to przed `builder.Build()`. Klucz czytany z konfiguracji `Syncfusion:LicenseKey` (user-secrets, zmienna środowiskowa `Syncfusion__LicenseKey`, magazyn sekretów; na CI też). **NIE WOLNO** zapisywać klucza w repozytorium, `appsettings.json` ani kodzie.
- Klucz zależy od wersji i platformy pakietów – aktualizacja Syncfusion o wersję główną wymaga nowego klucza (część zadania aktualizacji).
- Bez klucza komponenty UI pokazują komunikat wersji próbnej (baner u góry okna i okno „Claim your free account” przy pierwszym komponencie), a Document SDK i edytor DOCX dodają znak wodny do dokumentów. Pokazy u klientów – tylko z zarejestrowanym kluczem; zrzuty w `docs/ekrany` są na razie z wersji próbnej (opis w `docs/ekrany/README.md`). Brak klucza: w środowisku deweloperskim – informacja w logu przy starcie, w innym – ostrzeżenie. NIE WOLNO ukrywać ani obchodzić komunikatów wersji próbnej (CSS, skrypt).
- Tylko Interactive Server (sekcja 3); przy przejściu na WebAssembly klucza nie rejestruje się w kodzie klienta (zasada Syncfusion).

**Pułapki i rzeczy do sprawdzenia w demo:**

| Pułapka | Skutek | Rozwiązanie we wzorcu | Wynik w demo (2026-10-18, `tools/syncfusion-checks.mjs`) |
|---|---|---|---|
| Limit wiadomości SignalR w Interactive Server (domyślnie 32 KB) | wklejenie dużej treści do edytora, duży dokument DOCX – zerwane połączenie | edytory z `EnableChunkMessages="true"`; host ustawia `MaximumReceiveMessageSize` świadomie – w demo 2 MB (nie „1 GB” z przykładów); pliki strumieniem (`IBrowserFile` w menedżerze plików) albo przez endpointy `/pliki/…` (pobieranie, obrazy edytora) | przy 32 KB: wklejenie 100 KB HTML do edytora treści – OK (treść na serwerze po ~1,4 s), dokument DOCX 1,1 MB / 7 stron – OK (gotowy ≤ 4,3 s od otwarcia strony), ale 100 KB tekstu w zwykłym polu (`FluentTextArea`) zrywa połączenie; przy 2 MB – wszystkie trzy OK |
| Wyskakujące elementy Syncfusion w `<body>`, okna Fluent w warstwie górnej | lista lub menu pod oknem, nieaktywne | zasada 4: bez Syncfusion w oknach Fluent; edycja wpisu terminarza – okno `Dialogs/BookingDialog` z kontrolkami Fluent | okno „Dodaj rezerwację” nad terminarzem: 0 elementów Syncfusion w oknie, lista „Rodzaj” otwiera się i wybiera wartość, po zamknięciu szybki podgląd terminarza działa |
| CSS `zoom` na `<body>` | przesunięte okienka, zła pozycja przeciągania | test każdej skali; poprawka w komponencie wzorca (`eod-syncfusion.js`), nie na ekranie | 90 / 100 / 110 / 125%: szybki podgląd 9 / 10 / 11 / 12 px od wpisu; przeciągnięty wpis ląduje w komórce docelowej (osoba, dzień); menu kontekstowe plików i lista „Akapit” edytora ≤ 2 px od punktu kliknięcia / przycisku; przesunięcie schematu = ruch wskaźnika (−200 px), kliknięcie węzła zaznacza właściwy węzeł. Bez poprawek: podgląd przesunięty o (skala − 1) × szerokość wpisu, wpis lądował dzień wcześniej (90%), menu i listy w punkcie × skala |
| Tryb ciemny wyskakujących elementów | jasne okienka w ciemnym motywie | klasa `.e-dark-mode` na `<body>` (decyzja 17) | szybki podgląd `rgb(31,31,31)`, terminarz, menu kontekstowe, lista i pasek edytora `rgb(41,41,41)` |
| Prerendering (Interactive Server) | komponent bez skryptów do nawiązania połączenia | komponent wzorca renderuje komponent Syncfusion po pierwszym renderowaniu interaktywnym (`EodSyncfusionHost`), wcześniej `FluentSkeleton` w jego rozmiarze; układ zależny od szerokości (telefon) ustalany przed utworzeniem komponentu | 6 tras: HTML z serwera ma zastępnik, bez znaczników Syncfusion; brak wyjątków w logu serwera |
| Telemetria `Syncfusion.Telemetry` (zasada 8) | zdarzenia użycia wysyłane do Azure Application Insights | `Telemetry.Disable()` w `AddEodSyncfusion` | demo uruchomione przez lokalne proxy zapisujące połączenia (`HTTPS_PROXY`, bez przekazywania dalej), przejście przez wszystkie ekrany Syncfusion i generowanie PDF: **0** połączeń; kontrola z `Syncfusion:Telemetry = true`: 35 prób połączenia z `eastus-8.in.applicationinsights.azure.com` w ciągu minuty. Przeglądarka: 0 żądań poza adres demo (bez CDN) |
| Dwa zestawy CSS (Fluent `reboot.css` i motyw Syncfusion) | różnice czcionek, odstępów, obramowań | mostek tokenów; porównanie zrzutów z ekranami Fluent | listy w treści edytora bez numerów i punktorów (reset list), nagłówki 2–3× większe niż w aplikacji – typografia treści w `eod-syncfusion.css` |
| Nowa lista jako parametr przy każdym renderowaniu (zasada 9) | ciągłe renderowanie i ruch SignalR, opóźnione kliknięcia (zaznaczenie pliku po 8–12 s) | listy pasków i menu w polach komponentu wzorca | 0 ramek SignalR w spoczynku na wszystkich ekranach Syncfusion |
| Menedżer plików: element z `Id` | komponent przechodzi w tryb identyfikatorów (`Id` / `ParentId` / `FilterId`) – „Wytnij” i przeciąganie kończą się wyjątkiem | elementy bez `Id` (tryb ścieżek `FilterPath`); odpowiedź operacji zawiera element po zmianie (inaczej okno zmiany nazwy się nie zamyka) | wytnij / wklej, przeciąganie do folderu (wszystkie skale), nowy folder, zmiana nazwy, usunięcie z potwierdzeniem, wysyłanie, pobieranie – OK |
| Schemat: łącznik między węzłami = krawędź drzewa układu `OrganizationalChart` | zastępstwo / zależność funkcjonalna daje osobie drugiego „przełożonego” – układ się rozpada | szkielet drzewa = podległość; zależności dodatkowe to łączniki zaczepione w punktach, przeliczane po każdym układzie | style linii z legendy widoczne w obu motywach; łącznik osoby ukrytej w zwiniętej gałęzi – od jej departamentu |
| Schemat: węzły HTML (szablon) | zdarzenie `Click` diagramu nie przychodzi (sterowanie `ZoomPan`); węzłów HTML nie ma w eksporcie PNG / JPEG (dokumentacja Syncfusion) | kliknięcie obsługuje szablon węzła (`@onclick` – trafienie w DOM nie zależy od skali); „Eksport PNG” nie zbudowany – decyzja 31 | kliknięcie węzła zaznacza go przy każdej skali |
| Edytor DOCX i Document SDK na Linuksie | `SkiaSharp.NativeAssets.Linux.NoDependencies` nie ma fontconfig – renderer nie widzi czcionek systemu, PDF ma tylko linie tabel i znak wodny | czcionki dokumentu podawane strumieniem pliku zamiennika metrycznego (Calibri → Carlito, Arial → Liberation Sans…) w zdarzeniu `SubstituteFont` usługi dokumentów | PDF z polskimi znakami, czcionki osadzone (Carlito); DOCX 1,1 MB → PDF w ~1,8 s |
| Licencja próbna | baner u góry okna zasłania nagłówek strony (telefon: tytuł strony); okno „Claim your free account” szersze niż telefon (✕ poza ekranem) | nie obchodzimy (zasada licencji); zrzuty zamykają okno jego przyciskiem ✕ | opis w `docs/ekrany/README.md`; znika po rejestracji klucza |
| Klucz licencji na CI | zrzuty i testy z banerem wersji próbnej | klucz w sekretach CI | – (demo bez klucza, decyzja właściciela) |

**Dla agentów:** Syncfusion publikuje oficjalne instrukcje dla asystentów AI do komponentów Blazor ([syncfusion/blazor-ui-components-skills](https://github.com/syncfusion/blazor-ui-components-skills): scheduler, file-manager, diagram, rich-text-editor, charts, gantt-chart, themes, license, common). Używaj ich do API komponentów; **zasady wyglądu, komunikatów i układu z tej specyfikacji mają pierwszeństwo** (np. motyw Fluent 2 zamiast domyślnego w nich Material 3, okna `Dialogs/` zamiast `SfDialog`).

### 3.3 Język kodu i dokumentacja

- **Kod po angielsku (MUSI):** typy, komponenty (`Eod` + angielska nazwa: `EodCard`, `EodReport`), parametry komponentów (`Title`, `Description`, `Fill`), właściwości, wartości wyliczeń (`FormMode.AllSections`), metody (asynchroniczne z sufiksem `Async`: `ConfirmAsync`, `OpenAsync`), zmienne, pola prywatne (`_camelCase`), funkcje JS (`window.EodUI.*`), klasy CSS (`eod-*`), identyfikatory DOM, klucze konfiguracji (`Syncfusion:LicenseKey`). Nazwy idiomatyczne dla .NET, wartości logiczne jako orzeczenia (`IsKey`, `CanAdd`, `Required`).
- **Po polsku (MUSI):** wszystko, co widzi użytkownik (teksty, etykiety, komunikaty, podpowiedzi, `aria-label`, tytuły okien i kart przeglądarki), adresy stron i nazwy parametrów adresu (`/sprawy/ZR-2026-0041`, `/raporty/rekrutacje`, `?karta=komentarze`, `?formularz=zlecenie-rekrutacji`; w szablonie trasy po angielsku jest tylko nazwa parametru komponentu: `@page "/sprawy/{Number}"`), wartości danych (statusy „W toku”, dane przykładowe), nazwy zrzutów w `docs/ekrany`, specyfikacja i README, komentarze w kodzie, opisy commitów.
- **Dokumentacja XML po polsku** wg dobrych praktyk .NET: każdy publiczny i chroniony typ i członek w plikach `.cs` ma `///` – `<summary>` (co i po co, jedno–dwa zdania, nie powtórzenie nazwy), `<param>` dla każdego parametru, także pozycyjnych parametrów rekordów, `<typeparam>`, `<returns>`, `<exception>` dla rzucanych wyjątków, `<remarks>` (zasady wzorca, pułapki, odwołania do specyfikacji: „specyfikacja UI, sekcja 9.3”), `<example><code>…</code></example>` dla punktów wejścia i nieoczywistych API; `<see cref="…"/>` z aktualnymi nazwami; każda wartość wyliczenia ma `<summary>`. Terminologia i formaty jak w interfejsie (sekcje 5 i 21).
- **Komponent Razor:** komentarz `@* … *@` zaraz po dyrektywach (rola i sekcja specyfikacji, kiedy używać, najważniejsze zasady, krótki przykład użycia) i `/// <summary>` przy każdej właściwości `[Parameter]` / `[CascadingParameter]` (z `<value>` / `<remarks>`: wartość domyślna, dopuszczalne wartości, jednostki). Publiczne metody komponentu (np. `EodForm.ShowSectionAsync`) – jak w plikach `.cs`.
- Oba projekty (`Eod.Platform.UI`, `Eod.Demo`) mają `<GenerateDocumentationFile>true</GenerateDocumentationFile>` – brak dokumentacji publicznego członka to ostrzeżenie CS1591. **Build MUSI kończyć się bez ostrzeżeń** (`dotnet build Eod.Ui.slnx`: 0 ostrzeżeń, 0 błędów).
- Nazwy w tej specyfikacji (komponenty, parametry, usługi) MUSZĄ odpowiadać kodowi – zmiana API = zmiana specyfikacji w tym samym commicie (sekcja 18).

## 4. Zasady ogólne (najważniejsze – czytać zawsze)

1. **Jedna akcja główna (Primary) na widok i na okno.** Stoi pierwsza z lewej w sektorze przycisków. Pozostałe przyciski: Default; akcje narzędziowe: Subtle.
2. **Ta sama czynność = ta sama etykieta i ta sama ikona w całej aplikacji** (słownik ikon, sekcja 13). Przycisk nazywa czynność czasownikiem: „Zapisz”, „Przekaż do akceptacji”, „Usuń” – nigdy „OK/Tak/Nie” w oknach decyzji.
3. **Dwa poziomy komunikatów:** pasek (MessageBar) dla operacji mniej istotnych i walidacji; okno (MessageBox) dla operacji istotnych, wyników operacji istotnych i **każdego potwierdzenia** (sekcja 11).
4. **Stałe miejsce na komunikaty** – pod nagłówkiem strony; na formularzu sprawy przyklejone razem z sektorem przycisków.
5. **Treść zajmuje całą przestrzeń roboczą.** Strona nie ma limitu szerokości ani centrowania; **ostatnia karta strony rozciąga się do dolnej krawędzi** (wyjątek: formularz sprawy i strony-galerie rosną z treścią).
6. **Menu boczne ma ten sam układ w każdym kontekście procesu.** Kontekst zawęża listy i liczniki, nie zmienia pozycji menu.
7. **Każdy proces wygląda tak samo.** Formularz sprawy, raporty i strona główna procesu to te same komponenty; proces dostarcza tylko treść. Dotyczy też komponentów Syncfusion – zawsze przez komponenty wzorca (sekcja 3.2).
8. **Kolory statusów są zarezerwowane dla statusów** (zielony = pozytywny wynik, żółty = uwaga / termin blisko, czerwony = błąd / po terminie, niebieski marki = w toku). Status zawsze jako FluentBadge `Appearance="Tint"` z tekstem – nigdy sam kolor.
9. **Ustawienia użytkownika zapisują się od razu** po zmianie (bez przycisku „Zapisz”), MessageBar potwierdza.
10. **Pole tylko do odczytu – dwie formy:** sekcja w całości tylko do odczytu = etykieta + tekst (bez ramek pól); pojedyncze pole nie do edycji w edytowalnym formularzu = `ReadOnly` + wypełnienie `FilledDarker` + ikona kłódki. Formularz i sprawa w całości tylko do odczytu – sekcja 9.3.3.
11. **Terminologia: „departament”**, nigdy „dział”. Pełny słownik pojęć – sekcja 21.
12. **Interfejs w języku polskim**, poprawna odmiana liczebników („1 sprawa, 2 sprawy, 5 spraw”), polskie formaty dat i liczb (sekcja 5).
13. Dostępność jest wymaganiem, nie dodatkiem (sekcja 16).

## 5. Język, formaty, teksty

- **Daty:** `dd.mm.rrrr` (08.10.2026); data z godziną `dd.mm.rrrr gg:mm`; tydzień zaczyna się w poniedziałek; nazwa miesiąca małą literą w tekście („8 października 2026”).
- **Termin względny:** „dziś”, „jutro”, „za 4 dni”, „2 dni po terminie”.
- **Liczby:** separator tysięcy = spacja niełamliwa, część dziesiętna po przecinku (`12 000,00`). W tabelach i datach cyfry o stałej szerokości (`font-variant-numeric: tabular-nums`, klasa `eod-num`), liczby i kwoty wyrównane do prawej.
- **Kwoty:** w tekście i tabelach `12 000,00 zł`; w polu kwoty sufiks z kodem waluty `PLN`. Procenty bez spacji: `45%`.
- **Odmiana:** helper `PolishPlural.WithCount(n, "sprawa", "sprawy", "spraw")` (zasada 1 / 2–4 bez 12–14 / reszta).
- **Etykiety pól:** rzeczownik, wielka litera tylko na początku („Termin składania aplikacji”). Jednostka NIE w etykiecie, tylko jako prefiks/sufiks pola.
- **Podpowiedź pod polem** mówi, co wpisać; **komunikat walidacji** mówi, jak poprawić („Podaj pełny adres, np. imie.nazwisko@firma.pl.”), nie „Pole jest niepoprawne”.
- **Teksty pasków i okien:** pogrubiony tytuł mówi, co się stało; zdanie po nim – co z tego wynika lub co zrobić. Tytuł okna potwierdzenia jest pytaniem („Przekazać sprawę do akceptacji?”), tytuł okna wyniku jest wynikiem („Ogłoszenie opublikowane”). Treść podaje numer sprawy, skutek i odbiorców.
- **Pusta lista:** zdanie, co tu będzie i dlaczego pusto („Brak pozycji spełniających filtry.”, „Nikt jeszcze nie skomentował tej sprawy.”).

## 6. Szkielet aplikacji

Komponent: `EodShell` (na bazie `FluentLayout`). Zrzut: `docs/ekrany/start.png`.

```
┌──────────────────────────────────────────────────────────────────────────────┐
│ ☰ MOJE EOD │ [▦ Wszystkie procesy ▾]                       [🔔4] [(MW) Marta W.▾] │ belka 48 px, tło marki
├───────────────┬──────────────────────────────────────────────────────────────┤
│ [+ Nowa sprawa▾]│                                                              │
│ ⌂ Start         │   treść strony (EodPage)                                   │
│ ☑ Moje zadania 9│   tło colorNeutralBackground3, karty colorNeutralBackground1 │
│ 👁 Obserwowane   │                                                              │
│ 🗄 Zakończone    │                                                              │
│ 📊 Raporty       │                                                              │
│ INTRANET        │                                                              │
│ …               │                                                              │
│ ─────────────── │                                                              │
│ stopka menu     │                                                              │
└─────────────────┴──────────────────────────────────────────────────────────────┘
  menu 264 px, tło colorNeutralBackground4
```

**Belka górna** (`FluentLayoutItem Area="Header"`, 48 px, tło marki, tekst `colorNeutralForegroundOnBrand`):
- nazwa aplikacji „MOJE EOD” (link do Startu), separator;
- **przełącznik kontekstu procesu** (`EodContextSwitcher` = FluentMenuButton + FluentMenu z pozycjami radio): „Wszystkie procesy” + lista procesów dostępnych dla użytkownika. Zmiana kontekstu przechodzi na stronę główną kontekstu (`/` albo `/proces/{kod}`), przeładowuje menu „Nowa sprawa” i liczniki;
- po prawej: **powiadomienia** (ikona dzwonka + FluentCounterBadge `danger`, otwiera panel boczny) i **menu użytkownika** (awatar + imię i nazwisko): Ustawienia użytkownika · Moje zastępstwa · Administracja (jeśli rola – prowadzi do pierwszego ekranu sekcji „Administracja” menu bocznego, sekcja 9.13) · separator · Wyloguj.

**Menu boczne** (`FluentLayoutItem Area="Navigation"` + `FluentNav`, 264 px):
- na górze **jeden** przycisk Primary „Nowa sprawa” (FluentMenuButton) z menu formularzy startowych kontekstu – to jedyne stałe miejsce uruchamiania procesów; pozycja otwiera stronę nowej sprawy `/sprawy/nowa?formularz={kod}` (sekcja 9.3.1);
- pozycje (zawsze te same, w tej kolejności): **Start** (w kontekście procesu: „Start procesu”) · **Moje zadania** (z licznikiem) · **Obserwowane** · **Zakończone** · **Raporty**;
- w kontekście „Wszystkie procesy” dodatkowo sekcja **Intranet** (Aktualności, Regulacje wewnętrzne, Książka adresowa);
- w demo sekcja Intranet ma też ekrany wzorca na komponentach Syncfusion (sekcja 9.12): Nowa aktualność, Struktura organizacyjna, Biblioteka dokumentów, Terminarz; edytor DOCX otwiera się z karty „Dokumenty i wiadomości” formularza sprawy;
- sekcja **Administracja** (`FluentNavSectionHeader` + pozycje: Użytkownicy, Słowniki, kolejne ekrany administracji) – tylko dla ról administracyjnych; widoczność sekcji i pozycji ustala platforma (uprawnienia, sekcja 2), wzorzec jej nie liczy. Stoi po Intranecie i jest taka sama w każdym kontekście procesu (administracja nie zależy od procesu). W demo – zawsze widoczna (sekcja 9.13);
- ikony Regular 20; pozycja aktywna ma ikonę w kolorze marki i tło `colorNeutralBackground4Hover`; raport otwarty z katalogu podświetla pozycję „Raporty”;
- stopka menu: caption (wersja aplikacji, nazwa klienta). W aplikacji demo menu ma dodatkową sekcję **„Wzorzec UI”** (Formularz sprawy, Kontrolki, Komunikaty i okna, Stany ekranów) – tylko w demo, nie w produkcie.

**Treść** (`FluentLayoutItem Area="Content"`): przewija się tylko obszar treści; belka i menu stoją.

**Mobilnie (< 900 px):** menu boczne chowa się, w belce pojawia się przycisk menu (`FluentLayoutHamburger`), menu otwiera się jako panel; imię i nazwisko w belce znika (zostaje awatar); odstęp strony 16 px. `FluentLayout MobileBreakdownWidth="900"`.

**Rozmiar interfejsu (skala globalna):** jedna skala dla tekstu, kontrolek i odstępów (CSS `zoom` na `<body>`, zmienna `--eod-zoom`). Automatycznie: okno < 1440 px → 90%, 1440–2199 px → 100%, ≥ 2200 px → 110%. Ręcznie w ustawieniach: 90 / 100 / 110 / 125% (atrybut `data-scale` na `<html>`). Motyw: jak w systemie / jasny / ciemny (`IThemeService` Fluent; wybór zapamiętany w przeglądarce). Obsługa: usługa `AppearanceSettings` (sekcja 18). Wysokość układu: `FluentLayout Height="calc(100dvh / var(--eod-zoom, 1))"` (100dvh pod zoomem dałoby 90% / 110% okna).

## 7. Anatomia strony

Komponent: `EodPage`. Każdy ekran (poza formularzem sprawy) jest stroną:

```
EodPage
├── nagłówek: [ścieżka] · Tytuł (H1) · opis (caption) ……… [akcja główna strony]
├── miejsce komunikatów (FluentMessageBarProvider, sekcja „strona” – EodSections.Page)
└── treść: karty (EodCard) i siatki (FluentGrid Spacing="2")
```

- Odstępy strony: góra 20 px, boki 24 px (16 px poniżej 900 px), dół 24 px, odstęp między blokami 16 px.
- **Nagłówek strony:** tytuł `fontSizeBase600` semibold; opis `fontSizeBase200` w `colorNeutralForeground3` – mówi, co jest na stronie (i stan danych w raportach). Akcja główna strony (np. „Dodaj zastępstwo”, „Zlecenie rekrutacji”) stoi po prawej stronie nagłówka, wyrównana do dołu.
- **Karta** (`EodCard`): tło `colorNeutralBackground1`, obramowanie `colorNeutralStroke2` 1 px, promień `borderRadiusXLarge`. Nagłówek karty min. 56 px: tytuł H2 (`fontSizeBase400` semibold) + podpis + akcje (Subtle, small, np. „Wszystkie ›”). Treść 16 px odstępu albo bez odstępu (tabela, lista). Opcjonalnie stopka (caption, 8/16 px) i blok „zasad” (tło `colorNeutralBackground2`).
- **Ostatnia karta strony wypełnia wolną wysokość** (`Fill="true"`; na stronie startowej ostatni wiersz siatki). Stopka karty zostaje przy dolnej krawędzi.
- **Siatka:** `FluentGrid Spacing="2"` z `FluentGridItem` (punkty łamania: xs < 600, sm ≥ 600, md ≥ 960, lg ≥ 1280, xl ≥ 1920). Typowe układy: 8 + 4 (lista + panel boczny), 7 + 5 (dwie karty ustawień).
- **Raport** to strona w trybie `Fill`: strona ma wysokość obszaru treści, przewija się tylko tabela.

## 8. Katalog ekranów

| Ekran | Adres (`Eod.Demo`) | Plik | Zrzut | Wzorzec |
|---|---|---|---|---|
| Start (wszystkie procesy) | `/` (tylko demo: `/?demo=pusto` – bez zadań) | `Pages/Home.razor` (+ `Pages/CaseList.razor`) | `start.png`, `-ciemny`, `-telefon`, `start-pusto.png` | 9.1, 9.14 |
| Start procesu | `/proces/{kod}` (rekrutacja, onboarding, urlopy) | `Pages/ProcessHome.razor` | `start-procesu.png`, `start-procesu-onboarding.png`, `start-procesu-urlopy.png` | 9.2 |
| Moje zadania | `/zadania` (tylko demo: `/zadania?demo=pusto` – bez zadań) | `Pages/MenuReport.razor` → `Pages/ReportView.razor` | `moje-zadania.png`, `-ciemny`, `-telefon`, `moje-zadania-pusto.png` | 9.7, 9.14 |
| Obserwowane | `/obserwowane` | jw. | `obserwowane.png` | 9.7 |
| Zakończone | `/zakonczone` | jw. | `zakonczone.png` | 9.7 |
| Katalog raportów | `/raporty` | `Pages/Reports.razor` | `raporty.png` | 9.8 |
| Raport z katalogu | `/raporty/{klucz}` (rekrutacje, onboarding, urlopy) | `Pages/Report.razor` → `Pages/ReportView.razor` | `raport-grupowanie.png`, `raport-filtr.png`, `raport-onboarding.png`, `raport-urlopy.png` | 9.7 |
| Formularz sprawy (sprawa w toku) | `/sprawy/{numer}` (wzorcowa: `ZR-2026-0041`; `?karta=komentarze`) | `Pages/Cases/CasePage.razor` (wybór widoku sprawy) → `Pages/Cases/CaseDetails.razor` + karty i model w `Pages/Cases/` | `sprawa-formularz.png` (`-ciemny`, `-telefon`), `sprawa-walidacja.png`, `sprawa-zalaczniki.png`, `sprawa-komentarze.png`, `sprawa-akceptacje.png` | 9.3–9.6 |
| Formularz startowy (nowa sprawa) | `/sprawy/nowa?formularz={kod}` (wzorcowy: `zlecenie-rekrutacji`) | `Pages/Cases/NewCase.razor` + `RecruitmentStartForm.razor`, `NewRecruitmentRequest.cs` | `nowa-sprawa.png` (`-ciemny`, `-telefon`), `nowa-sprawa-walidacja.png` | 9.3.1 |
| Sprawa do decyzji (widok akceptującego) | `/sprawy/WU-2026-0871` | `Pages/Cases/CasePage.razor` → `LeaveRequestCase.razor` + `LeaveRequestForm.razor`, `LeaveRequest.cs` | `sprawa-decyzja.png` (`-ciemny`, `-telefon`), `okno-odrzucenia.png` | 9.3.2 |
| Sprawa zakończona (tylko do odczytu) | `/sprawy/ZR-2026-0027` | `Pages/Cases/CasePage.razor` → `ClosedRecruitmentCase.razor` (+ `RecruitmentForm ReadOnly`) | `sprawa-zakonczona.png` (`-ciemny`, `-telefon`), `sprawa-zakonczona-zalaczniki.png`, `sprawa-zakonczona-komentarze.png` | 9.3.3 |
| Ustawienia użytkownika | `/ustawienia` | `Pages/Settings.razor` | `ustawienia.png` | 9.9 |
| Moje zastępstwa | `/zastepstwa` | `Pages/Substitutions.razor` | `zastepstwa.png`, `zastepstwo-okno.png` | 9.10 |
| Administracja › Użytkownicy | `/administracja/uzytkownicy` | `Pages/Admin/Users.razor` | `uzytkownicy.png`, `-ciemny`, `-telefon`, `uzytkownik-okno.png` | 9.13 |
| Administracja › Słowniki | `/administracja/slowniki` (`?slownik={klucz}` – słownik wybrany przy wejściu) | `Pages/Admin/Dictionaries.razor` | `slowniki.png`, `slowniki-telefon.png`, `slowniki-edycja.png` | 9.13 |
| Kontrolki (galeria) | `/wzorzec/kontrolki` | `Pages/Showcase/Controls.razor` | `kontrolki.png` | 10 |
| Komunikaty i okna (galeria) | `/wzorzec/komunikaty` | `Pages/Showcase/MessagesAndDialogs.razor` | `komunikaty.png`, `okno-potwierdzenia.png` | 11 |
| Stany ekranów (galeria) | `/wzorzec/stany` | `Pages/Showcase/ScreenStates.razor` | `stany.png` | 9.14 |
| Nie znaleziono | każdy nieznany adres (`/nie-znaleziono`, kod 404; odpowiedzi bez treści – `/blad/{kod}`, `Pages/StatusCodePage.razor`) | `Pages/NotFound.razor` | `nie-znaleziono.png` | 7, 9.14 |
| Brak dostępu | `/brak-dostepu`, sprawa bez dostępu (demo: `/sprawy/ZR-2026-0007`), kod 403 | `Pages/AccessDenied.razor` | `brak-dostepu.png` | 9.14 |
| Błąd aplikacji | `/Error` (poza środowiskiem deweloperskim) | `Pages/Error.razor` | – | 7 |
| Panel powiadomień | (belka → dzwonek) | `Dialogs/NotificationsPanel.razor` | `powiadomienia.png` | 9.11 |
| Historia sprawy (panel) | (formularz → Historia, raport → Akcje) | `Dialogs/HistoryPanel.razor` | `historia.png` | 9.6 |
| Uprawnienia do sprawy (okno) | (formularz → tarcza przy wykonawcy) | `Dialogs/PermissionsDialog.razor` | `uprawnienia.png` | 9.6 |
| Przypisz sprawę innej osobie (okno) | (formularz → Zmień, raport → Więcej) | `Dialogs/AssignDialog.razor` | – | 9.6 |
| Zwrot do poprawy (okno) | (formularz → Zwróć do poprawy) | `Dialogs/ReturnReasonDialog.razor` | – | 9.3 |
| Odrzucenie sprawy (okno) | (widok akceptującego → Odrzuć) | `Dialogs/RejectDialog.razor` | `okno-odrzucenia.png` | 9.3.2 |
| Zmiana hasła (okno) | (Ustawienia → Zmień hasło) | `Dialogs/ChangePasswordDialog.razor` | – | 9.9 |
| Zastępstwo (okno) | (Zastępstwa → Dodaj / Edytuj) | `Dialogs/SubstitutionDialog.razor` | `zastepstwo-okno.png` | 9.10 |
| Użytkownik (okno) | (Użytkownicy → Dodaj użytkownika / Edytuj) | `Dialogs/UserDialog.razor` | `uzytkownik-okno.png` | 9.13 |
| Terminarz (grafik zespołów, Syncfusion Scheduler) | `/terminarz` | `Pages/Intranet/TeamCalendar.razor` (dane: `Data/DemoCalendar.cs`) | `terminarz.png`, `-ciemny`, `-telefon` | 9.12 |
| Rezerwacja (okno) | (Terminarz → Dodaj rezerwację, szybki podgląd → Edytuj, kliknięcie wolnego miejsca) | `Dialogs/BookingDialog.razor` | `terminarz-okno.png` | 9.12 |
| Biblioteka dokumentów (Syncfusion File Manager) | `/dokumenty` | `Pages/Intranet/DocumentLibrary.razor` (usługa: `Data/DemoFileLibrary.cs`) | `biblioteka-dokumentow.png`, `-ciemny`, `-telefon` | 9.12 |
| Struktura organizacyjna (Syncfusion Diagram) | `/intranet/struktura` | `Pages/Intranet/OrgStructure.razor` (dane: `Data/DemoOrganization.cs`) | `schemat-organizacyjny.png`, `-ciemny`, `schemat-lista-telefon.png` | 9.12 |
| Nowa aktualność (Syncfusion Rich Text Editor) | `/intranet/aktualnosci/nowa` | `Pages/Intranet/NewsEditor.razor` (`Services/DemoNewsStore.cs`) | `edytor-tresci.png`, `-ciemny` | 9.12 |
| Edytor dokumentu DOCX (Syncfusion DOCX Editor) | `/sprawy/{numer}/dokumenty/{id}` (`ogloszenie`; `plan-zatrudnienia` – ~1 MB, zatwierdzony, tylko do odczytu) | `Pages/Cases/CaseDocumentEditor.razor` (`Services/DemoDocumentStore.cs`, `Services/DocumentService.cs`) | `edytor-docx.png`, `-ciemny` | 9.12 |
| Generowanie dokumentu PDF (Document SDK) | (formularz → Więcej → Generuj dokument PDF; karta „Dokumenty i wiadomości”; `?demo=blad-pdf` – pierwsza próba kończy się błędem) | `Pages/Cases/DocumentsTab.razor`, `Services/DocumentService.cs` | `generuj-pdf.png` | 9.6, 9.12 |

Ścieżki plików – względem `src/Eod.Demo`; dane przykładowe w `Data/` (`DemoData`, `DemoReports`, `AppState`; administracja – `AdminDemoData`).

**Okna i panele** to komponenty w `Dialogs/`: dziedziczą `FluentDialogInstance` (tytuł i przyciski w `OnInitializeDialog`, walidacja w `OnActionClickedAsync` – przy błędzie okno zostaje otwarte) albo – panel boczny – `FluentDialogBody` z kaskadowym `IDialogInstance`. Każde ma statyczną metodę `OpenAsync(IDialogService dialogs, …)` zwracającą `Task<DialogResult>`; ekran nie konfiguruje `DialogOptions` sam.

**Ekrany na komponentach Syncfusion** (wzorce w sekcji 9.12) – w tabeli powyżej; pliki demo do pobierania idą przez endpointy `/pliki/…` (`Services/DemoFileEndpoints.cs`). Do zbudowania: wykresy (`EodChart`).

Nowy ekran, którego tu nie ma (np. kolejny ekran administracji – role, struktura, parametry procesów), składa się z tych samych elementów (dla administracji – wg wzorca 9.13): `EodPage` + `EodCard` + FluentGrid + FluentDataGrid / `EodReport` + kontrolki z sekcji 10 (+ komponenty wzorca Syncfusion z sekcji 3.2 w ich obszarach).

## 9. Wzorce ekranów

### 9.1 Strona startowa (kontekst „Wszystkie procesy”)

- Nagłówek: „Dzień dobry, {imię w wołaczu}” + opis „{dzień tygodnia, data} · masz N spraw do wykonania, w tym M po terminie.”
- Pod nagłówkiem może stać MessageBar o sprawie wymagającej uwagi (np. zaległe urlopy) z jedną akcją.
- Siatka 8 + 4: **„Sprawy do wykonania”** (5 najpilniejszych wg terminu, link „Wszystkie ›” do Moich zadań; kolumny: Sprawa [numer-link + przedmiot], Proces, Etap, Termin [data + znaczek]) · obok stos kart-list: **Komunikaty**, **Regulacje wewnętrzne** (ikona, tytuł-link, podpis, opcjonalny znaczek „Nowa”).
- Pod spodem karta **„Intranet – aktualności”** (siatka artykułów: nadtytuł `KATEGORIA · data`, tytuł-link, zajawka).
- Na listach spraw **cały wiersz otwiera sprawę** (w raportach – tylko link, sekcja 9.7).

### 9.2 Strona główna procesu (kontekst procesu)

- Nagłówek: nazwa procesu + jednozdaniowy opis procesu; po prawej akcja główna = formularz startowy procesu („+ Zlecenie rekrutacji”).
- **Kafle z liczbami** (`EodTiles`, auto-fit min. 170 px): pierwszy zawsze „Do wykonania przeze mnie” (dla procesów akceptacyjnych: „Do mojej akceptacji”) → Moje zadania; pozostałe 2–3 prowadzą do raportu procesu. Liczba `fontSizeHero800`; liczba alarmowa (po terminie) w `colorStatusDangerForeground1`. Kafle MUSZĄ zgadzać się z raportami, do których prowadzą.
- Siatka 8 + 4: „Sprawy do wykonania” (wszystkie zadania w procesie, bez kolumny Proces) · karta „Raporty” z raportami procesu (wypełnia wysokość).

### 9.3 Formularz sprawy

Komponent: `EodCaseForm`. Wzorcowa sprawa: Zlecenie rekrutacji ZR/2026/0041, etap „Opis stanowiska”. Zrzuty: `sprawa-formularz.png`, `sprawa-zalaczniki.png`, `sprawa-komentarze.png`, `sprawa-akceptacje.png`. Inne etapy obiegu sprawy w tym samym szkielecie: formularz startowy (9.3.1), sprawa do decyzji akceptującego (9.3.2), sprawa zakończona – tylko do odczytu (9.3.3).

```
A ┌ przyklejona sekcja główna ───────────────────────────────────────────────────┐
  │ Zlecenie rekrutacji › Moje zadania › ZR/2026/0041                            │
  │ Zlecenie rekrutacji ZR/2026/0041          [▶ Przekaż do akceptacji][Zapisz]  │
  │ (Etap: Opis stanowiska)(Termin: 12.10.2026 (4 dni))  [Zwróć do poprawy][…] | [Historia] │
  └──────────────────────────────────────────────────────────────────────────────┘
    [miejsce komunikatów – przyklejone razem z sekcją główną]
B ┌ informacje podstawowe ────────────────────────────── ┬ wykonawca kroku ──────┐
  │ Stanowisko · Departament · Liczba etatów · Data zgłoszenia · Zgłosiła │ (MW) Marta Wysocka [Zmień▾][🛡] │
  ├ kamienie milowe (tylko odczyt) ───────────────────────────────────────────────┤
  │ ✓ Zgłoszenie ── ● Opis stanowiska ── ○ Akceptacja budżetu ── ○ … ── ○ Zamknięcie │
  └──────────────────────────────────────────────────────────────────────────────┘
C ┌ karty: Formularz · Załączniki (3) · Komentarze (4) · Akceptacje (3) · Dokumenty i wiadomości ┐
  │ [opis etapu …]                                   [Wszystkie sekcje | Krok po kroku] │
  │ ┌ spis sekcji ┐ 1. Dane zlecenia (tylko odczyt)                              │
  │ │ ✓ Dane     │ 2. Opis stanowiska                                            │
  │ │ ● Opis     │ 3. Kryteria oceny (tabela edycyjna)                           │
  │ │ ○ Kryteria │ 4. Warunki zatrudnienia                                       │
  │ └────────────┘                                                                │
  └──────────────────────────────────────────────────────────────────────────────┘
```

**A. Sekcja główna (przyklejona u góry treści)**
- Ścieżka: proces › skąd przyszedł użytkownik (np. Moje zadania) › numer sprawy.
- Tytuł: „{nazwa procesu} {numer}” (`fontSizeBase500` semibold).
- Znaczki: etap (`brand`), termin (`warning`, gdy ≤ 3 dni – próg `Statuses.DueSoonThresholdDays`, decyzja 19 w sekcji 20; `danger` po terminie; inaczej `brand`).
- **Sektor przycisków** (kolejność stała): 1 Primary = główne przejście procesu na tym etapie (z ikoną) → akcje poboczne (Default, najwyżej 2: Zapisz, drugie przejście np. Zwróć do poprawy) → menu „Więcej” (ikona …): rzadsze akcje (Generuj PDF, Wyślij e-mail, Obserwuj sprawę, operacje nieodwracalne; „Anuluj sprawę” na końcu za separatorem) → separator → „Historia” (Subtle).
- Pod sekcją główną, przyklejone razem z nią: miejsce komunikatów.
- Poniżej 900 px sekcja główna przestaje być przyklejona.

**B. Informacje podstawowe + wykonawca kroku + kamienie milowe**
- Informacje podstawowe: 4–6 najważniejszych faktów sprawy (`EodFacts`: etykieta caption nad wartością semibold).
- **Wykonawca kroku** (`EodAssignee`, ramka z tłem `colorNeutralBackground2`): awatar, „Wykonawca kroku”, imię i nazwisko, rola; menu „Zmień”: Przypisz sprawę innej osobie… (okno), Przekaż zastępcy (X), Zwróć do puli departamentu; ikona tarczy = okno „Uprawnienia do sprawy” (macierz: interesariusz × Odczyt / Edycja / Decyzja / Załączniki).
- **Kamienie milowe** (`EodMilestones`, styl FluentWizard u góry, tylko odczyt): etapy w kolejności definicji procesu; zakończone = ptaszek, bieżący = wypełnione kółko i pogrubiony tytuł, przyszłe = kółko z numerem; pod tytułem podpis (data · kto, termin, kto wykona). Kamienie to skrót – etap może nie mieć kamienia. **Przebieg sprawy pokazują kamienie milowe i oś czasu w panelu historii – diagramu przebiegu procesu nie budujemy** (decyzja 2026-10-18).

**C. Karty (FluentTabs)** – kolejność stała: **Formularz · Załączniki · Komentarze · Akceptacje · Dokumenty i wiadomości**. Liczniki na kartach (FluentCounterBadge `informative`).

**Karta „Formularz”**
- Pasek nad formularzem: zdanie, co użytkownik uzupełnia na tym etapie + przełącznik trybu **„Wszystkie sekcje” (domyślny) | „Krok po kroku”** (małe FluentTabs Subtle).
- Z lewej **spis sekcji** (styl FluentWizard z lewej, 232 px, przyklejony): numer/ikona stanu, tytuł, podpis stanu („Do uzupełnienia”, „Suma wag: 100%”, „1 ostrzeżenie”, „1 pole do poprawy”). Stan sekcji: zakończona (✓), bieżąca, przyszła, **błąd** (czerwona ikona i podpis), **ostrzeżenie** (żółty podpis). Klik w spis przewija do sekcji (tryb „Wszystkie”) albo ją pokazuje (tryb „Krok po kroku”, z przyciskami Wstecz / Dalej / Gotowe).
- **Sekcja** (`EodFormSection`): nagłówek „N. Tytuł” (H2) + znaczek stanu („Do uzupełnienia” `brand`, „Tylko do odczytu” `subtle` z kłódką, „1 ostrzeżenie” `warning`) + opcjonalna akcja (np. „+ Dodaj kryterium”, small) + jednozdaniowy opis. Odstęp 20 px, separator między sekcjami.
- **Układ pól w sekcji: jedna z czterech siatek** – 1, 2, 3 albo 4 kolumny (`Columns="1..4"`; 2 kol. = xs 12 / md 6; 3 kol. = xs 12 / md 6 / lg 4; 4 kol. = xs 12 / sm 6 / lg 3). Pole może zająć 2 kolumny (`EodField Width="2"`); pole tekstowe wielowierszowe i tabela edycyjna zawsze całą szerokość.
- **Sekcja tylko do odczytu** (dane z wcześniejszych etapów): tło `colorNeutralBackground2`, pola jako `EodReadOnlyField` (etykieta caption + wartość semibold), zwykle 4 kolumny.
- **Tabela edycyjna** w sekcji: FluentDataGrid z kontrolkami w komórkach, ostatnia kolumna = usuń (Subtle, ikona kosza); pod tabelą pasek postępu reguły (np. suma wag = 100%, kolor wg stanu) i zdanie stanu.
- **Pola wymagane** mają gwiazdkę przy etykiecie (`Required`). Walidacja przy próbie przejścia: komunikat pod polem + stan błędu w spisie sekcji + MessageBar błędu „Nie można przekazać sprawy. Formularz zawiera N pól do poprawy w sekcji „X”.” z akcją „Przejdź do pola”. Od pierwszej próby błędy odświeżają się podczas poprawiania; po poprawieniu ostatniego pola pasek błędu znika sam (`UiMessages.CloseAsync`).

**Przejścia procesu (przyciski)** – zawsze okno potwierdzenia: tytuł-pytanie, treść ze skutkiem i odbiorcami („Sprawa ZR/2026/0041 trafi do akceptujących budżet: …”), przyciski „Przekaż” / „Anuluj”. Po wykonaniu: okno sukcesu z nowym etapem; kamienie milowe, znaczek etapu i wykonawca odświeżają się. Przejście wymagające danych (powód zwrotu) = okno niestandardowe z polem wymaganym. Operacja nieodwracalna z wieloma skutkami (publikacja ogłoszenia) = okno ostrzeżenia z listą skutków.

#### 9.3.1 Formularz startowy (nowa sprawa)

Komponent: `EodCaseForm`, jak sprawa w toku. Adres: `/sprawy/nowa?formularz={kod formularza}` – otwiera go menu „Nowa sprawa” (sekcja 6) i akcja główna strony procesu (9.2). Zrzuty: `nowa-sprawa.png` (`-ciemny`, `-telefon`), `nowa-sprawa-walidacja.png`.

- Sprawa jeszcze nie istnieje: **bez numeru, wykonawcy kroku, uprawnień i historii**. Ścieżka „proces › Nowa sprawa”, tytuł „Nowa sprawa: {nazwa formularza}”, znaczek **„Szkic”** (`informative`, sekcja 14).
- Sektor przycisków: **„Wyślij”** (Primary, `Send`) · „Zapisz szkic” (`Save`) · „Anuluj” – bez menu „Więcej” i bez „Historii”.
- Część B: informacje podstawowe (proces, zgłaszający, departament, data utworzenia, „Numer sprawy: nadany po wysłaniu”); kamienie milowe = **plan procesu** (pierwszy etap bieżący, w podpisach – kto wykona etap).
- Część C: tylko karty **„Formularz” i „Załączniki”** (kolejność jak w sprawie); komentarze, akceptacje i dokumenty pojawiają się po wysłaniu. Karta „Formularz” ma pola pierwszego etapu w tym samym układzie co każdy formularz (pasek, spis sekcji, sekcje, siatki 1–4 kolumn).
- „Wyślij” jest przejściem procesu: walidacja jak w sprawie w toku (pasek błędu „Nie można wysłać sprawy.” z akcją „Przejdź do pola”, błąd pod polem i w spisie sekcji) → okno potwierdzenia („Wysłać zlecenie rekrutacji?” – komu trafi sprawa i co dalej) → przycisk w stanie wczytywania → okno sukcesu „Sprawa utworzona” z numerem sprawy i przyciskiem „Przejdź do sprawy” → formularz sprawy.
- „Zapisz szkic” – pasek sukcesu („Zapisano szkic. … Sprawa nie trafiła jeszcze do obiegu.”). „Anuluj” bez zmian – od razu wyjście na stronę startową kontekstu; z niezapisanymi zmianami – okno ostrzeżenia „Zamknąć formularz bez zapisywania?” („Zamknij bez zapisywania” / „Wróć”), jak przy edytorze dokumentu (9.12).
- W czasie wysyłania i zapisu pozostałe przyciski sektora są wyłączone (9.14). Nieznany kod formularza – strona „Nie znaleziono”. Demo ma jeden formularz startowy („Zlecenie rekrutacji”) – inny formularz z menu otwiera się w jego układzie z paskiem informacji.

#### 9.3.2 Sprawa do decyzji (widok akceptującego)

Wzorcowa sprawa: wniosek urlopowy WU/2026/0871 na etapie „Akceptacja przełożonego”, zalogowana osoba jest wykonawcą kroku (`/sprawy/WU-2026-0871`, zadanie w „Moich zadaniach”). Zrzuty: `sprawa-decyzja.png` (`-ciemny`, `-telefon`), `okno-odrzucenia.png`.

- Formularz **w całości tylko do odczytu** (`EodForm ReadOnly`, 9.3.3) – akceptujący nie zmienia danych, tylko decyduje. Pasek nad formularzem mówi, czyj to wniosek i co zrobić („Sprawdź dane i podejmij decyzję – formularz jest tylko do odczytu.”). Proces dostarcza w formularzu to, czego akceptujący potrzebuje do decyzji (np. wymiar urlopu, nieobecności w departamencie).
- Sektor przycisków: **„Akceptuj”** (Primary, `Checkmark`) · „Odrzuć” (`Prohibited`) · „Zwróć do poprawy” (`ArrowUndo`, gdy proces ma zwrot) · „Więcej” · | · „Historia”.
- Decyzje są operacjami istotnymi (sekcja 11): „Akceptuj” – okno potwierdzenia ze skutkiem i odbiorcami („Zaakceptować wniosek urlopowy?”); „Odrzuć” – okno niestandardowe `RejectDialog` z **wymaganym uzasadnieniem** (bez uzasadnienia okno zostaje otwarte, komunikat pod polem); „Zwróć do poprawy” – `ReturnReasonDialog` z wymaganym powodem. Okno z danymi jest jednocześnie potwierdzeniem.
- Po decyzji: przycisk decyzji w stanie wczytywania, pozostałe wyłączone → okno wyniku – akceptacja i zwrot: **sukces**; odrzucenie: **informacja** (zielony kolor oznacza wynik pozytywny, sekcja 4 pkt 8). Znaczki, kamienie milowe (odrzucony etap – ikona błędu), wykonawca, karta „Akceptacje” (decyzja, data, uzasadnienie), „Dokumenty i wiadomości” i przebieg procesu odświeżają się. Zalogowana osoba nie jest już wykonawcą kroku – akcje decyzji znikają. Odrzucenie kończy sprawę – staje się ona tylko do odczytu (9.3.3).
- Załączniki bez dodawania i usuwania (gdy proces nie daje akceptującemu tego uprawnienia); komentarze dostępne – akceptujący może dopytać. Uzasadnienie decyzji należy do przebiegu procesu i karty „Akceptacje”, nie do komentarzy (9.5).
- Akceptacja w samej sprawie (etap procesu) – karta „Akceptacje” bez odnośnika do sprawy zależnej; akceptacja w procesie zależnym (np. budżet zlecenia rekrutacji) – raport z procesów zależnych (9.6).

#### 9.3.3 Sprawa zakończona – tylko do odczytu

Wzorcowa sprawa: ZR/2026/0027 z wynikiem „Zatrudniono” (`/sprawy/ZR-2026-0027`, raport „Zakończone”). Zrzuty: `sprawa-zakonczona.png` (`-ciemny`, `-telefon`), `sprawa-zakonczona-zalaczniki.png`, `sprawa-zakonczona-komentarze.png`.

- `EodCaseForm ReadOnly="true"`: po znaczkach ekranu (wynik – kolor z mapy, „Zakończone {data}”) znaczek **„Tylko do odczytu”** z kłódką (`subtle`) – jeden sygnał na poziomie sprawy; pasek nad formularzem powtarza go zdaniem („Sprawa zakończona … Formularz jest tylko do odczytu.”).
- Sektor przycisków **bez akcji Primary**; zostają akcje odczytu: „Generuj dokument PDF” (Default, stan wczytywania) · | · „Historia”.
- Część B: informacje podstawowe z datą zakończenia i czasem realizacji, **bez wykonawcy kroku**; kamienie milowe – wszystkie zakończone (data · kto).
- `ReadOnly` przekazuje kaskadowo `ReadOnlyScope` częściom B i C – komponenty wzorca same chowają możliwość zmian: `EodForm` (wszystkie sekcje w spisie zakończone, w trybie „Krok po kroku” bez „Gotowe”), `EodAttachments` / `EodAttachmentCategory` (bez strefy upuszczania, „Usuń”, ostrzeżenia i stopki; pobieranie i podgląd zostają), `EodComments` (bez pola nowego komentarza i akcji). Ekran nie ukrywa tych elementów pojedynczo.
- **Formularz w całości tylko do odczytu** (sprawa zakończona, widok akceptującego – `EodForm ReadOnly` albo `ReadOnlyScope`): wszystkie sekcje jako etykieta + tekst (`EodReadOnlyField`, zwykle 4 kolumny), tabele bez kontrolek (np. kryteria: Kryterium · Waga · Wymagane ✓ / –), bez znaczków stanu sekcji, ostrzeżeń i akcji edycji sekcji. Sekcje **nie mają** tła `colorNeutralBackground2` ani znaczka „Tylko do odczytu” – to wyróżnienie odróżnia sekcję tylko do odczytu od edytowalnych, a tu edytowalnych nie ma (decyzja 27). Pole bez wartości – „–”, wartość logiczna – „Tak” / „Nie”, kwota – `12 000,00 zł`.
- O tym, czy sprawa jest tylko do odczytu, decyduje definicja procesu (stan sprawy, uprawnienia), nie ekran.

### 9.4 Załączniki (karta „Załączniki”)

- **Kategorie** w siatce auto-fit (min. 380 px): nagłówek kategorii (tło `colorNeutralBackground2`): nazwa, znaczek „Wymagany” (`danger`, gdy kategoria wymagana), licznik plików („1 plik / 2 pliki / 5 plików”).
- Lista plików: ikona typu (PDF / e-mail / obraz / dokument), nazwa-link (podgląd), podpis „rozmiar · kto · kiedy”, ikony Subtle „Pobierz” i „Usuń” (usunięcie = potwierdzenie w oknie).
- Pusta kategoria wymagana: MessageBar ostrzeżenia w kategorii („Dodaj treść ogłoszenia przed przekazaniem do akceptacji.”).
- Strefa upuszczania w każdej kategorii: „Przeciągnij plik tutaj lub [Wybierz plik]” (FluentInputFile). Po dodaniu: MessageBar sukcesu „Dodano załącznik. Kategoria: X.”
- Stopka karty: dozwolone typy i limit („PDF, JPG, PNG, DOCX, XLSX, MSG · do 20 MB na plik.”).
- Bez prawa dodawania (akceptujący, sprawa zakończona – `CanAdd="false"` albo `ReadOnlyScope`): bez strefy upuszczania, „Usuń”, ostrzeżenia o wymaganym pliku i stopki; pobieranie i podgląd zostają; pusta kategoria – zwarty stan pusty „Brak plików w tej kategorii.” (sekcja 9.14).

### 9.5 Komentarze (karta „Komentarze”) – `EodComments`

- **Pole nowego komentarza** na górze: awatar autora, pole tekstowe („Napisz komentarz. Wpisz @, aby oznaczyć osobę.”), pasek pod polem: „@ Oznacz osobę” (Subtle, small – wstawia @ i otwiera listę), zdanie stanu („Powiadomienie dostaną: **X**, **Y**.” albo „Ctrl + Enter dodaje komentarz.”), przycisk „Dodaj komentarz” (Default – Primary zostaje przy przejściu procesu; nieaktywny bez treści). Ctrl + Enter dodaje.
- **Oznaczanie:** znak @ na początku wyrazu otwiera listę osób **pod tym znakiem** (awatar, imię i nazwisko, stanowisko; szer. 280 px). Lista zawęża się podczas pisania, bez rozróżniania wielkości liter i polskich znaków, po imieniu lub nazwisku. ↑/↓ wybiera, Enter/Tab wstawia „@Imię Nazwisko ”, Esc zamyka. W treści zapisujemy **identyfikatory osób**, nie same nazwiska.
- **Lista:** najnowszy na górze. Pozycja: awatar, autor, „stanowisko · data godz. · etap: X”, treść (zachowuje akapity, maks. 110 znaków szerokości). Oznaczenie w treści = znacznik w kolorze marki (`colorBrandBackground2`); oznaczenie zalogowanej osoby mocniejsze (wypełnione marką) + znaczek „Oznaczono Cię” + akcent 3 px z lewej i tło `colorNeutralBackground2`.
- Akcje pozycji: cudzy komentarz – „Odpowiedz” (wstawia @autora); własny – „Usuń” (potwierdzenie; zostaje wiersz „Komentarz usunięty przez autora · kto · kiedy”). Bez edycji komentarzy.
- Po dodaniu: MessageBar sukcesu „Dodano komentarz. Powiadomienie dostaną: …”. Oznaczona osoba dostaje powiadomienie w aplikacji i e-mail; klik w powiadomienie otwiera sprawę **na karcie Komentarze, przewiniętą do komentarza z oznaczeniem**.
- Komentarze = rozmowa o sprawie. Uwagi do decyzji (powód zwrotu, komentarz akceptującego) należą do przebiegu procesu i karty „Akceptacje”.
- Pusta lista: zwarty stan pusty „Nikt jeszcze nie skomentował tej sprawy.” (+ zdanie o oznaczaniu osób, gdy można pisać). Tylko do odczytu (`ReadOnly` albo `ReadOnlyScope` sprawy zakończonej): bez pola nowego komentarza i bez akcji „Odpowiedz” / „Usuń” – historia rozmowy zostaje czytelna.

### 9.6 Akceptacje, dokumenty, historia, uprawnienia

- **Akceptacje:** pasek „Raport z procesów zależnych. Wiersz otwiera sprawę akceptacji.” + link do sprawy zależnej; tabela: Rodzaj akceptacji · Akceptujący (`EodPerson`) · Decyzja (znaczek) · Data · Komentarz.
- **Dokumenty i wiadomości:** tabela tego, co wygenerował proces, od najnowszych: Data · Rodzaj (znaczek z ikoną: E-mail / Powiadomienie / Dokument) · Tytuł · Odbiorca · Akcje. Dokument DOCX: tytuł jest linkiem do edytora DOCX, akcje „Otwórz w edytorze” i „Pobierz”; PDF: „Podgląd w nowej karcie” i „Pobierz” (ikony Subtle). Pasek nad tabelą: liczba pozycji + „Generuj dokument PDF” (sekcja 9.12, „Generowanie dokumentów”); ta sama akcja w menu „Więcej” formularza przełącza na tę kartę.
- **Historia** (panel boczny z prawej, duży, `IDialogService.ShowDrawerAsync`): karty **„Przebieg procesu”** (oś czasu od najnowszych: ikona zdarzenia w kolorze znaczenia, „**Kto** zrobił co”, podpis „data · etap”, opcjonalny cytat komentarza) i **„Zmiany pól”** (filtr tekstowy + tabela Data · Kto · Pole · Było (przekreślone) · Jest).
- **Uprawnienia do sprawy** (okno szerokie, 840 px): macierz interesariusz × Odczyt / Edycja / Decyzja / Załączniki, w wierszu osoby (zastępca oznaczony), ✓ zielony / – wyszarzony. Tylko podgląd.
- **Przypisz sprawę innej osobie** (okno): Nowy wykonawca (lista osób uprawnionych do etapu, wymagane), Powód (pole wielowierszowe), „Powiadom nowego wykonawcę e-mailem” (zaznaczone). Przyciski: Przypisz (Primary) / Anuluj. Wynik: MessageBar sukcesu + zmiana wykonawcy w B.

### 9.7 Raport tabelaryczny – `EodReport`

Jeden silnik dla wszystkich raportów; raport różni się **tylko definicją** (`ReportDefinition`). Zrzuty: `moje-zadania.png`, `raport-grupowanie.png`, `raport-filtr.png`.

**Układ strony:** `EodPage Fill="true"`: ścieżka „Raporty › nazwa” (tylko dla raportów z katalogu), tytuł, opis = opis raportu + kontekst procesu + „Stan danych: {data godz.}. Dostęp przez rolę: X.”, po prawej „Odśwież” (Subtle). Raport zajmuje całą przestrzeń; przewija się tylko tabela.

**Karta raportu:**
1. **Pasek narzędzi:** szukanie we wszystkich kolumnach · odstęp · „Kolumny” (menu z polami wyboru w bieżącej kolejności; kolumna kluczowa zablokowana; na dole „Przywróć układ domyślny”) · „Eksport CSV”.
2. **Pasek układu** (zawsze widoczny), trzy segmenty z żetonami:
   - **Grupowanie:** poziomy w kolejności rozdzielone „›”, każdy z „×”; „+ Dodaj poziom” (menu kolumn); „Rozwiń wszystkie” / „Zwiń wszystkie” (gdy są grupy).
   - **Sortowanie** (ikona ⓘ z objaśnieniem Shift + klik): kolumny w kolejności; klik w żeton odwraca kierunek, „×” usuwa; „brak”, gdy pusto.
   - **Filtry:** żetony „Kolumna: warunek ×” + „Wyczyść filtry”; segment ukryty, gdy brak filtrów.
3. **Tabela** (wygląd FluentDataGrid, `RowSize` Small – wiersz 36 px, odstęp komórki 12 px): nagłówek przyklejony; **pierwsza kolumna przypięta** (numer sprawy = link do formularza); **kolumna „Akcje” przypięta z prawej** (88 px); przy 1920 px układ domyślny mieści się bez przewijania w poziomie, węziej tabela przewija się w poziomie.
4. **Stopka:** „N pozycji · {etykieta sumy}: {suma}” (+ „· po filtrach, z M”).

**Nagłówek kolumny:** tytuł = przycisk sortowania (strzałka tylko przy kolumnach sortowania; przy kilku – numer poziomu) + ikona filtra otwierająca **okienko kolumny** (FluentPopover, 300 px): tytuł kolumny · Rosnąco / Malejąco (tylko kierunek inny niż bieżący) · Dodaj do sortowania / Usuń z sortowania · Grupuj według tej kolumny / Dodaj poziom grupowania / Usuń z grupowania · Kolejność (na początek / w lewo / w prawo / na koniec) · separator · filtr wg typu kolumny · „Wyczyść filtr” / „Gotowe” (Primary). Aktywny filtr = ikona filtra wypełniona w kolorze marki.

**Sortowanie:** klik w nagłówek = sortuj tylko po tej kolumnie (ponowny klik odwraca kierunek); **Shift + klik** = dodaj kolumnę jako kolejny poziom. Sortować i grupować można po każdej kolumnie i po kilku naraz.

**Filtr zależy od typu kolumny** i działa od razu (bez „Zastosuj”):

| Typ kolumny | Filtr | Porządek wartości |
|---|---|---|
| `Text` | „zawiera” (pole z lupą) | alfabetycznie (pl) |
| `List` | pola wyboru z licznikami + Zaznacz / Odznacz wszystkie | własna kolejność z definicji (etapy wg procesu, statusy wg pilności), inaczej alfabetycznie |
| `Number` | Od – Do (pole liczbowe, jednostka jako sufiks) | rosnąco |
| `Date` | Od – Do (dwa pola daty `dd.mm.rrrr`) | rosnąco |

**Grupowanie wielopoziomowe:** każdy poziom to wiersz grupy na całą szerokość: przycisk zwiń/rozwiń, „Kolumna: wartość”, caption „N pozycji · suma”; wcięcie 28 px na poziom; poziom 0 na tle `colorNeutralBackground2`, głębsze na `colorNeutralBackground1`; etykieta grupy zostaje widoczna przy przewijaniu w poziomie. Grupy układają się wg kierunku sortowania kolumny grupującej (domyślnie rosnąco); sortowanie działa wewnątrz grup.

**Kolejność kolumn** ustawia użytkownik: przeciągnięcie nagłówka albo przyciski w okienku kolumny. Kolumna kluczowa zawsze pierwsza, „Akcje” zawsze ostatnia.

**Kolumna „Akcje”:** najwyżej dwie ikony Subtle (np. „Przebieg procesu” → panel historii) + menu „Więcej” (Otwórz sprawę, Obserwuj / Przestań obserwować, Przypisz innej osobie…). **W raporcie kliknięcie wiersza nic nie robi** (tekst można zaznaczać i kopiować) – do sprawy prowadzi link w kolumnie kluczowej.

**Eksport CSV:** widoczne kolumny w kolejności z ekranu, wiersze po filtrach w kolejności z ekranu; separator `;`, przecinek dziesiętny, daty `dd.mm.rrrr`, UTF-8 z BOM, CRLF (otwiera się w Excelu). Nazwa `{plik-raportu}-RRRR-MM-DD.csv`. Wynik: MessageBar sukcesu „Wyeksportowano raport. {plik}: N wierszy, M kolumn.” Kolumna „Akcje” nie trafia do eksportu.

**Raporty z menu** (Moje zadania, Obserwowane, Zakończone) to raporty przekrojowe platformy: bez ścieżki „Raporty ›”; w kontekście procesu pokazują tylko jego sprawy, a kolumna „Proces” jest domyślnie ukryta.

**Układ użytkownika** (kolumny, filtry, grupowanie, sortowanie) żyje do zmiany kontekstu lub odświeżenia strony. Trwały zapis – poza wzorcem.

**Definicja raportu** (dane, nie kod ekranu) – `ReportDefinition`: `Key, Title, Description, Process, Kind, Roles, InMenu, FileName, Columns, Grouping, Sorting (ReportSort), Total (ReportTotal)`; `Kind` („Tabela” / „Tabela z grupowaniem”) wynika z `Grouping`. Kolumna – `ReportColumn`: `Key, Label, Type (Text|List|Number|Date), IsKey, IsLink, Hidden, ValueOrder, BadgeColors (wartość → kolor), IsAmount, IsPercent`. Wzorcowe definicje: `Eod.Demo/Data/DemoReports.cs` (Moje zadania, Obserwowane, Zakończone, Rekrutacje w toku [grupowanie: Etap, suma budżetu], Wdrożenia w toku [grupowanie: Departament, kolumna procentowa], Wnioski urlopowe departamentu [grupowanie: Pracownik, suma dni]).

### 9.8 Katalog raportów

- Jedna pozycja „Raporty” w menu. Strona: pasek filtrów (szukaj + FluentField `LabelPosition="Before"` „Proces”, ukryty w kontekście procesu) + licznik „N raportów”; tabela: Raport (link + opis caption) · Proces · Rodzaj (ikona + „Tabela” / „Tabela z grupowaniem”) · Dostęp przez rolę (znaczki `informative`); wiersz otwiera raport. Blok zasad pod tabelą.
- Użytkownik widzi tylko raporty swoich ról. Kontekst procesu zawęża listę.

### 9.9 Ustawienia użytkownika (menu użytkownika)

- Nagłówek z awatarem 56 px: „Ustawienia użytkownika” + „imię nazwisko · stanowisko · departament”.
- Siatka 7 + 5: **Dane konta** (imię, nazwisko, nazwa konta, e-mail – tylko do odczytu z kłódką + zdanie „zmienia administrator”; Język aplikacji – Select, zapis od razu; Hasło – „Zmień hasło” (okno: obecne, nowe z regułą, powtórz; walidacja pod polami) + „Ostatnia zmiana: …”) · **Wygląd** (Rozmiar interfejsu – radio pionowo: Automatycznie / 90 / 100 / 110 / 125% + zdanie o bieżącej skali; Motyw – radio poziomo: Jak w systemie / Jasny / Ciemny).
- **Moje role** (karta wypełniająca): tabela Nazwa roli · Opis roli · Proces; stopka „Role nadaje administrator…”.
- Każda zmiana zapisuje się od razu + MessageBar sukcesu („Zapisano motyw. Motyw ciemny.”).
- „Zmień hasło” musi dać się łatwo ukryć dla kont logujących się zewnętrznie.

### 9.10 Moje zastępstwa (menu użytkownika)

- Nagłówek + akcja główna „+ Dodaj zastępstwo”; opis: „Zastępca widzi Twoje zadania z wybranych procesów i może je wykonać w Twoim imieniu.”
- Karta **„Kto mnie zastępuje”** (podpis: „aktywne: N · zaplanowane: N · zakończone: N”): Zastępca (`EodPerson`) · Okres (`od – do`) · Zakres („Wszystkie procesy” / lista) · Status (Aktywne `success`, Zaplanowane `informative`, Zakończone `subtle`) · Akcje (aktywne: „Zakończ teraz”; zaplanowane: Edytuj, Usuń; zakończone: brak). Kolejność: aktywne, zaplanowane, zakończone.
- Karta **„Kogo zastępuję”** (wypełnia; bez akcji) + zasady („działa od pierwszego do ostatniego dnia włącznie…”, „czynność w zastępstwie zapisuje się z adnotacją «w zastępstwie za»”).
- Okno dodania/edycji: Zastępca (Autocomplete pojedynczy, wymagany), Od – Do (DatePicker, wymagane, „Do” ≥ „Od”), Zakres (radio: Wszystkie procesy / Wybrane procesy → Select Multiple wymagany), „Powiadom zastępcę e-mailem”. Przyciski: Zapisz zastępstwo / Anuluj. Usunięcie i „Zakończ teraz” – potwierdzenie w oknie.

### 9.11 Powiadomienia

- Panel boczny z prawej (mały), lista: ikona rodzaju w kolorze znaczenia (oznaczenie @ – marka, nowe zadanie – marka, termin – ostrzeżenie, akceptacja – sukces), tytuł-link, podpis „proces · kiedy”. Kliknięcie zamyka panel i przechodzi do celu (np. sprawa, karta Komentarze).
- Licznik na dzwonku = liczba nieprzeczytanych (`FluentCounterBadge Color="danger"`).

### 9.12 Ekrany z komponentami Syncfusion

Wspólne: strona `EodPage` (zwykle `Fill="true"`), komponent w `EodCard NoPadding Fill`, filtry nad komponentem w pasku `eod-filterbar` (kontrolki Fluent), akcja główna w nagłówku strony, komunikaty przez `UiMessages`, bez komponentów Syncfusion w oknach (sekcja 3.2). Do pierwszego renderowania interaktywnego w miejscu komponentu stoi `FluentSkeleton` (`EodSyncfusionHost`). Ekrany wzorca w demo – sekcja 8; punkty „W demo” opisują, jak wzorzec zbudowano i co różni się od opisu.

**Terminarz / grafik – `EodScheduler`**
- Nagłówek: tytuł, opis (zakres, stan danych), akcja główna („+ Dodaj rezerwację”, „+ Dodaj wpis”). Pasek filtrów: np. departament (FluentSelect), osoba (FluentAutocomplete), rodzaj wpisu.
- Widoki: Dzień · Tydzień roboczy · Tydzień · Miesiąc · Agenda. **Grafik osób** = widoki osi czasu (tydzień / miesiąc) z zasobami: wiersz = osoba, grupy = departamenty, kolumny = dni. Ekran wybiera widok domyślny i dostępne widoki.
- Tydzień od poniedziałku, godziny 24 h, godziny pracy z konfiguracji (poza nimi tło wyszarzone), siatka co 30 min (grafik godzin: co 1 h), dzisiejszy dzień wyróżniony.
- Kolor wpisu = **rodzaj** (np. urlop, praca zdalna, dyżur) z palety danych (sekcja 3.2), z legendą nad terminarzem. Status wpisu (np. „Oczekuje”) jako tekst / ikona w treści wpisu i w oknie, nie kolorem tła.
- Kliknięcie wolnego miejsca lub wpisu: szybki podgląd w szablonie wzorca (tytuł, czas, osoba, status, przyciski „Edytuj” / „Usuń”), edycja i dodawanie – **okno z `Dialogs/`** (pola Fluent, walidacja jak w sekcji 11), nie edytor Syncfusion. Usunięcie – potwierdzenie `UiMessages`.
- Przeciąganie i zmiana długości wpisu – tylko z uprawnieniem; po upuszczeniu zapis od razu + pasek sukcesu („Przeniesiono rezerwację. Sala A, 15.10.2026, 10:00–11:00.”); konflikt lub brak zgody – pasek błędu i powrót wpisu na miejsce.
- Telefon (< 900 px): widok Agenda albo Dzień, bez przeciągania.
- **W demo** (`/terminarz`): grafik trzech departamentów (oś czasu – tydzień roboczy; widoki: oś czasu – tydzień roboczy / miesiąc, Dzień, Tydzień roboczy, Agenda); wiersz zasobu = awatar + imię i nazwisko + stanowisko, grupa = departament (zwijana); pasek filtrów: Departament, Rodzaj (FluentSelect `LabelPosition="Before"`), szukaj osoby + licznik „N osób · N wpisów”; legenda sześciu rodzajów nad terminarzem. Treść wpisu: tytuł, godziny, status z ikoną zegara („Oczekuje”). Szybki podgląd (szablon wzorca, przyciski Fluent): rodzaj, tytuł, czas, osoba, sala, opis; wolny termin – „Dodaj rezerwację”. Okno `BookingDialog`: Tytuł, Rodzaj, Osoba (Autocomplete), Cały dzień, Data / Od / Do (albo Od dnia / Do dnia), Sala, Opis; walidacja w oknie. Przeciągnięcie na osobę z urlopem w tym czasie – pasek błędu i powrót wpisu. Urlopy (z procesu „Wnioski urlopowe”) – tylko do odczytu. Telefon: Agenda bez kolumn zasobów, osoba w treści wpisu.

**Biblioteka dokumentów – `EodFileManager`**
- Karta z menedżerem plików: drzewo folderów z lewej, lista (widok domyślny: szczegóły – nazwa, data zmiany, rozmiar, kto) lub kafelki, ścieżka folderu, szukanie, sortowanie.
- Operacje wg uprawnień (niedostępne są ukryte): nowy folder, wyślij plik, pobierz, zmień nazwę, przenieś, usuń (potwierdzenie `UiMessages`). Wynik operacji – pasek. Typy i limit plików jak w załącznikach (sekcja 9.4), podane w podpowiedzi.
- Dostęp do plików wyłącznie przez usługę serwera (zdarzenia komponentu albo endpoint API), z uprawnieniami platformy i audytem – nigdy bezpośrednio do dysku.
- **Załączniki sprawy zostają w `EodAttachments`** (kategorie, wymagane, strefa upuszczania). Menedżer plików jest dla repozytoriów z folderami, nie dla sprawy.
- **W demo** (`/dokumenty`): usługa `IFileLibrary` (w demo w pamięci, `DemoFileLibrary`) – komponent nie zna dysku; wysyłanie strumieniem `IBrowserFile`, pobieranie strumieniem z usługi (kilka elementów – ZIP). Widok szczegółów: Nazwa · Data zmiany (`dd.mm.rrrr gg:mm`) · Rozmiar („412 KB”, „1,2 MB”) · Zmienił. Pasek komponentu wg zaznaczenia: Nowy folder, Wyślij plik, Sortuj według, Odśwież / Wytnij, Kopiuj, Usuń, Pobierz, Zmień nazwę; menu kontekstowe pliku, folderu i tła. Okno usunięcia komponentu zastąpione `UiMessages.ConfirmAsync` („Usunąć plik?” / „Usunąć folder?” / „Usunąć elementy?”); okna nowego folderu i zmiany nazwy – komponentu (zasada 5). Stopka karty: typy, limit, „Usunięcie wymaga potwierdzenia”. Telefon (do 900 px): bez drzewa folderów (ścieżka i dwuklik), kolumny Nazwa i Rozmiar.

**Edytor treści – `EodRichTextEditor`**
- Pole formularza jak inne (sekcja 10): etykieta nad polem, gwiazdka przy wymaganym, podpowiedź / walidacja pod polem; zawsze na całą szerokość sekcji (`EodField Width="full"`), wysokość dopasowana do treści z minimum.
- Pasek narzędzi – zestaw wzorca: nagłówek (2 poziomy) · pogrubienie · kursywa · podkreślenie · lista punktowana i numerowana · wcięcia · link · tabela · cofnij / ponów · wyczyść formatowanie. **Bez** wyboru czcionki, rozmiaru i koloru tekstu (zasada tokenów). Obrazy tylko tam, gdzie treść ich wymaga (aktualność), wysyłane na serwer, nie zapisywane w treści.
- Wklejanie z Worda i stron: czyszczenie formatowania. Treść zapisywana jako HTML i **sanityzowana po stronie serwera**; wyświetlanie przez sanityzację (jak treść okien).
- Odczyt (sekcja tylko do odczytu, etap zakończony): wyrenderowana treść bez ramki edytora.
- Edytor tylko tam, gdzie formatowanie jest częścią wyniku (ogłoszenie, aktualność, szablon e-maila). Zwykły tekst – `FluentTextArea`; komentarze do sprawy – `EodComments`.
- **W demo** (`/intranet/aktualnosci/nowa`): ścieżka „Intranet › Aktualności › Nowa aktualność”, akcje „Opublikuj aktualność” (Primary) + „Zapisz szkic”; siatka 7 + 5: karta „Aktualność” (Tytuł, Kategoria, Odbiorcy, Zajawka, Treść) i karta „Podgląd” (ta sama treść w trybie tylko do odczytu). Nagłówek w liście formatów: „Akapit / Nagłówek / Podtytuł” (`h2`, `h3`; `h1` z wklejonego tekstu wyświetla się jak `h2`). Wklejanie czyści style, klasy, czcionki i kolory, zostawia nagłówki, listy, tabele i linki. Walidacja przy publikacji: komunikat pod polem (bez czerwonej ramki edytora – jak pola Fluent) i pasek błędu „Przejdź do pola”; komunikat znika po poprawieniu pola. Publikacja: potwierdzenie, sanityzacja na serwerze (`HtmlSanitizer`, sekcja 18), okno sukcesu, formularz tylko do odczytu („Napisz kolejną aktualność”). Szkic – pasek sukcesu. Obrazy: `POST /pliki/obrazy` (PNG, JPG, WebP do 2 MB), w treści adres `/pliki/obrazy/{nazwa}`. `HtmlSanitizer` (lista dozwolonych znaczników i atrybutów, adresy tylko `http(s)`, `mailto` i względne, linki z `target="_blank" rel="noopener noreferrer"`) to zabezpieczenie wzorca – platforma może użyć sprawdzonej biblioteki sanityzacji o tym samym działaniu.

**Edytor dokumentu DOCX – `EodDocumentEditor`**
- Osobna strona w trybie `Fill` (np. `/sprawy/{numer}/dokumenty/{id}`): ścieżka „proces › numer sprawy › nazwa dokumentu”, opis (wersja, kto i kiedy zmienił), akcje: **„Zapisz dokument”** (Primary) · „Pobierz DOCX” · „Pobierz PDF” · „Zamknij” (wraca do karty „Dokumenty i wiadomości”). Pasek narzędzi edytora – wbudowany.
- Niezapisane zmiany przy wyjściu: okno potwierdzenia „Zamknąć bez zapisywania?” (przyciski „Zamknij bez zapisywania” / „Wróć”). Zapis – pasek sukcesu z numerem wersji.
- Dokument zatwierdzony lub brak uprawnień: tryb tylko do odczytu, bez akcji zapisu (pobieranie zostaje).
- **W demo** (`/sprawy/ZR-2026-0041/dokumenty/ogloszenie`; `plan-zatrudnienia` – 1,1 MB, zatwierdzony): opis „plik · wersja N · zmienił(a) kto data” + „· niezapisane zmiany”; akcje wyłączone, dopóki dokument się nie otworzy. Zatwierdzony – pasek informacji „Dokument zatwierdzony – tylko do odczytu.” nad edytorem, bez paska narzędzi. Pasek narzędzi edytora bez „Nowy” i „Otwórz”. „Pobierz DOCX” / „Pobierz PDF” – z niezapisanymi zmianami bieżąca treść edytora (plik tymczasowy), PDF konwertuje usługa serwera (przycisk w stanie wczytywania). Niezapisane zmiany pytają o zamknięcie także przy wyjściu ścieżką i menu (`NavigationLock`); zamknięcie karty przeglądarki – pytanie przeglądarki. Skrypt edytora (4,9 MB) ładuje się tylko na tej stronie.

**Schemat organizacyjny – `EodOrgChart`**
- Pokazuje **departamenty, pracowników i zależności między nimi**. Dane ze struktury organizacyjnej platformy (`IStrukturaOrganizacyjna`, `platforma-procesow.md`); wzorzec ich nie definiuje.
- Miejsca: Intranet › Książka adresowa (przełącznik „Lista | Schemat” nad treścią), Administracja › Struktura organizacyjna (administrator klienta).
- **Układ:** drzewo od góry (zarząd → piony → departamenty → osoby), automatyczny układ komponentu „schemat organizacyjny”. W drzewie każda osoba i każdy departament ma jednego przełożonego / jednostkę nadrzędną (wymóg układu); pozostałe relacje to dodatkowe łączniki.
- **Węzły:** departament – nazwa, kierownik (awatar z inicjałami, imię i nazwisko), liczba pracowników; osoba – awatar, imię i nazwisko, stanowisko; kierownik wyróżniony (pogrubienie, akcent `colorBrandStroke1`). Stanowisko nieobsadzone – przerywane obramowanie i znaczek „Wakat” (`informative`). Węzeł `colorNeutralBackground1` z obramowaniem `colorNeutralStroke1`; wynik szukania / zaznaczenie – `colorBrandBackground2`.
- **Zależności (łączniki):** podległość służbowa – linia ciągła (szkielet drzewa); zastępstwo – linia przerywana (aktywne zastępstwa, sekcja 9.10); zależność funkcjonalna – linia kropkowana (np. HR Business Partner departamentu, opiekun wdrożenia, przełożony merytoryczny). Rodzaje rozróżnia **styl linii** (nie tylko kolor) i legenda nad schematem. Zastępstwa i zależności funkcjonalne domyślnie ukryte – włączane polami wyboru „Pokaż: zastępstwa · zależności funkcjonalne”, żeby schemat był czytelny.
- **Pasek nad schematem (Fluent):** szukaj osoby lub departamentu (`FluentAutocomplete` – wybór rozwija gałąź, centruje i zaznacza węzeł) · Poziom: „Departamenty” | „Departamenty i pracownicy” · Rozwiń wszystkie / Zwiń wszystkie · Powiększ · Pomniejsz · Dopasuj · Eksport PNG.
- Duża struktura: domyślnie rozwinięte dwa poziomy, gałęzie zwijane na węźle; minimapa w rogu (komponent podglądu Diagram), gdy schemat nie mieści się na ekranie.
- **Szczegóły:** kliknięcie węzła pokazuje kartę „Szczegóły” obok schematu (siatka 8 + 4). Osoba: awatar 56 px, stanowisko, departament, przełożony, aktywny zastępca, kontakt (e-mail, telefon), „Pokaż w książce adresowej”. Departament: kierownik, jednostka nadrzędna, liczba osób, lista pracowników (`EodList`).
- **Edycja struktury** (tylko Administracja, wg uprawnień): zmiany przez okna Fluent z `Dialogs/` („Przenieś do departamentu…”, „Zmień przełożonego…”, „Dodaj departament…”) z potwierdzeniem; przeciągnięcie węzła na inny jest skrótem do tego samego okna (bez zapisu od razu). Zmiana zapisuje się w historii struktury. W intranecie schemat jest tylko do odczytu.
- **Telefon (< 900 px) i dostępność:** zamiast schematu lista drzewiasta (`FluentTreeView`: departamenty → osoby) z tymi samymi szczegółami. Przełącznik „Lista | Schemat” jest też na komputerze – schemat nie jest jedynym nośnikiem danych.
- **W demo** (`/intranet/struktura`, menu Intranet › Struktura organizacyjna – Książki adresowej w demo nie ma; docelowe miejsce – decyzja 32): siatka 8 + 4, schemat w karcie NoPadding Fill, karta „Szczegóły” (osoba: awatar 56, departament, przełożony, aktywne zastępstwo, e-mail, telefon, zależności funkcjonalne, „Pokaż w książce adresowej”; departament: kierownik, jednostka nadrzędna, liczba osób, wakaty, lista pracowników – klik pokazuje osobę na schemacie). Kierownik jest w węźle swojego departamentu (wyróżniony), nie jako osobny węzeł. Węzeł z podjednostkami lub osobami ma przycisk „Rozwiń (N)” / „Zwiń”; osoby departamentu pod nim pionowo (powyżej 4 – w dwóch kolumnach). Domyślnie: poziom „Departamenty i pracownicy”, rozwinięte dwa poziomy i departament zalogowanej osoby, zaznaczona zalogowana osoba; cały schemat w widoku, gdy mieści się przy powiększeniu ≥ 75%, inaczej 100% i zaznaczony węzeł na środku. Zależność do osoby w zwiniętej gałęzi rysuje się od jej departamentu; między sąsiednimi węzłami (rodzic – dziecko) się nie rysuje. Przesuwanie – przeciągnięcie tła, powiększenie – Ctrl + kółko lub przyciski. **Nie zbudowano:** „Eksport PNG” (Diagram nie eksportuje węzłów HTML) i minimapy – decyzja 31; edycji struktury (Administracja).

**Generowanie dokumentów – Document SDK (serwer)**
- UI widzi tylko efekt: „Generuj PDF” (menu „Więcej” formularza sprawy), „Eksport Excel” raportu (obok „Eksport CSV”, gdy raport go oferuje), dokumenty w karcie „Dokumenty i wiadomości” (pobierz, otwórz w edytorze DOCX, podgląd).
- Operacja trwa: przycisk w stanie wczytywania (`Loading`), bez podwójnego kliknięcia; wynik – pasek sukcesu „Wygenerowano dokument. {nazwa pliku}.” z akcją „Pobierz”; błąd – pasek błędu z akcją „Spróbuj ponownie”. Dokument trafia też do karty „Dokumenty i wiadomości”.
- Podgląd PDF: przeglądarka w nowej karcie (komponentu PDF Viewer nie ma w wybranym zestawie – decyzja 15).
- **W demo:** „Generuj dokument PDF” w menu „Więcej” formularza sprawy przełącza na kartę „Dokumenty i wiadomości” i uruchamia generowanie tamtejszym przyciskiem (menu nie ma stanu wczytywania); pasek sukcesu „Wygenerowano dokument. {plik} – w dokumentach sprawy.” z akcją „Pobierz” (pasek z akcją zostaje do zamknięcia); błąd – „Nie wygenerowano dokumentu.” z akcją „Spróbuj ponownie” (`?demo=blad-pdf`). PDF powstaje w usłudze serwera demo (`DocumentService`: DOCX z szablonu w kodzie → DocIORenderer), pobieranie przez `/pliki/dokumenty/{id}`.

**Wykresy – `EodChart` (założenie robocze, decyzja 5)**
- Wykres w `EodCard`: tytuł mówi, co pokazuje; podpis – okres i stan danych; legenda pod wykresem; osie i podpowiedzi po polsku, liczby i kwoty wg sekcji 5; najwyżej 6 serii z palety danych.
- Wykres nie jest jedynym nośnikiem danych: obok przycisk „Pokaż dane” (tabela) albo raport, z którego pochodzi (dostępność). Kolor statusu tylko wtedy, gdy seria oznacza status.

### 9.13 Administracja: lista z oknem edycji i tabela edycyjna słownika

Wzorzec ekranów administratora klienta i administratora biznesowego (sekcja 2). Dwa ekrany demo: **Użytkownicy** (lista z oknem edycji) i **Słowniki** (tabela edycyjna). Kolejne ekrany administracji (role, struktura, parametry procesów) składa się według jednego z nich. Zrzuty: `uzytkownicy.png` (`-ciemny`, `-telefon`), `uzytkownik-okno.png`, `slowniki.png` (`-telefon`), `slowniki-edycja.png`.

**Miejsce w aplikacji i uprawnienia**
- Sekcja **„Administracja”** w menu bocznym (sekcja 6): `FluentNavSectionHeader` + pozycje ekranów (Użytkownicy `People`, Słowniki `TextBulletListSquare`); pozycja „Administracja” w menu użytkownika prowadzi do pierwszego ekranu sekcji.
- **Kto widzi sekcję i każdy ekran, ustala platforma** (role administracyjne) – wzorzec tego nie definiuje. Bez uprawnienia pozycji nie ma w menu (ukryta, nie wyłączona – sekcja 2). W demo sekcja jest zawsze widoczna.
- Ekran administracji to zwykła strona `EodPage` (bez ścieżki – otwiera się z menu), akcja główna w nagłówku, komunikaty `UiMessages`.

**A. Lista z oknem edycji (wzór: Użytkownicy, `/administracja/uzytkownicy`)**
- Nagłówek: tytuł, zdanie opisu, akcja główna „Dodaj …” (Primary, `Add`).
- Jedna karta wypełniająca wysokość (`EodCard NoPadding Fill`): pasek filtrów `eod-formbar eod-filterbar` (szukanie `FluentTextInput` z lupą, `Immediate` – po imieniu i nazwisku lub adresie e-mail; listy `FluentSelect LabelPosition="Before"` z pozycją „Wszystkie” – Departament, Status; licznik po prawej: „17 użytkowników”, po filtrach „1 użytkownik z 17”) → tabela → paginacja (gdy potrzebna) → blok zasad.
- Tabela: **`FluentDataGrid`** (sekcja 12), **nie `EodReport`** – lista administracyjna nie ma grupowania, eksportu ani układu użytkownika, a jej wiersz edytuje się w oknie; `EodReport` zostaje dla raportów (9.7). Sortowanie przyciskiem w nagłówku (`SortBy`) po kolumnach, po których się szuka (osoba, departament, ostatnie logowanie); domyślnie kolumna kluczowa rosnąco (nazwisko, imię).
- Kolumny: **Użytkownik** (`EodPerson`: awatar 28, imię i nazwisko pogrubione, podpis = adres e-mail; kolumna elastyczna) · Departament · Stanowisko · Role (znaczki `informative`, jak w katalogu raportów) · Status (znaczek Tint: Aktywny `success`, Zablokowany `danger`, Zaproszony `warning` – sekcja 14) · Ostatnie logowanie (`dd.mm.rrrr gg:mm`, `eod-num`; brak logowania – „–”) · Akcje.
- **Akcje wiersza** (sekcja 12): „Edytuj” (`Edit`, otwiera okno) + menu „Więcej” (`MoreHorizontal`) z czynnościami zależnymi od stanu: „Zablokuj konto” / „Odblokuj konto” (`LockClosed` / `LockOpen`) – **potwierdzenie w oknie** („Zablokować konto?” – skutek, imię, nazwisko i adres), wynik – pasek sukcesu; „Wyślij ponownie zaproszenie” (`Mail`, tylko konto „Zaproszony”; drobna operacja – sam pasek). Czynności niedostępne w danym stanie nie pojawiają się w menu. Kliknięcie wiersza nic nie robi.
- Paginacja: `FluentPaginator` (`eod-paginator`) dopiero przy liście > 50 pozycji (sekcja 12); zmiana filtra wraca na pierwszą stronę. Dane demo (17 kont) jej nie pokazują.
- Pusta lista po filtrach: „Brak użytkowników spełniających filtry.”. Blok zasad: co znaczą stany (zaproszenie, blokada).
- **Okno edycji** (`Dialogs/UserDialog`, `OpenAsync(dialogs, editing)`, rozmiar Medium): jedno okno do dodania i edycji (tytuł „Dodaj użytkownika” / „Edytuj użytkownika”), przyciski **„Zapisz”** (Primary) / „Anuluj”. Pola: Imię* · Nazwisko* (2 kolumny) · Adres e-mail* (format i unikalność: „Ten adres ma już konto: X. Podaj inny adres.”; przy nowym koncie podpowiedź o zaproszeniu) · Departament* (`FluentSelect`) · Stanowisko* · Przełożony (`FluentAutocomplete` pojedynczy, opcjonalny, bez edytowanej osoby) · Role* (`FluentSelect Multiple`, co najmniej jedna) · „Konto aktywne” (`FluentCheckbox` + zdanie o skutku pod spodem). Stan zapisuje się z oknem, więc pole wyboru, nie przełącznik (`FluentSwitch` = ustawienie działające od razu, sekcja 10). Walidacja po „Zapisz”: komunikaty pod polami, okno zostaje otwarte (sekcja 11). Wynik – pasek sukcesu na liście („Dodano użytkownika. … Zaproszenie wysłaliśmy na adres …”, „Zapisano zmiany użytkownika.”).

**B. Tabela edycyjna słownika (wzór: Słowniki, `/administracja/slowniki`)**
- Układ: siatka 3 + 9 (`FluentGrid` z `eod-grid-fill`; md 4 + 8). Z lewej karta **lista słowników**: `EodList` z pozycjami `EodListItem Selected` (lista wyboru: cała pozycja klikalna, wybrana – tło `colorNeutralBackground1Selected`, akcent marki 3 px z lewej, `aria-current`; podpis „N pozycji · procesy”). Z prawej karta wybranego słownika (wypełnia wysokość): tytuł = nazwa słownika, podpis „procesy · N pozycji, M aktywnych” (z kopii roboczej), akcja karty „Dodaj pozycję” (Subtle, small, `Add`), treść – `EodEditableTable`, blok zasad. Adres `?slownik={klucz}` wybiera słownik przy wejściu na stronę (link z innego ekranu).
- **Zapis zbiorczy:** zmiany w tabeli nie zapisują się od razu (inaczej niż ustawienia użytkownika, sekcja 4 pkt 9), tylko przyciskiem **„Zapisz zmiany”** (Primary, `Save`) w nagłówku strony; obok „Odrzuć zmiany” (Default). Oba nieaktywne, gdy nie ma niezapisanych zmian.
- **Tabela** (`EodEditableTable` + `EditSession`, sekcja 18): Kolejność (numer + „Przenieś wyżej” / „Przenieś niżej”, `ArrowUp` / `ArrowDown`, na krawędzi tabeli nieaktywne; kolejność wierszy = kolejność na liście wyboru w formularzu) · Kod* · Nazwa* · Opis · Aktywna (`FluentCheckbox`, wyśrodkowane) · stan wiersza i „Usuń” (`Delete`). Kontrolki w komórkach bez etykiet: etykietą jest nagłówek kolumny (gwiazdka przy kolumnie wymaganej – klasa `eod-th-required`), kontrolka ma `aria-label` z numerem wiersza. Pola tekstowe `Immediate`, wiersz 44 px, komórki wyrównane do góry – komunikat pod polem zawija się w komórce i nie przesuwa sąsiednich kontrolek.
- **Stan zmian:** pasek nad tabelą: opis słownika · „Niezapisane zmiany – nowe: 1 · zmienione: 2 · usunięte: 1 · zmieniona kolejność” albo „Brak niezapisanych zmian.” · po próbie zapisu „N pól do poprawy” (kolor błędu). Wiersz nowy / zmieniony: znaczek „Nowa” / „Zmieniona” (`brand`) przy „Usuń” + akcent 3 px w kolorze marki z lewej. Wiersz usunięty znika z tabeli (liczy go pasek stanu; przywraca „Odrzuć zmiany”).
- **Dodanie:** „Dodaj pozycję” dopisuje wiersz na końcu (stan „Nowa”, aktywny) i ustawia fokus w jego pierwszym polu.
- **Usunięcie:** wiersz nowy (niezapisany) – od razu, bez pytania (nic nie przepada poza wpisanym tekstem); wiersz zapisany – okno „Usunąć pozycję?” („… zniknie ze słownika po zapisaniu zmian”; „Usuń” / „Anuluj”). **Pozycja użyta w sprawach** (informację podaje platforma; w demo flaga danych) – nie można jej usunąć ani zmienić jej kodu: kod `ReadOnly` + `FilledDarker` + kłódka (sekcja 4 pkt 10), a „Usuń” otwiera okno „Wyłączyć pozycję zamiast usunąć?” z przyciskiem „Wyłącz pozycję”; pozycja już wyłączona – okno informacji „Nie można usunąć pozycji”. Przycisk „Usuń” zostaje widoczny, bo użytkownik powinien wiedzieć, dlaczego pozycja zostaje (sekcja 2) – okno wyjaśnia powód i proponuje dozwoloną czynność.
- **Walidacja** (jak formularz sprawy, sekcje 9.3 i 11): przy „Zapisz zmiany” – komunikaty pod polami w komórkach („Podaj kod pozycji.”, „Kod ma już pozycja „X”. Podaj inny kod.”, „Podaj nazwę pozycji.”), licznik w pasku stanu i pasek błędu strony „Nie można zapisać słownika. Tabela zawiera N pól do poprawy…” z akcją „Przejdź do pola” (fokus w pierwszym polu z błędem). Od pierwszej próby błędy odświeżają się podczas poprawiania; po poprawieniu ostatniego pasek błędu znika sam. Kod jest unikalny w słowniku bez rozróżniania wielkości liter; duplikatu nie zgłasza się przy pozycji z zablokowanym kodem (poprawia się drugą).
- **Zapis:** pasek sukcesu „Zapisano słownik „X”. 3 zmiany: nowe: 1 · …”. **Odrzucenie:** okno ostrzeżenia „Odrzucić niezapisane zmiany?” („Odrzuć zmiany” / „Wróć”) → pasek informacji „Odrzucono zmiany.”.
- **Niezapisane zmiany przy zmianie widoku** (każdy ekran z zapisem zbiorczym, także edytor DOCX – 9.12): wybór innego słownika – okno ostrzeżenia „Przejść do innego słownika bez zapisywania?” („Przejdź bez zapisywania” / „Wróć”); wyjście ze strony (menu, link, przełącznik kontekstu) – `EodUnsavedChangesGuard`: okno ostrzeżenia „Opuścić stronę bez zapisywania?” („Opuść bez zapisywania” / „Wróć”; treść podaje słownik i zmiany); kontekst procesu zmienia dopiero strona docelowa, więc po „Wróć” belka pokazuje dotychczasowy kontekst; odświeżenie lub zamknięcie karty – pytanie przeglądarki (`beforeunload`, treści nie da się zmienić).
- **Komponent biblioteki, nie kompozycja na ekranie:** stan zmian, kolejność, znaczki stanu, fokus po dodaniu i reguła usuwania są takie same w każdej tabeli edycyjnej (słowniki, parametry procesów, tabele w administracji), więc daje je `EodEditableTable` (widok) i `EditSession` (kopia robocza, zmiany, walidacja). Ekran podaje kolumny z kontrolkami, reguły walidacji i decyzje (potwierdzenia, zapis). Tabela w sekcji formularza sprawy (9.3, „Kryteria oceny”) zapisuje się z całym formularzem i zostaje kompozycją FluentDataGrid na ekranie – przejście na `EodEditableTable` wymaga trybu bez paska stanu zmian (sekcja 20).

**Telefon (390 px)**
- Karty jedna pod drugą: lista słowników nad tabelą; pasek filtrów listy użytkowników – pole szukania na całą szerokość, listy pod nim.
- **Tabela – zarówno lista, jak i tabela edycyjna – przewija się w poziomie wewnątrz karty** (`eod-table-wrap` + `min-width` tabeli; `EodEditableTable MinWidth`), strona nie przewija się w poziomie (sekcja 15). Pasek stanu tabeli edycyjnej zawija się pod opis. Okno edycji zajmuje ekran, pola w jednej kolumnie.
- Edycja pojedynczego wiersza w oknie (zamiast komórek tabeli) – dopiero, gdy pojawi się słownik z wieloma kolumnami edytowany na telefonie (sekcja 20).

### 9.14 Stany ekranów (wczytywanie, brak danych, brak dostępu)

Galeria: `/wzorzec/stany` (`stany.png`). Każdy stan to komponent wzorca – ekran go nie rysuje sam.

**Wczytywanie**
- Dane przychodzą asynchronicznie dłużej niż chwilę → **szkielet** `EodPlaceholder` w miejscu i w kształcie treści (`PlaceholderKind`: `List`, `Table`, `Form`, `Tiles`; `Rows` i `Columns` zbliżone do oczekiwanej treści) – po wczytaniu strona nie „skacze”. Szkielet ma `role="status"`, `aria-busy` i opis dla czytników ekranu; przy `prefers-reduced-motion` nie połyskuje.
- Raport: `EodReport Loading="true"` – szkielety wierszy w kolumnach układu użytkownika; pasek narzędzi i pasek układu zostają, w stopce „Wczytywanie danych…”. Wzór: „Odśwież” w raportach (przycisk i raport w stanie wczytywania, potem pasek sukcesu).
- Komponent renderowany dopiero po połączeniu (np. komponent wzorca Syncfusion, sekcja 3.2) – szkielet w jego rozmiarze.
- Mały obszar bez struktury (np. treść okienka) – `FluentSpinner` (tiny) + tekst „Wczytywanie…”. NIE WOLNO pokazywać pustej karty ani spinnera na całą stronę.
- **Przycisk w trakcie operacji** (zapis, wysłanie, decyzja, generowanie dokumentu): `Loading="true"` na czas operacji – wskaźnik postępu, przycisk wyłączony, więc nie da się kliknąć dwa razy. Obsługa kliknięcia zaczyna się od sprawdzenia „operacja trwa” i zdejmuje ten stan w `finally`; pozostałe przyciski sektora są w tym czasie `Disabled`; etykieta się nie zmienia; wynik – pasek albo okno (sekcja 11). Gdy operacja startuje po oknie (potwierdzenie, okno z danymi), po ustawieniu stanu – `StateHasChanged()` (sekcja 3.1).

**Stan pusty**
- `EodEmptyState`: ikona (wyszarzona; 32 – stan pełny, 24 – zwarty), tytuł – co tu będzie albo dlaczego pusto, zdanie – co zrobić, najwyżej dwie akcje (Primary tylko na widoku bez innej akcji głównej). Stan pusty to nie błąd: bez czerwieni i bez paska komunikatu.
- **Pełny** – karta lub strona bez treści: karta „Sprawy do wykonania” bez spraw (`start-pusto.png`), strony „Nie znaleziono” i „Brak dostępu”. **Zwarty** (`Compact="true"`) – wiersz pustej tabeli (`moje-zadania-pusto.png`), pusta kategoria załączników, pusta lista komentarzy.
- Raport i tabela: brak danych – stan z treścią raportu (`EodReport EmptyContent`, np. „Nie masz zadań do wykonania”; domyślnie „Raport nie zawiera pozycji.”); brak wyników po wyszukiwaniu i filtrach – zawsze „Brak pozycji spełniających filtry.” z akcją „Wyczyść filtry” (czyści wyszukiwanie i filtry). Zwykła tabela – `EodEmptyState Compact` w `EmptyContent` FluentDataGrid.
- Licznik z wartością 0 (menu, karty sprawy) nie jest wyświetlany (`ShowEmpty="false"`, sekcja 3.1).
- Tylko w demo: parametr `?demo=pusto` (`/`, `/zadania`) pokazuje stan bez zadań (`AppState.IsEmptyDemo`); produkt go nie ma.

**Brak dostępu (403)**
- Strona `AccessDenied` (`/brak-dostepu`, `brak-dostepu.png`) – anatomia jak „Nie znaleziono”: tytuł „Brak dostępu”, karta wypełniająca ze stanem pełnym (ikona `LockClosed` 32, „Nie masz dostępu do tej sprawy” / „… strony”, wyjaśnienie bez żargonu i kodów, bez ujawniania treści zasobu), akcje „Przejdź na stronę startową” (Primary) · „Moje zadania”. „Poproś o dostęp” nie należy do wzorca (decyzja 28).
- Strona, do której definicja procesu nie daje dostępu, renderuje `<AccessDenied />` zamiast treści (demo: sprawa ZR/2026/0007 – `/sprawy/ZR-2026-0007`); przy prerenderowaniu odpowiedź ma kod **403** (`HttpContext.Response.StatusCode`). Odpowiedź z kodem błędu bez treści spoza routera kieruje na stronę wzorca `UseStatusCodePagesWithReExecute("/blad/{0}")` (`StatusCodePage`: 403 – „Brak dostępu”, inne kody – „Nie znaleziono”); kod odpowiedzi zostaje oryginalny.

## 10. Kontrolki

Galeria: `/wzorzec/kontrolki` (`kontrolki.png`). Pod każdym przykładem w galerii stoi nazwa komponentu.

**Pole formularza:** etykieta **nad polem** (domyślnie); **przed polem** tylko w wąskich formularzach i paskach filtrów (`LabelPosition="Before"`). Pole wymagane: gwiazdka (`Required`). Dłuższe objaśnienie pola: ikona ⓘ przy etykiecie (parametr `LabelInfo="@(LabelInfo.WithText("…"))"`). Pod polem: podpowiedź (neutralna) albo komunikat walidacji (`Message` + `MessageState`: błąd – czerwony tekst i ikona ✕, ostrzeżenie – ikona ⚠, sukces – ikona ✓; kolory tekstu wg Fluent 2). Pola tekstowe, liczbowe, wielowierszowe i daty z `Message` MUSZĄ mieć `MessageCondition="@FieldConditions.WhenHasMessage"` (sekcja 3.1).

**Który komponent do czego:**

| Potrzeba | Komponent | Zasada |
|---|---|---|
| Krótki tekst | `FluentTextInput` | ikona lupy jako `StartTemplate` w polach szukania |
| Długi tekst | `FluentTextArea` | zawsze całą szerokość sekcji (`Width="100%"` + `block="true"`), `Resize="TextAreaResize.Vertical"` |
| Liczba, kwota | `FluentNumberInput` | **zawsze Min i Max**; jednostka/waluta jako prefiks lub sufiks, nigdy w etykiecie; podpowiedź „Od 0 do 50 000,00 PLN.” |
| Wybór z ≤ 7 stałych pozycji | `FluentSelect` | |
| Lista słownikowa do przeszukania | `FluentCombobox` | |
| Osoby, stanowiska, duże zbiory | `FluentAutocomplete` | pojedynczy albo wielokrotny (żetony), wyszukiwanie od 1. znaku (zbiory zewnętrzne od 3.) |
| Wiele wartości z krótkiej listy | `FluentSelect Multiple` | |
| 2–5 wykluczających się opcji | `FluentRadioGroup` | poziomo, gdy etykiety krótkie |
| Oświadczenie / zgoda | `FluentCheckbox` | |
| Ustawienie działające od razu | `FluentSwitch` | |
| Data | `FluentDatePicker` | `dd.mm.rrrr`, tydzień od poniedziałku |
| Godzina | `FluentTimePicker` | |
| Kalendarz osadzony (wybór daty) | `FluentCalendar` | do zdarzeń i grafików – `EodScheduler` |
| Kolejność pozycji | `FluentSortableList` | |
| Pliki (załączniki, pojedyncze pliki w formularzu) | `FluentInputFile` | strefa upuszczania; typy i limit w podpowiedzi |
| Repozytorium plików z folderami | `EodFileManager` (Syncfusion File Manager) | sekcja 9.12 |
| Zdarzenia, rezerwacje, grafik | `EodScheduler` (Syncfusion Scheduler) | sekcja 9.12 |
| Treść formatowana | `EodRichTextEditor` (Syncfusion Rich Text Editor) | tylko gdy formatowanie jest częścią wyniku |
| Dokument Word | `EodDocumentEditor` (Syncfusion DOCX Editor) | osobna strona |
| Struktura organizacyjna (schemat) | `EodOrgChart` (Syncfusion Diagram) | zawsze z widokiem listy; przebieg sprawy – `EodMilestones` |
| Wykres | `EodChart` (Syncfusion Chart) | założenie robocze (decyzja 5) |
| Tabela danych | `FluentDataGrid` | sekcja 12; raport z grupowaniem – `EodReport` |
| Tabela edycyjna z zapisem zbiorczym (wiersze z kontrolkami, dodawanie, usuwanie, kolejność) | `EodEditableTable` + `EditSession` | sekcja 9.13; pole „aktywna” w wierszu – `FluentCheckbox` |
| Lista wyboru obok szczegółów (master–detail, np. słowniki) | `EodList` + `EodListItem Selected` | sekcja 9.13 |
| Postęp | `FluentProgressBar` | `Value` + stan (success/warning/error); bez `Value` = nieokreślony |
| Wczytywanie | `EodPlaceholder` (szkielet z `FluentSkeleton`), `EodReport Loading`, przycisk `Loading` | mały obszar bez struktury – `FluentSpinner` (tiny) + tekst; sekcja 9.14 |
| Brak danych | `EodEmptyState` | stan pełny w karcie / na stronie, `Compact` w tabeli, kategorii, liście; sekcja 9.14 |
| Akcje | `FluentButton`, `FluentMenuButton` + `FluentMenu` | sekcja 4 pkt 1–2 |

**Przyciski:** Appearance Primary (jedna akcja główna) · Default · Outline (rzadko, podgląd) · Subtle (narzędziowe, w nagłówkach kart, w wierszach) · Transparent (na belce, „Anuluj” w paskach). Rozmiar Small w nagłówkach kart, wierszach tabel, okienkach; Medium domyślnie. **Przycisk z samą ikoną MUSI mieć opis:** `IconOnly="true"` + `Title="…"` (daje `aria-label` i dymek). Ikona w przycisku: `IconStart` (czynność) albo `IconEnd` (nawigacja „Dalej ›”).

## 11. Komunikaty i okna

Galeria: `/wzorzec/komunikaty` (`komunikaty.png`). Usługa aplikacji: `UiMessages` (sekcja 18) – opakowuje `INotificationService` (paski) i `IDialogService` (okna) i pilnuje poniższych zasad.

| Sytuacja | Przykład | Forma |
|---|---|---|
| Drobna operacja zakończona powodzeniem | Zapisano zmiany. Dodano załącznik. Zmieniono wykonawcę kroku. | **Pasek · sukces** (znika po 7 s) |
| Informacja lub ostrzeżenie bez decyzji | Termin mija jutro. Sprawę obserwują 2 osoby. | **Pasek · informacja** (7 s) / **ostrzeżenie** (zostaje) |
| Błąd walidacji formularza | Formularz zawiera 1 pole do poprawy. | **Pasek · błąd** (zostaje, akcja „Przejdź do pola”) + komunikat pod polem |
| Decyzja przed operacją istotną lub nieodwracalną | Przekazać do akceptacji? Usunąć załącznik? Anulować sprawę? | **Okno · potwierdzenie** |
| Wynik operacji istotnej | Ogłoszenie opublikowane. Nie udało się opublikować ogłoszenia. | **Okno · sukces / błąd** |
| Decyzja wymagająca danych | Zwrot do poprawy z powodem. Odrzucenie z uzasadnieniem. Przypisanie sprawy innej osobie. | **Okno · niestandardowe** (komponent w `Dialogs/`, `OpenAsync` → `ShowDialogAsync<T>`) |
| Utrata niezapisanych zmian (wyjście ze strony, zmiana widoku, odrzucenie zmian) | Opuścić stronę bez zapisywania? Przejść do innego słownika bez zapisywania? Odrzucić niezapisane zmiany? | **Okno · ostrzeżenie** (`ConfirmAsync(…, MessageBoxKind.Warning)`, przyciski „Opuść / Przejdź bez zapisywania”, „Odrzuć zmiany” / „Wróć”); wyjście ze strony – `EodUnsavedChangesGuard` (sekcja 9.13) |
| Decyzja akceptującego (9.3.2) | Akceptuj · Odrzuć · Zwróć do poprawy | Akceptuj – **okno · potwierdzenie**; Odrzuć / Zwróć – **okno · niestandardowe** z wymaganym uzasadnieniem (`RejectDialog`, `ReturnReasonDialog`); wynik – **okno · sukces**, decyzja negatywna – **okno · informacja** |
| Operacja w toku | Wysyłanie sprawy, zapis szkicu, generowanie dokumentu, odświeżanie raportu | **Przycisk · `Loading`** (pozostałe przyciski sektora wyłączone), potem pasek albo okno wyniku (9.14) |

**Pasek (FluentMessageBar):** jedno stałe miejsce na stronie (provider sekcji „strona” – `EodSections.Page` – w `EodPage` / `EodCaseForm`); najwyżej 3 paski naraz (najstarszy znika); budowa: pogrubiony tytuł + zdanie; najwyżej jedna akcja; sukces i informacja znikają po 7 s, ostrzeżenie i błąd zostają do zamknięcia lub usunięcia przyczyny (metody paska zwracają identyfikator, `CloseAsync(id)` zamyka pasek, gdy przyczyna zniknęła); błąd ma `aria-live="assertive"`. Zmiana strony czyści paski; komunikat o nowej stronie pokazuje strona docelowa (np. strona nowej sprawy dla formularza spoza wzorca, `?formularz=`).

**Okno (FluentMessageBox):** modalne – nie zamyka go kliknięcie w tło; ikona 32 px w kolorze rodzaju (potwierdzenie i informacja – marka, sukces – zielony, ostrzeżenie – żółty, błąd – czerwony); tytuł-pytanie albo tytuł-wynik; treść podaje skutek, numer sprawy, odbiorców; przyciski nazwane czynnością, **przycisk główny pierwszy**; drugi zwykle „Anuluj” (przy „Anuluj sprawę” – „Wróć”). Długa treść (lista skutków) przewija się w oknie, przyciski zostają widoczne. Każde potwierdzenie w aplikacji używa tego samego okna.

**Toast (FluentToast)** – NIE używamy dla wyników operacji (od tego jest pasek); dopuszczalny tylko dla zdarzeń w tle niezwiązanych z bieżącą stroną (np. „Eksport gotowy do pobrania”).

**Komponenty Syncfusion:** ich wyniki, błędy i potwierdzenia idą przez `UiMessages` (komponent wzorca przechwytuje zdarzenia komponentu). `SfToast`, `SfDialog` i własne okna potwierdzeń Syncfusion – NIE WOLNO, jeśli komponent pozwala je zastąpić (sekcja 3.2).

## 12. Tabele i listy

- Zwykłe tabele: **`FluentDataGrid`** `DisplayMode="DataGridDisplayMode.Table"`, `RowSize="DataGridRowSize.Medium"` (wiersz 44 px), owinięty w `<div class="eod-table-wrap">` z `Style="min-width: …px"` na tabeli (węższe okno przewija tabelę w poziomie). Raport z grupowaniem, filtrami i eksportem: **`EodReport`**.
- **Kolumna kluczowa** (numer sprawy): link w kolorze linku (`colorBrandForegroundLink`), pogrubiony, podkreślenie po najechaniu – otwiera formularz. Na listach spraw cały wiersz otwiera sprawę; w raportach tylko link.
- Liczby, kwoty i daty: `tabular-nums`; liczby i kwoty do prawej.
- Status: FluentBadge Tint (mapa kolorów – sekcja 14).
- Akcje wiersza: ikony Subtle w ostatniej kolumnie, najwyżej dwie, reszta w menu „Więcej”.
- Kolumna z tekstem i podpisem (sprawa + przedmiot): link w pierwszej linii, caption w drugiej, kolumna elastyczna (42%).
- Osoba w tabeli: `EodPerson` (awatar 24–28 px + imię i nazwisko + opcjonalnie stanowisko caption).
- Paginacja (`FluentPaginator`) tylko na listach > 50 pozycji poza raportami; raport przewija się w całości (wirtualizacja przy > 500 wierszach).
- Pusta tabela: jeden wiersz ze stanem pustym `EodEmptyState Compact="true"` (sekcje 5 i 9.14); brak wyników po wyszukiwaniu i filtrach – z akcją „Wyczyść filtry”. Tabela w trakcie wczytywania – szkielety wierszy (`EodReport Loading`, `EodPlaceholder Kind="PlaceholderKind.Table"`).
- **Lista administracyjna** (konta, role, struktura): `FluentDataGrid` z paskiem filtrów, sortowaniem w nagłówku i akcjami wiersza „Edytuj” + „Więcej”; edycja w oknie z `Dialogs/` (sekcja 9.13). Nie `EodReport` – ten jest dla raportów.
- **Tabela edycyjna** z zapisem zbiorczym: `EodEditableTable` (sekcja 9.13) – kontrolki w komórkach bez etykiet (etykietą jest nagłówek; kolumna wymagana z gwiazdką, `eod-th-required`; kontrolka z `aria-label`), komórki wyrównane do góry, komunikat walidacji pod polem w komórce; tekst, pole wyboru i przyciski w komórce – w `eod-cell-line` (na wysokości pola tekstowego).

## 13. Ikony

**Fluent System Icons** (pakiet Icons), wariant **Regular, rozmiar 20** (`Icons.Regular.Size20.*`). Filled tylko dla statusów i stanu aktywnego (aktywny filtr, ikony komunikatów); 20 także w przyciskach Small; 16 w żetonach, znaczkach i komunikatach pod polem; 24 w spisach sekcji/kamieniach i w zwartym stanie pustym; 32 w oknach MessageBox i stanie pustym. Przed użyciem sprawdź, czy ikona istnieje w danym rozmiarze (nie każda ma 32 / 48). **Słownik ikon akcji** (ta sama czynność = ta sama ikona):

| Czynność | Ikona | Czynność | Ikona |
|---|---|---|---|
| Zapisz | `Save` | Historia / przebieg | `History` |
| Przekaż dalej / wyślij | `Send` | Uprawnienia | `ShieldKeyhole` |
| Zwróć | `ArrowUndo` | Przypisz | `PersonArrowRight` |
| Dodaj / nowa sprawa | `Add` | Zastępca / zastępstwa | `PersonSwap` / `PeopleSwap` |
| Edytuj | `Edit` | Szukaj | `Search` |
| Usuń | `Delete` | Filtruj (aktywny: Filled) | `Filter` |
| Pobierz / eksport | `ArrowDownload` | Drukuj | `Print` |
| Wyślij plik | `ArrowUpload` | Dokument PDF | `DocumentPdf` |
| Załącznik | `Attach` | E-mail | `Mail` |
| Otwórz | `Open` | Kalendarz | `CalendarLtr` |
| Więcej | `MoreHorizontal` | Zamknij | `Dismiss` |
| Obserwuj / przestań | `Eye` / `EyeOff` | Oznacz osobę | `Mention` |
| Odpowiedz | `ArrowReply` | Odśwież | `ArrowSync` |
| Kolumny | `ColumnTriple` | Grupuj | `GroupList` |
| Sortuj rosnąco / malejąco | `ArrowSortUp` / `ArrowSortDown` | Przywróć układ | `ArrowReset` |
| Przenieś wyżej / niżej (kolejność wierszy) | `ArrowUp` / `ArrowDown` | Zablokuj / odblokuj konto | `LockClosed` / `LockOpen` |
| Akceptuj | `Checkmark` | Odrzuć | `Prohibited` |
| Tylko do odczytu / brak dostępu | `LockClosed` | Stan pusty (32 / 24) | ikona treści: `ClipboardTextLtr`, `Comment`, `Filter`, `DocumentSearch` |

Menu boczne: Start `Home`, Moje zadania `ClipboardTask`, Obserwowane `Eye`, Zakończone `Archive`, Raporty `DataBarVertical`, Aktualności `News`, Regulacje `Book`, Książka adresowa `ContactCard`; administracja: Użytkownicy `People`, Słowniki `TextBulletListSquare` (menu użytkownika: Administracja `Wrench`). Procesy: Zlecenie rekrutacji `PeopleSearch`, Onboarding `PersonAdd`, Wnioski urlopowe `Beach`; wszystkie procesy `Apps`. Proces dostarcza swoją ikonę w definicji modułu.

W komponentach Syncfusion: wewnątrz komponentu ikony jego motywu (Fluent 2, czcionka `e-icons`); przyciski dodawane przez wzorzec – ikony z powyższego słownika.

## 14. Kolory, typografia, odstępy

- **Motyw:** tokeny Fluent 2 `webLightTheme` / `webDarkTheme`; kolor marki – domyślny Fluent `#0f6cbd` (do decyzji, sekcja 20). Motyw ciemny MUSI działać na każdym ekranie (sprawdzane zrzutem).
- **Powierzchnie:** tło aplikacji `colorNeutralBackground3`; menu `colorNeutralBackground4`; karty `colorNeutralBackground1`; sekcje tylko do odczytu, nagłówki kategorii, bloki zasad `colorNeutralBackground2`; obramowania `colorNeutralStroke2`; cień przyklejonej sekcji głównej `shadow4`; okienka `shadow16`.
- **Typografia (rampa Fluent 2):** H1 strony `fontSizeBase600`/semibold; tytuł sprawy `fontSizeBase500`; H2 karty/sekcji `fontSizeBase400`/semibold; H3 `fontSizeBase300`/semibold; tekst `fontSizeBase300`; caption `fontSizeBase200` `colorNeutralForeground3`; liczba w kaflu `fontSizeHero800`.
- **Mapa kolorów znaczków (FluentBadge Tint)** – w kodzie `Statuses.Color` (mapa domyślna `Statuses.DefaultColors`; stan terminu – `Statuses.GetDueState`):

| Znaczenie | Wartości | Kolor |
|---|---|---|
| W toku / etap | W toku, Etap: …, Do uzupełnienia; wiersz tabeli edycyjnej: Nowa, Zmieniona | `brand` |
| Termin blisko (≤ 3 dni, `Statuses.DueSoonThresholdDays`) / oczekuje / ostrzeżenie | Termin blisko, Oczekuje, 1 ostrzeżenie; konto: Zaproszony | `warning` |
| Po terminie / odrzucono / wymagany | Po terminie, Odrzucono, Odrzucony, Wymagany; konto: Zablokowany | `danger` |
| Wynik pozytywny / aktywne | Zatrudniono, Zaakceptowano, Wdrożenie zakończone, Aktywne; konto: Aktywny | `success` |
| Neutralne zakończenie / informacja | Anulowano, Przerwano, Zaplanowane, Oczekuje na opis, Zwrócono do poprawy, Szkic (sprawa przed wysłaniem), role | `informative` |
| Wygaszone | Zakończone (zastępstwo, sprawa), Tylko do odczytu | `subtle` |

- **NIE WOLNO** używać `Appearance="Outline"` z kolorem (błąd biblioteki: tekst w kolorze tła).
- **Kolory danych** (terminarz, wykresy): paleta person Fluent 2, najwyżej 6 kolorów, zdefiniowana raz w bibliotece (sekcja 3.2). Komponenty Syncfusion dostają kolory przez mostek tokenów `eod-syncfusion.css`, nie przez wartości szesnastkowe.
- **Odstępy:** 4 / 8 / 12 / 16 / 20 / 24 px (tokeny `spacing*`); karta 16; sekcja formularza 20; strona 24/16.

## 15. Responsywność i skala

| Szerokość okna | Zachowanie |
|---|---|
| ≥ 2200 px | skala automatyczna 110% |
| 1440–2199 px (1920×1080, 1536×864) | 100%; raport mieści układ domyślny bez przewijania przy 1920 |
| 901–1439 px (1366×768, 1280×720) | 90%; tabele przewijają się w poziomie z przypiętymi kolumnami |
| ≤ 1100 px | wykonawca kroku pod informacjami podstawowymi |
| ≤ 900 px | menu jako panel (hamburger); sekcja główna sprawy nieprzyklejona; spis sekcji formularza poziomo nad treścią; raport z max. wysokością 70vh |
| 390 px (telefon) | wszystko w jednej kolumnie; brak przewijania strony w poziomie |

Punkty łamania FluentGrid liczą się od rzeczywistej szerokości okna, nie od skali. Komponenty Syncfusion MUSZĄ być sprawdzone przy każdej skali (sekcja 3.2); poniżej 900 px terminarz przechodzi na widok Agenda / Dzień, menedżer plików chowa drzewo folderów, schemat organizacyjny – na listę drzewiastą.

## 16. Dostępność

- Kontrast i fokus z tokenów Fluent; widoczny fokus (`colorStrokeFocus2`, 2 px). NIE WOLNO usuwać obrysu fokusu.
- Każda kontrolka ma etykietę (widoczną albo `aria-label`); przycisk z ikoną – `aria-label` + tooltip; ikony dekoracyjne `aria-hidden`.
- Miejsce komunikatów ma `aria-live="polite"`; pasek błędu `role="alert"`.
- Tabele mają `aria-label`; sortowana kolumna `aria-sort`; przyciski zwijania grup `aria-expanded`.
- Status nigdy samym kolorem – zawsze tekst znaczka.
- Wszystko obsługiwalne z klawiatury: menu, okienka kolumn (Esc zamyka), lista oznaczeń (↑↓ Enter Tab Esc), Ctrl + Enter w komentarzu, przyciski kroków formularza.
- `prefers-reduced-motion`: bez płynnego przewijania.

## 17. Mikrointerakcje

- Bez własnych animacji poza tym, co dają komponenty Fluent (otwieranie menu, okien, paneli).
- Hover: karty-kafle `colorNeutralBackground1Hover`; wiersze list spraw – kursor wskazujący; wiersze raportu – kursor domyślny.
- Po przejściu na nową stronę obszar treści wraca na górę; po pokazaniu paska komunikatu poza formularzem sprawy – też na górę.
- Długie operacje: przycisk z spinnerem i blokadą podwójnego kliknięcia – `Loading` przycisku Fluent (sekcja 9.14; w platformie – wzorzec `PrzyciskAkcji` z `architektura-blazor.md`). Dane wczytywane dłużej niż chwilę – szkielet `EodPlaceholder`.

## 18. Biblioteka `Eod.Platform.UI` – komponenty i usługi

Biblioteka zawiera **tylko warstwę prezentacji** (bez domeny: nie zna procesów, uprawnień ani bazy). Platforma (`Eod.Platforma`) buduje na niej komponenty domenowe (`EodLista`, `EodTakNie`, `EodKwota`… z uprawnieniami i źródłami list; nazwy wg `architektura-blazor.md`). Nazewnictwo: prefiks `Eod` + angielska nazwa; parametry, metody i wartości wyliczeń po angielsku, dokumentacja XML po polsku (sekcja 3.3). Cała biblioteka ma jedną przestrzeń nazw `Eod.Platform.UI` (foldery jej nie dzielą). ⇄ = parametr z parą `…Changed` (`@bind-…`).

| Komponent | Wzorzec | Parametry |
|---|---|---|
| `EodHead` | zasoby do `<head>` (style Fluent, reboot, `eod.css`, skrypty IMask, SortableJS, `eod.js`, skala bez „skoku”) | – |
| `EodShell` | belka + menu + treść (FluentLayout, zawiera `FluentProviders`) | `AppName` (domyślnie „MOJE EOD”), `HomeHref`, `ContextSwitcher`, `HeaderActions`, `NewCase`, `Menu`, `MenuFooter`, `ChildContent` |
| `EodContextSwitcher` | przełącznik procesu w belce | `Contexts` (`ContextOption(Code, Name, Icon)`), `Current` ⇄ |
| `EodPage` | strona + miejsce komunikatów | `Title`, `BrowserTitle` (tytuł karty przeglądarki), `Description` / `DescriptionContent`, `Breadcrumb` (`BreadcrumbItem(Text, Href?)`), `Actions`, `HeaderContent`, `Fill`, `Class`, `ChildContent` |
| `EodCard` | karta | `Title`, `Subtitle`, `Actions`, `HeaderContent`, `Footer`, `Rules`, `NoPadding`, `Fill`, `Class`, `Style`, `AdditionalAttributes` (pozostałe atrybuty HTML), `ChildContent` |
| `EodRule` | wiersz bloku zasad (w `Rules` karty) | `ChildContent` |
| `EodTiles`, `EodTile` | kafle z liczbami | `ChildContent` · kafel: `Value`, `Label`, `Alert`, `Href` |
| `EodList`, `EodListItem` | lista w karcie; lista wyboru (master–detail, 9.13) | lista: `Description` (aria-label), `ChildContent`; pozycja: `Icon`, `IconClass`, `Title`, `Href` / `OnClick`, `Subtitle`, `Badge`, `Selected` (`null` – zwykła lista; `true` / `false` – lista wyboru: cała pozycja klikalna, wybrana z tłem zaznaczenia i `aria-current`) |
| `EodPerson` | awatar + osoba | `Name`, `Subtitle`, `Size`, `Color`, `Highlight` |
| `EodDueDate` | data + znaczek terminu | `Date`, `Today` |
| `EodFacts`, `EodFact` | informacje podstawowe | `ChildContent` · fakt: `Label`, `Value` / `ChildContent`, `Numeric` |
| `EodReadOnlyField` | pole w sekcji tylko do odczytu | `Label`, `Value`, `Numeric` |
| `EodIcon`, `EodBreadcrumb`, `EodActionSeparator` | ikona z klasą `ic`, ścieżka, separator w sektorze przycisków | `Value`, `Class`, `Title` · `Items` (`BreadcrumbItem`) · – |
| `EodCaseForm` | szkielet formularza sprawy (A + B + C) | `Breadcrumb`, `Title`, `Badges`, `Actions`, `Facts`, `Assignee`, `Milestones`, `ChildContent` (karty), `ReadOnly` (znaczek „Tylko do odczytu” + kaskadowy `ReadOnlyScope` dla B i C – 9.3.3) |
| `EodAssignee` | wykonawca kroku | `Name`, `Role`, `Menu`, `OnPermissions` |
| `EodMilestones` | kamienie milowe | `Steps` (`ProcessStep(Title, Subtitle, State, Issue)`; `StepState`: `Upcoming` / `Current` / `Completed`; `StepIssue`: `None` / `Warning` / `Error`) |
| `EodForm` | spis sekcji + tryb „Wszystkie / Krok po kroku” | `Sections` (`ProcessStep`), `Description`, `Mode` ⇄ (`FormMode.AllSections` – domyślnie / `FormMode.StepByStep`), `Current` ⇄, `OnDone`, `ReadOnly` (formularz w całości tylko do odczytu – 9.3.3), `ChildContent`; metoda `ShowSectionAsync(index)`, właściwość `IsReadOnly` (parametr albo `ReadOnlyScope`) |
| `EodFormSection` | sekcja formularza | `Number`, `Title`, `Description`, `Badge`, `Actions`, `Columns` (1–4, domyślnie 2; 0 = bez siatki), `ReadOnly`, `ChildContent`; w formularzu tylko do odczytu – bez tła i znaczka sekcji |
| `EodField` | komórka siatki pól | `Width` (`"2"`, `"3"`, `"full"` – domyślnie), `AlignBottom`, `ChildContent` |
| `EodAttachments`, `EodAttachmentCategory` | załączniki w kategoriach | `Footer`, `ChildContent`; kategoria: `Title`, `Required`, `Files` (`AttachmentFile(Name, Size, Author, AddedAt)`), `Warning`, `CanAdd`, `CanRemove`, `Accept`, `MaxFileSize`, `OnAdd`, `OnRemove`, `OnDownload`, `OnPreview`; w zasięgu `ReadOnlyScope` – bez dodawania, usuwania i stopki; pusta kategoria bez dodawania – zwarty stan pusty |
| `EodComments` | komentarze z oznaczaniem | `Comments` (`Comment`), `People` (`MentionablePerson(Name, JobTitle?)`), `CurrentUser`, `OnAdd` (`NewComment(Text, Mentions)`), `OnRemove`, `Footer` (etap komentarza dopisuje ekran), `ReadOnly` (także z `ReadOnlyScope`: bez pola nowego komentarza i akcji) |
| `EodTimeline` | oś czasu przebiegu | `Entries` (`TimelineEntry(Icon, Kind, Who, Description, Subtitle, Quote?)`, `TimelineEntryKind`) |
| `EodReport` | raport tabelaryczny | `Definition`, `Rows`, `LayoutKey`, `ExtraHiddenColumns`, `RowHref`, `RowActions`, `DataAsOf`, `Loading` (szkielety wierszy), `EmptyContent` (stan pusty bez wierszy; po filtrach – „Brak pozycji spełniających filtry.” z „Wyczyść filtry”) |
| `EodEmptyState` | stan pusty – karta, strona, wiersz tabeli, kategoria, lista (9.14) | `Icon`, `IconClass` (domyślnie `c-muted`), `Title`, `Description`, `ChildContent` (opis z formatowaniem), `Actions`, `Compact` |
| `EodPlaceholder` | szkielet wczytywania z `FluentSkeleton` (9.14) | `Kind` (`PlaceholderKind`: `List` – domyślnie / `Table` / `Form` / `Tiles`), `Rows` (3), `Columns` (4), `Label` (opis dla czytników ekranu, „Wczytywanie…”) |
| `EodPermissionMatrix` | podgląd uprawnień | `Rows` (`PermissionRow`, `AuthorizedPerson`) |
| `EodEditableTable<TGridItem>` | tabela edycyjna z zapisem zbiorczym (9.13): pasek stanu zmian, kolumna „Kolejność”, znaczek stanu wiersza, „Usuń”, fokus w nowym wierszu | `Session` (`EditSession<TGridItem>`), `ChildContent` (kolumny `TemplateColumn` z kontrolkami), `AriaLabel`, `Description`, `CanReorder`, `ItemLabel`, `OnRemove` (usunięcie wiersza zapisanego – potwierdza ekran), `OnChanged`, `EmptyContent`, `MinWidth` (domyślnie 880px), `Id`; metoda `AddAsync(item)`. `TGridItem` podawany jawnie (klasa) |
| `EodUnsavedChangesGuard` | ochrona niezapisanych zmian przy wyjściu ze strony (NavigationLock + okno `UiMessages`, `beforeunload`) | `IsDirty`, `Title`, `Message` (HTML), `LeaveButton` („Opuść bez zapisywania”), `StayButton` („Wróć”) |
| `EodStepIcon`, `EodMessageBar` | części wewnętrzne – ikona kroku w `EodMilestones` / `EodForm`, pasek pokazywany przez `UiMessages`; ekrany nie używają ich bezpośrednio | `Status`, `Number`, `Issue` · `BarAction` (`MessageBarAction`), kaskadowy `MessageBar` |

| Usługa / klasa | Rola |
|---|---|
| `UiMessages` | paski `ShowSuccessAsync` / `ShowInfoAsync` / `ShowWarningAsync` / `ShowErrorAsync(title, message?, action?)` (`MessageBarAction(Label, Execute)`; pasek sukcesu i informacji z akcją – np. „Pobierz” – zostaje do zamknięcia) → identyfikator paska; `CloseAsync(id)`, `ClearAsync()`; okna `ConfirmAsync(title, messageHtml, primaryButton, secondaryButton = "Anuluj", kind = MessageBoxKind.Confirmation)` → bool, `ShowMessageBoxAsync(kind, title, messageHtml, button = "Zamknij")`; `MessageBoxKind`: `Confirmation` / `Success` / `Information` / `Warning` / `Error` |
| `AppearanceSettings` | skala (`UiScale`: `Auto` / `Percent90` / `Percent100` / `Percent110` / `Percent125`) i motyw (`UiTheme`: `System` / `Light` / `Dark`); `GetScaleAsync` / `SetScaleAsync`, `GetThemeAsync` / `SetThemeAsync`, `DescribeScaleAsync` (tylko po pierwszym renderowaniu – JS) |
| `ReportLayoutStore` | układ raportów użytkownika (do zmiany kontekstu): `Get(key, definition, extraHidden?)` → `ReportLayout`, `Clear()` |
| `Formats` / `PolishPlural` / `Statuses` | daty, kwoty, terminy względne (`Date`, `DateAndTime`, `Amount`, `Number`, `Percent`, `RelativeDueDate`, `DayAndDate`); odmiana liczebników (`PolishPlural.Form`, `PolishPlural.WithCount`); kolory statusów (`Statuses.Color`, `Statuses.DefaultColors`) i stan terminu (`Statuses.GetDueState` → `DueState`: `OnTrack` / `DueSoon` / `Overdue`, `Statuses.Label`, próg `Statuses.DueSoonThresholdDays` = 3) – sekcje 5 i 14; `Formats.Culture` – kultura interfejsu |
| `FieldConditions` | `WhenHasMessage` – warunek komunikatu pod polem (sekcja 3.1) |
| `EditSession<TItem>` | sesja edycji listy dla `EodEditableTable` (9.13): kopia robocza (`Items`) i wersja zapisana (`Saved`); `Add`, `Remove`, `Move` / `CanMove`, `Discard`, `Accept` → nowa wersja zapisana; stan: `StateOf` (`RowEditState`: `Unchanged` / `Added` / `Modified`), `IsDirty`, `AddedCount`, `ModifiedCount`, `RemovedCount`, `IsReordered`, `ChangeCount`, `DescribeChanges()` („nowe: 1 · zmienione: 2”); walidacja: konstruktor `(saved, copy, sameValues, validate)` z regułą wiersza → `CellError(Field, Message)`, `Validate()`, `Revalidate()`, `WasValidated`, `ErrorFor(item, field)`, `ErrorCount`, `FirstError`; `KeyOf` – klucz wiersza (`@key`, identyfikatory kontrolek) |
| `ReadOnlyScope` | kaskadowy stan „tylko do odczytu” (`IsReadOnly`; `ReadOnlyScope.ReadOnly`, `ReadOnlyScope.Editable`, `ReadOnlyScope.From(bool)`) – ustawiają go `EodCaseForm.ReadOnly` i `EodForm.ReadOnly`; czytają `EodForm`, `EodFormSection`, `EodAttachments`, `EodAttachmentCategory`, `EodComments` i treść procesu (`[CascadingParameter] ReadOnlyScope?`) – sekcja 9.3.3 |
| `HtmlSanitizer` | `Sanitize(html, allowImages = false)` – treść formatowana przed zapisem i przed wyświetleniem: lista dozwolonych znaczników i atrybutów, `h1` → `h2`, adresy `http(s)` / `mailto` / względne, linki w nowej karcie z `rel="noopener noreferrer"`; `ToPlainText(html)` – tekst bez znaczników (walidacja „pusta treść”) – sekcja 9.12 |
| `ServiceCollectionExtensions` | `AddEodPlatformUI()`, `UseEodPolishCulture()` – rejestracja poniżej |

Rejestracja (całość):

```csharp
builder.Services.AddEodPlatformUI();   // Fluent UI Blazor v5 z polskimi tekstami + usługi wzorca
app.UseEodPolishCulture();              // przed MapRazorComponents
```

W `App.razor`: `<EodHead />` w `<head>` (przed arkuszem aplikacji), `Routes @rendermode="InteractiveServer"`; układ aplikacji: `<EodShell>` (wzór: `Eod.Demo/Layout/MainLayout.razor`).

### 18.1 Biblioteka `Eod.Platform.UI.Syncfusion`

Osobny projekt Razor (`src/Eod.Platform.UI.Syncfusion`, przestrzeń nazw `Eod.Platform.UI.Syncfusion`; referencje: `Eod.Platform.UI`, pakiety `Syncfusion.Blazor.*` 35.1.40). Zasady – sekcja 3.2, wzorce ekranów – sekcja 9.12. Komponent Syncfusion renderuje się po pierwszym renderowaniu interaktywnym, wcześniej `FluentSkeleton` (część wewnętrzna `EodSyncfusionHost`: `Height`, `Label`, `Ready`). ⇄ = parametr z parą `…Changed`.

| Element | Rola | Parametry i metody |
|---|---|---|
| `AddEodSyncfusion(IConfiguration)` | rejestracja: telemetria wyłączona (`Syncfusion:Telemetry`, domyślnie `false`), klucz licencji z `Syncfusion:LicenseKey` (brak – informacja / ostrzeżenie w logu przy starcie), `AddSyncfusionBlazor()`, polski `ISyncfusionStringLocalizer` (`SyncfusionPolishLocalizer`) | stałe `LicenseKeyConfigurationKey`, `TelemetryConfigurationKey` |
| `EodSyncfusionHead` | do `<head>` po `EodHead`: motyw `fluent2.css`, `eod-syncfusion.css` (mostek tokenów), skrypty `syncfusion-blazor.min.js` i `eod-syncfusion.js` (motyw ciemny `.e-dark-mode`, poprawki skali, `EodSyncfusion.ensureScript` / `isNarrow` / `downloadUrl`) | – |
| `EodScheduler<TItem>` (`TItem : SchedulerItem`) | terminarz / grafik (Scheduler) | `Items`, `Resources` (`SchedulerResource(Id, Name, GroupId?, Subtitle?)`), `ResourceGroups` (`SchedulerResourceGroup(Id, Name)`), `ItemKinds` (`SchedulerItemKind(Key, Name, DataColor)`), `Views` / `DefaultView` (`SchedulerView`), `SelectedDate` ⇄, `WorkHours` (`SchedulerWorkHours`), `VisibleFrom`, `VisibleTo`, `SlotMinutes`, `TimelineHours`, `AllowDragAndDrop`, `ReadOnly`, `ShowLegend`, `AddLabel`, `ResourceLabel`, `GroupLabel`, `OnAdd` (`SchedulerSlot`), `OnEdit`, `OnRemove`, `OnMove` (`Func<SchedulerChange<TItem>, Task<bool>>` – `false` cofa wpis); `RefreshAsync()`. Wpis: `SchedulerItem` (`Id`, `Title`, `Start`, `End`, `IsAllDay`, `Kind`, `ResourceId`, `Location`, `Description`, `Status`, `IsReadOnly`); kolory danych: `DataColor` / `DataColors.CssClass` |
| `EodFileManager` | biblioteka dokumentów (File Manager) | `Service` (`IFileLibrary`: `ReadAsync`, `SearchAsync`, `CreateFolderAsync`, `RenameAsync`, `MoveAsync`, `DeleteAsync`, `UploadAsync`, `DownloadAsync`; `FileLibraryItem`, `FileLibraryListing`, `FileLibraryDownload`, błąd dla użytkownika – `FileLibraryException`), `RootName`, `AllowedOperations` (`FileLibraryOperations`, flagi), `Accept`, `MaxFileSize`; `RefreshAsync()` |
| `EodRichTextEditor` | treść formatowana (Rich Text Editor) | `Value` ⇄, `Label`, `Required`, `Message`, `MessageState`, `Placeholder`, `ReadOnly`, `EmptyText`, `AllowImages`, `ImageSaveUrl`, `ImagePath`, `MinHeight`, `MaxLength`; `FocusAsync()` |
| `EodDocumentEditor` | dokument DOCX (DOCX Editor) | `Document` (DOCX, `byte[]`), `DocumentName`, `ReadOnly`, `ShowPropertiesPane`, `CurrentUser`, `IsDirtyChanged`, `IsReadyChanged`; `IsDirty`, `IsReady`, `GetDocxAsync()` (bieżąca treść), `MarkSavedAsync()` – zapis wersji robi strona (zamiast projektowanego `OnSave`) |
| `EodOrgChart` | schemat organizacyjny (Diagram, układ `OrganizationalChart`) i lista drzewiasta | `Units` (`OrgUnit(Id, Name, ParentId, ManagerId, Kind)`), `People` (`OrgPerson(Id, Name, JobTitle, UnitId, Email, Phone, IsVacancy)`), `Relations` (`OrgRelation(FromId, ToId, Kind, Label)`, `OrgRelationKind`), `Level` ⇄ (`OrgChartLevel`), `View` ⇄ (`OrgChartView`: `List` / `Chart`), `ShowSubstitutions`, `ShowFunctional`, `SelectedId` ⇄, `OnSelect` (`OrgSelection`), `ExpandedLevels`, `Expanded`; `RevealAsync(id)`, `ExpandAllAsync()`, `CollapseAllAsync()`, `ZoomInAsync()`, `ZoomOutAsync()`, `FitAsync()`. Edycja struktury (Administracja) – do zbudowania |
| `EodChart` | wykres (Chart) – założenie robocze, do zbudowania | `Kind`, `Series`, `Title`, `Subtitle`, `XAxis`, `YAxis`, `Format` (projekt) |
| `EodGantt<TItem>` | harmonogram z zależnościami (Gantt) – założenie robocze, do zbudowania | `Tasks`, `Dependencies`, `ReadOnly` (projekt) |

Usługi generowania dokumentów (Document SDK) należą do platformy (`Eod.Platforma`, serwer), nie do bibliotek UI; w demo – `Eod.Demo/Services/DocumentService.cs` (DocIO, DocIORenderer, czcionki strumieniem – sekcja 3.2).

## 19. Lista kontrolna „ekran gotowy” (Definition of Done UI)

- [ ] Ekran złożony z `EodPage`/`EodCaseForm` + `EodCard` + komponentów Fluent; brak własnych kolorów, czcionek i odstępów spoza tokenów.
- [ ] Jedna akcja Primary; etykiety przycisków to czasowniki; ikony ze słownika (sekcja 13).
- [ ] Komunikaty przez `UiMessages`: pasek dla drobnych operacji, okno dla istotnych i każdego potwierdzenia.
- [ ] Ostatnia karta wypełnia wysokość (poza formularzem i galeriami).
- [ ] Teksty po polsku, „departament”, poprawna odmiana, formaty dat i kwot z sekcji 5.
- [ ] Statusy jako znaczki Tint wg mapy kolorów (`Statuses.Color`).
- [ ] Pola: etykieta nad polem, wymagane z gwiazdką, podpowiedź / walidacja pod polem (`MessageCondition="@FieldConditions.WhenHasMessage"`), liczby z Min i Max, jednostka jako sufiks.
- [ ] Okna w `Dialogs/` z `OpenAsync`; walidacja w oknie nie zamyka okna.
- [ ] Zapis zbiorczy (tabela edycyjna, edytor): widoczny stan niezapisanych zmian, „Zapisz zmiany” / „Odrzuć zmiany” aktywne tylko przy zmianach, ostrzeżenie przed utratą zmian przy wyjściu ze strony (`EodUnsavedChangesGuard`) i przy zmianie widoku w obrębie strony.
- [ ] Stany ekranu obsłużone (sekcja 9.14): wczytywanie (szkielet `EodPlaceholder` / `EodReport Loading`, przycisk `Loading` bez podwójnego kliknięcia), brak danych (`EodEmptyState`; po filtrach – „Wyczyść filtry”), brak dostępu (`AccessDenied`, kod 403); sprawa tylko do odczytu – bez akcji zmian (`ReadOnly`).
- [ ] Działa w motywie jasnym i ciemnym, przy 1920, 1366 (skala 90%) i 390 px bez przewijania strony w poziomie.
- [ ] Klawiatura, fokus, `aria-label` przy ikonach, `aria-live` dla komunikatów.
- [ ] Komponenty Syncfusion tylko w obszarach z sekcji 3.2, przez komponenty wzorca, nie w oknach; sprawdzone w motywie jasnym i ciemnym oraz przy skali 90 / 110 / 125%.
- [ ] Ekran z komponentem Syncfusion: `node tools/syncfusion-checks.mjs` bez błędów (nowy komponent wzorca – nowe kontrole w skrypcie); brak ruchu SignalR w spoczynku (zasada 9 sekcji 3.2).
- [ ] Nazwy w kodzie po angielsku, teksty, adresy i dane po polsku; dokumentacja XML po polsku przy każdym publicznym członku i parametrze komponentu (sekcja 3.3).
- [ ] `dotnet build` bez ostrzeżeń (także CS1591 – brak dokumentacji XML); `node tools/screenshots.mjs` bez błędów (HTTP, konsola, połączenie Blazor) dla nowej trasy (`--only=<fragment adresu lub nazwy>`).
- [ ] Zrzuty ekranu dodane do `docs/ekrany` (jasny 1920, ciemny 1366, telefon 390), gdy ekran jest nowym wzorcem.

## 20. Decyzje otwarte i przyjęte założenia robocze

Do czasu decyzji właściciela obowiązuje założenie robocze (kolumna 3). Pytań o uprawnienia i reguły biznesowe nie rozstrzygamy w UI – rozstrzyga je definicja procesu.

| # | Kwestia | Założenie robocze |
|---|---|---|
| 1 | Tryb domyślny formularza: wszystkie sekcje czy krok po kroku | „Wszystkie sekcje”, przełącznik dostępny |
| 2 | Miejsce „Nowa sprawa” | przycisk na górze menu bocznego + akcja główna strony procesu |
| 3 | Kolor marki i logo | domyślny Fluent `#0f6cbd`, napis „MOJE EOD” |
| 4 | Przyklejona sekcja główna sprawy | tak (≥ 900 px) |
| 5 | Biblioteki wykresów i diagramów | diagram – Syncfusion Diagram do schematu organizacyjnego (decyzja 2026-10-18); wykresy – Syncfusion Chart dla wszystkich typów (jedna biblioteka, motyw Fluent 2) zamiast Fluent Charts – do potwierdzenia |
| 6 | Zastępstwa: zadania współdzielone czy przechodzą na zastępcę; akceptacja przełożonego | współdzielone (oboje widzą i mogą wykonać); bez akceptacji |
| 7 | Raporty: zmiana kolejności poziomów grupowania przeciąganiem; standardowe akcje wiersza | usuń i dodaj poziom; akcje: Przebieg procesu + Więcej (Otwórz, Obserwuj, Przypisz) |
| 8 | Progi skali automatycznej | 90% < 1440 px, 110% ≥ 2200 px |
| 9 | Formularz przy bardzo szerokim oknie | pola rosną z oknem (siatki 1–4 kolumn) |
| 10 | Komentarze: kogo można oznaczyć; edycja; usuwanie | osoby z dostępem do sprawy; bez edycji; autor usuwa, zostaje ślad |
| 11 | Skrypty zewnętrzne komponentów Fluent (IMask, SortableJS) | kopie lokalne w bibliotece (działanie bez internetu); aktualizacja razem z wersją Fluent |
| 12 | Komponenty Syncfusion (decyzja właściciela 2026-10-18) | Scheduler, File Manager, Diagram, Rich Text Editor, DOCX Editor, Document SDK; przez komponenty wzorca w `Eod.Platform.UI.Syncfusion`; motyw Fluent 2 |
| 13 | Grafiki (wcześniej odłożone, plan własnego komponentu) | **Rozstrzygnięte 2026-10-18 (zbudowane):** `EodScheduler` na Syncfusion Scheduler – grafik osób = oś czasu (tydzień roboczy / miesiąc) z zasobami pogrupowanymi w departamenty (`/terminarz`); własny komponent grafiku – nie budujemy |
| 14 | Harmonogram z zależnościami (Gantt) | Syncfusion Gantt, gdy proces go wymaga – do potwierdzenia |
| 15 | Podgląd PDF w aplikacji | przeglądarka w nowej karcie; komponent PDF Viewer Syncfusion poza wybranym zestawem – do decyzji |
| 16 | `SfGrid` mimo licencji | nie używamy – tabele: `FluentDataGrid` i `EodReport` (jeden wygląd tabel); ewentualny eksport Excel raportu przez Document SDK |
| 17 | Motyw ciemny Syncfusion | **Rozstrzygnięte 2026-10-18 (test w demo):** klasa `.e-dark-mode` na `<body>` ustawiana razem z `data-theme` Fluent (`eod-syncfusion.js`); wyskakujące elementy ciemne (sekcja 3.2); bez `fluent2-dark.css` |
| 18 | Przebieg procesu na diagramie | nie – przebieg sprawy pokazują kamienie milowe i oś czasu historii (decyzja 2026-10-18). Pełna mapa procesu dla administratora (rozgałęzienia, warunki, zwroty) – w razie potrzeby tym samym komponentem Diagram, po decyzji |
| 19 | Próg „termin blisko” – prototyp HTML pokazuje znaczek terminu sprawy „Termin: 12.10.2026 (4 dni)” w kolorze `warning`, specyfikacja (sekcje 9.3 i 14) – `warning` przy ≤ 3 dniach | **Rozstrzygnięte – decyzja właściciela 2026-10-18:** zostaje ≤ 3 dni (`Statuses.DueSoonThresholdDays = 3`, `Statuses.GetDueState`); obowiązuje specyfikacja, nie prototyp. Termin za 4 dni i więcej – znaczek `brand` |
| 20 | Wejście do administracji – sekcja 6 przewiduje pozycję „Administracja” w menu użytkownika, wzorzec administracyjny (9.13) dodaje sekcję „Administracja” w menu bocznym | obie: sekcja w menu bocznym (lista ekranów administracji, widoczna wg ról z platformy) + pozycja w menu użytkownika prowadząca do pierwszego ekranu sekcji – do potwierdzenia |
| 21 | Lista administracyjna: `FluentDataGrid` czy `EodReport` | `FluentDataGrid` z paskiem filtrów, sortowaniem w nagłówku i oknem edycji; `EodReport` tylko dla raportów (grupowanie, filtry kolumn, eksport) |
| 22 | Pozycja słownika użyta w sprawach: przycisk „Usuń” ukryty, wyłączony z podpowiedzią czy aktywny z wyjaśnieniem | aktywny – okno wyjaśnia, że pozycji nie można usunąć, i proponuje „Wyłącz pozycję” (sekcja 9.13); kod takiej pozycji tylko do odczytu. Które pozycje są użyte – podaje platforma |
| 23 | Tabela edycyjna na telefonie | przewijanie tabeli w poziomie wewnątrz karty; okno edycji pojedynczego wiersza – gdy pojawi się słownik z wieloma kolumnami edytowany na telefonie |
| 24 | Tabela edycyjna w sekcji formularza sprawy (9.3) na `EodEditableTable` | na razie kompozycja FluentDataGrid na ekranie (zapis z całym formularzem); przejście wymaga trybu komponentu bez paska stanu zmian – do decyzji |
| 25 | Sprawa do decyzji (widok akceptującego) we wzorcu | wniosek urlopowy WU/2026/0871 – akceptacja przełożonego jest etapem samej sprawy, a zalogowana osoba jest przełożoną (zgodnie z „Moimi zadaniami”). Nie zlecenie rekrutacji na etapie „Akceptacja budżetu”: akceptacja budżetu to proces zależny (9.6), a w danych przykładowych akceptują ją inne osoby – do potwierdzenia |
| 26 | Tytuł sprawy procesu o nazwie w liczbie mnogiej („Wnioski urlopowe”) | „{nazwa formularza startowego} {numer}” („Wniosek urlopowy WU/2026/0871”) zamiast „{nazwa procesu} {numer}” z sekcji 9.3 – do decyzji |
| 27 | Formularz w całości tylko do odczytu | jeden sygnał na poziomie sprawy (znaczek „Tylko do odczytu” z kłódką) i zdanie w pasku formularza; sekcje bez tła `colorNeutralBackground2` i bez znaczka sekcji (9.3.3) |
| 28 | „Poproś o dostęp” na stronie „Brak dostępu” | nie ma – wniosek o uprawnienia jest procesem platformy, nie przyciskiem ekranu; strona wskazuje osobę prowadzącą sprawę i administratora |
| 29 | Kolor znaczka „Szkic” (sprawa przed wysłaniem) | `informative` – sprawa jest poza obiegiem, to informacja, nie stan „w toku” |
| 30 | Okno wyniku decyzji negatywnej (odrzucenie) | okno informacji, nie sukcesu – zielony oznacza wynik pozytywny (sekcja 4 pkt 8) |
| 31 | „Eksport PNG” i minimapa schematu organizacyjnego (sekcja 9.12) – węzły HTML (szablony w stylu Fluent) nie trafiają do eksportu obrazu Diagram ani do podglądu | do decyzji: węzły rysowane kształtami SVG (eksportowalne, uboższy wygląd), eksport po stronie serwera (np. PDF struktury z Document SDK) albo bez eksportu; w demo – bez eksportu i minimapy |
| 32 | Miejsce schematu w intranecie: widok „Schemat” Książki adresowej (sekcja 9.12) czy osobna pozycja „Struktura organizacyjna” | demo: osobna strona `/intranet/struktura` z przełącznikiem „Lista \| Schemat” (Książki adresowej w demo nie ma) – do potwierdzenia |

## 21. Słownik pojęć

| Pojęcie | Znaczenie w UI |
|---|---|
| Proces | rodzaj sprawy (Zlecenie rekrutacji, Onboarding pracownika, Wnioski urlopowe) |
| Kontekst procesu | proces wybrany w belce; zawęża menu, listy i raporty |
| Sprawa | jedna instancja procesu, numer `KOD/RRRR/NNNN` (np. ZR/2026/0041; w adresach `ZR-2026-0041`) |
| Etap | krok procesu, na którym jest sprawa |
| Kamień milowy | etap pokazany na osi u góry formularza |
| Wykonawca kroku | osoba, która ma wykonać bieżący etap |
| Moje zadania | sprawy, w których jestem wykonawcą kroku |
| Obserwowane | sprawy, które obserwuję, choć nie wykonuję w nich kroku |
| Zakończone | zamknięte sprawy z moim udziałem |
| Formularz startowy | formularz zakładający nową sprawę („Nowa sprawa”) |
| Szkic | nowa sprawa zapisana przed wysłaniem – bez numeru, poza obiegiem |
| Akceptujący | osoba, która na etapie akceptacji podejmuje decyzję: akceptuje, odrzuca albo zwraca sprawę do poprawy |
| Stan pusty | widok bez danych: co tu będzie i co zrobić (nie błąd) |
| Przejście | przycisk zmieniający etap (Przekaż do akceptacji, Zwróć do poprawy) |
| Interesariusz | osoba z rolą w sprawie (zgłaszający, akceptujący…) |
| Zastępstwo | okres, w którym inna osoba widzi i wykonuje moje zadania |
| Departament | jednostka organizacyjna (nigdy „dział”) |
| Schemat organizacyjny | graficzny widok struktury: departamenty, pracownicy, podległość i inne zależności |
| Terminarz | widok wpisów w czasie (rezerwacje, zdarzenia); grafik = terminarz osób na osi czasu |
| Biblioteka dokumentów | repozytorium plików z folderami (poza sprawą); pliki sprawy to załączniki |
| Komponent wzorca | komponent `Eod*`, przez który ekran używa Fluent lub Syncfusion z ustawieniami wzorca |
| Słownik | lista wartości wyboru w formularzach procesów (np. Formy zatrudnienia), prowadzona przez administratora biznesowego; pozycja słownika: kod, nazwa, opis, aktywna, kolejność |
| Zapis zbiorczy | zmiany zbierane na ekranie (tabela edycyjna, edytor) i zapisywane jednym przyciskiem „Zapisz zmiany”; do zapisu można je odrzucić |
