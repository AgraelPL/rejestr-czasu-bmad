# Uzgodnienie: kręgosłup architektury ↔ inicjatywa i addendum

- Kręgosłup: `architecture-rejestr-czasu.md` (updated 31.10.2026, AD-1…AD-22, AD-3 wycofane)
- Ticket: `initiative-rejestr-czasu.md` (R1–R31, R14 wycofane; Constraints; Boundaries)
- Addendum: `prd-rejestr-czasu/addendum.md`, sekcja „Wdrożenie i infrastruktura”
- Podział epików: `tickets/tickets.toml`
- Data: 31.10.2026. Przegląd tylko do odczytu; żaden plik źródłowy nie został zmieniony.

## Werdykt

**Zgodne z drobnymi lukami — do poprawy przed rozpisaniem historii epiku Konta i wdrożenie.**

Każde R1–R31 (bez R14) ma co najmniej jedno AD, które nim rządzi, i żadne AD nie przeczy wymaganiu wprost. Luki są wąskie. Pierwsza to brak decyzji: role pierwszego Administratora (R20), polskie komunikaty Identity i Blazora, progi bezpieczeństwa (NFR-8). Druga to dwa niezgodne sformułowania: data kopii „od zalogowania” wobec „w tym obwodzie” (R7) oraz pomoc niedostępna przy wymuszonej zmianie hasła (R26). Trzecia to nieaktualne teksty w tickecie i addendum, które przeczą zaktualizowanemu kręgosłupowi.

## 1. Wymagania bez AD albo z AD, które im przeczy

| # | Wymaganie | Gdzie w kręgosłupie | Problem | Waga |
| --- | --- | --- | --- | --- |
| 1.1 | R20: „Konto Autora ma role User i Administrator” | AD-21 krok 3, AD-15 | AD-21 tworzy pierwsze Konto `active` „z rolą Administrator” (pośrednio: warunek „brak Konta z rolą Administrator”), ale nigdzie nie mówi, że dostaje też rolę `User`. AD-15 nadaje `User` tylko przy rejestracji. Historia epiku 4 może utworzyć Administratora bez roli User, a wtedy autoryzacja ekranów danych oparta na roli `User` odetnie Autora od własnych Wpisów. | wysoka |
| 1.2 | R7: data kopii „ostatnio w formularzu Nowy wpis (gdy ekranu nie otwierano **od zalogowania**, dzisiejsza)” | AD-10 (b) | AD-10 trzyma datę „w tym obwodzie”. Odświeżenie strony (F5) albo nowa karta tworzy nowy obwód przy tej samej sesji logowania, więc data przepada wcześniej, niż mówi R7. Trzeba zmienić R7 na „w tej karcie lub sesji obwodu” albo AD-10 (np. przechowywać datę per User w pamięci serwera lub w `account_settings`). | średnia |
| 1.3 | R26: „każdy ekran ma stronę pomocy” (także wymuszonej zmiany hasła; strony pisze epik 5) | AD-15 (przekierowanie przy `must_change_password`), AD-22 | AD-15: przy ustawionym `must_change_password` „każda trasa poza `/konto/zmiana-hasla` i `/konto/wyloguj` przekierowuje”, więc `/pomoc/{temat}` ekranu zmiany hasła jest nieosiągalna dokładnie wtedy, gdy jest potrzebna. Ponadto AD-11 daje stronom `/konto/...` układ „bez menu”, a AD-22 umieszcza ikonę `?` w belce; nie wiadomo, skąd na tych stronach otworzyć pomoc. Brakuje wyjątku dla tematów pomocy `/konto/...` i linku pomocy w prostym układzie. | średnia |
| 1.4 | R16: ≤ 10 s na Wpis „przy pracy przez internet” | AD-10 (klawiatura), AD-19 | AD-10 załatwia klawiaturę, ale nic nie decyduje o opóźnieniu: Interactive Server wysyła każdy klawisz przez SignalR, a region Dropletu nie jest ustalony (Deferred mówi tylko o rozmiarze). Brakuje reguły „region UE blisko Autora (np. FRA1/AMS3)” albo jawnego przeniesienia tego wyboru do `docs/admin/` jako warunku R16. | niska–średnia |
| 1.5 | R21: „zablokowanie działa od razu” | AD-17 | Przy aktywnym obwodzie warunek jest spełniony (sprawdzenie przy każdym kontekście). Bezczynny obwód zamyka się dopiero po rewalidacji co 1 minutę, a ekran bez operacji na bazie (np. otwarta pomoc) może pokazywać treść do minuty. Akceptowalne, ale warto to zapisać jako świadome odczytanie słów „od razu”. | niska |
| 1.6 | R21: „ostatnie logowanie” | AD-7 (kolumna `last_login_at`), AD-15 | Kolumna istnieje, ale żadne AD nie mówi, kto i kiedy ją ustawia (logowanie na statycznej stronie `/konto/logowanie`). Drobna luka właściciela; łatwo ją przeoczyć w historii epiku 4. | niska |
| 1.7 | R13: nazwa pliku z polskim miesiącem (`wrzesień`) | AD-14, AD-6 | Brak reguły kodowania nazwy w `Content-Disposition` (`filename*` UTF-8). Szczegół historii, ale dotyczy jedynego miejsca, w którym polski znak wychodzi poza UI. | niska |
| 1.8 | R15: dane lokalne „poza folderami synchronizowanymi z chmurą” | AD-2 | Nazwany wolumen Dockera spełnia warunek pośrednio (WSL), ale AD-2 tego nie mówi. Warto dodać jedno zdanie, żeby nikt nie zamienił wolumenu na bind mount w `Documents` (repozytorium leży w `Documents`). | niska |

Bez uwag: R1–R6, R8–R12, R17–R19, R22–R25, R27–R31. Mapa Capability → Architecture pokrywa wszystkie wymagania.

## 2. Ograniczenia ciche, które struktura AD zgubiła lub osłabiła

| # | Ograniczenie | Stan w kręgosłupie | Luka |
| --- | --- | --- | --- |
| 2.1 | NFR-4: „interfejs tylko po polsku” | AD-11 (Fluent UI po polsku, komunikat ponownego łączenia), konwencja nazewnictwa | **Błędy `IdentityResult`** (AD-8: „formularze haseł pokazują błędy `IdentityResult`”) są domyślnie po angielsku; potrzebny polski `IdentityErrorDescriber`, a żadne AD go nie wymaga. Podobnie odpowiedź limitera (429) na stronach `/konto/...` i strona błędu statycznego SSR. |
| 2.2 | NFR-4 + AD-11 „bez własnego JS” | AD-11 | Polskie teksty ponownego łączenia: w .NET 10 szablon personalizuje ten komunikat komponentem `ReconnectModal` z dołączonym plikiem `.razor.js`; wbudowany domyślny interfejs ma teksty angielskie. AD-11 zakłada oba warunki naraz bez wskazania mechanizmu. Ryzyko wykonalności dla historii z R31; trzeba rozstrzygnąć, czy plik JS z szablonu liczy się jako „własny JS”. |
| 2.3 | NFR-8: „progi, reguły haseł i czas sesji to bezpieczne wartości domyślne **z architektury**” | Deferred: „wybiera historia epic-konta-i-wdrozenie” | Constraint każe architekturze je ustalić, a kręgosłup przenosi je do historii. Ustalone są tylko 5 prób / 15 min. Trzeba albo wpisać wartości (limit IP, długość hasła, czas życia ciasteczka i sliding expiration) do AD-15, albo zmienić NFR-8 / ticket na „ustala historia”. |
| 2.4 | NFR-8: dostęp do bazy „na co dzień tylko do odczytu” | AD-19 („rolą tylko do odczytu”) | Nie ustalono, kto tworzy tę rolę (migracja? ręczny krok z `docs/admin/`?) ani jakim użytkownikiem bazy łączy się aplikacja (czy nie superuserem `postgres`). Spina się z SM-C1 (ręczne kroki tylko z instrukcji). |
| 2.5 | R18: zdanie o dostępie Administratora do danych przez bazę (prywatność) | brak | Żadne AD ani konwencja go nie wymienia. To treść ekranu, więc może zostać w historii, ale jest to jedyna jawna deklaracja prywatności wobec znajomego; warto dodać ją do AD-15 (strona rejestracji). |
| 2.6 | Europe/Warsaw (NFR-7) | AD-6, AD-20 (obraz z tzdata) | Zgodne. Uwaga: `PolishCalendar.Today` nie zależy od `TZ` kontenera, więc zbędna konfiguracja nie jest potrzebna; bez luki. |
| 2.7 | Terminy (Test wrześniowy do 28.12.2026, styczeń/luty 2027), SM-C2, SM-4/SM-C1, glosariusz dosłownie | brak AD (słusznie), AD-20 (ręczne kroki tylko z `docs/admin/`), konwencja nazewnictwa | Terminy nie wymagają AD. Bez luki, ale kręgosłup nigdzie nie odnotowuje, że epik 4 (cięższy po zmianie) leży na ścieżce krytycznej przed Testem wrześniowym. |
| 2.8 | NFR-1 bez połączeń wychodzących, NFR-2 prywatność repo | AD-12, AD-13, konwencja logów | Zgodne. GHCR publiczny jest zgodny z NFR-2, o ile obraz nie zawiera danych (AD-13 to mówi). |

## 3. Sprzeczności między kręgosłupem a addendum (i nieaktualny ticket)

| # | Addendum / ticket | Kręgosłup | Rozbieżność |
| --- | --- | --- | --- |
| 3.1 | Addendum, „Konta”: „globalny filtr EF Core **bez wyjątków**… Liczba Wpisów na ekranie Użytkownicy to **jedyne** nazwane zapytanie omijające filtr” | AD-16: dwa miejsca z `IgnoreQueryFilters()`: `AccountStats` **i** `DeleteAccount` | Bezpośrednia sprzeczność co do liczby wyjątków. Kręgosłup ma rację technicznie (usuwanie cudzych danych przez Administratora musi ominąć filtr); addendum trzeba zaktualizować. |
| 3.2 | Addendum: „Usunięcie Konta: **kaskada** po `UserId` w jednej transakcji” | AD-18: jawne usuwanie po kolei, „**bez `ON DELETE CASCADE`**” | Sprzeczność w słowach (semantycznie to samo: „usuń wszystko Usera”). Agent czytający addendum może dodać `ON DELETE CASCADE` w migracji. Addendum powinno mówić „usuwanie wszystkich danych Usera w jednej transakcji”. |
| 3.3 | Addendum: „unikalność `(user_id, lower(...))`” | AD-7: Projekt `(user_id, code)` z kodem normalizowanym do wielkich liter; Typ pracy `(user_id, lower(name))` | Niegroźna różnica realizacji dla Projektu; wynik zgodny z R1. Warto ujednolicić zapis. |
| 3.4 | Addendum, „Decyzje techniczne”: „AD-1–AD-14, 30.10.2026” i „Do aktualizacji w architekturze… AD-2, AD-3” | AD-2 przepisane, AD-3 wycofane, AD-15–AD-22 dodane 31.10.2026 | Nieaktualny tekst addendum. |
| 3.5 | Ticket, Constraints: „kręgosłup (AD-1…AD-14) wiąże każdy epik, **poza AD-2 i AD-3**, które PRD… zastępuje do czasu aktualizacji architektury” | aktualizacja wykonana | Nieaktualne ograniczenie; po aktualizacji powinno brzmieć „AD-1…AD-22 (AD-3 wycofane)”. Inaczej historie mogą pominąć nowe AD-2. |
| 3.6 | Ticket, Notes: „Architecture invalidated (31.10.2026)… Historie epiku Konta i wdrożenie planowane po aktualizacji” | aktualizacja wykonana | Brak notatki zamykającej („Architecture updated 31.10.2026”). Bez niej planowanie epiku 4 wygląda na zablokowane. |
| 3.7 | Addendum: Postgres na `127.0.0.1` Dropletu, Caddy, firewall, sekrety, CI/CD, rollback, migracje dodające, `docs/admin/` | AD-2, AD-13, AD-19, AD-20, AD-21 | Zgodne. |

## 4. Własność epików (tickets.toml) a reguły AD

- AD-7 (przepisanie `Initial`) → epic-konta-i-wdrozenie: zgodne z `covers` i opisem epiku 4.
- AD-17 (epik 5 tylko zmienia stan) → zgodne z opisem epiku 5.
- Ćwiczenie odtworzenia z Kopii serwera (Deferred) → epic-administracja-kont-i-pomoc: zgodne.
- Zmiana własnego hasła (AD-8, AD-15) → epic-miesiac-i-zestawienie (R25): zgodne z decyzją w tickecie.
- **Napięcie:** AD-22 „historia zmieniająca ekran aktualizuje jego temat w tej samej zmianie”, a ekrany `/konto/...` powstają w epiku 4, zanim mechanizm pomocy powstanie w historii 1.3 (epik 1). Ticket przenosi te strony do epiku 5; kręgosłup powinien zapisać ten wyjątek przy AD-22, żeby reguła nie wyglądała na złamaną.
- **Napięcie:** kontrola migracji z AD-21 („odrzuca usuwanie… tabeli”) musi przepuścić jednorazowe przepisanie `Initial` w epiku 4 (usunięcie starej migracji). Kręgosłup nie mówi, od którego commitu/wdrożenia kontrola obowiązuje.

## 5. Zalecane poprawki (bez edycji w tym przeglądzie)

1. AD-21 krok 3 (lub AD-15): pierwszy Administrator dostaje role `Administrator` **i** `User` (R20).
2. AD-15 / AD-8: polski `IdentityErrorDescriber`, polskie odpowiedzi limitera; AD-11: wskazać mechanizm polskiego komunikatu ponownego łączenia zgodny z „bez własnego JS”.
3. AD-15: wpisać wartości NFR-8 (limit prób według IP, reguły hasła, czas sesji) albo zmienić NFR-8 na „ustala historia”.
4. AD-10 vs R7: ustalić zakres zapamiętanej daty (obwód czy sesja logowania) i poprawić jedno z nich.
5. AD-15 / AD-22: wyjątek przekierowania dla tematów pomocy `/konto/...`, link pomocy w prostym układzie, notatka o stronach `/konto/...` pisanych w epiku 5.
6. Addendum: zaktualizować „Decyzje techniczne” (AD-1…AD-22), „jedyne zapytanie omijające filtr” (dwa miejsca), „kaskada” → „usuwanie w jednej transakcji bez `ON DELETE CASCADE`”.
7. Ticket: zaktualizować constraint o kręgosłupie (AD-1…AD-22) i dodać notatkę „Architecture updated 31.10.2026”.
8. Drobne: region Dropletu (R16), właściciel `last_login_at`, rola tylko do odczytu i użytkownik bazy aplikacji, `filename*` dla polskiej nazwy pliku, zdanie o wolumenie poza folderami synchronizowanymi, początek obowiązywania kontroli migracji.
