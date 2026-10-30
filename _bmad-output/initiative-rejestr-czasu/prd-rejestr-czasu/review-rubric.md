# Ocena jakości PRD — Rejestr czasu pracy

Recenzja wg rubryki `.claude/skills/bmad-prd/assets/prd-validation-checklist.md`. Przeczytane: `prd-rejestr-czasu.md`, `addendum.md`. Kalibracja: projekt hobbystyczny/naukowy, jeden użytkownik, PRD celowo krótki (2–3 strony), po polsku; UJ celowo jednozdaniowe; założenia potwierdzone 30.10.2026. Zwięzłości uzasadnionej tą skalą nie zgłaszam.

## Werdykt ogólny

PRD jest dobry i gotowy do architektury: ma jasną tezę (wyeliminować konkretne błędy z września 2026 i dać eksport bez otwierania Excela), wymagania są w większości testowalne aż do poziomu kolumn i formatów, a SM-1 to konkretny test akceptacyjny z liczbami (30/16/18/25/14, RAZEM 103 h). Najsłabsze miejsce to mechanika: tabela w FR-12, najważniejszym wymaganiu dla Zestawienia, ma zdublowany nagłówek, który psuje renderowanie. Do tego dochodzi kilka drobnych luk w warunkach brzegowych (nieudana kopia w trakcie działania, wielkość liter w nazwie Typu pracy). Żadna z nich nie blokuje pracy dalej.

## Gotowość do decyzji — mocna

Decyzje są zapisane jako decyzje, razem z tym, z czego zrezygnowano. FR-6: „v1 nie ma zamykania Miesiąca”. FR-5: „Suma dnia powyżej 24 h nie jest blokowana”. §4.5: sumy zapisywane „jako wartości, nie formuły”. Addendum: „data w B i format F zmienione decyzją autora”, czyli świadome odejście od oryginału. NFR-2 zapisuje decyzję autora o kodach projektów w dokumentach planistycznych z datą. §8 „Otwarte pytania: Brak” jest wiarygodne, bo podaje, co i kiedy rozstrzygnięto (sprawdzenie xlsx 30.10.2026). Przeciwwskaźniki SM-C1 i SM-C2 mówią wprost, co przegrywa w razie konfliktu.

### Ustalenia
Brak.

## Treść zamiast teatru — mocna

Nie ma person (jest Autor i Odbiorca, oba potrzebne). Wizja jest specyficzna dla produktu: wymienia cztery konkretne błędy („złej daty, literówki w kodzie projektu, dwóch projektów w jednym wpisie, ręcznie liczonych sum”), a każdy z nich ma wymaganie, które go zamyka (FR-5 kalendarz i dzień tygodnia, listy wyboru, „dokładnie jeden Projekt”, FR-10/FR-12 sumy liczone przez aplikację). NFR mają progi specyficzne dla produktu (`127.0.0.1`, minuty całkowite, `npm start`), nie ogólniki.

### Ustalenia
- **low** Miękki próg w NFR-3 (§5 NFR-3): „zajmuje do ok. 10 sekund”. Słowo „ok.” osłabia jedyny próg wydajności, a warunek „samą klawiaturą” nie mówi, czy chodzi np. o Tab/Enter bez myszy w polach kalendarza i list. *Poprawka:* „do 10 s” i jedno zdanie: „wszystkie pola, w tym data i listy wyboru, obsługiwane klawiaturą; zapis klawiszem Enter”.

## Spójność strategiczna — mocna

Teza z §1 przekłada się na zakres: funkcje §4 służą albo wpisowi (UJ-1), albo zamknięciu miesiąca (UJ-2), albo prostemu pytaniu o godziny (UJ-3). Kryteria sukcesu sprawdzają tezę, nie aktywność: SM-1 to odtworzenie realnego miesiąca z wynikiem liczbowym, SM-2 to brak równoległych notatek, SM-3 to przyjęcie pliku bez poprawek. Są przeciwwskaźniki. Rodzaj MVP (rozwiązanie problemu) zgadza się z logiką zakresu. Cel osobny (nauka BMad, SM-4) jest oddzielony od celu produktowego i nie miesza priorytetów.

### Ustalenia
Brak.

## Jasność „gotowe” — mocna, z drobnymi lukami

Większość FR ma sprawdzalne skutki: FR-1 (wielkość liter, zapis wielkimi literami), FR-5 (zakres i krok Czasu), FR-10 (identyczność sum), FR-11 (reguła złączenia i kolejność), FR-12 (kolumna po kolumnie, wiersz RAZEM, nazwa arkusza), FR-13 (wzorzec nazwy, zachowanie dla pustego Miesiąca), FR-14 (moment, nazwa, rotacja, zachowanie przy błędzie startowym). Brakuje kilku warunków brzegowych.

### Ustalenia
- **medium** Nieudana kopia w trakcie działania nie ma określonego zachowania (§4.6 FR-14). Opisana jest tylko „nieudana kopia przy starcie”. Dla kopii „co najmniej raz na dobę” nie wiadomo, czy aplikacja ma działać dalej, czy pokazać komunikat, czy ponowić próbę. Nie wiadomo też, czy przy rotacji usunąć starą kopię przed sprawdzeniem, że nowa jest poprawna. Kopia bazy to jedyna ochrona danych (§6 „Brak synchronizacji”), więc warto to domknąć. *Poprawka:* dopisać np. „Nieudana kopia w trakcie działania nie przerywa pracy; Autor widzi ostrzeżenie w interfejsie. Rotacja usuwa najstarszą kopię dopiero po udanym zapisie nowej.”
- **low** Unikalność nazwy Typu pracy bez reguły wielkości liter (§4.1 FR-2). FR-1 mówi jasno „bez względu na wielkość liter” dla kodu Projektu, a FR-2 tylko „unikalna”. Implementujący zgadnie różnie. *Poprawka:* „unikalna bez względu na wielkość liter; zapisywana tak, jak wpisano”.
- **low** Kolejność Wpisów w obrębie dnia w Widoku miesiąca (§4.4 FR-9). Dni są „chronologicznie”, a w Zestawieniu kolejność wierszy dnia określa FR-11. Dla Widoku miesiąca nie jest określona, a FR-10 zakłada, że Autor porównuje oba widoki. *Poprawka:* „Wpisy jednego dnia w kolejności dodania” albo „jak w Zestawieniu (FR-11)”.
- **low** Brak domyślnego prefiksu (§4.5 FR-13). Nie wiadomo, co się dzieje, gdy prefiks nie jest skonfigurowany (błąd, nazwa bez prefiksu?). *Poprawka:* jedno zdanie, np. „bez prefiksu nazwa zaczyna się od `Zestawienie-…`”.

## Uczciwość zakresu — mocna

§6 „Poza celem” wykonuje realną pracę: wyklucza timer, import, CSV, zamykanie Miesiąca, przypomnienia, autostart i synchronizację, czyli rzeczy, które ktoś mógłby przyjąć za oczywiste. Ryzyko braku kopii poza dyskiem jest nazwane jako przyjęte w briefie. Liczba otwartych elementów wynosi zero, co pasuje do PRD gotowego do budowy, a §9 wyjaśnia, dokąd trafiło osiem założeń.

### Ustalenia
Brak.

## Użyteczność dla kolejnych etapów — adekwatna

PRD zasila architekturę i historie, więc ten wymiar się liczy. Słownik jest obecny i stosowany konsekwentnie (wielkie litery dla terminów: Wpis, Czas, Zestawienie). Identyfikatory są ciągłe: FR-1…FR-15, NFR-1…5, UJ-1…3, SM-1…4, SM-C1…C2. Sekcje §4 wskazują UJ, które realizują, a SM wskazują FR, które walidują. Obniża ocenę błąd w tabeli FR-12 i kolizja nazw opisane niżej.

### Ustalenia
- **medium** Zdublowany nagłówek tabeli kolumn (§4.5 FR-12, linie tabeli 1–4). Tabela zaczyna się od `| Kolumna | Zawartość |` + `|---|---|`, a zaraz potem jest drugi nagłówek `| Kolumna (nagłówek) | Zawartość |` + `|---|---|`. Po renderowaniu drugi nagłówek staje się wierszem danych, a drugi separator wierszem z treścią „---”. FR-12 jest najważniejszą specyfikacją dla SM-1 i SM-3, a agent wyciągający tabelę może potraktować te wiersze jako kolumny. *Poprawka:* usunąć pierwsze dwie linie tabeli i zostawić nagłówek „Kolumna (nagłówek) | Zawartość”.
- **low** Termin „Słownik” w dwóch znaczeniach (§0, §3, §4.1). §0 odsyła do „Słownika (§3)” jako glosariusza dokumentu, a §3 definiuje **Słownik** jako „lista Projektów albo lista Typów pracy” (domena), której używa §4.1 „Słowniki” i FR-3/FR-4. Definicja Projektu mówi „pozycja słownika” małą literą. *Poprawka:* nazwać §3 „Pojęcia” (lub „Glosariusz”) i poprawić odwołanie w §0; w definicjach Projektu i Typu pracy pisać „pozycja Słownika”.

## Dopasowanie kształtu — mocne

Narzędzie dla jednego operatora, hobby: PRD ma kształt specyfikacji funkcji, a trzy jednozdaniowe UJ wystarczają (każde ma bohatera, Autora, i jest powiązane z sekcją §4). Nie ma nadmiaru formalizmu (brak person, brak sekcji innowacji, brak macierzy śledzenia). Szczegóły techniczne są w addendum, nie w PRD. Poziom rygoru w FR-12 jest wyższy niż reszta, i słusznie, bo zgodność z oczekiwanym układem Odbiorcy to kryterium SM-3.

### Ustalenia
Brak.

## Uwagi mechaniczne

- Tabela FR-12: zdublowany nagłówek i separator (patrz wyżej).
- „Słownik” jako nazwa sekcji §3 i jako termin domenowy (patrz wyżej).
- Indeks założeń: brak znaczników `[ASSUMPTION]` w treści, a §9 wymienia 8 potwierdzonych pozycji (FR-1, FR-3, FR-5 ×2, FR-7, FR-11, FR-13, FR-14), czyli liczba się zgadza. Rundy nie da się sprawdzić bez poprzedniej wersji; to akceptowalne.
- Spójność liczb: SM-1 (tygodnie 30/16/18/25/14, 103 h) zgadza się z addendum (5 tygodni: H2+H6+H11+H18+H26; wrzesień 2026 zaczyna się we wtorek, więc ma 5 tygodni pn–nd w obrębie Miesiąca). 28 Wpisów w SM-1 zgadza się z 28 wierszami danych w oryginale.
- Odwołania krzyżowe rozwiązują się poprawnie: FR-12 w Słowniku (Zestawienie), SM-1 w Słowniku (Test wrześniowy), UJ w opisach §4.
- Addendum odwołuje się do `docs/projekt-testowy-material.md` jako lokalnego źródła. Recenzja go nie otwierała (dane prywatne), więc zgodność opisu oryginału przyjęto na podstawie addendum.
