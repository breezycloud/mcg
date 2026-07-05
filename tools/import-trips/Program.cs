using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Api.Context;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Shared.Enums;
using Shared.Models.Stations;
using Shared.Models.Trips;

// Historical backfill tool: reads import_data.json (produced by
// resolve_trips.py from "Truck Loading Info.xlsx") and writes Trip/Discharge/
// Station rows via the real AppDbContext so JSONB columns serialize exactly
// like the running app.
//
// Defaults to local Docker Postgres. To target another environment (prod),
// pass --conn "<full Npgsql connection string>" explicitly — see
// prod-runbook/README.md for the full sequence. Writing (--commit) against a
// non-local host also requires --allow-prod, as a deliberate extra step to
// prevent an accidental prod write.

var dryRun = !args.Contains("--commit");
var allowProd = args.Contains("--allow-prod");
var jsonPath = args.FirstOrDefault(a => !a.StartsWith("--")) ?? "import_data.json";

string connString;
var connArgIndex = Array.IndexOf(args, "--conn");
if (connArgIndex >= 0 && connArgIndex + 1 < args.Length)
{
    connString = args[connArgIndex + 1];
}
else
{
    var password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD");
    if (string.IsNullOrEmpty(password))
    {
        var envFile = FindUp(Directory.GetCurrentDirectory(), ".env");
        if (envFile != null)
        {
            var line = File.ReadAllLines(envFile).FirstOrDefault(l => l.StartsWith("POSTGRES_PASSWORD="));
            if (line != null) password = line["POSTGRES_PASSWORD=".Length..].Trim();
        }
    }
    if (string.IsNullOrEmpty(password))
    {
        Console.WriteLine("POSTGRES_PASSWORD not found in env or .env — aborting.");
        return 1;
    }
    connString = $"Host=localhost;Port=5432;Username=postgres;Password={password};Database=mcg_db";
}

var isLocalHost = Regex.IsMatch(connString, @"Host\s*=\s*(localhost|127\.0\.0\.1)", RegexOptions.IgnoreCase);
if (!isLocalHost && !dryRun && !allowProd)
{
    Console.WriteLine("Refusing to --commit against a non-local connection string without --allow-prod.");
    Console.WriteLine("This is a deliberate safety check, not a bug — re-run with --allow-prod once you've");
    Console.WriteLine("confirmed you actually mean to write to this target.");
    return 1;
}

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);
dataSourceBuilder.EnableDynamicJson();
await using var dataSource = dataSourceBuilder.Build();

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseNpgsql(dataSource, o => o.SetPostgresVersion(16, 4))
    .Options;

await using var db = new AppDbContext(options);

var targetLabel = isLocalHost ? "local Postgres" : "NON-LOCAL target (prod?)";
Console.WriteLine(dryRun
    ? $"=== DRY RUN (no writes) — target: {targetLabel} — pass --commit to actually import ==="
    : $"=== COMMIT MODE — writing to {targetLabel} ===");

var json = await File.ReadAllTextAsync(jsonPath);
var data = JsonSerializer.Deserialize<ImportData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Failed to parse import_data.json");

Console.WriteLine($"Loaded {data.Trips.Count} trip records, {data.NewStations.Count} distinct new stations to create.");

// --- Resolve/create stations ---
var existingStations = (await db.Stations.ToListAsync())
    .GroupBy(s => NormalizeKey(s.Name))
    .ToDictionary(g => g.Key, g => g.First().Id);
var newStationLookup = new Dictionary<string, Guid>();
var stationsToCreate = new List<Station>();

foreach (var newStation in data.NewStations)
{
    var key = NormalizeKey(newStation.Name);
    if (existingStations.ContainsKey(key)) continue; // already resolved somehow, skip
    if (newStationLookup.ContainsKey(key)) continue;
    var id = Guid.NewGuid();
    newStationLookup[key] = id;
    stationsToCreate.Add(new Station
    {
        Id = id,
        Name = newStation.Name.Trim(),
        Type = StationType.DischargeStation,
        IsDepot = false,
        CreatedAt = DateTimeOffset.UtcNow,
        Address = newStation.Address is { } a
            ? new Shared.Models.BaseEntity.Address { Location = a.Location, State = a.State, ContactAddress = a.ContactAddress }
            : null,
    });
}

Console.WriteLine($"Stations to create: {stationsToCreate.Count}");

// --- Build trips ---
var tripsToCreate = new List<Trip>();
var dischargesToCreate = new List<Discharge>();
var unresolvedStationRefs = new List<string>();
var skippedNoDate = 0;

foreach (var t in data.Trips)
{
    if (!DateOnly.TryParse(t.LoadingDate, out var loadDate))
    {
        skippedNoDate++;
        continue;
    }

    var tripDate = LagosMidnightUtc(loadDate.Year, loadDate.Month, loadDate.Day);
    var tripId = Guid.NewGuid();

    var discharges = new List<Discharge>();
    for (int i = 0; i < t.Discharges.Count; i++)
    {
        var d = t.Discharges[i];
        Guid? stationId = null;
        if (d.StationId.HasValue)
        {
            stationId = d.StationId.Value;
        }
        else if (!string.IsNullOrWhiteSpace(d.NewStationName))
        {
            var key = NormalizeKey(d.NewStationName);
            if (existingStations.TryGetValue(key, out var sid)) stationId = sid;
            else if (newStationLookup.TryGetValue(key, out var nsid)) stationId = nsid;
        }

        if (stationId is null)
        {
            unresolvedStationRefs.Add($"trip waybill={t.WaybillNo} month={t.Month} location={d.NewStationName}");
            continue;
        }

        DateTimeOffset? dischargeDate = null;
        if (DateOnly.TryParse(d.Date, out var dd))
            dischargeDate = LagosMidnightUtc(dd.Year, dd.Month, dd.Day);

        discharges.Add(new Discharge
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            StationId = stationId.Value,
            QuantityDischarged = (decimal)d.Quantity,
            DischargeStartTime = dischargeDate,
            TruckArrival = dischargeDate,
            IsFinalDischarge = i == t.Discharges.Count - 1,
        });
    }

    var closeRemark = t.Remarks;
    if (!string.IsNullOrWhiteSpace(t.DischargeNote))
        closeRemark = string.IsNullOrWhiteSpace(closeRemark) ? t.DischargeNote : $"{closeRemark}; {t.DischargeNote}";

    DateTime? returnDate = DateOnly.TryParse(t.ReturnDate, out var rd) ? rd.ToDateTime(TimeOnly.MinValue) : null;

    var trip = new Trip
    {
        Id = tripId,
        Date = tripDate,
        TruckId = t.TruckId,
        DriverId = t.DriverId,
        LoadingDepotId = t.LoadingDepotId,
        Status = TripStatus.Closed,
        CreatedAt = tripDate,
        LoadingInfo = new LoadingInfo
        {
            LoadingDate = loadDate.ToDateTime(TimeOnly.MinValue),
            WaybillNo = t.WaybillNo,
            Quantity = t.DispatchQty.HasValue ? (decimal?)t.DispatchQty.Value : null,
            DispatchType = DispatchType.Depot,
            DestinationMode = discharges.Count > 1 ? DestinationMode.Multi : DestinationMode.Single,
            ElockStatus = t.ElockStatus == "Abnormal" ? ElockStatus.Abnormal : ElockStatus.Normal,
            Destination = t.Destination,
            Remark = t.Remarks,
        },
        ArrivalInfo = new ArrivalInfo
        {
            ArrivedDepot = t.ArrivedAtv == "Yes",
            LoadingLocationArrivalDateTime = DateOnly.TryParse(t.AtvArrivalDate, out var av) ? av.ToDateTime(TimeOnly.MinValue) : null,
            ArrivedAtStation = t.ArrivedStation == "Yes",
            StationArrivalDateTime = DateOnly.TryParse(t.StationArrivalDate, out var sa) ? sa.ToDateTime(TimeOnly.MinValue) : null,
            Destination = t.Destination,
            Remark = t.Remarks,
        },
        CloseInfo = new CloseInfo
        {
            ReturnDateTime = returnDate,
            TripRemark = closeRemark,
            Rating = 0,
            CloseDateTime = returnDate ?? loadDate.ToDateTime(TimeOnly.MinValue),
        },
    };

    tripsToCreate.Add(trip);
    dischargesToCreate.AddRange(discharges);
}

Console.WriteLine($"Trips built: {tripsToCreate.Count} (skipped, unparsable date: {skippedNoDate})");
Console.WriteLine($"Discharges built: {dischargesToCreate.Count}");
Console.WriteLine($"Unresolved discharge station refs (dropped): {unresolvedStationRefs.Count}");
foreach (var u in unresolvedStationRefs.Take(10)) Console.WriteLine($"  {u}");

var byMonth = tripsToCreate
    .GroupBy(t => new DateOnly(t.Date.Year, t.Date.Month, 1))
    .OrderBy(g => g.Key)
    .Select(g => $"{g.Key:yyyy-MM}: {g.Count()}");
Console.WriteLine("\nTrips per month:");
foreach (var line in byMonth) Console.WriteLine($"  {line}");

if (dryRun)
{
    Console.WriteLine("\nDry run complete. No changes written. Re-run with --commit to import.");
    return 0;
}

Console.WriteLine("\nWriting to database...");
await using var tx = await db.Database.BeginTransactionAsync();
db.Stations.AddRange(stationsToCreate);
db.Trips.AddRange(tripsToCreate);
db.Discharges.AddRange(dischargesToCreate);
await db.SaveChangesAsync();
await tx.CommitAsync();
Console.WriteLine($"Committed: {stationsToCreate.Count} stations, {tripsToCreate.Count} trips, {dischargesToCreate.Count} discharges.");

return 0;

static string NormalizeKey(string s) => Regex.Replace(s.Trim(), @"\s+", " ").ToUpperInvariant();

// Africa/Lagos (WAT, UTC+1) midnight, converted to the UTC-offset DateTimeOffset
// Npgsql requires for timestamptz — matches how existing app-created rows are stored
// (WAT midnight persists as 23:00 UTC the prior day).
static DateTimeOffset LagosMidnightUtc(int year, int month, int day)
    => new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.FromHours(1)).ToUniversalTime();

static string? FindUp(string startDir, string fileName)
{
    var dir = new DirectoryInfo(startDir);
    while (dir != null)
    {
        var candidate = Path.Combine(dir.FullName, fileName);
        if (File.Exists(candidate)) return candidate;
        dir = dir.Parent;
    }
    return null;
}

class ImportData
{
    [JsonPropertyName("trips")] public List<TripRecord> Trips { get; set; } = [];
    [JsonPropertyName("new_stations")] public List<NewStationRecord> NewStations { get; set; } = [];
}

class NewStationRecord
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("address")] public NewStationAddress? Address { get; set; }
}

class NewStationAddress
{
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("state")] public string State { get; set; } = "";
    [JsonPropertyName("contact_address")] public string? ContactAddress { get; set; }
}

class TripRecord
{
    [JsonPropertyName("month")] public string Month { get; set; } = "";
    [JsonPropertyName("truck_id")] public Guid TruckId { get; set; }
    [JsonPropertyName("driver_id")] public Guid? DriverId { get; set; }
    [JsonPropertyName("loading_date")] public string LoadingDate { get; set; } = "";
    [JsonPropertyName("waybill_no")] public string? WaybillNo { get; set; }
    [JsonPropertyName("dispatch_qty")] public double? DispatchQty { get; set; }
    [JsonPropertyName("product")] public string Product { get; set; } = "";
    [JsonPropertyName("status")] public string Status { get; set; } = "Closed";
    [JsonPropertyName("loading_depot_id")] public Guid? LoadingDepotId { get; set; }
    [JsonPropertyName("destination")] public string? Destination { get; set; }
    [JsonPropertyName("elock_status")] public string? ElockStatus { get; set; }
    [JsonPropertyName("arrived_atv")] public string? ArrivedAtv { get; set; }
    [JsonPropertyName("atv_arrival_date")] public string? AtvArrivalDate { get; set; }
    [JsonPropertyName("arrived_station")] public string? ArrivedStation { get; set; }
    [JsonPropertyName("station_arrival_date")] public string? StationArrivalDate { get; set; }
    [JsonPropertyName("return_date")] public string? ReturnDate { get; set; }
    [JsonPropertyName("remarks")] public string? Remarks { get; set; }
    [JsonPropertyName("discharge_note")] public string? DischargeNote { get; set; }
    [JsonPropertyName("discharges")] public List<DischargeRecord> Discharges { get; set; } = [];
}

class DischargeRecord
{
    [JsonPropertyName("station_id")] public Guid? StationId { get; set; }
    [JsonPropertyName("new_station_name")] public string? NewStationName { get; set; }
    [JsonPropertyName("quantity")] public double Quantity { get; set; }
    [JsonPropertyName("date")] public string? Date { get; set; }
}
