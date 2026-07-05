-- Prereq for the Feb-Sep 2025 trip import: prod still has 6 Station rows all
-- named plain "CCECC" (a pre-existing data-quality issue). Locally we
-- disambiguated them by state so the discharge-location mapping in
-- resolve_trips.py could target "CCECC LAGOS" specifically. Apply the same
-- rename to prod BEFORE re-exporting reference data / running the import,
-- using the same row IDs (safe: these rows are unchanged since the dump that
-- seeded local Postgres, only the Name column differs).
--
-- Run this against prod, then re-verify with:
--   SELECT "Id", "Name" FROM "Stations" WHERE "Name" ILIKE 'CCECC%' ORDER BY "Name";
-- Expect 8 distinct names: ABUJA, EDO, KADUNA, KANO, LAGOS, OGUN, Lokoja, Minna.

-- Each UPDATE is guarded with AND "Name" = 'CCECC' so it only fires if the row
-- is exactly where we expect it to be (unchanged since the dump); otherwise it
-- silently affects 0 rows rather than clobbering something unexpected.

BEGIN;

UPDATE "Stations" SET "Name" = 'CCECC EDO'    WHERE "Id" = '01997cea-d8ff-7c5e-88c6-0286b39baf7f' AND "Name" = 'CCECC';
UPDATE "Stations" SET "Name" = 'CCECC KANO'   WHERE "Id" = '019a7d70-8522-7667-88e1-f1683ec267a1' AND "Name" = 'CCECC';
UPDATE "Stations" SET "Name" = 'CCECC OGUN'   WHERE "Id" = '019a6e57-32ac-791f-82f2-7741de258a72' AND "Name" = 'CCECC';
UPDATE "Stations" SET "Name" = 'CCECC KADUNA' WHERE "Id" = '019ab6fa-8766-79d1-9cd5-b949a94c5991' AND "Name" = 'CCECC';
UPDATE "Stations" SET "Name" = 'CCECC ABUJA'  WHERE "Id" = '019ab6f1-6a33-7a9f-bbf5-9b5ecc17ac58' AND "Name" = 'CCECC';
UPDATE "Stations" SET "Name" = 'CCECC LAGOS'  WHERE "Id" = '01999058-7df4-70dc-ac1e-9a0878cb7f31' AND "Name" = 'CCECC';

-- Expect: UPDATE 1 for each of the 6 statements above. If any shows UPDATE 0,
-- stop and investigate before proceeding (row was already renamed, deleted,
-- or the ID doesn't match what we expect on prod).

COMMIT;
