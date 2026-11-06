---
title: 'Polskie teksty Fluent UI z testem pokrycia'
type: 'feature'
ticket: '6'
created: '2026-11-05'
status: 'built'
baseline_revision: '2d171402414fee9958f6cb288711bbf0462c9caf'
route: 'full'
route_source: 'auto'
risk: 'low'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Po usunięciu `FluentLocalizer` (7.7) nic nie pilnuje, że każdy klucz Fluent UI ma polski tekst; `PolishLocalizer` nie ma 10 z 107 kluczy zasobu 5.0.1 (m.in. `FluentInputBase_RequiredMessage` - puste wymagane pole w oknie Projektu pokazuje „This field is required”), ma 2 klucze spoza zasobu, a 5 tekstów gubi `{0}` biblioteki.

**Approach:** Słownik `PolishLocalizer` dokładnie na zbiorze kluczy `LanguageResource.resources` zainstalowanego Fluent UI, z tymi samymi miejscami `{n}`; test w `PolishTextsTests` czyta zasób z dll biblioteki i porównuje zbiory kluczy i miejsc. Zmiana kopii jako AD-23 poz. 16 i linia 16 w `DOPASOWANIA.md`.

## Boundaries & Constraints

**Always:** teksty naturalną polszczyzną, zgodne z kopią (np. „Wczytywanie…”, „Zamknij”); w tekstach zwykły łącznik „-”, nigdy „—” ani „–”; zbiór indeksów `{n}` w tekście polskim = zbiór w tekście biblioteki; fallback `IFluentLocalizer.GetDefault` dla klucza spoza słownika zostaje.

**Never:** zmiany w eod-ui; pliki inne niż `Ui/Common/PolishLocalizer.cs`, `PolishTextsTests.cs`, `docs/ui/DOPASOWANIA.md`, AD-23 w architekturze; lista wyjątków w teście; klucze spoza zasobu; przeróbka tekstów, które już są poprawne.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Pokrycie kluczy | 107 kluczy zasobu 5.0.1 | każdy w słowniku, słownik bez nadmiarowych | test wypisuje brakujące i nadmiarowe |
| Miejsca `{n}` | `Paginator_Status`, `TimeAgo_DayAgo`… | te same indeksy; `["Paginator_Status", 2, 5]` → „Strona 2 z 5” | test wypisuje klucz z różnicą |
| Pole wymagane | `FluentTextInput Required` puste w `EditForm`, po walidacji | „To pole jest wymagane.”, brak „This field is required” | No error expected |
| Nieznany klucz | `"Brak_Takiego_Klucza"` | wynik równy `IFluentLocalizer.GetDefault(key)` | bez wyjątku |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Ui/Common/PolishLocalizer.cs` -- `IFluentLocalizer` kopii; prywatny słownik `Texts` (`Ordinal`), indeksator z `string.Format(Formats.Culture, …)`, fallback `GetDefault`; rejestruje `AddEodPlatformUI`
- dll pakietu Fluent UI 5.0.1 -- zasób `….Localization.LanguageResource.resources`, 107 kluczy
- `tests/RejestrCzasu.Tests/PolishTextsTests.cs` -- test rejestracji `PolishLocalizer`, Identity, `LocalUrl`; dawne `FluentLocalizer_*` w `git show d3773ee^:tests/RejestrCzasu.Tests/PolishTextsTests.cs`
- `src/RejestrCzasu/RejestrCzasu.csproj` -- `InternalsVisibleTo RejestrCzasu.Tests` (bez zmian)
- `docs/ui/DOPASOWANIA.md`, `_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md` (AD-23) -- lista dopasowań 1-15

## Tasks & Acceptance

**Execution:**
- [ ] `src/RejestrCzasu/Ui/Common/PolishLocalizer.cs` -- `Texts` jako `internal static readonly IReadOnlyDictionary<string, string>`; dopisać 10 kluczy, usunąć `TextInput_RequiredMessage` i `Calendar_RequiredMessage`, poprawić 5 `TimeAgo_*` i łącznik (Design Notes); w `<remarks>` dopisać, że test pokrycia pilnuje zgodności z zasobem -- AD-6, ticket
- [ ] `tests/RejestrCzasu.Tests/PolishTextsTests.cs` -- testy matrycy: zbiory kluczy z `ResourceReader` po `typeof(IFluentLocalizer).Assembly` (jak dawny test, plus kierunek „nadmiarowe”); indeksy `\{(\d+)` na klucz; brak „—”/„–” w tekstach; formatowanie i fallback; bUnit `EditForm` z `FluentTextInput Required` (usługi `AddEodPlatformUI`, `JSInterop.Mode = Loose`) -- matryca
- [ ] `architecture-rejestr-czasu.md` -- AD-23 poz. (16) `PolishLocalizer` z pełnym zestawem kluczy zasobu i testem pokrycia -- reguła AD-23
- [ ] `docs/ui/DOPASOWANIA.md` -- linia 16: co i dlaczego (AD-6, AD-23; UI_SPEC 18)

**Acceptance Criteria:**
- Given Fluent UI w wersji z nowym kluczem, when `dotnet test`, then test pokrycia jest czerwony i nazywa klucz.
- Given aplikacja po zmianie, when User otwiera okna i listy z komponentami Fluent, then żaden tekst biblioteki nie jest po angielsku.

## Implementation Notes

## Plan Change Log

- 2026-11-06: wiersz Matrix „Nieznany klucz” (zamrożony) zakłada wynik bez wyjątku; w Fluent UI 5.0.1 `IFluentLocalizer.GetDefault` rzuca `ArgumentException` dla klucza spoza zasobu, więc `PolishLocalizer` zachowuje się dokładnie jak biblioteka (test porównuje wyjątek). Ujawnione w DOPASOWANIA poz. 16 i komentarzu klasy; sekcji zamrożonej nie zmieniono.

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `PolishLocalizer` `PresenceStatus_Away` | „zaraz wracam” zamiast „nieobecny” | low | „away” w polskim Microsoft 365 to „nieobecny” | patch |
| quick | `PolishTextsTests` nieznany klucz | Matrix wymaga „bez wyjątku”, biblioteka rzuca | low | `GetDefault` w 5.0.1 rzuca dla klucza spoza zasobu; zapisane w Plan Change Log i DOPASOWANIA | patch |
| quick | `PolishLocalizer` komentarze | `<remarks>`/`<returns>` obiecują tekst domyślny | low | sprzeczne z zachowaniem 5.0.1 | patch |

## Design Notes

Nowe teksty: `FluentInputBase_RequiredMessage` „To pole jest wymagane.”; `PresenceStatus_Available` „dostępny”, `_Away` „zaraz wracam”, `_Blocked` „zablokowany”, `_Busy` „zajęty”, `_DoNotDisturb` „nie przeszkadzać”, `_Offline` „offline”, `_OutOfOffice` „poza biurem”, `_Unknown` „nieznany” (małe litery jak w bibliotece); `Fake_Hello` „Witaj, {0}”. Poprawki parytetu: `TimeAgo_MinuteAgo` „{0} minutę temu”, `_HourAgo` „{0} godzinę temu”, `_DayAgo` „{0} dzień temu”, `_MonthAgo` „{0} miesiąc temu”, `_YearAgo` „{0} rok temu”; `FluentInputFile_LoadingInProgress` „Wczytywanie {0}/{1} - {2}”. Kategorie 107 kluczy: DataGrid 40, TimeAgo 12, MessageBox 9, PresenceStatus 8, Paginator 8, FluentInputFile 4, po 3 Field/ErrorBoundary/Wizard, po 2 LayoutHamburger/LabelInfo/ColorPickerInput/FluentInputBase, 9 pojedynczych.

Założenie: `Fake_Hello` (klucz testowy biblioteki) dostaje tekst, żeby test nie miał wyjątków.
Założenie: parytet ścisły (równe zbiory indeksów), więc formy pojedyncze dostają `{0}`; `TimeAgo` nie występuje na ekranach.
Założenie: `TextInput_RequiredMessage` i `Calendar_RequiredMessage` (nie ma ich w 5.0.1) usuwamy - „bez nadmiarowych kluczy”.
Założenie: liczebniki (`{0} lat temu`, `{0} pozycji`) bez odmiany; `Paginator_Summary*` zasłania `SummaryTemplate` w `EodReport` (poz. 11).
Założenie: wyzwalacz komunikatu pola wymaganego w bUnit (`EditContext.Validate()` lub utrata fokusu) dobiera implementacja; asercja na tekst w markupie.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test` -- expected: wszystkie zielone, w tym nowe testy `PolishTextsTests`

**Manual checks (if no CLI):**
- `dotnet run`: okno Nowy Projekt, wyczyść Kod i wyjdź z pola - komunikat po polsku; paginacja list Wpisów i menu Fluent bez angielskich tekstów.
