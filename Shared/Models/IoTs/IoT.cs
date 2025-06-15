using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Trucks;

namespace Shared.Models.IoTs;


public class IoT
{
    [Key]
    public Guid Id { get; set; }
    public string? Type { get; set; }
    public string? SerialNumber { get; set; }
    public Guid TruckId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    [ForeignKey(nameof(TruckId))]
    public virtual Truck? Truck { get; set; }
}