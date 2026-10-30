# Addendum: PRD Rejestr czasu pracy

Szczegóły dla architektury, które nie należą do PRD.

## Decyzje techniczne

Obowiązują decyzje z kręgosłupa architektury [`architecture-rejestr-czasu.md`](../architecture-rejestr-czasu/architecture-rejestr-czasu.md) (AD-1–AD-14, 30.10.2026). Zastąpiły pierwotne założenia briefu (Node.js, SQLite w jednym pliku, `npm start`).

- .NET 10, Blazor Web App (Interactive Server), Fluent UI Blazor 5, FluentValidation 12, EF Core 10 z Npgsql.
- PostgreSQL 18 w kontenerze Docker (`docker compose`), port tylko na `127.0.0.1`, dane w wolumenie Dockera.
- Uruchomienie: `docker compose up -d`, potem `dotnet run`.
- Kopia bazy przy starcie przez `pg_dump` (plik `.sql`), przed migracjami; rotacja do 10 plików. Aplikacja działa 1–2 h co kilka dni, więc kopie cykliczne nie są potrzebne.
- `.gitignore` wyklucza `docs/*.xlsx`, `docs/projekt-testowy-material.md` i katalog `backups/`; `appsettings.Local.json` (prefiks nazwy Zestawienia, katalog kopii, hasło) i `.env` też muszą być wykluczone.

## Źródło układu Zestawienia

Analiza oryginału: `docs/projekt-testowy-material.md` §1 (lokalnie, poza repozytorium). Sprawdzone w oryginalnym xlsx 30.10.2026:
- nagłówek w wierszu 1, zamrożone okienko od `A2`;
- 28 wierszy danych (2–29), wiersz 30 pusty, wiersz 31 pogrubiony z „RAZEM WRZESIEŃ 2026” w E;
- Suma dnia w pierwszym wierszu dnia (w oryginale `=SUM(F6:F7)`), Suma tygodnia w pierwszym wierszu tygodnia (w oryginale `=G6+G8+G9+G10`);
- wiersz RAZEM: `=SUM(F2:F29)` i `=H2+H6+H11+H18+H26`;
- data w B to tekst, F ma format `0`, E ma zawijanie tekstu;
- Sumy tygodnia: 30 / 16 / 18 / 25 / 14 = 103 h.

Odstępstwa od oryginału (decyzja Autora, PRD FR-12): aplikacja zapisuje sumy jako wartości, a nie formuły; data w B jest prawdziwą datą Excela, F ma format ogólny.
