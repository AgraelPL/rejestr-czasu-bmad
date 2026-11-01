---
title: 'Refactor sweep epiku Konta i wdrożenie'
type: 'refactor'
ticket: '7'
created: '2026-11-01'
status: done
baseline_revision: '2cb8b4b77a7cd50b01c18488c8f5f6bf21144620'
route: 'oneshot'
route_source: 'auto'
risk: 'low'
review: 'none'
review_source: 'auto'
lenses_ran: []
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Historia zamyka epik porządkami odłożonymi w planach i przeglądach historii 4.1–4.5 (wdrożenie i rollback przeniesione do epiku 6).

**Approach:** Przegląd listy odłożonych uwag; zamknięte uwagi tylko potwierdzić, otwarte posprzątać albo przenieść do nowych historii.

</frozen-after-approval>

## Implementation Notes

- Przegląd 01.11.2026: brak otwartych uwag z historii 4.1–4.5.
  - `deferred-work.md`: cztery wpisy epiku rozstrzygnięte 31.10.2026 (ochrona `main` przez publiczne repozytorium, zamrożenie `Initial` w notatkach 6.1, `account_settings` leniwie w epiku 3, SDK w Dockerfile).
  - Dockerfile używa `mcr.microsoft.com/dotnet/sdk:10.0.403`, zgodnie z `global.json`.
  - Logi przeglądu planów 4.1–4.5: jedyny wpis `defer` (4.2, `account_settings`) rozstrzygnięty; brak wpisów `maybe-false`.
- Bez zmian kodu, więc bez przeglądu kodu.

## Verification

**Commands:**
- `dotnet build -c Release` -- expected: bez błędów i ostrzeżeń.
- `dotnet test -c Release --no-build` -- expected: wszystkie testy zielone.
