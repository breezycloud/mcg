# import-trips

One-off backfill tool for the Feb–Sep 2025 historical trip data that only
existed in a spreadsheet (`Shared/Truck Loading Info.xlsx`, kept local —
not committed, see `.gitignore`). Not part of the running app; run manually
when needed.

## How it works

1. **`resolve_trips.py`** parses the Excel workbook and resolves each row
   against a target environment's Trucks/Drivers/Stations, using a set of
   manually-confirmed mapping tables (`LOADING_POINT_MAP`,
   `DISCHARGE_LOCATION_MAP`, `DISCARD_STATIONS`, `KNOWN_STATION_ADDRESSES`)
   built up through several rounds of human review — discharge location text
   in the sheet is messy (spelling variants, agent-name suffixes, compound
   "/"-joined locations), so this is deliberately manual-mapping-first with
   auto-create as the fallback for the genuine long tail, not fuzzy-matching
   against the full station list (tried early on, produced wrong matches like
   real place names mapped to unrelated person names).

   ```
   python3 resolve_trips.py --ref-dir /tmp --out import_data.json
   ```

   `--ref-dir` must contain `db_stations.txt`, `db_trucks.txt`,
   `db_drivers.txt` (pipe-delimited `psql` exports — see
   `prod-runbook/02_export_reference_data.sh`). Defaults to `/tmp`,
   `Shared/Truck Loading Info.xlsx`, and `./import_data.json`.

2. **`Program.cs`** (build via `ImportTrips.csproj`) reads that JSON and
   writes `Trip`/`Discharge`/`Station` rows via the real `AppDbContext`, so
   JSONB columns (`LoadingInfo`, `ArrivalInfo`, `CloseInfo`) serialize
   exactly like the running app would produce. All imported trips are forced
   to `Status = Closed`.

   ```
   dotnet run --project . -- import_data.json            # dry run, no writes
   dotnet run --project . -- import_data.json --commit    # writes to local Postgres
   ```

   Defaults to local Docker Postgres (reads `POSTGRES_PASSWORD` from the
   environment or repo-root `.env`). To target another environment, pass
   `--conn "<connection string>"`; `--commit` against anything that isn't
   `localhost`/`127.0.0.1` also requires `--allow-prod`.

## What was actually imported locally

1,491 trips (Feb–Sep 2025), 1,194 discharges, 3 new stations (`DSS`,
`Dahua Paper Company`, `Goodone Carton Nig Ltd` — addresses set via
`KNOWN_STATION_ADDRESSES`). Existing Jul–Sep 2025 trips were wiped first and
replaced wholesale rather than deduplicated (`Incidents`/`ServiceRequest`
rows referencing them had `TripId` nulled rather than being deleted).

## Running this against prod

See `prod-runbook/README.md` — it is **not** a re-run of the local process.
Local Postgres was restored from a prod dump taken earlier; prod's reference
data and its own Jul-Sep trips may have moved since, so the runbook re-derives
everything against a fresh prod export rather than replaying local GUIDs.
