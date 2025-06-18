using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
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
    [Required]
    public string? Description { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public Guid? MaintenanceSiteId { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? TreatedById { get; set; }
    public Guid? ClosedById { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset? TreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public virtual User? CreatedBy { get; set; }
    [ForeignKey(nameof(TreatedById))]
    public virtual User? TreatedBy { get; set; }
    [ForeignKey(nameof(ClosedById))]
    public virtual User? ClosedBy { get; set; }

    [ForeignKey(nameof(TruckId))]
    public virtual Truck? Truck { get; set; }
    [ForeignKey(nameof(MaintenanceSiteId))]

    public virtual MaintenanceSite? Site { get; set; }
}