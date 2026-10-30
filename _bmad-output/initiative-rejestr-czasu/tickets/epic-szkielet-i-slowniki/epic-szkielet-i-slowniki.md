---
type: epic
title: "Szkielet aplikacji i Słowniki"
parent: initiative-rejestr-czasu
covers: [R1, R2, R3, R14, R15, R17]
sources: []
companions: []
after: []
refined: true
risk: high
---

# Szkielet aplikacji i Słowniki

## Description

Fundament, na którym stoją pozostałe epiki: aplikacja .NET 10 z Blazorem startuje przez `docker compose up -d` i `dotnet run`, robi Kopię bazy i migracje w kolejności z AD-3, ma układ z menu, komunikaty i wspólną siatkę `ReportGrid` w pełnym zakresie AD-9. Na tym fundamencie Autor prowadzi Projekty i Typy pracy. Epik przekazuje kolejnym: schemat z migracją `Initial`, `Domain/Czas`, `PolishCalendar`, `YearMonth`, `DictionaryOptions`, `MainLayout`, `Notify`, `Confirm`, `ReportGrid` i Testcontainers.

## Outcome

Autor uruchamia aplikację lokalnie, widzi Kopię bazy w katalogu kopii i wpisuje 7 Projektów i 2 Typy pracy na listach z filtrem wartości (R1–R3, R14, R15, R17).

## Requirements

- R1 (parent R1): Autor dodaje i edytuje Projekty (kod unikalny bez względu na wielkość liter, zapisywany wielkimi literami; nazwa) i Typy pracy (nazwa unikalna bez względu na wielkość liter); zmiana obejmuje wszystkie Wpisy, także historyczne.
- R2 (parent R2): Projekt i Typ pracy można dezaktywować i przywrócić; nieaktywne znikają z list wyboru formularza Wpisu, a zostają w istniejących Wpisach (także przy ich edycji), filtrach Listy wpisów, Widoku miesiąca i Zestawieniu.
- R3 (parent R3): Usunięcie Projektu lub Typu pracy udaje się tylko, gdy nie występuje w żadnym Wpisie; inaczej Autor widzi komunikat z liczbą Wpisów i propozycją dezaktywacji.
- R14 (parent R14): Przy każdym uruchomieniu, zanim aplikacja przyjmie pierwsze żądanie, powstaje Kopia bazy `rejestr-RRRR-MM-DD_GGMMSS.sql` (`pg_dump`) w osobnym katalogu; zostaje 10 najnowszych, najstarsza jest usuwana dopiero po udanym zapisie nowej; niedostępna baza albo nieudana kopia zatrzymują uruchomienie z czytelnym komunikatem.
- R15 (parent R15): Parametry połączenia z bazą i katalog kopii pochodzą z konfiguracji poza gitem; dane bazy leżą w wolumenie Dockera, a domyślny katalog kopii poza repozytorium i poza folderami synchronizowanymi z chmurą.
- R17 (parent R17): Każdy raport (Widok miesiąca, Lista wpisów) i lista Słownika ma filtr wartości w każdej kolumnie i licznik pozycji; filtr nie zmienia sum liczonych dla Miesiąca; brak eksportu CSV.

## Constraints

- prd NFR-1: nasłuch tylko na `127.0.0.1`; zero połączeń wychodzących (telemetria, CDN, zewnętrzne czcionki), wszystkie zasoby lokalne; brak logowania i kont.
- prd NFR-2: baza, kopie i plik konfiguracji poza gitem; testy, przykłady i dane startowe zanonimizowane.
- prd NFR-4: desktop, interfejs tylko po polsku; bez autostartu.
- prd NFR-5: Czas liczony w minutach całkowitych; konwersja na godziny dziesiętne dopiero w Zestawieniu.
- architecture AD-1, AD-2: .NET 10, Blazor Interactive Server, Fluent UI Blazor 5, EF Core 10, PostgreSQL 18 w Dockerze (port tylko na `127.0.0.1`); uruchomienie `docker compose up -d`, potem `dotnet run`.
- architecture AD-12: zakaz komponentów Fluent UI ładujących skrypty z unpkg: `FluentNumberInput`, pola z `MaskPattern`, `FluentSortableList`.
- prd §3: terminy z `glossary.md` obowiązują dosłownie.

## Done when

- `docker compose up -d` i `dotnet run` na komputerze Autora uruchamiają aplikację na `127.0.0.1`; przy starcie powstaje Kopia bazy `.sql`, a bez działającej bazy start kończy się komunikatem (R14, R15).
- Odtworzenie bazy z Kopii bazy według README zostało raz wykonane i sprawdzone.
- Autor dodaje, edytuje, dezaktywuje i usuwa Projekty i Typy pracy z regułami R1–R3, na listach z filtrem wartości i licznikiem (R17).
- `ReportGrid` ze stałym grupowaniem i sumą od ekranu ma testy bUnit; zapis do bazy ma testy na Testcontainers.
- Przeglądarka nie wykonuje żadnego połączenia poza `127.0.0.1` (nagłówek CSP aktywny).

## Boundaries

- Formularz Wpisu, Lista wpisów i Sesja wpisywania należą do epic-wpisy-i-lista; ukrywanie nieaktywnych pozycji w listach wyboru formularza (część R2) realizuje tamten epik przez `DictionaryOptions` z tego epiku.
- `MonthReport`, Widok miesiąca i Zestawienie należą do epic-miesiac-i-zestawienie.
- Grupowanie sterowane przez użytkownika, sortowanie wielokolumnowe, wybór kolumn i edycja inline w `ReportGrid` są poza v1.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R1–R3, R14, R15, R17
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-1–AD-4, AD-6–AD-13
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Decision (30.10.2026): fundament przed Słownikami — pierwsze historie to scaffold, docker-compose, start z Kopią bazy, układ z pustą stroną; potem typy domeny, `ReportGrid`, na końcu Słowniki.
- Decision (30.10.2026): `Domain/Czas`, `PolishCalendar`, `YearMonth` i `DictionaryOptions` w tym epiku jako fundament, choć pierwszym konsumentem jest epic-wpisy-i-lista.
- Decision (30.10.2026): `ReportGrid` w pełnym zakresie AD-9, ze stałym grupowaniem, powstaje tutaj („najpierw mechanizm, później zastosowanie”).
- Decision (30.10.2026): epik dopracowany wyłącznie z wymagań inicjatywy (R1–R3, R14, R15, R17), bez dodatkowych wejść; do ograniczeń dopisany zakaz komponentów z AD-12.
- Decision (30.10.2026): tracer bullet to historia 1 „Szkielet aplikacji ze schematem Initial” — szeroka (wszystkie encje, CSP, Testcontainers) zgodnie z zasadą „fundament najpierw”; zaraz po niej najmniej pewna historia 2 (Kopia bazy przez `docker exec pg_dump`).
- Decision (30.10.2026): historia 1 zakłada README, szkielet rejestracji usług w Program.cs, bUnit i czyszczenie danych Testcontainers, żeby historie równoległe 2, 3, 5, 9 nie tworzyły ich osobno.
- Decision (30.10.2026): menu od razu zawiera wszystkie pozycje; ekrany z epików 2 i 3 do czasu ich budowy prowadzą do polskiej strony „nie znaleziono”, a każda późniejsza historia tylko dodaje swoją stronę.
- Decision (30.10.2026): opcje `DictionaryOptions` sortowane w C# według pl-PL — Projekty po kodzie, Typy pracy po nazwie.
- Decision (30.10.2026): bez zamykającej historii testów end-to-end — źródła nie mają planu testów; ReportGrid podzielony na listę (historia 5) i stałe grupowanie (historia 6, ryzyko wysokie).
