# Recenzja: PRD Rejestr czasu pracy, szlif końcowy (struktura, potem styl)

Data: 31.10.2026. Recenzowane: `prd-rejestr-czasu.md` (wersja 31.10.2026) i `addendum.md`. Soczewki: `structure`, potem `prose` (bmad-review).

**Odczyt celu:** PRD istnieje po to, by Autor i agenci BMad (architektura, epiki, historie) zbudowali aplikację rejestru czasu, teraz wieloużytkownikową i wdrażaną na serwer, bez ponownego otwierania zatwierdzonych decyzji. Addendum niesie decyzje techniczne, infrastrukturę z forge i dowody na układ Zestawienia.
**Model struktury:** Strategic/Context (piramida), właściwy dla PRD. Czytelnik: ludzie + agenci LLM.
**Rozmiar:** PRD 3763 słowa, addendum 638 słów (word_metrics).
**Ograniczenia:** treść nienaruszalna; żadne znaczenie ani decyzja się nie zmienia; numery FR/NFR/SM i numery sekcji (§4.1…, §6.1) zostają bez zmian. Wiersze oznaczone **[ZNACZENIE?]** mogą być odczytane jako zmiana sensu — Autor potwierdza je osobno.

Format: lokalizacja, tag, obecny tekst (dosłownie), proponowany tekst, uzasadnienie, wpływ na liczbę słów.

---

## Soczewka: struktura (9)

### S1. PRD §8 „Ustalone”: odwołanie do K1–K17 nierozwiązywalne dla czytelnika
Tag: CONDENSE. (Znana pozycja (a) z recenzji rubryką.) Uwaga: w aktualnej wersji §0 **nie** zawiera już „K1–K17”; odwołanie zostało tylko w §8. Identyfikatory K istnieją wyłącznie w `.memlog.md`.
Obecnie:
```
- Decyzje z 31.10.2026 (K1–K17) i siedem założeń wersji z kontami i wdrożeniem (FR-17, FR-18, FR-20, NFR-6, NFR-7, SM-1, SM-4) Autor potwierdził 31.10.2026; są wpisane w wymagania jako ustalenia.
```
Proponowane:
```
- Decyzje z aktualizacji po forge (31.10.2026) i siedem założeń wersji z kontami i wdrożeniem (FR-17, FR-18, FR-20, NFR-6, NFR-7, SM-1, SM-4) Autor potwierdził 31.10.2026; są wpisane w wymagania jako ustalenia.
```
Uzasadnienie: czytelnik PRD nie ma dostępu do kluczy K; wymagania i tak zawierają treść decyzji. Wpływ: +1 słowo.

### S2. Addendum, „Okno na przepisanie schematu (K8)”: ten sam problem co S1
Tag: CONDENSE. Powiązane z S1 i z PRD §8 (które cytuje nagłówek punktu bez „K8”).
Obecnie:
```
- **Okno na przepisanie schematu (K8):** historia 1.1 zbudowała migrację `Initial` bez `UserId`.
```
Proponowane:
```
- **Okno na przepisanie schematu:** historia 1.1 zbudowała migrację `Initial` bez `UserId`.
```
Uzasadnienie: „K8” nie jest nigdzie zdefiniowane w PRD ani addendum; PRD §8 i tak odwołuje się do punktu po nazwie. Wpływ: −1 słowo.

### S3. Addendum „Decyzje techniczne”: zastrzeżenie o AD-2/AD-3 schowane pod listą
Tag: MOVE. Pierwszy akapit mówi „Obowiązują decyzje… (AD-1–AD-14)”, a dopiero po liście (wiersz 15) czytelnik dowiaduje się, że AD-2 i AD-3 są zastąpione. Krytyczna informacja jest zakopana.
Obecnie (dwa miejsca):
```
Obowiązują decyzje z kręgosłupa architektury [`architecture-rejestr-czasu.md`](../architecture-rejestr-czasu/architecture-rejestr-czasu.md) (AD-1–AD-14, 30.10.2026). Zastąpiły pierwotne założenia briefu (Node.js, SQLite w jednym pliku, `npm start`).
…
Do aktualizacji w architekturze (forge 31.10.2026 je zastępuje): AD-2 (…), AD-3 (Kopia bazy przy starcie).
```
Proponowane: przenieść zdanie „Do aktualizacji w architekturze…” bezpośrednio za pierwszy akapit (przed listę), bez zmiany treści (z poprawką stylu z P12).
Uzasadnienie: front-loading — zakres obowiązywania AD widać od razu. Wpływ: 0 słów.

### S4. Addendum: rezygnacja z kopii bazy opisana trzy razy
Tag: CONDENSE. Ta sama informacja: wiersz 12 (przekreślona pozycja listy), wiersz 15 (AD-3), wiersz 39 („Odrzucone”: historia 1.2), plus wiersz 24 („Brak kodu kopii w aplikacji”).
Obecnie:
```
- ~~Kopia bazy przy starcie przez `pg_dump`~~ — wypada 31.10.2026 (forge; historia 1.2 usunięta z planu). Kopie zapewnia dostawca chmury.
```
Proponowane: usunąć tę pozycję z listy „Decyzje techniczne”; zostają AD-3 w zdaniu „Do aktualizacji…”, wpis w „Odrzucone (z forge)” i punkt „Kopie (Kopia serwera)”. Jeśli Autor chce zachować ślad historyczny w liście (przekreślenie jako sygnał dla agentów czytających stary plan) — PRESERVE.
Uzasadnienie: jedno źródło prawdy dla decyzji o kopiach. Wpływ: −19 słów.

### S5. PRD §1 Wizja: zapis historii decyzji w sekcji wizji
Tag: MOVE (QUESTION). (Pozostałość po wersji jednoosobowej/lokalnej, pozycja (c).)
Obecnie:
```
Wcześniejsza zasada, że dane nie opuszczają komputera Autora, była jego własną decyzją i została uchylona 31.10.2026; ani Odbiorca, ani klienci nie stawiają warunków co do miejsca danych.
```
Proponowane: Consider: przenieść to zdanie do §8 „Ustalone” jako pozycję listy:
```
- Zasada „dane nie opuszczają komputera Autora” (brief) była decyzją Autora i została przez niego uchylona 31.10.2026; ani Odbiorca, ani klienci nie stawiają warunków co do miejsca danych.
```
Uzasadnienie: Wizja opisuje stan docelowy; rekord zmiany decyzji pasuje do §8, gdzie są inne ustalenia z datami. Treść bez zmian. Alternatywa (PRESERVE): zostawić, bo czytelnik briefu od razu widzi, że zasada przestała obowiązywać. Wpływ: ~0 słów.

### S6. PRD §0: numeracja FR nie biegnie w kolejności sekcji
Tag: QUESTION (dodanie rusztowania). FR-16 stoi w §4.3, FR-14/FR-15 w §4.9 na końcu, po FR-29. §0 mówi, że numery są stałe, ale nie mówi wprost, że nie odpowiadają kolejności sekcji.
Obecnie:
```
Wymagania są pogrupowane w funkcje, numerowane globalnie (FR-1…); numery są stałe, usunięte wymagania zostają oznaczone.
```
Proponowane:
```
Wymagania są pogrupowane w funkcje, numerowane globalnie (FR-1…); numery są stałe i nie muszą iść w kolejności sekcji, usunięte wymagania zostają oznaczone.
```
Uzasadnienie: czytelnik (i agent) nie szuka FR-14 między FR-13 a FR-17. Bez przenumerowania. Wpływ: +6 słów.

### S7. PRD FR-16 w §4.3 Lista wpisów, choć dotyczy też Widoku miesiąca i Słowników
Tag: PRESERVE + dodać odsyłacze. Przeniesienie do nowej sekcji wymusiłoby zmianę numerów sekcji (§4.4…, cytowanych w §4), więc lepiej zostawić miejsce i dodać odsyłacze.
Obecnie (§4.1 Opis i §4.4 Opis):
```
**Opis:** User sam prowadzi oba Słowniki; na nowym Koncie są puste …
**Opis:** Kontrola przed eksportem, odpowiednik dotychczasowego arkusza. Realizuje UJ-2.
```
Proponowane: dopisać na końcu obu opisów „Siatka i filtry: FR-16.”
Uzasadnienie: wymaganie wspólne dla trzech ekranów jest dziś widoczne tylko z jednego; FR-9 już odsyła do FR-16, §4.1 nie. Wpływ: +6 słów.

### S8. PRD §4.7 i §4.9 bez „Opis:”, w odróżnieniu od pozostałych podsekcji §4
Tag: QUESTION. Wszystkie inne podsekcje §4 mają akapit „**Opis:**” (zwykle „Realizuje UJ-x”). §4.7 i §4.9 mają 0 słów wstępu.
Proponowane: Consider: albo dodać krótkie opisy, które tylko odsyłają do istniejącej treści, np. §4.7 „**Opis:** Pomoc dla Userów w aplikacji i instrukcja dla Administratora poza nią.”, §4.9 „**Opis:** Gdzie leżą dane i konfiguracja; kopie zapewnia Kopia serwera (§6.1).” — albo świadomie zostawić bez opisu.
Uzasadnienie: spójny schemat ułatwia skanowanie. Wpływ: +~20 słów.

### S9. Elementy, które wyglądają na zbędne, ale zostają
Tag: PRESERVE.
- §6 „Brak kodu kopii w aplikacji i osobnego środowiska przedprodukcyjnego” powtarza FR-14 i NFR-4 — wzmocnienie w liście „poza zakresem”, chroni przed dokładaniem zakresu.
- §3 Administrator, §4.6 Opis i §6.1 „Dostęp Administratora” trzykrotnie mówią o dostępie Administratora — każda wzmianka ma inną rolę (definicja, reguła, ryzyko); dotyczy prywatności, więc powtórzenie jest uzasadnione.
- Kolejność SM-1, SM-2, SM-3, SM-5, potem SM-4 — wynika z grupowania (Rejestr / Nauka BMad); numery są stałe.
- Addendum „Odrzucone (z forge)” powtarza część §6 PRD, ale dodaje uzasadnienia odrzucenia — to nie redundancja.
Wpływ: 0.

**Podsumowanie struktury:** 9 rekomendacji (w tym 1 PRESERVE). Po przyjęciu wszystkich: ok. −19 słów w addendum (−3%), ok. +30 słów w PRD (+0,8%); długość nie jest celem, zyskiem jest rozwiązywalność odwołań i front-loading. Brak kosztów dla zrozumienia.

---

## Soczewka: styl (14)

Odczyt stylu: zwięzły, rzeczowy polski techniczny; terminy z §3 pisane wielką literą jako nazwy własne (Wpis, Projekt, Miesiąc, Konto); celowe zapożyczenie „User/Userzy/Userowi” — zachowujemy. Zdania równoważnikowe w NFR i addendum są zamierzone.

### P1. Konto/konto — niespójna wielkość liter (pozycja (b))
§3 definiuje **Konto** jako termin; FR-17–FR-22 i §4.1 piszą „Konto”, ale w §0–§3 i §8 jest „konto”. Uwaga: „utrata konta u dostawcy” (§6.1) to konto w DigitalOcean — **inne pojęcie, zostaje małą literą**. „Ustawienia konta” to nazwa ekranu — zostaje bez zmian.
| Lokalizacja | Obecnie | Proponowane |
|---|---|---|
| §0 | „wprowadza zakres z …: konta, role, wdrożenie” | „…: Konta, role, wdrożenie” |
| §2 Znajomy | „drugie konto, zaproszone linkiem przez Autora” | „drugie Konto, zaproszone linkiem przez Autora” |
| §2 Znajomy | „a jego konto pokazuje, że rejestracja” | „a jego Konto pokazuje, że rejestracja” |
| §2.1 | „przyjąć nowe konto, pomóc przy zapomnianym haśle, usunąć konto” | „przyjąć nowe Konto, pomóc przy zapomnianym haśle, usunąć Konto” |
| §2.2 UJ-4 (tytuł) | „**UJ-4. Nowe konto.**” | „**UJ-4. Nowe Konto.**” |
| §2.2 UJ-4 | „Znajomy zakłada konto (login, hasło)” | „Znajomy zakłada Konto (login, hasło)” |
| §2.2 UJ-4 | „widzi, że konto Oczekuje na zatwierdzenie. Autor na ekranie Użytkownicy zatwierdza konto.” | „widzi, że Konto jest w stanie Oczekuje. Autor na ekranie Użytkownicy zatwierdza Konto.” |
| §3 Autor | „ma konto z rolami User i Administrator” | „ma Konto z rolami User i Administrator” |
| §3 Administrator | „zatwierdza, blokuje i usuwa konta” | „zatwierdza, blokuje i usuwa Konta” |
| §8 | „siedem założeń wersji z kontami i wdrożeniem” | „siedem założeń wersji z Kontami i wdrożeniem” |
Zmiana w UJ-4 („jest w stanie Oczekuje”) ujednolica też użycie nazwy stanu z FR-17/FR-20/FR-22 („w stanie Oczekuje”).

### P2. Miesiąc/miesiąc „na serio” — niespójna wielkość liter
SM-2, SM-5, §6.1, §2.2 używają „Miesiąc „na serio””, a SM-1 i SM-4 — „miesiąc „na serio””.
| Lokalizacja | Obecnie | Proponowane |
|---|---|---|
| SM-1 | „Niepowodzenie przesuwa pierwszy miesiąc „na serio” o miesiąc.” | „Niepowodzenie przesuwa pierwszy Miesiąc „na serio” o miesiąc.” |
| SM-4 | „Po pierwszym miesiącu „na serio” powstaje notatka” | „Po pierwszym Miesiącu „na serio” powstaje notatka” |
UJ-2 „Pierwszego dnia nowego miesiąca” może zostać (dzień kalendarza, nie jednostka Zestawienia) — Consider ujednolicenie na „Miesiąca”?

### P3. FR-14: niezgodny rodzaj gramatyczny w nagłówku i treści
§0 mówi o „usuniętych wymaganiach” (rodzaj nijaki); nagłówek „Kopia bazy — usunięty” łączy żeński tytuł z męską formą.
Obecnie:
```
#### FR-14: Kopia bazy — usunięty 31.10.2026
Zastąpiony Kopią serwera (§6.1); aplikacja nie zawiera kodu kopii.
```
Proponowane:
```
#### FR-14: Kopia bazy (wymaganie usunięte 31.10.2026)
Zastąpione Kopią serwera (§6.1); aplikacja nie zawiera kodu kopii.
```

### P4. §8 „Ustalone”: odwołanie do usuniętego FR-14 bez oznaczenia
Obecnie:
```
- Osiem założeń pierwszej wersji (FR-1, FR-3, FR-5 ×2, FR-7, FR-11, FR-13, FR-14) Autor potwierdził 30.10.2026.
```
Proponowane:
```
- Osiem założeń pierwszej wersji (FR-1, FR-3, FR-5 ×2, FR-7, FR-11, FR-13, FR-14 — dziś usunięte) Autor potwierdził 30.10.2026.
```
Uzasadnienie: czytelnik nie szuka treści założenia w FR-14, które już jej nie ma.

### P5. NFR-1: niejasne „na to”
Obecnie:
```
wszystkie zasoby są serwowane z aplikacji, a polityka CSP na to nie pozwala.
```
Proponowane: Consider:
```
wszystkie zasoby są serwowane z aplikacji, a polityka CSP blokuje ładowanie zasobów z innych źródeł.
```
Uzasadnienie: „na to” gramatycznie wskazuje na serwowanie z aplikacji, czyli odwrotnie do intencji (CSP zabrania połączeń zewnętrznych).

### P6. UJ-1: powtórzone „otwiera” i dwuznaczne „otwiera ekran Nowy wpis i notatki”
Obecnie:
```
Autor otwiera adres aplikacji w przeglądarce i loguje się, otwiera ekran Nowy wpis i notatki z ostatnich 3–4 dni, po czym wpisuje
```
Proponowane:
```
Autor otwiera adres aplikacji w przeglądarce, loguje się i przechodzi do ekranu Nowy wpis; obok ma notatki z ostatnich 3–4 dni. Po kolei wpisuje
```
(dalsza część zdania bez zmian: „6–12 Wpisów: ustawia datę…”).

### P7. FR-18: nazwy stanów bez „w stanie” i „i” zamiast „lub”
Obecnie:
```
Zalogować się może tylko Konto w stanie Aktywne; Konto Oczekuje i Zablokowane dostaje komunikat o swoim stanie.
```
Proponowane:
```
Zalogować się może tylko Konto w stanie Aktywne; Konto w stanie Oczekuje lub Zablokowane dostaje komunikat o swoim stanie.
```

### P8. §6 pierwsza pozycja: niejasne wyliczenie „…między Userami, zespołów, wglądu…”
Obecnie:
```
- Nie jest to narzędzie zespołowe: brak współdzielenia danych między Userami, zespołów, wglądu Administratora w treść danych w aplikacji.
```
Proponowane:
```
- Nie jest to narzędzie zespołowe: brak zespołów, współdzielenia danych między Userami i wglądu Administratora w treść danych w aplikacji.
```

### P9. §6.1 „Utrata danych”: skrót „utratę do Miesiąca”
Obecnie:
```
Autor akceptuje utratę do Miesiąca (31.10.2026).
```
Proponowane:
```
Autor akceptuje utratę danych do całego Miesiąca (31.10.2026).
```
Uzasadnienie: zgodnie z nagłówkiem punktu; „do Miesiąca” czyta się jak „do terminu”.

### P10. NFR-6: „Walidacja i mniej istotne operacje w pasku komunikatu”
Obecnie:
```
Walidacja i mniej istotne operacje w pasku komunikatu, każde potwierdzenie w oknie komunikatu.
```
Proponowane: Consider:
```
Komunikaty walidacji i mniej istotnych operacji w pasku komunikatu, każde potwierdzenie w oknie komunikatu.
```

### P11. SM-4: jedno zdanie z pięcioma członami po średnikach
Obecnie:
```
- **SM-4:** pełny cykl brief → PRD → architektura → epiki i historie → kod → wdrożenie; cały kod piszą agenci; wdrożenia na serwer odbywają się wyłącznie przez włączenie zmiany do gałęzi głównej (FR-28), a rollback (FR-29) został przećwiczony co najmniej raz; odtworzenie z Kopii serwera przećwiczone co najmniej raz na nowym, tymczasowym serwerze, bez ruszania produkcji, z potwierdzeniem, że aplikacja na nim działa. Po pierwszym miesiącu „na serio” powstaje notatka o tym, co działało, a co nie.
```
Proponowane (ta sama treść, podpunkty jak w SM-1 i SM-5):
```
- **SM-4:** pełny cykl brief → PRD → architektura → epiki i historie → kod → wdrożenie.
  - Cały kod piszą agenci.
  - Wdrożenia na serwer odbywają się wyłącznie przez włączenie zmiany do gałęzi głównej (FR-28); rollback (FR-29) został przećwiczony co najmniej raz.
  - Odtworzenie z Kopii serwera przećwiczone co najmniej raz na nowym, tymczasowym serwerze, bez ruszania produkcji, z potwierdzeniem, że aplikacja na nim działa.
  - Po pierwszym Miesiącu „na serio” powstaje notatka o tym, co działało, a co nie.
```
(Zawiera też poprawkę z P2.)

### P12. Addendum: zawiły nawias przy AD-2
Obecnie:
```
Do aktualizacji w architekturze (forge 31.10.2026 je zastępuje): AD-2 (uruchomienie wyłącznie lokalne; Postgres na `127.0.0.1` zostaje, teraz na Droplecie, dla tunelu SSH), AD-3 (Kopia bazy przy starcie).
```
Proponowane:
```
Do aktualizacji w architekturze (forge 31.10.2026 je zastępuje): AD-2 (uruchomienie wyłącznie lokalne; zostaje tylko Postgres na `127.0.0.1`, teraz na Droplecie, na potrzeby tunelu SSH), AD-3 (Kopia bazy przy starcie).
```

### P13. Odsyłacze „w addendum” bez wskazania miejsca (PRD §4.8, FR-26) i „per użytkownik” w addendum
| Lokalizacja | Obecnie | Proponowane |
|---|---|---|
| PRD §4.8 Opis | „Mechanizm w addendum.” | „Mechanizm: addendum, „Wdrożenie i infrastruktura”.” |
| PRD FR-26 | „(lista tematów w addendum)” | „(lista tematów: addendum, punkt `docs/admin/`)” |
| Addendum, „Kolejność z forge” | „epiki 1–3 per użytkownik” | Consider: „epiki 1–3 z danymi per User” — termin z §3 PRD zamiast „użytkownik”? |

### P14. Pozostałości po wersji jednoosobowej/lokalnej (pozycja (c)) — przegląd
Przejrzane pod kątem „jeden użytkownik / localhost”. Większość tekstu jest już przepisana (Sesja wpisywania, FR-5 data domyślna, NFR-4, FR-7 „od zalogowania”). Zostały:
- §1 akapit o uchylonej zasadzie — zob. S5 (struktura).
- FR-15: „(na Windowsie `Documents` bywa przekierowany do OneDrive, a repozytorium leży w `Documents`)” — szczegół środowiska Autora, aktualny tylko lokalnie; zdanie jest poprawnie zawężone do „W środowisku lokalnym”, więc zostaje. Consider przeniesienie nawiasu do addendum („Decyzje techniczne”, punkt „Lokalnie”)?
- §3 Odbiorca: „osoba, której Autor wysyła Zestawienie” — zob. sekcja „Dotyka znaczenia” niżej.
Pozostałe wzmianki o „Autorze” w UJ-1–UJ-3, SM i §6.1 są celowe (to ścieżki i decyzje Autora), nie są pozostałością.

---

## Dotyka znaczenia — wymaga decyzji Autora [ZNACZENIE?]

### M1. §3 Odbiorca zdefiniowany przez Autora, choć Zestawienia robi każdy User
Obecnie:
```
- **Odbiorca** — osoba, której Autor wysyła Zestawienie e-mailem; nie korzysta z aplikacji.
```
Możliwa wersja:
```
- **Odbiorca** — osoba, której User wysyła Zestawienie e-mailem; nie korzysta z aplikacji.
```
**[ZNACZENIE?]** To pozostałość po wersji jednoosobowej, ale zamiana na „User” rozszerza pojęcie: §4.5 mówi, że układ (Odbiorcy Autora) jest jeden dla wszystkich Userów, a §6 wyklucza układy per Odbiorca. Jeśli Odbiorca ma pozostać konkretną osobą Autora, obecne brzmienie jest poprawne i lepiej zostawić je bez zmian. Nie wprowadzać bez potwierdzenia.

Pozostałe wiersze (S1–S9, P1–P14) nie zmieniają znaczenia ani decyzji; S4 i S5 to przesunięcia/usunięcia powtórzeń, a treść zostaje w innym miejscu.
