# Rejestr czasu pracy — utwardzony pomysł

Wynik: HARDENED · 2026-10-30

## Po co
- Projekt do nauki (m.in. BMad). Dane na infrastrukturze, którą kontroluje autor, nie w cudzym SaaS. Nie konkuruje z Clockify ani z arkuszem.

## Ustalone
- Jeden użytkownik, bez logowania w v1. Aplikacja lokalna, nasłuchuje tylko na `127.0.0.1`.
- Stos: Node.js, Express, Handlebars, SQLite.
- Wpis składa się z pól: data, projekt (ze słownika), zadanie (ze słownika), opis, czas.
- Słownik zadań jest niezależny od projektu.
- Słowniki projektów i zadań:
  - mają flagę `CZY_AKTYWNY`; nieaktywne pozycje znikają z listy wyboru, ale zostają w historii i eksportach,
  - zmiana nazwy działa wstecz, bo wpis wskazuje na id,
  - usunięcie jest blokowane, jeśli pozycja jest użyta we wpisach; nieużytą można usunąć.
- Czas:
  - przechowywany w minutach (int),
  - wpisywany jako `h:mm`,
  - eksportowany jako godziny dziesiętne (`1,50`),
  - na jeden wpis: więcej niż 0 i najwyżej 24 h.
- Lista wpisów:
  - filtr zakresu dat, domyślnie bieżący miesiąc,
  - opcjonalny filtr projektu i zadania,
  - suma godzin pod listą.
- Edycja i usuwanie wpisu są w v1.
- Eksport:
  - CSV i xlsx z wyniku filtra,
  - kolumny: data, projekt, zadanie, opis, godziny,
  - CSV: separator `;`, UTF-8 z BOM.
- Ścieżka do pliku SQLite jest w konfiguracji, a plik bazy w `.gitignore`. Bez systemu backupów.

## Odrzucone
- Deploy na DigitalOcean (Ubuntu i nginx): wymaga domeny i HTTPS, ochrony logowania i utrzymania serwera. Za dużo jak na projekt do nauki.
- Mailer do resetu hasła: zbędna zależność przy jednym użytkowniku.
- Plik JSON jako baza: ryzyko uszkodzenia przy przerwanym zapisie i nadpisań.

## Poza zakresem v1
- Assumption: logowanie, timer, raporty i wykresy, stawki i kwoty, dostęp z telefonu, import danych.
