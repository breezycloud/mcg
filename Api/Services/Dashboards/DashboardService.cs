using System.Net.Http.Json;
using Api.Context;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;
using Shared.Interfaces.Dashboards;
using Shared.Models.Dashboards;
using Shared.Models.Drivers;
using Shared.Models.Trips;

namespace Api.Services.Dashboards;


public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(
        AppDbContext context,
        ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Core Metrics

    public async Task<DashboardMetricsDto> GetMetricsAsync(DateOnly? startDate, DateOnly? endDate, string? product = "All")
    {
        var trips = await GetFilteredTripsAsync(startDate, endDate, product);
        
        return new DashboardMetricsDto
        {
            TotalTrips = trips.Count,
            ActiveTrips = trips.Count(t => t.Status == TripStatus.Active),
            ClosedTrips = trips.Count(t => t.Status == TripStatus.Closed),
            AvgTripDurationDays = CalculateAverageTripDuration(trips),
            TotalDispatchedQuantity = trips.Sum(t => t.Origin!.Quantity!.Value),
            TotalShortage = trips.Sum(t => t.Destination!.ShortageAmount ?? 0)
        };
    }


    public async Task<MetricsTrendDto> GetMetricsTrendsAsync(DateOnly? startDate, DateOnly? endDate, string? product = "All")
    {
        var currentMetrics = await GetMetricsAsync(startDate, endDate, product);
        var (prevStart, prevEnd) = GetComparisonPeriod(startDate, endDate);
        var previousMetrics = await GetMetricsAsync(prevStart, prevEnd, product);

        return new MetricsTrendDto
        {
            TotalTripsTrend = CalculateTrend(currentMetrics.TotalTrips, previousMetrics.TotalTrips),
            ActiveTripsTrend = CalculateTrend(currentMetrics.ActiveTrips, previousMetrics.ActiveTrips),
            ClosedTripsTrend = CalculateTrend(currentMetrics.ClosedTrips, previousMetrics.ClosedTrips),
            AvgDurationTrend = CalculateTrend(currentMetrics.AvgTripDurationDays, previousMetrics.AvgTripDurationDays)
        };
    }

    #endregion

    #region Trip Analytics

    public async Task<TripStatusDistributionDto> GetTripStatusDistributionAsync(DateOnly? startDate, DateOnly? endDate, string? product = "All")
    {
        var trips = await GetFilteredTripsAsync(startDate, endDate, product);
        
        return new TripStatusDistributionDto
        {
            Active = trips.Count(t => t.Status == TripStatus.Active),
            Closed = trips.Count(t => t.Status == TripStatus.Closed),
            Overdue = trips.Count(t =>
                t.Status == TripStatus.Active)
            //&& t.ExpectedCompletionDate < DateTime.Now
        };
    }

    public async Task<List<TripMonthlySummaryDto>> GetTripMonthlySummaries(string? product = "All")
    {
        var now = DateTime.UtcNow;
        var startMonth = new DateOnly(now.Year, now.Month, 1).AddMonths(-4); // 5 months including current
        var endMonth = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));

        var trips = await GetFilteredTripsAsync(startMonth, endMonth, product);

        var summaries = trips
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .OrderByDescending(g => new DateTime(g.Key.Year, g.Key.Month, 1))
            .Take(5)
            .Select(g => new TripMonthlySummaryDto
            {
            Year = g.Key.Year,
            Month = g.Key.Month,
            TotalTrips = g.Count(),
            TotalQuantity = g.Sum(t => t.Origin!.Quantity!.Value),
            AvgDurationDays = g.Any(t => t.ReturnDate.HasValue)
                ? (decimal)g
                .Where(t => t.ReturnDate.HasValue)
                .Average(t => (t.ReturnDate!.Value.ToDateTime(TimeOnly.MinValue) - t.Date.ToDateTime(TimeOnly.MinValue)).TotalDays)
                : 0
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        return summaries;
    }

    public async Task<List<ProductShipmentDto>> GetProductShipmentsAsync(DateOnly? startDate, DateOnly? endDate, string? product = "All")
    {
        var trips = await GetFilteredTripsAsync(startDate, endDate, product);

        return trips
            .GroupBy(t => t.Truck!.Product!.Value)
            .Select(g => new ProductShipmentDto
            {
                Product = g.Key.ToString(),
                TotalTrips = g.Count(),
                TotalQuantity = g.Sum(t => t.Origin!.Quantity!.Value),
                Trend = CalculateProductTrend(g.Key, startDate, endDate)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .ToList();
    }

    public async Task<List<RecentTripDto>> GetRecentTripsAsync(int count, DateOnly? startDate, DateOnly? endDate, string? product = "All")
    {
        _logger.LogWarning($"Trip Start {startDate} end Date {endDate}");
        var trips = await GetFilteredTripsAsync(startDate, endDate, product);
        // if (trips is null || trips.Count == 0)
        // {
        //     return [];
        // }
        _logger.LogWarning($"Trips {trips.Count}");
        foreach (var trip in trips)
        {
            if (trip.Truck is null || trip.Origin is null || trip.Origin.Station is null)
            {
                _logger.LogWarning($"Trip {trip.Id} has missing truck or origin station data.");
                continue;
            }
        }
        return trips
            .OrderByDescending(t => t.Date)
            .Take(count)
            .Select(t => new RecentTripDto
            {
                TruckNumber = t.Truck!.TruckNo,
                Product = t.Truck.Product!.Value,
                LoadingPoint = t.Origin!.Station!.Name,
                Destination = t.Destination!.Station?.Name,
                Status = t.Status,
                LoadingDate = t.Date.ToDateTime(TimeOnly.MinValue),
                TripDurationDays = t.ReturnDate.HasValue
                ? (t.ReturnDate.Value.ToDateTime(TimeOnly.MinValue) - t.Date.ToDateTime(TimeOnly.MinValue)).Days
                : 0,
            })
            .ToList();
    }

    #endregion

    #region Driver Analytics

    public async Task<List<DriverPerformanceDto>> GetDriverPerformanceAsync(
        DateTime? startDate,
        DateTime? endDate,
        int minTrips)
    {
        return null;
        // var trips = await GetFilteredTripsAsync(startDate, endDate);

        // return trips
        //     .Where(t => t.Driver != null)
        //     .GroupBy(t => t.Driver!)
        //     .Where(g => g.Count() >= minTrips)
        //     .Select(g => new DriverPerformanceDto
        //     {
        //         DriverId = g.Key.Id,
        //         DriverName = $"{g.Key.FirstName} {g.Key.LastName}",
        //         ClosedTrips = g.Count(t => t.Status == TripStatus.Closed),
        //         OnTimeDeliveries = g.Count(t => 
        //             t.Status == TripStatus.Closed && 
        //             t.ActualCompletionDate <= t.ExpectedCompletionDate),
        //         AvgTripDurationHours = (decimal)g
        //             .Where(t => t.TripDurationDays.HasValue)
        //             .Average(t => t.TripDurationDays!.Value * 24),
        //         AvgShortagePercentage = g.Average(t => 
        //             (t.ShortageAmount ?? 0) / t.DispatchQuantity * 100),
        //         PerformanceScore = CalculateDriverScore(g)
        //     })
        //     .OrderByDescending(d => d.PerformanceScore)
        //     .ToList();
    }

    public async Task<DriverStatsDto?> GetDriverStatsAsync(
        Guid driverId,
        DateTime? startDate,
        DateTime? endDate)
    {
        return null;
        // var driver = await _context.Drivers.FindAsync(driverId);
        // if (driver == null) return null;

        // var trips = await GetFilteredTripsAsync(startDate, endDate);
        // var driverTrips = trips.Where(t => t.DriverId == driverId).ToList();

        // return new DriverStatsDto
        // {
        //     DriverName = $"{driver.FirstName} {driver.LastName}",
        //     TotalTrips = driverTrips.Count,
        //     TotalVolumeShipped = driverTrips.Sum(t => t.DispatchQuantity),
        //     OnTimePercentage = driverTrips.Any() ? 
        //         (decimal)driverTrips.Count(t => 
        //             t.ActualCompletionDate <= t.ExpectedCompletionDate) / driverTrips.Count * 100 : 0,
        //     RecentTrips = driverTrips
        //         .OrderByDescending(t => t.Date)
        //         .Take(5)
        //         .ToList()
        // };
    }

    #endregion

    #region Station Analytics

    public async Task<List<StationPerformanceDto>> GetStationPerformanceAsync(
        DateTime? startDate,
        DateTime? endDate)
    {
        //var trips = await GetFilteredTripsAsync(startDate, endDate);
        return [];
        
        // var loadingStats = trips
        //     .GroupBy(t => t.Origin.Station)
        //     .Select(g => new StationPerformanceDto
        //     {
        //         StationId = g.Key.Id,
        //         StationName = g.Key.Name,
        //         TripsProcessed = g.Count(),
        //         AvgProcessingTimeHours = (decimal)g
        //             .Where(t => t.LoadingDurationHours.HasValue)
        //             .Average(t => t.LoadingDurationHours!.Value),
        //         AvgShortagePercentage = g.Average(t => 
        //             (t.ShortageAmount ?? 0) / t.DispatchQuantity * 100)
        //     });

        // var unloadingStats = trips
        //     .GroupBy(t => t.Destination.Station)
        //     .Select(g => new StationPerformanceDto
        //     {
        //         StationId = g.Key.Id,
        //         StationName = g.Key.Name,
        //         TripsProcessed = g.Count(),
        //         AvgProcessingTimeHours = (decimal)g
        //             .Where(t => t.UnloadingDurationHours.HasValue)
        //             .Average(t => t.UnloadingDurationHours!.Value),
        //         AvgShortagePercentage = g.Average(t => 
        //             (t.ShortageAmount ?? 0) / t.DispatchQuantity * 100)
        //     });

        // return loadingStats.Concat(unloadingStats)
        //     .GroupBy(s => s.StationId)
        //     .Select(g => new StationPerformanceDto
        //     {
        //         StationId = g.Key,
        //         StationName = g.First().StationName,
        //         TripsProcessed = g.Sum(s => s.TripsProcessed),
        //         AvgProcessingTimeHours = g.Average(s => s.AvgProcessingTimeHours),
        //         AvgShortagePercentage = g.Average(s => s.AvgShortagePercentage)
        //     })
        //     .OrderByDescending(s => s.TripsProcessed)
        //     .ToList();
    }

    public async Task<StationThroughputDto?> GetStationThroughputAsync(
        Guid stationId, 
        DateOnly? startDate, 
        DateOnly? endDate,
        TimeGranularity granularity)
    {
        var station = await _context.Stations.FindAsync(stationId);
        if (station == null) return null;

        var trips = await GetFilteredTripsAsync(startDate, endDate);
        var stationTrips = trips
            .Where(t => t.Origin.StationId == stationId || 
                       t.Destination.StationId == stationId)
            .ToList();

        var throughputData = granularity switch
        {
            TimeGranularity.Hourly => stationTrips
                .GroupBy(t => new DateTime(
                    t.Date.Year, t.Date.Month, t.Date.Day, 
                    t.CreatedAt.Date.Hour, 0, 0))
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()),

            TimeGranularity.Daily => stationTrips
                .GroupBy(t => t.Date)
                .ToDictionary(
                    g => g.Key.ToDateTime(TimeOnly.MinValue),
                    g => g.Count()),

            TimeGranularity.Weekly => stationTrips
                .GroupBy(t => 
                    new DateTime(t.Date.Year, t.Date.Month, t.Date.Day).AddDays(
                        -(int)t.Date.DayOfWeek))
                .ToDictionary(
                    g => g.Key,
                    g => g.Count()),

            _ => stationTrips
                .GroupBy(t => new DateTime(t.Date.Year, t.Date.Month, 1))
                .ToDictionary(
                    g => g.Key,
                    g => g.Count())
        };

        return new StationThroughputDto(
            StationName: station.Name,
            ThroughputData: throughputData
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
    }

    #endregion

    #region Geospatial Services

    public async Task<List<TripRouteDto>> GetActiveTripRoutesAsync()
    {
        return [];
        // return await _context.Trips
        //     .Where(t => t.Status == TripStatus.Active)
        //     .Include(t => t.Truck)
        //     .Include(t => t.Origin).ThenInclude(o => o.Station)
        //     .Include(t => t.Destination).ThenInclude(d => d.Station)
        //     .Select(t => new TripRouteDto
        //     {
        //         TripId = t.Id,
        //         TruckNumber = t.Truck.TruckNo,
        //         Route = new GeoJsonLineString
        //         {
        //             Coordinates = new List<GeoJsonPosition>
        //             {
        //                 new(t.Origin.Station.Longitude, t.Origin.Station.Latitude),
        //                 new(t.Destination.Station.Longitude, t.Destination.Station.Latitude)
        //             }
        //         },
        //         Status = t.Status
        //     })
        //     .ToListAsync();
    }

    public async Task<GeoJsonFeatureCollection> GetStationsGeoJsonAsync()
    {
        throw new NotImplementedException();
        // return new GeoJsonFeatureCollection();
        // var stations = await _context.Stations
        //     .Where(s => s.Latitude != null && s.Longitude != null)
        //     .ToListAsync();

        // return new GeoJsonFeatureCollection
        // {
        //     Features = stations.Select(s => new GeoJsonFeature
        //     {
        //         Geometry = new GeoJsonPoint
        //         {
        //             Coordinates = new GeoJsonPosition(s.Longitude!.Value, s.Latitude!.Value)
        //         },
        //         Properties = new Dictionary<string, object>
        //         {
        //             ["id"] = s.Id,
        //             ["name"] = s.Name,
        //             ["type"] = s.IsDepot ? "Depot" : "Station"
        //         }
        //     }).ToList()
        // };
    }

    #endregion

    #region Helper Methods

    private async Task<List<Trip>> GetFilteredTripsAsync(DateOnly? startDate, DateOnly? endDate, string? product = "All")
    {
        var query = _context.Trips.AsNoTracking()
                                      .Include(x => x.Driver)
                                      .Include(x => x.Truck)
                                      .Include(x => x.Origin)
                                      .ThenInclude(x => x!.Station)
                                      .Include(x => x.Destination)
                                      .ThenInclude(x => x.Station)
                                      .Include(x => x.Discharges)
                                      .AsSplitQuery()
                                        .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(t => t.Date >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.Date <= endDate.Value);


        if (!product!.Contains("All"))
            query = query.Where(x => x.Truck!.Product.ToString() == product);


        return await query.ToListAsync();
    }

    private static (DateOnly?, DateOnly?) GetComparisonPeriod(DateOnly? startDate, DateOnly? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
        {
            return (null, null);
        }

        int durationDays = endDate.Value.DayNumber - startDate.Value.DayNumber;
        return (startDate.Value.AddDays(-durationDays), endDate.Value.AddDays(-durationDays));
    }

    private decimal CalculateAverageTripDuration(List<Trip> trips)
    {
        // var durations = trips
        //     .Where(t => t.date)
        //     .Select(t => t.TripDurationDays!.Value);

        // return durations.Any() ? (decimal)durations.Average() : 0;
        return 0m;
    }

    private decimal CalculateTrend(decimal current, decimal previous)
    {
        if (previous == 0) return 0;
        return (current - previous) / previous * 100;
    }

    private decimal CalculateProductTrend(Product product, DateOnly? startDate, DateOnly? endDate)
    {
        var (prevStart, prevEnd) = GetComparisonPeriod(startDate, endDate);
        
        var currentTrips = GetFilteredTripsAsync(startDate, endDate).Result
            .Where(t => t.Truck!.Product == product)
            .Sum(t => t.Origin!.Quantity) ?? 0;

        var previousTrips = GetFilteredTripsAsync(prevStart, prevEnd).Result
            .Where(t => t.Truck!.Product == product)
            .Sum(t => t.Origin!.Quantity ?? 0);

        return CalculateTrend(currentTrips, previousTrips);
    }

    private decimal CalculateDriverScore(IGrouping<Driver, Trip> driverTrips)
    {
        return 0;
        // var weights = new Dictionary<string, decimal>
        // {
        //     ["OnTimeRate"] = 0.4m,
        //     ["AvgDuration"] = 0.3m,
        //     ["ShortageRate"] = 0.3m
        // };

        // var ClosedTrips = driverTrips
        //     .Where(t => t.Status == TripStatus.Closed)
        //     .ToList();

        // if (!ClosedTrips.Any()) return 0;

        // var onTimeRate = (decimal)ClosedTrips.Count(t => 
        //     t.ActualCompletionDate <= t.ExpectedCompletionDate) / ClosedTrips.Count;

        // var avgDuration = ClosedTrips
        //     .Where(t => t.TripDurationDays.HasValue)
        //     .Average(t => t.TripDurationDays!.Value);

        // var normDuration = 1 - (avgDuration / ClosedTrips.Max(t => t.TripDurationDays ?? 1));

        // var shortageRate = 1 - ClosedTrips.Average(t => 
        //     (t.ShortageAmount ?? 0) / t.DispatchQuantity);

        // return (onTimeRate * weights["OnTimeRate"]) +
        //        (normDuration * weights["AvgDuration"]) +
        //        (shortageRate * weights["ShortageRate"]);
    }

    #endregion
}