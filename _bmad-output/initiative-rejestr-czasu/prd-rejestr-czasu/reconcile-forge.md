# Uzgodnienie: forge-chmura-konta-wdrozenie → PRD + addendum

Wejście: `../forge-chmura-konta-wdrozenie/forge-chmura-konta-wdrozenie.md` i jego `.memlog.md` (31.10.2026).
Cele: `prd-rejestr-czasu.md`, `addendum.md`. Uwzględnione późniejsze decyzje Autora z `.memlog.md` PRD (K1–K17, po wpisie „Update PRD: sygnal zmiany = forge…”).
Pozycje umieszczone poprawnie nie są tu wymienione.

## Luki

1. **Uzasadnienie zasady „tylko migracje dodające” (bezpieczny rollback).** Forge (memlog, lock): „tylko migracje dodające, *żeby rollback był bezpieczny*”, czyli poprzedni obraz musi działać na nowszym schemacie. NFR-9 podaje inne uzasadnienie („dane na serwerze nie mogą zostać utracone przez wdrożenie”), a addendum tylko odsyła do NFR-9. Nigdzie nie ma związku NFR-9 z FR-29. Bez niego architekt nie wie, że np. nowa kolumna musi być opcjonalna albo mieć wartość domyślną, żeby stary obraz po rollbacku dalej działał. Miejsce: PRD §5 NFR-9 / §4.8 FR-29; addendum „Migracje przy starcie”.

2. **Utrzymanie serwera po stronie Autora (aktualizacje systemu).** Forge memlog (decyzja o hostingu): „certyfikaty, aktualizacje, firewall po stronie Autora”, bo to był świadomy koszt wyboru Dropletu zamiast App Platform. Certyfikaty pokrywa Caddy, firewall jest w addendum. **Aktualizacje systemu i obrazów** (łatki OS, aktualizacja Postgres/Caddy) nie występują w PRD, w liście tematów `docs/admin/` ani w §6.1 Ryzyka. Miejsce: FR-26 / addendum `docs/admin/`; ewentualnie §6.1.

3. **Kamień milowy zakresu.** Forge: „Cały zakres przed pierwszym Miesiącem »na serio«”. PRD §6.1 Termin: „Nowy zakres wchodzi przed Testem wrześniowym”. To dwa różne terminy (28.12.2026 i styczeń/luty 2027). Dodatkowo K17 mówi, że Test wrześniowy jest lokalny i nie wymaga Dropletu. Gotowość Dropletu ma być potrzebna dopiero przed Miesiącem „na serio”. Jeśli chodziło tylko o SM-C2 (nic *ponad* ten zakres przed SM-1), §6.1 mówi więcej, niż ustalono. Miejsce: PRD §6.1 „Termin” (też SM-C2).

4. **Skala ryzyka terminu.** Forge, „Ryzyko, które zostało”: 8 tygodni do Testu wrześniowego, 1 z ~25 historii gotowa, dochodzi nowy epik. PRD §6.1 zostawia samo „nie ma planu awaryjnego”. Nie ma liczb, które pokazują, *dlaczego* to ryzyko jest duże. Utrata jakościowa, drobna. Miejsce: PRD §6.1 „Termin”.

5. **Kaskada usuwania obejmuje tylko Słowniki i Wpisy.** Forge: usunięcie Konta „razem ze wszystkimi danymi użytkownika”, kaskada po `UserId`. FR-22 wylicza tylko Słowniki i Wpisy. Ustawienia konta (prefiks, FR-24) i sam rekord Konta nie są nazwane, a addendum nie mówi, gdzie leży prefiks. Jeśli prefiks trafi do osobnej tabeli z `UserId`, łatwo go pominąć w kaskadzie. Miejsce: PRD FR-22, FR-24; addendum „Konta”.

## Przesunięcia znaczenia

1. **„Administrator nie widzi treści danych w aplikacji” vs FR-21.** Forge lock: Administrator nie widzi treści, a ekran administracyjny czyta tylko tabele Identity. Później Autor zdecydował o resecie hasła przez Administratora (K10, FR-21). Administrator, który ustawił hasło, może zalogować się jako User, zanim ten je zmieni, i zobaczyć treść. §6.1 to przyznaje („do chwili zmiany zna hasło”). §3 (Administrator: „nie widzi w aplikacji treści danych innych Userów”), opis §4.6 („nigdy treść”) i §6 nadal mówią o zasadzie bezwzględnej. Zmiana jest świadoma, ale jej sformułowanie w §3/§4.6 się rozjeżdża. Wymuszenie zmiany hasła przy pierwszym logowaniu nie domyka luki, bo pierwszym logującym może być Administrator.

2. **NFR-8: „tylko do odczytu” rozszerzone na serwer.** Forge: osobna *rola bazy* tylko do odczytu na co dzień. NFR-8: „dostęp administracyjny do serwera i bazy tylko szyfrowanym kanałem z kluczem, na co dzień z uprawnieniem tylko do odczytu”. Zdanie można czytać tak, że dostęp do serwera też jest read-only, a to nie zgadza się z wdrożeniem przez SSH (`docker compose pull && up -d`) i obsługą `.env`. Miejsce: PRD §5 NFR-8.

3. **FR-28: kto sprawdza `/health`.** Forge: workflow sprawdza `/health`, a błąd kończy workflow. FR-28: „Po wdrożeniu *aplikacja* sprawdza swój stan”. Sprawdza potok, nie aplikacja. Drobne, ale może skłonić do złej implementacji (self-check w aplikacji zamiast kroku w workflow). Miejsce: PRD FR-28.

4. **Cel „ćwiczenie wdrożenia” jako jedyny powód odrzucenia gotowych narzędzi.** Memlog PRD (K6): uzasadnienie odrzucenia Clockify opiera się teraz *tylko* na celu naukowym. PRD §1 mówi o „dwóch celach o równej wadze”, ale nie mówi, że argument prywatności danych odpadł. Czytelnik może dalej zakładać, że prywatność uzasadnia budowę własnego narzędzia. Utrata „dlaczego”, drobna. Miejsce: PRD §1.

## Sprzeczności PRD/addendum

1. **AD-2 „do zastąpienia” vs Postgres na `127.0.0.1` Dropletu (sprzeczność wewnątrz addendum).** Addendum, „Decyzje techniczne”, stawia AD-2 („port na `127.0.0.1`”) do aktualizacji, bo forge go zastępuje. Ta sama sekcja „Wdrożenie i infrastruktura” wymaga tunelu SSH z „Postgres na `127.0.0.1` Dropletu”, a forge memlog zapisał to jako lock. Wiązanie portu do `127.0.0.1` zostaje, zmienia się tylko kontekst (lokalny komputer → Droplet). Trzeba doprecyzować, *co* w AD-2 jest zastępowane.

2. **SM-4/SM-C1 („wyłącznie przez potok”) vs ręczne przygotowanie serwera.** SM-4 i SM-C1 zakazują ręcznych poprawek na serwerze poza potokiem. Addendum i FR-26 przewidują ręczne przygotowanie Dropletu: Caddy, firewall, `.env`, rola read-only, pierwszy Administrator z sekretu (FR-19). Brakuje granicy: przygotowanie serwera według `docs/admin/` jest dozwolone, zmiany aplikacji tylko przez potok.

## Uwagi

- **Częstotliwość Kopii serwera nie wybrana.** Addendum: „tygodniowe albo dzienne za dopłatą”. §6.1 akceptuje utratę do Miesiąca (K7), więc obie opcje mieszczą się w ryzyku, ale wybór powinien trafić do `docs/admin/` albo architektury.
- **Brak kopii lokalnej po usunięciu historii 1.2.** Forge odrzucił `pg_dump` przy starcie, bo „nie działa w tym układzie” (Droplet). Środowisko lokalne (Test wrześniowy, FR-15) zostało bez żadnej kopii. Ryzyko jest małe (28 Wpisów ręcznie), ale nie zostało nazwane w §6.1.
- **Stany Konta a mechanizm Identity.** PRD rozróżnia Zablokowane (decyzja Administratora) i czasowe wstrzymanie logowania (lockout, FR-18). Addendum nie mówi, jak Oczekuje/Zablokowane mają się do `LockoutEnd` w Identity. Użycie lockoutu do blokady administracyjnej zderzy się z automatycznym odblokowaniem po 15 min. Do architektury.
- **„Ostatnie logowanie” (FR-20)** nie jest standardowym polem Identity. Forge zakłada, że ekran czyta tylko tabele Identity, więc pole trzeba dodać do tabeli użytkowników, a nie liczyć z danych domenowych. Do architektury.
- **Brzegowe przypadki Administratora** (zablokowanie lub usunięcie własnego Konta, ostatniego Administratora) nie są opisane ani w forge, ani w PRD.
- **Reguła Definition of Done** (każda historia zmieniająca ekran aktualizuje jego pomoc) jest w addendum jako decyzja techniczna. To reguła procesu, więc powinna trafić także do definicji gotowości w epikach/historiach.
- **Granice inicjatywy.** Forge `supersedes` obejmuje też Boundaries inicjatywy („nie usługa sieciowa”, „brak kont”). PRD i addendum ich nie dotyczą, ale trzeba je zaktualizować poza PRD (razem z AD-2, AD-3).
