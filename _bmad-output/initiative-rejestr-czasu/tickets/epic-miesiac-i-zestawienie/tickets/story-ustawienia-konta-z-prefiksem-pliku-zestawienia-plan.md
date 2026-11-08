---
title: 'Ustawienia konta z prefiksem pliku Zestawienia'
type: 'feature'
ticket: '5'
created: '2026-11-05'
status: 'built'
baseline_revision: '3fcd5b10566f0aa278d38ada3afd44aab6750ed8'
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

**Problem:** Link „Ustawienia konta” w belce prowadzi do „nie znaleziono”, a Zestawienie zawsze ma nazwę bez prefiksu (endpoint podaje `prefix: null`).

**Approach:** Ekran `/ustawienia-konta` na `EodPage` z jednym polem „Prefiks nazwy pliku Zestawienia” (walidator Blazilla, AD-8), podglądem nazwy pliku i linkiem „Zmień hasło”; `AccountSettingsStore` czyta i zapisuje `account_settings` w kontekście Usera, tworząc wiersz przy pierwszym zapisie (AD-7); endpoint Zestawienia bierze z niego prefiks; temat pomocy `ustawienia-konta`.

## Boundaries & Constraints

**Always:** dane wyłącznie przez `UserDbContextFactory` (AD-16), `user_id` nadaje `AppDbContext`; prefiks po `Trim()`; reguły tylko w walidatorze, ekran nie zapisuje modelu bez walidacji; brak wiersza = pusty prefiks; komunikaty przez `UiMessages` (sukces paskiem, błędy walidacji pod polem); nazwa pliku wyłącznie przez `SummaryFile.Name`.

**Never:** zmiana `MonthPage.razor` i `EntryDialogs.cs` (historia 4); zmiana hasła na tym ekranie (tylko link, AD-15); nowa migracja (tabela i `ExportPrefixMaxLength` już są); tworzenie wiersza przy rejestracji lub odczycie; zmiany w `src/RejestrCzasu/Ui`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Pierwszy zapis | brak wiersza, „ABC” | wiersz z `user_id` Usera i „ABC”; pasek „Zapisano Ustawienia konta.” | — |
| Kolejny zapis | wiersz „ABC”, „XYZ” | ten sam wiersz, „XYZ” (nadal jeden wiersz) | — |
| Wyczyszczenie | wiersz „ABC”, „” lub „   ” | `ExportPrefix = ""`; plik `Zestawienie-wrzesień_2026.xlsx` | — |
| Spacje na końcach | „  ABC  ” | zapis „ABC”, plik `ABC_Zestawienie-…` | — |
| Polskie litery | „Łódź-Zespół” | zapis bez zmian, plik `Łódź-Zespół_Zestawienie-wrzesień_2026.xlsx` | — |
| Niedozwolony znak | „A/B” (każdy z `\ / : * ? " < > \|`) | nic nie zapisane | pod polem „Prefiks nie może zawierać znaków \ / : * ? " < > \|.” |
| 101 znaków | 101 × „a” (po obcięciu) | nic nie zapisane | pod polem „Prefiks może mieć najwyżej 100 znaków.” |
| 100 znaków + spacje | 100 × „a” + „  ” | zapis 100 znaków | — |
| Bez wiersza | User nigdy nie zapisał | pole puste; plik bez prefiksu | — |
| Oddzielenie | User B ma „BBB”, User A brak wiersza | plik Usera A bez prefiksu; ekran A pusty | — |
| Plik z prefiksem | „ABC”, wrzesień 2026 z Wpisami | `FileNameStar == "ABC_Zestawienie-wrzesień_2026.xlsx"` | — |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Data/AccountSettings.cs` -- `UserId` (PK, `ValueGeneratedNever`), `ExportPrefix` (niepusty, domyślnie `""`), `ExportPrefixMaxLength = 100`.
- `src/RejestrCzasu/Data/AppDbContext.cs:92-98`, `UserDbContextFactory.cs`, `DbErrors.cs` -- mapowanie, filtr `IUserOwned`, `IsUniqueViolation`.
- `src/RejestrCzasu/Features/Zestawienie/SummaryEndpoint.cs`, `SummaryFile.cs` -- szew `prefix` (pusty/`null` = bez prefiksu).
- `src/RejestrCzasu/Features/Projekty/ProjectStore.cs`, `ProjectFormValidator.cs`, `ProjectForm.cs`, `ProjectDialog.razor` (EditForm + `FluentValidator`, `novalidate`), `ProjectsPage.razor` (`EodPage`, `PageHelp`, `ShowSuccessAsync`) -- wzorce.
- `src/RejestrCzasu/Features/ScreenServiceCollectionExtensions.cs` -- rejestracja scoped.
- `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs`, `Topics/MiesiacHelp.razor` -- pomoc.
- `src/RejestrCzasu/Shared/MainLayout.razor:24` -- link już istnieje; `Accounts/ChangePassword.razor` -- `/konto/zmiana-hasla`.
- `tests/RejestrCzasu.Tests/Features/ProjectStoreTests.cs`, `ProjectFormValidatorTests.cs`, `ProjectsPageTests.cs`, `SummaryEndpointTests.cs`, `PostgresFixture.cs` -- wzorce testów.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Features/UstawieniaKonta/AccountSettingsStore.cs` -- `Task<string> GetPrefixAsync(ct)` (brak wiersza → `""`, `AsNoTracking`); `Task SavePrefixAsync(string? prefix, ct)`: `Trim()`, brak wiersza → `Add(new AccountSettings { ExportPrefix = … })`, jest → aktualizacja; puste → `""` -- AD-7, AD-16.
- [x] `src/RejestrCzasu/Features/UstawieniaKonta/AccountSettingsForm.cs`, `AccountSettingsFormValidator.cs` -- `Prefix`; reguły na wartości po `Trim()`: długość ≤ `ExportPrefixMaxLength`, brak znaków z `ForbiddenChars = "\\/:*?\"<>|"`; pusty dozwolony -- AD-8.
- [x] `src/RejestrCzasu/Features/UstawieniaKonta/AccountSettingsPage.razor` -- `@page "/ustawienia-konta"`; `EodPage` „Ustawienia konta” + `PageHelp`; `EodCard` „Zestawienie”: `EditForm novalidate` + `FluentValidator`, `FluentTextInput` „Prefiks nazwy pliku Zestawienia” (bez `maxlength`), podgląd „Nazwa pliku: …” z `SummaryFile.Name(bieżący Miesiąc z TimeProvider, wpisana wartość po Trim)` odświeżany przy wpisywaniu, przycisk „Zapisz” (blokada ponownego kliknięcia), po zapisie pole = wartość po obcięciu i `ShowSuccessAsync("Zapisano Ustawienia konta.")`; `EodCard` „Hasło”: `FluentAnchorButton` „Zmień hasło” → `/konto/zmiana-hasla` -- R7.
- [x] `src/RejestrCzasu/Features/ScreenServiceCollectionExtensions.cs` -- `AddScoped<AccountSettingsStore>()` + komentarz.
- [x] `src/RejestrCzasu/Features/Zestawienie/SummaryEndpoint.cs` -- wstrzyknąć `AccountSettingsStore`, `GetPrefixAsync` tylko gdy są Wiersze, przekazać do `SummaryFile.Name`; `SummaryFile.cs` -- poprawić komentarz o historii 5 -- R5, AD-14.
- [x] `src/RejestrCzasu/Features/Pomoc/HelpTopics.cs` + `Topics/UstawieniaKontaHelp.razor` -- stała `UstawieniaKonta = "ustawienia-konta"`, temat „Ustawienia konta” (`LoggedIn`): prefiks, reguły, przykład `ABC_Zestawienie-wrzesień_2026.xlsx`, wyczyszczenie, link do zmiany hasła; `Topics/MiesiacHelp.razor` -- w sekcji „Pobierz Zestawienie” zdanie o prefiksie z Ustawień konta -- R8, AD-22.
- [x] `tests/RejestrCzasu.Tests/Features/AccountSettingsStoreTests.cs` -- PostgreSQL: pierwszy zapis tworzy jeden wiersz z `user_id`, drugi aktualizuje (liczba wierszy 1), puste → `""`, trim, polskie litery, brak wiersza → `""`, dwóch Userów rozdzielonych.
- [x] `tests/RejestrCzasu.Tests/Features/AccountSettingsFormValidatorTests.cs` -- każdy znak zabroniony (Theory), 100/101, 100 + spacje, pusty, polskie litery.
- [x] `tests/RejestrCzasu.Tests/Features/AccountSettingsPageTests.cs` -- bUnit: pole z zapisanym prefiksem, podgląd, zapis + pasek, błąd pod polem bez zapisu, link `href="/konto/zmiana-hasla"`, `PageHelp` z kluczem `ustawienia-konta`.
- [x] `tests/RejestrCzasu.Tests/SummaryEndpointTests.cs` -- prefiks „ABC” → `ABC_Zestawienie-wrzesień_2026.xlsx`; User B z „BBB”, User A bez wiersza → nazwa bez prefiksu.

**Acceptance Criteria:**
- Given zalogowany User, when klika „Ustawienia konta” w belce, then widzi ekran z polem, podglądem nazwy dla bieżącego Miesiąca i linkiem „Zmień hasło”, a `?` otwiera temat `ustawienia-konta`.
- Given zapisany prefiks „ABC”, when User pobiera Zestawienie z Widoku miesiąca, then plik nazywa się `ABC_Zestawienie-<miesiąc>_<RRRR>.xlsx`; po wyczyszczeniu i zapisie — bez prefiksu.
- Given ekran po ponownym otwarciu, then pole pokazuje ostatnio zapisaną wartość (po obcięciu spacji).

## Implementation Notes

## Plan Change Log

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `DbErrors.AccountSettingsKey` | stała nazwy klucza bez testu na prawdziwym błędzie PostgreSQL | low | `DbErrorsTests` sprawdza każdą inną stałą; rozjazd nazwy wyłączyłby ponowną próbę | patch |
| quick | `SummaryEndpointTests` | „Wyczyszczenie” i „Polskie litery” nie sprawdzone na endpoincie | low | sprawdzone tylko w Store i podglądzie | patch |
| quick | `AccountSettingsPageTests` | położenie komunikatu walidacji nie sprawdzone | low | tekst szukany w całym markupie; komunikat i tak z `FluentValidator` pod polem | reject |

## Design Notes

- Założenie: wyczyszczenie zapisuje `ExportPrefix = ""` w istniejącym wierszu (kolumna niepusta), wiersza nie usuwa; wyczyszczenie bez wiersza też go tworzy — skutek identyczny (pusty prefiks).
- Założenie: wyścig pierwszego zapisu w dwóch kartach (naruszenie klucza `account_settings`, `DbErrors.IsUniqueViolation` z nazwą ograniczenia z `Initial`) — jedna ponowna próba jako aktualizacja.
- Założenie: podgląd liczy się z wpisanej wartości po obcięciu, także niepoprawnej; błąd pokazuje walidator pod polem. Bieżący Miesiąc z `PolishCalendar.Today(TimeProvider)`.
- Założenie: pole bez `maxlength`, żeby 101. znak dał komunikat walidatora, a nie cichą blokadę; reguły walidatora na wartości po `Trim()`, jak w `ProjectFormValidator`.
- Założenie: lista zakazanych znaków dokładnie jak w decyzji epiku (bez dodatkowych reguł np. dla kropki czy znaków sterujących).
- Założenie: `/konto/zmiana-hasla` jest statyczna (`[ExcludeFromInteractiveRouting]`); link musi dać pełne przeładowanie — sprawdzić ręcznie; gdy router obwodu go przechwyci, użyć `NavigateTo(..., forceLoad: true)`.
- Teksty komunikatów jak w Matrix; pasek sukcesu znika po 7 s (AD-23).

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów.
- `dotnet test` -- expected: wszystkie zielone, w tym `AccountSettingsStoreTests`, `AccountSettingsFormValidatorTests`, `AccountSettingsPageTests`, `SummaryEndpointTests`, `HelpTests`, `LayoutTests`.

**Manual checks (if no CLI):**
- Lokalnie: prefiks „ABC” → pobrany `ABC_Zestawienie-wrzesień_2026.xlsx`; wyczyszczenie → bez prefiksu; „A/B” → komunikat pod polem; „Zmień hasło” otwiera `/konto/zmiana-hasla`.
