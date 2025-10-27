using System.ComponentModel.DataAnnotations;

namespace Shared.Dtos;


public class TripExportDto
{
    [Display(Name = "S/N")]
    public int SerialNo { get; set; }
    [Display(Name = "Dispatch Date")]
    public DateTimeOffset Date { get; set; }
    [Display(Name = "Loading Depot Arrival Date")]
    public string? LoadingDepotDate { get; set; }
    [Display(Name = "Loading Depot Arrival Date")]
    public string? LoadingDate { get; set; }
    public string? TruckPlate { get; set; }
    public string? DispatchId { get; set; }
    public string? Product { get; set; }
    [Display(Name = "Trip Status")]
    public string? Status { get; set; }
    [Display(Name = "Loading Point")]
    public string? LoadingPoint { get; set; }
    [Display(Name = "Waybill Number")]
    public string? WaybillNo { get; set; }
    [Display(Name = "Dispatch Quantity")]
    public decimal DispatchQuantity { get; set; }
    [Display(Name = "Driver Name")]
    public string? DriverName { get; set; }
    [Display(Name = "Destination")]
    public string? Dest { get; set; }
    [Display(Name = "E-lock Status")]
    public string? ElockStatus { get; set; }
    [Display(Name = "Dispatch Type")]
    public string? DispatchType { get; set; }
    [Display(Name = "Arrived Depot")]
    public string? ArrivedDepot { get; set; }
    [Display(Name = "Depot Arrival Date")]
    public string? DepotArrival { get; set; }
    [Display(Name = "Depot Name")]
    public string? DepotName { get; set; }
    public string? Invoiced { get; set; }
    [Display(Name = "Invoice Date")]
    public string? InvoiceDate { get; set; }
    [Display(Name = "Arrived Staton")]
    public string? ArrivedStation { get; set; }
    [Display(Name = "Staton Arrival Date")]
    public string? StationArrivalDate { get; set; }
    [Display(Name = "Discharging/Discharged?")]
    public string? Discharged { get; set; }
    [Display(Name = "Discharged Date")]
    public string? DischargedDate { get; set; }
    [Display(Name = "Discharge Location")]
    public string? DischargeLocation { get; set; }
    [Display(Name = "Discharged Quantity")]
    public decimal DischargedQuantity { get; set; }
    [Display(Name = "Unit (SCM/KG/MT/LTR)")]
    public string? DischargedUnit { get; set; }
    [Display(Name = "Return Date")]
    public string? ReturnDate { get; set; }
    [Display(Name = "Shortage/Overage?")]
    public string? HasShortage { get; set; }
    [Display(Name = "Shortage/Overage Amount")]
    public decimal? ShortageAmount { get; set; } 
    [Display(Name = "Remarks")]
    public string? Notes { get; set; }   
    public int DurationDays { get; set; }
    public string? DischargeSummary { get; set; }    
}