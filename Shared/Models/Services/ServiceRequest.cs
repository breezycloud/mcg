using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Trucks;
using Shared.Models.Users;

namespace Shared.Models.Services;


public class ServiceRequest
{
    [Key]
    public Guid Id { get; set; }
    public ServiceType Type { get; set; }
    public Guid? TruckId { get; set; }
    [Required]
    public string? Description { get; set; }
    public Guid? MaintenanceSiteId { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? TreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public virtual User? CreatedBy { get; set; }
    [ForeignKey(nameof(TreatedById))]
    public virtual User? TreatedBy { get; set; }

    [ForeignKey(nameof(TruckId))]
    public virtual Truck? Truck { get; set; }


}