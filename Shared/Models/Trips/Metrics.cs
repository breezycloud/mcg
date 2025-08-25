using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;

namespace Shared.Models.Trips;


public record CompartmentQuantity([property: Column(TypeName = "decimal(18,2)")] decimal? Quantity);

public class Metrics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Compartment Compartment { get; set; }

    // LPG
    [Column(TypeName = "decimal(18,2)")]
    public decimal? TareWeight { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? GrossWeight { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal NetWeight => GrossWeight - TareWeight ?? 0;

    // AGO/ATK/PMS

    public CompartmentQuantity? Ullage { get; set; }
    public CompartmentQuantity? LiquidHeight { get; set; }
    public decimal? Overall => Ullage?.Quantity + LiquidHeight?.Quantity;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}