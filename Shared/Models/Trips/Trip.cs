using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Drivers;
using Shared.Models.Trucks;


namespace Shared.Models.Trips;


public class Trip
{
    [Key]
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public Guid? DriverId { get; set; }
    public Guid TruckId { get; set; }
    public string? WaybillNo { get; set; }   
    public TripStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }
    [ForeignKey(nameof(TruckId))]
    public virtual Truck? Truck { get; set; }
    public virtual Origin? Origin { get; set; }
    public virtual Destination? Destination { get; set; }
}