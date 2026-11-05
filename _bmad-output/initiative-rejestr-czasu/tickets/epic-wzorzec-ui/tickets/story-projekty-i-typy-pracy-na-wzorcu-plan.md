---
title: 'Projekty i Typy pracy na wzorcu'
type: 'feature'
ticket: '4'
created: '2026-11-03'
status: 'built'
baseline_revision: '9eafd137716179d117b17a47e35a7863d373e461'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
  - '{project-root}/docs/ui/DOPASOWANIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Ekrany `/projekty` i `/typy-pracy` stoją jeszcze na `PageHeader`, `Shared/ReportGrid` i formularzu nad listą, a nie na komponentach wzorca.

**Approach:** Oba ekrany na `EodPage` (Dodaj w nagłówku) z `EodCard` i `EodReport` według AD-9; dodawanie i edycja w oknie jak UI_SPEC 9.13 A; usunięcie z `UiMessages.ConfirmAsync`. Zachowanie R1–R3 bez zmian, każda zmiana zapisuje się od razu. Strony pomocy `projekty` i `typy-pracy` opisują nowy wygląd.

## Boundaries & Constraints

**Always:** definicja raportu `static readonly` na ekranie (`Key`/`LayoutKey` = klucz tematu, `ShowExport="false"`, `PreserveRowOrder`, kolumny `List` — filtr wartości, licznik w stopce); wiersze z jednej statycznej metody `ToRow` typu wiersza, z `Id` poza kolumnami, używanym tylko przez `RowActions`; akcje wiersza jako ikony Subtle (Edytuj, Dezaktywuj/Przywróć wg stanu, Usuń); dane tylko przez `ProjectStore`/`WorkTypeStore` (AD-4, AD-16); po każdej zmianie lista wczytana ponownie; walidacja tylko `ProjectFormValidator`/`WorkTypeFormValidator` (AD-8); wartości Usera w oknach kodowane.

**Never:** zmiany w `Ui/` (brak parametru = nowa pozycja AD-23, wtedy HALT); `EodEditableTable` i zapis zbiorczy; zmiany store'ów, walidatorów, schematu; usuwanie `ReportGrid`, `PageHeader`, `Confirm` (7.7); ekrany Wpisów (7.5).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Lista | Projekty A (aktywny), B (nieaktywny) | `EodReport` z kolumnami Kod, Nazwa, Stan w kolejności store'u, stopka „2 pozycje”, brak „Eksport CSV” | — |
| Filtr | filtr wartości Stan = Nieaktywny | widać B, stopka z licznikiem po filtrach | — |
| Dodaj | „Dodaj Projekt” w nagłówku → okno, poprawne pola, Zapisz | okno zamknięte, wiersz na liście, pasek sukcesu na stronie | — |
| Walidacja | puste pola, Zapisz | komunikaty pod polami, okno otwarte | — |
| Duplikat | kod już istnieje | błąd w treści okna, wpisane wartości zostają | — |
| Edytuj | ikona Edytuj → okno z wartościami, Zapisz | zapis, pasek „Zapisano …” | wiersz usunięty w międzyczasie: okno zamknięte, ostrzeżenie, lista ponownie |
| Dezaktywuj / Przywróć | ikona wg stanu | zapis od razu, Stan zmieniony, pasek sukcesu | — |
| Usuń | ikona Usuń → okno potwierdzenia | Enter nie potwierdza; Usuń → wiersz znika | użyty we Wpisach: ostrzeżenie R3 z liczbą Wpisów, wiersz zostaje |
| Anuluj / Escape | w oknie edycji | okno zamknięte bez zapisu | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Features/Projekty/ProjectsPage.razor`, `Features/TypyPracy/WorkTypesPage.razor` -- obecne ekrany (te same kroki; Typy pracy bez Kodu, z Nazwą). Zachować komunikaty (`InUseMessage`, `DuplicateMessage`, `NotFoundMessage`, teksty sukcesu), `_busy`, kolejność „najpierw `ReloadAsync`, potem pasek”. Usunąć formularz w treści.
- `ProjectStore.cs` / `WorkTypeStore.cs` -- `ProjectRow(Id, Code, Name, IsActive)` + `State`; `ProjectChange.{Done, DuplicateCode, InUse, NotFound}`; `ListAsync` daje kolejność ekranu. Dodać `static ToRow` (np. w `ProjectRow` lub na ekranie — jedna metoda) zwracające słownik `Id`, `Code`, `Name`, `State`.
- `ProjectForm.cs`, `ProjectFormValidator.cs` (+ odpowiedniki TypyPracy) -- model i walidator okna bez zmian.
- `Ui/Reports/EodReport.razor(.cs)`, `ReportDefinition.cs` -- `Definition` (wymagane `Key`, `Title`, `Description`, `FileName`, `Columns`), `Rows` (`IReadOnlyList<IReadOnlyDictionary<string, object?>>`), `RowActions` (`RenderFragment<row>`), `ShowExport`, `PreserveRowOrder`, `LayoutKey`. Kolumna Kod/Nazwa `IsKey` dla pierwszej. Wzór użycia: `tests/RejestrCzasu.Tests/Ui/EodReportTests.cs`.
- `Features/Wpisy/EntryDialog.razor` + `EntryDialogs.cs` (`ShowAsync`) -- wzór okna: `ShowDialogAsync<T>` modalne, tytuł `HtmlEncode`, `Confirm.HideLibraryFooter`, okno zapisuje przez store samo, błąd zapisu w treści okna (`FluentMessageBar` `role="alert"`), zamyka się po Done/NotFound; Anuluj/Escape = Cancel. Nowe okna: `Features/Projekty/ProjectDialog.razor`, `Features/TypyPracy/WorkTypeDialog.razor` (EditForm + `FluentValidator` Blazilla, `novalidate`, Kod z `AutoFocus`, Zapisz Primary pierwszy, Anuluj); wynik okna (`Done`/`NotFound`) mówi ekranowi, który pasek pokazać po zamknięciu (pasek strony leży pod modalem, AD-10).
- `Ui/Page/EodPage.razor`, `EodCard` -- `EodPage Fill="true"` z `Actions` (Dodaj Primary, `Add`), `PageHelp Topic`, `EodCard NoPadding Fill` z raportem.
- `Features/Pomoc/Topics/{ProjektyHelp,TypyPracyHelp}.razor` -- treść według nowego wyglądu (Dodaj w nagłówku, okno, ikony akcji, filtr w kolumnach, licznik).
- `wwwroot/app.css` -- reguły `.projects-add`, `.project-form*`, `.work-types-add`, `.work-type-form*` do usunięcia lub przeniesienia na okno.
- `tests/RejestrCzasu.Tests/Features/{ProjectsPage,WorkTypesPage}Tests.cs` -- bUnit na Testcontainers; przepisać selektory na `EodReport` i okno, zachować pokrycie dotychczasowych zachowań.

## Tasks & Acceptance

**Execution:**
- [x] `Features/Projekty/ProjectDialog.razor` -- okno dodania i edycji (tytuł „Dodaj Projekt” / „Edytuj Projekt {kod}”).
- [x] `Features/Projekty/ProjectsPage.razor` (+ `ToRow`) -- `EodPage` + `EodReport` + `RowActions` + otwieranie okna i usunięcie.
- [x] `Features/TypyPracy/WorkTypeDialog.razor`, `WorkTypesPage.razor` (+ `ToRow`) -- to samo dla Typów pracy.
- [x] `Features/Pomoc/Topics/ProjektyHelp.razor`, `TypyPracyHelp.razor` -- treść zgodna z ekranem.
- [x] `wwwroot/app.css` -- sprzątanie reguł starego formularza.
- [x] Testy -- przepisać `ProjectsPageTests`, `WorkTypesPageTests` i pokryć wiersze macierzy.

**Acceptance Criteria:**
- Given aplikacja po zmianie, when `grep` w `Features/Projekty` i `Features/TypyPracy` po `ReportGrid`, `PageHeader`, `Notify`, then brak trafień.
- Given `dotnet run`, when dodanie, edycja, dezaktywacja, przywrócenie i usunięcie Projektu i Typu pracy, then działają jak przed zmianą, okna mają fokus w pierwszym polu, konsola bez błędów CSP.

## Implementation Notes

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `ProjectDialog.razor`, `WorkTypeDialog.razor` `SaveAsync` | Komunikat duplikatu zostaje po nieudanej walidacji | low | `_error` czyszczony tylko w `OnValidSubmit`; przy błędzie walidacji alert ze starą wartością zostaje obok błędów pól | patch |

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)
