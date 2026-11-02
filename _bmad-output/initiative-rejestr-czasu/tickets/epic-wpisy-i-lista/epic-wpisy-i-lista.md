---
type: epic
title: "Wpisy w Sesji wpisywania i Lista wpisów"
parent: initiative-rejestr-czasu
covers: [R2, R4, R5, R6, R7, R8, R16, R17, R26]
sources: []
companions: []
after: []
refined: true
risk: medium
---

# Wpisy w Sesji wpisywania i Lista wpisów

## Description

Główna część pracy Usera, sprawdzana lokalnie: w Sesji wpisywania przenosi notatki z kilku dni do własnych Wpisów samą klawiaturą, a na Liście wpisów wyszukuje, edytuje, kopiuje i usuwa swoje Wpisy z sumą Czasu. Data domyślna to dzisiejsza (Europe/Warsaw) przy każdym otwarciu ekranu Nowy wpis. Każdy ekran tego epiku dopisuje swoją stronę pomocy przez mechanizm z epic-szkielet-i-slowniki (R26). Epik przekazuje epic-miesiac-i-zestawienie `EntryForm` (strona `/wpisy/nowy` i okno dialogowe) oraz `DeleteEntry` z potwierdzeniem.

## Outcome

User przenosi w aplikacji uruchomionej lokalnie notatki z 3–4 dni w jednej Sesji wpisywania, każdy kolejny Wpis w nie więcej niż 10 s samą klawiaturą, i odnajduje je na Liście wpisów (R4–R8, R16).

## Requirements

- R2 (parent R2): Projekt i Typ pracy można dezaktywować i przywrócić; nieaktywne znikają z list wyboru formularza Wpisu, a zostają w istniejących Wpisach (także przy ich edycji), filtrach Listy wpisów, Widoku miesiąca i Zestawieniu. Tu: listy wyboru formularza Wpisu (przez `DictionaryOptions`) i filtry Listy wpisów.
- R4 (parent R4): User dodaje Wpis z datą, jednym Projektem, jednym Typem pracy, wymaganym opisem i Czasem; Czas spoza `0:15`–`24:00` lub niebędący wielokrotnością 15 minut jest odrzucany z komunikatem; dzień tygodnia jest wyliczany z daty; Suma dnia ponad 24 h nie jest blokowana.
- R5 (parent R5): Przy każdym otwarciu ekranu Nowy wpis data domyślna to dzisiejsza (Europe/Warsaw); po zapisie formularz zostaje otwarty z tą samą datą, a Projekt, Typ pracy, opis i Czas są wyczyszczone.
- R6 (parent R6): User edytuje każde pole Wpisu w oknie dialogowym otwieranym ikoną w wierszu raportu i usuwa Wpis po potwierdzeniu, także w poprzednich Miesiącach; po zmianie raport pokazuje dane z bazy. Tu: z Listy wpisów; Widok miesiąca używa tego samego okna w epic-miesiac-i-zestawienie.
- R7 (parent R7): Kopia Wpisu, otwierana ikoną w wierszu raportu, otwiera okno z Projektem, Typem pracy, opisem i Czasem oryginału oraz datą ustawioną ostatnio w formularzu Nowy wpis (gdy ekranu nie otwierano od zalogowania, dzisiejszą); nic nie zapisuje się bez potwierdzenia; nieaktywny Projekt lub Typ pracy zostawia pole puste.
- R8 (parent R8): Lista wpisów filtruje po zakresie dat (domyślnie bieżący Miesiąc), Projekcie i Typie pracy (także nieaktywnych), sortuje od najnowszej daty i pokazuje liczbę Wpisów oraz sumę Czasu; z listy można edytować, kopiować i usuwać Wpis.
- R16 (parent R16): Każdy kolejny Wpis w Sesji wpisywania, od pustego formularza do zapisu, zajmuje nie więcej niż 10 s i da się go wykonać samą klawiaturą.
- R17 (parent R17): Każdy raport i lista Słownika ma filtr wartości w każdej kolumnie i licznik pozycji; brak eksportu CSV. Tu: Lista wpisów.
- R26 (parent R26): Każdy ekran ma stronę pomocy zgodną z jego działaniem. Tu: tematy `wpisy-nowy` i `wpisy`.

## Constraints

- prd NFR-4: przeglądarka na komputerze, interfejs tylko po polsku.
- prd NFR-5: Czas liczony w minutach całkowitych.
- prd NFR-7: „dzisiaj” w strefie Europe/Warsaw (`PolishCalendar.Today`).
- architecture: AD-4, AD-6, AD-8, AD-9, AD-10, AD-11, AD-12, AD-16 (`architecture-rejestr-czasu.md`).

## Done when

- Sesja wpisywania lokalnie: kilkanaście Wpisów z różnych dni, data domyślna dzisiejsza według Europe/Warsaw, formularz zostaje z tą samą datą po zapisie, każdy Wpis samą klawiaturą w nie więcej niż 10 s (R4, R5, R16).
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
- Decision (01.11.2026): Autor odkłada wdrożenie na Droplet na koniec, po dostarczeniu funkcji z epików 1–3 i 5: migracja `Initial` zostaje otwarta do pierwszego wdrożenia (NFR-9), bez kosztów i obsługi serwera, zanim aplikacja jest użyteczna. Wdrożenie, rollback i sprawdzenia na serwerze przeszły do nowego epic-wdrozenie-na-serwer (epik 6), przed Miesiącem „na serio”. Done when sprawdzany lokalnie; na serwerze powtarza go historia 6.3.
- Decision (01.11.2026): podział na 4 historie zatwierdzony przez Autora: 2.1 Nowy wpis i Sesja wpisywania (tracer bullet: `EntryForm`, zapis, klawiatura, `EntryDateMemory`), 2.2 Lista wpisów, 2.3 edycja, kopia i usuwanie w oknie dialogowym, 2.4 Refactor sweep. Najmniej pewny: wybór Projektu i Typu pracy z klawiatury w 10 s — komponent ustala historia 2.1.
