---
title: "PRD: Rejestr czasu pracy"
status: final
created: 2026-10-30
updated: 2026-10-31
---

# PRD: Rejestr czasu pracy

## 0. Cel dokumentu

PRD dla Autora i dla agentów BMad, którzy z niego zbudują architekturę, epiki i historie. Rozwija brief [`brief-rejestr-czasu.md`](../brief-rejestr-czasu/brief-rejestr-czasu.md) i jego addendum, nie powtarza ich uzasadnień. Terminy z §3 Pojęcia obowiązują dosłownie. Wymagania są pogrupowane w funkcje, numerowane globalnie (FR-1…); numery są stałe i nie muszą iść w kolejności sekcji, usunięte wymagania zostają oznaczone. Wersja z 31.10.2026 wprowadza zakres z [`forge-chmura-konta-wdrozenie.md`](../forge-chmura-konta-wdrozenie/forge-chmura-konta-wdrozenie.md): Konta, role, wdrożenie na serwer i pomoc. Decyzje techniczne są w [`addendum.md`](addendum.md).

## 1. Wizja

Autor zapisuje czas pracy w notatkach, a co kilka dni przenosi je do aplikacji w jednej Sesji wpisywania. Formularz nie pozwala popełnić błędów znanych z września 2026: złej daty, literówki w kodzie Projektu, dwóch Projektów w jednym Wpisie, ręcznie liczonych sum. Na koniec Miesiąca nie ma przepisywania do Excela: jedno kliknięcie daje Zestawienie xlsx w układzie, którego oczekuje Odbiorca, gotowe do wysłania bez otwierania Excela.

Aplikacja działa na serwerze w chmurze i jest dostępna przez przeglądarkę pod własnym adresem dla kilku osób: Autora i znajomego. Każdy User widzi wyłącznie własne dane.

Przedsięwzięcie ma dwa cele o równej wadze: narzędzie, w którym Autor od stycznia 2027 (najpóźniej od lutego) prowadzi rozliczenia, i naukę metody BMad na prawdziwym, małym produkcie, teraz razem z ciągłą integracją i wdrażaniem.

## 2. Użytkownicy

- **Autor** — prowadzi własne rozliczenia (role User i Administrator).
- **Znajomy** — drugie Konto, zaproszone linkiem przez Autora. Nie ma osobnej ścieżki: korzysta z tych samych funkcji co każdy User, a jego Konto pokazuje, że rejestracja, zatwierdzanie i oddzielenie danych działają.

### 2.1 Zadania do wykonania

- Co kilka dni przenieść notatki do aplikacji szybko, w jednej Sesji wpisywania, bez błędów przepisywania.
- Na koniec Miesiąca pobrać Zestawienie i wysłać je Odbiorcy e-mailem bez przepisywania i bez ręcznej kontroli sum.
- Odpowiedzieć sobie na pytania w rodzaju „ile godzin RDZ w I kwartale”.
- Jako Administrator: przyjąć nowe Konto, pomóc przy zapomnianym haśle, usunąć Konto.
- Przejść cały cykl BMad na prawdziwym produkcie, w tym CI/CD i wdrożenie (cel równorzędny z narzędziem).

### 2.2 Ścieżki użytkownika

- **UJ-1. Sesja wpisywania.** Autor otwiera adres aplikacji w przeglądarce, loguje się i przechodzi do ekranu Nowy wpis; obok ma notatki z ostatnich 3–4 dni. Po kolei wpisuje 6–12 Wpisów: ustawia datę pierwszego dnia, wybiera Projekt i Typ pracy, wpisuje opis i Czas, zapisuje; formularz zostaje z tą samą datą na kolejny Wpis, a przy następnym dniu Autor zmienia tylko datę.
- **UJ-2. Zamknięcie miesiąca.** Pierwszego dnia nowego miesiąca Autor otwiera Widok miesiąca za poprzedni Miesiąc, sprawdza Sumy dnia i Sumy tygodnia, uzupełnia brakujący Wpis, klika eksport i wysyła pobrane Zestawienie Odbiorcy e-mailem.
- **UJ-3. Pytanie o godziny.** Autor otwiera Listę wpisów, ustawia zakres 01.01–31.03 i Projekt RDZ, odczytuje sumę pod listą.
- **UJ-4. Nowe Konto.** Autor wysyła znajomemu link i opowiada o projekcie. Znajomy zakłada Konto (login, hasło), czyta zdanie o dostępie Administratora do bazy i widzi, że Konto jest w stanie Oczekuje. Autor na ekranie Użytkownicy zatwierdza Konto. Znajomy loguje się i zaczyna od pustych Słowników.
- **UJ-5. Zapomniane hasło.** User zgłasza się do Administratora poza aplikacją. Administrator ustawia mu hasło na ekranie Użytkownicy i przekazuje je Userowi. Przy logowaniu tym hasłem aplikacja każe Userowi ustawić własne.

## 3. Pojęcia

- **Autor** — twórca aplikacji; ma Konto z rolami User i Administrator.
- **User** — rola: prowadzi własne Słowniki, Wpisy i Zestawienia.
- **Administrator** — rola: zatwierdza, blokuje i usuwa Konta, ustawia hasła; nie widzi w aplikacji treści danych innych Userów (wyjątek: logowanie hasłem, które sam ustawił, FR-21 i §6.1).
- **Konto** — login i hasło jednej osoby, bez e-maila. Ma stan: **Oczekuje** (po rejestracji, do zatwierdzenia), **Aktywne**, **Zablokowane**.
- **Odbiorca** — osoba, której User wysyła Zestawienie e-mailem; nie korzysta z aplikacji.
- **Projekt** — pozycja Słownika: kod (np. `RDZ`, unikalny) i nazwa. Ma flagę aktywności.
- **Typ pracy** — pozycja Słownika: nazwa (np. `Development`, `Analiza bis-sys`). Ma flagę aktywności. Niezależny od Projektu.
- **Słownik** — lista Projektów albo lista Typów pracy prowadzona przez Usera; każdy User ma własne Słowniki.
- **Sesja wpisywania** — jedno posiedzenie (zwykle 1–2 h co kilka dni), w którym User przenosi notatki z kilku dni do Wpisów.
- **Wpis** — jeden zarejestrowany odcinek pracy: data, dokładnie jeden Projekt, dokładnie jeden Typ pracy, opis, czas.
- **Czas** — długość Wpisu w formacie `h:mm`, wielokrotność 15 minut, od `0:15` do `24:00`. W Zestawieniu wyrażany jako godziny dziesiętne (`1:30` → 1,5).
- **Miesiąc** — miesiąc kalendarzowy; jednostka Widoku miesiąca i Zestawienia.
- **Suma dnia** — suma Czasu Wpisów z jednego dnia.
- **Suma tygodnia** — suma Czasu Wpisów z dni jednego tygodnia (pn–nd) **należących do danego Miesiąca**; tydzień na przełomie miesięcy liczy się w każdym Miesiącu osobno.
- **Widok miesiąca** — ekran z Wpisami Miesiąca dzień po dniu, z Sumą dnia, Sumą tygodnia i sumą Miesiąca.
- **Lista wpisów** — ekran wyszukiwania Wpisów z filtrami i sumą.
- **Zestawienie** — plik xlsx za jeden Miesiąc w układzie Odbiorcy (FR-12).
- **Wiersz zestawienia** — wiersz Zestawienia powstały ze złączenia Wpisów z tego samego dnia o tym samym Projekcie i Typie pracy.
- **Kopia serwera** — kopia całego serwera z aplikacją i bazą, robiona automatycznie przez dostawcę chmury.
- **Test wrześniowy** — wpisanie 28 Wpisów z września 2026 i porównanie Zestawienia z oryginałem (SM-1).

## 4. Funkcje

Wszystkie funkcje z §4.1–4.5 dotyczą danych zalogowanego Usera (FR-23).

### 4.1 Słowniki

**Opis:** User sam prowadzi oba Słowniki; na nowym Koncie są puste i User wypełnia je ręcznie (Autor: 7 Projektów, 2 Typy pracy). Nieaktywne pozycje znikają z list wyboru, ale zostają we Wpisach, Widoku miesiąca, Liście wpisów (jako filtr) i Zestawieniu. Siatka i filtry: FR-16.

#### FR-1: Dodanie i edycja Projektu
User może dodać Projekt (kod, nazwa) i zmienić jego kod lub nazwę.
- Kod jest wymagany i unikalny wśród Projektów Usera bez względu na wielkość liter; `RDZ` i `rdz` to ten sam kod. Kod jest zapisywany wielkimi literami. Dwóch Userów może mieć Projekt o tym samym kodzie.
- Zmiana kodu lub nazwy jest widoczna we wszystkich Wpisach Usera, także historycznych, i w każdym późniejszym Zestawieniu. Świadoma konsekwencja: ponowny eksport Miesiąca już oddanego Odbiorcy po zmianie kodu da plik inny niż wysłany.

#### FR-2: Dodanie i edycja Typu pracy
User może dodać Typ pracy (nazwa) i zmienić jego nazwę. Nazwa jest wymagana i unikalna wśród Typów pracy Usera bez względu na wielkość liter. Zmiana obejmuje Wpisy historyczne.

#### FR-3: Aktywność pozycji Słownika
User może oznaczyć Projekt lub Typ pracy jako nieaktywny i z powrotem aktywny.
- Nieaktywna pozycja nie pojawia się na listach wyboru w formularzu Wpisu.
- Edycja istniejącego Wpisu z nieaktywną pozycją zachowuje ją.

#### FR-4: Usunięcie pozycji Słownika
User może usunąć Projekt lub Typ pracy, który nie występuje w żadnym jego Wpisie. Próba usunięcia użytej pozycji kończy się komunikatem z liczbą Wpisów, które jej używają, i propozycją dezaktywacji.

### 4.2 Wpisy

**Opis:** Formularz Wpisu to najczęściej używany ekran; ma działać szybko z klawiatury (NFR-3). Realizuje UJ-1.

#### FR-5: Dodanie Wpisu
User może dodać Wpis na osobnym ekranie Nowy wpis (przycisk na górze menu): data (z kalendarza), Projekt i Typ pracy (z list aktywnych pozycji), opis, Czas.
- Przy każdym otwarciu ekranu Nowy wpis data domyślna to dzisiejsza (NFR-7). Po zapisie formularz zostaje otwarty na kolejny Wpis z tą samą datą; Projekt, Typ pracy, opis i Czas są czyszczone.
- Dzień tygodnia jest wyliczany z daty i pokazywany przy niej; nie da się go wpisać.
- Czas spoza zakresu `0:15`–`24:00` lub niebędący wielokrotnością 15 minut jest odrzucany z komunikatem.
- Opis jest wymagany.
- Wpis ma dokładnie jeden Projekt; formularz nie pozwala wybrać dwóch.
- Suma dnia powyżej 24 h nie jest blokowana.

#### FR-6: Edycja i usunięcie Wpisu
User może zmienić każde pole Wpisu w oknie dialogowym otwieranym ikoną w wierszu raportu (Widok miesiąca, Lista wpisów) i usunąć Wpis ikoną w wierszu (z potwierdzeniem). Po zmianie raport pokazuje dane z bazy. Dotyczy też Wpisów z poprzednich Miesięcy; v1 nie ma zamykania Miesiąca.

#### FR-7: Kopiowanie Wpisu
User może skopiować Wpis ikoną w wierszu raportu: otwiera się okno dialogowe z formularzem nowego Wpisu z Projektem, Typem pracy, opisem i Czasem oryginału oraz z datą ustawioną ostatnio w formularzu Nowy wpis (jeśli ekran nie był otwierany od zalogowania, dzisiejszą). Nic nie zapisuje się bez potwierdzenia Usera. Jeśli Projekt lub Typ pracy oryginału jest nieaktywny, odpowiednie pole pozostaje puste do ponownego wyboru.

### 4.3 Lista wpisów

**Opis:** Realizuje UJ-3.

#### FR-8: Filtrowanie i suma
User może przeglądać Wpisy z filtrami: zakres dat (domyślnie bieżący Miesiąc), Projekt, Typ pracy (oba także nieaktywne).
- Lista jest posortowana od najnowszej daty.
- Pod listą widać liczbę Wpisów i sumę Czasu dla bieżących filtrów.
- Z listy można edytować, kopiować i usuwać Wpis.

#### FR-16: Filtry w siatce raportów
Każdy raport (Widok miesiąca, Lista wpisów) i lista Słownika jest siatką z filtrem wartości w każdej kolumnie (zaznaczanie wartości) i licznikiem pozycji.
- Filtr ukrywa wiersze; nie zmienia Sum dnia, Sum tygodnia ani sumy Miesiąca w Widoku miesiąca.
- Siatka nie ma eksportu CSV. Grupowanie sterowane przez Usera, sortowanie po kilku kolumnach i wybór kolumn są poza v1.

### 4.4 Widok miesiąca

**Opis:** Kontrola przed eksportem, odpowiednik dotychczasowego arkusza. Realizuje UJ-2. Siatka i filtry: FR-16.

#### FR-9: Widok miesiąca
User może wybrać Miesiąc (domyślnie bieżący) i zobaczyć jego Wpisy w siatce grupowanej Tydzień → Dzień, chronologicznie, z dniem tygodnia, Sumą tygodnia i Sumą dnia w nagłówkach grup oraz sumą Miesiąca.
- Sumy dotyczą całego Miesiąca, także gdy filtr (FR-16) ukrywa część wierszy.
- W obrębie dnia Wpisy są ułożone jak Wiersze zestawienia (FR-11): według kodu Projektu, potem nazwy Typu pracy.
- Suma tygodnia obejmuje tylko dni danego Miesiąca.
- Dni bez Wpisów są pomijane.
- Z widoku można edytować, kopiować i usuwać Wpis; sumy przeliczają się od razu.

#### FR-10: Zgodność sum z Zestawieniem
Sumy dnia, Sumy tygodnia i suma Miesiąca w Widoku miesiąca są identyczne z odpowiednimi sumami w Zestawieniu za ten Miesiąc.

### 4.5 Zestawienie

**Opis:** Jedno kliknięcie w Widoku miesiąca daje Zestawienie za wybrany Miesiąc; to samo daje ekran Eksport (pozycja w menu) po wyborze Miesiąca. Realizuje UJ-2. Układ jest jeden, wspólny dla wszystkich Userów.

#### FR-11: Złączenie Wpisów w Wiersze zestawienia
Wpisy z tego samego dnia o tym samym Projekcie i Typie pracy tworzą jeden Wiersz zestawienia: Czas się sumuje, opisy trafiają do kolejnych linii tej samej komórki w kolejności dodania Wpisów. Wiersze zestawienia jednego dnia są ułożone według kodu Projektu, potem nazwy Typu pracy.

#### FR-12: Układ Zestawienia
Plik ma jeden arkusz o nazwie „<Miesiąc RRRR>” w mianowniku (np. „Wrzesień 2026”), zamrożony wiersz nagłówka, jeden Wiersz zestawienia na wiersz arkusza, dni chronologicznie, tylko dni z Wpisami.

| Kolumna (nagłówek) | Zawartość |
|---|---|
| A „Dzień tygodnia” | np. „Środa”; tylko w pierwszym wierszu dnia |
| B „Data” | prawdziwa data Excela w formacie `dd.mm.rrrr`; tylko w pierwszym wierszu dnia |
| C „Kod” | kod Projektu |
| D „Typ” | nazwa Typu pracy |
| E „Opis” | opisy złączonych Wpisów, każdy w nowej linii; zawijanie tekstu włączone |
| F „Godziny” | Czas w godzinach dziesiętnych, format ogólny (4; 1,5; 0,75) |
| G „Suma dnia” | tylko w pierwszym wierszu dnia |
| H „Suma tygodnia” | tylko w pierwszym wierszu tygodnia (pierwszy dzień tygodnia z Wpisami w danym Miesiącu); tydzień bez Wpisów w danym Miesiącu nie ma wiersza ani sumy |

Wszystkie sumy aplikacja liczy sama i zapisuje jako wartości, nie formuły.

Po ostatnim Wierszu zestawienia jest jeden pusty wiersz, a pod nim pogrubiony wiersz RAZEM: w E tekst „RAZEM <MIESIĄC RRRR>” wielkimi literami (np. „RAZEM WRZESIEŃ 2026”), w F suma kolumny F, w H suma Sum tygodnia; obie równe sumie Miesiąca.

#### FR-13: Nazwa i zapis pliku
Plik nazywa się `<prefiks>_Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx` (np. `…_Zestawienie-wrzesień_2026.xlsx`); prefiks User ustawia w Ustawieniach konta (FR-24). Bez prefiksu nazwa to `Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx`. Przeglądarka pobiera plik standardowo. Eksport Miesiąca bez Wpisów kończy się komunikatem zamiast pustego pliku.

### 4.6 Konta i role

**Opis:** Realizuje UJ-4 i UJ-5. Ekrany Administratora pokazują dane o Kontach, nigdy treść Słowników ani Wpisów. Administrator może wejść w cudze dane tylko hasłem ustawionym w FR-21, zanim User je zmieni (ryzyko w §6.1).

#### FR-17: Rejestracja
Każdy może założyć Konto z adresu aplikacji: login i hasło, bez e-maila.
- Login jest wymagany i unikalny bez względu na wielkość liter.
- Formularz pokazuje jednym zdaniem, że Administrator ma przez bazę dostęp do wszystkich danych.
- Nowe Konto ma stan Oczekuje i nie pozwala się zalogować do czasu zatwierdzenia.

#### FR-18: Logowanie i wylogowanie
Zalogować się może tylko Konto w stanie Aktywne; Konto w stanie Oczekuje lub Zablokowane dostaje komunikat o swoim stanie.
- Po 5 nieudanych próbach z rzędu logowanie na to Konto jest wstrzymane na 15 minut.
- User może się wylogować z belki górnej.

#### FR-19: Pierwszy Administrator
Pierwsze Konto Administratora powstaje przy wdrożeniu z sekretu w konfiguracji serwera, nie z formularza rejestracji. Konto Autora ma role User i Administrator.

#### FR-20: Ekran Użytkownicy
Administrator widzi listę Kont: login, data rejestracji, ostatnie logowanie, stan, liczba Wpisów. Z listy może zatwierdzić Konto, zablokować je i odblokować, ustawić hasło (FR-21) i usunąć Konto (FR-22).
- Konto w stanie Oczekuje Administrator może odrzucić: odrzucenie usuwa je od razu, po zwykłym potwierdzeniu, bez blokowania.
- Zablokowanie działa od razu: zalogowany User traci dostęp najpóźniej przy następnej akcji.
- Administrator nie może zablokować, odrzucić ani usunąć własnego Konta.

#### FR-21: Hasło ustawione przez Administratora
Administrator może ustawić hasło wybranemu Kontu i przekazać je Userowi poza aplikacją. Przy pierwszym logowaniu takim hasłem aplikacja wymusza ustawienie własnego hasła, zanim pokaże cokolwiek innego.

#### FR-22: Usunięcie Konta
Administrator może usunąć Konto w stanie Zablokowane, po potwierdzeniu wpisaniem jego loginu; Konto w stanie Oczekuje usuwa odrzucenie (FR-20). Usunięcie usuwa wszystkie Słowniki, Wpisy i Ustawienia konta tego Konta w jednej operacji: albo wszystko, albo nic.

#### FR-23: Oddzielenie danych
Każdy User widzi i zmienia wyłącznie własne Słowniki i Wpisy, a Zestawienie zawiera wyłącznie jego Wpisy. Jedynym wyjątkiem jest liczba Wpisów na ekranie Użytkownicy (FR-20).

#### FR-24: Ustawienia konta
User ustawia w Ustawieniach konta prefiks pliku Zestawienia (FR-13) i zmienia własne hasło (z podaniem obecnego).

### 4.7 Pomoc i dokumentacja

**Opis:** Pomoc dla Userów w aplikacji i instrukcja dla Administratora poza nią.

#### FR-25: Pomoc w aplikacji
Aplikacja ma strony pomocy po polsku. Ikona `?` w belce górnej otwiera stronę bieżącego ekranu, a z niej spis tematów. Strona o administracji jest dostępna tylko dla Administratora. Każdy ekran ma stronę pomocy zgodną z jego aktualnym działaniem.

#### FR-26: Instrukcja administratora
Repozytorium zawiera instrukcję administratora w Markdownie, dostępną także wtedy, gdy aplikacja nie działa. Opisuje przygotowanie serwera i domeny, sekrety, dostęp do bazy, wdrożenie, rollback i odtworzenie z Kopii serwera (lista tematów: addendum, punkt `docs/admin/`).

### 4.8 Wdrożenie

**Opis:** Wdrożenie jest jednocześnie funkcją i przedmiotem nauki (SM-4). Mechanizm: addendum, „Wdrożenie i infrastruktura”.

#### FR-27: Sprawdzenie zmian przed włączeniem
Każda propozycja zmiany do gałęzi głównej przechodzi automatycznie budowanie i wszystkie testy; zmiana z błędem nie może zostać włączona.

#### FR-28: Wdrożenie po włączeniu zmiany
Włączenie zmiany do gałęzi głównej wdraża ją na serwer bez ręcznych kroków. Po wdrożeniu aplikacja sprawdza swój stan; nieudane sprawdzenie kończy wdrożenie błędem widocznym dla Autora. Aplikacja nie wraca sama do poprzedniej wersji; Autor decyduje o rollbacku (FR-29).

#### FR-29: Rollback
Autor może jednym poleceniem przywrócić na serwerze wskazaną wcześniejszą wersję aplikacji.

### 4.9 Dane

**Opis:** Gdzie leżą dane i konfiguracja; kopie zapewnia Kopia serwera (§6.1).

#### FR-14: Kopia bazy (wymaganie usunięte 31.10.2026)
Zastąpione Kopią serwera (§6.1); aplikacja nie zawiera kodu kopii.

#### FR-15: Konfiguracja i lokalizacja danych
Parametry połączenia z bazą i sekrety pochodzą z konfiguracji poza gitem, na serwerze i w potoku wdrożenia. W środowisku lokalnym (Test wrześniowy, praca nad kodem) dane bazy leżą poza repozytorium i poza folderami synchronizowanymi z chmurą (na Windowsie `Documents` bywa przekierowany do OneDrive, a repozytorium leży w `Documents`).

## 5. Wymagania niefunkcjonalne

- **NFR-1 Brak połączeń wychodzących:** aplikacja nie nawiązuje połączeń wychodzących (brak telemetrii, CDN, zewnętrznych czcionek); wszystkie zasoby są serwowane z aplikacji, a polityka CSP blokuje ładowanie zasobów z innych źródeł. Ruch z przeglądarki idzie wyłącznie przez HTTPS. Połączenia samej infrastruktury (certyfikaty, rejestr obrazów) nie są objęte zakazem.
- **NFR-2 Prywatność repozytorium:** repozytorium jest publiczne. Baza, źródłowy xlsx, sekrety i pliki konfiguracji z hasłami nie trafiają do gita, do obrazu aplikacji ani do logów potoku. Testy, przykłady i dane startowe w repozytorium są zanonimizowane. Kody Projektów i sumy z września mogą zostać w dokumentach planistycznych (decyzja Autora, 30.10.2026).
- **NFR-3 Szybkość wpisu:** każdy kolejny Wpis w Sesji wpisywania, od pustego formularza do zapisu, zajmuje kilka sekund, nie więcej niż 10, przy pracy przez internet; całość da się wykonać samą klawiaturą.
- **NFR-4 Platforma:** aktualna przeglądarka na komputerze; interfejs tylko po polsku; aplikacja nie jest projektowana pod telefon (adres zadziała, układ nie jest dopasowany). Dostęp pod subdomeną Autora. Środowiskiem testowym jest lokalne uruchomienie; nie ma osobnego środowiska przedprodukcyjnego.
- **NFR-5 Arytmetyka:** Czas jest liczony w minutach całkowitych; zaokrąglenia nie występują aż do konwersji na godziny dziesiętne w Zestawieniu.
- **NFR-6 Układ i komunikaty:** belka górna (Pomoc `?`, Ustawienia konta, Wyloguj), menu boczne (przycisk Nowy wpis; sekcja Rejestr: Widok miesiąca, Lista wpisów, Eksport; sekcja Słowniki: Projekty, Typy pracy; sekcja Administracja, tylko dla Administratora: Użytkownicy), treść na pełną szerokość. Komunikaty walidacji i mniej istotnych operacji w pasku komunikatu, każde potwierdzenie w oknie komunikatu. Skala 90/100/110/125% według szerokości okna; motyw jasny i ciemny.
- **NFR-7 Czas polski:** „dzisiaj”, daty Wpisów i daty na ekranie Użytkownicy liczone są w strefie Europe/Warsaw, niezależnie od zegara serwera.
- **NFR-8 Ochrona dostępu:** rejestracja i logowanie mają ograniczenie liczby prób; baza nie jest dostępna z internetu; dostęp administracyjny do serwera i bazy tylko szyfrowanym kanałem z kluczem; do bazy na co dzień z uprawnieniem tylko do odczytu. Limity prób, reguły haseł i czas trwania sesji logowania ustala architektura jako bezpieczne wartości domyślne.
- **NFR-9 Zmiany schematu:** od pierwszego wdrożenia zmiany schematu bazy tylko dodają (kolumny, tabele) i są stosowane przy starcie aplikacji; dane na serwerze nie mogą zostać utracone przez wdrożenie. Dzięki temu poprzednia wersja aplikacji działa na nowszym schemacie i rollback (FR-29) nie wymaga cofania bazy.
- **NFR-10 Zerwane połączenie:** gdy połączenie z serwerem się zerwie (uśpiony laptop, brak internetu), aplikacja pokazuje komunikat z prośbą o odświeżenie; niezapisany Wpis może przepaść.

## 6. Poza zakresem (świadomie nie robimy)

- Nie jest to narzędzie zespołowe: brak zespołów, współdzielenia danych między Userami i wglądu Administratora w treść danych w aplikacji.
- Brak e-maila w aplikacji: bez potwierdzania adresu i bez samodzielnego resetu hasła.
- Brak układów Zestawienia per Odbiorca; jeden układ dla wszystkich.
- Brak aplikacji na telefon i układu pod telefon.
- Nie jest to narzędzie raportowe ani rozliczeniowe: brak wykresów, raportów według projektu, stawek i kwot.
- Brak timera, importu (w tym z notatek), eksportu CSV, zamykania Miesiąca, przypomnień o dniu bez Wpisu.
- Brak kodu kopii w aplikacji i osobnego środowiska przedprodukcyjnego.
- Aplikacja nie zastępuje notatek i nie rozwiązuje „szacowania po fakcie”: jeśli notatka ma tylko sumę dnia, rozbicie na Projekty nadal pochodzi z pamięci. Rozbicie musi być w notatkach.

### 6.1 Ryzyka przyjęte świadomie

- **Utrata danych do całego Miesiąca.** Jedyną kopią jest Kopia serwera robiona przez dostawcę. Odtworzenie cofa cały serwer, wszystkim Userom naraz. Zła zmiana schematu, błąd przy usuwaniu Konta albo utrata konta u dostawcy mogą zabrać dane od ostatniej Kopii serwera. Projekt nie jest krytyczny; Autor akceptuje utratę danych do całego Miesiąca (31.10.2026).
- **Dostęp Administratora.** Administrator ma przez bazę dostęp do wszystkich danych i do chwili zmiany zna hasło, które ustawił (FR-21). Userzy są o tym informowani przy rejestracji.
- **Utrzymanie serwera.** Aktualizacje systemu i obsługę serwera wykonuje Autor; to koszt wyboru własnego serwera zamiast usługi zarządzanej.
- **Termin.** CI, Konta i wdrożenie wchodzą przed Słownikami, a więc przed Testem wrześniowym; ekran Użytkownicy, Ustawienia konta, pomoc i instrukcja administratora mogą przyjść po nim, ale przed Miesiącem „na serio”. Nie ma planu awaryjnego (decyzja Autora, 31.10.2026).

## 7. Kryteria sukcesu

Dwie grupy o równej wadze.

**Rejestr**
- **SM-1 Test wrześniowy (do 28.12.2026):** 28 Wpisów z września 2026 (wpis „WKU, RRJ” przypisany do jednego Projektu) daje Zestawienie zgodne z oryginałem w sumach: te same Sumy dnia, Sumy tygodnia 30 / 16 / 18 / 25 / 14 h, RAZEM 103 h. Liczba Wierszy zestawienia może być mniejsza niż 28, jeśli przypisanie „WKU, RRJ” spowoduje złączenie.
  - Warunki: Test wrześniowy odbywa się wyłącznie lokalnie, a Wpisy są wpisywane ręcznie; prawdziwe dane wrześniowe (opisy, Wpisy, plik) nigdy nie trafiają do repozytorium jako dane testowe ani na serwer.
  - Waliduje FR-5, FR-9–FR-12.
  - Niepowodzenie przesuwa pierwszy Miesiąc „na serio” o miesiąc.
- **SM-2 Miesiąc „na serio” (styczeń 2027, najpóźniej luty):** na serwerze wszystkie Wpisy Miesiąca trafiają do aplikacji (z notatek, w Sesjach wpisywania) przed końcem Miesiąca, a Zestawienie powstaje bez Excela. Waliduje FR-5, NFR-3.
- **SM-3 Zestawienie przyjęte:** Zestawienie za ten Miesiąc Autor wysyła Odbiorcy prosto z eksportu, bez poprawek w Excelu, i zostaje przyjęte. Waliduje FR-11–FR-13.
- **SM-5 Oddzielenie danych (przed Miesiącem „na serio”):** obie części muszą przejść.
  - Na serwerze Autor i znajomy mają po kilka własnych Projektów, Typów pracy i Wpisów. Każdy po zalogowaniu nie widzi żadnej pozycji Słownika ani Wpisu drugiego, ani w Liście wpisów, ani w Widoku miesiąca, ani w Zestawieniu; ekran Użytkownicy pokazuje o znajomym tylko dane z FR-20. Dwa Projekty o tym samym kodzie u obu Userów nie kolidują.
  - Testy automatyczne oddzielenia danych, opisane w architekturze, przechodzą w potoku (FR-27) przy każdej zmianie.
  - Waliduje FR-1, FR-20, FR-22, FR-23.

**Nauka BMad**
- **SM-4:** pełny cykl brief → PRD → architektura → epiki i historie → kod → wdrożenie.
  - Cały kod piszą agenci.
  - Wdrożenia na serwer odbywają się wyłącznie przez włączenie zmiany do gałęzi głównej (FR-28); rollback (FR-29) został przećwiczony co najmniej raz.
  - Odtworzenie z Kopii serwera przećwiczone co najmniej raz na nowym, tymczasowym serwerze, bez ruszania produkcji, z potwierdzeniem, że aplikacja na nim działa.
  - Po pierwszym Miesiącu „na serio” powstaje notatka o tym, co działało, a co nie.

**Przeciwwskaźniki (nie optymalizować)**
- **SM-C1 Termin kosztem zasady „tylko agenci”:** szybszy start przez ręczne poprawki kodu jest porażką SM-4, nie sukcesem SM-2. Utknięcie agenta to materiał do notatki końcowej, nie powód do obejścia zasady. Dotyczy także ręcznych poprawek na serwerze poza potokiem wdrożenia; wyjątkiem jest jednorazowe przygotowanie serwera i ćwiczenia z instrukcji administratora (FR-26).
- **SM-C2 Dokładanie zakresu przed Testem wrześniowym:** nic poza zakresem ustalonym 31.10.2026 (§4) nie wchodzi przed SM-1; każda nowa funkcja czeka na Test wrześniowy.

## 8. Otwarte pytania i założenia

Brak otwartych pytań ani założeń.

Ustalone:
- Migracja `Initial` od początku zawiera `UserId` i zostaje przepisana przed pierwszym wdrożeniem na serwer; od tego wdrożenia obowiązuje NFR-9 (decyzja Autora, 31.10.2026; addendum, „Przepisanie schematu”).
- Położenie sum i wiersza RAZEM sprawdzone w oryginalnym xlsx 30.10.2026 (FR-12).
- Osiem założeń pierwszej wersji (FR-1, FR-3, FR-5 ×2, FR-7, FR-11, FR-13, FR-14 — dziś usunięte) Autor potwierdził 30.10.2026.
- Zasada „dane nie opuszczają komputera Autora” (brief) była decyzją Autora i została przez niego uchylona 31.10.2026; ani Odbiorca, ani klienci nie stawiają warunków co do miejsca danych.
- Decyzje z aktualizacji po forge (31.10.2026) i siedem założeń wersji z Kontami i wdrożeniem (FR-17, FR-18, FR-20, NFR-6, NFR-7, SM-1, SM-4) Autor potwierdził 31.10.2026; są wpisane w wymagania jako ustalenia.
