# Przegląd: historia 4.2 — Nowy schemat Initial z Identity i UserId

Cel: commit 8e504f2 na gałęzi `4-2-nowy-schemat-initial`. Wszystkie bloki przejrzane..

## [x] 1. Cel (zaakceptowany; test lokalny wykonany)

Źródło: ticket 4.2 i sekcja Intent [planu](../tickets/epic-konta-i-wdrozenie/tickets/story-nowy-schemat-initial-z-identity-i-userid-plan.md).

> **Problem:** Schemat z historii 1.1 nie zna Userów: brak tabel Identity, `user_id` i oddzielenia danych, a po pierwszym wdrożeniu (historia 4.8) `Initial` zostaje zamrożony i wolno już tylko dodawać (AD-21).
>
> **Approach:** Usunąć starą migrację i wygenerować nową `Initial` z całym schematem AD-7 oraz wprowadzić warstwę oddzielenia danych z AD-16: `IUserOwned`, dwa tryby `AppDbContext` i `UserDbContextFactory` przyjmującą `UserId`. Źródło Usera (`CurrentUser`) dokłada historia 4.3.

## [x] 2. Ogólny zarys (zaakceptowany)

Zmiana to nowy schemat bazy i mechanizm, który sam dokłada `user_id` do zapytań i zapisów. Otwieraj w tej kolejności:

1. [AppDbContext.cs:52](../../../src/RejestrCzasu/Data/AppDbContext.cs#L52) — `OnModelCreating`: cały schemat (Identity, Słowniki, Wpisy).
2. [AppDbContext.cs:142](../../../src/RejestrCzasu/Data/AppDbContext.cs#L142) — `ConfigureUserOwned`: filtr, klucz obcy i token współbieżności dla każdej encji z `user_id`.
3. [AppDbContext.cs:172](../../../src/RejestrCzasu/Data/AppDbContext.cs#L172) — `ApplyUserOwnership`: co dzieje się z `user_id` przy zapisie w obu trybach.
4. [UserDbContextFactory.cs:11](../../../src/RejestrCzasu/Data/UserDbContextFactory.cs#L11) — jedyna droga do kontekstu Usera.
5. [DataIsolationTests.cs:26](../../../tests/RejestrCzasu.Tests/DataIsolationTests.cs#L26) — testy pokazujące zachowanie.

## [x] 3. Schemat i migracja (zaakceptowany)

Jedna nowa migracja zawiera cały schemat. Identity ma klucze `int` i jawne nazwy tabel, a passkeys są wyłączone wersją schematu. Słowniki mają klucz alternatywny `(user_id, id)`, a Wpis wskazuje je kluczem złożonym, więc baza nie przyjmie Wpisu z cudzym Słownikiem. Wszystkie klucze obce mają `RESTRICT`, także w tabelach Identity.

- [AppDbContext.cs:39](../../../src/RejestrCzasu/Data/AppDbContext.cs#L39) — `SchemaVersion` bez passkeys
- [AppDbContext.cs:59](../../../src/RejestrCzasu/Data/AppDbContext.cs#L59) — tabela `users` ze stanem Konta i check constraint
- [AppDbContext.cs:97](../../../src/RejestrCzasu/Data/AppDbContext.cs#L97) i [AppDbContext.cs:117](../../../src/RejestrCzasu/Data/AppDbContext.cs#L117) — klucz alternatywny i złożony klucz obcy
- [AppDbContext.cs:138](../../../src/RejestrCzasu/Data/AppDbContext.cs#L138) — `RESTRICT` wszędzie
- [20261031164517_Initial.cs:339](../../../src/RejestrCzasu/Data/Migrations/20261031164517_Initial.cs#L339) — indeks `(user_id, lower(name))` SQL-em

## [x] 4. Oddzielenie danych (zaakceptowany; zmieniony w przeglądzie: uszczelnienie, commit f54e60f)

Każda encja z `user_id` implementuje `IUserOwned`; filtr, klucz obcy do `users` i token współbieżności nakłada pętla po interfejsie. Tryb Usera ustawia tylko fabryka (`BindUser` raz), a tryb systemowy nie widzi i nie zapisuje danych Userów. Token współbieżności dokłada `user_id` do UPDATE i DELETE, żeby zapis po samym `Id` nie trafił w cudzy wiersz (poprawka z recenzji).

- [IUserOwned.cs](../../../src/RejestrCzasu/Data/IUserOwned.cs)
- [AppDbContext.cs:42](../../../src/RejestrCzasu/Data/AppDbContext.cs#L42) — `BindUser`
- [AppDbContext.cs:148](../../../src/RejestrCzasu/Data/AppDbContext.cs#L148) i [AppDbContext.cs:150](../../../src/RejestrCzasu/Data/AppDbContext.cs#L150) — token współbieżności i filtr
- [AppDbContext.cs:172](../../../src/RejestrCzasu/Data/AppDbContext.cs#L172) — `ApplyUserOwnership`
- [UserDbContextFactory.cs:9](../../../src/RejestrCzasu/Data/UserDbContextFactory.cs#L9)
- [DatabaseServiceCollectionExtensions.cs](../../../src/RejestrCzasu/Data/DatabaseServiceCollectionExtensions.cs) — jedyna rejestracja bazy, bez puli kontekstów
- [BannedSymbols.txt](../../../src/RejestrCzasu/BannedSymbols.txt) — zakazane API omijające filtr (błąd kompilacji)
- [20261031164517_Initial.cs](../../../src/RejestrCzasu/Data/Migrations/20261031164517_Initial.cs) — trigger blokujący zmianę `user_id`
- [DataIsolationGuardTests.cs](../../../tests/RejestrCzasu.Tests/DataIsolationGuardTests.cs) — testy strażników

## [x] 5. Testy (zaakceptowany)

Fixture tworzy fikcyjnych Userów i kontekst Usera przez prawdziwą fabrykę oraz czyści wszystkie tabele. Testy pokrywają każdy wiersz tabeli przypadków z planu oraz zapis stubem z cudzym `Id`.

- [PostgresFixture.cs:70](../../../tests/RejestrCzasu.Tests/PostgresFixture.cs#L70), [PostgresFixture.cs:74](../../../tests/RejestrCzasu.Tests/PostgresFixture.cs#L74), [PostgresFixture.cs:91](../../../tests/RejestrCzasu.Tests/PostgresFixture.cs#L91)
- [DataIsolationTests.cs](../../../tests/RejestrCzasu.Tests/DataIsolationTests.cs)
- [SchemaTests.cs](../../../tests/RejestrCzasu.Tests/SchemaTests.cs)

## [x] 6. Peryferia (zaakceptowany)

- [README.md:92](../../../README.md#L92) — wolumen od nowa i odświeżenie `appsettings.Local.json`
- [RejestrCzasu.csproj](../../../src/RejestrCzasu/RejestrCzasu.csproj) — pakiety Identity i DataProtection, `InternalsVisibleTo`
- [appsettings.json](../../../src/RejestrCzasu/appsettings.json), [appsettings.Local.example.json](../../../src/RejestrCzasu/appsettings.Local.example.json) — bez `Urls`, `Backup`, `Export`, `ContainerName`
- [DatabaseStartup.cs](../../../src/RejestrCzasu/Startup/DatabaseStartup.cs) — komentarze AD-21
- usunięte: `Options/BackupOptions.cs`, `Options/ExportOptions.cs`
