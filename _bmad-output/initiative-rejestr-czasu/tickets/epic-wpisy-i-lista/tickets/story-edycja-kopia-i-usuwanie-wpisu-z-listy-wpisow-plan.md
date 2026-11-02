---
title: 'Edycja, kopia i usuwanie Wpisu z Listy wpisów'
type: 'feature'
ticket: '3'
created: '2026-11-02'
status: 'built'
baseline_revision: 'c9996b7dfafe171fcd7111f60ddea388262c37ae'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-wpisy-i-lista/epic-wpisy-i-lista.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/glossary.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Na Liście wpisów User nie może poprawić, skopiować ani usunąć Wpisu (R6, R7, R8); epik 3 (Widok miesiąca) potrzebuje tego samego okna Wpisu i jednej operacji usunięcia (AD-10).

**Approach:** Usługa `Features/Wpisy/EntryDialogs` z trzema operacjami po `Id` Wpisu — `EditAsync`, `CopyAsync`, `DeleteAsync` (to jest `DeleteEntry` z AD-10) — każda zwraca, czy raport ma wczytać dane ponownie. Edycja i kopia otwierają okno `Features/Wpisy/EntryDialog.razor` (`IDialogService`) z `Shared/EntryForm`; usunięcie po `Shared/Confirm`. `EntryStore` dostaje `FindAsync`, `UpdateAsync`, `DeleteAsync`. Lista wpisów dodaje ikony Edytuj, Kopiuj, Usuń i po każdej zmianie wywołuje `ReloadAsync`.

## Boundaries & Constraints

**Always:**
- **Edycja:** okno „Edycja Wpisu” z datą, Projektem, Typem pracy, opisem i Czasem (`Czas.Format`) Wpisu wczytanego z bazy; opcje `DictionaryOptions.ProjectsAsync(projectId)` / `WorkTypesAsync(workTypeId)` (bieżąca wartość także nieaktywna, R2); Zapisz → `UpdateAsync` każdego pola, także w poprzednich Miesiącach.
- **Kopia:** okno „Kopia Wpisu” z Projektem, Typem pracy, opisem i Czasem oryginału; data = `EntryDateMemory.Find(userId)`, a bez niej `PolishCalendar.Today(TimeProvider)`; opcje z `null` (tylko aktywne) — nieaktywny Projekt lub Typ pracy oryginału zostawia to pole puste (R7). Zapisz → `AddAsync` (nowy Wpis); okno kopii nie zmienia `EntryDateMemory`.
- **Okno:** zapis jak w `EntryForm` (Enter, bez potwierdzenia); po zapisie okno się zamyka, `Notify.Success` (tekst jak `NewEntryPage.SavedMessage`), raport wczytuje dane; **Anuluj** i Escape zamykają bez zapisu i bez wczytywania. Stopka biblioteki ukryta i bez skrótów (jak `Confirm`), przycisk Anuluj w treści okna. `MissingDictionaryItem` → okno zostaje otwarte z wpisanymi wartościami, `Notify.Error`, opcje wczytane ponownie.
- **Usunięcie:** `Confirm.AskAsync("Usuń Wpis", "Usunąć Wpis z {dd.MM.yyyy} ({KOD}, {Czas}): {opis}?", "Usuń")`; potwierdzone → `EntryStore.DeleteAsync`, `Notify.Success`, wczytanie; Anuluj → nic.
- **Wpis już nie istnieje** (edycja, kopia, usunięcie, zapis edycji): `Notify.Warning("Ten Wpis już nie istnieje. Lista została wczytana ponownie.")`, okno zamknięte, raport wczytuje dane.
- `EntryStore`: jeden kontekst na operację, filtr Usera z `AppDbContext` (AD-4, AD-16) — Wpis innego Usera to `NotFound`; `EntryChange` dostaje `NotFound`.
- Temat pomocy `wpisy` opisuje ikony Edytuj, Kopiuj, Usuń i datę kopii.

**Never:**
- Drugi formularz Wpisu lub zmiany zachowania `EntryForm` dla strony `/wpisy/nowy`; zależność `Shared/` od `Features/`.
- Edycja inline w siatce; zmiany `ReportGrid`, `DictionaryOptions`, schematu.
- Ręczne `UserId` / `IgnoreQueryFilters`; `DateTime.Today`/`Now`; `ShowMessageBoxAsync`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Edycja | Wpis z sierpnia, zmiana Czasu i Projektu, Zapisz | Wpis zmieniony w bazie, okno zamknięte, lista z bazy | — |
| Edycja nieaktywnego | Wpis z nieaktywnym Projektem OLD | OLD wybrany w polu i na liście opcji | — |
| Kopia z pamięcią | Nowy wpis otwarty z datą 2026-09-10 | kopia z datą 2026-09-10, wartości oryginału | — |
| Kopia bez pamięci | brak daty w `EntryDateMemory` | data dzisiejsza (Europe/Warsaw) | — |
| Kopia nieaktywnego | oryginał z nieaktywnym Projektem | pole Projekt puste, OLD nie w opcjach | — |
| Anuluj | edycja lub kopia, zmiany, Anuluj | nic nie zapisane, liczba Wpisów bez zmian | — |
| Usunięcie | Usuń → potwierdź | Wpis usunięty, lista z bazy | — |
| Usunięcie anulowane | Usuń → Anuluj | Wpis zostaje | — |
| Drugi User | `Id` Wpisu Usera B | Find/Update/Delete → `NotFound`, Wpis B nietknięty | ostrzeżenie |
| Usunięty w międzyczasie | Wpis usunięty w innym oknie | `NotFound` | ostrzeżenie, wczytanie |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Features/Wpisy/EntryStore.cs` -- dopisać `FindAsync(id)` → `EntryRow?` (ta sama projekcja co `ListAsync`), `UpdateAsync(id, date, projectId, workTypeId, description, czas)` (walidacja Czasu jak `AddAsync`, FK → `MissingDictionaryItem`, brak → `NotFound`), `DeleteAsync(id)` → `Done`/`NotFound`; `EntryChange.NotFound`.
- `src/RejestrCzasu/Shared/EntryForm.razor` -- hostowany bez zmian: `Model`, `Projects`, `WorkTypes`, `OnSave`; Zapisz wewnątrz; `Autofocus` na Dacie.
- `src/RejestrCzasu/Shared/EntryInput.cs` -- model okna; `ProjectOption`/`WorkTypeOption` z `Data/DictionaryOptions.cs` (rekordy — pole wypełniać instancją z listy opcji, wybraną po `Id`).
- `src/RejestrCzasu/Shared/Confirm.cs`, `ConfirmDialog.razor` -- wzorzec okna: `ShowDialogAsync<T>`, `HtmlEncode` tytułu, `HideLibraryAction`, `FluentDialogBody` z `ActionTemplate`, `[CascadingParameter] IDialogInstance` (`CloseAsync(result)`/`CancelAsync`).
- `src/RejestrCzasu/Shared/EntryDateMemory.cs` (`Find`), `Accounts/CurrentUser.cs` (`GetUserIdAsync`), `Domain/PolishCalendar.cs`, `Domain/Czas.cs`.
- `src/RejestrCzasu/Features/Wpisy/NewEntryPage.razor` -- `SavedMessage`, `MissingDictionaryItemMessage`, obsługa `EntryChange`; bez zmian zachowania.
- `src/RejestrCzasu/Features/Wpisy/EntriesPage.razor` -- dodać `_actions` (`Icons.Regular.Size20.Edit`, `Copy`, `Delete`) do `ReportGrid` i wywołanie `ReloadAsync` po `true`; wzorzec `Features/Projekty/ProjectsPage.razor`.
- `src/RejestrCzasu/Features/ScreenServiceCollectionExtensions.cs` -- `AddScoped<EntryDialogs>()`.
- `src/RejestrCzasu/Features/Pomoc/Topics/WpisyHelp.razor` -- sekcja akcji w wierszu.
- Testy: `tests/RejestrCzasu.Tests/Features/EntryStoreTests.cs`, `EntriesPageTests.cs` (bUnit z `MainLayout`, okno przez `FluentProviders`; `.confirm-primary` jak `ProjectsPageTests.cs:188`).

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Features/Wpisy/EntryStore.cs` -- `FindAsync`, `UpdateAsync`, `DeleteAsync`, `EntryChange.NotFound` -- R6, AD-16.
- [x] `src/RejestrCzasu/Features/Wpisy/EntryDialog.razor` -- okno z `EntryForm` i Anuluj; parametry: model, opcje, `int? EntryId` (null = kopia); zapis przez `EntryStore`, zamknięcie z wynikiem -- AD-10.
- [x] `src/RejestrCzasu/Features/Wpisy/EntryDialogs.cs` -- `EditAsync`, `CopyAsync`, `DeleteAsync` zwracające `bool` (wczytać raport); rejestracja w `AddAppScreens` -- R6, R7.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesPage.razor` -- ikony w wierszu -- R8.
- [x] `src/RejestrCzasu/Features/Pomoc/Topics/WpisyHelp.razor` -- akcje -- R26.
- [x] `tests/RejestrCzasu.Tests/Features/EntryStoreTests.cs` -- Find/Update/Delete, drugi User, FK innego Usera, nieistniejący.
- [x] `tests/RejestrCzasu.Tests/Features/EntriesPageTests.cs` (lub nowy `EntryDialogsTests.cs`) -- bUnit: wiersze macierzy (okno edycji z wartościami, kopia z pamięcią i bez, kopia z nieaktywnym, Anuluj, usunięcie potwierdzone/anulowane, nieistniejący).

**Acceptance Criteria:**
- Given `dotnet run` i Wpisy z dwóch Miesięcy, when User edytuje Wpis z poprzedniego Miesiąca, kopiuje Wpis po otwarciu Nowego wpisu i usuwa Wpis z potwierdzeniem, then lista po każdej zmianie pokazuje dane z bazy, a kopia ma datę z Nowego wpisu i nie zapisuje się przed Zapisz.

## Implementation Notes

- `EntryStore.UpdateAsync`/`DeleteAsync`: `DbUpdateConcurrencyException` (Wpis usunięty między odczytem a zapisem) → `NotFound`.
- `EntryDialog` dostaje dodatkowo `CurrentProjectId`/`CurrentWorkTypeId` (ponowne wczytanie opcji po `MissingDictionaryItem` z bieżącą wartością przy edycji, `null` przy kopii).
- Usunięcie: `Notify.Success("Usunięto Wpis: {dd.MM.yyyy}, {KOD}, {Czas}.")`.
- Testy bUnit w `EntriesPageTests.cs`; Escape sprawdzany zdarzeniem `ondialogbeforetoggle` jak w `NotifyConfirmTests`.

## Plan Change Log

## Review Triage Log

Przebieg 1 (quick, 02.11.2026): low 4, false 1.

| # | Uwaga | Werdykt | Ścieżka | Dowód / działanie |
|---|-------|---------|---------|-------------------|
| 1 | AC ręczne (`dotnet run`) nie odnotowane | false | odrzucona | To kontrola Autora po zbudowaniu, nie defekt kodu; zgłoszona Autorowi. |
| 2 | Test edycji nie zmienia Projektu (wiersz macierzy „Edycja”) | low | patch | Test zmienia RDZ → ABC, sprawdza `ProjectId` i kod na liście. |
| 3 | Brak testu `MissingDictionaryItem` w oknie | low | patch | Nowy test: Typ pracy usunięty przed Zapisz — okno otwarte, wartości zostają, błąd, opcje wczytane ponownie. |
| 4 | Test Escape nie sprawdza zamknięcia i braku wczytania | low | odrzucona | bUnit nie usuwa `fluent-dialog` (robi to JS biblioteki); poprawka wymagałaby nowego mechanizmu w testach; sprawdza ręczna kontrola. |
| 5 | `entry-dialog-buttons` bez CSS | low | patch | Reguła w `app.css` (flex, odstępy jak w formularzach Słowników). |

## Design Notes

- `EntryDialogs` jest w `Features/Wpisy`, bo zna `EntryStore` (`Shared/` nie zależy od `Features/`); epik 3 wywołuje te same trzy metody z Widoku miesiąca po `Id` Wpisu — dlatego wejściem jest `Id`, nie `EntryRow`.
- Okno samo zapisuje przez `EntryStore` i zamyka się dopiero po `Done` / `NotFound`, żeby `MissingDictionaryItem` zostawiał wpisane wartości.

## Verification

**Commands:**
- `dotnet build -c Release` -- expected: bez błędów i ostrzeżeń.
- `dotnet test -c Release --no-build` -- expected: wszystkie testy zielone (Docker Desktop działa).

**Manual checks (if no CLI):**
- `dotnet run`: na `/wpisy` edycja Wpisu z poprzedniego Miesiąca, kopia po otwarciu Nowego wpisu z inną datą, Anuluj w oknie, usunięcie z potwierdzeniem; Escape zamyka okno.
