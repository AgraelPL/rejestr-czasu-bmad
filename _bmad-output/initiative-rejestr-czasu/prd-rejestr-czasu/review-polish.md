# Review: PRD Rejestr czasu pracy, final polish (structure, then prose)

Purpose read: the PRD exists to help the Autor and the BMad agents (architecture, epics, stories) build the time-register app without reopening approved decisions. The addendum carries technical decisions and the evidence for the Zestawienie layout.
Structure model: requirements spec (PRD). Reader type: humans + LLM agents.
Size: PRD 2021 words, addendum 188 words.
Constraint honored: no change to requirements, numbers, scope or decisions. Rows marked **[MEANING?]** could be read as changing meaning, so the author should confirm them.

Format: each finding has the file, the exact old text (find), the new text (replace) and a one-line reason.

---

## Lens: structure (6)

### S1. prd-rejestr-czasu.md: merge §8 and §9 (both say "Brak")
Find:
```
## 8. Otwarte pytania

Brak. Położenie sum i wiersza RAZEM sprawdzone w oryginalnym xlsx 30.10.2026 (FR-12).

## 9. Indeks założeń

Brak otwartych założeń. Osiem założeń pierwszej wersji (FR-1, FR-3, FR-5 ×2, FR-7, FR-11, FR-13, FR-14) autor potwierdził 30.10.2026; są wpisane w wymagania jako ustalenia.
```
Replace:
```
## 8. Otwarte pytania i założenia

Brak otwartych pytań ani założeń.
- Położenie sum i wiersza RAZEM sprawdzone w oryginalnym xlsx 30.10.2026 (FR-12).
- Osiem założeń pierwszej wersji (FR-1, FR-3, FR-5 ×2, FR-7, FR-11, FR-13, FR-14) Autor potwierdził 30.10.2026; są wpisane w wymagania jako ustalenia.
```
Reason: these are two near-empty sections with the same status, and one section is enough. If you accept this, change the reference "(§9)" in §0 to "(§8)" (see S2). It also includes the capitalization fix for "Autor".

### S2. prd-rejestr-czasu.md: §0 restates §9
Find: `Założenia z pierwszej wersji potwierdził autor 30.10.2026 (§9).`
Replace: `Założenia z pierwszej wersji potwierdził Autor 30.10.2026 (§8).`
Reason: keeps the pointer in sync with S1 and fixes the capitalization of the term. If you reject S1, use `(§9)`.

### S3. prd-rejestr-czasu.md: a requirement sits in the "Opis" of §4.5 instead of in an FR
Find: `**Opis:** Jedno kliknięcie w Widoku miesiąca daje plik xlsx za wybrany Miesiąc. Sumy liczy aplikacja i zapisuje jako wartości, nie formuły. Realizuje UJ-2.`
Replace: `**Opis:** Jedno kliknięcie w Widoku miesiąca daje Zestawienie za wybrany Miesiąc. Realizuje UJ-2.`
Then in FR-12, find: `Po ostatnim Wierszu zestawienia jest jeden pusty wiersz,`
Replace: `Wszystkie sumy aplikacja liczy sama i zapisuje jako wartości, nie formuły.

Po ostatnim Wierszu zestawienia jest jeden pusty wiersz,`
Reason: "wartości, nie formuły" is testable behavior. In the other sections, "Opis" holds only context, and FR-12 is where agents will look for the file layout. **[MEANING?]** This only moves text, but it promotes a sentence from Opis to the FR body.

### S4. prd-rejestr-czasu.md: FR-8 packs five rules into one paragraph, unlike FR-1, FR-5 and FR-9
Find: `Autor może przeglądać Wpisy z filtrami: zakres dat (domyślnie bieżący Miesiąc), Projekt, Typ pracy (oba także nieaktywne). Lista jest posortowana od najnowszej daty. Pod listą widać liczbę Wpisów i sumę Czasu dla bieżących filtrów. Z listy można edytować, kopiować i usuwać Wpis.`
Replace:
```
Autor może przeglądać Wpisy z filtrami: zakres dat (domyślnie bieżący Miesiąc), Projekt, Typ pracy (oba także nieaktywne).
- Lista jest posortowana od najnowszej daty.
- Pod listą widać liczbę Wpisów i sumę Czasu dla bieżących filtrów.
- Z listy można edytować, kopiować i usuwać Wpis.
```
Reason: the lead sentence plus bullets matches the pattern of the other FRs and splits the list into checkable criteria (0 words saved).

### S5. prd-rejestr-czasu.md: SM-1 is a ~90-word bullet mixing the criterion, the test conditions and the consequence
Find: `Test odbywa się wyłącznie lokalnie, wpisywany ręcznie; prawdziwe dane wrześniowe (opisy, Wpisy, plik) nigdy nie trafiają do repozytorium jako dane testowe. Waliduje FR-5, FR-9–FR-12. Niepowodzenie przesuwa pierwszy miesiąc „na serio” o miesiąc.`
Replace:
```
  - Warunki: Test wrześniowy odbywa się wyłącznie lokalnie, a Wpisy są wpisywane ręcznie; prawdziwe dane wrześniowe (opisy, Wpisy, plik) nigdy nie trafiają do repozytorium jako dane testowe.
  - Waliduje FR-5, FR-9–FR-12.
  - Niepowodzenie przesuwa pierwszy miesiąc „na serio” o miesiąc.
```
Reason: easier to scan; this also fixes the participle "Test … wpisywany ręcznie", because it is the Wpisy that are entered by hand, not the test. (This overlaps with prose finding P14. If you accept S5, skip P14.)

### S6. addendum.md: "Źródło układu Zestawienia" is one dense 93-word paragraph of cell facts
Find: `Analiza oryginału: \`docs/projekt-testowy-material.md\` §1 (lokalnie, poza repozytorium). Sprawdzone w oryginalnym xlsx 30.10.2026: nagłówek w wierszu 1 (zamrożony, \`A2\`), 28 wierszy danych (2–29), wiersz 30 pusty, wiersz 31 pogrubiony z „RAZEM WRZESIEŃ 2026” w E. Suma dnia (\`=SUM(F6:F7)\`) w pierwszym wierszu dnia, Suma tygodnia (\`=G6+G8+G9+G10\`) w pierwszym wierszu tygodnia, RAZEM \`=SUM(F2:F29)\` i \`=H2+H6+H11+H18+H26\`. Data w B to tekst, F ma format \`0\`, E ma zawijanie. Tygodnie: 30 / 16 / 18 / 25 / 14 = 103 h. Aplikacja zapisuje sumy jako wartości; data w B i format F zmienione decyzją autora (PRD FR-12).`
(The backslashes above only escape the backticks in this file. Match the raw text in addendum.md, line 14.)
Replace:
```
Analiza oryginału: `docs/projekt-testowy-material.md` §1 (lokalnie, poza repozytorium). Sprawdzone w oryginalnym xlsx 30.10.2026:
- nagłówek w wierszu 1, zamrożone okienko od `A2`;
- 28 wierszy danych (2–29), wiersz 30 pusty, wiersz 31 pogrubiony z „RAZEM WRZESIEŃ 2026” w E;
- Suma dnia w pierwszym wierszu dnia (np. `=SUM(F6:F7)`), Suma tygodnia w pierwszym wierszu tygodnia (np. `=G6+G8+G9+G10`);
- wiersz RAZEM: `=SUM(F2:F29)` i `=H2+H6+H11+H18+H26`;
- data w B to tekst, F ma format `0`, E ma zawijanie tekstu;
- Sumy tygodnia: 30 / 16 / 18 / 25 / 14 = 103 h.

Odstępstwa od oryginału (decyzja Autora, PRD FR-12): aplikacja zapisuje sumy jako wartości, a nie formuły; data w B i format F są zmienione.
```
Reason: separates observed facts from the deliberate deviations. It also clarifies "(zamrożony, `A2`)", marks the formulas as examples, and uses the term "Autor". **[MEANING?]** The "np." added before the formulas states that they are examples, which the original only implied.

Structure items reviewed and kept (PRESERVE, no change): the §2.1 bullet that repeats the dual goal from §1 (it reinforces the BMad goal); the FR-9 bullet "Suma tygodnia obejmuje tylko dni danego Miesiąca", which repeats §3 (useful reinforcement for agents); and addendum bullets 1–3, which overlap with NFR-1/NFR-4/FR-14/FR-15 (they record where these decisions came from in the brief).
Summary: 6 structure recommendations. Estimated reduction is about 0 words: these are reorganizations and the deletions in S3 and S1 are offset by the moved text. There are no comprehension trade-offs.

---

## Lens: prose (19)

Style noted and preserved: terse, specification-like Polish, verbless fragments in lists and UJ ("Data domyślnie dzisiejsza."), and Polish typographic quotes „…”.

### P1. prd-rejestr-czasu.md: the term "Autor" in lowercase (8 places)
- Find: `PRD dla autora (jedynego użytkownika)` → Replace: `PRD dla Autora (jedynego użytkownika)`
- Find: `wyłącznie na komputerze autora. Dane` → Replace: `wyłącznie na komputerze Autora. Dane`
- Find: `narzędzie, w którym autor od stycznia 2027` → Replace: `narzędzie, w którym Autor od stycznia 2027`
- Find: `(decyzja autora, 30.10.2026)` → Replace: `(decyzja Autora, 30.10.2026)`
- Find: `aktualna przeglądarka na komputerze autora;` → Replace: `aktualna przeglądarka na komputerze Autora;`
- Find: `FR-14) autor potwierdził 30.10.2026` → Replace: `FR-14) Autor potwierdził 30.10.2026` (skip if S1 is accepted)
- §0: covered by S2.

Reason: §3 defines "Autor" and requires that terms be used verbatim with the defined capitalization.

### P2. addendum.md: "Autor" in lowercase
Find: `zmienione decyzją autora (PRD FR-12)` → Replace: `zmienione decyzją Autora (PRD FR-12)` (skip if S6 is accepted)
Reason: the term from §3 of the PRD.

### P3. prd-rejestr-czasu.md: "Projekt" in §1 means the BMad undertaking, not the Słownik term
Find: `Projekt ma dwa cele o równej wadze:`
Replace: `Przedsięwzięcie ma dwa cele o równej wadze:`
Reason: §3 reserves "Projekt" for a Słownik entry, so the capitalized word here is ambiguous. **[MEANING?]** The word changes, but what it refers to does not.

### P4. prd-rejestr-czasu.md §1: Słownik and Wpis terms in lowercase
Find: `literówki w kodzie projektu, dwóch projektów w jednym wpisie, ręcznie liczonych sum. Na koniec miesiąca jedno kliknięcie`
Replace: `literówki w kodzie Projektu, dwóch Projektów w jednym Wpisie, ręcznie liczonych sum. Na koniec Miesiąca jedno kliknięcie`
Reason: these are the defined terms Projekt, Wpis and Miesiąc.

### P5. prd-rejestr-czasu.md §2.1: "Miesiąc" in lowercase
Find: `- Na koniec miesiąca oddać Odbiorcy Zestawienie`
Replace: `- Na koniec Miesiąca oddać Odbiorcy Zestawienie`
Reason: the defined term Miesiąc.

### P6. prd-rejestr-czasu.md UJ-1: terms in lowercase
Find: `przy projekcie RDZ, przechodzi do otwartej karty aplikacji, wybiera projekt i Typ pracy z list, wpisuje krótki opis i czas \`1:30\`, zapisuje.`
Replace: `przy Projekcie RDZ, przechodzi do otwartej karty aplikacji, wybiera Projekt i Typ pracy z list, wpisuje krótki opis i Czas \`1:30\`, zapisuje.`
Reason: the defined terms Projekt and Czas (and "Projekt" is already capitalized next to "Typ pracy" in the same sentence elsewhere).

### P7. prd-rejestr-czasu.md UJ-2: terms in lowercase and a paraphrased term
Find: `otwiera Widok miesiąca za poprzedni miesiąc, sprawdza sumy dni i tygodni, poprawia brakujący wpis, klika eksport i wysyła plik Odbiorcy.`
Replace: `otwiera Widok miesiąca za poprzedni Miesiąc, sprawdza Sumy dnia i Sumy tygodnia, uzupełnia brakujący Wpis, klika eksport i wysyła Zestawienie Odbiorcy.`
Reason: uses the terms Miesiąc, Suma dnia/Suma tygodnia, Wpis and Zestawienie verbatim. "poprawia brakujący" is illogical (you cannot correct a missing entry), so it becomes "uzupełnia".

### P8. prd-rejestr-czasu.md UJ-3: "Projekt" in lowercase
Find: `ustawia zakres 01.01–31.03 i projekt RDZ,`
Replace: `ustawia zakres 01.01–31.03 i Projekt RDZ,`
Reason: the defined term.

### P9. prd-rejestr-czasu.md: "Sumami/Sumy … Miesiąca" implies a term "Suma Miesiąca" that §3 does not define (2 places)
- Find: `z Sumami dnia, tygodnia i Miesiąca.` → Replace: `z Sumą dnia, Sumą tygodnia i sumą Miesiąca.`
- Find: `Sumy dnia, tygodnia i Miesiąca w Widoku miesiąca są identyczne z sumami w Zestawieniu za ten Miesiąc.` → Replace: `Sumy dnia, Sumy tygodnia i suma Miesiąca w Widoku miesiąca są identyczne z odpowiednimi sumami w Zestawieniu za ten Miesiąc.`

Reason: only "Suma dnia" and "Suma tygodnia" are terms. FR-9 already writes "sumą Miesiąca" in lowercase, so this matches it.

### P10. prd-rejestr-czasu.md §4.1 Opis: the subject switches mid-sentence
Find: `przy pierwszym uruchomieniu są puste i wpisuje je ręcznie (7 Projektów, 2 Typy pracy).`
Replace: `przy pierwszym uruchomieniu są puste i Autor wypełnia je ręcznie (7 Projektów, 2 Typy pracy).`
Reason: grammatically, the subject of "wpisuje" is "Słowniki", so the sentence needs an explicit subject.

### P11. prd-rejestr-czasu.md §4.2 Opis: unclear which "ekran" is meant
Find: `**Opis:** Najczęściej używany ekran; ma działać szybko z klawiatury (NFR-3).`
Replace: `**Opis:** Formularz Wpisu to najczęściej używany ekran; ma działać szybko z klawiatury (NFR-3).`
Reason: the section heading is "Wpisy", not a screen name, and agents need the referent stated.

### P12. prd-rejestr-czasu.md FR-7: "pole jest puste do wyboru" is unclear
Find: `pole jest puste do wyboru.`
Replace: `odpowiednie pole pozostaje puste do ponownego wyboru.`
Reason: says which field is empty and why.

### P13. prd-rejestr-czasu.md FR-13: inconsistent placeholder
Find: `Bez prefiksu nazwa to \`Zestawienie-<miesiąc>_<RRRR>.xlsx\`.`
Replace: `Bez prefiksu nazwa to \`Zestawienie-<miesiąc małą literą>_<RRRR>.xlsx\`.`
Reason: the same placeholder should have the same name within one FR.

### P14. prd-rejestr-czasu.md SM-1: misattached participle (skip if S5 is accepted)
Find: `Test odbywa się wyłącznie lokalnie, wpisywany ręcznie;`
Replace: `Test wrześniowy odbywa się wyłącznie lokalnie, a Wpisy są wpisywane ręcznie;`
Reason: "wpisywany" grammatically agrees with "Test", but it is the Wpisy that are entered by hand. This also uses the term verbatim.

### P15. prd-rejestr-czasu.md FR-14: "Kopia bazy" term and run-on sentence
- Find: `Kopia bazy powstaje przy każdym uruchomieniu aplikacji, zanim przyjmie pierwsze żądanie,` → Replace: `Kopia bazy powstaje przy każdym uruchomieniu aplikacji, zanim aplikacja przyjmie pierwsze żądanie,`
- Find: `Kopie trafiają do osobnego katalogu, nazwa \`rejestr-RRRR-MM-DD_GGMMSS.sqlite\`, zachowywanych jest 10 najnowszych.` → Replace: `Kopie bazy trafiają do osobnego katalogu pod nazwą \`rejestr-RRRR-MM-DD_GGMMSS.sqlite\`; zachowywanych jest 10 najnowszych.`
- Find: `- Najstarsza kopia jest usuwana dopiero po udanym zapisie nowej.` → Replace: `- Najstarsza Kopia bazy jest usuwana dopiero po udanym zapisie nowej.`
- Find: `- Nieudana kopia przy starcie` → Replace: `- Nieudana Kopia bazy przy starcie`
- Find: `- Nieudana kopia w trakcie działania` → Replace: `- Nieudana Kopia bazy w trakcie działania`

Reason: the subject of "przyjmie" is ambiguous (it reads as "Kopia bazy"); "nazwa …" is an unattached noun; and the term should be used verbatim.

### P16. prd-rejestr-czasu.md §6: "Kopia bazy" term, and Projekt in NFR-2
- Find: `- Brak synchronizacji i kopii poza dyskiem lokalnym;` → Replace: `- Brak synchronizacji i Kopii bazy poza dyskiem lokalnym;`
- Find: `Kody projektów i sumy z września` → Replace: `Kody Projektów i sumy z września`

Reason: the defined terms.

### P17. prd-rejestr-czasu.md §6 heading: unidiomatic
Find: `## 6. Poza celem (świadomie nie robimy)`
Replace: `## 6. Poza zakresem (świadomie nie robimy)`
Reason: "poza zakresem" is the standard Polish term for out of scope. "Poza celem" collides with §1, which talks about "cele".

### P18. prd-rejestr-czasu.md SM-4: missing "o tym"
Find: `powstaje notatka, co działało, a co nie.`
Replace: `powstaje notatka o tym, co działało, a co nie.`
Reason: in standard Polish syntax, a noun cannot take a "co"-clause directly.

### P19. addendum.md: wording differs from FR-14 for the same behavior (skip if not wanted)
Find: `- Kopia bazy przy starcie przed przyjęciem żądań i cyklicznie`
Replace: `- Kopia bazy przy starcie, przed przyjęciem pierwszego żądania, i cyklicznie`
Reason: aligns with FR-14 ("zanim przyjmie pierwsze żądanie") so that agents do not see two formulations.

---

## Flagged as possibly changing meaning (author to confirm)
- **S3**: moves "sumy jako wartości, nie formuły" from §4.5 Opis into the body of FR-12, which promotes it from description to requirement text.
- **S6**: adds "np." before the formulas taken from the original (states they are examples) and adds the heading "Odstępstwa od oryginału".
- **P3**: "Projekt" becomes "Przedsięwzięcie" in §1. The referent is unchanged, but this is a word change.
- Not proposed, only noted: FR-14 "co najmniej raz na dobę, gdy aplikacja działa dłużej" is ambiguous ("dłużej" than what? presumably a day). Clarifying it would touch the requirement, so it is left to the author.
