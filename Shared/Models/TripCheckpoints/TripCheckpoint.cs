using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.BaseEntity;
using Shared.Models.Checkpoints;
using Shared.Models.Stations;
using Shared.Models.Trips;

namespace Shared.Models.TripCheckpoints;

public class TripCheckpoint
{
    [Key]
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public Guid? CheckpointId { get; set; }   
    public decimal? ExpectedDistanceFromPreviousKm { get; set; }
    public decimal? ActualDistanceFromPreviousKm { get; set; }

    public DateTimeOffset? EstimatedArrivalTime { get; set; }
    public DateTimeOffset? ActualArrivalTime { get; set; }

    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletionTime { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }     
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    [ForeignKey(nameof(TripId))]
    public virtual Trip? Trip { get; set; }
    [ForeignKey(nameof(CheckpointId))]
    public virtual Checkpoint? Checkpoint { get; set; } 
}