---
type: epic
title: "Konta i wdrożenie"
parent: initiative-rejestr-czasu
covers: [R15, R18, R19, R20, R24, R27, R28, R29, R30]
sources: []
companions: []
after: []
refined: false
risk: high
---

# Konta i wdrożenie

## Description

Platforma, na której stoją pozostałe epiki. Każda propozycja zmiany przechodzi build i testy, a merge do gałęzi głównej wdraża aplikację na serwer pod subdomeną z HTTPS. Przepisany schemat `Initial` ma `UserId` z globalnym filtrem oraz dane Konta (stan, data rejestracji, ostatnie logowanie, licznik nieudanych prób). Działają rejestracja, logowanie, wylogowanie, role User i Administrator, pierwszy Administrator z sekretu i sprawdzanie stanu Konta przy każdej akcji. Buduje na zrobionej historii 1.1 z epic-szkielet-i-slowniki. Daje epic-szkielet-i-slowniki `UserId` z filtrem, logowanie, role, autoryzację tras, układ dla niezalogowanych i potok wdrożenia; daje epic-administracja-kont-i-pomoc stan Konta, jego dane i sprawdzanie stanu.

## Outcome

Autor rejestruje się i loguje na serwerze pod subdomeną, a jego merge do gałęzi głównej trafia tam bez ręcznych kroków (R18–R20, R28, R29; prd SM-4).

## Done when

- Propozycja zmiany z błędem builda albo testu nie może zostać włączona do gałęzi głównej (R28).
- Merge do gałęzi głównej wdraża aplikację na serwer, sprawdzenie stanu po wdrożeniu przechodzi, a nieudane kończy wdrożenie błędem bez automatycznego powrotu (R29).
- Rollback do wskazanej wcześniejszej wersji przećwiczony raz na serwerze (R30).
- Na serwerze: rejestracja daje Konto Oczekuje, pierwszy Administrator powstaje z sekretu, Konto Aktywne loguje się, 5 nieudanych prób wstrzymuje logowanie na 15 minut, wylogowanie działa (R18–R20).
- Testy automatyczne oddzielenia danych po `UserId` przechodzą w potoku; schemat `Initial` przepisany z `UserId` przed pierwszym wdrożeniem (R24; prd NFR-9).
- Instrukcja administratora w `docs/admin/` opisuje serwer, domenę, sekrety, SSH, dostęp do bazy tylko do odczytu, wdrożenie i rollback (R27).

## Boundaries

- Ekran Użytkownicy, odrzucanie, blokowanie, usuwanie Kont i ustawianie hasła przez Administratora należą do epic-administracja-kont-i-pomoc; ten epik daje tylko stan Konta i jego sprawdzanie.
- Układ z menu, belka i mechanizm pomocy należą do epic-szkielet-i-slowniki.
- Ustawienia konta należą do epic-miesiac-i-zestawienie.
- Strona o odtwarzaniu z Kopii serwera i ćwiczenie odtworzenia należą do epic-administracja-kont-i-pomoc.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R15, R18–R20, R24, R27–R30
- prd addendum — _bmad-output/initiative-rejestr-czasu/prd-rejestr-czasu/addendum.md, Wdrożenie i infrastruktura
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Decision (31.10.2026): historie tego epiku planowane po aktualizacji architektury przez `bmad-architecture` (AD-2, AD-3 nieaktualne; Identity, globalny filtr, sprawdzanie stanu Konta, CI/CD, wdrożenie).
- Decision (31.10.2026): brak `after` na epic-szkielet-i-slowniki, bo ten na niego czeka; historie tego epiku zależą od zrobionej historii 1.1.
