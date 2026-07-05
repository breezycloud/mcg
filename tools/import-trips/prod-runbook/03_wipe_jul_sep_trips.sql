-- Deletes Jul-Sep 2025 trips so they can be replaced wholesale by the
-- Excel-derived import, mirroring what was done on local Postgres. Run this
-- ONLY after:
--   1. Taking a fresh full prod backup (pg_dump) immediately before this.
--   2. Re-confirming the actual prod trip count in this range still looks
--      like the disposable/incomplete data we saw locally (re-run the SELECT
--      below and sanity-check the count before running the DELETE).
--   3. Applying 01_prereq_station_renames.sql.
--
-- Incidents/ServiceRequest have TripId FK with delete_rule = NO ACTION (not
-- CASCADE) and TripId is nullable on both, so we null those out first rather
-- than deleting the incident/service-request records themselves.
-- Discharges and TripCheckpoints cascade automatically.

-- Run first, standalone, to see what's actually there before deciding to proceed:
-- SELECT count(*) FROM "Trips"
-- WHERE to_char("Date" AT TIME ZONE 'Africa/Lagos', 'YYYY-MM-DD') >= '2025-02-01'
--   AND to_char("Date" AT TIME ZONE 'Africa/Lagos', 'YYYY-MM-DD') < '2025-10-01';

BEGIN;

UPDATE "Incidents" i SET "TripId" = NULL
FROM "Trips" t
WHERE t."Id" = i."TripId"
  AND to_char(t."Date" AT TIME ZONE 'Africa/Lagos', 'YYYY-MM-DD') >= '2025-02-01'
  AND to_char(t."Date" AT TIME ZONE 'Africa/Lagos', 'YYYY-MM-DD') < '2025-10-01';

UPDATE "ServiceRequest" s SET "TripId" = NULL
FROM "Trips" t
WHERE t."Id" = s."TripId"
  AND to_char(t."Date" AT TIME ZONE 'Africa/Lagos', 'YYYY-MM-DD') >= '2025-02-01'
  AND to_char(t."Date" AT TIME ZONE 'Africa/Lagos', 'YYYY-MM-DD') < '2025-10-01';

DELETE FROM "Trips"
WHERE to_char("Date" AT TIME ZONE 'Africa/Lagos', 'YYYY-MM-DD') >= '2025-02-01'
  AND to_char("Date" AT TIME ZONE 'Africa/Lagos', 'YYYY-MM-DD') < '2025-10-01';

-- Review the row counts printed above (UPDATE n / DELETE n) before committing.
-- If anything looks off, ROLLBACK instead.
COMMIT;
