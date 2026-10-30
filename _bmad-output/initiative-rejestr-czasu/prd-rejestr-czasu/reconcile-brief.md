---
title: "Uzgodnienie wejść: brief → PRD Rejestr czasu pracy"
created: 2026-10-30
inputs:
  - ../brief-rejestr-czasu/brief-rejestr-czasu.md
  - ../brief-rejestr-czasu/addendum.md
targets:
  - prd-rejestr-czasu.md
  - addendum.md
decision_log: .memlog.md
---

# Uzgodnienie wejść: brief i addendum briefu → PRD

## Werdykt

PRD wiernie przenosi brief. Zakres v1, poza zakresem, kryteria sukcesu, ryzyka i osiem pytań z addendum briefu są pokryte. Wszystkie odstępstwa od dotychczasowego układu arkusza (data w B jako prawdziwa data Excela, format ogólny w F, 10 kopii, kolejność wierszy, tygodnie tylko z danego miesiąca, pomijanie dni bez wpisów, ręczne słowniki) mają wpis w `.memlog.md`. Nie ma sprzeczności bez decyzji. Zostaje kilka **osłabień** i **pominięć** intencji jakościowych. Żadne nie blokuje architektury, ale dwa (G1, G2) warto domknąć przed epikami, bo dotyczą prywatności danych klientów.

## Luki

### G1. Baza poza folderami synchronizowanymi z chmurą: z wymagania zrobiła się rada w dokumentacji (osłabienie)

- **Brief, Zakres v1:** „Baza SQLite w lokalnym pliku poza folderami synchronizowanymi z chmurą, ścieżka w konfiguracji.” Uzasadnienie: dane o klientach zostają na sprzęcie autora („Dlaczego nie gotowe narzędzie”).
- **PRD FR-15:** „Domyślne ścieżki leżą poza repozytorium; dokumentacja uruchomienia mówi, żeby nie wskazywać folderów synchronizowanych z chmurą.” Addendum PRD powtarza ograniczenie tylko jako decyzję techniczną.
- **Problem:** „poza repozytorium” to nie to samo co „poza chmurą”. Na Windows `Documents` bywa przekierowany do OneDrive, a repozytorium leży w `C:/Users/kamil/Documents/...`. Domyślna ścieżka obok repozytorium może więc trafić do chmury, a wymaganie tego nie wyklucza. W `.memlog.md` nie ma decyzji o osłabieniu.
- **Propozycja:** w FR-15 wymagać, by domyślne ścieżki bazy i kopii leżały poza folderami synchronizowanymi z chmurą (np. `%LOCALAPPDATA%`), albo zapisać w memlogu, że to świadomie tylko zalecenie.

### G2. „Dane klientów nie opuszczają sprzętu” jest w wizji, ale nie w NFR (pominięcie)

- **Brief:** dane o klientach mają zostać na sprzęcie autora. To główny powód odrzucenia Clockify.
- **PRD §1:** „Dane o klientach nie opuszczają jego sprzętu.” Jednak NFR-1 mówi tylko o nasłuchu na `127.0.0.1` i braku logowania.
- **Problem:** żadne wymaganie nie zabrania połączeń wychodzących, takich jak telemetria, zewnętrzne CDN, fonty czy zgłaszanie błędów do usług zewnętrznych. Agent piszący kod może dodać zależność z CDN albo raportowanie błędów i formalnie spełnić NFR-1.
- **Propozycja:** dopisać do NFR-1 (albo dodać NFR-6): aplikacja nie wykonuje żadnych połączeń sieciowych poza `127.0.0.1`, a wszystkie zasoby frontendu są serwowane lokalnie.

### G3. Test wrześniowy „wyłącznie lokalnie” i „wpisywany ręcznie”: pominięte w SM-1 (pominięcie)

- **Brief, Ryzyka:** „Test wrześniowy odbywa się wyłącznie lokalnie.” **Brief, Poza zakresem:** „Wrzesień na potrzeby testu wpisuje się ręcznie.”
- **PRD SM-1** nie mówi, kto i gdzie wykonuje test. NFR-2 wymaga anonimizacji testów, ale pozwala zostawić kody projektów i sumy z września tylko w dokumentach planistycznych.
- **Problem:** agent mający walidować FR-9–FR-12 może naturalnie zbudować automatyczny test (fixture) z prawdziwymi kodami, opisami i sumami 30/16/18/25/14 i zacommitować go do publicznego repozytorium. Granica „dokumenty planistyczne: tak, testy: nie” wynika z NFR-2 tylko pośrednio.
- **Propozycja:** w SM-1 dopisać: wykonywany ręcznie przez Autora, na lokalnej bazie; automatyczne testy układu Zestawienia używają danych zanonimizowanych (np. ta sama struktura tygodni z fikcyjnymi kodami).

### G4. Dwa cele „o równej wadze” zeszły w PRD do roli celu pobocznego (osłabienie intencji)

- **Brief, W skrócie:** „Projekt ma dwa cele o równej wadze: nauczyć się metody BMad […] i mieć narzędzie […].”
- **PRD §2.1:** „(Cel osobny) Przejść cały cykl BMad”. **§7:** SM-1–SM-3 pod nagłówkiem „Główne”, SM-4 pod „Nauka BMad”.
- **Problem:** nagłówek „Główne” wprowadza hierarchię, której w briefie nie ma. SM-C1 częściowo to równoważy („wygrywa metoda”), ale czytelnik PRD (agent) widzi naukę jako dodatek. W memlogu nie ma decyzji o zmianie wag.
- **Propozycja:** zmienić „Główne” na „Narzędzie” i zaznaczyć, że obie grupy mają równą wagę, a przy konflikcie decyduje SM-C1.

### G5. Zasada „tylko agenci”: zgubiona druga połowa (drobne pominięcie)

- **Brief, Ryzyka:** „Utknięcie agenta to materiał do notatki końcowej, nie powód do obejścia zasady.”
- **PRD SM-C1** przenosi tylko pierwszą część (ręczne poprawki = porażka SM-4). Brakuje wskazówki, co robić, gdy agent utknie: zapisać to do notatki z SM-4.
- **Propozycja:** jedno zdanie w SM-4 albo SM-C1.

## Uwagi drobne (bez wpływu na zakres)

- **„W kilka sekund” → „do ok. 10 sekund” (NFR-3).** To uszczegółowienie, a nie sprzeczność. 10 s to górna granica „kilku sekund”. Można zostawić; ewentualnie dopisać, że chodzi o typowy wpis z domyślną datą.
- **Zmiana kodu Projektu (FR-1).** Brief mówi o zmianie *nazwy*, która obejmuje historię. PRD pozwala też zmienić *kod* z efektem wstecznym. To rozszerzenie zgodne z problemem `RDEZ`/`RDZ`, ale nie ma go w memlogu. Skutek: ponowny eksport starego miesiąca da inny plik niż wysłany. Warto zapisać jako świadomą decyzję.
- **Odstępstwa od „dotychczasowego układu arkusza”** (prawdziwa data w B, format ogólny w F, sumy jako wartości) mają wpisy w memlogu i addendum PRD. Odbiorca potwierdził tylko godziny dziesiętne (30.10.2026). Zmiana typu daty jest dla niego niewidoczna, więc ryzyko jest niskie. SM-3 i tak to zweryfikuje.
- **Skala z addendum briefu** (20 dni, do 3 wpisów i 12 h dziennie, 5 dni weekendowych) nie trafiła do PRD ani do addendum PRD. Nie jest wymaganiem, ale przydałaby się architekturze i testom (weekendy w Widoku miesiąca i w Zestawieniu).
- **Okno budowy „grudzień 2026”** nie jest nazwane w PRD. Wynika pośrednio z terminu SM-1 (28.12.2026).
- **Usterka redakcyjna FR-12:** tabela ma podwójny nagłówek (`| Kolumna | Zawartość |` i zaraz pod nim `| Kolumna (nagłówek) | Zawartość |` z drugim separatorem). Przy renderowaniu powstaje wiersz-śmieć. Trzeba usunąć pierwsze dwie linie tabeli.

## Pokryte bez uwag (kontrola)

Słowniki z flagą aktywności i usuwaniem tylko nieużytych pozycji (FR-1–FR-4); wpis z kalendarzem, wyliczanym dniem tygodnia, jednym projektem i krokiem 15 min (FR-5); kopiowanie z dzisiejszą datą (FR-7, memlog); lista z filtrami i sumą (FR-8); widok miesiąca z sumami pn–nd (FR-9, FR-10); złączanie wierszy i układ Zestawienia z wierszem RAZEM (FR-11, FR-12); jedno kliknięcie i sumy jako wartości (§4.5); kopie przy starcie i raz na dobę z rotacją (FR-14, memlog); `127.0.0.1`, brak logowania, `npm start`, brak autostartu (NFR-1, NFR-4, §6); `.gitignore` i anonimizacja (NFR-2, addendum PRD); cała lista „Poza zakresem v1” (§6); ryzyko kopii na tym samym dysku (§6); kryteria sukcesu 1–6 (SM-1–SM-4, SM-C1); punkt decyzyjny 28.12 i przesunięcie startu (SM-1); osiem pytań z addendum briefu (memlog, §8, §9).
