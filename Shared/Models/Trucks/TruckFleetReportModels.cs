namespace Shared.Models.Trucks;

public class TruckFleetReportMetricsDto
{
    // North-star KPIs
    public decimal FleetUtilizationRate { get; set; }  // TrucksLoadedInPeriod / TotalDeployedTrucks * 100
    public decimal FleetAvailabilityRate { get; set; }  // Deployed trucks NOT Out of Service/Under Repair right now / TotalDeployedTrucks * 100
    public decimal AvgTurnaroundDays { get; set; }      // Closed trips in period, negative (bad-data) durations excluded
    public decimal ShortageRate { get; set; }           // Volume-weighted: total shortage / total loaded, within the period

    // Supporting KPIs
    public int TotalTrucksInFleet { get; set; }
    public int TotalDeployedTrucks { get; set; }
    public int TrucksLoadedInPeriod { get; set; }
    public int TripsInPeriod { get; set; }
    public decimal TotalQuantityLoaded { get; set; }
    public int OpenServiceRequests { get; set; }
    public decimal AvgMaintenanceTatDays { get; set; }
    public int OpenIncidents { get; set; }
}

public class TruckStatusBreakdownDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

// One row per truck — the operational "what needs my attention" table.
public class TruckPerformanceRowDto
{
    public Guid TruckId { get; set; }
    public string TruckNo { get; set; } = string.Empty;
    public string? Product { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DriverName { get; set; }
    public int TripCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal ShortageRate { get; set; }
    public decimal AvgTurnaroundDays { get; set; }
    public int OpenServiceRequests { get; set; }
    public int OpenIncidents { get; set; }
    // True when the truck is Out of Service/Under Repair or has an open service request/incident —
    // drives the visual flag in the table, so ops staff can triage instead of reading every row.
    public bool NeedsAttention { get; set; }
}

public class MaintenanceSpendByTruckDto
{
    public string TruckNo { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public int RequestCount { get; set; }
    public decimal AvgTatDays { get; set; }
}

public class MaintenanceSpendByCategoryDto
{
    public string Category { get; set; } = string.Empty; // ServiceType or ServiceItem display name
    public decimal TotalCost { get; set; }
    public int RequestCount { get; set; }
}

public class CalibrationExpiryDto
{
    public string TruckNo { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class FleetMonthlyTrendDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal UtilizationRate { get; set; }
    public decimal ShortageRate { get; set; }
    public int TripCount { get; set; }
    public string Format => $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)}-{Year}";
}
