---
title: 'Refactor sweep epiku Szkielet i Słowniki'
type: 'refactor'
ticket: '10'
created: '2026-11-01'
status: 'built'
baseline_revision: 'ebc1d442e8ca984db62ce628d366953c01d679d4'
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

**Problem:** Po historiach 1–9 zostały drobne niespójności: usługi ekranów (`Notify`, `Confirm`, `ProjectStore`, `WorkTypeStore`) rejestrowane luzem w `Program.cs`, podczas gdy reszta aplikacji rejestruje się metodami `Add…`, a odmiana „w {n} Wpisie/Wpisach” jest powielona w dwóch ekranach. Odłożone uwagi epiku (ReportGrid i `ThenRender`, AD-11, pakiet ikon) Autor kazał załatwić przed tą historią — są zamknięte w `deferred-work.md`.

**Approach:** Wyłącznie sprzątanie bez zmiany zachowania: jedna metoda rejestracji usług ekranów obok pozostałych `Add…` i jedna funkcja tekstu liczby Wpisów w miejscowniku, używana przez Projekty i Typy pracy; build bez ostrzeżeń, wszystkie testy zielone.

</frozen-after-approval>

## Implementation Notes

- `AddAppScreens()` w `Features/ScreenServiceCollectionExtensions.cs` (rejestruje `Notify`, `Confirm`, `ProjectStore`, `WorkTypeStore`); `Program.cs` woła jedną metodę.
- `Shared/EntryCount.InEntries` daje „1 Wpisie” / „{n} Wpisach”; używają go komunikaty usuwania Projektów i Typów pracy (pokryte istniejącymi testami ekranów).

## Review Triage Log

- medium, patch: `AddAppScreens` w `Shared/` odwracało zależność (`Shared` → `Features`), wbrew diagramowi architektury — przeniesione do `Features/`.
- low, patch: osobny test `EntryCount` powtarzał macierz z testów obu ekranów — usunięty.

## Verification

**Commands:**
- `dotnet build -c Release` -- expected: bez błędów i ostrzeżeń.
- `dotnet test -c Release --no-build` -- expected: wszystkie testy zielone.
