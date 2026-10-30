---
type: epic
title: "Widok miesiąca i Zestawienie"
parent: initiative-rejestr-czasu
covers: [R9, R10, R11, R12, R13, R17]
sources: []
companions: []
after: []
refined: false
risk: high
---

# Widok miesiąca i Zestawienie

## Description

Zamknięcie Miesiąca: Autor sprawdza Miesiąc w siatce grupowanej Tydzień → Dzień i pobiera Zestawienie xlsx w układzie Odbiorcy, z Widoku miesiąca albo z ekranu Eksport. Epik posiada `MonthEntriesQuery` i `MonthReport.Build`, jedyne źródło sum (AD-5), oraz endpoint `/zestawienie/{rok}/{miesiac}` (AD-14).

## Outcome

Test wrześniowy przechodzi: Zestawienie za wrzesień 2026 ma Sumy dnia, Sumy tygodnia 30 / 16 / 18 / 25 / 14 h i RAZEM 103 h zgodne z oryginałem (prd SM-1; R9–R13).

## Done when

- `MonthReport` ma testy xUnit na zanonimizowanych danych o kształcie września: sumy tygodni 30 / 16 / 18 / 25 / 14, RAZEM 103, złączanie i kolejność wierszy (R10, R11).
- Widok miesiąca na komputerze Autora pokazuje sumy identyczne z pobranym Zestawieniem, także przy włączonym filtrze (R9, R10, R17).
- Zestawienie otwiera się w Excelu w układzie z `export-layout.md`, z nazwą pliku według R13; pusty Miesiąc daje komunikat (R12, R13).
- Test wrześniowy wykonany lokalnie na prawdziwych danych, bez zapisu ich w repozytorium.

## Boundaries

- Formularz Wpisu i `DeleteEntry` dostarcza epic-wpisy-i-lista; ten epik używa ich z ikon w wierszach Widoku miesiąca.
- `ReportGrid` ze stałym grupowaniem dostarcza epic-szkielet-i-slowniki; ten epik go nie rozszerza.
- Pierwszy Miesiąc „na serio” (SM-2) i notatka o metodzie (SM-4) są kryteriami inicjatywy, nie tego epiku.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R9–R13, R17
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-5, AD-6, AD-9, AD-14
- export layout — _bmad-output/initiative-rejestr-czasu/export-layout.md
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Waits on epic-szkielet-i-slowniki because: `ReportGrid` ze stałym grupowaniem, schemat, typy domeny, `Notify` i `Confirm`.
- Waits on epic-wpisy-i-lista because: `EntryForm` w oknie dialogowym i `DeleteEntry` dla ikon w wierszach.
