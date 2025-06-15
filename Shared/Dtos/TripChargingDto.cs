using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace Shared.Dtos;


public class TripDischargingDto
{
    public Guid TripId { get; set; }

    [Required]
    public bool ArrivedAtStation { get; set; }  

    public DateTime? StationArrivalDate { get; set; }
    [Required]
    public bool IsDischarged { get; set; }
    public DateTime? DischargeDate { get; set; }

    [StringLength(100)]
    public string? DischargeLocation { get; set; }

    public decimal? DischargedQuantity { get; set; }
    public UnitOfMeasure? DischargedUnit { get; set; }

    public DateTime? ReturnDate { get; set; }

    public bool? HasShortage { get; set; }
    public decimal? ShortageAmount { get; set; }

    [StringLength(500)]
    public string? Remarks { get; set; }
}