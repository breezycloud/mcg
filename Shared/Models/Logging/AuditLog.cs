using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Logging;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public string Action { get; set; } = string.Empty;

    [Required]
    public string EntityType { get; set; } = string.Empty;

    public string? EntityId { get; set; }

    [Required]
    public Guid UserId { get; set; }

    public string? UserName { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? AffectedFields { get; set; }

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    public string? IpAddress { get; set; }

    public string? AdditionalInfo { get; set; }
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