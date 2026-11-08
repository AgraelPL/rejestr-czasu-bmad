---
title: 'Opis Wpisu jako obszar tekstowy (500 znaków, Ctrl+Enter), Opis w raportach w jednej linii, okna według treści'
type: 'feature'
ticket: ''
created: '2026-11-07'
status: 'built'
baseline_revision: '5e8de2f1a8afdde602738fe7882b21639bb6de14'
route: 'full'
route_source: 'auto'
risk: 'medium'
review: 'quick'
review_source: 'pinned'
lenses_ran: ['quick']
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/initiative-rejestr-czasu/plan-7-8-dopracowanie-wpisow-po-tescie.md'
  - '{project-root}/docs/ui/DOPASOWANIA.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Opis to jedna linia bez limitu; długi Opis rozpycha tabele raportów, a okna (Edycja/Kopia Wpisu, potwierdzenie usunięcia) zajmują prawie całą wysokość ekranu mimo krótkiej treści.

**Approach:** Opis jako `FluentTextArea` z limitem 500 znaków po trim i zapisem Ctrl+Enter; kolumna Opis w raportach w jednej linii z „…” i pełnym tekstem w `title` (nowe dopasowanie AD-23 poz. 17); wysokość i szerokość okien według treści po diagnozie w przeglądarce, poprawka w `app.css`.

**Decyzje (Autor, 2026-11-07):**
1. Opis Wpisu: najwyżej 500 znaków, liczone po trim, polski komunikat; kolumna bazy zostaje `text` (bez migracji). Pole wieloliniowe (`FluentTextArea`).
2. W Opisie Enter wstawia nową linię; zapis przyciskiem Zapisz albo Ctrl+Enter (z bieżącym tekstem pola). Sesja wpisywania samą klawiaturą zostaje (R16 ≤ 10 s), kolejność fokusu bez zmian, Enter w Czas i Data dalej zapisuje.
3. Kolumna Opis we wszystkich raportach Wpisów (Widok miesiąca, Lista wpisów, lista na Nowym wpisie): jedna linia ucięta „…”, pełny tekst w podpowiedzi po najechaniu; Opis wieloliniowy też w jednej linii. Opis bierze pozostałą szerokość, ale nie poszerza tabeli ponad kartę.
4. Każde okno ma wysokość i szerokość według treści, nigdy większe niż ekran (przewijanie wewnątrz okna).

## Boundaries & Constraints

**Always:** eod-ui bez zmian; zmiana w `Ui/`/`wwwroot/ui/` tylko jako nowa pozycja AD-23 + linia w `docs/ui/DOPASOWANIA.md`; JS tylko z biblioteki Fluent (bez własnego, CSP `script-src 'self'`, AD-12); jeden walidator Wpisu (AD-8), jeden `EntryForm` (AD-10); `Report` definicje `static readonly` (AD-9); w nowych tekstach UI zwykły myślnik „-”.

**Never:** migracja lub `HasMaxLength` dla opisu; atrybut `maxlength` na polu (przeglądarka ucinałaby wklejony tekst przed trim i User nie zobaczyłby komunikatu); `tabindex`, CSS `order`; zmiana `EntryStore` poza tym, co wynika z testów; szablon kolumny w ekranie zamiast parametru kopii.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| 500 znaków | Opis 500 znaków (+ spacje na końcach) | zapis, opis przycięty | - |
| 501 znaków | 501 znaków po trim | brak zapisu | „Opis może mieć najwyżej 500 znaków.” pod polem |
| Tylko spacje / nowe linie | `"  \n "` | brak zapisu | „Podaj opis.” |
| Enter w Opisie | kursor w Opisie, Enter | nowa linia, bez zapisu, fokus zostaje | - |
| Ctrl+Enter w Opisie | tekst wpisany, jeszcze bez `change` | zapis z bieżącym tekstem; Nowy wpis: czyszczenie i fokus w Projekt (R5) | błąd walidacji jak przy Zapisz |
| Enter w Czas / Data | jak dziś | zapis | - |
| Opis wieloliniowy w raporcie | `"a\nb"` | jedna linia „a b”, `title` z pełnym tekstem (z podziałem linii) | - |
| Długi Opis w raporcie | 500 znaków, ekran 1366 px | komórka z „…”, tabela nie szersza niż karta | wąski ekran: przewijanie poziome jak dotąd |
| Eksport Wpisów | Opis z `\n` | komórka tekstowa z zawijaniem, linie widoczne | - |
| Zestawienie | Opisy z `\n` w Wierszu zestawienia | dalej linie w jednej komórce z zawijaniem | - |
| Okno Wpisu | Edycja/Kopia, ekran 1366×768 | okno wysokie jak treść, wyśrodkowane | - |
| Potwierdzenie usunięcia | krótka treść / Opis 500 znaków | okno według treści, szerokość ograniczona, tekst się zawija | - |
| Okna Projektu i Typu pracy | dodaj/edytuj | według treści, bez regresji | - |
| Mały ekran | 360×640 lub wysokość 400 px | okno ≤ ekran, przewijanie wewnątrz | - |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Shared/EntryForm.razor` -- Opis to `FluentTextInput` z `ChangeAfterKeyPress="EnterKey"`; Czas też; komentarz nagłówka opisuje Enter.
- `src/RejestrCzasu/Shared/EntryFormValidator.cs` -- reguły Wpisu (wzór długości: `ProjectFormValidator` `.Trim().Length <= Project.CodeMaxLength`).
- `src/RejestrCzasu/Data/TimeEntry.cs`, `Data/AppDbContext.cs:121` -- `Description` jako `text`.
- Fluent 5.0.1 `FluentTextArea`: `MaxLength`, `AutoResize`, `Height`, `Resize` (`TextAreaResize.Vertical`), `ChangeAfterKeyPress`/`OnChangeAfterKeyPress` (bez `Rows`/`Immediate`). Skrypt biblioteki dopasowuje `KeyPress` na keyup ze ścisłymi modyfikatorami i wysyła `textarea.value` - `KeyPress.For(KeyCode.Enter).AndCtrlKey(true)` nie łapie zwykłego Enter.
- `src/RejestrCzasu/Ui/Reports/ReportDefinition.cs` (`ReportColumn`), `Ui/Reports/EodReport.razor` (th ok. l. 165, `RenderRow` ok. l. 444: `td` bez klasy i `title`) -- brak parametru kolumny; `eod.css:136` ma wzorcową `.eod-col-flex { max-width:0; width:42% }` nieużywaną przez kopię; Fluent `td` ma już `nowrap` + `ellipsis`.
- `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor:49`, `Features/Miesiac/MonthPage.razor:106` -- kolumna „Opis”.
- `src/RejestrCzasu/Features/Wpisy/EntriesXlsx.cs` (`SetCell`), `Features/Zestawienie/SummaryXlsx.cs:60` (już `WrapText`).
- `src/RejestrCzasu/wwwroot/app.css:43-49` -- `::part(dialog) { height:auto; width:auto }`. Hipoteza: UA dla modalnego `<dialog>` ma `position:fixed; inset:0; height/width: fit-content`; `auto` przy `inset:0` rozciąga okno na cały ekran (do `max-height:100dvh`, szerokość do 48rem). `fluent-dialog-body` ma `container: dialog-body / inline-size`, więc przy `fit-content` treść bez jawnej szerokości (potwierdzenie) by się zwęziła.
- `Ui/Messages/EodConfirmDialog.razor`, `Features/Wpisy/EntryDialog.razor`, `Features/Projekty/ProjectDialog.razor` -- okna.
- `Features/Pomoc/Topics/WpisyNowyHelp.razor:15,34`, `WpisyHelp.razor:49` -- teksty o Enter.
- `tests/.../Features/NewEntryPageTests.cs` (`EnterAsync`, `Description`, `TextInput`), `EntriesPageTests.cs:760`, `Shared/EntryFormValidatorTests.cs`, `Ui/EodReportTests.cs`, `Features/EntriesXlsxTests.cs`, `Features/HelpTests.cs`.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Data/TimeEntry.cs` -- `public const int DescriptionMaxLength = 500;` bez zmian mapowania -- wzór `Project.CodeMaxLength`, bez migracji.
- [x] `src/RejestrCzasu/Shared/EntryFormValidator.cs` -- `DescriptionLengthMessage = $"Opis może mieć najwyżej {TimeEntry.DescriptionMaxLength} znaków."`, reguła po trim -- decyzja 1, AD-8.
- [x] `src/RejestrCzasu/Shared/EntryForm.razor` -- Opis: `FluentTextArea` (ta sama klasa `entry-description`, `Resize="TextAreaResize.Vertical"`, ok. 2 linie, bez `MaxLength`), `ChangeAfterKeyPress` = Ctrl+Enter (`WithPreventDefault(true)`) → `OnEnterAsync`; Czas bez zmian; komentarz nagłówka -- decyzja 2, R16.
- [x] `src/RejestrCzasu/wwwroot/app.css` -- wysokość pola Opis w siatce `.entry-form` (pole przez kolumnę, bez rozpychania wiersza na Nowym wpisie); poprawka okien po diagnozie: `::part(dialog)` `height/width: fit-content`, `max-height: calc(100dvh - 2rem)`, `max-width` jak dziś, `overflow:auto`; jawna szerokość treści potwierdzenia (np. `fluent-dialog .fluent-message-box { width: min(28rem, calc(100vw - 5rem)) }`), jeśli kontener inline-size ją zwęża -- decyzja 4.
- [x] `src/RejestrCzasu/Ui/Reports/ReportDefinition.cs`, `Ui/Reports/EodReport.razor`, `wwwroot/ui/eod.css` -- `ReportColumn.FillWidth` (`bool`, domyślnie `false`): th i td z klasą `eod-col-fill` (`max-width:0; width:100%`), td z `title` = tekst `ReportEngine.Display`; doc-komentarz z odwołaniem AD-23 poz. 17 -- decyzja 3.
- [x] `docs/ui/DOPASOWANIA.md`, `_bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md` -- linia 17 i poz. 17 AD-23 (`FillWidth`, UI_SPEC 9.7); AD-10 „zapis klawiszem Enter (w Opisie Ctrl+Enter, Enter = nowa linia)” -- AD-23 wymaga wpisu.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesReport.razor`, `Features/Miesiac/MonthPage.razor` -- kolumna Opis `{ FillWidth = true }`.
- [x] `src/RejestrCzasu/Features/Wpisy/EntriesXlsx.cs` -- komórka Opisu z `WrapText = true` -- linie widoczne w Excelu.
- [x] `src/RejestrCzasu/Features/Pomoc/Topics/WpisyNowyHelp.razor`, `WpisyHelp.razor` -- Opis: do 500 znaków, Enter = nowa linia, Ctrl+Enter zapisuje; Enter w Data i Czas zapisuje.
- [x] Testy -- `EntryFormValidatorTests` (500/501 po trim, same białe znaki); `NewEntryPageTests`/`EntriesPageTests`: pomocnik pola `FluentTextArea`, Opis ma tylko Ctrl+Enter (Enter nie jest w `ChangeAfterKeyPress`), Ctrl+Enter zapisuje bieżący tekst (dziś test l. 282), Enter w Czas dalej zapisuje, kolejność fokusu; `EodReportTests`: klasa i `title` dla `FillWidth`, brak bez; `EntriesXlsxTests`: Opis z `\n` zawijany; `HelpTests` jeśli sprawdza teksty.

**Acceptance Criteria:**
- Given Nowy wpis, when Sesja 10 Wpisów samą klawiaturą (Tab, Ctrl+Enter w Opisie albo Enter w Czas), then każdy Wpis zapisuje się bez myszy w ≤ 10 s.
- Given otwarte okno Wpisu, potwierdzenia, Projektu lub Typu pracy, when mierzę `getBoundingClientRect()` elementu `dialog`, then wysokość ≈ wysokości treści (+ odstępy), ≤ `innerHeight`, szerokość ≤ `innerWidth`.
- Given `Ui/` po zmianie, then każda różnica względem eod-ui ma pozycję AD-23 i linię w `DOPASOWANIA.md`.

## Design Notes

Ctrl+Enter przez `ChangeAfterKeyPress` biblioteki daje to samo, co dziś Enter: wartość pola przychodzi razem z klawiszem, więc zapis nie widzi starej wartości; Ctrl zwolniony przed Enter nie zapisze (keyup) - do sprawdzenia ręcznie. `FillWidth` zamiast `.eod-col-flex` (42%): Opis ma brać resztę szerokości, a `max-width:0` sprawia, że długi tekst nie poszerza tabeli.

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- 0 ostrzeżeń, 0 błędów (aplikacja zatrzymana, bo blokuje `bin/`).
- `dotnet test` -- wszystko zielone.

**Manual checks:**
- Diagnoza okien (przed poprawką CSS i po niej): `docker compose up -d`, `dotnet run --project src/RejestrCzasu` (Administrator z `FirstAdmin`, dane z `docs/dev/seed-*.sql`); Chrome bez okna (`C:\Program Files\Google\Chrome\Application\chrome.exe --headless=new --remote-debugging-port=9222`) sterowany skryptem Node 26 z wbudowanym `WebSocket` (bez pakietów npm, skrypt w katalogu tymczasowym, nie w repo): logowanie, otwarcie Edycji Wpisu i Usuń, `Runtime.evaluate` z `getComputedStyle` łańcucha `fluent-dialog` → shadow `dialog` → `fluent-dialog-body` → treść (`height`, `max-height`, `inset`, `position`, atrybut `style`), przy 1366×768 i 360×640. Zapisać przyczynę w Implementation Notes. Na koniec zatrzymać aplikację.
- W przeglądarce: Enter w Opisie = nowa linia, Ctrl+Enter zapisuje; „…” i podpowiedź w trzech raportach; okienko filtra kolumny Opis z długimi wartościami; eksport otwarty w Excelu; konsola bez naruszeń CSP.

## Implementation Notes

- **Diagnoza okien (Chrome bez okna, CDP z Node 26, skrypty w katalogu tymczasowym, aplikacja z `--artifacts-path` na porcie 5081):** przed poprawką (stara reguła odtworzona w CSSOM) okno Edycji i potwierdzenie usunięcia miały przy 1366×768 po 768×768 px CSS (`position: fixed; inset: 0` modalnego `<dialog>` + `height/width: auto` = cały ekran do `max-height`, szerokość do 48rem) przy treści 191 px i 31 px - hipoteza potwierdzona. Sam `fit-content` dawał poprawną wysokość, ale szerokość 48 px: `fluent-dialog-body` jest kontenerem `inline-size`, więc jego treść nigdy nie daje mu szerokości (nie tylko potwierdzeniu). Poprawka: `fit-content` + `max-height: calc(100dvh - 2rem)` + `overflow: auto` na `::part(dialog)`, a szerokość treści jako `contain-intrinsic-inline-size` ciała okna: domyślnie `min(28rem, 100vw - 5rem)` (potwierdzenie, okno wyniku, Projekt, Typ pracy), `40.5rem` dla `EntryForm` (na wąskim ekranie `100vw - 5rem`); długi tekst potwierdzenia zawija `overflow-wrap: anywhere`. Po poprawce (1366×768 / 360×640 / 1366×400): Edycja i Kopia 696×321 CSS, Usuń 496×230 (z Opisem 500 znaków 496×370, tekst zawinięty), Projekt 496×280, Typ pracy 496×212, wszystkie wyśrodkowane; 360×640: szerokość 328 = ekran − 2rem; 360×400: okno Wpisu 368 px = ekran − 2rem, treść 543 px przewija się w oknie. Wszystkie ≤ `innerWidth`/`innerHeight`.
- **`FillWidth`:** `width: 100%` kolumny z planu nie działa - tabela ma `table-layout: auto` (komentarz `eod.css` o `fixed` jest nieaktualny), więc kolumna 100% rozciągała tabelę do 1 000 000 px (zmierzone w przeglądarce na Liście wpisów i Widoku miesiąca). Zamiast tego w tabeli z kolumną `eod-col-fill` pozostałe komórki mają `width: 1px` (zwężone do treści), a kolumna wypełniająca `max-width: 0`; `table-layout: fixed` też odpadło (te same 1 000 000 px). Po zmianie tabela = szerokość obszaru raportu (1081/1066 px przy 1366, 1589 px przy 1920), Opis bierze resztę (454-1173 px), Opis 500 znaków ucięty „…”, `title` z pełnym tekstem i `\n`, wieloliniowy Opis w jednej linii; 360 px: przewijanie poziome w obszarze tabeli jak dotąd, strona bez przewijania poziomego.
- **Klawiatura w przeglądarce:** Enter w Opisie wstawia nową linię (okno zostaje, fokus w polu), Ctrl+Enter zapisuje bieżący tekst (Kopia: okno zamknięte, pasek; Nowy wpis: pasek, Opis wyczyszczony, fokus w Projekt), Enter w Czas zapisuje, 501 znaków: komunikat pod polem, bez zapisu. `fluent-textarea` bez `maxlength`, `resize="vertical"`, ok. 2 linie (`--min-block-size` biblioteki), na całą szerokość kolumny. Konsola bez błędów i naruszeń CSP. Wpisy testowe `[test 3.10]` zapisane na Koncie z `FirstAdmin` i usunięte przez Usuń.
- **Weryfikacja:** `dotnet build` 0/0; `dotnet test` 707/716 - aplikacja Autora (`dotnet run`) blokowała `bin/`, więc build i testy w osobnym `--artifacts-path`; 9 czerwonych to te same testy szukające katalogu repozytorium względem `bin/` co w planie 7.8 (`ShellTests` ×2, `ApplicationTests` ×4, `DataIsolationGuardTests` ×3), do powtórzenia zwykłym `dotnet test` po zatrzymaniu aplikacji.
- **Nie sprawdzone:** eksport otwarty w Excelu (test ClosedXML: `WrapText` i linie w komórce); Ctrl zwolniony przed Enter (keyup) ręcznie; Sesja 10 Wpisów na czas. Okienko filtra kolumny Opis: długa wartość zawija się w pozycji listy (500 znaków = ok. 250 px wysokości, lista przewija się) - działa, ale jest ciężkie przy bardzo długich Opisach.

## Plan Change Log

- 2026-11-07: `eod-col-fill` bez `width: 100%` - pozostałe kolumny tabeli z taką kolumną `width: 1px` (powód w Implementation Notes); `DOPASOWANIA.md` poz. 17 opisuje faktyczne reguły.
- 2026-11-07: szerokość okien przez `contain-intrinsic-inline-size` ciała okna zamiast samej szerokości `.fluent-message-box` (kontener `inline-size` ciała zwężał każde okno, nie tylko potwierdzenie).
- 2026-11-07: zdanie o jednej linii z „…” i podpowiedzi także w temacie Pomocy Widoku miesiąca (`MiesiacHelp.razor`).

## Review Triage Log

| Lens | Location | Finding | Verdict | Evidence | Route |
|---|---|---|---|---|---|
| quick | `EntryForm.razor` `CtrlEnterKey` | puszczenie Ctrl przed Enter: brak nowej linii i brak zapisu | medium | skrypt Fluent 5.0.1: `preventDefault` na keydown, zapis na keyup z dokładnym `ctrlKey` | defer |
| quick | `eod.css` `.eod-col-fill` | przy średnich szerokościach Opis kurczy się do „…” | medium | `max-width: 0` bez `min-width`, reszta `width: 1px` | patch |
| quick | `eod.css` selektor `:has(.eod-col-fill)` | `width: 1px` na wszystkich komórkach tabeli | low | działa, bo tabela ma 100%; bez szkody dziś | reject |
| quick | `eod.css` ~135 | sprzeczne komentarze o `table-layout` | low | stary komentarz mówi `fixed`, nowy `auto` | patch |
| quick | `app.css` `fluent-dialog-body` | reguła bez zawężenia jak `::part(dialog)` | low | obejmuje też panele boczne | patch |
| quick | `app.css` dwie reguły okna Wpisu | jedna martwa | low | zależnie od rodzica formularza | patch |
| quick | `app.css` próg 44rem | okno Wpisu przewija się w bok przy 704–728 px | low | potrzebne 45.5rem | patch |
| quick | `EntryFormValidator` | limit liczony w jednostkach UTF-16 | low | jak `CodeMaxLength`; emoji rzadkie | reject |
| quick | `EntryFormValidatorTests` | asercja porównuje stałe; brak 501 bez nowej linii | low | nic nie testuje | patch |
| quick | `EntriesXlsx` | powtórzone wyrażenie wartości | low | dwa miejsca do zmiany | patch |
| quick | `WpisyNowyHelp` | „Zapisz robi to samo” czyta się jak Ctrl+Enter | low | kolejność zdań | patch |
