# Materiał do notatki końcowej o metodzie BMad

Surowe obserwacje z 30.10.2026 (brief → PRD → inicjatywa → architektura), do wykorzystania w notatce po pierwszym miesiącu „na serio” (SM-4). Fakty, nie wnioski — wnioski wyciągniesz po miesiącu użycia.

## Co zadziałało

- **Audyt założeń na „gotowym” briefie** wyłapał rzecz, której nikt nie zapisał: aplikacja uruchamiana z terminala to tarcie dla „wpisu w dniu pracy”. Pytanie o kopię bazy przy długim działaniu doprowadziło potem do odkrycia prawdziwego rytmu pracy (notatki + wpisywanie co 3–4 dni), który zmienił Wizję, UJ-1, SM-2 i FR-5.
- **Sprawdzenie źródła zamiast założenia.** Otwarte pytanie „gdzie stoją sumy w arkuszu” zostało rozstrzygnięte odczytem oryginalnego xlsx w kilka sekund, zanim weszło do PRD jako `[ASSUMPTION]`.
- **Tryb szybki PRD** przy szczegółowym briefie: jedna tabela 9 pytań z proponowanymi odpowiedziami, „wszystko na tak”, pełny szkic w jednym kroku.
- **Niezależni recenzenci w osobnych kontekstach** znaleźli realne luki, których autor dokumentu nie widział: brak wspólnego formularza Wpisu, sumy liczone z przefiltrowanych wierszy, pobieranie pliku wymagające JavaScriptu, zarchiwizowany pakiet `Blazored.FluentValidation`, Fluent UI 5 ściągający skrypty z unpkg.com, zmieniona ścieżka wolumenu w `postgres:18`.
- **Dziennik decyzji (`.memlog.md`)** pozwolił odtworzyć, kto i kiedy podjął każdą decyzję, i oddzielić świadome odstępstwa od luk.

## Co nie zadziałało lub kosztowało

- **Stos technologiczny padł za późno.** Brief zapisał Node.js/SQLite/`npm start`; dopiero w architekturze okazało się, że chcesz ćwiczyć .NET/Blazor/PostgreSQL. Brief, PRD i inicjatywa wymagają poprawek po fakcie. Pytanie „jaki stos chcesz przećwiczyć?” powinno paść w briefie.
- **Rytm pracy odkryty dopiero w PRD.** „Wpis w dniu pracy” był założeniem briefu, nie zweryfikowanym z Tobą; wyszedł przy okazji pytania o backup.
- **Pomyłka agenta przy raportowaniu recenzji.** Przy finalizacji PRD pierwsza tabela uwag opierała się na podsumowaniach niezgodnych z faktycznymi raportami recenzentów; poprawiona po nadejściu prawdziwych raportów i odnotowana w dzienniku. Wniosek dla metody: wyniki subagentów weryfikować z ich plikami, zanim trafią do decyzji.
- **Zakres rośnie przez wzorce UI.** Zrzut ekranu innej aplikacji wniósł grupowanie, sortowanie wielokolumnowe, filtry i CSV — ponad PRD. Pomogło jawne „co v1, co później”, ale to praca dla Sofii i Johna.
- **Ciężar procesu na małym projekcie.** Na jeden dzień przypadło: audyt, PRD z 3 recenzjami i korektą, inicjatywa, architektura z 3 recenzjami. Dla aplikacji z 4 ekranami to dużo dokumentów; wartość widać w lukach, które wyłapały recenzje.

## Liczby z dnia

- PRD: 15 FR, 5 NFR, 31 wpisów w dzienniku, 2 recenzje + korekta (25 poprawek, 22 bez zmiany treści).
- Inicjatywa: R1–R16, 7 ograniczeń, 2 pliki towarzyszące.
- Architektura: AD-1–AD-14; 3 recenzje (zgodność, aktualność, adwersarz), łącznie ok. 20 ustaleń, z czego 3 wymagały Twojej decyzji.

## Do sprawdzenia po pierwszym miesiącu

- Czy reguły AD-5 (jedno źródło sum) i AD-10 (jeden formularz) faktycznie utrzymały spójność między epikami budowanymi przez różnych agentów?
- Czy zasada „tylko agenci” wytrzymała zderzenie z terminem 28.12.2026?
- Ile poprawek w dokumentach planistycznych wymusiła implementacja?
