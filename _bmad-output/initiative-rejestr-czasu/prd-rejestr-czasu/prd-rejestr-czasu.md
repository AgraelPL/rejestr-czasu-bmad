---
title: "PRD: Rejestr czasu pracy"
status: final
created: 2026-10-30
updated: 2026-10-30
---

# PRD: Rejestr czasu pracy

## 0. Cel dokumentu

PRD dla Autora (jedynego użytkownika) i dla agentów BMad, którzy z niego zbudują architekturę, epiki i historie. Rozwija brief [`brief-rejestr-czasu.md`](../brief-rejestr-czasu/brief-rejestr-czasu.md) i jego addendum, nie powtarza ich uzasadnień. Terminy z §3 Pojęcia obowiązują dosłownie. Wymagania są pogrupowane w funkcje, numerowane globalnie (FR-1…). Założenia z pierwszej wersji potwierdził Autor 30.10.2026 (§8). Decyzje techniczne są w [`addendum.md`](addendum.md).

## 1. Wizja

Autor zapisuje czas pracy w notatkach, a co kilka dni przenosi je do aplikacji w jednej Sesji wpisywania. Formularz nie pozwala popełnić błędów znanych z września 2026: złej daty, literówki w kodzie Projektu, dwóch Projektów w jednym Wpisie, ręcznie liczonych sum. Na koniec Miesiąca nie ma przepisywania do Excela: jedno kliknięcie daje Zestawienie xlsx w układzie, którego oczekuje Odbiorca, gotowe do wysłania bez otwierania Excela.

Aplikacja działa wyłącznie na komputerze Autora. Dane o klientach nie opuszczają jego sprzętu.

Przedsięwzięcie ma dwa cele o równej wadze: narzędzie, w którym Autor od stycznia 2027 (najpóźniej od lutego) prowadzi rozliczenia, i naukę metody BMad na prawdziwym, małym produkcie.

## 2. Użytkownik

### 2.1 Zadania do wykonania

- Co kilka dni przenieść notatki do aplikacji szybko, w jednej Sesji wpisywania, bez błędów przepisywania.
- Na koniec Miesiąca oddać Odbiorcy Zestawienie bez przepisywania i bez ręcznej kontroli sum.
- Odpowiedzieć sobie na pytania w rodzaju „ile godzin RDZ w I kwartale”.
- Przejść cały cykl BMad na prawdziwym produkcie (cel równorzędny z narzędziem).

### 2.2 Ścieżki użytkownika

- **UJ-1. Sesja wpisywania.** Autor uruchamia aplikację (`npm start`), otwiera notatki z ostatnich 3–4 dni i wpisuje po kolei 6–12 Wpisów: ustawia datę pierwszego dnia, wybiera Projekt i Typ pracy, wpisuje opis i Czas, zapisuje; formularz zostaje z tą samą datą na kolejny Wpis, a przy następnym dniu Autor zmienia tylko datę.
- **UJ-2. Zamknięcie miesiąca.** Pierwszego dnia nowego miesiąca autor otwiera Widok miesiąca za poprzedni Miesiąc, sprawdza Sumy dnia i Sumy tygodnia, uzupełnia brakujący Wpis, klika eksport i wysyła Zestawienie Odbiorcy.
- **UJ-3. Pytanie o godziny.** Autor otwiera Listę wpisów, ustawia zakres 01.01–31.03 i Projekt RDZ, odczytuje sumę pod listą.

## 3. Pojęcia

- **Autor** — jedyny użytkownik aplikacji.
- **Odbiorca** — osoba, która dostaje Zestawienie; nie korzysta z aplikacji.
- **Projekt** — pozycja Słownika: kod (np. `RDZ`, unikalny) i nazwa. Ma flagę aktywności.
- **Typ pracy** — pozycja Słownika: nazwa (np. `Development`, `Analiza bis-sys`). Ma flagę aktywności. Niezależny od Projektu.
- **Słownik** — lista Projektów albo lista Typów pracy prowadzona w aplikacji.
- **Sesja wpisywania** — jedno uruchomienie aplikacji (zwykle 1–2 h co kilka dni), w którym Autor przenosi notatki z kilku dni do Wpisów.
- **Wpis** — jeden zarejestrowany odcinek pracy: data, dokładnie jeden Projekt, dokładnie jeden Typ pracy, opis, czas.
- **Czas** — długość Wpisu w formacie `h:mm`, wielokrotność 15 minut, od `0:15` do `24:00`. W Zestawieniu wyrażany jako godziny dziesiętne (`1:30` → 1,5).
- **Miesiąc** — miesiąc kalendarzowy; jednostka Widoku miesiąca i Zestawienia.
- **Suma dnia** — suma Czasu Wpisów z jednego dnia.
- **Suma tygodnia** — suma Czasu Wpisów z dni jednego tygodnia (pn–nd) **należących do danego Miesiąca**; tydzień na przełomie miesięcy liczy się w każdym Miesiącu osobno.
- **Widok miesiąca** — ekran z Wpisami Miesiąca dzień po dniu, z Sumą dnia, Sumą tygodnia i sumą Miesiąca.
- **Lista wpisów** — ekran wyszukiwania Wpisów z filtrami i sumą.
- **Zestawienie** — plik xlsx za jeden Miesiąc w układzie Odbiorcy (FR-12).
- **Wiersz zestawienia** — wiersz Zestawienia powstały ze złączenia Wpisów z tego samego dnia o tym samym Projekcie i Typie pracy.
- **Kopia bazy** — plik z kopią danych aplikacji, tworzony automatycznie.
- **Test wrześniowy** — wpisanie 28 Wpisów z września 2026 i porównanie Zestawienia z oryginałem (SM-1).

## 4. Funkcje

### 4.1 Słowniki

**Opis:** Autor sam prowadzi oba Słowniki; przy pierwszym uruchomieniu są puste i Autor wypełnia je ręcznie (7 Projektów, 2 Typy pracy). Nieaktywne pozycje znikają z list wyboru, ale zostają we Wpisach, Widoku miesiąca, Liście wpisów (jako filtr) i Zestawieniu.

#### FR-1: Dodanie i edycja Projektu
Autor może dodać Projekt (kod, nazwa) i zmienić jego kod lub nazwę.
- Kod jest wymagany i unikalny bez względu na wielkość liter; `RDZ` i `rdz` to ten sam kod. Kod jest zapisywany wielkimi literami.
- Zmiana kodu lub nazwy jest widoczna we wszystkich Wpisach, także historycznych, i w każdym późniejszym Zestawieniu. Świadoma konsekwencja: ponowny eksport Miesiąca już oddanego Odbiorcy po zmianie kodu da plik inny niż wysłany.

#### FR-2: Dodanie i edycja Typu pracy
Autor może dodać Typ pracy (nazwa) i zmienić jego nazwę. Nazwa jest wymagana i unikalna bez względu na wielkość liter. Zmiana obejmuje Wpisy historyczne.

#### FR-3: Aktywność pozycji Słownika
Autor może oznaczyć Projekt lub Typ pracy jako nieaktywny i z powrotem aktywny.
- Nieaktywna pozycja nie pojawia się na listach wyboru w formularzu Wpisu.
- Edycja istniejącego Wpisu z nieaktywną pozycją zachowuje ją.

#### FR-4: Usunięcie pozycji Słownika
Autor może usunąć Projekt lub Typ pracy, który nie występuje w żadnym Wpisie. Próba usunięcia użytej pozycji kończy się komunikatem z liczbą Wpisów, które jej używają, i propozycją dezaktywacji.

### 4.2 Wpisy

**Opis:** Formularz Wpisu to najczęściej używany ekran; ma działać szybko z klawiatury (NFR-3). Realizuje UJ-1.

#### FR-5: Dodanie Wpisu
Autor może dodać Wpis: data (z kalendarza), Projekt i Typ pracy (z list aktywnych pozycji), opis, Czas.
- Pierwszy Wpis w Sesji wpisywania ma domyślnie dzisiejszą datę. Po zapisie formularz zostaje otwarty na kolejny Wpis z tą samą datą; Projekt, Typ pracy, opis i Czas są czyszczone.
- Dzień tygodnia jest wyliczany z daty i pokazywany przy niej; nie da się go wpisać.
- Czas spoza zakresu `0:15`–`24:00` lub niebędący wielokrotnością 15 minut jest odrzucany z komunikatem.
- Opis jest wymagany.
- Wpis ma dokładnie jeden Projekt; formularz nie pozwala wybrać dwóch.
- Suma dnia powyżej 24 h nie jest blokowana.

#### FR-6: Edycja i usunięcie Wpisu
Autor może zmienić każde pole Wpisu i usunąć Wpis (z potwierdzeniem). Dotyczy też Wpisów z poprzednich Miesięcy; v1 nie ma zamykania Miesiąca.

#### FR-7: Kopiowanie Wpisu
Autor może skopiować Wpis: otwiera się formularz nowego Wpisu z Projektem, Typem pracy, opisem i Czasem oryginału oraz z datą ustawioną aktualnie w formularzu Wpisu (na początku Sesji wpisywania dzisiejszą). Nic nie zapisuje się bez potwierdzenia Autora. Jeśli Projekt lub Typ pracy oryginału jest nieaktywny, odpowiednie pole pozostaje puste do ponownego wyboru.

### 4.3 Lista wpisów

**Opis:** Realizuje UJ-3.

#### FR-8: Filtrowanie i suma
Autor może przeglądać Wpisy z filtrami: zakres dat (domyślnie bieżący Miesiąc), Projekt, Typ pracy (oba także nieaktywne).
- Lista jest posortowana od najnowszej daty.
- Pod listą widać liczbę Wpisów i sumę Czasu dla bieżących filtrów.
- Z listy można edytować, kopiować i usuwać Wpis.

### 4.4 Widok miesiąca

**Opis:** Kontrola przed eksportem, odpowiednik dotychczasowego arkusza. Realizuje UJ-2.

#### FR-9: Widok miesiąca
Autor może wybrać Miesiąc (domyślnie bieżący) i zobaczyć jego Wpisy dzień po dniu, chronologicznie, z dniem tygodnia, Sumą dnia, Sumą tygodnia i sumą Miesiąca.
- W obrębie dnia Wpisy są ułożone jak Wiersze zestawienia (FR-11): według kodu Projektu, potem nazwy Typu pracy.
- Suma tygodnia obejmuje tylko dni danego Miesiąca.
- Dni bez Wpisów są pomijane.
- Z widoku można edytować, kopiować i usuwać Wpis; sumy przeliczają się od razu.

#### FR-10: Zgodność sum z Zestawieniem
Sumy dnia, Sumy tygodnia i suma Miesiąca w Widoku miesiąca są identyczne z odpowiednimi sumami w Zestawieniu za ten Miesiąc.

### 4.5 Zestawienie

**Opis:** Jedno kliknięcie w Widoku miesiąca daje Zestawienie za wybrany Miesiąc. Realizuje UJ-2.

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
Plik nazywa się `<prefiks>_Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx` (np. `…_Zestawienie-wrzesień_2026.xlsx`); prefiks pochodzi z konfiguracji. Bez prefiksu nazwa to `Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx`. Przeglądarka pobiera plik standardowo. Eksport Miesiąca bez Wpisów kończy się komunikatem zamiast pustego pliku.

### 4.6 Bezpieczeństwo danych

#### FR-14: Kopia bazy
Kopia bazy powstaje przy każdym uruchomieniu aplikacji, zanim aplikacja przyjmie pierwsze żądanie; w trakcie działania kopie nie powstają (aplikacja działa 1–2 h co kilka dni). Kopie bazy trafiają do osobnego katalogu pod nazwą `rejestr-RRRR-MM-DD_GGMMSS.sqlite`; zachowywanych jest 10 najnowszych.
- Najstarsza Kopia bazy jest usuwana dopiero po udanym zapisie nowej.
- Nieudana Kopia bazy zatrzymuje uruchomienie z czytelnym komunikatem.

#### FR-15: Lokalizacja danych
Ścieżki pliku bazy i katalogu kopii pochodzą z konfiguracji. Domyślne ścieżki leżą poza repozytorium i poza folderami synchronizowanymi z chmurą (na Windowsie `Documents` bywa przekierowany do OneDrive, a repozytorium leży w `Documents`).

## 5. Wymagania niefunkcjonalne

- **NFR-1 Tylko lokalnie:** aplikacja nasłuchuje wyłącznie na `127.0.0.1` i nie nawiązuje żadnych połączeń wychodzących (brak telemetrii, CDN, zewnętrznych czcionek); wszystkie zasoby są serwowane lokalnie. Brak logowania i kont.
- **NFR-2 Prywatność repozytorium:** baza, kopie, źródłowy xlsx i plik konfiguracji z prefiksem nie trafiają do gita. Testy, przykłady i dane startowe w repozytorium są zanonimizowane. Kody Projektów i sumy z września mogą zostać w dokumentach planistycznych (decyzja Autora, 30.10.2026).
- **NFR-3 Szybkość wpisu:** każdy kolejny Wpis w Sesji wpisywania, od pustego formularza do zapisu, zajmuje kilka sekund, nie więcej niż 10; całość da się wykonać samą klawiaturą.
- **NFR-4 Platforma:** desktop, aktualna przeglądarka na komputerze Autora; interfejs tylko po polsku. Uruchomienie komendą `npm start`.
- **NFR-5 Arytmetyka:** Czas jest liczony w minutach całkowitych; zaokrąglenia nie występują aż do konwersji na godziny dziesiętne w Zestawieniu.

## 6. Poza zakresem (świadomie nie robimy)

- Nie jest to narzędzie zespołowe ani usługa sieciowa: brak kont, logowania, dostępu z telefonu i innych urządzeń.
- Nie jest to narzędzie raportowe ani rozliczeniowe: brak wykresów, raportów według projektu, stawek i kwot.
- Brak timera, importu (w tym z notatek), eksportu CSV, zamykania Miesiąca, przypomnień o dniu bez Wpisu.
- Brak autostartu i skrótu uruchomieniowego; Autor uruchamia aplikację sam.
- Aplikacja nie zastępuje notatek i nie rozwiązuje „szacowania po fakcie”: jeśli notatka ma tylko sumę dnia, rozbicie na Projekty nadal pochodzi z pamięci. Rozbicie musi być w notatkach.
- Brak synchronizacji i Kopii bazy poza dyskiem lokalnym; awaria dysku lub utrata laptopa to ryzyko przyjęte świadomie w briefie.

## 7. Kryteria sukcesu

Dwie grupy o równej wadze.

**Rejestr**
- **SM-1 Test wrześniowy (do 28.12.2026):** 28 Wpisów z września 2026 (wpis „WKU, RRJ” przypisany do jednego Projektu) daje Zestawienie zgodne z oryginałem w sumach: te same Sumy dnia, Sumy tygodnia 30 / 16 / 18 / 25 / 14 h, RAZEM 103 h. Liczba Wierszy zestawienia może być mniejsza niż 28, jeśli przypisanie „WKU, RRJ” spowoduje złączenie. 
  - Warunki: Test wrześniowy odbywa się wyłącznie lokalnie, a Wpisy są wpisywane ręcznie; prawdziwe dane wrześniowe (opisy, Wpisy, plik) nigdy nie trafiają do repozytorium jako dane testowe.
  - Waliduje FR-5, FR-9–FR-12.
  - Niepowodzenie przesuwa pierwszy miesiąc „na serio” o miesiąc.
- **SM-2 Miesiąc „na serio” (styczeń 2027, najpóźniej luty):** wszystkie Wpisy Miesiąca trafiają do aplikacji (z notatek, w Sesjach wpisywania) przed końcem Miesiąca, a Zestawienie powstaje bez Excela. Waliduje FR-5, NFR-3.
- **SM-3 Zestawienie przyjęte:** Zestawienie za ten Miesiąc trafia do Odbiorcy prosto z eksportu, bez poprawek w Excelu, i zostaje przyjęte. Waliduje FR-11–FR-13.

**Nauka BMad**
- **SM-4:** pełny cykl brief → PRD → architektura → epiki i historie → kod; cały kod piszą agenci; po pierwszym miesiącu „na serio” powstaje notatka o tym, co działało, a co nie.

**Przeciwwskaźniki (nie optymalizować)**
- **SM-C1 Termin kosztem zasady „tylko agenci”:** szybszy start przez ręczne poprawki kodu jest porażką SM-4, nie sukcesem SM-2. Utknięcie agenta to materiał do notatki końcowej, nie powód do obejścia zasady.
- **SM-C2 Liczba funkcji:** każda funkcja spoza §4 opóźniająca SM-1 jest kosztem, nie wartością.

## 8. Otwarte pytania i założenia

Brak otwartych pytań ani założeń.
- Położenie sum i wiersza RAZEM sprawdzone w oryginalnym xlsx 30.10.2026 (FR-12).
- Osiem założeń pierwszej wersji (FR-1, FR-3, FR-5 ×2, FR-7, FR-11, FR-13, FR-14) Autor potwierdził 30.10.2026; są wpisane w wymagania jako ustalenia.
