---
review: freshness
target: architecture-rejestr-czasu.md (updated 2026-10-31)
date: 2026-10-31
verdict: PASS z poprawkami (4 do poprawienia w spine, 4 uwagi)
---

# Recenzja aktualności — spine „Rejestr czasu pracy”

Sprawdzałem, czy decyzje spine'u (Stack i nazwane API w AD) są potwierdzone w źródłach z 31.10.2026, a nie wzięte z pamięci modelu. Wpisy memloga `(version)` i `(constraint)` z 31.10 sprawdziłem wyrywkowo. Resztę zweryfikowałem w źródłach: Microsoft Learn, NuGet API, GitHub API, kod źródłowy Fluent UI Blazor v5.0.1 (tag `v5.0.1`), Dockerfile dotnet-docker, dokumentacja GitHub i DigitalOcean. Zajrzałem też do kodu historii 1.1 (`App.razor`, `Program.cs`, `*.csproj`).

## Werdykt

**PASS z poprawkami.** Wersje w tabeli Stack są aktualne: każdy pakiet to najnowsza stabilna wersja na NuGet, a każda akcja i Caddy to najnowsze wydanie. Wszystkie nazwane API istnieją w .NET 10 i Fluent UI 5. Cztery reguły zakładają jednak zachowanie, którego źródła nie potwierdzają albo które wymaga kroku, o którym spine milczy (F1–F4).

## Ustalenia do poprawy w spine

### F1 — AD-11: potwierdzenie „fokus na Anuluj” jest sprzeczne z `ShowConfirmationAsync` w Fluent UI 5 [wysoka]

- W v5.0.1 `DialogService.ShowConfirmationAsync` ustawia `PrimaryShortCut = "Enter;Y"` i `SecondaryShortCut = "Escape;N"`. Skrót obsługuje `FluentDialog` na poziomie dialogu (`ShortCutPressedAsync`), niezależnie od fokusu. Enter albo „Y” zatwierdza więc akcję główną, np. usunięcie, nawet gdy fokus stoi na Anuluj.
- `MessageBoxOptions` nie ma opcji fokusu na przycisku dodatkowym. „Fokus na Anuluj” nie jest więc funkcją biblioteki.
- Poprawka: `Shared/Confirm` używa `IDialogService.ShowMessageBoxAsync(new MessageBoxOptions { PrimaryShortCut = null /* albo bez Enter */, SecondaryShortCut = "Escape", ... })` zamiast `ShowConfirmationAsync`. Albo AD-11 przestaje obiecywać fokus na Anuluj i nazywa skrót Enter świadomie. Etykiety „Tak/Nie” z `ShowConfirmationAsync` i tak pochodzą z lokalizatora (zob. F4).
- Źródło: https://github.com/microsoft/fluentui-blazor/blob/v5.0.1/src/Core/Components/Dialog/MessageBox/DialogService.cs, https://github.com/microsoft/fluentui-blazor/blob/v5.0.1/src/Core/Components/Dialog/MessageBox/MessageBoxOptions.cs

### F2 — AD-15: `[ExcludeFromInteractiveRouting]` nie działa przy obecnym `App.razor` [wysoka]

- Atrybut wymusza tylko pełne przeładowanie strony. Statyczny render daje dopiero `App.razor`, który wybiera tryb przez `HttpContext.AcceptsInteractiveRouting() ? InteractiveServer : null` (wzorzec z dokumentacji .NET 10).
- Kod 1.1 ma na sztywno `<Routes @rendermode="InteractiveServer" />` i `<HeadOutlet @rendermode="InteractiveServer" />`. Bez zmiany strony `/konto/...` zostałyby interaktywne i nie ustawiłyby ciasteczka.
- Poprawka: dopisać do AD-15, że `App.razor` ustala `PageRenderMode` przez `AcceptsInteractiveRouting()` dla `Routes` i `HeadOutlet`.
- Źródło: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/render-modes?view=aspnetcore-10.0 (sekcja o `ExcludeFromInteractiveRouting`); `src/RejestrCzasu/Components/App.razor`.

### F3 — AD-19 / Stack: brak pakietu dla `PersistKeysToDbContext`; GHCR jest domyślnie prywatny [średnia]

- `PersistKeysToDbContext` jest w osobnym pakiecie `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` (najnowszy 10.0.14 z 25.10.2026), a w Stack go nie ma. `AppDbContext` musi też implementować `IDataProtectionKeyContext` (`DbSet<DataProtectionKey>`). Dopisać to do Stack i AD-19 lub AD-7. Źródło: https://www.nuget.org/packages/Microsoft.AspNetCore.DataProtection.EntityFrameworkCore
- GHCR: pierwszy obraz pakietu konta osobistego jest **prywatny** i nie dziedziczy widoczności publicznego repozytorium („inherits the access permissions (but not the visibility)”). Obraz publiczny można pobrać anonimowo, ale ktoś musi go raz ręcznie przełączyć na Public, i tej zmiany nie da się cofnąć. Bez tego kroku `docker compose pull` bez tokenu na Droplecie się nie uda. Dopisać do AD-20 jako ręczny krok w `docs/admin/`. Źródło: https://docs.github.com/en/packages/learn-github-packages/configuring-a-packages-access-control-and-visibility

### F4 — AD-11: „wbudowane teksty po polsku” bez własnego JS i bez własnej lokalizacji nie istnieją [średnia]

- **Komunikat ponownego łączenia Blazora.** Domyślny interfejs z `blazor.web.js` ma teksty angielskie i nie da się go zlokalizować. Polskie teksty bez JS dają się zrobić tylko własnym znacznikiem `id="components-reconnect-modal"` z CSS opartym o klasy `components-reconnect-show/-failed/-rejected/-paused`. Komponent `ReconnectModal` z szablonu .NET 10 przynosi `ReconnectModal.razor.js`, a scaffold `--empty` go nie tworzy (kodu 1.1 też nie ma). Bez JS przycisk „Ponów” (`Blazor.reconnect()`) i wznowienie po pauzie nie działają; zostaje zwykły link przeładowania. AD-11 powinien nazwać ten wariant: własny znacznik i CSS w `MainLayout`/`App`, stan `failed`/`rejected` z linkiem „Odśwież stronę”. Albo dopuścić collocated `.razor.js` z szablonu, które CSP `'self'` przepuszcza. Źródło: https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/signalr?view=aspnetcore-10.0
- **Fluent UI 5.** Pakiet ma tylko `LanguageResource.resx` po angielsku (111 kluczy), bez polskiego. Polskie teksty komponentów (DataGrid, MessageBox „Yes/No”, paginacja) wymagają własnej implementacji `IFluentLocalizer` w aplikacji. Spine powinien nazwać to miejsce (np. `Shared/FluentPolishLocalizer`). Źródło: https://github.com/microsoft/fluentui-blazor/tree/v5.0.1/src/Core/Localization

## Uwagi (bez zmiany decyzji)

- **U1 — Stack, .NET SDK 10.0.401.** Zainstalowany SDK zawiera runtime **10.0.12**. Najnowszy to SDK 10.0.403 z runtime 10.0.14, poprawką bezpieczeństwa z 25.10.2026. Pakiety w Stack są 10.0.14, a Blazilla 2.4.2 wymaga `Microsoft.AspNetCore.Components.Web >= 10.0.13`. Lokalnie warto zaktualizować SDK do 10.0.403 i przypiąć wersję w `global.json` dla `setup-dotnet`. Obraz `aspnet:10.0` i tak ma 10.0.14. Źródła: https://dotnet.microsoft.com/en-us/download/dotnet/10.0, https://www.nuget.org/packages/Blazilla
- **U2 — AD-15, `CanSignInAsync`.** API istnieje (`virtual`), ale `PreSignInCheck` woła je **przed** sprawdzeniem hasła. Dla Konta `pending`/`blocked` `PasswordSignInAsync` zwraca więc `NotAllowed` nawet przy złym haśle. Jeśli komunikat „zależy od stanu”, ujawnia on stan Konta bez znajomości hasła. Historia logowania powinna pokazywać komunikat stanu dopiero po `CheckPasswordAsync`. Wynika to z kodu `SignInManager`. Nie znalazłem oficjalnej dokumentacji, która to opisuje: do potwierdzenia testem.
- **U3 — AD-15/AD-19, limiter.** `[EnableRateLimiting]` na routowalnym komponencie Razor jest udokumentowany, pod warunkiem że `MapRazorComponents` nie wywołuje `RequireRateLimiting`. Działanie na stronach static SSR nie jest opisane wprost; to zwykłe żądania HTTP, więc powinno działać, ale wymaga testu. Partycja „według IP” wymaga `UseForwardedHeaders` **przed** `UseRateLimiter`, inaczej wszyscy mają IP Caddy. Źródło: https://learn.microsoft.com/aspnet/core/performance/rate-limit
- **U4 — AD-19, obraz i kopie.** `aspnet:10.0` (noble) ma `libicu74`, `tzdata` i `tzdata-legacy` (potwierdza memlog), działa jako `app` (UID 1654) i słucha na **8080** (`ASPNETCORE_HTTP_PORTS`). Caddyfile powinien kierować na `app:8080`. Kopie DigitalOcean: dzienne trzymane 7 dni, tygodniowe 4 tygodnie. Odtworzenie przez „Create Droplet” z kopii jest wspierane. Wolumeny Block Storage **nie** wchodzą do kopii Dropletu, więc wolumen Dockera z bazą musi leżeć na dysku Dropletu. Kopia jest migawką działającego dysku, więc PostgreSQL odtwarza się z WAL (spójność jak po awarii). Źródła: https://github.com/dotnet/dotnet-docker/blob/main/src/runtime-deps/10.0/noble/amd64/Dockerfile, https://docs.digitalocean.com/products/backups/how-to/create-and-restore/

## Potwierdzone (bez zastrzeżeń)

| Element | Stan 31.10.2026 | Źródło |
| --- | --- | --- |
| NuGet: Identity.EF 10.0.14, Npgsql EF 10.0.3, EFCore.NamingConventions 10.0.1, Fluent UI 5.0.1, FluentValidation 12.2.0, Blazilla 2.4.2, ClosedXML 0.105.1, xunit.v3 4.0.2, bUnit 2.13.3, Testcontainers.PostgreSql 4.16.0 | każda to najnowsza stabilna | api.nuget.org/v3-flatcontainer |
| Fluent UI Blazor | v5.0.0 28.09.2026, v5.0.1 14.10.2026 (memlog podaje „stabilna od 25.09” — drobna rozbieżność dat) | https://github.com/microsoft/fluentui-blazor/releases |
| AD-12: komponenty ładujące unpkg | potwierdzone w v5.0.1: `TextMasked` (IMask; używa go też `FluentNumberInput` i `MaskPattern` w `FluentTextInput`), `FluentSortableList` (SortableJS); innych ładowań z CDN brak | kod `src/Core.Scripts` v5.0.1 |
| `FluentMessageBar`, `FluentMessageBox`, `IDialogService` | istnieją w v5 | j.w. |
| `FluentDataGrid` v5 | brak wbudowanego grupowania wierszy; stałe grupowanie Tydzień → Dzień `ReportGrid` musi zbudować sam (AD-9 to dopuszcza) | j.w. |
| `RevalidatingServerAuthenticationStateProvider` | istnieje; domyślnie 30 min, `RevalidationInterval` da się nadpisać na 1 min (AD-17) | https://learn.microsoft.com/en-us/aspnet/core/blazor/security/?view=aspnetcore-10.0 |
| `IdentityUser<int>`, `IgnoreQueryFilters()`, `AddDbContextCheck`, `KnownIPNetworks` | istnieją w .NET/EF Core 10 (`KnownIPNetworks` w memlogu) | memlog, Microsoft Learn |
| GitHub Actions | checkout v7.0.1, setup-dotnet v6.0.0, login-action v4.7.0, setup-buildx-action v4.4.1, build-push-action v7.4.0, appleboy/ssh-action v1.2.5 — najnowsze | api.github.com/repos/*/releases/latest |
| Caddy | v2.11.7 (03.10.2026), tag `caddy:2.11` | https://github.com/caddyserver/caddy/releases |
| Anonimowe pobranie publicznego obrazu GHCR | potwierdzone (z zastrzeżeniem F3) | docs.github.com (j.w.) |
