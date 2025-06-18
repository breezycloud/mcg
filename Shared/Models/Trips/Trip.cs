using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Checkpoints;
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
    public string? DispatchId { get; set; }
    public string? WaybillNo { get; set; }
    public ElockStatus ElockStatus { get; set; }
    public TripStatus Status { get; set; }
    [Required]
    public string? Dest { get; set; }
    public bool ArrivedAtATV { get; set; } = false;
    public string? LocationAtv { get; set; }
    public DateTimeOffset? ATVArrivalDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }
    [ForeignKey(nameof(TruckId))]
    public virtual Truck? Truck { get; set; }
    public virtual Origin? Origin { get; set; }
    public virtual Destination? Destination { get; set; }
    public virtual ICollection<Checkpoint>? Checkpoints { get; set; } = [];
   
    public int CalculateTripDuration(DateOnly createdDate, DateOnly? returnDate)
    {
        DateOnly endDate = returnDate ?? DateOnly.FromDateTime(DateTime.Today);
        return endDate.DayNumber - createdDate.DayNumber;
    }

}