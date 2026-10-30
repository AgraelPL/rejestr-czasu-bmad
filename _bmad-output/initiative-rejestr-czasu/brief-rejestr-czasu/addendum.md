# Addendum: Rejestr czasu pracy

Szczegóły, które nie mieszczą się w briefie, a przydadzą się w PRD i architekturze.

## Układ zestawienia miesięcznego wymagany przez odbiorcę

Źródło: `docs/projekt-testowy-material.md` §1 (analiza zestawienia za wrzesień 2026). Plik xlsx z danymi klientów nie trafia do repozytorium.

Arkusz o nazwie „<Miesiąc RRRR>” (np. „Wrzesień 2026”), wiersz nagłówka zamrożony, jeden wiersz na wpis po złączeniu. Wpisy z tego samego dnia o tym samym projekcie i typie pracy łączą się w jeden wiersz: godziny się sumują, opisy trafiają do kolejnych linii tej samej komórki (zob. brief, „Rozwiązanie”).

| Kolumna | Zawartość | Uwagi z obecnego arkusza |
|---|---|---|
| A Dzień tygodnia | „Środa” | tylko w pierwszym wierszu dnia |
| B Data | „02.09.2026” | tekst `dd.mm.rrrr`; tylko w pierwszym wierszu dnia |
| C Kod | `RDZ`, `WKU`, `ORG`… | kod projektu |
| D Typ | `Development` / `Analiza bis-sys` | |
| E Opis | tekst | |
| F Godziny | 4 | liczby całkowite |
| G Suma dnia | | formuła z ręcznie dobranym zakresem |
| H Suma tygodnia | | tydzień pn–nd; formuła z ręcznie dobranymi komórkami |

Pod ostatnim wpisem wiersz RAZEM: suma kolumny F i suma kolumny H.

Skala na przykładzie września 2026: 28 wpisów, 20 dni pracy (w tym 5 weekendowych), do 3 wpisów i do 12 h dziennie, 7 kodów projektów, 2 typy pracy, łącznie 103 h.

## Pytania do PRD

Sprawy, które nie zmieniają kształtu produktu, ale trzeba je rozstrzygnąć przed napisaniem historii. Pytania 1 i 2 wpływają na wynik testu wrześniowego.

1. **Tydzień na przełomie miesięcy.** Suma tygodnia (pn–nd) w eksporcie: tylko dni z danego miesiąca czy cały tydzień? Sumy tygodni z września (30 / 16 / 18 / 25 / 14) sugerują pierwszy wariant; trzeba to sprawdzić w oryginale.
2. **Dni bez wpisów.** Czy w eksporcie pojawiają się puste wiersze dla dni bez pracy? We wrześniu ich nie było: arkusz zawiera tylko 20 dni z wpisami.
3. **Format daty w kolumnie B.** Data jako tekst `dd.mm.rrrr` (jak w obecnym arkuszu) czy jako prawdziwa data Excela w tym formacie? Dla odbiorcy wygląda tak samo, ale prawdziwą datę da się filtrować.
4. **Kolejność wierszy w dniu.** Po złączeniu: według kodu projektu, typu pracy czy kolejności wpisania?
5. **Kopie bazy.** Ile ostatnich kopii trzymać (brief: „kilka”) i jak nazywać pliki kopii?
6. **Kopiowanie wpisu.** Kopia z dzisiejszą datą (założenie briefu) czy z datą oryginału?
7. **Format godzin w kolumnie F.** Liczba z formatem `0,00` czy z formatem ogólnym (1,5 zamiast 1,50)?
8. **Słowniki startowe.** 7 projektów i 2 typy pracy wpisywane ręcznie przy pierwszym uruchomieniu czy przez skrypt startowy (bez nazw klientów w repozytorium)?
