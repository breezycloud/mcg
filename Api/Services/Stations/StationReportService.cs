using Api.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces.Stations;
using Shared.Models.RefuelInfos;
using Shared.Models.Stations;
using Shared.Models.Trips;

namespace Api.Services.Stations;

public class StationReportService : IStationReportService
{
    private readonly AppDbContext _context;

    public StationReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<StationFleetMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var totalStations = await _context.Stations.AsNoTracking().CountAsync(cancellationToken);

        var trips = await GetTripsInWindowAsync(windowStart, cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);
        var dischargedTrips = ApplyCngExclusion(
            trips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)), excludeCng).ToList();
        var stationsActiveInPeriod = trips.SelectMany(t => t.Discharges.Select(d => d.StationId)).Distinct().Count();

        return new StationFleetMetricsDto
        {
            ShortageRate = CalculateShortageRate(dischargedTrips),
            TotalStations = totalStations,
            StationsActiveInPeriod = stationsActiveInPeriod,
            TripsInPeriod = trips.Count,
            TotalQuantityDischarged = trips.Sum(t => t.Discharges.Sum(d => d.QuantityDischarged))
        };
    }

    public async Task<List<StationPerformanceRowDto>> GetStationPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var stations = await _context.Stations.AsNoTracking().ToListAsync(cancellationToken);
        var trips = await GetTripsInWindowAsync(windowStart, cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);

        var rows = new List<(StationPerformanceRowDto Row, decimal ShortageUnits)>();
        foreach (var station in stations)
        {
            var tripsAtStation = trips.Where(t => t.Discharges.Any(d => d.StationId == station.Id)).ToList();
            if (tripsAtStation.Count == 0) continue;

            var totalDispatched = tripsAtStation.Sum(t => t.LoadingInfo.Quantity ?? 0);
            var totalDischarged = tripsAtStation.Sum(t => t.Discharges.Where(d => d.StationId == station.Id).Sum(d => d.QuantityDischarged));

            // Shortage rate is scoped down further than the row's own TripCount/TotalDispatched/
            // TotalDischarged: only trips with a final discharge AT THIS STATION count toward it
            // (a mid-transit trip hasn't demonstrated a real shortage outcome yet), and CNG trips
            // are excluded when the setting says so — matching the Loading Depot report's own
            // convention of computing the rate from its own eligible subset, not the row totals.
            var shortageEligible = tripsAtStation
                .Where(t => t.Discharges.Any(d => d.StationId == station.Id && d.IsFinalDischarge))
                .Where(t => !(excludeCng && (t.Truck?.Product?.IsCng() ?? false)))
                .ToList();
            var shortageDispatched = shortageEligible.Sum(t => t.LoadingInfo.Quantity ?? 0);
            var shortageDischarged = shortageEligible.Sum(t => t.Discharges.Where(d => d.StationId == station.Id).Sum(d => d.QuantityDischarged));
            var shortageUnits = Math.Max(0, shortageDispatched - shortageDischarged);
            var shortageRate = shortageDispatched > 0 ? shortageUnits / shortageDispatched * 100 : 0;

            rows.Add((new StationPerformanceRowDto
            {
                StationId = station.Id,
                StationName = station.Name,
                TripCount = tripsAtStation.Count,
                TotalDispatched = totalDispatched,
                TotalDischarged = totalDischarged,
                ShortageRate = shortageRate,
                NeedsAttention = shortageRate > 20
            }, shortageUnits));
        }

        // Share of the TOTAL shortage across every station, not each station's own rate — a
        // station with a small share of a huge volume can still out-rank a 100%-shortage
        // station with only 1 trip, which is the point: this drives "who's actually
        // responsible for most of the lost product," not "whose one bad trip looked worst."
        var totalShortageUnits = rows.Sum(r => r.ShortageUnits);
        foreach (var (row, shortageUnits) in rows)
        {
            row.ShortageSharePercent = totalShortageUnits > 0 ? shortageUnits / totalShortageUnits * 100 : 0;
        }

        return rows.Select(r => r.Row).OrderByDescending(r => r.TripCount).ToList();
    }

    public async Task<List<StationMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        var monthsBack = Math.Max(1, months);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var earliestMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(monthsBack - 1));
        var windowStart = new DateTimeOffset(earliestMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var trips = await GetTripsInWindowAsync(windowStart, cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);

        var results = new List<StationMonthlyTrendDto>();
        for (var i = 0; i < monthsBack; i++)
        {
            var bucket = earliestMonth.AddMonths(i);
            var monthTrips = trips
                .Where(t => t.LoadingInfo.LoadingDate!.Value.Year == bucket.Year && t.LoadingInfo.LoadingDate!.Value.Month == bucket.Month)
                .ToList();
            var dischargedInMonth = ApplyCngExclusion(
                monthTrips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)), excludeCng).ToList();

            results.Add(new StationMonthlyTrendDto
            {
                Month = bucket.Month,
                Year = bucket.Year,
                TripCount = monthTrips.Count,
                ShortageRate = CalculateShortageRate(dischargedInMonth)
            });
        }

        return results;
    }

    // ─── Loading Depot report ────────────────────────────────────────────────
    // Built from Trip.LoadingDepotId + Trip.LoadingInfo.Quantity — a loading depot never
    // appears on a Discharge record, so this is a genuinely separate query from the Discharge
    // Station report above, not a filtered view of it. ShortageRate is downstream: of what was
    // loaded at a depot, how much came up short by the time whichever trip it rode on actually
    // discharged (wherever that ended up) — attributing receiving-side shortage back to origin.

    public async Task<LoadingDepotFleetMetricsDto> GetLoadingDepotMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var totalDepots = await _context.Stations.AsNoTracking().CountAsync(s => s.Type == StationType.LoadingDepot, cancellationToken);

        var trips = await GetTripsLoadedInWindowAsync(windowStart, cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);
        var dischargedTrips = ApplyCngExclusion(
            trips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)), excludeCng).ToList();
        var depotsActiveInPeriod = trips.Where(t => t.LoadingDepotId.HasValue).Select(t => t.LoadingDepotId!.Value).Distinct().Count();

        return new LoadingDepotFleetMetricsDto
        {
            ShortageRate = CalculateShortageRate(dischargedTrips),
            TotalLoadingDepots = totalDepots,
            DepotsActiveInPeriod = depotsActiveInPeriod,
            TripsInPeriod = trips.Count,
            TotalQuantityLoaded = trips.Sum(t => t.LoadingInfo.Quantity ?? 0)
        };
    }

    public async Task<List<LoadingDepotPerformanceRowDto>> GetLoadingDepotPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var depots = await _context.Stations.AsNoTracking().Where(s => s.Type == StationType.LoadingDepot).ToListAsync(cancellationToken);
        var trips = await GetTripsLoadedInWindowAsync(windowStart, cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);

        var rows = new List<(LoadingDepotPerformanceRowDto Row, decimal ShortageUnits)>();
        foreach (var depot in depots)
        {
            var depotTrips = trips.Where(t => t.LoadingDepotId == depot.Id).ToList();
            if (depotTrips.Count == 0) continue;

            var dischargedTrips = ApplyCngExclusion(
                depotTrips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)), excludeCng).ToList();
            var totalLoaded = dischargedTrips.Sum(t => t.LoadingInfo.Quantity ?? 0);
            var shortageUnits = dischargedTrips.Sum(GetShortageAmount);
            var shortageRate = totalLoaded > 0 ? shortageUnits / totalLoaded * 100 : 0;

            rows.Add((new LoadingDepotPerformanceRowDto
            {
                StationId = depot.Id,
                StationName = depot.Name,
                TripCount = depotTrips.Count,
                TotalQuantityLoaded = depotTrips.Sum(t => t.LoadingInfo.Quantity ?? 0),
                ShortageRate = shortageRate,
                NeedsAttention = shortageRate > 20
            }, shortageUnits));
        }

        // Same "share of the total problem" convention as GetStationPerformanceAsync — kept
        // consistent between the two reports.
        var totalShortageUnits = rows.Sum(r => r.ShortageUnits);
        foreach (var (row, shortageUnits) in rows)
        {
            row.ShortageSharePercent = totalShortageUnits > 0 ? shortageUnits / totalShortageUnits * 100 : 0;
        }

        return rows.Select(r => r.Row).OrderByDescending(r => r.TripCount).ToList();
    }

    public async Task<List<LoadingDepotMonthlyTrendDto>> GetLoadingDepotMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        var monthsBack = Math.Max(1, months);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var earliestMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(monthsBack - 1));
        var windowStart = new DateTimeOffset(earliestMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var trips = await GetTripsLoadedInWindowAsync(windowStart, cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);

        var results = new List<LoadingDepotMonthlyTrendDto>();
        for (var i = 0; i < monthsBack; i++)
        {
            var bucket = earliestMonth.AddMonths(i);
            var monthTrips = trips
                .Where(t => t.LoadingInfo.LoadingDate!.Value.Year == bucket.Year && t.LoadingInfo.LoadingDate!.Value.Month == bucket.Month)
                .ToList();
            var dischargedInMonth = ApplyCngExclusion(
                monthTrips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)), excludeCng).ToList();

            results.Add(new LoadingDepotMonthlyTrendDto
            {
                Month = bucket.Month,
                Year = bucket.Year,
                TripCount = monthTrips.Count,
                ShortageRate = CalculateShortageRate(dischargedInMonth)
            });
        }

        return results;
    }

    // Every trip loaded from a depot in the window, whether or not it has discharged yet
    // (unlike GetTripsInWindowAsync, which only cares about trips that HAVE discharged) —
    // TripCount/TotalQuantityLoaded need every loaded trip; only the ShortageRate calculation
    // narrows down to the subset that has actually discharged.
    private async Task<List<Trip>> GetTripsLoadedInWindowAsync(DateTimeOffset? windowStart, CancellationToken cancellationToken)
    {
        var query = _context.Trips.AsNoTracking().Where(t => t.LoadingDepotId.HasValue)
            .Include(t => t.Truck).Include(t => t.Discharges).AsSplitQuery().AsQueryable();

        if (windowStart.HasValue)
            query = query.Where(t => t.LoadingInfo.LoadingDate.HasValue && t.LoadingInfo.LoadingDate >= windowStart.Value);

        return await query.ToListAsync(cancellationToken);
    }

    // ─── Receiving Depot report ──────────────────────────────────────────────
    // A Receiving Depot never discharges anything — it's a holding stop a truck passes
    // through (Trip.ReceivingDepotId, LoadingInfo.DispatchType == Depot) before being
    // re-invoiced onward to the real discharge station. No shortage concept applies here; the
    // operational question is dwell time (how long trucks sit before re-invoicing) and how
    // many are sitting there right now.

    public async Task<ReceivingDepotFleetMetricsDto> GetReceivingDepotMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var totalDepots = await _context.Stations.AsNoTracking().CountAsync(s => s.Type == StationType.ReceivingDepot, cancellationToken);

        var trips = await GetTripsThroughReceivingDepotInWindowAsync(windowStart, cancellationToken);
        var depotsActiveInPeriod = trips.Select(t => t.ReceivingDepotId!.Value).Distinct().Count();
        var dwellHours = trips.Select(TryGetDwellHours).Where(h => h.HasValue).Select(h => h!.Value).ToList();

        var stuckTrips = await GetCurrentlyStuckTripsAsync(cancellationToken);

        return new ReceivingDepotFleetMetricsDto
        {
            AvgDwellHours = dwellHours.Count > 0 ? dwellHours.Average() : 0,
            TotalReceivingDepots = totalDepots,
            DepotsActiveInPeriod = depotsActiveInPeriod,
            TripsInPeriod = trips.Count,
            CurrentlyStuckCount = stuckTrips.Count
        };
    }

    public async Task<List<ReceivingDepotPerformanceRowDto>> GetReceivingDepotPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var depots = await _context.Stations.AsNoTracking().Where(s => s.Type == StationType.ReceivingDepot).ToListAsync(cancellationToken);
        var trips = await GetTripsThroughReceivingDepotInWindowAsync(windowStart, cancellationToken);
        var stuckTrips = await GetCurrentlyStuckTripsAsync(cancellationToken);
        var stuckCountByDepot = stuckTrips.GroupBy(t => t.ReceivingDepotId!.Value).ToDictionary(g => g.Key, g => g.Count());

        var rows = new List<ReceivingDepotPerformanceRowDto>();
        foreach (var depot in depots)
        {
            var depotTrips = trips.Where(t => t.ReceivingDepotId == depot.Id).ToList();
            if (depotTrips.Count == 0) continue;

            var dwellHours = depotTrips.Select(TryGetDwellHours).Where(h => h.HasValue).Select(h => h!.Value).ToList();
            var stuckCount = stuckCountByDepot.GetValueOrDefault(depot.Id, 0);

            rows.Add(new ReceivingDepotPerformanceRowDto
            {
                StationId = depot.Id,
                StationName = depot.Name,
                TripCount = depotTrips.Count,
                AvgDwellHours = dwellHours.Count > 0 ? dwellHours.Average() : 0,
                CurrentlyStuckCount = stuckCount,
                NeedsAttention = stuckCount > 0
            });
        }

        return rows.OrderByDescending(r => r.AvgDwellHours).ToList();
    }

    public async Task<List<ReceivingDepotMonthlyTrendDto>> GetReceivingDepotMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        var monthsBack = Math.Max(1, months);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var earliestMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(monthsBack - 1));
        var windowStart = new DateTimeOffset(earliestMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var trips = await GetTripsThroughReceivingDepotInWindowAsync(windowStart, cancellationToken);

        var results = new List<ReceivingDepotMonthlyTrendDto>();
        for (var i = 0; i < monthsBack; i++)
        {
            var bucket = earliestMonth.AddMonths(i);
            var monthTrips = trips
                .Where(t => t.LoadingInfo.LoadingDate!.Value.Year == bucket.Year && t.LoadingInfo.LoadingDate!.Value.Month == bucket.Month)
                .ToList();
            var dwellHours = monthTrips.Select(TryGetDwellHours).Where(h => h.HasValue).Select(h => h!.Value).ToList();

            results.Add(new ReceivingDepotMonthlyTrendDto
            {
                Month = bucket.Month,
                Year = bucket.Year,
                TripCount = monthTrips.Count,
                AvgDwellHours = dwellHours.Count > 0 ? dwellHours.Average() : 0
            });
        }

        return results;
    }

    // Every trip that passed through a receiving depot in the window, regardless of whether
    // it has since been re-invoiced/discharged — TripCount needs every one; only the dwell-time
    // calculation (TryGetDwellHours) narrows down to the subset with a complete, sane pair of
    // timestamps.
    private async Task<List<Trip>> GetTripsThroughReceivingDepotInWindowAsync(DateTimeOffset? windowStart, CancellationToken cancellationToken)
    {
        var query = _context.Trips.AsNoTracking().Where(t => t.ReceivingDepotId.HasValue).AsQueryable();

        if (windowStart.HasValue)
            query = query.Where(t => t.LoadingInfo.LoadingDate.HasValue && t.LoadingInfo.LoadingDate >= windowStart.Value);

        return await query.ToListAsync(cancellationToken);
    }

    // Arrived at the depot but not (yet) re-invoiced onward — a live snapshot, deliberately
    // not scoped to any period, and excludes trips someone has already closed out regardless
    // of what the ArrivalInfo checkboxes say (data-entry edge case, not a truck actually stuck).
    private async Task<List<Trip>> GetCurrentlyStuckTripsAsync(CancellationToken cancellationToken)
    {
        var candidates = await _context.Trips.AsNoTracking()
            .Where(t => t.ReceivingDepotId.HasValue
                && t.Status != TripStatus.Closed
                && t.Status != TripStatus.Completed)
            .ToListAsync(cancellationToken);

        return candidates
            .Where(t => t.ArrivalInfo.ArrivedDepot && t.ArrivalInfo.DepotArrivalDateTime.HasValue
                && !(t.ArrivalInfo.InvoiceIssued && t.ArrivalInfo.InvoiceToStationDateTime.HasValue))
            .ToList();
    }

    // Both timestamps are independently-optional and there's no validation anywhere that the
    // invoice date is actually after the arrival date — a trip only contributes to dwell-time
    // stats if both are present and the pair is sane (non-negative).
    private static decimal? TryGetDwellHours(Trip trip)
    {
        var arrival = trip.ArrivalInfo;
        if (!arrival.ArrivedDepot || !arrival.DepotArrivalDateTime.HasValue) return null;
        if (!arrival.InvoiceIssued || !arrival.InvoiceToStationDateTime.HasValue) return null;

        var span = arrival.InvoiceToStationDateTime.Value - arrival.DepotArrivalDateTime.Value;
        return span.TotalHours >= 0 ? (decimal)span.TotalHours : null;
    }

    // ─── Refuelling Station report ───────────────────────────────────────────
    // Built entirely from RefuelInfo, which already carries its own direct StationId FK — no
    // Trip cross-referencing needed at all, unlike the other three report types. No shortage/
    // rate concept applies; the operational questions are volume, event frequency, and cost.

    public async Task<RefuellingStationFleetMetricsDto> GetRefuellingStationMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var totalStations = await _context.Stations.AsNoTracking().CountAsync(s => s.Type == StationType.RefuellingStation, cancellationToken);
        var refuels = await GetRefuelsInWindowAsync(startDate, cancellationToken);

        return new RefuellingStationFleetMetricsDto
        {
            TotalCost = refuels.Sum(GetRefuelCost),
            TotalRefuellingStations = totalStations,
            StationsActiveInPeriod = refuels.Select(r => r.StationId!.Value).Distinct().Count(),
            RefuelEventCount = refuels.Count,
            TrucksServiced = refuels.Select(r => r.TruckId).Distinct().Count()
        };
    }

    public async Task<List<RefuellingStationPerformanceRowDto>> GetRefuellingStationPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var stations = await _context.Stations.AsNoTracking().Where(s => s.Type == StationType.RefuellingStation).ToListAsync(cancellationToken);
        var refuels = await GetRefuelsInWindowAsync(startDate, cancellationToken);

        var rows = new List<RefuellingStationPerformanceRowDto>();
        foreach (var station in stations)
        {
            var stationRefuels = refuels.Where(r => r.StationId == station.Id).ToList();
            if (stationRefuels.Count == 0) continue;

            rows.Add(new RefuellingStationPerformanceRowDto
            {
                StationId = station.Id,
                StationName = station.Name,
                RefuelEventCount = stationRefuels.Count,
                TrucksServiced = stationRefuels.Select(r => r.TruckId).Distinct().Count(),
                TotalQuantity = stationRefuels.Sum(r => r.Quantity),
                Unit = stationRefuels.Select(r => r.Unit.ToString()).FirstOrDefault() ?? "",
                TotalCost = stationRefuels.Sum(GetRefuelCost)
            });
        }

        // Share of total refuelling cost, not raw quantity — stays comparable across stations
        // even when they refuel in different units (SCM/LTR/KG/MT). Same "share of the total"
        // convention as the Discharge/Loading Depot reports.
        var totalCost = rows.Sum(r => r.TotalCost);
        foreach (var row in rows)
        {
            row.CostSharePercent = totalCost > 0 ? row.TotalCost / totalCost * 100 : 0;
        }

        return rows.OrderByDescending(r => r.RefuelEventCount).ToList();
    }

    public async Task<List<RefuellingStationMonthlyTrendDto>> GetRefuellingStationMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        var monthsBack = Math.Max(1, months);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var earliestMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(monthsBack - 1));

        var refuels = await GetRefuelsInWindowAsync(earliestMonth, cancellationToken);

        var results = new List<RefuellingStationMonthlyTrendDto>();
        for (var i = 0; i < monthsBack; i++)
        {
            var bucket = earliestMonth.AddMonths(i);
            var monthRefuels = refuels
                .Where(r => r.Date.Year == bucket.Year && r.Date.Month == bucket.Month)
                .ToList();

            results.Add(new RefuellingStationMonthlyTrendDto
            {
                Month = bucket.Month,
                Year = bucket.Year,
                RefuelEventCount = monthRefuels.Count,
                TotalCost = monthRefuels.Sum(GetRefuelCost)
            });
        }

        return results;
    }

    // RefuelInfo.Date is a plain DateOnly (no jsonb wrapping like Trip's LoadingInfo), so this
    // windows directly on it rather than going through ToWindowStart's DateTimeOffset
    // conversion. Only records with a StationId are relevant — the entry form always sets one
    // (restricted to RefuellingStation-typed stations), but the column is nullable.
    private async Task<List<RefuelInfo>> GetRefuelsInWindowAsync(DateOnly? startDate, CancellationToken cancellationToken)
    {
        var query = _context.RefuelInfos.AsNoTracking().Where(r => r.StationId.HasValue).AsQueryable();

        if (startDate.HasValue)
            query = query.Where(r => r.Date >= startDate.Value);

        return await query.ToListAsync(cancellationToken);
    }

    // RefuelInfo.Price is a per-unit price, not the event's total cost.
    private static decimal GetRefuelCost(RefuelInfo r) => r.Quantity * (r.Price ?? 0);

    // ─── Shared helpers ──────────────────────────────────────────────────────

    // null startDate ("All Time") means no lower bound at all.
    private static DateTimeOffset? ToWindowStart(DateOnly? startDate) =>
        startDate.HasValue ? new DateTimeOffset(startDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    // Trips are classified by the month they were loaded, not dispatched — matches
    // TruckReportService's convention. Only trips with at least one discharge are relevant
    // to a station report.
    private async Task<List<Trip>> GetTripsInWindowAsync(DateTimeOffset? windowStart, CancellationToken cancellationToken)
    {
        var query = _context.Trips.AsNoTracking().Where(t => t.Discharges.Any())
            .Include(t => t.Truck).Include(t => t.Discharges).AsSplitQuery().AsQueryable();

        if (windowStart.HasValue)
            query = query.Where(t => t.LoadingInfo.LoadingDate.HasValue && t.LoadingInfo.LoadingDate >= windowStart.Value);

        return await query.ToListAsync(cancellationToken);
    }

    // No caching, matching AppSettingsController's own read pattern — this is a low-traffic
    // settings row, not worth a cache invalidation story yet.
    private async Task<bool> GetExcludeCngSettingAsync(CancellationToken cancellationToken)
    {
        var settings = await _context.AppSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return settings?.ExcludeCngFromShortage ?? false;
    }

    private static IEnumerable<Trip> ApplyCngExclusion(IEnumerable<Trip> trips, bool excludeCng) =>
        excludeCng ? trips.Where(t => !(t.Truck?.Product?.IsCng() ?? false)) : trips;

    private static decimal GetShortageAmount(Trip trip)
    {
        var loadingQty = trip.LoadingInfo.Quantity ?? 0;
        var discharged = trip.Discharges.Sum(d => d.QuantityDischarged);
        return Math.Max(0, loadingQty - discharged);
    }

    // Shortage rate as % of volume (shortage / loaded quantity) — matches TruckReportService's
    // convention.
    private static decimal CalculateShortageRate(IEnumerable<Trip> dischargedTrips)
    {
        var trips = dischargedTrips as ICollection<Trip> ?? dischargedTrips.ToList();
        var totalLoaded = trips.Sum(t => t.LoadingInfo.Quantity ?? 0);
        var totalShortage = trips.Sum(GetShortageAmount);
        return totalLoaded > 0 ? totalShortage / totalLoaded * 100 : 0;
    }
}
