using Api.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Extensions;
using Shared.Interfaces.Drivers;
using Shared.Models.Drivers;
using Shared.Models.Trips;

namespace Api.Services.Drivers;

public class DriverReportService : IDriverReportService
{
    private readonly AppDbContext _context;

    public DriverReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<DriverFleetMetricsDto> GetMetricsAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var totalDrivers = await _context.Drivers.AsNoTracking().CountAsync(cancellationToken);

        var periodTrips = await GetTripsInWindowAsync(windowStart, cancellationToken);
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);
        var dischargedTrips = ApplyCngExclusion(
            periodTrips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)), excludeCng).ToList();
        var durations = GetValidDurations(periodTrips);
        var ratedTrips = periodTrips.Where(t => t.Status == TripStatus.Completed && t.CloseInfo.Rating > 0).ToList();

        var driversActiveInPeriod = periodTrips.Where(t => t.DriverId.HasValue).Select(t => t.DriverId!.Value).Distinct().Count();

        var onTripCount = await _context.Trips.AsNoTracking()
            .Where(t => t.DriverId.HasValue && (t.Status == TripStatus.Active || t.Status == TripStatus.Dispatched))
            .Select(t => t.DriverId!.Value).Distinct().CountAsync(cancellationToken);

        var expiryCounts = await _context.Drivers.AsNoTracking()
            .Where(d => d.ExpiryDate.HasValue)
            .Select(d => d.ExpiryDate!.Value)
            .ToListAsync(cancellationToken);
        var expiredLicenses = expiryCounts.Count(e => e.DayNumber < today.DayNumber);
        var licensesExpiringSoon = expiryCounts.Count(e => e.DayNumber >= today.DayNumber && e.DayNumber - today.DayNumber <= 30);

        return new DriverFleetMetricsDto
        {
            DriverUtilizationRate = totalDrivers > 0 ? (decimal)driversActiveInPeriod / totalDrivers * 100 : 0,
            AvgRating = ratedTrips.Count > 0 ? (decimal)ratedTrips.Average(t => t.CloseInfo.Rating) : 0,
            AvgTurnaroundDays = durations.Count > 0 ? (decimal)durations.Average() : 0,
            ShortageRate = CalculateShortageRate(dischargedTrips),

            TotalDrivers = totalDrivers,
            DriversActiveInPeriod = driversActiveInPeriod,
            OnTripCount = onTripCount,
            TripsInPeriod = periodTrips.Count,
            LicensesExpiringSoon = licensesExpiringSoon,
            ExpiredLicenses = expiredLicenses
        };
    }

    public async Task<List<DriverPerformanceRowDto>> GetDriverPerformanceAsync(DateOnly? startDate = null, CancellationToken cancellationToken = default)
    {
        var windowStart = ToWindowStart(startDate);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var drivers = await _context.Drivers.AsNoTracking().ToListAsync(cancellationToken);

        var periodTrips = await GetTripsInWindowAsync(windowStart, cancellationToken);
        var tripsByDriverId = periodTrips.Where(t => t.DriverId.HasValue)
            .GroupBy(t => t.DriverId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var excludeCng = await GetExcludeCngSettingAsync(cancellationToken);

        var driverIdsOnTrip = (await _context.Trips.AsNoTracking()
            .Where(t => t.DriverId.HasValue && (t.Status == TripStatus.Active || t.Status == TripStatus.Dispatched))
            .Select(t => t.DriverId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var rows = new List<DriverPerformanceRowDto>();
        foreach (var driver in drivers)
        {
            var trips = tripsByDriverId.GetValueOrDefault(driver.Id, []);
            var dischargedTrips = ApplyCngExclusion(
                trips.Where(t => t.Discharges.Any(d => d.IsFinalDischarge)), excludeCng).ToList();
            var durations = GetValidDurations(trips);
            var ratedTrips = trips.Where(t => t.Status == TripStatus.Completed && t.CloseInfo.Rating > 0).ToList();
            var licenseStatus = GetLicenseStatus(driver.ExpiryDate, today);
            var currentStatus = driverIdsOnTrip.Contains(driver.Id) ? "On Trip" : "Available";

            rows.Add(new DriverPerformanceRowDto
            {
                DriverId = driver.Id,
                FullName = driver.ToString(),
                LicenseNumber = driver.LicenseNo,
                LicenseStatus = licenseStatus,
                CurrentStatus = currentStatus,
                TripCount = trips.Count,
                AvgRating = ratedTrips.Count > 0 ? (decimal)ratedTrips.Average(t => t.CloseInfo.Rating) : 0,
                ShortageRate = CalculateShortageRate(dischargedTrips),
                AvgTurnaroundDays = durations.Count > 0 ? (decimal)durations.Average() : 0,
                NeedsAttention = licenseStatus is "Expired" or "Expiring Soon" || trips.Count == 0
            });
        }

        return rows.OrderByDescending(r => r.TripCount).ToList();
    }

    public async Task<List<DriverLicenseExpiryDto>> GetLicenseExpiryAsync(int withinDays = 30, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var cutoff = today.AddDays(withinDays);

        // Includes already-expired drivers too (negative DaysUntilExpiry) — overdue license
        // renewal is at least as important to surface as "expiring soon."
        var drivers = await _context.Drivers.AsNoTracking()
            .Where(d => d.ExpiryDate.HasValue && d.ExpiryDate.Value <= cutoff)
            .OrderBy(d => d.ExpiryDate)
            .ToListAsync(cancellationToken);

        return drivers.Select(d => new DriverLicenseExpiryDto
        {
            DriverId = d.Id,
            DriverName = d.ToString(),
            LicenseNumber = d.LicenseNo,
            ExpiryDate = d.ExpiryDate!.Value,
            DaysUntilExpiry = d.ExpiryDate.Value.DayNumber - today.DayNumber
        }).ToList();
    }

    public async Task<List<DriverMonthlyTrendDto>> GetMonthlyTrendAsync(int months = 6, CancellationToken cancellationToken = default)
    {
        var monthsBack = Math.Max(1, months);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var earliestMonth = new DateOnly(today.Year, today.Month, 1).AddMonths(-(monthsBack - 1));
        var windowStart = new DateTimeOffset(earliestMonth.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        var trips = await GetTripsInWindowAsync(windowStart, cancellationToken);

        var results = new List<DriverMonthlyTrendDto>();
        for (var i = 0; i < monthsBack; i++)
        {
            var bucket = earliestMonth.AddMonths(i);
            var monthTrips = trips
                .Where(t => t.LoadingInfo.LoadingDate!.Value.Year == bucket.Year && t.LoadingInfo.LoadingDate!.Value.Month == bucket.Month)
                .ToList();
            var ratedTrips = monthTrips.Where(t => t.Status == TripStatus.Completed && t.CloseInfo.Rating > 0).ToList();

            results.Add(new DriverMonthlyTrendDto
            {
                Month = bucket.Month,
                Year = bucket.Year,
                TripCount = monthTrips.Count,
                AvgRating = ratedTrips.Count > 0 ? (decimal)ratedTrips.Average(t => t.CloseInfo.Rating) : 0
            });
        }

        return results;
    }

    // ─── Shared helpers ──────────────────────────────────────────────────────

    // null startDate ("All Time") means no lower bound at all.
    private static DateTimeOffset? ToWindowStart(DateOnly? startDate) =>
        startDate.HasValue ? new DateTimeOffset(startDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null;

    // Trips are classified by the month they were loaded, not dispatched — matches
    // TruckReportService's convention.
    private async Task<List<Trip>> GetTripsInWindowAsync(DateTimeOffset? windowStart, CancellationToken cancellationToken)
    {
        var query = _context.Trips.AsNoTracking().Where(t => t.DriverId.HasValue)
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

    // A trip can't close before it was dispatched — a negative value means bad data, not a
    // real duration. Same guard as TruckReportService.
    private static List<int> GetValidDurations(IEnumerable<Trip> trips) =>
        trips
            .Where(t => t.CloseInfo.ReturnDateTime.HasValue)
            .Select(t => t.CalculateTripDuration(t.Date, t.CloseInfo.ReturnDateTime!.Value))
            .Where(d => d >= 0)
            .ToList();

    private static string GetLicenseStatus(DateOnly? expiryDate, DateOnly today)
    {
        if (!expiryDate.HasValue) return "Not Set";
        var days = expiryDate.Value.DayNumber - today.DayNumber;
        if (days < 0) return "Expired";
        if (days <= 30) return "Expiring Soon";
        return "Valid";
    }
}
