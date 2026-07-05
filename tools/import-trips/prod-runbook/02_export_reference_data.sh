#!/usr/bin/env bash
# Exports Trucks/Drivers/Stations reference data from a target Postgres so
# resolve_trips.py can re-resolve truck/driver/station GUIDs against that
# target's *current* data instead of the stale local-Docker snapshot.
#
# Usage:
#   PGHOST=... PGPORT=... PGUSER=postgres PGPASSWORD=... PGDATABASE=mcg_db \
#     ./02_export_reference_data.sh /path/to/output/dir
#
# For prod, run this ON THE VPS (psql against the local Postgres there) or via
# an SSH tunnel — whichever you normally use to reach prod Postgres. Do not
# expose prod Postgres directly to the internet to make this "convenient".

set -euo pipefail

OUT_DIR="${1:?Usage: $0 <output-dir>}"
mkdir -p "$OUT_DIR"

psql -t -A -F'|' -c 'SELECT "Id", "Name" FROM "Stations" ORDER BY "Name";' > "$OUT_DIR/db_stations.txt.tmp"
{ echo ' Id|Name'; echo '---+---'; cat "$OUT_DIR/db_stations.txt.tmp"; echo "($(wc -l < "$OUT_DIR/db_stations.txt.tmp") rows)"; } > "$OUT_DIR/db_stations.txt"
rm "$OUT_DIR/db_stations.txt.tmp"

psql -t -A -F'|' -c 'SELECT "Id", "LicensePlate", "Product" FROM "Trucks" ORDER BY "LicensePlate";' > "$OUT_DIR/db_trucks.txt.tmp"
{ echo ' Id|LicensePlate|Product'; echo '---+---+---'; cat "$OUT_DIR/db_trucks.txt.tmp"; echo "($(wc -l < "$OUT_DIR/db_trucks.txt.tmp") rows)"; } > "$OUT_DIR/db_trucks.txt"
rm "$OUT_DIR/db_trucks.txt.tmp"

psql -t -A -F'|' -c 'SELECT "Id", "FirstName", "LastName" FROM "Drivers" ORDER BY "LastName";' > "$OUT_DIR/db_drivers.txt.tmp"
{ echo ' Id|FirstName|LastName'; echo '---+---+---'; cat "$OUT_DIR/db_drivers.txt.tmp"; echo "($(wc -l < "$OUT_DIR/db_drivers.txt.tmp") rows)"; } > "$OUT_DIR/db_drivers.txt"
rm "$OUT_DIR/db_drivers.txt.tmp"

echo "Exported to $OUT_DIR:"
wc -l "$OUT_DIR"/db_stations.txt "$OUT_DIR"/db_trucks.txt "$OUT_DIR"/db_drivers.txt
