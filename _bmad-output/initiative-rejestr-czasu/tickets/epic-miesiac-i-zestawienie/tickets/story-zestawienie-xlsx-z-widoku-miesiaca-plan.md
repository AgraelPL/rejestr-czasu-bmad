---
title: 'Zestawienie xlsx z Widoku miesiąca'
type: 'feature'
ticket: '3'
created: '2026-11-05'
status: 'built'
baseline_revision: '50a52faa4a16d3305ee98af17fdb146c1595b3cc'
route: 'full'
route_source: 'auto'
risk: 'high'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/export-layout.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Widok miesiąca nie daje pliku dla Odbiorcy, a menu i dokumenty wciąż mają ekran „Eksport”, którego nie będzie (decyzja 05.11.2026).

**Approach:** Endpoint `GET /zestawienie/{rok}/{miesiac}` (AD-14) ładuje Wpisy przez `MonthEntriesQuery`, buduje `MonthReport` i oddaje xlsx pisany przez ClosedXML co do komórki według `export-layout.md`; w Widoku miesiąca link „Pobierz Zestawienie” przez nowy `Shared/DownloadLink`; usunięcie Eksportu z menu, pomocy, AD-11/14/22 i PRD.

## Boundaries & Constraints

**Always:** wszystkie liczby (Godziny, Suma dnia, Suma tygodnia, RAZEM) wyłącznie z `MonthReport` (`SummaryRow.Minutes/DayMinutes/WeekMinutes`, `Minutes`) jako wartości liczbowe `Czas.ToDecimalHours`, nigdy formuły (AD-5); teksty jako komórki tekstowe; dane tylko przez `UserDbContextFactory` (AD-16); nazwa pliku przez `Results.File(..., fileDownloadName)`; link tylko `<a href download>` (router Blazora pomija kotwice z `download`), zakaz `NavigateTo` do endpointu.

**Never:** odczyt prefiksu z `account_settings` (historia 5 — tylko szew `prefix`); wybór Miesiąca w Widoku (historia 4); `EodReport.ExportFile` dla Zestawienia; formuły; `AdjustToContents` (obraz serwera bez czcionek); prawdziwe dane w repozytorium.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Wrzesień | zalogowany Aktywny User, zestaw `SeptemberShape` | 200, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `filename*=UTF-8''Zestawienie-wrze%C5%9Bie%C5%84_2026.xlsx`; arkusz „Wrzesień 2026”, dane w wierszach 2–29, 30 pusty, RAZEM w 31 | — |
| Cudze Wpisy | drugi User ma Wpisy we wrześniu | jego opisy, kody i godziny nie występują w pliku | — |
| Pusty Miesiąc (wywołany wprost) | brak Wpisów | 404, `text/plain; charset=utf-8`, „Brak Wpisów w Miesiącu Wrzesień 2026 - Zestawienia nie ma do pobrania.” (z treścią, więc bez strony „nie znaleziono”) | brak pliku |
| Zły Miesiąc | `/zestawienie/2026/13`, `/zestawienie/0/9`, `/zestawienie/abc/9` | 404 → strona „nie znaleziono” | bez wyjątku z `YearMonth` |
| Anonim | bez ciasteczka | 302 na `/konto/logowanie?ReturnUrl=%2Fzestawienie%2F2026%2F9` | — |
| Nieaktywne Konto | sesja, potem status Zablokowane | 302 na logowanie (walidator ciasteczka, AD-17) | — |
| Wymagana zmiana hasła | `must_change_password` | 302 na `/konto/zmiana-hasla` (bez `AllowDuringPasswordChange`) | — |
| Tekst jak formuła | opis „=SUMA(A1)”, kod „0012” | komórka tekstowa, `HasFormula == false` | — |

**Decyzja (Autor, 2026-11-05):** wiersz nagłówka pogrubiony jak w eksporcie list Wpisów, stałe szerokości kolumn dobrane w planie; formatowania oryginału nie odwzorowujemy.

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Reporting/MonthReport.cs` -- `Rows` (pierwszy wiersz dnia ma `DayMinutes`, tygodnia `WeekMinutes`), `Minutes`, `Month`.
- `src/RejestrCzasu/Reporting/MonthEntriesQuery.cs` -- scoped; w żądaniu HTTP `CurrentUser` czyta `HttpContext.User`.
- `src/RejestrCzasu/Features/Wpisy/EntriesXlsx.cs` -- wzór ClosedXML (`ContentType`, data `dd.mm.yyyy`, liczby `(double)Czas.ToDecimalHours`, stałe szerokości).
- `src/RejestrCzasu/Accounts/AccountEndpoints.cs`, `Program.cs` -- wzór `Map…Endpoints`, `Results.Text` z treścią omija `UseStatusCodePagesWithReExecute`; mapowanie po `MapAccountEndpoints`.
- `src/RejestrCzasu/Domain/YearMonth.cs`, `PolishCalendar.cs` -- `Name` („Wrzesień 2026”), `MonthName`.
- `src/RejestrCzasu/Features/Miesiac/MonthPage.razor` -- `_report`, `_month`; `EodPage.Actions` (wzór `ProjectsPage.razor:13`).
- `src/RejestrCzasu/Shared/MainLayout.razor:57-58` -- pozycja „Eksport”.
- `src/RejestrCzasu/Features/Pomoc/Topics/StartHelp.razor:21`, `MiesiacHelp.razor` -- pomoc.
- `tests/RejestrCzasu.Tests/AppClient.cs`, `AccountsTests.cs`, `Reporting/SeptemberShape.cs`, `Reporting/MonthEntriesQueryTests.cs` (seed), `Shared/LayoutTests.cs:57`, `Features/MonthPageTests.cs`.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Features/Zestawienie/SummaryXlsx.cs` -- `static byte[] Write(MonthReport)` według Design Notes -- układ Odbiorcy (R4).
- [x] `src/RejestrCzasu/Features/Zestawienie/SummaryFile.cs` -- `Name(YearMonth, string? prefix)`: `Zestawienie-{MonthName.ToLowerInvariant()}_{RRRR}.xlsx`, niepusty prefiks → `{prefix}_…` -- R5, szew dla historii 5.
- [x] `src/RejestrCzasu/Features/Zestawienie/SummaryEndpoint.cs` -- `MapSummaryEndpoint()`: `MapGet("/zestawienie/{rok:int}/{miesiac:int}")`, `RequireAuthorization()`, zakres poza 1–9999 / 1–12 → `Results.NotFound()`, pusty → 404 tekst z matrycy, inaczej `Results.File(bytes, ContentType, SummaryFile.Name(month, prefix: null))`; `static string Path(YearMonth)` = `/zestawienie/{Year}/{Month}` -- AD-14.
- [x] `src/RejestrCzasu/Program.cs` -- `app.MapSummaryEndpoint();` po `MapAccountEndpoints`.
- [x] `src/RejestrCzasu/Shared/DownloadLink.razor` + `wwwroot/app.css` -- `<a href=@Href download class="app-download-link @Class">` z ikoną ArrowDownload i `ChildContent`, styl przycisku -- AD-14.
- [x] `src/RejestrCzasu/Features/Miesiac/MonthPage.razor` -- `<Actions>`: przy `Entries.Count > 0` `DownloadLink` „Pobierz Zestawienie” na `SummaryEndpoint.Path(_month)`, przy pustym tekst „Brak Wpisów - nie ma Zestawienia do pobrania.”, przy wczytywaniu nic; treść w `<ChildContent>`; poprawić komentarz nagłówka.
- [x] `src/RejestrCzasu/Shared/MainLayout.razor` -- usunąć `FluentNavItem` „Eksport” -- NFR-6.
- [x] `src/RejestrCzasu/Features/Pomoc/Topics/MiesiacHelp.razor` -- sekcja „Pobierz Zestawienie”: przycisk, nazwa `Zestawienie-wrzesień_2026.xlsx`, układ w skrócie, sumy równe Widokowi, pusty Miesiąc bez przycisku; `StartHelp.razor` -- Rejestr bez Eksportu, Zestawienie z Widoku miesiąca.
- [x] `_bmad-output/.../architecture-rejestr-czasu.md` -- tabela l. 36 „Eksport” → „Zestawienie”; AD-11 menu Rejestr bez Eksportu; AD-14 bez „Ekran Eksport (wybór Miesiąca) i”; AD-22 bez klucza `eksport`; Trasy bez `/eksport`. `prd-rejestr-czasu.md` -- §4.5 Opis bez zdania o ekranie Eksport; NFR-6 bez Eksportu.
- [x] `tests/RejestrCzasu.Tests/Features/SummaryXlsxTests.cs`, `SummaryFileTests.cs` -- odczyt ClosedXML na `SeptemberShape` + przypadki tekstu-formuły i nazw (wrzesień, październik, luty; prefiks „ABC”, pusty, `null`).
- [x] `tests/RejestrCzasu.Tests/SummaryEndpointTests.cs` -- `AppClient` + `PostgresFixture`: wiersze matrycy od „Wrzesień” do „Wymagana zmiana hasła”.
- [x] `tests/.../Features/MonthPageTests.cs` -- `a[download]` z `href="/zestawienie/2026/9"` i tekstem; pusty Miesiąc bez linku, z tekstem; `tests/.../Shared/LayoutTests.cs` -- bez `/eksport`, asercja braku.

**Acceptance Criteria:**
- Given skoroszyt z `SummaryXlsx.Write` dla zestawu wrześniowego, when odczytany ClosedXML, then: nagłówki A1–H1 „Dzień tygodnia”, „Data”, „Kod”, „Typ”, „Opis”, „Godziny”, „Suma dnia”, „Suma tygodnia”; zamrożony wiersz 1; H w pierwszych wierszach tygodni = 30/16/18/25/14, puste gdzie indziej; G i A/B tylko w pierwszym wierszu dnia, B komórka daty z formatem `dd.mm.yyyy`; E wiersza 02.09 `AAA`/Development = „Zadanie 4\nZadanie 9” z zawijaniem; F31 = H31 = 103; E31 „RAZEM WRZESIEŃ 2026”, wiersz 31 pogrubiony; żadna komórka nie ma formuły; D zawiera „Środowisko”.
- Given Widok miesiąca z Wpisami, when User klika „Pobierz Zestawienie”, then przeglądarka pobiera plik bez przejścia routera Blazora i bez zmiany strony.
- Given zalogowany User, then menu nie ma „Eksport”, a pomoc `start` i `miesiac` opisują Zestawienie tylko z Widoku miesiąca.

## Implementation Notes

- Matryca „Wrzesień”: `wrze%C5%9Bie%C5%84` to literówka (dekoduje się do „wrześień”); poprawny nagłówek to `filename*=UTF-8''Zestawienie-wrzesie%C5%84_2026.xlsx` i ten sprawdza `SummaryEndpointTests` (plus `FileNameStar == "Zestawienie-wrzesień_2026.xlsx"`).
- Seed `SeptemberShape.SeedAsync(postgres, userId, ct)` wspólny dla `MonthPageTests` i `SummaryEndpointTests`.
- Weryfikacja w `-c Release`: wyjście Debug blokował uruchomiony lokalnie `RejestrCzasu.exe`. Build 0 ostrzeżeń, 0 błędów; `dotnet test` 635/635.

## Plan Change Log

- 2026-11-05: w Matrix (zamrożone) oczekiwany nagłówek ma literówkę `wrze%C5%9Bie%C5%84` (= „wrześień”); poprawny i testowany to `filename*=UTF-8''Zestawienie-wrzesie%C5%84_2026.xlsx` („wrzesień”). Sekcji zamrożonej nie zmieniono — do poprawy przez Autora, jeśli chce.

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `MonthPage.razor:19,23` | klasy `month-summary` i `month-summary-empty` nie istnieją w CSS | low | brak reguł w `app.css`; poprawione: tekst pustego Miesiąca z `eod-caption` wzorca, zbędna klasa usunięta | patch |
| quick | `HelpTests` | brak testu treści pomocy `start`/`miesiac` (Eksport, Pobierz Zestawienie) | low | treść poprawna; to brak testu, nie wada | reject |

## Design Notes

Pisarz (arkusz `report.Month.Name`, `SheetView.FreezeRows(1)`); wiersz `r = i + 2` dla `Rows[i]`:

```text
A  DayMinutes != null ? PolishCalendar.DayName(Date) : puste
B  DayMinutes != null ? Date (DateTime, format "dd.mm.yyyy") : puste
C  ProjectCode (tekst)     D  WorkTypeName (tekst)
E  string.Join("\n", Descriptions), WrapText = true
F  ToDecimalHours(Minutes)   G  DayMinutes → godziny   H  WeekMinutes → godziny (format ogólny)
RAZEM: wiersz Rows.Count + 3 (jeden pusty), Bold; E "RAZEM " + Month.Name.ToUpperInvariant(); F i H = ToDecimalHours(report.Minutes)
```

A/B/G w tym samym warunku: `DayMinutes` jest tylko w pierwszym wierszu dnia (historia 1). F/H RAZEM z `report.Minutes` — pisarz nic nie sumuje; test porównuje je z sumą kolumn F i H. Szerokości stałe (propozycja: A 16, B 12, C 10, D 18, E 60, F 10, G 11, H 14); nagłówek pogrubiony jak w `EntriesXlsx` (decyzja Autora 2026-11-05). `ContentType` wspólny z `EntriesXlsx.ContentType`. Wyścig (Wpisy usunięte w innej karcie po wczytaniu Widoku) daje 404 z tekstem — przeglądarka pokaże nieudane pobranie; akceptowalne, historia 4 i tak przeładowuje dane.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów.
- `dotnet test` -- expected: wszystkie zielone, w tym `SummaryXlsxTests`, `SummaryFileTests`, `SummaryEndpointTests`, `MonthPageTests`, `LayoutTests`, `HelpTests`.

**Manual checks (if no CLI):**
- Lokalnie zestaw testowy: „Pobierz Zestawienie” zapisuje `Zestawienie-<miesiąc>_<RRRR>.xlsx`, Excel otwiera go bez widoku naprawy, sumy równe Widokowi; menu bez Eksportu.
