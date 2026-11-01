---
title: 'Typy pracy: lista, dodawanie, edycja, dezaktywacja i usuwanie'
type: 'feature'
ticket: '8'
created: '2026-11-01'
status: done
baseline_revision: 'ff61904ecdc7c513be8273db66a79feada265739'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/tickets/epic-szkielet-i-slowniki/tickets/story-projekty-lista-dodawanie-edycja-dezaktywacja-i-usuwanie-plan.md'
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/glossary.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Pozycja menu Słowniki → Typy pracy prowadzi do „nie znaleziono”; User nie może prowadzić swoich Typów pracy (R1–R3, R17, R26).

**Approach:** Ekran `/typy-pracy` jako wierna kopia wzorca Projektów z historii 1.7 (`ProjectStore`, formularz z walidatorem, `ProjectsPage`, temat pomocy), z jednym polem Nazwa; duplikat nazwy bez względu na wielkość liter rozpoznaje `DbErrors.WorkTypeNameIndex`.

## Boundaries & Constraints

**Always:**
- Ten sam układ i zachowanie co ekran Projekty (plan 1.7 w `context:` to wzorzec, kod w `Features/Projekty/` to przykład do powtórzenia): `Features/TypyPracy/WorkTypeStore` tylko przez `UserDbContextFactory`, wynik jako rekord (zapisano / duplikat / w użyciu z liczbą / nie istnieje), lista posortowana w C# po nazwie (`pl-PL`), potem po `Id`.
- Siatka: kolumny Nazwa i Stan („Aktywny” / „Nieaktywny”), akcje Edytuj, Dezaktywuj (aktywny), Przywróć (nieaktywny), Usuń; akcje opakowane jak w `ProjectsPage` (`ThenRender` z blokadą `_busy`).
- Formularz nad siatką: przycisk „Dodaj Typ pracy”, pole Nazwa (wymagane po obcięciu spacji, najwyżej `WorkType.NameMaxLength`), `EditForm` z `novalidate`, walidator `WorkTypeFormValidator` przez Blazilla z polskim komunikatem, zapis po `Trim()`; nazwa zapisywana tak, jak ją wpisano (bez zmiany wielkości liter).
- Duplikat: naruszenie `DbErrors.WorkTypeNameIndex` → `Notify` (błąd) „Typ pracy o nazwie {nazwa} już istnieje.”, formularz zostaje otwarty z wartością.
- Usuwanie po `Confirm.AskAsync` („Usuń”); liczba Wpisów z zapytania przed próbą; przy > 0 `Notify` (ostrzeżenie) „Typ pracy {nazwa} występuje w {n} {Wpisie|Wpisach}. Zamiast go usuwać, dezaktywuj go.”; naruszenie klucza obcego przy usunięciu → ten sam komunikat z ponownie policzoną liczbą.
- `PageHeader` „Typy pracy” z podtytułem i `Topic="typy-pracy"`; temat `typy-pracy` (Zalogowany) w `HelpTopics` z treścią zgodną z ekranem.
- README, sekcja „Wpis testowy”: dodać drugie polecenie, które dodaje Wpis z istniejącym Typem pracy o podanej nazwie i dowolnym Projektem tego Konta (pierwszym aktywnym), żeby sprawdzić blokadę usunięcia Typu pracy; sprawdzić je na lokalnej bazie w wycofanej transakcji.

**Never:**
- Zmiany `ReportGrid`, `MainLayout`, `Notify`, `Confirm`, `DbErrors` (poza użyciem), schematu i ekranu Projekty; wspólna klasa bazowa Projektów i Typów pracy.
- Sprawdzanie unikalności zapytaniem przed zapisem, `IgnoreQueryFilters`, ręczne `UserId`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Dodanie | „Development” | na liście „Development”, aktywny | — |
| Duplikat innej wielkości | istnieje „Development”, dodanie „development” | nic nie zapisane | komunikat o istniejącej nazwie, formularz otwarty |
| Inny User | User B dodaje „Development” | zapis się udaje; B nie widzi Typów pracy A | — |
| Walidacja | pusta nazwa, same spacje, > 200 znaków | brak zapisu, polski komunikat przy polu | — |
| Edycja | zmiana nazwy | lista pokazuje nową nazwę | duplikat jak przy dodaniu |
| Dezaktywacja / przywrócenie | aktywny / nieaktywny | Stan zmienia się | — |
| Usunięcie bez Wpisów | potwierdzone | znika z listy | Anuluj → nic |
| Usunięcie z Wpisami | Typ pracy w 1 / 3 Wpisach | nic nie usunięte | „…w 1 Wpisie…” / „…w 3 Wpisach…” |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Features/Projekty/ProjectStore.cs`, `ProjectForm.cs`, `ProjectFormValidator.cs`, `ProjectsPage.razor` -- wzorzec do powtórzenia 1:1 (wyniki `ProjectChange`, `ThenRender`, `_busy`, `novalidate`, komunikaty).
- `src/RejestrCzasu/Data/WorkType.cs` -- encja (`Name`, `IsActive`, `NameMaxLength` 200); `AppDbContext.WorkTypes`, `TimeEntries` (`WorkTypeId`).
- `src/RejestrCzasu/Data/DbErrors.cs` -- `WorkTypeNameIndex`, `IsUniqueViolation`, `IsForeignKeyViolation` (23503 i 23001).
- `src/RejestrCzasu/Program.cs` -- rejestracja `ProjectStore`; obok `WorkTypeStore`.
- `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/ProjektyHelp.razor` -- dopisać `typy-pracy`.
- `src/RejestrCzasu/wwwroot/app.css` -- style formularza Projektów; użyć tych samych klas albo dodać analogiczne.
- `README.md` sekcja „Wpis testowy” -- istniejące polecenie dla Projektu.
- `tests/RejestrCzasu.Tests/Features/ProjectStoreTests.cs`, `ProjectFormValidatorTests.cs`, `ProjectsPageTests.cs` -- wzorzec testów do powtórzenia.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Features/TypyPracy/WorkTypeStore.cs`, `WorkTypeForm.cs`, `WorkTypeFormValidator.cs`, `WorkTypesPage.razor` -- ekran i dane według wzorca -- R1–R3, R17.
- [x] `src/RejestrCzasu/Program.cs` -- rejestracja `WorkTypeStore` (scoped).
- [x] `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/TypyPracyHelp.razor` -- temat `typy-pracy` -- R26.
- [x] `README.md` -- polecenie Wpisu z istniejącym Typem pracy.
- [x] `tests/RejestrCzasu.Tests/Features/WorkTypeStoreTests.cs`, `WorkTypeFormValidatorTests.cs`, `WorkTypesPageTests.cs` -- cała macierz.

**Acceptance Criteria:**
- Given `dotnet run` i zalogowany User, when wybiera Słowniki → Typy pracy, then widzi ekran z siatką, filtrem i licznikiem, a `?` otwiera temat `typy-pracy`.

## Implementation Notes

- `WorkTypeChange`: `Done(Name)`, `DuplicateName(Name)`, `InUse(Name, EntryCount)`, `NotFound` — jak `ProjectChange`. Komunikat duplikatu podaje nazwę tak, jak ją wpisano (po `Trim()`).
- Style: klasy `work-types-add`, `work-type-form`, `work-type-form-title`, `work-type-form-buttons` dopisane do istniejących selektorów Projektów w `app.css` (bez nowych reguł).
- README: nagłówek sekcji zmieniony na „Wpis testowy (blokada usunięcia Projektu i Typu pracy)”; nowe polecenie sprawdzone na lokalnej bazie w `BEGIN … ROLLBACK` (`INSERT 0 1` dla istniejącej nazwy w innej wielkości liter, `INSERT 0 0` dla nieistniejącej).
- Weryfikacja przez `dotnet build -c Release` / `dotnet test -c Release`, bo uruchomiona lokalnie aplikacja blokowała `bin/Debug`.

## Plan Change Log

## Review Triage Log

Przebieg 1 (quick, 01.11.2026): low 4 (2 poprawione, 2 odrzucone), bez błędów działania.

| Uwaga | Werdykt | Ścieżka | Dowód / działanie |
|---|---|---|---|
| README: zdanie otwierające „Wpis testowy” mówi tylko o Projekcie | low | patch | Poprawione zdanie: Projekt lub Typ pracy. |
| Test edycji z duplikatem nie sprawdza wartości w polu | low | patch | Dodana asercja wartości „DEVELOPMENT”. |
| Ścieżka naruszenia klucza obcego przy usunięciu bez testu | low | odrzucona | Wyścig trudny do odtworzenia w teście; rozpoznanie błędu ma test w `DbErrorsTests` (jak w 1.7). |
| Apostrof w `<NAZWA>` psuje polecenie SQL z README | low | odrzucona | Ręczne polecenie deweloperskie, ten sam kształt co polecenie Projektu. |

## Verification

**Commands:**
- `dotnet build` -- expected: bez błędów i ostrzeżeń.
- `dotnet test` -- expected: wszystkie testy zielone.
