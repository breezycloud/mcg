using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Drivers;
using Shared.Models.Trips;
using Shared.Models.Trucks;
using Shared.Models.Users;

namespace Shared.Models.Incidents;

public class Incident
{
    [Key] public Guid Id { get; set; }    
    public Guid IncidentTypeId { get; set; }
    public string? Description { get; set;  }
    public Guid TruckId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? TripId { get; set; }            
    public Guid CreatedById { get; set; }
    public Guid? TreatedById { get; set; }
    public Guid? ClosedById { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.New;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? TreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CreatedById))]
    public virtual User? CreatedBy { get; set; }

    [ForeignKey(nameof(TreatedById))]
    public virtual User? TreatedBy { get; set; }

    [ForeignKey(nameof(ClosedById))]
    public virtual User? ClosedBy { get; set; }

    [ForeignKey(nameof(TruckId))]
    public virtual Truck? Truck { get; set; }
    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }    

    [ForeignKey(nameof(TripId))]
    public virtual Trip? Trip { get; set; }
    [ForeignKey(nameof(IncidentTypeId))]
    public virtual IncidentType? IncidentType { get; set; }
    public virtual ICollection<IncidentHistory> History { get; set; } = [];
}

public class IncidentHistory
{
    [Key] public Guid Id { get; set; }

    public Guid IncidentId { get; set; }
    public IncidentStatus Status { get; set; } = IncidentStatus.New;
    public string? Notes { get; set; } // Notes added during this status update

    public Guid? ChangedById { get; set; } // Who triggered the status change (e.g., wrote the note)

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(IncidentId))]
    public virtual Incident? Incident { get; set; }

    [ForeignKey(nameof(ChangedById))]
    public virtual User? ChangedBy { get; set; }    
}