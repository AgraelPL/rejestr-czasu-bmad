---
title: "Product Brief: Rejestr czasu pracy"
status: final
created: 2026-10-30
updated: 2026-10-30
---

# Product Brief: Rejestr czasu pracy

## W skrócie

Lokalna aplikacja webowa, w której jedna osoba (autor) co kilka dni przenosi czas pracy z notatek i na koniec miesiąca generuje zestawienie xlsx w układzie, jakiego oczekuje odbiorca. Zastępuje krok ręcznego przepisywania notatek do Excela na koniec miesiąca; notatki w ciągu dnia zostają.

Projekt ma dwa cele o równej wadze: nauczyć się metody BMad na prawdziwym, małym produkcie i mieć narzędzie, w którym od stycznia 2027 (najpóźniej od lutego) autor naprawdę prowadzi swoje rozliczenia. Grudzień 2026 to czas budowy. Styczeń ma być pierwszym miesiącem „na serio”.

## Problem

Dziś czas pracy trafia najpierw do notatek („RDZ – 4 h”, czasem tylko suma dnia), a na koniec miesiąca jest ręcznie przepisywany do arkusza, który idzie do odbiorcy. Zestawienie za wrzesień 2026 (28 wpisów, 103 h, 7 projektów) pokazuje, ile kosztuje ten krok:

- **Szacowanie po fakcie.** Gdy w notatce jest tylko suma dnia, rozbicie na zadania jest odtwarzane z pamięci.
- **Błędy przepisywania.** Zły rok w dacie, nazwa dnia niezgodna z datą, `RDEZ` zamiast `RDZ`. Nie wiadomo, czy to literówka, czy nowy projekt.
- **Kruche formuły.** Sumy dnia i tygodnia liczą formuły z ręcznie dobieranymi zakresami. Każdy dodany wiersz grozi cichym błędem w sumie.
- **Brak reguł.** Typ pracy jest przypisywany według niepisanej konwencji, a jeden wpis bywa przypisany do dwóch projektów naraz.

Każdy z tych błędów wymaga potem ręcznej kontroli (we wrześniu: osobny arkusz „Założenia”), a jeśli ją ominie, trafia do dokumentu, na podstawie którego autor się rozlicza.

## Dla kogo

- **Autor:** jedyny użytkownik. Pracuje przy kilku projektach naraz (7 kodów we wrześniu), także w weekendy, zawsze na jednym komputerze. Prowadzi notatki i co kilka dni chce je szybko przenieść do aplikacji (uruchamianej na 1–2 h), a na koniec miesiąca nie dotykać Excela.
- **Odbiorca zestawienia:** nie korzysta z aplikacji, dostaje plik xlsx. Akceptuje godziny dziesiętne (1,5; 0,75; potwierdzone 30.10.2026), ale oczekuje dotychczasowego układu arkusza.

## Rozwiązanie

Co kilka dni autor przenosi notatki do aplikacji w jednej sesji, w formularzu, który wyklucza błędy z września: data z kalendarza, projekt i typ pracy ze słowników, jeden projekt na wpis, czas w krokach co 15 minut. Lista wpisów pozwala szukać i sumować godziny, a widok miesiąca pokazuje sumy dnia, tygodnia i miesiąca tak jak dotychczasowy arkusz w Excelu, jeszcze przed eksportem.

Na koniec miesiąca jedno kliknięcie daje zestawienie xlsx za wybrany miesiąc w dotychczasowym układzie odbiorcy: dzień tygodnia, data, kod projektu, typ pracy, opis, godziny, suma dnia, suma tygodnia (pn–nd), wiersz RAZEM, arkusz nazwany „<Miesiąc RRRR>”. Wpisy z tego samego dnia o tym samym projekcie i typie pracy łączą się w jeden wiersz, tak jak dziś: godziny się sumują, a każdy opis trafia do nowej linii w tej samej komórce. Sumy liczy aplikacja, nie formuły z ręcznymi zakresami.

Całość działa lokalnie, bez logowania, z automatyczną kopią bazy przy każdym uruchomieniu.

## Dlaczego nie gotowe narzędzie

Clockify czy dalsze prowadzenie arkusza rozwiązałyby część problemu. Odpadają z dwóch powodów: dane o klientach mają zostać na sprzęcie autora, a wartością jest też sama droga, czyli zbudowanie aplikacji metodą BMad. Aplikacja nie ma przewagi nad tymi narzędziami poza tym, że robi dokładnie jedno zestawienie w dokładnie tym układzie, którego potrzebuje odbiorca.

## Kryteria sukcesu

**Rejestr**
1. **Test wrześniowy, do 28 grudnia 2026:** wrzesień 2026 wpisany do aplikacji (28 wpisów, wpis „WKU, RRJ” przypisany do jednego projektu) daje zestawienie zgodne z oryginałem: sumy dni, sumy tygodni 30 / 16 / 18 / 25 / 14 h i RAZEM 103 h.
2. **Pierwszy miesiąc „na serio” (cel: styczeń 2027, najpóźniej luty):** wszystkie wpisy miesiąca trafiają z notatek do aplikacji przed końcem miesiąca, a zestawienie powstaje bez Excela.
3. **Zestawienie za ten miesiąc** trafia do odbiorcy prosto z eksportu, bez ręcznych poprawek w Excelu, i odbiorca je przyjmuje.

**Nauka BMad**
4. Projekt przechodzi cały cykl: brief → PRD → architektura → epiki i historie → kod.
5. Cały kod piszą agenci BMad, nie autor ręcznie.
6. Po pierwszym miesiącu „na serio” powstaje krótka notatka: co w metodzie działało, a co nie.

## Zakres v1

**W zakresie**
- Słowniki projektów (kod i nazwa) i typów pracy, niezależne od siebie. Flaga aktywności: nieaktywne pozycje znikają z list wyboru, ale zostają w historii i eksporcie. Zmiana nazwy obejmuje też wpisy historyczne. Usunąć można tylko pozycję nieużytą we wpisach.
- Wpis: data (dzień tygodnia wyliczany z daty), projekt, typ pracy, opis, czas (`h:mm`, co 15 minut, od 0:15 do 24:00). Dodawanie, edycja, usuwanie i kopiowanie (kopia otwiera formularz z projektem, typem pracy, opisem i czasem oryginału, z datą ustawioną aktualnie w formularzu). Po zapisie formularz zostaje otwarty z tą samą datą na kolejny wpis.
- Lista wpisów do wyszukiwania (np. „ile godzin RDZ w I kwartale”) z filtrami: zakres dat (domyślnie bieżący miesiąc), projekt, typ pracy; pod listą suma godzin.
- Widok miesiąca do kontroli przed eksportem: wpisy dzień po dniu z sumą dnia, tygodnia (pn–nd) i miesiąca.
- Eksport xlsx za miesiąc w układzie odbiorcy, z łączeniem wierszy opisanym w „Rozwiązaniu”.
- Nasłuch tylko na `127.0.0.1`. Baza SQLite w lokalnym pliku poza folderami synchronizowanymi z chmurą, ścieżka w konfiguracji. Autor uruchamia aplikację ręcznie komendą `npm start`; autostart ani skrót nie są potrzebne. Kopia bazy w osobnym katalogu powstaje przy każdym uruchomieniu (aplikacja działa 1–2 h co kilka dni); zachowywanych jest kilka ostatnich kopii. Baza i kopie w `.gitignore`.

**Poza zakresem v1**
- Logowanie i konta (może wrócić później jako osobne ćwiczenie), dostęp z telefonu i innych urządzeń.
- Timer, raporty i wykresy (np. godziny według projektu), stawki i kwoty.
- Import danych, w tym z notatek. Wrzesień na potrzeby testu wpisuje się ręcznie.
- Eksport CSV.
- Zamknięcie miesiąca (blokada edycji po oddaniu zestawienia), przypomnienia o dniu bez wpisu.

## Ryzyka i otwarte sprawy

- **Brak miesiąca próbnego.** Pierwszy miesiąc „na serio” jest jednocześnie pierwszym prawdziwym użyciem. Zabezpieczeniem jest test wrześniowy i punkt decyzyjny: jeśli test nie przejdzie do 28 grudnia 2026, styczeń idzie po staremu (notatki i Excel), a start przesuwa się na kolejny miesiąc. Projekt nie jest krytyczny, więc przesunięcie to koszt akceptowalny.
- **Kopia na tym samym dysku.** Chroni przed uszkodzeniem bazy i przypadkowym usunięciem, nie przed awarią dysku ani utratą laptopa. Ryzyko przyjęte świadomie.
- **Dane klientów a publiczne repozytorium.** Do publicznego repozytorium trafia samo rozwiązanie. Test wrześniowy odbywa się wyłącznie lokalnie. Baza, kopie i źródłowy xlsx (w `docs/`) nie mogą trafić do gita; wyklucza je `.gitignore`. Testy, przykłady i słowniki startowe w repozytorium muszą być zanonimizowane. Kody projektów (`RDZ`, `WKU`…) i sumy godzin z września mogą zostać w dokumentach planistycznych (brief, PRD, tickety), które też trafiają do repozytorium (decyzja autora, 30.10.2026).
- **Termin a zasada „tylko agenci”.** Gdy się zderzą, wygrywa metoda: autor nie poprawia kodu ręcznie, nawet jeśli start przesunie się na luty. Utknięcie agenta to materiał do notatki końcowej, nie powód do obejścia zasady.
- **Szczegóły do PRD.** Osiem otwartych pytań (m.in. suma tygodnia na przełomie miesięcy, która wpływa na test wrześniowy) jest w `addendum.md`.

## Co dalej

Jeśli pierwszy miesiąc się uda, naturalne następne kroki to podsumowania miesiąca według projektu i typu pracy, zamknięcie miesiąca oraz logowanie, każde jako osobne ćwiczenie w BMad. Dostęp spoza komputera autora wymagałby powrotu do odrzuconego wcześniej (na etapie forge) pomysłu z serwerem, domeną i HTTPS, więc nie jest planowany.
