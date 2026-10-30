# Układ Zestawienia

Źródło: prd-rejestr-czasu FR-11, FR-12 i addendum PRD (odczyt oryginału z września 2026). Dotyczy R11, R12.

## Arkusz

- Jeden arkusz o nazwie „<Miesiąc RRRR>” w mianowniku, np. „Wrzesień 2026”.
- Wiersz 1: nagłówek, zamrożony (okienko od `A2`).
- Od wiersza 2: jeden Wiersz zestawienia na wiersz arkusza; dni chronologicznie, tylko dni z Wpisami; w dniu według kodu Projektu, potem nazwy Typu pracy.

## Kolumny

| Kolumna (nagłówek) | Zawartość |
|---|---|
| A „Dzień tygodnia” | np. „Środa”; tylko w pierwszym wierszu dnia |
| B „Data” | prawdziwa data Excela w formacie `dd.mm.rrrr`; tylko w pierwszym wierszu dnia |
| C „Kod” | kod Projektu |
| D „Typ” | nazwa Typu pracy |
| E „Opis” | opisy złączonych Wpisów, każdy w nowej linii, w kolejności dodania; zawijanie tekstu włączone |
| F „Godziny” | Czas w godzinach dziesiętnych, format ogólny (4; 1,5; 0,75) |
| G „Suma dnia” | tylko w pierwszym wierszu dnia |
| H „Suma tygodnia” | tylko w pierwszym wierszu tygodnia (pierwszy dzień tygodnia z Wpisami w danym Miesiącu); tydzień bez Wpisów nie ma wiersza ani sumy |

## Wiersz RAZEM

Po ostatnim Wierszu zestawienia jeden pusty wiersz, a pod nim pogrubiony wiersz RAZEM:
- E: „RAZEM <MIESIĄC RRRR>” wielkimi literami, np. „RAZEM WRZESIEŃ 2026”;
- F: suma kolumny F;
- H: suma Sum tygodnia; F i H są równe sumie Miesiąca.

Wszystkie sumy są wartościami liczonymi przez aplikację, nie formułami.

## Kontrola: wrzesień 2026

28 wierszy danych (2–29), pusty wiersz 30, RAZEM w wierszu 31. Sumy tygodnia: 30 / 16 / 18 / 25 / 14 = 103 h. Przy Teście wrześniowym liczba Wierszy zestawienia może spaść poniżej 28, jeśli wpis „WKU, RRJ” po przypisaniu do jednego Projektu złączy się z innym; porównuje się sumy, nie liczbę wierszy.
