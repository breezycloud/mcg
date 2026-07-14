namespace Shared.Models.Drivers;

public class DriverFleetMetricsDto
{
    // North-star KPIs
    public decimal DriverUtilizationRate { get; set; }  // Drivers with a trip in the period / TotalDrivers * 100
    public decimal AvgRating { get; set; }               // Across completed trips in the period
    public decimal AvgTurnaroundDays { get; set; }        // Closed trips in period, negative (bad-data) durations excluded
    public decimal ShortageRate { get; set; }             // Volume-weighted: total shortage / total loaded, within the period

    // Supporting KPIs
    public int TotalDrivers { get; set; }
    public int DriversActiveInPeriod { get; set; }
    public int OnTripCount { get; set; }                  // Currently on an Active/Dispatched trip right now
    public int TripsInPeriod { get; set; }
    public int LicensesExpiringSoon { get; set; }          // Within 30 days
    public int ExpiredLicenses { get; set; }
}

// One row per driver — the operational "what needs my attention" table.
public class DriverPerformanceRowDto
{
    public Guid DriverId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string LicenseStatus { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public int TripCount { get; set; }
    public decimal AvgRating { get; set; }
    public decimal ShortageRate { get; set; }
    public decimal AvgTurnaroundDays { get; set; }
    // True when the license is expired/expiring soon or the driver has had no trips in the
    // period — drives the visual flag in the table, so ops staff can triage instead of
    // reading every row.
    public bool NeedsAttention { get; set; }
}

public class DriverLicenseExpiryDto
{
    public Guid DriverId { get; set; }
    public string DriverName { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public DateOnly ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class DriverMonthlyTrendDto
{
    public int Month { get; set; }
    public int Year { get; set; }
    public int TripCount { get; set; }
    public decimal AvgRating { get; set; }
    public string Format => $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(Month)}-{Year}";
}
