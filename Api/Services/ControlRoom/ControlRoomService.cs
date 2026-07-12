using Api.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Interfaces.ControlRoom;
using Shared.Models.ControlRoom;
using Shared.Models.Trips;

namespace Api.Services.ControlRoom;

public class ControlRoomService : IControlRoomService
{
    private readonly AppDbContext _context;

    // A trip with no stored SLA/ETA is treated as "on time" if it closed within this many days —
    // matches the informal 7-day rule the ops team already uses when reading trip durations.
    private const int OnTimeThresholdDays = 7;

    public ControlRoomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ControlRoomMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var today = DateTimeOffset.UtcNow.Date;

        var totalTrucksInFleet = await _context.Trucks.AsNoTracking().CountAsync(cancellationToken);
        var totalDeployedTrucks = await _context.Trucks.AsNoTracking().CountAsync(t => t.IsActive, cancellationToken);

        var activeTrips = await _context.Trips.AsNoTracking()
            .Where(t => t.Status == TripStatus.Active)
            .ToListAsync(cancellationToken);

        // Matches Dashboard's TrucksLoadedInPeriod: distinct trucks with an actual loading (by
        // LoadingInfo.LoadingDate) within the selected filter window, not trucks on an active trip.
        var loadingQuery = _context.Trips.AsNoTracking().Where(t => t.LoadingInfo.LoadingDate.HasValue);
        if (windowStart.HasValue)
            loadingQuery = loadingQuery.Where(t => t.LoadingInfo.LoadingDate >= windowStart.Value);
        var trucksLoadedInPeriod = await loadingQuery.Select(t => t.TruckId).Distinct().CountAsync(cancellationToken);

        var periodTrips = await GetTripsInWindowAsync(windowStart, cancellationToken);

        var closedPeriodTrips = periodTrips.Where(t => t.CloseInfo.ReturnDateTime.HasValue).ToList();
        var durations = closedPeriodTrips
            .Select(t => t.CalculateTripDuration(t.Date, t.CloseInfo.ReturnDateTime!.Value))
            // A trip can't close before it was dispatched — a negative value means bad data (e.g.
            // a mistyped ReturnDateTime), not a real duration. One such row with a year typo
            // (0225 instead of 2025) alone was enough to drag "All Time" avg duration to -69.9d.
            .Where(d => d >= 0)
            .ToList();

        var dischargedTrips = periodTrips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)).ToList();

        var incidentsQuery = _context.Incidents.AsNoTracking().AsQueryable();
        if (windowStart.HasValue)
            incidentsQuery = incidentsQuery.Where(i => i.CreatedAt >= windowStart.Value);
        var incidentsInPeriod = await incidentsQuery.CountAsync(cancellationToken);

        var openIncidents = await _context.Incidents.AsNoTracking()
            .CountAsync(i => i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed, cancellationToken);

        var openServiceRequests = await _context.ServiceRequest.AsNoTracking()
            .CountAsync(s => s.Status != RequestStatus.Closed, cancellationToken);

        var serviceRequestQuery = _context.ServiceRequest.AsNoTracking().Where(s => s.ClosedAt.HasValue);
        if (windowStart.HasValue)
            serviceRequestQuery = serviceRequestQuery.Where(s => s.CreatedAt >= windowStart.Value);
        var closedServiceRequests = await serviceRequestQuery
            .Select(s => new { s.CreatedAt, s.ClosedAt })
            .ToListAsync(cancellationToken);

        return new ControlRoomMetricsDto
        {
            TotalTrucksInFleet = totalTrucksInFleet,
            TotalDeployedTrucks = totalDeployedTrucks,
            TrucksLoadedInPeriod = trucksLoadedInPeriod,
            TruckUtilizationRate = totalDeployedTrucks > 0 ? (decimal)trucksLoadedInPeriod / totalDeployedTrucks * 100 : 0,

            ActiveTrips = activeTrips.Count,
            TripsToday = periodTrips.Count(t => t.Date.Date == today),
            TripsInPeriod = periodTrips.Count,
            TripCompletionRate = periodTrips.Count > 0
                ? (decimal)periodTrips.Count(t => t.Status is TripStatus.Closed or TripStatus.Completed) / periodTrips.Count * 100
                : 0,

            AvgTripDurationDays = durations.Count > 0 ? (decimal)durations.Average() : 0,
            ShortageRate = CalculateShortageRate(dischargedTrips),
            OnTimeDeliveryRate = durations.Count > 0
                ? (decimal)durations.Count(d => d <= OnTimeThresholdDays) / durations.Count * 100
                : 0,

            OpenIncidents = openIncidents,
            IncidentsInPeriod = incidentsInPeriod,
            IncidentRate = periodTrips.Count > 0 ? (decimal)incidentsInPeriod / periodTrips.Count * 100 : 0,

            OpenServiceRequests = openServiceRequests,
            AvgMaintenanceTatDays = closedServiceRequests.Count > 0
                ? (decimal)closedServiceRequests.Average(s => (s.ClosedAt!.Value - s.CreatedAt).TotalDays)
                : 0
        };
    }

    public async Task<List<ProductBreakdownDto>> GetProductBreakdownAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var periodTrips = await GetTripsInWindowAsync(ToWindowStart(startDate), cancellationToken);

        return periodTrips
            .Where(t => t.Truck?.Product is not null)
            .GroupBy(t => t.Truck!.Product!.Value)
            .Select(g =>
            {
                var dischargedInGroup = g.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)).ToList();
                return new ProductBreakdownDto
                {
                    Product = g.Key.ToString(),
                    TripCount = g.Count(),
                    TotalQuantity = g.Sum(t => t.LoadingInfo.Quantity ?? 0),
                    ShortageRate = CalculateShortageRate(dischargedInGroup),
                    TotalShortageQuantity = dischargedInGroup.Sum(GetShortageAmount)
                };
            })
            .OrderByDescending(x => x.TripCount)
            .ToList();
    }

    public async Task<List<ProductLeaderDto>> GetProductLeadersAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var perTruckRows = await GetPerTruckProductRowsAsync(startDate, cancellationToken);

        return perTruckRows
            .GroupBy(x => x.Product)
            // Leader = most trips; ties broken by lowest shortage rate, then shortest avg turnaround.
            .Select(g => g
                .OrderByDescending(x => x.TripCount)
                .ThenBy(x => x.ShortageRate)
                .ThenBy(x => x.AvgTripDurationDays)
                .First())
            .OrderBy(x => x.Product)
            .ToList();
    }

    public async Task<List<ProductLeaderDto>> GetProductLaggardsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var perTruckRows = await GetPerTruckProductRowsAsync(startDate, cancellationToken);

        return perTruckRows
            .GroupBy(x => x.Product)
            // Laggard = highest shortage rate — the metric that actually signals a problem truck,
            // not simply "fewest trips" (a lightly-used truck isn't a bad one). Ties broken by
            // most trips, so a recurring issue ranks above a single unlucky delivery.
            .Select(g => g
                .OrderByDescending(x => x.ShortageRate)
                .ThenByDescending(x => x.TripCount)
                .First())
            .OrderBy(x => x.Product)
            .ToList();
    }

    // Shared by GetProductLeadersAsync/GetProductLaggardsAsync — one row per (product, truck)
    // pair for the selected period; each method then picks the best/worst row per product.
    private async Task<List<ProductLeaderDto>> GetPerTruckProductRowsAsync(DateOnly? startDate, CancellationToken cancellationToken)
    {
        var periodTrips = await GetTripsInWindowAsync(ToWindowStart(startDate), cancellationToken);

        return periodTrips
            // Only closed trips count — an in-progress trip hasn't actually demonstrated
            // completeness, turnaround, or a real shortage outcome yet.
            .Where(t => t.Truck?.Product is not null && (t.Status == TripStatus.Closed || t.Status == TripStatus.Completed))
            .GroupBy(t => t.Truck!.Product!.Value)
            .SelectMany(productGroup => productGroup
                // Group by TruckId, not the Truck navigation entity — with AsNoTracking(), EF Core
                // materializes a fresh Truck instance per row even for the same truck, so grouping
                // by the entity itself (reference equality) put every trip in its own group of one.
                .GroupBy(t => t.TruckId)
                .Select(truckGroup =>
                {
                    var dischargedInGroup = truckGroup.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)).ToList();
                    var closedInGroup = truckGroup.Where(t => t.CloseInfo.ReturnDateTime.HasValue).ToList();
                    var groupDurations = closedInGroup
                        .Select(t => t.CalculateTripDuration(t.Date, t.CloseInfo.ReturnDateTime!.Value))
                        // See the same guard in GetMetricsAsync — a negative duration is bad data,
                        // not a real trip, and would otherwise corrupt this truck's avg/ranking.
                        .Where(d => d >= 0)
                        .ToList();

                    return new ProductLeaderDto
                    {
                        Product = productGroup.Key.ToString(),
                        TruckNo = truckGroup.First().Truck!.TruckNo,
                        TripCount = truckGroup.Count(),
                        TotalQuantityDispatched = truckGroup.Sum(t => t.LoadingInfo.Quantity ?? 0),
                        ShortageRate = CalculateShortageRate(dischargedInGroup),
                        AvgTripDurationDays = groupDurations.Count > 0 ? (decimal)groupDurations.Average() : 0
                    };
                }))
            .ToList();
    }

    public async Task<List<RecentIncidentDto>> GetRecentIncidentsAsync(int count = 8, CancellationToken cancellationToken = default)
    {
        return await _context.Incidents.AsNoTracking()
            .Include(i => i.Truck)
            .Include(i => i.IncidentType)
            .OrderByDescending(i => i.CreatedAt)
            .Take(count)
            .Select(i => new RecentIncidentDto
            {
                Id = i.Id,
                TruckNo = i.Truck != null ? i.Truck.TruckNo : "Unknown",
                IncidentType = i.IncidentType != null ? i.IncidentType.Type ?? "Unknown" : "Unknown",
                Status = i.Status.ToString(),
                CreatedAt = i.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    // null startDate ("All Time") means no lower bound at all.
    private static DateTimeOffset? ToWindowStart(DateOnly? startDate) =>
        startDate.HasValue ? new DateTimeOffset(startDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    private async Task<List<Trip>> GetTripsInWindowAsync(DateTimeOffset? windowStart, CancellationToken cancellationToken)
    {
        var query = _context.Trips.AsNoTracking()
            .Include(t => t.Truck)
            .Include(t => t.Discharges)
            .AsSplitQuery()
            .AsQueryable();

        // Trips are classified by the month they were loaded, not dispatched — a trip dispatched
        // in one month can load in the next, and company standards attribute it to the loading
        // month. "All Time" still includes everything, including trips not yet loaded.
        if (windowStart.HasValue)
            query = query.Where(t => t.LoadingInfo.LoadingDate.HasValue && t.LoadingInfo.LoadingDate >= windowStart.Value);

        return await query.ToListAsync(cancellationToken);
    }

    private static decimal GetShortageAmount(Trip trip)
    {
        var loadingQty = trip.LoadingInfo.Quantity ?? 0;
        var discharged = trip.Discharges.Sum(d => d.QuantityDischarged);
        return Math.Max(0, loadingQty - discharged);
    }

    // Shortage rate as % of volume (shortage / loaded quantity), not % of trips that had any
    // shortage — a 5-litre shortage on a 45,000-litre load shouldn't weigh the same as a trip
    // that came up short by half its load.
    private static decimal CalculateShortageRate(IEnumerable<Trip> dischargedTrips)
    {
        var trips = dischargedTrips as ICollection<Trip> ?? dischargedTrips.ToList();
        var totalLoaded = trips.Sum(t => t.LoadingInfo.Quantity ?? 0);
        var totalShortage = trips.Sum(GetShortageAmount);
        return totalLoaded > 0 ? totalShortage / totalLoaded * 100 : 0;
    }
}
