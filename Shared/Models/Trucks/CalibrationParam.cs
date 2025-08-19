using Shared.Enums;
using Shared.Models.Trips;

namespace Shared.Models.Trucks;


public class CalibrationParam
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Compartment Compartment { get; set; }

    // LPG
    public decimal? TareWeight { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal NetWeight => GrossWeight - TareWeight ?? 0;

    // AGO/ATK/PMS

    public CompartmentQuantity? Ullage { get; set; }
    public CompartmentQuantity? LiquidHeight { get; set; }
    public decimal? Overall => Ullage?.Quantity + LiquidHeight?.Quantity;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}