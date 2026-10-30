# Addendum: PRD Rejestr czasu pracy

Szczegóły dla architektury, które nie należą do PRD.

## Decyzje techniczne z briefu

- Aplikacja webowa uruchamiana lokalnie przez `npm start` (Node.js), nasłuch na `127.0.0.1`.
- Baza SQLite w jednym pliku; ścieżka w konfiguracji, poza folderami synchronizowanymi z chmurą.
- Kopia bazy wyłącznie przy starcie, przed przyjęciem pierwszego żądania; rotacja do 10 plików. Aplikacja działa 1–2 h co kilka dni, więc kopie cykliczne nie są potrzebne.
- `.gitignore` już wyklucza `docs/*.xlsx`, `docs/projekt-testowy-material.md`, pliki `*.sqlite`/`*.db` z plikami WAL i katalog `backups/`. Plik konfiguracji z prefiksem nazwy Zestawienia też musi być wykluczony.

## Źródło układu Zestawienia

Analiza oryginału: `docs/projekt-testowy-material.md` §1 (lokalnie, poza repozytorium). Sprawdzone w oryginalnym xlsx 30.10.2026:
- nagłówek w wierszu 1, zamrożone okienko od `A2`;
- 28 wierszy danych (2–29), wiersz 30 pusty, wiersz 31 pogrubiony z „RAZEM WRZESIEŃ 2026” w E;
- Suma dnia w pierwszym wierszu dnia (w oryginale `=SUM(F6:F7)`), Suma tygodnia w pierwszym wierszu tygodnia (w oryginale `=G6+G8+G9+G10`);
- wiersz RAZEM: `=SUM(F2:F29)` i `=H2+H6+H11+H18+H26`;
- data w B to tekst, F ma format `0`, E ma zawijanie tekstu;
- Sumy tygodnia: 30 / 16 / 18 / 25 / 14 = 103 h.

Odstępstwa od oryginału (decyzja Autora, PRD FR-12): aplikacja zapisuje sumy jako wartości, a nie formuły; data w B jest prawdziwą datą Excela, F ma format ogólny.
