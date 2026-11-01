---
title: 'Projekty: lista, dodawanie, edycja, dezaktywacja i usuwanie'
type: 'feature'
ticket: '7'
created: '2026-11-01'
status: 'built'
baseline_revision: '71ece42bb07eb4786c2b95e76e44ee04ac4af75b'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/glossary.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** User nie ma gdzie prowadzić swoich Projektów: pozycja menu Słowniki → Projekty prowadzi do „nie znaleziono” (R1–R3, R17, R26).

**Approach:** Ekran `/projekty` na `ReportGrid` i układzie z historii 1.3, z formularzem dodawania i edycji (FluentValidation przez Blazilla), dezaktywacją, przywróceniem i usuwaniem tylko bez Wpisów; logika danych w jednej klasie testowanej na Testcontainers, duplikat kodu rozpoznawany przez nowe `Data/DbErrors`, temat pomocy `projekty`.

## Boundaries & Constraints

**Always:**
- **Dane (AD-4, AD-16):** `Features/Projekty/ProjectStore` używa wyłącznie `UserDbContextFactory`, jeden kontekst na operację, wynik zmaterializowany; ekran nie dotyka `AppDbContext`. Lista posortowana w C# po kodzie (`pl-PL`, jak `DictionaryOptions`), potem po `Id`.
- **Siatka:** `ReportGrid<T>` z kolumnami Kod, Nazwa, Stan („Aktywny” / „Nieaktywny”) i akcjami: Edytuj, Dezaktywuj (tylko aktywny), Przywróć (tylko nieaktywny), Usuń.
- **Formularz:** przycisk „Dodaj Projekt” otwiera formularz nad siatką (Kod, Nazwa, Zapisz, Anuluj); Edytuj otwiera ten sam formularz z wartościami. Jeden walidator FluentValidation (`ProjectFormValidator`) przez Blazilla: Kod i Nazwa wymagane po obcięciu spacji, Kod najwyżej `Project.CodeMaxLength`, Nazwa najwyżej `Project.NameMaxLength`, polskie komunikaty; walidator nie sprawdza unikalności (AD-8). Wartości zapisywane po `Trim()`; wielkie litery kodu nadaje istniejąca normalizacja w `AppDbContext`.
- **Duplikat kodu (AD-7):** naruszenie indeksu `ix_project_user_id_code` rozpoznaje wyłącznie `Data/DbErrors` (`PostgresException` 23505 z nazwą ograniczenia); ekran pokazuje „Projekt o kodzie {KOD} już istnieje.” w `Notify` (błąd), formularz zostaje otwarty z wpisanymi wartościami. `DbErrors` ma też rozpoznanie naruszenia klucza obcego (23503) i stałą nazwy indeksu Typu pracy `ux_work_type_user_id_name_lower` dla historii 1.8.
- **Usuwanie (R3):** po `Confirm.AskAsync` („Usuń”); liczba Wpisów z zapytania przed próbą; przy liczbie > 0 nic nie jest usuwane, a `Notify` (ostrzeżenie) mówi „Projekt {KOD} występuje w {n} {Wpisie|Wpisach}. Zamiast go usuwać, dezaktywuj go.” (1 → „Wpisie”, inne → „Wpisach”). Naruszenie klucza obcego przy samym usunięciu (Wpis dodany w międzyczasie) daje ten sam komunikat z liczbą policzoną ponownie.
- Po każdej zmianie ekran wczytuje listę ponownie z bazy (AD-4) i pokazuje krótki komunikat sukcesu w `Notify`.
- `PageHeader` z tytułem „Projekty”, podtytułem i `Topic="projekty"`, bez okruszków; temat `projekty` (Zalogowany) w `HelpTopics` z treścią opisującą dokładnie działanie ekranu.
- README: sekcja z poleceniem `docker exec … psql` dodającym testowy Typ pracy i Wpis z podanym `user_id` i Projektem, do ręcznego sprawdzenia blokady usunięcia.

**Never:**
- Ekran Typów pracy (historia 1.8), formularz Wpisu, zmiany schematu i migracji, zmiany `ReportGrid`, `MainLayout`, `Notify`, `Confirm`.
- Sprawdzanie unikalności zapytaniem przed zapisem, `IgnoreQueryFilters`, ręczne ustawianie `UserId`.
- Komponenty z AD-12 (`FluentNumberInput`, `MaskPattern`, `FluentSortableList`).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Dodanie | Kod „rdz”, Nazwa „Rozwój” | Projekt RDZ na liście, aktywny | — |
| Duplikat innej wielkości | istnieje RDZ, dodanie „Rdz” | nic nie zapisane | komunikat o istniejącym kodzie RDZ, formularz otwarty |
| Inny User | User B dodaje RDZ, gdy A ma RDZ | zapis się udaje; B nie widzi Projektów A | — |
| Walidacja | pusty Kod albo Nazwa, kod > 20 znaków | brak zapisu, polski komunikat przy polu | — |
| Edycja | zmiana nazwy i kodu RDZ → „abc” | lista pokazuje ABC z nową nazwą; Wpisy wskazują ten sam Projekt | duplikat jak przy dodaniu |
| Dezaktywacja / przywrócenie | aktywny / nieaktywny Projekt | Stan zmienia się na „Nieaktywny” / „Aktywny” | — |
| Usunięcie bez Wpisów | Projekt bez Wpisów, potwierdzone | znika z listy | Anuluj → nic się nie dzieje |
| Usunięcie z Wpisami | Projekt w 3 Wpisach | nic nie usunięte | „…występuje w 3 Wpisach…” |
| Jeden Wpis | Projekt w 1 Wpisie | nic nie usunięte | „…występuje w 1 Wpisie…” |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Data/Project.cs` -- encja (`Code`, `Name`, `IsActive`, `CodeMaxLength` 20, `NameMaxLength` 200); bez zmian.
- `src/RejestrCzasu/Data/AppDbContext.cs` -- `Projects`, `TimeEntries`; normalizacja kodu w `NormalizeProjectCodes` (linia ~252) przy `SaveChanges`; indeks `(user_id, code)`; bez zmian.
- `src/RejestrCzasu/Data/Migrations/20261031200135_Initial.cs` -- nazwy: `ix_project_user_id_code` (unikalny), `ux_work_type_user_id_name_lower`, tabela `time_entry` (`user_id`, `date`, `project_id`, `work_type_id`, `description`, `minutes`), `work_type` (`user_id`, `name`, `is_active`).
- `src/RejestrCzasu/Data/UserDbContextFactory.cs`, `Data/DictionaryOptions.cs` -- wzorzec klasy danych (factory, `AsNoTracking`, sortowanie `pl-PL` w C#).
- `src/RejestrCzasu/Shared/ReportGrid.razor`, `ReportColumn.cs`, `ReportAction.cs` (`Visible`) -- siatka i akcje; ikony `Icons.Regular.Size20.Edit/Delete/...`.
- `src/RejestrCzasu/Shared/PageHeader.razor`, `Notify.cs` (`Success/Warning/Error`), `Confirm.cs` (`AskAsync(title, message, primaryLabel)`) -- z historii 1.3.
- `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/StartHelp.razor` -- dopisać temat `projekty`; test `Topics_HaveUniqueKebabCaseKeys_AndStart` pilnuje kluczy.
- `src/RejestrCzasu/RejestrCzasu.csproj` -- dodać `FluentValidation` 12.2.0 i `Blazilla` 2.4.2 (wersje z sekcji Stack architektury).
- `tests/RejestrCzasu.Tests/DictionaryOptionsTests.cs`, `PostgresFixture.cs` (`ResetAsync`, `CreateUserAsync`, `CreateUserDbContextFactory`, `CreateUserContextAsync`) -- wzorzec testów Testcontainers; `Shared/UiTestServices.cs`, `Shared/LayoutTests.cs` -- wzorzec bUnit z 1.3.
- `README.md` -- sekcja „Konto lokalnie” (polecenie `UPDATE users`); obok dopisać polecenie Wpisu testowego.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/RejestrCzasu.csproj` -- pakiety FluentValidation i Blazilla -- AD-8.
- [x] `src/RejestrCzasu/Data/DbErrors.cs` -- rozpoznanie 23505 po nazwie ograniczenia i 23503, stałe nazw indeksów -- jedyne miejsce (AD-7).
- [x] `src/RejestrCzasu/Features/Projekty/ProjectStore.cs` -- lista, dodanie, edycja, zmiana stanu, usunięcie z wynikiem (sukces / duplikat / liczba Wpisów) -- logika testowalna bez UI; rejestracja scoped w `Program.cs`.
- [x] `src/RejestrCzasu/Features/Projekty/ProjectForm.cs` + `ProjectFormValidator.cs` -- model formularza i walidator -- AD-8.
- [x] `src/RejestrCzasu/Features/Projekty/ProjectsPage.razor` -- ekran `/projekty` -- R1–R3, R17.
- [x] `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/ProjektyHelp.razor` -- temat `projekty` -- R26.
- [x] `README.md` -- polecenie Wpisu testowego.
- [x] `tests/RejestrCzasu.Tests/Features/ProjectStoreTests.cs` (Testcontainers) -- macierz: normalizacja, duplikat innej wielkości liter, dwóch Userów z tym samym kodem i oddzielenie list, edycja z duplikatem, dezaktywacja i przywrócenie, blokada usunięcia z liczbą 1 i 3, usunięcie bez Wpisów; `Features/ProjectFormValidatorTests.cs`; `Features/ProjectsPageTests.cs` (bUnit z podstawionym `ProjectStore` lub fabryką) -- komunikat duplikatu, formularz zostaje otwarty, akcje Dezaktywuj/Przywróć zależne od stanu, odmiana „Wpisie/Wpisach”.

**Acceptance Criteria:**
- Given `dotnet run` i zalogowany User, when wybiera Słowniki → Projekty, then widzi ekran Projekty z siatką z filtrem i licznikiem, a `?` otwiera temat pomocy `projekty`.
- Given Projekt z Wpisem dodanym poleceniem z README, when User go usuwa, then widzi liczbę Wpisów i propozycję dezaktywacji, a Projekt zostaje na liście.

## Implementation Notes

- PostgreSQL zgłasza usunięcie Projektu użytego we Wpisie (`ON DELETE RESTRICT`) jako 23001 (`restrict_violation`), nie 23503. `DbErrors.IsForeignKeyViolation` rozpoznaje oba kody (23503 daje np. Wpis wskazujący cudzy Projekt); test `DbErrorsTests` sprawdza oba na prawdziwym schemacie.
- Obsługa kliknięcia ikony w `ReportGrid` należy do `ReportGrid`, więc Blazor po akcji przerysowuje tylko siatkę, nie ekran. `ProjectsPage` opakowuje akcje w `ThenRender` (po akcji `StateHasChanged()` ekranu). Historia 1.8 (Typy pracy) potrzebuje tego samego; `ReportGrid` bez zmian (Never).
- `ProjectChange`: `Done(Code)`, `DuplicateCode(Code)`, `InUse(Code, EntryCount)`, `NotFound` (Projekt usunięty w innym oknie albo cudzy) — `NotFound` daje ostrzeżenie i ponowne wczytanie listy.
- Testy bUnit ekranu używają prawdziwego `ProjectStore` na bazie Testcontainers (ekran w `MainLayout`); `PostgresFixture.CreateLoggedInUserDbContextFactory` zastąpił prywatną metodę z `DictionaryOptionsTests`. Dodatkowo `DbErrorsTests`.

## Plan Change Log

## Review Triage Log

Przebieg 1 (quick, 01.11.2026): medium 1, low 3 (w tym 1 odrzucona), false 0, maybe-false 0.

| Uwaga | Werdykt | Ścieżka | Dowód / działanie |
|---|---|---|---|
| `Required="true"` na polach Fluent: przeglądarka blokuje wysłanie pustego pola, polskie komunikaty walidatora się nie pokazują | medium | patch | Pola Fluent 5 są form-associated z natywnym `required`, `EditForm` bez `novalidate`; poprawka: `novalidate` na formularzu. |
| Polecenie „Wpis testowy” w README dodaje Typ pracy także przy `INSERT 0 0` | low | patch | CTE modyfikujące dane wykonuje się zawsze; poprawka: wstawienie Typu pracy warunkowane istnieniem Projektu. |
| Projekt usunięty w innym oknie między wczytaniem a zapisem daje `DbUpdateConcurrencyException` zamiast `NotFound` | low | odrzucona | Okno czasowe ułamka sekundy przy jednym Userze w dwóch oknach; skutek to ogólny komunikat granicy błędów, poprawka dokłada obsługę wyjątku w trzech metodach. |
| Akcje wiersza bez ochrony przed podwójnym kliknięciem | low | patch | `_busy` chroni tylko zapis formularza; poprawka w istniejącym `ThenRender`. |

## Design Notes

- `ProjectStore` zamiast logiki w komponencie: AD-4 dopuszcza ekran tworzący kontekst, ale reguły R1–R3 muszą mieć testy na Testcontainers, a bUnit nie ma bazy. Historia 1.8 powtórzy ten wzorzec dla Typów pracy.
- Wynik operacji jako mały rekord/enum (np. `Saved`, `DuplicateCode`, `InUse(int count)`), nie wyjątki do ekranu; nieoczekiwane wyjątki idą dalej do `AppErrorBoundary`.

## Verification

**Commands:**
- `dotnet build` -- expected: bez błędów i ostrzeżeń.
- `dotnet test` -- expected: wszystkie testy zielone (Docker Desktop działa).

**Manual checks (if no CLI):**
- `dotnet run`: dodanie „rdz” → RDZ; „Rdz” → komunikat; drugi User (zatwierdzony SQL-em z README) dodaje własny RDZ i nie widzi Projektów pierwszego; Wpis z README → usunięcie pokazuje liczbę Wpisów.
