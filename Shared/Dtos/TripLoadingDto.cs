using System.ComponentModel.DataAnnotations;
using Shared.Enums;
using Shared.Models.Trucks;

namespace Shared.Dtos;

public class TripLoadingDto
{
    [Required]
    public DateTimeOffset? LoadingDate { get; set; }  
    public Guid TruckId { get; set; }
    public Truck? Truck { get; set; }
    public string? LicensePlate { get; set; }
    public Guid? DriverId { get; set; }

    [Required]
    public Guid? LoadingPointId { get; set; }

    [Required]
    public string? DispatchId { get; set; }
    public string? WaybillNumber { get; set; }
    [Required]
    public string? Destination { get; set; } = "abuja";
    [Required]
    public decimal? DispatchQuantity { get; set; }

    public UnitOfMeasure DispatchUnit { get; set; }

    [Required]
    public Guid? DestinationId { get; set; } = null;
}