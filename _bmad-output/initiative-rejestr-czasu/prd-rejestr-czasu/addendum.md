# Addendum: PRD Rejestr czasu pracy

Szczegóły dla architektury, które nie należą do PRD.

## Decyzje techniczne

Obowiązują decyzje z kręgosłupa architektury [`architecture-rejestr-czasu.md`](../architecture-rejestr-czasu/architecture-rejestr-czasu.md) (AD-1–AD-22, 31.10.2026; AD-3 wycofane). Zastąpiły pierwotne założenia briefu (Node.js, SQLite w jednym pliku, `npm start`). Przy rozbieżności z tym addendum wiąże architektura.

- .NET 10, Blazor Web App (Interactive Server), Fluent UI Blazor 5, FluentValidation 12, EF Core 10 z Npgsql.
- PostgreSQL 18 w kontenerze Docker (`docker compose`), dane w wolumenie Dockera, bez publicznego portu.
- Lokalnie (praca nad kodem, Test wrześniowy): `docker compose up -d`, potem `dotnet run`.
- `.gitignore` wyklucza `docs/*.xlsx`, `docs/projekt-testowy-material.md`, `appsettings.Local.json` i `.env`.

## Wdrożenie i infrastruktura (forge 31.10.2026)

Źródło: [`forge-chmura-konta-wdrozenie.md`](../forge-chmura-konta-wdrozenie/forge-chmura-konta-wdrozenie.md).

- Serwer: Droplet DigitalOcean z `docker compose`; subdomena kupionej domeny; Caddy jako reverse proxy z Let's Encrypt.
- Cloud Firewall: tylko 80, 443, 22; SSH tylko kluczem. PostgreSQL bez publicznego portu; dostęp Administratora tunelem SSH (Postgres na `127.0.0.1` Dropletu) albo `docker exec psql`; na co dzień rola tylko do odczytu.
- Sekrety: `.env` (`chmod 600`) na Droplecie, GitHub Secrets dla workflow. Pierwszy Administrator z sekretu w konfiguracji.
- Kopie (Kopia serwera): włączone kopie Dropletu w DigitalOcean (tygodniowe albo dzienne za dopłatą). Brak kodu kopii w aplikacji.
- Konta: ASP.NET Core Identity; `UserId` w Project, WorkType, TimeEntry; globalny filtr EF Core omijany tylko w dwóch nazwanych miejscach z testem (liczba Wpisów na ekranie Użytkownicy, usuwanie Konta); usunięcie Konta jawne, w jednej transakcji, bez kaskady w bazie. Szczegóły: AD-15–AD-18.
- CI/CD (GitHub Actions): PR → `dotnet build` + `dotnet test` (Testcontainers na `ubuntu-latest`), ochrona `main`. Merge do `main` → obraz do GHCR z tagiem skrótu commita → SSH `docker compose pull && up -d` → `/health`; błąd kończy workflow. Rollback: `workflow_dispatch` z tagiem poprzedniego obrazu.
- Migracje przy starcie (jedna instancja); od pierwszego wdrożenia tylko migracje dodające (NFR-9).
- **Przepisanie schematu (decyzja Autora, 31.10.2026):** historia 1.1 zbudowała migrację `Initial` bez `UserId`. `Initial` zostaje przepisana tak, by od początku miała `UserId` w Project, WorkType i TimeEntry, przed pierwszym wdrożeniem na serwer (na serwerze nie ma jeszcze danych). Od pierwszego wdrożenia obowiązują tylko migracje dodające. Architektura ustala, jak to zrobić.
- Pomoc: strony `/pomoc/...`; `/pomoc/administracja` tylko dla roli Administrator. Definition of Done: każda historia zmieniająca ekran aktualizuje jego stronę pomocy.
- `docs/admin/`: Droplet, domena, Caddy, firewall, sekrety, tunel SSH, rola tylko do odczytu, wdrożenie, rollback, odtwarzanie z kopii Dropletu.
- Kolejność z forge: CI → Identity z `UserId` (przed Słownikami) → wdrożenie na Droplet → epiki 1–3 z danymi per User → Użytkownicy, Ustawienia konta, pomoc, `docs/admin/`.

### Odrzucone (z forge)

- App Platform + Managed DB — Droplet tańszy i bliższy obecnemu układowi.
- Potwierdzanie e-maila — niepotrzebne przy 2 osobach, wymaga usługi wysyłki.
- Wgląd Administratora w treść danych w aplikacji — wymagałby wyjątków w filtrze.
- Staging — podwaja koszt; środowiskiem testowym jest lokalny `docker compose`.
- Historia 1.2 „Kopia bazy przy starcie” (`docker exec pg_dump`) — nie działa w tym układzie.
- Układy Zestawienia per Odbiorca — poza zakresem.

## Źródło układu Zestawienia

Analiza oryginału: `docs/projekt-testowy-material.md` §1 (lokalnie, poza repozytorium). Sprawdzone w oryginalnym xlsx 30.10.2026:
- nagłówek w wierszu 1, zamrożone okienko od `A2`;
- 28 wierszy danych (2–29), wiersz 30 pusty, wiersz 31 pogrubiony z „RAZEM WRZESIEŃ 2026” w E;
- Suma dnia w pierwszym wierszu dnia (w oryginale `=SUM(F6:F7)`), Suma tygodnia w pierwszym wierszu tygodnia (w oryginale `=G6+G8+G9+G10`);
- wiersz RAZEM: `=SUM(F2:F29)` i `=H2+H6+H11+H18+H26`;
- data w B to tekst, F ma format `0`, E ma zawijanie tekstu;
- Sumy tygodnia: 30 / 16 / 18 / 25 / 14 = 103 h.

Odstępstwa od oryginału (decyzja Autora, PRD FR-12): aplikacja zapisuje sumy jako wartości, a nie formuły; data w B jest prawdziwą datą Excela, F ma format ogólny.
