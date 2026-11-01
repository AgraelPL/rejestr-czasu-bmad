---
type: epic
title: "Wdrożenie na serwer"
parent: initiative-rejestr-czasu
covers: [R15, R24, R27, R29, R30]
sources: []
companions: []
after: []
risk: high
---

# Wdrożenie na serwer

## Description

Zamknięcie inicjatywy przed Miesiącem „na serio”: aplikację z epików 1–3 i 5, zbudowaną i sprawdzoną lokalnie, wdraża się na Droplet pod `czas-pracy.kandula-consulting.pl` z HTTPS. Merge do gałęzi głównej wdraża bez ręcznych kroków, rollback do wskazanego tagu jest przećwiczony, a jedna zbiorcza weryfikacja na serwerze powtarza to, co epiki sprawdziły lokalnie, razem z próbą oddzielenia danych na dwóch Kontach i odtworzeniem z Kopii serwera. Epik korzysta z obrazu, workflow PR i pierwszego Administratora z epic-konta-i-wdrozenie. Pierwsze wdrożenie zamraża `Initial` (prd NFR-9, AD-21): od niego zmiany schematu tylko nową migracją dodającą.

## Outcome

Autor i znajomy pracują na serwerze pod subdomeną z HTTPS, merge Autora trafia tam bez ręcznych kroków, a ich dane są od siebie oddzielone (R24, R29, R30; prd SM-4, SM-5).

## Requirements

- R15 (parent R15): Parametry połączenia z bazą i sekrety pochodzą z konfiguracji poza gitem, na serwerze i w potoku wdrożenia; lokalnie dane bazy leżą poza repozytorium i poza folderami synchronizowanymi z chmurą. Tu: serwer i potok wdrożenia.
- R24 (parent R24): User widzi i zmienia wyłącznie własne Słowniki i Wpisy, a Zestawienie zawiera tylko jego Wpisy; jedynym wyjątkiem jest liczba Wpisów na ekranie Użytkownicy; sprawdzają to próba na serwerze na dwóch Kontach i testy automatyczne w potoku. Tu: próba na serwerze na dwóch Kontach.
- R27 (parent R27): Repozytorium zawiera instrukcję administratora w Markdownie, dostępną, gdy aplikacja nie działa, z tematami z addendum PRD (serwer, domena, sekrety, dostęp do bazy, wdrożenie, rollback, odtworzenie z Kopii serwera). Tu: serwer, domena, sekrety, SSH, dostęp do bazy, wdrożenie, rollback i odtworzenie z Kopii serwera.
- R29 (parent R29): Włączenie zmiany do gałęzi głównej wdraża ją na serwer bez ręcznych kroków; po wdrożeniu aplikacja sprawdza swój stan, a nieudane sprawdzenie kończy wdrożenie błędem widocznym dla Autora, bez automatycznego powrotu do poprzedniej wersji.
- R30 (parent R30): Autor jednym poleceniem przywraca na serwerze wskazaną wcześniejszą wersję aplikacji; rollback jest przećwiczony co najmniej raz.

## Constraints

- prd NFR-1: ruch tylko przez HTTPS; połączenia infrastruktury (certyfikaty, rejestr obrazów) dozwolone.
- prd NFR-2: repozytorium publiczne; baza, sekrety i konfiguracja z hasłami nie trafiają do gita, obrazu aplikacji ani logów potoku; prawdziwe dane wrześniowe ani w repozytorium, ani na serwerze.
- prd NFR-8: baza niedostępna z internetu; dostęp administracyjny tylko szyfrowanym kanałem z kluczem, do bazy na co dzień tylko do odczytu; wartości według AD-15.
- prd NFR-9: od pierwszego wdrożenia tylko migracje dodające, stosowane przy starcie.
- prd SM-4, SM-C1: zmiany na serwerze tylko przez potok, poza jednorazowym przygotowaniem serwera i ćwiczeniami z instrukcji administratora.
- architecture: kręgosłup `architecture-rejestr-czasu.md` (AD-1…AD-22; AD-3 wycofane), w szczególności AD-2, AD-13, AD-19–AD-21.

## Done when

- Merge do gałęzi głównej wdraża aplikację na serwer, sprawdzenie stanu po wdrożeniu przechodzi, a nieudane kończy wdrożenie błędem bez automatycznego powrotu (R29).
- Rollback do wskazanej wcześniejszej wersji przećwiczony raz na serwerze (R30).
- Zbiorcza weryfikacja na serwerze przechodzi: logowanie i Wyloguj z belki, menu zależne od roli, Słowniki, Sesja wpisywania, Widok miesiąca z sumami równymi Zestawieniu, ekran Użytkownicy.
- Próba SM-5 na serwerze: Autor i znajomy nie widzą swoich Słowników, Wpisów ani Zestawień, a ten sam kod Projektu u obu nie koliduje (R24).
- Odtworzenie z Kopii serwera przećwiczone na tymczasowym serwerze bez ruszania produkcji; `docs/admin/` pozwala przejść przygotowanie serwera, wdrożenie, rollback i odtworzenie od zera (R27).

## Boundaries

- Funkcje aplikacji należą do epików 1–3 i 5 i są tam sprawdzane lokalnie; ten epik ich nie zmienia. Błąd funkcji znaleziony na serwerze naprawia nowa historia w epiku właściciela; błąd samego serwera — historia w tym epiku.
- Obraz, `/health`, `ForwardedHeaders` i klucze Data Protection w bazie daje epic-konta-i-wdrozenie (historia 4.5).

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R15, R24, R27, R29, R30
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-2, AD-13, AD-19–AD-21
- prd addendum — _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/addendum.md, Wdrożenie i infrastruktura
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Decision (01.11.2026): epik wydzielony z epic-konta-i-wdrozenie, bo Autor odkłada wdrożenie na Droplet na koniec, po dostarczeniu funkcji: migracja `Initial` zostaje otwarta do pierwszego wdrożenia (NFR-9), bez kosztów i obsługi serwera, zanim aplikacja jest użyteczna. Historie „Wdrożenie na Droplet” (dawniej 4.8) i „Rollback do wskazanej wersji” (dawniej 4.6) przeniesione tu bez zmian treści planu; sprawdzenia „na serwerze” z epików 1–3 i 5 przechodzą do zbiorczej weryfikacji w tym epiku.
- Decision (01.11.2026): wdrożenie i tak przed Miesiącem „na serio” (styczeń 2027, najpóźniej luty) — tego wymaga Done when inicjatywy.
- Decision (01.11.2026): ćwiczenie odtworzenia z Kopii serwera i strona `docs/admin/` o nim przeniesione z epic-administracja-kont-i-pomoc, razem z próbą SM-5 na serwerze (część R24).
- Decision (01.11.2026): tracer bullet to „Wdrożenie na Droplet”; potem rollback, na końcu zbiorcza weryfikacja z próbą SM-5 i odtworzeniem z Kopii serwera.
- Waits on epic-konta-i-wdrozenie because: obraz aplikacji gotowy do pracy za proxy, workflow PR, pierwszy Administrator z sekretu.
- Waits on epic-szkielet-i-slowniki, epic-wpisy-i-lista, epic-miesiac-i-zestawienie, epic-administracja-kont-i-pomoc because: zbiorcza weryfikacja na serwerze i próba SM-5 potrzebują ich funkcji.
