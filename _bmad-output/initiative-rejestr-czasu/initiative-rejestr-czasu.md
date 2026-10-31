---
type: initiative
title: Rejestr czasu pracy
parent: none
sources:
  - _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/prd-rejestr-czasu.md
  - _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/addendum.md
  - _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md
companions:
  - _bmad-output/initiative-rejestr-czasu/glossary.md
  - _bmad-output/initiative-rejestr-czasu/export-layout.md
  - _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md
risk: medium
---

# Rejestr czasu pracy

## Description

Lokalna aplikacja webowa, do której Autor co kilka dni przenosi czas pracy z notatek w Sesji wpisywania, a na koniec Miesiąca jednym kliknięciem dostaje Zestawienie xlsx w układzie Odbiorcy, bez przepisywania do Excela i bez ręcznie liczonych sum. Przedsięwzięcie ma dwa równorzędne cele: narzędzie do prawdziwych rozliczeń od stycznia 2027 (najpóźniej lutego) i naukę metody BMad na pełnym cyklu. Szczegóły i uzasadnienia: PRD i brief.

## Outcome

Autor oddaje Odbiorcy miesięczne Zestawienie prosto z eksportu, przyjęte bez poprawek; sygnałem są Test wrześniowy i pierwszy Miesiąc „na serio” (prd SM-1, SM-2, SM-3).

## Requirements

- R1 (prd FR-1, FR-2): User dodaje i edytuje własne Projekty (kod unikalny wśród Projektów Usera bez względu na wielkość liter, zapisywany wielkimi literami; nazwa) i Typy pracy (nazwa unikalna wśród Typów pracy Usera bez względu na wielkość liter); zmiana obejmuje wszystkie jego Wpisy, także historyczne; dwóch Userów może mieć Projekt o tym samym kodzie.
- R2 (prd FR-3): Projekt i Typ pracy można dezaktywować i przywrócić; nieaktywne znikają z list wyboru formularza Wpisu, a zostają w istniejących Wpisach (także przy ich edycji), filtrach Listy wpisów, Widoku miesiąca i Zestawieniu.
- R3 (prd FR-4): Usunięcie Projektu lub Typu pracy udaje się tylko, gdy nie występuje w żadnym Wpisie Usera; inaczej User widzi komunikat z liczbą Wpisów i propozycją dezaktywacji.
- R4 (prd FR-5): User dodaje Wpis z datą, jednym Projektem, jednym Typem pracy, wymaganym opisem i Czasem; Czas spoza `0:15`–`24:00` lub niebędący wielokrotnością 15 minut jest odrzucany z komunikatem; dzień tygodnia jest wyliczany z daty; Suma dnia ponad 24 h nie jest blokowana.
- R5 (prd FR-5, NFR-7): Przy każdym otwarciu ekranu Nowy wpis data domyślna to dzisiejsza (Europe/Warsaw); po zapisie formularz zostaje otwarty z tą samą datą, a Projekt, Typ pracy, opis i Czas są wyczyszczone.
- R6 (prd FR-6, architecture AD-10): User edytuje każde pole Wpisu w oknie dialogowym otwieranym ikoną w wierszu raportu i usuwa Wpis po potwierdzeniu, także w poprzednich Miesiącach; po zmianie raport pokazuje dane z bazy.
- R7 (prd FR-7, architecture AD-10): Kopia Wpisu, otwierana ikoną w wierszu raportu, otwiera okno z Projektem, Typem pracy, opisem i Czasem oryginału oraz datą ustawioną ostatnio w formularzu Nowy wpis (gdy ekranu nie otwierano od zalogowania, dzisiejszą); nic nie zapisuje się bez potwierdzenia; nieaktywny Projekt lub Typ pracy zostawia pole puste.
- R8 (prd FR-8): Lista wpisów filtruje po zakresie dat (domyślnie bieżący Miesiąc), Projekcie i Typie pracy (także nieaktywnych), sortuje od najnowszej daty i pokazuje liczbę Wpisów oraz sumę Czasu; z listy można edytować, kopiować i usuwać Wpis.
- R9 (prd FR-9, architecture AD-9): Widok miesiąca to siatka Wpisów wybranego Miesiąca (domyślnie bieżącego) grupowana Tydzień → Dzień, tylko dni z Wpisami, w dniu według kodu Projektu, potem nazwy Typu pracy, z dniem tygodnia, Sumą tygodnia i Sumą dnia w nagłówkach grup oraz sumą Miesiąca; sumy dotyczą całego Miesiąca także przy włączonym filtrze; edycja, kopia i usunięcie Wpisu przeliczają sumy od razu.
- R10 (prd FR-10): Suma dnia, Suma tygodnia i suma Miesiąca w Widoku miesiąca są identyczne z sumami w Zestawieniu za ten Miesiąc.
- R11 (prd FR-11): W Zestawieniu Wpisy z tego samego dnia o tym samym Projekcie i Typie pracy tworzą jeden Wiersz zestawienia: Czas się sumuje, opisy trafiają do kolejnych linii jednej komórki w kolejności dodania.
- R12 (prd FR-12, architecture AD-14): Zestawienie xlsx w układzie z `export-layout.md`, wspólnym dla wszystkich Userów, pobiera się jednym kliknięciem w Widoku miesiąca albo z ekranu Eksport po wyborze Miesiąca; sumy są wartościami, nie formułami.
- R13 (prd FR-13, FR-24): Plik Zestawienia nazywa się `<prefiks>_Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx` (prefiks z Ustawień konta Usera; bez prefiksu `Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx`); eksport Miesiąca bez Wpisów daje komunikat zamiast pliku.
- R14 (prd FR-14): wycofane 31.10.2026 — Kopia bazy przy starcie usunięta z PRD; kopie zapewnia Kopia serwera dostawcy, aplikacja nie ma kodu kopii.
- R15 (prd FR-15): Parametry połączenia z bazą i sekrety pochodzą z konfiguracji poza gitem, na serwerze i w potoku wdrożenia; lokalnie dane bazy leżą poza repozytorium i poza folderami synchronizowanymi z chmurą.
- R16 (prd NFR-3): Każdy kolejny Wpis w Sesji wpisywania, od pustego formularza do zapisu, zajmuje nie więcej niż 10 s przy pracy przez internet i da się go wykonać samą klawiaturą.
- R17 (prd FR-16, architecture AD-9): Każdy raport (Widok miesiąca, Lista wpisów) i lista Słownika ma filtr wartości w każdej kolumnie i licznik pozycji; filtr nie zmienia sum liczonych dla Miesiąca; brak eksportu CSV.
- R18 (prd FR-17): Każdy zakłada Konto loginem i hasłem, bez e-maila; login jest unikalny bez względu na wielkość liter; formularz mówi jednym zdaniem, że Administrator ma przez bazę dostęp do wszystkich danych; nowe Konto jest w stanie Oczekuje i nie loguje się do zatwierdzenia.
- R19 (prd FR-18): Zalogować się może tylko Konto Aktywne; Konto Oczekuje lub Zablokowane dostaje komunikat o stanie; po 5 nieudanych próbach z rzędu logowanie na Konto jest wstrzymane na 15 minut; User wylogowuje się z belki górnej.
- R20 (prd FR-19): Pierwsze Konto Administratora powstaje przy wdrożeniu z sekretu w konfiguracji serwera, nie z formularza; Konto Autora ma role User i Administrator.
- R21 (prd FR-20): Ekran Użytkownicy pokazuje Administratorowi login, datę rejestracji, ostatnie logowanie, stan i liczbę Wpisów każdego Konta i pozwala zatwierdzić, zablokować, odblokować, ustawić hasło i usunąć Konto; odrzucenie Konta Oczekuje usuwa je od razu; zablokowanie działa od razu; Administrator nie blokuje, nie odrzuca i nie usuwa własnego Konta.
- R22 (prd FR-21): Administrator ustawia hasło wybranemu Kontu; pierwsze logowanie tym hasłem wymusza ustawienie własnego hasła, zanim aplikacja pokaże cokolwiek innego.
- R23 (prd FR-22): Administrator usuwa Konto Zablokowane po wpisaniu jego loginu; usunięcie obejmuje Słowniki, Wpisy i Ustawienia konta w jednej operacji: wszystko albo nic.
- R24 (prd FR-23, SM-5): User widzi i zmienia wyłącznie własne Słowniki i Wpisy, a Zestawienie zawiera tylko jego Wpisy; jedynym wyjątkiem jest liczba Wpisów na ekranie Użytkownicy; sprawdzają to próba na serwerze na dwóch Kontach i testy automatyczne w potoku.
- R25 (prd FR-24): W Ustawieniach konta User ustawia prefiks pliku Zestawienia i zmienia własne hasło, podając obecne.
- R26 (prd FR-25): Aplikacja ma strony pomocy po polsku; ikona `?` w belce otwiera stronę bieżącego ekranu, a z niej spis tematów; strona o administracji jest tylko dla Administratora; każdy ekran ma stronę pomocy zgodną z jego działaniem.
- R27 (prd FR-26): Repozytorium zawiera instrukcję administratora w Markdownie, dostępną, gdy aplikacja nie działa, z tematami z addendum PRD (serwer, domena, sekrety, dostęp do bazy, wdrożenie, rollback, odtworzenie z Kopii serwera).
- R28 (prd FR-27): Każda propozycja zmiany do gałęzi głównej przechodzi automatycznie budowanie i wszystkie testy; zmiana z błędem nie może zostać włączona.
- R29 (prd FR-28, SM-4): Włączenie zmiany do gałęzi głównej wdraża ją na serwer bez ręcznych kroków; po wdrożeniu aplikacja sprawdza swój stan, a nieudane sprawdzenie kończy wdrożenie błędem widocznym dla Autora, bez automatycznego powrotu do poprzedniej wersji.
- R30 (prd FR-29, SM-4): Autor jednym poleceniem przywraca na serwerze wskazaną wcześniejszą wersję aplikacji; rollback jest przećwiczony co najmniej raz.
- R31 (prd NFR-10): Gdy połączenie z serwerem się zerwie, aplikacja pokazuje komunikat z prośbą o odświeżenie; niezapisany Wpis może przepaść.

## Constraints

- prd NFR-1: aplikacja nie nawiązuje połączeń wychodzących (telemetria, CDN, zewnętrzne czcionki); zasoby serwuje sama, a CSP blokuje inne źródła; ruch tylko przez HTTPS; połączenia infrastruktury (certyfikaty, rejestr obrazów) dozwolone.
- prd NFR-2, SM-1: repozytorium publiczne; baza, źródłowy xlsx, sekrety i konfiguracja z hasłami nie trafiają do gita, obrazu aplikacji ani logów potoku; testy, przykłady i dane startowe zanonimizowane; prawdziwe dane wrześniowe ani w repozytorium, ani na serwerze. Kody Projektów i sumy z września mogą być w dokumentach planistycznych.
- prd NFR-4: aktualna przeglądarka na komputerze, bez układu pod telefon; interfejs tylko po polsku; dostęp pod subdomeną Autora; środowiskiem testowym jest lokalne uruchomienie, bez środowiska przedprodukcyjnego.
- prd NFR-5: Czas liczony w minutach całkowitych; konwersja na godziny dziesiętne dopiero w Zestawieniu.
- prd NFR-6: belka górna z Pomocą `?`, Ustawieniami konta i Wyloguj; menu: Nowy wpis; Rejestr (Widok miesiąca, Lista wpisów, Eksport); Słowniki (Projekty, Typy pracy); Administracja (Użytkownicy) tylko dla Administratora.
- prd NFR-7: „dzisiaj” i wszystkie daty w strefie Europe/Warsaw, niezależnie od zegara serwera.
- prd NFR-8: limity prób rejestracji i logowania; baza niedostępna z internetu; dostęp administracyjny do serwera i bazy tylko szyfrowanym kanałem z kluczem, do bazy na co dzień tylko do odczytu; progi, reguły haseł i czas sesji to bezpieczne wartości domyślne z architektury.
- prd NFR-9, addendum: migracja `Initial` zostaje przepisana z `UserId` przed pierwszym wdrożeniem na serwer; od tego wdrożenia zmiany schematu tylko dodają i są stosowane przy starcie.
- architecture AD-1, prd addendum: .NET 10, Blazor Web App Interactive Server, Fluent UI Blazor 5, EF Core 10, PostgreSQL 18 w Dockerze; lokalnie `docker compose up -d`, potem `dotnet run`; serwer, CI/CD i kopie według addendum PRD.
- architecture: kręgosłup `architecture-rejestr-czasu.md` (AD-1…AD-14) wiąże każdy epik, poza AD-2 i AD-3, które PRD z 31.10.2026 zastępuje do czasu aktualizacji architektury.
- prd SM-4, SM-C1: cały kod piszą agenci BMad, a zmiany na serwerze idą tylko przez potok wdrożenia (wyjątek: jednorazowe przygotowanie serwera i ćwiczenia z instrukcji administratora); przy zderzeniu z terminem wygrywa metoda.
- prd SM-C2: nic poza zakresem ustalonym 31.10.2026 nie wchodzi przed Testem wrześniowym.
- prd §3: terminy z `glossary.md` obowiązują dosłownie.

## Done when

- Test wrześniowy przechodzi lokalnie do 28.12.2026: Sumy dnia, Sumy tygodnia 30 / 16 / 18 / 25 / 14 h i RAZEM 103 h zgodne z oryginałem (prd SM-1).
- Aplikacja działa na komputerze Autora (`docker compose up -d`, `dotnet run`) i w pierwszym Miesiącu „na serio” (styczeń 2027, najpóźniej luty) wszystkie Wpisy trafiają do niej przed końcem Miesiąca (prd SM-2).
- Zestawienie za ten Miesiąc trafia do Odbiorcy prosto z eksportu, bez poprawek w Excelu, i zostaje przyjęte (prd SM-3).
- Cały kod napisali agenci BMad, a po pierwszym Miesiącu „na serio” powstała notatka o tym, co w metodzie działało (prd SM-4).

## Boundaries

- Nie jest to narzędzie zespołowe ani usługa sieciowa: brak kont, logowania, dostępu z telefonu i innych urządzeń.
- Brak wykresów, raportów według Projektu, stawek i kwot, timera, importu (w tym z notatek), eksportu CSV, zamykania Miesiąca i przypomnień.
- Brak autostartu, synchronizacji i kopii poza dyskiem lokalnym.
- Aplikacja nie zastępuje notatek i nie rozwiązuje „szacowania po fakcie”.
- Touch point: Docker Desktop i obraz `postgres:18` — instaluje i uruchamia Autor; compose, wolumen i `docker exec pg_dump`; owner: epic-szkielet-i-slowniki
- Touch point: katalog Kopii bazy na dysku lokalnym — wskazuje Autor w konfiguracji; owner: epic-szkielet-i-slowniki
- Touch point: Odbiorca — konsumuje Zestawienie w układzie z `export-layout.md`; owner: epic-miesiac-i-zestawienie
- Touch point: źródłowy xlsx wrześniowy — tylko lokalnie, poza gitem, do Testu wrześniowego; owner: epic-miesiac-i-zestawienie
- Touch point: notatki Autora — źródło ręczne, bez importu
- Ścieżka przez epiki: Szkielet (start, Kopia bazy, układ) → Słowniki → Wpis w Sesji wpisywania → Widok miesiąca z sumami → Zestawienie xlsx sprawdzone Testem wrześniowym.

## References

- prd — _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/prd-rejestr-czasu.md, §4 Funkcje, §5, §6.1, §7 (wersja 31.10.2026: Konta, wdrożenie, pomoc)
- prd addendum — _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/addendum.md
- brief — _bmad-output/initiative-rejestr-czasu/brief-rejestr-czasu/brief-rejestr-czasu.md, Ryzyka i otwarte sprawy
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md
- export layout — _bmad-output/initiative-rejestr-czasu/export-layout.md
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, Invariants & Rules, Capability → Architecture Map

## Notes

- Decision (30.10.2026): konflikt PRD (Node.js, SQLite, `npm start`, `.sqlite`) z architekturą AD-1–AD-3 rozwiązany aktualizacją PRD; PRD ma też FR-16 (filtry w siatce) i NFR-6 (układ i komunikaty).
- Decision (30.10.2026): podział na 3 epiki według zasady „jedna zdolność dostarczana w całości, szkielet otwiera”: szkielet i Słowniki → Wpisy i Lista wpisów → Widok miesiąca i Zestawienie.
- Decision (30.10.2026): `ReportGrid` w pełnym zakresie AD-9 i typy domeny powstają w epiku 1 („najpierw mechanizm, później zastosowanie”).
- Decision (30.10.2026): SM-2 i SM-4 zostają kryteriami inicjatywy; epik 3 kończy się Testem wrześniowym.
