namespace Shared.Models.ControlRoom;

public class ControlRoomMetricsDto
{
    public int TotalTrucksInFleet { get; set; }      // All trucks, regardless of Truck.IsActive — not period-filtered
    public int TotalDeployedTrucks { get; set; }      // Fleet-wide count of Truck.IsActive trucks, not period-filtered
    public int TrucksLoadedInPeriod { get; set; }     // Distinct trucks with >=1 loading in the filter — matches Dashboard's TrucksLoadedInPeriod
    public decimal TruckUtilizationRate { get; set; } // TrucksLoadedInPeriod / TotalDeployedTrucks * 100

    public int ActiveTrips { get; set; }
    public int TripsToday { get; set; }
    public int TripsInPeriod { get; set; }
    public decimal TripCompletionRate { get; set; }  // Closed+Completed / total, within the selected period

    public decimal AvgTripDurationDays { get; set; } // within the selected period, closed trips
    public decimal ShortageRate { get; set; }         // % of discharged trips with a shortage, within the selected period
    public decimal OnTimeDeliveryRate { get; set; }   // % of closed trips within SLA, within the selected period

    public int OpenIncidents { get; set; }
    public int IncidentsInPeriod { get; set; }
    public decimal IncidentRate { get; set; }         // incidents / trips, within the selected period

    public int OpenServiceRequests { get; set; }
    public decimal AvgMaintenanceTatDays { get; set; } // closed service requests, within the selected period
}

public class ProductBreakdownDto
{
    public string Product { get; set; } = string.Empty;
    public int TripCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal ShortageRate { get; set; }          // % of this product's own loaded volume that was short
    public decimal TotalShortageQuantity { get; set; } // Absolute shortage volume — used as pie-slice weight to show each product's share of total shortage
}

// One row per product: the truck ranked highest (GetProductLeadersAsync) or lowest
// (GetProductLaggardsAsync) within that product for the selected period. Leader = most trips,
// ties broken by lowest shortage rate, further ties broken by shortest avg turnaround. Laggard =
// highest shortage rate (the metric that actually matters operationally, not simply "fewest
// trips" — a lightly-used truck isn't a problem truck), ties broken by most trips so a recurring
// issue outranks a one-off.
public class ProductLeaderDto
{
    public string Product { get; set; } = string.Empty;
    public string TruckNo { get; set; } = string.Empty;
    public int TripCount { get; set; }
    public decimal TotalQuantityDispatched { get; set; }
    public decimal ShortageRate { get; set; }
    public decimal AvgTripDurationDays { get; set; }
}

public class RecentIncidentDto
{
    public Guid Id { get; set; }
    public string TruckNo { get; set; } = string.Empty;
    public string IncidentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
