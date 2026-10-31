---
type: epic
title: "Administracja Kont i pomoc"
parent: initiative-rejestr-czasu
covers: [R21, R22, R23, R24, R26, R27]
sources: []
companions: []
after: []
refined: false
risk: medium
---

# Administracja Kont i pomoc

## Description

Administrator prowadzi Konta na ekranie Użytkownicy: zatwierdza, odrzuca, blokuje, odblokowuje i usuwa je z kaskadą w jednej operacji, a zapomniane hasło ustawia ręcznie z wymuszoną zmianą przy pierwszym logowaniu. Epik tylko zmienia stan Konta; jego sprawdzanie przy każdej akcji daje epic-konta-i-wdrozenie. Pozycja Użytkownicy w menu z epic-szkielet-i-slowniki zaczyna prowadzić do tego ekranu. Epik zamyka pomoc (strona o administracji, strony ekranów bez logowania) i przed Miesiącem „na serio” sprawdza na serwerze oddzielenie danych oraz odtworzenie z Kopii serwera. Może iść równolegle z epikami 1–3.

## Outcome

Przed Miesiącem „na serio” znajomy ma własne, zatwierdzone Konto, a próba na serwerze pokazuje, że Autor i znajomy nie widzą swoich danych (R21–R24; prd SM-5).

## Done when

- Na serwerze Administrator zatwierdza, odrzuca, blokuje, odblokowuje i usuwa Konto zgodnie z R21 i R23; nie może tego zrobić z własnym Kontem.
- Hasło ustawione przez Administratora wymusza ustawienie własnego przy pierwszym logowaniu (R22).
- Próba SM-5 na serwerze: Autor i znajomy z własnymi Projektami, Typami pracy i Wpisami nie widzą swoich danych w Liście wpisów, Widoku miesiąca ani Zestawieniu; ten sam kod Projektu u obu nie koliduje (R24).
- Strona pomocy o administracji jest dostępna tylko dla Administratora; ekrany logowania, rejestracji i wymuszonej zmiany hasła mają strony pomocy (R26).
- Odtworzenie z Kopii serwera przećwiczone na tymczasowym serwerze bez ruszania produkcji, opisane w `docs/admin/` (R27; prd SM-4).

## Boundaries

- Rejestracja, logowanie, role, dane Konta i sprawdzanie stanu przy każdej akcji należą do epic-konta-i-wdrozenie.
- Ustawienia konta (prefiks, zmiana własnego hasła) należą do epic-miesiac-i-zestawienie.
- Mechanizm pomocy należy do epic-szkielet-i-slowniki; ten epik dopisuje tylko swoje strony.

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R21–R24, R26, R27
- glossary — _bmad-output/initiative-rejestr-czasu/glossary.md

## Notes

- Waits on epic-konta-i-wdrozenie because: Konta z danymi (stan, data rejestracji, ostatnie logowanie), role, logowanie i sprawdzanie stanu Konta przy każdej akcji.
