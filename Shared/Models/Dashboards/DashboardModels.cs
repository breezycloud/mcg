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
    public int Active { get; set; }
    public int Closed { get; set; }
    public int Overdue { get; set; }      // Trips exceeding expected duration
}

public class ProductShipmentDto
{
    public Product Product { get; set; }
    public int TotalTrips { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal Trend { get; set; } // % change vs previous period
}

public class RecentTripDto
{
    public string TruckNumber { get; set; }
    public Product Product { get; set; }
    public string LoadingPoint { get; set; }
    public string Destination { get; set; }
    public TripStatus Status { get; set; }
    public DateTime LoadingDate { get; set; }
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

