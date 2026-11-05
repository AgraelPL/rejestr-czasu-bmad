---
type: epic
title: "Widok miesiąca i Zestawienie"
parent: initiative-rejestr-czasu
covers: [R9, R10, R11, R12, R13, R17, R25, R26]
sources: []
companions: []
after: []
refined: false
risk: high
---

# Widok miesiąca i Zestawienie

## Description

Zamknięcie Miesiąca, sprawdzane lokalnie: User sprawdza Miesiąc w siatce grupowanej Tydzień → Dzień i pobiera Zestawienie xlsx we wspólnym układzie Odbiorcy jednym kliknięciem w Widoku miesiąca, z prefiksem nazwy pliku z Ustawień konta. Epik buduje ekran Ustawienia konta (prefiks i link do strony zmiany hasła z epic-konta-i-wdrozenie, R25); wiersz `account_settings` powstaje przy pierwszym zapisie prefiksu, a Zestawienie bez wiersza używa pustego prefiksu (AD-7), do którego prowadzi link w belce z epic-szkielet-i-slowniki. Każdy ekran tego epiku dopisuje swoją stronę pomocy (R26). Epik posiada `MonthEntriesQuery` i `MonthReport.Build`, jedyne źródło sum (AD-5), oraz endpoint `/zestawienie/{rok}/{miesiac}` (AD-14).

## Outcome

Test wrześniowy przechodzi: Zestawienie za wrzesień 2026 ma Sumy dnia, Sumy tygodnia 30 / 16 / 18 / 25 / 14 h i RAZEM 103 h zgodne z oryginałem (prd SM-1; R9–R13).

## Done when

- `MonthReport` ma testy xUnit na zanonimizowanych danych o kształcie września: sumy tygodni 30 / 16 / 18 / 25 / 14, RAZEM 103, złączanie i kolejność wierszy (R10, R11).
- Widok miesiąca lokalnie pokazuje sumy identyczne z pobranym Zestawieniem, także przy włączonym filtrze, sprawdzone na zanonimizowanych danych (R9, R10, R17).
- Zestawienie otwiera się w Excelu w układzie z `export-layout.md`, z nazwą pliku według R13 i prefiksem z Ustawień konta; pusty Miesiąc daje komunikat (R12, R13).
- W Ustawieniach konta User ustawia prefiks i przechodzi do zmiany hasła; ekrany Widok miesiąca i Ustawienia konta mają strony pomocy (R25, R26).
- Test wrześniowy wykonany lokalnie na prawdziwych danych, bez zapisu ich w repozytorium i na serwerze.

## Boundaries

- Formularz Wpisu i `DeleteEntry` dostarcza epic-wpisy-i-lista; ten epik używa ich z ikon w wierszach Widoku miesiąca.
- `ReportGrid` ze stałym grupowaniem i mechanizm pomocy dostarcza epic-szkielet-i-slowniki; ten epik go nie rozszerza.
- Strona zmiany hasła należy do epic-konta-i-wdrozenie; hasło ustawiane przez Administratora do epic-administracja-kont-i-pomoc.
- Pierwszy Miesiąc „na serio” (SM-2) i notatka o metodzie (SM-4) są kryteriami inicjatywy, nie tego epiku.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R9–R13, R17, R25, R26
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-5, AD-6, AD-9, AD-14
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
