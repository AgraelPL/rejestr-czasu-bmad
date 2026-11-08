---
title: 'Kopia wzorca eod-ui w Rejestrze z dopasowaniem EodReport'
type: 'feature'
ticket: '1'
created: '2026-11-02'
status: done
baseline_revision: '69c244a5c6b393ef45d47be7d7b0cc4175409fbd'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: [quick]
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Rejestr nie ma jeszcze komponentów wzorca eod-ui, a `EodReport` wzorca nie spełnia Rejestru: zawsze ma eksport CSV, sortuje kolumnę Czasu jak tekst, przestawia wiersze po kolumnie kluczowej, sam liczy sumy z `Definition.Total` i pozwala Userowi zmienić każdy układ.

**Approach:** Skopiować potrzebne komponenty `Eod.Platform.UI` z eod-ui @ `8e66aea` (klon `C:\Users\kamil\Documents\eod-ui`, tylko do czytania) do `src/RejestrCzasu/Ui/` (przestrzeń `RejestrCzasu.Ui`, nazwy i API jak we wzorcu), zasoby do `wwwroot/ui/`, migawkę UI_SPEC do `docs/ui/`, i w kopii wprowadzić dopasowania AD-23 poz. 1–5; każdą zmianę względem wzorca opisać linią w `docs/ui/DOPASOWANIA.md`. Ekrany jeszcze z kopii nie korzystają.

## Boundaries & Constraints

**Always:** kopia kompiluje się w `RejestrCzasu.csproj` bez ostrzeżeń (w tym RS0030 — `DateTime.Today` jest zakazany, AD-6); domyślne wartości nowych parametrów zachowują zachowanie wzorca (`ShowExport = true`, pozostałe wyłączone); każda zmiana względem wzorca (także przestrzeń nazw, ścieżki zasobów, pominięte pliki) ma linię w DOPASOWANIA.md z odwołaniem do sekcji UI_SPEC lub AD.

**Never:** zmiany w repozytorium eod-ui; rejestracja `AddEodPlatformUI`/`UseEodPolishCulture` w `Program.cs`, `EodHead` w `App.razor` lub `@using RejestrCzasu.Ui` w głównym `_Imports.razor` (historia 3); dopasowania AD-23 poz. 6–10 (historia 2); usuwanie `Shared/ReportGrid`, `Notify`, `Confirm`, `Localization/FluentLocalizer` (historia 7); części EOD i Syncfusion.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eksport ukryty | `ShowExport="false"` | brak przycisku „Eksport CSV” | — |
| Eksport domyślny | parametr pominięty | przycisk „Eksport CSV” jak we wzorcu | — |
| Format i sortowanie | kolumna `int` minut 120, 30, 45 z `Format = Czas.Format`, sort rosnąco | komórki „2h”, „30m”, „45m” (format Czasu „1h 30m”, glossary); kolejność 30, 45, 120 | — |
| Format w filtrze | filtr listy tej kolumny | pola wyboru z tekstem `Format`, filtr działa po wartości | — |
| Kolejność wejścia | `PreserveRowOrder`, wiersze C, A, B, bez sortowania | C, A, B; z grupowaniem — wewnątrz grup kolejność wejścia | — |
| Bez PreserveRowOrder | jak wyżej, parametr pominięty | porządek wzorca (po kolumnie kluczowej) | — |
| Sumy od ekranu | `GroupSummary`, `FooterSummary` | wiersz grupy: licznik + tekst z `GroupSummary(poziom, surowa wartość klucza)`; stopka: licznik + `FooterSummary` (+ „po filtrach, z N”) | `Definition.Total` pomijany, gdy podano parametr |
| Układ zablokowany | `LockedLayout` | brak sortowania nagłówkiem, przeciągania kolumn, zmiany grupowania i kolejności; filtr kolumny działa | — |

</frozen-after-approval>

## Code Map

- `C:\Users\kamil\Documents\eod-ui\src\Eod.Platform.UI\` -- źródło kopii (jedna przestrzeń `Eod.Platform.UI`; `@namespace` w każdym .razor). Tylko czytać.
- Kopiowane (zależności sprawdzone grepem): `_Imports.razor`, `ServiceCollectionExtensions.cs`, `Appearance/AppearanceSettings.cs`, `Common/{Formats,PolishLocalizer,Statuses}.cs`, `Layout/{EodHead,EodShell}.razor`, `Messages/{EodMessageBar.razor,UiMessages.cs}`, `Page/{BreadcrumbItem.cs,EodBreadcrumb,EodCard,EodEmptyState,EodIcon,EodPage,EodRule}.razor`, `Reports/{EodReport.razor,EodReport.razor.cs,ReportDefinition.cs,ReportEngine.cs,ReportLayout.cs}`, `wwwroot/{eod.css,eod.js,lib/imask/*,lib/sortablejs/*}`.
- Pomijane: `Attachments/`, `Comments/`, `EditableTable/` (AD-11), `Forms/`, `History/`, `Permissions/`, `Layout/{EodContextSwitcher.razor,ContextOption.cs}`, `Messages/EodUnsavedChangesGuard.razor`, `Common/{HtmlSanitizer,ReadOnlyScope,FieldConditions}.cs`, `Page/{EodDueDate,EodFact,EodFacts,EodList,EodListItem,EodPerson,EodPlaceholder,EodReadOnlyField,EodTile,EodTiles}.razor`, `Page/PlaceholderKind.cs`. `EodShell` ma tylko parametr `RenderFragment ContextSwitcher` — bez zależności od pominiętego typu (zostaje do historii 2).
- `EodHead.razor` -- ścieżki `_content/Eod.Platform.UI/...` → `ui/...`; skrypt skali inline zostaje (poz. 8 to historia 2).
- `EodReport.razor.cs:92` -- `DataAsOf = DateOnly.FromDateTime(DateTime.Today)` łamie RS0030; domyślnie `default`, ekran podaje datę z `PolishCalendar.Today` gdy potrzebuje.
- `EodReport.razor.cs` -- prywatna właściwość `FooterSummary` koliduje z nowym parametrem; zmienić jej nazwę.
- `ReportEngine.Rows` -- `List.Sort` niestabilny, remis rozstrzyga kolumna kluczowa; `Groups` grupuje po tekście `Display`; `Summary` liczy `Total`; `Compare` dla List/Text bez `ValueOrder` porównuje tekst.
- `src/RejestrCzasu/_Imports.razor` -- główne importy z `RejestrCzasu.Shared` (ma własne `ReportColumn<T>`, `ReportGroup`) i `[Authorize]`; w plikach `RejestrCzasu.Ui` typy własnej przestrzeni wygrywają, więc nie zmieniać. Testy kopii nie importują `RejestrCzasu.Shared`.
- `src/RejestrCzasu/Domain/Czas.cs:55` -- `Czas.Format(int)` do testu formatu.
- `tests/RejestrCzasu.Tests/Shared/ReportGridTests.cs` -- wzór testu bUnit (`BunitContext`, `JSInterop.Mode = Loose`).
- `C:\Users\kamil\Documents\eod-ui\docs\UI_SPEC.md` (156 KB) -- migawka bez zmian.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Ui/**` -- skopiować pliki z listy Code Map, zachowując podkatalogi; `namespace`/`@namespace` → `RejestrCzasu.Ui`, `@using Eod.Platform.UI` → `RejestrCzasu.Ui`; reszta treści bez zmian poza zadaniami poniżej.
- [x] `src/RejestrCzasu/wwwroot/ui/**` -- skopiować `eod.css`, `eod.js`, `lib/imask/*`, `lib/sortablejs/*` (z LICENSE); `EodHead` wskazuje `ui/...`.
- [x] `src/RejestrCzasu/Ui/Reports/ReportDefinition.cs` -- `ReportColumn.Format` (`Func<object?, string>?`): tekst do komórki, filtra i grupy.
- [x] `src/RejestrCzasu/Ui/Reports/ReportEngine.cs` -- `Display` używa `Format`; `Compare` dla kolumny z `Format` porównuje surowe wartości tego samego typu `IComparable`; `Rows(..., preserveRowOrder)` — stabilny porządek, remis rozstrzyga indeks wejścia zamiast kolumny kluczowej; `ReportGroup` dostaje surową wartość klucza (pierwszy wiersz grupy).
- [x] `src/RejestrCzasu/Ui/Reports/EodReport.razor(.cs)` -- parametry `ShowExport` (domyślnie `true`), `PreserveRowOrder`, `GroupSummary` (`Func<int, object?, string>?`), `FooterSummary` (`string?`), `LockedLayout`; `DataAsOf` bez `DateTime.Today`. Przy `LockedLayout`: nagłówek bez przycisku sortowania i `draggable`, żetony grupowania i sortowania bez przycisków zmiany, bez „Dodaj poziom”, w okienku kolumny tylko filtr; zwijanie grup działa.
- [x] `docs/ui/UI_SPEC.md` -- migawka z eod-ui @ `8e66aea` bez zmian.
- [x] `docs/ui/DOPASOWANIA.md` -- nagłówek (źródło, commit), linia na każdą zmianę: przestrzeń nazw, ścieżki zasobów, pominięte pliki, `DataAsOf`, poz. 1–5 AD-23; miejsca na poz. 6–10 nie rezerwować.
- [x] `tests/RejestrCzasu.Tests/Ui/EodReportTests.cs` -- bUnit dla wierszy macierzy I/O (usługi przez `AddEodPlatformUI()` z kopii, JS Loose).
- [x] `tests/RejestrCzasu.Tests/Ui/ReportEngineTests.cs` -- xUnit: `Compare`/`Display` z `Format`, `Rows` z `preserveRowOrder` (z grupowaniem i z sortowaniem — remis wg wejścia).

**Acceptance Criteria:**
- Given gałąź po zmianie, when `git -C C:\Users\kamil\Documents\eod-ui status --porcelain` i `rev-parse HEAD`, then pusto i `8e66aea…`.
- Given kopia w `Ui/`, when przegląd DOPASOWANIA.md wobec `diff` kopii z wzorcem (po zamianie przestrzeni nazw), then każda różnica ma linię.
- Given aplikacja uruchomiona, when otwarte istniejące ekrany, then wyglądają i działają jak przed zmianą (kopia nieużywana).

## Implementation Notes

## Plan Change Log

## Review Triage Log

**Przebieg 1 (quick):** high 0 · medium 0 · low 6 · false 2 · maybe-false 0.

| # | Znalezisko | Werdykt | Trasa | Dowód / działanie |
|---|-----------|---------|-------|-------------------|
| 1 | `Compare` z `Format` pomija `ValueOrder` | low | patch | gałąź Format wraca przed `case List when ValueOrder`; poprawka: gałąź tylko bez `ValueOrder` |
| 2 | `Compare` z `Format` dla `string` w kulturze bieżącej | low | patch | `string.CompareTo` używa CurrentCulture; poprawka: string idzie ścieżką z `Formats.Culture` |
| 3 | żeton filtra z `Format` gubi wartości spoza `Rows` | low | reject | wartości filtra pochodzą z bieżących `Rows`, układ żyje do odświeżenia; poprawka wymaga nowej gałęzi, a sytuacja rzadka |
| 4 | zdublowane etykiety filtra przy nieróżnowartościowym `Format` | false | reject | jedyny `Format` w Rejestrze to `Czas.Format`, różnowartościowy dla minut ≥ 0 |
| 5 | LF w `eod.css` bez linii w DOPASOWANIA | low | patch | linia o końcach wierszy obejmuje tylko .cs/.razor; uzupełnić |
| 6 | `DataAsOf` domyślnie 0001-01-01 w nazwie pliku | false | reject | zmiana opisana w DOPASOWANIA, Rejestr zawsze `ShowExport=false` (AD-9), więc pliku nie ma |
| 7 | `aria-label` na `<span>` bez roli w zablokowanym żetonie sortowania | low | patch | rola generic nie ma nazwy dostępnej; poprawka `role="img"` |
| 8 | dziedziczone `[Authorize]` i usingi z głównego `_Imports.razor` | low | patch | brak użycia typów Shared/Accounts w `Ui/`, atrybut bez skutku dla nie-stron; dopisać linię w DOPASOWANIA |

## Design Notes

Podsumowanie grupy i stopki zawsze zaczyna się od licznika pozycji (R17); tekst ekranu dochodzi po „ · ”:

```csharp
var text = PolishPlural.WithCount(rows.Count, "pozycja", "pozycje", "pozycji");
text += GroupSummary is null ? ReportEngine.Summary(...)-tail : $" · {GroupSummary(g.Level, g.Key)}";
```

Sortowanie z `Format`: `int` minut porównuje się liczbowo także w kolumnie `List` (filtr wartości), więc Czas może mieć filtr wartości i poprawne sortowanie naraz.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test --filter "FullyQualifiedName~RejestrCzasu.Tests.Ui"` -- expected: wszystkie zielone
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)
