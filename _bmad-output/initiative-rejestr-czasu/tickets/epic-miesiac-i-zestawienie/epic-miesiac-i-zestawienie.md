---
type: epic
title: "Widok miesiąca i Zestawienie"
parent: initiative-rejestr-czasu
covers: [R9, R10, R11, R12, R13, R17, R25, R26]
sources: []
companions: []
after: []
refined: true
risk: high
---

# Widok miesiąca i Zestawienie

## Description

Zamknięcie Miesiąca, sprawdzane lokalnie: User sprawdza Miesiąc w siatce grupowanej Tydzień → Dzień i pobiera Zestawienie xlsx we wspólnym układzie Odbiorcy jednym kliknięciem w Widoku miesiąca, z prefiksem nazwy pliku z Ustawień konta. Epik buduje ekran Ustawienia konta (prefiks i link do strony zmiany hasła z epic-konta-i-wdrozenie, R25); wiersz `account_settings` powstaje przy pierwszym zapisie prefiksu, a Zestawienie bez wiersza używa pustego prefiksu (AD-7), do którego prowadzi link w belce z epic-szkielet-i-slowniki. Każdy ekran tego epiku dopisuje swoją stronę pomocy (R26). Epik posiada `MonthEntriesQuery` i `MonthReport.Build`, jedyne źródło sum (AD-5), oraz endpoint `/zestawienie/{rok}/{miesiac}` (AD-14).

## Outcome

Test wrześniowy przechodzi: Zestawienie za wrzesień 2026 ma Sumy dnia, Sumy tygodnia 30 / 16 / 18 / 25 / 14 h i RAZEM 103 h zgodne z oryginałem (prd SM-1; R9–R13).

## Requirements

- R1 (parent R9): Widok miesiąca to siatka Wpisów wybranego Miesiąca (domyślnie bieżącego) grupowana Tydzień → Dzień, tylko dni z Wpisami, w dniu według kodu Projektu, potem nazwy Typu pracy, z dniem tygodnia, Sumą tygodnia i Sumą dnia w nagłówkach grup oraz sumą Miesiąca; sumy dotyczą całego Miesiąca także przy włączonym filtrze; edycja, kopia i usunięcie Wpisu przeliczają sumy od razu.
- R2 (parent R10): Suma dnia, Suma tygodnia i suma Miesiąca w Widoku miesiąca są identyczne z sumami w Zestawieniu za ten Miesiąc.
- R3 (parent R11): W Zestawieniu Wpisy z tego samego dnia o tym samym Projekcie i Typie pracy tworzą jeden Wiersz zestawienia: Czas się sumuje, opisy trafiają do kolejnych linii jednej komórki w kolejności dodania.
- R4 (parent R12): Zestawienie xlsx w układzie z `export-layout.md`, wspólnym dla wszystkich Userów, pobiera się jednym kliknięciem w Widoku miesiąca; sumy są wartościami, nie formułami.
- R5 (parent R13): Plik Zestawienia nazywa się `<prefiks>_Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx` (prefiks z Ustawień konta Usera; bez prefiksu `Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx`); eksport Miesiąca bez Wpisów daje komunikat zamiast pliku.
- R6 (parent R17): Każdy raport (Widok miesiąca, Lista wpisów, lista Wpisów bieżącego Miesiąca na ekranie Nowy wpis) i lista Słownika ma filtr wartości w każdej kolumnie i licznik pozycji; filtr nie zmienia sum liczonych dla Miesiąca. Lista wpisów i lista na Nowym wpisie dzielą się na strony po 30 wierszy i mają przycisk Eksportuj, który pobiera plik xlsx z Wpisami spełniającymi wszystkie filtry, ze wszystkich stron (na Nowym wpisie tylko bieżący Miesiąc): Data jako data, Czas jako Godziny dziesiętne, wiersz Suma; brak eksportu CSV.
- R7 (parent R25): W Ustawieniach konta User ustawia prefiks pliku Zestawienia i zmienia własne hasło, podając obecne.
- R8 (parent R26): Aplikacja ma strony pomocy po polsku; ikona `?` w belce otwiera stronę bieżącego ekranu, a z niej spis tematów; strona o administracji jest tylko dla Administratora; każdy ekran ma stronę pomocy zgodną z jego działaniem.

## Done when

- `MonthReport` ma testy xUnit na zanonimizowanych danych o kształcie września: sumy tygodni 30 / 16 / 18 / 25 / 14, RAZEM 103, złączanie i kolejność wierszy (R2, R3).
- Widok miesiąca lokalnie pokazuje sumy identyczne z pobranym Zestawieniem, także przy włączonym filtrze, sprawdzone na zanonimizowanych danych (R1, R2, R6).
- Zestawienie otwiera się w Excelu w układzie z `export-layout.md`, z nazwą pliku według R5 i prefiksem z Ustawień konta; pusty Miesiąc daje komunikat (R4, R5).
- W Ustawieniach konta User ustawia prefiks i przechodzi do zmiany hasła; ekrany Widok miesiąca i Ustawienia konta mają strony pomocy; w menu nie ma pozycji Eksport (R7, R8).
- Test wrześniowy wykonany lokalnie na prawdziwych danych, bez zapisu ich w repozytorium i na serwerze.

## Boundaries

- Formularz Wpisu i `DeleteEntry` dostarcza epic-wpisy-i-lista; ten epik używa ich z ikon w wierszach Widoku miesiąca.
- `ReportGrid` ze stałym grupowaniem i mechanizm pomocy dostarcza epic-szkielet-i-slowniki; ten epik go nie rozszerza.
- Strona zmiany hasła należy do epic-konta-i-wdrozenie; hasło ustawiane przez Administratora do epic-administracja-kont-i-pomoc.
- Pierwszy Miesiąc „na serio” (SM-2) i notatka o metodzie (SM-4) są kryteriami inicjatywy, nie tego epiku.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R9–R13, R17, R25, R26
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-5, AD-6, AD-7, AD-8, AD-9, AD-10, AD-11, AD-14, AD-15, AD-22, AD-23
- export layout — _bmad-output/initiative-rejestr-czasu/export-layout.md
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Waits on epic-szkielet-i-slowniki because: `ReportGrid` ze stałym grupowaniem, schemat, typy domeny, `Notify` i `Confirm`.
- Waits on epic-wpisy-i-lista because: `EntryForm` w oknie dialogowym i `DeleteEntry` dla ikon w wierszach.
- Decision (31.10.2026): re-slice po forge chmura-konta-wdrożenie: User zamiast Autora, praca na serwerze; dochodzi ekran Ustawienia konta z prefiksem (R13) i zmianą własnego hasła (R25), żeby epic-administracja-kont-i-pomoc zależał tylko od epic-konta-i-wdrozenie.
- Decision (31.10.2026): po aktualizacji architektury (AD-15) zmiana własnego hasła jest na statycznej `/konto/zmiana-hasla` z epic-konta-i-wdrozenie; Ustawienia konta tylko do niej linkują.
- Waits on epic-konta-i-wdrozenie because: strona `/konto/zmiana-hasla`.
- Decision (31.10.2026): wiersz `account_settings` powstaje leniwie przy pierwszym zapisie Ustawień konta w kontekście Usera; Zestawienie bez wiersza używa pustego prefiksu (decyzja Autora w przeglądzie historii 4.2, AD-7).
- Decision (01.11.2026): Autor odkłada wdrożenie na Droplet na koniec, po dostarczeniu funkcji z epików 1–3 i 5: migracja `Initial` zostaje otwarta do pierwszego wdrożenia (NFR-9), bez kosztów i obsługi serwera, zanim aplikacja jest użyteczna. Wdrożenie, rollback i sprawdzenia na serwerze przeszły do nowego epic-wdrozenie-na-serwer (epik 6), przed Miesiącem „na serio”. Done when sprawdzany lokalnie; na serwerze powtarza go historia 6.3.
- Waits on epic-wzorzec-ui because: `EodShell`, `EodPage`, `EodReport` z sumami od ekranu (`MonthReport`) i `UiMessages` ze wzorca eod-ui.
- Decision (02.11.2026): Autor przejmuje wzorzec UI eod-ui przed tym epikiem (epik 7); Widok miesiąca, Eksport i Ustawienia konta powstają z komponentów wzorca zamiast `ReportGrid`, `Notify` i `Confirm`; sumy Widoku miesiąca z `MonthReport` przekazywane do `EodReport` (R9, R10, AD-5). Opis epiku do dopracowania przy jego refinemencie.
- Decision (05.11.2026): replan po epiku 7 (decyzje Autora 04–05.11.2026): ekran Eksport odpada, Zestawienie tylko z Widoku miesiąca (R12); eksport xlsx list Wpisów i strony po 30 wierszy zbudował już epik 7 (R17). Ten epik usuwa pozycję menu „Eksport” (`Shared/MainLayout.razor`, NFR-6, AD-11). `EodReport` ma już parametry `ShowExport`, `PageSize`, `FooterSummary` i `ExportFile` (AD-23 poz. 1–14); Widok miesiąca podaje `ShowExport="false"`.
- Decision (05.11.2026): sprawy przekazane z epiku 7 (`deferred-work.md`) do rozpisania przy refinemencie tego epiku: (1) uzupełnienie kluczy `PolishLocalizer` z testem pokrycia zasobu Fluent UI (część tekstów biblioteki może być po angielsku); (2) `/Error` i „Sprawdzanie uprawnień...” na `EodPage`, potem usunięcie reguły przejściowej `.eod-content:not(:has(> .eod-page))` w `app.css`; (3) testy przeglądarkowe (Playwright) dla `EodUI.showSelectedText`, `eod-scale.js`, pobierania pliku i okien; (4) przy Widoku miesiąca rozważyć kolejność paska komunikatu i przeładowania listy po zmianie w oknie Wpisu.
- Decision (05.11.2026): refinement — wymagania epiku to linie rodzica R9–R13, R17, R25, R26 (R1–R8 tutaj). Ustalenia Autora: nagłówek grupy Tydzień pokazuje numer w Miesiącu i zakres dat tylko z tego Miesiąca, np. „Tydzień 1 (01.09–06.09)”; Miesiąc wybierają przyciski Poprzedni / Następny / Bieżący, a Miesiąc jest w adresie (`/miesiac/RRRR-MM`, bez niego bieżący); prefiks: spacje na końcach pomijane, najwyżej 100 znaków, bez znaków `\ / : * ? " < > |`, puste pole usuwa prefiks.
- Decision (05.11.2026): refinement — wiersze Widoku to pojedyncze Wpisy (ikony działają po `Id`), w dniu według kodu Projektu, nazwy Typu pracy, potem kolejności dodania; złączanie w Wiersze zestawienia tylko w pliku. Przy filtrze nagłówek grupy pokazuje mniejszą liczbę pozycji, a sumę całego Miesiąca (R1). Zestawienie generuje serwer (endpoint AD-14), nie `EodReport.ExportFile`. Historie, które zmieniają ekran Eksport w architekturze (AD-11, AD-14, AD-22) i PRD §4.5, poprawiają te zapisy tak jak plany 7.9–7.10.
- Decision (05.11.2026): testy przeglądarkowe (Playwright) poza tym epikiem — osobna historia po Teście wrześniowym, żeby nie opóźniać terminu 28.12.2026 (Autor).
- Decision (05.11.2026): podział na historie — tracer bullet to historia 1 (MonthReport w całości z Wierszami zestawienia, AD-5, plus ekran /miesiac z sumami); najmniej pewna jest historia 3 (Zestawienie co do komórki), zaraz po tracerze; 4 i 5 po 3; 6 i 7 niezależne, mogą iść równolegle; 8 (Test wrześniowy, hitl) po 3, 4 i 5; zamyka Refactor sweep (9). Pierwotna historia 2 (Wiersze zestawienia osobno) scalona z 1 po kontroli zestawu, bo AD-5 wymaga typów MonthReport w jednej historii; id 2 nie jest używane ponownie. Usunięcie pozycji menu „Eksport” i zapisów o ekranie Eksport w AD-11, AD-14, AD-22 i PRD §4.5 w historii 3. Zły adres /miesiac/... daje stronę „nie znaleziono” (propozycja PO, do zmiany przez Autora).
