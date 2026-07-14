using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Models.Users;

namespace Shared.Models.Drivers;

public class MotorMate
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(11, ErrorMessage = "Phone must be exactly 11 digits")]
    public string? PhoneNo { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual ICollection<Driver> Drivers { get; set; } = [];

    public override string ToString() => Name;
}

// Append-only log of Driver.CurrentMotorMateId changes — same shape as
// IncidentHistory/ServiceRequestHistory (who/when, not a full field diff).
public class MotorMateHistory
{
    [Key]
    public Guid Id { get; set; }

    public Guid DriverId { get; set; }
    public Guid? PreviousMotorMateId { get; set; }
    public Guid? NewMotorMateId { get; set; }
    public string? Notes { get; set; }
    public Guid? ChangedById { get; set; }
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(DriverId))]
    public virtual Driver? Driver { get; set; }

    [ForeignKey(nameof(PreviousMotorMateId))]
    public virtual MotorMate? PreviousMotorMate { get; set; }

    [ForeignKey(nameof(NewMotorMateId))]
    public virtual MotorMate? NewMotorMate { get; set; }

    [ForeignKey(nameof(ChangedById))]
    public virtual User? ChangedBy { get; set; }
}
