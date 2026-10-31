# Ocena jakości PRD — Rejestr czasu pracy (wersja z 31.10.2026)

Recenzja według rubryki `.claude/skills/bmad-prd/assets/prd-validation-checklist.md`. Przeczytałem `prd-rejestr-czasu.md` i `addendum.md`; dla porównania także `forge-chmura-konta-wdrozenie.md` i `.memlog.md`. Skala: projekt hobbystyczny i naukowy, Autor plus jedno konto znajomego; od tej wersji aplikacja działa publicznie w internecie. Nie zgłaszam zwięzłości, która wynika z tej skali. Bardziej rygorystycznie oceniam tylko to, co dotyka wystawienia aplikacji do internetu i spójności po aktualizacji.

## Werdykt ogólny

Rdzeń rejestru (FR-1–FR-13, FR-16) jest nadal bardzo dobry. Wymagania są testowalne, decyzje mają daty, a kompromisy nazwano wprost (FR-1 „Świadoma konsekwencja”, NFR-10, §6.1). Aktualizację wprowadzono starannie: dawne sformułowania o jednym użytkowniku i localhost prawie zniknęły, a numeracja FR jest stała i odsyłacze się rozwiązują. Słabsze są nowe obszary. Konta (FR-17–FR-22) nie mają pełnej maszyny stanów, NFR-8 nie podaje progów, a nie ma reguł dla haseł. Wdrożenie (FR-28, SM-4) nie mówi, co się dzieje po nieudanym sprawdzeniu stanu ani jak przećwiczyć odtworzenie Kopii serwera bez cofnięcia danych produkcyjnych. Do tego dochodzi świadomie przyjęte ryzyko terminu: cały nowy zakres ma powstać przed Testem wrześniowym i nie ma planu awaryjnego. Nic z tego nie blokuje architektury, ale kilka punktów trzeba domknąć, zanim powstaną historie z epiku kont.

## Gotowość do decyzji — strong

Decyzje są zapisane jako decyzje z datą i autorem: uchylenie zasady „dane nie opuszczają komputera” (§1), „Autor akceptuje utratę do Miesiąca (31.10.2026)” (§6.1), „nie ma planu awaryjnego (decyzja Autora, 31.10.2026)” (§6.1). Kompromisy podają, z czego zrezygnowano: odtworzenie „cofa cały serwer, wszystkim Userom naraz”, „niezapisany Wpis może przepaść” (NFR-10), Administrator „do chwili zmiany zna hasło, które ustawił” (§6.1). Odrzucone warianty są w addendum z uzasadnieniem. Tak powinna wyglądać szczerość wobec kompromisów.

Jedna rysa: §8 stwierdza „Brak otwartych pytań ani założeń”, a addendum zostawia otwartą decyzję o przepisaniu migracji `Initial` („Decyzja należy do architektury”, K8). Okno na tę decyzję zamyka się z pierwszym wdrożeniem. To prawdziwe otwarte pytanie z terminem, a nie widać go w §8.

### Findings
- **medium** Otwarta decyzja K8 nie figuruje w §8 (§8; addendum „Okno na przepisanie schematu”) — §8 mówi „Brak otwartych pytań”, a decyzja z nieodwracalnym terminem („po wdrożeniu to okno się zamyka”) jest odłożona. *Fix:* dodać do §8 jedną pozycję: „Przepisanie `Initial` czy migracja z `UserId` — decyzja architektury przed pierwszym wdrożeniem (NFR-9)”.
- **low** Ryzyko terminu nazwane, ale bez sygnału ostrzegawczego (§6.1 „Termin”; SM-1) — brak planu awaryjnego to decyzja Autora i można ją uszanować. Brakuje jednak punktu, w którym Autor dowie się, że termin jest zagrożony. Forge mówi „8 tygodni … 1 z ~25 historii gotowa”. *Fix:* jedno zdanie, np. „jeśli do 30.11 nie działa wdrożenie, Autor wraca do tej decyzji”. Bez zmiany samej decyzji.

## Treść zamiast teatru — strong

Nie ma tu persony dla ozdoby. Znajomy ma jasno określoną funkcję: „jego konto pokazuje, że rejestracja, zatwierdzanie i oddzielenie danych działają” (§2), bez udawanej osobnej ścieżki. NFR są w większości konkretne: „nie więcej niż 10” s (NFR-3), Europe/Warsaw (NFR-7), „tylko dodają” (NFR-9), lista portów i kanałów jest w addendum. Wizja (§1) dotyczy tego produktu i nie dałoby się jej przenieść do innego PRD.

### Findings
- **medium** NFR-8 to jedyny NFR bez progów (§5 NFR-8) — „rejestracja i logowanie mają ograniczenie liczby prób”. Dla logowania próg jest w FR-18 (5 prób / 15 min), dla rejestracji nie ma żadnej liczby. *Fix:* podać limit rejestracji (np. „N rejestracji na adres IP na godzinę”) albo odesłać do FR-18 i wskazać limit dla rejestracji.
- **low** Tytuł NFR-1 to pozostałość z wersji lokalnej (§5 NFR-1) — „Brak połączeń z zewnątrz” w aplikacji publicznie dostępnej z internetu można odczytać odwrotnie, a treść mówi o połączeniach *wychodzących*. Sformułowanie „a polityka CSP na to nie pozwala” jest niejasne, bo nie wiadomo, do czego odnosi się „to”. *Fix:* „NFR-1 Brak połączeń wychodzących” oraz „polityka CSP blokuje zasoby spoza aplikacji”.

## Spójność strategiczna — adequate

Teza jest jasna: dwa równorzędne cele, narzędzie i nauka BMad (§1, §7). Nowy zakres ma zakotwiczenie: wdrożenie i CI/CD obsługuje SM-4, a konta wynikają z decyzji, że aplikacja jest dla „kilku osób”. Przeciwwskaźniki istnieją i robią swoje. SM-C1 obejmuje teraz „ręczne poprawki na serwerze”, a SM-C2 ma nowy sens.

Najsłabsze ogniwo: konta i role (FR-17–FR-24) to największy przyrost zakresu, a żaden SM ich nie waliduje. SM-1 waliduje FR-5 i FR-9–FR-12, SM-3 FR-11–FR-13, SM-4 FR-28 i FR-29. UJ-4 opisuje scenariusz ze znajomym, ale sukces nie ma kryterium. Druga sprawa: SM-C2 („nic poza zakresem ustalonym 31.10.2026 nie wchodzi przed SM-1”) w praktyce oznacza, że cały nowy epik ma powstać przed 28.12. To świadoma decyzja (K14), ale razem z brakiem planu awaryjnego czyni SM-1 najbardziej zagrożonym kryterium w dokumencie.

### Findings
- **medium** Brak kryterium sukcesu dla kont i oddzielenia danych (§7; FR-17–FR-24; §2 „Znajomy”) — teza „oddzielenie danych działa” nie ma miary, a FR-23 to wymaganie o najwyższej stawce w nowym zakresie. *Fix:* dopisać do SM-4 albo jako SM-5: „Znajomy przeszedł UJ-4 na serwerze i nie widzi żadnych danych Autora; test izolacji (addendum: zapytanie omijające filtr) jest w potoku”.

## Jasność ukończenia — adequate

Rdzeń jest wzorcowy: FR-5, FR-9, FR-11–FR-13 i FR-22 („potwierdzenie wpisaniem jego loginu”, „albo wszystko, albo nic”) dają się przełożyć na kryteria akceptacji bez interpretacji. Nowe FR mają jednak luki, które wyjdą na jaw dopiero przy pisaniu historii.

### Findings
- **high** Niepełna maszyna stanów Konta (§3 „Konto”; FR-20, FR-22) — zdefiniowano stany Oczekuje / Aktywne / Zablokowane, ale nie przejścia. Nie wiadomo, czy Konto w stanie Oczekuje można odrzucić. FR-22 pozwala usunąć tylko Zablokowane, więc jedyną drogą jest zablokowanie, a FR-20 nie mówi, czy wolno zablokować Konto Oczekuje. Nie wiadomo też, czy Administrator może zablokować lub usunąć samego siebie, co przy jedynym Administratorze odcina dostęp do ekranu Użytkownicy. *Fix:* krótka tabela przejść (Oczekuje→Aktywne, Oczekuje→Zablokowane, Aktywne↔Zablokowane, Zablokowane→usunięte) i reguła „Administrator nie może zablokować ani usunąć własnego Konta”.
- **medium** Brak reguł hasła i sesji (FR-17, FR-21, FR-24; NFR-8) — aplikacja jest publiczna, a PRD nie podaje minimalnej długości hasła ani czasu życia sesji logowania. Identity ma domyślne wartości, ale wtedy wymagania ustali przypadkowa konfiguracja, a nie Autor. *Fix:* jedno zdanie w NFR-8, np. „hasło min. 12 znaków, bez innych reguł; sesja wygasa po N dniach bez aktywności”, albo jawnie „domyślne ustawienia ASP.NET Core Identity”.
- **medium** FR-28 nie mówi, co po nieudanym sprawdzeniu stanu (§4.8 FR-28) — „nieudane sprawdzenie kończy wdrożenie błędem widocznym dla Autora”. Nie wiadomo, czy serwer zostaje z nową, zepsutą wersją (i trzeba ręcznie użyć FR-29), czy wraca automatycznie do poprzedniej. Od tego zależy, czy aplikacja może leżeć w środku miesiąca. Nie wiadomo też, co oznacza „sprawdza swój stan” (addendum: `/health`, bez treści). *Fix:* dopisać „wersja pozostaje / poprzednia wersja jest przywracana automatycznie” oraz minimum, które sprawdza `/health` (np. „aplikacja odpowiada i łączy się z bazą”).
- **low** FR-25 „zgodną z jego aktualnym działaniem” — samo w sobie trudne do zweryfikowania. Ratuje je reguła Definition of Done w addendum. *Fix:* przenieść tę regułę do FR-25 jako kryterium („historia zmieniająca ekran zmienia jego stronę pomocy”).
- **low** FR-18: komunikat o stanie Konta (§4.6 FR-18) — nie wiadomo, czy komunikat „Oczekuje/Zablokowane” pojawia się dopiero po poprawnym haśle, czy dla samego loginu. To drobna kwestia ujawniania informacji przy publicznym adresie. *Fix:* „po poprawnym haśle”.

## Uczciwość zakresu — strong

§6 został przepisany pod nową rzeczywistość: „Nie jest to narzędzie zespołowe”, „Brak e-maila … bez samodzielnego resetu hasła”, „Brak … osobnego środowiska przedprodukcyjnego”. §6.1 to dobrze napisana sekcja ryzyk przyjętych świadomie. Założenia zostały potwierdzone i opisane w §8. Brakuje tylko kilku drobnych wyłączeń, które czytelnik mógłby przyjąć milcząco.

### Findings
- **medium** Ćwiczenie odtworzenia Kopii serwera koliduje z danymi produkcyjnymi (§7 SM-4; §6.1; NFR-4) — SM-4 wymaga „odtworzenie z Kopii serwera przećwiczone co najmniej raz”, a §6.1 mówi, że odtworzenie „cofa cały serwer, wszystkim Userom naraz”. Nie ma środowiska przedprodukcyjnego (NFR-4). Ćwiczenie na produkcji kosztuje dane od ostatniej kopii, a PRD nie mówi, czy chodzi o odtworzenie na nowy Droplet, czy ćwiczenie przed pierwszymi prawdziwymi danymi. *Fix:* doprecyzować w SM-4: „na nowy Droplet z kopii, bez zastępowania produkcji” albo „przed SM-2, gdy na serwerze nie ma prawdziwych danych”.
- **low** Brakujące wyłączenia dla ról (§6) — PRD nie mówi, że nie ma nadawania roli Administrator innym Kontom, kilku Administratorów ani samodzielnego usunięcia Konta przez Usera. Wynika to z FR-19 i FR-22, ale tylko pośrednio. *Fix:* jedna linia w §6.

## Użyteczność dla kolejnych kroków — strong

Słownik pojęć (§3) jest kompletny i ma moc wiążącą (§0: „Terminy z §3 Pojęcia obowiązują dosłownie”). Nowe pojęcia (User, Administrator, Konto ze stanami, Kopia serwera) są używane spójnie w FR-17–FR-29 i w §6.1. Numeracja FR-1–FR-29 jest ciągła, a usunięty FR-14 oznaczono zgodnie z regułą z §0. Wszystkie odsyłacze (FR-13↔FR-24, FR-20↔FR-21/22, FR-9↔FR-11/16, SM-4↔FR-28/29, §4 → FR-23) rozwiązują się poprawnie. Każda UJ ma bohatera. Sofia może cytować FR bez przeróbek. Uwagi mechaniczne są niżej.

## Dopasowanie kształtu — strong

Kształt pasuje do skali: spec możliwości z pięcioma jednozdaniowymi UJ, a nowe UJ-4 i UJ-5 są tak samo krótkie jak dotychczasowe. PRD nie jest przeformalizowany: nie dodano persony dla znajomego (K11), techniczne szczegóły trafiły do addendum. Ponieważ PRD zasila architekturę i historie, przy kontach przydałoby się trochę więcej precyzji. To jest już opisane w „Jasności ukończenia”.

## Uwagi mechaniczne

- **Pozostałości po wersji jednoosobowej i lokalnej:** praktycznie brak. Jedyną pozostałością jest tytuł NFR-1 (wyżej). FR-15 o OneDrive/`Documents` jest nadal zasadne, bo opisuje środowisko lokalne. §3 „Odbiorca — osoba, której *Autor* wysyła Zestawienie” jest zdefiniowany tylko dla Autora, a §1 mówi o „układzie, którego oczekuje Odbiorca” dla wszystkich Userów. To akceptowalne, bo §6 wyklucza układy per Odbiorca, ale definicję można uogólnić na „osobę, której User wysyła Zestawienie”.
- **Rozjazdy w słowniku:** „Konto” jest pojęciem z §3, a w §2 („drugie konto”, „jego konto”) i UJ-4 („zakłada konto”, „zatwierdza konto”) pisane małą literą. W UJ-4 „Oczekuje na zatwierdzenie” łączy nazwę stanu z czasownikiem. „miesiąc „na serio”” w SM-1/SM-4 jest małą literą, w SM-2 „Miesiąc „na serio”” wielką. W UJ-2 „klika eksport” odnosi się do akcji, a §4.5 i NFR-6 znają ekran „Eksport”. Ekrany „Użytkownicy”, „Ustawienia konta” i „Nowy wpis” są nazwane w NFR-6, ale nie w §3. To wystarcza, choć „Użytkownicy” obok roli „User” to mieszanka polsko-angielska, warta jednego zdania w §3.
- **Kolejność numerów:** FR-16 stoi w §4.3 między FR-8 a FR-9, a FR-14 i FR-15 na końcu (§4.9). To zgodne z regułą stałych numerów z §0, nie błąd.
- **Odsyłacze nierozwiązywalne dla czytelnika:** §8 „Decyzje z 31.10.2026 (K1–K17)” i addendum „(K8)” odsyłają do identyfikatorów, które istnieją tylko w `.memlog.md`. Nie ma ich ani w forge, ani w PRD. *Fix:* usunąć identyfikatory K albo zastąpić je odsyłaczem do forge.
- **SM-3 nie da się czytać osobno:** „Zestawienie za *ten* Miesiąc” odnosi się do SM-2. *Fix:* „za Miesiąc z SM-2”.
- **Indeks założeń:** w tekście nie ma tagów `[ASSUMPTION]`, a §8 opisuje je jako potwierdzone i wpisane. Lista zgadza się z memlogiem. `.memlog.md` zawiera jednak niezamknięte założenie: „Prawdziwe dane wrześniowe nie trafiają na Droplet … Autor nie potwierdził wprost”, które w SM-1 jest już stanowczym warunkiem. Warto sprawdzić, czy Autor to potwierdził. Jeśli tak, wystarczy notatka w memlogu.
- **Wymagane sekcje:** komplet dla tej skali.
