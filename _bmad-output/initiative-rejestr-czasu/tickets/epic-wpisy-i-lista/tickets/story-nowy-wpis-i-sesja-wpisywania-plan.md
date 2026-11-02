---
title: 'Nowy wpis i Sesja wpisywania'
type: 'feature'
ticket: '1'
created: '2026-11-01'
status: 'built'
baseline_revision: 'afcce4adb903e12c64756c14eebec72825d1f121'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/glossary.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-wpisy-i-lista/epic-wpisy-i-lista.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Przycisk Nowy wpis prowadzi do „nie znaleziono”; User nie ma jak zapisać Wpisu, a Sesja wpisywania (kilkanaście Wpisów z notatek, każdy do 10 s samą klawiaturą) to główna praca w aplikacji (R4, R5, R16, R2, R26).

**Approach:** Jeden komponent `Shared/EntryForm` (AD-10) hostowany na stronie `/wpisy/nowy`, zapis przez klasę danych `Features/Wpisy/EntryStore` na `UserDbContextFactory`, walidator FluentValidation przez Blazilla z regułami Czasu z `Domain/Czas`, listy Projektu i Typu pracy z `DictionaryOptions` jako `FluentCombobox` (wpisanie początku kodu lub nazwy i Tab/Enter), pamięć daty `Shared/EntryDateMemory` dla kopii z historii 2.3 i temat pomocy `wpisy-nowy`.

## Boundaries & Constraints

**Always:**
- **Pola i kolejność fokusu:** Data → Projekt → Typ pracy → Opis → Czas; obok daty dzień tygodnia z `PolishCalendar.DayName`. Data: `FluentDatePicker<DateOnly>` z `RenderStyle = DatePickerRenderStyle.Native` (datę da się wpisać z klawiatury). Projekt: `FluentCombobox` z tekstem opcji „KOD — nazwa”, Typ pracy: `FluentCombobox` z nazwą; opcje wyłącznie z `DictionaryOptions.ProjectsAsync(null)` / `WorkTypesAsync(null)`. Opis: `FluentTextInput`. Czas: `FluentTextInput` z podpowiedzią „np. 1h 30m”. **Format Czasu jak w Jirze (zmiana 01.11.2026, Autor):** wpisywanie `1h 30m`, `1h30m`, `2h`, `45m`, `90m`, `1,5h`, `1.5h` i `1:30`; wyświetlanie przez `Czas.Format` jako `1h 30m`, `2h`, `45m`, sumy `30h 30m`, zero `0h`.
- **Zapis:** Enter w polach Data, Opis i Czas zapisuje formularz (bez okna potwierdzenia); Enter w otwartej liście combobox tylko wybiera pozycję. Przycisk „Zapisz” jest też dostępny. W trakcie zapisu kolejny zapis jest ignorowany.
- **Walidator `EntryFormValidator`** (jeden, AD-8): data wymagana, Projekt i Typ pracy wybrane, opis wymagany po obcięciu spacji, Czas przez `Czas.TryParse` i `Czas.IsValidEntry` z komunikatem „Podaj Czas, np. 1h 30m, 45m albo 1:30 — od 15m do 24h, w krokach co 15 minut.”; polskie komunikaty przy polach; formularz z `novalidate`. Suma dnia ponad 24 h nie jest sprawdzana.
- **Po udanym zapisie (R5):** formularz zostaje, data bez zmian, Projekt, Typ pracy, Opis i Czas puste, fokus na Projekcie, `Notify.Success` z krótkim opisem Wpisu (data, kod Projektu, Czas).
- **Data przy otwarciu:** przy każdym otwarciu `/wpisy/nowy` data = `PolishCalendar.Today(TimeProvider)`.
- **`EntryStore.AddAsync`:** jeden kontekst `UserDbContextFactory` (AD-4, AD-16), opis po `Trim()`, minuty z `Czas.TryParse`; Projekt/Typ pracy usunięty w międzyczasie → naruszenie klucza obcego rozpoznane przez `DbErrors.IsForeignKeyViolation` → wynik „pozycja nie istnieje”, ekran pokazuje `Notify.Error` „Wybrany Projekt lub Typ pracy już nie istnieje.” i wczytuje opcje ponownie, wartości formularza zostają. Wynik jako mały rekord jak `ProjectChange`.
- **`EntryDateMemory`:** singleton w pamięci serwera, klucz `UserId` (z `CurrentUser`), wartość `DateOnly`; strona `/wpisy/nowy` zapisuje do niej datę przy otwarciu i przy każdej zmianie daty; endpoint `/konto/wyloguj` czyści wpis zalogowanego Usera przed wylogowaniem. Metoda odczytu (dla 2.3) zwraca `DateOnly?`.
- **`EntryForm`** to komponent prezentacji: przyjmuje model, opcje i wywołanie zapisu; nie tworzy kontekstu bazy, nie zna hosta, żeby historia 2.3 osadziła go w oknie dialogowym.
- `PageHeader` „Nowy wpis” z `Topic="wpisy-nowy"`; temat `wpisy-nowy` (Zalogowany) w `HelpTopics` z opisem pól, klawiatury i Sesji wpisywania. Rejestracja usług w `Features/ScreenServiceCollectionExtensions.AddAppScreens` (store) i obok `EntryDateMemory` (singleton).

**Never:**
- Lista wpisów, edycja, kopia, usuwanie i okno dialogowe (historie 2.2 i 2.3); zmiany schematu, `ReportGrid`, `DictionaryOptions`, `Notify`, `Confirm`.
- `DateTime.Today`, `DateTime.Now`, `DateOnly.FromDateTime(DateTime.Now)`; ręczne `UserId`; komponenty z AD-12 (`FluentNumberInput`, `MaskPattern`, `FluentSortableList`), `FluentAutocomplete` z wyszukiwaniem po serwerze.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Otwarcie | wejście na `/wpisy/nowy` | data dzisiejsza (Europe/Warsaw), pozostałe puste, fokus na dacie | — |
| Zapis | data, RDZ, Development, „Spotkanie”, `1h 30m` + Enter | Wpis zapisany (90 min, user_id Usera); data zostaje, reszta pusta, fokus na Projekcie, komunikat sukcesu | — |
| Czas graniczny | `15m`, `24h`, `0:15`, `24:00` | przyjęty | — |
| Czas błędny | `10m`, `24h 15m`, `0h`, `1,5` (bez jednostki), puste | brak zapisu | polski komunikat przy Czasie |
| Brak pól | pusty opis / brak Projektu / brak Typu pracy | brak zapisu | komunikat przy polu |
| Nieaktywne pozycje | Projekt nieaktywny | nie ma go na liście wyboru | — |
| Pozycja usunięta w międzyczasie | Projekt usunięty po otwarciu formularza | brak zapisu, opcje wczytane ponownie | „Wybrany Projekt lub Typ pracy już nie istnieje.” |
| Suma dnia > 24 h | dwa Wpisy po `24:00` tego samego dnia | oba zapisane | — |
| Pamięć daty | zmiana daty na 2026-09-03 | `EntryDateMemory` zwraca 2026-09-03 dla tego Usera, `null` dla innego | wylogowanie czyści |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Features/Projekty/ProjectStore.cs`, `ProjectForm.cs`, `ProjectFormValidator.cs`, `ProjectsPage.razor` -- wzorzec: klasa danych z wynikiem-rekordem, walidator Blazilla (`<FluentValidator Validator=…>`), `EditForm novalidate`, `Notify`, `_busy`.
- `src/RejestrCzasu/Data/TimeEntry.cs` -- encja (`Date` `DateOnly`, `ProjectId`, `WorkTypeId`, `Description`, `Minutes`); `AppDbContext.TimeEntries`.
- `src/RejestrCzasu/Data/DictionaryOptions.cs` -- `ProjectsAsync(int?)`, `WorkTypesAsync(int?)` zwracają `ProjectOption(Id, Code, Name, IsActive)` / `WorkTypeOption`; już zarejestrowane w `AddAppDatabase`.
- `src/RejestrCzasu/Data/DbErrors.cs` -- `IsForeignKeyViolation` (23503, 23001).
- `src/RejestrCzasu/Domain/Czas.cs` (`TryParse`, `IsValidEntry`, `Format`), `Domain/PolishCalendar.cs` (`Today(TimeProvider)`, `DayName(DateOnly)`); `TimeProvider` zarejestrowany jako singleton w `Program.cs`.
- `src/RejestrCzasu/Accounts/CurrentUser.cs` (`GetUserIdAsync`), `Accounts/AccountEndpoints.cs` (POST `/konto/wyloguj`) -- czyszczenie pamięci daty przed `SignOutAsync`.
- `src/RejestrCzasu/Features/ScreenServiceCollectionExtensions.cs` -- `AddAppScreens()`.
- `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/ProjektyHelp.razor` -- dopisać `wpisy-nowy`.
- `src/RejestrCzasu/Shared/MainLayout.razor` -- przycisk menu Nowy wpis już prowadzi do `/wpisy/nowy`.
- Fluent UI 5.0.1: `FluentCombobox<TOption,TValue>` (`Items`, `OptionText`, `OptionValue`, `@bind-Value`, `Label`, `Autofocus`; wpisanie początku tekstu podświetla pierwszą pasującą opcję, Enter/Tab wybiera), `FluentDatePicker<DateOnly>` (`RenderStyle`), `FluentTextInput` (`Element` do `FocusAsync`), `FluentValidationMessage`.
- `tests/RejestrCzasu.Tests/Features/ProjectStoreTests.cs`, `ProjectsPageTests.cs`, `PostgresFixture.cs` -- wzorzec testów Testcontainers i bUnit z `MainLayout`.

## Tasks & Acceptance

**Execution:**
- [ ] `src/RejestrCzasu/Features/Wpisy/EntryStore.cs` -- `AddAsync` z wynikiem; rejestracja w `AddAppScreens`.
- [ ] `src/RejestrCzasu/Shared/EntryInput.cs`, `Shared/EntryFormValidator.cs` -- model formularza i walidator -- AD-8.
- [ ] `src/RejestrCzasu/Shared/EntryForm.razor` -- formularz prezentacji z kolejnością fokusu, Enterem i metodą ustawienia fokusu na Projekcie -- AD-10.
- [ ] `src/RejestrCzasu/Shared/EntryDateMemory.cs` + rejestracja singleton + czyszczenie w `AccountEndpoints` -- R7 (przygotowanie).
- [ ] `src/RejestrCzasu/Features/Wpisy/NewEntryPage.razor` -- `/wpisy/nowy` -- R4, R5, R16.
- [ ] `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/WpisyNowyHelp.razor` -- R26.
- [ ] `tests/RejestrCzasu.Tests/Features/EntryStoreTests.cs` (Testcontainers: zapis z user_id, oddzielenie, usunięty Projekt), `Shared/EntryFormValidatorTests.cs`, `Features/NewEntryPageTests.cs` (bUnit: data przy otwarciu z podstawionym `TimeProvider`, zapis i stan po zapisie, komunikaty, brak nieaktywnych opcji, pamięć daty), `Shared/EntryDateMemoryTests.cs`.

**Acceptance Criteria:**
- Given `dotnet run` i zalogowany User z Projektami i Typami pracy, when wpisuje kilkanaście Wpisów samą klawiaturą (data, początek kodu + Tab, początek nazwy + Tab, opis, Czas, Enter), then każdy Wpis zajmuje nie więcej niż 10 s i trafia do bazy, a `?` otwiera temat `wpisy-nowy`.

## Implementation Notes

## Plan Change Log

## Review Triage Log

Przebieg 1 (quick, 01.11.2026): medium 2, low 2, odrzucone 1.

| Uwaga | Werdykt | Ścieżka | Dowód / działanie |
|---|---|---|---|
| Spacje wewnątrz liczby sklejane przed odczytem (`1 5h` → 15h) | medium | patch | `TryParseUnits` usuwa wszystkie białe znaki przed wyrażeniem regularnym; spacje dozwolone tylko między liczbą a jednostką i między grupami. |
| Enter w Dacie zapisuje na `keyup` — Enter, który otworzył ekran, może wysłać pusty formularz | medium | patch | Zapis z Daty tylko, gdy `keydown` Enter też był w polu Daty. |
| Słownik, AD-6 i opis historii 2.2 nadal z `h:mm` / bez `1.5h` | low | patch | Poprawione `glossary.md`, AD-6, `tickets.toml` (2.2). |
| Kolejność `using` w `AccountsTests.cs` | low | patch | Poprawiona. |
| Data jako `DateOnly?` zamiast `DateOnly` z planu | low | odrzucona | Celowe: wyczyszczone pole daje komunikat „Podaj datę.” zamiast zapisu starej daty; zmiana dotyczyłaby tylko tekstu planu. |

## Design Notes

- Enter z pól wewnątrz web components Fluent nie musi wywołać niejawnego wysłania formularza (pola są w shadow DOM): obsługa `keydown` Enter na polach Data, Opis i Czas, które wołają wysłanie `EditForm` (np. przez `EditContext.Validate()` i zapis), jest pewniejsza niż poleganie na przeglądarce. Combobox z otwartą listą sam blokuje Enter; nie podpinać obsługi Enter do comboboxów.
- Kolejność fokusu wynika z kolejności pól w znaczniku (bez `tabindex` > 0).

## Verification

**Commands:**
- `dotnet build -c Release` -- expected: bez błędów i ostrzeżeń.
- `dotnet test -c Release --no-build` -- expected: wszystkie testy zielone (Docker Desktop działa).

**Manual checks (if no CLI):**
- `dotnet run`: Sesja wpisywania 10 Wpisów samą klawiaturą ze stoperem; Enter w otwartej liście tylko wybiera; po zapisie fokus na Projekcie; Czas `0:10` odrzucony.
