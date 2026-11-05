-- Test Dictionaries for manual checks: Projects and Work types of one Account (login in the psql variable "login").
-- Safe to run again: existing codes and names are skipped. One inactive item each, for the R2 checks.
-- Run (docker cp + psql -f: the PowerShell 5.1 pipe Get-Content | docker exec -i turns Polish letters into "?"):
--   docker cp docs/dev/seed-slowniki.sql rejestr-czasu-db:/tmp/seed-slowniki.sql
--   docker exec rejestr-czasu-db psql -U rejestr -d rejestr -v login=<login> -f /tmp/seed-slowniki.sql

\set ON_ERROR_STOP on

-- Repairs names broken by that pipe in an earlier run: a Project with a seed code and "?" in its name gets the seed name.
UPDATE project p
SET name = v.name
FROM users u, (VALUES
    ('RDZ', 'Rozwój systemu'),
    ('WKU', 'Wdrożenie u klienta'),
    ('SZK', 'Szkolenia wewnętrzne')
) AS v(code, name)
WHERE u.id = p.user_id AND u.user_name = :'login' AND p.code = v.code AND p.name LIKE '%?%';

INSERT INTO project (user_id, code, name, is_active)
SELECT u.id, v.code, v.name, v.is_active
FROM users u
CROSS JOIN (VALUES
    ('RDZ', 'Rozwój systemu', true),
    ('WKU', 'Wdrożenie u klienta', true),
    ('ORG', 'Organizacja i administracja', true),
    ('RRJ', 'Raporty i rejestry', true),
    ('SZK', 'Szkolenia wewnętrzne', true),
    ('MIG', 'Migracja danych', true),
    ('OLD', 'Stary projekt', false)
) AS v(code, name, is_active)
WHERE u.user_name = :'login'
  AND NOT EXISTS (SELECT 1 FROM project p WHERE p.user_id = u.id AND p.code = v.code);

INSERT INTO work_type (user_id, name, is_active)
SELECT u.id, v.name, v.is_active
FROM users u
CROSS JOIN (VALUES
    ('Development', true),
    ('Analiza bis-sys', true),
    ('Spotkanie', true),
    ('Dokumentacja', true),
    ('Archiwalny', false)
) AS v(name, is_active)
WHERE u.user_name = :'login'
  AND NOT EXISTS (SELECT 1 FROM work_type w WHERE w.user_id = u.id AND lower(w.name) = lower(v.name));

SELECT (SELECT count(*) FROM project p JOIN users u ON u.id = p.user_id WHERE u.user_name = :'login') AS projekty,
       (SELECT count(*) FROM work_type w JOIN users u ON u.id = w.user_id WHERE u.user_name = :'login') AS typy_pracy;
