---
type: epic
title: "Szkielet aplikacji i Słowniki"
parent: initiative-rejestr-czasu
covers: [R1, R2, R3, R14, R15, R17]
sources: []
companions: []
after: []
refined: false
risk: medium
---

# Szkielet aplikacji i Słowniki

## Description

Fundament, na którym stoją pozostałe epiki: aplikacja .NET 10 z Blazorem startuje przez `docker compose up -d` i `dotnet run`, robi Kopię bazy i migracje w kolejności z AD-3, ma układ z menu, komunikaty i wspólną siatkę `ReportGrid` w pełnym zakresie AD-9. Na tym fundamencie Autor prowadzi Projekty i Typy pracy. Epik przekazuje kolejnym: schemat z migracją `Initial`, `Domain/Czas`, `PolishCalendar`, `YearMonth`, `DictionaryOptions`, `MainLayout`, `Notify`, `Confirm`, `ReportGrid` i Testcontainers.

## Outcome

Autor uruchamia aplikację lokalnie, widzi Kopię bazy w katalogu kopii i wpisuje 7 Projektów i 2 Typy pracy na listach z filtrem wartości (R1–R3, R14, R15, R17).

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
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-1–AD-4, AD-6–AD-9, AD-11–AD-13
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Decision (30.10.2026): fundament przed Słownikami — pierwsze historie to scaffold, docker-compose, start z Kopią bazy, układ z pustą stroną; potem typy domeny, `ReportGrid`, na końcu Słowniki.
- Decision (30.10.2026): `Domain/Czas`, `PolishCalendar`, `YearMonth` i `DictionaryOptions` w tym epiku jako fundament, choć pierwszym konsumentem jest epic-wpisy-i-lista.
- Decision (30.10.2026): `ReportGrid` w pełnym zakresie AD-9, ze stałym grupowaniem, powstaje tutaj („najpierw mechanizm, później zastosowanie”).
