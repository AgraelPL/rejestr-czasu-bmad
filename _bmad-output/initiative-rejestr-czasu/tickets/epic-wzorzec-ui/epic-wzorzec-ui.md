---
type: epic
title: "Przejęcie wzorca UI eod-ui"
parent: initiative-rejestr-czasu
covers: [R17, R26, R31]
sources: []
companions: []
after: []
refined: true
risk: high
---

# Przejęcie wzorca UI eod-ui

## Description

Lokalnie każdy istniejący ekran — Projekty, Typy pracy, Nowy wpis, Lista wpisów, pomoc, „nie znaleziono” i układ stron `/konto/...` — działa w szkielecie i komponentach wzorca UI eod-ui (`EodShell`, `EodPage`, `EodCard`, `EodReport`, `UiMessages`), dołączonego jako podmoduł git z `ProjectReference` do `Eod.Platform.UI`, bez części EOD i Syncfusion. Zachowanie ekranów się nie zmienia: wymagania R1–R8, R16, R17, R26 i R31 obowiązują jak dotąd. Epik daje epikom 3 i 5 szkielet, stronę, raport przyjmujący sumy od ekranu i komunikaty wzorca, żeby Widok miesiąca, Eksport, Ustawienia konta i Użytkownicy powstały od razu w nowym wyglądzie.

## Outcome

User pracuje we wszystkich dotychczasowych ekranach w wyglądzie wzorca eod-ui bez utraty żadnej funkcji, a kolejne epiki budują ekrany wyłącznie z komponentów wzorca.

## Requirements

- R1 (parent R17): Każdy raport (Widok miesiąca, Lista wpisów) i lista Słownika ma filtr wartości w każdej kolumnie i licznik pozycji; filtr nie zmienia sum liczonych dla Miesiąca; brak eksportu CSV.
- R2 (parent R26): Aplikacja ma strony pomocy po polsku; ikona `?` w belce otwiera stronę bieżącego ekranu, a z niej spis tematów; strona o administracji jest tylko dla Administratora; każdy ekran ma stronę pomocy zgodną z jego działaniem.
- R3 (parent R31): Gdy połączenie z serwerem się zerwie, aplikacja pokazuje komunikat z prośbą o odświeżenie; niezapisany Wpis może przepaść.

## Constraints

- prd NFR-1: bez połączeń wychodzących; CSP `script-src 'self'` bez wyjątków (AD-12).
- prd NFR-2: repozytoria aplikacji i eod-ui prywatne; sekrety tylko w GitHub Secrets i na serwerze.
- prd NFR-6: belka z Pomocą `?`, Ustawieniami konta i Wyloguj; menu Nowy wpis; Rejestr; Słowniki; Administracja tylko dla Administratora.
- architecture AD-1, AD-9, AD-11, AD-12, AD-22, AD-23: podmoduł `ui/`, `EodReport` dla raportów i list, `UiMessages`, `EodShell`/`EodPage`, `PageHelp`, kontrakt zmian we wzorcu.
- Zachowanie R1–R8, R16, R18, R19, R25 bez zmian; istniejące testy Testcontainers przechodzą bez zmian logiki.

## Done when

- `eod-ui` jest podmodułem repozytorium, `Eod.Platform.UI` referencją projektu; build bez ostrzeżeń, wszystkie testy (po dopasowaniu do nowych komponentów) przechodzą w CI.
- Każdy istniejący ekran działa lokalnie w `EodShell` i `EodPage`; listy Słowników i Lista wpisów na `EodReport` z filtrem wartości i licznikiem, bez eksportu CSV (R17).
- Komunikaty i potwierdzenia idą przez `UiMessages`; Enter nie potwierdza usunięcia, fokus na Anuluj, Escape anuluje.
- Sesja wpisywania nadal mieści każdy Wpis w 10 s samą klawiaturą (R16); strony pomocy opisują ekrany po zmianie (R26); zerwane połączenie pokazuje komunikat (R31).
- Nic z wzorca nie łączy się z zewnątrz (NFR-1, CSP `script-src 'self'`).

## Boundaries

- Nowe ekrany (Widok miesiąca, Eksport, Ustawienia konta, Użytkownicy) należą do epików 3 i 5; ten epik przenosi tylko istniejące.
- Bez części wzorca specyficznych dla EOD i Syncfusion: procesy, sprawy, przełącznik kontekstu, powiadomienia, terminarz, `Eod.Platform.UI.Syncfusion`.
- Bez zmian zachowania i danych: schemat, klasy danych i reguły z architektury zostają.
- Touch point: repozytorium eod-ui (github.com/AgraelPL/eod-ui) — parametr wyłączający eksport CSV w `EodReport`, potwierdzenie w `UiMessages` bez skrótu Enter z fokusem na Anuluj, sumy grup i całości `EodReport` podawane przez ekran, Fluent UI 5.0.1; owner: epic-wzorzec-ui (repozytorium Autora).

## References

- parent — _bmad-output/initiative-rejestr-czasu/initiative-rejestr-czasu.md, Requirements R17, R26, R31; Notes 02.11.2026
- design — https://github.com/AgraelPL/eod-ui, docs/UI_SPEC.md (sekcje 3.1, 4, 6, 7, 9.7, 9.13, 11, 12, 19)
- architecture — _bmad-output/initiative-rejestr-czasu/architecture-rejestr-czasu/architecture-rejestr-czasu.md, AD-1, AD-5, AD-9, AD-11, AD-12, AD-22 (do aktualizacji)

## Notes

- Waits on epic-wpisy-i-lista because: ekrany Nowy wpis i Lista wpisów oraz okno Wpisu do przeniesienia na wzorzec.
- Decision (02.11.2026): Autor przejmuje wzorzec UI eod-ui w całości (podmoduł + `ProjectReference`, ekrany z komponentów `Eod*`, także `EodReport` i `UiMessages`), bez części EOD i Syncfusion; epik przed epikiem 3, żeby nowe ekrany powstały od razu we wzorcu.
- Decision (02.11.2026): R17 bez zmian — eksport CSV w `EodReport` wyłączany parametrem dodanym we wzorcu.
- Decision (02.11.2026): potwierdzenie w `UiMessages` bez skrótu Enter, fokus na Anuluj — zmiana we wzorcu; jedna droga potwierdzeń, zasada AD-11 zostaje.
- Decision (02.11.2026): `EodReport` przyjmuje sumy grup i całości od ekranu (Widok miesiąca z `MonthReport`, AD-5, R9, R10) — zmiana we wzorcu.
- Decision (02.11.2026): refinement — wymagania epiku to linie rodzica R17, R26, R31; zmiany we wzorcu robi agent BMad w osobnym klonie eod-ui (gałąź i PR do eod-ui), PR otwiera i scala Autor (token bez uprawnień do PR), więc historie 1 i 2 mają hitl; zmiany we wzorcu w dwóch historiach: EodReport (AD-23 poz. 1–5) i reszta (poz. 6–10). Zamyka Open question o kolejności zmian we wzorcu.
- Decision (02.11.2026): tracer bullet — historia 3 (podmoduł, CI z prywatnym podmodułem, obraz, szkielet EodShell, UiMessages); najmniej pewna — historia 5 (Lista wpisów na EodReport z sumą od ekranu, R16 w nowym szkielecie); historie 1 i 2 mogą iść równolegle, 4 i 6 równolegle po 3; zamyka Refactor sweep (historia 7). Jeden PR epiku po stronie Rejestru, osobne PR w eod-ui dla historii 1 i 2.
