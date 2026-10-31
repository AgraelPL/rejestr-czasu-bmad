---
type: epic
title: "Wpisy w Sesji wpisywania i Lista wpisów"
parent: initiative-rejestr-czasu
covers: [R2, R4, R5, R6, R7, R8, R16, R17, R26]
sources: []
companions: []
after: []
refined: false
risk: medium
---

# Wpisy w Sesji wpisywania i Lista wpisów

## Description

Główna część pracy Usera, na serwerze: w Sesji wpisywania przenosi notatki z kilku dni do własnych Wpisów samą klawiaturą, a na Liście wpisów wyszukuje, edytuje, kopiuje i usuwa swoje Wpisy z sumą Czasu. Data domyślna to dzisiejsza (Europe/Warsaw) przy każdym otwarciu ekranu Nowy wpis. Każdy ekran tego epiku dopisuje swoją stronę pomocy przez mechanizm z epic-szkielet-i-slowniki (R26). Epik przekazuje epic-miesiac-i-zestawienie `EntryForm` (strona `/wpisy/nowy` i okno dialogowe) oraz `DeleteEntry` z potwierdzeniem.

## Outcome

User przenosi na serwerze notatki z 3–4 dni w jednej Sesji wpisywania, każdy kolejny Wpis w nie więcej niż 10 s samą klawiaturą, i odnajduje je na Liście wpisów (R4–R8, R16).

## Done when

- Sesja wpisywania na serwerze po wdrożeniu z gałęzi głównej: kilkanaście Wpisów z różnych dni, data domyślna dzisiejsza według Europe/Warsaw, formularz zostaje z tą samą datą po zapisie, każdy Wpis samą klawiaturą w nie więcej niż 10 s (R4, R5, R16).
- Edycja i kopia z ikon w wierszu Listy wpisów otwierają okno dialogowe; usunięcie wymaga potwierdzenia; lista pokazuje dane z bazy po zmianie (R6, R7, R8).
- Nieaktywne Projekty i Typy pracy nie pojawiają się w listach wyboru, a zostają przy edycji istniejącego Wpisu (R2).
- Filtr wartości i licznik na Liście wpisów; suma Czasu pod listą zgodna z widocznymi filtrami zakresu, Projektu i Typu pracy (R8, R17).
- Lista wpisów drugiego Usera nie pokazuje tych Wpisów; ekrany Nowy wpis i Lista wpisów mają strony pomocy (R26).

## Boundaries

- Słowniki, `ReportGrid`, typy domeny, `DictionaryOptions` i mechanizm pomocy dostarcza epic-szkielet-i-slowniki; logowanie i oddzielenie danych epic-konta-i-wdrozenie; ten epik ich używa.
- Widok miesiąca, `MonthReport` i Zestawienie należą do epic-miesiac-i-zestawienie.
- Edycja inline w siatce jest poza v1.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R2, R4–R8, R16, R17, R26
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-4, AD-6, AD-8–AD-11
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Waits on epic-szkielet-i-slowniki because: `Domain/Czas`, `PolishCalendar`, `YearMonth`, `DictionaryOptions`, `ReportGrid`, `MainLayout`, mechanizm pomocy, `Notify` i `Confirm`.
- Decision (31.10.2026): re-slice po forge chmura-konta-wdrożenie: User zamiast Autora, praca na serwerze, R5 z datą przy każdym otwarciu ekranu Nowy wpis, R16 przez internet, R26 strony pomocy ekranów tego epiku.
