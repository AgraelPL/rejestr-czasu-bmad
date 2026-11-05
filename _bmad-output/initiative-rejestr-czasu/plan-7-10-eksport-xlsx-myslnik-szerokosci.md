---
title: 'Eksport Wpisów do xlsx, myślnik „-” w tekstach, szerokości pól formularza Wpisu'
type: 'feature'
ticket: ''
created: '2026-11-05'
status: 'built'
baseline_revision: '46c382ebeccd359ef7a3a18ad862d0bf4b9caee5'
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
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/plan-7-9-eksport-i-paginacja-wpisow.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Po teście 7.8/7.9: „Eksportuj” daje CSV zamiast pliku Excela; teksty aplikacji używają „—” (np. opcja Projektu „ORG — Organizacja i administracja”), a Autor chce pojedynczego „-”; w szerokim `EntryForm` wybrany Projekt jest ucięty, a Data i Czas mają zapas.

**Approach:** Eksport `EodReport` wyłącznie przez plik od ekranu (`ExportFile`, AD-23 poz. 13–14), CSV usunięty z kopii; `EntriesReport` podaje pisarza xlsx po stronie aplikacji (ClosedXML), zakres eksportu bez zmian. „—” → „-” we wszystkich tekstach widocznych dla Usera. Nowe proporcje kolumn siatki `EntryForm` z poprawną matematyką progów.

**Decyzje (Autor, 2026-11-05):**
- Plik `wpisy-RRRR-MM-DD.xlsx`; zakres jak w 7.9 (Nowy wpis: bieżący Miesiąc; Lista wpisów: filtry ekranu, filtry kolumn, szukanie, wszystkie strony); Data jako komórka daty `dd.mm.yyyy`, „Godziny” jako liczba (sumowalna), reszta tekst, wiersz nagłówka.
- ClosedXML 0.105.1 już teraz (Stack, AD-14), także dla eksportu list Wpisów (aktualizacja AD-1, AD-9).
- CSV usunięty z kopii `EodReport`: `ToCsv` i jego pomocnicy, `ReportColumn.ExportFormat`, `EodUI.downloadFile` (bez innych użytkowników w `src/` i `tests/`); `ExportLabel` zostaje (czyta go pisarz xlsx). Przycisk „Eksportuj” tylko przy `ShowExport` i podanym `ExportFile`; bez delegata przycisku nie ma.
- „-” zamiast „—”; Projekt (i Typ pracy) szersze, Data i Czas węższe, Opis szeroki.

## Boundaries & Constraints

**Always:** eod-ui bez zmian; każda zmiana w `Ui/`/`wwwroot/ui/` = pozycja AD-23 + linia `docs/ui/DOPASOWANIA.md` w tej samej zmianie; `Ui/` nie zależy od ClosedXML ani od kodu aplikacji poza `Domain/` (AD-1); eksport z `_filteredRows` i `Layout.Visible` (widoczne kolumny w kolejności ekranu, bez Akcje i Id); kolejność pól w DOM bez zmian (R16), bez `order`/`grid-area`; w każdym progu `@container` suma minimów kolumn + odstępy ≤ próg.

**Never:** endpoint dla eksportu list (filtry kolumn żyją w obwodzie; AD-14 dotyczy tylko Zestawienia); formuły w pliku; `AdjustToContents` (czcionki w obrazie Ubuntu); zmiana „–” w zakresach (np. „Pozycje 1–30 z 95”) i w komentarzach kodu; zmiany `EntryStore`, `EntryDialogs`, walidatorów poza tekstem `CzasMessage`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Eksportuj `/wpisy` | filtry ekranu + filtr kolumny + szukanie, 95 wierszy na 4 stronach | `wpisy-2026-11-05.xlsx`, arkusz „Wpisy”, nagłówek + 95 wierszy w kolejności ekranu; pasek „Wyeksportowano raport. wpisy-2026-11-05.xlsx: 95 wierszy, 6 kolumn.” | — |
| Eksportuj `/wpisy/nowy` | bieżący Miesiąc | jak wyżej, tylko Wpisy Miesiąca | — |
| Typy komórek | Data 2026-11-03, Czas 90 min | Data: komórka daty, format `dd.mm.yyyy`; „Godziny”: liczba 1,5 (format ogólny; 60 min → 1, nie „1.”); Projekt, Dzień, Typ pracy, Opis: tekst | — |
| Polskie litery | Opis „Zażółć gęślą jaźń”, Projekt „ŁÓD - Łódź” | bez zmian w Excelu | — |
| Tekst jak formuła/liczba | Opis „=SUMA(A1)”, „0012”, „1,5” | tekst dosłownie, bez formuły i konwersji | — |
| Ukryta/przestawiona kolumna | User ukrył „Dzień”, przesunął „Opis” | plik bez „Dzień”, kolejność ekranu | — |
| Pusta lista | 0 wierszy po filtrach | plik z samym nagłówkiem | — |
| Bez delegata | `EodReport` z `ShowExport` (domyślnie `true`) bez `ExportFile` | brak przycisku „Eksportuj” | — |
| Opcja Projektu | Projekt ORG | „ORG - Organizacja i administracja” w polu, filtrze Listy i pomocy | — |
| Formularz szeroki | karta ≥ 61rem (np. 1366 px) | jeden wiersz, Projekt ≥ 18rem pokazuje pełny tekst ORG; brak przewijania poziomego od 61rem | — |
| Formularz średni/wąski | okno `DialogSize.Medium`; 360 px | średni: Data stała, Projekt szerszy niż Typ pracy, Opis przez 2 kolumny; wąski: pole pod polem | komunikat walidacji Czasu zawija się w kolumnie |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Ui/Reports/EodReport.razor:51-53` (przycisk przy `ShowExport`), `EodReport.razor.cs:87-110,321-327` -- komentarze „CSV”, `ExportAsync`: `{FileName}-{DataAsOf:yyyy-MM-dd}.csv`, `EodUI.downloadFile(..., ToCsv(...))`, pasek z licznikami.
- `src/RejestrCzasu/Ui/Reports/ReportDefinition.cs:86-100,163` -- `ExportFormat` (do usunięcia), `ExportLabel`, `FileName` (komentarze „CSV”).
- `src/RejestrCzasu/Ui/Reports/ReportEngine.cs:19,317-358` -- `ToCsv`, `ExportValue`, `CsvValue` (jedyni użytkownicy: `EodReport.ExportAsync`, `EodReportTests`); pomocników wspólnych z resztą silnika (`ToDate`, `ToNumber`, `Formats`, `Culture`) nie usuwać.
- `src/RejestrCzasu/wwwroot/ui/eod.js:55-65` -- `downloadFile`: tylko tekst, zawsze BOM; jedyny wywołujący `EodReport`.
- `src/RejestrCzasu/SecurityHeaders.cs:7` -- CSP `default-src 'self'; connect-src 'self'`: link `blob:` z `download` działa jak dziś dla CSV, strumień idzie obwodem SignalR — bez zmian CSP.
- `src/RejestrCzasu/RejestrCzasu.csproj:21-38` -- brak ClosedXML.
- `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor:16-70` -- `EodReport`, kolumny Data/Czas z `ExportFormat`, Czas `ExportLabel = "Godziny"`, komentarze „CSV”.
- `src/RejestrCzasu/Domain/Czas.cs:73` -- `ToDecimalHours(int)`.
- Myślnik „—” w tekstach Usera (35 wystąpień): `Features/Pomoc/Topics/StartHelp.razor` (9: l. 3×2, 9–13, 18, 24), `WpisyHelp.razor` (10: l. 10, 15, 39, 42, 44, 72, 82, 89…), `WpisyNowyHelp.razor` (9: l. 9, 12×2, 14, 15, 16, 24, 41, 60), `ProjektyHelp.razor` (2: l. 3, 43), `TypyPracyHelp.razor` (2: l. 4, 43), `Features/Wpisy/EntriesPage.razor:75` (opcja filtra Projektu), `Shared/EntryForm.razor:81` `ProjectText` (+ komentarz l. 80), `Shared/EntryFormValidator.cs:16` `CzasMessage`. `Ui/`: brak „—”. Pominięte: `Accounts/AccountState.cs:43,59` (komentarze).
- Testy z „—”: `tests/.../Features/EntriesPageTests.cs:93,106,126,482,614,627`, `NewEntryPageTests.cs:372`, `Shared/EntryFormValidatorTests.cs:61`; eksport: `EntriesPageTests.cs:638-649`, `NewEntryPageTests.cs:167-168`, `Ui/EodReportTests.cs:44-82,218-219`.
- `src/RejestrCzasu/wwwroot/app.css:93-114` -- progi `@container entry-form` 480px i 60rem, kolumny szerokie `minmax(9rem,10rem) minmax(12rem,1fr) minmax(12rem,1fr) minmax(16rem,2fr) minmax(7rem,8rem)` (Projekt przy ~64rem zamrożony na 12rem).

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Ui/Reports/ReportFile.cs` (nowy), `EodReport.razor(.cs)` -- `public sealed record ReportFile(string Extension, string ContentType, byte[] Content)`; `[Parameter] Func<IReadOnlyList<ReportColumn>, IReadOnlyList<IReadOnlyDictionary<string, object?>>, ReportFile>? ExportFile`; przycisk tylko przy `ShowExport && ExportFile is not null`; `ExportAsync`: nazwa `{FileName}-{DataAsOf:yyyy-MM-dd}.{Extension}`, `EodUI.downloadStream(fileName, new DotNetStreamReference(new MemoryStream(Content)), ContentType)`, ten sam pasek; komentarze bez „CSV” -- AD-23 poz. 14.
- [x] `src/RejestrCzasu/Ui/Reports/ReportEngine.cs`, `ReportDefinition.cs` -- usunąć `ToCsv`, `ExportValue`, `CsvValue`, sekcję „Eksport CSV” i `ReportColumn.ExportFormat`; `ExportLabel` = nagłówek kolumny w pliku `ExportFile`; komentarze (`FileName`, nagłówek silnika) bez CSV -- AD-23 poz. 13, 14.
- [x] `src/RejestrCzasu/wwwroot/ui/eod.js` -- `downloadFile` → `async downloadStream(fileName, streamRef, type)`: `await streamRef.arrayBuffer()` → `Blob` bez BOM → ten sam link `download`; komentarz sekcji -- AD-23 poz. 14.
- [x] `src/RejestrCzasu/RejestrCzasu.csproj` -- `ClosedXML` `0.105.1`.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesXlsx.cs` (nowy) -- `internal static ReportFile Write(columns, rows)`: arkusz „Wpisy”, nagłówek `ExportLabel ?? Label` pogrubiony; komórka wg klucza kolumny: `Date` → `DateTime` z formatem `dd.mm.yyyy`, `Minutes` → `(double)Czas.ToDecimalHours` (format ogólny), pozostałe → tekst `Format?.Invoke(v) ?? v?.ToString() ?? ""` (nigdy `FormulaA1`); stałe szerokości kolumn; MIME `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, rozszerzenie `xlsx`.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor` -- `ExportFile="EntriesXlsx.Write"`; bez `ExportFormat` (zostaje `ExportLabel = "Godziny"`); komentarze „CSV” → „xlsx”.
- [x] `docs/ui/DOPASOWANIA.md`, `architecture-rejestr-czasu.md` AD-23 -- wszystkie zmiany eksportu w poz. 13–14 (nie w poz. 10): poz. 13 przepisana na `ExportLabel` (nagłówek w pliku `ExportFile`; `ExportFormat` usunięty); nowa poz. 14 (`ExportFile` + `ReportFile`, przycisk tylko z delegatem, usunięty CSV wzorca: `ToCsv` z pomocnikami i `EodUI.downloadFile` → `EodUI.downloadStream`; UI_SPEC 9.7 „Eksport CSV”); poz. 1 i 2 bez wzmianki o CSV; AD-9: `wpisy-RRRR-MM-DD.xlsx`, Data komórką daty, „Godziny” liczbą, pisarz ClosedXML w `Features/Wpisy`; AD-1 tabela: ClosedXML także w `Features/Wpisy`.
- [x] Pliki z Code Map (myślnik) -- „—” → „-” ze spacjami jak dotąd („ORG - Organizacja…”, „KOD - nazwa”, „1:30 - od 15m”); komentarz `EntryForm.razor:80` też.
- [x] `src/RejestrCzasu/Features/Pomoc/Topics/WpisyHelp.razor:87-90`, `WpisyNowyHelp.razor:75-77` -- „plik Excela (xlsx)”, `wpisy-2026-09-03.xlsx`, „Godziny” jako liczba do sumowania -- R26.
- [x] `src/RejestrCzasu/wwwroot/app.css` -- schemat w Design Notes; komentarze progów z nową sumą.
- [x] `tests/RejestrCzasu.Tests/Features/EntriesXlsxTests.cs` (nowy) -- skoroszyt odczytany ClosedXML: wiersze Matrix (nagłówek z „Godziny”, Data komórką daty z formatem, „Godziny” liczbą 1,5 i 1, polskie litery, „=SUMA(A1)” jako tekst, kolejność/ukryta kolumna, pusta lista).
- [x] `tests/RejestrCzasu.Tests/Ui/EodReportTests.cs` -- usunąć testy `ToCsv`; brak przycisku bez `ExportFile` i przy `ShowExport=false`; z `ExportFile`: `EodUI.downloadStream`, nazwa z rozszerzeniem pisarza, pisarz dostaje widoczne kolumny i wszystkie wiersze po filtrze ze wszystkich stron.
- [x] `tests/.../Features/EntriesPageTests.cs`, `NewEntryPageTests.cs`, `Shared/EntryFormValidatorTests.cs` -- `downloadStream`, `wpisy-2026-09-03.xlsx`; strumień z argumentu otwarty ClosedXML: nagłówek, 40 wierszy ze wszystkich stron po filtrze, Data datą, „Godziny” = 1,5 liczbą, polskie litery; teksty z „-”.

**Acceptance Criteria:**
- Given Lista wpisów z filtrem ekranu, filtrem kolumny i szukaniem, when Eksportuj i otwarcie w Excelu, then kolumna Data filtruje się jak daty, a zaznaczenie „Godziny” pokazuje Sumę na pasku stanu.
- Given `grep -rn "—" src/RejestrCzasu --include=*.razor --include=*.cs`, when przegląd, then trafienia tylko w komentarzach.
- Given `grep -rn "ToCsv\|ExportFormat\|downloadFile" src tests`, when przegląd, then brak trafień.
- Given `git diff`, when przegląd, then każda zmiana w `Ui/`/`wwwroot/ui/` ma pozycję AD-23 (13, 14) i linię w `DOPASOWANIA.md`, a `Ui/` nie odwołuje się do ClosedXML.

## Implementation Notes

- **`ExportAsync`:** `ExportFile(Layout.Visible, _filteredRows)`, nazwa `{FileName}-{DataAsOf:yyyy-MM-dd}.{Extension}`, `using var` na `DotNetStreamReference` (zwalnia `MemoryStream` po wywołaniu; JS czeka na `arrayBuffer()`, więc transfer kończy się przed zwolnieniem). Testy czytają bajty przez `MemoryStream.ToArray()`, które działa też po zwolnieniu (`XlsxDownload.Open` w `EntriesXlsxTests.cs`).
- **`EntriesXlsx`:** tekst przez `cell.Value = string` (ClosedXML 0.105: komórka tekstowa bez konwersji i bez formuły — test „=SUMA(A1)”, „0012”, „1,5”, „03.11.2026”); stałe szerokości kolumn wg klucza, bez `AdjustToContents`; „Godziny” jako `double` w formacie ogólnym.
- **AD-1 „tabela”:** zaktualizowany wiersz „Eksport” tabeli obszarów (Design Paradigm) — `Features/Wpisy/EntriesXlsx`; tabela Stack już miała ClosedXML 0.105.1.
- **Myślnik:** „—” → „-” w 8 plikach `src` i 3 plikach testów; zostały tylko komentarze `Accounts/AccountState.cs`.
- Weryfikacja: `dotnet build RejestrCzasu.sln` 0/0; `dotnet test` 588/588. Kontrole ręczne (Excel, DevTools 1366 px, okno Edycji, 360 px, CSP) niewykonane — dla Autora.

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `EntriesXlsx.SetCell` + `ExportAsync` | Opis > 32 767 znaków może wywrócić eksport | low | nierealna długość Opisu; poprawka to nowa obsługa błędu | reject |
| quick | AD-23 (2) | tekst mówi, że `Format` nie wpływa na plik, a pisarz go używa dla kolumn tekstowych | low | `SetCell` woła `column.Format?.Invoke` | patch |
| quick | `EodReport.razor.cs` `ExportAsync` | liczba kolumn w pasku z `_visibleColumns`, plik z `Layout.Visible` | low | dwa źródła tej samej listy | patch |

## Design Notes

Pisarz w aplikacji, nie w kopii: `Ui/` nie zależy od ClosedXML (AD-1), a typy komórek zna tylko ekran (Czas to `int` minut). `EodReport` dalej trzyma zakres eksportu, nazwę pliku i pasek. `DotNetStreamReference` zamiast `byte[]` w wywołaniu — wzorzec pobierania plików Blazora, bez limitu wielkości wiadomości dla długich list. `ShowExport` zostaje (poz. 1); Słowniki dalej podają `false`, choć bez `ExportFile` przycisku i tak nie ma.

Siatka `EntryForm` (odstęp kolumn 1rem):

| Próg | Kolumny | Minima + odstępy |
|---|---|---|
| szeroki `≥ 61rem` | Data `8.5rem`, Projekt `minmax(18rem, 3fr)`, Typ pracy `minmax(12rem, 2fr)`, Opis `minmax(12rem, 4fr)`, Czas `6.5rem` | 57 + 4 = 61rem |
| średni `≥ 480px` | `8.5rem minmax(0, 3fr) minmax(0, 2fr)`; Opis `span 2` (kol. 1–2), Czas w kol. 3 | 8.5 + 2 = 10.5rem < 30rem |
| wąski | 1 kolumna | — |

Przy karcie 64rem: Projekt 18rem, Typ 12rem, Opis 15rem; przy 100rem: Projekt ~27rem, Typ ~18rem, Opis ~36rem. Data 8.5rem mieści „dd.mm.rrrr”, ikonę kalendarza i nazwę dnia; Czas 6.5rem mieści „np. 1h 30m”. Szerokości sprawdzić w DevTools przy 1366 px i w oknie Edycji.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone (Docker dla Testcontainers)

**Manual checks (if no CLI):**
- Autor: plik z obu list otwarty w Excelu (daty, suma „Godziny”, polskie litery); formularz przy 1366 px (pełny „ORG - Organizacja i administracja”), w oknie Edycji i przy 360 px; konsola bez naruszeń CSP przy Eksportuj.
