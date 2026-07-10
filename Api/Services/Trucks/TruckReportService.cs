using Api.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Interfaces.Trucks;
using Shared.Models.Trips;
using Shared.Models.Trucks;

namespace Api.Services.Trucks;

public class TruckReportService : ITruckReportService
{
    private readonly AppDbContext _context;

    public TruckReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<TruckFleetReportMetricsDto> GetMetricsAsync(DateOnly? startDate = null, string? product = "All", CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);

        var truckQuery = FilterByProduct(_context.Trucks.AsNoTracking(), product);
        var totalTrucksInFleet = await truckQuery.CountAsync(cancellationToken);
        var totalDeployedTrucks = await truckQuery.CountAsync(t => t.IsActive, cancellationToken);

        var periodTrips = await GetTripsInWindowAsync(windowStart, product, cancellationToken);
        var dischargedTrips = periodTrips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)).ToList();
        var durations = GetValidDurations(periodTrips);

        var loadingQuery = FilterByProduct(_context.Trips.AsNoTracking().Where(t => t.LoadingInfo.LoadingDate.HasValue), product);
        if (windowStart.HasValue)
            loadingQuery = loadingQuery.Where(t => t.LoadingInfo.LoadingDate >= windowStart.Value);
        var trucksLoadedInPeriod = await loadingQuery.Select(t => t.TruckId).Distinct().CountAsync(cancellationToken);

        // A truck within the deployed (IsActive) set can only be "unavailable" by being under
        // repair — a truck that's actually Out of Service is IsActive=false and isn't part of
        // totalDeployedTrucks to begin with, so it doesn't need separate subtraction here.
        var deployedTruckIds = await truckQuery.Where(t => t.IsActive).Select(t => t.Id).ToListAsync(cancellationToken);
        var underRepairCount = await _context.ServiceRequest.AsNoTracking()
            .Where(s => s.Status == RequestStatus.InProgress && s.TruckId.HasValue && deployedTruckIds.Contains(s.TruckId.Value))
            .Select(s => s.TruckId).Distinct().CountAsync(cancellationToken);

        var productTruckIds = IsAllProducts(product) ? null : await truckQuery.Select(t => t.Id).ToListAsync(cancellationToken);

        var incidentsQuery = _context.Incidents.AsNoTracking().AsQueryable();
        if (productTruckIds is not null)
            incidentsQuery = incidentsQuery.Where(i => productTruckIds.Contains(i.TruckId));
        var openIncidents = await incidentsQuery.CountAsync(i => i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed, cancellationToken);

        var serviceRequestQuery = _context.ServiceRequest.AsNoTracking().AsQueryable();
        if (productTruckIds is not null)
            serviceRequestQuery = serviceRequestQuery.Where(s => s.TruckId.HasValue && productTruckIds.Contains(s.TruckId.Value));
        var openServiceRequests = await serviceRequestQuery.CountAsync(s => s.Status != RequestStatus.Closed, cancellationToken);

        var closedServiceRequestQuery = serviceRequestQuery.Where(s => s.ClosedAt.HasValue);
        if (windowStart.HasValue)
            closedServiceRequestQuery = closedServiceRequestQuery.Where(s => s.CreatedAt >= windowStart.Value);
        var closedServiceRequests = await closedServiceRequestQuery
            .Select(s => new { s.CreatedAt, s.ClosedAt })
            .ToListAsync(cancellationToken);

        return new TruckFleetReportMetricsDto
        {
            FleetUtilizationRate = totalDeployedTrucks > 0 ? (decimal)trucksLoadedInPeriod / totalDeployedTrucks * 100 : 0,
            FleetAvailabilityRate = totalDeployedTrucks > 0 ? (decimal)(totalDeployedTrucks - underRepairCount) / totalDeployedTrucks * 100 : 0,
            AvgTurnaroundDays = durations.Count > 0 ? (decimal)durations.Average() : 0,
            ShortageRate = CalculateShortageRate(dischargedTrips),

            TotalTrucksInFleet = totalTrucksInFleet,
            TotalDeployedTrucks = totalDeployedTrucks,
            TrucksLoadedInPeriod = trucksLoadedInPeriod,
            TripsInPeriod = periodTrips.Count,
            TotalQuantityLoaded = periodTrips.Sum(t => t.LoadingInfo.Quantity ?? 0),
            OpenServiceRequests = openServiceRequests,
            AvgMaintenanceTatDays = closedServiceRequests.Count > 0
                ? (decimal)closedServiceRequests.Average(s => (s.ClosedAt!.Value - s.CreatedAt).TotalDays)
                : 0,
            OpenIncidents = openIncidents
        };
    }

    public async Task<List<TruckStatusBreakdownDto>> GetStatusBreakdownAsync(string? product = "All", CancellationToken cancellationToken = default)
    {
        var statuses = await GetTruckStatusesAsync(product, cancellationToken);

        return statuses
            .GroupBy(s => s.Status)
            .Select(g => new TruckStatusBreakdownDto { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();
    }

    public async Task<List<TruckPerformanceRowDto>> GetTruckPerformanceAsync(DateOnly? startDate = null, string? product = "All", CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);

        var trucks = await FilterByProduct(_context.Trucks.AsNoTracking().Include(t => t.Driver), product)
            .ToListAsync(cancellationToken);

        var statusByTruckId = (await GetTruckStatusesAsync(product, cancellationToken))
            .ToDictionary(s => s.TruckId, s => s.Status);

        var periodTrips = await GetTripsInWindowAsync(windowStart, product, cancellationToken);
        var tripsByTruckId = periodTrips.GroupBy(t => t.TruckId).ToDictionary(g => g.Key, g => g.ToList());

        var openServiceRequestCounts = await _context.ServiceRequest.AsNoTracking()
            .Where(s => s.Status != RequestStatus.Closed)
            .GroupBy(s => s.TruckId)
            .Select(g => new { TruckId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TruckId, x => x.Count, cancellationToken);

        var openIncidentCounts = await _context.Incidents.AsNoTracking()
            .Where(i => i.Status != IncidentStatus.Resolved && i.Status != IncidentStatus.Closed)
            .GroupBy(i => i.TruckId)
            .Select(g => new { TruckId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TruckId, x => x.Count, cancellationToken);

        var rows = new List<TruckPerformanceRowDto>();
        foreach (var truck in trucks)
        {
            var trips = tripsByTruckId.GetValueOrDefault(truck.Id, []);
            var dischargedTrips = trips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)).ToList();
            var durations = GetValidDurations(trips);

            var openServiceRequests = openServiceRequestCounts.GetValueOrDefault(truck.Id, 0);
            var openIncidents = openIncidentCounts.GetValueOrDefault(truck.Id, 0);
            var status = statusByTruckId.GetValueOrDefault(truck.Id, "Available");

            rows.Add(new TruckPerformanceRowDto
            {
                TruckId = truck.Id,
                TruckNo = truck.TruckNo,
                Product = truck.Product?.ToString(),
                Status = status,
                DriverName = truck.Driver is not null ? $"{truck.Driver.FirstName} {truck.Driver.LastName}".Trim() : null,
                TripCount = trips.Count,
                TotalQuantity = trips.Sum(t => t.LoadingInfo.Quantity ?? 0),
                ShortageRate = CalculateShortageRate(dischargedTrips),
                AvgTurnaroundDays = durations.Count > 0 ? (decimal)durations.Average() : 0,
                OpenServiceRequests = openServiceRequests,
                OpenIncidents = openIncidents,
                NeedsAttention = status is "Out of Service" or "Under Repair" || openServiceRequests > 0 || openIncidents > 0
            });
        }

        return rows.OrderByDescending(r => r.TripCount).ToList();
    }

    public async Task<List<MaintenanceSpendByTruckDto>> GetMaintenanceSpendByTruckAsync(DateOnly? startDate = null, int count = 10, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var query = _context.ServiceRequest.AsNoTracking().Include(s => s.Truck)
            .Where(s => s.Cost.HasValue && s.Cost > 0);
        if (windowStart.HasValue)
            query = query.Where(s => s.CreatedAt >= windowStart.Value);

        var requests = await query.ToListAsync(cancellationToken);

        return requests
            .GroupBy(s => s.Truck?.TruckNo ?? "Unknown")
            .Select(g =>
            {
                var tats = g.Where(s => s.ClosedAt.HasValue).Select(s => (s.ClosedAt!.Value - s.CreatedAt).TotalDays).ToList();
                return new MaintenanceSpendByTruckDto
                {
                    TruckNo = g.Key,
                    TotalCost = g.Sum(s => s.Cost ?? 0),
                    RequestCount = g.Count(),
                    AvgTatDays = tats.Count > 0 ? (decimal)tats.Average() : 0
                };
            })
            .OrderByDescending(x => x.TotalCost)
            .Take(count)
            .ToList();
    }

    public async Task<List<MaintenanceSpendByCategoryDto>> GetMaintenanceSpendByCategoryAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var query = _context.ServiceRequest.AsNoTracking().Where(s => s.Cost.HasValue && s.Cost > 0);
        if (windowStart.HasValue)
            query = query.Where(s => s.CreatedAt >= windowStart.Value);

        var requests = await query.ToListAsync(cancellationToken);

        return requests
            .GroupBy(s => $"{s.Type} - {s.Item}")
            .Select(g => new MaintenanceSpendByCategoryDto
            {
                Category = g.Key,
                TotalCost = g.Sum(s => s.Cost ?? 0),
                RequestCount = g.Count()
            })
            .OrderByDescending(x => x.TotalCost)
            .ToList();
    }

    public async Task<List<CalibrationExpiryDto>> GetCalibrationExpiryAsync(int withinDays = 30, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var cutoff = today.AddDays(withinDays);

        // Includes already-expired trucks too (negative DaysUntilExpiry) — overdue calibration is
        // at least as important to surface as "expiring soon."
        var trucks = await _context.Trucks.AsNoTracking()
            .Where(t => t.ExpiryDate.HasValue && t.ExpiryDate.Value <= cutoff)
            .OrderBy(t => t.ExpiryDate)
            .ToListAsync(cancellationToken);

        return trucks.Select(t => new CalibrationExpiryDto
        {
            TruckNo = t.TruckNo,
            ExpiryDate = t.ExpiryDate!.Value,
            DaysUntilExpiry = t.ExpiryDate.Value.DayNumber - today.DayNumber
        }).ToList();
    }

    public async Task<List<FleetMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, string? product = "All", CancellationToken cancellationToken = default)
    {
        var monthsBack = Math.Max(1, months);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var earliestMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(monthsBack - 1));
        var windowStart = new DateTimeOffset(earliestMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var trips = await GetTripsInWindowAsync(windowStart, product, cancellationToken);
        var totalDeployedTrucks = await FilterByProduct(_context.Trucks.AsNoTracking(), product)
            .CountAsync(t => t.IsActive, cancellationToken);

        var results = new List<FleetMonthlyTrendDto>();
        for (var i = 0; i < monthsBack; i++)
        {
            var bucket = earliestMonth.AddMonths(i);
            var monthTrips = trips
                .Where(t => t.LoadingInfo.LoadingDate!.Value.Year == bucket.Year && t.LoadingInfo.LoadingDate!.Value.Month == bucket.Month)
                .ToList();
            var dischargedInMonth = monthTrips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)).ToList();
            var trucksLoadedInMonth = monthTrips.Select(t => t.TruckId).Distinct().Count();

            results.Add(new FleetMonthlyTrendDto
            {
                Month = bucket.Month,
                Year = bucket.Year,
                UtilizationRate = totalDeployedTrucks > 0 ? (decimal)trucksLoadedInMonth / totalDeployedTrucks * 100 : 0,
                ShortageRate = CalculateShortageRate(dischargedInMonth),
                TripCount = monthTrips.Count
            });
        }

        return results;
    }

    // ─── Shared helpers ──────────────────────────────────────────────────────

    private static bool IsAllProducts(string? product) => string.IsNullOrWhiteSpace(product) || product == "All";

    private static IQueryable<Shared.Models.Trucks.Truck> FilterByProduct(IQueryable<Shared.Models.Trucks.Truck> query, string? product) =>
        IsAllProducts(product) ? query : query.Where(t => t.Product != null && t.Product.ToString() == product);

    private static IQueryable<Trip> FilterByProduct(IQueryable<Trip> query, string? product) =>
        IsAllProducts(product) ? query : query.Where(t => t.Truck != null && t.Truck.Product != null && t.Truck.Product.ToString() == product);

    // null startDate ("All Time") means no lower bound at all.
    private static DateTimeOffset? ToWindowStart(DateOnly? startDate) =>
        startDate.HasValue ? new DateTimeOffset(startDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    // Trips are classified by the month they were loaded, not dispatched — matches
    // ControlRoomService's convention (a trip dispatched in one month can load in the next, and
    // company standards attribute it to the loading month).
    private async Task<List<Trip>> GetTripsInWindowAsync(DateTimeOffset? windowStart, string? product, CancellationToken cancellationToken)
    {
        var query = FilterByProduct(
            _context.Trips.AsNoTracking().Include(t => t.Truck).Include(t => t.Discharges).AsSplitQuery(),
            product);

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
    // shortage — matches ControlRoomService's convention.
    private static decimal CalculateShortageRate(IEnumerable<Trip> dischargedTrips)
    {
        var trips = dischargedTrips as ICollection<Trip> ?? dischargedTrips.ToList();
        var totalLoaded = trips.Sum(t => t.LoadingInfo.Quantity ?? 0);
        var totalShortage = trips.Sum(GetShortageAmount);
        return totalLoaded > 0 ? totalShortage / totalLoaded * 100 : 0;
    }

    // A trip can't close before it was dispatched — a negative value means bad data (e.g. a
    // mistyped ReturnDateTime), not a real duration. Same guard as ControlRoomService.
    private static List<int> GetValidDurations(IEnumerable<Trip> trips) =>
        trips
            .Where(t => t.CloseInfo.ReturnDateTime.HasValue)
            .Select(t => t.CalculateTripDuration(t.Date, t.CloseInfo.ReturnDateTime!.Value))
            .Where(d => d >= 0)
            .ToList();

    private async Task<List<(Guid TruckId, string Status)>> GetTruckStatusesAsync(string? product, CancellationToken cancellationToken)
    {
        var trucks = await FilterByProduct(
                _context.Trucks.AsNoTracking()
                    .Include(t => t.Trips!.Where(tr => tr.Status == TripStatus.Active || tr.Status == TripStatus.Dispatched)
                        .OrderByDescending(tr => tr.Date))
                        .ThenInclude(tr => tr.Discharges)
                    .AsSplitQuery(),
                product)
            .ToListAsync(cancellationToken);

        var truckIdsWithClosedTrip = (await _context.Trips.AsNoTracking()
            .Where(t => t.Status == TripStatus.Closed)
            .Select(t => t.TruckId)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var truckIdsUnderRepair = (await _context.ServiceRequest.AsNoTracking()
            .Where(s => s.Status == RequestStatus.InProgress)
            .Select(s => s.TruckId)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();

        return trucks.Select(truck =>
        {
            var currentTrip = truck.Trips?.FirstOrDefault();
            var status = DeriveStatus(truck, currentTrip, truckIdsWithClosedTrip.Contains(truck.Id), truckIdsUnderRepair.Contains(truck.Id));
            return (truck.Id, status);
        }).ToList();
    }

    // The 9-step operational state machine, ordered so an in-progress trip's real status always
    // wins over the idle-state fallbacks (Out of Service / Under Repair / Awaiting / Available).
    private static string DeriveStatus(Shared.Models.Trucks.Truck truck, Trip? currentTrip, bool hasClosedTrip, bool isUnderRepair)
    {
        if (currentTrip is not null)
        {
            var hasWaybill = !string.IsNullOrWhiteSpace(currentTrip.LoadingInfo?.WaybillNo);
            if (!hasWaybill)
                return "Dispatched to Loading";

            var arrived = currentTrip.ArrivalInfo?.ArrivedAtStation == true || currentTrip.ArrivalInfo?.ArrivedDepot == true;
            if (!arrived)
                return "Delivery Trip to Station";

            if (currentTrip.LoadingInfo?.DispatchType == DispatchType.Depot)
            {
                if (currentTrip.ArrivalInfo?.ArrivedDepot == true && currentTrip.ArrivalInfo?.InvoiceIssued != true)
                    return "Arrived at Depot";

                if (currentTrip.ArrivalInfo?.InvoiceIssued == true && !currentTrip.Discharges.Any())
                    return "Invoiced to Station";
            }

            if (!currentTrip.Discharges.Any())
                return "Arrived at Discharge";

            if (!currentTrip.Discharges.Any(d => d.IsFinalDischarge))
                return "Discharging";

            return "Return Trip";
        }

        if (!truck.IsActive)
            return "Out of Service";

        if (isUnderRepair)
            return "Under Repair";

        return hasClosedTrip ? "Awaiting Loading" : "Available";
    }
}
