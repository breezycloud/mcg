using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace Shared.Dtos;

public class TripLoadingDto
{
    [Required]
    public DateTime LoadingDate { get; set; }  
    public required Guid TruckId { get; set; }
    public Product Product { get; set; }

    [Required]
    public required Guid LoadingPointId { get; set; }

    [Required]
    public string? WaybillNumber { get; set; }
    [Required]
    public decimal? DispatchQuantity { get; set; }

    public UnitOfMeasure DispatchUnit { get; set; }

    [Required]
    public required Guid? DestinationId { get; set; }
}