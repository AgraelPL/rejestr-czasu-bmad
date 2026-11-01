---
type: epic
title: "Administracja Kont i pomoc"
parent: initiative-rejestr-czasu
covers: [R21, R22, R23, R26]
sources: []
companions: []
after: []
refined: false
risk: medium
---

# Administracja Kont i pomoc

## Description

Administrator prowadzi Konta na ekranie Użytkownicy: zatwierdza, odrzuca, blokuje, odblokowuje i usuwa je z kaskadą w jednej operacji, a zapomniane hasło ustawia ręcznie z wymuszoną zmianą przy pierwszym logowaniu. Epik tylko zmienia stan Konta; jego sprawdzanie przy każdej akcji daje epic-konta-i-wdrozenie. Pozycja Użytkownicy w menu z epic-szkielet-i-slowniki zaczyna prowadzić do tego ekranu. Epik zamyka pomoc (strona o administracji, strony ekranów bez logowania) i jest sprawdzany lokalnie; próba oddzielenia danych na serwerze i odtworzenie z Kopii serwera należą do epic-wdrozenie-na-serwer. Może iść równolegle z epikami 1–3.

## Outcome

Administrator prowadzi Konta na ekranie Użytkownicy bez poleceń SQL, więc przed Miesiącem „na serio” znajomy może dostać własne, zatwierdzone Konto (R21–R23).

## Done when

- Lokalnie Administrator zatwierdza, odrzuca, blokuje, odblokowuje i usuwa Konto zgodnie z R21 i R23; nie może tego zrobić z własnym Kontem.
- Hasło ustawione przez Administratora wymusza ustawienie własnego przy pierwszym logowaniu (R22).
- Strona pomocy o administracji jest dostępna tylko dla Administratora; ekrany logowania, rejestracji i wymuszonej zmiany hasła mają strony pomocy (R26).

## Boundaries

- Rejestracja, logowanie, role, dane Konta i sprawdzanie stanu przy każdej akcji należą do epic-konta-i-wdrozenie.
- Ustawienia konta (prefiks, zmiana własnego hasła) należą do epic-miesiac-i-zestawienie.
- Mechanizm pomocy należy do epic-szkielet-i-slowniki; ten epik dopisuje tylko swoje strony.
- Próba SM-5 na serwerze (część R24) i odtworzenie z Kopii serwera ze stroną `docs/admin/` (część R27) należą do epic-wdrozenie-na-serwer.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R21–R23, R26
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Waits on epic-konta-i-wdrozenie because: Konta z danymi (stan, data rejestracji, ostatnie logowanie), role, logowanie i sprawdzanie stanu Konta przy każdej akcji.
- Decision (01.11.2026): Autor odkłada wdrożenie na Droplet na koniec, po dostarczeniu funkcji z epików 1–3 i 5: migracja `Initial` zostaje otwarta do pierwszego wdrożenia (NFR-9), bez kosztów i obsługi serwera, zanim aplikacja jest użyteczna. Wdrożenie, rollback i sprawdzenia na serwerze przeszły do nowego epic-wdrozenie-na-serwer (epik 6), przed Miesiącem „na serio”. R24 (próba na serwerze) i R27 (odtworzenie z Kopii serwera) przeszły do epiku 6.
