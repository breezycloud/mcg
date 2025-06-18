using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Logging;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string Action { get; set; } // "Created", "Updated", "Deleted", etc.
    
    [Required]
    public string EntityType { get; set; } // "User", "Trip", "Driver", etc.
    
    public string EntityId { get; set; } // ID of the affected entity
    
    [Required]
    public Guid UserId { get; set; } // Who performed the action
    
    public string UserName { get; set; }
    
    public string OldValues { get; set; } // JSON serialized previous state
    
    public string NewValues { get; set; } // JSON serialized new state
    
    public string AffectedFields { get; set; } // Comma-separated list of changed fields
    
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    
    public string IpAddress { get; set; }
    
    public string AdditionalInfo { get; set; }
}

public enum AuditAction
{
    Create,
    Update,
    Delete,
    Login,
    Logout,
    AccessDenied
}