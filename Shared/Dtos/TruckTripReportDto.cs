namespace Shared.Dtos;

// Mirrors StationReportFilter/StationReportDto — see that file's comments for why a
// dedicated filter shape (nullable dates) is used instead of reusing ReportFilter.
public class TruckTripReportFilter
{
    public Guid TruckId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

// One row per trip made by the chosen truck. Unlike StationReportDto, DischargedQuantity
// here is the trip-wide total (a truck's trip isn't split across "its portion" the way a
// station's delivery is) — so ShortageAmount = DispatchQuantity - DischargedQuantity directly.
// StationNames lists every station the trip discharged at (comma-joined; usually one, but a
// trip can discharge across multiple stations).
public class TruckTripReportDto
{
    public Guid TripId { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? StationNames { get; set; }
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
