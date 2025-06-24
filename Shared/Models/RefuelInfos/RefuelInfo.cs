using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Stations;
using Shared.Models.Trucks;

namespace Shared.Models.RefuelInfos;


public class RefuelInfo
{
    [Key]
    public Guid Id { get; set; }
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public Guid TruckId { get; set; }
    public Guid? StationId { get; set; }
    public decimal Quantity { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public decimal? Price { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    [ForeignKey(nameof(TruckId))]
    public virtual Truck? Truck { get; set; }
    [ForeignKey(nameof(StationId))]
    public virtual Station? Station { get; set; }

}