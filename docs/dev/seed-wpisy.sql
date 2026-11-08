-- Test Entries for manual checks of epic 3 (Widok miesiąca, Zestawienie, Lista wpisów): one Account (psql variable "login").
-- Needs the test Dictionaries first (docs/dev/seed-slowniki.sql: RDZ, WKU, ORG, MIG, Development, Analiza bis-sys,
-- Spotkanie, Dokumentacja). Fictional data only - never the real September Entries (Test wrześniowy stays manual).
-- Safe to run again: every Entry it adds starts with "[seed]", and the previous ones of this Account are deleted first;
-- Entries typed on the screens are not touched.
-- Run (docker cp + psql -f: the PowerShell 5.1 pipe Get-Content | docker exec -i turns Polish letters into "?"):
--   docker cp docs/dev/seed-wpisy.sql rejestr-czasu-db:/tmp/seed-wpisy.sql
--   docker exec rejestr-czasu-db psql -U rejestr -d rejestr -v login=<login> -f /tmp/seed-wpisy.sql
--
-- What it gives:
--   November 2026: Sunday 01.11 alone in "Tydzień 1 (01.11–01.11)"; every working day two Entries (more than 30 rows,
--   so Lista wpisów has pages); 03.11 a third RDZ/Development Entry that merges into one Wiersz zestawienia;
--   one Saturday Entry; week 30.11 cut to one day.
--   October 2026: a few Entries (Poprzedni from November).
--   December 2025 and January 2026: one Entry each (navigation across the year).

\set ON_ERROR_STOP on

DELETE FROM time_entry e
USING users u
WHERE u.id = e.user_id AND u.user_name = :'login' AND e.description LIKE '[seed]%';

WITH u AS (SELECT id FROM users WHERE user_name = :'login'),
p AS (SELECT p.code, p.id FROM project p JOIN u ON u.id = p.user_id),
w AS (SELECT w.name, w.id FROM work_type w JOIN u ON u.id = w.user_id),
days AS (SELECT d::date AS date FROM generate_series(DATE '2026-11-02', DATE '2026-11-30', INTERVAL '1 day') d
         WHERE extract(isodow FROM d) <= 5),
rows (date, code, work_type, description, minutes) AS (
    -- Every working day of November: development in the morning, a second Entry that rotates the Project and Work type.
    SELECT date, 'RDZ', 'Development', '[seed] Rozwój funkcji ' || to_char(date, 'DD.MM'), 300 FROM days
    UNION ALL
    SELECT date,
           (ARRAY['WKU', 'ORG', 'MIG'])[1 + extract(day FROM date)::int % 3],
           (ARRAY['Analiza bis-sys', 'Spotkanie', 'Dokumentacja'])[1 + extract(day FROM date)::int % 3],
           '[seed] Zadanie dodatkowe ' || to_char(date, 'DD.MM'),
           (ARRAY[90, 120, 165])[1 + extract(day FROM date)::int % 3]
    FROM days
    UNION ALL
    VALUES
        -- Sunday 01.11: week 1 has this one day only.
        (DATE '2026-11-01', 'ORG', 'Dokumentacja', '[seed] Porządki w dokumentach', 60),
        -- 03.11: the same day, Project and Work type as the morning Entry - one Wiersz zestawienia, two lines of description.
        (DATE '2026-11-03', 'RDZ', 'Development', '[seed] Poprawki po przeglądzie', 75),
        -- Saturday 14.11.
        (DATE '2026-11-14', 'MIG', 'Analiza bis-sys', '[seed] Analiza migracji w sobotę', 135),
        -- October 2026.
        (DATE '2026-10-05', 'RDZ', 'Development', '[seed] Październik - rozwój', 360),
        (DATE '2026-10-05', 'WKU', 'Spotkanie', '[seed] Październik - spotkanie u klienta', 90),
        (DATE '2026-10-21', 'ORG', 'Dokumentacja', '[seed] Październik - dokumentacja', 120),
        -- December 2025 and January 2026: one Entry each.
        (DATE '2025-12-31', 'RDZ', 'Development', '[seed] Sylwestrowe poprawki', 150),
        (DATE '2026-01-02', 'MIG', 'Analiza bis-sys', '[seed] Nowy rok - analiza', 240)
)
INSERT INTO time_entry (user_id, date, project_id, work_type_id, description, minutes)
SELECT u.id, r.date, p.id, w.id, r.description, r.minutes
FROM rows r
CROSS JOIN u
JOIN p ON p.code = r.code
JOIN w ON w.name = r.work_type
ORDER BY r.date, r.code, r.work_type;

SELECT to_char(e.date, 'YYYY-MM') AS miesiac, count(*) AS wpisy, round(sum(e.minutes) / 60.0, 2) AS godziny
FROM time_entry e JOIN users u ON u.id = e.user_id
WHERE u.user_name = :'login' AND e.description LIKE '[seed]%'
GROUP BY 1 ORDER BY 1;
