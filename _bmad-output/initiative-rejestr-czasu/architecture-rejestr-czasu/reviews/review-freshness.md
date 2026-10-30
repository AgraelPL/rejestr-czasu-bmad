---
review: freshness (aktualność technologii)
target: architecture-rejestr-czasu.md (+ .memlog.md)
date: 2026-10-30
method: NuGet API (v3-flatcontainer, nuspec), metadane wydań .NET, źródła repozytoriów GitHub (gałąź dev = v5), pakiet nupkg Fluent UI 5.0.1 przeskanowany lokalnie, `dotnet new blazor -h` na SDK 10.0.401, dokumentacja Microsoft Learn / npgsql.org / Docker Hub / bunit.dev
---

# Przegląd aktualności technologii — Rejestr czasu pracy

## Werdykt

Wszystkie wybrane technologie istnieją, są w wersjach stabilnych i pasują do .NET 10. Spine ma jednak dwie poważne luki, których nie wykazałaby sama kontrola numerów wersji. Fluent UI 5.0.1 pobiera biblioteki JS z unpkg.com, co łamie AD-11. AD-8 nie wskazuje pakietu integracji walidacji, a najpopularniejszy z nich jest zarchiwizowany. Do tego dochodzi kilka faktów, które spine podaje błędnie lub tylko domyślnie: snake_case, ścieżka wolumenu postgres:18 i domyślne ustawienia szablonu.

## Ustalenia (od najpoważniejszych)

### F1 — WYSOKA — AD-11, AD-1: Fluent UI v5 ładuje skrypty z CDN (unpkg.com)

- **Fakt:** plik `Microsoft.FluentUI.AspNetCore.Components.lib.module.js` w pakiecie 5.0.1 (zeskanowany nupkg) ma wbudowany `ExternalLibraryLoader` z adresami:
  - `https://unpkg.com/imask@7.6.1/dist/imask.min.js`: ładowany przy **pierwszym renderze każdego `FluentNumberInput`** (`OnAfterRenderAsync` → `ApplyNumberMaskAsync` → `TextMasked.applyNumberMask` → `await r0.load()`) oraz przez `FluentTextInput` z `MaskPattern`;
  - `https://unpkg.com/sortablejs@1.15.6/Sortable.min.js`: ładowany przez `FluentSortableList`.
- Opiekun biblioteki potwierdził, że to zamierzone zachowanie i że nie planuje dołączać tych plików do pakietu. Zalecane obejście to skopiować `imask.min.js` do `wwwroot` i wczytać go wcześniej tagiem `<script>`. Loader sprawdza `window.IMask` i wtedy nie sięga do CDN.
- **Skutek:** AD-11 („brak skryptów … z internetu”) jest łamane po cichu, gdy tylko formularz Wpisu lub Typu pracy użyje pola liczbowego. Do tego bez sieci pole liczbowe daje kilkusekundowe opóźnienie (issue #4939). AD-1 („bez JavaScriptu poza tym, co dostarcza Fluent UI”) też nie da się utrzymać dosłownie, bo albo dołączamy lokalnie IMask, albo zakazujemy `FluentNumberInput` i masek.
- **Propozycja:** dopisać do AD-11 jedną z dwóch zasad: (a) zakaz `FluentNumberInput`, `MaskPattern` i `FluentSortableList` (Czas jako tekst `h:mm` w `FluentTextInput` bez maski, walidowany przez FluentValidation); albo (b) dołączyć `imask@7.6.1` do `wwwroot/lib` i wczytać go w `App.razor` jako wyjątek nazwany w AD-1. W obu wariantach warto dodać nagłówek CSP `script-src 'self'`, który egzekwuje AD-11 i ujawnia regresję w przeglądarce. Wersję IMask trzeba sprawdzać przy każdej aktualizacji Fluent UI.
- **Źródła:**
  - https://github.com/microsoft/fluentui-blazor/issues/4939
  - https://github.com/microsoft/fluentui-blazor/pull/4503
  - https://github.com/microsoft/fluentui-blazor/pull/4506
  - https://raw.githubusercontent.com/microsoft/fluentui-blazor/dev/src/Core/Components/NumberInput/FluentNumberInput.razor.cs
  - https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/5.0.1

### F2 — WYSOKA — AD-8: integracja FluentValidation z formularzem nie jest wskazana, a „oczywisty” pakiet jest martwy

- **Fakt:** `Blazored.FluentValidation` zarchiwizowano 26.12.2025. Wszystkie jego wersje na NuGet są wycofane z listy (`listed:false`). Ostatnia wersja 2.2.0 zależy od FluentValidation ≥ 11.9.1 i celuje najwyżej w `net8.0`. W README Blazored jako następcę wskazuje **Blazilla** (loresoft). Blazilla 2.4.2 (13.10.2026) celuje w `net10.0` i zależy od `FluentValidation 12.2.0`. Dokumentacja FluentValidation też oznacza Blazored jako „(Archived)” i wymienia Blazilla jako pierwszą.
- **Skutek:** AD-8 mówi tylko „podpięty do formularza Fluent UI”. Agent z wiedzą treningową prawie na pewno doda `Blazored.FluentValidation`, a to pakiet bez wsparcia i z zależnością od FV 11.
- **Propozycja:** dopisać do AD-8 i tabeli Stack: `Blazilla 2.4.x` (`<FluentValidator />` w `EditForm`). Alternatywa to własny komponent walidatora (ok. 50 linii) w `Shared/`. Komponenty formularzy Fluent UI v5 (`FluentTextInput` itd.) dziedziczą po `InputBase`/`EditContext`, więc współpracują z każdą integracją opartą na `EditContext`. Wyświetlanie: `FluentValidationMessage` / `FluentValidationSummary` są w v5.
- **FluentValidation 12.2.0:** potwierdzone. Pakiet celuje tylko w `net8.0` (netstandard porzucony w 12.0) i działa na net10. Notatka w memlogu jest poprawna.
- **Źródła:**
  - https://github.com/Blazored/FluentValidation
  - https://www.nuget.org/packages/Blazored.FluentValidation
  - https://github.com/loresoft/Blazilla
  - https://www.nuget.org/packages/Blazilla
  - https://docs.fluentvalidation.net/en/latest/blazor.html
  - https://www.nuget.org/packages/FluentValidation

### F3 — ŚREDNIA — AD-2, AD-3, Structural Seed: obraz `postgres:18` zmienił wolumen; uwagi do `docker exec pg_dump`

- **Fakt (wolumen):** od wersji 18 obraz ma `PGDATA=/var/lib/postgresql/18/docker` i `VOLUME /var/lib/postgresql`. Docker Hub podaje to wprost: wolumen montuje się w `/var/lib/postgresql`. Jeśli w compose zostanie powszechny wzorzec `- dane:/var/lib/postgresql/data`, entrypoint wykrywa „unused mount/volume” i **kontener nie startuje** (`docker-entrypoint.sh`, sprawdzenie `OLD_DATABASES`).
- **Fakt (pg_dump przez docker exec):** podejście jest poprawne. `pg_dump` w tym samym kontenerze ma tę samą wersję główną co serwer, a `initdb` w obrazie nie dostaje `--auth-local`, więc połączenie przez gniazdo lokalne jest `trust` i hasło nie jest potrzebne. Do doprecyzowania:
  1. `docker exec` **bez** `-t` (TTY zamienia LF na CRLF i psuje zrzut). Strumień stdout kopiować binarnie (`Process.StandardOutput.BaseStream`) do pliku tymczasowego i dopiero po kodzie wyjścia 0 i niepustym pliku zmieniać nazwę na docelową. Pasuje to do rotacji z AD-3. Alternatywa: `pg_dump -f /tmp/x.sql` w kontenerze, potem `docker cp`.
  2. Nazwa kontenera: Compose nadaje nazwę `<projekt>-<usługa>-1`, chyba że w compose jest `container_name`. AD-12 trzyma nazwę w konfiguracji, więc compose powinien ją ustawić jawnie.
  3. Aplikacja wymaga `docker` w PATH procesu. **Na komputerze Autora (stan 30.10.2026) `docker` nie jest w PATH, a `C:\Program Files\Docker` nie istnieje**, czyli Docker Desktop nie jest zainstalowany. To warunek wstępny pierwszego epiku.
  4. Tag: na Docker Hub `latest` to dziś 18.6. Po wydaniu PG 19 (obecnie RC1 z 15.10.2026) `latest` przeskoczy na 19, więc compose musi wskazywać `postgres:18` (albo `18.6`), nigdy `latest`.
- **Źródła:**
  - https://hub.docker.com/_/postgres
  - https://raw.githubusercontent.com/docker-library/postgres/master/18/trixie/Dockerfile
  - https://raw.githubusercontent.com/docker-library/postgres/master/18/trixie/docker-entrypoint.sh
  - https://www.postgresql.org/support/versioning/

### F4 — ŚREDNIA — Consistency Conventions („snake_case … konwencja Npgsql”), AD-7: dwa fakty o EF Core / Npgsql

- **snake_case nie jest domyślny w Npgsql.** Dokumentacja Npgsql mówi, że EF Core mapuje nazwy dokładnie według klas i właściwości (`TimeEntry` → `"TimeEntry"`). snake_case wymaga zewnętrznego pakietu `EFCore.NamingConventions` (`.UseSnakeCaseNamingConvention()`). Aktualna wersja 10.0.1 celuje w net10.0 i EF Core [10.0.1, 11). Pakiet trzeba dopisać do tabeli Stack, inaczej agenci wygenerują tabele w PascalCase z cudzysłowami.
- **Indeks na `lower(name)` (AD-7):** EF Core 10 / Npgsql 10 nie mają natywnego API do indeksów wyrażeniowych w `HasIndex`. Do wyboru: `migrationBuilder.Sql("CREATE UNIQUE INDEX … (lower(name))")` w migracji, kolumna `citext`, niedeterministyczna kolacja ICU (`UseCollation`) albo pakiet zewnętrzny (EFCore.ComplexIndexes.PostgreSQL). Spine powinien wskazać jeden sposób, bo to właśnie „dwa sposoby wykrywania duplikatów”, którym AD-7 ma zapobiegać.
- **Wersje:** Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 to najnowsza 10.x (wymaga EF Core ≥ 10.0.4; linia 11.x jest w RC3). EF Core 10.0.14. `DateOnly` → `date` natywnie. Zgodne z memlogiem.
- **Źródła:**
  - https://www.npgsql.org/efcore/modeling/tables.html
  - https://www.nuget.org/packages/EFCore.NamingConventions
  - https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL
  - https://www.nuget.org/packages/EFCore.ComplexIndexes.PostgreSQL/

### F5 — ŚREDNIA — AD-1, AD-2: domyślne ustawienia szablonu `dotnet new blazor` (.NET 10) nie dają tego, co zakłada spine

Zweryfikowane na żywo: `dotnet new blazor -h`, SDK 10.0.401.

| Opcja | Domyślnie | Czego wymaga spine |
| --- | --- | --- |
| `-int, --interactivity` | `Server` | `Server` (OK) |
| `-ai, --all-interactive` | **`false`** (interaktywność per strona/komponent) | `true` (AD-1: „Interactive Server ustawionym globalnie”) |
| `-e, --empty` | **`false`** (strony przykładowe + **Bootstrap**) | `true` (AD-1: „bez innych bibliotek UI”) |
| `--no-https` | **`false`** (profil https, `UseHttpsRedirection`, HSTS) | `true` (Structural Seed: `http://127.0.0.1`; HTTPS poza v1) |

- **Polecenie:** `dotnet new blazor -n RejestrCzasu --interactivity Server --all-interactive --empty --no-https`. Przy `--all-interactive` tryb renderowania trafia na `<Routes @rendermode="InteractiveServer" />` i `<HeadOutlet>` w `App.razor`.
- **Nasłuch:** `launchSettings.json` z szablonu używa `http://localhost:<port>`. Kestrel wiąże wtedy loopback IPv4 **i** IPv6 (`[::1]`). To nadal lokalnie, ale AD-2 mówi „tylko 127.0.0.1”, więc trzeba jawnie ustawić `http://127.0.0.1:<port>` (`Kestrel:Endpoints` w appsettings albo `applicationUrl`).
- **Alternatywny starter:** istnieje `Microsoft.FluentUI.AspNetCore.Templates` 5.0.1 (szablony `fluentblazor`) z gotowym układem Fluent UI v5. Jeśli scaffold go użyje, trzeba sprawdzić jego domyślne ustawienia tak samo (nie weryfikowano, pakiet nie był instalowany).
- **Źródła:**
  - https://learn.microsoft.com/en-us/aspnet/core/blazor/tooling?view=aspnetcore-10.0&pivots=cli
  - https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-new-sdk-templates#blazor
  - https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Templates

### F6 — ŚREDNIA — AD-11 („telemetria .NET wyłączona”), AD-2 („potem `dotnet run`”)

- **Fakt:** uruchomiona aplikacja nie wysyła telemetrii SDK. Dokumentacja wprost mówi, że telemetria *nie* jest zbierana przy `dotnet <app>.dll`. **Jest** natomiast zbierana przy poleceniach CLI, w tym `dotnet run`, którego AD-2 używa jako sposobu uruchamiania. Od SDK 10.0.100 `dotnet run` wysyła m.in. zahashowany identyfikator projektu, liczbę PackageReference i profil uruchomienia. Wysyłane są też: adres IP skrócony do 3 oktetów, OS i hash MAC.
- `DOTNET_CLI_TELEMETRY_OPTOUT` działa tylko jako zmienna środowiskowa powłoki lub użytkownika. Ustawienie jej w `launchSettings.json` czy `appsettings` nic nie da, bo dotyczy to procesu aplikacji, nie CLI. **Na komputerze Autora zmienna nie jest ustawiona** (ani User, ani Machine).
- Inne połączenia w czasie developmentu, których spine nie rozstrzyga: restore z nuget.org, sprawdzanie aktualizacji szablonów przez `dotnet new` (`--no-update-check`), pobieranie obrazów przez Testcontainers z Docker Hub (`postgres:18` i kontener Ryuk), telemetria VS / VS Code. Proponowane doprecyzowanie AD-11: „zero połączeń wychodzących *działającej aplikacji*; narzędzia deweloperskie: `DOTNET_CLI_TELEMETRY_OPTOUT=1` ustawione jako zmienna użytkownika Windows (krok README)”.
- **Źródło:** https://learn.microsoft.com/en-us/dotnet/core/tools/telemetry

### F7 — NISKA — AD-9: założenie „filtry wartości i grupowanie trzeba zbudować” jest prawdziwe, ale v5 daje gotowe elementy

- **Fakt:** `FluentDataGrid` w v5 (gałąź `dev` = linia 5.x) **nie ma** wbudowanego filtra wartości ani grupowania. Kolumna ma `ColumnOptions` (dowolny `RenderFragment` w menu kolumny z ikoną Filter) i `Filtered` (wskaźnik w nagłówku). Filtrowanie robi się samemu na `Items`/`ItemsProvider`. Grid ma za to wiersze hierarchiczne: `HierarchicalGridItem<TItem,TGridItem>`, `IHierarchicalGridItem`, `TemplateColumn HierarchicalToggle="true"`, `ExpandAllHierarchicalRowsAsync()`/`CollapseAllHierarchicalRowsAsync()`, `HierarchicalSelectColumn`. Społeczność używa ich właśnie do grupowania (issue #4913). Sortowanie z hierarchią poprawiono w RC5 (PR #4982).
- **Propozycja:** AD-9 może wskazać, że stałe grupowanie Tydzień → Dzień w `ReportGrid<T>` buduje się na wierszach hierarchicznych v5. Wiersze-rodzice to Tydzień i Dzień z sumą z `MonthReport`, a filtr wartości siedzi w `ColumnOptions`. Zakres AD-9 się nie zmienia, ale maleje ryzyko, że ktoś napisze własny renderer grupowania. Uwaga: otwarte zgłoszenie #5487 opisuje wielokrotne wywołania `ItemsProvider` przy `RefreshDataAsync(true)`, co dotyczy przycisku Odśwież.
- **Źródła:**
  - https://raw.githubusercontent.com/microsoft/fluentui-blazor/dev/src/Core/Components/DataGrid/Columns/ColumnBase.razor.cs
  - https://github.com/microsoft/fluentui-blazor/tree/dev/src/Core/Components/DataGrid
  - https://github.com/microsoft/fluentui-blazor/issues/4913
  - https://github.com/microsoft/fluentui-blazor/issues/5487
  - https://dvoituron.com/2026/09/25/fluentui-blazor-5-0-0/

### F8 — NISKA — AD-10: nazwy komponentów w v5 potwierdzone, ale API różni się od v4

- `FluentMessageBar` istnieje w v5 (`Intent`, `Title`, `AllowDismiss`, `ActionsTemplate`) razem z `FluentMessageBarProvider`. Dostawcy usług są w v5 skonsolidowani w `<FluentProviders />`.
- MessageBox w v5 to `FluentMessageBox` (folder `Dialog/MessageBox`), wywoływany przez `IDialogService`: `ShowConfirmationAsync(message, title, primaryButton, secondaryButton)`, `ShowErrorAsync`, `ShowWarningAsync`, `ShowInfoAsync`, `ShowSuccessAsync`, `ShowMessageBoxAsync(MessageBoxOptions)`. W v5 **zwracają `Task<DialogResult>`**, a w v4 zwracały `IDialogReference`, na którego `Result` trzeba było czekać. Agent z wiedzą o v4 napisze kod, który się nie skompiluje.
- **Propozycja:** w AD-10 dopisać „MessageBox = `IDialogService.ShowConfirmationAsync` (v5, zwraca `DialogResult`)”, a w README epiku 1 wskazać przewodnik migracji v4 → v5 jako lekturę obowiązkową dla agentów.
- **Źródła:**
  - https://raw.githubusercontent.com/microsoft/fluentui-blazor/dev/src/Core/Components/Dialog/MessageBox/IDialogService.cs
  - https://github.com/microsoft/fluentui-blazor/tree/dev/src/Core/Components/MessageBar
  - https://v5.fluentui-blazor.net/Migration
  - https://zenn.dev/tomokusaba/articles/29e7615e6ff4ae?locale=en

### F9 — NISKA — tabela Stack: wersje do przypięcia i nieścisłości w memlogu

| Pozycja | W spine/memlogu | Stan na 30.10.2026 | Uwagi |
| --- | --- | --- | --- |
| .NET | 10 (LTS) | 10.0.14 runtime / SDK 10.0.403 (25.10.2026); LTS, EOL 14.11.2028 | Autor ma SDK 10.0.401. .NET 11 jest w RC3 („go-live”, STS), GA w listopadzie, więc wybór 10 LTS jest słuszny. Warto przypiąć SDK w `global.json` (`rollForward: latestFeature`), żeby preview 11 nie przejęło builda. |
| Fluent UI Blazor | „najnowsza stabilna 5.x; data niepotwierdzona” | **5.0.0 GA 25.09.2026, 5.0.1 14.10.2026**; TFM net8/9/10/11 | Notatkę w memlogu można zamknąć: przypiąć 5.0.1. |
| FluentValidation | 12.2.x | 12.2.0, TFM net8.0 | OK (patrz F2). |
| EF Core / Npgsql | 10.0.x (10.0.3) | EF Core 10.0.14; Npgsql EF 10.0.3 (wymaga EF ≥ 10.0.4) | OK; dopisać EFCore.NamingConventions 10.0.1 (F4). |
| PostgreSQL | 18.x (18.6) | 18.6, EOL 14.11.2030 | OK. Memlog: „19 dopiero GA październik 2026” jest nieścisłe: 19 ma **RC1 z 15.10.2026**, bez GA. Wybór 18 nadal słuszny. |
| ClosedXML | 0.105.x | 0.105.1 (25.07.2026), netstandard2.0/2.1 | Działa na net10. Zależności: DocumentFormat.OpenXml [3.1.1, 4.0), SixLabors.Fonts (autodopasowanie kolumn; na Windows bez problemu). Licencja MIT. |
| xUnit | „najnowsze stabilne” | **`xunit` 2.9.3 (linia v2) vs `xunit.v3` 4.0.2 (09.10.2026)** | Niejednoznaczne: nazwa „xUnit” pasuje do dwóch pakietów. Wskazać `xunit.v3`. |
| bUnit | „najnowsze stabilne” | 2.13.3, TFM net8/9/10/11 | Zgodny z .NET 10. Linia 2.x ma zmiany łamiące względem 1.x (`TestContext` → `BunitContext`, `RenderComponent` → `Render`, pakiety `bunit.core`/`bunit.web` scalone w `bunit`), a agenci często piszą API 1.x. Testy komponentów Fluent UI wymagają `Services.AddFluentUIComponents()` i `JSInterop.Mode = Loose`. |
| Testcontainers.PostgreSql | „najnowsze stabilne” | 4.16.0 (08.10.2026); dla xunit.v3: `Testcontainers.XunitV3` 4.16.0 | OK. W testach wskazać obraz `postgres:18` jawnie (`WithImage`), a nie domyślny tag modułu. |

- **Źródła:**
  - https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json
  - https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json
  - https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components
  - https://github.com/microsoft/fluentui-blazor/releases
  - https://www.nuget.org/packages/ClosedXML
  - https://www.nuget.org/packages/xunit.v3
  - https://www.nuget.org/packages/bunit
  - https://bunit.dev/docs/migrations/1to2.html
  - https://www.nuget.org/packages/Testcontainers.PostgreSql
  - https://www.postgresql.org/support/versioning/

## Co potwierdzono bez zastrzeżeń

- .NET 10 to bieżące LTS, a Blazor Web App z Interactive Server jest wspierany (szablon `blazor`, `--interactivity Server` domyślnie).
- Fluent UI Blazor 5.x jest stabilny (GA), celuje w net10 i ma `FluentDataGrid`, `FluentMessageBar`, `FluentMessageBox`/`IDialogService`, `FluentLayout` (belka górna i menu boczne), przełącznik trybu ciemnego oraz zasoby statyczne w `_content` (poza F1).
- Czcionka domyślna Fluent UI v5 to `'Segoe UI', … sans-serif` (lokalna, bez zewnętrznych webfontów), zgodnie z AD-11.
- Npgsql 10 / EF Core 10 / PostgreSQL 18 to zgodna trójka; `DateOnly` jest wspierany natywnie.
- `docker exec … pg_dump` z obrazem `postgres:18` jest poprawnym mechanizmem kopii (z uwagami z F3).
- ClosedXML 0.105 działa na .NET 10.
