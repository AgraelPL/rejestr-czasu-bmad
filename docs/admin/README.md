# Instrukcja administratora

Ręczne kroki w GitHubie i na serwerze, których nie wykonuje potok ani aplikacja.

## Spis tematów

1. [Ochrona gałęzi `main`](#ochrona-gałęzi-main)

## Ochrona gałęzi `main`

Workflow `.github/workflows/pr.yml` uruchamia się na każdy pull request do `main` i ma jeden job
`build-and-test`: build, wszystkie testy (w tym kontrola migracji dodających), `dotnet ef migrations
has-pending-model-changes`. Reguła ochrony sprawia, że do `main` trafia tylko PR, w którym ten job jest zielony.

Regułę ustawia się raz, po pierwszym uruchomieniu workflow (GitHub podpowiada na liście tylko joby, które już
raz się wykonały — najpierw otwórz PR z gałęzi zawierającej `pr.yml` i poczekaj na wynik joba).

1. Otwórz repozytorium na GitHubie i przejdź do **Settings** → **Branches**.
2. W sekcji **Branch protection rules** kliknij **Add classic branch protection rule**.
3. W polu **Branch name pattern** wpisz `main`.
4. Zaznacz **Require a pull request before merging**. GitHub zaznacza wtedy domyślnie **Require approvals** —
   odznacz tę opcję. Autor pracuje sam i nie może zatwierdzić własnego PR, a z krokiem 6 nie dałoby się scalić
   żadnego PR.
5. Zaznacz **Require status checks to pass before merging**, a pod nim:
   - zaznacz **Require branches to be up to date before merging** (wymagana aktualność gałęzi względem `main`);
   - w polu wyszukiwania wpisz `build-and-test` i wybierz ten job z listy.
6. Zaznacz **Do not allow bypassing the above settings**, żeby reguła obowiązywała także administratora repozytorium.
7. Upewnij się, że **Allow force pushes** i **Allow deletions** są odznaczone.
8. Kliknij **Create** na dole strony.

Od tej chwili bezpośredni `git push` do `main` jest odrzucany, a przycisk scalenia PR jest zablokowany,
dopóki job `build-and-test` nie jest zielony i gałąź nie jest aktualna względem `main`.

### Sprawdzenie

- Na liście PR otwórz dowolny PR do `main`: w sekcji sprawdzeń widać `build-and-test` oznaczony jako **Required**.
- PR z czerwonym testem: przycisk **Merge pull request** jest nieaktywny, z komunikatem o wymaganym sprawdzeniu.
- `git push origin main` z lokalnego repozytorium kończy się błędem `protected branch hook declined`.

### Gdy zmienia się nazwa joba

Nazwa `build-and-test` w `pr.yml` i w regule muszą być takie same. Jeśli job zostanie przemianowany, reguła
czeka w nieskończoność na sprawdzenie, które już nie istnieje — wtedy w kroku 5 usuń starą nazwę i dodaj nową.
