---
title: 'Dopasowanie komunikatów, szkieletu i EodHead w kopii wzorca'
type: 'feature'
ticket: '2'
created: '2026-11-02'
status: 'built'
baseline_revision: 'acbb28941fe0830b43224a46fb2d10df836bf38b'
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

**Problem:** Kopia wzorca w `Ui/` (historia 7.1) ma jeszcze zachowania sprzeczne z Rejestrem: potwierdzenie zatwierdza się Enterem z fokusem na przycisku głównym, belka i tytuł karty mówią „MOJE EOD”, `EodHead` ma skrypt inline łamiący CSP `script-src 'self'`, brak obejścia błędu comboboxa Fluent UI 5.0.1 w `eod.js`, a kopia niesie części EOD, których Rejestr nie użyje.

**Approach:** W kopii wprowadzić dopasowania AD-23 poz. 6–10 i opisać każde jedną linią w `docs/ui/DOPASOWANIA.md`. Ekrany nadal z kopii nie korzystają (historia 3).

## Boundaries & Constraints

**Always:** build bez ostrzeżeń; `ConfirmAsync` zachowuje sygnaturę wzorca (tytuł, treść HTML, przycisk główny, drugi przycisk, rodzaj) i wygląd okna wzorca (ikona 32 px w kolorze rodzaju, przycisk główny pierwszy); skrypt skali nadal ustawia `data-scale` przed pierwszym renderem (synchroniczny `<script src>` w `<head>`, ten sam klucz `eod.scale` i zbiór wartości co `eod.js`); każda zmiana względem wzorca ma linię w DOPASOWANIA.md.

**Never:** zmiany w eod-ui; podłączanie kopii w `Program.cs`, `App.razor`, `MainLayout` lub ekranach; usuwanie `Shared/EntryForm.razor.js`, `Shared/Confirm`, `ConfirmDialog`, `Notify` (historie 3 i 7); zmiana nazw publicznego API wzorca poza usunięciem części EOD (`EodShell.NewCase` zostaje — w Rejestrze to miejsce na Nowy wpis).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Enter w potwierdzeniu | okno `ConfirmAsync` otwarte, Enter | okno zostaje otwarte, wynik nieustalony | — |
| Fokus | okno `ConfirmAsync` otwarte | fokus na drugim przycisku („Anuluj”) | — |
| Przycisk główny | klik przycisku głównego | `true` | — |
| Anuluj / Escape | klik drugiego przycisku albo Escape | `false` | — |
| Treść HTML | `messageHtml` z `<p>`, `<strong>` | treść wyrenderowana jako HTML jak we wzorcu | ekran koduje wartości Usera (AD-11) |
| Okno wyniku | `ShowMessageBoxAsync` | bez zmian względem wzorca (jeden przycisk, Enter zamyka) | — |
| Nazwa aplikacji | `EodShell` bez `AppName`; `EodPage Title="Projekty"`; bez tytułu | belka „Rejestr czasu”; tytuł karty „Projekty · Rejestr czasu”; „Rejestr czasu” | — |
| EodHead | render | brak `<script>` bez `src`; `<script src="ui/eod-scale.js">` przed `eod.js` | — |
| Combobox | `EodUI.showSelectedText(dropdown, text)` | tekst pokazany, gdy pole tekstowe komponentu powstanie (do 30 klatek) | brak pola po 30 klatkach — nic |

</frozen-after-approval>

## Code Map

- `src/RejestrCzasu/Ui/Messages/UiMessages.cs` -- `ConfirmAsync` używa `dialogs.ShowMessageBoxAsync` z `PrimaryShortCut = "Enter"`; Fluent 5.0.1 `MessageBoxOptions` nie ma ustawienia fokusu (tylko Title, Message, Icon, IconColor, Primary/SecondaryButton, Primary/SecondaryShortCut), więc potwierdzenie wymaga własnego okna. `CreateOptions` i `ShowMessageBoxAsync` zostają dla okien wyniku.
- `src/RejestrCzasu/Shared/Confirm.cs` + `Shared/ConfirmDialog.razor` -- wzór rozwiązania w Rejestrze: `ShowDialogAsync<T>` z `Modal = true`, ukryta stopka biblioteki bez skrótów (`HideLibraryFooter`), własne przyciski w `FluentDialogBody.ActionTemplate`, `AutoFocus` na Anuluj, Escape = Cancel. Nie zmieniać — skopiować podejście do `Ui/Messages/`.
- `tests/RejestrCzasu.Tests/Shared/NotifyConfirmTests.cs:140-210` -- wzór testów bUnit okna (klawisze, Escape przez `Dialog.Id`, fokus).
- `src/RejestrCzasu/Ui/Layout/EodShell.razor` -- `AppName = "MOJE EOD"`; parametr `ContextSwitcher` i jego miejsce w belce (część EOD, AD-11).
- `src/RejestrCzasu/Ui/Page/EodPage.razor:14` -- `PageTitle` z „MOJE EOD”.
- `src/RejestrCzasu/Ui/Layout/EodHead.razor` -- `ScaleScript` jako `MarkupString` inline.
- `src/RejestrCzasu/Shared/EntryForm.razor.js` -- obejście comboboxa (`showSelectedText`) do przeniesienia jako `EodUI.showSelectedText`; EntryForm używa swojego modułu do historii 3.
- `src/RejestrCzasu/wwwroot/ui/eod.js` -- części EOD: `normalize`, `escapeHtml`, `mentionToken`, `caretPosition`, `show`/`hide`, `notifyInput`, `comments`, `observeMainSection`. Używane przez kopię: `getScale`, `setScale`, `describeWindow`, `getTheme`, `saveTheme`, `scrollToTop`, `downloadFile` (+ `scrollToElement`, `focusElement` zostają — ogólne).
- `src/RejestrCzasu/wwwroot/ui/eod.css` -- sekcje części EOD i galerii wzorca do usunięcia (nagłówki komentarzy w pliku): „Kafle z liczbami”, „Formularz sprawy” (z kamieniami milowymi, kartami, formularzem, polami), lista podpowiedzi „@”, FluentCalendar galerii, załączniki, oś czasu, matryca uprawnień, „Strony wzorca (galeria)”, „Schemat siatki”, „Komentarze do sprawy”, przełącznik sekcji formularza, karty formularza sprawy, panel historii, zdanie stanu pod tabelą formularza, formularz tylko do odczytu, nagłówek z awatarem 56 px. Reguł ogólnych (szkielet, karta, raport, okna, stany puste, chipy, paski filtrów, wąskie ekrany poza regułami usuwanych części) nie ruszać.

## Tasks & Acceptance

**Execution:**
- [x] `src/RejestrCzasu/Ui/Messages/EodConfirmDialog.razor` -- treść okna potwierdzenia: ikona i kolor jak `CreateOptions` dla rodzaju, treść jako `MarkupString`, przyciski: główny (Primary) pierwszy, drugi z `AutoFocus`, bez skrótów.
- [x] `src/RejestrCzasu/Ui/Messages/UiMessages.cs` -- `ConfirmAsync` przez `ShowDialogAsync<EodConfirmDialog>` (modalne, tytuł kodowany jak w `Shared/Confirm`, stopka biblioteki ukryta bez skrótów); remarks: Enter nie zatwierdza, fokus na drugim przycisku, Escape = drugi.
- [x] `src/RejestrCzasu/Ui/Layout/EodShell.razor` -- `AppName` domyślnie „Rejestr czasu”; usunąć `ContextSwitcher` (parametr, miejsce w belce i separator, przykład w komentarzu).
- [x] `src/RejestrCzasu/Ui/Page/EodPage.razor` -- tytuł karty z „Rejestr czasu”.
- [x] `src/RejestrCzasu/wwwroot/ui/eod-scale.js` + `Ui/Layout/EodHead.razor` -- skrypt skali z pliku, ładowany synchronicznie przed IMask/eod.js; usunąć `ScaleScript`.
- [x] `src/RejestrCzasu/wwwroot/ui/eod.js` -- dodać `showSelectedText(dropdown, text)` (logika z `EntryForm.razor.js`, komentarz o błędzie Fluent 5.0.1); usunąć części EOD z listy Code Map; komentarz nagłówka „Rejestr czasu (kopia wzorca eod-ui)”.
- [x] `src/RejestrCzasu/wwwroot/ui/eod.css` -- usunąć sekcje z listy Code Map i ich reguły w „Wąskie ekrany”.
- [x] `docs/ui/DOPASOWANIA.md` -- poz. 6–10 jako linie listy „Dopasowania AD-23” (co, dlaczego, UI_SPEC/AD); poz. 10 wymienia usunięte części `eod.js`, sekcje `eod.css` i parametr `ContextSwitcher`.
- [x] `tests/RejestrCzasu.Tests/Ui/UiMessagesTests.cs` -- bUnit: wiersze macierzy dla `ConfirmAsync` i `ShowMessageBoxAsync`.
- [x] `tests/RejestrCzasu.Tests/Ui/ShellTests.cs` -- bUnit: nazwa w belce, tytuł karty z `EodPage`, `EodHead` bez inline skryptu i z `ui/eod-scale.js`.

**Acceptance Criteria:**
- Given kopia po zmianie, when przeszukanie `src/RejestrCzasu/Ui` i `wwwroot/ui` pod „MOJE EOD”, `ContextSwitcher`, `comments`, `observeMainSection`, then brak trafień poza DOPASOWANIA.md.
- Given `eod.css` po zmianie, when przegląd klas używanych przez kopię (`grep -o 'eod-[a-z-]*'` w `Ui/`), then każda ma regułę, jeśli miała ją we wzorcu.
- Given aplikacja uruchomiona, when otwarte istniejące ekrany, then działają jak przed zmianą (kopia nieużywana).

## Implementation Notes

- `Ui/` nie zależy od `Shared/`, więc ukrywanie stopki biblioteki (`HideLibraryAction`) jest skopiowane do `UiMessages`, a nie wywołane z `Shared/Confirm`. Ikona i kolor rodzaju z nowej `UiMessages.KindIcon`, wspólnej z `CreateOptions`; znacznik ikony zgodny z oknem MessageBox biblioteki (`svg.icon`, `width: 32px`, ten sam `fill`).
- `eod.css`: z sekcji „Formularz sprawy” zostały `.eod-breadcrumb` i `.eod-formbar` (używane przez `EodBreadcrumb` i `EodReport`) oraz ogólne reguły pól Fluent (`.fluent-grid fluent-field`, `fluent-text[slot="message"]`, `.fluent-label-info` – ta ostatnia potrzebna paskowi układu raportu); z galerii `.eod-rule`/`.eod-rules` (`EodRule`, `EodCard`). Usunięte też osierocone `--eod-casebar-h`, `.eod-header-sep`, `.eod-ctx-label`, `.eod-person` i `.eod-tile`. Komentarz w `ServiceCollectionExtensions` nie wymienia już `.eod-fields`.

## Plan Change Log

## Review Triage Log

| # | Finding | Verdict | Route | Evidence |
|---|---------|---------|-------|----------|
| 1 | `eod.css` nadal ma reguły pominiętych komponentów (`.eod-placeholder*`, `.eod-edit-*`, `.eod-row-added/modified`, `.eod-due`, `.eod-list`, `.eod-roles`, `.eod-kind`) | low | defer | Reguły bez użytkowników w `Ui/`, istniały w kopii z 7.1; plan ich nie wymieniał; poz. 10 wylicza dokładnie usunięte. Do Refactor sweep 7.7. |
| 2 | Tytuł `ConfirmAsync` kodowany, `ShowMessageBoxAsync` surowy; dokumentacja parametru nie mówi, który kontrakt | low | patch | Kodowanie zgodne z planem i `Shared/Confirm`; brak opisu kontraktu w XML doc grozi podwójnym kodowaniem — poprawka w dokumentacji. |
| 3 | Wiersz macierzy „Combobox” pokryty tylko testem tekstu źródła | low | defer | Repo nie ma środowiska testów JS (obejście w `EntryForm.razor.js` też bez testu); logikę sprawdzono ręcznie w node. |
| 4 | Wygląd okna potwierdzenia poza ikoną i przyciskami niesprawdzony (szerokość, przycisk zamknięcia) | maybe-false | reject | Ten sam układ co `Shared/Confirm` w Rejestrze; rozstrzygnęłoby porównanie z `MessageBox` Fluent 5.0.1 w przeglądarce — najwyżej low. |
| 5 | Zmienione nagłówki sekcji w `eod.css` i komentarz `ServiceCollectionExtensions` bez linii w DOPASOWANIA.md | low | patch | Reguła planu: każda zmiana względem wzorca ma linię. |

## Verification

**Commands:**
- `dotnet build RejestrCzasu.sln` -- expected: 0 ostrzeżeń, 0 błędów
- `dotnet test --filter "FullyQualifiedName~RejestrCzasu.Tests.Ui"` -- expected: wszystkie zielone
- `dotnet test` -- expected: wszystkie zielone
