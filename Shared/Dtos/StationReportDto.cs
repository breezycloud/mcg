namespace Shared.Dtos;

// Dedicated request shape rather than reusing ReportFilter — ReportFilter.StartDate
// is non-nullable (defaults to DateTime.Now), which doesn't cleanly express
// "no date filter, show everything for this station" (the default view here).
public class StationReportFilter
{
    public Guid StationId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

// One row per trip that discharged at the chosen station. Shortage is
// trip-wide (loaded qty minus the sum of ALL of that trip's discharges,
// across every station it touched) — the only shortage concept that exists
// in this codebase (see ShortagesChart.razor) — while DischargedQuantity is
// scoped to just this station's portion.
public class StationReportDto
{
    public Guid TripId { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? TruckNo { get; set; }
    public string? TruckPlate { get; set; }
    public string? Product { get; set; }
    public string? DriverName { get; set; }
    public string? DriverPhone { get; set; }
    public string? LoadingDepot { get; set; }
    public string? LoadingDate { get; set; }
    public decimal DispatchQuantity { get; set; }
    public decimal DischargedQuantity { get; set; }
    public decimal ShortageAmount { get; set; }
    public string? Unit { get; set; }
    public string? Status { get; set; }
}
