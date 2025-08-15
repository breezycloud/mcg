using Shared.Enums;

namespace Shared.Models.Trips;


public record CompartmentQuantity(decimal? Quantity);

public class Metrics
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Compartment Compartment { get; set; }

    // LPG
    public decimal? TareWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal NetWeight => TareWeight + GrossWeight ?? 0;

    // AGO/ATK/PMS

    public CompartmentQuantity? Ullage { get; set; }
    public CompartmentQuantity? LiquidHeight { get; set; }
    public decimal? Overall => Ullage?.Quantity + LiquidHeight?.Quantity;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}