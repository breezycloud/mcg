# Feb–Sep 2025 trip import — prod runbook

This packages the same process already run and verified against local Docker
Postgres: importing ~1,491 historical trips (Feb–Sep 2025) parsed from
`Shared/Truck Loading Info.xlsx`, with Jul–Sep native trips wiped and replaced
wholesale by the Excel-derived data.

**Do not run any of this until explicitly told to.** Every step below writes
to prod. Go in order; stop and re-assess if any check doesn't match what's
expected.

## Why this isn't a copy-paste of the local run

Local Postgres was restored from a prod `pg_dump` taken earlier. Time has
passed since then — prod's Trucks/Drivers/Stations and its own Jul-Sep trips
may have moved. Reusing the local run's resolved GUIDs as-is would silently
write against stale references. So the resolution step gets re-run here
against a **fresh prod export**, not the cached local one.

## Sequence

1. **Take a fresh full prod backup** (`pg_dump`) immediately before touching
   anything. This is the actual safety net if something goes wrong.

2. **Sanity-check Jul-Sep prod data still looks disposable/incomplete**, the
   same way it did on local — this was the basis for deciding to wipe rather
   than dedupe. Run the `SELECT` at the top of `03_wipe_jul_sep_trips.sql`
   against prod first and eyeball it. If prod's Jul-Sep data looks
   meaningfully different from what we saw locally (e.g. far more rows, or
   clearly complete/curated data), stop and reconsider — wiping was decided
   against the local snapshot's state, not a fresh look at current prod.

3. **Apply `01_prereq_station_renames.sql`** to prod (disambiguates the 6
   duplicate "CCECC" stations by state, matching the local rename that
   `resolve_trips.py`'s discharge-location map depends on). Each statement is
   guarded to only fire if the row still says exactly `'CCECC'` — confirm you
   see `UPDATE 1` six times, not `UPDATE 0`.

4. **Export fresh reference data from prod**:
   ```
   ./02_export_reference_data.sh /path/to/prod-ref-data
   ```
   Run this against prod Postgres (SSH into the VPS and run it there, or via
   whatever tunnel you normally use — do not expose prod Postgres to reach it
   "conveniently" from a laptop).

5. **Re-run resolution against the fresh prod reference data**:
   ```
   cd tools/import-trips
   python3 resolve_trips.py --ref-dir /path/to/prod-ref-data --out prod_import_data.json
   ```
   Compare the printed stats against the local run's known-good numbers:
   1,491 trips resolved, 1 truck-not-found skip, 3 new stations. If prod's
   Trucks/Drivers/Stations have diverged from the local snapshot, these
   numbers will differ — investigate any difference before proceeding
   (new unmatched trucks, a different new-station count, etc. all mean
   something in prod's reference data isn't what the local run assumed).

6. **Dry-run the import against prod** (no writes yet):
   ```
   dotnet run --project . -- prod_import_data.json --conn "<prod connection string>"
   ```
   Review the trip-per-month counts and confirm zero unresolved discharge
   station refs, same as the local dry run.

7. **Wipe Jul-Sep prod trips**: run `03_wipe_jul_sep_trips.sql` against prod.
   Review the `UPDATE`/`DELETE` row counts it prints before the final
   `COMMIT` — if you're running it interactively in `psql`, you can `ROLLBACK`
   instead if anything looks off.

8. **Commit the import**:
   ```
   dotnet run --project . -- prod_import_data.json --conn "<prod connection string>" --commit --allow-prod
   ```
   Both `--commit` and `--allow-prod` are required together for any non-local
   connection string — this is deliberate, not a bug.

9. **Verify**: re-run the same per-month `SELECT ... GROUP BY month` query
   used to verify the local import, spot-check a record's discharges, and
   check the app's dashboard.

## Files in this directory

- `01_prereq_station_renames.sql` — CCECC disambiguation, run first.
- `02_export_reference_data.sh` — exports Trucks/Drivers/Stations from
  whatever Postgres it's pointed at.
- `03_wipe_jul_sep_trips.sql` — deletes Jul-Sep 2025 trips (nulls out
  Incidents/ServiceRequest FKs first; Discharges/TripCheckpoints cascade).

The resolution script (`../resolve_trips.py`) and import tool
(`../Program.cs` / `ImportTrips.csproj`) live one level up — they're the same
tool used for local, just pointed at different inputs via `--ref-dir` and
`--conn`.
