# Review log: walkthrough-4-3-rejestracja-logowanie

Target: commit 1be1d2c (gałąź 4-3-rejestracja-logowanie, baza 8f63f2c93d2a0afb82c843e3a389814b90594200)

## 1 — całość — start przeglądu

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: narracja z 9 blokami.
- Result: przegląd rozpoczęty; review quick (8 uwag: 5 poprawionych, 3 odrzucone) w Review Triage Log planu.
- Evidence: _bmad-output/initiative-rejestr-czasu/tickets/epic-konta-i-wdrozenie/tickets/story-rejestracja-logowanie-i-wylogowanie-plan.md
- Open: PR otwiera Autor ręcznie; lokalnie potrzebne `docker compose down -v`.

## 2 — Blok 1 Intencja — Thoughts

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: uwagi do intencji na prośbę Autora.
- Result: accepted as-is: (1) do 4.4 aktywacja Konta lokalnie tylko przez psql (README „Konto lokalnie”); (2) brak sprawdzania stanu Konta w sesji — 4.4; (3) limiter po IP i klucze Data Protection poprawne dopiero za proxy — 4.5; (4) login bez polskich liter (Decision 2a) daje polski błąd Identity; (5) angielski 400 przy nieważnym tokenie antyforgery — odrzucone w review (low).
- Open: brak.

## 3 — Blok 2 W skrócie — Test na żywo

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: Autor lokalnie: docker compose down -v / up -d, dotnet run; przekierowanie anonima na logowanie, rejestracja (Konto Oczekuje), logowanie przed i po zatwierdzeniu przez psql, last_login_at, GET /konto/wyloguj nie wylogowuje.
- Result: Autor potwierdził „wszystko ok, działa”. Błąd EF o braku __EFMigrationsHistory przy pierwszym starcie — oczekiwany (README).
- Evidence: kroki 1–7 z sesji; README „Konto lokalnie”.
- Open: brak.

## 4 — całość — ustalenie z Autorem

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: Autor przechodzi bloki bez ruchów; testy całości na końcu, chyba że wyjdą problemy.
- Result: ustalenie zapisane; bloki 4–9 bez osobnych testów.
- Open: testy całości po bloku 9.

## 5 — Blok 9 Peryferia — wrap-up

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: zaproponowano ręczny test końcowy (blokada po 5 złych hasłach, limit 10 POST/min); Autor przeszedł do wrap-up bez raportu wyników.
- Result: przegląd zakończony, 9/9 bloków zaakceptowanych. Blokada i limit sprawdzone tylko testami automatycznymi (AccountsTests: Login_FiveWrongPasswords_SuspendsLogin_EvenWithCorrectPassword, RateLimit_EleventhPostInAMinute_Returns429_InPolish_WithCsp), nie ręcznie.
- Open: scalenie PR po zielonym build-and-test; następna historia 4.4.
