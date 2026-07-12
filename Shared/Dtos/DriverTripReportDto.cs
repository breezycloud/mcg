namespace Shared.Dtos;

// Mirrors StationReportFilter/StationReportDto — see that file's comments for why a
// dedicated filter shape (nullable dates) is used instead of reusing ReportFilter.
//
// Named DriverTripReportDto (not DriverReportDto) to avoid colliding with the older,
// unrelated Shared.Models.Drivers.DriverReportDto used by the fleet-wide driver report.
public class DriverTripReportFilter
{
    public Guid DriverId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}

// One row per trip made by the chosen driver. DischargedQuantity is the trip-wide total
// (a driver's trip isn't split across "their portion" the way a station's delivery is) —
// so ShortageAmount = DispatchQuantity - DischargedQuantity directly. StationNames lists
// every station the trip discharged at (comma-joined; usually one, but a trip can
// discharge across multiple stations).
public class DriverTripReportDto
{
    public Guid TripId { get; set; }
    public DateTimeOffset Date { get; set; }
    public string? TruckNo { get; set; }
    public string? TruckPlate { get; set; }
    public string? StationNames { get; set; }
    public string? Product { get; set; }
    public string? LoadingDepot { get; set; }
    public string? LoadingDate { get; set; }
    public decimal DispatchQuantity { get; set; }
    public decimal DischargedQuantity { get; set; }
    public decimal ShortageAmount { get; set; }
    public string? Unit { get; set; }
    public string? Status { get; set; }
}
