---
type: epic
title: "Wpisy w Sesji wpisywania i Lista wpisów"
parent: initiative-rejestr-czasu
covers: [R2, R4, R5, R6, R7, R8, R16, R17]
sources: []
companions: []
after: []
refined: false
risk: medium
---

# Wpisy w Sesji wpisywania i Lista wpisów

## Description

Główna część pracy Autora: w Sesji wpisywania przenosi notatki z kilku dni do Wpisów samą klawiaturą, a na Liście wpisów wyszukuje, edytuje, kopiuje i usuwa Wpisy z sumą Czasu. Epik przekazuje epic-miesiac-i-zestawienie `EntryForm` (strona `/wpisy/nowy` i okno dialogowe) oraz `DeleteEntry` z potwierdzeniem.

## Outcome

Autor przenosi notatki z 3–4 dni w jednej Sesji wpisywania, każdy kolejny Wpis w nie więcej niż 10 s samą klawiaturą, i odnajduje je na Liście wpisów (R4–R8, R16).

## Done when

- Sesja wpisywania na komputerze Autora: kilkanaście Wpisów z różnych dni, formularz zostaje z tą samą datą po zapisie, każdy Wpis samą klawiaturą (R4, R5, R16).
- Edycja i kopia z ikon w wierszu Listy wpisów otwierają okno dialogowe; usunięcie wymaga potwierdzenia; lista pokazuje dane z bazy po zmianie (R6, R7, R8).
- Nieaktywne Projekty i Typy pracy nie pojawiają się w listach wyboru, a zostają przy edycji istniejącego Wpisu (R2).
- Filtr wartości i licznik na Liście wpisów; suma Czasu pod listą zgodna z widocznymi filtrami zakresu, Projektu i Typu pracy (R8, R17).

## Boundaries

- Słowniki, `ReportGrid`, typy domeny i `DictionaryOptions` dostarcza epic-szkielet-i-slowniki; ten epik ich używa.
- Widok miesiąca, `MonthReport` i Zestawienie należą do epic-miesiac-i-zestawienie.
- Edycja inline w siatce jest poza v1.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R2, R4–R8, R16, R17
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-4, AD-6, AD-8–AD-11
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Waits on epic-szkielet-i-slowniki because: schemat z migracją `Initial`, `Domain/Czas`, `PolishCalendar`, `YearMonth`, `DictionaryOptions`, `ReportGrid`, `MainLayout`, `Notify` i `Confirm`.
