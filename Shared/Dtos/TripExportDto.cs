namespace Shared.Dtos;


public class TripExportDto
{
    public DateOnly Date { get; set; }
    public string? DispatchId { get; set; }
    public string? TruckPlate { get; set; }
    public string? Product { get; set; }
    public string? Status { get; set; }
    public string? LoadingPoint { get; set; }
    public string? WaybillNo { get; set; }
    public decimal DispatchQuantity { get; set; }
    public string? DriverName { get; set; }
    public string? Dest { get; set; }
    public string? ElockStatus { get; set; }
    public string? ArrivedAtATV { get; set; }
    public string? AtvArrivalDate { get; set; }
    public string? InvoiceDate { get; set; }
    public string? ArrivedAtStation { get; set; }
    public string? StationArrivalDate { get; set; }
    public string? Discharged { get; set; }
    public string? DischargeLocation { get; set; }
    public string? DischargedDate { get; set; }
    public decimal DischargedQuantity { get; set; }
    public string? DischargedUnit { get; set; }
    public string? HasShortage { get; set; }
    public decimal? ShortageAmount { get; set; }
    public string? ReturnDate { get; set; }
    public int DurationDays { get; set; }
    public string? DischargeSummary { get; set; } // e.g., "Ikeja (5,000L) | Apapa (3,200L)"
    public string? Notes { get; set; }
}