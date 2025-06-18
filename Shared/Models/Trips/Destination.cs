using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Models.Stations;

namespace Shared.Models.Trips;

public class Destination
{
    [Key]
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid? StationId { get; set; }
    public bool ArrivedAtStation { get; set; } = false;
    public DateTimeOffset? StationArrivalDate { get; set; }
    public bool IsDischarged { get; set; } = false;
    public DateTimeOffset? DischargeDate { get; set; }

    [StringLength(100)]
    public string? DischargeLocation { get; set; }
    public decimal? DischargedQuantity { get; set; }
    public UnitOfMeasure? DischargedUnit { get; set; }
    public bool? HasShortage { get; set; }
    public decimal? ShortageAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    [ForeignKey(nameof(StationId))]
    public virtual Station? Station { get; set; }
}