using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Shared.Enums;
using Shared.Models.Trips;

namespace Shared.Models.Dashboards;

class DashboardModels
{

}

public class DashboardMetricsDto
{
    public int TotalTrips { get; set; }          // Total trips (open + closed)
    public int ActiveTrips { get; set; }         // Trips in progress
    public int ClosedTrips { get; set; }      // Trips with status = Closed
    public decimal AvgTripDurationDays { get; set; }  // Avg. trip duration
    public decimal TotalDispatchedQuantity { get; set; }  // Sum of all dispatched goods
    public decimal TotalShortage { get; set; }   // Sum of all shortages
}

public class TripStatusDistributionDto
{
    public int Active { get; set; } = 0;
    public int Closed { get; set; } = 0;
    public int Overdue { get; set; } = 0;    // Trips exceeding expected duration
}

public class TripStatusDto
{
    public TripStatus Status { get; set; }
    public int Count { get; set; }  // Number of trips in this status
}

public class TripMonthlySummaryDto
{
    public int Month { get; set; }  // Month number (1-12)
    public int Year { get; set; }   // Year
    public int TotalTrips { get; set; }  // Total trips in this month
    public decimal TotalQuantity { get; set; }  // Total quantity shipped in this month
    public decimal AvgDurationDays { get; set; }  // Average trip duration in days
    // Average trip duration in days    
    public string? Format => $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)}-{Year}";  // Format as YYYY-MonthName
}

public class TripMonthlyProductSummary
{
    public int Month { get; set; }  // Month number (1-12)
    public int Year { get; set; }   // Year
    public string? Product { get; set; }
    public int TotalTrips { get; set; }  // Total trips in this month
    public decimal TotalQuantity { get; set; }  // Total quantity shipped in this month
    public decimal AvgDurationDays { get; set; }  // Average trip duration in days
    // Average trip duration in days    
    public string? Format => $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)}-{Year}";  // Format as YYYY-MonthName
}

public class ProductMonthlyTripDistribution
{
    public string? Label { get; set; }
    public DistributionData[]? Data { get; set; }
}

public class DistributionData
{
    public string? Key { get; set; }
    public int Value { get; set; }
}


public class ProductShipmentDto
{
    public string? Product { get; set; }
    public int TotalTrips { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal Trend { get; set; } // % change vs previous period
}

public class RecentTripDto
{
    public string? TruckNumber { get; set; }
    public Product Product { get; set; }
    public string? LoadingPoint { get; set; }
    public string? Destination { get; set; }
    public TripStatus Status { get; set; }
    public DateTimeOffset LoadingDate { get; set; }
    public int? TripDurationDays { get; set; }
}

public class MetricsTrendDto
{
    public decimal TotalTripsTrend { get; set; }  // % change vs previous period
    public decimal ActiveTripsTrend { get; set; }
    public decimal ClosedTripsTrend { get; set; }
    public decimal AvgDurationTrend { get; set; }
}

// Driver Analytics
public record DriverPerformanceDto(
    Guid DriverId,
    string DriverName,
    int ClosedTrips,
    int OnTimeDeliveries,
    decimal AvgTripDurationHours,
    decimal AvgShortagePercentage,
    decimal PerformanceScore);

public record DriverStatsDto(
    string DriverName,
    int TotalTrips,
    decimal TotalVolumeShipped,
    decimal OnTimePercentage,
    List<Trip> RecentTrips);

// Station Analytics
public record StationPerformanceDto(
    Guid StationId,
    string StationName,
    int TripsProcessed,
    decimal AvgProcessingTimeHours,
    decimal AvgShortagePercentage);

public record StationThroughputDto(
    string StationName,
    Dictionary<DateTime, int> ThroughputData);

public enum TimeGranularity { Hourly, Daily, Weekly, Monthly }

// Geospatial
public record TripRouteDto(
    Guid TripId,
    string TruckNumber,
    GeoJsonLineString Route,
    TripStatus Status);


public abstract class GeoJsonGeometry
{
    [JsonPropertyName("type")]
    public abstract string Type { get; }
}

public class GeoJsonPoint : GeoJsonGeometry
{
    public override string Type => "Point";
    
    [JsonPropertyName("coordinates")]
    public GeoJsonPosition Coordinates { get; set; }
}

public class GeoJsonLineString : GeoJsonGeometry
{
    public override string Type => "LineString";
    
    [JsonPropertyName("coordinates")]
    public List<GeoJsonPosition> Coordinates { get; set; } = new();
}

public record GeoJsonPosition(double Longitude, double Latitude);

public record GeoJsonFeatureCollection(
    [property: JsonPropertyName("features")] List<GeoJsonFeature> Features,
    [property: JsonPropertyName("type")] string Type = "FeatureCollection");

public record GeoJsonFeature(
    [property: JsonPropertyName("type")] string Type = "Feature",
    [property: JsonPropertyName("geometry")] GeoJsonGeometry? Geometry = null,
    [property: JsonPropertyName("properties")] Dictionary<string, object>? Properties = null);


public class ShortageAnalysis
{
    public Guid TripId { get; set; }
    public string TruckNumber { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public decimal LoadingQuantity { get; set; }
    public decimal DischargedQuantity { get; set; }
    public decimal ShortageAmount { get; set; }
    public decimal VariancePercentage { get; set; }
    public DateOnly TripDate { get; set; }
}

public class ProductShortageData
{
    public string Product { get; set; } = string.Empty;
    public double TotalShortage { get; set; }
    public double AverageShortage { get; set; }
}

public class ShortageExportDto
{
    [Display(Name = "Trip ID")]
    public string TripId { get; set; } = string.Empty;

    [Display(Name = "Truck Number")]
    public string TruckNumber { get; set; } = string.Empty;

    [Display(Name = "Product")]
    public string Product { get; set; } = string.Empty;

    [Display(Name = "Loading Quantity")]
    public decimal LoadingQuantity { get; set; }

    [Display(Name = "Discharged Quantity")]
    public decimal DischargedQuantity { get; set; }

    [Display(Name = "Shortage Amount")]
    public decimal ShortageAmount { get; set; }

    [Display(Name = "Variance Percentage")]
    public decimal VariancePercentage { get; set; }

    [Display(Name = "Status")]
    public string Status { get; set; } = string.Empty;

    [Display(Name = "Trip Date")]
    public string TripDate { get; set; } = string.Empty;
}
