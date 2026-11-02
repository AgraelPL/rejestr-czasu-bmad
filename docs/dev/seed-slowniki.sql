-- Test Dictionaries for manual checks: Projects and Work types of one Account (login in the psql variable "login").
-- Safe to run again: existing codes and names are skipped. One inactive item each, for the R2 checks.
-- Run: Get-Content docs/dev/seed-slowniki.sql | docker exec -i rejestr-czasu-db psql -U rejestr -d rejestr -v login=<login>

\set ON_ERROR_STOP on

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
