# Review log: walkthrough-4-2-nowy-schemat-initial
Target: commit 8e504f2 (branch 4-2-nowy-schemat-initial)

## 1 — całość — orientacja
Session: unavailable · Timestamp: 2026-10-31T18:30:00+01:00
- Action: cel z ticketu 4.2 i planu story-nowy-schemat-initial-z-identity-i-userid-plan.md
- Result: zakres commit 8e504f2; recenzja quick w buildzie: 1 high (stub po Id) naprawione, 2 low naprawione, 1 defer (account_settings w trybie systemowym)

## 2 — blok 1 — test

Session: unavailable · Timestamp: 2026-10-31T18:45:00+01:00

- Action: docker compose down -v, up -d, dotnet run (profil http), GET 127.0.0.1:5080, psql: tabele, historia migracji, klucze obce time_entry
- Result: start OK, HTTP 200; 12 tabel + __EFMigrationsHistory z 1 migracją; FK time_entry złożone (user_id, project_id), (user_id, work_type_id) i do users, wszystkie RESTRICT ('r'). Log "fail: Failed executing DbCommand" na SELECT z __EFMigrationsHistory przy pustej bazie — zwykła próba EF przed utworzeniem tabeli historii, accepted as-is
- Evidence: kryterium akceptacji 1 planu 4.2

## 3 — blok 4 — opinia: account_settings w trybie systemowym

Session: unavailable · Timestamp: 2026-10-31T19:05:00+01:00

- Action: ocena sprzeczności AD-7/AD-21 (wiersz account_settings z Kontem w trybie systemowym) z AD-16 (tryb systemowy nie zapisuje IUserOwned); warianty: leniwe tworzenie, trzeci wyjątek BindUser w Accounts, AccountSettings poza IUserOwned, zapis w trybie systemowym
- Result: decyzja Autora — wariant A: wiersz account_settings powstaje leniwie przy pierwszym zapisie Ustawień konta w trybie Usera; brak wiersza = pusty prefiks. Kod 4.2 bez zmian
- Open: Winston — AD-7 i AD-21 krok 3 bez tworzenia wiersza; Sofia — opisy 4.3, 4.4 i epiku 3 (obsługa braku wiersza); wpis w deferred-work.md rozwiązany tą decyzją

## 4 — blok 4 — druga opinia: obejścia oddzielenia danych

Session: unavailable · Timestamp: 2026-10-31T19:20:00+01:00

- Action: świeży agent szukał obejść zwykłym kodem EF Core
- Result: SaveChanges, Include, Find, Attach, grafy, stub po Id — trzyma. Furtki (open → fix w 4.2): (1) ExecuteUpdate SetProperty(UserId) omija SaveChanges — trigger blokujący zmianę user_id w Initial; (2) IgnoreQueryFilters i (3) SqlQuery/ExecuteSql bez filtra — BannedApiAnalyzers z dozwolonymi miejscami AD-16; (4) Identity i (5) DataProtectionKeys dostępne w trybie Usera — odrzucać zapis nie-IUserOwned w trybie Usera, filtry na Identity i kluczach; (6) pula kontekstów — reguła bez puli; (7) encja bez UserId — ostrzejszy test
- Decision: Autor wybrał naprawę na gałęzi 4.2 przed scaleniem PR (bmad-build, plan uzupełniający)
