---
title: 'Refactor sweep'
type: 'refactor'
ticket: '4'
created: '2026-11-02'
status: done
baseline_revision: '3e750ac59f22d5978bbecde9617e8637b96f7b9c'
route: 'oneshot'
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

**Problem:** Historie 2.1–2.3 nie zostawiły odłożonych uwag w `deferred-work.md` (jedyna — wybór opcji w filtrach `/wpisy` — zamknięta ręczną kontrolą), ale w kodzie zostały powtórzenia: komunikaty Wpisu w `NewEntryPage` używane przez `EntryDialog`, format daty `dd.MM.yyyy` w dwóch miejscach, `HideLibraryAction` w `Confirm` i `EntryDialogs`, `FixedTimeProvider` w trzech plikach testów.

**Approach:** Wyłącznie sprzątanie bez zmiany zachowania: (1) `Features/Wpisy/EntryMessages` — jedno miejsce tekstów Wpisu (`Saved`, `Deleted`, `DeleteQuestion`, `MissingDictionaryItem`, `NotFound`) i formatu daty; `NewEntryPage`, `EntryDialog`, `EntryDialogs`, `EntryRow.DateText` z niego korzystają; (2) `Shared/Confirm` udostępnia `internal static HideLibraryFooter(DialogOptions)` używane też przez `EntryDialogs`; (3) jeden `tests/.../Shared/FixedTimeProvider.cs` zamiast trzech kopii. Teksty komunikatów bez zmian; wygląd bez zmian (zastąpi go wzorzec eod-ui po epiku 2).

</frozen-after-approval>

## Implementation Notes

- Nowy `Features/Wpisy/EntryMessages.cs` (internal static): `MissingDictionaryItem`, `NotFound`, `Date`, `Saved`, `DeleteQuestion`, `Deleted`; usunięte kopie z `NewEntryPage.razor` i `EntryDialogs.cs`; `EntryRow.DateText` przez `EntryMessages.Date`.
- `Shared/Confirm.HideLibraryFooter(DialogOptions)` (internal static) zamiast prywatnej kopii w `EntryDialogs`.
- `tests/RejestrCzasu.Tests/Shared/FixedTimeProvider.cs` zamiast kopii w `PolishCalendarTests`, `EntriesPageTests`, `NewEntryPageTests`.
- Teksty komunikatów bez zmian; testy porównują je literalnie i przechodzą (526/526).

## Review Triage Log

Przebieg 1 (quick, 02.11.2026): false 1.

- false — doc `EntryMessages.NotFound` „the report loads its data again” opisuje „zły ekran”: „raport” to celowo ogólne określenie (AD-9: Lista wpisów jest raportem, a `EntryDialogs` używa też Widok miesiąca w epiku 3); tekst komunikatu bez zmian.

## Verification

**Commands:**
- `dotnet build -c Release` -- expected: bez błędów i ostrzeżeń.
- `dotnet test -c Release --no-build` -- expected: wszystkie testy zielone.
