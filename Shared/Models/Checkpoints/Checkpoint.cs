using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.BaseEntity;
using Shared.Models.Stations;
using Shared.Models.Trips;

namespace Shared.Models.Checkpoints;

public class Checkpoint
{
    [Key]
    public Guid Id { get; set; }
    public Guid? TripId { get; set; }
    [Column(TypeName = "jsonb")]
    public Address? Address { get; set; } = new();
    public decimal? ExpectedDistanceFromPreviousKm { get; set; }
    public decimal? ActualDistanceFromPreviousKm { get; set; }
    
    public DateTimeOffset? EstimatedArrivalTime { get; set; }
    public DateTimeOffset? ActualArrivalTime { get; set; }
    
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletionTime { get; set; }
    
    [StringLength(500)]
    public string? Notes { get; set; }
    
    // Geo-coordinates
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    
    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}