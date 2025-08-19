using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Drivers;
using Shared.Models.Shops;
using Shared.Models.Trucks;
using Shared.Models.Users;

namespace Shared.Models.Services;


public class ServiceRequest
{
    [Key]
    public Guid Id { get; set; }

    public ServiceType Type { get; set; }
    public ServiceItem Item { get; set; }
    public Guid? TruckId { get; set; }
    public Guid? DriverId { get; set; }

    [Required]
    public string? Description { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Pending;

    public Guid? MaintenanceSiteId { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? TreatedById { get; set; }
    public Guid? ClosedById { get; set; }

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

    [ForeignKey(nameof(MaintenanceSiteId))]
    public virtual MaintenanceSite? Site { get; set; }

    // Collection of status change history
    public virtual ICollection<ServiceRequestHistory> History { get; set; } = new List<ServiceRequestHistory>();
}

public class ServiceRequestHistory
{
    [Key]
    public Guid Id { get; set; }

    public Guid ServiceRequestId { get; set; }
    public RequestStatus Status { get; set; }

    public string? Notes { get; set; } // Notes added during this status update

    public Guid? ChangedById { get; set; } // Who triggered the status change (e.g., wrote the note)

    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(ServiceRequestId))]
    public virtual ServiceRequest? ServiceRequest { get; set; }

    [ForeignKey(nameof(ChangedById))]
    public virtual User? ChangedBy { get; set; }    
}