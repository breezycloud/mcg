using Shared.Enums;

namespace Shared.Models.Dashboards;

class DashboardModels
{

}

public class DashboardMetricsDto
{
    public int TotalTrips { get; set; }          // Total trips (open + closed)
    public int ActiveTrips { get; set; }         // Trips in progress
    public int CompletedTrips { get; set; }      // Trips with status = Closed
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
    public Product Product { get; set; }  // CNG, PMS, ATK, LPG
    public int TotalTrips { get; set; }   // Trips per product
    public decimal TotalQuantity { get; set; }  // Quantity shipped per product
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
    public double TotalTripsTrend { get; set; }  // % change vs previous period
    public double ActiveTripsTrend { get; set; }
    public double CompletedTripsTrend { get; set; }
    public double AvgDurationTrend { get; set; }
}