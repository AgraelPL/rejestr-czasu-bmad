# Pojęcia: Rejestr czasu pracy

Źródło: prd-rejestr-czasu §3. Terminy obowiązują dosłownie w ticketach i kodzie (nazwy w UI).

- **Autor** — twórca aplikacji; ma Konto z rolami User i Administrator.
- **User** — rola: prowadzi własne Słowniki, Wpisy i Zestawienia.
- **Administrator** — rola: zatwierdza, blokuje i usuwa Konta, ustawia hasła; nie widzi w aplikacji treści danych innych Userów (wyjątek: logowanie hasłem, które sam ustawił).
- **Konto** — login i hasło jednej osoby, bez e-maila. Stan: **Oczekuje** (po rejestracji, do zatwierdzenia), **Aktywne**, **Zablokowane**.
- **Odbiorca** — osoba, której User wysyła Zestawienie e-mailem; nie korzysta z aplikacji.
- **Projekt** — pozycja Słownika: kod (np. `RDZ`, unikalny bez względu na wielkość liter, zapisywany wielkimi literami) i nazwa. Ma flagę aktywności.
- **Typ pracy** — pozycja Słownika: nazwa (np. `Development`). Ma flagę aktywności. Niezależny od Projektu.
- **Słownik** — lista Projektów albo lista Typów pracy prowadzona przez Usera; każdy User ma własne Słowniki.
- **Sesja wpisywania** — jedno posiedzenie (zwykle 1–2 h co kilka dni), w którym User przenosi notatki z kilku dni do Wpisów.
- **Wpis** — jeden zarejestrowany odcinek pracy: data, dokładnie jeden Projekt, dokładnie jeden Typ pracy, opis, Czas.
- **Czas** — długość Wpisu w formacie `h:mm`, wielokrotność 15 minut, od `0:15` do `24:00`. W Zestawieniu jako godziny dziesiętne (`1:30` → 1,5).
- **Miesiąc** — miesiąc kalendarzowy; jednostka Widoku miesiąca i Zestawienia.
- **Suma dnia** — suma Czasu Wpisów z jednego dnia.
- **Suma tygodnia** — suma Czasu Wpisów z dni jednego tygodnia (pn–nd) należących do danego Miesiąca; tydzień na przełomie miesięcy liczy się w każdym Miesiącu osobno.
- **Widok miesiąca** — ekran z Wpisami Miesiąca dzień po dniu, z Sumą dnia, Sumą tygodnia i sumą Miesiąca.
- **Lista wpisów** — ekran wyszukiwania Wpisów z filtrami i sumą.
- **Zestawienie** — plik xlsx za jeden Miesiąc w układzie Odbiorcy (export-layout.md).
- **Wiersz zestawienia** — wiersz Zestawienia ze złączenia Wpisów z tego samego dnia o tym samym Projekcie i Typie pracy.
- **Kopia serwera** — kopia całego serwera z aplikacją i bazą, robiona automatycznie przez dostawcę chmury. Pojęcie Kopia bazy wycofane 31.10.2026.
- **Test wrześniowy** — ręczne, lokalne wpisanie 28 Wpisów z września 2026 i porównanie sum Zestawienia z oryginałem.
