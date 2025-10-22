using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;
using Shared.Helpers;
using Shared.Models.Stations;

namespace Shared.Models.Trips;


public class Discharge
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TripId { get; set; }
    public Guid StationId { get; set; }
    public Station? InvoicedStation { get; set; }
    public Guid? InvoicedStationId { get; set; } = default;
    public bool IsSiding { get; set; } = default;
    public double SidingHours { get; set; } = default;
    public DateTimeOffset? TruckArrival { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DischargeStartTime { get; set; }
    public decimal QuantityDischarged { get; set; }
    public bool IsFinalDischarge { get; set; }
    [Column(TypeName = "jsonb")]
    public List<UploadResult> Files { get; set; } = [];    

    [StringLength(500)]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(TripId))]
    public virtual Trip? Trip { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(StationId))]
    public virtual Station? Station { get; set; }
}