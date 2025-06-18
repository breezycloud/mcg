using System.ComponentModel.DataAnnotations;
using Shared.Enums;
using Shared.Models.Trips;

namespace Shared.Models.Trucks;


public class Truck
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(20)]
    public string TruckNo { get; set; } = string.Empty;

    [Required]
    [StringLength(15)]
    public string LicensePlate { get; set; } = string.Empty;

    [StringLength(50)]
    [Required]
    public string? Manufacturer { get; set; } = "FAW";

    [StringLength(20)]
    public string? Color { get; set; } = "White";

    [Required]
    [StringLength(30)]
    public string? VIN { get; set; }

    [StringLength(30)]
    public string? EngineNo { get; set; }

    public Product? Product { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual ICollection<Trip>? Trips { get; set; } = [];
}