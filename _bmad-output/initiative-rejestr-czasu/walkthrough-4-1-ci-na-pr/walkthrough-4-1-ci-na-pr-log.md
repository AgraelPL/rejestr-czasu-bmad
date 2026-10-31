# Review log: walkthrough-4-1-ci-na-pr

Target: commit 9f570e4753d85783da14e49e85c3f7ede0f48507 (gałąź 4-1-ci-na-pr, baza 6c510584869a68e5a051806d5be9728d0c19bfca)

## 1 — całość — start przeglądu

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: utworzono narrację z 6 blokami (intencja, w skrócie, workflow, kontrola migracji, instrukcja ochrony, peryferia).
- Result: przegląd rozpoczęty; wcześniejszy review `quick` (5 znalezisk, 4 poprawione, 1 odrzucone) jest w Review Triage Log planu.
- Evidence: _bmad-output/initiative-rejestr-czasu/tickets/epic-konta-i-wdrozenie/tickets/story-ci-na-pr-z-kontrola-migracji-i-ochrona-gaezi-main-plan.md
- Open: gałąź wypchnięta, PR otwiera Autor ręcznie (brak gh); job w Actions jeszcze nieuruchomiony.

## 2 — Blok 1 Intencja — Thoughts

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: uwagi do intencji na prośbę Autora; blok 1 zaakceptowany.
- Result: (a) zamrożenie `Initial` po 4.8 niepilnowane — test pomija klasę `Initial`, has-pending-model-changes nie wykrywa edycji jej treści; deferred do 4.8 (kryterium przypięcia pliku Initial). (b) kontrola to lista zabronionych operacji, AD-21 opisuje dozwolone; DeleteData/UpdateData/AddCheckConstraint/AddForeignKey przechodzą — accepted as-is (brak HasData/HasCheckConstraint w src). (c) bramka działa dopiero po ręcznej ochronie main — accepted, krok człowieka w planie.
- Evidence: tests/RejestrCzasu.Tests/MigrationRulesTests.cs:24; architecture AD-21
- Open: dopisać (a) do historii 4.8.

## 3 — Blok 2 W skrócie — Test

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: tymczasowa migracja aplikacji ZepsutaMigracja ([DbContext], [Migration]) z DropColumn project.is_active; uruchomiono AppMigrations_AfterInitial_AreAdditiveOnly; potem plik usunięty i test powtórzony.
- Result: z migracją test czerwony z komunikatem „Migracja ZepsutaMigracja: DropColumnOperation usuwa kolumnę project.is_active — po Initial dozwolone są tylko migracje dodające (AD-21).”; po usunięciu zielony. Drzewo czyste poza folderem walkthrough.
- Evidence: dotnet test --filter-method RejestrCzasu.Tests.MigrationRulesTests.AppMigrations_AfterInitial_AreAdditiveOnly
- Open: to samo na GitHubie (job czerwony, scalenie zablokowane) po ustawieniu ochrony main.

## 4 — Blok 4 Kontrola migracji — Thoughts

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: sonda: tymczasowe wymagane właściwości bool/string w Project, `dotnet ef migrations add Probe`, test kontroli, potem `migrations remove` i przywrócenie Project.cs.
- Result: EF sam dopisuje defaultValue (false, "") dla wymaganych kolumn w istniejącej tabeli → kontrola je przepuszcza, zgodnie z AD-21. Kolumna identity NOT NULL bez default byłaby odrzucona — accepted as-is (identity powstaje w CreateTable). Domyślne "" dla tekstu to decyzja o danych przy konkretnej migracji.
- Evidence: tymczasowa migracja Probe (usunięta); drzewo czyste.

## 5 — Blok 4 Kontrola migracji — Second opinion

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: niezależny subagent porównał FindViolations z AD-21.
- Result: luki: DropIndex/DropUniqueConstraint/DropForeignKey przechodziły (stary obraz traci unikalność lub RESTRICT); fałszywy alarm: unikalny CreateIndex na nowej tabeli blokuje każdy przyszły Słownik (wzorzec AD-7). Accepted as-is: AlterColumn odrzuca też poszerzenie maxLength; AddCheckConstraint/DeleteData/UpdateData (brak w src); FK z defaultValue 0 psuje wdrożenie, nie rollback. Rozpoznawanie Initial po ID i przypięcie jej treści — deferred do 4.8 (wpis 2).

## 6 — Blok 4 Kontrola migracji — Edit (decyzja Autora: lista dozwolonych)

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: FindViolations przepisane na allowlistę: CreateTable, EnsureSchema, CreateSequence, AddColumn NULL lub z default/DefaultValueSql/ComputedColumnSql, nieunikalny CreateIndex; unikalny CreateIndex, AddUniqueConstraint, AddPrimaryKey, AddForeignKey tylko na tabeli z CreateTable w tej samej migracji; reszta to naruszenie. AdditiveTestMigration rozszerzona o nową tabelę z kluczem alternatywnym, unikalnym indeksem i FK; teoria +4 przypadki (DropIndex, DropUniqueConstraint, DropForeignKey, AddForeignKey na istniejącej tabeli).
- Result: build 0 ostrzeżeń, MigrationRulesTests 16/16, cały zestaw 28/28, has-pending-model-changes bez zmian. Odstępstwo od dosłownego AD-21 (unikalny indeks na nowej tabeli dozwolony) — do odnotowania w architekturze.
- Evidence: tests/RejestrCzasu.Tests/MigrationRulesTests.cs (FindViolations)
- Open: niezacommitowane; aktualizacja tekstu AD-21.

## 7 — Blok 4/5 — commity i AD-21

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: commit allowlisty a541e75f9d8b7848807293fe2420cdba8e7e88a3; tekst AD-21 w architekturze dopasowany do allowlisty (wyjątek: unikalny indeks/klucz/FK na tabeli z tej samej migracji), commit e1201855e6576c6b3c4e5832a184050f1c1bf99a.
- Result: wpis 6 Open zamknięty (commit i AD-21). Lokalnie 2 commity przed origin/4-1-ci-na-pr.
- Open: push po przeglądzie.

## 8 — Blok 5 Ochrona main — Test (krok 1: CI na GitHubie)

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: Autor otworzył PR #1 (4-1-ci-na-pr → main) na commicie e120185; odczytał log kroku Test joba build-and-test.
- Result: job zielony (1/1); `dotnet test --no-build` na ubuntu-latest: 28/28, 0 failed, Testcontainers działa; w pokazanym logu kroku Test brak haseł. PR #2 (4-2-nowy-schemat-initial, z innej sesji) bez sprawdzenia — gałąź nie ma jeszcze pr.yml.
- Evidence: PR #1 → Checks → build-and-test → Test.
- Open: krok 2 (reguła ochrony main wg docs/admin/README.md), krok 3 (PR z czerwonym testem zablokowany); możliwy konflikt w architecture-rejestr-czasu.md (AD-21) przy Update branch PR #2.

## 9 — Blok 5 Ochrona main — Test (krok 2) i decyzja

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: Autor ustawił klasyczną regułę ochrony main.
- Result: reguła zapisana, status „Not enforced” — prywatne repo na GitHub Free nie egzekwuje ochrony gałęzi. Decyzja Autora: bez egzekwowania (odrzucone: upublicznienie, GitHub Pro). docs/admin/README.md opisuje zasadę ręczną; kryterium akceptacji 4.1 w planie dopasowane; ryzyko dla 4.8 i przypięcie Initial zapisane w deferred-work.md. Commit 55f3ab49c5361b21004860f5c69cdb149c285432. Krok 3 (blokada PR z czerwonym testem) pominięty — nie da się go wykazać bez egzekwowania.
- Open: push 55f3ab4; kryterium „verify” ticketu 4.1 w pliku epiku nadal mówi „GitHub nie pozwala scalić” — do zmiany przez PO (ticketów nie edytujemy w build/walkthrough).

## 10 — Blok 6 Peryferia — Thoughts i wrap-up

Session: unavailable · Timestamp: 2026-10-31T00:00:00+01:00

- Action: przeszukanie repo pod kątem SDK 10.0.401; wrap-up na prośbę Autora.
- Result: 10.0.401 tylko w zapisach historycznych (memlog, plan 1.1, Code Map planu 4.1) — accepted as-is. Dockerfile 4.5 ma używać SDK 10.0.403 — deferred (deferred-work.md). Przegląd zakończony: 6/6 bloków zaakceptowanych.
- Evidence: global.json, README.md:10, architecture-rejestr-czasu.md:206
- Open: scalenie PR #1 ręcznie po zielonym build-and-test; Update branch PR #2 (możliwy konflikt AD-21); verify ticketu 4.1 w epiku do zmiany przez PO.
