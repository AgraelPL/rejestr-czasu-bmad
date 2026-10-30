---
type: initiative
title: Rejestr czasu pracy
parent: none
sources:
  - _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/prd-rejestr-czasu.md
  - _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/addendum.md
companions:
  - _bmad-output/initiative-rejestr-czasu/glossary.md
  - _bmad-output/initiative-rejestr-czasu/export-layout.md
risk: medium
---

# Rejestr czasu pracy

## Description

Lokalna aplikacja webowa, do której Autor co kilka dni przenosi czas pracy z notatek w Sesji wpisywania, a na koniec Miesiąca jednym kliknięciem dostaje Zestawienie xlsx w układzie Odbiorcy, bez przepisywania do Excela i bez ręcznie liczonych sum. Przedsięwzięcie ma dwa równorzędne cele: narzędzie do prawdziwych rozliczeń od stycznia 2027 (najpóźniej lutego) i naukę metody BMad na pełnym cyklu. Szczegóły i uzasadnienia: PRD i brief.

## Outcome

Autor oddaje Odbiorcy miesięczne Zestawienie prosto z eksportu, przyjęte bez poprawek; sygnałem są Test wrześniowy i pierwszy Miesiąc „na serio” (prd SM-1, SM-2, SM-3).

## Requirements

- R1 (prd FR-1, FR-2): Autor dodaje i edytuje Projekty (kod unikalny bez względu na wielkość liter, zapisywany wielkimi literami; nazwa) i Typy pracy (nazwa unikalna bez względu na wielkość liter); zmiana obejmuje wszystkie Wpisy, także historyczne.
- R2 (prd FR-3): Projekt i Typ pracy można dezaktywować i przywrócić; nieaktywne znikają z list wyboru formularza Wpisu, a zostają w istniejących Wpisach (także przy ich edycji), filtrach Listy wpisów, Widoku miesiąca i Zestawieniu.
- R3 (prd FR-4): Usunięcie Projektu lub Typu pracy udaje się tylko, gdy nie występuje w żadnym Wpisie; inaczej Autor widzi komunikat z liczbą Wpisów i propozycją dezaktywacji.
- R4 (prd FR-5): Autor dodaje Wpis z datą, jednym Projektem, jednym Typem pracy, wymaganym opisem i Czasem; Czas spoza `0:15`–`24:00` lub niebędący wielokrotnością 15 minut jest odrzucany z komunikatem; dzień tygodnia jest wyliczany z daty; Suma dnia ponad 24 h nie jest blokowana.
- R5 (prd FR-5): W Sesji wpisywania pierwszy Wpis ma domyślnie dzisiejszą datę; po zapisie formularz zostaje otwarty z tą samą datą, a Projekt, Typ pracy, opis i Czas są wyczyszczone.
- R6 (prd FR-6): Autor edytuje każde pole Wpisu i usuwa Wpis po potwierdzeniu, także w poprzednich Miesiącach.
- R7 (prd FR-7): Kopia Wpisu otwiera formularz z Projektem, Typem pracy, opisem i Czasem oryginału oraz datą ustawioną w formularzu; nic nie zapisuje się bez potwierdzenia; nieaktywny Projekt lub Typ pracy zostawia pole puste.
- R8 (prd FR-8): Lista wpisów filtruje po zakresie dat (domyślnie bieżący Miesiąc), Projekcie i Typie pracy (także nieaktywnych), sortuje od najnowszej daty i pokazuje liczbę Wpisów oraz sumę Czasu; z listy można edytować, kopiować i usuwać Wpis.
- R9 (prd FR-9): Widok miesiąca pokazuje Wpisy wybranego Miesiąca (domyślnie bieżącego) dzień po dniu, tylko dni z Wpisami, w dniu według kodu Projektu, potem nazwy Typu pracy, z dniem tygodnia, Sumą dnia, Sumą tygodnia i sumą Miesiąca; edycja, kopia i usunięcie Wpisu przeliczają sumy od razu.
- R10 (prd FR-10): Suma dnia, Suma tygodnia i suma Miesiąca w Widoku miesiąca są identyczne z sumami w Zestawieniu za ten Miesiąc.
- R11 (prd FR-11): W Zestawieniu Wpisy z tego samego dnia o tym samym Projekcie i Typie pracy tworzą jeden Wiersz zestawienia: Czas się sumuje, opisy trafiają do kolejnych linii jednej komórki w kolejności dodania.
- R12 (prd FR-12): Jedno kliknięcie w Widoku miesiąca daje Zestawienie xlsx w układzie z `export-layout.md`; sumy są wartościami, nie formułami.
- R13 (prd FR-13): Plik Zestawienia nazywa się `<prefiks>_Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx` (prefiks z konfiguracji; bez prefiksu `Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx`); eksport Miesiąca bez Wpisów daje komunikat zamiast pliku.
- R14 (prd FR-14): Przy każdym uruchomieniu, zanim aplikacja przyjmie pierwsze żądanie, powstaje Kopia bazy `rejestr-RRRR-MM-DD_GGMMSS.sqlite` w osobnym katalogu; zostaje 10 najnowszych, najstarsza jest usuwana dopiero po udanym zapisie nowej; nieudana kopia zatrzymuje uruchomienie z czytelnym komunikatem.
- R15 (prd FR-15): Ścieżki bazy i katalogu kopii pochodzą z konfiguracji; domyślne leżą poza repozytorium i poza folderami synchronizowanymi z chmurą.
- R16 (prd NFR-3): Każdy kolejny Wpis w Sesji wpisywania, od pustego formularza do zapisu, zajmuje nie więcej niż 10 s i da się go wykonać samą klawiaturą.

## Constraints

- prd NFR-1: nasłuch tylko na `127.0.0.1`; zero połączeń wychodzących (telemetria, CDN, zewnętrzne czcionki), wszystkie zasoby lokalne; brak logowania i kont.
- prd NFR-2, SM-1: baza, kopie, źródłowy xlsx i plik konfiguracji z prefiksem poza gitem; testy, przykłady i dane startowe zanonimizowane; prawdziwe dane wrześniowe nigdy w repozytorium. Kody Projektów i sumy z września mogą być w dokumentach planistycznych.
- prd NFR-4: desktop, aktualna przeglądarka na komputerze Autora; interfejs tylko po polsku; uruchomienie komendą `npm start`, bez autostartu.
- prd NFR-5: Czas liczony w minutach całkowitych; konwersja na godziny dziesiętne dopiero w Zestawieniu.
- prd addendum: Node.js; baza SQLite w jednym pliku.
- prd SM-4, SM-C1: cały kod piszą agenci BMad; przy zderzeniu z terminem wygrywa metoda, a utknięcie agenta to materiał do notatki końcowej.
- prd §3: terminy z `glossary.md` obowiązują dosłownie.

## Done when

- Test wrześniowy przechodzi lokalnie do 28.12.2026: Sumy dnia, Sumy tygodnia 30 / 16 / 18 / 25 / 14 h i RAZEM 103 h zgodne z oryginałem (prd SM-1).
- Aplikacja działa na komputerze Autora (`npm start`) i w pierwszym Miesiącu „na serio” (styczeń 2027, najpóźniej luty) wszystkie Wpisy trafiają do niej przed końcem Miesiąca (prd SM-2).
- Zestawienie za ten Miesiąc trafia do Odbiorcy prosto z eksportu, bez poprawek w Excelu, i zostaje przyjęte (prd SM-3).
- Cały kod napisali agenci BMad, a po pierwszym Miesiącu „na serio” powstała notatka o tym, co w metodzie działało (prd SM-4).

## Boundaries

- Nie jest to narzędzie zespołowe ani usługa sieciowa: brak kont, logowania, dostępu z telefonu i innych urządzeń.
- Brak wykresów, raportów według Projektu, stawek i kwot, timera, importu (w tym z notatek), eksportu CSV, zamykania Miesiąca i przypomnień.
- Brak autostartu, synchronizacji i kopii poza dyskiem lokalnym.
- Aplikacja nie zastępuje notatek i nie rozwiązuje „szacowania po fakcie”.
- Ścieżka przez epiki: Słowniki → Wpis w Sesji wpisywania → Widok miesiąca z sumami → Zestawienie xlsx sprawdzone Testem wrześniowym.

## References

- prd — _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/prd-rejestr-czasu.md, §4 Funkcje, §5, §7
- prd addendum — _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/addendum.md
- brief — _bmad-output/initiative-rejestr-czasu/brief-rejestr-czasu/brief-rejestr-czasu.md, Ryzyka i otwarte sprawy
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md
- export layout — _bmad-output/initiative-rejestr-czasu/export-layout.md
