using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Enums;

namespace Shared.Models.Trips;


public class Discharge
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public Guid TripId { get; set; }    
    
    [Required]
    [StringLength(100)]
    public string Location { get; set; } = string.Empty;
    
    public DateTimeOffset DischargeStartTime { get; set; } = DateTimeOffset.UtcNow;
    
    public decimal QuantityDischarged { get; set; }
    
    public bool IsFinalDischarge { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Geo-coordinates
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    // Navigation properties
    [ForeignKey(nameof(TripId))]
    public virtual Trip Trip { get; set; } = null!;
}