using System.ComponentModel.DataAnnotations;
using Shared.Enums;
using Shared.Models.Stations;

namespace Shared.Models.Trips;

public class Origin
{
    [Key]
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid StationId { get; set; }
    public decimal? Quantity { get; set; }
    public UnitOfMeasure Unit { get; set; }
    public virtual Station? Station { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}