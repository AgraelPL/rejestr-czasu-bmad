---
type: forged-idea
outcome: hardened
date: 2026-10-31
supersedes: prd NFR-1 (localhost, bez kont), architecture AD-2, AD-3, inicjatywa Boundaries („nie usługa sieciowa”, „brak kont”)
---

# Rejestr czasu w chmurze: konta, role, wdrożenie

**Po co:** ćwiczenie wdrożenia i CI/CD w metodzie BMad; produkt dla kilku osób (Autor + znajomy), tylko komputery, bez telefonu.

## Konta i dane
- Rejestracja otwarta, login + hasło, bez e-maila. Nowe konto ma stan **Oczekuje** do zatwierdzenia przez Administratora.
- Lockout po 5 nieudanych logowaniach; rate limiting na rejestracji i logowaniu.
- Pierwszy Administrator powstaje przy wdrożeniu z sekretu w konfiguracji, nie z formularza.
- Role: **User** prowadzi swoje dane; **Administrator** zatwierdza, blokuje, usuwa. Autor ma obie.
- `UserId` w Project, WorkType, TimeEntry; globalny filtr EF Core **bez wyjątków**; unikalność `(user_id, lower(...))`.
- Per użytkownik: dane i prefiks pliku Zestawienia (ekran Ustawienia konta). Wspólny: jeden układ z `export-layout.md`.
- Ekran Użytkownicy: login, data rejestracji, ostatnie logowanie, stan, liczba Wpisów — bez treści. Liczba Wpisów to jedyne nazwane zapytanie omijające filtr, z testem.
- Usuwanie konta: tylko zablokowanego, potwierdzenie wpisaniem loginu, kaskada po `UserId` w jednej transakcji.
- Jawne przy rejestracji: Administrator ma przez bazę dostęp do wszystkich danych.

## Infrastruktura
- Droplet DigitalOcean + docker compose; subdomena kupionej domeny; Caddy z Let's Encrypt.
- Cloud Firewall: 80, 443, 22; SSH tylko kluczem. PostgreSQL bez publicznego portu.
- Dostęp Administratora do bazy: tunel SSH lub `docker exec psql`; na co dzień rola tylko do odczytu.
- Sekrety: `.env` (600) na Droplecie, GitHub Secrets dla workflow.
- Kopie: włączone kopie Dropletu w DigitalOcean. Bez kodu kopii w aplikacji.
- Zostaje: CSP i zasoby lokalne bez CDN.

## CI/CD (GitHub Actions)
- PR: `dotnet build` + `dotnet test` (Testcontainers na ubuntu-latest); ochrona `main`.
- Merge do `main`: obraz do GHCR z tagiem skrótu commita → SSH `docker compose pull && up -d` → `/health`, błąd kończy workflow.
- Rollback: `workflow_dispatch` z tagiem poprzedniego obrazu.
- Migracje przy starcie (jedna instancja); reguła: tylko migracje dodające.

## Dokumentacja
- Pomoc użytkownika w aplikacji: `/pomoc/...`, ikona `?` w belce otwiera stronę bieżącego ekranu, z niej spis tematów.
- `/pomoc/administracja` tylko dla Administratora.
- `docs/admin/` w repo: Droplet, domena, Caddy, firewall, sekrety, tunel SSH, rola read-only, wdrożenie, rollback, odtwarzanie z kopii Dropletu.
- Każda historia zmieniająca ekran aktualizuje jego stronę pomocy (Definition of Done).

## Kolejność
CI → Identity z `UserId` (przed Słownikami) → wdrożenie na Droplet → epiki 1–3 per użytkownik → Użytkownicy, Ustawienia konta, pomoc, `docs/admin/`. Cały zakres przed pierwszym Miesiącem „na serio”, bez linii cięcia.

## Odrzucone
- App Platform + Managed DB — Droplet tańszy i bliższy obecnemu układowi.
- Potwierdzanie e-maila — niepotrzebne przy 2 osobach, wymaga usługi wysyłki.
- Wgląd Administratora w treść danych w aplikacji — wymagałby wyjątków w filtrze.
- Brak jakichkolwiek kopii — zastąpione kopiami Dropletu.
- Staging — podwaja koszt; środowiskiem testowym jest lokalny docker compose.
- Historia 1.2 „Kopia bazy przy starcie” (`docker exec pg_dump`) — nie działa w tym układzie, wypada.
- Układy Zestawienia per Odbiorca — poza zakresem.

## Ryzyko, które zostało
8 tygodni do Testu wrześniowego (28.12), 1 z ~25 historii gotowa, dochodzi nowy epik, brak planu awaryjnego. SM-C1 (metoda wygrywa z terminem) nadal obowiązuje.
