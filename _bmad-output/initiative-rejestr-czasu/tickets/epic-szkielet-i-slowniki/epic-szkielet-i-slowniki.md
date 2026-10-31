---
type: epic
title: "Szkielet aplikacji i Słowniki"
parent: initiative-rejestr-czasu
covers: [R1, R2, R3, R15, R17, R19, R26, R31]
sources: []
companions: []
after: []
refined: true
risk: high
---

# Szkielet aplikacji i Słowniki

## Description

Fundament ekranów, na którym stoją epiki 2, 3 i 5: aplikacja .NET 10 z Blazorem (szkielet z historii 1.1) ma układ z menu i belką, komunikaty, mechanizm stron pomocy i wspólną siatkę `ReportGrid` w pełnym zakresie AD-9. Na tym fundamencie, na serwerze, każdy User prowadzi własne Projekty i Typy pracy. Epik korzysta z epic-konta-i-wdrozenie (`UserId` z filtrem, logowanie, role, wdrożenie) i przekazuje kolejnym: `Domain/Czas`, `PolishCalendar`, `YearMonth`, `DictionaryOptions`, `MainLayout`, mechanizm pomocy, `Notify`, `Confirm`, `ReportGrid` i Testcontainers. Link Ustawienia konta w belce i pozycja Użytkownicy w menu do czasu budowy ich ekranów (epic-miesiac-i-zestawienie, epic-administracja-kont-i-pomoc) prowadzą do polskiej strony „nie znaleziono”.

## Outcome

Autor po zalogowaniu na serwerze wpisuje swoje 7 Projektów i 2 Typy pracy na listach z filtrem wartości, ze stroną pomocy pod ikoną `?` (R1–R3, R17, R26).

## Requirements

- R1 (parent R1): User dodaje i edytuje własne Projekty (kod unikalny wśród Projektów Usera bez względu na wielkość liter, zapisywany wielkimi literami; nazwa) i Typy pracy (nazwa unikalna wśród Typów pracy Usera bez względu na wielkość liter); zmiana obejmuje wszystkie jego Wpisy, także historyczne; dwóch Userów może mieć Projekt o tym samym kodzie.
- R2 (parent R2): Projekt i Typ pracy można dezaktywować i przywrócić; nieaktywne znikają z list wyboru formularza Wpisu, a zostają w istniejących Wpisach (także przy ich edycji), filtrach Listy wpisów, Widoku miesiąca i Zestawieniu.
- R3 (parent R3): Usunięcie Projektu lub Typu pracy udaje się tylko, gdy nie występuje w żadnym Wpisie Usera; inaczej User widzi komunikat z liczbą Wpisów i propozycją dezaktywacji.
- R15 (parent R15): Parametry połączenia z bazą i sekrety pochodzą z konfiguracji poza gitem, na serwerze i w potoku wdrożenia; lokalnie dane bazy leżą poza repozytorium i poza folderami synchronizowanymi z chmurą.
- R17 (parent R17): Każdy raport (Widok miesiąca, Lista wpisów) i lista Słownika ma filtr wartości w każdej kolumnie i licznik pozycji; filtr nie zmienia sum liczonych dla Miesiąca; brak eksportu CSV.
- R19 (parent R19): Zalogować się może tylko Konto Aktywne; Konto Oczekuje lub Zablokowane dostaje komunikat o stanie; po 5 nieudanych próbach z rzędu logowanie na Konto jest wstrzymane na 15 minut; User wylogowuje się z belki górnej.
- R26 (parent R26): Aplikacja ma strony pomocy po polsku; ikona `?` w belce otwiera stronę bieżącego ekranu, a z niej spis tematów; strona o administracji jest tylko dla Administratora; każdy ekran ma stronę pomocy zgodną z jego działaniem.
- R31 (parent R31): Gdy połączenie z serwerem się zerwie, aplikacja pokazuje komunikat z prośbą o odświeżenie; niezapisany Wpis może przepaść.

## Constraints

- prd NFR-1: aplikacja nie nawiązuje połączeń wychodzących; zasoby serwuje sama, a CSP blokuje inne źródła; ruch tylko przez HTTPS.
- prd NFR-2: baza, sekrety i konfiguracja z hasłami poza gitem, obrazem i logami potoku; testy, przykłady i dane startowe zanonimizowane.
- prd NFR-4: przeglądarka na komputerze, bez układu pod telefon; interfejs tylko po polsku.
- prd NFR-5: Czas liczony w minutach całkowitych; konwersja na godziny dziesiętne dopiero w Zestawieniu.
- prd NFR-6: belka górna z Pomocą `?`, Ustawieniami konta i Wyloguj; menu: Nowy wpis; Rejestr (Widok miesiąca, Lista wpisów, Eksport); Słowniki (Projekty, Typy pracy); Administracja (Użytkownicy) tylko dla Administratora.
- architecture AD-1: .NET 10, Blazor Interactive Server, Fluent UI Blazor 5, EF Core 10, PostgreSQL 18 w Dockerze; lokalnie `docker compose up -d`, potem `dotnet run`.
- architecture AD-12: zakaz komponentów Fluent UI ładujących skrypty z unpkg: `FluentNumberInput`, pola z `MaskPattern`, `FluentSortableList`.
- prd §3: terminy z `glossary.md` obowiązują dosłownie.

## Done when

- Na serwerze po zalogowaniu User dodaje, edytuje, dezaktywuje i usuwa własne Projekty i Typy pracy z regułami R1–R3, na listach z filtrem wartości i licznikiem (R17); drugi User nie widzi tych pozycji.
- Belka ma Pomoc `?`, Ustawienia konta i Wyloguj; menu pokazuje sekcję Administracja tylko Administratorowi; ikona `?` otwiera stronę pomocy bieżącego ekranu (R19, R26).
- Zerwanie połączenia pokazuje komunikat z prośbą o odświeżenie (R31).
- `ReportGrid` ze stałym grupowaniem i sumą od ekranu ma testy bUnit; zapis do bazy ma testy na Testcontainers.
- Przeglądarka nie wykonuje żadnego połączenia poza adresem aplikacji (nagłówek CSP aktywny).

## Boundaries

- Logowanie, rejestracja, role, `UserId` z filtrem i wdrożenie należą do epic-konta-i-wdrozenie; ten epik ich używa.
- Formularz Wpisu, Lista wpisów i Sesja wpisywania należą do epic-wpisy-i-lista; ukrywanie nieaktywnych pozycji w listach wyboru formularza (część R2) realizuje tamten epik przez `DictionaryOptions` z tego epiku.
- `MonthReport`, Widok miesiąca, Zestawienie i Ustawienia konta należą do epic-miesiac-i-zestawienie.
- Ekran Użytkownicy i strony pomocy ekranów bez logowania należą do epic-administracja-kont-i-pomoc.
- Grupowanie sterowane przez użytkownika, sortowanie wielokolumnowe, wybór kolumn i edycja inline w `ReportGrid` są poza v1.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R1–R3, R15, R17, R19, R26, R31
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-1, AD-2, AD-4, AD-6–AD-13, AD-16, AD-17, AD-22 (wersja 31.10.2026)
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Decision (30.10.2026): fundament przed Słownikami — pierwsze historie to scaffold, docker-compose, start z Kopią bazy, układ z pustą stroną; potem typy domeny, `ReportGrid`, na końcu Słowniki. Zmieniona 31.10.2026: Kopia bazy wypada, a platformę (CI, Konta, wdrożenie) daje epic-konta-i-wdrozenie.
- Decision (30.10.2026): `Domain/Czas`, `PolishCalendar`, `YearMonth` i `DictionaryOptions` w tym epiku jako fundament, choć pierwszym konsumentem jest epic-wpisy-i-lista.
- Decision (30.10.2026): `ReportGrid` w pełnym zakresie AD-9, ze stałym grupowaniem, powstaje tutaj („najpierw mechanizm, później zastosowanie”).
- Decision (30.10.2026): epik dopracowany wyłącznie z wymagań inicjatywy (R1–R3, R14, R15, R17), bez dodatkowych wejść; do ograniczeń dopisany zakaz komponentów z AD-12.
- Decision (30.10.2026): tracer bullet to historia 1 „Szkielet aplikacji ze schematem Initial” — szeroka (wszystkie encje, CSP, Testcontainers) zgodnie z zasadą „fundament najpierw”; zaraz po niej najmniej pewna historia 2 (Kopia bazy przez `docker exec pg_dump`). Historia 2 wycofana 31.10.2026.
- Decision (30.10.2026): historia 1 zakłada README, szkielet rejestracji usług w Program.cs, bUnit i czyszczenie danych Testcontainers, żeby historie równoległe 2, 3, 5, 9 nie tworzyły ich osobno.
- Decision (30.10.2026): menu od razu zawiera wszystkie pozycje; ekrany z epików 2 i 3 do czasu ich budowy prowadzą do polskiej strony „nie znaleziono”, a każda późniejsza historia tylko dodaje swoją stronę. Od 31.10.2026 dotyczy też Ustawień konta (epik 3) i Użytkowników (epik 5).
- Decision (30.10.2026): opcje `DictionaryOptions` sortowane w C# według pl-PL — Projekty po kodzie, Typy pracy po nazwie.
- Decision (30.10.2026): bez zamykającej historii testów end-to-end — źródła nie mają planu testów; ReportGrid podzielony na listę (historia 5) i stałe grupowanie (historia 6, ryzyko wysokie).
- Decision (31.10.2026): re-slice po forge chmura-konta-wdrożenie (PRD 31.10.2026): R14 wycofane, dochodzą R19 (Wyloguj w belce), R26 (mechanizm pomocy) i R31 (zerwane połączenie) w historii 3; historie 3, 7, 8 i 9 czekają na epic-konta-i-wdrozenie; zrobiona historia 1 zostaje bez zmian.
- Dropped: historia 2 „Kopia bazy przy starcie i odtwarzanie z README” (31.10.2026) — prd FR-14 usunięty, R14 wycofane; kopie zapewnia Kopia serwera dostawcy, a `docker exec pg_dump` nie działa w układzie z serwerem.
- Waits on epic-konta-i-wdrozenie because: schemat `Initial` z `UserId` i globalnym filtrem, logowanie i wylogowanie, role, autoryzacja tras, układ dla niezalogowanych, wdrożenie na serwer.
