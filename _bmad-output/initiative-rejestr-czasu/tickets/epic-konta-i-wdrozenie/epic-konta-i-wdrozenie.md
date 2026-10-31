---
type: epic
title: "Konta i wdrożenie"
parent: initiative-rejestr-czasu
covers: [R15, R18, R19, R20, R24, R25, R27, R28, R29, R30]
sources: []
companions: []
after: []
refined: true
risk: high
---

# Konta i wdrożenie

## Description

Platforma, na której stoją pozostałe epiki. Każda propozycja zmiany przechodzi build i testy, a merge do gałęzi głównej wdraża aplikację na serwer pod subdomeną z HTTPS. Przepisany schemat `Initial` ma `UserId` z globalnym filtrem oraz dane Konta (stan, data rejestracji, ostatnie logowanie, licznik nieudanych prób). Działają rejestracja, logowanie, wylogowanie, zmiana hasła na statycznej stronie (wymuszona i dobrowolna), role User i Administrator, pierwszy Administrator z sekretu i sprawdzanie stanu Konta przy każdej akcji. Buduje na zrobionej historii 1.1 z epic-szkielet-i-slowniki. Daje epic-szkielet-i-slowniki `UserId` z filtrem, logowanie, role, autoryzację tras, układ dla niezalogowanych i potok wdrożenia; daje epic-miesiac-i-zestawienie stronę `/konto/zmiana-hasla` (wiersz `account_settings` powstaje leniwie w epiku 3, AD-7); daje epic-administracja-kont-i-pomoc stan Konta, jego dane, sprawdzanie stanu i wymuszoną zmianę hasła.

## Outcome

Autor rejestruje się i loguje na serwerze pod subdomeną, a jego merge do gałęzi głównej trafia tam bez ręcznych kroków (R18–R20, R28, R29; prd SM-4).

## Requirements

- R15 (parent R15): Parametry połączenia z bazą i sekrety pochodzą z konfiguracji poza gitem, na serwerze i w potoku wdrożenia; lokalnie dane bazy leżą poza repozytorium i poza folderami synchronizowanymi z chmurą.
- R18 (parent R18): Każdy zakłada Konto loginem i hasłem, bez e-maila; login jest unikalny bez względu na wielkość liter; formularz mówi jednym zdaniem, że Administrator ma przez bazę dostęp do wszystkich danych; nowe Konto jest w stanie Oczekuje i nie loguje się do zatwierdzenia.
- R19 (parent R19): Zalogować się może tylko Konto Aktywne; Konto Oczekuje lub Zablokowane dostaje komunikat o stanie; po 5 nieudanych próbach z rzędu logowanie na Konto jest wstrzymane na 15 minut; User wylogowuje się z belki górnej.
- R20 (parent R20): Pierwsze Konto Administratora powstaje przy wdrożeniu z sekretu w konfiguracji serwera, nie z formularza; Konto Autora ma role User i Administrator.
- R24 (parent R24): User widzi i zmienia wyłącznie własne Słowniki i Wpisy, a Zestawienie zawiera tylko jego Wpisy; jedynym wyjątkiem jest liczba Wpisów na ekranie Użytkownicy; sprawdzają to próba na serwerze na dwóch Kontach i testy automatyczne w potoku. Tu: oddzielenie w warstwie danych i testy automatyczne; próba na dwóch Kontach w epic-administracja-kont-i-pomoc.
- R25 (parent R25): W Ustawieniach konta User ustawia prefiks pliku Zestawienia i zmienia własne hasło, podając obecne. Tu: strona zmiany hasła `/konto/zmiana-hasla`; ekran Ustawienia konta w epic-miesiac-i-zestawienie.
- R27 (parent R27): Repozytorium zawiera instrukcję administratora w Markdownie, dostępną, gdy aplikacja nie działa, z tematami z addendum PRD (serwer, domena, sekrety, dostęp do bazy, wdrożenie, rollback, odtworzenie z Kopii serwera). Tu: wszystkie tematy poza odtworzeniem z Kopii serwera.
- R28 (parent R28): Każda propozycja zmiany do gałęzi głównej przechodzi automatycznie budowanie i wszystkie testy; zmiana z błędem nie może zostać włączona.
- R29 (parent R29): Włączenie zmiany do gałęzi głównej wdraża ją na serwer bez ręcznych kroków; po wdrożeniu aplikacja sprawdza swój stan, a nieudane sprawdzenie kończy wdrożenie błędem widocznym dla Autora, bez automatycznego powrotu do poprzedniej wersji.
- R30 (parent R30): Autor jednym poleceniem przywraca na serwerze wskazaną wcześniejszą wersję aplikacji; rollback jest przećwiczony co najmniej raz.

## Constraints

- prd NFR-1: aplikacja nie nawiązuje połączeń wychodzących; zasoby serwuje sama, a CSP blokuje inne źródła; ruch tylko przez HTTPS; połączenia infrastruktury (certyfikaty, rejestr obrazów) dozwolone.
- prd NFR-2: repozytorium publiczne; baza, sekrety i konfiguracja z hasłami nie trafiają do gita, obrazu aplikacji ani logów potoku; testy, przykłady i dane startowe zanonimizowane.
- prd NFR-4: interfejs tylko po polsku; dostęp pod subdomeną Autora; środowiskiem testowym jest lokalne uruchomienie, bez środowiska przedprodukcyjnego.
- prd NFR-7: daty i znaczniki czasu w strefie Europe/Warsaw, niezależnie od zegara serwera.
- prd NFR-8: limity prób rejestracji i logowania; baza niedostępna z internetu; dostęp administracyjny tylko szyfrowanym kanałem z kluczem, do bazy na co dzień tylko do odczytu; wartości według AD-15.
- prd NFR-9: migracja `Initial` przepisana z `UserId` przed pierwszym wdrożeniem; od niego tylko migracje dodające, stosowane przy starcie.
- architecture: kręgosłup `architecture-rejestr-czasu.md` (AD-1…AD-22; AD-3 wycofane) wiąże ten epik, w szczególności AD-2, AD-7, AD-13, AD-15–AD-21.
- prd SM-4, SM-C1: cały kod piszą agenci BMad; zmiany na serwerze tylko przez potok, poza jednorazowym przygotowaniem serwera i ćwiczeniami z instrukcji administratora.
- prd §3: terminy z `glossary.md` obowiązują dosłownie.

## Done when

- Propozycja zmiany z błędem builda albo testu nie może zostać włączona do gałęzi głównej (R28).
- Merge do gałęzi głównej wdraża aplikację na serwer, sprawdzenie stanu po wdrożeniu przechodzi, a nieudane kończy wdrożenie błędem bez automatycznego powrotu (R29).
- Rollback do wskazanej wcześniejszej wersji przećwiczony raz na serwerze (R30).
- Na serwerze: rejestracja daje Konto Oczekuje, pierwszy Administrator powstaje z sekretu z rolami Administrator i User, Konto Aktywne loguje się, 5 nieudanych prób wstrzymuje logowanie na 15 minut, zmiana własnego hasła nie wylogowuje (R18–R20, R25); wylogowanie przyciskiem z belki na serwerze potwierdza historia 1.3 z epic-szkielet-i-slowniki.
- Testy automatyczne oddzielenia danych po `UserId` przechodzą w potoku; schemat `Initial` przepisany z `UserId` przed pierwszym wdrożeniem (R24; prd NFR-9).
- Instrukcja administratora w `docs/admin/` opisuje serwer, domenę, sekrety, SSH, dostęp do bazy tylko do odczytu, wdrożenie i rollback (R27).

## Boundaries

- Ekran Użytkownicy, odrzucanie, blokowanie, usuwanie Kont i ustawianie hasła przez Administratora należą do epic-administracja-kont-i-pomoc; ten epik daje tylko stan Konta i jego sprawdzanie.
- Układ z menu, belka i mechanizm pomocy należą do epic-szkielet-i-slowniki.
- Ekran Ustawienia konta (prefiks i link do zmiany hasła) należy do epic-miesiac-i-zestawienie; ten epik daje stronę zmiany hasła.
- Strona o odtwarzaniu z Kopii serwera i ćwiczenie odtworzenia należą do epic-administracja-kont-i-pomoc.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R15, R18–R20, R24, R25, R27–R30
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-2, AD-7, AD-13, AD-15–AD-21
- prd addendum — _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/addendum.md, Wdrożenie i infrastruktura
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Decision (31.10.2026): historie tego epiku planowane po aktualizacji architektury przez `bmad-architecture` (AD-2, AD-3 nieaktualne; Identity, globalny filtr, sprawdzanie stanu Konta, CI/CD, wdrożenie). Architektura zaktualizowana 31.10.2026 (AD-1…AD-22).
- Decision (31.10.2026): strona `/konto/zmiana-hasla` (wymuszona i dobrowolna) należy do tego epiku (AD-15); część R25 przeniesiona z epic-miesiac-i-zestawienie.
- Decision (31.10.2026): brak `after` na epic-szkielet-i-slowniki, bo ten na niego czeka; historie tego epiku zależą od zrobionej historii 1.1.
- Decision (31.10.2026): epik dopracowany wyłącznie z wymagań inicjatywy (R15, R18–R20, R24, R25, R27–R30) i architektury AD-1…AD-22, bez dodatkowych wejść (Autor).
- Decision (31.10.2026): pełny schemat i logowanie przed pierwszym wdrożeniem, bo AD-21 zamraża `Initial` w historii, która pierwsza wdraża na serwer.
- Decision (31.10.2026): tracer bullet to historia 2 „Nowy schemat Initial z Identity i UserId”; najmniej pewna jest historia 8 „Wdrożenie na Droplet”; historie 1 i 2 mogą iść równolegle.
- Decision (31.10.2026): logowanie w dwóch historiach (3: rejestracja, logowanie, wylogowanie; 4: pierwszy Administrator, stan Konta w sesji, zmiana hasła) — jedna przekraczałaby sesję agenta (Autor).
- Decision (31.10.2026): zamykająca historia „Refactor sweep”; bez historii testów end-to-end — źródła nie mają planu testów.
- Decision (31.10.2026): subdomena aplikacji `czas-pracy.kandula-consulting.pl` (domena `kandula-consulting.pl` kupiona przez Autora); wartość `SITE_DOMAIN` w `.env` serwera.
- Decision (31.10.2026): historie 1 i 2 bez `after` na „1.1” — historia 1.1 jest zrobiona, a wpis tworzyłby konflikt kolejności z epic-szkielet-i-slowniki, który czeka na ten epik; przekazanie opisuje Description.
- Decision (31.10.2026): po kontroli zestawu (Autor): kontrola migracji w historii 1, żeby 1 i 2 szły równolegle; `CurrentUser` w historii 3 (potrzebuje uwierzytelniania); `IFluentLocalizer` w historii 3 (pierwszy ekran); historia wdrożenia podzielona na 5 „Obraz aplikacji gotowy do pracy za proxy” (sprawdzalny lokalnie) i 8 „Wdrożenie na Droplet” (pierwsze wdrożenie); `/health` zwraca tag obrazu do sprawdzenia rollbacku; kolejność kroków GHCR: pierwszy workflow, przełączenie na publiczny, ponowne uruchomienie.
- Decision (31.10.2026): bez tymczasowego przycisku Wyloguj — historia 3 sprawdza wylogowanie POST-em i testem, przycisk w belce sprawdza historia 1.3 (Autor).
- Decision (31.10.2026): rejestracja i pierwszy Administrator nie tworzą wiersza `account_settings` — powstaje leniwie w epic-miesiac-i-zestawienie (decyzja Autora w przeglądzie historii 4.2; sprzeczność AD-7/AD-21 z AD-16).
