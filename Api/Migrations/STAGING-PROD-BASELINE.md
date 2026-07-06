# Adopting EF Core migrations on staging/prod

Local Postgres now runs on `Api/Migrations/*InitialBaseline*` via
`context.Database.MigrateAsync()` (see `Api/Data/SeedData.cs`), replacing the
old `EnsureCreatedAsync()` + raw-SQL approach. **Staging and prod have not
been touched.**

Environment relationship (per project owner, 2026-07-05): the local database
restored earlier this session is an exact copy of prod's current state.
Staging, going forward, will be replaced wholesale by whatever local looks
like at promotion time — not incrementally patched. This changes the plan
for staging significantly and simplifies it.

## Staging: inherits local wholesale, no reconciliation needed

Because staging gets replaced by a dump of local (not patched in place),
there's no need to inspect staging's current `DailyReports` schema or worry
about whether its 3 old migrations were ever actually applied there — that
state gets overwritten. Local is already correctly baselined
(`InitialBaseline` covers the true current model, including the
`WorkTasks`/`TomorrowTasks` shape), so staging inherits that automatically
the moment the DB is promoted.

**One thing worth checking before promoting**: staging is under active,
independent development — nerdyamin pushes there directly and may have
staging-only data (test daily reports, users, etc.) that a wholesale local→
staging DB replacement would destroy. Confirm this is expected/acceptable
before actually running the promotion, the same way local's earlier Jul-Sep
trip wipe was confirmed before executing.

Once promoted: deploy the code change (`MigrateAsync()` switch,
`InitialBaseline` migration) alongside or after the DB replacement so
`__EFMigrationsHistory` (which travels with the dump) matches what the app
expects.

## Prod: stays live, needs the careful incremental procedure

Prod is not wholesale-replaceable — it's the real, live database. It
currently matches what local looked like *before* this session's fixes: no
`InitialBaseline` migration applied, and `DailyReports` almost certainly
doesn't exist at all (the feature has only ever been pushed to `staging`,
per this repo's deploy workflows).

If this code ships to prod as-is without baselining first, `MigrateAsync()`
will see `__EFMigrationsHistory` doesn't exist, treat *no* migrations as
applied, and try to run `InitialBaseline`'s `Up()` — `CreateTable` for all 21
tables. Since prod already has 20 of them, this fails immediately
(`relation already exists`) and the app won't start.

**Procedure:**

1. Verify prod's actual state first — don't assume:
   ```sql
   \d "DailyReports"                        -- expect: doesn't exist
   SELECT * FROM "__EFMigrationsHistory";   -- expect: doesn't exist
   ```
2. Baseline the 20 pre-existing tables: mark `InitialBaseline` as applied
   without running it —
   ```sql
   CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
       "MigrationId" character varying(150) NOT NULL,
       "ProductVersion" character varying(32) NOT NULL,
       CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
   );
   INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
   VALUES ('<InitialBaseline's actual timestamp-prefixed id from the filename>', '9.0.17');
   ```
3. `DailyReports` itself still won't exist after step 2, since baselining
   skips running `Up()` entirely. Create it directly — extract just the
   `DailyReports` `CreateTable`/index/FK block from `InitialBaseline`'s
   `Up()` method and run it by hand, or write a small follow-up migration
   that only creates that one table.
4. Deploy the code change. At next boot, `MigrateAsync()` sees
   `InitialBaseline` as applied and does nothing further.

Do not run any of this against prod without a fresh backup immediately
before, and without explicit go-ahead — same standing rule as everything
else touching prod this session.
