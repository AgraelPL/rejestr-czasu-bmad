# Przegląd aktualności — przejęcie wzorca eod-ui (zmiana 02.11.2026)

- **Spine:** `architecture-rejestr-czasu.md` (AD-1, AD-9, AD-11, AD-12, tabela Stack)
- **Data przeglądu:** 02.11.2026
- **Metoda:** NuGet API (flatcontainer, rejestracja, nuspec), GitHub API (wydania), README actions/checkout v7.0.1 i docker/build-push-action, dokumentacja Docker o kontekście budowania, metadane wydań .NET 10, lokalny klon eod-ui (`8e66aea`) oraz **próbne budowanie** aplikacji testowej bez CPM z `ProjectReference` do `ui/src/Eod.Platform.UI` na SDK 10.0.403.

## Werdykt

Wersje w decyzjach są aktualne i sprawdzone: Fluent UI 5.0.1, SDK 10.0.403, runtime 10.0.14, checkout v7, `submodules: recursive`. `ProjectReference` z repo bez CPM do biblioteki na CPM **działa** (sprawdzone budowaniem). Spine opisuje jednak pobieranie podmodułu w CI i w Dockerze tylko jednym zdaniem: „CI i `Dockerfile` pobierają podmoduły”. To zdanie pomija dwie realne przeszkody, a obie zablokują pierwszy build:

1. **eod-ui jest repozytorium prywatnym.** Anonimowe `GET api.github.com/repos/AgraelPL/eod-ui` zwraca 404, a `git ls-remote` działa tylko z poświadczeniami Autora. `GITHUB_TOKEN` nie ma dostępu do innego repozytorium.
2. **Dockerfile musi skopiować pliki `Directory.*.props` z katalogu `ui/`.** Bez nich restore kończy się błędem NU1201 (sprawdzone).

## Ustalenia

### F1 — WYSOKA: prywatny podmoduł nie pobierze się w CI z domyślnym tokenem

- **Fakt:** anonimowe zapytanie o `AgraelPL/eod-ui` w API GitHuba zwraca `Not Found`, więc repozytorium jest prywatne albo niewidoczne publicznie. README actions/checkout v7.0.1 mówi, że „`${{ github.token }}` is scoped to the current repository”. Do innego prywatnego repozytorium potrzebny jest PAT (fine-grained, `contents: read` na eod-ui) albo klucz wdrożeniowy, przekazany przez `token:` lub `ssh-key:`. Bez tego `submodules: recursive` przerwie checkout.
- **Dotyczy:** AD-1 („CI … pobierają podmoduły”), AD-20, Stack (actions/checkout v7 z `submodules: recursive`), AD-13 (lista sekretów potoku: dziś tylko klucz SSH, host i użytkownik).
- **Poprawka:** dopisać do AD-1, AD-20 i AD-13 sekret, np. `EOD_UI_TOKEN` (fine-grained PAT tylko do odczytu eod-ui) albo deploy key, przekazany jako `token:` w `actions/checkout` w obu workflow. Ewentualnie upublicznić eod-ui i zapisać to jako decyzję. Jednorazowe utworzenie sekretu opisać w `docs/admin/`.

### F2 — WYSOKA: Dockerfile musi skopiować `ui/Directory.Build.props` i `ui/Directory.Packages.props`

- **Fakt (sprawdzone budowaniem):** biblioteka bierze `TargetFramework=net10.0` i `ManagePackageVersionsCentrally=true` z `ui/Directory.Build.props`, a wersje pakietów z `ui/Directory.Packages.props`. Jej csproj nie ma ani TFM, ani wersji. Gdy skopiuje się sam katalog `ui/src/Eod.Platform.UI/`, a tak wygląda dziś wzorzec warstw w `Dockerfile` (osobno `COPY` csproj, `restore`, `COPY` źródeł), restore kończy się błędem `NU1201: Projekt Eod.Platform.UI nie obsługuje żadnych docelowych platform`.
- Obecny `Dockerfile` kopiuje tylko `src/RejestrCzasu/`, więc trzeba go zmienić. Przy kontekście lokalnym (`context: .` po checkoucie z podmodułami) pliki podmodułu **są** w kontekście. Dokumentacja Docker: „all subdirectories are included”. `.dockerignore` z wzorcem `.git` wyklucza tylko `.git` w katalogu głównym, nie plik `ui/.git`, a to nie przeszkadza.
- **Poprawka:** zapisać w AD-20 (lub w strukturze) kolejność warstw: `COPY ui/Directory.Build.props ui/Directory.Packages.props ui/` → `COPY ui/src/Eod.Platform.UI/Eod.Platform.UI.csproj …` → `restore` → `COPY ui/src/Eod.Platform.UI/ …`. Do `.dockerignore` dodać `ui/docs`, `ui/tools`, `ui/src/Eod.Demo` i `ui/src/Eod.Platform.UI.Syncfusion`, bo same zrzuty w `docs/ekrany` mają 14 MB.

### F3 — ŚREDNIA: build obrazu w CI — kontekst Git a kontekst ścieżki nie są rozstrzygnięte

- **Fakt:** docker/build-push-action bez `context` używa **kontekstu Git**. BuildKit klonuje wtedy repozytorium sam, a według dokumentacji Docker „recursively clones the repository and any submodules it contains”. Domyślny `github-token` obejmuje jednak tylko bieżące repozytorium. Do prywatnego podmodułu potrzebny jest sekret `GIT_AUTH_TOKEN` w `secrets:` akcji. README akcji opisuje `GIT_AUTH_TOKEN` dla „another private repository”, ale nie mówi wprost o podmodułach, więc **tego nie potwierdzono**.
- **Poprawka:** zapisać w AD-20 kontekst ścieżki: `actions/checkout` z `submodules: recursive` i tokenem z F1, potem `build-push-action` z `context: .`. To jedna ścieżka uwierzytelnienia i jest zgodna z lokalnym `docker build .`. Jeśli wybierzecie kontekst Git, przekażcie `GIT_AUTH_TOKEN` i sprawdźcie to na pierwszym przebiegu.

### F4 — NISKA: dwie wersje Fluent UI w jednym buildzie (5.0.0 w bibliotece, 5.0.1 w aplikacji)

- **Fakt (sprawdzone budowaniem):** restore aplikacji bez CPM rozwiązuje Fluent i Icons **5.0.1**: zależność bezpośrednia wygrywa z przechodnią 5.0.0 z `ProjectReference`, bez NU1605 i bez ostrzeżeń. Sama biblioteka kompiluje się w swoim `obj/` przeciw **5.0.0**, a w `bin/` aplikacji ląduje 5.0.1. To działa, bo 5.0.1 jest wydaniem poprawkowym: „What's Changed” zawiera tylko poprawki TimePicker, Select, popover, tabs, Overlay, DataGrid i dokumentacji. CPM z `ui/` nie przeszkadza repo bez CPM, bo MSBuild szuka `Directory.*.props` w górę od katalogu projektu i najpierw trafia na te w `ui/`.
- **Ryzyko:** gdyby aplikacja kiedyś miała **niższą** wersję niż wzorzec, restore zgłosi NU1605 (downgrade). Gdyby wprowadziła własne `Directory.Packages.props` w katalogu głównym, biblioteka i tak użyje bliższego pliku z `ui/`.
- **Poprawka:** dopisać do AD-1 regułę „wersja Fluent w aplikacji ≥ wersja w przypiętym commicie eod-ui; podbicie wersji idzie najpierw we wzorcu”. Zadanie podbicia eod-ui do 5.0.1 (już w Stack) zostawić jako warunek przed epikiem 7.

### F5 — NISKA: AD-12 zależy od zmiany w eod-ui, której w `8e66aea` jeszcze nie ma

- **Fakt:** `EodHead.razor` w `8e66aea` wstrzykuje inline `<script>` skali (linie 23–24), co łamie `script-src 'self'` bez hashy. Spine to przyznaje („zmiana w eod-ui, 02.11.2026”). Podobnie AD-9 (wyłączenie eksportu CSV, sumy od ekranu, blokada układu) i AD-11 (Enter i fokus w `ConfirmAsync`) zakładają parametry, których we wzorcu jeszcze nie ma. Na dzień przeglądu `origin/main` eod-ui = `8e66aea` (`git ls-remote`), czyli żadnej z tych zmian nie wypchnięto.
- **Poprawka:** w Stack zapisać, że wymagany jest commit eod-ui **po** zmianach AD-9, AD-11 i AD-12 (nie `8e66aea`). W planie epiku 7 dodać historię „zmiany we wzorcu” jako pierwszą, blokującą.

## Potwierdzone (bez uwag)

| Twierdzenie | Źródło potwierdzenia |
| --- | --- |
| Fluent UI Blazor 5.0.1 to najnowsze wydanie 5.x (i ogólnie najnowsze) | NuGet flatcontainer: ostatnie `5.0.0`, `5.0.1`; tag GitHub `v5.0.1` z 14.10.2026; Icons też 5.0.1 |
| 5.0.1 pasuje do .NET 10.0.14 | nuspec: grupa `net10.0` wymaga `Microsoft.AspNetCore.Components.Web` ≥ 10.0.12 |
| .NET SDK 10.0.403 / runtime 10.0.14 to najnowsze | `releases.json` 10.0: `latest-sdk` 10.0.403, `latest-runtime` 10.0.14 (25.10.2026, wydanie bezpieczeństwa) |
| eod-ui `8e66aea` buduje się na SDK 10.0.403 | próbne `dotnet build -c Release` biblioteki `Eod.Platform.UI` przez `ProjectReference`: 0 ostrzeżeń, 0 błędów; eod-ui nie ma własnego `global.json`, więc obowiązuje `global.json` aplikacji |
| actions/checkout v7 obsługuje `submodules: recursive` | README v7.0.1: „`true` … or `recursive` to recursively checkout submodules”; v7.0.1 z 20.07.2026 |
| `COPY` w Dockerfile obejmuje treść podmodułu przy kontekście lokalnym po checkoucie z podmodułami | dokumentacja Docker „Build context”: „all subdirectories are included” |
| setup-dotnet v6, build-push-action v7 istnieją | GitHub: setup-dotnet v6.0.0 (16.07.2026), build-push-action v7.4.0 (15.09.2026) |
| IMask i SortableJS lokalnie, bez CDN (AD-12) | `EodHead.razor`: `_content/Eod.Platform.UI/lib/...`; w bibliotece brak adresów `https://` |
| Brak Git LFS w eod-ui (pliki dostępne po zwykłym checkoucie podmodułu) | `.gitattributes` bez `filter=lfs` |

## Niepotwierdzone (do sprawdzenia przy pierwszym przebiegu)

- Czy BuildKit przy kontekście Git używa `GIT_AUTH_TOKEN` także dla podmodułów (F3). Problem znika przy kontekście ścieżki.
- docker/login-action v4, docker/setup-buildx-action v4 i appleboy/ssh-action v1.2.5 są poza zakresem tej zmiany i ich nie sprawdzano.
- Czy obraz `mcr.microsoft.com/dotnet/sdk:10.0.403` istnieje jako tag. Nie sprawdzano, a Dockerfile już go używa.
